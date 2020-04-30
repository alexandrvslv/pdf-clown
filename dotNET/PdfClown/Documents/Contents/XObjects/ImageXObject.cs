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

using PdfClown;
using PdfClown.Documents;
using PdfClown.Documents.Contents.ColorSpaces;
using PdfClown.Objects;

using System;
using SkiaSharp;
using System.IO;
using System.Text;
using BitMiracle.LibTiff.Classic;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using PdfClown.Bytes;

namespace PdfClown.Documents.Contents.XObjects
{
    /**
      <summary>Image external object [PDF:1.6:4.8.4].</summary>
    */
    [PDF(VersionEnum.PDF10)]
    public sealed class ImageXObject : XObject, IImageObject
    {
        #region dynamic
        private ColorSpace colorSpace;
        private int? bitsPerComponent;
        private ImageXObject smask;
        private SKSize? size;

        #region constructors
        public ImageXObject(Document context, PdfStream baseDataObject) : base(context, baseDataObject)
        {
            /*
              NOTE: It's caller responsability to adequately populate the stream
              header and body in order to instantiate a valid object; header entries like
              'Width', 'Height', 'ColorSpace', 'BitsPerComponent' MUST be defined
              appropriately.
            */
            baseDataObject.Header[PdfName.Subtype] = PdfName.Image;
        }

        public ImageXObject(PdfDirectObject baseObject) : base(baseObject)
        { }

        #endregion

        #region interface
        #region public
        /**
          <summary>Gets the number of bits per color component.</summary>
        */
        public int BitsPerComponent => (bitsPerComponent ?? (bitsPerComponent = ((PdfInteger)BaseDataObject.Header[PdfName.BitsPerComponent]).RawValue)).Value;

        /**
          <summary>Gets the color space in which samples are specified.</summary>
        */
        public ColorSpace ColorSpace => colorSpace ?? (colorSpace = ColorSpace.Wrap(BaseDataObject.Header[PdfName.ColorSpace]));

        public IBuffer Data => Stream.GetBody(false);

        public PdfDirectObject Filter => Stream.Filter;

        public override SKMatrix Matrix
        {
            get
            {
                SKSize size = Size;
                /*
                  NOTE: Image-space-to-user-space matrix is [1/w 0 0 1/h 0 0],
                  where w and h are the width and height of the image in samples [PDF:1.6:4.8.3].
                */
                return new SKMatrix
                {
                    Values = new float[] { 1f / size.Width, 0, 0, 0, 1f / size.Height, 0, 0, 0, 1 }
                };
            }
            set
            {/* NOOP. */}
        }

        public PdfDirectObject Parameters => Stream.Parameters;

        public PdfDictionary Header => Stream.Header;

        /**
          <summary>Gets the size of the image (in samples).</summary>
        */
        public override SKSize Size
        {
            get => (size ?? (size = new SKSize(
                  ((PdfInteger)BaseDataObject.Header[PdfName.Width]).RawValue,
                  ((PdfInteger)BaseDataObject.Header[PdfName.Height]).RawValue
                  ))).Value;
            set => throw new NotSupportedException();
        }

        public SKBitmap LoadImage(GraphicsState state)
        {
            if (Document.Cache.TryGetValue((PdfReference)BaseObject, out var existingBitmap))
            {
                return (SKBitmap)existingBitmap;
            }
            var stream = BaseDataObject as PdfStream;

            SKBitmap image = ImageLoader.Load(this, state);
            Document.Cache[(PdfReference)BaseObject] = image;
            return image;
        }

        public IImageObject SMask
        {
            get
            {
                var maskData = BaseDataObject.Header[PdfName.SMask];
                return maskData == null ? null : (smask ?? (smask = Wrap<ImageXObject>(maskData)));
            }
        }

        public PdfStream Stream => BaseDataObject as PdfStream;

        public PdfArray Matte => BaseDataObject.Header.Resolve(PdfName.Matte) as PdfArray;

        public PdfArray Decode => BaseDataObject.Header.Resolve(PdfName.Decode) as PdfArray;

        public bool ImageMask => (BaseDataObject.Header.Resolve(PdfName.ImageMask) as PdfBoolean)?.BooleanValue ?? false;

        #endregion
        #endregion
        #endregion
    }
}