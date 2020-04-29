/*
  Copyright 2009-2015 Stefano Chizzolini. http://www.pdfclown.org

  Contributors:
    * Stefano Chizzolini (original code developer, http://www.stefanochizzolini.it)

  This file should be part of the source code distribution of "PDF Clown library" (the
  Program): see the accompanying README files for more info.

  This Program is free software; you can redistribute it and/or modify it under the terms
  of the GNU Lesser General Public License as published by the Free Software Foundation;
  either version 3 of the License, or (at your option) any later version.

  This Program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY,
  either expressed or implied; without even the implied warranty of MERCHANTABILITY or
  FITNESS FOR A PARTICULAR PURPOSE. See the License for more details.

  You should have received a copy of the GNU Lesser General Public License along with this
  Program (see README files); if not, go to the GNU website (http://www.gnu.org/licenses/).

  Redistribution and use, with or without modification, are permitted provided that such
  redistributions retain the above copyright notice, license and disclaimer, along with
  this list of conditions.
*/

using PdfClown.Documents;
using PdfClown.Documents.Contents.Fonts.TTF;
using PdfClown.Files;
using PdfClown.Objects;
using PdfClown.Util;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace PdfClown.Documents.Contents.Fonts
{
    /**
      <summary>TrueType font [PDF:1.6:5;OFF:2009].</summary>
    */
    [PDF(VersionEnum.PDF10)]
    public sealed class PdfTrueTypeFont : SimpleFont
    {
        private static readonly int START_RANGE_F000 = 0xF000;
        private static readonly int START_RANGE_F100 = 0xF100;
        private static readonly int START_RANGE_F200 = 0xF200;

        private static readonly Dictionary<string, int> InvertedMacosRoman = new Dictionary<string, int>(250, StringComparer.Ordinal);
        static PdfTrueTypeFont()
        {
            foreach (var entry in MacOSRomanEncoding.Instance.CodeToNameMap)
            {
                if (!InvertedMacosRoman.ContainsKey(entry.Value))
                {
                    InvertedMacosRoman[entry.Value] = entry.Key;
                }
            }
        }

        private readonly TrueTypeFont ttf;
        private readonly bool isEmbedded;
        private readonly bool isDamaged;
        private CmapSubtable cmapWinUnicode = null;
        private CmapSubtable cmapWinSymbol = null;
        private CmapSubtable cmapMacRoman = null;
        private bool cmapInitialized = false;
        private Dictionary<int, int> gidToCode; // for embedding
        private SKRect? fontBBox;


        #region dynamic
        #region constructors
        internal PdfTrueTypeFont(PdfDirectObject baseObject)
            : base(baseObject)
        {
            TrueTypeFont ttfFont = null;
            bool fontIsDamaged = false;
            if (FontDescriptor != null)
            {
                var fd = base.FontDescriptor;
                var ff2Stream = fd.FontFile2;
                if (ff2Stream != null)
                {
                    try
                    {
                        // embedded
                        TTFParser ttfParser = new TTFParser(true);
                        ttfFont = ttfParser.Parse(ff2Stream.BaseDataObject.ExtractBody(true));
                    }
                    catch (IOException e)
                    {
                        Debug.WriteLine($"warn: Could not read embedded TTF for font {BaseFont} {e}");
                        fontIsDamaged = true;
                    }
                }
            }
            isEmbedded = ttfFont != null;
            isDamaged = fontIsDamaged;

            // substitute
            if (ttfFont == null)
            {
                FontMapping<TrueTypeFont> mapping = FontMappers.Instance.GetTrueTypeFont(BaseFont, FontDescriptor);
                ttfFont = mapping.Font;

                if (mapping.IsFallback)
                {
                    Debug.WriteLine($"warn: Using fallback font '{ttfFont}' for '{BaseFont}'");
                }
            }
            ttf = ttfFont;
            ReadEncoding();
        }

        /**
         * Creates a new TrueType font for embedding.
         */
        private PdfTrueTypeFont(Document document, TrueTypeFont ttf, Encoding encoding, bool closeTTF)
            : base(document)
        {
            PDTrueTypeFontEmbedder embedder = new PDTrueTypeFontEmbedder(document, Dictionary, ttf, encoding);
            this.encoding = encoding;
            this.ttf = ttf;
            FontDescriptor = embedder.FontDescriptor;
            isEmbedded = true;
            isDamaged = false;
            glyphList = GlyphMapping.Default;
            if (closeTTF)
            {
                // the TTF is fully loaded and it is safe to close the underlying data source
                ttf.Dispose();
            }
        }

        /**
         * Loads a TTF to be embedded into a document as a simple font.
         * 
         * <p><b>Note:</b> Simple fonts only support 256 characters. For Unicode support, use
         * {@link PDType0Font#load(Document, File)} instead.</p>
         *
         * @param doc The PDF document that will hold the embedded font.
         * @param file A TTF file.
         * @param encoding The PostScript encoding vector to be used for embedding.
         * @return a PdfTrueTypeFont instance.
         * @throws IOException If there is an error loading the data.
         */
        public static PdfTrueTypeFont Load(Document doc, string file, Encoding encoding)

        {
            return new PdfTrueTypeFont(doc, new TTFParser().Parse(file), encoding, true);
        }

        /**
         * Loads a TTF to be embedded into a document as a simple font.
         *
         * <p><b>Note:</b> Simple fonts only support 256 characters. For Unicode support, use
         * {@link PDType0Font#load(Document, Bytes.IInputStream)} instead.</p>
         * 
         * @param doc The PDF document that will hold the embedded font.
         * @param input A TTF file stream
         * @param encoding The PostScript encoding vector to be used for embedding.
         * @return a PdfTrueTypeFont instance.
         * @throws IOException If there is an error loading the data.
         */
        public static PdfTrueTypeFont Load(Document doc, Bytes.IInputStream input, Encoding encoding)

        {
            return new PdfTrueTypeFont(doc, new TTFParser().Parse(input), encoding, true);
        }

        /**
         * Loads a TTF to be embedded into a document as a simple font.
         *
         * <p>
         * <b>Note:</b> Simple fonts only support 256 characters. For Unicode support, use
         * {@link PDType0Font#load(Document, Bytes.IInputStream)} instead.
         * </p>
         * 
         * @param doc The PDF document that will hold the embedded font.
         * @param ttf A true type font
         * @param encoding The PostScript encoding vector to be used for embedding.
         * @return a PdfTrueTypeFont instance.
         * @throws IOException If there is an error loading the data.
         */
        public static PdfTrueTypeFont Load(Document doc, TrueTypeFont ttf, Encoding encoding)
        {
            return new PdfTrueTypeFont(doc, ttf, encoding, false);
        }


        protected override Encoding ReadEncodingFromFont()
        {
            if (!IsEmbedded && Standard14AFM != null)
            {
                // read from AFM
                return new Type1Encoding(Standard14AFM);
            }
            else
            {
                // non-symbolic fonts don't have a built-in encoding per se, but there encoding is
                // assumed to be StandardEncoding by the PDF spec unless an explicit Encoding is present
                // which will override this anyway
                if (!(SymbolicFlag ?? true))
                {
                    return StandardEncoding.Instance;
                }

                // normalise the standard 14 name, e.g "Symbol,Italic" -> "Symbol"
                string standard14Name = Standard14Fonts.GetMappedFontName(Name);

                // likewise, if the font is standard 14 then we know it's Standard Encoding
                if (IsStandard14 &&
                    !standard14Name.Equals("Symbol", StringComparison.Ordinal) &&
                    !standard14Name.Equals("ZapfDingbats", StringComparison.Ordinal))
                {
                    return StandardEncoding.Instance;
                }

                // synthesize an encoding, so that getEncoding() is always usable
                PostScriptTable post = ttf.PostScript;
                Dictionary<int, string> codeToName = new Dictionary<int, string>();
                for (int code = 0; code <= 256; code++)
                {
                    int gid = CodeToGID(code);
                    if (gid > 0)
                    {
                        string name = null;
                        if (post != null)
                        {
                            name = post.GetName(gid);
                        }
                        if (name == null)
                        {
                            // GID pseudo-name
                            name = gid.ToString();
                        }
                        codeToName[code] = name;
                    }
                }
                return new BuiltInEncoding(codeToName);
            }
        }


        public override int ReadCode(Bytes.IInputStream input, out byte[] bytes)
        {
            bytes = new byte[1] { (byte)input.ReadByte() };
            return bytes[0];
        }


        public override string Name
        {
            get => BaseFont;
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
                Rectangle bbox = FontDescriptor.FontBBox;
                if (bbox != null)
                {
                    return bbox.ToRect();
                }
            }
            return ttf.FontBBox;
        }


        public override bool IsDamaged
        {
            get => isDamaged;
        }

        /**
         * Returns the embedded or substituted TrueType font.
         */
        public TrueTypeFont TrueTypeFont
        {
            get => ttf;
        }


        public override float GetWidthFromFont(int code)
        {
            int gid = CodeToGID(code);
            float width = ttf.GetAdvanceWidth(gid);
            float unitsPerEM = ttf.UnitsPerEm;
            if (unitsPerEM.CompareTo(1000) != 0)
            {
                width *= 1000f / unitsPerEM;
            }
            return width;
        }


        public override float GetHeight(int code)
        {
            int gid = CodeToGID(code);
            GlyphData glyph = ttf.Glyph.GetGlyph(gid);
            if (glyph != null)
            {
                return glyph.BoundingBox.Height;
            }
            return 0;
        }

        public override byte[] Encode(int unicode)
        {
            if (encoding != null)
            {
                if (!encoding.Contains(GlyphList.UnicodeToName(unicode)))
                {
                    throw new ArgumentException($"U+{unicode:x4} is not available in this font's encoding: {encoding.GetPdfObject()}");
                }

                string name = GlyphList.UnicodeToName(unicode);
                Dictionary<string, int> inverted = encoding.NameToCodeMap;

                if (!ttf.HasGlyph(name))
                {
                    // try unicode name
                    string uniName = UniUtil.GetUniNameOfCodePoint(unicode);
                    if (!ttf.HasGlyph(uniName))
                    {
                        throw new ArgumentException($"No glyph for U+{unicode:x4} in font {Name}");
                    }
                }

                inverted.TryGetValue(name, out int code);
                return new byte[] { (byte)code };
            }
            else
            {
                // use TTF font's built-in encoding
                string name = GlyphList.UnicodeToName(unicode);

                if (!ttf.HasGlyph(name))
                {
                    throw new ArgumentException($"No glyph for U+{unicode:x4} in font {Name}");
                }

                int gid = ttf.NameToGID(name);
                if (!GIDToCode.TryGetValue(gid, out int code))
                {
                    throw new ArgumentException($"U+{unicode:x4} is not available in this font's Encoding");
                }

                return new byte[] { (byte)(int)code };
            }
        }

        /**
         * Inverts the font's code -&gt; GID mapping. Any duplicate (GID -&gt; code) mappings will be lost.
         */
        private Dictionary<int, int> GIDToCode
        {
            get
            {
                if (gidToCode != null)
                {
                    return gidToCode;
                }

                gidToCode = new Dictionary<int, int>();
                for (int code = 0; code <= 255; code++)
                {
                    int gid = CodeToGID(code);
                    if (!gidToCode.ContainsKey(gid))
                    {
                        gidToCode[gid] = code;
                    }
                }
                return gidToCode;
            }
        }

        public override bool IsEmbedded
        {
            get => isEmbedded;
        }

        public override SKPath GetPath(int code)
        {
            int gid = CodeToGID(code);
            GlyphData glyph = ttf.Glyph.GetGlyph(gid);

            // some glyphs have no outlines (e.g. space, table, newline)
            if (glyph == null)
            {
                return null;
            }
            else
            {
                return glyph.GetPath();
            }
        }

        public override SKPath GetPath(string name)
        {
            // handle glyph names and uniXXXX names
            int gid = ttf.NameToGID(name);
            if (gid == 0)
            {
                try
                {
                    // handle GID pseudo-names
                    gid = int.Parse(name);
                    if (gid > ttf.NumberOfGlyphs)
                    {
                        gid = 0;
                    }
                }
                catch (Exception e)
                {
                    gid = 0;
                }
            }
            // I'm assuming .notdef paths are not drawn, as it PDFBOX-2421
            if (gid == 0)
            {
                return null;
            }

            GlyphData glyph = ttf.Glyph.GetGlyph(gid);
            if (glyph != null)
            {
                return glyph.GetPath();
            }
            else
            {
                return null;
            }
        }


        public override SKPath GetNormalizedPath(int code)
        {
            if (!cacheGlyphs.TryGetValue(code, out SKPath path))
            {
                bool hasScaling = ttf.UnitsPerEm != 1000;
                float scale = 1000f / ttf.UnitsPerEm;
                int gid = CodeToGID(code);

                path = GetPath(code);

                // Acrobat only draws GID 0 for embedded or "Standard 14" fonts, see PDFBOX-2372
                if (gid == 0 && !IsEmbedded && !IsStandard14)
                {
                    path = null;
                }
                //check empty glyph (e.g. space, newline)
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


        public override bool HasGlyph(string name)
        {
            int gid = ttf.NameToGID(name);
            return !(gid == 0 || gid >= ttf.MaximumProfile.NumGlyphs);
        }


        public override BaseFont Font
        {
            get => ttf;
        }


        public override bool HasGlyph(int code)
        {
            return CodeToGID(code) != 0;
        }

        /**
         * Returns the GID for the given character code.
         *
         * @param code character code
         * @return GID (glyph index)
         * @throws java.io.IOException
         */
        public int CodeToGID(int code)
        {
            ExtractCmapTable();
            int gid = 0;

            if (!Symbolic) // non-symbolic
            {
                string name = encoding.GetName(code);
                if (".notdef".Equals(name, StringComparison.Ordinal))
                {
                    return 0;
                }
                else
                {
                    // (3, 1) - (Windows, Unicode)
                    if (cmapWinUnicode != null)
                    {
                        var unicode = GlyphMapping.Default.ToUnicode(name);
                        if (unicode != null)
                        {
                            //int uni = unicode.codePointAt(0);
                            gid = cmapWinUnicode.GetGlyphId((int)unicode);
                        }
                    }

                    // (1, 0) - (Macintosh, Roman)
                    if (gid == 0 && cmapMacRoman != null)
                    {
                        if (InvertedMacosRoman.TryGetValue(name, out int macCode))
                        {
                            gid = cmapMacRoman.GetGlyphId(macCode);
                        }
                    }

                    // 'post' table
                    if (gid == 0)
                    {
                        gid = ttf.NameToGID(name);
                    }
                }
            }
            else // symbolic
            {
                // (3, 0) - (Windows, Symbol)
                if (cmapWinSymbol != null)
                {
                    gid = cmapWinSymbol.GetGlyphId(code);
                    if (code >= 0 && code <= 0xFF)
                    {
                        // the CMap may use one of the following code ranges,
                        // so that we have to add the high byte to get the
                        // mapped value
                        if (gid == 0)
                        {
                            // F000 - F0FF
                            gid = cmapWinSymbol.GetGlyphId(code + START_RANGE_F000);
                        }
                        if (gid == 0)
                        {
                            // F100 - F1FF
                            gid = cmapWinSymbol.GetGlyphId(code + START_RANGE_F100);
                        }
                        if (gid == 0)
                        {
                            // F200 - F2FF
                            gid = cmapWinSymbol.GetGlyphId(code + START_RANGE_F200);
                        }
                    }
                }

                // (1, 0) - (Mac, Roman)
                if (gid == 0 && cmapMacRoman != null)
                {
                    gid = cmapMacRoman.GetGlyphId(code);
                }

                // PDFBOX-4755 / PDF.js #5501
                if (gid == 0 && cmapWinUnicode != null)
                {
                    gid = cmapWinUnicode.GetGlyphId(code);
                }

                // PDFBOX-3965: fallback for font has that the symbol flag but isn't
                if (gid == 0 && cmapWinUnicode != null && encoding != null)
                {
                    string name = encoding.GetName(code);
                    if (".notdef".Equals(name, StringComparison.Ordinal))
                    {
                        return 0;
                    }
                    var unicode = GlyphMapping.Default.ToUnicode(name);
                    if (unicode != null)
                    {
                        //int uni = unicode.codePointAt(0);
                        gid = cmapWinUnicode.GetGlyphId((int)unicode);
                    }
                }
            }

            return gid;
        }

        /**
         * extract all useful "cmap" subtables.
         */
        private void ExtractCmapTable()
        {
            if (cmapInitialized)
            {
                return;
            }

            CmapTable cmapTable = ttf.Cmap;
            if (cmapTable != null)
            {
                // get all relevant "cmap" subtables
                CmapSubtable[] cmaps = cmapTable.Cmaps;
                foreach (CmapSubtable cmap in cmaps)
                {
                    if (CmapTable.PLATFORM_WINDOWS == cmap.PlatformId)
                    {
                        if (CmapTable.ENCODING_WIN_UNICODE_BMP == cmap.PlatformEncodingId)
                        {
                            cmapWinUnicode = cmap;
                        }
                        else if (CmapTable.ENCODING_WIN_SYMBOL == cmap.PlatformEncodingId)
                        {
                            cmapWinSymbol = cmap;
                        }
                    }
                    else if (CmapTable.PLATFORM_MACINTOSH == cmap.PlatformId
                            && CmapTable.ENCODING_MAC_ROMAN == cmap.PlatformEncodingId)
                    {
                        cmapMacRoman = cmap;
                    }
                    else if (CmapTable.PLATFORM_UNICODE == cmap.PlatformId
                            && CmapTable.ENCODING_UNICODE_1_0 == cmap.PlatformEncodingId)
                    {
                        // PDFBOX-4755 / PDF.js #5501
                        cmapWinUnicode = cmap;
                    }
                }
            }
            cmapInitialized = true;
        }
        #endregion
        #endregion
    }
}