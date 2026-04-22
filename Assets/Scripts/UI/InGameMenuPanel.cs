using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LochyIGorzala.Core;
using LochyIGorzala.Managers;

namespace LochyIGorzala.UI
{
    /// <summary>
    /// In-game pause/settings menu. Toggled by the gear icon button in the dungeon HUD.
    /// Provides: Save Game (-> slot picker), Return to Menu, Resume.
    /// Also wires the Bag button to the InventoryUIController.
    /// </summary>
    public class InGameMenuPanel : MonoBehaviour
    {
        [Header("Gear Toggle Button (HUD)")]
        [SerializeField] private Button gearButton;

        [Header("Bag / Inventory Button (HUD)")]
        [SerializeField] private Button bagButton;
        [SerializeField] private InventoryUIController inventoryUI;

        [Header("Menu Panel (shown/hidden on toggle)")]
        [SerializeField] private GameObject menuPanel;

        [Header("Menu Buttons")]
        [SerializeField] private Button saveButton;
        [SerializeField] private Button returnToMenuButton;
        [SerializeField] private Button resumeButton;

        [Header("Save Slot Panel (child of this canvas)")]
        [SerializeField] private SaveSlotPanel saveSlotPanel;

        [Header("Settings")]
        [SerializeField] private Button settingsButton;
        [SerializeField] private SettingsPanel settingsPanel;

        [Header("Floor Indicator (optional HUD text)")]
        [SerializeField] private TextMeshProUGUI floorLabel;

        private void Start()
        {
            gearButton?.onClick.AddListener(ToggleMenu);
            saveButton?.onClick.AddListener(OnSaveClicked);
            settingsButton?.onClick.AddListener(OnSettingsClicked);
            returnToMenuButton?.onClick.AddListener(OnReturnToMenu);
            resumeButton?.onClick.AddListener(OnResume);

            // Bag button opens / closes the inventory panel
            bagButton?.onClick.AddListener(OnBagClicked);

            if (menuPanel != null) menuPanel.SetActive(false);
            UpdateFloorLabel();

            GameEvents.OnDungeonGenerated += UpdateFloorLabel;
        }

        private void OnDestroy()
        {
            GameEvents.OnDungeonGenerated -= UpdateFloorLabel;
        }

        // ── Bag ──────────────────────────────────────────────────

        private void OnBagClicked()
        {
            if (inventoryUI == null)
            {
                // Fallback: search scene at runtime if not wired in editor
                inventoryUI = FindAnyObjectByType<InventoryUIController>(FindObjectsInactive.Include);
            }
            inventoryUI?.Toggle(inCombat: false);

            // Close the gear menu if it was open
            if (menuPanel != null && menuPanel.activeSelf)
                menuPanel.SetActive(false);
        }

        // ── Gear menu ─────────────────────────────────────────────

        private void UpdateFloorLabel()
        {
            if (floorLabel == null) return;
            int floor = GameManager.Instance?.CurrentGameState?.CurrentFloor ?? 0;
            floorLabel.text = floor == 0 ? "Przedsionek" : $"Pietro {floor}";
        }

        private void ToggleMenu()
        {
            if (menuPanel == null) return;
            bool nowActive = !menuPanel.activeSelf;
            menuPanel.SetActive(nowActive);

            if (!nowActive && saveSlotPanel != null)
                saveSlotPanel.gameObject.SetActive(false);
        }

        private void OnSaveClicked()
        {
            if (menuPanel != null) menuPanel.SetActive(false);
            if (saveSlotPanel != null)
                saveSlotPanel.ShowForSave();
        }

        private void OnSettingsClicked()
        {
            if (menuPanel != null) menuPanel.SetActive(false);
            if (settingsPanel != null)
                settingsPanel.Show(restore: null); // no panel to restore — player reopens gear menu manually
        }

        private void OnReturnToMenu()
        {
            GameManager.Instance?.ReturnToMainMenu();
        }

        private void OnResume()
        {
            if (menuPanel != null) menuPanel.SetActive(false);
        }
    }
}
