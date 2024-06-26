using System;
using System.Linq;
using text = System.Text;

namespace PdfClown.Tokens
{
    internal static class Charset
    {
        static Charset()
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        }
        public static readonly text::Encoding ISO88591 = text::Encoding.GetEncoding("ISO-8859-1");
        public static readonly text::Encoding UTF16BE = text::Encoding.BigEndianUnicode;
        public static readonly text::Encoding UTF16LE = text::Encoding.Unicode;
        public static readonly text::Encoding ASCII = text::Encoding.ASCII;
        public static readonly text::Encoding UTF8 = text::Encoding.UTF8;

        public static text::Encoding GetEnconding(string name)
        {
            return text::Encoding.GetEncoding(name);
        }
    }
}

