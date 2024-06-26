using PdfClown.Documents;
using PdfClown.Documents.Interaction.Navigation;
using PdfClown.Files;
using PdfClown.Objects;

using System;
using System.Collections.Generic;

namespace PdfClown.Samples.CLI
{
    /**
      <summary>This sample demonstrates how to inspect the object names within a PDF document.</summary>
    */
    public class NamesParsingSample
      : Sample
    {
        public override void Run(
          )
        {
            // 1. Opening the PDF file...
            string filePath = PromptFileChoice("Please select a PDF file");
            using (var file = new PdfFile(filePath))
            {
                PdfDocument document = file.Document;

                // 2. Named objects extraction.
                Names names = document.Names;
                if (!names.Exists())
                { Console.WriteLine("\nNo names dictionary."); }
                else
                {
                    Console.WriteLine("\nNames dictionary found (" + names.DataContainer.Reference + ")");

                    NamedDestinations namedDestinations = names.Destinations;
                    if (!namedDestinations.Exists())
                    { Console.WriteLine("\nNo named destinations."); }
                    else
                    {
                        Console.WriteLine("\nNamed destinations found (" + namedDestinations.DataContainer.Reference + ")");

                        // Parsing the named destinations...
                        foreach (KeyValuePair<PdfString, Destination> namedDestination in namedDestinations)
                        {
                            PdfString key = namedDestination.Key;
                            Destination value = namedDestination.Value;

                            Console.WriteLine("  Destination '" + key + "' (" + value.DataContainer.Reference + ")");

                            Console.Write("    Target Page: number = ");
                            object pageRef = value.Page;
                            if (pageRef is Int32) // NOTE: numeric page refs are typical of remote destinations.
                            { Console.WriteLine(((int)pageRef) + 1); }
                            else // NOTE: explicit page refs are typical of local destinations.
                            {
                                var page = (PdfPage)pageRef;
                                Console.WriteLine(page.Number + "; ID = " + ((PdfReference)page.BaseObject).Id);
                            }
                        }

                        Console.WriteLine("Named destinations count = " + namedDestinations.Count);
                    }
                }
            }
        }
    }
}