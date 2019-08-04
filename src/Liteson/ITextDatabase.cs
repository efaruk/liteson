using System.Collections.Generic;
using System.Threading.Tasks;

namespace Liteson
{
    public interface ITextDatabase
    {
        void Create(string tableName);
        void Drop(string tableName);
        void Insert<TRow>(string tableName, TRow row) where TRow: class, new();
        Task InsertAsync<TRow>(string tableName, TRow row) where TRow: class, new();
        List<TRow> Read<TRow>(string tableName) where TRow: class, new();
        Task<List<TRow>> ReadAsync<TRow>(string tableName) where TRow: class, new();
        void BulkInsert<TRow>(string tableName, List<TRow> rows) where TRow: class, new();
        Task BulkInsertAsync<TRow>(string tableName, List<TRow> rows) where TRow: class, new();
    }
}
