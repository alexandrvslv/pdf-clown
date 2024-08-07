/*
  Copyright 2012 Stefano Chizzolini. http://www.pdfclown.org

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

using System;

namespace PdfClown.Documents.Multimedia
{
    /// <summary>Media offset [PDF:1.7:9.1.5].</summary>
    [PDF(VersionEnum.PDF15)]
    public abstract class MediaOffset : PdfObjectWrapper<PdfDictionary>
    {
        /// <summary>Media offset frame [PDF:1.7:9.1.5].</summary>
        public sealed class Frame : MediaOffset
        {
            public Frame(PdfDocument context, int value) : base(context, PdfName.F)
            { Value = value; }

            public Frame(PdfDirectObject baseObject) : base(baseObject)
            { }

            /// <summary>Gets/Sets the (zero-based) frame within a media object.</summary>
            public override object Value
            {
                get => BaseDataObject.GetInt(PdfName.F);
                set
                {
                    int intValue = (int)value;
                    if (intValue < 0)
                        throw new ArgumentException("MUST be non-negative.");

                    BaseDataObject.Set(PdfName.F, intValue);
                }
            }
        }

        ///  <summary>Media offset marker [PDF:1.7:9.1.5].</summary>
        public sealed class Marker : MediaOffset
        {
            public Marker(PdfDocument context, string value)
                : base(context, PdfName.M)
            { Value = value; }

            public Marker(PdfDirectObject baseObject)
                : base(baseObject)
            { }

            /// <summary>Gets a named offset within a media object.</summary>
            public override object Value
            {
                get => BaseDataObject.GetString(PdfName.M);
                set => BaseDataObject.SetText(PdfName.M, (string)value);
            }
        }

        /// <summary>Media offset time [PDF:1.7:9.1.5].</summary>
        public sealed class Time : MediaOffset
        {
            public Time(PdfDocument context, double value) : base(context, PdfName.T)
            { BaseDataObject[PdfName.T] = new Timespan(value).BaseObject; }

            internal Time(PdfDirectObject baseObject) : base(baseObject)
            { }

            /// <summary>Gets/Sets the temporal offset (in seconds).</summary>
            public override object Value
            {
                get => Timespan.Time;
                set => Timespan.Time = (double)value;
            }

            private Timespan Timespan => new Timespan(BaseDataObject[PdfName.T]);
        }

        public static MediaOffset Wrap(PdfDirectObject baseObject)
        {
            if (baseObject == null)
                return null;
            if (baseObject.Wrapper is MediaOffset offset)
                return offset;

            PdfDictionary dataObject = (PdfDictionary)baseObject.Resolve();
            var offsetType = dataObject.Get<PdfName>(PdfName.S);
            if (offsetType == null
              || (dataObject.ContainsKey(PdfName.Type)
                  && !PdfName.MediaOffset.Equals(dataObject.Get<PdfName>(PdfName.Type))))
                return null;

            if (offsetType.Equals(PdfName.F))
                return new Frame(baseObject);
            else if (offsetType.Equals(PdfName.M))
                return new Marker(baseObject);
            else if (offsetType.Equals(PdfName.T))
                return new Time(baseObject);
            else
                throw new NotSupportedException();
        }

        protected MediaOffset(PdfDocument context, PdfName subtype)
            : base(context, new PdfDictionary(2)
            {
                { PdfName.Type,PdfName.MediaOffset },
                { PdfName.S, subtype },
              })
        { }

        public MediaOffset(PdfDirectObject baseObject) : base(baseObject)
        { }

        /// <summary>Gets/Sets the offset value.</summary>
        public abstract object Value
        {
            get;
            set;
        }
    }
}