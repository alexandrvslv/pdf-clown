/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using PdfClown.Objects;
using System;
using System.IO;

namespace PdfClown.Bytes.Filters
{
    /**
     * Decodes image data that has been encoded using either Group 3 or Group 4
     * CCITT facsimile (fax) encoding, and encodes image data to Group 4.
     *
     * @author Ben Litchfield
     * @author Marcel Kammer
     * @author Paul King
     */
    public class CCITTFaxFilter : Filter
    {

        public override byte[] Decode(byte[] data, int offset, int length, PdfDirectObject parameters, PdfDictionary header)
        {
            // get decode parameters
            PdfDictionary decodeParms = parameters as PdfDictionary;

            // parse dimensions
            int cols = ((IPdfNumber)decodeParms[PdfName.Columns])?.IntValue ?? 1728;
            int rows = ((IPdfNumber)decodeParms[PdfName.Rows])?.IntValue ?? 0;
            int height = ((IPdfNumber)(header[PdfName.Height] ?? header[PdfName.H]))?.IntValue ?? 0;
            if (rows > 0 && height > 0)
            {
                // PDFBOX-771, PDFBOX-3727: rows in DecodeParms sometimes contains an incorrect value
                rows = height;
            }
            else
            {
                // at least one of the values has to have a valid value
                rows = Math.Max(rows, height);
            }

            // decompress data
            int k = ((IPdfNumber)decodeParms[PdfName.K])?.IntValue ?? 0;
            bool encodedByteAlign = ((PdfBoolean)decodeParms[PdfName.EncodedByteAlign])?.BooleanValue ?? false;
            int arraySize = (cols + 7) / 8 * rows;
            // TODO possible options??
            byte[] decompressed = new byte[arraySize];
            int type;
            long tiffOptions;
            if (k == 0)
            {
                tiffOptions = encodedByteAlign ? TIFFExtension.GROUP3OPT_BYTEALIGNED : 0;
                type = TIFFExtension.COMPRESSION_CCITT_MODIFIED_HUFFMAN_RLE;
            }
            else
            {
                if (k > 0)
                {
                    tiffOptions = encodedByteAlign ? TIFFExtension.GROUP3OPT_BYTEALIGNED : 0;
                    tiffOptions |= TIFFExtension.GROUP3OPT_2DENCODING;
                    type = TIFFExtension.COMPRESSION_CCITT_T4;
                }
                else
                {
                    // k < 0
                    tiffOptions = encodedByteAlign ? TIFFExtension.GROUP4OPT_BYTEALIGNED : 0;
                    type = TIFFExtension.COMPRESSION_CCITT_T6;
                }
            }
            using (var encoded = new MemoryStream(data))
            using (var s = new CCITTFaxDecoderStream(encoded, cols, type, TIFFExtension.FILL_LEFT_TO_RIGHT, tiffOptions))
                ReadFromDecoderStream(s, decompressed);

            // invert bitmap
            //bool blackIsOne = ((PdfBoolean)decodeParms[PdfName.BlackIs1])?.BooleanValue ?? false;
            //if (!blackIsOne)
            //{
            //    // Inverting the bitmap
            //    // Note the previous approach with starting from an IndexColorModel didn't work
            //    // reliably. In some cases the image wouldn't be painted for some reason.
            //    // So a safe but slower approach was taken.
            //    InvertBitmap(decompressed);
            //}

            return decompressed;
        }

        private void ReadFromDecoderStream(CCITTFaxDecoderStream decoderStream, byte[] result)
        {
            int pos = 0;
            int read;
            while ((read = decoderStream.Read(result, pos, result.Length - pos)) > -1)
            {
                pos += read;
                if (pos >= result.Length)
                {
                    break;
                }
            }
            decoderStream.Close();
        }

        private void InvertBitmap(byte[] bufferData)
        {
            for (int i = 0, c = bufferData.Length; i < c; i++)
            {
                bufferData[i] = (byte)(~bufferData[i] & 0xFF);
            }
        }


        public override byte[] Encode(byte[] data, int offset, int length, PdfDirectObject parameters, PdfDictionary header)
        {
            PdfDictionary decodeParms = parameters as PdfDictionary;
            int cols = ((IPdfNumber)decodeParms[PdfName.Columns]).IntValue;
            int rows = ((IPdfNumber)decodeParms[PdfName.Rows]).IntValue;

            using (var encoded = new MemoryStream(data))
            using (var ccittFaxEncoderStream = new CCITTFaxEncoderStream(encoded, cols, rows, TIFFExtension.FILL_LEFT_TO_RIGHT))
            {

                foreach (var value in data)
                {
                    ccittFaxEncoderStream.Write(value);
                }
                return encoded.ToArray();
            }

        }
    }
}