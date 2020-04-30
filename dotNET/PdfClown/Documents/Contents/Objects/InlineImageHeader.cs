/*
  Copyright 2007-2010 Stefano Chizzolini. http://www.pdfclown.org

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

using PdfClown.Bytes;
using PdfClown.Objects;

using System;
using System.Collections;
using System.Collections.Generic;

namespace PdfClown.Documents.Contents.Objects
{
    /**
      <summary>Inline image entries (anonymous) operation [PDF:1.6:4.8.6].</summary>
      <remarks>This is a figurative operation necessary to constrain the inline image entries section
      within the content stream model.</remarks>
    */
    [PDF(VersionEnum.PDF10)]
    public sealed class InlineImageHeader : Operation, IDictionary<PdfName, PdfDirectObject>
    {
        #region dynamic
        #region constructors
        // [FIX:0.0.4:2] Null operator.
        public InlineImageHeader(IList<PdfDirectObject> operands) : base(String.Empty, operands)
        { }
        #endregion

        #region public
        #region IDictionary
        public void Add(PdfName key, PdfDirectObject value)
        {
            if (ContainsKey(key))
                throw new ArgumentException("Key '" + key + "' already in use.", "key");

            this[key] = value;
        }

        public int BitsPerComponent
        {
            get => (((IPdfNumber)this[PdfName.BPC]) ?? ((IPdfNumber)this[PdfName.BitsPerComponent]))?.IntValue ?? 8;
            set => this[PdfName.BPC] = new PdfInteger(value);
        }

        public PdfName ColorSpace
        {
            get => (((PdfName)this[PdfName.CS]) ?? ((PdfName)this[PdfName.ColorSpace]));
            set => this[PdfName.CS] = value;
        }

        public PdfName FormatColorSpace
        {
            get
            {
                var space = ColorSpace;
                if (space == null)
                {
                    return PdfName.DeviceGray;
                }
                if (space.Equals(PdfName.G)) return PdfName.DeviceGray;
                if (space.Equals(PdfName.RGB)) return PdfName.DeviceRGB;
                if (space.Equals(PdfName.CMYK)) return PdfName.DeviceCMYK;
                if (space.Equals(PdfName.I)) return PdfName.Indexed;
                return space;
            }
        }

        public PdfArray Decode
        {
            get => ((PdfArray)this[PdfName.D]) ?? ((PdfArray)this[PdfName.Decode]);
            set => this[PdfName.D] = value;
        }

        public PdfDirectObject DecodeParms
        {
            get => this[PdfName.DP] ?? this[PdfName.DecodeParms];
            set => this[PdfName.DP] = value;
        }

        public PdfDirectObject Filter
        {
            get => this[PdfName.F] ?? this[PdfName.Filter];
            set => this[PdfName.F] = value;
        }

        public int Height
        {
            get => (((IPdfNumber)this[PdfName.H]) ?? ((IPdfNumber)this[PdfName.Height]))?.IntValue ?? 0;
            set => this[PdfName.H] = new PdfInteger(value);
        }

        public int Width
        {
            get => (((IPdfNumber)this[PdfName.W]) ?? ((IPdfNumber)this[PdfName.Width]))?.IntValue ?? 0;
            set => this[PdfName.W] = new PdfInteger(value);
        }

        public string ImageMask
        {
            get => (this[PdfName.IM] ?? this[PdfName.ImageMask])?.ToString();
            set => this[PdfName.IM] = new PdfName(value);
        }

        public string Interpolate
        {
            get => (this[PdfName.I] ?? this[PdfName.Interpolate])?.ToString();
            set => this[PdfName.I] = new PdfName(value);
        }

        public string Intent
        {
            get => this[PdfName.Intent]?.ToString();
            set => this[PdfName.Intent] = new PdfName(value);
        }

        public bool ContainsKey(PdfName key)
        { return GetKeyIndex(key) != null; }

        public ICollection<PdfName> Keys
        {
            get
            {
                ICollection<PdfName> keys = new List<PdfName>();
                for (int index = 0, length = operands.Count - 1; index < length; index += 2)
                { keys.Add((PdfName)operands[index]); }
                return keys;
            }
        }

        public bool Remove(PdfName key)
        {
            int? index = GetKeyIndex(key);
            if (!index.HasValue)
                return false;

            operands.RemoveAt(index.Value);
            operands.RemoveAt(index.Value);
            return true;
        }

        public PdfDirectObject this[PdfName key]
        {
            get
            {
                /*
                  NOTE: This is an intentional violation of the official .NET Framework Class
                  Library prescription: no exception thrown anytime a key is not found.
                */
                int? index = GetKeyIndex(key);
                if (index == null)
                    return null;

                return operands[index.Value + 1];
            }
            set
            {
                int? index = GetKeyIndex(key);
                if (index == null)
                {
                    operands.Add(key);
                    operands.Add(value);
                }
                else
                {
                    operands[index.Value] = key;
                    operands[index.Value + 1] = value;
                }
            }
        }

        public bool TryGetValue(PdfName key, out PdfDirectObject value)
        { throw new NotImplementedException(); }

        public ICollection<PdfDirectObject> Values
        {
            get
            {
                ICollection<PdfDirectObject> values = new List<PdfDirectObject>();
                for (int index = 1, length = operands.Count - 1; index < length; index += 2)
                { values.Add(operands[index]); }
                return values;
            }
        }

        #region ICollection
        void ICollection<KeyValuePair<PdfName, PdfDirectObject>>.Add(KeyValuePair<PdfName, PdfDirectObject> keyValuePair)
        { Add(keyValuePair.Key, keyValuePair.Value); }

        public void Clear()
        { operands.Clear(); }

        bool ICollection<KeyValuePair<PdfName, PdfDirectObject>>.Contains(KeyValuePair<PdfName, PdfDirectObject> keyValuePair)
        { return (this[keyValuePair.Key] == keyValuePair.Value); }

        public void CopyTo(KeyValuePair<PdfName, PdfDirectObject>[] keyValuePairs, int index)
        { throw new NotImplementedException(); }

        public int Count => operands.Count / 2;

        public bool IsReadOnly => false;

        public bool Remove(KeyValuePair<PdfName, PdfDirectObject> keyValuePair)
        { throw new NotImplementedException(); }

        #region IEnumerable<KeyValuePair<PdfName,PdfDirectObject>>
        IEnumerator<KeyValuePair<PdfName, PdfDirectObject>> IEnumerable<KeyValuePair<PdfName, PdfDirectObject>>.GetEnumerator()
        {
            for (int index = 0, length = operands.Count - 1; index < length; index += 2)
            {
                yield return new KeyValuePair<PdfName, PdfDirectObject>(
                  (PdfName)operands[index],
                  operands[index + 1]
                  );
            }
        }

        #region IEnumerable
        IEnumerator IEnumerable.GetEnumerator()
        { return ((IEnumerable<KeyValuePair<PdfName, PdfDirectObject>>)this).GetEnumerator(); }
        #endregion
        #endregion
        #endregion
        #endregion
        #endregion

        #region private
        private int? GetKeyIndex(object key)
        {
            for (int index = 0, length = operands.Count - 1; index < length; index += 2)
            {
                if (operands[index].Equals(key))
                    return index;
            }
            return null;
        }
        #endregion
        #endregion
    }
}