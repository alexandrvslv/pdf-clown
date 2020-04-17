/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System.IO;
using System.Collections.Generic;
using PdfClown.Tokens;

namespace PdfClown.Documents.Contents.Fonts.TTF
{

    /**
     * A table in a true type font.
     * 
     * @author Ben Litchfield
     */
    public class NamingTable : TTFTable
    {
        /**
         * A tag that identifies this table type.
         */
        public static readonly string TAG = "name";

        private List<NameRecord> nameRecords;

        private Dictionary<int, Dictionary<int, Dictionary<int, Dictionary<int, string>>>> lookupTable;

        private string fontFamily = null;
        private string fontSubFamily = null;
        private string psName = null;

        public NamingTable(TrueTypeFont font)
            : base(font)
        {
        }

        /**
         * This will read the required data from the stream.
         * 
         * @param ttf The font that is being read.
         * @param data The stream to read the data from.
         * @ If there is an error reading the data.
         */
        protected override void Read(TrueTypeFont ttf, TTFDataStream data)
        {
            int formatSelector = data.ReadUnsignedShort();
            int numberOfNameRecords = data.ReadUnsignedShort();
            int offsetToStartOfStringStorage = data.ReadUnsignedShort();
            nameRecords = new List<NameRecord>(numberOfNameRecords);
            for (int i = 0; i < numberOfNameRecords; i++)
            {
                NameRecord nr = new NameRecord();
                nr.InitData(ttf, data);
                nameRecords.Add(nr);
            }

            foreach (NameRecord nr in nameRecords)
            {
                // don't try to read invalid offsets, see PDFBOX-2608
                if (nr.StringOffset > Length)
                {
                    nr.Text = null;
                    continue;
                }

                data.seek(Offset + (2 * 3) + numberOfNameRecords * 2 * 6 + nr.StringOffset);
                int platform = nr.PlatformId;
                int encoding = nr.PlatformEncodingId;
                var charset = Charset.ISO88591;
                if (platform == NameRecord.PLATFORM_WINDOWS && (encoding == NameRecord.ENCODING_WINDOWS_SYMBOL || encoding == NameRecord.ENCODING_WINDOWS_UNICODE_BMP))
                {
                    charset = Charset.UTF16LE;
                }
                else if (platform == NameRecord.PLATFORM_UNICODE)
                {
                    charset = Charset.UTF16LE;
                }
                else if (platform == NameRecord.PLATFORM_ISO)
                {
                    switch (encoding)
                    {
                        case 0:
                            charset = Charset.ASCII;
                            break;
                        case 1:
                            //not sure is this is correct??
                            charset = Charset.UTF16BE;
                            break;
                        case 2:
                            charset = Charset.ISO88591;
                            break;
                        default:
                            break;
                    }
                }
                string text = data.ReadString(nr.StringLength, charset);
                nr.Text = text;
            }

            // build multi-dimensional lookup table
            lookupTable = new Dictionary<int, Dictionary<int, Dictionary<int, Dictionary<int, string>>>>(nameRecords.Count);
            foreach (NameRecord nr in nameRecords)
            {
                // name id
                if (!lookupTable.TryGetValue(nr.NameId, out var platformLookup))
                {
                    platformLookup = new Dictionary<int, Dictionary<int, Dictionary<int, string>>>();
                    lookupTable[nr.NameId] = platformLookup;
                }
                // platform id

                if (!platformLookup.TryGetValue(nr.PlatformId, out var encodingLookup))
                {
                    encodingLookup = new Dictionary<int, Dictionary<int, string>>();
                    platformLookup[nr.PlatformId] = encodingLookup;
                }
                // encoding id
                if (!encodingLookup.TryGetValue(nr.PlatformEncodingId, out var languageLookup))
                {
                    languageLookup = new Dictionary<int, string>();
                    encodingLookup[nr.PlatformEncodingId] = languageLookup;
                }
                // language id / string
                languageLookup[nr.LanguageId] = nr.Text;
            }

            // extract strings of interest
            fontFamily = GetEnglishName(NameRecord.NAME_FONT_FAMILY_NAME);
            fontSubFamily = GetEnglishName(NameRecord.NAME_FONT_SUB_FAMILY_NAME);

            // extract PostScript name, only these two formats are valid
            psName = GetName(NameRecord.NAME_POSTSCRIPT_NAME,
                             NameRecord.PLATFORM_MACINTOSH,
                             NameRecord.ENCODING_MACINTOSH_ROMAN,
                             NameRecord.LANGUGAE_MACINTOSH_ENGLISH);
            if (psName == null)
            {
                psName = GetName(NameRecord.NAME_POSTSCRIPT_NAME,
                                 NameRecord.PLATFORM_WINDOWS,
                                 NameRecord.ENCODING_WINDOWS_UNICODE_BMP,
                                 NameRecord.LANGUGAE_WINDOWS_EN_US);
            }
            if (psName != null)
            {
                psName = psName.Trim();
            }

            initialized = true;
        }

        /**
         * Helper to get English names by best effort.
         */
        private string GetEnglishName(int nameId)
        {
            // Unicode, Full, BMP, 1.1, 1.0
            for (int i = 4; i >= 0; i--)
            {
                string nameUni =
                        GetName(nameId,
                                NameRecord.PLATFORM_UNICODE,
                                i,
                                NameRecord.LANGUGAE_UNICODE);
                if (nameUni != null)
                {
                    return nameUni;
                }
            }

            // Windows, Unicode BMP, EN-US
            string nameWin =
                    GetName(nameId,
                            NameRecord.PLATFORM_WINDOWS,
                            NameRecord.ENCODING_WINDOWS_UNICODE_BMP,
                            NameRecord.LANGUGAE_WINDOWS_EN_US);
            if (nameWin != null)
            {
                return nameWin;
            }

            // Macintosh, Roman, English
            string nameMac =
                    GetName(nameId,
                            NameRecord.PLATFORM_MACINTOSH,
                            NameRecord.ENCODING_MACINTOSH_ROMAN,
                            NameRecord.LANGUGAE_MACINTOSH_ENGLISH);
            if (nameMac != null)
            {
                return nameMac;
            }

            return null;
        }

        /**
         * Returns a name from the table, or null it it does not exist.
         *
         * @param nameId Name ID from NameRecord constants.
         * @param platformId Platform ID from NameRecord constants.
         * @param encodingId Platform Encoding ID from NameRecord constants.
         * @param languageId Language ID from NameRecord constants.
         * @return name, or null
         */
        public string GetName(int nameId, int platformId, int encodingId, int languageId)
        {
            if (!lookupTable.TryGetValue(nameId, out var platforms))
            {
                return null;
            }

            if (!platforms.TryGetValue(platformId, out var encodings))
            {
                return null;
            }
            if (!encodings.TryGetValue(encodingId, out var languages))
            {
                return null;
            }
            return languages.TryGetValue(languageId, out var text) ? text : null;
        }

        /**
         * This will get the name records for this naming table.
         *
         * @return A list of NameRecord objects.
         */
        public List<NameRecord> NameRecords
        {
            get => nameRecords;
        }

        /**
         * Returns the font family name, in English.
         *
         * @return the font family name, in English
         */
        public string FontFamily
        {
            get => fontFamily;
        }

        /**
         * Returns the font sub family name, in English.
         *
         * @return the font sub family name, in English
         */
        public string FontSubFamily
        {
            get => fontSubFamily;
        }

        /**
         * Returns the PostScript name.
         *
         * @return the PostScript name
         */
        public string PostScriptName
        {
            get => psName;
        }
    }
}
