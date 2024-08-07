﻿using Microsoft.Win32;
using PdfClown.UI.Test;
using PdfClown.UI.Test.WPF;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

[assembly: Xamarin.Forms.Dependency(typeof(OpenFileService))]
namespace PdfClown.UI.Test.WPF
{
    public class OpenFileService : IOpenFileService
    {
        private const string formats = "PostScript Documents|*.pdf|Any Documents|*.*";
        private static readonly OpenFileDialog dialog = new OpenFileDialog
        {
            Filter = formats,
            Multiselect = false
        };
        public Task<(Stream Stream, string FileName)> OpenFileDialog()
        {
            if (dialog.ShowDialog() ?? false)
            {
                return Task.FromResult<(Stream, string)>((new FileStream(dialog.FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite), dialog.FileName));
            }
            return Task.FromResult<(Stream, string)>((null, null));
        }
    }


}
