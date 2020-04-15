/*
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 */

using System;
using System.Diagnostics;
using System.Text;
using System.IO;

namespace PdfClown.Documents.Contents.Fonts.Type1
{

	/**
     * Lexer for the ASCII portions of an Adobe Type 1 font.
     *
     * @see Type1Parser
     *
     * The PostScript language, of which Type 1 fonts are a subset, has a
     * somewhat awkward lexical structure. It is neither regular nor
     * context-free, and the execution of the program can modify the
     * the behaviour of the lexer/parser.
     *
     * Nevertheless, this class represents an attempt to artificially separate
     * the PostScript parsing process into separate lexing and parsing phases
     * in order to reduce the complexity of the parsing phase.
     *
     * @see "PostScript Language Reference 3rd ed, Adobe Systems (1999)"
     *
     * @author John Hewson
     */
	class Type1Lexer
	{

		private readonly Bytes.Buffer buffer;
		private Token aheadToken;
		private int openParens = 0;

		/**
		 * Constructs a new Type1Lexer given a header-less .pfb segment.
		 * @param bytes Header-less .pfb segment
		 * @throws IOException
		 */
		public Type1Lexer(byte[] bytes)
		{
			buffer = new Bytes.Buffer(bytes);
			aheadToken = ReadToken(null);
		}

		/**
		 * Returns the next token and consumes it.
		 * @return The next token.
		 */
		public Token NextToken()
		{
			Token curToken = aheadToken;
			//System.out.println(curToken); // for debugging
			aheadToken = ReadToken(curToken);
			return curToken;
		}

		/**
		 * Returns the next token without consuming it.
		 * @return The next token
		 */
		public Token PeekToken()
		{
			return aheadToken;
		}

		/**
		 * Reads an ASCII char from the buffer.
		 */
		private char GetChar()
		{
			return (char)buffer.ReadByte();
		}

		/**
		 * Reads a single token.
		 * @param prevToken the previous token
		 */
		private Token ReadToken(Token prevToken)
		{
			bool skip;
			do
			{
				skip = false;
				while (buffer.Position < buffer.Length)
				{
					char c = GetChar();

					// delimiters
					if (c == '%')
					{
						// comment
						ReadComment();
					}
					else if (c == '(')
					{
						return ReadString();
					}
					else if (c == ')')
					{
						// not allowed outside a string context
						throw new IOException("unexpected closing parenthesis");
					}
					else if (c == '[')
					{
						return new Token(c, TokenKind.START_ARRAY);
					}
					else if (c == '{')
					{
						return new Token(c, TokenKind.START_PROC);
					}
					else if (c == ']')
					{
						return new Token(c, TokenKind.END_ARRAY);
					}
					else if (c == '}')
					{
						return new Token(c, TokenKind.END_PROC);
					}
					else if (c == '/')
					{
						return new Token(ReadRegular(), TokenKind.LITERAL);
					}
					else if (c == '<')
					{
						char c2 = GetChar();
						if (c2 == c)
						{
							return new Token("<<", TokenKind.START_DICT);
						}
						else
						{
							// code may have to be changed in something better, maybe new token type
							buffer.Seek(buffer.Position - 1);
							return new Token(c, TokenKind.NAME);
						}
					}
					else if (c == '>')
					{
						char c2 = GetChar();
						if (c2 == c)
						{
							return new Token(">>", TokenKind.END_DICT);
						}
						else
						{
							// code may have to be changed in something better, maybe new token type
							buffer.Seek(buffer.Position - 1);
							return new Token(c, TokenKind.NAME);
						}
					}
					else if (char.IsWhiteSpace(c))
					{
						skip = true;
					}
					else if (c == 0)
					{
						Debug.WriteLine("warning :NULL byte in font, skipped");
						skip = true;
					}
					else
					{
						buffer.Seek(buffer.Position - 1);

						// regular character: try parse as number
						Token number = TryReadNumber();
						if (number != null)
						{
							return number;
						}
						else
						{
							// otherwise this must be a name
							string name = ReadRegular();
							if (name == null)
							{
								// the stream is corrupt
								throw new DamagedFontException("Could not read token at position " +
															   buffer.Position);
							}

							if (string.Equals(name, "RD", StringComparison.Ordinal)
								|| string.Equals(name, "-|", StringComparison.Ordinal))
							{
								// return the next CharString instead
								if (prevToken != null && prevToken.Kind == TokenKind.INTEGER)
								{
									return ReadCharString(prevToken.IntValue);
								}
								else
								{
									throw new IOException("expected INTEGER before -| or RD");
								}
							}
							else
							{
								return new Token(name, TokenKind.NAME);
							}
						}
					}
				}
			}
			while (skip);
			return null;
		}

		/**
		 * Reads a number or returns null.
		 */
		private Token TryReadNumber()
		{
			buffer.Mark();

			StringBuilder sb = new StringBuilder();
			StringBuilder radix = null;
			char c = GetChar();
			bool hasDigit = false;

			// optional + or -
			if (c == '+' || c == '-')
			{
				sb.Append(c);
				c = GetChar();
			}

			// optional digits
			while (char.IsDigit(c))
			{
				sb.Append(c);
				c = GetChar();
				hasDigit = true;
			}

			// optional .
			if (c == '.')
			{
				sb.Append(c);
				c = GetChar();
			}
			else if (c == '#')
			{
				// PostScript radix number takes the form base#number
				radix = sb;
				sb = new StringBuilder();
				c = GetChar();
			}
			else if (sb.Length == 0 || !hasDigit)
			{
				// failure
				buffer.Reset();
				return null;
			}
			else
			{
				// integer
				buffer.Seek(buffer.Position - 1);
				return new Token(sb.ToString(), TokenKind.INTEGER);
			}

			// required digit
			if (char.IsDigit(c))
			{
				sb.Append(c);
				c = GetChar();
			}
			else
			{
				// failure
				buffer.Reset();
				return null;
			}

			// optional digits
			while (char.IsDigit(c))
			{
				sb.Append(c);
				c = GetChar();
			}

			// optional E
			if (c == 'E')
			{
				sb.Append(c);
				c = GetChar();

				// optional minus
				if (c == '-')
				{
					sb.Append(c);
					c = GetChar();
				}

				// required digit
				if (char.IsDigit(c))
				{
					sb.Append(c);
					c = GetChar();
				}
				else
				{
					// failure
					buffer.Reset();
					return null;
				}

				// optional digits
				while (char.IsDigit(c))
				{
					sb.Append(c);
					c = GetChar();
				}
			}

			buffer.Seek(buffer.Position - 1);
			if (radix != null)
			{
				int val = Convert.ToInt32(sb.ToString(), int.Parse(radix.ToString()));
				return new Token(val.ToString(), TokenKind.INTEGER);
			}
			return new Token(sb.ToString(), TokenKind.REAL);
		}

		/**
		 * Reads a sequence of regular characters, i.e. not delimiters
		 * or whitespace
		 */
		private string ReadRegular()
		{
			StringBuilder sb = new StringBuilder();
			while (buffer.Position < buffer.Length)
			{
				buffer.Mark();
				char c = GetChar();
				if (char.IsWhiteSpace(c) ||
					c == '(' || c == ')' ||
					c == '<' || c == '>' ||
					c == '[' || c == ']' ||
					c == '{' || c == '}' ||
					c == '/' || c == '%')
				{
					buffer.Reset();
					break;
				}
				else
				{
					sb.Append(c);
				}
			}
			string regular = sb.ToString();
			if (regular.Length == 0)
			{
				return null;
			}
			return regular;
		}

		/**
		 * Reads a line comment.
		 */
		private string ReadComment()
		{
			StringBuilder sb = new StringBuilder();
			while (buffer.Position < buffer.Length)
			{
				char c = GetChar();
				if (c == '\r' || c == '\n')
				{
					break;
				}
				else
				{
					sb.Append(c);
				}
			}
			return sb.ToString();
		}

		/**
		 * Reads a (string).
		 */
		private Token ReadString()
		{
			StringBuilder sb = new StringBuilder();

			while (buffer.Position < buffer.Length)
			{
				char c = GetChar();

				// string context
				switch (c)
				{
					case '(':
						openParens++;
						sb.Append('(');
						break;
					case ')':
						if (openParens == 0)
						{
							// end of string
							return new Token(sb.ToString(), TokenKind.STRING);
						}
						sb.Append(')');
						openParens--;
						break;
					case '\\':
						// escapes: \n \r \t \b \f \\ \( \)
						char c1 = GetChar();
						switch (c1)
						{
							case 'n':
							case 'r': sb.Append("\n"); break;
							case 't': sb.Append('\t'); break;
							case 'b': sb.Append('\b'); break;
							case 'f': sb.Append('\f'); break;
							case '\\': sb.Append('\\'); break;
							case '(': sb.Append('('); break;
							case ')': sb.Append(')'); break;
							default:
								break;
						}
						// octal \ddd
						if (char.IsDigit(c1))
						{
							string num = new string(new char[] { c1, GetChar(), GetChar() });
							int code = Convert.ToInt32(num, 8);
							sb.Append((char)(int)code);
						}
						break;
					case '\r':
					case '\n':
						sb.Append("\n");
						break;
					default:
						sb.Append(c);
						break;
				}
			}
			return null;
		}

		/**
		 * Reads a binary CharString.
		 */
		private Token ReadCharString(int length)
		{
			buffer.ReadByte(); // space
			byte[] data = new byte[length];
			buffer.Read(data);
			return new Token(data, TokenKind.CHARSTRING);
		}
	}
}
