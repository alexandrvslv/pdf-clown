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
namespace PdfClown.Documents.Contents.Fonts.TTF
{

    using SkiaSharp;
    using System.IO;

    /**
     * A glyph data record in the glyf table.
     * 
     * @author Ben Litchfield
     */
    public class GlyphData
    {
        private short xMin;
        private short yMin;
        private short xMax;
        private short yMax;
        private SKRect? boundingBox = null;
        private short numberOfContours;
        private GlyfDescript glyphDescription = null;
        private GlyphRenderer renderer;

        /**
         * This will read the required data from the stream.
         * 
         * @param glyphTable The glyph table this glyph belongs to.
         * @param data The stream to read the data from.
         * @param leftSideBearing The left side bearing for this glyph.
         * @ If there is an error reading the data.
         */
        public void InitData(GlyphTable glyphTable, TTFDataStream data, int leftSideBearing)
        {
            numberOfContours = data.ReadSignedShort();
            xMin = data.ReadSignedShort();
            yMin = data.ReadSignedShort();
            xMax = data.ReadSignedShort();
            yMax = data.ReadSignedShort();
            boundingBox = new SKRect(xMin, yMin, xMax, yMax);

            if (numberOfContours >= 0)
            {
                // create a simple glyph
                short x0 = (short)(leftSideBearing - xMin);
                glyphDescription = new GlyfSimpleDescript(numberOfContours, data, x0);
            }
            else
            {
                // create a composite glyph
                glyphDescription = new GlyfCompositeDescript(data, glyphTable);
            }
        }

        /**
         * @return Returns the boundingBox.
         */
        public SKRect BoundingBox
        {
            get => boundingBox ?? SKRect.Empty;
            set => boundingBox = value;
        }

        /**
         * @return Returns the numberOfContours.
         */
        public short NumberOfContours
        {
            get => numberOfContours;
            set => numberOfContours = value;
        }

        /**
         * Returns the description of the glyph.
         * @return the glyph description
         */
        public IGlyphDescription Description
        {
            get => glyphDescription;
        }

        /**
         * Returns the path of the glyph.
         * @return the path
         */
        public SKPath GetPath()
        {
            return (renderer ?? (renderer = new GlyphRenderer(glyphDescription))).GetPath();
        }

        /**
         * Returns the xMax value.
         * @return the XMax value
         */
        public short XMaximum
        {
            get => xMax;
        }

        /**
         * Returns the xMin value.
         * @return the xMin value
         */
        public short XMinimum
        {
            get => xMin;
        }

        /**
         * Returns the yMax value.
         * @return the yMax value
         */
        public short YMaximum
        {
            get => yMax;
        }

        /**
         * Returns the yMin value.
         * @return the yMin value
         */
        public short YMinimum
        {
            get => yMin;
        }
    }

}