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
		private float defaultWidth;
		private float averageWidth;

		private readonly Dictionary<int, float> verticalDisplacementY = new Dictionary<int, float>(); // w1y
		private readonly Dictionary<int, SKPoint> positionVectors = new Dictionary<int, SKPoint>();     // v
		private float[] dw2 = new float[] { 880, -1000 };

		private FontDescriptor fontDescriptor;

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

		private void ReadWidths()
		{
			widths = new Dictionary<int, float>();
			PdfObject wBase = Dictionary.Resolve(PdfName.W);
			if (wBase is PdfArray)
			{
				PdfArray wArray = (PdfArray)wBase;
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
			PdfObject dw2Base = Dictionary.Resolve(PdfName.DW2);
			if (dw2Base is PdfArray dw2Array)
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
			PdfObject w2Base = Dictionary.Resolve(PdfName.W2);
			if (w2Base is PdfArray)
			{
				PdfArray w2Array = (PdfArray)w2Base;
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
			get => BaseFont.StringValue;
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

		/**
		 * This will get the default width. The default value for the default width is 1000.
		 *
		 * @return The default width for the glyphs in this font.
		 */
		private float getDefaultWidth()
		{
			if (float.compare(defaultWidth, 0) == 0)
			{
				PdfObject baseObj = Dictionary.Resolve(PdfName.DW);
				if (baseObj is IPdfNumber)
				{
					defaultWidth = ((IPdfNumber)base).floatValue();
				}
				else
				{
					defaultWidth = 1000;
				}
			}
			return defaultWidth;
		}

		/**
		 * Returns the default position vector (v).
		 *
		 * @param cid CID
		 */
		private SKPoint getDefaultPositionVector(int cid)
		{
			return new SKPoint(getWidthForCID(cid) / 2, dw2[0]);
		}

		private float getWidthForCID(int cid)
		{
			float width = widths.get(cid);
			if (width == null)
			{
				width = getDefaultWidth();
			}
			return width;
		}

		override public bool hasExplicitWidth(int code)
		{
			return widths.get(codeToCID(code)) != null;
		}

		override public SKPoint getPositionVector(int code)
		{
			int cid = codeToCID(code);
			SKPoint v = positionVectors.get(cid);
			if (v == null)
			{
				v = getDefaultPositionVector(cid);
			}
			return v;
		}

		/**
		 * Returns the y-component of the vertical displacement vector (w1).
		 *
		 * @param code character code
		 * @return w1y
		 */
		public float getVerticalDisplacementVectorY(int code)
		{
			int cid = codeToCID(code);
			float w1y = verticalDisplacementY.get(cid);
			if (w1y == null)
			{
				w1y = dw2[1];
			}
			return w1y;
		}

		override public float getWidth(int code)
		{
			// these widths are supposed to be consistent with the actual widths given in the CIDFont
			// program, but PDFBOX-563 shows that when they are not, Acrobat overrides the embedded
			// font widths with the widths given in the font dictionary
			return getWidthForCID(codeToCID(code));
		}

		override
		// todo: this method is highly suspicious, the average glyph width is not usually a good metric
		public float getAverageFontWidth()
		{
			if (float.compare(averageWidth, 0) == 0)
			{
				float totalWidths = 0.0f;
				int characterCount = 0;
				if (widths != null)
				{
					foreach (float width in widths.values())
					{
						if (width > 0)
						{
							totalWidths += width;
							++characterCount;
						}
					}
				}
				averageWidth = totalWidths / characterCount;
				if (averageWidth <= 0 || float.isNaN(averageWidth))
				{
					averageWidth = getDefaultWidth();
				}
			}
			return averageWidth;
		}

		/**
		 * Returns the CIDSystemInfo, or null if it is missing (which isn't allowed but could happen).
		 */
		public PDCIDSystemInfo getCIDSystemInfo()
		{
			PdfObject baseObj = Dictionary.Resolve(PdfName.CIDSYSTEMINFO);
			if (baseObj is PdfDictionary)
			{
				return new PDCIDSystemInfo((PdfDictionary)baseObj);
			}
			return null;
		}

		/**
		 * Returns the CID for the given character code. If not found then CID 0 is returned.
		 *
		 * @param code character code
		 * @return CID
		 */
		public abstract int codeToCID(int code);

		/**
		 * Returns the GID for the given character code.
		 *
		 * @param code character code
		 * @return GID
		 * @throws java.io.IOException
		 */
		public abstract int codeToGID(int code);

		public abstract byte[] encodeGlyphId(int glyphId);

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
		protected abstract byte[] encode(int unicode);

		readonly int[] readCIDToGIDMap()
		{
			int[] cid2gid = null;
			PdfObject map = Dictionary.Resolve(PdfName.CID_TO_GID_MAP);
			if (map is PdfStream stream)
			{
				InputStream input = stream.createInputStream();
				byte[] mapAsBytes = IOUtils.toByteArray(input);
				IOUtils.closeQuietly(input);
				int numberOfInts = mapAsBytes.length / 2;
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