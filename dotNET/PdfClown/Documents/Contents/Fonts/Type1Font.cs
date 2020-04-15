/*
  Copyright 2007-2015 Stefano Chizzolini. http://www.pdfclown.org

  Contributors:
    * Stefano Chizzolini (original code developer, http://www.stefanochizzolini.it)

  This file should be part of the source code distribution of "PDF Clown library" (the
  Program): see the accompanying README files for more info.

  This Program is free software; you can redistribute it and/or modify it under the terms
  of the GNU Lesser General Public License as published by the Free Software Foundation;
  either version 3 of the License, or (at your option) any later version.

  This Program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY,
  either expressed or implied; without even the implied warranty of MERCHANTABILITY or
  FITNESS FOR A PARTICULAR PURPOSE. See the License for more details.

  You should have received a copy of the GNU Lesser General Public License along with this
  Program (see README files); if not, go to the GNU website (http://www.gnu.org/licenses/).

  Redistribution and use, with or without modification, are permitted provided that such
  redistributions retain the above copyright notice, license and disclaimer, along with
  this list of conditions.
*/

using PdfClown.Bytes;
using PdfClown.Documents;
using PdfClown.Objects;
using PdfClown.Util;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using text = System.Text;
using System.Text.RegularExpressions;
using SkiaSharp;

namespace PdfClown.Documents.Contents.Fonts
{
    /**
      <summary>Type 1 font [PDF:1.6:5.5.1;AFM:4.1].</summary>
    */
    /*
      NOTE: Type 1 fonts encompass several formats:
      * AFM+PFB;
      * CFF;
      * OpenFont/CFF (in case "CFF" table's Top DICT has no CIDFont operators).
    */
    [PDF(VersionEnum.PDF10)]
    public class Type1Font : SimpleFont
    {
        #region dynamic
        #region fields
        protected AfmParser.FontMetrics metrics;
        #endregion

        #region constructors
        internal Type1Font(Document context) : base(context)
        { }

        internal Type1Font(PdfDirectObject baseObject) : base(baseObject)
        { }
        #endregion

        protected override void OnLoad()
        {
            base.OnLoad();

            if (BaseDataObject.Resolve(PdfName.Encoding) is PdfDictionary encodingDictionary
                && encodingDictionary.Values.Count > 0
                && encodingDictionary.Resolve(PdfName.Differences) is PdfArray differencesObject)
            {
                var fontMapping = (GlyphMapping)null;

                if (BaseDataObject.Resolve(PdfName.BaseFont) is PdfName pdfName)
                {
                    var name = Regex.Replace(pdfName.RawValue, @"[\/?:*""><|]+", "", RegexOptions.Compiled);
                    if (GlyphMapping.IsExist(name))
                    {
                        fontMapping = new GlyphMapping(name);
                    }
                }

                byte[] charCodeData = new byte[1];
                foreach (PdfDirectObject differenceObject in differencesObject)
                {
                    if (differenceObject is PdfInteger pdfInteger) // Subsequence initial code.
                    { charCodeData[0] = (byte)(((int)pdfInteger.Value) & 0xFF); }
                    else // Character name.
                    {
                        ByteArray charCode = new ByteArray(charCodeData);
                        string charName = (string)((PdfName)differenceObject).Value;
                        if (charName.Equals(".notdef", StringComparison.Ordinal))
                        { codes.Remove(charCode); }
                        else
                        {
                            int? code = GlyphMapping.DLFONT.NameToCode(charName) ?? fontMapping?.NameToCode(charName);
                            if (code != null)
                            {
                                codes[charCode] = code.Value;
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($" {charName}");
                            }
                        }
                        charCodeData[0]++;
                    }
                }
            }
        }

        protected override SKTypeface GetTypeface(PdfDictionary fontDescription, PdfStream stream)
        {
            var name = fontDescription.Resolve(PdfName.FontName)?.ToString();
            var buffer = stream.GetBody(true);

            //System.IO.File.WriteAllBytes($"export{name}_part2.psc", bytes);
            var bytes = buffer.ToByteArray();
            var typeface = (SKTypeface)null;
            using (var data = new SKMemoryStream(bytes))
            {
                typeface = SKFontManager.Default.CreateTypeface(data);
            }

            if (typeface == null)
            {
#if DEBUG
                name = Regex.Replace(name, @"[\/?:*""><|]+", "", RegexOptions.Compiled);
                try { System.IO.File.WriteAllBytes($"export_{name}.psc", bytes); }
                catch { }
                //if (typeface == null)
                //{
                //    using (var manifestStream = typeof(Type1Font).Assembly.GetManifestResourceStream(name + ".otf"))
                //    {
                //        if (manifestStream != null)
                //        {
                //            typeface = SKFontManager.Default.CreateTypeface(manifestStream);
                //        }
                //    }
                //}
#endif
                if (stream.Header[PdfName.Subtype] == PdfName.Type1C)
                {

                }
                else
                {
                    var lenght1 = stream.Header[PdfName.Length1] as PdfInteger;
                    var lenght2 = stream.Header[PdfName.Length2] as PdfInteger;
                    var lenght3 = stream.Header[PdfName.Length3] as PdfInteger;
                    var bytes2 = buffer.GetByteArray(lenght1.IntValue, lenght2.IntValue + lenght3.IntValue);
                }
                typeface = ParseName(name, fontDescription);
            }
            return typeface;
        }

        #endregion
    }
}