/*
  Copyright 2008-2015 Stefano Chizzolini. http://www.pdfclown.org

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
using PdfClown.Documents;
using PdfClown.Documents.Interaction.Annotations;
using PdfClown.Objects;

using System;

namespace PdfClown.Documents.Interaction.Forms
{
    /**
      <summary>Signature field [PDF:1.6:8.6.3].</summary>
    */
    [PDF(VersionEnum.PDF13)]
    public sealed class SignatureField : Field
    {
        //TODO
        #region dynamic
        #region constructors
        /**
          <summary>Creates a new signature field within the given document context.</summary>
        */
        //TODO:dictionary mandatory items (if any)!!!
        public SignatureField(string name, Widget widget) : base(PdfName.Sig, name, widget)
        { }

        internal SignatureField(PdfDirectObject baseObject) : base(baseObject)
        { }
        #endregion

        #region interface
        #region public
        /**
          <returns>A <see cref="PdfDictionary"/>.</returns>
        */
        public override object Value
        {
            get => ValueDictionary;
            set
            {
                if (!(value == null
                    || value is PdfDictionary))
                    throw new ArgumentException("Value MUST be a PdfDictionary");

                ValueDictionary = (PdfDictionary)value;
            }
        }

        private PdfDictionary ValueDictionary
        {
            get => BaseDataObject.Resolve<PdfDictionary>(PdfName.V);
            set => BaseDataObject[PdfName.V] = value;
        }

        public PdfArray ByteRange
        {
            get => ValueDictionary.Resolve<PdfArray>(PdfName.ByteRange);
            set => ValueDictionary[PdfName.ByteRange] = value;
        }

        public PdfObject Contents
        {
            get => ValueDictionary.Resolve(PdfName.Contents);
            set => ValueDictionary[PdfName.Contents] = (PdfDirectObject)value;
        }

        public string Filter
        {
            get => ((IPdfString)ValueDictionary[PdfName.Filter])?.StringValue;
            set => ValueDictionary[PdfName.Filter] = new PdfName(value);
        }

        public DateTime? DateM
        {
            get => ((PdfDate)ValueDictionary[PdfName.M])?.DateValue;
            set => ValueDictionary[PdfName.M] = value is DateTime notNullValue ? new PdfDate(notNullValue) : null;
        }

        public string SignatureName
        {
            get => ((IPdfString)ValueDictionary[PdfName.Name]).StringValue;
            set=> ValueDictionary[PdfName.Name] = new PdfString(value);
        }

        #endregion
        #endregion
        #endregion
    }
}