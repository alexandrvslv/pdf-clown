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
            PdfReference reference = (PdfReference)baseObject;
            {
                // Has the font been already instantiated?
                /*
                  NOTE: Font structures are reified as complex objects, both IO- and CPU-intensive to load.
                  So, it's convenient to retrieve them from a common cache whenever possible.
                */
                Dictionary<PdfReference, object> cache = reference.IndirectObject.File.Document.Cache;
                if (cache.ContainsKey(reference))
                { return (Font)cache[reference]; }
            }

            PdfDictionary fontDictionary = (PdfDictionary)reference.DataObject;
            PdfName fontType = (PdfName)fontDictionary[PdfName.Subtype];
            if (fontType == null)
                throw new Exception("Font type undefined (reference: " + reference + ")");

            if (fontType.Equals(PdfName.Type1)) // Type 1.
            {
                if (!fontDictionary.ContainsKey(PdfName.FontDescriptor)) // Standard Type 1.
                {
                    return new StandardType1Font(reference);
                }
                else // Custom Type 1.
                {
                    return new PdfType1Font(reference);
                }
            }
            else if (fontType.Equals(PdfName.TrueType)) // TrueType.
            {
                return new PdfTrueTypeFont(reference);
            }
            else if (fontType.Equals(PdfName.Type0)) // OpenFont.
            {
                return new PdfType0Font(reference);
            }
            else if (fontType.Equals(PdfName.Type3)) // Type 3.
            {
                return new PdfType3Font(reference);
            }
            else if (fontType.Equals(PdfName.MMType1)) // MMType1.
            {
                return new MMType1Font(reference);
            }
            else // Unknown.
            {
                throw new NotSupportedException("Unknown font type: " + fontType + " (reference: " + reference + ")");
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
        protected SKTypeface typeface;
        protected List<float> widths;
        protected float avgFontWidth;
        protected float fontWidthOfSpace = -1f;
        /**
          <summary>Average glyph width.</summary>
        */
        private int averageWidth = UndefinedWidth;
        /**
          <summary>Maximum character code byte size.</summary>
        */
        private int CharCodeMaxLength => toUnicodeCMap?.MaxCodeLength ?? 1;
        /**
          <summary>Default Unicode for missing characters.</summary>
        */
        private int defaultCode = UndefinedDefaultCode;
        /**
          <summary>Default glyph width.</summary>
        */
        private int defaultWidth = UndefinedWidth;
        private double textHeight = -1; // TODO: temporary until glyph bounding boxes are implemented.
        private static Dictionary<string, SKTypeface> cache;
        #endregion

        #region constructors
        /**
         * Constructor for Standard 14.
         */
        public Font(string baseFont) : this((Document)null)
        {
            toUnicodeCMap = null;
            afmStandard14 = Standard14Fonts.GetAFM(baseFont);
            if (afmStandard14 == null)
            {
                throw new ArgumentException("No AFM for font " + baseFont);
            }
            FontDescriptor = PdfType1FontEmbedder.BuildFontDescriptor(afmStandard14);
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
            Load();
            LatestFont = this;
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
         <summary>Gets/Sets the Unicode codepoint used to substitute missing characters.</summary>
         <exception cref="EncodeException">If the value is not mapped in the font's encoding.</exception>
       */
        public int DefaultCode
        {
            get => defaultCode;
            set
            {
                if (!glyphIndexes.ContainsKey(value))
                    throw new EncodeException((char)value);

                defaultCode = value;
            }
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
          <summary>Gets the Unicode code-points supported by this font.</summary>
        */
        public ICollection<int> CodePoints => glyphIndexes.Keys;

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

        public virtual PdfArray Widths
        {
            get => (PdfArray)BaseDataObject.Resolve(PdfName.Widths);
            set => BaseDataObject[PdfName.Widths] = value.Reference;
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
        public abstract SKPoint GetPositionVector(int code);
        public abstract SKPoint GetDefaultPositionVector(int cid);
        public abstract float GetVerticalDisplacementVectorY(int code);
        public abstract SKPath GetPath(int code);
        public abstract SKPath GetNormalizedPath(int code);
        public abstract bool HasGlyph(int code);
        public abstract float GetWidthFromFont(int code);
        public abstract int ReadCode(Bytes.Buffer input, out byte[] bytes);
        /**
          <summary>Gets whether the font encoding is custom (that is non-Unicode).</summary>
        */
        public abstract bool Symbolic { get; }

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

        public virtual void DrawChar(SKCanvas context, SKPaint fill, SKPaint stroke, char textChar, int code, byte[] codeBytes)
        {
            var typeface = GetTypeface();
            var nameTypeface = GetTypefaceByName();

            var text = this is PdfType1Font
                ? System.Text.Encoding.UTF8.GetBytes(new[] { textChar })
                //: font is Type1Font && typeface != null
                //? BitConverter.GetBytes(font.GetGlyph(textChar))                
                : Encode(textChar.ToString());

            if (fill != null)
            {
                fill.Typeface = typeface;
                if (fill.ContainsGlyphs(text))
                {
                    context.DrawText(text, 0, 0, fill);
                }
                else if (typeface != nameTypeface)
                {
                    fill.Typeface = nameTypeface;
                    context.DrawText(text, 0, 0, fill);
                }
                else
                { }
            }

            if (stroke != null)
            {
                stroke.Typeface = typeface;
                if (stroke.ContainsGlyphs(text))
                {
                    context.DrawText(text, 0, 0, stroke);
                }
                else if (typeface != nameTypeface)
                {
                    stroke.Typeface = nameTypeface;
                    context.DrawText(text, 0, 0, stroke);
                }
                else
                { }
            }
        }

        public int MissingCharacter(byte[] byteElement, int code)
        {
            int textCode;
            switch (Document.Configuration.EncodingFallback)
            {
                case EncodingFallbackEnum.Exclusion:
                    textCode = -1;
                    break;
                case EncodingFallbackEnum.Substitution:
                    textCode = DefaultCode;
                    break;
                case EncodingFallbackEnum.Exception:
                    throw new DecodeException(byteElement, code);
                default:
                    throw new NotImplementedException();
            }

            return textCode;
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

        /**
          <summary>Gets the text from the given internal representation.</summary>
          <param name="bytes">Internal representation to decode.</param>
          <exception cref="DecodeException"/>
        */
        public string Decode(byte[] bytes)
        {
            var textBuilder = new StringBuilder();
            {
                using (var buffer = new Bytes.Buffer(bytes))
                {
                    while (buffer.Position < buffer.Length)
                    {
                        var code = toUnicodeCMap.ReadCode(buffer, out var codeBytes);
                        var textChar = ToUnicode(code);
                        if (textChar < 0)
                        {
                            textChar = MissingCharacter(codeBytes, code);
                        }
                        if (textChar > -1)
                        {
                            textBuilder.Append((char)textChar);
                        }
                    }
                }
            }
            return textBuilder.ToString();
        }


        /**
          <summary>Gets the internal representation of the given text.</summary>
          <param name="text">Text to encode.</param>
          <exception cref="EncodeException"/>
        */
        public byte[] Encode(string text)
        {
            using (var encodedStream = new io::MemoryStream())
            {
                for (int index = 0, length = text.Length; index < length; index++)
                {
                    int textCode = text[index];
                    if (textCode < 32) // NOTE: Control characters are ignored [FIX:7].
                        continue;

                    var code = toUnicodeCMap.ToCode(textCode);
                    if (code == null) // Missing glyph.
                    {
                        switch (Document.Configuration.EncodingFallback)
                        {
                            case EncodingFallbackEnum.Exclusion:
                                continue;
                            case EncodingFallbackEnum.Substitution:
                                code = toUnicodeCMap.ToCode(defaultCode);
                                break;
                            case EncodingFallbackEnum.Exception:
                                throw new EncodeException(text, index);
                            default:
                                throw new NotImplementedException();
                        }
                    }

                    encodedStream.Write(code, 0, code.Length);
                    //usedCodes.Add(textCode);
                }
                return encodedStream.ToArray();
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

        /**
          <summary>Gets the width (kerning inclusive) of the given text, scaled to the given font size.</summary>
          <param name="text">Text whose width has to be calculated.</param>
          <param name="size">Font size.</param>
          <exception cref="EncodeException"/>
        */
        public double GetKernedWidth(string text, double size)
        { return (GetWidth(text) + GetKerning(text)) * GetScalingFactor(size); }

        /**
          <summary>Gets the unscaled kerning width between two given characters.</summary>
          <param name="textChar1">Left character.</param>
          <param name="textChar2">Right character,</param>
        */
        public int GetKerning(char textChar1, char textChar2)
        {
            if (glyphKernings == null)
                return 0;

            int textChar1Index;
            if (!glyphIndexes.TryGetValue((int)textChar1, out textChar1Index))
                return 0;

            int textChar2Index;
            if (!glyphIndexes.TryGetValue((int)textChar2, out textChar2Index))
                return 0;

            int kerning;
            return glyphKernings.TryGetValue(
              textChar1Index << 16 // Left-hand glyph index.
                + textChar2Index, // Right-hand glyph index.
              out kerning) ? kerning : 0;
        }

        /**
          <summary>Gets the unscaled kerning width inside the given text.</summary>
          <param name="text">Text whose kerning has to be calculated.</param>
        */
        public int GetKerning(string text)
        {
            int kerning = 0;
            for (int index = 0, length = text.Length - 1; index < length; index++)
            {
                kerning += GetKerning(text[index], text[index + 1]);
            }
            return kerning;
        }

        /**
          <summary>Gets the kerning width inside the given text, scaled to the given font size.</summary>
          <param name="text">Text whose kerning has to be calculated.</param>
          <param name="size">Font size.</param>
        */
        public double GetKerning(string text, double size)
        { return GetKerning(text) * GetScalingFactor(size); }

        /**
          <summary>Gets the line height, scaled to the given font size.</summary>
          <param name="size">Font size.</param>
        */
        public double GetLineHeight(double size)
        { return LineHeight * GetScalingFactor(size); }

        public virtual float GetWidth(int code)
        {
            float width = codeToWidthMap.TryGet(code);
            if (width != null)
            {
                return width;
            }

            // Acrobat overrides the widths in the font program on the conforming reader's system with
            // the widths specified in the font dictionary." (Adobe Supplement to the ISO 32000)
            //
            // Note: The Adobe Supplement says that the override happens "If the font program is not
            // embedded", however PDFBOX-427 shows that it also applies to embedded fonts.

            // Type1, Type1C, Type3
            if (Widths != null || Dictionary.containsKey(PdfName.MISSING_WIDTH))
            {
                int firstChar = FirstChar;
                int lastChar = LastChar;
                int siz = getWidths().size();
                int idx = code - firstChar;
                if (siz > 0 && code >= firstChar && code <= lastChar && idx < siz)
                {
                    width = getWidths().get(idx);
                    if (width == null)
                    {
                        width = 0f;
                    }
                    codeToWidthMap.put(code, width);
                    return width;
                }

                var fd = FontDescriptor;
                if (fd != null)
                {
                    // get entry from /MissingWidth entry
                    width = fd.getMissingWidth();
                    codeToWidthMap.put(code, width);
                    return width;
                }
            }

            // standard 14 font widths are specified by an AFM
            if (isStandard14())
            {
                width = getStandard14Width(code);
                codeToWidthMap.put(code, width);
                return width;
            }

            // if there's nothing to override with, then obviously we fall back to the font
            width = GetWidthFromFont(code);
            codeToWidthMap.put(code, width);
            return width;
        }

        /**
          <summary>Gets the unscaled width of the given character.</summary>
          <param name="textChar">Character whose width has to be calculated.</param>
          <exception cref="EncodeException"/>
        */
        public int GetWidth(char textChar)
        {
            int glyphIndex;
            if (!glyphIndexes.TryGetValue((int)textChar, out glyphIndex))
            {
                switch (Document.Configuration.EncodingFallback)
                {
                    case EncodingFallbackEnum.Exclusion:
                        return 0;
                    case EncodingFallbackEnum.Substitution:
                        return DefaultWidth;
                    case EncodingFallbackEnum.Exception:
                        throw new EncodeException(textChar);
                    default:
                        throw new NotImplementedException();
                }
            }

            int glyphWidth;
            return glyphWidths.TryGetValue(glyphIndex, out glyphWidth) ? glyphWidth : DefaultWidth;
        }

        /**
          <summary>Gets the width of the given character, scaled to the given font size.</summary>
          <param name="textChar">Character whose height has to be calculated.</param>
          <param name="size">Font size.</param>
          <exception cref="EncodeException"/>
        */
        public double GetWidth(char textChar, double size)
        { return GetWidth(textChar) * GetScalingFactor(size); }

        /**
          <summary>Gets the unscaled width (kerning exclusive) of the given text.</summary>
          <param name="text">Text whose width has to be calculated.</param>
          <exception cref="EncodeException"/>
        */
        public int GetWidth(string text)
        {
            int width = 0;
            for (int index = 0, length = text.Length; index < length; index++)
            { width += GetWidth(text[index]); }
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

        /**
          <summary>Gets/Sets the average glyph width.</summary>
        */
        public virtual int AverageWidth
        {
            get
            {
                if (averageWidth == UndefinedWidth)
                {
                    if (glyphWidths.Count == 0)
                    { averageWidth = 1000; }
                    else
                    {
                        averageWidth = 0;
                        foreach (int glyphWidth in glyphWidths.Values)
                        { averageWidth += glyphWidth; }
                        averageWidth /= glyphWidths.Count;
                    }
                }
                return averageWidth;
            }
            set => averageWidth = value;
        }

        /**
          <summary>Gets/Sets the default glyph width.</summary>
        */
        public virtual int DefaultWidth
        {
            get
            {
                if (defaultWidth == UndefinedWidth)
                { defaultWidth = AverageWidth; }
                return defaultWidth;
            }
            set => defaultWidth = value;
        }


        /**
          <summary>Loads font information from existing PDF font structure.</summary>
        */
        protected void Load()
        {
            if (BaseDataObject.ContainsKey(PdfName.ToUnicode)) // To-Unicode explicit mapping.
            {
                toUnicodeCMap = CMap.Get(BaseDataObject.Resolve(PdfName.ToUnicode));
                symbolic = false;
            }

            if (BaseDataObject.ContainsKey(PdfName.Encoding)) // Encoding explicit mapping.
            {
                //PdfStream toUnicodeStream = (PdfStream)BaseDataObject.Resolve(PdfName.Encoding);
                //CMapParser parser = new CMapParser(toUnicodeStream.Body);
                //codes = new BiDictionary<ByteArray, int>(parser.Parse());
                //symbolic = false;
            }

            OnLoad();

            // Missing character substitute.
            if (defaultCode == UndefinedDefaultCode)
            {
                ICollection<int> codePoints = CodePoints;
                if (codePoints.Contains((int)'?'))
                { DefaultCode = '?'; }
                else if (codePoints.Contains((int)' '))
                { DefaultCode = ' '; }
                else
                { DefaultCode = codePoints.First(); }
            }
        }

        /**
          <summary>Notifies font information loading from an existing PDF font structure.</summary>
        */
        protected abstract void OnLoad();
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
            Document.Cache[(PdfReference)BaseObject] = this;
        }
        #endregion
        #endregion
        #endregion
    }
}