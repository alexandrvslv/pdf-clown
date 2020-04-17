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
namespace PdfClown.Documents.Contents.Fonts.TTF{

using SkiaSharp;
using System;
using System.IO;






using System.Collections.Generic;


using System.Diagnostics;

import org.apache.fontbox.FontBoxFont;
import org.apache.fontbox.ttf.model.GsubData;
import org.apache.fontbox.util.SKRect;

/**
 * A TrueType font file.
 * 
 * @author Ben Litchfield
 */
public class TrueTypeFont : FontBoxFont, IDisposable
{

    //private static readonly Log LOG = LogFactory.getLog(TrueTypeFont.class);

    private float version;
    private int numberOfGlyphs = -1;
    private int unitsPerEm = -1;
    protected Dictionary<string,TTFTable> tables = new Dictionary<>();
    private readonly TTFDataStream data;
    private volatile Dictionary<string, int> postScriptNames;
    
    private readonly object lockReadtable = new object();
    private readonly object lockPSNames = new object();
    private readonly List<string> enabledGsubFeatures = new List<>();

    /**
     * Constructor.  Clients should use the TTFParser to create a new TrueTypeFont object.
     * 
     * @param fontData The font data.
     */
    TrueTypeFont(TTFDataStream fontData)
    {
        data = fontData;
    }
    
    public override void Dispose() 
    {
        data.Dispose();
    }

    /**
     * @return Returns the version.
     */
    public float Version 
    {
        return version;
    }

    /**
     * Set the version. Package-private, used by TTFParser only.
     * @param versionValue The version to set.
     */
    void setVersion(float versionValue)
    {
        version = versionValue;
    }
    
    /**
     * Add a table definition. Package-private, used by TTFParser only.
     * 
     * @param table The table to add.
     */
    void addTable( TTFTable table )
    {
        tables[ table.getTag(), table );
    }
    
    /**
     * Get all of the tables.
     * 
     * @return All of the tables.
     */
    public Collection<TTFTable> Tables
    {
        return tables.Values;
    }

    /**
     * Get all of the tables.
     *
     * @return All of the tables.
     */
    public Dictionary<string, TTFTable> getTableMap()
    {
        return tables;
    }

    /**
     * Returns the raw bytes of the given table.
     * @param table the table to read.
     * @ if there was an error accessing the table.
     */
    public byte[] getTableBytes(TTFTable table) 
    {
        synchronized (lockReadtable)
        {
            // save current position
            long currentPosition = data.getCurrentPosition();
            data.seek(table.Offset);

            // read all data
            byte[] bytes = data.Read((int) table.Length);

            // restore current position
            data.seek(currentPosition);
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
    protected TTFTable getTable(string tag) 
    {
        // after the initial parsing of the ttf there aren't any write operations
        // to the HashMap anymore, so that we don't have to synchronize the read access
        TTFTable ttfTable = tables.get(tag);
        if (ttfTable != null && !ttfTable.initialized)
        {
            synchronized (lockReadtable)
            {
                if (!ttfTable.initialized)
                {
                    readTable(ttfTable);
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
    public NamingTable getNaming() 
    {
        return (NamingTable) getTable(NamingTable.TAG);
    }
    
    /**
     * Get the postscript table for this TTF.
     * 
     * @return The postscript table or null if it doesn't exist.
     * @ if there was an error reading the table.
     */
    public PostScriptTable PostScript 
    {
        return (PostScriptTable) getTable(PostScriptTable.TAG);
    }
    
    /**
     * Get the OS/2 table for this TTF.
     * 
     * @return The OS/2 table or null if it doesn't exist.
     * @ if there was an error reading the table.
     */
    public OS2WindowsMetricsTable getOS2Windows() 
    {
        return (OS2WindowsMetricsTable) getTable(OS2WindowsMetricsTable.TAG);
    }

    /**
     * Get the maxp table for this TTF.
     * 
     * @return The maxp table or null if it doesn't exist.
     * @ if there was an error reading the table.
     */
    public MaximumProfileTable MaximumProfile 
    {
        return (MaximumProfileTable) getTable(MaximumProfileTable.TAG);
    }
    
    /**
     * Get the head table for this TTF.
     * 
     * @return The head table or null if it doesn't exist.
     * @ if there was an error reading the table.
     */
    public HeaderTable Header 
    {
        return (HeaderTable) getTable(HeaderTable.TAG);
    }
    
    /**
     * Get the hhea table for this TTF.
     * 
     * @return The hhea table or null if it doesn't exist.
     * @ if there was an error reading the table.
     */
    public HorizontalHeaderTable HorizontalHeader 
    {
        return (HorizontalHeaderTable) getTable(HorizontalHeaderTable.TAG);
    }
    
    /**
     * Get the hmtx table for this TTF.
     * 
     * @return The hmtx table or null if it doesn't exist.
     * @ if there was an error reading the table.
     */
    public HorizontalMetricsTable HorizontalMetrics 
    {
        return (HorizontalMetricsTable) getTable(HorizontalMetricsTable.TAG);
    }
    
    /**
     * Get the loca table for this TTF.
     * 
     * @return The loca table or null if it doesn't exist.
     * @ if there was an error reading the table.
     */
    public IndexToLocationTable IndexToLocation 
    {
        return (IndexToLocationTable) getTable(IndexToLocationTable.TAG);
    }
    
    /**
     * Get the glyf table for this TTF.
     * 
     * @return The glyf table or null if it doesn't exist.
     * @ if there was an error reading the table.
     */
    public GlyphTable Glyph 
    {
        return (GlyphTable) getTable(GlyphTable.TAG);
    }
    
    /**
     * Get the "cmap" table for this TTF.
     * 
     * @return The "cmap" table or null if it doesn't exist.
     * @ if there was an error reading the table.
     */
    public CmapTable getCmap() 
    {
        return (CmapTable) getTable(CmapTable.TAG);
    }
    
    /**
     * Get the vhea table for this TTF.
     * 
     * @return The vhea table or null if it doesn't exist.
     * @ if there was an error reading the table.
     */
    public VerticalHeaderTable getVerticalHeader() 
    {
        return (VerticalHeaderTable) getTable(VerticalHeaderTable.TAG);
    }
    
    /**
     * Get the vmtx table for this TTF.
     * 
     * @return The vmtx table or null if it doesn't exist.
     * @ if there was an error reading the table.
     */
    public VerticalMetricsTable getVerticalMetrics() 
    {
        return (VerticalMetricsTable) getTable(VerticalMetricsTable.TAG);
    }
    
    /**
     * Get the VORG table for this TTF.
     * 
     * @return The VORG table or null if it doesn't exist.
     * @ if there was an error reading the table.
     */
    public VerticalOriginTable getVerticalOrigin() 
    {
        return (VerticalOriginTable) getTable(VerticalOriginTable.TAG);
    }
    
    /**
     * Get the "kern" table for this TTF.
     * 
     * @return The "kern" table or null if it doesn't exist.
     * @ if there was an error reading the table.
     */
    public KerningTable getKerning() 
    {
        return (KerningTable) getTable(KerningTable.TAG);
    }

    /**
     * Get the "gsub" table for this TTF.
     *
     * @return The "gsub" table or null if it doesn't exist.
     * @ if there was an error reading the table.
     */
    public GlyphSubstitutionTable getGsub() 
    {
        return (GlyphSubstitutionTable) getTable(GlyphSubstitutionTable.TAG);
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
    public Bytes.Buffer getOriginalData()  
    {
       return data.getOriginalData(); 
    }

    /**
     * Get the data size of the TrueType Font program representing the stream used to build this
     * object (normally from the TTFParser object).
     *
     * @return the size.
     */
    public long getOriginalDataSize()
    {
        return data.getOriginalDataSize();
    }

    /**
     * Read the given table if necessary. Package-private, used by TTFParser only.
     * 
     * @param table the table to be initialized
     * 
     * @ if there was an error reading the table.
     */
    void readTable(TTFTable table) 
    {
        // PDFBOX-4219: synchronize on data because it is accessed by several threads
        // when PDFBox is accessing a standard 14 font for the first time
        synchronized (data)
        {
            // save current position
            long currentPosition = data.getCurrentPosition();
            data.seek(table.Offset);
            table.Read(this, data);
            // restore current position
            data.seek(currentPosition);
        }
    }

    /**
     * Returns the number of glyphs (MaximumProfile.numGlyphs).
     * 
     * @return the number of glyphs
     * @ if there was an error reading the table.
     */
    public int getNumberOfGlyphs() 
    {
        if (numberOfGlyphs == -1)
        {
            MaximumProfileTable maximumProfile = MaximumProfile;
            if (maximumProfile != null)
            {
                numberOfGlyphs = maximumProfile.getNumGlyphs();
            }
            else
            {
                // this should never happen
                numberOfGlyphs = 0;
            }
        }
        return numberOfGlyphs;
    }

    /**
     * Returns the units per EM (Header.unitsPerEm).
     * 
     * @return units per EM
     * @ if there was an error reading the table.
     */
    public int getUnitsPerEm() 
    {
        if (unitsPerEm == -1)
        {
            HeaderTable header = Header;
            if (header != null)
            {
                unitsPerEm = header.getUnitsPerEm();
            }
            else
            {
                // this should never happen
                unitsPerEm = 0;
            }
        }
        return unitsPerEm;
    }

    /**
     * Returns the width for the given GID.
     * 
     * @param gid the GID
     * @return the width
     * @ if there was an error reading the metrics table.
     */
    public int getAdvanceWidth(int gid) 
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
    public int getAdvanceHeight(int gid) 
    {
        VerticalMetricsTable vmtx = getVerticalMetrics();
        if (vmtx != null)
        {
            return vmtx.getAdvanceHeight(gid);
        }
        else
        {
            // this should never happen
            return 250;
        }
    }

    public override string Name 
    {
        if (getNaming() != null)
        {
            return getNaming().getPostScriptName();
        }
        else
        {
            return null;
        }
    }

    private void readPostScriptNames() 
    {
        Dictionary<string, int> psnames = postScriptNames;
        if (psnames == null)
        {
            // the getter is already synchronized
            PostScriptTable post = PostScript;
            synchronized (lockPSNames)
            {
                psnames = postScriptNames;
                if (psnames == null)
                {
                    string[] names = post != null ? post.getGlyphNames() : null;
                    if (names != null)
                    {
                        psnames = new Dictionary<>(names.Length);
                        for (int i = 0; i < names.Length; i++)
                        {
                            psnames[names[i], i);
                        }
                    }
                    else
                    {
                        psnames = new Dictionary<>();
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
    public CmapLookup getUnicodeCmapLookup() 
    {
        return getUnicodeCmapLookup(true);
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
    public CmapLookup getUnicodeCmapLookup(bool isStrict) 
    {
        CmapSubtable cmap = getUnicodeCmapImpl(isStrict);
        if (!enabledGsubFeatures.isEmpty())
        {
            GlyphSubstitutionTable table = getGsub();
            if (table != null)
            {
                return new SubstitutingCmapLookup(cmap, table,
                        Collections.unmodifiableList(enabledGsubFeatures));
            }
        }
        return cmap;
    }

    private CmapSubtable getUnicodeCmapImpl(bool isStrict) 
    {
        CmapTable cmapTable = getCmap();
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

        CmapSubtable cmap = cmapTable.getSubtable(CmapTable.PLATFORM_UNICODE,
                                                  CmapTable.ENCODING_UNICODE_2_0_FULL);
        if (cmap == null)
        {
            cmap = cmapTable.getSubtable(CmapTable.PLATFORM_WINDOWS,
                                         CmapTable.ENCODING_WIN_UNICODE_FULL);
        }
        if (cmap == null)
        {
            cmap = cmapTable.getSubtable(CmapTable.PLATFORM_UNICODE,
                                         CmapTable.ENCODING_UNICODE_2_0_BMP);
        }
        if (cmap == null)
        {
            cmap = cmapTable.getSubtable(CmapTable.PLATFORM_WINDOWS,
                                         CmapTable.ENCODING_WIN_UNICODE_BMP);
        }
        if (cmap == null)
        {
            // Microsoft's "Recommendations for OpenType Fonts" says that "Symbol" encoding
            // actually means "Unicode, non-standard character set"
            cmap = cmapTable.getSubtable(CmapTable.PLATFORM_WINDOWS,
                                         CmapTable.ENCODING_WIN_SYMBOL);
        }
        if (cmap == null)
        {
            if (isStrict)
            {
                throw new IOException("The TrueType font does not contain a Unicode cmap");
            }
            else if (cmapTable.getCmaps().Length > 0)
            {
                // fallback to the first cmap (may not be Unicode, so may produce poor results)
                cmap = cmapTable.getCmaps()[0];
            }
        }
        return cmap;
    }

    /**
     * Returns the GID for the given PostScript name, if the "post" table is present.
     * @param name the PostScript name.
     */
    public int nameToGID(string name) 
    {
        // look up in 'post' table
        readPostScriptNames();
        if (postScriptNames != null)
        {
            int gid = postScriptNames.get(name);
            if (gid != null && gid > 0 && gid < MaximumProfile.getNumGlyphs())
            {
                return gid;
            }
        }

        // look up in 'cmap'
        int uni = parseUniName(name);
        if (uni > -1)
        {
            CmapLookup cmap = getUnicodeCmapLookup(false);
            return cmap.GetGlyphId(uni);
        }
        
        return 0;
    }

    public GsubData getGsubData() 
    {
        GlyphSubstitutionTable table = getGsub();
        if (table == null)
        {
            return GsubData.NO_DATA_FOUND;
        }

        return table.getGsubData();
    }

    /**
     * Parses a Unicode PostScript name in the format uniXXXX.
     */
    private int parseUniName(string name)
    {
        if (name.startsWith("uni") && name.Length == 7)
        {
            int nameLength = name.Length;
            StringBuilder uniStr = new StringBuilder();
            try
            {
                for (int chPos = 3; chPos + 4 <= nameLength; chPos += 4)
                {
                    int codePoint = int.parseInt(name.substring(chPos, chPos + 4), 16);
                    if (codePoint <= 0xD7FF || codePoint >= 0xE000) // disallowed code area
                    {
                        uniStr.Append((char) codePoint);
                    }
                }
                string unicode = uniStr.ToString();
                if (unicode.Length == 0)
                {
                    return -1;
                }
                return unicode.codePointAt(0);
            }
            catch (NumberFormatException e)
            {
                return -1;
            }
        }
        return -1;
    }
    
    public override SKPath GetPath(string name) 
    {
        int gid = nameToGID(name);

        // some glyphs have no outlines (e.g. space, table, newline)
        GlyphData glyph = Glyph.getGlyph(gid);
        if (glyph == null)
        {
            return new SKPath();
        }
        else
        {
            // must scaled by caller using FontMatrix
            return glyph.getPath();
        }
    }

    public override float getWidth(string name) 
    {
        int gid = nameToGID(name);
        return getAdvanceWidth(gid);
    }

    public override bool hasGlyph(string name) 
    {
        return nameToGID(name) != 0;
    }

    public override SKRect getFontBBox() 
    {
        short xMin = Header.getXMin();
        short xMax = Header.getXMax();
        short yMin = Header.getYMin();
        short yMax = Header.getYMax();
        float scale = 1000f / getUnitsPerEm();
        return new SKRect(xMin * scale, yMin * scale, xMax * scale, yMax * scale);
    }

    public override List<Number> getFontMatrix() 
    {
        float scale = 1000f / getUnitsPerEm();
        return Array.<Number>asList(0.001f * scale, 0, 0, 0.001f * scale, 0, 0);
    }

    /**
     * Enable a particular glyph substitution feature. This feature might not be supported by the
     * font, or might not be implemented in PDFBox yet.
     *
     * @param featureTag The GSUB feature to enable
     */
    public void enableGsubFeature(string featureTag)
    {
        enabledGsubFeatures.Add(featureTag);
    }

    /**
     * Disable a particular glyph substitution feature.
     *
     * @param featureTag The GSUB feature to disable
     */
    public void disableGsubFeature(string featureTag)
    {
        enabledGsubFeatures.remove(featureTag);
    }

    /**
     * Enable glyph substitutions for vertical writing.
     */
    public void enableVerticalSubstitutions()
    {
        enableGsubFeature("vrt2");
        enableGsubFeature("vert");
    }

    public override string ToString()
    {
        try
        {
            if (getNaming() != null)
            {
                return getNaming().getPostScriptName();
            }
            else
            {
                return "(null)";
            }
        }
        catch (IOException e)
        {
            Debug.WriteLine("debug: Error getting the NamingTable for the font", e);
            return "(null - " + e.getMessage() + ")";
        }
    }
}
}
