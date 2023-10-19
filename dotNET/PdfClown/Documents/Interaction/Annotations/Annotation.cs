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
//using System.Diagnostics;

namespace PdfClown.Documents.Interaction.Annotations
{
    /**
      <summary>Annotation [PDF:1.6:8.4].</summary>
    */
    [PDF(VersionEnum.PDF10)]
    public abstract class Annotation : PdfObjectWrapper<PdfDictionary>, ILayerable, INotifyPropertyChanged
    {
        private Page page;
        private string name;
        private SKColor? color;
        private SKRect? boxCache;
        protected BottomRightControlPoint cpBottomRight;
        protected BottomLeftControlPoint cpBottomLeft;
        protected TopRightControlPoint cpTopRight;
        protected TopLeftControlPoint cpTopLeft;

        public event PropertyChangedEventHandler PropertyChanged;

        #region static
        #region interface
        #region public
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

        #endregion
        #endregion
        #endregion

        #region dynamic
        #region constructors
        protected Annotation(Page page, PdfName subtype, SKRect box, string text)
            : base(
                  page.Document,
                  new PdfDictionary(
                      new PdfName[] { PdfName.Type, PdfName.Subtype, PdfName.Border },
                      new PdfDirectObject[]
                      {
                          PdfName.Annot,
                          subtype,
                          new PdfArray(new PdfDirectObject[]{PdfInteger.Default, PdfInteger.Default, PdfInteger.Default}) // NOTE: Hide border by default.
                      }
                      )
                  )
        {
            GenerateName();
            Page = page;
            Box = box;
            Contents = text;
            Printable = true;
            IsNew = true;
        }

        public Annotation(PdfDirectObject baseObject) : base(baseObject)
        { }
        #endregion

        #region interface
        #region public
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
            get => Wrap<Border>(BaseDataObject.Get<PdfDictionary>(PdfName.BS));
            set
            {
                var oldValue = Border;
                if (!(oldValue?.Equals(value) ?? value == null))
                {
                    BaseDataObject[PdfName.BS] = PdfObjectWrapper.GetBaseObject(value);
                    if (value != null)
                    { BaseDataObject.Remove(PdfName.Border); }
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        /**
          <summary>Gets/Sets the location of the annotation on the page in default user space units.
          </summary>
        */
        public virtual SKRect Box
        {
            get => boxCache ?? (boxCache = PageMatrix.MapRect(Rect?.ToRect() ?? SKRect.Empty)).Value;
            set
            {
                var oldValue = Box;
                boxCache = new SKRect((float)Math.Round(value.Left, 4),
                                      (float)Math.Round(value.Top, 4),
                                      (float)Math.Round(value.Right, 4),
                                      (float)Math.Round(value.Bottom, 4));
                if (!oldValue.Equals(boxCache.Value))
                {
                    Rect = new Objects.Rectangle(InvertPageMatrix.MapRect(boxCache.Value));
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
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        public virtual SKColor SKColor
        {
            get => color ?? (color = Color == null ? SKColors.Transparent : DeviceColorSpace.CalcSKColor(Color, Alpha)).Value;
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
            get => BaseDataObject.GetDate(PdfName.M);
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
            get => name ?? (name = BaseDataObject.GetString(PdfName.NM) ?? string.Empty);
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
        public virtual Page Page
        {
            get => page ?? (page = Wrap<Page>(BaseDataObject[PdfName.P]));
            set
            {
                page = null;
                var oldPage = Page;
                if (oldPage == value)
                {
                    return;
                }
                boxCache = null;
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

        #region ILayerable
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
        #endregion

        protected SKMatrix PageMatrix
        {
            get => Page?.RotateMatrix ?? GraphicsState.GetRotationMatrix(SKRect.Create(Document.GetSize()), 0);
        }

        protected SKMatrix InvertPageMatrix
        {
            get => Page?.InRotateMatrix ?? (GraphicsState.GetRotationMatrix(SKRect.Create(Document.GetSize()), 0).TryInvert(out var inverted) ? inverted : SKMatrix.Identity);
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


        public virtual bool ShowToolTip => true;

        public virtual bool AllowDrag => true;

        public virtual bool AllowSize => true;

        public bool IsNew { get; set; }

        public List<Annotation> Replies { get; set; } = new List<Annotation>();


        #endregion

        #region private
        protected RotationEnum GetPageRotation()
        {
            return Page?.Rotation ?? RotationEnum.Downward;
        }

        public void Draw(SKCanvas canvas)
        {
            var appearance = Appearance.Normal[null];
            if (appearance != null)
            {
                DrawAppearance(canvas, appearance);
            }
            else
            {
                DrawSpecial(canvas);
            }
        }

        public virtual void DrawSpecial(SKCanvas canvas)
        {

        }

        protected virtual void DrawAppearance(SKCanvas canvas, FormXObject appearance)
        {
            var bounds = Rect.ToRect();
            var appearanceBounds = appearance.Box;
            var picture = appearance.Render();

            var matrix = appearance.Matrix;

            var quad = new Quad(appearanceBounds);
            quad.Transform(ref matrix);

            var a = SKMatrix.CreateScale(bounds.Width / quad.HorizontalLength, bounds.Height / quad.VerticalLenght);
            var quadA = Quad.Transform(quad, ref a);
            a = a.PostConcat(SKMatrix.CreateTranslation(bounds.Left - quadA.Left, bounds.Top - quadA.Top));

            matrix = matrix.PostConcat(a);

            var self = PageMatrix;
            canvas.Save();
            canvas.Concat(ref self);

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
            canvas.Restore();
        }

        public virtual SKRect GetBounds(SKMatrix matrix)
        {
            var box = Box;
            return matrix.MapRect(box);
        }

        protected void OnPropertyChanged(object oldValue, object newValue, [CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new DetailedPropertyChangedEventArgs(oldValue, newValue, propertyName));
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

        public void GenerateName()
        {
            Name = Guid.NewGuid().ToString();
        }

        public void GenerateExistingName()
        {
            Name = $"Annot{Dictionary[PdfName.Subtype]}{Page?.Index}{BaseObject.Reference?.ObjectNumber}{BaseObject.Reference?.GenerationNumber}{Author}";
        }


        public virtual void RefreshBox()
        { }

        public virtual IEnumerable<ControlPoint> GetControlPoints()
        {
            yield break;
        }

        public IEnumerable<ControlPoint> GetDefaultControlPoint()
        {
            yield return cpTopLeft ?? (cpTopLeft = new TopLeftControlPoint { Annotation = this });
            yield return cpTopRight ?? (cpTopRight = new TopRightControlPoint { Annotation = this });
            yield return cpBottomLeft ?? (cpBottomLeft = new BottomLeftControlPoint { Annotation = this });
            yield return cpBottomRight ?? (cpBottomRight = new BottomRightControlPoint { Annotation = this });
        }
        #endregion
        #endregion
        #endregion
    }

    /**
      <summary>Annotation flags [PDF:1.6:8.4.2].</summary>
    */
    [Flags]
    public enum AnnotationFlagsEnum
    {
        /**
          <summary>Hide the annotation, both on screen and on print,
          if it does not belong to one of the standard annotation types
          and no annotation handler is available.</summary>
        */
        Invisible = 0x1,
        /**
          <summary>Hide the annotation, both on screen and on print
          (regardless of its annotation type or whether an annotation handler is available).</summary>
        */
        Hidden = 0x2,
        /**
          <summary>Print the annotation when the page is printed.</summary>
        */
        Print = 0x4,
        /**
          <summary>Do not scale the annotation's appearance to match the magnification of the page.</summary>
        */
        NoZoom = 0x8,
        /**
          <summary>Do not rotate the annotation's appearance to match the rotation of the page.</summary>
        */
        NoRotate = 0x10,
        /**
          <summary>Hide the annotation on the screen.</summary>
        */
        NoView = 0x20,
        /**
          <summary>Do not allow the annotation to interact with the user.</summary>
        */
        ReadOnly = 0x40,
        /**
          <summary>Do not allow the annotation to be deleted or its properties to be modified by the user.</summary>
        */
        Locked = 0x80,
        /**
          <summary>Invert the interpretation of the NoView flag.</summary>
        */
        ToggleNoView = 0x100,
        /**
          <summary>Do not allow the contents of the annotation to be modified by the user.</summary>
        */
        LockedContents = 0x200
    }


    public class DetailedPropertyChangedEventArgs : PropertyChangedEventArgs
    {
        public DetailedPropertyChangedEventArgs(object oldValue, object newValue, string propertyName)
            : base(propertyName)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }

        public object OldValue { get; }
        public object NewValue { get; }
    }

    public abstract class ControlPoint
    {
        private const int r = 3;

        public Annotation Annotation { get; set; }
        public abstract SKPoint Point { get; set; }
        public SKRect Bounds
        {
            get
            {
                var point = Point;
                return new SKRect(point.X - r, point.Y - r, point.X + r, point.Y + r);
            }
        }

        public virtual ControlPoint Clone(Annotation annotation)
        {
            var cloned = (ControlPoint)this.MemberwiseClone();
            cloned.Annotation = annotation;
            return cloned;
        }
    }

    public class TopLeftControlPoint : ControlPoint
    {
        public override SKPoint Point
        {
            get => Annotation.TopLeftPoint;
            set => Annotation.TopLeftPoint = value;
        }
    }

    public class TopRightControlPoint : ControlPoint
    {
        public override SKPoint Point
        {
            get => Annotation.TopRightPoint;
            set => Annotation.TopRightPoint = value;
        }
    }

    public class BottomLeftControlPoint : ControlPoint
    {
        public override SKPoint Point
        {
            get => Annotation.BottomLeftPoint;
            set => Annotation.BottomLeftPoint = value;
        }
    }

    public class BottomRightControlPoint : ControlPoint
    {
        public override SKPoint Point
        {
            get => Annotation.BottomRightPoint;
            set => Annotation.BottomRightPoint = value;
        }
    }

}