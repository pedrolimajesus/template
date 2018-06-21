using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace AppComponents.ControlFlow
{
    /// <summary>
    /// 
    /// </summary>
    public class LicenseItem
    {
        /// <summary>
        /// 
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public bool Enabled { get; set; }
        
        /// <summary>
        /// 
        /// </summary>
        public int Count { get; set; }
        
        /// <summary>
        /// 
        /// </summary>
        public string Value { get; set; }
        
        /// <summary>
        /// 
        /// </summary>
        public DateTime AtTime { get; set; }
    }


    /// <summary>
    /// 
    /// </summary>
    public class LicenseSpecification
    {
        /// <summary>
        /// 
        /// </summary>
        public LicenseSpecification()
        {
            Id = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public DateTimeOffset IssueDate { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string Customer { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string SKUCode { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public LicenseItem[] Items { get; set; }

        
    }

    public class LicenseTemplate
    {
        public LicenseItem[] Items { get; set; }

        public static LicenseTemplate Load(string path)
        {
            return JsonConvert.DeserializeObject<LicenseTemplate>(File.ReadAllText(path));
        }
    }

    public class License
    {
        public string AuthorizationCode { get; set; }
        public LicenseSpecification Specification { get; set; }

        public static License FromTemplate(string templateJson)
        {
            var retval = new License
                {
                    Specification =
                        new LicenseSpecification
                            {
                                Items = JsonConvert.DeserializeObject<LicenseTemplate>(templateJson).Items
                            }
                };
            return retval;
        }

        public static License Load(string path)
        {
            return JsonConvert.DeserializeObject<License>(File.ReadAllText(path));
        }

        public void Save(string path)
        {
            File.WriteAllText(path,JsonConvert.SerializeObject(this));
        }
    }


    public class LicenseGenerator: IDisposable
    {
        private RSACrypto _crypto;

        public const int KeyBitSize = 1024;

        public LicenseGenerator()
        {
          
        
        }
        
        public void GenerateLicenseMasterKeys()
        {
            GenerateKey();
            _crypto = new RSACrypto(PublicPrivateKeyInfoXML, KeyBitSize);
        }

        public void Initialize(string publicPrivateKeyInfoXML, string publicKeyInfoXML)
        {
            PublicPrivateKeyInfoXML = publicPrivateKeyInfoXML;
            PublicKeyInfoXML = publicKeyInfoXML;
        
            _crypto = new RSACrypto(PublicPrivateKeyInfoXML, KeyBitSize);
        }

        private void GenerateKey()
        {
            var csp = new RSACryptoServiceProvider(KeyBitSize);
            PublicPrivateKeyInfoXML = csp.ToXmlString(true);
            PublicKeyInfoXML = csp.ToXmlString(false);
        }

        public string PublicPrivateKeyInfoXML { get; private set; }
        public string PublicKeyInfoXML { get; private set; }

        public void Authorize(License license)
        {
            var specText = JsonConvert.SerializeObject(license.Specification);
            var hash = Hash.KnuthHash(specText);
            var ctx = _crypto.Encrypt(BitConverter.GetBytes(hash), true, true);
            license.AuthorizationCode = Convert.ToBase64String(ctx);
        }

        public void SaveKeyFile(string path)
        {
            var items = JsonConvert.SerializeObject(new[] {PublicPrivateKeyInfoXML, PublicKeyInfoXML});
            File.WriteAllText(path, items);
        }

        public void LoadKeyFile(string path)
        {
            var raw = File.ReadAllText(path);
            var items = JsonConvert.DeserializeObject<string[]>(raw);
            Initialize(items[0],items[1]);
        }

        public void SavePublicKeyFile(string path)
        {
            File.WriteAllText(path, PublicKeyInfoXML);
        }


        public void Dispose()
        {
            if(null != _crypto)
                _crypto.Dispose();
        }
    }



    public class LicenseAcceptor: IDisposable
    {
        private string _publicKeyInfo;
        private RSACrypto _crypto;

        public LicenseAcceptor()
        {
            
        }

        public LicenseAcceptor(string publicKeyInfo)
        {
            _publicKeyInfo = publicKeyInfo;
            _crypto = new RSACrypto(_publicKeyInfo, LicenseGenerator.KeyBitSize);
        }

        public void LoadKeyFile(string path)
        {
            _publicKeyInfo = File.ReadAllText(path);
            _crypto = new RSACrypto(_publicKeyInfo, LicenseGenerator.KeyBitSize);
        }

        public bool Validate(License license)
        {
            var specText = JsonConvert.SerializeObject(license.Specification);
            var hash = Hash.KnuthHash(specText);
            var etx = Convert.FromBase64String(license.AuthorizationCode);
            var ptx = _crypto.Decrypt(etx, false, true);
            var decrypted =BitConverter.ToUInt64(ptx,0);
            return hash == decrypted;
        }


        public void Dispose()
        {
            if(null!=_crypto)
                _crypto.Dispose();
        }
    }
}
