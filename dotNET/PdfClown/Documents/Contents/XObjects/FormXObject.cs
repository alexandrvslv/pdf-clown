/*
  Copyright 2006-2015 Stefano Chizzolini. http://www.pdfclown.org

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

using PdfClown.Documents;
using PdfClown.Documents.Contents;
using PdfClown.Documents.Contents.Composition;
using PdfClown.Documents.Contents.Objects;
using PdfClown.Documents.Interchange.Metadata;
using PdfClown.Files;
using PdfClown.Objects;

using System;
using SkiaSharp;
using System.Collections.Generic;
using PdfClown.Documents.Contents.Fonts;
using PdfClown.Documents.Contents.ColorSpaces;
using PdfClown.Util.Math.Geom;

namespace PdfClown.Documents.Contents.XObjects
{
    /**
      <summary>Form external object [PDF:1.6:4.9].</summary>
    */
    [PDF(VersionEnum.PDF10)]
    public sealed class FormXObject : XObject, IContentContext
    {
        public static new FormXObject Wrap(PdfDirectObject baseObject)
        {
            if (baseObject == null)
                return null;
            if (baseObject.Wrapper is FormXObject formObject)
                return formObject;

            var header = ((PdfStream)baseObject.Resolve()).Header;
            var subtype = header.Get<PdfName>(PdfName.Subtype);
            /*
              NOTE: Sometimes the form stream's header misses the mandatory Subtype entry; therefore, here
              we force integrity for convenience (otherwise, content resource allocation may fail, for
              example in case of Acroform flattening).
            */
            if (subtype == null && header.ContainsKey(PdfName.BBox))
            {
                header[PdfName.Subtype] = PdfName.Form;
            }
            else if (!subtype.Equals(PdfName.Form))
            {
                return null;
            }

            return new FormXObject(baseObject);
        }

        private SKPicture picture;
        private SKMatrix? matrix;
        private Stack<GraphicsState> states;

        /**
         <summary>Creates a new form within the specified document context.</summary>
         <param name="context">Document where to place this form.</param>
         <param name="size">Form size.</param>
       */
        public FormXObject(PdfDocument context, SKSize size)
            : this(context, SKRect.Create(size))
        { }

        /**
          <summary>Creates a new form within the specified document context.</summary>
          <param name="context">Document where to place this form.</param>
          <param name="box">Form box.</param>
        */
        public FormXObject(PdfDocument context, SKRect box)
            : base(context)
        {
            BaseDataObject.Header[PdfName.Subtype] = PdfName.Form;
            Box = box;
        }

        public FormXObject(PdfDirectObject baseObject)
            : base(baseObject)
        { }

        public override SKMatrix Matrix
        {
            //NOTE: Form-space-to-user-space matrix is identity [1 0 0 1 0 0] by default,
            //but may be adjusted by setting the matrix entry in the form dictionary [PDF:1.6:4.9].
            get => matrix ??= BaseDataObject.Header.Resolve(PdfName.Matrix) is PdfArray array
                ? new SKMatrix
                {
                    ScaleX = array.GetFloat(0),
                    SkewY = array.GetFloat(1),
                    SkewX = array.GetFloat(2),
                    ScaleY = array.GetFloat(3),
                    TransX = array.GetFloat(4),
                    TransY = array.GetFloat(5),
                    Persp2 = 1
                }
                : SKMatrix.Identity;
            set
            {
                matrix = value;
                BaseDataObject.Header[PdfName.Matrix] = new PdfArray(6)
                {
                    PdfReal.Get(value.ScaleX),
                    PdfReal.Get(value.SkewY),
                    PdfReal.Get(value.SkewX),
                    PdfReal.Get(value.ScaleY),
                    PdfReal.Get(value.TransX),
                    PdfReal.Get(value.TransY)
                };
            }
        }

        public TransparencyXObject Group => Wrap<TransparencyXObject>(BaseDataObject.Header[PdfName.Group]);

        public override SKSize Size
        {
            get
            {
                var box = BaseDataObject.Header.Get<PdfArray>(PdfName.BBox);
                return new SKSize(
                  box.GetFloat(2) - box.GetFloat(0),
                  box.GetFloat(3) - box.GetFloat(1));
            }
            set
            {
                var boxObject = BaseDataObject.Header.Get<PdfArray>(PdfName.BBox);
                boxObject[2] = PdfReal.Get(value.Width + boxObject.GetFloat(0));
                boxObject[3] = PdfReal.Get(value.Height + boxObject.GetFloat(1));
            }
        }

        public SKRect Box
        {
            get => Wrap<Rectangle>(BaseDataObject.Header[PdfName.BBox])?.ToRect() ?? SKRect.Empty;
            set
            {
                var newValue = PrimitiveExtensions.Round(value);
                if (Box != newValue)
                {
                    BaseDataObject.Header[PdfName.BBox] = new Rectangle(newValue).BaseDataObject;
                }
            }
        }

        public ContentWrapper Contents => ContentWrapper.Wrap(BaseObject, this);

        public void ClearContents()
        {
            BaseObject.ContentsWrapper = null;
            InvalidatePicture();
        }

        public SKPicture Render(ContentScanner parentLevel)
        {
            if (picture != null)
                return picture;
            var parentState = parentLevel?.State;
            SoftMask mask = parentState?.SMask;
            var box = Box;
            using (var recorder = new SKPictureRecorder())
            using (var canvas = recorder.BeginRecording(box))
            {
                if (mask != null)
                {
                    parentState.SMask = null;
                    if (!mask.SubType.Equals(PdfName.Luminosity))
                    {
                        // alpha
                        canvas.Clear(SKColors.Transparent);
                    }
                    else if (Group.ColorSpace is ColorSpace colorSpace)
                    {
                        var backgroundColorArray = (IList<PdfDirectObject>)mask.BackColor;
                        if (backgroundColorArray == null || backgroundColorArray.Count < colorSpace.ComponentCount)
                        {
                            backgroundColorArray = new List<PdfDirectObject>();
                            for (int i = 0; i < colorSpace.ComponentCount; i++)
                                backgroundColorArray.Add(new PdfReal(0));
                        }
                        var backgroundColor = colorSpace.GetColor(backgroundColorArray, null);
                        var backgroundColorSK = colorSpace.GetSKColor(backgroundColor, 0);

                        canvas.Clear(backgroundColorSK);
                    }
                    //InitialMatrix = mask.InitialMatrix;
                }

                Render(canvas, box.Size, false, parentLevel);
                if (mask != null)
                {
                    parentState.SMask = mask;
                }
                return picture = recorder.EndRecording();
            }
        }

        public void Render(SKCanvas context, SKSize size, bool clearContext = true)
            => Render(context, size, clearContext, null);

        public void Render(SKCanvas context, SKSize size, bool clearContext = true, ContentScanner baseScanner = null)
        {
            ClearContents();
            var scanner = new ContentScanner(this, baseScanner, context, size)
            {
                ClearContext = clearContext
            };
            scanner.Render(context, size);
        }

        public Resources Resources
        {
            get => Wrap<Resources>(BaseDataObject.Header.GetOrCreate<PdfDictionary>(PdfName.Resources));
            set => BaseDataObject.Header[PdfName.Resources] = PdfObjectWrapper.GetBaseObject(value);
        }

        public RotationEnum Rotation => RotationEnum.Downward;

        public int Rotate => 0;

        public SKMatrix RotateMatrix => SKMatrix.Identity;

        public SKMatrix TextMatrix => SKMatrix.Identity;

        public List<ITextString> Strings { get; } = new List<ITextString>();

        public AppDataCollection AppData
        {
            get => AppDataCollection.Wrap(BaseDataObject.Header.GetOrCreate<PdfDictionary>(PdfName.PieceInfo), this);
        }

        public DateTime? ModificationDate => BaseDataObject.Header.GetNDate(PdfName.LastModified);

        public SKMatrix InitialMatrix { get; internal set; } = SKMatrix.Identity;

        public Stack<GraphicsState> GetGraphicsStateContext() => states ??= new Stack<GraphicsState>();

        public AppData GetAppData(PdfName appName) => AppData.Ensure(appName);

        public void Touch(PdfName appName) => Touch(appName, DateTime.Now);

        public void Touch(PdfName appName, DateTime modificationDate)
        {
            GetAppData(appName).ModificationDate = modificationDate;
            BaseDataObject.Header[PdfName.LastModified] = PdfDate.Get(modificationDate);
        }

        public ContentObject ToInlineObject(PrimitiveComposer composer)
        {
            throw new NotImplementedException();
        }

        public XObject ToXObject(PdfDocument context) => (XObject)Clone(context);

        internal void InvalidatePicture()
        {
            picture?.Dispose();
            picture = null;
        }

        public PdfName GetDefaultFont(out Font defaultFont, FontName fontName = FontName.Helvetica)
        {
            // Retrieving the font to define the default appearance...
            PdfName defaultFontName = null;
            defaultFont = null;
            defaultFontName = null;
            {
                // Field fonts.
                FontResources normalAppearanceFonts = Resources.Fonts;
                foreach (KeyValuePair<PdfName, Font> entry in normalAppearanceFonts)
                {
                    if (!entry.Value.Symbolic)
                    {
                        defaultFont = entry.Value;
                        defaultFontName = entry.Key;
                        break;
                    }
                }
                if (defaultFontName == null)
                {
                    // Common fonts.
                    FontResources formFonts = Document.Form.Resources.Fonts;
                    foreach (KeyValuePair<PdfName, Font> entry in formFonts)
                    {
                        if (!entry.Value.Symbolic && !entry.Value.IsStandard14)
                        {
                            defaultFont = entry.Value;
                            defaultFontName = entry.Key;
                            break;
                        }
                    }
                    if (defaultFontName == null)
                    {
                        //TODO:manage name collision!
                        formFonts[defaultFontName = PdfName.Get("defaultTTF")] = defaultFont = FontType0.Load(Document, fontName);
                    }
                    normalAppearanceFonts[defaultFontName] = defaultFont;
                }
            }

            return defaultFontName;
        }
    }
}