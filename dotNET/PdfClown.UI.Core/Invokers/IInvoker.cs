﻿using System;
using System.Collections;
using System.ComponentModel;

namespace PdfClown.Util.Invokers
{
    public interface IInvoker
    {
        string Name { get; set; }
        Type DataType { get; }
        Type TargetType { get; }
        bool CanWrite { get; }

        object GetValue(object target);
        void SetValue(object target, object value);
    }

    public interface IInvoker<in T, V> : IInvoker
    {
        V GetValue(T target);
        void SetValue(T target, V value);
    }
}
