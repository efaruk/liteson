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
        private static readonly char[] DefaultColumnSeparator = { '|' };
        private readonly char[] _columnSeparator;
        private readonly string _columnSeparatorString;
        //private const char FieldSeperatorChar = '`';
        private static readonly char[] DefaultFieldSeperator = { '`' };
        private readonly char[] _fieldSeperator;
        private readonly string _fieldSeperatorString;
        private static readonly char[] DefaultFieldItemSeperator = { '^' };
        private readonly char[] _fieldItemSeperator;
        private readonly string _fieldItemSeperatorString;
        private const string NullString = "null";

        public LitesonSerializer(CultureInfo culture, char[] columnSeparator = null, char[] fieldSeperator = null, char[] fieldItemSeperator = null)
        {
            // Setup Culture for Culture specific operations
            _culture = culture ?? throw new ArgumentNullException(nameof(culture));
            // Setup Column Separator
            _columnSeparator = columnSeparator ?? DefaultColumnSeparator;
            _columnSeparatorString = new string(_columnSeparator);
            // Setup Field Separator
            _fieldSeperator = fieldSeperator ?? DefaultFieldSeperator;
            _fieldSeperatorString = new string(_fieldSeperator);
            // Setup Field Item Separator
            _fieldItemSeperator = fieldItemSeperator ?? DefaultFieldItemSeperator;
            _fieldItemSeperatorString = new string(_fieldSeperator);
            // Reflection Service
            _reflectionService = new ReflectionService();
        }

        public string SerializeRows<TRow>(List<TRow> rows, List<string> excludes = null) where TRow : class, new()
        {
            if (rows == null || !rows.Any()) return null;
            var sb = new StringBuilder(rows.Count);
            // Serialize List<TRow>
            foreach (var row in rows)
            {
                var rowString = SerializeRow(row, excludes);
                if (string.IsNullOrWhiteSpace(rowString)) continue;
                sb.Append($"{NewLineChar}{SerializeRow(row, excludes)}");
            }
            return sb.ToString();
        }

        public string SerializeRow<TRow>(TRow row, List<string> excludes = null) where TRow : class, new()
        {
            if (row == null) return null;
            var objDesc = _reflectionService.GetObjectDescription(typeof(TRow));
            var sb = new StringBuilder(objDesc.MemberDescriptions.Count);
            if (objDesc.MemberDescriptions == null || !objDesc.MemberDescriptions.Any()) return null;
            // Serialize TRow
            foreach (var memDesc in objDesc.MemberDescriptions)
            {
                if (excludes != null && excludes.Contains(memDesc.Name)) continue;
                var val = memDesc.GetValue(row);
                if (val == null)
                {
                    sb.Append($"{NullString}{_columnSeparatorString}");
                    continue;
                }
                if (memDesc.IsEnumerable)
                {
                    // Serialize Fields
                    var enumerable = (IEnumerable) val;
                    var sbf = new StringBuilder();
                    foreach (var item in enumerable)
                    {
                        var fieldString = SerializeField(item, excludes);
                        sbf.Append($"{fieldString}{_fieldSeperatorString}");
                    }
                    sb.Append($"{sbf}{_columnSeparatorString}");
                    continue;
                }
                if (memDesc.Type.IsEnum)
                {
                    sb.Append($"{val}{_columnSeparatorString}");
                    continue;
                }
                var typeCode = Utils.GetTypeCode(memDesc.Type);
                var valueString = GetValueString(typeCode, val);
                sb.Append($"{valueString}{_columnSeparatorString}");
            }
            return sb.ToString();
        }

        private string SerializeField<TField>(TField field, List<string> excludes = null)
        {
            if (field == null) return null;
            var fieldDesc = _reflectionService.GetObjectDescription(field.GetType());
            var fieldTypeCode = Utils.GetTypeCode(fieldDesc.Type);
            string valueString;
            if (fieldDesc.MemberDescriptions == null || !fieldDesc.MemberDescriptions.Any())
            {
                //Simple type
                valueString = GetValueString(fieldTypeCode, field);
            }
            else
            {
                // Serialize Non-Primitive Field
                var sb = new StringBuilder(fieldDesc.MemberDescriptions.Count);
                foreach (var fieldItemDesc in fieldDesc.MemberDescriptions)
                {
                    var fieldItemValue = fieldItemDesc.GetValue(field);
                    var fieldItemTypeCode = Utils.GetTypeCode(fieldItemDesc.Type);
                    var subFieldValueString = GetValueString(fieldItemTypeCode, fieldItemValue);
                    sb.Append($"{subFieldValueString}{_fieldItemSeperatorString}");
                }
                valueString = sb.ToString();
            }
            return valueString;
        }
        
        public List<TRow> DeserializeRows<TRow>(string data, List<string> excludes = null) where TRow : class, new()
        {
            if (string.IsNullOrWhiteSpace(data)) return null;
            var lines = data.Split(NewLineSeparator, StringSplitOptions.RemoveEmptyEntries);
            var result = new List<TRow>(lines.Length);
            foreach (var line in lines)
            {
                var row = DeserializeRow<TRow>(line);
                if (row == null) continue;
                result.Add(row);
            }
            return result;
        }

        public TRow DeserializeRow<TRow>(string line, List<string> excludes = null) where TRow : class, new()
        {
            var result = new TRow();
            if (string.IsNullOrWhiteSpace(line)) return null;
            var objDesc = _reflectionService.GetObjectDescription(typeof(TRow));
            if (objDesc?.MemberDescriptions == null || !objDesc.MemberDescriptions.Any()) return null;
            var rowValues = line.Split(_columnSeparator, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < objDesc.MemberDescriptions.Count; i++)
            {
                var memDesc = objDesc.MemberDescriptions[i];
                if (excludes != null && excludes.Contains(memDesc.Name)) continue;
                var valueString = rowValues[i];
                if (string.IsNullOrWhiteSpace(valueString) || valueString == NullString)
                {
                    continue;
                }
                if (memDesc.IsEnumerable)
                {
                    // CustomListWithFieldsOrProperties<Class> or List<Class> or List<string>
                    IEnumerable enumerable = null;
                    var memObjDesc = _reflectionService.GetObjectDescription(memDesc.Type);
                    if (memObjDesc?.MemberDescriptions == null || !memObjDesc.MemberDescriptions.Any())
                    {
                        // Simple IEnumerable
                        // List<Class> or List<string> or string[], etc.
                        
                    }
                    else
                    {
                        // Complex IEnumerable
                        // CustomListWithFieldsOrProperties<Class>
                    }
                    memDesc.SetValue(result, enumerable);
                    continue;
                }
                if (memDesc.Type.IsEnum)
                {
                    memDesc.SetValue(result, Enum.Parse(memDesc.Type, valueString, true));
                    continue;
                }
                var typeCode = Utils.GetTypeCode(memDesc.Type);
                var val = GetValue(typeCode, valueString);
                if (val != null)
                {
                    memDesc.SetValue(result, val);
                }
            }
            return result;
        }

        private object DeserializeField(string field, List<string> excludes = null)
        {
            throw new NotImplementedException();
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

        private object GetValue(PrimitiveTypeCode typeCode, string valueString)
        {
            object result = null;
            switch (typeCode)
            {
                case PrimitiveTypeCode.Char:
                case PrimitiveTypeCode.CharNullable:
                    result = char.Parse(valueString);
                    break;
                case PrimitiveTypeCode.Single:
                case PrimitiveTypeCode.SingleNullable:
                    result = float.Parse(valueString, _culture);
                    break;
                case PrimitiveTypeCode.Double:
                case PrimitiveTypeCode.DoubleNullable:
                    result = double.Parse(valueString, _culture);
                    break;
                case PrimitiveTypeCode.Decimal:
                case PrimitiveTypeCode.DecimalNullable:
                    result = decimal.Parse(valueString, _culture);
                    break;
                case PrimitiveTypeCode.Guid:
                case PrimitiveTypeCode.GuidNullable:
                    result = Guid.Parse(valueString);
                    break;
                case PrimitiveTypeCode.String:
                    result = valueString;
                    break;
                case PrimitiveTypeCode.Uri:
                    result = new Uri(valueString);
                    break;
                case PrimitiveTypeCode.DateTime:
                case PrimitiveTypeCode.DateTimeNullable:
                    result = DateTime.Parse(valueString);
                    break;
                case PrimitiveTypeCode.DateTimeOffset:
                case PrimitiveTypeCode.DateTimeOffsetNullable:
                    result = DateTimeOffset.Parse(valueString);
                    break;
                case PrimitiveTypeCode.TimeSpan:
                case PrimitiveTypeCode.TimeSpanNullable:
                    result = new TimeSpan(long.Parse(valueString));
                    break;
                case PrimitiveTypeCode.Boolean:
                case PrimitiveTypeCode.BooleanNullable:
                    result = bool.Parse(valueString);
                    break;
                case PrimitiveTypeCode.Byte:
                case PrimitiveTypeCode.ByteNullable:
                    result = byte.Parse(valueString);
                    break;
                case PrimitiveTypeCode.SByte:
                case PrimitiveTypeCode.SByteNullable:
                    result = sbyte.Parse(valueString);
                    break;
                case PrimitiveTypeCode.Int16:
                case PrimitiveTypeCode.Int16Nullable:
                    result = short.Parse(valueString);
                    break;
                case PrimitiveTypeCode.Int32:
                case PrimitiveTypeCode.Int32Nullable:
                    result = int.Parse(valueString);
                    break;
                case PrimitiveTypeCode.Int64:
                case PrimitiveTypeCode.Int64Nullable:
                    result = long.Parse(valueString);
                    break;
                case PrimitiveTypeCode.UInt16:
                case PrimitiveTypeCode.UInt16Nullable:
                    result = ushort.Parse(valueString);
                    break;
                case PrimitiveTypeCode.UInt32:
                case PrimitiveTypeCode.UInt32Nullable:
                    result = uint.Parse(valueString);
                    break;
                case PrimitiveTypeCode.UInt64:
                case PrimitiveTypeCode.UInt64Nullable:
                    result = ulong.Parse(valueString);
                    break;
                case PrimitiveTypeCode.BigInteger:
                case PrimitiveTypeCode.BigIntegerNullable:
                    result = BigInteger.Parse(valueString);
                    break;
                case PrimitiveTypeCode.Bytes:
                    result = Encoding.UTF8.GetBytes(valueString);
                    break;
                case PrimitiveTypeCode.Object:
                    break;
            }
            return result;
        }

        
    }
}
