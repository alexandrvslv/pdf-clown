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
using PdfClown.Util;
using System.Collections.Generic;

namespace PdfClown.Documents.Contents.Fonts.Type1
{

    /**
	 * This class represents a CharStringCommand.
	 * 
	 * @author Villu Ruusmann
	 */
    public class CharStringCommand
    {

        private ByteArray commandKey = null;

        /**
		 * Constructor with one value.
		 * 
		 * @param b0 value
		 */
        public CharStringCommand(byte b0)
        {
            Key = new ByteArray(b0);
        }

        /**
		 * Constructor with two values.
		 * 
		 * @param b0 value1
		 * @param b1 value2
		 */
        public CharStringCommand(byte b0, byte b1)
        {
            Key = new ByteArray(b0, b1);
        }

        /**
		 * Constructor with an array as values.
		 * 
		 * @param values array of values
		 */
        public CharStringCommand(byte[] values)
        {
            Key = new ByteArray(values);
        }

        /**
		 * The key of the CharStringCommand.
		 * @return the key
		 */
        public ByteArray Key
        {
            get => commandKey;
            set => commandKey = value;
        }

        /**
		 * {@inheritDoc}
		 */
        override public string ToString()
        {
            string str;
            if (!TYPE2_VOCABULARY.TryGetValue(Key, out str))
            {
                str = TYPE1_VOCABULARY[Key];
            }
            if (str == null)
            {
                return Key.ToString() + '|';
            }
            return str + '|';
        }

        /**
		 * {@inheritDoc}
		 */
        override public int GetHashCode()
        {
            return Key.GetHashCode();
        }

        /**
		 * {@inheritDoc}
		 */
        override public bool Equals(object obj)
        {
            if (obj is CharStringCommand that)
            {
                return Key.Equals(that.Key);
            }
            return false;
        }

        /**
		 * A map with the Type1 vocabulary.
		 */
        public static readonly Dictionary<ByteArray, string> TYPE1_VOCABULARY = new Dictionary<ByteArray, string>(26)
        {
            { new ByteArray(1), "hstem"},
            {new ByteArray(3), "vstem"},
            {new ByteArray(4), "vmoveto"},
            {new ByteArray(5), "rlineto"},
            {new ByteArray(6), "hlineto"},
            {new ByteArray(7), "vlineto"},
            {new ByteArray(8), "rrcurveto"},
            {new ByteArray(9), "closepath"},
            {new ByteArray(10), "callsubr"},
            {new ByteArray(11), "return"},
            {new ByteArray(12), "escape"},
            {new ByteArray(12, 0), "dotsection"},
            {new ByteArray(12, 1), "vstem3"},
            {new ByteArray(12, 2), "hstem3"},
            {new ByteArray(12, 6), "seac"},
            {new ByteArray(12, 7), "sbw"},
            {new ByteArray(12, 12), "div"},
            {new ByteArray(12, 16), "callothersubr"},
            {new ByteArray(12, 17), "pop"},
            {new ByteArray(12, 33), "setcurrentpoint"},
            {new ByteArray(13), "hsbw"},
            {new ByteArray(14), "endchar"},
            {new ByteArray(21), "rmoveto"},
            {new ByteArray(22), "hmoveto"},
            {new ByteArray(30), "vhcurveto"},
            {new ByteArray(31), "hvcurveto"},
        };

        /**
		 * A map with the Type2 vocabulary.
		 */
        public static readonly Dictionary<ByteArray, string> TYPE2_VOCABULARY = new Dictionary<ByteArray, string>
        {
            {new ByteArray(1), "hstem"},
            {new ByteArray(3), "vstem"},
            {new ByteArray(4), "vmoveto"},
            {new ByteArray(5), "rlineto"},
            {new ByteArray(6), "hlineto"},
            {new ByteArray(7), "vlineto"},
            {new ByteArray(8), "rrcurveto"},
            {new ByteArray(10), "callsubr"},
            {new ByteArray(11), "return"},
            {new ByteArray(12), "escape"},
            {new ByteArray(12, 3), "and"},
            {new ByteArray(12, 4), "or"},
            {new ByteArray(12, 5), "not"},
            {new ByteArray(12, 9), "abs"},
            {new ByteArray(12, 10), "add"},
            {new ByteArray(12, 11), "sub"},
            {new ByteArray(12, 12), "div"},
            {new ByteArray(12, 14), "neg"},
            {new ByteArray(12, 15), "eq"},
            {new ByteArray(12, 18), "drop"},
            {new ByteArray(12, 20), "put"},
            {new ByteArray(12, 21), "get"},
            {new ByteArray(12, 22), "ifelse"},
            {new ByteArray(12, 23), "random"},
            {new ByteArray(12, 24), "mul"},
            {new ByteArray(12, 26), "sqrt"},
            {new ByteArray(12, 27), "dup"},
            {new ByteArray(12, 28), "exch"},
            {new ByteArray(12, 29), "index"},
            {new ByteArray(12, 30), "roll"},
            {new ByteArray(12, 34), "hflex"},
            {new ByteArray(12, 35), "flex"},
            {new ByteArray(12, 36), "hflex1"},
            {new ByteArray(12, 37), "flex1"},
            {new ByteArray(14), "endchar"},
            {new ByteArray(18), "hstemhm"},
            {new ByteArray(19), "hintmask"},
            {new ByteArray(20), "cntrmask"},
            {new ByteArray(21), "rmoveto"},
            {new ByteArray(22), "hmoveto"},
            {new ByteArray(23), "vstemhm"},
            {new ByteArray(24), "rcurveline"},
            {new ByteArray(25), "rlinecurve"},
            {new ByteArray(26), "vvcurveto"},
            {new ByteArray(27), "hhcurveto"},
            {new ByteArray(28), "shortint"},
            {new ByteArray(29), "callgsubr"},
            {new ByteArray(30), "vhcurveto"},
            {new ByteArray(31), "hvcurveto"},
    };
    }
}
