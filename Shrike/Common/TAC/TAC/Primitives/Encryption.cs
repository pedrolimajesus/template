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
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using Newtonsoft.Json;

namespace AppComponents
{
    /// <summary>
    ///   constructed using reference sample at http://www.obviex.com/samples/Encryption.aspx
    /// </summary>
    public static class PWCrypto
    {
        private const string _salt = "t#4wcz9eef3";
        private static string _alg = "SHA1";
        private static int _pwiter = 7;
        private static string _iv = "@9EFnnjc2940c8b6";
        private static int _keyLength = 256;


        public static string Encrypt(string plainText, string passPhrase)
        {
            Contract.Requires(!string.IsNullOrEmpty(plainText));
            Contract.Requires(!string.IsNullOrWhiteSpace(passPhrase));
            Contract.Requires(passPhrase.Length > 4);

            byte[] initVectorBytes = Encoding.ASCII.GetBytes(_iv);
            byte[] saltValueBytes = Encoding.ASCII.GetBytes(_salt);


            byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);


            PasswordDeriveBytes password = new PasswordDeriveBytes(
                passPhrase,
                saltValueBytes,
                _alg,
                _pwiter);


            byte[] keyBytes = password.GetBytes(_keyLength/8);


            RijndaelManaged symmetricKey = new RijndaelManaged();


            symmetricKey.Mode = CipherMode.CBC;


            ICryptoTransform encryptor = symmetricKey.CreateEncryptor(
                keyBytes,
                initVectorBytes);


            MemoryStream memoryStream = new MemoryStream();


            CryptoStream cryptoStream = new CryptoStream(memoryStream,
                                                         encryptor,
                                                         CryptoStreamMode.Write);
            byte[] cipherTextBytes;
            try
            {
                cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);


                cryptoStream.FlushFinalBlock();


                cipherTextBytes = memoryStream.ToArray();
            }
            finally
            {
                memoryStream.Close();
                cryptoStream.Close();
            }


            string cipherText = Convert.ToBase64String(cipherTextBytes);

            return cipherText;
        }


        public static string Decrypt(string cipherText, string passPhrase)
        {
            Contract.Requires(!string.IsNullOrEmpty(cipherText));
            Contract.Requires(!string.IsNullOrWhiteSpace(passPhrase));
            Contract.Requires(passPhrase.Length > 4);

            byte[] initVectorBytes = Encoding.ASCII.GetBytes(_iv);
            byte[] saltValueBytes = Encoding.ASCII.GetBytes(_salt);

            byte[] cipherTextBytes = Convert.FromBase64String(cipherText);


            PasswordDeriveBytes password = new PasswordDeriveBytes(
                passPhrase,
                saltValueBytes,
                _alg,
                _pwiter);

            byte[] keyBytes = password.GetBytes(_keyLength/8);
            RijndaelManaged symmetricKey = new RijndaelManaged();

            symmetricKey.Mode = CipherMode.CBC;

            ICryptoTransform decryptor = symmetricKey.CreateDecryptor(
                keyBytes,
                initVectorBytes);

            MemoryStream memoryStream = new MemoryStream(cipherTextBytes);

            CryptoStream cryptoStream = new CryptoStream(memoryStream,
                                                         decryptor,
                                                         CryptoStreamMode.Read);
            byte[] plainTextBytes;
            int decryptedByteCount;
            try
            {
                plainTextBytes = new byte[cipherTextBytes.Length];
                decryptedByteCount = cryptoStream.Read(plainTextBytes,
                                                       0,
                                                       plainTextBytes.Length);
            }
            finally
            {
                memoryStream.Close();
                cryptoStream.Close();
            }


            string plainText = Encoding.UTF8.GetString(plainTextBytes,
                                                       0,
                                                       decryptedByteCount);

            return plainText;
        }
    }


    public static class EncryptedObject<T>
    {
        public static string EncryptSerialize(T token, string pass)
        {
            Contract.Requires(null != token);
            Contract.Requires(!string.IsNullOrWhiteSpace(pass));

            var log = ClassLogger.Create(typeof (EncryptedObject<T>));


            string s = JsonConvert.SerializeObject(token);
            log.InfoFormat("Securing object {0}", s);
            string c = PWCrypto.Encrypt(s, pass);
            return c;
        }


        public static T DecryptDeserialize(string t, string pass)
        {
            Contract.Requires(!string.IsNullOrEmpty(t));
            Contract.Requires(!string.IsNullOrWhiteSpace(pass));

            var log = ClassLogger.Create(typeof (EncryptedObject<T>));

            string p = PWCrypto.Decrypt(t, pass);
            log.InfoFormat("Decrypting object {0}", p);
            T token = JsonConvert.DeserializeObject<T>(p);
            return token;
        }
    }


    public class RSACrypto : IDisposable
    {
        private RSAConfig _rsaConfig;
        private RNGCryptoServiceProvider _randomGen = new RNGCryptoServiceProvider();

        
        public RSACrypto(RSAConfig rsaConfig)
        {
            this._rsaConfig = rsaConfig;
            FastPrivateDecrypt = true;
        }

      
        public RSACrypto(String keyInfo, int ModulusSize)
        {
            this._rsaConfig = RSAHelper.GetRSAConfig(keyInfo, ModulusSize);
            FastPrivateDecrypt = true;
        }

      
        public RSAConfig.HashAlgorithmKinds Hasher
        {
            set
            {
                _rsaConfig.HashAlg = value;
            }
        }

        public bool FastPrivateDecrypt
        {
            get;
            set;
        }

       
        public void Dispose()
        {
            _rsaConfig.Dispose();
        }

       
        public byte[] CrunchRSA(byte[] plainText, bool usePrivate)
        {

            if (usePrivate && (!_rsaConfig.Has_PRIVATE_Info))
            {
                throw new CryptographicException("No private key");
            }

            if ((usePrivate == false) && (!_rsaConfig.Has_PUBLIC_Info))
            {
                throw new CryptographicException("No public key");
            }

            BigInteger e;
            if (usePrivate)
                e = _rsaConfig.D;
            else
                e = _rsaConfig.E;

            var PT = RSAHelper.OS2IP(plainText, false);
            var M = BigInteger.ModPow(PT, e, _rsaConfig.N);

            return M.Sign == -1 ? 
                RSAHelper.I2OSP(M + _rsaConfig.N, _rsaConfig.OctetsInModulus, false) : 
                RSAHelper.I2OSP(M, _rsaConfig.OctetsInModulus, false);
        }

       
        public byte[] PrivateDecryptFast(byte[] data)
        {
            if (!_rsaConfig.Has_PRIVATE_Info || !_rsaConfig.HasCRTInfo)
            {
                throw new CryptographicException("No private key");
            }
            else
            {
                var c = RSAHelper.OS2IP(data, false);

                var m1 = BigInteger.ModPow(c, _rsaConfig.DP, _rsaConfig.P);
                var m2 = BigInteger.ModPow(c, _rsaConfig.DQ, _rsaConfig.Q);
                var h = ((m1 - m2)*_rsaConfig.InverseQ)%_rsaConfig.P;
                var m = (m2 + (_rsaConfig.Q*h));

                return m.Sign == -1 ? 
                    RSAHelper.I2OSP(m + _rsaConfig.N, _rsaConfig.OctetsInModulus, false) 
                    : RSAHelper.I2OSP(m, _rsaConfig.OctetsInModulus, false);
            }
        }

        private byte[] EncodePKCS(byte[] data, bool usePrivate)
        {
            if (data.Length > _rsaConfig.OctetsInModulus - 11)
            {
                throw new ArgumentException("data too long.");
            }
            else
            {
                // RFC3447 : Page 24. [RSAES-PKCS1-V1_5-ENCRYPT ((n, e), M)]
                // EM = 0x00 || 0x02 || PS || 0x00 || Msg 

                List<byte> PCKSv15_Msg = new List<byte>();

                PCKSv15_Msg.Add(0x00);
                PCKSv15_Msg.Add(0x02);

                int PaddingLength = _rsaConfig.OctetsInModulus - data.Length - 3;

                byte[] PS = new byte[PaddingLength];
                _randomGen.GetNonZeroBytes(PS);

                PCKSv15_Msg.AddRange(PS);
                PCKSv15_Msg.Add(0x00);

                PCKSv15_Msg.AddRange(data);

                return CrunchRSA(PCKSv15_Msg.ToArray(), usePrivate);
            }
        }

      
        private byte[] GenerateMask(byte[] seed, int outputLength)
        {
            if (outputLength > (Math.Pow(2, 32)))
            {
                throw new ArgumentException("Mask cannot exceed 2^32.");
            }
            else
            {
                var result = new List<byte>();
                for (var i = 0; i <= outputLength / _rsaConfig.hLen; i++)
                {
                    var data = new List<byte>();
                    data.AddRange(seed);
                    data.AddRange(RSAHelper.I2OSP(i, 4, false));
                    result.AddRange(_rsaConfig.ComputeHash(data.ToArray()));
                }

                if (outputLength <= result.Count)
                {
                    return result.GetRange(0, outputLength).ToArray();
                }
                else
                {
                    throw new ArgumentException("mask length invalid.");
                }
            }
        }


        private byte[] EncodeOAEP(byte[] message, byte[] p, bool usePrivate)
        {
            

            var mLen = message.Length;
            if (mLen > _rsaConfig.OctetsInModulus - 2 * _rsaConfig.hLen - 2)
            {
                throw new ArgumentException("message too long.");
            }
            else
            {
                var ps = new byte[_rsaConfig.OctetsInModulus - mLen - 2 * _rsaConfig.hLen - 2];
                
                var pHash = _rsaConfig.ComputeHash(p);

               
                var db = new List<byte>();
                db.AddRange(pHash);
                db.AddRange(ps);
                db.Add(0x01);
                db.AddRange(message);
                var dbArr = db.ToArray();

                           
                var seed = new byte[_rsaConfig.hLen];
                _randomGen.GetBytes(seed);

              
                var dbMask = GenerateMask(seed, _rsaConfig.OctetsInModulus - _rsaConfig.hLen - 1);
                var maskedDB = RSAHelper.XOR(dbArr, dbMask);
                var seedMask = GenerateMask(maskedDB, _rsaConfig.hLen);
                var maskedSeed = RSAHelper.XOR(seed, seedMask);
                
                var result = new List<byte>();
                result.Add(0x00);
                result.AddRange(maskedSeed);
                result.AddRange(maskedDB);

                return CrunchRSA(result.ToArray(), usePrivate);
            }
        }


        private byte[] Decrypt(byte[] message, byte[] parameters, bool usePrivate, bool fOAEP)
        {
            var EM = new byte[0];
            try
            {
                if ((usePrivate == true) && (FastPrivateDecrypt) && (_rsaConfig.HasCRTInfo))
                {
                    EM = PrivateDecryptFast(message);
                }
                else
                {
                    EM = CrunchRSA(message, usePrivate);
                }
            }
            catch (CryptographicException ex)
            {
                throw new CryptographicException("Exception while Decryption: " + ex.Message);
            }
            catch
            {
                throw new Exception("Exception while Decryption: ");
            }

            try
            {
                if (fOAEP) 
                {
                    if ((EM.Length == _rsaConfig.OctetsInModulus) && (EM.Length > (2 * _rsaConfig.hLen + 1)))
                    {
                        byte[] maskedSeed;
                        byte[] maskedDB;
                        byte[] pHash = _rsaConfig.ComputeHash(parameters);
                        if (EM[0] == 0) 
                        {
                            maskedSeed = EM.ToList().GetRange(1, _rsaConfig.hLen).ToArray();
                            maskedDB = EM.ToList().GetRange(1 + _rsaConfig.hLen, EM.Length - _rsaConfig.hLen - 1).ToArray();
                            var seedMask = GenerateMask(maskedDB, _rsaConfig.hLen);
                            var seed = RSAHelper.XOR(maskedSeed, seedMask);
                            var dbMask = GenerateMask(seed, _rsaConfig.OctetsInModulus - _rsaConfig.hLen - 1);
                            var DB = RSAHelper.XOR(maskedDB, dbMask);

                            if (DB.Length >= (_rsaConfig.hLen + 1))
                            {
                                var _pHash = DB.ToList().GetRange(0, _rsaConfig.hLen).ToArray();
                                var PS_M = DB.ToList().GetRange(_rsaConfig.hLen, DB.Length - _rsaConfig.hLen);
                                var pos = PS_M.IndexOf(0x01);
                                if (pos >= 0 && (pos < PS_M.Count))
                                {
                                    var _01_M = PS_M.GetRange(pos, PS_M.Count - pos);
                                    byte[] M;
                                    if (_01_M.Count > 1)
                                    {
                                        M = _01_M.GetRange(1, _01_M.Count - 1).ToArray();
                                    }
                                    else
                                    {
                                        M = new byte[0];
                                    }
                                    var success = true;
                                    for (var i = 0; i < _rsaConfig.hLen; i++)
                                    {
                                        if (_pHash[i] != pHash[i])
                                        {
                                            success = false;
                                            break;
                                        }
                                    }

                                    if (success)
                                    {
                                        return M;
                                    }
                                    else
                                    {
                                        M = new byte[_rsaConfig.OctetsInModulus]; 
                                        throw new CryptographicException("OAEP Decode Error");
                                    }
                                }
                                else
                                {
                                    throw new CryptographicException("OAEP Decode Error");
                                }
                            }
                            else
                            {
                                throw new CryptographicException("OAEP Decode Error");
                            }
                        }
                        else 
                        {
                            throw new CryptographicException("OAEP Decode Error");
                        }
                    }
                    else
                    {
                        throw new CryptographicException("OAEP Decode Error");
                    }
                }
                else 
                {
                    if (EM.Length >= 11)
                    {
                        if ((EM[0] == 0x00) && (EM[1] == 0x02))
                        {
                            var startIndex = 2;
                            var PS = new List<byte>();
                            for (var i = startIndex; i < EM.Length; i++)
                            {
                                if (EM[i] != 0)
                                {
                                    PS.Add(EM[i]);
                                }
                                else
                                {
                                    break;
                                }
                            }

                            if (PS.Count >= 8)
                            {
                                var DecodedDataIndex = startIndex + PS.Count + 1;
                                if (DecodedDataIndex < (EM.Length - 1))
                                {
                                    var DATA = new List<byte>();
                                    for (int i = DecodedDataIndex; i < EM.Length; i++)
                                    {
                                        DATA.Add(EM[i]);
                                    }
                                    return DATA.ToArray();
                                }
                                else
                                {
                                    return new byte[0];
                                   
                                }
                            }
                            else
                            {
                                throw new CryptographicException("PKCS v1.5 Decode Error");
                            }
                        }
                        else
                        {
                            throw new CryptographicException("PKCS v1.5 Decode Error");
                        }
                    }
                    else
                    {
                        throw new CryptographicException("PKCS v1.5 Decode Error");
                    }

                }
            }
            catch (CryptographicException ex)
            {
                throw new CryptographicException("Exception while decoding: " + ex.Message);
            }
            catch
            {
                throw new CryptographicException("Exception while decoding");
            }


        }

       

      
        public byte[] Encrypt(byte[] message, byte[] oaepParams, bool usePrivate)
        {
            return EncodeOAEP(message, oaepParams, usePrivate);
        }

     
        public byte[] Encrypt(byte[] message, bool usePrivate, bool fOAEP)
        {
            if (fOAEP)
            {
                return EncodeOAEP(message, new byte[0], usePrivate);
            }
            else
            {
                return EncodePKCS(message, usePrivate);
            }
        }

     
        public byte[] Encrypt(byte[] message, bool fOAEP)
        {
            if (fOAEP)
            {
                return EncodeOAEP(message, new byte[0], false);
            }
            else
            {
                return EncodePKCS(message, false);
            }
        }

       
        public byte[] Decrypt(byte[] message, bool usePrivate, bool fOAEP)
        {
            return Decrypt(message, new byte[0], usePrivate, fOAEP);
        }

     
        public byte[] Decrypt(byte[] message, byte[] OAEP_Params, bool usePrivate)
        {
            return Decrypt(message, OAEP_Params, usePrivate, true);
        }

     
        public byte[] Decrypt(byte[] message, bool fOAEP)
        {
            return Decrypt(message, new byte[0], true, fOAEP);
        }

    
    }

    public class RSAConfig : IDisposable
    {
        private int _ModulusOctets;
        private BigInteger _N;
        private BigInteger _P;
        private BigInteger _Q;
        private BigInteger _DP;
        private BigInteger _DQ;
        private BigInteger _InverseQ;
        private BigInteger _E;
        private BigInteger _D;
        private HashAlgorithm ha = SHA1Managed.Create();
        private int _hLen = 20;
        private bool _Has_CRT_Info = false;
        private bool _Has_PRIVATE_Info = false;
        private bool _Has_PUBLIC_Info = false;

        public enum HashAlgorithmKinds { SHA1, SHA256, SHA512, UNDEFINED };

        public void Dispose()
        {
            ha.Dispose();
        }

      
        public byte[] ComputeHash(byte[] data)
        {
            return ha.ComputeHash(data);
        }

       
        public HashAlgorithmKinds HashAlg
        {
            get
            {
                var al = HashAlgorithmKinds.UNDEFINED;
                switch (ha.GetType().ToString())
                {
                    case "SHA1":
                        al = HashAlgorithmKinds.SHA1;
                        break;

                    case "SHA256":
                        al = HashAlgorithmKinds.SHA256;
                        break;

                    case "SHA512":
                        al = HashAlgorithmKinds.SHA512;
                        break;
                }
                return al;
            }

            set
            {
                switch (value)
                {
                    case HashAlgorithmKinds.SHA1:
                        ha = SHA1Managed.Create();
                        _hLen = 20;
                        break;

                    case HashAlgorithmKinds.SHA256:
                        ha = SHA256Managed.Create();
                        _hLen = 32;
                        break;

                    case HashAlgorithmKinds.SHA512:
                        ha = SHA512Managed.Create();
                        _hLen = 64;
                        break;
                }
            }
        }

        public bool HasCRTInfo
        {
            get
            {
                return _Has_CRT_Info;
            }
        }

        public bool Has_PRIVATE_Info
        {
            get
            {
                return _Has_PRIVATE_Info;
            }
        }

        public bool Has_PUBLIC_Info
        {
            get
            {
                return _Has_PUBLIC_Info;
            }
        }

        public int OctetsInModulus
        {
            get
            {
                return _ModulusOctets;
            }
        }

        public BigInteger N
        {
            get
            {
                return _N;
            }
        }

        public int hLen
        {
            get
            {
                return _hLen;
            }
        }

        public BigInteger P
        {
            get
            {
                return _P;
            }
        }

        public BigInteger Q
        {
            get
            {
                return _Q;
            }
        }

        public BigInteger DP
        {
            get
            {
                return _DP;
            }
        }

        public BigInteger DQ
        {
            get
            {
                return _DQ;
            }
        }

        public BigInteger InverseQ
        {
            get
            {
                return _InverseQ;
            }
        }

        public BigInteger E
        {
            get
            {
                return _E;
            }
        }

        public BigInteger D
        {
            get
            {
                return _D;
            }
        }

       
        public RSAConfig(RSAParameters rsaParams, int ModulusSize)
        {
            _ModulusOctets = ModulusSize / 8;
            _E = RSAHelper.OS2IP(rsaParams.Exponent, false);
            _D = RSAHelper.OS2IP(rsaParams.D, false);
            _N = RSAHelper.OS2IP(rsaParams.Modulus, false);
            _P = RSAHelper.OS2IP(rsaParams.P, false);
            _Q = RSAHelper.OS2IP(rsaParams.Q, false);
            _DP = RSAHelper.OS2IP(rsaParams.DP, false);
            _DQ = RSAHelper.OS2IP(rsaParams.DQ, false);
            _InverseQ = RSAHelper.OS2IP(rsaParams.InverseQ, false);
            _Has_CRT_Info = true;
            _Has_PUBLIC_Info = true;
            _Has_PRIVATE_Info = true;
        }

        public RSAConfig(byte[] modulus, byte[] exponent, int modulusSize)
        {
          
            _ModulusOctets = modulusSize / 8;
            _E = RSAHelper.OS2IP(exponent, false);
            _N = RSAHelper.OS2IP(modulus, false);
            _Has_PUBLIC_Info = true;
        }

       
        public RSAConfig(byte[] modulus, byte[] exponent, byte[] d, int modulusSize)
        {
            // _rsaConfig;
            _ModulusOctets = modulusSize / 8;
            _E = RSAHelper.OS2IP(exponent, false);
            _N = RSAHelper.OS2IP(modulus, false);
            _D = RSAHelper.OS2IP(d, false);
            _Has_PUBLIC_Info = true;
            _Has_PRIVATE_Info = true;
        }

       
        public RSAConfig(byte[] modulus, byte[] exponent, byte[] d, byte[] P, byte[] Q, byte[] DP, byte[] DQ, byte[] inverseQ, int modulusSize)
        {
       
            _ModulusOctets = modulusSize / 8;
            _E = RSAHelper.OS2IP(exponent, false);
            _N = RSAHelper.OS2IP(modulus, false);
            _D = RSAHelper.OS2IP(d, false);
            _P = RSAHelper.OS2IP(P, false);
            _Q = RSAHelper.OS2IP(Q, false);
            _DP = RSAHelper.OS2IP(DP, false);
            _DQ = RSAHelper.OS2IP(DQ, false);
            _InverseQ = RSAHelper.OS2IP(inverseQ, false);
            _Has_CRT_Info = true;
            _Has_PUBLIC_Info = true;
            _Has_PRIVATE_Info = true;
        }

    }


    public class RSAHelper
    {
        
        public static RSAConfig GetRSAConfig(string XMLKeyInfo, int ModulusSize)
        {
            var hasCrtInfo = false;
            var hasPrivateInfo = false;
            var hasPublicInfo = false;

            var doc = new XmlDocument();
            try
            {
                doc.LoadXml(XMLKeyInfo);
            }
            catch (System.Exception ex)
            {
                throw new ApplicationException("Bad key xml" + ex.Message);
            }

            var Modulus = new byte[0];
            var Exponent = new byte[0];
            var D = new byte[0];
            var P = new byte[0];
            var Q = new byte[0];
            var DP = new byte[0];
            var DQ = new byte[0];
            var InverseQ = new byte[0];

            try
            {
                Modulus = Convert.FromBase64String(doc.DocumentElement.SelectSingleNode("Modulus").InnerText);
                Exponent = Convert.FromBase64String(doc.DocumentElement.SelectSingleNode("Exponent").InnerText);
                hasPublicInfo = true;
            }
            catch { }

            try
            {
                Modulus = Convert.FromBase64String(doc.DocumentElement.SelectSingleNode("Modulus").InnerText);
                D = Convert.FromBase64String(doc.DocumentElement.SelectSingleNode("D").InnerText);
                Exponent = Convert.FromBase64String(doc.DocumentElement.SelectSingleNode("Exponent").InnerText);
                hasPrivateInfo = true;
            }
            catch { }

            try
            {
                Modulus = Convert.FromBase64String(doc.DocumentElement.SelectSingleNode("Modulus").InnerText);
                P = Convert.FromBase64String(doc.DocumentElement.SelectSingleNode("P").InnerText);
                Q = Convert.FromBase64String(doc.DocumentElement.SelectSingleNode("Q").InnerText);
                DP = Convert.FromBase64String(doc.DocumentElement.SelectSingleNode("DP").InnerText);
                DQ = Convert.FromBase64String(doc.DocumentElement.SelectSingleNode("DQ").InnerText);
                InverseQ = Convert.FromBase64String(doc.DocumentElement.SelectSingleNode("InverseQ").InnerText);
                hasCrtInfo = true;
            }
            catch { }

            if (hasCrtInfo && hasPrivateInfo)
            {
                return new RSAConfig(Modulus, Exponent, D, P, Q, DP, DQ, InverseQ, ModulusSize);
            }
            else if (hasPrivateInfo)
            {
                return new RSAConfig(Modulus, Exponent, D, ModulusSize);
            }
            else if (hasPublicInfo)
            {
                return new RSAConfig(Modulus, Exponent, ModulusSize);
            }

            throw new ApplicationException("Invalid key xml");


        }

       
        public static byte[] I2OSP(BigInteger x, int xLen, bool makeLittleEndian)
        {
            byte[] result = new byte[xLen];
            int index = 0;
            while ((x > 0) && (index < result.Length))
            {
                result[index++] = (byte)(x % 256);
                x /= 256;
            }
            if (!makeLittleEndian)
                Array.Reverse(result);
            return result;
        }

    
        public static BigInteger OS2IP(byte[] data, bool isLittleEndian)
        {
            BigInteger bi = 0;
            if (isLittleEndian)
            {
                for (int i = 0; i < data.Length; i++)
                {
                    bi += BigInteger.Pow(256, i) * data[i];
                }
            }
            else
            {
                for (int i = 1; i <= data.Length; i++)
                {
                    bi += BigInteger.Pow(256, i - 1) * data[data.Length - i];
                }
            }
            return bi;
        }

       
        public static byte[] XOR(byte[] A, byte[] B)
        {
            if (A.Length != B.Length)
            {
                throw new ArgumentException("length mismatch");
            }
            else
            {
                byte[] R = new byte[A.Length];

                for (int i = 0; i < A.Length; i++)
                {
                    R[i] = (byte)(A[i] ^ B[i]);
                }
                return R;
            }
        }

        internal static void FixByteArraySign(ref byte[] bytes)
        {
            if ((bytes[bytes.Length - 1] & 0x80) > 0)
            {
                var temp = new byte[bytes.Length];
                Array.Copy(bytes, temp, bytes.Length);
                bytes = new byte[temp.Length + 1];
                Array.Copy(temp, bytes, temp.Length);
            }
        }


    }
    
}