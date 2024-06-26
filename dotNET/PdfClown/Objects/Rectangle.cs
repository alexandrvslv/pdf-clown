/*
  Copyright 2010-2012 Stefano Chizzolini. http://www.pdfclown.org

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
using PdfClown.Files;

using System;
using SkiaSharp;

namespace PdfClown.Objects
{
    /**
      <summary>PDF rectangle object [PDF:1.6:3.8.4].</summary>
      <remarks>
        <para>Rectangles are described by two diagonally-opposite corners. Corner pairs which don't
        respect the canonical form (lower-left and upper-right) are automatically normalized to
        provide a consistent representation.</para>
        <para>Coordinates are expressed within the PDF coordinate space (lower-left origin and
        positively-oriented axes).</para>
      </remarks>
    */
    public sealed class Rectangle : PdfObjectWrapper<PdfArray>, IEquatable<Rectangle>
    {
        private static PdfArray Normalize(PdfArray rectangle)
        {
            if (rectangle.Count > 3)
            {
                if (rectangle.GetNumber(0).CompareTo(rectangle.GetNumber(2)) > 0)
                {
                    var leftCoordinate = rectangle.GetNumber(2);
                    rectangle[2] = (PdfDirectObject)rectangle.GetNumber(0);
                    rectangle[0] = (PdfDirectObject)leftCoordinate;
                }
                if (rectangle.GetNumber(1).CompareTo(rectangle.GetNumber(3)) > 0)
                {
                    var bottomCoordinate = rectangle.GetNumber(3);
                    rectangle[3] = (PdfDirectObject)rectangle.GetNumber(1);
                    rectangle[1] = (PdfDirectObject)bottomCoordinate;
                }
            }
            return rectangle;
        }

        public Rectangle(SKRect rectangle)
            : this(rectangle.Left, rectangle.Bottom, rectangle.Width, rectangle.Height)
        { }

        public Rectangle(SKPoint lowerLeft, SKPoint upperRight)
            : this(lowerLeft.X, upperRight.Y, upperRight.X - lowerLeft.X, upperRight.Y - lowerLeft.Y)
        { }

        public Rectangle(double left, double top, double width, double height)
            : this(new PdfArray(4)
              {
                  PdfReal.Get(left), // Left (X).
                  PdfReal.Get(top - height), // Bottom (Y).
                  PdfReal.Get(left + width), // Right.
                  PdfReal.Get(top) // Top.
              })
        { }


        public Rectangle(PdfDirectObject baseObject)
            : base(Normalize((PdfArray)baseObject.Resolve()))
        { }

        public double Left
        {
            get => BaseDataObject.GetDouble(0);
            set => BaseDataObject.SetDouble(0, value);
        }

        public double Bottom
        {
            get => BaseDataObject.GetDouble(1);
            set => BaseDataObject.SetDouble(1, value);
        }

        public double Right
        {
            get => BaseDataObject.GetDouble(2);
            set => BaseDataObject.SetDouble(2, value);
        }

        public double Top
        {
            get => BaseDataObject.GetDouble(3);
            set => BaseDataObject.SetDouble(3, value);
        }

        public SKRect ToRect()
        {
            return new SKRect((float)Left, (float)Bottom, (float)Right, (float)Top);
        }

        public bool Equals(Rectangle other)
        {
            return Math.Round(Left, 5).Equals(Math.Round(other.Left, 5))
                && Math.Round(Bottom, 5).Equals(Math.Round(other.Bottom, 5))
                && Math.Round(Right, 5).Equals(Math.Round(other.Right, 5))
                && Math.Round(Top, 5).Equals(Math.Round(other.Top, 5));
        }

        public Rectangle Round() => Round(File?.Configuration?.RealPrecision ?? 5);

        public Rectangle Round(int precision)
        {
            Left = Math.Round(Left, precision);
            Bottom = Math.Round(Bottom, precision);
            Right = Math.Round(Right, precision);
            Top = Math.Round(Top, precision);
            return this;
        }

        public double Width
        {
            get => Right - Left;
            set => Right = Left + value;
        }

        public double Height
        {
            get => Top - Bottom;
            set => Bottom = Top - value;
        }

        public double X
        {
            get => Left;
            set => Left = value;
        }

        public double Y
        {
            get => Bottom;
            set => Bottom = value;
        }
    }
}