using System.Text.Json;
using Meitou.Data.Characters;
using Meitou.Data.Fcs;
using Meitou.Modding;
using Meitou.Simulation;
using Meitou.Simulation.Bodies;
using Meitou.Simulation.Factions;

namespace KenshiOrigins;

/// <summary>Owns per-world assignments. Only simulation callbacks may change characters or this state.</summary>
internal sealed class OriginSystem(OriginCatalog catalog, RngPurpose randomPurpose, Action<string> log)
    : IWorldReady, IModSaveData, IStateHashed
{
    internal const string ChoiceKey = "origin";
    internal const string ModifierKey = "kenshiorigins.origin";

    private readonly Dictionary<LastingId, string> _assignments = [];
    private readonly Dictionary<CharacterId, int> _startingChoices = [];
    private readonly Dictionary<CharacterId, AppliedBonus> _applied = [];
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
                var origin = catalog.All[choice];
                originId = origin.Id;
                var lasting = world.LastingIds.GetOrGive(id);
                if (lasting.IsNone) continue;
                _assignments.Add(lasting, originId);
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
            views[id] = View(definition, pending: false);
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
        return (uint)value < catalog.All.Count ? value : fallback;
    }

    private int RandomChoice(World world, CharacterId id) =>
        (int)(Rng.Hash(world.Seed, Rng.Key(id), randomPurpose) % (ulong)catalog.All.Count);

    private static OriginView View(OriginDefinition origin, bool pending) => new(origin.Name, origin.Description, origin.BonusSummary, pending);

    /// <summary>Assignments persist by lasting ID; runtime modifiers and the temporary roster do not.</summary>
    public void Write(Stream stream)
    {
        var saved = _assignments.OrderBy(a => a.Key.Value).Select(a => new SavedOrigin(a.Key.Value, a.Value)).ToArray();
        JsonSerializer.Serialize(stream, new OriginSave(1, saved), OriginCatalog.JsonOptions);
    }

    public void Read(Stream stream)
    {
        var saved = JsonSerializer.Deserialize<OriginSave>(stream, OriginCatalog.JsonOptions)
            ?? throw new InvalidDataException("Empty origin save data.");
        if (saved.FormatVersion != 1 || saved.Assignments is null)
            throw new InvalidDataException("Unsupported origin save format.");
        var restored = new Dictionary<LastingId, string>();
        foreach (var entry in saved.Assignments)
        {
            if (entry is null || entry.Character == 0 || string.IsNullOrWhiteSpace(entry.OriginId)
                || !restored.TryAdd(new LastingId(entry.Character), entry.OriginId))
                throw new InvalidDataException("Invalid or duplicate origin assignment in save data.");
        }
        _assignments.Clear();
        foreach (var (id, origin) in restored) _assignments.Add(id, origin);
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
        }
        foreach (var (id, origin) in _assignments.OrderBy(a => a.Key.Value))
        {
            hasher.Add(id.Value);
            hasher.Add(Rng.StableHash(origin));
        }
        foreach (var (id, choice) in _startingChoices.OrderBy(a => a.Key.Slot).ThenBy(a => a.Key.Generation))
        {
            hasher.Add(id);
            hasher.Add(choice);
        }
    }

    private sealed record AppliedBonus(CharacterStats Stats, string OriginId);
    private sealed record SavedOrigin(ulong Character, string OriginId);
    private sealed record OriginSave(int FormatVersion, SavedOrigin[] Assignments);
}

internal sealed record OriginView(string Name, string Description, string Bonuses, bool Pending);
