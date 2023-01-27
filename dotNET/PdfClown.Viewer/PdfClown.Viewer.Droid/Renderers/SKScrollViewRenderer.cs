using PdfClown.Viewer;
using PdfClown.Viewer.Droid;
using SkiaSharp;
using SkiaSharp.Views.Forms;
using SkiaSharp.Views.Android;
using System;
using System.ComponentModel;
using Xamarin.Forms.Platform.Android;
using Xamarin.Forms;
using Android.Content;
using Android.Views;

[assembly: ExportRenderer(typeof(SKScrollView), typeof(SKScrollViewRenderer))]
namespace PdfClown.Viewer.Droid
{
    public class SKScrollViewRenderer : SKCanvasViewRenderer
    {
        //private bool pressed;
        private ScaleGestureDetector _scaleDetector;
        public SKScrollViewRenderer(Context context) : base(context)
        {
            _scaleDetector = new ScaleGestureDetector(context, new MyScaleListener(this));
        }

        protected override void OnElementChanged(ElementChangedEventArgs<SkiaSharp.Views.Forms.SKCanvasView> e)
        {
            base.OnElementChanged(e);
            if (e.OldElement is SKScrollView)
            {
                e.NewElement.Touch -= OnElementTouch;
            }
            if (e.NewElement is SKScrollView scrollView)
            {
                
                scrollView.WheelTouchSupported = true;
                scrollView.Touch += OnElementTouch;                
                if (Control != null)
                {
                    Control.Focusable = Element.IsTabStop;
                    Control.Touch += OnControlTouch;
                }
            }
        }

        private void OnControlTouch(object sender, TouchEventArgs e)
        {
            _scaleDetector.OnTouchEvent(e.Event);
            
        }

        private void OnElementTouch(object sender, SKTouchEventArgs e)
        {
            if (e.ActionType == SKTouchAction.Released)
            {
                Element.Focus();
                Control.RequestFocus();
            }
        }

        private class MyScaleListener : ScaleGestureDetector.SimpleOnScaleGestureListener
        {
            private readonly SKScrollViewRenderer _view;

            public MyScaleListener(SKScrollViewRenderer view)
            {
                _view = view;
            }

            public override bool OnScale(ScaleGestureDetector detector)
            {
                if (_view.Element is SKScrollView scrollView)
                {
                    scrollView.OnScrolled((int)detector.CurrentSpan, KeyModifiers.Ctrl);
                }
                return true;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (Element != null)
                {
                    Element.Touch -= OnElementTouch;
                }
                if (Control != null)
                {
                    Control.Touch -= OnControlTouch;
                }
            }
            base.Dispose(disposing);
        }

       
    }


}
