/*
  Copyright 2015 Stefano Chizzolini. http://www.pdfclown.org

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

using System;
using System.Collections.Generic;
using SkiaSharp;

namespace PdfClown.Util.Math.Geom
{
    public static class Extension
    {
        public static float Cross(this SKPoint u, SKPoint v)
        {
            return u.X * v.Y - u.Y * v.X;
        }

        public static SKPoint GetPerp(this SKPoint a, float v, bool xbasis = true)
        {
            var b = xbasis
                ? SKPoint.Normalize(new SKPoint(a.Y == 0 ? 0 : v, a.Y == 0 ? v : -(a.X * v) / a.Y))
                : SKPoint.Normalize(new SKPoint(a.X == 0 ? v : -(a.Y * v) / a.X, a.X == 0 ? 0 : v));
            var abs = System.Math.Abs(v);
            return new SKPoint(b.X * abs, b.Y * abs);
        }

        public static void AddOpenArrow(this SKPath path, SKPoint point, SKPoint normal)
        {
            var matrix1 = SKMatrix.MakeRotationDegrees(35);
            var rotated1 = matrix1.MapVector(normal.X, normal.Y);
            var matrix2 = SKMatrix.MakeRotationDegrees(-35);
            var rotated2 = matrix2.MapVector(normal.X, normal.Y);

            path.MoveTo(point + new SKPoint(rotated1.X * 8, rotated1.Y * 8));
            path.LineTo(point);
            path.LineTo(point + new SKPoint(rotated2.X * 8, rotated2.Y * 8));
        }

        public static void AddCloseArrow(this SKPath path, SKPoint point, SKPoint normal)
        {
            var matrix1 = SKMatrix.MakeRotationDegrees(35);
            var rotated1 = matrix1.MapVector(normal.X, normal.Y);
            var matrix2 = SKMatrix.MakeRotationDegrees(-35);
            var rotated2 = matrix2.MapVector(normal.X, normal.Y);

            path.MoveTo(point + new SKPoint(rotated1.X * 8, rotated1.Y * 8));
            path.LineTo(point);
            path.LineTo(point + new SKPoint(rotated2.X * 8, rotated2.Y * 8));
            path.Close();
        }

        public static void Add(this ref SKRect rectangle, IEnumerable<SKPoint> points)
        {
            foreach (var point in points)
            {
                rectangle.Add(point);
            }
        }

        public static void Add(this ref SKRect rectangle, SKPoint[] points)
        {
            foreach (var point in points)
            {
                rectangle.Add(point);
            }
        }

        public static void Add(this ref SKRect rectangle, SKPoint point)
        {
            if (point.X < rectangle.Left)
            {
                rectangle.Left = point.X;
            }
            else if (point.X > rectangle.Right)
            {
                rectangle.Right = point.X;
            }
            if (point.Y < rectangle.Top)
            {
                rectangle.Top = point.Y;
            }
            else if (point.Y > rectangle.Bottom)
            {
                rectangle.Bottom = point.Y;
            }
        }

        public static SKPoint Center(this SKRect rectangle)
        {
            return new SKPoint(rectangle.CenterX(), rectangle.CenterY());
        }

        public static float CenterX(this SKRect rectangle)
        {
            return rectangle.Left + rectangle.Width / 2;
        }

        public static float CenterY(this SKRect rectangle)
        {
            return rectangle.Top + rectangle.Height / 2;
        }

        public static SKPath ToPath(this SKRect rectangle)
        {
            var path = new SKPath();
            path.AddRect(rectangle);
            return path;
        }

        //public static SKPoint Transform(
        //  this SKMatrix matrix,
        //  SKPoint point
        //  )
        //{
        //  var points = new SKPoint[]{point};
        //  matrix.MapPoints(points);
        //  return points[0];
        //}
    }
}

