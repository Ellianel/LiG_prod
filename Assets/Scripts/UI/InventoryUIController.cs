using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using LochyIGorzala.Core;
using LochyIGorzala.Items;
using LochyIGorzala.Managers;
using LochyIGorzala.Helpers;

namespace LochyIGorzala.UI
{
    /// <summary>
    /// Controls the Inventory overlay panel (works in Dungeon and Combat scenes).
    ///
    /// Self-contained: creates all 20 bag slots at runtime if bagGridParent has no children.
    /// No prefab reference needed — just wire the SerializedField references in the Inspector
    /// (done automatically by ProjectSetupEditor).
    ///
    /// Layout built by ProjectSetupEditor:
    ///   InventoryPanel (root, toggleable)
    ///   ├── Header (title + CloseButton)
    ///   ├── LeftPanel
    ///   │   ├── EquipSection (3 slot Images + labels)
    ///   │   └── BagGrid  (GridLayoutGroup — slots created at runtime)
    ///   ├── RightPanel / DetailPanel
    ///   │   ├── DetailName  (TMP)
    ///   │   ├── DetailDesc  (TMP, wrapping)
    ///   │   ├── DetailStats (TMP)
    ///   │   ├── EquipButton
    ///   │   ├── UseButton
    ///   │   └── DropButton
    ///   └── Footer (GoldText)
    /// </summary>
    public class InventoryUIController : MonoBehaviour
    {
        // ── Inspector references ──────────────────────────────────

        [Header("Panel root")]
        [SerializeField] private GameObject inventoryPanel;

        [Header("Equipment slots")]
        [SerializeField] private Image  weaponSlotIcon;
        [SerializeField] private Image  armorSlotIcon;
        [SerializeField] private Image  accessorySlotIcon;
        [SerializeField] private TMPro.TextMeshProUGUI weaponSlotLabel;
        [SerializeField] private TMPro.TextMeshProUGUI armorSlotLabel;
        [SerializeField] private TMPro.TextMeshProUGUI accessorySlotLabel;

        [Header("Bag grid (slots created at runtime)")]
        [SerializeField] private Transform bagGridParent;   // must have GridLayoutGroup

        [Header("Detail panel")]
        [SerializeField] private GameObject detailPanel;
        [SerializeField] private TMPro.TextMeshProUGUI detailName;
        [SerializeField] private TMPro.TextMeshProUGUI detailDesc;
        [SerializeField] private TMPro.TextMeshProUGUI detailStats;
        [SerializeField] private Button equipButton;
        [SerializeField] private Button useButton;
        [SerializeField] private Button dropButton;

        [Header("Footer")]
        [SerializeField] private TMPro.TextMeshProUGUI goldText;
        [SerializeField] private Button closeButton;

        [Header("Sprite sheet (items.png)")]
        [SerializeField] private Texture2D itemsSheet;

        // ── Runtime state ─────────────────────────────────────────

        private Inventory    _inventory;
        private string       _selectedItemId;
        private bool         _isOpenInCombat;

        /// <summary>Set by CombatUIController before opening in combat.</summary>
        public Combat.CombatEngine ActiveCombatEngine { get; set; }

        // Slot pool — built once in Awake, reused afterwards
        private readonly List<GameObject> _slotPool = new List<GameObject>();
        private bool _slotPoolReady; // guard: EnsureSlotPool runs only once per instance

        // ─────────────────────────────────────────────────────────
        //  Unity lifecycle
        // ─────────────────────────────────────────────────────────

        private void Awake()
        {
            // Hide on start
            if (inventoryPanel != null) inventoryPanel.SetActive(false);
            if (detailPanel    != null) detailPanel.SetActive(false);

            // Wire buttons
            if (closeButton  != null) closeButton.onClick.AddListener(Close);
            if (equipButton  != null) equipButton.onClick.AddListener(OnEquipClicked);
            if (useButton    != null) useButton.onClick.AddListener(OnUseClicked);
            if (dropButton   != null) dropButton.onClick.AddListener(OnDropClicked);

            // Build slot pool from existing children + create more if needed
            EnsureSlotPool();
        }

        private void OnEnable()
        {
            GameEvents.OnInventoryChanged += Refresh;
            GameEvents.OnGoldChanged      += OnGoldChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnInventoryChanged -= Refresh;
            GameEvents.OnGoldChanged      -= OnGoldChanged;
        }

        private void OnGoldChanged(int _) => RefreshGold();

        // ─────────────────────────────────────────────────────────
        //  Slot pool (self-building, no prefab required)
        // ─────────────────────────────────────────────────────────

        private void EnsureSlotPool()
        {
            if (bagGridParent == null) return;
            // Guard: only build the pool once per MonoBehaviour instance.
            // Calling this from Open() would duplicate slot references each time,
            // causing RefreshBagGrid() to clear items it just set (duplicate GO refs).
            if (_slotPoolReady) return;
            _slotPoolReady = true;

            // Adopt pre-existing children (placed by ProjectSetupEditor)
            int existingCount = bagGridParent.childCount;
            for (int i = 0; i < existingCount; i++)
                _slotPool.Add(bagGridParent.GetChild(i).gameObject);

            // Create any remaining slots up to bag capacity
            while (_slotPool.Count < InventoryData.MaxCapacity)
                _slotPool.Add(CreateSlotGO());
        }

        private GameObject CreateSlotGO()
        {
            // Root: Image + Button
            var go = new GameObject("ItemSlot");
            go.transform.SetParent(bagGridParent, false);

            var img = go.AddComponent<Image>();
            img.color = new Color(0.15f, 0.12f, 0.08f, 0.85f);

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            ColorBlock cb = btn.colors;
            cb.highlightedColor = new Color(0.9f, 0.75f, 0.35f, 1f);
            cb.selectedColor    = new Color(0.7f, 0.55f, 0.15f, 1f);
            btn.colors = cb;

            // Stack-count label (bottom-right corner)
            var labelGO = new GameObject("StackLabel");
            labelGO.transform.SetParent(go.transform, false);
            var lrt = labelGO.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;
            var tmp = labelGO.AddComponent<TMPro.TextMeshProUGUI>();
            tmp.text      = "";
            tmp.fontSize  = 17;
            tmp.alignment = TMPro.TextAlignmentOptions.BottomRight;
            tmp.color     = new Color(0.95f, 0.9f, 0.5f);

            return go;
        }

        // ─────────────────────────────────────────────────────────
        //  Open / Close / Toggle
        // ─────────────────────────────────────────────────────────

        public void Open(bool inCombat = false)
        {
            _isOpenInCombat = inCombat;
            _inventory = GameManager.Instance?.PlayerInventory;
            if (_inventory == null)
            {
                GameEvents.RaiseNotification("Inventory niedostępne.");
                return;
            }

            if (inventoryPanel != null) inventoryPanel.SetActive(true);
            _selectedItemId = null;
            if (detailPanel != null) detailPanel.SetActive(false);

            // Make sure pool covers max capacity
            EnsureSlotPool();
            Refresh();
        }

        public void Close()
        {
            if (inventoryPanel != null) inventoryPanel.SetActive(false);
            _selectedItemId = null;
        }

        public bool IsOpen => inventoryPanel != null && inventoryPanel.activeSelf;

        public void Toggle(bool inCombat = false)
        {
            if (IsOpen) Close();
            else        Open(inCombat);
        }

        // ─────────────────────────────────────────────────────────
        //  Refresh
        // ─────────────────────────────────────────────────────────

        private void Refresh()
        {
            if (!IsOpen || _inventory == null) return;
            RefreshEquipSlots();
            RefreshBagGrid();
            RefreshGold();
        }

        private void RefreshGold()
        {
            if (goldText == null) return;
            int gold = GameManager.Instance?.CurrentGameState?.GoldCoins ?? 0;
            goldText.text = $"Złoto: {gold}";
        }

        // ── Equipment slots ───────────────────────────────────────

        private void RefreshEquipSlots()
        {
            ApplyToSlot(weaponSlotIcon,    weaponSlotLabel,    _inventory.EquippedWeapon,    "Broń",        ItemType.Weapon);
            ApplyToSlot(armorSlotIcon,     armorSlotLabel,     _inventory.EquippedArmor,     "Zbroja",      ItemType.Armor);
            ApplyToSlot(accessorySlotIcon, accessorySlotLabel, _inventory.EquippedAccessory, "Akcesorium",  ItemType.Accessory);
        }

        private void ApplyToSlot(Image icon, TMPro.TextMeshProUGUI label, ItemData item, string emptyLabel, ItemType slotType)
        {
            if (icon == null) return;

            if (item != null)
            {
                icon.sprite  = GetItemSprite(item);
                icon.color   = RarityColor(item.Rarity);
                if (label != null) label.text = item.Name;

                // Make clickable → show detail (unequip option)
                Button btn = icon.GetComponent<Button>() ?? icon.gameObject.AddComponent<Button>();
                btn.onClick.RemoveAllListeners();
                string capturedId = item.ItemId;
                btn.onClick.AddListener(() => SelectEquippedItem(capturedId));
            }
            else
            {
                icon.sprite  = null;
                icon.color   = new Color(0.25f, 0.2f, 0.15f, 0.6f);
                if (label != null) label.text = emptyLabel;

                Button btn = icon.GetComponent<Button>() ?? icon.gameObject.AddComponent<Button>();
                btn.onClick.RemoveAllListeners();
            }
        }

        // ── Bag grid ──────────────────────────────────────────────

        private void RefreshBagGrid()
        {
            if (bagGridParent == null) return;

            var ids = new List<string>(_inventory.ItemIds);

            for (int i = 0; i < _slotPool.Count; i++)
            {
                if (i < ids.Count)
                {
                    _slotPool[i].SetActive(true);
                    PopulateSlot(_slotPool[i], ids[i]);
                }
                else
                {
                    ClearSlot(_slotPool[i]);
                }
            }
        }

        private void PopulateSlot(GameObject slot, string itemId)
        {
            ItemData item = ItemDatabase.Get(itemId);

            var icon = slot.GetComponent<Image>();
            if (icon != null)
            {
                icon.sprite = item != null ? GetItemSprite(item) : null;
                icon.color  = item != null ? RarityColor(item.Rarity) : new Color(0.15f, 0.12f, 0.08f, 0.85f);
            }

            var label = slot.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (label != null && item != null)
            {
                int count = 0;
                foreach (var id in _inventory.ItemIds)
                    if (id == itemId) count++;
                label.text = count > 1 ? count.ToString() : "";
            }

            var btn = slot.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                if (item != null)
                {
                    string captured = itemId;
                    btn.onClick.AddListener(() => SelectBagItem(captured));
                }
            }
        }

        private void ClearSlot(GameObject slot)
        {
            var icon = slot.GetComponent<Image>();
            if (icon != null)
            {
                icon.sprite = null;
                icon.color  = new Color(0.15f, 0.12f, 0.08f, 0.85f);
            }
            var label = slot.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (label != null) label.text = "";
            var btn = slot.GetComponent<Button>();
            if (btn != null) btn.onClick.RemoveAllListeners();
        }

        // ─────────────────────────────────────────────────────────
        //  Selection → Detail panel
        // ─────────────────────────────────────────────────────────

        private void SelectBagItem(string itemId)
        {
            _selectedItemId = itemId;
            ShowDetail(ItemDatabase.Get(itemId), isEquipped: false);
        }

        private void SelectEquippedItem(string itemId)
        {
            _selectedItemId = itemId;
            ShowDetail(ItemDatabase.Get(itemId), isEquipped: true);
        }

        private void ShowDetail(ItemData item, bool isEquipped)
        {
            if (detailPanel == null || item == null) return;
            detailPanel.SetActive(true);

            if (detailName != null)
                detailName.text = $"<color={item.RarityColour()}>{item.Name}</color>" +
                                  $"  <size=70%>[{item.Rarity}]</size>";

            if (detailDesc  != null) detailDesc.text  = item.Description;
            if (detailStats != null) detailStats.text = BuildStatsText(item);

            bool isConsumable = item.Type == ItemType.Consumable;

            if (equipButton != null)
            {
                equipButton.gameObject.SetActive(!isConsumable && !isEquipped);
                var lbl = equipButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (lbl != null) lbl.text = "Załóż";
            }

            if (useButton != null)
            {
                bool canUse = isConsumable && (!_isOpenInCombat || ActiveCombatEngine != null);
                useButton.gameObject.SetActive(canUse);
            }

            if (dropButton != null)
            {
                dropButton.gameObject.SetActive(true);
                var lbl = dropButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (lbl != null) lbl.text = isEquipped ? "Zdejmij" : "Wyrzuć";
            }
        }

        private string BuildStatsText(ItemData item)
        {
            var sb = new System.Text.StringBuilder();
            if (item.AttackBonus   > 0)  sb.AppendLine($"Atak:     +{item.AttackBonus}");
            if (item.AttackBonus   < 0)  sb.AppendLine($"Atak:     {item.AttackBonus}");
            if (item.DefenseBonus  > 0)  sb.AppendLine($"Obrona:   +{item.DefenseBonus}");
            if (item.DefenseBonus  < 0)  sb.AppendLine($"Obrona:   {item.DefenseBonus}");
            if (item.HpBonus       > 0)  sb.AppendLine($"Max HP:   +{item.HpBonus}");
            if (item.HealAmount    > 0)  sb.AppendLine($"Leczenie: +{item.HealAmount} HP");
            if (item.AttackBuff    > 0)  sb.AppendLine($"Buff Atk: +{item.AttackBuff} (1 tura)");
            if (item.ToxicityChange > 0) sb.AppendLine($"Toksyczność: +{item.ToxicityChange:0}");
            if (item.ToxicityChange < 0) sb.AppendLine($"Toksyczność: {item.ToxicityChange:0}");
            if (item.ClearsToxicity)     sb.AppendLine("Czyści toksyczność!");
            if (item.HasDamageTypeOverride) sb.AppendLine($"Dmg typ: {item.OverrideDamageType}");
            sb.AppendLine($"\nWartość: {item.GoldValue} złota");
            return sb.ToString().TrimEnd();
        }

        // ─────────────────────────────────────────────────────────
        //  Button handlers
        // ─────────────────────────────────────────────────────────

        private void OnEquipClicked()
        {
            if (_inventory == null || _selectedItemId == null) return;
            _inventory.EquipItem(_selectedItemId);
            _selectedItemId = null;
            if (detailPanel != null) detailPanel.SetActive(false);
        }

        private void OnUseClicked()
        {
            if (_inventory == null || _selectedItemId == null) return;

            if (_isOpenInCombat && ActiveCombatEngine != null)
            {
                var result = ActiveCombatEngine.UseItemInCombat(_selectedItemId);
                GameEvents.RaiseNotification(result.Message);
                Close();
            }
            else
            {
                var result = _inventory.UseConsumable(_selectedItemId);
                GameEvents.RaiseNotification(result.Message);
            }

            _selectedItemId = null;
            if (detailPanel != null) detailPanel.SetActive(false);
        }

        private void OnDropClicked()
        {
            if (_inventory == null || _selectedItemId == null) return;

            if (_selectedItemId == _inventory.EquippedWeaponId)
                _inventory.UnequipSlot(ItemType.Weapon, putBackInBag: false);
            else if (_selectedItemId == _inventory.EquippedArmorId)
                _inventory.UnequipSlot(ItemType.Armor, putBackInBag: false);
            else if (_selectedItemId == _inventory.EquippedAccessoryId)
                _inventory.UnequipSlot(ItemType.Accessory, putBackInBag: false);
            else
                _inventory.DropItem(_selectedItemId);

            _selectedItemId = null;
            if (detailPanel != null) detailPanel.SetActive(false);
        }

        // ─────────────────────────────────────────────────────────
        //  Helpers
        // ─────────────────────────────────────────────────────────

        private Sprite GetItemSprite(ItemData item)
        {
            if (itemsSheet == null || item == null) return null;
            return SpriteSheetHelper.ExtractSprite(itemsSheet, item.SpriteCol, item.SpriteRow);
        }

        private static Color RarityColor(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Rare:  return new Color(0.6f, 0.82f, 1f, 1f);
                case ItemRarity.Epic:  return new Color(1f,   0.9f,  0.35f, 1f);
                default:               return Color.white;
            }
        }
    }
}
