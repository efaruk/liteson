using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using SlowMember;

namespace Liteson
{

    public interface ITextSerializer
    {
        string Serialize<T>(T obj, List<string> excludes = null);
        T Deserialize<T>(string data);
    }

    public class LitesonSerializer : ITextSerializer
    {
        private readonly CultureInfo _culture;
        private ReflectionService _reflectionService;
        private const string NewLine = "\n";
        private const char SeparatorChar = '|';
        private static readonly char[] Separator = { SeparatorChar };


        public LitesonSerializer(CultureInfo culture)
        {
            _culture = culture ?? throw new ArgumentNullException(nameof(culture));
            _reflectionService = new ReflectionService();
        }

        public string Serialize<T>(T obj, List<string> excludes = null)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj), "Values can not be null");
            var objDesc = _reflectionService.GetObjectDescription(typeof(T));
            var sb = new StringBuilder(NewLine, objDesc.MemberDescriptions.Count);
            if (objDesc.IsEnumerable)
            {
                // Serialize List<TRow>
                var enumerable = (IEnumerable) obj;
                foreach (var e in enumerable)
                {
                    var edata = Serialize(e, excludes);
                    sb.Append($"{NewLine}{edata}");
                }
            }
            else
            {
                if (objDesc?.MemberDescriptions == null || !objDesc.MemberDescriptions.Any()) return null;
                // Serialize TRow
                var fieldsNProps = objDesc.MemberDescriptions.Where(d =>
                    d.MemberType.MemberType == MemberTypes.Field || d.MemberType.MemberType == MemberTypes.Property);
                foreach (var memDesc in fieldsNProps)
                {
                    if (excludes != null && excludes.Contains(memDesc.Name)) continue;
                    if (memDesc.IsEnumerable 
                        || memDesc.MemberType.IsAbstract
                        || memDesc.MemberType.IsGenericType
                        || memDesc.MemberType.IsInterface
                        || memDesc.MemberType.IsNested
                        || memDesc.MemberType.IsSealed) continue;
                    var val = memDesc.GetValue(obj);
                    if (memDesc.MemberType.IsEnum)
                    {
                        sb.Append($"{val}{Separator[0]}");
                        continue;
                    }
                    var typeCode = Utils.GetTypeCode(memDesc.MemberType);
                    switch (typeCode)
                    {
                        
                        case PrimitiveTypeCode.Boolean:
                        case PrimitiveTypeCode.BooleanNullable:
                        case PrimitiveTypeCode.Byte:
                        case PrimitiveTypeCode.Bytes:
                        case PrimitiveTypeCode.Char:
                        case PrimitiveTypeCode.DateTime:
                        case PrimitiveTypeCode.DateTimeOffset:
                        case PrimitiveTypeCode.Decimal:
                        case PrimitiveTypeCode.Double:
                        case PrimitiveTypeCode.Guid:
                        
                        case PrimitiveTypeCode.Object:
                        case PrimitiveTypeCode.SByte:
                        case PrimitiveTypeCode.Single:
                        case PrimitiveTypeCode.String:
                        case PrimitiveTypeCode.TimeSpan:
                        
                        case PrimitiveTypeCode.Uri:
                            break;
                        

                        case PrimitiveTypeCode.Int16:
                        case PrimitiveTypeCode.Int32:
                        case PrimitiveTypeCode.Int64:
                        case PrimitiveTypeCode.UInt16:
                        case PrimitiveTypeCode.UInt32:
                        case PrimitiveTypeCode.UInt64:
                        case PrimitiveTypeCode.BigInteger:
                        case PrimitiveTypeCode.BigIntegerNullable:
                            sb.Append($"{val}{Separator[0]}");
                            break;
                        case PrimitiveTypeCode.DBNull:
                            break;
                    }
                    
                    if (memDesc.MemberType.IsValueType)
                    {
                        switch (memDesc.MemberType.Name.ToLowerInvariant())
                        {
                            case "ushort":
                            case "short":
                            case "ulong":
                            case "long":
                            case "int":
                            case "int16":
                            case "uint16":
                            case "int32":
                            case "uint32":
                            case "int64":
                            case "uint64":
                            case "biginteger":
                                sb.Append($"{val}{Separator[0]}");
                                break;
                            case "single":
                            case "double":
                            case "decimal":
                                sb.Append($"{((decimal)val).ToString(_culture)}{Separator[0]}");
                                break;
                            case "date":
                                sb.Append($"{((decimal)val).ToString(_culture)}{Separator[0]}");
                                break;
                        }
                        continue;
                    }
                    
                    if (memDesc.MemberType == typeof(string))
                    {
                        sb.Append($"{val}{SeparatorChar}");
                        continue;
                    }
                }
            }
            
            var data = sb.ToString().TrimEnd('|');

            return sb.ToString();
        }

        public T Deserialize<T>(string data)
        {
            throw new NotImplementedException();
        }

        
    }

    
}
