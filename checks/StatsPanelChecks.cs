using System.Numerics;
using HarmonyLib;
using KenshiOrigins;
using Meitou.Data.Gameplay.Bodies;
using Meitou.Data.Ui;
using Meitou.Game.Host;
using Meitou.Game.Ui;
using Meitou.Simulation;

internal static class StatsPanelChecks
{
    internal static void Run(WorldSession session, object player, MyGuiScreenHost screens, string layoutFile, Action<bool, string> check)
    {
        var playerType = player.GetType();
        var stats = AccessTools.Property(playerType, "StatsWindow").GetValue(player)!;
        var router = AccessTools.Property(playerType, "ScreenRouter").GetValue(player)!;
        var bind = AccessTools.Method(router.GetType(), "BindFaithfulScreen");
        var decorate = AccessTools.Method(router.GetType(), "AddFaithfulContent");
        var hover = AccessTools.Method(stats.GetType(), "PollStatHover");
        var selection = AccessTools.Field(stats.GetType(), "SelectedStat");
        var world = session.World;
        var system = world.Systems.OfType<OriginSystem>().Single();
        var character = world.Player.Selection.First();
        var origin = system.Views[character];
        screens.Close("editor");
        screens.Register("stats", MyGuiReader.ReadLayout(layoutFile), modal: true);
        screens.Open("stats");
        AccessTools.Field(playerType, "Mouse").SetValue(player, new Vector2(-1, -1));
        hover.Invoke(stats, null);

        IReadOnlyList<UiControl> Frame(int width = 1280, int height = 900)
        {
            AccessTools.Field(playerType, "Width").SetValue(player, width);
            AccessTools.Field(playerType, "Height").SetValue(player, height);
            return screens.Arrange(width, height, id => (UiBindings)bind.Invoke(router, [id])!,
                (id, controls) => decorate.Invoke(router, [id, controls])).Single().Controls;
        }
        void Click(UiControl control)
        {
            screens.PointerDown(new Vector2(control.Bounds.X + control.Bounds.Width / 2, control.Bounds.Y + control.Bounds.Height / 2));
            screens.PointerUp();
        }
        var frame = Frame();
        check(frame.Single(c => c.Name == "kenshiorigins.stats-story").Caption == origin.Description,
            "opening native stats shows the current character's origin story");
        check(frame.Single(c => c.Name == "kenshiorigins.stats-bonuses").Caption.Contains(origin.Bonuses),
            "origin bonuses accompany the story in the stats description panels");
        check(frame.Single(c => c.Name == "kenshiorigins.stats-bonuses").Caption.Contains("Difficulty: " + origin.Difficulty),
            "stats screen displays the saved character's origin difficulty label");
        var resources = (MyGuiResources)AccessTools.Field(playerType, "UiResources").GetValue(player)!;
        check(frame.Single(c => c.Name == "kenshiorigins.stats-story").Properties["TextColour"]
            == (origin.TestingOnly ? OriginUi.TestingInk : MyGuiColours.TemplateInk(resources, "Kenshi_GenericTextBoxSkin")),
            "testing stories use red text while normal stories follow the theme");
        check(!origin.TestingOnly || frame.Where(c => c.Name.StartsWith("kenshiorigins.", StringComparison.Ordinal))
            .All(c => c.Properties.GetValueOrDefault("TextColour") == OriginUi.TestingInk),
            "the testing origin name, bonuses and story all use red text in stats");

        var skill = frame.First(c => c.Action == "stat:" + (int)StatsEnumerated.Strength);
        Click(skill);
        frame = Frame();
        check(!frame.Any(c => c.Name == "kenshiorigins.stats-story") && frame.Any(c => c.Name == "stat-description-title"),
            "selecting a normal skill restores its native description");
        Click(frame.Single(c => c.Name == "kenshiorigins.stats"));
        frame = Frame();
        check((string)selection.GetValue(stats)! == OriginUi.OriginStat
            && frame.Single(c => c.Name == "kenshiorigins.stats-story").Caption == origin.Description,
            "clicking Origin through the real screen router brings the story back");

        var entry = frame.Single(c => c.Name == "kenshiorigins.stats");
        selection.SetValue(stats, ((int)StatsEnumerated.Strength).ToString());
        AccessTools.Field(playerType, "Mouse").SetValue(player, new Vector2(entry.Bounds.X + 2, entry.Bounds.Y + 2));
        hover.Invoke(stats, null);
        check((string)selection.GetValue(stats)! == OriginUi.OriginStat, "native stat hover recognizes the origin entry");

        // Read the current character's published view each frame, including a changed selection.
        var other = system.Views.Keys.First(id => id != character);
        var otherOrigin = system.Views[other];
        world.Player.Selection.Clear();
        world.Player.Selection.Add(other);
        AccessTools.Method(typeof(World), "PublishPaused").Invoke(world, null);
        frame = Frame();
        check(origin.Description != otherOrigin.Description
            && frame.Single(c => c.Name == "kenshiorigins.stats-story").Caption == otherOrigin.Description,
            "changing the selected character refreshes the origin story");
        check(frame.Single(c => c.Name == "kenshiorigins.stats-story").Properties["TextColour"]
            == (otherOrigin.TestingOnly ? OriginUi.TestingInk : MyGuiColours.TemplateInk(resources, "Kenshi_GenericTextBoxSkin")),
            "changing to a normal character restores the theme's stats text colour");
        world.Player.Selection.Clear();
        world.Player.Selection.Add(character);
        AccessTools.Method(typeof(World), "PublishPaused").Invoke(world, null);
        foreach (var (width, height) in new[] { (800, 600), (1280, 720), (1920, 1080) })
        {
            frame = Frame(width, height);
            var panel = frame.Single(c => c.Name == "Description2Panel").Bounds;
            var story = frame.Single(c => c.Name == "kenshiorigins.stats-story");
            check(panel.Contains(story.Bounds.X, story.Bounds.Y)
                && panel.Contains(story.Bounds.X + story.Bounds.Width - 1, story.Bounds.Y + story.Bounds.Height - 1),
                $"origin story stays within the stats description panel at {width}x{height}");
        }
        screens.Close("stats");
    }
}
