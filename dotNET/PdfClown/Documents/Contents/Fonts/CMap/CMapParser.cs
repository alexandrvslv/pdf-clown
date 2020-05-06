/*
  Copyright 2009-2015 Stefano Chizzolini. http://www.pdfclown.org

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

using bytes = PdfClown.Bytes;
using PdfClown.Documents.Contents.Objects;
using PdfClown.Objects;
using PdfClown.Tokens;
using PdfClown.Util;
using PdfClown.Util.Math;
using PdfClown.Util.Parsers;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using io = System.IO;
using System.Text;

namespace PdfClown.Documents.Contents.Fonts
{
    /**
      <summary>CMap parser [PDF:1.6:5.6.4;CMAP].</summary>
    */
    internal sealed class CMapParser : PostScriptParser
    {
        #region static
        #region fields
        private static readonly string BeginCodeSpaceRangeOperator = "begincodespacerange";
        private static readonly string BeginBaseFontCharOperator = "beginbfchar";
        private static readonly string BeginBaseFontRangeOperator = "beginbfrange";
        private static readonly string BeginCIDCharOperator = "begincidchar";
        private static readonly string BeginCIDRangeOperator = "begincidrange";
        private static readonly string DefOperator = "def";
        private static readonly string UseCMapOperator = "usecmap";

        private static readonly string CMapName = PdfName.CMapName.StringValue;
        private static readonly string CMapType = PdfName.CMapType.StringValue;
        private static readonly string Registry = PdfName.Registry.StringValue;
        private static readonly string Ordering = PdfName.Ordering.StringValue;
        private static readonly string WMode = PdfName.WMode.StringValue;

        #endregion
        #endregion

        #region dynamic
        #region constructors
        public CMapParser(io::Stream stream) : this(new bytes::Buffer(stream))
        { }

        public CMapParser(bytes::IInputStream stream) : base(stream)
        { }
        #endregion

        #region interface
        #region public
        /**
          <summary>Parses the character-code-to-unicode mapping [PDF:1.6:5.9.1].</summary>
        */
        public CMap Parse()
        {
            Stream.Seek(0);
            var codes = new CMap();
            {
                IList<object> operands = new List<object>();

                while (MoveNext())
                {
                    switch (TokenType)
                    {
                        case TokenTypeEnum.Keyword:
                            {
                                string @operator = (string)Token;
                                if (@operator.Equals(BeginCodeSpaceRangeOperator, StringComparison.Ordinal))
                                {
                                    ParseCodeSpaceRange(codes, operands);
                                }
                                else if (@operator.Equals(BeginBaseFontCharOperator, StringComparison.Ordinal))
                                {
                                    ParseBFChar(codes, operands);
                                }
                                else if (@operator.Equals(BeginCIDCharOperator, StringComparison.Ordinal))
                                {
                                    ParseCIDChar(codes, operands);
                                }
                                else if (@operator.Equals(BeginBaseFontRangeOperator, StringComparison.Ordinal))
                                {
                                    ParseBFRange(codes, operands);
                                }
                                else if (@operator.Equals(BeginCIDRangeOperator, StringComparison.Ordinal))
                                {
                                    ParseCIDRange(codes, operands);
                                }
                                else if (@operator.Equals(UseCMapOperator, StringComparison.Ordinal))
                                {
                                    var useCMap = CMap.Get((string)operands[0]);
                                    codes.UseCmap(useCMap);
                                }
                                else if (@operator.Equals(DefOperator, StringComparison.Ordinal) && operands.Count != 0)
                                {
                                    if (CMapName.Equals((string)operands[0], StringComparison.Ordinal))
                                    {
                                        codes.CMapName = (string)operands[1];
                                    }
                                    else if (CMapType.Equals((string)operands[0], StringComparison.Ordinal))
                                    {
                                        codes.CMapType = (int)operands[1];
                                    }
                                    else if (Registry.Equals((string)operands[0], StringComparison.Ordinal))
                                    {
                                        codes.Registry = (string)operands[1];
                                    }
                                    else if (Ordering.Equals((string)operands[0], StringComparison.Ordinal))
                                    {
                                        codes.Ordering = (string)operands[1];
                                    }
                                    else if (WMode.Equals((string)operands[0], StringComparison.Ordinal))
                                    {
                                        codes.WMode = (int)operands[1];
                                    }
                                }
                                operands.Clear();
                                break;
                            }
                        case TokenTypeEnum.ArrayBegin:
                        case TokenTypeEnum.DictionaryBegin:
                            {
                                // Skip.
                                while (MoveNext())
                                {
                                    if (TokenType == TokenTypeEnum.ArrayEnd
                                      || TokenType == TokenTypeEnum.DictionaryEnd)
                                        break;
                                }
                                break;
                            }
                        case TokenTypeEnum.Comment:
                            // Skip.
                            break;
                        default:
                            {
                                operands.Add(Token);
                                break;
                            }
                    }
                }
            }
            return codes;
        }

        private void ParseCIDRange(CMap codes, IList<object> operands)
        {
            for (int itemIndex = 0, itemCount = (int)operands[0]; itemIndex < itemCount; itemIndex++)
            {
                // 1. Beginning input code.
                MoveNext();
                byte[] beginInputCode = ParseInputCode();
                int beginInput = ConvertUtils.ByteArrayToInt(beginInputCode);
                // 2. Ending input code.
                MoveNext();
                byte[] endInputCode = ParseInputCode();
                int entInput = ConvertUtils.ByteArrayToInt(endInputCode);
                MoveNext();
                int mappedCode = ParseUnicode();
                // 3. Character codes.
                if (beginInputCode.Length <= 2 && endInputCode.Length <= 2)
                {
                    // some CMaps are using CID ranges to map single values
                    if (beginInput == entInput)
                    {
                        codes.AddCIDMapping(mappedCode, beginInput);
                    }
                    else
                    {
                        codes.AddCIDRange((char)beginInput, (char)entInput, mappedCode);
                    }
                }
                else
                {
                    // TODO Is this even possible?
                    int endOfMappings = mappedCode + entInput - beginInput;
                    while (mappedCode <= endOfMappings)
                    {
                        int mappedCID = ConvertUtils.ByteArrayToInt(beginInputCode);
                        codes.AddCIDMapping(mappedCode++, mappedCID);
                        OperationUtils.Increment(beginInputCode);
                    }
                }
            }
        }

        private void ParseBFRange(CMap codes, IList<object> operands)
        {
            //NOTE: The first and second elements in each line are the beginning and
            //ending valid input codes for the template font; the third element is
            //the beginning character code for the range.
            for (int itemIndex = 0, itemCount = (int)operands[0]; itemIndex < itemCount; itemIndex++)
            {
                // 1. Beginning input code.
                MoveNext();
                byte[] beginInputCode = ParseInputCode();
                int beginInput = ConvertUtils.ByteArrayToInt(beginInputCode);
                // 2. Ending input code.
                MoveNext();
                byte[] endInputCode = ParseInputCode();
                int entInput = ConvertUtils.ByteArrayToInt(endInputCode);


                MoveNext();
                switch (TokenType)
                {
                    case TokenTypeEnum.ArrayBegin:
                        {
                            byte[] inputCode = beginInputCode;
                            while (MoveNext()
                              && TokenType != TokenTypeEnum.ArrayEnd)
                            {
                                // FIXME: Unicode character sequences (such as ligatures) have not been supported yet [BUG:72].
                                try
                                {
                                    codes.AddCharMapping(inputCode, ParseUnicode());
                                }
                                catch (OverflowException)
                                { Debug.WriteLine($"WARN: Unable to process Unicode sequence from {codes.CMapName} CMap: {Token}"); }
                                OperationUtils.Increment(inputCode);
                            }
                            break;
                        }
                    default:
                        {
                            var tokenBytes = ParseInputCode();
                            if (tokenBytes.Length > 0)
                            {
                                // some pdfs use the malformed bfrange <0000> <FFFF> <0000>. Add support by adding a identity
                                // mapping for the whole range instead of cutting it after 255 entries
                                // TODO find a more efficient method to represent all values for a identity mapping
                                if (tokenBytes.Length == 2 && beginInput == 0 && entInput == 0xffff
                                        && tokenBytes[0] == 0 && tokenBytes[1] == 0)
                                {
                                    for (int i = 0; i < 256; i++)
                                    {
                                        beginInputCode[1] = (byte)i;
                                        tokenBytes[1] = (byte)i;
                                        AddMappingFrombfrange(codes, beginInputCode, 0xff, tokenBytes);

                                    }
                                }
                                else
                                {
                                    // PDFBOX-4661: avoid overflow of the last byte, all following values are undefined
                                    int values = Math.Min(entInput - beginInput,
                                            255 - (tokenBytes[tokenBytes.Length - 1] & 0xFF)) + 1;
                                    AddMappingFrombfrange(codes, beginInputCode, values, tokenBytes);
                                }
                            }
                            break;
                        }
                }
            }
        }

        private void ParseCodeSpaceRange(CMap codes, IList<object> operands)
        {
            for (int itemIndex = 0, itemCount = (int)operands[0]; itemIndex < itemCount; itemIndex++)
            {
                MoveNext();
                byte[] startRange = ParseInputCode();
                MoveNext();
                byte[] endRange = ParseInputCode();
                codes.AddCodespaceRange(new CodespaceRange(startRange, endRange));
            }
        }

        private void ParseCIDChar(CMap codes, IList<object> operands)
        {
            for (int itemIndex = 0, itemCount = (int)operands[0]; itemIndex < itemCount; itemIndex++)
            {
                MoveNext();
                var inputCode = ParseInputCode();
                int mappedCID = ConvertUtils.ByteArrayToInt(inputCode);
                MoveNext();
                var mappedCode = ParseUnicode();

                codes.AddCIDMapping(mappedCode, mappedCID);
            }
        }

        private void ParseBFChar(CMap codes, IList<object> operands)
        {
            //NOTE: The first element on each line is the input code of the template font;
            //the second element is the code or name of the character.
            for (int itemIndex = 0, itemCount = (int)operands[0]; itemIndex < itemCount; itemIndex++)
            {
                MoveNext();
                var inputCode = ParseInputCode();
                MoveNext();
                // FIXME: Unicode character sequences (such as ligatures) have not been supported yet [BUG:72].
                try
                {
                    codes.AddCharMapping(inputCode, ParseUnicode());
                }
                catch (OverflowException)
                { Debug.WriteLine($"WARN: Unable to process Unicode sequence from {codes.CMapName} CMap: {Token}"); }
            }
        }
        #endregion

        #region private
        /**
          <summary>Converts the current token into its input code value.</summary>
        */
        private byte[] ParseInputCode()
        {
            return ConvertUtils.HexStringToByteArray((string)Token);
        }

        /**
          <summary>Converts the current token into its Unicode value.</summary>
        */
        private int ParseUnicode()
        {
            switch (TokenType)
            {
                case TokenTypeEnum.Hex: // Character code in hexadecimal format.
                    return int.TryParse((string)Token, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var result) ? result : -1;
                case TokenTypeEnum.Integer: // Character code in plain format.
                    return (int)Token;
                case TokenTypeEnum.Name: // Character name.
                    return GlyphMapping.Default.ToUnicode((string)Token).Value;
                default:
                    throw new Exception("Hex string, integer or name expected instead of " + TokenType);
            }
        }

        private void AddMappingFrombfrange(CMap cmap, byte[] startCode, int values, byte[] tokenBytes)
        {
            for (int i = 0; i < values; i++)
            {
                var value = ConvertUtils.ByteArrayToInt(tokenBytes);
                cmap.AddCharMapping(startCode, value);
                Increment(startCode);
                Increment(tokenBytes);
            }
        }

        private void Increment(byte[] data)
        {
            Increment(data, data.Length - 1);
        }

        private void Increment(byte[] data, int position)
        {
            if (position > 0 && (data[position] & 0xFF) == 255)
            {
                data[position] = 0;
                Increment(data, position - 1);
            }
            else
            {
                data[position] = (byte)(data[position] + 1);
            }
        }
        #endregion
        #endregion
        #endregion
    }



}