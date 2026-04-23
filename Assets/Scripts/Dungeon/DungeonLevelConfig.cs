using UnityEngine;

namespace LochyIGorzala.Dungeon
{
    /// <summary>
    /// Defines the visual and generation parameters for a single dungeon floor.
    /// Each floor has a unique tile theme drawn from the 32rogues tiles sprite sheet.
    ///
    /// Tile sheet coordinate reference (col, row) — 0-indexed, row 0 = top of sheet:
    ///   Group 1 (dirt wall)         → row 0
    ///   Group 2 (rough stone)       → row 1
    ///   Group 3 (stone brick)       → row 2
    ///   Group 4 (igneous)           → row 3
    ///   Group 5 (large stone)       → row 4
    ///   Group 6 (catacomb/skull)    → row 5
    ///   Group 7 (grey stone floor)  → row 6
    ///   Group 9 (dirt floor)        → row 8
    ///   Group 10 (stone floor)      → row 9
    ///   Group 11 (bone floor)       → row 10
    ///   Group 12 (red stone floor)  → row 11
    ///   Group 16 (dark brown/bone)  → row 15
    ///   Group 17 (doors/stairs)     → row 16 (stairsDown=col7, stairsUp=col8)
    ///   Group 18 (chests/props)     → row 17
    ///   Group 21 (mushrooms)        → row 20
    ///   Group 22 (corpse)           → row 21
    ///   Group 23 (blood/slime)      → row 22
    ///   Group 24 (coffin)           → row 23
    /// </summary>
    [System.Serializable]
    public class LevelTheme
    {
        public string Name;
        public string Description;

        // Wall tiles
        public Vector2Int WallTopTile;
        public Vector2Int WallSideTile;
        public Vector2Int WallSideTile2;   // variant for variety

        // Floor tiles
        public Vector2Int FloorBgTile;     // solid background colour
        public Vector2Int FloorVariant1;
        public Vector2Int FloorVariant2;
        public Vector2Int FloorVariant3;

        // Decorations placed on the deco tilemap (not blocking movement)
        public Vector2Int[] DecorationTiles;

        // Camera background tint
        public Color AmbientColor;

        // BSP generation tuning
        public int MinRoomSize;
        public int MaxSplitDepth;
        public float CorridorWidth;        // 1 = narrow, 2 = wide

        public LevelTheme(
            string name, string desc,
            Vector2Int wallTop, Vector2Int wallSide, Vector2Int wallSide2,
            Vector2Int floorBg, Vector2Int floor1, Vector2Int floor2, Vector2Int floor3,
            Vector2Int[] decos,
            Color ambient,
            int minRoom = 8, int splitDepth = 4, float corridorW = 1f)
        {
            Name = name;
            Description = desc;
            WallTopTile  = wallTop;
            WallSideTile  = wallSide;
            WallSideTile2 = wallSide2;
            FloorBgTile   = floorBg;
            FloorVariant1 = floor1;
            FloorVariant2 = floor2;
            FloorVariant3 = floor3;
            DecorationTiles = decos;
            AmbientColor  = ambient;
            MinRoomSize   = minRoom;
            MaxSplitDepth = splitDepth;
            CorridorWidth = corridorW;
        }
    }

    /// <summary>
    /// Static registry of all 6 level themes (0 = Lobby, 1–5 = Dungeon).
    /// </summary>
    public static class DungeonLevelThemes
    {
        private static readonly LevelTheme[] Themes = new LevelTheme[]
        {
            // ─────────────────────────────────────────────────────────────
            // Floor 0: Przedsionek (Lobby) — hand-crafted safe room, no monsters
            // Clean stone brick walls + bright stone floor.
            // Ambient: warm neutral white — this feels safe and welcoming.
            // ─────────────────────────────────────────────────────────────
            new LevelTheme(
                "Przedsionek", "Wejście do lochów. Tu możesz odpocząć przed zejściem.",
                wallTop:   new Vector2Int(0, 2),   // row 2 col 0 — stone brick (top face)
                wallSide:  new Vector2Int(1, 2),   // row 2 col 1 — stone brick side A
                wallSide2: new Vector2Int(2, 2),   // row 2 col 2 — stone brick side B
                floorBg:   new Vector2Int(0, 9),   // row 9 col 0 — clean stone floor
                floor1:    new Vector2Int(1, 9),
                floor2:    new Vector2Int(2, 9),
                floor3:    new Vector2Int(3, 9),
                decos: new Vector2Int[]
                {
                    new Vector2Int(0, 17),   // chest (closed)
                    new Vector2Int(2, 17),   // jar
                    new Vector2Int(4, 17),   // barrel
                    new Vector2Int(6, 17),   // log pile
                },
                ambient: new Color(1.00f, 0.98f, 0.90f),  // warm white
                minRoom: 12, splitDepth: 2, corridorW: 2f
            ),

            // ─────────────────────────────────────────────────────────────
            // Floor 1: Ziemne Korytarze — rough earth passages carved by hand
            // Dirt/earth walls (row 0) + grey stone floor (row 6).
            // Ambient: cool desaturated blue-grey — damp, cold stone.
            // ─────────────────────────────────────────────────────────────
            new LevelTheme(
                "Ziemne Korytarze", "Piętro 1 — surowe, ziemne korytarze. Pachnie wilgocią.",
                wallTop:   new Vector2Int(0, 0),   // row 0 col 0 — dirt wall top face
                wallSide:  new Vector2Int(1, 0),   // row 0 col 1 — dirt wall side A
                wallSide2: new Vector2Int(2, 0),   // row 0 col 2 — dirt wall side B
                floorBg:   new Vector2Int(0, 6),   // row 6 col 0 — dark grey stone floor
                floor1:    new Vector2Int(1, 6),
                floor2:    new Vector2Int(2, 6),
                floor3:    new Vector2Int(3, 6),
                decos: new Vector2Int[]
                {
                    new Vector2Int(0, 18),   // large rock 1
                    new Vector2Int(1, 18),   // large rock 2
                    new Vector2Int(4, 17),   // barrel
                    new Vector2Int(5, 17),   // ore sack
                    new Vector2Int(0, 21),   // bones 1
                },
                ambient: new Color(0.72f, 0.80f, 0.90f),  // cold blue-grey
                minRoom: 5, splitDepth: 3, corridorW: 1f
            ),

            // ─────────────────────────────────────────────────────────────
            // Floor 2: Jaskinie — natural caverns, water-carved
            // Rough stone walls (row 1) + dark earth floor (row 8).
            // Ambient: sickly green — dripping water, mould, mushrooms.
            // ─────────────────────────────────────────────────────────────
            new LevelTheme(
                "Jaskinie", "Piętro 2 — naturalne jaskinie. Czuć wilgoć i zapach stęchlizny.",
                wallTop:   new Vector2Int(0, 1),   // row 1 col 0 — rough stone top
                wallSide:  new Vector2Int(1, 1),   // row 1 col 1 — rough stone side
                wallSide2: new Vector2Int(3, 1),   // row 1 col 3 — rough stone alt
                floorBg:   new Vector2Int(0, 8),   // row 8 col 0 — dark earth floor
                floor1:    new Vector2Int(1, 8),
                floor2:    new Vector2Int(2, 8),
                floor3:    new Vector2Int(3, 8),
                decos: new Vector2Int[]
                {
                    new Vector2Int(0, 20),   // small mushrooms
                    new Vector2Int(1, 20),   // large mushroom
                    new Vector2Int(2, 20),   // mushroom cluster
                    new Vector2Int(0, 21),   // bones
                    new Vector2Int(1, 21),   // bones 2
                },
                ambient: new Color(0.55f, 0.78f, 0.55f),  // mossy green
                minRoom: 4, splitDepth: 3, corridorW: 1f
            ),

            // ─────────────────────────────────────────────────────────────
            // Floor 3: Krypty — catacomb corridors lined with skulls
            // Skull/catacomb walls (row 5) + bone-strewn floor (row 10).
            // Ambient: deep violet — ancient death magic permeates the air.
            // ─────────────────────────────────────────────────────────────
            new LevelTheme(
                "Krypty", "Piętro 3 — katakumby pełne kości. Cisza jest tu grobowa.",
                wallTop:   new Vector2Int(0, 5),   // row 5 col 0 — catacomb top face
                wallSide:  new Vector2Int(1, 5),   // row 5 col 1 — skull wall side A
                wallSide2: new Vector2Int(2, 5),   // row 5 col 2 — skull wall side B
                floorBg:   new Vector2Int(0, 10),  // row 10 col 0 — bone floor (dark)
                floor1:    new Vector2Int(1, 10),
                floor2:    new Vector2Int(2, 10),
                floor3:    new Vector2Int(3, 10),
                decos: new Vector2Int[]
                {
                    new Vector2Int(0, 23),   // coffin closed
                    new Vector2Int(1, 23),   // coffin ajar
                    new Vector2Int(3, 23),   // sarcophagus
                    new Vector2Int(0, 22),   // blood spatter
                    new Vector2Int(2, 22),   // slime patch
                },
                ambient: new Color(0.60f, 0.45f, 0.85f),  // deep violet
                minRoom: 4, splitDepth: 3, corridorW: 1f
            ),

            // ─────────────────────────────────────────────────────────────
            // Floor 4: Ogniste Głębiny — volcanic rock fissures
            // Igneous/lava walls (row 3) + red cracked stone floor (row 11).
            // Ambient: fiery orange-red — heat distortion, sulphur fumes.
            // ─────────────────────────────────────────────────────────────
            new LevelTheme(
                "Ogniste Głębiny", "Piętro 4 — wulkaniczne głębiny. Podłoga parzy.",
                wallTop:   new Vector2Int(0, 3),   // row 3 col 0 — igneous top face
                wallSide:  new Vector2Int(1, 3),   // row 3 col 1 — igneous side A
                wallSide2: new Vector2Int(2, 3),   // row 3 col 2 — igneous side B (cracked)
                floorBg:   new Vector2Int(0, 11),  // row 11 col 0 — cracked red floor
                floor1:    new Vector2Int(1, 11),
                floor2:    new Vector2Int(2, 11),
                floor3:    new Vector2Int(3, 11),
                decos: new Vector2Int[]
                {
                    new Vector2Int(0, 22),   // blood spatter 1
                    new Vector2Int(1, 22),   // blood spatter 2
                    new Vector2Int(2, 22),   // slime (lava bubble)
                    new Vector2Int(3, 22),   // slime large
                },
                ambient: new Color(1.00f, 0.55f, 0.20f),  // intense orange-red
                minRoom: 6, splitDepth: 3, corridorW: 2f
            ),

            // ─────────────────────────────────────────────────────────────
            // Floor 5: Mroczna Otchłań — the primordial abyss
            // Cyclopean stone blocks (row 4) + ancient bone-dust floor (row 15).
            // Ambient: near-black indigo — light barely reaches here.
            // ─────────────────────────────────────────────────────────────
            new LevelTheme(
                "Mroczna Otchłań", "Piętro 5 — samo dno. Tu czai się coś starszego niż czas.",
                wallTop:   new Vector2Int(0, 4),   // row 4 col 0 — large stone top face
                wallSide:  new Vector2Int(1, 4),   // row 4 col 1 — large stone side A
                wallSide2: new Vector2Int(2, 4),   // row 4 col 2 — large stone side B
                floorBg:   new Vector2Int(0, 15),  // row 15 col 0 — darkest floor
                floor1:    new Vector2Int(1, 15),
                floor2:    new Vector2Int(2, 15),
                floor3:    new Vector2Int(3, 15),
                decos: new Vector2Int[]
                {
                    new Vector2Int(0, 23),   // coffin
                    new Vector2Int(3, 23),   // sarcophagus
                    new Vector2Int(14, 16),  // pentagram (special tile)
                    new Vector2Int(2, 22),   // slime
                    new Vector2Int(3, 22),   // slime large
                    new Vector2Int(0, 21),   // bones
                },
                ambient: new Color(0.30f, 0.28f, 0.55f),  // near-black indigo
                minRoom: 5, splitDepth: 3, corridorW: 1f
            ),
        };

        public static LevelTheme GetTheme(int floor)
        {
            int idx = Mathf.Clamp(floor, 0, Themes.Length - 1);
            return Themes[idx];
        }

        public static int FloorCount => Themes.Length; // 6: floors 0–5
    }
}
