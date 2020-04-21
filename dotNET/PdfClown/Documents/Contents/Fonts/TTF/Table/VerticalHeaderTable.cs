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
     * A vertical header 'vhea' table in a TrueType or OpenType font.
     *
     * Supports versions 1.0 and 1.1, for which the only difference is changing
     * the specification names and descriptions of the ascender, descender,
     * and lineGap fields to vertTypoAscender, vertTypoDescender, vertTypeLineGap.
     *
     * This table is required by the OpenType CJK Font Guidelines for "all
     * OpenType fonts that are used for vertical writing".
     * 
     * This table is specified in both the TrueType and OpenType specifications.
     * 
     * @author Glenn Adams
     * 
     */
    public class VerticalHeaderTable : TTFTable
    {
        /**
         * A tag that identifies this table type.
         */
        public const string TAG = "vhea";

        private float version;
        private short ascender;
        private short descender;
        private short lineGap;
        private int advanceHeightMax;
        private short minTopSideBearing;
        private short minBottomSideBearing;
        private short yMaxExtent;
        private short caretSlopeRise;
        private short caretSlopeRun;
        private short caretOffset;
        private short reserved1;
        private short reserved2;
        private short reserved3;
        private short reserved4;
        private short metricDataFormat;
        private int numberOfVMetrics;

        public VerticalHeaderTable(TrueTypeFont font)
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
        public override void Read(TrueTypeFont ttf, TTFDataStream data)
        {
            version = data.Read32Fixed();
            ascender = data.ReadSignedShort();
            descender = data.ReadSignedShort();
            lineGap = data.ReadSignedShort();
            advanceHeightMax = data.ReadUnsignedShort();
            minTopSideBearing = data.ReadSignedShort();
            minBottomSideBearing = data.ReadSignedShort();
            yMaxExtent = data.ReadSignedShort();
            caretSlopeRise = data.ReadSignedShort();
            caretSlopeRun = data.ReadSignedShort();
            caretOffset = data.ReadSignedShort();
            reserved1 = data.ReadSignedShort();
            reserved2 = data.ReadSignedShort();
            reserved3 = data.ReadSignedShort();
            reserved4 = data.ReadSignedShort();
            metricDataFormat = data.ReadSignedShort();
            numberOfVMetrics = data.ReadUnsignedShort();
            initialized = true;
        }

        /**
         * @return Returns the advanceHeightMax.
         */
        public int AdvanceHeightMax
        {
            get => advanceHeightMax;
            set => advanceHeightMax = value;
        }

        /**
         * @return Returns the ascender.
         */
        public short Ascender
        {
            get => ascender;
        }

        /**
         * @return Returns the caretSlopeRise.
         */
        public short CaretSlopeRise
        {
            get => caretSlopeRise;
        }

        /**
         * @return Returns the caretSlopeRun.
         */
        public short CaretSlopeRun
        {
            get => caretSlopeRun;
        }

        /**
         * @return Returns the caretOffset.
         */
        public short CaretOffset
        {
            get => caretOffset;
        }

        /**
         * @return Returns the descender.
         */
        public short Descender
        {
            get => descender;
        }

        /**
         * @return Returns the lineGap.
         */
        public short LineGap
        {
            get => lineGap;
        }

        /**
         * @return Returns the metricDataFormat.
         */
        public short MetricDataFormat
        {
            get => metricDataFormat;
        }

        /**
         * @return Returns the minTopSideBearing.
         */
        public short MinTopSideBearing
        {
            get => minTopSideBearing;
        }

        /**
         * @return Returns the minBottomSideBearing.
         */
        public short MinBottomSideBearing
        {
            get => minBottomSideBearing;
        }

        /**
         * @return Returns the numberOfVMetrics.
         */
        public int NumberOfVMetrics
        {
            get => numberOfVMetrics;
        }

        /**
         * @return Returns the reserved1.
         */
        public short Reserved1
        {
            get => reserved1;
        }

        /**
         * @return Returns the reserved2.
         */
        public short Reserved2
        {
            get => reserved2;
        }

        /**
         * @return Returns the reserved3.
         */
        public short Reserved3
        {
            get => reserved3;
        }

        /**
         * @return Returns the reserved4.
         */
        public short Reserved4
        {
            get => reserved4;
        }

        /**
         * @return Returns the version.
         */
        public float Version
        {
            get => version;
        }
        /**
         * @return Returns the yMaxExtent.
         */
        public short YMaxExtent
        {
            get => yMaxExtent;
        }
    }
}