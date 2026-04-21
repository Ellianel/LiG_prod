using System;
using System.Collections.Generic;

namespace LochyIGorzala.Core
{
    /// <summary>
    /// Type of tracking for an achievement — determines which events trigger progress.
    /// </summary>
    public enum AchievementType
    {
        EnemyKill,      // Kill any enemy N times
        BossKill,       // Kill specific boss enemies
        FloorReach,     // Reach a specific dungeon floor
        GoldAccumulate, // Have N gold at once
        QuestComplete,  // Complete N quests
        BimberDrink,    // Drink bimber/nalewka N times (toxicity-raising consumables)
        TrapTrigger,    // Trigger N traps
        PuzzleSolve,    // Solve all puzzles on a floor
        DeliriusDefeat  // Defeat the final boss Delirius
    }

    /// <summary>
    /// Pure C# achievement definition — no Unity dependency.
    /// Each achievement has a unique Id, a type, a target count, and Polish display text.
    /// </summary>
    [Serializable]
    public class AchievementData
    {
        public string Id;
        public string Title;
        public string Description;
        public AchievementType Type;
        /// <summary>How many times the condition must be met (e.g. kill 20 enemies).</summary>
        public int TargetCount;

        public AchievementData(string id, string title, string description,
                               AchievementType type, int targetCount = 1)
        {
            Id = id;
            Title = title;
            Description = description;
            Type = type;
            TargetCount = targetCount;
        }
    }

    /// <summary>
    /// Serializable save data for a single achievement — tracks progress and unlock state.
    /// </summary>
    [Serializable]
    public class AchievementProgress
    {
        public string AchievementId;
        public int CurrentCount;
        public bool Unlocked;

        public AchievementProgress() { }

        public AchievementProgress(string id)
        {
            AchievementId = id;
            CurrentCount = 0;
            Unlocked = false;
        }
    }

    /// <summary>
    /// Serializable container for all achievement progress — stored in GameState.
    /// </summary>
    [Serializable]
    public class AchievementSaveData
    {
        public List<AchievementProgress> Progress = new List<AchievementProgress>();

        /// <summary>
        /// Gets or creates progress entry for the given achievement ID.
        /// </summary>
        public AchievementProgress GetOrCreate(string achievementId)
        {
            foreach (var p in Progress)
                if (p.AchievementId == achievementId)
                    return p;

            var newEntry = new AchievementProgress(achievementId);
            Progress.Add(newEntry);
            return newEntry;
        }

        /// <summary>Returns true if the given achievement has been unlocked.</summary>
        public bool IsUnlocked(string achievementId)
        {
            foreach (var p in Progress)
                if (p.AchievementId == achievementId)
                    return p.Unlocked;
            return false;
        }
    }

    /// <summary>
    /// Static database of all achievements in the game.
    /// 10 achievements covering combat, exploration, economy, quests, and special feats.
    /// </summary>
    public static class AchievementDatabase
    {
        public static readonly AchievementData[] All = new AchievementData[]
        {
            // ── Combat ──────────────────────────────────────────────
            new AchievementData(
                "first_blood", "Pierwsza krew",
                "Zabij pierwszego potwora.",
                AchievementType.EnemyKill, 1),

            new AchievementData(
                "monster_slayer", "Pogromca Potworów",
                "Zabij 20 potworów.",
                AchievementType.EnemyKill, 20),

            new AchievementData(
                "boss_hunter", "Łowca Bossów",
                "Pokonaj wszystkich 5 bossów.",
                AchievementType.BossKill, 5),

            // ── Exploration ─────────────────────────────────────────
            new AchievementData(
                "deep_diver", "Głębiny",
                "Dotarłeś do piątego piętra lochu.",
                AchievementType.FloorReach, 5),

            // ── Economy ─────────────────────────────────────────────
            new AchievementData(
                "rich_man", "Bogacz",
                "Zgromadź 500 złotych monet.",
                AchievementType.GoldAccumulate, 500),

            // ── Quests ──────────────────────────────────────────────
            new AchievementData(
                "quest_helper", "Pomocnik",
                "Wykonaj pierwszą misję od Tajemniczego Jegomościa.",
                AchievementType.QuestComplete, 1),

            // ── Bimber / Alcohol ────────────────────────────────────
            new AchievementData(
                "alcoholic", "Alkoholik",
                "Wypij bimber lub nalewkę 10 razy w walce.",
                AchievementType.BimberDrink, 10),

            // ── Traps ───────────────────────────────────────────────
            new AchievementData(
                "trap_veteran", "Weteran Pułapek",
                "Wdepnij w 5 pułapek.",
                AchievementType.TrapTrigger, 5),

            // ── Puzzles ─────────────────────────────────────────────
            new AchievementData(
                "rune_master", "Runiczny Mistrz",
                "Rozwiąż zagadkę runową na piętrze.",
                AchievementType.PuzzleSolve, 1),

            // ── Final Boss ──────────────────────────────────────────
            new AchievementData(
                "delirius_slayer", "Pogromca Deliriusa",
                "Pokonaj Deliriusa i zakończ grę.",
                AchievementType.DeliriusDefeat, 1),
        };

        /// <summary>Finds an achievement by Id, or null.</summary>
        public static AchievementData GetById(string id)
        {
            foreach (var a in All)
                if (a.Id == id) return a;
            return null;
        }
    }
}
