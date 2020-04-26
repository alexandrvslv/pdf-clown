/*
  Copyright 2007-2012 Stefano Chizzolini. http://www.pdfclown.org

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

using System.Collections.Generic;

namespace PdfClown.Documents.Contents.Objects
{
    /**
     <summary>'Font Type3 Char Proc Information' operation
     [PDF:1.6:4.4.1].</summary>
   */
    [PDF(VersionEnum.PDF10)]
    public sealed class CharProcWidth : Operation
    {
        #region static
        #region fields
        public static readonly string OperatorKeyword = "d0";
        #endregion
        #endregion

        #region dynamic
        #region constructors
        public CharProcWidth(double wx, double wy)
            : base(OperatorKeyword,
                  new List<PdfDirectObject>(new PdfDirectObject[] { PdfReal.Get(wx), PdfReal.Get(wy) }))
        { }

        public CharProcWidth(IList<PdfDirectObject> operands) : base(OperatorKeyword, operands)
        { }
        #endregion

        #region interface
        #region public        

        public override void Scan(GraphicsState state)
        {
        }
        public double WX
        {
            get => ((IPdfNumber)operands[0]).RawValue;
            set => operands[0] = PdfReal.Get(value);
        }

        public double WY
        {
            get => ((IPdfNumber)operands[1]).RawValue;
            set => operands[1] = PdfReal.Get(value);
        }
        #endregion
        #endregion
        #endregion
    }
}