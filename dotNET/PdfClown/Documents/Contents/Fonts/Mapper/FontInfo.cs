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

namespace PdfClown.Documents.Contents.Fonts
{

    /**
     * Information about a font on the system.
     *
     * @author John Hewson
     */
    public abstract class FontInfo
    {
        /**
         * Returns the PostScript name of the font.
         */
        public abstract string PostScriptName { get; }

        /**
         * Returns the font's format.
         */
        public abstract FontFormat Format { get; }

        /**
         * Returns the CIDSystemInfo associated with the font, if any.
         */
        public abstract CIDSystemInfo CIDSystemInfo { get; }

        /**
         * Returns a new FontBox font instance for the font. Implementors of this method must not
         * cache the return value of this method unless doing so via the current {@link FontCache}.
         */
        public abstract BaseFont Font { get; }

        /**
         * Returns the sFamilyClass field of the "OS/2" table, or -1.
         */
        public abstract int FamilyClass { get; }

        /**
         * Returns the usWeightClass field of the "OS/2" table, or -1.
         */
        public abstract int WeightClass { get; }

        /**
         * Returns the usWeightClass field as a Panose Weight.
         */
        public int WeightClassAsPanose
        {
            get
            {
                int usWeightClass = WeightClass;
                switch (usWeightClass)
                {
                    case -1: return 0;
                    case 0: return 0;
                    case 100: return 2;
                    case 200: return 3;
                    case 300: return 4;
                    case 400: return 5;
                    case 500: return 6;
                    case 600: return 7;
                    case 700: return 8;
                    case 800: return 9;
                    case 900: return 10;
                    default: return 0;
                }
            }
        }

        /**
         * Returns the ulCodePageRange1 field of the "OS/2" table, or 0.
         */
        public abstract int CodePageRange1 { get; }

        /**
         * Returns the ulCodePageRange2 field of the "OS/2" table, or 0.
         */
        public abstract int CodePageRange2 { get; }

        /**
         * Returns the ulCodePageRange1 and ulCodePageRange1 field of the "OS/2" table, or 0.
         */
        public long CodePageRange
        {
            get
            {
                long range1 = CodePageRange1 & 0x00000000ffffffffL;
                long range2 = CodePageRange2 & 0x00000000ffffffffL;
                return range2 << 32 | range1;
            }
        }

        /**
         * Returns the macStyle field of the "head" table, or -1.
         */
        public abstract int MacStyle { get; }

        /**
         * Returns the Panose classification of the font, if any.
         */
        public abstract PanoseClassification Panose { get; }

        // todo: 'post' table for Italic. Also: OS/2 fsSelection for italic/bold.
        // todo: ulUnicodeRange too?


        public override string ToString()
        {
            return $"{PostScriptName} ({Format}, mac: 0x{MacStyle:x2}, os/2: 0x{FamilyClass:x2}, cid: {CIDSystemInfo})";
        }
    }
}