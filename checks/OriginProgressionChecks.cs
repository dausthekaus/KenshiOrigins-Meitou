using KenshiOrigins;
using Meitou.Data.Gameplay;
using Meitou.Data.Gameplay.Bodies;
using Meitou.Simulation;
using Meitou.Simulation.Bodies;

internal static class OriginProgressionChecks
{
    internal static void Run(Action<bool, string> check)
    {
        var constants = GameConstants.Default;
        var definition = new OriginDefinition("checks.cap", "Cap fixture", "", new Dictionary<StatsEnumerated, float>
        {
            [StatsEnumerated.Toughness] = 25, [StatsEnumerated.Strength] = 75,
            [StatsEnumerated.Medic] = 25, [StatsEnumerated.MassCombat] = 25,
            [StatsEnumerated.Lockpicking] = 25, [StatsEnumerated.Dodge] = 0
        }, [], "Testing") { TestingOnly = true };
        var system = new OriginSystem(new OriginCatalog([definition]), RngPurpose.Test, _ => { }, constants: constants);
        using var world = new World(new WorldSettings { Seed = 123, PublishSnapshots = false },
            new OpenGroundWalkability((_, _) => 0), [system]);
        var stats = new CharacterStats();
        foreach (var stat in definition.Bonuses.Keys) stats[stat] = 70;
        stats[StatsEnumerated.Strength] = 90;
        var id = world.Characters.Spawn(new CharacterHot { Health = 100 },
            new CharacterCold { Name = "Cap fixture", IsPlayer = true, Stats = stats }, 0);
        world.SaveFields.SetString(id, Meitou.Simulation.Saving.CharacterSaveRecord.Stats, OriginSystem.SavedOriginKey, definition.Id);
        system.Ready(world, true);
        check(stats.Strength == 100 && stats.Stored(StatsEnumerated.Strength) == 90,
            "testing origins are capped too, without changing trained stats");
        stats.Gain(StatsEnumerated.Strength, 20);
        check(stats.Strength == 100 && stats.Stored(StatsEnumerated.Strength) == 90,
            "XP stops at the boosted 100 cap rather than pushing the stat past it");

        float expected = CharacterStats.CoreGain(95, 1);
        stats.Gain(StatsEnumerated.Toughness, 1);
        check(Near(stats.Toughness, expected) && stats.Stored(StatsEnumerated.Toughness) < 71,
            "real bound XP gains at trained 70 plus 25 match the vanilla curve at 95");
        stats[StatsEnumerated.Toughness] = 70;
        stats.XpRate = 2;
        stats.Gain(StatsEnumerated.Toughness, 1);
        check(Near(stats.Toughness, CharacterStats.CoreGain(95, 2)), "boosted XP difficulty preserves the engine's XP rate setting");
        stats.XpRate = 1;
        stats[StatsEnumerated.Toughness] = 70;
        float[] rates = Enumerable.Repeat(-1f, StatsInfo.Count).ToArray();
        rates[(int)StatsEnumerated.Toughness] = 3;
        stats.SkillXpRates = rates;
        stats.Gain(StatsEnumerated.Toughness, 1);
        check(Near(stats.Toughness, CharacterStats.CoreGain(95, 3)), "per-skill XP settings still apply before boosted difficulty");
        stats.SkillXpRates = null;

        var race = new RaceData();
        var xp = new XpService(constants, BodyOptions.Default);
        race.StatMultipliers[(int)StatsEnumerated.Lockpicking] = 1.2f;
        xp.Lockpicking(stats, race, true);
        check(Near(stats[StatsEnumerated.Lockpicking], CharacterStats.CoreGain(95, 1.2f * 1.2f)),
            "racial XP multipliers survive the origin curve adjustment");
        var comparison = new CharacterStats();
        comparison[StatsEnumerated.Medic] = 95;
        xp.Medic(comparison, race, race, 10, StatsEnumerated.Medic);
        xp.Medic(stats, race, race, 10, StatsEnumerated.Medic);
        check(Near(stats[StatsEnumerated.Medic], comparison[StatsEnumerated.Medic]),
            "real medical XP uses the vanilla seconds-per-point formula at the boosted level");
        stats[StatsEnumerated.Medic] = 74.999f;
        xp.Medic(stats, race, race, 100000, StatsEnumerated.Medic);
        xp.Medic(stats, race, race, 100000, StatsEnumerated.Medic);
        check(stats[StatsEnumerated.Medic] == 100, "large direct medical gains cannot cross 100 or accumulate after reaching it");
        stats[StatsEnumerated.MassCombat] = 74.999f;
        xp.MassCombat(stats);
        xp.MassCombat(stats);
        check(stats[StatsEnumerated.MassCombat] == 100, "mass combat's straight XP path also respects 100");

        stats[StatsEnumerated.Toughness] = 70;
        check(stats.Gain(StatsEnumerated.Toughness, 1, 90) == 70,
            "a training dummy cannot train a boosted skill already above its limit");
        stats.Gain(StatsEnumerated.Toughness, 1, 100);
        check(Near(stats.Toughness, CharacterStats.CoreGain(95, 1, 100)),
            "training gains retain their own curve cap below the origin limit");
        foreach (float level in new[] { 1f, 50, 70, 74, 74.99f, 74.99999f })
        {
            stats[StatsEnumerated.Toughness] = level;
            system.Reconcile(world);
            float before = stats.Toughness;
            stats.Gain(StatsEnumerated.Toughness, 20);
            check(Near(stats.Toughness, MathF.Min(100, CharacterStats.CoreGain(before, 20))) && stats.Toughness <= 100,
                $"boosted curve and hard cap hold at effective level {before}");
        }
        stats[StatsEnumerated.Toughness] = 74;
        for (int n = 0; n < 10000; n++) stats.Gain(StatsEnumerated.Toughness, 20);
        check(stats.Toughness <= 100 && stats.Stored(StatsEnumerated.Toughness) <= 75,
            "repeated gains between reconciliations never cross the origin cap");

        stats[StatsEnumerated.Strength] = 95;
        system.Reconcile(world);
        check(stats.Strength == 100 && Own(StatsEnumerated.Strength) == 5, "external trained-stat changes shrink only the origin contribution");
        stats.Modifiers.Set("another-mod", StatsEnumerated.Strength, 3);
        system.Reconcile(world);
        check(stats.Strength == 100 && Own(StatsEnumerated.Strength) == 2, "another mod's additive bonus is included when finding room");
        stats.Modifiers.Set("another-mod", StatsEnumerated.Strength, 20);
        system.Reconcile(world);
        check(stats.Strength == 115 && Own(StatsEnumerated.Strength) == 0 && stats.Stored(StatsEnumerated.Strength) == 95,
            "an external value above 100 is preserved, with zero added by Origins");
        stats.Modifiers.RemoveAll("another-mod");
        stats[StatsEnumerated.Strength] = 110;
        system.Reconcile(world);
        check(stats.Strength == 110 && stats.Stored(StatsEnumerated.Strength) == 110 && Own(StatsEnumerated.Strength) == 0,
            "legacy trained levels above 100 are not deleted or offset by a negative modifier");
        stats[StatsEnumerated.Strength] = 1;
        system.Reconcile(world);
        check(stats.Strength == 76 && Own(StatsEnumerated.Strength) == 75, "the configured bonus returns when there is room again");
        check(!system.Reconcile(world), "an unchanged cap refresh changes no modifiers or character save fields");

        stats[StatsEnumerated.Athletics] = 95;
        stats.Gain(StatsEnumerated.Athletics, 1);
        check(stats.Athletics == CharacterStats.CoreGain(95, 1), "skills without an origin bonus keep native XP behavior");
        stats[StatsEnumerated.Dodge] = 100;
        stats.Gain(StatsEnumerated.Dodge, 1);
        check(stats[StatsEnumerated.Dodge] == CharacterStats.CoreGain(100, 1), "an explicit zero bonus does not alter native training");
        var outsider = new CharacterStats();
        outsider[StatsEnumerated.Strength] = 95;
        world.Characters.Spawn(new CharacterHot { Health = 100 }, new CharacterCold { Stats = outsider }, 0);
        outsider.Gain(StatsEnumerated.Strength, 1);
        check(outsider.Strength == CharacterStats.CoreGain(95, 1), "characters without origin modifiers keep native XP behavior");
        check(ParallelHash(1) == ParallelHash(4), "origin-adjusted XP produces the same state with one and four worker threads");

        ulong ParallelHash(int threads)
        {
            var origin = new OriginSystem(new OriginCatalog([definition]), RngPurpose.Test, _ => { });
            using var simulation = new World(new WorldSettings { Seed = 42, Threads = threads, MinPartitionSize = 1, PublishSnapshots = false },
                new OpenGroundWalkability((_, _) => 0), [origin, new Training()]);
            for (int n = 0; n < 16; n++)
            {
                var trainee = new CharacterStats();
                trainee[StatsEnumerated.Toughness] = 70 + n * 0.1f;
                var traineeId = simulation.Characters.Spawn(new CharacterHot { Health = 100 },
                    new CharacterCold { Stats = trainee }, 0);
                simulation.SaveFields.SetString(traineeId, Meitou.Simulation.Saving.CharacterSaveRecord.Stats,
                    OriginSystem.SavedOriginKey, definition.Id);
            }
            origin.Ready(simulation, true);
            simulation.RunTicks(20);
            return simulation.StateHash();
        }

        float Own(StatsEnumerated stat) => stats.Modifiers.All.Single(m => m.Key == OriginSystem.ModifierKey && m.Stat == stat).Amount;
        static bool Near(float a, float b) => MathF.Abs(a - b) < 0.00002f;
    }

    private sealed class Training : ITickSystem
    {
        public void Act(World world, Partition partition, EffectBuffer effects)
        {
            for (int slot = partition.Start; slot < partition.End; slot++)
                world.Characters.Cold(slot)?.Stats?.Gain(StatsEnumerated.Toughness, 1);
        }
    }
}
