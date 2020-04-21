/*
  Copyright 2009-2015 Stefano Chizzolini. http://www.pdfclown.org

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

namespace PdfClown.Documents.Contents.Fonts
{
    /**
      <summary>Composite font, also called Type 0 font [PDF:1.6:5.6].</summary>
      <remarks>Do not confuse it with <see cref="PdfType0Font">Type 0 CIDFont</see>: the latter is
      a composite font descendant describing glyphs based on Adobe Type 1 font format.</remarks>
    */
    [PDF(VersionEnum.PDF12)]
    public abstract class CompositeFont : Font
    {

        #region static
        #region interface
        #region public
        new public static CompositeFont Get(Document context, bytes::IInputStream fontData)
        {
            OpenFontParser parser = new OpenFontParser(fontData);
            switch (parser.OutlineFormat)
            {
                case OpenFontParser.OutlineFormatEnum.PostScript:
                    return new PdfType0Font(context, parser);
                case OpenFontParser.OutlineFormatEnum.TrueType:
                    return new PdfType2Font(context, parser);
            }
            throw new NotSupportedException("Unknown composite font format.");
        }
        #endregion
        #endregion
        #endregion

        #region dynamic
        #region fields
        private CMap ucs2CMap;
        private bool isCMapPredefined;
        private bool isDescendantCJK;
        private CMap cMapUCS2;
        #endregion

        #region constructors
        internal CompositeFont(Document context, OpenFontParser parser)
            : base(context)
        { Load(parser); }

        protected CompositeFont(PdfDirectObject baseObject)
            : base(baseObject)
        { }
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
        protected CIDFont CIDFont
        {
            get => CIDFont.WrapFont((PdfDictionary)DescendantFonts.Resolve(0), this);
            set => DescendantFonts[0] = value?.BaseObject;
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


        public override bool IsEmbedded
        {
            get => CIDFont.IsEmbedded;
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
                if (LOG.isWarnEnabled() && !noUnicode.contains(code))
                {
                    // if no value has been produced, there is no way to obtain Unicode for the character.
                    String cid = "CID+" + CodeToCID(code);
                    Debug.WriteLine("warning: No Unicode mapping for " + cid + " (" + code + ") in font " + Name);
                    // we keep track of which warnings have been issued, so we don't log multiple times
                    noUnicode.add(code);
                }
                return null;
            }
        }

        //public override int ToUnicode(int code)
        //{
        //    // try to use a ToUnicode CMap
        //    var unicode = base.ToUnicode(code);
        //    if (unicode > -1)
        //    {
        //        return unicode;
        //    }

        //    if (ucs2CMap != null)
        //    {
        //        // if the font is composite and uses a predefined cmap (excluding Identity-H/V) then
        //        // or if its descendant font uses Adobe-GB1/CNS1/Japan1/Korea1

        //        // a) Map the character code to a character identifier (CID) according to the font?s CMap
        //        int cid = cmap.ToCID(code);

        //        // e) Map the CID according to the CMap from step d), producing a Unicode value
        //        return ucs2CMap.ToUnicode(cid);
        //    }
        //    else
        //    {
        //        return -1;
        //    }
        //}


        public override SKRect BoundingBox
        {
            // Will be cached by underlying font
            get => CIDFont.BoundingBox;
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

        protected void LoadEncoding()
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
        /**
          <summary>Loads the font data.</summary>
        */
        private void Load(OpenFontParser parser)
        {
            glyphIndexes = parser.GlyphIndexes;
            glyphKernings = parser.GlyphKernings;
            glyphWidths = parser.GlyphWidths;

            PdfDictionary baseDataObject = BaseDataObject;

            // BaseFont.
            baseDataObject[PdfName.BaseFont] = new PdfName(parser.FontName);

            // Subtype.
            baseDataObject[PdfName.Subtype] = PdfName.Type0;

            // Encoding.
            baseDataObject[PdfName.Encoding] = PdfName.IdentityH; //TODO: this is a simplification (to refine later).

            // Descendant font.
            PdfDictionary cidFontDictionary = new PdfDictionary(
              new PdfName[] { PdfName.Type },
              new PdfDirectObject[] { PdfName.Font }
              ); // CIDFont dictionary [PDF:1.6:5.6.3].
            {
                // Subtype.
                // FIXME: verify proper Type 0 detection.
                cidFontDictionary[PdfName.Subtype] = PdfName.CIDFontType2;

                // BaseFont.
                cidFontDictionary[PdfName.BaseFont] = new PdfName(parser.FontName);

                // CIDSystemInfo.
                cidFontDictionary[PdfName.CIDSystemInfo] = new PdfDictionary(
                  new PdfName[]
                  {
            PdfName.Registry,
            PdfName.Ordering,
            PdfName.Supplement
                  },
                  new PdfDirectObject[]
                  {
            new PdfTextString("Adobe"),
            new PdfTextString("Identity"),
            PdfInteger.Get(0)
                  }
                  ); // Generic predefined CMap (Identity-H/V (Adobe-Identity-0)) [PDF:1.6:5.6.4].

                // FontDescriptor.
                cidFontDictionary[PdfName.FontDescriptor] = Load_CreateFontDescriptor(parser);

                // Encoding.
                Load_CreateEncoding(baseDataObject, cidFontDictionary);
            }
            baseDataObject[PdfName.DescendantFonts] = new PdfArray(new PdfDirectObject[] { File.Register(cidFontDictionary) });

            Load();
        }

        /**
          <summary>Creates the character code mapping for composite fonts.</summary>
        */
        private void Load_CreateEncoding(PdfDictionary font, PdfDictionary cidFont)
        {
            /*
              NOTE: Composite fonts map text shown by content stream strings through a 2-level encoding
              scheme:
                character code -> CID (character index) -> GID (glyph index)
              This works for rendering purposes, but if we want our text data to be intrinsically meaningful,
              we need a further mapping towards some standard character identification scheme (Unicode):
                Unicode <- character code -> CID -> GID
              Such mapping may be provided by a known CID collection or (in case of custom encodings like
              Identity-H) by an explicit ToUnicode CMap.
              CID -> GID mapping is typically identity, that is CIDS correspond to GIDS, so we don't bother
              about that. Our base encoding is Identity-H, that is character codes correspond to CIDs;
              however, sometimes a font maps multiple Unicode codepoints to the same GID (for example, the
              hyphen glyph may be associated to the hyphen (\u2010) and minus (\u002D) symbols), breaking
              the possibility to recover their original Unicode values once represented as character codes
              in content stream strings. In this case, we are forced to remap the exceeding codes and
              generate an explicit CMap (TODO: I tried to emit a differential CMap using the usecmap
              operator in order to import Identity-H as base encoding, but it failed in several engines
              (including Acrobat, Ghostscript, Poppler, whilst it surprisingly worked with pdf.js), so we
              have temporarily to stick with full CMaps).
            */

            // Encoding [PDF:1.7:5.6.1,5.6.4].
            PdfDirectObject encodingObject = PdfName.IdentityH;
            SortedDictionary<ByteArray, int> sortedCodes;
            {
                codes = new BiDictionary<ByteArray, int>(glyphIndexes.Count);
                int lastRemappedCharCodeValue = 0;
                IList<int> removedGlyphIndexKeys = null;
                foreach (KeyValuePair<int, int> glyphIndexEntry in glyphIndexes.ToList())
                {
                    int glyphIndex = glyphIndexEntry.Value;
                    var charCode = new byte[]
                      {
                          (byte)((glyphIndex >> 8) & 0xFF),
                          (byte)(glyphIndex & 0xFF)
                      };

                    // Checking for multiple Unicode codepoints which map to the same glyph index...
                    /*
                      NOTE: In case the same glyph index maps to multiple Unicode codepoints, we are forced to
                      alter the identity encoding creating distinct cmap entries for the exceeding codepoints.
                    */
                    if (codes.ContainsKey(charCode))
                    {
                        if (glyphIndex == 0) // .notdef glyph already mapped.
                        {
                            if (removedGlyphIndexKeys == null)
                            { removedGlyphIndexKeys = new List<int>(); }
                            removedGlyphIndexKeys.Add(glyphIndexEntry.Key);
                            continue;
                        }

                        // Assigning the new character code...
                        /*
                          NOTE: As our base encoding is identity, we have to look for a value that doesn't
                          collide with existing glyph indices.
                        */
                        while (glyphIndexes.ContainsValue(++lastRemappedCharCodeValue)) ;
                        charCode.Data[0] = (byte)((lastRemappedCharCodeValue >> 8) & 0xFF);
                        charCode.Data[1] = (byte)(lastRemappedCharCodeValue & 0xFF);
                    }
                    else if (glyphIndex == 0) // .notdef glyph.
                    { DefaultCode = glyphIndexEntry.Key; }

                    codes[charCode] = glyphIndexEntry.Key;
                }
                if (removedGlyphIndexKeys != null)
                {
                    foreach (int removedGlyphIndexKey in removedGlyphIndexKeys)
                    { glyphIndexes.Remove(removedGlyphIndexKey); }
                }
                sortedCodes = new SortedDictionary<ByteArray, int>(codes);
                if (lastRemappedCharCodeValue > 0) // Custom encoding.
                {
                    string cmapName = "Custom";
                    bytes::IBuffer cmapBuffer = CMapBuilder.Build(
                      CMapBuilder.EntryTypeEnum.CID,
                      cmapName,
                      sortedCodes,
                      delegate (KeyValuePair<ByteArray, int> codeEntry)
                      { return glyphIndexes[codeEntry.Value]; }
                      );
                    encodingObject = File.Register(
                        new PdfStream(
                            new PdfDictionary(
                                new PdfName[]
                                {
                                    PdfName.Type,
                                    PdfName.CMapName,
                                    PdfName.CIDSystemInfo
                                },
                                new PdfDirectObject[]
                                {
                                    PdfName.CMap,
                                    new PdfName(cmapName),
                                    new PdfDictionary(
                                        new PdfName[]
                                        {
                                            PdfName.Registry,
                                            PdfName.Ordering,
                                            PdfName.Supplement
                                        },
                                        new PdfDirectObject[]
                                        {
                                            PdfTextString.Get("Adobe"),
                                            PdfTextString.Get("Identity"),
                                            PdfInteger.Get(0)
                                        }
                                        )
                                }
                                ),
                        cmapBuffer
                        )
                      );
                }
            }
            font[PdfName.Encoding] = encodingObject; // Character-code-to-CID mapping.
            cidFont[PdfName.CIDToGIDMap] = PdfName.Identity; // CID-to-glyph-index mapping.

            // ToUnicode [PDF:1.6:5.9.2].
            PdfDirectObject toUnicodeObject = null;
            {
                bytes::IBuffer toUnicodeBuffer = CMapBuilder.Build(
                  CMapBuilder.EntryTypeEnum.BaseFont,
                  null,
                  sortedCodes,
                  delegate (KeyValuePair<ByteArray, int> codeEntry)
                  { return codeEntry.Value; }
                  );
                toUnicodeObject = File.Register(new PdfStream(toUnicodeBuffer));
            }
            font[PdfName.ToUnicode] = toUnicodeObject; // Character-code-to-Unicode mapping.

            // Glyph widths.
            PdfArray widthsObject = new PdfArray();
            {
                int lastGlyphIndex = -10;
                PdfArray lastGlyphWidthRangeObject = null;
                foreach (int glyphIndex in glyphIndexes.Values.OrderBy(x => x).ToList())
                {
                    int width;
                    if (!glyphWidths.TryGetValue(glyphIndex, out width))
                    { width = 0; }
                    if (glyphIndex - lastGlyphIndex != 1)
                    {
                        widthsObject.Add(PdfInteger.Get(glyphIndex));
                        widthsObject.Add(lastGlyphWidthRangeObject = new PdfArray());
                    }
                    lastGlyphWidthRangeObject.Add(PdfInteger.Get(width));
                    lastGlyphIndex = glyphIndex;
                }
            }
            cidFont[PdfName.W] = widthsObject; // Glyph widths.
        }

        /**
          <summary>Creates the font descriptor.</summary>
        */
        private PdfReference Load_CreateFontDescriptor(OpenFontParser parser)
        {
            PdfDictionary fontDescriptor = new PdfDictionary();
            {
                OpenFontParser.FontMetrics metrics = parser.Metrics;

                // Type.
                fontDescriptor[PdfName.Type] = PdfName.FontDescriptor;

                // FontName.
                fontDescriptor[PdfName.FontName] = BaseDataObject[PdfName.BaseFont];

                // Flags [PDF:1.6:5.7.1].
                FlagsEnum flags = 0;
                if (metrics.IsFixedPitch)
                { flags |= FlagsEnum.FixedPitch; }
                if (metrics.IsCustomEncoding)
                { flags |= FlagsEnum.Symbolic; }
                else
                { flags |= FlagsEnum.Nonsymbolic; }
                fontDescriptor[PdfName.Flags] = PdfInteger.Get(Convert.ToInt32(flags));

                // FontBBox.
                fontDescriptor[PdfName.FontBBox] = new Rectangle(
                  new SKPoint(metrics.XMin * metrics.UnitNorm, metrics.YMin * metrics.UnitNorm),
                  new SKPoint(metrics.XMax * metrics.UnitNorm, metrics.YMax * metrics.UnitNorm)
                  ).BaseDataObject;

                // ItalicAngle.
                fontDescriptor[PdfName.ItalicAngle] = PdfReal.Get(metrics.ItalicAngle);

                // Ascent.
                fontDescriptor[PdfName.Ascent] = PdfReal.Get(
                  metrics.STypoAscender == 0
                    ? metrics.Ascender * metrics.UnitNorm
                    : (metrics.STypoLineGap == 0 ? metrics.SCapHeight : metrics.STypoAscender) * metrics.UnitNorm
                  );

                // Descent.
                fontDescriptor[PdfName.Descent] = PdfReal.Get(
                  metrics.STypoDescender == 0
                    ? metrics.Descender * metrics.UnitNorm
                    : metrics.STypoDescender * metrics.UnitNorm
                  );

                // CapHeight.
                fontDescriptor[PdfName.CapHeight] = PdfReal.Get(metrics.SCapHeight * metrics.UnitNorm);

                // StemV.
                /*
                  NOTE: '100' is just a rule-of-thumb value, 'cause I've still to solve the
                  'cvt' table puzzle (such a harsh headache!) for TrueType fonts...
                  TODO:IMPL TrueType and CFF stemv real value to extract!!!
                */
                fontDescriptor[PdfName.StemV] = PdfInteger.Get(100);

                // FontFile.
                fontDescriptor[PdfName.FontFile2] = File.Register(new PdfStream(new bytes::Buffer(parser.FontData.ToByteArray())));
            }
            return File.Register(fontDescriptor);
        }



        #endregion
        #endregion
        #endregion
    }
}