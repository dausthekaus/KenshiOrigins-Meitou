#!/usr/bin/env python3
"""Build a mod ZIP; game DLLs and machine-specific paths stay outside the package."""
from pathlib import Path
import subprocess
import sys
import zipfile

repo = Path(__file__).resolve().parent.parent
command = ["dotnet", "build", str(repo / "KenshiOrigins.csproj"), "-c", "Release", "--nologo"]
if len(sys.argv) > 1:
    command.append(f"-p:MeitouDirectory={Path(sys.argv[1]).resolve(strict=True)}")
subprocess.run(command, check=True)
destination = repo / "artifacts/KenshiOrigins-0.1.0.zip"
destination.parent.mkdir(exist_ok=True)
with zipfile.ZipFile(destination, "w", zipfile.ZIP_DEFLATED) as archive:
    for name in ("KenshiOrigins.mod", "meitou.json", "originform.json", "LICENSE", "README.md"):
        archive.write(repo / name, f"KenshiOrigins/{name}")
    archive.write(repo / "bin/Release/net11.0/KenshiOrigins.dll", "KenshiOrigins/KenshiOrigins.dll")
print(destination)
example = repo / "artifacts/ExtraOrigins-example.zip"
with zipfile.ZipFile(example, "w", zipfile.ZIP_DEFLATED) as archive:
    for source in sorted((repo / "examples/ExtraOrigins").iterdir()):
        if source.is_file():
            archive.write(source, f"ExtraOrigins/{source.name}")
print(example)
