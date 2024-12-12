﻿using PdfClown.Tools;
using PdfClown.UI;
using SkiaSharp;
using SkiaSharp.Views.Forms;
using System;
using System.Threading;
using Xamarin.Forms;

namespace PdfClown.UI
{
    public class SKGLScrollView : SKGLView, ISKScrollView
    {
        public static readonly BindableProperty CursorProperty = BindableProperty.Create(nameof(Cursor), typeof(CursorType), typeof(SKGLScrollView), CursorType.Arrow,
            propertyChanged: (bindable, oldValue, newValue) => ((SKGLScrollView)bindable).OnCursorChanged((CursorType)oldValue, (CursorType)newValue));

        public const int step = 16;
        private const string ahScroll = "VerticalScrollAnimation";
        private float xScaleFactor = 1F;
        private float yScaleFactor = 1F;
        private ScrollLogic scroll;

        public SKGLScrollView()
        {
            IsTabStop = true;
            EnableTouchEvents = true;
            Touch += OnTouch;
            scroll = new ScrollLogic(this);
        }

        public CursorType Cursor
        {
            get => (CursorType)GetValue(CursorProperty);
            set => SetValue(CursorProperty, value);
        }

        public event EventHandler<CanvasKeyEventArgs> KeyDown;

        public double VMaximum
        {
            get => scroll.VMaximum;
            set => scroll.VMaximum = value;
        }

        public double HMaximum
        {
            get => scroll.HMaximum;
            set => scroll.HMaximum = value;
        }

        public double VValue
        {
            get => scroll.VValue;
            set => scroll.VValue = value;
        }

        public double HValue
        {
            get => scroll.HValue;
            set => scroll.HValue = value;
        }

        public bool VBarVisible => scroll.VBarVisible;

        public bool HBarVisible => scroll.HBarVisible;

        public KeyModifiers KeyModifiers { get; set; }

        public Animation ScrollAnimation { get; private set; }

        public Animation VerticalScrollAnimation { get; private set; }

        public Animation HorizontalScrollAnimation { get; private set; }

        public event EventHandler<SKPaintGLSurfaceEventArgs> PaintContent;

        public event EventHandler<SKPaintGLSurfaceEventArgs> PaintOver;

        public float XScaleFactor
        {
            get => xScaleFactor;
            set
            {
                if (xScaleFactor != value)
                {
                    xScaleFactor = value;
                    OnWindowScaleChanged();
                }
            }
        }

        public float YScaleFactor
        {
            get => yScaleFactor;
            set
            {
                if (yScaleFactor != value)
                {
                    yScaleFactor = value;
                    OnWindowScaleChanged();
                }
            }
        }

        public bool IsVScrollAnimation => (VerticalScrollAnimation ?? ScrollAnimation) != null;

        public bool IsHScrollAnimation => (HorizontalScrollAnimation ?? ScrollAnimation) != null;

        protected override void OnTouch(SKTouchEventArgs e)
        {
            base.OnTouch(new SKTouchEventArgs(e.Id, e.ActionType, e.MouseButton, e.DeviceType,
                new SkiaSharp.SKPoint(e.Location.X / XScaleFactor, e.Location.Y / YScaleFactor),
                e.InContact));
            if (e.ActionType == SKTouchAction.WheelChanged)
            {
                OnScrolled(e.WheelDelta);
            }
        }

        protected virtual void OnWindowScaleChanged()
        {
        }

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);
            scroll.OnSizeAllocated(width, height);
        }

        protected void OnTouch(object sender, SKTouchEventArgs args)
        {
            var e = new TouchEventArgs((TouchAction)args.ActionType, (MouseButton)args.MouseButton);
            OnTouch(e);
            args.Handled = e.Handled;
            //if (ScrollBarVisible)
            //{
            //    var scrollBound = GetScrollBounds();
            //    if (scrollBound.Contains(e.Location))
            //    {
            //        e.Handled = true;
            //    }
            //}            
        }

        protected virtual void OnTouch(TouchEventArgs e)
        {
            scroll.OnTouch(e);
        }

        protected override void OnPaintSurface(SKPaintGLSurfaceEventArgs e)
        {
            var canvas = e.Surface.Canvas;
            XScaleFactor = (float)(CanvasSize.Width / Width);
            YScaleFactor = (float)(CanvasSize.Height / Height);

            canvas.Scale(XScaleFactor, YScaleFactor);
            canvas.Clear();

            base.OnPaintSurface(e);

            if (!IsVisible)
                return;

            canvas.Save();
            PaintContent?.Invoke(this, e);
            canvas.Restore();
            scroll.PaintScrollBars(canvas);
            PaintOver?.Invoke(this, e);
        }

        public void SetVerticalScrolledPosition(double top)
        {
            VValue = top;
        }

        public void AnimateScroll(double newValue)
        {
            if (ScrollAnimation != null)
            {
                this.AbortAnimation(ahScroll);
            }
            ScrollAnimation = new Animation(v => VValue = v, VValue, newValue, Easing.SinOut);
            ScrollAnimation.Commit(this, ahScroll, 16, 270, finished: (d, f) => ScrollAnimation = null);
        }

        private void OnCursorChanged(CursorType oldValue, CursorType newValue)
        {
        }

        public virtual bool OnKeyDown(string keyName, KeyModifiers modifiers)
        {
            var args = new CanvasKeyEventArgs(keyName, modifiers);
            KeyDown?.Invoke(this, args);
            return args.Handled;
        }

        protected virtual void OnScrolled(int delta)
        {
            VValue = VValue - step * 2 * Math.Sign(delta);
        }

        public Action CapturePointerFunc;

        public virtual void InvalidatePaint()
        {
            if (Envir.MainContext == SynchronizationContext.Current)
                InvalidateSurface();
            else
                Envir.MainContext.Post(state => InvalidateSurface(), null);
        }

        public void CapturePointer(long pointerId)
        {
            CapturePointerFunc?.Invoke();
        }
    }
}
