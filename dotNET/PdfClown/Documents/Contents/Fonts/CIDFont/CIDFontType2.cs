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
     * Type 2 CIDFont (TrueType).
     * 
     * @author Ben Litchfield
     */
    public class CIDFontType2 : CIDFont
    {
        private readonly TrueTypeFont ttf;
        private readonly int[] cid2gid;
        private readonly bool isEmbedded;
        private readonly bool isDamaged;
        private readonly CmapLookup cmap; // may be null
        private SKMatrix fontMatrix;
        private BoundingBox fontBBox;
        private readonly HashSet<int> noMapping = new HashSet<int>();

        /**
         * Constructor.
         * 
         * @param fontDictionary The font dictionary according to the PDF specification.
         * @param parent The parent font.
         * @throws IOException
         */
        public PDCIDFontType2(COSDictionary fontDictionary, PDType0Font parent)
        {
            this(fontDictionary, parent, null);
        }

        /**
         * Constructor.
         * 
         * @param fontDictionary The font dictionary according to the PDF specification.
         * @param parent The parent font.
         * @param trueTypeFont The true type font used to create the parent font
         * @throws IOException
         */
        public PDCIDFontType2(COSDictionary fontDictionary, PDType0Font parent, TrueTypeFont trueTypeFont)
        {
            super(fontDictionary, parent);

            PDFontDescriptor fd = getFontDescriptor();
            if (trueTypeFont != null)
            {
                ttf = trueTypeFont;
                isEmbedded = true;
                isDamaged = false;
            }
            else
            {
                bool fontIsDamaged = false;
                TrueTypeFont ttfFont = null;

                PDStream stream = null;
                if (fd != null)
                {
                    stream = fd.getFontFile2();
                    if (stream == null)
                    {
                        stream = fd.getFontFile3();
                    }
                    if (stream == null)
                    {
                        // Acrobat looks in FontFile too, even though it is not in the spec, see PDFBOX-2599
                        stream = fd.getFontFile();
                    }
                }
                if (stream != null)
                {
                    try
                    {
                        // embedded OTF or TTF
                        OTFParser otfParser = new OTFParser(true);
                        OpenTypeFont otf = otfParser.parse(stream.createInputStream());
                        ttfFont = otf;

                        if (otf.isPostScript())
                        {
                            // PDFBOX-3344 contains PostScript outlines instead of TrueType
                            fontIsDamaged = true;
                            LOG.warn("Found CFF/OTF but expected embedded TTF font " + fd.getFontName());
                        }
                    }
                    catch (IOException e)
                    {
                        fontIsDamaged = true;
                        LOG.warn("Could not read embedded OTF for font " + getBaseFont(), e);
                    }
                }
                isEmbedded = ttfFont != null;
                isDamaged = fontIsDamaged;

                if (ttfFont == null)
                {
                    ttfFont = findFontOrSubstitute();
                }
                ttf = ttfFont;
            }
            cmap = ttf.getUnicodeCmapLookup(false);
            cid2gid = readCIDToGIDMap();
        }

        private TrueTypeFont findFontOrSubstitute()
        {
            TrueTypeFont ttfFont;

            CIDFontMapping mapping = FontMappers.instance()
                    .getCIDFont(getBaseFont(), getFontDescriptor(),
                            getCIDSystemInfo());
            if (mapping.isCIDFont())
            {
                ttfFont = mapping.getFont();
            }
            else
            {
                ttfFont = (TrueTypeFont)mapping.getTrueTypeFont();
            }
            if (mapping.isFallback())
            {
                LOG.warn("Using fallback font " + ttfFont.getName() + " for CID-keyed TrueType font " + getBaseFont());
            }
            return ttfFont;
        }

        override public SKMatrix getFontMatrix()
        {
            if (fontMatrix == null)
            {
                // 1000 upem, this is not strictly true
                fontMatrix = new SKMatrix(0.001f, 0, 0, 0.001f, 0, 0);
            }
            return fontMatrix;
        }

        override public BoundingBox getBoundingBox()
        {
            if (fontBBox == null)
            {
                fontBBox = generateBoundingBox();
            }
            return fontBBox;
        }

        private BoundingBox generateBoundingBox()
        {
            if (getFontDescriptor() != null)
            {
                PDRectangle bbox = getFontDescriptor().getFontBoundingBox();
                if (bbox != null &&
                        (Float.compare(bbox.getLowerLeftX(), 0) != 0 ||
                         Float.compare(bbox.getLowerLeftY(), 0) != 0 ||
                         Float.compare(bbox.getUpperRightX(), 0) != 0 ||
                         Float.compare(bbox.getUpperRightY(), 0) != 0))
                {
                    return new BoundingBox(bbox.getLowerLeftX(), bbox.getLowerLeftY(),
                                           bbox.getUpperRightX(), bbox.getUpperRightY());
                }
            }
            return ttf.getFontBBox();
        }

        override public int codeToCID(int code)
        {
            CMap cMap = parent.getCMap();

            // Acrobat allows bad PDFs to use Unicode CMaps here instead of CID CMaps, see PDFBOX-1283
            if (!cMap.hasCIDMappings() && cMap.hasUnicodeMappings())
            {
                return cMap.toUnicode(code).codePointAt(0); // actually: code -> CID
            }

            return cMap.toCID(code);
        }

        /**
         * Returns the GID for the given character code.
         *
         * @param code character code
         * @return GID
         * @throws IOException
         */
        override public int codeToGID(int code)
        {
            if (!isEmbedded)
            {
                // The conforming reader shall select glyphs by translating characters from the
                // encoding specified by the predefined CMap to one of the encodings in the TrueType
                // font's 'cmap' table. The means by which this is accomplished are implementation-
                // dependent.
                // omit the CID2GID mapping if the embedded font is replaced by an external font
                if (cid2gid != null && !isDamaged)
                {
                    // Acrobat allows non-embedded GIDs - todo: can we find a test PDF for this?
                    LOG.warn("Using non-embedded GIDs in font " + getName());
                    int cid = codeToCID(code);
                    return cid2gid[cid];
                }
                else
                {
                    // fallback to the ToUnicode CMap, test with PDFBOX-1422 and PDFBOX-2560
                    String unicode = parent.toUnicode(code);
                    if (unicode == null)
                    {
                        if (!noMapping.contains(code))
                        {
                            // we keep track of which warnings have been issued, so we don't log multiple times
                            noMapping.add(code);
                            LOG.warn("Failed to find a character mapping for " + code + " in " + getName());
                        }
                        // Acrobat is willing to use the CID as a GID, even when the font isn't embedded
                        // see PDFBOX-2599
                        return codeToCID(code);
                    }
                    else if (unicode.length() > 1)
                    {
                        LOG.warn("Trying to map multi-byte character using 'cmap', result will be poor");
                    }

                    // a non-embedded font always has a cmap (otherwise FontMapper won't load it)
                    return cmap.getGlyphId(unicode.codePointAt(0));
                }
            }
            else
            {
                // If the TrueType font program is embedded, the Type 2 CIDFont dictionary shall contain
                // a CIDToGIDMap entry that maps CIDs to the glyph indices for the appropriate glyph
                // descriptions in that font program.

                int cid = codeToCID(code);
                if (cid2gid != null)
                {
                    // use CIDToGIDMap
                    if (cid < cid2gid.length)
                    {
                        return cid2gid[cid];
                    }
                    else
                    {
                        return 0;
                    }
                }
                else
                {
                    // "Identity" is the default CIDToGIDMap
                    if (cid < ttf.getNumberOfGlyphs())
                    {
                        return cid;
                    }
                    else
                    {
                        // out of range CIDs map to GID 0
                        return 0;
                    }
                }
            }
        }

        override public float getHeight(int code)
        {
            // todo: really we want the BBox, (for text extraction:)
            return (ttf.getHorizontalHeader().getAscender() + -ttf.getHorizontalHeader().getDescender())
                    / ttf.getUnitsPerEm(); // todo: shouldn't this be the yMax/yMin?
        }

        override public float getWidthFromFont(int code)
        {
            int gid = codeToGID(code);
            int width = ttf.getAdvanceWidth(gid);
            int unitsPerEM = ttf.getUnitsPerEm();
            if (unitsPerEM != 1000)
            {
                width *= 1000f / unitsPerEM;
            }
            return width;
        }

        override public byte[] encode(int unicode)
        {
            int cid = -1;
            if (isEmbedded)
            {
                // embedded fonts always use CIDToGIDMap, with Identity as the default
                if (parent.getCMap().getName().startsWith("Identity-"))
                {
                    if (cmap != null)
                    {
                        cid = cmap.getGlyphId(unicode);
                    }
                }
                else
                {
                    // if the CMap is predefined then there will be a UCS-2 CMap
                    if (parent.getCMapUCS2() != null)
                    {
                        cid = parent.getCMapUCS2().toCID(unicode);
                    }
                }

                // otherwise we require an explicit ToUnicode CMap
                if (cid == -1)
                {
                    //TODO: invert the ToUnicode CMap?
                    // see also PDFBOX-4233
                    cid = 0;
                }
            }
            else
            {
                // a non-embedded font always has a cmap (otherwise it we wouldn't load it)
                cid = cmap.getGlyphId(unicode);
            }

            if (cid == 0)
            {
                throw new IllegalArgumentException(
                        String.format("No glyph for U+%04X (%c) in font %s", unicode, (char)unicode, getName()));
            }

            return encodeGlyphId(cid);
        }

        override public byte[] encodeGlyphId(int glyphId)
        {
            // CID is always 2-bytes (16-bit) for TrueType
            return new byte[] { (byte)(glyphId >> 8 & 0xff), (byte)(glyphId & 0xff) };
        }

        override public bool isEmbedded()
        {
            return isEmbedded;
        }

        override public bool isDamaged()
        {
            return isDamaged;
        }

        /**
         * Returns the embedded or substituted TrueType font. May be an OpenType font if the font is
         * not embedded.
         */
        public TrueTypeFont getTrueTypeFont()
        {
            return ttf;
        }

        override public SKPath getPath(int code)
        {
            if (ttf is OpenTypeFont && ((OpenTypeFont)ttf).isPostScript())
            {
                // we're not supposed to have CFF fonts inside PDCIDFontType2, but if we do,
                // then we treat their CIDs as GIDs, see PDFBOX-3344
                int cid = codeToGID(code);
                Type2CharString charstring = ((OpenTypeFont)ttf).getCFF().getFont().getType2CharString(cid);
                return charstring.getPath();
            }
            else
            {
                int gid = codeToGID(code);
                GlyphData glyph = ttf.getGlyph().getGlyph(gid);
                if (glyph != null)
                {
                    return glyph.getPath();
                }
                return new SKPath();
            }
        }

        override public SKPath getNormalizedPath(int code)
        {
            bool hasScaling = ttf.getUnitsPerEm() != 1000;
            float scale = 1000f / ttf.getUnitsPerEm();
            int gid = codeToGID(code);

            SKPath path = getPath(code);

            // Acrobat only draws GID 0 for embedded CIDFonts, see PDFBOX-2372
            if (gid == 0 && !isEmbedded())
            {
                path = null;
            }

            if (path == null)
            {
                // empty glyph (e.g. space, newline)
                return new SKPath();
            }
            else
            {
                if (hasScaling)
                {
                    path.transform(AffineTransform.getScaleInstance(scale, scale));
                }
                return path;
            }
        }

        override public bool hasGlyph(int code)
        {
            return codeToGID(code) != 0;
        }
    }
}
