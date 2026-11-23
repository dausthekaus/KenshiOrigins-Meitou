using System.Globalization;
using System.Text.RegularExpressions;
using Meitou.Data.Gameplay.Bodies;

namespace KenshiOrigins;

/// <summary>An immutable definition shared by characters; their assignments store its stable ID.</summary>
internal sealed record OriginDefinition(string Id, string Name, string Description, IReadOnlyDictionary<StatsEnumerated, float> Bonuses,
    IReadOnlyList<OriginItem> Items, string Difficulty)
{
    internal IReadOnlyList<string> Tags { get; init; } = [];
    internal bool StartingCharactersOnly { get; init; }
    internal bool TestingOnly { get; init; }

    internal string BonusSummary => Bonuses.Count == 0 ? "No stat bonuses" : string.Join(", ", Bonuses.OrderBy(b => b.Key)
        .Select(b => $"{b.Value.ToString("+0.##;-0.##;0", CultureInfo.InvariantCulture)} {StatName(b.Key)}"));

    private static string StatName(StatsEnumerated stat) => Regex.Replace(stat.ToString(), "([a-z])([A-Z])", "$1 $2");
}

/// <summary>The JSON shape. Conversion validates names and resolves stat strings to the engine's enum.</summary>
internal sealed class OriginFile
{
    public int FormatVersion { get; init; }
    public List<OriginJson>? Origins { get; init; }
}

internal sealed class OriginJson
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? Difficulty { get; init; }
    public List<string>? Tags { get; init; }
    public bool StartingCharactersOnly { get; init; }
    public bool TestingOnly { get; init; }
    public Dictionary<string, float>? Bonuses { get; init; }
    public List<OriginItemJson>? Items { get; init; }

    internal OriginDefinition Validate()
    {
        if (string.IsNullOrWhiteSpace(Id) || !Regex.IsMatch(Id, @"^[a-z0-9][a-z0-9_.-]*\.[a-z0-9_-]+$"))
            throw new InvalidDataException("Each origin needs a stable ID such as extraorigins.dockworker.");
        if (string.IsNullOrWhiteSpace(Name) || Description is null || Bonuses is null)
            throw new InvalidDataException($"Origin '{Id}' needs a name, description and bonuses object.");

        var resolved = new Dictionary<StatsEnumerated, float>();
        foreach (var (name, amount) in Bonuses)
        {
            if (!Enum.TryParse<StatsEnumerated>(name, ignoreCase: true, out var stat) || !Enum.IsDefined(stat)
                || !string.Equals(name, stat.ToString(), StringComparison.OrdinalIgnoreCase) || !float.IsFinite(amount))
                throw new InvalidDataException($"Origin '{Id}' has an unknown stat or invalid amount: '{name}'.");
            var storage = StatsInfo.StorageOf(stat);
            if (storage == StatsEnumerated.None || !resolved.TryAdd(storage, amount))
                throw new InvalidDataException($"Origin '{Id}' has an unsupported stat or repeats shared stat storage: '{name}'.");
        }
        var items = Items?.Select(item => item?.Validate()
            ?? throw new InvalidDataException($"Origin '{Id}' has a null item.")).ToArray() ?? [];
        var tags = Tags?.Select(tag => string.IsNullOrWhiteSpace(tag)
            ? throw new InvalidDataException($"Origin '{Id}' has an empty tag.") : tag.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase).ToArray() ?? [];
        return new OriginDefinition(Id, Name.Trim(), Description.Trim(), resolved.AsReadOnly(), Array.AsReadOnly(items),
            string.IsNullOrWhiteSpace(Difficulty) ? "Unrated" : Difficulty.Trim())
        {
            Tags = Array.AsReadOnly(tags), StartingCharactersOnly = StartingCharactersOnly, TestingOnly = TestingOnly
        };
    }
}

/// <summary>An inventory grant, addressed by the stable FCS record ID rather than its translated name.</summary>
internal sealed record OriginItem(string Record, int Quantity, int Quality);

internal sealed class OriginItemJson
{
    public string? Record { get; init; }
    public int Quantity { get; init; } = 1;
    public int Quality { get; init; }

    internal OriginItem Validate()
    {
        if (string.IsNullOrWhiteSpace(Record) || Quantity is < 1 or > 1000 || Quality is < 0 or > 100)
            throw new InvalidDataException("Origin items need a record ID, quantity 1–1000 and quality 0–100.");
        return new OriginItem(Record.Trim(), Quantity, Quality);
    }
}
