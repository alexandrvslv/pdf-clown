/*
  Copyright 2007-2011 Stefano Chizzolini. http://www.pdfclown.org

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
using PdfClown.Documents.Contents.XObjects;
using PdfClown.Objects;

using System.Collections.Generic;

namespace PdfClown.Documents.Contents.Objects
{
    /**
      <summary>External object shown in a content stream context [PDF:1.6:4.7].</summary>
    */
    [PDF(VersionEnum.PDF10)]
    public sealed class GraphicsXObject : GraphicsObject, IResourceReference<XObject>
    {
        public static readonly string BeginOperatorKeyword = PaintXObject.OperatorKeyword;
        public static readonly string EndOperatorKeyword = BeginOperatorKeyword;

        public GraphicsXObject(PaintXObject operation) : base(operation)
        { }

        /**
          <summary>Gets the scanner for this object's contents.</summary>
          <param name="context">Scanning context.</param>
        */
        public ContentScanner GetScanner(ContentScanner context) => Operation.GetScanner(context);

        public XObject GetResource(ContentScanner context) => Operation.GetResource(context);

        public PdfName Name
        {
            get => Operation.Name;
            set => Operation.Name = value;
        }

        public override void Scan(GraphicsState state)
        {
            base.Scan(state);
        }

        private PaintXObject Operation => (PaintXObject)Objects[0];
    }
}