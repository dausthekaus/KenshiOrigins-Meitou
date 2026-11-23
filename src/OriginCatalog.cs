using System.Text.Json;
using System.Text.Json.Serialization;
using Meitou.Content;
using Meitou.Data;

namespace KenshiOrigins;

/// <summary>Loads our definitions first, then direct dependent packs in the enabled load order.</summary>
internal sealed class OriginCatalog
{
    internal const string FileName = "originform.json";
    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        WriteIndented = true
    };

    private readonly Dictionary<string, OriginDefinition> _byId = new(StringComparer.Ordinal);
    internal IReadOnlyList<OriginDefinition> All { get; }
    internal IReadOnlyList<int> RecruitChoices { get; }
    internal IReadOnlyList<int> StartingRandomChoices { get; }

    internal OriginCatalog(IEnumerable<OriginDefinition> definitions)
    {
        foreach (var origin in definitions) _byId.Add(origin.Id, origin);
        if (_byId.Count == 0) throw new InvalidDataException("The origin pool must not be empty.");
        All = _byId.Values.ToArray();
        // Keep the original catalog indices: creation drafts and saved IDs must not shift.
        StartingRandomChoices = Enumerable.Range(0, All.Count).Where(i => !All[i].TestingOnly).ToArray();
        RecruitChoices = StartingRandomChoices.Where(i => !All[i].StartingCharactersOnly).ToArray();
    }

    internal OriginDefinition? Find(string id) => _byId.GetValueOrDefault(id);

    internal int IndexOf(string id)
    {
        for (int i = 0; i < All.Count; i++)
            if (All[i].Id == id) return i;
        return -1;
    }

    internal static OriginCatalog Load(string folder, string modName, GameDatabase data, Action<string> log)
    {
        var definitions = ReadFile(Path.Combine(folder, FileName)).ToList();
        var install = GameInstall.Locate();
        if (install is null)
        {
            log("Could not locate Kenshi; only the built-in origin file was loaded.");
            return new OriginCatalog(definitions);
        }

        var discovered = ModList.Discover(install).ToDictionary(m => m.Name, StringComparer.Ordinal);
        // Files is the database's actual successful load set, rather than every installed mod.
        foreach (var file in data.Files)
        {
            string name = ModList.DependencyName(file);
            if (name == modName || !discovered.TryGetValue(name, out var mod)) continue;
            if (mod.Header is not { } header)
            {
                log($"Could not read '{file}' header; skipping its origin pack check.");
                continue;
            }
            if (header.Dependencies.Any(d => ModList.DependencyName(d) == modName))
                AddPack(definitions, mod.Folder, log);
        }
        log($"Loaded {definitions.Count} origin definitions.");
        return new OriginCatalog(definitions);
    }

    internal static IReadOnlyList<OriginDefinition> ReadFile(string path)
    {
        using var stream = File.OpenRead(path);
        var file = JsonSerializer.Deserialize<OriginFile>(stream, JsonOptions)
            ?? throw new InvalidDataException("The origin file is empty.");
        if (file.FormatVersion != 1 || file.Origins is not { Count: > 0 })
            throw new InvalidDataException("Expected formatVersion 1 and a nonempty origins array.");
        var definitions = file.Origins.Select(o => o?.Validate()
            ?? throw new InvalidDataException("An origin entry cannot be null.")).ToArray();
        if (definitions.Select(o => o.Id).Distinct(StringComparer.Ordinal).Count() != definitions.Length)
            throw new InvalidDataException("The file repeats an origin ID.");
        return definitions;
    }

    /// <summary>A bad optional pack is rejected as a whole; it cannot replace another pack's definitions.</summary>
    internal static void AddPack(List<OriginDefinition> pool, string folder, Action<string> log)
    {
        string path = Path.Combine(folder, FileName);
        if (!File.Exists(path)) return;
        try
        {
            var additions = ReadFile(path);
            if (additions.Any(a => pool.Any(existing => existing.Id == a.Id)))
                throw new InvalidDataException("An origin ID is already defined by an earlier pack.");
            pool.AddRange(additions);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException or InvalidDataException)
        {
            log($"Skipped origin pack '{path}': {e.Message}");
        }
    }
}
