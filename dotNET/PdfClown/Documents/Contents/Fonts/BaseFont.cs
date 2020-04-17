/*
 * https://github.com/apache/pdfbox
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

using System.Collections.Generic;
using SkiaSharp;

namespace PdfClown.Documents.Contents.Fonts
{
    public abstract class BaseFont
    {
        /**
         * The PostScript name of the font.
         */
        public abstract string Name { get; }

        /**
         * Returns the font's bounding box in PostScript units.
         */
        public abstract SKRect FontBBox { get; }

        /**
         * Returns the FontMatrix in PostScript units.
         */
        public abstract List<float> FontMatrix { get; }

        /**
         * Returns the path for the character with the given name.
         *
         * @return glyph path
         * @throws IOException if the path could not be read
         */
        public abstract SKPath GetPath(string name);

        /**
         * Returns the advance width for the character with the given name.
         *
         * @return glyph advance width
         * @throws IOException if the path could not be read
         */
        public abstract float GetWidth(string name);

        /**
         * Returns true if the font contains the given glyph.
         * 
         * @param name PostScript glyph name
         */
        public abstract bool HasGlyph(string name);
    }
}