using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Cerrado.Player;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Cerrado.UI
{
    public class PauseMenuUI : MonoBehaviour
    {
        [Header("Referências do Menu de Pausa")]
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject optionsPanel;
        [SerializeField] private SettingsPanelUI settingsPanelUI;
        [SerializeField] private TutorialOverlayUI tutorialOverlayUI;
        [SerializeField] private PlayerController playerController;
        [SerializeField] private AlbumUI albumUI;
        [SerializeField] private ShopUI shopUI;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button optionsButton;
        [SerializeField] private Button optionsBackButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button quitButton;

        public bool IsPaused => (pausePanel != null && pausePanel.activeSelf) || (optionsPanel != null && optionsPanel.activeSelf);

        private void Awake()
        {
            if (pausePanel == null)
            {
                Transform childWin = transform.Find("Janela_Pausa");
                if (childWin != null) pausePanel = childWin.gameObject;
            }
            if (optionsPanel == null)
            {
                Transform optWin = transform.Find("Janela_Opcoes_Pausa");
                if (optWin != null) optionsPanel = optWin.gameObject;
            }
            if (settingsPanelUI == null && optionsPanel != null)
            {
                settingsPanelUI = optionsPanel.GetComponent<SettingsPanelUI>();
            }

            if (pausePanel != null) pausePanel.SetActive(false);
            if (optionsPanel != null) optionsPanel.SetActive(false);

            if (playerController == null) playerController = FindAnyObjectByType<PlayerController>();
            if (albumUI == null) albumUI = FindAnyObjectByType<AlbumUI>(FindObjectsInactive.Include);
            if (shopUI == null) shopUI = FindAnyObjectByType<ShopUI>(FindObjectsInactive.Include);
            if (tutorialOverlayUI == null) tutorialOverlayUI = FindAnyObjectByType<TutorialOverlayUI>(FindObjectsInactive.Include);

            if (resumeButton != null) resumeButton.onClick.AddListener(ResumeGame);
            if (optionsButton != null) optionsButton.onClick.AddListener(OpenOptions);
            if (optionsBackButton != null) optionsBackButton.onClick.AddListener(CloseOptions);
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(ReturnToMainMenu);
            if (quitButton != null) quitButton.onClick.AddListener(QuitGame);

            if (settingsPanelUI != null)
            {
                settingsPanelUI.OnRequestOpenTutorial += HandleOpenTutorialFromOptions;
            }
        }

        private void OnDestroy()
        {
            if (settingsPanelUI != null)
            {
                settingsPanelUI.OnRequestOpenTutorial -= HandleOpenTutorialFromOptions;
            }
        }

        private void Update()
        {
            bool escPressed = false;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                escPressed = Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.pKey.wasPressedThisFrame;
            }
#else
            escPressed = Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P);
#endif
            if (!escPressed) return;

            // Se Álbum, Loja ou Tutorial estiverem abertos, eles tratam o ESC
            if ((albumUI != null && albumUI.IsOpen) ||
                (shopUI != null && shopUI.IsOpen) ||
                (tutorialOverlayUI != null && tutorialOverlayUI.IsOpen))
            {
                return;
            }

            if (optionsPanel != null && optionsPanel.activeSelf)
            {
                CloseOptions();
                return;
            }

            if (IsPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }

        public void PauseGame()
        {
            if (pausePanel == null) return;
            pausePanel.SetActive(true);
            if (optionsPanel != null) optionsPanel.SetActive(false);
            Time.timeScale = 0f;

            if (playerController != null)
            {
                playerController.SetControlActive(false);
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        public void OpenOptions()
        {
            if (optionsPanel == null) return;
            if (pausePanel != null) pausePanel.SetActive(false);
            optionsPanel.SetActive(true);
            if (settingsPanelUI != null) settingsPanelUI.RefreshUIFromSettings();
        }

        public void CloseOptions()
        {
            if (optionsPanel != null) optionsPanel.SetActive(false);
            if (pausePanel != null) pausePanel.SetActive(true);
        }

        private void HandleOpenTutorialFromOptions()
        {
            if (optionsPanel != null) optionsPanel.SetActive(false);
            if (pausePanel != null) pausePanel.SetActive(false);
            Time.timeScale = 1f;

            if (tutorialOverlayUI != null)
            {
                tutorialOverlayUI.ShowTutorial(true);
            }
        }

        public void ResumeGame()
        {
            if (pausePanel != null) pausePanel.SetActive(false);
            if (optionsPanel != null) optionsPanel.SetActive(false);
            Time.timeScale = 1f;

            if (playerController != null)
            {
                playerController.SetControlActive(true);
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        public void ReturnToMainMenu()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SceneManager.LoadScene("Assets/Jogo/Menu/menu_cena.unity");
        }

        public void QuitGame()
        {
            Time.timeScale = 1f;
            Application.Quit();
        }
    }
}
