using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
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

            ApplyWoodEarthyTheme();
        }

        private void Start()
        {
            UpdateQuestList();
        }

        private void ApplyWoodEarthyTheme()
        {
            if (questsContainer != null)
            {
                var bgImg = questsContainer.GetComponent<Image>();
                if (bgImg != null)
                {
                    // Fundo amadeirado/terroso semitransparente harmonizado com o Menu e Opções
                    bgImg.color = new Color(0.22f, 0.14f, 0.08f, 0.84f);
                }

                var outline = questsContainer.GetComponent<Outline>();
                if (outline == null)
                {
                    outline = questsContainer.AddComponent<Outline>();
                }
                outline.effectColor = new Color(0.84f, 0.68f, 0.42f, 0.75f);
                outline.effectDistance = new Vector2(2f, -2f);
            }

            if (questListText != null)
            {
                questListText.color = new Color(0.98f, 0.94f, 0.84f, 1f);
            }
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
                questListText.text = "<color=#F5DFA0><b>EXPEDIÇÃO CERRADO</b></color>\n<color=#E6D7B8><i>Todas as missões atuais foram concluídas! Fale com o Guia.</i></color>";
                return;
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("<size=108%><color=#F5DFA0><b>MISSÕES ATIVAS</b></color></size>");
            sb.AppendLine("<size=75%><color=#C8A458AA>------------------------------</color></size>");

            foreach (var q in activeQuests)
            {
                if (q == null) continue;
                sb.AppendLine($"<color=#F2C94C>></color> <color=#FFF6E0><b>{q.Title}</b></color>");
                sb.AppendLine($"   <size=86%><color=#E5D6BA>{q.Description}</color></size>");
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
