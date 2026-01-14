# Dapper 批次操作擴充方法

本文件詳細說明所有批次操作相關的擴充方法，用於處理大量資料。

## 為什麼需要批次操作？

當需要處理大量資料時，使用批次操作可以：

- ⚡ **提升效能** - 減少資料庫往返次數
- 💾 **節省資源** - 減少網路傳輸和記憶體使用
- 🔒 **保持一致性** - 在同一個操作中處理多筆資料

### 效能比較

```csharp
// ❌ 慢：迴圈中執行單筆操作（1000 次資料庫往返）
foreach (var user in users)
{
    await connection.pInsertAsync("Users", user);
}

// ✅ 快：批次操作（1 次資料庫往返）
await connection.pBulkInsertAsync("Users", users);
```

---

## 方法列表

> **使用頻率**：🔥 常用 | ⚡ 中頻 | 🔹 少用

- ⚡ [pBulkInsertAsync](#pbulkinsertasync) - 批次新增
- 🔹 [pBulkUpdateAsync](#pbulkupdateasync) - 批次更新
- ⚡ [pBulkDeleteAsync](#pbulkdeleteasync) - 批次刪除
- 🔹 [pBulkExecuteAsync](#pbulkexecuteasync) - 批次執行
- 🔹 [pBulkInsertInBatchesAsync](#pbulkinsertinbatchesasync) - 分批新增
- 🔹 [pBulkUpdateInBatchesAsync](#pbulkupdateinbatchesasync) - 分批更新

---

## pBulkInsertAsync

批次新增多筆資料。

### 方法簽章

```csharp
public static async Task<int> pBulkInsertAsync<T>(
    this IDbConnection connection, 
    string tableName, 
    IEnumerable<T> entities, 
    IDbTransaction? transaction = null, 
    int? commandTimeout = 30)
```

### 參數說明

- `entities` - 要新增的實體列表

### 注意事項

- 會自動排除名為 `Id` 的屬性
- 所有實體必須有相同的屬性結構

### 範例

```csharp
// 批次新增使用者
var users = new List<User>
{
    new User { Name = "User1", Email = "user1@example.com" },
    new User { Name = "User2", Email = "user2@example.com" },
    new User { Name = "User3", Email = "user3@example.com" }
};

var affected = await connection.pBulkInsertAsync("Users", users);
Console.WriteLine($"新增了 {affected} 筆資料");

// 使用匿名物件
var orders = new[]
{
    new { UserId = 1, TotalAmount = 1000, Status = "Pending" },
    new { UserId = 2, TotalAmount = 2000, Status = "Pending" },
    new { UserId = 3, TotalAmount = 1500, Status = "Pending" }
};

await connection.pBulkInsertAsync("Orders", orders);

// 在交易中使用
await connection.pExecuteInTransactionAsync(async (conn, trans) =>
{
    await conn.pBulkInsertAsync("Users", users, trans);
    await conn.pBulkInsertAsync("UserProfiles", profiles, trans);
});
```

---

## pBulkUpdateAsync

批次更新多筆資料。

### 方法簽章

```csharp
public static async Task<int> pBulkUpdateAsync<T>(
    this IDbConnection connection, 
    string tableName, 
    IEnumerable<T> entities, 
    string keyColumn = "Id", 
    IDbTransaction? transaction = null, 
    int? commandTimeout = 30)
```

### 參數說明

- `keyColumn` - 主鍵欄位名稱，預設為 "Id"

### 範例

```csharp
// 批次更新使用者狀態
var users = await connection.pQueryListAsync<User>(
    "SELECT * FROM Users WHERE Status = @Status",
    new { Status = "Pending" });

foreach (var user in users)
{
    user.Status = "Active";
    user.UpdateDate = DateTime.Now;
}

var affected = await connection.pBulkUpdateAsync("Users", users);
Console.WriteLine($"更新了 {affected} 筆資料");

// 使用自訂主鍵
var products = new[]
{
    new { ProductCode = "P001", Name = "Product 1", Price = 100 },
    new { ProductCode = "P002", Name = "Product 2", Price = 200 }
};

await connection.pBulkUpdateAsync("Products", products, keyColumn: "ProductCode");
```

---

## pBulkDeleteAsync

批次刪除多筆資料（根據 Id 列表）。

### 方法簽章

```csharp
public static async Task<int> pBulkDeleteAsync<TKey>(
    this IDbConnection connection, 
    string tableName, 
    IEnumerable<TKey> ids, 
    string keyColumn = "Id", 
    IDbTransaction? transaction = null, 
    int? commandTimeout = 30)
```

### 參數說明

- `TKey` - 主鍵的型別
- `ids` - 要刪除的 Id 列表

### 範例

```csharp
// 批次刪除使用者
var idsToDelete = new[] { 1, 2, 3, 4, 5 };
var affected = await connection.pBulkDeleteAsync<int>("Users", idsToDelete);
Console.WriteLine($"刪除了 {affected} 筆資料");

// 刪除大量資料
var inactiveUserIds = await connection.pQueryListAsync<int>(
    "SELECT Id FROM Users WHERE Status = @Status AND LastLoginDate < @Date",
    new { Status = "Inactive", Date = DateTime.Now.AddYears(-1) });

await connection.pBulkDeleteAsync<int>("Users", inactiveUserIds);

// 使用 long 型別的 Id
var orderIds = new[] { 1001L, 1002L, 1003L };
await connection.pBulkDeleteAsync<long>("Orders", orderIds);

// 使用自訂主鍵
var productCodes = new[] { "P001", "P002", "P003" };
await connection.pBulkDeleteAsync<string>("Products", productCodes, keyColumn: "ProductCode");
```

---

## pBulkExecuteAsync

批次執行多個 SQL 命令（使用相同的 SQL，不同的參數）。

### 方法簽章

```csharp
public static async Task<int> pBulkExecuteAsync(
    this IDbConnection connection, 
    string sql, 
    IEnumerable<object> parameters, 
    IDbTransaction? transaction = null, 
    int? commandTimeout = 30)
```

### 參數說明

- `sql` - SQL 命令語句（相同的 SQL）
- `parameters` - 參數列表，每個參數對應一次執行

### 範例

```csharp
// 批次更新多個使用者的狀態
var sql = "UPDATE Users SET Status = @Status WHERE Id = @Id";
var parameters = new[]
{
    new { Id = 1, Status = "Active" },
    new { Id = 2, Status = "Active" },
    new { Id = 3, Status = "Inactive" }
};

var affected = await connection.pBulkExecuteAsync(sql, parameters);

// 批次插入日誌
var sql = "INSERT INTO Logs (Message, Level, CreateDate) VALUES (@Msg, @Level, @Date)";
var logs = Enumerable.Range(1, 100).Select(i => new
{
    Msg = $"Log message {i}",
    Level = "Info",
    Date = DateTime.Now
});

await connection.pBulkExecuteAsync(sql, logs);
```

---

## pBulkInsertInBatchesAsync

批次新增資料（分批處理，避免一次新增太多筆）。

### 方法簽章

```csharp
public static async Task<int> pBulkInsertInBatchesAsync<T>(
    this IDbConnection connection, 
    string tableName, 
    IEnumerable<T> entities, 
    int batchSize = 1000, 
    IDbTransaction? transaction = null, 
    int? commandTimeout = 30)
```

### 參數說明

- `batchSize` - 每批次處理的筆數，預設 1000

### 使用時機

- 資料量非常大（數萬筆以上）
- 避免記憶體問題
- 避免交易鎖定時間過長

### 範例

```csharp
// 新增 10 萬筆資料，每次處理 1000 筆
var largeUserList = Enumerable.Range(1, 100000).Select(i => new User
{
    Name = $"User{i}",
    Email = $"user{i}@example.com",
    Status = "Active"
}).ToList();

var affected = await connection.pBulkInsertInBatchesAsync(
    "Users",
    largeUserList,
    batchSize: 1000);

Console.WriteLine($"總共新增了 {affected} 筆資料");

// 自訂批次大小
await connection.pBulkInsertInBatchesAsync(
    "Orders",
    orders,
    batchSize: 500);  // 每次處理 500 筆

// 在交易中使用（注意：大量資料可能導致鎖定時間過長）
await connection.pExecuteInTransactionAsync(async (conn, trans) =>
{
    await conn.pBulkInsertInBatchesAsync("Users", users, batchSize: 1000, transaction: trans);
});
```

---

## pBulkUpdateInBatchesAsync

批次更新資料（分批處理，避免一次更新太多筆）。

### 方法簽章

```csharp
public static async Task<int> pBulkUpdateInBatchesAsync<T>(
    this IDbConnection connection, 
    string tableName, 
    IEnumerable<T> entities, 
    int batchSize = 1000, 
    string keyColumn = "Id", 
    IDbTransaction? transaction = null, 
    int? commandTimeout = 30)
```

### 範例

```csharp
// 更新大量使用者資料
var users = await connection.pQueryListAsync<User>(
    "SELECT * FROM Users WHERE Status = @Status",
    new { Status = "Pending" });

foreach (var user in users)
{
    user.Status = "Active";
    user.UpdateDate = DateTime.Now;
}

var affected = await connection.pBulkUpdateInBatchesAsync(
    "Users",
    users,
    batchSize: 1000);

Console.WriteLine($"總共更新了 {affected} 筆資料");

// 自訂批次大小和主鍵
await connection.pBulkUpdateInBatchesAsync(
    "Products",
    products,
    batchSize: 500,
    keyColumn: "ProductCode");
```

---

## 效能最佳化建議

### 1. 選擇適當的批次大小

```csharp
// 資料量小（< 1000 筆）：直接使用批次方法
if (users.Count < 1000)
{
    await connection.pBulkInsertAsync("Users", users);
}
// 資料量大（>= 1000 筆）：使用分批方法
else
{
    await connection.pBulkInsertInBatchesAsync("Users", users, batchSize: 1000);
}
```

### 2. 批次大小的選擇

| 資料量 | 建議批次大小 | 說明 |
|-------|------------|------|
| < 100 | 不分批 | 直接使用 `pBulkInsertAsync` |
| 100 - 1,000 | 100 - 500 | 小批次 |
| 1,000 - 10,000 | 500 - 1,000 | 中批次 |
| 10,000 - 100,000 | 1,000 - 2,000 | 大批次 |
| > 100,000 | 1,000 - 5,000 | 超大批次，考慮背景處理 |

### 3. 使用交易（但要注意鎖定）

```csharp
// ✅ 好：中等資料量使用交易
await connection.pExecuteInTransactionAsync(async (conn, trans) =>
{
    await conn.pBulkInsertAsync("Users", users, trans);  // 1000 筆
});

// ⚠️ 注意：大量資料可能導致長時間鎖定
await connection.pExecuteInTransactionAsync(async (conn, trans) =>
{
    await conn.pBulkInsertInBatchesAsync("Users", users, batchSize: 1000, transaction: trans);  // 100,000 筆
});
```

### 4. 監控進度

```csharp
// 顯示進度
var totalUsers = largeUserList.Count;
var batchSize = 1000;
var batches = (int)Math.Ceiling(totalUsers / (double)batchSize);

Console.WriteLine($"開始處理 {totalUsers} 筆資料，分 {batches} 批次");

var processedCount = 0;
var userBatches = largeUserList
    .Select((user, index) => new { user, index })
    .GroupBy(x => x.index / batchSize)
    .Select(g => g.Select(x => x.user).ToList());

foreach (var batch in userBatches)
{
    await connection.pBulkInsertAsync("Users", batch);
    processedCount += batch.Count;
    Console.WriteLine($"已處理 {processedCount}/{totalUsers} ({processedCount * 100 / totalUsers}%)");
}
```

---

## 常見使用情境

### 情境 1: 資料匯入

```csharp
// 從 CSV 匯入大量資料
var csvUsers = ReadUsersFromCsv("users.csv");  // 假設有 50,000 筆

await connection.pBulkInsertInBatchesAsync(
    "Users",
    csvUsers,
    batchSize: 1000);
```

### 情境 2: 批次更新狀態

```csharp
// 將所有過期的訂單標記為已取消
var expiredOrders = await connection.pQueryListAsync<Order>(
    "SELECT * FROM Orders WHERE Status = @Status AND CreateDate < @Date",
    new { Status = "Pending", Date = DateTime.Now.AddDays(-7) });

foreach (var order in expiredOrders)
{
    order.Status = "Cancelled";
    order.UpdateDate = DateTime.Now;
}

await connection.pBulkUpdateAsync("Orders", expiredOrders);
```

### 情境 3: 資料清理

```csharp
// 刪除 1 年前的日誌
var oldLogIds = await connection.pQueryListAsync<int>(
    "SELECT Id FROM Logs WHERE CreateDate < @Date",
    new { Date = DateTime.Now.AddYears(-1) });

Console.WriteLine($"找到 {oldLogIds.Count} 筆舊日誌");

await connection.pBulkDeleteAsync<int>("Logs", oldLogIds);
```

### 情境 4: 資料同步

```csharp
// 從外部 API 同步資料
var externalUsers = await FetchUsersFromExternalApi();

await connection.pExecuteInTransactionAsync(async (conn, trans) =>
{
    // 清空暫存表
    await conn.pExecuteAsync("TRUNCATE TABLE TempUsers", transaction: trans);
    
    // 批次新增到暫存表
    await conn.pBulkInsertAsync("TempUsers", externalUsers, trans);
    
    // 合併到主表
    await conn.pExecuteAsync(@"
        MERGE INTO Users AS target
        USING TempUsers AS source
        ON target.ExternalId = source.ExternalId
        WHEN MATCHED THEN UPDATE SET Name = source.Name, Email = source.Email
        WHEN NOT MATCHED THEN INSERT (ExternalId, Name, Email) VALUES (source.ExternalId, source.Name, source.Email);
    ", transaction: trans);
});
```

---

## 注意事項

1. **記憶體使用**
   - 大量資料建議使用 `IEnumerable<T>` 而非 `List<T>`
   - 使用分批方法避免一次載入所有資料到記憶體

2. **交易鎖定**
   - 批次操作會鎖定資料表
   - 大量資料可能導致長時間鎖定
   - 考慮在離峰時間執行

3. **錯誤處理**
   - 批次操作失敗時，整批資料都不會被處理
   - 考慮使用 try-catch 並記錄錯誤

4. **索引影響**
   - 大量新增/更新會影響索引重建
   - 考慮暫時停用索引（非常大量資料時）
