/*
  Copyright 2006-2012 Stefano Chizzolini. http://www.pdfclown.org

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

using System;
using System.Collections.Generic;
using SkiaSharp;

namespace PdfClown.Documents.Contents.ColorSpaces
{
    ///<summary>Device Red-Green-Blue color value [PDF:1.6:4.5.3].</summary>
    [PDF(VersionEnum.PDF11)]
    public sealed class DeviceRGBColor : DeviceColor, IEquatable<DeviceRGBColor>
    {
        public static readonly DeviceRGBColor Black = Get(SKColors.Black);
        public static readonly DeviceRGBColor White = Get(SKColors.White);
        public static readonly DeviceRGBColor Red = Get(SKColors.Red);
        public static readonly DeviceRGBColor Green = Get(SKColors.Green);
        public static readonly DeviceRGBColor Blue = Get(SKColors.Blue);
        public static readonly DeviceRGBColor Cyan = Get(SKColors.Cyan);
        public static readonly DeviceRGBColor Magenta = Get(SKColors.Magenta);
        public static readonly DeviceRGBColor Yellow = Get(SKColors.Yellow);
        public static readonly DeviceRGBColor OrangeRed = Get(SKColors.OrangeRed);

        public static readonly DeviceRGBColor Default = Black;

        /**
          <summary>Gets the color corresponding to the specified system color.</summary>
          <param name="color">System color to convert.</param>
        */
        public static DeviceRGBColor Get(SKColor? color)
        {
            return (color.HasValue
              ? new DeviceRGBColor(color.Value.Red / 255d, color.Value.Green / 255d, color.Value.Blue / 255d)
              : Default);
        }

        public DeviceRGBColor(double r, double g, double b)
            : this(DeviceRGBColorSpace.Default,
                  new PdfArray(3)
            {
                NormalizeComponent(r),
                NormalizeComponent(g),
                NormalizeComponent(b)
            })
        { }

        internal DeviceRGBColor(DeviceColorSpace colorSpace, PdfArray components)
            : base(colorSpace, components)
        { }

        ///<summary>Gets/Sets the red component.</summary>
        public float R
        {
            get => this[0];
            set => this[0] = value;
        }

        ///<summary>Gets/Sets the green component.</summary>
        public float G
        {
            get => this[1];
            set => this[1] = value;
        }

        ///<summary>Gets/Sets the blue component.</summary>
        public float B
        {
            get => this[2];
            set => this[2] = value;
        }

        public override bool IsZero => Equals(Black);

        public override object Clone(PdfDocument context)
        {
            throw new NotImplementedException();
        }

        public bool Equals(DeviceRGBColor other)
        {
            if (other == null)
                return false;
            return R.Equals(other.R)
                && G.Equals(other.G)
                && B.Equals(other.B);
        }

    }
}