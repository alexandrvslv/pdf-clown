/*
  Copyright 2006-2013 Stefano Chizzolini. http://www.pdfclown.org

  Contributors:
    
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

using FreeImageAPI;
using PdfClown.Objects;

using System;
using System.IO;

namespace PdfClown.Bytes.Filters
{
    public sealed class JPXFilter : Filter
    {
        #region dynamic
        #region constructors
        internal JPXFilter()
        { }
        #endregion

        #region interface
        #region public
        public override byte[] Decode(byte[] data, int offset, int length, PdfDirectObject parameters, PdfDictionary header)
        {
            var imageParams = header;
            var width = imageParams.Resolve(PdfName.Width) as PdfInteger;
            var height = imageParams.Resolve(PdfName.Height) as PdfInteger;
            var bpp = imageParams.Resolve(PdfName.BitsPerComponent) as PdfInteger;
            var flag = imageParams.Resolve(PdfName.ImageMask) as PdfBoolean;
            using (var output = new MemoryStream())
            using (var input = new MemoryStream(data, offset, length))
            {
                var bmp = FreeImage.LoadFromStream(input);
                FreeImage.SaveToStream(bmp, output, FREE_IMAGE_FORMAT.FIF_JPEG, FREE_IMAGE_SAVE_FLAGS.JPEG_OPTIMIZE);
                FreeImage.Unload(bmp);

                return output.ToArray();
            }
        }

        public override byte[] Encode(byte[] data, int offset, int length, PdfDirectObject parameters, PdfDictionary header)
        {
            using (var output = new MemoryStream())
            using (var input = new MemoryStream(data, offset, length))
            {
                var bmp = FreeImage.LoadFromStream(input);

                FreeImage.SaveToStream(bmp, output, FREE_IMAGE_FORMAT.FIF_JP2, FREE_IMAGE_SAVE_FLAGS.DEFAULT);

                FreeImage.Unload(bmp);

                return output.ToArray();
            }
        }
        #endregion

        #region private
        #endregion
        #endregion
        #endregion
    }
}