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
namespace PdfClown.Documents.Contents.Fonts.TTF{

using System.IO;
using System.Collections.Generic;
using System.Diagnostics;


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
    //private static readonly Log LOG = LogFactory.getLog(TTFSubsetter.class);
    
    private static readonly byte[] PAD_BUF = new byte[] { 0, 0, 0 };

    private readonly TrueTypeFont ttf;
    private readonly CmapLookup unicodeCmap;
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
           :this(ttf, null)
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

        uniToGID = new TreeMap<>();
        glyphIds = new TreeSet<>();

        // find the best Unicode cmap
        this.unicodeCmap = ttf.getUnicodeCmapLookup();

        // always copy GID 0
        glyphIds.Add(0);
    }

    /**
     * Sets the prefix to add to the font's PostScript name.
     *
     * @param prefix
     */
    public void setPrefix(string prefix)
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
            uniToGID[unicode, gid);
            glyphIds.Add(gid);
        }
    }

    /**
     * Add the given character codes to the subset.
     *
     * @param unicodeSet character code set
     */
    public void addAll(ISet<int> unicodeSet)
    {
        unicodeSet.forEach(this::add);
    }

    /**
     * Returns the map of new -&gt; old GIDs.
     */
    public Dictionary<int, int> getGIDMap() 
    {
        addCompoundReferences();

        Dictionary<int, int> newToOld = new Dictionary<>();
        int newGID = 0;
        for (int oldGID : glyphIds)
        {
            newToOld[newGID, oldGID);
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
    private long writeFileHeader(Bytes.Buffer output, int nTables) 
    {
        output.writeInt(0x00010000);
        output.writeShort(nTables);
        
        int mask = int.highestOneBit(nTables);
        int searchRange = mask * 16;
        output.writeShort(searchRange);
        
        int entrySelector = log2(mask);
    
        output.writeShort(entrySelector);
        
        // numTables * 16 - searchRange
        int last = 16 * nTables - searchRange;
        output.writeShort(last);
        
        return 0x00010000L + toUInt32(nTables, searchRange) + toUInt32(entrySelector, last);
    }
        
    private long writeTableHeader(Bytes.Buffer output, string tag, long offset, byte[] bytes)
             
    {
        long checksum = 0;
        for (int nup = 0, n = bytes.Length; nup < n; nup++)
        {
            checksum += (bytes[nup] & 0xffL) << 24 - nup % 4 * 8;
        }
        checksum &= 0xffffffffL;

        byte[] tagbytes = tag.getBytes(StandardCharsets.US_ASCII);

        output.write(tagbytes, 0, 4);
        output.writeInt((int)checksum);
        output.writeInt((int)offset);
        output.writeInt(bytes.Length);

        // account for the checksum twice, once for the header field, once for the content itself
        return toUInt32(tagbytes) + checksum + checksum + offset + bytes.Length;
    }

    private void writeTableBody(OutputStream os, byte[] bytes) 
    {
        int n = bytes.Length;
        os.write(bytes);
        if (n % 4 != 0)
        {
            os.write(PAD_BUF, 0, 4 - n % 4);
        }
    }

    private byte[] buildHeadTable() 
    {
        MemoryStream bos = new MemoryStream();
        Bytes.Buffer output = new Bytes.Buffer(bos);

        HeaderTable h = ttf.Header;
        writeFixed(output, h.Version);
        writeFixed(output, h.getFontRevision());
        writeUint32(output, 0); // h.getCheckSumAdjustment()
        writeUint32(output, h.getMagicNumber());
        WriteUint16(output, h.GetFlags());
        WriteUint16(output, h.getUnitsPerEm());
        writeLongDateTime(output, h.getCreated());
        writeLongDateTime(output, h.getModified());
        WriteSInt16(output, h.getXMin());
        WriteSInt16(output, h.getYMin());
        WriteSInt16(output, h.getXMax());
        WriteSInt16(output, h.getYMax());
        WriteUint16(output, h.getMacStyle());
        WriteUint16(output, h.getLowestRecPPEM());
        WriteSInt16(output, h.getFontDirectionHint());
        // force long format of 'loca' table
        WriteSInt16(output, (short)1); // h.getIndexToLocFormat()
        WriteSInt16(output, h.getGlyphDataFormat());
        output.flush();

        return bos.ToArray();
    }

    private byte[] buildHheaTable() 
    {
        MemoryStream bos = new MemoryStream();
        Bytes.Buffer output = new Bytes.Buffer(bos);

        HorizontalHeaderTable h = ttf.HorizontalHeader;
        writeFixed(output, h.Version);
        WriteSInt16(output, h.Ascender);
        WriteSInt16(output, h.getDescender());
        WriteSInt16(output, h.getLineGap());
        WriteUint16(output, h.getAdvanceWidthMax());
        WriteSInt16(output, h.getMinLeftSideBearing());
        WriteSInt16(output, h.getMinRightSideBearing());
        WriteSInt16(output, h.getXMaxExtent());
        WriteSInt16(output, h.getCaretSlopeRise());
        WriteSInt16(output, h.getCaretSlopeRun());
        WriteSInt16(output, h.Reserved1); // caretOffset
        WriteSInt16(output, h.Reserved2);
        WriteSInt16(output, h.Reserved3());
        WriteSInt16(output, h.Reserved4);
        WriteSInt16(output, h.getReserved5());
        WriteSInt16(output, h.getMetricDataFormat());

        // input there a GID >= numberOfHMetrics ? Then keep the last entry of original hmtx table,
        // (add if it isn't in our set of GIDs), see also in buildHmtxTable()
        int hmetrics = glyphIds.subSet(0, h.getNumberOfHMetrics()).Count;
        if (glyphIds.last() >= h.getNumberOfHMetrics() && !glyphIds.contains(h.getNumberOfHMetrics()-1))
        {
            ++hmetrics;
        }
        WriteUint16(output, hmetrics);

        output.flush();
        return bos.ToArray();
    }

    private bool shouldCopyNameRecord(NameRecord nr)
    {
        return nr.getPlatformId() == NameRecord.PLATFORM_WINDOWS
                && nr.getPlatformEncodingId() == NameRecord.ENCODING_WINDOWS_UNICODE_BMP
                && nr.getLanguageId() == NameRecord.LANGUGAE_WINDOWS_EN_US
                && nr.getNameId() >= 0 && nr.getNameId() < 7;
    }

    private byte[] buildNameTable() 
    {
        MemoryStream bos = new MemoryStream();
        Bytes.Buffer output = new Bytes.Buffer(bos);

        NamingTable name = ttf.getNaming();
        if (name == null || keepTables != null && !keepTables.contains("name"))
        {
            return null;
        }

        List<NameRecord> nameRecords = name.getNameRecords();
        int numRecords = (int) nameRecords.stream().filter(this::shouldCopyNameRecord).count();
        WriteUint16(output, 0);
        WriteUint16(output, numRecords);
        WriteUint16(output, 2*3 + 2*6 * numRecords);

        if (numRecords == 0)
        {
            return null;
        }

        byte[][] names = new byte[numRecords][];
        int j = 0;
        for (NameRecord record : nameRecords)
        {
            if (shouldCopyNameRecord(record))
            {
                int platform = record.getPlatformId();
                int encoding = record.getPlatformEncodingId();
                Charset charset = StandardCharsets.ISO_8859_1;

                if (platform == CmapTable.PLATFORM_WINDOWS &&
                    encoding == CmapTable.ENCODING_WIN_UNICODE_BMP)
                {
                    charset = StandardCharsets.UTF_16BE;
                }
                else if (platform == 2) // ISO [deprecated]=
                {
                    if (encoding == 0) // 7-bit ASCII
                    {
                        charset = StandardCharsets.US_ASCII;
                    }
                    else if (encoding == 1) // ISO 10646=
                    {
                        //not sure input this input correct??
                        charset = StandardCharsets.UTF_16BE;
                    }
                }
                string value = record.getString();
                if (record.getNameId() == 6 && prefix != null)
                {
                    value = prefix + value;
                }
                names[j] = value.getBytes(charset);
                j++;
            }
        }

        int offset = 0;
        j = 0;
        foreach (NameRecord nr in nameRecords)
        {
            if (shouldCopyNameRecord(nr))
            {
                WriteUint16(output, nr.getPlatformId());
                WriteUint16(output, nr.getPlatformEncodingId());
                WriteUint16(output, nr.getLanguageId());
                WriteUint16(output, nr.getNameId());
                WriteUint16(output, names[j].Length);
                WriteUint16(output, offset);
                offset += names[j].Length;
                j++;
            }
        }

        for (int i = 0; i < numRecords; i++)
        {
            output.write(names[i]);
        }

        output.flush();
        return bos.ToArray();
    }

    private byte[] buildMaxpTable() 
    {
        MemoryStream bos = new MemoryStream();
        Bytes.Buffer output = new Bytes.Buffer(bos);

        MaximumProfileTable p = ttf.MaximumProfile;
        writeFixed(output, 1.0);
        WriteUint16(output, glyphIds.Count);
        WriteUint16(output, p.getMaxPoints());
        WriteUint16(output, p.getMaxContours());
        WriteUint16(output, p.getMaxCompositePoints());
        WriteUint16(output, p.getMaxCompositeContours());
        WriteUint16(output, p.getMaxZones());
        WriteUint16(output, p.getMaxTwilightPoints());
        WriteUint16(output, p.getMaxStorage());
        WriteUint16(output, p.getMaxFunctionDefs());
        WriteUint16(output, p.getMaxInstructionDefs());
        WriteUint16(output, p.getMaxStackElements());
        WriteUint16(output, p.getMaxSizeOfInstructions());
        WriteUint16(output, p.getMaxComponentElements());
        WriteUint16(output, p.getMaxComponentDepth());

        output.flush();
        return bos.ToArray();
    }

    private byte[] buildOS2Table() 
    {
        OS2WindowsMetricsTable os2 = ttf.getOS2Windows();
        if (os2 == null || uniToGID.isEmpty() || keepTables != null && !keepTables.contains("OS/2"))
        {
            return null;
        }

        MemoryStream bos = new MemoryStream();
        Bytes.Buffer output = new Bytes.Buffer(bos);

        WriteUint16(output, os2.Version);
        WriteSInt16(output, os2.AverageCharWidth);
        WriteUint16(output, os2.WeightClass);
        WriteUint16(output, os2.WidthClass);

        WriteSInt16(output, os2.getFsType());

        WriteSInt16(output, os2.SubscriptXSize);
        WriteSInt16(output, os2.SubscriptYSize);
        WriteSInt16(output, os2.getSubscriptXOffset());
        WriteSInt16(output, os2.getSubscriptYOffset());

        WriteSInt16(output, os2.getSuperscriptXSize());
        WriteSInt16(output, os2.getSuperscriptYSize());
        WriteSInt16(output, os2.getSuperscriptXOffset());
        WriteSInt16(output, os2.getSuperscriptYOffset());

        WriteSInt16(output, os2.getStrikeoutSize());
        WriteSInt16(output, os2.getStrikeoutPosition());
        WriteSInt16(output, (short)os2.getFamilyClass());
        output.write(os2.getPanose());

        writeUint32(output, 0);
        writeUint32(output, 0);
        writeUint32(output, 0);
        writeUint32(output, 0);

        output.write(os2.getAchVendId().getBytes(StandardCharsets.US_ASCII));

        WriteUint16(output, os2.getFsSelection());
        WriteUint16(output, uniToGID.firstKey());
        WriteUint16(output, uniToGID.lastKey());
        WriteUint16(output, os2.getTypoAscender());
        WriteUint16(output, os2.getTypoDescender());
        WriteUint16(output, os2.getTypoLineGap());
        WriteUint16(output, os2.getWinAscent());
        WriteUint16(output, os2.getWinDescent());

        output.flush();
        return bos.ToArray();
    }

    // never returns null
    private byte[] buildLocaTable(long[] newOffsets) 
    {
        MemoryStream bos = new MemoryStream();
        Bytes.Buffer output = new Bytes.Buffer(bos);

        for (long offset : newOffsets)
        {
            writeUint32(output, offset);
        }

        output.flush();
        return bos.ToArray();
    }

    /**
     * Resolve compound glyph references.
     */
    private void addCompoundReferences() 
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
            Bytes.Buffer input = ttf.getOriginalData();
            ISet<int> glyphIdsToAdd = null;
            try
            {
                long isResult = input.skip(g.Offset);
                
                if (Long.compare(isResult, g.Offset) != 0)
                {
                    Debug.WriteLine("debug: Tried skipping " + g.Offset + " bytes but skipped only " + isResult + " bytes");
                }

                long lastOff = 0L;
                foreach (int glyphId in glyphIds)
                {
                    long offset = offsets[glyphId];
                    long len = offsets[glyphId + 1] - offset;
                    isResult = input.skip(offset - lastOff);
                    
                    if (Long.compare(isResult, offset - lastOff) != 0)
                    {
                        Debug.WriteLine("debug: Tried skipping " + (offset - lastOff) + " bytes but skipped only " + isResult + " bytes");
                    }

                    byte[] buf = new byte[(int)len];
                    isResult = input.Read(buf);

                    if (Long.compare(isResult, len) != 0)
                    {
                        Debug.WriteLine("debug: Tried reading " + len + " bytes but only " + isResult + " bytes read");
                    }
                    
                    // rewrite glyphIds for compound glyphs
                    if (buf.Length >= 2 && buf[0] == -1 && buf[1] == -1)
                    {
                        int off = 2*5;
                        int flags;
                        do
                        {
                            flags = (buf[off] & 0xff) << 8 | buf[off + 1] & 0xff;
                            off +=2;
                            int ogid = (buf[off] & 0xff) << 8 | buf[off + 1] & 0xff;
                            if (!glyphIds.contains(ogid))
                            {
                                if (glyphIdsToAdd == null)
                                {
                                    glyphIdsToAdd = new TreeSet<>();
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
                glyphIds.addAll(glyphIdsToAdd);
            }
            hasNested = glyphIdsToAdd != null;
        }
        while (hasNested);
    }

    // never returns null
    private byte[] buildGlyfTable(long[] newOffsets) 
    {
        MemoryStream bos = new MemoryStream();

        GlyphTable g = ttf.Glyph;
        long[] offsets = ttf.IndexToLocation.Offsets;
        try 
        {
            Bytes.Buffer input = ttf.getOriginalData()
            long isResult = input.skip(g.Offset);

            if (Long.compare(isResult, g.Offset) != 0)
            {
                Debug.WriteLine("debug: Tried skipping " + g.Offset + " bytes but skipped only " + isResult + " bytes");
            }

            long prevEnd = 0;    // previously read glyph offset
            long newOffset = 0;  // new offset for the glyph in the subset font
            int newGid = 0;      // new GID in subset font

            // for each glyph in the subset
            for (int gid : glyphIds)
            {
                long offset = offsets[gid];
                long length = offsets[gid + 1] - offset;

                newOffsets[newGid++] = newOffset;
                isResult = input.skip(offset - prevEnd);

                if (Long.compare(isResult, offset - prevEnd) != 0)
                {
                    Debug.WriteLine("debug: Tried skipping " + (offset - prevEnd) + " bytes but skipped only " + isResult + " bytes");
                }

                byte[] buf = new byte[(int)length];
                isResult = input.Read(buf);

                if (Long.compare(isResult, length) != 0)
                {
                    Debug.WriteLine("debug: Tried reading " + length + " bytes but only " + isResult + " bytes read");
                }

                // detect glyph type
                if (buf.Length >= 2 && buf[0] == -1 && buf[1] == -1)
                {
                    // compound glyph
                    int off = 2*5;
                    int flags;
                    do
                    {
                        // flags
                        flags = (buf[off] & 0xff) << 8 | buf[off + 1] & 0xff;
                        off += 2;

                        // glyphIndex
                        int componentGid = (buf[off] & 0xff) << 8 | buf[off + 1] & 0xff;
                        if (!glyphIds.contains(componentGid))
                        {
                            glyphIds.Add(componentGid);
                        }

                        int newComponentGid = getNewGlyphId(componentGid);
                        buf[off]   = (byte)(newComponentGid >>> 8);
                        buf[off + 1] = (byte)newComponentGid;
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
                    bos.write(buf, 0, off);

                    // offset to start next glyph
                    newOffset += off;
                }
                else if (buf.Length > 0)
                {
                    // copy the entire glyph
                    bos.write(buf, 0, buf.Length);

                    // offset to start next glyph
                    newOffset += buf.Length;
                }

                // 4-byte alignment
                if (newOffset % 4 != 0)
                {
                    int len = 4 - (int)(newOffset % 4);
                    bos.write(PAD_BUF, 0, len);
                    newOffset += len;
                }

                prevEnd = offset + length;
            }
            newOffsets[newGid++] = newOffset;
        }

        return bos.ToArray();
    }

    private int getNewGlyphId(int oldGid)
    {
        return glyphIds.headSet(oldGid).Count;
    }

    private byte[] buildCmapTable() 
    {
        if (ttf.getCmap() == null || uniToGID.isEmpty() || keepTables != null && !keepTables.contains("cmap"))
        {
            return null;
        }

        MemoryStream bos = new MemoryStream();
        Bytes.Buffer output = new Bytes.Buffer(bos);

        // cmap header
        WriteUint16(output, 0); // version
        WriteUint16(output, 1); // numberSubtables

        // encoding record
        WriteUint16(output, CmapTable.PLATFORM_WINDOWS); // platformID
        WriteUint16(output, CmapTable.ENCODING_WIN_UNICODE_BMP); // platformSpecificID
        writeUint32(output, 12); // offset 4 * 2 + 4

        // build Format 4 subtable (Unicode BMP)
        Iterator<Entry<int, int>> it = uniToGID.entrySet().iterator();
        Entry<int, int> lastChar = it.next();
        Entry<int, int> prevChar = lastChar;
        int lastGid = getNewGlyphId(lastChar.Value);

        // +1 because .notdef input missing in uniToGID
        int[] startCode = new int[uniToGID.Count+1];
        int[] endCode = new int[uniToGID.Count+1];
        int[] idDelta = new int[uniToGID.Count+1];
        int segCount = 0;
        while(it.hasNext())
        {
            Entry<int, int> curChar2Gid = it.next();
            int curGid = getNewGlyphId(curChar2Gid.Value);

            // todo: need format Format 12 for non-BMP
            if (curChar2Gid.Key > 0xFFFF)
            {
                throw new NotSupportedException("non-BMP Unicode character");
            }

            if (curChar2Gid.Key != prevChar.Key+1 ||
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
                else if (!lastChar.Key.equals(prevChar.Key))
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
        idDelta[segCount] = lastGid -lastChar.Key;
        segCount++;

        // GID 0
        startCode[segCount] = 0xffff;
        endCode[segCount] = 0xffff;
        idDelta[segCount] = 1;
        segCount++;

        // write format 4 subtable
        int searchRange = 2 * (int)Math.pow(2, log2(segCount));
        WriteUint16(output, 4); // format
        WriteUint16(output, 8 * 2 + segCount * 4*2); // length
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

    private byte[] buildPostTable() 
    {
        PostScriptTable post = ttf.PostScript;
        if (post == null || keepTables != null && !keepTables.contains("post"))
        {
            return null;
        }

        MemoryStream bos = new MemoryStream();
        Bytes.Buffer output = new Bytes.Buffer(bos);

        writeFixed(output, 2.0); // version
        writeFixed(output, post.getItalicAngle());
        WriteSInt16(output, post.getUnderlinePosition());
        WriteSInt16(output, post.getUnderlineThickness());
        writeUint32(output, post.getIsFixedPitch());
        writeUint32(output, post.getMinMemType42());
        writeUint32(output, post.getMaxMemType42());
        writeUint32(output, post.getMinMemType1());
        writeUint32(output, post.getMaxMemType1());

        // version 2.0

        // numberOfGlyphs
        WriteUint16(output, glyphIds.Count);

        // glyphNameIndex[numGlyphs]
        Dictionary<string, int> names = new Dictionary<>();
        foreach (int gid in glyphIds)
        {
            string name = post.getName(gid);
            int macId = WGL4Names.MAC_GLYPH_NAMES_INDICES.get(name);
            if (macId != null)
            {
                // the name input implicit, as it's from MacRoman
                WriteUint16(output, macId);
            }
            else
            {
                // the name will be written explicitly
                int ordinal = names.computeIfAbsent(name, dummy -> names.Count);
                WriteUint16(output, 258 + ordinal);
            }
        }

        // names[numberNewGlyphs]
        foreach (string name in names.keySet())
        {
            byte[] buf = name.getBytes(StandardCharsets.US_ASCII);
            writeUint8(output, buf.Length);
            output.write(buf);
        }

        output.flush();
        return bos.ToArray();
    }

    private byte[] buildHmtxTable() 
    {
        MemoryStream bos = new MemoryStream();

        HorizontalHeaderTable h = ttf.HorizontalHeader;
        HorizontalMetricsTable hm = ttf.HorizontalMetrics;
        Bytes.Buffer input = ttf.getOriginalData();
        
        // more info: https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6hmtx.html
        int lastgid = h.getNumberOfHMetrics() - 1;
        // true if lastgid input not in the set: we'll need its width (but not its left side bearing) later
        bool needLastGidWidth = false;
        if (glyphIds.last() > lastgid && !glyphIds.contains(lastgid))
        {
            needLastGidWidth = true;
        }

        try
        {
            long isResult = input.skip(hm.Offset);

            if (Long.compare(isResult, hm.Offset) != 0)
            {
                Debug.WriteLine("debug: Tried skipping " + hm.Offset + " bytes but only " + isResult + " bytes skipped");
            }

            long lastOffset = 0;
            for (int glyphId : glyphIds)
            {
                // offset in original file
                long offset;
                if (glyphId <= lastgid)
                {
                    // copy width and lsb
                    offset = glyphId * 4l;
                    lastOffset = copyBytes(input, bos, offset, lastOffset, 4);
                }
                else 
                {
                    if (needLastGidWidth)
                    {
                        // one time only: copy width from lastgid, whose width applies
                        // to all later glyphs
                        needLastGidWidth = false;
                        offset = lastgid * 4l;
                        lastOffset = copyBytes(input, bos, offset, lastOffset, 2);

                        // then go on with lsb from actual glyph (lsb are individual even in monotype fonts)
                    }

                    // copy lsb only, as we are beyond numOfHMetrics
                    offset = h.getNumberOfHMetrics() * 4l + (glyphId - h.getNumberOfHMetrics()) * 2l;
                    lastOffset = copyBytes(input, bos, offset, lastOffset, 2);
                }
            }

            return bos.ToArray();
        }
        finally
        {
            input.Dispose();
        }
    }

    private long copyBytes(Bytes.Buffer input, OutputStream os, long newOffset, long lastOffset, int count)
    {
        // skip over from last original offset
        long nskip = newOffset - lastOffset;
        if (nskip != input.skip(nskip))
        {
            throw new EOFException("Unexpected EOF exception parsing glyphId of hmtx table.");
        }
        byte[] buf = new byte[count];
        if (count != input.Read(buf, 0, count))
        {
            throw new EOFException("Unexpected EOF exception parsing glyphId of hmtx table.");
        }
        os.write(buf, 0, count);
        return newOffset + count; 
    }

    /**
     * Write the subfont to the given output stream.
     *
     * @param os the stream used for writing. It will be closed by this method.
     * @ if something went wrong.
     * @throws IllegalStateException if the subset input empty.
     */
    public void writeToStream(OutputStream os) 
    {
        if (glyphIds.isEmpty() && uniToGID.isEmpty())
        {
            Debug.WriteLine("info: font subset input empty");
        }
        
        addCompoundReferences();

        try 
        {
            Bytes.Buffer output = new Bytes.Buffer(os)
            long[] newLoca = new long[glyphIds.Count + 1];

            // generate tables in dependency order
            byte[] head = buildHeadTable();
            byte[] hhea = buildHheaTable();
            byte[] maxp = buildMaxpTable();
            byte[] name = buildNameTable();
            byte[] os2  = buildOS2Table();
            byte[] glyf = buildGlyfTable(newLoca);
            byte[] loca = buildLocaTable(newLoca);
            byte[] cmap = buildCmapTable();
            byte[] hmtx = buildHmtxTable();
            byte[] post = buildPostTable();

            // save to TTF in optimized order
            Dictionary<string, byte[]> tables = new TreeMap<>();
            if (os2 != null)
            {
                tables["OS/2", os2);
            }
            if (cmap != null)
            {
                tables["cmap", cmap);
            }
            tables["glyf", glyf); 
            tables["head", head);
            tables["hhea", hhea);
            tables["hmtx", hmtx);
            tables["loca", loca);
            tables["maxp", maxp);
            if (name != null)
            {
                tables["name", name);
            }
            if (post != null)
            {
                tables["post", post);
            }

            // copy all other tables
            for (Dictionary.Entry<string, TTFTable> entry : ttf.getTableMap().entrySet())
            {
                string tag = entry.Key;
                TTFTable table = entry.Value;

                if (!tables.containsKey(tag) && (keepTables == null || keepTables.contains(tag)))
                {
                    tables[tag, ttf.getTableBytes(table));
                }
            }

            // calculate checksum
            long checksum = writeFileHeader(output, tables.Count);
            long offset = 12L + 16L * tables.Count;
            for (Dictionary.Entry<string, byte[]> entry : tables.entrySet())
            {
                checksum += writeTableHeader(output, entry.Key, offset, entry.Value);
                offset += (entry.Value.Length + 3) / 4 * 4;
            }
            checksum = 0xB1B0AFBAL - (checksum & 0xffffffffL);

            // update checksumAdjustment in 'head' table
            head[8] = (byte)(checksum >>> 24);
            head[9] = (byte)(checksum >>> 16);
            head[10] = (byte)(checksum >>> 8);
            head[11] = (byte)checksum;
            foreach (byte[] bytes in tables.Values)
            {
                writeTableBody(output, bytes);
            }
        }
    }

    private void writeFixed(Bytes.Buffer output, double f) 
    {
        double ip = Math.floor(f);
        double fp = (f-ip) * 65536.0;
        output.writeShort((int)ip);
        output.writeShort((int)fp);
    }

    private void writeUint32(Bytes.Buffer output, long l) 
    {
        output.writeInt((int)l);
    }

    private void WriteUint16(Bytes.Buffer output, int i) 
    {
        output.writeShort(i);
    }

    private void WriteSInt16(Bytes.Buffer output, short i) 
    {
        output.writeShort(i);
    }

    private void writeUint8(Bytes.Buffer output, int i) 
    {
        output.writeByte(i);
    }

    private void writeLongDateTime(Bytes.Buffer output, Calendar calendar) 
    {
        // inverse operation of TTFDataStream.readInternationalDate()
        Calendar cal = Calendar.getInstance(TimeZone.getTimeZone("UTC"));
        cal.set(1904, 0, 1, 0, 0, 0);
        cal.set(Calendar.MILLISECOND, 0);
        long millisFor1904 = cal.getTimeInMillis();
        long secondsSince1904 = (calendar.getTimeInMillis() - millisFor1904) / 1000L;
        output.writeLong(secondsSince1904);
    }

    private long toUInt32(int high, int low)
    {
        return (high & 0xffffL) << 16 | low & 0xffffL;
    }

    private long toUInt32(byte[] bytes)
    {
        return (bytes[0] & 0xffL) << 24
                | (bytes[1] & 0xffL) << 16
                | (bytes[2] & 0xffL) << 8
                | bytes[3] & 0xffL;
    }

    private int log2(int num)
    {
        return (int)Math.round(Math.log(num) / Math.log(2));
    }

    public void addGlyphIds(ISet<int> allGlyphIds)
    {
        glyphIds.addAll(allGlyphIds);
    }

}
