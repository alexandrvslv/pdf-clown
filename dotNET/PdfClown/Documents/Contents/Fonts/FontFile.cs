﻿/*
  Copyright 2006-2015 Stefano Chizzolini. http://www.pdfclown.org

  Contributors:
    * Stefano Chizzolini (original code developer, http://www.stefanochizzolini.it)
    * Manuel Guilbault (code contributor [FIX:27], manuel.guilbault at gmail.com)

  This file should be part of the source code distribution of "PDF Clown library" (the
  Program): see the accompanying README files for more info.

  This Program is free software; you can redistribute it and/or modify it under the terms
  of the GNU Lesser General Public License as published by the Free Software Foundation;
  either version 3 of the L
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

        public FontFile(PdfDocument document, PdfStream stream) : base(document, stream)
        { }


        public string Subtype
        {
            get => Dictionary.GetString(PdfName.Subtype);
            set => Dictionary.SetName(PdfName.Subtype, value);
        }

        public int Length1
        {
            get => Dictionary.GetInt(PdfName.Length1);
            set => Dictionary.Set(PdfName.Length1, value);
        }

        public int Length2
        {
            get => Dictionary.GetInt(PdfName.Length2);
            set => Dictionary.Set(PdfName.Length2, value);
        }

        public int Length3
        {
            get => Dictionary.GetInt(PdfName.Length3);
            set => Dictionary.Set(PdfName.Length3, value);
        }


    }
}