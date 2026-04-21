# LOCHY & GORZALA — Project Context
> Wersja: 2026-04-20 (v3)  |  Silnik: Unity 6000.4.0f1  |  Jezyk: C# (.NET Standard 2.1)
> Ten plik opisuje stan projektu dla nowych czatow z AI.

---

## OPIS GRY

**Gatunek:** 2D Dungeon Crawler / Turowe RPG
**Klimat:** Dark/Low Fantasy, slowianskie mity, brudny swiat
**Bohater:** Gniewko -- zgorzknially lowca potworow uzywajacy alkoholu (bimber/okowita) jako sily
**Cel:** Przejsc 5 pieter lochu i pokonac finalbossa Deliriusa

**Flow po pokonaniu Deliriusa:** Gracz zostaje na pietrze 5, NIE pojawiaja sie schody. Spawn NPC "Wladca Podziemi" (czarodziej) kilka kratek od miejsca walki z Deliriusem. Po wcisnieciu E: speech + credits (imiona autorow) + gra zamyka sie po 8 sekundach.

**Flow po smierci gracza:** Gniewko respawnuje w lobby (floor 0) z komunikatem "Sprobujmy na druga nozke!" widocznym 5 sek, pelne HP, toxicity 0, gracz moze probowac ponownie.

**Flow po ucieczce z walki:** Gracz wraca na mape, potwor zostaje z pelnym HP.

---

## STACK TECHNOLOGICZNY

| Element | Szczegoly |
|---|---|
| Silnik | Unity 6000.4.0f1 |
| Jezyk | C# 9.0 (.NET Standard 2.1) -- TYLKO C#, zero GDScript |
| Renderowanie | URP 2D / Compatibility |
| IDE | Visual Studio 2022 |
| Testy | xUnit (osobny projekt -- jeszcze nie zrobiony) |
| Sprites | 32rogues-0.5.0 (items.png 352x832 11c x 26r, monsters.png 384x416 12c x 13r, rogues.png 224x224 7c x 7r, tiles.png) |
| Input | Nowy Input System (UnityEngine.InputSystem) -- ZAKAZ Input.GetKeyDown |
| Deprecated | GetHashCode() zamiast GetInstanceID()/GetEntityId(), FindAnyObjectByType zamiast FindFirstObjectByType |

---

## ARCHITEKTURA -- SEPARACJA WARSTW

```
Assets/Scripts/
  Core/          -- Pure C# (bez Unity): GameState, PlayerData, GameEvents, CharacterClass, QuestData, AchievementData
  Combat/        -- Pure C# + Unity View: CombatEngine (pure), CombatAction (pure), StatusEffect (pure), CombatUIController (Unity), EnemySpawner (Unity)
  Enemies/       -- Pure C#: EnemyData (abstract), EnemyFactory, 8 klas wrogow
  Items/         -- Pure C#: Item, ItemData, ItemDatabase, Inventory, LootSystem, IInteractable
  Dungeon/       -- Unity: DungeonGenerator (BSP), TileType, DungeonLevelConfig, TrapData, PuzzleData
  Managers/      -- Unity Singleton: GameManager, QuestManager (static), AchievementManager (static)
  Player/        -- Unity: PlayerController, CameraFollow, PlayerInputActions
  NPC/           -- Unity: MerchantNPC, NpcSpawner, WladcaPodziemiNPC, TajemniczyJegomoscNPC
  UI/            -- Unity View: NotificationUIController, ShopUIController, InventoryUIController, CombatUIController, InGameMenuPanel, CharSelectController, MainMenuController, SaveSlotPanel, QuestJournalUIController, AchievementsUIController
  Helpers/       -- SpriteSheetHelper
  Editor/        -- ProjectSetupEditor (buduje wszystkie sceny jednym kliknieciem)
```

**Kluczowa zasada:** Klasy logiczne (Core, Combat, Enemies, Items) NIE dziedzicza po MonoBehaviour. Unity UI jest osobna warstwa View.

---

## WZORCE PROJEKTOWE (wymaganie prowadzacego: min. 3)

| Wzorzec | Gdzie |
|---|---|
| **Singleton** | `GameManager` -- persists across scenes via `DontDestroyOnLoad` |
| **Factory** | `EnemyFactory.Create(EnemyType)`, `CharacterClassFactory.CreatePlayer(PlayerClass)`, `ItemDatabase` |
| **Strategy** | `ICombatAction` -- kazda akcja walki to osobna klasa (LightAttackAction, DrinkBimberAction, etc.) |
| **Observer** | `GameEvents` -- statyczna klasa z C# events dla komunikacji Logika->UI |
| **Abstract/Polymorphism** | `EnemyData` -- abstract base, 8 konkretnych klas wrogow z override metod |
| **IInteractable** | Interface implementowany przez `ItemData`, `MerchantNPC`, `WladcaPodziemiNPC`, `TajemniczyJegomoscNPC` |
| **IEquippable / IConsumable** | Interfaces implementowane przez `ItemData` |

---

## SCENY (4 sceny w build settings)

| Scena | Zawartosc |
|---|---|
| `MainMenu` | Logo, przyciski Start/Kontynuuj/Wyjdz |
| `CharSelect` | Wybor klasy: Wojownik / Lucznik / Mag |
| `Dungeon` | Tilemap BSP, gracz, wrogowie, Mirek Handlarz (floor 0), Tajemniczy Jegomosc (floor 0), Wladca Podziemi (floor 5 po pokonaniu Deliriusa), HUD z dziennikiem misji, NotificationOverlay |
| `Combat` | Pokemon-style walka turowa, panel ekwipunku, tematyczne tlo per pietro |

**UWAGA:** Sceny generowane automatycznie przez: `Tools -> Lochy i Gorzala -> Setup Project`

---

## CORE SYSTEMS

### System Walki (CombatEngine.cs -- pure C#)
- Turowy, gracz ma 3 PA (Punkty Akcji) na ture
- Faza: PlayerTurn -> EnemyTurn -> Victory/Defeat/Fled
- Akcje gracza: Lekki Atak (1PA), Ciezki/Specjal (2PA), Ulecz sie (1PA), Bimber (1PA), Obrona (1PA), Uciekaj (0PA)
- `AttackBuff` z Bimbra jest tymczasowy -- CombatEngine go cofa po turze wroga
- Toksycznosc DoT: co 2 tury gdy Tox >= 50%
- **Status effects:** `_activeEffects` lista w CombatEngine, tick na poczatku enemy turn, clear w EndCombat()
- Polskie teksty we wszystkich akcjach i komunikatach (z polskimi literami)

### Tla Walki (Combat Backgrounds)
- 5 tematycznych tel PNG (960x540) w `Assets/Resources/CombatBackgrounds/`
- Ladowane dynamicznie w `CombatUIController.LoadFloorBackground(floor)` via `Resources.Load<Texture2D>()`
- Floor 1: niebiesko-szara jaskinia, Floor 2: zielona jaskinia z mchem, Floor 3: fioletowa jaskinia, Floor 4: pomaranczowo-czerwona lawowa jaskinia, Floor 5: ciemnogranatowa jaskinia
- Lobby (floor 0): domyslne ciemne tlo (brak obrazka)
- `backgroundImage` field w CombatUIController wired przez ProjectSetupEditor

### System Toksycznosci
- `PlayerData.Toxicity` (0-100), rosnie z kazdym bimbrem/nalewka
- Przy Tox=100: -15 HP damage, debuff
- `ClearsToxicity=true` w Antidotum czyci do zera

### Klasy Postaci
| Klasa | HP | Atak | Def | PA | Specjal |
|---|---|---|---|---|---|
| Wojownik | 120 | 14 | 8 | 3 | Potezne Uderzenie (2PA, 2x obrazenia) |
| Lucznik | 85 | 11 | 4 | 4 | Strzal w Slabosc (1PA, ignoruje pancerz) |
| Mag | 70 | 7 | 3 | 3 | Kula Ognia (2PA, Fire dmg, 18+Atk*1.5) |

Sprite sheet rogues.png: Wojownik (r1,c1), Lucznik (r0,c2), Mag (r4,c1)

### System Slabosci (DamageType)
Physical, Silver, Fire, Holy, Poison
- Utopiec -> slaby na Fire
- Chochlik -> slaby na Silver
- Strzyga -> slaby na Holy (regeneruje 2HP/hit innym typem)
- Bossowie: Gargulec->Fire, Nekromanta->Holy, Wampir->Holy, Ogien->Holy, Delirius->Holy

### System Ekwipunku (Inventory.cs -- pure C#)
- `InventoryData` -- serializowalny DTO (w PlayerData, zapis JSON)
- `Inventory` -- runtime wrapper z metodami AddItem/EquipItem/UseConsumable
- 3 sloty: Weapon, Armor, Accessory
- 20 miejsc w plecaku
- Wyposazony miecz automatycznie zmienia DamageType w walce

### System Lootu (LootSystem.cs)
- Roll po kazdej walce: 50% szansa na drop (boss=100%)
- Rarity weights per pietro:
  - Floor 1: Common 75%, Rare 23%, Epic 2%
  - Floor 5: Common 30%, Rare 40%, Epic 30%
- Bossi na pietrze 3+ gwarantuja Rare/Epic

### System Przedmiotow (ItemDatabase.cs)
- **41 zdefiniowanych przedmiotow** z pozycjami sprite w items.png
- Kategorie: Weapons (12 w tym Blogoslawiony Miecz), Armor (9 w tym Wzmocniona Kolczuga), Accessories (8), Consumables (13 w tym Wielki Eliksir i Boski Lek na Kaca), Keys (2)
- Rzadkosci: Common (szary), Rare (niebieski #4A9EFF), Epic (zloty #FFD700)
- Sprite sheet: `Assets/Art/32rogues-0.5.0/32rogues/items.png` (352x832, 11 cols x 26 rows, 32px per cell)
- "Boski Lek na Kaca" -- legendarny quest reward, HealAmount=999, ClearsToxicity=true, GoldValue=0, sprite (c5,r21)

### System Sklepu (ShopUIController.cs + MerchantNPC.cs)
- Mirek Handlarz stoi w lobby (floor 0) przy pozycji **x=26, y=10** (prawa nisza)
- Lobby jest hand-crafted i nigdy sie nie zmienia
- Otwieranie: nacisnij **E** gdy blisko (promien 2.2 kafelka)
- Kupno: cena z `ItemData.GoldValue`
- Sprzedaz: 50% ceny zakupu
- Mirek ma **18 itemow** w stalym stocku (w tym 3 epickie za 80-100 zlota):
  - **Blogoslawiony Miecz** (95zl) -- +12 atak, Holy dmg (kluczowy na Deliriusa)
  - **Wzmocniona Kolczuga** (100zl) -- +8 def, +25 HP
  - **Wielki Eliksir Odnowy** (80zl) -- leczy 80 HP + czyci toksycznosc

### System Questow (QuestManager.cs + QuestData.cs)
- **QuestManager** -- statyczna klasa, subskrybuje OnEnemyKilled, Initialize()/Cleanup()
- **QuestData** -- pure C# z dwoma typami: Kill i Floor
- **QuestDatabase** -- statyczne definicje wszystkich questow
- **QuestSaveData** -- serializowalne w GameState (Quests list, IntroCompleted, NextQuestIndex)

#### Flow questow:
1. Gracz rozmawia z Tajemniczym Jegomosciem -> intro speech -> schody do pietra 1 pojawiaja sie -> floor questy accepted -> pierwszy kill quest accepted
2. Gracz zabija potwory -> QuestManager.OnEnemyKilled aktualizuje postep -> notyfikacja po ukonczeniu
3. Gracz wraca do NPC -> nagroda (itemy) + nastepny kill quest
4. Po wszystkich questach -> NPC mowi "podzegna"

#### Kill Questy (od Tajemniczego Jegomoscia):
| Id | Tytul | Cel | Nagroda |
|---|---|---|---|
| kill_chochliki | Polowanie na Chochliki | 3x Chochlik | 2x healing_potion |
| kill_strzygi | Strzygi muszą zginąć | 3x Strzyga | 2x antidote |
| kill_utopce | Mokra robota | 3x Utopiec | 3x zmijowa_nalewka |

#### Floor Questy (auto-accepted, auto-completed):
| Id | Tytul | Target Floor |
|---|---|---|
| floor_1 | Przedostań się przez pierwszy poziom | 2 |
| floor_2 | Przedostań się przez drugi poziom | 3 |
| find_wladca | Odnajdź Władcę Podziemi | 6 (= po Deliriusie) |

### Dziennik Misji (QuestJournalUIController.cs)
- Przycisk "Dziennik" w lewym gornym rogu HUD
- Panel po lewej stronie (35% szerokosci)
- Subskrybuje GameEvents.OnQuestJournalChanged -> auto-odswiezanie
- Kolorowanie: zielony (#66FF66) = wykonana, bialy = aktywna, zloty (#FFD700) = postep
- Pokazuje questy Active + Completed (kill) + Rewarded (floor)

### System Pulapek (TrapData.cs + DungeonGenerator.cs + PlayerController.cs)
- 3 typy pulapek: Spikes (-15 HP), MagicDrain (+20 Tox), Teleport (do wejscia)
- Rozmieszczane losowo przez `DungeonGenerator.ScatterTraps()` na pietrach 2+
- Deterministic typ pulapki via hash koordynatow (TrapData.GetTrapType)
- Triggerowane w `PlayerController.CheckForTrap()` po kazdym ruchu
- TileType.Trap w DungeonGenerator -- renderowane jako floor + deco overlay
- Smierc od pulapki (HP <= 0) -> flow jak po przegranej walce

### System Osiagniec (AchievementData.cs + AchievementManager.cs + AchievementsUIController.cs)
- **AchievementManager** -- statyczna klasa, subskrybuje OnEnemyKilled, OnGoldChanged, OnQuestCompleted, OnTrapTriggered, OnAllPuzzlesSolved, OnToxicityChanged
- **AchievementSaveData** -- serializowalne w GameState (lista AchievementProgress)
- **10 osiagniec:**
  - Pierwsza krew (1 kill), Pogromca Potworow (20 kills), Lowca Bossow (5 bossow)
  - Glebiny (dotarcie do pietra 5), Bogacz (500 zlota)
  - Pomocnik (1 quest), Alkoholik (10 bimbrow), Weteran Pulapek (5 pulapek)
  - Runiczny Mistrz (1 puzzle), Pogromca Deliriusa (pokonanie finalbossa)
- **UI:** Przycisk "Osiagniecia" w HUD (pod "Dziennik"), panel po prawej stronie
- Notyfikacja przy odblokowaniu: "Osiagniecie odblokowane: {tytul}!"

### System Zagadek Logicznych (PuzzleData.cs + DungeonGenerator + PlayerController)
- **Runic Switches** na pietrach 3 i 4
- Po zabiciu wszystkich wrogow, zamiast schodow pojawiaja sie swiecace runy na podlodze
- Floor 3: 2 runy, Floor 4: 3 runy
- Gracz musi najsc na kazda rune (PuzzleSwitch -> PuzzleSwitchActive)
- Po aktywacji wszystkich run: schody pojawiaja sie (OnAllPuzzlesSolved -> PlaceStairsDownNow)
- Notyfikacje: "Runa aktywowana! (1/3)", "Wszystkie runy aktywne! Schody się pojawiły!"
- Deterministic pozycje: seed z `Dungeon.Seed ^ (floor * 7717 + 13)`
- Stan w DungeonData: `PuzzleSwitchesTotal`, `PuzzleSwitchesActivated`
- TileType: PuzzleSwitch (11) = nieaktywna, PuzzleSwitchActive (12) = aktywowana
- Sprite: nieaktywna = (c0,r22) gold key, aktywna = (c6,r16) ankh glow

### System Efektow Statusu (StatusEffect.cs + CombatEngine.cs + EnemyData.cs)
- **StatusEffectType:** Poison (HP DoT), Burn (HP DoT, mocniejszy), Slow (redukuje AP o 1)
- **StatusEffect class:** pure C#, Tick() aplikuje efekt i zwraca polski komunikat, IsExpired po RemainingTurns <= 0
- **EnemyData.TryApplyStatusEffect():** virtual metoda, base zwraca null, override w podklasach:
  - Utopiec: 20% szansa -> Poison (3 dmg/tura, 3 tury)
  - Strzyga: 25% szansa -> Slow (2 tury, -1 AP)
  - BossNekromanta: 30% szansa -> Poison (4 dmg/tura, 3 tury)
  - BossOgien: 35% szansa -> Burn (5 dmg/tura, 2 tury)
- **CombatEngine flow:**
  1. Poczatek enemy turn: tick wszystkich _activeEffects, usun expired
  2. Jesli HP <= 0 po tick: Defeat
  3. Po ataku wroga: TryApplyStatusEffect() -> dodaj do listy (ten sam typ: refresh, nie stack)
  4. Przy ustawianiu next player turn: jesli Slow aktywny -> AP -= 1
  5. EndCombat(): _activeEffects.Clear()
- Komunikaty po polsku: "TRUCIZNA zadaje X obrażeń!", "OGIEŃ pali ciało!", "SPOWOLNIENIE — mniej Wigoru w tej turze!"

### NPC System (NpcSpawner.cs)
- Floor 0 (lobby): Mirek Handlarz (x=26, y=10), sprite rogues.png (c4,r3)
- Floor 0 (lobby): Tajemniczy Jegomosc (x=1, y=10), sprite rogues.png (c6,r2)
  - Pierwsza interakcja: intro speech, odblokowanie schodow, questy
  - Kolejne: nagrody za questy, nowe zadania, postep
- Floor 5 + DeliriusDefeated: Wladca Podziemi (dynamiczna pozycja kilka kratek od miejsca walki z Deliriusem), sprite rogues.png (c3,r5)
  - Po wcisnieciu E: credits z imionami autorow, gra zamyka sie po 8 sek

### Lobby Stairs Gating
- Schody do pietra 1 ukryte do momentu rozmowy z Tajemniczym Jegomosciem
- Po intro: `state.QuestData.IntroCompleted = true` -> schody pojawiaja sie
- Runtime placement via `DungeonGenerator.PlaceStairsTile(sx, sy, isDown: true)`
- Przy ponownym ladowaniu: DungeonGenerator sprawdza `IntroCompleted` i rysuje schody

### Notification System (NotificationUIController.cs)
- Overlay na HUDCanvas w scenie Dungeon
- Fade in (0.3s) -> hold -> fade out (0.5s)
- fontSize=26, zloty kolor (#FFD700), ciemne tlo
- Auto-skalowanie czasu wyswietlania: `Mathf.Clamp(message.Length / 15f, 3f, 10f)`
- Automatycznie sprawdza przy Start():
  - ShowDeathMessage flag -> "Sprobujmy na druga nozke!"
  - DeliriusDefeated flag -> victory popup (teraz na floor 5, nie 0)
- Budowany przez ProjectSetupEditor.BuildNotificationOverlay()

### Generacja Lochu (DungeonGenerator.cs)
- Floor 0: hand-crafted lobby (28x22), stale
- Floors 1-5: BSP (Binary Space Partitioning), **36x26**, seed = `Dungeon.Seed + floor * 31337`
- BSP tuning w themes: minRoom 4-6, splitDepth 3
- Kill-gate: StairsDown pojawia sie dopiero po zabiciu wszystkich wrogow
- `FloorsCleared[]` -- boolean array w GameState, pietro nie respawnuje gdy cleared
- Po wygenerowaniu: `CameraFollow.SetBounds(W, H)` dla pieter 1-5, `CameraFollow.DisableBounds()` dla lobby
- Na starcie wywoluje `Resources.UnloadUnusedAssets()` -- zwalnia nazbierane Tile/Sprite
- Pulapki rozmieszczane na pietrach 2+ via `ScatterTraps()`
- Tile coords: trap deco = (1,22) w tiles.png

### Enemy Spawner (EnemySpawner.cs)
- Floors 1-4: 4 regular enemies + 1 floor boss
- Floor 5: only Delirius
- **Deterministic spawn positions:** RNG seedowany `state.Seed * 7919 + floor * 31` przed spawnem, przywracany po
- Static `defeatedIds` HashSet -- track zabitych wrogow, stable IDs = floor*10000 + x*100 + y
- MarkDefeated(stableId) -- wywolywany TYLKO po wygranej walce (GameManager.OnCombatWon)
- Po ucieczce/przegranej: wrogi zostaja na mapie z pelnym HP (EnemyFactory.Create tworzy od nowa)
- Kill-gate: gdy aliveEnemyCount==0 -> GameEvents.RaiseAllEnemiesDefeated -> schody
- `DebugClearFloor()` -- debug metoda (klawisz 0) zabija wszystko na pietrze

### Kamera (CameraFollow.cs)
- Orthographic, size **6.5**
- Start position (18, 13, -10) -- srodek mapy 36x26
- Bounds ustawiane DYNAMICZNIE przez DungeonGenerator po generacji mapy
- **Lobby (floor 0): bounds WYLACZONE** (DisableBounds()) -- mapa za mala na clamping
- Pietra 1-5: bounds wlaczone (SetBounds(w, h))

### System Zapisu (GameManager.cs)
- 3 sloty zapisu, JSON do `Application.persistentDataPath`
- `GameState` -> `PlayerData` (w tym `InventoryData`) + `DungeonData` + `QuestSaveData` + meta
- F5 = szybki zapis do slotu 1
- Pelna serializacja ekwipunku i questow
- Dodatkowe flagi w GameState:
  - `DeliriusDefeated` (bool) -- ustawiane po pokonaniu bossa floor 5
  - `ShowDeathMessage` (bool, w GameManager) -- ustawiane przy smierci gracza
  - `FloorsCleared[]` (bool[6]) -- ktore pietra sa wyczyszczone
  - `NextSpawnPoint` (enum SpawnPoint) -- AtEntrance / AtExit / Default
  - `QuestData` (QuestSaveData) -- lista questow, IntroCompleted, NextQuestIndex

### Combat Flow (GameManager.cs)
- `OnCombatWon()`: MarkDefeated(enemyId), RaiseEnemyKilled(name), player Y-=1, RaiseCombatEnded, load Dungeon
- `OnCombatLost()`: heal full HP, tox=0, set floor=0, ShowDeathMessage=true, load Dungeon (lobby)
- `OnCombatFled()`: just load Dungeon (enemy stays on map)
- `GoToNextFloor()`: if next > 5 -> stay on floor 5, set DeliriusDefeated=true, OnFloorReached(6), load Dungeon
- `StartNewGame()` / `LoadFromSlot()`: QuestManager.Initialize()
- `ReturnToMainMenu()`: QuestManager.Cleanup(), GameEvents.ClearAll()

---

## KLUCZOWE PLIKI -- MAPA ZALEZNOSCI

```
GameManager (Singleton, DontDestroyOnLoad)
  +-- CurrentGameState (GameState)
  |     +-- Player (PlayerData + InventoryData)
  |     +-- Dungeon (DungeonData)
  |     +-- QuestData (QuestSaveData)
  |     +-- DeliriusDefeated, FloorsCleared[]
  +-- PlayerInventory (Inventory -- runtime, rebuilt on new game/load)
  +-- ShowDeathMessage (bool)

QuestManager (static)
  +-- subscribes to OnEnemyKilled
  +-- AcceptQuest(), MarkRewarded(), GetVisibleQuests()
  +-- OnFloorReached(floor) -- auto-completes floor quests

GameEvents (static Observer hub)
  +-- OnDungeonGenerated -> EnemySpawner, NpcSpawner, PlayerController, DungeonGenerator
  +-- OnAllEnemiesDefeated -> DungeonGenerator (places StairsDown)
  +-- OnCombatEnded -> PlayerController, DungeonGenerator
  +-- OnInventoryChanged -> InventoryUIController, ShopUIController
  +-- OnGoldChanged (new total) -> InventoryUIController.RefreshGold, ShopUIController.RefreshGold
  +-- OnLootDropped -> GameManager.HandleLootDropped -> PlayerInventory.AddItem
  +-- OnNotification -> NotificationUIController (HUD notification text)
  +-- OnEnemyKilled(name) -> QuestManager.OnEnemyKilled
  +-- OnQuestAccepted/Progress/Completed -> (future UI hooks)
  +-- OnQuestJournalChanged -> QuestJournalUIController.Refresh
  +-- OnTrapTriggered(x, y) -> (future trap effects)

CombatEngine (pure C#)
  +-- receives PlayerData + EnemyData
  +-- executes ICombatAction (Strategy pattern)
  +-- _activeEffects: List<StatusEffect> (Poison, Burn, Slow)
  +-- returns CombatTurnResult (GoldGained, XPGained, LootItem)

ProjectSetupEditor [Editor only]
  +-- Builds all 4 scenes from scratch with all references wired
      Menu: Tools -> Lochy i Gorzala -> Setup Project
```

---

## WROGOWIE (EnemyFactory.cs)

| Nazwa | Level | HP | Atak | Def | XP | Gold | Slabosc | Status Effect | Uwagi |
|---|---|---|---|---|---|---|---|---|---|
| Chochlik | 1 | 25 | 7 | 2 | 15 | 25 | Silver | -- | -- |
| Utopiec | 2 | 45 | 10 | 6 | 30 | 40 | Fire | 20% Poison (3dmg, 3t) | 20% grab +40% dmg |
| Strzyga | 3 | 55 | 14 | 5 | 50 | 70 | Holy | 25% Slow (2t) | Regen 2HP/hit (bez Holy) |
| Bliźniacze Dziwadła | 3 | 110 | 16 | 12 | 120 | 130 | Fire | -- | Floor 1, 25% crush 1.8x |
| Boss Nekromanta | 4 | 130 | 18 | 8 | 160 | 180 | Holy | 30% Poison (4dmg, 3t) | Floor 2, drain 4HP/hit |
| Baba Jaga | 5 | 150 | 22 | 10 | 200 | 230 | Holy | -- | Floor 3, lifesteal 8HP/atk |
| Ognisty Diabeł | 6 | 150 | 20 | 9 | 260 | 280 | Holy | 25% Burn (4dmg, 2t) | Floor 4, immune Fire, 25% 1.6x |
| DELIRIUS | 10 | 200 | 22->26 | 10->12 | 999 | 600 | Holy | -- | Floor 5, phase 2 at 50% HP |

### Delirius Phase 2 (po spadku do 50% HP):
- Attack: 22 -> 26 (bylo 32, znerfione ponownie v3)
- Defense: 10 -> 12 (bylo 14, znerfione ponownie v3)
- Chaos Surge: 25% szansa na 1.5x dmg (bylo 30%/1.8x -- znerfione v3)
- Phase 1: 15% Chaos Surge 1.3x, Physical dmg deals only 50% (Holy omija te redukcje)
- Phase 2: Normal damage, Chaos Surge max ~39 dmg
- Z Blogoslwionym Mieczem (Holy, +12 atak) gracz daje rade

### Sprite Positions (monsters.png 384x416 = 12c x 13r):
- Chochlik: (c2, r0)
- Utopiec: (c5, r4)
- Strzyga: (c0, r5)
- Bliźniacze Dziwadła: (c3, r2)
- BossNekromanta: (c3, r5)
- Baba Jaga: (c4, r5)
- BossOgien: (c7, r7)
- Delirius: (c1, r6)

---

## ASSETS -- SPRITE SHEETS

Wszystkie z `Assets/Art/32rogues-0.5.0/32rogues/` (32px per cell, 0-indexed):

| Plik | Uzycie |
|---|---|
| `rogues.png` (7c x 7r) | Gracz (Wojownik r1c1, Lucznik r0c2, Mag r4c1), Mirek (r3c4), Tajemniczy Jegomosc (r2c6), Wladca Podziemi (r5c3) |
| `monsters.png` (12c x 13r) | Wrogowie (patrz tabela powyzej) |
| `tiles.png` | Tilemap lochow (5 tematow pieter), trap deco (c1,r22) |
| `items.png` (11c x 26r) | Przedmioty, ekwipunek, mikstury |

### Combat Backgrounds:
`Assets/Resources/CombatBackgrounds/` (tez kopia w `Assets/Art/CombatBackgrounds/`):
- `combat_bg_floor1.png` (960x540) -- niebiesko-szara jaskinia
- `combat_bg_floor2.png` (960x540) -- zielona jaskinia z mchem
- `combat_bg_floor3.png` (960x540) -- fioletowa jaskinia
- `combat_bg_floor4.png` (960x540) -- pomaranczowo-czerwona lawowa jaskinia
- `combat_bg_floor5.png` (960x540) -- ciemnogranatowa jaskinia

### Uzyte pozycje sprite w items.png:
**Weapons:** (c0,r0) dagger, (c3,r0) long sword, (c1,r3) battle axe, (c1,r8) spiked club, (c6,r0) sanguine dagger, (c8,r0) crystal sword, (c1,r5) mace, (c0,r9) crossbow, (c6,r10) flame staff, (c1,r10) holy staff, (c5,r0) zweihander, (c9,r0) blessed sword
**Armor:** (c1,r12) leather, (c4,r11) round shield, (c1,r14) boots, (c3,r12) chainmail, (c2,r11) cross shield, (c7,r15) helm, (c5,r12) plate, (c3,r11) dark shield, (c4,r12) reinforced mail
**Accessories:** (c0,r16) red pendant, (c1,r16) metal pendant, (c3,r17) ruby ring, (c4,r17) sapphire ring, (c6,r16) ankh, (c0,r17) emerald ring, (c2,r16) crystal pendant
**Consumables:** (c1,r19) red potion, (c2,r19) brown vial, (c1,r25) bread, (c3,r25) beer, (c0,r19) purple potion, (c4,r19) green potion, (c3,r20) blue potion, (c4,r20) orange potion, (c0,r20) black potion, (c2,r20) pink vial, (c1,r21) wielki eliksir, (c5,r21) boski lek na kaca
**Keys:** (c0,r22) gold key, (c1,r22) ornate key

---

## GAME EVENTS -- PELNA LISTA

```csharp
// Flow
OnGameStarted, OnGamePaused, OnGameResumed, OnSceneChangeRequested(string)
// Player
OnPlayerHealthChanged(int current, int max), OnPlayerXPGained(int), OnPlayerLevelUp(int)
OnToxicityChanged(float)
// Combat
OnCombatStarted, OnCombatEnded, OnDamageDealt(string name, int amount), OnPlayerDamaged(int)
OnEnemyKilled(string enemyName)
// Dungeon
OnRoomEntered(int), OnDungeonGenerated, OnAllEnemiesDefeated
OnTrapTriggered(int x, int y)
// Inventory
OnInventoryChanged, OnLootDropped(ItemData), OnGoldChanged(int newTotal)
// Quest
OnQuestAccepted(QuestData), OnQuestProgress(QuestData), OnQuestCompleted(QuestData)
OnQuestJournalChanged
// Puzzle
OnPuzzleSwitchActivated(int activated, int total)
OnAllPuzzlesSolved
// Achievements
OnAchievementUnlocked(AchievementData)
OnAchievementsChanged
// UI
OnNotification(string)
```

---

## ZNANE OGRANICZENIA / UWAGI

1. **Font:** LiberationSans SDF -- polskie litery (aoceelnosszz) dzialaja, emoji NIE
2. **Serializacja:** Unity `JsonUtility` nie obsluguje polimorfizmu -- ItemData jest concrete class
3. **Input:** Projekt uzywa **nowego Input System** (`UnityEngine.InputSystem.Keyboard`) -- zakaz `Input.GetKeyDown`
4. **Unity 6 deprecated:** Uzyj `GetHashCode()` zamiast `GetInstanceID()`, `FindAnyObjectByType<T>()` zamiast `FindFirstObjectByType`
5. **Sceny:** Po kazdej wiekszej zmianie kodu nalezy przebudowac: `Tools -> Lochy i Gorzala -> Setup Project`
6. **Gold:** Jedynym miejscem dodawania zlota po walce jest `CombatUIController.ProcessTurn` (Victory case)
7. **Spawn po walce:** `GameManager.OnCombatWon` przesuwa gracza Y-1 przed powrotem do Dungeon
8. **Enemy despawn bug NAPRAWIONY:** RNG w EnemySpawner jest teraz seedowany deterministycznie -- spawn positions sa identyczne miedzy reloadami sceny
9. ~~**Diagnostic logs `[FREEZE-*]`**~~ -- USUNIETE
10. **Font size:** Rozmiary fontow zostaly zwiekszone o +2 pkt we wszystkich UI kontrolerach
11. **Combat backgrounds:** Ladowane z Resources/ via Resources.Load -- wymagaja ze PNG sa w `Assets/Resources/CombatBackgrounds/`
12. **Status effects:** Poison/Burn/Slow tickuja na poczatku enemy turn -- moga zabic gracza (Defeat check). Slow redukuje AP o 1. Ten sam typ efektu nie stackuje -- refresh duration.

---

## SZYBKIE KOMENDY DLA AI

- Przebuduj sceny: `Tools -> Lochy i Gorzala -> Setup Project` (w Unity) -- **konieczne po kazdej zmianie kodu dotyczacej scen, kamer, prefabow**
- Projekt jest w: `Assets/Scripts/` podzielony na namespace LochyIGorzala.*
- Glowny edytor scen: `Assets/Scripts/Editor/ProjectSetupEditor.cs`
- Sprite pozycje: patrz sekcja ASSETS powyzej
- Input: `Keyboard.current.xKey.wasPressedThisFrame` (NIE `Input.GetKeyDown`)
- FindObject: `FindAnyObjectByType<T>(FindObjectsInactive.Include)` (NIE deprecated)
- GetInstanceID -> `GetHashCode()` w Unity 6 (GetEntityId tez deprecated)
- **Mapa:** 36x26 na piectra 1-5, Lobby 28x22
- **Kamera:** orthoSize 6.5, bounds dynamiczne (SetBounds/DisableBounds)
- Po zmianach kodu: ZAWSZE przebuduj sceny!
