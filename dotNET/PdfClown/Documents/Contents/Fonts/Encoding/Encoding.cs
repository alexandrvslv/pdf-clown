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

using System;
using System.Collections.Generic;

namespace PdfClown.Documents.Contents.Fonts
{
    /**
      <summary>Predefined encodings [PDF:1.6:5.5.5,D].</summary>
    */
    // TODO: This hierarchy is going to be superseded by PdfClown.Tokens.Encoding.
    public class Encoding
    {
        #region static
        #region fields
        private static readonly Dictionary<PdfName, Encoding> Encodings = new Dictionary<PdfName, Encoding>();
        #endregion

        #region constructors
        static Encoding()
        {
            //TODO:this collection MUST be automatically populated looking for Encoding subclasses!
            Encodings[PdfName.StandardEncoding] = new StandardEncoding();
            Encodings[PdfName.MacRomanEncoding] = new MacRomanEncoding();
            Encodings[PdfName.WinAnsiEncoding] = new WinAnsiEncoding();
            Encodings[PdfName.Identity] = new IdentityEncoding();
            Encodings[PdfName.Symbol] = new SymbolEncoding();
            Encodings[PdfName.ZapfDingbats] = new ZapfDingbatsEncoding();
        }
        #endregion

        #region interface
        public static Encoding Get(PdfName name)
        { return Encodings[name]; }
        #endregion
        #endregion
        public Encoding()
        { }

        public Encoding(Dictionary<int, string> codeToName)
        {
            names = codeToName;
        }

        #region dynamic
        #region fields
        private readonly Dictionary<int, string> names = new Dictionary<int, string>();
        private readonly Dictionary<int, int> codes = new Dictionary<int, int>();
        #endregion

        #region interface
        #region public
        public Dictionary<int, int> GetCodes()
        { return new Dictionary<int, int>(codes); }

        public int GetUnicode(int key)
        { return codes.TryGetValue(key, out var unicode) ? unicode : 0; }

        public virtual string GetName(int key)
        { return names.TryGetValue(key, out var name) ? name : null; }
        #endregion

        #region protected
        protected void Put(int charCode, string charName)
        {
            names[new ByteArray((byte)charCode)] = charName;
            Put(charCode, GlyphMapping.Default.NameToCode(charName).Value);
        }

        protected void Put(int charCode, int unicode)
        {
            codes[new ByteArray((byte)charCode)] = unicode;
        }
        #endregion
        #endregion
        #endregion
    }
}