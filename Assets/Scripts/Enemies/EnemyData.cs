using System;
using LochyIGorzala.Combat;

namespace LochyIGorzala.Enemies
{
    /// <summary>
    /// Abstract base class for all enemies (Polymorphism requirement).
    /// Pure C# — no Unity dependencies. Defines common stats and behavior interface.
    /// </summary>
    [Serializable]
    public abstract class EnemyData
    {
        public string Name;
        public string Description;
        public int Level;

        public int CurrentHP;
        public int MaxHP;
        public int Attack;
        public int Defense;
        public int ExperienceReward;
        public int GoldReward;

        // Sprite sheet position (col, row in 32x32 grid of monsters.png)
        public int SpriteCol;
        public int SpriteRow;

        // Boss properties
        public bool IsBoss;
        /// <summary>
        /// Visual scale multiplier for the sprite renderer.
        /// Regular enemies = 1.0, mini-bosses = 1.5, final boss = 2.0.
        /// </summary>
        public float Scale = 1f;

        // Weakness system: which damage type this enemy is vulnerable to
        public DamageType Weakness;

        /// <summary>
        /// Returns the damage this enemy deals on a basic attack.
        /// Can be overridden for unique attack patterns.
        /// </summary>
        public virtual int CalculateAttackDamage()
        {
            int baseDamage = Attack;
            // Add slight randomness (+/- 20%)
            int variance = Math.Max(1, baseDamage / 5);
            return baseDamage + new Random().Next(-variance, variance + 1);
        }

        /// <summary>
        /// Calculates damage taken after defense. Override for special resistances.
        /// </summary>
        public virtual int TakeDamage(int rawDamage, DamageType type)
        {
            float multiplier = (type == Weakness) ? 1.5f : 1.0f;
            int effectiveDamage = Math.Max(1, (int)(rawDamage * multiplier) - Defense / 2);
            CurrentHP = Math.Max(0, CurrentHP - effectiveDamage);
            return effectiveDamage;
        }

        /// <summary>
        /// Called after enemy attacks. Returns a StatusEffect to apply, or null.
        /// Override in subclasses for enemy-specific effects.
        /// </summary>
        public virtual StatusEffect TryApplyStatusEffect()
        {
            return null; // Base: no status effect
        }

        public bool IsAlive => CurrentHP > 0;

        /// <summary>
        /// Returns the enemy's taunt/flavor text when combat begins.
        /// Override in subclasses for unique dialogue.
        /// </summary>
        public abstract string GetEncounterMessage();
    }

    public enum DamageType
    {
        Physical,
        Silver,
        Fire,
        Holy,
        Poison
    }
}
