# Making a Kenshi Origins pack

You can add origins by editing `originform.json`; no C# is needed. Start with the supplied `ExtraOrigins-example.zip`, extract it into Kenshi's `mods` folder, and enable **ExtraOrigins** after **KenshiOrigins**. The example's `.mod` already lists `KenshiOrigins.mod` as its master dependency. Your own pack must also list that direct dependency.

Keep your pack's `.mod` and `originform.json` together. Edit the JSON, then restart Meitou to load your changes.

## A complete example

This example includes every supported field. Add more objects inside `origins`, separated by commas, to define more origins.

```json
{
  "formatVersion": 1,
  "origins": [
    {
      "id": "extraorigins.dockworker",
      "startingCharactersOnly": false,
      "testingOnly": false,
      "name": "Dockworker",
      "description": "Shifting cargo kept you strong.\n\nThe road offers a different life.",
      "difficulty": "Standard",
      "tags": ["Civilian", "Labouring"],
      "bonuses": { "Labouring": 3, "Swimming": 2 },
      "items": [
        { "record": "42334-changes_otto.mod", "quantity": 2, "quality": 0 }
      ]
    }
  ]
}
```

## File fields

| Field | Required | What it does |
| --- | --- | --- |
| `formatVersion` | Yes | The JSON format version. Use the number `1`. |
| `origins` | Yes | A nonempty array containing your origin definitions. |

## Origin fields

| Field | Required or default | What it does |
| --- | --- | --- |
| `id` | Required | A unique, permanent identifier, such as `extraorigins.dockworker`. Use lowercase letters, numbers, underscores or hyphens for the origin name after the dot. Give your pack its own prefix to avoid collisions. Saves refer to this ID, so keep it stable when changing the story or displayed name. |
| `name` | Required | The nonblank name shown in the selector and character stats, such as `Dockworker`. |
| `description` | Required | The story shown during creation and in character stats. Use `\n` for a line break or `\n\n` for a new paragraph. An empty string is allowed. |
| `difficulty` | `"Unrated"` | Any label describing the starting challenge, such as `Easy`, `Standard` or `Hard`. It is informational and does not change bonuses or random chances. |
| `tags` | `[]` | Category strings for the horizontal tag menu and search. You can invent categories. Spaces around tags are trimmed and repeated tags are combined without regard to case. Blank tags are invalid. Tags do not restrict who can receive an origin. |
| `startingCharactersOnly` | `false` | Set to `true` to reserve this origin for the initial Game Start roster. It stays available in the creation selector, random defaults and creation's **Random** option. Later recruits and player spawns cannot roll it. |
| `testingOnly` | `false` | Set to `true` for cheat or testing origins. They remain in the selector for manual choice, but are excluded from random defaults, creation's **Random** option, and recruit rolls. Their selector entry, description and character stats text are red. This rule takes priority over `startingCharactersOnly`. |
| `bonuses` | Required | An object mapping stat names to additive numbers. For example, `"Strength": 3` gives up to +3 Strength, limited by room below 100. Decimals are allowed. Use nonnegative amounts with the current MeitouClient; the official pack avoids negative modifiers. Use `{}` for no bonuses. |
| `items` | `[]` | An array of inventory grants. **Only initial-roster characters receive these**, after creation is confirmed, even when `startingCharactersOnly` is `false`. Use `[]` for no equipment. |

Common stat keys include `Strength`, `Athletics`, `Swimming`, `Farming`, `Labouring`, `Engineering`, `Science`, `Stealth`, `Thieving`, `Assassination`, `MeleeAttack`, `MeleeDefence`, `Crossbows` and `Turrets`. Keys come from Meitou's `StatsEnumerated` enum; use their names without spaces. Unsupported stats and two keys sharing the same underlying stat storage are rejected. Bonuses use a named modifier, so repeated updates do not stack them or change trained stat values.

The eighteen official origins use five broad tags: **Combat**, **Civilian**, **Technical**, **Survival** and **Special**. Choose a category to see origins carrying that tag; an origin with two tags appears under either category. **All tags** clears the category filter and is not a tag to add to your JSON. Your own pack can introduce any category strings. This guide's example deliberately adds **Labouring**, showing how a third-party category joins the menu. Old official tags do not remain unless a loaded pack explicitly uses them.

Origins have equal chances within the eligible random pool; there is no weight field. With both eligibility flags omitted or `false`, an origin can be rolled for starters and later characters. If every loaded origin is start-only or testing-only, later characters receive no origin and the log explains why. If all are testing-only, starters must choose manually; Random assigns nothing. The roster rule works with vanilla and modded Game Starts.

Use `Medic` for the medical skill, `SmithingWeapon` for weapon smithing and `SmithingArmour` for armour smithing. `FieldMedic`, `WeaponSmithing`, `ArmourSmithing` and `Splinting` are not enum keys in the target release; there is no separate Splinting stat.

The supported distinct stat keys are:

| Group | Keys |
| --- | --- |
| Attributes and movement | `Strength`, `Toughness`, `Dexterity`, `Perception`, `Athletics`, `Swimming` |
| Combat | `MeleeAttack`, `MeleeDefence`, `Katanas`, `Sabres`, `Hackers`, `HeavyWeapons`, `Blunt`, `Polearms`, `MartialArts`, `Dodge`, `Crossbows`, `Turrets`, `FriendlyFire`, `Assassination`, `MassCombat` |
| Stealth and crime | `Stealth`, `Thieving`, `Lockpicking` |
| Work and support | `Labouring`, `Science`, `Engineering`, `Robotics`, `SmithingWeapon`, `SmithingArmour`, `SmithingBow`, `Medic`, `Farming`, `Cooking` |

`MassCombat` has storage in this build but is not a normal visible FCS skill. `HiveMedic` and `Vet` share `Medic` storage, so use `Medic` once. `None`, `Weapons`, `Survival` and `End` cannot receive bonuses.

## Item fields

Each object inside `items` accepts these fields:

Look up IDs in the bundled [vanilla item reference](VANILLA_ITEMS.md), or open [VANILLA_ITEMS.csv](VANILLA_ITEMS.csv) in a spreadsheet. Crossbow rows also name their matching ammunition. Use the complete ID, including its suffix.

| Field | Required or default | What it does |
| --- | --- | --- |
| `record` | Required | The item's stable FCS record ID, not its display name. The example uses Dried Meat. Copy the record's ID from FCS; keep suffixes such as `-otto.mod`, which can be part of vanilla IDs. For a modded item, its supplying mod must also be loaded. |
| `quantity` | `1` | A whole number from `1` to `1000`. For ammunition, this counts inventory items or packs, not individual shots. |
| `quality` | `0` | A whole number from `0` to `100`. Weapons use the engine's craft-grade lookup; crossbows, armour and limbs use their level. Ordinary supplies such as food keep their record-defined quality. |

Equipment goes into the main inventory or an existing backpack's storage. It is never equipped or used to replace existing gear. Quantities that do not fit remain in a saved queue until space becomes available. Missing or unsupported item records are logged and skipped; other valid grants still proceed. Later recruits receive bonuses only, never this equipment.

## Editing and troubleshooting

Use double quotes for strings, unquoted `true` / `false` for booleans, and no comments or trailing commas. Optional fields can be omitted to use their defaults. Unknown fields and duplicate origin IDs are rejected; an invalid optional pack is skipped as a whole. Check the Meitou log if your pack does not appear.

Saved characters keep their assigned origin IDs. Changing either eligibility flag affects future rolls, so existing assignments remain valid. Story and bonus edits take effect after restarting and loading; editing the item list does not grant another set to characters already assigned an origin. Installing on an existing save uses the recruit pool for characters without an origin and grants no starting equipment.

With Kenshi Origins 0.4.1 on Meitou 0.24.0, the origin ID and outstanding item queue also travel in the character's saved record, preserving them through import. For older saves, load normally and save once with 0.4.1 before importing. Already-rerolled imports need a pre-import save to recover their old choices. Bonuses are temporary modifiers, saved separately from trained skill values, so restoring an origin does not add it again to trained stats.

Positive bonuses, including testing origins, are capped by room below 100. Trained 90 plus a configured +25 receives +10. Existing trained stats and other mods' bonuses are preserved even if already above 100; Origins then adds zero. Equipment, injury and age multipliers keep their native behavior. Configured JSON amounts remain unchanged and the description lists those configured amounts. XP difficulty uses the boosted level, retaining the engine's normal curve and separate medical formula; training in an origin-boosted skill stops at boosted 100. No extra JSON fields are needed.
