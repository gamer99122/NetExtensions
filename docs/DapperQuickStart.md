# Dapper 快速入門

本文件提供 Dapper 的基本概念與使用說明，適合不熟悉 Dapper 的開發者閱讀。

## 什麼是 Dapper？

Dapper 是一個輕量級的 ORM（Object-Relational Mapping）框架，由 Stack Overflow 團隊開發。它的特色是：

- ⚡ **高效能** - 幾乎與原生 ADO.NET 一樣快
- 🎯 **簡單易用** - API 簡潔直觀
- 🔧 **靈活** - 可以寫原生 SQL，完全掌控查詢
- 📦 **輕量** - 只有一個 DLL 檔案

## 基本概念

### 1. 連線管理

Dapper 是 `IDbConnection` 的擴充方法，所以需要先建立資料庫連線：

```csharp
using System.Data.SqlClient;

// 建立連線
using var connection = new SqlConnection(connectionString);

// Dapper 會自動開啟連線（如果需要）
var users = await connection.QueryAsync<User>("SELECT * FROM Users");
```

### 2. 參數化查詢

**永遠使用參數化查詢**以避免 SQL Injection：

```csharp
// ✅ 正確：使用參數
var user = await connection.QueryFirstOrDefaultAsync<User>(
    "SELECT * FROM Users WHERE Id = @Id",
    new { Id = 1 });

// ❌ 錯誤：字串拼接（有 SQL Injection 風險）
var user = await connection.QueryFirstOrDefaultAsync<User>(
    $"SELECT * FROM Users WHERE Id = {userId}");
```

### 3. 物件對應

Dapper 會自動將查詢結果對應到 C# 物件：

```csharp
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}

// Dapper 會自動將資料庫欄位對應到屬性
var user = await connection.QueryFirstOrDefaultAsync<User>(
    "SELECT Id, Name, Email FROM Users WHERE Id = @Id",
    new { Id = 1 });
```

## NetExtensions 的 Dapper 擴充方法

我們在原生 Dapper 的基礎上，新增了更方便的擴充方法，所有方法都以 `p` 開頭。

### 常用方法對照表

| 原生 Dapper | NetExtensions 擴充 | 說明 |
|------------|-------------------|------|
| `QueryFirstOrDefaultAsync<T>` | `pQueryFirstOrDefaultAsync<T>` | 查詢第一筆或 null |
| `QueryAsync<T>` | `pQueryListAsync<T>` | 查詢多筆並回傳 List |
| `ExecuteAsync` | `pExecuteAsync` | 執行 SQL 命令 |
| `ExecuteScalarAsync<T>` | `pExecuteScalarAsync<T>` | 執行並回傳單一值 |
| - | `pInsertAsync` | 新增單筆資料（自動產生 SQL） |
| - | `pUpdateAsync` | 更新單筆資料（自動產生 SQL） |
| - | `pDeleteAsync<TKey>` | 刪除單筆資料 |
| - | `pBulkInsertAsync<T>` | 批次新增多筆資料 |
| - | `pExecuteInTransactionAsync` | 在交易中執行操作 |

## 使用範例

### 查詢資料

```csharp
// 查詢單筆資料
var user = await connection.pQueryFirstOrDefaultAsync<User>(
    "SELECT * FROM Users WHERE Id = @Id",
    new { Id = 1 });

// 查詢多筆資料
var users = await connection.pQueryListAsync<User>(
    "SELECT * FROM Users WHERE Status = @Status",
    new { Status = "Active" });

// 分頁查詢
var (data, totalCount) = await connection.pQueryPagedAsync<User>(
    "SELECT * FROM Users WHERE Status = @Status",
    pageNumber: 1,
    pageSize: 10,
    param: new { Status = "Active" },
    orderBy: "CreateDate DESC");

Console.WriteLine($"總共 {totalCount} 筆，目前顯示第 1 頁");
```

### 新增資料

```csharp
// 方式 1: 使用擴充方法（自動產生 SQL）
var newId = await connection.pInsertWithIdAsync<int>(
    "Users",
    new { Name = "John", Email = "john@example.com" });

// 方式 2: 自己寫 SQL
await connection.pExecuteAsync(
    "INSERT INTO Users (Name, Email) VALUES (@Name, @Email)",
    new { Name = "John", Email = "john@example.com" });
```

### 更新資料

```csharp
// 使用擴充方法
await connection.pUpdateAsync(
    "Users",
    new { Id = 1, Name = "John Updated", Email = "john@example.com" });

// 自己寫 SQL
await connection.pExecuteAsync(
    "UPDATE Users SET Name = @Name WHERE Id = @Id",
    new { Id = 1, Name = "John Updated" });
```

### 刪除資料

```csharp
// 刪除單筆
await connection.pDeleteAsync<int>("Users", 1);

// 條件刪除
await connection.pDeleteWhereAsync(
    "Users",
    "Status = @Status AND CreateDate < @Date",
    new { Status = "Inactive", Date = DateTime.Now.AddYears(-1) });
```

### 交易處理

```csharp
// 自動管理交易（推薦）
await connection.pExecuteInTransactionAsync(async (conn, trans) =>
{
    // 新增訂單
    var orderId = await conn.pInsertWithIdAsync<int>(
        "Orders",
        new { UserId = 1, TotalAmount = 1000 },
        trans);

    // 更新庫存
    await conn.pExecuteAsync(
        "UPDATE Products SET Stock = Stock - @Quantity WHERE Id = @ProductId",
        new { ProductId = 1, Quantity = 2 },
        trans);

    // 如果發生例外，會自動 Rollback
});
```

### 批次操作

```csharp
// 批次新增
var users = new List<User>
{
    new User { Name = "User1", Email = "user1@example.com" },
    new User { Name = "User2", Email = "user2@example.com" },
    // ... 更多資料
};

await connection.pBulkInsertAsync("Users", users);

// 大量資料分批處理（避免記憶體問題）
await connection.pBulkInsertInBatchesAsync(
    "Users",
    largeUserList,
    batchSize: 1000);  // 每次處理 1000 筆
```

## 常見問題

### Q1: 什麼時候需要手動開啟連線？

A: Dapper 會在需要時自動開啟連線，但如果要在同一個連線中執行多個操作（特別是交易），建議手動開啟：

```csharp
using var connection = new SqlConnection(connectionString);
connection.Open();  // 手動開啟

using var transaction = connection.BeginTransaction();
// ... 執行多個操作
```

### Q2: 如何處理 NULL 值？

A: 使用可為 null 的型別：

```csharp
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string? Email { get; set; }  // 可為 null
    public DateTime? LastLoginDate { get; set; }  // 可為 null
}
```

### Q3: 如何執行預存程序？

A: 使用 `pQueryStoredProcedureAsync` 或 `pExecuteStoredProcedureAsync`：

```csharp
var result = await connection.pQueryStoredProcedureAsync<User>(
    "sp_GetUserById",
    new { UserId = 1 });
```

### Q4: 效能考量

- 使用 `async` 方法避免阻塞執行緒
- 大量資料使用批次方法（`pBulkInsertInBatchesAsync`）
- 適當設定 `commandTimeout` 避免長時間查詢
- 使用連線池（預設已啟用）

## 下一步

- [查詢擴充方法詳細說明](DapperQueryExtensions.md)
- [執行擴充方法詳細說明](DapperExecuteExtensions.md)
- [交易擴充方法詳細說明](DapperTransactionExtensions.md)
- [批次操作擴充方法詳細說明](DapperBulkExtensions.md)
