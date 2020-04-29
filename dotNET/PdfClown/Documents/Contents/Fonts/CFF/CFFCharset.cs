/*
 * https://github.com/apache/pdfbox
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

namespace PdfClown.Documents.Contents.Fonts.CCF
{
    /**
     * A CFF charset. A charset is an array of SIDs/CIDs for all glyphs in the font.
     *
     * todo: split this into two? CFFCharsetType1 and CFFCharsetCID ?
     *
     * @author John Hewson
     */
    public abstract class CFFCharset
    {
        private readonly bool isCIDFont;
        private readonly Dictionary<int, int> sidOrCidToGid = new Dictionary<int, int>(250);
        private readonly Dictionary<int, int> gidToSid = new Dictionary<int, int>(250);
        private readonly Dictionary<string, int> nameToSid = new Dictionary<string, int>(250, StringComparer.Ordinal);

        // inverse
        private readonly Dictionary<int, int> gidToCid = new Dictionary<int, int>();
        private readonly Dictionary<int, string> gidToName = new Dictionary<int, string>(250);

        /**
         * Package-private constructor for use by subclasses.
         *
         * @param isCIDFont true if the parent font is a CIDFont
         */
        public CFFCharset(bool isCIDFont)
        {
            this.isCIDFont = isCIDFont;
        }

        /**
         * Indicates if the charset belongs to a CID font.
         * 
         * @return true for CID fonts
         */
        public bool IsCIDFont
        {
            get => isCIDFont;
        }

        /**
         * Adds a new GID/SID/name combination to the charset.
         *
         * @param gid GID
         * @param sid SID
         */
        public void AddSID(int gid, int sid, string name)
        {
            if (isCIDFont)
            {
                throw new InvalidOperationException("Not a Type 1-equivalent font");
            }
            sidOrCidToGid[sid] = gid;
            gidToSid[gid] = sid;

            nameToSid[name] = sid;
            gidToName[gid] = name;
        }

        /**
         * Adds a new GID/CID combination to the charset.
         *
         * @param gid GID
         * @param cid CID
         */
        public void AddCID(int gid, int cid)
        {
            if (!isCIDFont)
            {
                throw new InvalidOperationException("Not a CIDFont");
            }
            sidOrCidToGid[cid] = gid;
            gidToCid[gid] = cid;
        }

        /**
         * Returns the SID for a given GID. SIDs are internal to the font and are not public.
         *
         * @param sid SID
         * @return GID
         */
        public virtual int GetSIDForGID(int gid)
        {
            if (isCIDFont)
            {
                throw new InvalidOperationException("Not a Type 1-equivalent font");
            }
            if (!gidToSid.TryGetValue(gid, out int sid))
            {
                return 0;
            }
            return sid;
        }

        /**
         * Returns the GID for the given SID. SIDs are internal to the font and are not public.
         *
         * @param sid SID
         * @return GID
         */
        public virtual int GetGIDForSID(int sid)
        {
            if (isCIDFont)
            {
                throw new InvalidOperationException("Not a Type 1-equivalent font");
            }
            if (!sidOrCidToGid.TryGetValue(sid, out int gid))
            {
                return 0;
            }
            return gid;
        }

        /**
         * Returns the GID for a given CID. Returns 0 if the CID is missing.
         *
         * @param cid CID
         * @return GID
         */
        public virtual int GetGIDForCID(int cid)
        {
            if (!isCIDFont)
            {
                throw new InvalidOperationException("Not a CIDFont");
            }
            if (!sidOrCidToGid.TryGetValue(cid, out int gid))
            {
                return 0;
            }
            return gid;
        }

        /**
         * Returns the SID for a given PostScript name, you would think this is not needed,
         * but some fonts have glyphs beyond their encoding with charset SID names.
         *
         * @param name PostScript glyph name
         * @return SID
         */
        public int GetSID(string name)
        {
            if (isCIDFont)
            {
                throw new InvalidOperationException("Not a Type 1-equivalent font");
            }
            if (!nameToSid.TryGetValue(name, out int sid))
            {
                return 0;
            }
            return sid;
        }

        /**
         * Returns the PostScript glyph name for the given GID.
         *
         * @param gid GID
         * @return PostScript glyph name
         */
        public string GetNameForGID(int gid)
        {
            if (isCIDFont)
            {
                throw new InvalidOperationException("Not a Type 1-equivalent font");
            }
            if (!gidToName.TryGetValue(gid, out var name))
            {
                return null;
            }
            return name;
        }

        /**
         * Returns the CID for the given GID.
         *
         * @param gid GID
         * @return CID
         */
        public virtual int GetCIDForGID(int gid)
        {
            if (!isCIDFont)
            {
                throw new InvalidOperationException("Not a CIDFont");
            }

            if (gidToCid.TryGetValue(gid, out int cid))
            {
                return cid;
            }
            return 0;
        }
    }
}