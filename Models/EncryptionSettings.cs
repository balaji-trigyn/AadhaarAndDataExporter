using System;
using System.Collections.Generic;
using System.Text;

namespace AadhaarAndDataExporter.Models
{
    public class EncryptionKeyConfig
    {
        public string AesKey { get; set; } = string.Empty;
        public string AesVector { get; set; } = string.Empty;
        public string Hmacsha256Key { get; set; } = string.Empty;
    }
}
