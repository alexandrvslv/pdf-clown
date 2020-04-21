/*
  Copyright 2006-2015 Stefano Chizzolini. http://www.pdfclown.org

  Contributors:
    * Stefano Chizzolini (original code developer, http://www.stefanochizzolini.it)
    * Manuel Guilbault (code contributor [FIX:27], manuel.guilbault at gmail.com)

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

using System;


namespace PdfClown.Documents.Contents.Fonts
{
    /**
        <summary>Font descriptor flags [PDF:1.6:5.7.1].</summary>
      */
    [Flags]
    public enum FlagsEnum
    {
        /**
          <summary>All glyphs have the same width.</summary>
        */
        FixedPitch = 0x1,
        /**
          <summary>Glyphs have serifs.</summary>
        */
        Serif = 0x2,
        /**
          <summary>Font contains glyphs outside the Adobe standard Latin character set.</summary>
        */
        Symbolic = 0x4,
        /**
          <summary>Glyphs resemble cursive handwriting.</summary>
        */
        Script = 0x8,
        /**
          <summary>Font uses the Adobe standard Latin character set.</summary>
        */
        Nonsymbolic = 0x20,
        /**
          <summary>Glyphs have dominant vertical strokes that are slanted.</summary>
        */
        Italic = 0x40,
        /**
          <summary>Font contains no lowercase letters.</summary>
        */
        AllCap = 0x10000,
        /**
          <summary>Font contains both uppercase and lowercase letters.</summary>
        */
        SmallCap = 0x20000,
        /**
          <summary>Thicken bold glyphs at small text sizes.</summary>
        */
        ForceBold = 0x40000
    }
}