# Kenshi Origins prototype

C# code mod for **MeitouClient 0.22.2-nightly.20261122.2**. It gives each player character an origin with additive stat bonuses and optional starting items.

Start a new game and use **Race / Origin** in the existing left description panel. **Origin** opens by default and contains a dropdown, description, difficulty label, bonuses and starting-item list. Scroll the description to read longer entries. **Race** restores the original race description. Each starter has a random default you can change. Choose **Random** to leave the final selection to chance when you press Confirm; refreshing the UI never rerolls it.

Selecting an origin immediately keeps it in that character's creation draft; there is no extra Apply or Save button. Switching characters, changing appearance or switching the information view preserves the choice. **CONFIRM** finishes creation and applies bonuses; normal game saves persist the assignment.

The stats window shows the assigned origin beneath the attributes. Its story and bonuses appear in the lower description panels when the window opens. Hover or click a skill for its usual help; hover or click the **Origin** entry to return to the story. This also works for origins supplied by other packs.

The standalone pool gives each origin eight bonus points in total:

| Origin | Bonuses | Starting items |
| --- | --- | --- |
| Farmhand | +3 Strength, +5 Farming | Iron Stick |
| Wanderer | +5 Athletics, +3 Swimming | 2 Dried Meat |
| Apprentice | +4 Engineering, +4 Science | Tools |
| Street Urchin | +5 Stealth, +3 Thieving | Dried Meat |
| Soldier | +3 Melee Attack, +5 Melee Defence | Horse Chopper |
| Gunner | +5 Crossbows, +3 Turrets | Tooth Pick and 2 packs of matching bolts |
| Assassin | +5 Assassination, +3 Stealth | Wakizashi |

Items go into the main inventory or an existing backpack's storage. They are never equipped, never replace custom-start equipment, and are never dropped on the ground. Anything that does not fit remains in a saved queue and is retried when space opens up. The stats description reports the number of pending items. Removing, selling or consuming delivered items never grants replacements.

Later recruits and characters added directly to player squads receive a random origin. Moving between squads, dismissal and re-recruitment keep the assignment. The system examines the actual roster, so it does not rely on specific vanilla Game Starts.

## Build and install

Requires a .NET 11 SDK and the extracted standard Meitou release. Copy `Meitou.local.props.example` to `Meitou.local.props` and set `MeitouDirectory` to the folder containing `MeitouClient.dll`. The local file is gitignored.

```sh
dotnet build KenshiOrigins.csproj -c Release
python3 scripts/package.py
```

Extract `artifacts/KenshiOrigins-0.2.0.zip` into your **Kenshi installation's `mods` directory**. Enable KenshiOrigins in the launcher and restart the client. Keep `KenshiOrigins.mod`, `meitou.json`, `KenshiOrigins.dll` and `originform.json` together in `mods/KenshiOrigins/`.

The package script also creates `artifacts/ExtraOrigins-example.zip`. Extract it into the same `mods` directory and enable it after KenshiOrigins to try the sample Dockworker origin. Neither ZIP includes game DLLs.

For local testing, build, install and enable the main mod with one command:

```sh
python3 scripts/deploy.py /path/to/Kenshi/mods
```

It verifies installed file hashes and preserves the existing enabled-mod order. Previous installed files and the changed `mods.cfg` are backed up under `artifacts/deploy-backups/`. Restart MeitouClient after deploying code changes.

## Add origins

Edit `originform.json` for the built-in pool. A separate pack needs only:

```text
ExtraOrigins/
    ExtraOrigins.mod
    originform.json
```

The `.mod` header must list **KenshiOrigins.mod as a direct master dependency**. The supplied example already does. A pack needs no C# or `meitou.json`. Definitions load once at client startup, in the successfully loaded mods' order.

```json
{
  "formatVersion": 1,
  "origins": [
    {
      "id": "mypack.traveller",
      "name": "Traveller",
      "description": "A life on the road.",
      "difficulty": "Standard",
      "bonuses": { "Athletics": 5, "Swimming": 3 },
      "items": [
        { "record": "42334-changes_otto.mod", "quantity": 2, "quality": 0 }
      ]
    }
  ]
}
```

Keep IDs stable and use your pack's prefix. Stat keys are names from Meitou's `StatsEnumerated` enum; numeric keys, unsupported stats and shared-storage duplicates are rejected. Unknown fields and malformed definitions produce a log message. An invalid optional pack is skipped as a whole, and duplicate IDs never replace earlier definitions. No second marker JSON is needed.

`difficulty` is optional free text, displayed in creation and stats without affecting gameplay. It defaults to `Unrated`; the bundled `Easy` / `Standard` labels describe the relative starting challenge and can be rewritten freely. `items` is optional and defaults to an empty list, so existing packs continue to work. An item's `record` is its stable FCS record ID, not its display name. The `-otto.mod` suffix in a vanilla ID is part of that ID; it does not mean a separate mod must be installed. Quantity defaults to 1 (range 1–1000); quality defaults to 0 (range 0–100). Weapon quality uses the engine's craft-grade lookup; crossbows, armour and limbs use their level. Ordinary items keep their record-defined quality. For ammunition, quantity counts inventory items/packs rather than individual shots. Missing or unsupported item records are logged and skipped while valid grants continue.

Equipment is queued once when an origin is assigned, including later recruits. Delivered quantities and outstanding quantities persist with the character's lasting ID. Saves from v0.1.x receive their first equipment grant when loaded by this version; save normally to preserve completion. Later edits to an origin's item list do not grant another set to an already assigned character. The JSON content format stays at version 1; the internal save format is now version 2.

## Read the code

- `src/OriginsMod.cs`: the entry point. Registers the world system and narrow Harmony hooks.
- `src/OriginCatalog.cs`: finds dependent packs and validates their JSON. Content loading stays separate from gameplay.
- `src/OriginDefinition.cs`: describes an origin without putting character-specific state in it.
- `src/OriginEquipment.cs`: makes item instances and puts them into storage, returning only quantities that still need space.
- `src/OriginSystem.cs`: remembers the starting roster, waits for creation to finish, resolves Random, assigns origins and restores modifiers. The save stores stable origin IDs and pending equipment against the engine's lasting character IDs.
- `src/OriginCreationPanel.cs`: extends the existing description area with Race/Origin buttons and a native dropdown. Each selection updates the current draft through the engine's setter, so Confirm includes it.
- `src/OriginUi.cs`: displays assigned origins in stats. Hooks preserve the chosen origin across cached race/gender looks, appearance reset/import, and handle recruitment or confirmation while paused.

Bonuses use one named modifier source. Setting that source replaces its previous amount, so repeated updates do not stack bonuses. Trained stats remain unchanged. Modifiers are rebuilt from the saved origin ID after loading.

## Checks

On Linux, run the headless checks with the release's own bundled runtime and Harmony:

```sh
python3 scripts/check.py /path/to/Meitou/versions/0.22.2-nightly.20261122.2
```

They exercise dependent-pack discovery, validation, multiple starting squads, deferred bonuses/items, partial inventory delivery, backpack storage, save migration, replacement stats, recruits, persistence with changed runtime IDs, missing definitions, paused confirmation, real editor/stats hooks, deterministic hashes and the real mod loader in an isolated process. They use synthetic game data and do not modify the installed game or saves.

Pass `--ui-root /path/to/Kenshi` to exercise the installed native editor layout and templates as well: mouse/keyboard dropdown selection including Random, automatic draft saving, character/view switching, difficulty labels, scrolling and panel bounds at three resolutions. It also checks all bundled item IDs against the installed vanilla database and verifies the Gunner's ammunition. These checks use the game's screen router, including its disabled-by-default button bindings, and read assets without copying them into the project.

To regenerate the two empty `.mod` headers using Meitou's own writer:

```sh
python3 scripts/check.py /path/to/Meitou/versions/0.22.2-nightly.20261122.2 --generate
```

## Prototype limits

- Animals receive random origins, but the game's animal editor only supports renaming, so it offers no manual origin selector.
- Installing on an existing save assigns random origins to existing player characters. An import starts fresh origin assignments, as the engine resets lasting IDs and mod sidecars on import.
- If a saved origin's pack is missing, its ID is retained and displayed as unavailable, with no origin bonuses. Restoring the pack restores its bonuses on the next load. Changes to an origin definition take effect after restarting and loading.
- No quirks or race-specific origin pools yet. All definitions are eligible for random selection.
- The UI hooks target this exact nightly. Headless controls are checked; visual layout and full vanilla/modded Game Start playtesting still need an in-game pass.

API reference: [Meitou documentation at the target build](https://github.com/VlcekM/MeitouClient/blob/v0.22.2-nightly.20261122.2/docs/modding.md).
