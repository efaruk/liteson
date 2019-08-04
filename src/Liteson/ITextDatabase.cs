﻿using System.Collections.Generic;

namespace Liteson
{
    public interface ITextDatabase
    {
        void CreateTable(string tableName);
        void DropTable(string tableName);
        void Insert<TRow>(string tableName, TRow row) where TRow: class, new();
        List<TRow> Read<TRow>(string tableName) where TRow: class, new();
        void Append<TRow>(string tableName, List<TRow> rowList) where TRow: class, new();
    }
}
