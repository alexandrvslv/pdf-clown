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
using PdfClown.Util;

using System;
using System.Collections.Generic;

namespace PdfClown.Documents.Contents.Fonts
{
    /**
     * A font's built-in encoding.
     *
     * @author John Hewson
     */
    internal class BuiltInEncoding : Encoding
    {
        /**
         * Constructor.
         *
         * @param codeToName the given code to name mapping
         */
        public BuiltInEncoding(Dictionary<int, string> codeToName)
        {
            foreach (var item in codeToName)
                Put(item.Key, item.Value);
        }

        public override PdfDirectObject GetPdfObject()
        {
            throw new NotSupportedException("Built-in encodings cannot be serialized");
        }
    }
}
