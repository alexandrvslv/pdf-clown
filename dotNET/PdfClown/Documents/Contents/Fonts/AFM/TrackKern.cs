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
namespace PdfClown.Documents.Contents.Fonts.AFM
{
    /**
     * This class represents a piece of track kerning data.
     *
     * @author Ben Litchfield
     */
    public class TrackKern
    {
        private int degree;
        private float minPointSize;
        private float minKern;
        private float maxPointSize;
        private float maxKern;

        /** Getter for property degree.
         * @return Value of property degree.
         */
        public int Degree
        {
            get => degree;
            set => degree = value;
        }

        /** Getter for property maxKern.
         * @return Value of property maxKern.
         */
        public float MaxKern
        {
            get => maxKern;
            set => maxKern = value;
        }

        /** Getter for property maxPointSize.
         * @return Value of property maxPointSize.
         */
        public float MaxPointSize
        {
            get => maxPointSize;
            set => maxPointSize = value;
        }

        /** Getter for property minKern.
         * @return Value of property minKern.
         */
        public float MinKern
        {
            get => minKern;
            set => minKern = value;
        }

        /** Getter for property minPointSize.
         * @return Value of property minPointSize.
         */
        public float MinPointSize
        {
            get => minPointSize;
            set => minPointSize = value;
        }
    }
}