using System;
using System.Security.Cryptography;
using System.Text;

namespace GameNative
{
    /// <summary>
    /// AES 加解密工具。默认使用 CBC 模式和 PKCS7 填充。
    /// </summary>
    public static class AesCrypto
    {
        private const int DefaultKeySize = 256;
        private const int IvSize = 16;

        /// <summary>生成随机 AES 密钥。</summary>
        /// <param name="keySize">密钥位数，只能为 128、192 或 256。</param>
        public static byte[] GenerateKey(int keySize = DefaultKeySize)
        {
            using (Aes aes = Aes.Create())
            {
                aes.KeySize = keySize;
                aes.GenerateKey();
                return aes.Key;
            }
        }

        /// <summary>
        /// 使用随机 IV 加密数据。返回值前 16 字节为 IV，其余为密文。
        /// </summary>
        public static byte[] Encrypt(byte[] plainData, byte[] key)
        {
            ValidateData(plainData, nameof(plainData));
            ValidateKey(key);

            using (Aes aes = CreateAes(key))
            {
                aes.GenerateIV();
                byte[] encryptedData = Transform(plainData, aes.CreateEncryptor());
                byte[] result = new byte[IvSize + encryptedData.Length];
                Buffer.BlockCopy(aes.IV, 0, result, 0, IvSize);
                Buffer.BlockCopy(encryptedData, 0, result, IvSize, encryptedData.Length);
                return result;
            }
        }

        /// <summary>使用指定 IV 加密数据，仅返回密文。</summary>
        public static byte[] Encrypt(byte[] plainData, byte[] key, byte[] iv)
        {
            ValidateData(plainData, nameof(plainData));
            ValidateKey(key);
            ValidateIv(iv);

            using (Aes aes = CreateAes(key, iv))
            {
                return Transform(plainData, aes.CreateEncryptor());
            }
        }

        /// <summary>
        /// 解密由 <see cref="Encrypt(byte[], byte[])"/> 生成的数据。
        /// </summary>
        public static byte[] Decrypt(byte[] encryptedData, byte[] key)
        {
            ValidateData(encryptedData, nameof(encryptedData));
            ValidateKey(key);

            if (encryptedData.Length <= IvSize)
            {
                throw new ArgumentException("Encrypted data must contain an IV and ciphertext.", nameof(encryptedData));
            }

            byte[] iv = new byte[IvSize];
            byte[] cipherData = new byte[encryptedData.Length - IvSize];
            Buffer.BlockCopy(encryptedData, 0, iv, 0, IvSize);
            Buffer.BlockCopy(encryptedData, IvSize, cipherData, 0, cipherData.Length);
            return Decrypt(cipherData, key, iv);
        }

        /// <summary>使用指定 IV 解密密文。</summary>
        public static byte[] Decrypt(byte[] encryptedData, byte[] key, byte[] iv)
        {
            ValidateData(encryptedData, nameof(encryptedData));
            ValidateKey(key);
            ValidateIv(iv);

            using (Aes aes = CreateAes(key, iv))
            {
                return Transform(encryptedData, aes.CreateDecryptor());
            }
        }

        /// <summary>加密 UTF-8 文本并返回 Base64 字符串。</summary>
        public static string EncryptString(string plainText, byte[] key)
        {
            if (plainText == null)
            {
                throw new ArgumentNullException(nameof(plainText));
            }

            return Convert.ToBase64String(Encrypt(Encoding.UTF8.GetBytes(plainText), key));
        }

        /// <summary>解密 Base64 字符串并返回 UTF-8 文本。</summary>
        public static string DecryptString(string encryptedText, byte[] key)
        {
            if (encryptedText == null)
            {
                throw new ArgumentNullException(nameof(encryptedText));
            }

            return Encoding.UTF8.GetString(Decrypt(Convert.FromBase64String(encryptedText), key));
        }

        private static Aes CreateAes(byte[] key, byte[] iv = null)
        {
            Aes aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = key;

            if (iv != null)
            {
                aes.IV = iv;
            }

            return aes;
        }

        private static byte[] Transform(byte[] data, ICryptoTransform transform)
        {
            using (transform)
            {
                return transform.TransformFinalBlock(data, 0, data.Length);
            }
        }

        private static void ValidateData(byte[] data, string parameterName)
        {
            if (data == null)
            {
                throw new ArgumentNullException(parameterName);
            }
        }

        private static void ValidateKey(byte[] key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (key.Length != 16 && key.Length != 24 && key.Length != 32)
            {
                throw new ArgumentException("AES key must be 16, 24, or 32 bytes.", nameof(key));
            }
        }

        private static void ValidateIv(byte[] iv)
        {
            if (iv == null)
            {
                throw new ArgumentNullException(nameof(iv));
            }

            if (iv.Length != IvSize)
            {
                throw new ArgumentException("AES IV must be 16 bytes.", nameof(iv));
            }
        }
    }
}
