#!/usr/bin/env python3
"""Build, install and enable Kenshi Origins in a specified Kenshi mods directory."""
import argparse
from datetime import datetime, timezone
import hashlib
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import zipfile

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("mods_directory", type=Path)
arguments = parser.parse_args()
repo = Path(__file__).resolve().parent.parent
mods = arguments.mods_directory.resolve(strict=True)
config = mods.parent / "data/mods.cfg"
destination = mods / "KenshiOrigins"
if not mods.is_dir() or not config.parent.is_dir():
    sys.exit("Expected a Kenshi mods directory beside its data directory.")
if destination.is_symlink() or config.is_symlink():
    sys.exit("Refusing to replace a symlinked mod folder or mods.cfg.")

subprocess.run([sys.executable, str(repo / "scripts/package.py")], check=True)
backup = repo / "artifacts/deploy-backups" / datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%S%fZ")
if destination.exists():
    backup.mkdir(parents=True)
    shutil.copytree(destination, backup / "KenshiOrigins")
destination.mkdir(exist_ok=True)


def replace_file(path, contents):
    """Publish a complete file without leaving a partially copied DLL behind."""
    with tempfile.NamedTemporaryFile(dir=path.parent, delete=False) as temporary:
        pending = Path(temporary.name)
        try:
            temporary.write(contents)
            temporary.flush()
            os.fchmod(temporary.fileno(), 0o644)
        except BaseException:
            pending.unlink(missing_ok=True)
            raise
    try:
        pending.replace(path)
    finally:
        pending.unlink(missing_ok=True)


with zipfile.ZipFile(repo / "artifacts/KenshiOrigins-0.1.0.zip") as archive:
    for member in archive.infolist():
        parts = Path(member.filename).parts
        if len(parts) != 2 or parts[0] != "KenshiOrigins" or member.is_dir():
            sys.exit(f"Unexpected package entry: {member.filename}")
        target = destination / parts[1]
        if target.is_symlink():
            sys.exit(f"Refusing to replace symlink: {target}")
        contents = archive.read(member)
        replace_file(target, contents)
        if hashlib.sha256(target.read_bytes()).digest() != hashlib.sha256(contents).digest():
            sys.exit(f"Verification failed: {target}")
        print(f"Verified {target.name}")

# Preserve existing names, order, encoding and line endings. The new name is ASCII.
previous = config.read_bytes() if config.exists() else b""
if b"KenshiOrigins.mod" not in [line.strip() for line in previous.splitlines()]:
    backup.mkdir(parents=True, exist_ok=True)
    if config.exists():
        (backup / "mods.cfg").write_bytes(previous)
    newline = b"\r\n" if b"\r\n" in previous or not previous else b"\n"
    separator = newline if previous and not previous.endswith(b"\n") else b""
    replace_file(config, previous + separator + b"KenshiOrigins.mod" + newline)
assert b"KenshiOrigins.mod" in [line.strip() for line in config.read_bytes().splitlines()]
print(f"Installed and enabled: {destination}")
if backup.exists():
    print(f"Previous files: {backup}")
