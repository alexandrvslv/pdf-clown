/*
  Copyright 2006-2015 Stefano Chizzolini. http://www.pdfclown.org

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

using PdfClown.Bytes.Filters;
using PdfClown.Objects;
using PdfClown.Tokens;
using PdfClown.Util;
using PdfClown.Util.IO;

using System;
using System.Collections.Generic;
using System.IO;
using text = System.Text;

namespace PdfClown.Bytes
{
    //TODO:IMPL Substitute System.Array static class invocations with System.Buffer static class invocations (better performance)!!!
    /**
      <summary>Byte buffer.</summary>
    */
    public sealed class Buffer : IBuffer
    {
        #region static
        #region fields
        /**
          <summary>Default buffer capacity.</summary>
        */
        private const int DefaultCapacity = 1 << 8;
        #endregion
        public static PdfDataObject Resolve(PdfObject @object)
        {
            return @object == null ? null : @object.Resolve();
        }

        public static void Decode(IBuffer buffer, PdfDataObject filter, PdfDirectObject parameters, PdfDictionary header)
        {

            if (filter is PdfName name) // Single filter.
            {
                buffer.Decode(Filter.Get(name), (PdfDictionary)parameters, header);
            }
            else // Multiple filters.
            {
                IEnumerator<PdfDirectObject> filterIterator = ((PdfArray)filter).GetEnumerator();
                IEnumerator<PdfDirectObject> parametersIterator = (parameters != null ? ((PdfArray)parameters).GetEnumerator() : null);
                while (filterIterator.MoveNext())
                {
                    PdfDictionary filterParameters;
                    if (parametersIterator == null)
                    { filterParameters = null; }
                    else
                    {
                        parametersIterator.MoveNext();
                        filterParameters = (PdfDictionary)Resolve(parametersIterator.Current);
                    }
                    buffer.Decode(Filter.Get((PdfName)Resolve(filterIterator.Current)), filterParameters, header);
                }
            }
        }

        public static IBuffer Extract(IBuffer buffer, PdfDataObject filter, PdfDirectObject parameters, PdfDictionary header)
        {
            if (filter == null)
            {
                return buffer;
            }
            if (filter is PdfName) // Single filter.
            {
                buffer = buffer.Extract(Filter.Get((PdfName)filter), (PdfDictionary)parameters, header);
            }
            else // Multiple filters.
            {
                var filterIterator = ((PdfArray)filter).GetEnumerator();
                var parametersIterator = (parameters != null ? ((PdfArray)parameters).GetEnumerator() : null);
                while (filterIterator.MoveNext())
                {
                    PdfDictionary filterParameters;
                    if (parametersIterator == null)
                    { filterParameters = null; }
                    else
                    {
                        parametersIterator.MoveNext();
                        filterParameters = (PdfDictionary)Resolve(parametersIterator.Current);
                    }
                    buffer = buffer.Extract(Filter.Get((PdfName)Resolve(filterIterator.Current)), filterParameters, header);
                }
            }
            return buffer;
        }
        #endregion

        #region dynamic
        #region events
        public event EventHandler OnChange;
        #endregion

        #region fields
        /**
          <summary>Inner buffer where data are stored.</summary>
        */
        private byte[] data;

        /**
 <summary>Number of bytes actually used in the buffer.</summary>
*/
        private int length;
        /**
          <summary>Pointer position within the buffer.</summary>
        */
        private int position = 0;

        private ByteOrderEnum byteOrder = ByteOrderEnum.BigEndian;

        private bool dirty;
        private int mark;
        #endregion

        #region constructors
        public Buffer() : this(0)
        { }

        public Buffer(int capacity)
        {
            if (capacity < 1)
            { capacity = DefaultCapacity; }

            this.data = new byte[capacity];
            this.length = 0;
        }

        public Buffer(byte[] data)
        {
            this.data = data;
            this.length = data.Length;
        }

        public Buffer(System.IO.Stream data) : this((int)data.Length)
        { Append(data); }

        public Buffer(string data) : this()
        { Append(data); }
        #endregion

        #region interface
        #region public
        #region IBuffer
        public IBuffer Append(byte data)
        {
            EnsureCapacity(1);
            this.data[this.length++] = data;
            NotifyChange();
            return this;
        }

        public IBuffer Append(byte[] data)
        { return Append(data, 0, data.Length); }

        public IBuffer Append(byte[] data, int offset, int length)
        {
            EnsureCapacity(length);
            Array.Copy(data, offset, this.data, this.length, length);
            this.length += length;
            NotifyChange();
            return this;
        }

        public IBuffer Append(string data)
        { return Append(Encoding.Pdf.Encode(data)); }

        public IBuffer Append(IInputStream data)
        { return Append(data.ToByteArray(), 0, (int)data.Length); }

        public IBuffer Append(System.IO.Stream data)
        {
            byte[] array = new byte[data.Length];
            {
                data.Position = 0;
                data.Read(array, 0, array.Length);
            }
            return Append(array);
        }

        public int Capacity => data.Length;

        public IBuffer Clone()
        {
            IBuffer clone = new Buffer(Capacity);
            clone.Append(data, 0, this.length);
            return clone;
        }

        public void Decode(Filter filter, PdfDirectObject parameters, PdfDictionary header)
        {
            data = filter.Decode(data, 0, length, parameters, header);
            length = data.Length;
        }

        public IBuffer Extract(Filter filter, PdfDirectObject parameters, PdfDictionary header)
        {
            var data = filter.Decode(this.data, 0, this.length, parameters, header);
            return new Buffer(data);
        }

        public void Delete(int index, int length)
        {
            // Shift left the trailing data block to override the deleted data!
            Array.Copy(this.data, index + length, this.data, index, this.length - (index + length));
            this.length -= length;
            NotifyChange();
        }

        public byte[] Encode(Filter filter, PdfDirectObject parameters, PdfDictionary header)
        { return filter.Encode(data, 0, length, parameters, header); }

        public int GetByte(int index)
        { return data[index]; }

        public byte[] GetByteArray(int index, int length)
        {
            byte[] data = new byte[length];
            Array.Copy(this.data, index, data, 0, length);
            return data;
        }

        public string GetString(int index, int length)
        { return Encoding.Pdf.Decode(data, index, length); }

        public void Insert(int index, byte[] data)
        { Insert(index, data, 0, data.Length); }

        public void Insert(int index, byte[] data, int offset, int length)
        {
            EnsureCapacity(length);
            // Shift right the existing data block to make room for new data!
            Array.Copy(this.data, index, this.data, index + length, this.length - index);
            // Insert additional data!
            Array.Copy(data, offset, this.data, index, length);
            this.length += length;
            NotifyChange();
        }

        public void Insert(int index, string data)
        { Insert(index, Encoding.Pdf.Encode(data)); }

        public void Insert(int index, IInputStream data)
        { Insert(index, data.ToByteArray()); }

        public void Replace(int index, byte[] data)
        {
            Array.Copy(data, 0, this.data, index, data.Length);
            NotifyChange();
        }

        public void Replace(int index, byte[] data, int offset, int length)
        {
            Array.Copy(data, offset, this.data, index, data.Length);
            NotifyChange();
        }

        public void Replace(int index, string data)
        { Replace(index, Encoding.Pdf.Encode(data)); }

        public void Replace(int index, IInputStream data)
        { Replace(index, data.ToByteArray()); }

        public void SetLength(int value)
        {
            length = value;
            NotifyChange();
        }

        public void WriteTo(IOutputStream stream)
        { stream.Write(data, 0, length); }

        #region IInputStream
        public bool Dirty
        {
            get => dirty;
            set => dirty = value;
        }

        public ByteOrderEnum ByteOrder
        {
            get => byteOrder;
            set => byteOrder = value;
        }

        /* int GetHashCode() uses inherited implementation. */

        public long Position
        {
            get => position;
            private set => position = (int)value;
        }

        public int Read(byte[] data)
        { return Read(data, 0, data.Length); }

        public int Read(byte[] data, int offset, int length)
        {
            if (position + length > Length)
            {
                length = (int)(Length - position);
            }
            Array.Copy(this.data, position, data, offset, length);
            position += length;
            return length;
        }

        public int Read(sbyte[] data)
        { return Read(data, 0, data.Length); }

        public int Read(sbyte[] data, int offset, int length)
        {
            if (position + length > Length)
            {
                length = (int)(Length - position);
            }
            System.Buffer.BlockCopy(this.data, position, data, offset, length);
            position += length;
            return length;
        }

        public byte[] ReadNullTermitaded()
        {
            var start = position;
            var length = 0;
            while (ReadByte() > 0) { length++; }
            return GetByteArray(start, length);
        }

        public byte[] ReadBytes(int length)
        {
            var start = position;
            position += length;
            return GetByteArray(start, length);
        }

        public int ReadByte()
        {
            if (position >= data.Length)
                return -1; //TODO:harmonize with other Read*() method EOF exceptions!!!

            return data[position++];
        }

        public int ReadInt()
        {
            int value = ConvertUtils.ByteArrayToInt(data, position, byteOrder);
            position += sizeof(int);
            return value;
        }

        public uint ReadUnsignedInt()
        {
            var value = (uint)ConvertUtils.ByteArrayToInt(data, position, byteOrder);
            position += sizeof(int);
            return value;
        }

        public int ReadInt(int length)
        {
            int value = ConvertUtils.ByteArrayToNumber(data, position, length, byteOrder);
            position += length;
            return value;
        }

        public ulong ReadUnsignedLong()
        {
            var size = sizeof(long);
            var value = ConvertUtils.ByteArrayToULong(data, position, size, byteOrder);
            position += size;
            return value;
        }

        public long ReadLong()
        {
            var size = sizeof(long);
            var value = ConvertUtils.ByteArrayToLong(data, position, size, byteOrder);
            position += size;
            return value;
        }

        public string ReadLine()
        {
            if (position >= data.Length)
                throw new EndOfStreamException();

            text::StringBuilder buffer = new text::StringBuilder();
            while (position < data.Length)
            {
                int c = data[position++];
                if (c == '\r'
                  || c == '\n')
                    break;

                buffer.Append((char)c);
            }
            return buffer.ToString();
        }

        public short ReadShort()
        {
            short value = (short)ConvertUtils.ByteArrayToNumber(data, position, sizeof(short), byteOrder);
            position += sizeof(short);
            return value;
        }

        public string ReadString(int length)
        {
            string data = Encoding.Pdf.Decode(this.data, position, length);
            position += length;
            return data;
        }

        public sbyte ReadSignedByte()
        {
            if (position >= data.Length)
                throw new EndOfStreamException();

            return (sbyte)data[position++];
        }

        public ushort ReadUnsignedShort()
        {
            ushort value = (ushort)ConvertUtils.ByteArrayToNumber(data, position, sizeof(ushort), byteOrder);
            position += sizeof(ushort);
            return value;
        }

        public float ReadFixed32()
        {
            return ReadShort() // Signed Fixed-point mantissa (16 bits).
               + ReadUnsignedShort() / 16384f; // Fixed-point fraction (16 bits).
        }

        public float ReadUnsignedFixed32()
        {
            return ReadUnsignedShort() // Fixed-point mantissa (16 bits).
               + ReadUnsignedShort() / 16384f; // Fixed-point fraction (16 bits).
        }

        public float ReadFixed16()
        {
            return ReadSignedByte() // Fixed-point mantissa (8 bits).
               + ReadByte() / 64f; // Fixed-point fraction (8 bits).
        }

        public float ReadUnsignedFixed16()
        {
            return (byte)ReadByte() // Fixed-point mantissa (8 bits).
               + ReadByte() / 64f; // Fixed-point fraction (8 bits).
        }

        public void Seek(long position)
        {
            if (position < 0)
            { position = 0; }
            else if (position > data.Length)
            { position = data.Length; }

            this.position = (int)position;
        }

        public long Skip(long offset)
        {
            var newPosition = position + offset;
            Seek(newPosition);
            return newPosition;
        }

        public int Mark()
        {
            return mark = position;
        }

        public void Reset()
        {
            Seek(mark);
        }

        #region IDataWrapper
        public byte[] ToByteArray()
        {
            byte[] data = new byte[this.length];
            Array.Copy(this.data, 0, data, 0, this.length);
            return data;
        }

        public byte[] GetBuffer()
        {
            return data;
        }
        #endregion

        #region IStream
        public long Length => length;

        public long Available { get => length - position; }

        #region IDisposable
        public void Dispose()
        { }
        #endregion
        #endregion
        #endregion
        #endregion

        #region IOutputStream
        public void Clear()
        { SetLength(0); }

        public void Write(byte[] data)
        { Append(data); }

        public void Write(byte[] data, int offset, int length)
        { Append(data, offset, length); }

        public void Write(string data)
        { Append(data); }

        public void Write(IInputStream data)
        { Append(data); }
        #endregion
        #endregion

        #region private
        /**
          <summary>Check whether the buffer has sufficient room for
          adding data.</summary>
        */
        private void EnsureCapacity(int additionalLength)
        {
            int minCapacity = this.length + additionalLength;
            // Is additional data within the buffer capacity?
            if (minCapacity <= this.data.Length)
                return;

            // Additional data exceed buffer capacity.
            // Reallocate the buffer!
            byte[] data = new byte[
              Math.Max(
                this.data.Length << 1, // 1 order of magnitude greater than current capacity.
                minCapacity // Minimum capacity required.
                )
              ];
            Array.Copy(this.data, 0, data, 0, this.length);
            this.data = data;
        }

        private void NotifyChange()
        {
            if (dirty || OnChange == null)
                return;

            dirty = true;
            OnChange(this, null);
        }

        internal void Flush()
        {
            throw new NotImplementedException();
        }


        #endregion
        #endregion
        #endregion
    }
}