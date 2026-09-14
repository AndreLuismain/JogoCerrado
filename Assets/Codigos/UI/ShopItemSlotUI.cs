using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cerrado.Economy;
using Cerrado.Album;

namespace Cerrado.UI
{
    public class ShopItemSlotUI : MonoBehaviour
    {
        [Header("Elementos Visuais")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text itemNameText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text priceText;
        [SerializeField] private Button buyButton;
        [SerializeField] private TMP_Text buyButtonText;

        [Header("Cores")]
        [SerializeField] private Color affordableColor = new Color(0.1f, 0.7f, 0.2f);
        [SerializeField] private Color unaffordableColor = new Color(0.7f, 0.2f, 0.2f);
        [SerializeField] private Color ownedColor = new Color(0.4f, 0.4f, 0.4f);

        private ShopItemData itemData;
        private Action<ShopItemData> onBuyCallback;

        public void Setup(ShopItemData item, Action<ShopItemData> onBuy)
        {
            itemData = item;
            onBuyCallback = onBuy;

            if (item == null) return;

            if (itemNameText != null) itemNameText.text = item.ItemName;
            if (descriptionText != null) descriptionText.text = item.ItemDescription;
            if (priceText != null) priceText.text = $"{item.Price} Moedas";

            if (iconImage != null && item.Icon != null)
            {
                iconImage.sprite = item.Icon;
                iconImage.gameObject.SetActive(true);
            }

            if (buyButton != null)
            {
                buyButton.onClick.RemoveAllListeners();
                buyButton.onClick.AddListener(() => onBuyCallback?.Invoke(itemData));
            }

            RefreshState();
        }

        public void RefreshState()
        {
            if (itemData == null || buyButton == null) return;

            bool isOwned = EquipmentManager.Instance != null && EquipmentManager.Instance.IsUnlocked(itemData.Id);
            int currentCoins = AlbumManager.Instance != null ? AlbumManager.Instance.PlayerCoins : 0;
            bool canAfford = currentCoins >= itemData.Price;

            if (isOwned)
            {
                buyButton.interactable = false;
                if (buyButtonText != null)
                {
                    buyButtonText.text = "EQUIPADO ✓";
                    buyButtonText.color = ownedColor;
                }
            }
            else
            {
                buyButton.interactable = canAfford;
                if (buyButtonText != null)
                {
                    buyButtonText.text = canAfford ? "COMPRAR" : "MOEDAS INSUF.";
                    buyButtonText.color = canAfford ? affordableColor : unaffordableColor;
                }
            }
        }
    }
}

