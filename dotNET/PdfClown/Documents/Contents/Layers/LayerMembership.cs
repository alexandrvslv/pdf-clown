/*
  Copyright 2011-2015 Stefano Chizzolini. http://www.pdfclown.org

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

using PdfClown.Objects;

using System;
using System.Collections;
using System.Collections.Generic;

namespace PdfClown.Documents.Contents.Layers
{
    /// <summary>Optional content membership [PDF:1.7:4.10.1].</summary>
    [PDF(VersionEnum.PDF15)]
    public sealed class LayerMembership : LayerEntity
    {
        /// <summary>Layers whose states determine the visibility of content controlled by a membership.</summary>
        private class VisibilityMembersImpl : PdfObjectWrapper<PdfDirectObject>, IList<Layer>
        {
            private LayerMembership membership;

            internal VisibilityMembersImpl(LayerMembership membership)
                : base(membership.BaseDataObject[PdfName.OCGs])
            { this.membership = membership; }

            public int IndexOf(Layer item)
            {
                PdfDataObject baseDataObject = BaseDataObject;
                if (baseDataObject == null) // No layer.
                    return -1;
                else if (baseDataObject is PdfDictionary) // Single layer.
                    return item.BaseObject.Equals(BaseObject) ? 0 : -1;
                else // Multiple layers.
                    return ((PdfArray)baseDataObject).IndexOf(item.BaseObject);
            }

            public void Insert(int index, Layer item) => EnsureArray().Insert(index, item.BaseObject);

            public void RemoveAt(int index) => EnsureArray().RemoveAt(index);

            public Layer this[int index]
            {
                get
                {
                    PdfDataObject baseDataObject = BaseDataObject;
                    if (baseDataObject == null) // No layer.
                        return null;
                    else if (baseDataObject is PdfDictionary) // Single layer.
                    {
                        if (index != 0)
                            throw new IndexOutOfRangeException();

                        return Wrap<Layer>(BaseObject);
                    }
                    else // Multiple layers.
                        return Wrap<Layer>(((PdfArray)baseDataObject)[index]);
                }
                set => EnsureArray()[index] = value.BaseObject;
            }

            public void Add(Layer item) => EnsureArray().Add(item.BaseObject);

            public void Clear() => EnsureArray().Clear();

            public bool Contains(Layer item)
            {
                PdfDataObject baseDataObject = BaseDataObject;
                if (baseDataObject == null) // No layer.
                    return false;
                else if (baseDataObject is PdfDictionary) // Single layer.
                    return item.BaseObject.Equals(BaseObject);
                else // Multiple layers.
                    return ((PdfArray)baseDataObject).Contains(item.BaseObject);
            }

            public void CopyTo(Layer[] items, int index)
            { throw new NotImplementedException(); }

            public int Count
            {
                get
                {
                    PdfDataObject baseDataObject = BaseDataObject;
                    if (baseDataObject == null) // No layer.
                        return 0;
                    else if (baseDataObject is PdfDictionary) // Single layer.
                        return 1;
                    else // Multiple layers.
                        return ((PdfArray)baseDataObject).Count;
                }
            }

            public bool IsReadOnly => false;

            public bool Remove(Layer item) => EnsureArray().Remove(item.BaseObject);

            public IEnumerator<Layer> GetEnumerator()
            {
                for (int index = 0, length = Count; index < length; index++)
                { yield return this[index]; }
            }

            IEnumerator IEnumerable.GetEnumerator()
            { return this.GetEnumerator(); }

            private PdfArray EnsureArray()
            {
                PdfDirectObject baseDataObject = BaseDataObject;
                if (baseDataObject is not PdfArray)
                {
                    var array = new PdfArray();
                    if (baseDataObject != null)
                    { array.Add(baseDataObject); }
                    BaseObject = baseDataObject = array;
                    membership.BaseDataObject[PdfName.OCGs] = BaseObject;
                }
                return (PdfArray)baseDataObject;
            }
        }

        public static PdfName TypeName = PdfName.OCMD;


        public LayerMembership(PdfDocument context) : base(context, TypeName)
        { }

        internal LayerMembership(PdfDirectObject baseObject) : base(baseObject)
        { }

        public override LayerEntity Membership => this;

        public override VisibilityExpression VisibilityExpression
        {
            get => Wrap2<VisibilityExpression>(BaseDataObject[PdfName.VE]);
            set => BaseDataObject[PdfName.VE] = PdfObjectWrapper.GetBaseObject(value);
        }

        public override IList<Layer> VisibilityMembers
        {
            get => new VisibilityMembersImpl(this);
            set
            {
                var visibilityMembers = this.VisibilityMembers;
                visibilityMembers.Clear();
                foreach (var layer in value)
                { visibilityMembers.Add(layer); }
            }
        }

        public override VisibilityPolicyEnum VisibilityPolicy
        {
            get => VisibilityPolicyEnumExtension.Get(BaseDataObject.GetString(PdfName.P));
            set => BaseDataObject[PdfName.P] = value.GetName();
        }
    }
}