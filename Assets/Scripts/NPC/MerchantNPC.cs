using UnityEngine;
using UnityEngine.InputSystem;   // new Input System — NOT legacy Input.GetKeyDown
using LochyIGorzala.Core;
using LochyIGorzala.Items;

namespace LochyIGorzala.NPC
{
    /// <summary>
    /// "Mirek Handlarz" — the merchant who stands in the lobby (floor 0).
    /// When the player walks within ProximityRange tiles, a prompt appears and
    /// pressing E opens the ShopUIController.
    ///
    /// Implements IInteractable so the same interface used by items covers NPCs
    /// (satisfies prowadzący's polymorphism requirement).
    ///
    /// Uses the new Input System (UnityEngine.InputSystem.Keyboard) because the
    /// project has switched away from the legacy UnityEngine.Input class.
    /// </summary>
    public class MerchantNPC : MonoBehaviour, IInteractable
    {
        [Header("Proximity trigger distance (in tiles)")]
        [SerializeField] private float proximityRange = 2.2f;

        [Header("Shop UI reference (injected by NpcSpawner)")]
        [SerializeField] private UI.ShopUIController shopUI;

        /// <summary>Called by NpcSpawner via SendMessage to inject the shop reference.</summary>
        public void SetShopUI(UI.ShopUIController ui) => shopUI = ui;

        private Transform _playerTransform;

        // ── IInteractable ─────────────────────────────────────────

        public string InteractLabel => "Sklep Mirka [E]";

        public void Interact(PlayerData player)
        {
            shopUI?.Open();
        }

        // ── Unity lifecycle ───────────────────────────────────────

        private void Start()
        {
            // Find player — retry is cheap since this is the lobby (no enemies)
            var playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO != null) _playerTransform = playerGO.transform;
        }

        private void Update()
        {
            // Lazy player lookup in case Start() ran before player spawned
            if (_playerTransform == null)
            {
                var playerGO = GameObject.FindGameObjectWithTag("Player");
                if (playerGO != null) _playerTransform = playerGO.transform;
                else return;
            }

            float dist    = Vector2.Distance(transform.position, _playerTransform.position);
            bool  inRange = dist <= proximityRange;

            // Open shop on E press (new Input System)
            if (inRange && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                Interact(null);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, proximityRange);
        }
    }
}
