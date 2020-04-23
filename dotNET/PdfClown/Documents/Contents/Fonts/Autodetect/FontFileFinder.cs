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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace PdfClown.Documents.Contents.Fonts.Autodetect
{

    /**
     * Helps to autodetect/locate available operating system fonts. This class is based on a class provided by Apache FOP.
     * see org.apache.fop.fonts.autodetect.FontFileFinder
     */
    public class FontFileFinder
    {

        private IFontDirFinder fontDirFinder = null;

        /**
         * Default constructor.
         */
        public FontFileFinder()
        {
        }

        private IFontDirFinder DetermineDirFinder()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return new WindowsFontDirFinder();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return new MacFontDirFinder();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return new UnixFontDirFinder();
            }
            return null;
        }

        /**
         * Automagically finds a list of font files on local system.
         * 
         * @return List&lt;Uri&gt; of font files
         */
        public List<FileInfo> Find()
        {
            if (fontDirFinder == null)
            {
                fontDirFinder = DetermineDirFinder();
            }
            var fontDirs = fontDirFinder.Find();
            var results = new List<FileInfo>();
            foreach (var dir in fontDirs)
                Walk(dir, results);
            return results;
        }

        /**
         * Searches a given directory for font files.
         * 
         * @param dir directory to search
         * @return list&lt;Uri&gt; of font files
         */
        public List<FileInfo> Find(string dir)
        {
            var results = new List<FileInfo>();
            if (Directory.Exists(dir))
            {
                var directory = new DirectoryInfo(dir);
                Walk(directory, results);
            }
            return results;
        }

        /**
         * walk down the directory tree and search for font files.
         * 
         * @param directory the directory to start at
         * @param results names of all found font files
         */
        private void Walk(DirectoryInfo directory, List<FileInfo> results)
        {
            // search for font files recursively in the given directory

            FileSystemInfo[] filelist = directory.GetFileSystemInfos();
            if (filelist == null)
            {
                return;
            }
            foreach (FileSystemInfo file in filelist)
            {
                if (file is DirectoryInfo directoryInfo)
                {
                    // skip hidden directories
                    if (directoryInfo.Name.StartsWith(".", StringComparison.Ordinal))
                    {
                        continue;
                    }
                    Walk(directoryInfo, results);
                }
                else if (file is FileInfo fileInfo)
                {
#if DEBUG
                    Debug.WriteLine("debug: check Fontfile start " + file);
#endif
                    if (CheckFontfile(fileInfo))
                    {
#if DEBUG
                        Debug.WriteLine("debug: check Fontfile success " + file);
#endif
                        results.Add(fileInfo);
                    }
                }
            }
        }

        /**
         * Check if the given name belongs to a font file.
         * 
         * @param file the given file
         * @return true if the given filename has a typical font file ending
         */
        private bool CheckFontfile(FileInfo file)
        {
            string name = file.Name.ToLowerInvariant();
            return (name.EndsWith(".ttf")
                || name.EndsWith(".otf")
                || name.EndsWith(".pfb")
                || name.EndsWith(".ttc"))
                    // PDFBOX-3377 exclude weird files in AIX
                    && !name.StartsWith("fonts.");
        }
    }
}