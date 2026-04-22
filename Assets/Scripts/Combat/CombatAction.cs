using System;
using LochyIGorzala.Core;
using LochyIGorzala.Enemies;

namespace LochyIGorzala.Combat
{
    /// <summary>
    /// Strategy pattern — each combat action is an interchangeable strategy.
    /// Allows easy addition of new actions without modifying the combat engine.
    /// </summary>
    public interface ICombatAction
    {
        string ActionName { get; }
        int ActionPointCost { get; }
        CombatActionResult Execute(PlayerData player, EnemyData enemy);
    }

    /// <summary>
    /// Result of executing a combat action. Pure data, no Unity dependencies.
    /// </summary>
    public class CombatActionResult
    {
        public string Message;
        public int  DamageDealt;
        public int  DamageReceived;
        public int  HealAmount;
        public bool FleeSuccessful;
        public bool ActionSuccessful;
        /// <summary>
        /// Temporary attack buff to apply for the next enemy-turn calculation.
        /// Set by DrinkBimberAction and consumable-item actions.
        /// CombatEngine reads this, applies it, then strips it after the enemy turn.
        /// </summary>
        public int  AttackBuff;

        public CombatActionResult(string message)
        {
            Message = message;
            ActionSuccessful = true;
        }
    }

    // =====================================================
    // CONCRETE COMBAT ACTIONS (Strategy pattern implementations)
    // =====================================================

    /// <summary>
    /// Light attack — costs 1 AP, basic damage.
    /// </summary>
    public class LightAttackAction : ICombatAction
    {
        public string ActionName => "Lekki Atak";
        public int ActionPointCost => 1;

        public CombatActionResult Execute(PlayerData player, EnemyData enemy)
        {
            int rawDamage = player.Attack;
            int variance = Math.Max(1, rawDamage / 5);
            rawDamage += new Random().Next(-variance, variance + 1);

            int actualDamage = enemy.TakeDamage(rawDamage, DamageType.Physical);

            return new CombatActionResult($"Gniewko zadaje {actualDamage} obrażeń lekkim atakiem!")
            {
                DamageDealt = actualDamage
            };
        }
    }

    /// <summary>
    /// Heavy attack — costs 2 AP, high damage.
    /// </summary>
    public class HeavyAttackAction : ICombatAction
    {
        public string ActionName => "Ciężki Atak";
        public int ActionPointCost => 2;

        public CombatActionResult Execute(PlayerData player, EnemyData enemy)
        {
            int rawDamage = (int)(player.Attack * 1.8f);
            int variance = Math.Max(1, rawDamage / 4);
            rawDamage += new Random().Next(-variance, variance + 1);

            int actualDamage = enemy.TakeDamage(rawDamage, DamageType.Physical);

            return new CombatActionResult($"Gniewko wymierza potężny cios! {actualDamage} obrażeń!")
            {
                DamageDealt = actualDamage
            };
        }
    }

    /// <summary>
    /// Heal — costs 1 AP, restores HP.
    /// </summary>
    public class HealAction : ICombatAction
    {
        public string ActionName => "Ulecz się";
        public int ActionPointCost => 1;

        public CombatActionResult Execute(PlayerData player, EnemyData enemy)
        {
            int healAmount = 15 + player.Level * 3;
            int oldHP = player.CurrentHP;
            player.CurrentHP = Math.Min(player.MaxHP, player.CurrentHP + healAmount);
            int actualHeal = player.CurrentHP - oldHP;

            return new CombatActionResult($"Gniewko leczy rany. Odzyskuje {actualHeal} HP.")
            {
                HealAmount = actualHeal
            };
        }
    }

    /// <summary>
    /// Drink bimber — costs 1 AP, boosts attack but increases toxicity.
    /// Core mechanic of the game: risk vs reward.
    /// </summary>
    public class DrinkBimberAction : ICombatAction
    {
        public string ActionName => "Wypij Bimber";
        public int ActionPointCost => 1;

        public CombatActionResult Execute(PlayerData player, EnemyData enemy)
        {
            const int   attackBoost      = 4;
            const float toxicityIncrease = 15f;

            // Toxicity is applied immediately (permanent within session)
            player.Toxicity = Math.Min(player.MaxToxicity, player.Toxicity + toxicityIncrease);
            GameEvents.RaiseToxicityChanged(player.Toxicity);

            string message = $"Gniewko pociąga łyk bimbru! Atak +{attackBoost}! Toksyczność +{toxicityIncrease}.";

            int toxDamage = 0;
            if (player.Toxicity >= player.MaxToxicity)
            {
                toxDamage        = 10;
                player.CurrentHP = Math.Max(1, player.CurrentHP - toxDamage);
                message         += $" TRUCIZNA! -{toxDamage} HP!";
                GameEvents.RaisePlayerHealthChanged(player.CurrentHP, player.MaxHP);
            }

            // Attack boost returned as AttackBuff so CombatEngine can strip it
            // after the enemy turn (prevents permanent bimber stacking exploit)
            return new CombatActionResult(message)
            {
                AttackBuff     = attackBoost,
                DamageReceived = toxDamage
            };
        }
    }

    /// <summary>
    /// Flee — costs 0 AP, chance-based escape.
    /// </summary>
    public class FleeAction : ICombatAction
    {
        public string ActionName => "Uciekaj";
        public int ActionPointCost => 0;

        public CombatActionResult Execute(PlayerData player, EnemyData enemy)
        {
            // 60% base flee chance, modified by level difference
            int fleeChance = 60 + (player.Level - enemy.Level) * 10;
            fleeChance = Math.Clamp(fleeChance, 20, 90);

            bool success = new Random().Next(100) < fleeChance;

            if (success)
            {
                return new CombatActionResult("Gniewko ucieka z pola bitwy!")
                {
                    FleeSuccessful = true
                };
            }

            return new CombatActionResult("Nie udało się uciec! Wróg blokuje drogę!")
            {
                FleeSuccessful = false,
                ActionSuccessful = false
            };
        }
    }

    /// <summary>
    /// Defend — costs 1 AP, reduces incoming damage next turn.
    /// </summary>
    public class DefendAction : ICombatAction
    {
        public string ActionName => "Obrona";
        public int ActionPointCost => 1;

        public CombatActionResult Execute(PlayerData player, EnemyData enemy)
        {
            // Temporarily boost defense (handled by CombatEngine)
            player.Defense += 5;

            return new CombatActionResult("Gniewko przyjmuje postawę obronną. Obrona +5 na tę turę.")
            {
                ActionSuccessful = true
            };
        }
    }

    // =====================================================
    // CLASS-SPECIFIC SPECIAL ACTIONS
    // =====================================================

    /// <summary>
    /// Wojownik special — Potężne Uderzenie.
    /// Costs 2 AP. Deals 2× base attack damage (physical).
    /// Simple but devastating — fits the Warrior archetype.
    /// </summary>
    public class WojownikSpecialAction : ICombatAction
    {
        public string ActionName => "Potężne Uderzenie";
        public int ActionPointCost => 2;

        public CombatActionResult Execute(PlayerData player, EnemyData enemy)
        {
            int rawDamage = (int)(player.Attack * 2.0f);
            int variance = Math.Max(1, rawDamage / 5);
            rawDamage += new Random().Next(-variance, variance + 1);

            int actualDamage = enemy.TakeDamage(rawDamage, DamageType.Physical);

            return new CombatActionResult(
                $"Gniewko zamachuje się z całej siły! POTĘŻNE UDERZENIE — {actualDamage} obrażeń!")
            {
                DamageDealt = actualDamage
            };
        }
    }

    /// <summary>
    /// Łucznik special — Strzał w Słabość.
    /// Costs 1 AP. Deals base attack damage but completely bypasses enemy Defense.
    /// Also has a 30% chance to deal double damage if hitting a Silver weakness.
    /// </summary>
    public class LucznikSpecialAction : ICombatAction
    {
        public string ActionName => "Strzał w Słabość";
        public int ActionPointCost => 1;

        public CombatActionResult Execute(PlayerData player, EnemyData enemy)
        {
            int rawDamage = player.Attack;
            int variance = Math.Max(1, rawDamage / 4);
            rawDamage += new Random().Next(-variance, variance + 1);

            // ARMOR PIERCE: TakeDamage subtracts Defense/2 internally.
            // Add that value back to rawDamage so net effect = full damage dealt.
            int armorPierce = enemy.Defense / 2;
            int actualDamage = enemy.TakeDamage(rawDamage + armorPierce, DamageType.Silver);

            string msg = $"Gniewko celuje w szczelinę w zbroi — STRZAŁ W SŁABOŚĆ! {actualDamage} obrażeń (ignoruje pancerz)!";
            if (actualDamage > rawDamage + armorPierce)
                msg += " TRAFIONY W SŁABOŚĆ!";

            return new CombatActionResult(msg) { DamageDealt = actualDamage };
        }
    }

    /// <summary>
    /// Mag special — Kula Ognia.
    /// Costs 2 AP. Deals fire-type magic damage with high base power.
    /// Hits Fire weakness for bonus damage (Utopiec is weak to Fire).
    /// Bypasses physical defense — uses magic penetration.
    /// </summary>
    public class MagSpecialAction : ICombatAction
    {
        public string ActionName => "Kula Ognia";
        public int ActionPointCost => 2;

        public CombatActionResult Execute(PlayerData player, EnemyData enemy)
        {
            // Magic damage: base 18 + player.Attack × 1.5, ignores physical defense
            int rawDamage = 18 + (int)(player.Attack * 1.5f);
            int variance = Math.Max(2, rawDamage / 5);
            rawDamage += new Random().Next(-variance, variance + 1);

            // Fire damage — hits Utopiec weakness for double
            int actualDamage = enemy.TakeDamage(rawDamage, DamageType.Fire);

            string msg = $"Gniewko rzuca KULĘ OGNIA! {actualDamage} obrażeń magicznych!";
            if (actualDamage > rawDamage)
                msg += " WRÓG SPŁONIE W PŁOMIENIACH!";

            return new CombatActionResult(msg) { DamageDealt = actualDamage };
        }
    }
}
