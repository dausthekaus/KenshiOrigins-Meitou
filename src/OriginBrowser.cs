namespace KenshiOrigins;

/// <summary>Search and category presentation; never reorders the catalog used by character drafts.</summary>
internal static class OriginBrowser
{
    internal static IReadOnlyList<string> Tags(OriginCatalog catalog) => catalog.All.SelectMany(origin => origin.Tags)
        .Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToArray();

    internal static IReadOnlyList<OriginDefinition> Filter(OriginCatalog catalog, string query, string tag)
    {
        var words = query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return catalog.All.Where(origin =>
        {
            if (tag.Length > 0 && !origin.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase)) return false;
            string text = $"{origin.Name} {origin.Description} {origin.Difficulty} {string.Join(' ', origin.Tags)}";
            return words.All(word => text.Contains(word, StringComparison.OrdinalIgnoreCase));
        }).OrderBy(origin => origin.Name, StringComparer.OrdinalIgnoreCase).ThenBy(origin => origin.Id, StringComparer.Ordinal).ToArray();
    }
}
