/*
 * https://github.com/apache/pdfbox
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
using PdfClown.Util.Math.Geom;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PdfClown.Documents.Contents.Fonts.CCF
{
    /**
     * An Adobe Compact Font Format (CFF) font. Thread safe.
     * 
     * @author Villu Ruusmann
     * @author John Hewson
     */
    public abstract class CFFFont : BaseFont
    {
        protected string fontName;
        protected readonly Dictionary<string, object> topDict = new Dictionary<string, object>(StringComparer.Ordinal);
        protected CFFCharset charset;
        protected byte[][] charStrings;
        protected byte[][] globalSubrIndex;
        private CFFParser.IByteSource source;

        /**
		 * The name of the font.
		 *
		 * @return the name of the font
		 */
        public override string Name
        {
            get => fontName;
        }

        public string FontName
        {
            get => fontName;
            set => fontName = value;
        }

        /**
		 * Returns the top dictionary.
		 * 
		 * @return the dictionary
		 */
        public Dictionary<string, object> TopDict
        {
            get => topDict;
        }

        /**
		 * Returns the FontBBox.
		 */
        public override SKRect FontBBox
        {
            get
            {
                List<float> numbers = (List<float>)topDict["FontBBox"];
                var rect = new SKRect(numbers[0], numbers[1], numbers[2], numbers[3]);
                return rect;
            }
        }

        /**
		 * Returns the CFFCharset of the font.
		 * 
		 * @return the charset
		 */
        public virtual CFFCharset Charset
        {
            get => charset;
            set => charset = value;
        }

        /**
		 * Returns the character strings dictionary. For expert users only.
		 *
		 * @return the dictionary
		 */
        public byte[][] CharStringBytes
        {
            get => charStrings;
            set => charStrings = value;
        }

        /**
		 * Sets a byte source to re-read the CFF data in the future.
		 */
        public void SetData(CFFParser.IByteSource source)
        {
            this.source = source;
        }

        /**
		 * Returns the CFF data.
		 */
        public byte[] Data
        {
            get => source.Bytes;
        }

        /**
		 * Returns the number of charstrings in the font.
		 */
        public int NumCharStrings
        {
            get => charStrings.Length;
        }


        /**
		 * Returns the list containing the global subroutine .
		 * 
		 * @return the dictionary
		 */
        public byte[][] GlobalSubrIndex
        {
            get => globalSubrIndex;
            set => globalSubrIndex = value;
        }

        /**
		 * Adds the given key/value pair to the top dictionary.
		 * 
		 * @param name the given key
		 * @param value the given value
		 */
        public void AddValueToTopDict(string name, object value)
        {
            if (value != null)
            {
                topDict[name] = value;
            }
        }

        /**
		 * Returns the Type 2 charstring for the given CID.
		 *
		 * @param cidOrGid CID for CIFFont, or GID for Type 1 font
		 * @throws IOException if the charstring could not be read
		 */
        public abstract Type2CharString GetType2CharString(int cidOrGid);


        public override string ToString()
        {
            return GetType().Name + "[name=" + fontName + ", topDict=" + topDict
                    + ", charset=" + charset + ", charStrings=" + string.Join(", ", charStrings.Select(p => p.Length))
                    + "]";
        }
    }
}
