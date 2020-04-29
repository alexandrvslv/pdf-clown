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
using System;
using System.Collections.Generic;

namespace PdfClown.Documents.Contents.Fonts.CCF
{
    /**
     * This class represents a CFF operator.
     * @author Villu Ruusmann
     */
    public sealed class CFFOperator
    {
        private ByteArray operatorKey = null;
        private string operatorName = null;

        private CFFOperator(ByteArray key, string name)
        {
            operatorKey = key;
            operatorName = name;
        }

        /**
		 * The key of the operator.
		 * @return the key
		 */
        public ByteArray Key
        {
            get => operatorKey;
            set => operatorKey = value;
        }

        /**
		 * The name of the operator.
		 * @return the name
		 */
        public string Name
        {
            get => operatorName;
            set => operatorName = value;
        }

        /**
		 * {@inheritDoc}
		 */

        public override string ToString()
        {
            return Name;
        }

        /**
		 * {@inheritDoc}
		 */

        public override int GetHashCode()
        {
            return Key.GetHashCode();
        }

        /**
		 * {@inheritDoc}
		 */
        public override bool Equals(object obj)
        {
            if (obj is CFFOperator that)
            {
                return Key.Equals(that.Key);
            }
            return false;
        }

        private static void Register(ByteArray key, string name)
        {
            CFFOperator ccfOperator = new CFFOperator(key, name);
            keyMap[key] = ccfOperator;
            nameMap[name] = ccfOperator;
        }

        /**
		 * Returns the operator corresponding to the given key.
		 * @param key the given key
		 * @return the corresponding operator
		 */
        public static CFFOperator GetOperator(ByteArray key)
        {
            return keyMap.TryGetValue(key, out var cFFOperator) ? cFFOperator : null;
        }

        /**
		 * Returns the operator corresponding to the given name.
		 * @param name the given name
		 * @return the corresponding operator
		 */
        public static CFFOperator GetOperator(string name)
        {
            return nameMap.TryGetValue(name, out var cFFOperator) ? cFFOperator : null;
        }

        /**
		 * This class is a holder for a key value. It consists of one or two bytes.  
		 * @author Villu Ruusmann
		 */


        private static Dictionary<ByteArray, CFFOperator> keyMap = new Dictionary<ByteArray, CFFOperator>(52);
        private static Dictionary<string, CFFOperator> nameMap = new Dictionary<string, CFFOperator>(52, StringComparer.Ordinal);

        static CFFOperator()
        {
            // Top DICT
            Register(new ByteArray(0), "version");
            Register(new ByteArray(1), "Notice");
            Register(new ByteArray(12, 0), "Copyright");
            Register(new ByteArray(2), "FullName");
            Register(new ByteArray(3), "FamilyName");
            Register(new ByteArray(4), "Weight");
            Register(new ByteArray(12, 1), "isFixedPitch");
            Register(new ByteArray(12, 2), "ItalicAngle");
            Register(new ByteArray(12, 3), "UnderlinePosition");
            Register(new ByteArray(12, 4), "UnderlineThickness");
            Register(new ByteArray(12, 5), "PaintType");
            Register(new ByteArray(12, 6), "CharstringType");
            Register(new ByteArray(12, 7), "FontMatrix");
            Register(new ByteArray(13), "UniqueID");
            Register(new ByteArray(5), "FontBBox");
            Register(new ByteArray(12, 8), "StrokeWidth");
            Register(new ByteArray(14), "XUID");
            Register(new ByteArray(15), "charset");
            Register(new ByteArray(16), "Encoding");
            Register(new ByteArray(17), "CharStrings");
            Register(new ByteArray(18), "Private");
            Register(new ByteArray(12, 20), "SyntheticBase");
            Register(new ByteArray(12, 21), "PostScript");
            Register(new ByteArray(12, 22), "BaseFontName");
            Register(new ByteArray(12, 23), "BaseFontBlend");
            Register(new ByteArray(12, 30), "ROS");
            Register(new ByteArray(12, 31), "CIDFontVersion");
            Register(new ByteArray(12, 32), "CIDFontRevision");
            Register(new ByteArray(12, 33), "CIDFontType");
            Register(new ByteArray(12, 34), "CIDCount");
            Register(new ByteArray(12, 35), "UIDBase");
            Register(new ByteArray(12, 36), "FDArray");
            Register(new ByteArray(12, 37), "FDSelect");
            Register(new ByteArray(12, 38), "FontName");

            // Private DICT
            Register(new ByteArray(6), "BlueValues");
            Register(new ByteArray(7), "OtherBlues");
            Register(new ByteArray(8), "FamilyBlues");
            Register(new ByteArray(9), "FamilyOtherBlues");
            Register(new ByteArray(12, 9), "BlueScale");
            Register(new ByteArray(12, 10), "BlueShift");
            Register(new ByteArray(12, 11), "BlueFuzz");
            Register(new ByteArray(10), "StdHW");
            Register(new ByteArray(11), "StdVW");
            Register(new ByteArray(12, 12), "StemSnapH");
            Register(new ByteArray(12, 13), "StemSnapV");
            Register(new ByteArray(12, 14), "ForceBold");
            Register(new ByteArray(12, 15), "LanguageGroup");
            Register(new ByteArray(12, 16), "ExpansionFactor");
            Register(new ByteArray(12, 17), "initialRandomSeed");
            Register(new ByteArray(19), "Subrs");
            Register(new ByteArray(20), "defaultWidthX");
            Register(new ByteArray(21), "nominalWidthX");
        }
    }
}
