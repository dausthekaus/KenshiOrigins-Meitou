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
    private static OriginCatalog s_catalog = null!;
    private static Action<string> s_log = null!;
    private static FieldInfo s_session = null!, s_editor = null!, s_editorPlayer = null!, s_statsPlayer = null!;
    private static FieldInfo s_width = null!, s_height = null!;
    private static PropertyInfo s_current = null!, s_kind = null!;
    private static MethodInfo s_draft = null!, s_selected = null!, s_publishPaused = null!;
    private static readonly ConditionalWeakTable<World, object> PausedFaults = new();

    internal static void Install(Harmony harmony, OriginCatalog catalog, Action<string> log)
    {
        s_catalog = catalog;
        s_log = log;
        var game = typeof(IMod).Assembly;
        var player = game.GetType("Meitou.Game.PlayerInterface", throwOnError: true)!;
        var editor = game.GetType("Meitou.Game.Editor.CharacterEditorPanel", throwOnError: true)!;
        var stats = game.GetType("Meitou.Game.Ui.StatsWindow", throwOnError: true)!;
        s_session = RequireField(player, "Session", typeof(WorldSession));
        s_width = RequireField(player, "Width", typeof(int));
        s_height = RequireField(player, "Height", typeof(int));
        s_editor = AccessTools.DeclaredField(editor, "Editor") ?? throw new MissingFieldException(editor.FullName, "Editor");
        s_editorPlayer = OwnerField(editor, player);
        s_statsPlayer = OwnerField(stats, player);
        s_current = RequireProperty(s_editor.FieldType, "Current", typeof(CharacterId));
        s_kind = RequireProperty(s_editor.FieldType, "Kind", typeof(EditKind));
        s_draft = RequireMethod(editor, "Draft", typeof(CharacterId));
        s_selected = RequireMethod(player, "Selected");
        s_publishPaused = RequireMethod(typeof(World), "PublishPaused");

        harmony.Patch(RequireMethod(editor, "AddEditorContent", typeof(List<UiControl>)),
            postfix: new HarmonyMethod(typeof(OriginUi), nameof(EditorPreview)));
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

    private static PropertyInfo RequireProperty(Type type, string name, Type expected)
    {
        var property = AccessTools.Property(type, name);
        return property?.PropertyType == expected ? property : throw new MissingMemberException(type.FullName, name);
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

    private static void EditorPreview(object __instance, List<UiControl> controls)
    {
        var opening = s_editor.GetValue(__instance);
        if (opening is null || s_kind.GetValue(opening) is not EditKind.Creation) return;
        var player = s_editorPlayer.GetValue(__instance)!;
        if (s_session.GetValue(player) is not WorldSession session) return;
        var system = session.World.Systems.OfType<OriginSystem>().FirstOrDefault();
        if (system is null) return;

        var id = (CharacterId)s_current.GetValue(opening)!;
        OriginView? view = null;
        if (s_draft.Invoke(__instance, [id]) is CharacterDraft draft)
        {
            var origin = s_catalog.All[system.ReadChoice(draft.Record)];
            view = new OriginView(origin.Name, origin.Description, origin.BonusSummary, true);
        }
        else if (system.Views.TryGetValue(id, out var pending)) view = pending;
        if (view is null) return;

        float width = (int)s_width.GetValue(player)!, height = (int)s_height.GetValue(player)!;
        float left = controls.FirstOrDefault(c => c.Name == "NamePanel")?.Bounds is { } names ? names.X + names.Width : width * .162f;
        float right = controls.FirstOrDefault(c => c.Name == "TabsPanel")?.Bounds.X ?? width * .7f;
        float boxWidth = Math.Min(420, right - left - 16);
        if (boxWidth < 160 || height < 300) return;
        var box = new UiRect(left + (right - left - boxWidth) / 2, height - 228, boxWidth, 112);
        AddText(controls, "preview", box, $"Origin: {view.Name}\n{view.Bonuses}\n{view.Description}\nBonuses apply after Confirm.", 14,
            new Vector4(.07f, .065f, .05f, .9f));
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
        AddText(controls, "stats", box, $"Origin: {view.Name}\n{view.Bonuses}", Math.Max(8, (int)(13 * scale)), Vector4.Zero,
            panel.Clip ?? panel.Bounds);
    }

    private static void AddText(List<UiControl> controls, string name, UiRect box, string text, int pixels, Vector4 background, UiRect? clip = null)
    {
        controls.Add(new UiControl($"kenshiorigins.{name}", "TextBox", box, text, "", "", true,
            new Dictionary<string, string>
            {
                ["TextAlign"] = "Left Top",
                ["WordWrap"] = "true",
                ["FontPixels"] = pixels.ToString(CultureInfo.InvariantCulture),
                ["TextColour"] = "0.95 0.90 0.75"
            }, background, Pickable: false, Clip: clip ?? box));
    }
}
