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
using System.Diagnostics;

namespace PdfClown.Documents.Contents.Fonts.TTF
{

    /**
     * A 'kern' table in a true type font.
     *
     * @author Glenn Adams
     */
    public class KerningTable : TTFTable
    {

        //private static readonly Log LOG = LogFactory.getLog(KerningTable.class);

        /**
         * Tag to identify this table.
         */
        public const string TAG = "kern";

        private KerningSubtable[] subtables;

        public KerningTable(TrueTypeFont font) : base(font)
        {
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
            int version = data.ReadUnsignedShort();
            if (version != 0)
            {
                version = (version << 16) | data.ReadUnsignedShort();
            }
            int numSubtables = 0;
            if (version == 0)
            {
                numSubtables = data.ReadUnsignedShort();
            }
            else if (version == 1)
            {
                numSubtables = (int)data.ReadUnsignedInt();
            }
            else
            {
                Debug.WriteLine("debug: Skipped kerning table due to an unsupported kerning table version: " + version);
            }
            if (numSubtables > 0)
            {
                subtables = new KerningSubtable[numSubtables];
                for (int i = 0; i < numSubtables; ++i)
                {
                    KerningSubtable subtable = new KerningSubtable();
                    subtable.Read(data, version);
                    subtables[i] = subtable;
                }
            }
            initialized = true;
        }

        /**
         * Obtain first subtable that supports non-cross-stream horizontal kerning.
         *
         * @return first matching subtable or null if none found
         */
        public KerningSubtable GetHorizontalKerningSubtable()
        {
            return GetHorizontalKerningSubtable(false);
        }

        /**
         * Obtain first subtable that supports horizontal kerning with specified cross stream.
         *
         * @param cross true if requesting cross stream horizontal kerning
         * @return first matching subtable or null if none found
         */
        public KerningSubtable GetHorizontalKerningSubtable(bool cross)
        {
            if (subtables != null)
            {
                foreach (KerningSubtable s in subtables)
                {
                    if (s.IsHorizontalKerning(cross))
                    {
                        return s;
                    }
                }
            }
            return null;
        }
    }
}