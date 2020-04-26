/*
 * Copyright 2015 The Apache Software Foundation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
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
using System.Diagnostics;
using System.IO;

namespace PdfClown.Documents.Contents.Fonts.TTF
{


    /**
     * This class is a version of the one published at
     * https://code.google.com/p/jmzreader/wiki/BufferedRandomAccessFile augmented to handle unsigned
     * bytes. The original class is published under Apache 2.0 license. Fix is marked below
     *
     * This is an optimized version of the RandomAccessFile class as described by Nick Zhang on
     * JavaWorld.com. The article can be found at
     * http://www.javaworld.com/javaworld/javatips/jw-javatip26.html
     *
     * @author jg
     */
    public class BufferedRandomAccessFile : BinaryReader
    {
        /**
         * Buffer size.
         */
        private readonly int BUFSIZE;

        /**
         * Creates a new instance of the BufferedRandomAccessFile.
         *
         * @param filename The path of the file to open.
         * @param mode Specifies the mode to use ("r", "rw", etc.) See the BufferedLineReader
         * documentation for more information.
         * @param bufsize The buffer size (in bytes) to use.
         * @throws FileNotFoundException If the mode is "r" but the given string does not denote an
         * existing regular file, or if the mode begins with "rw" but the given string does not denote
         * an existing, writable regular file and a new regular file of that name cannot be created, or
         * if some other error occurs while opening or creating the file.
         */
        public BufferedRandomAccessFile(string filename, System.Text.Encoding encoding, int bufsize)
            : this(new FileStream(filename, FileMode.Open), encoding, bufsize)
        { }

        public BufferedRandomAccessFile(string filename, int bufsize)
           : this(new FileStream(filename, FileMode.Open), System.Text.Encoding.Default, bufsize)
        { }

        /**
         * Creates a new instance of the BufferedRandomAccessFile.
         *
         * @param file The file to open.
         * @param mode Specifies the mode to use ("r", "rw", etc.) See the BufferedLineReader
         * documentation for more information.
         * @param bufsize The buffer size (in bytes) to use.
         * @throws FileNotFoundException If the mode is "r" but the given file path does not denote an
         * existing regular file, or if the mode begins with "rw" but the given file path does not denote
         * an existing, writable regular file and a new regular file of that name cannot be created, or
         * if some other error occurs while opening or creating the file.
         */
        public BufferedRandomAccessFile(Stream file, System.Text.Encoding encoding, int bufsize)
            : base(file, encoding, false)
        {
            BUFSIZE = bufsize;
        }

        public BufferedRandomAccessFile(Stream file, int bufsize)
          : this(file, System.Text.Encoding.Default, bufsize)
        { }

        /**
         * {@inheritDoc}
         */
        public override int Read()
        {
            try
            {
                FillBuffer(BUFSIZE);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"error: FillBuffer {ex}");
                return -1;
            }

            return base.Read();//TODO test + 256) & 0xFF
            // End of fix
        }

        public short ReadShort()
        {
            try
            {
                FillBuffer(BUFSIZE);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"error: FillBuffer {ex}");
                return -1;
            }
            return base.ReadInt16();
        }

        public ushort ReadUnsignedShort()
        {
            try
            {
                FillBuffer(BUFSIZE);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"error: FillBuffer {ex}");
                return 0;
            }
            return base.ReadUInt16();
        }

        /**
         * {@inheritDoc}
         */
        public override int Read(byte[] b, int off, int len)
        {
            try
            {
                FillBuffer(BUFSIZE);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"error: FillBuffer {ex}");
            }
            return base.Read(b, off, len);
        }

        /**
         * {@inheritDoc}
         */
        public long FilePointer
        {
            get => base.BaseStream.Position;
        }

        /**
         * {@inheritDoc}
         */
        public virtual void Seek(long pos)
        {
            base.BaseStream.Seek(pos, SeekOrigin.Begin);

        }
    }

}