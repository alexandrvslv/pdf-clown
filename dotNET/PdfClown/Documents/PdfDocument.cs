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

using PdfClown.Documents.Contents;
using PdfClown.Documents.Contents.Fonts;
using PdfClown.Documents.Contents.Fonts.TTF;
using PdfClown.Documents.Contents.Layers;
using PdfClown.Documents.Interaction.Annotations;
using PdfClown.Documents.Interaction.Forms;
using PdfClown.Documents.Interaction.Forms.Signature;
using PdfClown.Documents.Interaction.Navigation;
using PdfClown.Documents.Interaction.Viewer;
using PdfClown.Documents.Interchange.Metadata;
using PdfClown.Objects;
using SkiaSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace PdfClown.Documents
{
    /**
      <summary>PDF document [PDF:1.6::3.6.1].</summary>
    */
    [PDF(VersionEnum.PDF10)]
    public sealed class PdfDocument : PdfObjectWrapper<PdfDictionary>, IAppDataHolder
    {
        public static T Resolve<T>(PdfDirectObject baseObject) where T : PdfObjectWrapper
        {
            if (typeof(Destination).IsAssignableFrom(typeof(T)))
                return Destination.Wrap(baseObject) as T;
            else
                throw new NotSupportedException("Type '" + typeof(T).Name + "' wrapping is not supported.");
        }

        internal Dictionary<PdfDirectObject, object> Cache = new();
        internal ConcurrentDictionary<FontName, FontType1> Type1FontCache = new();
        internal ConcurrentDictionary<TrueTypeFont, FontType0> Type0FontCache = new();

        private DocumentConfiguration configuration;
        public readonly ManualResetEventSlim LockObject = new ManualResetEventSlim(true);

        internal PdfDocument(PdfFile context) :
            base(context, new PdfDictionary(1) { { PdfName.Type, PdfName.Catalog } })
        {
            configuration = new DocumentConfiguration(this);

            // Attach the document catalog to the file trailer!
            context.Trailer[PdfName.Root] = BaseObject;

            // Pages collection.
            this.Pages = new Pages(this);

            // Default page size.
            PageSize = PageFormat.GetSize();

            // Default resources collection.
            Resources = new Resources(this);
        }

        internal PdfDocument(PdfDirectObject baseObject)// Catalog.
            : base(baseObject)
        { configuration = new DocumentConfiguration(this); }

        /**
          <summary>Gets/Sets the document's behavior in response to trigger events.</summary>
        */
        [PDF(VersionEnum.PDF14)]
        public DocumentActions Actions
        {
            get => Wrap<DocumentActions>(BaseDataObject.Get<PdfDictionary>(PdfName.AA));
            set => BaseDataObject[PdfName.AA] = PdfObjectWrapper.GetBaseObject(value);
        }

        /**
          <summary>Gets the article threads.</summary>
        */
        [PDF(VersionEnum.PDF11)]
        public Articles Articles
        {
            get => Wrap<Articles>(BaseDataObject.Get<PdfArray>(PdfName.Threads, false));
            set => BaseDataObject[PdfName.Threads] = PdfObjectWrapper.GetBaseObject(value);
        }

        /**
          <summary>Gets/Sets the bookmark collection.</summary>
        */
        public Bookmarks Bookmarks
        {
            get => Wrap<Bookmarks>(BaseDataObject.Get<PdfDictionary>(PdfName.Outlines, false));
            set => BaseDataObject[PdfName.Outlines] = PdfObjectWrapper.GetBaseObject(value);
        }

        public override object Clone(PdfDocument context)
        {
            throw new NotImplementedException();
        }

        /**
          <summary>Gets/Sets the configuration of this document.</summary>
        */
        public DocumentConfiguration Configuration
        {
            get => configuration;
            set => configuration = value;
        }

        /**
          <summary>Deletes the object from this document context.</summary>
        */
        public void Exclude(PdfObjectWrapper obj)
        {
            if (obj.File != File)
                return;

            obj.Delete();
        }

        /**
          <summary>Deletes the objects from this document context.</summary>
        */
        public void Exclude<T>(ICollection<T> objs) where T : PdfObjectWrapper
        {
            foreach (T obj in objs)
            {
                Exclude(obj);
            }
        }

        /**
          <summary>Gets/Sets the interactive form (AcroForm).</summary>
        */
        [PDF(VersionEnum.PDF12)]
        public Form Form
        {
            get => Wrap<Form>(BaseDataObject.Get<PdfDictionary>(PdfName.AcroForm));
            set => BaseDataObject[PdfName.AcroForm] = PdfObjectWrapper.GetBaseObject(value);
        }

        /**
          <summary>Gets/Sets common document metadata.</summary>
        */
        public Information Information
        {
            get => Wrap<Information>(File.Trailer.Get<PdfDictionary>(PdfName.Info, false));
            set => File.Trailer[PdfName.Info] = PdfObjectWrapper.GetBaseObject(value);
        }

        /**
          <summary>Gets/Sets the optional content properties.</summary>
        */
        [PDF(VersionEnum.PDF15)]
        public LayerDefinition Layer
        {
            get => Wrap<LayerDefinition>(BaseDataObject.Get<PdfDictionary>(PdfName.OCProperties));
            set
            {
                CheckCompatibility("Layer");
                BaseDataObject[PdfName.OCProperties] = PdfObjectWrapper.GetBaseObject(value);
            }
        }

        /**
          <summary>Gets/Sets the name dictionary.</summary>
        */
        [PDF(VersionEnum.PDF12)]
        public Names Names
        {
            get => Wrap<Names>(BaseDataObject.Get<PdfDictionary>(PdfName.Names));
            set => BaseDataObject[PdfName.Names] = PdfObjectWrapper.GetBaseObject(value);
        }

        /**
          <summary>Gets/Sets the page label ranges.</summary>
        */
        [PDF(VersionEnum.PDF13)]
        public PageLabels PageLabels
        {
            get => PageLabels.Wrap(BaseDataObject.Get<PdfDictionary>(PdfName.PageLabels));
            set
            {
                CheckCompatibility("PageLabels");
                BaseDataObject[PdfName.PageLabels] = PdfObjectWrapper.GetBaseObject(value);
            }
        }

        /**
          <summary>Gets/Sets the page collection.</summary>
        */
        public Pages Pages
        {
            get => Wrap<Pages>(BaseDataObject[PdfName.Pages]);
            set => BaseDataObject[PdfName.Pages] = PdfObjectWrapper.GetBaseObject(value);
        }

        /**
          <summary>Gets/Sets the default page size [PDF:1.6:3.6.2].</summary>
        */
        public SKSize? PageSize
        {
            get
            {
                PdfArray mediaBox = MediaBox;
                return mediaBox != null
                  ? new SKSize(
                    (int)mediaBox.GetDouble(2),
                    (int)mediaBox.GetDouble(3))
                  : (SKSize?)null;
            }
            set
            {
                PdfArray mediaBox = MediaBox;
                if (mediaBox == null)
                {
                    // Create default media box!
                    mediaBox = new PdfClown.Objects.Rectangle(0, 0, 0, 0).BaseDataObject;
                    // Assign the media box to the document!
                    ((PdfDictionary)BaseDataObject.Resolve(PdfName.Pages))[PdfName.MediaBox] = mediaBox;
                }
                mediaBox[2] = PdfReal.Get(value.Value.Width);
                mediaBox[3] = PdfReal.Get(value.Value.Height);
            }
        }

        /**
          <summary>Gets the document size, that is the maximum page dimensions across the whole document.
          </summary>
          <seealso cref="PageSize"/>
        */
        public SKSize GetSize()
        {
            float height = 0, width = 0;
            foreach (var page in Pages)
            {
                SKSize pageSize = page.Size;
                height = Math.Max(height, pageSize.Height);
                width = Math.Max(width, pageSize.Width);
            }
            return new SKSize(width, height);
        }

        /**
          <summary>Clones the object within this document context.</summary>
        */
        public PdfObjectWrapper Include(PdfObjectWrapper obj)
        {
            if (obj.File == File)
                return obj;

            return (PdfObjectWrapper)obj.Clone(this);
        }

        /**
          <summary>Clones the collection objects within this document context.</summary>
        */
        public ICollection<T> Include<T>(ICollection<T> objs) where T : PdfObjectWrapper
        {
            List<T> includedObjects = new List<T>(objs.Count);
            foreach (T obj in objs)
            { includedObjects.Add((T)Include(obj)); }

            return (ICollection<T>)includedObjects;
        }

        /**
          <summary>Registers a named object.</summary>
          <param name="name">Object name.</param>
          <param name="object">Named object.</param>
          <returns>Registered named object.</returns>
        */
        public T Register<T>(PdfString name, T @object) where T : PdfObjectWrapper
        {
            PdfObjectWrapper namedObjects = Names.Get(@object.GetType());
            namedObjects.GetType().GetMethod("set_Item", BindingFlags.Public | BindingFlags.Instance).Invoke(namedObjects, new object[] { name, @object });
            return @object;
        }

        /**
          <summary>Forces a named base object to be expressed as its corresponding high-level
          representation.</summary>
        */
        public T ResolveName<T>(PdfDirectObject namedBaseObject) where T : PdfObjectWrapper
        {
            if (namedBaseObject is PdfString) // Named object.
                return Names.Get<T>((PdfString)namedBaseObject);
            else // Explicit object.
                return Resolve<T>(namedBaseObject);
        }

        internal void ClearCache()
        {
            foreach (var entry in Cache)
            {
                if (entry.Value is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            Type0FontCache.Clear();
            Type1FontCache.Clear();
            Cache.Clear();
        }

        /**
          <summary>Gets/Sets the default resource collection [PDF:1.6:3.6.2].</summary>
          <remarks>The default resource collection is used as last resort by every page that doesn't
          reference one explicitly (and doesn't reference an intermediate one implicitly).</remarks>
        */
        public Resources Resources
        {
            get => Wrap<Resources>(((PdfDictionary)BaseDataObject.Resolve(PdfName.Pages)).Get<PdfDictionary>(PdfName.Resources));
            set => ((PdfDictionary)BaseDataObject.Resolve(PdfName.Pages))[PdfName.Resources] = PdfObjectWrapper.GetBaseObject(value);
        }

        /**
          <summary>Gets/Sets the version of the PDF specification this document conforms to.</summary>
        */
        [PDF(VersionEnum.PDF14)]
        public PdfVersion Version
        {
            get
            {
                /*
                  NOTE: If the header specifies a later version, or if this entry is absent, the document
                  conforms to the version specified in the header.
                */
                PdfVersion fileVersion = File.Version;

                PdfName versionObject = (PdfName)BaseDataObject[PdfName.Version];
                if (versionObject == null)
                    return fileVersion;

                PdfVersion version = PdfVersion.Get(versionObject);
                if (File.Reader == null)
                    return version;

                return (version.CompareTo(fileVersion) > 0 ? version : fileVersion);
            }
            set => BaseDataObject[PdfName.Version] = PdfName.Get(value);
        }

        /**
          <summary>Gets the way the document is to be presented.</summary>
        */
        public ViewerPreferences ViewerPreferences
        {
            get => Wrap<ViewerPreferences>(BaseDataObject.Get<PdfDictionary>(PdfName.ViewerPreferences));
            set => BaseDataObject[PdfName.ViewerPreferences] = PdfObjectWrapper.GetBaseObject(value);
        }

        public AppDataCollection AppData => AppDataCollection.Wrap(BaseDataObject.Get<PdfDictionary>(PdfName.PieceInfo), this);

        /**
          <summary>Gets the default media box.</summary>
        */
        private PdfArray MediaBox =>
                //NOTE: Document media box MUST be associated with the page-tree root node in order to be
                //inheritable by all the pages.
                (PdfArray)((PdfDictionary)BaseDataObject.Resolve(PdfName.Pages)).Resolve(PdfName.MediaBox);



        public AppData GetAppData(PdfName appName) => AppData.Ensure(appName);

        public DateTime? ModificationDate => Information.ModificationDate;

        public void Touch(PdfName appName) => Touch(appName, DateTime.Now);

        public void Touch(PdfName appName, DateTime modificationDate)
        {
            GetAppData(appName).ModificationDate = modificationDate;
            Information.ModificationDate = modificationDate;
        }

        internal void RegisterTrueTypeFontForClosing(TrueTypeFont ttf)
        {
            throw new NotImplementedException();
        }

        public bool HasSignatures => BaseDataObject.ContainsKey(PdfName.AcroForm) && Form.Fields.OfType<SignatureField>().Any();

        public bool HasSignatureDictionaries => GetSignatureDictionaries().Any();

        internal IEnumerable<SignatureDictionary> GetSignatureDictionaries()
        {
            if (!BaseDataObject.ContainsKey(PdfName.AcroForm))
                yield break;
            foreach (var sigField in Form.Fields.OfType<SignatureField>())
            {
                if (sigField.SignatureDictionary != null)
                    yield return sigField.SignatureDictionary;
            }
        }

        public Annotation FindAnnotation(string name, int? pageIndex = null)
        {
            var annotation = (Annotation)null;
            if (pageIndex == null)
            {
                foreach (var page in Pages)
                {
                    annotation = page.Annotations[name];
                    if (annotation != null)
                        return annotation;
                }
            }
            else
            {
                var page = Pages[(int)pageIndex];
                return page?.Annotations[name];
            }
            return null;
        }
    }
}