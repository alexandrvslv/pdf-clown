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
    public class MaximumProfileTable : TTFTable
    {
        /**
         * A tag that identifies this table type.
         */
        public const string TAG = "maxp";

        private float version;
        private int numGlyphs;
        private int maxPoints;
        private int maxContours;
        private int maxCompositePoints;
        private int maxCompositeContours;
        private int maxZones;
        private int maxTwilightPoints;
        private int maxStorage;
        private int maxFunctionDefs;
        private int maxInstructionDefs;
        private int maxStackElements;
        private int maxSizeOfInstructions;
        private int maxComponentElements;
        private int maxComponentDepth;

        public MaximumProfileTable(TrueTypeFont font)
            : base(font)
        {
        }

        /**
         * @return Returns the maxComponentDepth.
         */
        public int MaxComponentDepth
        {
            get => maxComponentDepth;
            set => maxComponentDepth = value;
        }

        /**
         * @return Returns the maxComponentElements.
         */
        public int MaxComponentElements
        {
            get => maxComponentElements;
            set => maxComponentElements = value;
        }

        /**
         * @return Returns the maxCompositeContours.
         */
        public int MaxCompositeContours
        {
            get => maxCompositeContours;
            set => maxCompositeContours = value;
        }

        /**
         * @return Returns the maxCompositePoints.
         */
        public int MaxCompositePoints
        {
            get => maxCompositePoints;
            set => maxCompositePoints = value;
        }

        /**
         * @return Returns the maxContours.
         */
        public int MaxContours
        {
            get => maxContours;
            set => maxContours = value;
        }

        /**
         * @return Returns the maxFunctionDefs.
         */
        public int MaxFunctionDefs
        {
            get => maxFunctionDefs;
            set => maxFunctionDefs = value;
        }

        /**
         * @return Returns the maxInstructionDefs.
         */
        public int MaxInstructionDefs
        {
            get => maxInstructionDefs;
            set => maxInstructionDefs = value;
        }

        /**
         * @return Returns the maxPoints.
         */
        public int MaxPoints
        {
            get => maxPoints;
            set => maxPoints = value;
        }

        /**
         * @return Returns the maxSizeOfInstructions.
         */
        public int MaxSizeOfInstructions
        {
            get => maxSizeOfInstructions;
            set => maxSizeOfInstructions = value;
        }

        /**
         * @return Returns the maxStackElements.
         */
        public int MaxStackElements
        {
            get => maxStackElements;
            set => maxStackElements = value;
        }

        /**
         * @return Returns the maxStorage.
         */
        public int MaxStorage
        {
            get => maxStorage;
            set => maxStorage = value;
        }

        /**
         * @return Returns the maxTwilightPoints.
         */
        public int MaxTwilightPoints
        {
            get => maxTwilightPoints;
            set => maxTwilightPoints = value;
        }

        /**
         * @return Returns the maxZones.
         */
        public int MaxZones
        {
            get => maxZones;
            set => maxZones = value;
        }

        /**
         * @return Returns the numGlyphs.
         */
        public int NumGlyphs
        {
            get => numGlyphs;
            set => numGlyphs = value;
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
         * This will read the required data from the stream.
         * 
         * @param ttf The font that is being read.
         * @param data The stream to read the data from.
         * @ If there is an error reading the data.
         */
        public override void Read(TrueTypeFont ttf, TTFDataStream data)
        {
            version = data.Read32Fixed();
            numGlyphs = data.ReadUnsignedShort();
            maxPoints = data.ReadUnsignedShort();
            maxContours = data.ReadUnsignedShort();
            maxCompositePoints = data.ReadUnsignedShort();
            maxCompositeContours = data.ReadUnsignedShort();
            maxZones = data.ReadUnsignedShort();
            maxTwilightPoints = data.ReadUnsignedShort();
            maxStorage = data.ReadUnsignedShort();
            maxFunctionDefs = data.ReadUnsignedShort();
            maxInstructionDefs = data.ReadUnsignedShort();
            maxStackElements = data.ReadUnsignedShort();
            maxSizeOfInstructions = data.ReadUnsignedShort();
            maxComponentElements = data.ReadUnsignedShort();
            maxComponentDepth = data.ReadUnsignedShort();
            initialized = true;
        }
    }
}