using UnityEngine;
using Cerrado.Data;
using Cerrado.Photography;

namespace Cerrado.Quests
{
    [CreateAssetMenu(fileName = "NovaMissao", menuName = "Cerrado/Missão", order = 3)]
    public class QuestData : ScriptableObject
    {
        [Header("Identificação")]
        [SerializeField] private string id = "missao_01";
        [SerializeField] private string title = "Título da Missão";
        [TextArea(2, 5)]
        [SerializeField] private string description = "Objetivo detalhado do que o explorador precisa fazer.";

        [Header("Condições para Cumprimento")]
        [Tooltip("Espécie alvo requerida (deixe vazio se for qualquer espécie)")]
        [SerializeField] private SpeciesData targetSpecies;

        [Tooltip("Avaliação mínima em estrelas necessária na foto (1 a 5)")]
        [Range(1, 5)]
        [SerializeField] private int minStarsRequired = 1;

        [Tooltip("Exige que a fotografia seja tirada durante o período noturno")]
        [SerializeField] private bool requiresNight = false;

        [Header("Recompensas")]
        [SerializeField] private int rewardCoins = 60;
        [Tooltip("ID da área que esta missão desbloqueia no mapa (se houver)")]
        [SerializeField] private string unlockedAreaId = "";

        public string Id => id;
        public string Title => title;
        public string Description => description;
        public SpeciesData TargetSpecies => targetSpecies;
        public int MinStarsRequired => minStarsRequired;
        public bool RequiresNight => requiresNight;
        public int RewardCoins => rewardCoins;
        public string UnlockedAreaId => unlockedAreaId;

        /// <summary>
        /// Valida se uma foto capturada satisfaz todas as condições desta missão
        /// </summary>
        public bool IsFulfilled(PhotoResult result)
        {
            if (result == null) return false;

            // 1. Checa espécie
            if (targetSpecies != null)
            {
                if (result.Species == null || result.Species.Id != targetSpecies.Id)
                {
                    return false;
                }
            }

            // 2. Checa qualidade de estrelas
            if (result.Stars < minStarsRequired)
            {
                return false;
            }

            // 3. Checa condição noturna
            if (requiresNight && !result.IsNightPhoto)
            {
                return false;
            }

            return true;
        }
    }
}

