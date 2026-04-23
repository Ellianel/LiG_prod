namespace LochyIGorzala.Dungeon
{
    /// <summary>
    /// Defines the type of each tile in the dungeon grid.
    /// Used by both the logic layer (DungeonData) and the renderer (DungeonGenerator).
    /// </summary>
    public enum TileType
    {
        Empty = 0,
        Floor = 1,
        WallTop = 2,
        WallSide = 3,
        DoorClosed = 4,
        DoorOpen = 5,
        StairsDown = 6,
        StairsUp = 7,
        Trap = 8,
        Chest = 9,
        Water = 10,
        PuzzleSwitch = 11,
        PuzzleSwitchActive = 12
    }
}
