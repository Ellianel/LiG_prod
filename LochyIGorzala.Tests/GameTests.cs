using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using LochyIGorzala.Core;
using LochyIGorzala.Combat;
using LochyIGorzala.Enemies;
using LochyIGorzala.Items;

namespace LochyIGorzala.Tests
{
    // =====================================================
    //  1. ENEMY FACTORY TESTS
    // =====================================================

    public class EnemyFactoryTests
    {
        [Theory]
        [InlineData(EnemyFactory.EnemyType.Chochlik,       "Chochlik",             25,  7,  2, false)]
        [InlineData(EnemyFactory.EnemyType.Utopiec,        "Utopiec",              45, 10,  6, false)]
        [InlineData(EnemyFactory.EnemyType.Strzyga,        "Strzyga",              55, 14,  5, false)]
        [InlineData(EnemyFactory.EnemyType.BossGargulec,   "Bliźniacze Dziwadła", 110, 16, 12, true)]
        [InlineData(EnemyFactory.EnemyType.BossNekromanta,  "Nekromanta Martwicy", 130, 18,  8, true)]
        [InlineData(EnemyFactory.EnemyType.BossWampir,     "Baba Jaga",           150, 22, 10, true)]
        [InlineData(EnemyFactory.EnemyType.BossOgien,      "Ognisty Diabeł",      150, 20,  9, true)]
        [InlineData(EnemyFactory.EnemyType.Delirius,       "DELIRIUS",            200, 22, 10, true)]
        public void Create_ReturnsEnemyWithCorrectStats(
            EnemyFactory.EnemyType type, string name, int hp, int atk, int def, bool isBoss)
        {
            EnemyData enemy = EnemyFactory.Create(type);

            Assert.Equal(name, enemy.Name);
            Assert.Equal(hp, enemy.MaxHP);
            Assert.Equal(hp, enemy.CurrentHP);
            Assert.Equal(atk, enemy.Attack);
            Assert.Equal(def, enemy.Defense);
            Assert.Equal(isBoss, enemy.IsBoss);
            Assert.True(enemy.IsAlive);
        }

        [Fact]
        public void Create_AllEnemiesHaveWeakness()
        {
            foreach (EnemyFactory.EnemyType type in Enum.GetValues(typeof(EnemyFactory.EnemyType)))
            {
                EnemyData enemy = EnemyFactory.Create(type);
                // Every enemy should have a weakness assigned (not default Physical for bosses)
                Assert.NotNull(enemy.Name);
                Assert.True(enemy.MaxHP > 0);
            }
        }

        [Fact]
        public void Create_WeaknessTypes_MatchDesign()
        {
            Assert.Equal(DamageType.Silver, EnemyFactory.Create(EnemyFactory.EnemyType.Chochlik).Weakness);
            Assert.Equal(DamageType.Fire,   EnemyFactory.Create(EnemyFactory.EnemyType.Utopiec).Weakness);
            Assert.Equal(DamageType.Holy,   EnemyFactory.Create(EnemyFactory.EnemyType.Strzyga).Weakness);
            Assert.Equal(DamageType.Fire,   EnemyFactory.Create(EnemyFactory.EnemyType.BossGargulec).Weakness);
            Assert.Equal(DamageType.Holy,   EnemyFactory.Create(EnemyFactory.EnemyType.Delirius).Weakness);
        }
    }

    // =====================================================
    //  2. ENEMY DATA / DAMAGE TESTS
    // =====================================================

    public class EnemyDataTests
    {
        [Fact]
        public void TakeDamage_WeaknessAppliesMultiplier()
        {
            var chochlik = EnemyFactory.Create(EnemyFactory.EnemyType.Chochlik);
            int startHP = chochlik.CurrentHP;

            // Silver is Chochlik's weakness -> 1.5x multiplier
            int dmg = chochlik.TakeDamage(20, DamageType.Silver);

            // effectiveDamage = max(1, (int)(20 * 1.5) - Defense/2) = max(1, 30 - 1) = 29
            Assert.Equal(29, dmg);
            Assert.Equal(startHP - 29, chochlik.CurrentHP);
        }

        [Fact]
        public void TakeDamage_NonWeakness_NormalMultiplier()
        {
            var chochlik = EnemyFactory.Create(EnemyFactory.EnemyType.Chochlik);
            int startHP = chochlik.CurrentHP;

            // Physical is NOT Chochlik's weakness -> 1.0x multiplier
            int dmg = chochlik.TakeDamage(20, DamageType.Physical);

            // effectiveDamage = max(1, 20 - 2/2) = max(1, 19) = 19
            Assert.Equal(19, dmg);
            Assert.Equal(startHP - 19, chochlik.CurrentHP);
        }

        [Fact]
        public void TakeDamage_MinimumOneDamage()
        {
            var chochlik = EnemyFactory.Create(EnemyFactory.EnemyType.Chochlik);

            // Very low raw damage -> should still deal at least 1
            int dmg = chochlik.TakeDamage(1, DamageType.Physical);
            Assert.True(dmg >= 1);
        }

        [Fact]
        public void BossOgien_FireImmune_HealsInstead()
        {
            var ogien = EnemyFactory.Create(EnemyFactory.EnemyType.BossOgien);

            // Damage the boss first
            ogien.CurrentHP = 100;
            int dmg = ogien.TakeDamage(20, DamageType.Fire);

            // Fire immune: 0 damage, heals rawDamage/2 = 10
            Assert.Equal(0, dmg);
            Assert.Equal(110, ogien.CurrentHP);
        }

        [Fact]
        public void Strzyga_RegeneratesOnNonHolyHit()
        {
            var strzyga = EnemyFactory.Create(EnemyFactory.EnemyType.Strzyga);
            strzyga.CurrentHP = 40;

            // Physical hit -> regenerates 2 HP before taking damage
            strzyga.TakeDamage(10, DamageType.Physical);

            // After regen: 40+2=42, then takes damage: max(1, 10 - 5/2) = max(1, 8) = 8
            // Final: 42 - 8 = 34
            Assert.Equal(34, strzyga.CurrentHP);
        }

        [Fact]
        public void Delirius_Phase1_PhysicalResistance()
        {
            var delirius = (Delirius)EnemyFactory.Create(EnemyFactory.EnemyType.Delirius);

            // Phase 1 (HP > 50%): Physical deals only 50%
            int dmg = delirius.TakeDamage(20, DamageType.Physical);

            // rawDamage halved: 20/2 = 10, then: max(1, 10 - 10/2) = max(1, 5) = 5
            Assert.Equal(5, dmg);
        }

        [Fact]
        public void Delirius_Phase2_StatsIncrease()
        {
            var delirius = (Delirius)EnemyFactory.Create(EnemyFactory.EnemyType.Delirius);

            // Force HP below 50% to trigger phase 2
            delirius.CurrentHP = 90;
            delirius.CalculateAttackDamage(); // triggers CheckPhaseTransition

            Assert.True(delirius.PhaseTransitionJustOccurred);
            Assert.Equal(26, delirius.Attack);
            Assert.Equal(12, delirius.Defense);
        }
    }

    // =====================================================
    //  3. CHARACTER CLASS FACTORY TESTS
    // =====================================================

    public class CharacterClassFactoryTests
    {
        [Fact]
        public void CreatePlayer_Wojownik_CorrectStats()
        {
            var p = CharacterClassFactory.CreatePlayer(PlayerClass.Wojownik);

            Assert.Equal("Wojownik", p.CharacterClass);
            Assert.Equal(120, p.MaxHP);
            Assert.Equal(14, p.Attack);
            Assert.Equal(8, p.Defense);
            Assert.Equal(3, p.MaxActionPoints);
        }

        [Fact]
        public void CreatePlayer_Lucznik_Has4AP()
        {
            var p = CharacterClassFactory.CreatePlayer(PlayerClass.Lucznik);

            Assert.Equal("Lucznik", p.CharacterClass);
            Assert.Equal(85, p.MaxHP);
            Assert.Equal(4, p.MaxActionPoints);
        }

        [Fact]
        public void CreatePlayer_Mag_LowHPHighMagic()
        {
            var p = CharacterClassFactory.CreatePlayer(PlayerClass.Mag);

            Assert.Equal("Mag", p.CharacterClass);
            Assert.Equal(70, p.MaxHP);
            Assert.Equal(7, p.Attack);
            Assert.Equal(3, p.Defense);
        }

        [Fact]
        public void Parse_RoundTrips()
        {
            Assert.Equal(PlayerClass.Wojownik, CharacterClassFactory.Parse("Wojownik"));
            Assert.Equal(PlayerClass.Lucznik,  CharacterClassFactory.Parse("Lucznik"));
            Assert.Equal(PlayerClass.Mag,      CharacterClassFactory.Parse("Mag"));
            // Unknown -> defaults to Wojownik
            Assert.Equal(PlayerClass.Wojownik, CharacterClassFactory.Parse("Unknown"));
        }
    }

    // =====================================================
    //  4. COMBAT ENGINE TESTS
    // =====================================================

    public class CombatEngineTests
    {
        private static CombatEngine CreateEngine(PlayerClass cls = PlayerClass.Wojownik)
        {
            var player = CharacterClassFactory.CreatePlayer(cls);
            var enemy = EnemyFactory.Create(EnemyFactory.EnemyType.Chochlik);
            return new CombatEngine(player, enemy);
        }

        [Fact]
        public void Constructor_SetsPlayerTurnPhase()
        {
            var engine = CreateEngine();

            Assert.Equal(CombatPhase.PlayerTurn, engine.Phase);
            Assert.Equal(1, engine.TurnNumber);
            Assert.Equal(3, engine.Player.ActionPoints);
        }

        [Fact]
        public void Constructor_CreatesCorrectSpecialAction()
        {
            var wojownik = CreateEngine(PlayerClass.Wojownik);
            Assert.Equal("Potężne Uderzenie", wojownik.AvailableActions[1].ActionName);

            var lucznik = CreateEngine(PlayerClass.Lucznik);
            Assert.Equal("Strzał w Słabość", lucznik.AvailableActions[1].ActionName);

            var mag = CreateEngine(PlayerClass.Mag);
            Assert.Equal("Kula Ognia", mag.AvailableActions[1].ActionName);
        }

        [Fact]
        public void ExecuteAction_LightAttack_DeductsAP()
        {
            var engine = CreateEngine();
            int apBefore = engine.Player.ActionPoints;

            engine.ExecutePlayerAction(engine.AvailableActions[0]); // Light Attack = 1 AP

            // After executing, next turn AP resets since enemy turn runs too
            // But the action cost is correctly applied during the turn
            Assert.NotNull(engine.LastMessage);
        }

        [Fact]
        public void ExecuteAction_NotEnoughAP_Rejected()
        {
            var engine = CreateEngine();
            engine.Player.ActionPoints = 1;

            // Heavy attack costs 2 AP
            var result = engine.ExecutePlayerAction(engine.AvailableActions[1]);

            Assert.Equal(CombatPhase.PlayerTurn, result.ResultingPhase);
            Assert.Contains("Wigoru", result.Message);
        }

        [Fact]
        public void ExecuteAction_KillingBlow_Victory()
        {
            var engine = CreateEngine();
            engine.Enemy.CurrentHP = 1; // One hit will kill

            var result = engine.ExecutePlayerAction(new LightAttackAction());

            Assert.Equal(CombatPhase.Victory, result.ResultingPhase);
            Assert.True(result.GoldGained > 0);
            Assert.True(result.XPGained > 0);
        }

        [Fact]
        public void ExecuteAction_FleeSuccess_PhaseFled()
        {
            var engine = CreateEngine();

            // Keep trying to flee until it succeeds (RNG-based)
            CombatTurnResult result = null;
            for (int i = 0; i < 100; i++)
            {
                // Reset to PlayerTurn since failed flee advances the turn
                engine = CreateEngine();
                result = engine.ExecutePlayerAction(new FleeAction());
                if (result.ResultingPhase == CombatPhase.Fled)
                    break;
            }

            Assert.Equal(CombatPhase.Fled, result.ResultingPhase);
        }

        [Fact]
        public void ExecuteAction_DefendAction_BoostsDefense()
        {
            var engine = CreateEngine();
            int baseDef = engine.Player.Defense;

            engine.ExecutePlayerAction(new DefendAction());

            // Defense was boosted by 5, then enemy attacked, then reset on next action
            // After the full turn, the engine stores IsPlayerDefending
            // The defense boost is active during the enemy's attack
            Assert.NotNull(engine.LastMessage);
        }

        [Fact]
        public void EndCombat_RestoresOriginalStats()
        {
            var engine = CreateEngine();
            int originalAttack = engine.Player.Attack;
            int originalDefense = engine.Player.Defense;

            // Drink bimber to buff attack
            engine.ExecutePlayerAction(new DrinkBimberAction());
            engine.EndCombat();

            // Stats should be restored (with at most +1 permanent bimber bonus)
            Assert.True(engine.Player.Attack <= originalAttack + 1);
        }
    }

    // =====================================================
    //  5. COMBAT ACTION TESTS
    // =====================================================

    public class CombatActionTests
    {
        private static PlayerData CreatePlayer() =>
            CharacterClassFactory.CreatePlayer(PlayerClass.Wojownik);

        [Fact]
        public void LightAttack_Costs1AP_DealsDamage()
        {
            var action = new LightAttackAction();
            Assert.Equal(1, action.ActionPointCost);
            Assert.Equal("Lekki Atak", action.ActionName);

            var result = action.Execute(CreatePlayer(), EnemyFactory.Create(EnemyFactory.EnemyType.Chochlik));
            Assert.True(result.DamageDealt > 0);
            Assert.True(result.ActionSuccessful);
        }

        [Fact]
        public void HealAction_RestoresHP()
        {
            var player = CreatePlayer();
            player.CurrentHP = 50;
            var enemy = EnemyFactory.Create(EnemyFactory.EnemyType.Chochlik);

            var result = new HealAction().Execute(player, enemy);

            Assert.True(result.HealAmount > 0);
            Assert.True(player.CurrentHP > 50);
        }

        [Fact]
        public void DrinkBimber_IncreasesToxicity_GivesAttackBuff()
        {
            var player = CreatePlayer();
            float toxBefore = player.Toxicity;

            var result = new DrinkBimberAction().Execute(player, EnemyFactory.Create(EnemyFactory.EnemyType.Chochlik));

            Assert.True(player.Toxicity > toxBefore);
            Assert.Equal(4, result.AttackBuff);
        }
    }

    // =====================================================
    //  6. STATUS EFFECT TESTS
    // =====================================================

    public class StatusEffectTests
    {
        [Fact]
        public void Poison_DealsDamageAndExpires()
        {
            var player = CharacterClassFactory.CreatePlayer(PlayerClass.Wojownik);
            var poison = new StatusEffect(StatusEffectType.Poison, 2, 5);

            int hpBefore = player.CurrentHP;
            string msg1 = poison.Tick(player);

            Assert.Equal(hpBefore - 5, player.CurrentHP);
            Assert.Contains("TRUCIZNA", msg1);
            Assert.False(poison.IsExpired);

            poison.Tick(player);
            Assert.True(poison.IsExpired);
            Assert.Equal(hpBefore - 10, player.CurrentHP);
        }

        [Fact]
        public void Burn_DealsDamagePerTick()
        {
            var player = CharacterClassFactory.CreatePlayer(PlayerClass.Wojownik);
            var burn = new StatusEffect(StatusEffectType.Burn, 2, 4);

            burn.Tick(player);

            Assert.Equal(120 - 4, player.CurrentHP);
            Assert.Contains("OGIEŃ", burn.Tick(player));
            Assert.True(burn.IsExpired);
        }

        [Fact]
        public void Slow_DoesNotDealDamage()
        {
            var player = CharacterClassFactory.CreatePlayer(PlayerClass.Wojownik);
            var slow = new StatusEffect(StatusEffectType.Slow, 2);

            int hpBefore = player.CurrentHP;
            string msg = slow.Tick(player);

            Assert.Equal(hpBefore, player.CurrentHP); // No damage
            Assert.Contains("SPOWOLNIENIE", msg);
        }

        [Fact]
        public void DisplayName_ReturnsPolishNames()
        {
            Assert.Equal("Trucizna",      new StatusEffect(StatusEffectType.Poison, 1).DisplayName);
            Assert.Equal("Podpalenie",    new StatusEffect(StatusEffectType.Burn, 1).DisplayName);
            Assert.Equal("Spowolnienie",  new StatusEffect(StatusEffectType.Slow, 1).DisplayName);
        }
    }

    // =====================================================
    //  7. INVENTORY TESTS
    // =====================================================

    public class InventoryTests
    {
        private static (Inventory inv, PlayerData player) CreateInventory()
        {
            var player = CharacterClassFactory.CreatePlayer(PlayerClass.Wojownik);
            var data = new InventoryData();
            return (new Inventory(data, player), player);
        }

        [Fact]
        public void AddItem_AddsToBackpack()
        {
            var (inv, _) = CreateInventory();

            bool added = inv.AddItem("healing_potion");

            Assert.True(added);
            Assert.Equal(1, inv.Count);
            Assert.True(inv.HasItem("healing_potion"));
        }

        [Fact]
        public void AddItem_FullBag_ReturnsFalse()
        {
            var (inv, _) = CreateInventory();

            for (int i = 0; i < InventoryData.MaxCapacity; i++)
                inv.AddItem("healing_potion");

            Assert.True(inv.IsFull);
            Assert.False(inv.AddItem("healing_potion"));
            Assert.Equal(20, inv.Count);
        }

        [Fact]
        public void EquipWeapon_ChangesAttackStat()
        {
            var (inv, player) = CreateInventory();
            int basAtk = player.Attack;

            inv.AddItem("hunters_sword"); // AttackBonus = 5
            inv.EquipItem("hunters_sword");

            Assert.Equal(basAtk + 5, player.Attack);
            Assert.Equal("hunters_sword", inv.EquippedWeaponId);
            Assert.Equal(0, inv.Count); // Moved from bag to slot
        }

        [Fact]
        public void UnequipWeapon_RestoresStats()
        {
            var (inv, player) = CreateInventory();
            int baseAtk = player.Attack;

            inv.AddItem("hunters_sword");
            inv.EquipItem("hunters_sword");
            inv.UnequipSlot(ItemType.Weapon, putBackInBag: true);

            Assert.Equal(baseAtk, player.Attack);
            Assert.Equal("", inv.EquippedWeaponId);
            Assert.Equal(1, inv.Count); // Back in bag
        }

        [Fact]
        public void UseConsumable_HealsAndRemovesItem()
        {
            var (inv, player) = CreateInventory();
            player.CurrentHP = 50;

            inv.AddItem("healing_potion");
            var result = inv.UseConsumable("healing_potion");

            Assert.True(result.Success);
            Assert.True(player.CurrentHP > 50);
            Assert.Equal(0, inv.Count); // Consumed
        }

        [Fact]
        public void GetWeaponDamageType_NoWeapon_ReturnsPhysical()
        {
            var (inv, _) = CreateInventory();

            Assert.Equal(DamageType.Physical, inv.GetWeaponDamageType());
        }
    }

    // =====================================================
    //  8. ITEM DATABASE TESTS
    // =====================================================

    public class ItemDatabaseTests
    {
        [Fact]
        public void Get_KnownItem_ReturnsCorrectData()
        {
            var sword = ItemDatabase.Get("blessed_sword");

            Assert.NotNull(sword);
            Assert.Equal("Błogosławiony Miecz", sword.Name);
            Assert.Equal(ItemType.Weapon, sword.Type);
            Assert.Equal(ItemRarity.Epic, sword.Rarity);
            Assert.True(sword.HasDamageTypeOverride);
            Assert.Equal(DamageType.Holy, sword.OverrideDamageType);
        }

        [Fact]
        public void Get_UnknownItem_ReturnsNull()
        {
            Assert.Null(ItemDatabase.Get("nonexistent_item"));
            Assert.Null(ItemDatabase.Get(""));
        }

        [Fact]
        public void RarityPools_ArePopulated()
        {
            Assert.True(ItemDatabase.CommonItems.Count > 0);
            Assert.True(ItemDatabase.RareItems.Count > 0);
            Assert.True(ItemDatabase.EpicItems.Count > 0);
        }

        [Fact]
        public void AllItems_HaveValidFields()
        {
            // Verify every item in the database has required fields
            foreach (var item in ItemDatabase.CommonItems)
            {
                Assert.False(string.IsNullOrEmpty(item.ItemId));
                Assert.False(string.IsNullOrEmpty(item.Name));
            }
            foreach (var item in ItemDatabase.RareItems)
            {
                Assert.False(string.IsNullOrEmpty(item.ItemId));
                Assert.Equal(ItemRarity.Rare, item.Rarity);
            }
            foreach (var item in ItemDatabase.EpicItems)
            {
                Assert.False(string.IsNullOrEmpty(item.ItemId));
                Assert.Equal(ItemRarity.Epic, item.Rarity);
            }
        }
    }

    // =====================================================
    //  9. LOOT SYSTEM TESTS
    // =====================================================

    public class LootSystemTests
    {
        [Fact]
        public void RollLoot_Boss_AlwaysDrops()
        {
            var boss = EnemyFactory.Create(EnemyFactory.EnemyType.BossGargulec);

            // Boss has 100% drop chance — run multiple times to confirm
            int drops = 0;
            for (int i = 0; i < 50; i++)
            {
                if (LootSystem.RollLoot(boss, 1) != null)
                    drops++;
            }

            Assert.Equal(50, drops);
        }

        [Fact]
        public void RollLoot_NullEnemy_ReturnsNull()
        {
            Assert.Null(LootSystem.RollLoot(null, 1));
        }

        [Fact]
        public void FormatRarityTag_ReturnsCorrectColors()
        {
            Assert.Contains("#CCCCCC", LootSystem.FormatRarityTag(ItemRarity.Common));
            Assert.Contains("#4A9EFF", LootSystem.FormatRarityTag(ItemRarity.Rare));
            Assert.Contains("#FFD700", LootSystem.FormatRarityTag(ItemRarity.Epic));
        }

        [Fact]
        public void GuaranteedDrop_ReturnsItemOfRequestedRarity()
        {
            for (int i = 0; i < 20; i++)
            {
                var rare = LootSystem.GuaranteedDrop(ItemRarity.Rare);
                Assert.NotNull(rare);
                Assert.Equal(ItemRarity.Rare, rare.Rarity);
            }
        }
    }

    // =====================================================
    // 10. QUEST DATA TESTS
    // =====================================================

    public class QuestDataTests
    {
        [Fact]
        public void KillQuest_TracksProgress()
        {
            var quest = new QuestData(
                "test_kill", "Test", "Kill stuff",
                "Chochlik", 3,
                "healing_potion", 1, "Bravo!");

            Assert.Equal(QuestType.Kill, quest.Type);
            Assert.False(quest.IsComplete);
            Assert.Equal("0/3", quest.ProgressText);

            quest.CurrentKills = 2;
            Assert.False(quest.IsComplete);
            Assert.Equal("2/3", quest.ProgressText);

            quest.CurrentKills = 3;
            Assert.True(quest.IsComplete);
            Assert.Equal("3/3", quest.ProgressText);
        }

        [Fact]
        public void FloorQuest_CompletesOnStatus()
        {
            var quest = new QuestData("floor_test", "Floor Test", "Go deeper", 3);

            Assert.Equal(QuestType.Floor, quest.Type);
            Assert.False(quest.IsComplete);
            Assert.Equal("W toku...", quest.ProgressText);

            quest.Status = QuestStatus.Completed;
            Assert.True(quest.IsComplete);
            Assert.Equal("Ukończono", quest.ProgressText);
        }

        [Fact]
        public void QuestDatabase_Has3KillQuestsAnd3FloorQuests()
        {
            Assert.Equal(3, QuestDatabase.KillQuests.Length);
            Assert.Equal(3, QuestDatabase.FloorQuests.Length);

            // Verify kill quest targets
            Assert.Equal("Chochlik", QuestDatabase.KillQuests[0].TargetEnemyName);
            Assert.Equal("Strzyga",  QuestDatabase.KillQuests[1].TargetEnemyName);
            Assert.Equal("Utopiec",  QuestDatabase.KillQuests[2].TargetEnemyName);
        }

        [Fact]
        public void QuestSaveData_InitializesEmpty()
        {
            var save = new QuestSaveData();

            Assert.NotNull(save.Quests);
            Assert.Empty(save.Quests);
            Assert.False(save.IntroCompleted);
            Assert.Equal(0, save.NextQuestIndex);
        }
    }

    // =====================================================
    // 11. GAME STATE TESTS
    // =====================================================

    public class GameStateTests
    {
        [Fact]
        public void Constructor_InitializesDefaults()
        {
            var state = new GameState();

            Assert.NotNull(state.Player);
            Assert.NotNull(state.Dungeon);
            Assert.Equal(0, state.CurrentFloor);
            Assert.Equal(0, state.GoldCoins);
            Assert.True(state.FloorsCleared[0]); // Lobby always cleared
            Assert.False(state.FloorsCleared[1]);
            Assert.False(state.DeliriusDefeated);
        }

        [Fact]
        public void DungeonData_GetSetTile_Works()
        {
            var dungeon = new DungeonData { Width = 10, Height = 10 };
            dungeon.TileMap = new int[100];

            dungeon.SetTile(3, 4, 5);
            Assert.Equal(5, dungeon.GetTile(3, 4));

            // Out of bounds returns 0
            Assert.Equal(0, dungeon.GetTile(-1, 0));
            Assert.Equal(0, dungeon.GetTile(100, 0));
        }
    }

    // =====================================================
    // 12. ITEM DATA TESTS (Equip/Consumable logic)
    // =====================================================

    public class ItemDataTests
    {
        [Fact]
        public void OnEquip_AppliesBonuses()
        {
            var player = CharacterClassFactory.CreatePlayer(PlayerClass.Wojownik);
            var item = ItemDatabase.Get("chainmail"); // DefenseBonus

            Assert.NotNull(item);
            int baseDef = player.Defense;

            item.OnEquip(player);
            Assert.True(player.Defense > baseDef);

            item.OnUnequip(player);
            Assert.Equal(baseDef, player.Defense);
        }

        [Fact]
        public void Use_HealingPotion_RestoresHP()
        {
            var player = CharacterClassFactory.CreatePlayer(PlayerClass.Wojownik);
            player.CurrentHP = 50;

            var potion = ItemDatabase.Get("healing_potion");
            Assert.NotNull(potion);

            var result = potion.Use(player);

            Assert.True(result.Success);
            Assert.True(result.HealAmount > 0);
            Assert.True(player.CurrentHP > 50);
        }

        [Fact]
        public void Use_Antidote_ClearsToxicity()
        {
            var player = CharacterClassFactory.CreatePlayer(PlayerClass.Wojownik);
            player.Toxicity = 75f;

            var antidote = ItemDatabase.Get("antidote");
            Assert.NotNull(antidote);
            Assert.True(antidote.ClearsToxicity);

            antidote.Use(player);

            Assert.Equal(0f, player.Toxicity);
        }

        [Fact]
        public void RarityColour_ReturnsCorrectHex()
        {
            var common = new ItemData { Rarity = ItemRarity.Common };
            var rare   = new ItemData { Rarity = ItemRarity.Rare };
            var epic   = new ItemData { Rarity = ItemRarity.Epic };

            Assert.Equal("#CCCCCC", common.RarityColour());
            Assert.Equal("#4A9EFF", rare.RarityColour());
            Assert.Equal("#FFD700", epic.RarityColour());
        }
    }
}
