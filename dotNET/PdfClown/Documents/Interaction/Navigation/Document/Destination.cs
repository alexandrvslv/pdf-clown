/*
  Copyright 2006-2015 Stefano Chizzolini. http://www.pdfclown.org

  Contributors:
    * Stefano Chizzolini (original code developer, http://www.stefanochizzolini.it)
    * Furkan Duman (bug reporter [FIX:66], https://sourceforge.net/u/fduman/)

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
using PdfClown.Util;
using SkiaSharp;
using System;

namespace PdfClown.Documents.Interaction.Navigation
{
    /// <summary>Interaction target [PDF:1.6:8.2.1].</summary>
    /// <remarks>
    ///   It represents a particular view of a document, consisting of the following items:
    ///   <list type="bullet">
    ///     <item>the page of the document to be displayed;</item>
    ///     <item>the location of the document window on that page;</item>
    ///     <item>the magnification (zoom) factor to use when displaying the page.</item>
    ///   </list>
    /// </remarks>
    [PDF(VersionEnum.PDF10)]
    public abstract class Destination : PdfObjectWrapper<PdfArray>, IPdfNamedObjectWrapper, ITextDisplayable
    {
        /// <summary>Destination mode [PDF:1.6:8.2.1].</summary>
        public enum ModeEnum
        {
            /// <summary>Display the page at the given upper-left position,
            /// applying the given magnification.</summary>
            /// <remarks>
            ///   View parameters:
            ///   <list type="number">
            ///     <item>left coordinate</item>
            ///     <item>top coordinate</item>
            ///     <item>zoom</item>
            ///   </list>
            /// </remarks>
            XYZ,
            /// <summary>Display the page with its contents magnified just enough to fit
            /// the entire page within the window both horizontally and vertically.</summary>
            /// <remarks>No view parameters.</remarks>
            Fit,
            /// <summary>Display the page with the vertical coordinate <code>top</code> positioned
            /// at the top edge of the window and the contents of the page magnified
            /// just enough to fit the entire width of the page within the window.</summary>
            /// <remarks>
            ///   View parameters:
            ///   <list type="number">
            ///     <item>top coordinate</item>
            ///   </list>
            /// </remarks>
            FitHorizontal,
            /// <summary>Display the page with the horizontal coordinate <code>left</code> positioned
            /// at the left edge of the window and the contents of the page magnified
            /// just enough to fit the entire height of the page within the window.</summary>
            /// <remarks>
            ///   View parameters:
            ///   <list type="number">
            ///     <item>left coordinate</item>
            ///   </list>
            /// </remarks>
            FitVertical,
            /// <summary>Display the page with its contents magnified just enough to fit
            /// the rectangle specified by the given coordinates entirely
            /// within the window both horizontally and vertically.</summary>
            /// <remarks>
            ///   View parameters:
            ///   <list type="number">
            ///     <item>left coordinate</item>
            ///     <item>bottom coordinate</item>
            ///     <item>right coordinate</item>
            ///     <item>top coordinate</item>
            ///   </list>
            /// </remarks>
            FitRectangle,
            /// <summary>Display the page with its contents magnified just enough to fit
            /// its bounding box entirely within the window both horizontally and vertically.</summary>
            /// <remarks>No view parameters.</remarks>
            FitBoundingBox,
            /// <summary>Display the page with the vertical coordinate <code>top</code> positioned
            /// at the top edge of the window and the contents of the page magnified
            /// just enough to fit the entire width of its bounding box within the window.</summary>
            /// <remarks>
            ///   View parameters:
            ///   <list type="number">
            ///     <item>top coordinate</item>
            ///   </list>
            /// </remarks>
            FitBoundingBoxHorizontal,
            /// <summary>Display the page with the horizontal coordinate <code>left</code> positioned
            /// at the left edge of the window and the contents of the page magnified
            /// just enough to fit the entire height of its bounding box within the window.</summary>
            /// <remarks>
            ///   View parameters:
            ///   <list type="number">
            ///     <item>left coordinate</item>
            ///   </list>
            /// </remarks>
            FitBoundingBoxVertical
        }

        /// <summary>Wraps a destination base object into a destination object.</summary>
        /// <param name="baseObject">Destination base object.</param>
        /// <returns>Destination object associated to the base object.</returns>
        public static Destination Wrap(PdfDirectObject baseObject)
        {
            if (baseObject == null)
                return null;
            if (baseObject.Wrapper is Destination destination)
                return destination;

            var dataObject = (PdfArray)baseObject.Resolve();
            var pageObject = dataObject[0];
            if (pageObject is PdfReference)
                return new LocalDestination(baseObject);
            else if (pageObject is PdfInteger)
                return new RemoteDestination(baseObject);
            else
                throw new ArgumentException("Not a valid destination object.", "baseObject");
        }

        /// <summary>Creates a new destination within the given document context.</summary>
        /// <param name="context">Document context.</param>
        /// <param name="page">Page reference. It may be either a <see cref="Page"/> or a page index (int).
        /// </param>
        /// <param name="mode">Destination mode.</param>
        /// <param name="location">Destination location.</param>
        /// <param name="zoom">Magnification factor to use when displaying the page.</param>
        protected Destination(PdfDocument context, object page, ModeEnum mode, object location, double? zoom)
            : base(context, new PdfArray(2) { (string)null, (string)null })
        {
            Page = page;
            Mode = mode;
            Location = location;
            Zoom = zoom;
        }

        public Destination(PdfDirectObject baseObject) : base(baseObject)
        { }

        /// <summary>Gets/Sets the page location.</summary>
        public object Location
        {
            get
            {
                // [FIX:66] Invalid cast exception on number unboxing.
                switch (Mode)
                {
                    case ModeEnum.FitBoundingBoxHorizontal:
                    case ModeEnum.FitBoundingBoxVertical:
                    case ModeEnum.FitHorizontal:
                    case ModeEnum.FitVertical:
                        return BaseDataObject.GetFloat(2, float.NaN);
                    case ModeEnum.FitRectangle:
                        {
                            float left = BaseDataObject.GetFloat(2, float.NaN);
                            float top = BaseDataObject.GetFloat(5, float.NaN);
                            float width = BaseDataObject.GetFloat(4, float.NaN) - left;
                            float height = BaseDataObject.GetFloat(3, float.NaN) - top;
                            return SKRect.Create(left, top, width, height);
                        }
                    case ModeEnum.XYZ:
                        return new SKPoint(
                          BaseDataObject.GetFloat(2, float.NaN),
                          BaseDataObject.GetFloat(3, float.NaN));
                    default:
                        return null;
                }
            }
            set
            {
                PdfArray baseDataObject = BaseDataObject;
                switch (Mode)
                {
                    case ModeEnum.FitBoundingBoxHorizontal:
                    case ModeEnum.FitBoundingBoxVertical:
                    case ModeEnum.FitHorizontal:
                    case ModeEnum.FitVertical:
                        baseDataObject.Set(2, Convert.ToDouble(value));
                        break;
                    case ModeEnum.FitRectangle:
                        {
                            SKRect rectangle = (SKRect)value;
                            baseDataObject.Set(2, rectangle.Left);
                            baseDataObject.Set(3, rectangle.Top);
                            baseDataObject.Set(4, rectangle.Right);
                            baseDataObject.Set(5, rectangle.Bottom);
                            break;
                        }
                    case ModeEnum.XYZ:
                        {
                            SKPoint point = (SKPoint)value;
                            baseDataObject.Set(2, point.X);
                            baseDataObject.Set(3, point.Y);
                            break;
                        }
                    default:
                        /* NOOP */
                        break;
                }
            }
        }

        /// <summary>Gets the destination mode.</summary>
        public ModeEnum Mode
        {
            get => ModeEnumExtension.Get(BaseDataObject.Get<PdfName>(1)).Value;
            set
            {
                PdfArray baseDataObject = BaseDataObject;

                baseDataObject[1] = value.GetName();

                // Adjusting parameter list...
                int parametersCount;
                switch (value)
                {
                    case ModeEnum.Fit:
                    case ModeEnum.FitBoundingBox:
                        parametersCount = 2;
                        break;
                    case ModeEnum.FitBoundingBoxHorizontal:
                    case ModeEnum.FitBoundingBoxVertical:
                    case ModeEnum.FitHorizontal:
                    case ModeEnum.FitVertical:
                        parametersCount = 3;
                        break;
                    case ModeEnum.XYZ:
                        parametersCount = 5;
                        break;
                    case ModeEnum.FitRectangle:
                        parametersCount = 6;
                        break;
                    default:
                        throw new NotSupportedException("Mode unknown: " + value);
                }
                while (baseDataObject.Count < parametersCount)
                { baseDataObject.AddDirect(null); }
                while (baseDataObject.Count > parametersCount)
                { baseDataObject.RemoveAt(baseDataObject.Count - 1); }
            }
        }

        /// <summary>Gets/Sets the target page reference.</summary>
        public abstract object Page
        {
            get;
            set;
        }

        /// <summary>Gets the magnification factor to use when displaying the page.</summary>
        public double? Zoom
        {
            get
            {
                switch (Mode)
                {
                    case ModeEnum.XYZ:
                        return PdfSimpleObject<object>.GetDoubleValue(BaseDataObject[4]);
                    default:
                        return null;
                }
            }
            set
            {
                switch (Mode)
                {
                    case ModeEnum.XYZ:
                        BaseDataObject.Set(4, value);
                        break;
                    default:
                        /* NOOP */
                        break;
                }
            }
        }

        public PdfString Name => RetrieveName();

        public PdfDirectObject NamedBaseObject => RetrieveNamedBaseObject();

        public virtual string GetDisplayName()
        {
            return Page is PdfPage page ? "Page " + page.Number : string.Empty;
        }
    }

    internal static class ModeEnumExtension
    {
        private static readonly BiDictionary<Destination.ModeEnum, PdfName> codes;

        static ModeEnumExtension()
        {
            codes = new BiDictionary<Destination.ModeEnum, PdfName>
            {
                [Destination.ModeEnum.Fit] = PdfName.Fit,
                [Destination.ModeEnum.FitBoundingBox] = PdfName.FitB,
                [Destination.ModeEnum.FitBoundingBoxHorizontal] = PdfName.FitBH,
                [Destination.ModeEnum.FitBoundingBoxVertical] = PdfName.FitBV,
                [Destination.ModeEnum.FitHorizontal] = PdfName.FitH,
                [Destination.ModeEnum.FitRectangle] = PdfName.FitR,
                [Destination.ModeEnum.FitVertical] = PdfName.FitV,
                [Destination.ModeEnum.XYZ] = PdfName.XYZ
            };
        }

        public static Destination.ModeEnum? Get(PdfName name)
        {
            if (name == null)
                return null;

            Destination.ModeEnum? mode = codes.GetKey(name);
            if (!mode.HasValue)
                throw new NotSupportedException("Mode unknown: " + name);

            return mode;
        }

        public static PdfName GetName(this Destination.ModeEnum mode) => codes[mode];
    }
}