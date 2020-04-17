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
namespace PdfClown.Documents.Contents.Fonts.TTF{

using System.IO;

import java.io.InputStreamReader;
import java.io.LineNumberReader;



import java.util.Dictionary.Entry;
import java.util.StringTokenizer;
import java.util.TreeMap;

using System.Diagnostics;


/**
 * A class for mapping Unicode codepoints to OpenType script tags
 *
 * @author Aaron Madlon-Kay
 *
 * @see <a href="https://www.microsoft.com/typography/otspec/scripttags.htm">Microsoft Typography:
 * Script Tags</a>
 * @see <a href="https://www.unicode.org/reports/tr24/">Unicode Script Property</a>
 */
public sealed class OpenTypeScript
{
    //private static readonly Log LOG = LogFactory.getLog(OpenTypeScript.class);

    public static readonly string INHERITED = "Inherited";
    public static readonly string UNKNOWN = "Unknown";
    public static readonly string TAG_DEFAULT = "DFLT";

    /**
     * A map associating Unicode scripts with one or more OpenType script tags. Script tags are not
     * necessarily the same as Unicode scripts. A single Unicode script may correspond to multiple
     * tags, especially when there has been a revision to the latter (e.g. Bengali -> [bng2, beng]).
     * When there are multiple tags, they are ordered from newest to oldest.
     *
     * @see <a href="https://www.microsoft.com/typography/otspec/scripttags.htm">Microsoft
     * Typography: Script Tags</a>
     */
    private static readonly Dictionary<string, string[]> UNICODE_SCRIPT_TO_OPENTYPE_TAG_MAP;

    static
    {
        object[][] table = 
        {
            {"Adlam", new string[] { "adlm" }},
            {"Ahom", new string[] { "ahom" }},
            {"Anatolian_Hieroglyphs", new string[] { "hluw" }},
            {"Arabic", new string[] { "arab" }},
            {"Armenian", new string[] { "armn" }},
            {"Avestan", new string[] { "avst" }},
            {"Balinese", new string[] { "bali" }},
            {"Bamum", new string[] { "bamu" }},
            {"Bassa_Vah", new string[] { "bass" }},
            {"Batak", new string[] { "batk" }},
            {"Bengali", new string[] { "bng2", "beng" }},
            {"Bhaiksuki", new string[] { "bhks" }},
            {"Bopomofo", new string[] { "bopo" }},
            {"Brahmi", new string[] { "brah" }},
            {"Braille", new string[] { "brai" }},
            {"Buginese", new string[] { "bugi" }},
            {"Buhid", new string[] { "buhd" }},
            // Byzantine Music: byzm
            {"Canadian_Aboriginal", new string[] { "cans" }},
            {"Carian", new string[] { "cari" }},
            {"Caucasian_Albanian", new string[] { "aghb" }},
            {"Chakma", new string[] { "cakm" }},
            {"Cham", new string[] { "cham" }},
            {"Cherokee", new string[] { "cher" }},
            {"Common", new string[] { TAG_DEFAULT }}, // "Default" in OpenType
            {"Coptic", new string[] { "copt" }},
            {"Cuneiform", new string[] { "xsux" }}, // "Sumero-Akkadian Cuneiform" in OpenType
            {"Cypriot", new string[] { "cprt" }},
            {"Cyrillic", new string[] { "cyrl" }},
            {"Deseret", new string[] { "dsrt" }},
            {"Devanagari", new string[] { "dev2", "deva" }},
            {"Duployan", new string[] { "dupl" }},
            {"Egyptian_Hieroglyphs", new string[] { "egyp" }},
            {"Elbasan", new string[] { "elba" }},
            {"Ethiopic", new string[] { "ethi" }},
            {"Georgian", new string[] { "geor" }},
            {"Glagolitic", new string[] { "glag" }},
            {"Gothic", new string[] { "goth" }},
            {"Grantha", new string[] { "gran" }},
            {"Greek", new string[] { "grek" }},
            {"Gujarati", new string[] { "gjr2", "gujr" }},
            {"Gurmukhi", new string[] { "gur2", "guru" }},
            {"Han", new string[] { "hani" }}, // "CJK Ideographic" in OpenType
            {"Hangul", new string[] { "hang" }},
            // Hangul Jamo: jamo
            {"Hanunoo", new string[] { "hano" }},
            {"Hatran", new string[] { "hatr" }},
            {"Hebrew", new string[] { "hebr" }},
            {"Hiragana", new string[] { "kana" }},
            {"Imperial_Aramaic", new string[] { "armi" }},
            {INHERITED, new string[] { INHERITED }},
            {"Inscriptional_Pahlavi", new string[] { "phli" }},
            {"Inscriptional_Parthian", new string[] { "prti" }},
            {"Javanese", new string[] { "java" }},
            {"Kaithi", new string[] { "kthi" }},
            {"Kannada", new string[] { "knd2", "knda" }},
            {"Katakana", new string[] { "kana" }},
            {"Kayah_Li", new string[] { "kali" }},
            {"Kharoshthi", new string[] { "khar" }},
            {"Khmer", new string[] { "khmr" }},
            {"Khojki", new string[] { "khoj" }},
            {"Khudawadi", new string[] { "sind" }},
            {"Lao", new string[] { "lao " }},
            {"Latin", new string[] { "latn" }},
            {"Lepcha", new string[] { "lepc" }},
            {"Limbu", new string[] { "limb" }},
            {"Linear_A", new string[] { "lina" }},
            {"Linear_B", new string[] { "linb" }},
            {"Lisu", new string[] { "lisu" }},
            {"Lycian", new string[] { "lyci" }},
            {"Lydian", new string[] { "lydi" }},
            {"Mahajani", new string[] { "mahj" }},
            {"Malayalam", new string[] { "mlm2", "mlym" }},
            {"Mandaic", new string[] { "mand" }},
            {"Manichaean", new string[] { "mani" }},
            {"Marchen", new string[] { "marc" }},
            // Mathematical Alphanumeric Symbols: math
            {"Meetei_Mayek", new string[] { "mtei" }},
            {"Mende_Kikakui", new string[] { "mend" }},
            {"Meroitic_Cursive", new string[] { "merc" }},
            {"Meroitic_Hieroglyphs", new string[] { "mero" }},
            {"Miao", new string[] { "plrd" }},
            {"Modi", new string[] { "modi" }},
            {"Mongolian", new string[] { "mong" }},
            {"Mro", new string[] { "mroo" }},
            {"Multani", new string[] { "mult" }},
            // Musical Symbols: musc
            {"Myanmar", new string[] { "mym2", "mymr" }},
            {"Nabataean", new string[] { "nbat" }},
            {"Newa", new string[] { "newa" }},
            {"New_Tai_Lue", new string[] { "talu" }},
            {"Nko", new string[] { "nko " }},
            {"Ogham", new string[] { "ogam" }},
            {"Ol_Chiki", new string[] { "olck" }},
            {"Old_Italic", new string[] { "ital" }},
            {"Old_Hungarian", new string[] { "hung" }},
            {"Old_North_Arabian", new string[] { "narb" }},
            {"Old_Permic", new string[] { "perm" }},
            {"Old_Persian", new string[] { "xpeo" }},
            {"Old_South_Arabian", new string[] { "sarb" }},
            {"Old_Turkic", new string[] { "orkh" }},
            {"Oriya", new string[] { "ory2", "orya" }}, // "Odia (formerly Oriya)" in OpenType
            {"Osage", new string[] { "osge" }},
            {"Osmanya", new string[] { "osma" }},
            {"Pahawh_Hmong", new string[] { "hmng" }},
            {"Palmyrene", new string[] { "palm" }},
            {"Pau_Cin_Hau", new string[] { "pauc" }},
            {"Phags_Pa", new string[] { "phag" }},
            {"Phoenician", new string[] { "phnx" }},
            {"Psalter_Pahlavi", new string[] { "phlp" }},
            {"Rejang", new string[] { "rjng" }},
            {"Runic", new string[] { "runr" }},
            {"Samaritan", new string[] { "samr" }},
            {"Saurashtra", new string[] { "saur" }},
            {"Sharada", new string[] { "shrd" }},
            {"Shavian", new string[] { "shaw" }},
            {"Siddham", new string[] { "sidd" }},
            {"SignWriting", new string[] { "sgnw" }},
            {"Sinhala", new string[] { "sinh" }},
            {"Sora_Sompeng", new string[] { "sora" }},
            {"Sundanese", new string[] { "sund" }},
            {"Syloti_Nagri", new string[] { "sylo" }},
            {"Syriac", new string[] { "syrc" }},
            {"Tagalog", new string[] { "tglg" }},
            {"Tagbanwa", new string[] { "tagb" }},
            {"Tai_Le", new string[] { "tale" }},
            {"Tai_Tham", new string[] { "lana" }},
            {"Tai_Viet", new string[] { "tavt" }},
            {"Takri", new string[] { "takr" }},
            {"Tamil", new string[] { "tml2", "taml" }},
            {"Tangut", new string[] { "tang" }},
            {"Telugu", new string[] { "tel2", "telu" }},
            {"Thaana", new string[] { "thaa" }},
            {"Thai", new string[] { "thai" }},
            {"Tibetan", new string[] { "tibt" }},
            {"Tifinagh", new string[] { "tfng" }},
            {"Tirhuta", new string[] { "tirh" }},
            {"Ugaritic", new string[] { "ugar" }},
            {UNKNOWN, new string[] { TAG_DEFAULT }},
            {"Vai", new string[] { "vai " }},
            {"Warang_Citi", new string[] { "wara" }},
            {"Yi", new string[] { "yi  " }}
        };
        UNICODE_SCRIPT_TO_OPENTYPE_TAG_MAP = new Dictionary<>(table.Length);
        for (object[] array : table)
        {
            UNICODE_SCRIPT_TO_OPENTYPE_TAG_MAP[(string) array[0], (string[]) array[1]);
        }
    }

    private static int[] unicodeRangeStarts;
    private static string[] unicodeRangeScripts;

    static OpenTypeScript()
    {
        string path = "/org/apache/fontbox/unicode/Scripts.txt";
        try (Bytes.Buffer input = OpenTypeScript.class.getResourceAsStream(path))
        {
            if (input != null)
            {
                parseScriptsFile(input);
            }
            else
            {
                Debug.WriteLine("warning: Could not find '" + path + "', mirroring char map will be empty: ");
            }
        }
        catch (IOException e)
        {
            Debug.WriteLine("warning: Could not parse Scripts.txt, mirroring char map will be empty: "
                    + e.getMessage(), e);
        }
    }

    private OpenTypeScript()
    {
    }

    private static void parseScriptsFile(Bytes.Buffer inputStream) 
    {
        Dictionary<int[], string> unicodeRanges = new TreeMap<>((o1, o2) -> int.compare(o1[0], o2[0]));
        try (LineNumberReader rd = new LineNumberReader(new InputStreamReader(inputStream)))
        {
            int[] lastRange = { int.MinValue, int.MinValue };
            string lastScript = null;
            do
            {
                string s = rd.readLine();
                if (s == null)
                {
                    break;
                }
                
                // ignore comments
                int comment = s.indexOf('#');
                if (comment != -1)
                {
                    s = s.substring(0, comment);
                }
                
                if (s.Length < 2)
                {
                    continue;
                }
                
                StringTokenizer st = new StringTokenizer(s, ";");
                int nFields = st.countTokens();
                if (nFields < 2)
                {
                    continue;
                }
                string characters = st.nextToken().trim();
                string script = st.nextToken().trim();
                int[] range = new int[2];
                int rangeDelim = characters.indexOf("..");
                if (rangeDelim == -1)
                {
                    range[0] = range[1] = int.parseInt(characters, 16);
                }
                else
                {
                    range[0] = int.parseInt(characters.substring(0, rangeDelim), 16);
                    range[1] = int.parseInt(characters.substring(rangeDelim + 2), 16);
                }
                if (range[0] == lastRange[1] + 1 && script.equals(lastScript))
                {
                    // Combine with previous range
                    lastRange[1] = range[1];
                }
                else
                {
                    unicodeRanges[range, script);
                    lastRange = range;
                    lastScript = script;
                }
            }
            while (true);
        }

        unicodeRangeStarts = new int[unicodeRanges.Count];
        unicodeRangeScripts = new string[unicodeRanges.Count];
        int i = 0;
        for (Entry<int[], string> e : unicodeRanges.entrySet())
        {
            unicodeRangeStarts[i] = e.Key[0];
            unicodeRangeScripts[i] = e.Value;
            i++;
        }
    }

    /**
     * Obtain the Unicode script associated with the given Unicode codepoint.
     *
     * @param codePoint
     * @return A Unicode script string, or {@code #UNKNOWN} if unknown
     */
    private static string getUnicodeScript(int codePoint)
    {
        ensureValidCodePoint(codePoint);
        int type = Character.getType(codePoint);
        if (type == Character.UNASSIGNED)
        {
            return UNKNOWN;
        }
        int scriptIndex = Array.binarySearch(unicodeRangeStarts, codePoint);
        if (scriptIndex < 0)
        {
            scriptIndex = -scriptIndex - 2;
        }
        return unicodeRangeScripts[scriptIndex];
    }

    /**
     * Obtain the OpenType script tags associated with the given Unicode codepoint.
     *
     * The result may contain the special value {@code #INHERITED}, which indicates that the
     * codepoint's script can only be determined by its context.
     *
     * Unknown codepoints are mapped to {@code #TAG_DEFAULT}.
     *
     * @param codePoint
     * @return An array of four-char script tags
     */
    public static string[] getScriptTags(int codePoint)
    {
        ensureValidCodePoint(codePoint);
        string unicode = getUnicodeScript(codePoint);
        return UNICODE_SCRIPT_TO_OPENTYPE_TAG_MAP.get(unicode);
    }

    private static void ensureValidCodePoint(int codePoint)
    {
        if (codePoint < Character.MIN_CODE_POINT || codePoint > Character.MAX_CODE_POINT)
        {
            throw new IllegalArgumentException("Invalid codepoint: " + codePoint);
        }
    }
}
