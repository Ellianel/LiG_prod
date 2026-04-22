using System;
using LochyIGorzala.Core;
using LochyIGorzala.Enemies;

namespace LochyIGorzala.Items
{
    // ─────────────────────────────────────────────────────────────
    //  ENUMS
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Item rarity tier — drives drop weights and visual colour coding.
    /// Common (white) → Rare (blue) → Epic (gold).
    /// Requirement: system rzadkości lootu (Common, Rare, Epic) — Ocena 5.0
    /// </summary>
    public enum ItemRarity
    {
        Common,
        Rare,
        Epic
    }

    /// <summary>
    /// Broad category of an item — determines slot usage and available actions.
    /// </summary>
    public enum ItemType
    {
        Weapon,      // Equip in weapon slot — grants AttackBonus + optional DamageType override
        Armor,       // Equip in armor slot  — grants DefenseBonus
        Accessory,   // Equip in accessory slot — can grant both
        Consumable   // Single-use; used from bag during exploration or combat
    }

    // ─────────────────────────────────────────────────────────────
    //  INTERFACES  (Polimorfizm — wymaganie prowadzącego)
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Marker interface for items that can be equipped to a slot.
    /// Implementors apply / remove stat bonuses on equip / unequip.
    /// </summary>
    public interface IEquippable
    {
        void OnEquip(PlayerData player);
        void OnUnequip(PlayerData player);
    }

    /// <summary>
    /// Marker interface for items that can be consumed.
    /// Returns a result describing what happened (heal, toxicity, buff).
    /// </summary>
    public interface IConsumable
    {
        CombatItemResult Use(PlayerData player);
    }

    /// <summary>
    /// IInteractable — generic interface for anything the player can interact with.
    /// Satisfies the prowadzący's explicit example: IInteractable.
    /// Items, NPCs, traps, and puzzle elements all implement this.
    /// </summary>
    public interface IInteractable
    {
        string InteractLabel { get; }
        void Interact(PlayerData player);
    }

    // ─────────────────────────────────────────────────────────────
    //  ITEM DATA  (pure C#, serializable)
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Pure data record for a single item type.
    /// Kept as a concrete class (not abstract) so Unity's JsonUtility
    /// can serialize / deserialize InventoryData without type-name tricks.
    ///
    /// Runtime behaviour is determined by the ItemType field.
    /// Implements IEquippable and IConsumable directly — all logic lives here
    /// so the inventory can call .OnEquip() / .Use() without casting.
    /// </summary>
    [Serializable]
    public class ItemData : IEquippable, IConsumable, IInteractable
    {
        // ── Identity ──────────────────────────────────────────────
        public string ItemId;          // Unique key used throughout the save system
        public string Name;
        public string Description;
        public ItemType  Type;
        public ItemRarity Rarity;

        // ── Equipment bonuses (for Weapon / Armor / Accessory) ───
        public int AttackBonus;
        public int DefenseBonus;
        public int HpBonus;

        // ── Weapon damage type override ───────────────────────────
        /// <summary>
        /// If true, attacks with this weapon use <see cref="OverrideDamageType"/>
        /// instead of the default Physical.  Enables Silver/Holy/Fire weapons that
        /// trigger enemy weaknesses (System Słabości requirement).
        /// </summary>
        public bool   HasDamageTypeOverride;
        public DamageType OverrideDamageType;

        // ── Consumable effects ────────────────────────────────────
        public int   HealAmount;          // HP restored on use
        public float ToxicityChange;      // >0 = increase tox, <0 = reduce tox
        public int   AttackBuff;          // Temporary attack boost for one combat turn
        public bool  ClearsToxicity;      // True → resets toxicity to 0 (antidote)

        // ── Presentation ──────────────────────────────────────────
        /// <summary>Column in items.png (0-indexed, 32 px per cell).</summary>
        public int SpriteCol;
        /// <summary>Row in items.png (0-indexed, 32 px per cell).</summary>
        public int SpriteRow;

        // ── Economy ───────────────────────────────────────────────
        public int GoldValue;    // Buy / sell price at the merchant

        // ─────────────────────────────────────────────────────────
        //  IInteractable
        // ─────────────────────────────────────────────────────────

        public string InteractLabel =>
            Type == ItemType.Consumable ? $"Użyj {Name}" : $"Zdobądź {Name}";

        public void Interact(PlayerData player)
        {
            // Default: picking up the item is handled by LootSystem / InventoryUI.
            // This method is here to satisfy IInteractable for puzzle / trap items
            // that might be placed directly in the dungeon.
            GameEvents.RaiseNotification($"Podniesiono: {Name}");
        }

        // ─────────────────────────────────────────────────────────
        //  IEquippable
        // ─────────────────────────────────────────────────────────

        public void OnEquip(PlayerData player)
        {
            player.Attack    += AttackBonus;
            player.Defense   += DefenseBonus;
            player.MaxHP     += HpBonus;
            // Heal the bonus HP amount so equipping armor feels responsive
            player.CurrentHP  = Math.Min(player.CurrentHP + HpBonus, player.MaxHP);

            GameEvents.RaisePlayerHealthChanged(player.CurrentHP, player.MaxHP);
        }

        public void OnUnequip(PlayerData player)
        {
            player.Attack    = Math.Max(1,  player.Attack  - AttackBonus);
            player.Defense   = Math.Max(0,  player.Defense - DefenseBonus);
            player.MaxHP     = Math.Max(10, player.MaxHP   - HpBonus);
            player.CurrentHP = Math.Min(player.CurrentHP,   player.MaxHP);

            GameEvents.RaisePlayerHealthChanged(player.CurrentHP, player.MaxHP);
        }

        // ─────────────────────────────────────────────────────────
        //  IConsumable
        // ─────────────────────────────────────────────────────────

        public CombatItemResult Use(PlayerData player)
        {
            if (Type != ItemType.Consumable)
                return new CombatItemResult($"{Name} nie jest używalnym przedmiotem.", false);

            string message = $"Gniewko używa {Name}.";
            int    healGiven    = 0;
            int    toxicDamage  = 0;

            // 1. Healing
            if (HealAmount > 0)
            {
                int before       = player.CurrentHP;
                player.CurrentHP = Math.Min(player.MaxHP, player.CurrentHP + HealAmount);
                healGiven        = player.CurrentHP - before;
                message         += $" Leczenie +{healGiven} HP.";
                GameEvents.RaisePlayerHealthChanged(player.CurrentHP, player.MaxHP);
            }

            // 2. Toxicity change
            if (ClearsToxicity)
            {
                player.Toxicity  = 0f;
                message         += " Toksyczność wyczyszczona!";
                GameEvents.RaiseToxicityChanged(player.Toxicity);
            }
            else if (Math.Abs(ToxicityChange) > 0.001f)
            {
                player.Toxicity = Math.Clamp(player.Toxicity + ToxicityChange, 0f, player.MaxToxicity);
                if (ToxicityChange > 0)
                    message += $" Toksyczność +{ToxicityChange:0}.";
                else
                    message += $" Toksyczność -{Math.Abs(ToxicityChange):0}.";

                GameEvents.RaiseToxicityChanged(player.Toxicity);

                // Poison threshold damage
                if (player.Toxicity >= player.MaxToxicity)
                {
                    toxicDamage      = 15;
                    player.CurrentHP = Math.Max(1, player.CurrentHP - toxicDamage);
                    message         += $" TRUCIZNA! -{toxicDamage} HP!";
                    GameEvents.RaisePlayerHealthChanged(player.CurrentHP, player.MaxHP);
                }
            }

            // 3. Attack buff (temporary — CombatEngine must apply for one turn)
            if (AttackBuff > 0)
                message += $" Atak +{AttackBuff} na następną turę!";

            return new CombatItemResult(message, true)
            {
                HealAmount     = healGiven,
                ToxicityChange = ToxicityChange,
                AttackBuff     = AttackBuff,
                ToxicDamage    = toxicDamage
            };
        }

        // ─────────────────────────────────────────────────────────
        //  Helpers
        // ─────────────────────────────────────────────────────────

        /// <summary>Returns a Unity rich-text colour tag for the rarity.</summary>
        public string RarityColour()
        {
            switch (Rarity)
            {
                case ItemRarity.Rare:  return "#4A9EFF";
                case ItemRarity.Epic:  return "#FFD700";
                default:               return "#CCCCCC";
            }
        }

        /// <summary>Formatted display name including rarity colour.</summary>
        public string DisplayName() =>
            $"<color={RarityColour()}>{Name}</color>";

        public override string ToString() => $"[{Rarity}] {Name} ({Type})";
    }

    // ─────────────────────────────────────────────────────────────
    //  COMBAT ITEM RESULT
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Returned by IConsumable.Use() so CombatEngine / UI can react to item effects.
    /// </summary>
    public class CombatItemResult
    {
        public string Message;
        public bool   Success;
        public int    HealAmount;
        public float  ToxicityChange;
        public int    AttackBuff;     // To be applied by CombatEngine for the next enemy turn
        public int    ToxicDamage;    // HP lost due to toxicity overflow

        public CombatItemResult(string message, bool success = true)
        {
            Message = message;
            Success = success;
        }
    }
}
