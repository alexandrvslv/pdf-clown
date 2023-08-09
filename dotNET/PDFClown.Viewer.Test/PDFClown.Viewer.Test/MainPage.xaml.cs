using PdfClown.Objects;
using PdfClown.Tokens;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using Xamarin.Forms;
using Xamarin.Forms.PlatformConfiguration.TizenSpecific;

namespace PdfClown.Viewer.Test
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private async void OnOpenFileClicked(object sender, EventArgs e)
        {
            var service = DependencyService.Get<IOpenFileService>();
            try
            {
                var fileInfo = await service.OpenFileDialog();
                if (fileInfo.Stream != null)
                {
                    label.Text = Path.GetFileName(fileInfo.FileName);
                    viewer.Load(fileInfo.Stream);
                    objects.Clear();
                    var items = new List<string>();
                    items.AddRange(ExtractDictionary(viewer.Document.File.Trailer));
                    list.ItemsSource = items;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error: " + ex.GetType().Name, ex.Message, "Close");
            }
        }

        private IEnumerable<string> ExtractDictionary(PdfDictionary dictionary, int level = 0)
        {
            foreach (var key in dictionary.Keys)
            {
                if (key.Equals(PdfName.Parent)
                    || key.Equals(PdfName.P)
                    || key.Equals(PdfName.Page))
                    continue;
                var prntKey = "".PadLeft(level, '-') + key.ToString();
                var obj = dictionary[key];
                foreach (var sub in Extract(obj, prntKey, level))
                    yield return sub;
            }
        }

        private IEnumerable<string> ExtractArray(PdfArray array, int level = 0)
        {
            int i = 0;
            foreach (var obj in array)
            {
                var prntKey = "".PadLeft(level, '-') + i.ToString();
                foreach (var sub in Extract(obj, prntKey, level))
                    yield return sub;
                i++;
            }
        }
        private HashSet<PdfObject> objects = new HashSet<PdfObject>();
        private IEnumerable<string> Extract(PdfObject obj, string prntKey, int level)
        {
            if (objects.Contains(obj))
                yield break;
            objects.Add(obj);

            if (obj is IPdfIndirectObject reference)
            {
                //yield return $"{prntKey} {reference} {reference.GetType()}";

                obj = reference.DataObject as PdfDirectObject;
                if (reference.DataObject is PdfStream stream)
                {
                    foreach (var sub in ExtractDictionary(stream.Header, level + 2))
                        yield return sub;
                }
                if (obj == null)
                {
                    yield break;
                }
            }

            if (obj is PdfDictionary subDictionary)
            {
                yield return $"{prntKey} {obj.GetType()}";
                foreach (var sub in ExtractDictionary(subDictionary, level + 2))
                    yield return sub;
            }
            else if (obj is PdfArray array)
            {
                yield return $"{prntKey} {obj.GetType()}";
                foreach (var sub in ExtractArray(array, level + 2))
                    yield return sub;
            }
            else
            {
                yield return $"{prntKey} {obj} {obj.GetType()}";
            }
        }


    }
}
