/*
 * MindTouch Dream - a distributed REST framework 
 * Copyright (C) 2006-2011 MindTouch, Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit wiki.developer.mindtouch.com;
 * please review the licensing section.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;

namespace MindTouch.Security.Cryptography {

    /// <summary>
    /// Static utility class containing factory methods for creation <see cref="RSACryptoServiceProvider"/> instances.
    /// </summary>
    public static class RSAUtil {

        //--- Constants ---
        private const byte PUBLIC_KEYBLOB = 0x06;
        private const byte PRIVATE_KEYBLOB = 0x07;
        private const byte BLOB_VERSION = 0x02;
        private const uint CALG_RSA_KEYX = 0x0000a400;
        private const uint CALG_RSA_SIGN = 0x00002400;
        private const uint RSA1 = 0x31415352;  // "RSA1"
        private const uint RSA2 = 0x32415352;  // "RSA2"

        /// <summary>
        /// Create a <see cref="RSACryptoServiceProvider"/>.
        /// </summary>
        /// <param name="path">Path to the file containing the provider data.</param>
        /// <returns>New Crypto service Provider.</returns>
        public static RSACryptoServiceProvider ProviderFromFile(string path) {
            if(path == null) {
                throw new ArgumentNullException("path");
            }
            using(var fs = new FileStream(path, FileMode.Open, FileAccess.Read)) {
                return ProviderFrom(new BinaryReader(fs));
            }
        }

        /// <summary>
        /// Create a <see cref="RSACryptoServiceProvider"/>.
        /// </summary>
        /// <param name="bytes">Data block to create provider from.</param>
        /// <returns>New Crypto service Provider.</returns>
        public static RSACryptoServiceProvider ProviderFrom(byte[] bytes) {
            if((bytes == null) || (bytes.Length == 0)) {
                return null;
            }
            using(var reader = new BinaryReader(new MemoryStream(bytes))) {
                return ProviderFrom(reader);
            }
        }

        /// <summary>
        /// Create a <see cref="RSACryptoServiceProvider"/>.
        /// </summary>
        /// <param name="assembly">Assembly, whose public key is to be used to create a provider.</param>
        /// <returns>New Crypto service Provider.</returns>
        public static RSACryptoServiceProvider ProviderFrom(Assembly assembly) {
            if(assembly == null) {
                throw new ArgumentNullException("assembly");
            }
            return ProviderFrom(assembly.GetName().GetPublicKey());
        }

        /// <summary>
        /// Create a <see cref="RSACryptoServiceProvider"/>.
        /// </summary>
        /// <param name="reader">Reader containing provider data.</param>
        /// <returns>New Crypto service Provider.</returns>
        public static RSACryptoServiceProvider ProviderFrom(BinaryReader reader) {

            // check if file is a public/private key blob
            byte blobType = reader.ReadByte();
            if((blobType != PUBLIC_KEYBLOB) && (blobType != PRIVATE_KEYBLOB)) {
                reader.BaseStream.Seek(11, SeekOrigin.Current);
                blobType = reader.ReadByte();
                if((blobType != PUBLIC_KEYBLOB) && (blobType != PRIVATE_KEYBLOB)) {
                    return null;
                }
            }

            // validate header values
            if(reader.ReadByte() != BLOB_VERSION) {
                return null;
            }
            if(reader.ReadUInt16() != 0) {
                return null;
            }
            uint algorithm = reader.ReadUInt32();
            if(algorithm != CALG_RSA_KEYX && algorithm != CALG_RSA_SIGN) {
                return null;
            }

            // check if key is either of RSA1 or RSA2
            uint magic = reader.ReadUInt32();
            if((magic != RSA1) && (magic != RSA2)) {
                return null;
            }

            // read key length
            int keyLength = reader.ReadInt32();

            // initialize RSA parameters
            var param = new RSAParameters();

            // read RSA public exponent
            uint exponentLength = reader.ReadUInt32();
            param.Exponent = new byte[4];
            param.Exponent[0] = (byte)(exponentLength >> 0);
            param.Exponent[1] = (byte)(exponentLength >> 8);
            param.Exponent[2] = (byte)(exponentLength >> 16);
            param.Exponent[3] = (byte)(exponentLength >> 24);
            Array.Reverse(param.Exponent);

            // read RSA modulus
            param.Modulus = ReadBytes(reader, keyLength / 8);

            // check if this is a valid unencrypted PRIVATEKEYBLOB
            if(blobType == PRIVATE_KEYBLOB) {

                // read RSA private key properties
                int bitlen16 = keyLength / 16;
                param.P = ReadBytes(reader, bitlen16);
                param.Q = ReadBytes(reader, bitlen16);
                param.DP = ReadBytes(reader, bitlen16);
                param.DQ = ReadBytes(reader, bitlen16);
                param.InverseQ = ReadBytes(reader, bitlen16);
                param.D = ReadBytes(reader, keyLength / 8);
            }

            // initialize rsa crypto provider
            var result = new RSACryptoServiceProvider();
            result.ImportParameters(param);
            return result;
        }

        private static byte[] ReadBytes(BinaryReader reader, int length) {

            // read and verify bytes
            byte[] result = reader.ReadBytes(length);
            if(result.Length != length) {
                throw new EndOfStreamException();
            }

            // reverse result
            Array.Reverse(result);
            return result;
        }
    }
}
