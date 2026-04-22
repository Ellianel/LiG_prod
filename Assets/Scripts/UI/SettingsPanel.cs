using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace LochyIGorzala.UI
{
    /// <summary>
    /// Simple settings panel — Master Volume, Fullscreen toggle, Resolution picker.
    /// All values persisted via PlayerPrefs.  Works identically in MainMenu and Dungeon.
    /// </summary>
    public class SettingsPanel : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private TextMeshProUGUI volumeLabel;
        [SerializeField] private Toggle fullscreenToggle;
        [SerializeField] private Button resolutionLeftBtn;
        [SerializeField] private Button resolutionRightBtn;
        [SerializeField] private TextMeshProUGUI resolutionLabel;
        [SerializeField] private Button backButton;

        [Header("Panel to restore when closing (optional)")]
        [SerializeField] private GameObject restoreOnClose;

        // Supported resolutions (common 16:9)
        private static readonly Vector2Int[] Resolutions = new[]
        {
            new Vector2Int(1280, 720),
            new Vector2Int(1366, 768),
            new Vector2Int(1600, 900),
            new Vector2Int(1920, 1080),
            new Vector2Int(2560, 1440),
        };

        private int _currentResIndex;

        // PlayerPrefs keys
        private const string KeyVolume     = "settings_volume";
        private const string KeyFullscreen = "settings_fullscreen";
        private const string KeyResW       = "settings_res_w";
        private const string KeyResH       = "settings_res_h";

        private void Start()
        {
            // Load saved values (or sensible defaults)
            float savedVol = PlayerPrefs.GetFloat(KeyVolume, 1f);
            bool savedFS   = PlayerPrefs.GetInt(KeyFullscreen, Screen.fullScreen ? 1 : 0) == 1;
            int savedW     = PlayerPrefs.GetInt(KeyResW, Screen.width);
            int savedH     = PlayerPrefs.GetInt(KeyResH, Screen.height);

            // Find closest matching resolution index
            _currentResIndex = FindClosestResolution(savedW, savedH);

            // Apply loaded values
            AudioListener.volume = savedVol;
            if (volumeSlider != null)
            {
                volumeSlider.minValue = 0f;
                volumeSlider.maxValue = 1f;
                volumeSlider.value = savedVol;
                volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            }
            UpdateVolumeLabel(savedVol);

            if (fullscreenToggle != null)
            {
                fullscreenToggle.isOn = savedFS;
                fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
            }

            UpdateResolutionLabel();

            resolutionLeftBtn?.onClick.AddListener(() => ChangeResolution(-1));
            resolutionRightBtn?.onClick.AddListener(() => ChangeResolution(+1));
            backButton?.onClick.AddListener(OnBack);
        }

        // ── Callbacks ─────────────────────────────────────────────

        private void OnVolumeChanged(float val)
        {
            AudioListener.volume = val;
            PlayerPrefs.SetFloat(KeyVolume, val);
            UpdateVolumeLabel(val);
        }

        private void OnFullscreenChanged(bool isOn)
        {
            Screen.fullScreen = isOn;
            PlayerPrefs.SetInt(KeyFullscreen, isOn ? 1 : 0);
        }

        private void ChangeResolution(int dir)
        {
            _currentResIndex = Mathf.Clamp(_currentResIndex + dir, 0, Resolutions.Length - 1);
            var res = Resolutions[_currentResIndex];
            bool fs = fullscreenToggle != null ? fullscreenToggle.isOn : Screen.fullScreen;
            Screen.SetResolution(res.x, res.y, fs);
            PlayerPrefs.SetInt(KeyResW, res.x);
            PlayerPrefs.SetInt(KeyResH, res.y);
            UpdateResolutionLabel();
        }

        private void OnBack()
        {
            PlayerPrefs.Save();
            gameObject.SetActive(false);
            if (restoreOnClose != null) restoreOnClose.SetActive(true);
        }

        // ── Helpers ───────────────────────────────────────────────

        private void UpdateVolumeLabel(float val)
        {
            if (volumeLabel != null)
                volumeLabel.text = $"Głośność: {Mathf.RoundToInt(val * 100)}%";
        }

        private void UpdateResolutionLabel()
        {
            if (resolutionLabel != null)
            {
                var r = Resolutions[_currentResIndex];
                resolutionLabel.text = $"{r.x} x {r.y}";
            }
        }

        private int FindClosestResolution(int w, int h)
        {
            int best = 0;
            int bestDist = int.MaxValue;
            for (int i = 0; i < Resolutions.Length; i++)
            {
                int dist = Mathf.Abs(Resolutions[i].x - w) + Mathf.Abs(Resolutions[i].y - h);
                if (dist < bestDist) { bestDist = dist; best = i; }
            }
            return best;
        }

        // ── Public API ────────────────────────────────────────────

        /// <summary>Show panel and optionally set which panel to restore on close.</summary>
        public void Show(GameObject restore = null)
        {
            restoreOnClose = restore;
            gameObject.SetActive(true);
        }
    }
}
