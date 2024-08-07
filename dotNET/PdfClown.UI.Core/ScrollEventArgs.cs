﻿using System;

namespace PdfClown.UI
{
    public class ScrollEventArgs : EventArgs
    {
        public ScrollEventArgs(int delta, KeyModifiers keyModifiers)
        {
            Delta = delta;
            Modifiers = keyModifiers;
        }

        public int Delta { get; }
        public KeyModifiers Modifiers { get; }
    }
}
