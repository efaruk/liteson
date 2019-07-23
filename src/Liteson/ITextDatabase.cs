using System.Collections.Generic;
using System.Reflection;
using System.Text;
using SlowMember;

namespace Liteson
{
    public interface ITextDatabase
    {
        void CreateTable(string tableName);
        void DropTable(string tableName);
        void Insert<TRow>(string tableName, TRow row) where TRow: class, new();
        List<TRow> ReadTable<TRow>(string tableName) where TRow: class, new();
        void AppendTable<TRow>(string tableName, List<TRow> rowList) where TRow: class, new();
    }
}
