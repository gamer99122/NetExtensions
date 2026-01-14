using System.Data;
using Dapper;

namespace NetExtensions.Extensions.Data
{
    /// <summary>
    /// Dapper 批次操作相關的擴充方法
    /// </summary>
    public static class DapperBulkExtensions
    {
        /// <summary>
        /// 批次新增多筆資料
        /// </summary>
        /// <typeparam name="T">實體型別</typeparam>
        /// <param name="connection">資料庫連線</param>
        /// <param name="tableName">資料表名稱</param>
        /// <param name="entities">要新增的實體列表</param>
        /// <param name="transaction">交易物件（選用）</param>
        /// <param name="commandTimeout">命令逾時秒數（預設 30 秒）</param>
        /// <returns>受影響的資料列數</returns>
        public static async Task<int> pBulkInsertAsync<T>(this IDbConnection connection, string tableName, IEnumerable<T> entities, IDbTransaction? transaction = null, int? commandTimeout = 30)
        {
            var entityList = entities.ToList();
            if (!entityList.Any())
                return 0;

            var properties = typeof(T).GetProperties().Where(p => p.Name != "Id").ToList();
            var columns = string.Join(", ", properties.Select(p => p.Name));
            var values = string.Join(", ", properties.Select(p => $"@{p.Name}"));
            var sql = $"INSERT INTO {tableName} ({columns}) VALUES ({values})";
            return await connection.ExecuteAsync(sql, entityList, transaction, commandTimeout, CommandType.Text);
        }

        /// <summary>
        /// 批次更新多筆資料
        /// </summary>
        /// <typeparam name="T">實體型別</typeparam>
        /// <param name="connection">資料庫連線</param>
        /// <param name="tableName">資料表名稱</param>
        /// <param name="entities">要更新的實體列表</param>
        /// <param name="keyColumn">主鍵欄位名稱（預設為 "Id"）</param>
        /// <param name="transaction">交易物件（選用）</param>
        /// <param name="commandTimeout">命令逾時秒數（預設 30 秒）</param>
        /// <returns>受影響的資料列數</returns>
        public static async Task<int> pBulkUpdateAsync<T>(this IDbConnection connection, string tableName, IEnumerable<T> entities, string keyColumn = "Id", IDbTransaction? transaction = null, int? commandTimeout = 30)
        {
            var entityList = entities.ToList();
            if (!entityList.Any())
                return 0;

            var properties = typeof(T).GetProperties().Where(p => p.Name != keyColumn).ToList();
            var setClause = string.Join(", ", properties.Select(p => $"{p.Name} = @{p.Name}"));
            var sql = $"UPDATE {tableName} SET {setClause} WHERE {keyColumn} = @{keyColumn}";
            return await connection.ExecuteAsync(sql, entityList, transaction, commandTimeout, CommandType.Text);
        }

        /// <summary>
        /// 批次刪除多筆資料（根據 Id 列表）
        /// </summary>
        /// <typeparam name="TKey">主鍵的型別</typeparam>
        /// <param name="connection">資料庫連線</param>
        /// <param name="tableName">資料表名稱</param>
        /// <param name="ids">要刪除的 Id 列表</param>
        /// <param name="keyColumn">主鍵欄位名稱（預設為 "Id"）</param>
        /// <param name="transaction">交易物件（選用）</param>
        /// <param name="commandTimeout">命令逾時秒數（預設 30 秒）</param>
        /// <returns>受影響的資料列數</returns>
        public static async Task<int> pBulkDeleteAsync<TKey>(this IDbConnection connection, string tableName, IEnumerable<TKey> ids, string keyColumn = "Id", IDbTransaction? transaction = null, int? commandTimeout = 30)
        {
            var idList = ids.ToList();
            if (!idList.Any())
                return 0;

            var sql = $"DELETE FROM {tableName} WHERE {keyColumn} IN @Ids";
            return await connection.ExecuteAsync(sql, new { Ids = idList }, transaction, commandTimeout, CommandType.Text);
        }

        /// <summary>
        /// 批次執行多個 SQL 命令
        /// </summary>
        /// <param name="connection">資料庫連線</param>
        /// <param name="sql">SQL 命令語句</param>
        /// <param name="parameters">參數列表（每個參數對應一次執行）</param>
        /// <param name="transaction">交易物件（選用）</param>
        /// <param name="commandTimeout">命令逾時秒數（預設 30 秒）</param>
        /// <returns>受影響的資料列數</returns>
        public static async Task<int> pBulkExecuteAsync(this IDbConnection connection, string sql, IEnumerable<object> parameters, IDbTransaction? transaction = null, int? commandTimeout = 30)
        {
            var paramList = parameters.ToList();
            if (!paramList.Any())
                return 0;

            return await connection.ExecuteAsync(sql, paramList, transaction, commandTimeout, CommandType.Text);
        }

        /// <summary>
        /// 批次新增資料（分批處理，避免一次新增太多筆）
        /// </summary>
        /// <typeparam name="T">實體型別</typeparam>
        /// <param name="connection">資料庫連線</param>
        /// <param name="tableName">資料表名稱</param>
        /// <param name="entities">要新增的實體列表</param>
        /// <param name="batchSize">每批次處理的筆數（預設 1000）</param>
        /// <param name="transaction">交易物件（選用）</param>
        /// <param name="commandTimeout">命令逾時秒數（預設 30 秒）</param>
        /// <returns>受影響的資料列數</returns>
        public static async Task<int> pBulkInsertInBatchesAsync<T>(this IDbConnection connection, string tableName, IEnumerable<T> entities, int batchSize = 1000, IDbTransaction? transaction = null, int? commandTimeout = 30)
        {
            var entityList = entities.ToList();
            if (!entityList.Any())
                return 0;

            var totalAffected = 0;
            var batches = entityList
                .Select((entity, index) => new { entity, index })
                .GroupBy(x => x.index / batchSize)
                .Select(g => g.Select(x => x.entity).ToList());

            foreach (var batch in batches)
            {
                totalAffected += await connection.pBulkInsertAsync(tableName, batch, transaction, commandTimeout);
            }

            return totalAffected;
        }

        /// <summary>
        /// 批次更新資料（分批處理，避免一次更新太多筆）
        /// </summary>
        /// <typeparam name="T">實體型別</typeparam>
        /// <param name="connection">資料庫連線</param>
        /// <param name="tableName">資料表名稱</param>
        /// <param name="entities">要更新的實體列表</param>
        /// <param name="batchSize">每批次處理的筆數（預設 1000）</param>
        /// <param name="keyColumn">主鍵欄位名稱（預設為 "Id"）</param>
        /// <param name="transaction">交易物件（選用）</param>
        /// <param name="commandTimeout">命令逾時秒數（預設 30 秒）</param>
        /// <returns>受影響的資料列數</returns>
        public static async Task<int> pBulkUpdateInBatchesAsync<T>(this IDbConnection connection, string tableName, IEnumerable<T> entities, int batchSize = 1000, string keyColumn = "Id", IDbTransaction? transaction = null, int? commandTimeout = 30)
        {
            var entityList = entities.ToList();
            if (!entityList.Any())
                return 0;

            var totalAffected = 0;
            var batches = entityList
                .Select((entity, index) => new { entity, index })
                .GroupBy(x => x.index / batchSize)
                .Select(g => g.Select(x => x.entity).ToList());

            foreach (var batch in batches)
            {
                totalAffected += await connection.pBulkUpdateAsync(tableName, batch, keyColumn, transaction, commandTimeout);
            }

            return totalAffected;
        }
    }
}
