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

//using PdfClown.Bytes;
using Org.BouncyCastle.Security;
using PdfClown.Documents.Contents.Fonts.TTF.GSUB;
using PdfClown.Objects;
using PdfClown.Tokens;
using PdfClown.Util.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security;
using System.Security.Cryptography;

namespace PdfClown.Documents.Encryption
{

    /**
     * A security handler as described in the PDF specifications.
     * A security handler is responsible of documents protection.
     *
     * @author Ben Litchfield
     * @author Benoit Guillon
     * @author Manuel Kasper
     */
    public abstract class SecurityHandler
    {
        private static readonly int DEFAULT_KEY_LENGTH = 40;

        // see 7.6.2, page 58, PDF 32000-1:2008
        private static readonly byte[] AES_SALT = { (byte)0x73, (byte)0x41, (byte)0x6c, (byte)0x54 };

        /** The Length in bits of the secret key used to encrypt the document. */
        protected int keyLength = DEFAULT_KEY_LENGTH;

        /** The encryption key that will used to encrypt / decrypt.*/
        protected byte[] encryptionKey;

        /** The RC4 implementation used for cryptographic functions. */
        private readonly RC4Cipher rc4 = new RC4Cipher();

        /** indicates if the Metadata have to be decrypted of not. */
        private bool decryptMetadata;

        // PDFBOX-4453, PDFBOX-4477: Originally this was just a Set. This failed in rare cases
        // when a decrypted string was identical to an encrypted string.
        // Because PdfString.equals() checks the contents, decryption was then skipped.
        // This solution keeps all different "equal" objects.
        // IdentityHashMap solves this problem and is also faster than a HashMap
        private readonly HashSet<PdfObject> objects = new HashSet<PdfObject>();

        private bool useAES;

        /**
		 * The access permission granted to the current user for the document. These
		 * permissions are computed during decryption and are in read only mode.
		 */
        private AccessPermission currentAccessPermission = null;

        /**
		 * The stream filter name.
		 */
        private PdfName streamFilterName;

        /**
		 * The string filter name.
		 */
        private PdfName stringFilterName;

        /**
		 * Set whether to decrypt meta data.
		 *
		 * @param decryptMetadata true if meta data has to be decrypted.
		 */
        protected void SetDecryptMetadata(bool decryptMetadata)
        {
            this.decryptMetadata = decryptMetadata;
        }

        /**
		 * Set the string filter name.
		 * 
		 * @param stringFilterName the string filter name.
		 */
        protected void SetStringFilterName(PdfName stringFilterName)
        {
            this.stringFilterName = stringFilterName;
        }

        /**
		 * Set the stream filter name.
		 * 
		 * @param streamFilterName the stream filter name.
		 */
        protected void SetStreamFilterName(PdfName streamFilterName)
        {
            this.streamFilterName = streamFilterName;
        }

        /**
		 * Prepare the document for encryption.
		 *
		 * @param doc The document that will be encrypted.
		 *
		 * @throws IOException If there is an error with the document.
		 */
        public abstract void PrepareDocumentForEncryption(Document doc);

        /**
		 * Prepares everything to decrypt the document.
		 *
		 * @param encryption  encryption dictionary, can be retrieved via {@link PDDocument#getEncryption()}
		 * @param documentIDArray  document id which is returned via {@link org.apache.pdfbox.cos.PdfDocument#getDocumentID()}
		 * @param decryptionMaterial Information used to decrypt the document.
		 *
		 * @throws InvalidPasswordException If the password is incorrect.
		 * @throws IOException If there is an error accessing data.
		 */
        public abstract void PrepareForDecryption(PdfEncryption encryption, PdfArray documentIDArray, DecryptionMaterial decryptionMaterial);

        /**
		 * Encrypt or decrypt a set of data.
		 *
		 * @param objectNumber The data object number.
		 * @param genNumber The data generation number.
		 * @param data The data to encrypt.
		 * @param output The output to write the encrypted data to.
		 * @param decrypt true to decrypt the data, false to encrypt it.
		 *
		 * @throws IOException If there is an error reading the data.
		 */
        private bool EncryptData(long objectNumber, long genNumber, Stream data, Stream output, bool decrypt)
        {
            // Determine whether we're using Algorithm 1 (for RC4 and AES-128), or 1.A (for AES-256)
            if (useAES && encryptionKey.Length == 32)
            {
                return EncryptDataAES256(data, output, decrypt);
            }
            else
            {
                byte[] readonlyKey = CalcFinalKey(objectNumber, genNumber);

                if (useAES)
                {
                    return EncryptDataAESother(readonlyKey, data, output, decrypt);
                }
                else
                {
                    return EncryptDataRC4(readonlyKey, data, output);
                }
            }
            //output.Flush();
        }

        /**
		 * Calculate the key to be used for RC4 and AES-128.
		 *
		 * @param objectNumber The data object number.
		 * @param genNumber The data generation number.
		 * @return the calculated key.
		 */
        private byte[] CalcFinalKey(long objectNumber, long genNumber)
        {
            byte[] newKey = new byte[encryptionKey.Length + 5];
            Array.Copy(encryptionKey, 0, newKey, 0, encryptionKey.Length);
            // PDF 1.4 reference pg 73
            // step 1
            // we have the reference
            // step 2
            newKey[newKey.Length - 5] = (byte)(objectNumber & 0xff);
            newKey[newKey.Length - 4] = (byte)(objectNumber >> 8 & 0xff);
            newKey[newKey.Length - 3] = (byte)(objectNumber >> 16 & 0xff);
            newKey[newKey.Length - 2] = (byte)(genNumber & 0xff);
            newKey[newKey.Length - 1] = (byte)(genNumber >> 8 & 0xff);
            // step 3
            using (MD5 md = MD5.Create())
            {
                md.Update(newKey);
                if (useAES)
                {
                    md.Update(AES_SALT);
                }
                byte[] digestedKey = md.Digest();

                // step 4
                int Length = Math.Min(newKey.Length, 16);
                byte[] readonlyKey = new byte[Length];
                Array.Copy(digestedKey, 0, readonlyKey, 0, Length);
                return readonlyKey;
            }
        }

        /**
		 * Encrypt or decrypt data with RC4.
		 *
		 * @param readonlyKey The readonly key obtained with via {@link #calcFinalKey(long, long)}.
		 * @param input The data to encrypt.
		 * @param output The output to write the encrypted data to.
		 *
		 * @throws IOException If there is an error reading the data.
		 */
        protected bool EncryptDataRC4(byte[] readonlyKey, Stream input, Stream output)
        {
            rc4.SetKey(readonlyKey);
            rc4.Write(input, output);
            return true;
        }

        /**
		 * Encrypt or decrypt data with RC4.
		 *
		 * @param readonlyKey The readonly key obtained with via {@link #calcFinalKey(long, long)}.
		 * @param input The data to encrypt.
		 * @param output The output to write the encrypted data to.
		 *
		 * @throws IOException If there is an error reading the data.
		 */
        protected bool EncryptDataRC4(byte[] readonlyKey, byte[] input, Stream output)
        {
            rc4.SetKey(readonlyKey);
            rc4.Write(input, output);
            return true;
        }


        /**
		 * Encrypt or decrypt data with AES with key Length other than 256 bits.
		 *
		 * @param readonlyKey The readonly key obtained with via {@link #calcFinalKey(long, long)}.
		 * @param data The data to encrypt.
		 * @param output The output to write the encrypted data to.
		 * @param decrypt true to decrypt the data, false to encrypt it.
		 *
		 * @throws IOException If there is an error reading the data.
		 */
        private bool EncryptDataAESother(byte[] readonlyKey, Stream data, Stream output, bool decrypt)
        {
            byte[] iv = new byte[16];

            if (!PrepareAESInitializationVector(decrypt, iv, data, output))
            {
                return false;
            }

            try
            {
                using (var cipher = CreateCipher(readonlyKey, iv))
                using (var decryptCipher = decrypt ? cipher.CreateDecryptor() : cipher.CreateEncryptor())
                {
                    byte[] buffer = new byte[decryptCipher.InputBlockSize];
                    byte[] dst = new byte[decryptCipher.OutputBlockSize];
                    int n;
                    while ((n = data.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        var len = decryptCipher.TransformBlock(buffer, 0, n, dst, 0);
                        if (len > 0)
                        {
                            output.Write(dst, 0, len);
                        }
                    }
                    output.Write(decryptCipher.TransformFinalBlock(Array.Empty<byte>(), 0, 0));
                }
                return true;
            }
            catch (Exception exception)
            {
                if (!(exception is CryptographicException))
                {
                    throw exception;
                }
                Debug.WriteLine("debug: A CryptographicException occurred when decrypting some stream data " + exception);
            }
            return false;
        }

        /**
		 * Encrypt or decrypt data with AES256.
		 *
		 * @param data The data to encrypt.
		 * @param output The output to write the encrypted data to.
		 * @param decrypt true to decrypt the data, false to encrypt it.
		 *
		 * @throws IOException If there is an error reading the data.
		 */
        private bool EncryptDataAES256(Stream data, Stream output, bool decrypt)
        {
            byte[] iv = new byte[16];

            if (!PrepareAESInitializationVector(decrypt, iv, data, output))
            {
                return false;
            }

            try
            {
                using (var cipher = CreateCipher(this.encryptionKey, iv))
                {
                    //IOUtils.copy(cis, output);
                    if (decrypt)
                    {
                        using (var decryptCipher = cipher.CreateDecryptor())
                        using (CryptoStream reader = new CryptoStream(data, decryptCipher, CryptoStreamMode.Read))
                        {
                            reader.CopyTo(output);
                        }
                    }
                    else
                    {
                        using (var decryptCipher = cipher.CreateEncryptor())
                        using (CryptoStream writer = new CryptoStream(output, decryptCipher, CryptoStreamMode.Write))
                        {
                            data.CopyTo(writer);
                        }
                    }
                }
                return true;
            }
            catch (Exception exception)
            {
                // starting with java 8 the JVM wraps an IOException around a GeneralSecurityException
                // it should be safe to swallow a GeneralSecurityException
                if (!(exception is CryptographicException))
                {
                    throw exception;
                }
                Debug.WriteLine("debug: A CryptographicException occurred when decrypting some stream data " + exception);
            }
            return false;
        }

        private SymmetricAlgorithm CreateCipher(byte[] key, byte[] iv)
        {
            //@SuppressWarnings({ "squid:S4432"}) // PKCS#5 padding is requested by PDF specification

            var cipher = new RijndaelManaged();
            cipher.Mode = CipherMode.CBC;
            cipher.Padding = PaddingMode.PKCS7;
            cipher.Key = key;
            cipher.IV = iv;
            //VS
            //Key keySpec = new SecretKeySpec(key, "AES");
            //IvParameterSpec ips = new IvParameterSpec(iv);
            //cipher.init(decrypt ? Cipher.DECRYPT_MODE : Cipher.ENCRYPT_MODE, keySpec, ips);
            return cipher;
        }

        private bool PrepareAESInitializationVector(bool decrypt, byte[] iv, Stream data, Stream output)
        {
            if (decrypt)
            {
                // read IV from stream
                int ivSize = data.Read(iv, 0, iv.Length);
                if (ivSize <= 0)
                {
                    return false;
                }
                if (ivSize != iv.Length)
                {
                    throw new IOException(
                            "AES initialization vector not fully read: only "
                                    + ivSize + " bytes read instead of " + iv.Length);
                }
            }
            else
            {
                // generate random IV and write to stream
                SecureRandom rnd = new SecureRandom();
                rnd.NextBytes(iv);
                output.Write(iv);
            }
            return true;
        }

        /**
		 * This will dispatch to the correct method.
		 *
		 * @param obj The object to decrypt.
		 * @param objNum The object number.
		 * @param genNum The object generation Number.
		 *
		 * @throws IOException If there is an error getting the stream data.
		 */
        public void Decrypt(PdfObject obj, long objNum, long genNum)
        {
            // PDFBOX-4477: only cache strings and streams, this improves speed and memory footprint
            if (obj is PdfString pdfString)
            {
                if (objects.Contains(obj))
                {
                    return;
                }
                objects.Add(obj);
                DecryptString(pdfString, objNum, genNum);
            }
            else if (obj is PdfStream stream)
            {
                if (objects.Contains(obj))
                {
                    return;
                }
                objects.Add(obj);
                DecryptStream(stream, objNum, genNum);
            }
            else if (obj is PdfDictionary dictionary)
            {
                DecryptDictionary(dictionary, objNum, genNum);
            }
            else if (obj is PdfArray array)
            {
                DecryptArray(array, objNum, genNum);
            }            
        }

        /**
		 * This will decrypt a stream.
		 *
		 * @param stream The stream to decrypt.
		 * @param objNum The object number.
		 * @param genNum The object generation number.
		 *
		 * @throws IOException If there is an error getting the stream data.
		 */
        public void DecryptStream(PdfStream stream, long objNum, long genNum)
        {
            if (stream.encoded == EncodeState.Decoded)
            {
                return;
            }
            stream.encoded = EncodeState.Encoded;
            // Stream encrypted with identity filter
            if (PdfName.Identity.Equals(streamFilterName))
            {
                stream.encoded = EncodeState.Identity;
                return;
            }

            var type = stream.Header.Resolve(PdfName.Type);
            if (!decryptMetadata && PdfName.Metadata.Equals(type))
            {
                stream.encoded = EncodeState.SkipMetadata;
                return;
            }
            // "The cross-reference stream shall not be encrypted"
            if (PdfName.XRef.Equals(type))
            {
                stream.encoded = EncodeState.SkipXRef;
                return;
            }
            if (PdfName.Metadata.Equals(type))
            {
                byte[] buf;
                // PDFBOX-3229 check case where metadata is not encrypted despite /EncryptMetadata missing
                var metadata = stream.GetBody(false);
                using (var istream = new MemoryStream(metadata.GetBuffer(), 0, (int)metadata.Length))
                {
                    buf = new byte[10];
                    long isResult = istream.Read(buf, 0, 10);

                    if (isResult.CompareTo(buf.Length) != 0)
                    {
                        Debug.WriteLine("debug: Tried reading " + buf.Length + " bytes but only " + isResult + " bytes read");
                    }
                }
                if (buf.AsSpan().SequenceEqual(Charset.ISO88591.GetBytes("<?xpacket ").AsSpan()))
                {
                    Debug.WriteLine("warn: Metadata is not encrypted, but was expected to be");
                    Debug.WriteLine("warn: Read PDF specification about EncryptMetadata (default value: true)");
                    return;
                }
            }

            DecryptDictionary(stream.Header, objNum, genNum);
            var body = stream.GetBody(false);
            using (var encryptedStream = new MemoryStream(body.GetBuffer(), 0, (int)body.Length))
            using (var output = new MemoryStream())
            {
                stream.encoded = EncodeState.Encoded;
                if (EncryptData(objNum, genNum, encryptedStream, output, true /* decrypt */))
                {
                    stream.GetBody(false).SetBuffer(output.ToArray());
                    stream.encoded = EncodeState.Decoded;
                }
            }
        }

        /**
		 * This will encrypt a stream, but not the dictionary as the dictionary is
		 * encrypted by visitFromString() in PdfWriter and we don't want to encrypt
		 * it twice.
		 *
		 * @param stream The stream to decrypt.
		 * @param objNum The object number.
		 * @param genNum The object generation number.
		 *
		 * @throws IOException If there is an error getting the stream data.
		 */
        public void EncryptStream(PdfStream stream, long objNum, int genNum)
        {
            var body = stream.GetBody(false);
            using (var encryptedStream = new MemoryStream(body.GetBuffer(), 0, (int)body.Length))
            using (var output = new MemoryStream())
            {
                if (EncryptData(objNum, genNum, encryptedStream, output, false /* encrypt */))
                {
                    stream.GetBody(false).SetBuffer(output.ToArray());
                }
            }
        }

        /**
		 * This will decrypt a dictionary.
		 *
		 * @param dictionary The dictionary to decrypt.
		 * @param objNum The object number.
		 * @param genNum The object generation number.
		 *
		 * @throws IOException If there is an error creating a new string.
		 */
        private void DecryptDictionary(PdfDictionary dictionary, long objNum, long genNum)
        {
            if (dictionary[PdfName.CF] != null)
            {
                // PDFBOX-2936: avoid orphan /CF dictionaries found in US govt "I-" files
                return;
            }
            var type = dictionary.Resolve(PdfName.Type);
            bool isSignature = PdfName.Sig.Equals(type) || PdfName.DocTimeStamp.Equals(type) ||
                    // PDFBOX-4466: /Type is optional, see
                    // https://ec.europa.eu/cefdigital/tracker/browse/DSS-1538
                    (dictionary.Resolve(PdfName.Contents) is PdfString &&
                     dictionary.Resolve(PdfName.ByteRange) is PdfArray);
            foreach (var entry in dictionary)
            {
                if (isSignature && PdfName.Contents.Equals(entry.Key))
                {
                    // do not decrypt the signature contents string
                    continue;
                }
                var value = entry.Value;
                Decrypt(value, objNum, genNum);
            }
        }

        /**
		 * This will decrypt a string.
		 *
		 * @param string the string to decrypt.
		 * @param objNum The object number.
		 * @param genNum The object generation number.
		 *
		 * @throws IOException If an error occurs writing the new string.
		 */
        private void DecryptString(PdfString pdfString, long objNum, long genNum)
        {
            // String encrypted with identity filter
            if (PdfName.Identity.Equals(stringFilterName))
            {
                return;
            }

            using (var data = new MemoryStream(pdfString.GetBuffer()))
            using (var outputStream = new MemoryStream())
            {
                try
                {
                    if (EncryptData(objNum, genNum, data, outputStream, true /* decrypt */))
                    {
                        pdfString.SetBuffer(outputStream.ToArray());
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"error: Failed to decrypt PdfString of Length {pdfString.GetBuffer().Length} in object {objNum}: {ex.Message}", ex);
                }
            }
        }

        /**
		 * This will encrypt a string.
		 *
		 * @param string the string to encrypt.
		 * @param objNum The object number.
		 * @param genNum The object generation number.
		 *
		 * @throws IOException If an error occurs writing the new string.
		 */
        public void EncryptString(PdfString pdfString, long objNum, int genNum)
        {
            using (var data = new MemoryStream(pdfString.GetBuffer()))
            using (var buffer = new MemoryStream())
            {
                if (EncryptData(objNum, genNum, data, buffer, false /* encrypt */))
                {
                    pdfString.SetBuffer(buffer.GetBuffer());
                }
            }
        }

        /**
		 * This will decrypt an array.
		 *
		 * @param array The array to decrypt.
		 * @param objNum The object number.
		 * @param genNum The object generation number.
		 *
		 * @throws IOException If there is an error accessing the data.
		 */
        private void DecryptArray(PdfArray array, long objNum, long genNum)
        {
            for (int i = 0; i < array.Count; i++)
            {
                Decrypt(array[i], objNum, genNum);
            }
        }

        /**
		 * Getter of the property <tt>keyLength</tt>.
		 * @return  Returns the keyLength.
		 */
        /**
		 * Setter of the property <tt>keyLength</tt>.
		 *
		 * @param keyLen  The keyLength to set.
		 */
        public int KeyLength
        {
            get => keyLength;
            set => keyLength = value;
        }

        /**
		 * Returns the access permissions that were computed during document decryption.
		 * The returned object is in read only mode.
		 *
		 * @return the access permissions or null if the document was not decrypted.
		 */
        /**
		 * Sets the access permissions.
		 *
		 * @param currentAccessPermission The access permissions to be set.
		 */
        public AccessPermission CurrentAccessPermission
        {
            get => currentAccessPermission;
            set => currentAccessPermission = value;
        }


        /**
		 * True if AES is used for encryption and decryption.
		 *
		 * @return true if AEs is used
		 */
        /**
		* Set to true if AES for encryption and decryption should be used.
		*
		* @param aesValue if true AES will be used
		*
		*/
        public bool IsAES
        {
            get => useAES;
            set => useAES = value;
        }

        /**
		 * Returns whether a protection policy has been set.
		 *
		 * @return true if a protection policy has been set.
		 */
        public abstract bool HasProtectionPolicy();
    }
}