/*
  Copyright 2008-2015 Stefano Chizzolini. http://www.pdfclown.org

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
using PdfClown.Documents.Contents.Layers;
using PdfClown.Objects;

using System.Collections.Generic;

namespace PdfClown.Documents.Contents.Objects
{
    /**
      <summary>'Begin marked-content sequence' operation [PDF:1.6:10.5].</summary>
    */
    [PDF(VersionEnum.PDF12)]
    public sealed class BeginMarkedContent : ContentMarker
    {
        public static readonly string PropertyListOperatorKeyword = "BDC";
        public static readonly string SimpleOperatorKeyword = "BMC";

        public BeginMarkedContent(PdfName tag) : base(tag)
        { }

        public BeginMarkedContent(PdfName tag, PdfDirectObject properties) : base(tag, properties)
        { }

        internal BeginMarkedContent(string @operator, IList<PdfDirectObject> operands) : base(@operator, operands)
        { }

        protected override string PropertyListOperator => PropertyListOperatorKeyword;

        protected override string SimpleOperator => SimpleOperatorKeyword;

        public override void Scan(GraphicsState state)
        {
            var properties = GetProperties(state.Scanner);
            if (properties is Layer layer
                && layer.Viewable == false)
            {
               //state.Scanner.ContentContext.HiddenLayer++;
            }
        }
    }


}