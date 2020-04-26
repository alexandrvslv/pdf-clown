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

    using SkiaSharp;
    using System;
    using System.IO;

    /**
     * An OpenType (OTF/TTF) font.
     */
    public class OpenTypeFont : TrueTypeFont
    {
        private bool isPostScript;

        /**
         * Constructor. Clients should use the OTFParser to create a new OpenTypeFont object.
         *
         * @param fontData The font data.
         */
        public OpenTypeFont(TTFDataStream fontData) : base(fontData)
        {

        }

        public override float Version
        {
            get => base.Version;
            set
            {
                //isPostScript = Float.floatToIntBits(value) == 0x469EA8A9; // OTTO
                base.Version = value;
            }
        }
        /**
         * Get the "CFF" table for this OTF.
         *
         * @return The "CFF" table.
         */
        public CFFTable CFF
        {
            get
            {
                if (!IsPostScript)
                {
                    throw new NotSupportedException("TTF fonts do not have a CFF table");
                }
                return (CFFTable)GetTable(CFFTable.TAG);
            }
        }

        public override GlyphTable Glyph
        {
            get
            {
                if (IsPostScript)
                {
                    throw new NotSupportedException("OTF fonts do not have a glyf table");
                }
                return base.Glyph;
            }
        }

        public override SKPath GetPath(string name)
        {
            int gid = NameToGID(name);
            return CFF.Font.GetType2CharString(gid).Path;
        }

        /**
         * Returns true if this font is a PostScript outline font.
         */
        public bool IsPostScript
        {
            get => tables.ContainsKey(CFFTable.TAG);
        }

        /**
         * Returns true if this font uses OpenType Layout (Advanced Typographic) tables.
         */
        public bool HasLayoutTables()
        {
            return tables.ContainsKey("BASE") ||
                   tables.ContainsKey("GDEF") ||
                   tables.ContainsKey("GPOS") ||
                   tables.ContainsKey("GSUB") ||
                   tables.ContainsKey("JSTF");
        }
    }
}
