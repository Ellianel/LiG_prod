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
    /// Shop panel — "Sklep Mirka Handlarza".
    ///
    /// Layout built by ProjectSetupEditor (same pattern as InventoryUIController):
    ///   ShopPanel
    ///   ├── Header (title + close)
    ///   ├── StockGrid  (GridLayoutGroup — items for sale, self-building slots)
    ///   ├── PlayerInventoryGrid (GridLayoutGroup — items to sell)
    ///   ├── DetailPanel (name / desc / price / Buy or Sell button)
    ///   └── GoldText
    ///
    /// Economy:
    ///   Buy  price = item.GoldValue
    ///   Sell price = item.GoldValue / 2  (floor)
    /// </summary>
    public class ShopUIController : MonoBehaviour
    {
        // ── Inspector references ──────────────────────────────────

        [Header("Panel root")]
        [SerializeField] private GameObject shopPanel;

        [Header("Header")]
        [SerializeField] private Button closeButton;
        [SerializeField] private TMPro.TextMeshProUGUI goldText;

        [Header("Stock (items for sale)")]
        [SerializeField] private Transform stockGridParent;    // GridLayoutGroup

        [Header("Player inventory (items to sell)")]
        [SerializeField] private Transform sellGridParent;     // GridLayoutGroup

        [Header("Detail panel")]
        [SerializeField] private GameObject detailPanel;
        [SerializeField] private TMPro.TextMeshProUGUI detailName;
        [SerializeField] private TMPro.TextMeshProUGUI detailDesc;
        [SerializeField] private TMPro.TextMeshProUGUI detailPrice;
        [SerializeField] private Button buyButton;
        [SerializeField] private Button sellButton;

        [Header("Sprite sheets")]
        [SerializeField] private Texture2D itemsSheet;

        // ── Stock definition ──────────────────────────────────────

        /// <summary>
        /// Mirek's fixed stock — always available regardless of floor.
        /// Add or remove items here to change what Mirek sells.
        /// Prices are taken from ItemDatabase.GoldValue.
        /// </summary>
        private static readonly string[] MirekStock = new[]
        {
            "healing_potion",
            "greater_healing_potion",
            "bimber",
            "zmijowa_nalewka",
            "antidote",
            "beer",
            "old_knife",
            "hunters_sword",
            "leather_armor",
            "round_shield",
            "red_pendant",
            "silver_dagger",
            "chainmail",
            "ruby_ring",
            "holy_mace",
            // Epic gear (80-100g) — endgame preparation for Delirius
            "blessed_sword",
            "wzmocniona_kolczuga",
            "wielki_eliksir",
        };

        // ── Runtime state ─────────────────────────────────────────

        private string   _selectedItemId;
        #pragma warning disable CS0414 // field assigned but never used — kept for future use
        private bool     _selectedIsSell;   // true = selling, false = buying
        #pragma warning restore CS0414
        private readonly List<GameObject> _stockSlots = new List<GameObject>();
        private readonly List<GameObject> _sellSlots  = new List<GameObject>();

        // ── Unity lifecycle ───────────────────────────────────────

        private void Awake()
        {
            if (shopPanel  != null) shopPanel.SetActive(false);
            if (detailPanel != null) detailPanel.SetActive(false);

            closeButton?.onClick.AddListener(Close);
            buyButton?.onClick.AddListener(OnBuyClicked);
            sellButton?.onClick.AddListener(OnSellClicked);
        }

        private void OnEnable()
        {
            // Named methods — lambdas cannot be unsubscribed (new delegate instance each time)
            GameEvents.OnGoldChanged      += OnGoldChangedHandler;
            GameEvents.OnInventoryChanged += RefreshSellGrid;
        }

        private void OnDisable()
        {
            GameEvents.OnGoldChanged      -= OnGoldChangedHandler;
            GameEvents.OnInventoryChanged -= RefreshSellGrid;
        }

        private void OnGoldChangedHandler(int _) => RefreshGold();

        // ── Open / Close ──────────────────────────────────────────

        public void Open()
        {
            if (shopPanel != null) shopPanel.SetActive(true);
            _selectedItemId  = null;
            if (detailPanel != null) detailPanel.SetActive(false);

            EnsureSlotPools();
            RefreshStock();
            RefreshSellGrid();
            RefreshGold();
        }

        public void Close()
        {
            if (shopPanel != null) shopPanel.SetActive(false);
        }

        // ── Slot pools ────────────────────────────────────────────

        private void EnsureSlotPools()
        {
            if (stockGridParent != null)
            {
                for (int i = _stockSlots.Count; i < MirekStock.Length; i++)
                    _stockSlots.Add(CreateSlotGO(stockGridParent));
            }
        }

        private GameObject CreateSlotGO(Transform parent)
        {
            var go  = new GameObject("ShopSlot");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.12f, 0.1f, 0.07f, 0.9f);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            ColorBlock cb = btn.colors;
            cb.highlightedColor = new Color(0.8f, 0.6f, 0.1f);
            btn.colors = cb;

            // Price label
            var priceGO = new GameObject("Price");
            priceGO.transform.SetParent(go.transform, false);
            var prt = priceGO.AddComponent<RectTransform>();
            prt.anchorMin = new Vector2(0f, 0f);
            prt.anchorMax = new Vector2(1f, 0.3f);
            prt.offsetMin = Vector2.zero; prt.offsetMax = Vector2.zero;
            var pTmp = priceGO.AddComponent<TMPro.TextMeshProUGUI>();
            pTmp.fontSize  = 16;
            pTmp.alignment = TMPro.TextAlignmentOptions.Center;
            pTmp.color     = new Color(0.95f, 0.8f, 0.2f);

            return go;
        }

        // ── Stock grid (buy side) ─────────────────────────────────

        private void RefreshStock()
        {
            if (stockGridParent == null) return;

            for (int i = 0; i < MirekStock.Length; i++)
            {
                if (i >= _stockSlots.Count) break;
                PopulateStockSlot(_stockSlots[i], MirekStock[i]);
            }
        }

        private void PopulateStockSlot(GameObject slot, string itemId)
        {
            ItemData item = ItemDatabase.Get(itemId);
            if (item == null) { slot.SetActive(false); return; }
            slot.SetActive(true);

            var icon = slot.GetComponent<Image>();
            if (icon != null)
            {
                icon.sprite = GetSprite(item);
                icon.color  = RarityColor(item.Rarity);
            }

            var price = slot.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (price != null) price.text = $"{item.GoldValue}z";

            var btn = slot.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                string captured = itemId;
                btn.onClick.AddListener(() => SelectForBuy(captured));
            }
        }

        // ── Player inventory grid (sell side) ─────────────────────

        private void RefreshSellGrid()
        {
            if (sellGridParent == null || !gameObject.activeInHierarchy) return;

            var inv = GameManager.Instance?.PlayerInventory;
            if (inv == null) return;

            var ids = new List<string>(inv.ItemIds);

            // Ensure enough sell slots exist
            while (_sellSlots.Count < ids.Count)
                _sellSlots.Add(CreateSlotGO(sellGridParent));

            for (int i = 0; i < _sellSlots.Count; i++)
            {
                if (i < ids.Count)
                {
                    _sellSlots[i].SetActive(true);
                    PopulateSellSlot(_sellSlots[i], ids[i]);
                }
                else
                {
                    _sellSlots[i].SetActive(false);
                }
            }
        }

        private void PopulateSellSlot(GameObject slot, string itemId)
        {
            ItemData item = ItemDatabase.Get(itemId);
            if (item == null) { slot.SetActive(false); return; }

            var icon = slot.GetComponent<Image>();
            if (icon != null)
            {
                icon.sprite = GetSprite(item);
                icon.color  = RarityColor(item.Rarity);
            }

            int sellPrice = Mathf.FloorToInt(item.GoldValue * 0.5f);
            var price = slot.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (price != null) price.text = $"{sellPrice}z";

            var btn = slot.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                string captured = itemId;
                btn.onClick.AddListener(() => SelectForSell(captured));
            }
        }

        // ── Selection ─────────────────────────────────────────────

        private void SelectForBuy(string itemId)
        {
            _selectedItemId  = itemId;
            _selectedIsSell  = false;
            ShowDetail(ItemDatabase.Get(itemId), isSell: false);
        }

        private void SelectForSell(string itemId)
        {
            _selectedItemId  = itemId;
            _selectedIsSell  = true;
            ShowDetail(ItemDatabase.Get(itemId), isSell: true);
        }

        private void ShowDetail(ItemData item, bool isSell)
        {
            if (detailPanel == null || item == null) return;
            detailPanel.SetActive(true);

            if (detailName  != null)
                detailName.text = $"<color={item.RarityColour()}>{item.Name}</color>  [{item.Rarity}]";
            if (detailDesc  != null) detailDesc.text  = item.Description;

            int goldCoins = GameManager.Instance?.CurrentGameState?.GoldCoins ?? 0;

            if (isSell)
            {
                int sellPrice = Mathf.FloorToInt(item.GoldValue * 0.5f);
                if (detailPrice != null) detailPrice.text = $"Cena sprzedazy: {sellPrice} zlota";
                if (buyButton   != null) buyButton.gameObject.SetActive(false);
                if (sellButton  != null)
                {
                    sellButton.gameObject.SetActive(true);
                    var lbl = sellButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                    if (lbl != null) lbl.text = $"Sprzedaj ({sellPrice}z)";
                }
            }
            else
            {
                bool canAfford = goldCoins >= item.GoldValue;
                if (detailPrice != null)
                    detailPrice.text = canAfford
                        ? $"Cena: {item.GoldValue} zlota"
                        : $"<color=#FF4444>Cena: {item.GoldValue} zlota\n(za malo zlota!)</color>";

                if (sellButton  != null) sellButton.gameObject.SetActive(false);
                if (buyButton   != null)
                {
                    buyButton.gameObject.SetActive(true);
                    buyButton.interactable = canAfford;
                    var lbl = buyButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                    if (lbl != null) lbl.text = $"Kup ({item.GoldValue}z)";
                }
            }
        }

        // ── Transactions ──────────────────────────────────────────

        private void OnBuyClicked()
        {
            if (_selectedItemId == null) return;
            ItemData item = ItemDatabase.Get(_selectedItemId);
            if (item == null) return;

            var gm = GameManager.Instance;
            if (gm?.CurrentGameState == null) return;

            if (gm.CurrentGameState.GoldCoins < item.GoldValue)
            {
                GameEvents.RaiseNotification("Za malo zlota!");
                return;
            }

            if (gm.PlayerInventory == null || gm.PlayerInventory.IsFull)
            {
                GameEvents.RaiseNotification("Plecak pelny!");
                return;
            }

            gm.CurrentGameState.GoldCoins -= item.GoldValue;
            gm.PlayerInventory.AddItem(_selectedItemId);
            GameEvents.RaiseGoldChanged(gm.CurrentGameState.GoldCoins);
            GameEvents.RaiseNotification($"Kupiono: {item.Name}!");

            // Refresh UI
            RefreshGold();
            if (detailPanel != null) detailPanel.SetActive(false);
            _selectedItemId = null;
        }

        private void OnSellClicked()
        {
            if (_selectedItemId == null) return;
            ItemData item = ItemDatabase.Get(_selectedItemId);
            if (item == null) return;

            var gm = GameManager.Instance;
            if (gm?.CurrentGameState == null || gm.PlayerInventory == null) return;

            // Remove from inventory
            if (!gm.PlayerInventory.RemoveItem(_selectedItemId))
            {
                GameEvents.RaiseNotification("Przedmiot nie znaleziony w plecaku.");
                return;
            }

            int sellPrice = Mathf.FloorToInt(item.GoldValue * 0.5f);
            gm.CurrentGameState.GoldCoins += sellPrice;
            GameEvents.RaiseGoldChanged(gm.CurrentGameState.GoldCoins);
            GameEvents.RaiseNotification($"Sprzedano {item.Name} za {sellPrice} zlota!");

            RefreshGold();
            RefreshSellGrid();
            if (detailPanel != null) detailPanel.SetActive(false);
            _selectedItemId = null;
        }

        // ── Helpers ───────────────────────────────────────────────

        private void RefreshGold()
        {
            if (goldText == null) return;
            int gold = GameManager.Instance?.CurrentGameState?.GoldCoins ?? 0;
            goldText.text = $"Twoje zloto: {gold}";
        }

        private Sprite GetSprite(ItemData item)
        {
            if (itemsSheet == null || item == null) return null;
            return SpriteSheetHelper.ExtractSprite(itemsSheet, item.SpriteCol, item.SpriteRow);
        }

        private static Color RarityColor(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Rare: return new Color(0.6f, 0.82f, 1f);
                case ItemRarity.Epic: return new Color(1f,   0.9f,  0.35f);
                default:              return Color.white;
            }
        }
    }
}
