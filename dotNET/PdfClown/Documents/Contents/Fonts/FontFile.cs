/*
  Copyright 2006-2015 Stefano Chizzolini. http://www.pdfclown.org

  Contributors:
    * Stefano Chizzolini (original code developer, http://www.stefanochizzolini.it)
    * Manuel Guilbault (code contributor [FIX:27], manuel.guilbault at gmail.com)

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


namespace PdfClown.Documents.Contents.Fonts
{
    public class FontFile : PdfObjectWrapper<PdfStream>
    {
        public FontFile(PdfDirectObject baseObject) : base(baseObject)
        { }

        public FontFile(Document document, PdfStream stream) : base(document, stream)
        { }


        public string Subtype
        {
            get => ((PdfName)Dictionary[PdfName.Subtype])?.StringValue;
            set => Dictionary[PdfName.Subtype] = new PdfName(value);
        }

        public int Length1
        {
            get => ((PdfInteger)Dictionary[PdfName.Length1])?.IntValue ?? 0;
            set => Dictionary[PdfName.Length1] = new PdfInteger(value);
        }

        public int Length2
        {
            get => ((PdfInteger)Dictionary[PdfName.Length2])?.IntValue ?? 0;
            set => Dictionary[PdfName.Length2] = new PdfInteger(value);
        }

        public int Length3
        {
            get => ((PdfInteger)Dictionary[PdfName.Length3])?.IntValue ?? 0;
            set => Dictionary[PdfName.Length3] = new PdfInteger(value);
        }


    }
}