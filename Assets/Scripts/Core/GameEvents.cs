using System;
using LochyIGorzala.Items;

namespace LochyIGorzala.Core
{
    /// <summary>
    /// Observer pattern implementation for game-wide event communication.
    /// Decouples game logic from UI — any system can subscribe to events
    /// without direct references to the publisher.
    /// </summary>
    public static class GameEvents
    {
        // Scene / Flow events
        public static event Action OnGameStarted;
        public static event Action OnGamePaused;
        public static event Action OnGameResumed;
        public static event Action<string> OnSceneChangeRequested;

        // Player events
        public static event Action<int, int> OnPlayerHealthChanged; // current, max
        public static event Action<int> OnPlayerXPGained;
        public static event Action<int> OnPlayerLevelUp;
        public static event Action<float> OnToxicityChanged;

        // Combat events
        public static event Action OnCombatStarted;
        public static event Action OnCombatEnded;
        public static event Action<string, int> OnDamageDealt; // targetName, amount
        public static event Action<int> OnPlayerDamaged;

        // Dungeon events
        public static event Action<int> OnRoomEntered; // roomIndex
        public static event Action OnDungeonGenerated;

        /// <summary>
        /// Fired by EnemySpawner when all enemies on the current floor are dead.
        /// DungeonGenerator subscribes to this and dynamically places the StairsDown tile.
        /// </summary>
        public static event Action OnAllEnemiesDefeated;

        // ── Inventory events (Observer for UI refresh) ────────────
        /// <summary>
        /// Fired whenever the bag or equip slots change (add, remove, equip, unequip, use).
        /// InventoryUIController subscribes to this to redraw the panel.
        /// </summary>
        public static event Action OnInventoryChanged;

        /// <summary>
        /// Fired when loot drops after combat, carrying the dropped item.
        /// CombatUIController uses this to show a "Loot!" popup before returning
        /// to the dungeon.
        /// </summary>
        public static event Action<ItemData> OnLootDropped;

        // Gold
        public static event Action<int> OnGoldChanged; // new total

        // Trap events
        public static event Action<int, int> OnTrapTriggered; // tileX, tileY

        // Quest events
        public static event Action<string> OnEnemyKilled;      // enemyName (e.g. "Chochlik")
        public static event Action<QuestData> OnQuestAccepted;
        public static event Action<QuestData> OnQuestProgress;  // kill count updated
        public static event Action<QuestData> OnQuestCompleted;
        public static event Action OnQuestJournalChanged;       // any change — journal UI refresh

        // Puzzle events
        public static event Action<int, int> OnPuzzleSwitchActivated; // activated, total
        public static event Action OnAllPuzzlesSolved;

        // Achievement events
        public static event Action<AchievementData> OnAchievementUnlocked;
        public static event Action OnAchievementsChanged; // any progress — UI refresh

        // UI events
        public static event Action<string> OnNotification; // message
        public static event Action<string, float> OnNotificationTimed; // message + duration

        // --- Invocation methods (only called from game logic) ---

        public static void RaiseGameStarted() => OnGameStarted?.Invoke();
        public static void RaiseGamePaused() => OnGamePaused?.Invoke();
        public static void RaiseGameResumed() => OnGameResumed?.Invoke();
        public static void RaiseSceneChangeRequested(string sceneName) => OnSceneChangeRequested?.Invoke(sceneName);

        public static void RaisePlayerHealthChanged(int current, int max) => OnPlayerHealthChanged?.Invoke(current, max);
        public static void RaisePlayerXPGained(int amount) => OnPlayerXPGained?.Invoke(amount);
        public static void RaisePlayerLevelUp(int newLevel) => OnPlayerLevelUp?.Invoke(newLevel);
        public static void RaiseToxicityChanged(float value) => OnToxicityChanged?.Invoke(value);

        public static void RaiseCombatStarted() => OnCombatStarted?.Invoke();
        public static void RaiseCombatEnded() => OnCombatEnded?.Invoke();
        public static void RaiseDamageDealt(string target, int amount) => OnDamageDealt?.Invoke(target, amount);
        public static void RaisePlayerDamaged(int amount) => OnPlayerDamaged?.Invoke(amount);

        public static void RaiseRoomEntered(int roomIndex) => OnRoomEntered?.Invoke(roomIndex);
        public static void RaiseDungeonGenerated() => OnDungeonGenerated?.Invoke();
        public static void RaiseAllEnemiesDefeated() => OnAllEnemiesDefeated?.Invoke();

        public static void RaiseInventoryChanged() => OnInventoryChanged?.Invoke();
        public static void RaiseLootDropped(ItemData item) => OnLootDropped?.Invoke(item);
        public static void RaiseGoldChanged(int total) => OnGoldChanged?.Invoke(total);

        public static void RaiseTrapTriggered(int x, int y) => OnTrapTriggered?.Invoke(x, y);

        public static void RaiseEnemyKilled(string enemyName) => OnEnemyKilled?.Invoke(enemyName);
        public static void RaiseQuestAccepted(QuestData q) => OnQuestAccepted?.Invoke(q);
        public static void RaiseQuestProgress(QuestData q) => OnQuestProgress?.Invoke(q);
        public static void RaiseQuestCompleted(QuestData q) => OnQuestCompleted?.Invoke(q);
        public static void RaiseQuestJournalChanged() => OnQuestJournalChanged?.Invoke();

        public static void RaisePuzzleSwitchActivated(int activated, int total) => OnPuzzleSwitchActivated?.Invoke(activated, total);
        public static void RaiseAllPuzzlesSolved() => OnAllPuzzlesSolved?.Invoke();

        public static void RaiseAchievementUnlocked(AchievementData a) => OnAchievementUnlocked?.Invoke(a);
        public static void RaiseAchievementsChanged() => OnAchievementsChanged?.Invoke();

        public static void RaiseNotification(string message) => OnNotification?.Invoke(message);
        public static void RaiseNotification(string message, float duration) => OnNotificationTimed?.Invoke(message, duration);

        /// <summary>
        /// Clears all event subscribers. Call when returning to main menu
        /// to prevent memory leaks from destroyed objects.
        /// </summary>
        public static void ClearAll()
        {
            OnGameStarted = null;
            OnGamePaused = null;
            OnGameResumed = null;
            OnSceneChangeRequested = null;
            OnPlayerHealthChanged = null;
            OnPlayerXPGained = null;
            OnPlayerLevelUp = null;
            OnToxicityChanged = null;
            OnCombatStarted = null;
            OnCombatEnded = null;
            OnDamageDealt = null;
            OnPlayerDamaged = null;
            OnRoomEntered = null;
            OnDungeonGenerated = null;
            OnAllEnemiesDefeated = null;
            OnInventoryChanged = null;
            OnLootDropped = null;
            OnGoldChanged = null;
            OnTrapTriggered = null;
            OnEnemyKilled = null;
            OnQuestAccepted = null;
            OnQuestProgress = null;
            OnQuestCompleted = null;
            OnQuestJournalChanged = null;
            OnPuzzleSwitchActivated = null;
            OnAllPuzzlesSolved = null;
            OnAchievementUnlocked = null;
            OnAchievementsChanged = null;
            OnNotification = null;
            OnNotificationTimed = null;
        }
    }
}
