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
using PdfClown.Documents.Contents.Fonts.Type1;

namespace PdfClown.Documents.Contents.Fonts.CCF
{
    /**
	 * This is specialized DataInput. It's used to parse a CFFFont.
	 * 
	 * @author Villu Ruusmann
	 */
    public class CFFDataInput : DataInput
    {

        /**
		 * Constructor.
		 * @param buffer the buffer to be read 
		 */
        public CFFDataInput(byte[] buffer)
            : base(buffer)
        {
        }

        /**
		 * Read one single Card8 value from the buffer. 
		 * @return the card8 value
		 * @throws IOException if an error occurs during reading
		 */
        public byte ReadCard8()
        {
            return ReadUnsignedByte();
        }

        /**
		 * Read one single Card16 value from the buffer.
		 * @return the card16 value
		 * @throws IOException if an error occurs during reading
		 */
        public ushort ReadCard16()
        {
            return readUnsignedShort();
        }

        /**
		 * Read the offset from the buffer.
		 * @param offSize the given offsize
		 * @return the offset
		 * @throws IOException if an error occurs during reading
		 */
        public int ReadOffset(int offSize)
        {
            int value = 0;
            for (int i = 0; i < offSize; i++)
            {
                value = value << 8 | ReadUnsignedByte();
            }
            return value;
        }

        /**
		 * Read the offsize from the buffer.
		 * @return the offsize
		 * @throws IOException if an error occurs during reading
		 */
        public byte ReadOffSize()
        {
            return ReadUnsignedByte();
        }

        /**
		 * Read a SID from the buffer.
		 * @return the SID
		 * @throws IOException if an error occurs during reading
		 */
        public ushort ReadSID()
        {
            return readUnsignedShort();
        }
    }
}