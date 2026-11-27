using System.Numerics;
using System.Text.Json;
using HarmonyLib;
using KenshiOrigins;
using Meitou.Data;
using Meitou.Data.Characters;
using Meitou.Data.Fcs;
using Meitou.Data.Gameplay;
using Meitou.Data.Gameplay.Bodies;
using Meitou.Data.Gameplay.Combat;
using Meitou.Data.Save;
using Meitou.Modding;
using Meitou.Simulation;
using Meitou.Simulation.Bodies;
using Meitou.Simulation.Combat;
using Meitou.Simulation.Factions;
using Meitou.Simulation.Items;
using Meitou.Simulation.Population;
using Meitou.Simulation.Saving;

/// <summary>Real save-file roundtrips and imports in scratch worlds; never opens the player's saves.</summary>
internal static class OriginPersistenceChecks
{
    internal static void Run(string repo, string install, Action<bool, string> check)
    {
        var db = new GameDatabase();
        foreach (string name in LoadOrder.BaseFiles) db.Apply(FcsReader.ReadFile(Path.Combine(install, "data", name)), name);
        var data = PopulationData.Create(db, []);
        var catalog = new OriginCatalog(OriginCatalog.ReadFile(Path.Combine(repo, OriginCatalog.FileName)));
        var factory = new ItemFactory(db, GameConstants.FromDatabase(db));
        string[] expected = ["kenshiorigins.forgottenveteran", "kenshiorigins.nobody", "kenshiorigins.unfairone", "kenshiorigins.wanderer"];
        var logs = new List<string>();
        OriginSystem System() => new(catalog, RngPurpose.Test, logs.Add, factory);
        World World(OriginSystem origin) => new(new WorldSettings { Seed = 41873, PublishSnapshots = false },
            new OpenGroundWalkability((_, _) => 0), [origin]);
        var system = System();
        var world = World(system);
        LoadedSave? loaded = null;
        string scratch = Path.Combine(Path.GetTempPath(), "ko-origin-persistence-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(scratch);
        var observations = new List<object>();
        try
        {
            world.Player.Faction = data.PlayerFaction;
            var race = db.OfType(FcsRecordType.Race).First(r => r.Name == "Greenlander");
            var template = db.OfType(FcsRecordType.Character).First(r => r.GetReferences("race").Any(reference => reference.TargetStringId == race.StringId));
            var squad = new Squad { Id = world.Squads.NextId(), TemplateId = db.OfType(FcsRecordType.SquadTemplate).First().StringId,
                Faction = data.PlayerFaction, HomeCentre = Vector2.Zero, HomeRadius = 10 };
            world.Squads.Add(squad);
            world.Player.Squad = squad.Id;
            for (int i = 0; i < expected.Length; i++)
            {
                var appearance = new CharacterAppearance { Race = race };
                typeof(CharacterAppearance).GetProperty("Loadout")!.SetValue(appearance, new Loadout
                {
                    Seed = 1, Race = race, Female = false, Appearance = new AppearanceFile { Record = new FcsRecord() }
                });
                var cold = new CharacterCold { Name = "Same name", RecordId = template.StringId, Faction = data.PlayerFaction,
                    SquadId = squad.Id, IsPlayer = true, Appearance = appearance, Stats = new CharacterStats(), Inventory = new Inventory() };
                cold.Stats[StatsEnumerated.Toughness] = 90;
                cold.Stats[StatsEnumerated.Strength] = 80 + i;
                var id = world.Characters.Spawn(new CharacterHot { Health = 100 }, cold, 0);
                squad.Members.Add(id);
            }
            squad.Leader = squad.Members[0];
            // Fill the fourth character's main grid with non-stacking vanilla items so its grant stays queued.
            int filler = 0;
            while (true)
            {
                var item = factory.Create("42334-changes_otto.mod", 5)!;
                item.ModTag = "ko-test-filler-" + filler++;
                if (!Cold(squad.Members[3]).Inventory!.Add(item)) break;
            }
            world.Player.InCreation = true;
            system.Ready(world, false);
            for (int i = 0; i < expected.Length; i++)
                Cold(squad.Members[i]).Appearance!.Loadout!.Appearance.Record.Ints[ModLooks.KeyOf(OriginsMod.Id, OriginSystem.ChoiceKey)] = catalog.IndexOf(expected[i]);
            world.Player.InCreation = false;
            system.Reconcile(world);
            for (int i = 0; i < 3; i++) Cold(squad.Members[i]).Inventory!.Items.Clear(); // Consumed/sold starting equipment.
            check(system.Views[squad.Members[3]].ItemsWaiting == 2, "persistence fixture starts with exactly two undelivered starter items");

            var veteran = Cold(squad.Members[0]).Stats!;
            var constants = GameConstants.FromDatabase(db);
            var combat = CombatConstants.FromDatabase(db);
            var hundred = DamageFormulas.Unarmed(100, 100, 100, combat);
            var excess = DamageFormulas.Unarmed(100, veteran.Toughness, 100, combat);
            var capFinding = new
            {
                TrainedToughness = veteran.Stored(StatsEnumerated.Toughness), ConfiguredOriginBonus = 25,
                AppliedOriginBonus = veteran.Modifiers.All.Single(m => m.Key == OriginSystem.ModifierKey && m.Stat == StatsEnumerated.Toughness).Amount,
                EffectiveToughness = veteran.Toughness,
                ComaThresholdAt100 = MedicalState.KoPoint(constants, 100),
                ComaThresholdWithOrigin = MedicalState.KoPoint(constants, veteran.Toughness),
                UnarmedDamageAt100 = hundred.Cut + hundred.Blunt, UnarmedDamageWithOrigin = excess.Cut + excess.Blunt,
                DegenerationTermAt100 = MedicalState.DegenerationMult(constants, 100),
                DegenerationTermWithOrigin = MedicalState.DegenerationMult(constants, veteran.Toughness),
                XpCurveCap = CharacterStats.XpCap
            };
            check(veteran.Toughness == 100 && capFinding.ComaThresholdWithOrigin == capFinding.ComaThresholdAt100
                && capFinding.UnarmedDamageWithOrigin == capFinding.UnarmedDamageAt100,
                "the Veteran's capped Toughness uses the real coma and damage formulas at 100");
            Console.WriteLine(JsonSerializer.Serialize(capFinding));
            // Unbound stats still use the engine's native curve; the mod adjusts XP only in its world.
            var plain = new CharacterStats();
            plain[StatsEnumerated.Toughness] = 90;
            var enhanced = new CharacterStats();
            enhanced[StatsEnumerated.Toughness] = 90;
            enhanced.Modifiers.Set(OriginSystem.ModifierKey, StatsEnumerated.Toughness, 25);
            check(plain.Gain(StatsEnumerated.Toughness, 1) == enhanced.Gain(StatsEnumerated.Toughness, 1),
                "unbound origin modifiers retain native engine training without baking bonuses into stored values");

            for (int cycle = 1; cycle <= 3; cycle++) Roundtrip(cycle, import: false);
            for (int cycle = 1; cycle <= 3; cycle++) Roundtrip(cycle, import: true);
            var queueOwner = Members()[3];
            int before = Cold(queueOwner).Inventory!.Items.Sum(i => i.Quantity);
            int removed = Cold(queueOwner).Inventory!.Items[0].Quantity;
            Cold(queueOwner).Inventory!.Items.RemoveAt(0);
            system.Reconcile(world);
            system.Reconcile(world);
            check(system.Views[queueOwner].ItemsWaiting == 1
                && Cold(queueOwner).Inventory!.Items.Sum(i => i.Quantity) == before - removed + 1,
                "freeing one inventory space delivers only one unit and preserves the exact remainder");
            Roundtrip(4, import: true, waiting: 1);
            queueOwner = Members()[3];
            before = Cold(queueOwner).Inventory!.Items.Sum(i => i.Quantity);
            removed = Cold(queueOwner).Inventory!.Items[0].Quantity;
            Cold(queueOwner).Inventory!.Items.RemoveAt(0);
            system.Reconcile(world);
            system.Reconcile(world);
            check(system.Views[queueOwner].ItemsWaiting == 0
                && Cold(queueOwner).Inventory!.Items.Sum(i => i.Quantity) == before - removed + 1,
                "after repeated imports the exact outstanding quantity arrives once when space opens");
            Directory.CreateDirectory(Path.Combine(repo, "artifacts"));
            File.WriteAllText(Path.Combine(repo, "artifacts/origin-024-audit.json"), JsonSerializer.Serialize(new
            {
                Runtime = typeof(World).Assembly.GetName().Version?.ToString(), CapFinding = capFinding, Cycles = observations,
                ProductionCapBehaviorChanged = true
            }, new JsonSerializerOptions { WriteIndented = true }));

            void Roundtrip(int cycle, bool import, int waiting = 2)
            {
                var save = SaveCapture.Capture(world, data, new SaveClock(2, 10, 0), loaded);
                system.Reconcile(world); // An unchanged paused refresh must not rewrite fields held for a save.
                check(save.Platoons.SelectMany(p => p.Characters).Where(c => c.Name == "Same name")
                    .All(c => c.Stats!.Floats.GetValueOrDefault("toughness2") == 90),
                    $"{(import ? "import" : "load")} {cycle}: the real save stores trained stats without origin modifiers");
                using var payload = new MemoryStream();
                system.Write(payload);
                // The host's actual sidecar envelope, around OriginSystem's real payload.
                var envelope = typeof(IMod).Assembly.GetType("Meitou.Modding.ModSaveFile", true)!;
                save.ModData[OriginsMod.Id] = (byte[])AccessTools.Method(envelope, "Write").Invoke(null,
                    [new List<(string Name, byte[] Data)> { ("origins", payload.ToArray()) }])!;
                if (!import && cycle == 1)
                {
                    foreach (var saved in save.Platoons.SelectMany(p => p.Characters))
                    {
                        saved.Stats?.Strings.Remove(OriginSystem.SavedOriginKey);
                        saved.Stats?.Strings.Remove(OriginSystem.SavedItemsKey);
                    }
                }
                string folder = Path.Combine(scratch, (import ? "import" : "load") + cycle);
                save.Write(folder);
                var read = SaveGame.Load(folder);
                if (import)
                {
                    read = SaveImport.Filter(read, ImportOptions.Default, data);
                    check(read.ModData.Count == 0 && read.LastingIds is null,
                        $"import {cycle}: the real engine resets mod sidecars and lasting IDs");
                }
                var nextSystem = System();
                var nextWorld = World(nextSystem);
                var nextLoaded = SaveLoader.Load(nextWorld, read, data);
                if (read.ModData.TryGetValue(OriginsMod.Id, out var bytes))
                {
                    var entries = (Dictionary<string, byte[]>)AccessTools.Method(envelope, "Read").Invoke(null, [bytes])!;
                    nextSystem.Read(new MemoryStream(entries["origins"]));
                }
                world.Dispose();
                world = nextWorld;
                loaded = nextLoaded;
                system = nextSystem;
                system.Ready(world, true); // Imports count as saves in IWorldReady.
                for (int refresh = 0; refresh < 5; refresh++) system.Reconcile(world);
                var members = Members();
                var actual = members.Select(id => catalog.All.Single(o => o.Name == system.Views[id].Name)).ToArray();
                Console.WriteLine($"{(import ? "IMPORT" : "LOAD")} {cycle}: {string.Join(", ", actual.Select(o => o.Id))}");
                check(actual.Select(o => o.Id).SequenceEqual(expected),
                    $"{(import ? "import" : "load")} {cycle}: all four origins survive, including Nobody and The Unfair One");
                check(members.Select((id, i) => world.SaveFields.String(id, CharacterSaveRecord.Stats, OriginSystem.SavedOriginKey) == expected[i]).All(match => match),
                    $"{(import ? "import" : "load")} {cycle}: character fields are backfilled without depending on names or old lasting IDs");
                check(members.Select((id, i) => (id, i)).All(pair =>
                {
                    var stats = Cold(pair.id).Stats!;
                    return stats.Stored(StatsEnumerated.Toughness) == 90
                        && stats.Toughness == MathF.Min(100, 90 + actual[pair.i].Bonuses.GetValueOrDefault(StatsEnumerated.Toughness))
                        && stats.Modifiers.All.Count(m => m.Key == OriginSystem.ModifierKey) == actual[pair.i].Bonuses.Count;
                }), $"{(import ? "import" : "load")} {cycle}: origin bonuses remain a single modifier set");
                check(members.Take(3).All(id => Cold(id).Inventory!.Items.Count == 0)
                    && system.Views[members[3]].ItemsWaiting == waiting,
                    $"{(import ? "import" : "load")} {cycle}: consumed equipment stays gone and only the original queue remains");
                observations.Add(new { Import = import, Cycle = cycle, OriginIds = actual.Select(o => o.Id).ToArray(),
                    EffectiveToughness = members.Select(id => Cold(id).Stats!.Toughness).ToArray(), ItemsWaiting = system.Views[members[3]].ItemsWaiting });
            }

            CharacterCold Cold(CharacterId id) => world.Characters.Cold(id.Slot)!;
            CharacterId[] Members() => SquadManagement.PlayerSquads(world).SelectMany(s => s.Members).ToArray();
        }
        finally
        {
            world.Dispose();
            Directory.Delete(scratch, recursive: true);
        }
    }
}
