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
        public static readonly int PLATFORM_ISO = 2;//Deprecated
        public static readonly int PLATFORM_WINDOWS = 3;
        public static readonly int PLATFORM_CUSTOME = 4;

        // Unicode encoding ids
        public static readonly int ENCODING_UNI_UNICODE_1 = 0;
        public static readonly int ENCODING_UNI_UNICODE_1_1 = 1;
        public static readonly int ENCODING_UNI_ISO_10646 = 2;
        public static readonly int ENCODING_UNI_UNICODE_2_0_BMP = 3;
        public static readonly int ENCODING_UNI_UNICODE_2_0_FULL = 4;

        // ISO encoding ids
        public static readonly int ENCODING_ISO_ASCII = 0;//7-bit ASCII
        public static readonly int ENCODING_ISO_ISO_10646 = 1;
        public static readonly int ENCODING_ISO_ISO_88591 = 3;

        // Widows encoding ids
        public static readonly int ENCODING_WIN_SYMBOL = 0;//UTF-16BE
        public static readonly int ENCODING_WIN_UNICODE_BMP = 1;//UTF-16BE
        public static readonly int ENCODING_WIN_SHIFTJIS = 2;
        public static readonly int ENCODING_WIN_PRC = 3;
        public static readonly int ENCODING_WIN_BIG5 = 4;
        public static readonly int ENCODING_WIN_WANSUNG = 5;
        public static readonly int ENCODING_WIN_JOHAB = 6;
        public static readonly int ENCODING_WIN_RESERV1 = 7;
        public static readonly int ENCODING_WIN_RESERV2 = 8;
        public static readonly int ENCODING_WIN_RESERV3 = 9;
        public static readonly int ENCODING_WIN_UNICODE_FULL = 10;

        // MacOS encoding ids
        public static readonly int ENCODING_MAC_ROMAN = 0;//x-mac-romanian
        public static readonly int ENCODING_MAC_JAPANESE = 1;//x-mac-japanese
        public static readonly int ENCODING_MAC_CHINESE_TRAD = 2;//x-mac-chinesetrad
        public static readonly int ENCODING_MAC_KOREAN = 3;//x-mac-korean
        public static readonly int ENCODING_MAC_ARABIC = 4;//x-mac-arabic
        public static readonly int ENCODING_MAC_HEBREW = 5;//x-mac-hebrew
        public static readonly int ENCODING_MAC_GREEK = 6;//x-mac-greek 
        public static readonly int ENCODING_MAC_RUSSIAN = 7;//x-mac-cyrillic 
        public static readonly int ENCODING_MAC_RSYMBOL = 8;
        public static readonly int ENCODING_MAC_DEVANAGARI = 9;
        public static readonly int ENCODING_MAC_GURMUKHI = 10;
        public static readonly int ENCODING_MAC_GUJARATI = 11;
        public static readonly int ENCODING_MAC_ORIYA = 12;
        public static readonly int ENCODING_MAC_BENGALI = 13;
        public static readonly int ENCODING_MAC_TAMIL = 14;
        public static readonly int ENCODING_MAC_TELUGU = 15;
        public static readonly int ENCODING_MAC_KANNADA = 16;
        public static readonly int ENCODING_MAC_MALAYALAM = 17;
        public static readonly int ENCODING_MAC_SINHALESE = 18;
        public static readonly int ENCODING_MAC_BURMESE = 19;
        public static readonly int ENCODING_MAC_KHMER = 20;
        public static readonly int ENCODING_MAC_THAI = 21;//x-mac-thai
        public static readonly int ENCODING_MAC_LAOTIAN = 22;
        public static readonly int ENCODING_MAC_GEORGIAN = 23;
        public static readonly int ENCODING_MAC_ARMENIAN = 24;
        public static readonly int ENCODING_MAC_CHINESE_SIMP = 25;//x-mac-chinesesimp
        public static readonly int ENCODING_MAC_TIBETAN = 26;
        public static readonly int ENCODING_MAC_MONGOLIAN = 27;
        public static readonly int ENCODING_MAC_GEEZ = 28;
        public static readonly int ENCODING_MAC_SLAVIC = 29;
        public static readonly int ENCODING_MAC_VIETNAMESE = 30;
        public static readonly int ENCODING_MAC_SINDHI = 31;
        public static readonly int ENCODING_MAC_UNINTERPRETED = 32;

        // Unicode encoding ids
        public static readonly int LANGUGAE_UNICODE = 0;

        // Windows language ids
        public static readonly int LANGUGAE_WIN_EN_AU = 0x0C09;
        public static readonly int LANGUGAE_WIN_EN_BZ = 0x2809;
        public static readonly int LANGUGAE_WIN_EN_CA = 0x1009;
        public static readonly int LANGUGAE_WIN_EN_CR = 0x2409;
        public static readonly int LANGUGAE_WIN_EN_IN = 0x4009;
        public static readonly int LANGUGAE_WIN_EN_IR = 0x1809;
        public static readonly int LANGUGAE_WIN_EN_JM = 0x2009;
        public static readonly int LANGUGAE_WIN_EN_ML = 0x4409;
        public static readonly int LANGUGAE_WIN_EN_NZ = 0x1409;
        public static readonly int LANGUGAE_WIN_EN_PH = 0x3909;
        public static readonly int LANGUGAE_WIN_EN_SG = 0x4809;
        public static readonly int LANGUGAE_WIN_EN_SA = 0x1C09;
        public static readonly int LANGUGAE_WIN_EN_TT = 0x2C09;
        public static readonly int LANGUGAE_WIN_EN_UK = 0x0809;
        public static readonly int LANGUGAE_WIN_EN_US = 0x0409;
        public static readonly int LANGUGAE_WIN_EN_ZB = 0x3009;

        // Macintosh language ids
        public static readonly int LANGUGAE_MAC_ENGLISH = 0;
        public static readonly int LANGUGAE_MAC_FRENCH = 1;
        public static readonly int LANGUGAE_MAC_GERMAN = 2;
        public static readonly int LANGUGAE_MAC_ITALIAN = 3;
        public static readonly int LANGUGAE_MAC_DUTCH = 4;
        public static readonly int LANGUGAE_MAC_SWEDISH = 5;
        public static readonly int LANGUGAE_MAC_SPANISH = 6;
        public static readonly int LANGUGAE_MAC_DANISH = 7;
        public static readonly int LANGUGAE_MAC_PORTUGUESE = 8;
        public static readonly int LANGUGAE_MAC_NORWEGIAN = 9;
        public static readonly int LANGUGAE_MAC_HEBREW = 10;
        public static readonly int LANGUGAE_MAC_JAPANESE = 11;
        public static readonly int LANGUGAE_MAC_ARABIC = 12;
        public static readonly int LANGUGAE_MAC_FINNISH = 13;
        public static readonly int LANGUGAE_MAC_GREEK = 14;
        public static readonly int LANGUGAE_MAC_ICELANDIC = 15;
        public static readonly int LANGUGAE_MAC_MALTESE = 16;
        public static readonly int LANGUGAE_MAC_TURKISH = 17;
        public static readonly int LANGUGAE_MAC_CROATIAN = 18;
        public static readonly int LANGUGAE_MAC_CHINESE_TRAD = 19;
        public static readonly int LANGUGAE_MAC_URDU = 20;
        public static readonly int LANGUGAE_MAC_HINDI = 21;
        public static readonly int LANGUGAE_MAC_THAI = 22;
        public static readonly int LANGUGAE_MAC_KOREAN = 23;
        public static readonly int LANGUGAE_MAC_LITHUANIAN = 24;
        public static readonly int LANGUGAE_MAC_POLISH = 25;
        public static readonly int LANGUGAE_MAC_HUNGARIAN = 26;
        public static readonly int LANGUGAE_MAC_ESTONIAN = 27;
        public static readonly int LANGUGAE_MAC_LATVIAN = 28;
        public static readonly int LANGUGAE_MAC_SAMI = 29;
        public static readonly int LANGUGAE_MAC_FAROESE = 30;
        public static readonly int LANGUGAE_MAC_FARSI = 31;
        public static readonly int LANGUGAE_MAC_RUSSIAN = 32;
        public static readonly int LANGUGAE_MAC_CHINESE_SIMP = 33;
        public static readonly int LANGUGAE_MAC_FLEMISH = 34;
        public static readonly int LANGUGAE_MAC_IRISH_GAELIC = 35;
        public static readonly int LANGUGAE_MAC_ALBANIAN = 36;
        public static readonly int LANGUGAE_MAC_ROMANIAN = 37;
        public static readonly int LANGUGAE_MAC_CZECH = 38;


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
