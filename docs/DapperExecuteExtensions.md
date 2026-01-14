# Dapper 執行擴充方法

本文件詳細說明所有執行相關的擴充方法（INSERT、UPDATE、DELETE 等操作）。

## 方法列表

- [pExecuteAsync](#pexecuteasync) - 執行 SQL 命令
- [pExecuteScalarAsync](#pexecutescalarasync) - 執行並回傳單一值
- [pExecuteStoredProcedureAsync](#pexecutestoredprocedureasync) - 執行預存程序
- [pInsertAsync](#pinsertasync) - 新增單筆資料
- [pInsertWithIdAsync](#pinsertwitidasync) - 新增並回傳 Id
- [pUpdateAsync](#pupdateasync) - 更新單筆資料
- [pDeleteAsync](#pdeleteasync) - 刪除單筆資料
- [pDeleteWhereAsync](#pdeletewhereasync) - 條件刪除

---

## pExecuteAsync

執行 SQL 命令（INSERT、UPDATE、DELETE 等），回傳受影響的資料列數。

### 方法簽章

```csharp
public static async Task<int> pExecuteAsync(
    this IDbConnection connection, 
    string sql, 
    object? param = null, 
    IDbTransaction? transaction = null, 
    int? commandTimeout = 30)
```

### 使用時機

- 執行自訂的 INSERT、UPDATE、DELETE 語句
- 需要完全控制 SQL 語句時使用

### 範例

```csharp
// INSERT
var affected = await connection.pExecuteAsync(
    "INSERT INTO Users (Name, Email) VALUES (@Name, @Email)",
    new { Name = "John", Email = "john@example.com" });

// UPDATE
var affected = await connection.pExecuteAsync(
    "UPDATE Users SET Status = @Status WHERE Id = @Id",
    new { Id = 1, Status = "Inactive" });

// DELETE
var affected = await connection.pExecuteAsync(
    "DELETE FROM Users WHERE Id = @Id",
    new { Id = 1 });

// 複雜的更新
var affected = await connection.pExecuteAsync(
    @"UPDATE Orders 
      SET Status = @NewStatus, UpdateDate = GETDATE() 
      WHERE UserId = @UserId AND Status = @OldStatus",
    new { UserId = 1, OldStatus = "Pending", NewStatus = "Processing" });
```

---

## pExecuteScalarAsync

執行 SQL 命令並回傳單一值（例如：COUNT、SUM、MAX 等）。

### 方法簽章

```csharp
public static async Task<T> pExecuteScalarAsync<T>(
    this IDbConnection connection, 
    string sql, 
    object? param = null, 
    IDbTransaction? transaction = null, 
    int? commandTimeout = 30)
```

### 使用時機

- 取得聚合函數結果（COUNT、SUM、AVG、MAX、MIN）
- 取得單一欄位值
- 取得新增後的 IDENTITY 值

### 範例

```csharp
// COUNT
var userCount = await connection.pExecuteScalarAsync<int>(
    "SELECT COUNT(*) FROM Users WHERE Status = @Status",
    new { Status = "Active" });

// SUM
var totalAmount = await connection.pExecuteScalarAsync<decimal>(
    "SELECT SUM(Amount) FROM Orders WHERE UserId = @UserId",
    new { UserId = 1 });

// MAX
var maxId = await connection.pExecuteScalarAsync<int>(
    "SELECT MAX(Id) FROM Users");

// 檢查是否存在
var exists = await connection.pExecuteScalarAsync<bool>(
    "SELECT CASE WHEN EXISTS(SELECT 1 FROM Users WHERE Email = @Email) THEN 1 ELSE 0 END",
    new { Email = "test@example.com" });

// 取得新增後的 Id
var newId = await connection.pExecuteScalarAsync<int>(
    @"INSERT INTO Users (Name, Email) VALUES (@Name, @Email);
      SELECT CAST(SCOPE_IDENTITY() AS INT);",
    new { Name = "John", Email = "john@example.com" });
```

---

## pExecuteStoredProcedureAsync

執行預存程序。

### 方法簽章

```csharp
public static async Task<int> pExecuteStoredProcedureAsync(
    this IDbConnection connection, 
    string storedProcedure, 
    object? param = null, 
    IDbTransaction? transaction = null, 
    int? commandTimeout = 30)
```

### 範例

```csharp
// 執行預存程序
var affected = await connection.pExecuteStoredProcedureAsync(
    "sp_UpdateUserStatus",
    new { UserId = 1, Status = "Active" });

// 帶有多個參數
var affected = await connection.pExecuteStoredProcedureAsync(
    "sp_ProcessOrder",
    new 
    { 
        OrderId = 123,
        Action = "Complete",
        ProcessedBy = "Admin"
    });
```

---

## pInsertAsync

新增單筆資料（自動產生 INSERT SQL）。

### 方法簽章

```csharp
public static async Task<int> pInsertAsync(
    this IDbConnection connection, 
    string tableName, 
    object entity, 
    IDbTransaction? transaction = null, 
    int? commandTimeout = 30)
```

### 參數說明

- `tableName` - 資料表名稱
- `entity` - 要新增的實體物件

### 注意事項

- 會自動排除名為 `Id` 的屬性（假設為自動產生的主鍵）
- 其他所有屬性都會被插入

### 範例

```csharp
// 使用匿名物件
var affected = await connection.pInsertAsync(
    "Users",
    new { Name = "John", Email = "john@example.com", Status = "Active" });

// 使用實體類別
var user = new User 
{ 
    Name = "Jane", 
    Email = "jane@example.com",
    Status = "Active"
};
var affected = await connection.pInsertAsync("Users", user);

// 在交易中使用
using var transaction = connection.pBeginTransactionSafe();
await connection.pInsertAsync("Users", user, transaction);
await connection.pInsertAsync("UserProfiles", profile, transaction);
transaction.pCommitSafe();
```

---

## pInsertWithIdAsync

新增單筆資料並回傳新增的 Id。

### 方法簽章

```csharp
public static async Task<TKey> pInsertWithIdAsync<TKey>(
    this IDbConnection connection, 
    string tableName, 
    object entity, 
    IDbTransaction? transaction = null, 
    int? commandTimeout = 30)
```

### 參數說明

- `TKey` - Id 的型別（int、long 等）

### 範例

```csharp
// 新增並取得 int 型別的 Id
var newId = await connection.pInsertWithIdAsync<int>(
    "Users",
    new { Name = "John", Email = "john@example.com" });

Console.WriteLine($"新增成功，Id: {newId}");

// 新增並取得 long 型別的 Id
var newId = await connection.pInsertWithIdAsync<long>(
    "Orders",
    new { UserId = 1, TotalAmount = 1000 });

// 使用回傳的 Id 繼續操作
var userId = await connection.pInsertWithIdAsync<int>(
    "Users",
    new { Name = "John", Email = "john@example.com" });

await connection.pInsertAsync(
    "UserProfiles",
    new { UserId = userId, Bio = "Hello" });
```

---

## pUpdateAsync

更新單筆資料（自動產生 UPDATE SQL）。

### 方法簽章

```csharp
public static async Task<int> pUpdateAsync(
    this IDbConnection connection, 
    string tableName, 
    object entity, 
    string keyColumn = "Id", 
    IDbTransaction? transaction = null, 
    int? commandTimeout = 30)
```

### 參數說明

- `keyColumn` - 主鍵欄位名稱，預設為 "Id"

### 注意事項

- 會自動排除主鍵欄位（不會更新主鍵）
- 其他所有屬性都會被更新

### 範例

```csharp
// 基本更新（使用預設主鍵 "Id"）
var affected = await connection.pUpdateAsync(
    "Users",
    new { Id = 1, Name = "John Updated", Email = "john.new@example.com" });

// 使用自訂主鍵欄位
var affected = await connection.pUpdateAsync(
    "Products",
    new { ProductCode = "P001", Name = "New Name", Price = 99.99 },
    keyColumn: "ProductCode");

// 使用實體類別
var user = await connection.pQueryFirstOrDefaultAsync<User>(
    "SELECT * FROM Users WHERE Id = @Id",
    new { Id = 1 });

user.Name = "Updated Name";
user.Email = "updated@example.com";

var affected = await connection.pUpdateAsync("Users", user);
```

---

## pDeleteAsync

刪除單筆資料（根據 Id）。

### 方法簽章

```csharp
public static async Task<int> pDeleteAsync<TKey>(
    this IDbConnection connection, 
    string tableName, 
    TKey id, 
    string keyColumn = "Id", 
    IDbTransaction? transaction = null, 
    int? commandTimeout = 30)
```

### 參數說明

- `TKey` - 主鍵的型別
- `id` - 要刪除的資料 Id
- `keyColumn` - 主鍵欄位名稱，預設為 "Id"

### 範例

```csharp
// 刪除單筆資料（int 型別 Id）
var affected = await connection.pDeleteAsync<int>("Users", 1);

// 刪除單筆資料（long 型別 Id）
var affected = await connection.pDeleteAsync<long>("Orders", 12345L);

// 使用自訂主鍵欄位
var affected = await connection.pDeleteAsync<string>(
    "Products", 
    "P001", 
    keyColumn: "ProductCode");

// 檢查是否刪除成功
var affected = await connection.pDeleteAsync<int>("Users", 1);
if (affected > 0)
{
    Console.WriteLine("刪除成功");
}
else
{
    Console.WriteLine("找不到要刪除的資料");
}
```

---

## pDeleteWhereAsync

刪除符合條件的資料。

### 方法簽章

```csharp
public static async Task<int> pDeleteWhereAsync(
    this IDbConnection connection, 
    string tableName, 
    string whereClause, 
    object? param = null, 
    IDbTransaction? transaction = null, 
    int? commandTimeout = 30)
```

### 參數說明

- `whereClause` - WHERE 條件（不含 "WHERE" 關鍵字）

### 範例

```csharp
// 刪除符合單一條件的資料
var affected = await connection.pDeleteWhereAsync(
    "Users",
    "Status = @Status",
    new { Status = "Inactive" });

// 刪除符合多個條件的資料
var affected = await connection.pDeleteWhereAsync(
    "Orders",
    "Status = @Status AND CreateDate < @Date",
    new { Status = "Cancelled", Date = DateTime.Now.AddYears(-1) });

// 刪除所有資料（小心使用！）
var affected = await connection.pDeleteWhereAsync(
    "TempLogs",
    "1 = 1");  // 永遠為真的條件

// 使用 IN 條件
var affected = await connection.pDeleteWhereAsync(
    "Users",
    "Id IN @Ids",
    new { Ids = new[] { 1, 2, 3, 4, 5 } });
```

---

## 最佳實踐

### 1. 使用交易

多個相關操作應該在交易中執行：

```csharp
await connection.pExecuteInTransactionAsync(async (conn, trans) =>
{
    var userId = await conn.pInsertWithIdAsync<int>(
        "Users",
        new { Name = "John", Email = "john@example.com" },
        trans);

    await conn.pInsertAsync(
        "UserProfiles",
        new { UserId = userId, Bio = "Hello" },
        trans);
});
```

### 2. 檢查受影響的資料列數

```csharp
var affected = await connection.pUpdateAsync("Users", user);
if (affected == 0)
{
    throw new Exception("找不到要更新的資料");
}
```

### 3. 使用適當的方法

- 簡單的 CRUD：使用 `pInsertAsync`、`pUpdateAsync`、`pDeleteAsync`
- 複雜的 SQL：使用 `pExecuteAsync`
- 需要回傳值：使用 `pExecuteScalarAsync` 或 `pInsertWithIdAsync`

### 4. 效能考量

- 大量資料使用批次方法（參考 [DapperBulkExtensions.md](DapperBulkExtensions.md)）
- 避免在迴圈中執行單筆操作
- 適當使用索引
