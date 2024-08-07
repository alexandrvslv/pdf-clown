﻿using PdfClown.Documents;
using PdfClown.Documents.Interaction.Annotations;
using PdfClown.Documents.Interaction.Annotations.ControlPoints;
using PdfClown.UI.Operations;
using System;

namespace PdfClown.UI
{
    public interface IPdfView
    {
        IPdfDocumentViewModel Document { get; set; }
        IPdfPageViewModel Page { get; set; }
        PdfPage CurrentPage { get; set; }
        CursorType Cursor { get; set; }
        bool ShowMarkup { get; }
        bool ShowCharBound { get; }
        EditOperationList Operations { get; }
        ControlPoint SelectedPoint { get; set; }
        ControlPoint HoverPoint { get; set; }
        bool IsReadOnly { get; set; }
        int PagesCount { get; }
        int PageNumber { get; set; }

        Annotation SelectedAnnotation { get; set; }
        TextSelection TextSelection { get; }

        event EventHandler<AnnotationEventArgs> SelectedAnnotationChanged;
        event EventHandler<EventArgs> DocumentChanged;

        void InvalidateSurface();

        void ScrollTo(Annotation annotation);
        void ScrollTo(PdfPage page);
        void ScrollTo(IPdfPageViewModel currentPageView);
        void Reload();
        void UpdateMaximums();
    }
}
