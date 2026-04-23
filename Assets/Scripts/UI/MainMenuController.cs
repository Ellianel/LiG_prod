using UnityEngine;
using UnityEngine.UI;
using LochyIGorzala.Managers;

namespace LochyIGorzala.UI
{
    /// <summary>
    /// Controls the Main Menu UI. Handles button clicks and panel visibility.
    /// This is the View layer — it delegates all logic to GameManager.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject authorsPanel;
        [SerializeField] private GameObject optionsPanel;

        [Header("Buttons")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button loadButton;
        [SerializeField] private Button optionsButton;
        [SerializeField] private Button authorsButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private Button authorsBackButton;
        [SerializeField] private Button optionsBackButton;

        [Header("Save System")]
        [SerializeField] private SaveSlotPanel saveSlotPanel;

        private void Start()
        {
            SetupButtonListeners();
            ShowMainPanel();
            UpdateLoadButtonState();
        }

        private void SetupButtonListeners()
        {
            if (startButton != null)
                startButton.onClick.AddListener(OnStartClicked);

            if (loadButton != null)
                loadButton.onClick.AddListener(OnLoadClicked);

            if (optionsButton != null)
                optionsButton.onClick.AddListener(OnOptionsClicked);

            if (authorsButton != null)
                authorsButton.onClick.AddListener(OnAuthorsClicked);

            if (quitButton != null)
                quitButton.onClick.AddListener(OnQuitClicked);

            if (authorsBackButton != null)
                authorsBackButton.onClick.AddListener(OnAuthorsBackClicked);

            if (optionsBackButton != null)
                optionsBackButton.onClick.AddListener(OnOptionsBackClicked);
        }

        private void UpdateLoadButtonState()
        {
            if (loadButton != null && GameManager.Instance != null)
            {
                loadButton.interactable = GameManager.Instance.HasAnySave();
            }
        }

        // --- Button Handlers ---

        private void OnStartClicked()
        {
            Debug.Log("Menu: Nowa Przygoda -> Wybor Klasy");
            if (GameManager.Instance != null)
            {
                GameManager.Instance.GoToCharacterSelect();
            }
        }

        private void OnLoadClicked()
        {
            Debug.Log("Menu: Wczytaj Gre -> slot picker");
            if (saveSlotPanel != null)
            {
                // Hide main panel while slot picker is open; restore it when cancelled
                if (mainPanel != null) mainPanel.SetActive(false);
                saveSlotPanel.ShowForLoad(restoreOnClose: mainPanel);
            }
            else if (GameManager.Instance != null)
            {
                // Fallback: load from slot 1 if panel not wired up
                GameManager.Instance.LoadFromSlot(1);
            }
        }

        private void OnOptionsClicked()
        {
            Debug.Log("Menu: Opcje (DLC)");
            if (optionsPanel != null)
            {
                if (mainPanel != null) mainPanel.SetActive(false);
                optionsPanel.SetActive(true);
            }
        }

        private void OnAuthorsClicked()
        {
            Debug.Log("Menu: Autorzy");
            ShowAuthorsPanel();
        }

        private void OnQuitClicked()
        {
            Debug.Log("Menu: Wyjscie");
            if (GameManager.Instance != null)
            {
                GameManager.Instance.QuitGame();
            }
        }

        private void OnAuthorsBackClicked()
        {
            ShowMainPanel();
        }

        private void OnOptionsBackClicked()
        {
            if (optionsPanel != null) optionsPanel.SetActive(false);
            ShowMainPanel();
        }

        // --- Panel Management ---

        private void ShowMainPanel()
        {
            if (mainPanel != null) mainPanel.SetActive(true);
            if (authorsPanel != null) authorsPanel.SetActive(false);
            if (optionsPanel != null) optionsPanel.SetActive(false);
        }

        private void ShowAuthorsPanel()
        {
            if (mainPanel != null) mainPanel.SetActive(false);
            if (authorsPanel != null) authorsPanel.SetActive(true);
        }
    }
}
