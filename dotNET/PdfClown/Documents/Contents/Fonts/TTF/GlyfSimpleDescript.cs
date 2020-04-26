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
    using System.Diagnostics;


    /**
     * This class is based on code from Apache Batik a subproject of Apache XMLGraphics. see
     * http://xmlgraphics.apache.org/batik/ for further details.
     */
    public class GlyfSimpleDescript : GlyfDescript
    {

        /**
         * Log instance.
         */
        //private static readonly Log LOG = LogFactory.getLog(GlyfSimpleDescript.class);

        private ushort[] endPtsOfContours;
        private byte[] flags;
        private short[] xCoordinates;
        private short[] yCoordinates;
        private readonly int pointCount;

        /**
         * Constructor.
         * 
         * @param numberOfContours number of contours
         * @param bais the stream to be read
         * @param x0 the initial X-position
         * @ is thrown if something went wrong
         */
        public GlyfSimpleDescript(short numberOfContours, TTFDataStream bais, short x0)
            : base(numberOfContours, bais)
        {

            /*
             * https://developer.apple.com/fonts/TTRefMan/RM06/Chap6glyf.html
             * "If a glyph has zero contours, it need not have any glyph data." set the pointCount to zero to initialize
             * attributes and avoid nullpointer but maybe there shouldn't have GlyphDescript in the GlyphData?
             */
            if (numberOfContours == 0)
            {
                pointCount = 0;
                return;
            }

            // Simple glyph description
            endPtsOfContours = bais.ReadUnsignedShortArray(numberOfContours);

            int lastEndPt = endPtsOfContours[numberOfContours - 1];
            if (numberOfContours == 1 && lastEndPt == 65535)
            {
                // PDFBOX-2939: assume an empty glyph
                pointCount = 0;
                return;
            }
            // The last end point index reveals the total number of points
            pointCount = lastEndPt + 1;

            flags = new byte[pointCount];
            xCoordinates = new short[pointCount];
            yCoordinates = new short[pointCount];

            int instructionCount = bais.ReadUnsignedShort();
            ReadInstructions(bais, instructionCount);
            ReadFlags(pointCount, bais);
            ReadCoords(pointCount, bais, x0);
        }

        /**
         * {@inheritDoc}
         */
        public override int GetEndPtOfContours(int i)
        {
            return endPtsOfContours[i];
        }

        /**
         * {@inheritDoc}
         */
        public override byte GetFlags(int i)
        {
            return flags[i];
        }

        /**
         * {@inheritDoc}
         */
        public override short GetXCoordinate(int i)
        {
            return xCoordinates[i];
        }

        /**
         * {@inheritDoc}
         */
        public override short GetYCoordinate(int i)
        {
            return yCoordinates[i];
        }

        /**
         * {@inheritDoc}
         */
        public override bool IsComposite
        {
            get => false;
        }

        /**
         * {@inheritDoc}
         */
        public override int PointCount
        {
            get => pointCount;
        }

        /**
         * The table is stored as relative values, but we'll store them as absolutes.
         */
        private void ReadCoords(int count, TTFDataStream bais, short x0)
        {
            short x = x0;
            short y = 0;
            for (int i = 0; i < count; i++)
            {
                if ((flags[i] & X_DUAL) != 0)
                {
                    if ((flags[i] & X_SHORT_VECTOR) != 0)
                    {
                        x += (short)bais.ReadUnsignedByte();
                    }
                }
                else
                {
                    if ((flags[i] & X_SHORT_VECTOR) != 0)
                    {
                        x += (short)-((short)bais.ReadUnsignedByte());
                    }
                    else
                    {
                        x += bais.ReadSignedShort();
                    }
                }
                xCoordinates[i] = x;
            }

            for (int i = 0; i < count; i++)
            {
                if ((flags[i] & Y_DUAL) != 0)
                {
                    if ((flags[i] & Y_SHORT_VECTOR) != 0)
                    {
                        y += (short)bais.ReadUnsignedByte();
                    }
                }
                else
                {
                    if ((flags[i] & Y_SHORT_VECTOR) != 0)
                    {
                        y += (short)-((short)bais.ReadUnsignedByte());
                    }
                    else
                    {
                        y += bais.ReadSignedShort();
                    }
                }
                yCoordinates[i] = y;
            }
        }

        /**
         * The flags are run-length encoded.
         */
        private void ReadFlags(int flagCount, TTFDataStream bais)
        {
            for (int index = 0; index < flagCount; index++)
            {
                flags[index] = (byte)bais.ReadUnsignedByte();
                if ((flags[index] & REPEAT) != 0)
                {
                    int repeats = bais.ReadUnsignedByte();
                    for (int i = 1; i <= repeats && index + i < flags.Length; i++)
                    {
                        flags[index + i] = flags[index];
                    }
                    index += repeats;
                }
            }
        }
    }
}
