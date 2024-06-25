﻿/*
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

using System;
using SkiaSharp;

namespace PdfClown.Objects
{
    public sealed class Padding : PdfObjectWrapper<PdfArray>, IEquatable<Padding>
    {
        public Padding(SKRect rectangle)
            : this(rectangle.Left, rectangle.Bottom, rectangle.Right, rectangle.Top)
        { }

        public Padding(SKPoint lowerLeft, SKPoint upperRight)
            : this(lowerLeft.X, upperRight.Y, upperRight.X, upperRight.Y)
        { }

        public Padding(double left, double top, double right, double bottom)
            : this(new PdfArray(4)
              {
                  left, // Left (X).
                  bottom, // Bottom (Y).
                  right, // Right.
                  top // Top.
              })
        { }


        public Padding(PdfDirectObject baseObject)
            : base(baseObject)
        { }

        public double Left
        {
            get => BaseDataObject.GetDouble(0);
            set => BaseDataObject.Set(0, value);
        }

        public double Bottom
        {
            get => BaseDataObject.GetDouble(1);
            set => BaseDataObject.Set(1, value);
        }

        public double Right
        {
            get => BaseDataObject.GetDouble(2);
            set => BaseDataObject.Set(2, value);
        }

        public double Top
        {
            get => BaseDataObject.GetDouble(3);
            set => BaseDataObject.Set(3, value);
        }

        public bool Equals(Padding other)
        {
            return Left.Equals(other.Left)
                && Bottom.Equals(other.Bottom)
                && Right.Equals(other.Right)
                && Top.Equals(other.Top);
        }
        
        public double LeftRight
        {
            get => Right + Left;
            set => Right = Left = value / 2;
        }

        public double TopBottom
        {
            get => Top + Bottom;
            set => Bottom = Top = value / 2;
        }
    }
}