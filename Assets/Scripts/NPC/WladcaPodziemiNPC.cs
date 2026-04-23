using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using LochyIGorzala.Core;
using LochyIGorzala.Items;
using LochyIGorzala.Managers;

namespace LochyIGorzala.NPC
{
    /// <summary>
    /// "Władca Podziemi" — a drunk wizard NPC who appears in the lobby after
    /// the player defeats Delirius (the final boss).
    ///
    /// When the player approaches and presses E, the wizard delivers a funny
    /// Slavic-style congratulations speech and gives the player "Boski Lek na Kaca"
    /// (a godlike hangover cure).
    ///
    /// After giving the item, sets DeliriusDefeated = false so the NPC and
    /// victory notification don't appear again on next visit.
    /// </summary>
    public class WladcaPodziemiNPC : MonoBehaviour, IInteractable
    {
        [Header("Proximity trigger distance (in tiles)")]
        [SerializeField] private float proximityRange = 2.2f;

        private Transform _playerTransform;
        private bool _giftGiven;

        // ── IInteractable ─────────────────────────────────────────

        public string InteractLabel => "Porozmawiaj z Władcą Podziemi [E]";

        public void Interact(PlayerData player)
        {
            GiveReward();
        }

        // ── Unity lifecycle ───────────────────────────────────────

        private void Start()
        {
            var playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO != null) _playerTransform = playerGO.transform;
        }

        private void Update()
        {
            if (_giftGiven) return;

            // Lazy player lookup
            if (_playerTransform == null)
            {
                var playerGO = GameObject.FindGameObjectWithTag("Player");
                if (playerGO != null) _playerTransform = playerGO.transform;
                else return;
            }

            float dist    = Vector2.Distance(transform.position, _playerTransform.position);
            bool  inRange = dist <= proximityRange;

            if (inRange && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                GiveReward();
        }

        // ── Reward logic ──────────────────────────────────────────

        private void GiveReward()
        {
            if (_giftGiven) return;

            var gm = GameManager.Instance;
            if (gm == null || gm.PlayerInventory == null) return;

            _giftGiven = true;

            // Give the godlike item
            if (gm.PlayerInventory.IsFull)
            {
                // Notify and try again next time
                _giftGiven = false;
                GameEvents.RaiseNotification("Plecak pełny! Zrób miejsce i wróć do mnie.");
                return;
            }

            gm.PlayerInventory.AddItem("boski_lek_na_kaca");

            // ── Credits message ──────────────────────────────────────
            string credits =
                "Udało się Ci się zabić finałowego bossa i zdobyć lek na kaca! " +
                "Dziękujemy za grę!\n\n" +
                "Daniel Borowski\n" +
                "Michał Niekłań\n" +
                "Jarek Młodziejewski\n" +
                "Kuba Ciecierski\n" +
                "Radek Młynarczyk";

            GameEvents.RaiseNotification(credits);

            // Clear the flag so the NPC doesn't repeat on reload
            if (gm.CurrentGameState != null)
                gm.CurrentGameState.DeliriusDefeated = false;

            Debug.Log("[WladcaPodziemiNPC] Gift given — showing credits, quitting in 8s.");

            // Quit the game after a delay so the player can read the credits
            StartCoroutine(QuitAfterDelay(8f));
        }

        /// <summary>
        /// Waits for the credits to be readable, then quits the application.
        /// In the editor it stops play mode instead.
        /// </summary>
        private IEnumerator QuitAfterDelay(float seconds)
        {
            yield return new WaitForSeconds(seconds);

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, proximityRange);
        }
    }
}
