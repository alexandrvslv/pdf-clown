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

using PdfClown.Objects;
using SkiaSharp;

namespace PdfClown.Documents.Contents.Objects
{
    /// <summary>'Append a rectangle to the current path as a complete subpath' operation
    /// [PDF:1.6:4.4.1].</summary>
    [PDF(VersionEnum.PDF10)]
    public sealed class DrawRectangle : Operation
    {
        public static readonly string OperatorKeyword = "re";

        public DrawRectangle(double x, double y, double width, double height)
            : base(OperatorKeyword,
                  new PdfArrayImpl(4) { x, y, width, height })
        { }

        public DrawRectangle(PdfArray operands) : base(OperatorKeyword, operands)
        { }

        public float Height
        {
            get => operands.GetFloat(3);
            set => operands.Set(3, value);
        }

        public float Width
        {
            get => operands.GetFloat(2);
            set => operands.Set(2, value);
        }

        public float X
        {
            get => operands.GetFloat(0);
            set => operands.Set(0, value);
        }

        public float Y
        {
            get => operands.GetFloat(1);
            set => operands.Set(1, value);
        }

        public override void Scan(GraphicsState state) => state.Scanner.Path?.AddRect(SKRect.Create(X, Y, Width, Height));

    }
}
