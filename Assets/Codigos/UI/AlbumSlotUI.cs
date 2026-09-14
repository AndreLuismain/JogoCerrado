using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cerrado.Album;
using Cerrado.Data;

namespace Cerrado.UI
{
    public class AlbumSlotUI : MonoBehaviour
    {
        [Header("Elementos Visuais")]
        [SerializeField] private Button button;
        [SerializeField] private RawImage photoThumbnail;
        [SerializeField] private GameObject undiscoveredPlaceholder;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text starsText;
        [SerializeField] private TMP_Text rarityBadge;
        [SerializeField] private Image rarityBadgeBackground;

        [Header("Cores de Raridade")]
        [SerializeField] private Color commonColor = new Color(0.6f, 0.6f, 0.6f);
        [SerializeField] private Color uncommonColor = new Color(0.2f, 0.7f, 0.3f);
        [SerializeField] private Color rareColor = new Color(0.2f, 0.5f, 0.9f);
        [SerializeField] private Color veryRareColor = new Color(0.9f, 0.6f, 0.1f);

        private AlbumEntry currentEntry;
        private Action<AlbumEntry> onSelectCallback;

        private void Awake()
        {
            if (button != null)
            {
                button.onClick.AddListener(HandleClick);
            }
        }

        public void Setup(AlbumEntry entry, Action<AlbumEntry> onSelect)
        {
            currentEntry = entry;
            onSelectCallback = onSelect;

            if (entry == null || entry.Species == null) return;

            if (entry.IsDiscovered)
            {
                if (undiscoveredPlaceholder != null) undiscoveredPlaceholder.SetActive(false);
                if (photoThumbnail != null)
                {
                    photoThumbnail.gameObject.SetActive(true);
                    Texture2D photo = AlbumManager.Instance != null ? AlbumManager.Instance.GetPhotoTexture(entry) : null;
                    if (photo != null)
                    {
                        photoThumbnail.texture = photo;
                    }
                }

                if (nameText != null) nameText.text = entry.Species.CommonName;
                if (starsText != null)
                {
                    starsText.text = new string('★', entry.BestStars) + new string('☆', 5 - entry.BestStars);
                    starsText.gameObject.SetActive(true);
                }

                if (rarityBadge != null)
                {
                    rarityBadge.text = entry.Species.Rarity.ToString().ToUpper();
                    rarityBadge.gameObject.SetActive(true);
                }

                if (rarityBadgeBackground != null)
                {
                    rarityBadgeBackground.color = GetRarityColor(entry.Species.Rarity);
                }
            }
            else
            {
                // Espécie não descoberta ainda
                if (undiscoveredPlaceholder != null) undiscoveredPlaceholder.SetActive(true);
                if (photoThumbnail != null) photoThumbnail.gameObject.SetActive(false);
                if (nameText != null) nameText.text = "??? (Não Descoberto)";
                if (starsText != null) starsText.gameObject.SetActive(false);
                if (rarityBadge != null)
                {
                    rarityBadge.text = entry.Species.Category.ToString().ToUpper();
                }
                if (rarityBadgeBackground != null)
                {
                    rarityBadgeBackground.color = commonColor;
                }
            }
        }

        private Color GetRarityColor(SpeciesRarity rarity) => rarity switch
        {
            SpeciesRarity.Comum => commonColor,
            SpeciesRarity.Incomum => uncommonColor,
            SpeciesRarity.Raro => rareColor,
            SpeciesRarity.MuitoRaro => veryRareColor,
            _ => commonColor
        };

        private void HandleClick()
        {
            onSelectCallback?.Invoke(currentEntry);
        }
    }
}

