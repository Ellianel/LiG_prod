using UnityEngine;
using UnityEngine.UI;
using LochyIGorzala.Core;
using LochyIGorzala.Managers;

namespace LochyIGorzala.UI
{
    /// <summary>
    /// Achievements panel toggled by a button in the top-left HUD corner.
    /// Shows all achievements with progress bars and unlock status.
    /// Subscribes to GameEvents.OnAchievementsChanged for auto-refresh.
    /// Built by ProjectSetupEditor.BuildAchievementsPanel().
    /// </summary>
    public class AchievementsUIController : MonoBehaviour
    {
        [SerializeField] private GameObject achievementsPanel;
        [SerializeField] private TMPro.TextMeshProUGUI achievementsText;
        [SerializeField] private Button toggleButton;

        private bool _isOpen;

        private void Awake()
        {
            GameEvents.OnAchievementsChanged += Refresh;
            GameEvents.OnAchievementUnlocked += OnUnlocked;
            if (achievementsPanel != null) achievementsPanel.SetActive(false);
            if (toggleButton != null) toggleButton.onClick.AddListener(Toggle);
        }

        private void OnDestroy()
        {
            GameEvents.OnAchievementsChanged -= Refresh;
            GameEvents.OnAchievementUnlocked -= OnUnlocked;
            if (toggleButton != null) toggleButton.onClick.RemoveListener(Toggle);
        }

        public void Toggle()
        {
            _isOpen = !_isOpen;
            if (achievementsPanel != null) achievementsPanel.SetActive(_isOpen);
            if (_isOpen) Refresh();
        }

        private void OnUnlocked(AchievementData achievement)
        {
            // Auto-refresh if panel is open
            if (_isOpen) Refresh();
        }

        public void Refresh()
        {
            if (achievementsText == null) return;

            var state = GameManager.Instance?.CurrentGameState;
            if (state == null)
            {
                achievementsText.text = "<color=#FFD700>Osiągnięcia</color>\n\n" +
                    "<color=#888888>Brak danych.</color>";
                return;
            }

            int unlocked = AchievementManager.GetUnlockedCount();
            int total    = AchievementManager.GetTotalCount();

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"<color=#FFD700>Osiągnięcia ({unlocked}/{total})</color>\n");

            foreach (var def in AchievementDatabase.All)
            {
                var progress = state.Achievements.GetOrCreate(def.Id);

                if (progress.Unlocked)
                {
                    // Unlocked — green with checkmark
                    sb.AppendLine($"<color=#66FF66><b>[+] {def.Title}</b></color>");
                    sb.AppendLine($"  <color=#AAFFAA>{def.Description}</color>");
                }
                else
                {
                    // Locked — grey with progress
                    string progressText = def.TargetCount > 1
                        ? $" ({progress.CurrentCount}/{def.TargetCount})"
                        : "";
                    sb.AppendLine($"<color=#888888><b>[ ] {def.Title}</b>{progressText}</color>");
                    sb.AppendLine($"  <color=#666666>{def.Description}</color>");
                }
                sb.AppendLine();
            }

            achievementsText.text = sb.ToString();
        }
    }
}
