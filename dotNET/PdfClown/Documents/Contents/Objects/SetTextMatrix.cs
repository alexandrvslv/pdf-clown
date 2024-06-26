/*
  Copyright 2007-2012 Stefano Chizzolini. http://www.pdfclown.org

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
using PdfClown.Objects;

using System;
using System.Collections.Generic;
using SkiaSharp;
using PdfClown.Documents.Contents.Scanner;

namespace PdfClown.Documents.Contents.Objects
{
    /**
      <summary>'Set the text matrix' operation [PDF:1.6:5.3.1].</summary>
      <remarks>The specified matrix is not concatenated onto the current text SKMatrix,
      but replaces it.</remarks>
    */
    [PDF(VersionEnum.PDF10)]
    public sealed class SetTextMatrix : Operation
    {
        public static readonly string OperatorKeyword = "Tm";

        public SetTextMatrix(SKMatrix value)
            : this(value.ScaleX,
                  value.SkewY,
                  value.SkewX,
                  value.ScaleY,
                  value.TransX,
                  value.TransY)
        { }

        public SetTextMatrix(double a, double b, double c, double d, double e, double f)
            : base(OperatorKeyword,
                  PdfReal.Get(a),
                  PdfReal.Get(b),
                  PdfReal.Get(c),
                  PdfReal.Get(d),
                  PdfReal.Get(e),
                  PdfReal.Get(f))
        { }

        public SetTextMatrix(IList<PdfDirectObject> operands) : base(OperatorKeyword, operands)
        { }

        public override void Scan(GraphicsState state)
        {
            state.TextState.Tm =
                state.TextState.Tlm = Value;
        }

        public SKMatrix Value => new SKMatrix
        {
            ScaleX = ((IPdfNumber)operands[0]).FloatValue,
            SkewY = ((IPdfNumber)operands[1]).FloatValue,
            SkewX = ((IPdfNumber)operands[2]).FloatValue,
            ScaleY = ((IPdfNumber)operands[3]).FloatValue,
            TransX = ((IPdfNumber)operands[4]).FloatValue,
            TransY = ((IPdfNumber)operands[5]).FloatValue,
            Persp2 = 1
        };
    }
}