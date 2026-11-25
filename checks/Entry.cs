using Meitou.Content;
using Meitou.Data;
using Meitou.Data.Characters;
using Meitou.Data.Fcs;
using Meitou.Modding;
using HarmonyLib;
using Meitou.Simulation;
using Meitou.Simulation.Population;

// This branch runs without KenshiOrigins on the host's trusted platform assembly list.
// It must not reference any mod types: the real loader owns that assembly's load context.
if (!args.Contains("--loader"))
{
    PrototypeChecks.Run(args);
    return;
}

string repo = Path.GetFullPath(args[0]);
var db = new GameDatabase();
var file = new FcsFile();
file.Records.Add(new FcsRecord { StringId = "204-gamedata.base", Name = "Nameless", RecordType = FcsRecordType.Faction });
file.Records.Add(new FcsRecord { StringId = "race", Name = "Human", RecordType = FcsRecordType.Race });
db.Apply(file, "loader.mod");
var data = PopulationData.Create(db, []);
var info = new ModInfo("KenshiOrigins", Path.Combine(repo, "bin/Release/net11.0/KenshiOrigins.mod"), ModSource.Local);
var host = ModHost.Load(new ModHostOptions([info], db, data, () => null, GameVersion: new CodeModVersion(0, 23, 2)));
Check(host.Mods.Count == 1 && host.Notices.Count == 0, "actual Meitou loader accepts the manifest, DLL and hook signatures");
Check(host.Simulation!.Systems.Single().Name == "origins", "actual loader registers the per-world origin system");
var rows = ModLooks.Rows(new LookRowContext(db.Find("race")!, false, new FcsRecord(), true));
Check(!rows.Any(r => r.Mod == "kenshiorigins"), "old origin arrow row is no longer registered among body sliders");
Check(!ModLooks.Rows(new LookRowContext(db.Find("race")!, false, new FcsRecord(), false)).Any(r => r.Mod == "kenshiorigins"),
    "origin selection is hidden in recruit and surgeon editors");
var editorType = typeof(IMod).Assembly.GetType("Meitou.Game.Editor.CharacterEditorPanel", true)!;
Check(Harmony.GetPatchInfo(AccessTools.Method(editorType, "BindEditor")).Owners.Contains("kenshiorigins"),
    "actual loader installs the description-panel binding hook");
Console.WriteLine("PASS: 5 real-loader assertions in an isolated process.");

static void Check(bool condition, string message)
{
    if (!condition) throw new Exception("FAIL: " + message);
    Console.WriteLine("  OK " + message);
}
