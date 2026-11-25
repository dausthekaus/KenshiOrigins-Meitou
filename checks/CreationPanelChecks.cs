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
        CharacterDraft draft, CharacterId character, string? install, Action<bool, string> check, string? theme = null, string? preview = null,
        bool corePack = false)
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
        if (theme is not null)
            foreach (string file in Directory.EnumerateFiles(Path.Combine(theme, "gui"), "*", SearchOption.AllDirectories))
                files[Path.GetFileName(file)] = file;
        string? Resolve(string name) => files.GetValueOrDefault(Path.GetFileName(name))
            ?? (File.Exists(Path.Combine(gui, name)) ? Path.Combine(gui, name) : null);
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
        Func<UiControl, float>? actualTextHeight = null;
        IReadOnlyList<UiControl> Frame(int width = 1280, int height = 900)
        {
            AccessTools.Field(playerType, "Width").SetValue(player, width);
            AccessTools.Field(playerType, "Height").SetValue(player, height);
            return screens.Arrange(width, height, _ =>
            {
                bindings = (UiBindings)bind.Invoke(router, ["editor"])!;
                // A headless stand-in for the painter's font measurement, to exercise native scrolling.
                if (measureText)
                {
                    // Exercise a genuinely long story, since the roomier panel fits the bundled ones.
                    bindings.Text["DescriptionPanelText"] = string.Join("\n\n", Enumerable.Repeat(bindings.Text["DescriptionPanelText"], 4));
                    bindings.TextHeight = c => c.Caption.Split('\n').Sum(line =>
                        Math.Max(1, (int)Math.Ceiling(line.Length / Math.Max(1, c.Bounds.Width / 7)))) * 16;
                }
                if (actualTextHeight is not null) bindings.TextHeight = actualTextHeight;
                return bindings;
            }).Single().Controls;
        }
        void Click(UiControl control)
        {
            var visible = control.Bounds.Intersect(control.Clip ?? control.Bounds);
            if (visible.Width <= 0 || visible.Height <= 0) throw new InvalidOperationException($"Cannot click clipped control {control.Name}.");
            screens.PointerDown(new Vector2(visible.X + visible.Width / 2, visible.Y + visible.Height / 2));
            screens.PointerUp();
        }
        UiControl Toggle(IReadOnlyList<UiControl> controls) => controls.First(c => c.Action == $"mygui:combo:{OriginCreationPanel.Combo}:toggle");
        string? SelectedId() => bindings!.SelectedItems[OriginCreationPanel.Combo] is var index && index >= 0
            ? bindings.Lists[OriginCreationPanel.Combo][index].Id : null;
        IReadOnlyList<UiControl> Select(string menu, string caption)
        {
            var controls = Frame();
            if (!controls.Any(c => c.Action.StartsWith($"mygui:combo:{menu}:", StringComparison.Ordinal)
                && c.Action != $"mygui:combo:{menu}:toggle"))
                Click(controls.First(c => c.Action == $"mygui:combo:{menu}:toggle"));
            controls = Frame();
            bool IsOption(UiControl control) => control.Action.StartsWith($"mygui:combo:{menu}:", StringComparison.Ordinal)
                && control.Action != $"mygui:combo:{menu}:toggle";
            for (int scroll = 0; scroll < 40; scroll++)
            {
                var option = controls.FirstOrDefault(c => IsOption(c) && c.Caption == caption);
                if (option is not null && option.Bounds.Intersect(option.Clip ?? option.Bounds).Height >= option.Bounds.Height / 2)
                {
                    Click(option);
                    return Frame();
                }
                // Larger catalogs virtualize off-screen rows; scroll a visible popup row until the target is arranged.
                var anchor = option ?? controls.First(c => IsOption(c)
                    && c.Bounds.Intersect(c.Clip ?? c.Bounds).Height > 0);
                var clip = anchor.Clip ?? anchor.Bounds;
                int targetIndex = bindings!.Lists[menu].ToList().FindIndex(item => item.Caption == caption);
                int anchorIndex = bindings.Lists[menu].ToList().FindIndex(item => item.Caption == anchor.Caption);
                float direction = targetIndex < anchorIndex || option is not null && option.Bounds.Y < clip.Y ? 1 : -1;
                screens.Wheel(new Vector2(clip.X + clip.Width / 2, clip.Y + clip.Height / 2), direction);
                controls = Frame();
            }
            throw new InvalidOperationException($"Could not scroll the native selector to '{caption}'.");
        }
        void SelectTag(string tag)
        {
            var controls = Frame();
            UiControl Button() => controls.Single(c => c.Name.StartsWith(OriginCreationPanel.TagButtonPrefix, StringComparison.Ordinal) && c.Caption == tag);
            for (int scroll = 0; scroll < 40 && Button().Bounds.Intersect(Button().Clip ?? Button().Bounds).Width < Button().Bounds.Width / 2; scroll++)
            {
                var button = Button();
                var clip = button.Clip!.Value;
                screens.Wheel(new Vector2(clip.X + clip.Width / 2, clip.Y + clip.Height / 2), button.Bounds.X < clip.X ? 1 : -1);
                controls = Frame();
            }
            Click(Button());
            Frame();
        }
        void Query(string query)
        {
            var controls = Frame();
            Click(controls.First(c => c.Name == OriginCreationPanel.Search));
            screens.EditKey(UiEditKey.SelectAll);
            screens.Backspace();
            bool typed = true;
            foreach (char letter in query) typed &= screens.TextInput(letter);
            check(typed && screens.GetText("editor", OriginCreationPanel.Search) == query, "native search field accepts the query");
            Frame();
        }

        if (corePack)
        {
            var coreFrame = Frame(1920, 1080);
            check(bindings!.Lists[OriginCreationPanel.Combo].Count == catalog.All.Count + 1,
                "the expanded native selector contains all loaded origins plus Random");
            if (catalog.All.Count == 18)
            {
                foreach (var (width, height) in new[] { (1920, 1080), (1280, 720) })
                {
                    coreFrame = Frame(width, height);
                    var buttons = coreFrame.Where(c => c.Name.StartsWith(OriginCreationPanel.TagButtonPrefix, StringComparison.Ordinal)).ToArray();
                    check(buttons.Length == 6 && buttons.All(c => c.Bounds.Intersect(c.Clip ?? c.Bounds).Width >= c.Bounds.Width - 1),
                        $"all five official categories and All tags fit without horizontal clipping at {width}x{height}");
                }
            }
            int unchanged = draft.Version;
            foreach (string tag in new[] { "Civilian", "Combat", "Special", "Survival", "Technical" })
            {
                SelectTag(tag);
                check(bindings!.Lists[OriginCreationPanel.Combo].Where(item => item.Id != OriginCreationPanel.RandomOption)
                    .Select(item => item.Id).SequenceEqual(catalog.All.Where(o => o.Tags.Contains(tag))
                        .OrderBy(o => o.Name, StringComparer.OrdinalIgnoreCase).Select(o => o.Id)),
                    $"native {tag} button filters the consolidated official tags in alphabetical order");
                if (tag is "Combat" or "Survival")
                    check(bindings.Lists[OriginCreationPanel.Combo].Any(item => item.Id == "kenshiorigins.forgottenveteran"),
                        "The Forgotten Veteran appears under either category without requiring both");
            }
            check(draft.Version == unchanged, "category browsing preserves the saved draft choice");
            SelectTag("Combat");
            Query("RECOVERY");
            check(bindings!.Lists[OriginCreationPanel.Combo].Select(item => item.Id)
                .SequenceEqual(["kenshiorigins.forgottenveteran", OriginCreationPanel.RandomOption]),
                "native search combines with a category and finds the new veteran story case-insensitively");
            Query("");
            SelectTag("All tags");
            Query("scientist");
            Select(OriginCreationPanel.Combo, "Scientist");
            check(draft.Record.Ints[originKey] == catalog.IndexOf("kenshiorigins.scientist")
                && bindings!.Text["DescriptionPanelText"].Contains(catalog.Find("kenshiorigins.scientist")!.Description),
                "selecting a new origin saves its original catalog index and shows its complete story");
            Query("");
            if (catalog.Find("extraorigins.dockworker") is not null)
            {
                SelectTag("Labouring");
                check(bindings!.Lists[OriginCreationPanel.Combo].Select(item => item.Id)
                    .SequenceEqual(["extraorigins.dockworker", OriginCreationPanel.RandomOption]),
                    "an explicitly contributed old tag remains usable through the native third-party category button");
                Select(OriginCreationPanel.Combo, "Dockworker");
                check(draft.Record.Ints[originKey] == catalog.IndexOf("extraorigins.dockworker"),
                    "third-party tag selection still saves the correct external origin index");
                SelectTag("All tags");
            }
            coreFrame = Select(OriginCreationPanel.Combo, "The Unfair One");
            check(coreFrame.Any(c => c.Caption == "The Unfair One" && c.Properties.GetValueOrDefault("TextColour") == OriginUi.TestingInk)
                && coreFrame.Where(c => c.Caption.Contains("TESTING ONLY")).All(c => c.Properties.GetValueOrDefault("TextColour") == OriginUi.TestingInk),
                "the real cheat origin retains its red selector, story and manual-only warning in the expanded pack");
            if (preview is not null)
            {
                UiPreview.Save(resources, Resolve, painter =>
                {
                    actualTextHeight = c => painter.TextHeight(c, 1080, 13);
                    var rendered = Frame(1920, 1080);
                    foreach (var button in rendered.Where(c => c.Name.StartsWith(OriginCreationPanel.TagButtonPrefix, StringComparison.Ordinal)))
                    {
                        var wrapping = button with { Properties = new Dictionary<string, string>(button.Properties) { ["WordWrap"] = "true" } };
                        check(painter.TextHeight(wrapping, 1080, 0) <= painter.TextHeight(wrapping with { Caption = "M" }, 1080, 0),
                            $"the full '{button.Caption}' label fits its native text area without truncation");
                    }
                    return rendered;
                }, preview, 1920, 1080);
                actualTextHeight = null;
            }
            return;
        }

        var frame = Frame();
        check(screens.Layout("editor").Find(OriginCreationPanel.Combo)!.Properties.GetValueOrDefault("ResolvedTemplate") == "true",
            "origin dropdown expands the game's native combo skin");
        check(bindings!.Lists[OriginCreationPanel.Combo].Count == catalog.All.Count + 1, "native dropdown lists the loaded origin pool plus Random");
        check(bindings.Text["DescriptionPanelText"].Contains(catalog.All[2].Description), "existing race-description text area now shows the selected origin");
        check(bindings.Text["DescriptionPanelText"].Contains("Difficulty: " + catalog.All[2].Difficulty),
            "creation displays the origin's informational difficulty string");
        check(bindings.Text["DescriptionPanelText"].Contains("Starting items"), "creation previews starting equipment from the origin definition");
        check(frame.Single(c => c.Name == OriginCreationPanel.Combo).Caption.Length == 0
            && frame.Count(c => c.Name.StartsWith(OriginCreationPanel.Combo, StringComparison.Ordinal) && c.Caption == catalog.All[2].Name) == 1,
            "the dropdown paints its selected label once, through the template client");
        check(float.Parse(frame.Single(c => c.Name == OriginCreationPanel.TagRow).Properties["ScrollMax"],
            System.Globalization.CultureInfo.InvariantCulture) == 0,
            "horizontal tags have no hidden vertical overflow to intercept wheel input");
        check(bindings.Text["DescriptionPanelText"].Contains("Tags: " + string.Join(", ", catalog.All[2].Tags)),
            "origin details display the JSON tags");
        check(!frame.Any(c => c.Name == "kenshiorigins.preview"), "floating origin preview has been removed");
        check(!draft.Controls(EditorTab.Body).Any(c => c.Mod == OriginsMod.Id), "origin selector is absent from the body slider list");

        // The second frame includes our injected buttons in the router's disabled-by-default set.
        frame = Frame();
        check(frame.Single(c => c.Action == OriginCreationPanel.ModeButton).Enabled,
            "the compact mode switch remains enabled after the screen router applies its defaults");
        check(!frame.Any(c => c.Name == "StatsPanel"), "origin browser uses the race-stat panel's space for its description");

        int before = draft.Version;
        Click(Toggle(frame));
        frame = Frame();
        var option = frame.First(c => c.Caption == catalog.All[0].Name && c.Action.StartsWith("mygui:combo:", StringComparison.Ordinal)
            && c.Action != $"mygui:combo:{OriginCreationPanel.Combo}:toggle");
        Click(option);
        Frame();
        check(draft.Record.Ints[originKey] == 0 && draft.Version > before, "real popup selection immediately saves and marks the current draft changed");
        check(bindings!.Text["DescriptionPanelText"].Contains(catalog.All[0].BonusSummary), "description and bonuses refresh on selection without Apply");

        int beforeSearch = draft.Version;
        Query("GUN");
        check(bindings!.Lists[OriginCreationPanel.Combo].Select(item => item.Id).SequenceEqual(["kenshiorigins.gunner", OriginCreationPanel.RandomOption])
            && draft.Record.Ints[originKey] == 0 && draft.Version == beforeSearch,
            "live search filters case-insensitively without changing the saved origin");
        check(screens.EditEnter(false), "Enter submits the search through its own action");
        Frame();
        check(drafts.Count == 2 && openingField.GetValue(editor) == opening && draft.Version == beforeSearch,
            "Enter in search cannot confirm character creation or modify a draft");
        Select(OriginCreationPanel.Combo, "Gunner");
        check(draft.Record.Ints[originKey] == catalog.IndexOf("kenshiorigins.gunner"),
            "a filtered menu writes the original catalog index rather than the visible row index");
        Query("zz-no-matching-origin");
        check(bindings!.Lists[OriginCreationPanel.Combo].Single().Id == OriginCreationPanel.RandomOption
            && bindings.Text[OriginCreationPanel.ResultCount] == "No matches; choice kept"
            && draft.Record.Ints[originKey] == catalog.IndexOf("kenshiorigins.gunner"),
            "no matches preserve the current choice and keep Random available");
        check(screens.LastFrame.Single().Controls.Count(c => c.Name.StartsWith(OriginCreationPanel.Combo, StringComparison.Ordinal)
            && c.Caption == "Gunner") == 1, "a filtered-out choice also paints its fallback caption exactly once");
        Query("");
        SelectTag("Combat");
        check(bindings!.Lists[OriginCreationPanel.Combo].Count == 4 && SelectedId() == "kenshiorigins.gunner",
            "horizontal tag buttons gather Combat origins from their metadata");
        SelectTag("Stealth");
        check(screens.GetScroll("editor", OriginCreationPanel.TagRow + "@h") > 0,
            "the tag row scrolls horizontally with the mouse wheel to reveal later categories");
        Query("street");
        check(bindings!.Lists[OriginCreationPanel.Combo].Select(item => item.Id).SequenceEqual(["kenshiorigins.streeturchin", OriginCreationPanel.RandomOption]),
            "search and tag filters combine without excluding Random");
        Select(OriginCreationPanel.Combo, "Street Urchin");
        check(draft.Record.Ints[originKey] == catalog.IndexOf("kenshiorigins.streeturchin"), "filtered tag selection automatically saves the correct origin");
        Query("");
        SelectTag("All tags");
        frame = Select(OriginCreationPanel.Combo, catalog.All[0].Name);

        action.Invoke(editor, [new MyGuiEvent("editor", "FaceTabButton", "FaceTabButton")]);
        frame = Frame();
        check(screens.Layout("editor").Widgets.Count(w => w.Name == OriginCreationPanel.Combo) == 1,
            "body-row reconstruction preserves exactly one origin dropdown");
        Click(frame.First(c => c.Action == OriginCreationPanel.ModeButton));
        frame = Frame();
        check(bindings!.Text["DescriptionPanelText"] == draft.Race.GetString("description"), "Race toggle restores the original race description");
        check(!frame.Any(c => c.Action == $"mygui:combo:{OriginCreationPanel.Combo}:toggle"), "dropdown is hidden in Race view");
        check(frame.Any(c => c.Name == "StatsPanel") && !frame.Any(c => c.Name == OriginCreationPanel.Search)
            && !frame.Any(c => c.Name == OriginCreationPanel.TagRow),
            "Race mode restores race stats and hides the search and tag controls");
        Click(frame.First(c => c.Action == OriginCreationPanel.ModeButton));
        frame = Frame();
        check(SelectedId() == catalog.All[0].Id, "switching views retains the saved choice");

        AccessTools.Field(opening.GetType(), "Index").SetValue(opening, 1);
        frame = Frame();
        check(SelectedId() == catalog.All[1].Id, "switching characters restores their own choice instead of the combo cache");
        Click(Toggle(frame));
        Frame();
        check(screens.ListKey(UiListKey.End), "native dropdown supports keyboard navigation");
        Frame();
        check(second.Record.Ints[originKey] == catalog.All.Count && draft.Record.Ints[originKey] == 0,
            "keyboard selection saves only the currently shown character");
        screens.ListKey(UiListKey.Accept);
        Frame();
        check(SelectedId() == OriginCreationPanel.RandomOption
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
            var mode = frame.Single(c => c.Name == OriginCreationPanel.ModeButton).Bounds;
            var search = frame.Single(c => c.Name == OriginCreationPanel.Search).Bounds;
            var tag = frame.Single(c => c.Name == OriginCreationPanel.TagRow).Bounds;
            var body = frame.Single(c => c.Name == "DescriptionPanelText").Bounds;
            var count = frame.Single(c => c.Name == OriginCreationPanel.ResultCount).Bounds;
            var namePanel = frame.Single(c => c.Name == "NamePanel").Bounds;
            var confirmPanel = frame.Single(c => c.Name == "ConfirmPanel").Bounds;
            check(panel.Y >= namePanel.Y + namePanel.Height && panel.Y - (namePanel.Y + namePanel.Height) <= 8 * bindings!.TemplateScale + 1
                && panel.Y + panel.Height <= confirmPanel.Y,
                "browser follows the theme's original sidebar anchors without leaving an extra gap or covering Confirm");
            check(mode.X + mode.Width <= search.X && search.Y + search.Height <= combo.Y
                && combo.Y + combo.Height <= tag.Y && tag.Y + tag.Height <= body.Y
                && panel.Contains(body.X + body.Width - 1, body.Y + body.Height - 1),
                $"mockup rows remain separate and the description stays inside the browser at {width}x{height}");
            check(body.Y - (tag.Y + tag.Height) <= 32 * bindings!.TemplateScale + 1
                && count.Height >= 13 * bindings.TemplateScale,
                $"header uses compact spacing instead of a growing blank gap at {width}x{height}");
            check(frame.Any(c => c.Type == "ScrollBar" && c.Properties.GetValueOrDefault("TemplateOwner") == OriginCreationPanel.TagRow
                && c.Properties.GetValueOrDefault("TemplatePart") == "HScroll" && c.Skin != "ScrollBarH"),
                "tag navigation has a native Kenshi horizontal scrollbar skin");
        }
        Select(OriginCreationPanel.Combo, "Soldier");
        frame = Frame();
        check(bindings!.Lists[OriginCreationPanel.Combo].Single(item => item.Caption == "Soldier").Colour == OriginUi.TestingInk
            && frame.Any(c => c.Caption == "Soldier" && c.Properties.GetValueOrDefault("TextColour") == OriginUi.TestingInk),
            "testing entries and the selected dropdown caption use native red text");
        check(frame.Single(c => c.Name == "DescriptionPanelText").Properties.GetValueOrDefault("TextColour") == OriginUi.TestingInk
            && bindings.Text["DescriptionPanelText"].Contains("TESTING ONLY")
            && frame.Where(c => c.Caption.Contains("TESTING ONLY")).All(c => c.Properties.GetValueOrDefault("TextColour") == OriginUi.TestingInk),
            "testing origin details are red and state that selection is manual only");
        Query("zz-no-matching-origin");
        frame = Frame();
        check(frame.Any(c => c.Caption == "Soldier" && c.Properties.GetValueOrDefault("TextColour") == OriginUi.TestingInk),
            "a testing origin retains its red fallback caption when filters hide it");
        Query("");
        frame = Select(OriginCreationPanel.Combo, "Assassin");
        check(frame.Single(c => c.Name == "DescriptionPanelText").Properties.GetValueOrDefault("TextColour") != OriginUi.TestingInk,
            "switching to a normal origin restores the theme's description colour");
        Select(OriginCreationPanel.Combo, "Soldier");
        measureText = true;
        frame = Frame();
        var description = frame.First(c => c.Name == "DescriptionPanelText");
        check(float.Parse(description.Properties["ScrollMax"], System.Globalization.CultureInfo.InvariantCulture) > 0,
            "long origin details retain the native description scrollbar");
        screens.Wheel(new Vector2(description.Bounds.X + 5, description.Bounds.Y + 5), -1);
        Frame();
        check(screens.GetScroll("editor", "DescriptionPanelText") > 0, "origin description can scroll to bonuses and starting items");
        measureText = false;
        if (preview is not null)
        {
            Select(OriginCreationPanel.Combo, "Soldier");
            measureText = false;
            UiPreview.Save(resources, Resolve, painter =>
            {
                actualTextHeight = c => painter.TextHeight(c, 1080, 13);
                return Frame(1920, 1080);
            }, preview, 1920, 1080);
            actualTextHeight = null;
        }
        var surgery = Opening(EditKind.Surgery);
        openingField.SetValue(editor, surgery);
        ((Dictionary<CharacterId, CharacterDraft>)AccessTools.Property(surgery.GetType(), "Drafts").GetValue(surgery)!)[character] =
            new CharacterDraft(database, install, draft.Record, "Surgery");
        frame = Frame();
        check(!frame.Any(c => c.Action == OriginCreationPanel.ModeButton || c.Name == OriginCreationPanel.Search
            || c.Name == OriginCreationPanel.TagRow
            || c.Action == $"mygui:combo:{OriginCreationPanel.Combo}:toggle"), "creation-only controls stay hidden at the surgeon");
        check(bindings!.Text["DescriptionPanelText"] == draft.Race.GetString("description"), "surgeon keeps the standard race description");
        StatsPanelChecks.Run(session, player, screens, files["Kenshi_StatsWindow.layout"], check);
    }
}
