# Kenshi Origins prototype

C# code mod for **MeitouClient 0.24.0**. It gives each player character an origin with additive stat bonuses and optional starting items. Version 0.4.1 preserves origins through import and caps origin-boosted skills at 100, using the boosted level for XP difficulty. Its manifest does not claim compatibility with older builds.

Start a new game and use the compact **Origin / Race** mode button at the upper left of the description panel. **Origin** opens by default with search beside the button, the origin selector underneath, a horizontal row of tag buttons, and a scrollable description below. Move the mouse wheel over the tags to scroll sideways, or use the horizontal scrollbar; **All tags** clears the category filter. Origin mode expands into the race-stat area for more reading space. **Race** restores the original race description and race stats.

Search filters as you type, matching names, stories, difficulty labels and tags without regard to case. Search and tag filters combine, and matching origins appear alphabetically. Filtering never changes the saved choice by itself; if it is excluded, the menu retains its caption and reports that the choice was kept. Each starter has a random default you can change. **Random** remains available with every filter and chooses from all loaded origins except those marked `testingOnly` when you press Confirm; refreshing the UI never rerolls it. Enter in search submits the search rather than confirming creation.

Selecting an origin immediately keeps it in that character's creation draft; there is no extra Apply or Save button. Switching characters, changing appearance or switching the information view preserves the choice. **CONFIRM** finishes creation and applies bonuses; normal game saves persist the assignment.

The stats window shows the assigned origin beneath the attributes. Its story and bonuses appear in the lower description panels when the window opens. Hover or click a skill for its usual help; hover or click the **Origin** entry to return to the story. This also works for origins supplied by other packs.

The standalone pool includes these eighteen origins:

| Origin | Bonuses | Starting items |
| --- | --- | --- |
| Nobody | None | None |
| Farmhand | +3 Strength, +12 Farming, +5 Labouring | Iron Stick |
| Wanderer | +5 Athletics, +3 Swimming | 2 Dried Meat |
| Smith Apprentice | +10 Weapon Smithing, +5 Armour Smithing, +3 Toughness | Tools |
| Street Urchin | +12 Stealth, +8 Thieving, +5 Athletics | Dried Meat |
| Soldier | +10 Melee Attack, +12 Melee Defence, +5 Toughness | Horse Chopper |
| Gunner | +12 Crossbows, +10 Turrets, +5 Perception | Tooth Pick and 2 packs of matching bolts |
| Assassin | +20 Assassination, +15 Stealth, +5 Dexterity | Wakizashi |
| Escaped Slave | +12 Labouring, +5 Martial Arts, +3 Toughness | None |
| Fugitive | +10 Martial Arts, +8 Toughness, +5 Athletics | None |
| Miner | +15 Labouring, +5 Strength, +3 Toughness | Iron Club |
| Field Medic | +20 Medic, +5 Athletics | Standard first aid kit and Splint Kit |
| The Unfair One | +75 physical/combat stats, +50 other skills (34 distinct supported stats) | None |
| Scientist | +30 Science, +15 Robotics, +10 Engineering | Ancient Science Book |
| Engineer | +20 Engineering, +8 Labouring, +5 Science, +5 Strength | 2 Electrical Components and 2 Building Material |
| The Forgotten Veteran | +25 Toughness, +15 Melee Defence, +15 Melee Attack, +10 Strength, +5 Dexterity, +5 Medic | Low-quality Longsword and Standard first aid kit |
| Seasoned Wanderer | +15 Athletics, +12 Toughness, +8 Swimming, +8 Medic, +8 Stealth, +5 Lockpicking, +5 Cooking | Wooden Backpack, 3 Dried Meat and Basic First Aid Kit |
| Adventurous Innkeeper | +25 Cooking, +5 Strength, +5 Farming, +3 Melee Defence | 2 Foodcube and 2 Bread |

The five new origins are available for manual creation, random starting choices and normal recruitment. The Forgotten Veteran is intentionally the strongest legitimate combat origin; the Seasoned Wanderer is a survival generalist without direct combat-skill bonuses. No negative bonuses are used in the official pack.

Official categories are **Combat**, **Civilian**, **Technical**, **Survival** and **Special**. The menu selects one category at a time; an origin tagged Combat and Survival appears under either button. **All tags** is a UI control rather than a tag stored in an origin. The six buttons fit in the horizontal menu at 1920×1080 and 1280×720; smaller screens and additional pack categories can scroll. New packs can still define their own tags. For example, the bundled ExtraOrigins pack explicitly adds Labouring; that tag appears only when a contributing pack is loaded.

**Nobody** is available only for the initial roster. **The Unfair One** is a cheat origin for testing: its text is red in the selector, creation details and stats, and it requires manual selection. It never appears in random creation defaults, creation's Random rolls or later recruits. Its warnings are also written in its story.

Only characters in the initial Game Start roster receive starting items, after character creation is confirmed. The roster is captured from all player squads when a new world becomes ready, so custom Game Starts and multiple starting squads work without matching any start or squad names. Items go into the main inventory or an existing backpack's storage. They are never equipped, never replace custom-start equipment, and are never dropped on the ground. Anything that does not fit remains in a saved queue and is retried when space opens up. The stats description reports the number of pending items. Removing, selling or consuming delivered items never grants replacements.

Later recruits and characters added directly to player squads receive a random origin and its stat bonuses, but no starting equipment or equipment queue. This also applies to player characters added after the initial roster capture while the creation screen is still open. Moving between squads, dismissal and re-recruitment keep the assignment and do not create equipment grants.

Positive bonuses have room only up to **100**, including The Unfair One. For example, trained Toughness 90 with a configured +25 receives +10. The JSON and description list the configured bonus; its actual contribution can be smaller near the cap. The contribution is checked again as trained stats or other additive modifiers change. Origins never reduces existing trained levels or another mod's modifier: if those already exceed 100, its contribution is zero. Equipment, injury and age multipliers retain their engine behavior; this is a cap on Origins' additive contribution rather than a global clamp on other systems.

Origin-boosted skills train at their boosted level: trained 70 with +25 trains like vanilla 95. The supported `IExperienceFilter` adjusts incoming XP while preserving racial bonuses, XP settings and training-dummy limits. Combat and general skills retain the native curve toward 101, with a hard stop at boosted 100. Medical skills retain their separate seconds-per-point formula at the boosted level; mass combat retains straight gains. Reaching boosted 100 stops further training in that skill while its bonus is active. Skills without a positive origin bonus keep native training behavior. Bonuses stay separate from trained values and are never baked into a save.

## Build and install

Requires a .NET 11 SDK and the extracted standard Meitou release. Copy `Meitou.local.props.example` to `Meitou.local.props` and set `MeitouDirectory` to the folder containing `MeitouClient.dll`. The local file is gitignored.

```sh
dotnet build KenshiOrigins.csproj -c Release
python3 scripts/package.py
```

Extract `artifacts/KenshiOrigins-0.4.1.zip` into your **Kenshi installation's `mods` directory**. Enable KenshiOrigins in the launcher and restart the client. Keep `KenshiOrigins.mod`, `meitou.json`, `KenshiOrigins.dll` and `originform.json` together in `mods/KenshiOrigins/`.

For saves made before 0.4.1, **load the original save normally and save it again with this version before importing**. That save adds the per-character origin and outstanding-equipment fields which survive import. Importing an older, unmigrated save directly still lacks those fields; already-rerolled imports cannot reconstruct the old choices without a pre-import save.

The package script also creates `artifacts/ExtraOrigins-example.zip`. Extract it into the same `mods` directory and enable it after KenshiOrigins to try the sample Dockworker origin. Its JSON includes all supported fields as a template for your own pack. Neither ZIP includes game DLLs.

For local testing, build, install and enable the main mod with one command:

```sh
python3 scripts/deploy.py /path/to/Kenshi/mods
```

It verifies installed file hashes and preserves the existing enabled-mod order. Previous installed files and the changed `mods.cfg` are backed up under `artifacts/deploy-backups/`. Restart MeitouClient after deploying code changes.

## Add origins

See the [origin pack guide](ORIGIN_GUIDE.md) for a complete JSON example, every field's meaning and default, item rules, and save behavior. It is also included in both distribution ZIPs.

Find equipment IDs in the [vanilla item reference](VANILLA_ITEMS.md), grouped by type with matching crossbow ammunition, or use the [CSV list](VANILLA_ITEMS.csv) in a spreadsheet. Both are bundled with the mod and example pack. The reference is extracted from the four core game data files without loading enabled mods.

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
      "startingCharactersOnly": false,
      "testingOnly": false,
      "name": "Traveller",
      "description": "A life on the road.",
      "difficulty": "Standard",
      "tags": ["Travel", "Survival"],
      "bonuses": { "Athletics": 5, "Swimming": 3 },
      "items": [
        { "record": "42334-changes_otto.mod", "quantity": 2, "quality": 0 }
      ]
    }
  ]
}
```

Keep IDs stable and use your pack's prefix. Stat keys are names from Meitou's `StatsEnumerated` enum; numeric keys, unsupported stats and shared-storage duplicates are rejected. Unknown fields and malformed definitions produce a log message. An invalid optional pack is skipped as a whole, and duplicate IDs never replace earlier definitions. No second marker JSON is needed.

`tags` is an optional array of category strings. Any pack can introduce new categories; the horizontal tag row gathers them from all loaded origins. Whitespace is trimmed and duplicates are collapsed without regard to case; blank or non-string tags are rejected. Omit it or use `[]` for an untagged origin, which remains available under **All tags** and in search. Tags affect browsing only. Display sorting and filtering never reorder the catalog used by creation drafts, random assignment and saves.

`startingCharactersOnly` is an optional boolean defaulting to `false`. Set it to `true` to allow an origin only for the initial Game Start roster: it remains in the creation selector, random defaults and creation's **Random** pool unless also marked `testingOnly`, but later recruits and player spawns cannot roll it. Use JSON booleans (`true` / `false`), without quotes. Existing saved assignments remain valid if you change the flag. Installing on an existing save uses the recruit pool for unassigned characters. If every loaded origin is start-only, those characters receive no origin and the log explains the empty pool. The same roster tracking supports vanilla and modded Game Starts.

`testingOnly` is an optional boolean defaulting to `false`. When `true`, the origin's text is red in the selector and descriptions and it requires manual selection. It is excluded from both starting random pools and later recruitment, regardless of `startingCharactersOnly`. Existing saved assignments remain valid. This uses native row colours and text properties, so names and IDs stay unchanged. If all loaded origins are testing-only, no automatic origin is assigned; starting characters must choose manually. In the bundled pool Nobody and The Unfair One have `startingCharactersOnly: true`, and The Unfair One additionally has `testingOnly: true`.

`difficulty` is optional free text, displayed in creation and stats without affecting gameplay. It defaults to `Unrated`; the bundled `Easy` / `Standard` labels describe the relative starting challenge and can be rewritten freely. `items` is optional and defaults to an empty list, so existing packs continue to work. An item's `record` is its stable FCS record ID, not its display name. The `-otto.mod` suffix in a vanilla ID is part of that ID; it does not mean a separate mod must be installed. Quantity defaults to 1 (range 1–1000); quality defaults to 0 (range 0–100). Weapon quality uses the engine's craft-grade lookup; crossbows, armour and limbs use their level. Ordinary items keep their record-defined quality. For ammunition, quantity counts inventory items/packs rather than individual shots. Missing or unsupported item records are logged and skipped while valid grants continue.

Equipment is queued once when an initial-roster character's origin is finalized. `OriginSystem` reuses its existing `_startingChoices` roster to make that decision before removing the temporary entry; no extra roster state is introduced. Delivered quantities and outstanding quantities persist with the character's lasting ID. Existing version-2 equipment queues resume exactly as saved, including queues written by v0.2.0, whose saves do not distinguish starters from recruits. No new grants are inferred on load. Version-1 saves from v0.1.x keep their origins and bonuses but receive no retroactive equipment, because they do not identify the initial roster. Installing on an existing save likewise grants origins and bonuses only. Later edits to an origin's item list do not grant another set to an already assigned character. The JSON content format stays at version 1; the internal save format stays at version 2.

Normal loads still restore the existing sidecar by lasting ID. The mod also writes `kenshiorigins.origin` and `kenshiorigins.pendingItems` as strings in each character's STATS record through `world.SaveFields`. Import drops sidecars and lasting IDs but retains these character records. The mod restores the origin and exact outstanding queue before considering random assignment, then gives the character its new lasting ID. It never matches names or recreates delivered equipment. Unknown origin IDs remain unavailable rather than being rerolled. Invalid queue metadata is logged without inventing replacement equipment.

## Read the code

- `src/OriginsMod.cs`: the entry point. Registers the world system and narrow Harmony hooks.
- `src/OriginCatalog.cs`: finds dependent packs and validates their JSON. Content loading stays separate from gameplay.
- `src/OriginDefinition.cs`: describes an origin without putting character-specific state in it.
- `src/OriginEquipment.cs`: makes item instances and puts them into storage, returning only quantities that still need space.
- `src/OriginSystem.cs`: remembers the starting roster, waits for creation to finish, resolves Random, assigns origins and restores modifiers. The save stores stable origin IDs and pending equipment against lasting character IDs, with per-character fields as the import-safe backup.
- `src/OriginProgression.cs`: calculates room for the origin's bonus and adjusts native XP using the boosted skill level. The XP callback only reads state, so it can run on simulation worker threads.
- `src/OriginBrowser.cs`: searches and sorts the displayed origins and gathers their tags without mutating the underlying catalog.
- `src/OriginCreationPanel.cs`: extends the existing description area with a mode switch, native search and selector, horizontal category buttons and scrolling details. Each origin selection updates the current draft through the engine's setter, so Confirm includes it; browsing filters do not edit drafts.
- `src/OriginUi.cs`: displays assigned origins in stats. Hooks preserve the chosen origin across cached race/gender looks, appearance reset/import, and handle recruitment or confirmation while paused.

Bonuses use one named modifier source. Setting that source replaces its previous amount, so repeated updates do not stack bonuses. Trained stats remain unchanged. Modifiers are rebuilt from the saved origin ID after loading.

## Checks

On Linux, run the headless checks with the release's own bundled runtime and Harmony:

```sh
python3 scripts/check.py /path/to/Meitou/versions/0.24.0
```

They exercise dependent-pack discovery, validation, multiple custom starting squads, start-only eligibility (including Random, empty recruit pools and saved assignments), deferred starter equipment, recruits and later player spawns receiving bonuses without gear, partial inventory delivery, backpack storage, save migration, replacement stats, persistence with changed runtime IDs, missing definitions, paused confirmation, real editor/stats hooks, deterministic hashes and the real mod loader in an isolated process. They use synthetic game data and do not modify the installed game or saves.

The interaction checks use a fixed fixture so story and balance edits do not alter their assumptions. The current authored definitions are checked separately through real assignment, inventory delivery, recruitment and save/load; The Unfair One's full +75/+50 stat coverage is checked against the installed enum. Testing-only origins are also checked for manual selection, random exclusion and red native UI text.

Expansion checks verify the exact eighteen-origin roster, official tag assignments, new random-pool membership, nonnegative bonuses and preservation of the previous thirteen definitions except their tags. Saves made with that previous catalog are loaded with the expanded catalog to check stable assignments, non-stacking bonuses and no retroactive equipment grants.

Persistence checks use the actual save capture, on-disk writer/reader, mod sidecar envelope, loader and import filter in temporary worlds. Four characters share a name but have different origins; one has queued equipment. Three normal roundtrips and four imports check unchanged trained values, single capped modifier sets, restricted-origin retention, old-save field migration, consumed items and exact remaining deliveries. They also compare capped Toughness against the real coma and damage formulas at 100. Run these alone with `--audit-origins`; the observations are written to `artifacts/origin-024-audit.json`.

Progression checks exercise real bound XP calls, racial and per-skill XP multipliers, medical and mass-combat gains, dummy limits, repeated gains without a refresh, testing origins, legacy trained values over 100, changes from other modifiers and identical XP state with one and four worker threads.

To validate another candidate without deploying or replacing the bundled JSON:

```sh
python3 scripts/check.py /path/to/Meitou/versions/0.24.0 --ui-root /path/to/Kenshi --validate-origins /path/to/originform.json
```

Pass `--ui-root /path/to/Kenshi` to exercise the installed native editor layout and templates as well: mouse/keyboard dropdown selection including Random, search typing and Enter handling, horizontal tag scrolling and filtering, automatic draft saving, character/view switching, difficulty labels, scrolling and panel bounds at three resolutions. It also checks all bundled item IDs against the installed vanilla database and verifies the Gunner's ammunition. These checks use the game's screen router, including its disabled-by-default button bindings, and read assets without copying them into the project.

Add `--core-ui` to exercise the actual expanded official catalog in the native editor, including all five categories, either-category matching, alphabetical sorting, search, new-origin selection, the cheat warning and tag-button bounds. Add `--extra-pack-ui` alongside it to include the example pack and check its custom category through the same native controls.

Add `--ui-theme /path/to/Dark-UI-mod` to test an installed theme's layouts and skins over the base assets. Add `--render-ui` to render a preview through the release's actual Vulkan painter, fonts and textures; this requires a Vulkan device and writes `artifacts/origin-ui.png` (or `origin-ui-theme.png` with a theme). The browser follows the theme's sidebar anchors and uses compact UI-scaled rows, a native Kenshi horizontal scrollbar, and a single caption for the origin selector.

To regenerate the two empty `.mod` headers using Meitou's own writer:

```sh
python3 scripts/check.py /path/to/Meitou/versions/0.24.0 --generate
```

To regenerate the bundled vanilla item reference after a game-data update:

```sh
python3 scripts/check.py /path/to/Meitou/versions/0.24.0 --ui-root /path/to/Kenshi --export-vanilla-items
```

This export reads only the fixed core file list, validates each listed record through the installed release's item factory, checks bundled origin grants are covered, and writes `VANILLA_ITEMS.md` and `VANILLA_ITEMS.csv`. It does not modify the installation or run gameplay checks. Source version and file hashes are recorded in the reference.

## Prototype limits

- Animals receive random origins, but the game's animal editor only supports renaming, so it offers no manual origin selector.
- Installing on an existing save assigns random origins to player characters without saved origin data. Imports preserve fields saved by 0.4.1 or later; older saves need one normal load-and-save first.
- Preexisting trained levels or other mods can still exceed 100; Origins preserves them and contributes zero where no room remains. Its cap applies before equipment, injury and age multipliers. Other mods that directly edit trained stats or modifiers take effect in the cap calculation at the next serial refresh.
- If a saved origin's pack is missing, its ID is retained and displayed as unavailable, with no origin bonuses. Restoring the pack restores its bonuses on the next load. Changes to an origin definition take effect after restarting and loading.
- No quirks or race-specific origin pools yet. Creation's random selection excludes testing-only definitions; later characters also exclude start-only origins. Debt, past crimes and similar story details are flavour text without additional mechanics.
- The UI hooks target MeitouClient 0.24.0. Native controls and rendered previews are checked; full vanilla/modded Game Start playtesting still needs an in-game pass.

API reference: [Meitou documentation at the target build](https://github.com/VlcekM/MeitouClient/blob/v0.24.0/docs/modding.md).
