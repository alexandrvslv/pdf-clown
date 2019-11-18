using PdfClown.Documents;
using PdfClown.Documents.Contents;
using PdfClown.Files;
using PdfClown.Tools;

using System;
using System.Collections.Generic;
using SkiaSharp;

namespace PdfClown.Samples.CLI
{
    /**
      <summary>This sample demonstrates how to retrieve text content along with its graphic attributes
      (font, font size, text color, text rendering mode, text bounding box, and so on) from a PDF document;
      text is automatically sorted and aggregated.</summary>
    */
    public class AdvancedTextExtractionSample
      : Sample
    {
        public override void Run(
          )
        {
            // 1. Opening the PDF file...
            string filePath = PromptFileChoice("Please select a PDF file");
            using (var file = new File(filePath))
            {
                Document document = file.Document;

                // 2. Text extraction from the document pages.
                TextExtractor extractor = new TextExtractor();
                foreach (Page page in document.Pages)
                {
                    if (!PromptNextPage(page, false))
                    {
                        Quit();
                        break;
                    }

                    IList<ITextString> textStrings = extractor.Extract(page)[TextExtractor.DefaultArea];
                    foreach (ITextString textString in textStrings)
                    {
                        SKRect textStringBox = textString.Box.Value;
                        Console.WriteLine(
                          "Text ["
                            + "x:" + Math.Round(textStringBox.Left) + ","
                            + "y:" + Math.Round(textStringBox.Top) + ","
                            + "w:" + Math.Round(textStringBox.Width) + ","
                            + "h:" + Math.Round(textStringBox.Height)
                            + "]: " + textString.Text
                            );
                    }
                }
            }
        }
    }
}