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
using System.IO;
using System.Collections.Generic;
using PdfClown.Util.Collections.Generic;
using PdfClown.Util;

namespace PdfClown.Documents.Contents.Fonts.Type1
{
    /**
     * Parses an Adobe Type 1 (.pfb) font. It is used exclusively by Type1Font.
     *
     * The Type 1 font format is a free-text format which is somewhat difficult
     * to parse. This is made worse by the fact that many Type 1 font files do
     * not conform to the specification, especially those embedded in PDFs. This
     * parser therefore tries to be as forgiving as possible.
     *
     * @see "Adobe Type 1 Font Format, Adobe Systems (1999)"
     *
     * @author John Hewson
     */
    sealed class Type1Parser
    {
        // constants for encryption
        private static readonly int EEXEC_KEY = 55665;
        private static readonly int CHARSTRING_KEY = 4330;

        // state
        private Type1Lexer lexer;
        private Type1Font font;

        /**
		 * Parses a Type 1 font and returns a Type1Font class which represents it.
		 *
		 * @param segment1 Segment 1: ASCII
		 * @param segment2 Segment 2: Binary
		 * @throws IOException
		 */
        public Type1Font Parse(byte[] segment1, byte[] segment2)
        {
            font = new Type1Font(segment1, segment2);
            ParseASCII(segment1);
            if (segment2.Length > 0)
            {
                ParseBinary(segment2);
            }
            return font;
        }

        /**
		 * Parses the ASCII portion of a Type 1 font.
		 */
        private void ParseASCII(byte[] bytes)
        {
            if (bytes.Length == 0)
            {
                throw new ArgumentException("byte[] is empty");
            }

            // %!FontType1-1.0
            // %!PS-AdobeFont-1.0
            if (bytes.Length < 2 || (bytes[0] != '%' && bytes[1] != '!'))
            {
                throw new IOException("Invalid start of ASCII segment");
            }

            lexer = new Type1Lexer(bytes);

            // (corrupt?) synthetic font
            if (lexer.PeekToken().Text.Equals("FontDirectory", StringComparison.Ordinal))
            {
                Read(TokenKind.NAME, "FontDirectory");
                Read(TokenKind.LITERAL); // font name
                Read(TokenKind.NAME, "known");
                Read(TokenKind.START_PROC);
                ReadProc();
                Read(TokenKind.START_PROC);
                ReadProc();
                Read(TokenKind.NAME, "ifelse");
            }

            // font dict
            int Length = Read(TokenKind.INTEGER).IntValue;
            Read(TokenKind.NAME, "dict");
            // found in some TeX fonts
            ReadMaybe(TokenKind.NAME, "dup");
            // if present, the "currentdict" is not required
            Read(TokenKind.NAME, "begin");

            for (int i = 0; i < Length; i++)
            {
                // premature end
                Token token = lexer.PeekToken();
                if (token == null)
                {
                    break;
                }
                if (token.Kind == TokenKind.NAME &&
                    ("currentdict".Equals(token.Text, StringComparison.Ordinal)
                    || "end".Equals(token.Text, StringComparison.Ordinal)))
                {
                    break;
                }

                // key/value
                string key = Read(TokenKind.LITERAL).Text;
                switch (key)
                {
                    case "FontInfo":
                    case "Fontinfo":
                        ReadFontInfo(ReadSimpleDict());
                        break;
                    case "Metrics":
                        ReadSimpleDict();
                        break;
                    case "Encoding":
                        ReadEncoding();
                        break;
                    default:
                        ReadSimpleValue(key);
                        break;
                }
            }

            ReadMaybe(TokenKind.NAME, "currentdict");
            Read(TokenKind.NAME, "end");

            Read(TokenKind.NAME, "currentfile");
            Read(TokenKind.NAME, "eexec");
        }

        private void ReadSimpleValue(string key)
        {
            List<Token> value = ReadDictValue();

            switch (key)

            {
                case "FontName":
                    font.FontName = value[0].Text;
                    break;
                case "PaintType":
                    font.PaintType = value[0].IntValue;
                    break;
                case "FontType":
                    font.FontType = value[0].IntValue;
                    break;
                case "FontMatrix":
                    font.FontMatrix = ArrayToNumbers(value);
                    break;
                case "FontBBox":
                    font.FontBBox = ArrayToNumbers(value);
                    break;
                case "UniqueID":
                    font.UniqueID = value[0].IntValue;
                    break;
                case "StrokeWidth":
                    font.StrokeWidth = value[0].FloatValue;
                    break;
                case "FID":
                    font.FontID = value[0].Text;
                    break;
                default:
                    break;
            }
        }

        private void ReadEncoding()
        {
            if (lexer.PeekToken().Kind == TokenKind.NAME)
            {
                string name = lexer.NextToken().Text;

                if (name.Equals("StandardEncoding", StringComparison.Ordinal))
                {
                    font.Encoding = StandardEncoding.Instance;
                }
                else
                {
                    throw new IOException("Unknown encoding: " + name);
                }
                ReadMaybe(TokenKind.NAME, "readonly");
                Read(TokenKind.NAME, "def");
            }
            else
            {
                var intValue = Read(TokenKind.INTEGER).IntValue;
                ReadMaybe(TokenKind.NAME, "array");

                // 0 1 255 {1 index exch /.notdef put } for
                // we have to check "readonly" and "def" too
                // as some fonts don't provide any dup-values, see PDFBOX-2134
                while (!(lexer.PeekToken().Kind == TokenKind.NAME &&
                        (lexer.PeekToken().Text.Equals("dup", StringComparison.Ordinal) ||
                        lexer.PeekToken().Text.Equals("readonly", StringComparison.Ordinal) ||
                        lexer.PeekToken().Text.Equals("def", StringComparison.Ordinal))))
                {
                    lexer.NextToken();
                }

                Dictionary<int, string> codeToName = new Dictionary<int, string>();
                while (lexer.PeekToken().Kind == TokenKind.NAME &&
                        lexer.PeekToken().Text.Equals("dup", StringComparison.Ordinal))
                {
                    Read(TokenKind.NAME, "dup");
                    int code = Read(TokenKind.INTEGER).IntValue;
                    string name = Read(TokenKind.LITERAL).Text;
                    Read(TokenKind.NAME, "put");
                    codeToName.Add(code, name);
                }
                font.Encoding = new Encoding(codeToName);
                ReadMaybe(TokenKind.NAME, "readonly");
                Read(TokenKind.NAME, "def");
            }
        }

        /**
		 * Extracts values from an array as numbers.
		 */
        private List<float> ArrayToNumbers(List<Token> value)
        {
            List<float> numbers = new List<float>();
            for (int i = 1, size = value.Count - 1; i < size; i++)
            {
                Token token = value[i];
                if (token.Kind == TokenKind.REAL)
                {
                    numbers.Add(token.FloatValue);
                }
                else if (token.Kind == TokenKind.INTEGER)
                {
                    numbers.Add(token.IntValue);
                }
                else
                {
                    throw new IOException("Expected INTEGER or REAL but got " + token.Kind);
                }
            }
            return numbers;
        }

        /**
		 * Extracts values from the /FontInfo dictionary.
		 */
        private void ReadFontInfo(Dictionary<string, List<Token>> fontInfo)
        {
            foreach (KeyValuePair<string, List<Token>> entry in fontInfo)
            {
                string key = entry.Key;
                List<Token> value = entry.Value;

                switch (key)
                {
                    case "version":
                        font.Version = value[0].Text;
                        break;
                    case "Notice":
                        font.Notice = value[0].Text;
                        break;
                    case "FullName":
                        font.FullName = value[0].Text;
                        break;
                    case "FamilyName":
                        font.FamilyName = value[0].Text;
                        break;
                    case "Weight":
                        font.Weight = value[0].Text;
                        break;
                    case "ItalicAngle":
                        font.ItalicAngle = value[0].FloatValue;
                        break;
                    case "isFixedPitch":
                        font.FixedPitch = value[0].BooleanValue;
                        break;
                    case "UnderlinePosition":
                        font.UnderlinePosition = value[0].FloatValue;
                        break;
                    case "UnderlineThickness":
                        font.UnderlineThickness = value[0].FloatValue;
                        break;
                    default:
                        break;
                }
            }
        }

        /**
		 * Reads a dictionary whose values are simple, i.e., do not contain
		 * nested dictionaries.
		 */
        private Dictionary<string, List<Token>> ReadSimpleDict()
        {
            Dictionary<string, List<Token>> dict = new Dictionary<string, List<Token>>(StringComparer.Ordinal);

            int Length = Read(TokenKind.INTEGER).IntValue;
            Read(TokenKind.NAME, "dict");
            ReadMaybe(TokenKind.NAME, "dup");
            Read(TokenKind.NAME, "begin");

            for (int i = 0; i < Length; i++)
            {
                if (lexer.PeekToken() == null)
                {
                    break;
                }
                if (lexer.PeekToken().Kind == TokenKind.NAME &&
                   !lexer.PeekToken().Text.Equals("end", StringComparison.Ordinal))
                {
                    Read(TokenKind.NAME);
                }
                // premature end
                if (lexer.PeekToken() == null)
                {
                    break;
                }
                if (lexer.PeekToken().Kind == TokenKind.NAME &&
                    lexer.PeekToken().Text.Equals("end", StringComparison.Ordinal))
                {
                    break;
                }

                // simple value
                string key = Read(TokenKind.LITERAL).Text;
                List<Token> value = ReadDictValue();
                dict.Add(key, value);
            }

            Read(TokenKind.NAME, "end");
            ReadMaybe(TokenKind.NAME, "readonly");
            Read(TokenKind.NAME, "def");

            return dict;
        }

        /**
		 * Reads a simple value from a dictionary.
		 */
        private List<Token> ReadDictValue()
        {
            List<Token> value = ReadValue();
            ReadDef();
            return value;
        }

        /**
		 * Reads a simple value. This is either a number, a string,
		 * a name, a literal name, an array, a procedure, or a charstring.
		 * This method does not support reading nested dictionaries unless they're empty.
		 */
        private List<Token> ReadValue()
        {
            List<Token> value = new List<Token>();
            Token token = lexer.NextToken();
            if (lexer.PeekToken() == null)
            {
                return value;
            }
            value.Add(token);

            if (token.Kind == TokenKind.START_ARRAY)
            {
                int openArray = 1;
                while (true)
                {
                    if (lexer.PeekToken() == null)
                    {
                        return value;
                    }
                    if (lexer.PeekToken().Kind == TokenKind.START_ARRAY)
                    {
                        openArray++;
                    }

                    token = lexer.NextToken();
                    value.Add(token);

                    if (token.Kind == TokenKind.END_ARRAY)
                    {
                        openArray--;
                        if (openArray == 0)
                        {
                            break;
                        }
                    }
                }
            }
            else if (token.Kind == TokenKind.START_PROC)
            {
                value.AddRange(ReadProc());
            }
            else if (token.Kind == TokenKind.START_DICT)
            {
                // skip "/GlyphNames2HostCode << >> def"
                Read(TokenKind.END_DICT);
                return value;
            }

            ReadPostScriptWrapper(value);
            return value;
        }

        private void ReadPostScriptWrapper(List<Token> value)
        {
            // postscript wrapper (not in the Type 1 spec)
            if (lexer.PeekToken().Text.Equals("systemdict", StringComparison.Ordinal))
            {
                Read(TokenKind.NAME, "systemdict");
                Read(TokenKind.LITERAL, "internaldict");
                Read(TokenKind.NAME, "known");

                Read(TokenKind.START_PROC);
                ReadProc();

                Read(TokenKind.START_PROC);
                ReadProc();

                Read(TokenKind.NAME, "ifelse");

                // replace value
                Read(TokenKind.START_PROC);
                Read(TokenKind.NAME, "pop");
                value.Clear();
                value.AddRange(ReadValue());
                Read(TokenKind.END_PROC);

                Read(TokenKind.NAME, "if");
            }
        }

        /**
		 * Reads a procedure.
		 */
        private List<Token> ReadProc()
        {
            List<Token> value = new List<Token>();

            int openProc = 1;
            while (true)
            {
                if (lexer.PeekToken().Kind == TokenKind.START_PROC)
                {
                    openProc++;
                }

                Token token = lexer.NextToken();
                value.Add(token);

                if (token.Kind == TokenKind.END_PROC)
                {
                    openProc--;
                    if (openProc == 0)
                    {
                        break;
                    }
                }
            }
            Token executeonly = ReadMaybe(TokenKind.NAME, "executeonly");
            if (executeonly != null)
            {
                value.Add(executeonly);
            }

            return value;
        }

        /**
		 * Parses the binary portion of a Type 1 font.
		 */
        private void ParseBinary(byte[] bytes)
        {
            byte[] decrypted;
            // Sometimes, fonts use the hex format, so this needs to be converted before decryption
            if (IsBinary(bytes))
            {
                decrypted = Decrypt(bytes, EEXEC_KEY, 4);
            }
            else
            {
                decrypted = Decrypt(hexToBinary(bytes), EEXEC_KEY, 4);
            }
            lexer = new Type1Lexer(decrypted);

            // find /Private dict
            Token peekToken = lexer.PeekToken();
            while (peekToken != null && !peekToken.Text.Equals("Private", StringComparison.Ordinal))
            {
                // for a more thorough validation, the presence of "begin" before Private
                // determines how code before and following charstrings should look
                // it is not currently checked anyway
                lexer.NextToken();
                peekToken = lexer.PeekToken();
            }
            if (peekToken == null)
            {
                throw new IOException("/Private token not found");
            }

            // Private dict
            Read(TokenKind.LITERAL, "Private");
            int Length = Read(TokenKind.INTEGER).IntValue;
            Read(TokenKind.NAME, "dict");
            // actually could also be "/Private 10 dict def Private begin"
            // instead of the "dup"
            ReadMaybe(TokenKind.NAME, "dup");
            Read(TokenKind.NAME, "begin");

            int lenIV = 4; // number of random bytes at start of charstring

            for (int i = 0; i < Length; i++)
            {
                // premature end
                if (lexer.PeekToken() == null || lexer.PeekToken().Kind != TokenKind.LITERAL)
                {
                    break;
                }

                // key/value
                string key = Read(TokenKind.LITERAL).Text;

                switch (key)
                {
                    case "Subrs":
                        ReadSubrs(lenIV);
                        break;
                    case "OtherSubrs":
                        ReadOtherSubrs();
                        break;
                    case "lenIV":
                        lenIV = ReadDictValue()[0].IntValue;
                        break;
                    case "ND":
                        Read(TokenKind.START_PROC);
                        // the access restrictions are not mandatory
                        ReadMaybe(TokenKind.NAME, "noaccess");
                        Read(TokenKind.NAME, "def");
                        Read(TokenKind.END_PROC);
                        ReadMaybe(TokenKind.NAME, "executeonly");
                        Read(TokenKind.NAME, "def");
                        break;
                    case "NP":
                        Read(TokenKind.START_PROC);
                        ReadMaybe(TokenKind.NAME, "noaccess");
                        Read(TokenKind.NAME);
                        Read(TokenKind.END_PROC);
                        ReadMaybe(TokenKind.NAME, "executeonly");
                        Read(TokenKind.NAME, "def");
                        break;
                    case "RD":
                        // /RD {string currentfile exch readstring pop} bind executeonly def
                        Read(TokenKind.START_PROC);
                        ReadProc();
                        ReadMaybe(TokenKind.NAME, "bind");
                        ReadMaybe(TokenKind.NAME, "executeonly");
                        Read(TokenKind.NAME, "def");
                        break;
                    default:
                        ReadPrivate(key, ReadDictValue());
                        break;
                }
            }

            // some fonts have "2 index" here, others have "end noaccess put"
            // sometimes followed by "put". Either way, we just skip until
            // the /CharStrings dict is found
            while (!(lexer.PeekToken().Kind == TokenKind.LITERAL &&
                     lexer.PeekToken().Text.Equals("CharStrings", StringComparison.Ordinal)))
            {
                lexer.NextToken();
            }

            // CharStrings dict
            Read(TokenKind.LITERAL, "CharStrings");
            ReadCharStrings(lenIV);
        }

        /**
		 * Extracts values from the /Private dictionary.
		 */
        private void ReadPrivate(string key, List<Token> value)
        {
            switch (key)

            {
                case "BlueValues":
                    font.BlueValues = ArrayToNumbers(value);
                    break;
                case "OtherBlues":
                    font.OtherBlues = ArrayToNumbers(value);
                    break;
                case "FamilyBlues":
                    font.FamilyBlues = ArrayToNumbers(value);
                    break;
                case "FamilyOtherBlues":
                    font.FamilyOtherBlues = ArrayToNumbers(value);
                    break;
                case "BlueScale":
                    font.BlueScale = value[0].FloatValue;
                    break;
                case "BlueShift":
                    font.BlueShift = value[0].IntValue;
                    break;
                case "BlueFuzz":
                    font.BlueFuzz = value[0].IntValue;
                    break;
                case "StdHW":
                    font.StdHW = ArrayToNumbers(value);
                    break;
                case "StdVW":
                    font.StdVW = ArrayToNumbers(value);
                    break;
                case "StemSnapH":
                    font.StemSnapH = ArrayToNumbers(value);
                    break;
                case "StemSnapV":
                    font.StemSnapV = ArrayToNumbers(value);
                    break;
                case "ForceBold":
                    font.IsForceBold = value[0].BooleanValue;
                    break;
                case "LanguageGroup":
                    font.LanguageGroup = value[0].IntValue;
                    break;
                default:
                    break;
            }
        }

        /**
		 * Reads the /Subrs array.
		 * @param lenIV The number of random bytes used in charstring encryption.
		 */
        private void ReadSubrs(int lenIV)
        {
            // allocate size (array indexes may not be in-order)
            int Length = Read(TokenKind.INTEGER).IntValue;
            for (int i = 0; i < Length; i++)

            {
                font.SubrsArray.Add(null);
            }
            Read(TokenKind.NAME, "array");

            for (int i = 0; i < Length; i++)

            {
                // premature end
                if (lexer.PeekToken() == null)
                {
                    break;
                }
                if (!(lexer.PeekToken().Kind == TokenKind.NAME &&
                      lexer.PeekToken().Text.Equals("dup", StringComparison.Ordinal)))
                {
                    break;
                }

                Read(TokenKind.NAME, "dup");
                Token index = Read(TokenKind.INTEGER);
                Read(TokenKind.INTEGER);

                // RD
                Token charstring = Read(TokenKind.CHARSTRING);
                font.SubrsArray.Insert(index.IntValue, Decrypt(charstring.Data, CHARSTRING_KEY, lenIV));
                ReadPut();
            }
            ReadDef();
        }

        // OtherSubrs are embedded PostScript procedures which we can safely ignore
        private void ReadOtherSubrs()
        {
            if (lexer.PeekToken().Kind == TokenKind.START_ARRAY)
            {
                ReadValue();
                ReadDef();
            }
            else

            {
                int Length = Read(TokenKind.INTEGER).IntValue;
                Read(TokenKind.NAME, "array");

                for (int i = 0; i < Length; i++)
                {
                    Read(TokenKind.NAME, "dup");
                    Read(TokenKind.INTEGER); // index
                    ReadValue(); // PostScript
                    ReadPut();
                }
                ReadDef();
            }
        }

        /**
		 * Reads the /CharStrings dictionary.
		 * @param lenIV The number of random bytes used in charstring encryption.
		 */
        private void ReadCharStrings(int lenIV)
        {
            int Length = Read(TokenKind.INTEGER).IntValue;
            Read(TokenKind.NAME, "dict");
            // could actually be a sequence ending in "CharStrings begin", too
            // instead of the "dup begin"
            Read(TokenKind.NAME, "dup");
            Read(TokenKind.NAME, "begin");

            for (int i = 0; i < Length; i++)

            {
                // premature end
                if (lexer.PeekToken() == null)
                {
                    break;
                }
                if (lexer.PeekToken().Kind == TokenKind.NAME &&
                    lexer.PeekToken().Text.Equals("end", StringComparison.Ordinal))
                {
                    break;
                }
                // key/value
                string name = Read(TokenKind.LITERAL).Text;

                // RD
                Read(TokenKind.INTEGER);
                Token charstring = Read(TokenKind.CHARSTRING);
                font.CharStringsDict.Add(name, Decrypt(charstring.Data, CHARSTRING_KEY, lenIV));
                ReadDef();
            }

            // some fonts have one "end", others two
            Read(TokenKind.NAME, "end");
            // since checking ends here, this does not matter ....
            // more thorough checking would see whether there is "begin" before /Private
            // and expect a "def" somewhere, otherwise a "put"
        }

        /**
		 * Reads the sequence "noaccess def" or equivalent.
		 */
        private void ReadDef()
        {
            ReadMaybe(TokenKind.NAME, "readonly");
            ReadMaybe(TokenKind.NAME, "noaccess"); // allows "noaccess ND" (not in the Type 1 spec)

            Token token = Read(TokenKind.NAME);
            switch (token.Text)
            {
                case "ND":
                case "|-":
                    return;
                case "noaccess":
                    token = Read(TokenKind.NAME);
                    break;
                default:
                    break;
            }

            if (token.Text.Equals("def", StringComparison.Ordinal))
            {
                return;
            }
            throw new IOException("Found " + token + " but expected ND");
        }

        /**
		 * Reads the sequence "noaccess put" or equivalent.
		 */
        private void ReadPut()
        {
            ReadMaybe(TokenKind.NAME, "readonly");

            Token token = Read(TokenKind.NAME);
            switch (token.Text)
            {
                case "NP":
                case "|":
                    return;
                case "noaccess":
                    token = Read(TokenKind.NAME);
                    break;
                default:
                    break;
            }

            if (token.Text.Equals("put", StringComparison.Ordinal))
            {
                return;
            }
            throw new IOException("Found " + token + " but expected NP");
        }

        /**
		 * Reads the next token and throws an error if it is not of the given kind.
		 */
        private Token Read(TokenKind kind)
        {
            Token token = lexer.NextToken();
            if (token == null || token.Kind != kind)
            {
                throw new IOException("Found " + token + " but expected " + kind);
            }
            return token;
        }

        /**
		 * Reads the next token and throws an error if it is not of the given kind
		 * and does not have the given value.
		 */
        private void Read(TokenKind kind, string name)
        {
            Token token = Read(kind);
            if (!token.Text.Equals(name, StringComparison.Ordinal))
            {
                throw new IOException("Found " + token + " but expected " + name);
            }
        }

        /**
		 * Reads the next token if and only if it is of the given kind and
		 * has the given value.
		 */
        private Token ReadMaybe(TokenKind kind, string name)
        {
            Token token = lexer.PeekToken();
            if (token != null && token.Kind == kind && token.Text.Equals(name, StringComparison.Ordinal))

            {
                return lexer.NextToken();
            }
            return null;
        }

        /**
		 * Type 1 Decryption (eexec, charstring).
		 *
		 * @param cipherBytes cipher text
		 * @param r key
		 * @param n number of random bytes (lenIV)
		 * @return plain text
		 */
        private byte[] Decrypt(byte[] cipherBytes, int r, int n)
        {
            // lenIV of -1 means no encryption (not documented)
            if (n == -1)
            {
                return cipherBytes;
            }
            // empty charstrings and charstrings of insufficient Length
            if (cipherBytes.Length == 0 || n > cipherBytes.Length)
            {
                return new byte[] { };
            }
            // decrypt
            int c1 = 52845;
            int c2 = 22719;
            byte[] plainBytes = new byte[cipherBytes.Length - n];
            for (int i = 0; i < cipherBytes.Length; i++)
            {
                int cipher = cipherBytes[i] & 0xFF;
                int plain = cipher ^ r >> 8;
                if (i >= n)
                {
                    plainBytes[i - n] = (byte)plain;
                }
                r = (cipher + r) * c1 + c2 & 0xffff;
            }
            return plainBytes;
        }

        // Check whether binary or hex encoded. See Adobe Type 1 Font Format specification
        // 7.2 eexec encryption
        private bool IsBinary(byte[] bytes)
        {
            if (bytes.Length < 4)
            {
                return true;
            }
            // "At least one of the first 4 ciphertext bytes must not be one of
            // the ASCII hexadecimal character codes (a code for 0-9, A-F, or a-f)."
            for (int i = 0; i < 4; ++i)
            {
                byte by = bytes[i];
                if (by != 0x0a && by != 0x0d && by != 0x20 && by != '\t' &&
                        ((char)by).Digit(16) == -1)
                {
                    return true;
                }
            }
            return false;
        }

        private byte[] hexToBinary(byte[] bytes)
        {
            // calculate needed Length
            int len = 0;
            foreach (byte by in bytes)
            {
                if (((char)by).Digit(16) != -1)
                {
                    ++len;
                }
            }
            byte[] res = new byte[len / 2];
            int r = 0;
            int prev = -1;
            foreach (byte by in bytes)
            {
                int digit = ((char)by).Digit(16);
                if (digit != -1)
                {
                    if (prev == -1)
                    {
                        prev = digit;
                    }
                    else
                    {
                        res[r++] = (byte)(prev * 16 + digit);
                        prev = -1;
                    }
                }
            }
            return res;
        }
    }

    public static class CharExtension
    {
        public static int Digit(this char value, int radix)
        {
            if ((radix <= 0) || (radix > 36))
                return -1; // Or throw exception

            if (radix <= 10)
                if (value >= '0' && value < '0' + radix)
                    return value - '0';
                else
                    return -1;
            else if (value >= '0' && value <= '9')
                return value - '0';
            else if (value >= 'a' && value < 'a' + radix - 10)
                return value - 'a' + 10;
            else if (value >= 'A' && value < 'A' + radix - 10)
                return value - 'A' + 10;

            return -1;
        }
    }
}