using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Liteson
{
    public class JsonTextSerializer : ITextSerializer
    {
        private static JsonSerializerSettings _jsonSerializerSettings;
        private const char NewLineChar = '\n';
        private static readonly char[] NewLineSeparator = { NewLineChar };

        public JsonTextSerializer(CultureInfo culture)
        {
            var serializationCulture = culture ?? throw new ArgumentNullException(nameof(culture), "Culture can not be null!");
            _jsonSerializerSettings = new JsonSerializerSettings
            {
                Culture = serializationCulture,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.None,
                //FloatParseHandling = FloatParseHandling.Decimal,
                MaxDepth = 4,
            };
            //_jsonSerializerSettings.FloatFormatHandling = FloatFormatHandling.DefaultValue;
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
            return JsonConvert.SerializeObject(row, _jsonSerializerSettings);
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
            return JsonConvert.DeserializeObject<TRow>(line, _jsonSerializerSettings);
        }
    }
}