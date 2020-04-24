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

using System.Collections.Generic;

namespace PdfClown.Documents.Contents.Fonts
{

	/**
     * This is the Mac OS Roman encoding, which is similar to the
     * MacRomanEncoding with the addition of 15 entries
     */
	internal class MacOSRomanEncoding : MacRomanEncoding
	{
		/**
		 * Constructor.
		 */
		public MacOSRomanEncoding() : base()
		{
			Put(255, "notequal");
			Put(260, "infinity");
			Put(262, "lessequal");
			Put(263, "greaterequal");
			Put(266, "partialdiff");
			Put(267, "summation");
			Put(270, "product");
			Put(271, "pi");
			Put(272, "integral");
			Put(275, "Omega");
			Put(303, "radical");
			Put(305, "approxequal");
			Put(306, "Delta");
			Put(327, "lozenge");
			Put(333, "Euro");
			Put(360, "apple");
		}

		public override PdfDirectObject GetPdfObject()
		{
			return null;
		}
	}
}