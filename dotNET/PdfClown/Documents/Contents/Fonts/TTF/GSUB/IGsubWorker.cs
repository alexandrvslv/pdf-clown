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

namespace PdfClown.Documents.Contents.Fonts.TTF.GSUB
{

    /// <summary>
    /// This class is responsible for replacing GlyphIDs with new ones according to the GSUB tables.Each language should
    /// have an implementation of this.
    /// @author Palash Ray
    /// </summary>
    public interface IGsubWorker
    {
        /// <summary>
        /// Applies language-specific transforms including GSUB and any other pre or post-processing necessary for displaying
        /// Glyphs correctly.
        /// </summary>
        /// <param name="originalGlyphIds"></param>
        /// <returns></returns>
        HashList<ushort> ApplyTransforms(HashList<ushort> originalGlyphIds);

    }
}