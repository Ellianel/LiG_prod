using UnityEngine;
using System.Collections.Generic;
using LochyIGorzala.Enemies;
using LochyIGorzala.Helpers;
using LochyIGorzala.Managers;
using LochyIGorzala.Core;
using LochyIGorzala.Dungeon;

namespace LochyIGorzala.Combat
{
    /// <summary>
    /// Spawns exactly 4 regular enemies + 1 floor boss per dungeon floor (floors 1-4).
    /// Floor 5 spawns only the final boss Delirius.
    /// Floor 0 (Lobby) is a safe zone — no enemies.
    ///
    /// Kill-gate system: once aliveEnemyCount reaches 0, the floor is marked cleared
    /// and GameEvents.OnAllEnemiesDefeated fires so DungeonGenerator can place the stairs.
    ///
    /// Defeated enemies are tracked by stable position-based IDs so they never respawn
    /// after being killed (even after the Dungeon scene reloads for another combat).
    /// The same floor+seed always produces the same spawn positions, so the IDs are stable.
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [Header("Sprite Sheet")]
        [SerializeField] private Texture2D monstersSheet;

        private List<GameObject>              spawnedEnemies = new List<GameObject>();
        private Dictionary<GameObject, EnemyData> enemyDataMap = new Dictionary<GameObject, EnemyData>();
        // Maps each alive enemy GO → its stable ID (floor*10000 + x*100 + y)
        private Dictionary<GameObject, int>   enemyStableIdMap = new Dictionary<GameObject, int>();

        // Alive enemy count for the kill-gate
        private int aliveEnemyCount;
        // Simple counter for GO name uniqueness (not used as kill-gate key)
        private int nextSpawnIndex;

        // ── Static state that survives scene reloads ───────────────────────
        // Enemies that have been killed — stable IDs so they never respawn.
        // Cleared only on new game / load (ClearDefeatedEnemies).
        private static readonly HashSet<int> defeatedIds = new HashSet<int>();

        // ── Stable ID helpers ─────────────────────────────────────────────
        /// <summary>
        /// Generates a unique int from floor + tile coordinates.
        /// Map is at most 60×60, so x and y are 0..59.
        /// floor 0-5 → floor*10000 gives clean separation (max = 55959).
        /// </summary>
        private static int StableId(int floor, int x, int y) => floor * 10000 + x * 100 + y;

        // ── Unity lifecycle ───────────────────────────────────────────────

        private void Awake()
        {
            // Subscribe in Awake() so the event is ready before DungeonGenerator.Start() fires.
            GameEvents.OnDungeonGenerated += SpawnEnemies;
        }

        private void OnDestroy()
        {
            GameEvents.OnDungeonGenerated -= SpawnEnemies;
        }

        // ── Public API ────────────────────────────────────────────────────

        public void SpawnEnemies()
        {
            ClearLiveEnemies();
            aliveEnemyCount = 0;
            nextSpawnIndex  = 0;

            GameState state = GameManager.Instance?.CurrentGameState;
            if (state == null) return;

            int floor = state.CurrentFloor;

            // Floor 0 (Lobby) — safe zone
            if (floor == 0)
            {
                Debug.Log("EnemySpawner: Lobby — no enemies.");
                return;
            }

            // Floor already cleared → skip spawning and fire the stairs event right now
            if (IsFloorCleared(floor, state))
            {
                Debug.Log($"EnemySpawner: Floor {floor} already cleared — raising stairs event.");
                GameEvents.RaiseAllEnemiesDefeated();
                return;
            }

            // CRITICAL: Seed Unity's Random with a deterministic value based on
            // floor + dungeon seed so that FindSpawnPosition() always returns the
            // same sequence of positions for the same floor.  This ensures that
            // stable IDs (floor*10000 + x*100 + y) are truly stable across scene
            // reloads, preventing defeated enemies from respawning at new positions.
            var oldState = Random.state;
            int spawnSeed = state.Dungeon.Seed * 7919 + floor * 31;
            Random.InitState(spawnSeed);

            if (floor >= 5)
            {
                // Floor 5: only Delirius
                TrySpawnBoss(state, EnemyFactory.EnemyType.Delirius);
            }
            else
            {
                // Floors 1-4: 4 regular enemies + 1 floor boss
                var pool = BuildRegularPool(floor);
                for (int i = 0; i < 4; i++)
                    TrySpawnRegular(state, pool);

                TrySpawnBoss(state, GetFloorBossType(floor));
            }

            // Restore RNG state so dungeon gen / other systems aren't affected
            Random.state = oldState;

            Debug.Log($"EnemySpawner: Floor {floor} — spawned {aliveEnemyCount} enemies alive.");

            // Edge case: valid positions existed but every candidate was in defeatedIds
            // (floor almost cleared but not flagged yet)
            if (aliveEnemyCount == 0)
            {
                MarkFloorCleared(floor, state);
                GameEvents.RaiseAllEnemiesDefeated();
            }
        }

        // ── Spawning internals ────────────────────────────────────────────

        private void TrySpawnRegular(
            GameState state,
            List<(EnemyFactory.EnemyType type, float weight)> pool)
        {
            int floor = state.CurrentFloor;
            Vector2Int pos = FindSpawnPosition(state.Dungeon, isBoss: false);
            if (pos.x < 0) return;

            int sid = StableId(floor, pos.x, pos.y);
            if (defeatedIds.Contains(sid))
            {
                Debug.Log($"EnemySpawner: slot ({pos.x},{pos.y}) already defeated — skipped.");
                return;
            }

            EnemyData data = EnemyFactory.Create(PickRandom(pool));
            DoSpawn(data, pos, sid);
            aliveEnemyCount++;
        }

        private void TrySpawnBoss(GameState state, EnemyFactory.EnemyType bossType)
        {
            int floor = state.CurrentFloor;
            Vector2Int pos = FindSpawnPosition(state.Dungeon, isBoss: true);
            if (pos.x < 0)
                pos = FindSpawnPosition(state.Dungeon, isBoss: false); // fallback
            if (pos.x < 0) return;

            int sid = StableId(floor, pos.x, pos.y);
            if (defeatedIds.Contains(sid))
            {
                Debug.Log($"EnemySpawner: boss slot ({pos.x},{pos.y}) already defeated — skipped.");
                return;
            }

            EnemyData data = EnemyFactory.Create(bossType);
            DoSpawn(data, pos, sid);
            aliveEnemyCount++;
        }

        private void DoSpawn(EnemyData data, Vector2Int pos, int stableId)
        {
            GameObject obj = CreateEnemyObject(data, pos, nextSpawnIndex++);
            spawnedEnemies.Add(obj);
            enemyDataMap[obj]       = data;
            enemyStableIdMap[obj]   = stableId;
        }

        // ── Spawn position finder ─────────────────────────────────────────

        private Vector2Int FindSpawnPosition(DungeonData dungeon, bool isBoss)
        {
            var entrance = new Vector2(dungeon.EntranceX, dungeon.EntranceY);
            var exit      = new Vector2(dungeon.ExitX,     dungeon.ExitY);

            // Bosses spawn deep in the dungeon (near exit); regular enemies in the middle.
            // Tuned for 36×26 maps — tighter than 60×60 values so spawn succeeds reliably.
            // Tried in three passes, each relaxing the constraints, so a tight BSP layout
            // (e.g. floor 4 with minRoom=6) can never produce an empty floor — the previous
            // behaviour was to give up and leave the floor with zero enemies AND no stairs.
            float[][] passes = isBoss
                ? new[] { new[] { 14f, 2f }, new[] { 9f, 2f }, new[] { 4f, 1f } }
                : new[] { new[] { 6f, 4f }, new[] { 4f, 3f }, new[] { 2f, 1f } };

            for (int pass = 0; pass < passes.Length; pass++)
            {
                float minFromEntrance = passes[pass][0];
                float minFromExit     = passes[pass][1];

                for (int attempt = 0; attempt < 150; attempt++)
                {
                    int x = Random.Range(3, dungeon.Width  - 3);
                    int y = Random.Range(3, dungeon.Height - 3);

                    if (dungeon.GetTile(x, y) != (int)TileType.Floor) continue;

                    var pos = new Vector2(x, y);
                    if (Vector2.Distance(pos, entrance) < minFromEntrance) continue;
                    if (Vector2.Distance(pos, exit)     < minFromExit)     continue;

                    // No overlap with already-placed enemies (boss = larger radius)
                    bool overlap = false;
                    foreach (var existing in spawnedEnemies)
                    {
                        if (existing == null) continue;
                        float minDist = (enemyDataMap.TryGetValue(existing, out var ed) && ed.IsBoss) ? 3f : 2.5f;
                        if (Vector2.Distance(pos, (Vector2)existing.transform.position) < minDist)
                        { overlap = true; break; }
                    }
                    if (overlap) continue;

                    if (pass > 0)
                        Debug.Log($"EnemySpawner: relaxed-pass-{pass} spawn at ({x},{y}) isBoss={isBoss}");
                    return new Vector2Int(x, y);
                }
            }

            Debug.LogWarning($"EnemySpawner: Could not find spawn position after 3 passes (isBoss={isBoss}).");
            return new Vector2Int(-1, -1);
        }

        // ── GameObject creation ───────────────────────────────────────────

        private GameObject CreateEnemyObject(EnemyData data, Vector2Int pos, int index)
        {
            var obj = new GameObject($"Enemy_{data.Name}_{index}");
            obj.transform.position   = new Vector3(pos.x, pos.y, 0f);
            obj.transform.localScale = new Vector3(data.Scale, data.Scale, 1f);

            var sr = obj.AddComponent<SpriteRenderer>();
            sr.sortingOrder = data.IsBoss ? 8 : 5;

            if (monstersSheet != null)
            {
                Sprite spr = SpriteSheetHelper.ExtractSprite(monstersSheet, data.SpriteCol, data.SpriteRow);
                if (spr != null) sr.sprite = spr;
            }

            // Fallback coloured square if sprite missing
            if (sr.sprite == null)
            {
                Color c = data.IsBoss ? new Color(0.85f, 0.10f, 0.10f) : new Color(0.75f, 0.20f, 0.20f);
                var tex = new Texture2D(32, 32) { filterMode = FilterMode.Point };
                var px  = new Color[1024];
                for (int i = 0; i < 1024; i++) px[i] = c;
                tex.SetPixels(px); tex.Apply();
                sr.sprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);
            }

            return obj;
        }

        // ── Query API (called by PlayerController) ────────────────────────

        /// <summary>Returns the EnemyData for the enemy at worldPos, or null.</summary>
        public EnemyData GetEnemyAtPosition(Vector2 worldPos)
        {
            foreach (var kvp in enemyDataMap)
            {
                if (kvp.Key == null) continue;
                float radius = kvp.Value.IsBoss ? 0.9f : 0.6f;
                if (Vector2.Distance(worldPos, (Vector2)kvp.Key.transform.position) < radius)
                    return kvp.Value;
            }
            return null;
        }

        /// <summary>Returns the stable ID for the enemy at worldPos, or -1.</summary>
        public int GetEnemyIdAtPosition(Vector2 worldPos)
        {
            foreach (var kvp in enemyStableIdMap)
            {
                if (kvp.Key == null) continue;
                float radius = enemyDataMap.TryGetValue(kvp.Key, out var d) && d.IsBoss ? 0.9f : 0.6f;
                if (Vector2.Distance(worldPos, (Vector2)kvp.Key.transform.position) < radius)
                    return kvp.Value;
            }
            return -1;
        }

        /// <summary>
        /// Marks an enemy as permanently defeated by its stable ID.
        /// Called by GameManager.OnCombatWon() — NOT before combat.
        /// The Dungeon scene reloads after combat; SpawnEnemies will skip this ID
        /// and fire OnAllEnemiesDefeated if the floor is now clear.
        /// </summary>
        public static void MarkDefeated(int stableId)
        {
            if (stableId >= 0)
            {
                defeatedIds.Add(stableId);
                Debug.Log($"[EnemySpawner] Marked stableId={stableId} as defeated.");
            }
        }

        // ── Floor-cleared helpers ─────────────────────────────────────────

        private static bool IsFloorCleared(int floor, GameState state)
        {
            if (state?.FloorsCleared == null || floor >= state.FloorsCleared.Length) return false;
            return state.FloorsCleared[floor];
        }

        private static void MarkFloorCleared(int floor, GameState state)
        {
            if (state.FloorsCleared == null || state.FloorsCleared.Length < 6)
            {
                var arr = new bool[6];
                if (state.FloorsCleared != null)
                    System.Array.Copy(state.FloorsCleared, arr,
                        System.Math.Min(state.FloorsCleared.Length, 6));
                state.FloorsCleared = arr;
            }
            if (floor >= 0 && floor < state.FloorsCleared.Length)
                state.FloorsCleared[floor] = true;
        }

        // ── Enemy pool helpers ────────────────────────────────────────────

        private List<(EnemyFactory.EnemyType type, float weight)> BuildRegularPool(int floor)
        {
            float chochlikW = Mathf.Max(0f, 0.55f - floor * 0.12f);
            float utopiecW  = 0.35f;
            float strzygaW  = Mathf.Min(0.65f, 0.10f + floor * 0.12f);

            var pool = new List<(EnemyFactory.EnemyType, float)>();
            if (chochlikW > 0.01f) pool.Add((EnemyFactory.EnemyType.Chochlik, chochlikW));
            pool.Add((EnemyFactory.EnemyType.Utopiec, utopiecW));
            if (strzygaW > 0.01f) pool.Add((EnemyFactory.EnemyType.Strzyga, strzygaW));
            return pool;
        }

        private static EnemyFactory.EnemyType GetFloorBossType(int floor)
        {
            switch (floor)
            {
                case 1: return EnemyFactory.EnemyType.BossGargulec;
                case 2: return EnemyFactory.EnemyType.BossNekromanta;
                case 3: return EnemyFactory.EnemyType.BossWampir;
                case 4: return EnemyFactory.EnemyType.BossOgien;
                default: return EnemyFactory.EnemyType.BossGargulec;
            }
        }

        private EnemyFactory.EnemyType PickRandom(List<(EnemyFactory.EnemyType type, float weight)> table)
        {
            float total = 0f;
            foreach (var e in table) total += e.weight;
            float roll = Random.Range(0f, total);
            float cum  = 0f;
            foreach (var e in table) { cum += e.weight; if (roll <= cum) return e.type; }
            return table[0].type;
        }

        // ── Cleanup ───────────────────────────────────────────────────────

        private void ClearLiveEnemies()
        {
            foreach (var obj in spawnedEnemies)
                if (obj != null) Destroy(obj);
            spawnedEnemies.Clear();
            enemyDataMap.Clear();
            enemyStableIdMap.Clear();
            aliveEnemyCount = 0;
        }

        /// <summary>
        /// Clears all defeated-enemy memory.
        /// Call on new game or load — survivors from the previous run must not persist.
        /// </summary>
        public static void ClearDefeatedEnemies() => defeatedIds.Clear();

        /// <summary>
        /// DEBUG: Instantly kills all enemies on the current floor, marks them defeated,
        /// marks the floor cleared, and fires OnAllEnemiesDefeated so stairs appear.
        /// Bound to the 0 key in PlayerController for quick testing.
        /// </summary>
        public void DebugClearFloor()
        {
            GameState state = GameManager.Instance?.CurrentGameState;
            if (state == null) return;
            int floor = state.CurrentFloor;

            // Mark every spawned enemy as defeated
            foreach (var kvp in enemyStableIdMap)
                defeatedIds.Add(kvp.Value);

            // Destroy all enemy GameObjects
            ClearLiveEnemies();

            // Mark floor cleared + raise event so stairs appear
            MarkFloorCleared(floor, state);

            // Floor 5: set DeliriusDefeated so Władca Podziemi spawns
            if (floor == 5)
            {
                state.DeliriusDefeated = true;
                Managers.QuestManager.OnFloorReached(6);
                Managers.AchievementManager.OnFloorReached(6);
                Debug.Log("[EnemySpawner] DEBUG: Delirius force-killed — DeliriusDefeated = true");
            }

            GameEvents.RaiseAllEnemiesDefeated();

            // Auto-solve puzzles if this floor has them
            if (state.Dungeon.PuzzleSwitchesTotal > 0
                && state.Dungeon.PuzzleSwitchesActivated < state.Dungeon.PuzzleSwitchesTotal)
            {
                state.Dungeon.PuzzleSwitchesActivated = state.Dungeon.PuzzleSwitchesTotal;
                GameEvents.RaiseAllPuzzlesSolved();
                Debug.Log($"[EnemySpawner] DEBUG: Puzzles force-solved on floor {floor}.");
            }

            Debug.Log($"[EnemySpawner] DEBUG: Floor {floor} force-cleared.");
        }
    }
}
