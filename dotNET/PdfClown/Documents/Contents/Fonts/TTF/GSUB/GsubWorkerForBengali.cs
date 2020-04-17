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

using PdfClown.Documents.Contents.Fonts.TTF.Model;
using PdfClown.Util.Collections.Generic;
using System.Collections.Generic;
using System.Diagnostics;

namespace PdfClown.Documents.Contents.Fonts.TTF.GSUB
{

    /**
     * 
     * Bengali-specific implementation of GSUB system
     * 
     * @author Palash Ray
     *
     */
    public class GsubWorkerForBengali : GsubWorker
    {

        //private static readonly Log LOG = LogFactory.getLog(GsubWorkerForBengali.class);

        private static readonly string INIT_FEATURE = "init";

        /**
         * This sequence is very important. This has been taken from <a href=
         * "https://docs.microsoft.com/en-us/typography/script-development/bengali">https://docs.microsoft.com/en-us/typography/script-development/bengali</a>
         */
        private static readonly List<string> FEATURES_IN_ORDER = new List<string>{"locl", "nukt", "akhn",
                "rphf", "blwf", "pstf", "half", "vatu", "cjct", INIT_FEATURE, "pres", "abvs", "blws",
                "psts", "haln", "calt" };

        private static readonly char[] BEFORE_HALF_CHARS = new char[] { '\u09BF', '\u09C7', '\u09C8' };
        private static readonly BeforeAndAfterSpanComponent[] BEFORE_AND_AFTER_SPAN_CHARS = new BeforeAndAfterSpanComponent[] {
            new BeforeAndAfterSpanComponent('\u09CB', '\u09C7', '\u09BE'),
            new BeforeAndAfterSpanComponent('\u09CC', '\u09C7', '\u09D7') };

        private readonly CmapLookup cmapLookup;
        private readonly GsubData gsubData;

        private readonly List<int> beforeHalfGlyphIds;
        private readonly Dictionary<int, BeforeAndAfterSpanComponent> beforeAndAfterSpanGlyphIds;


        public GsubWorkerForBengali(CmapLookup cmapLookup, GsubData gsubData)
        {
            this.cmapLookup = cmapLookup;
            this.gsubData = gsubData;
            beforeHalfGlyphIds = GetBeforeHalfGlyphIds();
            beforeAndAfterSpanGlyphIds = GetBeforeAndAfterSpanGlyphIds();
        }

        public List<int> ApplyTransforms(List<int> originalGlyphIds)
        {
            List<int> intermediateGlyphsFromGsub = originalGlyphIds;

            foreach (string feature in FEATURES_IN_ORDER)
            {
                if (!gsubData.IsFeatureSupported(feature))
                {
                    Debug.WriteLine("debug: the feature " + feature + " was not found");
                    continue;
                }

                Debug.WriteLine("debug: applying the feature " + feature);

                ScriptFeature scriptFeature = gsubData.GetFeature(feature);

                intermediateGlyphsFromGsub = ApplyGsubFeature(scriptFeature, intermediateGlyphsFromGsub);
            }

            return RepositionGlyphs(intermediateGlyphsFromGsub);
        }

        private List<int> RepositionGlyphs(List<int> originalGlyphIds)
        {
            List<int> glyphsRepositionedByBeforeHalf = RepositionBeforeHalfGlyphIds(
                    originalGlyphIds);
            return RepositionBeforeAndAfterSpanGlyphIds(glyphsRepositionedByBeforeHalf);
        }

        private List<int> RepositionBeforeHalfGlyphIds(List<int> originalGlyphIds)
        {
            List<int> repositionedGlyphIds = new List<int>(originalGlyphIds);

            for (int index = 1; index < originalGlyphIds.Count; index++)
            {
                int glyphId = originalGlyphIds[index];
                if (beforeHalfGlyphIds.Contains(glyphId))
                {
                    int previousGlyphId = originalGlyphIds[index - 1];
                    repositionedGlyphIds[index] = previousGlyphId;
                    repositionedGlyphIds[index - 1] = glyphId;
                }
            }
            return repositionedGlyphIds;
        }

        private List<int> RepositionBeforeAndAfterSpanGlyphIds(List<int> originalGlyphIds)
        {
            List<int> repositionedGlyphIds = new List<int>(originalGlyphIds);

            for (int index = 1; index < originalGlyphIds.Count; index++)
            {
                int glyphId = originalGlyphIds[index];
                if (beforeAndAfterSpanGlyphIds.TryGetValue(glyphId, out BeforeAndAfterSpanComponent beforeAndAfterSpanComponent))
                {
                    int previousGlyphId = originalGlyphIds[index - 1];
                    repositionedGlyphIds[index] = previousGlyphId;
                    repositionedGlyphIds[index - 1] = GetGlyphId(beforeAndAfterSpanComponent.beforeComponentCharacter);
                    repositionedGlyphIds[index + 1] = GetGlyphId(beforeAndAfterSpanComponent.afterComponentCharacter);
                }
            }
            return repositionedGlyphIds;
        }

        private List<int> ApplyGsubFeature(ScriptFeature scriptFeature, List<int> originalGlyphs)
        {
            GlyphArraySplitter glyphArraySplitter = new GlyphArraySplitterRegexImpl(scriptFeature.AllGlyphIdsForSubstitution);

            List<List<int>> tokens = glyphArraySplitter.Split(originalGlyphs);

            List<int> gsubProcessedGlyphs = new List<int>();

            foreach (List<int> chunk in tokens)
            {
                if (scriptFeature.CanReplaceGlyphs(chunk))
                {
                    // gsub system kicks in, you get the glyphId directly
                    int glyphId = scriptFeature.GetReplacementForGlyphs(chunk);
                    gsubProcessedGlyphs.Add(glyphId);
                }
                else
                {
                    gsubProcessedGlyphs.AddAll(chunk);
                }
            }

            Debug.WriteLine($"debug: originalGlyphs: {originalGlyphs}, gsubProcessedGlyphs: {gsubProcessedGlyphs}");

            return gsubProcessedGlyphs;
        }

        private List<int> GetBeforeHalfGlyphIds()
        {
            List<int> glyphIds = new List<int>();

            foreach (char character in BEFORE_HALF_CHARS)
            {
                glyphIds.Add(GetGlyphId(character));
            }

            if (gsubData.IsFeatureSupported(INIT_FEATURE))
            {
                ScriptFeature feature = gsubData.GetFeature(INIT_FEATURE);
                foreach (List<int> glyphCluster in feature.AllGlyphIdsForSubstitution)
                {
                    glyphIds.Add(feature.GetReplacementForGlyphs(glyphCluster));
                }
            }

            return glyphIds;

        }

        private int GetGlyphId(char character)
        {
            return cmapLookup.GetGlyphId(character);
        }

        private Dictionary<int, BeforeAndAfterSpanComponent> GetBeforeAndAfterSpanGlyphIds()
        {
            Dictionary<int, BeforeAndAfterSpanComponent> result = new Dictionary<int, BeforeAndAfterSpanComponent>(BEFORE_AND_AFTER_SPAN_CHARS.Length);

            foreach (BeforeAndAfterSpanComponent beforeAndAfterSpanComponent in BEFORE_AND_AFTER_SPAN_CHARS)
            {
                result[GetGlyphId(beforeAndAfterSpanComponent.originalCharacter)] = beforeAndAfterSpanComponent;
            }

            return result;
        }

        /**
         * Models characters like O-kar (\u09CB) and OU-kar (\u09CC). Since these 2 characters is
         * represented by 2 components, one before and one after the Vyanjan Varna on which this is
         * used, this glyph has to be replaced by these 2 glyphs. For O-kar, it has to be replaced by
         * E-kar (\u09C7) and AA-kar (\u09BE). For OU-kar, it has be replaced by E-kar (\u09C7) and
         * \u09D7.
         *
         */
        private class BeforeAndAfterSpanComponent
        {
            internal readonly char originalCharacter;
            internal readonly char beforeComponentCharacter;
            internal readonly char afterComponentCharacter;

            public BeforeAndAfterSpanComponent(char originalCharacter, char beforeComponentCharacter, char afterComponentCharacter)
            {
                this.originalCharacter = originalCharacter;
                this.beforeComponentCharacter = beforeComponentCharacter;
                this.afterComponentCharacter = afterComponentCharacter;
            }

        }

    }
}