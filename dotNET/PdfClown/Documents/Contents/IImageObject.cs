using PdfClown.Documents.Contents.ColorSpaces;
using PdfClown.Documents.Contents.Scanner;
using PdfClown.Objects;
using SkiaSharp;

namespace PdfClown.Documents.Contents
{
    public interface IImageObject
    {
        int BitsPerComponent { get; }
        ColorSpace ColorSpace { get; }
        PdfDirectObject Filter { get; }
        PdfDirectObject Parameters { get; }
        Bytes.IBuffer Data { get; }
        SKSize Size { get; }
        IImageObject SMask { get; }
        bool ImageMask { get; }
        PdfArray Matte { get; }
        PdfDictionary Header { get; }
        PdfArray Decode { get; }
        SKBitmap LoadImage(GraphicsState state);
    }
}