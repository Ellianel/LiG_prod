using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using LochyIGorzala.Core;
using LochyIGorzala.Enemies;  // EnemyData, Delirius
using LochyIGorzala.Helpers;
using LochyIGorzala.Managers;

namespace LochyIGorzala.Combat
{
    /// <summary>
    /// Controls the combat UI scene — Pokemon Fire Red style layout.
    /// This is purely the View layer; all logic runs through CombatEngine.
    /// </summary>
    public class CombatUIController : MonoBehaviour
    {
        [Header("Enemy Display")]
        [SerializeField] private Image enemyImage;
        [SerializeField] private TMPro.TextMeshProUGUI enemyNameText;
        [SerializeField] private TMPro.TextMeshProUGUI enemyLevelText;
        [SerializeField] private Image enemyHPBar;
        [SerializeField] private TMPro.TextMeshProUGUI enemyHPText;

        [Header("Player Display")]
        [SerializeField] private TMPro.TextMeshProUGUI playerNameText;
        [SerializeField] private TMPro.TextMeshProUGUI playerLevelText;
        [SerializeField] private Image playerHPBar;
        [SerializeField] private TMPro.TextMeshProUGUI playerHPText;
        [SerializeField] private TMPro.TextMeshProUGUI playerAPText;
        [SerializeField] private Image toxicityBar;

        [Header("Action Buttons")]
        [SerializeField] private Button lightAttackButton;
        [SerializeField] private Button heavyAttackButton;
        [SerializeField] private Button healButton;
        [SerializeField] private Button bimberButton;
        [SerializeField] private Button defendButton;
        [SerializeField] private Button fleeButton;

        [Header("Message Box")]
        [SerializeField] private TMPro.TextMeshProUGUI messageText;
        [SerializeField] private GameObject actionPanel;
        [SerializeField] private GameObject messagePanel;

        [Header("Sprite Sheet")]
        [SerializeField] private Texture2D monstersSheet;

        [Header("Background")]
        [SerializeField] private Image backgroundImage;

        [Header("Inventory (bag button in action panel)")]
        [SerializeField] private Button bagButton;

        private CombatEngine combatEngine;
        private bool isProcessingTurn;

        // Inventory panel — found at runtime in same canvas
        private UI.InventoryUIController _inventoryUI;

        private void Start()
        {
            InitializeCombat();
            SetupButtons();
            // Wire bag button after inventory UI is found in InitializeCombat
            bagButton?.onClick.AddListener(OnBagClicked);
        }

        private void OnBagClicked()
        {
            _inventoryUI?.Toggle(inCombat: true);
        }

        private void InitializeCombat()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || gm.CurrentCombatEnemy == null)
            {
                Debug.LogError("CombatUIController: No enemy data found. Returning to dungeon.");
                ReturnToDungeon();
                return;
            }

            PlayerData player = gm.CurrentGameState.Player;
            EnemyData enemy = gm.CurrentCombatEnemy;

            combatEngine = new CombatEngine(player, enemy)
            {
                // Wire up runtime inventory so equipped weapon damage type
                // (Silver / Holy / Fire) is applied and loot is auto-picked up.
                PlayerInventory = gm.PlayerInventory
            };

            // Setup enemy sprite
            if (monstersSheet != null)
            {
                Sprite enemySprite = SpriteSheetHelper.ExtractSprite(
                    monstersSheet, enemy.SpriteCol, enemy.SpriteRow);
                if (enemySprite != null && enemyImage != null)
                {
                    enemyImage.sprite = enemySprite;
                    enemyImage.preserveAspect = true;
                }
            }

            // Load floor-specific combat background
            LoadFloorBackground(gm.CurrentGameState.CurrentFloor);

            // Show class-specific special name on the heavy attack button (slot 1)
            if (heavyAttackButton != null)
            {
                var lbl = heavyAttackButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (lbl != null)
                    lbl.text = combatEngine.AvailableActions[1].ActionName;
            }

            // Wire InventoryUIController (found anywhere in the scene)
            _inventoryUI = FindAnyObjectByType<UI.InventoryUIController>(FindObjectsInactive.Include);
            if (_inventoryUI != null)
                _inventoryUI.ActiveCombatEngine = combatEngine;

            UpdateUI();
            ShowMessage(combatEngine.LastMessage);
        }

        private void SetupButtons()
        {
            if (lightAttackButton != null)
                lightAttackButton.onClick.AddListener(() => ExecuteAction(0));
            if (heavyAttackButton != null)
                heavyAttackButton.onClick.AddListener(() => ExecuteAction(1));
            if (healButton != null)
                healButton.onClick.AddListener(() => ExecuteAction(2));
            if (bimberButton != null)
                bimberButton.onClick.AddListener(() => ExecuteAction(3));
            if (defendButton != null)
                defendButton.onClick.AddListener(() => ExecuteAction(4));
            if (fleeButton != null)
                fleeButton.onClick.AddListener(() => ExecuteAction(5));
        }

        private void ExecuteAction(int actionIndex)
        {
            if (isProcessingTurn || combatEngine == null) return;
            if (actionIndex < 0 || actionIndex >= combatEngine.AvailableActions.Count) return;

            ICombatAction action = combatEngine.AvailableActions[actionIndex];
            StartCoroutine(ProcessTurn(action));
        }

        private IEnumerator ProcessTurn(ICombatAction action)
        {
            isProcessingTurn = true;
            SetButtonsInteractable(false);

            // Execute the action
            CombatTurnResult result = combatEngine.ExecutePlayerAction(action);

            // Immediately refresh HP bars so damage is visible right away
            UpdateUI();

            // Check if Delirius just entered phase 2 (must do before showing result message)
            string phaseMessage = null;
            if (combatEngine.Enemy is Delirius deliriusBoss && deliriusBoss.PhaseTransitionJustOccurred)
            {
                deliriusBoss.ClearPhaseTransitionFlag();
                // ASCII only — no emoji (LiberationSans does not support them)
                phaseMessage = "\n\n!! DELIRIUS WPADA W SZAŁ !!\n\"TY...TY NAPRAWDĘ ŚMIESZ MNIE RANIĆ?!\"\nJego moc rośnie BEZGRANICZNIE!";
            }

            string fullMessage = result.Message + (phaseMessage ?? "");
            ShowMessage(fullMessage);

            yield return new WaitForSeconds(1.5f);

            UpdateUI();

            // Handle combat end states
            switch (result.ResultingPhase)
            {
                case CombatPhase.Victory:
                    // Special message for defeating the final boss
                    bool isFinalBoss = combatEngine.Enemy is Delirius;
                    if (isFinalBoss)
                        ShowMessage("DELIRIUS upada po raz ostatni...\n\"Nie... to... niemożliwe...\"\nJego forma rozpada się w pył!");
                    yield return new WaitForSeconds(isFinalBoss ? 3f : 2f);
                    combatEngine.EndCombat();
                    if (GameManager.Instance != null)
                    {
                        // Add gold here — single source of truth (CombatEngine does NOT add it)
                        if (result.GoldGained > 0 && GameManager.Instance.CurrentGameState != null)
                        {
                            GameManager.Instance.CurrentGameState.GoldCoins += result.GoldGained;
                            // Notify UI (InventoryUIController/ShopUIController can refresh gold display)
                            GameEvents.RaiseGoldChanged(GameManager.Instance.CurrentGameState.GoldCoins);
                        }
                        GameManager.Instance.OnCombatWon();
                    }
                    ReturnToDungeon();
                    break;

                case CombatPhase.Defeat:
                    yield return new WaitForSeconds(2f);
                    combatEngine.EndCombat();
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.OnCombatLost();
                    }
                    break;

                case CombatPhase.Fled:
                    yield return new WaitForSeconds(1f);
                    combatEngine.EndCombat();
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.OnCombatFled();
                    }
                    ReturnToDungeon();
                    break;

                default:
                    // Continue combat
                    SetButtonsInteractable(true);
                    UpdateButtonStates();
                    break;
            }

            isProcessingTurn = false;
        }

        private void UpdateUI()
        {
            if (combatEngine == null) return;

            PlayerData player = combatEngine.Player;
            EnemyData enemy = combatEngine.Enemy;

            // Enemy info
            if (enemyNameText != null) enemyNameText.text = enemy.Name;
            if (enemyLevelText != null) enemyLevelText.text = $"Poz. {enemy.Level}";
            if (enemyHPBar != null)
            {
                float eRatio = Mathf.Clamp01((float)enemy.CurrentHP / enemy.MaxHP);
                enemyHPBar.rectTransform.anchorMax = new Vector2(eRatio, 1f);
            }
            if (enemyHPText != null) enemyHPText.text = $"{enemy.CurrentHP}/{enemy.MaxHP}";

            // Player info
            if (playerNameText != null) playerNameText.text = player.Name;
            if (playerLevelText != null) playerLevelText.text = $"Poz. {player.Level}";
            if (playerHPBar != null)
            {
                float pRatio = Mathf.Clamp01((float)player.CurrentHP / player.MaxHP);
                playerHPBar.rectTransform.anchorMax = new Vector2(pRatio, 1f);
            }
            if (playerHPText != null) playerHPText.text = $"{player.CurrentHP}/{player.MaxHP}";
            if (playerAPText != null) playerAPText.text = $"Wigor: {player.ActionPoints}/{player.MaxActionPoints}";
            if (toxicityBar != null)
            {
                float tRatio = Mathf.Clamp01(player.Toxicity / player.MaxToxicity);
                toxicityBar.rectTransform.anchorMax = new Vector2(tRatio, 1f);
            }
        }

        private void UpdateButtonStates()
        {
            if (combatEngine == null) return;
            int ap = combatEngine.Player.ActionPoints;

            // Disable buttons that cost more AP than available
            if (lightAttackButton != null) lightAttackButton.interactable = ap >= 1;
            if (heavyAttackButton != null) heavyAttackButton.interactable = ap >= 2;
            if (healButton != null) healButton.interactable = ap >= 1;
            if (bimberButton != null) bimberButton.interactable = ap >= 1;
            if (defendButton != null) defendButton.interactable = ap >= 1;
            if (fleeButton != null) fleeButton.interactable = true; // Flee is always available
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (lightAttackButton != null) lightAttackButton.interactable = interactable;
            if (heavyAttackButton != null) heavyAttackButton.interactable = interactable;
            if (healButton != null) healButton.interactable = interactable;
            if (bimberButton != null) bimberButton.interactable = interactable;
            if (defendButton != null) defendButton.interactable = interactable;
            if (fleeButton != null) fleeButton.interactable = interactable;
        }

        private void ShowMessage(string message)
        {
            if (messageText != null) messageText.text = message;
        }

        /// <summary>
        /// Loads the combat background texture matching the current dungeon floor.
        /// Images are in Resources/CombatBackgrounds/combat_bg_floorN.
        /// Falls back to a dark tint if no image is found (lobby or missing asset).
        /// </summary>
        private void LoadFloorBackground(int floor)
        {
            if (backgroundImage == null) return;

            // Floors 1-5 have dedicated backgrounds; lobby (0) keeps the default dark color
            if (floor < 1 || floor > 5)
            {
                backgroundImage.color = new Color(0.08f, 0.06f, 0.1f);
                return;
            }

            Texture2D tex = Resources.Load<Texture2D>($"CombatBackgrounds/combat_bg_floor{floor}");
            if (tex != null)
            {
                Sprite bgSprite = Sprite.Create(
                    tex,
                    new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f));
                backgroundImage.sprite = bgSprite;
                backgroundImage.type = Image.Type.Simple;
                backgroundImage.preserveAspect = false;
                backgroundImage.color = Color.white; // Show the texture at full brightness
            }
            else
            {
                Debug.LogWarning($"[CombatUI] No background found for floor {floor}");
                backgroundImage.color = new Color(0.08f, 0.06f, 0.1f);
            }
        }

        private void ReturnToDungeon()
        {
            // BUG FIX: was using GameEvents which could be dead after ClearAll().
            // Use SceneManager directly — always safe.
            UnityEngine.SceneManagement.SceneManager.LoadScene("Dungeon");
        }
    }
}
