using System.Collections.Generic;
using LochyIGorzala.Enemies;

namespace LochyIGorzala.Items
{
    /// <summary>
    /// Static registry of every item definition in the game.
    ///
    /// Design pattern: Factory (builds ItemData objects from typed constructors)
    /// combined with a lookup Dictionary for O(1) retrieval by ItemId.
    ///
    /// Sprite positions map to Assets/Art/32rogues-0.5.0/32rogues/items.png
    /// Grid is 0-indexed: col = letter offset (a=0, b=1 …), row = number-1.
    ///   e.g. "20.b. red potion" → row 19, col 1
    ///
    /// Items are divided by loot tier so LootSystem can draw from the correct pool.
    /// </summary>
    public static class ItemDatabase
    {
        // ─────────────────────────────────────────────────────────
        //  Lookup table — populated once on first access
        // ─────────────────────────────────────────────────────────

        private static Dictionary<string, ItemData> _db;

        private static Dictionary<string, ItemData> DB
        {
            get
            {
                if (_db == null) Build();
                return _db;
            }
        }

        /// <summary>Returns the ItemData for a given id, or null if not found.</summary>
        public static ItemData Get(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return null;
            return DB.TryGetValue(itemId, out var item) ? item : null;
        }

        // ─────────────────────────────────────────────────────────
        //  Rarity pools — used by LootSystem
        // ─────────────────────────────────────────────────────────

        public static IReadOnlyList<ItemData> CommonItems  { get; private set; }
        public static IReadOnlyList<ItemData> RareItems    { get; private set; }
        public static IReadOnlyList<ItemData> EpicItems    { get; private set; }

        // ─────────────────────────────────────────────────────────
        //  Build
        // ─────────────────────────────────────────────────────────

        private static void Build()
        {
            _db = new Dictionary<string, ItemData>();

            var common = new List<ItemData>();
            var rare   = new List<ItemData>();
            var epic   = new List<ItemData>();

            void Register(ItemData d)
            {
                _db[d.ItemId] = d;
                switch (d.Rarity)
                {
                    case ItemRarity.Common: common.Add(d); break;
                    case ItemRarity.Rare:   rare.Add(d);   break;
                    case ItemRarity.Epic:   epic.Add(d);   break;
                }
            }

            // ══════════════════════════════════════════════════════
            //  BROŃ (WEAPONS)
            //  items.png rows 0–10 contain melee and ranged weapons
            // ══════════════════════════════════════════════════════

            // --- Common ---

            Register(new ItemData
            {
                ItemId        = "old_knife",
                Name          = "Stary Nóż",
                Description   = "Wyszczerbiony nóż łowcy. Lepsze to niż nic.",
                Type          = ItemType.Weapon,
                Rarity        = ItemRarity.Common,
                AttackBonus   = 3,
                GoldValue     = 8,
                SpriteCol     = 0,   // 1.a. dagger
                SpriteRow     = 0
            });

            Register(new ItemData
            {
                ItemId        = "hunters_sword",
                Name          = "Miecz Łowcy",
                Description   = "Niezawodny miecz używany przez wiejskich łowców potworów.",
                Type          = ItemType.Weapon,
                Rarity        = ItemRarity.Common,
                AttackBonus   = 5,
                GoldValue     = 20,
                SpriteCol     = 3,   // 1.d. long sword
                SpriteRow     = 0
            });

            Register(new ItemData
            {
                ItemId        = "battle_axe",
                Name          = "Topór Bojowy",
                Description   = "Ciężki topór kowalski. Rozbija zbroje bez litości.",
                Type          = ItemType.Weapon,
                Rarity        = ItemRarity.Common,
                AttackBonus   = 6,
                GoldValue     = 22,
                SpriteCol     = 1,   // 4.b. battle axe
                SpriteRow     = 3
            });

            Register(new ItemData
            {
                ItemId        = "spiked_club",
                Name          = "Maczuga z Gwoźdźmi",
                Description   = "Prymitywna, ale skuteczna. Zostawia brzydkie rany.",
                Type          = ItemType.Weapon,
                Rarity        = ItemRarity.Common,
                AttackBonus   = 4,
                GoldValue     = 12,
                SpriteCol     = 1,   // 9.b. spiked club
                SpriteRow     = 8
            });

            // --- Rare ---

            Register(new ItemData
            {
                ItemId            = "silver_dagger",
                Name              = "Srebrny Sztylet",
                Description       = "Wykuty z czystego srebra. Zabójczy dla nieumarłych i leśnych duchów.",
                Type              = ItemType.Weapon,
                Rarity            = ItemRarity.Rare,
                AttackBonus       = 5,
                HasDamageTypeOverride = true,
                OverrideDamageType    = DamageType.Silver,
                GoldValue         = 55,
                SpriteCol         = 6,   // 1.g. sanguine dagger
                SpriteRow         = 0
            });

            Register(new ItemData
            {
                ItemId            = "silver_sword",
                Name              = "Srebrny Miecz",
                Description       = "Legendarny miecz łowcy. Strzygi i Chochliki drżą przed jego blaskiem.",
                Type              = ItemType.Weapon,
                Rarity            = ItemRarity.Rare,
                AttackBonus       = 8,
                HasDamageTypeOverride = true,
                OverrideDamageType    = DamageType.Silver,
                GoldValue         = 90,
                SpriteCol         = 8,   // 1.i. crystal sword (white = silver)
                SpriteRow         = 0
            });

            Register(new ItemData
            {
                ItemId            = "holy_mace",
                Name              = "Kiścień Świętej Wody",
                Description       = "Maczuga nasączona święconą wodą. Niszczy Wampiry i Strzygi.",
                Type              = ItemType.Weapon,
                Rarity            = ItemRarity.Rare,
                AttackBonus       = 7,
                HasDamageTypeOverride = true,
                OverrideDamageType    = DamageType.Holy,
                GoldValue         = 80,
                SpriteCol         = 1,   // 6.b. mace 2
                SpriteRow         = 5
            });

            Register(new ItemData
            {
                ItemId            = "crossbow",
                Name              = "Kusza Łowiecka",
                Description       = "Precyzyjny oręż. W rękach Łucznika — zabójczy.",
                Type              = ItemType.Weapon,
                Rarity            = ItemRarity.Rare,
                AttackBonus       = 7,
                GoldValue         = 65,
                SpriteCol         = 0,   // 10.a. crossbow
                SpriteRow         = 9
            });

            // --- Epic ---

            Register(new ItemData
            {
                ItemId            = "flame_staff",
                Name              = "Laska Ognia",
                Description       = "Pradawna laska emanująca żywym ogniem. Utopce giną od jednego dotknięcia.",
                Type              = ItemType.Weapon,
                Rarity            = ItemRarity.Epic,
                AttackBonus       = 13,
                HasDamageTypeOverride = true,
                OverrideDamageType    = DamageType.Fire,
                GoldValue         = 180,
                SpriteCol         = 6,   // 11.g. flame staff
                SpriteRow         = 10
            });

            Register(new ItemData
            {
                ItemId            = "holy_staff",
                Name              = "Laska Świętości",
                Description       = "Laska arcykapłana, przesiąknięta boską energią. Wampiry nienawidzą jej blasku.",
                Type              = ItemType.Weapon,
                Rarity            = ItemRarity.Epic,
                AttackBonus       = 11,
                HasDamageTypeOverride = true,
                OverrideDamageType    = DamageType.Holy,
                GoldValue         = 160,
                SpriteCol         = 1,   // 11.b. holy staff
                SpriteRow         = 10
            });

            Register(new ItemData
            {
                ItemId            = "zweihander",
                Name              = "Gryf — Wielki Miecz",
                Description       = "Dwuręczny miecz wykuty przez krasnoludów. Jeden zamach potrafi rozciąć trzy trupy naraz.",
                Type              = ItemType.Weapon,
                Rarity            = ItemRarity.Epic,
                AttackBonus       = 15,
                GoldValue         = 220,
                SpriteCol         = 5,   // 1.f. zweihander
                SpriteRow         = 0
            });

            // ══════════════════════════════════════════════════════
            //  ZBROJA I TARCZE (ARMOR)
            //  items.png rows 11–15: shields, armor, gloves, boots, helms
            // ══════════════════════════════════════════════════════

            // --- Common ---

            Register(new ItemData
            {
                ItemId        = "leather_armor",
                Name          = "Skórzana Zbroja",
                Description   = "Podstawowa ochrona z wyprawionej skóry wieprza błotnego.",
                Type          = ItemType.Armor,
                Rarity        = ItemRarity.Common,
                DefenseBonus  = 3,
                GoldValue     = 18,
                SpriteCol     = 1,   // 13.b. leather armor
                SpriteRow     = 12
            });

            Register(new ItemData
            {
                ItemId        = "round_shield",
                Name          = "Okrągła Tarcza",
                Description   = "Prosta drewniana tarcza obita żelazem. Wytrzyma niejedne uderzenie.",
                Type          = ItemType.Armor,
                Rarity        = ItemRarity.Common,
                DefenseBonus  = 2,
                GoldValue     = 14,
                SpriteCol     = 4,   // 12.e. round shield
                SpriteRow     = 11
            });

            Register(new ItemData
            {
                ItemId        = "leather_boots",
                Name          = "Skórzane Buty",
                Description   = "Wygodne buty dla długich marszów przez lochy. Chronią kostki.",
                Type          = ItemType.Armor,
                Rarity        = ItemRarity.Common,
                DefenseBonus  = 1,
                GoldValue     = 10,
                SpriteCol     = 1,   // 15.b. leather boots
                SpriteRow     = 14
            });

            // --- Rare ---

            Register(new ItemData
            {
                ItemId        = "chainmail",
                Name          = "Kolczuga",
                Description   = "Tysiące żelaznych ogniw — mniej wygodna od skóry, ale nieporównywalnie skuteczniejsza.",
                Type          = ItemType.Armor,
                Rarity        = ItemRarity.Rare,
                DefenseBonus  = 6,
                GoldValue     = 75,
                SpriteCol     = 3,   // 13.d. chain mail
                SpriteRow     = 12
            });

            Register(new ItemData
            {
                ItemId        = "kite_shield",
                Name          = "Tarcza Kościelna",
                Description   = "Tarcza z wyrytym świętym symbolem. Wampiry nie lubią na nią patrzeć.",
                Type          = ItemType.Armor,
                Rarity        = ItemRarity.Rare,
                DefenseBonus  = 4,
                GoldValue     = 60,
                SpriteCol     = 2,   // 12.c. cross shield
                SpriteRow     = 11
            });

            Register(new ItemData
            {
                ItemId        = "plate_helm",
                Name          = "Hełm Płytowy",
                Description   = "Solidny hełm kowalskiej roboty. Odchyla ciosy od głowy.",
                Type          = ItemType.Armor,
                Rarity        = ItemRarity.Rare,
                DefenseBonus  = 3,
                GoldValue     = 50,
                SpriteCol     = 7,   // 16.h. plate helm 2
                SpriteRow     = 15
            });

            // --- Epic ---

            Register(new ItemData
            {
                ItemId        = "plate_armor",
                Name          = "Zbroja Płytowa",
                Description   = "Szczyt rzemiosła kowalskiego. Tylko najgrubsze kły potworów mogą ją przebić.",
                Type          = ItemType.Armor,
                Rarity        = ItemRarity.Epic,
                DefenseBonus  = 10,
                GoldValue     = 200,
                SpriteCol     = 5,   // 13.f. chest plate
                SpriteRow     = 12
            });

            Register(new ItemData
            {
                ItemId        = "dark_shield",
                Name          = "Tarcza Mroku",
                Description   = "Wykuta z metalu lochów. Pochłania magię jak gąbka wodę.",
                Type          = ItemType.Armor,
                Rarity        = ItemRarity.Epic,
                DefenseBonus  = 7,
                HpBonus       = 15,
                GoldValue     = 175,
                SpriteCol     = 3,   // 12.d. dark shield
                SpriteRow     = 11
            });

            // ══════════════════════════════════════════════════════
            //  AKCESORIA
            //  items.png rows 16–18: pendants, rings
            // ══════════════════════════════════════════════════════

            // --- Common ---

            Register(new ItemData
            {
                ItemId        = "red_pendant",
                Name          = "Czerwony Wisiorek",
                Description   = "Kamień z krwistoczerwonym oczkiem. Krąży stara plotka, że wzmacnia siłę.",
                Type          = ItemType.Accessory,
                Rarity        = ItemRarity.Common,
                AttackBonus   = 1,
                GoldValue     = 12,
                SpriteCol     = 0,   // 17.a. red pendant
                SpriteRow     = 16
            });

            Register(new ItemData
            {
                ItemId        = "metal_pendant",
                Name          = "Stalowy Medalion",
                Description   = "Stalowy krążek na skórzanym sznurku. Nieznacznie usztywnia obronę.",
                Type          = ItemType.Accessory,
                Rarity        = ItemRarity.Common,
                DefenseBonus  = 1,
                GoldValue     = 10,
                SpriteCol     = 1,   // 17.b. metal pendant
                SpriteRow     = 16
            });

            // --- Rare ---

            Register(new ItemData
            {
                ItemId        = "ruby_ring",
                Name          = "Rubin Mocy",
                Description   = "Pierścień z ognistym rubinem. Użytkownik czuje nagłe przypływy siły.",
                Type          = ItemType.Accessory,
                Rarity        = ItemRarity.Rare,
                AttackBonus   = 4,
                GoldValue     = 70,
                SpriteCol     = 3,   // 18.d. ruby ring
                SpriteRow     = 17
            });

            Register(new ItemData
            {
                ItemId        = "sapphire_ring",
                Name          = "Szafirowy Pierścień",
                Description   = "Lodowatoszafirowy pierścień. Wzmacnia odporność na ciosy.",
                Type          = ItemType.Accessory,
                Rarity        = ItemRarity.Rare,
                DefenseBonus  = 4,
                GoldValue     = 65,
                SpriteCol     = 4,   // 18.e. sapphire ring
                SpriteRow     = 17
            });

            Register(new ItemData
            {
                ItemId        = "ankh",
                Name          = "Amulet Ankh",
                Description   = "Starożytny symbol życia i śmierci. Nosi się go jako ochronę przed ziomkami z tamtego świata.",
                Type          = ItemType.Accessory,
                Rarity        = ItemRarity.Rare,
                HpBonus       = 20,
                GoldValue     = 80,
                SpriteCol     = 6,   // 17.g. ankh
                SpriteRow     = 16
            });

            // --- Epic ---

            Register(new ItemData
            {
                ItemId        = "emerald_ring",
                Name          = "Pierścień Szmaragdowy",
                Description   = "Potężny artefakt Lochów. Noszący go czuje się niemal niezniszczalny.",
                Type          = ItemType.Accessory,
                Rarity        = ItemRarity.Epic,
                AttackBonus   = 5,
                DefenseBonus  = 5,
                GoldValue     = 250,
                SpriteCol     = 0,   // 18.a. gold emerald ring
                SpriteRow     = 17
            });

            Register(new ItemData
            {
                ItemId        = "crystal_pendant",
                Name          = "Kryształowy Amulet",
                Description   = "Pulsuje tajemniczą energią. Znacznie wzmacnia żywotność noszącego.",
                Type          = ItemType.Accessory,
                Rarity        = ItemRarity.Epic,
                HpBonus       = 35,
                DefenseBonus  = 3,
                GoldValue     = 210,
                SpriteCol     = 2,   // 17.c. crystal pendant
                SpriteRow     = 16
            });

            // ══════════════════════════════════════════════════════
            //  MIKSTURY I UŻYWKI (CONSUMABLES)
            //  items.png rows 19–20: potions
            //  items.png row 25:     food/drink
            // ══════════════════════════════════════════════════════

            // --- Common ---

            Register(new ItemData
            {
                ItemId         = "healing_potion",
                Name           = "Mikstura Lecznicza",
                Description    = "Czerwona substancja o mdłym smaku. Leczy rany jak z rękawa.",
                Type           = ItemType.Consumable,
                Rarity         = ItemRarity.Common,
                HealAmount     = 30,
                GoldValue      = 15,
                SpriteCol      = 1,   // 20.b. red potion
                SpriteRow      = 19
            });

            Register(new ItemData
            {
                ItemId         = "bimber",
                Name           = "Bimber",
                Description    = "Własnoręcznie pędzona okowita. Smakuje jak nafta, działa jak magia. Zwiększa toksyczność.",
                Type           = ItemType.Consumable,
                Rarity         = ItemRarity.Common,
                AttackBuff     = 3,
                ToxicityChange = 15f,
                GoldValue      = 12,
                SpriteCol      = 2,   // 20.c. brown vial
                SpriteRow      = 19
            });

            Register(new ItemData
            {
                ItemId         = "bread",
                Name           = "Czerstwy Chleb",
                Description    = "Twardy jak kamień, ale zawiera kalorie. Lekkie leczenie i zero toksyczności.",
                Type           = ItemType.Consumable,
                Rarity         = ItemRarity.Common,
                HealAmount     = 12,
                GoldValue      = 6,
                SpriteCol      = 1,   // 26.b. bread
                SpriteRow      = 25
            });

            Register(new ItemData
            {
                ItemId         = "beer",
                Name           = "Butelka Piwa",
                Description    = "Słaby trunek. Chwilowy zastrzyk odwagi i minimalna toksyczność.",
                Type           = ItemType.Consumable,
                Rarity         = ItemRarity.Common,
                HealAmount     = 8,
                AttackBuff     = 1,
                ToxicityChange = 5f,
                GoldValue      = 8,
                SpriteCol      = 3,   // 26.d. bottle of beer
                SpriteRow      = 25
            });

            // --- Rare ---

            Register(new ItemData
            {
                ItemId         = "greater_healing_potion",
                Name           = "Wielka Mikstura Lecznicza",
                Description    = "Skoncentrowany wyciąg z ziół leśnych. Leczy głębokie rany w mgnieniu oka.",
                Type           = ItemType.Consumable,
                Rarity         = ItemRarity.Rare,
                HealAmount     = 60,
                GoldValue      = 45,
                SpriteCol      = 0,   // 20.a. purple potion
                SpriteRow      = 19
            });

            Register(new ItemData
            {
                ItemId         = "zmijowa_nalewka",
                Name           = "Nalewka Żmijowa",
                Description    = "Żółtozielona ciecz ze wgniecionymi żmijami. Daje moc, ale też trucizna sączy się w żyły.",
                Type           = ItemType.Consumable,
                Rarity         = ItemRarity.Rare,
                HealAmount     = 20,
                AttackBuff     = 6,
                ToxicityChange = 22f,
                GoldValue      = 50,
                SpriteCol      = 4,   // 20.e. green potion
                SpriteRow      = 19
            });

            Register(new ItemData
            {
                ItemId         = "antidote",
                Name           = "Antidotum",
                Description    = "Niebieska mikstura neutralizująca trucizny. Oczyszcza krew z toksyn. Absolutny niezbędnik.",
                Type           = ItemType.Consumable,
                Rarity         = ItemRarity.Rare,
                ClearsToxicity = true,
                HealAmount     = 10,
                GoldValue      = 55,
                SpriteCol      = 3,   // 21.d. blue potion
                SpriteRow      = 20
            });

            Register(new ItemData
            {
                ItemId         = "orange_potion",
                Name           = "Eliksir Siły",
                Description    = "Pomarańczowy płyn drżący od energii. Tymczasowo podnosi moc uderzenia.",
                Type           = ItemType.Consumable,
                Rarity         = ItemRarity.Rare,
                AttackBuff     = 8,
                ToxicityChange = 10f,
                GoldValue      = 48,
                SpriteCol      = 4,   // 21.e. orange potion
                SpriteRow      = 20
            });

            // --- Epic ---

            Register(new ItemData
            {
                ItemId         = "okowita",
                Name           = "Okowita — Mroczna Wódka",
                Description    = "Czarna jak smoła, parzy jak ogień. Skrajnie niebezpieczna dawka bimbru. Gniewko żyje tylko dlatego, że przyzwyczaił się do trucizny.",
                Type           = ItemType.Consumable,
                Rarity         = ItemRarity.Epic,
                HealAmount     = 30,
                AttackBuff     = 12,
                ToxicityChange = 30f,
                GoldValue      = 120,
                SpriteCol      = 0,   // 21.a. black potion
                SpriteRow      = 20
            });

            Register(new ItemData
            {
                ItemId         = "elixir_of_life",
                Name           = "Eliksir Życia",
                Description    = "Różowy nektar wytwarzany raz na sto lat przez leśne duchy. Przywraca ogromną ilość HP.",
                Type           = ItemType.Consumable,
                Rarity         = ItemRarity.Epic,
                HealAmount     = 100,
                GoldValue      = 200,
                SpriteCol      = 2,   // 21.c. pink vial
                SpriteRow      = 20
            });

            // ══════════════════════════════════════════════════════
            //  KLUCZE (specjalne — używane przez system questów i skarbców)
            // ══════════════════════════════════════════════════════

            Register(new ItemData
            {
                ItemId      = "golden_key",
                Name        = "Złoty Klucz",
                Description = "Klucz wykuty ze złota. Otwiera zamknięte skrzynie skarbów w lochach.",
                Type        = ItemType.Consumable,
                Rarity      = ItemRarity.Rare,
                GoldValue   = 40,
                SpriteCol   = 0,   // 23.a. gold key
                SpriteRow   = 22
            });

            Register(new ItemData
            {
                ItemId      = "ornate_key",
                Name        = "Ozdobny Klucz",
                Description = "Misternie zdobiony klucz. Pasuje do zamków bossów.",
                Type        = ItemType.Consumable,
                Rarity      = ItemRarity.Epic,
                GoldValue   = 0,   // Quest item — not for sale
                SpriteCol   = 1,   // 23.b. ornate key
                SpriteRow   = 22
            });

            // ══════════════════════════════════════════════════════
            //  EPICKIE EQ U MIRKA (dostępne w sklepie, 80–100 złota)
            //  Zaprojektowane tak, by gracz mógł się przygotować na Deliriusa
            // ══════════════════════════════════════════════════════

            Register(new ItemData
            {
                ItemId            = "blessed_sword",
                Name              = "Błogosławiony Miecz",
                Description       = "Miecz poświęcony przez kapłanów Światła. Święty oręż — "
                                  + "idealny przeciw Deliriusowi i innym chaotycznym bestiom.",
                Type              = ItemType.Weapon,
                Rarity            = ItemRarity.Epic,
                AttackBonus       = 12,
                HasDamageTypeOverride = true,
                OverrideDamageType    = DamageType.Holy,
                GoldValue         = 95,
                SpriteCol         = 9,   // 1.j. glowing sword
                SpriteRow         = 0
            });

            Register(new ItemData
            {
                ItemId        = "wzmocniona_kolczuga",
                Name          = "Wzmocniona Kolczuga",
                Description   = "Kolczuga wzmocniona magicznymi runami. Znacznie zwiększa wytrzymałość "
                              + "i dodaje punkty życia.",
                Type          = ItemType.Armor,
                Rarity        = ItemRarity.Epic,
                DefenseBonus  = 8,
                HpBonus       = 25,
                GoldValue     = 100,
                SpriteCol     = 4,   // 13.e. reinforced mail
                SpriteRow     = 12
            });

            Register(new ItemData
            {
                ItemId         = "wielki_eliksir",
                Name           = "Wielki Eliksir Odnowy",
                Description    = "Potężna mikstura przygotowana specjalnie na bossów. "
                               + "Leczy ogromną ilość HP i czyści toksyczność.",
                Type           = ItemType.Consumable,
                Rarity         = ItemRarity.Epic,
                HealAmount     = 80,
                ClearsToxicity = true,
                GoldValue      = 80,
                SpriteCol      = 1,   // 21.b. large potion
                SpriteRow      = 21
            });

            // ══════════════════════════════════════════════════════
            //  SPECJALNE PRZEDMIOTY FABULARNE
            // ══════════════════════════════════════════════════════

            Register(new ItemData
            {
                ItemId         = "boski_lek_na_kaca",
                Name           = "Boski Lek na Kaca",
                Description    = "Legendarny eliksir stworzony przez samego Władcę Podziemi. "
                               + "Jedna kropla leczy każdego kaca, dwie — wskrzeszają umarłych, "
                               + "trzy — no lepiej nie próbować trzech. "
                               + "Całkowicie oczyszcza toksyczność i przywraca pełnię sił.",
                Type           = ItemType.Consumable,
                Rarity         = ItemRarity.Epic,
                HealAmount     = 999,       // Full heal — godlike
                ClearsToxicity = true,
                GoldValue      = 0,         // Quest reward — not for sale
                SpriteCol      = 5,         // 22.f. ornate large sprite
                SpriteRow      = 21
            });

            // Assign pool lists
            CommonItems = common.AsReadOnly();
            RareItems   = rare.AsReadOnly();
            EpicItems   = epic.AsReadOnly();
        }
    }
}
