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
using System.Collections.Generic;
using System.Diagnostics;
using System;
using PdfClown.Documents.Contents.Fonts.Type1;

namespace PdfClown.Documents.Contents.Fonts.TTF
{

    /**
     * A "cmap" subtable.
     * 
     * @author Ben Litchfield
     */
    public class CmapSubtable : ICmapLookup
    {
        //private static readonly Log LOG = LogFactory.getLog(CmapSubtable.class);

        private static readonly long LEAD_OFFSET = 0xD800L - (0x10000 >> 10);
        private static readonly long SURROGATE_OFFSET = 0x10000L - (0xD800 << 10) - 0xDC00;

        private int platformId;
        private int platformEncodingId;
        private long subTableOffset;
        private int[] glyphIdToCharacterCode;
        private readonly Dictionary<int, List<int>> glyphIdToCharacterCodeMultiple = new Dictionary<int, List<int>>();
        private Dictionary<int, int> characterCodeToGlyphId = new Dictionary<int, int>();

        /**
         * @return Returns the platformEncodingId.
         */
        public int PlatformEncodingId
        {
            get => platformEncodingId;
            set => platformEncodingId = value;
        }

        /**
         * @return Returns the platformId.
         */
        public int PlatformId
        {
            get => platformId;
            set => platformId = value;
        }

        /**
         * This will read the required data from the stream.
         * 
         * @param data The stream to read the data from.
         * @ If there is an error reading the data.
         */
        public void InitData(TTFDataStream data)
        {
            platformId = data.ReadUnsignedShort();
            platformEncodingId = data.ReadUnsignedShort();
            subTableOffset = data.ReadUnsignedInt();
        }

        /**
         * This will read the required data from the stream.
         * 
         * @param cmap the CMAP this encoding belongs to.
         * @param numGlyphs number of glyphs.
         * @param data The stream to read the data from.
         * @ If there is an error reading the data.
         */
        public void InitSubtable(CmapTable cmap, int numGlyphs, TTFDataStream data)
        {
            data.Seek(cmap.Offset + subTableOffset);
            int subtableFormat = data.ReadUnsignedShort();
            long length;
            long version;
            if (subtableFormat < 8)
            {
                length = data.ReadUnsignedShort();
                version = data.ReadUnsignedShort();
            }
            else
            {
                // read an other UnsignedShort to read a Fixed32
                data.ReadUnsignedShort();
                length = data.ReadUnsignedInt();
                version = data.ReadUnsignedInt();
            }

            switch (subtableFormat)
            {
                case 0:
                    ProcessSubtype0(data);
                    break;
                case 2:
                    ProcessSubtype2(data, numGlyphs);
                    break;
                case 4:
                    ProcessSubtype4(data, numGlyphs);
                    break;
                case 6:
                    ProcessSubtype6(data, numGlyphs);
                    break;
                case 8:
                    ProcessSubtype8(data, numGlyphs);
                    break;
                case 10:
                    ProcessSubtype10(data, numGlyphs);
                    break;
                case 12:
                    ProcessSubtype12(data, numGlyphs);
                    break;
                case 13:
                    ProcessSubtype13(data, numGlyphs);
                    break;
                case 14:
                    ProcessSubtype14(data, numGlyphs);
                    break;
                default:
                    throw new IOException("Unknown cmap format:" + subtableFormat);
            }
        }

        /**
         * Reads a format 8 subtable.
         * 
         * @param data the data stream of the to be parsed ttf font
         * @param numGlyphs number of glyphs to be read
         * @ If there is an error parsing the true type font.
         */
        void ProcessSubtype8(TTFDataStream data, int numGlyphs)
        {
            // --- is32 is a 65536 BITS array ( = 8192 BYTES)
            byte[] is32 = data.ReadUnsignedByteArray(8192);
            long nbGroups = data.ReadUnsignedInt();

            // --- nbGroups shouldn't be greater than 65536
            if (nbGroups > 65536)
            {
                throw new IOException("CMap ( Subtype8 ) is invalid");
            }

            glyphIdToCharacterCode = NewGlyphIdToCharacterCode(numGlyphs);
            characterCodeToGlyphId = new Dictionary<int, int>(numGlyphs);
            if (numGlyphs == 0)
            {
                Debug.WriteLine("warn: subtable has no glyphs");
                return;
            }
            // -- Read all sub header
            for (long i = 0; i < nbGroups; ++i)
            {
                long firstCode = data.ReadUnsignedInt();
                long endCode = data.ReadUnsignedInt();
                long startGlyph = data.ReadUnsignedInt();

                // -- process simple validation
                if (firstCode > endCode || 0 > firstCode)
                {
                    throw new IOException("Range invalid");
                }

                for (long j = firstCode; j <= endCode; ++j)
                {
                    // -- Convert the Character code in decimal
                    if (j > int.MaxValue)
                    {
                        throw new IOException("[Sub Format 8] Invalid character code " + j);
                    }
                    if ((int)j / 8 >= is32.Length)
                    {
                        throw new IOException("[Sub Format 8] Invalid character code " + j);
                    }

                    int currentCharCode;
                    if ((is32[(int)j / 8] & (1 << ((int)j % 8))) == 0)
                    {
                        currentCharCode = (int)j;
                    }
                    else
                    {
                        // the character code uses a 32bits format
                        // convert it in decimal : see http://www.unicode.org/faq//utf_bom.html#utf16-4
                        long lead = LEAD_OFFSET + (j >> 10);
                        long trail = 0xDC00 + (j & 0x3FF);

                        long codepoint = (lead << 10) + trail + SURROGATE_OFFSET;
                        if (codepoint > int.MaxValue)
                        {
                            throw new IOException("[Sub Format 8] Invalid character code " + codepoint);
                        }
                        currentCharCode = (int)codepoint;
                    }

                    long glyphIndex = startGlyph + (j - firstCode);
                    if (glyphIndex > numGlyphs || glyphIndex > int.MaxValue)
                    {
                        throw new IOException("CMap contains an invalid glyph index");
                    }

                    glyphIdToCharacterCode[(int)glyphIndex] = currentCharCode;
                    characterCodeToGlyphId[currentCharCode] = (int)glyphIndex;
                }
            }
        }

        /**
         * Reads a format 10 subtable.
         * 
         * @param data the data stream of the to be parsed ttf font
         * @param numGlyphs number of glyphs to be read
         * @ If there is an error parsing the true type font.
         */
        void ProcessSubtype10(TTFDataStream data, int numGlyphs)
        {
            long startCode = data.ReadUnsignedInt();
            long numChars = data.ReadUnsignedInt();
            if (numChars > int.MaxValue)
            {
                throw new IOException("Invalid number of Characters");
            }

            if (startCode < 0 || startCode > 0x0010FFFF || (startCode + numChars) > 0x0010FFFF
                    || ((startCode + numChars) >= 0x0000D800 && (startCode + numChars) <= 0x0000DFFF))
            {
                throw new IOException("Invalid Characters codes");

            }
        }

        /**
         * Reads a format 12 subtable.
         * 
         * @param data the data stream of the to be parsed ttf font
         * @param numGlyphs number of glyphs to be read
         * @ If there is an error parsing the true type font.
         */
        void ProcessSubtype12(TTFDataStream data, int numGlyphs)
        {
            long nbGroups = data.ReadUnsignedInt();
            glyphIdToCharacterCode = NewGlyphIdToCharacterCode(numGlyphs);
            characterCodeToGlyphId = new Dictionary<int, int>(numGlyphs);
            if (numGlyphs == 0)
            {
                Debug.WriteLine("warn: subtable has no glyphs");
                return;
            }
            for (long i = 0; i < nbGroups; ++i)
            {
                long firstCode = data.ReadUnsignedInt();
                long endCode = data.ReadUnsignedInt();
                long startGlyph = data.ReadUnsignedInt();

                if (firstCode < 0 || firstCode > 0x0010FFFF ||
                    firstCode >= 0x0000D800 && firstCode <= 0x0000DFFF)
                {
                    throw new IOException("Invalid characters codes");
                }

                if (endCode > 0 && endCode < firstCode ||
                    endCode > 0x0010FFFF ||
                    endCode >= 0x0000D800 && endCode <= 0x0000DFFF)
                {
                    throw new IOException("Invalid characters codes");
                }

                for (long j = 0; j <= endCode - firstCode; ++j)
                {
                    long glyphIndex = startGlyph + j;
                    if (glyphIndex >= numGlyphs)
                    {
                        Debug.WriteLine("warn: Format 12 cmap contains an invalid glyph index");
                        break;
                    }

                    if (firstCode + j > 0x10FFFF)
                    {
                        Debug.WriteLine("warn: Format 12 cmap contains character beyond UCS-4");
                    }

                    glyphIdToCharacterCode[(int)glyphIndex] = (int)(firstCode + j);
                    characterCodeToGlyphId[(int)(firstCode + j)] = (int)glyphIndex;
                }
            }
        }

        /**
         * Reads a format 13 subtable.
         * 
         * @param data the data stream of the to be parsed ttf font
         * @param numGlyphs number of glyphs to be read
         * @ If there is an error parsing the true type font.
         */
        void ProcessSubtype13(TTFDataStream data, int numGlyphs)
        {
            long nbGroups = data.ReadUnsignedInt();
            glyphIdToCharacterCode = NewGlyphIdToCharacterCode(numGlyphs);
            characterCodeToGlyphId = new Dictionary<int, int>(numGlyphs);
            if (numGlyphs == 0)
            {
                Debug.WriteLine("warn: subtable has no glyphs");
                return;
            }
            for (long i = 0; i < nbGroups; ++i)
            {
                long firstCode = data.ReadUnsignedInt();
                long endCode = data.ReadUnsignedInt();
                long glyphId = data.ReadUnsignedInt();

                if (glyphId > numGlyphs)
                {
                    Debug.WriteLine("warn: Format 13 cmap contains an invalid glyph index");
                    break;
                }

                if (firstCode < 0 || firstCode > 0x0010FFFF || (firstCode >= 0x0000D800 && firstCode <= 0x0000DFFF))
                {
                    throw new IOException("Invalid Characters codes");
                }

                if ((endCode > 0 && endCode < firstCode) || endCode > 0x0010FFFF
                        || (endCode >= 0x0000D800 && endCode <= 0x0000DFFF))
                {
                    throw new IOException("Invalid Characters codes");
                }

                for (long j = 0; j <= endCode - firstCode; ++j)
                {
                    if (firstCode + j > int.MaxValue)
                    {
                        throw new IOException("Character Code greater than int.MAX_VALUE");
                    }

                    if (firstCode + j > 0x10FFFF)
                    {
                        Debug.WriteLine("warn: Format 13 cmap contains character beyond UCS-4");
                    }

                    glyphIdToCharacterCode[(int)glyphId] = (int)(firstCode + j);
                    characterCodeToGlyphId[(int)(firstCode + j)] = (int)glyphId;
                }
            }
        }

        /**
         * Reads a format 14 subtable.
         * 
         * @param data the data stream of the to be parsed ttf font
         * @param numGlyphs number of glyphs to be read
         * @ If there is an error parsing the true type font.
         */
        void ProcessSubtype14(TTFDataStream data, int numGlyphs)
        {
            // Unicode Variation Sequences (UVS)
            // see http://blogs.adobe.com/CCJKType/2013/05/opentype-cmap-table-ramblings.html
            Debug.WriteLine("warn: Format 14 cmap table is not supported and will be ignored");
        }

        /**
         * Reads a format 6 subtable.
         * 
         * @param data the data stream of the to be parsed ttf font
         * @param numGlyphs number of glyphs to be read
         * @ If there is an error parsing the true type font.
         */
        void ProcessSubtype6(TTFDataStream data, int numGlyphs)
        {
            int firstCode = data.ReadUnsignedShort();
            int entryCount = data.ReadUnsignedShort();
            // skip empty tables
            if (entryCount == 0)
            {
                return;
            }
            characterCodeToGlyphId = new Dictionary<int, int>(numGlyphs);
            ushort[] glyphIdArray = data.ReadUnsignedShortArray(entryCount);
            int maxGlyphId = 0;
            for (int i = 0; i < entryCount; i++)
            {
                maxGlyphId = Math.Max(maxGlyphId, glyphIdArray[i]);
                characterCodeToGlyphId[firstCode + i] = glyphIdArray[i];
            }
            BuildGlyphIdToCharacterCodeLookup(maxGlyphId);
        }

        /**
         * Reads a format 4 subtable.
         * 
         * @param data the data stream of the to be parsed ttf font
         * @param numGlyphs number of glyphs to be read
         * @ If there is an error parsing the true type font.
         */
        void ProcessSubtype4(TTFDataStream data, int numGlyphs)
        {
            var segCountX2 = data.ReadUnsignedShort();
            var segCount = segCountX2 / 2;
            var searchRange = data.ReadUnsignedShort();
            var entrySelector = data.ReadUnsignedShort();
            var rangeShift = data.ReadUnsignedShort();
            var endCount = data.ReadUnsignedShortArray(segCount);
            var reservedPad = data.ReadUnsignedShort();
            var startCount = data.ReadUnsignedShortArray(segCount);
            var idDelta = data.ReadUnsignedShortArray(segCount);
            var idRangeOffsetPosition = data.CurrentPosition;
            var idRangeOffset = data.ReadUnsignedShortArray(segCount);

            characterCodeToGlyphId = new Dictionary<int, int>(numGlyphs);
            int maxGlyphId = 0;

            for (int i = 0; i < segCount; i++)
            {
                int start = startCount[i];
                int end = endCount[i];
                int delta = idDelta[i];
                int rangeOffset = idRangeOffset[i];
                long segmentRangeOffset = idRangeOffsetPosition + (i * 2) + rangeOffset;
                if (start != 65535 && end != 65535)
                {
                    for (int j = start; j <= end; j++)
                    {
                        if (rangeOffset == 0)
                        {
                            int glyphid = (j + delta) & 0xFFFF;
                            maxGlyphId = Math.Max(glyphid, maxGlyphId);
                            characterCodeToGlyphId[j] = glyphid;
                        }
                        else
                        {
                            long glyphOffset = segmentRangeOffset + ((j - start) * 2);
                            data.Seek(glyphOffset);
                            int glyphIndex = data.ReadUnsignedShort();
                            if (glyphIndex != 0)
                            {
                                glyphIndex = (glyphIndex + delta) & 0xFFFF;
                                maxGlyphId = Math.Max(glyphIndex, maxGlyphId);
                                characterCodeToGlyphId[j] = glyphIndex;
                            }
                        }
                    }
                }
            }

            /*
             * this is the final result key=glyphId, value is character codes Create an array that contains MAX(GlyphIds)
             * element, or -1
             */
            if (characterCodeToGlyphId.Count == 0)
            {
                Debug.WriteLine("warn: cmap format 4 subtable is empty");
                return;
            }
            BuildGlyphIdToCharacterCodeLookup(maxGlyphId);
        }

        private void BuildGlyphIdToCharacterCodeLookup(int maxGlyphId)
        {
            glyphIdToCharacterCode = NewGlyphIdToCharacterCode(maxGlyphId + 1);
            foreach (var entry in characterCodeToGlyphId)
            {
                if (glyphIdToCharacterCode[entry.Value] == -1)
                {
                    // add new value to the array
                    glyphIdToCharacterCode[entry.Value] = entry.Key;
                }
                else
                {
                    // there is already a mapping for the given glyphId
                    if (!glyphIdToCharacterCodeMultiple.TryGetValue(entry.Value, out List<int> mappedValues))
                    {
                        mappedValues = new List<int>();
                        glyphIdToCharacterCodeMultiple[entry.Value] = mappedValues;
                        mappedValues.Add(glyphIdToCharacterCode[entry.Value]);
                        // mark value as multiple mapping
                        glyphIdToCharacterCode[entry.Value] = int.MinValue;
                    }
                    mappedValues.Add(entry.Key);
                }
            }
        }

        /**
         * Read a format 2 subtable.
         * 
         * @param data the data stream of the to be parsed ttf font
         * @param numGlyphs number of glyphs to be read
         * @ If there is an error parsing the true type font.
         */
        void ProcessSubtype2(TTFDataStream data, int numGlyphs)
        {
            int[] subHeaderKeys = new int[256];
            // ---- keep the Max Index of the SubHeader array to know its length
            int maxSubHeaderIndex = 0;
            for (int i = 0; i < 256; i++)
            {
                subHeaderKeys[i] = data.ReadUnsignedShort();
                maxSubHeaderIndex = Math.Max(maxSubHeaderIndex, subHeaderKeys[i] / 8);
            }

            // ---- Read all SubHeaders to avoid useless seek on DataSource
            SubHeader[] subHeaders = new SubHeader[maxSubHeaderIndex + 1];
            for (int i = 0; i <= maxSubHeaderIndex; ++i)
            {
                int firstCode = data.ReadUnsignedShort();
                int entryCount = data.ReadUnsignedShort();
                short idDelta = data.ReadSignedShort();
                int idRangeOffset = data.ReadUnsignedShort() - (maxSubHeaderIndex + 1 - i - 1) * 8 - 2;
                subHeaders[i] = new SubHeader(firstCode, entryCount, idDelta, idRangeOffset);
            }
            long startGlyphIndexOffset = data.CurrentPosition;
            glyphIdToCharacterCode = NewGlyphIdToCharacterCode(numGlyphs);
            characterCodeToGlyphId = new Dictionary<int, int>(numGlyphs);
            if (numGlyphs == 0)
            {
                Debug.WriteLine("warn: subtable has no glyphs");
                return;
            }
            for (int i = 0; i <= maxSubHeaderIndex; ++i)
            {
                SubHeader sh = subHeaders[i];
                int firstCode = sh.FirstCode;
                int idRangeOffset = sh.IdRangeOffset;
                int idDelta = sh.IdDelta;
                int entryCount = sh.EntryCount;
                data.Seek(startGlyphIndexOffset + idRangeOffset);
                for (int j = 0; j < entryCount; ++j)
                {
                    // ---- compute the Character Code
                    int charCode = i;
                    charCode = (charCode << 8) + (firstCode + j);

                    // ---- Go to the CharacterCOde position in the Sub Array
                    // of the glyphIndexArray
                    // glyphIndexArray contains Unsigned Short so add (j * 2) bytes
                    // at the index position
                    int p = data.ReadUnsignedShort();
                    // ---- compute the glyphIndex
                    if (p > 0)
                    {
                        p = (p + idDelta) % 65536;
                        if (p < 0)
                        {
                            p += 65536;
                        }
                    }

                    if (p >= numGlyphs)
                    {
                        Debug.WriteLine($"warn: glyphId {p} for charcode {charCode} ignored, numGlyphs is {numGlyphs}");
                        continue;
                    }

                    glyphIdToCharacterCode[p] = charCode;
                    characterCodeToGlyphId[charCode] = p;
                }
            }
        }

        /**
         * Initialize the CMapEntry when it is a subtype 0.
         * 
         * @param data the data stream of the to be parsed ttf font
         * @ If there is an error parsing the true type font.
         */
        void ProcessSubtype0(TTFDataStream data)
        {
            byte[] glyphMapping = data.Read(256);
            glyphIdToCharacterCode = NewGlyphIdToCharacterCode(256);
            characterCodeToGlyphId = new Dictionary<int, int>(glyphMapping.Length);
            for (int i = 0; i < glyphMapping.Length; i++)
            {
                int glyphIndex = glyphMapping[i] & 0xFF;
                glyphIdToCharacterCode[glyphIndex] = i;
                characterCodeToGlyphId[i] = glyphIndex;
            }
        }

        /**
         * Workaround for the fact that glyphIdToCharacterCode doesn't distinguish between
         * missing character codes and code 0.
         */
        private int[] NewGlyphIdToCharacterCode(int size)
        {
            int[] gidToCode = new int[size];
            gidToCode.Fill(-1);
            return gidToCode;
        }

        /**
         * Returns the GlyphId linked with the given character code.
         *
         * @param characterCode the given character code to be mapped
         * @return glyphId the corresponding glyph id for the given character code
         */

        public virtual int GetGlyphId(int characterCode)
        {
            return characterCodeToGlyphId.TryGetValue(characterCode, out int glyphId) ? glyphId : 0;
        }

        /**
         * Returns the character code for the given GID, or null if there is none.
         *
         * @param gid glyph id
         * @return character code
         * 
         * @deprecated the mapping may be ambiguous, see {@link #getCharCodes(int)}. The first mapped value is returned by
         * default.
         */
        public int? GetCharacterCode(int gid)
        {
            int code = GetCharCode(gid);
            if (code == -1)
            {
                return null;
            }
            // ambiguous mapping
            if (code == int.MinValue)
            {
                if (glyphIdToCharacterCodeMultiple.TryGetValue(gid, out List<int> mappedValues))
                {
                    // use the first mapping
                    return mappedValues[0];
                }
            }
            return code;
        }

        private int GetCharCode(int gid)
        {
            if (gid < 0 || gid >= glyphIdToCharacterCode.Length)
            {
                return -1;
            }
            return glyphIdToCharacterCode[gid];
        }

        /**
         * Returns all possible character codes for the given gid, or null if there is none.
         *
         * @param gid glyph id
         * @return a list with all character codes the given gid maps to
         * 
         */
        public virtual List<int> GetCharCodes(int gid)
        {
            int code = GetCharCode(gid);
            if (code == -1)
            {
                return null;
            }
            List<int> codes = null;
            if (code == int.MinValue)
            {
                if (glyphIdToCharacterCodeMultiple.TryGetValue(gid, out List<int> mappedValues))
                {
                    codes = new List<int>(mappedValues);
                    // sort the list to provide a reliable order
                    codes.Sort();
                }
            }
            else
            {
                codes = new List<int>(1);
                codes.Add(code);
            }
            return codes;
        }

        override
        public string ToString()
        {
            return $"{{{PlatformId} {PlatformEncodingId}}}";
        }

        /**
         * 
         * Class used to manage CMap - Format 2.
         * 
         */
        private class SubHeader
        {
            private readonly int firstCode;
            private readonly int entryCount;
            /**
             * used to compute the GlyphIndex : P = glyphIndexArray.SubArray[pos] GlyphIndex = P + idDelta % 65536.
             */
            private readonly short idDelta;
            /**
             * Number of bytes to skip to reach the firstCode in the glyphIndexArray.
             */
            private readonly int idRangeOffset;

            public SubHeader(int firstCodeValue, int entryCountValue, short idDeltaValue, int idRangeOffsetValue)
            {
                firstCode = firstCodeValue;
                entryCount = entryCountValue;
                idDelta = idDeltaValue;
                idRangeOffset = idRangeOffsetValue;
            }

            /**
             * @return the firstCode
             */
            public int FirstCode
            {
                get => firstCode;
            }

            /**
             * @return the entryCount
             */
            public int EntryCount
            {
                get => entryCount;
            }

            /**
             * @return the idDelta
             */
            public short IdDelta
            {
                get => idDelta;
            }

            /**
             * @return the idRangeOffset
             */
            public int IdRangeOffset
            {
                get => idRangeOffset;
            }
        }
    }
}