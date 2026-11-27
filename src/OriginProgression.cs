using Meitou.Data.Gameplay.Bodies;
using Meitou.Data.Gameplay;
using Meitou.Simulation;
using Meitou.Simulation.Bodies;

namespace KenshiOrigins;

/// <summary>Limits our additive bonus and uses the boosted level for the engine's existing XP curve.</summary>
internal static class OriginProgression
{
    internal const float Limit = 100;

    internal static float Bonus(CharacterStats stats, StatsEnumerated stat, float configured)
    {
        // Negative amounts supplied by third-party packs keep their meaning.
        if (configured <= 0) return configured;
        float baseline = stats.Stored(stat);
        foreach (var modifier in stats.Modifiers.All)
            if (modifier.Stat == stat && modifier.Key != OriginSystem.ModifierKey) baseline += modifier.Amount;
        // Never subtract trained levels or another mod's bonus, even if either already exceeds 100.
        return MathF.Min(configured, MathF.Max(0, Limit - baseline));
    }

    internal static float Gain(World world, in ExperienceGain gain, GameConstants constants)
    {
        if (!world.Characters.TryResolveNext(gain.Character, out int slot)
            || world.Characters.Cold(slot)?.Stats is not { } stats) return gain.Amount;
        var storage = StatsInfo.StorageOf(gain.Skill);
        bool boosted = false;
        foreach (var modifier in stats.Modifiers.All)
            if (modifier.Key == OriginSystem.ModifierKey && modifier.Stat == storage && modifier.Amount >= 0)
            {
                boosted = true;
                break;
            }
        if (!boosted) return gain.Amount;

        float level = stats[storage];
        float room = Limit - level;
        if (room <= 0 || level >= gain.Cap) return 0;
        // First aid and mass combat add XP directly rather than passing through CoreGain.
        if (gain.Source == ExperienceSource.FirstAid)
        {
            float Seconds(float skill) => constants.XpRateMedic1
                + (constants.XpRateMedic99 - constants.XpRateMedic1) * (skill * 0.01f);
            float seconds = Seconds(level);
            if (!(seconds > 0)) return 0;
            return MathF.Min(gain.Amount * Seconds(gain.Level) / seconds, MathF.Min(room, gain.Cap - level));
        }
        if (float.IsPositiveInfinity(gain.Cap))
            return MathF.Min(gain.Amount, MathF.Min(room, gain.Cap - level));

        // The engine still applies its curve at the stored level. Scale by the ratio so
        // the resulting increase matches that same curve evaluated at the boosted level.
        double native = (gain.Cap - gain.Level) / gain.Cap;
        native *= native;
        double boostedCurve = (gain.Cap - level) / gain.Cap;
        boostedCurve *= boostedCurve;
        if (!(native > 0)) return gain.Amount;
        double delta = Math.Min(gain.Amount * boostedCurve, Math.Min(room, gain.Cap - level));
        return (float)(delta / native);
    }
}
