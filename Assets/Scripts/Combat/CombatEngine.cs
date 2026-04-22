using System;
using System.Collections.Generic;
using LochyIGorzala.Core;
using LochyIGorzala.Enemies;
using LochyIGorzala.Items;

namespace LochyIGorzala.Combat
{
    /// <summary>
    /// Pure C# combat engine — no Unity dependencies.
    /// Manages turn-based combat flow between player and enemy.
    /// Communicates results via GameEvents (Observer pattern).
    ///
    /// v2 changes:
    ///  • Victory block rolls loot via LootSystem and fires OnLootDropped.
    ///  • Gold is credited to GameManager.CurrentGameState.GoldCoins.
    ///  • ItemBuff from consumables is tracked and applied for one enemy turn.
    ///  • Weapon DamageType is read from Inventory so equipped silver/holy/fire
    ///    weapons automatically trigger enemy weaknesses (System Słabości).
    /// </summary>
    public class CombatEngine
    {
        public PlayerData   Player { get; private set; }
        public EnemyData    Enemy  { get; private set; }
        public CombatPhase  Phase  { get; private set; }
        public int          TurnNumber       { get; private set; }
        public bool         IsPlayerDefending { get; private set; }
        public string       LastMessage      { get; private set; }

        private int _originalDefense;
        private int _originalAttack;

        /// <summary>Pending attack buff from a consumable used this turn.</summary>
        private int _pendingAttackBuff;

        /// <summary>Active status effects on the player (Poison, Burn, Slow).</summary>
        private List<StatusEffect> _activeEffects = new List<StatusEffect>();

        /// <summary>
        /// Runtime inventory — set by CombatUIController immediately after construction.
        /// When null the engine falls back to Physical damage type.
        /// </summary>
        public Inventory PlayerInventory { get; set; }

        // Available actions for the player
        public List<ICombatAction> AvailableActions { get; private set; }

        public CombatEngine(PlayerData player, EnemyData enemy)
        {
            Player = player;
            Enemy  = enemy;
            Phase  = CombatPhase.PlayerTurn;
            TurnNumber        = 1;
            IsPlayerDefending = false;
            LastMessage       = enemy.GetEncounterMessage();

            _originalDefense = player.Defense;
            _originalAttack  = player.Attack;
            _pendingAttackBuff = 0;

            // Reset AP at combat start
            Player.ActionPoints = Player.MaxActionPoints;

            // Slot 0: light attack, Slot 1: class special, 2-5: utility
            AvailableActions = new List<ICombatAction>
            {
                new LightAttackAction(),
                CreateClassSpecial(player.CharacterClass),
                new HealAction(),
                new DrinkBimberAction(),
                new DefendAction(),
                new FleeAction()
            };
        }

        // ─────────────────────────────────────────────────────────
        //  MAIN TURN LOOP
        // ─────────────────────────────────────────────────────────

        /// <summary>
        /// Executes a player action and processes the result.
        /// Returns the combined result of player action + enemy retaliation.
        /// </summary>
        public CombatTurnResult ExecutePlayerAction(ICombatAction action)
        {
            if (Phase != CombatPhase.PlayerTurn)
                return new CombatTurnResult("To nie jest tura gracza!", CombatPhase.PlayerTurn);

            // AP check (flee costs 0 AP)
            if (!(action is FleeAction) && action.ActionPointCost > Player.ActionPoints)
                return new CombatTurnResult(
                    $"Za mało Wigoru! Potrzebujesz {action.ActionPointCost} PA.",
                    CombatPhase.PlayerTurn);

            // Strip defend bonus from the previous turn
            if (IsPlayerDefending)
            {
                Player.Defense    = _originalDefense;
                IsPlayerDefending = false;
            }

            // Apply any pending attack buff from a consumed item last turn
            if (_pendingAttackBuff != 0)
            {
                Player.Attack     -= _pendingAttackBuff;
                _pendingAttackBuff = 0;
            }

            // ── Execute player action ─────────────────────────────
            CombatActionResult playerResult = action.Execute(Player, Enemy);
            Player.ActionPoints -= action.ActionPointCost;

            // ── Flee ──────────────────────────────────────────────
            if (action is FleeAction)
            {
                if (playerResult.FleeSuccessful)
                {
                    Phase = CombatPhase.Fled;
                    return new CombatTurnResult(playerResult.Message, CombatPhase.Fled);
                }
                // Failed flee falls through to enemy turn
            }

            // ── Defend ────────────────────────────────────────────
            if (action is DefendAction)
            {
                IsPlayerDefending = true;
                _originalDefense  = Player.Defense; // already boosted by DefendAction
            }

            // ── Item buff (UseItemInCombatAction) ─────────────────
            if (playerResult.AttackBuff != 0)
            {
                Player.Attack      += playerResult.AttackBuff;
                _pendingAttackBuff  = playerResult.AttackBuff; // strip next turn
            }

            // ── Victory check ─────────────────────────────────────
            if (!Enemy.IsAlive)
                return BuildVictoryResult(playerResult.Message);

            // ── Enemy turn ────────────────────────────────────────
            Phase = CombatPhase.EnemyTurn;

            // Tick active status effects at start of enemy turn
            string statusMsg = "";
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
            {
                statusMsg += "\n" + _activeEffects[i].Tick(Player);
                if (_activeEffects[i].IsExpired)
                    _activeEffects.RemoveAt(i);
            }

            // Check if status effects killed the player
            if (Player.CurrentHP <= 0)
            {
                Phase = CombatPhase.Defeat;
                return new CombatTurnResult(
                    playerResult.Message + statusMsg +
                    "\n\nGniewko pada na ziemię… Ciemność go pochłania.",
                    CombatPhase.Defeat);
            }

            // Delirius phase transition
            string phaseMsg = "";
            if (Enemy is Delirius delirius && delirius.PhaseTransitionJustOccurred)
            {
                delirius.ClearPhaseTransitionFlag();
                phaseMsg = "\n\n!! DELIRIUS PRZECHODZI W FAZĘ 2 !! Atak i Obrona rosną dramatycznie!";
            }

            int rawEnemyDmg       = Enemy.CalculateAttackDamage();
            int defReduction      = IsPlayerDefending ? Player.Defense : Player.Defense / 2;
            int damageAfterDefense = Math.Max(1, rawEnemyDmg - defReduction);

            Player.CurrentHP = Math.Max(0, Player.CurrentHP - damageAfterDefense);

            string enemyMsg = $"{Enemy.Name} atakuje! Zadaje {damageAfterDefense} obrażeń.{phaseMsg}";
            GameEvents.RaisePlayerDamaged(damageAfterDefense);
            GameEvents.RaiseDamageDealt(Enemy.Name, damageAfterDefense);

            // Try to apply status effect after enemy attack
            StatusEffect newEffect = Enemy.TryApplyStatusEffect();
            string effectMsg = "";
            if (newEffect != null)
            {
                // Don't stack same type — refresh duration instead
                _activeEffects.RemoveAll(e => e.Type == newEffect.Type);
                _activeEffects.Add(newEffect);
                effectMsg = $"\n{Enemy.Name} nakłada efekt: {newEffect.DisplayName}!";
            }

            // Toxicity tick — poison DoT every 2 turns if over 50 %
            string toxMsg = "";
            if (Player.Toxicity >= Player.MaxToxicity * 0.5f && TurnNumber % 2 == 0)
            {
                int toxDot = (int)(Player.MaxHP * 0.05f);
                Player.CurrentHP = Math.Max(0, Player.CurrentHP - toxDot);
                toxMsg = $"\nTOKSYCZNOŚĆ sączy się w żyły! -{toxDot} HP!";
                GameEvents.RaisePlayerHealthChanged(Player.CurrentHP, Player.MaxHP);
            }

            // ── Defeat check ──────────────────────────────────────
            if (Player.CurrentHP <= 0)
            {
                Phase = CombatPhase.Defeat;
                return new CombatTurnResult(
                    playerResult.Message + statusMsg + "\n" + enemyMsg + effectMsg + toxMsg +
                    "\n\nGniewko pada na ziemię… Ciemność go pochłania.",
                    CombatPhase.Defeat);
            }

            // ── Next player turn ──────────────────────────────────
            TurnNumber++;
            Player.ActionPoints = Player.MaxActionPoints;

            // Apply Slow: reduce AP by 1 if slowed
            bool isSlowed = _activeEffects.Exists(e => e.Type == StatusEffectType.Slow);
            if (isSlowed && Player.ActionPoints > 1)
                Player.ActionPoints -= 1;

            Phase = CombatPhase.PlayerTurn;

            GameEvents.RaisePlayerHealthChanged(Player.CurrentHP, Player.MaxHP);
            return new CombatTurnResult(
                playerResult.Message + statusMsg + "\n" + enemyMsg + effectMsg + toxMsg,
                CombatPhase.PlayerTurn);
        }

        // ─────────────────────────────────────────────────────────
        //  ITEM USE (from inventory during combat)
        // ─────────────────────────────────────────────────────────

        /// <summary>
        /// Uses a consumable from the player's inventory during combat (costs 1 AP).
        /// Called by CombatUIController when the player picks an item from the bag panel.
        /// </summary>
        public CombatTurnResult UseItemInCombat(string itemId)
        {
            if (Phase != CombatPhase.PlayerTurn)
                return new CombatTurnResult("To nie jest tura gracza!", CombatPhase.PlayerTurn);

            const int apCost = 1;
            if (Player.ActionPoints < apCost)
                return new CombatTurnResult("Za mało Wigoru! (1 PA)", CombatPhase.PlayerTurn);

            if (PlayerInventory == null)
                return new CombatTurnResult("Plecak niedostępny.", CombatPhase.PlayerTurn);

            CombatItemResult itemResult = PlayerInventory.UseConsumable(itemId);
            if (!itemResult.Success)
                return new CombatTurnResult(itemResult.Message, CombatPhase.PlayerTurn);

            Player.ActionPoints -= apCost;

            // Queue attack buff for this turn's damage calculation
            if (itemResult.AttackBuff > 0)
            {
                Player.Attack      += itemResult.AttackBuff;
                _pendingAttackBuff  = itemResult.AttackBuff;
            }

            // End turn if AP exhausted — force enemy turn without recursion
            if (Player.ActionPoints <= 0)
            {
                Player.ActionPoints = 0;
                Phase = CombatPhase.EnemyTurn;

                int rawDmg  = Enemy.CalculateAttackDamage();
                int netDmg  = Math.Max(1, rawDmg - Player.Defense / 2);
                Player.CurrentHP = Math.Max(0, Player.CurrentHP - netDmg);
                GameEvents.RaisePlayerDamaged(netDmg);

                string forcedMsg = itemResult.Message +
                    $"\n{Enemy.Name} korzysta z chwili! Zadaje {netDmg} obrażeń.";

                if (Player.CurrentHP <= 0)
                {
                    Phase = CombatPhase.Defeat;
                    return new CombatTurnResult(forcedMsg + "\n\nGniewko pada...", CombatPhase.Defeat);
                }

                TurnNumber++;
                Player.ActionPoints = Player.MaxActionPoints;
                Phase = CombatPhase.PlayerTurn;
                GameEvents.RaisePlayerHealthChanged(Player.CurrentHP, Player.MaxHP);
                return new CombatTurnResult(forcedMsg, CombatPhase.PlayerTurn);
            }

            GameEvents.RaisePlayerHealthChanged(Player.CurrentHP, Player.MaxHP);
            return new CombatTurnResult(itemResult.Message, CombatPhase.PlayerTurn);
        }

        // ─────────────────────────────────────────────────────────
        //  WEAPON DAMAGE TYPE (System Słabości integration)
        // ─────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the DamageType of the player's equipped weapon.
        /// CombatAction implementations call this via the engine reference
        /// so attacks automatically use the correct type.
        /// </summary>
        public DamageType GetEquippedWeaponDamageType()
        {
            return PlayerInventory?.GetWeaponDamageType() ?? DamageType.Physical;
        }

        // ─────────────────────────────────────────────────────────
        //  CLEANUP
        // ─────────────────────────────────────────────────────────

        public void EndCombat()
        {
            // Bimber attack boost: keep at most +1 permanently (flavour progression)
            if (Player.Attack > _originalAttack)
            {
                int kept  = Math.Min(1, Player.Attack - _originalAttack);
                Player.Attack = _originalAttack + kept;
            }

            // Strip pending buff if EndCombat is called before next turn
            if (_pendingAttackBuff != 0)
            {
                Player.Attack     -= _pendingAttackBuff;
                _pendingAttackBuff = 0;
            }

            if (IsPlayerDefending)
                Player.Defense = _originalDefense;

            // Clear all status effects
            _activeEffects.Clear();
        }

        // ─────────────────────────────────────────────────────────
        //  PRIVATE HELPERS
        // ─────────────────────────────────────────────────────────

        private CombatTurnResult BuildVictoryResult(string actionMsg)
        {
            Phase = CombatPhase.Victory;

            int xpGain   = Enemy.ExperienceReward;
            int goldGain = Enemy.GoldReward;
            Player.Experience += xpGain;
            GameEvents.RaisePlayerXPGained(xpGain);

            string victoryMsg = actionMsg +
                $"\n\n{Enemy.Name} pada martwy!" +
                $"\n+{xpGain} XP  |  +{goldGain} złota!";

            // Level up
            if (Player.Experience >= Player.ExperienceToNextLevel)
            {
                Player.Level++;
                Player.Experience        -= Player.ExperienceToNextLevel;
                Player.ExperienceToNextLevel = (int)(Player.ExperienceToNextLevel * 1.5f);
                Player.MaxHP  += 10;
                Player.CurrentHP = Player.MaxHP;
                Player.Attack += 2;
                Player.Defense += 1;
                victoryMsg += $"\n\n[POZIOM {Player.Level}!] HP, Atak i Obrona rosną!";
                GameEvents.RaisePlayerLevelUp(Player.Level);
            }

            // Loot roll
            int floor = Managers.GameManager.Instance?.CurrentGameState?.CurrentFloor ?? 1;
            ItemData loot = LootSystem.RollLoot(Enemy, floor);
            string lootMsg = "";
            if (loot != null)
            {
                lootMsg = $"\n\n[LOOT] Znaleziono: {loot.DisplayName()} " +
                          $"{LootSystem.FormatRarityTag(loot.Rarity)}";
                GameEvents.RaiseLootDropped(loot);   // UI subscribes to show pickup panel
            }

            // Note: gold is credited by CombatUIController after this result is returned.
            // Do NOT call RaiseGoldChanged here — that would double-add gold via
            // GameManager.HandleGoldGained. CombatUIController is the single source of truth.

            return new CombatTurnResult(victoryMsg + lootMsg, CombatPhase.Victory)
            {
                GoldGained = goldGain,
                XPGained   = xpGain,
                LootItem   = loot
            };
        }

        private ICombatAction CreateClassSpecial(string characterClass)
        {
            switch (characterClass)
            {
                case "Lucznik": return new LucznikSpecialAction();
                case "Mag":     return new MagSpecialAction();
                default:        return new WojownikSpecialAction();
            }
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  ENUMS & RESULT TYPES
    // ─────────────────────────────────────────────────────────────

    public enum CombatPhase
    {
        PlayerTurn,
        EnemyTurn,
        Victory,
        Defeat,
        Fled
    }

    /// <summary>
    /// Complete result of a combat turn (player action + enemy response).
    /// Extended with LootItem for the victory case.
    /// </summary>
    public class CombatTurnResult
    {
        public string      Message;
        public CombatPhase ResultingPhase;
        public int         GoldGained;
        public int         XPGained;
        /// <summary>Non-null only when ResultingPhase == Victory and loot dropped.</summary>
        public Items.ItemData LootItem;

        public CombatTurnResult(string message, CombatPhase phase)
        {
            Message        = message;
            ResultingPhase = phase;
        }
    }

}
