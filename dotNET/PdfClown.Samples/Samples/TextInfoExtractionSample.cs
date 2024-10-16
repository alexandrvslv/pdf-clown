using PdfClown.Documents;
using PdfClown.Documents.Contents;
using PdfClown.Documents.Contents.ColorSpaces;
using PdfClown.Documents.Contents.Composition;
using PdfClown.Documents.Contents.Objects;
using PdfClown.Documents.Contents.Scanner;
using PdfClown.Tools;
using SkiaSharp;
using System;

namespace PdfClown.Samples.CLI
{
    /// <summary>This sample demonstrates how to retrieve text content along with its graphic attributes
    /// (font, font size, text color, text rendering mode, text bounding box...) from a PDF document;
    /// it also generates a document version decorated by text bounding boxes.</summary>
    public class TextInfoExtractionSample : Sample
    {
        private DeviceRGBColor[] textCharBoxColors = new DeviceRGBColor[]
          {
        new DeviceRGBColor(200 / 255d, 100 / 255d, 100 / 255d),
        new DeviceRGBColor(100 / 255d, 200 / 255d, 100 / 255d),
        new DeviceRGBColor(100 / 255d, 100 / 255d, 200 / 255d)
          };
        private DeviceRGBColor textStringBoxColor = DeviceRGBColor.Black;

        public override void Run()
        {
            // 1. Opening the PDF file...
            string filePath = PromptFileChoice("Please select a PDF file");
            using (var file = new PdfFile(filePath))
            {
                PdfDocument document = file.Document;

                PageStamper stamper = new PageStamper(); // NOTE: Page stamper is used to draw contents on existing pages.

                // 2. Iterating through the document pages...
                foreach (var page in document.Pages)
                {
                    Console.WriteLine("\nScanning page " + page.Number + "...\n");

                    stamper.Page = page;

                    Extract(new ContentScanner(page), // Wraps the page contents into a scanner.
                      stamper.Foreground);

                    stamper.Flush();
                }

                // 3. Decorated version serialization.
                Serialize(file);
            }
        }

        /// <summary>Scans a content level looking for text.</summary>
        // NOTE: Page contents are represented by a sequence of content objects,
        // possibly nested into multiple levels.
        private void Extract(ContentScanner level, PrimitiveComposer composer)
        {
            if (level == null)
                return;
            level.OnObjectScanned += OnObjectFinish;
            level.Scan();
            level.OnObjectScanned -= OnObjectFinish;
            void OnObjectFinish(ContentObject content)
            {
                if (content is GraphicsText text)
                {
                    int colorIndex = 0;
                    foreach (var textString in text.Strings)
                    {
                        SKRect textStringBox = textString.Quad.GetBounds();
                        Console.WriteLine(
                          "Text ["
                            + "x:" + Math.Round(textStringBox.Left) + ","
                            + "y:" + Math.Round(textStringBox.Top) + ","
                            + "w:" + Math.Round(textStringBox.Width) + ","
                            + "h:" + Math.Round(textStringBox.Height)
                            + "] [font size:" + Math.Round(textString.Style.FontSize) + "]: " + textString.Text
                            );

                        // Drawing text character bounding boxes...
                        colorIndex = (colorIndex + 1) % textCharBoxColors.Length;
                        composer.SetStrokeColor(textCharBoxColors[colorIndex]);
                        foreach (TextChar textChar in textString.Chars)
                        {
                            // NOTE: You can get further text information
                            // (font, font size, text color, text rendering mode)
                            // through textChar.style.
                            composer.DrawPolygon(textChar.Quad.GetPoints());
                            composer.Stroke();
                        }

                        // Drawing text string bounding box...
                        composer.BeginLocalState();
                        composer.SetLineDash(new LineDash(new float[] { 5 }));
                        composer.SetStrokeColor(textStringBoxColor);
                        composer.DrawRectangle(textString.Quad.GetBounds());
                        composer.Stroke();
                        composer.End();
                    }
                }
                else if (content is PaintXObject paint)
                {
                    // Scan the external level!
                    Extract(paint.GetScanner(level), composer);
                }
            }
        }
    }
}