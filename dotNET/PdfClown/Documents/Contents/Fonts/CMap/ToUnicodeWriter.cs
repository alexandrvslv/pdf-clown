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

using System;
using System.Collections.Generic;
using System.IO;

namespace PdfClown.Documents.Contents.Fonts
{

    /**
     * Writes ToUnicode Mapping Files.
     *
     * @author John Hewson
     */
    public sealed class ToUnicodeWriter
    {
        private readonly Dictionary<int, string> cidToUnicode = new Dictionary<int, string>();
        private int wMode;

        /**
		 * To test corner case of PDFBOX-4302.
		 */
        static readonly int MAX_ENTRIES_PER_OPERATOR = 100;

        /**
		 * Creates a new ToUnicode CMap writer.
		 */
        public ToUnicodeWriter()
        {
            this.wMode = 0;
        }

        /**
		 * Sets the WMode (writing mode) of this CMap.
		 *
		 * @param wMode 1 for vertical, 0 for horizontal (default)
		 */
        public void SetWMode(int wMode)
        {
            this.wMode = wMode;
        }

        /**
		 * Adds the given CID to Unicode mapping.
		 *
		 * @param cid CID
		 * @param text Unicode text, up to 512 bytes.
		 */
        public void Add(int cid, string text)
        {
            if (cid < 0 || cid > 0xFFFF)
            {
                throw new ArgumentException("CID is not valid");
            }

            if (string.IsNullOrEmpty(text))
            {
                throw new ArgumentException("Text is null or empty");
            }

            cidToUnicode[cid] = text;
        }

        /**
		 * Writes the CMap as ASCII to the given output stream.
		 *
		 * @param output ASCII output stream
		 * @throws IOException if the stream could not be written
		 */
        public void WriteTo(Stream output)
        {
            var writer = new StreamWriter(output, System.Text.Encoding.ASCII);

            WriteLine(writer, "/CIDInit /ProcSet findresource begin");
            WriteLine(writer, "12 dict begin\n");

            WriteLine(writer, "begincmap");
            WriteLine(writer, "/CIDSystemInfo");
            WriteLine(writer, "<< /Registry (Adobe)");
            WriteLine(writer, "/Ordering (UCS)");
            WriteLine(writer, "/Supplement 0");
            WriteLine(writer, ">> def\n");

            WriteLine(writer, "/CMapName /Adobe-Identity-UCS" + " def");
            WriteLine(writer, "/CMapType 2 def\n"); // 2 = ToUnicode

            if (wMode != 0)
            {
                WriteLine(writer, "/WMode /" + wMode + " def");
            }

            // ToUnicode always uses 16-bit CIDs
            WriteLine(writer, "1 begincodespacerange");
            WriteLine(writer, "<0000> <FFFF>");
            WriteLine(writer, "endcodespacerange\n");

            // CID -> Unicode mappings, we use ranges to generate a smaller CMap
            List<int> srcFrom = new List<int>();
            List<int> srcTo = new List<int>();
            List<string> dstString = new List<string>();

            int srcPrev = -1;
            string dstPrev = "";

            int srcCode1 = -1;

            foreach (var entry in cidToUnicode)
            {
                int cid = entry.Key;
                string text = entry.Value;

                if (cid == srcPrev + 1 &&                                 // CID must be last CID + 1
                    dstPrev.codePointCount(0, dstPrev.Length) == 1 &&   // no UTF-16 surrogates
                    text.codePointAt(0) == dstPrev.codePointAt(0) + 1 &&  // dstString must be prev + 1
                    dstPrev.codePointAt(0) + 1 <= 255 - (cid - srcCode1)) // increment last byte only
                {
                    // extend range
                    srcTo.Insert(srcTo.Count - 1, cid);
                }
                else
                {
                    // begin range
                    srcCode1 = cid;
                    srcFrom.Add(cid);
                    srcTo.Add(cid);
                    dstString.Add(text);
                }
                srcPrev = cid;
                dstPrev = text;
            }

            // limit entries per operator
            int batchCount = (int)Math.Ceiling(srcFrom.Count / (double)MAX_ENTRIES_PER_OPERATOR);
            for (int batch = 0; batch < batchCount; batch++)
            {
                int count = batch == batchCount - 1 ?
                                srcFrom.Count - MAX_ENTRIES_PER_OPERATOR * batch :
                                MAX_ENTRIES_PER_OPERATOR;
                writer.Write(count + " beginbfrange\n");
                for (int j = 0; j < count; j++)
                {
                    int index = batch * MAX_ENTRIES_PER_OPERATOR + j;
                    writer.Write('<');
                    writer.Write(Hex.getChars(srcFrom[index].shortValue()));
                    writer.Write("> ");

                    writer.Write('<');
                    writer.Write(Hex.getChars(srcTo[index].shortValue()));
                    writer.Write("> ");

                    writer.Write('<');
                    writer.Write(Hex.getCharsUTF16BE(dstString[index]));
                    writer.Write(">\n");
                }
                WriteLine(writer, "endbfrange\n");
            }

            // footer
            WriteLine(writer, "endcmap");
            WriteLine(writer, "CMapName currentdict /CMap defineresource pop");
            WriteLine(writer, "end");
            WriteLine(writer, "end");

            writer.Flush();
        }

        private void WriteLine(StreamWriter writer, string text)
        {
            writer.Write(text);
            writer.Write('\n');
        }
    }
}