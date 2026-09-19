using AadhaarAndDataExporter.Models;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using System.Text;

namespace AadhaarAndDataExporter.Services
{
    public interface IAadhaarDecryptor
    {
        string DecryptSingle(string encryptedVal);
        List<string> DecryptBatch(IEnumerable<string> encryptedBatch);
    }

    public class AadhaarDecryptor : IAadhaarDecryptor
    {
        private readonly byte[] _key;
        private readonly byte[] _iv;

        public AadhaarDecryptor(IOptions<EncryptionKeyConfig> config)
        {
            var aesKeyStr = config.Value.AesKey;
            var aesVectorStr = config.Value.AesVector;

            if (string.IsNullOrEmpty(aesKeyStr) || string.IsNullOrEmpty(aesVectorStr))
                throw new ArgumentNullException("EncryptionKey settings are missing from appsettings.json");

            _key = Encoding.UTF8.GetBytes(aesKeyStr);
            _iv = Encoding.UTF8.GetBytes(aesVectorStr);
        }

        public string DecryptSingle(string encryptedVal)
        {
            if (string.IsNullOrWhiteSpace(encryptedVal)) return string.Empty;

            try
            {
                byte[] cipherTextBytes = Convert.FromBase64String(encryptedVal);
                using var aes = System.Security.Cryptography.Aes.Create();
                aes.Key = _key;
                aes.IV = _iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                using var ms = new MemoryStream(cipherTextBytes);
                using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
                using var reader = new StreamReader(cs, Encoding.UTF8);

                return reader.ReadToEnd();
            }
            catch
            {
                return "[Decryption_Failed]";
            }
        }

        public List<string> DecryptBatch(IEnumerable<string> encryptedBatch)
        {
            var list = new List<string>();
            foreach (var item in encryptedBatch)
            {
                list.Add(DecryptSingle(item));
            }
            return list;
        }
    }
}