/*
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
using PdfClown.Objects;
using PdfClown.Util;
using PdfClown.Util.Collections.Generic;
using System;
using System.Collections.Generic;

namespace PdfClown.Documents.Contents.Fonts
{
    /**
     * This will perform the encoding from a dictionary.
     *
     * @author Ben Litchfield
     */
    internal class DictionaryEncoding : Encoding
    {
        private readonly PdfDictionary encoding;
        private readonly Encoding baseEncoding;
        private readonly Dictionary<int, string> differences = new Dictionary<int, string>();

        /**
		 * Creates a new DictionaryEncoding for embedding.
		 *
		 * @param baseEncoding
		 * @param differences
		 */
        public DictionaryEncoding(PdfName baseEncoding, PdfArray differences)
        {
            encoding = new PdfDictionary();
            encoding[PdfName.Name] = PdfName.Encoding;
            encoding[PdfName.Differences] = differences;
            if (baseEncoding != PdfName.StandardEncoding)
            {
                encoding[PdfName.BaseEncoding] = baseEncoding;
                this.baseEncoding = Encoding.Get(baseEncoding);
            }
            else
            {
                this.baseEncoding = Encoding.Get(baseEncoding);
            }

            if (this.baseEncoding == null)
            {
                throw new ArgumentException("Invalid encoding: " + baseEncoding);
            }

            codeToName.AddAll(this.baseEncoding.codeToName);
            inverted.AddAll(this.baseEncoding.inverted);
            ApplyDifferences();
        }

        /**
		 * Creates a new DictionaryEncoding for a Type 3 font from a PDF.
		 *
		 * @param fontEncoding The Type 3 encoding dictionary.
		 */
        public DictionaryEncoding(PdfDictionary fontEncoding)
        {
            encoding = fontEncoding;
            baseEncoding = null;
            ApplyDifferences();
        }

        /**
		 * Creates a new DictionaryEncoding from a PDF.
		 *
		 * @param fontEncoding The encoding dictionary.
		 * @param isNonSymbolic True if the font is non-symbolic. False for Type 3 fonts.
		 * @param builtIn The font's built-in encoding. Null for Type 3 fonts.
		 */
        public DictionaryEncoding(PdfDictionary fontEncoding, bool isNonSymbolic, Encoding builtIn)
        {
            encoding = fontEncoding;

            Encoding baseEncoding = null;
            var hasBaseEncoding = encoding.Resolve(PdfName.BaseEncoding);
            if (hasBaseEncoding is PdfName name)
            {
                baseEncoding = Encoding.Get(name); // null when the name is invalid
            }

            if (baseEncoding == null)
            {
                if (isNonSymbolic)
                {
                    // Otherwise, for a nonsymbolic font, it is StandardEncoding
                    baseEncoding = StandardEncoding.Instance;
                }
                else
                {
                    // and for a symbolic font, it is the font's built-in encoding.
                    if (builtIn != null)
                    {
                        baseEncoding = builtIn;
                    }
                    else
                    {
                        // triggering this error indicates a bug in PDFBox. Every font should always have
                        // a built-in encoding, if not, we parsed it incorrectly.
                        throw new ArgumentException("Symbolic fonts must have a built-in " + "encoding");
                    }
                }
            }
            this.baseEncoding = baseEncoding;

            codeToName.AddAll(baseEncoding.codeToName);
            inverted.AddAll(baseEncoding.inverted);
            ApplyDifferences();
        }

        private void ApplyDifferences()
        {
            // now replace with the differences
            var diff = encoding.Resolve(PdfName.Differences);
            if (!(diff is PdfArray diffArray))
            {
                return;
            }
            int currentIndex = -1;
            for (int i = 0; i < diffArray.Count; i++)
            {
                var next = diffArray[i];
                if (next is IPdfNumber number)
                {
                    currentIndex = number.IntValue;
                }
                else if (next is PdfName name)
                {
                    Overwrite(currentIndex, name.StringValue);
                    this.differences[currentIndex] = name.StringValue;
                    currentIndex++;
                }
            }
        }

        /**
		 * Returns the base encoding. Will be null for Type 3 fonts.
		 */
        public Encoding BaseEncoding
        {
            get => baseEncoding;
        }

        /**
		 * Returns the Differences array.
		 */
        public Dictionary<int, string> Differences
        {
            get => differences;
        }

        public override PdfDirectObject GetPdfObject()
        {
            return encoding;
        }

    }
}
