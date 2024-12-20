/*
  Copyright 2010-2011 Stefano Chizzolini. http://www.pdfclown.org

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

using PdfClown.Documents.Contents.Shadings;
using PdfClown.Objects;
using SkiaSharp;
using System.Collections.Generic;

namespace PdfClown.Documents.Contents.Patterns
{
    /// <summary>Pattern providing a smooth transition between colors across an area to be painted[PDF:1.6:4.6.3].</summary>
    /// <remarks>The transition is continuous and independent of the resolution of any particular output device.</remarks>
    [PDF(VersionEnum.PDF13)]
    public sealed class ShadingPattern : Pattern
    {
        //TODO:IMPL new element constructor!

        internal ShadingPattern(Dictionary<PdfName, PdfDirectObject> baseObject)
            : base(baseObject)
        { }

        /// <summary>Gets the graphics state parameters to be put into effect temporarily
        /// while the shading pattern is painted.</summary>
        /// <remarks>Any parameters that are not so specified are inherited from the graphics state
        /// that was in effect at the beginning of the content stream in which the pattern
        /// is defined as a resource.</remarks>
        public ExtGState ExtGState
        {
            get => Get<ExtGState>(PdfName.ExtGState);
            set => Set(PdfName.ExtGState, value);
        }

        /// <summary>Gets a shading object defining the shading pattern's gradient fill.</summary>
        public Shading Shading
        {
            get => Get<Shading>(PdfName.Shading);
            set => Set(PdfName.Shading, value);
        }

        public override SKShader GetShader(GraphicsState state)
        {
            return Shading.GetShader(Matrix, state);
        }

    }
}