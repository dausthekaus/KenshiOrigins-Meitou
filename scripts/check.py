#!/usr/bin/env python3
"""Run headless checks on the release's bundled Linux runtime, including its Harmony build."""
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile

repo = Path(__file__).resolve().parent.parent
if len(sys.argv) < 2:
    sys.exit("Usage: python3 scripts/check.py <Meitou release folder> [--ui-root <Kenshi folder>] [--ui-theme <mod folder>] [--render-ui] [--core-ui] [--extra-pack-ui] [--generate] [--export-vanilla-items] [--validate-origins <JSON file>]")
runtime = Path(sys.argv[1]).resolve(strict=True)
subprocess.run([
    "dotnet", "build", str(repo / "checks/KOChecks.csproj"), "-c", "Release",
    f"-p:MeitouDirectory={runtime}", "--nologo"
], check=True)

# The installed SDK can use an RC runtime. Run Harmony with the same final runtime as the game.
with tempfile.TemporaryDirectory(prefix="kenshiorigins-runtime-") as temporary:
    run = Path(temporary)
    replaced = {"KOChecks", "KOChecks.dll", "KOChecks.pdb", "KOChecks.runtimeconfig.json", "KOChecks.deps.json", "KenshiOrigins.dll"}
    for source in runtime.iterdir():
        if source.is_file() and source.name not in replaced:
            (run / source.name).symlink_to(source)
    apphost = (runtime / "MeitouClient").read_bytes()
    original = b"MeitouClient.dll"
    if apphost.count(original) != 1:
        sys.exit("Unsupported Linux apphost layout")
    (run / "KOChecks").write_bytes(apphost.replace(original, b"KOChecks.dll".ljust(len(original), b"\0")))
    (run / "KOChecks").chmod(0o755)
    for name in ("KOChecks.dll", "KOChecks.pdb"):
        shutil.copy2(repo / "checks/bin/Release/net11.0" / name, run / name)
    shutil.copy2(repo / "bin/Release/net11.0/KenshiOrigins.dll", run / "KenshiOrigins.dll")
    shutil.copy2(runtime / "MeitouClient.runtimeconfig.json", run / "KOChecks.runtimeconfig.json")
    deps = json.loads((runtime / "MeitouClient.deps.json").read_text())
    target = deps["targets"][deps["runtimeTarget"]["name"]]
    mod_version = json.loads((repo / "meitou.json").read_text())["version"]
    for name, version in (("KOChecks", "1.0.0"), ("KenshiOrigins", mod_version)):
        key = f"{name}/{version}"
        target[key] = {"runtime": {f"{name}.dll": {}}}
        deps["libraries"][key] = {"type": "project", "serviceable": False, "sha512": ""}
    (run / "KOChecks.deps.json").write_text(json.dumps(deps))
    environment = dict(os.environ, XDG_CONFIG_HOME=str(run / "config"))
    subprocess.run([str(run / "KOChecks"), str(repo), *sys.argv[2:]], env=environment, check=True)
    if not {"--generate", "--export-vanilla-items", "--validate-origins"}.intersection(sys.argv):
        # A game-supplied (TPA) DLL is deliberately refused as a mod. Retest loading with
        # our assembly absent from that list, just as in an actual Meitou installation.
        del target[f"KenshiOrigins/{mod_version}"]
        del deps["libraries"][f"KenshiOrigins/{mod_version}"]
        (run / "KOChecks.deps.json").write_text(json.dumps(deps))
        subprocess.run([str(run / "KOChecks"), str(repo), "--loader"], env=environment, check=True)
