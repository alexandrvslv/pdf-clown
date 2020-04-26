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
 * distributed under the License input distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
namespace PdfClown.Documents.Contents.Fonts.TTF
{

    using System.IO;
    using System.Collections.Generic;
    using System.Diagnostics;
    using PdfClown.Tokens;
    using System;
    using PdfClown.Util.Collections.Generic;
    using System.Linq;
    using System.ComponentModel;
    using System.Runtime.InteropServices;
    using System.Collections.Concurrent;


    /**
     * Subsetter for TrueType (TTF) fonts.
     *
     * <p>Originally developed by Wolfgang Glas for
     * <a href="https://clazzes.org/display/SKETCH/Clazzes.org+Sketch+Home">Sketch</a>.
     *
     * @author Wolfgang Glas
     */
    public sealed class TTFSubsetter
    {
        //private static readonly Log LOG = LogFactory.Log(TTFSubsetter.class);

        private static readonly byte[] PAD_BUF = new byte[] { 0, 0, 0 };

        private readonly TrueTypeFont ttf;
        private readonly ICmapLookup unicodeCmap;
        private readonly SortedDictionary<int, int> uniToGID;

        private readonly List<string> keepTables;
        private readonly SortedSet<int> glyphIds; // new glyph ids
        private string prefix;
        private bool hasAddedCompoundReferences;

        /**
         * Creates a subsetter for the given font.
         *
         * @param ttf the font to be subset
         */
        public TTFSubsetter(TrueTypeFont ttf)
               : this(ttf, null)
        {
        }

        /**
         * Creates a subsetter for the given font.
         * 
         * @param ttf the font to be subset
         * @param tables optional tables to keep if present
         */
        public TTFSubsetter(TrueTypeFont ttf, List<string> tables)
        {
            this.ttf = ttf;
            this.keepTables = tables;

            uniToGID = new SortedDictionary<int, int>();
            glyphIds = new SortedSet<int>();

            // find the best Unicode cmap
            this.unicodeCmap = ttf.GetUnicodeCmapLookup();

            // always copy GID 0
            glyphIds.Add(0);
        }

        /**
         * Sets the prefix to add to the font's PostScript name.
         *
         * @param prefix
         */
        public void SetPrefix(string prefix)
        {
            this.prefix = prefix;
        }

        /**
         * Add the given character code to the subset.
         * 
         * @param unicode character code
         */
        public void Add(int unicode)
        {
            int gid = unicodeCmap.GetGlyphId(unicode);
            if (gid != 0)
            {
                uniToGID[unicode] = gid;
                glyphIds.Add(gid);
            }
        }

        /**
         * Add the given character codes to the subset.
         *
         * @param unicodeSet character code set
         */
        public void AddAll(ISet<int> unicodeSet)
        {
            foreach (var item in unicodeSet) { Add(item); }
        }

        /**
         * Returns the map of new -&gt; old GIDs.
         */
        public Dictionary<int, int> GetGIDMap()
        {
            AddCompoundReferences();

            Dictionary<int, int> newToOld = new Dictionary<int, int>();
            int newGID = 0;
            foreach (int oldGID in glyphIds)
            {
                newToOld[newGID] = oldGID;
                newGID++;
            }
            return newToOld;
        }

        /**
         * @param output The data output stream.
         * @param nTables The number of table.
         * @return The file offset of the first TTF table to write.
         * @ Upon errors.
         */
        private long WriteFileHeader(BinaryWriter output, int nTables)
        {
            output.Write(0x00010000);
            output.Write((short)nTables);

            short mask = (short)((uint)nTables).HighestOneBit();
            short searchRange = (short)(mask * 16);
            output.Write((short)searchRange);

            var entrySelector = (short)log2(mask);

            output.Write((short)entrySelector);

            // numTables * 16 - searchRange
            int last = 16 * nTables - searchRange;
            output.Write((short)last);

            return 0x00010000L + ToUInt32(nTables, searchRange) + ToUInt32(entrySelector, last);
        }

        private long WriteTableHeader(BinaryWriter output, string tag, long offset, byte[] bytes)
        {
            long checksum = 0;
            for (int nup = 0, n = bytes.Length; nup < n; nup++)
            {
                checksum += (bytes[nup] & 0xffL) << 24 - nup % 4 * 8;
            }
            checksum &= 0xffffffffL;

            byte[] tagbytes = Charset.ASCII.GetBytes(tag);

            output.Write(tagbytes, 0, 4);
            output.Write((int)checksum);
            output.Write((int)offset);
            output.Write((int)bytes.Length);

            // account for the checksum twice, once for the header field, once for the content itself
            return ToUInt32(tagbytes) + checksum + checksum + offset + bytes.Length;
        }

        private void WriteTableBody(BinaryWriter os, byte[] bytes)
        {
            int n = bytes.Length;
            os.Write(bytes, 0, n);
            if (n % 4 != 0)
            {
                os.Write(PAD_BUF, 0, 4 - n % 4);
            }
        }

        private byte[] buildHeadTable()
        {
            using (var bos = new MemoryStream())
            using (var output = new BinaryWriter(bos))
            {
                HeaderTable h = ttf.Header;
                WriteFixed(output, h.Version);
                WriteFixed(output, h.FontRevision);
                WriteUint32(output, 0); // h.CheckSumAdjustment()
                WriteUint32(output, h.MagicNumber);
                WriteUint16(output, h.Flags);
                WriteUint16(output, h.UnitsPerEm);
                WriteLongDateTime(output, h.Created);
                WriteLongDateTime(output, h.Modified);
                WriteSInt16(output, h.XMin);
                WriteSInt16(output, h.YMin);
                WriteSInt16(output, h.XMax);
                WriteSInt16(output, h.YMax);
                WriteUint16(output, h.MacStyle);
                WriteUint16(output, h.LowestRecPPEM);
                WriteSInt16(output, h.FontDirectionHint);
                // force long format of 'loca' table
                WriteSInt16(output, (short)1); // h.IndexToLocFormat()
                WriteSInt16(output, h.GlyphDataFormat);
                output.Flush();

                return bos.ToArray();
            }
        }

        private byte[] buildHheaTable()
        {
            using (var bos = new MemoryStream())
            using (var output = new BinaryWriter(bos))
            {
                HorizontalHeaderTable h = ttf.HorizontalHeader;
                WriteFixed(output, h.Version);
                WriteSInt16(output, h.Ascender);
                WriteSInt16(output, h.Descender);
                WriteSInt16(output, h.LineGap);
                WriteUint16(output, h.AdvanceWidthMax);
                WriteSInt16(output, h.MinLeftSideBearing);
                WriteSInt16(output, h.MinRightSideBearing);
                WriteSInt16(output, h.XMaxExtent);
                WriteSInt16(output, h.CaretSlopeRise);
                WriteSInt16(output, h.CaretSlopeRun);
                WriteSInt16(output, h.Reserved1); // caretOffset
                WriteSInt16(output, h.Reserved2);
                WriteSInt16(output, h.Reserved3);
                WriteSInt16(output, h.Reserved4);
                WriteSInt16(output, h.Reserved5);
                WriteSInt16(output, h.MetricDataFormat);

                // input there a GID >= numberOfHMetrics ? Then keep the last entry of original hmtx table,
                // (add if it isn't in our set of GIDs), see also in buildHmtxTable()
                int hmetrics = glyphIds.GetViewBetween(0, h.NumberOfHMetrics).Count();
                if (glyphIds.LastOrDefault() >= h.NumberOfHMetrics && !glyphIds.Contains(h.NumberOfHMetrics - 1))
                {
                    ++hmetrics;
                }
                WriteUint16(output, hmetrics);

                output.Flush();
                return bos.ToArray();
            }
        }

        private bool ShouldCopyNameRecord(NameRecord nr)
        {
            return nr.PlatformId == NameRecord.PLATFORM_WINDOWS
                    && nr.PlatformEncodingId == NameRecord.ENCODING_WINDOWS_UNICODE_BMP
                    && nr.LanguageId == NameRecord.LANGUGAE_WINDOWS_EN_US
                    && nr.NameId >= 0 && nr.NameId < 7;
        }

        private byte[] BuildNameTable()
        {
            using (var bos = new MemoryStream())
            using (var output = new BinaryWriter(bos))
            {
                NamingTable name = ttf.Naming;
                if (name == null || keepTables != null && !keepTables.Contains("name", StringComparer.Ordinal))
                {
                    return null;
                }

                List<NameRecord> nameRecords = name.NameRecords;
                int numRecords = (int)nameRecords.Count(p => ShouldCopyNameRecord(p));
                WriteUint16(output, 0);
                WriteUint16(output, numRecords);
                WriteUint16(output, 2 * 3 + 2 * 6 * numRecords);

                if (numRecords == 0)
                {
                    return null;
                }

                byte[][] names = new byte[numRecords][];
                int j = 0;
                foreach (NameRecord record in nameRecords)
                {
                    if (ShouldCopyNameRecord(record))
                    {
                        int platform = record.PlatformId;
                        int encoding = record.PlatformEncodingId;
                        var charset = Charset.ISO88591;

                        if (platform == CmapTable.PLATFORM_WINDOWS &&
                            encoding == CmapTable.ENCODING_WIN_UNICODE_BMP)
                        {
                            charset = Charset.UTF16BE;
                        }
                        else if (platform == 2) // ISO [deprecated]=
                        {
                            if (encoding == 0) // 7-bit ASCII
                            {
                                charset = Charset.ASCII;
                            }
                            else if (encoding == 1) // ISO 10646=
                            {
                                //not sure input this input correct??
                                charset = Charset.UTF16BE;
                            }
                        }
                        string value = record.Text;
                        if (record.NameId == 6 && prefix != null)
                        {
                            value = prefix + value;
                        }
                        names[j] = charset.GetBytes(value);
                        j++;
                    }
                }

                int offset = 0;
                j = 0;
                foreach (NameRecord nr in nameRecords)
                {
                    if (ShouldCopyNameRecord(nr))
                    {
                        WriteUint16(output, nr.PlatformId);
                        WriteUint16(output, nr.PlatformEncodingId);
                        WriteUint16(output, nr.LanguageId);
                        WriteUint16(output, nr.NameId);
                        WriteUint16(output, names[j].Length);
                        WriteUint16(output, offset);
                        offset += names[j].Length;
                        j++;
                    }
                }

                for (int i = 0; i < numRecords; i++)
                {
                    output.Write(names[i]);
                }

                output.Flush();
                return bos.ToArray();
            }
        }

        private byte[] BuildMaxpTable()
        {
            using (var bos = new MemoryStream())
            using (var output = new BinaryWriter(bos))
            {
                MaximumProfileTable p = ttf.MaximumProfile;
                WriteFixed(output, 1.0);
                WriteUint16(output, glyphIds.Count);
                WriteUint16(output, p.MaxPoints);
                WriteUint16(output, p.MaxContours);
                WriteUint16(output, p.MaxCompositePoints);
                WriteUint16(output, p.MaxCompositeContours);
                WriteUint16(output, p.MaxZones);
                WriteUint16(output, p.MaxTwilightPoints);
                WriteUint16(output, p.MaxStorage);
                WriteUint16(output, p.MaxFunctionDefs);
                WriteUint16(output, p.MaxInstructionDefs);
                WriteUint16(output, p.MaxStackElements);
                WriteUint16(output, p.MaxSizeOfInstructions);
                WriteUint16(output, p.MaxComponentElements);
                WriteUint16(output, p.MaxComponentDepth);

                output.Flush();
                return bos.ToArray();
            }
        }

        private byte[] BuildOS2Table()
        {
            OS2WindowsMetricsTable os2 = ttf.OS2Windows;
            if (os2 == null || uniToGID.Count == 0 || keepTables != null && !keepTables.Contains("OS/2", StringComparer.Ordinal))
            {
                return null;
            }

            using (var bos = new MemoryStream())
            using (var output = new BinaryWriter(bos))
            {

                WriteUint16(output, os2.Version);
                WriteSInt16(output, os2.AverageCharWidth);
                WriteUint16(output, os2.WeightClass);
                WriteUint16(output, os2.WidthClass);

                WriteSInt16(output, os2.FsType);

                WriteSInt16(output, os2.SubscriptXSize);
                WriteSInt16(output, os2.SubscriptYSize);
                WriteSInt16(output, os2.SubscriptXOffset);
                WriteSInt16(output, os2.SubscriptYOffset);

                WriteSInt16(output, os2.SuperscriptXSize);
                WriteSInt16(output, os2.SuperscriptYSize);
                WriteSInt16(output, os2.SuperscriptXOffset);
                WriteSInt16(output, os2.SuperscriptYOffset);

                WriteSInt16(output, os2.StrikeoutSize);
                WriteSInt16(output, os2.StrikeoutPosition);
                WriteSInt16(output, (short)os2.FamilyClass);
                output.Write(os2.Panose);

                WriteUint32(output, 0);
                WriteUint32(output, 0);
                WriteUint32(output, 0);
                WriteUint32(output, 0);

                output.Write(Charset.ASCII.GetBytes(os2.AchVendId));

                WriteUint16(output, os2.FsSelection);
                WriteUint16(output, uniToGID.Keys.First());
                WriteUint16(output, uniToGID.Keys.Last());
                WriteUint16(output, os2.TypoAscender);
                WriteUint16(output, os2.TypoDescender);
                WriteUint16(output, os2.TypoLineGap);
                WriteUint16(output, os2.WinAscent);
                WriteUint16(output, os2.WinDescent);

                output.Flush();
                return bos.ToArray();
            }
        }

        // never returns null
        private byte[] BuildLocaTable(long[] newOffsets)
        {
            using (var bos = new MemoryStream())
            using (var output = new BinaryWriter(bos))
            {
                foreach (long offset in newOffsets)
                {
                    WriteUint32(output, offset);
                }

                output.Flush();
                return bos.ToArray();
            }
        }

        /**
         * Resolve compound glyph references.
         */
        private void AddCompoundReferences()
        {
            if (hasAddedCompoundReferences)
            {
                return;
            }
            hasAddedCompoundReferences = true;

            bool hasNested;
            do
            {
                GlyphTable g = ttf.Glyph;
                long[] offsets = ttf.IndexToLocation.Offsets;
                Bytes.Buffer input = ttf.OriginalData;
                ISet<int> glyphIdsToAdd = null;
                try
                {
                    long isResult = input.Skip(g.Offset);

                    if (isResult.CompareTo(g.Offset) != 0)
                    {
                        Debug.WriteLine($"debug: Tried skipping {g.Offset} bytes but skipped only {isResult} bytes");
                    }

                    long lastOff = 0L;
                    foreach (int glyphId in glyphIds)
                    {
                        long offset = offsets[glyphId];
                        long len = offsets[glyphId + 1] - offset;
                        isResult = input.Skip(offset - lastOff);

                        if (isResult.CompareTo(offset - lastOff) != 0)
                        {
                            Debug.WriteLine($"debug: Tried skipping {(offset - lastOff)} bytes but skipped only {isResult} bytes");
                        }

                        sbyte[] buf = new sbyte[(int)len];
                        isResult = input.Read(buf);

                        if (isResult.CompareTo(len) != 0)
                        {
                            Debug.WriteLine($"debug: Tried reading {len} bytes but only {isResult} bytes read");
                        }

                        // rewrite glyphIds for compound glyphs
                        if (buf.Length >= 2 && buf[0] == -1 && buf[1] == -1)
                        {
                            int off = 2 * 5;
                            int flags;
                            do
                            {
                                flags = (buf[off] & 0xff) << 8 | buf[off + 1] & 0xff;
                                off += 2;
                                int ogid = (buf[off] & 0xff) << 8 | buf[off + 1] & 0xff;
                                if (!glyphIds.Contains(ogid))
                                {
                                    if (glyphIdsToAdd == null)
                                    {
                                        glyphIdsToAdd = new HashSet<int>();
                                    }
                                    glyphIdsToAdd.Add(ogid);
                                }
                                off += 2;
                                // ARG_1_AND_2_ARE_WORDS
                                if ((flags & 1 << 0) != 0)
                                {
                                    off += 2 * 2;
                                }
                                else
                                {
                                    off += 2;
                                }
                                // WE_HAVE_A_TWO_BY_TWO
                                if ((flags & 1 << 7) != 0)
                                {
                                    off += 2 * 4;
                                }
                                // WE_HAVE_AN_X_AND_Y_SCALE
                                else if ((flags & 1 << 6) != 0)
                                {
                                    off += 2 * 2;
                                }
                                // WE_HAVE_A_SCALE
                                else if ((flags & 1 << 3) != 0)
                                {
                                    off += 2;
                                }
                            }
                            while ((flags & 1 << 5) != 0); // MORE_COMPONENTS

                        }
                        lastOff = offsets[glyphId + 1];
                    }
                }
                finally
                {
                    input.Dispose();
                }
                if (glyphIdsToAdd != null)
                {
                    glyphIds.AddAll(glyphIdsToAdd);
                }
                hasNested = glyphIdsToAdd != null;
            }
            while (hasNested);
        }

        // never returns null
        private byte[] BuildGlyfTable(long[] newOffsets)
        {

            GlyphTable g = ttf.Glyph;
            long[] offsets = ttf.IndexToLocation.Offsets;
            MemoryStream bos = new MemoryStream();

            using (Bytes.Buffer input = ttf.OriginalData)
            {
                long isResult = input.Skip(g.Offset);

                if (isResult.CompareTo(g.Offset) != 0)
                {
                    Debug.WriteLine($"debug: Tried skipping {g.Offset} bytes but skipped only {isResult} bytes");
                }

                long prevEnd = 0;    // previously read glyph offset
                long newOffset = 0;  // new offset for the glyph in the subset font
                int newGid = 0;      // new GID in subset font

                // for each glyph in the subset
                foreach (int gid in glyphIds)
                {
                    long offset = offsets[gid];
                    long length = offsets[gid + 1] - offset;

                    newOffsets[newGid++] = newOffset;
                    isResult = input.Skip(offset - prevEnd);

                    if (isResult.CompareTo(offset - prevEnd) != 0)
                    {
                        Debug.WriteLine($"debug: Tried skipping {(offset - prevEnd)} bytes but skipped only {isResult} bytes");
                    }

                    sbyte[] buf = new sbyte[(int)length];
                    isResult = input.Read(buf);

                    if (isResult.CompareTo(length) != 0)
                    {
                        Debug.WriteLine($"debug: Tried reading {length} bytes but only {isResult} bytes read");
                    }

                    // detect glyph type
                    if (buf.Length >= 2 && buf[0] == -1 && buf[1] == -1)
                    {
                        // compound glyph
                        int off = 2 * 5;
                        int flags;
                        do
                        {
                            // flags
                            flags = (buf[off] & 0xff) << 8 | buf[off + 1] & 0xff;
                            off += 2;

                            // glyphIndex
                            int componentGid = (buf[off] & 0xff) << 8 | buf[off + 1] & 0xff;
                            if (!glyphIds.Contains(componentGid))
                            {
                                glyphIds.Add(componentGid);
                            }

                            int newComponentGid = GetNewGlyphId(componentGid);
                            buf[off] = (sbyte)((uint)(newComponentGid) >> 8);
                            buf[off + 1] = (sbyte)newComponentGid;
                            off += 2;

                            // ARG_1_AND_2_ARE_WORDS
                            if ((flags & 1 << 0) != 0)
                            {
                                off += 2 * 2;
                            }
                            else
                            {
                                off += 2;
                            }
                            // WE_HAVE_A_TWO_BY_TWO
                            if ((flags & 1 << 7) != 0)
                            {
                                off += 2 * 4;
                            }
                            // WE_HAVE_AN_X_AND_Y_SCALE
                            else if ((flags & 1 << 6) != 0)
                            {
                                off += 2 * 2;
                            }
                            // WE_HAVE_A_SCALE
                            else if ((flags & 1 << 3) != 0)
                            {
                                off += 2;
                            }
                        }
                        while ((flags & 1 << 5) != 0); // MORE_COMPONENTS

                        // WE_HAVE_INSTRUCTIONS
                        if ((flags & 0x0100) == 0x0100)
                        {
                            // USHORT numInstr
                            int numInstr = (buf[off] & 0xff) << 8 | buf[off + 1] & 0xff;
                            off += 2;

                            // BYTE instr[numInstr]
                            off += numInstr;
                        }

                        // write the compound glyph
                        bos.Write((byte[])(Array)buf, 0, off);

                        // offset to start next glyph
                        newOffset += off;
                    }
                    else if (buf.Length > 0)
                    {
                        // copy the entire glyph
                        bos.Write((byte[])(Array)buf, 0, buf.Length);

                        // offset to start next glyph
                        newOffset += buf.Length;
                    }

                    // 4-byte alignment
                    if (newOffset % 4 != 0)
                    {
                        int len = 4 - (int)(newOffset % 4);
                        bos.Write(PAD_BUF, 0, len);
                        newOffset += len;
                    }

                    prevEnd = offset + length;
                }
                newOffsets[newGid++] = newOffset;
            }

            return bos.ToArray();
        }

        private int GetNewGlyphId(int oldGid)
        {
            return glyphIds.GetViewBetween(0, oldGid).Count;
        }

        private byte[] BuildCmapTable()
        {
            if (ttf.Cmap == null || uniToGID.Count == 0 || keepTables != null && !keepTables.Contains("cmap", StringComparer.Ordinal))
            {
                return null;
            }

            using (var bos = new MemoryStream())
            using (var output = new BinaryWriter(bos))
            {
                // cmap header
                WriteUint16(output, 0); // version
                WriteUint16(output, 1); // numberSubtables

                // encoding record
                WriteUint16(output, CmapTable.PLATFORM_WINDOWS); // platformID
                WriteUint16(output, CmapTable.ENCODING_WIN_UNICODE_BMP); // platformSpecificID
                WriteUint32(output, 12); // offset 4 * 2 + 4

                // build Format 4 subtable (Unicode BMP)
                var it = uniToGID.GetEnumerator();
                it.MoveNext();
                var lastChar = it.Current;
                var prevChar = lastChar;
                int lastGid = GetNewGlyphId(lastChar.Value);

                // +1 because .notdef input missing in uniToGID
                int[] startCode = new int[uniToGID.Count + 1];
                int[] endCode = new int[uniToGID.Count + 1];
                int[] idDelta = new int[uniToGID.Count + 1];
                int segCount = 0;
                while (it.MoveNext())
                {
                    var curChar2Gid = it.Current;
                    int curGid = GetNewGlyphId(curChar2Gid.Value);

                    // todo: need format Format 12 for non-BMP
                    if (curChar2Gid.Key > 0xFFFF)
                    {
                        throw new NotSupportedException("non-BMP Unicode character");
                    }

                    if (curChar2Gid.Key != prevChar.Key + 1 ||
                        curGid - lastGid != curChar2Gid.Key - lastChar.Key)
                    {
                        if (lastGid != 0)
                        {
                            // don't emit ranges, which map to GID 0, the
                            // undef glyph input emitted a the very last segment
                            startCode[segCount] = lastChar.Key;
                            endCode[segCount] = prevChar.Key;
                            idDelta[segCount] = lastGid - lastChar.Key;
                            segCount++;
                        }
                        else if (!lastChar.Key.Equals(prevChar.Key))
                        {
                            // shorten ranges which start with GID 0 by one
                            startCode[segCount] = lastChar.Key + 1;
                            endCode[segCount] = prevChar.Key;
                            idDelta[segCount] = lastGid - lastChar.Key;
                            segCount++;
                        }
                        lastGid = curGid;
                        lastChar = curChar2Gid;
                    }
                    prevChar = curChar2Gid;
                }

                // trailing segment
                startCode[segCount] = lastChar.Key;
                endCode[segCount] = prevChar.Key;
                idDelta[segCount] = lastGid - lastChar.Key;
                segCount++;

                // GID 0
                startCode[segCount] = 0xffff;
                endCode[segCount] = 0xffff;
                idDelta[segCount] = 1;
                segCount++;

                // write format 4 subtable
                int searchRange = 2 * (int)Math.Pow(2, log2(segCount));
                WriteUint16(output, 4); // format
                WriteUint16(output, 8 * 2 + segCount * 4 * 2); // length
                WriteUint16(output, 0); // language
                WriteUint16(output, segCount * 2); // segCountX2
                WriteUint16(output, searchRange); // searchRange
                WriteUint16(output, log2(searchRange / 2)); // entrySelector
                WriteUint16(output, 2 * segCount - searchRange); // rangeShift

                // endCode[segCount]
                for (int i = 0; i < segCount; i++)
                {
                    WriteUint16(output, endCode[i]);
                }

                // reservedPad
                WriteUint16(output, 0);

                // startCode[segCount]
                for (int i = 0; i < segCount; i++)
                {
                    WriteUint16(output, startCode[i]);
                }

                // idDelta[segCount]
                for (int i = 0; i < segCount; i++)
                {
                    WriteUint16(output, idDelta[i]);
                }

                for (int i = 0; i < segCount; i++)
                {
                    WriteUint16(output, 0);
                }

                return bos.ToArray();
            }
        }

        private byte[] BuildPostTable()
        {
            PostScriptTable post = ttf.PostScript;
            if (post == null || keepTables != null && !keepTables.Contains("post", StringComparer.Ordinal))
            {
                return null;
            }

            using (var bos = new MemoryStream())
            using (var output = new BinaryWriter(bos))
            {

                WriteFixed(output, 2.0); // version
                WriteFixed(output, post.ItalicAngle);
                WriteSInt16(output, post.UnderlinePosition);
                WriteSInt16(output, post.UnderlineThickness);
                WriteUint32(output, post.IsFixedPitch);
                WriteUint32(output, post.MinMemType42);
                WriteUint32(output, post.MaxMemType42);
                WriteUint32(output, post.MinMemType1);
                WriteUint32(output, post.MaxMemType1);

                // version 2.0

                // numberOfGlyphs
                WriteUint16(output, glyphIds.Count);

                // glyphNameIndex[numGlyphs]
                ConcurrentDictionary<string, int> names = new ConcurrentDictionary<string, int>(StringComparer.Ordinal);
                foreach (int gid in glyphIds)
                {
                    string name = post.GetName(gid);
                    if (WGL4Names.MAC_GLYPH_NAMES_INDICES.TryGetValue(name, out int macId))
                    {
                        // the name input implicit, as it's from MacRoman
                        WriteUint16(output, macId);
                    }
                    else
                    {
                        // the name will be written explicitly
                        int ordinal = names.GetOrAdd(name, (p) => names.Count);
                        WriteUint16(output, 258 + ordinal);
                    }
                }

                // names[numberNewGlyphs]
                foreach (string name in names.Keys)
                {
                    byte[] buf = Charset.ASCII.GetBytes(name);
                    WriteUint8(output, buf.Length);
                    output.Write(buf);
                }

                output.Flush();
                return bos.ToArray();
            }
        }

        private byte[] BuildHmtxTable()
        {
            MemoryStream bos = new MemoryStream();

            HorizontalHeaderTable h = ttf.HorizontalHeader;
            HorizontalMetricsTable hm = ttf.HorizontalMetrics;
            Bytes.Buffer input = ttf.OriginalData;

            // more info: https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6hmtx.html
            int lastgid = h.NumberOfHMetrics - 1;
            // true if lastgid input not in the set: we'll need its width (but not its left side bearing) later
            bool needLastGidWidth = false;
            if (glyphIds.LastOrDefault() > lastgid && !glyphIds.Contains(lastgid))
            {
                needLastGidWidth = true;
            }

            try
            {
                long isResult = input.Skip(hm.Offset);

                if (isResult.CompareTo(hm.Offset) != 0)
                {
                    Debug.WriteLine($"debug: Tried skipping {hm.Offset} bytes but only {isResult} bytes skipped");
                }

                long lastOffset = 0;
                foreach (int glyphId in glyphIds)
                {
                    // offset in original file
                    long offset;
                    if (glyphId <= lastgid)
                    {
                        // copy width and lsb
                        offset = glyphId * 4L;
                        lastOffset = ÑopyBytes(input, bos, offset, lastOffset, 4);
                    }
                    else
                    {
                        if (needLastGidWidth)
                        {
                            // one time only: copy width from lastgid, whose width applies
                            // to all later glyphs
                            needLastGidWidth = false;
                            offset = lastgid * 4L;
                            lastOffset = ÑopyBytes(input, bos, offset, lastOffset, 2);

                            // then go on with lsb from actual glyph (lsb are individual even in monotype fonts)
                        }

                        // copy lsb only, as we are beyond numOfHMetrics
                        offset = h.NumberOfHMetrics * 4L + (glyphId - h.NumberOfHMetrics) * 2L;
                        lastOffset = ÑopyBytes(input, bos, offset, lastOffset, 2);
                    }
                }

                return bos.ToArray();
            }
            finally
            {
                input.Dispose();
            }
        }

        private long ÑopyBytes(Bytes.Buffer input, Stream os, long newOffset, long lastOffset, int count)
        {
            // skip over from last original offset
            long nskip = newOffset - lastOffset;
            if (nskip != input.Skip(nskip))
            {
                throw new EndOfStreamException("Unexpected EOF exception parsing glyphId of hmtx table.");
            }
            byte[] buf = new byte[count];
            if (count != input.Read(buf, 0, count))
            {
                throw new EndOfStreamException("Unexpected EOF exception parsing glyphId of hmtx table.");
            }
            os.Write(buf, 0, count);
            return newOffset + count;
        }

        /**
         * Write the subfont to the given output stream.
         *
         * @param os the stream used for writing. It will be closed by this method.
         * @ if something went wrong.
         * @throws IllegalStateException if the subset input empty.
         */
        public void WriteToStream(Stream os)
        {
            if (glyphIds.Count == 0 && uniToGID.Count == 0)
            {
                Debug.WriteLine("info: font subset input empty");
            }

            AddCompoundReferences();

            using (var output = new BinaryWriter(os, Charset.ASCII, true))
            {
                long[] newLoca = new long[glyphIds.Count + 1];

                // generate tables in dependency order
                byte[] head = buildHeadTable();
                byte[] hhea = buildHheaTable();
                byte[] maxp = BuildMaxpTable();
                byte[] name = BuildNameTable();
                byte[] os2 = BuildOS2Table();
                byte[] glyf = BuildGlyfTable(newLoca);
                byte[] loca = BuildLocaTable(newLoca);
                byte[] cmap = BuildCmapTable();
                byte[] hmtx = BuildHmtxTable();
                byte[] post = BuildPostTable();

                // save to TTF in optimized order
                Dictionary<string, byte[]> tables = new Dictionary<string, byte[]>(StringComparer.Ordinal);
                if (os2 != null)
                {
                    tables["OS/2"] = os2;
                }
                if (cmap != null)
                {
                    tables["cmap"] = cmap;
                }
                tables["glyf"] = glyf;
                tables["head"] = head;
                tables["hhea"] = hhea;
                tables["hmtx"] = hmtx;
                tables["loca"] = loca;
                tables["maxp"] = maxp;
                if (name != null)
                {
                    tables["name"] = name;
                }
                if (post != null)
                {
                    tables["post"] = post;
                }

                // copy all other tables
                foreach (KeyValuePair<string, TTFTable> entry in ttf.TableMap)
                {
                    string tag = entry.Key;
                    TTFTable table = entry.Value;

                    if (!tables.ContainsKey(tag) && (keepTables == null || keepTables.Contains(tag, StringComparer.Ordinal)))
                    {
                        tables[tag] = ttf.GetTableBytes(table);
                    }
                }

                // calculate checksum
                long checksum = WriteFileHeader(output, tables.Count);
                long offset = 12L + 16L * tables.Count;
                foreach (var entry in tables)
                {
                    checksum += WriteTableHeader(output, entry.Key, offset, entry.Value);
                    offset += (entry.Value.Length + 3) / 4 * 4;
                }
                checksum = 0xB1B0AFBAL - (checksum & 0xffffffffL);

                // update checksumAdjustment in 'head' table
                head[8] = (byte)(((uint)checksum) >> 24);
                head[9] = (byte)(((uint)checksum) >> 16);
                head[10] = (byte)(((uint)checksum) >> 8);
                head[11] = (byte)checksum;
                foreach (byte[] bytes in tables.Values)
                {
                    WriteTableBody(output, bytes);
                }
            }
        }

        private void WriteFixed(BinaryWriter output, double f)
        {
            double ip = Math.Floor(f);
            double fp = (f - ip) * 65536.0;
            output.Write((short)ip);
            output.Write((short)fp);
        }

        private void WriteUint32(BinaryWriter output, long l)
        {
            output.Write((uint)l);
        }

        private void WriteUint16(BinaryWriter output, int i)
        {
            output.Write((ushort)i);
        }

        private void WriteSInt16(BinaryWriter output, short i)
        {
            output.Write(i);
        }

        private void WriteUint8(BinaryWriter output, int i)
        {
            output.Write((byte)i);
        }

        private void WriteLongDateTime(BinaryWriter output, DateTime calendar)
        {
            // inverse operation of TTFDataStream.readInternationalDate()
            DateTime cal = new DateTime(1904, 0, 1, 0, 0, 0, DateTimeKind.Utc);


            long secondsSince1904 = (long)(((calendar - cal).TotalMilliseconds) / 1000D);
            output.Write(secondsSince1904);
        }

        private long ToUInt32(int high, int low)
        {
            return (high & 0xffffL) << 16 | low & 0xffffL;
        }

        private long ToUInt32(byte[] bytes)
        {
            return (bytes[0] & 0xffL) << 24
                    | (bytes[1] & 0xffL) << 16
                    | (bytes[2] & 0xffL) << 8
                    | bytes[3] & 0xffL;
        }

        private int log2(int num)
        {
            return (int)Math.Round(Math.Log(num) / Math.Log(2));
        }

        public void AddGlyphIds(ISet<int> allGlyphIds)
        {
            glyphIds.AddAll(allGlyphIds);
        }
    }
    public static class IntExtension
    {
        public static uint HighestOneBit(this uint i)
        {
            i |= (i >> 1);
            i |= (i >> 2);
            i |= (i >> 4);
            i |= (i >> 8);
            i |= (i >> 16);
            return i - (i >> 1);
        }
    }
}
