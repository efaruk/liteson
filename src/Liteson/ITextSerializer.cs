using System.Collections.Generic;

namespace Liteson
{
    public interface ITextSerializer
    {
        string SerializeRows<TRow>(List<TRow> rows, List<string> excludes = null) where TRow : class, new();
        string SerializeRow<TRow>(TRow row, List<string> excludes = null) where TRow : class, new();
        List<TRow> DeserializeRows<TRow>(string data, List<string> excludes = null) where TRow : class, new();
        TRow DeserializeRow<TRow>(string line, List<string> excludes = null) where TRow : class, new();
    }
}