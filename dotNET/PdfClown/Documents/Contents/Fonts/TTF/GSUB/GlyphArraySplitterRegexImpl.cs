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
using System;
using System.Collections.Generic;
using System.Text;

namespace PdfClown.Documents.Contents.Fonts.TTF.GSUB
{

    /**
     * This is an in-efficient implementation based on regex, which helps split the array.
     * 
     * @author Palash Ray
     *
     */
    public class GlyphArraySplitterRegexImpl : GlyphArraySplitter
    {
        private static readonly char[] GLYPH_ID_SEPARATOR = new[] { '_' };

        private readonly CompoundCharacterTokenizer compoundCharacterTokenizer;

        public GlyphArraySplitterRegexImpl(ICollection<List<int>> matchers)
        {
            compoundCharacterTokenizer = new CompoundCharacterTokenizer(GetMatchersAsStrings(matchers));
        }

        public List<List<int>> Split(List<int> glyphIds)
        {
            string originalGlyphsAsText = ConvertGlyphIdsToString(glyphIds);
            List<string> tokens = compoundCharacterTokenizer.Tokenize(originalGlyphsAsText);

            List<List<int>> modifiedGlyphs = new List<List<int>>();
            foreach (var token in tokens) modifiedGlyphs.Add(ConvertGlyphIdsToList(token));
            return modifiedGlyphs;
        }

        private ISet<string> GetMatchersAsStrings(ICollection<List<int>> matchers)
        {
            ISet<string> stringMatchers = new HashSet<string>();
            foreach (var glyphIds in matchers) stringMatchers.Add(ConvertGlyphIdsToString(glyphIds));
            return stringMatchers;
        }

        private string ConvertGlyphIdsToString(List<int> glyphIds)
        {
            StringBuilder sb = new StringBuilder(20);
            sb.Append(GLYPH_ID_SEPARATOR);
            foreach (var glyphId in glyphIds) sb.Append(glyphId).Append(GLYPH_ID_SEPARATOR);
            return sb.ToString();
        }

        private List<int> ConvertGlyphIdsToList(string glyphIdsAsString)
        {
            List<int> gsubProcessedGlyphsIds = new List<int>();

            foreach (string glyphId in glyphIdsAsString.Split(GLYPH_ID_SEPARATOR, StringSplitOptions.RemoveEmptyEntries))
            {
                if (glyphId.Trim().Length == 0)
                {
                    continue;
                }
                gsubProcessedGlyphsIds.Add(int.Parse(glyphId));
            }

            return gsubProcessedGlyphsIds;
        }

    }
}