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
namespace PdfClown.Documents.Contents.Fonts.TTF
{
    using System;
    using System.IO;

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
         * Uses a byte instead of a char buffer for efficiency reasons.
         */
        private readonly byte[] buffer;
        private int bufend = 0;
        private int bufpos = 0;

        /**
         * The position inside the actual file.
         */
        private long realpos = 0;

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
        public BufferedRandomAccessFile(string filename, System.Text.Encoding mode, int bufsize)
            : this(new FileStream(filename, FileMode.Open), mode, bufsize)
        {
        }

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
        public BufferedRandomAccessFile(Stream file, System.Text.Encoding mode, int bufsize)
            : base(file, mode)
        {
            BUFSIZE = bufsize;
            buffer = new byte[BUFSIZE];
        }

        /**
         * {@inheritDoc}
         */
        public override int Read()
        {
            if (bufpos >= bufend && FillBuffer() < 0)
            {
                return -1;
            }
            if (bufend == 0)
            {
                return -1;
            }

            return buffer[bufpos++];//TODO test + 256) & 0xFF
            // End of fix
        }

        /**
         * Reads the next BUFSIZE bytes into the internal buffer.
         *
         * @return The total number of bytes read into the buffer, or -1 if there is no more data
         * because the end of the file has been reached.
         * @ If the first byte cannot be read for any reason other than end of file,
         * or if the random access file has been closed, or if some other I/O error occurs.
         */
        private int FillBuffer()
        {
            int n = base.Read(buffer, 0, BUFSIZE);

            if (n >= 0)
            {
                realpos += n;
                bufend = n;
                bufpos = 0;
            }
            return n;
        }

        /**
         * Clears the local buffer.
         *
         * @ If an I/O error occurs.
         */
        private void Invalidate()
        {
            bufend = 0;
            bufpos = 0;
            realpos = base.BaseStream.Position;
        }

        /**
         * {@inheritDoc}
         */
        public override int Read(byte[] b, int off, int len)
        {
            int leftover = bufend - bufpos;
            if (len <= leftover)
            {
                Array.Copy(buffer, bufpos, b, off, len);
                bufpos += len;
                return len;
            }
            Array.Copy(buffer, bufpos, b, off, leftover);
            bufpos += leftover;
            if (FillBuffer() > 0)
            {
                int bytesRead = Read(b, off + leftover, len - leftover);
                if (bytesRead > 0)
                {
                    leftover += bytesRead;
                }
            }
            return leftover > 0 ? leftover : -1;
        }

        /**
         * {@inheritDoc}
         */
        public long FilePointer
        {
            get => realpos - bufend + bufpos;
        }

        /**
         * {@inheritDoc}
         */
        public virtual void Seek(long pos)
        {
            int n = (int)(realpos - pos);
            if (n >= 0 && n <= bufend)
            {
                bufpos = bufend - n;
            }
            else
            {
                base.BaseStream.Seek(pos, SeekOrigin.Begin);
                Invalidate();
            }
        }
    }

}