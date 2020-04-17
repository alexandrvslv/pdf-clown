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
using PdfClown.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace PdfClown.Documents.Contents.Fonts.CCF
{
    /**
     * This class represents a parser for a CFF font. 
     * //@author Villu Ruusmann
     */
    public class CFFParser
    {

        private const string TAG_OTTO = "OTTO";
        private const string TAG_TTCF = "ttcf";
        private const string TAG_TTFONLY = "\u0000\u0001\u0000\u0000";

        private string[] stringIndex = null;
        private IByteSource source;

        // for debugging only
        private string debugFontName;

        /**
		 * Source from which bytes may be read in the future.
		 */
        public interface IByteSource
        {
            /**
             * Returns the source bytes. May be called more than once.
             */
            byte[] Bytes { get; }
        }

        /**
		 * Parse CFF font using byte array, also passing in a byte source for future use.
		 * 
		 * //@param bytes source bytes
		 * //@param source source to re-read bytes from in the future
		 * //@return the parsed CFF fonts
		 * //@throws IOException If there is an error reading from the stream
		 */
        public List<CFFFont> Parse(byte[] bytes, IByteSource source)
        {
            this.source = source;
            return Parse(bytes);
        }

        /**
		 * Parse CFF font using a byte array as input.
		 * 
		 * //@param bytes the given byte array
		 * //@return the parsed CFF fonts
		 * //@throws IOException If there is an error reading from the stream
		 */
        public List<CFFFont> Parse(byte[] bytes)
        {
            CFFDataInput input = new CFFDataInput(bytes);

            string firstTag = ReadTagName(input);
            // try to determine which kind of font we have
            switch (firstTag)
            {
                case TAG_OTTO:
                    input = CreateTaggedCFFDataInput(input, bytes);
                    break;
                case TAG_TTCF:
                    throw new IOException("True Type Collection fonts are not supported.");
                case TAG_TTFONLY:
                    throw new IOException("OpenType fonts containing a true type font are not supported.");
                default:
                    input.Position = 0;
                    break;
            }

            //@SuppressWarnings("unused")
            Header header = ReadHeader(input);
            string[] nameIndex = ReadStringIndexData(input);
            if (nameIndex == null)
            {
                throw new IOException("Name index missing in CFF font");
            }
            byte[][] topDictIndex = ReadIndexData(input);
            if (topDictIndex == null)
            {
                throw new IOException("Top DICT INDEX missing in CFF font");
            }

            stringIndex = ReadStringIndexData(input);
            byte[][] globalSubrIndex = ReadIndexData(input);

            List<CFFFont> fonts = new List<CFFFont>();
            for (int i = 0; i < nameIndex.Length; i++)
            {
                CFFFont font = ParseFont(input, nameIndex[i], topDictIndex[i]);
                font.GlobalSubrIndex = globalSubrIndex;
                font.SetData(source);
                fonts.Add(font);
            }
            return fonts;
        }

        private CFFDataInput CreateTaggedCFFDataInput(CFFDataInput input, byte[] bytes)
        {
            // this is OpenType font containing CFF data
            // so find CFF tag
            short numTables = input.ReadShort();
            //@SuppressWarnings({"unused", "squid:S1854"})
            short searchRange = input.ReadShort();
            //@SuppressWarnings({"unused", "squid:S1854"})
            short entrySelector = input.ReadShort();
            //@SuppressWarnings({"unused", "squid:S1854"})
            short rangeShift = input.ReadShort();
            for (int q = 0; q < numTables; q++)
            {
                string tagName = ReadTagName(input);
                //@SuppressWarnings("unused")
                long checksum = ReadLong(input);
                long offset = ReadLong(input);
                long Length = ReadLong(input);
                if ("CFF ".Equals(tagName, StringComparison.Ordinal))
                {
                    byte[] bytes2 = bytes.CopyOfRange((int)offset, (int)(offset + Length));
                    return new CFFDataInput(bytes2);
                }
            }
            throw new IOException("CFF tag not found in this OpenType font.");
        }

        private static string ReadTagName(CFFDataInput input)
        {
            byte[] b = input.ReadBytes(4);
            return PdfClown.Tokens.Encoding.Pdf.Decode(b);
        }

        private static long ReadLong(CFFDataInput input)
        {
            return (input.ReadCard16() << 16) | input.ReadCard16();
        }

        private static Header ReadHeader(CFFDataInput input)
        {
            Header cffHeader = new Header();
            cffHeader.major = input.ReadCard8();
            cffHeader.minor = input.ReadCard8();
            cffHeader.hdrSize = input.ReadCard8();
            cffHeader.offSize = input.ReadOffSize();
            return cffHeader;
        }

        private static int[] ReadIndexDataOffsets(CFFDataInput input)
        {
            int count = input.ReadCard16();
            if (count == 0)
            {
                return null;
            }
            int offSize = input.ReadOffSize();
            int[] offsets = new int[count + 1];
            for (int i = 0; i <= count; i++)
            {
                int offset = input.ReadOffset(offSize);
                if (offset > input.Length)
                {
                    throw new IOException("illegal offset value " + offset + " in CFF font");
                }
                offsets[i] = offset;
            }
            return offsets;
        }

        private static byte[][] ReadIndexData(CFFDataInput input)
        {
            int[] offsets = ReadIndexDataOffsets(input);
            if (offsets == null)
            {
                return null;
            }
            int count = offsets.Length - 1;
            byte[][] indexDataValues = new byte[count][];
            for (int i = 0; i < count; i++)
            {
                int Length = offsets[i + 1] - offsets[i];
                indexDataValues[i] = input.ReadBytes(Length);
            }
            return indexDataValues;
        }

        private static string[] ReadStringIndexData(CFFDataInput input)
        {
            int[] offsets = ReadIndexDataOffsets(input);
            if (offsets == null)
            {
                return null;
            }
            int count = offsets.Length - 1;
            string[] indexDataValues = new string[count];
            for (int i = 0; i < count; i++)
            {
                int Length = offsets[i + 1] - offsets[i];
                if (Length < 0)
                {
                    throw new IOException("Negative index data Length + " + Length + " at " +
                            i + ": offsets[" + (i + 1) + "]=" + offsets[i + 1] +
                            ", offsets[" + i + "]=" + offsets[i]);
                }
                indexDataValues[i] = PdfClown.Tokens.Encoding.Pdf.Decode(input.ReadBytes(Length));
            }
            return indexDataValues;
        }

        private static DictData ReadDictData(CFFDataInput input)
        {
            DictData dict = new DictData();
            while (input.HasRemaining())
            {
                dict.Add(ReadEntry(input));
            }
            return dict;
        }

        private static DictData ReadDictData(CFFDataInput input, int dictSize)
        {
            DictData dict = new DictData();
            int endPosition = input.Position + dictSize;
            while (input.Position < endPosition)
            {
                dict.Add(ReadEntry(input));
            }
            return dict;
        }

        private static DictData.Entry ReadEntry(CFFDataInput input)
        {
            DictData.Entry entry = new DictData.Entry();
            while (true)
            {
                byte b0 = input.ReadUnsignedByte();

                if (b0 >= 0 && b0 <= 21)
                {
                    entry.CFFOperator = ReadOperator(input, b0);
                    break;
                }
                else if (b0 == 28 || b0 == 29)
                {
                    entry.Operands.Add(ReadIntegerNumber(input, b0));
                }
                else if (b0 == 30)
                {
                    entry.Operands.Add(ReadRealNumber(input, b0));
                }
                else if (b0 >= 32 && b0 <= 254)
                {
                    entry.Operands.Add(ReadIntegerNumber(input, b0));
                }
                else
                {
                    throw new IOException("invalid DICT data b0 byte: " + b0);
                }
            }
            return entry;
        }

        private static CFFOperator ReadOperator(CFFDataInput input, byte b0)
        {
            var key = ReadOperatorKey(input, b0);
            return CFFOperator.getOperator(key);
        }

        private static ByteArray ReadOperatorKey(CFFDataInput input, byte b0)
        {
            if (b0 == 12)
            {
                byte b1 = input.ReadUnsignedByte();
                return new ByteArray(b0, b1);
            }
            return new ByteArray(b0);
        }

        private static int ReadIntegerNumber(CFFDataInput input, byte b0)
        {
            if (b0 == 28)
            {
                return (int)input.ReadShort();
            }
            else if (b0 == 29)
            {
                return input.ReadInt();
            }
            else if (b0 >= 32 && b0 <= 246)
            {
                return b0 - 139;
            }
            else if (b0 >= 247 && b0 <= 250)
            {
                int b1 = input.ReadUnsignedByte();
                return (b0 - 247) * 256 + b1 + 108;
            }
            else if (b0 >= 251 && b0 <= 254)
            {
                int b1 = input.ReadUnsignedByte();
                return -(b0 - 251) * 256 - b1 - 108;
            }
            else
            {
                throw new ArgumentException();
            }
        }

        /**
         * //@param b0  
         */
        private static float ReadRealNumber(CFFDataInput input, byte b0)
        {
            var sb = new StringBuilder();
            bool done = false;
            bool exponentMissing = false;
            bool hasExponent = false;
            while (!done)
            {
                byte b = input.ReadUnsignedByte();
                int[] nibbles = { b / 16, b % 16 };
                foreach (int nibble in nibbles)
                {
                    switch (nibble)
                    {
                        case 0x0:
                        case 0x1:
                        case 0x2:
                        case 0x3:
                        case 0x4:
                        case 0x5:
                        case 0x6:
                        case 0x7:
                        case 0x8:
                        case 0x9:
                            sb.Append(nibble);
                            exponentMissing = false;
                            break;
                        case 0xa:
                            sb.Append(".");
                            break;
                        case 0xb:
                            if (hasExponent)
                            {
                                Debug.WriteLine("warning: duplicate 'E' ignored after " + sb);
                                break;
                            }
                            sb.Append("E");
                            exponentMissing = true;
                            hasExponent = true;
                            break;
                        case 0xc:
                            if (hasExponent)
                            {
                                Debug.WriteLine("warning: duplicate 'E-' ignored after " + sb);
                                break;
                            }
                            sb.Append("E-");
                            exponentMissing = true;
                            hasExponent = true;
                            break;
                        case 0xd:
                            break;
                        case 0xe:
                            sb.Append("-");
                            break;
                        case 0xf:
                            done = true;
                            break;
                        default:
                            throw new ArgumentException();
                    }
                }
            }
            if (exponentMissing)
            {
                // the exponent is missing, just Append "0" to avoid an exception
                // not sure if 0 is the correct value, but it seems to fit
                // see PDFBOX-1522
                sb.Append("0");
            }
            if (sb.Length == 0)
            {
                return 0f;
            }
            try
            {
                return float.Parse(sb.ToString());
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private CFFFont ParseFont(CFFDataInput input, string name, byte[] topDictIndex)
        {
            // top dict
            CFFDataInput topDictInput = new CFFDataInput(topDictIndex);
            DictData topDict = ReadDictData(topDictInput);

            // we don't support synthetic fonts
            DictData.Entry syntheticBaseEntry = topDict.GetEntry("SyntheticBase");
            if (syntheticBaseEntry != null)
            {
                throw new IOException("Synthetic Fonts are not supported");
            }

            // determine if this is a Type 1-equivalent font or a CIDFont
            CFFFont font;
            bool isCIDFont = topDict.GetEntry("ROS") != null;
            if (isCIDFont)
            {
                font = new CFFCIDFont();
                DictData.Entry rosEntry = topDict.GetEntry("ROS");
                ((CFFCIDFont)font).Registry = ReadString((int)rosEntry.GetNumber(0));
                ((CFFCIDFont)font).Ordering = ReadString((int)rosEntry.GetNumber(1));
                ((CFFCIDFont)font).Supplement = (int)rosEntry.GetNumber(2);
            }
            else
            {
                font = new CFFType1Font();
            }

            // name
            debugFontName = name;
            font.FontName = name;

            // top dict
            font.AddValueToTopDict("version", GetString(topDict, "version"));
            font.AddValueToTopDict("Notice", GetString(topDict, "Notice"));
            font.AddValueToTopDict("Copyright", GetString(topDict, "Copyright"));
            font.AddValueToTopDict("FullName", GetString(topDict, "FullName"));
            font.AddValueToTopDict("FamilyName", GetString(topDict, "FamilyName"));
            font.AddValueToTopDict("Weight", GetString(topDict, "Weight"));
            font.AddValueToTopDict("isFixedPitch", topDict.GetBoolean("isFixedPitch", false));
            font.AddValueToTopDict("ItalicAngle", topDict.GetNumber("ItalicAngle", 0));
            font.AddValueToTopDict("UnderlinePosition", topDict.GetNumber("UnderlinePosition", -100));
            font.AddValueToTopDict("UnderlineThickness", topDict.GetNumber("UnderlineThickness", 50));
            font.AddValueToTopDict("PaintType", topDict.GetNumber("PaintType", 0));
            font.AddValueToTopDict("CharstringType", topDict.GetNumber("CharstringType", 2));
            font.AddValueToTopDict("FontMatrix", topDict.GetArray("FontMatrix", new List<float> { 0.001F, 0F, 0F, 0.001F, 0F, 0F }));
            font.AddValueToTopDict("UniqueID", topDict.GetNumber("UniqueID", null));
            font.AddValueToTopDict("FontBBox", topDict.GetArray("FontBBox", new List<float> { 0, 0, 0, 0 }));
            font.AddValueToTopDict("StrokeWidth", topDict.GetNumber("StrokeWidth", 0));
            font.AddValueToTopDict("XUID", topDict.GetArray("XUID", null));

            // charstrings index
            DictData.Entry charStringsEntry = topDict.GetEntry("CharStrings");
            int charStringsOffset = (int)charStringsEntry.GetNumber(0);
            input.Position = charStringsOffset;
            byte[][] charStringsIndex = ReadIndexData(input);

            // charset
            DictData.Entry charsetEntry = topDict.GetEntry("charset");
            CFFCharset charset;
            if (charsetEntry != null)
            {
                int charsetId = (int)charsetEntry.GetNumber(0);
                if (!isCIDFont && charsetId == 0)
                {
                    charset = CFFISOAdobeCharset.Instance;
                }
                else if (!isCIDFont && charsetId == 1)
                {
                    charset = CFFExpertCharset.Instance;
                }
                else if (!isCIDFont && charsetId == 2)
                {
                    charset = CFFExpertSubsetCharset.Instance;
                }
                else if (charStringsIndex != null)
                {
                    input.Position = charsetId;
                    charset = readCharset(input, charStringsIndex.Length, isCIDFont);
                }
                // that should not happen
                else
                {
                    Debug.WriteLine("debug: Couldn't read CharStrings index - returning empty charset instead");
                    charset = new EmptyCharset(0);
                }

            }
            else
            {
                if (isCIDFont)
                {
                    // CharStrings index could be null if the index data couldnÄt be read
                    int numEntries = charStringsIndex == null ? 0 : charStringsIndex.Length;
                    // a CID font with no charset does not default to any predefined charset
                    charset = new EmptyCharset(numEntries);
                }
                else
                {
                    charset = CFFISOAdobeCharset.Instance;
                }
            }
            font.Charset = charset;

            // charstrings dict
            font.CharStringBytes = charStringsIndex;

            // format-specific dictionaries
            if (isCIDFont)
            {

                // CharStrings index could be null if the index data couldn't be read
                int numEntries = 0;
                if (charStringsIndex == null)
                {
                    Debug.WriteLine("debug: Couldn't read CharStrings index - parsing CIDFontDicts with number of char strings set to 0");
                }
                else
                {
                    numEntries = charStringsIndex.Length;
                }

                ParseCIDFontDicts(input, topDict, (CFFCIDFont)font, numEntries);

                List<float> privMatrix = null;
                List<Dictionary<string, Object>> fontDicts = ((CFFCIDFont)font).FontDicts;
                if (fontDicts.Count > 0 && fontDicts[0].ContainsKey("FontMatrix"))
                {
                    privMatrix = (List<float>)fontDicts[0]["FontMatrix"];
                }
                // some malformed fonts have FontMatrix in their Font DICT, see PDFBOX-2495
                List<float> matrix = topDict.GetArray("FontMatrix", null);
                if (matrix == null)
                {
                    if (privMatrix != null)
                    {
                        font.AddValueToTopDict("FontMatrix", privMatrix);
                    }
                    else
                    {
                        // default
                        font.AddValueToTopDict("FontMatrix", topDict.GetArray("FontMatrix", new List<float> { 0.001f, 0f, 0, 0.001f, 0f, 0f }));
                    }
                }
                else if (privMatrix != null)
                {
                    // we have to multiply the font matrix from the top directory with the font matrix
                    // from the private directory. This should be done for synthetic fonts only but in
                    // case of PDFBOX-3579 it's needed as well to get the right scaling
                    concatenateMatrix(matrix, privMatrix);
                }

            }
            else
            {
                ParseType1Dicts(input, topDict, (CFFType1Font)font, charset);
            }

            return font;
        }

        private void concatenateMatrix(List<float> matrixDest, List<float> matrixConcat)
        {
            // concatenate matrices
            // (a b 0)
            // (c d 0)
            // (x y 1)
            double a1 = matrixDest[0];
            double b1 = matrixDest[1];
            double c1 = matrixDest[2];
            double d1 = matrixDest[3];
            double x1 = matrixDest[4];
            double y1 = matrixDest[5];

            double a2 = matrixConcat[0];
            double b2 = matrixConcat[1];
            double c2 = matrixConcat[2];
            double d2 = matrixConcat[3];
            double x2 = matrixConcat[4];
            double y2 = matrixConcat[5];

            matrixDest[0] = (float)(a1 * a2 + b1 * c2);
            matrixDest[1] = (float)(a1 * b2 + b1 * d1);
            matrixDest[2] = (float)(c1 * a2 + d1 * c2);
            matrixDest[3] = (float)(c1 * b2 + d1 * d2);
            matrixDest[4] = (float)(x1 * a2 + y1 * c2 + x2);
            matrixDest[5] = (float)(x1 * b2 + y1 * d2 + y2);
        }

        /**
         * Parse dictionaries specific to a CIDFont.
         */
        private void ParseCIDFontDicts(CFFDataInput input, DictData topDict, CFFCIDFont font, int nrOfcharStrings)
        {
            // In a CIDKeyed Font, the Private dictionary isn't in the Top Dict but in the Font dict
            // which can be accessed by a lookup using FDArray and FDSelect
            DictData.Entry fdArrayEntry = topDict.GetEntry("FDArray");
            if (fdArrayEntry == null)
            {
                throw new IOException("FDArray is missing for a CIDKeyed Font.");
            }

            // font dict index
            int fontDictOffset = (int)fdArrayEntry.GetNumber(0);
            input.Position = fontDictOffset;
            byte[][] fdIndex = ReadIndexData(input);

            List<Dictionary<string, Object>> privateDictionaries = new List<Dictionary<string, Object>>();
            List<Dictionary<string, Object>> fontDictionaries = new List<Dictionary<string, Object>>();

            foreach (byte[] bytes in fdIndex)
            {
                CFFDataInput fontDictInput = new CFFDataInput(bytes);
                DictData fontDict = ReadDictData(fontDictInput);

                // read private dict
                DictData.Entry privateEntry = fontDict.GetEntry("Private");
                if (privateEntry == null)
                {
                    throw new IOException("Font DICT invalid without \"Private\" entry");
                }

                // font dict
                Dictionary<string, Object> fontDictMap = new Dictionary<string, Object>(4, StringComparer.Ordinal);
                fontDictMap["FontName"] = GetString(fontDict, "FontName");
                fontDictMap["FontType"] = fontDict.GetNumber("FontType", 0);
                fontDictMap["FontBBox"] = fontDict.GetArray("FontBBox", null);
                fontDictMap["FontMatrix"] = fontDict.GetArray("FontMatrix", null);
                // TODO OD-4 : Add here other keys
                fontDictionaries.Add(fontDictMap);

                int privateOffset = (int)privateEntry.GetNumber(1);
                input.Position = privateOffset;
                int privateSize = (int)privateEntry.GetNumber(0);
                DictData privateDict = ReadDictData(input, privateSize);

                // populate private dict
                Dictionary<string, Object> privDict = ReadPrivateDict(privateDict);
                privateDictionaries.Add(privDict);

                // local subrs
                int localSubrOffset = (int)privateDict.GetNumber("Subrs", 0);
                if (localSubrOffset > 0)
                {
                    input.Position = privateOffset + localSubrOffset;
                    privDict["Subrs"] = ReadIndexData(input);
                }
            }

            // font-dict (FD) select
            DictData.Entry fdSelectEntry = topDict.GetEntry("FDSelect");
            int fdSelectPos = (int)fdSelectEntry.GetNumber(0);
            input.Position = fdSelectPos;
            FDSelect fdSelect = ReadFDSelect(input, nrOfcharStrings, font);

            // TODO almost certainly erroneous - CIDFonts do not have a top-level private dict
            // font.addValueToPrivateDict("defaultWidthX", 1000);
            // font.addValueToPrivateDict("nominalWidthX", 0);

            font.FontDicts = fontDictionaries;
            font.PrivDicts = privateDictionaries;
            font.FdSelect = fdSelect;
        }

        private Dictionary<string, Object> ReadPrivateDict(DictData privateDict)
        {
            Dictionary<string, Object> privDict = new Dictionary<string, Object>(17);
            privDict["BlueValues"] = privateDict.GetDelta("BlueValues", null);
            privDict["OtherBlues"] = privateDict.GetDelta("OtherBlues", null);
            privDict["FamilyBlues"] = privateDict.GetDelta("FamilyBlues", null);
            privDict["FamilyOtherBlues"] = privateDict.GetDelta("FamilyOtherBlues", null);
            privDict["BlueScale"] = privateDict.GetNumber("BlueScale", 0.039625f);
            privDict["BlueShift"] = privateDict.GetNumber("BlueShift", 7);
            privDict["BlueFuzz"] = privateDict.GetNumber("BlueFuzz", 1);
            privDict["StdHW"] = privateDict.GetNumber("StdHW", null);
            privDict["StdVW"] = privateDict.GetNumber("StdVW", null);
            privDict["StemSnapH"] = privateDict.GetDelta("StemSnapH", null);
            privDict["StemSnapV"] = privateDict.GetDelta("StemSnapV", null);
            privDict["ForceBold"] = privateDict.GetBoolean("ForceBold", false);
            privDict["LanguageGroup"] = privateDict.GetNumber("LanguageGroup", 0);
            privDict["ExpansionFactor"] = privateDict.GetNumber("ExpansionFactor", 0.06f);
            privDict["initialRandomSeed"] = privateDict.GetNumber("initialRandomSeed", 0);
            privDict["defaultWidthX"] = privateDict.GetNumber("defaultWidthX", 0);
            privDict["nominalWidthX"] = privateDict.GetNumber("nominalWidthX", 0);
            return privDict;
        }

        /**
         * Parse dictionaries specific to a Type 1-equivalent font.
         */
        private void ParseType1Dicts(CFFDataInput input, DictData topDict, CFFType1Font font, CFFCharset charset)
        {
            // encoding
            DictData.Entry encodingEntry = topDict.GetEntry("Encoding");
            CFFEncoding encoding;
            int encodingId = encodingEntry != null ? (int)encodingEntry.GetNumber(0) : 0;
            switch (encodingId)
            {
                case 0:
                    encoding = CFFStandardEncoding.Instance;
                    break;
                case 1:
                    encoding = CFFExpertEncoding.Instance;
                    break;
                default:
                    input.Position = encodingId;
                    encoding = ReadEncoding(input, charset);
                    break;
            }
            font.Encoding = encoding;

            // read private dict
            DictData.Entry privateEntry = topDict.GetEntry("Private");
            if (privateEntry == null)
            {
                throw new IOException("Private dictionary entry missing for font " + font.Name);
            }
            int privateOffset = (int)privateEntry.GetNumber(1);
            input.Position = privateOffset;
            int privateSize = (int)privateEntry.GetNumber(0);
            DictData privateDict = ReadDictData(input, privateSize);

            // populate private dict
            Dictionary<string, Object> privDict = ReadPrivateDict(privateDict);
            foreach (var entry in privDict) font.AddToPrivateDict(entry.Key, entry.Value);

            // local subrs
            int localSubrOffset = (int)privateDict.GetNumber("Subrs", 0);
            if (localSubrOffset > 0)
            {
                input.Position = privateOffset + localSubrOffset;
                font.AddToPrivateDict("Subrs", ReadIndexData(input));
            }
        }

        private string ReadString(int index)
        {
            if (index >= 0 && index <= 390)
            {
                return CFFStandardString.GetName(index);
            }
            if (index - 391 < stringIndex.Length)
            {
                return stringIndex[index - 391];
            }
            else
            {
                // technically this maps to .notdef, but we need a unique sid name
                return "SID" + index;
            }
        }

        private string GetString(DictData dict, string name)
        {
            DictData.Entry entry = dict.GetEntry(name);
            return entry != null ? ReadString((int)entry.GetNumber(0)) : null;
        }

        private CFFEncoding ReadEncoding(CFFDataInput dataInput, CFFCharset charset)
        {
            int format = dataInput.ReadCard8();
            int baseFormat = format & 0x7f;

            switch (baseFormat)
            {
                case 0:
                    return ReadFormat0Encoding(dataInput, charset, format);
                case 1:
                    return ReadFormat1Encoding(dataInput, charset, format);
                default:
                    throw new ArgumentException();
            }
        }

        private Format0Encoding ReadFormat0Encoding(CFFDataInput dataInput, CFFCharset charset,
                                                    int format)
        {
            Format0Encoding encoding = new Format0Encoding();
            encoding.format = format;
            encoding.nCodes = dataInput.ReadCard8();
            encoding.Add(0, 0, ".notdef");
            for (int gid = 1; gid <= encoding.nCodes; gid++)
            {
                int code = dataInput.ReadCard8();
                int sid = charset.GetSIDForGID(gid);
                encoding.Add(code, sid, ReadString(sid));
            }
            if ((format & 0x80) != 0)
            {
                ReadSupplement(dataInput, encoding);
            }
            return encoding;
        }

        private Format1Encoding ReadFormat1Encoding(CFFDataInput dataInput, CFFCharset charset, int format)
        {
            Format1Encoding encoding = new Format1Encoding();
            encoding.format = format;
            encoding.nRanges = dataInput.ReadCard8();
            encoding.Add(0, 0, ".notdef");
            int gid = 1;
            for (int i = 0; i < encoding.nRanges; i++)
            {
                int rangeFirst = dataInput.ReadCard8();
                int rangeLeft = dataInput.ReadCard8();
                for (int j = 0; j < 1 + rangeLeft; j++)
                {
                    int sid = charset.GetSIDForGID(gid);
                    int code = rangeFirst + j;
                    encoding.Add(code, sid, ReadString(sid));
                    gid++;
                }
            }
            if ((format & 0x80) != 0)
            {
                ReadSupplement(dataInput, encoding);
            }
            return encoding;
        }

        private void ReadSupplement(CFFDataInput dataInput, CFFBuiltInEncoding encoding)
        {
            encoding.nSups = dataInput.ReadCard8();
            encoding.supplement = new CFFBuiltInEncoding.Supplement[encoding.nSups];
            for (int i = 0; i < encoding.supplement.Length; i++)
            {
                CFFBuiltInEncoding.Supplement supplement = new CFFBuiltInEncoding.Supplement();
                supplement.code = dataInput.ReadCard8();
                supplement.sid = dataInput.ReadSID();
                supplement.name = ReadString(supplement.sid);
                encoding.supplement[i] = supplement;
                encoding.Add(supplement.code, supplement.sid, ReadString(supplement.sid));
            }
        }

        /**
         * Read the FDSelect Data according to the format.
         * //@param dataInput
         * //@param nGlyphs
         * //@param ros
         * //@return the FDSelect data
         * //@throws IOException
         */
        private static FDSelect ReadFDSelect(CFFDataInput dataInput, int nGlyphs, CFFCIDFont ros)
        {
            int format = dataInput.ReadCard8();
            switch (format)
            {
                case 0:
                    return ReadFormat0FDSelect(dataInput, format, nGlyphs, ros);
                case 3:
                    return ReadFormat3FDSelect(dataInput, format, nGlyphs, ros);
                default:
                    throw new ArgumentException();
            }
        }

        /**
         * Read the Format 0 of the FDSelect data structure.
         * //@param dataInput
         * //@param format
         * //@param nGlyphs
         * //@param ros
         * //@return the Format 0 of the FDSelect data
         * //@throws IOException
         */
        private static Format0FDSelect ReadFormat0FDSelect(CFFDataInput dataInput, int format, int nGlyphs, CFFCIDFont ros)
        {
            Format0FDSelect fdselect = new Format0FDSelect(ros);
            fdselect.format = format;
            fdselect.fds = new int[nGlyphs];
            for (int i = 0; i < fdselect.fds.Length; i++)
            {
                fdselect.fds[i] = dataInput.ReadCard8();
            }
            return fdselect;
        }

        /**
         * Read the Format 3 of the FDSelect data structure.
         * 
         * //@param dataInput
         * //@param format
         * //@param nGlyphs
         * //@param ros
         * //@return the Format 3 of the FDSelect data
         * //@throws IOException
         */
        private static Format3FDSelect ReadFormat3FDSelect(CFFDataInput dataInput, int format, int nGlyphs, CFFCIDFont ros)
        {
            Format3FDSelect fdselect = new Format3FDSelect(ros);
            fdselect.format = format;
            fdselect.nbRanges = dataInput.ReadCard16();

            fdselect.range3 = new Range3[fdselect.nbRanges];
            for (int i = 0; i < fdselect.nbRanges; i++)
            {
                Range3 r3 = new Range3();
                r3.first = dataInput.ReadCard16();
                r3.fd = dataInput.ReadCard8();
                fdselect.range3[i] = r3;

            }

            fdselect.sentinel = dataInput.ReadCard16();
            return fdselect;
        }

        /**
         *  Format 3 FDSelect data.
         */
        internal sealed class Format3FDSelect : FDSelect
        {
            internal int format;
            internal int nbRanges;
            internal Range3[] range3;
            internal int sentinel;

            public Format3FDSelect(CFFCIDFont owner)
                : base(owner)
            {
            }

            public override int GetFDIndex(int gid)
            {
                for (int i = 0; i < nbRanges; ++i)
                {
                    if (range3[i].first <= gid)
                    {
                        if (i + 1 < nbRanges)
                        {
                            if (range3[i + 1].first > gid)
                            {
                                return range3[i].fd;
                            }
                            // go to next range
                        }
                        else
                        {
                            // last range reach, the sentinel must be greater than gid
                            if (sentinel > gid)
                            {
                                return range3[i].fd;
                            }
                            return -1;
                        }
                    }
                }
                return 0;
            }

            public override string ToString()
            {
                return GetType().Name + "[format=" + format + " nbRanges=" + nbRanges + ", range3="
                        + string.Join(", ", range3.Select(p => p.ToString())) + " sentinel=" + sentinel + "]";
            }
        }

        /**
         * Structure of a Range3 element.
         */
        internal sealed class Range3
        {
            internal int first;
            internal int fd;

            public override string ToString()
            {
                return GetType().Name + "[first=" + first + ", fd=" + fd + "]";
            }
        }

        /**
         *  Format 0 FDSelect.
         */
        internal class Format0FDSelect : FDSelect
        {
            //@SuppressWarnings("unused")
            internal int format;
            internal int[] fds;

            public Format0FDSelect(CFFCIDFont owner)
                : base(owner)
            {
            }

            public override int GetFDIndex(int gid)
            {
                if (gid < fds.Length)
                {
                    return fds[gid];
                }
                return 0;
            }

            public override string ToString()
            {
                return GetType().Name + "[fds=" + string.Join(", ", fds) + "]";
            }
        }

        private CFFCharset readCharset(CFFDataInput dataInput, int nGlyphs, bool isCIDFont)

        {
            int format = dataInput.ReadCard8();
            switch (format)
            {
                case 0:
                    return ReadFormat0Charset(dataInput, format, nGlyphs, isCIDFont);
                case 1:
                    return ReadFormat1Charset(dataInput, format, nGlyphs, isCIDFont);
                case 2:
                    return ReadFormat2Charset(dataInput, format, nGlyphs, isCIDFont);
                default:
                    throw new ArgumentException();
            }
        }

        private Format0Charset ReadFormat0Charset(CFFDataInput dataInput, int format, int nGlyphs, bool isCIDFont)
        {
            Format0Charset charset = new Format0Charset(isCIDFont);
            charset.format = format;
            if (isCIDFont)
            {
                charset.AddCID(0, 0);
            }
            else
            {
                charset.AddSID(0, 0, ".notdef");
            }

            for (int gid = 1; gid < nGlyphs; gid++)
            {
                int sid = dataInput.ReadSID();
                if (isCIDFont)
                {
                    charset.AddCID(gid, sid);
                }
                else
                {
                    charset.AddSID(gid, sid, ReadString(sid));
                }
            }
            return charset;
        }

        private Format1Charset ReadFormat1Charset(CFFDataInput dataInput, int format, int nGlyphs,
                                                  bool isCIDFont)
        {
            Format1Charset charset = new Format1Charset(isCIDFont);
            charset.format = format;
            if (isCIDFont)
            {
                charset.AddCID(0, 0);
                charset.rangesCID2GID = new List<RangeMapping>();
            }
            else
            {
                charset.AddSID(0, 0, ".notdef");
            }

            for (int gid = 1; gid < nGlyphs; gid++)
            {
                int rangeFirst = dataInput.ReadSID();
                int rangeLeft = dataInput.ReadCard8();
                if (!isCIDFont)
                {
                    for (int j = 0; j < 1 + rangeLeft; j++)
                    {
                        int sid = rangeFirst + j;
                        charset.AddSID(gid + j, sid, ReadString(sid));
                    }
                }
                else
                {
                    charset.rangesCID2GID.Add(new RangeMapping(gid, rangeFirst, rangeLeft));
                }
                gid += rangeLeft;
            }
            return charset;
        }

        private Format2Charset ReadFormat2Charset(CFFDataInput dataInput, int format, int nGlyphs,
                                                  bool isCIDFont)
        {
            Format2Charset charset = new Format2Charset(isCIDFont);
            charset.format = format;
            if (isCIDFont)
            {
                charset.AddCID(0, 0);
                charset.rangesCID2GID = new List<RangeMapping>();
            }
            else
            {
                charset.AddSID(0, 0, ".notdef");
            }

            for (int gid = 1; gid < nGlyphs; gid++)
            {
                int first = dataInput.ReadSID();
                int nLeft = dataInput.ReadCard16();
                if (!isCIDFont)
                {
                    for (int j = 0; j < 1 + nLeft; j++)
                    {
                        int sid = first + j;
                        charset.AddSID(gid + j, sid, ReadString(sid));
                    }
                }
                else
                {
                    charset.rangesCID2GID.Add(new RangeMapping(gid, first, nLeft));
                }
                gid += nLeft;
            }
            return charset;
        }

        /**
         * Inner class holding the header of a CFF font. 
         */
        private class Header
        {
            internal int major;
            internal int minor;
            internal int hdrSize;
            internal int offSize;

            public override string ToString()
            {
                return GetType().Name + "[major=" + major + ", minor=" + minor + ", hdrSize=" + hdrSize
                        + ", offSize=" + offSize + "]";
            }
        }

        /**
         * Inner class holding the DictData of a CFF font. 
         */
        internal class DictData
        {
            private readonly Dictionary<string, Entry> entries = new Dictionary<string, Entry>();

            public void Add(Entry entry)
            {
                if (entry.CFFOperator != null)
                {
                    entries[entry.CFFOperator.Name] = entry;
                }
            }

            public Entry GetEntry(string name)
            {
                return entries.TryGetValue(name, out var entry) ? entry : null;
            }

            public bool GetBoolean(string name, bool defaultValue)
            {
                Entry entry = GetEntry(name);
                return entry != null && entry.Operands.Count > 0 ? entry.GetBool(0) : defaultValue;
            }

            public List<float> GetArray(string name, List<float> defaultValue)
            {
                Entry entry = GetEntry(name);
                return entry != null && entry.Operands.Count > 0 ? entry.Operands : defaultValue;
            }

            public float? GetNumber(string name, float? defaultValue)
            {
                Entry entry = GetEntry(name);
                return entry != null && entry.Operands.Count > 0 ? entry.GetNumber(0) : defaultValue;
            }

            public List<float> GetDelta(string name, List<float> defaultValue)
            {
                Entry entry = GetEntry(name);
                return entry != null && entry.Operands.Count > 0 ? entry.GetDelta() : defaultValue;
            }

            /**
             * {//@inheritDoc} 
             */
            public override string ToString()
            {
                return GetType().Name + "[entries=" + entries + "]";
            }

            /**
             * Inner class holding an operand of a CFF font. 
             */
            internal class Entry
            {
                internal List<float> Operands = new List<float>();
                internal CFFOperator CFFOperator = null;

                public float GetNumber(int index)
                {
                    return Operands[index];
                }

                public bool GetBool(int index)
                {
                    float operand = Operands[index];
                    switch (operand)
                    {
                        case 0F:
                            return false;
                        case 1F:
                            return true;
                        default:
                            break;
                    }
                    throw new ArgumentException();
                }

                public List<float> GetDelta()
                {
                    List<float> result = new List<float>(Operands);
                    for (int i = 1; i < result.Count; i++)
                    {
                        float previous = result[i - 1];
                        float current = result[i];
                        int sum = (int)previous + (int)current;
                        result[i] = sum;
                    }
                    return result;
                }

                public override string ToString()
                {
                    return GetType().Name + "[operands=" + Operands + ", operator=" + CFFOperator + "]";
                }
            }
        }

        /**
         * Inner class representing a font's built-in CFF encoding. 
         */
        internal abstract class CFFBuiltInEncoding : CFFEncoding
        {
            internal int nSups;
            internal Supplement[] supplement;

            /**
             * Inner class representing a supplement for an encoding. 
             */
            internal class Supplement
            {
                internal int code;
                internal int sid;
                internal string name;

                public int Code
                {
                    get => code;
                }

                public int SID
                {
                    get => sid;
                }

                public string Name
                {
                    get => name;
                }

                public override string ToString()
                {
                    return GetType().Name + "[code=" + code + ", sid=" + sid + "]";
                }
            }
        }

        /**
         * Inner class representing a Format0 encoding. 
         */
        internal class Format0Encoding : CFFBuiltInEncoding
        {
            internal int format;
            internal int nCodes;

            public override string ToString()
            {
                return GetType().Name + "[format=" + format + ", nCodes=" + nCodes
                        + ", supplement=" + string.Join(", ", base.supplement.Select(p => p.ToString())) + "]";
            }
        }

        /**
         * Inner class representing a Format1 encoding. 
         */
        internal class Format1Encoding : CFFBuiltInEncoding
        {
            internal int format;
            internal int nRanges;

            public override string ToString()
            {
                return GetType().Name + "[format=" + format + ", nRanges=" + nRanges
                        + ", supplement=" + string.Join(", ", base.supplement.Select(p => p.ToString())) + "]";
            }
        }

        /**
         * Inner class representing an embedded CFF charset.
         */
        internal abstract class EmbeddedCharset : CFFCharset
        {
            protected EmbeddedCharset(bool isCIDFont)
                : base(isCIDFont)
            {
            }
        }

        /**
         * An empty charset in a malformed CID font.
         */
        internal class EmptyCharset : EmbeddedCharset
        {
            internal EmptyCharset(int numCharStrings)
                : base(true)
            {
                AddCID(0, 0); // .notdef

                // Adobe Reader treats CID as GID, PDFBOX-2571 p11.
                for (int i = 1; i <= numCharStrings; i++)
                {
                    AddCID(i, i);
                }
            }

            public override string ToString()
            {
                return GetType().Name;
            }
        }

        /**
         * Inner class representing a Format0 charset. 
         */
        internal class Format0Charset : EmbeddedCharset
        {
            internal int format;

            internal Format0Charset(bool isCIDFont)
                : base(isCIDFont)
            {
            }

            public override string ToString()
            {
                return GetType().Name + "[format=" + format + "]";
            }
        }

        /**
         * Inner class representing a Format1 charset. 
         */
        internal class Format1Charset : EmbeddedCharset
        {

            internal int format;
            internal List<RangeMapping> rangesCID2GID;

            public Format1Charset(bool isCIDFont)
                : base(isCIDFont)
            {
            }

            public override int GetCIDForGID(int gid)
            {
                if (IsCIDFont)
                {
                    foreach (RangeMapping mapping in rangesCID2GID)
                    {
                        if (mapping.IsInRange(gid))
                        {
                            return mapping.MapValue(gid);
                        }
                    }
                }
                return base.GetCIDForGID(gid);
            }

            public override int GetGIDForCID(int cid)
            {
                if (IsCIDFont)
                {
                    foreach (RangeMapping mapping in rangesCID2GID)
                    {
                        if (mapping.IsInReverseRange(cid))
                        {
                            return mapping.MapReverseValue(cid);
                        }
                    }
                }
                return base.GetGIDForCID(cid);
            }

            public override string ToString()
            {
                return GetType().Name + "[format=" + format + "]";
            }
        }

        /**
         * Inner class representing a Format2 charset. 
         */
        internal class Format2Charset : EmbeddedCharset
        {
            internal int format;
            internal List<RangeMapping> rangesCID2GID;

            internal Format2Charset(bool isCIDFont)
                : base(isCIDFont)
            {
            }

            public override int GetCIDForGID(int gid)
            {
                foreach (RangeMapping mapping in rangesCID2GID)
                {
                    if (mapping.IsInRange(gid))
                    {
                        return mapping.MapValue(gid);
                    }
                }
                return base.GetCIDForGID(gid);
            }

            public override int GetGIDForCID(int cid)
            {
                foreach (RangeMapping mapping in rangesCID2GID)
                {
                    if (mapping.IsInReverseRange(cid))
                    {
                        return mapping.MapReverseValue(cid);
                    }
                }
                return base.GetGIDForCID(cid);
            }

            public override string ToString()
            {
                return GetType().Name + "[format=" + format + "]";
            }

        }

        /**
         * Inner class representing a rang mapping for a CID charset. 
         */
        internal readonly struct RangeMapping
        {
            private readonly int startValue;
            private readonly int endValue;
            private readonly int startMappedValue;
            private readonly int endMappedValue;

            public RangeMapping(int startGID, int first, int nLeft)
            {
                this.startValue = startGID;
                endValue = startValue + nLeft;
                this.startMappedValue = first;
                endMappedValue = startMappedValue + nLeft;
            }

            public bool IsInRange(int value)
            {
                return value >= startValue && value <= endValue;
            }

            public bool IsInReverseRange(int value)
            {
                return value >= startMappedValue && value <= endMappedValue;
            }

            public int MapValue(int value)
            {
                if (IsInRange(value))
                {
                    return startMappedValue + (value - startValue);
                }
                else
                {
                    return 0;
                }
            }

            public int MapReverseValue(int value)
            {
                if (IsInReverseRange(value))
                {
                    return startValue + (value - startMappedValue);
                }
                else
                {
                    return 0;
                }
            }

            public override string ToString()
            {
                return GetType().Name + "[start value=" + startValue + ", end value=" + endValue + ", start mapped-value=" + startMappedValue + ", end mapped-value=" + endMappedValue + "]";
            }
        }

        public override string ToString()
        {
            return GetType().Name + "[" + debugFontName + "]";
        }
    }

    public static class BytesExtension
    {
        public static byte[] CopyOfRange(this byte[] src, int start, int end)
        {
            var len = end - start;
            var dest = new byte[len];
            Array.Copy(src, start, dest, 0, len);
            return dest;
        }
    }
}
