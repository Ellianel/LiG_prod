// Minimal stub so CombatEngine.BuildVictoryResult() compiles outside Unity.
// CombatEngine references Managers.GameManager.Instance?.CurrentGameState?.CurrentFloor ?? 1
// In tests the Instance is always null, so floor defaults to 1.

namespace LochyIGorzala.Managers
{
    public class GameManager
    {
        public static GameManager Instance;
        public LochyIGorzala.Core.GameState CurrentGameState;
        public LochyIGorzala.Items.Inventory PlayerInventory;
    }
}
