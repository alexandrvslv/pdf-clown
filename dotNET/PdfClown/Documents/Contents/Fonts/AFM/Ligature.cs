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
     * This class represents a ligature, which is an entry of the CharMetrics.
     *
     * @author Ben Litchfield
     */
    public class Ligature
    {
        private string successor;
        private string ligature;

        /** Getter for property ligature.
         * @return Value of property ligature.
         */
        public string LigatureValue
        {
            get => ligature;
            set => ligature = value;
        }

        /** Getter for property successor.
         * @return Value of property successor.
         */
        public string Successor
        {
            get => successor;
            set => successor = value;
        }
    }
}