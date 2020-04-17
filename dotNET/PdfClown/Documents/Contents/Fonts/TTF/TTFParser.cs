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


using System.IO;


/**
 * TrueType font file parser.
 * 
 * @author Ben Litchfield
 */
public class TTFParser
{
    private bool isEmbedded = false;
    private bool parseOnDemandOnly = false;

    /**
     * Constructor.
     */
    public TTFParser()
    {
        this(false);
    }

    /**
     * Constructor.
     *  
     * @param isEmbedded true if the font is embedded in PDF
     */
    public TTFParser(bool isEmbedded)
    :this(isEmbedded, false)
    {
    }

    /**
     *  Constructor.
     *  
     * @param isEmbedded true if the font is embedded in PDF
     * @param parseOnDemand true if the tables of the font should be parsed on demand
     */
    public TTFParser(bool isEmbedded, bool parseOnDemand)
    {
        this.isEmbedded = isEmbedded;
        parseOnDemandOnly = parseOnDemand;
    }

    /**
     * Parse a file and return a TrueType font.
     *
     * @param ttfFile The TrueType font filename.
     * @return A TrueType font.
     * @ If there is an error parsing the TrueType font.
     */
    public TrueTypeFont Parse(string ttfFile) 
    {
        return Parse(new File(ttfFile));
    }

    /**
     * Parse a file and return a TrueType font.
     *
     * @param ttfFile The TrueType font file.
     * @return A TrueType font.
     * @ If there is an error parsing the TrueType font.
     */
    public TrueTypeFont Parse(File ttfFile) 
    {
        RAFDataStream raf = new RAFDataStream(ttfFile, "r");
        try
        {
            return Parse(raf);
        }
        catch (IOException ex)
        {
            // close only on error (file is still being accessed later)
            raf.Dispose();
            throw ex;
        }
    }

    /**
     * Parse an input stream and return a TrueType font.
     *
     * @param inputStream The TTF data stream to parse from. It will be closed before returning.
     * @return A TrueType font.
     * @ If there is an error parsing the TrueType font.
     */
    public TrueTypeFont Parse(Bytes.Buffer inputStream) 
    {
        return Parse(new MemoryTTFDataStream(inputStream));
    }

    /**
     * Parse an input stream and return a TrueType font that is to be embedded.
     *
     * @param inputStream The TTF data stream to parse from. It will be closed before returning.
     * @return A TrueType font.
     * @ If there is an error parsing the TrueType font.
     */
    public TrueTypeFont parseEmbedded(Bytes.Buffer inputStream) 
    {
        this.isEmbedded = true;
        return Parse(new MemoryTTFDataStream(inputStream));
    }

    /**
     * Parse a file and get a true type font.
     *
     * @param raf The TTF file.
     * @return A TrueType font.
     * @ If there is an error parsing the TrueType font.
     */
    TrueTypeFont Parse(TTFDataStream raf) 
    {
        TrueTypeFont font = newFont(raf);
        font.setVersion(raf.Read32Fixed());
        int numberOfTables = raf.ReadUnsignedShort();
        int searchRange = raf.ReadUnsignedShort();
        int entrySelector = raf.ReadUnsignedShort();
        int rangeShift = raf.ReadUnsignedShort();
        for (int i = 0; i < numberOfTables; i++)
        {
            TTFTable table = readTableDirectory(font, raf);
            
            // skip tables with zero length
            if (table != null)
            {
                font.addTable(table);
            }
        }
        // parse tables if wanted
        if (!parseOnDemandOnly)
        {
            parseTables(font);
        }

        return font;
    }

    TrueTypeFont newFont(TTFDataStream raf)
    {
        return new TrueTypeFont(raf);
    }

    /**
     * Parse all tables and check if all needed tables are present.
     *
     * @param font the TrueTypeFont instance holding the parsed data.
     * @ If there is an error parsing the TrueType font.
     */
    private void parseTables(TrueTypeFont font) 
    {
        foreach (TTFTable table in font.Tables)
        {
            if (!table.getInitialized())
            {
                font.readTable(table);
            }
        }

        bool isPostScript = allowCFF() && font.tables.containsKey(CFFTable.TAG);
        
        HeaderTable head = font.Header;
        if (head == null)
        {
            throw new IOException("head is mandatory");
        }

        HorizontalHeaderTable hh = font.HorizontalHeader;
        if (hh == null)
        {
            throw new IOException("hhead is mandatory");
        }

        MaximumProfileTable maxp = font.MaximumProfile;
        if (maxp == null)
        {
            throw new IOException("maxp is mandatory");
        }

        PostScriptTable post = font.PostScript;
        if (post == null && !isEmbedded)
        {
            // in an embedded font this table is optional
            throw new IOException("post is mandatory");
        }

        if (!isPostScript)
        {
            IndexToLocationTable loc = font.IndexToLocation;
            if (loc == null)
            {
                throw new IOException("loca is mandatory");
            }

            if (font.Glyph == null)
            {
                throw new IOException("glyf is mandatory");
            }
        }
        
        if (font.getNaming() == null && !isEmbedded)
        {
            throw new IOException("name is mandatory");
        }
        
        if (font.HorizontalMetrics == null)
        {
            throw new IOException("hmtx is mandatory");
        }
        
        if (!isEmbedded && font.getCmap() == null)
        {
            throw new IOException("cmap is mandatory");
        }
    }

    protected bool allowCFF()
    {
        return false;
    }

    private TTFTable readTableDirectory(TrueTypeFont font, TTFDataStream raf) 
    {
        TTFTable table;
        string tag = raf.ReadString(4);
        switch (tag)
        {
            case CmapTable.TAG:
                table = new CmapTable(font);
                break;
            case GlyphTable.TAG:
                table = new GlyphTable(font);
                break;
            case HeaderTable.TAG:
                table = new HeaderTable(font);
                break;
            case HorizontalHeaderTable.TAG:
                table = new HorizontalHeaderTable(font);
                break;
            case HorizontalMetricsTable.TAG:
                table = new HorizontalMetricsTable(font);
                break;
            case IndexToLocationTable.TAG:
                table = new IndexToLocationTable(font);
                break;
            case MaximumProfileTable.TAG:
                table = new MaximumProfileTable(font);
                break;
            case NamingTable.TAG:
                table = new NamingTable(font);
                break;
            case OS2WindowsMetricsTable.TAG:
                table = new OS2WindowsMetricsTable(font);
                break;
            case PostScriptTable.TAG:
                table = new PostScriptTable(font);
                break;
            case DigitalSignatureTable.TAG:
                table = new DigitalSignatureTable(font);
                break;
            case KerningTable.TAG:
                table = new KerningTable(font);
                break;
            case VerticalHeaderTable.TAG:
                table = new VerticalHeaderTable(font);
                break;
            case VerticalMetricsTable.TAG:
                table = new VerticalMetricsTable(font);
                break;
            case VerticalOriginTable.TAG:
                table = new VerticalOriginTable(font);
                break;
            case GlyphSubstitutionTable.TAG:
                table = new GlyphSubstitutionTable(font);
                break;
            default:
                table = readTable(font, tag);
                break;
        }
        table.setTag(tag);
        table.setCheckSum(raf.ReadUnsignedInt());
        table.setOffset(raf.ReadUnsignedInt());
        table.setLength(raf.ReadUnsignedInt());
        
        // skip tables with zero length (except glyf)
        if (table.Length == 0 && !tag.equals(GlyphTable.TAG))
        {
            return null;
        }

        return table;
    }

    protected TTFTable readTable(TrueTypeFont font, string tag)
    {
        // unknown table type but read it anyway.
        return new TTFTable(font);
    }
}
}
