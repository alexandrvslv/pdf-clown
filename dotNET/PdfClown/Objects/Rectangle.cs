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
        #region static
        #region interface
        #region private
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

        #endregion
        #endregion
        #endregion

        #region dynamic
        #region constructors
        public Rectangle(SKRect rectangle)
            : this(rectangle.Left, rectangle.Bottom, rectangle.Width, rectangle.Height)
        { }

        public Rectangle(SKPoint lowerLeft, SKPoint upperRight)
            : this(lowerLeft.X, upperRight.Y, upperRight.X - lowerLeft.X, upperRight.Y - lowerLeft.Y)
        { }

        public Rectangle(double left, double top, double width, double height)
            : this(new PdfArray(new PdfDirectObject[]
              {
                  PdfReal.Get(left), // Left (X).
                  PdfReal.Get(top - height), // Bottom (Y).
                  PdfReal.Get(left + width), // Right.
                  PdfReal.Get(top) // Top.
              }
              )
            )
        { }

        public Rectangle(PdfDirectObject baseObject) : base(Normalize((PdfArray)baseObject.Resolve()))
        { }
        #endregion

        #region interface
        #region public
        public double Left
        {
            get => BaseDataObject.GetNumber(0).RawValue;
            set => BaseDataObject[0] = PdfReal.Get(value);
        }

        public double Bottom
        {
            get => BaseDataObject.GetNumber(1).RawValue;
            set => BaseDataObject[1] = PdfReal.Get(value);
        }

        public double Right
        {
            get => BaseDataObject.GetNumber(2).RawValue;
            set => BaseDataObject[2] = PdfReal.Get(value);
        }

        public double Top
        {
            get => BaseDataObject.GetNumber(3).RawValue;
            set => BaseDataObject[3] = PdfReal.Get(value);
        }

        public SKRect ToRect()
        {
            return new SKRect((float)Left, (float)Bottom, (float)Right, (float)Top);
        }

        public bool Equals(Rectangle other)
        {
            return Left.Equals(other.Left)
                && Bottom.Equals(other.Bottom)
                && Right.Equals(other.Right)
                && Top.Equals(other.Top);
        }

        public double Width
        {
            get => Right - Left;
            set => Right = Left + value;
        }

        public double Height
        {
            get => (Top - Bottom);
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
        #endregion
        #endregion
        #endregion
    }
}