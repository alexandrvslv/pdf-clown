/*
 * https://github.com/apache/pdfbox
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
using PdfClown.Documents.Contents.Fonts.TTF;
using PdfClown.Objects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace PdfClown.Documents.Contents.Fonts
{
    /**
     * Embedded PDCIDFontType2 builder. Helper class to populate a PDCIDFontType2 and its parent
     * PDType0Font from a TTF.
     *
     * @author Keiji Suzuki
     * @author John Hewson
     */
    internal sealed class PdfCIDFontType2Embedder : TrueTypeEmbedder
    {
        private readonly Document document;
        private readonly PdfType0Font parent;
        private readonly PdfDictionary dict;
        private readonly PdfDictionary cidFont;
        private readonly bool vertical;

        /**
         * Creates a new TrueType font embedder for the given TTF as a PDCIDFontType2.
         *
         * @param document parent document
         * @param dict font dictionary
         * @param ttf True Type Font
         * @param parent parent Type 0 font
         * @ if the TTF could not be read
         */
        public PdfCIDFontType2Embedder(Document document, PdfDictionary dict, TrueTypeFont ttf, bool embedSubset, PdfType0Font parent, bool vertical)
                : base(document, dict, ttf, embedSubset)
        {
            this.document = document;
            this.dict = dict;
            this.parent = parent;
            this.vertical = vertical;

            // parent Type 0 font
            dict[PdfName.Subtype] = PdfName.Type0;
            dict[PdfName.BaseFont] = PdfName.Get(FontDescriptor.FontName);
            dict[PdfName.Encoding] = vertical ? PdfName.IdentityV : PdfName.IdentityH; // CID = GID

            // descendant CIDFont
            cidFont = CreateCIDFont();
            PdfArray descendantFonts = new PdfArray();
            descendantFonts.Add(cidFont);
            dict[PdfName.DescendantFonts] = descendantFonts;

            if (!embedSubset)
            {
                // build GID -> Unicode map
                BuildToUnicodeCMap(null);
            }
        }

        /**
         * Rebuild a font subset.
         */
        protected override void BuildSubset(Bytes.Buffer ttfSubset, string tag, Dictionary<int, int> gidToCid)
        {
            // build CID2GIDMap, because the content stream has been written with the old GIDs
            Dictionary<int, int> cidToGid = new Dictionary<int, int>(gidToCid.Count);
            foreach (var entry in gidToCid)
            {
                //(newGID, oldGID)->
                cidToGid[entry.Value] = entry.Key;
            }

            // build unicode mapping before subsetting as the subsetted font won't have a cmap
            BuildToUnicodeCMap(gidToCid);
            // build vertical metrics before subsetting as the subsetted font won't have vhea, vmtx
            if (vertical)
            {
                BuildVerticalMetrics(cidToGid);
            }
            // rebuild the relevant part of the font
            BuildFontFile2(ttfSubset);
            AddNameTag(tag);
            BuildWidths(cidToGid);
            buildCIDToGIDMap(cidToGid);
            BuildCIDSet(cidToGid);
        }

        private void BuildToUnicodeCMap(Dictionary<int, int> newGIDToOldCID)
        {
            ToUnicodeWriter toUniWriter = new ToUnicodeWriter();
            bool hasSurrogates = false;
            for (int gid = 1, max = ttf.MaximumProfile.NumGlyphs; gid <= max; gid++)
            {
                // optional CID2GIDMap for subsetting
                int cid;
                if (newGIDToOldCID != null)
                {
                    if (!newGIDToOldCID.TryGetValue(gid, out cid))
                    {
                        continue;
                    }
                }
                else
                {
                    cid = gid;
                }

                // skip composite glyph components that have no code point
                List<int> codes = cmapLookup.GetCharCodes(cid); // old GID -> Unicode
                if (codes != null)
                {
                    // use the first entry even for ambiguous mappings
                    int codePoint = codes[0];
                    if (codePoint > 0xFFFF)
                    {
                        hasSurrogates = true;
                    }
                    toUniWriter.Add(cid, new string(new char[] { (char)codePoint }, 0, 1));
                }
            }

            var output = new MemoryStream();
            toUniWriter.WriteTo(output);
            var cMapStream = new Bytes.Buffer(output.ToArray());

            PdfStream stream = new PdfStream(cMapStream);

            // surrogate code points, requires PDF 1.5
            if (hasSurrogates)
            {
                var version = document.Version;
                if (version.GetFloat() < 1.5)
                {
                    document.Version = new Version(1, 5);
                }
            }

            dict[PdfName.ToUnicode] = stream.Reference;
        }

        private PdfDictionary toCIDSystemInfo(string registry, string ordering, int supplement)
        {
            PdfDictionary info = new PdfDictionary();
            info[PdfName.Registry] = new PdfString(registry);
            info[PdfName.Ordering] = new PdfString(ordering);
            info[PdfName.Supplement] = new PdfInteger(supplement);
            return info;
        }

        private PdfDictionary CreateCIDFont()
        {
            PdfDictionary cidFont = new PdfDictionary();

            // Type, Subtype
            cidFont[PdfName.Type] = PdfName.Font;
            cidFont[PdfName.Subtype] = PdfName.CIDFontType2;
            // BaseFont
            cidFont[PdfName.BaseFont] = PdfName.Get(fontDescriptor.FontName);
            // CIDSystemInfo
            PdfDictionary info = toCIDSystemInfo("Adobe", "Identity", 0);
            cidFont[PdfName.CIDSystemInfo] = info.Reference;
            // FontDescriptor
            cidFont[PdfName.FontDescriptor] = fontDescriptor.BaseObject;

            // W - widths
            BuildWidths(cidFont);

            // Vertical metrics
            if (vertical)
            {
                BuildVerticalMetrics(cidFont);
            }

            // CIDToGIDMap
            cidFont[PdfName.CIDToGIDMap] = PdfName.Identity;

            return cidFont;
        }

        private void AddNameTag(string tag)
        {
            string name = fontDescriptor.FontName;
            string newName = tag + name;

            dict[PdfName.BaseFont] = new PdfName(newName);
            fontDescriptor.FontName = newName;
            cidFont[PdfName.BaseFont] = new PdfName(newName);
        }

        private void buildCIDToGIDMap(Dictionary<int, int> cidToGid)
        {
            MemoryStream output = new MemoryStream();
            int cidMax = cidToGid.Keys.Max();
            for (int i = 0; i <= cidMax; i++)
            {
                int gid;
                if (!cidToGid.TryGetValue(i, out gid))
                {
                    gid = 0;
                }
                output.Write(new byte[] { (byte)(gid >> 8 & 0xff), (byte)(gid & 0xff) }, 0, 2);
            }

            var input = new Bytes.Buffer(output.ToArray());
            PdfStream stream = new PdfStream(input);

            cidFont[PdfName.CIDToGIDMap] = stream.Reference;
        }

        /**
         * Builds the CIDSet entry, required by PDF/A. This lists all CIDs in the font, including those
         * that don't have a GID.
         */
        private void BuildCIDSet(Dictionary<int, int> cidToGid)
        {
            int cidMax = cidToGid.Keys.Max();
            byte[] bytes = new byte[cidMax / 8 + 1];
            for (int cid = 0; cid <= cidMax; cid++)
            {
                int mask = 1 << 7 - cid % 8;
                bytes[cid / 8] = (byte)(bytes[cid / 8] | mask);
            }

            var input = new Bytes.Buffer(bytes);
            PdfStream stream = new PdfStream(input);

            fontDescriptor.CIDSet = stream;
        }

        /**
         * Builds widths with a custom CIDToGIDMap (for embedding font subset).
         */
        private void BuildWidths(Dictionary<int, int> cidToGid)
        {
            float scaling = 1000f / ttf.Header.UnitsPerEm;

            PdfArray widths = new PdfArray();
            PdfArray ws = new PdfArray();
            int prev = int.MinValue;
            // Use a sorted list to get an optimal width array  
            ISet<int> keys = new HashSet<int>(cidToGid.Keys);
            foreach (int cid in keys)
            {
                int gid = cidToGid[cid];
                long width = (long)Math.Round(ttf.HorizontalMetrics.GetAdvanceWidth(gid) * scaling);
                if (width == 1000)
                {
                    // skip default width
                    continue;
                }
                // c [w1 w2 ... wn]
                if (prev != cid - 1)
                {
                    ws = new PdfArray();
                    widths.Add(PdfInteger.Get(cid)); // c
                    widths.Add(ws);
                }
                ws.Add(PdfInteger.Get(width)); // wi
                prev = cid;
            }
            cidFont[PdfName.W] = widths;
        }

        private bool BuildVerticalHeader(PdfDictionary cidFont)
        {
            VerticalHeaderTable vhea = ttf.VerticalHeader;
            if (vhea == null)
            {
                Debug.WriteLine("warn: Font to be subset is set to vertical, but has no 'vhea' table");
                return false;
            }

            float scaling = 1000f / ttf.Header.UnitsPerEm;

            long v = (long)Math.Round(vhea.Ascender * scaling);
            long w1 = (long)Math.Round(-vhea.AdvanceHeightMax * scaling);
            if (v != 880 || w1 != -1000)
            {
                PdfArray cosDw2 = new PdfArray();
                cosDw2.Add(PdfInteger.Get(v));
                cosDw2.Add(PdfInteger.Get(w1));
                cidFont[PdfName.DW2] = cosDw2;
            }
            return true;
        }

        /**
         * Builds vertical metrics with a custom CIDToGIDMap (for embedding font subset).
         */
        private void BuildVerticalMetrics(Dictionary<int, int> cidToGid)
        {
            // The "vhea" and "vmtx" tables that specify vertical metrics shall never be used by a conforming
            // reader. The only way to specify vertical metrics in PDF shall be by means of the DW2 and W2
            // entries in a CIDFont dictionary.

            if (!BuildVerticalHeader(cidFont))
            {
                return;
            }

            float scaling = 1000f / ttf.Header.UnitsPerEm;

            VerticalHeaderTable vhea = ttf.VerticalHeader;
            VerticalMetricsTable vmtx = ttf.VerticalMetrics;
            GlyphTable glyf = ttf.Glyph;
            HorizontalMetricsTable hmtx = ttf.HorizontalMetrics;

            long v_y = (long)Math.Round(vhea.Ascender * scaling);
            long w1 = (long)Math.Round(-vhea.AdvanceHeightMax * scaling);

            PdfArray heights = new PdfArray();
            PdfArray w2 = new PdfArray();
            int prev = int.MinValue;
            // Use a sorted list to get an optimal width array
            ISet<int> keys = new HashSet<int>(cidToGid.Keys);
            foreach (int cid in keys)
            {
                // Unlike buildWidths, we look up with cid (not gid) here because this is
                // the original TTF, not the rebuilt one.
                GlyphData glyph = glyf.GetGlyph(cid);
                if (glyph == null)
                {
                    continue;
                }
                long height = (long)Math.Round((glyph.YMaximum + vmtx.GetTopSideBearing(cid)) * scaling);
                long advance = (long)Math.Round(-vmtx.GetAdvanceHeight(cid) * scaling);
                if (height == v_y && advance == w1)
                {
                    // skip default metrics
                    continue;
                }
                // c [w1_1y v_1x v_1y w1_2y v_2x v_2y ... w1_ny v_nx v_ny]
                if (prev != cid - 1)
                {
                    w2 = new PdfArray();
                    heights.Add(PdfInteger.Get(cid)); // c
                    heights.Add(w2);
                }
                w2.Add(PdfInteger.Get(advance)); // w1_iy
                long width = (long)Math.Round(hmtx.GetAdvanceWidth(cid) * scaling);
                w2.Add(PdfInteger.Get(width / 2)); // v_ix
                w2.Add(PdfInteger.Get(height)); // v_iy
                prev = cid;
            }
            cidFont[PdfName.W2] = heights;
        }

        /**
         * Build widths with Identity CIDToGIDMap (for embedding full font).
         */
        private void BuildWidths(PdfDictionary cidFont)
        {
            int cidMax = ttf.NumberOfGlyphs;
            int[] gidwidths = new int[cidMax * 2];
            for (int cid = 0; cid < cidMax; cid++)
            {
                gidwidths[cid * 2] = cid;
                gidwidths[cid * 2 + 1] = ttf.HorizontalMetrics.GetAdvanceWidth(cid);
            }

            cidFont[PdfName.W] = GetWidths(gidwidths);
        }

        enum State
        {
            FIRST, BRACKET, SERIAL
        }

        private PdfArray GetWidths(int[] widths)
        {
            if (widths.Length == 0)
            {
                throw new ArgumentException("length of widths must be > 0");
            }

            float scaling = 1000f / ttf.Header.UnitsPerEm;

            long lastCid = widths[0];
            long lastValue = (long)Math.Round(widths[1] * scaling);

            PdfArray inner = new PdfArray();
            PdfArray outer = new PdfArray();
            outer.Add(PdfInteger.Get(lastCid));

            State state = State.FIRST;

            for (int i = 2; i < widths.Length; i += 2)
            {
                long cid = widths[i];
                long value = (long)Math.Round(widths[i + 1] * scaling);

                switch (state)
                {
                    case State.FIRST:
                        if (cid == lastCid + 1 && value == lastValue)
                        {
                            state = State.SERIAL;
                        }
                        else if (cid == lastCid + 1)
                        {
                            state = State.BRACKET;
                            inner = new PdfArray();
                            inner.Add(PdfInteger.Get(lastValue));
                        }
                        else
                        {
                            inner = new PdfArray();
                            inner.Add(PdfInteger.Get(lastValue));
                            outer.Add(inner);
                            outer.Add(PdfInteger.Get(cid));
                        }
                        break;
                    case State.BRACKET:
                        if (cid == lastCid + 1 && value == lastValue)
                        {
                            state = State.SERIAL;
                            outer.Add(inner);
                            outer.Add(PdfInteger.Get(lastCid));
                        }
                        else if (cid == lastCid + 1)
                        {
                            inner.Add(PdfInteger.Get(lastValue));
                        }
                        else
                        {
                            state = State.FIRST;
                            inner.Add(PdfInteger.Get(lastValue));
                            outer.Add(inner);
                            outer.Add(PdfInteger.Get(cid));
                        }
                        break;
                    case State.SERIAL:
                        if (cid != lastCid + 1 || value != lastValue)
                        {
                            outer.Add(PdfInteger.Get(lastCid));
                            outer.Add(PdfInteger.Get(lastValue));
                            outer.Add(PdfInteger.Get(cid));
                            state = State.FIRST;
                        }
                        break;
                }
                lastValue = value;
                lastCid = cid;
            }

            switch (state)
            {
                case State.FIRST:
                    inner = new PdfArray();
                    inner.Add(PdfInteger.Get(lastValue));
                    outer.Add(inner);
                    break;
                case State.BRACKET:
                    inner.Add(PdfInteger.Get(lastValue));
                    outer.Add(inner);
                    break;
                case State.SERIAL:
                    outer.Add(PdfInteger.Get(lastCid));
                    outer.Add(PdfInteger.Get(lastValue));
                    break;
            }
            return outer;
        }

        /**
         * Build vertical metrics with Identity CIDToGIDMap (for embedding full font).
         */
        private void BuildVerticalMetrics(PdfDictionary cidFont)
        {
            if (!BuildVerticalHeader(cidFont))
            {
                return;
            }

            int cidMax = ttf.NumberOfGlyphs;
            int[]
        gidMetrics = new int[cidMax * 4];
            for (int cid = 0; cid < cidMax; cid++)
            {
                GlyphData glyph = ttf.Glyph.GetGlyph(cid);
                if (glyph == null)
                {
                    gidMetrics[cid * 4] = int.MinValue;
                }
                else
                {
                    gidMetrics[cid * 4] = cid;
                    gidMetrics[cid * 4 + 1] = ttf.VerticalMetrics.GetAdvanceHeight(cid);
                    gidMetrics[cid * 4 + 2] = ttf.HorizontalMetrics.GetAdvanceWidth(cid);
                    gidMetrics[cid * 4 + 3] = glyph.YMaximum + ttf.VerticalMetrics.GetTopSideBearing(cid);
                }
            }

            cidFont[PdfName.W2] = GetVerticalMetrics(gidMetrics);
        }

        private PdfArray GetVerticalMetrics(int[] values)
        {
            if (values.Length == 0)
            {
                throw new ArgumentException("length of values must be > 0");
            }

            float scaling = 1000f / ttf.Header.UnitsPerEm;

            long lastCid = values[0];
            long lastW1Value = (long)Math.Round(-values[1] * scaling);
            long lastVxValue = (long)Math.Round(values[2] * scaling / 2f);
            long lastVyValue = (long)Math.Round(values[3] * scaling);

            PdfArray inner = new PdfArray();
            PdfArray outer = new PdfArray();
            outer.Add(PdfInteger.Get(lastCid));

            State state = State.FIRST;

            for (int i = 4; i < values.Length; i += 4)
            {
                long cid = values[i];
                if (cid == int.MinValue)
                {
                    // no glyph for this cid
                    continue;
                }
                long w1Value = (long)Math.Round(-values[i + 1] * scaling);
                long vxValue = (long)Math.Round(values[i + 2] * scaling / 2);
                long vyValue = (long)Math.Round(values[i + 3] * scaling);

                switch (state)
                {
                    case State.FIRST:
                        if (cid == lastCid + 1 && w1Value == lastW1Value && vxValue == lastVxValue && vyValue == lastVyValue)
                        {
                            state = State.SERIAL;
                        }
                        else if (cid == lastCid + 1)
                        {
                            state = State.BRACKET;
                            inner = new PdfArray();
                            inner.Add(PdfInteger.Get(lastW1Value));
                            inner.Add(PdfInteger.Get(lastVxValue));
                            inner.Add(PdfInteger.Get(lastVyValue));
                        }
                        else
                        {
                            inner = new PdfArray();
                            inner.Add(PdfInteger.Get(lastW1Value));
                            inner.Add(PdfInteger.Get(lastVxValue));
                            inner.Add(PdfInteger.Get(lastVyValue));
                            outer.Add(inner);
                            outer.Add(PdfInteger.Get(cid));
                        }
                        break;
                    case State.BRACKET:
                        if (cid == lastCid + 1 && w1Value == lastW1Value && vxValue == lastVxValue && vyValue == lastVyValue)
                        {
                            state = State.SERIAL;
                            outer.Add(inner);
                            outer.Add(PdfInteger.Get(lastCid));
                        }
                        else if (cid == lastCid + 1)
                        {
                            inner.Add(PdfInteger.Get(lastW1Value));
                            inner.Add(PdfInteger.Get(lastVxValue));
                            inner.Add(PdfInteger.Get(lastVyValue));
                        }
                        else
                        {
                            state = State.FIRST;
                            inner.Add(PdfInteger.Get(lastW1Value));
                            inner.Add(PdfInteger.Get(lastVxValue));
                            inner.Add(PdfInteger.Get(lastVyValue));
                            outer.Add(inner);
                            outer.Add(PdfInteger.Get(cid));
                        }
                        break;
                    case State.SERIAL:
                        if (cid != lastCid + 1 || w1Value != lastW1Value || vxValue != lastVxValue || vyValue != lastVyValue)
                        {
                            outer.Add(PdfInteger.Get(lastCid));
                            outer.Add(PdfInteger.Get(lastW1Value));
                            outer.Add(PdfInteger.Get(lastVxValue));
                            outer.Add(PdfInteger.Get(lastVyValue));
                            outer.Add(PdfInteger.Get(cid));
                            state = State.FIRST;
                        }
                        break;
                }
                lastW1Value = w1Value;
                lastVxValue = vxValue;
                lastVyValue = vyValue;
                lastCid = cid;
            }

            switch (state)
            {
                case State.FIRST:
                    inner = new PdfArray();
                    inner.Add(PdfInteger.Get(lastW1Value));
                    inner.Add(PdfInteger.Get(lastVxValue));
                    inner.Add(PdfInteger.Get(lastVyValue));
                    outer.Add(inner);
                    break;
                case State.BRACKET:
                    inner.Add(PdfInteger.Get(lastW1Value));
                    inner.Add(PdfInteger.Get(lastVxValue));
                    inner.Add(PdfInteger.Get(lastVyValue));
                    outer.Add(inner);
                    break;
                case State.SERIAL:
                    outer.Add(PdfInteger.Get(lastCid));
                    outer.Add(PdfInteger.Get(lastW1Value));
                    outer.Add(PdfInteger.Get(lastVxValue));
                    outer.Add(PdfInteger.Get(lastVyValue));
                    break;
            }
            return outer;
        }

        /**
         * Returns the descendant CIDFont.
         */
        public CIDFont GetCIDFont()
        {
            return new CIDFontType2(cidFont, parent, ttf);
        }
    }
}
