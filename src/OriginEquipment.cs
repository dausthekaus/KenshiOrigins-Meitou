using Meitou.Data.Fcs;
using Meitou.Simulation;
using Meitou.Simulation.Items;

namespace KenshiOrigins;

/// <summary>Delivers only into storage grids. Undelivered quantities remain in the origin save data.</summary>
internal sealed class OriginEquipment(ItemFactory factory, Action<string> log)
{
    private readonly HashSet<string> _missing = new(StringComparer.Ordinal);

    internal OriginItem[] Deliver(CharacterCold character, IReadOnlyList<OriginItem> pending)
    {
        if (character.Inventory is not { } inventory || !character.IsPlayer || character.Medical?.Dead == true)
            return pending.ToArray();
        var remaining = new List<OriginItem>();
        foreach (var grant in pending)
        {
            // A removed mod item must not prevent the rest of the pack's equipment from arriving.
            if (factory.Db.Find(grant.Record)?.Type is not (FcsRecordType.Item or FcsRecordType.Weapon or FcsRecordType.Crossbow
                or FcsRecordType.Armour or FcsRecordType.Container or FcsRecordType.Blueprint or FcsRecordType.MapItem or FcsRecordType.LimbReplacement)
                || factory.Create(grant.Record, grant.Quantity, grant.Quality) is not { } item)
            {
                if (_missing.Add(grant.Record)) log($"Origin item '{grant.Record}' is unavailable or is not an item; skipped.");
                continue;
            }
            if (item.Type == FcsRecordType.Weapon)
            {
                // The same grade lookup used by the engine's item-giving tool; no debug command runs.
                if (DebugGiveItem.Grade(factory.Db, grant.Quality) is { } grade)
                    (item.CompanyId, item.MaterialId, item.Level) = ("PLAYER_WEAPONS", grade.Model, grade.Level);
                else item.Level = grant.Quality;
            }
            // Add can place part of a stack before returning false. Only its remainder is retried.
            if (!inventory.Add(item, InventoryCommands.MainClosed(character)))
                remaining.Add(grant with { Quantity = item.Quantity });
        }
        return remaining.ToArray();
    }
}
