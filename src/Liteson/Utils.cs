using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace Liteson
{
    internal class Utils
    {
        private static readonly Dictionary<Type, PrimitiveTypeCode> TypeCodeMap =
            new Dictionary<Type, PrimitiveTypeCode>
            {
                { typeof(char), PrimitiveTypeCode.Char },
                { typeof(char?), PrimitiveTypeCode.CharNullable },
                { typeof(bool), PrimitiveTypeCode.Boolean },
                { typeof(bool?), PrimitiveTypeCode.BooleanNullable },
                { typeof(sbyte), PrimitiveTypeCode.SByte },
                { typeof(sbyte?), PrimitiveTypeCode.SByteNullable },
                { typeof(short), PrimitiveTypeCode.Int16 },
                { typeof(short?), PrimitiveTypeCode.Int16Nullable },
                { typeof(ushort), PrimitiveTypeCode.UInt16 },
                { typeof(ushort?), PrimitiveTypeCode.UInt16Nullable },
                { typeof(int), PrimitiveTypeCode.Int32 },
                { typeof(int?), PrimitiveTypeCode.Int32Nullable },
                { typeof(byte), PrimitiveTypeCode.Byte },
                { typeof(byte?), PrimitiveTypeCode.ByteNullable },
                { typeof(uint), PrimitiveTypeCode.UInt32 },
                { typeof(uint?), PrimitiveTypeCode.UInt32Nullable },
                { typeof(long), PrimitiveTypeCode.Int64 },
                { typeof(long?), PrimitiveTypeCode.Int64Nullable },
                { typeof(ulong), PrimitiveTypeCode.UInt64 },
                { typeof(ulong?), PrimitiveTypeCode.UInt64Nullable },
                { typeof(float), PrimitiveTypeCode.Single },
                { typeof(float?), PrimitiveTypeCode.SingleNullable },
                { typeof(double), PrimitiveTypeCode.Double },
                { typeof(double?), PrimitiveTypeCode.DoubleNullable },
                { typeof(DateTime), PrimitiveTypeCode.DateTime },
                { typeof(DateTime?), PrimitiveTypeCode.DateTimeNullable },
                { typeof(DateTimeOffset), PrimitiveTypeCode.DateTimeOffset },
                { typeof(DateTimeOffset?), PrimitiveTypeCode.DateTimeOffsetNullable },
                { typeof(decimal), PrimitiveTypeCode.Decimal },
                { typeof(decimal?), PrimitiveTypeCode.DecimalNullable },
                { typeof(Guid), PrimitiveTypeCode.Guid },
                { typeof(Guid?), PrimitiveTypeCode.GuidNullable },
                { typeof(TimeSpan), PrimitiveTypeCode.TimeSpan },
                { typeof(TimeSpan?), PrimitiveTypeCode.TimeSpanNullable },
                { typeof(BigInteger), PrimitiveTypeCode.BigInteger },
                { typeof(BigInteger?), PrimitiveTypeCode.BigIntegerNullable },
                { typeof(Uri), PrimitiveTypeCode.Uri },
                { typeof(string), PrimitiveTypeCode.String },
                { typeof(byte[]), PrimitiveTypeCode.Bytes },
            };

        public static bool AreSame(object currentObject, object oldObject)
        {
            if (currentObject == null || oldObject == null) return false;
            return ReferenceEquals(currentObject, oldObject);
        }

        public static void LockedAction(SemaphoreSlim semaphoreLock, Action action, SemaphoreSlim oldLock = null, Action<Exception> exceptionHandleAction = null)
        {
            var sameLock = AreSame(semaphoreLock, oldLock);
            try
            {
                if (!sameLock)
                {
                    semaphoreLock.Wait();
                }
                action();
            }
            catch (Exception ex)
            {
                exceptionHandleAction?.Invoke(ex);
            }
            finally
            {
                if (!sameLock)
                {
                    semaphoreLock.Release();
                }
            }
        }

        public static async Task LockedActionAsync(SemaphoreSlim semaphoreLock, Action action, SemaphoreSlim oldLock = null, Action<Exception> exceptionHandleAction = null)
        {
            var sameLock = AreSame(semaphoreLock, oldLock);
            try
            {
                if (!sameLock)
                {
                    await semaphoreLock.WaitAsync();
                }
                await Task.Run(action);
            }
            catch (Exception ex)
            {
                exceptionHandleAction?.Invoke(ex);
            }
            finally
            {
                if (!sameLock)
                {
                    semaphoreLock.Release();
                }
            }
        }

        public static TResult LockedFunc<TResult>(SemaphoreSlim semaphoreLock, Func<TResult> func, SemaphoreSlim oldLock = null, Action<Exception> exceptionHandleAction = null) where TResult: class
        {
            var sameLock = AreSame(semaphoreLock, oldLock);
            TResult result = null;
            try
            {
                if (!sameLock)
                {
                    semaphoreLock.Wait();
                }
                result = func();
            }
            catch (Exception ex)
            {
                exceptionHandleAction?.Invoke(ex);
            }
            finally
            {
                if (!sameLock)
                {
                    semaphoreLock.Release();
                }
            }
            return result;
        }

        public static async Task<TResult> LockedFuncAsync<TResult>(SemaphoreSlim semaphoreLock, Func<TResult> func, SemaphoreSlim oldLock = null, Action<Exception> exceptionHandleAction = null) where TResult: class
        {
            var sameLock = AreSame(semaphoreLock, oldLock);
            TResult result = null;
            try
            {
                if (!sameLock)
                {
                    await semaphoreLock.WaitAsync();
                }
                result = await Task.Run(func);
            }
            catch (Exception ex)
            {
                exceptionHandleAction?.Invoke(ex);
            }
            finally
            {
                if (!sameLock)
                {
                    semaphoreLock.Release();
                }
            }
            return result;
        }

        public static async Task<TResult> LockedFuncAsync<TParam, TResult>(SemaphoreSlim semaphoreLock, TParam t1, Func<TParam, Task<TResult>> func, SemaphoreSlim oldLock = null, Action<Exception> exceptionHandleAction = null) where TResult: class
        {
            var sameLock = AreSame(semaphoreLock, oldLock);
            TResult result = null;
            try
            {
                if (!sameLock)
                {
                    await semaphoreLock.WaitAsync();
                }
                result = await Task.Run(() => func(t1));
            }
            catch (Exception ex)
            {
                exceptionHandleAction?.Invoke(ex);
            }
            finally
            {
                if (!sameLock)
                {
                    semaphoreLock.Release();
                }
            }
            return result;
        }

        public static PrimitiveTypeCode GetTypeCode<T>(T obj)
        {
            var type = GetType(obj);
            return GetTypeCode(type, out _);
        }

        public static PrimitiveTypeCode GetTypeCode(Type t)
        {
            return GetTypeCode(t, out _);
        }

        public static PrimitiveTypeCode GetTypeCode(Type t, out bool isEnum)
        {
            if (TypeCodeMap.TryGetValue(t, out PrimitiveTypeCode typeCode))
            {
                isEnum = false;
                return typeCode;
            }

            if (t.IsEnum)
            {
                isEnum = true;
                return GetTypeCode(Enum.GetUnderlyingType(t));
            }

            // performance?
            if (IsNullableType(t))
            {
                Type nonNullable = Nullable.GetUnderlyingType(t);
                if (nonNullable != null && nonNullable.IsEnum)
                {
                    Type nullableUnderlyingType = typeof(Nullable<>).MakeGenericType(Enum.GetUnderlyingType(nonNullable));
                    isEnum = true;
                    return GetTypeCode(nullableUnderlyingType);
                }
            }

            isEnum = false;
            return PrimitiveTypeCode.Object;
        }

        public static bool IsNullableType(Type t)
        {
            return (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>));
        }

        public static Type GetType<T>(T obj)
        {
            return typeof(T);
        }
    }

    internal enum PrimitiveTypeCode
    {
        Empty = 0,
        Object = 1,
        Char = 2,
        CharNullable = 3,
        Boolean = 4,
        BooleanNullable = 5,
        SByte = 6,
        SByteNullable = 7,
        Int16 = 8,
        Int16Nullable = 9,
        UInt16 = 10,
        UInt16Nullable = 11,
        Int32 = 12,
        Int32Nullable = 13,
        Byte = 14,
        ByteNullable = 15,
        UInt32 = 16,
        UInt32Nullable = 17,
        Int64 = 18,
        Int64Nullable = 19,
        UInt64 = 20,
        UInt64Nullable = 21,
        Single = 22,
        SingleNullable = 23,
        Double = 24,
        DoubleNullable = 25,
        DateTime = 26,
        DateTimeNullable = 27,
        DateTimeOffset = 28,
        DateTimeOffsetNullable = 29,
        Decimal = 30,
        DecimalNullable = 31,
        Guid = 32,
        GuidNullable = 33,
        TimeSpan = 34,
        TimeSpanNullable = 35,
        BigInteger = 36,
        BigIntegerNullable = 37,
        Uri = 38,
        String = 39,
        Bytes = 40,
        //DBNull = 41
    }
}
