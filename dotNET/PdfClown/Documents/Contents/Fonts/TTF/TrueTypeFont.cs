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
using SkiaSharp;
using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using PdfClown.Documents.Contents.Fonts.TTF.Model;
using System.Text;

namespace PdfClown.Documents.Contents.Fonts.TTF
{

    /**
     * A TrueType font file.
     * 
     * @author Ben Litchfield
     */
    public class TrueTypeFont : BaseFont, IDisposable
    {

        //private static readonly Log LOG = LogFactory.getLog(TrueTypeFont.class);

        private float version;
        private int numberOfGlyphs = -1;
        private int unitsPerEm = -1;
        protected Dictionary<string, TTFTable> tables = new Dictionary<string, TTFTable>(StringComparer.Ordinal);
        private readonly TTFDataStream data;
        private volatile Dictionary<string, int> postScriptNames;
        private readonly object lockReadtable = new object();
        private readonly object lockPSNames = new object();
        private readonly List<string> enabledGsubFeatures = new List<string>();

        /**
         * Constructor.  Clients should use the TTFParser to create a new TrueTypeFont object.
         * 
         * @param fontData The font data.
         */
        public TrueTypeFont(TTFDataStream fontData)
        {
            data = fontData;
        }

        public void Dispose()
        {
            data.Dispose();
        }

        /**
         * @return Returns the version.
         */
        public virtual float Version
        {
            get => version;
            set => version = value;
        }

        /**
         * Add a table definition. Package-private, used by TTFParser only.
         * 
         * @param table The table to add.
         */
        public void AddTable(TTFTable table)
        {
            tables[table.Tag] = table;
        }

        /**
         * Get all of the tables.
         * 
         * @return All of the tables.
         */
        public ICollection<TTFTable> Tables
        {
            get => tables.Values;
        }

        /**
         * Get all of the tables.
         *
         * @return All of the tables.
         */
        public Dictionary<string, TTFTable> TableMap
        {
            get => tables;
        }

        /**
         * Returns the raw bytes of the given table.
         * @param table the table to read.
         * @ if there was an error accessing the table.
         */
        public byte[] GetTableBytes(TTFTable table)
        {
            lock (lockReadtable)
            {
                // save current position
                long currentPosition = data.CurrentPosition;
                data.Seek(table.Offset);

                // read all data
                byte[] bytes = data.Read((int)table.Length);

                // restore current position
                data.Seek(currentPosition);
                return bytes;
            }
        }

        /**
         * This will get the table for the given tag.
         * 
         * @param tag the name of the table to be returned
         * @return The table with the given tag.
         * @ if there was an error reading the table.
         */
        protected TTFTable GetTable(string tag)
        {
            // after the initial parsing of the ttf there aren't any write operations
            // to the HashMap anymore, so that we don't have to synchronize the read access
            if (tables.TryGetValue(tag, out TTFTable ttfTable) && !ttfTable.Initialized)
            {
                lock (lockReadtable)
                {
                    if (!ttfTable.Initialized)
                    {
                        ReadTable(ttfTable);
                    }
                }
            }
            return ttfTable;
        }

        /**
         * This will get the naming table for the true type font.
         * 
         * @return The naming table or null if it doesn't exist.
         * @ if there was an error reading the table.
         */
        public NamingTable Naming
        {
            get => (NamingTable)GetTable(NamingTable.TAG);
        }

        /**
         * Get the postscript table for this TTF.
         * 
         * @return The postscript table or null if it doesn't exist.
         * @ if there was an error reading the table.
         */
        public PostScriptTable PostScript
        {
            get => (PostScriptTable)GetTable(PostScriptTable.TAG);
        }

        /**
         * Get the OS/2 table for this TTF.
         * 
         * @return The OS/2 table or null if it doesn't exist.
         * @ if there was an error reading the table.
         */
        public OS2WindowsMetricsTable OS2Windows
        {
            get => (OS2WindowsMetricsTable)GetTable(OS2WindowsMetricsTable.TAG);
        }

        /**
         * Get the maxp table for this TTF.
         * 
         * @return The maxp table or null if it doesn't exist.
         * @ if there was an error reading the table.
         */
        public MaximumProfileTable MaximumProfile
        {
            get => (MaximumProfileTable)GetTable(MaximumProfileTable.TAG);
        }

        /**
         * Get the head table for this TTF.
         * 
         * @return The head table or null if it doesn't exist.
         * @ if there was an error reading the table.
         */
        public HeaderTable Header
        {
            get => (HeaderTable)GetTable(HeaderTable.TAG);
        }

        /**
         * Get the hhea table for this TTF.
         * 
         * @return The hhea table or null if it doesn't exist.
         * @ if there was an error reading the table.
         */
        public HorizontalHeaderTable HorizontalHeader
        {
            get => (HorizontalHeaderTable)GetTable(HorizontalHeaderTable.TAG);
        }

        /**
         * Get the hmtx table for this TTF.
         * 
         * @return The hmtx table or null if it doesn't exist.
         * @ if there was an error reading the table.
         */
        public HorizontalMetricsTable HorizontalMetrics
        {
            get => (HorizontalMetricsTable)GetTable(HorizontalMetricsTable.TAG);
        }

        /**
         * Get the loca table for this TTF.
         * 
         * @return The loca table or null if it doesn't exist.
         * @ if there was an error reading the table.
         */
        public IndexToLocationTable IndexToLocation
        {
            get => (IndexToLocationTable)GetTable(IndexToLocationTable.TAG);
        }

        /**
         * Get the glyf table for this TTF.
         * 
         * @return The glyf table or null if it doesn't exist.
         * @ if there was an error reading the table.
         */
        public virtual GlyphTable Glyph
        {
            get => (GlyphTable)GetTable(GlyphTable.TAG);
        }

        /**
         * Get the "cmap" table for this TTF.
         * 
         * @return The "cmap" table or null if it doesn't exist.
         * @ if there was an error reading the table.
         */
        public CmapTable Cmap
        {
            get => (CmapTable)GetTable(CmapTable.TAG);
        }

        /**
         * Get the vhea table for this TTF.
         * 
         * @return The vhea table or null if it doesn't exist.
         * @ if there was an error reading the table.
         */
        public VerticalHeaderTable VerticalHeader
        {
            get => (VerticalHeaderTable)GetTable(VerticalHeaderTable.TAG);
        }

        /**
         * Get the vmtx table for this TTF.
         * 
         * @return The vmtx table or null if it doesn't exist.
         * @ if there was an error reading the table.
         */
        public VerticalMetricsTable VerticalMetrics
        {
            get => (VerticalMetricsTable)GetTable(VerticalMetricsTable.TAG);
        }

        /**
         * Get the VORG table for this TTF.
         * 
         * @return The VORG table or null if it doesn't exist.
         * @ if there was an error reading the table.
         */
        public VerticalOriginTable VerticalOrigin
        {
            get => (VerticalOriginTable)GetTable(VerticalOriginTable.TAG);
        }

        /**
         * Get the "kern" table for this TTF.
         * 
         * @return The "kern" table or null if it doesn't exist.
         * @ if there was an error reading the table.
         */
        public KerningTable Kerning
        {
            get => (KerningTable)GetTable(KerningTable.TAG);
        }

        /**
         * Get the "gsub" table for this TTF.
         *
         * @return The "gsub" table or null if it doesn't exist.
         * @ if there was an error reading the table.
         */
        public GlyphSubstitutionTable Gsub
        {
            get => (GlyphSubstitutionTable)GetTable(GlyphSubstitutionTable.TAG);
        }

        /**
         * Get the data of the TrueType Font
         * program representing the stream used to build this 
         * object (normally from the TTFParser object).
         * 
         * @return COSStream TrueType font program stream
         * 
         * @ If there is an error getting the font data.
         */
        public Bytes.Buffer OriginalData
        {
            get => data.OriginalData;
        }

        /**
         * Get the data size of the TrueType Font program representing the stream used to build this
         * object (normally from the TTFParser object).
         *
         * @return the size.
         */
        public long OriginalDataSize
        {
            get => data.OriginalDataSize;
        }

        /**
         * Read the given table if necessary. Package-private, used by TTFParser only.
         * 
         * @param table the table to be initialized
         * 
         * @ if there was an error reading the table.
         */
        public void ReadTable(TTFTable table)
        {
            // PDFBOX-4219: synchronize on data because it is accessed by several threads
            // when PDFBox is accessing a standard 14 font for the first time
            lock (data)
            {
                // save current position
                long currentPosition = data.CurrentPosition;
                data.Seek(table.Offset);
                table.Read(this, data);
                // restore current position
                data.Seek(currentPosition);
            }
        }

        /**
         * Returns the number of glyphs (MaximumProfile.numGlyphs).
         * 
         * @return the number of glyphs
         * @ if there was an error reading the table.
         */
        public int NumberOfGlyphs
        {
            get
            {
                if (numberOfGlyphs == -1)
                {
                    MaximumProfileTable maximumProfile = MaximumProfile;
                    if (maximumProfile != null)
                    {
                        numberOfGlyphs = maximumProfile.NumGlyphs;
                    }
                    else
                    {
                        // this should never happen
                        numberOfGlyphs = 0;
                    }
                }
                return numberOfGlyphs;
            }
        }

        /**
         * Returns the units per EM (Header.unitsPerEm).
         * 
         * @return units per EM
         * @ if there was an error reading the table.
         */
        public int UnitsPerEm
        {
            get
            {
                if (unitsPerEm == -1)
                {
                    HeaderTable header = Header;
                    if (header != null)
                    {
                        unitsPerEm = header.UnitsPerEm;
                    }
                    else
                    {
                        // this should never happen
                        unitsPerEm = 0;
                    }
                }
                return unitsPerEm;
            }
        }

        /**
         * Returns the width for the given GID.
         * 
         * @param gid the GID
         * @return the width
         * @ if there was an error reading the metrics table.
         */
        public int GetAdvanceWidth(int gid)
        {
            HorizontalMetricsTable hmtx = HorizontalMetrics;
            if (hmtx != null)
            {
                return hmtx.GetAdvanceWidth(gid);
            }
            else
            {
                // this should never happen
                return 250;
            }
        }

        /**
         * Returns the height for the given GID.
         * 
         * @param gid the GID
         * @return the height
         * @ if there was an error reading the metrics table.
         */
        public int GetAdvanceHeight(int gid)
        {
            VerticalMetricsTable vmtx = VerticalMetrics;
            if (vmtx != null)
            {
                return vmtx.GetAdvanceHeight(gid);
            }
            else
            {
                // this should never happen
                return 250;
            }
        }

        public override string Name
        {
            get
            {
                if (Naming != null)
                {
                    return Naming.PostScriptName;
                }
                else
                {
                    return null;
                }
            }
        }

        private void ReadPostScriptNames()
        {
            Dictionary<string, int> psnames = postScriptNames;
            if (psnames == null)
            {
                // the getter is already synchronized
                PostScriptTable post = PostScript;
                lock (lockPSNames)
                {
                    psnames = postScriptNames;
                    if (psnames == null)
                    {
                        string[] names = post != null ? post.GlyphNames : null;
                        if (names != null)
                        {
                            psnames = new Dictionary<string, int>(names.Length, StringComparer.Ordinal);
                            for (int i = 0; i < names.Length; i++)
                            {
                                psnames[names[i]] = i;
                            }
                        }
                        else
                        {
                            psnames = new Dictionary<string, int>(StringComparer.Ordinal);
                        }
                        postScriptNames = psnames;
                    }
                }
            }
        }

        /**
         * Returns the best Unicode from the font (the most general). The PDF spec says that "The means
         * by which this is accomplished are implementation-dependent."
         *
         * The returned cmap will perform glyph substitution.
         *
         * @ if the font could not be read
         */
        public ICmapLookup GetUnicodeCmapLookup()
        {
            return GetUnicodeCmapLookup(true);
        }

        /**
         * Returns the best Unicode from the font (the most general). The PDF spec says that "The means
         * by which this is accomplished are implementation-dependent."
         *
         * The returned cmap will perform glyph substitution.
         *
         * @param isStrict False if we allow falling back to any cmap, even if it's not Unicode.
         * @ if the font could not be read, or there is no Unicode cmap
         */
        public ICmapLookup GetUnicodeCmapLookup(bool isStrict)
        {
            CmapSubtable cmap = GetUnicodeCmapImpl(isStrict);
            if (enabledGsubFeatures.Count > 0)
            {
                GlyphSubstitutionTable table = Gsub;
                if (table != null)
                {
                    return new SubstitutingCmapLookup(cmap, table, enabledGsubFeatures);
                }
            }
            return cmap;
        }

        private CmapSubtable GetUnicodeCmapImpl(bool isStrict)
        {
            CmapTable cmapTable = Cmap;
            if (cmapTable == null)
            {
                if (isStrict)
                {
                    throw new IOException("The TrueType font " + Name + " does not contain a 'cmap' table");
                }
                else
                {
                    return null;
                }
            }

            CmapSubtable cmap = cmapTable.GetSubtable(CmapTable.PLATFORM_UNICODE,
                                                      CmapTable.ENCODING_UNICODE_2_0_FULL);
            if (cmap == null)
            {
                cmap = cmapTable.GetSubtable(CmapTable.PLATFORM_WINDOWS,
                                             CmapTable.ENCODING_WIN_UNICODE_FULL);
            }
            if (cmap == null)
            {
                cmap = cmapTable.GetSubtable(CmapTable.PLATFORM_UNICODE,
                                             CmapTable.ENCODING_UNICODE_2_0_BMP);
            }
            if (cmap == null)
            {
                cmap = cmapTable.GetSubtable(CmapTable.PLATFORM_WINDOWS,
                                             CmapTable.ENCODING_WIN_UNICODE_BMP);
            }
            if (cmap == null)
            {
                // Microsoft's "Recommendations for OpenType Fonts" says that "Symbol" encoding
                // actually means "Unicode, non-standard character set"
                cmap = cmapTable.GetSubtable(CmapTable.PLATFORM_WINDOWS,
                                             CmapTable.ENCODING_WIN_SYMBOL);
            }
            if (cmap == null)
            {
                if (isStrict)
                {
                    throw new IOException("The TrueType font does not contain a Unicode cmap");
                }
                else if (cmapTable.Cmaps.Length > 0)
                {
                    // fallback to the first cmap (may not be Unicode, so may produce poor results)
                    cmap = cmapTable.Cmaps[0];
                }
            }
            return cmap;
        }

        /**
         * Returns the GID for the given PostScript name, if the "post" table is present.
         * @param name the PostScript name.
         */
        public int NameToGID(string name)
        {
            // look up in 'post' table
            ReadPostScriptNames();
            if (postScriptNames != null)
            {
                if (postScriptNames.TryGetValue(name, out int gid)
                    && gid > 0
                    && gid < MaximumProfile.NumGlyphs)
                {
                    return gid;
                }
            }

            // look up in 'cmap'
            int uni = ParseUniName(name);
            if (uni > -1)
            {
                ICmapLookup cmap = GetUnicodeCmapLookup(false);
                return cmap.GetGlyphId(uni);
            }

            return 0;
        }

        public GsubData GsubData
        {
            get
            {
                GlyphSubstitutionTable table = Gsub;
                if (table == null)
                {
                    return DefaultGsubData.NO_DATA_FOUND;
                }

                return table.GsubData;
            }
        }

        /**
         * Parses a Unicode PostScript name in the format uniXXXX.
         */
        private int ParseUniName(string name)
        {
            if (name.StartsWith("uni", StringComparison.Ordinal) && name.Length == 7)
            {
                int nameLength = name.Length;
                var uniStr = new StringBuilder();
                try
                {
                    for (int chPos = 3; chPos + 4 <= nameLength; chPos += 4)
                    {
                        int codePoint = Convert.ToInt32(name.Substring(chPos, 4), 16);
                        if (codePoint <= 0xD7FF || codePoint >= 0xE000) // disallowed code area
                        {
                            uniStr.Append((char)codePoint);
                        }
                    }
                    string unicode = uniStr.ToString();
                    if (unicode.Length == 0)
                    {
                        return -1;
                    }
                    return unicode[0];
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"error: ParseUniName {e}");
                    return -1;
                }
            }
            return -1;
        }

        public override SKPath GetPath(string name)
        {
            int gid = NameToGID(name);

            // some glyphs have no outlines (e.g. space, table, newline)
            GlyphData glyph = Glyph.GetGlyph(gid);
            if (glyph == null)
            {
                return null;
            }
            else
            {
                // must scaled by caller using FontMatrix
                return glyph.GetPath();
            }
        }

        public override float GetWidth(string name)
        {
            int gid = NameToGID(name);
            return GetAdvanceWidth(gid);
        }

        public override bool HasGlyph(string name)
        {
            return NameToGID(name) != 0;
        }

        public override SKRect FontBBox
        {
            get
            {
                short xMin = Header.XMin;
                short xMax = Header.XMax;
                short yMin = Header.YMin;
                short yMax = Header.YMax;
                float scale = 1000f / UnitsPerEm;
                return new SKRect(xMin * scale, yMin * scale, xMax * scale, yMax * scale);
            }
        }

        public override List<float> FontMatrix
        {
            get
            {
                float scale = 1000f / UnitsPerEm;
                return new List<float> { 0.001f * scale, 0, 0, 0.001f * scale, 0, 0 };
            }
        }

        /**
         * Enable a particular glyph substitution feature. This feature might not be supported by the
         * font, or might not be implemented in PDFBox yet.
         *
         * @param featureTag The GSUB feature to enable
         */
        public void EnableGsubFeature(string featureTag)
        {
            enabledGsubFeatures.Add(featureTag);
        }

        /**
         * Disable a particular glyph substitution feature.
         *
         * @param featureTag The GSUB feature to disable
         */
        public void DisableGsubFeature(string featureTag)
        {
            enabledGsubFeatures.Remove(featureTag);
        }

        /**
         * Enable glyph substitutions for vertical writing.
         */
        public void EnableVerticalSubstitutions()
        {
            EnableGsubFeature("vrt2");
            EnableGsubFeature("vert");
        }

        public override string ToString()
        {
            try
            {
                if (Naming != null)
                {
                    return Naming.PostScriptName;
                }
                else
                {
                    return "(null)";
                }
            }
            catch (IOException e)
            {
                Debug.WriteLine("debug: Error getting the NamingTable for the font", e);
                return "(null - " + e.Message + ")";
            }
        }
    }
}
