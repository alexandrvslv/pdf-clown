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

namespace PdfClown.Documents.Contents.Fonts.TTF.Model
{
    using System;
    using System.Collections.Generic;

    /**
     * 
     * A {@link Dictionary} based simple implementation of the {@link GsubData}
     * 
     * @author Palash Ray
     *
     */
    public class MapBackedGsubData : GsubData
    {
        private readonly Language language;
        private readonly string activeScriptName;
        private readonly Dictionary<string, Dictionary<List<int>, int>> glyphSubstitutionMap;

        public MapBackedGsubData(Language language, string activeScriptName,
                Dictionary<string, Dictionary<List<int>, int>> glyphSubstitutionMap)
        {
            this.language = language;
            this.activeScriptName = activeScriptName;
            this.glyphSubstitutionMap = glyphSubstitutionMap;
        }

        public Language Language
        {
            get => language;
        }

        public string ActiveScriptName
        {
            get => activeScriptName;
        }

        public ICollection<string> SupportedFeatures
        {
            get => glyphSubstitutionMap.Keys;
        }


        public bool IsFeatureSupported(string featureName)
        {
            return glyphSubstitutionMap.ContainsKey(featureName);
        }

        public ScriptFeature GetFeature(string featureName)
        {
            if (!IsFeatureSupported(featureName))
            {
                throw new NotSupportedException("The feature " + featureName + " is not supported!");
            }

            return new MapBackedScriptFeature(featureName, glyphSubstitutionMap[featureName]);
        }


    }
}
