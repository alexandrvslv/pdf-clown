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
using PdfClown.Documents.Contents.XObjects;
using PdfClown.Documents.Interaction.Forms;
using PdfClown.Objects;
using PdfClown.Util;

using System;
using System.Collections.Generic;
using SkiaSharp;

namespace PdfClown.Documents.Interaction.Annotations
{
    /**
      <summary>Widget annotation [PDF:1.6:8.4.5].</summary>
    */
    [PDF(VersionEnum.PDF12)]
    public sealed class Widget : Annotation
    {
        #region types
        /**
          <summary>Highlighting mode [PDF:1.6:8.4.5].</summary>
        */
        public enum HighlightModeEnum
        {
            /**
              <summary>No highlighting.</summary>
            */
            None,
            /**
              <summary>Invert the contents of the annotation rectangle.</summary>
            */
            Invert,
            /**
              <summary>Invert the annotation's border.</summary>
            */
            Outline,
            /**
              <summary>Display the annotation's down appearance.</summary>
            */
            Push,
            /**
              <summary>Same as Push (which is preferred).</summary>
            */
            Toggle
        };
        #endregion

        #region static
        #region fields
        private static readonly Dictionary<HighlightModeEnum, PdfName> HighlightModeEnumCodes;
        #endregion

        #region constructors
        static Widget()
        {
            HighlightModeEnumCodes = new Dictionary<HighlightModeEnum, PdfName>
            {
                [HighlightModeEnum.None] = PdfName.N,
                [HighlightModeEnum.Invert] = PdfName.I,
                [HighlightModeEnum.Outline] = PdfName.O,
                [HighlightModeEnum.Push] = PdfName.P,
                [HighlightModeEnum.Toggle] = PdfName.T
            };
        }
        #endregion

        #region interface
        #region private
        /**
          <summary>Gets the code corresponding to the given value.</summary>
        */
        private static PdfName ToCode(HighlightModeEnum value)
        { return HighlightModeEnumCodes[value]; }

        /**
          <summary>Gets the highlighting mode corresponding to the given value.</summary>
        */
        private static HighlightModeEnum ToHighlightModeEnum(IPdfString value)
        {
            if (value == null)
                return HighlightModeEnum.Invert;
            foreach (KeyValuePair<HighlightModeEnum, PdfName> mode in HighlightModeEnumCodes)
            {
                if (string.Equals(mode.Value.StringValue, value.StringValue, StringComparison.Ordinal))
                    return mode.Key;
            }
            return HighlightModeEnum.Invert;
        }
        #endregion
        #endregion
        #endregion

        #region dynamic
        #region constructors
        /**
          <summary>Creates a new generic widget.</summary>
        */
        public Widget(Page page, SKRect box)
            : base(page, PdfName.Widget, box, null)
        { Flags = EnumUtils.Mask(Flags, AnnotationFlagsEnum.Print, true); }

        /**
          <summary>Creates a new dual-state widget (required by <see
          cref="PdfClown.Documents.forms.RadioButton"/> fields).</summary>
        */
        public Widget(Page page, SKRect box, string name)
            : this(page, box)
        {
            // Initialize the on-state appearance!
            /*
              NOTE: This is necessary to keep the reference to the on-state name.
            */
            var appearance = new Appearance(page.Document);
            Appearance = appearance;
            AppearanceStates normalAppearance = appearance.Normal;
            normalAppearance[new PdfName(name)] = new FormXObject(page.Document, Box.Size);
        }

        internal Widget(PdfDirectObject baseObject) : base(baseObject)
        { }
        #endregion

        #region interface
        #region public
        public override AnnotationActions Actions
        {
            get => WidgetActions.Wrap(this, BaseDataObject.Get<PdfDictionary>(PdfName.AA));
            set => base.Actions = value;
        }

        /**
          <summary>Gets/Sets the annotation's appearance characteristics to be used for its visual
          presentation on the page.</summary>
        */
        public AppearanceCharacteristics AppearanceCharacteristics
        {
            get => Wrap<AppearanceCharacteristics>(BaseDataObject.Get<PdfDictionary>(PdfName.MK));
            set => BaseDataObject[PdfName.MK] = value.BaseObject;
        }

        /**
          <summary>Gets/Sets the annotation's highlighting mode, the visual effect to be used when the
          mouse button is pressed or held down inside its active area.</summary>
        */
        public HighlightModeEnum HighlightMode
        {
            get => ToHighlightModeEnum((IPdfString)BaseDataObject[PdfName.H]);
            set => BaseDataObject[PdfName.H] = ToCode(value);
        }

        /**
          <summary>Gets the widget value (applicable to dual-state widgets only). It corresponds to the
          on-state appearance of this widget.</summary>
        */
        public string Value
        {
            get
            {
                foreach (KeyValuePair<PdfName, FormXObject> normalAppearanceEntry in Appearance.Normal)
                {
                    PdfName key = normalAppearanceEntry.Key;
                    if (!key.Equals(PdfName.Off)) // 'On' state.
                        return (string)key.Value;
                }
                return null; // NOTE: It MUST NOT happen (on-state should always be defined).
            }
        }
        #endregion
        #endregion
        #endregion
    }
}