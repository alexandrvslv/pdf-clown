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
     * A table in a true type font.
     * 
     * @author Ben Litchfield
     */
    public class TTFTable
    {
        private string tag;
        private long checkSum;
        private long offset;
        private long length;

        /**
         * Indicates if the table is initialized or not.
         */
        protected bool initialized;

        /**
         * The font which contains this table.
         */
        protected readonly TrueTypeFont font;

        /**
         * Constructor.
         * 
         * @param font The font which contains this table.
         */
        public TTFTable(TrueTypeFont font)
        {
            this.font = font;
        }

        /**
         * @return Returns the checkSum.
         */
        public long CheckSum
        {
            get => checkSum;
            set => checkSum = value;
        }

        /**
         * @return Returns the length.
         */
        public long Length
        {
            get => length;
            set => length = value;
        }

        /**
         * @return Returns the offset.
         */
        public long Offset
        {
            get => offset;
            set => offset = value;
        }

        /**
         * @return Returns the tag.
         */
        public string Tag
        {
            get => tag;
            set => tag = value;
        }

        /**
         * Indicates if the table is already initialized.
         * 
         * @return true if the table is initialized
         */
        public bool Initialized
        {
            get => initialized;
        }

        /**
         * This will read the required data from the stream.
         * 
         * @param ttf The font that is being read.
         * @param data The stream to read the data from.
         * @ If there is an error reading the data.
         */
        protected virtual void Read(TrueTypeFont ttf, TTFDataStream data)
        {
        }
    }
}