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

using PdfClown.Documents;
using PdfClown.Documents.Contents.Fonts;
using PdfClown.Documents.Contents.Objects;
using PdfClown.Documents.Contents.Scanner;
using PdfClown.Documents.Contents.XObjects;
using PdfClown.Objects;

using System;
using System.Collections.Generic;

namespace PdfClown.Documents.Contents
{
    /**
      <summary>Graphics state parameters [PDF:1.6:4.3.4].</summary>
    */
    [PDF(VersionEnum.PDF12)]
    public sealed class ExtGState : PdfObjectWrapper<PdfDictionary>
    {
        #region static
        #region fields
        internal static readonly IList<BlendModeEnum> DefaultBlendMode = new BlendModeEnum[0];
        #endregion

        #endregion

        #region dynamic
        #region constructors
        public ExtGState(Document context) : base(context, new PdfDictionary())
        { }

        public ExtGState(PdfDirectObject baseObject) : base(baseObject)
        { }
        #endregion

        #region interface
        #region public
        /**
          <summary>Gets/Sets whether the current soft mask and alpha constant are to be interpreted as
          shape values instead of opacity values.</summary>
        */
        [PDF(VersionEnum.PDF14)]
        public bool AlphaShape
        {
            get => (bool)PdfSimpleObject<object>.GetValue(BaseDataObject[PdfName.AIS], false);
            set => BaseDataObject[PdfName.AIS] = PdfBoolean.Get(value);
        }

        public void ApplyTo(GraphicsState state)
        {
            foreach (PdfName parameterName in BaseDataObject.Keys)
            {
                if (parameterName.Equals(PdfName.Font))
                {
                    state.Font = Font;
                    state.FontSize = FontSize.Value;
                }
                else if (parameterName.Equals(PdfName.CA))
                {
                    if (!AlphaShape)
                        state.StrokeAlpha = StrokeAlpha;
                }
                else if (parameterName.Equals(PdfName.ca))
                {
                    if (!AlphaShape)
                        state.FillAlpha = FillAlpha;
                }
                else if (parameterName.Equals(PdfName.AIS))
                { state.AlphaIsShape = AlphaShape; }
                else if (parameterName.Equals(PdfName.LC))
                { state.LineCap = LineCap.Value; }
                else if (parameterName.Equals(PdfName.D))
                { state.LineDash = LineDash; }
                else if (parameterName.Equals(PdfName.LJ))
                { state.LineJoin = LineJoin.Value; }
                else if (parameterName.Equals(PdfName.LW))
                { state.LineWidth = LineWidth.Value; }
                else if (parameterName.Equals(PdfName.ML))
                { state.MiterLimit = MiterLimit.Value; }
                else if (parameterName.Equals(PdfName.BM))
                { state.BlendMode = BlendMode; }
                else if (parameterName.Equals(PdfName.Type))
                { }
                else if (parameterName.Equals(PdfName.SMask))
                {
                    state.SMask = SMask;
                }
                else
                {

                }
                //TODO:extend supported parameters!!!
            }
        }

        [PDF(VersionEnum.PDF14)]
        public SoftMask SMask
        {
            get => SoftMask.WrapSoftMask(BaseDataObject[PdfName.SMask]);
            set => BaseDataObject[PdfName.SMask] = value.BaseObject;
        }


        /**
          <summary>Gets/Sets the blend mode to be used in the transparent imaging model [PDF:1.7:7.2.4].
          </summary>
        */
        [PDF(VersionEnum.PDF14)]
        public IList<BlendModeEnum> BlendMode
        {
            get
            {
                PdfDirectObject blendModeObject = BaseDataObject[PdfName.BM];
                if (blendModeObject == null)
                    return DefaultBlendMode;

                IList<BlendModeEnum> blendMode = new List<BlendModeEnum>();
                if (blendModeObject is PdfName name)
                { blendMode.Add(BlendModeEnumExtension.Get(name).Value); }
                else // MUST be an array.
                {
                    foreach (PdfDirectObject alternateBlendModeObject in (PdfArray)blendModeObject)
                    { blendMode.Add(BlendModeEnumExtension.Get((PdfName)alternateBlendModeObject).Value); }
                }
                return blendMode;
            }
            set
            {
                PdfDirectObject blendModeObject;
                if (value == null || value.Count == 0)
                { blendModeObject = null; }
                else if (value.Count == 1)
                { blendModeObject = value[0].GetName(); }
                else
                {
                    PdfArray blendModeArray = new PdfArray();
                    foreach (BlendModeEnum blendMode in value)
                    { blendModeArray.Add(blendMode.GetName()); }
                    blendModeObject = blendModeArray;
                }
                BaseDataObject[PdfName.BM] = blendModeObject;
            }
        }

        /**
          <summary>Gets/Sets the nonstroking alpha constant, specifying the constant shape or constant
          opacity value to be used for nonstroking operations in the transparent imaging model
          [PDF:1.7:7.2.6].</summary>
        */
        [PDF(VersionEnum.PDF14)]
        public float? FillAlpha
        {
            get
            {
                var number = BaseDataObject[PdfName.ca] as IPdfNumber;
                return number == null ? null : (float?)number.FloatValue;
            }
            set => BaseDataObject[PdfName.ca] = PdfReal.Get(value);
        }

        /**
          <summary>Gets/Sets the stroking alpha constant, specifying the constant shape or constant
          opacity value to be used for stroking operations in the transparent imaging model
          [PDF:1.7:7.2.6].</summary>
        */
        [PDF(VersionEnum.PDF14)]
        public float? StrokeAlpha
        {
            get
            {
                var number = BaseDataObject[PdfName.CA] as IPdfNumber;
                return number == null ? null : (float?)number.FloatValue;
            }
            set => BaseDataObject[PdfName.CA] = PdfReal.Get(value);
        }

        [PDF(VersionEnum.PDF13)]
        public Font Font
        {
            get
            {
                PdfArray fontObject = (PdfArray)BaseDataObject[PdfName.Font];
                return fontObject != null ? Font.Wrap(fontObject[0]) : null;
            }
            set
            {
                PdfArray fontObject = (PdfArray)BaseDataObject[PdfName.Font];
                if (fontObject == null)
                { fontObject = new PdfArray(PdfObjectWrapper.GetBaseObject(value), PdfInteger.Default); }
                else
                { fontObject[0] = PdfObjectWrapper.GetBaseObject(value); }
                BaseDataObject[PdfName.Font] = fontObject;
            }
        }

        [PDF(VersionEnum.PDF13)]
        public float? FontSize
        {
            get
            {
                PdfArray fontObject = (PdfArray)BaseDataObject[PdfName.Font];
                return fontObject != null ? ((IPdfNumber)fontObject[1]).FloatValue : (float?)null;
            }
            set
            {
                PdfArray fontObject = (PdfArray)BaseDataObject[PdfName.Font];
                if (fontObject == null)
                { fontObject = new PdfArray(null, PdfReal.Get(value)); }
                else
                { fontObject[1] = PdfReal.Get(value); }
                BaseDataObject[PdfName.Font] = fontObject;
            }
        }

        [PDF(VersionEnum.PDF13)]
        public LineCapEnum? LineCap
        {
            get
            {
                PdfInteger lineCapObject = (PdfInteger)BaseDataObject[PdfName.LC];
                return lineCapObject != null ? (LineCapEnum)lineCapObject.RawValue : (LineCapEnum?)null;
            }
            set => BaseDataObject[PdfName.LC] = value.HasValue ? PdfInteger.Get((int)value.Value) : null;
        }

        [PDF(VersionEnum.PDF13)]
        public LineDash LineDash
        {
            get
            {
                PdfArray lineDashObject = (PdfArray)BaseDataObject[PdfName.D];
                return lineDashObject != null ? LineDash.Get((PdfArray)lineDashObject[0], (IPdfNumber)lineDashObject[1]) : null;
            }
            set
            {
                PdfArray lineDashObject = new PdfArray();
                {
                    PdfArray dashArrayObject = new PdfArray();
                    foreach (double dashArrayItem in value.DashArray)
                    { dashArrayObject.Add(PdfReal.Get(dashArrayItem)); }
                    lineDashObject.Add(dashArrayObject);
                    lineDashObject.Add(PdfReal.Get(value.DashPhase));
                }
                BaseDataObject[PdfName.D] = lineDashObject;
            }
        }

        [PDF(VersionEnum.PDF13)]
        public LineJoinEnum? LineJoin
        {
            get
            {
                PdfInteger lineJoinObject = (PdfInteger)BaseDataObject[PdfName.LJ];
                return lineJoinObject != null ? (LineJoinEnum)lineJoinObject.RawValue : (LineJoinEnum?)null;
            }
            set => BaseDataObject[PdfName.LJ] = value.HasValue ? PdfInteger.Get((int)value.Value) : null;
        }

        [PDF(VersionEnum.PDF13)]
        public float? LineWidth
        {
            get
            {
                IPdfNumber lineWidthObject = (IPdfNumber)BaseDataObject[PdfName.LW];
                return lineWidthObject != null ? lineWidthObject.FloatValue : (float?)null;
            }
            set => BaseDataObject[PdfName.LW] = PdfReal.Get(value);
        }

        [PDF(VersionEnum.PDF13)]
        public float? MiterLimit
        {
            get
            {
                IPdfNumber miterLimitObject = (IPdfNumber)BaseDataObject[PdfName.ML];
                return miterLimitObject != null ? miterLimitObject.FloatValue : (float?)null;
            }
            set => BaseDataObject[PdfName.ML] = PdfReal.Get(value);
        }


        #endregion
        #endregion
        #endregion
    }
}