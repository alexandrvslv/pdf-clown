/*
 * Copyright (c) 2013, Harald Kuhr
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
using PdfClown.Objects;
using System;
using System.IO;

namespace PdfClown.Bytes.Filters
{
    /**
     * CCITT Modified Group 4 (T6) fax compression.
     *
     * @author <a href="mailto:mail@schmidor.de">Oliver Schmidtmer</a>
     *
     * Taken from commit 047884e3d9e1b30516c79b147ead763303dc9bcb of 21.4.2016 from
     * twelvemonkeys/imageio/plugins/tiff/CCITTFaxEncoderStream.java
     *
     * Initial changes for PDFBox:
     * - removed Validate
     * - G4 compression only
     * - removed options
     */
    internal sealed class CCITTFaxEncoderStream : BinaryWriter
    {

        private int currentBufferLength = 0;
        private readonly byte[] inputBuffer;
        private readonly int inputBufferLength;
        private readonly int columns;
        private readonly int rows;

        private int[] changesCurrentRow;
        private int[] changesReferenceRow;
        private int currentRow = 0;
        private int changesCurrentRowLength = 0;
        private int changesReferenceRowLength = 0;
        private byte outputBuffer = 0;
        private byte outputBufferBitLength = 0;
        private readonly int fillOrder;
        private readonly System.IO.Stream stream;

        public CCITTFaxEncoderStream(System.IO.Stream stream, int columns, int rows, int fillOrder)
        {

            this.stream = stream;
            this.columns = columns;
            this.rows = rows;
            this.fillOrder = fillOrder;

            this.changesReferenceRow = new int[columns];
            this.changesCurrentRow = new int[columns];

            inputBufferLength = (columns + 7) / 8;
            inputBuffer = new byte[inputBufferLength];
        }


        public override void Write(int b)
        {
            inputBuffer[currentBufferLength] = (byte)b;
            currentBufferLength++;

            if (currentBufferLength == inputBufferLength)
            {
                EncodeRow();
                currentBufferLength = 0;
            }
        }


        public override void Flush()
        {
            base.Flush();
        }


        public override void Close()
        {
            base.Close();
        }

        private void EncodeRow()
        {
            currentRow++;
            int[] tmp = changesReferenceRow;
            changesReferenceRow = changesCurrentRow;
            changesCurrentRow = tmp;
            changesReferenceRowLength = changesCurrentRowLength;
            changesCurrentRowLength = 0;

            int index = 0;
            bool white = true;
            while (index < columns)
            {
                int byteIndex = index / 8;
                int bit = index % 8;
                if ((((inputBuffer[byteIndex] >> (7 - bit)) & 1) == 1) == (white))
                {
                    changesCurrentRow[changesCurrentRowLength] = index;
                    changesCurrentRowLength++;
                    white = !white;
                }
                index++;
            }

            EncodeRowType6();

            if (currentRow == rows)
            {
                WriteEOL();
                WriteEOL();
                Fill();
            }
        }


        private void EncodeRowType6()
        {
            Encode2D();
        }

        private int[] GetNextChanges(int pos, bool white)
        {
            int[] result = new int[] { columns, columns };
            for (int i = 0; i < changesCurrentRowLength; i++)
            {
                if (pos < changesCurrentRow[i] || (pos == 0 && white))
                {
                    result[0] = changesCurrentRow[i];
                    if ((i + 1) < changesCurrentRowLength)
                    {
                        result[1] = changesCurrentRow[i + 1];
                    }
                    break;
                }
            }

            return result;
        }

        private void WriteRun(int runLength, bool white)
        {
            int nonterm = runLength / 64;
            Code[]
            codes = white ? WHITE_NONTERMINATING_CODES : BLACK_NONTERMINATING_CODES;
            while (nonterm > 0)
            {
                if (nonterm >= codes.Length)
                {
                    Write(codes[codes.Length - 1].code, codes[codes.Length - 1].length);
                    nonterm -= codes.Length;
                }
                else
                {
                    Write(codes[nonterm - 1].code, codes[nonterm - 1].length);
                    nonterm = 0;
                }
            }

            Code c = white ? WHITE_TERMINATING_CODES[runLength % 64] : BLACK_TERMINATING_CODES[runLength % 64];
            Write(c.code, c.length);
        }

        private void Encode2D()
        {
            bool white = true;
            int index = 0; // a0
            while (index < columns)
            {
                int[] nextChanges = GetNextChanges(index, white); // a1, a2

                int[] nextRefs = GetNextRefChanges(index, white); // b1, b2

                int difference = nextChanges[0] - nextRefs[0];
                if (nextChanges[0] > nextRefs[1])
                {
                    // PMODE
                    Write(1, 4);
                    index = nextRefs[1];
                }
                else if (difference > 3 || difference < -3)
                {
                    // HMODE
                    Write(1, 3);
                    WriteRun(nextChanges[0] - index, white);
                    WriteRun(nextChanges[1] - nextChanges[0], !white);
                    index = nextChanges[1];

                }
                else
                {
                    // VMODE
                    switch (difference)
                    {
                        case 0:
                            Write(1, 1);
                            break;
                        case 1:
                            Write(3, 3);
                            break;
                        case 2:
                            Write(3, 6);
                            break;
                        case 3:
                            Write(3, 7);
                            break;
                        case -1:
                            Write(2, 3);
                            break;
                        case -2:
                            Write(2, 6);
                            break;
                        case -3:
                            Write(2, 7);
                            break;
                        default:
                            break;
                    }
                    white = !white;
                    index = nextRefs[0] + difference;
                }
            }
        }

        private int[] GetNextRefChanges(int a0, bool white)
        {
            int[] result = new int[] { columns, columns };
            for (int i = (white ? 0 : 1); i < changesReferenceRowLength; i += 2)
            {
                if (changesReferenceRow[i] > a0 || (a0 == 0 && i == 0))
                {
                    result[0] = changesReferenceRow[i];
                    if ((i + 1) < changesReferenceRowLength)
                    {
                        result[1] = changesReferenceRow[i + 1];
                    }
                    break;
                }
            }
            return result;
        }

        private void Write(int code, int codeLength)
        {

            for (int i = 0; i < codeLength; i++)
            {
                bool codeBit = ((code >> (codeLength - i - 1)) & 1) == 1;
                if (fillOrder == TIFFExtension.FILL_LEFT_TO_RIGHT)
                {
                    outputBuffer = (byte)(outputBuffer | (codeBit ? 1 << (7 - ((outputBufferBitLength) % 8)) : 0));
                }
                else
                {
                    outputBuffer = (byte)(outputBuffer | (codeBit ? 1 << (((outputBufferBitLength) % 8)) : 0));
                }
                outputBufferBitLength++;

                if (outputBufferBitLength == 8)
                {
                    base.Write(outputBuffer);
                    ClearOutputBuffer();
                }
            }
        }

        private void WriteEOL()
        {
            Write(1, 12);
        }

        private void Fill()
        {
            if (outputBufferBitLength != 0)
            {
                base.Write(outputBuffer);
            }
            ClearOutputBuffer();
        }

        private void ClearOutputBuffer()
        {
            outputBuffer = 0;
            outputBufferBitLength = 0;
        }

        private class Code
        {
            public Code(int code, int length)
            {
                this.code = code;
                this.length = length;
            }

            readonly internal int code;
            readonly internal int length;
        }

        private static readonly Code[] WHITE_TERMINATING_CODES;

        private static readonly Code[] WHITE_NONTERMINATING_CODES;

        private static readonly Code[] BLACK_TERMINATING_CODES;

        private static readonly Code[] BLACK_NONTERMINATING_CODES;

        static CCITTFaxEncoderStream()
        {
            // Setup HUFFMAN Codes
            WHITE_TERMINATING_CODES = new Code[64];
            WHITE_NONTERMINATING_CODES = new Code[40];
            for (int i = 0; i < CCITTFaxDecoderStream.WHITE_CODES.Length; i++)
            {
                int bitLength = i + 4;
                for (int j = 0; j < CCITTFaxDecoderStream.WHITE_CODES[i].Length; j++)
                {
                    int value = CCITTFaxDecoderStream.WHITE_RUN_LENGTHS[i][j];
                    int code = CCITTFaxDecoderStream.WHITE_CODES[i][j];

                    if (value < 64)
                    {
                        WHITE_TERMINATING_CODES[value] = new Code(code, bitLength);
                    }
                    else
                    {
                        WHITE_NONTERMINATING_CODES[(value / 64) - 1] = new Code(code, bitLength);
                    }
                }
            }

            BLACK_TERMINATING_CODES = new Code[64];
            BLACK_NONTERMINATING_CODES = new Code[40];
            for (int i = 0; i < CCITTFaxDecoderStream.BLACK_CODES.Length; i++)
            {
                int bitLength = i + 2;
                for (int j = 0; j < CCITTFaxDecoderStream.BLACK_CODES[i].Length; j++)
                {
                    int value = CCITTFaxDecoderStream.BLACK_RUN_LENGTHS[i][j];
                    int code = CCITTFaxDecoderStream.BLACK_CODES[i][j];

                    if (value < 64)
                    {
                        BLACK_TERMINATING_CODES[value] = new Code(code, bitLength);
                    }
                    else
                    {
                        BLACK_NONTERMINATING_CODES[(value / 64) - 1] = new Code(code, bitLength);
                    }
                }
            }
        }
    }
}
