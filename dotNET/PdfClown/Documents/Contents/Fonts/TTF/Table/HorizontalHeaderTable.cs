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
     */
    public class HorizontalHeaderTable : TTFTable
    {
        /**
         * A tag that identifies this table type.
         */
        public const string TAG = "hhea";

        private float version;
        private short ascender;
        private short descender;
        private short lineGap;
        private int advanceWidthMax;
        private short minLeftSideBearing;
        private short minRightSideBearing;
        private short xMaxExtent;
        private short caretSlopeRise;
        private short caretSlopeRun;
        private short reserved1;
        private short reserved2;
        private short reserved3;
        private short reserved4;
        private short reserved5;
        private short metricDataFormat;
        private int numberOfHMetrics;

        public HorizontalHeaderTable(TrueTypeFont font) : base(font)
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
            ascender = data.ReadSignedShort();
            descender = data.ReadSignedShort();
            lineGap = data.ReadSignedShort();
            advanceWidthMax = data.ReadUnsignedShort();
            minLeftSideBearing = data.ReadSignedShort();
            minRightSideBearing = data.ReadSignedShort();
            xMaxExtent = data.ReadSignedShort();
            caretSlopeRise = data.ReadSignedShort();
            caretSlopeRun = data.ReadSignedShort();
            reserved1 = data.ReadSignedShort();
            reserved2 = data.ReadSignedShort();
            reserved3 = data.ReadSignedShort();
            reserved4 = data.ReadSignedShort();
            reserved5 = data.ReadSignedShort();
            metricDataFormat = data.ReadSignedShort();
            numberOfHMetrics = data.ReadUnsignedShort();
            initialized = true;
        }

        /**
         * @return Returns the advanceWidthMax.
         */
        public int AdvanceWidthMax
        {
            get => advanceWidthMax;
            set => advanceWidthMax = value;
        }

        /**
         * @return Returns the ascender.
         */
        public short Ascender
        {
            get => ascender;
            set => ascender = value;
        }

        /**
         * @return Returns the caretSlopeRise.
         */
        public short CaretSlopeRise
        {
            get => caretSlopeRise;
            set => caretSlopeRise = value;
        }

        /**
         * @return Returns the caretSlopeRun.
         */
        public short CaretSlopeRun
        {
            get => caretSlopeRun;
            set => caretSlopeRun = value;
        }

        /**
         * @return Returns the descender.
         */
        public short Descender
        {
            get => descender;
            set => descender = value;
        }

        /**
         * @return Returns the lineGap.
         */
        public short LineGap
        {
            get => lineGap;
            set => lineGap = value;
        }

        /**
         * @return Returns the metricDataFormat.
         */
        public short MetricDataFormat
        {
            get => metricDataFormat;
            set => metricDataFormat = value;
        }

        /**
         * @return Returns the minLeftSideBearing.
         */
        public short MinLeftSideBearing
        {
            get => minLeftSideBearing;
            set => minLeftSideBearing = value;
        }

        /**
         * @return Returns the minRightSideBearing.
         */
        public short MinRightSideBearing
        {
            get => minRightSideBearing;
            set => minRightSideBearing = value;
        }

        /**
         * @return Returns the numberOfHMetrics.
         */
        public int NumberOfHMetrics
        {
            get => numberOfHMetrics;
            set => numberOfHMetrics = value;
        }

        /**
         * @return Returns the reserved1.
         */
        public short Reserved1
        {
            get => reserved1;
            set => reserved1 = value;
        }

        /**
         * @return Returns the reserved2.
         */
        public short Reserved2
        {
            get => reserved2;
            set => reserved2 = value;
        }

        /**
         * @return Returns the reserved3.
         */
        public short Reserved3
        {
            get => reserved3;
            set => reserved3 = value;
        }

        /**
         * @return Returns the reserved4.
         */
        public short Reserved4
        {
            get => reserved4;
            set => reserved4 = value;
        }

        /**
         * @return Returns the reserved5.
         */
        public short Reserved5
        {
            get => reserved5;
            set => reserved5 = value;
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
         * @return Returns the xMaxExtent.
         */
        public short XMaxExtent
        {
            get => xMaxExtent;
            set => xMaxExtent = value;
        }

    }
}