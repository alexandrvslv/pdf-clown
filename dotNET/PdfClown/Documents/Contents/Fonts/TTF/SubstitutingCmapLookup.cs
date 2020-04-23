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
namespace PdfClown.Documents.Contents.Fonts.TTF
{

    using System.Collections.Generic;

    /**
     * A cmap lookup that performs substitution via the 'GSUB' table.
     *
     * @author Aaron Madlon-Kay
     */
    public class SubstitutingCmapLookup : ICmapLookup
    {
        private readonly CmapSubtable cmap;
        private readonly GlyphSubstitutionTable gsub;
        private readonly List<string> enabledFeatures;

        public SubstitutingCmapLookup(CmapSubtable cmap, GlyphSubstitutionTable gsub,
                List<string> enabledFeatures)
        {
            this.cmap = cmap;
            this.gsub = gsub;
            this.enabledFeatures = enabledFeatures;
        }

        public override int GetGlyphId(int characterCode)
        {
            int gid = cmap.GetGlyphId(characterCode);
            string[] scriptTags = OpenTypeScript.getScriptTags(characterCode);
            return gsub.GetSubstitution(gid, scriptTags, enabledFeatures);
        }

        public override List<int> GetCharCodes(int gid)
        {
            return cmap.GetCharCodes(gsub.GetUnsubstitution(gid));
        }
    }
}