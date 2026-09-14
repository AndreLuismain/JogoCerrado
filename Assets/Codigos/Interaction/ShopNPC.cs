using UnityEngine;
using Cerrado.Player;
using Cerrado.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Cerrado.Interaction
{
    public class ShopNPC : MonoBehaviour
    {
        [Header("Configurações de Interação")]
        [SerializeField] private float interactionDistance = 3.5f;
        [SerializeField] private string npcName = "Pesquisador / Comerciante do Cerrado";
        public string NpcName => npcName;

        [Header("Interface")]
        [SerializeField] private GameObject interactionPromptUI; // Ex: Texto "[E] Falar com Comerciante"

        private Transform playerTransform;
        private PlayerController playerController;
        private ShopUI shopUI;
        private bool isPlayerNearby;

        private void Start()
        {
            LocatePlayer();
            shopUI = FindAnyObjectByType<ShopUI>(FindObjectsInactive.Include);

            if (interactionPromptUI != null)
            {
                interactionPromptUI.SetActive(false);
            }
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
                if (playerObj != null)
                {
                    playerTransform = playerObj.transform;
                }
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
                if (interactionPromptUI != null)
                {
                    interactionPromptUI.SetActive(isPlayerNearby);
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
                    OpenShop();
                }
            }
        }

        public void OpenShop()
        {
            if (shopUI == null)
            {
                shopUI = FindAnyObjectByType<ShopUI>(FindObjectsInactive.Include);
            }

            if (shopUI != null)
            {
                shopUI.OpenShop();
            }
            else
            {
                Debug.LogWarning("[ShopNPC] ShopUI não encontrado na cena.");
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, interactionDistance);
        }
    }
}

