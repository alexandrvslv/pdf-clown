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
     * This class represents a part of composite character data.
     *
     * @author Ben Litchfield
     */
    public class CompositePart
    {
        private string name;
        private int xDisplacement;
        private int yDisplacement;

        /** Getter for property name.
         * @return Value of property name.
         */
        public string Name
        {
            get => name;
            set => name = value;
        }

        /** Getter for property xDisplacement.
         * @return Value of property xDisplacement.
         */
        public int XDisplacement
        {
            get => xDisplacement;
            set => xDisplacement = value;
        }

        /** Getter for property yDisplacement.
         * @return Value of property yDisplacement.
         */
        public int YDisplacement
        {
            get => yDisplacement;
            set => yDisplacement = value;
        }
    }
}