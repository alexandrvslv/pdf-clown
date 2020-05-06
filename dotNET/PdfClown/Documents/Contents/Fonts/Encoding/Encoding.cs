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
        protected static readonly Dictionary<PdfName, Encoding> Encodings = new Dictionary<PdfName, Encoding>();
        #endregion

        #region interface
        public static Encoding Get(PdfName name)
        {
            if (!Encodings.TryGetValue(name, out var encoding))
            {
                if (name == PdfName.Identity)
                    encoding = IdentityEncoding.Instance;
                else if (name == PdfName.MacExpertEncoding)
                    encoding = MacExpertEncoding.Instance;
                else if (name == PdfName.MacRomanEncoding)
                    encoding = MacRomanEncoding.Instance;
                else if (name == PdfName.StandardEncoding)
                    encoding = StandardEncoding.Instance;
                else if (name == PdfName.Symbol)
                    encoding = SymbolEncoding.Instance;
                else if (name == PdfName.WinAnsiEncoding)
                    encoding = WinAnsiEncoding.Instance;
                else if (name == PdfName.ZapfDingbats)
                    encoding = ZapfDingbatsEncoding.Instance;
            }
            return encoding;
        }
        #endregion
        #endregion
        public Encoding()
        { }

        public Encoding(Dictionary<int, string> codeToName)
        {
            this.codeToName = codeToName;
        }

        #region dynamic
        #region fields
        protected internal readonly Dictionary<int, string> codeToName = new Dictionary<int, string>();
        protected internal readonly Dictionary<string, int> inverted = new Dictionary<string, int>(StringComparer.Ordinal);
        #endregion

        #region interface
        #region public

        public Dictionary<int, string> CodeToNameMap
        {
            get => codeToName;
        }

        public Dictionary<string, int> NameToCodeMap
        {
            get => inverted;
        }

        public virtual string GetName(int key)
        { return codeToName.TryGetValue(key, out var name) ? name : null; }
        #endregion

        #region protected
        protected void Put(int charCode, string charName)
        {
            codeToName[charCode] = charName;
            if (!inverted.ContainsKey(charName))
            {
                inverted[charName] = charCode;
            }
        }

        /**
         * This will add a character encoding. An already existing mapping is overwritten when creating the reverse mapping.
         * 
         * @see Encoding#add(int, string)
         *
         * @param code character code
         * @param name PostScript glyph name
         */
        protected void Overwrite(int code, string name)
        {
            // remove existing reverse mapping first
            if (codeToName.TryGetValue(code, out string oldName))
            {
                if (inverted.TryGetValue(oldName, out int oldCode) && oldCode == code)
                {
                    inverted.Remove(oldName);
                }
            }
            inverted[name] = code;
            codeToName[code] = name;
        }

        /**
         * Determines if the encoding has a mapping for the given name value.
         * 
         * @param name PostScript glyph name
         */
        public bool Contains(string name)
        {
            return inverted.ContainsKey(name);
        }

        public virtual PdfDirectObject GetPdfObject()
        {
            return null;
        }
        #endregion
        #endregion
        #endregion
    }
}