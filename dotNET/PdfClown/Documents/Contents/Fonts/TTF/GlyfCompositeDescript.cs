/*

   Licensed to the Apache Software Foundation (ASF) under one or more
   contributor license agreements.  See the NOTICE file distributed with
   this work for additional information regarding copyright ownership.
   The ASF licenses this file to You under the Apache License, Version 2.0
   (the "License"); you may not use this file except in compliance with
   the License.  You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.

 */
namespace PdfClown.Documents.Contents.Fonts.TTF{

using System.IO;


using System.Collections.Generic;


using System.Diagnostics;


/**
 * Glyph description for composite glyphs. Composite glyphs are made up of one
 * or more simple glyphs, usually with some sort of transformation applied to
 * each.
 *
 * This class is based on code from Apache Batik a subproject of Apache
 * XMLGraphics. see http://xmlgraphics.apache.org/batik/ for further details.
 */
public class GlyfCompositeDescript : GlyfDescript
{
    /**
     * Log instance.
     */
    //private static readonly Log LOG = LogFactory.getLog(GlyfCompositeDescript.class);

    private readonly List<GlyfCompositeComp> components = new List<>();
    private readonly Dictionary<int,GlyphDescription> descriptions = new Dictionary<>();
    private GlyphTable glyphTable = null;
    private bool beingResolved = false;
    private bool resolved = false;
    private int pointCount = -1;
    private int contourCount = -1;

    /**
     * Constructor.
     * 
     * @param bais the stream to be read
     * @param glyphTable the Glyphtable containing all glyphs
     * @ is thrown if something went wrong
     */
    GlyfCompositeDescript(TTFDataStream bais, GlyphTable glyphTable) 
    {
        :base((short) -1, bais);

        this.glyphTable = glyphTable;

        // Get all of the composite components
        GlyfCompositeComp comp;
        do
        {
            comp = new GlyfCompositeComp(bais);
            components.Add(comp);
        } 
        while ((comp.GetFlags() & GlyfCompositeComp.MORE_COMPONENTS) != 0);

        // Are there hinting instructions to read?
        if ((comp.GetFlags() & GlyfCompositeComp.WE_HAVE_INSTRUCTIONS) != 0)
        {
            readInstructions(bais, (bais.ReadUnsignedShort()));
        }
        initDescriptions();
    }

    /**
     * {@inheritDoc}
     */
    public override void resolve()
    {
        if (resolved)
        {
            return;
        }
        if (beingResolved)
        {
            Debug.WriteLine("error: Circular reference in GlyfCompositeDesc");
            return;
        }
        beingResolved = true;

        int firstIndex = 0;
        int firstContour = 0;

        for (GlyfCompositeComp comp : components)
        {
            comp.setFirstIndex(firstIndex);
            comp.setFirstContour(firstContour);

            GlyphDescription desc = descriptions.get(comp.GetGlyphIndex());
            if (desc != null)
            {
                desc.resolve();
                firstIndex += desc.getPointCount();
                firstContour += desc.getContourCount();
            }
        }
        resolved = true;
        beingResolved = false;
    }

    /**
     * {@inheritDoc}
     */
    public override int getEndPtOfContours(int i)
    {
        GlyfCompositeComp c = getCompositeCompEndPt(i);
        if (c != null)
        {
            GlyphDescription gd = descriptions.get(c.GetGlyphIndex());
            return gd.getEndPtOfContours(i - c.getFirstContour()) + c.getFirstIndex();
        }
        return 0;
    }

    /**
     * {@inheritDoc}
     */
    public override byte GetFlags(int i)
    {
        GlyfCompositeComp c = getCompositeComp(i);
        if (c != null)
        {
            GlyphDescription gd = descriptions.get(c.GetGlyphIndex());
            return gd.GetFlags(i - c.getFirstIndex());
        }
        return 0;
    }

    /**
     * {@inheritDoc}
     */
    public override short getXCoordinate(int i)
    {
        GlyfCompositeComp c = getCompositeComp(i);
        if (c != null)
        {
            GlyphDescription gd = descriptions.get(c.GetGlyphIndex());
            int n = i - c.getFirstIndex();
            int x = gd.getXCoordinate(n);
            int y = gd.getYCoordinate(n);
            short x1 = (short) c.scaleX(x, y);
            x1 += c.getXTranslate();
            return x1;
        }
        return 0;
    }

    /**
     * {@inheritDoc}
     */
    public override short getYCoordinate(int i)
    {
        GlyfCompositeComp c = getCompositeComp(i);
        if (c != null)
        {
            GlyphDescription gd = descriptions.get(c.GetGlyphIndex());
            int n = i - c.getFirstIndex();
            int x = gd.getXCoordinate(n);
            int y = gd.getYCoordinate(n);
            short y1 = (short) c.scaleY(x, y);
            y1 += c.getYTranslate();
            return y1;
        }
        return 0;
    }

    /**
     * {@inheritDoc}
     */
    public override bool isComposite()
    {
        return true;
    }

    /**
     * {@inheritDoc}
     */
    public override int getPointCount()
    {
        if (!resolved)
        {
            Debug.WriteLine("error: getPointCount called on unresolved GlyfCompositeDescript");
        }
        if (pointCount < 0)
        {
            GlyfCompositeComp c = components.get(components.Count - 1);
            GlyphDescription gd = descriptions.get(c.GetGlyphIndex());
            if (gd == null)
            {
                Debug.WriteLine("error: GlyphDescription for index " + c.GetGlyphIndex() + " is null, returning 0");
                pointCount = 0;
            }
            else
            {
                pointCount = c.getFirstIndex() + gd.getPointCount();
            }
        }   
        return pointCount;
    }

    /**
     * {@inheritDoc}
     */
    public override int getContourCount()
    {
        if (!resolved)
        {
            Debug.WriteLine("error: getContourCount called on unresolved GlyfCompositeDescript");
        }
        if (contourCount < 0)
        {
            GlyfCompositeComp c = components.get(components.Count - 1);
            contourCount = c.getFirstContour() + descriptions.get(c.GetGlyphIndex()).getContourCount();
        }
        return contourCount;
    }

    /**
     * Get number of components.
     * 
     * @return the number of components
     */
    public int getComponentCount()
    {
        return components.Count;
    }

    private GlyfCompositeComp getCompositeComp(int i)
    {
        foreach (GlyfCompositeComp c in components)
        {
            GlyphDescription gd = descriptions.get(c.GetGlyphIndex());
            if (c.getFirstIndex() <= i && gd != null && i < (c.getFirstIndex() + gd.getPointCount()))
            {
                return c;
            }
        }
        return null;
    }

    private GlyfCompositeComp getCompositeCompEndPt(int i)
    {
        foreach (GlyfCompositeComp c in components)
        {
            GlyphDescription gd = descriptions.get(c.GetGlyphIndex());
            if (c.getFirstContour() <= i && gd != null && i < (c.getFirstContour() + gd.getContourCount()))
            {
                return c;
            }
        }
        return null;
    }

    private void initDescriptions()
    {
        for (GlyfCompositeComp component : components)
        {
            try
            {
                int index = component.GetGlyphIndex();
                GlyphData glyph = glyphTable.getGlyph(index);
                if (glyph != null)
                {
                    descriptions[index, glyph.getDescription());
                }
            }
            catch (IOException e)
            {
                LOG.error(e);
            }            
        }
    }
}
}
