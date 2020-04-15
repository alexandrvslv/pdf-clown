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
using System;

namespace PdfClown.Documents.Contents.Fonts.Type1
{

	/**
     * A lexical token in an Adobe Type 1 font.
     *
     * @see Type1Lexer
     *
     * @author John Hewson
     */
	public class Token
	{
		private string text;
		private byte[] data;
		private readonly TokenKind kind;

		/**
		 * Constructs a new Token object given its text and kind.
		 * @param text
		 * @param type
		 */
		public Token(string text, TokenKind type)
		{
			this.text = text;
			this.kind = type;
		}

		/**
         * Constructs a new Token object given its single-character text and kind.
         * @param character
         * @param type
         */
		public Token(char character, TokenKind type)
		{
			this.text = character.ToString();
			this.kind = type;
		}

		/**
         * Constructs a new Token object given its raw data and kind.
         * This is for CHARSTRING tokens only.
         * @param data
         * @param type
         */
		public Token(byte[] data, TokenKind type)
		{
			this.data = data;
			this.kind = type;
		}

		public string Text => text;

		public TokenKind Kind => kind;

		public int IntValue
		{
			get
			{
				// some fonts have reals where integers should be, so we tolerate it
				return (int)float.Parse(text);
			}
		}

		public float FloatValue
		{
			get
			{
				return float.Parse(text);
			}
		}

		public bool BooleanValue
		{
			get
			{
				return string.Equals(text, "true", StringComparison.OrdinalIgnoreCase);
			}
		}

		public byte[] Data => data;

		override public String ToString()
		{
			if (kind == TokenKind.CHARSTRING)
			{
				return "Token[kind=CHARSTRING, data=" + data.Length + " bytes]";
			}
			else
			{
				return "Token[kind=" + kind + ", text=" + text + "]";
			}
		}
	}

	/**
	 * All different types of tokens.
	 */
	public enum TokenKind
	{
		NONE,
		STRING,
		NAME,
		LITERAL,
		REAL,
		INTEGER,
		START_ARRAY,
		END_ARRAY,
		START_PROC,
		END_PROC,
		START_DICT,
		END_DICT,
		CHARSTRING
	}
}