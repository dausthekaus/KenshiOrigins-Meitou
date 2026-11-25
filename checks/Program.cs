using System.Numerics;
using System.Reflection;
using System.Text.Json;
using HarmonyLib;
using KenshiOrigins;
using Meitou.Content;
using Meitou.Data;
using Meitou.Data.Characters;
using Meitou.Data.Fcs;
using Meitou.Data.Gameplay.Bodies;
using Meitou.Data.Gameplay;
using Meitou.Data.Ui;
using Meitou.Game.Host;
using Meitou.Game.Ui;
using Meitou.Modding;
using Meitou.Simulation;
using Meitou.Simulation.Bodies;
using Meitou.Simulation.Editing;
using Meitou.Simulation.Factions;
using Meitou.Simulation.Items;
using Meitou.Simulation.Player;
using Meitou.Simulation.Population;

internal static class PrototypeChecks
{
    internal static void Run(string[] args)
    {
        string repo = Path.GetFullPath(args[0]);
        int uiArgument = Array.IndexOf(args, "--ui-root");
        string? uiRoot = uiArgument >= 0 ? args[uiArgument + 1] : GameInstall.Locate()?.Root;
        if (args.Contains("--export-vanilla-items"))
        {
            VanillaItemReference.Write(repo, uiRoot ?? throw new ArgumentException("Pass --ui-root with the Kenshi installation folder."));
            return;
        }
        int validateArgument = Array.IndexOf(args, "--validate-origins");
        if (validateArgument >= 0)
        {
            OriginContentChecks.Run(new OriginCatalog(OriginCatalog.ReadFile(args[validateArgument + 1])),
                uiRoot ?? throw new ArgumentException("Pass --ui-root with the Kenshi installation folder."),
                (condition, message) =>
                {
                    if (!condition) throw new InvalidDataException(message);
                    Console.WriteLine("  OK " + message);
                });
            return;
        }
        int themeArgument = Array.IndexOf(args, "--ui-theme");
        string? theme = themeArgument >= 0 ? args[themeArgument + 1] : null;
        string? preview = args.Contains("--render-ui") ? Path.Combine(repo, "artifacts", theme is null ? "origin-ui.png" : "origin-ui-theme.png") : null;
        if (args.Contains("--generate"))
        {
            WriteMod(Path.Combine(repo, "KenshiOrigins.mod"), "Character origins for MeitouClient.");
            WriteMod(Path.Combine(repo, "examples/ExtraOrigins/ExtraOrigins.mod"), "Example origin pack.", "KenshiOrigins.mod");
            Console.WriteLine("Generated the main mod and example pack headers with Meitou's FcsWriter.");
            return;
        }

        int assertions = 0;
        var logs = new List<string>();
        // Fixed content keeps UI interaction and lifecycle regressions independent of authored balance changes.
        string fixtures = Path.Combine(repo, "checks", "fixtures");
        var catalog = new OriginCatalog(OriginCatalog.ReadFile(Path.Combine(fixtures, OriginCatalog.FileName)));
        var bundled = new OriginCatalog(OriginCatalog.ReadFile(Path.Combine(repo, OriginCatalog.FileName)));
        Check(bundled.All.Count > 0, $"{bundled.All.Count} authored origin definitions validate");
        var db = new GameDatabase();
        var records = new FcsFile();
        records.Records.Add(new FcsRecord { StringId = "204-gamedata.base", Name = "Nameless", RecordType = FcsRecordType.Faction });
        records.Records.Add(new FcsRecord { StringId = "race", Name = "Human", RecordType = FcsRecordType.Race });
        records.Records.Add(new FcsRecord { StringId = "race2", Name = "Another race", RecordType = FcsRecordType.Race });
        records.Records.Single(r => r.StringId == "race").Strings["description"] = "The original race description.";
        records.Records.Add(new FcsRecord { StringId = "hero", Name = "Hero", RecordType = FcsRecordType.Character });
        records.Records.Add(new FcsRecord { StringId = "475-gamedata.base", Name = "Horse Chopper", RecordType = FcsRecordType.Weapon });
        records.Records.Add(new FcsRecord { StringId = "1020-gamedata.base", Name = "Wakizashi", RecordType = FcsRecordType.Weapon });
        db.Apply(records, "fixtures.mod");

        string scratch = Path.Combine(Path.GetTempPath(), "kenshiorigins-check-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(scratch);
        string? originalInstall = Environment.GetEnvironmentVariable("KENSHI_PATH");
        try
        {
            CheckDiscovery();
            CheckValidation();
            CoreExpansionChecks.Run(repo, bundled, Check);
            CheckLifecycle();
            CheckStartOnlyOrigins();
            CheckTestingOrigins();
            CheckCombatOrigins();
            EquipmentChecks.Run(bundled, uiRoot, Check);
            if (uiRoot is not null) OriginContentChecks.Run(bundled, uiRoot, Check,
                new OriginCatalog(OriginCatalog.ReadFile(Path.Combine(fixtures, "core-v0.3.3.json"))));
            CheckUi();
            Check(DeterministicHash(1) == DeterministicHash(4), "assignments and hashes agree with one and four worker threads");
            Console.WriteLine($"PASS: {assertions} assertions against {typeof(IMod).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion}.");
        }
        finally
        {
            Environment.SetEnvironmentVariable("KENSHI_PATH", originalInstall);
            Directory.Delete(scratch, recursive: true);
        }

        void CheckDiscovery()
        {
            string install = Path.Combine(scratch, "install");
            Directory.CreateDirectory(Path.Combine(install, "data"));
            WriteMod(Path.Combine(install, "data/gamedata.base"), "Synthetic installation");
            Environment.SetEnvironmentVariable("KENSHI_PATH", install);

            var packDb = new GameDatabase();
            packDb.Files.AddRange(["KenshiOrigins.mod", "Enabled.mod", "Unrelated.mod", "NoJson.mod", "Missing.mod", "Broken.mod"]);
            foreach (string name in new[] { "Enabled", "Disabled", "Unrelated", "NoJson", "Missing", "Broken" })
            {
                string folder = Path.Combine(install, "mods", name);
                Directory.CreateDirectory(folder);
                WriteMod(Path.Combine(folder, name + ".mod"), "Synthetic pack", name == "Unrelated" ? "Other.mod" : "KenshiOrigins.mod");
                if (name == "NoJson") continue;
                string json = File.ReadAllText(Path.Combine(repo, "examples/ExtraOrigins/originform.json"));
                File.WriteAllText(Path.Combine(folder, OriginCatalog.FileName), name == "Broken" ? "{" : json.Replace("extraorigins", name.ToLowerInvariant()));
            }
            packDb.NotLoaded.Add(new LoadOrderEntry("Missing.mod", "unused"));
            packDb.Files.Remove("Missing.mod");
            var found = OriginCatalog.Load(fixtures, "KenshiOrigins", packDb, logs.Add);
            Check(found.Find("enabled.dockworker") is not null, "direct dependent loaded mod contributes origins");
            Check(found.Find("disabled.dockworker") is null, "installed but disabled pack is ignored");
            Check(found.Find("unrelated.dockworker") is null, "mod without our master is ignored");
            Check(found.Find("missing.dockworker") is null, "failed data mod is ignored");
            Check(found.All.Count == catalog.All.Count + 1, "missing JSON and broken optional pack do not prevent loading valid definitions");
            Check(logs.Any(l => l.Contains("Skipped origin pack")), "broken optional JSON has a diagnostic");
            Check(ModHeader.TryRead(Path.Combine(repo, "examples/ExtraOrigins/ExtraOrigins.mod"))!.Dependencies.Contains("KenshiOrigins.mod"),
                "example pack has the real FCS master dependency");
        }

        void CheckValidation()
        {
            string invalid = Path.Combine(scratch, "invalid");
            Directory.CreateDirectory(invalid);
            string path = Path.Combine(invalid, OriginCatalog.FileName);
            string valid = File.ReadAllText(Path.Combine(fixtures, OriginCatalog.FileName));
            foreach (string text in new[]
            {
                valid.Replace("\"Strength\"", "\"Strenght\""),
                valid.Replace("\"formatVersion\": 1", "\"formatVersion\": 99"),
                valid.Replace("kenshiorigins.wanderer", "kenshiorigins.farmhand"),
                valid.Replace("\"Strength\": 3", "\"Strength\": 3, \"UnknownField\": 1"),
                valid.Replace("\"Strength\": 3", "\"None\": 2"),
                valid.Replace("\"Strength\": 3", "\"Medic\": 2, \"Vet\": 1"),
                valid.Replace("\"quantity\": 1", "\"quantity\": 0"),
                valid.Replace("\"quantity\": 1", "\"quantity\": 1001"),
                valid.Replace("\"quality\": 0", "\"quality\": -1"),
                valid.Replace("\"quality\": 0", "\"quality\": 101"),
                valid.Replace("2064-gamedata.base", " "),
                valid.Replace("\"difficulty\": \"Standard\"", "\"difficulty\": 3"),
                valid.Replace("\"startingCharactersOnly\": false", "\"startingCharactersOnly\": \"true\""),
                valid.Replace("\"startingCharactersOnly\": false", "\"startingCharactersOnly\": null"),
                valid.Replace("\"startingCharactersOnly\": false", "\"startingCharactersOnly\": false, \"testingOnly\": \"true\""),
                valid.Replace("[\"Civilian\", \"Farming\"]", "[\"\"]"),
                valid.Replace("[\"Civilian\", \"Farming\"]", "[null]"),
                valid.Replace("[\"Civilian\", \"Farming\"]", "[42]")
            })
            {
                File.WriteAllText(path, text);
                bool rejected = false;
                try { OriginCatalog.ReadFile(path); }
                catch (Exception e) when (e is InvalidDataException or JsonException) { rejected = true; }
                Check(rejected, "invalid origin definition is rejected");
            }
            File.WriteAllText(path, valid);
            var pool = catalog.All.ToList();
            OriginCatalog.AddPack(pool, invalid, logs.Add);
            Check(pool.Count == catalog.All.Count, "duplicate pack IDs cannot overwrite existing definitions");
            File.WriteAllText(path, """
                { "formatVersion": 1, "origins": [
                  { "id": "oldpack.legacy", "name": "Legacy", "description": "Old pack", "bonuses": {} }
                ] }
                """);
            var oldPack = OriginCatalog.ReadFile(path).Single();
            Check(oldPack.Items.Count == 0 && oldPack.Difficulty == "Unrated" && oldPack.Tags.Count == 0
                && !oldPack.StartingCharactersOnly && !oldPack.TestingOnly,
                "existing packs can omit items, difficulty, tags, startingCharactersOnly and testingOnly");
            File.WriteAllText(path, valid.Replace("\"startingCharactersOnly\": false", "\"startingCharactersOnly\": true"));
            Check(OriginCatalog.ReadFile(path).All(origin => origin.StartingCharactersOnly),
                "JSON true survives validation as a start-only origin rule");
            Check(!OriginCatalog.ReadFile(Path.Combine(repo, "examples/ExtraOrigins/originform.json")).Single().StartingCharactersOnly,
                "the distributable example explicitly allows both starters and recruits");
            var tagged = new OriginJson { Id = "test.tagged", Name = "Extra", Description = "External pack", Bonuses = [],
                Tags = [" Custom tag ", "CUSTOM TAG", "Another"] }.Validate();
            Check(tagged.Tags.SequenceEqual(["Custom tag", "Another"]), "tags trim whitespace and collapse case-insensitive duplicates");
            var combined = new OriginCatalog([.. catalog.All, tagged]);
            var ids = combined.All.Select(origin => origin.Id).ToArray();
            Check(OriginBrowser.Tags(combined).Contains("Custom tag")
                && OriginBrowser.Filter(combined, "EXTERNAL", "custom TAG").Single().Id == tagged.Id,
                "external packs contribute arbitrary tags and searchable descriptions to the browser");
            Check(OriginBrowser.Filter(combined, "", "").Select(origin => origin.Name)
                .SequenceEqual(combined.All.Select(origin => origin.Name).Order(StringComparer.OrdinalIgnoreCase))
                && combined.All.Select(origin => origin.Id).SequenceEqual(ids),
                "alphabetical menu presentation never reorders the catalog used by saves and drafts");
        }

        void CheckLifecycle()
        {
            var system = MakeSystem();
            using var world = MakeWorld(system);
            var starter = Spawn(world, "Starter", true);
            var second = Spawn(world, "Second squad", true);
            var npc = Spawn(world, "Recruit", false);
            world.Player.InCreation = true;
            system.Ready(world, false);
            Check(system.Views[starter].Pending && system.Views[second].Pending, "starters in multiple squads await creation confirmation");
            Check(!system.Views.ContainsKey(npc), "unrecruited NPC receives no origin");
            Check(Cold(world, starter).Stats!.Modifiers.All.Count == 0, "preview does not apply bonuses early");

            var look = Cold(world, starter).Appearance!.Loadout!.Appearance.Record;
            Check(look.Ints.ContainsKey(ModLooks.KeyOf(OriginsMod.Id, OriginSystem.ChoiceKey)), "random starting choice reaches the per-character editor look");
            look.Ints[ModLooks.KeyOf(OriginsMod.Id, OriginSystem.ChoiceKey)] = catalog.IndexOf("kenshiorigins.farmhand");
            Cold(world, starter).Stats = new CharacterStats();
            world.Player.InCreation = false;
            system.Reconcile(world);
            Check(system.Views[starter].Name == "Farmhand", "confirmed choice wins over the random default");
            Check(Cold(world, starter).Stats!.Strength == 4, "bonus applies to replacement stats after creation");
            Check(Cold(world, starter).Stats!.Stored(StatsEnumerated.Strength) == 1, "origin bonus does not change trained stats");
            system.Reconcile(world);
            Check(Cold(world, starter).Stats!.Strength == 4, "repeated reconciliation cannot stack the bonus");

            Cold(world, npc).IsPlayer = true;
            system.Reconcile(world);
            Check(system.Views.ContainsKey(npc) && !system.Views[npc].Pending, "existing NPC changing membership receives an origin");
            string recruitedOrigin = system.Views[npc].Name;
            Cold(world, npc).IsPlayer = false;
            system.Reconcile(world);
            Cold(world, npc).IsPlayer = true;
            system.Reconcile(world);
            Check(system.Views[npc].Name == recruitedOrigin, "dismissal and re-recruitment preserve origin");
            var late = Spawn(world, "Direct mod spawn", true);
            system.Reconcile(world);
            Check(system.Views.ContainsKey(late), "direct player spawn receives an origin too");

            byte[] state = Save(system);
            var lasting = Cold(world, starter).LastingId;
            var restored = MakeSystem();
            restored.Read(new MemoryStream(state));
            using var loaded = MakeWorld(restored);
            Spawn(loaded, "Changes slot numbering", false);
            var same = Spawn(loaded, "Renamed starter", true);
            typeof(CharacterCold).GetProperty("LastingId")!.SetValue(Cold(loaded, same), lasting);
            restored.Ready(loaded, true);
            Check(same != starter && restored.Views[same].Name == "Farmhand", "save data follows lasting ID across changed runtime IDs and names");
            Check(Cold(loaded, same).Stats!.Strength == 4, "loading restores the modifier exactly once");
            restored.Reconcile(loaded);
            Check(Cold(loaded, same).Stats!.Strength == 4, "loaded bonuses remain idempotent");

            var withoutPack = new OriginSystem(new OriginCatalog(catalog.All.Where(o => o.Id != "kenshiorigins.farmhand")), RngPurpose.Test, logs.Add);
            withoutPack.Read(new MemoryStream(state));
            using var missing = MakeWorld(withoutPack);
            var missingChar = Spawn(missing, "Missing pack", true);
            typeof(CharacterCold).GetProperty("LastingId")!.SetValue(Cold(missing, missingChar), lasting);
            withoutPack.Ready(missing, true);
            Check(withoutPack.Views[missingChar].Name.Contains("Unavailable origin"), "missing saved definition is visible instead of silently rerolled");
            Check(Cold(missing, missingChar).Stats!.Modifiers.All.Count == 0, "missing pack grants no unavailable bonuses");

            var existing = MakeSystem();
            using var oldSave = MakeWorld(existing);
            var veteran = Spawn(oldSave, "Existing save", true);
            existing.Ready(oldSave, true);
            Check(existing.Views.ContainsKey(veteran), "installing on an existing save assigns a random origin");

            var lucky = MakeSystem();
            using var luckyWorld = MakeWorld(lucky);
            var gambler = Spawn(luckyWorld, "Lucky starter", true);
            luckyWorld.Player.InCreation = true;
            lucky.Ready(luckyWorld, false);
            string roll = lucky.Views[gambler].Name;
            Cold(luckyWorld, gambler).Appearance!.Loadout!.Appearance.Record.Ints[ModLooks.KeyOf(OriginsMod.Id, OriginSystem.ChoiceKey)] = catalog.All.Count;
            luckyWorld.Player.InCreation = false;
            lucky.Reconcile(luckyWorld);
            lucky.Reconcile(luckyWorld);
            Check(lucky.Views[gambler].Name == roll && !lucky.Views[gambler].Pending,
                "Random resolves to one deterministic real origin at confirmation and never rerolls on reconciliation");
            using var luckySave = JsonDocument.Parse(Save(lucky));
            Check(catalog.Find(luckySave.RootElement.GetProperty("Assignments")[0].GetProperty("OriginId").GetString()!) is not null,
                "Random persists the resolved stable origin ID rather than a random placeholder");
        }

        void CheckStartOnlyOrigins()
        {
            var restricted = catalog.All[0] with { StartingCharactersOnly = true };
            var ordinary = catalog.All[1] with { StartingCharactersOnly = false };
            var pool = new OriginCatalog([restricted, ordinary]);
            var system = new OriginSystem(pool, RngPurpose.Test, logs.Add);
            using var world = MakeWorld(system);
            var starter = Spawn(world, "Exclusive starter", true);
            var recruit = Spawn(world, "Future recruit", false);
            world.Player.InCreation = true;
            system.Ready(world, false);
            Cold(world, starter).Appearance!.Loadout!.Appearance.Record.Ints[ModLooks.KeyOf(OriginsMod.Id, OriginSystem.ChoiceKey)] = 0;
            var duringCreation = Spawn(world, "Late creation spawn", true);
            system.Reconcile(world);
            world.Player.InCreation = false;
            system.Reconcile(world);
            Check(system.Views[starter].Name == restricted.Name && Cold(world, starter).Stats!.Strength == 4,
                "manually selected start-only origin applies to an initial-roster character");
            Cold(world, recruit).IsPlayer = true;
            var later = Enumerable.Range(0, 20).Select(i => Spawn(world, "Later spawn " + i, true)).ToArray();
            system.Reconcile(world);
            Check(later.Append(recruit).Append(duringCreation).All(id => system.Views[id].Name == ordinary.Name
                && Cold(world, id).Stats!.Athletics == 6),
                "recruits and player spawns outside the initial roster roll only eligible origins and receive their bonuses");

            // A one-entry pool guarantees that creation Random can still resolve a restricted origin.
            var exclusivePool = new OriginCatalog([restricted]);
            var exclusive = new OriginSystem(exclusivePool, RngPurpose.Test, logs.Add);
            using var exclusiveWorld = MakeWorld(exclusive);
            var lucky = Spawn(exclusiveWorld, "Random starter", true);
            exclusiveWorld.Player.InCreation = true;
            exclusive.Ready(exclusiveWorld, false);
            Check(exclusive.Views[lucky].Name == restricted.Name && exclusive.Views[lucky].Pending,
                "random creation defaults include start-only origins");
            Cold(exclusiveWorld, lucky).Appearance!.Loadout!.Appearance.Record.Ints[ModLooks.KeyOf(OriginsMod.Id, OriginSystem.ChoiceKey)] = exclusivePool.All.Count;
            exclusiveWorld.Player.InCreation = false;
            var excluded = Spawn(exclusiveWorld, "No eligible origin", true);
            exclusive.Reconcile(exclusiveWorld);
            exclusive.Reconcile(exclusiveWorld);
            Check(exclusive.Views[lucky].Name == restricted.Name && !exclusive.Views[lucky].Pending,
                "creation Random resolves from the full pool including start-only origins");
            Check(!exclusive.Views.ContainsKey(excluded) && Cold(exclusiveWorld, excluded).Stats!.Modifiers.All.Count == 0
                && logs.Any(line => line.Contains("All loaded origins are startingCharactersOnly")),
                "an empty recruit pool skips assignment safely with a diagnostic");

            // Changing eligibility governs future rolls, not IDs already recorded in saves.
            var restored = new OriginSystem(new OriginCatalog([restricted, ordinary with { StartingCharactersOnly = true }]),
                RngPurpose.Test, logs.Add);
            restored.Read(new MemoryStream(Save(system)));
            using var loaded = MakeWorld(restored);
            var veteran = Spawn(loaded, "Saved recruit", true);
            typeof(CharacterCold).GetProperty("LastingId")!.SetValue(Cold(loaded, veteran), Cold(world, recruit).LastingId);
            var existing = Spawn(loaded, "Existing unassigned character", true);
            restored.Ready(loaded, true);
            Check(restored.Views[veteran].Name == ordinary.Name && Cold(loaded, veteran).Stats!.Athletics == 6,
                "saved recruit assignments survive a later change to start-only eligibility");
            Check(!restored.Views.ContainsKey(existing), "installing on an existing save respects the recruit eligibility rule");

            using var unrestrictedWorld = MakeWorld(new OriginSystem(new OriginCatalog([ordinary]), RngPurpose.Test, logs.Add));
            using var restrictedWorld = MakeWorld(new OriginSystem(new OriginCatalog([ordinary with { StartingCharactersOnly = true }]),
                RngPurpose.Test, logs.Add));
            Check(unrestrictedWorld.StateHash() != restrictedWorld.StateHash(),
                "eligibility content participates in deterministic state hashes");
        }

        void CheckTestingOrigins()
        {
            var normal = catalog.All[0];
            var testing = catalog.All[1] with { TestingOnly = true, StartingCharactersOnly = false };
            var pool = new OriginCatalog([testing, normal]);
            var system = new OriginSystem(pool, RngPurpose.Test, logs.Add);
            using var world = MakeWorld(system);
            var starters = Enumerable.Range(0, 20).Select(i => Spawn(world, "Random default " + i, true)).ToArray();
            world.Player.InCreation = true;
            system.Ready(world, false);
            Check(starters.All(id => system.Views[id].Name == normal.Name),
                "testing origins never appear as random creation defaults");
            foreach (var id in starters)
                Cold(world, id).Appearance!.Loadout!.Appearance.Record.Ints[ModLooks.KeyOf(OriginsMod.Id, OriginSystem.ChoiceKey)] = pool.All.Count;
            world.Player.InCreation = false;
            system.Reconcile(world);
            var recruits = Enumerable.Range(0, 20).Select(i => Spawn(world, "Random recruit " + i, true)).ToArray();
            system.Reconcile(world);
            Check(starters.Concat(recruits).All(id => system.Views[id].Name == normal.Name),
                "creation Random and recruits exclude testing origins even without startingCharactersOnly");

            var onlyTesting = new OriginSystem(new OriginCatalog([testing]), RngPurpose.Test, logs.Add);
            using var testWorld = MakeWorld(onlyTesting);
            var manual = Spawn(testWorld, "Manual choice", true);
            testWorld.Player.InCreation = true;
            onlyTesting.Ready(testWorld, false);
            Check(onlyTesting.Views[manual].Name == "Random" && onlyTesting.Views[manual].Pending,
                "a testing-only catalog safely leaves the default unassigned");
            testWorld.Player.InCreation = false;
            onlyTesting.Reconcile(testWorld);
            Check(!onlyTesting.Views.ContainsKey(manual), "an empty normal pool cannot automatically assign a testing origin");
            Cold(testWorld, manual).Appearance!.Loadout!.Appearance.Record.Ints[ModLooks.KeyOf(OriginsMod.Id, OriginSystem.ChoiceKey)] = 0;
            onlyTesting.Reconcile(testWorld);
            Check(onlyTesting.Views[manual].Name == testing.Name && onlyTesting.Views[manual].TestingOnly,
                "explicit manual selection still assigns a testing origin and publishes its warning flag");
            byte[] saved = Save(onlyTesting);
            var restored = new OriginSystem(new OriginCatalog([testing]), RngPurpose.Test, logs.Add);
            restored.Read(new MemoryStream(saved));
            using var loaded = MakeWorld(restored);
            var same = Spawn(loaded, "Saved tester", true);
            typeof(CharacterCold).GetProperty("LastingId")!.SetValue(Cold(loaded, same), Cold(testWorld, manual).LastingId);
            restored.Ready(loaded, true);
            Check(restored.Views[same].TestingOnly && restored.Views[same].Name == testing.Name,
                "saved manual testing assignments remain valid on load");
        }

        void CheckCombatOrigins()
        {
            foreach (string originId in new[] { "kenshiorigins.soldier", "kenshiorigins.gunner", "kenshiorigins.assassin" })
            {
                var definition = catalog.Find(originId)!;
                var system = MakeSystem();
                using var world = MakeWorld(system);
                var id = Spawn(world, definition.Name, true);
                world.Player.InCreation = true;
                system.Ready(world, false);
                Cold(world, id).Appearance!.Loadout!.Appearance.Record.Ints[ModLooks.KeyOf(OriginsMod.Id, OriginSystem.ChoiceKey)] = catalog.IndexOf(originId);
                world.Player.InCreation = false;
                system.Reconcile(world);
                Check(system.Views[id].Description == definition.Description && definition.Description.Length > 100,
                    $"{definition.Name} publishes its full origin story after creation");
                Check(definition.Bonuses.All(b => Math.Abs(Cold(world, id).Stats![b.Key] - (1 + b.Value)) < .001f),
                    $"{definition.Name} applies its supported combat bonuses");
            }
        }

        void CheckUi()
        {
            var harmony = new Harmony("kenshiorigins.checks");
            bool coreUi = args.Contains("--core-ui");
            var uiCatalog = coreUi ? new OriginCatalog([.. bundled.All, .. (args.Contains("--extra-pack-ui")
                ? OriginCatalog.ReadFile(Path.Combine(repo, "examples/ExtraOrigins/originform.json")) : [])])
                : new OriginCatalog(catalog.All.Select(origin => origin.Id == "kenshiorigins.soldier"
                    ? origin with { TestingOnly = true } : origin));
            OriginUi.Install(harmony, uiCatalog, logs.Add);
            var system = new OriginSystem(uiCatalog, RngPurpose.Test, logs.Add, new ItemFactory(db, GameConstants.Default));
            using var session = new WorldSession(Vector3.Zero, (-100, -100, 100, 100), (_, _) => 0,
                simulation: new WorldSettings { Seed = 918273 }, systems: [new PlayerSystem(), system]);
            var world = session.World;
            world.Player.Faction = 0;
            var id = Spawn(world, "Paused starter", true);
            var companion = Spawn(world, "Assassin companion", true);
            world.Player.Selection.Add(id);
            world.Player.InCreation = true;
            world.TellReady(false);
            Cold(world, id).Appearance!.Loadout!.Appearance.Record.Ints[ModLooks.KeyOf(OriginsMod.Id, OriginSystem.ChoiceKey)] =
                uiCatalog.IndexOf("kenshiorigins.soldier");
            Cold(world, companion).Appearance!.Loadout!.Appearance.Record.Ints[ModLooks.KeyOf(OriginsMod.Id, OriginSystem.ChoiceKey)] =
                uiCatalog.IndexOf("kenshiorigins.assassin");
            world.Commands.Enqueue(new FinishCreation { Tick = world.Tick });
            world.ApplyPausedInterfaceCommands();
            Check(!world.Player.InCreation && !system.Views[id].Pending, "real paused FinishCreation triggers assignment through the Harmony bridge");
            Check(Cold(world, id).Stats!.Modifiers.All.Count > 0, "paused confirmation applies bonuses before unpausing");
            Check(Cold(world, id).Inventory!.Items.Single().Record == "475-gamedata.base"
                && Cold(world, id).Inventory!.Items.Single().Section == "main",
                "real paused FinishCreation delivers starting gear into inventory before unpausing");

            var playerType = typeof(IMod).Assembly.GetType("Meitou.Game.PlayerInterface", true)!;
            var player = Activator.CreateInstance(playerType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null,
                [session, (Func<float, float, float>)((_, _) => 0), (Func<CharacterSnapshot, Vector3>)(c => c.Position)], null)!;
            AccessTools.Field(playerType, "FaithfulScreens").SetValue(player, new MyGuiScreenHost(new MyGuiResources()));
            AccessTools.Field(playerType, "FaithfulHud").SetValue(player, new MyGuiLayout("", []));
            var statsType = typeof(IMod).Assembly.GetType("Meitou.Game.Ui.StatsWindow", true)!;
            var stats = Activator.CreateInstance(statsType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, [player], null)!;
            var controls = new List<UiControl>
            {
                new("AttributesPanel", "Widget", new UiRect(10, 10, 245, 180), "", "", "", true, new Dictionary<string, string>(), Vector4.Zero)
            };
            AccessTools.Method(statsType, "AddStatsContent").Invoke(stats, [controls]);
            Check(controls.Any(c => c.Name == "kenshiorigins.stats" && c.Caption.Contains(system.Views[id].Name)), "real stats content hook displays the selected character's origin");
            Check(controls.Single(c => c.Name == "kenshiorigins.stats").Action == "stat:" + OriginUi.OriginStat,
                "origin entry uses native stat selection to expose its story");
            Check(controls.Where(c => c.Name.StartsWith("kenshiorigins.") && c.Name != "kenshiorigins.stats").All(c => c.Pickable == false),
                "origin body text does not intercept game controls");

            string key = ModLooks.KeyOf(OriginsMod.Id, OriginSystem.ChoiceKey);
            var draft = new CharacterDraft(db, scratch, Cold(world, id).Appearance!.Loadout!.Appearance.Record, "Draft") { Creation = true };
            draft.Record.Ints[key] = 0;
            draft.SetFemale(true, 5);
            draft.Record.Ints[key] = 1;
            draft.SetFemale(false, 6);
            Check(draft.Record.Ints[key] == 1, "returning to a cached gender look preserves the latest origin choice");
            draft.SetRace(db.Find("race2")!, 7);
            draft.Record.Ints[key] = 2;
            draft.SetRace(db.Find("race")!, 8);
            Check(draft.Record.Ints[key] == 2, "returning to a cached race look preserves the latest origin choice");
            draft.Reset();
            Check(draft.Record.Ints[key] == 2, "appearance Reset does not reset the origin choice");

            var editor = AccessTools.Property(playerType, "CharacterEditor").GetValue(player)!;
            var editorType = editor.GetType();
            var openingType = AccessTools.Field(editorType, "Editor").FieldType;
            var opening = Activator.CreateInstance(openingType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null,
                [EditKind.Creation, new[] { id }, new HashSet<CharacterId>(), CharacterId.None, false], null)!;
            AccessTools.Field(editorType, "Editor").SetValue(editor, opening);
            AccessTools.Property(editorType, "EditorData").SetValue(editor, (db, scratch));
            ((Dictionary<CharacterId, CharacterDraft>)AccessTools.Property(openingType, "Drafts").GetValue(opening)!)[id] = draft;
            AccessTools.Field(playerType, "Width").SetValue(player, 1280);
            AccessTools.Field(playerType, "Height").SetValue(player, 900);
            CreationPanelChecks.Run(uiCatalog, db, session, player, editor, draft, id, uiRoot, Check, theme, preview, coreUi);
            harmony.UnpatchAll(harmony.Id);
        }

        ulong DeterministicHash(int threads)
        {
            var system = MakeSystem();
            using var world = new World(new WorldSettings { Seed = 12345, Threads = threads, MinPartitionSize = 1, PublishSnapshots = false },
                new OpenGroundWalkability((_, _) => 0), [system]);
            world.Player.Faction = 0;
            Spawn(world, "One", true);
            Spawn(world, "Two", true);
            system.Ready(world, false);
            world.RunTicks(3);
            return world.StateHash();
        }

        OriginSystem MakeSystem() => new(catalog, RngPurpose.Test, logs.Add);

        World MakeWorld(OriginSystem system)
        {
            var world = new World(new WorldSettings { Seed = 12345, PublishSnapshots = false }, new OpenGroundWalkability((_, _) => 0), [system]);
            world.Player.Faction = 0;
            return world;
        }

        CharacterId Spawn(World world, string name, bool player)
        {
            var squad = new Squad { Id = world.Squads.NextId(), TemplateId = "test", Faction = player ? 0 : 1, HomeCentre = Vector2.Zero, HomeRadius = 10 };
            world.Squads.Add(squad);
            var appearance = new CharacterAppearance { Race = db.Find("race")! };
            var loadout = new Loadout { Seed = 1, Race = db.Find("race")!, Female = false, Appearance = new AppearanceFile { Record = new FcsRecord() } };
            typeof(CharacterAppearance).GetProperty("Loadout")!.SetValue(appearance, loadout);
            var cold = new CharacterCold
            {
                Name = name, RecordId = "hero", IsPlayer = player, Faction = squad.Faction, SquadId = squad.Id,
                Stats = new CharacterStats(), Appearance = appearance, Inventory = new Inventory()
            };
            var id = world.Characters.Spawn(new CharacterHot { Health = 100 }, cold, 0);
            squad.Members.Add(id);
            squad.Leader = id;
            return id;
        }

        static CharacterCold Cold(World world, CharacterId id) => world.Characters.Cold(id.Slot)!;

        static byte[] Save(IModSaveData system)
        {
            using var stream = new MemoryStream();
            system.Write(stream);
            return stream.ToArray();
        }

        static void WriteMod(string path, string description, params string[] dependencies)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var file = new FcsFile { Author = "Kenshi Origins", Description = description };
            file.Dependencies.AddRange(dependencies);
            FcsWriter.WriteFile(file, path);
        }

        void Check(bool condition, string message)
        {
            if (!condition) throw new Exception("FAIL: " + message);
            assertions++;
            Console.WriteLine("  OK " + message);
        }
    }
}
