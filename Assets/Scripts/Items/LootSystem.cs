using System;
using System.Collections.Generic;
using LochyIGorzala.Enemies;

namespace LochyIGorzala.Items
{
    /// <summary>
    /// Generates item drops after combat based on enemy type, floor level,
    /// and rarity weight tables.
    ///
    /// Requirement satisfied: "Implementacja systemu rzadkości lootu (Common, Rare, Epic)"
    ///
    /// Design:
    ///  • Each enemy has a base drop chance (boss = always drops, regular = ~50%).
    ///  • Rarity is rolled separately: floor acts as a modifier — deeper floors
    ///    increase Rare/Epic weight so progression feels rewarding.
    ///  • Items are drawn at random from the matching rarity pool in ItemDatabase.
    ///  • Gold reward is already in EnemyData; LootSystem only handles items.
    /// </summary>
    public static class LootSystem
    {
        private static readonly Random _rng = new Random();

        // ── Drop-chance per enemy category ────────────────────────
        private const int RegularDropChance = 50;   // % chance a regular enemy drops anything
        private const int BossDropChance    = 100;  // bosses always drop something

        // ── Rarity weights per floor (floor 0 = lobby, floors 1-5) ──
        // Format: [common, rare, epic] — higher = more likely
        private static readonly int[,] RarityWeights =
        {
            // Floor 0  (lobby — no drops, but defined for safety)
            { 100,  0,  0 },
            // Floor 1
            {  75, 23,  2 },
            // Floor 2
            {  65, 30,  5 },
            // Floor 3
            {  55, 35, 10 },
            // Floor 4
            {  45, 38, 17 },
            // Floor 5 (final)
            {  30, 40, 30 },
        };

        // ── Boss bonus: guarantees at least Rare on floors 3+ ────
        private static readonly int BossMinFloorForRareGuarantee = 3;

        // ─────────────────────────────────────────────────────────
        //  Public API
        // ─────────────────────────────────────────────────────────

        /// <summary>
        /// Rolls for an item drop after defeating an enemy.
        /// Returns null if no item drops this time.
        /// </summary>
        /// <param name="enemy">The defeated enemy.</param>
        /// <param name="floor">Current dungeon floor (0–5).</param>
        public static ItemData RollLoot(EnemyData enemy, int floor)
        {
            if (enemy == null) return null;

            // 1. Should we drop anything at all?
            int dropChance = enemy.IsBoss ? BossDropChance : RegularDropChance;
            if (_rng.Next(100) >= dropChance) return null;

            // 2. Pick rarity
            ItemRarity rarity = RollRarity(floor, enemy.IsBoss);

            // 3. Pick a random item from that rarity pool
            return PickFromPool(rarity);
        }

        /// <summary>
        /// Generates a guaranteed drop of specified rarity — used for
        /// special chests and quest rewards.
        /// </summary>
        public static ItemData GuaranteedDrop(ItemRarity rarity)
        {
            return PickFromPool(rarity);
        }

        /// <summary>
        /// Returns a descriptive rarity string with rich-text colour
        /// for display in the combat result message box.
        /// </summary>
        public static string FormatRarityTag(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Rare:  return "<color=#4A9EFF>[RZADKI]</color>";
                case ItemRarity.Epic:  return "<color=#FFD700>[EPICKI]</color>";
                default:               return "<color=#CCCCCC>[ZWYKŁY]</color>";
            }
        }

        // ─────────────────────────────────────────────────────────
        //  Internal helpers
        // ─────────────────────────────────────────────────────────

        private static ItemRarity RollRarity(int floor, bool isBoss)
        {
            // Clamp floor index to the weight table
            int f = Math.Clamp(floor, 0, RarityWeights.GetLength(0) - 1);

            int wCommon = RarityWeights[f, 0];
            int wRare   = RarityWeights[f, 1];
            int wEpic   = RarityWeights[f, 2];

            // Boss on floor 3+ guarantees at least Rare
            if (isBoss && floor >= BossMinFloorForRareGuarantee)
                wCommon = 0;

            int total = wCommon + wRare + wEpic;
            int roll  = _rng.Next(total);

            if (roll < wCommon)            return ItemRarity.Common;
            if (roll < wCommon + wRare)    return ItemRarity.Rare;
            return ItemRarity.Epic;
        }

        private static ItemData PickFromPool(ItemRarity rarity)
        {
            IReadOnlyList<ItemData> pool;
            switch (rarity)
            {
                case ItemRarity.Rare:  pool = ItemDatabase.RareItems;   break;
                case ItemRarity.Epic:  pool = ItemDatabase.EpicItems;   break;
                default:               pool = ItemDatabase.CommonItems;  break;
            }

            if (pool == null || pool.Count == 0)
            {
                // Fallback: common healing potion if pool is somehow empty
                return ItemDatabase.Get("healing_potion");
            }

            return pool[_rng.Next(pool.Count)];
        }
    }
}
