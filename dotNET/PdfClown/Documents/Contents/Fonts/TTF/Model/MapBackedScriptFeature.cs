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

namespace PdfClown.Documents.Contents.Fonts.TTF.Model
{

    /**
     * 
     * A {@link Dictionary} based simple implementation of the {@link ScriptFeature}
     * 
     * @author Palash Ray
     *
     */
    public class MapBackedScriptFeature : ScriptFeature
    {

        private readonly string name;
        private readonly Dictionary<List<int>, int> featureMap;

        public MapBackedScriptFeature(string name, Dictionary<List<int>, int> featureMap)
        {
            this.name = name;
            this.featureMap = featureMap;
        }

        public string Name
        {
            get => name;
        }

        public ICollection<List<int>> AllGlyphIdsForSubstitution
        {
            get => featureMap.Keys;
        }

        public bool CanReplaceGlyphs(List<int> glyphIds)
        {
            return featureMap.ContainsKey(glyphIds);
        }

        public int GetReplacementForGlyphs(List<int> glyphIds)
        {
            if (!CanReplaceGlyphs(glyphIds))
            {
                throw new NotSupportedException("The glyphs " + glyphIds + " cannot be replaced");
            }
            return featureMap[glyphIds];
        }

        public override int GetHashCode()
        {
            const int prime = 31;
            int result = 1;
            result = prime * result + ((featureMap == null) ? 0 : featureMap.GetHashCode());
            result = prime * result + ((name == null) ? 0 : name.GetHashCode());
            return result;
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }
            if (obj == null)
            {
                return false;
            }
            if (GetType() != obj.GetType())
            {
                return false;
            }
            MapBackedScriptFeature other = (MapBackedScriptFeature)obj;
            if (featureMap == null)
            {
                if (other.featureMap != null)
                {
                    return false;
                }
            }
            else if (!featureMap.Equals(other.featureMap))
            {
                return false;
            }
            if (name == null)
            {
                if (other.name != null)
                {
                    return false;
                }
            }
            else if (!name.Equals(other.name, StringComparison.Ordinal))
            {
                return false;
            }
            return true;
        }
    }
}