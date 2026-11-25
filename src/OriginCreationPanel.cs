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

/// <summary>Race/origin mode switch, live search, origin selector, horizontal tags, and a scrollable description.</summary>
internal static class OriginCreationPanel
{
    internal const string ModeButton = "kenshiorigins.mode";
    internal const string Search = "kenshiorigins.search";
    internal const string SearchHint = "kenshiorigins.search-hint";
    internal const string TagRow = "kenshiorigins.tags";
    internal const string TagButtonPrefix = "kenshiorigins.tag-";
    internal const string ResultCount = "kenshiorigins.results";
    internal const string Combo = "kenshiorigins.dropdown";
    internal const string RandomOption = "kenshiorigins:random";
    private const string Screen = "editor";

    private static OriginCatalog s_catalog = null!;
    private static EditorControl s_choice = null!;
    private static Action<string> s_log = null!;
    private static FieldInfo s_opening = null!, s_player = null!, s_session = null!, s_screens = null!, s_layout = null!, s_height = null!;
    private static PropertyInfo s_kind = null!, s_current = null!;
    private static MethodInfo s_draft = null!;
    private static readonly ConditionalWeakTable<object, PanelState> States = new();

    private sealed class PanelState
    {
        internal bool ShowOrigin = true;
        internal bool MissingPanelLogged;
        internal string Query = "";
        internal string Tag = "";
        internal CharacterId Character = CharacterId.None;
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
        s_height = Field(player, "Height");
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
            b.Hidden.UnionWith([ModeButton, Search, SearchHint, TagRow, Combo, ResultCount]);
            return;
        }
        var state = States.GetValue(opening, _ => new PanelState());
        if (!EnsureLayout(__instance, screens, state)) return;
        if (state.Character != id)
        {
            state.Character = id;
            screens.SetScroll(Screen, "DescriptionPanelText", 0);
        }
        var draft = s_draft.Invoke(__instance, [id]) as CharacterDraft;
        OriginView? origin = null;
        string tags = "";
        if (draft is not null)
        {
            int choice = system.ReadChoice(draft.Record);
            origin = choice == s_catalog.All.Count
                ? new OriginView("Random", "Choose a random origin when you confirm character creation. Testing-only origins are excluded.",
                    "Determined by the chosen origin", true, "Varies", "Determined by the chosen origin")
                : system.Preview(s_catalog.All[choice]);
            if (choice < s_catalog.All.Count) tags = string.Join(", ", s_catalog.All[choice].Tags);
            var matches = OriginBrowser.Filter(s_catalog, state.Query, state.Tag);
            var options = matches.Select(o => new UiListItem(o.Id, o.Name, Colour: o.TestingOnly ? OriginUi.TestingInk : null))
                .Append(new UiListItem(RandomOption, "Random")).ToArray();
            b.Lists[Combo] = options;
            string selectedId = choice == s_catalog.All.Count ? RandomOption : s_catalog.All[choice].Id;
            int selectedIndex = Array.FindIndex(options, option => option.Id == selectedId);
            b.SelectedItems[Combo] = selectedIndex;
            // A filter may exclude the saved choice. Keep its caption and draft until the player selects another.
            b.Text[Combo] = origin.Name;
            b.Text[ResultCount] = matches.Count == 0 ? "No matches; choice kept" : $"{matches.Count} origins" + (selectedIndex < 0 ? "; choice kept" : "");
        }
        else if (system.Views.TryGetValue(id, out var pending)) origin = pending;

        // Host selection caches outlive character switches; the current draft is authoritative.
        screens.ClearSelection(Screen, Combo);
        b.Hidden.Add("lbRaceDescription");
        // The screen router disables buttons before the editor supplies its bindings.
        b.Disabled.Remove(ModeButton);
        b.Actions[ModeButton] = ModeButton;
        b.Properties[ModeButton] = Checked(state.ShowOrigin);
        b.Text[ModeButton] = state.ShowOrigin ? "Origin" : "Race";
        b.Text[Search] = state.Query;
        b.Text[SearchHint] = "Search...";
        if (state.Query.Length > 0) b.Hidden.Add(SearchHint);
        bool choosing = state.ShowOrigin && draft is not null;
        if (!choosing) b.Hidden.UnionWith([Search, SearchHint, TagRow, Combo, ResultCount]);

        var layout = screens.Layout(Screen);
        if (origin?.TestingOnly == true)
        {
            // The fallback caption is used when browsing filters hide the selected entry.
            var client = layout.Find(Combo)?.Children.FirstOrDefault(w => w.Properties.GetValueOrDefault("TemplatePart") == "Client");
            if (client is not null) b.Properties[client.Name] = new Dictionary<string, string> { ["TextColour"] = OriginUi.TestingInk };
        }
        void Position(string name, UiRect position)
        {
            if (layout.Find(name) is { } widget) b.RelativePositions[widget] = position;
        }
        var descriptionRegion = layout.Find("DescriptionPanel")!.Position;
        if (choosing)
        {
            // Give the description the race-stat area's space while browsing origins. Race mode
            // and non-creation editors keep the original panels and geometry.
            float bottom = layout.Find("StatsPanel") is { RealPosition: true } raceStats
                ? raceStats.Position.Y + raceStats.Position.Height : descriptionRegion.Y + descriptionRegion.Height;
            descriptionRegion = descriptionRegion with { Width = Math.Max(descriptionRegion.Width, .255f), Height = bottom - descriptionRegion.Y };
            Position("DescriptionPanel", descriptionRegion);
            b.Hidden.Add("StatsPanel");
        }
        if (!choosing) Position(ModeButton, new(.025f, .02f, .35f, .15f));
        if (choosing)
        {
            // Headers keep the same readable size as the panel grows; remaining height goes to the story.
            float height = Math.Max(1, (int)s_height.GetValue(s_player.GetValue(__instance))! * descriptionRegion.Height);
            float scale = b.TemplateScale;
            float Unit(float pixels) => pixels * scale / height;
            Position(ModeButton, new(.025f, Unit(8), .285f, Unit(30)));
            Position(Search, new(.335f, Unit(8), .64f, Unit(30)));
            Position(SearchHint, new(.36f, Unit(8), .55f, Unit(30)));
            Position(Combo, new(.025f, Unit(44), .95f, Unit(30)));
            Position(TagRow, new(.025f, Unit(80), .95f, Unit(46)));
            Position(ResultCount, new(.035f, Unit(132), .93f, Unit(18)));
            Position("DescriptionPanelText", new(.025f, Unit(156), .95f, Math.Max(0, 1 - Unit(164))));
            var categories = Categories();
            float totalWidth = categories.Sum(TagWidth) + 6 * (categories.Length - 1);
            // Only the horizontal axis scrolls. Let the skin supply the client height so rounding
            // cannot create a tiny hidden vertical range that steals the mouse wheel.
            b.Canvas[TagRow] = (totalWidth * scale, 0);
            float x = 0;
            for (int index = 0; index < categories.Length; index++)
            {
                string name = TagButtonPrefix + index;
                string tag = categories[index];
                b.Disabled.Remove(name);
                b.Actions[name] = name;
                b.Text[name] = tag;
                b.Properties[name] = Checked(index == 0 ? state.Tag.Length == 0 : state.Tag == tag);
                Position(name, new(x / totalWidth, 0, TagWidth(tag) / totalWidth, 1));
                x += TagWidth(tag) + 6;
            }
        }

        var body = layout.Find("DescriptionPanelText");
        if (body is null) return;
        if (!choosing) b.RelativePositions[body] = new(.025f, .19f, .95f, .78f);
        if (state.ShowOrigin && origin is not null)
        {
            if (origin.TestingOnly)
                foreach (var text in body.DescendantsAndSelf().Where(w => w.Name == body.Name
                    || w.Properties.GetValueOrDefault("CaptionOwner") == body.Name))
                    b.Properties[text.Name] = new Dictionary<string, string> { ["TextColour"] = OriginUi.TestingInk };
            b.Text["DescriptionPanelText"] = $"{origin.Name}\nDifficulty: {origin.Difficulty}" + (tags.Length > 0 ? $"\nTags: {tags}" : "")
                + (origin.TestingOnly ? "\nTESTING ONLY — manual selection; excluded from random origins." : "")
                + $"\n\n{origin.Description}\n\nBonuses\n{origin.Bonuses}\n\nStarting items\n{origin.Items}";
        }
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
                return widget with { Children = [.. widget.Children,
                    new MyGuiWidget("Button", ModeButton, "Kenshi_Button1", "", "Left Top", new(.025f, .02f, .285f, .095f), true,
                        new Dictionary<string, string> { ["TextAlign"] = "Center VCenter" }, new Dictionary<string, string>(), []),
                    new MyGuiWidget("EditBox", Search, "Kenshi_EditBox", "", "Left Top", new(.335f, .02f, .64f, .095f), true,
                        new Dictionary<string, string> { ["MultiLine"] = "false", ["MaxTextLength"] = "100", ["SubmitAction"] = "search" },
                        new Dictionary<string, string>(), []),
                    Label(SearchHint, new(.36f, .02f, .55f, .095f)),
                    Dropdown(Combo, new(.025f, .135f, .95f, .095f)),
                    new MyGuiWidget("ScrollView", TagRow, "Kenshi_ScrollViewEmpty", "", "Left Top", new(.025f, .25f, .95f, .14f), true,
                        new Dictionary<string, string> { ["VisibleVScroll"] = "false", ["VisibleHScroll"] = "true", ["CanvasAlign"] = "Left Top" },
                        new Dictionary<string, string>(), Categories().Select((tag, index) =>
                            new MyGuiWidget("Button", TagButtonPrefix + index, "Kenshi_Button2", "", "Left Top", new(0, 0, 1, 1), true,
                                new Dictionary<string, string> { ["TextAlign"] = "Center VCenter" }, new Dictionary<string, string>(), [])).ToArray()),
                    Label(ResultCount, new(.035f, .40f, .93f, .055f))] };
            var children = widget.Children.Select(Visit).ToArray();
            // BindEditor has already positioned the body controls using widget-keyed bindings.
            // Preserve those objects; only the description panel and its ancestors need copies.
            return children.Where((child, index) => !ReferenceEquals(child, widget.Children[index])).Any()
                ? widget with { Children = children } : widget;
        }
        return layout with { Roots = layout.Roots.Select(Visit).ToArray() };
    }

    private static MyGuiWidget Label(string name, UiRect position) => new("TextBox", name, "Kenshi_TextboxStandardText", "", "Left Top", position,
        true, new Dictionary<string, string> { ["NeedMouse"] = "false", ["TextAlign"] = "Left VCenter" }, new Dictionary<string, string>(), []);

    private static MyGuiWidget Dropdown(string name, UiRect position) => new("ComboBox", name, "Kenshi_ComboBox", "", "Left Top", position, true,
        // The template's Client paints the selected text. The owner still supplies a fallback
        // caption for filtered-out selections, but must never paint that caption a second time.
        new Dictionary<string, string> { ["ReadOnly"] = "true", ["MaxListLength"] = "240", ["SmoothShow"] = "false", ["SkipCaption"] = "true" },
        new Dictionary<string, string>(), []);

    private static string[] Categories() => ["All tags", .. OriginBrowser.Tags(s_catalog)];
    private static float TagWidth(string tag) => Math.Max(56, tag.Length * 7 + 16);

    /// <summary>Selection writes straight to the current draft; no secondary Apply/Save action exists.</summary>
    private static bool Action(object __instance, MyGuiEvent e)
    {
        if (e.Screen != Screen || !Context(__instance, out var opening, out var screens, out _, out var id)) return true;
        var state = States.GetValue(opening, _ => new PanelState());
        if (e.Widget == ModeButton)
        {
            state.ShowOrigin = !state.ShowOrigin;
            screens.Blur();
            screens.SetScroll(Screen, "DescriptionPanelText", 0);
            return false;
        }
        if (e.Widget == Search)
        {
            if (e.Action is "text" or "search")
            {
                state.Query = e.Text;
                screens.SetScroll(Screen, Combo + "@popup", 0);
            }
            return false;
        }
        if (e.Widget.StartsWith(TagButtonPrefix, StringComparison.Ordinal))
        {
            var categories = Categories();
            if (int.TryParse(e.Widget.AsSpan(TagButtonPrefix.Length), out int tagIndex) && (uint)tagIndex < categories.Length)
            {
                state.Tag = tagIndex == 0 ? "" : categories[tagIndex];
                screens.SetScroll(Screen, Combo + "@popup", 0);
            }
            return false;
        }
        if (e.Widget != Combo) return true;
        if (e.Action is not ("select" or "accept") || s_draft.Invoke(__instance, [id]) is not CharacterDraft draft) return false;
        int index = e.Text == RandomOption ? s_catalog.All.Count : s_catalog.IndexOf(e.Text);
        if (index >= 0)
        {
            draft.Set(s_choice, index);
            screens.SetScroll(Screen, "DescriptionPanelText", 0);
        }
        return false;
    }
}
