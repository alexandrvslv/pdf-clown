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
using PdfClown.Objects;
using System;
using System.Collections.Generic;

namespace PdfClown.Documents.Contents.Fonts
{
    /**
     * Represents a CIDSystemInfo.
     *
     * @author John Hewson
     */
    public sealed class CIDSystemInfo : PdfObjectWrapper<PdfDictionary>
    {
        private string registry;

        public CIDSystemInfo(Document document, string registry, string ordering, int supplement)
            : base(document, new PdfDictionary(
                new PdfName[] { PdfName.Registry, PdfName.Ordering, PdfName.Supplement },
                new PdfDirectObject[] {
                    new PdfString( registry),
                    new PdfString( ordering),
                    new PdfInteger( supplement),
                }))
        {
        }

        public CIDSystemInfo(PdfDirectObject dictionary) : base(dictionary)
        {
        }

        public string Registry
        {
            get => Dictionary.GetString(PdfName.Registry);
            set => Dictionary[PdfName.Registry] = new PdfString(registry = value);
        }

        public string Ordering
        {
            get => Dictionary.GetString(PdfName.Ordering);
            set => Dictionary[PdfName.Ordering] = new PdfString(value);
        }

        public int Supplement
        {
            get => Dictionary.GetInt(PdfName.Supplement, 0);
            set => Dictionary[PdfName.Supplement] = new PdfInteger(value);
        }

        public override string ToString()
        {
            return $"{Registry}-{Ordering}-{Supplement}";
        }
    }
}
