namespace Catalog.Dal.Configurations;

internal static class ConfigurationHelpers
{
    public static string InList(string column, IEnumerable<string> values)
        => $"[{column}] IN ({string.Join(", ", values.Select(v => $"N'{v}'"))})";
}