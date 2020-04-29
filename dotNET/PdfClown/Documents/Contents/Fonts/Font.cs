/*
  Copyright 2006-2015 Stefano Chizzolini. http://www.pdfclown.org

  Contributors:
    * Stefano Chizzolini (original code developer, http://www.stefanochizzolini.it)
    * Manuel Guilbault (code contributor [FIX:27], manuel.guilbault at gmail.com)

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

using PdfClown.Bytes;
using PdfClown.Documents;
using PdfClown.Files;
using PdfClown.Objects;
using PdfClown.Tokens;
using PdfClown.Util;

using System;
using io = System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SkiaSharp;
using PdfClown.Documents.Contents.Fonts.CCF;
using PdfClown.Documents.Contents.Fonts.AFM;
using System.IO;
using System.Diagnostics;

namespace PdfClown.Documents.Contents.Fonts
{
    /**
      <summary>Abstract font [PDF:1.6:5.4].</summary>
    */
    [PDF(VersionEnum.PDF10)]
    public abstract class Font : PdfObjectWrapper<PdfDictionary>
    {
        #region types

        #endregion

        #region static
        #region fields
        public static Font LatestFont { get; private set; }
        protected static readonly SKMatrix DefaultFontMatrix = new SKMatrix(0.001f, 0, 0, 0, 0.001f, 0, 0, 0, 1);
        private const int UndefinedDefaultCode = int.MinValue;
        private const int UndefinedWidth = int.MinValue;
        #endregion

        #region interface
        #region public
        /**
          <summary>Gets the scaling factor to be applied to unscaled metrics to get actual
          measures.</summary>
        */
        public static double GetScalingFactor(double size)
        { return 0.001 * size; }

        /**
          <summary>Wraps a font reference into a font object.</summary>
          <param name="baseObject">Font base object.</param>
          <returns>Font object associated to the reference.</returns>
        */
        public static Font Wrap(PdfDirectObject baseObject)
        {
            if (baseObject == null)
                return null;
            if (baseObject.Wrapper is Font font)
                return font;
            if (baseObject is PdfReference pdfReference
                && pdfReference.DataObject?.Wrapper is Font referenceFont)
            {
                baseObject.Wrapper = referenceFont;
                return referenceFont;
            }
            // Has the font been already instantiated?
            /*
              NOTE: Font structures are reified as complex objects, both IO- and CPU-intensive to load.
              So, it's convenient to retrieve them from a common cache whenever possible.
            */
            if (baseObject.IndirectObject.File.Document.Cache.TryGetValue(baseObject, out var cache))
            { return (Font)cache; }


            PdfDictionary fontDictionary = (PdfDictionary)baseObject.Resolve();
            PdfName fontType = (PdfName)fontDictionary[PdfName.Subtype];
            if (fontType == null)
                throw new Exception("Font type undefined (reference: " + baseObject + ")");

            if (fontType.Equals(PdfName.Type1)) // Type 1.
            {
                var fd = fontDictionary.Resolve(PdfName.FontDescriptor);
                if (fd is PdfDictionary fdDictionary && fdDictionary.ContainsKey(PdfName.FontFile3))
                {
                    return new PdfType1CFont(baseObject);
                }
                else
                {
                    return new PdfType1Font(baseObject);
                }
            }
            else if (fontType.Equals(PdfName.MMType1)) // MMType1.
            {
                var fd = fontDictionary.Resolve(PdfName.FontDescriptor);
                if (fd is PdfDictionary fdDictionary && fdDictionary.ContainsKey(PdfName.FontFile3))
                {
                    return new PdfType1CFont(baseObject);
                }
                else
                {
                    return new PdfMMType1Font(baseObject);
                }
            }
            else if (fontType.Equals(PdfName.TrueType)) // TrueType.
            {
                return new PdfTrueTypeFont(baseObject);
            }
            else if (fontType.Equals(PdfName.Type0)) // CompositeFont.
            {
                return new PdfType0Font(baseObject);
            }
            else if (fontType.Equals(PdfName.Type3)) // Type 3.
            {
                return new PdfType3Font(baseObject);
            }
            else // Unknown.
            {
                Debug.WriteLine($"warn: Invalid font subtype '{fontType}'");
                return new PdfType1Font(baseObject);
            }
        }
        #endregion
        #endregion
        #endregion

        #region dynamic
        #region fields
        /*
          NOTE: In order to avoid nomenclature ambiguities, these terms are used consistently within the
          code:
          * character code: internal codepoint corresponding to a character expressed inside a string
            object of a content stream;
          * unicode: external codepoint corresponding to a character expressed according to the Unicode
            standard encoding;
          * glyph index: internal identifier of the graphical representation of a character.
        */
        protected CMap toUnicodeCMap;
        protected FontMetrics afmStandard14;
        protected FontDescriptor fontDescriptor;
        protected readonly Dictionary<int, float> codeToWidthMap;
        protected readonly Dictionary<int, SKPath> cacheGlyphs = new Dictionary<int, SKPath>();
        protected SKTypeface typeface;
        protected List<float> widths;
        protected float avgFontWidth;
        protected float fontWidthOfSpace = -1f;
        /**
          <summary>Maximum character code byte size.</summary>
        */
        private int CharCodeMaxLength => toUnicodeCMap?.MaxCodeLength ?? 1;
        /**
          <summary>Default glyph width.</summary>
        */
        private double textHeight = -1; // TODO: temporary until glyph bounding boxes are implemented.
        private static Dictionary<string, SKTypeface> cache;
        #endregion

        #region constructors
        /**
         * Constructor for Standard 14.
         */
        public Font(Document context, string baseFont) : this(context)
        {
            toUnicodeCMap = null;
            afmStandard14 = Standard14Fonts.GetAFM(baseFont);
            if (afmStandard14 == null)
            {
                throw new ArgumentException("No AFM for font " + baseFont);
            }
            fontDescriptor = PdfType1FontEmbedder.BuildFontDescriptor(afmStandard14);
            // standard 14 fonts may be accessed concurrently, as they are singletons
            codeToWidthMap = new Dictionary<int, float>();
        }
        /**
          <summary>Creates a new font structure within the given document context.</summary>
        */
        protected Font(Document context)
            : this(context, new PdfDictionary(new PdfName[1] { PdfName.Type }, new PdfDirectObject[1] { PdfName.Font }))
        { Initialize(); }

        protected Font(Document context, PdfDictionary dictionary)
            : base(context, dictionary)
        { Initialize(); }

        /**
          <summary>Loads an existing font structure.</summary>
        */
        public Font(PdfDirectObject baseObject) : base(baseObject)
        {
            Initialize();
            afmStandard14 = Standard14Fonts.GetAFM(Name); // may be null (it usually is)
            fontDescriptor = LoadFontDescriptor();
            toUnicodeCMap = LoadUnicodeCmap();
            LatestFont = this;
            codeToWidthMap = new Dictionary<int, float>();
        }
        #endregion

        #region interface
        #region public

        public virtual SKMatrix FontMatrix { get => DefaultFontMatrix; }
        public abstract SKRect BoundingBox { get; }

        /**
          <summary>Gets the unscaled vertical offset from the baseline to the ascender line (ascent).
          The value is a positive number.</summary>
        */
        public virtual float Ascent
        {
            get => FontDescriptor?.Ascent ?? 750;
        }

        /**
          <summary>Gets the unscaled vertical offset from the baseline to the descender line (descent).
          The value is a negative number.</summary>
        */
        public virtual double Descent
        {
            /*
                  NOTE: Sometimes font descriptors specify positive descent, therefore normalization is
                  required [FIX:27].
                */
            get => -Math.Abs(FontDescriptor?.Descent ?? 250);
        }

        /**
        <summary>Gets the unscaled line height.</summary>
      */
        public double LineHeight => Ascent - Descent;

        public string Type
        {
            get => ((PdfName)BaseDataObject[PdfName.Type]).ToString();
            set => BaseDataObject[PdfName.Type] = new PdfName(value);
        }

        public string Subtype
        {
            get => ((PdfName)BaseDataObject[PdfName.Subtype]).ToString();
            set => BaseDataObject[PdfName.Subtype] = new PdfName(value);
        }

        public string BaseFont
        {
            get => ((PdfName)Dictionary[PdfName.BaseFont]).StringValue;
            set => Dictionary[PdfName.BaseFont] = new PdfName(value);
        }

        /**
         * This will get the fonts bounding box from its dictionary.
         *
         * @return The fonts bounding box.
         */
        public Rectangle FontBBox
        {
            get => Wrap<Rectangle>(Dictionary.Resolve<PdfArray>(PdfName.FontBBox));
            set => Dictionary[PdfName.FontBBox] = value?.BaseObject;
        }

        /**
          <summary>Gets the PostScript name of the font.</summary>
        */
        public virtual string Name
        {
            get => BaseFont;
            set => BaseFont = value;
        }

        public int? FirstChar
        {
            get => ((PdfInteger)BaseDataObject[PdfName.FirstChar])?.IntValue;
            set => BaseDataObject[PdfName.FirstChar] = PdfInteger.Get(value);
        }

        public int? LastChar
        {
            get => ((PdfInteger)BaseDataObject[PdfName.LastChar])?.IntValue;
            set => BaseDataObject[PdfName.LastChar] = PdfInteger.Get(value);
        }

        public PdfDataObject EncodingData
        {
            get => BaseDataObject.Resolve(PdfName.Encoding);
            set => BaseDataObject[PdfName.Encoding] = value.Reference;
        }

        public virtual PdfArray Widths
        {
            get => (PdfArray)BaseDataObject.Resolve(PdfName.Widths);
            set => BaseDataObject[PdfName.Widths] = value;
        }

        public virtual FontDescriptor FontDescriptor
        {
            get => fontDescriptor ?? (fontDescriptor = Wrap<FontDescriptor>((PdfDictionary)BaseDataObject.Resolve(PdfName.FontDescriptor)));
            set => BaseDataObject[PdfName.FontDescriptor] = (fontDescriptor = value)?.BaseObject;
        }

        /**
         <summary>Gets the font descriptor flags.</summary>
       */
        public virtual FlagsEnum Flags
        {
            get
            {
                var flagsObject = FontDescriptor?.Flags;
                return flagsObject != null ? (FlagsEnum)Enum.ToObject(typeof(FlagsEnum), flagsObject) : 0;
            }
        }

        public virtual bool IsVertical { get => false; set { } }
        public virtual bool IsDamaged { get => false; }
        public virtual bool IsEmbedded { get => false; }

        public virtual bool IsStandard14
        {
            get
            {
                if (IsEmbedded)
                {
                    return false;
                }

                // if the name matches, this is a Standard 14 font
                return Standard14Fonts.ContainsName(Name);
            }
        }
        /**
         * Returns the AFM if this is a Standard 14 font.
         */
        protected FontMetrics Standard14AFM
        {
            get => afmStandard14;
        }

        public abstract float GetHeight(int code);
        public abstract bool HasExplicitWidth(int code);
        public virtual float AverageFontWidth
        {
            get
            {
                float average;
                if (avgFontWidth.CompareTo(0.0f) != 0)
                {
                    average = avgFontWidth;
                }
                else
                {
                    float totalWidth = 0.0f;
                    float characterCount = 0.0f;
                    var widths = Widths;
                    if (widths != null)
                    {
                        for (int i = 0; i < widths.Count; i++)
                        {
                            var fontWidth = (IPdfNumber)widths[i];
                            if (fontWidth.FloatValue > 0)
                            {
                                totalWidth += fontWidth.FloatValue;
                                characterCount += 1;
                            }
                        }
                    }

                    if (totalWidth > 0)
                    {
                        average = totalWidth / characterCount;
                    }
                    else
                    {
                        average = 0;
                    }
                    avgFontWidth = average;
                }
                return average;
            }
        }

        /**
    * Determines the width of the space character.
    * 
    * @return the width of the space character
    */
        public float SpaceWidth
        {
            get
            {
                if (fontWidthOfSpace.CompareTo(-1f) == 0)
                {
                    var toUnicode = Dictionary.Resolve(PdfName.ToUnicode);
                    try
                    {
                        if (toUnicode != null && toUnicodeCMap != null)
                        {
                            int spaceMapping = toUnicodeCMap.SpaceMapping;
                            if (spaceMapping > -1)
                            {
                                fontWidthOfSpace = GetWidth(spaceMapping);
                            }
                        }
                        else
                        {
                            fontWidthOfSpace = GetWidth(32);
                        }

                        // try to get it from the font itself
                        if (fontWidthOfSpace <= 0)
                        {
                            fontWidthOfSpace = GetWidthFromFont(32);
                        }
                        // use the average font width as fall back
                        if (fontWidthOfSpace <= 0)
                        {
                            fontWidthOfSpace = AverageFontWidth;
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine($"error: Can't determine the width of the space character, assuming 250 {e}");
                        fontWidthOfSpace = 250f;
                    }
                }
                return fontWidthOfSpace;
            }
        }

        /**
         * Returns true if this font will be subset when embedded.
         */
        public abstract bool WillBeSubset { get; }

        /**
         * Adds the given Unicode point to the subset.
         * 
         * @param codePoint Unicode code point
         */

        public abstract void AddToSubset(int codePoint);

        /**
         * Replaces this font with a subset containing only the given Unicode characters.
         *
         * @throws IOException if the subset could not be written
         */
        public abstract void Subset();

        public virtual SKPoint GetPositionVector(int code)
        {
            throw new NotSupportedException("Horizontal fonts have no position vector");
        }

        protected abstract float GetStandard14Width(int code);

        public abstract SKPath GetPath(int code);

        public abstract SKPath GetNormalizedPath(int code);

        public abstract bool HasGlyph(int code);

        public abstract float GetWidthFromFont(int code);

        public abstract int ReadCode(Bytes.IInputStream input, out byte[] bytes);

        /**
          <summary>Gets whether the font encoding is custom (that is non-Unicode).</summary>
        */
        public virtual bool Symbolic { get => false; }

        public CMap CMap => toUnicodeCMap;

        /**
      * Returns the displacement vector (w0, w1) in text space, for the given character.
      * For horizontal text only the x component is used, for vertical text only the y component.
      *
      * @param code character code
      * @return displacement vector
      * @throws IOException
      */
        public virtual SKPoint GetDisplacement(int code)
        {
            return new SKPoint(GetWidth(code) / 1000, 0);
        }

        public virtual void DrawChar(SKCanvas context, SKPaint fill, SKPaint stroke, char textChar, int code, byte[] codeBytes, ref SKMatrix parameters)
        {
            var path = GetNormalizedPath(code);
            if (path == null)
            {
                Debug.WriteLine($"info: no path for Code: {code}  Char: '{textChar}'");
                return;
            }
            context.Save();
            var m = FontMatrix;

            if (!IsEmbedded && !IsVertical && !IsStandard14 && HasExplicitWidth(code))
            {
                var w = GetDisplacement(code);
                float fontWidth = GetWidthFromFont(code);
                if (fontWidth > 0 && // ignore spaces
                        Math.Abs(fontWidth - w.X * 1000) > 0.0001)
                {
                    float pdfWidth = w.X * 1000;
                    SKMatrix.PostConcat(ref m, SKMatrix.MakeScale(pdfWidth / fontWidth, 1));
                }
            }

            SKMatrix.PreConcat(ref m, parameters);
            context.Concat(ref m);

            if (fill != null)
            {
                context.DrawPath(path, fill);
            }

            if (stroke != null)
            {
                context.DrawPath(path, stroke);
            }
            context.Restore();
        }

        public virtual int ToUnicode(int code, GlyphMapping customGlyphList)
        {
            return ToUnicode(code);
        }

        /**
        * Returns the Unicode character sequence which corresponds to the given character code.
        *
        * @param code character code
        * @return Unicode character(s)
        * @throws IOException
        */
        public virtual int ToUnicode(int code)
        {
            // if the font dictionary containsName a ToUnicode CMap, use that CMap
            if (toUnicodeCMap != null)
            {
                if ((toUnicodeCMap.CMapName?.StartsWith("Identity-", StringComparison.Ordinal) ?? false)
                    && (Dictionary.Resolve(PdfName.ToUnicode) is PdfName || !toUnicodeCMap.HasUnicodeMappings))
                {
                    // handle the undocumented case of using Identity-H/V as a ToUnicode CMap, this
                    // isn't actually valid as the Identity-x CMaps are code->CID maps, not
                    // code->Unicode maps. See sample_fonts_solidconvertor.pdf for an example.
                    // PDFBOX-3123: do this only if the /ToUnicode entry is a name
                    // PDFBOX-4322: identity streams are OK too
                    return code;
                }
                else
                {
                    // proceed as normal
                    return toUnicodeCMap.ToUnicode(code);
                }
            }

            // if no value has been produced, there is no way to obtain Unicode for the character.
            // this behaviour can be overridden is subclasses, but this method *must* return null here
            return -1;
        }

        ///**
        //  <summary>Gets the text from the given internal representation.</summary>
        //  <param name="bytes">Internal representation to decode.</param>
        //  <exception cref="DecodeException"/>
        //*/
        //public string Decode(byte[] bytes)
        //{
        //    var textBuilder = new StringBuilder();
        //    {
        //        using (var buffer = new Bytes.Buffer(bytes))
        //        {
        //            while (buffer.Position < buffer.Length)
        //            {
        //                var code = toUnicodeCMap.ReadCode(buffer, out var codeBytes);
        //                var textChar = ToUnicode(code);
        //                if (textChar < 0)
        //                {
        //                    textChar = MissingCharacter(codeBytes, code);
        //                }
        //                if (textChar > -1)
        //                {
        //                    textBuilder.Append((char)textChar);
        //                }
        //            }
        //        }
        //    }
        //    return textBuilder.ToString();
        //}


        /**
          <summary>Gets the internal representation of the given text.</summary>
          <param name="text">Text to encode.</param>
          <exception cref="EncodeException"/>
        */
        public byte[] Encode(string text)
        {
            using (var output = new MemoryStream())
            {
                int offset = 0;
                while (offset < text.Length)
                {
                    int codePoint = text[offset];

                    // multi-byte encoding with 1 to 4 bytes
                    byte[] bytes = Encode(codePoint);
                    output.Write(bytes, 0, bytes.Length);

                    offset += 1;//Character.charCount(codePoint);
                }
                return output.ToArray();
            }
        }

        public abstract byte[] Encode(int unicode);

        public override bool Equals(object obj)
        {
            return obj != null
              && obj.GetType().Equals(GetType())
              && ((Font)obj).Name.Equals(Name, StringComparison.Ordinal);
        }

        /**
          <summary>Gets the vertical offset from the baseline to the ascender line (ascent),
          scaled to the given font size. The value is a positive number.</summary>
          <param name="size">Font size.</param>
        */
        public double GetAscent(double size)
        { return Ascent * GetScalingFactor(size); }

        /**
          <summary>Gets the vertical offset from the baseline to the descender line (descent),
          scaled to the given font size. The value is a negative number.</summary>
          <param name="size">Font size.</param>
        */
        public double GetDescent(double size)
        { return Descent * GetScalingFactor(size); }

        public override int GetHashCode()
        { return Name.GetHashCode(); }

        /**
          <summary>Gets the unscaled height of the given character.</summary>
          <param name="textChar">Character whose height has to be calculated.</param>
        */
        public double GetHeight(char textChar)
        {
            /*
              TODO: Calculate actual text height through glyph bounding box.
            */
            if (textHeight == -1)
            { textHeight = Ascent - Descent; }
            return textHeight;
        }

        /**
          <summary>Gets the height of the given character, scaled to the given font size.</summary>
          <param name="textChar">Character whose height has to be calculated.</param>
          <param name="size">Font size.</param>
        */
        public double GetHeight(char textChar, double size)
        { return GetHeight(textChar) * GetScalingFactor(size); }

        /**
          <summary>Gets the unscaled height of the given text.</summary>
          <param name="text">Text whose height has to be calculated.</param>
        */
        public double GetHeight(string text)
        {
            double height = 0;
            for (int index = 0, length = text.Length; index < length; index++)
            {
                double charHeight = GetHeight(text[index]);
                if (charHeight > height)
                { height = charHeight; }
            }
            return height;
        }

        /**
          <summary>Gets the height of the given text, scaled to the given font size.</summary>
          <param name="text">Text whose height has to be calculated.</param>
          <param name="size">Font size.</param>
        */
        public double GetHeight(string text, double size)
        { return GetHeight(text) * GetScalingFactor(size); }

        ///**
        //  <summary>Gets the width (kerning inclusive) of the given text, scaled to the given font size.</summary>
        //  <param name="text">Text whose width has to be calculated.</param>
        //  <param name="size">Font size.</param>
        //  <exception cref="EncodeException"/>
        //*/
        //public double GetKernedWidth(string text, double size)
        //{ return (GetWidth(text) + GetKerning(text)) * GetScalingFactor(size); }

        ///**
        //  <summary>Gets the unscaled kerning width between two given characters.</summary>
        //  <param name="textChar1">Left character.</param>
        //  <param name="textChar2">Right character,</param>
        //*/
        //public int GetKerning(char textChar1, char textChar2)
        //{
        //    if (glyphKernings == null)
        //        return 0;

        //    int textChar1Index;
        //    if (!glyphIndexes.TryGetValue((int)textChar1, out textChar1Index))
        //        return 0;

        //    int textChar2Index;
        //    if (!glyphIndexes.TryGetValue((int)textChar2, out textChar2Index))
        //        return 0;

        //    int kerning;
        //    return glyphKernings.TryGetValue(
        //      textChar1Index << 16 // Left-hand glyph index.
        //        + textChar2Index, // Right-hand glyph index.
        //      out kerning) ? kerning : 0;
        //}

        ///**
        //  <summary>Gets the unscaled kerning width inside the given text.</summary>
        //  <param name="text">Text whose kerning has to be calculated.</param>
        //*/
        //public int GetKerning(string text)
        //{
        //    int kerning = 0;
        //    for (int index = 0, length = text.Length - 1; index < length; index++)
        //    {
        //        kerning += GetKerning(text[index], text[index + 1]);
        //    }
        //    return kerning;
        //}

        ///**
        //  <summary>Gets the kerning width inside the given text, scaled to the given font size.</summary>
        //  <param name="text">Text whose kerning has to be calculated.</param>
        //  <param name="size">Font size.</param>
        //*/
        //public double GetKerning(string text, double size)
        //{ return GetKerning(text) * GetScalingFactor(size); }

        /**
          <summary>Gets the line height, scaled to the given font size.</summary>
          <param name="size">Font size.</param>
        */
        public double GetLineHeight(double size)
        { return LineHeight * GetScalingFactor(size); }

        public float GetWidth(char code, double size)
        {
            return GetWidth(code, (float)size);
        }

        public float GetWidth(char code, float size)
        {
            return GetWidth(code) * (float)GetScalingFactor(size);
        }

        public virtual float GetWidth(int code)
        {
            if (codeToWidthMap.TryGetValue(code, out float width))
            {
                return width;
            }

            // Acrobat overrides the widths in the font program on the conforming reader's system with
            // the widths specified in the font dictionary." (Adobe Supplement to the ISO 32000)
            //
            // Note: The Adobe Supplement says that the override happens "If the font program is not
            // embedded", however PDFBOX-427 shows that it also applies to embedded fonts.

            // Type1, Type1C, Type3
            if (Widths != null)
            {
                var widths = Widths;
                int firstChar = FirstChar ?? 0;
                int lastChar = LastChar ?? 0;
                int size = widths.Count;
                int idx = code - firstChar;
                if (size > 0 && code >= firstChar && code <= lastChar && idx < size)
                {
                    width = ((IPdfNumber)widths[idx])?.FloatValue ?? 0f;
                    codeToWidthMap[code] = width;
                    return width;
                }
            }
            var fd = FontDescriptor;
            if (fd?.MissingWidth != null)
            {
                // get entry from /MissingWidth entry
                width = fd.MissingWidth ?? 0;
                codeToWidthMap[code] = width;
                return width;
            }

            // standard 14 font widths are specified by an AFM
            if (IsStandard14)
            {
                width = GetStandard14Width(code);
                codeToWidthMap[code] = width;
                return width;
            }

            // if there's nothing to override with, then obviously we fall back to the font
            width = GetWidthFromFont(code);
            codeToWidthMap[code] = width;
            return width;
        }

        ///**
        //  <summary>Gets the width of the given character, scaled to the given font size.</summary>
        //  <param name="textChar">Character whose height has to be calculated.</param>
        //  <param name="size">Font size.</param>
        //  <exception cref="EncodeException"/>
        //*/
        //public double GetWidth(char textChar, double size)
        //{ return GetWidth(textChar) * GetScalingFactor(size); }

        /**
          <summary>Gets the unscaled width (kerning exclusive) of the given text.</summary>
          <param name="text">Text whose width has to be calculated.</param>
          <exception cref="EncodeException"/>
        */
        public virtual float GetWidth(string text)
        {
            byte[] bytes = Encode(text);
            var input = new Bytes.Buffer(bytes);

            float width = 0;
            while (input.Available > 0)
            {
                int code = ReadCode(input, out _);
                width += GetWidth(code);
            }

            return width;
        }

        /**
          <summary>Gets the width (kerning exclusive) of the given text, scaled to the given font
          size.</summary>
          <param name="text">Text whose width has to be calculated.</param>
          <param name="size">Font size.</param>
          <exception cref="EncodeException"/>
        */
        public double GetWidth(string text, double size)
        { return GetWidth(text) * GetScalingFactor(size); }

        protected bool IsNonZeroBoundingBox(Rectangle bbox)
        {
            return bbox != null && (
                bbox.Left.CompareTo(0) != 0 ||
                bbox.Bottom.CompareTo(0) != 0 ||
                bbox.Right.CompareTo(0) != 0 ||
                bbox.Top.CompareTo(0) != 0
            );
        }

        public virtual SKTypeface GetTypeface()
        {
            if (typeface != null)
                return typeface;

            var fontDescriptor = FontDescriptor;
            if (fontDescriptor != null)
            {
                if (fontDescriptor.FontFile?.BaseDataObject is PdfStream stream)
                {
                    return typeface = GetTypeface(fontDescriptor, stream);
                }
                if (fontDescriptor.FontFile2?.BaseDataObject is PdfStream stream2)
                {
                    return typeface = GetTypeface(fontDescriptor, stream2);
                }
                if (fontDescriptor.FontFile3?.BaseDataObject is PdfStream stream3)
                {
                    return typeface = GetTypeface(fontDescriptor, stream3);
                }
                if (fontDescriptor.FontName is string fonName)
                {
                    return typeface = ParseName(fonName, fontDescriptor);
                }
            }
            else if (BaseFont is string baseFont)
            {
                return typeface = ParseName(baseFont, fontDescriptor);
            }

            return null;
        }

        public virtual SKTypeface GetTypefaceByName()
        {
            var fontDescription = FontDescriptor;
            if (fontDescription != null)
            {
                return ParseName(fontDescription.FontName, fontDescription);
            }
            else if (BaseDataObject.Resolve(PdfName.BaseFont) is PdfName baseFont)
            {
                return typeface = ParseName(baseFont.StringValue, null);
            }
            return null;
        }

        protected virtual SKTypeface ParseName(string name, FontDescriptor header)
        {
            if (cache == null)
            { cache = new Dictionary<string, SKTypeface>(StringComparer.Ordinal); }
            if (cache.TryGetValue(name, out var typeface))
            {
                return typeface;
            }

            var parameters = name.Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);

            var style = GetStyle(name, header);

            var fontName = parameters[0].Equals("Courier", StringComparison.OrdinalIgnoreCase)
                || parameters[0].StartsWith("CourierNew", StringComparison.OrdinalIgnoreCase)
                ? "Courier New"
                : parameters[0].Equals("Times", StringComparison.OrdinalIgnoreCase)
                || parameters[0].StartsWith("TimesNewRoman", StringComparison.OrdinalIgnoreCase)
                ? "Times New Roman"
                : parameters[0].Equals("Helvetica", StringComparison.OrdinalIgnoreCase)
                ? "Helvetica"
                : parameters[0].Equals("ZapfDingbats", StringComparison.OrdinalIgnoreCase)
                ? "Wingdings"
                : parameters[0];

            //SKFontManager.Default.FontFamilies
            if (fontName.IndexOf("Arial", StringComparison.Ordinal) > -1)
            {
                fontName = "Arial";
            }
            return cache[name] = SKTypeface.FromFamilyName(fontName, style);
        }

        protected virtual SKFontStyle GetStyle(string name, FontDescriptor fontDescription)
        {
            var weight = name.IndexOf("Bold", StringComparison.OrdinalIgnoreCase) > -1 ? 700 : 400;
            var weightParam = fontDescription?.FontWeight;
            if (weightParam != null)
            {
                weight = (int)weightParam;
            }
            return new SKFontStyle(
                weight,
                (int)SKFontStyleWidth.Normal,
                name.IndexOf("Italic", StringComparison.OrdinalIgnoreCase) > -1
                    ? SKFontStyleSlant.Italic
                    : name.IndexOf("Oblique", StringComparison.OrdinalIgnoreCase) > -1
                        ? SKFontStyleSlant.Oblique
                        : SKFontStyleSlant.Upright);
        }

        protected virtual SKTypeface GetTypeface(FontDescriptor fontDescription, PdfStream stream)
        {
            var name = fontDescription.BaseDataObject.Resolve(PdfName.FontName)?.ToString();

            var body = stream.GetBody(true).ToByteArray();
            //System.IO.File.WriteAllBytes($"export{name}.ttf", body);

            var data = new SKMemoryStream(body);

            var typeface = SKFontManager.Default.CreateTypeface(data);
            // var typeface = SKTypeface.FromStream(data);
            if (typeface == null)
            {
                typeface = ParseName(name, fontDescription);
            }
            return typeface;
        }


        #endregion

        #region protected

        private FontDescriptor LoadFontDescriptor()
        {
            var fd = FontDescriptor;
            if (fd != null)
            {
                return fd;
            }
            else if (afmStandard14 != null)
            {
                // build font descriptor from the AFM
                return PdfType1FontEmbedder.BuildFontDescriptor(afmStandard14);
            }
            else
            {
                return null;
            }
        }

        private CMap LoadUnicodeCmap()
        {
            var toUnicode = Dictionary.Resolve(PdfName.ToUnicode);
            if (toUnicode == null)
            {
                return null;
            }
            CMap cmap = null;
            try
            {
                cmap = CMap.Get(toUnicode);
                if (cmap != null && !cmap.HasUnicodeMappings)
                {
                    Debug.WriteLine($"warn: Invalid ToUnicode CMap in font {Name}");
                    string cmapName = cmap.CMapName ?? "";
                    string ordering = cmap.Ordering ?? "";
                    var encoding = Dictionary.Resolve(PdfName.Encoding);
                    if (cmapName.IndexOf("Identity", StringComparison.Ordinal) > -1 //
                            || cmapName.IndexOf("Identity", StringComparison.Ordinal) > -1 //
                            || PdfName.IdentityH.Equals(encoding) //
                            || PdfName.IdentityV.Equals(encoding))
                    {
                        // assume that if encoding is identity, then the reverse is also true
                        cmap = CMap.Get(PdfName.IdentityH);
                    }
                }
            }
            catch (IOException ex)
            {
                Debug.WriteLine($"error: Could not read ToUnicode CMap in font {Name} {ex}");
            }
            return cmap;
        }
        #endregion

        #region private
        private void Initialize()
        {
            //usedCodes = new HashSet<int>();

            // Put the newly-instantiated font into the common cache!
            /*
              NOTE: Font structures are reified as complex objects, both IO- and CPU-intensive to load.
              So, it's convenient to put them into a common cache for later reuse.
            */
            if (Document != null)
                Document.Cache[BaseObject] = this;
        }
        #endregion
        #endregion
        #endregion
    }
}