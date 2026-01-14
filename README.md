# NetExtensions

.NET 8 擴充方法函式庫，提供常用的擴充方法以提升開發效率。

## 📦 安裝

將此專案加入您的方案中，然後在需要使用的地方加入命名空間：

```csharp
using NetExtensions.Extensions;
using NetExtensions.Extensions.Data;
```

## 🚀 快速開始

### Dapper 資料庫操作

```csharp
using var connection = new SqlConnection(connectionString);

// 查詢單筆資料
var user = await connection.pQueryFirstOrDefaultAsync<User>(
    "SELECT * FROM Users WHERE Id = @Id", 
    new { Id = 1 });

// 新增資料並取得 Id
var newId = await connection.pInsertWithIdAsync<int>(
    "Users",
    new { Name = "John", Email = "john@example.com" });

// 在交易中執行多個操作
await connection.pExecuteInTransactionAsync(async (conn, trans) =>
{
    await conn.pInsertAsync("Orders", order, trans);
    await conn.pUpdateAsync("Products", product, transaction: trans);
});

// 批次新增資料
var users = new List<User> { /* ... */ };
await connection.pBulkInsertAsync("Users", users);
```

### 字串擴充

```csharp
string text = "Hello World";
string left = text.pLeft(5);        // "Hello"
string right = text.pRight(5);      // "World"

string dateStr = "2024/01/15";
DateTime date = dateStr.pParseDateTime("yyyy/MM/dd");
```

### 目錄擴充

```csharp
string path = @"C:\Temp\MyFolder";
path.pDirectoryEnsureExists();  // 確保資料夾存在，不存在則建立
```

## 📚 詳細文件

### Dapper 擴充方法
- [Dapper 快速入門](docs/DapperQuickStart.md) - Dapper 基本概念與使用說明
- [查詢擴充](docs/DapperQueryExtensions.md) - 查詢相關的擴充方法
- [執行擴充](docs/DapperExecuteExtensions.md) - INSERT、UPDATE、DELETE 操作
- [交易擴充](docs/DapperTransactionExtensions.md) - 交易管理相關方法
- [批次操作擴充](docs/DapperBulkExtensions.md) - 批次處理大量資料

## 🎯 功能特色

- ✅ **繁體中文註解** - 所有方法都有詳細的中文說明
- ✅ **統一命名規範** - 所有擴充方法使用 `p` 前綴
- ✅ **完整的參數支援** - 支援交易、逾時設定等進階參數
- ✅ **安全的交易處理** - 自動處理 Commit 和 Rollback
- ✅ **批次操作優化** - 支援分批處理避免記憶體問題

## 📋 方法總覽

### Dapper 查詢擴充 (7 個方法)
- `pQueryFirstOrDefaultAsync<T>` - 查詢第一筆或 null
- `pQuerySingleAsync<T>` - 查詢單一筆（嚴格模式）
- `pQuerySingleOrDefaultAsync<T>` - 查詢單一筆或 null
- `pQueryListAsync<T>` - 查詢多筆資料
- `pQueryPagedAsync<T>` - 分頁查詢
- `pQueryMultipleAsync` - 多結果集查詢
- `pQueryStoredProcedureAsync<T>` - 執行預存程序查詢

### Dapper 執行擴充 (8 個方法)
- `pExecuteAsync` - 執行 SQL 命令
- `pExecuteScalarAsync<T>` - 執行並回傳單一值
- `pExecuteStoredProcedureAsync` - 執行預存程序
- `pInsertAsync` - 新增單筆資料
- `pInsertWithIdAsync<TKey>` - 新增並回傳 Id
- `pUpdateAsync` - 更新單筆資料
- `pDeleteAsync<TKey>` - 刪除單筆資料
- `pDeleteWhereAsync` - 條件刪除

### Dapper 交易擴充 (5 個方法)
- `pExecuteInTransactionAsync<T>` - 在交易中執行（有回傳值）
- `pExecuteInTransactionAsync` - 在交易中執行（無回傳值）
- `pBeginTransactionSafe` - 安全開始交易
- `pCommitSafe` - 安全提交交易
- `pExecuteMultipleInTransactionAsync` - 執行多個 SQL 命令

### Dapper 批次操作擴充 (6 個方法)
- `pBulkInsertAsync<T>` - 批次新增
- `pBulkUpdateAsync<T>` - 批次更新
- `pBulkDeleteAsync<TKey>` - 批次刪除
- `pBulkExecuteAsync` - 批次執行
- `pBulkInsertInBatchesAsync<T>` - 分批新增
- `pBulkUpdateInBatchesAsync<T>` - 分批更新

## 🔧 技術規格

- **目標框架**: .NET 8.0
- **相依套件**:
  - Dapper 2.1.35
  - System.Data.SqlClient 4.8.6

## 📝 授權

此專案為個人使用的擴充方法函式庫。
