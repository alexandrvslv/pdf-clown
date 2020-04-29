/*
 * https://github.com/apache/pdfbox
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
using System.Collections.Generic;
using System;
using SkiaSharp;
using System.Diagnostics;
using System.Linq;

namespace PdfClown.Documents.Contents.Fonts.Type1
{

    /**
     * This class represents and renders a Type 1 CharString.
     *
     * @author Villu Ruusmann
     * @author John Hewson
     */
    public class Type1CharString
    {
        private IType1CharStringReader font;
        private readonly string fontName;
        private readonly string glyphName;
        private SKPath path = null;
        private int width = 0;
        private SKPoint leftSideBearing;
        private SKPoint current;
        private bool isFlex = false;
        private readonly List<SKPoint> flexPoints = new List<SKPoint>();
        protected List<object> type1Sequence;
        protected int commandCount;

        /**
         * Constructs a new Type1CharString object.
         *
         * @param font Parent Type 1 CharString font.
         * @param fontName Name of the font.
         * @param glyphName Name of the glyph.
         * @param sequence Type 1 char string sequence
         */
        public Type1CharString(IType1CharStringReader font, string fontName, string glyphName, List<object> sequence)
            : this(font, fontName, glyphName)
        {
            type1Sequence = sequence;
        }

        /**
         * Constructor for use in subclasses.
         *
         * @param font Parent Type 1 CharString font.
         * @param fontName Name of the font.
         * @param glyphName Name of the glyph.
         */
        protected Type1CharString(IType1CharStringReader font, string fontName, string glyphName)
        {
            this.font = font;
            this.fontName = fontName;
            this.glyphName = glyphName;
            this.current = new SKPoint(0, 0);
        }

        // todo: NEW name (or CID as hex)
        public string Name
        {
            get => glyphName;
        }

        public string FontName
        {
            get => fontName;
        }

        /**
         * Returns the bounds of the renderer path.
         * @return the bounds as SKRect
         */
        public SKRect Bounds
        {
            get
            {
                if (path == null)
                {
                    Render();
                }
                return path.Bounds;
            }
        }

        /**
         * Returns the advance width of the glyph.
         * @return the width
         */
        public int Width
        {
            get
            {
                if (path == null)
                {
                    Render();
                }

                return width;
            }
        }

        /**
         * Returns the path of the character.
         * @return the path
         */
        public SKPath Path
        {
            get
            {
                if (path == null)
                {
                    Render();
                }

                return path;
            }
        }

        /**
         * Returns the Type 1 char string sequence.
         * @return the Type 1 sequence
         */
        public List<object> Type1Sequence => type1Sequence;

        /**
         * Renders the Type 1 char string sequence to a GeneralPath.
         */
        private void Render()
        {
            path = new SKPath() { FillType = SKPathFillType.EvenOdd };
            leftSideBearing = new SKPoint(0, 0);
            width = 0;
            CharStringHandler handler = new CharStringHandler();
            handler.HandleSequence(type1Sequence, RenderHandleCommand);
        }

        public List<float> RenderHandleCommand(List<float> numbers, CharStringCommand command)
        {
            commandCount++;
            CharStringCommand.TYPE1_VOCABULARY.TryGetValue(command.Key, out string name);

            switch (name)
            {
                case "rmoveto":
                    if (numbers.Count >= 2)
                    {
                        if (isFlex)
                        {
                            flexPoints.Add(new SKPoint(numbers[0], numbers[1]));
                        }
                        else
                        {
                            rmoveTo(numbers[0], numbers[1]);
                        }
                    }
                    break;
                case "vmoveto":
                    if (numbers.Count > 0)
                    {
                        if (isFlex)
                        {
                            // not in the Type 1 spec, but exists in some fonts
                            flexPoints.Add(new SKPoint(0f, numbers[0]));
                        }
                        else
                        {
                            rmoveTo(0, numbers[0]);
                        }
                    }
                    break;
                case "hmoveto":
                    if (numbers.Count > 0)
                    {
                        if (isFlex)
                        {
                            // not in the Type 1 spec, but exists in some fonts
                            flexPoints.Add(new SKPoint(numbers[0], 0f));
                        }
                        else
                        {
                            rmoveTo(numbers[0], 0);
                        }
                    }
                    break;
                case "rlineto":
                    if (numbers.Count >= 2)
                    {
                        rlineTo(numbers[0], numbers[1]);
                    }
                    break;
                case "hlineto":
                    if (numbers.Count > 0)
                    {
                        rlineTo(numbers[0], 0);
                    }
                    break;
                case "vlineto":
                    if (numbers.Count > 0)
                    {
                        rlineTo(0, numbers[0]);
                    }
                    break;
                case "rrcurveto":
                    if (numbers.Count >= 6)
                    {
                        rrcurveTo(numbers[0], numbers[1], numbers[2],
                                numbers[3], numbers[4], numbers[5]);
                    }
                    break;
                case "closepath":
                    closepath();
                    break;
                case "sbw":
                    if (numbers.Count >= 3)
                    {
                        leftSideBearing = new SKPoint(numbers[0], numbers[1]);
                        width = (int)numbers[2];
                        current = leftSideBearing;
                    }
                    break;
                case "hsbw":
                    if (numbers.Count >= 2)
                    {
                        leftSideBearing = new SKPoint(numbers[0], 0);
                        width = (int)numbers[1];
                        current = leftSideBearing;
                    }
                    break;
                case "vhcurveto":
                    if (numbers.Count >= 4)
                    {
                        rrcurveTo(0, numbers[0], numbers[1], numbers[2], numbers[3], 0);
                    }
                    break;
                case "hvcurveto":
                    if (numbers.Count >= 4)
                    {
                        rrcurveTo(numbers[0], 0, numbers[1], numbers[2], 0, numbers[3]);
                    }
                    break;
                case "seac":
                    if (numbers.Count >= 5)
                    {
                        seac(numbers[0], numbers[1], numbers[2], numbers[3], numbers[4]);
                    }
                    break;
                case "setcurrentpoint":
                    if (numbers.Count >= 2)
                    {
                        SetCurrentPoint(numbers[0], numbers[1]);
                    }
                    break;
                case "callothersubr":
                    if (numbers.Count > 0)
                    {
                        callothersubr((int)numbers[0]);
                    }
                    break;
                case "div":
                    float b = numbers[numbers.Count - 1];
                    float a = numbers[numbers.Count - 2];

                    float result = a / b;

                    List<float> list = new List<float>(numbers);
                    list.RemoveAt(list.Count - 1);
                    list.RemoveAt(list.Count - 1);
                    list.Add(result);
                    return list;
                case "hstem":
                case "vstem":
                case "hstem3":
                case "vstem3":
                case "dotsection":
                    // ignore hints
                    break;
                case "endchar":
                    // end
                    break;
                case "return":
                    // indicates an invalid charstring
                    Debug.WriteLine($"warn: Unexpected charstring command: {command.Key} in glyph {glyphName} of font {fontName}");
                    break;
                default:
                    if (name != null)
                    {
                        // indicates a PDFBox bug
                        throw new ArgumentException($"Unhandled command: {name}");
                    }
                    else
                    {
                        // indicates an invalid charstring
                        Debug.WriteLine($"warn: Unknown charstring command: {command.Key} in glyph {glyphName} of font {fontName}");
                    }
                    break;
            }
            return new List<float>();
        }

        /**
         * Sets the current absolute point without performing a moveto.
         * Used only with results from callothersubr
         */
        private void SetCurrentPoint(float x, float y)
        {
            current = new SKPoint(x, y);
        }

        /**
         * Flex (via OtherSubrs)
         * @param num OtherSubrs entry number
         */
        private void callothersubr(int num)
        {
            if (num == 0)
            {
                // end flex
                isFlex = false;

                if (flexPoints.Count < 7)
                {
                    Debug.WriteLine("warn: flex without moveTo in font " + fontName + ", glyph " + glyphName + ", command " + commandCount);
                    return;
                }

                // reference point is relative to start point
                SKPoint reference = flexPoints[0];
                reference = new SKPoint(current.X + reference.X,
                                      current.Y + reference.Y);

                // first point is relative to reference point
                SKPoint first = flexPoints[1];
                first = new SKPoint(reference.X + first.X, reference.Y + first.Y);

                // make the first point relative to the start point
                first = new SKPoint(first.X - current.X, first.Y - current.Y);

                rrcurveTo(flexPoints[1].X, flexPoints[1].Y,
                          flexPoints[2].X, flexPoints[2].Y,
                          flexPoints[3].X, flexPoints[3].Y);

                rrcurveTo(flexPoints[4].X, flexPoints[4].Y,
                          flexPoints[5].X, flexPoints[5].Y,
                          flexPoints[6].X, flexPoints[6].Y);

                flexPoints.Clear();
            }
            else if (num == 1)
            {
                // begin flex
                isFlex = true;
            }
            else
            {
                // indicates a PDFBox bug
                throw new ArgumentException("Unexpected other subroutine: " + num);
            }
        }

        /**
         * Relative moveto.
         */
        private void rmoveTo(float dx, float dy)
        {
            float x = (float)current.X + dx;
            float y = (float)current.Y + dy;
            path.MoveTo(x, y);
            current = new SKPoint(x, y);
        }

        /**
         * Relative lineto.
         */
        private void rlineTo(float dx, float dy)
        {
            float x = (float)current.X + dx;
            float y = (float)current.Y + dy;
            if (path.PointCount == 0)
            {
                Debug.WriteLine($"warn: rlineTo without initial moveTo in font {fontName}, glyph {glyphName}");
                path.MoveTo(x, y);
            }
            else
            {
                path.LineTo(x, y);
            }
            current = new SKPoint(x, y);
        }

        /**
         * Relative curveto.
         */
        private void rrcurveTo(float dx1, float dy1, float dx2, float dy2, float dx3, float dy3)
        {
            float x1 = (float)current.X + dx1;
            float y1 = (float)current.Y + dy1;
            float x2 = x1 + dx2;
            float y2 = y1 + dy2;
            float x3 = x2 + dx3;
            float y3 = y2 + dy3;
            if (path.PointCount == 0)
            {
                Debug.WriteLine($"warn: rrcurveTo without initial moveTo in font {fontName}, glyph {glyphName}");
                path.MoveTo(x1, y1);
            }

            {
                path.CubicTo(x1, y1, x2, y2, x3, y3);
            }
            current = new SKPoint(x3, y3);
        }

        /**
         * Close path.
         */
        private void closepath()
        {
            if (path.PointCount == 0)
            {
                Debug.WriteLine($"warn: closepath without initial moveTo in font {fontName}, glyph {glyphName}");
            }
            else
            {
                path.Close();
            }
            path.MoveTo(current.X, current.Y);
        }

        /**
         * Standard Encoding Accented Character
         *
         * Makes an accented character from two other characters.
         * @param asb
         */
        private void seac(float asb, float adx, float ady, float bchar, float achar)
        {
            // base character
            string baseName = StandardEncoding.Instance.GetName((int)bchar);
            try
            {
                Type1CharString baseString = font.GetType1CharString(baseName);
                path.AddPath(baseString.Path);
            }
            catch (Exception e)
            {
                Debug.WriteLine("warn: invalid seac character in glyph " + glyphName + " of font " + fontName, e);
            }
            // accent character
            string accentName = StandardEncoding.Instance.GetName((int)achar);
            try
            {
                Type1CharString accent = font.GetType1CharString(accentName);
                var at = SKMatrix.MakeTranslation(
                        leftSideBearing.X + adx - asb,
                        leftSideBearing.Y + ady);
                path.AddPath(accent.Path, ref at, SKPathAddMode.Append);
            }
            catch (Exception e)
            {
                Debug.WriteLine("warn: invalid seac character in glyph " + glyphName + " of font " + fontName, e);
            }
        }

        override public string ToString()
        {
            return string.Join("\n", type1Sequence.Select(p => p.ToString().Replace("|", "\n").Replace(",", " ")));
        }
    }
}
