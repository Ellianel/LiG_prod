using UnityEngine;
using UnityEngine.UI;
using LochyIGorzala.Core;
using LochyIGorzala.Managers;

namespace LochyIGorzala.UI
{
    public enum SlotPanelMode { Save, Load }

    /// <summary>
    /// Reusable 3-slot save/load panel. Works in both Save and Load mode.
    /// Used from the in-game settings panel and the main menu.
    /// </summary>
    public class SaveSlotPanel : MonoBehaviour
    {
        [Header("Slot Buttons")]
        [SerializeField] private Button slot1Button;
        [SerializeField] private Button slot2Button;
        [SerializeField] private Button slot3Button;

        [Header("Slot Labels")]
        [SerializeField] private TMPro.TextMeshProUGUI slot1Label;
        [SerializeField] private TMPro.TextMeshProUGUI slot2Label;
        [SerializeField] private TMPro.TextMeshProUGUI slot3Label;

        [Header("Panel Header + Cancel")]
        [SerializeField] private TMPro.TextMeshProUGUI titleText;
        [SerializeField] private Button cancelButton;

        private SlotPanelMode currentMode;
        // Optional: restore this GameObject when the panel is closed/cancelled
        private GameObject objectToRestoreOnClose;
        private bool listenersAdded = false;

        private void Awake()
        {
            // Set up listeners in Awake() so they're ready even when the object starts inactive.
            // Awake() IS called on inactive objects at scene load, Start() is NOT.
            // BUG FIX: previously in Start() with gameObject.SetActive(false), which caused:
            //   ShowForLoad() → SetActive(true) → Start() deferred → Start() immediately hid panel
            if (!listenersAdded)
            {
                slot1Button?.onClick.AddListener(() => OnSlotClicked(1));
                slot2Button?.onClick.AddListener(() => OnSlotClicked(2));
                slot3Button?.onClick.AddListener(() => OnSlotClicked(3));
                cancelButton?.onClick.AddListener(OnCancel);
                listenersAdded = true;
            }
            // Panel stays in whatever state the editor/caller put it in.
            // DO NOT call gameObject.SetActive(false) here — the editor script already creates it inactive.
        }

        public void ShowForSave(GameObject restoreOnClose = null)
        {
            currentMode = SlotPanelMode.Save;
            objectToRestoreOnClose = restoreOnClose;
            if (titleText != null) titleText.text = "ZAPISZ GRĘ";
            RefreshSlots();
            gameObject.SetActive(true);
        }

        public void ShowForLoad(GameObject restoreOnClose = null)
        {
            currentMode = SlotPanelMode.Load;
            objectToRestoreOnClose = restoreOnClose;
            if (titleText != null) titleText.text = "WCZYTAJ GRĘ";
            RefreshSlots();
            gameObject.SetActive(true);
        }

        private void OnCancel()
        {
            gameObject.SetActive(false);
            if (objectToRestoreOnClose != null)
                objectToRestoreOnClose.SetActive(true);
        }

        private void OnSlotClicked(int slot)
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            if (currentMode == SlotPanelMode.Save)
            {
                gm.SaveToSlot(slot);
                gameObject.SetActive(false);
                RefreshSlots(); // Update labels in case panel is reopened
            }
            else // Load
            {
                if (gm.HasSaveInSlot(slot))
                {
                    gameObject.SetActive(false);
                    gm.LoadFromSlot(slot);
                }
            }
        }

        private void RefreshSlots()
        {
            RefreshSlot(1, slot1Button, slot1Label);
            RefreshSlot(2, slot2Button, slot2Label);
            RefreshSlot(3, slot3Button, slot3Label);
        }

        private void RefreshSlot(int slot, Button btn, TMPro.TextMeshProUGUI label)
        {
            if (label == null) return;
            var gm = GameManager.Instance;

            bool hasData = gm != null && gm.HasSaveInSlot(slot);

            if (!hasData)
            {
                label.text = $"SLOT {slot}\n<size=75%>— Pusty slot —</size>";
                // In Load mode disable empty slots
                if (btn != null) btn.interactable = currentMode == SlotPanelMode.Save;
            }
            else
            {
                GameState preview = gm.GetSlotPreview(slot);
                if (preview == null)
                {
                    label.text = $"SLOT {slot}\n<size=75%>Błąd odczytu</size>";
                }
                else
                {
                    string cls   = preview.Player?.CharacterClass ?? "?";
                    int    lvl   = preview.Player?.Level ?? 0;
                    int    floor = preview.CurrentFloor;
                    string time  = FormatTime(preview.PlayTimeSeconds);
                    string date  = string.IsNullOrEmpty(preview.SaveDate) ? "" : $"\n{preview.SaveDate}";
                    label.text = $"SLOT {slot}  {cls} Poz.{lvl}\n<size=75%>Piętro {floor}  |  {time}{date}</size>";
                }
                if (btn != null) btn.interactable = true;
            }
        }

        private static string FormatTime(float totalSeconds)
        {
            int m = (int)(totalSeconds / 60);
            int s = (int)(totalSeconds % 60);
            return $"{m:00}:{s:00}";
        }
    }
}
