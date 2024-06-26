using PdfClown.Bytes;
using PdfClown.Documents;
using PdfClown.Files;
using PdfClown.Objects;

using System;
using System.IO;

namespace PdfClown.Samples.CLI
{
    /**
      <summary>This sample demonstrates how to extract XObject images from a PDF document.</summary>
      <remarks>
        <para>Inline images are ignored.</para>
        <para>XObject images other than JPEG aren't currently supported for handling.</para>
      </remarks>
    */
    public class ImageExtractionSample : Sample
    {
        public override void Run()
        {
            // 1. Opening the PDF file...
            string filePath = PromptFileChoice("Please select a PDF file");
            using (var file = new PdfFile(filePath))
            {
                // 2. Iterating through the indirect object collection...
                int index = 0;
                foreach (PdfIndirectObject indirectObject in file.IndirectObjects)
                {
                    // Get the data object associated to the indirect object!
                    PdfDataObject dataObject = indirectObject.DataObject;
                    // Is this data object a stream?
                    if (dataObject is PdfStream)
                    {
                        PdfDictionary header = ((PdfStream)dataObject).Header;
                        // Is this stream an image?
                        if (header.ContainsKey(PdfName.Type)
                          && header[PdfName.Type].Equals(PdfName.XObject)
                          && header[PdfName.Subtype].Equals(PdfName.Image))
                        {
                            // Which kind of image?
                            if (header[PdfName.Filter].Equals(PdfName.DCTDecode)) // JPEG image.
                            {
                                // Get the image data (keeping it encoded)!
                                IByteStream body = ((PdfStream)dataObject).GetBody(false);
                                // Export the image!
                                ExportImage(
                                  body,
                                  "ImageExtractionSample_" + (index++) + ".jpg"
                                  );
                            }
                            else // Unsupported image.
                            { Console.WriteLine("Image XObject " + indirectObject.Reference + " couldn't be extracted (filter: " + header[PdfName.Filter] + ")"); }
                        }
                    }
                }
            }
        }

        private void ExportImage(
          IByteStream data,
          string filename
          )
        {
            string outputPath = GetOutputPath(filename);
            FileStream outputStream;
            try
            { outputStream = new FileStream(outputPath, FileMode.CreateNew); }
            catch (Exception e)
            { throw new Exception(outputPath + " file couldn't be created.", e); }

            try
            {
                BinaryWriter writer = new BinaryWriter(outputStream);
                writer.Write(data.ToArray());
                writer.Close();
                outputStream.Close();
            }
            catch (Exception e)
            { throw new Exception(outputPath + " file writing has failed.", e); }

            Console.WriteLine("Output: " + outputPath);
        }
    }
}