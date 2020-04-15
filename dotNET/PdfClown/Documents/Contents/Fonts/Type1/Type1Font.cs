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

//import java.awt.geom.GeneralPath;
//import java.io.IOException;
//import java.io.InputStream;
//import java.util.ArrayList;
//import java.util.Collections;
//import java.util.LinkedHashMap;
//import java.util.List;
//import java.util.Dictionary;
//import java.util.concurrent.ConcurrentHashMap;
//import org.apache.fontbox.FontBoxFont;
//import org.apache.fontbox.EncodedFont;
//import org.apache.fontbox.cff.Type1CharString;
//import org.apache.fontbox.cff.Type1CharStringParser;
//import org.apache.fontbox.encoding.Encoding;
//import org.apache.fontbox.pfb.PfbParser;
//import org.apache.fontbox.util.BoundingBox;
using System;
using System.IO;
using System.Collections.Generic;
using SkiaSharp;
using PdfClown.Util;

namespace PdfClown.Documents.Contents.Fonts.Type1
{
    /**
     * Represents an Adobe Type 1 (.pfb) font. Thread safe.
     *
     * @author John Hewson
     */
    public sealed class Type1Font : Type1CharStringReader//, EncodedFont, FontBoxFont
    {
        /**
		 * Constructs a new Type1Font object from a .pfb stream.
		 *
		 * @param pfbStream .pfb input stream, including headers
		 * @return a type1 font
		 * 
		 * @throws IOException if something went wrong
		 */
        public static Type1Font CreateWithPFB(Bytes.Buffer pfbStream)
        {
            PfbParser pfb = new PfbParser(pfbStream);
            Type1Parser parser = new Type1Parser();
            return parser.Parse(pfb.GetSegment1().ToArray(), pfb.GetSegment2().ToArray());
        }

        /**
		 * Constructs a new Type1Font object from a .pfb stream.
		 *
		 * @param pfbBytes .pfb data, including headers
		 * @return a type1 font
		 *
		 * @throws IOException if something went wrong
		 */
        public static Type1Font createWithPFB(byte[] pfbBytes)
        {
            PfbParser pfb = new PfbParser(pfbBytes);
            Type1Parser parser = new Type1Parser();
            return parser.Parse(pfb.GetSegment1().ToArray(), pfb.GetSegment2().ToArray());
        }

        /**
		 * Constructs a new Type1Font object from two header-less .pfb segments.
		 *
		 * @param segment1 The first segment, without header
		 * @param segment2 The second segment, without header
		 * @return A new Type1Font instance
		 * @throws IOException if something went wrong
		 */
        public static Type1Font createWithSegments(byte[] segment1, byte[] segment2)
        {
            Type1Parser parser = new Type1Parser();
            return parser.Parse(segment1, segment2);
        }

        // font dictionary
        string fontName = "";
        Encoding encoding = null;
        int paintType;
        int fontType;
        List<float> fontMatrix = new List<float>();
        List<float> fontBBox = new List<float>();
        int uniqueID;
        float strokeWidth;
        string fontID = "";

        // FontInfo dictionary
        string version = "";
        string notice = "";
        string fullName = "";
        string familyName = "";
        string weight = "";
        float italicAngle;
        bool isFixedPitch;
        float underlinePosition;
        float underlineThickness;

        // Private dictionary
        List<float> blueValues = new List<float>();
        List<float> otherBlues = new List<float>();
        List<float> familyBlues = new List<float>();
        List<float> familyOtherBlues = new List<float>();
        float blueScale;
        int blueShift, blueFuzz;
        List<float> stdHW = new List<float>();
        List<float> stdVW = new List<float>();
        List<float> stemSnapH = new List<float>();
        List<float> stemSnapV = new List<float>();
        bool forceBold;
        int languageGroup;

        // Subrs array, and CharStrings dictionary
        readonly List<byte[]> subrs = new List<byte[]>();
        readonly Dictionary<string, byte[]> charstrings = new Dictionary<string, byte[]>(StringComparer.Ordinal);

        // private caches
        private readonly Dictionary<string, Type1CharString> charStringCache = new Dictionary<string, Type1CharString>(StringComparer.Ordinal);

        // raw data
        private readonly byte[] segment1, segment2;

        /**
		 * Constructs a new Type1Font, called by Type1Parser.
		 */
        public Type1Font(byte[] segment1, byte[] segment2)
        {
            this.segment1 = segment1;
            this.segment2 = segment2;
        }

        /**
		 * Returns the /Subrs array as raw bytes.
		 *
		 * @return Type 1 char string bytes
		 */
        public List<byte[]> SubrsArray
        {
            get => subrs;
        }

        /**
		 * Returns the /CharStrings dictionary as raw bytes.
		 *
		 * @return Type 1 char string bytes
		 */
        public Dictionary<string, byte[]> CharStringsDict
        {
            get => charstrings;
        }


        public string Name => fontName;

        public SKPath GetPath(string name)
        {
            return GetType1CharString(name).Path;
        }


        public float GetWidth(string name)
        {
            return GetType1CharString(name).Width;
        }

        public bool HasGlyph(string name)
        {
            return charstrings.TryGetValue(name, out _);
        }

        public Type1CharString GetType1CharString(ByteArray key)
        {

        }

        public Type1CharString GetType1CharString(string name)
        {
            if (!charStringCache.TryGetValue(name, out Type1CharString type1))
            {
                if (!charstrings.TryGetValue(name, out byte[] bytes))
                {
                    bytes = charstrings[".notdef"];
                }
                List<Object> sequence = Type1CharStringParser.Parse(fontName, name, bytes, subrs);
                type1 = new Type1CharString(this, fontName, name, sequence);
                charStringCache.Add(name, type1);
            }
            return type1;
        }

        // font dictionary

        /**
		 * Returns the font name.
		 * 
		 * @return the font name
		 */
        public string FontName
        {
            get => fontName;
            set => fontName = value;
        }

        /**
		 * Returns the Encoding, if present.
		 * @return the encoding or null
		 */
        public Encoding Encoding
        {
            get => encoding;
            set => encoding = value;
        }

        /**
		 * Returns the paint type.
		 * 
		 * @return the paint type
		 */
        public int PaintType
        {
            get => paintType;
            set => paintType = value;
        }

        /**
		 * Returns the font type.
		 * 
		 * @return the font type
		 */
        public int FontType
        {
            get => fontType;
            set => fontType = value;
        }

        /**
		 * Returns the font matrix.
		 * 
		 * @return the font matrix
		 */
        public List<float> FontMatrix
        {
            get => fontMatrix;
            set => fontMatrix = value;
        }

        /**
		 * Returns the font bounding box.
		 * 
		 * @return the font bounding box
		 */
        public List<float> FontBBox
        {
            get => fontBBox;
            set => fontBBox = value;
        }

        /**
		 * Returns unique ID.
		 * 
		 * @return the unique ID
		 */
        public int UniqueID
        {
            get => uniqueID;
            set => uniqueID = value;
        }

        /**
		 * Returns the stroke width.
		 * 
		 * @return the stroke width
		 */
        public float StrokeWidth
        {
            get => strokeWidth;
            set => strokeWidth = value;
        }

        /**
		 * Returns the font ID.
		 * 
		 * @return the font ID
		 */
        public string FontID
        {
            get => fontID;
            set => fontID = value;
        }

        // FontInfo dictionary

        /**
		 * Returns the version.
		 * 
		 * @return the version
		 */
        public string Version
        {
            get => version;
            set => version = value;
        }

        /**
		 * Returns the notice.
		 * 
		 * @return the notice
		 */
        public string Notice
        {
            get => notice;
            set => notice = value;
        }

        /**
		 * Returns the full name.
		 *
		 * @return the full name
		 */
        public string FullName
        {
            get => fullName;
            set => fullName = value;
        }

        /**
		 * Returns the family name.
		 * 
		 * @return the family name
		 */
        public string FamilyName
        {
            get => familyName;
            set => familyName = value;
        }

        /**
		 * Returns the weight.
		 * 
		 * @return the weight
		 */
        public string Weight
        {
            get => weight;
            set => weight = value;
        }

        /**
		 * Returns the italic angle.
		 * 
		 * @return the italic angle
		 */
        public float ItalicAngle
        {
            get => italicAngle;
            set => italicAngle = value;
        }

        /**
		 * Determines if the font has a fixed pitch.
		 * 
		 * @return true if the font has a fixed pitch
		 */
        public bool FixedPitch
        {
            get => isFixedPitch;
            set => isFixedPitch = value;
        }

        /**
		 * Returns the underline position
		 * 
		 * @return the underline position
		 */
        public float UnderlinePosition
        {
            get => underlinePosition;
            set => underlinePosition = value;
        }

        /**
		 * Returns the underline thickness.
		 * 
		 * @return the underline thickness
		 */
        public float UnderlineThickness
        {
            get => underlineThickness;
            set => underlineThickness = value;
        }

        // Private dictionary

        /**
		 * Returns the blues values.
		 * 
		 * @return the blues values
		 */
        public List<float> BlueValues
        {
            get => blueValues;
            set => blueValues = value;
        }

        /**
		 * Returns the other blues values.
		 * 
		 * @return the other blues values
		 */
        public List<float> OtherBlues
        {
            get => otherBlues;
            set => otherBlues = value;
        }

        /**
		 * Returns the family blues values.
		 * 
		 * @return the family blues values
		 */
        public List<float> FamilyBlues
        {
            get => familyBlues;
            set => familyBlues = value;
        }

        /**
		 * Returns the other family blues values.
		 * 
		 * @return the other family blues values
		 */
        public List<float> FamilyOtherBlues
        {
            get => familyOtherBlues;
            set => familyOtherBlues = value;
        }

        /**
		 * Returns the blue scale.
		 * 
		 * @return the blue scale
		 */
        public float BlueScale
        {
            get => blueScale;
            set => blueScale = value;
        }

        /**
		 * Returns the blue shift.
		 * 
		 * @return the blue shift
		 */
        public int BlueShift
        {
            get => blueShift;
            set => blueShift = value;
        }

        /**
		 * Returns the blue fuzz.
		 * 
		 * @return the blue fuzz
		 */
        public int BlueFuzz
        {
            get => blueFuzz;
            set => blueFuzz = value;
        }

        /**
		 * Returns the StdHW value.
		 * 
		 * @return the StdHW value
		 */
        public List<float> StdHW
        {
            get => stdHW;
            set => stdHW = value;
        }

        /**
		 * Returns the StdVW value.
		 * 
		 * @return the StdVW value
		 */
        public List<float> StdVW
        {
            get => stdVW;
            set => stdVW = value;
        }

        /**
		 * Returns the StemSnapH value.
		 * 
		 * @return the StemSnapH value
		 */
        public List<float> StemSnapH
        {
            get => stemSnapH;
            set => stemSnapH = value;
        }

        /**
		 * Returns the StemSnapV value.
		 * 
		 * @return the StemSnapV value
		 */
        public List<float> StemSnapV
        {
            get => stemSnapV;
            set => stemSnapV = value;
        }

        /**
		 * Determines if the font is bold.
		 * 
		 * @return true if the font is bold
		 */
        public bool IsForceBold
        {
            get => forceBold;
            set => forceBold = value;
        }

        /**
		 * Returns the language group.
		 * 
		 * @return the language group
		 */
        public int LanguageGroup
        {
            get => languageGroup;
            set => languageGroup = value;
        }

        /**
		 * Returns the ASCII segment.
		 *
		 * @return the ASCII segment.
		 */
        public byte[] ASCIISegment
        {
            get => segment1;
        }

        /**
		 * Returns the binary segment.
		 *
		 * @return the binary segment.
		 */
        public byte[] BinarySegment
        {
            get => segment2;
        }

        /**
		 * {@inheritDoc}
		 */
        override public string ToString()
        {
            return GetType().Name + "[fontName=" + fontName + ", fullName=" + fullName
                    + ", encoding=" + encoding + ", charStringsDict=" + charstrings
                    + "]";
        }
    }
}