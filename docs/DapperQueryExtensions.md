# Dapper 查詢擴充方法

本文件詳細說明所有查詢相關的擴充方法。

## 方法列表

> **使用頻率**：🔥 常用 | ⚡ 中頻 | 🔹 少用

- 🔥 [pQueryFirstOrDefaultAsync](#pqueryfirstordefaultasync) - 查詢第一筆或 null
- 🔹 [pQuerySingleAsync](#pquerysingleasync) - 查詢單一筆（嚴格模式）
- 🔥 [pQuerySingleOrDefaultAsync](#pquerysingleordefaultasync) - 查詢單一筆或 null
- 🔥 [pQueryListAsync](#pquerylistasync) - 查詢多筆資料
- 🔥 [pQueryPagedAsync](#pquerypagedasync) - 分頁查詢
- ⚡ [pQueryMultipleAsync](#pquerymultipleasync) - 多結果集查詢
- 🔹 [pQueryStoredProcedureAsync](#pquerystoredprocedureasync) - 執行預存程序查詢

---

## pQueryFirstOrDefaultAsync

查詢第一筆資料，如果沒有資料則回傳 `null`。

### 方法簽章

```csharp
public static async Task<T?> pQueryFirstOrDefaultAsync<T>(
    this IDbConnection connection, 
    string sql, 
    object? param = null, 
    IDbTransaction? transaction = null, 
    int? commandTimeout = 30)
```

### 參數說明

- `connection` - 資料庫連線
- `sql` - SQL 查詢語句
- `param` - 查詢參數（選用）
- `transaction` - 交易物件（選用）
- `commandTimeout` - 命令逾時秒數，預設 30 秒

### 使用時機

- 查詢可能不存在的單筆資料
- 不確定是否有資料時使用
- 有多筆資料時只取第一筆

### 範例

```csharp
// 基本查詢
var user = await connection.pQueryFirstOrDefaultAsync<User>(
    "SELECT * FROM Users WHERE Id = @Id",
    new { Id = 1 });

if (user == null)
{
    Console.WriteLine("找不到使用者");
}

// 使用 ORDER BY 確保取得特定的第一筆
var latestOrder = await connection.pQueryFirstOrDefaultAsync<Order>(
    "SELECT * FROM Orders WHERE UserId = @UserId ORDER BY CreateDate DESC",
    new { UserId = 1 });
```

---

## pQuerySingleAsync

查詢單一筆資料，如果沒有資料或有多筆資料則拋出例外。

### 方法簽章

```csharp
public static async Task<T> pQuerySingleAsync<T>(
    this IDbConnection connection, 
    string sql, 
    object? param = null, 
    IDbTransaction? transaction = null, 
    int? commandTimeout = 30)
```

### 使用時機

- 確定只會有一筆資料時使用
- 需要嚴格檢查資料唯一性
- 例如：根據主鍵查詢

### 範例

```csharp
// 根據主鍵查詢（確定只有一筆）
var user = await connection.pQuerySingleAsync<User>(
    "SELECT * FROM Users WHERE Id = @Id",
    new { Id = 1 });

// 如果沒有資料或有多筆資料，會拋出例外
try
{
    var user = await connection.pQuerySingleAsync<User>(
        "SELECT * FROM Users WHERE Email = @Email",
        new { Email = "test@example.com" });
}
catch (InvalidOperationException ex)
{
    // 處理沒有資料或多筆資料的情況
}
```

---

## pQuerySingleOrDefaultAsync

查詢單一筆資料，如果沒有資料則回傳 `null`，有多筆資料則拋出例外。

### 方法簽章

```csharp
public static async Task<T?> pQuerySingleOrDefaultAsync<T>(
    this IDbConnection connection, 
    string sql, 
    object? param = null, 
    IDbTransaction? transaction = null, 
    int? commandTimeout = 30)
```

### 使用時機

- 資料可能不存在，但如果存在必須是唯一的
- 例如：根據唯一鍵（Unique Key）查詢

### 範例

```csharp
// 根據唯一的 Email 查詢
var user = await connection.pQuerySingleOrDefaultAsync<User>(
    "SELECT * FROM Users WHERE Email = @Email",
    new { Email = "john@example.com" });

if (user == null)
{
    Console.WriteLine("找不到使用者");
}
```

---

## pQueryListAsync

查詢多筆資料並回傳 `List<T>`。

### 方法簽章

```csharp
public static async Task<List<T>> pQueryListAsync<T>(
    this IDbConnection connection, 
    string sql, 
    object? param = null, 
    IDbTransaction? transaction = null, 
    int? commandTimeout = 30)
```

### 使用時機

- 查詢多筆資料
- 需要 `List<T>` 型別時使用

### 範例

```csharp
// 查詢所有啟用的使用者
var users = await connection.pQueryListAsync<User>(
    "SELECT * FROM Users WHERE Status = @Status",
    new { Status = "Active" });

Console.WriteLine($"找到 {users.Count} 位使用者");

// 使用 LINQ 進一步處理
var emails = users.Select(u => u.Email).ToList();

// 複雜查詢
var orders = await connection.pQueryListAsync<Order>(
    @"SELECT o.*, u.Name as UserName 
      FROM Orders o 
      INNER JOIN Users u ON o.UserId = u.Id 
      WHERE o.CreateDate >= @StartDate",
    new { StartDate = DateTime.Now.AddDays(-7) });
```

---

## pQueryPagedAsync

查詢分頁資料，自動計算總筆數。

### 方法簽章

```csharp
public static async Task<(List<T> Data, int TotalCount)> pQueryPagedAsync<T>(
    this IDbConnection connection, 
    string sql, 
    int pageNumber, 
    int pageSize, 
    object? param = null, 
    string orderBy = "Id", 
    IDbTransaction? transaction = null, 
    int? commandTimeout = 30)
```

### 參數說明

- `sql` - SQL 查詢語句（**不含** ORDER BY 和分頁語法）
- `pageNumber` - 頁碼（從 1 開始）
- `pageSize` - 每頁筆數
- `orderBy` - 排序欄位，預設為 "Id"

### 回傳值

回傳一個 Tuple：
- `Data` - 當前頁的資料
- `TotalCount` - 總筆數

### 範例

```csharp
// 基本分頁查詢
var (users, totalCount) = await connection.pQueryPagedAsync<User>(
    "SELECT * FROM Users WHERE Status = @Status",
    pageNumber: 1,
    pageSize: 10,
    param: new { Status = "Active" },
    orderBy: "CreateDate DESC");

Console.WriteLine($"總共 {totalCount} 筆，目前顯示第 1 頁");
Console.WriteLine($"本頁有 {users.Count} 筆資料");

// 計算總頁數
int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

// 複雜查詢的分頁
var (orders, total) = await connection.pQueryPagedAsync<Order>(
    @"SELECT o.*, u.Name as UserName 
      FROM Orders o 
      INNER JOIN Users u ON o.UserId = u.Id 
      WHERE o.Status = @Status",
    pageNumber: 2,
    pageSize: 20,
    param: new { Status = "Pending" },
    orderBy: "o.CreateDate DESC, o.Id DESC");
```

### 注意事項

- SQL 語句**不要**包含 `ORDER BY` 和 `OFFSET/FETCH`，這些會自動加上
- `orderBy` 參數可以包含多個欄位，例如："CreateDate DESC, Id ASC"
- 如果 `pageNumber` < 1，會自動設為 1
- 如果 `pageSize` < 1，會自動設為 10

---

## pQueryMultipleAsync

執行多個查詢並回傳多個結果集。

### 方法簽章

```csharp
public static async Task<SqlMapper.GridReader> pQueryMultipleAsync(
    this IDbConnection connection, 
    string sql, 
    object? param = null, 
    IDbTransaction? transaction = null, 
    int? commandTimeout = 30)
```

### 使用時機

- 需要一次執行多個查詢
- 減少資料庫往返次數
- 查詢相關的多個資料表

### 範例

```csharp
// 一次查詢多個結果集
var sql = @"
    SELECT * FROM Users WHERE Id = @UserId;
    SELECT * FROM Orders WHERE UserId = @UserId;
    SELECT * FROM Addresses WHERE UserId = @UserId;
";

using var multi = await connection.pQueryMultipleAsync(sql, new { UserId = 1 });

var user = await multi.ReadFirstOrDefaultAsync<User>();
var orders = (await multi.ReadAsync<Order>()).ToList();
var addresses = (await multi.ReadAsync<Address>()).ToList();

Console.WriteLine($"使用者: {user.Name}");
Console.WriteLine($"訂單數: {orders.Count}");
Console.WriteLine($"地址數: {addresses.Count}");
```

---

## pQueryStoredProcedureAsync

執行預存程序查詢。

### 方法簽章

```csharp
public static async Task<List<T>> pQueryStoredProcedureAsync<T>(
    this IDbConnection connection, 
    string storedProcedure, 
    object? param = null, 
    IDbTransaction? transaction = null, 
    int? commandTimeout = 30)
```

### 參數說明

- `storedProcedure` - 預存程序名稱

### 範例

```csharp
// 執行預存程序
var users = await connection.pQueryStoredProcedureAsync<User>(
    "sp_GetActiveUsers",
    new { MinAge = 18 });

// 帶有多個參數的預存程序
var orders = await connection.pQueryStoredProcedureAsync<Order>(
    "sp_GetOrdersByDateRange",
    new 
    { 
        StartDate = DateTime.Now.AddDays(-30),
        EndDate = DateTime.Now,
        Status = "Completed"
    });
```

---

## 效能建議

1. **使用適當的方法**
   - 確定只有一筆：用 `pQuerySingleAsync`
   - 可能沒有資料：用 `pQueryFirstOrDefaultAsync` 或 `pQuerySingleOrDefaultAsync`
   - 多筆資料：用 `pQueryListAsync`

2. **分頁查詢**
   - 大量資料務必使用 `pQueryPagedAsync`
   - 避免一次載入所有資料

3. **索引優化**
   - 確保 WHERE 條件的欄位有建立索引
   - ORDER BY 的欄位也建議建立索引

4. **參數化查詢**
   - 永遠使用參數化查詢避免 SQL Injection
   - 參數化查詢也能提升效能（查詢計畫快取）
