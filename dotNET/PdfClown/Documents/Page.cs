/*
  Copyright 2006-2015 Stefano Chizzolini. http://www.pdfclown.org

  Contributors:
    * Stefano Chizzolini (original code developer, http://www.stefanochizzolini.it)
    * Andreas Pinter (bug reporter [FIX:53], https://sourceforge.net/u/drunal/)

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
using PdfClown.Documents.Contents;
using PdfClown.Documents.Contents.Composition;
using PdfClown.Documents.Contents.Objects;
using xObjects = PdfClown.Documents.Contents.XObjects;
using PdfClown.Documents.Interaction.Navigation;
using PdfClown.Documents.Interchange.Metadata;
using PdfClown.Files;
using PdfClown.Objects;

using System;
using System.Collections.Generic;
using SkiaSharp;
using PdfClown.Documents.Contents.XObjects;

namespace PdfClown.Documents
{
    /**
      <summary>Document page [PDF:1.6:3.6.2].</summary>
    */
    [PDF(VersionEnum.PDF10)]
    public class Page : PdfObjectWrapper<PdfDictionary>, IContentContext
    {
        /*
          NOTE: Inheritable attributes are NOT early-collected, as they are NOT part
          of the explicit representation of a page. They are retrieved every time
          clients call.
        */
        #region types
        /**
          <summary>Annotations tab order [PDF:1.6:3.6.2].</summary>
        */
        [PDF(VersionEnum.PDF15)]
        public enum TabOrderEnum
        {
            /**
              <summary>Row order.</summary>
            */
            Row,
            /**
              <summary>Column order.</summary>
            */
            Column,
            /**
              <summary>Structure order.</summary>
            */
            Structure
        };
        #endregion

        #region static
        #region fields
        public static readonly ISet<PdfName> InheritableAttributeKeys;

        private static readonly Dictionary<TabOrderEnum, PdfName> TabOrderEnumCodes;
        private SKMatrix? initialMatrix;
        private SKMatrix? inInitialMatrix;
        private SKMatrix? rotateMatrix;
        private SKMatrix? inRotateMatrix;
        #endregion

        #region constructors
        static Page()
        {
            InheritableAttributeKeys = new HashSet<PdfName>
            {
                PdfName.Resources,
                PdfName.MediaBox,
                PdfName.CropBox,
                PdfName.Rotate
            };

            TabOrderEnumCodes = new Dictionary<TabOrderEnum, PdfName>
            {
                [TabOrderEnum.Row] = PdfName.R,
                [TabOrderEnum.Column] = PdfName.C,
                [TabOrderEnum.Structure] = PdfName.S
            };
        }
        #endregion

        #region interface
        #region public
        /**
          <summary>Gets the attribute value corresponding to the specified key, possibly recurring to
          its ancestor nodes in the page tree.</summary>
          <param name="pageObject">Page object.</param>
          <param name="key">Attribute key.</param>
        */
        public static PdfDirectObject GetInheritableAttribute(PdfDictionary pageObject, PdfName key)
        {
            /*
              NOTE: It moves upward until it finds the inherited attribute.
            */
            PdfDictionary dictionary = pageObject;
            while (true)
            {
                PdfDirectObject entry = dictionary[key];
                if (entry != null)
                    return entry;

                dictionary = (PdfDictionary)dictionary.Resolve(PdfName.Parent);
                if (dictionary == null)
                {
                    // Isn't the page attached to the page tree?
                    /* NOTE: This condition is illegal. */
                    if (pageObject[PdfName.Parent] == null)
                        throw new Exception("Inheritable attributes unreachable: Page objects MUST be inserted into their document's Pages collection before being used.");

                    return null;
                }
            }
        }

        #endregion

        #region private
        /**
          <summary>Gets the code corresponding to the given value.</summary>
        */
        private static PdfName ToCode(TabOrderEnum value)
        { return TabOrderEnumCodes[value]; }

        /**
          <summary>Gets the tab order corresponding to the given value.</summary>
        */
        private static TabOrderEnum ToTabOrderEnum(PdfName value)
        {
            foreach (KeyValuePair<TabOrderEnum, PdfName> tabOrder in TabOrderEnumCodes)
            {
                if (tabOrder.Value.Equals(value))
                    return tabOrder.Key;
            }
            return TabOrderEnum.Row;
        }
        #endregion
        #endregion
        #endregion

        #region dynamic
        #region constructors
        /**
          <summary>Creates a new page within the specified document context, using the default size.
          </summary>
          <param name="context">Document where to place this page.</param>
        */
        public Page(Document context) : this(context, null)
        { }

        /**
          <summary>Creates a new page within the specified document context.</summary>
          <param name="context">Document where to place this page.</param>
          <param name="size">Page size. In case of <code>null</code>, uses the default SKSize.</param>
        */
        public Page(Document context, SKSize? size) : base(
            context,
            new PdfDictionary(
              new PdfName[] { PdfName.Type, PdfName.Contents },
              new PdfDirectObject[] { PdfName.Page, context.File.Register(new PdfStream()) }
              )
            )
        {
            if (size.HasValue)
            { size = size.Value; }
        }

        public Page(PdfDirectObject baseObject) : base(baseObject)
        { }
        #endregion

        #region interface
        #region public
        /**
          <summary>Gets/Sets the page's behavior in response to trigger events.</summary>
        */
        [PDF(VersionEnum.PDF12)]
        public PageActions Actions
        {
            get => Wrap<PageActions>(BaseDataObject.Get<PdfDictionary>(PdfName.AA));
            set => BaseDataObject[PdfName.AA] = PdfObjectWrapper.GetBaseObject(value);
        }

        /**
          <summary>Gets/Sets the annotations associated to the page.</summary>
        */
        public PageAnnotations Annotations
        {
            get => PageAnnotations.Wrap(BaseDataObject.Get<PdfArray>(PdfName.Annots), this);
            set => BaseDataObject[PdfName.Annots] = PdfObjectWrapper.GetBaseObject(value);
        }

        /**
          <summary>Gets/Sets the extent of the page's meaningful content (including potential white space)
          as intended by the page's creator [PDF:1.7:10.10.1].</summary>
          <seealso cref="CropBox"/>
        */
        [PDF(VersionEnum.PDF13)]
        public SKRect? ArtBox
        {
            get
            {
                /*
                  NOTE: The default value is the page's crop box.
                */
                PdfDirectObject artBoxObject = GetInheritableAttribute(PdfName.ArtBox);
                return artBoxObject != null ? Wrap<Rectangle>(artBoxObject).ToRect() : CropBox;
            }
            set => BaseDataObject[PdfName.ArtBox] = (value.HasValue ? new Rectangle(value.Value).BaseDataObject : null);
        }

        /**
          <summary>Gets the page article beads.</summary>
        */
        public PageArticleElements ArticleElements => PageArticleElements.Wrap(BaseDataObject.Get<PdfArray>(PdfName.B), this);

        /**
          <summary>Gets/Sets the region to which the contents of the page should be clipped when output
          in a production environment [PDF:1.7:10.10.1].</summary>
          <remarks>
            <para>This may include any extra bleed area needed to accommodate the physical limitations of
            cutting, folding, and trimming equipment. The actual printed page may include printing marks
            that fall outside the bleed box.</para>
          </remarks>
          <seealso cref="CropBox"/>
        */
        [PDF(VersionEnum.PDF13)]
        public SKRect? BleedBox
        {
            get
            {
                /*
                  NOTE: The default value is the page's crop box.
                */
                PdfDirectObject bleedBoxObject = GetInheritableAttribute(PdfName.BleedBox);
                return bleedBoxObject != null ? Wrap<Rectangle>(bleedBoxObject).ToRect() : CropBox;
            }
            set => BaseDataObject[PdfName.BleedBox] = (value.HasValue ? new Rectangle(value.Value).BaseDataObject : null);
        }

        /**
          <summary>Gets/Sets the region to which the contents of the page are to be clipped (cropped)
          when displayed or printed [PDF:1.7:10.10.1].</summary>
          <remarks>
            <para>Unlike the other boxes, the crop box has no defined meaning in terms of physical page
            geometry or intended use; it merely imposes clipping on the page contents. However, in the
            absence of additional information, the crop box determines how the page's contents are to be
            positioned on the output medium.</para>
          </remarks>
          <seealso cref="Box"/>
        */
        public SKRect? CropBox
        {
            get
            {
                /*
                  NOTE: The default value is the page's media box.
                */
                PdfDirectObject cropBoxObject = GetInheritableAttribute(PdfName.CropBox);
                return cropBoxObject != null ? Wrap<Rectangle>(cropBoxObject).ToRect() : Box;
            }
            set => BaseDataObject[PdfName.CropBox] = (value.HasValue ? new Rectangle(value.Value).BaseDataObject : null);
        }

        /**
          <summary>Gets/Sets the page's display duration.</summary>
          <remarks>
            <para>The page's display duration (also called its advance timing)
            is the maximum length of time, in seconds, that the page is displayed
            during presentations before the viewer application automatically advances
            to the next page.</para>
            <para>By default, the viewer does not advance automatically.</para>
          </remarks>
        */
        [PDF(VersionEnum.PDF11)]
        public double Duration
        {
            get
            {
                IPdfNumber durationObject = (IPdfNumber)BaseDataObject[PdfName.Dur];
                return durationObject == null ? 0 : durationObject.RawValue;
            }
            set => BaseDataObject[PdfName.Dur] = (value > 0 ? PdfReal.Get(value) : null);
        }

        /**
          <summary>Gets the index of this page.</summary>
        */
        public int Index
        {
            get
            {
                /*
                  NOTE: We'll scan sequentially each page-tree level above this page object
                  collecting page counts. At each level we'll scan the kids array from the
                  lower-indexed item to the ancestor of this page object at that level.
                */
                PdfReference ancestorKidReference = (PdfReference)BaseObject;
                PdfReference parentReference = (PdfReference)BaseDataObject[PdfName.Parent];
                PdfDictionary parent = (PdfDictionary)parentReference.DataObject;
                PdfArray kids = (PdfArray)parent.Resolve(PdfName.Kids);
                int index = 0;
                for (int i = 0; true; i++)
                {
                    PdfReference kidReference = (PdfReference)kids[i];
                    // Is the current-level counting complete?
                    // NOTE: It's complete when it reaches the ancestor at this level.
                    if (kidReference.Equals(ancestorKidReference)) // Ancestor node.
                    {
                        // Does the current level correspond to the page-tree root node?
                        if (!parent.ContainsKey(PdfName.Parent))
                        {
                            // We reached the top: counting's finished.
                            return index;
                        }
                        // Set the ancestor at the next level!
                        ancestorKidReference = parentReference;
                        // Move up one level!
                        parentReference = (PdfReference)parent[PdfName.Parent];
                        parent = (PdfDictionary)parentReference.DataObject;
                        kids = (PdfArray)parent.Resolve(PdfName.Kids);
                        i = -1;
                    }
                    else // Intermediate node.
                    {
                        PdfDictionary kid = (PdfDictionary)kidReference.DataObject;
                        if (kid[PdfName.Type].Equals(PdfName.Page))
                            index++;
                        else
                            index += ((PdfInteger)kid[PdfName.Count]).RawValue;
                    }
                }
            }
        }

        /**
          <summary>Gets the page number.</summary>
        */
        public int Number => Index + 1;

        public TransparencyXObject Group => Wrap<TransparencyXObject>(BaseDataObject[PdfName.Group]);

        /**
          <summary>Gets/Sets the page size.</summary>
        */
        public SKSize Size
        {
            get => Box.Size;
            set
            {
                SKRect box;
                try
                { box = Box; }
                catch
                { box = SKRect.Create(0, 0, 0, 0); }
                box.Size = value;
                Box = box;
            }
        }

        /**
          <summary>Gets/Sets the tab order to be used for annotations on the page.</summary>
        */
        [PDF(VersionEnum.PDF15)]
        public TabOrderEnum TabOrder
        {
            get => ToTabOrderEnum((PdfName)BaseDataObject[PdfName.Tabs]);
            set => BaseDataObject[PdfName.Tabs] = ToCode(value);
        }

        /**
          <summary>Gets the transition effect to be used
          when displaying the page during presentations.</summary>
        */
        [PDF(VersionEnum.PDF11)]
        public Transition Transition
        {
            get => Wrap<Transition>(BaseDataObject[PdfName.Trans]);
            set => BaseDataObject[PdfName.Trans] = PdfObjectWrapper.GetBaseObject(value);
        }

        /**
          <summary>Gets/Sets the intended dimensions of the finished page after trimming
          [PDF:1.7:10.10.1].</summary>
          <remarks>
            <para>It may be smaller than the media box to allow for production-related content, such as
            printing instructions, cut marks, or color bars.</para>
          </remarks>
          <seealso cref="CropBox"/>
        */
        [PDF(VersionEnum.PDF13)]
        public SKRect? TrimBox
        {
            get
            {
                /*
                  NOTE: The default value is the page's crop box.
                */
                PdfDirectObject trimBoxObject = GetInheritableAttribute(PdfName.TrimBox);
                return trimBoxObject != null ? Wrap<Rectangle>(trimBoxObject).ToRect() : CropBox;
            }
            set => BaseDataObject[PdfName.TrimBox] = (value.HasValue ? new Rectangle(value.Value).BaseDataObject : null);
        }

        #region IContentContext
        public SKRect Box
        {
            get => Wrap<Rectangle>(GetInheritableAttribute(PdfName.MediaBox)).ToRect();
            /* NOTE: Mandatory. */
            set => BaseDataObject[PdfName.MediaBox] = new Rectangle(value).BaseDataObject;
        }

        public SKRect RotatedBox => Box.RotateRect(Rotate);

        public ContentWrapper Contents
        {
            get
            {
                PdfDirectObject contentsObject = BaseDataObject[PdfName.Contents];
                if (contentsObject == null)
                { BaseDataObject[PdfName.Contents] = (contentsObject = File.Register(new PdfStream())); }
                return ContentWrapper.Wrap(contentsObject, this);
            }
        }

        public void Render(SKCanvas context, SKSize size)
        {
            ContentScanner scanner = new ContentScanner(Contents);
            scanner.Render(context, size);
        }

        public Resources Resources
        {
            get
            {
                Resources resources = Wrap<Resources>(GetInheritableAttribute(PdfName.Resources));
                return resources != null ? resources : Wrap<Resources>(BaseDataObject.Get<PdfDictionary>(PdfName.Resources));
            }
        }

        public RotationEnum Rotation
        {
            get => RotationEnumExtension.Get((IPdfNumber)GetInheritableAttribute(PdfName.Rotate));
            set => BaseDataObject[PdfName.Rotate] = PdfInteger.Get((int)value);
        }

        public int Rotate
        {
            get => ((IPdfNumber)GetInheritableAttribute(PdfName.Rotate))?.IntValue ?? 0;
            set => BaseDataObject[PdfName.Rotate] = PdfInteger.Get(value);
        }

        public SKMatrix InitialMatrix
        {
            get
            {
                if (initialMatrix == null)
                {
                    var size = Box.Size;
                    initialMatrix = GraphicsState.GetInitialMatrix(this, size);
                }
                return initialMatrix.Value;
            }
        }

        public SKMatrix InvertMatrix
        {
            get
            {
                if (inInitialMatrix == null)
                {
                    InitialMatrix.TryInvert(out var invert);
                    inInitialMatrix = invert;
                }
                return inInitialMatrix.Value;
            }
        }

        public SKMatrix RotateMatrix
        {
            get
            {
                if (rotateMatrix == null)
                {
                    rotateMatrix = GraphicsState.GetRotationMatrix(Box, Rotate);
                }
                return rotateMatrix.Value;
            }
        }

        public SKMatrix InRotateMatrix
        {
            get
            {
                if (inRotateMatrix == null)
                {
                    RotateMatrix.TryInvert(out var invert);
                    inRotateMatrix = invert;
                }
                return inRotateMatrix.Value;
            }
        }

        #region IAppDataHolder
        public AppDataCollection AppData => AppDataCollection.Wrap(BaseDataObject.Get<PdfDictionary>(PdfName.PieceInfo), this);

        public AppData GetAppData(PdfName appName)
        { return AppData.Ensure(appName); }

        public DateTime? ModificationDate => (DateTime)PdfSimpleObject<object>.GetValue(BaseDataObject[PdfName.LastModified]);

        public List<ITextString> Strings { get; } = new List<ITextString>();

        public void Touch(PdfName appName)
        { Touch(appName, DateTime.Now); }

        public void Touch(PdfName appName, DateTime modificationDate)
        {
            GetAppData(appName).ModificationDate = modificationDate;
            BaseDataObject[PdfName.LastModified] = new PdfDate(modificationDate);
        }
        #endregion

        #region IContentEntity
        public ContentObject ToInlineObject(PrimitiveComposer composer)
        { throw new NotImplementedException(); }

        public xObjects::XObject ToXObject(Document context)
        {
            xObjects::FormXObject form;
            {
                form = new xObjects::FormXObject(context, Box);
                form.Resources = (Resources)(
                  context == Document  // [FIX:53] Ambiguous context identity.
                    ? Resources // Same document: reuses the existing resources.
                    : Resources.Clone(context) // Alien document: clones the resources.
                  );

                // Body (contents).
                {
                    IBuffer formBody = form.BaseDataObject.Body;
                    PdfDataObject contentsDataObject = BaseDataObject.Resolve(PdfName.Contents);
                    if (contentsDataObject is PdfStream stream)
                    { formBody.Append(stream.Body); }
                    else if (contentsDataObject is PdfArray array)
                    {
                        foreach (PdfDirectObject contentStreamObject in array)
                        { formBody.Append(((PdfStream)contentStreamObject.Resolve()).Body); }
                    }
                }
            }
            return form;
        }
        #endregion
        #endregion
        #endregion

        #region private
        private PdfDirectObject GetInheritableAttribute(PdfName key)
        { return GetInheritableAttribute(BaseDataObject, key); }

        public void OnSetCtm(SKMatrix ctm)
        {
            
        }
        #endregion
        #endregion
        #endregion
    }
}