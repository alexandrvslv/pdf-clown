/*
  Copyright 2009-2015 Stefano Chizzolini. http://www.pdfclown.org

  Contributors:
    * Stefano Chizzolini (original code developer, http://www.stefanochizzolini.it)

  This file should be part of the source code distribution of "PDF Clown library" (the
  Program): see the accompanying README files for more info.

  This Program is free software; you can redistribute it and/or modify it under the terms
  of the GNU Lesser General Public License as published by the Free Software Foundation;
  either version 3 of the License, or (at your option) any later version.

  This Program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY,
  either expressed or implied; without even the implied warranty of MERCHANTABILITY or
  FITNESS FOR A PARTICULAR PURPOSE. See the License for more details.

  You should have received a copy of the GNU Lesser General Public License along with this
  Program (see README files); if not, go to the GNU website (http://www.gnu.org/licenses/).

  Redistribution and use, with or without modification, are permitted provided that such
  redistributions retain the above copyright notice, license and disclaimer, along with
  this list of conditions.
*/

using PdfClown.Documents.Contents;
using PdfClown.Documents.Contents.Objects;
using PdfClown.Documents.Contents.Scanner;
using PdfClown.Util.Math;
using PdfClown.Util.Math.Geom;
using SkiaSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace PdfClown.Tools
{
    /// <summary>Tool for extracting text from <see cref="IContentContext">content contexts</see>.</summary>
    public sealed class TextExtractor
    {
        /// <summary>Text-to-area matching mode.</summary>
        public enum AreaModeEnum
        {
            /// <summary>Text string must be contained by the area.</summary>
            Containment,
            /// <summary>Text string must intersect the area.</summary>
            Intersection
        }

        /// <summary>Text filter by interval.</summary>
        /// <remarks>Iterated intervals MUST be ordered.</remarks>
        public interface IIntervalFilter : IEnumerator<Interval<int>>
        {
            /// <summary>Notifies current matching.</summary>
            /// <param name="interval">Current interval.</param>
            /// <param name="match">Text string matching the current interval.</param>
            void Process(Interval<int> interval, ITextString match);
        }

        private class IntervalFilter : IIntervalFilter
        {
            private IList<Interval<int>> intervals;

            private IList<ITextString> textStrings = new List<ITextString>();
            private int index = 0;

            public IntervalFilter(IList<Interval<int>> intervals)
            {
                this.intervals = intervals;
            }

            public Interval<int> Current => intervals[index];

            object IEnumerator.Current => this.Current;

            public void Dispose()
            {/* NOOP */}

            public bool MoveNext()
            { return (++index < intervals.Count); }

            public void Process(Interval<int> interval, ITextString match)
            { textStrings.Add(match); }

            public void Reset()
            { throw new NotSupportedException(); }

            public IList<ITextString> TextStrings => textStrings;
        }


        public static readonly SKRect DefaultArea = SKRect.Create(0, 0, 0, 0);

        /// <summary>Converts text information into plain text.</summary>
        /// <param name="textStrings">Text information to convert.</param>
        /// <returns>Plain text.</returns>
        public static string ToString(IDictionary<SKRect?, IList<ITextString>> textStrings)
        { return ToString(textStrings, "", ""); }

        /// <summary>Converts text information into plain text.</summary>
        /// <param name="textStrings">Text information to convert.</param>
        /// <param name="lineSeparator">Separator to apply on line break.</param>
        /// <param name="areaSeparator">Separator to apply on area break.</param>
        /// <returns>Plain text.</returns>
        public static string ToString(IDictionary<SKRect?, IList<ITextString>> textStrings, string lineSeparator, string areaSeparator)
        {
            StringBuilder textBuilder = new StringBuilder();
            foreach (IList<ITextString> areaTextStrings in textStrings.Values)
            {
                if (textBuilder.Length > 0)
                { textBuilder.Append(areaSeparator); }

                foreach (ITextString textString in areaTextStrings)
                { textBuilder.Append(textString.Text).Append(lineSeparator); }
            }
            return textBuilder.ToString();
        }

        private AreaModeEnum areaMode = AreaModeEnum.Containment;
        private List<SKRect> areas;
        private float areaTolerance = 0;
        private bool dehyphenated;
        private bool sorted;

        public TextExtractor() : this(true, false)
        { }

        public TextExtractor(bool sorted, bool dehyphenated) : this(null, sorted, dehyphenated)
        { }

        public TextExtractor(IList<SKRect> areas, bool sorted, bool dehyphenated)
        {
            Areas = areas;
            Dehyphenated = dehyphenated;
            Sorted = sorted;
        }

        /// <summary>Gets the text-to-area matching mode.</summary>
        public AreaModeEnum AreaMode
        {
            get => areaMode;
            set => areaMode = value;
        }

        /// <summary>Gets the graphic areas whose text has to be extracted.</summary>
        public IList<SKRect> Areas
        {
            get => areas;
            set => areas = (value == null ? new List<SKRect>() : new List<SKRect>(value));
        }

        /// <summary>Gets the admitted outer area (in points) for containment matching purposes.</summary>
        /// <remarks>This measure is useful to ensure that text whose boxes overlap with the area bounds
        /// is not excluded from the match.</remarks>
        public float AreaTolerance
        {
            get => areaTolerance;
            set => areaTolerance = value;
        }

        /// <summary>Gets/Sets whether the text strings have to be dehyphenated.</summary>
        public bool Dehyphenated
        {
            get => dehyphenated;
            set
            {
                dehyphenated = value;
                if (dehyphenated)
                { Sorted = true; }
            }
        }

        /// <summary>Extracts text strings from the specified content context.</summary>
        /// <param name="contentContext">Source content context.</param>
        public IDictionary<SKRect?, IList<ITextString>> Extract(IContentContext contentContext)
        {
            IDictionary<SKRect?, IList<ITextString>> extractedTextStrings;
            {
                List<ITextString> textStrings = new List<ITextString>();
                {
                    // 1. Extract the source text strings!
                    List<TextStringWrapper> rawTextStrings = new List<TextStringWrapper>();
                    Extract(new ContentScanner(contentContext), rawTextStrings);

                    // 2. Sort the target text strings!
                    if (sorted)
                    { Sort(rawTextStrings, textStrings); }
                    else
                    {
                        foreach (TextStringWrapper rawTextString in rawTextStrings)
                        { textStrings.Add(rawTextString); }
                    }
                }

                // 3. Filter the target text strings!
                if (areas.Count == 0)
                {
                    extractedTextStrings = new Dictionary<SKRect?, IList<ITextString>>();
                    extractedTextStrings[DefaultArea] = textStrings;
                }
                else
                { extractedTextStrings = Filter(textStrings, areas.ToArray()); }
            }
            return extractedTextStrings;
        }

        /// <summary>Extracts text strings from the specified contents.</summary>
        /// <param name="contents">Source contents.</param>
        public IDictionary<SKRect?, IList<ITextString>> Extract(ContentWrapper contents)
        { return Extract(contents.ContentContext); }

        /// <summary>Gets the text strings matching the specified intervals.</summary>
        /// <param name="textStrings">Text strings to filter.</param>
        /// <param name="intervals">Text intervals to match. They MUST be ordered and not overlapping.</param>
        /// <returns>A list of text strings corresponding to the specified intervals.</returns>
        public IList<ITextString> Filter(IDictionary<SKRect?, IList<ITextString>> textStrings, IList<Interval<int>> intervals)
        {
            IntervalFilter filter = new IntervalFilter(intervals);
            Filter(textStrings, filter);
            return filter.TextStrings;
        }

        /// <summary>Processes the text strings matching the specified filter.</summary>
        /// <param name="textStrings">Text strings to filter.</param>
        /// <param name="filter">Matching processor.</param>
        public void Filter(IDictionary<SKRect?, IList<ITextString>> textStrings, IIntervalFilter filter)
        {
            IEnumerator<IList<ITextString>> textStringsIterator = textStrings.Values.GetEnumerator();
            if (!textStringsIterator.MoveNext())
                return;

            IEnumerator<ITextString> areaTextStringsIterator = textStringsIterator.Current.GetEnumerator();
            if (!areaTextStringsIterator.MoveNext())
                return;

            IList<TextChar> textChars = areaTextStringsIterator.Current.TextChars;
            int baseTextCharIndex = 0;
            int textCharIndex = 0;
            while (filter.MoveNext())
            {
                Interval<int> interval = filter.Current;
                TextString match = new TextString();
                {
                    int matchStartIndex = interval.Low;
                    int matchEndIndex = interval.High;
                    while (matchStartIndex > baseTextCharIndex + textChars.Count)
                    {
                        baseTextCharIndex += textChars.Count;
                        if (!areaTextStringsIterator.MoveNext())
                        { areaTextStringsIterator = textStringsIterator.Current.GetEnumerator(); areaTextStringsIterator.MoveNext(); }
                        textChars = areaTextStringsIterator.Current.TextChars;
                    }
                    textCharIndex = matchStartIndex - baseTextCharIndex;

                    while (baseTextCharIndex + textCharIndex < matchEndIndex)
                    {
                        if (textCharIndex == textChars.Count)
                        {
                            baseTextCharIndex += textChars.Count;
                            if (!areaTextStringsIterator.MoveNext())
                            { areaTextStringsIterator = textStringsIterator.Current.GetEnumerator(); areaTextStringsIterator.MoveNext(); }
                            textChars = areaTextStringsIterator.Current.TextChars;
                            textCharIndex = 0;
                        }
                        match.TextChars.Add(textChars[textCharIndex++]);
                    }
                }
                filter.Process(interval, match);
            }
        }

        /// <summary>Gets the text strings matching the specified area.</summary>
        /// <param name="textStrings">Text strings to filter, grouped by source area.</param>
        /// <param name="area">Graphic area which text strings have to be matched to.</param>
        public IList<ITextString> Filter(IDictionary<SKRect?, IList<ITextString>> textStrings, SKRect area)
        { return Filter(textStrings, new SKRect[] { area })[area]; }

        /// <summary>Gets the text strings matching the specified areas.</summary>
        /// <param name="textStrings">Text strings to filter, grouped by source area.</param>
        /// <param name="areas">Graphic areas which text strings have to be matched to.</param>
        public IDictionary<SKRect?, IList<ITextString>> Filter(IDictionary<SKRect?, IList<ITextString>> textStrings, params SKRect[] areas)
        {
            IDictionary<SKRect?, IList<ITextString>> filteredTextStrings = null;
            foreach (IList<ITextString> areaTextStrings in textStrings.Values)
            {
                IDictionary<SKRect?, IList<ITextString>> filteredAreasTextStrings = Filter(areaTextStrings, areas);
                if (filteredTextStrings == null)
                { filteredTextStrings = filteredAreasTextStrings; }
                else
                {
                    foreach (KeyValuePair<SKRect?, IList<ITextString>> filteredAreaTextStringsEntry in filteredAreasTextStrings)
                    {
                        IList<ITextString> filteredTextStringsList = filteredTextStrings[filteredAreaTextStringsEntry.Key];
                        foreach (ITextString filteredAreaTextString in filteredAreaTextStringsEntry.Value)
                        { filteredTextStringsList.Add(filteredAreaTextString); }
                    }
                }
            }
            return filteredTextStrings;
        }

        /// <summary>Gets the text strings matching the specified area.</summary>
        /// <param name="textStrings">Text strings to filter.</param>
        /// <param name="area">Graphic area which text strings have to be matched to.</param>
        public IList<ITextString> Filter(IList<ITextString> textStrings, SKRect area)
        { return Filter(textStrings, new SKRect[] { area })[area]; }

        /// <summary>Gets the text strings matching the specified areas.</summary>
        /// <param name="textStrings">Text strings to filter.</param>
        /// <param name="areas">Graphic areas which text strings have to be matched to.</param>
        public IDictionary<SKRect?, IList<ITextString>> Filter(IList<ITextString> textStrings, params SKRect[] areas)
        {
            IDictionary<SKRect?, IList<ITextString>> filteredAreasTextStrings = new Dictionary<SKRect?, IList<ITextString>>();
            foreach (SKRect area in areas)
            {
                IList<ITextString> filteredAreaTextStrings = new List<ITextString>();
                filteredAreasTextStrings[area] = filteredAreaTextStrings;
                var toleratedArea = (areaTolerance != 0
                  ? new Quad(SKRect.Create(
                    area.Left - areaTolerance,
                    area.Top - areaTolerance,
                    area.Width + areaTolerance * 2,
                    area.Height + areaTolerance * 2))
                  : new Quad(area));
                foreach (ITextString textString in textStrings)
                {
                    var textStringQuad = textString.Quad;
                    if (toleratedArea.IntersectsWith(textStringQuad))
                    {
                        TextString filteredTextString = new TextString();
                        List<TextChar> filteredTextStringChars = filteredTextString.TextChars;
                        foreach (TextChar textChar in textString.TextChars)
                        {
                            var textCharQuad = textChar.Quad;
                            if ((areaMode == AreaModeEnum.Containment && toleratedArea.Contains(textCharQuad))
                              || (areaMode == AreaModeEnum.Intersection && toleratedArea.IntersectsWith(textCharQuad)))
                            { filteredTextStringChars.Add(textChar); }
                        }
                        if (filteredTextStringChars.Count > 0)
                        { filteredAreaTextStrings.Add(filteredTextString); }
                    }
                }
            }
            return filteredAreasTextStrings;
        }

        /// <summary>Gets/Sets whether the text strings have to be sorted.</summary>
        public bool Sorted
        {
            get => sorted;
            set
            {
                sorted = value;
                if (!sorted)
                { Dehyphenated = false; }
            }
        }

        /// <summary>Scans a content level looking for text.</summary>
        private void Extract(ContentScanner level, IList<TextStringWrapper> extractedTextStrings)
        {
            if (level == null)
                return;

            while (level.MoveNext())
            {
                ContentObject content = level.Current;
                if (content is GraphicsText)
                {
                    // Collect the text strings!
                    foreach (TextStringWrapper textString in ((TextWrapper)level.CurrentWrapper).TextStrings)
                    {
                        if (textString.TextChars.Count > 0)
                        { extractedTextStrings.Add(textString); }
                    }
                }
                else if (content is GraphicsXObject)
                {
                    // Scan the external level!
                    Extract(
                      ((GraphicsXObject)content).GetScanner(level),
                      extractedTextStrings);
                }
                else if (content is ContainerObject)
                {
                    // Scan the inner level!
                    Extract(
                      level.ChildLevel,
                      extractedTextStrings);
                }
            }
        }

        /// <summary>Sorts the extracted text strings.</summary>
        /// <remarks>Sorting implies text position ordering, integration and aggregation.</remarks>
        /// <param name="rawTextStrings">Source (lower-level) text strings.</param>
        /// <param name="textStrings">Target (higher-level) text strings.</param>
        private void Sort(List<TextStringWrapper> rawTextStrings, List<ITextString> textStrings)
        {
            // Sorting the source text strings...
            {
                TextStringPositionComparer<TextStringWrapper> positionComparator = new TextStringPositionComparer<TextStringWrapper>();
                rawTextStrings.Sort(positionComparator);
            }

            // Aggregating and integrating the source text strings into the target ones...
            TextString textString = null;
            TextStyle textStyle = null;
            TextChar previousTextChar = null;
            bool dehyphenating = false;
            foreach (TextStringWrapper rawTextString in rawTextStrings)
            {
                /*
                  NOTE: Contents on the same line are grouped together within the same text string.
                */
                // Add a new text string in case of new line!
                if (textString != null
                  && textString.TextChars.Count > 0
                  && !TextStringPositionComparer<ITextString>.IsOnTheSameLine(textString.Quad, rawTextString.Quad))
                {
                    if (dehyphenated
                      && previousTextChar.Value == '-') // Hyphened word.
                    {
                        textString.TextChars.Remove(previousTextChar);
                        dehyphenating = true;
                    }
                    else // Full word.
                    {
                        // Add synthesized space character!
                        textString.TextChars.Add(
                          new TextChar(
                            ' ',
                            new Quad(SKRect.Create(
                              previousTextChar.Quad.Right,
                              previousTextChar.Quad.Top,
                              0,
                              previousTextChar.Quad.Height)),
                            textString,
                            true));
                        textString = null;
                        dehyphenating = false;
                    }
                    previousTextChar = null;
                }
                if (textString == null)
                { textStrings.Add(textString = new TextString { Context = rawTextString.Context, Style = rawTextString.Style }); }

                textStyle = rawTextString.Style;
                double spaceWidth = textStyle.GetWidth(' ') * .5;
                foreach (TextChar textChar in rawTextString.TextChars)
                {
                    if (previousTextChar != null)
                    {
                        /*
                          NOTE: PDF files may have text contents omitting space characters,
                          so they must be inferred and synthesized, marking them as virtual
                          in order to allow the user to distinguish between original contents
                          and augmented ones.
                        */
                        if (!textChar.Contains(' ')
                          && !previousTextChar.Contains(' '))
                        {
                            float charSpace = textChar.Quad.Left - previousTextChar.Quad.Right;
                            if (charSpace > spaceWidth)
                            {
                                // Add synthesized space character!
                                textString.TextChars.Add(
                                  previousTextChar = new TextChar(
                                    ' ',
                                    new Quad(SKRect.Create(
                                      previousTextChar.Quad.Right,
                                      textChar.Quad.Top,
                                      charSpace,
                                      textChar.Quad.Height
                                      )),
                                    textString,
                                    true));
                            }
                        }
                        else if (dehyphenating && previousTextChar.Contains(' '))
                        {
                            textStrings.Add(textString = new TextString
                            {
                                Context = rawTextString.Context,
                                Style = rawTextString.Style
                            });
                            dehyphenating = false;
                        }
                    }
                    textString.TextChars.Add(previousTextChar = textChar);
                }
            }
        }
    }
}
