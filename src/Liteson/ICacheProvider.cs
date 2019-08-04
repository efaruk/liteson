using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Liteson
{
    public interface ICacheProvider
    {
        void Put<TRow>(List<TRow> table, string tableName) where TRow : class, new();
        Task PutAsync<TRow>(List<TRow> table, string tableName) where TRow : class, new();
        void Drop(string tableName);
        Task DropAsync(string tableName);
        void Insert<TRow>(string tableName, TRow row) where TRow : class, new();
        Task InsertAsync<TRow>(string tableName, TRow row) where TRow : class, new();
        List<TRow> Read<TRow>(string tableName) where TRow : class, new();
        void BulkInsert<TRow>(string tableName, List<TRow> rowList) where TRow : class, new();
        void Clear();
    }

    public class InMemoryCacheProvider : ICacheProvider
    {
        private const int DefaultCapacity = 1000;
        private readonly ConcurrentDictionary<string, object> _cache = new ConcurrentDictionary<string, object>(Environment.ProcessorCount, DefaultCapacity);
        private readonly ConcurrentDictionary<string, Lazy<SemaphoreSlim>> _locks = new ConcurrentDictionary<string, Lazy<SemaphoreSlim>>(Environment.ProcessorCount, DefaultCapacity);

        private SemaphoreSlim GetCacheItemLock(string tableName)
        {
            return _locks.GetOrAdd(tableName, tn => new Lazy<SemaphoreSlim>(() => new SemaphoreSlim(0, 1))).Value;
        }

        public void Put<TRow>(List<TRow> table, string tableName) where TRow : class, new()
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
            });
        }



        public async Task PutAsync<TRow>(List<TRow> table, string tableName) where TRow : class, new()
        {
           await Task.Run(() => Put(table, tableName));
        }

        public void Drop(string tableName)
        {
            var cacheItemLock = GetCacheItemLock(tableName);
            Utils.LockedAction(cacheItemLock, () =>
            {
                if (!_cache.ContainsKey(tableName)) return;
                while (!_cache.TryRemove(tableName, out _)) { }
            });
        }

        public async Task DropAsync(string tableName)
        {
            await Task.Run(() => Drop(tableName));
        }

        public void Insert<TRow>(string tableName, TRow row) where TRow : class, new()
        {
            throw new System.NotImplementedException();
        }

        public async Task InsertAsync<TRow>(string tableName, TRow row) where TRow : class, new()
        {
            await Task.Run(() =>
            {
                object table;
                while (!_cache.TryGetValue(tableName, out table))
                {
                }

                if (table != null)
                {

                }
            });
        }

        public List<TRow> Read<TRow>(string tableName) where TRow : class, new()
        {
            var cacheItemLock = GetCacheItemLock(tableName);
            return Utils.LockedFunc<List<TRow>>(cacheItemLock, () =>
            {
                if (!_cache.ContainsKey(tableName)) return null;
                object ro;
                while (!_cache.TryGetValue(tableName, out ro)) { }
                return (List<TRow>)ro;
            });
        }

        public void BulkInsert<TRow>(string tableName, List<TRow> rowList) where TRow : class, new()
        {
            throw new System.NotImplementedException();
        }

        public void Clear()
        {
            throw new System.NotImplementedException();
        }
    }
}
