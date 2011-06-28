using System;
using System.Text;
using System.Security.Cryptography;

namespace MindTouch.Deki.Services.Mtps.Aws {
    public sealed class AwsSignature {

        //--- Class Fields ---
        private const string HASH_METHOD = "HmacSHA1";

        //--- Fields ---
        private readonly string _privateKey;

        //--- Constructors ---
        public AwsSignature(string privateKey) {
            _privateKey = privateKey;
        }

        //--- Properties ---
        public static string HashMethod {
            get { return HASH_METHOD; }
        }

        //--- Methods ---
        public string GetSignature(string request) {
            var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(_privateKey));
            return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(request)));
        }
    }
}