using KenshiOrigins;
using Meitou.Data.Gameplay.Bodies;

internal static class CoreExpansionChecks
{
    internal static void Run(string repo, OriginCatalog core, Action<bool, string> check)
    {
        var previous = OriginCatalog.ReadFile(Path.Combine(repo, "checks/fixtures/core-v0.3.3.json"));
        check(core.All.Count == 18 && core.All.Select(o => o.Id).Distinct(StringComparer.Ordinal).Count() == 18,
            "the official pack contains exactly eighteen unique origins");
        foreach (var old in previous)
        {
            var current = core.Find(old.Id);
            check(current is not null && current.Name == old.Name && current.Description == old.Description
                && current.Difficulty == old.Difficulty && current.StartingCharactersOnly == old.StartingCharactersOnly
                && current.TestingOnly == old.TestingOnly && current.Items.SequenceEqual(old.Items)
                && current.Bonuses.OrderBy(b => b.Key).SequenceEqual(old.Bonuses.OrderBy(b => b.Key)),
                $"{old.Name}: existing content and flags are unchanged apart from tags");
        }
        var expectedTags = new Dictionary<string, string[]>
        {
            ["Nobody"] = ["Special"], ["Wanderer"] = ["Survival"], ["Farmhand"] = ["Civilian"],
            ["Smith Apprentice"] = ["Civilian", "Technical"], ["Street Urchin"] = ["Civilian", "Survival"],
            ["Soldier"] = ["Combat"], ["Gunner"] = ["Combat"], ["Assassin"] = ["Combat"],
            ["Escaped Slave"] = ["Civilian", "Survival"], ["Fugitive"] = ["Combat", "Survival"],
            ["Miner"] = ["Civilian"], ["Field Medic"] = ["Technical"], ["The Unfair One"] = ["Special"],
            ["Scientist"] = ["Technical"], ["Engineer"] = ["Technical"],
            ["The Forgotten Veteran"] = ["Combat", "Survival"], ["Seasoned Wanderer"] = ["Survival"],
            ["Adventurous Innkeeper"] = ["Civilian"]
        };
        check(core.All.All(o => expectedTags.TryGetValue(o.Name, out var tags) && o.Tags.SequenceEqual(tags)),
            "all official origins use the requested consolidated tag assignments");
        check(OriginBrowser.Tags(core).SequenceEqual(["Civilian", "Combat", "Special", "Survival", "Technical"]),
            "the official menu has only five categories and no All or stale tags in origin data");
        check(core.All.All(o => o.Bonuses.Values.All(bonus => bonus >= 0)), "no official origin introduces negative stat modifiers");
        foreach (var origin in core.All)
            foreach (string tag in origin.Tags)
                check(OriginBrowser.Filter(core, "", tag).Any(o => o.Id == origin.Id),
                    $"{origin.Name}: either of its categories independently includes it");

        string[] newIds = ["scientist", "engineer", "forgottenveteran", "seasonedwanderer", "adventurousinnkeeper"];
        foreach (string suffix in newIds)
        {
            int index = core.IndexOf("kenshiorigins." + suffix);
            check(index >= 0 && !core.All[index].StartingCharactersOnly && !core.All[index].TestingOnly
                && core.StartingRandomChoices.Contains(index) && core.RecruitChoices.Contains(index),
                $"{suffix}: available for manual creation, random starts and normal recruitment");
        }
        var veteran = core.Find("kenshiorigins.forgottenveteran")!;
        check(veteran.Bonuses[StatsEnumerated.Toughness] == 25 && veteran.Bonuses[StatsEnumerated.MeleeDefence] == 15
            && veteran.Bonuses[StatsEnumerated.MeleeAttack] == 15 && veteran.Bonuses[StatsEnumerated.Strength] == 10
            && veteran.Bonuses[StatsEnumerated.Dexterity] == 5 && veteran.Bonuses[StatsEnumerated.Medic] == 5
            && veteran.Bonuses.Count == 6 && veteran.Items.Single(i => i.Record == "901-gamedata.base").Quality == 0,
            "The Forgotten Veteran keeps the requested combat bonuses and low-quality Longsword");

        var external = OriginCatalog.ReadFile(Path.Combine(repo, "examples/ExtraOrigins/originform.json"));
        var custom = new OriginJson { Id = "thirdparty.custom", Name = "Custom specialist", Description = "External origin",
            Bonuses = [], Tags = ["A custom category", "Technical"] }.Validate();
        var combined = new OriginCatalog([.. core.All, .. external, custom]);
        check(OriginBrowser.Tags(combined).Contains("Labouring") && OriginBrowser.Tags(combined).Contains("A custom category")
            && OriginBrowser.Filter(core, "", "Labouring").Count == 0
            && OriginBrowser.Filter(combined, "", "LABOURING").Single().Id == "extraorigins.dockworker",
            "old and arbitrary tags appear only when a loaded third-party definition explicitly contributes them");
        check(OriginBrowser.Filter(combined, "EXTERNAL", "a CUSTOM category").Single().Id == custom.Id
            && OriginBrowser.Filter(combined, "specialist", "Technical").Single().Id == custom.Id,
            "third-party multi-tag origins match either category and combine with case-insensitive search");
        var originalOrder = combined.All.Select(o => o.Id).ToArray();
        var filtered = OriginBrowser.Filter(combined, "", "Technical");
        check(filtered.Select(o => o.Name).SequenceEqual(filtered.Select(o => o.Name).Order(StringComparer.OrdinalIgnoreCase))
            && combined.All.Select(o => o.Id).SequenceEqual(originalOrder),
            "official and third-party filtering preserves alphabetical display and original catalog indices");
    }
}
