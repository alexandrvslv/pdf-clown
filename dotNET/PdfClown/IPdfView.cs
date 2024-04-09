using PdfClown.Documents;
using PdfClown.Documents.Interaction.Annotations;
using System.Collections.Generic;

namespace PdfClown
{
    public interface IPdfView
    {
        PdfDocument Document { get; }
        bool IsReadOnly { get; set; }

        bool Redo();
        bool Undo();

        IEnumerable<Annotation> RemoveAnnotation(Annotation annotation);
        void InvalidateSurface();

        void ClearOperations();
        void RejectOperations();
    }
}