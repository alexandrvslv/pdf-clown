/*
  Copyright 2006-2015 Stefano Chizzolini. http://www.pdfclown.org

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
using PdfClown.Files;
using PdfClown.Objects;
using PdfClown.Tokens;

using System;
using System.Collections.Generic;
using System.Text;

namespace PdfClown.Documents.Contents.Objects
{
    /**
      <summary>Content stream instruction [PDF:1.6:3.7.1].</summary>
    */
    [PDF(VersionEnum.PDF10)]
    public abstract class Operation : ContentObject
    {
        #region static
        #region interface
        #region public
        /**
          <summary>Gets an operation.</summary>
          <param name="@operator">Operator.</param>
          <param name="operands">List of operands.</param>
        */
        public static Operation Get(string @operator, IList<PdfDirectObject> operands)
        {
            if (@operator == null)
                return null;

            if (@operator.Equals(SaveGraphicsState.OperatorKeyword, StringComparison.Ordinal))
                return SaveGraphicsState.Value;
            else if (@operator.Equals(SetFont.OperatorKeyword, StringComparison.Ordinal))
                return new SetFont(operands);
            else if (@operator.Equals(SetStrokeColor.OperatorKeyword, StringComparison.Ordinal)
              || @operator.Equals(SetStrokeColor.ExtendedOperatorKeyword, StringComparison.Ordinal))
                return new SetStrokeColor(@operator, operands);
            else if (@operator.Equals(SetStrokeColorSpace.OperatorKeyword, StringComparison.Ordinal))
                return new SetStrokeColorSpace(operands);
            else if (@operator.Equals(SetFillColor.OperatorKeyword, StringComparison.Ordinal)
              || @operator.Equals(SetFillColor.ExtendedOperatorKeyword, StringComparison.Ordinal))
                return new SetFillColor(@operator, operands);
            else if (@operator.Equals(SetFillColorSpace.OperatorKeyword, StringComparison.Ordinal))
                return new SetFillColorSpace(operands);
            else if (@operator.Equals(SetDeviceGrayStrokeColor.OperatorKeyword, StringComparison.Ordinal))
                return new SetDeviceGrayStrokeColor(operands);
            else if (@operator.Equals(SetDeviceGrayFillColor.OperatorKeyword, StringComparison.Ordinal))
                return new SetDeviceGrayFillColor(operands);
            else if (@operator.Equals(SetDeviceRGBStrokeColor.OperatorKeyword, StringComparison.Ordinal))
                return new SetDeviceRGBStrokeColor(operands);
            else if (@operator.Equals(SetDeviceRGBFillColor.OperatorKeyword, StringComparison.Ordinal))
                return new SetDeviceRGBFillColor(operands);
            else if (@operator.Equals(SetDeviceCMYKStrokeColor.OperatorKeyword, StringComparison.Ordinal))
                return new SetDeviceCMYKStrokeColor(operands);
            else if (@operator.Equals(SetDeviceCMYKFillColor.OperatorKeyword, StringComparison.Ordinal))
                return new SetDeviceCMYKFillColor(operands);
            else if (@operator.Equals(RestoreGraphicsState.OperatorKeyword, StringComparison.Ordinal))
                return RestoreGraphicsState.Value;
            else if (@operator.Equals(BeginSubpath.OperatorKeyword, StringComparison.Ordinal))
                return new BeginSubpath(operands);
            else if (@operator.Equals(CloseSubpath.OperatorKeyword, StringComparison.Ordinal))
                return CloseSubpath.Value;
            else if (@operator.Equals(PaintPath.CloseStrokeOperatorKeyword, StringComparison.Ordinal))
                return PaintPath.CloseStroke;
            else if (@operator.Equals(PaintPath.FillOperatorKeyword, StringComparison.Ordinal)
              || @operator.Equals(PaintPath.FillObsoleteOperatorKeyword, StringComparison.Ordinal))
                return PaintPath.Fill;
            else if (@operator.Equals(PaintPath.FillEvenOddOperatorKeyword, StringComparison.Ordinal))
                return PaintPath.FillEvenOdd;
            else if (@operator.Equals(PaintPath.StrokeOperatorKeyword, StringComparison.Ordinal))
                return PaintPath.Stroke;
            else if (@operator.Equals(PaintPath.FillStrokeOperatorKeyword, StringComparison.Ordinal))
                return PaintPath.FillStroke;
            else if (@operator.Equals(PaintPath.FillStrokeEvenOddOperatorKeyword, StringComparison.Ordinal))
                return PaintPath.FillStrokeEvenOdd;
            else if (@operator.Equals(PaintPath.CloseFillStrokeOperatorKeyword, StringComparison.Ordinal))
                return PaintPath.CloseFillStroke;
            else if (@operator.Equals(PaintPath.CloseFillStrokeEvenOddOperatorKeyword, StringComparison.Ordinal))
                return PaintPath.CloseFillStrokeEvenOdd;
            else if (@operator.Equals(PaintPath.EndPathNoOpOperatorKeyword, StringComparison.Ordinal))
                return PaintPath.EndPathNoOp;
            else if (@operator.Equals(ModifyClipPath.NonZeroOperatorKeyword, StringComparison.Ordinal))
                return ModifyClipPath.NonZero;
            else if (@operator.Equals(ModifyClipPath.EvenOddOperatorKeyword, StringComparison.Ordinal))
                return ModifyClipPath.EvenOdd;
            else if (@operator.Equals(TranslateTextToNextLine.OperatorKeyword, StringComparison.Ordinal))
                return TranslateTextToNextLine.Value;
            else if (@operator.Equals(ShowSimpleText.OperatorKeyword, StringComparison.Ordinal))
                return new ShowSimpleText(operands);
            else if (@operator.Equals(ShowTextToNextLine.SimpleOperatorKeyword, StringComparison.Ordinal)
              || @operator.Equals(ShowTextToNextLine.SpaceOperatorKeyword, StringComparison.Ordinal))
                return new ShowTextToNextLine(@operator, operands);
            else if (@operator.Equals(ShowAdjustedText.OperatorKeyword, StringComparison.Ordinal))
                return new ShowAdjustedText(operands);
            else if (@operator.Equals(TranslateTextRelative.SimpleOperatorKeyword, StringComparison.Ordinal)
              || @operator.Equals(TranslateTextRelative.LeadOperatorKeyword, StringComparison.Ordinal))
                return new TranslateTextRelative(@operator, operands);
            else if (@operator.Equals(SetTextMatrix.OperatorKeyword, StringComparison.Ordinal))
                return new SetTextMatrix(operands);
            else if (@operator.Equals(ModifyCTM.OperatorKeyword, StringComparison.Ordinal))
                return new ModifyCTM(operands);
            else if (@operator.Equals(PaintXObject.OperatorKeyword, StringComparison.Ordinal))
                return new PaintXObject(operands);
            else if (@operator.Equals(PaintShading.OperatorKeyword, StringComparison.Ordinal))
                return new PaintShading(operands);
            else if (@operator.Equals(SetCharSpace.OperatorKeyword, StringComparison.Ordinal))
                return new SetCharSpace(operands);
            else if (@operator.Equals(SetLineCap.OperatorKeyword, StringComparison.Ordinal))
                return new SetLineCap(operands);
            else if (@operator.Equals(SetLineDash.OperatorKeyword, StringComparison.Ordinal))
                return new SetLineDash(operands);
            else if (@operator.Equals(SetLineJoin.OperatorKeyword, StringComparison.Ordinal))
                return new SetLineJoin(operands);
            else if (@operator.Equals(SetLineWidth.OperatorKeyword, StringComparison.Ordinal))
                return new SetLineWidth(operands);
            else if (@operator.Equals(SetMiterLimit.OperatorKeyword, StringComparison.Ordinal))
                return new SetMiterLimit(operands);
            else if (@operator.Equals(SetTextLead.OperatorKeyword, StringComparison.Ordinal))
                return new SetTextLead(operands);
            else if (@operator.Equals(SetTextRise.OperatorKeyword, StringComparison.Ordinal))
                return new SetTextRise(operands);
            else if (@operator.Equals(SetTextScale.OperatorKeyword, StringComparison.Ordinal))
                return new SetTextScale(operands);
            else if (@operator.Equals(SetTextRenderMode.OperatorKeyword, StringComparison.Ordinal))
                return new SetTextRenderMode(operands);
            else if (@operator.Equals(SetWordSpace.OperatorKeyword, StringComparison.Ordinal))
                return new SetWordSpace(operands);
            else if (@operator.Equals(DrawLine.OperatorKeyword, StringComparison.Ordinal))
                return new DrawLine(operands);
            else if (@operator.Equals(DrawRectangle.OperatorKeyword, StringComparison.Ordinal))
                return new DrawRectangle(operands);
            else if (@operator.Equals(DrawCurve.FinalOperatorKeyword, StringComparison.Ordinal)
              || @operator.Equals(DrawCurve.FullOperatorKeyword, StringComparison.Ordinal)
              || @operator.Equals(DrawCurve.InitialOperatorKeyword, StringComparison.Ordinal))
                return new DrawCurve(@operator, operands);
            else if (@operator.Equals(EndInlineImage.OperatorKeyword, StringComparison.Ordinal))
                return EndInlineImage.Value;
            else if (@operator.Equals(BeginText.OperatorKeyword, StringComparison.Ordinal))
                return BeginText.Value;
            else if (@operator.Equals(EndText.OperatorKeyword, StringComparison.Ordinal))
                return EndText.Value;
            else if (@operator.Equals(BeginMarkedContent.SimpleOperatorKeyword, StringComparison.Ordinal)
              || @operator.Equals(BeginMarkedContent.PropertyListOperatorKeyword, StringComparison.Ordinal))
                return new BeginMarkedContent(@operator, operands);
            else if (@operator.Equals(EndMarkedContent.OperatorKeyword, StringComparison.Ordinal))
                return EndMarkedContent.Value;
            else if (@operator.Equals(MarkedContentPoint.SimpleOperatorKeyword, StringComparison.Ordinal)
              || @operator.Equals(MarkedContentPoint.PropertyListOperatorKeyword, StringComparison.Ordinal))
                return new MarkedContentPoint(@operator, operands);
            else if (@operator.Equals(BeginInlineImage.OperatorKeyword, StringComparison.Ordinal))
                return BeginInlineImage.Value;
            else if (@operator.Equals(EndInlineImage.OperatorKeyword, StringComparison.Ordinal))
                return EndInlineImage.Value;
            else if (@operator.Equals(ApplyExtGState.OperatorKeyword, StringComparison.Ordinal))
                return new ApplyExtGState(operands);
            else if (@operator.Equals(CharProcWidth.OperatorKeyword, StringComparison.Ordinal))
                return new CharProcWidth(operands);
            else if (@operator.Equals(CharProcBBox.OperatorKeyword, StringComparison.Ordinal))
                return new CharProcBBox(operands);
            else // No explicit operation implementation available.
                return new GenericOperation(@operator, operands);
        }
        #endregion
        #endregion
        #endregion

        #region dynamic
        #region fields
        protected string @operator;
        protected IList<PdfDirectObject> operands;
        #endregion

        #region constructors
        protected Operation(string @operator)
        { this.@operator = @operator; }

        protected Operation(string @operator, PdfDirectObject operand)
        {
            this.@operator = @operator;

            this.operands = new List<PdfDirectObject>();
            this.operands.Add(operand);
        }

        protected Operation(string @operator, params PdfDirectObject[] operands)
        {
            this.@operator = @operator;
            this.operands = new List<PdfDirectObject>(operands);
        }

        protected Operation(string @operator, IList<PdfDirectObject> operands)
        {
            this.@operator = @operator;
            this.operands = operands;
        }
        #endregion

        #region interface
        #region public
        public string Operator => @operator;

        public IList<PdfDirectObject> Operands => operands;

        public override string ToString()
        {
            StringBuilder buffer = new StringBuilder();

            // Begin.
            buffer.Append("{");

            // Operator.
            buffer.Append(@operator);

            // Operands.
            if (operands != null)
            {
                buffer.Append(" [");
                for (int i = 0, count = operands.Count; i < count; i++)
                {
                    if (i > 0)
                    { buffer.Append(", "); }

                    buffer.Append(operands[i].ToString());
                }
                buffer.Append("]");
            }

            // End.
            buffer.Append("}");

            return buffer.ToString();
        }

        public override void WriteTo(IOutputStream stream, Document context)
        {
            if (operands != null)
            {
                File fileContext = context.File;
                foreach (PdfDirectObject operand in operands)
                { operand.WriteTo(stream, fileContext); stream.Write(Chunk.Space); }
            }
            stream.Write(@operator); stream.Write(Chunk.LineFeed);
        }
        #endregion
        #endregion
        #endregion
    }
}