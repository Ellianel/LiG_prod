using System;

namespace LochyIGorzala.Combat
{
    /// <summary>
    /// Types of status effects enemies can inflict on the player.
    /// </summary>
    public enum StatusEffectType
    {
        Poison,  // HP damage over time (from Utopiec, Nekromanta)
        Burn,    // HP damage over time, stronger (from BossOgien)
        Slow     // Reduces AP by 1 next turn (from Strzyga)
    }

    /// <summary>
    /// Pure C# status effect applied to the player during combat.
    /// Ticks at the start of each enemy turn. Removed when duration expires.
    /// </summary>
    [Serializable]
    public class StatusEffect
    {
        public StatusEffectType Type;
        /// <summary>Turns remaining (decremented each tick, removed at 0).</summary>
        public int RemainingTurns;
        /// <summary>HP damage dealt each tick (0 for non-damaging effects like Slow).</summary>
        public int DamagePerTick;

        public StatusEffect(StatusEffectType type, int duration, int damagePerTick = 0)
        {
            Type = type;
            RemainingTurns = duration;
            DamagePerTick = damagePerTick;
        }

        /// <summary>
        /// Applies one tick of this effect to the player.
        /// Returns a Polish message describing what happened.
        /// </summary>
        public string Tick(Core.PlayerData player)
        {
            RemainingTurns--;

            switch (Type)
            {
                case StatusEffectType.Poison:
                    player.CurrentHP = Math.Max(0, player.CurrentHP - DamagePerTick);
                    return $"TRUCIZNA zadaje {DamagePerTick} obrażeń! ({RemainingTurns} tur pozostało)";

                case StatusEffectType.Burn:
                    player.CurrentHP = Math.Max(0, player.CurrentHP - DamagePerTick);
                    return $"OGIEŃ pali ciało! -{DamagePerTick} HP! ({RemainingTurns} tur pozostało)";

                case StatusEffectType.Slow:
                    // AP reduction handled by CombatEngine when setting up the turn
                    return $"SPOWOLNIENIE — mniej Wigoru w tej turze! ({RemainingTurns} tur pozostało)";

                default:
                    return "";
            }
        }

        public bool IsExpired => RemainingTurns <= 0;

        /// <summary>Polish display name for the effect.</summary>
        public string DisplayName
        {
            get
            {
                switch (Type)
                {
                    case StatusEffectType.Poison: return "Trucizna";
                    case StatusEffectType.Burn:   return "Podpalenie";
                    case StatusEffectType.Slow:   return "Spowolnienie";
                    default: return Type.ToString();
                }
            }
        }
    }
}
