using PdfClown.Documents;
using PdfClown.Documents.Interaction.Annotations;
using PdfClown.Documents.Interaction.Forms;
using PdfClown.Files;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace PdfClown.Viewer
{
    public class PdfDocumentView : IDisposable
    {
        internal readonly ManualResetEventSlim LockObject = new ManualResetEventSlim(true);
        public bool IsPaintComplete => LockObject.IsSet;

        public static PdfDocumentView LoadFrom(string filePath)
        {
            var document = new PdfDocumentView();
            document.Load(filePath);
            return document;
        }

        public static PdfDocumentView LoadFrom(System.IO.Stream fileStream)
        {
            var document = new PdfDocumentView();
            document.Load(fileStream);
            return document;
        }


        private readonly List<PdfPageView> pageViews = new List<PdfPageView>();
        private readonly Dictionary<int, PdfPageView> pagesIndex = new Dictionary<int, PdfPageView>();
        private readonly float indent = 10;
        private Fields fields;

        public Files.File File { get; private set; }

        public Document Document => File.Document;

        public Pages Pages { get; private set; }

        public List<PdfPageView> PageViews => pageViews;

        public string FilePath { get; private set; }

        public string TempFilePath { get; set; }

        public SKSize Size { get; private set; }

        public float AvgHeigth { get; private set; }

        public PdfPageView this[int index]
        {
            get => pagesIndex.TryGetValue(index, out var page) ? page : null;
        }

        public event EventHandler EndOperation;

        public event EventHandler<AnnotationEventArgs> AnnotationAdded;

        public event EventHandler<AnnotationEventArgs> AnnotationRemoved;

        public void LoadPages()
        {
            float totalWidth, totalHeight;
            var doubleIndent = indent * 2;
            Pages = Document.Pages;

            totalWidth = 0F;
            totalHeight = 0F;
            pageViews.Clear();
            pagesIndex.Clear();
            int order = 0;
            foreach (var page in Pages)
            {
                totalHeight += indent;
                var box = page.RotatedBox;
                var dpi = 1F;
                var imageSize = new SKSize(box.Width * dpi, box.Height * dpi);
                var pageView = new PdfPageView()
                {
                    Document = this,
                    Order = order++,
                    Index = page.Index,
                    Page = page,
                    Size = imageSize
                };
                pageView.Matrix = pageView.Matrix.PostConcat(SKMatrix.CreateTranslation(indent, totalHeight));
                pagesIndex[pageView.Index] = pageView;
                pageViews.Add(pageView);
                if (imageSize.Width > totalWidth)
                    totalWidth = imageSize.Width;

                totalHeight += imageSize.Height;
            }
            Size = new SKSize(totalWidth + doubleIndent, totalHeight);

            AvgHeigth = totalHeight / pageViews.Count;

            foreach (var pageView in pageViews)
            {
                var indWidth = pageView.Size.Width + doubleIndent;
                if (indWidth < Size.Width)
                {
                    pageView.Matrix = pageView.Matrix.PostConcat(SKMatrix.CreateTranslation((Size.Width - indWidth) / 2, 0));
                }
            }
        }

        public PdfPageView GetPageView(Documents.Page page)
        {
            foreach (var pageView in pageViews)
            {
                if (pageView.Page == page)
                {
                    return pageView;
                }
            }
            return null;
        }

        private void ClearPages()
        {
            foreach (var pageView in pageViews)
            {
                pageView.Dispose();
            }
            pageViews.Clear();
            pagesIndex.Clear();
        }

        public void Dispose()
        {
            if (File != null)
            {

                ClearPages();
                File.Dispose();
                File = null;
                LockObject.Dispose();

                try { System.IO.File.Delete(TempFilePath); }
                catch { }
            }
        }

        public void Load(string filePath)
        {
            FilePath = filePath;
            TempFilePath = GetTempPath(filePath);
            System.IO.File.Copy(filePath, TempFilePath, true);
            var fileStream = new FileStream(TempFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            Load(fileStream);
        }

        private static string GetTempPath(string filePath)
        {
            int? index = null;
            var tempPath = "";
            do
            {
                tempPath = $"{filePath}~{index}";
                index = (index ?? 0) + 1;
            }
            while (System.IO.File.Exists(tempPath));
            return tempPath;
        }

        public void Load(Stream stream)
        {
            if (string.IsNullOrEmpty(FilePath)
                && stream is FileStream fileStream)
            {
                FilePath = fileStream.Name;
                TempFilePath = GetTempPath(FilePath);
                System.IO.File.Copy(FilePath, TempFilePath, true);
                fileStream.Close();
                stream = new FileStream(TempFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            }
            File = new Files.File(stream);
            fields = null;
            LoadPages();
        }

        public void Save(SerializationModeEnum mode = SerializationModeEnum.Standard)
        {
            var path = FilePath;
            var tempPath = Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path) + ".tmp~");
            Save(tempPath, mode);
            System.IO.File.Copy(tempPath, path, true);
        }

        public void Save(string path, SerializationModeEnum mode = SerializationModeEnum.Standard)
        {
            File.Save(path, mode);
        }

        public void SaveTo(Stream stream, SerializationModeEnum mode = SerializationModeEnum.Standard)
        {
            File.Save(stream, mode);
        }

        public void OnEndOperation(object result)
        {
            EndOperation?.Invoke(this, new OperationEventArgs(result));
        }

        public Field GetField(string name)
        {
            return Fields[name];
        }

        public Fields Fields
        {
            get
            {
                if (fields == null || fields != Document.Form.Fields)
                {
                    LockObject.Wait();
                    try
                    {
                        LockObject.Reset();
                        Document.Form.RefreshCache();
                        fields = Document.Form.Fields;
                    }
                    finally
                    {
                        LockObject.Set();
                    }
                }
                return fields;
            }
        }

        public Field AddField(Field field)
        {
            if (string.IsNullOrEmpty(field.Name))
            {
                field.Name = Fields.GenerateName(field.GetType());
            }
            if (!Fields.ContainsKey(field.Name))
            {
                Fields.Add(field);
            }
            return field;
        }

        public IEnumerable<Annotation> GetAllAnnotations()
        {
            foreach (var pageView in PageViews)
            {
                foreach (var annotation in pageView.GetAnnotations())
                {
                    yield return annotation;
                }
            }
        }

        public Annotation FindAnnotation(string name, int? pageIndex = null)
        {
            var annotation = (Annotation)null;
            if (pageIndex == null)
            {
                foreach (var pageView in PageViews)
                {
                    annotation = pageView.GetAnnotation(name);
                    if (annotation != null)
                        return annotation;
                }
            }
            else
            {
                var pageView = this[(int)pageIndex];
                return pageView?.GetAnnotation(name);
            }
            return null;
        }

        public List<Annotation> AddAnnotation(Annotation annotation)
        {
            return AddAnnotation(annotation.Page, annotation);
        }

        public List<Annotation> AddAnnotation(Page page, Annotation annotation)
        {
            var list = new List<Annotation>();
            if (page != null)
            {
                annotation.Page = page;
                if (annotation is Widget widget && widget.Field != null)
                {
                    AddField(widget.Field);
                    list.Add(annotation);
                }
                else if (!page.Annotations.Contains(annotation))
                {
                    page.Annotations.Add(annotation);
                    list.Add(annotation);
                }
                AnnotationAdded?.Invoke(this, new AnnotationEventArgs(annotation));
                foreach (var item in annotation.Replies)
                {
                    if (item is Markup markup)
                    {
                        list.AddRange(AddAnnotation(page, item));
                    }
                }
            }
            if (annotation is Popup popup
                && popup.Parent != null)
            {
                list.AddRange(AddAnnotation(popup.Parent));
            }
            return list;
        }

        public List<Annotation> RemoveAnnotation(Annotation annotation)
        {
            var list = new List<Annotation>();

            if (annotation.Page != null)
            {
                foreach (var item in annotation.Page.Annotations.ToList())
                {
                    if (item is Markup markup
                        && markup.ReplyTo == annotation)//&& markup.ReplyType == Markup.ReplyTypeEnum.Thread
                    {
                        annotation.Replies.Add(item);
                        list.AddRange(RemoveAnnotation(markup));
                    }
                }
            }
            if (annotation is Widget widget && widget.Field != null)
            {
                Fields.Remove(widget.Field);
            }

            annotation.Remove();

            AnnotationRemoved?.Invoke(this, new AnnotationEventArgs(annotation));
            if (annotation is Popup popup)
            {
                list.AddRange(RemoveAnnotation(popup.Parent));
            }
            list.Add(annotation);

            return list;
        }


    }
}
