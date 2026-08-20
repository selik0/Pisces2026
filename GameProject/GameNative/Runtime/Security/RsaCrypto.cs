using System;
using System.Security.Cryptography;
using System.Text;

namespace GameNative
{
    /// <summary>
    /// RSA 加解密工具。默认使用 OAEP 填充，适合加密密钥等短数据。
    /// </summary>
    public static class RsaCrypto
    {
        private const int DefaultKeySize = 2048;

        /// <summary>生成 RSA 公钥和私钥参数。</summary>
        public static void GenerateKeyPair(out RSAParameters publicKey, out RSAParameters privateKey, int keySize = DefaultKeySize)
        {
            if (keySize < 1024 || keySize % 8 != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(keySize), "RSA key size must be at least 1024 bits and divisible by 8.");
            }

            using (RSA rsa = RSA.Create())
            {
                rsa.KeySize = keySize;
                publicKey = rsa.ExportParameters(false);
                privateKey = rsa.ExportParameters(true);
            }
        }

        /// <summary>
        /// 使用公钥加密数据。OAEP 为 true 时使用 SHA-1 OAEP，以兼容 Unity 支持的 .NET 运行时。
        /// </summary>
        public static byte[] Encrypt(byte[] plainData, RSAParameters publicKey, bool useOaep = true)
        {
            ValidateData(plainData, nameof(plainData));

            using (RSACryptoServiceProvider rsa = CreateProvider(publicKey))
            {
                return rsa.Encrypt(plainData, useOaep);
            }
        }

        /// <summary>使用私钥解密数据，填充方式必须与加密时一致。</summary>
        public static byte[] Decrypt(byte[] encryptedData, RSAParameters privateKey, bool useOaep = true)
        {
            ValidateData(encryptedData, nameof(encryptedData));

            using (RSACryptoServiceProvider rsa = CreateProvider(privateKey))
            {
                return rsa.Decrypt(encryptedData, useOaep);
            }
        }

        /// <summary>使用公钥加密 UTF-8 文本并返回 Base64 字符串。</summary>
        public static string EncryptString(string plainText, RSAParameters publicKey, bool useOaep = true)
        {
            if (plainText == null)
            {
                throw new ArgumentNullException(nameof(plainText));
            }

            return Convert.ToBase64String(Encrypt(Encoding.UTF8.GetBytes(plainText), publicKey, useOaep));
        }

        /// <summary>使用私钥解密 Base64 字符串并返回 UTF-8 文本。</summary>
        public static string DecryptString(string encryptedText, RSAParameters privateKey, bool useOaep = true)
        {
            if (encryptedText == null)
            {
                throw new ArgumentNullException(nameof(encryptedText));
            }

            return Encoding.UTF8.GetString(Decrypt(Convert.FromBase64String(encryptedText), privateKey, useOaep));
        }

        private static RSACryptoServiceProvider CreateProvider(RSAParameters parameters)
        {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();

            try
            {
                rsa.PersistKeyInCsp = false;
                rsa.ImportParameters(parameters);
                return rsa;
            }
            catch
            {
                rsa.Dispose();
                throw;
            }
        }

        private static void ValidateData(byte[] data, string parameterName)
        {
            if (data == null)
            {
                throw new ArgumentNullException(parameterName);
            }
        }
    }
}
