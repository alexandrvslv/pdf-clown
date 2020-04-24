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
using PdfClown.Objects;
using PdfClown.Util.Collections.Generic;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PdfClown.Documents.Contents.Fonts
{
    /**
     * Common functionality for embedding TrueType fonts.
     *
     * @author Ben Litchfield
     * @author John Hewson
     */
    abstract class TrueTypeEmbedder : ISubsetter
    {
        private static readonly int ITALIC = 1;
        private static readonly int OBLIQUE = 512;
        private static readonly string BASE25 = "BCDEFGHIJKLMNOPQRSTUVWXYZ";

        private readonly Document document;
        protected TrueTypeFont ttf;
        protected FontDescriptor fontDescriptor;

        protected readonly ICmapLookup cmapLookup;
        private readonly ISet<int> subsetCodePoints = new HashSet<int>();
        private readonly bool embedSubset;

        private readonly ISet<int> allGlyphIds = new HashSet<int>();

        /**
		 * Creates a new TrueType font for embedding.
		 */
        public TrueTypeEmbedder(Document document, PdfDictionary dict, TrueTypeFont ttf,
                         bool embedSubset)
        {
            this.document = document;
            this.embedSubset = embedSubset;
            this.ttf = ttf;
            fontDescriptor = CreateFontDescriptor(ttf);

            if (!IsEmbeddingPermitted(ttf))
            {
                throw new IOException("This font does not permit embedding");
            }

            if (!embedSubset)
            {
                // full embedding
                PdfStream stream = new PdfStream(ttf.OriginalData);
                stream.Header[PdfName.Length1] = PdfInteger.Get(ttf.OriginalDataSize);
                fontDescriptor.FontFile2 = new FontFile(document, stream);
            }

            dict[PdfName.BaseFont] = new PdfName(ttf.Name);

            // choose a Unicode "cmap"
            cmapLookup = ttf.GetUnicodeCmapLookup();
        }

        public void BuildFontFile2(Bytes.Buffer ttfStream)
        {
            PdfStream stream = new PdfStream(ttfStream);

            // as the stream was closed within the PdfStream constructor, we have to recreate it
            using (var input = (Bytes.Buffer)stream.ExtractBody(true))
            {
                ttf = new TTFParser().ParseEmbedded(input);
                if (!IsEmbeddingPermitted(ttf))
                {
                    throw new IOException("This font does not permit embedding");
                }
                if (fontDescriptor == null)
                {
                    fontDescriptor = CreateFontDescriptor(ttf);
                }
            }
            stream.Header[PdfName.Length1] = PdfInteger.Get(ttf.OriginalDataSize);
            fontDescriptor.FontFile2 = new FontFile(document, stream);
        }

        /**
		 * Returns true if the fsType in the OS/2 table permits embedding.
		 */
        private bool IsEmbeddingPermitted(TrueTypeFont ttf)
        {
            if (ttf.OS2Windows != null)

            {
                int fsType = ttf.OS2Windows.FsType;
                int exclusive = fsType & 0x8; // bits 0-3 are a set of exclusive bits

                if ((exclusive & OS2WindowsMetricsTable.FSTYPE_RESTRICTED) ==
                                 OS2WindowsMetricsTable.FSTYPE_RESTRICTED)
                {
                    // restricted License embedding
                    return false;
                }
                else if ((exclusive & OS2WindowsMetricsTable.FSTYPE_BITMAP_ONLY) ==
                                     OS2WindowsMetricsTable.FSTYPE_BITMAP_ONLY)
                {
                    // bitmap embedding only
                    return false;
                }
            }
            return true;
        }

        /**
		 * Returns true if the fsType in the OS/2 table permits subsetting.
		 */
        private bool IsSubsettingPermitted(TrueTypeFont ttf)
        {
            if (ttf.OS2Windows != null)

            {
                int fsType = ttf.OS2Windows.FsType;
                if ((fsType & OS2WindowsMetricsTable.FSTYPE_NO_SUBSETTING) ==
                              OS2WindowsMetricsTable.FSTYPE_NO_SUBSETTING)
                {
                    return false;
                }
            }
            return true;
        }

        /**
		 * Creates a new font descriptor dictionary for the given TTF.
		 */
        private FontDescriptor CreateFontDescriptor(TrueTypeFont ttf)
        {
            FontDescriptor fd = new FontDescriptor(new PdfDictionary());
            fd.FontName = ttf.Name;

            OS2WindowsMetricsTable os2 = ttf.OS2Windows;
            PostScriptTable post = ttf.PostScript;

            // Flags
            var flags = (FlagsEnum)0;
            flags |= (post.IsFixedPitch > 0 || ttf.HorizontalHeader.NumberOfHMetrics == 1) ? FlagsEnum.FixedPitch : 0;

            int fsSelection = os2.FsSelection;
            flags |= ((fsSelection & (ITALIC | OBLIQUE)) != 0) ? FlagsEnum.Italic : 0;

            switch (os2.FamilyClass)
            {
                case OS2WindowsMetricsTable.FAMILY_CLASS_CLAREDON_SERIFS:
                case OS2WindowsMetricsTable.FAMILY_CLASS_FREEFORM_SERIFS:
                case OS2WindowsMetricsTable.FAMILY_CLASS_MODERN_SERIFS:
                case OS2WindowsMetricsTable.FAMILY_CLASS_OLDSTYLE_SERIFS:
                case OS2WindowsMetricsTable.FAMILY_CLASS_SLAB_SERIFS:
                    flags |= FlagsEnum.Serif;
                    break;
                case OS2WindowsMetricsTable.FAMILY_CLASS_SCRIPTS:
                    flags |= FlagsEnum.Script;
                    break;
                default:
                    break;
            }

            fd.FontWeight = os2.WeightClass;

            flags |= FlagsEnum.Symbolic;
            flags &= ~FlagsEnum.Nonsymbolic;

            fd.Flags = flags;
            // ItalicAngle
            fd.ItalicAngle = post.ItalicAngle;

            // FontBBox
            HeaderTable header = ttf.Header;
            float scaling = 1000f / header.UnitsPerEm;
            var skRect = new SKRect(
                header.XMin * scaling,
                header.YMin * scaling,
                header.XMax * scaling,
                header.YMax * scaling
                );


            Rectangle rect = new Rectangle(skRect);

            fd.FontBBox = rect;

            // Ascent, Descent
            HorizontalHeaderTable hHeader = ttf.HorizontalHeader;
            fd.Ascent = hHeader.Ascender * scaling;
            fd.Descent = hHeader.Descender * scaling;

            // CapHeight, XHeight
            if (os2.Version >= 1.2)
            {
                fd.CapHeight = os2.CapHeight * scaling;
                fd.XHeight = os2.Height * scaling;
            }
            else
            {
                var capHPath = ttf.GetPath("H");
                if (capHPath != null)
                {
                    fd.CapHeight = (float)Math.Round(capHPath.Bounds.Bottom * scaling);
                }
                else
                {
                    // estimate by summing the typographical +ve ascender and -ve descender
                    fd.CapHeight = os2.TypoAscender + (os2.TypoDescender * scaling);
                }
                var xPath = ttf.GetPath("x");
                if (xPath != null)
                {
                    fd.XHeight = (float)Math.Round(xPath.Bounds.Bottom * scaling);
                }
                else
                {
                    // estimate by halving the typographical ascender
                    fd.XHeight = os2.TypoAscender / (2.0f * scaling);
                }
            }

            // StemV - there's no true TTF equivalent of this, so we estimate it
            fd.StemV = skRect.Width * .13f;

            return fd;
        }

        /**
		 * Returns the font descriptor.
		 */
        public FontDescriptor FontDescriptor
        {
            get => fontDescriptor;
        }

        public virtual void AddToSubset(int codePoint)
        {
            subsetCodePoints.Add(codePoint);
        }

        public void AddGlyphIds(ISet<int> glyphIds)
        {
            allGlyphIds.AddAll(glyphIds);
        }

        public virtual void Subset()
        {
            if (!IsSubsettingPermitted(ttf))
            {
                throw new IOException("This font does not permit subsetting");
            }

            if (!embedSubset)
            {
                throw new InvalidOperationException("Subsetting is disabled");
            }

            // PDF spec required tables (if present), all others will be removed
            List<string> tables = new List<string>();
            tables.Add("head");
            tables.Add("hhea");
            tables.Add("loca");
            tables.Add("maxp");
            tables.Add("cvt ");
            tables.Add("prep");
            tables.Add("glyf");
            tables.Add("hmtx");
            tables.Add("fpgm");
            // Windows ClearType
            tables.Add("gasp");

            // set the GIDs to subset
            TTFSubsetter subsetter = new TTFSubsetter(ttf, tables);
            subsetter.AddAll(subsetCodePoints);

            if (allGlyphIds.Count > 0)
            {
                subsetter.AddGlyphIds(allGlyphIds);
            }

            // calculate deterministic tag based on the chosen subset
            Dictionary<int, int> gidToCid = subsetter.GetGIDMap();
            string tag = GetTag(gidToCid);
            subsetter.SetPrefix(tag);

            // save the subset font
            using (var output = new MemoryStream())
            {
                subsetter.WriteToStream(output);

                // re-build the embedded font
                BuildSubset(new Bytes.Buffer(output), tag, gidToCid);
            }
            ttf.Dispose();
        }

        /**
		 * Returns true if the font needs to be subset.
		 */
        public bool NeedsSubset
        {
            get => embedSubset;
        }

        /**
		 * Rebuild a font subset.
		 */
        protected abstract void BuildSubset(Bytes.Buffer ttfSubset, string tag, Dictionary<int, int> gidToCid);
        /**
		 * Returns an uppercase 6-character unique tag for the given subset.
		 */
        public string GetTag(Dictionary<int, int> gidToCid)
        {
            // deterministic
            long num = gidToCid.GetHashCode();

            // base25 encode
            var sb = new StringBuilder();
            do
            {
                long div = num / 25;
                int mod = (int)(num % 25);
                sb.Append(BASE25[mod]);
                num = div;
            } while (num != 0 && sb.Length < 6);

            // pad
            while (sb.Length < 6)
            {
                sb.Insert(0, 'A');
            }

            sb.Append('+');
            return sb.ToString();
        }
    }

    public interface ISubsetter
    {
        /**
         * Adds the given Unicode code point to this subset.
         * 
         * @param codePoint Unicode code point
         */
        void AddToSubset(int codePoint);

        /**
         * Subset this font now.
         * 
         * @throws IOException if the font could not be read
         */
        void Subset();
    }

}