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
using PdfClown.Documents.Contents.Fonts.CCF;
using System.IO;

namespace PdfClown.Documents.Contents.Fonts.TTF
{
    /**
     * PostScript font program (compact font format).
     */
    public class CFFTable : TTFTable
    {
        /**
         * A tag that identifies this table type.
         */
        public static readonly string TAG = "CFF ";

        private CFFFont cffFont;

        public CFFTable(TrueTypeFont font) : base(font)
        {
        }

        /**
         * This will read the required data from the stream.
         *
         * @param ttf The font that is being read.
         * @param data The stream to read the data from.
         * @throws java.io.IOException If there is an error reading the data.
         */
        public override void Read(TrueTypeFont ttf, TTFDataStream data)
        {
            byte[] bytes = data.Read((int)Length);

            CFFParser parser = new CFFParser();
            cffFont = parser.Parse(bytes, new CFFBytesource(font))[0];

            initialized = true;
        }

        /**
         * Returns the CFF font, which is a compact representation of a PostScript Type 1, or CIDFont
         */
        public CFFFont Font
        {
            get => cffFont;
        }

        /**
         * Allows bytes to be re-read later by CFFParser.
         */
        internal class CFFBytesource : CFFParser.IByteSource
        {
            private readonly TrueTypeFont ttf;

            public CFFBytesource(TrueTypeFont ttf)
            {
                this.ttf = ttf;
            }

            public byte[] GetBytes()
            {
                return ttf.GetTableBytes(ttf.TableMap.TryGetValue(CFFTable.TAG, out var ccfTable) ? ccfTable : null);
            }
        }
    }
}
