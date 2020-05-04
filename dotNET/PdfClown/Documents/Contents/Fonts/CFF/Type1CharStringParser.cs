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
using System.IO;
using System;
using System.Diagnostics;
using PdfClown.Util.Collections.Generic;

namespace PdfClown.Documents.Contents.Fonts.Type1
{

    /**
     * This class represents a converter for a mapping into a Type 1 sequence.
     *
     * @see "Adobe Type 1 Font Format, Adobe Systems (1999)"
     *
     * @author Villu Ruusmann
     * @author John Hewson
     */
    public static class Type1CharStringParser
    {

        // 1-byte commands
        static readonly byte RETURN = 11;
        static readonly byte CALLSUBR = 10;

        // 2-byte commands
        static readonly byte TWO_BYTE = 12;
        static readonly byte CALLOTHERSUBR = 16;
        static readonly byte POP = 17;

        /**
		 * The given byte array will be parsed and converted to a Type1 sequence.
		 *
		 * @param bytes the given mapping as byte array
		 * @param subrs list of local subroutines
		 * @return the Type1 sequence
		 * @throws IOException if an error occurs during reading
		 */
        public static List<object> Parse(string fontName, string glyphName, byte[] bytes, List<byte[]> subrs)
        {
            return Parse(fontName, glyphName, bytes, subrs, new List<object>());
        }

        private static List<object> Parse(string fontName, string glyphName, byte[] bytes, List<byte[]> subrs, List<object> sequence)
        {
            DataInput input = new DataInput(bytes);
            while (input.HasRemaining())
            {
                int b0 = input.ReadUnsignedByte();
                if (b0 == CALLSUBR)
                {
                    // callsubr command
                    object obj = sequence.RemoveAtValue(sequence.Count - 1);
                    if (!(obj is int))
                    {
                        Debug.WriteLine($"warn: Parameter {obj} for CALLSUBR is ignored, integer expected in glyph '{glyphName}' of font {fontName}");
                        continue;
                    }
                    int operand = (int)obj;

                    if (operand >= 0 && operand < subrs.Count)
                    {
                        byte[] subrBytes = subrs[operand];
                        Parse(fontName, glyphName, subrBytes, subrs, sequence);
                        object lastItem = sequence[sequence.Count - 1];
                        if (lastItem is CharStringCommand charStringCommand &&
                              charStringCommand.Key.Data[0] == RETURN)
                        {
                            sequence.RemoveAt(sequence.Count - 1); // remove "return" command
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"warn: CALLSUBR is ignored, operand: {operand}, subrs.Count: {subrs.Count} in glyph '{glyphName}' of font {fontName}");
                        // remove all parameters (there can be more than one)
                        while (sequence[sequence.Count - 1] is int intValue)
                        {
                            sequence.RemoveAt(sequence.Count - 1);
                        }
                    }
                }
                else if (b0 == TWO_BYTE && input.PeekUnsignedByte(0) == CALLOTHERSUBR)
                {
                    // callothersubr command (needed in order to expand Subrs)
                    input.ReadUnsignedByte();

                    int othersubrNum = (int)sequence.RemoveAtValue(sequence.Count - 1);
                    int numArgs = (int)sequence.RemoveAtValue(sequence.Count - 1);

                    // othersubrs 0-3 have their own semantics
                    Stack<int> results = new Stack<int>();
                    switch (othersubrNum)
                    {
                        case 0:
                            results.Push(RemoveInteger(sequence));
                            results.Push(RemoveInteger(sequence));
                            sequence.RemoveAt(sequence.Count - 1);
                            // end flex
                            sequence.Add(0);
                            sequence.Add(new CharStringCommand(TWO_BYTE, CALLOTHERSUBR));
                            break;
                        case 1:
                            // begin flex
                            sequence.Add(1);
                            sequence.Add(new CharStringCommand(TWO_BYTE, CALLOTHERSUBR));
                            break;
                        case 3:
                            // allows hint replacement
                            results.Push(RemoveInteger(sequence));
                            break;
                        default:
                            // all remaining othersubrs use this fallback mechanism
                            for (int i = 0; i < numArgs; i++)
                            {
                                results.Push(RemoveInteger(sequence));
                            }
                            break;
                    }

                    // pop must follow immediately
                    while (input.PeekUnsignedByte(0) == TWO_BYTE && input.PeekUnsignedByte(1) == POP)
                    {
                        input.ReadUnsignedByte(); // B0_POP
                        input.ReadUnsignedByte(); // B1_POP
                        sequence.Add(results.Pop());
                    }

                    if (results.Count > 0)
                    {
                        Debug.WriteLine("warn: Value left on the PostScript stack in glyph " + glyphName + " of font " + fontName);
                    }
                }
                else if (b0 >= 0 && b0 <= 31)
                {
                    sequence.Add(ReadCommand(input, (byte)b0));
                }
                else if (b0 >= 32 && b0 <= 255)
                {
                    sequence.Add(ReadNumber(input, (byte)b0));
                }
                else
                {
                    throw new ArgumentException();
                }
            }
            return sequence;
        }

        // this method is a workaround for the fact that Type1CharStringParser assumes that subrs and
        // othersubrs can be unrolled without executing the 'div' operator, which isn't true
        private static int RemoveInteger(List<object> sequence)
        {
            object item = sequence.RemoveAtValue(sequence.Count - 1);
            if (item is int intValue)
            {
                return intValue;
            }
            CharStringCommand command = (CharStringCommand)item;

            // div
            if (command.Key.Data[0] == 12 && command.Key.Data[1] == 12)
            {
                int a = (int)sequence.RemoveAtValue(sequence.Count - 1);
                int b = (int)sequence.RemoveAtValue(sequence.Count - 1);
                return b / a;
            }
            throw new IOException("Unexpected char string command: " + command.Key);
        }

        private static CharStringCommand ReadCommand(DataInput input, byte b0)
        {
            if (b0 == 12)

            {
                var b1 = input.ReadUnsignedByte();
                return new CharStringCommand(b0, b1);
            }
            return new CharStringCommand(b0);
        }

        private static int ReadNumber(DataInput input, byte b0)
        {
            if (b0 >= 32 && b0 <= 246)
            {
                return b0 - 139;
            }
            else if (b0 >= 247 && b0 <= 250)
            {
                var b1 = input.ReadUnsignedByte();
                return (b0 - 247) * 256 + b1 + 108;
            }
            else if (b0 >= 251 && b0 <= 254)
            {
                var b1 = input.ReadUnsignedByte();
                return -(b0 - 251) * 256 - b1 - 108;
            }
            else if (b0 == 255)

            {
                return input.ReadInt();
            }
            else
            {
                throw new ArgumentException();
            }
        }
    }


}
