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
namespace PdfClown.Documents.Contents.Fonts.TTF
{

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

        private readonly List<GlyfCompositeComp> components = new List<GlyfCompositeComp>();
        private readonly Dictionary<int, IGlyphDescription> descriptions = new Dictionary<int, IGlyphDescription>();
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
        public GlyfCompositeDescript(TTFDataStream bais, GlyphTable glyphTable)
             : base((short)-1, bais)
        {
            this.glyphTable = glyphTable;

            // Get all of the composite components
            GlyfCompositeComp comp;
            do
            {
                comp = new GlyfCompositeComp(bais);
                components.Add(comp);
            }
            while ((comp.Flags & GlyfCompositeComp.MORE_COMPONENTS) != 0);

            // Are there hinting instructions to read?
            if ((comp.Flags & GlyfCompositeComp.WE_HAVE_INSTRUCTIONS) != 0)
            {
                ReadInstructions(bais, (bais.ReadUnsignedShort()));
            }
            InitDescriptions();
        }

        /**
         * {@inheritDoc}
         */
        public override void Resolve()
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

            foreach (GlyfCompositeComp comp in components)
            {
                comp.FirstIndex = firstIndex;
                comp.FirstContour = firstContour;

                if (descriptions.TryGetValue(comp.GlyphIndex, out IGlyphDescription desc))
                {
                    desc.Resolve();
                    firstIndex += desc.PointCount;
                    firstContour += desc.ContourCount;
                }
            }
            resolved = true;
            beingResolved = false;
        }

        /**
         * {@inheritDoc}
         */
        public override int GetEndPtOfContours(int i)
        {
            GlyfCompositeComp c = GetCompositeCompEndPt(i);
            if (c != null)
            {
                descriptions.TryGetValue(c.GlyphIndex, out IGlyphDescription gd);
                return gd.GetEndPtOfContours(i - c.FirstContour) + c.FirstIndex;
            }
            return 0;
        }

        /**
         * {@inheritDoc}
         */
        public override byte GetFlags(int i)
        {
            GlyfCompositeComp c = GetCompositeComp(i);
            if (c != null)
            {
                descriptions.TryGetValue(c.GlyphIndex, out IGlyphDescription gd);
                return gd.GetFlags(i - c.FirstIndex);
            }
            return 0;
        }

        /**
         * {@inheritDoc}
         */
        public override short GetXCoordinate(int i)
        {
            GlyfCompositeComp c = GetCompositeComp(i);
            if (c != null && descriptions.TryGetValue(c.GlyphIndex, out IGlyphDescription gd))
            {
                int n = i - c.FirstIndex;
                int x = gd.GetXCoordinate(n);
                int y = gd.GetYCoordinate(n);
                short x1 = (short)c.ScaleX(x, y);
                x1 += (short)c.XTranslate;
                return x1;
            }
            return 0;
        }

        /**
         * {@inheritDoc}
         */
        public override short GetYCoordinate(int i)
        {
            GlyfCompositeComp c = GetCompositeComp(i);
            if (c != null && descriptions.TryGetValue(c.GlyphIndex, out IGlyphDescription gd))
            {
                int n = i - c.FirstIndex;
                int x = gd.GetXCoordinate(n);
                int y = gd.GetYCoordinate(n);
                short y1 = (short)c.ScaleY(x, y);
                y1 += (short)c.YTranslate;
                return y1;
            }
            return 0;
        }

        /**
         * {@inheritDoc}
         */
        public override bool IsComposite
        {
            get => true;
        }

        /**
         * {@inheritDoc}
         */
        public override int PointCount
        {
            get
            {
                if (!resolved)
                {
                    Debug.WriteLine("error: getPointCount called on unresolved GlyfCompositeDescript");
                }
                if (pointCount < 0)
                {
                    GlyfCompositeComp c = components[components.Count - 1];
                    if (!descriptions.TryGetValue(c.GlyphIndex, out IGlyphDescription gd))
                    {
                        Debug.WriteLine($"error: GlyphDescription for index {c.GlyphIndex} is null, returning 0");
                        pointCount = 0;
                    }
                    else
                    {
                        pointCount = c.FirstIndex + gd.PointCount;
                    }
                }
                return pointCount;
            }
        }

        /**
         * {@inheritDoc}
         */
        public override int ContourCount
        {
            get
            {
                if (!resolved)
                {
                    Debug.WriteLine("error: getContourCount called on unresolved GlyfCompositeDescript");
                }
                if (contourCount < 0)
                {
                    GlyfCompositeComp c = components[components.Count - 1];
                    contourCount = c.FirstContour +
                        (descriptions.TryGetValue(c.GlyphIndex, out IGlyphDescription gd) ? gd.ContourCount : 0);
                }
                return contourCount;
            }
        }

        /**
         * Get number of components.
         * 
         * @return the number of components
         */
        public int ComponentCount
        {
            get => components.Count;
        }

        private GlyfCompositeComp GetCompositeComp(int i)
        {
            foreach (GlyfCompositeComp c in components)
            {
                if (c.FirstIndex <= i
                    && descriptions.TryGetValue(c.GlyphIndex, out IGlyphDescription gd)
                    && i < (c.FirstIndex + gd.PointCount))
                {
                    return c;
                }
            }
            return null;
        }

        private GlyfCompositeComp GetCompositeCompEndPt(int i)
        {
            foreach (GlyfCompositeComp c in components)
            {
                ;
                if (c.FirstContour <= i
                    && descriptions.TryGetValue(c.GlyphIndex, out IGlyphDescription gd)
                    && i < (c.FirstContour + gd.ContourCount))
                {
                    return c;
                }
            }
            return null;
        }

        private void InitDescriptions()
        {
            foreach (GlyfCompositeComp component in components)
            {
                try
                {
                    int index = component.GlyphIndex;
                    GlyphData glyph = glyphTable.GetGlyph(index);
                    if (glyph != null)
                    {
                        descriptions[index] = glyph.Description;
                    }
                }
                catch (IOException e)
                {
                    Debug.WriteLine($"error: {e}");
                }
            }
        }
    }
}
