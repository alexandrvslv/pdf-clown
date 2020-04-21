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
    public class IndexToLocationTable : TTFTable
    {
        private static readonly short SHORT_OFFSETS = 0;
        private static readonly short LONG_OFFSETS = 1;

        /**
         * A tag that identifies this table type.
         */
        public const string TAG = "loca";

        private long[] offsets;

        public IndexToLocationTable(TrueTypeFont font)
            : base(font)
        {
        }

        /**
        * @return Returns the offsets.
        */
        public long[] Offsets
        {
            get => offsets;
            set => offsets = value;
        }

        /**
         * This will read the required data from the stream.
         * 
         * @param ttf The font that is being read.
         * @param data The stream to read the data from.
         * @ If there is an error reading the data.
         */
        public override void Read(TrueTypeFont ttf, TTFDataStream data)
        {
            HeaderTable head = ttf.Header;
            if (head == null)
            {
                throw new IOException("Could not get head table");
            }
            int numGlyphs = ttf.NumberOfGlyphs;
            offsets = new long[numGlyphs + 1];
            for (int i = 0; i < numGlyphs + 1; i++)
            {
                if (head.IndexToLocFormat == SHORT_OFFSETS)
                {
                    offsets[i] = data.ReadUnsignedShort() * 2L;
                }
                else if (head.IndexToLocFormat == LONG_OFFSETS)
                {
                    offsets[i] = data.ReadUnsignedInt();
                }
                else
                {
                    throw new IOException("Error:TTF.loca unknown offset format: " + head.IndexToLocFormat);
                }
            }
            initialized = true;
        }

    }
}