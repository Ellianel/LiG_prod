using UnityEngine;
using UnityEngine.UI;
using LochyIGorzala.Core;
using LochyIGorzala.Helpers;
using LochyIGorzala.Managers;

namespace LochyIGorzala.UI
{
    /// <summary>
    /// Character class selection screen. Player picks Wojownik / Łucznik / Mag
    /// before the game starts. Each class has different stats and a unique special attack.
    /// This is pure View — class data is read from CharacterClassFactory (Model).
    /// </summary>
    public class CharSelectController : MonoBehaviour
    {
        [Header("Class Card Buttons")]
        [SerializeField] private Button wojownikButton;
        [SerializeField] private Button lucznikButton;
        [SerializeField] private Button magButton;

        [Header("Class Card Labels (name on each card)")]
        [SerializeField] private TMPro.TextMeshProUGUI wojownikLabel;
        [SerializeField] private TMPro.TextMeshProUGUI lucznikLabel;
        [SerializeField] private TMPro.TextMeshProUGUI magLabel;

        [Header("Preview Panel")]
        [SerializeField] private Image previewSprite;
        [SerializeField] private TMPro.TextMeshProUGUI previewName;
        [SerializeField] private TMPro.TextMeshProUGUI previewStats;
        [SerializeField] private TMPro.TextMeshProUGUI previewDescription;

        [Header("Navigation")]
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button backButton;

        [Header("Assets")]
        [SerializeField] private Texture2D rogueSheet;

        // Colors for card highlight states
        private static readonly Color CardSelected   = new Color(0.8f, 0.6f, 0.1f, 0.9f);
        private static readonly Color CardUnselected = new Color(0.07f, 0.05f, 0.04f, 0.95f);

        private PlayerClass selectedClass = PlayerClass.Wojownik;

        private void Start()
        {
            if (wojownikLabel != null) wojownikLabel.text = "WOJOWNIK";
            if (lucznikLabel  != null) lucznikLabel.text  = "ŁUCZNIK";
            if (magLabel      != null) magLabel.text      = "MAG";

            SetupButtons();
            SelectClass(PlayerClass.Wojownik); // Default selection
        }

        private void SetupButtons()
        {
            if (wojownikButton != null)
                wojownikButton.onClick.AddListener(() => SelectClass(PlayerClass.Wojownik));
            if (lucznikButton != null)
                lucznikButton.onClick.AddListener(() => SelectClass(PlayerClass.Lucznik));
            if (magButton != null)
                magButton.onClick.AddListener(() => SelectClass(PlayerClass.Mag));

            if (confirmButton != null)
                confirmButton.onClick.AddListener(OnConfirm);
            if (backButton != null)
                backButton.onClick.AddListener(OnBack);
        }

        private void SelectClass(PlayerClass cls)
        {
            selectedClass = cls;
            PlayerData data = CharacterClassFactory.CreatePlayer(cls);

            // Update preview sprite
            if (rogueSheet != null && previewSprite != null)
            {
                Sprite sprite = SpriteSheetHelper.ExtractSprite(rogueSheet, data.SpriteCol, data.SpriteRow);
                if (sprite != null)
                {
                    previewSprite.sprite = sprite;
                    previewSprite.preserveAspect = true;
                }
            }

            // Update preview texts
            if (previewName != null)
                previewName.text = $"GNIEWKO\n<size=70%>{data.CharacterClass.ToUpper()}</size>";

            if (previewStats != null)
                previewStats.text =
                    $"Zdrowie   {data.MaxHP}\n" +
                    $"Atak      {data.Attack}\n" +
                    $"Obrona    {data.Defense}\n" +
                    $"Wigor     {data.MaxActionPoints} PA";

            if (previewDescription != null)
                previewDescription.text = data.ClassDescription;

            // Highlight selected card, dim others
            SetCardColor(wojownikButton, cls == PlayerClass.Wojownik);
            SetCardColor(lucznikButton,  cls == PlayerClass.Lucznik);
            SetCardColor(magButton,      cls == PlayerClass.Mag);
        }

        private void SetCardColor(Button card, bool selected)
        {
            if (card == null) return;
            Image img = card.GetComponent<Image>();
            if (img != null)
                img.color = selected ? CardSelected : CardUnselected;
        }

        private void OnConfirm()
        {
            Debug.Log($"CharSelect: Wybrano klase {selectedClass}");
            GameManager.Instance?.StartNewGame(selectedClass);
        }

        private void OnBack()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
    }
}
