using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Cerrado.Data;
using Cerrado.Photography;
using Cerrado.Persistence;

namespace Cerrado.Album
{
    public class AlbumManager : MonoBehaviour
    {
        public static AlbumManager Instance { get; private set; }

        [Header("Banco de Espécies do Jogo")]
        [Tooltip("Arraste aqui todos os ScriptableObjects de espécies cadastrados")]
        [SerializeField] private List<SpeciesData> allRegisteredSpecies = new List<SpeciesData>();

        [Header("Economia")]
        [SerializeField] private int playerCoins = 0;

        // Dicionário de registros do álbum indexado pelo ID da espécie
        private readonly Dictionary<string, AlbumEntry> albumEntries = new Dictionary<string, AlbumEntry>();
        private int totalPhotosTaken = 0;

        // Eventos
        public static event Action<AlbumEntry> OnSpeciesDiscovered;
        public static event Action<AlbumEntry, PhotoResult> OnBestPhotoUpdated;
        public static event Action<int> OnCoinsChanged;
        public static event Action OnAlbumDataChanged;

        public int PlayerCoins => playerCoins;
        public int TotalPhotosTaken => totalPhotosTaken;
        public IReadOnlyCollection<AlbumEntry> AllEntries => albumEntries.Values;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeAlbum();
            LoadFromSave();
        }

        private void OnEnable()
        {
            PhotoCameraSystem.OnPhotoTaken += HandlePhotoTaken;
        }

        private void OnDisable()
        {
            PhotoCameraSystem.OnPhotoTaken -= HandlePhotoTaken;
        }

        private void InitializeAlbum()
        {
            albumEntries.Clear();

            // Se a lista estiver vazia no Inspector, tenta carregar da pasta Resources
            if (allRegisteredSpecies == null || allRegisteredSpecies.Count == 0)
            {
                allRegisteredSpecies = Resources.LoadAll<SpeciesData>("").ToList();
            }

            foreach (var species in allRegisteredSpecies)
            {
                if (species != null && !albumEntries.ContainsKey(species.Id))
                {
                    albumEntries.Add(species.Id, new AlbumEntry(species));
                }
            }
        }

        private void HandlePhotoTaken(PhotoResult result)
        {
            totalPhotosTaken++;

            // Se for foto de paisagem (sem espécie identificada)
            if (result.Species == null)
            {
                AddCoins(result.CoinsEarned);
                SaveProgress();
                return;
            }

            // Garante que a espécie existe no catálogo
            if (!albumEntries.TryGetValue(result.Species.Id, out var entry))
            {
                entry = new AlbumEntry(result.Species);
                albumEntries.Add(result.Species.Id, entry);
            }

            entry.TimesPhotographed++;
            AddCoins(result.CoinsEarned);

            bool isFirstDiscovery = !entry.IsDiscovered;
            if (isFirstDiscovery)
            {
                entry.IsDiscovered = true;
                result.IsNewDiscovery = true;
                Debug.Log($"[AlbumManager] Nova espécie descoberta! {result.Species.CommonName}");
                OnSpeciesDiscovered?.Invoke(entry);
            }

            // Checa se é um novo recorde de pontuação
            if (isFirstDiscovery || result.TotalScore > entry.BestScore)
            {
                entry.BestScore = result.TotalScore;
                entry.BestStars = result.Stars;
                entry.CaptureDate = result.Timestamp.ToString("dd/MM/yyyy HH:mm");

                // Salva a nova melhor foto no disco
                if (result.PhotoTexture != null)
                {
                    string fileName = SaveSystem.SavePhotoToDisk(result.Species.Id, result.PhotoTexture);
                    entry.PhotoFileName = fileName;
                    entry.CachedTexture = result.PhotoTexture;
                }

                Debug.Log($"[AlbumManager] Novo recorde fotográfico para {result.Species.CommonName}: {entry.BestScore} pts ({entry.BestStars}★)");
                OnBestPhotoUpdated?.Invoke(entry, result);
            }

            OnAlbumDataChanged?.Invoke();
            SaveProgress();
        }

        public void AddCoins(int amount)
        {
            if (amount <= 0) return;
            playerCoins += amount;
            OnCoinsChanged?.Invoke(playerCoins);
        }

        public bool SpendCoins(int amount)
        {
            if (amount <= 0) return true;
            if (playerCoins >= amount)
            {
                playerCoins -= amount;
                OnCoinsChanged?.Invoke(playerCoins);
                SaveProgress();
                return true;
            }
            return false;
        }

        // Estatísticas de Conclusão (GDD 5.7)
        public int GetTotalSpeciesCount() => albumEntries.Count;
        public int GetDiscoveredCount() => albumEntries.Values.Count(e => e.IsDiscovered);

        public float GetOverallCompletionPercentage()
        {
            if (albumEntries.Count == 0) return 0f;
            return (float)GetDiscoveredCount() / albumEntries.Count * 100f;
        }

        public float GetFaunaCompletionPercentage()
        {
            var fauna = albumEntries.Values.Where(e => e.Species.Category == SpeciesCategory.Fauna).ToList();
            if (fauna.Count == 0) return 0f;
            return (float)fauna.Count(e => e.IsDiscovered) / fauna.Count * 100f;
        }

        public float GetFloraCompletionPercentage()
        {
            var flora = albumEntries.Values.Where(e => e.Species.Category == SpeciesCategory.Flora).ToList();
            if (flora.Count == 0) return 0f;
            return (float)flora.Count(e => e.IsDiscovered) / flora.Count * 100f;
        }

        public AlbumEntry GetEntry(string speciesId)
        {
            albumEntries.TryGetValue(speciesId, out var entry);
            return entry;
        }

        public Texture2D GetPhotoTexture(AlbumEntry entry)
        {
            if (entry == null) return null;
            if (entry.CachedTexture != null) return entry.CachedTexture;

            if (!string.IsNullOrEmpty(entry.PhotoFileName))
            {
                entry.CachedTexture = SaveSystem.LoadPhotoFromDisk(entry.PhotoFileName);
            }
            return entry.CachedTexture;
        }

        // Persistência
        public void SaveProgress()
        {
            var saveData = new GameSaveData
            {
                PlayerCoins = playerCoins,
                TotalPhotosTaken = totalPhotosTaken
            };

            foreach (var entry in albumEntries.Values)
            {
                if (entry.IsDiscovered)
                {
                    saveData.DiscoveredSpecies.Add(new SavedSpeciesEntry
                    {
                        SpeciesId = entry.Species.Id,
                        BestScore = entry.BestScore,
                        BestStars = entry.BestStars,
                        TimesPhotographed = entry.TimesPhotographed,
                        PhotoFileName = entry.PhotoFileName,
                        CaptureDate = entry.CaptureDate
                    });
                }
            }

            SaveSystem.SaveGame(saveData);
        }

        public void LoadFromSave()
        {
            GameSaveData saveData = SaveSystem.LoadGame();
            if (saveData == null) return;

            playerCoins = saveData.PlayerCoins;
            totalPhotosTaken = saveData.TotalPhotosTaken;

            foreach (var savedEntry in saveData.DiscoveredSpecies)
            {
                if (albumEntries.TryGetValue(savedEntry.SpeciesId, out var entry))
                {
                    entry.IsDiscovered = true;
                    entry.BestScore = savedEntry.BestScore;
                    entry.BestStars = savedEntry.BestStars;
                    entry.TimesPhotographed = savedEntry.TimesPhotographed;
                    entry.PhotoFileName = savedEntry.PhotoFileName;
                    entry.CaptureDate = savedEntry.CaptureDate;
                }
            }

            OnCoinsChanged?.Invoke(playerCoins);
            OnAlbumDataChanged?.Invoke();
        }
    }
}

