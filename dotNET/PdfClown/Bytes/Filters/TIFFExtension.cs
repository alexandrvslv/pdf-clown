/*
 * Copyright (c) 2012, Harald Kuhr
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name "TwelveMonkeys" nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
 * "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
 * LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
 * A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
 * EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
 * PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
 * PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
 * LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
 * NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

namespace PdfClown.Bytes.Filters
{
	/**
	 * TIFFExtension
	 *
	 * @author <a href="mailto:harald.kuhr@gmail.com">Harald Kuhr</a>
	 * @author last modified by $Author: haraldk$
	 * @version $Id: TIFFExtension.java,v 1.0 08.05.12 16:45 haraldk Exp$
	 */
	internal static class TIFFExtension
	{
		/** CCITT T.4/Group 3 Fax compression. */
		public const int COMPRESSION_CCITT_T4 = 3;
		/** CCITT T.6/Group 4 Fax compression. */
		public const int COMPRESSION_CCITT_T6 = 4;
		/** LZW Compression. Was baseline, but moved to extension due to license issues in the LZW algorithm. */
		public const int COMPRESSION_LZW = 5;
		/** Deprecated. For backwards compatibility only ("Old-style" JPEG). */
		public const int COMPRESSION_OLD_JPEG = 6;
		/** JPEG Compression (lossy). */
		public const int COMPRESSION_JPEG = 7;
		/** Custom: PKZIP-style Deflate. */
		public const int COMPRESSION_DEFLATE = 32946;
		/** Adobe-style Deflate. */
		public const int COMPRESSION_ZLIB = 8;

		public const int PHOTOMETRIC_SEPARATED = 5;
		public const int PHOTOMETRIC_YCBCR = 6;
		public const int PHOTOMETRIC_CIELAB = 8;
		public const int PHOTOMETRIC_ICCLAB = 9;
		public const int PHOTOMETRIC_ITULAB = 10;

		public const int PLANARCONFIG_PLANAR = 2;

		public const int PREDICTOR_HORIZONTAL_DIFFERENCING = 2;
		public const int PREDICTOR_HORIZONTAL_FLOATINGPOINT = 3;

		public const int FILL_RIGHT_TO_LEFT = 2;

		public const int SAMPLEFORMAT_INT = 2;
		public const int SAMPLEFORMAT_FP = 3;
		public const int SAMPLEFORMAT_UNDEFINED = 4;

		public const int YCBCR_POSITIONING_CENTERED = 1;
		public const int YCBCR_POSITIONING_COSITED = 2;

		/** Deprecated. For backwards compatibility only ("Old-style" JPEG). */
		public const int JPEG_PROC_BASELINE = 1;
		/** Deprecated. For backwards compatibility only ("Old-style" JPEG). */
		public const int JPEG_PROC_LOSSLESS = 14;

		/** For use with Photometric: 5 (Separated), when image data is in CMYK color space. */
		public const int INKSET_CMYK = 1;

		/**
		 * For use with Photometric: 5 (Separated), when image data is in a color space other than CMYK.
		 * See {@link com.twelvemonkeys.imageio.metadata.exif.TIFF#TAG_INK_NAMES InkNames} field for a
		 * description of the inks to be used.
		 */
		public const int INKSET_NOT_CMYK = 2;

		public const int ORIENTATION_TOPRIGHT = 2;
		public const int ORIENTATION_BOTRIGHT = 3;
		public const int ORIENTATION_BOTLEFT = 4;
		public const int ORIENTATION_LEFTTOP = 5;
		public const int ORIENTATION_RIGHTTOP = 6;
		public const int ORIENTATION_RIGHTBOT = 7;
		public const int ORIENTATION_LEFTBOT = 8;

		public const int GROUP3OPT_2DENCODING = 1;
		public const int GROUP3OPT_UNCOMPRESSED = 2;
		public const int GROUP3OPT_FILLBITS = 4;
		public const int GROUP3OPT_BYTEALIGNED = 8;
		public const int GROUP4OPT_UNCOMPRESSED = 2;
		public const int GROUP4OPT_BYTEALIGNED = 4;
		public const int COMPRESSION_CCITT_MODIFIED_HUFFMAN_RLE = 2;
		public const int FILL_LEFT_TO_RIGHT = 1; // Default
	}

}