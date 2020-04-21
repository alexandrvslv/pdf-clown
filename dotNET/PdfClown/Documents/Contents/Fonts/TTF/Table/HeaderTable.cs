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

using System;
using System.IO;

namespace PdfClown.Documents.Contents.Fonts.TTF
{
    /**
     * A table in a true type font.
     * 
     * @author Ben Litchfield
     */
    public class HeaderTable : TTFTable
    {
        /**
         * Tag to identify this table.
         */
        public const string TAG = "head";

        /**
         * Bold macStyle flag.
         */
        public static readonly int MAC_STYLE_BOLD = 1;

        /**
         * Italic macStyle flag.
         */
        public static readonly int MAC_STYLE_ITALIC = 2;

        private float version;
        private float fontRevision;
        private long checkSumAdjustment;
        private long magicNumber;
        private int flags;
        private int unitsPerEm;
        private DateTime created;
        private DateTime modified;
        private short xMin;
        private short yMin;
        private short xMax;
        private short yMax;
        private int macStyle;
        private int lowestRecPPEM;
        private short fontDirectionHint;
        private short indexToLocFormat;
        private short glyphDataFormat;

        public HeaderTable(TrueTypeFont font) : base(font)
        {
        }

        /**
         * This will read the required data from the stream.
         * 
         * @param ttf The font that is being read.
         * @param data The stream to read the data from.
         * @ If there is an error reading the data.
         */
        public override void Read(TrueTypeFont ttf, TTFDataStream data)
        {
            version = data.Read32Fixed();
            fontRevision = data.Read32Fixed();
            checkSumAdjustment = data.ReadUnsignedInt();
            magicNumber = data.ReadUnsignedInt();
            flags = data.ReadUnsignedShort();
            unitsPerEm = data.ReadUnsignedShort();
            created = data.ReadInternationalDate();
            modified = data.ReadInternationalDate();
            xMin = data.ReadSignedShort();
            yMin = data.ReadSignedShort();
            xMax = data.ReadSignedShort();
            yMax = data.ReadSignedShort();
            macStyle = data.ReadUnsignedShort();
            lowestRecPPEM = data.ReadUnsignedShort();
            fontDirectionHint = data.ReadSignedShort();
            indexToLocFormat = data.ReadSignedShort();
            glyphDataFormat = data.ReadSignedShort();
            initialized = true;
        }

        /**
         * @return Returns the checkSumAdjustment.
         */
        public long CheckSumAdjustment
        {
            get => checkSumAdjustment;
            set => checkSumAdjustment = value;
        }

        /**
         * @return Returns the created.
         */
        public DateTime Created
        {
            get => created;
            set => created = value;
        }

        /**
         * @return Returns the flags.
         */
        public int Flags
        {
            get => flags;
            set => flags = value;
        }

        /**
         * @return Returns the fontDirectionHint.
         */
        public short FontDirectionHint
        {
            get => fontDirectionHint;
            set => fontDirectionHint = value;
        }

        /**
         * @return Returns the fontRevision.
         */
        public float FontRevision
        {
            get => fontRevision;
            set => fontRevision = value;
        }

        /**
         * @return Returns the glyphDataFormat.
         */
        public short GlyphDataFormat
        {
            get => glyphDataFormat;
            set => glyphDataFormat = value;
        }

        /**
         * @return Returns the indexToLocFormat.
         */
        public short IndexToLocFormat
        {
            get => indexToLocFormat;
            set => indexToLocFormat = value;
        }

        /**
         * @return Returns the lowestRecPPEM.
         */
        public int LowestRecPPEM
        {
            get => lowestRecPPEM;
            set => lowestRecPPEM = value;
        }

        /**
         * @return Returns the macStyle.
         */
        public int MacStyle
        {
            get => macStyle;
            set => macStyle = value;
        }

        /**
         * @return Returns the magicNumber.
         */
        public long MagicNumber
        {
            get => magicNumber;
            set => magicNumber = value;
        }

        /**
         * @return Returns the modified.
         */
        public DateTime Modified
        {
            get => modified;
            set => modified = value;
        }

        /**
         * @return Returns the unitsPerEm.
         */
        public int UnitsPerEm
        {
            get => unitsPerEm;
            set => unitsPerEm = value;
        }

        /**
         * @return Returns the version.
         */
        public float Version
        {
            get => version;
            set => version = value;
        }

        /**
         * @return Returns the xMax.
         */
        public short XMax
        {
            get => xMax;
            set => xMax = value;
        }

        /**
         * @return Returns the xMin.
         */
        public short XMin
        {
            get => xMin;
            set => xMin = value;
        }

        /**
         * @return Returns the yMax.
         */
        public short YMax
        {
            get => yMax;
            set => yMax = value;
        }

        /**
         * @return Returns the yMin.
         */
        public short YMin
        {
            get => yMin;
            set => yMin = value;
        }

    }
}