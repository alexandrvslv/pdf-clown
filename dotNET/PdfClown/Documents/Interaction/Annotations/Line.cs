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
using PdfClown.Documents.Contents.ColorSpaces;
using PdfClown.Objects;

using System;
using System.Collections.Generic;
using SkiaSharp;
using PdfClown.Util.Math.Geom;
using PdfClown.Documents.Interaction.Actions;
using PdfClown.Util;
using System.Net;
using PdfClown.Documents.Interaction.Annotations.ControlPoints;
using PdfClown.Documents.Contents.Composition;
using PdfClown.Documents.Contents.Fonts;
using PdfClown.Util.Math;
using PdfClown.Documents.Contents.XObjects;

namespace PdfClown.Documents.Interaction.Annotations
{
    /**
      <summary>Line annotation [PDF:1.6:8.4.5].</summary>
      <remarks>It displays displays a single straight line on the page.
      When opened, it displays a pop-up window containing the text of the associated note.</remarks>
    */
    [PDF(VersionEnum.PDF13)]
    public sealed class Line : Markup
    {
        private const int DefaultFontSize = 9;
        private static readonly double DefaultLeaderLineExtension = 0;
        private static readonly double DefaultLeaderLineLength = 0;
        private static readonly double DefaultLeaderLineOffset = 0;
        private static readonly LineEndStyleEnum DefaultLineEndStyle = LineEndStyleEnum.None;

        private SKPoint? captionOffset;
        private SKPoint? startPoint;
        private SKPoint? endPoint;

        private LineStartControlPoint cpStart;
        private LineEndControlPoint cpEnd;

        public Line(PdfPage page, SKPoint startPoint, SKPoint endPoint, string text, DeviceRGBColor color)
            : base(page, PdfName.Line, SKRect.Create(startPoint.X, startPoint.Y, endPoint.X - startPoint.X, endPoint.Y - startPoint.Y), text)
        {
            BaseDataObject[PdfName.L] = new PdfArray(4) { PdfReal.Get(0), PdfReal.Get(0), PdfReal.Get(0), PdfReal.Get(0) };
            StartPoint = page.InvertRotateMatrix.MapPoint(startPoint);
            EndPoint = page.InvertRotateMatrix.MapPoint(endPoint);
            Color = color;
        }

        public Line(PdfDirectObject baseObject) : base(baseObject)
        { }

        public override string Contents
        {
            get => base.Contents;
            set
            {
                if (base.Contents != value)
                {
                    base.Contents = value;
                    QueueRefreshAppearance();
                }
            }
        }

        /**
          <summary>Gets/Sets whether the contents should be shown as a caption.</summary>
        */
        [PDF(VersionEnum.PDF16)]
        public bool CaptionVisible
        {
            get => BaseDataObject.GetBool(PdfName.Cap);
            set
            {
                var oldValue = CaptionVisible;
                if (oldValue != value)
                {
                    BaseDataObject[PdfName.Cap] = PdfBoolean.Get(value);
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        [PDF(VersionEnum.PDF17)]
        public LineCaptionPosition? CaptionPosition
        {
            get => CaptionPositionExtension.Get((PdfName)BaseDataObject[PdfName.CP]);
            set
            {
                var oldValue = CaptionPosition;
                if (oldValue != value)
                {
                    BaseDataObject[PdfName.CP] = CaptionPositionExtension.GetName(value);
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        [PDF(VersionEnum.PDF17)]
        public SKPoint? CaptionOffset
        {
            get => captionOffset ??= BaseDataObject[PdfName.CO] is PdfArray offset
                    ? new SKPoint(offset.GetFloat(0), offset.GetFloat(1))
                    : new SKPoint(0F, 0F);
            set
            {
                var oldValue = CaptionOffset;
                if (oldValue != value)
                {
                    BaseDataObject[PdfName.CO] = value == null
                        ? null :
                        new PdfArray(2) { PdfReal.Get(value.Value.X), PdfReal.Get((float)value.Value.Y) };
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        /**
          <summary>Gets/Sets the style of the starting line ending.</summary>
        */
        [PDF(VersionEnum.PDF14)]
        public LineEndStyleEnum StartStyle
        {
            get => BaseDataObject[PdfName.LE] is PdfArray endstylesObject
                  ? LineEndStyleEnumExtension.Get(endstylesObject.GetString(0))
                  : DefaultLineEndStyle;
            set
            {
                var oldValue = StartStyle;
                if (oldValue != value)
                {
                    EnsureLineEndStylesObject().SetName(0, value.GetName());
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        /**
          <summary>Gets/Sets the style of the ending line ending.</summary>
        */
        [PDF(VersionEnum.PDF14)]
        public LineEndStyleEnum EndStyle
        {
            get => BaseDataObject[PdfName.LE] is PdfArray endstylesObject
                  ? LineEndStyleEnumExtension.Get(endstylesObject.GetString(1))
                  : DefaultLineEndStyle;
            set
            {
                var oldValue = EndStyle;
                if (oldValue != value)
                {
                    EnsureLineEndStylesObject().SetName(1, value.GetName());
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        /**
          <summary>Gets/Sets the length of leader line extensions that extend
          in the opposite direction from the leader lines.</summary>
        */
        [PDF(VersionEnum.PDF16)]
        public double LeaderLineExtension
        {
            get => BaseDataObject.GetDouble(PdfName.LLE, DefaultLeaderLineExtension);
            set
            {
                var oldValue = LeaderLineExtension;
                if (oldValue != value)
                {
                    BaseDataObject.SetDouble(PdfName.LLE, value);
                    /*
                      NOTE: If leader line extension entry is present, leader line MUST be too.
                    */
                    if (!BaseDataObject.ContainsKey(PdfName.LL))
                    {
                        LeaderLineLength = DefaultLeaderLineLength;
                    }
                    OnPropertyChanged(oldValue, value);
                    QueueRefreshAppearance();
                }
            }
        }

        [PDF(VersionEnum.PDF17)]
        public double LeaderLineOffset
        {
            get => BaseDataObject.GetDouble(PdfName.LLO, DefaultLeaderLineOffset);
            set
            {
                var oldValue = LeaderLineOffset;
                if (oldValue != value)
                {
                    BaseDataObject.SetDouble(PdfName.LLO, value);
                    /*
                      NOTE: If leader line extension entry is present, leader line MUST be too.
                    */
                    if (!BaseDataObject.ContainsKey(PdfName.LL))
                    {
                        LeaderLineLength = DefaultLeaderLineLength;
                    }
                    OnPropertyChanged(oldValue, value);
                    QueueRefreshAppearance();
                }
            }
        }

        /**
          <summary>Gets/Sets the length of leader lines that extend from each endpoint
          of the line perpendicular to the line itself.</summary>
          <remarks>A positive value means that the leader lines appear in the direction
          that is clockwise when traversing the line from its starting point
          to its ending point; a negative value indicates the opposite direction.</remarks>
        */
        [PDF(VersionEnum.PDF16)]
        public double LeaderLineLength
        {
            get => BaseDataObject.GetDouble(PdfName.LL, DefaultLeaderLineLength);
            set
            {
                var oldValue = LeaderLineOffset;
                if (oldValue != value)
                {
                    BaseDataObject.SetDouble(PdfName.LL, value);
                    OnPropertyChanged(oldValue, value);
                    QueueRefreshAppearance();
                }
            }
        }

        public PdfArray LineData
        {
            get => (PdfArray)BaseDataObject[PdfName.L];
            set
            {
                var oldValue = LineData;
                if (oldValue != value)
                {
                    BaseDataObject[PdfName.L] = value;
                    if (startPoint != null)
                    {
                        startPoint = null;
                        endPoint = null;
                        QueueRefreshAppearance();
                    }
                }
            }
        }

        /**
          <summary>Gets/Sets the starting coordinates.</summary>
        */
        public SKPoint StartPoint
        {
            get => startPoint ??= new SKPoint(LineData.GetFloat(0), LineData.GetFloat(1));
            set
            {
                var oldValue = StartPoint;
                if (oldValue != value)
                {
                    startPoint = value;
                    var coordinatesObject = LineData;
                    coordinatesObject.SetFloat(0, value.X);
                    coordinatesObject.SetFloat(1, value.Y);
                    OnPropertyChanged(coordinatesObject, coordinatesObject, nameof(LineData));
                    QueueRefreshAppearance();
                }
            }
        }


        /**
          <summary>Gets/Sets the ending coordinates.</summary>
        */
        public SKPoint EndPoint
        {
            get => endPoint ??= new SKPoint(LineData.GetFloat(2), LineData.GetFloat(3));
            set
            {
                var oldValue = EndPoint;
                if (oldValue != value)
                {
                    endPoint = value;
                    var coordinatesObject = LineData;
                    coordinatesObject.SetFloat(2, value.X);
                    coordinatesObject.SetFloat(3, value.Y);
                    QueueRefreshAppearance();
                    OnPropertyChanged(LineData, LineData, nameof(LineData));
                }
            }
        }

        public override bool ShowToolTip => !CaptionVisible;

        public override void MoveTo(SKRect newBox)
        {
            var oldBox = Box;
            if (oldBox.Width != newBox.Width
               || oldBox.Height != newBox.Height)
            {
                QueueRefreshAppearance();
            }
            var dif = SKMatrix.CreateIdentity()
                .PreConcat(SKMatrix.CreateTranslation(newBox.MidX, newBox.MidY))
                .PreConcat(SKMatrix.CreateScale(newBox.Width / oldBox.Width, newBox.Height / oldBox.Height))
                .PreConcat(SKMatrix.CreateTranslation(-oldBox.MidX, -oldBox.MidY));

            StartPoint = dif.MapPoint(StartPoint);
            EndPoint = dif.MapPoint(EndPoint);
            base.MoveTo(newBox);
        }

        private PdfArray EnsureLineEndStylesObject()
        {
            PdfArray endStylesObject = (PdfArray)BaseDataObject[PdfName.LE];
            if (endStylesObject == null)
            {
                BaseDataObject[PdfName.LE] = endStylesObject = new PdfArray(2)
                {
                      new PdfName(DefaultLineEndStyle.GetName()),
                      new PdfName(DefaultLineEndStyle.GetName())
                };
            }
            return endStylesObject;
        }

        protected override FormXObject GenerateAppearance()
        {
            var appearence = ResetAppearance(out var matrix);
            var paint = new PrimitiveComposer(appearence);
            paint.SetStrokeColor(Color ?? DeviceRGBColor.Default);
            paint.SetLineWidth(1);
            Border?.Apply(paint);
            paint.SetFillColor(InteriorColor ?? DeviceRGBColor.Default);


            var startPoint = matrix.MapPoint(StartPoint);
            var endPoint = matrix.MapPoint(EndPoint);
            var lineLength = SKPoint.Distance(startPoint, endPoint);
            var normal = SKPoint.Normalize(endPoint - startPoint);
            var invertNormal = new SKPoint(normal.X * -1, normal.Y * -1);
            if (CaptionVisible && !string.IsNullOrEmpty(Contents))
            {
                var fontName = appearence.GetDefaultFont(out var font);
                var textLength = (float)font.GetWidth(Contents, DefaultFontSize);

                var offset = (lineLength - textLength) / 2;

                var textLocation = startPoint + new SKPoint(normal.X * offset, normal.Y * offset);
                if (CaptionPosition == LineCaptionPosition.Inline)
                {
                    paint.StartPath(startPoint);
                    paint.DrawLine(startPoint + new SKPoint(normal.X * offset, normal.Y * offset));

                    paint.StartPath(endPoint);
                    paint.DrawLine(endPoint + new SKPoint(normal.X * -offset, normal.Y * -offset));
                }
                else
                {
                    paint.StartPath(startPoint);
                    paint.DrawLine(endPoint);
                }
                paint.Stroke();

                var horizontal = new SKPoint(1, 0);
                var theta = Math.Atan2(normal.X, normal.Y) - Math.Atan2(horizontal.X, horizontal.Y);

                while (theta <= -Math.PI)
                    theta += 2 * Math.PI;

                while (theta > Math.PI)
                    theta -= 2 * Math.PI;

                paint.BeginLocalState();
                paint.SetFillColor(DeviceRGBColor.Default);
                paint.SetFont(fontName, DefaultFontSize);
                paint.ShowText(Contents, textLocation, XAlignmentEnum.Left,
                    CaptionPosition == LineCaptionPosition.Inline ? YAlignmentEnum.Middle : YAlignmentEnum.Top,
                    MathUtils.ToDegrees(theta));
                paint.End();
            }
            else
            {
                paint.StartPath(startPoint);
                paint.DrawLine(endPoint);
                paint.Stroke();
            }
            if (StartStyle == LineEndStyleEnum.OpenArrow)
            {
                paint.AddOpenArrow(startPoint, normal);
                paint.Stroke();
            }
            else if (StartStyle == LineEndStyleEnum.ClosedArrow)
            {
                paint.AddClosedArrow(startPoint, normal);
                paint.FillStroke();
            }
            else if (StartStyle == LineEndStyleEnum.Circle)
            {
                paint.DrawCircle(startPoint, 4);
                paint.FillStroke();
            }

            if (EndStyle == LineEndStyleEnum.OpenArrow)
            {
                paint.AddOpenArrow(endPoint, invertNormal);
                paint.Stroke();
            }
            else if (EndStyle == LineEndStyleEnum.ClosedArrow)
            {
                paint.AddClosedArrow(endPoint, invertNormal);
                paint.FillStroke();
            }
            else if (EndStyle == LineEndStyleEnum.Circle)
            {
                paint.DrawCircle(endPoint, 4);
                paint.FillStroke();
            }

            paint.Flush();
            return appearence;
        }

        public override void RefreshBox()
        {
            if (LineData == null)
                return;
            var box = SKRect.Create(StartPoint, SKSize.Empty);
            box.Add(EndPoint);
            box.Inflate(box.Width < 5 ? 5 : 0, box.Height < 5 ? 5 : 0);
            Box = box;
        }

        public override IEnumerable<ControlPoint> GetControlPoints()
        {
            yield return cpStart ??= new LineStartControlPoint { Annotation = this };
            yield return cpEnd ??= new LineEndControlPoint { Annotation = this };
        }

        public override object Clone(Cloner cloner)
        {
            var cloned = (Line)base.Clone(cloner);
            cloned.cpStart = null;
            cloned.cpEnd = null;
            return cloned;
        }

    }

    public enum LineCaptionPosition
    {
        Inline,
        Top
    }

    public static class CaptionPositionExtension
    {
        private static readonly BiDictionary<LineCaptionPosition, PdfName> codes;

        static CaptionPositionExtension()
        {
            codes = new BiDictionary<LineCaptionPosition, PdfName>
            {
                [LineCaptionPosition.Inline] = PdfName.Inline,
                [LineCaptionPosition.Top] = PdfName.Top
            };
        }

        public static LineCaptionPosition? Get(PdfName name)
        {
            if (name == null)
                return LineCaptionPosition.Inline;

            return codes.GetKey(name);
        }

        public static PdfName GetName(this LineCaptionPosition? type)
        {
            return type == null ? null : codes[type.Value];
        }
    }

    public abstract class LineControlPoint : ControlPoint
    {
        public Line Line => (Line)Annotation;
    }

    public class LineStartControlPoint : LineControlPoint
    {
        
        public override SKPoint GetPoint() => Line.StartPoint;

        public override void SetPoint(SKPoint point)
        {
            base.SetPoint(point);
            Line.StartPoint = point;
        }
    }

    public class LineEndControlPoint : LineControlPoint
    {
        public override SKPoint GetPoint() => Line.EndPoint;
        public override void SetPoint(SKPoint point)
        {
            base.SetPoint(point);
            Line.EndPoint = point;
        }
    }
}