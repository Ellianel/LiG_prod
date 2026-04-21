using System.Collections.Generic;
using LochyIGorzala.Core;

namespace LochyIGorzala.Managers
{
    /// <summary>
    /// Static quest manager — tracks active quests, listens to OnEnemyKilled,
    /// updates kill counts, fires completion events.
    /// Also tracks floor progression quests (auto-completed on floor entry).
    /// Pure logic — no MonoBehaviour. Called from GameManager on game start/load.
    /// </summary>
    public static class QuestManager
    {
        private static bool _subscribed;

        /// <summary>Call once on game start/load to wire up event listeners.</summary>
        public static void Initialize()
        {
            if (!_subscribed)
            {
                GameEvents.OnEnemyKilled += OnEnemyKilled;
                _subscribed = true;
            }
        }

        /// <summary>Call on return to main menu to prevent leaks.</summary>
        public static void Cleanup()
        {
            GameEvents.OnEnemyKilled -= OnEnemyKilled;
            _subscribed = false;
        }

        // ── Quest operations ─────────────────────────────────────────

        /// <summary>
        /// Accepts a kill quest — copies the template into save data as Active.
        /// </summary>
        public static void AcceptQuest(QuestData template)
        {
            var state = GameManager.Instance?.CurrentGameState;
            if (state == null) return;

            // Don't accept duplicates
            foreach (var q in state.QuestData.Quests)
                if (q.Id == template.Id) return;

            QuestData quest;
            if (template.Type == QuestType.Floor)
            {
                quest = new QuestData(
                    template.Id, template.Title, template.Description,
                    template.TargetFloor);
            }
            else
            {
                quest = new QuestData(
                    template.Id, template.Title, template.Description,
                    template.TargetEnemyName, template.RequiredKills,
                    template.RewardItemId, template.RewardItemCount,
                    template.RewardMessage);
            }
            quest.Status = QuestStatus.Active;

            state.QuestData.Quests.Add(quest);
            GameEvents.RaiseQuestAccepted(quest);
            GameEvents.RaiseQuestJournalChanged();
        }

        /// <summary>
        /// Accepts all floor progression quests at once (called on intro).
        /// </summary>
        public static void AcceptFloorQuests()
        {
            foreach (var fq in QuestDatabase.FloorQuests)
                AcceptQuest(fq);
        }

        /// <summary>
        /// Returns the next kill-quest to offer, or null if all done.
        /// </summary>
        public static QuestData GetNextKillQuestToOffer()
        {
            var state = GameManager.Instance?.CurrentGameState;
            if (state == null) return null;

            int idx = state.QuestData.NextQuestIndex;
            if (idx >= QuestDatabase.KillQuests.Length) return null;
            return QuestDatabase.KillQuests[idx];
        }

        /// <summary>
        /// Finds a completed (but not yet rewarded) kill quest, or null.
        /// Floor quests are self-rewarding and don't need NPC return.
        /// </summary>
        public static QuestData GetCompletedUnrewardedQuest()
        {
            var state = GameManager.Instance?.CurrentGameState;
            if (state == null) return null;

            foreach (var q in state.QuestData.Quests)
                if (q.Type == QuestType.Kill && q.Status == QuestStatus.Completed)
                    return q;
            return null;
        }

        /// <summary>
        /// Marks a kill quest as rewarded and advances the next quest index.
        /// </summary>
        public static void MarkRewarded(QuestData quest)
        {
            quest.Status = QuestStatus.Rewarded;

            var state = GameManager.Instance?.CurrentGameState;
            if (state != null)
                state.QuestData.NextQuestIndex++;

            GameEvents.RaiseQuestJournalChanged();
        }

        /// <summary>
        /// Returns all quests that are Active or Completed (for journal display).
        /// </summary>
        public static List<QuestData> GetVisibleQuests()
        {
            var result = new List<QuestData>();
            var state = GameManager.Instance?.CurrentGameState;
            if (state == null) return result;

            foreach (var q in state.QuestData.Quests)
            {
                // Show active and completed kill quests (waiting for reward)
                // Show all floor quests (active + rewarded) so they stay in the journal
                if (q.Status == QuestStatus.Active || q.Status == QuestStatus.Completed)
                    result.Add(q);
                else if (q.Type == QuestType.Floor && q.Status == QuestStatus.Rewarded)
                    result.Add(q);
            }
            return result;
        }

        // ── Floor progression tracking ───────────────────────────────

        /// <summary>
        /// Call when the player enters a new floor. Checks all active Floor quests
        /// and completes any whose TargetFloor has been reached.
        /// For "Odnajdź Władcę Podziemi" (targetFloor=6), pass floor=6 after Delirius.
        /// </summary>
        public static void OnFloorReached(int floor)
        {
            var state = GameManager.Instance?.CurrentGameState;
            if (state == null) return;

            bool changed = false;
            foreach (var quest in state.QuestData.Quests)
            {
                if (quest.Type != QuestType.Floor) continue;
                if (quest.Status != QuestStatus.Active) continue;
                if (floor < quest.TargetFloor) continue;

                quest.Status = QuestStatus.Completed;
                // Floor quests are self-rewarding — mark as Rewarded immediately
                quest.Status = QuestStatus.Rewarded;
                GameEvents.RaiseQuestCompleted(quest);
                changed = true;
            }

            if (changed)
                GameEvents.RaiseQuestJournalChanged();
        }

        // ── Kill tracking ────────────────────────────────────────────

        private static void OnEnemyKilled(string enemyName)
        {
            var state = GameManager.Instance?.CurrentGameState;
            if (state == null) return;

            foreach (var quest in state.QuestData.Quests)
            {
                if (quest.Type != QuestType.Kill) continue;
                if (quest.Status != QuestStatus.Active) continue;
                if (quest.TargetEnemyName != enemyName) continue;

                quest.CurrentKills++;
                GameEvents.RaiseQuestProgress(quest);

                if (quest.IsComplete)
                {
                    quest.Status = QuestStatus.Completed;
                    GameEvents.RaiseQuestCompleted(quest);
                    GameEvents.RaiseNotification(
                        $"Misja \"{quest.Title}\" wykonana!\nUdaj się do Tajemniczego Jegomościa po nagrodę.");
                }

                GameEvents.RaiseQuestJournalChanged();
            }
        }
    }
}
