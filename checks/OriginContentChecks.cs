using System.Numerics;
using KenshiOrigins;
using Meitou.Data;
using Meitou.Data.Characters;
using Meitou.Data.Fcs;
using Meitou.Data.Gameplay;
using Meitou.Data.Gameplay.Bodies;
using Meitou.Simulation;
using Meitou.Simulation.Bodies;
using Meitou.Simulation.Factions;
using Meitou.Simulation.Items;
using Meitou.Simulation.Population;

/// <summary>Checks authored content against the installed enum, vanilla items and real assignment system.</summary>
internal static class OriginContentChecks
{
    internal static void Run(OriginCatalog catalog, string install, Action<bool, string> check, OriginCatalog? previous = null)
    {
        var database = new GameDatabase();
        foreach (string name in LoadOrder.BaseFiles)
            database.Apply(FcsReader.ReadFile(Path.Combine(install, "data", name)), name);
        var factory = new ItemFactory(database, GameConstants.Default);
        var race = database.OfType(FcsRecordType.Race).First();
        check(catalog.StartingRandomChoices.All(i => !catalog.All[i].TestingOnly)
            && catalog.RecruitChoices.All(i => !catalog.All[i].TestingOnly && !catalog.All[i].StartingCharactersOnly),
            "random pools exclude testing origins and respect starter-only eligibility");
        if (catalog.Find("kenshiorigins.unfairone") is { } unfair)
        {
            var otherSkills = new HashSet<StatsEnumerated>
            {
                StatsEnumerated.Labouring, StatsEnumerated.Science, StatsEnumerated.Engineering, StatsEnumerated.Robotics,
                StatsEnumerated.SmithingWeapon, StatsEnumerated.SmithingArmour, StatsEnumerated.Medic, StatsEnumerated.Thieving,
                StatsEnumerated.Farming, StatsEnumerated.Cooking, StatsEnumerated.Stealth, StatsEnumerated.Lockpicking,
                StatsEnumerated.SmithingBow
            };
            var supported = Enum.GetValues<StatsEnumerated>().Where(s => s != StatsEnumerated.None && StatsInfo.StorageOf(s) == s).ToArray();
            check(unfair.Bonuses.Count == supported.Length
                && supported.All(stat => unfair.Bonuses.GetValueOrDefault(stat) == (otherSkills.Contains(stat) ? 50 : 75)),
                "The Unfair One grants +75 physical/combat and +50 other skills across every supported unique stat");
            check(unfair.StartingCharactersOnly && unfair.TestingOnly, "The Unfair One is explicitly reserved for manual testing selection");
        }
        foreach (var definition in catalog.All)
        {
            var logs = new List<string>();
            var single = new OriginCatalog([definition]);
            var system = new OriginSystem(single, RngPurpose.Test, logs.Add, factory);
            using var world = MakeWorld(system);
            var starter = Spawn(world, true, race);
            var recruit = Spawn(world, false, race);
            world.Player.InCreation = true;
            system.Ready(world, false);
            check(Cold(world, starter).Stats!.Modifiers.All.Count == 0 && Cold(world, starter).Inventory!.Items.Count == 0,
                $"{definition.Name}: bonuses and items wait for creation confirmation");
            // Explicit selection is required even with only one definition if it is testing-only.
            Cold(world, starter).Appearance!.Loadout!.Appearance.Record.Ints[ModLooks.KeyOf(OriginsMod.Id, OriginSystem.ChoiceKey)] = 0;
            world.Player.InCreation = false;
            system.Reconcile(world);
            system.Reconcile(world);
            var cold = Cold(world, starter);
            check(system.Views[starter].Description == definition.Description && system.Views[starter].TestingOnly == definition.TestingOnly
                && definition.Bonuses.All(b => cold.Stats![b.Key] - cold.Stats.Stored(b.Key) == b.Value),
                $"{definition.Name}: manual selection applies the authored bonuses and full story without stacking");
            check(system.Views[starter].ItemsWaiting == 0 && cold.Inventory!.Items.All(item => item.Section == "main")
                && cold.Inventory.Items.Sum(item => item.Quantity) == definition.Items.Sum(item => item.Quantity)
                && definition.Items.All(grant => database.Find(grant.Record) is not null
                    && cold.Inventory.Items.Where(item => item.Record == grant.Record).Sum(item => item.Quantity)
                        == definition.Items.Where(item => item.Record == grant.Record).Sum(item => item.Quantity))
                && !logs.Any(line => line.Contains("skipped", StringComparison.OrdinalIgnoreCase)),
                $"{definition.Name}: all vanilla equipment arrives once in inventory, including intentionally empty loadouts");
            Cold(world, recruit).IsPlayer = true;
            system.Reconcile(world);
            bool restricted = definition.TestingOnly || definition.StartingCharactersOnly;
            check(system.Views.ContainsKey(recruit) == !restricted && Cold(world, recruit).Inventory!.Items.Count == 0,
                $"{definition.Name}: recruitment respects eligibility and never grants starter equipment");

            using var saved = new MemoryStream();
            system.Write(saved);
            saved.Position = 0;
            var restored = new OriginSystem(single, RngPurpose.Test, logs.Add, factory);
            restored.Read(saved);
            using var loaded = MakeWorld(restored);
            Spawn(loaded, false, race);
            var same = Spawn(loaded, true, race);
            typeof(CharacterCold).GetProperty("LastingId")!.SetValue(Cold(loaded, same), cold.LastingId);
            Cold(loaded, same).Inventory = cold.Inventory!.Copy();
            restored.Ready(loaded, true);
            check(restored.Views[same].Name == definition.Name
                && definition.Bonuses.All(b => Cold(loaded, same).Stats!.Modifiers[b.Key] == b.Value)
                && Cold(loaded, same).Inventory!.Items.Sum(item => item.Quantity) == definition.Items.Sum(item => item.Quantity),
                $"{definition.Name}: save/load restores its origin and bonuses without duplicating equipment");
        }
        Console.WriteLine($"Validated {catalog.All.Count} origins against the installed runtime and vanilla database.");
        if (previous is not null)
        {
            var oldSystem = new OriginSystem(previous, RngPurpose.Test, _ => { }, factory);
            using var oldWorld = MakeWorld(oldSystem);
            var oldCharacters = previous.All.Select(_ => Spawn(oldWorld, true, race)).ToArray();
            oldWorld.Player.InCreation = true;
            oldSystem.Ready(oldWorld, false);
            for (int i = 0; i < oldCharacters.Length; i++)
                Cold(oldWorld, oldCharacters[i]).Appearance!.Loadout!.Appearance.Record.Ints[ModLooks.KeyOf(OriginsMod.Id, OriginSystem.ChoiceKey)] = i;
            oldWorld.Player.InCreation = false;
            oldSystem.Reconcile(oldWorld);
            // Already consumed/sold grants must not reappear when the expanded catalog is loaded.
            foreach (var id in oldCharacters) Cold(oldWorld, id).Inventory!.Items.Clear();
            using var save = new MemoryStream();
            oldSystem.Write(save);
            save.Position = 0;
            var upgraded = new OriginSystem(catalog, RngPurpose.Test, _ => { }, factory);
            upgraded.Read(save);
            using var upgradedWorld = MakeWorld(upgraded);
            Spawn(upgradedWorld, false, race);
            var upgradedCharacters = oldCharacters.Select(id =>
            {
                var same = Spawn(upgradedWorld, true, race);
                typeof(CharacterCold).GetProperty("LastingId")!.SetValue(Cold(upgradedWorld, same), Cold(oldWorld, id).LastingId);
                return same;
            }).ToArray();
            upgraded.Ready(upgradedWorld, true);
            upgraded.Reconcile(upgradedWorld);
            for (int i = 0; i < upgradedCharacters.Length; i++)
            {
                var id = upgradedCharacters[i];
                var old = previous.All[i];
                var cold = Cold(upgradedWorld, id);
                check(upgraded.Views[id].Name == old.Name && upgraded.Views[id].Description == old.Description
                    && cold.Stats!.Modifiers.All.Count == old.Bonuses.Count
                    && old.Bonuses.All(b => cold.Stats.Modifiers[b.Key] == b.Value)
                    && cold.Inventory!.Items.Count == 0 && upgraded.Views[id].ItemsWaiting == 0,
                    $"{old.Name}: pre-expansion save keeps its assigned origin without stacked bonuses or retroactive equipment");
            }
        }
    }

    private static World MakeWorld(OriginSystem system)
    {
        var world = new World(new WorldSettings { Seed = 7654321, PublishSnapshots = false }, new OpenGroundWalkability((_, _) => 0), [system]);
        world.Player.Faction = 0;
        return world;
    }

    private static CharacterId Spawn(World world, bool player, GameRecord race)
    {
        var squad = new Squad { Id = world.Squads.NextId(), TemplateId = "content-check", Faction = player ? 0 : 1,
            HomeCentre = Vector2.Zero, HomeRadius = 10 };
        world.Squads.Add(squad);
        var appearance = new CharacterAppearance { Race = race };
        typeof(CharacterAppearance).GetProperty("Loadout")!.SetValue(appearance, new Loadout
        {
            Seed = 1, Race = race, Female = false, Appearance = new AppearanceFile { Record = new FcsRecord() }
        });
        var id = world.Characters.Spawn(new CharacterHot { Health = 100 }, new CharacterCold
        {
            Name = "Content check", IsPlayer = player, Faction = squad.Faction, SquadId = squad.Id,
            Appearance = appearance, Stats = new CharacterStats(), Inventory = new Inventory()
        }, 0);
        squad.Members.Add(id);
        squad.Leader = id;
        return id;
    }

    private static CharacterCold Cold(World world, CharacterId id) => world.Characters.Cold(id.Slot)!;
}
