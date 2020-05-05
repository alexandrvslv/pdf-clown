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
using PdfClown.Documents.Contents.Fonts;
using PdfClown.Objects;

using System;
using System.Collections.Generic;
using SkiaSharp;
using PdfClown.Documents.Contents.Scanner;
using System.Text;
using PdfClown.Util.Math.Geom;
using PdfClown.Tokens;
using System.Linq;

namespace PdfClown.Documents.Contents.Objects
{
    /**
      <summary>Abstract 'show a text string' operation [PDF:1.6:5.3.2].</summary>
    */
    [PDF(VersionEnum.PDF10)]
    public abstract class ShowText : Operation
    {
        #region types
        public interface IScanner
        {
            /**
              <summary>Notifies the scanner about a text character.</summary>
              <param name="textChar">Scanned character.</param>
              <param name="textCharBox">Bounding box of the scanned character.</param>
            */
            void ScanChar(char textChar, Quad textCharBox);
        }
        public TextStringWrapper textString;
        #endregion

        #region dynamic
        #region constructors
        protected ShowText(string @operator) : base(@operator)
        { }

        protected ShowText(string @operator, params PdfDirectObject[] operands) : base(@operator, operands)
        { }

        protected ShowText(string @operator, IList<PdfDirectObject> operands) : base(@operator, operands) { }
        #endregion

        #region interface
        #region public

        /**
         <summary>Gets/Sets the encoded text.</summary>
         <remarks>Text is expressed in native encoding: to resolve it to Unicode, pass it
         to the decode method of the corresponding font.</remarks>
       */
        public abstract byte[] Text
        {
            get;
            set;
        }

        /**
          <summary>Gets/Sets the encoded text elements along with their adjustments.</summary>
          <remarks>Text is expressed in native encoding: to resolve it to Unicode, pass it
          to the decode method of the corresponding font.</remarks>
          <returns>Each element can be either a byte array or a number:
            <list type="bullet">
              <item>if it's a byte array (encoded text), the operator shows text glyphs;</item>
              <item>if it's a number (glyph adjustment), the operator inversely adjusts the next glyph position
              by that amount (that is: a positive value reduces the distance between consecutive glyphs).</item>
            </list>
          </returns>
        */
        public virtual IEnumerable<object> Value
        {
            get => Enumerable.Repeat(Text, 1);
            set => Text = (byte[])value.FirstOrDefault();
        }

        public override void Scan(GraphicsState state)
        {
            if (textString == null)
                textString = new TextStringWrapper(state.Scanner, false);
            textString.TextChars.Clear();
            Scan(state, new TextStringWrapper.ShowTextScanner(textString));
        }

        /**
          <summary>Executes scanning on this operation.</summary>
          <param name="state">Graphics state context.</param>
          <param name="textScanner">Scanner to be notified about text contents.
          In case it's null, the operation is applied to the graphics state context.</param>
        */
        public virtual void Scan(GraphicsState state, IScanner textScanner)
        {
            /*
              TODO: I really dislike this solution -- it's a temporary hack until the event-driven
              parsing mechanism is implemented...
            */

            Font font = state.Font ?? Font.LatestFont;
            if (font == null)
                return;

            bool wordSpaceSupported = !(font is PdfType0Font);
            bool vertical = font.IsVertical;

            double fontSize = state.FontSize;
            double horizontalScaling = state.Scale;
            double wordSpace = wordSpaceSupported ? state.WordSpace : 0;
            double charSpace = state.CharSpace;
            var charBBox = font.BoundingBox;
            SKMatrix fm = font.FontMatrix;
            SKMatrix ctm = state.Ctm;
            SKMatrix tm = state.TextState.Tm;

            //var encoding = font.GetEnoding();
            var context = state.Scanner.RenderContext;

            // put the text state parameters into matrix form
            var parameters = new SKMatrix(
                (float)(fontSize * horizontalScaling), 0f, 0f,
                0f, (float)fontSize, (float)state.Rise,
                0f, 0f, 1f);

            if (context != null)
            {
                context.Save();
            }

            var fill = context != null && state.RenderModeFill ? state.CreateFillPaint() : null;
            var stroke = context != null && state.RenderModeStroke ? state.CreateStrokePaint() : null;

            if (this is ShowTextToNextLine showTextToNextLine)
            {
                double? newWordSpace = showTextToNextLine.WordSpace;
                if (newWordSpace != null)
                {
                    state.WordSpace = newWordSpace.Value;
                    if (wordSpaceSupported)
                    { wordSpace = newWordSpace.Value; }
                }
                double? newCharSpace = showTextToNextLine.CharSpace;
                if (newCharSpace != null)
                {
                    state.CharSpace = newCharSpace.Value;
                    charSpace = newCharSpace.Value;
                }
                tm = state.TextState.Tlm;
                SKMatrix.PreConcat(ref tm, new SKMatrix { Values = new float[] { 1, 0, 0, 0, 1, (float)-state.Lead, 0, 0, 1 } });
            }
            else
            { tm = state.TextState.Tm; }

            foreach (object textElement in Value)
            {
                if (textElement is byte[] byteElement) // Text string.
                {
                    using (var buffer = new Bytes.Buffer(byteElement))
                    {
                        while (buffer.Position < buffer.Length)
                        {
                            var code = font.ReadCode(buffer, out var codeBytes);
                            var textCode = font.ToUnicode(code);
                            if (textCode < 0)
                            {
                                // Missing character.
                                textCode = '?';// font.MissingCharacter(byteElement, code);
                            }
                            var textChar = (char)textCode;
                            // Word spacing shall be applied to every occurrence of the single-byte character code
                            // 32 in a string when using a simple font or a composite font that defines code 32 as
                            // a single-byte code.
                            double wordSpacing = 0;
                            if (codeBytes.Length == 1 && code == 32)
                            {
                                wordSpacing = wordSpace;
                            }
                            //NOTE: The text rendering matrix is recomputed before each glyph is painted
                            // during a text-showing operation.
                            SKMatrix trm = parameters;// ctm;
                            SKMatrix.PostConcat(ref trm, tm);
                            SKMatrix.PostConcat(ref trm, ctm);

                            // get glyph's position vector if this is vertical text
                            // changes to vertical text should be tested with PDFBOX-2294 and PDFBOX-1422
                            if (vertical)
                            {
                                // position vector, in text space
                                var v = font.GetPositionVector(code);
                                // apply the position vector to the horizontal origin to get the vertical origin
                                SKMatrix.PreConcat(ref trm, SKMatrix.MakeTranslation(v.X, v.Y));
                            }
                            SKMatrix.PreConcat(ref trm, fm);

                            if (context != null && !(textChar == ' '))
                            {
                                context.SetMatrix(trm);
                                font.DrawChar(context, fill, stroke, textChar, code, codeBytes);
                            }

                            var w = font.GetDisplacement(code);

                            if (textScanner != null)
                            {
                                var width = font.GetWidth(code);
                                var charBox = vertical
                                    ? SKRect.Create(charBBox.Right, 0, width, charBBox.Height)
                                    : SKRect.Create(0, -charBBox.Bottom, width, charBBox.Height);

                                var quad = new Quad(charBox);
                                SKMatrix.PreConcat(ref trm, SKMatrix.MakeScale(1, -1));
                                quad.Transform(ref trm);
                                textScanner.ScanChar(textChar, quad);
                            }
                            /*
                              NOTE: After the glyph is painted, the text matrix is updated
                              according to the glyph displacement and any applicable spacing parameter.
                            */
                            // calculate the combined displacements
                            float tx;
                            float ty;
                            if (vertical)
                            {
                                tx = 0;
                                ty = (float)(w.Y * fontSize + charSpace + wordSpacing);
                            }
                            else
                            {
                                tx = (float)((w.X * fontSize + charSpace + wordSpacing) * horizontalScaling);
                                ty = 0;
                            }
                            SKMatrix.PreConcat(ref tm, SKMatrix.MakeTranslation(tx, ty));
                        }
                    }
                }
                else // Text position adjustment.
                {
                    // calculate the combined displacements
                    var tj = -Convert.ToSingle(textElement);
                    float tx;
                    float ty;
                    if (vertical)
                    {
                        tx = 0;
                        ty = (float)(tj / 1000 * fontSize);
                    }
                    else
                    {
                        tx = (float)(tj / 1000 * fontSize * horizontalScaling);
                        ty = 0;
                    }

                    SKMatrix.PreConcat(ref tm, SKMatrix.MakeTranslation(tx, ty));
                }
            }
            if (context != null)
            {
                context.Restore();
            }
            state.TextState.Tm = tm;
            if (this is ShowTextToNextLine)
            { state.TextState.Tlm = tm; }
        }


        #endregion
        #endregion
        #endregion
    }
}