using System;
using System.Collections.Generic;
using LochyIGorzala.Core;
using LochyIGorzala.Enemies;

namespace LochyIGorzala.Items
{
    // ─────────────────────────────────────────────────────────────
    //  INVENTORY DATA — serializable snapshot (saved to JSON)
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Pure serializable data class.  Stored inside PlayerData so the
    /// entire game state (including inventory) is written in one JSON file.
    ///
    /// Items are stored as ItemId strings; the runtime Inventory class
    /// resolves them through ItemDatabase for zero allocation on save/load.
    /// </summary>
    [Serializable]
    public class InventoryData
    {
        /// <summary>Max items the player can carry in the bag.</summary>
        public const int MaxCapacity = 20;

        /// <summary>ItemIds of all items currently in the bag (including stacked consumables).</summary>
        public List<string> ItemIds = new List<string>();

        // Equipment slots — empty string means nothing equipped
        public string EquippedWeaponId    = "";
        public string EquippedArmorId     = "";
        public string EquippedAccessoryId = "";

        public InventoryData() { }
    }

    // ─────────────────────────────────────────────────────────────
    //  INVENTORY — runtime wrapper (pure C#, no Unity)
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Runtime inventory manager.  Wraps InventoryData (the save-friendly DTO)
    /// and exposes all bag / equip operations.
    ///
    /// Design notes:
    ///  • Pure C# — no MonoBehaviour, no Unity types.
    ///  • Communicates state changes via GameEvents (Observer pattern).
    ///  • Equip / unequip methods delegate stat changes to ItemData.OnEquip / OnUnequip
    ///    which are IEquippable implementations (polymorphism).
    ///  • GetWeaponDamageType() feeds into CombatEngine so equipped weapon type
    ///    automatically triggers enemy weaknesses (System Słabości).
    /// </summary>
    public class Inventory
    {
        private readonly InventoryData _data;
        private readonly PlayerData    _player;

        // ── Public read access ────────────────────────────────────
        public IReadOnlyList<string> ItemIds          => _data.ItemIds;
        public string EquippedWeaponId                => _data.EquippedWeaponId;
        public string EquippedArmorId                 => _data.EquippedArmorId;
        public string EquippedAccessoryId             => _data.EquippedAccessoryId;
        public int    Count                           => _data.ItemIds.Count;
        public bool   IsFull                          => _data.ItemIds.Count >= InventoryData.MaxCapacity;

        public ItemData EquippedWeapon    => ItemDatabase.Get(_data.EquippedWeaponId);
        public ItemData EquippedArmor     => ItemDatabase.Get(_data.EquippedArmorId);
        public ItemData EquippedAccessory => ItemDatabase.Get(_data.EquippedAccessoryId);

        // ── Constructor ───────────────────────────────────────────

        public Inventory(InventoryData data, PlayerData player)
        {
            _data   = data   ?? throw new ArgumentNullException(nameof(data));
            _player = player ?? throw new ArgumentNullException(nameof(player));
        }

        // ─────────────────────────────────────────────────────────
        //  BAG OPERATIONS
        // ─────────────────────────────────────────────────────────

        /// <summary>
        /// Adds an item to the bag.  Returns false if bag is full.
        /// Fires GameEvents.RaiseInventoryChanged so the UI can refresh.
        /// </summary>
        public bool AddItem(string itemId)
        {
            if (IsFull)
            {
                GameEvents.RaiseNotification("Plecak jest pełny! (20/20)");
                return false;
            }

            ItemData item = ItemDatabase.Get(itemId);
            if (item == null) return false;

            _data.ItemIds.Add(itemId);
            GameEvents.RaiseInventoryChanged();
            GameEvents.RaiseNotification($"Podniesiono: {item.Name}");
            return true;
        }

        /// <summary>
        /// Removes the first occurrence of itemId from the bag.
        /// Returns false if the item was not present.
        /// </summary>
        public bool RemoveItem(string itemId)
        {
            bool removed = _data.ItemIds.Remove(itemId);
            if (removed) GameEvents.RaiseInventoryChanged();
            return removed;
        }

        /// <summary>Drops (removes) a bag item without consuming its effects.</summary>
        public bool DropItem(string itemId)
        {
            if (!RemoveItem(itemId)) return false;
            ItemData item = ItemDatabase.Get(itemId);
            if (item != null)
                GameEvents.RaiseNotification($"Wyrzucono: {item.Name}");
            return true;
        }

        public bool HasItem(string itemId) => _data.ItemIds.Contains(itemId);

        // ─────────────────────────────────────────────────────────
        //  EQUIP / UNEQUIP
        // ─────────────────────────────────────────────────────────

        /// <summary>
        /// Equips an item from the bag to the appropriate slot.
        /// Automatically unequips any previously equipped item in that slot
        /// (it stays in the bag rather than being discarded).
        /// </summary>
        public bool EquipItem(string itemId)
        {
            ItemData item = ItemDatabase.Get(itemId);
            if (item == null) return false;

            // Only equippable types
            if (item.Type == ItemType.Consumable)
            {
                GameEvents.RaiseNotification($"{item.Name} jest używalny, nie zakładany.");
                return false;
            }

            switch (item.Type)
            {
                case ItemType.Weapon:
                    if (!string.IsNullOrEmpty(_data.EquippedWeaponId))
                        UnequipSlot(ItemType.Weapon, putBackInBag: true);
                    _data.EquippedWeaponId = itemId;
                    break;

                case ItemType.Armor:
                    if (!string.IsNullOrEmpty(_data.EquippedArmorId))
                        UnequipSlot(ItemType.Armor, putBackInBag: true);
                    _data.EquippedArmorId = itemId;
                    break;

                case ItemType.Accessory:
                    if (!string.IsNullOrEmpty(_data.EquippedAccessoryId))
                        UnequipSlot(ItemType.Accessory, putBackInBag: true);
                    _data.EquippedAccessoryId = itemId;
                    break;
            }

            // Remove from bag (it now lives in the equip slot)
            _data.ItemIds.Remove(itemId);

            // Apply stat bonuses
            item.OnEquip(_player);

            GameEvents.RaiseInventoryChanged();
            GameEvents.RaiseNotification($"Założono: {item.Name}");
            return true;
        }

        /// <summary>
        /// Unequips the item in the specified slot.
        /// If putBackInBag is true and there is bag space, it moves to the bag.
        /// </summary>
        public void UnequipSlot(ItemType slot, bool putBackInBag = true)
        {
            string id;
            switch (slot)
            {
                case ItemType.Weapon:
                    id = _data.EquippedWeaponId;
                    _data.EquippedWeaponId = "";
                    break;
                case ItemType.Armor:
                    id = _data.EquippedArmorId;
                    _data.EquippedArmorId = "";
                    break;
                case ItemType.Accessory:
                    id = _data.EquippedAccessoryId;
                    _data.EquippedAccessoryId = "";
                    break;
                default:
                    return;
            }

            ItemData item = ItemDatabase.Get(id);
            if (item == null) return;

            // Remove stat bonuses
            item.OnUnequip(_player);

            // Try to put back in bag
            if (putBackInBag && !IsFull)
                _data.ItemIds.Add(id);

            GameEvents.RaiseInventoryChanged();
        }

        // ─────────────────────────────────────────────────────────
        //  CONSUMABLE USE
        // ─────────────────────────────────────────────────────────

        /// <summary>
        /// Uses a consumable item from the bag.
        /// The item is removed after use.
        /// Returns a CombatItemResult that CombatEngine / UI can act on.
        /// </summary>
        public CombatItemResult UseConsumable(string itemId)
        {
            ItemData item = ItemDatabase.Get(itemId);
            if (item == null)
                return new CombatItemResult("Nieznany przedmiot.", false);

            if (item.Type != ItemType.Consumable)
                return new CombatItemResult($"{item.Name} nie jest używalnym przedmiotem.", false);

            if (!_data.ItemIds.Contains(itemId))
                return new CombatItemResult($"Nie masz {item.Name} w plecaku.", false);

            // Execute IConsumable.Use()
            CombatItemResult result = item.Use(_player);

            if (result.Success)
                _data.ItemIds.Remove(itemId);   // consume one instance

            GameEvents.RaiseInventoryChanged();
            return result;
        }

        // ─────────────────────────────────────────────────────────
        //  COMBAT HELPERS
        // ─────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the DamageType of the currently equipped weapon.
        /// Physical is returned when no weapon is equipped.
        /// Used by CombatEngine to resolve enemy weakness bonuses.
        /// </summary>
        public DamageType GetWeaponDamageType()
        {
            ItemData weapon = EquippedWeapon;
            if (weapon == null || !weapon.HasDamageTypeOverride)
                return DamageType.Physical;
            return weapon.OverrideDamageType;
        }

        // ─────────────────────────────────────────────────────────
        //  QUERIES
        // ─────────────────────────────────────────────────────────

        /// <summary>
        /// Returns all unique consumable ItemIds in the bag
        /// (deduped so the UI can show "Mikstura x3" instead of three rows).
        /// </summary>
        public Dictionary<string, int> GetConsumableStacks()
        {
            var stacks = new Dictionary<string, int>();
            foreach (string id in _data.ItemIds)
            {
                ItemData item = ItemDatabase.Get(id);
                if (item == null || item.Type != ItemType.Consumable) continue;
                if (!stacks.ContainsKey(id)) stacks[id] = 0;
                stacks[id]++;
            }
            return stacks;
        }

        /// <summary>Returns all non-consumable (equipment) items in the bag.</summary>
        public List<ItemData> GetEquipmentInBag()
        {
            var list = new List<ItemData>();
            // Avoid duplicates for same-id items (shouldn't happen for equipment but be safe)
            var seen = new HashSet<string>();
            foreach (string id in _data.ItemIds)
            {
                if (seen.Contains(id)) continue;
                ItemData item = ItemDatabase.Get(id);
                if (item != null && item.Type != ItemType.Consumable)
                {
                    list.Add(item);
                    seen.Add(id);
                }
            }
            return list;
        }

        /// <summary>
        /// Returns a human-readable one-liner describing equipped gear —
        /// useful for quick-display in the HUD or save slot preview.
        /// </summary>
        public string GetGearSummary()
        {
            string w = EquippedWeapon    != null ? EquippedWeapon.Name    : "brak";
            string a = EquippedArmor     != null ? EquippedArmor.Name     : "brak";
            string x = EquippedAccessory != null ? EquippedAccessory.Name : "brak";
            return $"Broń: {w} | Zbroja: {a} | Akc.: {x}";
        }
    }
}
