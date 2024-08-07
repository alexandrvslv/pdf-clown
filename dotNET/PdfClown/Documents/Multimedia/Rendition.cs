/*
  Copyright 2012 Stefano Chizzolini. http://www.pdfclown.org

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

using PdfClown.Documents.Interchange.Access;
using PdfClown.Objects;
using PdfClown.Util.Math;

using System;
using System.Collections.Generic;
using SkiaSharp;

namespace PdfClown.Documents.Multimedia
{
    /// <summary>Rendition [PDF:1.7:9.1.2].</summary>
    [PDF(VersionEnum.PDF15)]
    public abstract class Rendition : PdfObjectWrapper<PdfDictionary>, IPdfNamedObjectWrapper
    {
        /// <summary>Rendition viability [PDF:1.7:9.1.2].</summary>
        public class Viability : PdfObjectWrapper<PdfDictionary>
        {
            public Viability(PdfDirectObject baseObject) : base(baseObject)
            { }

            /// <summary>Gets the minimum system's bandwidth (in bits per second).</summary>
            /// <remarks>Equivalent to SMIL's systemBitrate attribute.</remarks>
            public int? Bandwidth
            {
                get => MediaCriteria.GetNInt(PdfName.R);
                set => MediaCriteria.Set(PdfName.R, value);
            }

            /// <summary>Gets the minimum screen color depth (in bits per pixel).</summary>
            /// <remarks>Equivalent to SMIL's systemScreenDepth attribute.</remarks>
            public int? ScreenDepth
            {
                get => MediaCriteria.Get<PdfDictionary>(PdfName.D)?.GetInt(PdfName.V);
            }

            /// <summary>Gets the minimum screen size (in pixels).</summary>
            /// <remarks>Equivalent to SMIL's systemScreenSize attribute.</remarks>
            public SKSize? ScreenSize
            {
                get
                {
                    var screenSizeObject = MediaCriteria.Get<PdfDictionary>(PdfName.Z);
                    if (screenSizeObject == null)
                        return null;

                    var screenSizeValueObject = screenSizeObject.Get<PdfArray>(PdfName.V);
                    return screenSizeValueObject != null
                      ? new SKSize(
                        screenSizeValueObject.GetInt(0),
                        screenSizeValueObject.GetInt(1))
                      : (SKSize?)null;
                }
            }

            /// <summary>Gets the list of supported viewer applications.</summary>
            public Array<SoftwareIdentifier> Renderers => Wrap<Array<SoftwareIdentifier>>(MediaCriteria.GetOrCreate<PdfArray>(PdfName.V));

            /// <summary>Gets the PDF version range supported by the viewer application.</summary>
            public Interval<PdfVersion> Version
            {
                get
                {
                    var pdfVersionArray = MediaCriteria.Get<PdfArray>(PdfName.P);
                    return pdfVersionArray != null && pdfVersionArray.Count > 0
                      ? new Interval<PdfVersion>(
                        PdfVersion.Get((PdfName)pdfVersionArray[0]),
                        pdfVersionArray.Count > 1 ? PdfVersion.Get((PdfName)pdfVersionArray[1]) : null)
                      : null;
                }
            }

            /// <summary>Gets the list of supported languages.</summary>
            /// <remarks>Equivalent to SMIL's systemLanguage attribute.</remarks>
            public IList<LanguageIdentifier> Languages
            {
                get
                {
                    IList<LanguageIdentifier> languages = new List<LanguageIdentifier>();
                    {
                        var languagesObject = (PdfArray)MediaCriteria[PdfName.L];
                        if (languagesObject != null)
                        {
                            foreach (var languageObject in languagesObject)
                            { languages.Add(LanguageIdentifier.Wrap(languageObject)); }
                        }
                    }
                    return languages;
                }
            }

            /// <summary>Gets whether to hear audio descriptions.</summary>
            /// <remarks>Equivalent to SMIL's systemAudioDesc attribute.</remarks>
            public bool AudioDescriptionEnabled
            {
                get => MediaCriteria.GetBool(PdfName.A);
                set => MediaCriteria.Set(PdfName.A, value);
            }

            /// <summary>Gets whether to hear audio overdubs.</summary>
            public bool AudioOverdubEnabled
            {
                get => MediaCriteria.GetBool(PdfName.O);
                set => MediaCriteria.Set(PdfName.O, value);
            }

            /// <summary>Gets whether to see subtitles.</summary>
            public bool SubtitleEnabled
            {
                get => MediaCriteria.GetBool(PdfName.S);
                set => MediaCriteria.Set(PdfName.S, value);
            }

            /// <summary>Gets whether to see text captions.</summary>
            /// <remarks>Equivalent to SMIL's systemCaptions attribute.</remarks>
            public bool TextCaptionEnabled
            {
                get => MediaCriteria.GetBool(PdfName.C);
                set => MediaCriteria.Set(PdfName.C, value);
            }

            private PdfDictionary MediaCriteria => BaseDataObject.Resolve<PdfDictionary>(PdfName.C);

            //TODO:setters!
        }

        /// <summary>Wraps a rendition base object into a rendition object.</summary>
        /// <param name="baseObject">Rendition base object.</param>
        /// <returns>Rendition object associated to the base object.</returns>
        public static Rendition Wrap(PdfDirectObject baseObject)
        {
            if (baseObject == null)
                return null;
            if (baseObject.Wrapper is Rendition rendition)
                return rendition;

            PdfName subtype = (PdfName)((PdfDictionary)baseObject.Resolve())[PdfName.S];
            if (PdfName.MR.Equals(subtype))
                return new MediaRendition(baseObject);
            else if (PdfName.SR.Equals(subtype))
                return new SelectorRendition(baseObject);
            else
                throw new ArgumentException("It doesn't represent a valid clip object.", "baseObject");
        }

        protected Rendition(PdfDocument context, PdfName subtype)
            : base(context, new PdfDictionary
            {
                { PdfName.Type, PdfName.Rendition },
                { PdfName.S, subtype },
            })
        { }

        public Rendition(PdfDirectObject baseObject) : base(baseObject)
        { }

        /// <summary>Gets/Sets the preferred options the renderer should attempt to honor without affecting
        /// its viability [PDF:1.7:9.1.1].</summary>
        public Viability Preferences
        {
            get => Wrap<Viability>(BaseDataObject.GetOrCreate<PdfDictionary>(PdfName.BE));
            set => BaseDataObject[PdfName.BE] = PdfObjectWrapper.GetBaseObject(value);
        }

        /// <summary>Gets/Sets the minimum requirements the renderer must honor in order to be considered
        /// viable [PDF:1.7:9.1.1].</summary>
        public Viability Requirements
        {
            get => Wrap<Viability>(BaseDataObject.GetOrCreate<PdfDictionary>(PdfName.MH));
            set => BaseDataObject[PdfName.MH] = PdfObjectWrapper.GetBaseObject(value);
        }

        public PdfString Name => RetrieveName();

        public PdfDirectObject NamedBaseObject => RetrieveNamedBaseObject();

        protected override PdfString RetrieveName()
        {
              //NOTE: A rendition dictionary is not required to have a name tree entry. When it does, the
              //viewer application should ensure that the name specified in the tree is kept the same as the
              //value of the N entry (for example, if the user interface allows the name to be changed).
            return (PdfString)BaseDataObject[PdfName.N];
        }
    }
}