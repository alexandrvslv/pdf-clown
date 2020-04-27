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
using PdfClown.Documents.Contents.Fonts.CCF;
using PdfClown.Objects;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace PdfClown.Documents.Contents.Fonts
{

    /**
     * Type 1-equivalent CFF font.
     *
     * @author Villu Ruusmann
     * @author John Hewson
     */
    public class PdfType1CFont : SimpleFont//,  PDVectorFont
    {
        private readonly Dictionary<string, float> glyphHeights = new Dictionary<string, float>();
        private readonly SKMatrix fontMatrixTransform;
        private readonly CFFType1Font cffFont; // embedded font
        private readonly BaseFont genericFont; // embedded or system font for rendering
        private readonly bool isEmbedded;
        private readonly bool isDamaged;
        private float? avgWidth = null;
        private SKMatrix? fontMatrix;
        private SKRect? fontBBox;

        /**
         * Constructor.
         * 
         * @param fontDictionary the corresponding dictionary
         * @throws IOException it something went wrong
         */
        public PdfType1CFont(PdfDirectObject fontDictionary)
            : base(fontDictionary)
        {
            FontDescriptor fd = FontDescriptor;
            byte[] bytes = null;
            if (fd != null)
            {
                var ff3Stream = fd.FontFile3;
                if (ff3Stream != null)
                {
                    bytes = ff3Stream.BaseDataObject.ExtractBody(true).GetBuffer();
                    if (bytes.Length == 0)
                    {
                        Debug.WriteLine($"error: Invalid data for embedded Type1C font {Name}");
                        bytes = null;
                    }
                }
            }

            bool fontIsDamaged = false;
            CFFType1Font cffEmbedded = null;
            try
            {
                if (bytes != null)
                {
                    // note: this could be an OpenType file, fortunately CFFParser can handle that
                    CFFParser cffParser = new CFFParser();
                    cffEmbedded = (CFFType1Font)cffParser.Parse(bytes, new FF3ByteSource(fd))[0];
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine($"error: Can't read the embedded Type1C font {Name} {e}");
                fontIsDamaged = true;
            }
            isDamaged = fontIsDamaged;
            cffFont = cffEmbedded;

            if (cffFont != null)
            {
                genericFont = cffFont;
                isEmbedded = true;
            }
            else
            {
                FontMapping<BaseFont> mapping = FontMappers.Instance.GetBaseFont(BaseFont, fd);
                genericFont = mapping.Font;

                if (mapping.IsFallback)
                {
                    Debug.WriteLine($"warn: Using fallback font {genericFont.Name} for {BaseFont}");
                }
                isEmbedded = false;
            }
            ReadEncoding();
            fontMatrixTransform = FontMatrix;
            fontMatrixTransform.SetScaleTranslate(1000, 1000, 0, 0);
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
                var bbox = FontDescriptor.FontBBox;
                if (IsNonZeroBoundingBox(bbox))
                {
                    return bbox.ToRect();
                }
            }
            return genericFont.FontBBox;
        }

        public override SKMatrix FontMatrix
        {
            get
            {
                if (fontMatrix == null)
                {
                    List<float> numbers = null;
                    try
                    {
                        numbers = genericFont.FontMatrix;
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine($"debug: Couldn't get font matrix - returning default value {e}");
                        fontMatrix = DefaultFontMatrix;
                    }

                    if (numbers != null && numbers.Count == 6)
                    {
                        fontMatrix = new SKMatrix(
                                numbers[0], numbers[1], numbers[4],
                                numbers[2], numbers[3], numbers[5],
                                0, 0, 1);
                    }
                    else
                    {
                        return base.FontMatrix;
                    }
                }
                return (SKMatrix)fontMatrix;
            }
        }

        public override bool IsDamaged
        {
            get => isDamaged;
        }

        public override bool IsEmbedded
        {
            get => isEmbedded;
        }

        public override float AverageFontWidth
        {
            get
            {
                if (avgWidth == null)
                {
                    avgWidth = AverageCharacterWidth;
                }
                return (float)avgWidth;
            }
        }

        /**
         * Returns the embedded Type 1-equivalent CFF font.
         * 
         * @return the cffFont
         */
        public CFFType1Font CFFType1Font
        {
            get => cffFont;
        }

        // todo: this is a replacement for FontMetrics method
        private float AverageCharacterWidth
        {
            // todo: not implemented, highly suspect
            get => 500;
        }

        public override SKPath GetPath(string name)
        {
            // Acrobat only draws .notdef for embedded or "Standard 14" fonts, see PDFBOX-2372
            if (name.Equals(".notdef", StringComparison.Ordinal) && !IsEmbedded && !IsStandard14)
            {
                return null;
            }
            else
            {
                return genericFont.GetPath(name);
            }
        }

        public override bool HasGlyph(int code)
        {
            string name = Encoding.GetName(code);
            name = GetNameInFont(name);
            return HasGlyph(name);
        }

        public override SKPath GetPath(int code)
        {
            string name = Encoding.GetName(code);
            name = GetNameInFont(name);
            return GetPath(name);
        }

        public override SKPath GetNormalizedPath(int code)
        {
            string name = Encoding.GetName(code);
            name = GetNameInFont(name);
            SKPath path = GetPath(name);
            if (path == null)
            {
                return GetPath(".notdef");
            }
            return path;
        }

        public override bool HasGlyph(string name)
        {
            return genericFont.HasGlyph(name);
        }

        //override
        public string CodeToName(int code)
        {
            return Encoding.GetName(code);
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


        public override int ReadCode(Bytes.IInputStream input, out byte[] bytes)
        {
            bytes = new byte[1] { (byte)input.ReadByte() };
            return bytes[0];
        }

        public override float GetWidthFromFont(int code)
        {
            string name = CodeToName(code);
            name = GetNameInFont(name);
            float width = genericFont.GetWidth(name);

            var p = fontMatrixTransform.MapVector(width, 0);
            return p.X;
        }


        public override float GetHeight(int code)
        {
            string name = CodeToName(code);
            if (!glyphHeights.TryGetValue(name, out float height))
            {
                glyphHeights[name] =
                height = cffFont.GetType1CharString(name).Bounds.Height; // todo: cffFont could be null
            }
            return height;
        }


        public override byte[] Encode(int unicode)
        {
            string name = GlyphList.UnicodeToName(unicode);
            if (!encoding.Contains(name))
            {
                throw new ArgumentException($"U+{unicode:x4} ('{name}') is not available in this font's encoding: {encoding.GetPdfObject()}");
            }

            string nameInFont = GetNameInFont(name);

            Dictionary<string, int> inverted = encoding.NameToCodeMap;

            if (nameInFont.Equals(".notdef", StringComparison.Ordinal) || !genericFont.HasGlyph(nameInFont))
            {
                throw new ArgumentException($"No glyph for U+{unicode:x4} in font {Name}");
            }

            inverted.TryGetValue(name, out int code);
            return new byte[] { (byte)code };
        }


        public override float GetWidth(string text)
        {
            float width = 0;
            for (int i = 0; i < text.Length; i++)
            {
                int codePoint = text[i];
                string name = GlyphList.UnicodeToName(codePoint);
                width += cffFont.GetType1CharString(name).Width;
            }
            return width;
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
            else
            {
                // try unicode name
                var unicode = GlyphList.ToUnicode(name);
                if (unicode != null)
                {
                    string uniName = UniUtil.GetUniNameOfCodePoint((int)unicode);
                    if (genericFont.HasGlyph(uniName))
                    {
                        return uniName;
                    }
                }
            }
            return ".notdef";
        }

        private class FF3ByteSource : CFFParser.IByteSource
        {
            public FF3ByteSource(FontDescriptor fontDescriptor)
            {
                FontDescriptor = fontDescriptor;
            }

            public FontDescriptor FontDescriptor { get; }

            public byte[] GetBytes()
            {
                return FontDescriptor.FontFile3.BaseDataObject.ExtractBody(true).GetBuffer();
            }
        }
    }
}