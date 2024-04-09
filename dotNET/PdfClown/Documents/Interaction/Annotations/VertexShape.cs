/*
  Copyright 2008-2012 Stefano Chizzolini. http://www.pdfclown.org

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
using PdfClown.Objects;

using System;
using System.Collections.Generic;
using SkiaSharp;
using System.Linq;
using PdfClown.Util.Math.Geom;
using PdfClown.Documents.Interaction.Annotations.ControlPoints;

namespace PdfClown.Documents.Interaction.Annotations
{
    /**
      <summary>Abstract vertexed shape annotation.</summary>
    */
    [PDF(VersionEnum.PDF15)]
    public abstract class VertexShape : Shape
    {
        private SKPoint[] points;
        private Dictionary<int, IndexControlPoint> controlPoints = new Dictionary<int, IndexControlPoint>();

        protected VertexShape(PdfPage page, SKRect box, string text, PdfName subtype)
            : base(page, box, text, subtype)
        { }

        protected VertexShape(PdfDirectObject baseObject)
            : base(baseObject)
        { }

        /**
          <summary>Gets/Sets the coordinates of each vertex.</summary>
        */
        public SKPoint[] Points
        {
            get
            {
                if (points == null)
                {
                    PdfArray verticesObject = Vertices;

                    var length = verticesObject.Count;
                    points = new SKPoint[length / 2];
                    for (int i = 0, j = 0; i < length; i += 2, j++)
                    {
                        var mappedPoint = new SKPoint(
                            verticesObject.GetFloat(i),
                            verticesObject.GetFloat(i + 1));
                        points[j] = mappedPoint;
                    }
                }
                return points;
            }
            set
            {
                if (points != value)
                {
                    points = value;
                }
                PdfArray verticesObject = Vertices;
                verticesObject.Clear();
                float pageHeight = Page.Box.Height;
                foreach (SKPoint vertex in value)
                {
                    verticesObject.Add(PdfReal.Get(vertex.X));
                    verticesObject.Add(PdfReal.Get(vertex.Y));
                }
                Vertices = verticesObject;
            }
        }

        public PdfArray Vertices
        {
            get => (PdfArray)BaseDataObject.Get<PdfArray>(PdfName.Vertices);
            set
            {
                var oldValue = Vertices;
                //if (oldValue != value)
                {
                    BaseDataObject[PdfName.Vertices] = value;
                    OnPropertyChanged(oldValue, value);
                    QueueRefreshAppearance();
                }
            }
        }

        public SKPoint this[int index]
        {
            get => Points[index];
            set
            {
                Points[index] = value;
                Points = Points;
            }
        }

        public SKPoint FirstPoint
        {
            get => Points.Length == 0 ? SKPoint.Empty : points[0];
            set
            {
                if (Points.Length > 0)
                {
                    Points[0] = value;
                    Points = Points;
                }
            }
        }

        public SKPoint LastPoint
        {
            get => Points.Length == 0 ? SKPoint.Empty : points[points.Length - 1];
            set
            {
                if (Points.Length > 0)
                {
                    Points[points.Length - 1] = value;
                    Points = Points;
                }
            }
        }

        public IndexControlPoint FirstControlPoint => GetControlPoint(0);

        public IndexControlPoint LastControlPoint => GetControlPoint(Points.Length - 1);

        public bool ClosePath { get; internal set; }

        public IndexControlPoint GetControlPoint(int index)
        {
            return controlPoints.TryGetValue(index, out var controlPoint) ? controlPoint
                                : (controlPoints[index] = new IndexControlPoint { Annotation = this, Index = index });
        }

        public IndexControlPoint InsertPoint(int index, SKPoint point)
        {
            var oldVertices = Points;
            var newVertices = new SKPoint[oldVertices.Length + 1];

            Array.Copy(oldVertices, 0, newVertices, 0, index);
            newVertices[index] = point;
            if ((oldVertices.Length - 1) > index)
            {
                Array.Copy(oldVertices, index, newVertices, index + 1, (oldVertices.Length - 1) - index);
            }
            Points = newVertices;
            return GetControlPoint(index);
        }

        public IndexControlPoint AddPoint(SKPoint point)
        {
            return InsertPoint(Points.Length, point);
        }

        public bool RemovePoint(int index)
        {
            if (index > -1 && index < Points.Length)
            {
                var oldVertices = Points;
                var newVertices = new SKPoint[oldVertices.Length - 1];
                Array.Copy(oldVertices, 0, newVertices, 0, index);
                if ((oldVertices.Length - 1) > index)
                {
                    Array.Copy(oldVertices, index + 1, newVertices, index, (oldVertices.Length - 1) - index);
                }
                Points = newVertices;
                controlPoints.Remove(index);
                return true;
            }
            return false;
        }

        public override void RefreshBox()
        {
            if (!(Points?.Any() ?? false))
            {
                return;
            }
            SKRect box = SKRect.Empty;
            foreach (SKPoint point in Points)
            {
                if (box == SKRect.Empty)
                { box = SKRect.Create(point.X, point.Y, 10, 10); }
                else
                { box.Add(point); }
            }
            BorderEffect?.ApplyEffect(ref box);
            Box = box;
        }

        public override SKPath GetPath(SKMatrix sKMatrix)
        {
            var path = new SKPath();
            path.AddPoly(Points.ToArray(), ClosePath);
            path.Transform(sKMatrix);
            return path;
        }

        public override void MoveTo(SKRect newBox)
        {
            var oldBox = Box;
            //base.MoveTo(newBox);
            var dif = SKMatrix.CreateIdentity()
                .PreConcat(SKMatrix.CreateTranslation(newBox.MidX, newBox.MidY))
                .PreConcat(SKMatrix.CreateScale(newBox.Width / oldBox.Width, newBox.Height / oldBox.Height))
                .PreConcat(SKMatrix.CreateTranslation(-oldBox.MidX, -oldBox.MidY));
            for (int i = 0; i < Points.Length; i++)
            {
                points[i] = dif.MapPoint(points[i]);
            }
            Points = points;
            base.MoveTo(newBox);
        }

        public override IEnumerable<ControlPoint> GetControlPoints()
        {
            foreach (var cpBase in GetDefaultControlPoint())
            {
                yield return cpBase;
            }
            for (int i = 0; i < Points.Length; i++)
            {
                yield return GetControlPoint(i);
            }
        }

        public override object Clone(Cloner cloner)
        {
            var cloned = (VertexShape)base.Clone(cloner);
            cloned.controlPoints = new Dictionary<int, IndexControlPoint>();
            return cloned;
        }
    }

    public class IndexControlPoint : ControlPoint
    {
        public VertexShape VertexShape => (VertexShape)Annotation;

        public int Index { get; set; }

        public override SKPoint GetPoint() => VertexShape[Index];

        public override void SetPoint(SKPoint point)
        {
            base.SetPoint(point);
            VertexShape[Index] = point;
        }
    }
}