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
using System.IO;

namespace PdfClown.Documents.Contents.Fonts.TTF
{
    /**
     * An implementation of the TTFDataStream that goes against a RAF.
     * 
     * @author Ben Litchfield
     */
    public class RAFDataStream : TTFDataStream
    {
        private BufferedRandomAccessFile raf = null;
        private Stream ttfFile = null;
        private static readonly int BUFFERSIZE = 16384;

        /**
         * Constructor.
         * 
         * @param name The raf file.
         * @param mode The mode to open the RAF.
         * 
         * @ If there is a problem creating the RAF.
         * 
         * @see RandomAccessFile#RandomAccessFile( string, string )
         */
        public RAFDataStream(string name, System.Text.Encoding mode)
            : this(new FileStream(name, FileMode.Open), mode)
        {
        }

        /**
         * Constructor.
         * 
         * @param file The raf file.
         * @param mode The mode to open the RAF.
         * 
         * @ If there is a problem creating the RAF.
         * 
         * @see RandomAccessFile#RandomAccessFile( File, string )
         */
        public RAFDataStream(Stream file, System.Text.Encoding mode)
        {
            raf = new BufferedRandomAccessFile(file, mode, BUFFERSIZE);
            ttfFile = file;
        }

        /**
         * Read a signed short.
         * 
         * @return An signed short.
         * @ If there is an error reading the data.
         * @see RandomAccessFile#readShort()
         */
        public override short ReadSignedShort()
        {
            return raf.ReadShort();
        }

        /**
         * Get the current position in the stream.
         * @return The current position in the stream.
         * @ If an error occurs while reading the stream.
         */
        public override long CurrentPosition
        {
            get => raf.FilePointer;
        }

        /**
         * Close the underlying resources.
         * 
         * @ If there is an error closing the resources.
         */
        public override void Dispose()
        {
            if (raf != null)
            {
                raf.Dispose();
                raf = null;
            }
        }

        /**
         * Read an unsigned byte.
         * @return An unsigned byte.
         * @ If there is an error reading the data.
         * @see RandomAccessFile#read()
         */
        public override int Read()
        {
            return raf.Read();
        }

        /**
         * Read an unsigned short.
         * 
         * @return An unsigned short.
         * @ If there is an error reading the data.
         * @see RandomAccessFile#ReadUnsignedShort()
         */
        public override ushort ReadUnsignedShort()
        {
            return raf.ReadUnsignedShort();
        }

        /**
         * Read a signed 64-bit integer.
         * 
         * @return eight bytes interpreted as a long.
         * @ If there is an error reading the data.
         * @see RandomAccessFile#readLong()    
         */
        public override long ReadLong()
        {
            return raf.ReadLong();
        }

        /**
         * Seek into the datasource.
         * 
         * @param pos The position to seek to.
         * @ If there is an error seeking to that position.
         */
        public override void Seek(long pos)
        {
            raf.Seek(pos);
        }

        /**
         * @see java.io.Bytes.Buffer#read( byte[], int, int )
         * 
         * @param b The buffer to write to.
         * @param off The offset into the buffer.
         * @param len The length into the buffer.
         * 
         * @return The number of bytes read.
         * 
         * @ If there is an error reading from the stream.
         */
        public override int Read(byte[] b, int off, int len)
        {
            return raf.Read(b, off, len);
        }

        /**
         * {@inheritDoc}
         */
        public override bytes.Buffer OriginalData
        {
            get => new FileInputStream(ttfFile);
        }

        /**
         * {@inheritDoc}
         */
        public override long OriginalDataSize
        {
            get => ttfFile.Length;
        }
    }
}