using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using LochyIGorzala.Core;
using LochyIGorzala.Enemies;
using LochyIGorzala.Items;

namespace LochyIGorzala.Managers
{
    /// <summary>
    /// Where the player should appear when the Dungeon scene loads.
    /// Consumed by PlayerController once then reset to Default.
    /// </summary>
    public enum SpawnPoint { Default, AtEntrance, AtExit }

    /// <summary>
    /// Singleton pattern — single instance persists across all scenes.
    /// Manages overall game state, combat transitions, save/load, and floor navigation.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Game State")]
        public GameState CurrentGameState { get; private set; }
        public bool IsGameRunning { get; private set; }

        /// <summary>
        /// Hints to PlayerController where to position the player when the Dungeon scene loads.
        /// Reset to Default after it is consumed.
        /// </summary>
        public SpawnPoint NextSpawnPoint { get; private set; } = SpawnPoint.Default;

        // Combat data (passed between Dungeon and Combat scenes)
        public EnemyData CurrentCombatEnemy { get; set; }
        public Vector2 CombatEnemyPosition { get; set; }
        public int CombatEnemyId { get; set; }
        public bool LastCombatWon { get; private set; }

        /// <summary>
        /// Set to true when the player dies in combat (OnCombatLost).
        /// NotificationUIController reads and clears this to show the death message.
        /// Not serialized — only lives for one scene transition.
        /// </summary>
        public bool ShowDeathMessage { get; set; }

        // ── Runtime inventory (wraps CurrentGameState.Player.Inventory) ──
        /// <summary>
        /// Runtime Inventory instance — rebuilt whenever a game is started or loaded.
        /// Pass this reference to CombatEngine and InventoryUIController.
        /// </summary>
        public Inventory PlayerInventory { get; private set; }

        private const string SaveFilePrefix = "lochy_save_slot_";
        private const string SaveFileExtension = ".json";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (CurrentGameState == null)
            {
                CurrentGameState = new GameState();
            }
        }

        private void Update()
        {
            // Track play time while game is running in the dungeon
            if (IsGameRunning && CurrentGameState != null)
            {
                CurrentGameState.PlayTimeSeconds += Time.deltaTime;
            }
        }

        private void OnEnable()
        {
            GameEvents.OnSceneChangeRequested += HandleSceneChange;
            // Note: OnGoldChanged is a pure UI notification — GameManager does NOT
            // subscribe to it for state changes. Gold is added directly in:
            //   CombatUIController (after victory) and ShopUIController (buy/sell).
            GameEvents.OnLootDropped += HandleLootDropped;
        }

        private void OnDisable()
        {
            GameEvents.OnSceneChangeRequested -= HandleSceneChange;
            GameEvents.OnLootDropped          -= HandleLootDropped;
        }

        // ── Loot handler ──────────────────────────────────────────

        private void HandleLootDropped(ItemData item)
        {
            if (item == null || PlayerInventory == null) return;
            // Auto-pick up loot; if bag full the Inventory.AddItem fires a notification
            PlayerInventory.AddItem(item.ItemId);
        }

        // ─────────────────────────────────────────────────────────
        //  Inventory helper
        // ─────────────────────────────────────────────────────────

        private void RebuildInventory()
        {
            if (CurrentGameState?.Player == null) return;
            if (CurrentGameState.Player.Inventory == null)
                CurrentGameState.Player.Inventory = new InventoryData();
            PlayerInventory = new Inventory(CurrentGameState.Player.Inventory, CurrentGameState.Player);
        }

        // --- Public API ---

        /// <summary>
        /// Goes to character selection screen before starting a new game.
        /// </summary>
        public void GoToCharacterSelect()
        {
            LoadScene("CharSelect");
        }

        /// <summary>
        /// Starts a new game with the chosen character class.
        /// Called by CharSelectController after class is selected.
        /// </summary>
        public void StartNewGame(Core.PlayerClass chosenClass = Core.PlayerClass.Wojownik)
        {
            CurrentGameState = new GameState();
            CurrentGameState.Player = Core.CharacterClassFactory.CreatePlayer(chosenClass);
            CurrentGameState.CurrentFloor = 0;  // Start in the Lobby
            CurrentGameState.Dungeon.Seed = UnityEngine.Random.Range(1, int.MaxValue);
            NextSpawnPoint = SpawnPoint.Default;
            IsGameRunning = true;
            LastCombatWon = false;
            Combat.EnemySpawner.ClearDefeatedEnemies();
            RebuildInventory();
            QuestManager.Initialize();
            AchievementManager.Initialize();
            AchievementManager.SyncToxicity(CurrentGameState.Player.Toxicity);
            GameEvents.RaiseGameStarted();
            LoadScene("Dungeon");
        }

        public void ReturnToMainMenu()
        {
            IsGameRunning = false;
            QuestManager.Cleanup();
            AchievementManager.Cleanup();
            // ClearAll() removes ALL event subscribers including this GameManager.
            // Re-subscribe both persistent handlers immediately after clearing.
            GameEvents.ClearAll();
            GameEvents.OnSceneChangeRequested += HandleSceneChange;
            GameEvents.OnLootDropped          += HandleLootDropped;
            LoadScene("MainMenu");
        }

        public void QuitGame()
        {
            Debug.Log("Quitting game...");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // --- Combat Flow ---

        /// <summary>
        /// Initiates combat with an enemy. Saves position and transitions to Combat scene.
        /// </summary>
        public void StartCombat(EnemyData enemy, Vector2 enemyPosition, int enemyId)
        {
            CurrentCombatEnemy = enemy;
            CombatEnemyPosition = enemyPosition;
            CombatEnemyId = enemyId;
            LastCombatWon = false;

            // Save player position before combat
            CurrentGameState.Player.PositionX = enemyPosition.x;
            CurrentGameState.Player.PositionY = enemyPosition.y;

            GameEvents.RaiseCombatStarted();
            LoadScene("Combat");
        }

        public void OnCombatWon()
        {
            LastCombatWon = true;

            // Mark this specific enemy as permanently defeated so it never respawns.
            // On flee/loss we do NOT call this — the enemy stays alive with full HP.
            Combat.EnemySpawner.MarkDefeated(CombatEnemyId);

            // Detect Delirius defeat — set flag immediately so Władca spawns on reload
            bool killedDelirius = CurrentCombatEnemy is Enemies.Delirius;
            if (killedDelirius && CurrentGameState != null)
            {
                CurrentGameState.DeliriusDefeated = true;
                QuestManager.OnFloorReached(6); // floor 6 = "past Delirius" for quest
                AchievementManager.OnFloorReached(6);
                Debug.Log("[GameManager] DELIRIUS DEFEATED — Władca Podziemi will spawn on floor 5!");
            }

            // Notify quest system about the kill (before clearing the reference)
            if (CurrentCombatEnemy != null)
                GameEvents.RaiseEnemyKilled(CurrentCombatEnemy.Name);

            CurrentCombatEnemy = null;

            // Nudge the player 1 tile south so they don't respawn exactly on the
            // enemy's tile (avoids instant re-encounter with a nearby enemy).
            if (CurrentGameState?.Player != null)
                CurrentGameState.Player.PositionY -= 1f;

            GameEvents.RaiseCombatEnded();
        }

        public void OnCombatLost()
        {
            LastCombatWon = false;
            CurrentCombatEnemy = null;
            // Reset player HP and toxicity on death
            CurrentGameState.Player.CurrentHP = CurrentGameState.Player.MaxHP;
            CurrentGameState.Player.Toxicity = 0f;
            // Send player back to lobby (floor 0) on death
            CurrentGameState.CurrentFloor = 0;
            NextSpawnPoint = SpawnPoint.AtEntrance;
            ShowDeathMessage = true;
            GameEvents.RaiseCombatEnded();
            LoadScene("Dungeon");
        }

        public void OnCombatFled()
        {
            LastCombatWon = false;
            CurrentCombatEnemy = null;
            // Keep player at the enemy position — PlayerController.FindValidSpawn
            // will spiral-search to a valid floor tile if needed.
            // (The old X-2 nudge often landed in walls.)
            GameEvents.RaiseCombatEnded();
            LoadScene("Dungeon");
        }

        // --- Floor Navigation ---

        /// <summary>
        /// Consumes the NextSpawnPoint hint. Called by PlayerController after reading it.
        /// </summary>
        public void ResetSpawnPoint() => NextSpawnPoint = SpawnPoint.Default;

        /// <summary>
        /// Descend to the next dungeon floor.  Lobby→Floor1→…→Floor5.
        /// Player will spawn at the StairsUp (entrance) of the new floor.
        /// </summary>
        public void GoToNextFloor()
        {
            if (CurrentGameState == null) return;
            int next = CurrentGameState.CurrentFloor + 1;

            if (next > 5)
            {
                // Safety fallback — floor 5 has no stairs, so this should never be reached.
                // Delirius defeat is handled in OnCombatWon() which sets DeliriusDefeated.
                Debug.Log("[GameManager] GoToNextFloor called past floor 5 — ignoring.");
                return;
            }

            CurrentGameState.CurrentFloor = next;
            QuestManager.OnFloorReached(next);
            AchievementManager.OnFloorReached(next);
            NextSpawnPoint = SpawnPoint.AtEntrance;
            Debug.Log($"[GameManager] Descending to floor {next}");
            LoadScene("Dungeon");
        }

        /// <summary>
        /// Ascend to the previous dungeon floor.  Floor1→Lobby.
        /// Player will spawn at the StairsDown (exit) of the upper floor.
        /// </summary>
        public void GoToPreviousFloor()
        {
            if (CurrentGameState == null) return;
            int prev = CurrentGameState.CurrentFloor - 1;
            if (prev < 0) return; // Already in the lobby

            CurrentGameState.CurrentFloor = prev;
            NextSpawnPoint = SpawnPoint.AtExit;
            Debug.Log($"[GameManager] Ascending to floor {prev}");
            LoadScene("Dungeon");
        }

        // --- Save / Load (3-Slot Serialization) ---

        private string GetSlotPath(int slot) =>
            Path.Combine(Application.persistentDataPath, $"{SaveFilePrefix}{slot}{SaveFileExtension}");

        /// <summary>Saves current game state to a specific slot (1–3).</summary>
        public void SaveToSlot(int slot)
        {
            if (!IsGameRunning || CurrentGameState == null) return;

            try
            {
                CurrentGameState.SaveDate = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
                string json = JsonUtility.ToJson(CurrentGameState, true);
                File.WriteAllText(GetSlotPath(slot), json);
                GameEvents.RaiseNotification($"Gra zapisana w slocie {slot}!");
                Debug.Log($"Saved slot {slot} to: {GetSlotPath(slot)}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"SaveToSlot({slot}) failed: {ex.Message}");
            }
        }

        /// <summary>Loads game from a specific slot and transitions to Dungeon scene.</summary>
        public bool LoadFromSlot(int slot)
        {
            string path = GetSlotPath(slot);
            if (!File.Exists(path))
            {
                Debug.LogWarning($"Slot {slot} is empty.");
                return false;
            }

            try
            {
                string json = File.ReadAllText(path);
                CurrentGameState = JsonUtility.FromJson<GameState>(json);
                IsGameRunning = true;
                Combat.EnemySpawner.ClearDefeatedEnemies();
                RebuildInventory();
                QuestManager.Initialize();
                AchievementManager.Initialize();
                AchievementManager.SyncToxicity(CurrentGameState.Player.Toxicity);
                GameEvents.RaiseGameStarted();
                LoadScene("Dungeon");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"LoadFromSlot({slot}) failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>Returns the saved GameState for preview (class/level/time), or null if slot is empty.</summary>
        public GameState GetSlotPreview(int slot)
        {
            string path = GetSlotPath(slot);
            if (!File.Exists(path)) return null;
            try { return JsonUtility.FromJson<GameState>(File.ReadAllText(path)); }
            catch { return null; }
        }

        public bool HasSaveInSlot(int slot) => File.Exists(GetSlotPath(slot));
        public bool HasAnySave() => HasSaveInSlot(1) || HasSaveInSlot(2) || HasSaveInSlot(3);

        // Legacy single-save kept for F5 quick-save (saves to slot 1)
        public void SaveGame() => SaveToSlot(1);
        public bool HasSaveFile() => HasAnySave();

        // --- Scene Management ---

        private void LoadScene(string sceneName)
        {
            // Fire an asset-cleanup pass BEFORE unloading the current scene.
            // SpriteSheetHelper and DungeonGenerator now cache their sprites/tiles, so the
            // pool of "unused" assets is tiny — this call returns almost immediately instead
            // of grinding through thousands of orphaned Sprite/Tile instances on every
            // Dungeon↔Combat transition (which was the freeze root cause).
            // The AsyncOperation is intentionally not awaited; Unity runs it in parallel
            // with the scene load.
            Resources.UnloadUnusedAssets();
            SceneManager.LoadScene(sceneName);
        }

        private void HandleSceneChange(string sceneName)
        {
            LoadScene(sceneName);
        }
    }
}
