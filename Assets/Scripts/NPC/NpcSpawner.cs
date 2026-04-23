using UnityEngine;
using LochyIGorzala.Core;
using LochyIGorzala.Managers;

namespace LochyIGorzala.NPC
{
    /// <summary>
    /// Spawns NPCs based on current floor.
    /// Floor 0 (Lobby): spawns Mirek Handlarz at a fixed position inside the
    /// right-side alcove of the hand-crafted lobby (never changes layout).
    /// Floor 5: spawns Władca Podziemi (wizard) near defeated Delirius when
    /// DeliriusDefeated flag is set.
    ///
    /// Lobby layout reference (28×22 grid):
    ///   Main room  : x=2..25, y=2..18
    ///   Right niche: x=25..27, y=8..13  (carved, always floor)
    ///   Opening    : x=25, y=9..12
    ///   MIREK POS  : x=26, y=10 — centre of the right niche, guaranteed floor
    /// </summary>
    public class NpcSpawner : MonoBehaviour
    {
        [Header("Merchant sprite sheet (rogues.png recommended)")]
        [SerializeField] private Texture2D merchantSheet;
        [SerializeField] private int merchantSpriteCol = 4;
        [SerializeField] private int merchantSpriteRow = 3;

        [Header("Władca Podziemi sprite (rogues.png)")]
        [SerializeField] private int wladcaSpriteCol = 3;
        [SerializeField] private int wladcaSpriteRow = 5;

        [Header("Tajemniczy Jegomość sprite (rogues.png)")]
        [SerializeField] private int jegomoscSpriteCol = 6;
        [SerializeField] private int jegomoscSpriteRow = 2;

        [Header("Shop UI — assigned by ProjectSetupEditor")]
        [SerializeField] private UI.ShopUIController shopUI;

        // Fixed world position of Mirek inside the right niche of the lobby.
        // The lobby is hand-crafted and never changes, so this is always valid.
        private const float MirekX = 26f;
        private const float MirekY = 10f;

        // Tajemniczy Jegomość position — left niche of lobby
        private const float JegomoscX = 1f;
        private const float JegomoscY = 10f;

        private GameObject _currentMerchant;
        private GameObject _currentWladca;
        private GameObject _currentJegomosc;

        private void Awake()
        {
            GameEvents.OnDungeonGenerated += OnDungeonGenerated;
        }

        private void OnDestroy()
        {
            GameEvents.OnDungeonGenerated -= OnDungeonGenerated;
        }

        private void OnDungeonGenerated()
        {
            // Clean up any previous instances
            if (_currentMerchant != null)
            {
                Destroy(_currentMerchant);
                _currentMerchant = null;
            }
            if (_currentWladca != null)
            {
                Destroy(_currentWladca);
                _currentWladca = null;
            }
            if (_currentJegomosc != null)
            {
                Destroy(_currentJegomosc);
                _currentJegomosc = null;
            }

            GameState state = GameManager.Instance?.CurrentGameState;
            if (state == null) return;

            // Lobby (floor 0): Mirek Handlarz + Tajemniczy Jegomość
            if (state.CurrentFloor == 0)
            {
                SpawnMirek();
                SpawnTajemniczyJegomosc(state);
            }

            // Floor 5 + Delirius defeated: Władca Podziemi spawns near where Delirius was
            // (a few blocks from the player's saved position, which is the combat site)
            if (state.CurrentFloor == 5 && state.DeliriusDefeated)
            {
                float wx = state.Player.PositionX + 3f;
                float wy = state.Player.PositionY;
                SpawnWladcaPodziemi(wx, wy);
            }
        }

        private void SpawnMirek()
        {
            _currentMerchant = new GameObject("Mirek_Handlarz");
            _currentMerchant.transform.position = new Vector3(MirekX, MirekY, 0f);

            // ── Sprite ────────────────────────────────────────────
            var sr = _currentMerchant.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 10;
            sr.transform.localScale = new Vector3(1.4f, 1.4f, 1f);

            if (merchantSheet != null)
            {
                Sprite sprite = Helpers.SpriteSheetHelper.ExtractSprite(
                    merchantSheet, merchantSpriteCol, merchantSpriteRow);
                if (sprite != null) sr.sprite = sprite;
            }

            if (sr.sprite == null)
            {
                // Fallback: solid gold square so Mirek is always visible
                sr.sprite = MakePlaceholderSprite();
                sr.color  = new Color(0.95f, 0.75f, 0.1f);
            }

            // ── Name label (TextMeshPro world-space) ──────────────
            var labelGO = new GameObject("MirekLabel");
            labelGO.transform.SetParent(_currentMerchant.transform, false);
            labelGO.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            var tmp = labelGO.AddComponent<TMPro.TextMeshPro>();
            tmp.text        = "<color=#FFD700>Mirek Handlarz</color>\n<size=60%>[E] Sklep</size>";
            tmp.fontSize    = 3.2f;
            tmp.alignment   = TMPro.TextAlignmentOptions.Center;
            tmp.sortingOrder = 15;

            // ── MerchantNPC component ─────────────────────────────
            _currentMerchant.AddComponent<MerchantNPC>();

            // Inject shop reference at runtime via public method
            if (shopUI != null)
                _currentMerchant.SendMessage("SetShopUI", shopUI, SendMessageOptions.DontRequireReceiver);

            Debug.Log($"[NpcSpawner] Mirek Handlarz placed at ({MirekX}, {MirekY}) in lobby right niche.");
        }

        private void SpawnTajemniczyJegomosc(GameState state)
        {
            _currentJegomosc = new GameObject("Tajemniczy_Jegomosc");
            _currentJegomosc.transform.position = new Vector3(JegomoscX, JegomoscY, 0f);

            // ── Sprite ────────────────────────────────────────────
            var sr = _currentJegomosc.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 10;
            sr.transform.localScale = new Vector3(1.4f, 1.4f, 1f);

            if (merchantSheet != null)
            {
                Sprite sprite = Helpers.SpriteSheetHelper.ExtractSprite(
                    merchantSheet, jegomoscSpriteCol, jegomoscSpriteRow);
                if (sprite != null) sr.sprite = sprite;
            }

            if (sr.sprite == null)
            {
                sr.sprite = MakePlaceholderSprite();
                sr.color = new Color(0.3f, 0.7f, 0.3f); // Green fallback
            }

            // ── Name label ────────────────────────────────────────
            var labelGO = new GameObject("JegomoscLabel");
            labelGO.transform.SetParent(_currentJegomosc.transform, false);
            labelGO.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            var tmp = labelGO.AddComponent<TMPro.TextMeshPro>();
            tmp.text = "<color=#66FF66>Tajemniczy Jegomość</color>\n<size=60%>[E] Porozmawiaj</size>";
            tmp.fontSize = 3.2f;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.sortingOrder = 15;

            // ── NPC component ─────────────────────────────────────
            _currentJegomosc.AddComponent<TajemniczyJegomoscNPC>();

            // ── Intro hangover message (only on first visit) ──────
            if (!state.QuestData.IntroCompleted)
            {
                GameEvents.RaiseNotification(
                    "Kurewsko boli Cię głowa, ilości podejrzanego alkoholu zabiłyby " +
                    "normalnego człowieka, ale nie Ciebie.\n\n" +
                    "A kto to tam stoi? Skądś kojarzysz faceta...");
            }

            Debug.Log($"[NpcSpawner] Tajemniczy Jegomość placed at ({JegomoscX}, {JegomoscY}) in lobby left niche.");
        }

        private void SpawnWladcaPodziemi(float posX, float posY)
        {
            _currentWladca = new GameObject("Wladca_Podziemi");
            _currentWladca.transform.position = new Vector3(posX, posY, 0f);

            // ── Sprite ────────────────────────────────────────────
            var sr = _currentWladca.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 10;
            sr.transform.localScale = new Vector3(1.6f, 1.6f, 1f);

            if (merchantSheet != null)
            {
                Sprite sprite = Helpers.SpriteSheetHelper.ExtractSprite(
                    merchantSheet, wladcaSpriteCol, wladcaSpriteRow);
                if (sprite != null) sr.sprite = sprite;
            }

            if (sr.sprite == null)
            {
                sr.sprite = MakePlaceholderSprite();
                sr.color  = new Color(0.5f, 0.2f, 0.9f); // Purple fallback
            }

            // ── Name label (TextMeshPro world-space) ──────────────
            var labelGO = new GameObject("WladcaLabel");
            labelGO.transform.SetParent(_currentWladca.transform, false);
            labelGO.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            var tmp = labelGO.AddComponent<TMPro.TextMeshPro>();
            tmp.text        = "<color=#AA55FF>Władca Podziemi</color>\n<size=60%>[E] Porozmawiaj</size>";
            tmp.fontSize    = 3.2f;
            tmp.alignment   = TMPro.TextAlignmentOptions.Center;
            tmp.sortingOrder = 15;

            // ── WladcaPodziemiNPC component ────────────────────────
            _currentWladca.AddComponent<WladcaPodziemiNPC>();

            Debug.Log($"[NpcSpawner] Władca Podziemi placed at ({posX}, {posY}) on floor 5.");
        }

        private static Sprite MakePlaceholderSprite()
        {
            var tex = new Texture2D(32, 32);
            var pixels = new Color[32 * 32];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);
        }
    }
}
