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
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace PdfClown.Documents.Contents.Fonts.TTF.GSUB
{
    /**
     * Takes in the given text having compound-glyphs to substitute, and splits it into chunks consisting of parts that
     * should be substituted and the ones that can be processed normally.
     * 
     * @author Palash Ray
     * 
     */
    public class CompoundCharacterTokenizer
    {

        private readonly Regex regexExpression;

        public CompoundCharacterTokenizer(ISet<string> compoundWords)
        {
            regexExpression = new Regex(GetRegexFromTokens(compoundWords), RegexOptions.Compiled);
        }

        public CompoundCharacterTokenizer(string singleRegex)
        {
            regexExpression = new Regex(singleRegex, RegexOptions.Compiled);
        }

        public List<string> Tokenize(string text)
        {
            List<string> tokens = new List<string>();

            var regexMatcher = regexExpression.Matches(text);

            int lastIndexOfPrevMatch = 0;

            foreach (Match match in regexMatcher)
            {
                string prevToken = text.Substring(lastIndexOfPrevMatch, match.Index - lastIndexOfPrevMatch);

                if (prevToken.Length > 0)
                {
                    tokens.Add(prevToken);
                }

                tokens.Add(match.Value);
                lastIndexOfPrevMatch = match.Index + match.Length;
            }

            string tail = text.Substring(lastIndexOfPrevMatch, text.Length - lastIndexOfPrevMatch);

            if (tail.Length > 0)
            {
                tokens.Add(tail);
            }

            return tokens;
        }

        private string GetRegexFromTokens(ISet<string> compoundWords)
        {
            StringBuilder sb = new StringBuilder();

            foreach (string compoundWord in compoundWords)
            {
                sb.Append("(");
                sb.Append(compoundWord);
                sb.Append(")|");
            }

            sb.Length = sb.Length - 1;

            return sb.ToString();
        }

    }
}