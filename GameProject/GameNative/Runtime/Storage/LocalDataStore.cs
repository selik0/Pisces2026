using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace GameNative
{
    /// <summary>
    /// 本地加密存储，提供存档槽位、自定义路径、完整性校验和原子写入。
    /// </summary>
    public sealed class LocalDataStore
    {
        private const int FormatVersion = 1;
        private const int SignatureSize = 32;
        private const string TemporarySuffix = ".tmp";
        private static readonly byte[] Magic = Encoding.ASCII.GetBytes("PISCESLD");
        private static readonly byte[] AuthenticationContext = Encoding.ASCII.GetBytes("GameNative.LocalDataStore.Authentication.v1");

        private readonly string _rootDirectory;
#if GAME_RELEASE
        private readonly byte[] _encryptionKey;
        private readonly byte[] _authenticationKey;
#endif
        private readonly ILocalDataSerializer _serializer;

        /// <param name="rootDirectory">本地数据根目录，通常传入 <see cref="FileSystem.PersistentRoot"/>。</param>
        /// <param name="encryptionKey">16、24 或 32 字节 AES 密钥，后续读取必须使用同一密钥。</param>
        /// <param name="serializer">序列化器，默认使用 Unity JsonUtility。</param>
        public LocalDataStore(string rootDirectory, byte[] encryptionKey, ILocalDataSerializer serializer = null)
        {
            if (string.IsNullOrWhiteSpace(rootDirectory))
            {
                throw new ArgumentException("Local data root directory cannot be empty.", nameof(rootDirectory));
            }

            _rootDirectory = Path.GetFullPath(rootDirectory);
#if GAME_RELEASE
            ValidateEncryptionKey(encryptionKey);
            _encryptionKey = (byte[])encryptionKey.Clone();
            _authenticationKey = DeriveAuthenticationKey(_encryptionKey);
#endif
            _serializer = serializer ?? new UnityJsonLocalDataSerializer();
        }

        /// <summary>保存自定义相对路径的数据；仅在定义 GAME_RELEASE 时加密并校验完整性。</summary>
        public void Save<T>(string relativePath, T data) where T : class
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            string path = ResolvePath(relativePath);
            byte[] plainData = _serializer.Serialize(data);
            WriteAtomically(path, CreateFileData(plainData));
        }

        /// <summary>读取自定义相对路径的数据；仅在定义 GAME_RELEASE 时校验并解密。</summary>
        public T Load<T>(string relativePath) where T : class
        {
            if (!Exists(relativePath))
            {
                return null;
            }
            string path = ResolvePath(relativePath);
            byte[] fileData = File.ReadAllBytes(path);
            byte[] plainData = ParseFileData(fileData);
            return _serializer.Deserialize<T>(plainData);
        }

        /// <summary>检查自定义路径的数据是否存在。</summary>
        public bool Exists(string relativePath)
        {
            return File.Exists(ResolvePath(relativePath));
        }

        /// <summary>删除自定义路径的数据和未完成的临时文件。</summary>
        public void Delete(string relativePath)
        {
            string path = ResolvePath(relativePath);
            DeleteIfExists(path);
            DeleteIfExists(path + TemporarySuffix);
        }

        private byte[] CreateFileData(byte[] plainData)
        {
#if !GAME_RELEASE
            return plainData;
#else
            byte[] encryptedData = AesCrypto.Encrypt(plainData, _encryptionKey);

            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                writer.Write(Magic);
                writer.Write(FormatVersion);
                writer.Write(encryptedData.Length);
                writer.Write(encryptedData);
                writer.Flush();

                byte[] signature = ComputeSignature(stream.ToArray());
                writer.Write(signature);
                writer.Flush();
                return stream.ToArray();
            }
#endif
        }

        private byte[] ParseFileData(byte[] fileData)
        {
#if !GAME_RELEASE
            if (fileData == null)
            {
                throw new InvalidDataException("Local data file is incomplete.");
            }

            return fileData;
#else
            int minimumLength = Magic.Length + sizeof(int) * 2 + SignatureSize;
            if (fileData == null || fileData.Length < minimumLength)
            {
                throw new InvalidDataException("Local data file is incomplete.");
            }

            int signedLength = fileData.Length - SignatureSize;
            byte[] expectedSignature = ComputeSignature(fileData, signedLength);
            if (!FixedTimeEquals(fileData, signedLength, expectedSignature))
            {
                throw new CryptographicException("Local data signature verification failed.");
            }

            using (MemoryStream stream = new MemoryStream(fileData, 0, signedLength, false))
            using (BinaryReader reader = new BinaryReader(stream, Encoding.UTF8))
            {
                byte[] magic = reader.ReadBytes(Magic.Length);
                if (!FixedTimeEquals(magic, 0, Magic))
                {
                    throw new InvalidDataException("Local data file header is invalid.");
                }

                int formatVersion = reader.ReadInt32();
                if (formatVersion != FormatVersion)
                {
                    throw new InvalidDataException($"Unsupported local data format version: {formatVersion}.");
                }

                int encryptedLength = reader.ReadInt32();
                if (encryptedLength <= 0 || encryptedLength != signedLength - stream.Position)
                {
                    throw new InvalidDataException("Local data payload length is invalid.");
                }

                return AesCrypto.Decrypt(reader.ReadBytes(encryptedLength), _encryptionKey);
            }
#endif
        }

        private static void WriteAtomically(string path, byte[] data)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string temporaryPath = path + TemporarySuffix;
            DeleteIfExists(temporaryPath);

            try
            {
                using (FileStream stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    stream.Write(data, 0, data.Length);
                    stream.Flush(true);
                }

                if (File.Exists(path))
                {
                    ReplaceFile(temporaryPath, path);
                }
                else
                {
                    File.Move(temporaryPath, path);
                }
            }
            finally
            {
                DeleteIfExists(temporaryPath);
            }
        }

        private static void ReplaceFile(string temporaryPath, string path)
        {
            try
            {
                File.Replace(temporaryPath, path, null);
            }
            catch (PlatformNotSupportedException)
            {
                File.Delete(path);
                File.Move(temporaryPath, path);
            }
            catch (IOException) when (File.Exists(temporaryPath) && File.Exists(path))
            {
                File.Delete(path);
                File.Move(temporaryPath, path);
            }
        }

        private string ResolvePath(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath))
            {
                throw new ArgumentException("Local data path must be a non-empty relative path.", nameof(relativePath));
            }

            string fullPath = Path.GetFullPath(Path.Combine(_rootDirectory, relativePath));
            string rootPrefix = _rootDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            if (!fullPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Local data path cannot escape the root directory.", nameof(relativePath));
            }

            return fullPath;
        }

        private static string GetSaveRelativePath(string slot)
        {
            if (string.IsNullOrWhiteSpace(slot) || slot.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
                || slot.Contains("/") || slot.Contains("\\"))
            {
                throw new ArgumentException("Save slot must be a valid file name.", nameof(slot));
            }

            return Path.Combine("Saves", slot + ".dat");
        }

#if GAME_RELEASE
        private byte[] ComputeSignature(byte[] data, int count = -1)
        {
            using (HMACSHA256 hmac = new HMACSHA256(_authenticationKey))
            {
                return hmac.ComputeHash(data, 0, count < 0 ? data.Length : count);
            }
        }

        private static byte[] DeriveAuthenticationKey(byte[] encryptionKey)
        {
            using (HMACSHA256 hmac = new HMACSHA256(encryptionKey))
            {
                return hmac.ComputeHash(AuthenticationContext);
            }
        }
#endif

        private static bool FixedTimeEquals(byte[] data, int offset, byte[] expected)
        {
            if (data == null || expected == null || offset < 0 || data.Length - offset < expected.Length)
            {
                return false;
            }

            int difference = 0;
            for (int index = 0; index < expected.Length; index++)
            {
                difference |= data[offset + index] ^ expected[index];
            }

            return difference == 0;
        }

#if GAME_RELEASE
        private static void ValidateEncryptionKey(byte[] key)
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
#endif

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
