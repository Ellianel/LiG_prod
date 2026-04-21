using LochyIGorzala.Core;

namespace LochyIGorzala.Managers
{
    /// <summary>
    /// Static achievement manager — subscribes to game events and tracks progress.
    /// Pattern mirrors QuestManager: Initialize() on game start, Cleanup() on menu return.
    /// Pure logic — no MonoBehaviour.
    /// </summary>
    public static class AchievementManager
    {
        private static bool _subscribed;

        // ── Boss names for BossKill tracking ────────────────────────
        private static readonly string[] BossNames = new string[]
        {
            "Gargulec Trupiooki", "Nekromanta Martwicy", "Lord Wampirów", "Ognisty Diabeł", "DELIRIUS"
        };

        /// <summary>Call once on game start/load to wire up event listeners.</summary>
        public static void Initialize()
        {
            if (_subscribed) return;

            GameEvents.OnEnemyKilled       += OnEnemyKilled;
            GameEvents.OnGoldChanged       += OnGoldChanged;
            GameEvents.OnQuestCompleted    += OnQuestCompleted;
            GameEvents.OnTrapTriggered     += OnTrapTriggered;
            GameEvents.OnAllPuzzlesSolved  += OnPuzzleSolved;
            GameEvents.OnToxicityChanged   += OnToxicityChanged;

            _subscribed = true;
        }

        /// <summary>Call on return to main menu to prevent leaks.</summary>
        public static void Cleanup()
        {
            GameEvents.OnEnemyKilled       -= OnEnemyKilled;
            GameEvents.OnGoldChanged       -= OnGoldChanged;
            GameEvents.OnQuestCompleted    -= OnQuestCompleted;
            GameEvents.OnTrapTriggered     -= OnTrapTriggered;
            GameEvents.OnAllPuzzlesSolved  -= OnPuzzleSolved;
            GameEvents.OnToxicityChanged   -= OnToxicityChanged;

            _subscribed = false;
        }

        // ── Event handlers ──────────────────────────────────────────

        private static void OnEnemyKilled(string enemyName)
        {
            // General kill counter
            TryIncrement("first_blood");
            TryIncrement("monster_slayer");

            // Boss kill tracking
            if (IsBoss(enemyName))
            {
                TryIncrement("boss_hunter");
            }

            // Delirius special
            if (enemyName == "DELIRIUS")
            {
                TryIncrement("delirius_slayer");
            }

            // Floor reach: check after kills because GoToNextFloor fires after combat
            CheckFloorReach();
        }

        private static void OnGoldChanged(int newTotal)
        {
            // Gold accumulate — set current to max ever seen
            var state = GameManager.Instance?.CurrentGameState;
            if (state == null) return;

            var progress = state.Achievements.GetOrCreate("rich_man");
            if (!progress.Unlocked && newTotal > progress.CurrentCount)
            {
                progress.CurrentCount = newTotal;
                var def = AchievementDatabase.GetById("rich_man");
                if (def != null && progress.CurrentCount >= def.TargetCount)
                    Unlock(progress, def);
                else
                    GameEvents.RaiseAchievementsChanged();
            }
        }

        private static void OnQuestCompleted(QuestData quest)
        {
            // Only count kill quests (floor quests are auto-completed, less interesting)
            if (quest.Type == QuestType.Kill)
            {
                TryIncrement("quest_helper");
            }
        }

        private static void OnTrapTriggered(int x, int y)
        {
            TryIncrement("trap_veteran");
        }

        private static void OnPuzzleSolved()
        {
            TryIncrement("rune_master");
        }

        /// <summary>
        /// Tracks bimber/nalewka usage by detecting toxicity increases.
        /// Called on OnToxicityChanged — we track via a simple rising-edge detector.
        /// Since bimber always raises toxicity, any positive change during combat counts.
        /// </summary>
        private static float _lastToxicity = -1f;

        private static void OnToxicityChanged(float newToxicity)
        {
            // Only count increases (bimber/nalewka raise toxicity)
            if (_lastToxicity >= 0f && newToxicity > _lastToxicity)
            {
                TryIncrement("alcoholic");
            }
            _lastToxicity = newToxicity;
        }

        /// <summary>
        /// Call this after starting/loading a game to sync the toxicity tracker.
        /// </summary>
        public static void SyncToxicity(float currentToxicity)
        {
            _lastToxicity = currentToxicity;
        }

        // ── Floor reach check (called after enemy killed) ────────────

        private static void CheckFloorReach()
        {
            var state = GameManager.Instance?.CurrentGameState;
            if (state == null) return;

            int floor = state.CurrentFloor;
            if (floor >= 5)
            {
                var progress = state.Achievements.GetOrCreate("deep_diver");
                if (!progress.Unlocked)
                {
                    progress.CurrentCount = floor;
                    var def = AchievementDatabase.GetById("deep_diver");
                    if (def != null && progress.CurrentCount >= def.TargetCount)
                        Unlock(progress, def);
                }
            }
        }

        /// <summary>
        /// Check floor reach externally — called by GameManager.GoToNextFloor().
        /// </summary>
        public static void OnFloorReached(int floor)
        {
            var state = GameManager.Instance?.CurrentGameState;
            if (state == null) return;

            var progress = state.Achievements.GetOrCreate("deep_diver");
            if (!progress.Unlocked && floor >= 5)
            {
                progress.CurrentCount = floor;
                var def = AchievementDatabase.GetById("deep_diver");
                if (def != null && progress.CurrentCount >= def.TargetCount)
                    Unlock(progress, def);
            }
        }

        // ── Core helpers ────────────────────────────────────────────

        /// <summary>
        /// Increments the counter for a given achievement and checks for unlock.
        /// </summary>
        private static void TryIncrement(string achievementId)
        {
            var state = GameManager.Instance?.CurrentGameState;
            if (state == null) return;

            var progress = state.Achievements.GetOrCreate(achievementId);
            if (progress.Unlocked) return; // Already unlocked

            progress.CurrentCount++;

            var def = AchievementDatabase.GetById(achievementId);
            if (def != null && progress.CurrentCount >= def.TargetCount)
            {
                Unlock(progress, def);
            }
            else
            {
                GameEvents.RaiseAchievementsChanged();
            }
        }

        private static void Unlock(AchievementProgress progress, AchievementData def)
        {
            progress.Unlocked = true;
            GameEvents.RaiseAchievementUnlocked(def);
            GameEvents.RaiseAchievementsChanged();
            GameEvents.RaiseNotification($"Osiągnięcie odblokowane: {def.Title}!");
        }

        private static bool IsBoss(string enemyName)
        {
            foreach (var boss in BossNames)
                if (boss == enemyName) return true;
            return false;
        }

        // ── Public query helpers (for UI) ───────────────────────────

        /// <summary>Returns the number of unlocked achievements.</summary>
        public static int GetUnlockedCount()
        {
            var state = GameManager.Instance?.CurrentGameState;
            if (state == null) return 0;

            int count = 0;
            foreach (var p in state.Achievements.Progress)
                if (p.Unlocked) count++;
            return count;
        }

        /// <summary>Returns total number of achievements in the game.</summary>
        public static int GetTotalCount() => AchievementDatabase.All.Length;
    }
}
