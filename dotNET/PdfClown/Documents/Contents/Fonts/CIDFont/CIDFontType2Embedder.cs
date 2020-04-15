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
using System;
using System.Collections.Generic;

namespace PdfClown.Documents.Contents.Fonts
{


    /**
     * Embedded PDCIDFontType2 builder. Helper class to populate a PDCIDFontType2 and its parent
     * PDType0Font from a TTF.
     *
     * @author Keiji Suzuki
     * @author John Hewson
     */
    sealed class PDCIDFontType2Embedder : TrueTypeEmbedder
    {

        private readonly PdfDocument document;
        private readonly Type0Font parent;
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
        PDCIDFontType2Embedder(PDDocument document, PdfDictionary dict, TrueTypeFont ttf,
                bool embedSubset, PDType0Font parent, bool vertical)
                : base(document, dict, ttf, embedSubset)
        {
            this.document = document;
            this.dict = dict;
            this.parent = parent;
            this.vertical = vertical;

            // parent Type 0 font
            dict.setItem(COSName.SUBTYPE, COSName.TYPE0);
            dict.setName(COSName.BASE_FONT, fontDescriptor.getFontName());
            dict.setItem(COSName.ENCODING, vertical ? COSName.IDENTITY_V : COSName.IDENTITY_H); // CID = GID

            // descendant CIDFont
            cidFont = createCIDFont();
            PdfArray descendantFonts = new PdfArray();
            descendantFonts.add(cidFont);
            dict.setItem(COSName.DESCENDANT_FONTS, descendantFonts);

            if (!embedSubset)
            {
                // build GID -> Unicode map
                buildToUnicodeCMap(null);
            }
        }

        /**
         * Rebuild a font subset.
         */
        override protected void buildSubset(InputStream ttfSubset, String tag, Dictionary<Integer, Integer> gidToCid)
        {
            // build CID2GIDMap, because the content stream has been written with the old GIDs
            Dictionary<Integer, Integer> cidToGid = new HashMap<>(gidToCid.size());
            gidToCid.forEach((newGID, oldGID)->cidToGid.put(oldGID, newGID));

            // build unicode mapping before subsetting as the subsetted font won't have a cmap
            buildToUnicodeCMap(gidToCid);
            // build vertical metrics before subsetting as the subsetted font won't have vhea, vmtx
            if (vertical)
            {
                buildVerticalMetrics(cidToGid);
            }
            // rebuild the relevant part of the font
            buildFontFile2(ttfSubset);
            addNameTag(tag);
            buildWidths(cidToGid);
            buildCIDToGIDMap(cidToGid);
            buildCIDSet(cidToGid);
        }

        private void buildToUnicodeCMap(Dictionary<Integer, Integer> newGIDToOldCID)
        {
            ToUnicodeWriter toUniWriter = new ToUnicodeWriter();
            bool hasSurrogates = false;
            for (int gid = 1, max = ttf.getMaximumProfile().getNumGlyphs(); gid <= max; gid++)
            {
                // optional CID2GIDMap for subsetting
                int cid;
                if (newGIDToOldCID != null)
                {
                    if (!newGIDToOldCID.containsKey(gid))
                    {
                        continue;
                    }
                    else
                    {
                        cid = newGIDToOldCID.get(gid);
                    }
                }
                else
                {
                    cid = gid;
                }

                // skip composite glyph components that have no code point
                List<Integer> codes = cmapLookup.getCharCodes(cid); // old GID -> Unicode
                if (codes != null)
                {
                    // use the first entry even for ambiguous mappings
                    int codePoint = codes.get(0);
                    if (codePoint > 0xFFFF)
                    {
                        hasSurrogates = true;
                    }
                    toUniWriter.add(cid, new String(new int[] { codePoint }, 0, 1));
                }
            }

            ByteArrayOutputStream output = new ByteArrayOutputStream();
            toUniWriter.writeTo(output);
            InputStream cMapStream = new ByteArrayInputStream(output.toByteArray());

            PDStream stream = new PDStream(document, cMapStream, COSName.FLATE_DECODE);

            // surrogate code points, requires PDF 1.5
            if (hasSurrogates)
            {
                float version = document.getVersion();
                if (version < 1.5)
                {
                    document.setVersion(1.5f);
                }
            }

            dict.setItem(COSName.TO_UNICODE, stream);
        }

        private PdfDictionary toCIDSystemInfo(String registry, String ordering, int supplement)
        {
            PdfDictionary info = new PdfDictionary();
            info.setString(COSName.REGISTRY, registry);
            info.setString(COSName.ORDERING, ordering);
            info.setInt(COSName.SUPPLEMENT, supplement);
            return info;
        }

        private PdfDictionary createCIDFont()
        {
            PdfDictionary cidFont = new PdfDictionary();

            // Type, Subtype
            cidFont.setItem(COSName.TYPE, COSName.FONT);
            cidFont.setItem(COSName.SUBTYPE, COSName.CID_FONT_TYPE2);

            // BaseFont
            cidFont.setName(COSName.BASE_FONT, fontDescriptor.getFontName());

            // CIDSystemInfo
            PdfDictionary info = toCIDSystemInfo("Adobe", "Identity", 0);
            cidFont.setItem(COSName.CIDSYSTEMINFO, info);

            // FontDescriptor
            cidFont.setItem(COSName.FONT_DESC, fontDescriptor.getCOSObject());

            // W - widths
            buildWidths(cidFont);

            // Vertical metrics
            if (vertical)
            {
                buildVerticalMetrics(cidFont);
            }

            // CIDToGIDMap
            cidFont.setItem(COSName.CID_TO_GID_MAP, COSName.IDENTITY);

            return cidFont;
        }

        private void addNameTag(String tag)
        {
            String name = fontDescriptor.getFontName();
            String newName = tag + name;

            dict.setName(COSName.BASE_FONT, newName);
            fontDescriptor.setFontName(newName);
            cidFont.setName(COSName.BASE_FONT, newName);
        }

        private void buildCIDToGIDMap(Dictionary<Integer, Integer> cidToGid)
        {
            ByteArrayOutputStream output = new ByteArrayOutputStream();
            int cidMax = Collections.max(cidToGid.keySet());
            for (int i = 0; i <= cidMax; i++)
            {
                int gid;
                if (cidToGid.containsKey(i))
                {
                    gid = cidToGid.get(i);
                }
                else
                {
                    gid = 0;
                }
                output.write(new byte[] { (byte)(gid >> 8 & 0xff), (byte)(gid & 0xff) });
            }

            InputStream input = new ByteArrayInputStream(output.toByteArray());
            PDStream stream = new PDStream(document, input, COSName.FLATE_DECODE);

            cidFont.setItem(COSName.CID_TO_GID_MAP, stream);
        }

        /**
         * Builds the CIDSet entry, required by PDF/A. This lists all CIDs in the font, including those
         * that don't have a GID.
         */
        private void buildCIDSet(Dictionary<Integer, Integer> cidToGid)
        {
            int cidMax = Collections.max(cidToGid.keySet());
            byte[] bytes = new byte[cidMax / 8 + 1];
            for (int cid = 0; cid <= cidMax; cid++)
            {
                int mask = 1 << 7 - cid % 8;
                bytes[cid / 8] |= mask;
            }

            InputStream input = new ByteArrayInputStream(bytes);
            PDStream stream = new PDStream(document, input, COSName.FLATE_DECODE);

            fontDescriptor.setCIDSet(stream);
        }

        /**
         * Builds widths with a custom CIDToGIDMap (for embedding font subset).
         */
        private void buildWidths(Dictionary<Integer, Integer> cidToGid)
        {
            float scaling = 1000f / ttf.getHeader().getUnitsPerEm();

            PdfArray widths = new PdfArray();
            PdfArray ws = new PdfArray();
            int prev = Integer.MIN_VALUE;
            // Use a sorted list to get an optimal width array  
            Set<Integer> keys = new TreeSet<>(cidToGid.keySet());
            foreach (int cid in keys)
            {
                int gid = cidToGid.get(cid);
                long width = Math.round(ttf.getHorizontalMetrics().getAdvanceWidth(gid) * scaling);
                if (width == 1000)
                {
                    // skip default width
                    continue;
                }
                // c [w1 w2 ... wn]
                if (prev != cid - 1)
                {
                    ws = new PdfArray();
                    widths.add(PdfInteger.get(cid)); // c
                    widths.add(ws);
                }
                ws.add(PdfInteger.get(width)); // wi
                prev = cid;
            }
            cidFont.setItem(COSName.W, widths);
        }

        private bool buildVerticalHeader(PdfDictionary cidFont)
        {
            VerticalHeaderTable vhea = ttf.getVerticalHeader();
            if (vhea == null)
            {
                LOG.warn("Font to be subset is set to vertical, but has no 'vhea' table");
                return false;
            }

            float scaling = 1000f / ttf.getHeader().getUnitsPerEm();

            long v = Math.round(vhea.getAscender() * scaling);
            long w1 = Math.round(-vhea.getAdvanceHeightMax() * scaling);
            if (v != 880 || w1 != -1000)
            {
                PdfArray cosDw2 = new PdfArray();
                cosDw2.add(PdfInteger.get(v));
                cosDw2.add(PdfInteger.get(w1));
                cidFont.setItem(COSName.DW2, cosDw2);
            }
            return true;
        }

        /**
         * Builds vertical metrics with a custom CIDToGIDMap (for embedding font subset).
         */
        private void buildVerticalMetrics(Dictionary<Integer, Integer> cidToGid)
        {
            // The "vhea" and "vmtx" tables that specify vertical metrics shall never be used by a conforming
            // reader. The only way to specify vertical metrics in PDF shall be by means of the DW2 and W2
            // entries in a CIDFont dictionary.

            if (!buildVerticalHeader(cidFont))
            {
                return;
            }

            float scaling = 1000f / ttf.getHeader().getUnitsPerEm();

            VerticalHeaderTable vhea = ttf.getVerticalHeader();
            VerticalMetricsTable vmtx = ttf.getVerticalMetrics();
            GlyphTable glyf = ttf.getGlyph();
            HorizontalMetricsTable hmtx = ttf.getHorizontalMetrics();

            long v_y = Math.round(vhea.getAscender() * scaling);
            long w1 = Math.round(-vhea.getAdvanceHeightMax() * scaling);

            PdfArray heights = new PdfArray();
            PdfArray w2 = new PdfArray();
            int prev = Integer.MIN_VALUE;
            // Use a sorted list to get an optimal width array
            Set<Integer> keys = new TreeSet<>(cidToGid.keySet());
            foreach (int cid in keys)
            {
                // Unlike buildWidths, we look up with cid (not gid) here because this is
                // the original TTF, not the rebuilt one.
                GlyphData glyph = glyf.getGlyph(cid);
                if (glyph == null)
                {
                    continue;
                }
                long height = Math.round((glyph.getYMaximum() + vmtx.getTopSideBearing(cid)) * scaling);
                long advance = Math.round(-vmtx.getAdvanceHeight(cid) * scaling);
                if (height == v_y && advance == w1)
                {
                    // skip default metrics
                    continue;
                }
                // c [w1_1y v_1x v_1y w1_2y v_2x v_2y ... w1_ny v_nx v_ny]
                if (prev != cid - 1)
                {
                    w2 = new PdfArray();
                    heights.add(PdfInteger.get(cid)); // c
                    heights.add(w2);
                }
                w2.add(PdfInteger.get(advance)); // w1_iy
                long width = Math.round(hmtx.getAdvanceWidth(cid) * scaling);
                w2.add(PdfInteger.get(width / 2)); // v_ix
                w2.add(PdfInteger.get(height)); // v_iy
                prev = cid;
            }
            cidFont.setItem(COSName.W2, heights);
        }

        /**
         * Build widths with Identity CIDToGIDMap (for embedding full font).
         */
        private void buildWidths(PdfDictionary cidFont)
        {
            int cidMax = ttf.getNumberOfGlyphs();
            int[] gidwidths = new int[cidMax * 2];
            for (int cid = 0; cid < cidMax; cid++)
            {
                gidwidths[cid * 2] = cid;
                gidwidths[cid * 2 + 1] = ttf.getHorizontalMetrics().getAdvanceWidth(cid);
            }

            cidFont.setItem(COSName.W, getWidths(gidwidths));
        }

        enum State
        {
            FIRST, BRACKET, SERIAL
        }

        private PdfArray getWidths(int[] widths)
        {
            if (widths.length == 0)
            {
                throw new IllegalArgumentException("length of widths must be > 0");
            }

            float scaling = 1000f / ttf.getHeader().getUnitsPerEm();

            long lastCid = widths[0];
            long lastValue = Math.round(widths[1] * scaling);

            PdfArray inner = new PdfArray();
            PdfArray outer = new PdfArray();
            outer.add(PdfInteger.get(lastCid));

            State state = State.FIRST;

            for (int i = 2; i < widths.length; i += 2)
            {
                long cid = widths[i];
                long value = Math.round(widths[i + 1] * scaling);

                switch (state)
                {
                    case FIRST:
                        if (cid == lastCid + 1 && value == lastValue)
                        {
                            state = State.SERIAL;
                        }
                        else if (cid == lastCid + 1)
                        {
                            state = State.BRACKET;
                            inner = new PdfArray();
                            inner.add(PdfInteger.get(lastValue));
                        }
                        else
                        {
                            inner = new PdfArray();
                            inner.add(PdfInteger.get(lastValue));
                            outer.add(inner);
                            outer.add(PdfInteger.get(cid));
                        }
                        break;
                    case BRACKET:
                        if (cid == lastCid + 1 && value == lastValue)
                        {
                            state = State.SERIAL;
                            outer.add(inner);
                            outer.add(PdfInteger.get(lastCid));
                        }
                        else if (cid == lastCid + 1)
                        {
                            inner.add(PdfInteger.get(lastValue));
                        }
                        else
                        {
                            state = State.FIRST;
                            inner.add(PdfInteger.get(lastValue));
                            outer.add(inner);
                            outer.add(PdfInteger.get(cid));
                        }
                        break;
                    case SERIAL:
                        if (cid != lastCid + 1 || value != lastValue)
                        {
                            outer.add(PdfInteger.get(lastCid));
                            outer.add(PdfInteger.get(lastValue));
                            outer.add(PdfInteger.get(cid));
                            state = State.FIRST;
                        }
                        break;
                }
                lastValue = value;
                lastCid = cid;
            }

            switch (state)
            {
                case FIRST:
                    inner = new PdfArray();
                    inner.add(PdfInteger.get(lastValue));
                    outer.add(inner);
                    break;
                case BRACKET:
                    inner.add(PdfInteger.get(lastValue));
                    outer.add(inner);
                    break;
                case SERIAL:
                    outer.add(PdfInteger.get(lastCid));
                    outer.add(PdfInteger.get(lastValue));
                    break;
            }
            return outer;
        }

        /**
         * Build vertical metrics with Identity CIDToGIDMap (for embedding full font).
         */
        private void buildVerticalMetrics(PdfDictionary cidFont)
        {
            if (!buildVerticalHeader(cidFont))
            {
                return;
            }

            int cidMax = ttf.getNumberOfGlyphs();
            int[]
        gidMetrics = new int[cidMax * 4];
            for (int cid = 0; cid < cidMax; cid++)
            {
                GlyphData glyph = ttf.getGlyph().getGlyph(cid);
                if (glyph == null)
                {
                    gidMetrics[cid * 4] = Integer.MIN_VALUE;
                }
                else
                {
                    gidMetrics[cid * 4] = cid;
                    gidMetrics[cid * 4 + 1] = ttf.getVerticalMetrics().getAdvanceHeight(cid);
                    gidMetrics[cid * 4 + 2] = ttf.getHorizontalMetrics().getAdvanceWidth(cid);
                    gidMetrics[cid * 4 + 3] = glyph.getYMaximum() + ttf.getVerticalMetrics().getTopSideBearing(cid);
                }
            }

            cidFont.setItem(COSName.W2, getVerticalMetrics(gidMetrics));
        }

        private PdfArray getVerticalMetrics(int[] values)
        {
            if (values.length == 0)
            {
                throw new IllegalArgumentException("length of values must be > 0");
            }

            float scaling = 1000f / ttf.getHeader().getUnitsPerEm();

            long lastCid = values[0];
            long lastW1Value = Math.round(-values[1] * scaling);
            long lastVxValue = Math.round(values[2] * scaling / 2f);
            long lastVyValue = Math.round(values[3] * scaling);

            PdfArray inner = new PdfArray();
            PdfArray outer = new PdfArray();
            outer.add(PdfInteger.get(lastCid));

            State state = State.FIRST;

            for (int i = 4; i < values.length; i += 4)
            {
                long cid = values[i];
                if (cid == Integer.MIN_VALUE)
                {
                    // no glyph for this cid
                    continue;
                }
                long w1Value = Math.round(-values[i + 1] * scaling);
                long vxValue = Math.round(values[i + 2] * scaling / 2);
                long vyValue = Math.round(values[i + 3] * scaling);

                switch (state)
                {
                    case FIRST:
                        if (cid == lastCid + 1 && w1Value == lastW1Value && vxValue == lastVxValue && vyValue == lastVyValue)
                        {
                            state = State.SERIAL;
                        }
                        else if (cid == lastCid + 1)
                        {
                            state = State.BRACKET;
                            inner = new PdfArray();
                            inner.add(PdfInteger.get(lastW1Value));
                            inner.add(PdfInteger.get(lastVxValue));
                            inner.add(PdfInteger.get(lastVyValue));
                        }
                        else
                        {
                            inner = new PdfArray();
                            inner.add(PdfInteger.get(lastW1Value));
                            inner.add(PdfInteger.get(lastVxValue));
                            inner.add(PdfInteger.get(lastVyValue));
                            outer.add(inner);
                            outer.add(PdfInteger.get(cid));
                        }
                        break;
                    case BRACKET:
                        if (cid == lastCid + 1 && w1Value == lastW1Value && vxValue == lastVxValue && vyValue == lastVyValue)
                        {
                            state = State.SERIAL;
                            outer.add(inner);
                            outer.add(PdfInteger.get(lastCid));
                        }
                        else if (cid == lastCid + 1)
                        {
                            inner.add(PdfInteger.get(lastW1Value));
                            inner.add(PdfInteger.get(lastVxValue));
                            inner.add(PdfInteger.get(lastVyValue));
                        }
                        else
                        {
                            state = State.FIRST;
                            inner.add(PdfInteger.get(lastW1Value));
                            inner.add(PdfInteger.get(lastVxValue));
                            inner.add(PdfInteger.get(lastVyValue));
                            outer.add(inner);
                            outer.add(PdfInteger.get(cid));
                        }
                        break;
                    case SERIAL:
                        if (cid != lastCid + 1 || w1Value != lastW1Value || vxValue != lastVxValue || vyValue != lastVyValue)
                        {
                            outer.add(PdfInteger.get(lastCid));
                            outer.add(PdfInteger.get(lastW1Value));
                            outer.add(PdfInteger.get(lastVxValue));
                            outer.add(PdfInteger.get(lastVyValue));
                            outer.add(PdfInteger.get(cid));
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
                case FIRST:
                    inner = new PdfArray();
                    inner.add(PdfInteger.get(lastW1Value));
                    inner.add(PdfInteger.get(lastVxValue));
                    inner.add(PdfInteger.get(lastVyValue));
                    outer.add(inner);
                    break;
                case BRACKET:
                    inner.add(PdfInteger.get(lastW1Value));
                    inner.add(PdfInteger.get(lastVxValue));
                    inner.add(PdfInteger.get(lastVyValue));
                    outer.add(inner);
                    break;
                case SERIAL:
                    outer.add(PdfInteger.get(lastCid));
                    outer.add(PdfInteger.get(lastW1Value));
                    outer.add(PdfInteger.get(lastVxValue));
                    outer.add(PdfInteger.get(lastVyValue));
                    break;
            }
            return outer;
        }

        /**
         * Returns the descendant CIDFont.
         */
        public PDCIDFont getCIDFont()
        {
            return new PDCIDFontType2(cidFont, parent, ttf);
        }
    }
}
