using System.Data;
using Dapper;

namespace NetExtensions.Extensions.Data
{
    /// <summary>
    /// Dapper 查詢相關的擴充方法
    /// </summary>
    public static class DapperQueryExtensions
    {
        /// <summary>
        /// 非同步查詢第一筆資料,如果沒有資料則回傳 null
        /// </summary>
        /// <typeparam name="T">回傳的資料型別</typeparam>
        /// <param name="connection">資料庫連線</param>
        /// <param name="sql">SQL 查詢語句</param>
        /// <param name="param">查詢參數</param>
        /// <param name="transaction">交易物件（選用）</param>
        /// <param name="commandTimeout">命令逾時秒數（預設 30 秒）</param>
        /// <returns>查詢結果或 null</returns>
        public static async Task<T?> pQueryFirstOrDefaultAsync<T>(this IDbConnection connection, string sql, object? param = null, IDbTransaction? transaction = null, int? commandTimeout = 30)
        {
            return await connection.QueryFirstOrDefaultAsync<T>(sql, param, transaction, commandTimeout, CommandType.Text);
        }

        /// <summary>
        /// 非同步查詢單一筆資料，如果沒有資料或有多筆資料則拋出例外
        /// </summary>
        /// <typeparam name="T">回傳的資料型別</typeparam>
        /// <param name="connection">資料庫連線</param>
        /// <param name="sql">SQL 查詢語句</param>
        /// <param name="param">查詢參數</param>
        /// <param name="transaction">交易物件（選用）</param>
        /// <param name="commandTimeout">命令逾時秒數（預設 30 秒）</param>
        /// <returns>查詢結果</returns>
        public static async Task<T> pQuerySingleAsync<T>(this IDbConnection connection, string sql, object? param = null, IDbTransaction? transaction = null, int? commandTimeout = 30)
        {
            return await connection.QuerySingleAsync<T>(sql, param, transaction, commandTimeout, CommandType.Text);
        }

        /// <summary>
        /// 非同步查詢單一筆資料，如果沒有資料則回傳 null，有多筆資料則拋出例外
        /// </summary>
        /// <typeparam name="T">回傳的資料型別</typeparam>
        /// <param name="connection">資料庫連線</param>
        /// <param name="sql">SQL 查詢語句</param>
        /// <param name="param">查詢參數</param>
        /// <param name="transaction">交易物件（選用）</param>
        /// <param name="commandTimeout">命令逾時秒數（預設 30 秒）</param>
        /// <returns>查詢結果或 null</returns>
        public static async Task<T?> pQuerySingleOrDefaultAsync<T>(this IDbConnection connection, string sql, object? param = null, IDbTransaction? transaction = null, int? commandTimeout = 30)
        {
            return await connection.QuerySingleOrDefaultAsync<T>(sql, param, transaction, commandTimeout, CommandType.Text);
        }

        /// <summary>
        /// 非同步查詢多筆資料並回傳列表
        /// </summary>
        /// <typeparam name="T">回傳的資料型別</typeparam>
        /// <param name="connection">資料庫連線</param>
        /// <param name="sql">SQL 查詢語句</param>
        /// <param name="param">查詢參數</param>
        /// <param name="transaction">交易物件（選用）</param>
        /// <param name="commandTimeout">命令逾時秒數（預設 30 秒）</param>
        /// <returns>查詢結果列表</returns>
        public static async Task<List<T>> pQueryListAsync<T>(this IDbConnection connection, string sql, object? param = null, IDbTransaction? transaction = null, int? commandTimeout = 30)
        {
            var result = await connection.QueryAsync<T>(sql, param, transaction, commandTimeout, CommandType.Text);
            return result.ToList();
        }

        /// <summary>
        /// 非同步查詢分頁資料
        /// </summary>
        /// <typeparam name="T">回傳的資料型別</typeparam>
        /// <param name="connection">資料庫連線</param>
        /// <param name="sql">SQL 查詢語句（不含分頁語法）</param>
        /// <param name="pageNumber">頁碼（從 1 開始）</param>
        /// <param name="pageSize">每頁筆數</param>
        /// <param name="param">查詢參數</param>
        /// <param name="orderBy">排序欄位（例如："CreateDate DESC"）</param>
        /// <param name="transaction">交易物件（選用）</param>
        /// <param name="commandTimeout">命令逾時秒數（預設 30 秒）</param>
        /// <returns>分頁查詢結果</returns>
        public static async Task<(List<T> Data, int TotalCount)> pQueryPagedAsync<T>(this IDbConnection connection, string sql, int pageNumber, int pageSize, object? param = null, string orderBy = "Id", IDbTransaction? transaction = null, int? commandTimeout = 30)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;

            var offset = (pageNumber - 1) * pageSize;

            // 計算總筆數
            var countSql = $"SELECT COUNT(*) FROM ({sql}) AS CountTable";
            var totalCount = await connection.ExecuteScalarAsync<int>(countSql, param, transaction, commandTimeout, CommandType.Text);

            // 查詢分頁資料
            var pagedSql = $@"
                SELECT * FROM ({sql}) AS PagedTable 
                ORDER BY {orderBy} 
                OFFSET {offset} ROWS 
                FETCH NEXT {pageSize} ROWS ONLY";

            var data = await connection.QueryAsync<T>(pagedSql, param, transaction, commandTimeout, CommandType.Text);

            return (data.ToList(), totalCount);
        }

        /// <summary>
        /// 非同步執行多個查詢並回傳多個結果集
        /// </summary>
        /// <param name="connection">資料庫連線</param>
        /// <param name="sql">SQL 查詢語句（可包含多個 SELECT）</param>
        /// <param name="param">查詢參數</param>
        /// <param name="transaction">交易物件（選用）</param>
        /// <param name="commandTimeout">命令逾時秒數（預設 30 秒）</param>
        /// <returns>多結果集查詢器</returns>
        public static async Task<SqlMapper.GridReader> pQueryMultipleAsync(this IDbConnection connection, string sql, object? param = null, IDbTransaction? transaction = null, int? commandTimeout = 30)
        {
            return await connection.QueryMultipleAsync(sql, param, transaction, commandTimeout, CommandType.Text);
        }

        /// <summary>
        /// 非同步執行預存程序查詢
        /// </summary>
        /// <typeparam name="T">回傳的資料型別</typeparam>
        /// <param name="connection">資料庫連線</param>
        /// <param name="storedProcedure">預存程序名稱</param>
        /// <param name="param">查詢參數</param>
        /// <param name="transaction">交易物件（選用）</param>
        /// <param name="commandTimeout">命令逾時秒數（預設 30 秒）</param>
        /// <returns>查詢結果列表</returns>
        public static async Task<List<T>> pQueryStoredProcedureAsync<T>(this IDbConnection connection, string storedProcedure, object? param = null, IDbTransaction? transaction = null, int? commandTimeout = 30)
        {
            var result = await connection.QueryAsync<T>(storedProcedure, param, transaction, commandTimeout, CommandType.StoredProcedure);
            return result.ToList();
        }
    }
}
