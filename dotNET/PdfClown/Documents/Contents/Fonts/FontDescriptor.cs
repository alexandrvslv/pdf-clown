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
    public class FontDescriptor : PdfObjectWrapper<PdfDictionary>
    {
        public FontDescriptor(PdfDirectObject baseObject) : base(baseObject)
        { }

        public FontDescriptor(Document document, PdfDictionary baseObject) : base(document, baseObject)
        { }

        public string FontName
        {
            get => ((PdfName)Dictionary[PdfName.FontName])?.StringValue;
            set => Dictionary[PdfName.FontName] = new PdfName(value);
        }

        public string FontFamily
        {
            get => ((PdfString)Dictionary[PdfName.FontFamily])?.StringValue;
            set => Dictionary[PdfName.FontFamily] = new PdfString(value);
        }

        public float? FontStretch
        {
            get => ((PdfReal)Dictionary[PdfName.FontStretch])?.FloatValue;
            set => Dictionary[PdfName.FontStretch] = value.HasValue ? new PdfReal((float)value) : null;
        }

        public float? FontWeight
        {
            get => ((PdfReal)Dictionary[PdfName.FontWeight])?.FloatValue;
            set => Dictionary[PdfName.FontWeight] = value.HasValue ? new PdfReal((float)value) : null;
        }

        public int Flags
        {
            get => ((PdfInteger)Dictionary[PdfName.Flags])?.IntValue ?? 0;
            set => Dictionary[PdfName.Flags] = new PdfInteger(value);
        }

        public Rectangle FontBBox
        {
            get => Wrap<Rectangle>(Dictionary.Resolve<PdfArray>(PdfName.FontBBox));
            set => Dictionary[PdfName.FontBBox] = value?.BaseObject;
        }

        public float ItalicAngle
        {
            get => ((PdfReal)Dictionary[PdfName.ItalicAngle])?.FloatValue ?? 0F;
            set => Dictionary[PdfName.ItalicAngle] = new PdfReal(value);
        }

        public float Ascent
        {
            get => ((PdfReal)Dictionary[PdfName.Ascent])?.FloatValue ?? 0F;
            set => Dictionary[PdfName.Ascent] = new PdfReal(value);
        }

        public float Descent
        {
            get => ((PdfReal)Dictionary[PdfName.Descent])?.FloatValue ?? 0F;
            set => Dictionary[PdfName.Descent] = new PdfReal(value);
        }

        public float? Leading
        {
            get => ((PdfReal)Dictionary[PdfName.Leading])?.FloatValue;
            set => Dictionary[PdfName.Leading] = value.HasValue ? new PdfReal((float)value) : null;
        }

        public float? CapHeight
        {
            get => ((PdfReal)Dictionary[PdfName.CapHeight])?.FloatValue;
            set => Dictionary[PdfName.CapHeight] = value.HasValue ? new PdfReal((float)value) : null;
        }

        public float? XHeight
        {
            get => ((PdfReal)Dictionary[PdfName.XHeight])?.FloatValue;
            set => Dictionary[PdfName.XHeight] = value.HasValue ? new PdfReal((float)value) : null;
        }

        public float StemV
        {
            get => ((PdfReal)Dictionary[PdfName.StemV])?.FloatValue ?? 0F;
            set => Dictionary[PdfName.StemV] = new PdfReal((float)value);
        }

        public float StemH
        {
            get => ((PdfReal)Dictionary[PdfName.StemH])?.FloatValue ?? 0F;
            set => Dictionary[PdfName.StemH] = new PdfReal((float)value);
        }

        public float? AvgWidth
        {
            get => ((PdfReal)Dictionary[PdfName.AvgWidth])?.FloatValue;
            set => Dictionary[PdfName.AvgWidth] = value.HasValue ? new PdfReal((float)value) : null;
        }

        public float? MaxWidth
        {
            get => ((PdfReal)Dictionary[PdfName.MaxWidth])?.FloatValue;
            set => Dictionary[PdfName.MaxWidth] = value.HasValue ? new PdfReal((float)value) : null;
        }

        public float? MissingWidth
        {
            get => ((PdfReal)Dictionary[PdfName.MissingWidth])?.FloatValue;
            set => Dictionary[PdfName.MissingWidth] = value.HasValue ? new PdfReal((float)value) : null;
        }

        public FontFile FontFile
        {
            get => Wrap<FontFile>((PdfDirectObject)Dictionary.Resolve(PdfName.FontFile));
            set => Dictionary[PdfName.FontFile] = value?.BaseObject;
        }

        public FontFile FontFile2
        {
            get => Wrap<FontFile>((PdfDirectObject)Dictionary.Resolve(PdfName.FontFile2));
            set => Dictionary[PdfName.FontFile2] = value?.BaseObject;
        }

        public FontFile FontFile3
        {
            get => Wrap<FontFile>((PdfDirectObject)Dictionary.Resolve(PdfName.FontFile3));
            set => Dictionary[PdfName.FontFile3] = value?.BaseObject;
        }

        public PdfString CharSet
        {
            get => (PdfString)Dictionary.Resolve(PdfName.CharSet);
            set => Dictionary[PdfName.CharSet] = value;
        }

        //CID Font Specific
        public string Lang
        {
            get => ((PdfName)Dictionary[PdfName.Lang])?.StringValue;
            set => Dictionary[PdfName.Lang] = new PdfName(value);
        }

        public Style Style
        {
            get => Wrap<Style>((PdfDirectObject)Dictionary.Resolve(PdfName.Style));
            set => Dictionary[PdfName.Style] = value?.BaseObject;
        }

        public PdfDictionary FD
        {
            get => (PdfDictionary)Dictionary.Resolve(PdfName.FD);
            set => Dictionary[PdfName.FD] = value?.Reference;
        }

        public PdfStream CIDSet
        {
            get => (PdfStream)Dictionary.Resolve(PdfName.CIDSet);
            set => Dictionary[PdfName.CIDSet] = value?.Reference;
        }
    }

    public class Style : PdfObjectWrapper<PdfDictionary>
    {
        public Style(PdfDirectObject baseObject) : base(baseObject)
        { }

        public Style(Document document, PdfDictionary dictionary) : base(document, dictionary)
        { }

    }
}