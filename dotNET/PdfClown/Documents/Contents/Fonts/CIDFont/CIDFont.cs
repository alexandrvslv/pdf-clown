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
using PdfClown.Objects;
using SkiaSharp;
using System;
using System.Collections.Generic;

namespace PdfClown.Documents.Contents.Fonts
{

    /**
     * A CIDFont. A CIDFont is a PDF object that contains information about a CIDFont program. Although
     * its Type value is Font, a CIDFont is not actually a font.
     *
     * <p>It is not usually necessary to use this class directly, prefer {@link PDType0Font}.
     *
     * @author Ben Litchfield
     */
    public abstract class CIDFont : PdfObjectWrapper<PdfDictionary>// PDFontLike, PDVectorFont
    {
        protected readonly Type0Font parent;

        private Dictionary<int, float> widths;
        private int? defaultWidth;
        private float averageWidth;

        private readonly Dictionary<int, float> verticalDisplacementY = new Dictionary<int, float>(); // w1y
        private readonly Dictionary<int, SKPoint> positionVectors = new Dictionary<int, SKPoint>();     // v
        private float[] dw2 = new float[] { 880, -1000 };

        private FontDescriptor fontDescriptor;

        public CIDFont(Document document, PdfDictionary fontObject) : base(document, fontObject)
        {
        }

        internal CIDFont(PdfDirectObject fontObject)
            : base(fontObject)
        {
            ReadWidths();
            ReadVerticalDisplacements();
        }

        /**
         * Constructor.
         *
         * @param fontDictionary The font dictionary according to the PDF specification.
         */
        public CIDFont(PdfDictionary fontDictionary, Type0Font parent)
            : base(fontDictionary)
        {
            this.parent = parent;
            ReadWidths();
            ReadVerticalDisplacements();
        }

        public string Type
        {
            get => ((PdfName)Dictionary[PdfName.Type]).StringValue;
            set => Dictionary[PdfName.Type] = new PdfName(value);
        }

        public string Subtype
        {
            get => ((PdfName)Dictionary[PdfName.Subtype]).StringValue;
            set => Dictionary[PdfName.Subtype] = new PdfName(value);
        }

        /**
         * The PostScript name of the font.
         *
         * @return The postscript name of the font.
         */
        public string BaseFont
        {
            get => ((PdfName)Dictionary[PdfName.BaseFont]).StringValue;
            set => Dictionary[PdfName.BaseFont] = new PdfName(value);
        }

        public CIDSystemInfo CIDSystemInfo
        {
            get => Wrap<CIDSystemInfo>((PdfDirectObject)Dictionary.Resolve(PdfName.CIDSystemInfo));
            set => Dictionary[PdfName.BaseFont] = value?.BaseObject;
        }

        //override 
        public string Name
        {
            get => BaseFont;
        }

        //override
        public FontDescriptor FontDescriptor
        {
            get
            {
                if (fontDescriptor == null)
                {
                    fontDescriptor = Wrap<FontDescriptor>((PdfDirectObject)Dictionary.Resolve(PdfName.FontDescriptor));
                }
                return fontDescriptor;
            }
            set => Dictionary[PdfName.FontDescriptor] = value?.BaseObject;
        }

        public int DefaultWidth
        {
            get => defaultWidth ?? (defaultWidth = ((PdfInteger)Dictionary[PdfName.DW])?.IntValue ?? 1000).Value;
            set => Dictionary[PdfName.DW] = new PdfInteger(value);
        }

        public PdfArray Widths
        {
            get => (PdfArray)Dictionary.Resolve(PdfName.W);
            set => Dictionary[PdfName.W] = value.Reference;
        }

        public PdfArray VerticalDefaultWidth
        {
            get => (PdfArray)Dictionary.Resolve(PdfName.DW2);
            set => Dictionary[PdfName.DW2] = value.Reference;
        }

        public PdfArray VerticaltWidths
        {
            get => (PdfArray)Dictionary.Resolve(PdfName.W2);
            set => Dictionary[PdfName.W2] = value.Reference;
        }

        public PdfStream CIDToGIDMap
        {
            get => (PdfStream)Dictionary.Resolve(PdfName.CIDToGIDMap);
            set => Dictionary[PdfName.CIDToGIDMap] = value.Reference;
        }

        /**
         * Returns the Type 0 font which is the parent of this font.
         *
         * @return parent Type 0 font
         */
        public Type0Font Parent
        {
            get => parent;
        }

        private void ReadWidths()
        {
            widths = new Dictionary<int, float>();
            var wArray = Widths;
            if (wArray != null)
            {
                int size = wArray.Count;
                int counter = 0;
                while (counter < size)
                {
                    IPdfNumber firstCode = (IPdfNumber)wArray.Resolve(counter++);
                    PdfObject next = wArray.Resolve(counter++);
                    if (next is PdfArray array)
                    {
                        int startRange = firstCode.IntValue;
                        int arraySize = array.Count;
                        for (int i = 0; i < arraySize; i++)
                        {
                            IPdfNumber width = (IPdfNumber)array.Resolve(i);
                            widths[startRange + i] = width.FloatValue;
                        }
                    }

                    else
                    {
                        IPdfNumber secondCode = (IPdfNumber)next;
                        IPdfNumber rangeWidth = (IPdfNumber)wArray.Resolve(counter++);
                        int startRange = firstCode.IntValue;
                        int endRange = secondCode.IntValue;
                        float width = rangeWidth.FloatValue;
                        for (int i = startRange; i <= endRange; i++)
                        {
                            widths[i] = width;
                        }
                    }
                }
            }
        }

        private void ReadVerticalDisplacements()
        {
            // default position vector and vertical displacement vector
            var dw2Array = VerticalDefaultWidth;
            if (dw2Array != null)
            {
                PdfObject base0 = dw2Array.Resolve(0);
                PdfObject base1 = dw2Array.Resolve(1);
                if (base0 is IPdfNumber number0 && base1 is IPdfNumber number1)
                {
                    dw2[0] = number0.FloatValue;
                    dw2[1] = number1.FloatValue;
                }
            }

            // vertical metrics for individual CIDs.
            var w2Array = VerticaltWidths;
            if (w2Array != null)
            {
                for (int i = 0; i < w2Array.Count; i++)
                {
                    IPdfNumber c = (IPdfNumber)w2Array.Resolve(i);
                    PdfObject next = w2Array.Resolve(++i);
                    if (next is PdfArray array)
                    {
                        for (int j = 0; j < array.Count; j++)
                        {
                            int cid = c.IntValue + j / 3;
                            IPdfNumber w1y = (IPdfNumber)array.Resolve(j);
                            IPdfNumber v1x = (IPdfNumber)array.Resolve(++j);
                            IPdfNumber v1y = (IPdfNumber)array.Resolve(++j);
                            verticalDisplacementY[cid] = w1y.FloatValue;
                            positionVectors[cid] = new SKPoint(v1x.FloatValue, v1y.FloatValue);
                        }
                    }
                    else
                    {
                        int first = c.IntValue;
                        int last = ((IPdfNumber)next).IntValue;
                        IPdfNumber w1y = (IPdfNumber)w2Array.Resolve(++i);
                        IPdfNumber v1x = (IPdfNumber)w2Array.Resolve(++i);
                        IPdfNumber v1y = (IPdfNumber)w2Array.Resolve(++i);
                        for (int cid = first; cid <= last; cid++)
                        {
                            verticalDisplacementY[cid] = w1y.FloatValue;
                            positionVectors[cid] = new SKPoint(v1x.FloatValue, v1y.FloatValue);
                        }
                    }
                }
            }
        }

        /**
         * Returns the default position vector (v).
         *
         * @param cid CID
         */
        private SKPoint GetDefaultPositionVector(int cid)
        {
            return new SKPoint(GetWidthForCID(cid) / 2, dw2[0]);
        }

        private float GetWidthForCID(int cid)
        {
            if (widths.TryGetValue(cid, out var width))
                return width;
            return DefaultWidth;
        }

        //override 
        public bool HasExplicitWidth(int code)
        {
            var cid = CodeToCID(code);
            return widths.TryGetValue(cid, out _);
        }

        //override 
        public SKPoint GetPositionVector(int code)
        {
            int cid = CodeToCID(code);
            if (positionVectors.TryGetValue(cid, out var position))
                return position;
            return GetDefaultPositionVector(cid);
        }

        /**
         * Returns the y-component of the vertical displacement vector (w1).
         *
         * @param code character code
         * @return w1y
         */
        public float GetVerticalDisplacementVectorY(int code)
        {
            int cid = CodeToCID(code);
            if (verticalDisplacementY.TryGetValue(cid, out var w1y))
            {
                return w1y;
            }
            return dw2[1];
        }

        //override 
        public float GetWidth(int code)
        {
            // these widths are supposed to be consistent with the actual widths given in the CIDFont
            // program, but PDFBOX-563 shows that when they are not, Acrobat overrides the embedded
            // font widths with the widths given in the font dictionary
            return GetWidthForCID(CodeToCID(code));
        }

        //override
        // todo: this method is highly suspicious, the average glyph width is not usually a good metric
        public float GetAverageFontWidth()
        {
            if (averageWidth == 0)
            {
                float totalWidths = 0.0f;
                int characterCount = 0;
                if (widths != null)
                {
                    foreach (float width in widths.Values)
                    {
                        if (width > 0)
                        {
                            totalWidths += width;
                            ++characterCount;
                        }
                    }
                }
                averageWidth = totalWidths / characterCount;
                if (averageWidth <= 0 || averageWidth == float.NaN)
                {
                    averageWidth = DefaultWidth;
                }
            }
            return averageWidth;
        }

        /**
         * Returns the CID for the given character code. If not found then CID 0 is returned.
         *
         * @param code character code
         * @return CID
         */
        public abstract int CodeToCID(int code);

        /**
         * Returns the GID for the given character code.
         *
         * @param code character code
         * @return GID
         * @throws java.io.IOException
         */
        public abstract int CodeToGID(int code);

        public abstract byte[] EncodeGlyphId(int glyphId);

        /**
         * Encodes the given Unicode code point for use in a PDF content stream.
         * Content streams use a multi-byte encoding with 1 to 4 bytes.
         *
         * <p>This method is called when embedding text in PDFs and when filling in fields.
         *
         * @param unicode Unicode code point.
         * @return Array of 1 to 4 PDF content stream bytes.
         * @throws IOException If the text could not be encoded.
         */
        protected abstract byte[] Encode(int unicode);

        public int[] ReadCIDToGIDMap()
        {
            int[] cid2gid = null;
            var stream = CIDToGIDMap;
            if (stream != null)
            {
                var input = stream.GetBody(false);
                input = Bytes.Buffer.Extract(input, stream.Filter, stream.Parameters);
                byte[] mapAsBytes = input.ToByteArray();

                int numberOfInts = mapAsBytes.Length / 2;
                cid2gid = new int[numberOfInts];
                int offset = 0;
                for (int index = 0; index < numberOfInts; index++)
                {
                    int gid = (mapAsBytes[offset] & 0xff) << 8 | mapAsBytes[offset + 1] & 0xff;
                    cid2gid[index] = gid;
                    offset += 2;
                }
            }
            return cid2gid;
        }
    }
}