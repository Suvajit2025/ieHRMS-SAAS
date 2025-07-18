using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ieHRMS.Core.Interface
{
    public interface IEncryptDecrypt
    {
        Task<string> EncryptAsync(string plainText, string encryptionKey);
        Task<string> DecryptAsync(string cipherText, string encryptionKey); 

    }
}
