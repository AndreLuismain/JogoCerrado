using UnityEngine;
using UnityEngine.UI;
using Cerrado.Player;
using Cerrado.Settings;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Cerrado.UI
{
    /// <summary>
    /// Painel flutuante de tutorial rápido exibido automaticamente na primeira entrada na SampleScene
    /// (controlado por PlayerPrefs "TutorialVisto") ou sob demanda pelo botão "Ver Tutorial" nas Opções.
    /// Destrava o cursor e pausa a movimentação enquanto aberto; fecha no botão 'X', 'Começar Expedição' ou ESC.
    /// </summary>
    public class TutorialOverlayUI : MonoBehaviour
    {
        [Header("Referências do Tutorial")]
        [SerializeField] private GameObject tutorialPanel;
        [SerializeField] private PlayerController playerController;
        [SerializeField] private Button closeXButton;
        [SerializeField] private Button startExpeditionButton;

        public bool IsOpen => tutorialPanel != null && tutorialPanel.activeSelf;

        private void Awake()
        {
            if (tutorialPanel == null)
            {
                Transform t = transform.Find("Janela_Tutorial");
                if (t != null) tutorialPanel = t.gameObject;
            }

            if (playerController == null)
            {
                playerController = FindAnyObjectByType<PlayerController>();
            }

            if (closeXButton != null)
            {
                closeXButton.onClick.RemoveAllListeners();
                closeXButton.onClick.AddListener(CloseTutorial);
            }

            if (startExpeditionButton != null)
            {
                startExpeditionButton.onClick.RemoveAllListeners();
                startExpeditionButton.onClick.AddListener(CloseTutorial);
            }

            if (tutorialPanel != null)
            {
                tutorialPanel.SetActive(false);
            }
        }

        private void Start()
        {
            if (!GameSettingsManager.HasSeenTutorial())
            {
                ShowTutorial(false);
            }
        }

        private void Update()
        {
            if (!IsOpen) return;

            bool escPressed = false;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                escPressed = Keyboard.current.escapeKey.wasPressedThisFrame;
            }
#else
            escPressed = Input.GetKeyDown(KeyCode.Escape);
#endif
            if (escPressed)
            {
                CloseTutorial();
            }
        }

        public void ShowTutorial(bool force)
        {
            if (tutorialPanel == null) return;
            if (!force && GameSettingsManager.HasSeenTutorial()) return;

            tutorialPanel.SetActive(true);
            Time.timeScale = 1f;

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

        public void CloseTutorial()
        {
            if (tutorialPanel != null)
            {
                tutorialPanel.SetActive(false);
            }

            GameSettingsManager.SetTutorialSeen(true);
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
    }
}
