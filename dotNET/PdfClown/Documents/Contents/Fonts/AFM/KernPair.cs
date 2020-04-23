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
     * This represents some kern pair data.
     *
     * @author Ben Litchfield
     */
    public class KernPair
    {
        private string firstKernCharacter;
        private string secondKernCharacter;
        private float x;
        private float y;

        /** Getter for property firstKernCharacter.
         * @return Value of property firstKernCharacter.
         */
        public string FirstKernCharacter
        {
            get => firstKernCharacter;
            set => firstKernCharacter = value;
        }

        /** Getter for property secondKernCharacter.
         * @return Value of property secondKernCharacter.
         */
        public string SecondKernCharacter
        {
            get => secondKernCharacter;
            set => secondKernCharacter = value;
        }

        /** Getter for property x.
         * @return Value of property x.
         */
        public float X
        {
            get => x;
            set => x = value;
        }

        /** Getter for property y.
         * @return Value of property y.
         */
        public float Y
        {
            get => y;
            set => y = value;
        }
    }
}