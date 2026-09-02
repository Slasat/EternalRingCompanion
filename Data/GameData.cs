using System.Collections.Generic;
using EternalRingCompanion.Core;

namespace EternalRingCompanion.Data;

/// <summary>
/// The Eternal Ring (PAL / SLES-500.51) memory map used by this app: player stats, the
/// inventory quantity arrays, the per-item combat-stat table, and the level-warp fields.
/// Every offset is relative to PCSX2's "EEmem" export. Derived from the PS2Trainer project's
/// EternalRingFieldMap (which documents how each was found and live write-tested); only the
/// stats / items / teleport parts are carried over here.
/// </summary>
public static class GameData
{
    // ===================================================================
    //  Player stats
    // ===================================================================

    public sealed record StatField(string Name, long EememOffset, FieldType Type, string? Hint = null);

    // The stats block is anchored at EEmem + 0x1FFB94 (the live HP field).
    private const long HpBase = 0x1FFB94;

    public static readonly StatField[] PlayerStats =
    {
        new("HP",     HpBase + 0x00, FieldType.Int16),
        new("Max HP", HpBase + 0x02, FieldType.Int16),
        new("MP",     HpBase + 0x04, FieldType.Int16),
        new("Max MP", HpBase + 0x06, FieldType.Int16),
    };

    // Character name: fixed 16-byte null-terminated ASCII buffer (15 usable chars).
    public const long NameOffset = 0x355290;
    public const int NameBufferSize = 16;

    // Time-of-day clock (seconds; zero point is noon on Day 1).
    public const long TimeOfDayOffset = 0x1F68F0;
    public const int SecondsPerDay = 86400;
    public const int DayEpochOffset = 1;
    public const int NoonShiftSeconds = SecondsPerDay / 2;

    // ===================================================================
    //  Inventory — flat byte-per-slot quantity arrays
    // ===================================================================

    public sealed record InvItem(string Name, long EememOffset);
    public sealed record InvCategory(string Name, string Glyph, InvItem[] Items);

    public static readonly InvCategory[] Inventory =
    {
        new("Weapons", "", new InvItem[]
        {
            new("Small Sword", 0x21EA31), new("Knife", 0x21EA32), new("Cinquedea", 0x21EA33),
            new("Stiletto", 0x21EA34), new("Rapier", 0x21EA35), new("Schiavona", 0x21EA36),
            new("Hunting Sword", 0x21EA37), new("Eternal Sword", 0x21EA38), new("Kaizer Knuckle", 0x21EA39),
        }),
        new("Key Items", "", new InvItem[]
        {
            new("King Letter", 0x21EA41), new("Weapon Form", 0x21EA42), new("Bracelet", 0x21EA43),
            new("Water Key", 0x21EA44), new("Fire Key", 0x21EA46), new("Wind Key", 0x21EA47),
            new("Earth Key", 0x21EA48),
        }),
        new("Consumables", "", new InvItem[]
        {
            new("Golden Grass", 0x21EA73), new("Golden Flower", 0x21EA74), new("Golden Fruit", 0x21EA75),
            new("Magic Stone", 0x21EA76), new("Magic Crystal", 0x21EA77), new("Magic Jewel", 0x21EA78),
            new("Dragon Scale", 0x21EA79), new("Dragon Egg", 0x21EA7A), new("Rat's Tail", 0x21EA7B),
            new("Golden Seed", 0x21EA7C), new("Sahagin's Thorn", 0x21EA7D), new("Cockatrice Feather", 0x21EA7E),
            new("Dragon Potion", 0x21EA7F), new("Fire Dragon Fang", 0x21EA80), new("Earth Dragon Fang", 0x21EA81),
            new("Air Dragon Fang", 0x21EA82), new("Water Dragon Fang", 0x21EA83), new("Gold Dragon Fang", 0x21EA84),
            new("Silver Dragon Fang", 0x21EA85), new("Elder Dragon Fang", 0x21EA86),
        }),
        new("Rings · Fire", "", new InvItem[]
        {
            new("Fireball", 0x21EAC1), new("Fire Wall", 0x21EAC2), new("Floating Bomb", 0x21EAC3),
            new("Bomber", 0x21EAC4), new("Silence", 0x21EAC5), new("Weaken", 0x21EAC6),
            new("Alter Fire", 0x21EAC7), new("Deplete", 0x21EAC8), new("Fiery Avenger", 0x21EAC9),
            new("Fire Dragon (Ring)", 0x21EACA), new("Ring of Heat", 0x21EACB), new("Ring of Flame", 0x21EACC),
            new("Ring of Inferno", 0x21EACD), new("Wind Heat", 0x21EACE), new("Earth Heat", 0x21EACF),
            new("Sword Heat", 0x21EAD0), new("Water Heat", 0x21EAD1), new("Animated Sword", 0x21EAD2),
            new("Power of Branch", 0x21EAD3), new("Burn Edge", 0x21EAD4),
        }),
        new("Rings · Water", "", new InvItem[]
        {
            new("Ice Needle", 0x21EAD5), new("Ice Wall", 0x21EAD6), new("Ice Trap", 0x21EAD7),
            new("Blizzard", 0x21EAD8), new("Healing Water", 0x21EAD9), new("Curing Water", 0x21EADA),
            new("Unpoison", 0x21EADB), new("Purify", 0x21EADC), new("Projectile", 0x21EADD),
            new("Water Dragon (Ring)", 0x21EADE), new("Ring of Rain", 0x21EADF), new("Ring of Sea", 0x21EAE0),
            new("Ring of Abyss", 0x21EAE1), new("Wind Rain", 0x21EAE2), new("Earth Rain", 0x21EAE3),
            new("Sword Rain", 0x21EAE4), new("Fire Rain", 0x21EAE5), new("Fortune", 0x21EAE6),
            new("Enlightenment", 0x21EAE7), new("Sacrifice", 0x21EAE8),
        }),
        new("Rings · Wind", "", new InvItem[]
        {
            new("Wind Cutter", 0x21EAE9), new("Sonic", 0x21EAEA), new("Tornado (Ring)", 0x21EAEB),
            new("Vortex", 0x21EAEC), new("Turbulence", 0x21EAED), new("Confusion", 0x21EAEE),
            new("Invisible", 0x21EAEF), new("Bind", 0x21EAF0), new("Spear", 0x21EAF1),
            new("Wind Dragon (Ring)", 0x21EAF2), new("Ring of Breeze", 0x21EAF3), new("Ring of Gust", 0x21EAF4),
            new("Ring of Storm", 0x21EAF5), new("Fire Breeze", 0x21EAF6), new("Water Breeze", 0x21EAF7),
            new("Sword Breeze", 0x21EAF8), new("Earth Breeze", 0x21EAF9), new("Power of Seek", 0x21EAFA),
            new("Clarity", 0x21EAFB), new("Power of Time", 0x21EAFC),
        }),
        new("Rings · Earth", "", new InvItem[]
        {
            new("Material Arrow", 0x21EAFD), new("Upheaval", 0x21EAFE), new("Poison", 0x21EAFF),
            new("Mist", 0x21EB00), new("Earth Heal", 0x21EB01), new("Protect", 0x21EB02),
            new("Imprisonment", 0x21EB03), new("Orbiter", 0x21EB04), new("Earthy Axe", 0x21EB05),
            new("Earth Dragon (Ring)", 0x21EB06), new("Ring of Stone", 0x21EB07), new("Ring of Rock", 0x21EB08),
            new("Ring of Nature", 0x21EB09), new("Fire Stone", 0x21EB0A), new("Water Stone", 0x21EB0B),
            new("Sword Stone", 0x21EB0C), new("Wind Stone", 0x21EB0D), new("Ring of Insight", 0x21EB0E),
            new("Power of Growth", 0x21EB0F), new("True Sight", 0x21EB10),
        }),
        new("Rings · Light", "", new InvItem[]
        {
            new("Divine (Ring)", 0x21EB11), new("Thunder", 0x21EB12), new("Holy Flash", 0x21EB13),
            new("Absorb", 0x21EB14), new("Bless", 0x21EB15), new("Light Dragon (Ring)", 0x21EB16),
            new("Ring of Light", 0x21EB17), new("Ring of Shine", 0x21EB18), new("Ring of Glare", 0x21EB19),
            new("Create Life", 0x21EB1A),
        }),
        new("Rings · Dark", "", new InvItem[]
        {
            new("Banishment", 0x21EB1B), new("Dark Thunder", 0x21EB1C), new("Dark Flash", 0x21EB1D),
            new("Guardian", 0x21EB1E), new("Dark Stop", 0x21EB1F), new("Dark Dragon (Ring)", 0x21EB20),
            new("Ring of Shadow", 0x21EB21), new("Ring of Night", 0x21EB22), new("Ring of Chaos", 0x21EB23),
            new("Dark Pact", 0x21EB24),
        }),
        new("Gems", "", new InvItem[]
        {
            new("Glowing", 0x21EB41), new("Leaf", 0x21EB42), new("Feather", 0x21EB43),
            new("Watery", 0x21EB44), new("Flame", 0x21EB45), new("Tree", 0x21EB46),
            new("Winged", 0x21EB47), new("Waterfall", 0x21EB48), new("Lava", 0x21EB49),
            new("Forest", 0x21EB4A), new("Cloudy", 0x21EB4B), new("Sea", 0x21EB4C),
            new("Sunbeam", 0x21EB4D), new("Dead", 0x21EB4E), new("Phoenix", 0x21EB4F),
            new("Mountain", 0x21EB50), new("Tornado", 0x21EB51), new("Iceberg", 0x21EB52),
            new("Divine", 0x21EB53), new("Ritual", 0x21EB54), new("Fire Dragon", 0x21EB55),
            new("Earth Dragon", 0x21EB56), new("Wind Dragon", 0x21EB57), new("Water Dragon", 0x21EB58),
            new("Light Dragon", 0x21EB59), new("Dark Dragon", 0x21EB5A),
        }),
    };

    // ===================================================================
    //  Per-item combat-stat table (game-wide, not per-save)
    //  7 x Int16 [Fire, Earth, Wind, Water, Light, Dark, STR] per 0x100 slot.
    //  Displayed in-game stat = character base + floor(table value / 2).
    // ===================================================================

    public const long ItemStatStride = 0x100;
    public static readonly string[] ItemStatColumns = { "Fire", "Earth", "Wind", "Water", "Light", "Dark", "STR" };

    public sealed record StatItemCategory(string Name, string[] ItemNames, long BaseOffset);

    public static readonly StatItemCategory[] ItemStatTables =
    {
        new("Weapons",       Names(Inventory[0]), 0x1A8A21C),
        new("Rings · Fire",  Names(Inventory[3]), 0x1A82220),
        new("Rings · Water", Names(Inventory[4]), 0x1A83620),
        new("Rings · Wind",  Names(Inventory[5]), 0x1A84A20),
        new("Rings · Earth", Names(Inventory[6]), 0x1A85E20),
        new("Rings · Light", Names(Inventory[7]), 0x1A87220),
        new("Rings · Dark",  Names(Inventory[8]), 0x1A87C20),
    };

    private static string[] Names(InvCategory c)
    {
        var a = new string[c.Items.Length];
        for (int i = 0; i < a.Length; i++) a[i] = c.Items[i].Name;
        return a;
    }

    // ===================================================================
    //  Level warp
    // ===================================================================
    // Writing these mirrors exactly what the game's own door-trigger code writes on a real
    // transition, so it works from anywhere with no adjacent door required.
    public const long WarpPositionOffset = 0x202C50;    // X, Y, Z, W  (4 floats)
    public const long WarpOrientationOffset = 0x202C60; // 3 unused floats + heading
    public const long WarpHeadingOffset = 0x202C6C;     // heading float (inside the block above)
    public const long WarpTargetIdOffset = 0x202C70;    // Int32 local level id
    public const long WarpLoadFlagOffset = 0x202C74;    // Int32, write 1
    public const long WarpTriggerOffset = 0x1FFDAB;     // Byte, write 1 last

    public sealed record WarpPart(string? Label, int LevelId, float X, float Y, float Z, float Heading);

    public sealed record WarpArea(
        string Phase, string Name, string Note, string Glyph, IReadOnlyList<WarpPart> Parts);

    /// <summary>
    /// Named destinations in game-progression order. Levels the game leaves unnamed (its own
    /// location table shows "Unknown" for local ids 24, 60-69, 80-82, 88, 95-99) are excluded.
    /// Areas that load as several separate rooms under one name are collapsed into a single
    /// entry with a Part selector. Order follows the retail walkthrough spine; genuinely
    /// optional side areas are grouped last. Spawn coordinates are the real entrance records
    /// extracted from each level's own data (most are live-teleport-confirmed; a few are
    /// best-available and may need a step to safe ground).
    /// </summary>
    public static readonly WarpArea[] WarpAreas =
    {
        // ---- Arrival on the Island -------------------------------------
        new("Arrival on the Island", "Waterfall Cavern", "Starting area — the cave you wash up in", "", new WarpPart[]
        {
            new(null, 72, -578f, -950f, 7000f, 0f),
        }),
        new("Arrival on the Island", "Research Team HQ", "Trade the King's Letter to Evans for the Small Sword", "", new WarpPart[]
        {
            new("Camp",   70, -2500f, -300f, 7430f, -1.57f),
            new("Inner",  73, -2500f, -300f, 7430f, -1.57f),
            new("Annex",  74,  2000f, -150f, 4800f,  1.57f),
        }),
        new("Arrival on the Island", "Water Shrine", "Fireball ring, Water Devil boss, then the Water Key", "", new WarpPart[]
        {
            new("Upper", 1, 0f,    -150f, -5500f, 3.14f),
            new("Lower", 2, 5300f, -150f, -8400f, 1.57f),
        }),
        new("Arrival on the Island", "Forgotten Dais", "The ring-crafting altar (reached through the Blue Door)", "", new WarpPart[]
        {
            new(null, 90, 0f, -50f, -2200f, 3.14f),
        }),

        // ---- Across the Island ---------------------------------------
        new("Across the Island", "Worshipping Area", "The Place of Ritual", "", new WarpPart[]
        {
            new(null, 3, 2200f, -1350f, 10650f, -1.57f),
        }),
        new("Across the Island", "Disposal Valley", "The Abandoned Place", "", new WarpPart[]
        {
            new(null, 4, 4200f, -150f, 1500f, -1.57f),
        }),
        new("Across the Island", "Tree Hideout", "The Tree Village", "", new WarpPart[]
        {
            new("Outer", 5, -2100f, -150f,  2600f, 0f),
            new("Inner", 6,     0f, -1750f, -2400f, 0f),
        }),
        new("Across the Island", "Limestone Cave", "The Great Looping Cave", "", new WarpPart[]
        {
            new("West", 7, -4000f, -150f, -3600f, -3.14f),
            new("East", 8,  8100f, -150f, -3250f,  0f),
        }),
        new("Across the Island", "Cliff Forest", "The Sea Cliff Forest", "", new WarpPart[]
        {
            new(null, 0, 13800f, -150f, -2370f, 0f),
        }),

        // ---- The Elemental Keys ------------------------------------
        new("The Elemental Keys", "Mine Ruins", "The Fire Key is found here", "", new WarpPart[]
        {
            new(null, 10, -2700f, -150f, -2800f, 3.14f),
        }),
        new("The Elemental Keys", "Iron Mill", "The Ironworks — Fire Demons and a cast-to-open door", "", new WarpPart[]
        {
            new(null, 11, 11100f, -150f, 1100f, 0f),
        }),
        new("The Elemental Keys", "River of Lava", "The Molten River — use the Fire Key to press on", "", new WarpPart[]
        {
            new(null, 12, -4500f, -150f, -6700f, -3.14f),
        }),
        new("The Elemental Keys", "Magic Laboratory", "The Magic Testing Center — the Hunting Sword", "", new WarpPart[]
        {
            new(null, 13, -6000f, -150f, -300f, -1.57f),
        }),
        new("The Elemental Keys", "Ruins", "", "", new WarpPart[]
        {
            new(null, 14, -3000f, -150f, -2400f, -1.57f),
        }),
        new("The Elemental Keys", "Library", "Beat the Water Dragon for the Wind Key", "", new WarpPart[]
        {
            new(null, 15, 600f, -750f, 3500f, 0f),
        }),
        new("The Elemental Keys", "Underground Lake", "The Camp on the South Beach", "", new WarpPart[]
        {
            new(null, 16, 300f, -450f, -3300f, 3.14f),
        }),
        new("The Elemental Keys", "Tower of Storms", "Two Griffins guard the Earth Key", "", new WarpPart[]
        {
            new(null, 18, 1200f, -150f, -3700f, 3.14f),
        }),
        new("The Elemental Keys", "Snowstorm Plain", "The Blizzard Field — use the Earth Key on the first pedestal", "", new WarpPart[]
        {
            new(null, 17, -5430f, -750f, 6620f, -1.57f),
        }),

        // ---- The Eternal Dimension -------------------------------
        new("The Eternal Dimension", "Eternal Dimension", "The Hidden Dungeon, four sealed parts", "", new WarpPart[]
        {
            new("Part 1", 19, -5600f, -750f, 8400f, -1.57f),
            new("Part 2", 20,     0f, -350f, 9350f,  0f),
            new("Part 3", 21,  9650f, -150f, -2600f, 1.57f),
            new("Part 4", 22,  -400f, -150f, 7650f,  0f),
        }),
        new("The Eternal Dimension", "Eternal Ring", "The final confrontation", "", new WarpPart[]
        {
            new(null, 23, 0f, -250f, -555f, 3.14f),
        }),

        // ---- Optional Areas ------------------------------------
        new("Optional Areas", "Southern Hideout", "A side pocket off Disposal Valley", "", new WarpPart[]
        {
            new(null, 9, 3250f, -150f, 1000f, 3.14f),
        }),
        new("Optional Areas", "N. Shore of Island", "The island's north shore", "", new WarpPart[]
        {
            new("Shore",   71, -2200f, -150f, 296f,  -1.5f),
            new("Landing", 75,  3700f, -150f, 1600f,  1.57f),
        }),
        new("Optional Areas", "Sealed Labyrinth", "An optional dungeon reached from the shore", "", new WarpPart[]
        {
            new("Part 1", 50, -6000f,  -150f, 16000f, 0f),
            new("Part 2", 51, -9600f,  -150f, 9000f,  0f),
            new("Part 3", 52, -13900f, -150f, 9900f, -0.2f),
            new("Part 4", 53, 0f,      -150f, -5800f, 3.14f),
        }),
    };
}
