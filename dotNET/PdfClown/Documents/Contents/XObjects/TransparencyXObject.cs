﻿/*
  Copyright 2006-2015 Stefano Chizzolini. http://www.pdfclown.org

  Contributors:
    * Stefano Chizzolini (original code developer, http://www.stefanochizzolini.it)
    * Alexandr

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

namespace PdfClown.Documents.Contents.XObjects
{
    public sealed class TransparencyXObject : GroupXObject
    {
        public TransparencyXObject(PdfDocument context, PdfDictionary baseDataObject)
            : base(context, baseDataObject)
        {
            SubType = PdfName.Transparency;
        }

        public TransparencyXObject(PdfDirectObject baseObject) : base(baseObject)
        { }

        public ColorSpaces.ColorSpace ColorSpace
        {
            get => ColorSpaces.ColorSpace.Wrap(BaseDataObject[PdfName.CS]);
            set => BaseDataObject[PdfName.CS] = value?.BaseObject;
        }

        public bool Isolated
        {
            get => BaseDataObject.GetBool(PdfName.I);
            set => BaseDataObject.Set(PdfName.I, value);
        }

        public bool Knockout
        {
            get => BaseDataObject.GetBool(PdfName.K);
            set => BaseDataObject.Set(PdfName.K, value);
        }
    }
}