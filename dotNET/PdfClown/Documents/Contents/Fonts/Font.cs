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


namespace PdfClown.Documents.Contents.Fonts
{
    /**
      <summary>Abstract font [PDF:1.6:5.4].</summary>
    */
    [PDF(VersionEnum.PDF10)]
    public abstract class Font : PdfObjectWrapper<PdfDictionary>
    {
        #region types
        /**
          <summary>Font descriptor flags [PDF:1.6:5.7.1].</summary>
        */
        [Flags]
        public enum FlagsEnum
        {
            /**
              <summary>All glyphs have the same width.</summary>
            */
            FixedPitch = 0x1,
            /**
              <summary>Glyphs have serifs.</summary>
            */
            Serif = 0x2,
            /**
              <summary>Font contains glyphs outside the Adobe standard Latin character set.</summary>
            */
            Symbolic = 0x4,
            /**
              <summary>Glyphs resemble cursive handwriting.</summary>
            */
            Script = 0x8,
            /**
              <summary>Font uses the Adobe standard Latin character set.</summary>
            */
            Nonsymbolic = 0x20,
            /**
              <summary>Glyphs have dominant vertical strokes that are slanted.</summary>
            */
            Italic = 0x40,
            /**
              <summary>Font contains no lowercase letters.</summary>
            */
            AllCap = 0x10000,
            /**
              <summary>Font contains both uppercase and lowercase letters.</summary>
            */
            SmallCap = 0x20000,
            /**
              <summary>Thicken bold glyphs at small text sizes.</summary>
            */
            ForceBold = 0x40000
        }
        #endregion

        #region static
        #region fields
        public static Font LatestFont { get; private set; }
        private const int UndefinedDefaultCode = int.MinValue;
        private const int UndefinedWidth = int.MinValue;
        #endregion

        #region interface
        #region public
        /**
          <summary>Creates the representation of a font.</summary>
        */
        public static Font Get(Document context, string path)
        {
            return Get(
              context,
              new Bytes.Stream(
                  new io::FileStream(path, io::FileMode.Open, io::FileAccess.Read))
              );
        }

        /**
          <summary>Creates the representation of a font.</summary>
        */
        public static Font Get(Document context, IInputStream fontData)
        {
            if (OpenFontParser.IsOpenFont(fontData))
                return CompositeFont.Get(context, fontData);
            else
                throw new NotImplementedException();
        }

        internal SKTextEncoding GetEnoding()
        {
            return CharCodeMaxLength == 2 ? SKTextEncoding.Utf16 : SKTextEncoding.Utf8;// Symbolic ? SKTextEncoding.GlyphId : SKTextEncoding.Utf8; 
        }

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
                    return new StandardType1Font(reference);
                else // Custom Type 1.
                {
                    PdfDictionary fontDescriptor = (PdfDictionary)fontDictionary.Resolve(PdfName.FontDescriptor);
                    if (fontDescriptor.ContainsKey(PdfName.FontFile3)
                      && ((PdfName)((PdfStream)fontDescriptor.Resolve(PdfName.FontFile3)).Header.Resolve(PdfName.Subtype)).Equals(PdfName.OpenType)) // OpenFont/CFF.
                        throw new NotImplementedException();
                    else // Non-OpenFont Type 1.
                        return new Type1Font(reference);
                }
            }
            else if (fontType.Equals(PdfName.TrueType)) // TrueType.
                return new TrueTypeFont(reference);
            else if (fontType.Equals(PdfName.Type0)) // OpenFont.
            {
                PdfDictionary cidFontDictionary = (PdfDictionary)((PdfArray)fontDictionary.Resolve(PdfName.DescendantFonts)).Resolve(0);
                PdfName cidFontType = (PdfName)cidFontDictionary[PdfName.Subtype];
                if (cidFontType.Equals(PdfName.CIDFontType0)) // OpenFont/CFF.
                    return new Type0Font(reference);
                else if (cidFontType.Equals(PdfName.CIDFontType2)) // OpenFont/TrueType.
                    return new Type2Font(reference);
                else
                    throw new NotImplementedException("Type 0 subtype " + cidFontType + " not supported yet.");
            }
            else if (fontType.Equals(PdfName.Type3)) // Type 3.
                return new Type3Font(reference);
            else if (fontType.Equals(PdfName.MMType1)) // MMType1.
                return new MMType1Font(reference);
            else // Unknown.
                throw new NotSupportedException("Unknown font type: " + fontType + " (reference: " + reference + ")");
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
        /**
          <summary>Unicodes by character code.</summary>
          <remarks>
            <para>When this map is populated, <code>symbolic</code> variable shall accordingly be set.</para>
          </remarks>
        */
        protected CMap cmap;
        /**
          <summary>Glyph indexes by unicode.</summary>
        */
        protected Dictionary<int, int> glyphIndexes;
        /**
          <summary>Glyph kernings by (left-right) glyph index pairs.</summary>
        */
        protected Dictionary<int, int> glyphKernings;
        /**
          <summary>Glyph widths by glyph index.</summary>
        */
        protected Dictionary<int, int> glyphWidths;
        /**
          <summary>Whether the font encoding is custom (that is non-Unicode).</summary>
        */
        protected bool symbolic = true;
        /**
          <summary>Used unicodes.</summary>
        */
        //protected HashSet<int> usedCodes;

        protected SKTypeface typeface;

        /**
          <summary>Average glyph width.</summary>
        */
        private int averageWidth = UndefinedWidth;
        /**
          <summary>Maximum character code byte size.</summary>
        */
        private int CharCodeMaxLength => cmap?.MaxCodeLength ?? 1;
        /**
          <summary>Default Unicode for missing characters.</summary>
        */
        private int defaultCode = UndefinedDefaultCode;
        /**
          <summary>Default glyph width.</summary>
        */
        private int defaultWidth = UndefinedWidth;
        #endregion

        #region constructors
        /**
          <summary>Creates a new font structure within the given document context.</summary>
        */
        protected Font(Document context)
            : base(context, new PdfDictionary(new PdfName[1] { PdfName.Type }, new PdfDirectObject[1] { PdfName.Font }))
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
        /**
          <summary>Gets the unscaled vertical offset from the baseline to the ascender line (ascent).
          The value is a positive number.</summary>
        */
        public virtual double Ascent
        {
            get
            {
                IPdfNumber ascentObject = (IPdfNumber)GetDescriptorValue(PdfName.Ascent);
                return ascentObject != null ? ascentObject.DoubleValue : 750;
            }
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
            get
            {
                /*
                  NOTE: Sometimes font descriptors specify positive descent, therefore normalization is
                  required [FIX:27].
                */
                IPdfNumber descentObject = (IPdfNumber)GetDescriptorValue(PdfName.Descent);
                return -Math.Abs(descentObject != null ? descentObject.DoubleValue : 250);
            }
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

        /**
          <summary>Gets the PostScript name of the font.</summary>
        */
        public string Name
        {
            get => ((PdfName)BaseDataObject[PdfName.BaseFont]).ToString();
            set => BaseDataObject[PdfName.BaseFont] = new PdfName(value);
        }

        public int FirstChar
        {
            get => ((PdfInteger)BaseDataObject[PdfName.FirstChar])?.IntValue ?? 0;
            set => BaseDataObject[PdfName.FirstChar] = new PdfInteger(value);
        }

        public int LastChar
        {
            get => ((PdfInteger)BaseDataObject[PdfName.LastChar])?.IntValue ?? 0;
            set => BaseDataObject[PdfName.LastChar] = new PdfInteger(value);
        }

        public PdfArray Widths
        {
            get => (PdfArray)BaseDataObject.Resolve(PdfName.Widths);
            set => BaseDataObject[PdfName.Widths] = value.Reference;
        }

        public FontDescriptor FontDescriptor
        {
            get => Wrap<FontDescriptor>((PdfDictionary)BaseDataObject.Resolve(PdfName.FontDescriptor));
            set => BaseDataObject[PdfName.FontDescriptor] = value.BaseObject;
        }

        /**
          <summary>Gets whether the font encoding is custom (that is non-Unicode).</summary>
        */
        public bool Symbolic => symbolic;

        public CMap CMap => cmap;

        public ushort GetGlyph(int key)
        {
            return glyphIndexes.TryGetValue(key, out var glyph) ? (ushort)glyph : (ushort)0;
        }

        public virtual void DrawChar(SKCanvas context, SKPaint fill, SKPaint stroke, char textChar, int code, byte[] codeBytes)
        {
            var typeface = GetTypeface();
            var nameTypeface = GetTypefaceByName();

            var text = this is Type1Font
                ? System.Text.Encoding.UTF8.GetBytes(new[] { textChar })
                //: font is Type1Font && typeface != null
                //? BitConverter.GetBytes(font.GetGlyph(textChar))
                : this is Type2Font
                ? System.Text.Encoding.UTF8.GetBytes(new[] { textChar })
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
            //From PdfBox
            // if the font dictionary containsName a ToUnicode CMap, use that CMap
            if (cmap != null)
            {
                if ((cmap.CMapName?.StartsWith("Identity-", StringComparison.Ordinal) ?? false)
                    && (Dictionary.Resolve(PdfName.ToUnicode) is PdfName
                    || !cmap.HasUnicodeMappings))
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
                    return cmap.ToUnicode(code);
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
                        var code = cmap.ReadCode(buffer, out var codeBytes);
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

                    var code = cmap.ToCode(textCode);
                    if (code == null) // Missing glyph.
                    {
                        switch (Document.Configuration.EncodingFallback)
                        {
                            case EncodingFallbackEnum.Exclusion:
                                continue;
                            case EncodingFallbackEnum.Substitution:
                                code = cmap.ToCode(defaultCode);
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

        public override bool Equals(object obj)
        {
            return obj != null
              && obj.GetType().Equals(GetType())
              && ((Font)obj).Name.Equals(Name, StringComparison.Ordinal);
        }

        /**
          <summary>Gets the font descriptor flags.</summary>
        */
        public virtual FlagsEnum Flags
        {
            get
            {
                PdfInteger flagsObject = (PdfInteger)GetDescriptorValue(PdfName.Flags);
                return flagsObject != null ? (FlagsEnum)Enum.ToObject(typeof(FlagsEnum), flagsObject.RawValue) : 0;
            }
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

        private double textHeight = -1; // TODO: temporary until glyph bounding boxes are implemented.
        private static Dictionary<string, SKTypeface> cache;

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

            var fontDescription = GetFontDescription();
            if (fontDescription != null)
            {
                if (fontDescription.Resolve(PdfName.FontFile) is PdfStream stream)
                {
                    return typeface = GetTypeface(fontDescription, stream);
                }
                if (fontDescription.Resolve(PdfName.FontFile2) is PdfStream stream2)
                {
                    return typeface = GetTypeface(fontDescription, stream2);
                }
                if (fontDescription.Resolve(PdfName.FontFile3) is PdfStream stream3)
                {
                    return typeface = GetTypeface(fontDescription, stream3);
                }
                if (fontDescription.Resolve(PdfName.FontName) is PdfName fontName)
                {
                    return typeface = ParseName(fontName.StringValue, fontDescription);
                }
            }
            else if (BaseDataObject.Resolve(PdfName.BaseFont) is PdfName baseFont)
            {
                return typeface = ParseName(baseFont.StringValue, Dictionary);
            }

            return null;
        }

        public PdfDictionary GetFontDescription()
        {
            if (BaseDataObject.Resolve(PdfName.FontDescriptor) is PdfDictionary fontDescription)
                return fontDescription;
            if (BaseDataObject.Resolve(PdfName.DescendantFonts) is PdfArray array
                && array.Resolve(0) is PdfDictionary descendant
                && descendant.Resolve(PdfName.FontDescriptor) is PdfDictionary descendantDescription)
                return descendantDescription;
            return null;
        }

        public virtual SKTypeface GetTypefaceByName()
        {
            var fontDescription = GetFontDescription();
            if (fontDescription != null)
            {
                return ParseName(fontDescription.Resolve(PdfName.FontName)?.ToString(), fontDescription);
            }
            else if (BaseDataObject.Resolve(PdfName.BaseFont) is PdfName baseFont)
            {
                return typeface = ParseName(baseFont.StringValue, Dictionary);
            }
            return null;
        }

        protected virtual SKTypeface ParseName(string name, PdfDictionary header)
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

        protected virtual SKFontStyle GetStyle(string name, PdfDictionary fontDescription)
        {
            var weight = name.IndexOf("Bold", StringComparison.OrdinalIgnoreCase) > -1 ? 700 : 400;
            var weightParam = (IPdfNumber)fontDescription?.Resolve(PdfName.FontWeight);
            if (weightParam != null)
            {
                weight = weightParam.IntValue;
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

        protected virtual SKTypeface GetTypeface(PdfDictionary fontDescription, PdfStream stream)
        {
            var name = fontDescription.Resolve(PdfName.FontName)?.ToString();

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
        protected int AverageWidth
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
        protected int DefaultWidth
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
          <summary>Gets the specified font descriptor entry value.</summary>
        */
        protected abstract PdfDataObject GetDescriptorValue(PdfName key);

        /**
          <summary>Loads font information from existing PDF font structure.</summary>
        */
        protected void Load()
        {
            if (BaseDataObject.ContainsKey(PdfName.ToUnicode)) // To-Unicode explicit mapping.
            {
                cmap = CMap.Get(BaseDataObject.Resolve(PdfName.ToUnicode));
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