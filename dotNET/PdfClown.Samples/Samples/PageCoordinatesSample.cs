using PdfClown.Documents;
using PdfClown.Documents.Contents;
using colorSpaces = PdfClown.Documents.Contents.ColorSpaces;
using PdfClown.Documents.Contents.Composition;
using PdfClown.Documents.Contents.Fonts;
using PdfClown.Documents.Contents.Objects;
using PdfClown.Files;
using PdfClown.Objects;

using System;
using SkiaSharp;
using System.Drawing.Drawing2D;

namespace PdfClown.Samples.CLI
{
    /**
      <summary>This sample shows the effects of the manipulation of the CTM (Current Transformation
      Matrix), that is the logical device which affects the PDF page coordinate system used to place graphics
      contents onto the canvas.</summary>
    */
    public class PageCoordinatesSample : Sample
    {
        private static readonly PdfName ResourceName_DefaultFont = new PdfName("default");

        private static float Max(params float[] values)
        {
            float maxValue = values[0];
            foreach (float value in values)
            { maxValue = Math.Max(maxValue, value); }
            return maxValue;
        }

        public override void Run()
        {
            // 1. Instantiate a new PDF file!
            File file = new File();
            Document document = file.Document;

            // 2. Insert the contents into the document!
            BuildContent(document);

            // 3. Serialize the PDF file!
            Serialize(file, "Page coordinates", "manipulating the CTM", "page coordinates, ctm");
        }

        private void BuildContent(Document document)
        {
            // Set default page size (A4)!
            document.PageSize = PageFormat.GetSize();
            // Add a font to the fonts collection!
            document.Resources.Fonts[ResourceName_DefaultFont] = PdfType1Font.Load(document, PdfType1Font.FamilyEnum.Courier, true, false);

            // Add a page to the document!
            Page page = new Page(document); // Instantiates the page inside the document context.
            document.Pages.Add(page); // Puts the page in the pages collection.

            // Create a content composer for the page content stream!
            PrimitiveComposer composer = new PrimitiveComposer(page);

            string[] steps = new string[5];
            colorSpaces::Color[] colors = new colorSpaces::Color[5];
            SKSize pageSize = page.Size;

            BuildSteps(composer, steps, colors, pageSize);

            BuildLegend(composer, steps, colors, pageSize);

            composer.Flush();
        }

        private string GetStepNote(
          PrimitiveComposer composer,
          string comment
          )
        {
            // Get the CTM!
            var ctm = composer.Scanner.State.Ctm;

            return "CTM (" + comment + "): " + ctm.ScaleX + ", " + ctm.SkewY + ", " + ctm.SkewX + ", " + ctm.ScaleY + ", " + ctm.TransX + ", " + ctm.TransY;
        }

        private void BuildLegend(PrimitiveComposer composer, string[] steps, colorSpaces::Color[] colors, SKSize pageSize)
        {
            float maxCtmInversionApproximation;
            {
                float[] ctmInversionApproximations = new float[9];
                {
                    float[] initialCtmValues, finalCtmValues;
                    {
                        var state = composer.Scanner.State;
                        initialCtmValues = state.GetInitialCtm().Values;
                        finalCtmValues = state.Ctm.Values;
                    }
                    for (
                      int index = 0,
                        length = finalCtmValues.Length;
                      index < length;
                      index++
                      )
                    { ctmInversionApproximations[index] = Math.Abs(finalCtmValues[index]) - initialCtmValues[index]; }
                }
                maxCtmInversionApproximation = Max(ctmInversionApproximations);
            }

            BlockComposer blockComposer = new BlockComposer(composer);
            blockComposer.LineSpace = new Length(.25, Length.UnitModeEnum.Relative);

            composer.BeginLocalState();
            composer.SetFillColor(
              new colorSpaces::DeviceRGBColor(115 / 255d, 164 / 255d, 232 / 255d)
              );
            SKRect frame = SKRect.Create(
              18,
              18,
              pageSize.Width * .5f,
              pageSize.Height * .5f
              );
            blockComposer.Begin(frame, XAlignmentEnum.Left, YAlignmentEnum.Top);
            composer.SetFont(ResourceName_DefaultFont, 24);
            blockComposer.ShowText("Page coordinates sample");
            SKSize breakSize = new SKSize(0, 8);
            blockComposer.ShowBreak(breakSize);
            composer.SetFont(ResourceName_DefaultFont, 8);
            blockComposer.ShowText(
              "This sample shows the effects of the manipulation of the CTM (Current Transformation Matrix), "
                + "that is the mathematical device which affects the page coordinate system used to place "
                + "graphic contents onto the canvas."
              );
            blockComposer.ShowBreak(breakSize);
            blockComposer.ShowText(
              "The following steps represent the operations applied to this page's CTM in order to alter it. "
                + "Each step writes the word \"Step\" at the lower-left corner of the current page frame:"
              );
            blockComposer.ShowBreak(breakSize);
            for (int i = 0; i < steps.Length; i++)
            {
                composer.SetFillColor(colors[i]);
                blockComposer.ShowText("Step " + i + ")");
                composer.SetFillColor(
                  new colorSpaces::DeviceRGBColor(115 / 255d, 164 / 255d, 232 / 255d)
                  );
                blockComposer.ShowText(" " + steps[i]);
                blockComposer.ShowBreak(breakSize);
            }
            blockComposer.ShowText("Note that the approximation (" + maxCtmInversionApproximation + ") of the CTM components at step 4 is due to floating point precision limits; their exact values should be 1.0, 0.0, 0.0, 1.0, 0.0, 0.0.");
            blockComposer.End();
            composer.End();
        }

        private void BuildSteps(
          PrimitiveComposer composer,
          string[] steps,
          colorSpaces::Color[] colors,
          SKSize pageSize
          )
        {
            composer.SetFont(ResourceName_DefaultFont, 32);
            SKRect frame = SKRect.Create(
              0,
              0,
              pageSize.Width,
              pageSize.Height
              );

            // Step 0.
            {
                colors[0] = new colorSpaces::DeviceRGBColor(30 / 255d, 10 / 255d, 0);
                composer.SetFillColor(colors[0]);
                composer.SetStrokeColor(colors[0]);

                // Draw the page frame!
                composer.DrawRectangle(frame);
                composer.Stroke();

                // Draw the lower-left corner mark!
                composer.ShowText(
                  "Step 0",
                  new SKPoint(0, pageSize.Height),
                  XAlignmentEnum.Left,
                  YAlignmentEnum.Bottom,
                  0
                  );

                steps[0] = GetStepNote(composer, "default");
            }

            // Step 1.
            {
                colors[1] = new colorSpaces::DeviceRGBColor(80 / 255d, 25 / 255d, 0);
                composer.SetFillColor(colors[1]);
                composer.SetStrokeColor(colors[1]);

                // Transform the coordinate space, applying translation!
                composer.Translate(72, 72);

                // Draw the page frame!
                composer.DrawRectangle(frame);
                composer.Stroke();

                // Draw the lower-left corner mark!
                composer.ShowText(
                  "Step 1",
                  new SKPoint(0, pageSize.Height),
                  XAlignmentEnum.Left,
                  YAlignmentEnum.Bottom,
                  0
                  );

                steps[1] = GetStepNote(composer, "after translate(72,72)");
            }

            // Step 2.
            {
                colors[2] = new colorSpaces::DeviceRGBColor(130 / 255d, 45 / 255d, 0);
                composer.SetFillColor(colors[2]);
                composer.SetStrokeColor(colors[2]);

                // Transform the coordinate space, applying clockwise rotation!
                composer.Rotate(-20);

                // Draw the page frame!
                composer.DrawRectangle(frame);
                composer.Stroke();

                // Draw the coordinate space origin mark!
                composer.ShowText("Origin 2");

                // Draw the lower-left corner mark!
                composer.ShowText(
                  "Step 2",
                  new SKPoint(0, pageSize.Height),
                  XAlignmentEnum.Left,
                  YAlignmentEnum.Bottom,
                  0
                  );

                steps[2] = GetStepNote(composer, "after rotate(20)");
            }

            // Step 3.
            {
                colors[3] = new colorSpaces::DeviceRGBColor(180 / 255d, 60 / 255d, 0);
                composer.SetFillColor(colors[3]);
                composer.SetStrokeColor(colors[3]);

                // Transform the coordinate space, applying translation and scaling!
                composer.Translate(0, 72);
                composer.Scale(.5f, .5f);

                // Draw the page frame!
                composer.DrawRectangle(frame);
                composer.Stroke();

                // Draw the lower-left corner mark!
                composer.ShowText(
                  "Step 3",
                  new SKPoint(0, pageSize.Height),
                  XAlignmentEnum.Left,
                  YAlignmentEnum.Bottom,
                  0
                  );

                steps[3] = GetStepNote(composer, "after translate(0,72) and scale(.5,.5)");
            }

            // Step 4.
            {
                colors[4] = new colorSpaces::DeviceRGBColor(230 / 255d, 75 / 255d, 0);
                composer.SetFillColor(colors[4]);
                composer.SetStrokeColor(colors[4]);

                // Transform the coordinate space, restoring its initial CTM!
                composer.Add(
                  ModifyCTM.GetResetCTM(
                    composer.Scanner.State
                    )
                  );

                // Draw the page frame!
                composer.DrawRectangle(frame);
                composer.Stroke();

                // Draw the lower-left corner mark!
                composer.ShowText(
                  "Step 4",
                  new SKPoint(0, pageSize.Height),
                  XAlignmentEnum.Left,
                  YAlignmentEnum.Bottom,
                  0
                  );

                steps[4] = GetStepNote(composer, "after resetting CTM");
            }
        }
    }
}