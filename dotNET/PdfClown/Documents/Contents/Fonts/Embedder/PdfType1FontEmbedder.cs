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
using PdfClown.Documents.Contents.Fonts.AFM;
using PdfClown.Documents.Contents.Fonts.Type1;
using PdfClown.Objects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PdfClown.Documents.Contents.Fonts
{
    /**
     * Embedded PDType1Font builder. Helper class to populate a PDType1Font from a PFB and AFM.
     *
     * @author Michael Niedermair
     */
    internal class PdfType1FontEmbedder
    {
        private readonly Encoding fontEncoding;
        private readonly Type1Font type1;

        /**
		 * This will load a PFB to be embedded into a document.
		 *
		 * @param doc The PDF document that will hold the embedded font.
		 * @param dict The Font dictionary to write to.
		 * @param pfbStream The pfb input.
		 * @throws IOException If there is an error loading the data.
		 */
        public PdfType1FontEmbedder(Document doc, PdfDictionary dict, Bytes.IInputStream pfbStream, Encoding encoding)
        {
            dict[PdfName.Subtype] = PdfName.Type1;

            // read the pfb
            byte[] pfbBytes = pfbStream.ToByteArray();
            PfbParser pfbParser = new PfbParser(pfbBytes);
            type1 = Type1Font.CreateWithPFB(pfbBytes);

            if (encoding == null)
            {
                fontEncoding = Type1Encoding.FromFontBox(type1.Encoding);
            }
            else
            {
                fontEncoding = encoding;
            }

            // build font descriptor
            FontDescriptor fd = BuildFontDescriptor(type1);

            PdfStream fontStream = new PdfStream(pfbParser.GetInputStream());
            fontStream.Header[PdfName.Length] = PdfInteger.Get(pfbParser.Size);
            for (int i = 0; i < pfbParser.Lengths.Length; i++)
            {
                fontStream.Header[new PdfName("Length" + (i + 1))] = PdfInteger.Get(pfbParser.Lengths[i]);
            }
            fd.FontFile = new FontFile(doc, fontStream);

            // set the values
            dict[PdfName.FontDescriptor] = fd.BaseObject;
            dict[PdfName.BaseFont] = PdfName.Get(type1.Name);

            // widths
            List<int> widths = new List<int>(256);
            for (int code = 0; code <= 255; code++)
            {
                string name = fontEncoding.GetName(code);
                int width = (int)Math.Round(type1.GetWidth(name));
                widths.Add(width);
            }

            dict[PdfName.FirstChar] = PdfInteger.Get(0);
            dict[PdfName.LastChar] = PdfInteger.Get(255);
            dict[PdfName.Widths] = new PdfArray(widths.Select(p => PdfInteger.Get(p)));
            dict[PdfName.Encoding] = encoding.GetPdfObject();
        }

        /**
		 * Returns a FontDescriptor for the given PFB.
		 */
        public static FontDescriptor BuildFontDescriptor(Type1Font type1)
        {
            bool isSymbolic = type1.Encoding is BuiltInEncoding;

            FontDescriptor fd = new FontDescriptor();
            fd.FontName = type1.Name;
            fd.FontFamily = type1.FamilyName;
            fd.NonSymbolic(!isSymbolic);
            fd.Symbolic(isSymbolic);
            fd.FontBBox = new Rectangle(type1.FontBBox);
            fd.ItalicAngle = type1.ItalicAngle;
            fd.Ascent = type1.FontBBox.Top;
            fd.Descent = type1.FontBBox.Bottom;
            fd.CapHeight = type1.BlueValues[2];
            fd.StemV = 0; // for PDF/A
            return fd;
        }


        /**
		 * Returns a FontDescriptor for the given AFM. Used only for Standard 14 fonts.
		 *
		 * @param metrics AFM
		 */
        public static FontDescriptor BuildFontDescriptor(FontMetrics metrics)
        {
            bool isSymbolic = metrics.getEncodingScheme().equals("FontSpecific");

            FontDescriptor fd = new FontDescriptor();
            fd.FontName = metrics.FontName;
            fd.FontFamily = metrics.FamilyName;
            fd.NonSymbolic(!isSymbolic);
            fd.Symbolic(isSymbolic);
            fd.FontBBox = new Rectangle(metrics.FontBBox);
            fd.ItalicAngle = metrics.ItalicAngle;
            fd.Ascent = metrics.Ascender;
            fd.Descent = metrics.Descender;
            fd.CapHeight = metrics.CapHeight;
            fd.XHeight = metrics.XHeight;
            fd.AverageWidth = metrics.AverageCharacterWidth;
            fd.CharacterSet = metrics.CharacterSet;
            fd.StemV = 0; // for PDF/A
            return fd;
        }

        /**
		 * Returns the font's encoding.
		 */
        public Encoding FontEncoding
        {
            get => fontEncoding;
        }

        /**
		 * Returns the font's glyph list.
		 */
        public GlyphMapping GlyphList
        {
            get => GlyphMapping.Default;
        }

        /**
		 * Returns the Type 1 font.
		 */
        public Type1Font Type1Font
        {
            get => type1;
        }
    }
}
