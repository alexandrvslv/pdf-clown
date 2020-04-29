/*
  Copyright 2007-2015 Stefano Chizzolini. http://www.pdfclown.org

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

using PdfClown.Bytes;
using PdfClown.Documents;
using PdfClown.Objects;
using PdfClown.Util;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using text = System.Text;
using System.Text.RegularExpressions;
using SkiaSharp;
using PdfClown.Documents.Contents.Fonts.Type1;
using System.Diagnostics;

namespace PdfClown.Documents.Contents.Fonts
{
    /**
      <summary>Type 1 font [PDF:1.6:5.5.1;AFM:4.1].</summary>
    */
    /*
      NOTE: Type 1 fonts encompass several formats:
      * AFM+PFB;
      * CFF;
      * OpenFont/CFF (in case "CFF" table's Top DICT has no CIDFont operators).
    */
    [PDF(VersionEnum.PDF10)]
    public class PdfType1Font : SimpleFont
    {
        public static readonly PdfType1Font TIMES_ROMAN = new PdfType1Font(null, "Times-Roman");
        public static readonly PdfType1Font TIMES_BOLD = new PdfType1Font(null, "Times-Bold");
        public static readonly PdfType1Font TIMES_ITALIC = new PdfType1Font(null, "Times-Italic");
        public static readonly PdfType1Font TIMES_BOLD_ITALIC = new PdfType1Font(null, "Times-BoldItalic");
        public static readonly PdfType1Font HELVETICA = new PdfType1Font(null, "Helvetica");
        public static readonly PdfType1Font HELVETICA_BOLD = new PdfType1Font(null, "Helvetica-Bold");
        public static readonly PdfType1Font HELVETICA_OBLIQUE = new PdfType1Font(null, "Helvetica-Oblique");
        public static readonly PdfType1Font HELVETICA_BOLD_OBLIQUE = new PdfType1Font(null, "Helvetica-BoldOblique");
        public static readonly PdfType1Font COURIER = new PdfType1Font(null, "Courier");
        public static readonly PdfType1Font COURIER_BOLD = new PdfType1Font(null, "Courier-Bold");
        public static readonly PdfType1Font COURIER_OBLIQUE = new PdfType1Font(null, "Courier-Oblique");
        public static readonly PdfType1Font COURIER_BOLD_OBLIQUE = new PdfType1Font(null, "Courier-BoldOblique");
        public static readonly PdfType1Font SYMBOL = new PdfType1Font(null, "Symbol");
        public static readonly PdfType1Font ZAPF_DINGBATS = new PdfType1Font(null, "ZapfDingbats");
        public enum FamilyEnum
        {
            Courier,
            Helvetica,
            Times,
            Symbol,
            ZapfDingbats
        };

        public static PdfType1Font Load(Document context, FamilyEnum family, bool bold, bool italic)
        {
            string fontName = family.ToString();
            switch (family)
            {
                case (FamilyEnum.Symbol):
                case (FamilyEnum.ZapfDingbats):
                    break;
                case (FamilyEnum.Times):
                    if (bold)
                    {
                        fontName += "-Bold";
                        if (italic)
                        { fontName += "Italic"; }
                    }
                    else if (italic)
                    { fontName += "-Italic"; }
                    else
                    { fontName += "-Roman"; }
                    break;
                default:
                    if (bold)
                    {
                        fontName += "-Bold";
                        if (italic)
                        { fontName += "Oblique"; }
                    }
                    else if (italic)
                    { fontName += "-Oblique"; }
                    break;
            }


            return new PdfType1Font(context, fontName);
        }
        // alternative names for glyphs which are commonly encountered
        private static readonly Dictionary<string, string> ALT_NAMES = new Dictionary<string, string>(StringComparer.Ordinal);
        private static readonly int PFB_START_MARKER = 0x80;
        static PdfType1Font()
        {
            ALT_NAMES.Add("ff", "f_f");
            ALT_NAMES.Add("ffi", "f_f_i");
            ALT_NAMES.Add("ffl", "f_f_l");
            ALT_NAMES.Add("fi", "f_i");
            ALT_NAMES.Add("fl", "f_l");
            ALT_NAMES.Add("st", "s_t");
            ALT_NAMES.Add("IJ", "I_J");
            ALT_NAMES.Add("ij", "i_j");
            ALT_NAMES.Add("ellipsis", "elipsis"); // misspelled in ArialMT
        }
        #region dynamic
        #region fields
        private readonly Type1Font type1font;
        private readonly BaseFont genericFont;
        private readonly bool isEmbedded;
        private readonly bool isDamaged;
        private readonly SKMatrix fontMatrixTransform;
        private readonly Dictionary<int, byte[]> codeToBytesMap;
        private SKMatrix? fontMatrix;
        private SKRect? fontBBox;
        #endregion

        #region constructors
        internal PdfType1Font(Document context) : base(context)
        { }

        internal PdfType1Font(PdfDirectObject baseObject) : base(baseObject)
        {
            codeToBytesMap = new Dictionary<int, byte[]>();

            var fd = FontDescriptor;
            Type1Font t1 = null;
            bool fontIsDamaged = false;
            if (fd != null)
            {
                // a Type1 font may contain a Type1C font
                var fontFile3 = fd.FontFile3;
                if (fontFile3 != null)
                {
                    throw new ArgumentException("Use PDType1CFont for FontFile3");
                }

                // or it may contain a PFB
                var fontFile = fd.FontFile;
                if (fontFile != null)
                {
                    try
                    {
                        var stream = fontFile.BaseDataObject;
                        int length1 = fontFile.Length1;
                        int length2 = fontFile.Length2;

                        // repair Length1 and Length2 if necessary
                        byte[] bytes = stream.ExtractBody(true).GetBuffer();
                        length1 = RepairLength1(bytes, length1);
                        length2 = RepairLength2(bytes, length1, length2);

                        if (bytes.Length > 0 && (bytes[0] & 0xff) == PFB_START_MARKER)
                        {
                            // some bad files embed the entire PFB, see PDFBOX-2607
                            t1 = Type1Font.CreateWithPFB(bytes);
                        }
                        else
                        {
                            // the PFB embedded as two segments back-to-back
                            byte[] segment1 = new byte[length1];
                            Array.Copy(bytes, 0, segment1, 0, length1);

                            byte[] segment2 = new byte[length2];
                            Array.Copy(bytes, length1, segment2, 0, length2);

                            // empty streams are simply ignored
                            if (length1 > 0 && length2 > 0)
                            {
                                t1 = Type1Font.CreateWithSegments(segment1, segment2);
                            }
                        }
                    }
                    catch (DamagedFontException e)
                    {
                        Debug.WriteLine($"warn: Can't read damaged embedded Type1 font {fd.FontName} {e}");
                        fontIsDamaged = true;
                    }
                    catch (IOException e)
                    {
                        Debug.WriteLine($"error: Can't read the embedded Type1 font {fd.FontName} {e}");
                        fontIsDamaged = true;
                    }
                }
            }
            isEmbedded = t1 != null;
            isDamaged = fontIsDamaged;
            type1font = t1;

            // find a generic font to use for rendering, could be a .pfb, but might be a .ttf
            if (type1font != null)
            {
                genericFont = type1font;
            }
            else
            {
                FontMapping<BaseFont> mapping = FontMappers.Instance.GetBaseFont(BaseFont, fd);
                genericFont = mapping.Font;

                if (mapping.IsFallback)
                {
                    Debug.WriteLine($"warn Using fallback font {genericFont.Name} for {BaseFont}");
                }
            }
            ReadEncoding();
            fontMatrixTransform = FontMatrix;
            fontMatrixTransform.SetScaleTranslate(1000, 1000, 0, 0);
        }

        public PdfType1Font(Document context, string baseFont) : base(context, baseFont)
        {
            Dictionary[PdfName.Subtype] = PdfName.Type1;
            Dictionary[PdfName.BaseFont] = PdfName.Get(baseFont);
            switch (baseFont)
            {
                case "ZapfDingbats":
                    encoding = ZapfDingbatsEncoding.Instance;
                    break;
                case "Symbol":
                    encoding = SymbolEncoding.Instance;
                    break;
                default:
                    encoding = WinAnsiEncoding.Instance;
                    Dictionary[PdfName.Encoding] = PdfName.WinAnsiEncoding;
                    break;
            }

            // standard 14 fonts may be accessed concurrently, as they are singletons
            codeToBytesMap = new Dictionary<int, byte[]>();

            // todo: could load the PFB font here if we wanted to support Standard 14 embedding
            type1font = null;
            FontMapping<BaseFont> mapping = FontMappers.Instance.GetBaseFont(BaseFont, FontDescriptor);
            genericFont = mapping.Font;

            if (mapping.IsFallback)
            {
                string fontName;
                try
                {
                    fontName = genericFont.Name;
                }
                catch (IOException e)
                {
                    Debug.WriteLine($"debug: Couldn't get font name - setting to '?' {e}");
                    fontName = "?";
                }
                Debug.WriteLine($"warn: Using fallback font {fontName} for base font {BaseFont}");
            }
            isEmbedded = false;
            isDamaged = false;
            fontMatrixTransform = SKMatrix.MakeIdentity();
        }

        public PdfType1Font(Document doc, Bytes.IInputStream pfbIn) : base(doc)
        {
            PdfType1FontEmbedder embedder = new PdfType1FontEmbedder(doc, Dictionary, pfbIn, null);
            encoding = embedder.FontEncoding;
            glyphList = embedder.GlyphList;
            type1font = embedder.Type1Font;
            genericFont = embedder.Type1Font;
            isEmbedded = true;
            isDamaged = false;
            fontMatrixTransform = SKMatrix.MakeIdentity();
            codeToBytesMap = new Dictionary<int, byte[]>();
        }

        public PdfType1Font(Document doc, Bytes.IInputStream pfbIn, Encoding encoding) : base(doc)
        {
            PdfType1FontEmbedder embedder = new PdfType1FontEmbedder(doc, Dictionary, pfbIn, encoding);
            this.encoding = encoding;
            glyphList = embedder.GlyphList;
            type1font = embedder.Type1Font;
            genericFont = embedder.Type1Font;
            isEmbedded = true;
            isDamaged = false;
            fontMatrixTransform = SKMatrix.MakeIdentity();
            codeToBytesMap = new Dictionary<int, byte[]>();
        }

        #endregion

        /**
         * Some Type 1 fonts have an invalid Length1, which causes the binary segment of the font
         * to be truncated, see PDFBOX-2350, PDFBOX-3677.
         *
         * @param bytes Type 1 stream bytes
         * @param length1 Length1 from the Type 1 stream
         * @return repaired Length1 value
         */
        private int RepairLength1(byte[] bytes, int length1)
        {
            // scan backwards from the end of the first segment to find 'exec'
            int offset = Math.Max(0, length1 - 4);
            if (offset <= 0 || offset > bytes.Length - 4)
            {
                offset = bytes.Length - 4;
            }

            offset = FindBinaryOffsetAfterExec(bytes, offset);
            if (offset == 0 && length1 > 0)
            {
                // 2nd try with brute force
                offset = FindBinaryOffsetAfterExec(bytes, bytes.Length - 4);
            }

            if (length1 - offset != 0 && offset > 0)
            {
                Debug.WriteLine($"warn: Ignored invalid Length1 {length1} for Type 1 font {Name}");
                return offset;
            }

            return length1;
        }

        private static int FindBinaryOffsetAfterExec(byte[] bytes, int startOffset)
        {
            int offset = startOffset;
            while (offset > 0)
            {
                if (bytes[offset + 0] == 'e'
                        && bytes[offset + 1] == 'x'
                        && bytes[offset + 2] == 'e'
                        && bytes[offset + 3] == 'c')
                {
                    offset += 4;
                    // skip additional CR LF space characters
                    while (offset < bytes.Length &&
                            (bytes[offset] == '\r' || bytes[offset] == '\n' ||
                             bytes[offset] == ' ' || bytes[offset] == '\t'))
                    {
                        offset++;
                    }
                    break;
                }
                offset--;
            }
            return offset;
        }

        /**
         * Some Type 1 fonts have an invalid Length2, see PDFBOX-3475. A negative /Length2 brings an
         * ArgumentException in Arrays.copyOfRange(), a huge value eats up memory because of
         * padding.
         *
         * @param bytes Type 1 stream bytes
         * @param length1 Length1 from the Type 1 stream
         * @param length2 Length2 from the Type 1 stream
         * @return repaired Length2 value
         */
        private int RepairLength2(byte[] bytes, int length1, int length2)
        {
            // repair Length2 if necessary
            if (length2 < 0 || length2 > bytes.Length - length1)
            {
                Debug.WriteLine($"warn: Ignored invalid Length2 {length2} for Type 1 font {Name}");
                return bytes.Length - length1;
            }
            return length2;
        }

        public override float GetHeight(int code)
        {
            string name = CodeToName(code);
            if (Standard14AFM != null)
            {
                string afmName = Encoding.GetName(code);
                return Standard14AFM.GetCharacterHeight(afmName); // todo: isn't this the y-advance, not the height?
            }
            else
            {
                // todo: should be scaled by font matrix
                return (float)genericFont.GetPath(name).Bounds.Height;
            }
        }

        public override byte[] Encode(int unicode)
        {
            if (codeToBytesMap.TryGetValue(unicode, out byte[] bytes))
            {
                return bytes;
            }

            string name = GlyphList.UnicodeToName(unicode);
            if (IsStandard14)
            {
                // genericFont not needed, thus simplified code
                // this is important on systems with no installed fonts
                if (!encoding.Contains(name))
                {
                    throw new ArgumentException(
                            $"U+{unicode:x4} ('{name}') is not available in this font {Name} encoding: {encoding.GetPdfObject()}");
                }
                if (".notdef".Equals(name, StringComparison.Ordinal))
                {
                    throw new ArgumentException(
                            $"No glyph for U+{unicode:x4} in font {Name}");
                }
            }
            else
            {
                if (!encoding.Contains(name))
                {
                    throw new ArgumentException(
                            string.Format("U+%04X ('%s') is not available in this font %s (generic: %s) encoding: %s",
                                    unicode, name, Name, genericFont.Name, encoding.GetPdfObject()));
                }

                string nameInFont = GetNameInFont(name);

                if (".notdef".Equals(nameInFont, StringComparison.Ordinal) || !genericFont.HasGlyph(nameInFont))
                {
                    throw new ArgumentException(
                            $"No glyph for U+{unicode:x4} in font {Name} (generic: {genericFont.Name})");
                }
            }

            Dictionary<string, int> inverted = encoding.NameToCodeMap;
            int code = inverted.TryGetValue(name, out var nameCode) ? nameCode : 0;
            bytes = new byte[] { (byte)code };
            codeToBytesMap[unicode] = bytes;
            return bytes;
        }


        public override float GetWidthFromFont(int code)
        {
            string name = CodeToName(code);

            // width of .notdef is ignored for substitutes, see PDFBOX-1900
            if (!isEmbedded && name.Equals(".notdef", StringComparison.Ordinal))
            {
                return 250;
            }
            float width = genericFont.GetWidth(name);

            var p = fontMatrixTransform.MapVector(width, 0);
            return p.X;
        }

        public override bool IsEmbedded
        {
            get => isEmbedded;
        }

        public override float AverageFontWidth
        {
            get
            {
                if (Standard14AFM != null)
                {
                    return Standard14AFM.GetAverageCharacterWidth();
                }
                else
                {
                    return base.AverageFontWidth;
                }
            }
        }


        public override int ReadCode(Bytes.IInputStream input, out byte[] bytes)
        {
            bytes = new byte[1];
            input.Read(bytes);
            return bytes[0];
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
                // extract from Type1 font/substitute
                if (genericFont is IEncodedFont encodedFont)
                {
                    return Type1Encoding.FromFontBox(encodedFont.Encoding);
                }
                else
                {
                    // default (only happens with TTFs)
                    return StandardEncoding.Instance;
                }
            }
        }

        /**
         * Returns the embedded or substituted Type 1 font, or null if there is none.
         */
        public Type1Font Type1Font
        {
            get => type1font;
        }

        public override BaseFont Font
        {
            get => genericFont;
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
                if (IsNonZeroBoundingBox(bbox))
                {
                    return bbox.ToRect();
                }
            }
            return genericFont.FontBBox;
        }

        public string CodeToName(int code)
        {
            string name = Encoding.GetName(code);
            return GetNameInFont(name);
        }

        /**
         * Maps a PostScript glyph name to the name in the underlying font, for example when
         * using a TTF font we might map "W" to "uni0057".
         */
        private string GetNameInFont(string name)
        {
            if (IsEmbedded || genericFont.HasGlyph(name))
            {
                return name;
            }

            // try alternative name
            if (ALT_NAMES.TryGetValue(name, out string altName)
                && !name.Equals(".notdef", StringComparison.Ordinal)
                && genericFont.HasGlyph(altName))
            {
                return altName;
            }

            // try unicode name
            var unicodes = GlyphList.ToUnicode(name);
            if (unicodes != null && unicodes.Value < char.MaxValue)
            {
                string uniName = UniUtil.GetUniNameOfCodePoint((int)unicodes);
                if (genericFont.HasGlyph(uniName))
                {
                    return uniName;
                }
                // PDFBOX-4017: no postscript table on Windows 10, and the low uni00NN
                // names are not found in Symbol font. What works is using the PDF code plus 0xF000
                // while disregarding encoding from the PDF (because of file from PDFBOX-1606,
                // makes sense because this segment is about finding the name in a standard font)
                //TODO bring up better solution than this
                if ("SymbolMT".Equals(genericFont.Name, StringComparison.Ordinal))
                {
                    if (SymbolEncoding.Instance.NameToCodeMap.TryGetValue(name, out int code))
                    {
                        uniName = UniUtil.GetUniNameOfCodePoint(code + 0xF000);
                        if (genericFont.HasGlyph(uniName))
                        {
                            return uniName;
                        }
                    }
                }
            }

            return ".notdef";
        }

        public override SKPath GetPath(string name)
        {
            if (name == null)
                return null;
            // Acrobat does not draw .notdef for Type 1 fonts, see PDFBOX-2421
            // I suspect that it does do this for embedded fonts though, but this is untested
            if (name.Equals(".notdef") && !isEmbedded)
            {
                return null;
            }
            else
            {
                return genericFont.GetPath(GetNameInFont(name));
            }
        }

        public override SKPath GetPath(int code)
        {
            string name = Encoding.GetName(code);
            return GetPath(name);
        }

        public override SKPath GetNormalizedPath(int code)
        {
            if (!cacheGlyphs.TryGetValue(code, out SKPath path))
            {
                string name = Encoding.GetName(code);
                if (name == null)
                {
                    path = null;
                }
                else
                {
                    path = GetPath(name);
                }
                if (path == null)
                {
                    path = GetPath(".notdef");
                }
                cacheGlyphs[code] = path;
            }
            return path;
        }


        public override bool HasGlyph(string name)
        {
            return genericFont.HasGlyph(GetNameInFont(name));
        }


        public override bool HasGlyph(int code)
        {
            return !Encoding.GetName(code).Equals(".notdef", StringComparison.Ordinal);
        }


        public override SKMatrix FontMatrix
        {
            get
            {
                if (fontMatrix == null)
                {
                    // PDF specified that Type 1 fonts use a 1000upem matrix, but some fonts specify
                    // their own custom matrix anyway, for example PDFBOX-2298
                    List<float> numbers = null;
                    try
                    {
                        numbers = genericFont.FontMatrix;
                    }
                    catch (IOException e)
                    {
                        Debug.WriteLine("debug: Couldn't get font matrix box - returning default value", e);
                        fontMatrix = DefaultFontMatrix;
                    }

                    if (numbers != null && numbers.Count == 6)
                    {
                        fontMatrix = new SKMatrix(
                                numbers[0], numbers[1], numbers[4],
                                numbers[2], numbers[3], numbers[5],
                                0f, 0f, 1f);
                    }
                    else
                    {
                        fontMatrix = base.FontMatrix;
                    }
                }
                return (SKMatrix)fontMatrix;
            }
        }

        public override bool IsDamaged
        {
            get => isDamaged;
        }
        #endregion
    }
}