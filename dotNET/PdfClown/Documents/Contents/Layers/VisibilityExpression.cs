/*
  Copyright 2015 Stefano Chizzolini. http://www.pdfclown.org

  Contributors:
    * Stefano Chizzolini (original code developer, http://www.stefanochizzolini.it)

  This file should be part of the source code distribution of "PDF Clown library"
  (the Program): see the accompanying README files for more info.

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

using PdfClown;
using PdfClown.Objects;
using PdfClown.Util;

using System;

namespace PdfClown.Documents.Contents.Layers
{
    /**
      <summary>Visibility expression, used to compute visibility of content based on a set of layers
      [PDF:1.7:4.10.1].</summary>
    */
    [PDF(VersionEnum.PDF16)]
    public class VisibilityExpression : PdfObjectWrapper2<PdfArray>
    {
        public enum OperatorEnum
        {
            And,
            Or,
            Not
        }

        private class OperandsImpl : Array<IPdfObjectWrapper>
        {
            private class ItemWrapper : IEntryWrapper<IPdfObjectWrapper>
            {
                public IPdfObjectWrapper Wrap(PdfDirectObject baseObject)
                {
                    if (baseObject.Wrapper is IPdfObjectWrapper wrapper)
                        return wrapper;
                    if (baseObject.Resolve() is PdfArray)
                        return Wrap2<VisibilityExpression>(baseObject);
                    else
                        return Wrap<Layer>(baseObject);
                }
            }

            private static readonly ItemWrapper Wrapper = new ItemWrapper();

            public OperandsImpl(PdfDirectObject baseObject) : base(Wrapper, baseObject)
            { }

            public override int Count => base.Count - 1;

            public override int IndexOf(IPdfObjectWrapper item)
            {
                int index = base.IndexOf(item);
                return index > 0 ? index - 1 : -1;
            }

            public override void Insert(int index, IPdfObjectWrapper item)
            {
                if (PdfName.Not.Equals(base[0]) && base.Count >= 2)
                    throw new ArgumentException("'Not' operator requires only one operand.");

                ValidateItem(item);
                base.Insert(index + 1, item);
            }

            public override void RemoveAt(int index)
            { base.RemoveAt(index + 1); }

            public override IPdfObjectWrapper this[int index]
            {
                get => base[index + 1];
                set
                {
                    ValidateItem(value);
                    base[index + 1] = value;
                }
            }

            private void ValidateItem(IPdfObjectWrapper item)
            {
                if (!(item is VisibilityExpression
                  || item is Layer))
                    throw new ArgumentException("Operand MUST be either VisibilityExpression or Layer");
            }
        }

        public VisibilityExpression(PdfDocument context, OperatorEnum @operator, params IPdfObjectWrapper[] operands)
            : base(context, new PdfArray(operands?.Length ?? 1) { (PdfDirectObject)null })
        {
            Operator = @operator;
            var operands_ = Operands;
            foreach (var operand in operands)
            { operands_.Add(operand); }
        }

        public VisibilityExpression(PdfDirectObject baseObject) : base(baseObject)
        { }

        public Array<IPdfObjectWrapper> Operands => Wrap<OperandsImpl>(BaseObject);

        public OperatorEnum Operator
        {
            get => OperatorEnumExtension.Get(BaseDataObject.GetString(0));
            set
            {
                if (value == OperatorEnum.Not && BaseDataObject.Count > 2)
                    throw new ArgumentException("'Not' operator requires only one operand.");

                BaseDataObject.SetName(0, value.GetName());
            }
        }
    }

    internal static class OperatorEnumExtension
    {
        private static readonly BiDictionary<VisibilityExpression.OperatorEnum, string> codes;

        static OperatorEnumExtension()
        {
            codes = new BiDictionary<VisibilityExpression.OperatorEnum, string>
            {
                [VisibilityExpression.OperatorEnum.And] = PdfName.And.StringValue,
                [VisibilityExpression.OperatorEnum.Not] = PdfName.Not.StringValue,
                [VisibilityExpression.OperatorEnum.Or] = PdfName.Or.StringValue
            };
        }

        public static VisibilityExpression.OperatorEnum Get(string name)
        {
            if (name == null)
                throw new ArgumentNullException();

            VisibilityExpression.OperatorEnum? @operator = codes.GetKey(name);
            if (!@operator.HasValue)
                throw new NotSupportedException("Operator unknown: " + name);

            return @operator.Value;
        }

        public static string GetName(this VisibilityExpression.OperatorEnum @operator) => codes[@operator];
    }
}

