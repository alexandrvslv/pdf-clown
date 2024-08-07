﻿using System;
using System.ComponentModel;

namespace PdfClown.Documents.Interaction.Annotations
{
    public class AnnotationEventArgs : CancelEventArgs
    {
        public AnnotationEventArgs(Annotation annotation) : base(false)
        {
            this.Annotation = annotation;
        }

        public Annotation Annotation { get; }
    }
}