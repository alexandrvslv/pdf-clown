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
    using System;
    using System.IO;

    /**
     * This class is based on code from Apache Batik a subproject of Apache XMLGraphics. see
     * http://xmlgraphics.apache.org/batik/ for further details.
     */
    public class GlyfCompositeComp
    {

        // Flags for composite glyphs.

        /**
         * If set, the arguments are words; otherwise, they are bytes.
         */
        public static readonly short ARG_1_AND_2_ARE_WORDS = 0x0001;
        /**
         * If set, the arguments are xy values; otherwise they are points.
         */
        public static readonly short ARGS_ARE_XY_VALUES = 0x0002;
        /**
         * If set, xy values are rounded to those of the closest grid lines.
         */
        public static readonly short ROUND_XY_TO_GRID = 0x0004;
        /**
         * If set, there is a simple scale; otherwise, scale = 1.0.
         */
        public static readonly short WE_HAVE_A_SCALE = 0x0008;
        /**
         * Indicates at least one more glyph after this one.
         */
        public static readonly short MORE_COMPONENTS = 0x0020;
        /**
         * The x direction will use a different scale from the y direction.
         */
        public static readonly short WE_HAVE_AN_X_AND_Y_SCALE = 0x0040;
        /**
         * There is a 2 by2 transformation that will be used to scale the component.
         */
        public static readonly short WE_HAVE_A_TWO_BY_TWO = 0x0080;
        /**
         * Following the last component are instructions for the composite character.
         */
        public static readonly short WE_HAVE_INSTRUCTIONS = 0x0100;
        /**
         * If set, this forces the aw and lsb (and rsb) for the composite to be equal to those from this original glyph.
         */
        public static readonly short USE_MY_METRICS = 0x0200;

        private int firstIndex;
        private int firstContour;
        private readonly short argument1;
        private readonly short argument2;
        private readonly short flags;
        private readonly int glyphIndex;
        private double xscale = 1.0;
        private double yscale = 1.0;
        private double scale01 = 0.0;
        private double scale10 = 0.0;
        private int xtranslate = 0;
        private int ytranslate = 0;
        private int point1 = 0;
        private int point2 = 0;

        /**
         * Constructor.
         * 
         * @param bais the stream to be read
         * @ is thrown if something went wrong
         */
        public GlyfCompositeComp(TTFDataStream bais)
        {
            flags = bais.ReadSignedShort();
            glyphIndex = bais.ReadUnsignedShort();// number of glyph in a font is uint16

            // Get the arguments as just their raw values
            if ((flags & ARG_1_AND_2_ARE_WORDS) != 0)
            {
                // If this is set, the arguments are 16-bit (uint16 or int16)
                argument1 = bais.ReadSignedShort();
                argument2 = bais.ReadSignedShort();
            }
            else
            {
                // otherwise, they are bytes (uint8 or int8).
                argument1 = (short)bais.ReadSignedByte();
                argument2 = (short)bais.ReadSignedByte();
            }

            // Assign the arguments according to the flags
            if ((flags & ARGS_ARE_XY_VALUES) != 0)
            {
                // If this is set, the arguments are signed xy values
                xtranslate = argument1;
                ytranslate = argument2;
            }
            else
            {
                // otherwise, they are unsigned point numbers.
                //TODO why unused?
                // https://docs.microsoft.com/en-us/typography/opentype/spec/glyf
                // "In the latter case, the first point number indicates the point that is to be matched
                // to the new glyph. The second number indicates the new glyph’s “matched” point.
                // Once a glyph is added, its point numbers begin directly after the last glyphs
                // (endpoint of first glyph + 1).
                point1 = argument1;
                point2 = argument2;
            }

            // Get the scale values (if any)
            if ((flags & WE_HAVE_A_SCALE) != 0)
            {
                int i = bais.ReadSignedShort();
                xscale = yscale = i / (double)0x4000;
            }
            else if ((flags & WE_HAVE_AN_X_AND_Y_SCALE) != 0)
            {
                short i = bais.ReadSignedShort();
                xscale = i / (double)0x4000;
                i = bais.ReadSignedShort();
                yscale = i / (double)0x4000;
            }
            else if ((flags & WE_HAVE_A_TWO_BY_TWO) != 0)
            {
                int i = bais.ReadSignedShort();
                xscale = i / (double)0x4000;
                i = bais.ReadSignedShort();
                scale01 = i / (double)0x4000;
                i = bais.ReadSignedShort();
                scale10 = i / (double)0x4000;
                i = bais.ReadSignedShort();
                yscale = i / (double)0x4000;
            }
        }

        /**
         * Returns the first index.
         * 
         * @return the first index.
         */
        public int FirstIndex
        {
            get => firstIndex;
            set => firstIndex = value;
        }

        /**
         * Returns the index of the first contour.
         * 
         * @return the index of the first contour.
         */
        public int FirstContour
        {
            get => firstContour;
            set => firstContour = value;
        }

        /**
         * Returns argument 1.
         * 
         * @return argument 1.
         */
        public short Argument1
        {
            get => argument1;
        }

        /**
         * Returns argument 2.
         * 
         * @return argument 2.
         */
        public short Argument2
        {
            get => argument2;
        }

        /**
         * Returns the flags of the glyph.
         * 
         * @return the flags.
         */
        public short Flags
        {
            get => flags;
        }

        /**
         * Returns the index of the first contour.
         * 
         * @return index of the first contour.
         */
        public int GlyphIndex
        {
            get => glyphIndex;
        }

        /**
         * Returns the scale-01 value.
         * 
         * @return the scale-01 value.
         */
        public double Scale01
        {
            get => scale01;
        }

        /**
         * Returns the scale-10 value.
         * 
         * @return the scale-10 value.
         */
        public double Scale10
        {
            get => scale10;
        }

        /**
         * Returns the x-scaling value.
         * 
         * @return the x-scaling value.
         */
        public double XScale
        {
            get => xscale;
        }

        /**
         * Returns the y-scaling value.
         * 
         * @return the y-scaling value.
         */
        public double YScale
        {
            get => yscale;
        }

        /**
         * Returns the x-translation value.
         * 
         * @return the x-translation value.
         */
        public int XTranslate
        {
            get => xtranslate;
        }

        /**
         * Returns the y-translation value.
         * 
         * @return the y-translation value.
         */
        public int YTranslate
        {
            get => ytranslate;
        }

        /**
         * Transforms an x-coordinate of a point for this component.
         * 
         * @param x The x-coordinate of the point to transform
         * @param y The y-coordinate of the point to transform
         * @return The transformed x-coordinate
         */
        public int ScaleX(int x, int y)
        {
            return (int)Math.Round((float)(x * xscale + y * scale10));
        }

        /**
         * Transforms a y-coordinate of a point for this component.
         * 
         * @param x The x-coordinate of the point to transform
         * @param y The y-coordinate of the point to transform
         * @return The transformed y-coordinate
         */
        public int ScaleY(int x, int y)
        {
            return (int)Math.Round((float)(x * scale01 + y * yscale));
        }
    }
}
