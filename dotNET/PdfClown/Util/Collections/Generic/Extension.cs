/*
  Copyright 2010 Stefano Chizzolini. http://www.pdfclown.org

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

using System;
using System.Collections.Generic;

namespace PdfClown.Util.Collections.Generic
{
    public static class Extension
    {
        public static void AddAll<T>(this ICollection<T> collection, IEnumerable<T> enumerable)
        {
            foreach (T item in enumerable)
            { collection.Add(item); }
        }

        public static void RemoveAll<T>(this ICollection<T> collection, IEnumerable<T> enumerable)
        {
            foreach (T item in enumerable)
            { collection.Remove(item); }
        }

        /**
          <summary>Sets all the specified entries into this dictionary.</summary>
          <remarks>The effect of this call is equivalent to that of calling the indexer on this dictionary
          once for each entry in the specified enumerable.</remarks>
        */
        public static void SetAll<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, IEnumerable<KeyValuePair<TKey, TValue>> enumerable)
        {
            foreach (KeyValuePair<TKey, TValue> entry in enumerable)
            { dictionary[entry.Key] = entry.Value; }
        }

        public static T RemoveAtValue<T>(this List<T> list, int index)
        {
            var item = list[index];
            list.RemoveAt(index);
            return item;
        }

        public static void Fill<T>(this T[] list, T value)
        {
            for (int i = 0; i < list.Length; i++)
            {
                list[i] = value;
            }
        }

        public static void Fill<T>(this T[] list, int offset, int length, T value)
        {
            var max = System.Math.Min(list.Length, offset + length);
            for (int i = offset; i < max; i++)
            {
                list[i] = value;
            }
        }

        public static void Set<T>(this T[] dest, T[] src, int index)
        {
            Array.Copy(src, 0, dest, index, src.Length);
        }

        public static T[] SubArray<T>(this T[] src, int start, int end)
        {
            var len = end - start;
            var dest = new T[len];
            Array.Copy(src, start, dest, 0, len);
            return dest;
        }

        public static T[] CopyOf<T>(this T[] src, int length)
        {
            var minLength = System.Math.Min(length, src.Length);
            var dest = new T[length];
            Array.Copy(src, 0, dest, 0, minLength);
            return dest;
        }

    }

    public static class BytesExtension
    {


        public static byte[] CopyOfRange(this byte[] src, int start, int end)
        {
            var len = end - start;
            var dest = new byte[len];
            Array.Copy(src, start, dest, 0, len);
            return dest;
        }
    }
}

