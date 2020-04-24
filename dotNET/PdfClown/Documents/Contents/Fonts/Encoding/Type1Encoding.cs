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
using PdfClown.Objects;
using PdfClown.Documents.Contents.Fonts.AFM;

using System.Collections.Generic;

namespace PdfClown.Documents.Contents.Fonts
{


	/**
     * An encoding for a Type 1 font.
     */
	internal class Type1Encoding : Encoding
	{
		/**
		 * Creates an encoding from the given FontBox encoding.
		 *
		 * @param encoding FontBox encoding
		 */
		public static Type1Encoding FromFontBox(Encoding encoding)
		{
			// todo: could optimise this by looking for specific subclasses
			Dictionary<int, string> codeToName = encoding.CodeToNameMap;
			Type1Encoding enc = new Type1Encoding();
			foreach (var entry in codeToName)
				enc.Put(entry.Key, entry.Value);
			return enc;
		}

		/**
		 * Creates an empty encoding.
		 */
		public Type1Encoding()
		{
		}

		/**
		 * Creates an encoding from the given AFM font metrics.
		 *
		 * @param fontMetrics AFM font metrics.
		 */
		public Type1Encoding(FontMetrics fontMetrics)
		{
			foreach (CharMetric nextMetric in fontMetrics.CharMetrics)
			{
				Put(nextMetric.CharacterCode, nextMetric.Name);
			}
		}

		public override PdfDirectObject GetPdfObject()
		{
			return null;
		}

	}
}
