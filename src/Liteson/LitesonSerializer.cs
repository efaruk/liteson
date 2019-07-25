using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using SlowMember;

namespace Liteson
{
    public class LitesonSerializer : ITextSerializer
    {
        private readonly CultureInfo _culture;
        private readonly ReflectionService _reflectionService;
        private const char NewLineChar = '\n';
        private static readonly char[] NewLineSeparator = { NewLineChar };
        //private const char ColumnSeparatorChar = '|';
        private static readonly char[] ColumnSeparator = { '<', '|', '>' };
        private static readonly string ColumnSeparatorString = new string(ColumnSeparator);
        //private const char FieldSeperatorChar = '`';
        private static readonly char[] FieldSeperator = { '<', '@', '>' };
        private static readonly string FieldSeperatorString = new string(FieldSeperator);
        private const string NullString = "null";

        public LitesonSerializer(CultureInfo culture)
        {
            _culture = culture ?? throw new ArgumentNullException(nameof(culture));
            _reflectionService = new ReflectionService();
        }

        public string SerializeRows<TRow>(List<TRow> rows, List<string> excludes = null) where TRow : class, new()
        {
            if (rows == null || !rows.Any()) throw new ArgumentNullException(nameof(rows), "Values can not be null");
            var sb = new StringBuilder(rows.Count);
            // Serialize List<TRow>
            foreach (var row in rows)
            {
                var edata = SerializeRow(row, excludes);
                sb.Append($"{NewLineChar}{edata}");
            }
            return sb.ToString();
        }

        public string SerializeRow<TRow>(TRow row, List<string> excludes = null) where TRow : class, new()
        {
            if (row == null) throw new ArgumentNullException(nameof(row), "Values can not be null");
            var objDesc = _reflectionService.GetObjectDescription(typeof(TRow));
            var sb = new StringBuilder(objDesc.MemberDescriptions.Count);
            if (objDesc.MemberDescriptions == null || !objDesc.MemberDescriptions.Any()) return null;
            // Serialize TRow
            for (var i = 0; i < objDesc.MemberDescriptions.Count; i++)
            {
                var memDesc = objDesc.MemberDescriptions[i];
                if (excludes != null && excludes.Contains(memDesc.Name)) continue;
                var val = memDesc.GetValue(row);
                if (memDesc.IsEnumerable)
                {
                    var enumerable = (IEnumerable)val;
                    foreach (var item in enumerable)
                    {
                        var fieldString = SerializeField(item, excludes);
                        sb.Append($"{fieldString}{FieldSeperatorString}");
                    }
                    sb.Append($"{ColumnSeparatorString}");
                    continue;
                }
                if (val == null)
                {
                    sb.Append($"{NullString}{ColumnSeparatorString}");
                    continue;
                }

                if (memDesc.Type.IsEnum)
                {
                    sb.Append($"{val}{ColumnSeparatorString}");
                    continue;
                }
                var typeCode = Utils.GetTypeCode(memDesc.Type);
                var valueString = GetValueString(typeCode, val);
                sb.Append($"{valueString}{ColumnSeparatorString}");
            }
            return sb.ToString().TrimEnd(ColumnSeparator);
        }

        

        private string SerializeField<TField>(TField field, List<string> excludes = null)
        {
            if (field == null) throw new ArgumentNullException(nameof(field), "Values can not be null");
            var fieldDesc = _reflectionService.GetObjectDescription(field.GetType());
            var fieldTypeCode = Utils.GetTypeCode(fieldDesc.Type);
            var valueString = NullString;
            if (fieldDesc.MemberDescriptions == null || !fieldDesc.MemberDescriptions.Any())
            {
                //Simple type
                valueString = GetValueString(fieldTypeCode, field);
            }
            else
            {
                var sb = new StringBuilder(fieldDesc.MemberDescriptions.Count);
                for (var i = 0; i < fieldDesc.MemberDescriptions.Count; i++)
                {
                    var memDesc = fieldDesc.MemberDescriptions[i];
                    var fieldValue = memDesc.GetValue(field);
                    var subFieldTypeCode = Utils.GetTypeCode(fieldDesc.Type);
                }
            }
            return valueString;
        }

        private string GetValueString(PrimitiveTypeCode typeCode, object val)
        {
            // Serialize Value Types
            var valueString = NullString;
            switch (typeCode)
            {
                case PrimitiveTypeCode.Char:
                case PrimitiveTypeCode.CharNullable:
                    valueString = ((char)val).ToString();
                    break;
                case PrimitiveTypeCode.Single:
                case PrimitiveTypeCode.SingleNullable:
                    valueString = ((float)val).ToString("R", _culture);
                    break;
                case PrimitiveTypeCode.Double:
                case PrimitiveTypeCode.DoubleNullable:
                    valueString = ((double)val).ToString("R", _culture);
                    break;
                case PrimitiveTypeCode.Decimal:
                case PrimitiveTypeCode.DecimalNullable:
                    valueString = ((decimal)val).ToString(_culture);
                    break;
                case PrimitiveTypeCode.Guid:
                case PrimitiveTypeCode.GuidNullable:
                case PrimitiveTypeCode.String:
                case PrimitiveTypeCode.Uri:
                    valueString = val.ToString();
                    break;
                case PrimitiveTypeCode.DateTime:
                case PrimitiveTypeCode.DateTimeNullable:
                    valueString = ((DateTime)val).ToString("O");
                    break;
                case PrimitiveTypeCode.DateTimeOffset:
                case PrimitiveTypeCode.DateTimeOffsetNullable:
                    valueString = ((DateTimeOffset)val).ToString("O");
                    break;
                case PrimitiveTypeCode.TimeSpan:
                case PrimitiveTypeCode.TimeSpanNullable:
                    valueString = ((TimeSpan)val).Ticks.ToString();
                    break;
                case PrimitiveTypeCode.Boolean:
                case PrimitiveTypeCode.BooleanNullable:
                    valueString = ((bool)val).ToString();
                    break;
                case PrimitiveTypeCode.Byte:
                case PrimitiveTypeCode.ByteNullable:
                    valueString = ((byte)val).ToString();
                    break;
                case PrimitiveTypeCode.SByte:
                case PrimitiveTypeCode.SByteNullable:
                    valueString = ((sbyte)val).ToString();
                    break;
                case PrimitiveTypeCode.Int16:
                case PrimitiveTypeCode.Int16Nullable:
                    valueString = ((short)val).ToString();
                    break;
                case PrimitiveTypeCode.Int32:
                case PrimitiveTypeCode.Int32Nullable:
                    valueString = ((int)val).ToString();
                    break;
                case PrimitiveTypeCode.Int64:
                case PrimitiveTypeCode.Int64Nullable:
                    valueString = ((long)val).ToString();
                    break;
                case PrimitiveTypeCode.UInt16:
                case PrimitiveTypeCode.UInt16Nullable:
                    valueString = ((ushort)val).ToString();
                    break;
                case PrimitiveTypeCode.UInt32:
                case PrimitiveTypeCode.UInt32Nullable:
                    valueString = ((uint)val).ToString();
                    break;
                case PrimitiveTypeCode.UInt64:
                case PrimitiveTypeCode.UInt64Nullable:
                    valueString = ((ulong)val).ToString();
                    break;
                case PrimitiveTypeCode.BigInteger:
                case PrimitiveTypeCode.BigIntegerNullable:
                    valueString = ((BigInteger)val).ToString();
                    break;
                case PrimitiveTypeCode.Bytes:
                    valueString = Encoding.UTF8.GetString((byte[])val);
                    break;
                case PrimitiveTypeCode.Object:
                    break;
            }
            return valueString;
        }

        public List<TRow> DeserializeRows<TRow>(string data, List<string> excludes = null) where TRow : class, new()
        {
            if (string.IsNullOrWhiteSpace(data)) throw new ArgumentNullException(nameof(data), "Liteson data can not be null or empty");
            var lines = data.Split(NewLineSeparator, StringSplitOptions.RemoveEmptyEntries);
            var result = new List<TRow>(lines.Length);
            foreach (var line in lines)
            {
                result.Add(DeserializeRow<TRow>(line));
            }
            return result;
        }

        public TRow DeserializeRow<TRow>(string line, List<string> excludes = null) where TRow : class, new()
        {
            var result = new TRow();
            if (string.IsNullOrWhiteSpace(line))
                throw new ArgumentNullException(nameof(line), "Liteson data can not be null or empty");
            var objDesc = _reflectionService.GetObjectDescription(typeof(TRow));
            if (objDesc?.MemberDescriptions == null || !objDesc.MemberDescriptions.Any()) return null;
            var rowValues = line.Split(ColumnSeparator, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < objDesc.MemberDescriptions.Count; i++)
            {
                var memDesc = objDesc.MemberDescriptions[i];
                if (excludes != null && excludes.Contains(memDesc.Name)) continue;
                var valueString = rowValues[i];
                if (string.IsNullOrWhiteSpace(valueString) || valueString == NullString)
                {
                    continue;
                }

                if (memDesc.Type.IsEnum)
                {
                    memDesc.SetValue(result, Enum.Parse(memDesc.Type, valueString, true));
                    continue;
                }
                var typeCode = Utils.GetTypeCode(memDesc.Type);
                object val = null;
                switch (typeCode)
                {
                    case PrimitiveTypeCode.Char:
                    case PrimitiveTypeCode.CharNullable:
                        val = char.Parse(valueString);
                        break;
                    case PrimitiveTypeCode.Single:
                    case PrimitiveTypeCode.SingleNullable:
                        val = float.Parse(valueString, _culture);
                        break;
                    case PrimitiveTypeCode.Double:
                    case PrimitiveTypeCode.DoubleNullable:
                        val = double.Parse(valueString, _culture);
                        break;
                    case PrimitiveTypeCode.Decimal:
                    case PrimitiveTypeCode.DecimalNullable:
                        val = decimal.Parse(valueString, _culture);
                        break;
                    case PrimitiveTypeCode.Guid:
                    case PrimitiveTypeCode.GuidNullable:
                        val = Guid.Parse(valueString);
                        break;
                    case PrimitiveTypeCode.String:
                        val = valueString;
                        break;
                    case PrimitiveTypeCode.Uri:
                        val = new Uri(valueString);
                        break;
                    case PrimitiveTypeCode.DateTime:
                    case PrimitiveTypeCode.DateTimeNullable:
                        val = DateTime.Parse(valueString);
                        break;
                    case PrimitiveTypeCode.DateTimeOffset:
                    case PrimitiveTypeCode.DateTimeOffsetNullable:
                        val = DateTimeOffset.Parse(valueString);
                        break;
                    case PrimitiveTypeCode.TimeSpan:
                    case PrimitiveTypeCode.TimeSpanNullable:
                        val = new TimeSpan(long.Parse(valueString));
                        break;
                    case PrimitiveTypeCode.Boolean:
                    case PrimitiveTypeCode.BooleanNullable:
                        val = bool.Parse(valueString);
                        break;
                    case PrimitiveTypeCode.Byte:
                    case PrimitiveTypeCode.ByteNullable:
                        val = byte.Parse(valueString);
                        break;
                    case PrimitiveTypeCode.SByte:
                    case PrimitiveTypeCode.SByteNullable:
                        val = sbyte.Parse(valueString);
                        break;
                    case PrimitiveTypeCode.Int16:
                    case PrimitiveTypeCode.Int16Nullable:
                        val = short.Parse(valueString);
                        break;
                    case PrimitiveTypeCode.Int32:
                    case PrimitiveTypeCode.Int32Nullable:
                        val = int.Parse(valueString);
                        break;
                    case PrimitiveTypeCode.Int64:
                    case PrimitiveTypeCode.Int64Nullable:
                        val = long.Parse(valueString);
                        break;
                    case PrimitiveTypeCode.UInt16:
                    case PrimitiveTypeCode.UInt16Nullable:
                        val = ushort.Parse(valueString);
                        break;
                    case PrimitiveTypeCode.UInt32:
                    case PrimitiveTypeCode.UInt32Nullable:
                        val = uint.Parse(valueString);
                        break;
                    case PrimitiveTypeCode.UInt64:
                    case PrimitiveTypeCode.UInt64Nullable:
                        val = ulong.Parse(valueString);
                        break;
                    case PrimitiveTypeCode.BigInteger:
                    case PrimitiveTypeCode.BigIntegerNullable:
                        val = BigInteger.Parse(valueString);
                        break;
                    case PrimitiveTypeCode.Bytes:
                        val = Encoding.UTF8.GetBytes(valueString);
                        break;
                    case PrimitiveTypeCode.Object:
                        break;
                }
                if (val != null)
                {
                    memDesc.SetValue(result, val);
                }
            }
            return result;
        }

    }
}
