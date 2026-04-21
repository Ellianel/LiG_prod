# TODO -- Lochy & Gorzala
> Aktualizuj ten plik po kazdej sesji.
> Status: [ ] = nie zrobione, [x] = zrobione, [~] = czesciowo
> Ostatnia aktualizacja: 2026-04-20

---

## CEL: Ocena 5.0 z przedmiotu

---

## POZIOM PODSTAWOWY (Ocena 3.0) -- STATUS: DONE

- [x] Grywalna postac + system walki turowej (3 PA, akcje)
- [x] Min. 3 typy przeciwnikow -- mamy 8 (3 regularne + 5 bossow)
- [x] System mapy -- 5 pieter BSP + lobby
- [x] Ekwipunek i statystyki postaci (HP, Atak, Obrona, XP, Level)
- [x] Serializacja JSON -- 3 sloty zapisu/odczytu

---

## POZIOM ROZSZERZONY (Ocena 4.0) -- STATUS: DONE

- [x] Proceduralna generacja lochu (BSP)
- [x] System klas postaci (Wojownik, Lucznik, Mag) z unikalnymi specjalami
- [x] Mechanika efektow statusow (Toksycznosc + DoT co 2 tury)
- [x] Efekty czasowe -- Trucizna (Utopiec, Nekromanta), Ogien (BossOgien), Spowolnienie (Strzyga) + Tox od bimbru
- [x] Interaktywne NPC -- Mirek Handlarz + Tajemniczy Jegomosc + Wladca Podziemi
- [x] System waluty -- zloto z wrogow, sklep z kupnem/sprzedaza

---

## POZIOM ZAAWANSOWANY (Ocena 5.0) -- STATUS: W TRAKCIE

- [x] **QUESTY + Dziennik misji** -- ZROBIONE
  - QuestData, QuestManager, QuestJournalUIController
  - 3 kill questy (Chochliki, Strzygi, Utopce) + 3 floor questy
  - UI: przycisk "Dziennik" w HUD, panel z lista questow
  - Tajemniczy Jegomosc NPC daje questy i nagrody

- [x] **Pulapki (Traps)** -- ZROBIONE
  - TrapData z 3 typami: Spikes (-15HP), MagicDrain (+20 Tox), Teleport (do wejscia)
  - Losowe rozmieszczanie na pietrach 2+
  - Deterministic typ via hash koordynatow

- [x] **Tematyczne tla walki** -- ZROBIONE
  - 5 unikatowych tel PNG per pietro (960x540)
  - Ladowane dynamicznie z Resources/ w CombatUIController

- [x] **Zagadki logiczne (Runic Switches)** -- ZROBIONE
  - PuzzleData.cs (pure C#): config, ktore pietra maja puzzle, ile switchow
  - Floor 3: 2 runy, Floor 4: 3 runy -- po zabiciu wrogow pojawiaja sie runy
  - Gracz musi najsc na kazda rune zeby aktywowac -- potem pojawiaja sie schody
  - Nowe TileType: PuzzleSwitch (11), PuzzleSwitchActive (12)
  - Eventy: OnPuzzleSwitchActivated, OnAllPuzzlesSolved
  - Stan w DungeonData: PuzzleSwitchesTotal, PuzzleSwitchesActivated

- [ ] **Unit testy (xUnit)** -- BRAKUJE projektu testowego
  - Osobny projekt `.csproj` w solucji `.slnx`
  - Testowac: CombatEngine, LootSystem, Inventory, EnemyFactory
  - Cel: min. 15 testow pokrywajacych logike biznesowa

- [x] **System rzadkosci lootu (Common/Rare/Epic)** -- ZROBIONE

- [x] **System osiagniec (Achievements)** -- ZROBIONE
  - AchievementData.cs (pure C#): 10 osiagniec, AchievementSaveData, AchievementDatabase
  - AchievementManager.cs (static): subskrybuje OnEnemyKilled, OnGoldChanged, OnQuestCompleted, OnTrapTriggered, OnAllPuzzlesSolved, OnToxicityChanged
  - AchievementsUIController.cs: panel toggle "Osiagniecia" w HUD (prawy panel)
  - Eventy: OnAchievementUnlocked, OnAchievementsChanged
  - Osiagniecia: Pierwsza krew, Pogromca Potworow, Lowca Bossow, Glebiny, Bogacz, Pomocnik, Alkoholik, Weteran Pulapek, Runiczny Mistrz, Pogromca Deliriusa

---

## NAPRAWIONE W SESJACH 2026-04

- [x] Combat UI: mniejsze przyciski akcji (125x34, autosize font 12-17), panel wiekszy (4 rzedy)
- [x] HUD: Dziennik i Osiagniecia obok siebie (nie nakladaja sie), mniejsze (120x36)
- [x] Ekwipunek: szerszy przycisk (140x38) z autosize (12-16), tekst sie miesci
- [x] Tajemniczy Jegomosc: rozbudowana wiadomosc powitalna z informacja o grze
- [x] Paski HP: natychmiastowa aktualizacja po akcji (UpdateUI po ExecutePlayerAction)
- [x] Paski HP: fillAmount = 1f na starcie w edytorze
- [x] Rename: Gargulec Trupiooki -> Blizniacze Dziwadla, Lord Wampirow -> Baba Jaga
- [x] Nerf Ognisty Diabel: 170->150 HP, 24->20 atk, 11->9 def, 35%->25% inferno 2x->1.6x, burn 5->4 dmg
- [x] Nerf Delirius: 220->200 HP, 26->22 atk, 10 def, phase 2: 26 atk/12 def, surge 25%/1.5x
- [x] Wladca Podziemi: spawn kilka kratek od Deliriusa (nie przy wejsciu), brak schodow na floor 5
- [x] GameManager.OnCombatWon: wykrywa zabicie Deliriusa i ustawia flage natychmiast
- [x] CS0618 warningi GetInstanceID -> GetHashCode (SpriteSheetHelper x2, DungeonGenerator x1)
- [x] Spawn gracza w scianie (hardcoded 5,5) -> spiral search + entrance coords
- [x] Brak sprite bossa (BossGargulec, BossOgien) -> naprawione pozycje sprite
- [x] Wrog znika przed walka -> RemoveEnemyAtPosition usuniety, MarkDefeated tylko po wygranej
- [x] Po smierci gracza: respawn w lobby z komunikatem "Sprobujmy na druga nozke!" (auto-skalowany czas)
- [x] Po pokonaniu Deliriusa: gracz zostaje na floor 5, spawn Wladca Podziemi, credits + zamkniecie gry
- [x] Polskie litery (diacrtics) we wszystkich tekstach gry
- [x] Wieksze fonty (+2 pkt we wszystkich UI)
- [x] NotificationUIController -- nowy system powiadomien z fade in/out, auto-skalowanie czasu
- [x] Boss respawn bug -- RNG seedowany deterministycznie w EnemySpawner
- [x] Delirius za mocny -- znerfiony phase 2 (atk 40->32, surge 2.5x->1.8x)
- [x] Epickie eq u Mirka (80-100zl) -- Blogoslawiony Miecz, Wzmocniona Kolczuga, Wielki Eliksir
- [x] Kamera w lobby centrowana zamiast sledzic gracza -> DisableBounds() na floor 0
- [x] Mapa 60x60 -> 36x26, BSP themes dostrojone
- [x] Camera bounds hardcoded -> dynamiczne SetBounds
- [x] OrthoSize 8 -> 6.5
- [x] Enemy spawn distance za duze -> dostrojone do 36x26
- [x] Asset accumulation freeze -> Resources.UnloadUnusedAssets
- [x] FindFirstObjectByType deprecated -> FindAnyObjectByType
- [x] CS0414 _selectedIsSell -> pragma warning disable
- [x] Lobby stairs gated behind NPC intro
- [x] Quest system z kill tracking via OnEnemyKilled
- [x] Floor questy widoczne w dzienniku po Rewarded
- [x] Combat backgrounds per floor (5 PNG, Resources.Load)
- [x] Debug skip level (klawisz 0)
- [x] Status effects turowe -- Poison (Utopiec 20%, Nekromanta 30%), Burn (BossOgien 35%), Slow (Strzyga 25%)

---

## ARCHITEKTURA / KOD -- DO NAPRAWY

- [ ] **Unit testy** -- stworz projekt `LochyIGorzala.Tests` z xUnit
- [x] **Status effects system** -- Poison/Burn/Slow zaimplementowane w CombatEngine + EnemyData.TryApplyStatusEffect()
- [x] **ReturnToMainMenu event bug** -- NAPRAWIONE: re-subscribe OnLootDropped po ClearAll()
- [x] **Usunac diagnostic logi `[FREEZE-*]`** -- USUNIETE z GameManager, CombatUIController
- [x] **Usunac debug skip (klawisz 0)** -- USUNIETE z PlayerController.Update()

---

## UI / GAMEPLAY -- DO POPRAWY

- [ ] **Animacje** -- nie ma animacji postaci/wrogow
- [ ] **Sound** -- brak dzwiekow
- [ ] **Minimap** -- byloby mile
- [ ] **Level up screen** -- brak wizualnego feedbacku (tylko tekst w combat logu)

---

## NASTEPNE KROKI (priorytet dla 5.0)

### ~~1. Zagadki logiczne~~ -- ZROBIONE (Runic Switches)
```
Nowe pliki:
  Assets/Scripts/Dungeon/PuzzleData.cs  -- pure C# config (FloorHasPuzzle, GetSwitchCount, messages PL)
Zmodyfikowane:
  TileType.cs          -- PuzzleSwitch=11, PuzzleSwitchActive=12
  GameEvents.cs        -- OnPuzzleSwitchActivated, OnAllPuzzlesSolved
  GameState.cs         -- DungeonData: PuzzleSwitchesTotal, PuzzleSwitchesActivated
  DungeonGenerator.cs  -- ScatterPuzzleSwitches(), HandleAllPuzzlesSolved(), rendering
  PlayerController.cs  -- CheckForPuzzleSwitch()
```

### 2. Testy xUnit (wymagane do 5.0)
```
Stworz: LochyIGorzala.Tests/LochyIGorzala.Tests.csproj
Testuj:
  - CombatEngineTests: damage calc, AP management, flee chance
  - InventoryTests: equip/unequip stat changes, full bag
  - LootSystemTests: rarity weights per floor, boss guarantees
  - EnemyFactoryTests: correct stats, weakness types
  - QuestManagerTests: accept, progress, complete, reward flow
  Cel: min. 15 testow
```

### ~~3. System osiagniec~~ -- ZROBIONE
```
Nowe pliki:
  Assets/Scripts/Core/AchievementData.cs         -- pure C# definicje, baza 10 osiagniec
  Assets/Scripts/Managers/AchievementManager.cs   -- static manager, event tracking
  Assets/Scripts/UI/AchievementsUIController.cs   -- UI panel z lista osiagniec
Zmodyfikowane:
  GameEvents.cs           -- OnAchievementUnlocked, OnAchievementsChanged
  GameState.cs            -- AchievementSaveData w GameState
  GameManager.cs          -- Initialize/Cleanup/OnFloorReached
  ProjectSetupEditor.cs   -- BuildAchievementsPanel() + przycisk "Osiagniecia"
```

### ~~4. Status effects turowe~~ -- ZROBIONE
```
Nowe pliki:
  Assets/Scripts/Combat/StatusEffect.cs  -- StatusEffectType enum + StatusEffect class (Tick, IsExpired)
Zmodyfikowane:
  EnemyData.cs      -- virtual TryApplyStatusEffect() (base returns null)
  EnemyFactory.cs   -- override w Utopiec (Poison 20%), Strzyga (Slow 25%), BossOgien (Burn 35%), BossNekromanta (Poison 30%)
  CombatEngine.cs   -- _activeEffects list, tick at enemy turn start, apply Slow (-1 AP), clear in EndCombat()
```

---

## SZYBKIE KOMENDY DLA AI

- Przebuduj sceny: `Tools -> Lochy i Gorzala -> Setup Project` (w Unity)
- Projekt: `Assets/Scripts/` namespace LochyIGorzala.*
- Glowny edytor scen: `Assets/Scripts/Editor/ProjectSetupEditor.cs`
- Sprite pozycje: patrz `project_context.md` sekcja ASSETS
- Input: `Keyboard.current.xKey.wasPressedThisFrame`
- FindObject: `FindAnyObjectByType<T>(FindObjectsInactive.Include)`
- GetInstanceID -> `GetHashCode()` w Unity 6
- Mapa: 36x26 (pietra 1-5), Lobby 28x22
- Kamera: orthoSize 6.5, bounds dynamiczne
- Po zmianach kodu: ZAWSZE przebuduj sceny!
- ~~Debug: klawisz 0 = skip level~~ -- USUNIETY
