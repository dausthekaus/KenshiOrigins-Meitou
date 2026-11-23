# Vanilla item IDs for Kenshi Origins

Extracted from **Kenshi 1.0.65 - x64 (Newland)**: **568 inventory item records**. Names are the English database names. Use Ctrl+F to find an item, then copy its full ID into `record`. [CSV version](VANILLA_ITEMS.csv) · [Origin field guide](ORIGIN_GUIDE.md).

Only `gamedata.base`, `Newwworld.mod`, `Dialogue.mod` and `rebirth.mod` were loaded, in that order. These are vanilla core files despite the `.mod` suffixes. No enabled mods, Workshop files or translations were loaded. Removed records are excluded.

```json
"items": [
  { "record": "42334-changes_otto.mod", "quantity": 2, "quality": 0 }
]
```

This example gives two Dried Meat. Insert the `items` field inside an origin object. Starting items go into inventory only for the initial Game Start roster after creation is confirmed.

Different records can share a name: copy the ID from the intended row. This reference includes vanilla templates and unused records as well as ordinary obtainable items; a listing does not mean an item appears in normal gameplay. Blueprint templates do not select a particular research recipe through the current origin JSON. Inventory dimensions still determine whether a grant fits; oversized items can remain queued.

## Categories

- [Armour and clothing](#armour-and-clothing) (142)
- [Backpacks](#backpacks) (16)
- [Crossbows](#crossbows) (7)
- [Maps](#maps) (18)
- [Melee weapons](#melee-weapons) (48)
- [Prosthetic limbs](#prosthetic-limbs) (28)
- [Supplies and other items](#supplies-and-other-items) (309)

## Armour and clothing

| Item name | Record ID | Notes |
| --- | --- | --- |
| Ancient Samurai Armour | `1533508-Newwworld.mod` |  |
| Ancient Samurai Boots | `1533509-Newwworld.mod` |  |
| Ancient Samurai Helmet | `1533510-Newwworld.mod` |  |
| Ancient Samurai Legplates | `1533511-Newwworld.mod` |  |
| Armoured Face Plates | `18962-gamedata.base` |  |
| Armoured Hood | `2200-gamedata.base` |  |
| Armoured Rag Skirt | `5346-armor_otto.mod` |  |
| Armoured Rag Skirt (colored) | `42260-rebirth.mod` |  |
| Armoured Rags | `2307-clothes_v1.mod` |  |
| Armoured Rags (colored) | `42052-rebirth.mod` |  |
| Ashland Hat | `55393-rebirth.mod` |  |
| Ashlander Stormgoggles | `55395-rebirth.mod` |  |
| Assassin's Rags | `2154-gamedata.base` |  |
| Bandana | `2222-chris_r.mod` |  |
| Basket Hat | `45556-changes_otto.mod` |  |
| Black Bandana | `98563-rebirth.mod` |  |
| Black Cloth Shirt | `18917-gamedata.base` |  |
| Black Plate Jacket | `53299-rebirth.mod` |  |
| Black Rag Shirt | `42060-rebirth.mod` |  |
| Black Rag Shirt copy | `63234-Newwworld.mod` |  |
| Blackened Chain Hive Shirt | `1534061-Newwworld Plus.mod` |  |
| Blackened Chain Shirt | `577-gamedata.base` |  |
| Blackened Chainmail | `2211-gamedata.base` |  |
| Blackened Chainmail Tagelmust | `1533852-rebirth.mod` |  |
| Bucket Zukin | `2225-chris_r.mod` |  |
| Cap | `2167-gamedata.base` |  |
| Cargopants | `550-gamedata.base` |  |
| Cargopants (colored) | `50391-rebirth.mod` |  |
| Cargopants [padded] | `1394-gamedata.base` |  |
| Cargopants [reinforced] | `1393-gamedata.base` |  |
| Cargopants [sneaky chain] | `1392-gamedata.base` |  |
| Chain Shirt | `544-gamedata.base` |  |
| Chainmail | `2210-gamedata.base` |  |
| Cloth Shirt | `1168-gamedata.base` |  |
| Conscript Leather Armour | `56042-rebirth.mod` |  |
| Crab Armour | `64892-Newwworld.mod` |  |
| Crab Helmet | `64888-Newwworld.mod` |  |
| Crab Shoes | `64890-Newwworld.mod` |  |
| Crab Trousers | `64891-Newwworld.mod` |  |
| Dark Leather Shirt | `2214-gamedata.base` |  |
| Drifter's Boots | `2306-clothes_v1.mod` |  |
| Drifter's Leather Jacket | `2304-clothes_v1.mod` |  |
| Drifter's Leather Pants | `2305-clothes_v1.mod` |  |
| Dustcoat | `3061-gamedata.base` |  |
| Dyed Rag Shirt | `51642-rebirth.mod` |  |
| Dyed Robes | `51707-Dialogue.mod` |  |
| Dyed Trousers | `51746-Dialogue.mod` |  |
| Dyed Turban | `54582-rebirth.mod` |  |
| Dyed Turtleneck | `51643-rebirth.mod` |  |
| Empire Samurai Armour | `51708-rebirth.mod` |  |
| Faulty Prisoner Shackles | `5255-gamedata.base` |  |
| Flared Helmet | `2198-gamedata.base` |  |
| Fog Mask | `18029-small_changes_otto.mod` |  |
| Gi | `564-gamedata.base` |  |
| Gi Pants | `2193-gamedata.base` |  |
| Hachigane | `2224-chris_r.mod` |  |
| Hack Stopper Jacket | `64906-Newwworld.mod` |  |
| Hack Stopper Pants | `64907-Newwworld.mod` |  |
| Haircut21_attachment | `2711-clothes_v2.mod` |  |
| Haircut22_attachment | `2712-clothes_v2.mod` |  |
| Haircut23_attachment | `2713-clothes_v2.mod` |  |
| Halfpants (colored) | `1273-gamedata.base` |  |
| Halfpants (colored) | `64492-rebirth.mod` |  |
| Halfpants (ragged) | `551-gamedata.base` |  |
| Halfpants [padded] | `554-gamedata.base` |  |
| Halfpants [reinforced] | `1311-gamedata.base` |  |
| Halfpants [sneaky chain] | `1395-gamedata.base` |  |
| Heart Protector | `2156-gamedata.base` |  |
| Hive Prisoner Shackles | `SHACKLES-BODY` |  |
| Hiver Chain Shirt | `1534053-Newwworld Plus.mod` |  |
| Holy Chest Plate | `2166-gamedata.base` |  |
| Holy Servant Rags | `42021-rebirth.mod` |  |
| Human Skin | `63744-Newwworld.mod` |  |
| Human Skin Suit | `63235-Newwworld.mod` |  |
| Iron Hat | `2227-chris_r.mod` |  |
| Karuta Zukin | `2228-chris_r.mod` |  |
| Kusari Zukin | `2229-chris_r.mod` |  |
| Leather Hive Vest | `1534050-Newwworld.mod` |  |
| Leather Shirt | `1169-gamedata.base` |  |
| Leather Turtleneck | `2213-gamedata.base` |  |
| Leather Vest | `2212-gamedata.base` |  |
| Longcoat | `2165-gamedata.base` |  |
| Martial Artist Bindings | `2326-gamedata.base` |  |
| Mask Type III | `18034-small_changes_otto.mod` |  |
| Masked Helmet | `2201-gamedata.base` |  |
| Mercenary Leather Armour | `2283-gamedata.base` |  |
| Mercenary Plate | `2148-gamedata.base` |  |
| Monk Pants | `2188-gamedata.base` |  |
| Monk Robe | `2189-gamedata.base` |  |
| Ninja Gi | `2197-gamedata.base` |  |
| Ninja Mask | `2221-chris_r.mod` |  |
| Ninja Pants | `2194-gamedata.base` |  |
| Ninja Rags | `2151-gamedata.base` |  |
| Noble's Robe | `51663-Newwworld.mod` |  |
| Noble's Trousers | `51662-Newwworld.mod` |  |
| Padded Leather Jacket | `1534057-Newwworld Plus.mod` |  |
| Paladin's Heavy Hachigane | `2226-chris_r.mod` |  |
| Plate Jacket | `2182-gamedata.base` |  |
| Plated Drifter's Leather Pants | `1532813-Newwworld.mod` |  |
| Plated Longboots | `556-gamedata.base` |  |
| Police Helmet | `2180-gamedata.base` |  |
| Prisoner Shackles | `SHACKLES` |  |
| Rag Loincloth | `2308-clothes_v1.mod` |  |
| Rag Loincloth (Dyed) | `64923-rebirth.mod` |  |
| Rag Shirt | `2309-clothes_v1.mod` |  |
| Rattan Hat | `2185-gamedata.base` |  |
| Red Bandana | `98562-rebirth.mod` |  |
| REFERENCE | `REFERENCEARMOUR` |  |
| Rusted Hive Shirt | `1534062-Newwworld Plus.mod` |  |
| Rusty Chain Shirt | `18018-small_changes_otto.mod` |  |
| Rusty Chainmail | `18019-small_changes_otto.mod` |  |
| Rusty Chainmail Tagelmust  | `1532828-Newwworld.mod` |  |
| Samurai Armour | `2122-gamedata.base` |  |
| Samurai Boots | `549-gamedata.base` |  |
| Samurai Clothpants | `2159-gamedata.base` |  |
| Samurai Helmet | `2145-gamedata.base` |  |
| Samurai Legplates | `2150-gamedata.base` |  |
| Sandogasa | `1534055-Newwworld Plus.mod` |  |
| Side-Angle Hachigane | `2223-chris_r.mod` |  |
| Skeleton Mask | `62795-Newwworld.mod` |  |
| Sleeveless Dustcoat | `62178-rebirth.mod` |  |
| Sleeveless Longcoat | `684-gamedata.base` |  |
| Spiked Helmet | `2203-gamedata.base` |  |
| Square Goggles | `2168-gamedata.base` |  |
| Stout Hessian | `18911-gamedata.base` |  |
| Stout Hessian Uniform | `2169-gamedata.base` |  |
| Straw Hat | `575-gamedata.base` |  |
| Swamp Ninja Mask | `18033-small_changes_otto.mod` |  |
| Swamp Ninja Rags | `51640-rebirth.mod` |  |
| Tagelmust | `2220-chris_r.mod` |  |
| Tin Can | `2149-gamedata.base` |  |
| Trader's Leathers | `2163-gamedata.base` |  |
| Tricorn Hat | `1415-gamedata.base` |  |
| Turban | `2219-chris_r.mod` |  |
| Turtleneck | `543-gamedata.base` |  |
| Unholy Chest Plate | `1533849-rebirth.mod` |  |
| Visored Helmet | `2202-gamedata.base` |  |
| White Plate Jacket | `53301-rebirth.mod` |  |
| White Vest | `1167-gamedata.base` |  |
| Wooden Sandals | `557-gamedata.base` |  |
| Wool Hat | `2164-gamedata.base` |  |
| Worn-out Shorts | `2158-gamedata.base` |  |

## Backpacks

| Item name | Record ID | Notes |
| --- | --- | --- |
| Bull Backpack | `43948-rebirth.mod` |  |
| Garru Backpack | `4002-gamedata.base` |  |
| Large Backpack | `1012-gamedata.base` |  |
| Medium Backpack | `635-gamedata.base` |  |
| Medium backpack (colored) | `1286-gamedata.base` |  |
| Scavenger's Basket | `45554-changes_otto.mod` |  |
| Shopkeeper goods bag | `1389-gamedata.base` |  |
| Shopkeepers Backpack | `910-gamedata.base` |  |
| Small Backpack | `576-gamedata.base` |  |
| Small Backpack (colored) | `46037-rebirth.mod` |  |
| Small Thieves Backpack | `1288-gamedata.base` |  |
| Thieves Backpack | `46036-rebirth.mod` |  |
| Trader's Wooden Backpack | `45555-changes_otto.mod` |  |
| Traders Backpack Large | `1499-gamedata.base` |  |
| Traders Backpack Medium | `1498-gamedata.base` |  |
| Wooden Backpack | `46042-rebirth.mod` |  |

## Crossbows

| Item name | Record ID | Notes |
| --- | --- | --- |
| Eagle's Cross | `66290-Newwworld.mod` | Ammo: Bolts [Longs] (96000-Newwworld.mod) |
| Junkbow | `95724-Dialogue.mod` | Ammo: Bolts [Toothpicks] (96014-rebirth.mod) |
| Oldworld Bow MkI | `65870-rebirth.mod` | Ammo: Bolts [Regulars] (95781-rebirth.mod) |
| Oldworld Bow MkII | `53643-rebirth.mod` | Ammo: Bolts [Regulars] (95781-rebirth.mod) |
| Ranger | `66169-Newwworld.mod` | Ammo: Bolts [Regulars] (95781-rebirth.mod) |
| Spring Bat | `95764-rebirth.mod` | Ammo: Bolts [Heavies] (95872-Newwworld.mod) |
| Tooth Pick | `66534-Newwworld.mod` | Ammo: Bolts [Toothpicks] (96014-rebirth.mod) |

## Maps

| Item name | Record ID | Notes |
| --- | --- | --- |
| Ancient Military Documents | `49358-rebirth.mod` |  |
| armouries | `49944-rebirth.mod` |  |
| Bugmaster's Map | `1533877-rebirth.mod` |  |
| Engineers Map | `1532843-rebirth.mod` |  |
| Glorious Holy Map | `44915-rebirth.mod` |  |
| Map of the Ashlands | `1533730-rebirth.mod` |  |
| Map of the Border Zone | `44919-rebirth.mod` |  |
| Map of the Shek Kingdom | `51403-rebirth.mod` |  |
| Map of the Swamps | `51404-rebirth.mod` |  |
| Map of the United Cities (Central Territories) | `96157-rebirth.mod` |  |
| Map of the United Cities (Extended Territories) | `96158-rebirth.mod` |  |
| Map of Vain | `44918-rebirth.mod` |  |
| Map to Mongrel | `44917-rebirth.mod` |  |
| Map to World's End | `51406-rebirth.mod` |  |
| Old Treasure Map | `48693-rebirth.mod` |  |
| Secret Holy Nation Documents | `51410-rebirth.mod` |  |
| Tech Hunter Library Map | `51405-rebirth.mod` |  |
| Tech Hunters Map | `1532844-rebirth.mod` |  |

## Melee weapons

| Item name | Record ID | Notes |
| --- | --- | --- |
| Bull Horn Axe | `56729-Dialogue.mod` |  |
| Combat Cleaver | `52292-rebirth.mod` |  |
| Desert Sabre | `52305-rebirth.mod` |  |
| Exile Plank | `56728-Dialogue.mod` |  |
| Falling Sun | `52306-rebirth.mod` |  |
| FISTS | `FISTS` |  |
| FISTS- robo | `53668-rebirth.mod` |  |
| Flesh Cleaver | `2606-gamedata.base` |  |
| Foreign Sabre | `52304-rebirth.mod` |  |
| Fragment Axe | `477-gamedata.base` |  |
| Guardless Katana | `923-gamedata.base` |  |
| Heavy Jitte | `52290-rebirth.mod` |  |
| Heavy Polearm | `52307-rebirth.mod` |  |
| Holed Sabre | `52287-rebirth.mod` |  |
| Horse Chopper | `475-gamedata.base` |  |
| Iron Club | `52295-rebirth.mod` |  |
| Iron Stick | `2064-gamedata.base` |  |
| Jitte | `52303-rebirth.mod` |  |
| Katana | `476-gamedata.base` |  |
| Long Cleaver | `478-gamedata.base` |  |
| Longsword | `901-gamedata.base` |  |
| Mercenary Club | `2110-gamedata.base` |  |
| Moon Cleaver | `52300-rebirth.mod` |  |
| Naginata | `52308-rebirth.mod` |  |
| Naginata Katana | `52309-rebirth.mod` |  |
| Ninja Blade | `924-gamedata.base` |  |
| Nodachi | `922-gamedata.base` |  |
| Paladin's Cross | `52299-rebirth.mod` |  |
| Plank | `902-gamedata.base` |  |
| Polearm | `52301-rebirth.mod` |  |
| Ringed Sabre | `903-gamedata.base` |  |
| Short-Cleaver | `65260-rebirth.mod` |  |
| Spiked Club | `52297-rebirth.mod` |  |
| Staff | `52302-rebirth.mod` |  |
| Topper | `474-gamedata.base` |  |
| Wakizashi | `1020-gamedata.base` |  |
| _bull type | `46990-rebirth.mod` |  |
| _bull type weak | `46991-rebirth.mod` |  |
| _dog type weapon | `3982-gamedata.base` |  |
| _duck type weapon | `43887-rebirth.mod` |  |
| _elephant type | `3968-gamedata.base` |  |
| _garru | `56124-rebirth.mod` |  |
| _giraffe type | `3934-gamedata.base` |  |
| _gorillo | `43897-rebirth.mod` |  |
| _robot spider type | `42075-gamedata.base` |  |
| _skimmer | `56092-rebirth.mod` |  |
| _spider type | `43883-rebirth.mod` |  |
| _spider type small | `50853-rebirth.mod` |  |

## Prosthetic limbs

| Item name | Record ID | Notes |
| --- | --- | --- |
| Economy Arm (left) | `96134-Newwworld.mod` |  |
| Economy Arm (right) | `96137-Newwworld.mod` |  |
| Economy Leg (left)  | `96136-Newwworld.mod` |  |
| Economy Leg (right) | `96135-Newwworld.mod` |  |
| HIVE Left Arm Stump | `96798-Newwworld.mod` |  |
| Hive Left Leg | `96796-Newwworld.mod` |  |
| HIVE Right Arm Stump | `96799-Newwworld.mod` |  |
| Hive Right Leg | `96797-Newwworld.mod` |  |
| Human Left Arm Stump | `95510-Newwworld.mod` |  |
| Human Left Leg | `95515-Newwworld.mod` |  |
| Human Right Arm Stump | `95513-Newwworld.mod` |  |
| Human Right Leg | `95514-Newwworld.mod` |  |
| Industrial Lifter Arm (left) | `96022-Newwworld.mod` |  |
| Industrial Lifter Arm (right) | `96024-Newwworld.mod` |  |
| KLR Series Arm (left)  | `96186-Newwworld.mod` |  |
| KLR Series Arm (right) | `96187-Newwworld.mod` |  |
| KLR Series Leg (left) | `96188-Newwworld.mod` |  |
| KLR Series Leg (right)  | `96189-Newwworld.mod` |  |
| Scout Leg (left) | `96020-Newwworld.mod` |  |
| Scout Leg (right) | `96021-Newwworld.mod` |  |
| Skeleton Arm (left) | `95747-Newwworld.mod` |  |
| Skeleton Arm (right) | `95748-Newwworld.mod` |  |
| Skeleton Leg (left) | `95872-rebirth.mod` |  |
| Skeleton Leg (right) | `96022-rebirth.mod` |  |
| Steady Arm (left) | `95871-rebirth.mod` |  |
| Stealth Leg (left) | `95579-Newwworld.mod` |  |
| Stealth Leg (right) | `95742-Newwworld.mod` |  |
| Thief's Arm (right) | `95870-rebirth.mod` |  |

## Supplies and other items

| Item name | Record ID | Notes |
| --- | --- | --- |
| A Holy Purpose | `49052-Dialogue.mod` |  |
| Advanced First Aid Kit | `1359-gamedata.base` |  |
| Advanced Splint Kit | `1436-gamedata.base` |  |
| AI Core | `43392-changes_otto.mod` |  |
| Ancient Nutri-Rations | `62173-rebirth.mod` |  |
| Ancient Science Book | `43953-rebirth.mod` |  |
| Animal Claw | `42313-changes_otto.mod` |  |
| Animal Horn | `42314-changes_otto.mod` |  |
| Animal Skin | `42241-rebirth.mod` |  |
| Animal Teeth | `42315-changes_otto.mod` |  |
| Armour Plating | `2288-gamedata.base` |  |
| Authentic Skeleton Repair Kit | `97903-rebirth.mod` |  |
| Basic First Aid Kit | `209-gamedata.base` |  |
| Beautiful Tengu: A Tribute | `53652-Dialogue.mod` |  |
| Birth Of The Phoenix | `49050-Dialogue.mod` |  |
| Bloodrum | `43316-rebirth.mod` |  |
| Blueprints | `BLUEPRINT_ITEM` |  |
| Blueprints (armour) | `BLUEPRINT_ITEM_ARMOUR` |  |
| Blueprints (gear) | `BLUEPRINT_ITEM_GEAR` |  |
| Blueprints large | `2223-gamedata.base` |  |
| Bolts [Heavies] | `95872-Newwworld.mod` |  |
| Bolts [Longs] | `96000-Newwworld.mod` |  |
| Bolts [Regulars] | `95781-rebirth.mod` |  |
| Bolts [Toothpicks] | `96014-rebirth.mod` |  |
| Book | `16855-nodes_otto1.mod` |  |
| Book Of Grace | `50383-Dialogue.mod` |  |
| Book Of Sacrifice | `50382-Dialogue.mod` |  |
| Bread | `1946-gamedata.base` |  |
| Building Material | `580-gamedata.base` |  |
| Cactus | `42304-changes_otto.mod` |  |
| Cactus Rum | `1015-gamedata.base` |  |
| Capacitor | `43393-changes_otto.mod` |  |
| Captive's Journal I | `56737-Dialogue.mod` |  |
| Captive's Journal II | `56738-Dialogue.mod` |  |
| Captive's Journal III | `56739-Dialogue.mod` |  |
| Captive's Journal IV | `56740-Dialogue.mod` |  |
| Carbon | `42328-changes_otto.mod` |  |
| Cats | `54546-Newwworld.mod` |  |
| Ceramic Bowl | `42316-changes_otto.mod` |  |
| Chainmail Sheets | `2289-gamedata.base` |  |
| Chalice Of Fire | `57222-Dialogue.mod` |  |
| Chewsticks | `43956-rebirth.mod` |  |
| Coin | `54547-Newwworld.mod` |  |
| Cooked Vegetables | `43955-rebirth.mod` |  |
| Copper | `42158-gamedata.base` |  |
| Copper Alloy Plates | `42160-gamedata.base` |  |
| Cotton | `42178-rebirth.mod` |  |
| CPU of Cat-Lon | `1533843-rebirth.mod` |  |
| CPU of Cat-Lon | `1533859-rebirth.mod` |  |
| CPU of General Hat-12 | `1533669-rebirth.mod` |  |
| CPU of General Jang | `1533665-rebirth.mod` |  |
| CPU of Rhinobot | `1533517-rebirth.mod` |  |
| CPU of the Head of Agriculture | `1533516-rebirth.mod` |  |
| CPU Unit | `43394-changes_otto.mod` |  |
| Crossbow Parts | `97089-rebirth.mod` |  |
| Cultural Groupings And Evolution I | `51312-Dialogue.mod` |  |
| Cultural Groupings And Evolution II | `51313-Dialogue.mod` |  |
| Cup | `42319-changes_otto.mod` |  |
| Damaged Book | `43927-rebirth.mod` |  |
| Doll | `65078-Dialogue.mod` |  |
| Dried Fish | `50518-Newwworld.mod` |  |
| Dried Gristle Flaps | `50568-rebirth.mod` |  |
| Dried Meat | `42334-changes_otto.mod` |  |
| Dustwich | `43961-rebirth.mod` |  |
| Electrical Components | `42164-gamedata.base` |  |
| Empty Grog Can | `44429-rebirth.mod` |  |
| Empty Rum Bottle | `44428-rebirth.mod` |  |
| Engineering Research | `43951-rebirth.mod` |  |
| Fabrics | `584-gamedata.base` |  |
| Fancy Rug | `54030-Newwworld.mod` |  |
| Flotsam Safehouse Note | `49381-rebirth.mod` |  |
| Fog Prince Head | `42083-rebirth.mod` |  |
| Foodcube | `43959-rebirth.mod` |  |
| Forbidden Sympathy | `49058-Dialogue.mod` |  |
| Foul Raw Meat | `50567-rebirth.mod` |  |
| Fuel | `1514-gamedata.base` |  |
| Gears | `42318-changes_otto.mod` |  |
| Generator Core | `42189-rebirth.mod` |  |
| Gohan | `43957-rebirth.mod` |  |
| Grand Fish | `50514-Newwworld.mod` |  |
| Great White Claw | `1534121-__Fixes.mod` |  |
| Great White Skin | `98510-rebirth.mod` |  |
| Greenfruit | `42309-changes_otto.mod` |  |
| Grind Machine | `43863-changes_otto.mod` |  |
| Grog | `42311-changes_otto.mod` |  |
| Gurgler Head | `96409-Dialogue.mod` |  |
| Hacksaw | `581-gamedata.base` |  |
| Half-Eaten Ancient Science Book | `64917-rebirth.mod` |  |
| Hashish | `1230-gamedata.base` |  |
| Hemp | `1965-gamedata.base` |  |
| Hinge | `43396-changes_otto.mod` |  |
| Hoe | `46982-Newwworld.mod` |  |
| Holy Edict | `49053-Dialogue.mod` |  |
| Holy Seal | `1533445-Dialogue.mod` |  |
| Horn of the Megaraptor | `50984-rebirth.mod` |  |
| Human Teeth | `51372-rebirth.mod` |  |
| Iron Plates | `42159-gamedata.base` |  |
| ITEM | `42240-rebirth.mod` |  |
| ITEM | `47238-rebirth.mod` |  |
| King Shager's Reign | `48293-Dialogue.mod` |  |
| Kral's Last Stand | `48294-Dialogue.mod` |  |
| Lam's Survival Vol.1 | `50323-rebirth.mod` |  |
| Lam's Survival Vol.3 | `48787-rebirth.mod` |  |
| Lantern | `5348-gamedata.base` |  |
| Lantern of Radiance | `47185-Dialogue.mod` |  |
| Leather | `2292-gamedata.base` |  |
| Legend of Luquin I | `53673-Dialogue.mod` |  |
| Legend of Luquin II | `53674-Dialogue.mod` |  |
| Legend of the Forsaken Howler | `51296-Dialogue.mod` |  |
| Legend of the Foulhound | `51298-Dialogue.mod` |  |
| Legend of the Grievewraith | `51299-Dialogue.mod` |  |
| Legend of the Headless Ancient I | `51294-Dialogue.mod` |  |
| Legend of the Headless Ancient II | `51295-Dialogue.mod` |  |
| Legend of the Mist Ghoul | `51297-Dialogue.mod` |  |
| Letter From A Freedom Fighter | `49492-Dialogue.mod` |  |
| Letter From A Swamper I | `49479-Dialogue.mod` |  |
| Letter From A Swamper II | `49480-Dialogue.mod` |  |
| Letter From A Swamper III | `49481-Dialogue.mod` |  |
| Letter From A Swamper IV | `49482-Dialogue.mod` |  |
| Letter From A Swamper V | `49486-Dialogue.mod` |  |
| Letter From Bad Teeth | `50384-Dialogue.mod` |  |
| Letter Of Farewell | `49493-Dialogue.mod` |  |
| Letter Of Parting | `53676-Dialogue.mod` |  |
| Letter Of The Condemned | `53677-Dialogue.mod` |  |
| Letter To A Priest | `49055-Dialogue.mod` |  |
| Letter To Darkbrow | `49474-Dialogue.mod` |  |
| Letter To Hash | `49476-Dialogue.mod` |  |
| Letter To Hash II | `49477-Dialogue.mod` |  |
| Letter To Moll | `49057-Dialogue.mod` |  |
| Letter To Priest Ton | `49054-Dialogue.mod` |  |
| Leviathan Pearl | `42338-changes_otto.mod` |  |
| Ludin's Report I | `49781-Dialogue.mod` |  |
| Ludin's Report II | `49782-Dialogue.mod` |  |
| Ludin's Report III | `49783-Dialogue.mod` |  |
| Luxury Goods | `585-gamedata.base` |  |
| Maril's Diary I | `49198-Dialogue.mod` |  |
| Maril's Diary II | `49199-Dialogue.mod` |  |
| Maril's Diary III | `49200-Dialogue.mod` |  |
| Mass Extinction I | `51307-Dialogue.mod` |  |
| Mass Extinction II | `51304-Dialogue.mod` |  |
| Mass Extinction III | `51305-Dialogue.mod` |  |
| Mass Extinction IV | `51306-Dialogue.mod` |  |
| Meatwrap | `43960-rebirth.mod` |  |
| Medical Supplies | `582-gamedata.base` |  |
| Mega City: The Beginning I | `50960-Dialogue.mod` |  |
| Mega City: The Beginning II | `50961-Dialogue.mod` |  |
| Megacrab Ganglion | `56122-rebirth.mod` |  |
| Membership Pack | `59239-Dialogue.mod` |  |
| Memo From Big Grim | `49478-Dialogue.mod` |  |
| Memo From Moll | `49495-Dialogue.mod` |  |
| Men of Dirt | `65002-Dialogue.mod` |  |
| Mission Graeme: Status | `49197-Dialogue.mod` |  |
| Moll's Speech | `50381-Dialogue.mod` |  |
| Money is Freedom | `53654-Dialogue.mod` |  |
| Motor | `43397-changes_otto.mod` |  |
| Mystery Sack | `59012-Dialogue.mod` |  |
| Note From Blue Eyes | `49488-Dialogue.mod` |  |
| Note From Moll | `1533014-Dialogue.mod` |  |
| Okranite Theories In Relation To Findings I | `51310-Dialogue.mod` |  |
| Okranite Theories In Relation To Findings II | `51311-Dialogue.mod` |  |
| Old CPU Unit | `1533855-rebirth.mod` |  |
| Old Map | `45549-changes_otto.mod` |  |
| Parchment | `16856-nodes_otto1.mod` |  |
| Pearl Cup | `42330-changes_otto.mod` |  |
| Pearl Sword Holder | `43391-changes_otto.mod` |  |
| Pearl Urn | `42331-changes_otto.mod` |  |
| Pearl Vase | `42332-changes_otto.mod` |  |
| Pickaxe | `46981-Newwworld.mod` |  |
| Power Core | `43398-changes_otto.mod` |  |
| Press | `43399-changes_otto.mod` |  |
| Priest Tyluig's Note | `49787-Dialogue.mod` |  |
| Priest Varloo's Note | `49786-Dialogue.mod` |  |
| Puppet | `53675-Dialogue.mod` |  |
| Ration Pack | `42337-changes_otto.mod` |  |
| Raw Iron | `1866-gamedata.base` |  |
| Raw Meat | `42243-rebirth.mod` |  |
| Raw Stone | `1515-gamedata.base` |  |
| Rice Bowl | `1016-gamedata.base` |  |
| Riceweed | `42307-changes_otto.mod` |  |
| RoboPiece13 | `48736-Newwworld.mod` |  |
| RoboPiece14 | `48737-Newwworld.mod` |  |
| RoboPiece16 | `48738-Newwworld.mod` |  |
| RoboPiece4 | `48733-Newwworld.mod` |  |
| RoboPiece8 | `48735-Newwworld.mod` |  |
| Robotics Components | `583-gamedata.base` |  |
| Rotten Food | `48700-rebirth.mod` |  |
| Sake | `42310-changes_otto.mod` |  |
| Scripture of Radiance Vol. I | `49045-Dialogue.mod` |  |
| Scripture of Radiance Vol. II | `49046-Dialogue.mod` |  |
| Scripture of Radiance Vol. III | `49047-Dialogue.mod` |  |
| Scripture of Radiance Vol. IV | `49048-Dialogue.mod` |  |
| Scripture of Radiance Vol. V | `49049-Dialogue.mod` |  |
| Severed Cannibal Left Arm | `108-limbs.mod` |  |
| Severed Cannibal Left Leg | `110-limbs.mod` |  |
| Severed Cannibal Right Arm | `109-limbs.mod` |  |
| Severed Cannibal Right Leg | `111-limbs.mod` |  |
| Severed Cannibal Skav Left Arm | `112-limbs.mod` |  |
| Severed Cannibal Skav Left Leg | `114-limbs.mod` |  |
| Severed Cannibal Skav Right Arm | `113-limbs.mod` |  |
| Severed Cannibal Skav Right Leg | `115-limbs.mod` |  |
| Severed Hive Left Arm | `96901-limbs.mod` |  |
| Severed Hive Left Arm | `96906-limbs.mod` |  |
| Severed Hive Left Arm | `96911-limbs.mod` |  |
| Severed Hive Left Leg | `96902-limbs.mod` |  |
| Severed Hive Left Leg | `96907-limbs.mod` |  |
| Severed Hive Left Leg | `96912-limbs.mod` |  |
| Severed Hive Right Arm | `96903-limbs.mod` |  |
| Severed Hive Right Arm | `96908-limbs.mod` |  |
| Severed Hive Right Arm | `96913-limbs.mod` |  |
| Severed Hive Right Leg | `96904-limbs.mod` |  |
| Severed Hive Right Leg | `96909-limbs.mod` |  |
| Severed Hive Right Leg | `96914-limbs.mod` |  |
| Severed Human Left Arm | `100-limbs.mod` |  |
| Severed Human Left Leg | `102-limbs.mod` |  |
| Severed Human Right Arm | `101-limbs.mod` |  |
| Severed Human Right Leg | `103-limbs.mod` |  |
| Severed Shek Left Arm | `104-limbs.mod` |  |
| Severed Shek Left Leg | `106-limbs.mod` |  |
| Severed Shek Right Arm | `105-limbs.mod` |  |
| Severed Shek Right Leg | `107-limbs.mod` |  |
| Shager's Overthrow | `48291-Dialogue.mod` |  |
| Simple Rug | `1534063-Newwworld.mod` |  |
| Skeleton Eye | `45557-changes_otto.mod` |  |
| Skeleton Muscle | `43395-changes_otto.mod` |  |
| Skeleton Repair Kit | `18020-gamedata.base` |  |
| Sleeping Bag | `56645-rebirth.mod` |  |
| Small Emperor Statue  | `42320-changes_otto.mod` |  |
| Splint Kit | `1435-gamedata.base` |  |
| Spring Steel | `97090-rebirth.mod` |  |
| Standard first aid kit | `515-gamedata.base` |  |
| Steel bars | `579-gamedata.base` |  |
| Strawflour | `1932-gamedata.base` |  |
| String of Cats | `54549-Newwworld.mod` |  |
| Suicide Note | `49494-Dialogue.mod` |  |
| Tech  | `52283-rebirth.mod` |  |
| Tech Hunter's Notes: Leviathan Coast | `52284-rebirth.mod` |  |
| Tech Hunter's Notes: Pool of Obedience | `57396-rebirth.mod` |  |
| Tech Hunter's Notes: Shopper's Guide | `95868-rebirth.mod` |  |
| Tech Hunter's Notes: The Ashlands | `57728-rebirth.mod` |  |
| Tech Hunter's Notes: The Ashlands pt II | `1531906-rebirth.mod` |  |
| Tech Hunter's Notes: The Black Desert | `49361-rebirth.mod` |  |
| Tech Hunter's Notes: The Holy Nation | `49360-rebirth.mod` |  |
| Tech Hunter's Notes: The Shrieking Forest | `57395-rebirth.mod` |  |
| Tech Hunter's Notes: The Swamps | `52282-rebirth.mod` |  |
| The Blessing Of Slavery | `53656-Dialogue.mod` |  |
| The Cannibal Plains: De-evolution Of Man? | `51300-Dialogue.mod` |  |
| The Guiding Light I | `48295-Dialogue.mod` |  |
| The Guiding Light II | `48296-Dialogue.mod` |  |
| The Guiding Light III | `48297-Dialogue.mod` |  |
| The Guiding Light IV | `49043-Dialogue.mod` |  |
| The Guiding Light V | `49056-Dialogue.mod` |  |
| The Guiding Light VI | `49780-Dialogue.mod` |  |
| The Holy Crusade | `49051-Dialogue.mod` |  |
| The Holy Flame | `42322-changes_otto.mod` |  |
| The Little Boy That Got Dismembered By Skeletons | `48298-Dialogue.mod` |  |
| The Lost Ancients I | `51315-Dialogue.mod` |  |
| The Lost Ancients II | `51314-Dialogue.mod` |  |
| The Mystery Of Skeletech I | `51309-Dialogue.mod` |  |
| The Mystery Of Skeletech II | `51308-Dialogue.mod` |  |
| The Pacifier: Guidance For A Troublemaker | `64281-Dialogue.mod` |  |
| The Red Rebellion | `53653-Dialogue.mod` |  |
| The United Cities, A United Power | `53655-Dialogue.mod` |  |
| Thinfish | `50517-Newwworld.mod` |  |
| Tools | `43404-changes_otto.mod` |  |
| United Weekly I | `53657-Dialogue.mod` |  |
| United Weekly II | `53658-Dialogue.mod` |  |
| United Weekly III | `53659-Dialogue.mod` |  |
| United Weekly IV | `53660-Dialogue.mod` |  |
| United Weekly V | `53661-Dialogue.mod` |  |
| United Weekly VI | `97852-Dialogue.mod` |  |
| United Weekly VII | `97853-Dialogue.mod` |  |
| Unknown Old Book | `1533966-Dialogue.mod` |  |
| WANTED: 'Four-Teeth' Milton | `49487-Dialogue.mod` |  |
| WANTED: Big Bo | `56682-Dialogue.mod` |  |
| WANTED: Blue Eyes | `56791-Dialogue.mod` |  |
| WANTED: Bugmaster | `51391-rebirth.mod` |  |
| WANTED: Butcher Of Black Scratch | `56789-Dialogue.mod` |  |
| WANTED: Cannibal Leaders | `64914-rebirth.mod` |  |
| WANTED: Dust King | `44921-rebirth.mod` |  |
| WANTED: Ells | `56685-Dialogue.mod` |  |
| WANTED: Flying Bull | `64701-Dialogue.mod` |  |
| WANTED: Ghost | `64699-Dialogue.mod` |  |
| WANTED: Giant King Gurgler | `98228-Dialogue.mod` |  |
| WANTED: Gorrillo | `98632-Dialogue.mod` |  |
| WANTED: Gutterhead | `98227-Dialogue.mod` |  |
| WANTED: Jaegar The Mad | `56735-Dialogue.mod` |  |
| WANTED: Moll | `51393-rebirth.mod` |  |
| WANTED: Old Grey | `56733-Dialogue.mod` |  |
| WANTED: Razor | `56792-Dialogue.mod` |  |
| WANTED: Sand Ninja Oni | `96354-__Southern hive.mod` |  |
| WANTED: Savant the Skin Eater | `65254-Dialogue.mod` |  |
| WANTED: Shade | `63883-Dialogue.mod` |  |
| WANTED: Simion | `65005-Dialogue.mod` |  |
| WANTED: The Black Dragon Duo | `56787-Dialogue.mod` |  |
| WANTED: The Canyonlands Killer | `56790-Dialogue.mod` |  |
| WANTED: The Preacher | `56788-Dialogue.mod` |  |
| WANTED: The Red Bandit | `56786-Dialogue.mod` |  |
| WANTED: Tinfist | `56736-Dialogue.mod` |  |
| WANTED: Tora The Gutless | `98165-Dialogue.mod` |  |
| WANTED: Valamon | `95810-Dialogue.mod` |  |
| WANTED: Voodoo Brothers | `98646-Dialogue.mod` |  |
| WANTED: White Eyebrow | `56732-Dialogue.mod` |  |
| Water | `1914-gamedata.base` |  |
| Water Jug | `12617-nodes_otto.mod` |  |
| Way Of The Warrior: The Book Of Kral | `48292-Dialogue.mod` |  |
| Wheatstraw | `1913-gamedata.base` |  |
| Wooden Bowl | `42317-changes_otto.mod` |  |
| Wrench | `43862-changes_otto.mod` |  |
| _LETTER TEMPLATE | `49380-rebirth.mod` |  |

## Source file fingerprints

Generated from the local vanilla database with Meitou's FCS reader and checked against its item factory. These SHA256 hashes identify the exact source files. Regenerate after a vanilla data update; mods can change item names and behavior without changing their IDs.

- `gamedata.base`: `ea6130f4b0d973862d4dd84e26f1b2fc6202212c64a61f267fc6f151f07319d4`
- `Newwworld.mod`: `34bf2754933bac39870dcb09b826e1ae0ab896b23092c2ffb99918cfe4ffd211`
- `Dialogue.mod`: `323e129649994c05e02cb93fa2e258baf31eada53487db790767d1ac6ea20fa1`
- `rebirth.mod`: `23b4a8cc214badbb04c3e28c1a082f20e759c96510cc2a7ffd6ff2663e849689`
