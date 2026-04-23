namespace LochyIGorzala.Dungeon
{
    /// <summary>
    /// Pure C# puzzle definitions — no Unity dependency.
    /// Runic Switch puzzles appear on floors 3-4 after all enemies are defeated.
    /// The player must step on all glowing rune switches to unlock the stairs.
    /// </summary>
    public static class PuzzleData
    {
        /// <summary>Returns true if the given floor has a rune puzzle gate.</summary>
        public static bool FloorHasPuzzle(int floor) => floor >= 3 && floor <= 4;

        /// <summary>Number of switches to scatter on the given floor.</summary>
        public static int GetSwitchCount(int floor)
        {
            switch (floor)
            {
                case 3: return 2;
                case 4: return 3;
                default: return 0;
            }
        }

        // ── Messages (Polish) ────────────────────────────────────────

        public const string SwitchesAppearedMessage =
            "Runy pojawiły się na podłodze! Aktywuj je wszystkie, aby otworzyć przejście.";

        public static string SwitchActivatedMessage(int activated, int total) =>
            $"Runa aktywowana! ({activated}/{total})";

        public const string AllSwitchesSolvedMessage =
            "Wszystkie runy aktywne! Schody się pojawiły!";

        public const string SwitchAlreadyActiveMessage =
            "Ta runa jest już aktywna.";
    }
}
