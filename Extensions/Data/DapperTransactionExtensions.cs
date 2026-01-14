using System.Data;
using Dapper;

namespace NetExtensions.Extensions.Data
{
    /// <summary>
    /// Dapper 交易相關的擴充方法
    /// </summary>
    public static class DapperTransactionExtensions
    {
        /// <summary>
        /// 在交易中執行操作，自動處理 Commit 和 Rollback
        /// </summary>
        /// <typeparam name="T">回傳值的型別</typeparam>
        /// <param name="connection">資料庫連線</param>
        /// <param name="action">要在交易中執行的操作</param>
        /// <param name="isolationLevel">交易隔離等級（預設為 ReadCommitted）</param>
        /// <returns>操作的回傳值</returns>
        public static async Task<T> pExecuteInTransactionAsync<T>(this IDbConnection connection, Func<IDbConnection, IDbTransaction, Task<T>> action, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            using var transaction = connection.BeginTransaction(isolationLevel);
            try
            {
                var result = await action(connection, transaction);
                transaction.Commit();
                return result;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        /// <summary>
        /// 在交易中執行操作（無回傳值），自動處理 Commit 和 Rollback
        /// </summary>
        /// <param name="connection">資料庫連線</param>
        /// <param name="action">要在交易中執行的操作</param>
        /// <param name="isolationLevel">交易隔離等級（預設為 ReadCommitted）</param>
        public static async Task pExecuteInTransactionAsync(this IDbConnection connection, Func<IDbConnection, IDbTransaction, Task> action, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            using var transaction = connection.BeginTransaction(isolationLevel);
            try
            {
                await action(connection, transaction);
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        /// <summary>
        /// 安全地開始交易，確保連線已開啟
        /// </summary>
        /// <param name="connection">資料庫連線</param>
        /// <param name="isolationLevel">交易隔離等級（預設為 ReadCommitted）</param>
        /// <returns>交易物件</returns>
        public static IDbTransaction pBeginTransactionSafe(this IDbConnection connection, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            return connection.BeginTransaction(isolationLevel);
        }

        /// <summary>
        /// 安全地提交交易，發生錯誤時自動 Rollback
        /// </summary>
        /// <param name="transaction">交易物件</param>
        /// <param name="disposeAfterCommit">提交後是否自動釋放交易物件（預設為 true）</param>
        public static void pCommitSafe(this IDbTransaction transaction, bool disposeAfterCommit = true)
        {
            try
            {
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
            finally
            {
                if (disposeAfterCommit)
                {
                    transaction.Dispose();
                }
            }
        }

        /// <summary>
        /// 在交易中執行多個 SQL 命令
        /// </summary>
        /// <param name="connection">資料庫連線</param>
        /// <param name="sqlCommands">要執行的 SQL 命令列表（每個元素包含 SQL 和參數）</param>
        /// <param name="isolationLevel">交易隔離等級（預設為 ReadCommitted）</param>
        /// <param name="commandTimeout">命令逾時秒數（預設 30 秒）</param>
        /// <returns>所有命令受影響的總資料列數</returns>
        public static async Task<int> pExecuteMultipleInTransactionAsync(this IDbConnection connection, IEnumerable<(string Sql, object? Param)> sqlCommands, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, int? commandTimeout = 30)
        {
            return await connection.pExecuteInTransactionAsync(async (conn, trans) =>
            {
                var totalAffected = 0;
                foreach (var (sql, param) in sqlCommands)
                {
                    totalAffected += await conn.ExecuteAsync(sql, param, trans, commandTimeout, CommandType.Text);
                }
                return totalAffected;
            }, isolationLevel);
        }
    }
}
