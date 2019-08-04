using System;
using System.Collections.Generic;
using System.Text;

namespace Liteson
{
    public interface ICacheProvider
    {
        void PutTable(string tableName);
        void DeleteTable(string tableName);
        void Insert<TRow>(string tableName, TRow row) where TRow: class, new();
        List<TRow> Read<TRow>(string tableName) where TRow: class, new();
        void Append<TRow>(string tableName, List<TRow> rowList) where TRow: class, new();
    }
}
