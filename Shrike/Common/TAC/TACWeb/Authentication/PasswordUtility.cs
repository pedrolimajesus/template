using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

namespace AppComponents.Web
{
	public static class PasswordUtility
	{
		public static string RandomSalt()
		{
			var saltBytes = new Byte[4];
			var rng = new RNGCryptoServiceProvider();
			rng.GetBytes(saltBytes);
			return Convert.ToBase64String(saltBytes);
		}

		public static string HashPassword(string pass, string salt, string hashAlgorithm, string macKey)
		{
			byte[] bytes = Encoding.Unicode.GetBytes(pass);
			byte[] src = Encoding.Unicode.GetBytes(salt);

			var dst = new byte[src.Length + bytes.Length];
			Buffer.BlockCopy(src, 0, dst, 0, src.Length);
			Buffer.BlockCopy(bytes, 0, dst, src.Length, bytes.Length);

		    HashAlgorithm algorithm;
			if (hashAlgorithm.ToUpper().Contains("HMAC"))
            {
                if(string.IsNullOrEmpty(macKey))
                    throw new ArgumentException("macKey");
                var keyedAlg = KeyedHashAlgorithm.Create(hashAlgorithm);
                keyedAlg.Key = DecodeHexString(macKey);
                algorithm = keyedAlg;
            }
            else
            {
                algorithm = HashAlgorithm.Create(hashAlgorithm);
            }
			var inArray = algorithm.ComputeHash(dst);
			return Convert.ToBase64String(inArray);
		}

        private static byte[] DecodeHexString(string hexString)
        {
            var returnBytes = new byte[hexString.Length / 2];
            for (int i = 0; i < returnBytes.Length; i++)
                returnBytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            return returnBytes;

            //byte[] returnBytes = Enumerable.Range(0, hexString.Length / 2)
            //    .Select((x, i) => Convert.ToByte(hexString.Substring(i * 2, 2), 16))
            //    .ToArray();
            //return returnBytes;
        }
	}
}
