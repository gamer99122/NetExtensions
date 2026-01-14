using System.Security.Cryptography;
using System.Text;

namespace NetExtensions.Extensions.Data
{
    /// <summary>
    /// 連線字串加解密工具（使用 AES-256 加密）
    /// </summary>
    public static class ConnectionStringProtector
    {
        private const string EncryptedPrefix = "ENCRYPTED:";

        /// <summary>
        /// 檢查連線字串是否已加密
        /// </summary>
        /// <param name="connectionString">連線字串</param>
        /// <returns>是否已加密</returns>
        public static bool IsEncrypted(string connectionString)
        {
            return !string.IsNullOrEmpty(connectionString) && 
                   connectionString.StartsWith(EncryptedPrefix, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 加密連線字串
        /// </summary>
        /// <param name="connectionString">明文連線字串</param>
        /// <param name="key">加密金鑰</param>
        /// <returns>加密後的連線字串（含 ENCRYPTED: 前綴）</returns>
        public static string Encrypt(string connectionString, string key)
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentNullException(nameof(connectionString));

            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            // 如果已經加密，直接返回
            if (IsEncrypted(connectionString))
                return connectionString;

            try
            {
                using var aes = Aes.Create();
                aes.Key = DeriveKey(key);
                aes.GenerateIV();

                using var encryptor = aes.CreateEncryptor();
                var plainBytes = Encoding.UTF8.GetBytes(connectionString);
                var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

                // 組合 IV 和加密資料
                var combined = new byte[aes.IV.Length + encryptedBytes.Length];
                Buffer.BlockCopy(aes.IV, 0, combined, 0, aes.IV.Length);
                Buffer.BlockCopy(encryptedBytes, 0, combined, aes.IV.Length, encryptedBytes.Length);

                return $"{EncryptedPrefix}{Convert.ToBase64String(combined)}";
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("加密連線字串失敗", ex);
            }
        }

        /// <summary>
        /// 解密連線字串
        /// </summary>
        /// <param name="encryptedConnectionString">加密的連線字串</param>
        /// <param name="key">加密金鑰</param>
        /// <returns>明文連線字串</returns>
        public static string Decrypt(string encryptedConnectionString, string key)
        {
            if (string.IsNullOrEmpty(encryptedConnectionString))
                throw new ArgumentNullException(nameof(encryptedConnectionString));

            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            // 如果沒有加密，直接返回
            if (!IsEncrypted(encryptedConnectionString))
                return encryptedConnectionString;

            try
            {
                // 移除 ENCRYPTED: 前綴
                var encrypted = encryptedConnectionString.Substring(EncryptedPrefix.Length);
                var combined = Convert.FromBase64String(encrypted);

                using var aes = Aes.Create();
                aes.Key = DeriveKey(key);

                // 提取 IV
                var iv = new byte[aes.IV.Length];
                Buffer.BlockCopy(combined, 0, iv, 0, iv.Length);
                aes.IV = iv;

                // 提取加密資料
                var encryptedBytes = new byte[combined.Length - iv.Length];
                Buffer.BlockCopy(combined, iv.Length, encryptedBytes, 0, encryptedBytes.Length);

                using var decryptor = aes.CreateDecryptor();
                var decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);

                return Encoding.UTF8.GetString(decryptedBytes);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("解密連線字串失敗，請確認加密金鑰是否正確", ex);
            }
        }

        /// <summary>
        /// 從金鑰字串派生 AES 金鑰
        /// </summary>
        private static byte[] DeriveKey(string key)
        {
            using var sha256 = SHA256.Create();
            return sha256.ComputeHash(Encoding.UTF8.GetBytes(key));
        }

        /// <summary>
        /// 加密連線字串中的密碼部分
        /// </summary>
        /// <param name="connectionString">連線字串</param>
        /// <param name="key">加密金鑰</param>
        /// <returns>密碼已加密的連線字串</returns>
        public static string EncryptPassword(string connectionString, string key)
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentNullException(nameof(connectionString));

            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            // 尋找密碼部分
            var passwordPatterns = new[] { "Password=", "Pwd=" };
            foreach (var pattern in passwordPatterns)
            {
                var index = connectionString.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
                if (index >= 0)
                {
                    var startIndex = index + pattern.Length;
                    var endIndex = connectionString.IndexOf(';', startIndex);
                    
                    string password;
                    if (endIndex > 0)
                    {
                        password = connectionString.Substring(startIndex, endIndex - startIndex);
                    }
                    else
                    {
                        password = connectionString.Substring(startIndex);
                    }

                    // 如果密碼已經加密，跳過
                    if (password.StartsWith(EncryptedPrefix))
                        continue;

                    // 加密密碼
                    var encryptedPassword = Encrypt(password, key);

                    // 替換密碼
                    if (endIndex > 0)
                    {
                        connectionString = connectionString.Substring(0, startIndex) + 
                                         encryptedPassword + 
                                         connectionString.Substring(endIndex);
                    }
                    else
                    {
                        connectionString = connectionString.Substring(0, startIndex) + encryptedPassword;
                    }
                }
            }

            return connectionString;
        }

        /// <summary>
        /// 解密連線字串中的密碼部分
        /// </summary>
        /// <param name="connectionString">連線字串</param>
        /// <param name="key">加密金鑰</param>
        /// <returns>密碼已解密的連線字串</returns>
        public static string DecryptPassword(string connectionString, string key)
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentNullException(nameof(connectionString));

            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            // 尋找加密的密碼部分
            var passwordPatterns = new[] { "Password=", "Pwd=" };
            foreach (var pattern in passwordPatterns)
            {
                var index = connectionString.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
                if (index >= 0)
                {
                    var startIndex = index + pattern.Length;
                    var endIndex = connectionString.IndexOf(';', startIndex);
                    
                    string password;
                    if (endIndex > 0)
                    {
                        password = connectionString.Substring(startIndex, endIndex - startIndex);
                    }
                    else
                    {
                        password = connectionString.Substring(startIndex);
                    }

                    // 如果密碼已加密，解密它
                    if (IsEncrypted(password))
                    {
                        var decryptedPassword = Decrypt(password, key);

                        // 替換密碼
                        if (endIndex > 0)
                        {
                            connectionString = connectionString.Substring(0, startIndex) + 
                                             decryptedPassword + 
                                             connectionString.Substring(endIndex);
                        }
                        else
                        {
                            connectionString = connectionString.Substring(0, startIndex) + decryptedPassword;
                        }
                    }
                }
            }

            return connectionString;
        }
    }
}
