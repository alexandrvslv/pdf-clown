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

using BitMiracle.LibTiff.Classic;
using PdfClown.Objects;

using System;
using System.IO;

namespace PdfClown.Bytes.Filters
{
    //BitMiracle.LibTiff not implement JBIG2!!!!!!!!!!!!
    public sealed class JBIG2Filter : Filter
    {
        #region dynamic
        #region constructors
        internal JBIG2Filter()
        { }
        #endregion

        #region interface
        #region public
        public override byte[] Decode(byte[] data, int offset, int length, PdfDirectObject parameters, PdfDictionary header)
        {
            var imageParams = header;
            const short TIFF_BIGENDIAN = 0x4d4d;
            const short TIFF_LITTLEENDIAN = 0x4949;
            const int ifd_length = 10;
            const int header_length = 10 + (ifd_length * 12 + 4);
            var width = imageParams.Resolve(PdfName.Width) as PdfInteger;
            var height = imageParams.Resolve(PdfName.Height) as PdfInteger;
            var bpp = imageParams.Resolve(PdfName.BitsPerComponent) as PdfInteger;
            var flag = imageParams.Resolve(PdfName.ImageMask) as PdfBoolean;
            using (MemoryStream output = new MemoryStream())
            {
                output.Write(BitConverter.GetBytes(BitConverter.IsLittleEndian ? TIFF_LITTLEENDIAN : TIFF_BIGENDIAN), 0, 2); // tiff_magic (big/little endianness)
                output.Write(BitConverter.GetBytes((uint)42), 0, 2);         // tiff_version
                output.Write(BitConverter.GetBytes((uint)8), 0, 4);          // first_ifd (Image file directory) / offset
                output.Write(BitConverter.GetBytes((uint)ifd_length), 0, 2); // ifd_length, number of tags (ifd entries)

                // Dictionary should be in order based on the TiffTag value
                WriteTiffTag(output, TiffTag.SUBFILETYPE, TiffType.LONG, 1, 0);
                WriteTiffTag(output, TiffTag.IMAGEWIDTH, TiffType.LONG, 1, (uint)width.RawValue);
                WriteTiffTag(output, TiffTag.IMAGELENGTH, TiffType.LONG, 1, (uint)height.RawValue);
                WriteTiffTag(output, TiffTag.BITSPERSAMPLE, TiffType.SHORT, 1, (uint)bpp.RawValue);
                WriteTiffTag(output, TiffTag.COMPRESSION, TiffType.SHORT, 1, (uint)Compression.JBIG); // CCITT Group 4 fax encoding.
                WriteTiffTag(output, TiffTag.PHOTOMETRIC, TiffType.SHORT, 1, flag?.BooleanValue ?? false
                    ? (uint)(int)Photometric.MINISWHITE : (uint)(int)Photometric.MINISBLACK); // WhiteIsZero
                WriteTiffTag(output, TiffTag.STRIPOFFSETS, TiffType.LONG, 1, header_length);
                WriteTiffTag(output, TiffTag.SAMPLESPERPIXEL, TiffType.SHORT, 1, 1);
                WriteTiffTag(output, TiffTag.ROWSPERSTRIP, TiffType.LONG, 1, (uint)height.RawValue);
                WriteTiffTag(output, TiffTag.STRIPBYTECOUNTS, TiffType.LONG, 1, (uint)length);

                // Next IFD Offset
                output.Write(BitConverter.GetBytes((uint)0), 0, 4);

                output.Write(data, offset, length);
                return output.ToArray();
            }
        }

        public override byte[] Encode(byte[] data, int offset, int length, PdfDirectObject parameters, PdfDictionary header)
        {
            return data;
        }
        #endregion

        #region private
        private static void WriteTiffTag(System.IO.Stream stream, TiffTag tag, TiffType type, uint count, uint value)
        {
            if (stream == null) return;

            stream.Write(BitConverter.GetBytes((uint)tag), 0, 2);
            stream.Write(BitConverter.GetBytes((uint)type), 0, 2);
            stream.Write(BitConverter.GetBytes(count), 0, 4);
            stream.Write(BitConverter.GetBytes(value), 0, 4);
        }


        #endregion
        #endregion
        #endregion
    }
}