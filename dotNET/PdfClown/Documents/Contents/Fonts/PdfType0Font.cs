/*
  Copyright 2009-2010 Stefano Chizzolini. http://www.pdfclown.org

  Contributors:
    * Stefano Chizzolini (original code developer, http://www.stefanochizzolini.it)

  This file should be part of the source code distribution of "PDF Clown library" (the
  Program): see the accompanying README files for more info.

  This Program is free software; you can redistribute it and/or modify it under the terms
  of the GNU Lesser General Public License as published by the Free Software Foundation;
  either version 3 of the License, or (at your option) any later version.

  This Program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY,
  either expressed or implied; without even the implied warranty of MERCHANTABILITY or
  FITNESS FOR A PARTICULAR PURPOSE. See the License for more details.

  You should have received a copy of the GNU Lesser General Public License along with this
  Program (see README files); if not, go to the GNU website (http://www.gnu.org/licenses/).

  Redistribution and use, with or without modification, are permitted provided that such
  redistributions retain the above copyright notice, license and disclaimer, along with
  this list of conditions.
*/

using bytes = PdfClown.Bytes;
using PdfClown.Documents;
using PdfClown.Files;
using PdfClown.Objects;
using PdfClown.Util;
using PdfClown.Util.IO;

using System;
using System.IO;
using System.Collections.Generic;
using SkiaSharp;
using System.Linq;
using System.Text;
using System.Diagnostics;
using PdfClown.Documents.Contents.Fonts.TTF.Model;
using PdfClown.Documents.Contents.Fonts.TTF;

namespace PdfClown.Documents.Contents.Fonts
{
    /**
      <summary>Composite font associated to a Type 0 CIDFont,
      containing glyph descriptions based on the Adobe Type 1 font format [PDF:1.6:5.6.3].</summary>
    */
    /*
      NOTE: Type 0 CIDFonts encompass several formats:
      * CFF;
      * OpenFont/CFF (in case "CFF" table's Top DICT has CIDFont operators).
    */
    [PDF(VersionEnum.PDF12)]
    public sealed class PdfType0Font : Font
    {
        #region constructors

        #endregion
        #region static
        #region interface
        #region public
        /**
         * Loads a TTF to be embedded and subset into a document as a Type 0 font. If you are loading a
         * font for AcroForm, then use the 3-parameter constructor instead.
         *
         * @param doc The PDF document that will hold the embedded font.
         * @param file A TrueType font.
         * @return A Type0 font with a CIDFontType2 descendant.
         * @throws IOException If there is an error reading the font file.
         */
        public static PdfType0Font Load(Document doc, Stream file)
        {
            return new PdfType0Font(doc, new TTFParser().Parse(file), true, true, false);
        }

        /**
         * Loads a TTF to be embedded and subset into a document as a Type 0 font. If you are loading a
         * font for AcroForm, then use the 3-parameter constructor instead.
         *
         * @param doc The PDF document that will hold the embedded font.
         * @param input An input stream of a TrueType font. It will be closed before returning.
         * @return A Type0 font with a CIDFontType2 descendant.
         * @throws IOException If there is an error reading the font stream.
         */
        public static PdfType0Font Load(Document doc, bytes.Buffer input)
        {
            return new PdfType0Font(doc, new TTFParser().Parse(input), true, true, false);
        }

        /**
         * Loads a TTF to be embedded into a document as a Type 0 font.
         *
         * @param doc The PDF document that will hold the embedded font.
         * @param input An input stream of a TrueType font. It will be closed before returning.
         * @param embedSubset True if the font will be subset before embedding. Set this to false when
         * creating a font for AcroForm.
         * @return A Type0 font with a CIDFontType2 descendant.
         * @throws IOException If there is an error reading the font stream.
         */
        public static PdfType0Font Load(Document doc, bytes.Buffer input, bool embedSubset)
        {
            return new PdfType0Font(doc, new TTFParser().Parse(input), embedSubset, true, false);
        }

        /**
         * Loads a TTF to be embedded into a document as a Type 0 font.
         *
         * @param doc The PDF document that will hold the embedded font.
         * @param ttf A TrueType font.
         * @param embedSubset True if the font will be subset before embedding. Set this to false when
         * creating a font for AcroForm.
         * @return A Type0 font with a CIDFontType2 descendant.
         * @throws IOException If there is an error reading the font stream.
         */
        public static PdfType0Font Load(Document doc, TrueTypeFont ttf, bool embedSubset)
        {
            return new PdfType0Font(doc, ttf, embedSubset, false, false);
        }

        /**
         * Loads a TTF to be embedded into a document as a vertical Type 0 font.
         *
         * @param doc The PDF document that will hold the embedded font.
         * @param file A TrueType font.
         * @return A Type0 font with a CIDFontType2 descendant.
         * @throws IOException If there is an error reading the font file.
         */
        public static PdfType0Font LoadVertical(Document doc, Stream file)
        {
            return new PdfType0Font(doc, new TTFParser().Parse(file), true, true, true);
        }

        /**
         * Loads a TTF to be embedded into a document as a vertical Type 0 font.
         *
         * @param doc The PDF document that will hold the embedded font.
         * @param input A TrueType font.
         * @return A Type0 font with a CIDFontType2 descendant.
         * @throws IOException If there is an error reading the font stream.
         */
        public static PdfType0Font LoadVertical(Document doc, bytes.Buffer input)
        {
            return new PdfType0Font(doc, new TTFParser().Parse(input), true, true, true);
        }

        /**
         * Loads a TTF to be embedded into a document as a vertical Type 0 font.
         *
         * @param doc The PDF document that will hold the embedded font.
         * @param input A TrueType font.
         * @param embedSubset True if the font will be subset before embedding
         * @return A Type0 font with a CIDFontType2 descendant.
         * @throws IOException If there is an error reading the font stream.
         */
        public static PdfType0Font LoadVertical(Document doc, bytes.Buffer input, bool embedSubset)
        {
            return new PdfType0Font(doc, new TTFParser().Parse(input), embedSubset, true, true);
        }

        /**
         * Loads a TTF to be embedded into a document as a vertical Type 0 font.
         *
         * @param doc The PDF document that will hold the embedded font.
         * @param ttf A TrueType font.
         * @param embedSubset True if the font will be subset before embedding
         * @return A Type0 font with a CIDFontType2 descendant.
         * @throws IOException If there is an error reading the font stream.
         */
        public static PdfType0Font LoadVertical(Document doc, TrueTypeFont ttf, bool embedSubset)
        {
            return new PdfType0Font(doc, ttf, embedSubset, false, true);
        }
        #endregion
        #endregion
        #endregion

        #region dynamic
        #region fields
        private bool isCMapPredefined;
        private bool isDescendantCJK;
        private CMap cMapUCS2;
        private PDCIDFontType2Embedder embedder;
        private GsubData gsubData;
        private ICmapLookup cmapLookup;
        private TrueTypeFont ttf;
        #endregion

        #region constructors       

        internal PdfType0Font(Document document, TrueTypeFont ttf, bool embedSubset, bool closeTTF, bool vertical)
            : base(document, new PdfDictionary())
        {
            if (vertical)
            {
                ttf.EnableVerticalSubstitutions();
            }

            gsubData = ttf.GsubData;
            cmapLookup = ttf.GetUnicodeCmapLookup();

            embedder = new PDCIDFontType2Embedder(document, Dictionary, ttf, embedSubset, this, vertical);
            CIDFont = embedder.GetCIDFont();
            LoadEncoding();
            if (closeTTF)
            {
                if (embedSubset)
                {
                    this.ttf = ttf;
                    document.registerTrueTypeFontForClosing(ttf);
                }
                else
                {
                    // the TTF is fully loaded and it is safe to close the underlying data source
                    ttf.Dispose();
                }
            }
        }

        internal PdfType0Font(PdfDirectObject baseObject) : base(baseObject)
        {
            LoadEncoding();
        }
        #endregion

        #region interface
        #region protected

        public PdfArray DescendantFonts
        {
            get => (PdfArray)BaseDataObject.Resolve(PdfName.DescendantFonts);
            set => BaseDataObject[PdfName.DescendantFonts] = value?.Reference;
        }
        /**
          <summary>Gets the CIDFont dictionary that is the descendant of this composite font.</summary>
        */
        public CIDFont CIDFont
        {
            get => CIDFont.WrapFont((PdfDictionary)DescendantFonts.Resolve(0), this);
            set
            {
                if (DescendantFonts == null)
                {
                    DescendantFonts = new PdfArray(new[] { value?.BaseObject });
                }
                else
                {
                    DescendantFonts[0] = value?.BaseObject;
                }
            }
        }

        public override FontDescriptor FontDescriptor
        {
            get => CIDFont.FontDescriptor;
            set => CIDFont.FontDescriptor = value;
        }

        public override int DefaultWidth
        {
            get => base.DefaultWidth;
            set => base.DefaultWidth = value;
        }

        public override SKMatrix FontMatrix
        {
            get => CIDFont.FontMatrix;
        }

        public override bool IsVertical
        {
            get => cmap.WMode == 1;
        }

        public override bool IsEmbedded
        {
            get => CIDFont.IsEmbedded;
        }

        public override SKRect BoundingBox
        {
            // Will be cached by underlying font
            get => CIDFont.BoundingBox;
        }

        public override float GetHeight(int code)
        {
            return CIDFont.GetHeight(code);
        }

        protected override byte[] Encode(int unicode)
        {
            return CIDFont.Encode(unicode);
        }

        public override bool HasExplicitWidth(int code)
        {
            return CIDFont.HasExplicitWidth(code);
        }

        public override float AverageFontWidth
        {
            get => CIDFont.AverageFontWidth;
        }

        public override SKPoint GetPositionVector(int code)
        {
            // units are always 1/1000 text space, font matrix is not used, see FOP-2252
            return CIDFont.GetPositionVector(code).Scale(-1 / 1000f);
        }


        public override SKPoint GetDisplacement(int code)
        {
            if (IsVertical)
            {
                return new SKPoint(0, CIDFont.GetVerticalDisplacementVectorY(code) / 1000f);
            }
            else
            {
                return base.GetDisplacement(code);
            }
        }

        public override float GetWidth(int code)
        {
            return CIDFont.GetWidth(code);
        }

        protected override float GetStandard14Width(int code)
        {
            throw new NotSupportedException("not supported");
        }

        public override float GetWidthFromFont(int code)
        {
            return CIDFont.GetWidthFromFont(code);
        }

        public override int ToUnicode(int code)
        {
            // try to use a ToUnicode CMap
            var unicode = base.ToUnicode(code);
            if (unicode > -1)
            {
                return unicode;
            }

            if ((isCMapPredefined || isDescendantCJK) && cMapUCS2 != null)
            {
                // if the font is composite and uses a predefined cmap (excluding Identity-H/V) then
                // or if its descendant font uses Adobe-GB1/CNS1/Japan1/Korea1

                // a) Map the character code to a character identifier (CID) according to the font?s CMap
                int cid = CodeToCID(code);

                // e) Map the CID according to the CMap from step d), producing a Unicode value
                return cMapUCS2.ToUnicode(cid);
            }
            else
            {
                //LOG.isWarnEnabled()
                if (!noUnicode.contains(code))
                {
                    // if no value has been produced, there is no way to obtain Unicode for the character.
                    String cid = "CID+" + CodeToCID(code);
                    Debug.WriteLine("warning: No Unicode mapping for " + cid + " (" + code + ") in font " + Name);
                    // we keep track of which warnings have been issued, so we don't log multiple times
                    noUnicode.add(code);
                }
                return -1;
            }
        }



        public override int ReadCode(Bytes.Buffer input, out byte[] bytes)
        {
            return CMap.ReadCode(input, out bytes);
        }

        /**
         * Returns the CID for the given character code. If not found then CID 0 is returned.
         *
         * @param code character code
         * @return CID
         */
        public int CodeToCID(int code)
        {
            return CIDFont.CodeToCID(code);
        }

        /**
         * Returns the GID for the given character code.
         *
         * @param code character code
         * @return GID
         */
        public int CodeToGID(int code)
        {
            return CIDFont.CodeToGID(code);
        }

        public override bool IsStandard14
        {
            get => false;
        }

        public override bool IsDamaged
        {
            get => CIDFont.IsDamaged;
        }

        public override SKPath GetPath(int code)
        {
            return CIDFont.GetPath(code);
        }

        public override SKPath GetNormalizedPath(int code)
        {
            return CIDFont.GetNormalizedPath(code);
        }

        public override bool HasGlyph(int code)
        {
            return CIDFont.HasGlyph(code);
        }

        public GsubData GsubData
        {
            get => gsubData;
        }

        public byte[] EncodeGlyphId(int glyphId)
        {
            return CIDFont.EncodeGlyphId(glyphId);
        }

        private void LoadEncoding()
        {
            var encoding = Dictionary.Resolve(PdfName.Encoding);
            if (encoding is PdfName encodingName)
            {
                // predefined CMap
                cmap = CMap.Get(encodingName);
                if (cmap != null)
                {
                    isCMapPredefined = true;
                }
                else
                {
                    throw new Exception("Missing required CMap");
                }
            }
            else if (encoding != null)
            {
                cmap = CMap.Get(encoding);
                if (cmap == null)
                {
                    throw new IOException("Missing required CMap");
                }
                else if (!cmap.HasCIDMappings)
                {
                    Debug.WriteLine("warning Invalid Encoding CMap in font " + Name);
                }
            }

            // check if the descendant font is CJK
            var ros = CIDFont.CIDSystemInfo;
            if (ros != null)
            {
                isDescendantCJK = "Adobe".Equals(ros.Registry, StringComparison.OrdinalIgnoreCase) &&
                        ("GB1".Equals(ros.Ordering, StringComparison.OrdinalIgnoreCase) ||
                         "CNS1".Equals(ros.Ordering, StringComparison.OrdinalIgnoreCase) ||
                         "Japan1".Equals(ros.Ordering, StringComparison.OrdinalIgnoreCase) ||
                         "Korea1".Equals(ros.Ordering, StringComparison.OrdinalIgnoreCase));
            }


            // if the font is composite and uses a predefined cmap (excluding Identity-H/V)
            // or whose descendant CIDFont uses the Adobe-GB1, Adobe-CNS1, Adobe-Japan1, or
            // Adobe-Korea1 character collection:

            if (isCMapPredefined && !(encoding == PdfName.IdentityH || encoding == PdfName.IdentityV) ||
                isDescendantCJK)
            {
                // a) Map the character code to a CID using the font's CMap
                // b) Obtain the ROS from the font's CIDSystemInfo
                // c) Construct a second CMap name by concatenating the ROS in the format "R-O-UCS2"
                // d) Obtain the CMap with the constructed name
                // e) Map the CID according to the CMap from step d), producing a Unicode value

                // todo: not sure how to interpret the PDF spec here, do we always override? or only when Identity-H/V?
                string strName = null;
                if (isDescendantCJK)
                {
                    strName = $"{ros.Registry}-{ros.Ordering}-{ros.Supplement}";
                }
                else if (encoding is PdfName encodingName2)
                {
                    strName = encodingName2.StringValue;
                }

                // try to find the corresponding Unicode (UC2) CMap
                if (strName != null)
                {
                    CMap prdCMap = CMap.Get(strName);
                    string ucs2Name = prdCMap.Registry + "-" + prdCMap.Ordering + "-UCS2";
                    cMapUCS2 = CMap.Get(ucs2Name);
                }
            }
        }


        protected override void OnLoad()
        {
            LoadEncoding();

            // Glyph widths.
            {
                glyphWidths = new Dictionary<int, int>();
                PdfArray glyphWidthObjects = CIDFont.Widths;
                if (glyphWidthObjects != null)
                {
                    for (IEnumerator<PdfDirectObject> iterator = glyphWidthObjects.GetEnumerator(); iterator.MoveNext();)
                    {
                        //TODO: this algorithm is valid only in case cid-to-gid mapping is identity (see cidtogid map)!!
                        /*
                          NOTE: Font widths are grouped in one of the following formats [PDF:1.6:5.6.3]:
                            1. startCID [glyphWidth1 glyphWidth2 ... glyphWidthn]
                            2. startCID endCID glyphWidth
                        */
                        int startCID = ((PdfInteger)iterator.Current).RawValue;
                        iterator.MoveNext();
                        PdfDirectObject glyphWidthObject2 = iterator.Current;
                        if (glyphWidthObject2 is PdfArray) // Format 1: startCID [glyphWidth1 glyphWidth2 ... glyphWidthn].
                        {
                            int cID = startCID;
                            foreach (PdfDirectObject glyphWidthObject in (PdfArray)glyphWidthObject2)
                            { glyphWidths[cID++] = ((IPdfNumber)glyphWidthObject).IntValue; }
                        }
                        else // Format 2: startCID endCID glyphWidth.
                        {
                            int endCID = ((PdfInteger)glyphWidthObject2).RawValue;
                            iterator.MoveNext();
                            int glyphWidth = ((IPdfNumber)iterator.Current).IntValue;
                            for (int cID = startCID; cID <= endCID; cID++)
                            { glyphWidths[cID] = glyphWidth; }
                        }
                    }
                }
            }
            // Default glyph width.
            {
                PdfInteger defaultWidthObject = (PdfInteger)BaseDataObject[PdfName.DW];
                if (defaultWidthObject != null)
                { DefaultWidth = defaultWidthObject.IntValue; }
            }
        }
        #endregion

        #region private



        #endregion
        #endregion
        #endregion
    }
}