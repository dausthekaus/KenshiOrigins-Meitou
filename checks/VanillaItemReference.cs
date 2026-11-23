using System.Security.Cryptography;
using System.Text;
using KenshiOrigins;
using Meitou.Data;
using Meitou.Data.Fcs;
using Meitou.Data.Gameplay;
using Meitou.Simulation.Items;

/// <summary>Offline documentation export. Never reads the enabled-mod list or writes game data.</summary>
internal static class VanillaItemReference
{
    internal static void Write(string repo, string install)
    {
        var database = new GameDatabase();
        var fingerprints = new List<(string Name, string Hash)>();
        foreach (string name in LoadOrder.BaseFiles)
        {
            string path = Path.Combine(install, "data", name);
            // Fail instead of publishing a reference extracted from an incomplete installation.
            database.Apply(FcsReader.ReadFile(path), name);
            using var stream = File.OpenRead(path);
            fingerprints.Add((name, Convert.ToHexStringLower(SHA256.HashData(stream))));
        }
        var factory = new ItemFactory(database, GameConstants.Default);
        var rows = database.Records.Values.Where(record => Category(record.Type) is not null)
            .OrderBy(record => Category(record.Type), StringComparer.Ordinal)
            .ThenBy(record => record.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(record => record.StringId, StringComparer.Ordinal)
            .Select(record => new Row(Category(record.Type)!, record.Name, record.StringId, Notes(record))).ToArray();
        if (rows.Length == 0 || rows.Select(row => row.Record).Distinct(StringComparer.Ordinal).Count() != rows.Length)
            throw new InvalidDataException("Expected a nonempty reference with unique item IDs.");
        foreach (var row in rows)
            if (factory.Create(row.Record) is null)
                throw new InvalidDataException($"The item factory cannot create '{row.Record}'.");
        foreach (var origin in OriginCatalog.ReadFile(Path.Combine(repo, OriginCatalog.FileName)))
            foreach (var item in origin.Items)
                if (!rows.Any(row => row.Record == item.Record))
                    throw new InvalidDataException($"Bundled origin item '{item.Record}' is missing from the reference.");

        string versionPath = Path.Combine(install, "currentVersion.txt");
        string version = File.Exists(versionPath) ? File.ReadAllText(versionPath).Trim() : "version not recorded";
        var markdown = new StringBuilder();
        markdown.AppendLine("# Vanilla item IDs for Kenshi Origins\n");
        markdown.AppendLine($"Extracted from **{Cell(version)}**: **{rows.Length} inventory item records**. Names are the English database names. Use Ctrl+F to find an item, then copy its full ID into `record`. [CSV version](VANILLA_ITEMS.csv) · [Origin field guide](ORIGIN_GUIDE.md).\n");
        markdown.AppendLine("Only `gamedata.base`, `Newwworld.mod`, `Dialogue.mod` and `rebirth.mod` were loaded, in that order. These are vanilla core files despite the `.mod` suffixes. No enabled mods, Workshop files or translations were loaded. Removed records are excluded.\n");
        markdown.AppendLine("```json\n\"items\": [\n  { \"record\": \"42334-changes_otto.mod\", \"quantity\": 2, \"quality\": 0 }\n]\n```\n");
        markdown.AppendLine("This example gives two Dried Meat. Insert the `items` field inside an origin object. Starting items go into inventory only for the initial Game Start roster after creation is confirmed.\n");
        markdown.AppendLine("Different records can share a name: copy the ID from the intended row. This reference includes vanilla templates and unused records as well as ordinary obtainable items; a listing does not mean an item appears in normal gameplay. Blueprint templates do not select a particular research recipe through the current origin JSON. Inventory dimensions still determine whether a grant fits; oversized items can remain queued.\n");
        markdown.AppendLine("## Categories\n");
        foreach (var group in rows.GroupBy(row => row.Category))
            markdown.AppendLine($"- [{group.Key}](#{group.Key.ToLowerInvariant().Replace(' ', '-')}) ({group.Count()})");
        foreach (var group in rows.GroupBy(row => row.Category))
        {
            markdown.AppendLine($"\n## {group.Key}\n");
            markdown.AppendLine("| Item name | Record ID | Notes |\n| --- | --- | --- |");
            foreach (var row in group)
                markdown.AppendLine($"| {Cell(row.Name)} | `{Cell(row.Record)}` | {Cell(row.Notes)} |");
        }
        markdown.AppendLine("\n## Source file fingerprints\n");
        markdown.AppendLine("Generated from the local vanilla database with Meitou's FCS reader and checked against its item factory. These SHA256 hashes identify the exact source files. Regenerate after a vanilla data update; mods can change item names and behavior without changing their IDs.\n");
        foreach (var (name, hash) in fingerprints) markdown.AppendLine($"- `{name}`: `{hash}`");
        File.WriteAllText(Path.Combine(repo, "VANILLA_ITEMS.md"), markdown.ToString(), new UTF8Encoding(false));
        var csv = new StringBuilder("Category,Name,Record,Notes\r\n");
        foreach (var row in rows)
            csv.AppendJoin(',', new[] { row.Category, row.Name, row.Record, row.Notes }.Select(Quote)).Append("\r\n");
        File.WriteAllText(Path.Combine(repo, "VANILLA_ITEMS.csv"), csv.ToString(), new UTF8Encoding(false));
        Console.WriteLine($"Exported and validated {rows.Length} unique vanilla inventory item IDs from {fingerprints.Count} core files.");
        foreach (var group in rows.GroupBy(row => row.Category)) Console.WriteLine($"  {group.Key}: {group.Count()}");

        string Notes(GameRecord record)
        {
            if (record.Type != FcsRecordType.Crossbow) return "";
            return string.Join("; ", record.GetReferences("ammo").Select(reference =>
            {
                if (database.Find(reference.TargetStringId) is not { Type: FcsRecordType.Item } ammo)
                    throw new InvalidDataException($"Missing vanilla ammo for '{record.StringId}'.");
                return $"Ammo: {ammo.Name} ({ammo.StringId})";
            }));
        }
    }

    private static string? Category(FcsRecordType type) => type switch
    {
        FcsRecordType.Item => "Supplies and other items",
        FcsRecordType.Weapon => "Melee weapons",
        FcsRecordType.Crossbow => "Crossbows",
        FcsRecordType.Armour => "Armour and clothing",
        FcsRecordType.Container => "Backpacks",
        FcsRecordType.Blueprint => "Blueprint templates",
        FcsRecordType.MapItem => "Maps",
        FcsRecordType.LimbReplacement => "Prosthetic limbs",
        _ => null
    };

    private static string Cell(string text) => text.Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");
    private static string Quote(string text) => "\"" + text.Replace("\"", "\"\"") + "\"";
    private sealed record Row(string Category, string Name, string Record, string Notes);
}
