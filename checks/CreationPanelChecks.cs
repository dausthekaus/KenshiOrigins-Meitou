using System.Numerics;
using System.Reflection;
using HarmonyLib;
using KenshiOrigins;
using Meitou.Data;
using Meitou.Data.Characters;
using Meitou.Data.Ui;
using Meitou.Game.Host;
using Meitou.Game.Ui;
using Meitou.Simulation;
using Meitou.Simulation.Editing;

internal static class CreationPanelChecks
{
    internal static void Run(OriginCatalog catalog, GameDatabase database, WorldSession session, object player, object editor,
        CharacterDraft draft, CharacterId character, string? install, Action<bool, string> check)
    {
        if (install is null)
        {
            Console.WriteLine("SKIP: native creation layout checks need --ui-root <Kenshi directory>.");
            return;
        }
        var playerType = player.GetType();
        var editorType = editor.GetType();
        string gui = Path.Combine(install, "data/gui");
        var files = Directory.EnumerateFiles(gui, "*", SearchOption.AllDirectories).Order(StringComparer.Ordinal)
            .GroupBy(p => Path.GetFileName(p), StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
        string? Resolve(string name) => File.Exists(Path.Combine(gui, name)) ? Path.Combine(gui, name) : files.GetValueOrDefault(Path.GetFileName(name));
        var resources = MyGuiResources.Load("kenshi_init.xml", Resolve, "kenshi_fonts.xml");
        var layout = MyGuiReader.ReadLayout(files["Kenshi_CharacterEditor.layout"]);
        var screens = new MyGuiScreenHost(resources);
        screens.Register("editor", layout, modal: true);
        screens.Open("editor");
        AccessTools.Field(playerType, "UiResources").SetValue(player, resources);
        AccessTools.Field(playerType, "FaithfulScreens").SetValue(player, screens);
        AccessTools.Field(editorType, "EditorLayout").SetValue(editor, screens.Layout("editor"));
        AccessTools.Field(editorType, "EditorRowsShape").SetValue(editor, "");
        var original = screens.Layout("editor");
        var extended = (MyGuiLayout)AccessTools.Method(typeof(OriginCreationPanel), "Extend").Invoke(null, [original])!;
        check(ReferenceEquals(original.Find("TabsPanel"), extended.Find("TabsPanel")),
            "extending the description preserves the body controls and their widget-keyed bindings");

        var other = new CharacterId(777, 1);
        var openingField = AccessTools.Field(editorType, "Editor");
        object Opening(EditKind kind) => Activator.CreateInstance(openingField.FieldType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null,
            [kind, new[] { character, other }, new HashSet<CharacterId>(), CharacterId.None, false], null)!;
        var opening = Opening(EditKind.Creation);
        openingField.SetValue(editor, opening);
        var drafts = (Dictionary<CharacterId, CharacterDraft>)AccessTools.Property(opening.GetType(), "Drafts").GetValue(opening)!;
        drafts[character] = draft;
        var second = new CharacterDraft(database, install, CharacterDraft.Copy(draft.Record), "Other") { Creation = true };
        string originKey = ModLooks.KeyOf(OriginsMod.Id, OriginSystem.ChoiceKey);
        second.Record.Ints[originKey] = 1;
        drafts[other] = second;
        var action = AccessTools.Method(editorType, "EditorAction");
        // Exercise the same default-disabled buttons and event routing as the running game.
        var router = AccessTools.Property(playerType, "ScreenRouter").GetValue(player)!;
        var bind = AccessTools.Method(router.GetType(), "BindFaithfulScreen");
        var route = AccessTools.Method(router.GetType(), "FaithfulAction");
        screens.Activated += e => route.Invoke(router, [e]);
        UiBindings? bindings = null;
        bool measureText = false;
        IReadOnlyList<UiControl> Frame(int width = 1280, int height = 900)
        {
            AccessTools.Field(playerType, "Width").SetValue(player, width);
            AccessTools.Field(playerType, "Height").SetValue(player, height);
            return screens.Arrange(width, height, _ =>
            {
                bindings = (UiBindings)bind.Invoke(router, ["editor"])!;
                // A headless stand-in for the painter's font measurement, to exercise native scrolling.
                if (measureText) bindings.TextHeight = c => c.Caption.Split('\n').Sum(line =>
                    Math.Max(1, (int)Math.Ceiling(line.Length / Math.Max(1, c.Bounds.Width / 7)))) * 16;
                return bindings;
            }).Single().Controls;
        }
        void Click(UiControl control)
        {
            screens.PointerDown(new Vector2(control.Bounds.X + control.Bounds.Width / 2, control.Bounds.Y + control.Bounds.Height / 2));
            screens.PointerUp();
        }
        UiControl Toggle(IReadOnlyList<UiControl> controls) => controls.First(c => c.Action == $"mygui:combo:{OriginCreationPanel.Combo}:toggle");

        var frame = Frame();
        check(screens.Layout("editor").Find(OriginCreationPanel.Combo)!.Properties.GetValueOrDefault("ResolvedTemplate") == "true",
            "origin dropdown expands the game's native combo skin");
        check(bindings!.Lists[OriginCreationPanel.Combo].Count == catalog.All.Count + 1, "native dropdown lists the loaded origin pool plus Random");
        check(bindings.Text["DescriptionPanelText"].Contains(catalog.All[2].Description), "existing race-description text area now shows the selected origin");
        check(bindings.Text["DescriptionPanelText"].Contains("Difficulty: " + catalog.All[2].Difficulty),
            "creation displays the origin's informational difficulty string");
        check(bindings.Text["DescriptionPanelText"].Contains("Starting items"), "creation previews starting equipment from the origin definition");
        check(!frame.Any(c => c.Name == "kenshiorigins.preview"), "floating origin preview has been removed");
        check(!draft.Controls(EditorTab.Body).Any(c => c.Mod == OriginsMod.Id), "origin selector is absent from the body slider list");

        // The second frame includes our injected buttons in the router's disabled-by-default set.
        frame = Frame();
        check(frame.Single(c => c.Action == OriginCreationPanel.RaceButton).Enabled
            && frame.Single(c => c.Action == OriginCreationPanel.OriginButton).Enabled,
            "Race and Origin remain enabled after the screen router applies its defaults");

        int before = draft.Version;
        Click(Toggle(frame));
        frame = Frame();
        var option = frame.First(c => c.Caption == catalog.All[0].Name && c.Action.StartsWith("mygui:combo:", StringComparison.Ordinal)
            && c.Action != $"mygui:combo:{OriginCreationPanel.Combo}:toggle");
        Click(option);
        Frame();
        check(draft.Record.Ints[originKey] == 0 && draft.Version > before, "real popup selection immediately saves and marks the current draft changed");
        check(bindings!.Text["DescriptionPanelText"].Contains(catalog.All[0].BonusSummary), "description and bonuses refresh on selection without Apply");

        action.Invoke(editor, [new MyGuiEvent("editor", "FaceTabButton", "FaceTabButton")]);
        frame = Frame();
        check(screens.Layout("editor").Widgets.Count(w => w.Name == OriginCreationPanel.Combo) == 1,
            "body-row reconstruction preserves exactly one origin dropdown");
        Click(frame.First(c => c.Action == OriginCreationPanel.RaceButton));
        frame = Frame();
        check(bindings!.Text["DescriptionPanelText"] == draft.Race.GetString("description"), "Race toggle restores the original race description");
        check(!frame.Any(c => c.Action == $"mygui:combo:{OriginCreationPanel.Combo}:toggle"), "dropdown is hidden in Race view");
        Click(frame.First(c => c.Action == OriginCreationPanel.OriginButton));
        frame = Frame();
        check(bindings!.SelectedItems[OriginCreationPanel.Combo] == 0, "switching views retains the saved choice");

        AccessTools.Field(opening.GetType(), "Index").SetValue(opening, 1);
        frame = Frame();
        check(bindings!.SelectedItems[OriginCreationPanel.Combo] == 1, "switching characters restores their own choice instead of the combo cache");
        Click(Toggle(frame));
        Frame();
        check(screens.ListKey(UiListKey.End), "native dropdown supports keyboard navigation");
        Frame();
        check(second.Record.Ints[originKey] == catalog.All.Count && draft.Record.Ints[originKey] == 0,
            "keyboard selection saves only the currently shown character");
        screens.ListKey(UiListKey.Accept);
        Frame();
        check(bindings!.SelectedItems[OriginCreationPanel.Combo] == catalog.All.Count
            && bindings.Text["DescriptionPanelText"].StartsWith("Random\n", StringComparison.Ordinal),
            "Random stays selected and explains that the roll happens on confirmation");
        int randomVersion = second.Version;
        Frame();
        check(second.Version == randomVersion && second.Record.Ints[originKey] == catalog.All.Count,
            "refreshing the creation panel cannot reroll or change the Random draft");

        foreach (var (width, height) in new[] { (800, 600), (1280, 720), (1920, 1080) })
        {
            frame = Frame(width, height);
            var panel = frame.First(c => c.Name == "DescriptionPanel").Bounds;
            var combo = Toggle(frame).Bounds;
            check(combo.Width > 0 && combo.Height > 0 && panel.Contains(combo.X, combo.Y)
                && panel.Contains(combo.X + combo.Width - 1, combo.Y + combo.Height - 1), $"dropdown stays in the description panel at {width}x{height}");
        }
        measureText = true;
        frame = Frame();
        var description = frame.First(c => c.Name == "DescriptionPanelText");
        check(float.Parse(description.Properties["ScrollMax"], System.Globalization.CultureInfo.InvariantCulture) > 0,
            "long origin details retain the native description scrollbar");
        screens.Wheel(new Vector2(description.Bounds.X + 5, description.Bounds.Y + 5), -1);
        Frame();
        check(screens.GetScroll("editor", "DescriptionPanelText") > 0, "origin description can scroll to bonuses and starting items");
        var surgery = Opening(EditKind.Surgery);
        openingField.SetValue(editor, surgery);
        ((Dictionary<CharacterId, CharacterDraft>)AccessTools.Property(surgery.GetType(), "Drafts").GetValue(surgery)!)[character] =
            new CharacterDraft(database, install, draft.Record, "Surgery");
        frame = Frame();
        check(!frame.Any(c => c.Action == OriginCreationPanel.RaceButton || c.Action == OriginCreationPanel.OriginButton
            || c.Action == $"mygui:combo:{OriginCreationPanel.Combo}:toggle"), "creation-only controls stay hidden at the surgeon");
        check(bindings!.Text["DescriptionPanelText"] == draft.Race.GetString("description"), "surgeon keeps the standard race description");
        StatsPanelChecks.Run(session, player, screens, files["Kenshi_StatsWindow.layout"], check);
    }
}
