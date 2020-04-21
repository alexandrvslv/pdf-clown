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

    /**
     * This class is based on code from Apache Batik a subproject of Apache XMLGraphics.
     * see http://xmlgraphics.apache.org/batik/ for further details.
     * 
     */
    public abstract class GlyfDescript : GlyphDescription
    {

        // Flags describing a coordinate of a glyph.
        /**
         * if set, the point is on the curve.
         */
        public static readonly byte ON_CURVE = 0x01;
        /**
         * if set, the x-coordinate is 1 byte long.
         */
        public static readonly byte X_SHORT_VECTOR = 0x02;
        /**
         * if set, the y-coordinate is 1 byte long.
         */
        public static readonly byte Y_SHORT_VECTOR = 0x04;
        /**
         * if set, the next byte specifies the number of additional 
         * times this set of flags is to be repeated.
         */
        public static readonly byte REPEAT = 0x08;
        /**
         * This flag as two meanings, depending on how the
         * x-short vector flags is set.
         * If the x-short vector is set, this bit describes the sign
         * of the value, with 1 equaling positive and 0 positive.
         * If the x-short vector is not set and this bit is also not
         * set, the current x-coordinate is a signed 16-bit delta vector.
         */
        public static readonly byte X_DUAL = 0x10;
        /**
         * This flag as two meanings, depending on how the
         * y-short vector flags is set.
         * If the y-short vector is set, this bit describes the sign
         * of the value, with 1 equaling positive and 0 positive.
         * If the y-short vector is not set and this bit is also not
         * set, the current y-coordinate is a signed 16-bit delta vector.
         */
        public static readonly byte Y_DUAL = 0x20;

        private int[] instructions;
        private readonly int contourCount;

        /**
         * Constructor.
         * 
         * @param numberOfContours the number of contours
         * @param bais the stream to be read
         * @ is thrown if something went wrong
         */
        public GlyfDescript(short numberOfContours, TTFDataStream bais)
        {
            contourCount = numberOfContours;
        }

        /**
         * {@inheritDoc}
         */
        public virtual int ContourCount
        {
            get => contourCount;
        }

        /**
         * Returns the hinting instructions.
         * @return an array containing the hinting instructions.
         */
        public virtual int[] Instructions
        {
            get => instructions;
        }

        public abstract bool IsComposite { get; }

        public abstract int PointCount { get; }

        /**
         * Read the hinting instructions.
         * @param bais the stream to be read
         * @param count the number of instructions to be read 
         * @ is thrown if something went wrong
         */
        protected void ReadInstructions(TTFDataStream bais, int count)
        {
            instructions = bais.ReadUnsignedByteArray(count);
        }

        /**
         * {@inheritDoc}
         */
        public virtual void Resolve()
        {
        }

        public abstract int GetEndPtOfContours(int i);
        public abstract byte GetFlags(int i);
        public abstract short GetXCoordinate(int i);
        public abstract short GetYCoordinate(int i);
    }
}
