using Microsoft.Extensions.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace NetExtensions.Extensions.Data
{
    /// <summary>
    /// 資料庫連線工廠
    /// </summary>
    public static class ConnectionFactory
    {
        /// <summary>
        /// 建立資料庫連線
        /// </summary>
        /// <param name="connectionString">連線字串</param>
        /// <param name="databaseType">資料庫類型（預設為 SQL Server）</param>
        /// <returns>資料庫連線</returns>
        public static IDbConnection Create(string connectionString, DatabaseType databaseType = DatabaseType.SqlServer)
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentNullException(nameof(connectionString));

            return databaseType switch
            {
                DatabaseType.SqlServer => new SqlConnection(connectionString),
                DatabaseType.MySql => throw new NotSupportedException("MySQL 支援需要安裝 MySql.Data 套件"),
                DatabaseType.PostgreSql => throw new NotSupportedException("PostgreSQL 支援需要安裝 Npgsql 套件"),
                DatabaseType.Sqlite => throw new NotSupportedException("SQLite 支援需要安裝 Microsoft.Data.Sqlite 套件"),
                DatabaseType.Oracle => throw new NotSupportedException("Oracle 支援需要安裝 Oracle.ManagedDataAccess.Core 套件"),
                _ => throw new ArgumentException($"不支援的資料庫類型: {databaseType}", nameof(databaseType))
            };
        }

        /// <summary>
        /// 從配置檔建立資料庫連線（支援自動解密）
        /// </summary>
        /// <param name="configuration">配置物件</param>
        /// <param name="connectionName">連線名稱（預設為 "DefaultConnection"）</param>
        /// <param name="encryptionKey">加密金鑰（如果為 null，會從環境變數 DB_ENCRYPTION_KEY 讀取）</param>
        /// <returns>資料庫連線</returns>
        public static IDbConnection CreateFromConfig(IConfiguration configuration, string connectionName = "DefaultConnection", string? encryptionKey = null)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            // 讀取連線字串
            var connectionString = configuration.GetConnectionString(connectionName);
            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException($"找不到連線字串: {connectionName}");

            // 如果連線字串已加密，解密它
            if (ConnectionStringProtector.IsEncrypted(connectionString))
            {
                // 取得加密金鑰
                var key = encryptionKey ?? 
                         configuration["Encryption:Key"] ?? 
                         Environment.GetEnvironmentVariable("DB_ENCRYPTION_KEY");

                if (string.IsNullOrEmpty(key))
                    throw new InvalidOperationException("找不到加密金鑰。請設定環境變數 DB_ENCRYPTION_KEY 或在配置檔中設定 Encryption:Key");

                connectionString = ConnectionStringProtector.Decrypt(connectionString, key);
            }

            // 偵測資料庫類型
            var databaseType = DetectDatabaseType(configuration, connectionName, connectionString);

            return Create(connectionString, databaseType);
        }

        /// <summary>
        /// 建立並開啟資料庫連線
        /// </summary>
        /// <param name="connectionString">連線字串</param>
        /// <param name="databaseType">資料庫類型（預設為 SQL Server）</param>
        /// <returns>已開啟的資料庫連線</returns>
        public static IDbConnection CreateAndOpen(string connectionString, DatabaseType databaseType = DatabaseType.SqlServer)
        {
            var connection = Create(connectionString, databaseType);
            connection.Open();
            return connection;
        }

        /// <summary>
        /// 從配置檔建立並開啟資料庫連線
        /// </summary>
        /// <param name="configuration">配置物件</param>
        /// <param name="connectionName">連線名稱（預設為 "DefaultConnection"）</param>
        /// <param name="encryptionKey">加密金鑰（如果為 null，會從環境變數讀取）</param>
        /// <returns>已開啟的資料庫連線</returns>
        public static IDbConnection CreateAndOpenFromConfig(IConfiguration configuration, string connectionName = "DefaultConnection", string? encryptionKey = null)
        {
            var connection = CreateFromConfig(configuration, connectionName, encryptionKey);
            connection.Open();
            return connection;
        }

        /// <summary>
        /// 偵測資料庫類型
        /// </summary>
        private static DatabaseType DetectDatabaseType(IConfiguration configuration, string connectionName, string connectionString)
        {
            // 1. 從配置檔讀取資料庫類型
            var typeFromConfig = configuration[$"DatabaseTypes:{connectionName}"];
            if (!string.IsNullOrEmpty(typeFromConfig) && Enum.TryParse<DatabaseType>(typeFromConfig, true, out var dbType))
                return dbType;

            // 2. 從連線字串偵測
            var lowerConnectionString = connectionString.ToLower();

            if (lowerConnectionString.Contains("server=") || lowerConnectionString.Contains("data source="))
            {
                if (lowerConnectionString.Contains(".db") || lowerConnectionString.Contains("sqlite"))
                    return DatabaseType.Sqlite;
                
                return DatabaseType.SqlServer;
            }

            if (lowerConnectionString.Contains("host="))
                return DatabaseType.PostgreSql;

            if (lowerConnectionString.Contains("mysql"))
                return DatabaseType.MySql;

            // 預設為 SQL Server
            return DatabaseType.SqlServer;
        }
    }
}
