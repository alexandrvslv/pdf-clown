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
		public bool hasRemaining()
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
		public byte readByte()
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
		 * Read one single unsigned byte from the buffer.
		 * @return the unsigned byte as int
		 * @throws IOException if an error occurs during reading
		 */
		public byte ReadUnsignedByte()
		{
			return readByte();
		}

		/**
		 * Peeks one single unsigned byte from the buffer.
		 * @return the unsigned byte as int
		 * @throws IOException if an error occurs during reading
		 */
		public byte peekUnsignedByte(int offset)
		{
			var b = peek(offset);
			if (b < 0)
			{
				throw new EndOfStreamException();
			}
			return b;
		}

		/**
		 * Read one single short value from the buffer.
		 * @return the short value
		 * @throws IOException if an error occurs during reading
		 */
		public short readShort()
		{
			return (short)readUnsignedShort();
		}

		/**
		 * Read one single unsigned short (2 bytes) value from the buffer.
		 * @return the unsigned short value as int
		 * @throws IOException if an error occurs during reading
		 */
		public ushort readUnsignedShort()
		{
			var b1 = read();
			var b2 = read();

			return (ushort)(b1 << 8 | b2);
		}

		/**
		 * Read one single int (4 bytes) from the buffer.
		 * @return the int value
		 * @throws IOException if an error occurs during reading
		 */
		public int readInt()
		{
			var b1 = read();
			var b2 = read();
			var b3 = read();
			var b4 = read();
			return b1 << 24 | b2 << 16 | b3 << 8 | b4;
		}

		/**
		 * Read a number of single byte values from the buffer.
		 * @param length the number of bytes to be read
		 * @return an array with containing the bytes from the buffer 
		 * @throws IOException if an error occurs during reading
		 */
		public byte[] readBytes(int length)
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

		private byte read()
		{
			try
			{
				byte value = inputBuffer[bufferPosition];
				bufferPosition++;
				return value;
			}
			catch (Exception re)
			{
				Debug.WriteLine("debug: An error occurred reading an int - returning -1", re);
				throw new EndOfStreamException();
			}
		}

		private byte peek(int offset)
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

		public int Length => inputBuffer.Length;
	}
}