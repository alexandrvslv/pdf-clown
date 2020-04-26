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

namespace PdfClown.Documents.Contents.XObjects
{
    /**
      <summary>Form external object [PDF:1.6:4.9].</summary>
    */
    [PDF(VersionEnum.PDF10)]
    public sealed class FormXObject : XObject, IContentContext
    {
        private SKPicture picture;
        #region static
        #region interface
        #region public
        public static new FormXObject Wrap(PdfDirectObject baseObject)
        {
            if (baseObject == null)
                return null;
            if (baseObject.Wrapper is FormXObject formObject)
                return formObject;
            if (baseObject is PdfReference pdfReference && pdfReference.DataObject?.Wrapper is FormXObject referenceFormObject)
            {
                baseObject.Wrapper = referenceFormObject;
                return referenceFormObject;
            }
            var header = ((PdfStream)PdfObject.Resolve(baseObject)).Header;
            var subtype = (PdfName)header[PdfName.Subtype];
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
        #endregion
        #endregion
        #endregion

        #region dynamic
        #region constructors
        /**
          <summary>Creates a new form within the specified document context.</summary>
          <param name="context">Document where to place this form.</param>
          <param name="size">Form size.</param>
        */
        public FormXObject(Document context, SKSize size)
            : this(context, SKRect.Create(new SKPoint(0, 0), size))
        { }

        /**
          <summary>Creates a new form within the specified document context.</summary>
          <param name="context">Document where to place this form.</param>
          <param name="box">Form box.</param>
        */
        public FormXObject(Document context, SKRect box)
            : base(context)
        {
            BaseDataObject.Header[PdfName.Subtype] = PdfName.Form;
            Box = box;
        }

        public FormXObject(PdfDirectObject baseObject)
            : base(baseObject)
        { }
        #endregion

        #region interface
        #region public
        public override SKMatrix Matrix
        {
            get
            {
                /*
                  NOTE: Form-space-to-user-space matrix is identity [1 0 0 1 0 0] by default,
                  but may be adjusted by setting the matrix entry in the form dictionary [PDF:1.6:4.9].
                */
                PdfArray matrix = (PdfArray)BaseDataObject.Header.Resolve(PdfName.Matrix);
                if (matrix == null)
                    return SKMatrix.MakeIdentity();
                else
                    return new SKMatrix
                    {
                        ScaleX = ((IPdfNumber)matrix[0]).FloatValue,
                        SkewY = ((IPdfNumber)matrix[1]).FloatValue,
                        SkewX = ((IPdfNumber)matrix[2]).FloatValue,
                        ScaleY = ((IPdfNumber)matrix[3]).FloatValue,
                        TransX = ((IPdfNumber)matrix[4]).FloatValue,
                        TransY = ((IPdfNumber)matrix[5]).FloatValue,
                        Persp2 = 1
                    };
            }
            set => BaseDataObject.Header[PdfName.Matrix] =
                 new PdfArray(
                    PdfReal.Get(value.ScaleX),
                    PdfReal.Get(value.SkewY),
                    PdfReal.Get(value.SkewX),
                    PdfReal.Get(value.ScaleY),
                    PdfReal.Get(value.TransX),
                    PdfReal.Get(value.TransY)
                    )
                ;
        }

        public TransparencyXObject Group => Wrap<TransparencyXObject>(BaseDataObject.Header[PdfName.Group]);

        public override SKSize Size
        {
            get
            {
                PdfArray box = (PdfArray)BaseDataObject.Header.Resolve(PdfName.BBox);
                return new SKSize(
                  ((IPdfNumber)box[2]).FloatValue - ((IPdfNumber)box[0]).FloatValue,
                  ((IPdfNumber)box[3]).FloatValue - ((IPdfNumber)box[1]).FloatValue
                  );
            }
            set
            {
                PdfArray boxObject = (PdfArray)BaseDataObject.Header.Resolve(PdfName.BBox);
                boxObject[2] = PdfReal.Get(value.Width + ((IPdfNumber)boxObject[0]).FloatValue);
                boxObject[3] = PdfReal.Get(value.Height + ((IPdfNumber)boxObject[1]).FloatValue);
            }
        }
        #endregion

        #region internal
        #region IContentContext
        public SKRect Box
        {
            get => Wrap<Rectangle>(BaseDataObject.Header[PdfName.BBox]).ToRect();
            set => BaseDataObject.Header[PdfName.BBox] = new Rectangle(value).BaseDataObject;
        }

        public ContentWrapper Contents => ContentWrapper.Wrap(BaseObject, this);

        public void ClearContents()
        {
            BaseObject.ContentsWrapper = null;
            InvalidatePicture();
        }

        public SKPicture Render(SoftMask mask = null)
        {
            if (picture != null)
                return picture;
            var box = Box;
            using (var recorder = new SKPictureRecorder())
            using (var canvas = recorder.BeginRecording(box))
            {
                if (mask != null)
                {
                    if (!mask.SubType.Equals(PdfName.Luminosity))
                    {
                        // alpha
                        canvas.Clear(SKColors.Transparent);
                    }
                    else
                    {
                        var backgroundColorArray = mask.BackColor;
                        var colorSpace = Group.ColorSpace;
                        var backgroundColor = colorSpace.GetColor(backgroundColorArray, null);
                        var backgroundColorSK = colorSpace.GetSKColor(backgroundColor, 0);

                        canvas.Clear(backgroundColorSK);
                    }
                }

                Render(canvas, box.Size);
                return picture = recorder.EndRecording();
            }
        }

        public void Render(SKCanvas context, SKSize size)
        {
            ClearContents();
            var scanner = new ContentScanner(this, context, size);
            scanner.ClearContext = false;
            scanner.Render(context, size);
        }

        public Resources Resources
        {
            get => Wrap<Resources>(BaseDataObject.Header.Get<PdfDictionary>(PdfName.Resources));
            set => BaseDataObject.Header[PdfName.Resources] = PdfObjectWrapper.GetBaseObject(value);
        }

        public RotationEnum Rotation => RotationEnum.Downward;

        public int Rotate => 0;

        public SKMatrix InitialMatrix => SKMatrix.MakeIdentity();

        public SKMatrix RotateMatrix => SKMatrix.MakeIdentity();

        public SKMatrix TextMatrix => SKMatrix.MakeIdentity();

        public List<ITextString> Strings { get; } = new List<ITextString>();

        #region IAppDataHolder
        public AppDataCollection AppData => AppDataCollection.Wrap(BaseDataObject.Header.Get<PdfDictionary>(PdfName.PieceInfo), this);

        public AppData GetAppData(PdfName appName)
        { return AppData.Ensure(appName); }

        public DateTime? ModificationDate => (DateTime?)PdfSimpleObject<object>.GetValue(BaseDataObject.Header[PdfName.LastModified]);

        public SKMatrix? StartMatrix { get; private set; }

        public void Touch(PdfName appName)
        { Touch(appName, DateTime.Now); }

        public void Touch(PdfName appName, DateTime modificationDate)
        {
            GetAppData(appName).ModificationDate = modificationDate;
            BaseDataObject.Header[PdfName.LastModified] = new PdfDate(modificationDate);
        }
        #endregion

        #region IContentEntity
        public ContentObject ToInlineObject(PrimitiveComposer composer)
        { throw new NotImplementedException(); }

        public XObject ToXObject(Document context)
        { return (XObject)Clone(context); }

        internal void InvalidatePicture()
        {
            picture?.Dispose();
            picture = null;
        }

        public void OnSetCtm(SKMatrix ctm)
        {
            if (StartMatrix == null)
                StartMatrix = ctm;
        }
        #endregion
        #endregion
        #endregion
        #endregion
        #endregion
    }
}