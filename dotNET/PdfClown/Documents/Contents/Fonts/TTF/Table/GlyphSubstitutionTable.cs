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
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using PdfClown.Documents.Contents.Fonts.TTF.Table.Common;
using PdfClown.Documents.Contents.Fonts.TTF.Model;
using PdfClown.Documents.Contents.Fonts.TTF.GSUB;
using System;
using PdfClown.Documents.Contents.Fonts.TTF.Table.GSUB;
using System.Linq;

namespace PdfClown.Documents.Contents.Fonts.TTF
{
    /**
     * A glyph substitution 'GSUB' table in a TrueType or OpenType font.
     *
     * @author Aaron Madlon-Kay
     */
    public class GlyphSubstitutionTable : TTFTable
    {
        //private static readonly Log LOG = LogFactory.getLog(GlyphSubstitutionTable.class);

        public static readonly string TAG = "GSUB";

        private Dictionary<string, ScriptTable> scriptList;
        // featureList and lookupList are not maps because we need to index into them
        private FeatureListTable featureListTable;
        private LookupListTable lookupListTable;

        private readonly Dictionary<int, int> lookupCache = new Dictionary<int, int>();
        private readonly Dictionary<int, int> reverseLookup = new Dictionary<int, int>();

        private string lastUsedSupportedScript;

        private GsubData gsubData;

        public GlyphSubstitutionTable(TrueTypeFont font) : base(font)
        {
        }

        public GsubData GsubData
        {
            get => gsubData;
        }

        //@SuppressWarnings({"squid:S1854"})
        protected override void Read(TrueTypeFont ttf, TTFDataStream data)
        {
            long start = data.CurrentPosition;
            //@SuppressWarnings({"unused"})
            int majorVersion = data.ReadUnsignedShort();
            int minorVersion = data.ReadUnsignedShort();
            int scriptListOffset = data.ReadUnsignedShort();
            int featureListOffset = data.ReadUnsignedShort();
            int lookupListOffset = data.ReadUnsignedShort();
            //@SuppressWarnings({"unused"})
            long featureVariationsOffset = -1L;
            if (minorVersion == 1L)
            {
                featureVariationsOffset = data.ReadUnsignedInt();
            }

            scriptList = ReadScriptList(data, start + scriptListOffset);
            featureListTable = ReadFeatureList(data, start + featureListOffset);
            lookupListTable = ReadLookupList(data, start + lookupListOffset);

            var glyphSubstitutionDataExtractor = new GlyphSubstitutionDataExtractor();

            gsubData = glyphSubstitutionDataExtractor.GetGsubData(scriptList, featureListTable, lookupListTable);
        }

        private Dictionary<string, ScriptTable> ReadScriptList(TTFDataStream data, long offset)
        {
            data.seek(offset);
            int scriptCount = data.ReadUnsignedShort();
            ScriptTable[] scriptTables = new ScriptTable[scriptCount];
            int[] scriptOffsets = new int[scriptCount];
            string[] scriptTags = new string[scriptCount];
            for (int i = 0; i < scriptCount; i++)
            {
                scriptTags[i] = data.ReadString(4);
                scriptOffsets[i] = data.ReadUnsignedShort();
            }
            for (int i = 0; i < scriptCount; i++)
            {
                scriptTables[i] = ReadScriptTable(data, offset + scriptOffsets[i]);
            }
            var resultScriptList = new Dictionary<string, ScriptTable>(scriptCount, StringComparer.Ordinal);
            for (int i = 0; i < scriptCount; i++)
            {
                ScriptRecord scriptRecord = new ScriptRecord(scriptTags[i], scriptTables[i]);
                resultScriptList[scriptRecord.ScriptTag] = scriptRecord.ScriptTable;
            }
            return resultScriptList;
        }

        private ScriptTable ReadScriptTable(TTFDataStream data, long offset)
        {
            data.seek(offset);
            int defaultLangSys = data.ReadUnsignedShort();
            int langSysCount = data.ReadUnsignedShort();
            LangSysRecord[] langSysRecords = new LangSysRecord[langSysCount];
            string[] langSysTags = new string[langSysCount];
            int[] langSysOffsets = new int[langSysCount];
            for (int i = 0; i < langSysCount; i++)
            {
                langSysTags[i] = data.ReadString(4);
                if (i > 0 && langSysTags[i].CompareTo(langSysTags[i - 1]) <= 0)
                {
                    // PDFBOX-4489: catch corrupt file
                    // https://docs.microsoft.com/en-us/typography/opentype/spec/chapter2#slTbl_sRec
                    Debug.WriteLine("error: LangSysRecords not alphabetically sorted by LangSys tag: " +
                              langSysTags[i] + " <= " + langSysTags[i - 1]);
                    return new ScriptTable(null, new Dictionary<string, LangSysTable>(StringComparer.Ordinal));
                }
                langSysOffsets[i] = data.ReadUnsignedShort();
            }

            LangSysTable defaultLangSysTable = null;

            if (defaultLangSys != 0)
            {
                defaultLangSysTable = ReadLangSysTable(data, offset + defaultLangSys);
            }
            for (int i = 0; i < langSysCount; i++)
            {
                LangSysTable langSysTable = ReadLangSysTable(data, offset + langSysOffsets[i]);
                langSysRecords[i] = new LangSysRecord(langSysTags[i], langSysTable);
            }
            Dictionary<string, LangSysTable> langSysTables = new Dictionary<string, LangSysTable>(langSysCount, StringComparer.Ordinal);
            foreach (LangSysRecord langSysRecord in langSysRecords)
            {
                langSysTables[langSysRecord.LangSysTag] = langSysRecord.LangSysTable;
            }
            return new ScriptTable(defaultLangSysTable, langSysTables);
        }

        private LangSysTable ReadLangSysTable(TTFDataStream data, long offset)
        {
            data.seek(offset);
            int lookupOrder = data.ReadUnsignedShort();
            int requiredFeatureIndex = data.ReadUnsignedShort();
            int featureIndexCount = data.ReadUnsignedShort();
            int[] featureIndices = new int[featureIndexCount];
            for (int i = 0; i < featureIndexCount; i++)
            {
                featureIndices[i] = data.ReadUnsignedShort();
            }
            return new LangSysTable(lookupOrder, requiredFeatureIndex, featureIndexCount,
                    featureIndices);
        }

        private FeatureListTable ReadFeatureList(TTFDataStream data, long offset)
        {
            data.seek(offset);
            int featureCount = data.ReadUnsignedShort();
            FeatureRecord[] featureRecords = new FeatureRecord[featureCount];
            int[] featureOffsets = new int[featureCount];
            string[] featureTags = new string[featureCount];
            for (int i = 0; i < featureCount; i++)
            {
                featureTags[i] = data.ReadString(4);
                if (i > 0 && featureTags[i].CompareTo(featureTags[i - 1]) < 0)
                {
                    // catch corrupt file
                    // https://docs.microsoft.com/en-us/typography/opentype/spec/chapter2#flTbl
                    Debug.WriteLine("warning: FeatureRecord array not alphabetically sorted by FeatureTag: " +
                              featureTags[i] + " < " + featureTags[i - 1]);
                    return new FeatureListTable(0, new FeatureRecord[0]);
                }
                featureOffsets[i] = data.ReadUnsignedShort();
            }
            for (int i = 0; i < featureCount; i++)
            {
                FeatureTable featureTable = ReadFeatureTable(data, offset + featureOffsets[i]);
                featureRecords[i] = new FeatureRecord(featureTags[i], featureTable);
            }
            return new FeatureListTable(featureCount, featureRecords);
        }

        private FeatureTable ReadFeatureTable(TTFDataStream data, long offset)
        {
            data.seek(offset);
            int featureParams = data.ReadUnsignedShort();
            int lookupIndexCount = data.ReadUnsignedShort();
            int[] lookupListIndices = new int[lookupIndexCount];
            for (int i = 0; i < lookupIndexCount; i++)
            {
                lookupListIndices[i] = data.ReadUnsignedShort();
            }
            return new FeatureTable(featureParams, lookupIndexCount, lookupListIndices);
        }

        private LookupListTable ReadLookupList(TTFDataStream data, long offset)
        {
            data.seek(offset);
            int lookupCount = data.ReadUnsignedShort();
            int[] lookups = new int[lookupCount];
            for (int i = 0; i < lookupCount; i++)
            {
                lookups[i] = data.ReadUnsignedShort();
            }
            LookupTable[] lookupTables = new LookupTable[lookupCount];
            for (int i = 0; i < lookupCount; i++)
            {
                lookupTables[i] = ReadLookupTable(data, offset + lookups[i]);
            }
            return new LookupListTable(lookupCount, lookupTables);
        }

        private LookupTable ReadLookupTable(TTFDataStream data, long offset)
        {
            data.seek(offset);
            int lookupType = data.ReadUnsignedShort();
            int lookupFlag = data.ReadUnsignedShort();
            int subTableCount = data.ReadUnsignedShort();
            int[] subTableOffets = new int[subTableCount];
            for (int i = 0; i < subTableCount; i++)
            {
                subTableOffets[i] = data.ReadUnsignedShort();
            }

            int markFilteringSet;
            if ((lookupFlag & 0x0010) != 0)
            {
                markFilteringSet = data.ReadUnsignedShort();
            }
            else
            {
                markFilteringSet = 0;
            }
            LookupSubTable[] subTables = new LookupSubTable[subTableCount];
            switch (lookupType)
            {
                case 1:
                    // Single
                    // https://docs.microsoft.com/en-us/typography/opentype/spec/gsub#SS
                    for (int i = 0; i < subTableCount; i++)
                    {
                        subTables[i] = ReadLookupSubTable(data, offset + subTableOffets[i]);
                    }
                    break;
                case 4:
                    // Ligature Substitution Subtable
                    // https://docs.microsoft.com/en-us/typography/opentype/spec/gsub#LS
                    for (int i = 0; i < subTableCount; i++)
                    {
                        subTables[i] = ReadLigatureSubstitutionSubtable(data, offset + subTableOffets[i]);
                    }
                    break;
                default:
                    // Other lookup types are not supported
                    Debug.WriteLine("debug: Type " + lookupType
                            + " GSUB lookup table is not supported and will be ignored");
                    break;
            }
            return new LookupTable(lookupType, lookupFlag, markFilteringSet, subTables);
        }

        private LookupSubTable ReadLookupSubTable(TTFDataStream data, long offset)
        {
            data.seek(offset);
            int substFormat = data.ReadUnsignedShort();
            switch (substFormat)
            {
                case 1:
                    {
                        // LookupType 1: Single Substitution Subtable
                        // https://docs.microsoft.com/en-us/typography/opentype/spec/gsub#11-single-substitution-format-1
                        int coverageOffset = data.ReadUnsignedShort();
                        short deltaGlyphID = data.ReadSignedShort();
                        CoverageTable coverageTable = ReadCoverageTable(data, offset + coverageOffset);
                        return new LookupTypeSingleSubstFormat1(substFormat, coverageTable, deltaGlyphID);
                    }
                case 2:
                    {
                        // Single Substitution Format 2
                        // https://docs.microsoft.com/en-us/typography/opentype/spec/gsub#12-single-substitution-format-2
                        int coverageOffset = data.ReadUnsignedShort();
                        int glyphCount = data.ReadUnsignedShort();
                        int[] substituteGlyphIDs = new int[glyphCount];
                        for (int i = 0; i < glyphCount; i++)
                        {
                            substituteGlyphIDs[i] = data.ReadUnsignedShort();
                        }
                        CoverageTable coverageTable = ReadCoverageTable(data, offset + coverageOffset);
                        return new LookupTypeSingleSubstFormat2(substFormat, coverageTable, substituteGlyphIDs);
                    }
                default:
                    throw new IOException("Unknown substFormat: " + substFormat);
            }
        }

        private LookupSubTable ReadLigatureSubstitutionSubtable(TTFDataStream data, long offset)
        {
            data.seek(offset);
            int substFormat = data.ReadUnsignedShort();

            if (substFormat != 1)
            {
                throw new IOException(
                        "The expected SubstFormat for LigatureSubstitutionTable is 1");
            }

            int coverage = data.ReadUnsignedShort();
            int ligSetCount = data.ReadUnsignedShort();

            int[] ligatureOffsets = new int[ligSetCount];

            for (int i = 0; i < ligSetCount; i++)
            {
                ligatureOffsets[i] = data.ReadUnsignedShort();
            }

            CoverageTable coverageTable = ReadCoverageTable(data, offset + coverage);

            if (ligSetCount != coverageTable.Size)
            {
                throw new IOException(
                        "According to the OpenTypeFont specifications, the coverage count should be equal to the no. of LigatureSetTables");
            }

            LigatureSetTable[] ligatureSetTables = new LigatureSetTable[ligSetCount];

            for (int i = 0; i < ligSetCount; i++)
            {

                int coverageGlyphId = coverageTable.GetGlyphId(i);

                ligatureSetTables[i] = ReadLigatureSetTable(data,
                        offset + ligatureOffsets[i], coverageGlyphId);
            }

            return new LookupTypeLigatureSubstitutionSubstFormat1(substFormat, coverageTable,
                    ligatureSetTables);
        }

        private LigatureSetTable ReadLigatureSetTable(TTFDataStream data, long ligatureSetTableLocation,
                int coverageGlyphId)
        {
            data.seek(ligatureSetTableLocation);

            int ligatureCount = data.ReadUnsignedShort();
            Debug.WriteLine("debug: ligatureCount=" + ligatureCount);

            int[] ligatureOffsets = new int[ligatureCount];
            LigatureTable[] ligatureTables = new LigatureTable[ligatureCount];

            for (int i = 0; i < ligatureOffsets.Length; i++)
            {
                ligatureOffsets[i] = data.ReadUnsignedShort();
            }

            for (int i = 0; i < ligatureOffsets.Length; i++)
            {
                int ligatureOffset = ligatureOffsets[i];
                ligatureTables[i] = ReadLigatureTable(data,
                        ligatureSetTableLocation + ligatureOffset, coverageGlyphId);
            }

            return new LigatureSetTable(ligatureCount, ligatureTables);
        }

        private LigatureTable ReadLigatureTable(TTFDataStream data, long ligatureTableLocation,
                int coverageGlyphId)
        {
            data.seek(ligatureTableLocation);

            int ligatureGlyph = data.ReadUnsignedShort();

            int componentCount = data.ReadUnsignedShort();

            int[] componentGlyphIDs = new int[componentCount];

            if (componentCount > 0)
            {
                componentGlyphIDs[0] = coverageGlyphId;
            }

            for (int i = 1; i <= componentCount - 1; i++)
            {
                componentGlyphIDs[i] = data.ReadUnsignedShort();
            }

            return new LigatureTable(ligatureGlyph, componentCount, componentGlyphIDs);

        }

        private CoverageTable ReadCoverageTable(TTFDataStream data, long offset)
        {
            data.seek(offset);
            int coverageFormat = data.ReadUnsignedShort();
            switch (coverageFormat)
            {
                case 1:
                    {
                        int glyphCount = data.ReadUnsignedShort();
                        int[] glyphArray = new int[glyphCount];
                        for (int i = 0; i < glyphCount; i++)
                        {
                            glyphArray[i] = data.ReadUnsignedShort();
                        }
                        return new CoverageTableFormat1(coverageFormat, glyphArray);
                    }
                case 2:
                    {
                        int rangeCount = data.ReadUnsignedShort();
                        RangeRecord[] rangeRecords = new RangeRecord[rangeCount];


                        for (int i = 0; i < rangeCount; i++)
                        {
                            rangeRecords[i] = ReadRangeRecord(data);
                        }

                        return new CoverageTableFormat2(coverageFormat, rangeRecords);
                    }
                default:
                    // Should not happen (the spec indicates only format 1 and format 2)
                    throw new IOException("Unknown coverage format: " + coverageFormat);
            }
        }

        /**
         * Choose from one of the supplied OpenType script tags, depending on what the font supports and potentially on
         * context.
         *
         * @param tags
         * @return The best OpenType script tag
         */
        private string SelectScriptTag(string[] tags)
        {
            if (tags.Length == 1)
            {
                string tag = tags[0];
                if (OpenTypeScript.INHERITED.Equals(tag, StringComparison.Ordinal)
                        || (OpenTypeScript.TAG_DEFAULT.Equals(tag, StringComparison.Ordinal)
                            && !scriptList.ContainsKey(tag)))
                {
                    // We don't know what script this should be.
                    if (lastUsedSupportedScript == null)
                    {
                        // We have no past context and (currently) no way to get future context so we guess.
                        lastUsedSupportedScript = scriptList.keySet().iterator().next();
                    }
                    // else use past context

                    return lastUsedSupportedScript;
                }
            }
            foreach (string tag in tags)
            {
                if (scriptList.ContainsKey(tag))
                {
                    // Use the first recognized tag. We assume a single font only recognizes one version ("ver. 2")
                    // of a single script, or if it recognizes more than one that it prefers the latest one.
                    lastUsedSupportedScript = tag;
                    return lastUsedSupportedScript;
                }
            }
            return tags[0];
        }

        private ICollection<LangSysTable> GetLangSysTables(string scriptTag)
        {
            ICollection<LangSysTable> result = new List<LangSysTable>(0);
            if (scriptList.TryGetValue(scriptTag, out ScriptTable scriptTable))
            {
                if (scriptTable.DefaultLangSysTable == null)
                {
                    result = scriptTable.LangSysTables.Values;
                }
                else
                {
                    result = new List<LangSysTable>(scriptTable.LangSysTables.Values);
                    result.Add(scriptTable.DefaultLangSysTable);
                }
            }
            return result;
        }

        /**
         * Get a list of {@code FeatureRecord}s from a collection of {@code LangSysTable}s. Optionally
         * filter the returned features by supplying a list of allowed feature tags in
         * {@code enabledFeatures}.
         *
         * Note that features listed as required ({@code LangSysTable#requiredFeatureIndex}) will be
         * included even if not explicitly enabled.
         *
         * @param langSysTables The {@code LangSysTable}s indicating {@code FeatureRecord}s to search
         * for
         * @param enabledFeatures An optional whitelist of feature tags ({@code null} to allow all)
         * @return The indicated {@code FeatureRecord}s
         */
        private List<FeatureRecord> GetFeatureRecords(ICollection<LangSysTable> langSysTables,
                List<string> enabledFeatures)
        {
            if (langSysTables.Count == 0)
            {
                return new List<FeatureRecord>(0);
            }
            List<FeatureRecord> result = new List<FeatureRecord>();
            foreach (var langSysTable in langSysTables)
            {
                int required = langSysTable.RequiredFeatureIndex;
                if (required != 0xffff) // if no required features = 0xFFFF
                {
                    result.Add(featureListTable.FeatureRecords[required]);
                }
                foreach (int featureIndex in langSysTable.FeatureIndices)
                {
                    if (enabledFeatures == null
                        || enabledFeatures.Contains(featureListTable.FeatureRecords[featureIndex].FeatureTag))
                    {
                        result.Add(featureListTable.FeatureRecords[featureIndex]);
                    }
                }
            }

            // 'vrt2' supersedes 'vert' and they should not be used together
            // https://www.microsoft.com/typography/otspec/features_uz.htm
            if (ContainsFeature(result, "vrt2"))
            {
                RemoveFeature(result, "vert");
            }

            if (enabledFeatures != null && result.Count > 1)
            {
                result.Sort((o1, o2) => enabledFeatures.IndexOf(o1.FeatureTag).CompareTo(enabledFeatures.IndexOf(o2.FeatureTag)));
            }

            return result;
        }

        private bool ContainsFeature(List<FeatureRecord> featureRecords, string featureTag)
        {
            return featureRecords.Any(featureRecord => featureRecord.FeatureTag.Equals(featureTag, StringComparison.Ordinal));
        }

        private void RemoveFeature(List<FeatureRecord> featureRecords, string featureTag)
        {
            Iterator<FeatureRecord> iter = featureRecords.iterator();
            while (iter.hasNext())
            {
                if (iter.next().getFeatureTag().equals(featureTag))
                {
                    iter.remove();
                }
            }
        }

        private int ApplyFeature(FeatureRecord featureRecord, int gid)
        {
            int lookupResult = gid;
            foreach (int lookupListIndex in featureRecord.FeatureTable.LookupListIndices)
            {
                LookupTable lookupTable = lookupListTable.Lookups[lookupListIndex];
                if (lookupTable.LookupType != 1)
                {
                    Debug.WriteLine("debug: Skipping GSUB feature '" + featureRecord.FeatureTag
                            + "' because it requires unsupported lookup table type "
                            + lookupTable.LookupType);
                    continue;
                }
                lookupResult = DoLookup(lookupTable, lookupResult);
            }
            return lookupResult;
        }

        private int DoLookup(LookupTable lookupTable, int gid)
        {
            foreach (LookupSubTable lookupSubtable in lookupTable.SubTables)
            {
                int coverageIndex = lookupSubtable.CoverageTable.GetCoverageIndex(gid);
                if (coverageIndex >= 0)
                {
                    return lookupSubtable.DoSubstitution(gid, coverageIndex);
                }
            }
            return gid;
        }

        /**
         * Apply glyph substitutions to the supplied gid. The applicable substitutions are determined by
         * the {@code scriptTags} which indicate the language of the gid, and by the
         * {@code enabledFeatures} which acts as a whitelist.
         *
         * To ensure that a single gid isn't mapped to multiple substitutions, subsequent invocations
         * with the same gid will return the same result as the first, regardless of script or enabled
         * features.
         *
         * @param gid GID
         * @param scriptTags Script tags applicable to the gid (see {@link OpenTypeScript})
         * @param enabledFeatures Whitelist of features to apply
         */
        public int GetSubstitution(int gid, string[] scriptTags, List<string> enabledFeatures)
        {
            if (gid == -1)
            {
                return -1;
            }
            if (lookupCache.TryGetValue(gid, out int cached))
            {
                // Because script detection for indeterminate scripts (COMMON, INHERIT, etc.) depends on context,
                // it is possible to return a different substitution for the same input. However we don't want that,
                // as we need a one-to-one mapping.
                return cached;
            }
            string scriptTag = SelectScriptTag(scriptTags);
            ICollection<LangSysTable> langSysTables = GetLangSysTables(scriptTag);
            List<FeatureRecord> featureRecords = GetFeatureRecords(langSysTables, enabledFeatures);
            int sgid = gid;
            foreach (FeatureRecord featureRecord in featureRecords)
            {
                sgid = ApplyFeature(featureRecord, sgid);
            }
            lookupCache[gid] = sgid;
            reverseLookup[sgid] = gid;
            return sgid;
        }

        /**
         * For a substitute-gid (obtained from {@link #getSubstitution(int, string[], List)}), retrieve
         * the original gid.
         *
         * Only gids previously substituted by this instance can be un-substituted. If you are trying to
         * unsubstitute before you substitute, something is wrong.
         *
         * @param sgid Substitute GID
         */
        public int GetUnsubstitution(int sgid)
        {
            if (!reverseLookup.TryGetValue(sgid, out int gid))
            {
                Debug.WriteLine("warning: Trying to un-substitute a never-before-seen gid: " + sgid);
                return sgid;
            }
            return gid;
        }

        private RangeRecord ReadRangeRecord(TTFDataStream data)
        {
            int startGlyphID = data.ReadUnsignedShort();
            int endGlyphID = data.ReadUnsignedShort();
            int startCoverageIndex = data.ReadUnsignedShort();
            return new RangeRecord(startGlyphID, endGlyphID, startCoverageIndex);
        }

    }
}
