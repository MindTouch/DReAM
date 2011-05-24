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
using System.Security.Cryptography;

namespace MindTouch.Security.Cryptography {

    /// <summary>
    /// Static utility class containing extension methods related to Cryptography.
    /// </summary>
    public static class CryptoEx {

        //--- Extension Methods ---

        /// <summary>
        /// Verify a data payload against a signature.
        /// </summary>
        /// <param name="data">Data to be verified.</param>
        /// <param name="signature">Signature string.</param>
        /// <param name="rsa">The Crypto Service Provider to be used.</param>
        /// <returns><see langword="True"/> if the data is signed by the signature.</returns>
       public static bool VerifySignature(this byte[] data, string signature, RSACryptoServiceProvider rsa) {
            if(data == null) {
                throw new ArgumentNullException("data");
            }
            if(signature == null) {
                throw new ArgumentNullException("signature");
            }
            if(rsa == null) {
                throw new ArgumentNullException("rsa");
            }

            // verify signature type
            if(!signature.StartsWithInvariant("rsa-sha1:")) {
                throw new ArgumentException("unsupported signature type");
            }

            // verify signature
            return rsa.VerifyData(data, "SHA1", Convert.FromBase64String(signature.Substring(9)));
        }

        /// <summary>
        /// Create a crytographic signature for a block of data.
        /// </summary>
        /// <param name="data">The data to be signed.</param>
        /// <param name="rsa">The Crypto Service Provider to be used.</param>
        /// <returns>The signature string for the data block.</returns>
        public static string SignData(this byte[] data, RSACryptoServiceProvider rsa) {
            if(data == null) {
                throw new ArgumentNullException("data");
            }
            if(rsa == null) {
                throw new ArgumentNullException("rsa");
            }

            // sign data and prepend signature type
            return "rsa-sha1:" + Convert.ToBase64String(rsa.SignData(data, "SHA1"));
        }
    }
}
