using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Liteson
{
    public interface ICacheProvider
    {
        void Put<TRow>(List<TRow> table, string tableName, SemaphoreSlim operationLock = null) where TRow : class, new();
        Task PutAsync<TRow>(List<TRow> table, string tableName, SemaphoreSlim operationLock = null) where TRow : class, new();
        void Drop(string tableName, SemaphoreSlim operationLock = null);
        Task DropAsync(string tableName, SemaphoreSlim operationLock = null);
        void Insert<TRow>(string tableName, TRow row, SemaphoreSlim operationLock = null) where TRow : class, new();
        Task InsertAsync<TRow>(string tableName, TRow row, SemaphoreSlim operationLock = null) where TRow : class, new();
        List<TRow> Read<TRow>(string tableName, SemaphoreSlim operationLock = null) where TRow : class, new();
        Task<List<TRow>> ReadAsync<TRow>(string tableName, SemaphoreSlim operationLock = null) where TRow : class, new();
        void BulkInsert<TRow>(string tableName, List<TRow> rows, SemaphoreSlim operationLock = null) where TRow : class, new();
        Task BulkInsertAsync<TRow>(string tableName, List<TRow> rows, SemaphoreSlim operationLock = null) where TRow : class, new();
        void Clear();
        Task ClearAsync();
    }
}
