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

using Org.BouncyCastle.X509;

namespace PdfClown.Documents.Encryption
{

    /**
     * Represents a recipient in the public key protection policy.
     *
     * @see PublicKeyProtectionPolicy
     *
     * @author Benoit Guillon
     *
     */
    public class PublicKeyRecipient
    {
        private X509Certificate x509;

        private AccessPermission permission;

        /**
		 * Returns the X509 certificate of the recipient.
		 *
		 * @return The X509 certificate
		 */
        /**
		 * Set the X509 certificate of the recipient.
		 *
		 * @param aX509 The X509 certificate
		 */
        public X509Certificate X509
        {
            get => x509;
            set => x509 = value;
        }

        /**
		 * Returns the access permission granted to the recipient.
		 *
		 * @return The access permission object.
		 */
        /**
		* Set the access permission granted to the recipient.
		*
		* @param permissions The permission to set.
		*/
        public AccessPermission Permission
        {
            get => permission;
            set => permission = value;
        }
    }
}