/*
  Copyright 2013-2015 Stefano Chizzolini. http://www.pdfclown.org

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

using PdfClown.Documents.Contents.ColorSpaces;
using PdfClown.Documents.Contents.Composition;
using PdfClown.Objects;
using PdfClown.Util;
using SkiaSharp;
using System;

namespace PdfClown.Documents.Interaction.Annotations
{
    /// <summary>Markup annotation [PDF:1.6:8.4.5].</summary>
    /// <remarks>It represents text-based annotations used primarily to mark up documents.</remarks>
    [PDF(VersionEnum.PDF11)]
    public abstract class Markup : Annotation
    {
        protected Markup(PdfPage page, PdfName subtype, SKRect box, string text)
            : base(page, subtype, box, text)
        {
            CreationDate = DateTime.Now;
        }

        protected Markup(PdfDirectObject baseObject)
            : base(baseObject)
        { }

        /// <summary>Gets/Sets the annotation editor. It is displayed as a text label in the title bar of
        /// the annotation's pop-up window when open and active. By convention, it identifies the user who
        /// added the annotation.</summary>
        [PDF(VersionEnum.PDF11)]
        public override string Author
        {
            get => BaseDataObject.GetString(PdfName.T);
            set
            {
                var oldValue = Author;
                if (!string.Equals(oldValue, value, StringComparison.Ordinal))
                {
                    BaseDataObject.SetText(PdfName.T, value);
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        /// <summary>Gets/Sets the date and time when the annotation was created.</summary>
        [PDF(VersionEnum.PDF15)]
        public override DateTime? CreationDate
        {
            get => BaseDataObject.GetNDate(PdfName.CreationDate);
            set
            {
                var oldValue = CreationDate;
                if (oldValue != PdfDate.Trimm(value))
                {
                    BaseDataObject.Set(PdfName.CreationDate, value);
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        /// <summary>Gets/Sets the annotation that this one is in reply to. Both annotations must be on the
        /// same page of the document.</summary>
        /// <remarks>The relationship between the two annotations is specified by the
        /// <see cref="ReplyType"/> property.</remarks>
        [PDF(VersionEnum.PDF15)]
        public virtual Annotation ReplyTo
        {
            get => Annotation.Wrap(BaseDataObject[PdfName.IRT]);
            set
            {
                var oldValue = ReplyTo;
                if (oldValue != value)
                {
                    BaseDataObject[PdfName.IRT] = PdfObjectWrapper.GetBaseObject(value);
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        /// <summary>Gets/Sets the pop-up annotation associated with this one.</summary>
        /// <exception cref="InvalidOperationException">If pop-up annotations can't be associated with
        /// this markup.</exception>
        [PDF(VersionEnum.PDF13)]
        public virtual Popup Popup
        {
            get => (Popup)Annotation.Wrap(BaseDataObject[PdfName.Popup]);
            set
            {
                var oldValue = Popup;
                if (oldValue != value)
                {
                    if (value != null)
                    {
                        value.Parent = this;
                    }
                    BaseDataObject[PdfName.Popup] = value.BaseObject;
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        /// <summary>Gets/Sets the relationship between this annotation and one specified by
        /// <see cref="ReplyTo"/> property.</summary>
        [PDF(VersionEnum.PDF16)]
        public virtual ReplyTypeEnum? ReplyType
        {
            get => ReplyTypeEnumExtension.Get(BaseDataObject.GetString(PdfName.RT));
            set
            {
                var oldValue = ReplyType;
                if (oldValue != value)
                {
                    BaseDataObject[PdfName.RT] = value.GetCode();
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        [PDF(VersionEnum.PDF15)]
        public string RichContents
        {
            get => BaseDataObject.GetString(PdfName.RC);
            set
            {
                var oldValue = RichContents;
                if (!string.Equals(oldValue, value, StringComparison.Ordinal))
                {
                    BaseDataObject.SetText(PdfName.RC, value);
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        public string DefaultStyle
        {
            get => BaseDataObject.GetString(PdfName.DS);
            set
            {
                var oldValue = DefaultStyle;
                if (!string.Equals(oldValue, value, StringComparison.Ordinal))
                {
                    BaseDataObject.SetText(PdfName.DS, value);
                    OnPropertyChanged(oldValue, value);
                }
            }
        }


        [PDF(VersionEnum.PDF16)]
        public MarkupIntent? Intent
        {
            get => MarkupIntentExtension.Get(BaseDataObject.GetString(PdfName.IT));
            set
            {
                var oldValue = Intent;
                if (oldValue != value)
                {
                    BaseDataObject[PdfName.IT] = MarkupIntentExtension.GetCode(value.Value);
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        /// <summary>Gets/Sets the color with which to fill the interior of following markups: Line Ending, Circle, Square.</summary>
        public DeviceColor InteriorColor
        {
            get => DeviceColor.Get(BaseDataObject.Get<PdfArray>(PdfName.IC));
            set
            {
                var oldValue = InteriorColor;
                if (oldValue != value)
                {
                    BaseDataObject[PdfName.IC] = (PdfArray)value?.BaseDataObject;
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        public SKColor? InteriorSKColor
        {
            get => InteriorColor == null ? (SKColor?)null : DeviceColorSpace.CalcSKColor(InteriorColor, Alpha);
            set
            {
                var oldValue = InteriorSKColor;
                if (oldValue != value)
                {
                    InteriorColor = DeviceRGBColor.Get(value);
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        /// <summary>Gets/Sets the border effect.</summary>
        [PDF(VersionEnum.PDF15)]
        public BorderEffect BorderEffect
        {
            get => Wrap<BorderEffect>(BaseDataObject[PdfName.BE]);
            set
            {
                var oldValue = BorderEffect;
                if (!(oldValue?.Equals(value) ?? value == null))
                {
                    BaseDataObject[PdfName.BE] = PdfObjectWrapper.GetBaseObject(value);
                    QueueRefreshAppearance();
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        public SKPath ApplyBorderAndEffect(PrimitiveComposer composer, SKPath path)
        {
            ApplyBorder(composer);
            return BorderEffect?.Apply(composer, path) ?? path;
        }

        protected void ApplyBorder(PrimitiveComposer composer)
        {
            if (Border != null) Border.Apply(composer);
            else composer.SetLineWidth(1);
        }

        public virtual void ApplyBorderAndEffect(ref SKRect rect)
        {
            ApplyBorder(ref rect);
            BorderEffect?.Apply(ref rect);
        }

        protected void ApplyBorder(ref SKRect rect)
        {
            if (Border == null)
                rect.Inflate(1, 1);
            else
                Border.Apply(ref rect);
        }

        public virtual void InvertBorderAndEffect(ref SKRect rect)
        {
            InvertBorder(ref rect);
            BorderEffect?.Invert(ref rect);
        }

        protected void InvertBorder(ref SKRect rect)
        {
            if (Border == null)
            {
                if (rect.Width > 2 && rect.Height > 2)
                    rect.Inflate(-1, -1);
            }
            else
                Border.Invert(ref rect);
        }
    }

    // <summary>Annotation relationship [PDF:1.6:8.4.5].</summary>
    [PDF(VersionEnum.PDF16)]
    public enum ReplyTypeEnum
    {
        Thread,
        Group
    }

    internal static class ReplyTypeEnumExtension
    {
        private static readonly BiDictionary<ReplyTypeEnum, string> codes;

        static ReplyTypeEnumExtension()
        {
            codes = new BiDictionary<ReplyTypeEnum, string>
            {
                [ReplyTypeEnum.Thread] = PdfName.R.StringValue,
                [ReplyTypeEnum.Group] = PdfName.Group.StringValue
            };
        }

        public static ReplyTypeEnum? Get(string name)
        {
            if (name == null)
                return ReplyTypeEnum.Thread;

            return codes.GetKey(name);
        }

        public static PdfName GetCode(this ReplyTypeEnum? replyType)
        {
            return replyType == null ? null : PdfName.Get(codes[replyType.Value], true);
        }
    }
}