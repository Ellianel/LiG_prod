using System;
using System.Collections.Generic;

namespace LochyIGorzala.Core
{
    /// <summary>
    /// Pure C# quest definitions — no Unity dependency.
    /// Supports kill-type quests (tracked by enemy name) and
    /// floor-type quests (tracked by reaching a dungeon floor).
    /// </summary>

    public enum QuestStatus
    {
        NotStarted,
        Active,
        Completed,
        Rewarded
    }

    public enum QuestType
    {
        Kill,   // Kill N enemies of a given name
        Floor   // Reach a specific dungeon floor (self-completing, no NPC return)
    }

    [Serializable]
    public class QuestData
    {
        public string Id;
        public string Title;
        public string Description;
        public QuestType Type;

        // Kill quest fields
        public string TargetEnemyName;  // e.g. "Chochlik", "Strzyga"
        public int RequiredKills;
        public int CurrentKills;

        // Floor quest fields
        public int TargetFloor;         // e.g. 2 = cleared floor 1 and entered floor 2

        public QuestStatus Status;

        // Reward (only for Kill quests — Floor quests are self-tracking)
        public string RewardItemId;     // ItemDatabase id to give on completion
        public int RewardItemCount;
        public string RewardMessage;    // Funny NPC thank-you text

        public QuestData() { }

        /// <summary>Kill quest constructor.</summary>
        public QuestData(string id, string title, string description,
            string targetEnemy, int requiredKills,
            string rewardItemId, int rewardItemCount, string rewardMessage)
        {
            Id = id;
            Title = title;
            Description = description;
            Type = QuestType.Kill;
            TargetEnemyName = targetEnemy;
            RequiredKills = requiredKills;
            CurrentKills = 0;
            TargetFloor = 0;
            Status = QuestStatus.NotStarted;
            RewardItemId = rewardItemId;
            RewardItemCount = rewardItemCount;
            RewardMessage = rewardMessage;
        }

        /// <summary>Floor quest constructor.</summary>
        public QuestData(string id, string title, string description, int targetFloor)
        {
            Id = id;
            Title = title;
            Description = description;
            Type = QuestType.Floor;
            TargetFloor = targetFloor;
            TargetEnemyName = "";
            RequiredKills = 0;
            CurrentKills = 0;
            Status = QuestStatus.NotStarted;
            RewardItemId = "";
            RewardItemCount = 0;
            RewardMessage = "";
        }

        public bool IsComplete
        {
            get
            {
                if (Type == QuestType.Kill) return CurrentKills >= RequiredKills;
                return Status == QuestStatus.Completed || Status == QuestStatus.Rewarded;
            }
        }

        public string ProgressText
        {
            get
            {
                if (Type == QuestType.Kill) return $"{CurrentKills}/{RequiredKills}";
                return Status == QuestStatus.Completed || Status == QuestStatus.Rewarded
                    ? "Ukończono" : "W toku...";
            }
        }
    }

    /// <summary>
    /// Serializable quest save data — stored in GameState.
    /// </summary>
    [Serializable]
    public class QuestSaveData
    {
        public List<QuestData> Quests = new List<QuestData>();
        public bool IntroCompleted;   // true after talking to Jegomość for the first time
        public int NextQuestIndex;    // which kill-quest to offer next

        public QuestSaveData()
        {
            Quests = new List<QuestData>();
            IntroCompleted = false;
            NextQuestIndex = 0;
        }
    }

    /// <summary>
    /// Static quest definitions — all available quests in the game.
    /// </summary>
    public static class QuestDatabase
    {
        /// <summary>Kill quests offered by Tajemniczy Jegomość one-by-one.</summary>
        public static readonly QuestData[] KillQuests = new QuestData[]
        {
            new QuestData(
                id: "kill_chochliki",
                title: "Polowanie na Chochliki",
                description: "Zabij 3 Chochliki w lochach",
                targetEnemy: "Chochlik",
                requiredKills: 3,
                rewardItemId: "healing_potion",
                rewardItemCount: 2,
                rewardMessage: "Nieźle, nieźle! Chochliki to paskudztwo, dobrze że się ich pozbywasz.\n" +
                    "Masz tu mikstury, przydadzą Ci się w głębszych lochach!"
            ),
            new QuestData(
                id: "kill_strzygi",
                title: "Strzygi muszą zginąć",
                description: "Zabij 3 Strzygi w lochach",
                targetEnemy: "Strzyga",
                requiredKills: 3,
                rewardItemId: "antidote",
                rewardItemCount: 2,
                rewardMessage: "Ho ho! Strzygi to nie lada wyzwanie, a Ty dałeś radę!\n" +
                    "Łap antidotum — po tych wszystkich bimbrach pewnie Ci się przyda, hehe!"
            ),
            new QuestData(
                id: "kill_utopce",
                title: "Mokra robota",
                description: "Zabij 3 Utopce w lochach",
                targetEnemy: "Utopiec",
                requiredKills: 3,
                rewardItemId: "zmijowa_nalewka",
                rewardItemCount: 3,
                rewardMessage: "Utopce to podstępne stwory, dobra robota!\n" +
                    "Na nagrodę mam dla Ciebie nalewki bojowe — walcz dzielnie!"
            ),
        };

        /// <summary>
        /// Floor progression quests — auto-accepted on intro, auto-completed on floor entry.
        /// </summary>
        public static readonly QuestData[] FloorQuests = new QuestData[]
        {
            new QuestData(
                id: "floor_1",
                title: "Przedostań się przez pierwszy poziom",
                description: "Pokonaj wszystkie potwory na pierwszym piętrze i zejdź niżej",
                targetFloor: 2
            ),
            new QuestData(
                id: "floor_2",
                title: "Przedostań się przez drugi poziom",
                description: "Pokonaj wszystkie potwory na drugim piętrze i zejdź niżej",
                targetFloor: 3
            ),
            new QuestData(
                id: "find_wladca",
                title: "Odnajdź Władcę Podziemi",
                description: "Przejdź przez wszystkie piętra i pokonaj Deliriusa",
                targetFloor: 6  // 6 = past floor 5, i.e. Delirius defeated
            ),
        };
    }
}
