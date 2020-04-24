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
using System.Diagnostics;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace PdfClown.Documents.Contents.Fonts.TTF
{
    /**
     * A 'kern' table in a true type font.
     * 
     * @author Glenn Adams
     */
    public class KerningSubtable
    {
        //private static readonly Log LOG = LogFactory.getLog(KerningSubtable.class);

        // coverage field bit masks and values
        private static readonly int COVERAGE_HORIZONTAL = 0x0001;
        private static readonly int COVERAGE_MINIMUMS = 0x0002;
        private static readonly int COVERAGE_CROSS_STREAM = 0x0004;
        private static readonly int COVERAGE_FORMAT = 0xFF00;

        private static readonly int COVERAGE_HORIZONTAL_SHIFT = 0;
        private static readonly int COVERAGE_MINIMUMS_SHIFT = 1;
        private static readonly int COVERAGE_CROSS_STREAM_SHIFT = 2;
        private static readonly int COVERAGE_FORMAT_SHIFT = 8;

        // true if horizontal kerning
        private bool horizontal;
        // true if minimum adjustment values (versus kerning values)
        private bool minimums;
        // true if cross-stream (block progression) kerning
        private bool crossStream;
        // format specific pair data
        private IPairData pairs;

        public KerningSubtable()
        {
        }

        /**
         * This will read the required data from the stream.
         * 
         * @param data The stream to read the data from.
         * @param version The version of the table to be read
         * @ If there is an error reading the data.
         */
        public void Read(TTFDataStream data, int version)
        {
            if (version == 0)
            {
                ReadSubtable0(data);
            }
            else if (version == 1)
            {
                ReadSubtable1(data);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        /**
         * Determine if subtable is designated for use in horizontal writing modes and
         * contains inline progression kerning pairs (not block progression "cross stream")
         * kerning pairs.
         *
         * @return true if subtable is for horizontal kerning
         */
        public bool IsHorizontalKerning()
        {
            return IsHorizontalKerning(false);
        }

        /**
         * Determine if subtable is designated for use in horizontal writing modes, contains
         * kerning pairs (as opposed to minimum pairs), and, if CROSS is true, then return
         * cross stream designator; otherwise, if CROSS is false, return true if cross stream
         * designator is false.
         *
         * @param cross if true, then return cross stream designator in horizontal modes
         * @return true if subtable is for horizontal kerning in horizontal modes
         */
        public bool IsHorizontalKerning(bool cross)
        {
            if (!horizontal)
            {
                return false;
            }
            else if (minimums)
            {
                return false;
            }
            else if (cross)
            {
                return crossStream;
            }
            else
            {
                return !crossStream;
            }
        }

        /**
         * Obtain kerning adjustments for GLYPHS sequence, where the
         * Nth returned adjustment is associated with the Nth glyph
         * and the succeeding non-zero glyph in the GLYPHS sequence.
         *
         * Kerning adjustments are returned in font design coordinates.
         *
         * @param glyphs a (possibly empty) array of glyph identifiers
         * @return a (possibly empty) array of kerning adjustments
         */
        public int[] GetKerning(int[] glyphs)
        {
            int[] kerning = null;
            if (pairs != null)
            {
                int ng = glyphs.Length;
                kerning = new int[ng];
                for (int i = 0; i < ng; ++i)
                {
                    int l = glyphs[i];
                    int r = -1;
                    for (int k = i + 1; k < ng; ++k)
                    {
                        int g = glyphs[k];
                        if (g >= 0)
                        {
                            r = g;
                            break;
                        }
                    }
                    kerning[i] = GetKerning(l, r);
                }
            }
            else
            {
                Debug.WriteLine("warn: No kerning subtable data available due to an unsupported kerning subtable version");
            }
            return kerning;
        }

        /**
         * Obtain kerning adjustment for glyph pair {L,R}.
         *
         * @param l left member of glyph pair
         * @param r right member of glyph pair
         * @return a (possibly zero) kerning adjustment
         */
        public int GetKerning(int l, int r)
        {
            if (pairs == null)
            {
                Debug.WriteLine("warn: No kerning subtable data available due to an unsupported kerning subtable version");
                return 0;
            }
            return pairs.GetKerning(l, r);
        }

        private void ReadSubtable0(TTFDataStream data)
        {
            int version = data.ReadUnsignedShort();
            if (version != 0)
            {
                Debug.WriteLine("info: Unsupported kerning sub-table version: " + version);
                return;
            }
            int length = data.ReadUnsignedShort();
            if (length < 6)
            {
                throw new IOException("Kerning sub-table too short, got " + length
                        + " bytes, expect 6 or more.");
            }
            int coverage = data.ReadUnsignedShort();
            if (IsBitsSet(coverage, COVERAGE_HORIZONTAL, COVERAGE_HORIZONTAL_SHIFT))
            {
                this.horizontal = true;
            }
            if (IsBitsSet(coverage, COVERAGE_MINIMUMS, COVERAGE_MINIMUMS_SHIFT))
            {
                this.minimums = true;
            }
            if (IsBitsSet(coverage, COVERAGE_CROSS_STREAM, COVERAGE_CROSS_STREAM_SHIFT))
            {
                this.crossStream = true;
            }
            int format = GetBits(coverage, COVERAGE_FORMAT, COVERAGE_FORMAT_SHIFT);
            if (format == 0)
            {
                ReadSubtable0Format0(data);
            }
            else if (format == 2)
            {
                ReadSubtable0Format2(data);
            }
            else
            {
                Debug.WriteLine("debug: Skipped kerning subtable due to an unsupported kerning subtable version: " + format);
            }
        }

        private void ReadSubtable0Format0(TTFDataStream data)
        {
            pairs = new PairData0Format0();
            pairs.Read(data);
        }

        private void ReadSubtable0Format2(TTFDataStream data)
        {
            Debug.WriteLine("info: Kerning subtable format 2 not yet supported.");
        }

        private void ReadSubtable1(TTFDataStream data)
        {
            Debug.WriteLine("info: Kerning subtable format 1 not yet supported.");
        }

        private static bool IsBitsSet(int bits, int mask, int shift)
        {
            return GetBits(bits, mask, shift) != 0;
        }

        private static int GetBits(int bits, int mask, int shift)
        {
            return (bits & mask) >> shift;
        }

        private interface IPairData
        {
            void Read(TTFDataStream data);

            int GetKerning(int l, int r);
        }

        private class PairData0Format0 : IComparer<int[]>, IPairData
        {
            private int searchRange;
            private int[][] pairs;

            public void Read(TTFDataStream data)
            {
                int numPairs = data.ReadUnsignedShort();
                searchRange = data.ReadUnsignedShort() / 6;
                int entrySelector = data.ReadUnsignedShort();
                int rangeShift = data.ReadUnsignedShort();
                pairs = new int[numPairs][];
                for (int i = 0; i < numPairs; ++i)
                {
                    int left = data.ReadUnsignedShort();
                    int right = data.ReadUnsignedShort();
                    int value = data.ReadSignedShort();
                    pairs[i] = new int[3] { left, right, value };
                }
            }

            public int GetKerning(int l, int r)
            {
                int[] key = new int[] { l, r, 0 };
                int index = Array.BinarySearch(pairs, key, this);
                if (index >= 0)
                {
                    return pairs[index][2];
                }
                return 0;
            }

            public int Compare(int[] p1, int[] p2)
            {
                Debug.Assert(p1 != null);
                Debug.Assert(p1.Length >= 2);
                Debug.Assert(p2 != null);
                Debug.Assert(p2.Length >= 2);
                int cmp1 = p1[0].CompareTo(p2[0]);
                if (cmp1 != 0)
                {
                    return cmp1;
                }
                return p1[1].CompareTo(p2[1]);
            }
        }
    }
}
