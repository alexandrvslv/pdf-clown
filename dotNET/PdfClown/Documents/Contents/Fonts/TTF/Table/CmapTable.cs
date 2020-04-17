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

    using System.IO;

    /**
     * The "cmap" table of a true type font.
     * 
     * @author Ben Litchfield
     */
    public class CmapTable : TTFTable
    {
        /**
         * A tag used to identify this table.
         */
        public static readonly string TAG = "cmap";

        // platform
        public static readonly int PLATFORM_UNICODE = 0;
        public static readonly int PLATFORM_MACINTOSH = 1;
        public static readonly int PLATFORM_WINDOWS = 3;

        // Mac encodings
        public static readonly int ENCODING_MAC_ROMAN = 0;

        // Windows encodings
        public static readonly int ENCODING_WIN_SYMBOL = 0; // Unicode, non-standard character set
        public static readonly int ENCODING_WIN_UNICODE_BMP = 1; // Unicode BMP (UCS-2)
        public static readonly int ENCODING_WIN_SHIFT_JIS = 2;
        public static readonly int ENCODING_WIN_BIG5 = 3;
        public static readonly int ENCODING_WIN_PRC = 4;
        public static readonly int ENCODING_WIN_WANSUNG = 5;
        public static readonly int ENCODING_WIN_JOHAB = 6;
        public static readonly int ENCODING_WIN_UNICODE_FULL = 10; // Unicode Full (UCS-4)

        // Unicode encodings
        public static readonly int ENCODING_UNICODE_1_0 = 0;
        public static readonly int ENCODING_UNICODE_1_1 = 1;
        public static readonly int ENCODING_UNICODE_2_0_BMP = 3;
        public static readonly int ENCODING_UNICODE_2_0_FULL = 4;

        private CmapSubtable[] cmaps;

        public CmapTable(TrueTypeFont font) : base(font)
        {
        }

        /**
         * @return Returns the cmaps.
         */
        public CmapSubtable[] Cmaps
        {
            get => cmaps;
            set => cmaps = value;
        }


        /**
         * This will read the required data from the stream.
         * 
         * @param ttf The font that is being read.
         * @param data The stream to read the data from.
         * @ If there is an error reading the data.
         */

        protected override void Read(TrueTypeFont ttf, TTFDataStream data)
        {
            //@SuppressWarnings({"unused", "squid:S1854", "squid:S1481"})
            int version = data.ReadUnsignedShort();
            int numberOfTables = data.ReadUnsignedShort();
            cmaps = new CmapSubtable[numberOfTables];
            for (int i = 0; i < numberOfTables; i++)
            {
                CmapSubtable cmap = new CmapSubtable();
                cmap.InitData(data);
                cmaps[i] = cmap;
            }
            for (int i = 0; i < numberOfTables; i++)
            {
                cmaps[i].InitSubtable(this, ttf.getNumberOfGlyphs, data);
            }
            initialized = true;
        }

        /**
         * Returns the subtable, if any, for the given platform and encoding.
         */
        public CmapSubtable getSubtable(int platformId, int platformEncodingId)
        {
            foreach (CmapSubtable cmap in cmaps)
            {
                if (cmap.PlatformId == platformId &&
                    cmap.PlatformEncodingId == platformEncodingId)
                {
                    return cmap;
                }
            }
            return null;
        }
    }
}