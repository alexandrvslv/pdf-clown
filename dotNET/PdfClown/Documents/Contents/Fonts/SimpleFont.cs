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

using PdfClown.Objects;
using PdfClown.Util;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace PdfClown.Documents.Contents.Fonts
{
    /**
      <summary>Simple font [PDF:1.6:5.5].</summary>
    */
    [PDF(VersionEnum.PDF10)]
    public abstract class SimpleFont : Font
    {
        protected Encoding encoding;
        protected GlyphMapping glyphList;
        private bool? isSymbolic;

        #region constructors
        protected SimpleFont(Document context) : base(context)
        { }

        public SimpleFont(string baseFont)
            : base(baseFont)
        {

            // assign the glyph list based on the font
            if ("ZapfDingbats".Equals(baseFont, StringComparison.Ordinal))
            {
                glyphList = GlyphMapping.ZapfDingbats;
            }
            else
            {
                glyphList = GlyphMapping.Default;
            }
        }

        protected SimpleFont(PdfDirectObject baseObject) : base(baseObject)
        { }
        #endregion

        /**
        * Returns the Encoding vector.
        */
        public Encoding Encoding
        {
            get => encoding;
        }

        /**
         * Returns the Encoding vector.
         */
        public GlyphMapping GlyphList
        {
            get => glyphList;
        }

        public override bool IsStandard14
        {
            get
            {
                // this logic is based on Acrobat's behaviour, see PDFBOX-2372
                // the Encoding entry cannot have Differences if we want "standard 14" font handling
                if (Encoding is DictionaryEncoding dictionary)
                {
                    if (dictionary.Differences.Count > 0)
                    {
                        // we also require that the differences are actually different, see PDFBOX-1900 with
                        // the file from PDFBOX-2192 on Windows
                        Encoding baseEncoding = dictionary.BaseEncoding;
                        foreach (var entry in dictionary.Differences)
                        {
                            if (!entry.Value.Equals(baseEncoding.GetName(entry.Key), StringComparison.Ordinal))
                            {
                                return false;
                            }
                        }
                    }
                }
                return base.IsStandard14;
            }
        }

        #region interface
        /**
         * Returns true the font is a symbolic (that is, it does not use the Adobe Standard Roman
         * character set).
         */
        public override bool Symbolic
        {
            get
            {
                if (isSymbolic == null)
                {
                    isSymbolic = FontSymbolic ?? false;
                }
                return isSymbolic.Value;
            }
        }

        /**
         * Internal implementation of isSymbolic, allowing for the fact that the result may be
         * indeterminate.
         */
        protected bool? FontSymbolic
        {
            get
            {
                bool? result = SymbolicFlag;
                if (result != null)
                {
                    return result;
                }
                else if (IsStandard14)
                {
                    string mappedName = Standard14Fonts.GetMappedFontName(Name);
                    return mappedName.Equals("Symbol", StringComparison.Ordinal) || mappedName.Equals("ZapfDingbats", StringComparison.Ordinal);
                }
                else
                {
                    if (encoding == null)
                    {
                        // sanity check, should never happen
                        if (!(this is PdfTrueTypeFont))
                        {
                            throw new InvalidOperationException("PDFBox bug: encoding should not be null!");
                        }

                        // TTF without its non-symbolic flag set must be symbolic
                        return true;
                    }
                    else if (encoding is WinAnsiEncoding ||
                             encoding is MacRomanEncoding ||
                             encoding is StandardEncoding)
                    {
                        return false;
                    }
                    else if (encoding is DictionaryEncoding)
                    {
                        // each name in Differences array must also be in the latin character set
                        foreach (string name in ((DictionaryEncoding)encoding).Differences.Values)
                        {
                            if (".notdef".Equals(name, StringComparison.Ordinal))
                            {
                                // skip
                            }
                            else if (!(WinAnsiEncoding.Instance.Contains(name) &&
                                       MacRomanEncoding.Instance.Contains(name) &&
                                       StandardEncoding.Instance.Contains(name)))
                            {
                                return true;
                            }

                        }
                        return false;
                    }
                    else
                    {
                        // we don't know
                        return null;
                    }
                }
            }
        }

        /**
        * Returns the value of the symbolic flag,  allowing for the fact that the result may be
        * indeterminate.
*/
        protected bool? SymbolicFlag
        {
            get
            {
                if (FontDescriptor != null)
                {
                    // fixme: isSymbolic() defaults to false if the flag is missing so we can't trust this
                    return (FontDescriptor.Flags & FlagsEnum.Symbolic) == FlagsEnum.Symbolic;
                }
                return null;
            }
        }

        public override int ToUnicode(int code)
        {
            return ToUnicode(code, GlyphMapping.Default);
        }

        public virtual int ToUnicode(int code, GlyphMapping customGlyphList)
        {
            // allow the glyph list to be overridden for the purpose of extracting Unicode
            // we only do this when the font's glyph list is the AGL, to avoid breaking Zapf Dingbats
            GlyphMapping unicodeGlyphList;
            if (this.glyphList == GlyphMapping.Default)
            {
                unicodeGlyphList = customGlyphList;
            }
            else
            {
                unicodeGlyphList = this.glyphList;
            }

            // first try to use a ToUnicode CMap
            var unicode = base.ToUnicode(code);
            if (unicode > -1)
            {
                return unicode;
            }

            // if the font is a "simple font" and uses MacRoman/MacExpert/WinAnsi[Encoding]
            // or has Differences with names from only Adobe Standard and/or Symbol, then:
            //
            //    a) Dictionary the character codes to names
            //    b) Look up the name in the Adobe Glyph List to obtain the Unicode value

            string name = null;
            if (encoding != null)
            {
                name = encoding.GetName(code);
                var temp = unicodeGlyphList.NameToCode(name);
                if (temp != null)
                {
                    return temp.Value;
                }
            }

            return -1;
        }


        public override bool IsVertical
        {
            get => false;
        }

        protected virtual float GetStandard14Width(int code)
        {
            if (Standard14AFM != null)
            {
                string nameInAFM = Encoding.GetName(code);

                // the Adobe AFMs don't include .notdef, but Acrobat uses 250, test with PDFBOX-2334
                if (".notdef".Equals(nameInAFM, StringComparison.Ordinal))
                {
                    return 250f;
                }

                return Standard14AFM.GetCharacterWidth(nameInAFM);
            }
            throw new InvalidOperationException("No AFM");
        }


        /**
         * Returns the path for the character with the given name. For some fonts, GIDs may be used
         * instead of names when calling this method.
         *
         * @return glyph path
         * @throws IOException if the path could not be read
         */
        public abstract SKPath GetPath(string name);

        /**
         * Returns true if the font contains the character with the given name.
         *
         * @throws IOException if the path could not be read
         */
        public abstract bool HasGlyph(string name);

        /**
         * Returns the embedded or system font used for rendering. This is never null.
         */
        public abstract BaseFont Font { get; }


        public virtual void AddToSubset(int codePoint)
        {
            throw new NotSupportedException();
        }

        public virtual void Subset()
        {
            // only TTF subsetting via PDType0Font is currently supported
            throw new NotSupportedException();
        }

        public virtual bool WillBeSubset
        {
            get => false;
        }

        public override bool HasExplicitWidth(int code)
        {
            if (Dictionary.ContainsKey(PdfName.Widths))
            {
                //Widths
                int firstChar = FirstChar ?? -1;
                if (code >= firstChar && code - firstChar < Widths.Count)
                {
                    return true;
                }
            }
            return false;
        }
        #region protected

        /**
        * Reads the Encoding from the Font dictionary or the embedded or substituted font file.
        * Must be called at the end of any subclass constructors.
        *
        * @throws IOException if the font file could not be read
*/
        protected void ReadEncoding()
        {
            var encoding = Dictionary.Resolve(PdfName.Encoding);
            if (encoding != null)
            {
                if (encoding is PdfName encodingName)
                {
                    this.encoding = Encoding.Get(encodingName);
                    if (this.encoding == null)
                    {
                        Debug.WriteLine("warn: Unknown encoding: " + encodingName.StringValue);
                        this.encoding = ReadEncodingFromFont(); // fallback
                    }
                }
                else if (encoding is PdfDictionary encodingDict)
                {
                    Encoding builtIn = null;
                    bool symbolic = Symbolic;
                    bool isFlaggedAsSymbolic = symbolic;

                    PdfName baseEncoding = (PdfName)encodingDict.Resolve(PdfName.BaseEncoding);

                    bool hasValidBaseEncoding = baseEncoding != null && Encoding.Get(baseEncoding) != null;

                    if (!hasValidBaseEncoding && isFlaggedAsSymbolic)
                    {
                        builtIn = ReadEncodingFromFont();
                    }

                    this.encoding = new DictionaryEncoding(encodingDict, !symbolic, builtIn);
                }
            }
            else
            {
                this.encoding = ReadEncodingFromFont();
            }

            // normalise the standard 14 name, e.g "Symbol,Italic" -> "Symbol"
            string standard14Name = Standard14Fonts.GetMappedFontName(Name);

            // assign the glyph list based on the font
            if ("ZapfDingbats".Equals(standard14Name, StringComparison.Ordinal))
            {
                glyphList = GlyphMapping.ZapfDingbats;
            }
            else
            {
                // StandardEncoding and Symbol are in the AGL
                glyphList = GlyphMapping.Default;
            }
        }

        /**
         * Called by readEncoding() if the encoding needs to be extracted from the font file.
         *
         * @throws IOException if the font file could not be read.
         */
        protected abstract Encoding ReadEncodingFromFont();

        protected override void OnLoad()
        {
            ReadEncoding();
        }
        #endregion
        #endregion
    }
}