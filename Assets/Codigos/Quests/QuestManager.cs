using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Cerrado.Photography;
using Cerrado.Album;
using Cerrado.Persistence;

namespace Cerrado.Quests
{
    public class QuestManager : MonoBehaviour
    {
        public static QuestManager Instance { get; private set; }

        [Header("Catálogo Geral de Missões")]
        [SerializeField] private List<QuestData> allQuests = new List<QuestData>();

        [Header("Configuração Inicial")]
        [Tooltip("Quantas missões ativas simultâneas o jogador pode ter")]
        [SerializeField] private int maxSimultaneousQuests = 3;

        private readonly List<QuestData> activeQuests = new List<QuestData>();
        private readonly HashSet<string> completedQuestIds = new HashSet<string>();
        private readonly HashSet<string> unlockedAreaIds = new HashSet<string>();

        // Eventos
        public static event Action<QuestData> OnQuestCompleted;
        public static event Action<QuestData> OnQuestActivated;
        public static event Action<string> OnAreaUnlocked;

        public IReadOnlyList<QuestData> ActiveQuests => activeQuests;
        public IReadOnlyCollection<string> CompletedQuestIds => completedQuestIds;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadQuestsProgress();
        }

        private void Start()
        {
            RefreshActiveQuests();
        }

        private void OnEnable()
        {
            PhotoCameraSystem.OnPhotoTaken += HandlePhotoTaken;
        }

        private void OnDisable()
        {
            PhotoCameraSystem.OnPhotoTaken -= HandlePhotoTaken;
        }

        private void HandlePhotoTaken(PhotoResult result)
        {
            if (result == null || activeQuests.Count == 0) return;

            // Cria uma cópia da lista ativa para permitir remoção segura durante a iteração
            var questsToCheck = new List<QuestData>(activeQuests);

            foreach (var quest in questsToCheck)
            {
                if (quest != null && quest.IsFulfilled(result))
                {
                    CompleteQuest(quest);
                }
            }
        }

        public void CompleteQuest(QuestData quest)
        {
            if (quest == null || completedQuestIds.Contains(quest.Id)) return;

            completedQuestIds.Add(quest.Id);
            activeQuests.Remove(quest);

            // Entrega recompensa em moedas
            if (AlbumManager.Instance != null && quest.RewardCoins > 0)
            {
                AlbumManager.Instance.AddCoins(quest.RewardCoins);
            }

            // Desbloqueia nova área se houver
            if (!string.IsNullOrEmpty(quest.UnlockedAreaId))
            {
                unlockedAreaIds.Add(quest.UnlockedAreaId);
                OnAreaUnlocked?.Invoke(quest.UnlockedAreaId);
                Debug.Log($"[QuestManager] Nova área desbloqueada: {quest.UnlockedAreaId}!");
            }

            Debug.Log($"[QuestManager] Missão Concluída: {quest.Title} (+{quest.RewardCoins} moedas)!");
            OnQuestCompleted?.Invoke(quest);

            SaveQuestsProgress();
            RefreshActiveQuests();
        }

        private void RefreshActiveQuests()
        {
            // Completa vagas de missões ativas
            foreach (var quest in allQuests)
            {
                if (activeQuests.Count >= maxSimultaneousQuests) break;

                if (quest != null && !completedQuestIds.Contains(quest.Id) && !activeQuests.Contains(quest))
                {
                    activeQuests.Add(quest);
                    OnQuestActivated?.Invoke(quest);
                }
            }
        }

        public bool IsQuestCompleted(string questId)
        {
            return !string.IsNullOrEmpty(questId) && completedQuestIds.Contains(questId);
        }

        public bool IsAreaUnlocked(string areaId)
        {
            return !string.IsNullOrEmpty(areaId) && unlockedAreaIds.Contains(areaId);
        }

        private void SaveQuestsProgress()
        {
            var saveData = SaveSystem.LoadGame() ?? new GameSaveData();
            saveData.CompletedQuestIds = completedQuestIds.ToList();
            SaveSystem.SaveGame(saveData);
        }

        private void LoadQuestsProgress()
        {
            var saveData = SaveSystem.LoadGame();
            if (saveData != null && saveData.CompletedQuestIds != null)
            {
                foreach (var id in saveData.CompletedQuestIds)
                {
                    completedQuestIds.Add(id);
                    // Restaura áreas desbloqueadas com base nas missões salvas
                    var quest = allQuests.FirstOrDefault(q => q.Id == id);
                    if (quest != null && !string.IsNullOrEmpty(quest.UnlockedAreaId))
                    {
                        unlockedAreaIds.Add(quest.UnlockedAreaId);
                    }
                }
            }
        }
    }
}

