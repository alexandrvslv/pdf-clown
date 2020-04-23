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
using SkiaSharp;
using System.Collections.Generic;

namespace PdfClown.Documents.Contents.Fonts.AFM
{
    /**
     * This class represents a single character metric.
     *
     * @author Ben Litchfield
     */
    public class CharMetric
    {
        private int characterCode;

        private float wx;
        private float w0x;
        private float w1x;

        private float wy;
        private float w0y;
        private float w1y;

        private float[] w;
        private float[] w0;
        private float[] w1;
        private float[] vv;

        private string name;
        private SKRect boundingBox;
        private List<Ligature> ligatures = new List<Ligature>();

        /** Getter for property boundingBox.
         * @return Value of property boundingBox.
         */
        public SKRect BoundingBox
        {
            get => boundingBox;
            set => boundingBox = value;
        }

        /** Getter for property characterCode.
         * @return Value of property characterCode.
         */
        public int CharacterCode
        {
            get => characterCode;
            set => characterCode = value;
        }

        /**
         * This will add an entry to the list of ligatures.
         *
         * @param ligature The ligature to add.
         */
        public void AddLigature(Ligature ligature)
        {
            ligatures.Add(ligature);
        }

        /** Getter for property ligatures.
         * @return Value of property ligatures.
         */
        public List<Ligature> Ligatures
        {
            get => ligatures;
            set => ligatures = value;
        }

        /** Getter for property name.
         * @return Value of property name.
         */
        public string Name
        {
            get => name;
            set => name = value;
        }

        /** Getter for property vv.
         * @return Value of property vv.
         */
        public float[] Vv
        {
            get => vv;
            set => vv = value;
        }

        /** Getter for property w.
         * @return Value of property w.
         */
        public float[] W
        {
            get => w;
            set => w = value;
        }

        /** Getter for property w0.
         * @return Value of property w0.
         */
        public float[] W0
        {
            get => w0;
            set => w0 = value;
        }

        /** Getter for property w0x.
         * @return Value of property w0x.
         */
        public float W0x
        {
            get => w0x;
            set => w0x = value;
        }

        /** Getter for property w0y.
         * @return Value of property w0y.
         */
        public float W0y
        {
            get => w0y;
            set => w0y = value;
        }

        /** Getter for property w1.
         * @return Value of property w1.
         */
        public float[] W1
        {
            get => w1;
            set => w1 = value;
        }

        /** Getter for property w1x.
         * @return Value of property w1x.
         */
        public float W1x
        {
            get => w1x;
            set => w1x = value;
        }

        /** Getter for property w1y.
         * @return Value of property w1y.
         */
        public float W1y
        {
            get => w1y;
            set => w1y = value;
        }

        /** Getter for property wx.
         * @return Value of property wx.
         */
        public float Wx
        {
            get => wx;
            set => wx = value;
        }

        /** Getter for property wy.
         * @return Value of property wy.
         */
        public float Wy
        {
            get => wy;
            set => wy = value;
        }
    }
}