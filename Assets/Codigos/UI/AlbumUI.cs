using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cerrado.Album;
using Cerrado.Data;
using Cerrado.Player;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Cerrado.UI
{
    public class AlbumUI : MonoBehaviour
    {
        [Header("Painel Principal")]
        [SerializeField] private GameObject albumPanel;
        [SerializeField] private PlayerController playerController;

        [Header("Estatísticas e Progresso")]
        [SerializeField] private TMP_Text progressPercentageText;
        [SerializeField] private TMP_Text progressCountText;
        [SerializeField] private Slider progressBar;
        [SerializeField] private TMP_Text coinsDisplayText;

        [Header("Navegação de Abas")]
        [SerializeField] private Button tabAllButton;
        [SerializeField] private Button tabFaunaButton;
        [SerializeField] private Button tabFloraButton;

        [Header("Grade de Espécies")]
        [SerializeField] private Transform gridContainer;
        [SerializeField] private GameObject slotPrefab;

        [Header("Painel Lateral de Detalhes")]
        [SerializeField] private GameObject detailsPanel;
        [SerializeField] private RawImage detailPhoto;
        [SerializeField] private TMP_Text detailCommonName;
        [SerializeField] private TMP_Text detailScientificName;
        [SerializeField] private TMP_Text detailRarity;
        [SerializeField] private TMP_Text detailEducational;
        [SerializeField] private TMP_Text detailHabitat;
        [SerializeField] private TMP_Text detailConservation;
        [SerializeField] private TMP_Text detailBestScore;
        [SerializeField] private TMP_Text detailStars;
        [SerializeField] private TMP_Text detailCaptureDate;

        private enum FilterCategory { All, Fauna, Flora }
        private FilterCategory currentFilter = FilterCategory.All;
        private readonly List<AlbumSlotUI> spawnedSlots = new List<AlbumSlotUI>();

        public bool IsOpen => albumPanel != null && albumPanel.activeSelf;

        private void Awake()
        {
            if (albumPanel != null)
            {
                albumPanel.SetActive(false);
            }

            if (playerController == null)
            {
                playerController = FindAnyObjectByType<PlayerController>();
            }

            if (tabAllButton != null) tabAllButton.onClick.AddListener(() => SetFilter(FilterCategory.All));
            if (tabFaunaButton != null) tabFaunaButton.onClick.AddListener(() => SetFilter(FilterCategory.Fauna));
            if (tabFloraButton != null) tabFloraButton.onClick.AddListener(() => SetFilter(FilterCategory.Flora));
        }

        private void OnEnable()
        {
            AlbumManager.OnAlbumDataChanged += RefreshUI;
            AlbumManager.OnCoinsChanged += UpdateCoinsText;
        }

        private void OnDisable()
        {
            AlbumManager.OnAlbumDataChanged -= RefreshUI;
            AlbumManager.OnCoinsChanged -= UpdateCoinsText;
        }

        private void Update()
        {
            bool toggleKey = false;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                toggleKey = Keyboard.current.tabKey.wasPressedThisFrame || Keyboard.current.jKey.wasPressedThisFrame;
                if (IsOpen && Keyboard.current.escapeKey.wasPressedThisFrame)
                {
                    CloseAlbum();
                    return;
                }
            }
#else
            toggleKey = Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.J);
            if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
            {
                CloseAlbum();
                return;
            }
#endif

            if (toggleKey)
            {
                ToggleAlbum();
            }
        }

        public void ToggleAlbum()
        {
            if (IsOpen)
            {
                CloseAlbum();
            }
            else
            {
                OpenAlbum();
            }
        }

        public void OpenAlbum()
        {
            if (albumPanel == null) return;

            albumPanel.SetActive(true);

            if (playerController != null)
            {
                playerController.SetControlActive(false);
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            RefreshUI();
        }

        public void CloseAlbum()
        {
            if (albumPanel == null) return;

            albumPanel.SetActive(false);

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

        private void SetFilter(FilterCategory filter)
        {
            currentFilter = filter;
            RefreshUI();
        }

        public void RefreshUI()
        {
            if (!IsOpen || AlbumManager.Instance == null) return;

            UpdateProgressHeader();
            UpdateGrid();
        }

        private void UpdateCoinsText(int coins)
        {
            if (coinsDisplayText != null)
            {
                coinsDisplayText.text = $"Moedas: {coins}";
            }
        }

        private void UpdateProgressHeader()
        {
            float overallPercent = AlbumManager.Instance.GetOverallCompletionPercentage();
            int discovered = AlbumManager.Instance.GetDiscoveredCount();
            int total = AlbumManager.Instance.GetTotalSpeciesCount();

            if (progressPercentageText != null)
            {
                progressPercentageText.text = $"{overallPercent:F1}% Concluído";
            }

            if (progressCountText != null)
            {
                progressCountText.text = $"{discovered} / {total} Espécies Descobertas";
            }

            if (progressBar != null)
            {
                progressBar.value = overallPercent / 100f;
            }

            UpdateCoinsText(AlbumManager.Instance.PlayerCoins);
        }

        private void UpdateGrid()
        {
            if (gridContainer == null || slotPrefab == null) return;

            // Limpa slots anteriores
            foreach (Transform child in gridContainer)
            {
                Destroy(child.gameObject);
            }
            spawnedSlots.Clear();

            var entries = AlbumManager.Instance.AllEntries.AsEnumerable();

            if (currentFilter == FilterCategory.Fauna)
            {
                entries = entries.Where(e => e.Species != null && e.Species.Category == SpeciesCategory.Fauna);
            }
            else if (currentFilter == FilterCategory.Flora)
            {
                entries = entries.Where(e => e.Species != null && e.Species.Category == SpeciesCategory.Flora);
            }

            AlbumEntry firstEntry = null;

            foreach (var entry in entries)
            {
                if (firstEntry == null) firstEntry = entry;

                GameObject slotObj = Instantiate(slotPrefab, gridContainer);
                var slotUI = slotObj.GetComponent<AlbumSlotUI>();
                if (slotUI != null)
                {
                    slotUI.Setup(entry, DisplayDetails);
                    spawnedSlots.Add(slotUI);
                }
            }

            // Exibe os detalhes do primeiro item selecionado
            if (firstEntry != null)
            {
                DisplayDetails(firstEntry);
            }
            else if (detailsPanel != null)
            {
                detailsPanel.SetActive(false);
            }
        }

        public void DisplayDetails(AlbumEntry entry)
        {
            if (detailsPanel == null || entry == null || entry.Species == null) return;

            detailsPanel.SetActive(true);

            if (entry.IsDiscovered)
            {
                if (detailCommonName != null) detailCommonName.text = entry.Species.CommonName;
                if (detailScientificName != null) detailScientificName.text = $"<i>{entry.Species.ScientificName}</i>";
                if (detailRarity != null) detailRarity.text = $"Raridade: {entry.Species.Rarity} | {entry.Species.Category}";
                if (detailEducational != null) detailEducational.text = entry.Species.EducationalDescription;
                if (detailHabitat != null) detailHabitat.text = $"<b>Habitat:</b> {entry.Species.HabitatDescription}";
                if (detailConservation != null) detailConservation.text = $"<b>Status:</b> {entry.Species.ConservationStatus}";
                if (detailBestScore != null) detailBestScore.text = $"Melhor Foto: {entry.BestScore} pts (x{entry.TimesPhotographed} capturas)";
                if (detailStars != null) detailStars.text = new string('★', entry.BestStars) + new string('☆', 5 - entry.BestStars);
                if (detailCaptureDate != null) detailCaptureDate.text = $"Data: {entry.CaptureDate}";

                if (detailPhoto != null)
                {
                    detailPhoto.gameObject.SetActive(true);
                    Texture2D photo = AlbumManager.Instance.GetPhotoTexture(entry);
                    if (photo != null)
                    {
                        detailPhoto.texture = photo;
                    }
                }
            }
            else
            {
                if (detailCommonName != null) detailCommonName.text = "??? (Espécie Desconhecida)";
                if (detailScientificName != null) detailScientificName.text = "<i>Explore o cerrado para fotografar e catalogar</i>";
                if (detailRarity != null) detailRarity.text = $"Categoria: {entry.Species.Category}";
                if (detailEducational != null) detailEducational.text = "Dados biológicos bloqueados até a primeira fotografia.";
                if (detailHabitat != null) detailHabitat.text = "<b>Habitat:</b> Desconhecido";
                if (detailConservation != null) detailConservation.text = "<b>Status:</b> ???";
                if (detailBestScore != null) detailBestScore.text = "Ainda não fotografado";
                if (detailStars != null) detailStars.text = "☆☆☆☆☆";
                if (detailCaptureDate != null) detailCaptureDate.text = "";

                if (detailPhoto != null)
                {
                    detailPhoto.gameObject.SetActive(false);
                }
            }
        }
    }
}

