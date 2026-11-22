using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Meitou.Data;
using Meitou.Data.Characters;
using Meitou.Data.Ui;
using Meitou.Game.Host;
using Meitou.Modding;
using Meitou.Simulation;
using Meitou.Simulation.Editing;

namespace KenshiOrigins;

/// <summary>Version-specific UI bridges. Rendering uses editor drafts or published views, never live character stats.</summary>
internal static class OriginUi
{
    internal const string OriginStat = "kenshiorigins.origin";
    private static Action<string> s_log = null!;
    private static FieldInfo s_session = null!, s_statsPlayer = null!, s_selectedStat = null!, s_uiResources = null!;
    private static MethodInfo s_selected = null!, s_publishPaused = null!;
    private static readonly ConditionalWeakTable<World, object> PausedFaults = new();

    internal static void Install(Harmony harmony, OriginCatalog catalog, Action<string> log)
    {
        s_log = log;
        var game = typeof(IMod).Assembly;
        var player = game.GetType("Meitou.Game.PlayerInterface", throwOnError: true)!;
        var stats = game.GetType("Meitou.Game.Ui.StatsWindow", throwOnError: true)!;
        s_session = RequireField(player, "Session", typeof(WorldSession));
        s_uiResources = RequireField(player, "UiResources", typeof(MyGuiResources));
        s_statsPlayer = OwnerField(stats, player);
        s_selectedStat = RequireField(stats, "SelectedStat", typeof(string));
        s_selected = RequireMethod(player, "Selected");
        s_publishPaused = RequireMethod(typeof(World), "PublishPaused");

        OriginCreationPanel.Install(harmony, catalog, log);
        harmony.Patch(RequireMethod(stats, "AddStatsContent", typeof(List<UiControl>)),
            postfix: new HarmonyMethod(typeof(OriginUi), nameof(StatsContent)));
        harmony.Patch(RequireMethod(typeof(World), nameof(World.ApplyPausedInterfaceCommands)),
            postfix: new HarmonyMethod(typeof(OriginUi), nameof(AfterPausedCommands)));
        // The engine caches a different look for each race/gender pair. Origin is character data,
        // so returning to an old look must not restore that look's older origin selection.
        foreach (var method in new[]
        {
            RequireMethod(typeof(CharacterDraft), "SwitchLook", typeof(GameRecord), typeof(bool), typeof(int)),
            RequireMethod(typeof(CharacterDraft), nameof(CharacterDraft.Reset)),
            RequireMethod(typeof(CharacterDraft), nameof(CharacterDraft.Import), typeof(Meitou.Data.Fcs.FcsRecord))
        })
            harmony.Patch(method, prefix: new HarmonyMethod(typeof(OriginUi), nameof(KeepOrigin)),
                postfix: new HarmonyMethod(typeof(OriginUi), nameof(RestoreOrigin)));
    }

    private static void KeepOrigin(CharacterDraft __instance, out int? __state)
    {
        string key = ModLooks.KeyOf(OriginsMod.Id, OriginSystem.ChoiceKey);
        __state = __instance.Creation && __instance.Record.Ints.TryGetValue(key, out int choice) ? choice : null;
    }

    private static void RestoreOrigin(CharacterDraft __instance, int? __state)
    {
        if (__state is { } choice) __instance.Record.Ints[ModLooks.KeyOf(OriginsMod.Id, OriginSystem.ChoiceKey)] = choice;
    }

    private static FieldInfo RequireField(Type type, string name, Type expected)
    {
        var field = AccessTools.DeclaredField(type, name);
        return field?.FieldType == expected ? field : throw new MissingFieldException(type.FullName, name);
    }

    private static FieldInfo OwnerField(Type container, Type player) => container.GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
        .Single(f => f.FieldType == player);

    private static MethodInfo RequireMethod(Type type, string name, params Type[] parameters) =>
        AccessTools.DeclaredMethod(type, name, parameters) ?? throw new MissingMethodException(type.FullName, name);

    /// <summary>The default ITickSystem.Inputs callback is not run for arbitrary mods during paused commands.</summary>
    private static void AfterPausedCommands(World __instance)
    {
        if (PausedFaults.TryGetValue(__instance, out _)) return;
        var system = __instance.Systems.OfType<OriginSystem>().FirstOrDefault();
        if (system is null) return;
        try
        {
            if (system.Reconcile(__instance)) s_publishPaused.Invoke(__instance, null);
        }
        catch (Exception e)
        {
            PausedFaults.Add(__instance, new object());
            s_log($"Paused origin update stopped: {e.GetBaseException().Message}");
        }
    }

    private static void StatsContent(object __instance, List<UiControl> controls)
    {
        var player = s_statsPlayer.GetValue(__instance)!;
        if (s_session.GetValue(player) is not WorldSession session
            || s_selected.Invoke(player, null) is not IEnumerable<CharacterSnapshot> selected) return;
        var lead = selected.FirstOrDefault();
        var system = session.World.Systems.OfType<OriginSystem>().FirstOrDefault();
        if (lead is null || system is null || !system.Views.TryGetValue(lead.Id, out var view)) return;
        if (controls.FirstOrDefault(c => c.Name == "AttributesPanel") is not { } panel) return;

        // Use the free area below the four attributes, staying inside the game's panel and its clip.
        float scale = panel.Bounds.Width / 245f;
        float top = controls.Where(c => c.Name.StartsWith("AttributesPanel-", StringComparison.Ordinal))
            .Select(c => c.Bounds.Y + c.Bounds.Height).DefaultIfEmpty(panel.Bounds.Y + 80 * scale).Max() + 6 * scale;
        float available = panel.Bounds.Y + panel.Bounds.Height - top;
        if (available < 24 * scale) return;
        var box = new UiRect(panel.Bounds.X + 4 * scale, top, panel.Bounds.Width - 8 * scale, available);
        int pixels = Math.Max(8, (int)(13 * scale));
        string ink = MyGuiColours.TemplateInk((MyGuiResources)s_uiResources.GetValue(player)!, "Kenshi_GenericTextBoxSkin");
        var clip = panel.Bounds.Intersect(panel.Clip ?? panel.Bounds);
        var heading = box with { Height = Math.Min(20 * scale, box.Height) };
        AddText(controls, "stats", heading, $"Origin: {view.Name}", pixels, Vector4.Zero, clip, ink);
        // Native stat actions support both hover and click, including nonnumeric mod IDs.
        controls[^1] = controls[^1] with { Type = "Button", Action = "stat:" + OriginStat, Pickable = true };
        AddText(controls, "stats-summary", box with { Y = box.Y + heading.Height, Height = box.Height - heading.Height },
            $"{view.Bonuses}\nHover or click Origin for its story.", pixels, Vector4.Zero, clip, ink);

        // An empty selection is the screen's normal opening state. Real skill selections keep
        // their own description; our entry uses the same selection mechanism to return here.
        string selectedStat = (string)s_selectedStat.GetValue(__instance)!;
        if (selectedStat.Length > 0 && selectedStat != OriginStat) return;
        if (controls.FirstOrDefault(c => c.Name == "Description1Panel") is not { } left
            || controls.FirstOrDefault(c => c.Name == "Description2Panel") is not { } right) return;
        string waiting = view.ItemsWaiting > 0 ? $"\n{view.ItemsWaiting} item(s) waiting for inventory space." : "";
        AddOriginDescription(controls, left, $"Origin: {view.Name}\nDifficulty: {view.Difficulty}\n\nBonuses\n{view.Bonuses}\n\nStarting items\n{view.Items}{waiting}",
            scale, "stats-bonuses", ink);
        AddOriginDescription(controls, right, view.Description, scale, "stats-story", ink);
    }

    private static void AddOriginDescription(List<UiControl> controls, UiControl panel, string text, float scale, string name, string ink)
    {
        float inset = 12 * scale;
        var box = new UiRect(panel.Bounds.X + inset, panel.Bounds.Y + 8 * scale,
            Math.Max(0, panel.Bounds.Width - 2 * inset), Math.Max(0, panel.Bounds.Height - 16 * scale));
        AddText(controls, name, box, text, Math.Max(8, (int)(14 * scale)), Vector4.Zero,
            panel.Bounds.Intersect(panel.Clip ?? panel.Bounds), ink);
    }

    private static void AddText(List<UiControl> controls, string name, UiRect box, string text, int pixels, Vector4 background, UiRect clip, string ink)
    {
        controls.Add(new UiControl($"kenshiorigins.{name}", "TextBox", box, text, "", "", true,
            new Dictionary<string, string>
            {
                ["TextAlign"] = "Left Top",
                ["WordWrap"] = "true",
                ["FontPixels"] = pixels.ToString(CultureInfo.InvariantCulture),
                ["TextColour"] = ink
            }, background, Pickable: false, Clip: clip));
    }
}
