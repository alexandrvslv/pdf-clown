/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System.Diagnostics;
using SkiaSharp;
using System.Collections.Generic;

namespace PdfClown.Documents.Contents.Fonts.TTF
{

    /**
     * This class provides a glyph to SKPath conversion for true type fonts.
     * Based on code from Apache Batik, a subproject of Apache XMLGraphics.
     *
     * @see
     * <a href="http://xmlgraphics.apache.org/batik">http://xmlgraphics.apache.org/batik</a>
     * 
     * Contour rendering ported from PDF.js, viewed on 14.2.2015, rev 2e97c0d
     *
     * @see
     * <a href="https://github.com/mozilla/pdf.js/blob/c0d17013a28ee7aa048831560b6494a26c52360c/src/core/font_renderer.js">pdf.js/src/core/font_renderer.js</a>
     *
     */
    public class GlyphRenderer
    {
        //private static readonly Log LOG = LogFactory.getLog(GlyphRenderer.class);

        private GlyphDescription glyphDescription;

        public GlyphRenderer(GlyphDescription glyphDescription)
        {
            this.glyphDescription = glyphDescription;
        }

        /**
         * Returns the path of the glyph.
         * @return the path
         */
        public SKPath getPath()
        {
            Point[] points = Describe(glyphDescription);
            return CalculatePath(points);
        }

        /**
         * Set the points of a glyph from the GlyphDescription.
         */
        private Point[] Describe(GlyphDescription gd)
        {
            int endPtIndex = 0;
            int endPtOfContourIndex = -1;
            Point[] points = new Point[gd.PointCount];
            for (int i = 0; i < gd.PointCount; i++)
            {
                if (endPtOfContourIndex == -1)
                {
                    endPtOfContourIndex = gd.GetEndPtOfContours(endPtIndex);
                }
                bool endPt = endPtOfContourIndex == i;
                if (endPt)
                {
                    endPtIndex++;
                    endPtOfContourIndex = -1;
                }
                points[i] = new Point(gd.GetXCoordinate(i), gd.GetYCoordinate(i),
                        (gd.GetFlags(i) & GlyfDescript.ON_CURVE) != 0, endPt);
            }
            return points;
        }

        /**
         * Use the given points to calculate a SKPath.
         *
         * @param points the points to be used to generate the SKPath
         *
         * @return the calculated SKPath
         */
        private SKPath CalculatePath(Point[] points)
        {
            SKPath path = new SKPath();
            int start = 0;
            for (int p = 0, len = points.Length; p < len; ++p)
            {
                if (points[p].endOfContour)
                {
                    Point firstPoint = points[start];
                    Point lastPoint = points[p];
                    List<Point> contour = new List<Point>();
                    for (int q = start; q <= p; ++q)
                    {
                        contour.Add(points[q]);
                    }
                    if (points[start].onCurve)
                    {
                        // using start point at the contour end
                        contour.Add(firstPoint);
                    }
                    else if (points[p].onCurve)
                    {
                        // first is off-curve point, trying to use one from the end
                        contour.Insert(0, lastPoint);
                    }
                    else
                    {
                        // start and end are off-curve points, creating implicit one
                        Point pmid = MidValue(firstPoint, lastPoint);
                        contour.Insert(0, pmid);
                        contour.Add(pmid);
                    }
                    MoveTo(path, contour[0]);
                    for (int j = 1, clen = contour.Count; j < clen; j++)
                    {
                        Point pnow = contour[j];
                        if (pnow.onCurve)
                        {
                            LineTo(path, pnow);
                        }
                        else if (contour[j + 1].onCurve)
                        {
                            QuadTo(path, pnow, contour[j + 1]);
                            ++j;
                        }
                        else
                        {
                            QuadTo(path, pnow, MidValue(pnow, contour[j + 1]));
                        }
                    }
                    path.Close();
                    start = p + 1;
                }
            }
            return path;
        }

        private void MoveTo(SKPath path, Point point)
        {
            path.MoveTo(point.x, point.y);
#if DEBUG
            Debug.WriteLine("trace: moveTo: " + $"{point.x},{point.y}");
#endif
        }

        private void LineTo(SKPath path, Point point)
        {
            path.LineTo(point.x, point.y);
#if DEBUG
            Debug.WriteLine("trace: lineTo: " + $"{point.x},{point.y}");
#endif
        }

        private void QuadTo(SKPath path, Point ctrlPoint, Point point)
        {
            path.QuadTo(ctrlPoint.x, ctrlPoint.y, point.x, point.y);
#if DEBUG
            Debug.WriteLine("trace: quadTo: " + $"{ctrlPoint.x},{ctrlPoint.y} {point.x},{point.y}");
#endif
        }

        private int MidValue(int a, int b)
        {
            return a + (b - a) / 2;
        }

        // this creates an onCurve point that is between point1 and point2
        private Point MidValue(Point point1, Point point2)
        {
            return new Point(MidValue(point1.x, point2.x), MidValue(point1.y, point2.y));
        }

        /**
         * This class represents one point of a glyph.
         */
        private struct Point
        {
            internal int x;
            internal int y;
            internal bool onCurve;
            internal bool endOfContour;

            public Point(int xValue, int yValue, bool onCurveValue, bool endOfContourValue)
            {
                x = xValue;
                y = yValue;
                onCurve = onCurveValue;
                endOfContour = endOfContourValue;
            }

            // this constructs an on-curve, non-endofcountour point
            public Point(int xValue, int yValue)
                : this(xValue, yValue, true, false)
            {
            }


            public override string ToString()
            {
                return $"Point({x},{y},{(onCurve ? "onCurve" : "")},{(endOfContour ? "endOfContour" : "")})";
            }
        }

    }
}
