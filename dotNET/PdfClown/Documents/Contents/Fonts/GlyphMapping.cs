/*
  Copyright 2009-2011 Stefano Chizzolini. http://www.pdfclown.org

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

using PdfClown.Bytes;
using PdfClown.Documents;
using PdfClown.Objects;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace PdfClown.Documents.Contents.Fonts
{
    /**
      <summary>Adobe standard glyph mapping (unicode-encoding against glyph-naming)
      [PDF:1.6:D;AGL:2.0].</summary>
    */
    public class GlyphMapping
    {
        public static readonly GlyphMapping Default = new GlyphMapping("AGL20");
        public static readonly GlyphMapping ZapfDingbats = new GlyphMapping("ZapfDingbats");
        public static readonly GlyphMapping DLFONT = new GlyphMapping("G500");
        public static bool IsExist(string fontName) => typeof(GlyphMapping).Assembly.GetManifestResourceNames().Contains($"fonts.{fontName}");

        private readonly Dictionary<string, int> nameToCode = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<int, string> codeToName = new Dictionary<int, string>();
        private readonly Dictionary<string, int> uniNameToUnicodeCache = new Dictionary<string, int>();
        public GlyphMapping(string fontName)
        { Load($"fonts.{fontName}"); }

        public int? ToUnicode(string name)
        {
            if (name == null)
            {
                return null;
            }
            if (nameToCode.TryGetValue(name, out var unicode))
            {
                return unicode;
            }
            // separate read/write cache for thread safety
            if (!uniNameToUnicodeCache.TryGetValue(name, out unicode))
            {
                // test if we have a suffix and if so remove it
                var dotIndex = name.IndexOf('.');
                int nameLength = name.Length;
                if (dotIndex > 0)
                {
                    unicode = ToUnicode(name.Substring(0, dotIndex)) ?? -1;
                }
                else if (name.StartsWith("uni", StringComparison.Ordinal) && name.Length == 7)
                {
                    // test for Unicode name in the format uniXXXX where X is hex
                    var uniStr = new StringBuilder();
                    try
                    {
                        for (int chPos = 3; chPos + 4 <= nameLength; chPos += 4)
                        {
                            int codePoint = Convert.ToInt32(name.Substring(chPos, 4), 16);
                            if (codePoint > 0xD7FF && codePoint < 0xE000)
                            {
                                Debug.WriteLine($"warn: Unicode character name with disallowed code area: {name}");
                            }
                            else
                            {
                                unicode = codePoint;// uniStr.Append((char)codePoint);
                                break;
                            }
                        }
                        //unicode = uniStr.ToString();
                    }
                    catch (Exception nfe)
                    {
                        Debug.WriteLine($"warn: Not a number in Unicode character name: {name} {nfe}");
                    }
                }
                else if (name.StartsWith("u", StringComparison.Ordinal) && name.Length == 5)
                {
                    // test for an alternate Unicode name representation uXXXX
                    try
                    {
                        int codePoint = Convert.ToInt32(name.Substring(1), 16);
                        if (codePoint > 0xD7FF && codePoint < 0xE000)
                        {
                            Debug.WriteLine($"Unicode character name with disallowed code area: {name}");
                        }
                        else
                        {
                            unicode = codePoint;
                        }
                    }
                    catch (Exception nfe)
                    {
                        Debug.WriteLine($"warn: Not a number in Unicode character name: {name} {nfe}");
                    }
                }
                //else if (int.TryParse(name, out var number))
                //{
                //    if (number > 0xD7FF && number < 0xE000)
                //    {
                //        Debug.WriteLine($"Unicode character name with disallowed code area: {name}");
                //    }
                //    else
                //    {
                //        unicode = number;
                //    }
                //}
                if (unicode > 0)
                {
                    // null value not allowed in ConcurrentHashMap
                    uniNameToUnicodeCache[name] = unicode;
                }
            }
            return unicode != 0 ? unicode : (int?)null;
        }

        public string UnicodeToName(int unicode)
        {
            if (codeToName.TryGetValue(unicode, out var name))
            {
                return name;
            }
            return ".notdef";
        }

        /**
          <summary>Loads the glyph list mapping character names to character codes (unicode
          encoding).</summary>
        */
        private void Load(string fontName)
        {
            StreamReader glyphListStream = null;
            try
            {
                // Open the glyph list!
                /*
                  NOTE: The Adobe Glyph List [AGL:2.0] represents the reference name-to-unicode map
                  for consumer applications.
                */
                glyphListStream = new StreamReader(typeof(GlyphMapping).Assembly.GetManifestResourceStream(fontName));

                // Parsing the glyph list...
                string line;
                Regex linePattern = new Regex("^(\\w+);([A-F0-9]+)$");
                while ((line = glyphListStream.ReadLine()) != null)
                {
                    MatchCollection lineMatches = linePattern.Matches(line);
                    if (lineMatches.Count < 1)
                        continue;

                    Match lineMatch = lineMatches[0];

                    string name = lineMatch.Groups[1].Value;
                    int code = Int32.Parse(lineMatch.Groups[2].Value, NumberStyles.HexNumber);

                    // Associate the character name with its corresponding character code!
                    nameToCode[name] = code;
                    // reverse mapping
                    // PDFBOX-3884: take the various standard encodings as canonical, 
                    // e.g. tilde over ilde
                    bool forceOverride =
                          WinAnsiEncoding.Instance.Contains(name) ||
                          MacRomanEncoding.Instance.Contains(name) ||
                          MacExpertEncoding.Instance.Contains(name) ||
                          SymbolEncoding.Instance.Contains(name) ||
                          ZapfDingbatsEncoding.Instance.Contains(name);
                    if (!codeToName.ContainsKey(code) || forceOverride)
                    {
                        codeToName[code] = name;
                    }
                }
            }
            finally
            {
                if (glyphListStream != null)
                { glyphListStream.Close(); }
            }
        }


    }
}