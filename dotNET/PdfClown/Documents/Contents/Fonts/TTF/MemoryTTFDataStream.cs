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
using System;
using System.IO;

namespace PdfClown.Documents.Contents.Fonts.TTF
{
    /**
     * An interface into a data stream.
     * 
     * @author Ben Litchfield
     * 
     */
    public class MemoryTTFDataStream : TTFDataStream
    {
        private byte[] data = null;
        private int currentPosition = 0;

        /**
         * Constructor from a stream. 
         * @param is The stream to read from. It will be closed by this method.
         * @ If an error occurs while reading from the stream.
         */
        public MemoryTTFDataStream(Bytes.Buffer isource)
        {
            try
            {
                MemoryStream output = new MemoryStream(isource.available());
                byte[] buffer = new byte[1024];
                int amountRead;
                while ((amountRead = isource.Read(buffer)) != -1)
                {
                    output.Write(buffer, 0, amountRead);
                }
                data = output.ToArray();
            }
            finally
            {
                isource.Dispose();
            }
        }

        /**
         * Read an unsigned byte.
         * @return An unsigned byte.
         * @ If there is an error reading the data.
         */
        public override long ReadLong()
        {
            return ((long)(ReadSignedInt()) << 32) + (ReadSignedInt() & 0xFFFFFFFFL);
        }

        /**
         * Read a signed integer.
         * 
         * @return A signed integer.
         * @ If there is a problem reading the file.
         */
        public int ReadSignedInt()
        {
            int ch1 = Read();
            int ch2 = Read();
            int ch3 = Read();
            int ch4 = Read();
            return ((ch1 << 24) + (ch2 << 16) + (ch3 << 8) + (ch4 << 0));
        }

        /**
         * Read an unsigned byte.
         * @return An unsigned byte.
         * @ If there is an error reading the data.
         */
        public override int Read()
        {
            if (currentPosition >= data.Length)
            {
                throw new EndOfStreamException();
            }
            byte retval = data[currentPosition];
            currentPosition++;
            return retval;//TODO Test (retval + 256) % 256;
        }

        /**
         * Read an unsigned short.
         * 
         * @return An unsigned short.
         * @ If there is an error reading the data.
         */
        public override ushort ReadUnsignedShort()
        {
            var ch1 = this.Read();
            var ch2 = this.Read();

            return (ushort)((ch1 << 8) + (ch2 << 0));
        }

        /**
         * Read an signed short.
         * 
         * @return An signed short.
         * @ If there is an error reading the data.
         */
        public override short ReadSignedShort()
        {
            var ch1 = this.Read();
            var ch2 = this.Read();
            if ((ch1 | ch2) < 0)
            {
                throw new EndOfStreamException();
            }
            return (short)((ch1 << 8) + (ch2 << 0));
        }

        /**
         * Close the underlying resources.
         * 
         * @ If there is an error closing the resources.
         */
        public override void Dispose()
        {
        }

        /**
         * Seek into the datasource.
         *
         * @param pos The position to seek to.
         * @ If the seek position is negative or larger than MAXINT.
         */
        public override void Seek(long pos)
        {
            if (pos < 0 || pos > int.MaxValue)
            {
                throw new IOException("Illegal seek position: " + pos);
            }
            currentPosition = (int)pos;
        }

        /**
         * @see java.io.Bytes.Buffer#read( byte[], int, int )
         * 
         * @param b The buffer to write to.
         * @param off The offset into the buffer.
         * @param len The length into the buffer.
         * 
         * @return The number of bytes read, or -1 at the end of the stream
         * 
         * @ If there is an error reading from the stream.
         */
        public override int Read(byte[] b, int off, int len)
        {
            if (currentPosition < data.Length)
            {
                int amountRead = Math.Min(len, data.Length - currentPosition);
                Array.Copy(data, currentPosition, b, off, amountRead);
                currentPosition += amountRead;
                return amountRead;
            }
            else
            {
                return -1;
            }
        }

        /**
         * Get the current position in the stream.
         * @return The current position in the stream.
         * @ If an error occurs while reading the stream.
         */
        public override long CurrentPosition
        {
            get => currentPosition;
        }

        /**
         * {@inheritDoc}
         */
        public override bytes.Buffer OriginalData
        {
            get => new ByteArrayInputStream(data);
        }

        /**
         * {@inheritDoc}
         */
        public override long OriginalDataSize
        {
            get => data.Length;
        }
    }
}