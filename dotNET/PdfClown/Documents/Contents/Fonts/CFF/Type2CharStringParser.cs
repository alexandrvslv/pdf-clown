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

using PdfClown.Documents.Contents.Fonts.Type1;
using System;
using System.Collections.Generic;

namespace PdfClown.Documents.Contents.Fonts.CCF
{

    /**
     * This class represents a converter for a mapping into a Type2-sequence.
     * @author Villu Ruusmann
     */
    public static class Type2CharStringParser
    {
        public static List<object> Parse(string fontName, int cid, byte[] bytes, byte[][] globalSubrIndex, byte[][] localSubrIndex)
        {
            return Parse(fontName, cid.ToString("x4"), bytes, globalSubrIndex, localSubrIndex); // for debugging only
        }

        public static List<object> Parse(string fontName, string glyphName, byte[] bytes, byte[][] globalSubrIndex, byte[][] localSubrIndex)
        {
            return Parse(bytes, globalSubrIndex, localSubrIndex);
        }

        /**
		 * The given byte array will be parsed and converted to a Type2 sequence.
		 * @param bytes the given mapping as byte array
		 * @param globalSubrIndex array containing all global subroutines
		 * @param localSubrIndex array containing all local subroutines
		 * 
		 * @return the Type2 sequence
		 * @throws IOException if an error occurs during reading
		 */
        public static List<object> Parse(byte[] bytes, byte[][] globalSubrIndex, byte[][] localSubrIndex)
        {
            int hstemCount = 0;
            int vstemCount = 0;
            List<object> sequence = null;
            return Parse(bytes, globalSubrIndex, localSubrIndex, true, ref hstemCount, ref vstemCount, ref sequence);
        }

        private static List<object> Parse(byte[] bytes, byte[][] globalSubrIndex, byte[][] localSubrIndex, bool init, ref int hstemCount, ref int vstemCount, ref List<object> sequence)
        {
            if (init)
            {
                hstemCount = 0;
                vstemCount = 0;
                sequence = new List<object>();
            }
            DataInput input = new DataInput(bytes);
            bool localSubroutineIndexProvided = localSubrIndex != null && localSubrIndex.Length > 0;
            bool globalSubroutineIndexProvided = globalSubrIndex != null && globalSubrIndex.Length > 0;

            while (input.HasRemaining())
            {
                var b0 = input.ReadUnsignedByte();
                if (b0 == 10 && localSubroutineIndexProvided)
                { // process subr command
                    int operand = (int)sequence.RemoveAtValue(sequence.Count - 1);
                    //get subrbias
                    int bias = 0;
                    int nSubrs = localSubrIndex.Length;

                    if (nSubrs < 1240)
                    {
                        bias = 107;
                    }
                    else if (nSubrs < 33900)
                    {
                        bias = 1131;
                    }
                    else
                    {
                        bias = 32768;
                    }
                    int subrNumber = bias + operand;
                    if (subrNumber < localSubrIndex.Length)
                    {
                        byte[] subrBytes = localSubrIndex[subrNumber];
                        Parse(subrBytes, globalSubrIndex, localSubrIndex, false, ref hstemCount, ref vstemCount, ref sequence);
                        object lastItem = sequence[sequence.Count - 1];
                        if (lastItem is CharStringCommand && ((CharStringCommand)lastItem).Key.Data[0] == 11)
                        {
                            sequence.RemoveAt(sequence.Count - 1); // remove "return" command
                        }
                    }

                }
                else if (b0 == 29 && globalSubroutineIndexProvided)
                { // process globalsubr command
                    int operand = (int)sequence.RemoveAtValue(sequence.Count - 1);
                    //get subrbias
                    int bias;
                    int nSubrs = globalSubrIndex.Length;

                    if (nSubrs < 1240)
                    {
                        bias = 107;
                    }
                    else if (nSubrs < 33900)
                    {
                        bias = 1131;
                    }
                    else
                    {
                        bias = 32768;
                    }

                    int subrNumber = bias + operand;
                    if (subrNumber < globalSubrIndex.Length)
                    {
                        byte[] subrBytes = globalSubrIndex[subrNumber];
                        Parse(subrBytes, globalSubrIndex, localSubrIndex, false, ref hstemCount, ref vstemCount, ref sequence);
                        object lastItem = sequence[sequence.Count - 1];
                        if (lastItem is CharStringCommand && ((CharStringCommand)lastItem).Key.Data[0] == 11)
                        {
                            sequence.RemoveAt(sequence.Count - 1); // remove "return" command
                        }
                    }

                }
                else if (b0 >= 0 && b0 <= 27)
                {
                    sequence.Add(ReadCommand(b0, input, ref hstemCount, ref vstemCount, sequence));
                }
                else if (b0 == 28)
                {
                    sequence.Add(ReadNumber(b0, input));
                }
                else if (b0 >= 29 && b0 <= 31)
                {
                    sequence.Add(ReadCommand(b0, input, ref hstemCount, ref vstemCount, sequence));
                }
                else if (b0 >= 32 && b0 <= 255)
                {
                    sequence.Add(ReadNumber(b0, input));
                }
                else
                {
                    throw new ArgumentException();
                }
            }
            return sequence;
        }

        private static CharStringCommand ReadCommand(byte b0, DataInput input, ref int hstemCount, ref int vstemCount, List<object> sequence)
        {
            if (b0 == 1 || b0 == 18)
            {
                hstemCount += PeekNumbers(sequence).Count / 2;
            }
            else if (b0 == 3 || b0 == 19 || b0 == 20 || b0 == 23)
            {
                vstemCount += PeekNumbers(sequence).Count / 2;
            } // End if

            if (b0 == 12)
            {
                var b1 = input.ReadUnsignedByte();

                return new CharStringCommand(b0, b1);
            }
            else if (b0 == 19 || b0 == 20)
            {
                byte[] value = new byte[1 + GetMaskLength(hstemCount, vstemCount)];
                value[0] = b0;

                for (int i = 1; i < value.Length; i++)
                {
                    value[i] = input.ReadUnsignedByte();
                }

                return new CharStringCommand(value);
            }

            return new CharStringCommand(b0);
        }

        private static float ReadNumber(int b0, DataInput input)
        {
            if (b0 == 28)
            {
                return (int)input.ReadShort();
            }
            else if (b0 >= 32 && b0 <= 246)
            {
                return b0 - 139;
            }
            else if (b0 >= 247 && b0 <= 250)
            {
                int b1 = input.ReadUnsignedByte();

                return (b0 - 247) * 256 + b1 + 108;
            }
            else if (b0 >= 251 && b0 <= 254)
            {
                int b1 = input.ReadUnsignedByte();

                return -(b0 - 251) * 256 - b1 - 108;
            }
            else if (b0 == 255)
            {
                short value = input.ReadShort();
                // The lower bytes are representing the digits after the decimal point
                double fraction = input.ReadUnsignedShort() / 65535d;
                return value + (float)fraction;
            }
            else
            {
                throw new ArgumentException();
            }
        }

        private static int GetMaskLength(int hstemCount, int vstemCount)
        {
            int hintCount = hstemCount + vstemCount;
            int Length = hintCount / 8;
            if (hintCount % 8 > 0)
            {
                Length++;
            }
            return Length;
        }

        private static List<float> PeekNumbers(List<object> sequence)
        {
            List<float> numbers = new List<float>();
            for (int i = sequence.Count - 1; i > -1; i--)
            {
                object obj = sequence[i];

                if (!(obj is float))
                {
                    return numbers;
                }
                numbers.AddRange(new[] { 0F, (float)obj });
            }
            return numbers;
        }
    }
}
