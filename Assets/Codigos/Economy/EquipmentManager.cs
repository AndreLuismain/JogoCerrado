using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Cerrado.Album;
using Cerrado.Photography;
using Cerrado.Persistence;

namespace Cerrado.Economy
{
    public class EquipmentManager : MonoBehaviour
    {
        public static EquipmentManager Instance { get; private set; }

        [Header("Catálogo de Itens da Loja")]
        [SerializeField] private List<ShopItemData> allAvailableEquipment = new List<ShopItemData>();

        private readonly HashSet<string> unlockedItemIds = new HashSet<string>();

        public static event Action<ShopItemData> OnItemPurchased;

        public IReadOnlyCollection<ShopItemData> Catalog => allAvailableEquipment;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadUnlockedEquipment();
        }

        private void Start()
        {
            ApplyBonusesToCamera();
        }

        public bool IsUnlocked(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return false;
            return unlockedItemIds.Contains(itemId);
        }

        public bool BuyItem(ShopItemData item)
        {
            if (item == null) return false;
            if (IsUnlocked(item.Id))
            {
                Debug.Log($"[EquipmentManager] Item {item.ItemName} já foi adquirido anteriormente.");
                return false;
            }

            if (AlbumManager.Instance == null) return false;

            if (AlbumManager.Instance.SpendCoins(item.Price))
            {
                unlockedItemIds.Add(item.Id);
                ApplyBonusesToCamera();
                SaveEquipment();

                Debug.Log($"[EquipmentManager] Item adquirido com sucesso: {item.ItemName} por {item.Price} moedas!");
                OnItemPurchased?.Invoke(item);
                return true;
            }

            Debug.Log("[EquipmentManager] Moedas insuficientes para comprar o item.");
            return false;
        }

        public void ApplyBonusesToCamera()
        {
            var cameraSystem = FindAnyObjectByType<PhotoCameraSystem>();
            if (cameraSystem == null) return;

            float totalZoomDecrease = 0f;
            float totalFocusBonus = 0f;

            foreach (var item in allAvailableEquipment)
            {
                if (item != null && unlockedItemIds.Contains(item.Id))
                {
                    totalZoomDecrease += item.ZoomBonusFOV;
                    totalFocusBonus += item.FocusToleranceBonus;
                }
            }

            cameraSystem.UpgradeCamera(totalZoomDecrease, totalFocusBonus);
        }

        public bool HasNightVision()
        {
            return allAvailableEquipment.Any(item => item != null && item.EnablesNightVision && unlockedItemIds.Contains(item.Id));
        }

        public int GetExtraBackpackCapacity()
        {
            int extra = 0;
            foreach (var item in allAvailableEquipment)
            {
                if (item != null && unlockedItemIds.Contains(item.Id))
                {
                    extra += item.ExtraBackpackSlots;
                }
            }
            return extra;
        }

        private void SaveEquipment()
        {
            var saveData = SaveSystem.LoadGame() ?? new GameSaveData();
            saveData.UnlockedEquipmentIds = unlockedItemIds.ToList();
            SaveSystem.SaveGame(saveData);
        }

        private void LoadUnlockedEquipment()
        {
            var saveData = SaveSystem.LoadGame();
            if (saveData != null && saveData.UnlockedEquipmentIds != null)
            {
                foreach (var id in saveData.UnlockedEquipmentIds)
                {
                    unlockedItemIds.Add(id);
                }
            }
        }
    }
}

