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

using System;
using System.IO;
using System.Collections.Generic;
using PdfClown.Documents.Contents.Fonts.AFM;

namespace PdfClown.Documents.Contents.Fonts
{

    /**
	 * The "Standard 14" PDF fonts, also known as the "base 14" fonts.
	 * There are 14 font files, but Acrobat uses additional names for compatibility, e.g. Arial.
	 *
	 * @author John Hewson
	 */
    public sealed class Standard14Fonts
    {
        private static readonly HashSet<string> StandardNames = new HashSet<string>(StringComparer.Ordinal);
        private static readonly Dictionary<string, string> StandardMapping = new Dictionary<string, string>(34, StringComparer.Ordinal);
        private static readonly Dictionary<string, FontMetrics> StandardAFMMapping = new Dictionary<string, FontMetrics>(34, StringComparer.Ordinal);
        static Standard14Fonts()
        {
            try
            {
                AddAFM("Courier-Bold");
                AddAFM("Courier-BoldOblique");
                AddAFM("Courier");
                AddAFM("Courier-Oblique");
                AddAFM("Helvetica");
                AddAFM("Helvetica-Bold");
                AddAFM("Helvetica-BoldOblique");
                AddAFM("Helvetica-Oblique");
                AddAFM("Symbol");
                AddAFM("Times-Bold");
                AddAFM("Times-BoldItalic");
                AddAFM("Times-Italic");
                AddAFM("Times-Roman");
                AddAFM("ZapfDingbats");

                // alternative names from Adobe Supplement to the ISO 32000
                AddAFM("CourierCourierNew", "Courier");
                AddAFM("CourierNew", "Courier");
                AddAFM("CourierNew,Italic", "Courier-Oblique");
                AddAFM("CourierNew,Bold", "Courier-Bold");
                AddAFM("CourierNew,BoldItalic", "Courier-BoldOblique");
                AddAFM("Arial", "Helvetica");
                AddAFM("Arial,Italic", "Helvetica-Oblique");
                AddAFM("Arial,Bold", "Helvetica-Bold");
                AddAFM("Arial,BoldItalic", "Helvetica-BoldOblique");
                AddAFM("TimesNewRoman", "Times-Roman");
                AddAFM("TimesNewRoman,Italic", "Times-Italic");
                AddAFM("TimesNewRoman,Bold", "Times-Bold");
                AddAFM("TimesNewRoman,BoldItalic", "Times-BoldItalic");

                // Acrobat treats these fonts as "standard 14" too (at least Acrobat preflight says so)
                AddAFM("Symbol,Italic", "Symbol");
                AddAFM("Symbol,Bold", "Symbol");
                AddAFM("Symbol,BoldItalic", "Symbol");
                AddAFM("Times", "Times-Roman");
                AddAFM("Times,Italic", "Times-Italic");
                AddAFM("Times,Bold", "Times-Bold");
                AddAFM("Times,BoldItalic", "Times-BoldItalic");

                // PDFBOX-3457: PDF.js file bug864847.pdf
                AddAFM("ArialMT", "Helvetica");
                AddAFM("Arial-ItalicMT", "Helvetica-Oblique");
                AddAFM("Arial-BoldMT", "Helvetica-Bold");
                AddAFM("Arial-BoldItalicMT", "Helvetica-BoldOblique");
            }
            catch (IOException e)
            {
                throw new Exception("Bla bla", e);
            }
        }

        private Standard14Fonts()
        {
        }

        private static void AddAFM(string fontName)
        {
            AddAFM(fontName, fontName);
        }

        private static void AddAFM(string fontName, string afmName)
        {
            StandardNames.Add(fontName);
            StandardMapping[fontName] = afmName;

            if (StandardAFMMapping.ContainsKey(afmName))
            {
                StandardAFMMapping[fontName] = StandardAFMMapping.TryGetValue(afmName, out var fontMetrics) ? fontMetrics : null;
            }

            string resourceName = "/org/apache/pdfbox/resources/afm/" + afmName + ".afm";
            using (var afmStream = typeof(PdfType1Font).Assembly.getResourceAsStream(resourceName))
            {
                if (afmStream == null)
                {
                    throw new IOException(resourceName + " not found");
                }
                AFMParser parser = new AFMParser(afmStream);
                FontMetrics metric = parser.Parse(true);
                StandardAFMMapping[fontName] = metric;
            }
        }

        /**
		 * Returns the AFM for the given font.
		 * @param baseName base name of font
		 */
        public static FontMetrics GetAFM(string baseName)
        {
            return StandardAFMMapping.TryGetValue(baseName, out var fontMetrics) ? fontMetrics : null;
        }

        /**
		 * Returns true if the given font name a Standard 14 font.
		 * @param baseName base name of font
		 */
        public static bool ContainsName(string baseName)
        {
            return StandardNames.Contains(baseName);
        }

        /**
		 * Returns the set of Standard 14 font names, including additional names.
		 */
        public static HashSet<string> Names
        {
            get => StandardNames;
        }

        /**
		 * Returns the name of the actual font which the given font name maps to.
		 * @param baseName base name of font
		 */
        public static string GetMappedFontName(string baseName)
        {
            return StandardMapping.TryGetValue(baseName, out var name) ? name : null;
        }
    }
}
