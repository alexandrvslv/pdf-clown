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
using PdfClown.Documents.Contents.Fonts.CCF;
using PdfClown.Documents.Contents.Fonts.TTF;
using PdfClown.Objects;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

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
        private readonly ICmapLookup cmapLookup; // may be null
        private SKMatrix? fontMatrix;
        private SKRect? fontBBox;
        private readonly HashSet<int> noMapping = new HashSet<int>();

        public CIDFontType2(Document document, PdfDictionary fontObject) : base(document, fontObject)
        {
        }

        internal CIDFontType2(PdfDirectObject fontObject)
            : base(fontObject)
        {
        }

        /**
         * Constructor.
         * 
         * @param fontDictionary The font dictionary according to the PDF specification.
         * @param parent The parent font.
         * @throws IOException
         */
        public CIDFontType2(PdfDirectObject fontDictionary, PdfType0Font parent)
            : this(fontDictionary, parent, null)
        {
        }

        /**
         * Constructor.
         * 
         * @param fontDictionary The font dictionary according to the PDF specification.
         * @param parent The parent font.
         * @param trueTypeFont The true type font used to create the parent font
         * @throws IOException
         */
        public CIDFontType2(PdfDirectObject fontDictionary, PdfType0Font parent, TrueTypeFont trueTypeFont)
            : base(fontDictionary, parent)
        {

            FontDescriptor fd = FontDescriptor;
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

                FontFile stream = null;
                if (fd != null)
                {
                    stream = fd.FontFile2;
                    if (stream == null)
                    {
                        stream = fd.FontFile3;
                    }
                    if (stream == null)
                    {
                        // Acrobat looks in FontFile too, even though it is not in the spec, see PDFBOX-2599
                        stream = fd.FontFile;
                    }
                }
                if (stream != null)
                {
                    try
                    {
                        // embedded OTF or TTF
                        OTFParser otfParser = new OTFParser(true);
                        OpenTypeFont otf = otfParser.Parse((Bytes.Buffer)stream.BaseDataObject.ExtractBody(true));
                        ttfFont = otf;

                        if (otf.IsPostScript)
                        {
                            // PDFBOX-3344 contains PostScript outlines instead of TrueType
                            fontIsDamaged = true;
                            Debug.WriteLine($"warning: Found CFF/OTF but expected embedded TTF font {fd.FontName}");
                        }
                    }
                    catch (IOException e)
                    {
                        fontIsDamaged = true;
                        Debug.WriteLine($"warning: Could not read embedded OTF for font {BaseFont} {e}");
                    }
                }
                isEmbedded = ttfFont != null;
                isDamaged = fontIsDamaged;

                if (ttfFont == null)
                {
                    ttfFont = FindFontOrSubstitute();
                }
                ttf = ttfFont;
            }
            cmapLookup = ttf.GetUnicodeCmapLookup(false);
            cid2gid = ReadCIDToGIDMap();
        }

        private TrueTypeFont FindFontOrSubstitute()
        {
            TrueTypeFont ttfFont;

            CIDFontMapping mapping = FontMappers.Instance.GetCIDFont(BaseFont, FontDescriptor, CIDSystemInfo);
            if (mapping.IsCIDFont)
            {
                ttfFont = mapping.Font;
            }
            else
            {
                ttfFont = (TrueTypeFont)mapping.TrueTypeFont;
            }
            if (mapping.IsFallback)
            {
                Debug.WriteLine($"warning: Using fallback font {ttfFont.Name} for CID-keyed TrueType font {BaseFont}");
            }
            return ttfFont;
        }

        public override bool IsEmbedded
        {
            get => isEmbedded;
        }

        public override bool IsDamaged
        {
            get => isDamaged;
        }

        /**
         * Returns the embedded or substituted TrueType font. May be an OpenType font if the font is
         * not embedded.
         */
        public TrueTypeFont TrueTypeFont
        {
            get => ttf;
        }

        public override SKMatrix FontMatrix
        {
            get
            {
                if (fontMatrix == null)
                {
                    // 1000 upem, this is not strictly true
                    fontMatrix = new SKMatrix(0.001f, 0, 0, 0, 0.001f, 0, 0, 0, 1);
                }
                return (SKMatrix)fontMatrix;
            }
        }

        public override SKRect BoundingBox
        {
            get
            {
                if (fontBBox == null)
                {
                    fontBBox = GenerateBoundingBox();
                }
                return (SKRect)fontBBox;
            }
        }

        private SKRect GenerateBoundingBox()
        {
            if (FontDescriptor != null)
            {
                var bbox = FontDescriptor.FontBBox;
                if (bbox != null &&
                        (bbox.Left.CompareTo(0) != 0 ||
                         bbox.Bottom.CompareTo(0) != 0 ||
                         bbox.Right.CompareTo(0) != 0 ||
                         bbox.Top.CompareTo(0) != 0))
                {
                    return bbox.ToRect();
                }
            }
            return ttf.FontBBox;
        }

        public override int CodeToCID(int code)
        {
            CMap cMap = parent.CMap;

            // Acrobat allows bad PDFs to use Unicode CMaps here instead of CID CMaps, see PDFBOX-1283
            if (!cMap.HasCIDMappings && cMap.HasUnicodeMappings)
            {
                return cMap.ToUnicode(code); // actually: code -> CID
            }

            return cMap.ToCID(code);
        }

        /**
         * Returns the GID for the given character code.
         *
         * @param code character code
         * @return GID
         * @throws IOException
         */
        public override int CodeToGID(int code)
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
                    Debug.WriteLine("warn: Using non-embedded GIDs in font " + Name);
                    int cid = CodeToCID(code);
                    return cid2gid[cid];
                }
                else
                {
                    // fallback to the ToUnicode CMap, test with PDFBOX-1422 and PDFBOX-2560
                    var unicode = parent.ToUnicode(code);
                    if (unicode < 0)
                    {
                        if (!noMapping.Contains(code))
                        {
                            // we keep track of which warnings have been issued, so we don't log multiple times
                            noMapping.Add(code);
                            Debug.WriteLine($"warn: Failed to find a character mapping for {code} in {Name}");
                        }
                        // Acrobat is willing to use the CID as a GID, even when the font isn't embedded
                        // see PDFBOX-2599
                        return CodeToCID(code);
                    }
                    else if (unicode > char.MaxValue)
                    {
                        Debug.WriteLine("warn: Trying to map multi-byte character using 'cmap', result will be poor");
                    }

                    // a non-embedded font always has a cmap (otherwise FontMapper won't load it)
                    return cmapLookup.GetGlyphId(unicode);
                }
            }
            else
            {
                // If the TrueType font program is embedded, the Type 2 CIDFont dictionary shall contain
                // a CIDToGIDMap entry that maps CIDs to the glyph indices for the appropriate glyph
                // descriptions in that font program.

                int cid = CodeToCID(code);
                if (cid2gid != null)
                {
                    // use CIDToGIDMap
                    if (cid < cid2gid.Length)
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
                    if (cid < ttf.NumberOfGlyphs)
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

        override public float GetHeight(int code)
        {
            // todo: really we want the BBox, (for text extraction:)
            return (ttf.HorizontalHeader.Ascender + -ttf.HorizontalHeader.Descender);
            /// ttf.UnitsPerEm; // todo: shouldn't this be the yMax/yMin?
        }

        override public float GetWidthFromFont(int code)
        {
            int gid = CodeToGID(code);
            float width = ttf.GetAdvanceWidth(gid);
            int unitsPerEM = ttf.UnitsPerEm;
            if (unitsPerEM != 1000)
            {
                width *= 1000f / unitsPerEM;
            }
            return width;
        }

        public override int ReadCode(Bytes.IInputStream input, out byte[] bytes)
        {
            throw new NotImplementedException();
        }

        public override byte[] Encode(int unicode)
        {
            int cid = -1;
            if (isEmbedded)
            {
                // embedded fonts always use CIDToGIDMap, with Identity as the default
                if (parent.CMap.CMapName.StartsWith("Identity-", StringComparison.Ordinal))
                {
                    if (cmapLookup != null)
                    {
                        cid = cmapLookup.GetGlyphId(unicode);
                    }
                }
                else
                {
                    // if the CMap is predefined then there will be a UCS-2 CMap
                    if (parent.CMapUCS2 != null)
                    {
                        cid = parent.CMapUCS2.ToCID(unicode);
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
                cid = cmapLookup.GetGlyphId(unicode);
            }

            if (cid == 0)
            {
                throw new ArgumentException($"No glyph for U+{unicode:x4} ({(char)unicode}) in font {Name}");
            }

            return EncodeGlyphId(cid);
        }

        public override byte[] EncodeGlyphId(int glyphId)
        {
            // CID is always 2-bytes (16-bit) for TrueType
            return new byte[] { (byte)(glyphId >> 8 & 0xff), (byte)(glyphId & 0xff) };
        }

        public override SKPath GetPath(int code)
        {
            if (ttf is OpenTypeFont otf && otf.IsPostScript)
            {
                // we're not supposed to have CFF fonts inside PDCIDFontType2, but if we do,
                // then we treat their CIDs as GIDs, see PDFBOX-3344
                int cid = CodeToGID(code);
                Type2CharString charstring = otf.CFF.Font.GetType2CharString(cid);
                return charstring.Path;
            }
            else
            {
                int gid = CodeToGID(code);
                GlyphData glyph = ttf.Glyph.GetGlyph(gid);
                if (glyph != null)
                {
                    return glyph.GetPath();
                }
                return null;
            }
        }

        override public SKPath GetNormalizedPath(int code)
        {
            if (!cacheGlyphs.TryGetValue(code, out SKPath path))
            {
                bool hasScaling = ttf.UnitsPerEm != 1000;
                float scale = 1000f / ttf.UnitsPerEm;
                int gid = CodeToGID(code);

                path = GetPath(code);

                // Acrobat only draws GID 0 for embedded CIDFonts, see PDFBOX-2372
                if (gid == 0 && !IsEmbedded)
                {
                    path = null;
                }

                // empty glyph (e.g. space, newline)
                if (path != null && hasScaling)
                {
                    var scaledPath = new SKPath(path);
                    scaledPath.Transform(SKMatrix.MakeScale(scale, scale));
                    path = scaledPath;
                }
                cacheGlyphs[code] = path;
            }
            return path;
        }

        public override bool HasGlyph(int code)
        {
            return CodeToGID(code) != 0;
        }
    }
}
