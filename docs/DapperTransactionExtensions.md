# Dapper 交易擴充方法

本文件詳細說明所有交易相關的擴充方法。

## 什麼是資料庫交易？

交易（Transaction）是一組必須全部成功或全部失敗的資料庫操作。交易具有 ACID 特性：

- **A**tomicity（原子性）- 全部成功或全部失敗
- **C**onsistency（一致性）- 資料保持一致狀態
- **I**solation（隔離性）- 交易之間互不干擾
- **D**urability（持久性）- 提交後永久保存

### 常見使用情境

- 轉帳操作（扣款和入款必須同時成功）
- 訂單處理（建立訂單、扣庫存、建立出貨單）
- 資料同步（更新多個相關資料表）

---

## 方法列表

- [pExecuteInTransactionAsync (有回傳值)](#pexecuteintransactionasync-有回傳值) - 在交易中執行操作並回傳結果
- [pExecuteInTransactionAsync (無回傳值)](#pexecuteintransactionasync-無回傳值) - 在交易中執行操作
- [pBeginTransactionSafe](#pbegintransactionsafe) - 安全地開始交易
- [pCommitSafe](#pcommitsafe) - 安全地提交交易
- [pExecuteMultipleInTransactionAsync](#pexecutemultipleintransactionasync) - 執行多個 SQL 命令

---

## pExecuteInTransactionAsync (有回傳值)

在交易中執行操作，自動處理 Commit 和 Rollback，並回傳結果。

### 方法簽章

```csharp
public static async Task<T> pExecuteInTransactionAsync<T>(
    this IDbConnection connection, 
    Func<IDbConnection, IDbTransaction, Task<T>> action, 
    IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
```

### 參數說明

- `action` - 要在交易中執行的操作（Lambda 表達式）
- `isolationLevel` - 交易隔離等級，預設為 `ReadCommitted`

### 運作方式

1. 自動開啟連線（如果未開啟）
2. 開始交易
3. 執行您的操作
4. 成功則 Commit，失敗則 Rollback
5. 回傳結果

### 範例

```csharp
// 轉帳操作
var success = await connection.pExecuteInTransactionAsync(async (conn, trans) =>
{
    // 扣款
    var deducted = await conn.pExecuteAsync(
        "UPDATE Accounts SET Balance = Balance - @Amount WHERE Id = @FromId",
        new { FromId = 1, Amount = 1000 },
        trans);

    if (deducted == 0)
        throw new Exception("扣款失敗");

    // 入款
    var added = await conn.pExecuteAsync(
        "UPDATE Accounts SET Balance = Balance + @Amount WHERE Id = @ToId",
        new { ToId = 2, Amount = 1000 },
        trans);

    if (added == 0)
        throw new Exception("入款失敗");

    return true;  // 回傳成功
});

// 建立訂單並回傳訂單 Id
var orderId = await connection.pExecuteInTransactionAsync(async (conn, trans) =>
{
    // 建立訂單
    var newOrderId = await conn.pInsertWithIdAsync<int>(
        "Orders",
        new { UserId = 1, TotalAmount = 1000, Status = "Pending" },
        trans);

    // 扣庫存
    await conn.pExecuteAsync(
        "UPDATE Products SET Stock = Stock - @Quantity WHERE Id = @ProductId",
        new { ProductId = 1, Quantity = 2 },
        trans);

    // 建立訂單明細
    await conn.pInsertAsync(
        "OrderDetails",
        new { OrderId = newOrderId, ProductId = 1, Quantity = 2, Price = 500 },
        trans);

    return newOrderId;  // 回傳訂單 Id
});

Console.WriteLine($"訂單建立成功，Id: {orderId}");
```

---

## pExecuteInTransactionAsync (無回傳值)

在交易中執行操作，自動處理 Commit 和 Rollback，無回傳值。

### 方法簽章

```csharp
public static async Task pExecuteInTransactionAsync(
    this IDbConnection connection, 
    Func<IDbConnection, IDbTransaction, Task> action, 
    IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
```

### 範例

```csharp
// 批次更新多個資料表
await connection.pExecuteInTransactionAsync(async (conn, trans) =>
{
    await conn.pUpdateAsync("Users", user, transaction: trans);
    await conn.pUpdateAsync("UserProfiles", profile, transaction: trans);
    await conn.pInsertAsync("AuditLogs", log, transaction: trans);
});

// 資料同步
await connection.pExecuteInTransactionAsync(async (conn, trans) =>
{
    // 清空暫存表
    await conn.pExecuteAsync("TRUNCATE TABLE TempData", transaction: trans);

    // 複製資料
    await conn.pExecuteAsync(
        "INSERT INTO TempData SELECT * FROM MainData WHERE Status = @Status",
        new { Status = "Active" },
        trans);

    // 更新統計
    await conn.pExecuteAsync(
        "UPDATE Statistics SET LastSync = GETDATE()",
        transaction: trans);
});
```

---

## pBeginTransactionSafe

安全地開始交易，確保連線已開啟。

### 方法簽章

```csharp
public static IDbTransaction pBeginTransactionSafe(
    this IDbConnection connection, 
    IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
```

### 使用時機

- 需要手動控制交易的生命週期
- 需要在多個方法間共用同一個交易

### 範例

```csharp
// 手動管理交易
using var transaction = connection.pBeginTransactionSafe();
try
{
    await connection.pInsertAsync("Users", user, transaction);
    await connection.pInsertAsync("UserProfiles", profile, transaction);
    
    transaction.Commit();
}
catch
{
    transaction.Rollback();
    throw;
}

// 跨方法使用交易
using var transaction = connection.pBeginTransactionSafe();
try
{
    await CreateUser(connection, user, transaction);
    await CreateProfile(connection, profile, transaction);
    await SendWelcomeEmail(user.Email);
    
    transaction.Commit();
}
catch
{
    transaction.Rollback();
    throw;
}

async Task CreateUser(IDbConnection conn, User user, IDbTransaction trans)
{
    await conn.pInsertAsync("Users", user, trans);
}
```

---

## pCommitSafe

安全地提交交易，發生錯誤時自動 Rollback。

### 方法簽章

```csharp
public static void pCommitSafe(
    this IDbTransaction transaction, 
    bool disposeAfterCommit = true)
```

### 參數說明

- `disposeAfterCommit` - 提交後是否自動釋放交易物件，預設為 `true`

### 範例

```csharp
// 基本使用
using var transaction = connection.pBeginTransactionSafe();
await connection.pInsertAsync("Users", user, transaction);
await connection.pInsertAsync("UserProfiles", profile, transaction);
transaction.pCommitSafe();  // 自動處理錯誤

// 不自動釋放（需要繼續使用交易物件）
var transaction = connection.pBeginTransactionSafe();
await connection.pInsertAsync("Users", user, transaction);
transaction.pCommitSafe(disposeAfterCommit: false);

// 繼續使用...
transaction.Dispose();
```

---

## pExecuteMultipleInTransactionAsync

在交易中執行多個 SQL 命令。

### 方法簽章

```csharp
public static async Task<int> pExecuteMultipleInTransactionAsync(
    this IDbConnection connection, 
    IEnumerable<(string Sql, object? Param)> sqlCommands, 
    IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, 
    int? commandTimeout = 30)
```

### 參數說明

- `sqlCommands` - SQL 命令列表，每個元素包含 SQL 和參數

### 回傳值

所有命令受影響的總資料列數

### 範例

```csharp
// 執行多個 SQL 命令
var commands = new[]
{
    ("INSERT INTO Logs (Message) VALUES (@Msg)", new { Msg = "開始處理" }),
    ("UPDATE Users SET Status = @Status WHERE Id = @Id", new { Id = 1, Status = "Processing" }),
    ("INSERT INTO Logs (Message) VALUES (@Msg)", new { Msg = "處理完成" })
};

var totalAffected = await connection.pExecuteMultipleInTransactionAsync(commands);
Console.WriteLine($"總共影響 {totalAffected} 筆資料");

// 動態建立命令列表
var userIds = new[] { 1, 2, 3, 4, 5 };
var commands = userIds.Select(id => 
    ("UPDATE Users SET LastUpdate = GETDATE() WHERE Id = @Id", 
     new { Id = id } as object)
).ToList();

await connection.pExecuteMultipleInTransactionAsync(commands);
```

---

## 交易隔離等級

### 常用的隔離等級

| 隔離等級 | 說明 | 使用時機 |
|---------|------|---------|
| `ReadUncommitted` | 可讀取未提交的資料（髒讀） | 不建議使用 |
| `ReadCommitted` | 只能讀取已提交的資料（預設） | 一般情況 |
| `RepeatableRead` | 確保重複讀取結果一致 | 需要一致性讀取 |
| `Serializable` | 最高隔離等級，完全隔離 | 關鍵交易 |
| `Snapshot` | 使用快照隔離 | SQL Server 特有 |

### 範例

```csharp
// 使用 Serializable 隔離等級
await connection.pExecuteInTransactionAsync(async (conn, trans) =>
{
    // 執行關鍵操作
}, IsolationLevel.Serializable);

// 使用 RepeatableRead
await connection.pExecuteInTransactionAsync(async (conn, trans) =>
{
    var count1 = await conn.pExecuteScalarAsync<int>(
        "SELECT COUNT(*) FROM Users", transaction: trans);
    
    // 其他操作...
    
    var count2 = await conn.pExecuteScalarAsync<int>(
        "SELECT COUNT(*) FROM Users", transaction: trans);
    
    // count1 和 count2 保證相同
}, IsolationLevel.RepeatableRead);
```

---

## 最佳實踐

### 1. 優先使用 pExecuteInTransactionAsync

這是最簡單且最安全的方式：

```csharp
// ✅ 推薦
await connection.pExecuteInTransactionAsync(async (conn, trans) =>
{
    // 您的操作
});

// ❌ 不推薦（除非有特殊需求）
using var transaction = connection.BeginTransaction();
try
{
    // 您的操作
    transaction.Commit();
}
catch
{
    transaction.Rollback();
    throw;
}
```

### 2. 保持交易簡短

```csharp
// ✅ 好：交易只包含資料庫操作
await connection.pExecuteInTransactionAsync(async (conn, trans) =>
{
    await conn.pInsertAsync("Orders", order, trans);
    await conn.pUpdateAsync("Products", product, transaction: trans);
});

// ❌ 不好：交易包含外部 API 呼叫
await connection.pExecuteInTransactionAsync(async (conn, trans) =>
{
    await conn.pInsertAsync("Orders", order, trans);
    await SendEmailAsync(order.Email);  // 外部操作，會延長交易時間
});
```

### 3. 適當的錯誤處理

```csharp
try
{
    await connection.pExecuteInTransactionAsync(async (conn, trans) =>
    {
        // 您的操作
    });
}
catch (SqlException ex)
{
    _logger.LogError(ex, "資料庫交易失敗");
    throw;
}
catch (Exception ex)
{
    _logger.LogError(ex, "交易執行失敗");
    throw;
}
```

### 4. 避免巢狀交易

```csharp
// ❌ 避免巢狀交易
await connection.pExecuteInTransactionAsync(async (conn, trans) =>
{
    await conn.pExecuteInTransactionAsync(async (conn2, trans2) =>
    {
        // 巢狀交易可能導致問題
    });
});

// ✅ 使用單一交易
await connection.pExecuteInTransactionAsync(async (conn, trans) =>
{
    // 所有操作在同一個交易中
});
```
