﻿/*
  Copyright 2009-2015 Stefano Chizzolini. http://www.pdfclown.org

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

namespace PdfClown.Documents.Contents.Objects
{
    public sealed class ShowTextToNextLineNoSpace : ShowTextToNextLine
    {
        /// <summary>Specifies no text state parameter
        /// (just uses the current settings).</summary>
        public static readonly string OperatorKeyword = "'";


        /// <param name="text">Text encoded using current font's encoding.</param>
        public ShowTextToNextLineNoSpace(byte[] text)
            : base(OperatorKeyword, text)
        { }

        public ShowTextToNextLineNoSpace(PdfArray operands)
            : base(OperatorKeyword, operands)
        { }

        protected override PdfString TextElement
        {
            get => (PdfString)operands.Get(0);
            set => operands.SetSimple(0, value);
        }

    }
}