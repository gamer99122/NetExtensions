using System.Data;
using Dapper;

namespace NetExtensions.Extensions.Data
{
    /// <summary>
    /// Dapper 執行相關的擴充方法
    /// </summary>
    public static class DapperExecuteExtensions
    {
        /// <summary>
        /// 非同步執行 SQL 命令（INSERT、UPDATE、DELETE 等）
        /// </summary>
        /// <param name="connection">資料庫連線</param>
        /// <param name="sql">SQL 命令語句</param>
        /// <param name="param">命令參數</param>
        /// <param name="transaction">交易物件（選用）</param>
        /// <param name="commandTimeout">命令逾時秒數（預設 30 秒）</param>
        /// <returns>受影響的資料列數</returns>
        public static async Task<int> pExecuteAsync(this IDbConnection connection, string sql, object? param = null, IDbTransaction? transaction = null, int? commandTimeout = 30)
        {
            return await connection.ExecuteAsync(sql, param, transaction, commandTimeout, CommandType.Text);
        }

        /// <summary>
        /// 非同步執行 SQL 命令並回傳單一值（例如：COUNT、SUM、MAX 等）
        /// </summary>
        /// <typeparam name="T">回傳值的型別</typeparam>
        /// <param name="connection">資料庫連線</param>
        /// <param name="sql">SQL 命令語句</param>
        /// <param name="param">命令參數</param>
        /// <param name="transaction">交易物件（選用）</param>
        /// <param name="commandTimeout">命令逾時秒數（預設 30 秒）</param>
        /// <returns>查詢結果的單一值</returns>
        public static async Task<T> pExecuteScalarAsync<T>(this IDbConnection connection, string sql, object? param = null, IDbTransaction? transaction = null, int? commandTimeout = 30)
        {
            return (await connection.ExecuteScalarAsync<T>(sql, param, transaction, commandTimeout, CommandType.Text))!;
        }

        /// <summary>
        /// 非同步執行預存程序
        /// </summary>
        /// <param name="connection">資料庫連線</param>
        /// <param name="storedProcedure">預存程序名稱</param>
        /// <param name="param">命令參數</param>
        /// <param name="transaction">交易物件（選用）</param>
        /// <param name="commandTimeout">命令逾時秒數（預設 30 秒）</param>
        /// <returns>受影響的資料列數</returns>
        public static async Task<int> pExecuteStoredProcedureAsync(this IDbConnection connection, string storedProcedure, object? param = null, IDbTransaction? transaction = null, int? commandTimeout = 30)
        {
            return await connection.ExecuteAsync(storedProcedure, param, transaction, commandTimeout, CommandType.StoredProcedure);
        }

        /// <summary>
        /// 非同步新增單筆資料（使用動態參數）
        /// </summary>
        /// <param name="connection">資料庫連線</param>
        /// <param name="tableName">資料表名稱</param>
        /// <param name="entity">要新增的實體物件</param>
        /// <param name="transaction">交易物件（選用）</param>
        /// <param name="commandTimeout">命令逾時秒數（預設 30 秒）</param>
        /// <returns>受影響的資料列數</returns>
        public static async Task<int> pInsertAsync(this IDbConnection connection, string tableName, object entity, IDbTransaction? transaction = null, int? commandTimeout = 30)
        {
            var properties = entity.GetType().GetProperties().Where(p => p.Name != "Id").ToList();
            var columns = string.Join(", ", properties.Select(p => p.Name));
            var values = string.Join(", ", properties.Select(p => $"@{p.Name}"));
            var sql = $"INSERT INTO {tableName} ({columns}) VALUES ({values})";
            return await connection.ExecuteAsync(sql, entity, transaction, commandTimeout, CommandType.Text);
        }

        /// <summary>
        /// 非同步新增單筆資料並回傳新增的 Id
        /// </summary>
        /// <typeparam name="TKey">Id 的型別</typeparam>
        /// <param name="connection">資料庫連線</param>
        /// <param name="tableName">資料表名稱</param>
        /// <param name="entity">要新增的實體物件</param>
        /// <param name="transaction">交易物件（選用）</param>
        /// <param name="commandTimeout">命令逾時秒數（預設 30 秒）</param>
        /// <returns>新增資料的 Id</returns>
        public static async Task<TKey> pInsertWithIdAsync<TKey>(this IDbConnection connection, string tableName, object entity, IDbTransaction? transaction = null, int? commandTimeout = 30)
        {
            var properties = entity.GetType().GetProperties().Where(p => p.Name != "Id").ToList();
            var columns = string.Join(", ", properties.Select(p => p.Name));
            var values = string.Join(", ", properties.Select(p => $"@{p.Name}"));
            var sql = $@"INSERT INTO {tableName} ({columns}) VALUES ({values}); SELECT CAST(SCOPE_IDENTITY() AS {typeof(TKey).Name});";
            return (await connection.ExecuteScalarAsync<TKey>(sql, entity, transaction, commandTimeout, CommandType.Text))!;
        }

        /// <summary>
        /// 非同步更新單筆資料
        /// </summary>
        /// <param name="connection">資料庫連線</param>
        /// <param name="tableName">資料表名稱</param>
        /// <param name="entity">要更新的實體物件</param>
        /// <param name="keyColumn">主鍵欄位名稱（預設為 "Id"）</param>
        /// <param name="transaction">交易物件（選用）</param>
        /// <param name="commandTimeout">命令逾時秒數（預設 30 秒）</param>
        /// <returns>受影響的資料列數</returns>
        public static async Task<int> pUpdateAsync(this IDbConnection connection, string tableName, object entity, string keyColumn = "Id", IDbTransaction? transaction = null, int? commandTimeout = 30)
        {
            var properties = entity.GetType().GetProperties().Where(p => p.Name != keyColumn).ToList();
            var setClause = string.Join(", ", properties.Select(p => $"{p.Name} = @{p.Name}"));
            var sql = $"UPDATE {tableName} SET {setClause} WHERE {keyColumn} = @{keyColumn}";
            return await connection.ExecuteAsync(sql, entity, transaction, commandTimeout, CommandType.Text);
        }

        /// <summary>
        /// 非同步刪除單筆資料
        /// </summary>
        /// <typeparam name="TKey">主鍵的型別</typeparam>
        /// <param name="connection">資料庫連線</param>
        /// <param name="tableName">資料表名稱</param>
        /// <param name="id">要刪除的資料 Id</param>
        /// <param name="keyColumn">主鍵欄位名稱（預設為 "Id"）</param>
        /// <param name="transaction">交易物件（選用）</param>
        /// <param name="commandTimeout">命令逾時秒數（預設 30 秒）</param>
        /// <returns>受影響的資料列數</returns>
        public static async Task<int> pDeleteAsync<TKey>(this IDbConnection connection, string tableName, TKey id, string keyColumn = "Id", IDbTransaction? transaction = null, int? commandTimeout = 30)
        {
            var sql = $"DELETE FROM {tableName} WHERE {keyColumn} = @Id";
            return await connection.ExecuteAsync(sql, new { Id = id }, transaction, commandTimeout, CommandType.Text);
        }

        /// <summary>
        /// 非同步刪除符合條件的資料
        /// </summary>
        /// <param name="connection">資料庫連線</param>
        /// <param name="tableName">資料表名稱</param>
        /// <param name="whereClause">WHERE 條件（例如："Status = @Status"）</param>
        /// <param name="param">條件參數</param>
        /// <param name="transaction">交易物件（選用）</param>
        /// <param name="commandTimeout">命令逾時秒數（預設 30 秒）</param>
        /// <returns>受影響的資料列數</returns>
        public static async Task<int> pDeleteWhereAsync(this IDbConnection connection, string tableName, string whereClause, object? param = null, IDbTransaction? transaction = null, int? commandTimeout = 30)
        {
            var sql = $"DELETE FROM {tableName} WHERE {whereClause}";
            return await connection.ExecuteAsync(sql, param, transaction, commandTimeout, CommandType.Text);
        }
    }
}
