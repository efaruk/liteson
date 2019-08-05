using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Liteson
{
    public class InMemoryCacheProvider : ICacheProvider
    {
        private const int DefaultCacheCapacity = 1000;
        private readonly ConcurrentDictionary<string, object> _cache = new ConcurrentDictionary<string, object>(Environment.ProcessorCount, DefaultCacheCapacity);
        private readonly ConcurrentDictionary<string, Lazy<SemaphoreSlim>> _locks = new ConcurrentDictionary<string, Lazy<SemaphoreSlim>>(Environment.ProcessorCount, DefaultCacheCapacity);

        private SemaphoreSlim GetCacheItemLock(string tableName)
        {
            return _locks.GetOrAdd(tableName, tn => new Lazy<SemaphoreSlim>(() => new SemaphoreSlim(1, 1))).Value;
        }

        public void Put<TRow>(List<TRow> table, string tableName, SemaphoreSlim operationLock = null) where TRow : class, new()
        {
            var cacheItemLock = GetCacheItemLock(tableName);
            Utils.LockedAction(cacheItemLock, () =>
            {
                if (_cache.ContainsKey(tableName))
                {
                    while (!_cache.TryUpdate(tableName, table, null)) { }
                }
                else
                {
                    while (!_cache.TryAdd(tableName, table)) { }
                }
            }, operationLock);
        }



        public async Task PutAsync<TRow>(List<TRow> table, string tableName, SemaphoreSlim operationLock = null) where TRow : class, new()
        {
            await Task.Run(() => Put(table, tableName, operationLock));
        }

        public void Drop(string tableName, SemaphoreSlim operationLock = null)
        {
            var cacheItemLock = GetCacheItemLock(tableName);
            Utils.LockedAction(cacheItemLock, () =>
            {
                if (!_cache.ContainsKey(tableName)) return;
                while (!_cache.TryRemove(tableName, out _)) { }
            }, operationLock);
        }

        public async Task DropAsync(string tableName, SemaphoreSlim operationLock = null)
        {
            await Task.Run(() => Drop(tableName, operationLock));
        }

        public void Insert<TRow>(string tableName, TRow row, SemaphoreSlim operationLock = null) where TRow : class, new()
        {
            var cacheItemLock = GetCacheItemLock(tableName);
            Utils.LockedAction(cacheItemLock, () =>
            {
                var table = Read<TRow>(tableName, operationLock) ?? new List<TRow>(LitesonDatabase.DefaultTableRowCapacity);
                table.Add(row);
            }, operationLock);
        }

        public async Task InsertAsync<TRow>(string tableName, TRow row, SemaphoreSlim operationLock = null) where TRow : class, new()
        {
            
            var cacheItemLock = GetCacheItemLock(tableName);
            await Utils.LockedActionAsync(cacheItemLock, async () =>
            {
                var table = await ReadAsync<TRow>(tableName, operationLock);
                if (table == null)
                {
                    table = new List<TRow>(LitesonDatabase.DefaultTableRowCapacity);
                    await PutAsync(table, tableName, operationLock);
                }
                await Task.Run(() => table.Add(row));
            }, operationLock);
        }

        public List<TRow> Read<TRow>(string tableName, SemaphoreSlim operationLock = null) where TRow : class, new()
        {
            var cacheItemLock = GetCacheItemLock(tableName);
            return Utils.LockedFunc(cacheItemLock, () =>
            {
                if (!_cache.ContainsKey(tableName)) return null;
                object ro;
                while (!_cache.TryGetValue(tableName, out ro)) { }
                return (List<TRow>)ro;
            }, operationLock);
        }

        public async Task<List<TRow>> ReadAsync<TRow>(string tableName, SemaphoreSlim operationLock = null) where TRow : class, new()
        {
            return await Task.Run(() => Read<TRow>(tableName, operationLock));
        }

        public void BulkInsert<TRow>(string tableName, List<TRow> rows, SemaphoreSlim operationLock = null) where TRow : class, new()
        {
            var cacheItemLock = GetCacheItemLock(tableName);
            Utils.LockedAction(cacheItemLock, () =>
            {   
                var table = Read<TRow>(tableName, operationLock);
                if (table == null)
                {
                    table = new List<TRow>(LitesonDatabase.DefaultTableRowCapacity);
                    Put(table, tableName, operationLock);
                }
                table.AddRange(rows);
            }, operationLock);
        }

        public async Task BulkInsertAsync<TRow>(string tableName, List<TRow> rows, SemaphoreSlim operationLock = null) where TRow : class, new()
        {
            var cacheItemLock = GetCacheItemLock(tableName);
            await Utils.LockedActionAsync(cacheItemLock, async () =>
            {
                var table = await ReadAsync<TRow>(tableName, operationLock);
                if (table == null)
                {
                    table = new List<TRow>(LitesonDatabase.DefaultTableRowCapacity);
                    await PutAsync(table, tableName, operationLock);
                }
                await Task.Run(() => table.AddRange(rows));
            }, operationLock);
        }

        public void Clear()
        {
            _cache.Clear();
        }

        public async Task ClearAsync()
        {
            await Task.Run(() => Clear());
        }
    }
}