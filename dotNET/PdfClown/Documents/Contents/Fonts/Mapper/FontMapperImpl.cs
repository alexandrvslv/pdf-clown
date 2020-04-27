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
using PdfClown.Documents.Contents.Fonts.TTF;
using PdfClown.Documents.Contents.Fonts.Type1;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PdfClown.Documents.Contents.Fonts
{

    /**
     * Font mapper, locates non-embedded fonts via a pluggable FontProvider.
     *
     * @author John Hewson
     */
    sealed class FontMapperImpl : IFontMapper
    {
        private static readonly FontCache fontCache = new FontCache(); // todo: static cache isn't ideal
        private FontProvider fontProvider;
        private Dictionary<string, FontInfo> fontInfoByName;
        private readonly TrueTypeFont lastResortFont;

        /** Dictionary of PostScript name substitutes, in priority order. */
        private readonly Dictionary<string, List<string>> substitutes = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        public FontMapperImpl()
        {
            // substitutes for standard 14 fonts
            substitutes.Add("Courier", new List<string> { "CourierNew", "CourierNewPSMT", "LiberationMono", "NimbusMonL-Regu" });
            substitutes.Add("Courier-Bold", new List<string> { "CourierNewPS-BoldMT", "CourierNew-Bold", "LiberationMono-Bold", "NimbusMonL-Bold" });
            substitutes.Add("Courier-Oblique", new List<string> { "CourierNewPS-ItalicMT", "CourierNew-Italic", "LiberationMono-Italic", "NimbusMonL-ReguObli" });
            substitutes.Add("Courier-BoldOblique", new List<string> { "CourierNewPS-BoldItalicMT", "CourierNew-BoldItalic", "LiberationMono-BoldItalic", "NimbusMonL-BoldObli" });
            substitutes.Add("Helvetica", new List<string> { "ArialMT", "Arial", "LiberationSans", "NimbusSanL-Regu" });
            substitutes.Add("Helvetica-Bold", new List<string> { "Arial-BoldMT", "Arial-Bold", "LiberationSans-Bold", "NimbusSanL-Bold" });
            substitutes.Add("Helvetica-Oblique", new List<string> { "Arial-ItalicMT", "Arial-Italic", "Helvetica-Italic", "LiberationSans-Italic", "NimbusSanL-ReguItal" });
            substitutes.Add("Helvetica-BoldOblique", new List<string> { "Arial-BoldItalicMT", "Helvetica-BoldItalic", "LiberationSans-BoldItalic", "NimbusSanL-BoldItal" });
            substitutes.Add("Times-Roman", new List<string> { "TimesNewRomanPSMT", "TimesNewRoman", "TimesNewRomanPS", "LiberationSerif", "NimbusRomNo9L-Regu" });
            substitutes.Add("Times-Bold", new List<string> { "TimesNewRomanPS-BoldMT", "TimesNewRomanPS-Bold", "TimesNewRoman-Bold", "LiberationSerif-Bold", "NimbusRomNo9L-Medi" });
            substitutes.Add("Times-Italic", new List<string> { "TimesNewRomanPS-ItalicMT", "TimesNewRomanPS-Italic", "TimesNewRoman-Italic", "LiberationSerif-Italic", "NimbusRomNo9L-ReguItal" });
            substitutes.Add("Times-BoldItalic", new List<string> { "TimesNewRomanPS-BoldItalicMT", "TimesNewRomanPS-BoldItalic", "TimesNewRoman-BoldItalic", "LiberationSerif-BoldItalic", "NimbusRomNo9L-MediItal" });
            substitutes.Add("Symbol", new List<string> { "Symbol", "SymbolMT", "StandardSymL" });
            substitutes.Add("ZapfDingbats", new List<string> { "ZapfDingbatsITC", "Dingbats", "MS-Gothic" });

            // Acrobat also uses alternative names for Standard 14 fonts, which we map to those above
            // these include names such as "Arial" and "TimesNewRoman"
            foreach (string baseName in Standard14Fonts.Names)
            {
                if (!substitutes.ContainsKey(baseName))
                {
                    string mappedName = Standard14Fonts.GetMappedFontName(baseName);
                    substitutes.Add(baseName, CopySubstitutes(mappedName));
                }
            }

            // -------------------------

            try
            {
                string ttfName = "fonts.ttf.LiberationSans-Regular";
                var ttfStream = typeof(IFontMapper).Assembly.GetManifestResourceStream(ttfName);
                if (ttfStream == null)
                {
                    throw new IOException("Error loading resource: " + ttfName);
                }
                TTFParser ttfParser = new TTFParser();
                lastResortFont = ttfParser.Parse(ttfStream);
            }
            catch (IOException e)
            {
                throw new Exception("Load LiberationSans", e);
            }
        }

        // lazy thread safe singleton
        private static class DefaultFontProvider
        {
            public static readonly FontProvider Instance = new FileSystemFontProvider(fontCache);
        }

        /**
         * Returns the font service provider. Defaults to using FileSystemFontProvider.
         */
        public FontProvider Provider
        {
            get
            {
                if (fontProvider == null)
                {
                    Provider = DefaultFontProvider.Instance;
                }
                return fontProvider;
            }
            set
            {
                fontInfoByName = CreateFontInfoByName(value.FontInfo);
                this.fontProvider = value;
            }
        }

        /**
         * Returns the font cache associated with this FontMapper. This method is needed by
         * FontProvider subclasses.
         */
        public FontCache FontCache
        {
            get => fontCache;
        }

        private Dictionary<string, FontInfo> CreateFontInfoByName(IEnumerable<FontInfo> fontInfoList)
        {
            Dictionary<string, FontInfo> map = new Dictionary<string, FontInfo>(StringComparer.Ordinal);
            foreach (FontInfo info in fontInfoList)
            {
                foreach (string name in GetPostScriptNames(info.PostScriptName))
                {
                    map.Add(name, info);
                }
            }
            return map;
        }

        /**
         * Gets alternative names, as seen in some PDFs, e.g. PDFBOX-142.
         */
        private HashSet<string> GetPostScriptNames(string postScriptName)
        {
            HashSet<string> names = new HashSet<string>(StringComparer.Ordinal);

            // built-in PostScript name
            names.Add(postScriptName);

            // remove hyphens (e.g. Arial-Black -> ArialBlack)
            names.Add(postScriptName.Replace("-", ""));

            return names;
        }

        /**
         * Copies a list of font substitutes, adding the original font at the start of the list.
         */
        private List<string> CopySubstitutes(string postScriptName)
        {
            return new List<string>(substitutes.TryGetValue(postScriptName, out var items) ? items : null);
        }

        /**
         * Adds a top-priority substitute for the given font.
         *
         * @param match PostScript name of the font to match
         * @param replace PostScript name of the font to use as a replacement
         */
        public void AddSubstitute(string match, string replace)
        {
            if (!substitutes.ContainsKey(match))
            {
                substitutes.Add(match, new List<string>());
            }
            substitutes[match].Add(replace);
        }

        /**
         * Returns the substitutes for a given font.
         */
        private List<string> GetSubstitutes(string postScriptName)
        {
            if (substitutes.TryGetValue(postScriptName.Replace(" ", ""), out List<string> subs))
            {
                return subs;
            }
            else
            {
                return new List<string>(0);
            }
        }

        /**
         * Attempts to find a good fallback based on the font descriptor.
         */
        private string GetFallbackFontName(FontDescriptor fontDescriptor)
        {
            string fontName;
            if (fontDescriptor != null)
            {
                // heuristic detection of bold
                bool isBold = false;
                string name = fontDescriptor.FontName;
                if (name != null)
                {
                    string lower = fontDescriptor.FontName.ToLower();
                    isBold = lower.IndexOf("bold", StringComparison.Ordinal) > -1 ||
                             lower.IndexOf("black", StringComparison.Ordinal) > -1 ||
                             lower.IndexOf("heavy", StringComparison.Ordinal) > -1;
                }

                // font descriptor flags should describe the style
                if ((fontDescriptor.Flags & FlagsEnum.FixedPitch) == FlagsEnum.FixedPitch)
                {
                    fontName = "Courier";
                    if (isBold && (fontDescriptor.Flags & FlagsEnum.Italic) == FlagsEnum.Italic)
                    {
                        fontName += "-BoldOblique";
                    }
                    else if (isBold)
                    {
                        fontName += "-Bold";
                    }
                    else if ((fontDescriptor.Flags & FlagsEnum.Italic) == FlagsEnum.Italic)
                    {
                        fontName += "-Oblique";
                    }
                }
                else if ((fontDescriptor.Flags & FlagsEnum.Serif) == FlagsEnum.Serif)
                {
                    fontName = "Times";
                    if (isBold && (fontDescriptor.Flags & FlagsEnum.Italic) == FlagsEnum.Italic)
                    {
                        fontName += "-BoldItalic";
                    }
                    else if (isBold)
                    {
                        fontName += "-Bold";
                    }
                    else if ((fontDescriptor.Flags & FlagsEnum.Italic) == FlagsEnum.Italic)
                    {
                        fontName += "-Italic";
                    }
                    else
                    {
                        fontName += "-Roman";
                    }
                }
                else
                {
                    fontName = "Helvetica";
                    if (isBold && (fontDescriptor.Flags & FlagsEnum.Italic) == FlagsEnum.Italic)
                    {
                        fontName += "-BoldOblique";
                    }
                    else if (isBold)
                    {
                        fontName += "-Bold";
                    }
                    else if ((fontDescriptor.Flags & FlagsEnum.Italic) == FlagsEnum.Italic)
                    {
                        fontName += "-Oblique";
                    }
                }
            }
            else
            {
                // if there is no FontDescriptor then we just fall back to Times Roman
                fontName = "Times-Roman";
            }
            return fontName;
        }

        /**
         * Finds a TrueType font with the given PostScript name, or a suitable substitute, or null.
         *
         * @param fontDescriptor FontDescriptor
         */
        public FontMapping<TrueTypeFont> GetTrueTypeFont(string baseFont, FontDescriptor fontDescriptor)
        {
            TrueTypeFont ttf = (TrueTypeFont)FindFont(FontFormat.TTF, baseFont);
            if (ttf != null)
            {
                return new FontMapping<TrueTypeFont>(ttf, false);
            }
            else
            {
                // fallback - todo: i.e. fuzzy match
                string fontName = GetFallbackFontName(fontDescriptor);
                ttf = (TrueTypeFont)FindFont(FontFormat.TTF, fontName);
                if (ttf == null)
                {
                    // we have to return something here as TTFs aren't strictly required on the system
                    ttf = lastResortFont;
                }
                return new FontMapping<TrueTypeFont>(ttf, true);
            }
        }

        /**
         * Finds a font with the given PostScript name, or a suitable substitute, or null. This allows
         * any font to be substituted with a PFB, TTF or OTF.
         *
         * @param fontDescriptor the FontDescriptor of the font to find
         */
        public FontMapping<BaseFont> GetBaseFont(string baseFont, FontDescriptor fontDescriptor)
        {
            BaseFont font = FindBaseFont(baseFont);
            if (font != null)
            {
                return new FontMapping<BaseFont>(font, false);
            }
            else
            {
                // fallback - todo: i.e. fuzzy match
                string fallbackName = GetFallbackFontName(fontDescriptor);
                font = FindBaseFont(fallbackName);
                if (font == null)
                {
                    // we have to return something here as TTFs aren't strictly required on the system
                    font = lastResortFont;
                }
                return new FontMapping<BaseFont>(font, true);
            }
        }

        /**
         * Finds a font with the given PostScript name, or a suitable substitute, or null.
         *
         * @param postScriptName PostScript font name
         */
        private BaseFont FindBaseFont(string postScriptName)
        {
            Type1Font t1 = (Type1Font)FindFont(FontFormat.PFB, postScriptName);
            if (t1 != null)
            {
                return t1;
            }

            TrueTypeFont ttf = (TrueTypeFont)FindFont(FontFormat.TTF, postScriptName);
            if (ttf != null)
            {
                return ttf;
            }

            OpenTypeFont otf = (OpenTypeFont)FindFont(FontFormat.OTF, postScriptName);
            if (otf != null)
            {
                return otf;
            }

            return null;
        }

        /**
         * Finds a font with the given PostScript name, or a suitable substitute, or null.
         *
         * @param postScriptName PostScript font name
         */
        private BaseFont FindFont(FontFormat format, string postScriptName)
        {
            // handle damaged PDFs, see PDFBOX-2884
            if (postScriptName == null)
            {
                return null;
            }

            // make sure the font provider is initialized
            if (fontProvider == null)
            {
                var temp = Provider;
            }

            // first try to match the PostScript name
            FontInfo info = GetFont(format, postScriptName);
            if (info != null)
            {
                return info.Font;
            }

            // remove hyphens (e.g. Arial-Black -> ArialBlack)
            info = GetFont(format, postScriptName.Replace("-", ""));
            if (info != null)
            {
                return info.Font;
            }

            // then try named substitutes
            foreach (string substituteName in GetSubstitutes(postScriptName))
            {
                info = GetFont(format, substituteName);
                if (info != null)
                {
                    return info.Font;
                }
            }

            // then try converting Windows names e.g. (ArialNarrow,Bold) -> (ArialNarrow-Bold)
            info = GetFont(format, postScriptName.Replace(",", "-"));
            if (info != null)
            {
                return info.Font;
            }

            // try appending "-Regular", works for Wingdings on windows
            info = GetFont(format, postScriptName + "-Regular");
            if (info != null)
            {
                return info.Font;
            }
            // no matches
            return null;
        }

        /**
         * Finds the named font with the given format.
         */
        private FontInfo GetFont(FontFormat format, string postScriptName)
        {
            // strip subset tag (happens when we substitute a corrupt embedded font, see PDFBOX-2642)
            var index = postScriptName.IndexOf('+');
            if (index > -1)
            {
                postScriptName = postScriptName.Substring(index + 1);
            }

            // look up the PostScript name
            if (fontInfoByName.TryGetValue(postScriptName, out FontInfo info) && info.Format == format)
            {
                return info;
            }
            return null;
        }

        /**
         * Finds a CFF CID-Keyed font with the given PostScript name, or a suitable substitute, or null.
         * This method can also map CJK fonts via their CIDSystemInfo (ROS).
         * 
         * @param fontDescriptor FontDescriptor
         * @param cidSystemInfo the CID system info, e.g. "Adobe-Japan1", if any.
         */
        public CIDFontMapping GetCIDFont(string baseFont, FontDescriptor fontDescriptor, CIDSystemInfo cidSystemInfo)
        {
            // try name match or substitute with OTF
            OpenTypeFont otf1 = (OpenTypeFont)FindFont(FontFormat.OTF, baseFont);
            if (otf1 != null)
            {
                return new CIDFontMapping(otf1, null, false);
            }

            // try name match or substitute with TTF
            TrueTypeFont ttf = (TrueTypeFont)FindFont(FontFormat.TTF, baseFont);
            if (ttf != null)
            {
                return new CIDFontMapping(null, ttf, false);
            }

            if (cidSystemInfo != null)
            {
                // "In Acrobat 3.0.1 and later, Type 0 fonts that use a CMap whose CIDSystemInfo
                // dictionary defines the Adobe-GB1, Adobe-CNS1 Adobe-Japan1, or Adobe-Korea1 character
                // collection can also be substituted." - Adobe Supplement to the ISO 32000

                string collection = cidSystemInfo.Registry + "-" + cidSystemInfo.Ordering;

                if (collection.Equals("Adobe-GB1", StringComparison.Ordinal) ||
                    collection.Equals("Adobe-CNS1", StringComparison.Ordinal) ||
                    collection.Equals("Adobe-Japan1", StringComparison.Ordinal) ||
                    collection.Equals("Adobe-Korea1", StringComparison.Ordinal))
                {
                    // try automatic substitutes via character collection
                    SortedSet<FontMatch> queue = GetFontMatches(fontDescriptor, cidSystemInfo);
                    FontMatch bestMatch = queue.FirstOrDefault();
                    if (bestMatch != null)
                    {
                        BaseFont font = bestMatch.info.Font;
                        if (font is OpenTypeFont openTypeFont)
                        {
                            return new CIDFontMapping(openTypeFont, null, true);
                        }
                        else if (font != null)
                        {
                            return new CIDFontMapping(null, font, true);
                        }
                    }
                }
            }

            // last-resort fallback
            return new CIDFontMapping(null, lastResortFont, true);
        }

        /**
         * Returns a list of matching fonts, scored by suitability. Positive scores indicate matches
         * for certain attributes, while negative scores indicate mismatches. Zero scores are neutral.
         * 
         * @param fontDescriptor FontDescriptor, always present.
         * @param cidSystemInfo Font's CIDSystemInfo, may be null.
         */
        private SortedSet<FontMatch> GetFontMatches(FontDescriptor fontDescriptor, CIDSystemInfo cidSystemInfo)
        {
            SortedSet<FontMatch> queue = new SortedSet<FontMatch>();

            foreach (FontInfo info in fontInfoByName.Values)
            {
                // filter by CIDSystemInfo, if given
                if (cidSystemInfo != null && !IsCharSetMatch(cidSystemInfo, info))
                {
                    continue;
                }

                FontMatch match = new FontMatch(info);

                // Panose is the most reliable
                if (fontDescriptor.Style?.Panose != null && info.Panose != null)
                {
                    PanoseClassification panose = fontDescriptor.Style.Panose.PanoseClassification;
                    if (panose.FamilyKind == info.Panose.FamilyKind)
                    {
                        if (panose.FamilyKind == 0 &&
                            (info.PostScriptName.IndexOf("barcode", StringComparison.OrdinalIgnoreCase) > -1 ||
                             info.PostScriptName.StartsWith("Code", StringComparison.Ordinal)) &&
                            !ProbablyBarcodeFont(fontDescriptor))
                        {
                            // PDFBOX-4268: ignore barcode font if we aren't searching for one.
                            continue;
                        }
                        // serifs
                        if (panose.SerifStyle == info.Panose.SerifStyle)
                        {
                            // exact match
                            match.score += 2;
                        }
                        else if (panose.SerifStyle >= 2 && panose.SerifStyle <= 5 &&
                                 info.Panose.SerifStyle >= 2 &&
                                 info.Panose.SerifStyle <= 5)
                        {
                            // cove (serif)
                            match.score += 1;
                        }
                        else if (panose.SerifStyle >= 11 && panose.SerifStyle <= 13 &&
                                 info.Panose.SerifStyle >= 11 &&
                                 info.Panose.SerifStyle <= 13)
                        {
                            // sans-serif
                            match.score += 1;
                        }
                        else if (panose.SerifStyle != 0 && info.Panose.SerifStyle != 0)
                        {
                            // mismatch
                            match.score -= 1;
                        }

                        // weight
                        int weight = info.Panose.Weight;
                        int weightClass = info.WeightClassAsPanose;
                        if (Math.Abs(weight - weightClass) > 2)
                        {
                            // inconsistent data in system font, usWeightClass wins
                            weight = weightClass;
                        }

                        if (panose.Weight == weight)
                        {
                            // exact match
                            match.score += 2;
                        }
                        else if (panose.Weight > 1 && weight > 1)
                        {
                            float dist = Math.Abs(panose.Weight - weight);
                            match.score += 1 - dist * 0.5;
                        }

                        // todo: italic
                        // ...
                    }
                }
                else if (fontDescriptor.FontWeight > 0 && info.WeightClass > 0)
                {
                    // usWeightClass is pretty reliable
                    float dist = (float)Math.Abs((float)fontDescriptor.FontWeight - info.WeightClass);
                    match.score += 1 - (dist / 100) * 0.5;
                }
                // todo: italic
                // ...

                queue.Add(match);
            }
            return queue;
        }

        private bool ProbablyBarcodeFont(FontDescriptor fontDescriptor)
        {
            string ff = fontDescriptor.FontFamily;
            if (ff == null)
            {
                ff = "";
            }
            string fn = fontDescriptor.FontName;
            if (fn == null)
            {
                fn = "";
            }
            return ff.StartsWith("Code", StringComparison.Ordinal) || ff.IndexOf("barcode", StringComparison.OrdinalIgnoreCase) > -1 ||
                   fn.StartsWith("Code", StringComparison.Ordinal) || fn.IndexOf("barcode", StringComparison.OrdinalIgnoreCase) > -1;
        }

        /**
         * Returns true if the character set described by CIDSystemInfo is present in the given font.
         * Only applies to Adobe-GB1, Adobe-CNS1, Adobe-Japan1, Adobe-Korea1, as per the PDF spec.
         */
        private bool IsCharSetMatch(CIDSystemInfo cidSystemInfo, FontInfo info)
        {
            if (info.CIDSystemInfo != null)
            {
                return info.CIDSystemInfo.Registry.Equals(cidSystemInfo.Registry, StringComparison.Ordinal) &&
                       info.CIDSystemInfo.Ordering.Equals(cidSystemInfo.Ordering, StringComparison.Ordinal);
            }
            else
            {
                long codePageRange = info.CodePageRange;

                long JIS_JAPAN = 1 << 17;
                long CHINESE_SIMPLIFIED = 1 << 18;
                long KOREAN_WANSUNG = 1 << 19;
                long CHINESE_TRADITIONAL = 1 << 20;
                long KOREAN_JOHAB = 1 << 21;

                if ("MalgunGothic-Semilight".Equals(info.PostScriptName, StringComparison.Ordinal))
                {
                    // PDFBOX-4793 and PDF.js 10699: This font has only Korean, but has bits 17-21 set.
                    codePageRange &= ~(JIS_JAPAN | CHINESE_SIMPLIFIED | CHINESE_TRADITIONAL);
                }
                if (cidSystemInfo.Ordering.Equals("GB1", StringComparison.Ordinal) &&
                        (codePageRange & CHINESE_SIMPLIFIED) == CHINESE_SIMPLIFIED)
                {
                    return true;
                }
                else if (cidSystemInfo.Ordering.Equals("CNS1", StringComparison.Ordinal) &&
                        (codePageRange & CHINESE_TRADITIONAL) == CHINESE_TRADITIONAL)
                {
                    return true;
                }
                else if (cidSystemInfo.Ordering.Equals("Japan1", StringComparison.Ordinal) &&
                        (codePageRange & JIS_JAPAN) == JIS_JAPAN)
                {
                    return true;
                }
                else
                {
                    return cidSystemInfo.Ordering.Equals("Korea1", StringComparison.Ordinal) &&
                            ((codePageRange & KOREAN_WANSUNG) == KOREAN_WANSUNG ||
                             (codePageRange & KOREAN_JOHAB) == KOREAN_JOHAB);
                }
            }
        }

        /**
         * A potential match for a font substitution.
         */
        private class FontMatch : IComparable<FontMatch>
        {
            internal double score;
            internal readonly FontInfo info;

            public FontMatch(FontInfo info)
            {
                this.info = info;
            }

            public int CompareTo(FontMatch match)
            {
                return match.score.CompareTo(score);
            }
        }

        /**
         * For debugging. Prints all matches and returns the best match.
         */
        private FontMatch PrintMatches(SortedSet<FontMatch> queue)
        {
            FontMatch bestMatch = queue.FirstOrDefault();
            System.Console.WriteLine("-------");
            while (queue.Count > 0)
            {
                var match = queue.FirstOrDefault();
                queue.Remove(match);
                FontInfo info = match.info;
                System.Console.WriteLine(match.score + " | " + info.MacStyle + " " +
                                   info.FamilyClass + " " + info.Panose + " " +
                                   info.CIDSystemInfo + " " + info.PostScriptName + " " +
                                   info.Format);
            }
            System.Console.WriteLine("-------");
            return bestMatch;
        }
    }
}