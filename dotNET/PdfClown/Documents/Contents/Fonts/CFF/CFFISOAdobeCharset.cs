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
using System;
using System.Collections.Generic;

namespace PdfClown.Documents.Contents.Fonts.CCF
{
	/**
     * This is specialized CFFCharset. It's used if the CharsetId of a font is set to 0.
     * 
     * @author Villu Ruusmann
     */
	public sealed class CFFISOAdobeCharset : CFFCharset
	{

		private CFFISOAdobeCharset()
			: base(false)
		{
		}

		public static readonly CFFISOAdobeCharset Instance = new CFFISOAdobeCharset();

		static CFFISOAdobeCharset()
		{
			int gid = 0;
			Instance.AddSID(gid++, 0, ".notdef");
			Instance.AddSID(gid++, 1, "space");
			Instance.AddSID(gid++, 2, "exclam");
			Instance.AddSID(gid++, 3, "quotedbl");
			Instance.AddSID(gid++, 4, "numbersign");
			Instance.AddSID(gid++, 5, "dollar");
			Instance.AddSID(gid++, 6, "percent");
			Instance.AddSID(gid++, 7, "ampersand");
			Instance.AddSID(gid++, 8, "quoteright");
			Instance.AddSID(gid++, 9, "parenleft");
			Instance.AddSID(gid++, 10, "parenright");
			Instance.AddSID(gid++, 11, "asterisk");
			Instance.AddSID(gid++, 12, "plus");
			Instance.AddSID(gid++, 13, "comma");
			Instance.AddSID(gid++, 14, "hyphen");
			Instance.AddSID(gid++, 15, "period");
			Instance.AddSID(gid++, 16, "slash");
			Instance.AddSID(gid++, 17, "zero");
			Instance.AddSID(gid++, 18, "one");
			Instance.AddSID(gid++, 19, "two");
			Instance.AddSID(gid++, 20, "three");
			Instance.AddSID(gid++, 21, "four");
			Instance.AddSID(gid++, 22, "five");
			Instance.AddSID(gid++, 23, "six");
			Instance.AddSID(gid++, 24, "seven");
			Instance.AddSID(gid++, 25, "eight");
			Instance.AddSID(gid++, 26, "nine");
			Instance.AddSID(gid++, 27, "colon");
			Instance.AddSID(gid++, 28, "semicolon");
			Instance.AddSID(gid++, 29, "less");
			Instance.AddSID(gid++, 30, "equal");
			Instance.AddSID(gid++, 31, "greater");
			Instance.AddSID(gid++, 32, "question");
			Instance.AddSID(gid++, 33, "at");
			Instance.AddSID(gid++, 34, "A");
			Instance.AddSID(gid++, 35, "B");
			Instance.AddSID(gid++, 36, "C");
			Instance.AddSID(gid++, 37, "D");
			Instance.AddSID(gid++, 38, "E");
			Instance.AddSID(gid++, 39, "F");
			Instance.AddSID(gid++, 40, "G");
			Instance.AddSID(gid++, 41, "H");
			Instance.AddSID(gid++, 42, "I");
			Instance.AddSID(gid++, 43, "J");
			Instance.AddSID(gid++, 44, "K");
			Instance.AddSID(gid++, 45, "L");
			Instance.AddSID(gid++, 46, "M");
			Instance.AddSID(gid++, 47, "N");
			Instance.AddSID(gid++, 48, "O");
			Instance.AddSID(gid++, 49, "P");
			Instance.AddSID(gid++, 50, "Q");
			Instance.AddSID(gid++, 51, "R");
			Instance.AddSID(gid++, 52, "S");
			Instance.AddSID(gid++, 53, "T");
			Instance.AddSID(gid++, 54, "U");
			Instance.AddSID(gid++, 55, "V");
			Instance.AddSID(gid++, 56, "W");
			Instance.AddSID(gid++, 57, "X");
			Instance.AddSID(gid++, 58, "Y");
			Instance.AddSID(gid++, 59, "Z");
			Instance.AddSID(gid++, 60, "bracketleft");
			Instance.AddSID(gid++, 61, "backslash");
			Instance.AddSID(gid++, 62, "bracketright");
			Instance.AddSID(gid++, 63, "asciicircum");
			Instance.AddSID(gid++, 64, "underscore");
			Instance.AddSID(gid++, 65, "quoteleft");
			Instance.AddSID(gid++, 66, "a");
			Instance.AddSID(gid++, 67, "b");
			Instance.AddSID(gid++, 68, "c");
			Instance.AddSID(gid++, 69, "d");
			Instance.AddSID(gid++, 70, "e");
			Instance.AddSID(gid++, 71, "f");
			Instance.AddSID(gid++, 72, "g");
			Instance.AddSID(gid++, 73, "h");
			Instance.AddSID(gid++, 74, "i");
			Instance.AddSID(gid++, 75, "j");
			Instance.AddSID(gid++, 76, "k");
			Instance.AddSID(gid++, 77, "l");
			Instance.AddSID(gid++, 78, "m");
			Instance.AddSID(gid++, 79, "n");
			Instance.AddSID(gid++, 80, "o");
			Instance.AddSID(gid++, 81, "p");
			Instance.AddSID(gid++, 82, "q");
			Instance.AddSID(gid++, 83, "r");
			Instance.AddSID(gid++, 84, "s");
			Instance.AddSID(gid++, 85, "t");
			Instance.AddSID(gid++, 86, "u");
			Instance.AddSID(gid++, 87, "v");
			Instance.AddSID(gid++, 88, "w");
			Instance.AddSID(gid++, 89, "x");
			Instance.AddSID(gid++, 90, "y");
			Instance.AddSID(gid++, 91, "z");
			Instance.AddSID(gid++, 92, "braceleft");
			Instance.AddSID(gid++, 93, "bar");
			Instance.AddSID(gid++, 94, "braceright");
			Instance.AddSID(gid++, 95, "asciitilde");
			Instance.AddSID(gid++, 96, "exclamdown");
			Instance.AddSID(gid++, 97, "cent");
			Instance.AddSID(gid++, 98, "sterling");
			Instance.AddSID(gid++, 99, "fraction");
			Instance.AddSID(gid++, 100, "yen");
			Instance.AddSID(gid++, 101, "florin");
			Instance.AddSID(gid++, 102, "section");
			Instance.AddSID(gid++, 103, "currency");
			Instance.AddSID(gid++, 104, "quotesingle");
			Instance.AddSID(gid++, 105, "quotedblleft");
			Instance.AddSID(gid++, 106, "guillemotleft");
			Instance.AddSID(gid++, 107, "guilsinglleft");
			Instance.AddSID(gid++, 108, "guilsinglright");
			Instance.AddSID(gid++, 109, "fi");
			Instance.AddSID(gid++, 110, "fl");
			Instance.AddSID(gid++, 111, "endash");
			Instance.AddSID(gid++, 112, "dagger");
			Instance.AddSID(gid++, 113, "daggerdbl");
			Instance.AddSID(gid++, 114, "periodcentered");
			Instance.AddSID(gid++, 115, "paragraph");
			Instance.AddSID(gid++, 116, "bullet");
			Instance.AddSID(gid++, 117, "quotesinglbase");
			Instance.AddSID(gid++, 118, "quotedblbase");
			Instance.AddSID(gid++, 119, "quotedblright");
			Instance.AddSID(gid++, 120, "guillemotright");
			Instance.AddSID(gid++, 121, "ellipsis");
			Instance.AddSID(gid++, 122, "perthousand");
			Instance.AddSID(gid++, 123, "questiondown");
			Instance.AddSID(gid++, 124, "grave");
			Instance.AddSID(gid++, 125, "acute");
			Instance.AddSID(gid++, 126, "circumflex");
			Instance.AddSID(gid++, 127, "tilde");
			Instance.AddSID(gid++, 128, "macron");
			Instance.AddSID(gid++, 129, "breve");
			Instance.AddSID(gid++, 130, "dotaccent");
			Instance.AddSID(gid++, 131, "dieresis");
			Instance.AddSID(gid++, 132, "ring");
			Instance.AddSID(gid++, 133, "cedilla");
			Instance.AddSID(gid++, 134, "hungarumlaut");
			Instance.AddSID(gid++, 135, "ogonek");
			Instance.AddSID(gid++, 136, "caron");
			Instance.AddSID(gid++, 137, "emdash");
			Instance.AddSID(gid++, 138, "AE");
			Instance.AddSID(gid++, 139, "ordfeminine");
			Instance.AddSID(gid++, 140, "Lslash");
			Instance.AddSID(gid++, 141, "Oslash");
			Instance.AddSID(gid++, 142, "OE");
			Instance.AddSID(gid++, 143, "ordmasculine");
			Instance.AddSID(gid++, 144, "ae");
			Instance.AddSID(gid++, 145, "dotlessi");
			Instance.AddSID(gid++, 146, "lslash");
			Instance.AddSID(gid++, 147, "oslash");
			Instance.AddSID(gid++, 148, "oe");
			Instance.AddSID(gid++, 149, "germandbls");
			Instance.AddSID(gid++, 150, "onesuperior");
			Instance.AddSID(gid++, 151, "logicalnot");
			Instance.AddSID(gid++, 152, "mu");
			Instance.AddSID(gid++, 153, "trademark");
			Instance.AddSID(gid++, 154, "Eth");
			Instance.AddSID(gid++, 155, "onehalf");
			Instance.AddSID(gid++, 156, "plusminus");
			Instance.AddSID(gid++, 157, "Thorn");
			Instance.AddSID(gid++, 158, "onequarter");
			Instance.AddSID(gid++, 159, "divide");
			Instance.AddSID(gid++, 160, "brokenbar");
			Instance.AddSID(gid++, 161, "degree");
			Instance.AddSID(gid++, 162, "thorn");
			Instance.AddSID(gid++, 163, "threequarters");
			Instance.AddSID(gid++, 164, "twosuperior");
			Instance.AddSID(gid++, 165, "registered");
			Instance.AddSID(gid++, 166, "minus");
			Instance.AddSID(gid++, 167, "eth");
			Instance.AddSID(gid++, 168, "multiply");
			Instance.AddSID(gid++, 169, "threesuperior");
			Instance.AddSID(gid++, 170, "copyright");
			Instance.AddSID(gid++, 171, "Aacute");
			Instance.AddSID(gid++, 172, "Acircumflex");
			Instance.AddSID(gid++, 173, "Adieresis");
			Instance.AddSID(gid++, 174, "Agrave");
			Instance.AddSID(gid++, 175, "Aring");
			Instance.AddSID(gid++, 176, "Atilde");
			Instance.AddSID(gid++, 177, "Ccedilla");
			Instance.AddSID(gid++, 178, "Eacute");
			Instance.AddSID(gid++, 179, "Ecircumflex");
			Instance.AddSID(gid++, 180, "Edieresis");
			Instance.AddSID(gid++, 181, "Egrave");
			Instance.AddSID(gid++, 182, "Iacute");
			Instance.AddSID(gid++, 183, "Icircumflex");
			Instance.AddSID(gid++, 184, "Idieresis");
			Instance.AddSID(gid++, 185, "Igrave");
			Instance.AddSID(gid++, 186, "Ntilde");
			Instance.AddSID(gid++, 187, "Oacute");
			Instance.AddSID(gid++, 188, "Ocircumflex");
			Instance.AddSID(gid++, 189, "Odieresis");
			Instance.AddSID(gid++, 190, "Ograve");
			Instance.AddSID(gid++, 191, "Otilde");
			Instance.AddSID(gid++, 192, "Scaron");
			Instance.AddSID(gid++, 193, "Uacute");
			Instance.AddSID(gid++, 194, "Ucircumflex");
			Instance.AddSID(gid++, 195, "Udieresis");
			Instance.AddSID(gid++, 196, "Ugrave");
			Instance.AddSID(gid++, 197, "Yacute");
			Instance.AddSID(gid++, 198, "Ydieresis");
			Instance.AddSID(gid++, 199, "Zcaron");
			Instance.AddSID(gid++, 200, "aacute");
			Instance.AddSID(gid++, 201, "acircumflex");
			Instance.AddSID(gid++, 202, "adieresis");
			Instance.AddSID(gid++, 203, "agrave");
			Instance.AddSID(gid++, 204, "aring");
			Instance.AddSID(gid++, 205, "atilde");
			Instance.AddSID(gid++, 206, "ccedilla");
			Instance.AddSID(gid++, 207, "eacute");
			Instance.AddSID(gid++, 208, "ecircumflex");
			Instance.AddSID(gid++, 209, "edieresis");
			Instance.AddSID(gid++, 210, "egrave");
			Instance.AddSID(gid++, 211, "iacute");
			Instance.AddSID(gid++, 212, "icircumflex");
			Instance.AddSID(gid++, 213, "idieresis");
			Instance.AddSID(gid++, 214, "igrave");
			Instance.AddSID(gid++, 215, "ntilde");
			Instance.AddSID(gid++, 216, "oacute");
			Instance.AddSID(gid++, 217, "ocircumflex");
			Instance.AddSID(gid++, 218, "odieresis");
			Instance.AddSID(gid++, 219, "ograve");
			Instance.AddSID(gid++, 220, "otilde");
			Instance.AddSID(gid++, 221, "scaron");
			Instance.AddSID(gid++, 222, "uacute");
			Instance.AddSID(gid++, 223, "ucircumflex");
			Instance.AddSID(gid++, 224, "udieresis");
			Instance.AddSID(gid++, 225, "ugrave");
			Instance.AddSID(gid++, 226, "yacute");
			Instance.AddSID(gid++, 227, "ydieresis");
			Instance.AddSID(gid++, 228, "zcaron");
		}
	}
}