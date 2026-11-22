using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Meitou.Data.Characters;
using Meitou.Data.Ui;
using Meitou.Game.Host;
using Meitou.Game.Ui;
using Meitou.Modding;
using Meitou.Simulation;
using Meitou.Simulation.Editing;

namespace KenshiOrigins;

/// <summary>Extends the existing description panel with two views and a native dropdown.</summary>
internal static class OriginCreationPanel
{
    internal const string RaceButton = "kenshiorigins.race";
    internal const string OriginButton = "kenshiorigins.origin";
    internal const string Combo = "kenshiorigins.dropdown";
    internal const string RandomOption = "kenshiorigins:random";
    private const string Screen = "editor";

    private static OriginCatalog s_catalog = null!;
    private static EditorControl s_choice = null!;
    private static Action<string> s_log = null!;
    private static FieldInfo s_opening = null!, s_player = null!, s_session = null!, s_screens = null!, s_layout = null!;
    private static PropertyInfo s_kind = null!, s_current = null!;
    private static MethodInfo s_draft = null!;
    private static readonly ConditionalWeakTable<object, PanelState> States = new();

    private sealed class PanelState
    {
        internal bool ShowOrigin = true;
        internal bool MissingPanelLogged;
    }

    internal static void Install(Harmony harmony, OriginCatalog catalog, Action<string> log)
    {
        s_catalog = catalog;
        s_log = log;
        // Use the engine's draft setter so it increments Version and Confirm includes the edit.
        var row = LookRow.Choice(EditorTab.Body, OriginSystem.ChoiceKey, "Origin", [.. catalog.All.Select(o => o.Name), "Random"]);
        s_choice = new EditorControl("Origin", EditorControlKind.ModChoice, 0, catalog.All.Count, 0) { Mod = OriginsMod.Id, Row = row };
        var game = typeof(IMod).Assembly;
        var player = game.GetType("Meitou.Game.PlayerInterface", throwOnError: true)!;
        var editor = game.GetType("Meitou.Game.Editor.CharacterEditorPanel", throwOnError: true)!;
        s_opening = Field(editor, "Editor");
        s_player = editor.GetFields(BindingFlags.Instance | BindingFlags.NonPublic).Single(f => f.FieldType == player);
        s_session = Field(player, "Session");
        s_screens = Field(player, "FaithfulScreens");
        s_layout = Field(editor, "EditorLayout");
        s_kind = AccessTools.Property(s_opening.FieldType, "Kind") ?? throw new MissingMemberException("EditorSession.Kind");
        s_current = AccessTools.Property(s_opening.FieldType, "Current") ?? throw new MissingMemberException("EditorSession.Current");
        s_draft = Method(editor, "Draft", typeof(CharacterId));
        harmony.Patch(Method(editor, "BindEditor", typeof(UiBindings)),
            postfix: new HarmonyMethod(typeof(OriginCreationPanel), nameof(Bind)));
        harmony.Patch(Method(editor, "EditorAction", typeof(MyGuiEvent)),
            prefix: new HarmonyMethod(typeof(OriginCreationPanel), nameof(Action)));
    }

    private static FieldInfo Field(Type type, string name) => AccessTools.DeclaredField(type, name)
        ?? throw new MissingFieldException(type.FullName, name);

    private static MethodInfo Method(Type type, string name, params Type[] parameters) => AccessTools.DeclaredMethod(type, name, parameters)
        ?? throw new MissingMethodException(type.FullName, name);

    private static bool Context(object editor, out object opening, out MyGuiScreenHost screens, out OriginSystem system, out CharacterId id)
    {
        opening = s_opening.GetValue(editor)!;
        screens = null!;
        system = null!;
        id = CharacterId.None;
        if (opening is null || s_kind.GetValue(opening) is not EditKind.Creation) return false;
        var player = s_player.GetValue(editor)!;
        if (s_session.GetValue(player) is not WorldSession session) return false;
        system = session.World.Systems.OfType<OriginSystem>().FirstOrDefault()!;
        if (system is null || s_screens.GetValue(player) is not MyGuiScreenHost host || !host.IsRegistered(Screen)) return false;
        screens = host;
        id = (CharacterId)s_current.GetValue(opening)!;
        return true;
    }

    private static void Bind(object __instance, UiBindings b)
    {
        if (!Context(__instance, out var opening, out var screens, out var system, out var id))
        {
            // The same screen also serves surgeons and recruits. Keep its normal description there.
            b.Hidden.UnionWith([RaceButton, OriginButton, Combo]);
            return;
        }
        var state = States.GetValue(opening, _ => new PanelState());
        if (!EnsureLayout(__instance, screens, state)) return;
        var draft = s_draft.Invoke(__instance, [id]) as CharacterDraft;
        OriginView? origin = null;
        if (draft is not null)
        {
            int choice = system.ReadChoice(draft.Record);
            origin = choice == s_catalog.All.Count
                ? new OriginView("Random", "Choose a random origin from the loaded pool when you confirm character creation.",
                    "Determined by the chosen origin", true, "Varies", "Determined by the chosen origin")
                : system.Preview(s_catalog.All[choice]);
            b.Lists[Combo] = [.. s_catalog.All.Select(o => new UiListItem(o.Id, o.Name)), new UiListItem(RandomOption, "Random")];
            b.SelectedItems[Combo] = choice;
        }
        else if (system.Views.TryGetValue(id, out var pending)) origin = pending;

        // Host selection caches outlive character switches; the current draft is authoritative.
        screens.ClearSelection(Screen, Combo);
        b.Hidden.Add("lbRaceDescription");
        // The screen router disables buttons before the editor supplies its bindings.
        b.Disabled.Remove(RaceButton);
        b.Disabled.Remove(OriginButton);
        b.Actions[RaceButton] = RaceButton;
        b.Actions[OriginButton] = OriginButton;
        b.Properties[RaceButton] = Checked(!state.ShowOrigin);
        b.Properties[OriginButton] = Checked(state.ShowOrigin);
        b.Text[RaceButton] = "Race";
        b.Text[OriginButton] = "Origin";
        if (!state.ShowOrigin || draft is null) b.Hidden.Add(Combo);

        var body = screens.Layout(Screen).Find("DescriptionPanelText");
        if (body is null) return;
        bool choosing = state.ShowOrigin && draft is not null;
        b.RelativePositions[body] = choosing ? new(.025f, .35f, .95f, .62f) : new(.025f, .18f, .95f, .79f);
        if (state.ShowOrigin && origin is not null)
            b.Text["DescriptionPanelText"] = $"{origin.Name}\nDifficulty: {origin.Difficulty}\n\n{origin.Description}\n\nBonuses\n{origin.Bonuses}\n\nStarting items\n{origin.Items}";
    }

    private static Dictionary<string, string> Checked(bool selected) => new()
    {
        ["Checked"] = selected ? "true" : "false",
        ["StateSelected"] = selected ? "true" : "false"
    };

    private static bool EnsureLayout(object editor, MyGuiScreenHost screens, PanelState state)
    {
        var current = screens.Layout(Screen);
        if (current.Find("DescriptionPanel") is null)
        {
            if (!state.MissingPanelLogged) s_log("The editor layout has no DescriptionPanel; origin controls could not be added.");
            state.MissingPanelLogged = true;
            return false;
        }
        // The engine reconstructs body rows from EditorLayout on tab/race changes. Extend that
        // template too, and preserve the current layout's rows rather than replacing the screen.
        if (s_layout.GetValue(editor) is MyGuiLayout template && template.Find(Combo) is null)
            s_layout.SetValue(editor, Extend(template));
        if (current.Find(Combo) is null) screens.Replace(Screen, Extend(current));
        return true;
    }

    private static MyGuiLayout Extend(MyGuiLayout layout)
    {
        MyGuiWidget Visit(MyGuiWidget widget)
        {
            if (widget.Name == "DescriptionPanel")
                return widget with { Children = [.. widget.Children, Button(RaceButton, .025f), Button(OriginButton, .51f), Dropdown()] };
            var children = widget.Children.Select(Visit).ToArray();
            // BindEditor has already positioned the body controls using widget-keyed bindings.
            // Preserve those objects; only the description panel and its ancestors need copies.
            return children.Where((child, index) => !ReferenceEquals(child, widget.Children[index])).Any()
                ? widget with { Children = children } : widget;
        }
        return layout with { Roots = layout.Roots.Select(Visit).ToArray() };
    }

    private static MyGuiWidget Button(string name, float left) => new("Button", name, "Kenshi_Button1", "", "Left Top",
        new(left, .02f, .465f, .135f), true, new Dictionary<string, string> { ["TextAlign"] = "Center VCenter" },
        new Dictionary<string, string>(), []);

    private static MyGuiWidget Dropdown() => new("ComboBox", Combo, "Kenshi_ComboBox", "", "Left Top",
        new(.025f, .18f, .95f, .14f), true,
        new Dictionary<string, string> { ["ReadOnly"] = "true", ["MaxListLength"] = "240" }, new Dictionary<string, string>(), []);

    /// <summary>Selection writes straight to the current draft; no secondary Apply/Save action exists.</summary>
    private static bool Action(object __instance, MyGuiEvent e)
    {
        if (e.Screen != Screen || !Context(__instance, out var opening, out _, out _, out var id)) return true;
        var state = States.GetValue(opening, _ => new PanelState());
        if (e.Widget == RaceButton || e.Widget == OriginButton)
        {
            state.ShowOrigin = e.Widget == OriginButton;
            return false;
        }
        if (e.Widget != Combo) return true;
        if (e.Action is not ("select" or "accept") || s_draft.Invoke(__instance, [id]) is not CharacterDraft draft) return false;
        int index = e.Text == RandomOption ? s_catalog.All.Count : s_catalog.IndexOf(e.Text);
        if (index >= 0) draft.Set(s_choice, index);
        return false;
    }
}
