using System.Text.RegularExpressions;

namespace BikMetric.Infrastructure.Services;

public static partial class SqlSanitizer
{
    private static readonly Dictionary<char, char> TurkishCharMap = new()
    {
        ['ç'] = 'c', ['Ç'] = 'C',
        ['ğ'] = 'g', ['Ğ'] = 'G',
        ['ı'] = 'i', ['İ'] = 'I',
        ['ö'] = 'o', ['Ö'] = 'O',
        ['ş'] = 's', ['Ş'] = 'S',
        ['ü'] = 'u', ['Ü'] = 'U',
    };

    public static string Sanitize(string sql)
    {
        // Remove prefixed function calls like F.SUM -> SUM
        sql = FunctionPrefixRegex().Replace(sql, "$1");

        // Replace Turkish characters with ASCII equivalents
        var chars = sql.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            if (TurkishCharMap.TryGetValue(chars[i], out var replacement))
                chars[i] = replacement;
        }
        sql = new string(chars);

        // Remove trailing semicolons
        sql = sql.TrimEnd().TrimEnd(';').Trim();

        return sql;
    }

    [GeneratedRegex(@"\b[A-Z]\.(formatDateTime|toString|toFloat64|toUInt64|toDate|SUM|COUNT|AVG|MIN|MAX)\b", RegexOptions.IgnoreCase)]
    private static partial Regex FunctionPrefixRegex();
}
