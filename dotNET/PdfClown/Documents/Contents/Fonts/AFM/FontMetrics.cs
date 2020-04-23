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
using SkiaSharp;
using System;
using System.Collections.Generic;

namespace PdfClown.Documents.Contents.Fonts.AFM
{

    /**
     * This is the outermost AFM type.  This can be created by the afmparser with a valid AFM document.
     *
     * @author Ben Litchfield
     */
    public class FontMetrics
    {
        /**
         * This is the version of the FontMetrics.
         */
        private float afmVersion;
        private int metricSets = 0;
        private string fontName;
        private string fullName;
        private string familyName;
        private string weight;
        private SKRect fontBBox;
        private string fontVersion;
        private string notice;
        private string encodingScheme;
        private int mappingScheme;
        private int escChar;
        private string characterSet;
        private int characters;
        private bool isBaseFont;
        private float[] vVector;
        private bool isFixedV;
        private float capHeight;
        private float xHeight;
        private float ascender;
        private float descender;
        private readonly List<string> comments = new List<string>();

        private float underlinePosition;
        private float underlineThickness;
        private float italicAngle;
        private float[] charWidth;
        private bool isFixedPitch;
        private float standardHorizontalWidth;
        private float standardVerticalWidth;

        private List<CharMetric> charMetrics = new List<CharMetric>();
        private Dictionary<string, CharMetric> charMetricsMap = new Dictionary<string, CharMetric>(StringComparer.Ordinal);
        private List<TrackKern> trackKern = new List<TrackKern>();
        private List<Composite> composites = new List<Composite>();
        private List<KernPair> kernPairs = new List<KernPair>();
        private List<KernPair> kernPairs0 = new List<KernPair>();
        private List<KernPair> kernPairs1 = new List<KernPair>();

        /**
         * Constructor.
         */
        public FontMetrics()
        {
        }

        /**
         * This will get the width of a character.
         *
         * @param name The character to get the width for.
         *
         * @return The width of the character.
         */
        public float GetCharacterWidth(string name)
        {
            float result = 0;
            if (charMetricsMap.TryGetValue(name, out CharMetric metric))
            {
                result = metric.Wx;
            }
            return result;
        }

        /**
         * This will get the width of a character.
         *
         * @param name The character to get the width for.
         * @return The width of the character.
         */
        public float GetCharacterHeight(string name)
        {
            float result = 0;
            if (charMetricsMap.TryGetValue(name, out CharMetric metric))
            {
                result = metric.Wy;
                if (result.CompareTo(0) == 0)
                {
                    result = metric.BoundingBox.Height;
                }
            }
            return result;
        }


        /**
         * This will get the average width of a character.
         *
         * @return The width of the character.
         */
        public float GetAverageCharacterWidth()
        {
            float average = 0;
            float totalWidths = 0;
            float characterCount = 0;
            foreach (CharMetric metric in charMetrics)
            {
                if (metric.Wx > 0)
                {
                    totalWidths += metric.Wx;
                    characterCount += 1;
                }
            }
            if (totalWidths > 0)
            {
                average = totalWidths / characterCount;
            }
            return average;
        }

        /**
         * This will add a new comment.
         *
         * @param comment The comment to add to this metric.
         */
        public void AddComment(string comment)
        {
            comments.Add(comment);
        }

        /**
         * This will get all comments.
         *
         * @return The list of all comments.
         */
        public List<string> Comments
        {
            get => comments;
        }

        /**
         * This will get the version of the AFM document.
         *
         * @return The version of the document.
         */
        public float AFMVersion
        {
            get => afmVersion;
            set => afmVersion = value;
        }

        /**
         * This will get the metricSets attribute.
         *
         * @return The value of the metric sets.
         */
        public int MetricSets
        {
            get => metricSets;
            set
            {
                if (value < 0 || value > 2)
                {
                    throw new ArgumentException("The metricSets attribute must be in the "
                            + "set {0,1,2} and not '" + value + "'");
                }
                metricSets = value;
            }
        }

        /**
         * Getter for property fontName.
         *
         * @return Value of property fontName.
         */
        public string FontName
        {
            get => fontName;
            set => fontName = value;
        }

        /**
         * Getter for property fullName.
         *
         * @return Value of property fullName.
         */
        public string FullName
        {
            get => fullName;
            set => fullName = value;
        }

        /**
         * Getter for property familyName.
         *
         * @return Value of property familyName.
         */
        public string FamilyName
        {
            get => familyName;
            set => familyName = value;
        }

        /**
         * Getter for property weight.
         *
         * @return Value of property weight.
         */
        public string Weight
        {
            get => weight;
            set => weight = value;
        }

        /**
         * Getter for property fontBBox.
         *
         * @return Value of property fontBBox.
         */
        public SKRect FontBBox
        {
            get => fontBBox;
            set => fontBBox = value;
        }

        /**
         * Getter for property notice.
         *
         * @return Value of property notice.
         */
        public string Notice
        {
            get => notice;
            set => notice = value;
        }

        /**
         * Getter for property encodingScheme.
         *
         * @return Value of property encodingScheme.
         */
        public string EncodingScheme
        {
            get => encodingScheme;
            set => encodingScheme = value;
        }

        /**
         * Getter for property mappingScheme.
         *
         * @return Value of property mappingScheme.
         */
        public int MappingScheme
        {
            get => mappingScheme;
            set => mappingScheme = value;
        }

        /**
         * Getter for property escChar.
         *
         * @return Value of property escChar.
         */
        public int EscChar
        {
            get => escChar;
            set => escChar = value;
        }

        /**
         * Getter for property characterSet.
         *
         * @return Value of property characterSet.
         */
        public string CharacterSet
        {
            get => characterSet;
            set => characterSet = value;
        }

        /**
         * Getter for property characters.
         *
         * @return Value of property characters.
         */
        public int Characters
        {
            get => characters;
            set => characters = value;
        }

        /**
         * Getter for property isBaseFont.
         *
         * @return Value of property isBaseFont.
         */
        public bool IsBaseFont
        {
            get => isBaseFont;
            set => isBaseFont = value;
        }

        /**
         * Getter for property vVector.
         *
         * @return Value of property vVector.
         */
        public float[] VVector
        {
            get => vVector;
            set => vVector = value;
        }

        /**
         * Getter for property isFixedV.
         *
         * @return Value of property isFixedV.
         */
        public bool IsFixedV
        {
            get => isFixedV;
            set => isFixedV = value;
        }

        /**
         * Getter for property capHeight.
         *
         * @return Value of property capHeight.
         */
        public float CapHeight
        {
            get => capHeight;
            set => capHeight = value;
        }

        /**
         * Getter for property xHeight.
         *
         * @return Value of property xHeight.
         */
        public float XHeight
        {
            get => xHeight;
            set => xHeight = value;
        }

        /**
         * Getter for property ascender.
         *
         * @return Value of property ascender.
         */
        public float Ascender
        {
            get => ascender;
            set => ascender = value;
        }

        /**
         * Getter for property descender.
         *
         * @return Value of property descender.
         */
        public float Descender
        {
            get => descender;
            set => descender = value;
        }

        /**
         * Getter for property fontVersion.
         *
         * @return Value of property fontVersion.
         */
        public string FontVersion
        {
            get => fontVersion;
            set => fontVersion = value;
        }

        /**
         * Getter for property underlinePosition.
         *
         * @return Value of property underlinePosition.
         */
        public float UnderlinePosition
        {
            get => underlinePosition;
            set => underlinePosition = value;
        }

        /**
         * Getter for property underlineThickness.
         *
         * @return Value of property underlineThickness.
         */
        public float UnderlineThickness
        {
            get => underlineThickness;
            set => underlineThickness = value;
        }

        /**
         * Getter for property italicAngle.
         *
         * @return Value of property italicAngle.
         */
        public float ItalicAngle
        {
            get => italicAngle;
            set => italicAngle = value;
        }

        /**
         * Getter for property charWidth.
         *
         * @return Value of property charWidth.
         */
        public float[] CharWidth
        {
            get => charWidth;
            set => charWidth = value;
        }

        /**
         * Getter for property isFixedPitch.
         *
         * @return Value of property isFixedPitch.
         */
        public bool IsFixedPitch
        {
            get => isFixedPitch;
            set => isFixedPitch = value;
        }

        /** Getter for property charMetrics.
         * @return Value of property charMetrics.
         */
        public List<CharMetric> CharMetrics
        {
            get => charMetrics;
            set
            {
                charMetrics = value;
                charMetricsMap = new Dictionary<string, CharMetric>(charMetrics.Count, StringComparer.Ordinal);
                foreach (var metric in charMetrics)
                    charMetricsMap[metric.Name] = metric;
            }
        }

        /**
         * This will add another character metric.
         *
         * @param metric The character metric to add.
         */
        public void AddCharMetric(CharMetric metric)
        {
            charMetrics.Add(metric);
            charMetricsMap[metric.Name] = metric;
        }

        /** Getter for property trackKern.
         * @return Value of property trackKern.
         */
        public List<TrackKern> TrackKern
        {
            get => trackKern;
            set => trackKern = value;
        }

        /**
         * This will add another track kern.
         *
         * @param kern The track kerning data.
         */
        public void AddTrackKern(TrackKern kern)
        {
            trackKern.Add(kern);
        }

        /** Getter for property composites.
         * @return Value of property composites.
         */
        public List<Composite> Composites
        {
            get => composites;
            set => composites = value;
        }

        /**
         * This will add a single composite part to the picture.
         *
         * @param composite The composite info to add.
         */
        public void AddComposite(Composite composite)
        {
            composites.Add(composite);
        }

        public List<KernPair> KernPairs
        {
            get => kernPairs;
            set => kernPairs = value;
        }

        /**
         * This will add a kern pair.
         *
         * @param kernPair The kern pair to add.
         */
        public void AddKernPair(KernPair kernPair)
        {
            kernPairs.Add(kernPair);
        }

        /** Getter for property kernPairs0.
         * @return Value of property kernPairs0.
         */
        public List<KernPair> KernPairs0
        {
            get => kernPairs0;
            set => kernPairs0 = value;
        }

        /**
         * This will add a kern pair.
         *
         * @param kernPair The kern pair to add.
         */
        public void AddKernPair0(KernPair kernPair)
        {
            kernPairs0.Add(kernPair);
        }

        /** Getter for property kernPairs1.
         * @return Value of property kernPairs1.
         */
        public List<KernPair> KernPairs1
        {
            get => kernPairs1;
            set => kernPairs1 = value;
        }

        /**
         * This will add a kern pair.
         *
         * @param kernPair The kern pair to add.
         */
        public void AddKernPair1(KernPair kernPair)
        {
            kernPairs1.Add(kernPair);
        }

        /** Getter for property standardHorizontalWidth.
         * @return Value of property standardHorizontalWidth.
         */
        public float StandardHorizontalWidth
        {
            get => standardHorizontalWidth;
            set => standardHorizontalWidth = value;
        }

        /** Getter for property standardVerticalWidth.
         * @return Value of property standardVerticalWidth.
         */
        public float StandardVerticalWidth
        {
            get => standardVerticalWidth;
            set => standardVerticalWidth = value;
        }

    }
}