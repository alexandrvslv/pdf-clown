using PdfClown.Documents;
using PdfClown.Documents.Contents;
using PdfClown.Documents.Contents.Objects;
using PdfClown.Documents.Contents.Scanner;
using SkiaSharp;
using System;
using xObjects = PdfClown.Documents.Contents.XObjects;

namespace PdfClown.Samples.CLI
{
    /**
      <summary>This sample demonstrates how to retrieve the precise position (page and coordinates)
      of each image within a PDF document, using the page content scanning functionality.</summary>
      <remarks>This sample leverages the ContentScanner class, a powerful device for accessing
      each single content object within a page.</remarks>
    */
    public class ContentScanningSample : Sample
    {
        public override void Run()
        {
            // 1. Opening the PDF file...
            string filePath = PromptFileChoice("Please select a PDF file");
            using (var file = new PdfFile(filePath))
            {
                PdfDocument document = file.Document;

                // 2. Parsing the document...
                Console.WriteLine("\nLooking for images...");
                foreach (var page in document.Pages)
                {
                    Scan(new ContentScanner(page), // Wraps the page contents into the scanner.
                      page);
                }
            }
        }

        /**
          <summary>Scans a content level looking for images.</summary>
        */
        /*
          NOTE: Page contents are represented by a sequence of content objects,
          possibly nested into multiple levels.
        */
        private void Scan(ContentScanner level, PdfPage page)
        {
            if (level == null)
                return;

            while (level.MoveNext())
            {
                ContentObject current = level.Current;
                if (current is ContainerObject)
                {
                    // Scan the inner level!
                    Scan(level.ChildLevel, page);
                }
                else
                {
                    GraphicsObjectWrapper objectWrapper = level.CurrentWrapper;
                    if (objectWrapper == null)
                        continue;

                    /*
                      NOTE: Images can be represented on a page either as
                      external objects (XObject) or inline objects.
                    */
                    SKSize? imageSize = null; // Image native size.
                    if (objectWrapper is XObjectWrapper)
                    {
                        XObjectWrapper xObjectWrapper = (XObjectWrapper)objectWrapper;
                        xObjects::XObject xObject = xObjectWrapper.XObject;
                        // Is the external object an image?
                        if (xObject is xObjects::ImageXObject)
                        {
                            Console.Write(
                              "External Image '" + xObjectWrapper.Name + "' (" + xObject.BaseObject + ")" // Image key and indirect reference.
                              );
                            imageSize = xObject.Size; // Image native size.
                        }
                    }
                    else if (objectWrapper is InlineImageWrapper)
                    {
                        Console.Write("Inline Image");
                        GraphicsInlineImage inlineImage = ((InlineImageWrapper)objectWrapper).InlineImage;
                        imageSize = inlineImage.Size; // Image native size.
                    }

                    if (imageSize.HasValue)
                    {
                        SKRect box = objectWrapper.Box.Value; // Image position (location and size) on the page.
                        Console.WriteLine(
                          " on page " + page.Number + " (" + page.BaseObject + ")" // Page index and indirect reference.
                          );
                        Console.WriteLine("  Coordinates:");
                        Console.WriteLine("     x: " + Math.Round(box.Left));
                        Console.WriteLine("     y: " + Math.Round(box.Top));
                        Console.WriteLine("     width: " + Math.Round(box.Width) + " (native: " + Math.Round(imageSize.Value.Width) + ")");
                        Console.WriteLine("     height: " + Math.Round(box.Height) + " (native: " + Math.Round(imageSize.Value.Height) + ")");
                    }
                }
            }
        }
    }
}