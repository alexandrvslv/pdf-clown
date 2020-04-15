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

using PdfClown.Util.IO;

using System;
using System.Globalization;
using System.Text;

namespace PdfClown.Util
{
    /**
      <summary>Data convertion utility.</summary>
      <remarks>This class is a specialized adaptation from the original <a href="http://commons.apache.org/codec/">
      Apache Commons Codec</a> project, licensed under the <a href="http://www.apache.org/licenses/LICENSE-2.0">
      Apache License, Version 2.0</a>.</remarks>
    */
    public static class ConvertUtils
    {
        #region static
        #region fields
        private static readonly char[] HexDigits = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };
        public static readonly string HexAlphabet = "0123456789ABCDEF";

        public static readonly int[] HexValue = new int[] {
            0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F };
        #endregion

        #region interface
        #region public
        public static string ByteArrayToHex(byte[] data)
        {
            int dataLength = data.Length;
            char[] result = new char[dataLength * 2];
            for (int dataIndex = 0, resultIndex = 0; dataIndex < dataLength; dataIndex++)
            {
                result[resultIndex++] = HexDigits[(0xF0 & data[dataIndex]) >> 4];
                result[resultIndex++] = HexDigits[0x0F & data[dataIndex]];
            }
            return new string(result);
        }

        public static int ByteArrayToInt(byte[] data)
        { return ByteArrayToInt(data, 0, ByteOrderEnum.BigEndian); }

        public static int ByteArrayToInt(byte[] data, int index, ByteOrderEnum byteOrder)
        { return ByteArrayToNumber(data, index, 4, byteOrder); }

        public static int ByteArrayToNumber(byte[] data, int index, int length, ByteOrderEnum byteOrder)
        {
            int value = 0;
            length = (int)System.Math.Min(length, data.Length - index);
            for (int i = index, endIndex = index + length; i < endIndex; i++)
            { value |= (data[i] & 0xff) << 8 * (byteOrder == ByteOrderEnum.LittleEndian ? i - index : endIndex - i - 1); }
            return value;
        }
        internal static long ByteArrayToLong(byte[] data, int position, int size, ByteOrderEnum byteOrder)
        {
            var buffer = new byte[size];
            data.CopyTo(buffer, position);
            if (byteOrder == ByteOrderEnum.BigEndian)
                Array.Reverse(data);
            return BitConverter.ToInt64(data, position);
        }

        internal static ulong ByteArrayToULong(byte[] data, int position, int size, ByteOrderEnum byteOrder)
        {
            var buffer = new byte[size];
            data.CopyTo(buffer, position);
            if (byteOrder == ByteOrderEnum.BigEndian)
                Array.Reverse(data);
            return BitConverter.ToUInt64(data, position);
        }

        public static byte[] HexToByteArray(string data)
        {
            byte[] result;
            {
                int dataLength = data.Length;
                if ((dataLength % 2) != 0)
                    throw new Exception("Odd number of characters.");

                result = new byte[dataLength / 2];
                for (int resultIndex = 0, dataIndex = 0; dataIndex < dataLength; resultIndex++)
                {
                    result[resultIndex] = byte.Parse(
                      data[dataIndex++].ToString() + data[dataIndex++].ToString(),
                      System.Globalization.NumberStyles.HexNumber
                      );
                }
            }
            return result;
        }

        //https://stackoverflow.com/a/5919521/4682355
        public static string ByteArrayToHexString(byte[] Bytes)
        {
            StringBuilder Result = new StringBuilder(Bytes.Length * 2);

            foreach (byte B in Bytes)
            {
                Result.Append(HexAlphabet[(int)(B >> 4)]);
                Result.Append(HexAlphabet[(int)(B & 0xF)]);
            }

            return Result.ToString();
        }

        public static byte[] HexStringToByteArray(string Hex)
        {
            byte[] Bytes = new byte[Hex.Length / 2];


            for (int x = 0, i = 0; i < Hex.Length; i += 2, x += 1)
            {
                Bytes[x] = (byte)(HexValue[Char.ToUpper(Hex[i + 0]) - '0'] << 4 |
                                  HexValue[Char.ToUpper(Hex[i + 1]) - '0']);
            }

            return Bytes;
        }

        public static byte[] IntToByteArray(int data, bool compact = false)
        {
            if (compact)
            {
                if (data < 1 << 8)
                {
                    return new byte[] { (byte)data };
                }
                else if (data < 1 << 16)
                {
                    return new byte[] { (byte)(data >> 8), (byte)data };
                }
                else if (data < 1 << 24)
                {
                    return new byte[] { (byte)(data >> 16), (byte)(data >> 8), (byte)data };
                }
            }
            return new byte[] { (byte)(data >> 24), (byte)(data >> 16), (byte)(data >> 8), (byte)data };
        }

        public static byte[] NumberToByteArray(int data, int length, ByteOrderEnum byteOrder)
        {
            byte[] result = new byte[length];
            for (int index = 0; index < length; index++)
            { result[index] = (byte)(data >> 8 * (byteOrder == ByteOrderEnum.LittleEndian ? index : length - index - 1)); }
            return result;
        }

        public static int ParseAsIntInvariant(string value)
        { return (int)ParseFloatInvariant(value); }

        public static double ParseDoubleInvariant(string value)
        { return Double.Parse(value, NumberStyles.Float, NumberFormatInfo.InvariantInfo); }

        public static float ParseFloatInvariant(string value)
        { return Single.Parse(value, NumberStyles.Float, NumberFormatInfo.InvariantInfo); }

        public static int ParseIntInvariant(string value)
        { return Int32.Parse(value, NumberStyles.Integer, NumberFormatInfo.InvariantInfo); }

        public static float[] ToFloatArray(double[] array)
        {
            float[] result = new float[array.Length];
            for (int index = 0, length = array.Length; index < length; index++)
            { result[index] = (float)array[index]; }
            return result;
        }


        #endregion
        #endregion
        #endregion
    }
}