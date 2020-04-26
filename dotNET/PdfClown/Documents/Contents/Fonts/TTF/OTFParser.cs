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
     * OpenType font file parser.
     */
    public sealed class OTFParser : TTFParser
    {
        /**
         * Constructor.
         */
        public OTFParser() : base()
        { }

        /**
         * Constructor.
         *
         * @param isEmbedded true if the font is embedded in PDF
         */
        public OTFParser(bool isEmbedded) : this(isEmbedded, false)
        { }

        /**
         *  Constructor.
         *
         * @param isEmbedded true if the font is embedded in PDF
         * @param parseOnDemand true if the tables of the font should be parsed on demand
         */
        public OTFParser(bool isEmbedded, bool parseOnDemand) : base(isEmbedded, parseOnDemand)
        { }

        public new OpenTypeFont Parse(string file)
        {
            return (OpenTypeFont)base.Parse(file);
        }

        public new OpenTypeFont Parse(Stream file)
        {
            return (OpenTypeFont)base.Parse(file);
        }

        public new OpenTypeFont Parse(Bytes.IInputStream data)
        {
            return (OpenTypeFont)base.Parse(data);
        }

        public new OpenTypeFont Parse(TTFDataStream raf)
        {
            return (OpenTypeFont)base.Parse(raf);
        }

        public override TrueTypeFont NewFont(TTFDataStream raf)
        {
            return new OpenTypeFont(raf);
        }

        protected override TTFTable ReadTable(TrueTypeFont font, string tag)
        {
            // todo: this is a stub, a full implementation is needed
            switch (tag)
            {
                case "BASE":
                case "GDEF":
                case "GPOS":
                case "GSUB":
                case "JSTF":
                    return new OTLTable(font);
                case "CFF ":
                    return new CFFTable(font);
                default:
                    return base.ReadTable(font, tag);
            }
        }


        protected override bool AllowCFF
        {
            get => true;
        }
    }
}
