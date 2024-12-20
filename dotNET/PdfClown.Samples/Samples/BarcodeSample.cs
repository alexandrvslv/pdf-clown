using PdfClown.Documents;
using PdfClown.Documents.Contents.Composition;
using PdfClown.Documents.Contents.Entities;
using PdfClown.Documents.Contents.Fonts;
using PdfClown.Util.Math;
using SkiaSharp;

namespace PdfClown.Samples.CLI
{
    /// <summary>This sample demonstrates how to show bar codes in a PDF document.</summary>
    public class BarcodeSample : Sample
    {
        private const float Margin = 36;

        public override void Run()
        {
            // 1. PDF file instantiation.
            var document = new PdfDocument();

            // 2. Content creation.
            Populate(document);

            // 3. Serialize the PDF file!
            Serialize(document, "Barcode", "showing barcodes", "barcodes, creation, EAN13");
        }

        /**
          <summary>Populates a PDF file with contents.</summary>
        */
        private void Populate(PdfDocument document)
        {
            // Get the abstract barcode entity!
            var barcode = new EAN13Barcode("8012345678901");
            // Create the reusable barcode within the document!
            var barcodeXObject = barcode.ToXObject(document);

            var pages = document.Pages;
            // Page 1.
            {
                var page = new PdfPage(document);
                pages.Add(page);
                SKSize pageSize = page.Size;

                var composer = new PrimitiveComposer(page);
                {
                    var blockComposer = new BlockComposer(composer);
                    blockComposer.Hyphenation = true;
                    blockComposer.Begin(
                      SKRect.Create(
                        Margin,
                        Margin,
                        pageSize.Width - Margin * 2,
                        pageSize.Height - Margin * 2),
                      XAlignmentEnum.Left,
                      YAlignmentEnum.Top);
                    var bodyFont = PdfType1Font.Load(document, FontName.CourierBold);
                    composer.SetFont(bodyFont, 32);
                    blockComposer.ShowText("Barcode sample"); blockComposer.ShowBreak();
                    composer.SetFont(bodyFont, 16);
                    blockComposer.ShowText("Showing the EAN-13 Bar Code on different compositions:"); blockComposer.ShowBreak();
                    blockComposer.ShowText("- page 1: on the lower right corner of the page, 100pt wide;"); blockComposer.ShowBreak();
                    blockComposer.ShowText("- page 2: on the middle of the page, 1/3-page wide, 25 degree counterclockwise rotated;"); blockComposer.ShowBreak();
                    blockComposer.ShowText("- page 3: filled page, 90 degree clockwise rotated."); blockComposer.ShowBreak();
                    blockComposer.End();
                }

                // Show the barcode!
                composer.ShowXObject(
                  barcodeXObject,
                  new SKPoint(pageSize.Width - Margin, pageSize.Height - Margin),
                  barcodeXObject.Size.Scale(new SKSize(100, 0)),
                  XAlignmentEnum.Right,
                  YAlignmentEnum.Bottom,
                  0);
                composer.Flush();
            }

            // Page 2.
            {
                var page = new PdfPage(document);
                pages.Add(page);
                SKSize pageSize = page.Size;

                var composer = new PrimitiveComposer(page);
                // Show the barcode!
                composer.ShowXObject(
                  barcodeXObject,
                  new SKPoint(pageSize.Width / 2, pageSize.Height / 2),
                  barcodeXObject.Size.Scale(new SKSize(pageSize.Width / 3, 0)),
                  XAlignmentEnum.Center,
                  YAlignmentEnum.Middle,
                  25);
                composer.Flush();
            }

            // Page 3.
            {
                var page = new PdfPage(document);
                pages.Add(page);
                SKSize pageSize = page.Size;

                var composer = new PrimitiveComposer(page);
                // Show the barcode!
                composer.ShowXObject(
                  barcodeXObject,
                  new SKPoint(pageSize.Width / 2, pageSize.Height / 2),
                  new SKSize(pageSize.Height, pageSize.Width),
                  XAlignmentEnum.Center,
                  YAlignmentEnum.Middle,
                  -90);
                composer.Flush();
            }
        }
    }
}