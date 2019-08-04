using System.Collections.Generic;

namespace Liteson
{
    public interface ITextDatabase
    {
        void Create(string tableName);
        void Drop(string tableName);
        void Insert<TRow>(string tableName, TRow row) where TRow: class, new();
        List<TRow> Read<TRow>(string tableName) where TRow: class, new();
        void BulkInsert<TRow>(string tableName, List<TRow> rowList) where TRow: class, new();
    }
}
