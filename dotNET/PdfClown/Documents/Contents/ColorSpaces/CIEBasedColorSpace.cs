/*
  Copyright 2006-2011 Stefano Chizzolini. http://www.pdfclown.org

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
using System.Collections.Generic;

namespace PdfClown.Documents.Contents.ColorSpaces
{
    /// <summary>Abstract CIE-based color space [PDF:1.6:4.5.4].</summary>
    [PDF(VersionEnum.PDF11)]
    public abstract class CIEBasedColorSpace : ColorSpace
    {
        private float[] blackPoint;
        private float[] whitePoint;

        //TODO:IMPL new element constructor!

        protected CIEBasedColorSpace(List<PdfDirectObject> baseObject) 
            : base(baseObject)
        { }

        /// <summary>Gets the tristimulus value, in the CIE 1931 XYZ space, of the diffuse black point.</summary>
        public float[] BlackPoint
        {
            get => blackPoint ??= Dictionary.Get<PdfArray>(PdfName.BlackPoint) is PdfArray array
                      ? new float[] { array.GetFloat(0), array.GetFloat(1), array.GetFloat(2) }
                      : new float[] { 0, 0, 0 };
        }

        /// <summary>Gets the tristimulus value, in the CIE 1931 XYZ space, of the diffuse white point.</summary>
        public float[] WhitePoint
        {
            get => whitePoint ??= Dictionary.Get<PdfArray>(PdfName.WhitePoint) is PdfArray array
                  ? new float[] { array.GetFloat(0), array.GetFloat(1), array.GetFloat(2) }
                  : new float[] { 0, 0, 0 };
        }

        /// <summary>Gets this color space's dictionary.</summary>
        protected PdfDictionary Dictionary => Get<PdfDictionary>(1);

    }
}