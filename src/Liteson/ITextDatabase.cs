using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using SlowMember;

namespace Liteson
{
    public interface ITextDatabase
    {
        void CreateTable(string tableName);
        void DropTable(string tableName);
        void Insert<TRow>(string tableName, TRow row);
        List<TRow> ReadTable<TRow>(string tableName);
        void AppendTable<TRow>(string tableName, List<TRow> rowList);
    }

    public class LitesonDatabase : ITextDatabase
    {
        private readonly string _databasePath;
        private static readonly ReaderWriterLockSlim ReadWriteLock = new ReaderWriterLockSlim();
        
        private readonly LitesonSerializer _serializer;
        

        public LitesonDatabase(CultureInfo culture, string databasePath)
        {
            Culture = culture ?? throw new ArgumentNullException(nameof(culture));
            if (string.IsNullOrWhiteSpace(databasePath)) throw  new ArgumentNullException(nameof(databasePath));
            _databasePath = databasePath;
            if (!Directory.Exists(_databasePath))
            {
                Directory.CreateDirectory(_databasePath);
            }
            _serializer = new LitesonSerializer(culture);
        }

        public CultureInfo Culture { get; }

        private void LockedAction(Action action)
        {
            ReadWriteLock.EnterWriteLock();
            try
            {
                action();
            }
            finally
            {
                ReadWriteLock.ExitWriteLock();
            }
        }

        private T LockedFunc<T>(Func<T> func)
        {
            ReadWriteLock.EnterWriteLock();
            try
            {
                return func();
            }
            finally
            {
                ReadWriteLock.ExitWriteLock();
            }
        }

        public void CreateTable(string tableName)
        {
            LockedAction(() =>
            {
                var tableFilePath = GetTableFilePath(tableName);
                if (!File.Exists(tableFilePath))
                {
                    File.CreateText(tableFilePath).Close();
                }
            });
        }

        private FileNotFoundException TableNotFoundException(string path)
        {
            return new FileNotFoundException($"Table Path: {path} not found, please check the table already exists.");
        }

        public void DropTable(string tableName)
        {
            LockedAction(() => {
                var tableFilePath = GetTableFilePath(tableName);
                if (File.Exists(tableFilePath))
                {
                    File.Delete(tableFilePath);
                }
            });
        }

        private string GetTableFilePath(string tableName)
        {
            return Path.Combine(_databasePath, tableName);
        }

        public void Insert<TRow>(string tableName, TRow row)
        {
            LockedAction(() =>
            {
                var tableFilePath = GetTableFilePath(tableName);
                var valueText = _serializer.Serialize(row);
                if (string.IsNullOrWhiteSpace(valueText)) return;
                File.AppendAllText(tableFilePath, valueText);
            });
        }

        

        private bool CheckTableExists(string tableFilePath)
        {
            if (File.Exists(tableFilePath)) return true;
            throw TableNotFoundException(tableFilePath);
        }

        public List<TRow> ReadTable<TRow>(string tableName)
        {
            return LockedFunc(() =>
            {
                var tableFilePath = GetTableFilePath(tableName);
                if (CheckTableExists(tableFilePath))
                {
                    var data = File.ReadAllText(tableFilePath);
                    if (data.Length == 0)
                    {
                        return null;
                    }
                    var table = _serializer.Deserialize<List<TRow>>(data);
                    return table;
                }
                return null;
            });
        }

        public void AppendTable<TRow>(string tableName, List<TRow> rows)
        {
            LockedAction(() =>
            {
                if (rows == null || !rows.Any()) throw new ArgumentNullException(nameof(rows), "Rows can not be null or empty");
                var tableFilePath = GetTableFilePath(tableName);
                var data = _serializer.Serialize<List<TRow>>(rows);
                File.AppendAllText(tableFilePath, data);
            });
        }
    }
}
