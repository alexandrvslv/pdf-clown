using PdfClown.Documents;
using PdfClown.Documents.Contents;
using PdfClown.Documents.Contents.ColorSpaces;
using PdfClown.Documents.Contents.Composition;
using PdfClown.Documents.Contents.Fonts;
using PdfClown.Documents.Contents.XObjects;
using PdfClown.Tools;
using SkiaSharp;

namespace PdfClown.Samples.CLI
{
    /// <summary>This sample demonstrates how to insert semi-transparent watermark text into an
    /// existing PDF document.</summary>
    /// <remarks>
    ///   <para> The watermark is implemented as a Form XObject [PDF:1.6:4.9] to conveniently achieve a
    ///   consistent page look -- Form XObjects promote content reuse providing an independent context
    ///   which encapsulates contents (and resources) in a single stream.</para>
    ///   <para>The watermark is seamlessly inserted upon each page content using the PageStamper class.
    ///   </para>
    /// </remarks>
    public class WatermarkSample : Sample
    {
        public override void Run()
        {
            // 1. Open the PDF file!
            string filePath = PromptFileChoice("Please select a PDF file");
            using (var document = new PdfDocument(filePath))
            {
                // 2. Create a watermark!
                FormXObject watermark = CreateWatermark(document);

                // 3. Apply the watermark to the pages of the document!
                ApplyWatermark(watermark);

                // 4. Serialize the PDF file!
                Serialize(document, "Watermark", "how to place some content behind existing pages", "watermark, transparent text, background, foreground, page, composition");
            }
        }

        private void ApplyWatermark(FormXObject watermark)
        {
            // 1. Instantiate the stamper!
            /* NOTE: The PageStamper is optimized for dealing with pages. */
            var stamper = new PageStamper();

            // 2. Inserting the watermark into each page of the document...
            foreach (var page in watermark.Document.Pages)
            {
                // 2.1. Associate the page to the stamper!
                stamper.Page = page;

                // 2.2. Stamping the watermark on the foreground...
                // Get the content composer!
                PrimitiveComposer composer = stamper.Foreground;
                // Show the watermark into the page background!
                composer.ShowXObject(watermark);

                // 2.3. End the stamping!
                stamper.Flush();
            }
        }

        private FormXObject CreateWatermark(PdfDocument document)
        {
            SKSize size = document.Catalog.GetSize();

            // 1. Create an external form object to represent the watermark!
            var watermark = new FormXObject(document, size);

            // 2. Inserting the contents of the watermark...
            // 2.1. Create a content composer!
            var composer = new PrimitiveComposer(watermark);
            // 2.2. Inserting the contents...
            // Set the font to use!
            composer.SetFont(PdfType1Font.Load(document, FontName.TimesBold), 120);
            // Set the color to fill the text characters!
            composer.SetFillColor(new RGBColor(115 / 255d, 164 / 255d, 232 / 255d));
            // Apply transparency!
            {
                var state = new ExtGState(document);
                state.FillAlpha = .3F;
                composer.ApplyState(state);
            }
            // Show the text!
            composer.ShowText(
              "PdfClown", // Text to show.
              new SKPoint(size.Width / 2f, size.Height / 2f), // Anchor location: page center.
              XAlignmentEnum.Center, // Horizontal placement (relative to the anchor): center.
              YAlignmentEnum.Middle, // Vertical placement (relative to the anchor): middle.
              50); // Rotation: 50-degree-counterclockwise.

            // 2.3. Flush the contents into the watermark!
            composer.Flush();

            return watermark;
        }
    }
}