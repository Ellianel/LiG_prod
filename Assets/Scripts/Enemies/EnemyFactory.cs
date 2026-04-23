using LochyIGorzala.Combat;

namespace LochyIGorzala.Enemies
{
    /// <summary>
    /// Factory pattern — creates enemy instances based on enemy type ID.
    /// Centralizes enemy construction and makes it easy to add new enemy types.
    /// </summary>
    public static class EnemyFactory
    {
        public enum EnemyType
        {
            // Regular enemies
            Utopiec,
            Chochlik,
            Strzyga,
            // Floor bosses (floors 1-4)
            BossGargulec,    // Floor 1: stone gargoyle
            BossNekromanta,  // Floor 2: necromancer
            BossWampir,      // Floor 3: master vampire
            BossOgien,       // Floor 4: fire demon
            // Final boss (floor 5 only)
            Delirius
        }

        /// <summary>
        /// Creates a new enemy instance with full stats based on type.
        /// </summary>
        public static EnemyData Create(EnemyType type)
        {
            switch (type)
            {
                case EnemyType.Utopiec:        return new Utopiec();
                case EnemyType.Chochlik:       return new Chochlik();
                case EnemyType.Strzyga:        return new Strzyga();
                case EnemyType.BossGargulec:   return new BossGargulec();
                case EnemyType.BossNekromanta: return new BossNekromanta();
                case EnemyType.BossWampir:     return new BossWampir();
                case EnemyType.BossOgien:      return new BossOgien();
                case EnemyType.Delirius:       return new Delirius();
                default:                       return new Chochlik();
            }
        }
    }

    // =====================================================
    // REGULAR ENEMY TYPES
    // =====================================================

    /// <summary>
    /// Utopiec — drowned spirit lurking in swamps.
    /// Slow but tanky, weak to Fire.
    /// Sprite: ghoul (col=5, row=4 in monsters.png)
    /// </summary>
    public class Utopiec : EnemyData
    {
        public Utopiec()
        {
            Name = "Utopiec";
            Description = "Topielec z bagien. Śmierdzi gnijącym błotem.";
            Level = 2;
            MaxHP = 45;
            CurrentHP = 45;
            Attack = 10;
            Defense = 6;
            ExperienceReward = 30;
            GoldReward = 40;
            SpriteCol = 5;
            SpriteRow = 4;
            Weakness = DamageType.Fire;
        }

        public override string GetEncounterMessage()
        {
            return "Utopiec wynurza się z mrocznej wody! Cuchnący oddech paraliżuje powietrze...";
        }

        public override int CalculateAttackDamage()
        {
            int baseDmg = base.CalculateAttackDamage();
            if (new System.Random().Next(100) < 20)
                return (int)(baseDmg * 1.4f); // Grab attack
            return baseDmg;
        }

        public override StatusEffect TryApplyStatusEffect()
        {
            // 20% chance to poison (3 dmg/turn, 3 turns)
            if (new System.Random().Next(100) < 20)
                return new StatusEffect(StatusEffectType.Poison, 3, 3);
            return null;
        }
    }

    /// <summary>
    /// Chochlik — mischievous forest imp.
    /// Fast and annoying, weak to Silver.
    /// Sprite: goblin (col=2, row=0 in monsters.png)
    /// </summary>
    public class Chochlik : EnemyData
    {
        public Chochlik()
        {
            Name = "Chochlik";
            Description = "Mały psotnik leśny. Szybki i irytujący.";
            Level = 1;
            MaxHP = 25;
            CurrentHP = 25;
            Attack = 7;
            Defense = 2;
            ExperienceReward = 15;
            GoldReward = 25;
            SpriteCol = 2;
            SpriteRow = 0;
            Weakness = DamageType.Silver;
        }

        public override string GetEncounterMessage()
        {
            return "Chochlik wyskakuje zza kamienia! Chichocze złośliwie...";
        }
    }

    /// <summary>
    /// Strzyga — undead vampire-like creature from Slavic mythology.
    /// High attack, weak to Holy. Regenerates HP each hit.
    /// Sprite: banshee (col=0, row=5 in monsters.png)
    /// </summary>
    public class Strzyga : EnemyData
    {
        public Strzyga()
        {
            Name = "Strzyga";
            Description = "Nieumarła istota o dwóch sercach. Żywi się krwią.";
            Level = 3;
            MaxHP = 55;
            CurrentHP = 55;
            Attack = 14;
            Defense = 5;
            ExperienceReward = 50;
            GoldReward = 70;
            SpriteCol = 0;
            SpriteRow = 5;
            Weakness = DamageType.Holy;
        }

        public override string GetEncounterMessage()
        {
            return "Strzyga rozkłada skrzydła! Jej oczy płoną krwistym blaskiem!";
        }

        public override int TakeDamage(int rawDamage, DamageType type)
        {
            // Strzyga regenerates 2 HP per hit unless hit with Holy
            if (type != DamageType.Holy && CurrentHP > 0)
                CurrentHP = System.Math.Min(MaxHP, CurrentHP + 2);
            return base.TakeDamage(rawDamage, type);
        }

        public override StatusEffect TryApplyStatusEffect()
        {
            // 25% chance to slow (reduces AP, 2 turns)
            if (new System.Random().Next(100) < 25)
                return new StatusEffect(StatusEffectType.Slow, 2);
            return null;
        }
    }

    // =====================================================
    // FLOOR BOSS TYPES (floors 1-4)
    // =====================================================

    /// <summary>
    /// BossGargulec — Floor 1 boss. Twin abomination animated by dark magic.
    /// Enormous defense, shatters armor with heavy blows. Weak to Fire.
    /// Sprite: col=3, row=2 in monsters.png (two-headed green beast)
    /// </summary>
    public class BossGargulec : EnemyData
    {
        public BossGargulec()
        {
            Name = "Bliźniacze Dziwadła";
            Description = "Dwugłowe monstrum zrodzone z mrocznej magii.";
            Level = 3;
            MaxHP = 110;
            CurrentHP = 110;
            Attack = 16;
            Defense = 12;
            ExperienceReward = 120;
            GoldReward = 130;
            SpriteCol = 3;
            SpriteRow = 2;
            Weakness = DamageType.Fire;
            IsBoss = true;
            Scale = 1.5f;
        }

        public override string GetEncounterMessage()
        {
            return "BLIŹNIACZE DZIWADŁA rozdzierają ciemność! Cztery oczy płoną nienawiścią!\n\"INTRUZI UMRĄ!\"";
        }

        public override int CalculateAttackDamage()
        {
            // Stone crush: 25% chance for massive damage
            int baseDmg = base.CalculateAttackDamage();
            if (new System.Random().Next(100) < 25)
                return (int)(baseDmg * 1.8f); // Crushing blow
            return baseDmg;
        }
    }

    /// <summary>
    /// BossNekromanta — Floor 2 boss. Ancient necromancer who commands the dead.
    /// Curses the player reducing their attack. Weak to Holy.
    /// Sprite: col=3, row=5 in monsters.png (adjust if needed)
    /// </summary>
    public class BossNekromanta : EnemyData
    {
        public BossNekromanta()
        {
            Name = "Nekromanta Martwicy";
            Description = "Pradawny czarownik manipulujący śmiercią.";
            Level = 4;
            MaxHP = 130;
            CurrentHP = 130;
            Attack = 18;
            Defense = 8;
            ExperienceReward = 160;
            GoldReward = 180;
            SpriteCol = 3;
            SpriteRow = 5;
            Weakness = DamageType.Holy;
            IsBoss = true;
            Scale = 1.5f;
        }

        public override string GetEncounterMessage()
        {
            return "NEKROMANTA MARTWICY unosi swoją kosturę!\n\"Zasilisz moją armię nieumarłych!\"";
        }

        public override int TakeDamage(int rawDamage, DamageType type)
        {
            // Necromancer drains life on each hit (partial HP regen)
            if (type != DamageType.Holy && CurrentHP > 0)
                CurrentHP = System.Math.Min(MaxHP, CurrentHP + 4);
            return base.TakeDamage(rawDamage, type);
        }

        public override StatusEffect TryApplyStatusEffect()
        {
            // 30% chance to poison (4 dmg/turn, 3 turns)
            if (new System.Random().Next(100) < 30)
                return new StatusEffect(StatusEffectType.Poison, 4, 4);
            return null;
        }
    }

    /// <summary>
    /// BossWampir — Floor 3 boss. Baba Jaga, ancient witch of Slavic myth.
    /// Life steal on attacks. Weak to Holy.
    /// Sprite: col=4, row=5 in monsters.png (adjust if needed)
    /// </summary>
    public class BossWampir : EnemyData
    {
        public BossWampir()
        {
            Name = "Baba Jaga";
            Description = "Pradawna wiedźma z głębin słowiańskich legend.";
            Level = 5;
            MaxHP = 150;
            CurrentHP = 150;
            Attack = 22;
            Defense = 10;
            ExperienceReward = 200;
            GoldReward = 230;
            SpriteCol = 4;
            SpriteRow = 5;
            Weakness = DamageType.Holy;
            IsBoss = true;
            Scale = 1.5f;
        }

        public override string GetEncounterMessage()
        {
            return "BABA JAGA wyłania się z mroku! Jej szpony skrzeczą po kamieniu!\n\"Twoje kości posłużą mi za kolację...\"";
        }

        public override int CalculateAttackDamage()
        {
            // Life drain: heals 8 HP on attack
            int dmg = base.CalculateAttackDamage();
            CurrentHP = System.Math.Min(MaxHP, CurrentHP + 8);
            return dmg;
        }
    }

    /// <summary>
    /// BossOgien — Floor 4 boss. Fire demon from the infernal depths.
    /// Burns the player for continuous damage. Weak to Holy, immune to Fire.
    /// Sprite: col=7, row=7 in monsters.png (fiery red beast)
    /// </summary>
    public class BossOgien : EnemyData
    {
        public BossOgien()
        {
            Name = "Ognisty Diabeł";
            Description = "Demon ognia z czeluści piekielnych. Jego dotyk spala.";
            Level = 6;
            MaxHP = 150;
            CurrentHP = 150;
            Attack = 20;
            Defense = 9;
            ExperienceReward = 260;
            GoldReward = 280;
            SpriteCol = 7;
            SpriteRow = 7;
            Weakness = DamageType.Holy;
            IsBoss = true;
            Scale = 1.5f;
        }

        public override string GetEncounterMessage()
        {
            return "OGNISTY DIABEŁ wybucha z płonącej lawy!\n\"SPŁONĘ NA POPIÓŁ!\"";
        }

        public override int CalculateAttackDamage()
        {
            int baseDmg = base.CalculateAttackDamage();
            // Hellfire: 25% chance for inferno strike (nerfed from 35%/2.0x)
            if (new System.Random().Next(100) < 25)
                return (int)(baseDmg * 1.6f);
            return baseDmg;
        }

        public override int TakeDamage(int rawDamage, DamageType type)
        {
            // Fire immune — heals instead of taking damage
            if (type == DamageType.Fire)
            {
                CurrentHP = System.Math.Min(MaxHP, CurrentHP + rawDamage / 2);
                return 0; // No damage taken
            }
            return base.TakeDamage(rawDamage, type);
        }

        public override StatusEffect TryApplyStatusEffect()
        {
            // 25% chance to burn (4 dmg/turn, 2 turns) — nerfed from 35%/5dmg
            if (new System.Random().Next(100) < 25)
                return new StatusEffect(StatusEffectType.Burn, 2, 4);
            return null;
        }
    }

    // =====================================================
    // FINAL BOSS (floor 5)
    // =====================================================

    /// <summary>
    /// Delirius — The final boss of the dungeon. Chaos incarnate.
    ///
    /// PHASE 1 (HP > 50%): Powerful but manageable.
    /// PHASE 2 (HP <= 50%): Enrages — attack doubles, chaos strikes trigger more often.
    ///
    /// Only Holy damage is effective. At 50% HP, a phase transition message is triggered
    /// via an internal flag (visible to CombatEngine through LastPhaseMessage).
    ///
    /// Sprite: col=1, row=6 in monsters.png (adjust if needed for a large demon sprite)
    /// </summary>
    public class Delirius : EnemyData
    {
        private bool _phase2Active = false;

        /// <summary>
        /// Set to true the moment phase 2 activates. CombatEngine reads and clears this
        /// each turn to show a one-time phase transition message.
        /// </summary>
        public bool PhaseTransitionJustOccurred { get; private set; } = false;

        public Delirius()
        {
            Name = "DELIRIUS";
            Description = "Chaos ucieleśniony. Ostatni strażnik Lochów i źródło wszelkiego zła.";
            Level = 10;
            MaxHP = 200;
            CurrentHP = 200;
            Attack = 22;
            Defense = 10;
            ExperienceReward = 999;
            GoldReward = 600;
            SpriteCol = 1;
            SpriteRow = 6;
            Weakness = DamageType.Holy;
            IsBoss = true;
            Scale = 2.0f;
        }

        public override string GetEncounterMessage()
        {
            return "DELIRIUS otwiera oczy po tysiącach lat uśpienia...\n" +
                   "\"Jak ŚMIESZ zakłócić mój wieczny sen, robaku?!\"\n" +
                   "\"Twoje kości zostaną PROCHAMI tych lochów NA WIEKI!\"";
        }

        private void CheckPhaseTransition()
        {
            if (!_phase2Active && CurrentHP <= MaxHP / 2)
            {
                _phase2Active = true;
                PhaseTransitionJustOccurred = true;
                // Enrage: increase stats (nerfed — beatable with epic gear)
                Attack = 26;
                Defense = 12;
            }
        }

        public void ClearPhaseTransitionFlag() => PhaseTransitionJustOccurred = false;

        public override int CalculateAttackDamage()
        {
            CheckPhaseTransition();

            var rng = new System.Random();
            int baseDmg = _phase2Active ? Attack : base.CalculateAttackDamage();

            // Phase 1: 15% chance of chaos surge (1.3x)
            // Phase 2: 25% chance of chaos surge (1.5x) — nerfed to be beatable with epic gear
            float surgeProbability = _phase2Active ? 0.25f : 0.15f;
            float surgeMultiplier  = _phase2Active ? 1.5f  : 1.3f;

            if (rng.NextDouble() < surgeProbability)
                return (int)(baseDmg * surgeMultiplier);

            return baseDmg;
        }

        public override int TakeDamage(int rawDamage, DamageType type)
        {
            CheckPhaseTransition();

            // Phase 1: Physical resistance (deals only 50% physical damage)
            if (!_phase2Active && type == DamageType.Physical)
                rawDamage = System.Math.Max(1, rawDamage / 2);

            return base.TakeDamage(rawDamage, type);
        }
    }
}
