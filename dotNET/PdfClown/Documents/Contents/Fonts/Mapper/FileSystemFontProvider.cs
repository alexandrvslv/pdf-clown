/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using PdfClown.Documents.Contents.Fonts.Autodetect;
using PdfClown.Documents.Contents.Fonts.CCF;
using PdfClown.Documents.Contents.Fonts.TTF;
using PdfClown.Documents.Contents.Fonts.Type1;
using System.Diagnostics;
using System.Security;
using PdfClown.Tokens;
using PdfClown.Util.Collections.Generic;
using System.Linq;

namespace PdfClown.Documents.Contents.Fonts
{

    /**
     * A FontProvider which searches for fonts on the local filesystem.
     *
     * @author John Hewson
     */
    public sealed class FileSystemFontProvider : FontProvider
    {
        private readonly List<FSFontInfo> fontInfoList = new List<FSFontInfo>();
        private readonly FontCache cache;

        private class FSFontInfo : FontInfo
        {
            private readonly string postScriptName;
            private readonly FontFormat format;
            private readonly CIDSystemInfo cidSystemInfo;
            private readonly int usWeightClass;
            private readonly int sFamilyClass;
            private readonly int ulCodePageRange1;
            private readonly int ulCodePageRange2;
            private readonly int macStyle;
            private readonly PanoseClassification panose;
            internal readonly FileInfo file;
            private readonly FileSystemFontProvider parent;

            public FSFontInfo(FileInfo file, FontFormat format, string postScriptName,
                               CIDSystemInfo cidSystemInfo, int usWeightClass, int sFamilyClass,
                               int ulCodePageRange1, int ulCodePageRange2, int macStyle, byte[] panose,
                               FileSystemFontProvider parent)
            {
                this.file = file;
                this.format = format;
                this.postScriptName = postScriptName;
                this.cidSystemInfo = cidSystemInfo;
                this.usWeightClass = usWeightClass;
                this.sFamilyClass = sFamilyClass;
                this.ulCodePageRange1 = ulCodePageRange1;
                this.ulCodePageRange2 = ulCodePageRange2;
                this.macStyle = macStyle;
                this.panose = panose != null ? new PanoseClassification(panose) : null;
                this.parent = parent;
            }

            public override string PostScriptName
            {
                get => postScriptName;
            }

            public override FontFormat Format
            {
                get => format;
            }

            public override CIDSystemInfo CIDSystemInfo
            {
                get => cidSystemInfo;
            }

            /**
			 * {@inheritDoc}
			 * <p>
			 * The method returns null if there is there was an error opening the font.
			 * 
			 */
            public override BaseFont Font
            {
                get
                {
                    BaseFont cached = parent.cache.GetFont(this);
                    if (cached != null)
                    {
                        return cached;
                    }
                    else
                    {
                        BaseFont font;
                        switch (format)
                        {
                            case FontFormat.PFB: font = GetType1Font(postScriptName, file); break;
                            case FontFormat.TTF: font = GetTrueTypeFont(postScriptName, file); break;
                            case FontFormat.OTF: font = GetOTFFont(postScriptName, file); break;
                            default: throw new Exception("can't happen");
                        }
                        if (font != null)
                        {
                            parent.cache.AddFont(this, font);
                        }
                        return font;
                    }
                }
            }


            public override int FamilyClass
            {
                get => sFamilyClass;
            }


            public override int WeightClass
            {
                get => usWeightClass;
            }


            public override int CodePageRange1
            {
                get => ulCodePageRange1;
            }


            public override int CodePageRange2
            {
                get => ulCodePageRange2;
            }


            public override int MacStyle
            {
                get => macStyle;
            }


            public override PanoseClassification Panose
            {
                get => panose;
            }


            public override string ToString()
            {
                return base.ToString() + " " + file;
            }

            private TrueTypeFont GetTrueTypeFont(string postScriptName, FileInfo file)
            {
                try
                {

                    TrueTypeFont ttf = ReadTrueTypeFont(postScriptName, file);
#if DEBUG
                    Debug.WriteLine($"debug: Loaded {postScriptName} from {file}");
#endif
                    return ttf;
                }
                catch (IOException e)
                {
                    Debug.WriteLine($"error: Could not load font file: {file} {e}");
                }
                return null;
            }

            private TrueTypeFont ReadTrueTypeFont(string postScriptName, FileInfo file)
            {
                if (file.Name.ToLowerInvariant().EndsWith(".ttc"))
                {
                    //@SuppressWarnings("squid:S2095")
                    // ttc not closed here because it is needed later when ttf is accessed,
                    // e.g. rendering PDF with non-embedded font which is in ttc file in our font directory
                    TrueTypeCollection ttc = new TrueTypeCollection(file);
                    TrueTypeFont ttf = ttc.GetFontByName(postScriptName);
                    if (ttf == null)
                    {
                        ttc.Dispose();
                        throw new IOException("Font " + postScriptName + " not found in " + file);
                    }
                    return ttf;
                }
                else
                {
                    TTFParser ttfParser = new TTFParser(false, true);
                    return ttfParser.Parse(file.FullName);
                }
            }

            private OpenTypeFont GetOTFFont(string postScriptName, FileInfo file)
            {
                try
                {
                    if (file.Name.EndsWith(".ttc", StringComparison.OrdinalIgnoreCase))
                    {
                        //@SuppressWarnings("squid:S2095")
                        // ttc not closed here because it is needed later when ttf is accessed,
                        // e.g. rendering PDF with non-embedded font which is in ttc file in our font directory
                        TrueTypeCollection ttc = new TrueTypeCollection(file);
                        TrueTypeFont ttf = ttc.GetFontByName(postScriptName);
                        if (ttf == null)
                        {
                            ttc.Dispose();
                            throw new IOException("Font " + postScriptName + " not found in " + file);
                        }
                        return (OpenTypeFont)ttf;
                    }

                    OTFParser parser = new OTFParser(false, true);
                    using (var stream = file.OpenRead())
                    {
                        var otf = parser.Parse(stream);
#if DEBUG
                        Debug.WriteLine($"debug: Loaded {postScriptName} from {file}");
#endif
                        return (OpenTypeFont)otf;
                    }
                }
                catch (IOException e)
                {
                    Debug.WriteLine($"eror: Could not load font file: {file} {e}");
                }
                return null;
            }

            private Type1Font GetType1Font(string postScriptName, FileInfo file)
            {
                try
                {
                    using (var input = file.OpenRead())
                    {
                        Type1Font type1 = Type1Font.CreateWithPFB(new Bytes.Buffer(input));
#if DEBUG
                        Debug.WriteLine($"debug: Loaded {postScriptName} from {file}");
#endif
                        return type1;
                    }
                }
                catch (IOException e)
                {
                    Debug.WriteLine($"eror: Could not load font file: {file} {e}");
                }
                return null;
            }
        }

        /**
		 * Represents ignored fonts (i.e. bitmap fonts).
		 */
        private class FSIgnored : FSFontInfo
        {
            public FSIgnored(FileInfo file, FontFormat format, string postScriptName)
                : base(file, format, postScriptName, null, 0, 0, 0, 0, 0, null, null)
            {
            }
        }

        /**
		 * Constructor.
		 */
        public FileSystemFontProvider(FontCache cache)
        {
            this.cache = cache;
            try
            {
#if TRACE
                Debug.WriteLine("trace: Will search the local system for fonts");
#endif
                // scan the local system for font files
                var fontFileFinder = new FontFileFinder();
                List<FileInfo> fonts = fontFileFinder.Find();

#if TRACE
                Debug.WriteLine($"trace: Found {fonts.Count} fonts on the local system");
#endif

                // load cached FontInfo objects
                List<FSFontInfo> cachedInfos = LoadDiskCache(fonts);
                if (cachedInfos != null && cachedInfos.Count > 0)
                {
                    fontInfoList.AddAll(cachedInfos);
                }
                else
                {
                    Debug.WriteLine("warn: Building on-disk font cache, this may take a while");
                    ScanFonts(fonts);
                    SaveDiskCache();
                    Debug.WriteLine($"warn: Finished building on-disk font cache, found {fontInfoList.Count} fonts");
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine($"error: Error accessing the file system {e}");
            }
        }

        private void ScanFonts(List<FileInfo> files)
        {
            foreach (FileInfo file in files)
            {
                try
                {
                    if (file.Name.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase) ||
                            file.Name.EndsWith(".otf", StringComparison.OrdinalIgnoreCase))
                    {
                        AddTrueTypeFont(file);
                    }
                    else if (file.Name.EndsWith(".ttc", StringComparison.OrdinalIgnoreCase) ||
                            file.Name.EndsWith(".otc", StringComparison.OrdinalIgnoreCase))
                    {
                        AddTrueTypeCollection(file);
                    }
                    else if (file.Name.EndsWith(".pfb", StringComparison.OrdinalIgnoreCase))
                    {
                        AddType1Font(file);
                    }
                }
                catch (IOException e)
                {
                    Debug.WriteLine($"eror: Error parsing font {file.Name} {e}");
                }
            }
        }

        private FileInfo GetDiskCacheFile()
        {
            string path = Environment.GetEnvironmentVariable("PdfBox.FontCache");
            if (path == null || !Directory.Exists(path))// || !new DirectoryInfo(path).GetAccessControl()
            {
                path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PdfBox.FontCache");
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }
            return new FileInfo(Path.Combine(path, "PdfBox.Cache"));
            //return new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite);
        }

        /**
		 * Saves the font metadata cache to disk.
		 */
        private void SaveDiskCache()
        {
            try
            {
                FileInfo file = GetDiskCacheFile();

                try
                {
                    using (var fileStream = file.OpenWrite())
                    using (var writer = new BinaryWriter(fileStream, System.Text.Encoding.UTF8))
                    {
                        foreach (FSFontInfo fontInfo in fontInfoList)
                        {
                            WriteFontInfo(writer, fontInfo);
                        }
                    }
                }
                catch (IOException e)
                {
                    Debug.WriteLine($"warn: Could not write to font cache {e}");
                    Debug.WriteLine("warn: Installed fonts information will have to be reloaded for each start");
                    Debug.WriteLine("warn: You can assign a directory to the 'pdfbox.fontcache' property");
                }
            }
            catch (SecurityException e)
            {
                Debug.WriteLine($"debug: Couldn't create writer for font cache file {e}");
            }
        }

        private void WriteFontInfo(BinaryWriter writer, FSFontInfo fontInfo)
        {
            writer.Write(fontInfo.PostScriptName.Trim());
            writer.Write((int)fontInfo.Format);
            if (fontInfo.CIDSystemInfo != null)
            {
                writer.Write(fontInfo.CIDSystemInfo.Registry + '-' +
                                fontInfo.CIDSystemInfo.Ordering + '-' +
                                fontInfo.CIDSystemInfo.Supplement);
            }
            else
            {
                writer.Write(" ");
            }
            writer.Write(fontInfo.WeightClass);
            writer.Write(fontInfo.FamilyClass);
            writer.Write(fontInfo.CodePageRange1);
            writer.Write(fontInfo.CodePageRange2);
            writer.Write(fontInfo.MacStyle);
            if (fontInfo.Panose != null)
            {
                writer.Write((byte)10);
                byte[] bytes = fontInfo.Panose.Bytes;
                writer.Write(bytes);
            }
            else
            {
                writer.Write((byte)0);
            }
            writer.Write(fontInfo.file.FullName);
            writer.Write((byte)'\n');
        }

        /**
		 * Loads the font metadata cache from disk.
		 */
        private List<FSFontInfo> LoadDiskCache(List<FileInfo> files)
        {
            ISet<string> pending = new HashSet<string>(files.Select(x => x.FullName), StringComparer.Ordinal);
            List<FSFontInfo> results = new List<FSFontInfo>();

            // Get the disk cache
            FileInfo file = null;
            bool fileExists = false;
            try
            {
                file = GetDiskCacheFile();
                fileExists = file.Exists;
            }
            catch (SecurityException e)
            {
                Debug.WriteLine("debug: Error checking for file existence", e);
            }

            if (fileExists)
            {
                try
                {
                    using (var fileStream = file.OpenRead())
                    using (var reader = new BinaryReader(fileStream, System.Text.Encoding.UTF8))
                    {
                        while (reader.PeekChar() > -1)
                        {

                            string postScriptName = reader.ReadString();
                            FontFormat format = (FontFormat)reader.ReadInt32();

                            CIDSystemInfo cidSystemInfo = null;
                            string cidSystemInfoText = reader.ReadString();
                            if (!string.IsNullOrWhiteSpace(cidSystemInfoText))
                            {
                                string[] ros = cidSystemInfoText.Split('-');
                                cidSystemInfo = new CIDSystemInfo(null, ros[0], ros[1], int.TryParse(ros[2], out var intValue) ? intValue : 0);
                            }
                            int usWeightClass = reader.ReadInt32();
                            int sFamilyClass = reader.ReadInt32();
                            int ulCodePageRange1 = reader.ReadInt32();
                            int ulCodePageRange2 = reader.ReadInt32();
                            int macStyle = reader.ReadInt32();

                            byte[] panose = null;
                            if (reader.ReadByte() > 0)
                            {
                                panose = new byte[10];
                                reader.Read(panose, 0, 10);
                            }

                            var fullpath = reader.ReadString();
                            reader.ReadByte();

                            if (!string.IsNullOrEmpty(fullpath))
                            {
                                FileInfo fontFile = new FileInfo(fullpath);
                                if (fontFile.Exists)
                                {
                                    FSFontInfo info = new FSFontInfo(fontFile, format, postScriptName,
                                            cidSystemInfo, usWeightClass, sFamilyClass, ulCodePageRange1,
                                            ulCodePageRange2, macStyle, panose, this);
                                    results.Add(info);
                                }
                                else
                                {
                                    Debug.WriteLine($"debug: Font file {fullpath} not found, skipped");
                                }
                            }
                            else
                            {
                                Debug.WriteLine($"debug: Font file {fullpath} not found, skipped");
                            }
                            pending.Remove(fullpath);
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"eror: Error loading font cache, will be re-built {e}");
                }
            }

            if (pending.Count > 0)
            {
                // re-build the entire cache if we encounter un-cached fonts (could be optimised)
                Debug.WriteLine("warn: New fonts found, font cache will be re-built");
                return null;
            }

            return results;
        }

        /**
		 * Adds a TTC or OTC to the file cache. To reduce memory, the parsed font is not cached.
		 */
        private void AddTrueTypeCollection(FileInfo ttcFile)
        {
            try
            {
                using (TrueTypeCollection ttc = new TrueTypeCollection(ttcFile))
                {
                    ttc.ProcessAllFonts(AddTrueTypeFontImpl, ttcFile);
                }
            }
            catch (IOException e)
            {
                Debug.WriteLine("eror: Could not load font file: " + ttcFile, e);
            }
        }

        /**
		 * Adds an OTF or TTF font to the file cache. To reduce memory, the parsed font is not cached.
		 */
        private void AddTrueTypeFont(FileInfo ttfFile)
        {
            try
            {
                if (ttfFile.Name.EndsWith(".otf", StringComparison.OrdinalIgnoreCase))
                {
                    OTFParser parser = new OTFParser(false, true);
                    var otf = parser.Parse(ttfFile.FullName);
                    AddTrueTypeFontImpl(otf, ttfFile);
                }
                else
                {
                    TTFParser parser = new TTFParser(false, true);
                    var ttf = parser.Parse(ttfFile.FullName);
                    AddTrueTypeFontImpl(ttf, ttfFile);
                }
            }
            catch (IOException e)
            {
                Debug.WriteLine("eror: Could not load font file: " + ttfFile, e);
            }
        }

        /**
		 * Adds an OTF or TTF font to the file cache. To reduce memory, the parsed font is not cached.
		 */
        private void AddTrueTypeFontImpl(TrueTypeFont ttf, object tag)
        {
            AddTrueTypeFontImpl(ttf, (FileInfo)tag);
        }

        private void AddTrueTypeFontImpl(TrueTypeFont ttf, FileInfo file)
        {
            try
            {
                // read PostScript name, if any
                if (ttf.Name != null && ttf.Name.Contains("|"))
                {
                    fontInfoList.Add(new FSIgnored(file, FontFormat.TTF, "*skippipeinname*"));
                    Debug.WriteLine($"warn: Skipping font with '|' in name {ttf.Name} in file {file}");
                }
                else if (ttf.Name != null)
                {
                    // ignore bitmap fonts
                    if (ttf.Header == null)
                    {
                        fontInfoList.Add(new FSIgnored(file, FontFormat.TTF, ttf.Name));
                        return;
                    }
                    int macStyle = ttf.Header.MacStyle;

                    int sFamilyClass = -1;
                    int usWeightClass = -1;
                    int ulCodePageRange1 = 0;
                    int ulCodePageRange2 = 0;
                    byte[] panose = null;
                    // Apple's AAT fonts don't have an OS/2 table
                    if (ttf.OS2Windows != null)
                    {
                        sFamilyClass = ttf.OS2Windows.FamilyClass;
                        usWeightClass = ttf.OS2Windows.WeightClass;
                        ulCodePageRange1 = (int)ttf.OS2Windows.CodePageRange1;
                        ulCodePageRange2 = (int)ttf.OS2Windows.CodePageRange2;
                        panose = ttf.OS2Windows.Panose;
                    }

                    string format;
                    if (ttf is OpenTypeFont openTypeFont && openTypeFont.IsPostScript)
                    {
                        format = "OTF";
                        CFFFont cff = openTypeFont.CFF.Font;
                        CIDSystemInfo ros = null;
                        if (cff is CFFCIDFont cidFont)
                        {
                            string registry = cidFont.Registry;
                            string ordering = cidFont.Ordering;
                            int supplement = cidFont.Supplement;
                            ros = new CIDSystemInfo(null, registry, ordering, supplement);
                        }
                        fontInfoList.Add(new FSFontInfo(file, FontFormat.OTF, ttf.Name, ros,
                                usWeightClass, sFamilyClass, ulCodePageRange1, ulCodePageRange2,
                                macStyle, panose, this));
                    }
                    else
                    {
                        CIDSystemInfo ros = null;
                        if (ttf.TableMap.TryGetValue("gcid", out var gcid))
                        {
                            // Apple's AAT fonts have a "gcid" table with CID info
                            byte[] bytes = ttf.GetTableBytes(gcid);
                            string reg = Charset.ASCII.GetString(bytes, 10, 64);
                            string registryName = reg.Substring(0, reg.IndexOf('\0'));
                            string ord = Charset.ASCII.GetString(bytes, 76, 64);
                            string orderName = ord.Substring(0, ord.IndexOf('\0'));
                            int supplementVersion = bytes[140] << 8 & (bytes[141] & 0xFF);
                            ros = new CIDSystemInfo(null, registryName, orderName, supplementVersion);
                        }

                        format = "TTF";
                        fontInfoList.Add(new FSFontInfo(file, FontFormat.TTF, ttf.Name, ros,
                                usWeightClass, sFamilyClass, ulCodePageRange1, ulCodePageRange2,
                                macStyle, panose, this));
                    }

#if TRACE
                    NamingTable name = ttf.Naming;
                    if (name != null)
                    {
                        Debug.WriteLine($"trace: {format}: '{name.PostScriptName}' / '{name.FontFamily}' / '{name.FontSubFamily}'");
                    }
#endif
                }
                else
                {
                    fontInfoList.Add(new FSIgnored(file, FontFormat.TTF, "*skipnoname*"));
                    Debug.WriteLine($"warn: Missing 'name' entry for PostScript name in font {file}");
                }
            }
            catch (IOException e)
            {
                fontInfoList.Add(new FSIgnored(file, FontFormat.TTF, "*skipexception*"));
                Debug.WriteLine($"eror: Could not load font file: {file} {e}");
            }
            finally
            {
                ttf.Dispose();
            }
        }

        /**
		 * Adds a Type 1 font to the file cache. To reduce memory, the parsed font is not cached.
		 */
        private void AddType1Font(FileInfo pfbFile)
        {
            try
            {
                using (var fileStream = pfbFile.OpenRead())
                using (var input = new Bytes.Buffer(fileStream))
                {
                    Type1Font type1 = Type1Font.CreateWithPFB(input);
                    if (type1.Name != null && type1.Name.Contains("|"))
                    {
                        fontInfoList.Add(new FSIgnored(pfbFile, FontFormat.PFB, "*skippipeinname*"));
                        Debug.WriteLine($"warn: Skipping font with '|' in name {type1.Name} in file {pfbFile}");
                        return;
                    }
                    fontInfoList.Add(new FSFontInfo(pfbFile, FontFormat.PFB, type1.Name,
                                                    null, -1, -1, 0, 0, -1, null, this));

#if TRACE
                    Debug.WriteLine($"trace: PFB: '{type1.Name}' / '{type1.FamilyName}' / '{type1.Weight}'");
#endif
                }
            }
            catch (IOException e)
            {
                Debug.WriteLine($"eror: Could not load font file: {pfbFile} {e}");
            }
        }


        public override string ToDebugString()
        {
            var sb = new StringBuilder();
            foreach (FSFontInfo info in fontInfoList)
            {
                sb.Append(info.Format);
                sb.Append(": ");
                sb.Append(info.PostScriptName);
                sb.Append(": ");
                sb.Append(info.file.Name);
                sb.Append('\n');
            }
            return sb.ToString();
        }


        public override IEnumerable<FontInfo> FontInfo
        {
            get => fontInfoList;
        }
    }
}
