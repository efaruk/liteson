using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Liteson
{
    public class LitesonDatabase : ITextDatabase
    {
        private const int DefaultTableCapacity = 1000;
        public const int DefaultTableRowCapacity = 1000000;
        private readonly string _databasePath;
        private readonly ICacheProvider _cacheProvider;
        //private static readonly ReaderWriterLockSlim ReadWriteLock = new ReaderWriterLockSlim();
        //private readonly SemaphoreSlim _ioLock = new SemaphoreSlim(1,1);
        private readonly ConcurrentDictionary<string, Lazy<SemaphoreSlim>> _locks = new ConcurrentDictionary<string, Lazy<SemaphoreSlim>>(Environment.ProcessorCount, DefaultTableCapacity);
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
#pragma warning disable 618
            _serializer = serializer ?? new LitesonSerializer(culture);
#pragma warning restore 618
            _cacheProvider = cacheProvider ?? new InMemoryCacheProvider();
        }

        private SemaphoreSlim GetTableLock(string tableName)
        {
            return _locks.GetOrAdd(tableName, tn => new Lazy<SemaphoreSlim>(() => new SemaphoreSlim(1, 1))).Value;
        }

        public CultureInfo Culture { get; }

        public void Create(string tableName)
        {
            var tableLock = GetTableLock(tableName);
            Utils.LockedAction(tableLock,() =>
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
            var tableLock = GetTableLock(tableName);
            Utils.LockedAction(tableLock,() =>
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
            var tableLock = GetTableLock(tableName);
            Utils.LockedAction(tableLock,() =>
            {
                var rowSting = _serializer.SerializeRow(row);
                if (string.IsNullOrWhiteSpace(rowSting)) return;
                var tableFilePath = GetTableFilePath(tableName);
                _cacheProvider.Insert(tableName, row);
                File.AppendAllText(tableFilePath, rowSting);
            });
        }

        public async Task InsertAsync<TRow>(string tableName, TRow row) where TRow : class, new()
        {
            var tableLock = GetTableLock(tableName);
            await Utils.LockedActionAsync(tableLock, async () =>
            {
                var rowSting = _serializer.SerializeRow(row);
                if (string.IsNullOrWhiteSpace(rowSting)) return;
                var tableFilePath = GetTableFilePath(tableName);
#pragma warning disable 4014
                // Fire And Forget
                _cacheProvider.InsertAsync(tableName, row);
                // Fire And Forget
                Task.Run(() => File.AppendAllText(tableFilePath, rowSting));
#pragma warning restore 4014
            });
        }

        private bool CheckTableExists(string tableFilePath)
        {
            if (File.Exists(tableFilePath)) return true;
            throw TableNotFoundException(tableFilePath);
        }

        public List<TRow> Read<TRow>(string tableName) where TRow : class, new()
        {
            var tableLock = GetTableLock(tableName);
            return Utils.LockedFunc(tableLock, () =>
            {
                var fromCache = _cacheProvider.Read<TRow>(tableName);
                if (fromCache != null && fromCache.Any()) return fromCache;
                var tableFilePath = GetTableFilePath(tableName);
                if (!CheckTableExists(tableFilePath)) return null;
                var tableText = File.ReadAllText(tableFilePath);
                var result = string.IsNullOrWhiteSpace(tableText) ? null : _serializer.DeserializeRows<TRow>(tableText);
                if (result == null) return null;
                _cacheProvider.Put(result, tableName);
                return result;
            });
        }

        public async Task<List<TRow>> ReadAsync<TRow>(string tableName) where TRow : class, new()
        {
            var tableLock = GetTableLock(tableName);
            return await Utils.LockedFuncAsync(tableLock, tableName, async tn =>
            {
                var fromCache = await _cacheProvider.ReadAsync<TRow>(tn);
                if (fromCache != null && fromCache.Any()) return fromCache;
                var tableFilePath = GetTableFilePath(tn);
                if (!CheckTableExists(tableFilePath)) return null;
                var tableText = File.ReadAllText(tableFilePath);
                var result = string.IsNullOrWhiteSpace(tableText) ? null : _serializer.DeserializeRows<TRow>(tableText);
                if (result == null) return null;
#pragma warning disable 4014
                // Fire And Forget
                _cacheProvider.PutAsync(result, tn);
#pragma warning restore 4014
                return result;
            });
        }

        public void BulkInsert<TRow>(string tableName, List<TRow> rows) where TRow : class, new()
        {
            var tableLock = GetTableLock(tableName);
            Utils.LockedAction(tableLock,() =>
            {
                if (rows == null || !rows.Any()) return;
                var rowsString = _serializer.SerializeRows(rows);
                if (string.IsNullOrWhiteSpace(rowsString)) return;
                var tableFilePath = GetTableFilePath(tableName);
                _cacheProvider.BulkInsert(tableName, rows);
                File.AppendAllText(tableFilePath, rowsString);
            });
        }

        public async Task BulkInsertAsync<TRow>(string tableName, List<TRow> rows) where TRow : class, new()
        {
            var tableLock = GetTableLock(tableName);
            await Utils.LockedActionAsync(tableLock, async () =>
            {
                if (rows == null || !rows.Any()) return;
                var rowsString = _serializer.SerializeRows(rows);
                if (string.IsNullOrWhiteSpace(rowsString)) return;
                var tableFilePath = GetTableFilePath(tableName);
#pragma warning disable 4014
                // Fire And Forget
                _cacheProvider.BulkInsertAsync(tableName, rows);
#pragma warning restore 4014
                File.AppendAllText(tableFilePath, rowsString);
            });
        }
    }
}