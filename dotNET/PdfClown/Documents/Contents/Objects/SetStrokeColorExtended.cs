﻿/*
  Copyright 2007-2015 Stefano Chizzolini. http://www.pdfclown.org

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

using PdfClown.Documents.Contents.ColorSpaces;
using PdfClown.Objects;

namespace PdfClown.Documents.Contents.Objects
{
    public sealed class SetStrokeColorExtended : SetStrokeColor
    {
        ///<summary>'Set the color to use for stroking operations in any color space' operator.</summary>
        [PDF(VersionEnum.PDF12)]
        public static readonly string OperatorKeyword = "SCN";

        public SetStrokeColorExtended(PdfArray operands)
            : base(OperatorKeyword, operands)
        { }

        internal SetStrokeColorExtended(IColor value)
            : base(OperatorKeyword, value.Components)
        { }

        /// <param name="operator"> Graphics operator.</param>
        /// <param name="name"> Name of the color resource entry(see<see cref="Patterns" />).</param>
        internal SetStrokeColorExtended(PdfName name)
            : this(name, null)
        { }

        /// <param name="operator"> Graphics operator.</param>
        /// <param name="name"> Name of the color resource entry(see<see cref="Patterns" />).</param>
        /// <param name="underlyingColor"> Color used to colorize the pattern.</param>
        internal SetStrokeColorExtended(PdfName name, IColor underlyingColor)
            : base(OperatorKeyword, name, underlyingColor)
        { }

    }

}