# ConnectionFactory 使用文件

資料庫連線工廠，支援多種資料庫類型與連線字串加密功能。

## 🚀 快速開始

### 基本使用

```csharp
using NetExtensions.Extensions.Data;
using Microsoft.Extensions.Configuration;

// 方式 1: 直接建立連線
using var connection = ConnectionFactory.Create("Server=localhost;Database=MyDb;User Id=sa;Password=xxx;", DatabaseType.SqlServer);

// 方式 2: 從配置檔建立
using var connection = ConnectionFactory.CreateFromConfig(configuration, "DefaultConnection");

// 方式 3: 建立並開啟連線
using var connection = ConnectionFactory.CreateAndOpen(connectionString, DatabaseType.SqlServer);
```

---

## 🔐 加密連線字串

### 為什麼要加密？

- ✅ 可以安全地將連線字串加入版控
- ✅ 避免密碼明文儲存
- ✅ 使用 AES-256 加密標準

### 加密步驟

#### 1. 使用 CLI 工具加密

```bash
# 進入專案目錄
cd NetExtensions

# 加密整個連線字串
dotnet run --project Tools encrypt \
  "Server=localhost;Database=MyDb;User Id=sa;Password=123456" \
  "your-secret-key"

# 輸出：
# ✅ 加密成功！
# 加密後的連線字串：
# ENCRYPTED:AQAAAAEAACcQAAAAEH8f9x...
```

#### 2. 將加密後的字串放入配置檔

```json
// appsettings.json（可以安全地加入版控）
{
  "ConnectionStrings": {
    "DefaultConnection": "ENCRYPTED:AQAAAAEAACcQAAAAEH8f9x..."
  }
}
```

#### 3. 設定加密金鑰（環境變數）

```bash
# Windows (PowerShell)
$env:DB_ENCRYPTION_KEY="your-secret-key"

# Linux / macOS
export DB_ENCRYPTION_KEY="your-secret-key"
```

#### 4. 在程式中使用（自動解密）

```csharp
// ConnectionFactory 會自動偵測並解密
using var connection = ConnectionFactory.CreateFromConfig(configuration, "DefaultConnection");

// 完全透明，不需要手動解密
var users = await connection.pQueryListAsync<User>("SELECT * FROM Users");
```

---

## 📋 支援的資料庫類型

| 資料庫 | DatabaseType | 連線字串範例 |
|-------|-------------|------------|
| SQL Server | `DatabaseType.SqlServer` | `Server=localhost;Database=MyDb;User Id=sa;Password=xxx;` |
| MySQL | `DatabaseType.MySql` | `Server=localhost;Database=MyDb;User=root;Password=xxx;` |
| PostgreSQL | `DatabaseType.PostgreSql` | `Host=localhost;Database=MyDb;Username=postgres;Password=xxx;` |
| SQLite | `DatabaseType.Sqlite` | `Data Source=mydb.db` |
| Oracle | `DatabaseType.Oracle` | `Data Source=MyOracleDB;User Id=system;Password=xxx;` |

> **注意**：目前只內建 SQL Server 支援。其他資料庫需要安裝對應的 NuGet 套件。

---

## 🔧 配置檔格式

### 基本配置

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=MyDb;...",
    "MySqlConnection": "Server=localhost;Database=MyDb;...",
    "PostgresConnection": "Host=localhost;Database=MyDb;..."
  }
}
```

### 加密配置

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "ENCRYPTED:AQAAAAEAACcQAAAAEH8f9x...",
    "MySqlConnection": "ENCRYPTED:BQAAAAEAACcQAAAAEH8f9x..."
  },
  "Encryption": {
    "Key": "${DB_ENCRYPTION_KEY}"  // 從環境變數讀取
  }
}
```

### 指定資料庫類型

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "...",
    "MySqlConnection": "..."
  },
  "DatabaseTypes": {
    "DefaultConnection": "SqlServer",
    "MySqlConnection": "MySql"
  }
}
```

---

## 💡 使用範例

### 範例 1: 基本 CRUD 操作

```csharp
public class UserService
{
    private readonly IConfiguration _configuration;

    public UserService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<User> GetUser(int id)
    {
        using var connection = ConnectionFactory.CreateFromConfig(_configuration, "DefaultConnection");

        return await connection.pQueryFirstOrDefaultAsync<User>("SELECT * FROM Users WHERE Id = @Id", new { Id = id });
    }

    public async Task<int> CreateUser(User user)
    {
        using var connection = ConnectionFactory.CreateFromConfig(_configuration, "DefaultConnection");

        return await connection.pInsertWithIdAsync<int>("Users", user);
    }
}
```

### 範例 2: 使用交易

```csharp
public async Task TransferMoney(int fromId, int toId, decimal amount)
{
    using var connection = ConnectionFactory.CreateAndOpenFromConfig(_configuration, "DefaultConnection");

    await connection.pExecuteInTransactionAsync(async (conn, trans) =>
    {
        await conn.pExecuteAsync(
            "UPDATE Accounts SET Balance = Balance - @Amount WHERE Id = @Id",
            new { Id = fromId, Amount = amount },
            trans);

        await conn.pExecuteAsync(
            "UPDATE Accounts SET Balance = Balance + @Amount WHERE Id = @Id",
            new { Id = toId, Amount = amount },
            trans);
    });
}
```

### 範例 3: 多資料庫連線

```csharp
public async Task SyncData()
{
    // 從 SQL Server 讀取
    using var sqlConnection = ConnectionFactory.CreateFromConfig(_configuration, "SqlServerConnection");

    var users = await sqlConnection.pQueryListAsync<User>("SELECT * FROM Users");

    // 寫入 MySQL
    using var mysqlConnection = ConnectionFactory.CreateFromConfig(_configuration, "MySqlConnection");

    await mysqlConnection.pBulkInsertAsync("Users", users);
}
```

---

## 🔒 安全性最佳實踐

### ✅ 建議做法

1. **使用加密**
   ```bash
   # 加密連線字串
   dotnet run --project Tools encrypt "..." "your-key"
   ```

2. **金鑰管理**
   ```bash
   # 使用環境變數儲存金鑰
   export DB_ENCRYPTION_KEY="your-secret-key"
   ```

3. **版控管理**
   ```gitignore
   # .gitignore
   appsettings.Development.json
   appsettings.Production.json
   *.local.json
   ```

4. **不同環境使用不同金鑰**
   - 開發環境：`DEV_ENCRYPTION_KEY`
   - 測試環境：`TEST_ENCRYPTION_KEY`
   - 正式環境：`PROD_ENCRYPTION_KEY`

### ❌ 避免做法

1. ❌ 不要將金鑰寫在配置檔中
2. ❌ 不要將明文密碼提交到版控
3. ❌ 不要在程式碼中硬編碼連線字串
4. ❌ 不要在多個環境使用相同金鑰

---

## 🛠️ CLI 工具使用

### 加密整個連線字串

```bash
dotnet run --project Tools encrypt \
  "Server=localhost;Database=MyDb;User Id=sa;Password=123" \
  "my-secret-key"
```

### 只加密密碼部分

```bash
dotnet run --project Tools encrypt-password \
  "Server=localhost;Database=MyDb;User Id=sa;Password=123" \
  "my-secret-key"

# 輸出：
# Server=localhost;Database=MyDb;User Id=sa;Password=ENCRYPTED:xxx
```

### 解密連線字串

```bash
dotnet run --project Tools decrypt \
  "ENCRYPTED:AQAAAAEAACcQAAAAEH8f9x..." \
  "my-secret-key"
```

---

## ❓ 常見問題

### Q1: 如何更換加密金鑰？

A: 使用新金鑰重新加密所有連線字串，然後更新環境變數。

```bash
# 1. 用新金鑰加密
dotnet run --project Tools encrypt "..." "new-key"

# 2. 更新 appsettings.json

# 3. 更新環境變數
export DB_ENCRYPTION_KEY="new-key"
```

### Q2: 忘記加密金鑰怎麼辦？

A: 無法復原。需要重新設定連線字串並使用新金鑰加密。

### Q3: 可以在不同環境使用不同的連線字串嗎？

A: 可以，使用不同的配置檔：

```
appsettings.Development.json  # 開發環境
appsettings.Staging.json      # 測試環境
appsettings.Production.json   # 正式環境
```

### Q4: 加密會影響效能嗎？

A: 解密只在建立連線時執行一次，對效能影響極小。

---

## 📚 相關文件

- [Dapper 快速入門](DapperQuickStart.md)
- [Dapper 查詢擴充](DapperQueryExtensions.md)
- [Dapper 執行擴充](DapperExecuteExtensions.md)
- [Dapper 交易擴充](DapperTransactionExtensions.md)
