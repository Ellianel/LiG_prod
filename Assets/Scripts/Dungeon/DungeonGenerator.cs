using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using LochyIGorzala.Core;
using LochyIGorzala.Helpers;
using LochyIGorzala.Managers;

namespace LochyIGorzala.Dungeon
{
    /// <summary>
    /// Generates dungeon tilemaps at runtime using level-specific themes.
    ///
    /// Floor 0 — Lobby: hand-crafted safe room, no enemies.
    /// Floors 1-5 — Procedural BSP: Binary Space Partitioning splits the map into
    ///              rectangular areas, places rooms in each leaf, and connects sibling
    ///              rooms with L-shaped corridors.  Each floor has a distinct visual theme.
    ///
    /// Implements Factory pattern (tile creation from sprite sheet coordinates) and
    /// raises GameEvents.OnDungeonGenerated as Observer notification.
    /// </summary>
    public class DungeonGenerator : MonoBehaviour
    {
        [Header("Tilemap References")]
        [SerializeField] private Tilemap floorTilemap;
        [SerializeField] private Tilemap wallTilemap;
        [SerializeField] private Tilemap decorTilemap;

        [Header("Sprite Sheet")]
        [SerializeField] private Texture2D tileSheet;

        [Header("Map Settings")]
        // Reduced from 60×60 to 36×26 so the whole playable area fits comfortably
        // in the camera view (orthoSize=8, aspect≈16:9 ⇒ ~28×16 visible tiles).
        // Smaller map also means less sparse dungeons on BSP.
        [SerializeField] private int mapWidth  = 36;
        [SerializeField] private int mapHeight = 26;

        // ── Cached tile assets (re-built per floor from theme config) ───
        private Tile floorBgTile;
        private Tile[] floorVariants = new Tile[3];
        private Tile wallSideTile;
        private Tile wallSideTile2;
        private Tile wallTopTile;   // "cap" tile rendered on walls whose south neighbour is floor
        private Tile stairsDownTile;
        private Tile stairsUpTile;
        private Tile[] decoTiles;

        // Stair tile positions are the same regardless of theme (17.h / 17.i in tiles.txt)
        private static readonly Vector2Int StairsDownCoord = new Vector2Int(7, 16);
        private static readonly Vector2Int StairsUpCoord   = new Vector2Int(8, 16);

        // Trap tile — row 22, col 1 (blood/crack sprite — visually reads as danger)
        private static readonly Vector2Int TrapTileCoord = new Vector2Int(1, 22);
        private Tile trapTile;

        // Puzzle switch tiles — gold key sprite (c0,r22) for inactive, ornate key (c1,r16) glow for active
        private static readonly Vector2Int PuzzleSwitchCoord       = new Vector2Int(0, 22);
        private static readonly Vector2Int PuzzleSwitchActiveCoord = new Vector2Int(6, 16);
        private Tile puzzleSwitchTile;
        private Tile puzzleSwitchActiveTile;

        // ── BSP node ────────────────────────────────────────────────────
        private class BSPNode
        {
            public int X, Y, W, H;
            public BSPNode Left, Right;
            public RectInt Room;
            public bool HasRoom;

            public BSPNode(int x, int y, int w, int h)
            { X = x; Y = y; W = w; H = h; }

            public bool IsLeaf => Left == null && Right == null;

            public RectInt GetAnyRoom()
            {
                if (HasRoom) return Room;
                if (Left  != null) return Left.GetAnyRoom();
                if (Right != null) return Right.GetAnyRoom();
                return new RectInt(X + 1, Y + 1, 3, 3);
            }
        }

        // ── Lifecycle ────────────────────────────────────────────────────

        private void Awake()
        {
            // ── Tile anchor fix ───────────────────────────────────────────
            // Unity's default tileAnchor is (0.5, 0.5, 0), which shifts tile sprites
            // so their visual centre is at (cellX + 0.5, cellY + 0.5).  Players and
            // enemies are positioned at integer world coordinates, so they end up at the
            // bottom-left corner of the tile they logically occupy — producing the
            // "collisions feel shifted by 1" symptom.
            //
            // Setting tileAnchor to Vector3.zero anchors each sprite's pivot to the
            // cell's INTEGER origin.  With our sprites' pivot at (0.5, 0.5) this means
            // the sprite centre coincides with the integer world coordinate, which is
            // exactly where the player and enemies are placed.
            SetTileAnchor(floorTilemap);
            SetTileAnchor(wallTilemap);
            SetTileAnchor(decorTilemap);

            // Subscribe early so we receive the event even if it fires on the same frame
            GameEvents.OnAllEnemiesDefeated += HandleAllEnemiesDefeated;
            GameEvents.OnAllPuzzlesSolved   += HandleAllPuzzlesSolved;
        }

        private static void SetTileAnchor(Tilemap tm)
        {
            if (tm != null) tm.tileAnchor = Vector3.zero;
        }

        private void OnDestroy()
        {
            GameEvents.OnAllEnemiesDefeated -= HandleAllEnemiesDefeated;
            GameEvents.OnAllPuzzlesSolved   -= HandleAllPuzzlesSolved;
        }

        private void Start() => GenerateAndRender();

        /// <summary>
        /// Generates dungeon data for CurrentFloor and renders it.
        /// Safe to call multiple times (re-generates).
        /// </summary>
        public void GenerateAndRender()
        {
            // Free sprites/tiles left over from previous scene loads.
            // Without this, runtime-created Tile ScriptableObjects and Sprite instances
            // accumulate across scene reloads and eventually starve the editor.
            Resources.UnloadUnusedAssets();

            GameState state = GameManager.Instance?.CurrentGameState;
            if (state == null)
            {
                state = new GameState();
                Debug.LogWarning("DungeonGenerator: No GameState — using default.");
            }

            int floor = state.CurrentFloor;
            LevelTheme theme = DungeonLevelThemes.GetTheme(floor);

            // Floor-specific seed so the same floor always generates identically on reload
            int seed = state.Dungeon.Seed ^ (floor * 999983 + 7);
            Random.InitState(seed);

            // Lobby (floor 0) uses a smaller fixed map; dungeon floors use the inspector values
            int w = (floor == 0) ? 28 : mapWidth;
            int h = (floor == 0) ? 22 : mapHeight;

            state.Dungeon.Width  = w;
            state.Dungeon.Height = h;
            state.Dungeon.TileMap = new int[w * h];

            LoadThemeTiles(theme);

            if (floor == 0)
                GenerateLobby(state.Dungeon);
            else
                GenerateProcedural(state.Dungeon, theme, floor);

            RenderMap(state.Dungeon, theme);

            // Wire camera bounds to the actual map size so the player never appears
            // to walk "off" the dungeon (previously bounds were hardcoded 50×50 while
            // maps were 60×60, so the camera stopped half a screen short of the edge).
            // Lobby (floor 0) is small enough that bounds clamping locks the camera
            // near the centre — disable bounds there so it follows the player freely.
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                var camFollow = mainCam.GetComponent<Player.CameraFollow>();
                if (camFollow != null)
                {
                    if (floor == 0)
                        camFollow.DisableBounds();
                    else
                        camFollow.SetBounds(w, h);
                }
            }

            GameEvents.RaiseDungeonGenerated();
        }

        // ── Tile factory (Factory pattern) ───────────────────────────────
        private void LoadThemeTiles(LevelTheme theme)
        {
            wallSideTile  = MakeTile(theme.WallSideTile)  ?? MakeColourTile(new Color(0.40f, 0.35f, 0.28f));
            wallSideTile2 = MakeTile(theme.WallSideTile2) ?? wallSideTile;
            wallTopTile   = MakeTile(theme.WallTopTile)   ?? wallSideTile;

            floorBgTile      = MakeTile(theme.FloorBgTile)    ?? MakeColourTile(new Color(0.18f, 0.18f, 0.22f));
            floorVariants[0] = MakeTile(theme.FloorVariant1)  ?? floorBgTile;
            floorVariants[1] = MakeTile(theme.FloorVariant2)  ?? floorBgTile;
            floorVariants[2] = MakeTile(theme.FloorVariant3)  ?? floorBgTile;

            stairsDownTile = MakeTile(StairsDownCoord);
            stairsUpTile   = MakeTile(StairsUpCoord);
            trapTile       = MakeTile(TrapTileCoord);
            puzzleSwitchTile       = MakeTile(PuzzleSwitchCoord);
            puzzleSwitchActiveTile = MakeTile(PuzzleSwitchActiveCoord);

            int decoLen = theme.DecorationTiles?.Length ?? 0;
            decoTiles = new Tile[decoLen];
            for (int i = 0; i < decoLen; i++)
                decoTiles[i] = MakeTile(theme.DecorationTiles[i]);
        }

        // ── Tile & fallback-texture caches (session-scoped) ──────────────
        // Prevents ScriptableObject.CreateInstance<Tile>() and Sprite.Create from running
        // on every scene reload. Without these caches the editor accumulates thousands of
        // Tile/Sprite instances across Dungeon↔Combat transitions and eventually freezes
        // on scene unload (see decisions.md — "Asset leak pomimo UnloadUnusedAssets").
        private static readonly Dictionary<(int sheetId, int col, int row), Tile> _tileCache
            = new Dictionary<(int, int, int), Tile>(64);
        private static readonly Dictionary<int, Tile> _colorTileCache
            = new Dictionary<int, Tile>(16);

        private Tile MakeTile(Vector2Int coord)
        {
            if (tileSheet == null) return null;

            var key = (tileSheet.GetHashCode(), coord.x, coord.y);
            if (_tileCache.TryGetValue(key, out Tile cached) && cached != null)
                return cached;

            Sprite s = SpriteSheetHelper.ExtractSprite(tileSheet, coord.x, coord.y);
            if (s == null) return null;
            var t = ScriptableObject.CreateInstance<Tile>();
            t.sprite = s;
            t.name   = $"Tile_{tileSheet.name}_c{coord.x}_r{coord.y}";
            _tileCache[key] = t;
            return t;
        }

        private Tile MakeColourTile(Color c)
        {
            // Quantise to 8-bit channels so tiny float drift doesn't miss the cache.
            int key = (Mathf.RoundToInt(c.r * 255f)      )
                    | (Mathf.RoundToInt(c.g * 255f) <<  8)
                    | (Mathf.RoundToInt(c.b * 255f) << 16)
                    | (Mathf.RoundToInt(c.a * 255f) << 24);
            if (_colorTileCache.TryGetValue(key, out Tile cached) && cached != null)
                return cached;

            var tex = new Texture2D(32, 32) { filterMode = FilterMode.Point };
            var px  = new Color[1024]; for (int i = 0; i < 1024; i++) px[i] = c;
            tex.SetPixels(px); tex.Apply();
            var sp = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);
            var t  = ScriptableObject.CreateInstance<Tile>();
            t.sprite = sp;
            t.name   = $"ColorTile_{key:X8}";
            _colorTileCache[key] = t;
            return t;
        }

        // ── Floor 0: Lobby (hand-crafted, 28×22) ────────────────────────

        /// <summary>
        /// Small entrance lobby — safe zone, no enemies.
        /// Layout (28 wide × 22 tall):
        ///   - One main room covering nearly the whole map
        ///   - Four 2×2 decorative pillars symmetrically placed
        ///   - Staircase to Floor 1 in the upper-right area
        ///   - Player spawns lower-left on a new game
        /// </summary>
        private void GenerateLobby(DungeonData d)
        {
            // d.Width = 28, d.Height = 22 (set in GenerateAndRender for floor 0)
            FillAll(d, TileType.WallSide);

            // ── Main room: 2-tile border of walls around a single open hall ──
            //    x: 2..25  (width 24)    y: 2..18  (height 17)
            CarveRoom(d, 2, 2, 24, 17);

            // ── Four symmetrical pillars (2×2) inside the hall ──────────
            PlacePillar(d,  5,  5);   // lower-left
            PlacePillar(d,  5, 13);   // upper-left
            PlacePillar(d, 19,  5);   // lower-right
            PlacePillar(d, 19, 13);   // upper-right

            // ── Left wall niche (small alcove for atmosphere) ────────────
            CarveRoom(d, 0, 8, 3, 6);   // x=0..2, y=8..13
            // open the niche into the main room
            for (int cy = 9; cy <= 12; cy++) d.SetTile(2, cy, (int)TileType.Floor);

            // ── Right wall niche ─────────────────────────────────────────
            CarveRoom(d, 25, 8, 3, 6);  // x=25..27, y=8..13
            for (int cy = 9; cy <= 12; cy++) d.SetTile(25, cy, (int)TileType.Floor);

            // ── Staircase to Floor 1 — upper-right of the hall ──────────
            //    Placed at (23, 16): well inside the room, visible in the corner.
            //    Only placed immediately if the intro has been completed (quest NPC talked to).
            //    Otherwise, TajemniczyJegomoscNPC places it after the intro dialogue.
            const int sx = 23, sy = 16;
            d.ExitX = sx; d.ExitY = sy;

            GameState curState = GameManager.Instance?.CurrentGameState;
            bool introCompleted = curState?.QuestData?.IntroCompleted ?? false;
            if (introCompleted)
            {
                d.SetTile(sx, sy, (int)TileType.StairsDown);
            }
            else
            {
                // Stairs hidden until player talks to Tajemniczy Jegomość
                d.SetTile(sx, sy, (int)TileType.Floor);
            }

            // Player default spawn — lower-left of the hall (away from stairs)
            d.EntranceX = 4; d.EntranceY = 4;

            // ── Scatter a handful of decorative props ────────────────────
            ScatterDecorations(d, 6, 2, seed: 42);
        }

        // ── Floors 1-5: BSP procedural ───────────────────────────────────

        /// <summary>
        /// BSP procedural generation:
        ///   1. Recursively split map into leaf nodes.
        ///   2. Place a random room inside each leaf.
        ///   3. Connect siblings with L-shaped corridors.
        ///   4. Place StairsUp (entrance) near first room, StairsDown (exit) near last room.
        ///   5. Scatter theme-appropriate decorations.
        /// </summary>
        private void GenerateProcedural(DungeonData d, LevelTheme theme, int floor)
        {
            int W = d.Width, H = d.Height;

            FillAll(d, TileType.WallSide);

            // Build BSP tree from the inner area (1-tile border)
            var root = new BSPNode(1, 1, W - 2, H - 2);
            SplitBSP(root, theme.MaxSplitDepth, theme.MinRoomSize);

            // Carve rooms & corridors, collect room list
            var rooms = new List<RectInt>();
            CarveRoomsAndCorridors(root, d, rooms, (int)theme.CorridorWidth);

            if (rooms.Count == 0)
            {
                // Fallback: single open room
                CarveRoom(d, 3, 3, W - 6, H - 6);
                rooms.Add(new RectInt(3, 3, W - 6, H - 6));
            }

            // ── Entrance stairs (StairsUp) — centre of room closest to (5,5) ──
            RectInt entrance = ClosestRoom(rooms, 5, 5);
            int ux = entrance.x + entrance.width  / 2;
            int uy = entrance.y + entrance.height / 2;
            d.SetTile(ux, uy, (int)TileType.StairsUp);
            // EntranceX/Y now points at the actual StairsUp TILE (not the spawn tile).
            // PlayerController.FindAdjacentSpawn() offsets east/north/west/south from here
            // to place the player on a non-stair Floor tile — otherwise the player spawns
            // ON the stair and instantly re-triggers a floor change (looping bug).
            d.EntranceX = ux;
            d.EntranceY = uy;

            // ── Exit stairs (StairsDown) — centre of room farthest from entrance ──
            // Only PLACE the tile if this floor has been cleared (kill-gate system).
            // On uncleared floors the stairs appear when OnAllEnemiesDefeated fires.
            {
                RectInt exitRoom = FarthestRoom(rooms, ux, uy);
                int dx = exitRoom.x + exitRoom.width  / 2;
                int dy = exitRoom.y + exitRoom.height / 2;
                // Nudge so they never coincide with StairsUp
                if (dx == ux && dy == uy) { dx = Mathf.Clamp(dx + 3, 1, W - 2); }

                // ExitX/Y is the StairsDown TILE position (single source of truth).
                // HandleAllEnemiesDefeated reads this to place the kill-gated stair, and
                // PlayerController uses it with FindAdjacentSpawn to spawn beside (not on)
                // the stair when the player ascends back from the floor below.
                //
                // Previously ExitY was set to FindSafeSpawnY (a south-offset Floor tile).
                // That caused the "2 stairs per floor" bug: on re-entering a cleared floor,
                // GenerateProcedural placed StairsDown at (dx,dy) AND SpawnEnemies fired
                // OnAllEnemiesDefeated which placed another StairsDown at (ExitX,ExitY).
                d.ExitX = dx;
                d.ExitY = dy;

                // Only place the tile if the floor is already cleared
                GameState curState = GameManager.Instance?.CurrentGameState;
                bool alreadyCleared = IsFloorCleared(curState, floor);
                if (alreadyCleared)
                {
                    d.SetTile(dx, dy, (int)TileType.StairsDown);
                    Debug.Log($"[DungeonGenerator] Floor {floor} cleared — placing StairsDown at ({dx},{dy})");
                }
                else
                {
                    Debug.Log($"[DungeonGenerator] Floor {floor} not yet cleared — StairsDown hidden.");
                }
            }

            // ── Decorations ──────────────────────────────────────────────
            ScatterDecorations(d, 8 + floor * 3, 2, seed: floor * 17);

            // ── Traps (floors 2+, increasing count with depth) ───────────
            if (floor >= 2)
            {
                int trapCount = 2 + floor;  // floor2=4, floor3=5, floor4=6, floor5=7
                ScatterTraps(d, trapCount, 2, seed: floor * 31 + 7);
            }
        }

        // ── BSP helpers ──────────────────────────────────────────────────

        private void SplitBSP(BSPNode node, int depth, int minRoom)
        {
            if (depth <= 0) return;
            int minSplit = (minRoom + 2) * 2;

            bool canH = node.W >= minSplit;
            bool canV = node.H >= minSplit;
            if (!canH && !canV) return;

            bool splitV = (!canH) ? true : (!canV) ? false : (node.H >= node.W);

            if (splitV)
            {
                int cut = Random.Range(minRoom + 2, node.H - minRoom - 1);
                node.Left  = new BSPNode(node.X, node.Y,          node.W, cut);
                node.Right = new BSPNode(node.X, node.Y + cut,    node.W, node.H - cut);
            }
            else
            {
                int cut = Random.Range(minRoom + 2, node.W - minRoom - 1);
                node.Left  = new BSPNode(node.X,         node.Y, cut,          node.H);
                node.Right = new BSPNode(node.X + cut,   node.Y, node.W - cut, node.H);
            }

            SplitBSP(node.Left,  depth - 1, minRoom);
            SplitBSP(node.Right, depth - 1, minRoom);
        }

        private void CarveRoomsAndCorridors(BSPNode node, DungeonData d, List<RectInt> rooms, int corrW)
        {
            if (node == null) return;

            if (node.IsLeaf)
            {
                int margin = 1;
                int rw = Random.Range(4, node.W - margin * 2 + 1);
                int rh = Random.Range(4, node.H - margin * 2 + 1);
                rw = Mathf.Max(rw, 4);
                rh = Mathf.Max(rh, 4);

                int rx = node.X + margin + Random.Range(0, Mathf.Max(1, node.W - margin * 2 - rw + 1));
                int ry = node.Y + margin + Random.Range(0, Mathf.Max(1, node.H - margin * 2 - rh + 1));

                CarveRoom(d, rx, ry, rw, rh);
                node.Room    = new RectInt(rx, ry, rw, rh);
                node.HasRoom = true;
                rooms.Add(node.Room);
            }
            else
            {
                CarveRoomsAndCorridors(node.Left,  d, rooms, corrW);
                CarveRoomsAndCorridors(node.Right, d, rooms, corrW);

                if (node.Left != null && node.Right != null)
                {
                    ConnectRooms(d, node.Left.GetAnyRoom(), node.Right.GetAnyRoom(), corrW);
                }
            }
        }

        // ── Geometry helpers ─────────────────────────────────────────────

        private void FillAll(DungeonData d, TileType t)
        {
            for (int i = 0; i < d.TileMap.Length; i++)
                d.TileMap[i] = (int)t;
        }

        private void CarveRoom(DungeonData d, int x, int y, int w, int h)
        {
            for (int cy = y; cy < y + h && cy < d.Height; cy++)
                for (int cx = x; cx < x + w && cx < d.Width; cx++)
                    d.SetTile(cx, cy, (int)TileType.Floor);
        }

        private void ConnectRooms(DungeonData d, RectInt a, RectInt b, int w)
        {
            int ax = a.x + a.width / 2, ay = a.y + a.height / 2;
            int bx = b.x + b.width / 2, by = b.y + b.height / 2;

            if (Random.value > 0.5f)
            {
                CarveH(d, Mathf.Min(ax, bx), Mathf.Max(ax, bx), ay, w);
                CarveV(d, Mathf.Min(ay, by), Mathf.Max(ay, by), bx, w);
            }
            else
            {
                CarveV(d, Mathf.Min(ay, by), Mathf.Max(ay, by), ax, w);
                CarveH(d, Mathf.Min(ax, bx), Mathf.Max(ax, bx), by, w);
            }
        }

        private void CarveH(DungeonData d, int x0, int x1, int y, int thick)
        {
            for (int x = x0; x <= x1 && x < d.Width; x++)
                for (int t = 0; t < thick; t++)
                    d.SetTile(x, Mathf.Clamp(y + t, 0, d.Height - 1), (int)TileType.Floor);
        }

        private void CarveV(DungeonData d, int y0, int y1, int x, int thick)
        {
            for (int y = y0; y <= y1 && y < d.Height; y++)
                for (int t = 0; t < thick; t++)
                    d.SetTile(Mathf.Clamp(x + t, 0, d.Width - 1), y, (int)TileType.Floor);
        }

        private void PlacePillar(DungeonData d, int x, int y)
        {
            d.SetTile(x,     y,     (int)TileType.WallSide);
            d.SetTile(x + 1, y,     (int)TileType.WallSide);
            d.SetTile(x,     y + 1, (int)TileType.WallSide);
            d.SetTile(x + 1, y + 1, (int)TileType.WallSide);
        }

        private RectInt ClosestRoom(List<RectInt> rooms, int tx, int ty)
        {
            RectInt best = rooms[0];
            float bestDist = float.MaxValue;
            foreach (var r in rooms)
            {
                float d = Vector2.Distance(new Vector2(r.x + r.width / 2f, r.y + r.height / 2f),
                                           new Vector2(tx, ty));
                if (d < bestDist) { bestDist = d; best = r; }
            }
            return best;
        }

        private RectInt FarthestRoom(List<RectInt> rooms, int tx, int ty)
        {
            RectInt best = rooms[0];
            float bestDist = 0f;
            foreach (var r in rooms)
            {
                float d = Vector2.Distance(new Vector2(r.x + r.width / 2f, r.y + r.height / 2f),
                                           new Vector2(tx, ty));
                if (d > bestDist) { bestDist = d; best = r; }
            }
            return best;
        }

        /// <summary>
        /// Returns the Y coordinate of the first Floor tile found scanning south from
        /// (stairX, stairY), staying inside the room bounds.
        /// Falls back to scanning north, then east/west, to always return a valid floor tile.
        /// </summary>
        private int FindSafeSpawnY(DungeonData d, int stairX, int stairY, RectInt room)
        {
            // Scan south (decreasing Y)
            for (int y = stairY - 1; y >= room.y; y--)
            {
                if ((TileType)d.GetTile(stairX, y) == TileType.Floor)
                    return y;
            }
            // Scan north (increasing Y)
            for (int y = stairY + 1; y < room.y + room.height; y++)
            {
                if ((TileType)d.GetTile(stairX, y) == TileType.Floor)
                    return y;
            }
            // Fallback: stairY itself (stair tile is walkable, won't re-trigger
            // immediately because isTransitioning is reset only after spawn)
            return stairY;
        }

        // ── Kill-gate: dynamic stairs placement ──────────────────────────

        private static bool IsFloorCleared(GameState state, int floor)
        {
            if (state?.FloorsCleared == null || floor >= state.FloorsCleared.Length) return false;
            return state.FloorsCleared[floor];
        }

        /// <summary>
        /// Called when EnemySpawner fires OnAllEnemiesDefeated.
        /// Places the StairsDown tile at the pre-calculated exit position and
        /// re-renders those cells on the tilemap so the player can see and use them.
        /// </summary>
        private void HandleAllEnemiesDefeated()
        {
            GameState state = GameManager.Instance?.CurrentGameState;
            if (state == null) return;

            int floor = state.CurrentFloor;
            if (floor <= 0) return; // Lobby has no locked stairs

            // ── Floor 5: no stairs — Delirius defeat handled by GameManager ──
            if (floor >= 5)
            {
                // Władca Podziemi spawns via NpcSpawner on scene reload
                // (DeliriusDefeated flag was set in OnCombatWon)
                Debug.Log("[DungeonGenerator] Floor 5 cleared — no stairs, Władca will spawn.");
                return;
            }

            // ── Puzzle floors: scatter rune switches instead of placing stairs ──
            if (PuzzleData.FloorHasPuzzle(floor))
            {
                int switchCount = PuzzleData.GetSwitchCount(floor);
                ScatterPuzzleSwitches(state.Dungeon, switchCount, floor);
                state.Dungeon.PuzzleSwitchesTotal     = switchCount;
                state.Dungeon.PuzzleSwitchesActivated  = 0;
                GameEvents.RaiseNotification(PuzzleData.SwitchesAppearedMessage);
                Debug.Log($"[DungeonGenerator] Floor {floor}: placed {switchCount} puzzle switches.");
                return;
            }

            // ── Non-puzzle floors: place stairs immediately ──
            PlaceStairsDownNow(state, floor);
        }

        /// <summary>
        /// Called when all puzzle switches on the current floor have been activated.
        /// Places the StairsDown tile — the final step of the puzzle gate.
        /// </summary>
        private void HandleAllPuzzlesSolved()
        {
            GameState state = GameManager.Instance?.CurrentGameState;
            if (state == null) return;

            int floor = state.CurrentFloor;
            GameEvents.RaiseNotification(PuzzleData.AllSwitchesSolvedMessage);
            PlaceStairsDownNow(state, floor);
        }

        /// <summary>
        /// Shared helper — writes StairsDown into the tilemap and re-renders it.
        /// Used by both the normal kill-gate flow and the puzzle-solved flow.
        /// </summary>
        private void PlaceStairsDownNow(GameState state, int floor)
        {
            int ex = state.Dungeon.ExitX;
            int ey = state.Dungeon.ExitY;

            // Write into the logical tilemap so IsWalkable / CheckForStairs sees it
            state.Dungeon.SetTile(ex, ey, (int)TileType.StairsDown);

            // Re-render just that tile (floor tile underneath + deco on top)
            var tilePos = new Vector3Int(ex, ey, 0);
            SetFloor(tilePos, ex, ey);
            if (stairsDownTile == null)
            {
                Debug.LogWarning("[DungeonGenerator] stairsDownTile was null — rebuilding from StairsDownCoord.");
                stairsDownTile = MakeTile(StairsDownCoord) ?? MakeColourTile(new Color(0.95f, 0.85f, 0.30f));
            }
            SetDeco(tilePos, stairsDownTile);

            if (floor >= 5)
                Debug.Log("[DungeonGenerator] DELIRIUS DEFEATED — exit portal appears!");
            else
                Debug.Log($"[DungeonGenerator] StairsDown placed at ({ex},{ey}) on floor {floor}!");
        }

        // ── Decorations ──────────────────────────────────────────────────

        private void ScatterDecorations(DungeonData d, int count, int minDistFromEdge, int seed)
        {
            // Use a local RNG so decoration positions don't shift enemy spawn RNG
            var rng = new System.Random(seed);
            int W = d.Width, H = d.Height;
            int placed = 0;

            for (int attempt = 0; attempt < count * 10 && placed < count; attempt++)
            {
                int x = rng.Next(minDistFromEdge + 1, W - minDistFromEdge - 1);
                int y = rng.Next(minDistFromEdge + 1, H - minDistFromEdge - 1);

                if ((TileType)d.GetTile(x, y) != TileType.Floor) continue;
                if (x == d.EntranceX && y == d.EntranceY) continue;
                if (x == d.ExitX     && y == d.ExitY)     continue;

                d.SetTile(x, y, (int)TileType.Chest);
                placed++;
            }
        }

        /// <summary>
        /// Places Trap tiles on random Floor positions, avoiding stairs, entrance,
        /// exit and existing decorations/chests. Uses its own RNG to stay deterministic.
        /// </summary>
        private void ScatterTraps(DungeonData d, int count, int minDistFromEdge, int seed)
        {
            var rng = new System.Random(seed);
            int W = d.Width, H = d.Height;
            int placed = 0;

            for (int attempt = 0; attempt < count * 15 && placed < count; attempt++)
            {
                int x = rng.Next(minDistFromEdge + 1, W - minDistFromEdge - 1);
                int y = rng.Next(minDistFromEdge + 1, H - minDistFromEdge - 1);

                if ((TileType)d.GetTile(x, y) != TileType.Floor) continue;
                if (x == d.EntranceX && y == d.EntranceY) continue;
                if (x == d.ExitX     && y == d.ExitY)     continue;

                d.SetTile(x, y, (int)TileType.Trap);
                placed++;
            }
        }

        // ── Puzzle switches ──────────────────────────────────────────────

        /// <summary>
        /// Places rune switch tiles on random Floor positions after enemies are cleared.
        /// Avoids stairs, entrance, exit and existing special tiles.
        /// </summary>
        private void ScatterPuzzleSwitches(DungeonData d, int count, int floor)
        {
            var rng = new System.Random(d.Seed ^ (floor * 7717 + 13));
            int W = d.Width, H = d.Height;
            int placed = 0;

            for (int attempt = 0; attempt < count * 20 && placed < count; attempt++)
            {
                int x = rng.Next(3, W - 3);
                int y = rng.Next(3, H - 3);

                if ((TileType)d.GetTile(x, y) != TileType.Floor) continue;
                if (x == d.EntranceX && y == d.EntranceY) continue;
                if (x == d.ExitX     && y == d.ExitY)     continue;

                d.SetTile(x, y, (int)TileType.PuzzleSwitch);
                placed++;

                // Render the switch tile immediately
                var pos = new Vector3Int(x, y, 0);
                SetFloor(pos, x, y);
                if (puzzleSwitchTile != null)
                    SetDeco(pos, puzzleSwitchTile);
            }
        }

        /// <summary>
        /// Re-renders a single tile as an activated puzzle switch.
        /// Called by PlayerController after the player steps on a rune.
        /// </summary>
        public void RenderPuzzleSwitchActive(int x, int y)
        {
            var pos = new Vector3Int(x, y, 0);
            SetFloor(pos, x, y);
            if (puzzleSwitchActiveTile != null)
                SetDeco(pos, puzzleSwitchActiveTile);
            else if (puzzleSwitchTile != null)
                SetDeco(pos, puzzleSwitchTile); // fallback
        }

        // ── Tilemap rendering ────────────────────────────────────────────

        private void RenderMap(DungeonData d, LevelTheme theme)
        {
            floorTilemap?.ClearAllTiles();
            wallTilemap?.ClearAllTiles();
            decorTilemap?.ClearAllTiles();

            for (int y = 0; y < d.Height; y++)
            {
                for (int x = 0; x < d.Width; x++)
                {
                    var pos  = new Vector3Int(x, y, 0);
                    var type = (TileType)d.GetTile(x, y);

                    switch (type)
                    {
                        case TileType.Floor:
                            SetFloor(pos, x, y);
                            break;

                        case TileType.WallSide:
                        case TileType.WallTop:
                            // Place floor underneath wall for visual continuity
                            if (floorTilemap != null) floorTilemap.SetTile(pos, floorBgTile);
                            SetWall(pos, x, y, d);
                            break;

                        case TileType.StairsDown:
                            SetFloor(pos, x, y);
                            SetDeco(pos, stairsDownTile);
                            break;

                        case TileType.StairsUp:
                            SetFloor(pos, x, y);
                            SetDeco(pos, stairsUpTile);
                            break;

                        case TileType.Chest:
                            SetFloor(pos, x, y);
                            SetRandomDeco(pos, x, y);
                            break;

                        case TileType.Trap:
                            // Render as normal floor — trap is hidden until stepped on
                            SetFloor(pos, x, y);
                            if (trapTile != null)
                                SetDeco(pos, trapTile);
                            break;

                        case TileType.PuzzleSwitch:
                            SetFloor(pos, x, y);
                            if (puzzleSwitchTile != null)
                                SetDeco(pos, puzzleSwitchTile);
                            break;

                        case TileType.PuzzleSwitchActive:
                            SetFloor(pos, x, y);
                            if (puzzleSwitchActiveTile != null)
                                SetDeco(pos, puzzleSwitchActiveTile);
                            break;
                    }
                }
            }

            // ── Tint tilemaps to match floor depth and atmosphere ────────
            // Each floor gets a distinct colour wash applied to all tiles uniformly.
            Color tint = theme.AmbientColor;
            if (floorTilemap != null) floorTilemap.color = Color.Lerp(Color.white, tint, 0.30f);
            if (wallTilemap  != null) wallTilemap.color  = Color.Lerp(Color.white, tint, 0.45f);
            if (decorTilemap != null) decorTilemap.color = Color.Lerp(Color.white, tint, 0.20f);

            // Tint camera background to match floor depth
            Camera cam = Camera.main;
            if (cam != null) cam.backgroundColor = tint * 0.08f;
        }

        private void SetFloor(Vector3Int pos, int x, int y)
        {
            if (floorTilemap == null) return;
            int hash    = Mathf.Abs(x * 7 + y * 13 + x * y) % 8;
            Tile chosen = hash == 0 ? floorBgTile : floorVariants[(hash - 1) % 3] ?? floorBgTile;
            floorTilemap.SetTile(pos, chosen);
        }

        private void SetWall(Vector3Int pos, int x, int y, DungeonData d)
        {
            if (wallTilemap == null) return;
            // If the tile directly south of this wall is walkable floor, render the
            // "top face" sprite — gives the illusion of a 3-D ledge viewed from above.
            bool isLedge = y > 0 && IsFloorLike((TileType)d.GetTile(x, y - 1));
            Tile t = isLedge ? (wallTopTile ?? wallSideTile)
                             : (((x + y) % 3 == 0) ? wallSideTile2 : wallSideTile);
            wallTilemap.SetTile(pos, t ?? wallSideTile);
        }

        private static bool IsFloorLike(TileType t) =>
            t == TileType.Floor || t == TileType.StairsDown || t == TileType.StairsUp
            || t == TileType.Chest || t == TileType.DoorOpen
            || t == TileType.PuzzleSwitch || t == TileType.PuzzleSwitchActive;

        private void SetDeco(Vector3Int pos, Tile tile)
        {
            if (tile == null) return;
            if (decorTilemap != null) decorTilemap.SetTile(pos, tile);
            else if (wallTilemap != null) wallTilemap.SetTile(pos, tile);
        }

        private void SetRandomDeco(Vector3Int pos, int x, int y)
        {
            if (decoTiles == null || decoTiles.Length == 0) return;
            int idx = Mathf.Abs(x * 17 + y * 13) % decoTiles.Length;
            SetDeco(pos, decoTiles[idx]);
        }

        // ── Public tile placement (called at runtime by NPCs) ────────────

        /// <summary>
        /// Places a stairs tile on the tilemap at runtime (used by TajemniczyJegomoscNPC
        /// to reveal stairs after the intro dialogue).
        /// </summary>
        public void PlaceStairsTile(int x, int y, bool isDown)
        {
            var pos = new Vector3Int(x, y, 0);
            // Render floor underneath
            if (floorTilemap != null && floorBgTile != null)
                floorTilemap.SetTile(pos, floorBgTile);
            // Render stair decoration
            Tile stairTile = isDown ? stairsDownTile : stairsUpTile;
            if (decorTilemap != null && stairTile != null)
                decorTilemap.SetTile(pos, stairTile);

            Debug.Log($"[DungeonGenerator] Placed stairs ({(isDown ? "down" : "up")}) at ({x},{y}) at runtime.");
        }

        // ── Walkability query ────────────────────────────────────────────

        /// <summary>
        /// Returns true if the given world position corresponds to a walkable tile.
        /// Called by PlayerController for movement validation.
        /// </summary>
        public bool IsWalkable(Vector2 worldPos)
        {
            GameState state = GameManager.Instance?.CurrentGameState;
            if (state == null) return false;

            int x = Mathf.FloorToInt(worldPos.x + 0.5f);
            int y = Mathf.FloorToInt(worldPos.y + 0.5f);

            if (x < 0 || x >= state.Dungeon.Width || y < 0 || y >= state.Dungeon.Height)
                return false;

            TileType t = (TileType)state.Dungeon.GetTile(x, y);
            return t == TileType.Floor
                || t == TileType.StairsDown
                || t == TileType.StairsUp
                || t == TileType.Chest
                || t == TileType.Trap
                || t == TileType.DoorOpen
                || t == TileType.PuzzleSwitch
                || t == TileType.PuzzleSwitchActive;
        }
    }
}
