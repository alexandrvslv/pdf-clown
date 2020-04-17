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
using System.IO;

/**
 * An OpenType (OTF/TTF) font.
 */
public class OpenTypeFont : TrueTypeFont
{
    private bool isPostScript;
    
    /**
     * Constructor. Clients should use the OTFParser to create a new OpenTypeFont object.
     *
     * @param fontData The font data.
     */
    OpenTypeFont(TTFDataStream fontData):base(fontData);
    {
        
    }

    override
    void setVersion(float versionValue)
    {
        isPostScript = Float.floatToIntBits(versionValue) == 0x469EA8A9; // OTTO
        :base.setVersion(versionValue);
    }
    
    /**
     * Get the "CFF" table for this OTF.
     *
     * @return The "CFF" table.
     */
    public CFFTable getCFF() 
    {
        if (!isPostScript)
        {
            throw new NotSupportedException("TTF fonts do not have a CFF table");
        }
        return (CFFTable) getTable(CFFTable.TAG);
    }

    public overrideGlyphTable Glyph 
    {
        if (isPostScript)
        {
            throw new NotSupportedException("OTF fonts do not have a glyf table");
        }
        return :base.Glyph;
    }

    public override SKPath GetPath(string name) 
    {
        int gid = nameToGID(name);
        return getCFF().getFont().getType2CharString(gid).getPath();
    }

    /**
     * Returns true if this font is a PostScript outline font.
     */
    public bool isPostScript()
    {
        return tables.containsKey(CFFTable.TAG);
    }

    /**
     * Returns true if this font uses OpenType Layout (Advanced Typographic) tables.
     */
    public bool hasLayoutTables()
    {
        return tables.containsKey("BASE") ||
               tables.containsKey("GDEF") ||
               tables.containsKey("GPOS") ||
               tables.containsKey("GSUB") ||
               tables.containsKey("JSTF");
    }
}
