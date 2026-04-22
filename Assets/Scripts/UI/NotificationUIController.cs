using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using LochyIGorzala.Core;

namespace LochyIGorzala.UI
{
    /// <summary>
    /// Displays timed notification messages on screen.
    /// Subscribes to GameEvents.OnNotification.
    /// Checks flags on Start() for death-respawn and Delirius-victory messages.
    /// The notification fades in, stays for a configurable duration, then fades out.
    /// </summary>
    public class NotificationUIController : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TMPro.TextMeshProUGUI messageText;

        private Coroutine _activeCoroutine;
        private CanvasGroup _canvasGroup;

        private void Awake()
        {
            _canvasGroup = panel?.GetComponent<CanvasGroup>();
            if (_canvasGroup == null && panel != null)
                _canvasGroup = panel.AddComponent<CanvasGroup>();

            if (panel != null) panel.SetActive(false);

            GameEvents.OnNotification += ShowNotification;
        }

        private void OnDestroy()
        {
            GameEvents.OnNotification -= ShowNotification;
        }

        private void Start()
        {
            var gm    = Managers.GameManager.Instance;
            var state = gm?.CurrentGameState;
            if (state == null || gm == null) return;

            // ── Death respawn message ────────────────────────────────
            if (gm.ShowDeathMessage && state.CurrentFloor == 0)
            {
                gm.ShowDeathMessage = false;
                ShowNotification("Spróbujmy na drugą nóżkę!", 5f);
                return; // Don't stack with Delirius message
            }

            // ── Delirius-defeated victory popup ──────────────────────
            if (state.DeliriusDefeated && state.CurrentFloor == 5)
            {
                // Don't clear the flag yet — NpcSpawner needs it to spawn Władca Podziemi
                ShowNotification(
                    "Brawo, pokonałeś finałowego bossa\ni ktoś czeka aby wręczyć Ci nagrodę!", 5f);
            }
        }

        /// <summary>
        /// Shows a notification with auto-calculated duration.
        /// Short messages: 3s. Longer messages: ~1s per 15 characters, min 3s, max 10s.
        /// </summary>
        public void ShowNotification(string message)
        {
            float duration = Mathf.Clamp(message.Length / 15f, 3f, 10f);
            ShowNotification(message, duration);
        }

        /// <summary>Shows a notification for a custom duration.</summary>
        public void ShowNotification(string message, float duration)
        {
            if (panel == null || messageText == null) return;

            if (_activeCoroutine != null)
                StopCoroutine(_activeCoroutine);

            _activeCoroutine = StartCoroutine(NotificationRoutine(message, duration));
        }

        private IEnumerator NotificationRoutine(string message, float duration)
        {
            messageText.text = message;
            panel.SetActive(true);
            if (_canvasGroup != null) _canvasGroup.alpha = 0f;

            // Fade in (0.3s)
            float elapsed = 0f;
            while (elapsed < 0.3f)
            {
                elapsed += Time.deltaTime;
                if (_canvasGroup != null) _canvasGroup.alpha = elapsed / 0.3f;
                yield return null;
            }
            if (_canvasGroup != null) _canvasGroup.alpha = 1f;

            // Hold
            yield return new WaitForSeconds(duration);

            // Fade out (0.5s)
            elapsed = 0f;
            while (elapsed < 0.5f)
            {
                elapsed += Time.deltaTime;
                if (_canvasGroup != null) _canvasGroup.alpha = 1f - (elapsed / 0.5f);
                yield return null;
            }

            panel.SetActive(false);
            _activeCoroutine = null;
        }
    }
}
