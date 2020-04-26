using PdfClown.Documents;
using PdfClown.Documents.Contents.Composition;
using PdfClown.Documents.Contents.Fonts;
using PdfClown.Files;

using System;
using SkiaSharp;

namespace PdfClown.Samples.CLI
{
    /**
      <summary>This sample generates a series of PDF pages from the default page formats available,
      varying both in size and orientation.</summary>
    */
    public class PageFormatSample
      : Sample
    {
        public override void Run(
          )
        {
            // 1. PDF file instantiation.
            File file = new File();
            Document document = file.Document;

            // 2. Populate the document!
            Populate(document);

            // 3. Serialize the PDF file!
            Serialize(file, "Page Format", "page formats", "page formats");
        }

        private void Populate(Document document)
        {
            PdfType1Font bodyFont = PdfType1Font.Load(document, PdfType1Font.FamilyEnum.Courier, true, false);

            Pages pages = document.Pages;
            PageFormat.SizeEnum[] pageFormats = (PageFormat.SizeEnum[])Enum.GetValues(typeof(PageFormat.SizeEnum));
            PageFormat.OrientationEnum[] pageOrientations = (PageFormat.OrientationEnum[])Enum.GetValues(typeof(PageFormat.OrientationEnum));
            foreach (PageFormat.SizeEnum pageFormat in pageFormats)
            {
                foreach (PageFormat.OrientationEnum pageOrientation in pageOrientations)
                {
                    // Add a page to the document!
                    Page page = new Page(
                      document,
                      PageFormat.GetSize(
                        pageFormat,
                        pageOrientation
                        )
                      ); // Instantiates the page inside the document context.
                    pages.Add(page); // Puts the page in the pages collection.

                    // Drawing the text label on the page...
                    SKSize pageSize = page.Size;
                    PrimitiveComposer composer = new PrimitiveComposer(page);
                    composer.SetFont(bodyFont, 32);
                    composer.ShowText(
                      pageFormat + " (" + pageOrientation + ")", // Text.
                      new SKPoint(
                        pageSize.Width / 2,
                        pageSize.Height / 2
                        ), // Location: page center.
                      XAlignmentEnum.Center, // Places the text on horizontal center of the location.
                      YAlignmentEnum.Middle, // Places the text on vertical middle of the location.
                      45 // Rotates the text 45 degrees counterclockwise.
                      );
                    composer.Flush();
                }
            }
        }
    }
}