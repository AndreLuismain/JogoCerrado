using UnityEngine;
using Cerrado.Quests;

namespace Cerrado.Exploration
{
    public class AreaBarrier : MonoBehaviour
    {
        [Header("Configuração de Desbloqueio")]
        [Tooltip("ID correspondente ao campo UnlockedAreaId da missão necessária")]
        [SerializeField] private string requiredAreaId = "area_chapada";
        [SerializeField] private string barrierName = "Acesso à Chapada";

        [Header("Elementos Físicos e Visuais")]
        [SerializeField] private Collider barrierCollider;
        [SerializeField] private GameObject visualObstacle;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip unlockSound;

        [Header("Efeito de Desbloqueio")]
        [SerializeField] private ParticleSystem unlockParticles;

        private bool isUnlocked;

        private void Start()
        {
            if (barrierCollider == null)
            {
                barrierCollider = GetComponent<Collider>();
            }

            CheckUnlockStatus();
        }

        private void OnEnable()
        {
            QuestManager.OnAreaUnlocked += HandleAreaUnlocked;
        }

        private void OnDisable()
        {
            QuestManager.OnAreaUnlocked -= HandleAreaUnlocked;
        }

        private void CheckUnlockStatus()
        {
            if (QuestManager.Instance != null && QuestManager.Instance.IsAreaUnlocked(requiredAreaId))
            {
                Unlock(false);
            }
        }

        private void HandleAreaUnlocked(string areaId)
        {
            if (areaId == requiredAreaId && !isUnlocked)
            {
                Unlock(true);
            }
        }

        private void Unlock(bool playEffects)
        {
            isUnlocked = true;

            if (barrierCollider != null)
            {
                barrierCollider.enabled = false;
            }

            if (visualObstacle != null)
            {
                visualObstacle.SetActive(false);
            }

            if (playEffects)
            {
                if (unlockParticles != null)
                {
                    unlockParticles.Play();
                }

                if (audioSource != null && unlockSound != null)
                {
                    audioSource.PlayOneShot(unlockSound);
                }

                Debug.Log($"[AreaBarrier] '{barrierName}' foi aberta com sucesso!");
            }
        }
    }
}

