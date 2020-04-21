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

    using System;
    using System.IO;


    /**
     * A TrueType Collection, now more properly known as a "Font Collection" as it may contain either
     * TrueType or OpenType fonts.
     * 
     * @author John Hewson
     */
    public class TrueTypeCollection : IDisposable
    {
        private readonly TTFDataStream stream;
        private readonly int numFonts;
        private readonly long[] fontOffsets;

        /**
         * Creates a new TrueTypeCollection from a .ttc file.
         *
         * @param file The TTC file.
         * @ If the font could not be parsed.
         */
        public TrueTypeCollection(File file)
                : this(new RAFDataStream(file, "r"))
        {
        }

        /**
         * Creates a new TrueTypeCollection from a .ttc input stream.
         *
         * @param stream A TTC input stream.
         * @ If the font could not be parsed.
         */
        public TrueTypeCollection(Bytes.Buffer stream)
            : this(new MemoryTTFDataStream(stream))
        {
        }

        /**
         * Creates a new TrueTypeCollection from a TTC stream.
         *
         * @param stream The TTF file.
         * @ If the font could not be parsed.
         */
        public TrueTypeCollection(TTFDataStream stream)
        {
            this.stream = stream;

            // TTC header
            string tag = stream.ReadTag();
            if (!tag.Equals("ttcf", StringComparison.Ordinal))
            {
                throw new IOException("Missing TTC header");
            }
            float version = stream.Read32Fixed();
            numFonts = (int)stream.ReadUnsignedInt();
            fontOffsets = new long[numFonts];
            for (int i = 0; i < numFonts; i++)
            {
                fontOffsets[i] = stream.ReadUnsignedInt();
            }
            if (version >= 2)
            {
                // not used at this time
                int ulDsigTag = stream.ReadUnsignedShort();
                int ulDsigLength = stream.ReadUnsignedShort();
                int ulDsigOffset = stream.ReadUnsignedShort();
            }
        }

        /**
         * Run the callback for each TT font in the collection.
         * 
         * @param trueTypeFontProcessor the object with the callback method.
         * @ 
         */
        public void ProcessAllFonts(TrueTypeFontProcessor trueTypeFontProcessor)
        {
            for (int i = 0; i < numFonts; i++)
            {
                TrueTypeFont font = getFontAtIndex(i);
                trueTypeFontProcessor.Process(font);
            }
        }

        private TrueTypeFont getFontAtIndex(int idx)
        {
            stream.Seek(fontOffsets[idx]);
            TTFParser parser;
            if (stream.ReadTag().Equals("OTTO", StringComparison.Ordinal))
            {
                parser = new OTFParser(false, true);
            }
            else
            {
                parser = new TTFParser(false, true);
            }
            stream.Seek(fontOffsets[idx]);
            return parser.Parse(new TTCDataStream(stream));
        }

        /**
         * Get a TT font from a collection.
         * 
         * @param name The postscript name of the font.
         * @return The found font, nor null if none is found.
         * @ 
         */
        public TrueTypeFont getFontByName(string name)
        {
            for (int i = 0; i < numFonts; i++)
            {
                TrueTypeFont font = getFontAtIndex(i);
                if (font.Name.Equals(name, StringComparison.Ordinal))
                {
                    return font;
                }
            }
            return null;
        }

        /**
         * Implement the callback method to call {@link TrueTypeCollection#processAllFonts(TrueTypeFontProcessor)}.
         */
        //@FunctionalInterface
        public interface TrueTypeFontProcessor
        {
            void Process(TrueTypeFont ttf);
        }

        public void Dispose()
        {
            stream.Dispose();
        }
    }
}
