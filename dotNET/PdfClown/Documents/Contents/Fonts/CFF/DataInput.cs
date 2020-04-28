/*
 * https://github.com/apache/pdfbox
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
using PdfClown.Tokens;
using System;
using System.Diagnostics;
using System.IO;

namespace PdfClown.Documents.Contents.Fonts.Type1
{

    /**
     * This class contains some functionality to read a byte buffer.
     * 
     * @author Villu Ruusmann
     */
    public class DataInput
    {

        private byte[] inputBuffer = null;
        private int bufferPosition = 0;


        /**
		 * Constructor.
		 * @param buffer the buffer to be read
		 */
        public DataInput(byte[] buffer)
        {
            inputBuffer = buffer;
        }

        /**
		 * Determines if there are any bytes left to read or not. 
		 * @return true if there are any bytes left to read
		 */
        public bool HasRemaining()
        {
            return bufferPosition < inputBuffer.Length;
        }

        /**
		 * Returns the current position.
		 * @return current position
		 */
        public int Position
        {
            get => bufferPosition;
            set => bufferPosition = value;
        }

        public int Length => inputBuffer.Length;


        /** 
		 * Returns the buffer as an ISO-8859-1 string.
		 * @return the buffer as string
		 * @throws IOException if an error occurs during reading
		 */
        public string GetString()
        {
            return PdfEncoding.Pdf.Decode(inputBuffer);
        }

        /**
		 * Read one single byte from the buffer.
		 * @return the byte
		 * @throws IOException if an error occurs during reading
		 */
        public byte ReadUnsignedByte()
        {
            try
            {
                byte value = inputBuffer[bufferPosition];
                bufferPosition++;
                return value;
            }
            catch (Exception re)
            {
                Debug.WriteLine("debug: An error occurred reading a byte - returning -1", re);
                throw new EndOfStreamException();
            }
        }

        /**
		 * Peeks one single unsigned byte from the buffer.
		 * @return the unsigned byte as int
		 * @throws IOException if an error occurs during reading
		 */
        public byte PeekUnsignedByte(int offset)
        {
            try
            {
                return inputBuffer[bufferPosition + offset];
            }
            catch (Exception re)
            {
                Debug.WriteLine("debug: An error occurred peeking at offset " + offset + " - returning -1", re);
                throw new EndOfStreamException();
            }
        }

        /**
		 * Read one single signed byte from the buffer.
		 * @return the signed byte as int
		 * @throws IOException if an error occurs during reading
		 */
        public sbyte ReadSignedByte()
        {
            try
            {
                sbyte value = unchecked((sbyte)inputBuffer[bufferPosition]);
                bufferPosition++;
                return value;
            }
            catch (Exception re)
            {
                Debug.WriteLine("debug: An error occurred reading a byte - returning -1", re);
                throw new EndOfStreamException();
            }
        }

        /**
		 * Peeks one single signed byte from the buffer.
		 * @return the signed byte as int
		 * @throws IOException if an error occurs during reading
		 */
        public sbyte PeekSignedByte(int offset)
        {
            try
            {
                return unchecked((sbyte)inputBuffer[bufferPosition + offset]);
            }
            catch (Exception re)
            {
                Debug.WriteLine("debug: An error occurred peeking at offset " + offset + " - returning -1", re);
                throw new EndOfStreamException();
            }
        }

        /**
		 * Read one single short value from the buffer.
		 * @return the short value
		 * @throws IOException if an error occurs during reading
		 */
        public short ReadShort()
        {
            var b1 = ReadUnsignedByte();
            var b2 = ReadUnsignedByte();

            return (short)(b1 << 8 | b2);
        }

        /**
		 * Read one single unsigned short (2 bytes) value from the buffer.
		 * @return the unsigned short value as int
		 * @throws IOException if an error occurs during reading
		 */
        public ushort ReadUnsignedShort()
        {
            var b1 = ReadUnsignedByte();
            var b2 = ReadUnsignedByte();

            return (ushort)(b1 << 8 | b2);
        }

        /**
		 * Read one single int (4 bytes) from the buffer.
		 * @return the int value
		 * @throws IOException if an error occurs during reading
		 */
        public int ReadInt()
        {
            var b1 = ReadUnsignedByte();
            var b2 = ReadUnsignedByte();
            var b3 = ReadUnsignedByte();
            var b4 = ReadUnsignedByte();
            return b1 << 24 | b2 << 16 | b3 << 8 | b4;
        }

        /**
		 * Read a number of single byte values from the buffer.
		 * @param length the number of bytes to be read
		 * @return an array with containing the bytes from the buffer 
		 * @throws IOException if an error occurs during reading
		 */
        public byte[] ReadBytes(int length)
        {
            if (inputBuffer.Length - bufferPosition < length)
            {
                throw new EndOfStreamException();
            }
            byte[] bytes = new byte[length];
            Array.Copy(inputBuffer, bufferPosition, bytes, 0, length);
            bufferPosition += length;
            return bytes;
        }

        public int Read()
        {
            try
            {
                var value = inputBuffer[bufferPosition] & 0xff;
                bufferPosition++;
                return value;
            }
            catch (Exception re)
            {
                Debug.WriteLine("debug: An error occurred reading an int - returning -1", re);
                throw new EndOfStreamException();
            }
        }

        public int Peek(int offset)
        {
            try
            {
                return inputBuffer[bufferPosition + offset] & 0xff;
            }
            catch (Exception re)
            {
                Debug.WriteLine("debug: An error occurred peeking at offset " + offset + " - returning -1", re);
                throw new EndOfStreamException();
            }
        }

    }
}