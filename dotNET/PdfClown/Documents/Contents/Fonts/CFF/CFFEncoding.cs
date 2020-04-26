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

namespace PdfClown.Documents.Contents.Fonts.CCF
{
    /**
     * A CFF Type 1-equivalent Encoding. An encoding is an array of codes associated with some or all
     * glyphs in a font
     *
     * @author John Hewson
     */
    public abstract class CFFEncoding : Encoding
    {
        private new readonly Dictionary<int, string> codeToName = new Dictionary<int, string>(250);

        /**
		 * Package-private constructor for subclasses.
		 */
        public CFFEncoding()
        { }

        /**
		 * Returns the name of the glyph for the given character code.
		 *
		 * @param code character code
		 * @return PostScript glyph name
		 */
        public override string GetName(int code)
        {
            if (!codeToName.TryGetValue(code, out var name))
            {
                return ".notdef";
            }
            return name;
        }

        /**
		 * Adds a new code/SID combination to the encoding.
		 * @param code the given code
		 * @param sid the given SID
		 */
        public void Add(int code, int sid, string name)
        {
            codeToName[code] = name;
            Put(code, name);
        }

        /**
		 * For use by subclasses only.
		 */
        protected void Add(int code, int sid)
        {
            string name = CFFStandardString.GetName(sid);
            codeToName[code] = name;
            Put(code, name);
        }
    }
}