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
     * A table in a true type font.
     * 
     * @author Ben Litchfield
     * 
     */
    public class OS2WindowsMetricsTable : TTFTable
    {

        /**
         * Weight class constant.
         */
        public static readonly int WEIGHT_CLASS_THIN = 100;
        /**
         * Weight class constant.
         */
        public static readonly int WEIGHT_CLASS_ULTRA_LIGHT = 200;
        /**
         * Weight class constant.
         */
        public static readonly int WEIGHT_CLASS_LIGHT = 300;
        /**
         * Weight class constant.
         */
        public static readonly int WEIGHT_CLASS_NORMAL = 400;
        /**
         * Weight class constant.
         */
        public static readonly int WEIGHT_CLASS_MEDIUM = 500;
        /**
         * Weight class constant.
         */
        public static readonly int WEIGHT_CLASS_SEMI_BOLD = 600;
        /**
         * Weight class constant.
         */
        public static readonly int WEIGHT_CLASS_BOLD = 700;
        /**
         * Weight class constant.
         */
        public static readonly int WEIGHT_CLASS_EXTRA_BOLD = 800;
        /**
         * Weight class constant.
         */
        public static readonly int WEIGHT_CLASS_BLACK = 900;

        /**
         * Width class constant.
         */
        public static readonly int WIDTH_CLASS_ULTRA_CONDENSED = 1;
        /**
         * Width class constant.
         */
        public static readonly int WIDTH_CLASS_EXTRA_CONDENSED = 2;
        /**
         * Width class constant.
         */
        public static readonly int WIDTH_CLASS_CONDENSED = 3;
        /**
         * Width class constant.
         */
        public static readonly int WIDTH_CLASS_SEMI_CONDENSED = 4;
        /**
         * Width class constant.
         */
        public static readonly int WIDTH_CLASS_MEDIUM = 5;
        /**
         * Width class constant.
         */
        public static readonly int WIDTH_CLASS_SEMI_EXPANDED = 6;
        /**
         * Width class constant.
         */
        public static readonly int WIDTH_CLASS_EXPANDED = 7;
        /**
         * Width class constant.
         */
        public static readonly int WIDTH_CLASS_EXTRA_EXPANDED = 8;
        /**
         * Width class constant.
         */
        public static readonly int WIDTH_CLASS_ULTRA_EXPANDED = 9;

        /**
         * Family class constant.
         */
        public static readonly int FAMILY_CLASS_NO_CLASSIFICATION = 0;
        /**
         * Family class constant.
         */
        public static readonly int FAMILY_CLASS_OLDSTYLE_SERIFS = 1;
        /**
         * Family class constant.
         */
        public static readonly int FAMILY_CLASS_TRANSITIONAL_SERIFS = 2;
        /**
         * Family class constant.
         */
        public static readonly int FAMILY_CLASS_MODERN_SERIFS = 3;
        /**
         * Family class constant.
         */
        public static readonly int FAMILY_CLASS_CLAREDON_SERIFS = 4;
        /**
         * Family class constant.
         */
        public static readonly int FAMILY_CLASS_SLAB_SERIFS = 5;
        /**
         * Family class constant.
         */
        public static readonly int FAMILY_CLASS_FREEFORM_SERIFS = 7;
        /**
         * Family class constant.
         */
        public static readonly int FAMILY_CLASS_SANS_SERIF = 8;
        /**
         * Family class constant.
         */
        public static readonly int FAMILY_CLASS_ORNAMENTALS = 9;
        /**
         * Family class constant.
         */
        public static readonly int FAMILY_CLASS_SCRIPTS = 10;
        /**
         * Family class constant.
         */
        public static readonly int FAMILY_CLASS_SYMBOLIC = 12;

        /**
         * Restricted License embedding: must not be modified, embedded or exchanged in any manner.
         *
         * <p>For Restricted License embedding to take effect, it must be the only level of embedding
         * selected.
         */
        public static readonly short FSTYPE_RESTRICTED = 0x0001;

        /**
         * Preview and Print embedding: the font may be embedded, and temporarily loaded on the
         * remote system. No edits can be applied to the document.
         */
        public static readonly short FSTYPE_PREVIEW_AND_PRINT = 0x0004;

        /**
         * Editable embedding: the font may be embedded but must only be installed temporarily on other
         * systems. Documents may be edited and changes saved.
         */
        public static readonly short FSTYPE_EDITIBLE = 0x0008;

        /**
         * No subsetting: the font must not be subsetted prior to embedding.
         */
        public static readonly short FSTYPE_NO_SUBSETTING = 0x0100;

        /**
         * Bitmap embedding only: only bitmaps contained in the font may be embedded. No outline data
         * may be embedded. Other embedding restrictions specified in bits 0-3 and 8 also apply.
         */
        public static readonly short FSTYPE_BITMAP_ONLY = 0x0200;

        private int version;
        private short averageCharWidth;
        private int weightClass;
        private int widthClass;
        private short fsType;
        private short subscriptXSize;
        private short subscriptYSize;
        private short subscriptXOffset;
        private short subscriptYOffset;
        private short superscriptXSize;
        private short superscriptYSize;
        private short superscriptXOffset;
        private short superscriptYOffset;
        private short strikeoutSize;
        private short strikeoutPosition;
        private int familyClass;
        private byte[] panose = new byte[10];
        private long unicodeRange1;
        private long unicodeRange2;
        private long unicodeRange3;
        private long unicodeRange4;
        private string achVendId = "XXXX";
        private int fsSelection;
        private int firstCharIndex;
        private int lastCharIndex;
        private int typoAscender;
        private int typoDescender;
        private int typoLineGap;
        private int winAscent;
        private int winDescent;
        private long codePageRange1 = 0;
        private long codePageRange2 = 0;
        private int sxHeight;
        private int sCapHeight;
        private int usDefaultChar;
        private int usBreakChar;
        private int usMaxContext;

        public OS2WindowsMetricsTable(TrueTypeFont font) : base(font)
        {

        }

        /**
         * @return Returns the achVendId.
         */
        public string AchVendId
        {
            get => achVendId;
            set => achVendId = value;
        }

        /**
         * @return Returns the averageCharWidth.
         */
        public short AverageCharWidth
        {
            get => averageCharWidth;
            set => averageCharWidth = value;
        }

        /**
         * @return Returns the codePageRange1.
         */
        public long CodePageRange1
        {
            get => codePageRange1;
            set => codePageRange1 = value;
        }

        /**
         * @return Returns the codePageRange2.
         */
        public long CodePageRange2
        {
            get => codePageRange2;
            set => codePageRange2 = value;
        }

        /**
         * @return Returns the familyClass.
         */
        public int FamilyClass
        {
            get => familyClass;
            set => familyClass = value;
        }

        /**
         * @return Returns the firstCharIndex.
         */
        public int FirstCharIndex
        {
            get => firstCharIndex;
            set => firstCharIndex = value;
        }

        /**
         * @return Returns the fsSelection.
         */
        public int FsSelection
        {
            get => fsSelection;
            set => fsSelection = value;
        }

        /**
         * @return Returns the fsType.
         */
        public short FsType
        {
            get => fsType;
            set => fsType = value;
        }

        /**
         * @return Returns the lastCharIndex.
         */
        public int LastCharIndex
        {
            get => lastCharIndex;
            set => lastCharIndex = value;
        }

        /**
         * @return Returns the panose.
         */
        public byte[] Panose
        {
            get => panose;
            set => panose = value;
        }

        /**
         * @return Returns the strikeoutPosition.
         */
        public short StrikeoutPosition
        {
            get => strikeoutPosition;
            set => strikeoutPosition = value;
        }

        /**
         * @return Returns the strikeoutSize.
         */
        public short StrikeoutSize
        {
            get => strikeoutSize;
            set => strikeoutSize = value;
        }

        /**
         * @return Returns the subscriptXOffset.
         */
        public short SubscriptXOffset
        {
            get => subscriptXOffset;
            set => subscriptXOffset = value;
        }

        /**
         * @return Returns the subscriptXSize.
         */
        public short SubscriptXSize
        {
            get => subscriptXSize;
            set => subscriptXSize = value;
        }

        /**
         * @return Returns the subscriptYOffset.
         */
        public short SubscriptYOffset
        {
            get => subscriptYOffset;
            set => subscriptYOffset = value;
        }

        /**
         * @return Returns the subscriptYSize.
         */
        public short SubscriptYSize
        {
            get => subscriptYSize;
            set => subscriptYSize = value;
        }

        /**
         * @return Returns the superscriptXOffset.
         */
        public short SuperscriptXOffset
        {
            get => superscriptXOffset;
            set => superscriptXOffset = value;
        }

        /**
         * @return Returns the superscriptXSize.
         */
        public short SuperscriptXSize
        {
            get => superscriptXSize;
            set => superscriptXSize = value;
        }

        /**
         * @return Returns the superscriptYOffset.
         */
        public short SuperscriptYOffset
        {
            get => superscriptYOffset;
            set => superscriptYOffset = value;
        }

        /**
         * @return Returns the superscriptYSize.
         */
        public short SuperscriptYSize
        {
            get => superscriptYSize;
            set => superscriptYSize = value;
        }

        /**
         * @return Returns the typoLineGap.
         */
        public int TypoLineGap
        {
            get => typoLineGap;
            set => typoLineGap = value;
        }

        /**
         * @return Returns the typoAscender.
         */
        public int TypoAscender
        {
            get => typoAscender;
            set => typoAscender = value;
        }

        /**
         * @return Returns the typoDescender.
         */
        public int TypoDescender
        {
            get => typoDescender;
            set => typoDescender = value;
        }

        /**
         * @return Returns the unicodeRange1.
         */
        public long UnicodeRange1
        {
            get => unicodeRange1;
            set => unicodeRange1 = value;
        }

        /**
         * @return Returns the unicodeRange2.
         */
        public long UnicodeRange2
        {
            get => unicodeRange2;
            set => unicodeRange2 = value;
        }

        /**
         * @return Returns the unicodeRange3.
         */
        public long UnicodeRange3
        {
            get => unicodeRange3;
            set => unicodeRange3 = value;
        }

        /**
         * @return Returns the unicodeRange4.
         */
        public long UnicodeRange4
        {
            get => unicodeRange4;
            set => unicodeRange4 = value;
        }

        /**
         * @return Returns the version.
         */
        public int Version
        {
            get => version;
            set => version = value;
        }

        /**
         * @return Returns the weightClass.
         */
        public int WeightClass
        {
            get => weightClass;
            set => weightClass = value;
        }

        /**
         * @return Returns the widthClass.
         */
        public int WidthClass
        {
            get => widthClass;
            set => widthClass = value;
        }

        /**
         * @return Returns the winAscent.
         */
        public int WinAscent
        {
            get => winAscent;
            set => winAscent = value;
        }

        /**
         * @return Returns the winDescent.
         */
        public int WinDescent
        {
            get => winDescent;
            set => winDescent = value;
        }

        /**
         * Returns the sxHeight.
         */
        public int Height
        {
            get => sxHeight;
            set => sxHeight = value;
        }

        /**
         * Returns the sCapHeight.
         */
        public int CapHeight
        {
            get => sCapHeight;
        }

        /**
         * Returns the usDefaultChar.
         */
        public int DefaultChar
        {
            get => usDefaultChar;
        }

        /**
         * Returns the usBreakChar.
         */
        public int BreakChar
        {
            get => usBreakChar;
        }

        /**
         * Returns the usMaxContext.
         */
        public int MaxContext
        {
            get => usMaxContext;
        }

        /**
         * A tag that identifies this table type.
         */
        public static readonly string TAG = "OS/2";

        /**
         * This will read the required data from the stream.
         * 
         * @param ttf The font that is being read.
         * @param data The stream to read the data from.
         * @ If there is an error reading the data.
         */
        protected override void Read(TrueTypeFont ttf, TTFDataStream data)
        {
            version = data.ReadUnsignedShort();
            averageCharWidth = data.ReadSignedShort();
            weightClass = data.ReadUnsignedShort();
            widthClass = data.ReadUnsignedShort();
            fsType = data.ReadSignedShort();
            subscriptXSize = data.ReadSignedShort();
            subscriptYSize = data.ReadSignedShort();
            subscriptXOffset = data.ReadSignedShort();
            subscriptYOffset = data.ReadSignedShort();
            superscriptXSize = data.ReadSignedShort();
            superscriptYSize = data.ReadSignedShort();
            superscriptXOffset = data.ReadSignedShort();
            superscriptYOffset = data.ReadSignedShort();
            strikeoutSize = data.ReadSignedShort();
            strikeoutPosition = data.ReadSignedShort();
            familyClass = data.ReadSignedShort();
            panose = data.Read(10);
            unicodeRange1 = data.ReadUnsignedInt();
            unicodeRange2 = data.ReadUnsignedInt();
            unicodeRange3 = data.ReadUnsignedInt();
            unicodeRange4 = data.ReadUnsignedInt();
            achVendId = data.ReadString(4);
            fsSelection = data.ReadUnsignedShort();
            firstCharIndex = data.ReadUnsignedShort();
            lastCharIndex = data.ReadUnsignedShort();
            typoAscender = data.ReadSignedShort();
            typoDescender = data.ReadSignedShort();
            typoLineGap = data.ReadSignedShort();
            winAscent = data.ReadUnsignedShort();
            winDescent = data.ReadUnsignedShort();
            if (version >= 1)
            {
                codePageRange1 = data.ReadUnsignedInt();
                codePageRange2 = data.ReadUnsignedInt();
            }
            if (version >= 1.2)
            {
                sxHeight = data.ReadSignedShort();
                sCapHeight = data.ReadSignedShort();
                usDefaultChar = data.ReadUnsignedShort();
                usBreakChar = data.ReadUnsignedShort();
                usMaxContext = data.ReadUnsignedShort();
            }
            initialized = true;
        }
    }
}