// // 
// //  Copyright 2012 David Gressett
// // 
// //    Licensed under the Apache License, Version 2.0 (the "License");
// //    you may not use this file except in compliance with the License.
// //    You may obtain a copy of the License at
// // 
// //        http://www.apache.org/licenses/LICENSE-2.0
// // 
// //    Unless required by applicable law or agreed to in writing, software
// //    distributed under the License is distributed on an "AS IS" BASIS,
// //    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// //    See the License for the specific language governing permissions and
// //    limitations under the License.

using System;
using System.Diagnostics.Contracts;

namespace AppComponents
{
    using System.Web;

    public class AuthorizationCode
    {
        private const string _magic = "&%@!";

        private static string _pwd;

        private string _code;

        static AuthorizationCode()
        {
            var config = Catalog.Factory.Resolve<IConfig>();
            _pwd = config.Get(CommonConfiguration.AuthCodeSecret, string.Empty);
        }

        public DateTime ExpirationTime { get; set; }

        public string Principal { get; set; }

        public string EmailedTo { get; set; }

        public string Referent { get; set; }

        public string Tenant { get; set; }

        [DocumentIdentifier]
        public virtual string Code
        {
            get
            {
                return this._code ?? (this._code = GenerateCode());
            }

            set
            {
                _code = value;
            }
        }

        public string UrlEncodedCode
        {
            get
            {
                return UrlEncode(Code);
            }
        }

        public static string UrlEncode(string code)
        {
            return HttpUtility.UrlEncode(code.Replace('=', '!').Replace('/', '_').Replace('+', '-'));
        }

        public static string UrlDecode(string code)
        {
            return HttpUtility.HtmlDecode(code.Replace('!', '=').Replace('_', '/').Replace('-', '+'));
        }

        public static string GenerateCode()
        {
            var plainText = string.Format("{0}|{1}", GuidEncoder.Encode(Guid.NewGuid()), _magic);
            return GenerateCode(plainText);
        }

        //the way a code is encrypted may be changed by class inheritors
        //but adding the _magic is unique using the | separator and _magic
        protected static string GenerateCode(string plainText)
        {
            var log = ClassLogger.Create(typeof(AuthorizationCode));
            var dblog = DebugOnlyLogger.Create(log);

            var cryptText = PWCrypto.Encrypt(plainText, _pwd);

            dblog.InfoFormat(GeneratedCodeMsgFormat, plainText);

            return cryptText;
        }

        private const string AuthCodeFailedFormat = "Authcode authorization failed for {0}";
        private const string GeneratedCodeMsgFormat = "Generated authcode for {0}";
        private const string AuthCodeAuthorizedFormat = "Authcode {0} authorized";
        private const string DecryptedAuthCodeFormat = "Decrypted authcode: {0}";

        private const char Separator = '|';
       
        //validates an auth code
        public static bool IsGoodAuthCode(string authCode)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(authCode));

            var log = ClassLogger.Create(typeof(AuthorizationCode));
            var dblog = DebugOnlyLogger.Create(log);
            try
            {
                var plain = DecryptCode(authCode);

                dblog.InfoFormat(DecryptedAuthCodeFormat, plain);

                var parts = plain.Split(Separator);
                if (parts.Length != 2)
                {
                    log.ErrorFormat(AuthCodeFailedFormat, plain);
                    return false;
                }

                var goodGuid = GuidEncoder.Decode(parts[0]);
                if (Guid.Empty != goodGuid && parts[1].Contains(_magic))
                {
                    dblog.InfoFormat(AuthCodeAuthorizedFormat, plain);
                    return true;
                }
                
                //when not in right format
                log.ErrorFormat(AuthCodeFailedFormat, plain);
                return false;
            }
            catch (Exception exception)
            {
                log.Error("IsGoodAuthCode", exception);
                log.ErrorFormat(AuthCodeFailedFormat, authCode);
                return false;
            }
        }

        //decrypt a code, this method may be overriten by inheritors with other ways to decrypt
        protected static string DecryptCode(string authCode)
        {
            var plain = PWCrypto.Decrypt(authCode, _pwd);
            return plain;
        }
    }

    public sealed class AuthCodeException : ApplicationException
    {
        public AuthCodeException()
        {
        }

        public AuthCodeException(string msg)
            : base(msg)
        {
        }
    }
}