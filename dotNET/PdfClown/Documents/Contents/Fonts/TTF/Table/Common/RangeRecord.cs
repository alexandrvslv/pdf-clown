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

namespace PdfClown.Documents.Contents.Fonts.TTF.Table.Common
{
    /// <summary>
    /// This class models the
    /// <a href="https://docs.microsoft.com/en-us/typography/opentype/spec/chapter2#coverage-format-2">Range Record in the
    /// Coverage format 2</a> in the Open Type layout common tables.
    /// @author Palash Ray
    /// </summary>
    public class RangeRecord
    {
        private readonly ushort startGlyphID;
        private readonly ushort endGlyphID;
        private readonly int startCoverageIndex;

        public RangeRecord(ushort startGlyphID, ushort endGlyphID, int startCoverageIndex)
        {
            this.startGlyphID = startGlyphID;
            this.endGlyphID = endGlyphID;
            this.startCoverageIndex = startCoverageIndex;
        }

        public ushort StartGlyphID
        {
            get => startGlyphID;
        }

        public ushort EndGlyphID
        {
            get => endGlyphID;
        }

        public int StartCoverageIndex
        {
            get => startCoverageIndex;
        }

        public override string ToString()
        {
            return $"RangeRecord[startGlyphID={startGlyphID},endGlyphID={endGlyphID},startCoverageIndex={startCoverageIndex}]";
        }
    }
}