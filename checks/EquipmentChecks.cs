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
            new Dictionary<StatsEnumerated, float>(), Array.AsReadOnly(items), "Challenging")]);
        var catalog = Catalog(new("test.weapon", 1, 15), new("test.supply", 7, 0));
        OriginSystem System(OriginCatalog? pool = null) => new(pool ?? catalog, RngPurpose.Test, logs.Add, factory);
        World MakeWorld(OriginSystem system) => new(new WorldSettings { Seed = 45, PublishSnapshots = false },
            new OpenGroundWalkability((_, _) => 0), [system]);
        CharacterId Spawn(World world, Inventory? inventory, bool player = true) => world.Characters.Spawn(new CharacterHot { Health = 100 },
            new CharacterCold { Name = "Equipment test", IsPlayer = player, Stats = new CharacterStats(), Inventory = inventory }, 0);
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
        world.Player.InCreation = true;
        origin.Ready(world, false);
        check(inventory.Items.Count == 1, "origin gear is not granted while character creation is open");
        world.Player.InCreation = false;
        origin.Reconcile(world);
        check(inventory.Items.Contains(worn) && worn.Section == "back"
            && inventory.Items.Where(i => !ReferenceEquals(i, worn)).All(i => i.Section == "main"),
            "origin items go into storage while custom-start worn gear stays untouched");
        check(inventory.Items.Where(i => i.Record == "test.supply").Sum(i => i.Quantity) == 7
            && inventory.Items.Single(i => i.Record == "test.weapon" && i.Section == "main").Level == 15,
            "JSON quantities and weapon quality reach the inventory");
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
        check(Cold(legacyWorld, legacyId).Inventory!.Items.Sum(i => i.Quantity) == 7,
            "pre-equipment version-1 saves receive their first grant during migration");
        var migrated = System(supplies);
        Restore(migrated, Save(legacy));
        migrated.Reconcile(legacyWorld);
        check(Cold(legacyWorld, legacyId).Inventory!.Items.Sum(i => i.Quantity) == 7,
            "migrated equipment completion persists in version-2 saves");

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
