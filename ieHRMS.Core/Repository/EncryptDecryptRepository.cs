 
using ieHRMS.Core.Interface;
using Serilog;
using System.Security.Cryptography;
using System.Text;

namespace ieHRMS.Core.Repository
{
    public class EncryptDecryptRepository:IEncryptDecrypt
    {
        // Configure Serilog in a static constructor
        static EncryptDecryptRepository()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.File("logs/encryption_log-.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();
        }
        public async Task<string> EncryptAsync(string plainText, string encryptionKey)
        {
            if (string.IsNullOrEmpty(encryptionKey) || encryptionKey.Length < 16)
            {
                Log.Error("Encryption failed: Encryption key must be at least 16 characters long.");
                throw new ArgumentException("Encryption key must be at least 16 characters long.");
            }

            try
            {
                byte[] salt = GenerateSalt();
                string plainTextWithSalt = Convert.ToBase64String(salt) + plainText;

                using (Aes aes = Aes.Create())
                {
                    aes.Key = Encoding.UTF8.GetBytes(encryptionKey.Substring(0, 32));
                    aes.IV = Encoding.UTF8.GetBytes(encryptionKey.Substring(0, 16));

                    ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                        {
                            using (StreamWriter sw = new StreamWriter(cs))
                            {
                                await sw.WriteAsync(plainTextWithSalt);
                            }
                        }
                        return Convert.ToBase64String(ms.ToArray());
                    }
                }
            }
            catch (CryptographicException cryptoEx)
            {
                Log.Error(cryptoEx, "Encryption failed due to a cryptographic error.");
                return null;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An unexpected error occurred during encryption.");
                return null;
            }
        }

        public async Task<string> DecryptAsync(string cipherText, string encryptionKey)
        {
            if (string.IsNullOrEmpty(encryptionKey) || encryptionKey.Length < 16)
            {
                Log.Error("Decryption failed: Encryption key must be at least 16 characters long.");
                throw new ArgumentException("Encryption key must be at least 16 characters long.");
            }

            try
            {
                cipherText = cipherText.Replace(" ", "+");
                byte[] buffer = Convert.FromBase64String(cipherText);

                using (Aes aes = Aes.Create())
                {
                    aes.Key = Encoding.UTF8.GetBytes(encryptionKey.Substring(0, 32));
                    aes.IV = Encoding.UTF8.GetBytes(encryptionKey.Substring(0, 16));

                    ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                    using (MemoryStream ms = new MemoryStream(buffer))
                    {
                        using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                        {
                            using (StreamReader sr = new StreamReader(cs))
                            {
                                string decryptedTextWithSalt = await sr.ReadToEndAsync();
                                string saltBase64 = decryptedTextWithSalt.Substring(0, 24);
                                string plainText = decryptedTextWithSalt.Substring(24);

                                return plainText;
                            }
                        }
                    }
                }
            }
            catch (FormatException formatEx)
            {
                Log.Error(formatEx, "Decryption failed due to an invalid input format.");
                return null;
            }
            catch (CryptographicException cryptoEx)
            {
                Log.Error(cryptoEx, "Decryption failed due to a cryptographic error.");
                return null;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An unexpected error occurred during decryption.");
                return null;
            }
        }

        private byte[] GenerateSalt()
        {
            try
            {
                byte[] salt = new byte[16];
                using (var rng = new RNGCryptoServiceProvider())
                {
                    rng.GetBytes(salt);
                }
                return salt;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Salt generation failed.");
                return new byte[0];
            }
        }
    }
} 
