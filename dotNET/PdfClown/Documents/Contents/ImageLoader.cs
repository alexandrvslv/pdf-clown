
using FreeImageAPI;
using PdfClown.Bytes;
using PdfClown.Bytes.Filters;
using PdfClown.Documents.Contents.ColorSpaces;
using PdfClown.Documents.Contents.Scanner;
using PdfClown.Objects;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace PdfClown.Documents.Contents
{
    public class ImageLoader
    {
        public static SKBitmap Load(IImageObject imageObject, GraphicsState state)
        {
            var data = imageObject.Data;
            var image = (SKBitmap)null;
            var filter = imageObject.Filter;
            if (filter is PdfArray filterArray)
            {
                var parameterArray = imageObject.Parameters as PdfArray;
                for (int i = 0; i < filterArray.Count; i++)
                {
                    var filterItem = (PdfName)filterArray[i];
                    var parameterItem = parameterArray?[i];
                    var temp = LoadImage(imageObject, data, filterItem, parameterItem, imageObject.Header);
                    if (temp is IBuffer tempBuffer)
                    {
                        data = tempBuffer;
                    }
                    else if (temp is SKBitmap tempImage)
                    {
                        image = tempImage;
                        break;
                    }
                }
            }
            else if (filter != null)
            {
                var filterItem = (PdfName)filter;
                var temp = LoadImage(imageObject, data, filterItem, imageObject.Parameters, imageObject.Header);
                if (temp is IBuffer tempBuffer)
                {
                    data = tempBuffer;
                }
                else if (temp is SKBitmap tempImage)
                {
                    image = tempImage;
                }
            }

            if (image == null)
            {
                var imageLoader = new ImageLoader(imageObject, data.GetBuffer(), state);
                image = imageLoader.Load();
            }
            return image;
        }

        public static object LoadImage(IImageObject imageObject, IBuffer data, PdfName filterItem, PdfDirectObject parameterItem, PdfDictionary header)
        {
            if (filterItem.Equals(PdfName.DCTDecode)
                || filterItem.Equals(PdfName.DCT))
            {
                return SKBitmap.Decode(data.GetBuffer());
            }
            else if (filterItem.Equals(PdfName.JPXDecode))
            {
                return LoadJPEG2000(data, parameterItem, imageObject.Header);
            }
            else if (filterItem.Equals(PdfName.JBIG2Decode))
            {
                return LoadJBIG(data, parameterItem, imageObject.Header);
            }
            else if (filterItem.Equals(PdfName.CCITTFaxDecode)
                || filterItem.Equals(PdfName.CCF))
            {
                return Bytes.Buffer.Extract(data, filterItem, parameterItem, imageObject.Header);
            }
            else if (filterItem != null)
            {
                return Bytes.Buffer.Extract(data, filterItem, parameterItem, imageObject.Header);
            }

            return data;
        }

        private static SKBitmap LoadJBIG(IBuffer data, PdfDirectObject parameters, PdfDictionary header)
        {
            var imageParams = header;
            var width = imageParams.Resolve(PdfName.Width) as PdfInteger;
            var height = imageParams.Resolve(PdfName.Height) as PdfInteger;
            var bpp = imageParams.Resolve(PdfName.BitsPerComponent) as PdfInteger;
            var flag = imageParams.Resolve(PdfName.ImageMask) as PdfBoolean;

            using (var output = new MemoryStream())
            using (var input = new MemoryStream())
            {
                //
                input.Write(new byte[] { 0X97, 0x4A, 0x42, 0x32, 0x0D, 0x0A, 0x1A, 0x0A, 0x01, 0x00, 0x00, 0x00, 0x79 }, 0, 13);
                if (parameters is PdfDictionary dict)
                {
                    var jbigGlobal = dict.Resolve(PdfName.JBIG2Globals) as PdfStream;
                    if (jbigGlobal != null)
                    {
                        var body = jbigGlobal.GetBody(false);
                        var bodyBuffer = body.GetBuffer();
                        input.Write(bodyBuffer, 0, bodyBuffer.Length);
                    }
                }
                input.Write(data.GetBuffer(), 0, (int)data.Length);
                input.Write(new byte[] { 0X00, 0x00, 0x00, 0x03, 0x31, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00 }, 0, 11);
                input.Write(new byte[] { 0X00, 0x00, 0x00, 0x04, 0x33, 0x01, 0x00, 0x00, 0x00, 0x00 }, 0, 10);
                input.Flush();
                input.Position = 0;
                FREE_IMAGE_FORMAT format = FreeImage.GetFileTypeFromStream(input);
                var bmp = FreeImage.LoadFromStream(input);

                if (bmp.IsNull)
                {
                    return null;
                }
                FreeImage.SaveToStream(bmp, output, FREE_IMAGE_FORMAT.FIF_JPEG, FREE_IMAGE_SAVE_FLAGS.JPEG_OPTIMIZE);
                FreeImage.Unload(bmp);
                output.Flush();
                output.Position = 0;
                return SKBitmap.Decode(output);
            }
        }

        public static SKBitmap LoadJPEG2000(IBuffer jpegStream, PdfDirectObject parameters, PdfDictionary header)
        {
            var imageParams = header;
            var width = imageParams.Resolve(PdfName.Width) as PdfInteger;
            var height = imageParams.Resolve(PdfName.Height) as PdfInteger;
            var bpp = imageParams.Resolve(PdfName.BitsPerComponent) as PdfInteger;
            var flag = imageParams.Resolve(PdfName.ImageMask) as PdfBoolean;

            using (var output = new MemoryStream())
            using (var input = new MemoryStream(jpegStream.GetBuffer()))
            {
                var bmp = FreeImage.LoadFromStream(input);
                if (bmp.IsNull)
                {
                    return null;
                }
                FreeImage.SaveToStream(bmp, output, FREE_IMAGE_FORMAT.FIF_JPEG, FREE_IMAGE_SAVE_FLAGS.JPEG_OPTIMIZE);
                FreeImage.Unload(bmp);
                output.Flush();
                output.Position = 0;
                return SKBitmap.Decode(output);
            }
        }

        private GraphicsState state;
        private IImageObject image;
        private ColorSpace colorSpace;
        private ICCBasedColorSpace iccColorSpace;
        private int bitPerComponent;
        private PdfArray matte;
        private SKSize size;
        private bool indexed;
        private int componentsCount;
        private double maximum;
        private double min;
        private double max;
        private double interpolateConst;
        private byte[] buffer;
        private bool imageMask;
        private PdfArray decode;
        private IImageObject sMask;
        private ImageLoader sMaskLoader;

        public ImageLoader(IImageObject image, GraphicsState state)
        {
            var buffer = image.Data;
            var data = buffer.ToByteArray();
            var filter = image.Filter;
            if (filter != null)
            {
                buffer = Bytes.Buffer.Extract(buffer, filter, image.Parameters, image.Header);
                data = buffer.ToByteArray();
            }
            Init(image, data, state);
        }

        public ImageLoader(IImageObject image, byte[] buffer, GraphicsState state)
        {
            Init(image, buffer, state);
        }

        private void Init(IImageObject image, byte[] buffer, GraphicsState state)
        {
            this.state = state;
            this.image = image;
            this.buffer = buffer;
            imageMask = image.ImageMask;
            decode = image.Decode;
            matte = image.Matte;
            size = image.Size;
            bitPerComponent = image.BitsPerComponent;

            maximum = Math.Pow(2, bitPerComponent) - 1;
            min = 0D;
            max = indexed ? maximum : 1D;
            interpolateConst = (max - min) / maximum;
            sMask = image.SMask;
            if (sMask != null)
            {
                sMaskLoader = new ImageLoader(sMask, state);
            }
            if (imageMask)
            {
                return;
            }
            colorSpace = image.ColorSpace;
            componentsCount = colorSpace.ComponentCount;
            iccColorSpace = colorSpace as ICCBasedColorSpace;
            if (colorSpace is IndexedColorSpace indexedColorSpace)
            {
                indexed = true;
                iccColorSpace = indexedColorSpace.BaseSpace as ICCBasedColorSpace;
            }
        }

        public Color GetColor(int x, int y)
        {
            return GetColor((y * (int)size.Width + x));
        }

        public Color GetColor(int index)
        {
            var componentIndex = index * componentsCount;
            var components = new PdfDirectObject[componentsCount];
            for (int i = 0; i < componentsCount; i++)
            {
                var value = componentIndex < buffer.Length ? buffer[componentIndex] : 0;
                var interpolate = indexed ? value : min + (value * (interpolateConst));
                components[i] = indexed
                    ? (PdfDirectObject)new PdfInteger((int)interpolate)
                    : (PdfDirectObject)new PdfReal(interpolate);
                componentIndex++;
            }
            return colorSpace.GetColor(components, null);
        }

        public void GetColor(int index, ref double[] components)
        {
            var componentIndex = index * componentsCount;
            if (bitPerComponent == 1)
            {
                for (int i = 0; i < componentsCount; i++)
                {
                    var byteIndex = componentIndex / 8;
                    //if (emptyBytes)
                    //    byteIndex += y;
                    var bitIndex = 7 - index % 8;
                    var byteValue = buffer[byteIndex];
                    var value = (byteValue & (1 << bitIndex)) == 0 ? (byte)0 : (byte)1;
                    var interpolate = indexed ? value : min + (value * (interpolateConst));
                    components[i] = interpolate;
                    componentIndex++;
                }
            }
            else
            {
                for (int i = 0; i < componentsCount; i++)
                {
                    var value = componentIndex < buffer.Length ? buffer[componentIndex] : 0;
                    var interpolate = indexed ? value : min + (value * (interpolateConst));
                    components[i] = interpolate;
                    componentIndex++;
                }
            }
        }

        public SKBitmap Load()
        {
            if (imageMask)
            {
                return LoadImageMask();
            }
            var info = new SKImageInfo((int)size.Width, (int)size.Height)
            {
                AlphaType = SKAlphaType.Premul,
            };
            if (iccColorSpace != null)
            {
                //info.ColorSpace = iccColorSpace.GetSKColorSpace();
            }

            // create the buffer that will hold the pixels
            var raster = new int[info.Width * info.Height];//var bitmap = new SKBitmap();
            var components = new double[componentsCount];//TODO stackalloc
            var maskComponents = new double[componentsCount];//TODO stackalloc
            for (int y = 0; y < info.Height; y++)
            {
                var row = y * info.Width;
                for (int x = 0; x < info.Width; x++)
                {
                    var index = row + x;
                    //var color = GetColor(index);
                    GetColor(index, ref components);
                    var skColor = colorSpace.GetSKColor(components, null);
                    if (sMaskLoader != null)
                    {
                        sMaskLoader.GetColor(index, ref maskComponents);
                        //alfa
                        skColor = skColor.WithAlpha((byte)(maskComponents[0] * 255));
                        //shaping
                        //for (int i = 0; i < color.Components.Count; i++)
                        //{
                        //    var m = sMaskLoader.matte == null ? 0D : ((IPdfNumber)sMaskLoader.matte[i]).DoubleValue;
                        //    var a = ((IPdfNumber)sMaskColor.Components[sMaskColor.Components.Count == color.Components.Count ? i : 0]).DoubleValue;
                        //    var c = ((IPdfNumber)color.Components[i]).DoubleValue;
                        //    color.Components[i] = new PdfReal(m + a * (c - m));
                        //}
                    }
                    raster[index] = (int)(uint)skColor;//bitmap.SetPixel(x, y, skColor);
                }
            }

            // get a pointer to the buffer, and give it to the bitmap
            var ptr = GCHandle.Alloc(raster, GCHandleType.Pinned);
            var bitmap = new SKBitmap();
            bitmap.InstallPixels(info, ptr.AddrOfPinnedObject(), info.RowBytes, (addr, ctx) => ptr.Free(), null);

            return bitmap;
        }

        private SKBitmap LoadImageMask()
        {
            var info = new SKImageInfo((int)size.Width, (int)size.Height)
            {
                AlphaType = SKAlphaType.Premul,
            };
            var skColor = state.FillColorSpace.GetSKColor(state.FillColor);
            var raster = new int[info.Width * info.Height];
            var emptyBytes = info.Width % 8 > 0;
            for (int y = 0; y < info.Height; y++)
            {
                for (int x = 0; x < info.Width; x++)
                {
                    var index = (y * info.Width + x);
                    byte value = 0;
                    if (bitPerComponent == 1)
                    {
                        var byteIndex = index / 8;
                        //if (emptyBytes)
                        //    byteIndex += y;
                        var bitIndex = 7 - index % 8;
                        var byteValue = buffer[byteIndex];
                        value = (byteValue & (1 << bitIndex)) == 0 ? (byte)0 : (byte)255;
                    }
                    else
                    {
                        value = index < buffer.Length ? buffer[index] : (byte)0;
                    }
                    raster[index] = (int)(uint)skColor.WithAlpha(value);
                }
            }

            // get a pointer to the buffer, and give it to the bitmap
            var ptr = GCHandle.Alloc(raster, GCHandleType.Pinned);
            var bitmap = new SKBitmap();
            bitmap.InstallPixels(info, ptr.AddrOfPinnedObject(), info.RowBytes, (addr, ctx) => ptr.Free(), null);

            return bitmap;
        }

        public static bool IsImage(byte[] buf)
        {
            if (buf != null)
            {
                if (IsBitmap(buf)
                    || IsGif(buf)
                    || IsTiff(buf)
                    || IsPng(buf)
                    || IsJPEG(buf))
                    return true;
            }
            return false;
        }

        private static bool IsBitmap(byte[] buf)
        {
            return Encoding.ASCII.GetString(buf, 0, 2) == "BM";
        }

        private static bool IsPng(byte[] buf)
        {
            return (buf[0] == 137 && buf[1] == 80 && buf[2] == 78 && buf[3] == (byte)71); //png
        }

        private static bool IsJPEG(byte[] buf)
        {
            return (buf[0] == 255 && buf[1] == 216 && buf[2] == 255 && buf[3] == 224) //jpeg
                    || (buf[0] == 255 && buf[1] == 216 && buf[2] == 255 && buf[3] == 225); //jpeg canon
        }

        private static bool IsGif(byte[] buf)
        {
            return Encoding.ASCII.GetString(buf, 0, 3) == "GIF";
        }

        private static bool IsTiff(byte[] buf)
        {
            return (buf[0] == 73 && buf[1] == 73 && buf[2] == 42) // TIFF
                || (buf[0] == 77 && buf[1] == 77 && buf[2] == 42); // TIFF2;
        }
    }
}