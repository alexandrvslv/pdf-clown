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
namespace PdfClown.Documents.Contents.Fonts.CCF
{

	/**
     * This is specialized CFFCharset. It's used if the CharsetId of a font is set to 2.
     * 
     * @author Villu Ruusmann
     */
	public sealed class CFFExpertSubsetCharset : CFFCharset
	{
		private CFFExpertSubsetCharset()
			: base(false)
		{
		}

		public static readonly CFFExpertSubsetCharset Instance = new CFFExpertSubsetCharset();

		static CFFExpertSubsetCharset()
		{
			int gid = 0;
			Instance.AddSID(gid++, 0, ".notdef");
			Instance.AddSID(gid++, 1, "space");
			Instance.AddSID(gid++, 231, "dollaroldstyle");
			Instance.AddSID(gid++, 232, "dollarsuperior");
			Instance.AddSID(gid++, 235, "parenleftsuperior");
			Instance.AddSID(gid++, 236, "parenrightsuperior");
			Instance.AddSID(gid++, 237, "twodotenleader");
			Instance.AddSID(gid++, 238, "onedotenleader");
			Instance.AddSID(gid++, 13, "comma");
			Instance.AddSID(gid++, 14, "hyphen");
			Instance.AddSID(gid++, 15, "period");
			Instance.AddSID(gid++, 99, "fraction");
			Instance.AddSID(gid++, 239, "zerooldstyle");
			Instance.AddSID(gid++, 240, "oneoldstyle");
			Instance.AddSID(gid++, 241, "twooldstyle");
			Instance.AddSID(gid++, 242, "threeoldstyle");
			Instance.AddSID(gid++, 243, "fouroldstyle");
			Instance.AddSID(gid++, 244, "fiveoldstyle");
			Instance.AddSID(gid++, 245, "sixoldstyle");
			Instance.AddSID(gid++, 246, "sevenoldstyle");
			Instance.AddSID(gid++, 247, "eightoldstyle");
			Instance.AddSID(gid++, 248, "nineoldstyle");
			Instance.AddSID(gid++, 27, "colon");
			Instance.AddSID(gid++, 28, "semicolon");
			Instance.AddSID(gid++, 249, "commasuperior");
			Instance.AddSID(gid++, 250, "threequartersemdash");
			Instance.AddSID(gid++, 251, "periodsuperior");
			Instance.AddSID(gid++, 253, "asuperior");
			Instance.AddSID(gid++, 254, "bsuperior");
			Instance.AddSID(gid++, 255, "centsuperior");
			Instance.AddSID(gid++, 256, "dsuperior");
			Instance.AddSID(gid++, 257, "esuperior");
			Instance.AddSID(gid++, 258, "isuperior");
			Instance.AddSID(gid++, 259, "lsuperior");
			Instance.AddSID(gid++, 260, "msuperior");
			Instance.AddSID(gid++, 261, "nsuperior");
			Instance.AddSID(gid++, 262, "osuperior");
			Instance.AddSID(gid++, 263, "rsuperior");
			Instance.AddSID(gid++, 264, "ssuperior");
			Instance.AddSID(gid++, 265, "tsuperior");
			Instance.AddSID(gid++, 266, "ff");
			Instance.AddSID(gid++, 109, "fi");
			Instance.AddSID(gid++, 110, "fl");
			Instance.AddSID(gid++, 267, "ffi");
			Instance.AddSID(gid++, 268, "ffl");
			Instance.AddSID(gid++, 269, "parenleftinferior");
			Instance.AddSID(gid++, 270, "parenrightinferior");
			Instance.AddSID(gid++, 272, "hyphensuperior");
			Instance.AddSID(gid++, 300, "colonmonetary");
			Instance.AddSID(gid++, 301, "onefitted");
			Instance.AddSID(gid++, 302, "rupiah");
			Instance.AddSID(gid++, 305, "centoldstyle");
			Instance.AddSID(gid++, 314, "figuredash");
			Instance.AddSID(gid++, 315, "hypheninferior");
			Instance.AddSID(gid++, 158, "onequarter");
			Instance.AddSID(gid++, 155, "onehalf");
			Instance.AddSID(gid++, 163, "threequarters");
			Instance.AddSID(gid++, 320, "oneeighth");
			Instance.AddSID(gid++, 321, "threeeighths");
			Instance.AddSID(gid++, 322, "fiveeighths");
			Instance.AddSID(gid++, 323, "seveneighths");
			Instance.AddSID(gid++, 324, "onethird");
			Instance.AddSID(gid++, 325, "twothirds");
			Instance.AddSID(gid++, 326, "zerosuperior");
			Instance.AddSID(gid++, 150, "onesuperior");
			Instance.AddSID(gid++, 164, "twosuperior");
			Instance.AddSID(gid++, 169, "threesuperior");
			Instance.AddSID(gid++, 327, "foursuperior");
			Instance.AddSID(gid++, 328, "fivesuperior");
			Instance.AddSID(gid++, 329, "sixsuperior");
			Instance.AddSID(gid++, 330, "sevensuperior");
			Instance.AddSID(gid++, 331, "eightsuperior");
			Instance.AddSID(gid++, 332, "ninesuperior");
			Instance.AddSID(gid++, 333, "zeroinferior");
			Instance.AddSID(gid++, 334, "oneinferior");
			Instance.AddSID(gid++, 335, "twoinferior");
			Instance.AddSID(gid++, 336, "threeinferior");
			Instance.AddSID(gid++, 337, "fourinferior");
			Instance.AddSID(gid++, 338, "fiveinferior");
			Instance.AddSID(gid++, 339, "sixinferior");
			Instance.AddSID(gid++, 340, "seveninferior");
			Instance.AddSID(gid++, 341, "eightinferior");
			Instance.AddSID(gid++, 342, "nineinferior");
			Instance.AddSID(gid++, 343, "centinferior");
			Instance.AddSID(gid++, 344, "dollarinferior");
			Instance.AddSID(gid++, 345, "periodinferior");
			Instance.AddSID(gid++, 346, "commainferior");
		}
	}
}