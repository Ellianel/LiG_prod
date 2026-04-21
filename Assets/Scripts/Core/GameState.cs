using System;
using LochyIGorzala.Items;

namespace LochyIGorzala.Core
{
    /// <summary>
    /// Pure C# class representing the entire saveable game state.
    /// No Unity dependencies — can be serialized to JSON for save/load.
    /// </summary>
    [Serializable]
    public class GameState
    {
        public PlayerData Player;
        public DungeonData Dungeon;
        public int CurrentFloor;
        public int GoldCoins;
        public float PlayTimeSeconds;

        public string SaveDate = "";

        /// <summary>
        /// Tracks which floors have had all enemies defeated.
        /// Index 0 = Lobby (always clear), 1-5 = dungeon floors.
        /// StairsDown only appears once FloorsCleared[currentFloor] is true.
        /// </summary>
        public bool[] FloorsCleared = new bool[6];

        /// <summary>
        /// True once the player defeats Delirius and returns to the lobby.
        /// Triggers the victory notification on the next Dungeon scene load.
        /// </summary>
        public bool DeliriusDefeated;

        /// <summary>
        /// Quest system save data — active quests, progress, intro flag.
        /// </summary>
        public QuestSaveData QuestData = new QuestSaveData();

        /// <summary>
        /// Achievement system save data — progress counters and unlock flags.
        /// </summary>
        public AchievementSaveData Achievements = new AchievementSaveData();

        public GameState()
        {
            Player = new PlayerData();
            Dungeon = new DungeonData();
            CurrentFloor = 0;  // 0 = Lobby, 1-5 = dungeon floors
            GoldCoins = 0;
            PlayTimeSeconds = 0f;
            SaveDate = "";
            FloorsCleared = new bool[6];
            FloorsCleared[0] = true; // Lobby is always open
            QuestData = new QuestSaveData();
            Achievements = new AchievementSaveData();
        }
    }

    /// <summary>
    /// Serializable player statistics and inventory data.
    /// </summary>
    [Serializable]
    public class PlayerData
    {
        public string Name;
        public string CharacterClass;
        public string ClassDescription;
        public int Level;
        public int Experience;
        public int ExperienceToNextLevel;

        public int CurrentHP;
        public int MaxHP;
        public int Attack;
        public int Defense;
        public int ActionPoints;
        public int MaxActionPoints;

        public float Toxicity;
        public float MaxToxicity;

        // Sprite sheet position for the player sprite (varies by class)
        public int SpriteCol;
        public int SpriteRow;
        // True if the sprite naturally faces LEFT in the spritesheet (needs initial flip to face right)
        public bool FacingLeft;

        public float PositionX;
        public float PositionY;

        /// <summary>
        /// Serializable inventory — bag contents + equipped slots.
        /// Populated on new game; persisted in the save file.
        /// </summary>
        public InventoryData Inventory = new InventoryData();

        public PlayerData()
        {
            Name = "Gniewko";
            CharacterClass = "Wojownik";
            ClassDescription = "";
            Level = 1;
            Experience = 0;
            ExperienceToNextLevel = 100;

            MaxHP = 120;
            CurrentHP = 120;
            Attack = 14;
            Defense = 8;
            MaxActionPoints = 3;
            ActionPoints = 3;

            Toxicity = 0f;
            MaxToxicity = 100f;

            // Default sprite: male fighter (row 1, col 1 in rogues.png)
            SpriteCol = 1;
            SpriteRow = 1;
            FacingLeft = true; // rogues.png sprites naturally face LEFT, flip on spawn

            PositionX = 5f;
            PositionY = 5f;

            Inventory = new InventoryData();
        }
    }

    /// <summary>
    /// Serializable dungeon layout data for save/load.
    /// </summary>
    [Serializable]
    public class DungeonData
    {
        public int Width;
        public int Height;
        public int Seed;
        public int[] TileMap; // Flattened 2D array of TileType values

        // Entrance (StairsUp) position — where player spawns when arriving from below
        public int EntranceX = 5;
        public int EntranceY = 5;
        // Exit (StairsDown) position — where player spawns when arriving from above
        public int ExitX = 55;
        public int ExitY = 55;

        // ── Puzzle state (rune switches on floors 3-4) ──────────────
        /// <summary>Total number of puzzle switches placed on this floor (0 = no puzzle).</summary>
        public int PuzzleSwitchesTotal;
        /// <summary>How many switches the player has activated so far.</summary>
        public int PuzzleSwitchesActivated;

        public DungeonData()
        {
            Width = 60;
            Height = 60;
            Seed = 0;
            TileMap = new int[0];
            EntranceX = 5;
            EntranceY = 5;
            ExitX = 55;
            ExitY = 55;
        }

        public int GetTile(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) return 0;
            return TileMap[y * Width + x];
        }

        public void SetTile(int x, int y, int tileType)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) return;
            TileMap[y * Width + x] = tileType;
        }
    }
}
