using Microsoft.Win32;
using PdfClown.Viewer.Test;
using PdfClown.Viewer.Test.Droid;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

[assembly: Xamarin.Forms.Dependency(typeof(OpenFileService))]
namespace PdfClown.Viewer.Test.Droid
{
    public class OpenFileService : IOpenFileService
    {
        public Task<(Stream Stream, string FileName)> OpenFileDialog()
        {
            var assembly = typeof(OpenFileService).Assembly;
            var keyName = $"{assembly.GetName().Name}.eastman.pdf";
            var names = assembly.GetManifestResourceNames();
            return Task.FromResult<(Stream, string)>((assembly.GetManifestResourceStream(keyName), "eastman.pdf"));
        }

    }
}