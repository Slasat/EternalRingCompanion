# Eternal Ring Companion

A Windows desktop application that reads and writes the memory of a running *Eternal Ring*
(PAL, `SLES-500.51`) session under the PCSX2 emulator. It provides direct editing of player
stats and inventory, and a level-warp function. It does not scan memory, patch the ISO, or
modify PCSX2.

## Requirements

| | |
|---|---|
| OS | Windows 10 / 11, 64-bit. |
| Runtime | .NET 10 Desktop Runtime for a framework-dependent build; none for a self-contained release build. |
| Emulator | PCSX2 (64-bit, `pcsx2-qt`) running *Eternal Ring* PAL (`SLES-500.51`). |
| Privileges | Administrator (see below). |

Offsets are specific to the PAL release. Other regions are not supported.

## Administrator rights

The application must run elevated. It opens the PCSX2 process with `PROCESS_ALL_ACCESS` and
calls `ReadProcessMemory` / `WriteProcessMemory`; Windows denies cross-process memory access
to a non-elevated process. The executable manifest declares
`requestedExecutionLevel level="requireAdministrator"`, so launching it raises a UAC prompt
(or elevates silently, depending on the local UAC policy).

If the process is not elevated, **Link** fails with "Could not open that process" and no
read or write is possible. If PCSX2 is itself running elevated, this application must also be
elevated.

The target platform is fixed to x64. A 32-bit process cannot access a 64-bit process's
memory, and current PCSX2 builds are 64-bit.

## Pages

### Player Stats

Editable fields, each with a **Set** button and a per-field **Lock** toggle:

| Field | Type | Address |
|---|---|---|
| HP | int16 | `EEmem + 0x1FFB94` |
| Max HP | int16 | `EEmem + 0x1FFB96` |
| MP | int16 | `EEmem + 0x1FFB98` |
| Max MP | int16 | `EEmem + 0x1FFB9A` |

**Lock** re-writes the field every 120 ms until cleared. **Restore HP & MP** copies Max HP
into HP and Max MP into MP.

Character name: 16-byte null-terminated ASCII buffer at `EEmem + 0x355290`, 15 usable
characters. **Read** reloads from memory; **Rename** writes the buffer.

### Inventory

One page per category (Weapons, Key Items, Consumables, Rings by element, Gems). Each item is
a single byte in a flat per-slot array in the save structure; the byte value is the held
quantity (0–255), or an ownership flag (0/1) for weapons and key items. Editing a slot that
was never obtained sets it directly.

Per-item **Set**, and **Fill category** / **Clear category** which write every slot in the
current category (`99`, or `1` for weapons and key items / `0`).

### Teleport

A fixed list of named areas in game-progression order. Selecting **Warp** writes the same
fields the game's own door-trigger routine writes, so a transition occurs from any location
without an adjacent door:

| Address | Type | Value |
|---|---|---|
| `EEmem + 0x202C50` | 4 × float | spawn X, Y, Z, W |
| `EEmem + 0x202C60` | 4 × float | orientation; heading at `+0x202C6C` |
| `EEmem + 0x202C70` | int32 | destination local level id |
| `EEmem + 0x202C74` | int32 | `1` |
| `EEmem + 0x1FFDAB` | byte | `1` (written last; triggers the load) |

The load settles in 1–3 seconds. Areas that load as several rooms under one name expose a
part selector.

## Operation

1. On launch the application lists processes named `pcsx2*`. If exactly one is present it
   attaches automatically; otherwise use **Link** and select one.
2. Attaching opens a process handle. `EEmem` (PCSX2's exported pointer to the base of
   emulated PS2 RAM) is resolved by parsing the export directory of the PCSX2 executable on
   disk for the `EEmem` RVA, then reading the 8-byte pointer at `moduleBase + RVA`. The
   value is cached per attach. All game addresses are `EEmem + offset`.
3. Each page polls its fields once per second and updates any field that is not focused and
   not locked.
4. A background timer checks every second that the attached process still exists and detaches
   if it has exited.

If a read returns nothing after attach, PCSX2 is at the BIOS or main menu — load a save,
then **Link** again.

## Teleport list

Order and names were derived from three sources:
- Community notes ("Wind Route", Vergiliaux) for the key order (Water → Fire → Wind → Earth)
  and where each key is obtained and used.

Levels the game's own location-name table leaves as "Unknown" (local ids 24, 60–69, 80–82,
88, 95–99) are excluded; they are cutscene shells and code-driven spaces with no entrance
record.

| # | Area | Parts | Level id(s) |
|---:|---|---|---|
| 1 | Waterfall Cavern | | 72 |
| 2 | Research Team HQ | Camp / Inner / Annex | 70 / 73 / 74 |
| 3 | Water Shrine | Upper / Lower | 1 / 2 |
| 4 | Forgotten Dais | | 90 |
| 5 | Worshipping Area | | 3 |
| 6 | Disposal Valley | | 4 |
| 7 | Tree Hideout | Outer / Inner | 5 / 6 |
| 8 | Limestone Cave | West / East | 7 / 8 |
| 9 | Cliff Forest | | 0 |
| 10 | Mine Ruins | | 10 |
| 11 | Iron Mill | | 11 |
| 12 | River of Lava | | 12 |
| 13 | Magic Laboratory | | 13 |
| 14 | Ruins | | 14 |
| 15 | Library | | 15 |
| 16 | Underground Lake | | 16 |
| 17 | Tower of Storms | | 18 |
| 18 | Snowstorm Plain | | 17 |
| 19 | Eternal Dimension | Part 1–4 | 19 / 20 / 21 / 22 |
| 20 | Eternal Ring | | 23 |
| 21 | Southern Hideout | | 9 |
| 22 | N. Shore of Island | Shore / Landing | 71 / 75 |
| 23 | Sealed Labyrinth | Part 1–4 | 50 / 51 / 52 / 53 |

## Credits

Created by **Slasat**, with support from members of the Discord server
*Eternal Ring's Island of Going Fast*.
