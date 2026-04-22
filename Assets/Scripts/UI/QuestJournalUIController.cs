using UnityEngine;
using UnityEngine.UI;
using LochyIGorzala.Core;
using LochyIGorzala.Managers;

namespace LochyIGorzala.UI
{
    /// <summary>
    /// Quest journal panel toggled by a button in the top-left HUD corner.
    /// Shows active quests with kill progress.
    /// Subscribes to GameEvents.OnQuestJournalChanged for auto-refresh.
    /// Built by ProjectSetupEditor.BuildQuestJournal().
    /// </summary>
    public class QuestJournalUIController : MonoBehaviour
    {
        [SerializeField] private GameObject journalPanel;
        [SerializeField] private TMPro.TextMeshProUGUI journalText;
        [SerializeField] private Button toggleButton;

        private bool _isOpen;

        private void Awake()
        {
            GameEvents.OnQuestJournalChanged += Refresh;
            if (journalPanel != null) journalPanel.SetActive(false);
            if (toggleButton != null) toggleButton.onClick.AddListener(Toggle);
        }

        private void OnDestroy()
        {
            GameEvents.OnQuestJournalChanged -= Refresh;
            if (toggleButton != null) toggleButton.onClick.RemoveListener(Toggle);
        }

        public void Toggle()
        {
            _isOpen = !_isOpen;
            if (journalPanel != null) journalPanel.SetActive(_isOpen);
            if (_isOpen) Refresh();
        }

        public void Refresh()
        {
            if (journalText == null) return;

            var quests = QuestManager.GetVisibleQuests();
            if (quests.Count == 0)
            {
                journalText.text = "<color=#FFD700>Dziennik Misji</color>\n\n" +
                    "<color=#888888>Brak aktywnych misji.\nPorozmawiaj z Tajemniczym Jegomościem.</color>";
                return;
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("<color=#FFD700>Dziennik Misji</color>\n");

            foreach (var q in quests)
            {
                string statusColor;
                string statusLabel;

                if (q.Status == QuestStatus.Completed || q.Status == QuestStatus.Rewarded)
                {
                    statusColor = "#66FF66";
                    statusLabel = "WYKONANA!";
                }
                else
                {
                    statusColor = "#FFFFFF";
                    statusLabel = q.ProgressText;
                }

                sb.AppendLine($"<color={statusColor}><b>{q.Title}</b></color>");
                sb.AppendLine($"  {q.Description}");
                sb.AppendLine($"  Postęp: <color=#FFD700>{statusLabel}</color>");
                sb.AppendLine();
            }

            journalText.text = sb.ToString();
        }
    }
}
