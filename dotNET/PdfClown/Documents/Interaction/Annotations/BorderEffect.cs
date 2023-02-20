/*
  Copyright 2015 Stefano Chizzolini. http://www.pdfclown.org

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

using PdfClown.Bytes;
using PdfClown.Documents;
using PdfClown.Documents.Contents;
using PdfClown.Objects;
using SkiaSharp;
using System;
using System.Collections.Generic;

namespace PdfClown.Documents.Interaction.Annotations
{
    /**
      <summary>Border effect [PDF:1.6:8.4.3].</summary>
    */
    [PDF(VersionEnum.PDF15)]
    public sealed class BorderEffect : PdfObjectWrapper<PdfDictionary>, IEquatable<BorderEffect>
    {
        #region static

        #region fields
        private static readonly double DefaultIntensity = 0;
        private static readonly BorderEffectType DefaultType = BorderEffectType.None;

        private static readonly Dictionary<BorderEffectType, PdfName> TypeEnumCodes;

        #endregion

        #region constructors
        static BorderEffect()
        {
            TypeEnumCodes = new Dictionary<BorderEffectType, PdfName>
            {
                [BorderEffectType.None] = PdfName.S,
                [BorderEffectType.Cloudy] = PdfName.C
            };
        }
        #endregion

        #region interface
        #region private
        /**
          <summary>Gets the code corresponding to the given value.</summary>
        */
        private static PdfName ToCode(BorderEffectType value)
        {
            return TypeEnumCodes[value];
        }

        /**
          <summary>Gets the style corresponding to the given value.</summary>
        */
        private static BorderEffectType ToTypeEnum(IPdfString value)
        {
            if (value == null)
                return DefaultType;
            foreach (KeyValuePair<BorderEffectType, PdfName> type in TypeEnumCodes)
            {
                if (string.Equals(type.Value.StringValue, value.StringValue, StringComparison.Ordinal))
                    return type.Key;
            }
            return DefaultType;
        }
        #endregion
        #endregion
        #endregion

        #region dynamic
        #region constructors
        /**
          <summary>Creates a non-reusable instance.</summary>
        */
        public BorderEffect(BorderEffectType type) : this(null, type)
        { }

        /**
          <summary>Creates a non-reusable instance.</summary>
        */
        public BorderEffect(BorderEffectType type, double intensity) : this(null, type, intensity)
        { }

        /**
          <summary>Creates a reusable instance.</summary>
        */
        public BorderEffect(Document context, BorderEffectType type) : this(context, type, DefaultIntensity)
        { }

        /**
          <summary>Creates a reusable instance.</summary>
        */
        public BorderEffect(Document context, BorderEffectType type, double intensity) : base(context, new PdfDictionary())
        {
            Type = type;
            Intensity = intensity;
        }

        public BorderEffect(PdfDirectObject baseObject) : base(baseObject) { }
        #endregion

        #region interface
        #region public
        /**
          <summary>Gets/Sets the effect intensity.</summary>
          <returns>Value in the range 0-2.</returns>
        */
        public double Intensity
        {
            get => BaseDataObject.GetDouble(PdfName.I, DefaultIntensity);
            set => BaseDataObject[PdfName.I] = value != DefaultIntensity ? PdfReal.Get(value) : null;
        }

        /**
          <summary>Gets/Sets the effect type.</summary>
        */
        public BorderEffectType Type
        {
            get => ToTypeEnum((IPdfString)BaseDataObject[PdfName.S]);
            set => BaseDataObject[PdfName.S] = value != DefaultType ? ToCode(value) : null;
        }

        public SKPath Apply(SKPaint paint, SKPath targetPath = null)
        {
            if (Type == BorderEffectType.Cloudy)
            {
                var intensity = (float)Intensity;
                const int r = 5;
                var clode = new SKRect(-r * intensity, -r * intensity, r * intensity, r * intensity);

                if (paint.IsStroke)
                {
                    using (var path = new SKPath())
                    {
                        var clode2 = clode;
                        clode2.Inflate(-1, -1);
                        path.AddArc(clode, 30, -175);
                        path.ArcTo(clode2, -155, 175, false);
                        path.Close();

                        paint.PathEffect = SKPathEffect.Create1DPath(path, intensity * (r * 1.45F), intensity, SKPath1DPathEffectStyle.Rotate);
                    }
                }
                else
                {
                    using (var path = new SKPath())
                    {
                        path.AddOval(clode);
                        paint.PathEffect = SKPathEffect.Create1DPath(path, intensity * (r * 1.45F), intensity, SKPath1DPathEffectStyle.Rotate);
                    }

                    if (targetPath != null)
                    {
                        var dest = paint.GetFillPath(targetPath, 1);
                        paint.PathEffect = null;

                        dest.FillType = SKPathFillType.Winding;
                        dest = dest.Op(targetPath, SKPathOp.Union);
                        return dest;
                    }
                }
            }
            return targetPath;
        }

        public bool Equals(BorderEffect other)
        {
            if (other == null)
                return false;
            return Type == other.Type
                && Intensity.Equals(other.Intensity);
        }

        #endregion
        #endregion
        #endregion
    }

    /**
      <summary>Border effect type [PDF:1.6:8.4.3].</summary>
    */
    public enum BorderEffectType
    {
        /**
          No effect.
        */
        None,
        /**
          Cloudy.
        */
        Cloudy
    }

}