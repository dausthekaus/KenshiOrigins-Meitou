using System.Numerics;
using System.Text.Json;
using KenshiOrigins;
using Meitou.Data;
using Meitou.Data.Fcs;
using Meitou.Data.Gameplay;
using Meitou.Data.Gameplay.Bodies;
using Meitou.Simulation;
using Meitou.Simulation.Bodies;
using Meitou.Simulation.Items;
using Meitou.Simulation.Factions;

internal static class EquipmentChecks
{
    internal static void Run(OriginCatalog builtins, string? install, Action<bool, string> check)
    {
        var db = new GameDatabase();
        var file = new FcsFile();
        file.Records.Add(new FcsRecord { StringId = "test.supply", Name = "Supplies", RecordType = FcsRecordType.Item,
            Ints = { ["inventory footprint width"] = 1, ["inventory footprint height"] = 1, ["stackable"] = 5 } });
        file.Records.Add(new FcsRecord { StringId = "test.weapon", Name = "Weapon", RecordType = FcsRecordType.Weapon,
            Ints = { ["inventory footprint width"] = 2, ["inventory footprint height"] = 1 } });
        db.Apply(file, "equipment-fixture.mod");
        var factory = new ItemFactory(db, GameConstants.Default);
        var logs = new List<string>();
        OriginCatalog Catalog(params OriginItem[] items) => new([new OriginDefinition("test.origin", "Test", "Test story",
            new Dictionary<StatsEnumerated, float> { [StatsEnumerated.Strength] = 2 }, Array.AsReadOnly(items), "Challenging")]);
        var catalog = Catalog(new("test.weapon", 1, 15), new("test.supply", 7, 0));
        OriginSystem System(OriginCatalog? pool = null) => new(pool ?? catalog, RngPurpose.Test, logs.Add, factory);
        World MakeWorld(OriginSystem system)
        {
            var world = new World(new WorldSettings { Seed = 45, PublishSnapshots = false },
                new OpenGroundWalkability((_, _) => 0), [system]);
            world.Player.Faction = 0;
            return world;
        }
        CharacterId Spawn(World world, Inventory? inventory, bool player = true)
        {
            // A start may put characters in multiple custom squads, not just one named template.
            var squad = new Squad { Id = world.Squads.NextId(), TemplateId = "custom-start-squad", Faction = player ? 0 : 1,
                HomeCentre = Vector2.Zero, HomeRadius = 10 };
            world.Squads.Add(squad);
            var id = world.Characters.Spawn(new CharacterHot { Health = 100 },
                new CharacterCold { Name = "Equipment test", IsPlayer = player, SquadId = squad.Id, Faction = squad.Faction,
                    Stats = new CharacterStats(), Inventory = inventory }, 0);
            squad.Members.Add(id);
            squad.Leader = id;
            return id;
        }
        static CharacterCold Cold(World world, CharacterId id) => world.Characters.Cold(id.Slot)!;
        static byte[] Save(OriginSystem system)
        {
            using var stream = new MemoryStream();
            system.Write(stream);
            return stream.ToArray();
        }
        static void Restore(OriginSystem system, byte[] saved)
        {
            using var stream = new MemoryStream(saved);
            system.Read(stream);
        }
        static void Lasting(CharacterCold cold, LastingId id) => typeof(CharacterCold).GetProperty("LastingId")!.SetValue(cold, id);

        var origin = System();
        using var world = MakeWorld(origin);
        var inventory = new Inventory();
        var worn = factory.Create("test.weapon")!;
        worn.Section = "back";
        inventory.Items.Add(worn);
        var id = Spawn(world, inventory);
        var secondStarter = Spawn(world, new Inventory());
        var recruit = Spawn(world, new Inventory(), false);
        world.Player.InCreation = true;
        origin.Ready(world, false);
        check(inventory.Items.Count == 1 && Cold(world, secondStarter).Inventory!.Items.Count == 0,
            "initial-roster equipment waits until character creation is confirmed");
        var addedDuringCreation = Spawn(world, new Inventory());
        origin.Reconcile(world);
        world.Player.InCreation = false;
        origin.Reconcile(world);
        check(inventory.Items.Contains(worn) && worn.Section == "back"
            && inventory.Items.Where(i => !ReferenceEquals(i, worn)).All(i => i.Section == "main"),
            "origin items go into storage while custom-start worn gear stays untouched");
        check(inventory.Items.Where(i => i.Record == "test.supply").Sum(i => i.Quantity) == 7
            && inventory.Items.Single(i => i.Record == "test.weapon" && i.Section == "main").Level == 15,
            "JSON quantities and weapon quality reach the inventory");
        check(Cold(world, secondStarter).Inventory!.Items.Where(i => i.Record == "test.supply").Sum(i => i.Quantity) == 7,
            "initial starters in another modded squad receive their starting equipment");
        check(origin.Views.ContainsKey(addedDuringCreation) && Cold(world, addedDuringCreation).Stats!.Strength == 3
            && Cold(world, addedDuringCreation).Inventory!.Items.Count == 0 && origin.Views[addedDuringCreation].ItemsWaiting == 0,
            "a player character added after initial capture gets an origin and bonuses without equipment");
        Cold(world, recruit).IsPlayer = true;
        origin.Reconcile(world);
        check(origin.Views[recruit].Name == "Test" && Cold(world, recruit).Stats!.Strength == 3
            && Cold(world, recruit).Inventory!.Items.Count == 0 && origin.Views[recruit].ItemsWaiting == 0,
            "later recruitment assigns an origin and stat bonuses without creating an equipment queue");
        Cold(world, recruit).IsPlayer = false;
        origin.Reconcile(world);
        Cold(world, recruit).IsPlayer = true;
        origin.Reconcile(world);
        check(Cold(world, recruit).Inventory!.Items.Count == 0 && origin.Views[recruit].ItemsWaiting == 0,
            "dismissal and re-recruitment cannot turn a later recruit into a starter");
        var lateSpawn = Spawn(world, new Inventory());
        origin.Reconcile(world);
        check(origin.Views.ContainsKey(lateSpawn) && Cold(world, lateSpawn).Stats!.Strength == 3
            && Cold(world, lateSpawn).Inventory!.Items.Count == 0 && origin.Views[lateSpawn].ItemsWaiting == 0,
            "direct player spawns after creation also receive no starting equipment");
        origin.Reconcile(world);
        Cold(world, id).IsPlayer = false;
        origin.Reconcile(world);
        Cold(world, id).IsPlayer = true;
        origin.Reconcile(world);
        check(inventory.Items.Where(i => i.Record == "test.supply").Sum(i => i.Quantity) == 7,
            "reconciliation and re-recruitment cannot duplicate equipment");
        var restored = System();
        Restore(restored, Save(origin));
        using var loaded = MakeWorld(restored);
        Spawn(loaded, null, false);
        var same = Spawn(loaded, inventory.Copy());
        Lasting(Cold(loaded, same), Cold(world, id).LastingId);
        restored.Ready(loaded, true);
        check(Cold(loaded, same).Inventory!.Items.Where(i => i.Record == "test.supply").Sum(i => i.Quantity) == 7,
            "saving and loading with changed runtime IDs cannot grant equipment twice");
        var loadedRecruit = Spawn(loaded, new Inventory());
        Lasting(Cold(loaded, loadedRecruit), Cold(world, recruit).LastingId);
        restored.Reconcile(loaded);
        check(Cold(loaded, loadedRecruit).Stats!.Strength == 3 && Cold(loaded, loadedRecruit).Inventory!.Items.Count == 0
            && restored.Views[loadedRecruit].ItemsWaiting == 0,
            "a saved later recruit keeps bonuses and never acquires starting gear on load");

        var supplies = Catalog(new OriginItem("test.supply", 7, 0));
        var partial = System(supplies);
        using var partialWorld = MakeWorld(partial);
        var small = new Inventory(1, 1);
        small.Add(factory.Create("test.supply", 3)!);
        var partialId = Spawn(partialWorld, small);
        partial.Ready(partialWorld, false);
        check(small.Items.Single().Quantity == 5 && partial.Views[partialId].ItemsWaiting == 5,
            "partial stack delivery records only the undelivered quantity");
        partial.Reconcile(partialWorld);
        check(small.Items.Single().Quantity == 5 && partial.Views[partialId].ItemsWaiting == 5,
            "a full inventory retains its queue without duplicating partial deliveries");
        var retry = System(supplies);
        Restore(retry, Save(partial));
        using var retryWorld = MakeWorld(retry);
        var retryId = Spawn(retryWorld, small.Copy());
        Lasting(Cold(retryWorld, retryId), Cold(partialWorld, partialId).LastingId);
        retry.Ready(retryWorld, true);
        Cold(retryWorld, retryId).Inventory!.Items.Clear();
        retry.Reconcile(retryWorld);
        retry.Reconcile(retryWorld);
        check(Cold(retryWorld, retryId).Inventory!.Items.Single().Quantity == 5 && retry.Views[retryId].ItemsWaiting == 0,
            "the saved remainder arrives once when space becomes available");
        check(retryWorld.GroundItems.All.Count == 0, "full inventories never spill origin equipment onto the ground");

        var backpack = new ItemInstance { Record = "existing.backpack", Section = "backpack_attach", Contents = new Inventory(2, 2) };
        var full = new Inventory(1, 1);
        full.Add(factory.Create("test.supply", 5)!);
        full.Items.Add(backpack);
        var backpackCharacter = new CharacterCold { IsPlayer = true, Inventory = full };
        var backpackRemainder = new OriginEquipment(factory, logs.Add).Deliver(backpackCharacter, [new("test.supply", 2, 0)]);
        check(backpackRemainder.Length == 0 && backpack.Section == "backpack_attach"
            && backpack.Contents!.Items.Single().Section == "backpack_content" && backpack.Contents.Items.Single().Quantity == 2,
            "a full main inventory uses existing backpack storage without replacing or equipping anything");

        var deferred = System(supplies);
        using var deferredWorld = MakeWorld(deferred);
        var deferredId = Spawn(deferredWorld, null);
        deferred.Ready(deferredWorld, false);
        check(deferred.Views[deferredId].ItemsWaiting == 7, "characters without an inventory keep their equipment pending");
        Cold(deferredWorld, deferredId).Inventory = new Inventory();
        deferred.Reconcile(deferredWorld);
        check(Cold(deferredWorld, deferredId).Inventory!.Items.Sum(i => i.Quantity) == 7,
            "equipment arrives after the character's inventory is initialized");

        var missing = System(Catalog(new("removed.item", 1, 0), new("test.supply", 1, 0)));
        using var missingWorld = MakeWorld(missing);
        var missingId = Spawn(missingWorld, new Inventory());
        missing.Ready(missingWorld, false);
        missing.Reconcile(missingWorld);
        check(Cold(missingWorld, missingId).Inventory!.Items.Single().Record == "test.supply"
            && logs.Count(l => l.Contains("'removed.item'")) == 1, "missing item records are logged once and do not block valid equipment");

        var legacy = System(supplies);
        byte[] old = JsonSerializer.SerializeToUtf8Bytes(new { FormatVersion = 1,
            Assignments = new[] { new { Character = 400UL, OriginId = "test.origin" } } });
        Restore(legacy, old);
        using var legacyWorld = MakeWorld(legacy);
        var legacyId = Spawn(legacyWorld, new Inventory());
        Lasting(Cold(legacyWorld, legacyId), new LastingId(400));
        legacy.Ready(legacyWorld, true);
        check(Cold(legacyWorld, legacyId).Stats!.Strength == 3 && Cold(legacyWorld, legacyId).Inventory!.Items.Count == 0
            && legacy.Views[legacyId].ItemsWaiting == 0,
            "version-1 migration keeps origins and bonuses without inventing starter eligibility");
        var migrated = System(supplies);
        Restore(migrated, Save(legacy));
        migrated.Reconcile(legacyWorld);
        check(Cold(legacyWorld, legacyId).Inventory!.Items.Count == 0 && migrated.Views[legacyId].ItemsWaiting == 0,
            "saving migrated assignments cannot create new equipment grants");

        var installedLater = System(supplies);
        using var existingWorld = MakeWorld(installedLater);
        var existingPlayer = Spawn(existingWorld, new Inventory());
        installedLater.Ready(existingWorld, true);
        check(installedLater.Views.ContainsKey(existingPlayer) && Cold(existingWorld, existingPlayer).Stats!.Strength == 3
            && Cold(existingWorld, existingPlayer).Inventory!.Items.Count == 0 && installedLater.Views[existingPlayer].ItemsWaiting == 0,
            "installing on an existing save assigns origins and bonuses without assuming a new-game roster");

        var hashBefore = new StateHasher();
        partial.Hash(ref hashBefore);
        var hashAfter = new StateHasher();
        retry.Hash(ref hashAfter);
        check(hashBefore.Value != hashAfter.Value, "pending equipment contributes to deterministic state hashes");

        if (install is null) return;
        var core = new GameDatabase();
        foreach (string fileName in LoadOrder.BaseFiles)
            if (File.Exists(Path.Combine(install, "data", fileName)))
                core.Apply(FcsReader.ReadFile(Path.Combine(install, "data", fileName)), fileName);
        var nativeFactory = new ItemFactory(core, GameConstants.Default);
        foreach (var definition in builtins.All)
        {
            var pack = new Inventory();
            var character = new CharacterCold { IsPlayer = true, Inventory = pack };
            var grants = new OriginEquipment(nativeFactory, logs.Add).Deliver(character, definition.Items);
            check(definition.Items.Count > 0 && definition.Items.All(i => core.Find(i.Record) is not null)
                && grants.Length == 0 && pack.Items.All(i => i.Section == "main")
                && pack.Items.Sum(i => i.Quantity) == definition.Items.Sum(i => i.Quantity),
                $"{definition.Name}'s actual vanilla item records all fit unequipped in an empty inventory");
        }
        var gunner = builtins.Find("kenshiorigins.gunner")!;
        var bow = core.Find(gunner.Items.Single(i => core.Find(i.Record)?.Type == FcsRecordType.Crossbow).Record)!;
        check(bow.GetReferences("ammo").Any(a => gunner.Items.Any(i => i.Record == a.TargetStringId)),
            "Gunner receives the Tooth Pick's actual compatible ammunition");
    }
}
