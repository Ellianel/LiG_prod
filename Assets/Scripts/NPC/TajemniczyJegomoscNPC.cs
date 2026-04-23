using UnityEngine;
using UnityEngine.InputSystem;
using LochyIGorzala.Core;
using LochyIGorzala.Items;
using LochyIGorzala.Managers;

namespace LochyIGorzala.NPC
{
    /// <summary>
    /// "Tajemniczy Jegomość" — a mysterious vagabond NPC in the lobby.
    ///
    /// First interaction: delivers the intro story, then unlocks stairs to floor 1.
    /// Subsequent interactions: gives kill quests and rewards.
    ///
    /// Quest flow:
    ///   1. Talk → intro speech → stairs appear → first quest offered
    ///   2. Complete quest → return → NPC gives reward + next quest
    ///   3. After all quests → NPC says farewell
    /// </summary>
    public class TajemniczyJegomoscNPC : MonoBehaviour, IInteractable
    {
        [Header("Proximity trigger distance (in tiles)")]
        [SerializeField] private float proximityRange = 2.2f;

        private Transform _playerTransform;
        private bool _interacting; // prevent double-triggers

        // ── IInteractable ─────────────────────────────────────────

        public string InteractLabel => "Porozmawiaj z Tajemniczym Jegomościem [E]";

        public void Interact(PlayerData player)
        {
            DoInteraction();
        }

        // ── Unity lifecycle ───────────────────────────────────────

        private void Start()
        {
            var playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO != null) _playerTransform = playerGO.transform;
        }

        private void Update()
        {
            if (_interacting) return;

            if (_playerTransform == null)
            {
                var playerGO = GameObject.FindGameObjectWithTag("Player");
                if (playerGO != null) _playerTransform = playerGO.transform;
                else return;
            }

            float dist = Vector2.Distance(transform.position, _playerTransform.position);
            if (dist <= proximityRange &&
                Keyboard.current != null &&
                Keyboard.current.eKey.wasPressedThisFrame)
            {
                DoInteraction();
            }
        }

        // ── Interaction logic ─────────────────────────────────────

        private void DoInteraction()
        {
            if (_interacting) return;
            _interacting = true;

            var gm = GameManager.Instance;
            var state = gm?.CurrentGameState;
            if (state == null) { _interacting = false; return; }

            if (!state.QuestData.IntroCompleted)
            {
                HandleFirstMeeting(state);
            }
            else
            {
                HandleQuestInteraction(state, gm);
            }

            _interacting = false;
        }

        private void HandleFirstMeeting(GameState state)
        {
            string speech =
                "Gniewko, Ty żyjesz? Chłopie, po tym jak zasnąłeś w beczce bimbru " +
                "myśleliśmy, że już po Tobie.\n\n" +
                "Wyglądasz okropnie, na taki stan pomóc Ci może tylko " +
                "Władca Podziemi — potężny czarodziej ukrywający się " +
                "na samym dnie tych lochów, za piątym piętrem.\n\n" +
                "Ale uważaj — lochy pełne są potworów! " +
                "Musisz przejść przez 5 pięter i pokonać straszliwego Deliriusa, " +
                "żeby do niego dotrzeć.\n\n" +
                "Mirek Handlarz po prawej sprzedaje ekwipunek — " +
                "wracaj do niego po złoto z potworów. Powodzenia!";

            // Mark intro as done
            state.QuestData.IntroCompleted = true;

            // Unlock stairs — place StairsDown on the lobby map
            const int sx = 23, sy = 16;
            state.Dungeon.SetTile(sx, sy, (int)Dungeon.TileType.StairsDown);

            // Re-render that tile visually
            var dg = FindAnyObjectByType<Dungeon.DungeonGenerator>(FindObjectsInactive.Include);
            if (dg != null)
                dg.PlaceStairsTile(sx, sy, isDown: true);

            Debug.Log("[TajemniczyJegomoscNPC] Intro completed — stairs unlocked.");

            // Accept floor progression quests (auto-tracked in journal)
            QuestManager.AcceptFloorQuests();

            // Offer the first kill quest — append to intro speech so it's one notification
            QuestData nextQuest = QuestManager.GetNextKillQuestToOffer();
            if (nextQuest != null)
            {
                QuestManager.AcceptQuest(nextQuest);
                speech += $"\n\n--- Nowa misja ---\n{nextQuest.Title}\n{nextQuest.Description}";
            }

            // Single notification with intro + quest combined (10s fixed duration)
            GameEvents.RaiseNotification(speech, 10f);
        }

        private void HandleQuestInteraction(GameState state, GameManager gm)
        {
            // Check for completed quest to reward
            QuestData completed = QuestManager.GetCompletedUnrewardedQuest();
            if (completed != null)
            {
                // Give reward items
                for (int i = 0; i < completed.RewardItemCount; i++)
                {
                    if (gm.PlayerInventory != null && !gm.PlayerInventory.IsFull)
                        gm.PlayerInventory.AddItem(completed.RewardItemId);
                }

                GameEvents.RaiseNotification(completed.RewardMessage);
                QuestManager.MarkRewarded(completed);

                // Offer next quest if available
                QuestData nextQuest = QuestManager.GetNextKillQuestToOffer();
                if (nextQuest != null)
                {
                    QuestManager.AcceptQuest(nextQuest);
                    // Small delay between messages — show quest after reward
                    GameEvents.RaiseNotification(
                        $"Nowa misja: {nextQuest.Title}\n{nextQuest.Description}");
                }
                else
                {
                    GameEvents.RaiseNotification(
                        "Nie mam już dla Ciebie zadań, ale bądź dzielny!\n" +
                        "Władca Podziemi czeka na kogoś odważnego...");
                }

                return;
            }

            // Check if there's an active quest in progress
            var visible = QuestManager.GetVisibleQuests();
            if (visible.Count > 0)
            {
                var active = visible[visible.Count - 1];
                if (active.Status == QuestStatus.Active)
                {
                    GameEvents.RaiseNotification(
                        $"Misja w toku: {active.Title}\n" +
                        $"Postęp: {active.ProgressText}\n" +
                        "Idź, nie stój tu — potwory same się nie zabiją!");
                }
            }
            else
            {
                // All quests done
                GameEvents.RaiseNotification(
                    "Nie mam już dla Ciebie zadań.\n" +
                    "Idź szukać Władcy Podziemi — powodzenia!");
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, proximityRange);
        }
    }
}
