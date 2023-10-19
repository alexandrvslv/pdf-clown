using Org.BouncyCastle.Security;
using PdfClown.Documents.Contents;
using PdfClown.Documents.Contents.Objects;
using PdfClown.Documents.Interaction.Annotations;
using PdfClown.Objects;
using PdfClown.Tools;
using PdfClown.Util.Math.Geom;
using PdfClown.Util.Reflection;
using SkiaSharp;
using SkiaSharp.Views.Forms;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace PdfClown.Viewer
{
    public partial class PdfView : SKScrollView
    {
        public static readonly BindableProperty FitModeProperty = BindableProperty.Create(nameof(FitMode), typeof(PdfViewFitMode), typeof(PdfView), PdfViewFitMode.PageSize,
            propertyChanged: (bindable, oldValue, newValue) => ((PdfView)bindable).OnFitModeChanged((PdfViewFitMode)oldValue, (PdfViewFitMode)newValue));
        public static readonly BindableProperty PageBackgroundProperty = BindableProperty.Create(nameof(PageBackground), typeof(Color), typeof(PdfView), Color.White,
            propertyChanged: (bindable, oldValue, newValue) => ((PdfView)bindable).OnPageBackgroundChanged((Color)oldValue, (Color)newValue));
        public static readonly BindableProperty ScaleContentProperty = BindableProperty.Create(nameof(ScaleContent), typeof(float), typeof(PdfView), 1F,
            propertyChanged: (bindable, oldValue, newValue) => ((PdfView)bindable).OnScaleContentChanged((float)oldValue, (float)newValue));
        public static readonly BindableProperty ShowMarkupProperty = BindableProperty.Create(nameof(ShowMarkup), typeof(bool), typeof(PdfView), true,
            propertyChanged: (bindable, oldValue, newValue) => ((PdfView)bindable).OnShowMarkupChanged((bool)oldValue, (bool)newValue));
        public static readonly BindableProperty HoverAnnotationProperty = BindableProperty.Create(nameof(HoverAnnotation), typeof(Annotation), typeof(PdfView), null,
            propertyChanged: (bindable, oldValue, newValue) => ((PdfView)bindable).OnHoverAnnotationChanged((Annotation)oldValue, (Annotation)newValue));
        public static readonly BindableProperty SelectedAnnotationProperty = BindableProperty.Create(nameof(SelectedAnnotation), typeof(Annotation), typeof(PdfView), null,
            propertyChanged: (bindable, oldValue, newValue) => ((PdfView)bindable).OnSelectedAnnotationChanged((Annotation)oldValue, (Annotation)newValue));
        public static readonly BindableProperty SelectedMarkupProperty = BindableProperty.Create(nameof(SelectedMarkup), typeof(Markup), typeof(PdfView), null,
            propertyChanged: (bindable, oldValue, newValue) => ((PdfView)bindable).OnSelectedMarkupChanged((Markup)oldValue, (Markup)newValue));
        public static readonly BindableProperty CurrentOperationProperty = BindableProperty.Create(nameof(CurrentOperation), typeof(OperationType), typeof(PdfView), OperationType.None,
            propertyChanged: (bindable, oldValue, newValue) => ((PdfView)bindable).OnCurrentOperationChanged((OperationType)oldValue, (OperationType)newValue));
        public static readonly BindableProperty SelectedPointProperty = BindableProperty.Create(nameof(SelectedPoint), typeof(ControlPoint), typeof(PdfView), null,
            propertyChanged: (bindable, oldValue, newValue) => ((PdfView)bindable).OnSelectedPointChanged((ControlPoint)oldValue, (ControlPoint)newValue));
        public static readonly BindableProperty HoverPointProperty = BindableProperty.Create(nameof(HoverPoint), typeof(ControlPoint), typeof(PdfView), null,
           propertyChanged: (bindable, oldValue, newValue) => ((PdfView)bindable).OnHoverPointChanged((ControlPoint)oldValue, (ControlPoint)newValue));
        public static readonly BindableProperty IsReadOnlyProperty = BindableProperty.Create(nameof(IsReadOnly), typeof(bool), typeof(PdfView), false,
           propertyChanged: (bindable, oldValue, newValue) => ((PdfView)bindable).OnIsReadOnlyChanged((bool)oldValue, (bool)newValue));
        public static readonly BindableProperty ShowCharBoundProperty = BindableProperty.Create(nameof(ShowCharBound), typeof(bool), typeof(PdfView), false,
           propertyChanged: (bindable, oldValue, newValue) => ((PdfView)bindable).OnShowCharBoundChanged((bool)oldValue, (bool)newValue));

        internal readonly SKPaint paintPageBackground = new SKPaint { Style = SKPaintStyle.Fill, Color = SKColors.White };
        internal readonly SKPaint paintRed = new SKPaint { Style = SKPaintStyle.Stroke, Color = SKColors.OrangeRed };
        internal readonly SKPaint paintText = new SKPaint { Style = SKPaintStyle.StrokeAndFill, Color = SKColors.Black, TextSize = 14, IsAntialias = true };
        internal readonly SKPaint paintPointFill = new SKPaint { Color = SKColors.White, Style = SKPaintStyle.Fill };
        internal readonly SKPaint paintTextSelectionFill = new SKPaint { Color = SKColors.LightBlue, Style = SKPaintStyle.Fill, BlendMode = SKBlendMode.Multiply, IsAntialias = true };
        internal readonly SKPaint paintBorderDefault = new SKPaint { Color = SKColors.Silver, Style = SKPaintStyle.Stroke, StrokeWidth = 1, IsAntialias = true };
        internal readonly SKPaint paintBorderSelection = new SKPaint { Color = SKColors.Blue, Style = SKPaintStyle.Stroke, StrokeWidth = 1, IsAntialias = true };
        internal readonly SKPaint paintAnnotationToolTip = new SKPaint() { Color = SKColors.LightGray, Style = SKPaintStyle.Fill, IsAntialias = true };

        private float oldScale = 1;
        private float scale = 1;

        private readonly PdfViewEventArgs state = new PdfViewEventArgs();

        private Annotation selectedAnnotation;
        private ControlPoint selectedPoint;

        private string selectedString;
        private Quad? selectedQuad;
        private TextChar startSelectionChar;

        private LinkedList<EditorOperation> operations = new LinkedList<EditorOperation>();

        private LinkedListNode<EditorOperation> lastOperationLink;
        private bool handlePropertyChanged = true;
        private bool readOnly;
        private bool showCharBound;
        private PdfDocumentView document;
        private PdfPageView currentPage;

        public PdfView()
        {
            state.Viewer = this;
        }

        public PdfViewFitMode FitMode
        {
            get => (PdfViewFitMode)GetValue(FitModeProperty);
            set => SetValue(FitModeProperty, value);
        }

        public Color PageBackground
        {
            get => (Color)GetValue(PageBackgroundProperty);
            set => SetValue(PageBackgroundProperty, value);
        }

        public float ScaleContent
        {
            get => (float)GetValue(ScaleContentProperty);
            set => SetValue(ScaleContentProperty, value);
        }

        public bool ShowMarkup
        {
            get => (bool)GetValue(ShowMarkupProperty);
            set => SetValue(ShowMarkupProperty, value);
        }

        public Annotation SelectedAnnotation
        {
            get => (Annotation)GetValue(SelectedAnnotationProperty);
            set => SetValue(SelectedAnnotationProperty, value);
        }

        public Annotation HoverAnnotation
        {
            get => (Annotation)GetValue(HoverAnnotationProperty);
            set => SetValue(HoverAnnotationProperty, value);
        }

        public Markup SelectedMarkup
        {
            get => (Markup)GetValue(SelectedMarkupProperty);
            set => SetValue(SelectedMarkupProperty, value);
        }

        public ControlPoint SelectedPoint
        {
            get => (ControlPoint)GetValue(SelectedPointProperty);
            set => SetValue(SelectedPointProperty, value);
        }

        public ControlPoint HoverPoint
        {
            get => (ControlPoint)GetValue(HoverPointProperty);
            set => SetValue(HoverPointProperty, value);
        }

        public OperationType CurrentOperation
        {
            get => (OperationType)GetValue(CurrentOperationProperty);
            set => SetValue(CurrentOperationProperty, value);
        }

        public bool IsReadOnly
        {
            get => (bool)GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        public bool ShowCharBound
        {
            get => (bool)GetValue(ShowCharBoundProperty);
            set => SetValue(ShowCharBoundProperty, value);
        }

        public PdfDocumentView Document
        {
            get => document;
            set
            {
                if (document == value)
                    return;
                if (document != null)
                {
                    document.AnnotationAdded -= OnDocumentAnnotationAdded;
                    document.AnnotationRemoved -= OnDocumentAnnotationRemoved;
                    document.EndOperation -= OnDocumentEndOperation;
                }
                SelectedAnnotation = null;
                SelectedPoint = null;
                document = value;
                OnPropertyChanged(nameof(PagesCount));
                UpdateMaximums();
                if (document != null)
                {
                    document.AnnotationAdded += OnDocumentAnnotationAdded;
                    document.AnnotationRemoved += OnDocumentAnnotationRemoved;
                    document.EndOperation += OnDocumentEndOperation;

                    CurrentPage = document.PageViews.FirstOrDefault();
                    ScrollTo(CurrentPage);
                }
            }
        }

        public SKSize DocumentSize => Document?.Size ?? SKSize.Empty;

        public PdfPageView CurrentPage
        {
            get
            {
                if (currentPage == null || currentPage.Document != Document)
                {
                    currentPage = GetCenterPage();
                }
                return currentPage;
            }
            set
            {
                if (currentPage != value)
                {
                    currentPage = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(PageNumber));
                }
            }
        }

        public List<TextChar> TextSelection { get; private set; } = new List<TextChar>();

        public Quad? SelectedQuad
        {
            get
            {
                if (selectedQuad == null)
                {
                    foreach (TextChar textChar in TextSelection)
                    {
                        if (!selectedQuad.HasValue)
                        { selectedQuad = textChar.Quad; }
                        else
                        { selectedQuad = Quad.Union(selectedQuad.Value, textChar.Quad); }
                    }
                }
                return selectedQuad;
            }
        }

        public string SelectedString
        {
            get
            {
                if (selectedString == null)
                {
                    var textBuilder = new StringBuilder();
                    TextChar prevTextChar = null;
                    foreach (TextChar textChar in TextSelection)
                    {
                        if (prevTextChar != null && prevTextChar.TextString != textChar.TextString)
                        {
                            textBuilder.Append(' ');
                        }
                        textBuilder.Append(textChar.Value);
                        prevTextChar = textChar;
                    }
                    selectedString = textBuilder.ToString();
                }
                return selectedString;
            }
        }


        public bool IsChanged => lastOperationLink != null;

        public bool CanRedo => (lastOperationLink == null ? operations.First : lastOperationLink?.Next) != null;

        public bool CanUndo => lastOperationLink != null;

        public int PagesCount
        {
            get => Document?.PageViews.Count ?? 0;
        }

        public int PageNumber
        {
            get => (CurrentPage?.Index ?? -1) + 1;
            set
            {
                if (Document == null
                    || Document.PageViews.Count == 0)
                {
                    return;
                }
                var index = value - 1;
                if (index < 0)
                {
                    index = Document.PageViews.Count - 1;
                }
                else if (index >= Document.PageViews.Count)
                {
                    index = 0;
                }

                CurrentPage = Document.PageViews[index];
                OnPropertyChanged();
            }
        }

        public void NextPage()
        {
            PageNumber += 1;
            ScrollTo(CurrentPage);
        }

        public void PrevPage()
        {
            PageNumber -= 1;
            ScrollTo(CurrentPage);
        }

        public event EventHandler<AnnotationEventArgs> CheckCanRemove;

        public event EventHandler<AnnotationEventArgs> AnnotationAdded;

        public event EventHandler<AnnotationEventArgs> AnnotationRemoved;

        public event EventHandler<EventArgs> TextSelectionChanged;

        public event EventHandler<EventArgs> DragComplete;

        public event EventHandler<EventArgs> SizeComplete;

        public event EventHandler<AnnotationEventArgs> SelectedAnnotationChanged;

        private PdfPageView GetCenterPage()
        {
            if (Document == null)
                return null;
            var area = state.WindowArea;
            area.Inflate(-area.Width / 3F, -area.Height / 3F);
            area = state.InvertNavigationMatrix.MapRect(area);
            for (int i = GetDisplayPageIndex(); i < Document.PageViews.Count; i++)
            {
                var pageView = Document.PageViews[i];
                if (pageView.Bounds.IntersectsWith(area))
                {
                    return pageView;
                }
            }
            return Document.PageViews.FirstOrDefault();
        }

        private int GetDisplayPageIndex()
        {
            var verticalValue = -(state.NavigationMatrix.TransY);
            var page = Document.PageViews.FirstOrDefault(p => (p.Bounds.Bottom * state.NavigationMatrix.ScaleY) > verticalValue);
            return page?.Order ?? 0;
        }

        private void OnFitModeChanged(PdfViewFitMode oldValue, PdfViewFitMode newValue)
        {
            ScrollTo(CurrentPage);
        }

        private void OnPageBackgroundChanged(Color oldValue, Color newValue)
        {
            paintPageBackground.Color = newValue.ToSKColor();
        }

        private void OnScaleContentChanged(float oldValue, float newValue)
        {
            oldScale = oldValue;
            scale = newValue;
            UpdateMaximums();
        }

        private void OnShowMarkupChanged(bool oldValue, bool newValue)
        {
            InvalidateSurface();
        }

        private void OnHoverAnnotationChanged(Annotation oldValue, Annotation newValue)
        {

        }

        private void OnSelectedAnnotationChanged(Annotation oldValue, Annotation newValue)
        {
            selectedAnnotation = newValue;
            SelectedMarkup = newValue as Markup;
            CurrentOperation = OperationType.None;
            SelectedPoint = null;
            if (oldValue != null)
            {
                SuspendAnnotationPropertyHandler(oldValue);
            }
            if (newValue != null)
            {
                ResumeAnnotationPropertyHandler(newValue);
                if (newValue.IsNew)
                {
                    if (newValue is TextMarkup
                        || (newValue is StickyNote note
                        && note.ReplyTo != null))
                    {
                        AddAnnotation(newValue).EndOperation();
                    }
                    else
                    {
                        CurrentOperation = OperationType.AnnotationDrag;
                    }
                }
            }
            SelectedAnnotationChanged?.Invoke(this, new AnnotationEventArgs(newValue));
            InvalidateSurface();
        }

        private AnnotationOperation AddAnnotation(Annotation newValue)
        {
            var operation = BeginOperation(selectedAnnotation, OperationType.AnnotationAdd);
            selectedAnnotation.IsNew = false;
            return operation;
        }

        private void SuspendAnnotationPropertyHandler(Annotation annotation)
        {
            if (annotation != null)
            {
                annotation.PropertyChanged -= OnAnnotationPropertyChanged;
            }
        }

        private void ResumeAnnotationPropertyHandler(Annotation annotation)
        {
            if (annotation != null)
            {
                annotation.PropertyChanged += OnAnnotationPropertyChanged;
            }
        }

        private void OnAnnotationPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!handlePropertyChanged)
                return;
            var annotation = (Annotation)sender;
            var details = (DetailedPropertyChangedEventArgs)e;
            switch (e.PropertyName)
            {
                case nameof(Annotation.SKColor):
                case nameof(Annotation.Contents):
                case nameof(Annotation.Subject):
                case nameof(Annotation.Border):
                case nameof(Markup.Popup):
                case nameof(Markup.ReplyTo):
                case nameof(Markup.ReplyType):
                case nameof(Markup.BorderEffect):
                case nameof(Markup.RichContents):
                case nameof(Markup.DefaultStyle):
                case nameof(Line.StartStyle):
                case nameof(Line.EndStyle):
                    var invoker = Invoker.GetPropertyInvoker(annotation.GetType(), e.PropertyName);
                    BeginOperation(annotation, OperationType.AnnotationProperty, invoker, details.OldValue, details.NewValue);
                    break;
            }
            Device.BeginInvokeOnMainThread(() => InvalidateSurface());
        }

        private void OnSelectedMarkupChanged(Markup oldValue, Markup newValue)
        {
            SelectedAnnotation = newValue;
        }

        private void OnCurrentOperationChanged(OperationType oldValue, OperationType newValue)
        {
            if (selectedAnnotation != null
                && lastOperationLink?.Value is AnnotationOperation annotationOperation
                && annotationOperation.Annotation == selectedAnnotation)
            {
                switch (oldValue)
                {
                    case OperationType.AnnotationAdd:
                        lastOperationLink.Value.EndOperation();
                        break;
                    case OperationType.AnnotationDrag:
                        lastOperationLink.Value.EndOperation();
                        break;
                    case OperationType.AnnotationSize:
                        lastOperationLink.Value.EndOperation();
                        break;
                    case OperationType.PointMove:
                    case OperationType.PointAdd:
                    case OperationType.PointRemove:
                        lastOperationLink.Value.EndOperation();
                        break;
                }
            }
            if (oldValue == OperationType.AnnotationDrag)
            {
                DragComplete?.Invoke(this, new AnnotationEventArgs(selectedAnnotation));
            }
            if (oldValue == OperationType.AnnotationSize)
            {
                SizeComplete?.Invoke(this, new AnnotationEventArgs(selectedAnnotation));
            }
            if (newValue != OperationType.None)
            {
                if (selectedAnnotation == null)
                {
                    throw new InvalidOperationException("SelectedAnnotation is not specified!");
                }
            }
            switch (newValue)
            {
                case OperationType.AnnotationDrag:
                    if (!selectedAnnotation.IsNew)
                    {
                        BeginOperation(selectedAnnotation, newValue, "Box");
                    }
                    else
                    {
                        Cursor = CursorType.Cross;
                    }
                    break;
                case OperationType.AnnotationSize:
                    BeginOperation(selectedAnnotation, newValue, "Box");
                    Cursor = CursorType.SizeNWSE;
                    break;
                case OperationType.PointMove:
                case OperationType.PointAdd:
                case OperationType.PointRemove:
                    if (selectedPoint == null)
                    {
                        throw new InvalidOperationException("SelectedPoint is not specified!");
                    }
                    BeginOperation(selectedAnnotation, newValue, selectedPoint);
                    Cursor = CursorType.Cross;
                    break;
                case OperationType.None:
                    Cursor = CursorType.Arrow;
                    break;
            }
        }

        private void OnSelectedPointChanged(ControlPoint oldValue, ControlPoint newValue)
        {
            selectedPoint = newValue;
            if (newValue != null)
            {
                SelectedAnnotation = newValue.Annotation;
            }
            else
            {
                CurrentOperation = OperationType.None;
            }
        }

        private void OnHoverPointChanged(ControlPoint oldValue, ControlPoint newValue)
        {
            if (newValue != null)
            {
                Cursor = CursorType.Cross;
            }
            else
            {
                Cursor = CursorType.Arrow;
            }
        }

        private void OnShowCharBoundChanged(bool oldValue, bool newValue)
        {
            showCharBound = newValue;
            InvalidateSurface();
        }

        private void OnIsReadOnlyChanged(bool oldValue, bool newValue)
        {
            readOnly = newValue;
        }

        protected override void OnVerticalValueChanged(double oldValue, double newValue)
        {
            UpdateCurrentMatrix();
            base.OnVerticalValueChanged(oldValue, newValue);
            if (ScrollAnimation == null)
            {
                CurrentPage = GetCenterPage();
            }
        }

        protected override void OnHorizontalValueChanged(double oldValue, double newValue)
        {
            UpdateCurrentMatrix();
            base.OnHorizontalValueChanged(oldValue, newValue);
        }

        protected override void OnWindowScaleChanged()
        {
            base.OnWindowScaleChanged();
            state.WindowScaleMatrix = SKMatrix.CreateScale(XScaleFactor, YScaleFactor);
        }

        protected override void OnPaintContent(SKPaintSurfaceEventArgs e)
        {
            if (Document == null)
                return;
            state.Canvas = e.Surface.Canvas;
            try
            {
                state.Canvas.SetMatrix(state.ViewMatrix);
                var area = state.InvertNavigationMatrix.MapRect(state.WindowArea);
                for (int i = GetDisplayPageIndex(); i < Document.PageViews.Count; i++)
                {
                    var pageView = Document.PageViews[i];
                    var pageBounds = pageView.Bounds;
                    if (pageBounds.IntersectsWith(area))
                    {
                        state.PageView = pageView;
                        state.Canvas.Save();
                        var pageMatrix = pageView.Matrix;
                        state.Canvas.Concat(ref pageMatrix);
                        var pageRect = SKRect.Create(pageView.Size);
                        state.Canvas.ClipRect(pageRect);
                        state.Canvas.DrawRect(pageRect, paintPageBackground);

                        var picture = pageView.GetPicture(this);
                        if (picture != null)
                        {
                            //var image = pageView.GetImage(this, scale, scale);
                            //if (image != null)
                            //{
                            //    state.Canvas.DrawImage(image, SKPoint.Empty);
                            //}

                            state.Canvas.DrawPicture(picture);


                            if (ShowMarkup && pageView.GetAnnotations().Any())
                            {
                                OnPaintAnnotations(state);
                            }
                            if (TextSelection.Count > 0)
                            {
                                foreach (var textChar in TextSelection)
                                {
                                    if (textChar.TextString.Context == pageView.Page)
                                    {
                                        using (var path = textChar.Quad.GetPath())
                                        {
                                            state.Canvas.DrawPath(path, paintTextSelectionFill);
                                        }
                                    }
                                }
                            }
                            if (showCharBound)
                            {
                                DrawCharBounds(state);
                            }
                        }
                        state.Canvas.Restore();
                    }
                    else if (pageBounds.Top > area.Bottom)
                    {
                        break;
                    }
                }
            }
            finally
            {
                state.Canvas = null;
            }
        }

        private void DrawCharBounds(PdfViewEventArgs state)
        {
            try
            {
                foreach (var textString in state.PageView.Page.Strings)
                {
                    foreach (var textChar in textString.TextChars)
                    {
                        state.Canvas.DrawPoints(SKPointMode.Polygon, textChar.Quad.GetPoints(), paintRed);
                    }
                }
            }
            catch
            {

            }
        }

        private void OnPaintAnnotations(PdfViewEventArgs state)
        {
            state.DrawAnnotation = null;
            foreach (var annotation in state.PageView.GetAnnotations())
            {
                if (annotation != null && annotation.Visible)
                {
                    if (annotation == selectedAnnotation)
                    {
                        state.DrawAnnotation = annotation;
                        OnPaintSelectedAnnotation(state);
                    }
                    else
                    {
                        annotation.Draw(state.Canvas);
                    }
                }
            }
            if (selectedAnnotation != null && state.DrawAnnotation == null)
            {
                state.DrawAnnotation = selectedAnnotation;
                OnPaintSelectedAnnotation(state);
            }
            OnPaintAnnotationToolTip(state);
        }

        private void OnPaintSelectedAnnotation(PdfViewEventArgs state)
        {
            state.DrawAnnotation.Draw(state.Canvas);
            //if (CurrentOperation == OperationType.None
            //&& state.DrawAnnotation != selectedPoint?.Annotation)
            {
                var bounds = state.DrawAnnotation.Box;
                if (state.DrawAnnotation is StickyNote stick)
                {
                    bounds.Size = new SKSize(StickyNote.size / state.Canvas.TotalMatrix.ScaleX, StickyNote.size / state.Canvas.TotalMatrix.ScaleY);
                }
                bounds.Inflate(2, 2);
                state.Canvas.DrawRoundRect(bounds, 3, 3, paintBorderSelection);
            }
            foreach (var controlPoint in state.DrawAnnotation.GetControlPoints())
            {
                var bounds = controlPoint.Bounds;
                state.Canvas.DrawOval(bounds, paintPointFill);
                state.Canvas.DrawOval(bounds, controlPoint == selectedPoint ? paintBorderSelection : paintBorderDefault);
            }
        }

        private void OnPaintAnnotationToolTip(PdfViewEventArgs state)
        {
            if (!string.IsNullOrEmpty(state.AnnotationText))
            {
                state.Canvas.Save();
                state.Canvas.SetMatrix(state.WindowScaleMatrix);
                state.Canvas.DrawRect(state.AnnotationTextBounds, paintAnnotationToolTip);
                state.Canvas.DrawText(state.AnnotationText,
                    state.AnnotationTextBounds.Left + 5,
                    state.AnnotationTextBounds.MidY + paintText.FontMetrics.Bottom,
                    paintText);
                state.Canvas.Restore();
            }
        }

        public override bool OnKeyDown(string keyName, KeyModifiers modifiers)
        {
            if (string.Equals(keyName, "Delete", StringComparison.OrdinalIgnoreCase))
            {
                if (!readOnly)
                {
                    if (selectedPoint is IndexControlPoint indexControlPoint)
                    {
                        BeginOperation(selectedPoint.Annotation, OperationType.PointRemove, indexControlPoint, indexControlPoint.Point, indexControlPoint.Point);
                        ((VertexShape)selectedPoint.Annotation).RemovePoint(indexControlPoint.Index);
                        return true;
                    }
                    else if (selectedAnnotation != null)
                    {
                        RemoveAnnotation(selectedAnnotation);
                    }
                }
            }
            else if (string.Equals(keyName, "Escape", StringComparison.OrdinalIgnoreCase))
            {
                if (selectedPoint != null
                    && selectedAnnotation is VertexShape vertexShape
                    && CurrentOperation == OperationType.PointAdd)
                {
                    CloseVertextShape(vertexShape);
                }
            }
            else if (string.Equals(keyName, "Z", StringComparison.OrdinalIgnoreCase))
            {
                if (modifiers == KeyModifiers.Ctrl)
                {
                    Undo();
                }
                else if (modifiers == (KeyModifiers.Ctrl | KeyModifiers.Shift))
                {
                    Redo();
                }
            }
            return base.OnKeyDown(keyName, modifiers);
        }

        private void OnTextSelectionChanged()
        {
            selectedQuad = null;
            selectedString = null;
            InvalidateSurface();
            TextSelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        private void CloseVertextShape(VertexShape vertexShape)
        {
            vertexShape.RefreshBox();
            SelectedPoint = null;
            CurrentOperation = OperationType.None;
        }

        protected override void OnTouch(SKTouchEventArgs e)
        {
            base.OnTouch(e);
            state.TouchEvent = e;
            state.PointerLocation = e.Location;
            state.ViewPointerLocation = state.InvertViewMatrix.MapPoint(state.PointerLocation);
            if (e.MouseButton == SKMouseButton.Middle)
            {
                if (e.ActionType == SKTouchAction.Pressed)
                {
                    state.MoveLocation = state.PointerLocation;
                    Cursor = CursorType.ScrollAll;
                    return;
                }
                else if (e.ActionType == SKTouchAction.Moved)
                {
                    var vector = state.PointerLocation - state.MoveLocation;
                    HorizontalValue -= vector.X;
                    VerticalValue -= vector.Y;
                    state.MoveLocation = state.PointerLocation;
                    return;
                }
            }
            if (Document == null || !Document.IsPaintComplete)
            {
                return;
            }
            for (int i = GetDisplayPageIndex(); i < Document.PageViews.Count; i++)
            {
                var pageView = Document.PageViews[i];
                if (pageView.Bounds.Contains(state.ViewPointerLocation))
                {
                    state.PageView = pageView;
                    OnTouchPage(state);
                    return;
                }
            }
            if (CurrentOperation != OperationType.AnnotationDrag)
                Cursor = CursorType.Arrow;
            state.PageView = null;
        }

        private void OnTouchPage(PdfViewEventArgs state)
        {
            state.PageMatrix = state.ViewMatrix.PreConcat(state.PageView.Matrix);
            state.PageMatrix.TryInvert(out state.InvertPageMatrix);
            state.PagePointerLocation = state.InvertPageMatrix.MapPoint(state.PointerLocation);
            //SKMatrix.PreConcat(ref state.PageMatrix, state.PageView.InitialMatrix);

            switch (CurrentOperation)
            {
                case OperationType.PointMove:
                    OnTouchPointMove(state);
                    return;
                case OperationType.PointAdd:
                    OnTouchPointAdd(state);
                    return;
                case OperationType.AnnotationSize:
                    OnTouchSized(state);
                    return;
                case OperationType.AnnotationDrag:
                    OnTouchDragged(state);
                    return;
                default:
                    break;
            }

            if (selectedAnnotation != null && selectedAnnotation.Page == state.PageView.Page)
            {
                var bounds = selectedAnnotation.GetBounds(state.PageMatrix);
                bounds.Inflate(4, 4);
                if (bounds.Contains(state.PointerLocation))
                {
                    state.Annotation = selectedAnnotation;
                    state.AnnotationBounds = bounds;
                    OnTouchAnnotation(state);
                    return;
                }
            }
            if (OnTouchText(state))
            {
                return;
            }
            foreach (var annotation in state.PageView.GetAnnotations())
            {
                if (annotation == null || !annotation.Visible)
                    continue;
                var bounds = annotation.GetBounds(state.PageMatrix);
                bounds.Inflate(4, 4);
                if (bounds.Contains(state.PointerLocation))
                {
                    state.Annotation = annotation;
                    state.AnnotationBounds = bounds;
                    OnTouchAnnotation(state);
                    return;
                }
            }
            if (state.TouchEvent.ActionType == SKTouchAction.Released)
            {
                SelectedAnnotation = null;
            }
            else if (state.TouchEvent.ActionType == SKTouchAction.Pressed)
            {
                TextSelection.Clear();
                OnTextSelectionChanged();
            }
            Cursor = CursorType.Arrow;
            state.Annotation = null;
            state.AnnotationText = null;
            HoverPoint = null;
        }

        private bool OnTouchText(PdfViewEventArgs state)
        {
            var textSelectionChanged = false;
            var page = state.PageView.Page;
            foreach (var textString in page.Strings)
            {
                foreach (var textChar in textString.TextChars)
                {
                    if (textChar.Quad.Contains(state.PagePointerLocation))
                    {
                        Cursor = CursorType.IBeam;
                        if (state.TouchEvent.ActionType == SKTouchAction.Pressed
                            && state.TouchEvent.MouseButton == SKMouseButton.Left)
                        {
                            TextSelection.Clear();
                            startSelectionChar = textChar;
                            TextSelection.Add(textChar);
                            OnTextSelectionChanged();
                        }
                        textSelectionChanged = true;
                        break;
                    }
                }
            }
            if (state.TouchEvent.ActionType == SKTouchAction.Moved
                && state.TouchEvent.MouseButton == SKMouseButton.Left
                && startSelectionChar != null)
            {
                var firstCharIndex = startSelectionChar.TextString.TextChars.IndexOf(startSelectionChar);
                var firstString = startSelectionChar.TextString;
                var firstStringIndex = page.Strings.IndexOf(firstString);
                var firstMiddle = startSelectionChar.Quad.Middle.Value;
                var line = new SKLine(firstMiddle, state.PagePointerLocation);
                TextSelection.Clear();
                if (startSelectionChar.Quad.Contains(line.a) && startSelectionChar.Quad.Contains(line.b))
                {
                    TextSelection.Add(startSelectionChar);
                }
                else
                {
                    for (int i = firstStringIndex < 1 ? 0 : firstStringIndex - 1; i < page.Strings.Count; i++)
                    {
                        var textString = page.Strings[i];
                        foreach (var textChar in textString.TextChars)
                        {
                            if (SKLine.FindIntersection(line, textChar.Quad, true) != null)
                            {
                                TextSelection.Add(textChar);
                            }
                            else if (TextSelection.Count > 1)
                            {
                                break;
                            }
                        }
                    }
                }
                OnTextSelectionChanged();
                textSelectionChanged = true;
            }
            if (state.TouchEvent.ActionType == SKTouchAction.Released)
            {
                startSelectionChar = null;
            }

            return textSelectionChanged;
        }

        private void OnTouchPointAdd(PdfViewEventArgs state)
        {
            var vertexShape = selectedAnnotation as VertexShape;
            if (state.TouchEvent.ActionType == SKTouchAction.Moved)
            {
                selectedPoint.Point = state.PagePointerLocation;
            }
            else if (state.TouchEvent.ActionType == SKTouchAction.Released)
            {
                var rect = new SKRect(vertexShape.FirstPoint.X - 5, vertexShape.FirstPoint.Y - 5, vertexShape.FirstPoint.X + 5, vertexShape.FirstPoint.Y + 5);
                if (vertexShape.Points.Length > 2 && rect.Contains(vertexShape.LastPoint))
                {
                    //EndOperation(operationLink.Value);
                    //BeginOperation(annotation, OperationType.PointRemove, CurrentPoint);
                    //annotation.RemovePoint(annotation.Points.Length - 1);
                    CloseVertextShape(vertexShape);
                }
                else
                {
                    lastOperationLink.Value.EndOperation();
                    SelectedPoint = vertexShape.AddPoint(state.PagePointerLocation);
                    BeginOperation(vertexShape, OperationType.PointAdd, selectedPoint);
                    return;
                }

                CurrentOperation = OperationType.None;
            }

            InvalidateSurface();
        }

        private void OnTouchPointMove(PdfViewEventArgs state)
        {
            if (state.TouchEvent.ActionType == SKTouchAction.Moved)
            {
                if (state.TouchEvent.MouseButton == SKMouseButton.Left)
                {
                    selectedPoint.Point = state.PagePointerLocation;
                }
            }
            else if (state.TouchEvent.ActionType == SKTouchAction.Released)
            {
                //CurrentPoint.Point = state.PagePointerLocation;

                CurrentOperation = OperationType.None;
            }

            InvalidateSurface();
        }

        private void OnTouchSized(PdfViewEventArgs state)
        {
            if (state.PressedLocation == null)
                return;

            if (state.TouchEvent.ActionType == SKTouchAction.Moved)
            {
                var bound = selectedAnnotation.GetBounds(state.PageMatrix);
                bound.Size += new SKSize(state.PointerLocation - state.PressedLocation.Value);

                selectedAnnotation.MoveTo(state.InvertPageMatrix.MapRect(bound));

                state.PressedLocation = state.PointerLocation;
            }
            else if (state.TouchEvent.ActionType == SKTouchAction.Released)
            {
                CurrentOperation = OperationType.None;
            }
            InvalidateSurface();

        }

        private void OnTouchDragged(PdfViewEventArgs state)
        {
            if (state.TouchEvent.ActionType == SKTouchAction.Moved)
            {
                if (state.PageView.Page != selectedAnnotation.Page)
                {
                    if (!selectedAnnotation.IsNew)
                    {
                        lastOperationLink.Value.EndOperation();
                        BeginOperation(selectedAnnotation, OperationType.AnnotationRePage, nameof(Annotation.Page), selectedAnnotation.Page.Index, state.PageView.Index);
                    }
                    selectedAnnotation.Page = state.PageView.Page;
                    if (!selectedAnnotation.IsNew)
                    {
                        BeginOperation(selectedAnnotation, OperationType.AnnotationDrag, nameof(Annotation.Box));
                        state.PressedLocation = null;
                    }
                }
                var bound = selectedAnnotation.GetBounds(state.PageMatrix);
                if (bound.Width == 0)
                    bound.Right = bound.Left + 1;
                if (bound.Height == 0)
                    bound.Bottom = bound.Top + 1;
                if (state.PressedLocation == null || state.TouchEvent.MouseButton == SKMouseButton.Unknown)
                {
                    bound.Location = state.PointerLocation;
                }
                else
                {
                    bound.Location += state.PointerLocation - state.PressedLocation.Value;
                    state.PressedLocation = state.PointerLocation;
                }
                selectedAnnotation.MoveTo(state.InvertPageMatrix.MapRect(bound));

                InvalidateSurface();
            }
            else if (state.TouchEvent.ActionType == SKTouchAction.Pressed)
            {
                state.PressedLocation = state.PointerLocation;
                if (selectedAnnotation.IsNew)
                {
                    AddAnnotation(selectedAnnotation);
                    if (selectedAnnotation is StickyNote sticky)
                    {
                        CurrentOperation = OperationType.None;
                    }
                    else if (selectedAnnotation is Line line)
                    {
                        line.StartPoint = state.InvertPageMatrix.MapPoint(state.PointerLocation);
                        SelectedPoint = line.GetControlPoints().OfType<LineEndControlPoint>().FirstOrDefault();
                        CurrentOperation = OperationType.PointMove;
                    }
                    else if (selectedAnnotation is VertexShape vertexShape)
                    {
                        vertexShape.FirstPoint = state.InvertPageMatrix.MapPoint(state.PointerLocation);
                        SelectedPoint = vertexShape.FirstControlPoint;
                        CurrentOperation = OperationType.PointAdd;
                    }
                    else if (selectedAnnotation is Shape shape)
                    {
                        CurrentOperation = OperationType.AnnotationSize;
                    }
                    else if (selectedAnnotation is FreeText freeText)
                    {
                        if (freeText.Line == null)
                        {
                            CurrentOperation = OperationType.AnnotationSize;
                        }
                        else
                        {
                            freeText.Line.Start = state.InvertPageMatrix.MapPoint(state.PointerLocation);
                            SelectedPoint = selectedAnnotation.GetControlPoints().OfType<TextMidControlPoint>().FirstOrDefault();
                            CurrentOperation = OperationType.PointMove;
                        }
                    }
                    else
                    {
                        var controlPoint = selectedAnnotation.GetControlPoints().OfType<BottomRightControlPoint>().FirstOrDefault();
                        if (controlPoint != null)
                        {
                            SelectedPoint = controlPoint;
                            CurrentOperation = OperationType.PointMove;
                        }
                        else
                        {
                            CurrentOperation = OperationType.AnnotationSize;
                        }
                    }
                }
            }
            else if (state.TouchEvent.ActionType == SKTouchAction.Released)
            {
                state.PressedLocation = null;
                CurrentOperation = OperationType.None;
            }
        }

        private void OnTouchAnnotation(PdfViewEventArgs state)
        {
            if (state.TouchEvent.ActionType == SKTouchAction.Moved)
            {
                state.AnnotationText = state.Annotation.Contents;
                if (state.TouchEvent.MouseButton == SKMouseButton.Left)
                {
                    if (!readOnly
                        && selectedAnnotation != null
                        && state.PressedLocation != null
                        && Cursor == CursorType.Hand)
                    {
                        if (CurrentOperation == OperationType.None)
                        {
                            var dif = SKPoint.Distance(state.PointerLocation, state.PressedLocation.Value);
                            if (Math.Abs(dif) > 5)
                            {
                                CurrentOperation = OperationType.AnnotationDrag;
                            }
                        }
                    }
                }
                else if (state.TouchEvent.MouseButton == SKMouseButton.Unknown)
                {
                    if (state.Annotation == selectedAnnotation)
                    {
                        foreach (var controlPoint in state.Annotation.GetControlPoints())
                        {
                            if (state.PageMatrix.MapRect(controlPoint.Bounds).Contains(state.PointerLocation))
                            {
                                HoverPoint = controlPoint;
                                return;
                            }
                        }
                    }
                    HoverPoint = null;
                    if (!readOnly)
                    {
                        var rect = new SKRect(
                            state.AnnotationBounds.Right - 10,
                            state.AnnotationBounds.Bottom - 10,
                            state.AnnotationBounds.Right,
                            state.AnnotationBounds.Bottom);
                        if (rect.Contains(state.PointerLocation))
                        {
                            Cursor = CursorType.SizeNWSE;
                        }
                        else
                        {
                            Cursor = CursorType.Hand;
                        }
                    }
                }
            }
            else if (state.TouchEvent.ActionType == SKTouchAction.Pressed)
            {
                if (state.TouchEvent.MouseButton == SKMouseButton.Left)
                {
                    state.PressedLocation = state.PointerLocation;
                    SelectedAnnotation = state.Annotation;
                    if (state.Annotation == selectedAnnotation && HoverPoint != null && !readOnly)
                    {
                        SelectedPoint = HoverPoint;
                        CurrentOperation = OperationType.PointMove;
                    }
                    else if (Cursor == CursorType.SizeNWSE && !readOnly)
                    {
                        CurrentOperation = OperationType.AnnotationSize;
                    }
                }
            }
            else if (state.TouchEvent.ActionType == SKTouchAction.Released)
            {
                if (state.TouchEvent.MouseButton == SKMouseButton.Left)
                {
                    state.PressedLocation = null;
                }
            }
        }

        public override void OnScrolled(int delta, KeyModifiers keyModifiers)
        {
            if (keyModifiers == KeyModifiers.None)
            {
                base.OnScrolled(delta, keyModifiers);
            }
            if (keyModifiers == KeyModifiers.Ctrl)
            {
                var scaleStep = 0.06F * Math.Sign(delta);

                var newSclae = scale + scaleStep + scaleStep * scale;
                if (newSclae < 0.01F)
                    newSclae = 0.01F;
                if (newSclae > 60F)
                    newSclae = 60F;
                if (newSclae != scale)
                {
                    var unscaleLocations = new SKPoint(state.PointerLocation.X / XScaleFactor, state.PointerLocation.Y / YScaleFactor);
                    var oldSpacePoint = state.InvertNavigationMatrix.MapPoint(unscaleLocations);

                    ScaleContent = newSclae;

                    var newCurrentLocation = state.NavigationMatrix.MapPoint(oldSpacePoint);

                    var vector = newCurrentLocation - unscaleLocations;
                    if (HorizontalScrollBarVisible)
                    {
                        HorizontalValue += vector.X;
                    }
                    if (VerticalScrollBarVisible)
                    {
                        VerticalValue += vector.Y;
                    }

                }
            }
        }

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);
            UpdateCurrentMatrix();
            ScrollTo(CurrentPage);
        }

        public void Save()
        {
            if (Document == null)
                return;

            Save(Document.FilePath);
        }

        public void SaveWithoutClearOperations(string filePath)
        {
            Document?.Save(filePath);
        }

        public void Save(string filePath)
        {
            ClearOperations();
            Document?.Save(filePath);
        }

        public void SaveTo(Stream stream)
        {
            Document?.SaveTo(stream);
        }

        public void Reload()
        {
            var newDocument = PdfDocumentView.LoadFrom(Document.FilePath);
            if (operations.Count > 0)
            {
                var newOperations = new LinkedList<EditorOperation>();
                foreach (var oldOperation in operations)
                {
                    var newOperation = oldOperation.Clone(newDocument);

                    newOperation.Redo();

                    newOperations.AddLast(newOperation);
                }
                operations = newOperations;
                lastOperationLink = operations.Last;
            }
            var oldDocument = Document;
            oldDocument.Dispose();

            Document = newDocument;
            InvalidateSurface();
            //TODO File.Reload();
        }

        public void Load(string filePath)
        {
            if (Document != null)
                Document.Dispose();

            Document = PdfDocumentView.LoadFrom(filePath);
            ClearOperations();
        }

        public void Load(System.IO.Stream stream)
        {
            if (Document != null)
                Document.Dispose();

            Document = PdfDocumentView.LoadFrom(stream);
            ClearOperations();
        }

        public void ScrollTo(PdfPageView page)
        {
            if (page == null || Document == null)
            {
                return;
            }
            ScrollAnimation = new Animation();
            if (FitMode == PdfViewFitMode.DocumentWidth)
            {
                ScaleContent = (float)Width / Document.Size.Width;
            }
            else if (FitMode == PdfViewFitMode.PageWidth)
            {
                ScaleContent = (float)Width / (page.Bounds.Width + 10);
            }
            else if (FitMode == PdfViewFitMode.PageSize)
            {
                var vScale = (float)Height / (page.Bounds.Height + 10);
                var hScale = (float)Width / (page.Bounds.Width + 10);
                ScaleContent = hScale < vScale ? hScale : vScale;
            }

            var matrix = SKMatrix.CreateIdentity()
                .PreConcat(SKMatrix.CreateScale(scale, scale));
            var bound = matrix.MapRect(page.Bounds);
            var top = bound.Top - 10;
            var left = bound.Left - 10;
            AnimateScroll(Math.Max(top, 0), Math.Max(left, 0));
        }

        public void ScrollTo(Annotation annotation)
        {
            if (annotation?.Page == null)
            {
                return;
            }

            var pageView = Document.GetPageView(annotation.Page);
            if (pageView == null)
            {
                return;
            }
            var matrix = SKMatrix.CreateIdentity()
                .PreConcat(SKMatrix.CreateScale(scale, scale))
                .PreConcat(pageView.Matrix);
            var bound = annotation.GetBounds(matrix);
            var top = bound.Top - (state.WindowArea.MidY - bound.Height / 2);
            var left = bound.Left - (state.WindowArea.MidX - bound.Width / 2);
            AnimateScroll(Math.Max(top, 0), Math.Max(left, 0));
        }

        private void UpdateMaximums()
        {
            UpdateCurrentMatrix();
            HorizontalMaximum = DocumentSize.Width * scale;
            VerticalMaximum = DocumentSize.Height * scale;
            InvalidateSurface();
        }

        private void UpdateCurrentMatrix()
        {
            state.WindowArea = SKRect.Create(0, 0, (float)Width, (float)Height);
            state.Area = SKRect.Create(0, 0, state.WindowArea.Width / XScaleFactor, state.WindowArea.Height / YScaleFactor);

            var maximumWidth = DocumentSize.Width * scale;
            var maximumHeight = DocumentSize.Height * scale;
            var dx = 0F; var dy = 0F;
            if (maximumWidth < state.WindowArea.Width)
            {
                dx = (float)((Width - 10) - maximumWidth) / 2;
            }

            if (maximumHeight < state.WindowArea.Height)
            {
                dy = (float)(Height - maximumHeight) / 2;
            }
            state.NavigationMatrix = new SKMatrix(
                scale, 0, ((float)-HorizontalValue) + dx,
                0, scale, ((float)-VerticalValue) + dy,
                0, 0, 1);
            state.NavigationMatrix.TryInvert(out state.InvertNavigationMatrix);

            state.ViewMatrix = state.NavigationMatrix.PostConcat(state.WindowScaleMatrix);
            state.ViewMatrix.TryInvert(out state.InvertViewMatrix);
        }

        private AnnotationOperation BeginOperation(Annotation annotation, OperationType type, object property = null, object begin = null, object end = null)
        {
            var operation = new AnnotationOperation
            {
                Document = document,
                Annotation = annotation,
                Type = type,
                Property = property,
                OldValue = begin,
                NewValue = end
            };
            if (type == OperationType.AnnotationDrag
                || type == OperationType.AnnotationSize)
            {
                operation.OldValue = annotation.Box;
            }
            if (property is ControlPoint controlPoint)
            {
                operation.OldValue = controlPoint.Point;
            }
            EqueuOperation(operation);
            if (end != null)
            {
                CheckOperations();
            }
            return operation;
        }

        private void EqueuOperation(AnnotationOperation operation)
        {
            if (lastOperationLink == null)
            {
                operations.Clear();
                lastOperationLink = operations.AddFirst(operation);
            }
            else
            {
                var next = lastOperationLink;
                while (next?.Next != null)
                {
                    next = next.Next;
                }
                while (next != lastOperationLink)
                {
                    next = next.Previous;
                    operations.Remove(next.Next);
                }
                lastOperationLink = operations.AddAfter(lastOperationLink, operation);
            }
        }

        public bool Redo()
        {
            var operationLink = lastOperationLink == null ? operations.First : lastOperationLink?.Next;
            if (operationLink != null)
            {
                lastOperationLink = operationLink;
                var operation = lastOperationLink.Value;
                try
                {
                    handlePropertyChanged = false;
                    var annotationOperation = operation as AnnotationOperation;
                    if (annotationOperation != null
                        && (annotationOperation.Annotation.Page?.Annotations.Contains(annotationOperation.Annotation) ?? false))
                    {
                        SelectedAnnotation = annotationOperation.Annotation;
                    }
                    operation.Redo();
                    if (annotationOperation != null
                        && (annotationOperation.Annotation.Page?.Annotations.Contains(annotationOperation.Annotation) ?? false))
                    {
                        SelectedAnnotation = annotationOperation.Annotation;
                    }
                }
                finally
                {
                    handlePropertyChanged = true;
                }
                CheckOperations();
                InvalidateSurface();
                return true;
            }
            return false;
        }

        public bool Undo()
        {
            if (lastOperationLink != null)
            {
                var operation = lastOperationLink.Value;
                lastOperationLink = lastOperationLink.Previous;
                try
                {
                    handlePropertyChanged = false;
                    var annotationOperation = operation as AnnotationOperation;
                    if (annotationOperation != null
                        && (annotationOperation.Annotation.Page?.Annotations.Contains(annotationOperation.Annotation) ?? false))
                    {
                        SelectedAnnotation = annotationOperation.Annotation;
                    }
                    operation.Undo();
                    if (annotationOperation != null
                        && (annotationOperation.Annotation.Page?.Annotations.Contains(annotationOperation.Annotation) ?? false))
                    {
                        SelectedAnnotation = annotationOperation.Annotation;
                    }
                }
                finally
                {
                    handlePropertyChanged = true;
                }
                CheckOperations();
                InvalidateSurface();
                return true;
            }
            return false;
        }

        private void CancalOperation(EditorOperation operation)
        {
            lastOperationLink = operations.Find(operation);
            Undo();
        }

        public void RejectOperations()
        {
            while (Undo())
            { }
        }

        public IEnumerable<EditorOperation> GetOperations()
        {
            return operations.Select(x => x);
        }

        public void ClearOperations()
        {
            operations.Clear();
            lastOperationLink = null;
            CheckOperations();
        }

        private void CheckOperations()
        {
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
            OnPropertyChanged(nameof(IsChanged));
        }

        private void OnDocumentAnnotationAdded(object sender, AnnotationEventArgs e)
        {
            AnnotationAdded?.Invoke(this, e);
            InvalidateSurface();
        }

        private void OnDocumentAnnotationRemoved(object sender, AnnotationEventArgs e)
        {
            if (e.Annotation == selectedAnnotation)
                SelectedAnnotation = null;
            AnnotationRemoved?.Invoke(this, e);
            InvalidateSurface();
        }

        private void OnDocumentEndOperation(object sender, EventArgs e)
        {
            CheckOperations();
        }

        public IEnumerable<Annotation> RemoveAnnotation(Annotation annotation)
        {
            if (!OnCheckCanRemove(annotation))
            {
                return null;
            }
            var operation = BeginOperation(annotation, OperationType.AnnotationRemove);
            if (annotation == selectedAnnotation)
            {
                SelectedAnnotation = null;
            }

            var list = operation.EndOperation() as List<Annotation>;

            if (list.Contains(selectedAnnotation))
                SelectedAnnotation = null;

            AnnotationRemoved?.Invoke(this, new AnnotationEventArgs(annotation));
            InvalidateSurface();
            return list;
        }

        private bool OnCheckCanRemove(Annotation annotation)
        {
            if (CheckCanRemove != null)
            {
                var args = new AnnotationEventArgs(annotation);
                CheckCanRemove(this, args);
                return !args.Cancel;
            }
            return true;
        }

    }
}
