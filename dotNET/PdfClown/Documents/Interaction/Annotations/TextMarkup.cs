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
using PdfClown.Documents.Contents.Composition;
using PdfClown.Documents.Contents.XObjects;
using PdfClown.Objects;
using PdfClown.Util.Math.Geom;

using System;
using System.Collections.Generic;
using System.Linq;
using SkiaSharp;

namespace PdfClown.Documents.Interaction.Annotations
{
    /**
      <summary>Text markup annotation [PDF:1.6:8.4.5].</summary>
      <remarks>It displays highlights, underlines, strikeouts, or jagged ("squiggly") underlines in
      the text of a document.</remarks>
    */
    [PDF(VersionEnum.PDF13)]
    public sealed class TextMarkup : Markup
    {

        #region static
        #region fields
        private static readonly Dictionary<TextMarkupType, PdfName> MarkupTypeEnumCodes;
        #endregion

        #region constructors
        static TextMarkup()
        {
            MarkupTypeEnumCodes = new Dictionary<TextMarkupType, PdfName>
            {
                [TextMarkupType.Highlight] = PdfName.Highlight,
                [TextMarkupType.Squiggly] = PdfName.Squiggly,
                [TextMarkupType.StrikeOut] = PdfName.StrikeOut,
                [TextMarkupType.Underline] = PdfName.Underline
            };
        }
        #endregion

        #region interface
        #region private
        /**
          <summary>Gets the code corresponding to the given value.</summary>
        */
        private static PdfName ToCode(TextMarkupType value)
        {
            return MarkupTypeEnumCodes[value];
        }

        /**
          <summary>Gets the markup type corresponding to the given value.</summary>
        */
        private static TextMarkupType ToMarkupTypeEnum(IPdfString value)
        {
            if (value == null)
                throw new Exception("Invalid markup type.");
            foreach (KeyValuePair<TextMarkupType, PdfName> markupType in MarkupTypeEnumCodes)
            {
                if (string.Equals(markupType.Value.StringValue, value.StringValue, StringComparison.Ordinal))
                    return markupType.Key;
            }
            throw new Exception("Invalid markup type.");
        }
        #endregion
        #endregion
        #endregion

        #region static
        #region fields
        private static readonly PdfName HighlightExtGStateName = new PdfName("highlight");
        private List<Quad> markupBoxes = new List<Quad>();
        #endregion

        #region interface
        private static float GetMarkupBoxMargin(float boxHeight)
        {
            return boxHeight * .25f;
        }
        #endregion
        #endregion

        #region dynamic
        #region constructors
        /**
          <summary>Creates a new text markup on the specified page, making it printable by default.
          </summary>
          <param name="page">Page to annotate.</param>
          <param name="markupBox">Quadrilateral encompassing a word or group of contiguous words in the
          text underlying the annotation.</param>
          <param name="text">Annotation text.</param>
          <param name="markupType">Markup type.</param>
        */
        public TextMarkup(Page page, Quad markupBox, string text, TextMarkupType markupType)
            : this(page, new List<Quad>() { markupBox }, text, markupType)
        { }

        /**
          <summary>Creates a new text markup on the specified page, making it printable by default.
          </summary>
          <param name="page">Page to annotate.</param>
          <param name="markupBoxes">Quadrilaterals encompassing a word or group of contiguous words in
          the text underlying the annotation.</param>
          <param name="text">Annotation text.</param>
          <param name="markupType">Markup type.</param>
        */
        public TextMarkup(Page page, IList<Quad> markupBoxes, string text, TextMarkupType markupType)
            : base(page, ToCode(markupType), markupBoxes[0].GetBounds(), text)
        {
            MarkupType = markupType;
            MarkupBoxes = markupBoxes;
            Printable = true;
        }

        public TextMarkup(PdfDirectObject baseObject) : base(baseObject)
        { }
        #endregion

        #region interface
        #region public
        public override DeviceColor Color
        {
            set
            {
                base.Color = value;
                if (MarkupBoxes.Count > 0)
                {
                    RefreshAppearance();
                }
            }
        }

        public override Page Page
        {
            get => base.Page;
            set
            {
                base.Page = value;
                if (MarkupBoxes.Count > 0)
                {
                    RefreshAppearance();
                }
            }
        }

        public PdfArray QuadPoints
        {
            get => (PdfArray)BaseDataObject[PdfName.QuadPoints];
            set
            {
                var oldValue = QuadPoints;
                if (oldValue != value)
                {
                    BaseDataObject[PdfName.QuadPoints] = value;
                    markupBoxes.Clear();
                    OnPropertyChanged(oldValue, value);
                    RefreshAppearance();
                }
            }
        }
        /**
          <summary>Gets/Sets the quadrilaterals encompassing a word or group of contiguous words in the
          text underlying the annotation.</summary>
        */
        public IList<Quad> MarkupBoxes
        {
            get
            {

                var quadPoints = QuadPoints;
                if (quadPoints != null)
                {
                    var length = quadPoints.Count;
                    if (markupBoxes.Count * 8 != length)
                    {
                        markupBoxes.Clear();
                        var pageMatrix = PageMatrix;

                        for (int index = 0; index < length; index += 8)
                        {
                            /*
                              NOTE: Despite the spec prescription, point 3 and point 4 MUST be inverted.
                            */
                            var quad = new Quad(
                                new SKPoint(quadPoints.GetFloat(index), quadPoints.GetFloat(index + 1)),
                                new SKPoint(quadPoints.GetFloat(index + 2), quadPoints.GetFloat(index + 3)),
                                new SKPoint(quadPoints.GetFloat(index + 6), quadPoints.GetFloat(index + 7)),
                                new SKPoint(quadPoints.GetFloat(index + 4), quadPoints.GetFloat(index + 5)));
                            quad.Transform(ref pageMatrix);
                            markupBoxes.Add(quad);
                        }
                    }
                }
                return markupBoxes;
            }
            set
            {
                var quadPoints = new PdfArray();
                var pageMatrix = InvertPageMatrix;
                SKRect box = SKRect.Empty;
                foreach (Quad markupBox in value)
                {
                    /*
                      NOTE: Despite the spec prescription, point 3 and point 4 MUST be inverted.
                    */
                    SKPoint[] points = pageMatrix.MapPoints(markupBox.GetPoints());

                    quadPoints.Add(PdfReal.Get(points[0].X)); // x1.
                    quadPoints.Add(PdfReal.Get(points[0].Y)); // y1.
                    quadPoints.Add(PdfReal.Get(points[1].X)); // x2.
                    quadPoints.Add(PdfReal.Get(points[1].Y)); // y2.
                    quadPoints.Add(PdfReal.Get(points[3].X)); // x4.
                    quadPoints.Add(PdfReal.Get(points[3].Y)); // y4.
                    quadPoints.Add(PdfReal.Get(points[2].X)); // x3.
                    quadPoints.Add(PdfReal.Get(points[2].Y)); // y3.
                    if (box.IsEmpty)
                    { box = markupBox.GetBounds(); }
                    else
                    { box = SKRect.Union(box, markupBox.GetBounds()); }
                }

                /*
                  NOTE: Box width is expanded to make room for end decorations (e.g. rounded highlight caps).
                */
                float markupBoxMargin = GetMarkupBoxMargin(box.Height);
                box.Inflate(markupBoxMargin, 0);
                Box = box;

                QuadPoints = quadPoints;
            }
        }

        /**
          <summary>Gets/Sets the markup type.</summary>
        */
        public TextMarkupType MarkupType
        {
            get => ToMarkupTypeEnum((IPdfString)BaseDataObject[PdfName.Subtype]);
            set
            {
                BaseDataObject[PdfName.Subtype] = ToCode(value);
                switch (value)
                {
                    case TextMarkupType.Highlight:
                        Color = new DeviceRGBColor(1, 1, 0);
                        break;
                    case TextMarkupType.Squiggly:
                        Color = new DeviceRGBColor(1, 0, 0);
                        break;
                    default:
                        Color = new DeviceRGBColor(0, 0, 0);
                        break;
                }
            }
        }
        #endregion

        #region private
        /*
          TODO: refresh should happen for every Annotation with overwrite\remove check 
        */
        protected override void RefreshAppearance()
        {
            if (Page == null)
            {
                return;
            }
            if (Rect == null)
            {
                MarkupBoxes = MarkupBoxes;
                return;
            }
            SKRect box = Rect.ToRect();
            FormXObject normalAppearance = ResetAppearance(box);
            normalAppearance.Matrix = SKMatrix.CreateTranslation(box.Left, box.Top);
            PrimitiveComposer composer = new PrimitiveComposer(normalAppearance);
            {
                var first = MarkupBoxes.FirstOrDefault();
                var matrix = SKMatrix.CreateTranslation(-box.Left, -box.Top).PreConcat(InvertPageMatrix);

                TextMarkupType markupType = MarkupType;
                switch (markupType)
                {
                    case TextMarkupType.Highlight:
                        {
                            ExtGState defaultExtGState;
                            {
                                ExtGStateResources extGStates = normalAppearance.Resources.ExtGStates;
                                defaultExtGState = extGStates[HighlightExtGStateName];
                                if (defaultExtGState == null)
                                {
                                    if (extGStates.Count > 0)
                                    { extGStates.Clear(); }

                                    extGStates[HighlightExtGStateName] = defaultExtGState = new ExtGState(Document);
                                    defaultExtGState.AlphaShape = false;
                                    defaultExtGState.BlendMode = new List<BlendModeEnum>(new BlendModeEnum[] { BlendModeEnum.Multiply });
                                }
                            }

                            composer.ApplyState(defaultExtGState);
                            composer.SetFillColor(Color);
                            {
                                foreach (Quad markup in MarkupBoxes)
                                {
                                    var markupBox = Quad.Transform(markup, ref matrix);

                                    var sign = Math.Sign((markupBox.TopLeft - markupBox.BottomLeft).Y);
                                    sign = sign == 0 ? 1 : sign;

                                    float markupBoxHeight = markupBox.Height;
                                    float markupBoxMargin = GetMarkupBoxMargin(markupBoxHeight) * sign;

                                    composer.DrawCurve(
                                      markupBox.BottomLeft,
                                      markupBox.TopLeft,
                                      GetMarginPoint(markupBox.TopLeft, markupBox.BottomLeft, -markupBoxMargin),
                                      GetMarginPoint(markupBox.BottomLeft, markupBox.TopLeft, -markupBoxMargin));
                                    composer.DrawLine(markupBox.TopRight);
                                    composer.DrawCurve(
                                      markupBox.BottomRight,
                                      GetMarginPoint(markupBox.BottomRight, markupBox.TopRight, markupBoxMargin),
                                      GetMarginPoint(markupBox.TopRight, markupBox.BottomRight, markupBoxMargin));
                                    composer.Fill();
                                }
                            }
                        }
                        break;
                    case TextMarkupType.Squiggly:
                        {
                            composer.SetStrokeColor(Color);
                            composer.SetLineCap(LineCapEnum.Round);
                            composer.SetLineJoin(LineJoinEnum.Round);
                            {
                                foreach (Quad markup in MarkupBoxes)
                                {
                                    var markupBox = Quad.Transform(markup, ref matrix);
                                    var sign = Math.Sign((markupBox.TopLeft - markupBox.BottomLeft).Y);
                                    sign = sign == 0 ? 1 : sign;

                                    float markupBoxHeight = markupBox.Height;
                                    float markupBoxWidth = markupBox.Width;
                                    float lineWidth = markupBoxHeight * .06f;
                                    float step = markupBoxHeight * .125f;
                                    float length = (float)Math.Sqrt(Math.Pow(step, 2) * 2);
                                    var bottomUp = SKPoint.Normalize(markupBox.BottomLeft - markupBox.TopLeft);
                                    bottomUp = new SKPoint(bottomUp.X * lineWidth, bottomUp.Y * lineWidth);
                                    var startPoint = markupBox.TopLeft + (new SKPoint(bottomUp.X * 2, bottomUp.Y * 2));
                                    var leftRight = SKPoint.Normalize(markupBox.BottomRight - markupBox.BottomLeft);
                                    leftRight = new SKPoint(leftRight.X * step, leftRight.Y * step);
                                    var leftRightPerp = leftRight.GetPerp(step * sign);
                                    var stepPoint = startPoint + leftRight + leftRightPerp;
                                    bool phase = true;
                                    composer.SetLineWidth(lineWidth);
                                    composer.StartPath(startPoint);
                                    startPoint += leftRight;
                                    var x = 0F;
                                    while (x < markupBoxWidth)
                                    {
                                        composer.DrawLine(phase ? stepPoint : startPoint);
                                        startPoint += leftRight;
                                        stepPoint += leftRight;
                                        x += step;
                                        phase = !phase;
                                    }
                                }
                                composer.Stroke();
                            }
                        }
                        break;
                    case TextMarkupType.StrikeOut:
                    case TextMarkupType.Underline:
                        {
                            composer.SetStrokeColor(Color);
                            {
                                float lineYRatio = 0;
                                switch (markupType)
                                {
                                    case TextMarkupType.StrikeOut:
                                        lineYRatio = .5f;
                                        break;
                                    case TextMarkupType.Underline:
                                        lineYRatio = .9f;
                                        break;
                                    default:
                                        throw new NotImplementedException();
                                }
                                foreach (Quad markup in MarkupBoxes)
                                {
                                    var markupBox = Quad.Transform(markup, ref matrix);
                                    float markupBoxHeight = markupBox.Height;
                                    float boxYOffset = markupBoxHeight * lineYRatio;
                                    var normal = SKPoint.Normalize(markupBox.TopLeft - markupBox.BottomLeft);
                                    normal = new SKPoint(normal.X * boxYOffset, normal.Y * boxYOffset);
                                    composer.SetLineWidth(markupBoxHeight * .065);
                                    composer.DrawLine(markupBox.BottomLeft + normal, markupBox.BottomRight + normal);
                                }
                                composer.Stroke();
                            }
                        }
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
            composer.Flush();
        }

        private static SKPoint GetMarginPoint(SKPoint pointA, SKPoint pointB, float margin)
        {
            float absMargin = Math.Abs(margin);
            var normal = SKPoint.Normalize(pointA - pointB);
            normal = new SKPoint(normal.X * absMargin, normal.Y * absMargin);
            var perp = normal.GetPerp(margin);
            return (pointB + normal) + perp;
        }

        #endregion
        #endregion
        #endregion
    }

    /**
      <summary>Markup type [PDF:1.6:8.4.5].</summary>
    */
    public enum TextMarkupType
    {
        /**
          <summary>Highlight.</summary>
        */
        [PDF(VersionEnum.PDF13)]
        Highlight,
        /**
          <summary>Squiggly.</summary>
        */
        [PDF(VersionEnum.PDF14)]
        Squiggly,
        /**
          <summary>StrikeOut.</summary>
        */
        [PDF(VersionEnum.PDF13)]
        StrikeOut,
        /**
          <summary>Underline.</summary>
        */
        [PDF(VersionEnum.PDF13)]
        Underline
    };
}