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


namespace PdfClown.Documents.Contents.Fonts.TTF
{
    /**
     * A name record in the name table.
     * 
     * @author Ben Litchfield
     */
    public class NameRecord
    {
        // platform ids
        public static readonly int PLATFORM_UNICODE = 0;
        public static readonly int PLATFORM_MACINTOSH = 1;
        public static readonly int PLATFORM_ISO = 2;
        public static readonly int PLATFORM_WINDOWS = 3;

        // Unicode encoding ids
        public static readonly int ENCODING_UNICODE_1_0 = 0;
        public static readonly int ENCODING_UNICODE_1_1 = 1;
        public static readonly int ENCODING_UNICODE_2_0_BMP = 3;
        public static readonly int ENCODING_UNICODE_2_0_FULL = 4;

        // Unicode encoding ids
        public static readonly int LANGUGAE_UNICODE = 0;

        // Windows encoding ids
        public static readonly int ENCODING_WINDOWS_SYMBOL = 0;
        public static readonly int ENCODING_WINDOWS_UNICODE_BMP = 1;
        public static readonly int ENCODING_WINDOWS_UNICODE_UCS4 = 10;

        // Windows language ids
        public static readonly int LANGUGAE_WINDOWS_EN_US = 0x0409;

        // Macintosh encoding ids
        public static readonly int ENCODING_MACINTOSH_ROMAN = 0;

        // Macintosh language ids
        public static readonly int LANGUGAE_MACINTOSH_ENGLISH = 0;

        // name ids
        public static readonly int NAME_COPYRIGHT = 0;
        public static readonly int NAME_FONT_FAMILY_NAME = 1;
        public static readonly int NAME_FONT_SUB_FAMILY_NAME = 2;
        public static readonly int NAME_UNIQUE_FONT_ID = 3;
        public static readonly int NAME_FULL_FONT_NAME = 4;
        public static readonly int NAME_VERSION = 5;
        public static readonly int NAME_POSTSCRIPT_NAME = 6;
        public static readonly int NAME_TRADEMARK = 7;

        private int platformId;
        private int platformEncodingId;
        private int languageId;
        private int nameId;
        private int stringLength;
        private int stringOffset;
        private string text;

        /**
         * @return Returns the stringLength.
         */
        public int StringLength
        {
            get => stringLength;
            set => stringLength = value;
        }

        /**
         * @return Returns the stringOffset.
         */
        public int StringOffset
        {
            get => stringOffset;
            set => stringOffset = value;
        }

        /**
         * @return Returns the languageId.
         */
        public int LanguageId
        {
            get => languageId;
            set => languageId = value;
        }

        /**
         * @return Returns the nameId.
         */
        public int NameId
        {
            get => nameId;
            set => nameId = value;
        }

        /**
         * @return Returns the platformEncodingId.
         */
        public int PlatformEncodingId
        {
            get => platformEncodingId;
            set => platformEncodingId = value;
        }

        /**
         * @return Returns the platformId.
         */
        public int PlatformId
        {
            get => platformId;
            set => platformId = value;
        }

        /**
         * @return Returns the string.
         */
        public string Text
        {
            get => text;
            set => text = value;
        }

        /**
         * This will read the required data from the stream.
         * 
         * @param ttf The font that is being read.
         * @param data The stream to read the data from.
         * @ If there is an error reading the data.
         */
        public void InitData(TrueTypeFont ttf, TTFDataStream data)
        {
            platformId = data.ReadUnsignedShort();
            platformEncodingId = data.ReadUnsignedShort();
            languageId = data.ReadUnsignedShort();
            nameId = data.ReadUnsignedShort();
            stringLength = data.ReadUnsignedShort();
            stringOffset = data.ReadUnsignedShort();
        }

        /**
         * Return a string representation of this class.
         * 
         * @return A string for this class.
         */
        public override string ToString()
        {
            return
                "platform=" + platformId +
                " pEncoding=" + platformEncodingId +
                " language=" + languageId +
                " name=" + nameId +
                " " + text;
        }
    }
}
