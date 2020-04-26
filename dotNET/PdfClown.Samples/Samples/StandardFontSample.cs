using PdfClown.Documents;
using PdfClown.Documents.Contents.Composition;
using PdfClown.Documents.Contents.Fonts;
using PdfClown.Files;
using PdfClown.Tokens;

using System;
using System.Collections.Generic;
using SkiaSharp;
using System.Linq;

namespace PdfClown.Samples.CLI
{
    /**
      <summary>This sample demonstrates the use of standard Type 1 fonts, which are the 14 built-in fonts
      prescribed by the PDF specification to be shipped along with any conformant PDF viewer.</summary>
      <remarks>In particular, this sample displays the complete glyphset of each standard font,
      iterating through character codes and glyph styles (regular, italic, bold).</remarks>
    */
    public class StandardFontSample : Sample
    {
        private static readonly int FontBaseSize = 20;
        private static readonly int Margin = 50;

        public override void Run(
          )
        {
            // 1. PDF file instantiation.
            File file = new File();
            Document document = file.Document;

            // 2. Content creation.
            Populate(document);

            // 3. Serialize the PDF file!
            Serialize(file, "Standard Type 1 fonts", "applying standard Type 1 fonts", "Standard Type1 fonts");
        }

        private void Populate(Document document)
        {
            Page page = new Page(document);
            document.Pages.Add(page);
            SKSize pageSize = page.Size;

            /*
              NOTE: Default fallback behavior on text encoding mismatch is substitution with default
              character; in this case, we want to force an exception to be thrown so we can explicitly
              handle the issue.
            */
            document.Configuration.EncodingFallback = EncodingFallbackEnum.Exception;

            PrimitiveComposer composer = new PrimitiveComposer(page);

            int x = Margin, y = Margin;
            PdfType1Font titleFont = PdfType1Font.Load(document, PdfType1Font.FamilyEnum.Times, true, true);
            PdfType1Font font = null;
            // Iterating through the standard Type 1 fonts...
            foreach (PdfType1Font.FamilyEnum fontFamily
              in (PdfType1Font.FamilyEnum[])Enum.GetValues(typeof(PdfType1Font.FamilyEnum)))
            {
                // Iterating through the font styles...
                for (int styleIndex = 0; styleIndex < 4; styleIndex++)
                {
                    /*
                      NOTE: Symbol and Zapf Dingbats are available just as regular fonts (no italic or bold variant).
                    */
                    if (styleIndex > 0
                      && (fontFamily == PdfType1Font.FamilyEnum.Symbol
                        || fontFamily == PdfType1Font.FamilyEnum.ZapfDingbats))
                        break;

                    bool bold = (styleIndex & 1) > 0;
                    bool italic = (styleIndex & 2) > 0;

                    // Define the font used to show its character set!
                    font = PdfType1Font.Load(document, fontFamily, bold, italic);

                    if (y > pageSize.Height - Margin)
                    {
                        composer.Flush();

                        page = new Page(document);
                        document.Pages.Add(page);
                        pageSize = page.Size;
                        composer = new PrimitiveComposer(page);
                        x = Margin;
                        y = Margin;
                    }

                    if (styleIndex == 0)
                    {
                        composer.DrawLine(
                          new SKPoint(x, y),
                          new SKPoint(pageSize.Width - Margin, y)
                          );
                        composer.Stroke();
                        y += 5;
                    }

                    composer.SetFont(
                      titleFont,
                      FontBaseSize * (styleIndex == 0 ? 1.5f : 1)
                      );
                    composer.ShowText(
                      fontFamily.ToString() + (bold ? " bold" : "") + (italic ? " italic" : ""),
                      new SKPoint(x, y)
                      );

                    y += 40;
                    // Set the font used to show its character set!
                    composer.SetFont(font, FontBaseSize);
                    // Iterating through the font characters...
                    foreach (int charCode in font.CodePoints.OrderBy(codePoint => codePoint))
                    {
                        if (y > pageSize.Height - Margin)
                        {
                            composer.Flush();

                            page = new Page(document);
                            document.Pages.Add(page);
                            pageSize = page.Size;
                            composer = new PrimitiveComposer(page);
                            x = Margin;
                            y = Margin;

                            composer.SetFont(titleFont, FontBaseSize);
                            composer.ShowText(
                              fontFamily.ToString() + " (continued)",
                              new SKPoint(pageSize.Width - Margin, y),
                              XAlignmentEnum.Right,
                              YAlignmentEnum.Top,
                              0
                              );
                            composer.SetFont(font, FontBaseSize);
                            y += FontBaseSize * 2;
                        }

                        try
                        {
                            // Show the character!
                            composer.ShowText(
                              new String((char)charCode, 1),
                              new SKPoint(x, y)
                              );
                            x += FontBaseSize;
                            if (x > pageSize.Width - Margin)
                            { x = Margin; y += 30; }
                        }
                        catch (EncodeException)
                        {
                            /*
                              NOOP -- NOTE: document.Configuration.EncodingFallback allows to customize the
                              behavior in case of missing character: we can alternatively catch an exception, have
                              the character substituted by a default one (typically '?' symbol) or have the
                              character silently excluded.
                            */
                        }
                    }

                    x = Margin;
                    y += Margin;
                }
            }
            composer.Flush();
        }
    }
}