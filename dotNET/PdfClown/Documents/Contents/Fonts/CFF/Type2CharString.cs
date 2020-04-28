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
using PdfClown.Documents.Contents.Fonts.Type1;
using System;
using System.Linq;
using System.Diagnostics;

namespace PdfClown.Documents.Contents.Fonts.CCF
{
    /**
     * Represents a Type 2 CharString by converting it into an equivalent Type 1 CharString.
     * 
     * @author Villu Ruusmann
     * @author John Hewson
     */
    public class Type2CharString : Type1CharString
    {
        private float defWidthX = 0;
        private float nominalWidthX = 0;
        private int pathCount = 0;
        private readonly List<object> type2sequence;
        private readonly int gid;

        /**
		 * Constructor.
		 * @param font Parent CFF font
		 * @param fontName font name
		 * @param glyphName glyph name (or CID as hex string)
		 * @param gid GID
		 * @param sequence Type 2 char string sequence
		 * @param defaultWidthX default width
		 * @param nomWidthX nominal width
		 */
        public Type2CharString(IType1CharStringReader font, string fontName, string glyphName, int gid, List<object> sequence,
                               int defaultWidthX, int nomWidthX)
            : base(font, fontName, glyphName)
        {

            this.gid = gid;
            type2sequence = sequence;
            defWidthX = defaultWidthX;
            nominalWidthX = nomWidthX;
            ConvertType1ToType2(sequence);
        }

        /**
		 * Return the GID (glyph id) of this charstring.
		 */
        public int GID
        {
            get => gid;
        }

        /**
		 * Returns the Type 2 charstring sequence.
		 */
        public List<object> Type2Sequence
        {
            get => type2sequence;
        }

        /**
		 * Converts a sequence of Type 2 commands into a sequence of Type 1 commands.
		 * @param sequence the Type 2 char string sequence
		 */
        private void ConvertType1ToType2(List<object> sequence)
        {
            type1Sequence = new List<object>();
            pathCount = 0;
            CharStringHandler handler = new CharStringHandler();
            handler.HandleSequence(sequence, TranslateHandleCommand);
        }

        //@SuppressWarnings(value = { "unchecked" })
        public List<float> TranslateHandleCommand(List<float> numbers, CharStringCommand command)
        {
            commandCount++;
            if (!CharStringCommand.TYPE2_VOCABULARY.TryGetValue(command.Key, out string name))
            {
                if (command.Key.Data[0] == 10)
                {
                    Debug.WriteLine($"warn: Parameter {numbers} for CALLSUBR is ignored, integer expected in glyph '{Name}' of font {FontName}");
                    return new List<float>(0);
                }
                AddCommand(numbers, command);
                return new List<float>(0);
            }
            switch (name)
            {
                case "hstem":
                    numbers = ClearStack(numbers, numbers.Count % 2 != 0);
                    ExpandStemHints(numbers, true);
                    break;
                case "vstem":
                    numbers = ClearStack(numbers, numbers.Count % 2 != 0);
                    ExpandStemHints(numbers, false);
                    break;
                case "vmoveto":
                    numbers = ClearStack(numbers, numbers.Count > 1);
                    MarkPath();
                    AddCommand(numbers, command);
                    break;
                case "rlineto":
                    AddCommandList(Split(numbers, 2), command);
                    break;
                case "hlineto":
                    DrawAlternatingLine(numbers, true);
                    break;
                case "vlineto":
                    DrawAlternatingLine(numbers, false);
                    break;
                case "rrcurveto":
                    AddCommandList(Split(numbers, 6), command);
                    break;
                case "endchar":
                    numbers = ClearStack(numbers, numbers.Count == 5 || numbers.Count == 1);
                    ClosePath();
                    if (numbers.Count == 4)
                    {
                        // deprecated "seac" operator
                        numbers.AddRange(new[] { 0F, 0F });
                        AddCommand(numbers, new CharStringCommand(12, 6));
                    }
                    else
                    {
                        AddCommand(numbers, command);
                    }
                    break;
                case "rmoveto":
                    numbers = ClearStack(numbers, numbers.Count > 2);
                    MarkPath();
                    AddCommand(numbers, command);
                    break;
                case "hmoveto":
                    numbers = ClearStack(numbers, numbers.Count > 1);
                    MarkPath();
                    AddCommand(numbers, command);
                    break;
                case "vhcurveto":
                    DrawAlternatingCurve(numbers, false);
                    break;
                case "hvcurveto":
                    DrawAlternatingCurve(numbers, true);
                    break;
                case "hflex":
                    {
                        List<float> first = new List<float>{numbers[0], 0,
                                numbers[1], numbers[2], numbers[3], 0 };
                        List<float> second = new List<float>{numbers[4], 0,
                                numbers[5], -(numbers[2]),
                                numbers[6], 0 };
                        AddCommandList(new List<List<float>> { first, second }, new CharStringCommand(8));
                        break;
                    }
                case "flex":
                    {
                        List<float> first = numbers.GetRange(0, 6);
                        List<float> second = numbers.GetRange(6, 6);
                        AddCommandList(new List<List<float>> { first, second }, new CharStringCommand(8));
                        break;
                    }
                case "hflex1":
                    {
                        List<float> first = new List<float>{numbers[0], numbers[1],
                                numbers[2], numbers[3], numbers[4], 0 };
                        List<float> second = new List<float>{numbers[5], 0,
                                numbers[6], numbers[7], numbers[8], 0 };
                        AddCommandList(new List<List<float>> { first, second }, new CharStringCommand(8));
                        break;
                    }
                case "flex1":
                    {
                        int dx = 0;
                        int dy = 0;
                        for (int i = 0; i < 5; i++)
                        {
                            dx += (int)numbers[i * 2];
                            dy += (int)numbers[i * 2 + 1];
                        }
                        List<float> first = numbers.GetRange(0, 6);
                        List<float> second = new List<float>{numbers[6], numbers[7], numbers[8],
                                numbers[9], (Math.Abs(dx) > Math.Abs(dy) ? numbers[10] : -dx),
                                (Math.Abs(dx) > Math.Abs(dy) ? -dy : numbers[10]) };
                        AddCommandList(new List<List<float>> { first, second }, new CharStringCommand(8));
                        break;
                    }
                case "hstemhm":
                    numbers = ClearStack(numbers, numbers.Count % 2 != 0);
                    ExpandStemHints(numbers, true);
                    break;
                case "hintmask":
                case "cntrmask":
                    numbers = ClearStack(numbers, numbers.Count % 2 != 0);
                    if (numbers.Count > 0)
                    {
                        ExpandStemHints(numbers, false);
                    }
                    break;
                case "vstemhm":
                    numbers = ClearStack(numbers, numbers.Count % 2 != 0);
                    ExpandStemHints(numbers, false);
                    break;
                case "rcurveline":
                    if (numbers.Count >= 2)
                    {
                        AddCommandList(Split(numbers.GetRange(0, numbers.Count - 2), 6),
                                new CharStringCommand(8));
                        AddCommand(numbers.GetRange(numbers.Count - 2, 2),
                                new CharStringCommand(5));
                    }
                    break;
                case "rlinecurve":
                    if (numbers.Count >= 6)
                    {
                        AddCommandList(Split(numbers.GetRange(0, numbers.Count - 6), 2),
                                new CharStringCommand(5));
                        AddCommand(numbers.GetRange(numbers.Count - 6, 6),
                                new CharStringCommand(8));
                    }
                    break;
                case "vvcurveto":
                    DrawCurve(numbers, false);
                    break;
                case "hhcurveto":
                    DrawCurve(numbers, true);
                    break;
                default:
                    AddCommand(numbers, command);
                    break;
            }
            return new List<float>(0);
        }

        private List<float> ClearStack(List<float> numbers, bool flag)
        {
            if (type1Sequence.Count == 0)
            {
                if (flag)
                {
                    AddCommand(new List<float> { 0f, numbers[0] + nominalWidthX },
                            new CharStringCommand(13));
                    numbers = numbers.GetRange(1, numbers.Count - 1);
                }
                else
                {
                    AddCommand(new List<float> { 0f, defWidthX }, new CharStringCommand(13));
                }
            }
            return numbers;
        }

        /**
		 * @param numbers  
		 * @param horizontal 
		 */
        private void ExpandStemHints(List<float> numbers, bool horizontal)
        {
            // TODO
        }

        private void MarkPath()
        {
            if (pathCount > 0)
            {
                ClosePath();
            }
            pathCount++;
        }

        private void ClosePath()
        {
            CharStringCommand command = pathCount > 0 ? (CharStringCommand)type1Sequence
                    [type1Sequence.Count - 1]
                    : null;

            CharStringCommand closepathCommand = new CharStringCommand(9);
            if (command != null && !closepathCommand.Equals(command))
            {
                AddCommand(new List<float>(0), closepathCommand);
            }
        }

        private void DrawAlternatingLine(List<float> numbers, bool horizontal)
        {
            while (numbers.Count > 0)
            {
                AddCommand(numbers.GetRange(0, 1), new CharStringCommand(
                        horizontal ? (byte)6 : (byte)7));
                numbers = numbers.GetRange(1, numbers.Count - 1);
                horizontal = !horizontal;
            }
        }

        private void DrawAlternatingCurve(List<float> numbers, bool horizontal)
        {
            while (numbers.Count >= 4)
            {
                bool last = numbers.Count == 5;
                if (horizontal)
                {
                    AddCommand(new List<float> { numbers[0], 0, numbers[1], numbers[2], last ? numbers[4] : 0, numbers[3] },
                            new CharStringCommand(8));
                }
                else
                {
                    AddCommand(new List<float> { 0, numbers[0], numbers[1], numbers[2], numbers[3], last ? numbers[4] : 0 },
                            new CharStringCommand(8));
                }
                numbers = numbers.GetRange(last ? 5 : 4, numbers.Count - (last ? 5 : 4));
                horizontal = !horizontal;
            }
        }

        private void DrawCurve(List<float> numbers, bool horizontal)
        {
            while (numbers.Count >= 4)
            {
                bool first = numbers.Count % 4 == 1;

                if (horizontal)
                {
                    AddCommand(new List<float> { numbers[first ? 1 : 0], first ? numbers[0] : 0, numbers[first ? 2 : 1], numbers[first ? 3 : 2], numbers[first ? 4 : 3], 0 },
                             new CharStringCommand(8));
                }
                else
                {
                    AddCommand(new List<float> { first ? numbers[0] : 0, numbers[first ? 1 : 0], numbers[first ? 2 : 1], numbers[first ? 3 : 2], 0, numbers[first ? 4 : 3] },
                            new CharStringCommand(8));
                }
                numbers = numbers.GetRange(first ? 5 : 4, numbers.Count - (first ? 5 : 4));
            }
        }

        private void AddCommandList(List<List<float>> numbers, CharStringCommand command)
        {
            foreach (var ns in numbers)
                AddCommand(ns, command);
        }

        private void AddCommand(List<float> numbers, CharStringCommand command)
        {
            type1Sequence.AddRange(numbers.Cast<object>());
            type1Sequence.Add(command);
        }

        private static List<List<E>> Split<E>(List<E> list, int size)
        {
            List<List<E>> result = new List<List<E>>();
            for (int i = 0; i < list.Count / size; i++)
            {
                result.Add(list.GetRange(i * size, size));
            }
            return result;
        }
    }
}
