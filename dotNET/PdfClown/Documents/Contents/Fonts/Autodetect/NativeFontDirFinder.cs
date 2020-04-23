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

namespace PdfClown.Documents.Contents.Fonts.Autodetect
{

    /**
     * Native font finder base class. This class is based on a class provided by Apache FOP. see
     * org.apache.fop.fonts.autodetect.NativeFontDirFinder
     */
    public class NativeFontDirFinder : IFontDirFinder
    {

        /**
         * Generic method used by Mac and Unix font finders.
         * 
         * @return list of natively existing font directories {@inheritDoc}
         */
        public List<DirectoryInfo> Find()
        {
            List<DirectoryInfo> fontDirList = new List<DirectoryInfo>();
            string[] searchableDirectories = GetSearchableDirectories();
            if (searchableDirectories != null)
            {
                foreach (string searchableDirectorie in searchableDirectories)
                {
                    try
                    {
                        if (Directory.Exists(searchableDirectorie))
                        {
                            fontDirList.Add(new DirectoryInfo(searchableDirectorie));
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine("debug: Couldn't get native font directories - ignoring", e);
                        // should continue if this fails
                    }
                }
            }
            return fontDirList;
        }

        /**
         * Returns an array of directories to search for fonts in.
         * 
         * @return an array of directories
         */
        protected virtual string[] GetSearchableDirectories()
        {
            return new[] { Environment.GetFolderPath(Environment.SpecialFolder.Fonts) };
        }

    }
}