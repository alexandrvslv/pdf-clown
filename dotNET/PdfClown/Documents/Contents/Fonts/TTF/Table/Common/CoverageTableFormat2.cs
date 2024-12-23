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
using System.Collections.Generic;

namespace PdfClown.Documents.Contents.Fonts.TTF.Table.Common
{
    /// <summary>
    /// This class models the
    /// <a href="https://docs.microsoft.com/en-us/typography/opentype/spec/chapter2#coverage-format-2">Coverage format 2</a>
    /// in the Open Type layout common tables.
    /// @author Palash Ray
    /// </summary>
    public class CoverageTableFormat2 : CoverageTableFormat1
    {
        private readonly RangeRecord[] rangeRecords;

        public CoverageTableFormat2(int coverageFormat, RangeRecord[] rangeRecords)
                : base(coverageFormat, GetRangeRecordsAsArray(rangeRecords))
        {
            this.rangeRecords = rangeRecords;
        }

        public RangeRecord[] RangeRecords
        {
            get => rangeRecords;
        }

        private static ushort[] GetRangeRecordsAsArray(RangeRecord[] rangeRecords)
        {
            var glyphIds = new List<ushort>();

            foreach (RangeRecord rangeRecord in rangeRecords)
            {
                for (var glyphId = rangeRecord.StartGlyphID; glyphId <= rangeRecord.EndGlyphID; glyphId++)
                {
                    glyphIds.Add(glyphId);
                }
            }

            return glyphIds.ToArray();
        }
        
        public override string ToString()
        {
            return $"CoverageTableFormat2[coverageFormat={CoverageFormat}]";
        }
    }
}
