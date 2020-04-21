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
namespace PdfClown.Documents.Contents.Fonts.TTF
{
    using PdfClown.Tokens;
    using System;
    using System.Globalization;
    using System.IO;

    /**
     * An interface into a data stream.
     * 
     * @author Ben Litchfield
     */
    public abstract class TTFDataStream : IDisposable
    {
        public TTFDataStream()
        {
        }

        /**
         * Read a 16.16 fixed value, where the first 16 bits are the decimal and the last 16 bits are the fraction.
         * 
         * @return A 32 bit value.
         * @ If there is an error reading the data.
         */
        public float Read32Fixed()
        {
            float retval = 0;
            retval = ReadSignedShort();
            retval += (ReadUnsignedShort() / 65536.0F);
            return retval;
        }

        /**
         * Read a fixed length ascii string.
         * 
         * @param length The length of the string to read.
         * @return A string of the desired length.
         * @ If there is an error reading the data.
         */
        public string ReadString(int length)
        {
            return ReadString(length, Charset.ISO88591);
        }

        /**
         * Read a fixed length string.
         * 
         * @param length The length of the string to read in bytes.
         * @param charset The expected character set of the string.
         * @return A string of the desired length.
         * @ If there is an error reading the data.
         */
        public string ReadString(int length, System.Text.Encoding charset)
        {
            byte[] buffer = Read(length);
            return charset.GetString(buffer, 0, buffer.Length);
        }

        /**
         * Read an unsigned byte.
         * 
         * @return An unsigned byte.
         * @ If there is an error reading the data.
         */
        public abstract int Read();

        /**
         * Read an unsigned byte.
         * 
         * @return An unsigned byte.
         * @ If there is an error reading the data.
         */
        public abstract long ReadLong();

        /**
         * Read a signed byte.
         * 
         * @return A signed byte.
         * @ If there is an error reading the data.
         */
        public sbyte ReadSignedByte()
        {
            byte signedByte = (byte)Read();
            return unchecked((sbyte)signedByte);// signedByte <= 127 ? signedByte : signedByte - 256;
        }

        /**
         * Read a unsigned byte. Similar to {@link #read()}, but throws an exception if EOF is unexpectedly reached.
         * 
         * @return A unsigned byte.
         * @ If there is an error reading the data.
         */
        public byte ReadUnsignedByte()
        {
            return (byte)Read();
        }

        /**
         * Read an unsigned integer.
         * 
         * @return An unsigned integer.
         * @ If there is an error reading the data.
         */
        public uint ReadUnsignedInt()
        {
            var byte1 = Read();
            var byte2 = Read();
            var byte3 = Read();
            var byte4 = Read();
            if (byte4 < 0)
            {
                throw new EndOfStreamException();
            }
            return (uint)((byte1 << 24) + (byte2 << 16) + (byte3 << 8) + (byte4 << 0));
        }

        /**
         * Read an unsigned short.
         * 
         * @return An unsigned short.
         * @ If there is an error reading the data.
         */
        public abstract ushort ReadUnsignedShort();

        /**
         * Read an unsigned byte array.
         * 
         * @param length the length of the array to be read
         * @return An unsigned byte array.
         * @ If there is an error reading the data.
         */
        public byte[] ReadUnsignedByteArray(int length)
        {
            byte[] array = new byte[length];
            for (int i = 0; i < length; i++)
            {
                array[i] = (byte)Read();
            }
            return array;
        }

        /**
         * Read an unsigned short array.
         * 
         * @param length The length of the array to read.
         * @return An unsigned short array.
         * @ If there is an error reading the data.
         */
        public ushort[] ReadUnsignedShortArray(int length)
        {
            ushort[] array = new ushort[length];
            for (int i = 0; i < length; i++)
            {
                array[i] = ReadUnsignedShort();
            }
            return array;
        }

        /**
         * Read an signed short.
         * 
         * @return An signed short.
         * @ If there is an error reading the data.
         */
        public abstract short ReadSignedShort();

        /**
         * Read an eight byte international date.
         * 
         * @return An signed short.
         * @ If there is an error reading the data.
         */
        public DateTime ReadInternationalDate()
        {
            long secondsSince1904 = ReadLong();
            var cal = new DateTime(1904, 0, 1, 0, 0, 0, DateTimeKind.Utc);
            return cal + TimeSpan.FromMilliseconds(secondsSince1904);
        }

        /**
         * Reads a tag, an array of four uint8s used to identify a script, language system, feature,
         * or baseline.
         */
        public string ReadTag()
        {
            return Charset.ASCII.GetString(Read(4));
        }

        /**
         * Seek into the datasource.
         * 
         * @param pos The position to seek to.
         * @ If there is an error seeking to that position.
         */
        public abstract void Seek(long pos);

        /**
         * Read a specific number of bytes from the stream.
         * 
         * @param numberOfBytes The number of bytes to read.
         * @return The byte buffer.
         * @ If there is an error while reading.
         */
        public byte[] Read(int numberOfBytes)
        {
            byte[] data = new byte[numberOfBytes];
            int amountRead = 0;
            int totalAmountRead = 0;
            // read at most numberOfBytes bytes from the stream.
            while (totalAmountRead < numberOfBytes
                    && (amountRead = Read(data, totalAmountRead, numberOfBytes - totalAmountRead)) != -1)
            {
                totalAmountRead += amountRead;
            }
            if (totalAmountRead == numberOfBytes)
            {
                return data;
            }
            else
            {
                throw new IOException("Unexpected end of TTF stream reached");
            }
        }

        /**
         * @see java.io.Bytes.Buffer#read(byte[], int, int )
         * 
         * @param b The buffer to write to.
         * @param off The offset into the buffer.
         * @param len The length into the buffer.
         * 
         * @return The number of bytes read, or -1 at the end of the stream
         * 
         * @ If there is an error reading from the stream.
         */
        public abstract int Read(byte[] b, int off, int len);

        public abstract void Dispose();

        /**
         * Get the current position in the stream.
         * 
         * @return The current position in the stream.
         * @ If an error occurs while reading the stream.
         */
        public abstract long CurrentPosition { get; }

        /**
         * This will get the original data file that was used for this stream.
         * 
         * @return The data that was read from.
         * @ If there is an issue reading the data.
         */
        public abstract Bytes.Buffer OriginalData { get; }

        /**
         * This will get the original data size that was used for this stream.
         * 
         * @return The size of the original data.
         * @ If there is an issue reading the data.
         */
        public abstract long OriginalDataSize { get; }
    }
}