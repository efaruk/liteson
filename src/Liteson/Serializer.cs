using System;
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

    public class LitesonSerializer: ITextSerializer
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
            if (objDesc?.MemberDescriptions == null || !objDesc.MemberDescriptions.Any()) return null;
            var sb = new StringBuilder(NewLine, objDesc.MemberDescriptions.Count);
            if (objDesc.Type.IsClass)
            {
                var fieldsNProps = objDesc.MemberDescriptions.Where(d =>
                    d.MemberType.MemberType == MemberTypes.Field || d.MemberType.MemberType == MemberTypes.Property);
                foreach (var memDesc in fieldsNProps)
                {
                    if (excludes != null && excludes.Contains(memDesc.Name)) continue;
                    if (memDesc.MemberType.IsAbstract 
                        || memDesc.MemberType.IsGenericType 
                        || memDesc.MemberType.IsInterface 
                        || memDesc.MemberType.IsNested
                        || memDesc.MemberType.IsSealed) continue;
                    var val = memDesc.GetValue(obj);
                    if (memDesc.MemberType.IsClass)
                    {
                        sb.Append(memDesc.MemberType == typeof(string)
                            ? $"{val}{SeparatorChar}"
                            : Serialize(val, excludes));
                        continue;
                    }
                    if (memDesc.MemberType.IsEnum)
                    {
                        sb.Append($"{val}{Separator[0]}");
                        continue;
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

                    if (memDesc.IsEnumerable)
                    {
                        var enumerable = (IEnumerable<object>)val;
                        foreach (var o in enumerable)
                        {
                            
                        }
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
