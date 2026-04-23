using UnityEngine;
using UnityEngine.InputSystem;
using LochyIGorzala.Core;
using LochyIGorzala.Combat;
using LochyIGorzala.Dungeon;
using LochyIGorzala.Enemies;
using LochyIGorzala.Helpers;
using LochyIGorzala.Managers;

namespace LochyIGorzala.Player
{
    /// <summary>
    /// Handles player movement on the dungeon tilemap.
    /// Grid-based movement (tile-by-tile) with smooth interpolation.
    /// Uses the new Input System package.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;

        [Header("Sprite Sheet")]
        [SerializeField] private Texture2D rogueSheet;

        [Header("References")]
        [SerializeField] private DungeonGenerator dungeonGenerator;
        [SerializeField] private EnemySpawner enemySpawner;

        private SpriteRenderer spriteRenderer;
        private Vector3 targetPosition;
        private bool isMoving;
        // Set to true the moment a scene change is triggered.
        // Prevents further input, movement or trigger checks while the scene is unloading.
        private bool isTransitioning;

        // Input System
        private PlayerInputActions inputActions;

        // Sprite position is now read from PlayerData (set by CharacterClassFactory)
        // Fallback: male fighter (row 1, col 1) in rogues.png
        private const int FallbackSpriteCol = 1;
        private const int FallbackSpriteRow = 1;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

            inputActions = new PlayerInputActions();

            // Subscribe in Awake() so we are GUARANTEED to be listening before
            // DungeonGenerator.Start() fires OnDungeonGenerated.
            // (All Awake() calls complete before any Start() runs in Unity.)
            GameEvents.OnDungeonGenerated += InitializePosition;
        }

        private void OnDestroy()
        {
            GameEvents.OnDungeonGenerated -= InitializePosition;
        }

        private void OnEnable()
        {
            inputActions.Player.Enable();
        }

        private void OnDisable()
        {
            inputActions.Player.Disable();
        }

        private void Start()
        {
            // Sprite is safe to initialize immediately.
            // Position is initialized via OnDungeonGenerated (subscribed in Awake),
            // which fires from DungeonGenerator.Start() after the map data is ready.
            InitializeSprite();
        }

        private void Update()
        {
            // Stop all logic once a scene transition has been requested
            if (isTransitioning) return;

            if (isMoving)
            {
                MoveTowardsTarget();
            }
            else
            {
                HandleInput();
            }
        }

        /// <summary>
        /// Loads the player sprite from the rogues sprite sheet.
        /// </summary>
        private void InitializeSprite()
        {
            // Read sprite position from the player's chosen class
            GameState state = GameManager.Instance?.CurrentGameState;
            int spriteCol = state?.Player.SpriteCol ?? FallbackSpriteCol;
            int spriteRow = state?.Player.SpriteRow ?? FallbackSpriteRow;
            // If the sprite faces LEFT by default, flip it so the player starts facing RIGHT
            bool facingLeft = state?.Player.FacingLeft ?? true;

            if (rogueSheet != null)
            {
                Sprite playerSprite = SpriteSheetHelper.ExtractSprite(rogueSheet, spriteCol, spriteRow);
                if (playerSprite != null)
                {
                    spriteRenderer.sprite = playerSprite;
                }
                else
                {
                    Debug.LogWarning($"PlayerController: Failed to extract sprite at col={spriteCol} row={spriteRow}, using fallback.");
                    CreateFallbackSprite();
                }
            }
            else
            {
                Debug.LogWarning("PlayerController: rogueSheet not assigned, using fallback sprite.");
                CreateFallbackSprite();
            }

            // Set initial facing: flip left-facing sprites so they look right when idle
            spriteRenderer.flipX = facingLeft;
            spriteRenderer.sortingOrder = 10; // Above tiles
        }

        private void CreateFallbackSprite()
        {
            Texture2D tex = new Texture2D(32, 32);
            tex.filterMode = FilterMode.Point;
            Color[] pixels = new Color[32 * 32];
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    if (x >= 10 && x <= 22 && y >= 2 && y <= 30)
                        pixels[y * 32 + x] = new Color(0.6f, 0.3f, 0.2f);
                    else if (x >= 8 && x <= 24 && y >= 20 && y <= 30)
                        pixels[y * 32 + x] = new Color(0.5f, 0.25f, 0.15f);
                    else
                        pixels[y * 32 + x] = Color.clear;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();

            spriteRenderer.sprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);
        }

        /// <summary>
        /// Called via GameEvents.OnDungeonGenerated — fires after DungeonGenerator has
        /// finished building the map and written valid EntranceX/Y and ExitX/Y coords.
        /// Never call this before the dungeon is generated.
        /// </summary>
        private void InitializePosition()
        {
            GameState state = GameManager.Instance?.CurrentGameState;
            if (state == null)
            {
                Debug.LogWarning("PlayerController.InitializePosition: GameState is null.");
                return;
            }

            SpawnPoint spawn = GameManager.Instance.NextSpawnPoint;

            // Consume the hint immediately — must happen before any early return
            GameManager.Instance.ResetSpawnPoint();

            float startX, startY;

            // For stair-based spawns we want to land BESIDE the stair (east preferred),
            // not ON it — otherwise the moment the scene becomes interactive the player
            // is standing on a staircase and any tiny input re-triggers the floor change.
            bool isStairSpawn = false;

            switch (spawn)
            {
                case SpawnPoint.AtEntrance:
                    // EntranceX/Y is the StairsUp TILE position.
                    startX = state.Dungeon.EntranceX;
                    startY = state.Dungeon.EntranceY;
                    isStairSpawn = true;
                    Debug.Log($"[Player] Spawn AtEntrance (stair tile) → ({startX}, {startY})");
                    break;

                case SpawnPoint.AtExit:
                    // ExitX/Y is the StairsDown TILE position.
                    startX = state.Dungeon.ExitX;
                    startY = state.Dungeon.ExitY;
                    isStairSpawn = true;
                    Debug.Log($"[Player] Spawn AtExit (stair tile) → ({startX}, {startY})");
                    break;

                default:
                    // New game or load: use saved position
                    startX = state.Player.PositionX;
                    startY = state.Player.PositionY;

                    // Guard against uninitialized / out-of-range saved positions
                    if (startX < 2f || startX >= state.Dungeon.Width  - 1) startX = state.Dungeon.EntranceX;
                    if (startY < 2f || startY >= state.Dungeon.Height - 1) startY = state.Dungeon.EntranceY;
                    break;
            }

            Vector2 finalPos;
            if (isStairSpawn)
            {
                // Prefer East, then North, West, South — any non-stair Floor tile beside the stair
                finalPos = FindAdjacentSpawn(state.Dungeon,
                    Mathf.RoundToInt(startX), Mathf.RoundToInt(startY));
                Debug.Log($"[Player] Spawn offset from stair → ({finalPos.x},{finalPos.y})");
            }
            else
            {
                // Safety check: make sure we're not spawning inside a wall
                finalPos = FindValidSpawn(state.Dungeon, startX, startY);
                if (finalPos.x != startX || finalPos.y != startY)
                    Debug.LogWarning($"[Player] Spawn ({startX},{startY}) was a wall — moved to ({finalPos.x},{finalPos.y})");
            }

            transform.position = new Vector3(finalPos.x, finalPos.y, 0f);
            targetPosition     = transform.position;
            isTransitioning    = false;
        }

        /// <summary>
        /// Finds a non-stair Floor tile immediately adjacent to (stairX, stairY).
        /// Preference: East, North, West, South. Falls back to FindValidSpawn
        /// (spiral search) if no adjacent non-stair Floor tile exists.
        /// </summary>
        private Vector2 FindAdjacentSpawn(Core.DungeonData dungeon, int stairX, int stairY)
        {
            // East first — matches the user's requested spawn direction.
            // Order matches standard dungeon-crawler preference where you tend to
            // explore rightward after descending.
            (int dx, int dy)[] dirs = { (1, 0), (0, 1), (-1, 0), (0, -1) };
            foreach (var (dx, dy) in dirs)
            {
                int nx = stairX + dx, ny = stairY + dy;
                if (IsFloorOnly(dungeon, nx, ny))
                    return new Vector2(nx, ny);
            }
            // Nothing adjacent is plain Floor — last resort: spiral search from stair
            // (which will happily include stair tiles, but at least won't be in a wall).
            return FindValidSpawn(dungeon, stairX, stairY);
        }

        /// <summary>Returns true only for TileType.Floor — excludes StairsUp/StairsDown.</summary>
        private bool IsFloorOnly(Core.DungeonData dungeon, int x, int y)
        {
            if (x < 0 || x >= dungeon.Width || y < 0 || y >= dungeon.Height) return false;
            return (Dungeon.TileType)dungeon.GetTile(x, y) == Dungeon.TileType.Floor;
        }

        /// <summary>
        /// Validates a spawn position. If the tile at (x,y) is not walkable, spirals
        /// outward to find the nearest Floor tile. Prevents spawning inside walls
        /// after combat or floor transitions.
        /// </summary>
        private Vector2 FindValidSpawn(Core.DungeonData dungeon, float requestedX, float requestedY)
        {
            int ix = Mathf.RoundToInt(requestedX);
            int iy = Mathf.RoundToInt(requestedY);

            if (IsWalkable(dungeon, ix, iy))
                return new Vector2(ix, iy);

            // Spiral search outward up to radius 8
            for (int radius = 1; radius <= 8; radius++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        // Only process the outer ring of this radius
                        if (Mathf.Abs(dx) != radius && Mathf.Abs(dy) != radius) continue;
                        if (IsWalkable(dungeon, ix + dx, iy + dy))
                            return new Vector2(ix + dx, iy + dy);
                    }
                }
            }

            // Final fallback: entrance
            return new Vector2(dungeon.EntranceX, dungeon.EntranceY);
        }

        private bool IsWalkable(Core.DungeonData dungeon, int x, int y)
        {
            Dungeon.TileType t = (Dungeon.TileType)dungeon.GetTile(x, y);
            return t == Dungeon.TileType.Floor
                || t == Dungeon.TileType.StairsUp
                || t == Dungeon.TileType.StairsDown;
        }

        private void HandleInput()
        {
            // Read movement from new Input System
            Vector2 moveInput = inputActions.Player.Move.ReadValue<Vector2>();

            Vector2 direction = Vector2.zero;

            // Prioritize one direction at a time for grid movement
            if (moveInput.y > 0.5f)
                direction = Vector2.up;
            else if (moveInput.y < -0.5f)
                direction = Vector2.down;
            else if (moveInput.x < -0.5f)
                direction = Vector2.left;
            else if (moveInput.x > 0.5f)
                direction = Vector2.right;

            if (direction != Vector2.zero)
            {
                TryMove(direction);

                // Flip sprite based on horizontal movement direction.
                // XOR with FacingLeft: if sprite naturally faces left, invert the flip logic.
                if (direction.x != 0)
                {
                    bool facingLeft = GameManager.Instance?.CurrentGameState?.Player.FacingLeft ?? true;
                    // facingLeft=true: flipX=false when moving left (natural), flipX=true when moving right
                    // facingLeft=false: flipX=true when moving left, flipX=false when moving right
                    spriteRenderer.flipX = facingLeft ^ (direction.x < 0);
                }
            }

            // Quick save with F5 (using Keyboard directly)
            if (Keyboard.current != null && Keyboard.current.f5Key.wasPressedThisFrame)
            {
                SavePlayerPosition();
                GameManager.Instance?.SaveGame();
            }

            // DEBUG: Press 0 to instantly clear all enemies on the current floor
            if (Keyboard.current != null && Keyboard.current.digit0Key.wasPressedThisFrame)
            {
                var spawner = FindAnyObjectByType<Combat.EnemySpawner>(FindObjectsInactive.Include);
                if (spawner != null)
                {
                    spawner.DebugClearFloor();
                    Debug.Log("[PlayerController] DEBUG: Floor cleared via digit 0.");
                }
            }
        }

        private void TryMove(Vector2 direction)
        {
            Vector3 newTarget = transform.position + (Vector3)direction;

            if (dungeonGenerator != null && dungeonGenerator.IsWalkable(newTarget))
            {
                targetPosition = newTarget;
                isMoving = true;
            }
        }

        private void MoveTowardsTarget()
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
            {
                transform.position = targetPosition;
                isMoving = false;

                SavePlayerPosition();
                CheckForEnemyEncounter();
                // Guard: if encounter triggered a scene transition, skip further checks
                if (isTransitioning) return;
                CheckForStairs();
                if (isTransitioning) return;
                CheckForPuzzleSwitch();
                CheckForTrap();
            }
        }

        /// <summary>
        /// Checks if the player has stepped onto a staircase tile and triggers a floor transition.
        /// Called after each completed move step.
        /// </summary>
        private void CheckForStairs()
        {
            GameState state = GameManager.Instance?.CurrentGameState;
            if (state == null) return;

            int tx = Mathf.FloorToInt(transform.position.x + 0.5f);
            int ty = Mathf.FloorToInt(transform.position.y + 0.5f);
            TileType tileType = (TileType)state.Dungeon.GetTile(tx, ty);

            if (tileType == TileType.StairsDown)
            {
                Debug.Log("[PlayerController] StairsDown — descending to next floor.");
                isTransitioning = true;
                GameManager.Instance.GoToNextFloor();
            }
            else if (tileType == TileType.StairsUp)
            {
                Debug.Log("[PlayerController] StairsUp — ascending to previous floor.");
                isTransitioning = true;
                GameManager.Instance.GoToPreviousFloor();
            }
        }

        /// <summary>
        /// Checks if the player stepped on a PuzzleSwitch tile.
        /// Activates it — converts to PuzzleSwitchActive, fires event, shows progress notification.
        /// When all switches are activated, fires OnAllPuzzlesSolved which triggers stairs placement.
        /// </summary>
        private void CheckForPuzzleSwitch()
        {
            GameState state = GameManager.Instance?.CurrentGameState;
            if (state == null) return;

            int tx = Mathf.FloorToInt(transform.position.x + 0.5f);
            int ty = Mathf.FloorToInt(transform.position.y + 0.5f);
            TileType tileType = (TileType)state.Dungeon.GetTile(tx, ty);

            if (tileType != TileType.PuzzleSwitch) return;

            // Activate the switch — mark as active so it can't be re-triggered
            state.Dungeon.SetTile(tx, ty, (int)TileType.PuzzleSwitchActive);
            state.Dungeon.PuzzleSwitchesActivated++;

            // Re-render the tile with the "active" sprite
            if (dungeonGenerator != null)
                dungeonGenerator.RenderPuzzleSwitchActive(tx, ty);

            int activated = state.Dungeon.PuzzleSwitchesActivated;
            int total     = state.Dungeon.PuzzleSwitchesTotal;

            GameEvents.RaisePuzzleSwitchActivated(activated, total);
            GameEvents.RaiseNotification(PuzzleData.SwitchActivatedMessage(activated, total));
            Debug.Log($"[PlayerController] Puzzle switch activated at ({tx},{ty}) — {activated}/{total}");

            // Check if all switches are now active
            if (activated >= total)
            {
                Debug.Log("[PlayerController] All puzzle switches activated — raising OnAllPuzzlesSolved!");
                GameEvents.RaiseAllPuzzlesSolved();
            }
        }

        /// <summary>
        /// Checks if the player stepped on a Trap tile and applies its effect.
        /// The trap fires once — tile is converted to Floor after triggering.
        /// Trap type is determined deterministically from tile coordinates (see TrapData).
        /// </summary>
        private void CheckForTrap()
        {
            GameState state = GameManager.Instance?.CurrentGameState;
            if (state == null) return;

            int tx = Mathf.FloorToInt(transform.position.x + 0.5f);
            int ty = Mathf.FloorToInt(transform.position.y + 0.5f);
            TileType tileType = (TileType)state.Dungeon.GetTile(tx, ty);

            if (tileType != TileType.Trap) return;

            int floor = state.CurrentFloor;
            TrapType trap = TrapData.GetTrapType(tx, ty, floor);

            // Consume the trap — convert to normal floor so it only triggers once
            state.Dungeon.SetTile(tx, ty, (int)TileType.Floor);

            switch (trap)
            {
                case TrapType.Spikes:
                    state.Player.CurrentHP = Mathf.Max(0, state.Player.CurrentHP - TrapData.SpikesDamage);
                    GameEvents.RaisePlayerHealthChanged(state.Player.CurrentHP, state.Player.MaxHP);
                    GameEvents.RaiseNotification(TrapData.SpikesMessage);
                    Debug.Log($"[PlayerController] Trap: Spikes at ({tx},{ty}) — dealt {TrapData.SpikesDamage} dmg, HP now {state.Player.CurrentHP}");

                    // Check if player died to trap
                    if (state.Player.CurrentHP <= 0)
                    {
                        Debug.Log("[PlayerController] Player killed by trap — triggering death flow.");
                        GameManager.Instance.OnCombatLost();
                        return;
                    }
                    break;

                case TrapType.MagicDrain:
                    state.Player.Toxicity = Mathf.Min(state.Player.MaxToxicity,
                        state.Player.Toxicity + TrapData.MagicDrainToxicity);
                    GameEvents.RaiseToxicityChanged(state.Player.Toxicity);
                    GameEvents.RaiseNotification(TrapData.MagicDrainMessage);
                    Debug.Log($"[PlayerController] Trap: MagicDrain at ({tx},{ty}) — Tox now {state.Player.Toxicity}");
                    break;

                case TrapType.Teleport:
                    // Warp player back to floor entrance
                    float ex = state.Dungeon.EntranceX;
                    float ey = state.Dungeon.EntranceY + 1; // one tile south of stairs to avoid re-trigger
                    transform.position = new Vector3(ex, ey, transform.position.z);
                    targetPosition = transform.position;
                    SavePlayerPosition();
                    GameEvents.RaiseNotification(TrapData.TeleportMessage);
                    Debug.Log($"[PlayerController] Trap: Teleport at ({tx},{ty}) — warped to entrance ({ex},{ey})");
                    break;
            }

            GameEvents.RaiseTrapTriggered(tx, ty);
        }

        private void CheckForEnemyEncounter()
        {
            if (enemySpawner == null) return;

            EnemyData enemy = enemySpawner.GetEnemyAtPosition(transform.position);
            if (enemy != null && GameManager.Instance != null)
            {
                // Lock out all further input/checks immediately — the scene is about to change
                isTransitioning = true;
                int enemyId = enemySpawner.GetEnemyIdAtPosition(transform.position);
                // Do NOT remove the enemy here — it stays on the map with full HP.
                // Only mark it as defeated AFTER combat is won (see EnemySpawner.MarkDefeated).
                // On flee/loss the enemy naturally respawns when the Dungeon scene reloads.
                GameManager.Instance.StartCombat(enemy, transform.position, enemyId);
            }
        }

        private void SavePlayerPosition()
        {
            GameState state = GameManager.Instance?.CurrentGameState;
            if (state != null)
            {
                state.Player.PositionX = transform.position.x;
                state.Player.PositionY = transform.position.y;
            }
        }
    }
}
