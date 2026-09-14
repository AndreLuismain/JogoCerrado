using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Cerrado.Player;
using Cerrado.Album;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Cerrado.Interaction
{
    public class GuideNPC : MonoBehaviour
    {
        [Header("Configuração de Interação")]
        [SerializeField] private float interactionDistance = 3.5f;
        [SerializeField] private string npcName = "Prof. Silva (Biólogo do Cerrado)";

        [Header("Diálogos Educativos do Tutorial")]
        [TextArea(2, 5)]
        [SerializeField] private List<string> dialogueLines = new List<string>()
        {
            "Bem-vindo ao Cerrado brasileiro! O segundo maior bioma do nosso país e lar de milhares de espécies únicas.",
            "Como fotógrafo e pesquisador de campo, sua missão é catalogar nossa fauna e flora no Álbum da expedição.",
            "Dica de ouro: Segure o BOTÃO DIREITO do mouse para olhar pelo visor da câmera e use a RODINHA do mouse para zoom.",
            "Aproxime-se AGACHADO (Ctrl) para não assustar espécies velozes como a Ema, e clique no BOTÃO ESQUERDO para fotografar.",
            "Consulte suas missões ativas na tela e venha falar comigo sempre que precisar de orientação!"
        };

        [Header("Interface de Diálogo")]
        [SerializeField] private GameObject promptUI;
        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private TMP_Text dialogueSpeakerText;
        [SerializeField] private TMP_Text dialogueBodyText;

        private Transform playerTransform;
        private PlayerController playerController;
        private int currentLineIndex;
        private bool isPlayerNearby;
        private bool isTalking;

        private void Start()
        {
            LocatePlayer();

            if (promptUI != null) promptUI.SetActive(false);
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
        }

        private void LocatePlayer()
        {
            playerController = FindAnyObjectByType<PlayerController>();
            if (playerController != null)
            {
                playerTransform = playerController.transform;
            }
            else
            {
                var playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null) playerTransform = playerObj.transform;
            }
        }

        private void Update()
        {
            if (playerTransform == null)
            {
                LocatePlayer();
                return;
            }

            float dist = Vector3.Distance(transform.position, playerTransform.position);
            bool nearby = dist <= interactionDistance;

            if (nearby != isPlayerNearby)
            {
                isPlayerNearby = nearby;
                if (promptUI != null && !isTalking)
                {
                    promptUI.SetActive(isPlayerNearby);
                }
            }

            if (isPlayerNearby)
            {
                bool interactPressed = false;

#if ENABLE_INPUT_SYSTEM
                if (Keyboard.current != null)
                {
                    interactPressed = Keyboard.current.eKey.wasPressedThisFrame;
                }
#else
                interactPressed = Input.GetKeyDown(KeyCode.E);
#endif

                if (interactPressed)
                {
                    if (!isTalking)
                    {
                        StartDialogue();
                    }
                    else
                    {
                        AdvanceDialogue();
                    }
                }
            }
            else if (isTalking)
            {
                EndDialogue();
            }
        }

        private void StartDialogue()
        {
            isTalking = true;
            currentLineIndex = 0;

            if (promptUI != null) promptUI.SetActive(false);
            if (dialoguePanel != null) dialoguePanel.SetActive(true);

            if (playerController != null)
            {
                playerController.SetControlActive(false);
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            ShowCurrentLine();
        }

        public void AdvanceDialogue()
        {
            currentLineIndex++;
            if (currentLineIndex >= dialogueLines.Count)
            {
                EndDialogue();
            }
            else
            {
                ShowCurrentLine();
            }
        }

        private void ShowCurrentLine()
        {
            if (dialogueSpeakerText != null)
            {
                dialogueSpeakerText.text = npcName;
            }

            if (dialogueBodyText != null && currentLineIndex < dialogueLines.Count)
            {
                dialogueBodyText.text = dialogueLines[currentLineIndex];
            }
        }

        public void EndDialogue()
        {
            isTalking = false;

            if (dialoguePanel != null) dialoguePanel.SetActive(false);
            if (promptUI != null && isPlayerNearby) promptUI.SetActive(true);

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

        public void OpenShopFromGuide()
        {
            EndDialogue();
            var shopNPC = GetComponent<ShopNPC>();
            if (shopNPC != null)
            {
                shopNPC.OpenShop();
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, interactionDistance);
        }
    }
}

