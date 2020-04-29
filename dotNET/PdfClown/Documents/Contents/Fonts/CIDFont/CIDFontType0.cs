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
using PdfClown.Objects;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace PdfClown.Documents.Contents.Fonts
{
    /**
     * Type 0 CIDFont (CFF).
     * 
     * @author Ben Litchfield
     * @author John Hewson
     */
    public class CIDFontType0 : CIDFont
    {
        private readonly CFFCIDFont cidFont;  // Top DICT that uses CIDFont operators
        private readonly BaseFont t1Font; // Top DICT that does not use CIDFont operators

        private readonly Dictionary<int, float> glyphHeights = new Dictionary<int, float>();
        private readonly bool isEmbedded;
        private readonly bool isDamaged;
        private readonly SKMatrix fontMatrixTransform;
        private float? avgWidth = null;
        private SKMatrix? fontMatrix;
        private SKRect? fontBBox;
        private int[] cid2gid = null;

        public CIDFontType0(Document document, PdfDictionary fontObject) : base(document, fontObject)
        {
        }

        internal CIDFontType0(PdfDirectObject fontObject)
            : this((PdfDictionary)fontObject, null)
        {
        }
        /**
         * Constructor.
         * 
         * @param fontDictionary The font dictionary according to the PDF specification.
         * @param parent The parent font.
         */
        public CIDFontType0(PdfDirectObject fontDictionary, PdfType0Font parent)
            : base(fontDictionary, parent)
        {
            FontDescriptor fd = FontDescriptor;
            byte[] bytes = null;
            if (fd != null)
            {
                var ff3Stream = fd.FontFile3;
                if (ff3Stream != null)
                {
                    bytes = ff3Stream.BaseDataObject.ExtractBody(true).ToByteArray();
                }
            }

            bool fontIsDamaged = false;
            CFFFont cffFont = null;
            if (bytes != null && bytes.Length > 0 && (bytes[0] & 0xff) == '%')
            {
                // PDFBOX-2642 contains a corrupt PFB font instead of a CFF
                Debug.WriteLine("warn: Found PFB but expected embedded CFF font " + fd.FontName);
                fontIsDamaged = true;
            }
            else if (bytes != null)
            {
                CFFParser cffParser = new CFFParser();
                try
                {
                    cffFont = cffParser.Parse(bytes, new FF3ByteSource(fd, bytes))[0];
                }
                catch (IOException e)
                {
                    Debug.WriteLine("error: Can't read the embedded CFF font " + fd.FontName, e);
                    fontIsDamaged = true;
                }
            }

            if (cffFont != null)
            {
                // embedded
                if (cffFont is CFFCIDFont)
                {
                    cidFont = (CFFCIDFont)cffFont;
                    t1Font = null;
                }
                else
                {
                    cidFont = null;
                    t1Font = cffFont;
                }
                cid2gid = ReadCIDToGIDMap();
                isEmbedded = true;
                isDamaged = false;
            }
            else
            {
                // find font or substitute
                CIDFontMapping mapping = FontMappers.Instance.GetCIDFont(BaseFont, FontDescriptor, CIDSystemInfo);
                BaseFont font;
                if (mapping.IsCIDFont)
                {
                    cffFont = mapping.Font.CFF.Font;
                    if (cffFont is CFFCIDFont)
                    {
                        cidFont = (CFFCIDFont)cffFont;
                        t1Font = null;
                        font = cidFont;
                    }
                    else
                    {
                        // PDFBOX-3515: OpenType fonts are loaded as CFFType1Font
                        CFFType1Font f = (CFFType1Font)cffFont;
                        cidFont = null;
                        t1Font = f;
                        font = f;
                    }
                }
                else
                {
                    cidFont = null;
                    t1Font = mapping.TrueTypeFont;
                    font = t1Font;
                }

                if (mapping.IsFallback)
                {
                    Debug.WriteLine($"warning: Using fallback {font.Name} for CID-keyed font {BaseFont}");
                }
                isEmbedded = false;
                isDamaged = fontIsDamaged;
            }
            fontMatrixTransform = FontMatrix;
            SKMatrix.PostConcat(ref fontMatrixTransform, SKMatrix.MakeScale(1000, 1000));
        }

        public override SKMatrix FontMatrix
        {
            get
            {
                if (fontMatrix == null)
                {
                    List<float> numbers;
                    if (cidFont != null)
                    {
                        numbers = cidFont.FontMatrix;
                    }
                    else
                    {
                        try
                        {
                            numbers = t1Font.FontMatrix;
                        }
                        catch (IOException e)
                        {
                            Debug.WriteLine("debug:Couldn't get font matrix - returning default value", e);
                            return new SKMatrix(0.001f, 0, 0, 0, 0.001f, 0, 0, 0, 1);
                        }
                    }

                    if (numbers != null && numbers.Count == 6)
                    {
                        fontMatrix = new SKMatrix(numbers[0], numbers[1], numbers[4],
                                                numbers[2], numbers[3], numbers[5],
                                                0, 0, 1);
                    }
                    else
                    {
                        fontMatrix = new SKMatrix(0.001f, 0, 0, 0, 0.001f, 0, 0, 0, 1f);
                    }
                }
                return (SKMatrix)fontMatrix;
            }
        }

        public override SKRect BoundingBox
        {
            get => fontBBox ?? (fontBBox = GenerateBoundingBox()).Value;
        }

        private SKRect GenerateBoundingBox()
        {
            if (FontDescriptor != null)
            {
                var bbox = FontDescriptor.FontBBox;
                if (bbox != null && (
                    bbox.Left.CompareTo(0) != 0 ||
                    bbox.Bottom.CompareTo(0) != 0 ||
                    bbox.Right.CompareTo(0) != 0 ||
                    bbox.Top.CompareTo(0) != 0))
                {
                    return bbox.ToRect();
                }
            }
            if (cidFont != null)
            {
                return cidFont.FontBBox;
            }
            else
            {
                try
                {
                    return t1Font.FontBBox;
                }
                catch (IOException e)
                {
                    Debug.WriteLine("debug: Couldn't get font bounding box - returning default value", e);
                    return SKRect.Empty;
                }
            }
        }

        /**
         * Returns the embedded CFF CIDFont, or null if the substitute is not a CFF font.
         */
        public CFFFont CFFFont
        {
            get
            {
                if (cidFont != null)
                {
                    return cidFont;
                }
                else if (t1Font is CFFType1Font)
                {
                    return (CFFType1Font)t1Font;
                }
                else
                {
                    return null;
                }
            }
        }

        /**
         * Returns the embedded or substituted font.
         */
        public BaseFont Holder
        {
            get => cidFont ?? t1Font;
        }

        /**
         * Returns the Type 2 charstring for the given CID, or null if the substituted font does not
         * contain Type 2 charstrings.
         *
         * @param cid CID
         * @throws IOException if the charstring could not be read
         */
        public Type2CharString GetType2CharString(int cid)
        {
            if (cidFont != null)
            {
                return cidFont.GetType2CharString(cid);
            }
            else if (t1Font is CFFType1Font)
            {
                return ((CFFType1Font)t1Font).GetType2CharString(cid);
            }
            else
            {
                return null;
            }
        }

        /**
         * Returns the name of the glyph with the given character code. This is done by looking up the
         * code in the parent font's ToUnicode map and generating a glyph name from that.
         */
        private string GetGlyphName(int code)
        {
            int unicodes = parent.ToUnicode(code);
            if (unicodes < 0)
            {
                return ".notdef";
            }
            return UniUtil.GetUniNameOfCodePoint(unicodes);
        }

        public override SKPath GetPath(int code)
        {
            int cid = CodeToCID(code);
            if (cid2gid != null && isEmbedded)
            {
                // PDFBOX-4093: despite being a type 0 font, there is a CIDToGIDMap
                cid = cid2gid[cid];
            }
            Type2CharString charstring = GetType2CharString(cid);
            if (charstring != null)
            {
                return charstring.Path;
            }
            else if (isEmbedded && t1Font is CFFType1Font fFType1Font)
            {
                return fFType1Font.GetType2CharString(cid).Path;
            }
            else
            {
                return t1Font.GetPath(GetGlyphName(code));
            }
        }

        public override SKPath GetNormalizedPath(int code)
        {
            return GetPath(code);
        }

        public override bool HasGlyph(int code)
        {
            int cid = CodeToCID(code);
            Type2CharString charstring = GetType2CharString(cid);
            if (charstring != null)
            {
                return charstring.GID != 0;
            }
            else if (isEmbedded && t1Font is CFFType1Font fFType1Font)
            {
                return fFType1Font.GetType2CharString(cid).GID != 0;
            }
            else
            {
                return t1Font.HasGlyph(GetGlyphName(code));
            }
        }

        /**
         * Returns the CID for the given character code. If not found then CID 0 is returned.
         *
         * @param code character code
         * @return CID
         */
        override public int CodeToCID(int code)
        {
            return parent.CMap.ToCID(code);
        }

        override public int CodeToGID(int code)
        {
            int cid = CodeToCID(code);
            if (cidFont != null)
            {
                // The CIDs shall be used to determine the GID value for the glyph procedure using the
                // charset table in the CFF program
                return cidFont.Charset.GetGIDForCID(cid);
            }
            else
            {
                // The CIDs shall be used directly as GID values
                return cid;
            }
        }
        public override int ReadCode(Bytes.IInputStream input, out byte[] bytes)
        {
            throw new NotSupportedException();
        }

        public override byte[] Encode(int unicode)
        {
            // todo: we can use a known character collection CMap for a CIDFont
            //       and an Encoding for Type 1-equivalent
            throw new NotSupportedException();
        }

        public override byte[] EncodeGlyphId(int glyphId)
        {
            throw new NotSupportedException();
        }

        public override float GetWidthFromFont(int code)
        {
            int cid = CodeToCID(code);
            float width;
            if (cidFont != null)
            {
                width = GetType2CharString(cid).Width;
            }
            else if (isEmbedded && t1Font is CFFType1Font fFType1Font)
            {
                width = fFType1Font.GetType2CharString(cid).Width;
            }
            else
            {
                width = t1Font.GetWidth(GetGlyphName(code));
            }

            SKPoint p = new SKPoint(width, 0);
            p = fontMatrixTransform.MapPoint(p);
            return p.X;
        }

        public override bool IsEmbedded
        {
            get => isEmbedded;
        }

        public override bool IsDamaged
        {
            get => isDamaged;
        }

        public override float GetHeight(int code)
        {
            int cid = CodeToCID(code);

            if (!glyphHeights.TryGetValue(cid, out float height))
            {
                height = (float)GetType2CharString(cid).Bounds.Height;
                glyphHeights[cid] = height;
            }
            return height;
        }

        public override float AverageFontWidth
        {
            get
            {
                if (avgWidth == null)
                {
                    avgWidth = GetAverageCharacterWidth();
                }
                return (float)avgWidth;
            }
        }

        // todo: this is a replacement for FontMetrics method
        private float GetAverageCharacterWidth()
        {
            // todo: not implemented, highly suspect
            return 500;
        }

        private class FF3ByteSource : CFFParser.IByteSource
        {
            private readonly byte[] data;

            public FF3ByteSource(FontDescriptor fontDescriptor, byte[] data)
            {
                FontDescriptor = fontDescriptor;
                this.data = data;
            }

            public FontDescriptor FontDescriptor { get; }

            public byte[] GetBytes()
            {
                return data;
            }
        }
    }
}
