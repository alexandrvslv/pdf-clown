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
using PdfClown.Documents.Contents;
using PdfClown.Documents.Contents.ColorSpaces;
using PdfClown.Documents.Contents.Layers;
using PdfClown.Documents.Interaction.Actions;
using PdfClown.Files;
using PdfClown.Objects;
using PdfClown.Util;

using System;
using SkiaSharp;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using PdfClown.Documents.Contents.XObjects;
using PdfClown.Util.Math.Geom;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Globalization;
using PdfClown.Documents.Interaction.Annotations.ControlPoints;
using PdfClown.Documents.Contents.Objects;
using PdfClown.Documents.Contents.Tokens;
using Org.BouncyCastle.Bcpg.OpenPgp;
//using System.Diagnostics;

namespace PdfClown.Documents.Interaction.Annotations
{
    /**
      <summary>Annotation [PDF:1.6:8.4].</summary>
    */
    [PDF(VersionEnum.PDF10)]
    public abstract class Annotation : PdfObjectWrapper<PdfDictionary>, ILayerable, INotifyPropertyChanged
    {
        private PdfPage page;
        private string name;
        private SKColor? color;
        private SKRect? box;
        protected BottomRightControlPoint cpBottomRight;
        protected BottomLeftControlPoint cpBottomLeft;
        protected TopRightControlPoint cpTopRight;
        protected TopLeftControlPoint cpTopLeft;
        private SetFont setFontOperation;
        internal RefreshAppearanceState queueRefresh;

        public event PropertyChangedEventHandler PropertyChanged;

        /**
          <summary>Wraps an annotation base object into an annotation object.</summary>
          <param name="baseObject">Annotation base object.</param>
          <returns>Annotation object associated to the base object.</returns>
        */
        public static Annotation Wrap(PdfDirectObject baseObject)
        {
            if (baseObject == null)
                return null;
            if (baseObject.Wrapper is Annotation annotation)
                return annotation;
            if (baseObject is PdfReference reference && reference.DataObject?.Wrapper is Annotation referenceAnnotation)
            {
                baseObject.Wrapper = referenceAnnotation;
                return referenceAnnotation;
            }
            var dictionary = baseObject.Resolve() as PdfDictionary;
            if (dictionary == null)
                return null;
            PdfName annotationType = (PdfName)dictionary[PdfName.Subtype];
            if (annotationType.Equals(PdfName.Text))
                return new StickyNote(baseObject);
            else if (annotationType.Equals(PdfName.Link))
                return new Link(baseObject);
            else if (annotationType.Equals(PdfName.FreeText))
                return new FreeText(baseObject);
            else if (annotationType.Equals(PdfName.Line))
                return new Line(baseObject);
            else if (annotationType.Equals(PdfName.Square))
                return new Rectangle(baseObject);
            else if (annotationType.Equals(PdfName.Circle))
                return new Ellipse(baseObject);
            else if (annotationType.Equals(PdfName.Polygon))
                return new Polygon(baseObject);
            else if (annotationType.Equals(PdfName.PolyLine))
                return new Polyline(baseObject);
            else if (annotationType.Equals(PdfName.Highlight)
              || annotationType.Equals(PdfName.Underline)
              || annotationType.Equals(PdfName.Squiggly)
              || annotationType.Equals(PdfName.StrikeOut))
                return new TextMarkup(baseObject);
            else if (annotationType.Equals(PdfName.Stamp))
                return new Stamp(baseObject);
            else if (annotationType.Equals(PdfName.Caret))
                return new Caret(baseObject);
            else if (annotationType.Equals(PdfName.Ink))
                return new Scribble(baseObject);
            else if (annotationType.Equals(PdfName.Popup))
                return new Popup(baseObject);
            else if (annotationType.Equals(PdfName.FileAttachment))
                return new FileAttachment(baseObject);
            else if (annotationType.Equals(PdfName.Sound))
                return new Sound(baseObject);
            else if (annotationType.Equals(PdfName.Movie))
                return new Movie(baseObject);
            else if (annotationType.Equals(PdfName.Widget))
                return new Widget(baseObject);
            else if (annotationType.Equals(PdfName.Screen))
                return new Screen(baseObject);
            //TODO
            //     else if(annotationType.Equals(PdfName.PrinterMark)) return new PrinterMark(baseObject);
            //     else if(annotationType.Equals(PdfName.TrapNet)) return new TrapNet(baseObject);
            //     else if(annotationType.Equals(PdfName.Watermark)) return new Watermark(baseObject);
            //     else if(annotationType.Equals(PdfName.3DAnnotation)) return new 3DAnnotation(baseObject);
            else // Other annotation type.
                return new GenericAnnotation(baseObject);
        }

        protected Annotation(PdfPage page, PdfName subtype, SKRect box, string text)
            : base(page.Document,
                  new PdfDictionary(3)
                  {
                      { PdfName.Type, PdfName.Annot },
                      { PdfName.Subtype, subtype }, // NOTE: Hide border by default.
                  })
        {
            GenerateName();
            Page = page;
            Box = Page.InvertRotateMatrix.MapRect(box);
            Contents = text;
            Printable = true;
            IsNew = true;
        }

        public Annotation(PdfDirectObject baseObject) : base(baseObject)
        { }

        public virtual string Author
        {
            get => string.Empty;
            set { }
        }

        public virtual DateTime? CreationDate
        {
            get => null;
            set { }
        }

        /**
          <summary>Gets/Sets action to be performed when the annotation is activated.</summary>
        */
        [PDF(VersionEnum.PDF11)]
        public virtual Actions.Action Action
        {
            get => Interaction.Actions.Action.Wrap(BaseDataObject[PdfName.A]);
            set
            {
                var oldValue = Action;
                if (oldValue != value)
                {
                    BaseDataObject[PdfName.A] = PdfObjectWrapper.GetBaseObject(value);
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        /**
          <summary>Gets/Sets the annotation's behavior in response to various trigger events.</summary>
        */
        [PDF(VersionEnum.PDF12)]
        public virtual AnnotationActions Actions
        {
            get => CommonAnnotationActions.Wrap(this, BaseDataObject.Get<PdfDictionary>(PdfName.AA));
            set
            {
                var oldValue = Actions;
                if (oldValue != value)
                {
                    BaseDataObject[PdfName.AA] = PdfObjectWrapper.GetBaseObject(value);
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        /**
         <summary>Gets/Sets the constant opacity value to be used in painting the annotation.</summary>
         <remarks>This value applies to all visible elements of the annotation (including its background
         and border) but not to the popup window that appears when the annotation is opened.</remarks>
       */
        [PDF(VersionEnum.PDF14)]
        public virtual float Alpha
        {
            get => BaseDataObject.GetFloat(PdfName.CA, 1F);
            set
            {
                var oldValue = Alpha;
                if (oldValue != value)
                {
                    BaseDataObject.SetFloat(PdfName.CA, value);
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        /**
          <summary>Gets/Sets the appearance specifying how the annotation is presented visually on the page.</summary>
        */
        [PDF(VersionEnum.PDF12)]
        public virtual Appearance Appearance
        {
            get => Wrap<Appearance>(BaseDataObject.Get<PdfDictionary>(PdfName.AP));
            set
            {
                var oldValue = (Appearance)null;
                if (oldValue != value)
                {
                    BaseDataObject[PdfName.AP] = PdfObjectWrapper.GetBaseObject(value);
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        /**
          <summary>Gets/Sets the border style.</summary>
        */
        [PDF(VersionEnum.PDF11)]
        public virtual Border Border
        {
            get => Wrap<Border>(BaseDataObject[PdfName.BS]);
            set
            {
                var oldValue = Border;
                if (!(oldValue?.Equals(value) ?? value == null))
                {
                    BaseDataObject[PdfName.BS] = value.BaseDataObject;
                    if (value != null)
                    { BaseDataObject.Remove(PdfName.Border); }
                    OnPropertyChanged(oldValue, value);
                    QueueRefreshAppearance();
                }
            }
        }

        /**
          <summary>Gets/Sets the location of the annotation on the page.
          </summary>
        */
        public virtual SKRect Box
        {
            get => box ??= Rect?.ToRect() ?? SKRect.Empty;
            set
            {
                var oldValue = Box;
                var newValue = PrimitiveExtensions.Round(value);
                if (oldValue != newValue)
                {
                    Rect = new Objects.Rectangle(box.Value);
                    box = newValue;
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        public virtual Objects.Rectangle Rect
        {
            get => Wrap<Objects.Rectangle>(BaseDataObject[PdfName.Rect]);
            set
            {
                var oldValue = Rect;
                if (!(oldValue?.Equals(value) ?? value == null))
                {
                    box = null;
                    BaseDataObject[PdfName.Rect] = value.BaseDataObject;
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        /**
          <summary>Gets/Sets the annotation color.</summary>
        */
        [PDF(VersionEnum.PDF11)]
        public virtual DeviceColor Color
        {
            get => DeviceColor.Get((PdfArray)BaseDataObject[PdfName.C]);
            set
            {
                var oldValue = Color;
                if (oldValue != value)
                {
                    BaseDataObject[PdfName.C] = PdfObjectWrapper.GetBaseObject(value);
                    QueueRefreshAppearance();
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        public virtual SKColor SKColor
        {
            get => color ??= (Color == null
                ? DeviceRGBColorSpace.CalcSKColor(DeviceRGBColor.Black, Alpha)
                : DeviceColorSpace.CalcSKColor(Color, Alpha));
            set
            {
                var oldValue = SKColor;
                if (!oldValue.Equals(value))
                {
                    color = value;
                    Color = DeviceRGBColor.Get(value);
                    Alpha = value.Alpha / 255F;
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        /**
          <summary>Gets/Sets the annotation flags.</summary>
        */
        [PDF(VersionEnum.PDF11)]
        public virtual AnnotationFlagsEnum Flags
        {
            get => (AnnotationFlagsEnum)BaseDataObject.GetInt(PdfName.F);
            set
            {
                var oldValue = Flags;
                if (oldValue != value)
                {
                    BaseDataObject.SetInt(PdfName.F, (int)value);
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        /**
          <summary>Gets/Sets the date and time when the annotation was most recently modified.</summary>
        */
        [PDF(VersionEnum.PDF11)]
        public virtual DateTime? ModificationDate
        {
            //NOTE: Despite PDF date being the preferred format, loose formats are tolerated by the spec.
            get => BaseDataObject.GetNDate(PdfName.M);
            set
            {
                var oldValue = ModificationDate;
                if (oldValue != value)
                {
                    BaseDataObject.SetDate(PdfName.M, value);
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        /**
          <summary>Gets/Sets the annotation name.</summary>
          <remarks>The annotation name uniquely identifies the annotation among all the annotations on its page.</remarks>
        */
        [PDF(VersionEnum.PDF14)]
        public virtual string Name
        {
            get => name ??= BaseDataObject.GetString(PdfName.NM);
            set
            {
                var oldValue = Name;
                if (!string.Equals(oldValue, value, StringComparison.Ordinal))
                {
                    name = value;
                    BaseDataObject.SetText(PdfName.NM, value);
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        /**
          <summary>Gets/Sets the associated page.</summary>
        */
        [PDF(VersionEnum.PDF13)]
        public virtual PdfPage Page
        {
            get => page ??= Wrap<PdfPage>(BaseDataObject[PdfName.P]);
            set
            {
                page = null;
                var oldPage = Page;
                if (oldPage == value)
                {
                    return;
                }
                box = null;
                oldPage?.Annotations.Remove(this);
                page = value;
                if (page != null)
                {
                    if (!page.Annotations.Contains(this))
                    {
                        page.Annotations.Add(this);
                    }
                    else if (BaseDataObject[PdfName.P] == null)
                    {
                        page.Annotations.DoAdd(this);
                    }
                    //Debug.WriteLine($"Move to page {page}");
                }
                OnPropertyChanged(oldPage, value);
            }
        }

        /**
          <summary>Gets/Sets whether to print the annotation when the page is printed.</summary>
        */
        [PDF(VersionEnum.PDF11)]
        public virtual bool Printable
        {
            get => (Flags & AnnotationFlagsEnum.Print) == AnnotationFlagsEnum.Print;
            set
            {
                var oldValue = Printable;
                if (oldValue != value)
                {
                    Flags = EnumUtils.Mask(Flags, AnnotationFlagsEnum.Print, value);
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        /**
          <summary>Gets/Sets the annotation text.</summary>
          <remarks>Depending on the annotation type, the text may be either directly displayed
          or (in case of non-textual annotations) used as alternate description.</remarks>
        */
        public virtual string Contents
        {
            get => BaseDataObject.GetString(PdfName.Contents);
            set
            {
                var oldValue = Contents;
                if (!string.Equals(oldValue, value, StringComparison.Ordinal))
                {
                    BaseDataObject.SetText(PdfName.Contents, value);
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        /**
         <summary>Gets/Sets the annotation subject.</summary>
       */
        [PDF(VersionEnum.PDF15)]
        public virtual string Subject
        {
            get => BaseDataObject.GetString(PdfName.Subj);
            set
            {
                var oldValue = Subject;
                if (!string.Equals(oldValue, value, StringComparison.Ordinal))
                {
                    BaseDataObject.SetText(PdfName.Subj, value);
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        /**
          <summary>Gets/Sets whether the annotation is visible.</summary>
        */
        [PDF(VersionEnum.PDF11)]
        public virtual bool Visible
        {
            get => (Flags & AnnotationFlagsEnum.Hidden) != AnnotationFlagsEnum.Hidden;
            set
            {
                var oldValue = Visible;
                if (oldValue != value)
                {
                    Flags = EnumUtils.Mask(Flags, AnnotationFlagsEnum.Hidden, !value);
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        [PDF(VersionEnum.PDF15)]
        public virtual LayerEntity Layer
        {
            get => (LayerEntity)PropertyList.Wrap(BaseDataObject[PdfName.OC]);
            set
            {
                var oldValue = Layer;
                if (oldValue != value)
                {
                    BaseDataObject[PdfName.OC] = value?.Membership.BaseObject;
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        protected internal SKMatrix PageMatrix
        {
            get => Page?.RotateMatrix ?? GraphicsState.GetRotationLeftBottomMatrix(SKRect.Create(Document.GetSize()), 0);
        }

        protected internal SKMatrix InvertPageMatrix
        {
            get => Page?.InvertRotateMatrix ?? (GraphicsState.GetRotationLeftBottomMatrix(SKRect.Create(Document.GetSize()), 0).TryInvert(out var inverted) ? inverted : SKMatrix.Identity);
        }

        public SKPoint TopLeftPoint
        {
            get => new SKPoint(Box.Left, Box.Top);
            set
            {
                var rect = new SKRect(value.X, value.Y, Box.Right, Box.Bottom);
                MoveTo(rect);
            }
        }

        public SKPoint TopRightPoint
        {
            get => new SKPoint(Box.Right, Box.Top);
            set
            {
                var rect = new SKRect(Box.Left, value.Y, value.X, Box.Bottom);
                MoveTo(rect);
            }
        }

        public SKPoint BottomLeftPoint
        {
            get => new SKPoint(Box.Left, Box.Bottom);
            set
            {
                var rect = new SKRect(value.X, Box.Top, Box.Right, value.Y);
                MoveTo(rect);
            }
        }

        public SKPoint BottomRightPoint
        {
            get => new SKPoint(Box.Right, Box.Bottom);
            set
            {
                var rect = new SKRect(Box.Left, Box.Top, value.X, value.Y);
                MoveTo(rect);
            }
        }


        public virtual bool ShowToolTip => true;

        public virtual bool AllowDrag => true;

        public virtual bool AllowSize => true;

        public bool IsNew { get; set; }

        public List<Annotation> Replies { get; set; } = new List<Annotation>();

        protected PdfString DAString
        {
            get => (PdfString)Dictionary[PdfName.DA];
            set => Dictionary[PdfName.DA] = value;
        }

        protected SetFont DAOperation
        {
            get
            {
                if (setFontOperation != null)
                    return setFontOperation;
                if (DAString == null)
                    return null;
                var parser = new ContentParser(DAString.RawValue);
                foreach (ContentObject content in parser.ParseContentObjects())
                {
                    if (content is SetFont setFont)
                    {
                        return setFontOperation = setFont;
                    }
                }
                return null;
            }
            set
            {
                setFontOperation = value;
                if (setFontOperation != null)
                {
                    var buffer = new ByteStream(64);
                    value.WriteTo(buffer, Document);
                    DAString = new PdfString(buffer.AsMemory());
                }
            }
        }

        public bool IsDrawed { get; private set; }

        public virtual void MoveTo(SKRect newBox)
        {
            Box = newBox;
        }

        /**
          <summary>Deletes this annotation removing also its reference on the page.</summary>
        */
        public override bool Delete()
        {
            Remove();

            // Deep removal (indirect object).
            return base.Delete();
        }

        public virtual void Remove()
        {
            // Shallow removal (references):
            // * reference on page
            Page?.Annotations.Remove(this);
        }

        protected RotationEnum GetPageRotation()
        {
            return Page?.Rotation ?? RotationEnum.Downward;
        }

        public SKRect Draw(SKCanvas canvas)
        {
            if ((queueRefresh & RefreshAppearanceState.Queued) == RefreshAppearanceState.Queued
                && (queueRefresh & RefreshAppearanceState.Suspend) != RefreshAppearanceState.Suspend)
            {
                try
                {
                    queueRefresh |= RefreshAppearanceState.Process;
                    RefreshAppearance();
                }
                finally
                {
                    queueRefresh = RefreshAppearanceState.None;
                }
            }
            var appearance = Appearance.Normal[null];
            if (appearance != null && appearance.BaseDataObject?.Body?.Length > 0)
            {
                return DrawAppearance(canvas, appearance);
            }
            else
            {
                return RefreshAppearance(canvas);
            }
        }

        public void RefreshAppearance()
        {
            RefreshBox();
            GenerateAppearance();
        }

        protected abstract FormXObject GenerateAppearance();

        public virtual SKRect RefreshAppearance(SKCanvas canvas)
        {
            var appearance = GenerateAppearance();
            return appearance != null ? DrawAppearance(canvas, appearance) : Box;
        }

        protected virtual SKRect DrawAppearance(SKCanvas canvas, FormXObject appearance)
        {
            var picture = appearance.Render(null);
            SKMatrix matrix = GenerateDrawMatrix(appearance, out var bounds);

            if (Alpha < 1)
            {
                using (var paint = new SKPaint())
                {
                    paint.Color = paint.Color.WithAlpha((byte)(Alpha * 255));
                    canvas.DrawPicture(picture, ref matrix, paint);
                }
            }
            else
            {
                canvas.DrawPicture(picture, ref matrix);
            }
            IsDrawed = true;
            return bounds;
        }

        public FormXObject ResetAppearance(out SKMatrix zeroMatrix) => ResetAppearance(Box, out zeroMatrix);

        public virtual FormXObject ResetAppearance(SKRect box, out SKMatrix zeroMatrix)
        {
            var boxSize = SKRect.Create(box.Width, box.Height);
            zeroMatrix = PageMatrix;
            var pageBox = zeroMatrix.MapRect(box);
            zeroMatrix = zeroMatrix.PostConcat(SKMatrix.CreateTranslation(-pageBox.Left, -pageBox.Top));
            AppearanceStates normalAppearances = Appearance.Normal;
            FormXObject normalAppearance = normalAppearances[null];
            if (normalAppearance != null)
            {
                normalAppearance.Box = boxSize;
                normalAppearance.BaseDataObject.Body.SetLength(0);
                normalAppearance.ClearContents();
            }
            else
            {
                normalAppearances[null] =
                      normalAppearance = new FormXObject(Document, boxSize);
            }
            IsDrawed = false;
            return normalAppearance;
        }

        protected virtual SKMatrix GenerateDrawMatrix(FormXObject appearance, out SKRect bound)
        {
            bound = GetDrawBox();
            var appearanceBounds = appearance.Box;

            var matrix = appearance.Matrix;
            var quad = new Quad(appearanceBounds);
            quad.Transform(ref matrix);

            var a = SKMatrix.CreateScale(bound.Width / quad.HorizontalLength, bound.Height / quad.VerticalLenght);
            var quadA = Quad.Transform(quad, ref a);
            a = a.PostConcat(SKMatrix.CreateTranslation(bound.Left - quadA.Left, bound.Top - quadA.Top));

            return matrix = matrix.PostConcat(a);
        }

        protected virtual SKRect GetDrawBox()
        {
            return Box;
        }

        public virtual SKRect GetViewBounds() => PageMatrix.MapRect(Box);

        public virtual SKRect GetViewBounds(SKMatrix viewMatrix)
        {
            if ((Flags & AnnotationFlagsEnum.NoZoom) == AnnotationFlagsEnum.NoZoom)
            {

            }
            return viewMatrix.PreConcat(PageMatrix).MapRect(Box);
        }

        public virtual void SetBounds(SKRect value) => MoveTo(InvertPageMatrix.MapRect(value));

        protected virtual void OnPropertyChanged<T>(T oldValue, T newValue, [CallerMemberName] string propertyName = "")
        {
            if ((queueRefresh & RefreshAppearanceState.Suspend) == RefreshAppearanceState.Suspend)
                return;
            PropertyChanged?.Invoke(this, new DetailedPropertyChangedEventArgs<T>(oldValue, newValue, propertyName));
            if (!string.Equals(propertyName, nameof(ModificationDate), StringComparison.Ordinal))
            {
                ModificationDate = DateTime.UtcNow;
            }
        }

        public override object Clone(Cloner cloner)
        {
            var cloned = (Annotation)base.Clone(cloner);
            cloned.page = null;
            cloned.cpBottomRight = null;
            cloned.cpBottomLeft = null;
            cloned.cpTopLeft = null;
            cloned.cpTopRight = null;
            return cloned;
        }

        public string GenerateName()
        {
            return Name = Guid.NewGuid().ToString();
        }

        public string GenerateExistingName(string key = null)
        {
            return Name = $"{GetType().Name}{Subject}{Page?.Index}{BaseObject.Reference?.ObjectNumber}{BaseObject.Reference?.GenerationNumber}{Author}{key}";
        }


        public virtual void RefreshBox()
        { }

        public virtual IEnumerable<ControlPoint> GetControlPoints()
        {
            yield break;
        }

        public IEnumerable<ControlPoint> GetDefaultControlPoint()
        {
            yield return cpTopLeft ??= new TopLeftControlPoint { Annotation = this };
            yield return cpTopRight ??= new TopRightControlPoint { Annotation = this };
            yield return cpBottomLeft ??= new BottomLeftControlPoint { Annotation = this };
            yield return cpBottomRight ??= new BottomRightControlPoint { Annotation = this };
        }

        public bool IsQueueRefreshAppearance => (queueRefresh | RefreshAppearanceState.Queued) == RefreshAppearanceState.Queued;

        public void QueueRefreshAppearance()
        {
            queueRefresh |= RefreshAppearanceState.Queued;
        }

        public void DequeueRefreshAppearance()
        {
            queueRefresh &= ~RefreshAppearanceState.Queued;
        }

        public void SuspendRefreshAppearance()
        {
            queueRefresh |= RefreshAppearanceState.Suspend;
        }

        public void ResumeRefreshAppearance()
        {
            queueRefresh &= ~RefreshAppearanceState.Suspend;
        }

        public void UserQueueRefreshAppearance()
        {
            queueRefresh |= RefreshAppearanceState.User;
        }

        
    }

    [Flags]
    internal enum RefreshAppearanceState
    {
        None = 0,
        Queued = 1,
        Process = 2,
        Move = 4,
        GenerateBox = 8,
        Suspend = 16,
        User = 32,
    }
}