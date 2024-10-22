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

using PdfClown.Objects;
using PdfClown.Util.Math;
using SkiaSharp;

namespace PdfClown.Documents.Interaction.Navigation
{
    /// <summary>Article bead [PDF:1.7:8.3.2].</summary>
    [PDF(VersionEnum.PDF11)]
    public sealed class ArticleElement : PdfObjectWrapper<PdfDictionary>
    {
        private SKRect? box;

        public ArticleElement(PdfPage page, SKRect box) : base(
            page.Document,
            new PdfDictionary(1) { { PdfName.Type, PdfName.Bead } })
        {
            page.ArticleElements.Add(this);
            Box = box;
        }

        public ArticleElement(PdfDirectObject baseObject) : base(baseObject)
        { }

        /// <summary>Gets the thread article this bead belongs to.</summary>
        public Article Article
        {
            get
            {
                PdfDictionary bead = BaseDataObject;
                Article article;
                while ((article = Wrap<Article>(bead[PdfName.T])) == null)
                { bead = bead.Get<PdfDictionary>(PdfName.V); }
                return article;
            }
        }

        /// <summary>Gets/Sets the location on the page in default user space units.</summary>
        public SKRect Box
        {
            get => box ??= GetUserSpaceBox();
            set
            {
                box = null;
                var newValue = Page.InvertRotateMatrix.MapRect(value);
                BaseDataObject[PdfName.R] = newValue.ToPdfArray();
            }
        }

        private SKRect GetUserSpaceBox()
        {
            var box = BaseDataObject.Get<PdfArray>(PdfName.R)?.ToSKRect() ?? SKRect.Empty;
            return Page.RotateMatrix.MapRect(box);
        }

        /// <summary>Deletes this bead removing also its references on the page and its article thread.
        /// </summary>
        public override bool Delete()
        {
            // Shallow removal (references):
            // * thread links
            Article.Elements.Remove(this);
            // * reference on page
            Page.ArticleElements.Remove(this);

            // Deep removal (indirect object).
            return base.Delete();
        }

        /// <summary>Gets whether this is the first bead in its thread.</summary>
        public bool IsHead()
        {
            PdfDictionary thread = BaseDataObject.Get<PdfDictionary>(PdfName.T);
            return thread != null && BaseObject.Equals(thread[PdfName.F]);
        }

        /// <summary>Gets the next bead.</summary>
        public ArticleElement Next => Wrap<ArticleElement>(BaseDataObject[PdfName.N]);

        /// <summary>Gets the location page.</summary>
        public PdfPage Page => Wrap<PdfPage>(BaseDataObject[PdfName.P]);

        /// <summary>Gets the previous bead.</summary>
        public ArticleElement Previous => Wrap<ArticleElement>(BaseDataObject[PdfName.V]);
    }
}