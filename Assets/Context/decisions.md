# DECISIONS -- Lochy & Gorzala
> Dlaczego cos zostalo zrobione tak a nie inaczej.
> Kazda decyzja ma kontekst -- piszemy tu zeby nie pytac po raz drugi.
> Aktualizacja: 2026-04-20 (v3 -- UI fixes, renames, nerfs, Wladca spawn)

---

## ARCHITEKTURA

### Dlaczego logika nie dziedziczy po MonoBehaviour?
**Decyzja:** `CombatEngine`, `EnemyData`, `Inventory`, `ItemData`, `QuestData`, `QuestManager` itd. sa pure C#.
**Powod:** Wymaganie prowadzacego (separacja warstw). Pure C# klasy moga byc testowane przez xUnit bez Unity runtime.

### Dlaczego GameEvents jest static?
**Decyzja:** `GameEvents` to statyczna klasa z C# events (nie ScriptableObject/UnityEvent).
**Powod:** Prostosc. Brak potrzeby referencji. Implementuje wzorzec Observer bez boilerplate. Wada: koniecznosc wywolywania `ClearAll()` przy powrocie do menu.

### Dlaczego ItemData nie jest abstract?
**Decyzja:** `ItemData` jest concrete class implementujaca 3 interfejsy.
**Powod:** Unity `JsonUtility` nie obsluguje polimorfizmu przy serializacji. ItemType enum determinuje zachowanie runtime.

### Dlaczego InventoryData jest DTO (tylko stringi ItemIds)?
**Decyzja:** `InventoryData` przechowuje `List<string> ItemIds` zamiast `List<ItemData>`.
**Powod:** Serializacja. ItemId jako string + `ItemDatabase.Get(id)` to lookup O(1) bez duplikacji danych.

### Dlaczego QuestManager jest static (nie MonoBehaviour)?
**Decyzja:** `QuestManager` to statyczna klasa, inicjalizowana przez `GameManager.StartNewGame()` / `LoadFromSlot()`.
**Powod:** Questy to czysta logika -- nie potrzebuja Update() ani coroutines. Static = latwiejszy dostep z dowolnego miejsca. Cleanup() przy powrocie do menu odlacza eventy.

---

## GOLD SYSTEM

### Dlaczego CombatUIController jest jedynym miejscem dodawania zlota po walce?
**Decyzja:** Gold jest dodawany TYLKO w `CombatUIController.ProcessTurn` (Victory case).
**Powod:** Wczesniejsza architektura miala podwojne zloto (CombatEngine + CombatUIController). Naprawione: CombatEngine nie wywoluje RaiseGoldChanged. `OnGoldChanged` jest teraz wylacznie notyfikacja UI.

---

## EKWIPUNEK / INVENTORY

### Dlaczego EnsureSlotPool ma flage `_slotPoolReady`?
**Decyzja:** Pool slotow buduje sie dokladnie raz per instancja MonoBehaviour.
**Powod:** Bez flagi lista rosla: 20->40->60. Itemy byly ustawiane a potem czyszczone przez duplikat. Efekt: puste sloty mimo posiadania itemow.

### Dlaczego event subscriptions uzywaja named methods (nie lambda)?
**Decyzja:** `GameEvents.OnGoldChanged += OnGoldChangedHandler` zamiast `+= _ => RefreshGold()`.
**Powod:** Lambda tworzy nowa instancje delegata -- `-=` w `OnDisable()` nie odpina handlera. Named method = poprawna sub/unsub.

---

## QUEST SYSTEM

### Dlaczego quest flow wymaga rozmowy z NPC?
**Decyzja:** Gracz musi porozmawiaj z Tajemniczym Jegomosciem zeby zaczac gre (odblokowanie schodow + questy).
**Powod:** Daje narracje do gry (intro speech), uczy gracza interakcji z NPC, gating zapobiega wejsciu na pietra bez kontekstu fabularnego.

### Dlaczego schody w lobby sa ukryte do rozmowy z NPC?
**Decyzja:** DungeonGenerator sprawdza `QuestData.IntroCompleted` -- jesli false, stawia Floor tile zamiast StairsDown.
**Powod:** Gracz MUSI porozmawiac z NPC zeby zaczac. Runtime placement via `PlaceStairsTile()` po pierwszej interakcji.

### Dlaczego floor questy sa osobnym typem (QuestType.Floor)?
**Decyzja:** Floor questy maja `TargetFloor` zamiast `TargetEnemyName/RequiredKills`.
**Powod:** Auto-complete na wejsciu na pietro (OnFloorReached). Nie wymagaja powrotu do NPC -- sa self-rewarding (Status = Rewarded od razu). Zapelniaja dziennik misji od samego poczatku.

### Dlaczego floor questy maja Status = Rewarded (nie Completed)?
**Decyzja:** `OnFloorReached()` ustawia Completed a potem natychmiast Rewarded.
**Powod:** Floor questy nie maja NPC return. GetVisibleQuests() specjalnie uwzglednia `Type == Floor && Status == Rewarded` zeby te questy zostaly w dzienniku.

### Dlaczego kill quest rewards to konkretne itemy (nie zloto)?
**Decyzja:** Nagrody: healing_potion, antidote, zmijowa_nalewka.
**Powod:** Gracz dostaje uzyteczne consumables ktore pomagaja na kolejnych pietrach. ID itemu weryfikowane z ItemDatabase.

### Dlaczego Tajemniczy Jegomosc jest w lewej niszy (x=1, y=10)?
**Decyzja:** Stala pozycja `JegomoscX=1, JegomoscY=10`.
**Powod:** Lewa nisza lobby. Mirek jest w prawej niszy (26,10). Gracz widzi obydwu NPC po wejsciu do lobby.

---

## TRAP SYSTEM

### Dlaczego pulapki tylko od pietra 2+?
**Decyzja:** `ScatterTraps()` wstawia pulapki tylko na `CurrentFloor >= 2`.
**Powod:** Floor 1 to introduction, gracz uczy sie walki. Floor 2+ dodaje pulapki jako dodatkowe wyzwanie.

### Dlaczego typ pulapki jest deterministyczny (hash koordynatow)?
**Decyzja:** `TrapData.GetTrapType(x, y)` uzywa hash koordynatow do deterministic wyboru.
**Powod:** Ten sam tile zawsze daje ten sam typ pulapki -- konsystencja miedzy reloadami. Bez zapisywania typu w save data.

### Dlaczego pulapka Teleport przenosi do wejscia?
**Decyzja:** Teleport ustawia pozycje gracza na `StairsUp` (wejscie pietra).
**Powod:** Kara za wdepniecie -- gracz musi przejsc pietro od poczatku. Nie traci HP/tox, wiec jest inna niz Spikes/MagicDrain.

---

## ACHIEVEMENT SYSTEM

### Dlaczego AchievementManager jest static (nie MonoBehaviour)?
**Decyzja:** `AchievementManager` to statyczna klasa, inicjalizowana przez `GameManager.StartNewGame()` / `LoadFromSlot()`.
**Powod:** Wzorzec identyczny z QuestManager. Osiagniecia to czysta logika -- nie potrzebuja Update(). Static = latwiejszy dostep. Cleanup() przy powrocie do menu odlacza eventy.

### Dlaczego bimber tracking uzywa OnToxicityChanged (nie osobnego eventu)?
**Decyzja:** AchievementManager subskrybuje `OnToxicityChanged` i wykrywa wzrost toksycznosci.
**Powod:** Bimber i nalewka zawsze zwiekszaja toksycznosc. Nie trzeba nowego eventu -- rising-edge detection (newTox > lastTox) jest wystarczajacy. `_lastToxicity` synchronizowany przy Initialize via `SyncToxicity()`.

### Dlaczego 10 osiagniec (nie wiecej)?
**Decyzja:** 10 osiagniec pokrywajacych rozne systemy gry.
**Powod:** Wystarczajaco duzo na demonstracje systemu. Kazde osiagniecie uzywa innego eventu -- pokazuje elastycznosc Observer pattern. Wiecej byloby powtorzeniem.

### Dlaczego panel osiagniec po prawej stronie (nie lewej)?
**Decyzja:** Achievements panel anchored na 60-98% szerokosci ekranu.
**Powod:** Dziennik misji jest po lewej (0-35%). Oba panele moga byc otwarte jednoczesnie bez nakladania sie.

---

## PUZZLE SYSTEM (Runic Switches)

### Dlaczego puzzle tylko na pietrach 3-4?
**Decyzja:** `PuzzleData.FloorHasPuzzle()` zwraca true tylko dla floor 3 i 4.
**Powod:** Floor 1-2 to intro -- gracz uczy sie walki i pulapek. Floor 3-4 dodaje puzzle jako dodatkowe wyzwanie. Floor 5 to boss Delirius -- puzzle by odrywaly od klimatu finalu.

### Dlaczego runy pojawiaja sie PO zabiciu wrogow (nie od razu)?
**Decyzja:** `HandleAllEnemiesDefeated()` na puzzle floors wywoluje `ScatterPuzzleSwitches()` zamiast `PlaceStairsDownNow()`.
**Powod:** Dwuetapowy gate: najpierw zabij wrogow, potem rozwiaz puzzle. Runy nie moga byc od poczatku bo kolidowalyby ze spawnem wrogow i pulapek. Sekwencyjnosc daje jasny feedback graczowi.

### Dlaczego 2 runy na floor 3 i 3 na floor 4?
**Decyzja:** `PuzzleData.GetSwitchCount()` zwraca 2 i 3.
**Powod:** Progresja trudnosci. 2 runy sa proste do znalezienia, 3 wymagaja wiecej eksploracji. Wiecej niz 3 byloby frustrujace na mapie 36x26.

### Dlaczego PuzzleSwitch i PuzzleSwitchActive to osobne TileType?
**Decyzja:** Dwa stany: PuzzleSwitch (11, nieaktywna) i PuzzleSwitchActive (12, aktywna).
**Powod:** Unika potrzeby dodatkowej struktury danych. Tile type sam w sobie jest stanem. PlayerController sprawdza == PuzzleSwitch zeby zapobiec re-triggerowaniu aktywnych run. Oba typy sa walkable.

---

## STATUS EFFECTS SYSTEM

### Dlaczego status effects sa w CombatEngine (nie osobnym managerze)?
**Decyzja:** `_activeEffects` lista bezposrednio w CombatEngine, tick w ExecutePlayerAction().
**Powod:** Efekty sa scisle zwiazane z turami walki. Dodanie osobnego managera byloby overengineering -- lista jest czyszczona w EndCombat(), nie potrzebuje persystencji.

### Dlaczego efekty nie stackuja (ten sam typ refreshuje)?
**Decyzja:** `_activeEffects.RemoveAll(e => e.Type == newEffect.Type)` przed dodaniem nowego.
**Powod:** Stackowanie Poison bylby za mocne (Utopiec + Nekromanta = 7 dmg/tura). Refresh duration jest fair -- gracz ma czas na heal miedzy tickami.

### Dlaczego Slow redukuje AP o 1 (nie o 50%)?
**Decyzja:** `Player.ActionPoints -= 1` jesli slowed, ale minimum 1 AP.
**Powod:** Gracz z 3 AP traci 1 (ma 2). Moze dalej atakowac LightAttack + cos. 50% by bylo 1.5 = zaokraglenie problemowe. Lucznik z 4 AP traci 1 = 3, dalej silny.

### Dlaczego Chochlik, BossGargulec, BossWampir i Delirius nie maja status effectow?
**Decyzja:** TryApplyStatusEffect() zwraca null (base implementation).
**Powod:** Chochlik to slaby enemy (floor 1). Gargulec i Wampir juz maja swoje unikalne mechaniki (crush, lifesteal). Delirius ma phase system -- dodatkowy status effect byby za duzo.

### Dlaczego status effects tickuja na POCZATKU enemy turn (nie koncu)?
**Decyzja:** Tick effects -> check death -> enemy attack -> apply new effect.
**Powod:** Gracz widzi efekty PRZED atakiem wroga. Jesli tick zabije gracza, nie dostaje dodatkowego ataku. Nowy efekt aplikowany PO ataku = gracz ma pelna ture na reakcje (heal/flee) nastepna ture.

---

## COMBAT BACKGROUNDS

### Dlaczego tla walki ladowane z Resources/?
**Decyzja:** PNG w `Assets/Resources/CombatBackgrounds/`, ladowane via `Resources.Load<Texture2D>()`.
**Powod:** Combat scene jest budowana programatycznie (ProjectSetupEditor). Nie mozna uzyc AssetDatabase w runtime. Resources.Load to standard Unity dla runtime asset loading.

### Dlaczego 5 osobnych obrazkow (nie jeden z parametrami)?
**Decyzja:** Kazdy floor ma dedykowany PNG 960x540.
**Powod:** Rozne kolory i tekstury per floor (blue, green, violet, orange, indigo). Generowane proceduralnie przez Python PIL z gradientami i szumem -- kazdy wyjatkowy.

### Dlaczego lobby nie ma tla walki?
**Decyzja:** Floor 0 -> domyslny ciemny kolor (0.08, 0.06, 0.1).
**Powod:** W lobby nie ma walk (brak wrogow). Fallback na wypadek edge case.

---

## NPC SYSTEM

### Dlaczego Mirek jest zawsze na pozycji (26, 10)?
**Decyzja:** Stala pozycja `const float MirekX = 26f, MirekY = 10f`.
**Powod:** Lobby jest hand-crafted. `GenerateLobby()` zawsze wycina prawa nisze `CarveRoom(d, 25, 8, 3, 6)`. Pozycja (26,10) jest centrum tej niszy i zawsze jest Floor tile.

### Dlaczego Wladca Podziemi na dynamicznej pozycji (nie stalej)? (UPDATED v3)
**Decyzja:** `SpawnWladcaPodziemi(float posX, float posY)` przyjmuje parametry pozycji. Pozycja = player.PositionX+3, player.PositionY (kilka kratek od Deliriusa).
**Powod:** Wladca spawni sie na pietrze 5 blisko miejsca walki z Deliriusem, nie przy wejsciu. Gracz widzi Wladce natychmiast po powrocie z walki, bez szukania.

### Dlaczego na pietrze 5 nie ma schodow? (NEW v3)
**Decyzja:** `HandleAllEnemiesDefeated()` na floor >= 5 nie stawia StairsDown. DeliriusDefeated ustawiane w `OnCombatWon()`.
**Powod:** Po pokonaniu Deliriusa nie ma sensu schodzic nizej. Wladca Podziemi pojawia sie obok, gracz rozmawia z nim i gra sie konczy. Eliminuje mylacy step "zejscia schodami" po ostatniej walce.

### Dlaczego E key do interakcji z NPC (nie klikniecie)?
**Decyzja:** `Keyboard.current.eKey.wasPressedThisFrame` zamiast klikniecia sprite'a.
**Powod:** Gracz porusza sie klawiatura. Proximity trigger (radius 2.2) + E key pasuje do reszty systemu. Nowy Input System (NIE legacy `Input.GetKeyDown`).

### Dlaczego Wladca Podziemi pokazuje credits i zamyka gre?
**Decyzja:** Po interakcji: credits z imionami autorow, gra zamyka sie po 8 sek.
**Powod:** To jest ending gry. Gracz pokonol Deliriusa, dostarl na koniec -- credits jako finalowe "dziekujemy za gre".

---

## ENEMY SYSTEM

### Dlaczego EnemySpawner seeduje RNG przed spawnem? (FIX 2026-04-17)
**Decyzja:** `Random.InitState(state.Seed * 7919 + floor * 31)` przed `TrySpawnBoss/TrySpawnRegular`, restore po.
**Powod:** BUG -- `FindSpawnPosition()` uzywal `Random.Range()` ktory dawal rozne pozycje przy kazdym reload sceny. Stable IDs (floor*10000+x*100+y) nie matchowaly miedzy reloadami, wiec zabity boss spawnowal sie ponownie na nowej pozycji. Gracz musial zabic kilku bossow przed pojawieniem sie schodow. Seedowanie RNG sprawia ze pozycje spawn sa identyczne przy kazdym ladowaniu tego samego pietra.

### Dlaczego RemoveEnemyAtPosition zostalo zastapione MarkDefeated? (FIX 2026-04-17)
**Decyzja:** `RemoveEnemyAtPosition` usuniety z `PlayerController.CheckForEnemyEncounter`. Nowa statyczna metoda `MarkDefeated(stableId)` wywolywana TYLKO w `GameManager.OnCombatWon`.
**Powod:** BUG -- stary kod usuwaal wroga z mapy PRZED walka. Jesli gracz przegral lub ucieki, wrog znikal permanentnie. Teraz: wrog znika tylko po wygranej. Po ucieczce/smierci EnemyFactory.Create() tworzy swiezego wroga z pelnym HP na tej samej pozycji.

### Dlaczego po smierci gracza respawn w lobby?
**Decyzja:** `OnCombatLost()` ustawia floor=0, heal full, tox=0, `ShowDeathMessage=true`, load Dungeon.
**Powod:** Gracz wymownie "budzi sie" w lobby z komunikatem "Sprobujmy na druga nozke!" i moze probowac ponownie. Wrogowie na poprzednim pietrze zostaja z pelnym HP.

### Dlaczego OnCombatWon() odpala RaiseEnemyKilled?
**Decyzja:** `GameEvents.RaiseEnemyKilled(CurrentCombatEnemy.Name)` przed wyczyszczeniem referencji.
**Powod:** QuestManager subskrybuje OnEnemyKilled i aktualizuje kill count questow. Musi byc przed `CurrentCombatEnemy = null`.

---

## BALANCE

### Dlaczego Delirius zostal znerfiony ponownie? (FIX 2026-04-20 v3)
**Decyzja:** Base: 220->200 HP, 26->22 atk, 12->10 def. Phase 2: 32->26 atk, 14->12 def. Surge: 30%/1.8x -> 25%/1.5x. Phase 1 surge: 20%/1.5x -> 15%/1.3x.
**Powod:** Gracz z Blogoslwionym Mieczem, Wzmocniona Kolczuga i pelnym HP nadal przegrywal. Max hit phase 2 teraz ~39 (bylo ~57). Phase 1 surge max ~29 (bylo ~39).

### Dlaczego Ognisty Diabel zostal znerfiony? (FIX 2026-04-20 v3)
**Decyzja:** 170->150 HP, 24->20 atk, 11->9 def, inferno 35%/2x -> 25%/1.6x, burn 5->4 dmg/tick.
**Powod:** Floor 4 boss byl za mocny -- gracze nie dochodzili do Deliriusa. Max inferno hit teraz ~32 (bylo ~48).

### Dlaczego Mirek sprzedaje epickie eq za 80-100 zl?
**Decyzja:** 3 nowe epickie przedmioty: Blogoslawiony Miecz (95zl, Holy +12atk), Wzmocniona Kolczuga (100zl, +8def +25HP), Wielki Eliksir (80zl, 80HP + clears tox).
**Powod:** Gracz zbiera ~200 zl z floor 1 i wiecej z kolejnych. Za te pieniadze moze kupic Holy bron (kluczowa na Deliriusa ktory ma Holy weakness i 50% physical resistance w phase 1) i solidna zbroje. Bez tego Delirius byl matematycznie nie do pokonania dla wielu buildow.

### Dlaczego gold wartosci x5-8 od oryginalnych?
**Decyzja:** Chochlik 5->25, Utopiec 8->40 itd.
**Powod:** Oryginalne wartosci dawaly ~35 zlota po calym pietrze, za malo na cokolwiek w sklepie.

### Dlaczego Bimber/Nalewka zwieksza toksycznosc zamiast bezposrednio zmniejszac HP?
**Decyzja:** Alkohol daje AttackBuff (tymczasowy) i zwieksza Toxicity.
**Powod:** Core mechanic gry -- risk/reward. `_pendingAttackBuff` w CombatEngine jest cofany po 1 turze wroga.

---

## GENERACJA LOCHU / KAMERA

### Dlaczego mapa 36x26 (nie 60x60)?
**Decyzja:** `DungeonGenerator.mapWidth = 36, mapHeight = 26`.
**Powod:** 60x60 bylo za duze -- BSP produkool za malo pokoi, dungeon wygladal pusty. 36x26 miesci sie w widoku kamery, BSP tworzy 6-8 pokoi.

### Dlaczego lobby nie ma camera bounds? (FIX 2026-04-17)
**Decyzja:** `CameraFollow.DisableBounds()` dla floor 0, `SetBounds(w,h)` dla floor 1-5.
**Powod:** Lobby (28x22) jest mniejsze niz widok kamery (~23x13 przy orthoSize 6.5). Bounds clamping blokowal kamere blisko centrum mapy zamiast sledzic gracza. Wylaczenie bounds = kamera podaza za Gniewkiem swobodnie jak na innych pietrach.

### Dlaczego DungeonGenerator ustawia CameraFollow.SetBounds?
**Decyzja:** Po `RenderMap` w `GenerateAndRender()` wywolywane jest `camFollow.SetBounds(w, h)`.
**Powod:** `CameraFollow` mial hardcoded maxX=50, maxY=50. SetBounds(mapWidth, mapHeight) teraz ustawia poprawne bounds.

### Dlaczego Resources.UnloadUnusedAssets() w GenerateAndRender?
**Decyzja:** Na starcie `GenerateAndRender()` wywolywane jest `Resources.UnloadUnusedAssets()`.
**Powod:** Runtime Tiles i Sprites kumulowaly sie przez kilka walk i dusily edytor.

### Dlaczego minFromEntrance/Exit zmniejszone?
**Decyzja:** Boss 14/2, regular 6/4 (byly 20/8).
**Powod:** Stare wartosci niemozliwe na mapie 36x26.

---

## NAZWY WROGÓW

### Dlaczego Gargulec Trupiooki -> Bliźniacze Dziwadła? (v3)
**Decyzja:** Zmiana nazwy bossa floor 1.
**Powod:** Lepiej pasuje do sprite'a (dwuglowy stwor, c3 r2 monsters.png). Nazwa bardziej klimatyczna.

### Dlaczego Lord Wampirów -> Baba Jaga? (v3)
**Decyzja:** Zmiana nazwy bossa floor 3.
**Powod:** Klimat slowianski — Baba Jaga pasuje do dark/low fantasy lepiej niz generyczny "Lord Wampirow". Mechanika (lifesteal) zostaje ta sama.

---

## TECHNICZNE DECYZJE UNITY

### Dlaczego ProjectSetupEditor buduje sceny programatycznie?
**Decyzja:** Jeden klik `Tools -> Setup Project` buduje wszystkie 4 sceny z referencjami.
**Powod:** Zapewnia spojnosc. Bez tego po kazdej zmianie skryptu trzeba recznie drag&drop referencji.

### Dlaczego Texture2D z items.png zamiast Sprites/SpriteAtlas?
**Decyzja:** SpriteSheetHelper.ExtractSprite(texture, col, row) wyodrebnia sprite w runtime.
**Powod:** 32rogues to jeden PNG z 32px grid. Runtime extraction jest deterministic i latwa do debugowania.

### Dlaczego TextMeshPro zamiast UI Text?
**Decyzja:** Wszystkie teksty uzywaja TMPro.TextMeshProUGUI (canvas) lub TMPro.TextMeshPro (world).
**Powod:** Standard w nowoczesnym Unity. LiberationSans SDF wspiera polskie litery ale NIE emoji.

---

## DIAGNOSTIC / TEMP CODE

### ~~Diagnostic logs [FREEZE-*]~~ -- USUNIETE
Logi diagnostyczne usuniete z GameManager.StartCombat i CombatUIController.Start/InitializeCombat.

### pragma CS0414 w ShopUIController
Pole `_selectedIsSell` jest przypisywane ale nie odczytywane -- otoczone `#pragma warning disable CS0414`. Zostawione dla przyszlego uzycia.

### ~~Debug skip level (klawisz 0)~~ -- USUNIETY
Blok digit0Key usuniety z PlayerController.Update(). DebugClearFloor() nadal istnieje w EnemySpawner ale nie jest juz wywolywany.

---

## ZNANE PROBLEMY (nie naprawione)

1. ~~**ReturnToMainMenu event leak**~~ -- NAPRAWIONE: ReturnToMainMenu() re-subskrybuje zarowno OnSceneChangeRequested JAK I OnLootDropped po ClearAll().

2. ~~**Status effects (Poison/Fire/Slow od wrogow)**~~ -- ZROBIONE: Utopiec (Poison 20%), Strzyga (Slow 25%), BossNekromanta (Poison 30%), BossOgien (Burn 35%). CombatEngine tickuje efekty, Slow redukuje AP.

3. **Asset leak monitoring** -- Jesli freeze wraca na 5-6 walce, UnloadUnusedAssets moze byc za slabe. Potrzebny cache runtime Tiles/Sprites.
