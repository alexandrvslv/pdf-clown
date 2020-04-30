/*
  Copyright 2010-2015 Stefano Chizzolini. http://www.pdfclown.org

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

using PdfClown.Objects;
using PdfClown.Util;
using PdfClown.Util.Math.Geom;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace PdfClown.Documents.Contents.Fonts
{
    /**
      <summary>Type 3 font [PDF:1.6:5.5.4].</summary>
    */
    [PDF(VersionEnum.PDF10)]
    public sealed class PdfType3Font : SimpleFont
    {
        private Resources resources;
        private PdfDictionary charProcs;
        private SKMatrix? fontMatrix;
        private SKRect? fontBBox;

        #region dynamic
        #region constructors
        internal PdfType3Font(Document context) : base(context)
        { }

        internal PdfType3Font(PdfDirectObject baseObject) : base(baseObject)
        {
            ReadEncoding();
        }
        #endregion

        #region interface
        #region public

        public override string Name
        {
            get => Dictionary.Resolve(PdfName.Name)?.ToString();
        }


        protected override void ReadEncoding()
        {
            var encodingBase = EncodingData;
            if (encodingBase is PdfName encodingName)
            {
                encoding = Encoding.Get(encodingName);
                if (encoding == null)
                {
                    Debug.WriteLine($"warn: Unknown encoding: {encodingName}");
                }
            }
            else if (encodingBase is PdfDictionary dictionary)
            {
                encoding = new DictionaryEncoding(dictionary);
            }
            glyphList = GlyphMapping.Default;
        }

        protected override Encoding ReadEncodingFromFont()
        {
            // Type 3 fonts do not have a built-in encoding
            throw new NotSupportedException("not supported for Type 3 fonts");
        }

        protected override bool? FontSymbolic
        {
            get => false;
        }

        public override BaseFont Font
        {
            // Type 3 fonts do not use FontBox fonts
            get => throw new NotSupportedException("not supported for Type 3 fonts");
        }

        public override bool IsEmbedded
        {
            get => true;
        }

        public override SKMatrix FontMatrix
        {
            get
            {
                if (fontMatrix == null)
                {
                    var baseObject = Dictionary.Resolve(PdfName.FontMatrix);
                    if (baseObject is PdfArray array)
                    {
                        fontMatrix = new SKMatrix(
                            ((IPdfNumber)array[0]).FloatValue, ((IPdfNumber)array[1]).FloatValue, ((IPdfNumber)array[4]).FloatValue,
                            ((IPdfNumber)array[2]).FloatValue, ((IPdfNumber)array[3]).FloatValue, ((IPdfNumber)array[5]).FloatValue,
                            0, 0, 1);
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
            // there's no font file to load
            get => false;
        }

        public override bool IsStandard14
        {
            get => false;
        }

        /**
         * Returns the optional resources of the type3 stream.
         *
         * @return the resources bound to be used when parsing the type3 stream
         */
        public Resources Resources
        {
            get => resources ?? (resources = Wrap<Resources>(Dictionary[PdfName.Resources]));
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

        /**
         * Returns the dictionary containing all streams to be used to render the glyphs.
         * 
         * @return the dictionary containing all glyph streams.
         */
        public PdfDictionary CharProcs
        {
            get
            {
                if (charProcs == null)
                {
                    charProcs = (PdfDictionary)Dictionary.Resolve(PdfName.CharProcs);
                }
                return charProcs;
            }
        }

        public override SKPath GetPath(int code)
        {
            throw new NotSupportedException("not supported for Type 3 fonts");
        }

        public override SKPath GetPath(string name)
        {
            // Type 3 fonts do not use vector paths
            throw new NotSupportedException("not supported for Type 3 fonts");
        }

        public override SKPath GetNormalizedPath(int code)
        {
            throw new NotSupportedException("not supported for Type 3 fonts");
        }

        public override bool HasGlyph(int code)
        {
            string name = Encoding.GetName(code);
            return GetCharProc(PdfName.Get(name)) != null;
        }

        public override bool HasGlyph(string name)
        {
            return GetCharProc(PdfName.Get(name)) != null;
        }

        public override SKPoint GetDisplacement(int code)
        {
            return FontMatrix.MapVector(base.GetWidth(code), 0);
        }

        public override double GetScalingFactor(double size)
        {
            return base.GetScalingFactor(size);
        }

        public override float GetWidth(int code)
        {
            int firstChar = FirstChar ?? -1;
            int lastChar = LastChar ?? -1;
            if (Widths.Count > 0 && code >= firstChar && code <= lastChar)
            {
                var w = (IPdfNumber)Widths[code - firstChar];
                return w == null ? 0 : w.FloatValue;
            }
            else
            {
                return FontDescriptor?.MissingWidth ?? GetWidthFromFont(code);
            }
        }

        public override float GetWidthFromFont(int code)
        {
            PdfType3CharProc charProc = GetCharProc(code);
            if (charProc == null || charProc.Contents == null ||
                charProc.Contents.Count == 0)
            {
                return 0;
            }
            return charProc.Width ?? 0;
        }

        public override float GetHeight(int code)
        {
            var desc = FontDescriptor;
            if (desc != null)
            {
                // the following values are all more or less accurate at least all are average
                // values. Maybe we'll find another way to get those value for every single glyph
                // in the future if needed
                var bbox = desc.FontBBox;
                float retval = 0;
                if (bbox != null)
                {
                    retval = (float)bbox.Height / 2;
                }
                if (retval.CompareTo(0) == 0)
                {
                    retval = desc.CapHeight ?? 0;
                }
                if (retval.CompareTo(0) == 0)
                {
                    retval = desc.Ascent;
                }
                if (retval.CompareTo(0) == 0)
                {
                    retval = desc.XHeight ?? 0;
                    if (retval > 0)
                    {
                        retval -= desc.Descent;
                    }
                }
                return retval;
            }
            return 0;
        }

        public override byte[] Encode(int unicode)
        {
            throw new NotSupportedException("Not implemented: Type3");
        }

        public override int ReadCode(Bytes.IInputStream input, out byte[] bytes)
        {
            bytes = new byte[] { (byte)input.ReadByte() };
            return bytes[0];
        }

        private SKRect GenerateBoundingBox()
        {
            var rect = FontBBox.ToRect();
            if (rect.Width == 0 || rect.Height == 0)
            {
                // Plan B: get the max bounding box of the glyphs
                PdfDictionary cp = CharProcs;
                foreach (PdfName name in cp.Keys)
                {
                    var charProc = GetCharProc(name);
                    if (charProc != null)
                    {
                        try
                        {
                            var glyphBBox = charProc.GlyphBox;
                            if (glyphBBox == null)
                            {
                                continue;
                            }
                            rect.Left = Math.Min(rect.Left, glyphBBox.Value.Left);
                            rect.Top = Math.Min(rect.Top, glyphBBox.Value.Top);
                            rect.Right = Math.Max(rect.Right, glyphBBox.Value.Right);
                            rect.Bottom = Math.Max(rect.Bottom, glyphBBox.Value.Bottom);
                        }
                        catch (Exception ex)
                        {
                            // ignore
                            Debug.WriteLine($"debug: error getting the glyph bounding box - font bounding box will be used {ex}");
                        }
                    }
                }
            }
            return rect;
        }

        /**
         * Returns the stream of the glyph for the given character code
         * 
         * @param code character code
         * @return the stream to be used to render the glyph
         */
        public PdfType3CharProc GetCharProc(int code)
        {
            string name = Encoding.GetName(code);
            return GetCharProc(PdfName.Get(name));
        }

        private PdfType3CharProc GetCharProc(PdfName name)
        {
            var baseObject = CharProcs[name];
            return PdfType3CharProc.Wrap(baseObject, this);
        }

        public override void DrawChar(SKCanvas context, SKPaint fill, SKPaint stroke, char textChar, int code, byte[] codeBytes, ref SKMatrix parameters)
        {
            var proc = GetCharProc(code);
            if (proc == null)
            {
                Debug.WriteLine($"info: no Glyph for Code: {code}  Char: '{textChar}'");
                return;
            }
            context.Save();
            var m = FontMatrix;

            //SKMatrix.PreConcat(ref m,parameters );
            context.Concat(ref m);

            if (fill != null)
            {
                var picture = proc.Render();
                context.DrawPicture(picture, fill);
            }

            if (stroke != null)
            {
                var picture = proc.Render();
                context.DrawPicture(picture, stroke);
            }
            context.Restore();
        }
        #endregion
        #endregion
        #endregion
    }
}