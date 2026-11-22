using System.Text.Json;
using Meitou.Data.Characters;
using Meitou.Data.Fcs;
using Meitou.Modding;
using Meitou.Simulation;
using Meitou.Simulation.Bodies;
using Meitou.Simulation.Factions;
using Meitou.Simulation.Items;

namespace KenshiOrigins;

/// <summary>Owns per-world assignments. Only simulation callbacks may change characters or this state.</summary>
internal sealed class OriginSystem(OriginCatalog catalog, RngPurpose randomPurpose, Action<string> log, ItemFactory? items = null)
    : IWorldReady, IModSaveData, IStateHashed
{
    internal const string ChoiceKey = "origin";
    internal const string ModifierKey = "kenshiorigins.origin";

    private readonly Dictionary<LastingId, string> _assignments = [];
    private readonly Dictionary<CharacterId, int> _startingChoices = [];
    private readonly Dictionary<CharacterId, AppliedBonus> _applied = [];
    private readonly Dictionary<LastingId, OriginItem[]> _pendingItems = [];
    private readonly HashSet<LastingId> _waitingLogged = [];
    private readonly OriginEquipment? _equipment = items is null ? null : new(items, log);
    private readonly HashSet<string> _missingLogged = new(StringComparer.Ordinal);
    private Dictionary<CharacterId, OriginView> _views = [];

    /// <summary>The UI reads published copies, never the live table while simulation work runs.</summary>
    internal IReadOnlyDictionary<CharacterId, OriginView> Views => Volatile.Read(ref _views);
    internal OriginCatalog Catalog => catalog;

    public void Ready(World world, bool fromSave)
    {
        if (!fromSave)
        {
            foreach (var id in SquadManagement.PlayerSquads(world).SelectMany(s => s.Members))
            {
                if (!world.Characters.TryResolveNext(id, out int slot) || world.Characters.Cold(slot) is not { } cold) continue;
                int choice = RandomChoice(world, id);
                _startingChoices[id] = choice;
                // The per-character rolled look is copied into the editor draft. Never write a shared body-file record.
                if (cold.Appearance?.Loadout?.Appearance.Record is { } look)
                    look.Ints[ModLooks.KeyOf(OriginsMod.Id, ChoiceKey)] = choice;
            }
        }
        Reconcile(world);
    }

    public void Inputs(World world, IReadOnlyList<SimCommand> commands) => Reconcile(world);
    public void SlowWorld(World world) => Reconcile(world);

    /// <summary>Also called after paused commands: this is a serial tick boundary, not a UI render callback.</summary>
    internal bool Reconcile(World world)
    {
        bool changed = false;
        var table = world.Characters;
        var views = new Dictionary<CharacterId, OriginView>();
        var present = new HashSet<CharacterId>();
        for (int slot = 0; slot < table.HighWater; slot++)
        {
            if (table.Cold(slot) is not { } cold) continue;
            var id = table.IdOf(slot);
            if (!table.TryResolveNext(id, out _)) continue;
            present.Add(id);

            string? originId = cold.LastingId.IsNone ? null : _assignments.GetValueOrDefault(cold.LastingId);
            if (originId is null && cold.IsPlayer && cold.Medical?.Dead != true && cold.Stats is not null)
            {
                if (world.Player.InCreation)
                {
                    if (_startingChoices.TryGetValue(id, out int pending)) views[id] = View(catalog.All[pending], pending: true);
                    continue;
                }

                int choice = _startingChoices.TryGetValue(id, out int initial)
                    ? ReadChoice(cold.Appearance?.Loadout?.Appearance.Record, initial) : RandomChoice(world, id);
                if (choice == catalog.All.Count) choice = RandomChoice(world, id);
                var origin = catalog.All[choice];
                originId = origin.Id;
                var lasting = world.LastingIds.GetOrGive(id);
                if (lasting.IsNone) continue;
                _assignments.Add(lasting, originId);
                if (origin.Items.Count > 0) _pendingItems[lasting] = origin.Items.ToArray();
                _startingChoices.Remove(id);
                log($"Assigned {origin.Name} to {cold.Name} ({lasting}).");
                changed = true;
            }
            if (originId is null) continue;

            var definition = catalog.Find(originId);
            if (definition is null)
            {
                if (_missingLogged.Add(originId)) log($"Saved origin '{originId}' is unavailable; keeping its ID without bonuses.");
                views[id] = new OriginView($"Unavailable origin: {originId}", "Its origin pack is not loaded.", "No bonuses applied", false);
                continue;
            }
            changed |= Apply(id, cold.Stats, definition);
            if (_equipment is not null && _pendingItems.TryGetValue(cold.LastingId, out var pendingItems))
            {
                var remaining = _equipment.Deliver(cold, pendingItems);
                if (!remaining.SequenceEqual(pendingItems))
                {
                    if (remaining.Length == 0) _pendingItems.Remove(cold.LastingId);
                    else _pendingItems[cold.LastingId] = remaining;
                    changed = true;
                }
                if (remaining.Length > 0 && cold.IsPlayer && _waitingLogged.Add(cold.LastingId))
                    log($"Origin items for {cold.Name} are waiting for inventory space; they will be retried.");
            }
            views[id] = View(definition, pending: false) with
            {
                ItemsWaiting = _pendingItems.TryGetValue(cold.LastingId, out var waiting) ? waiting.Sum(item => item.Quantity) : 0
            };
        }

        foreach (var gone in _applied.Keys.Where(id => !present.Contains(id)).ToArray()) _applied.Remove(gone);
        foreach (var gone in _startingChoices.Keys.Where(id => !present.Contains(id)).ToArray()) _startingChoices.Remove(gone);
        Volatile.Write(ref _views, views);
        return changed;
    }

    private bool Apply(CharacterId id, CharacterStats? stats, OriginDefinition origin)
    {
        if (stats is null) return false;
        if (_applied.TryGetValue(id, out var previous) && ReferenceEquals(previous.Stats, stats) && previous.OriginId == origin.Id)
            return false;

        stats.Modifiers.RemoveAll(ModifierKey);
        foreach (var (stat, amount) in origin.Bonuses) stats.Modifiers.Set(ModifierKey, stat, amount);
        _applied[id] = new AppliedBonus(stats, origin.Id);
        return true;
    }

    internal int ReadChoice(FcsRecord? look, int fallback = 0)
    {
        int value = look is null ? fallback : ModLooks.Int(look, OriginsMod.Id, ChoiceKey, fallback);
        // The extra last index represents Random; real origin indices keep their previous meaning.
        return (uint)value <= catalog.All.Count ? value : fallback;
    }

    private int RandomChoice(World world, CharacterId id) =>
        (int)(Rng.Hash(world.Seed, Rng.Key(id), randomPurpose) % (ulong)catalog.All.Count);

    internal OriginView Preview(OriginDefinition origin) => View(origin, pending: true);

    private OriginView View(OriginDefinition origin, bool pending) => new(origin.Name, origin.Description, origin.BonusSummary, pending,
        origin.Difficulty, origin.Items.Count == 0 ? "None" : string.Join(", ", origin.Items.Select(item =>
            $"{item.Quantity} × {items?.Db.Find(item.Record)?.Name ?? "Unavailable item"}")));

    /// <summary>Assignments persist by lasting ID; runtime modifiers and the temporary roster do not.</summary>
    public void Write(Stream stream)
    {
        var saved = _assignments.OrderBy(a => a.Key.Value).Select(a => new SavedOrigin(a.Key.Value, a.Value,
            _pendingItems.GetValueOrDefault(a.Key) ?? [])).ToArray();
        JsonSerializer.Serialize(stream, new OriginSave(2, saved), OriginCatalog.JsonOptions);
    }

    public void Read(Stream stream)
    {
        var saved = JsonSerializer.Deserialize<OriginSave>(stream, OriginCatalog.JsonOptions)
            ?? throw new InvalidDataException("Empty origin save data.");
        if (saved.FormatVersion is not (1 or 2) || saved.Assignments is null)
            throw new InvalidDataException("Unsupported origin save format.");
        var restored = new Dictionary<LastingId, string>();
        var pending = new Dictionary<LastingId, OriginItem[]>();
        foreach (var entry in saved.Assignments)
        {
            if (entry is null || entry.Character == 0 || string.IsNullOrWhiteSpace(entry.OriginId)
                || !restored.TryAdd(new LastingId(entry.Character), entry.OriginId))
                throw new InvalidDataException("Invalid or duplicate origin assignment in save data.");
            // Version 1 predates equipment. Give those characters their first grant exactly once.
            var grants = saved.FormatVersion == 1 ? catalog.Find(entry.OriginId)?.Items.ToArray() ?? []
                : entry.PendingItems ?? throw new InvalidDataException("Missing pending origin items in save data.");
            if (grants.Any(item => item is null || string.IsNullOrWhiteSpace(item.Record)
                || item.Quantity is < 1 or > 1000 || item.Quality is < 0 or > 100))
                throw new InvalidDataException("Invalid pending origin item in save data.");
            if (grants.Length > 0) pending.Add(new LastingId(entry.Character), grants);
        }
        _assignments.Clear();
        foreach (var (id, origin) in restored) _assignments.Add(id, origin);
        _pendingItems.Clear();
        foreach (var (id, grants) in pending) _pendingItems.Add(id, grants);
        _waitingLogged.Clear();
        _startingChoices.Clear();
        _applied.Clear();
    }

    /// <summary>Include our decisions and content in the engine's determinism checks.</summary>
    public void Hash(ref StateHasher hasher)
    {
        foreach (var definition in catalog.All)
        {
            hasher.Add(Rng.StableHash(definition.Id));
            foreach (var (stat, amount) in definition.Bonuses.OrderBy(b => b.Key))
            {
                hasher.Add((int)stat);
                hasher.Add(amount);
            }
            HashItems(ref hasher, definition.Items);
        }
        foreach (var (id, origin) in _assignments.OrderBy(a => a.Key.Value))
        {
            hasher.Add(id.Value);
            hasher.Add(Rng.StableHash(origin));
            HashItems(ref hasher, _pendingItems.GetValueOrDefault(id) ?? []);
        }
        foreach (var (id, choice) in _startingChoices.OrderBy(a => a.Key.Slot).ThenBy(a => a.Key.Generation))
        {
            hasher.Add(id);
            hasher.Add(choice);
        }
    }

    private static void HashItems(ref StateHasher hasher, IReadOnlyList<OriginItem> grants)
    {
        hasher.Add(grants.Count);
        foreach (var item in grants)
        {
            hasher.Add(Rng.StableHash(item.Record));
            hasher.Add(item.Quantity);
            hasher.Add(item.Quality);
        }
    }

    private sealed record AppliedBonus(CharacterStats Stats, string OriginId);
    private sealed record SavedOrigin(ulong Character, string OriginId, OriginItem[]? PendingItems = null);
    private sealed record OriginSave(int FormatVersion, SavedOrigin[] Assignments);
}

internal sealed record OriginView(string Name, string Description, string Bonuses, bool Pending,
    string Difficulty = "Unrated", string Items = "None", int ItemsWaiting = 0);
