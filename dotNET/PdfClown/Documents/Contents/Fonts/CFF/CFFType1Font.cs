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
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using PdfClown.Documents.Contents.Fonts.Type1;

namespace PdfClown.Documents.Contents.Fonts.CCF
{
    /**
     * A Type 1-equivalent font program represented in a CFF file. Thread safe.
     *
     * @author Villu Ruusmann
     * @author John Hewson
     */
    public class CFFType1Font : CFFFont, IType1CharStringReader, IEncodedFont
    {
        private readonly Dictionary<string, object> privateDict = new Dictionary<string, object>(StringComparer.Ordinal);
        private CFFEncoding encoding;

        private readonly Dictionary<int, Type2CharString> charStringCache = new Dictionary<int, Type2CharString>();

        public override SKPath GetPath(string name)
        {
            return GetType1CharString(name).Path;
        }


        public override float GetWidth(string name)
        {
            return GetType1CharString(name).Width;
        }


        public override bool HasGlyph(string name)
        {
            int sid = charset.GetSID(name);
            int gid = charset.GetGIDForSID(sid);
            return gid != 0;
        }

        public override List<float> FontMatrix
        {
            get => topDict.TryGetValue("FontMatrix", out var array) ? (List<float>)array : null;
        }

        /**
		 * Returns the Type 1 charstring for the given PostScript glyph name.
		 *
		 * @param name PostScript glyph name
		 * @throws IOException if the charstring could not be read
		 */
        public Type1CharString GetType1CharString(string name)
        {
            // lookup via charset
            int gid = NameToGID(name);

            // lookup in CharStrings INDEX
            return GetType2CharString(gid, name);
        }

        /**
		 * Returns the GID for the given PostScript glyph name.
		 * 
		 * @param name a PostScript glyph name.
		 * @return GID
		 */
        public int NameToGID(string name)
        {
            // some fonts have glyphs beyond their encoding, so we look up by charset SID
            int sid = charset.GetSID(name);
            return charset.GetGIDForSID(sid);
        }

        /**
		 * Returns the Type 1 charstring for the given GID.
		 *
		 * @param gid GID
		 * @throws IOException if the charstring could not be read
		 */

        public override Type2CharString GetType2CharString(int gid)
        {
            string name = "GID+" + gid; // for debugging only
            return GetType2CharString(gid, name);
        }

        // Returns the Type 2 charstring for the given GID, with name for debugging
        private Type2CharString GetType2CharString(int gid, string name)
        {
            if (!charStringCache.TryGetValue(gid, out Type2CharString type2))
            {
                byte[] bytes = null;
                if (gid < charStrings.Length)
                {
                    bytes = charStrings[gid];
                }
                if (bytes == null)
                {
                    // .notdef
                    bytes = charStrings[0];
                }
                List<object> type2seq = Type2CharStringParser.Parse(fontName, name, bytes, globalSubrIndex, LocalSubrIndex);
                type2 = new Type2CharString(this, fontName, name, gid, type2seq, GetDefaultWidthX(), GetNominalWidthX());
                charStringCache[gid] = type2;
            }
            return type2;
        }

        /**
		 * Returns the private dictionary.
		 *
		 * @return the dictionary
		 */
        public Dictionary<string, object> PrivateDict
        {
            get => privateDict;
        }

        /**
		 * Adds the given key/value pair to the private dictionary.
		 *
		 * @param name the given key
		 * @param value the given value
		 */
        // todo: can't we just accept a Dictionary?
        public void AddToPrivateDict(string name, object value)
        {
            if (value != null)
            {
                privateDict[name] = value;
            }
        }

        /**
		 * Returns the CFFEncoding of the font.
		 *
		 * @return the encoding
		 */
        public Encoding Encoding
        {
            get => encoding;
            set => encoding = (CFFEncoding)value;
        }

        private byte[][] LocalSubrIndex
        {
            get => (byte[][])(privateDict.TryGetValue("Subrs", out var array) ? array : null);
        }

        // helper for looking up keys/values
        private object GetProperty(string name)
        {
            if (topDict.TryGetValue(name, out var topDictValue))
            {
                return topDictValue;
            }
            if (privateDict.TryGetValue(name, out object privateDictValue))
            {
                return privateDictValue;
            }
            return null;
        }

        private int GetDefaultWidthX()
        {
            var num = GetProperty("defaultWidthX");
            if (num == null)
            {
                return 1000;
            }
            return (int)(float)num;
        }

        private int GetNominalWidthX()
        {
            var num = GetProperty("nominalWidthX");
            if (num == null)
            {
                return 0;
            }
            return (int)(float)num;
        }
    }
}