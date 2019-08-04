using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;

namespace Liteson
{
    public class LitesonDatabase : ITextDatabase
    {
        private readonly string _databasePath;
        private readonly ICacheProvider _cacheProvider;
        //private static readonly ReaderWriterLockSlim ReadWriteLock = new ReaderWriterLockSlim();
        private readonly SemaphoreSlim _ioLock = new SemaphoreSlim(0,1);
        private const string TableFileExtension = ".lson";
        private readonly ITextSerializer _serializer;

        public LitesonDatabase(CultureInfo culture, string databasePath, ITextSerializer serializer = null, ICacheProvider cacheProvider = null)
        {
            Culture = culture ?? throw new ArgumentNullException(nameof(culture));
            if (string.IsNullOrWhiteSpace(databasePath)) throw new ArgumentNullException(nameof(databasePath));
            _databasePath = databasePath;
            if (!Directory.Exists(_databasePath))
            {
                Directory.CreateDirectory(_databasePath);
            }
            _serializer = serializer ?? new LitesonSerializer(culture);
            _cacheProvider = cacheProvider ?? new InMemoryCacheProvider();
        }

        public CultureInfo Culture { get; }

        public void Create(string tableName)
        {
            Utils.LockedAction(_ioLock,() =>
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

        public void Drop(string tableName)
        {
            Utils.LockedAction(_ioLock,() =>
            {
                var tableFilePath = GetTableFilePath(tableName);
                if (File.Exists(tableFilePath))
                {
                    _cacheProvider.Drop(tableName);
                    File.Delete(tableFilePath);
                }
            });
        }

        private string GetTableFilePath(string tableName)
        {
            var tableFileName = $"{tableName}{TableFileExtension}";
            return Path.Combine(_databasePath, tableFileName);
        }

        public void Insert<TRow>(string tableName, TRow row) where TRow : class, new()
        {
            Utils.LockedAction(_ioLock,() =>
            {
                var rowSting = _serializer.SerializeRow(row);
                if (string.IsNullOrWhiteSpace(rowSting)) return;
                var tableFilePath = GetTableFilePath(tableName);
                _cacheProvider.Insert(tableName, row);
                File.AppendAllText(tableFilePath, rowSting);
            });
        }

        private bool CheckTableExists(string tableFilePath)
        {
            if (File.Exists(tableFilePath)) return true;
            throw TableNotFoundException(tableFilePath);
        }

        public List<TRow> Read<TRow>(string tableName) where TRow : class, new()
        {
            return Utils.LockedFunc(_ioLock, () =>
            {
                var fromCache = _cacheProvider.Read<TRow>(tableName);
                if (fromCache != null && fromCache.Any()) return fromCache;
                var tableFilePath = GetTableFilePath(tableName);
                if (!CheckTableExists(tableFilePath)) return null;
                var tableText = File.ReadAllText(tableFilePath);
                var result = string.IsNullOrWhiteSpace(tableText) ? null : _serializer.DeserializeRows<TRow>(tableText);
                _cacheProvider.Put(result, tableName);
                return result;
            });
        }

        public void BulkInsert<TRow>(string tableName, List<TRow> rows) where TRow : class, new()
        {
            Utils.LockedAction(_ioLock,() =>
            {
                if (rows == null || !rows.Any()) return;
                var rowsString = _serializer.SerializeRows(rows);
                if (string.IsNullOrWhiteSpace(rowsString)) return;
                var tableFilePath = GetTableFilePath(tableName);
                _cacheProvider.BulkInsert(tableName, rows);
                File.AppendAllText(tableFilePath, rowsString);
            });
        }
    }
}