using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Cerrado.Quests;

namespace Cerrado.UI
{
    public class QuestHUDUI : MonoBehaviour
    {
        [Header("Painel de Missões Ativas na HUD")]
        [SerializeField] private GameObject questsContainer;
        [SerializeField] private TMP_Text questListText;

        [Header("Banner de Notificação de Conclusão")]
        [SerializeField] private GameObject completionBanner;
        [SerializeField] private TMP_Text completionTitleText;
        [SerializeField] private TMP_Text completionRewardText;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip questCompletedSound;
        [SerializeField] private float bannerDuration = 4.0f;

        private Coroutine bannerCoroutine;

        private void Awake()
        {
            if (completionBanner != null)
            {
                completionBanner.SetActive(false);
            }
        }

        private void Start()
        {
            UpdateQuestList();
        }

        private void OnEnable()
        {
            QuestManager.OnQuestActivated += HandleQuestChanged;
            QuestManager.OnQuestCompleted += HandleQuestCompleted;
        }

        private void OnDisable()
        {
            QuestManager.OnQuestActivated -= HandleQuestChanged;
            QuestManager.OnQuestCompleted -= HandleQuestCompleted;
        }

        private void HandleQuestChanged(QuestData quest)
        {
            UpdateQuestList();
        }

        private void HandleQuestCompleted(QuestData quest)
        {
            UpdateQuestList();

            if (completionBanner != null && quest != null)
            {
                if (bannerCoroutine != null) StopCoroutine(bannerCoroutine);
                bannerCoroutine = StartCoroutine(ShowCompletionBannerRoutine(quest));
            }
        }

        private void UpdateQuestList()
        {
            if (questListText == null || QuestManager.Instance == null) return;

            var activeQuests = QuestManager.Instance.ActiveQuests;
            if (activeQuests == null || activeQuests.Count == 0)
            {
                questListText.text = "<b>Missões do Cerrado:</b>\n<i>Nenhuma missão ativa no momento. Fale com o Guia!</i>";
                return;
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("<b>MISSÕES ATIVAS:</b>");

            foreach (var q in activeQuests)
            {
                if (q == null) continue;
                sb.AppendLine($"• <b>{q.Title}</b>");
                sb.AppendLine($"   <size=85%>{q.Description}</size>");
            }

            questListText.text = sb.ToString();
        }

        private IEnumerator ShowCompletionBannerRoutine(QuestData quest)
        {
            completionBanner.SetActive(true);

            if (completionTitleText != null)
            {
                completionTitleText.text = $"MISSÃO CUMPRIDA: {quest.Title}!";
            }

            if (completionRewardText != null)
            {
                completionRewardText.text = $"+{quest.RewardCoins} Moedas adicionadas à sua carteira!";
            }

            if (audioSource != null && questCompletedSound != null)
            {
                audioSource.PlayOneShot(questCompletedSound);
            }

            yield return new WaitForSeconds(bannerDuration);

            completionBanner.SetActive(false);
        }
    }
}

