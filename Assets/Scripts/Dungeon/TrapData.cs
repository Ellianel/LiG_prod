namespace LochyIGorzala.Dungeon
{
    /// <summary>
    /// Pure C# trap definitions — no Unity dependency.
    /// Each TrapType has fixed effects applied when the player steps on the tile.
    /// </summary>
    public enum TrapType
    {
        Spikes,      // Direct HP damage
        MagicDrain,  // Increases toxicity
        Teleport     // Warps player back to entrance
    }

    public static class TrapData
    {
        // ── Spikes ───────────────────────────────────────────────────────
        /// <summary>Flat HP damage dealt by spike traps.</summary>
        public const int SpikesDamage = 15;
        public const string SpikesMessage = "Kolce przebijają ci stopy! (-15 HP)";

        // ── Magic Drain ──────────────────────────────────────────────────
        /// <summary>Toxicity increase from magical trap fumes.</summary>
        public const float MagicDrainToxicity = 20f;
        public const string MagicDrainMessage = "Magiczna pułapka! Toksyczność rośnie! (+20 Tox)";

        // ── Teleport ─────────────────────────────────────────────────────
        public const string TeleportMessage = "Teleport! Wracasz do wejścia na piętro!";

        /// <summary>
        /// Picks a TrapType deterministically from tile coordinates so the same
        /// tile always produces the same trap across scene reloads.
        /// Distribution: ~50% Spikes, ~30% MagicDrain, ~20% Teleport.
        /// </summary>
        public static TrapType GetTrapType(int tileX, int tileY, int floor)
        {
            // Simple deterministic hash
            int hash = ((tileX * 73856093) ^ (tileY * 19349663) ^ (floor * 83492791)) & 0x7FFFFFFF;
            int roll = hash % 100;

            if (roll < 50) return TrapType.Spikes;
            if (roll < 80) return TrapType.MagicDrain;
            return TrapType.Teleport;
        }
    }
}
