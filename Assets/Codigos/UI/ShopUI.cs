using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cerrado.Economy;
using Cerrado.Album;
using Cerrado.Player;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Cerrado.UI
{
    public class ShopUI : MonoBehaviour
    {
        [Header("Painel Principal")]
        [SerializeField] private GameObject shopPanel;
        [SerializeField] private PlayerController playerController;

        [Header("Cabeçalho")]
        [SerializeField] private TMP_Text playerCoinsText;
        [SerializeField] private Button closeButton;

        [Header("Grade de Itens")]
        [SerializeField] private Transform itemsContainer;
        [SerializeField] private GameObject itemSlotPrefab;

        [Header("Áudio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip purchaseSuccessSound;

        private readonly List<ShopItemSlotUI> spawnedSlots = new List<ShopItemSlotUI>();

        public bool IsOpen => shopPanel != null && shopPanel.activeSelf;

        private void Awake()
        {
            if (shopPanel != null)
            {
                shopPanel.SetActive(false);
            }

            if (playerController == null)
            {
                playerController = FindAnyObjectByType<PlayerController>();
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(CloseShop);
            }
        }

        private void OnEnable()
        {
            AlbumManager.OnCoinsChanged += UpdateCoinsDisplay;
            EquipmentManager.OnItemPurchased += HandleItemPurchased;
        }

        private void OnDisable()
        {
            AlbumManager.OnCoinsChanged -= UpdateCoinsDisplay;
            EquipmentManager.OnItemPurchased -= HandleItemPurchased;
        }

        private void Update()
        {
            if (!IsOpen) return;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.eKey.wasPressedThisFrame))
            {
                CloseShop();
            }
#else
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.E))
            {
                CloseShop();
            }
#endif
        }

        public void OpenShop()
        {
            if (shopPanel == null) return;

            shopPanel.SetActive(true);

            if (playerController != null)
            {
                playerController.SetControlActive(false);
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            PopulateShopItems();
            UpdateCoinsDisplay(AlbumManager.Instance != null ? AlbumManager.Instance.PlayerCoins : 0);
        }

        public void CloseShop()
        {
            if (shopPanel == null) return;

            shopPanel.SetActive(false);

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

        private void UpdateCoinsDisplay(int coins)
        {
            if (playerCoinsText != null)
            {
                playerCoinsText.text = $"Suas Moedas: {coins}";
            }

            // Atualiza botões com base nas moedas
            foreach (var slot in spawnedSlots)
            {
                if (slot != null) slot.RefreshState();
            }
        }

        private void PopulateShopItems()
        {
            if (itemsContainer == null || itemSlotPrefab == null || EquipmentManager.Instance == null) return;

            foreach (Transform child in itemsContainer)
            {
                Destroy(child.gameObject);
            }
            spawnedSlots.Clear();

            foreach (var item in EquipmentManager.Instance.Catalog)
            {
                if (item == null) continue;

                GameObject slotObj = Instantiate(itemSlotPrefab, itemsContainer);
                var slotUI = slotObj.GetComponent<ShopItemSlotUI>();
                if (slotUI != null)
                {
                    slotUI.Setup(item, HandleBuyClicked);
                    spawnedSlots.Add(slotUI);
                }
            }
        }

        private void HandleBuyClicked(ShopItemData item)
        {
            if (EquipmentManager.Instance == null) return;

            bool success = EquipmentManager.Instance.BuyItem(item);
            if (success)
            {
                if (audioSource != null && purchaseSuccessSound != null)
                {
                    audioSource.PlayOneShot(purchaseSuccessSound);
                }
            }
        }

        private void HandleItemPurchased(ShopItemData item)
        {
            foreach (var slot in spawnedSlots)
            {
                if (slot != null) slot.RefreshState();
            }
        }
    }
}

