using UnityEngine;

namespace Cerrado.Data
{
    [CreateAssetMenu(fileName = "NovaEspecie", menuName = "Cerrado/Espécie", order = 1)]
    public class SpeciesData : ScriptableObject
    {
        [Header("Identificação")]
        [Tooltip("Identificador único sem espaços, ex: 'lobo_guara'")]
        [SerializeField] private string id = "especie_id";
        [SerializeField] private string commonName = "Nome Popular";
        [SerializeField] private string scientificName = "Nome Científico";
        [SerializeField] private SpeciesCategory category = SpeciesCategory.Fauna;
        [SerializeField] private SpeciesRarity rarity = SpeciesRarity.Comum;
        [SerializeField] private ActivityTime activityTime = ActivityTime.Diurno;

        [Header("Informações Educativas (Compêndio)")]
        [TextArea(3, 8)]
        [SerializeField] private string educationalDescription = "Descrição detalhada sobre a espécie e seu papel ecológico no Cerrado.";
        [SerializeField] private string habitatDescription = "Campos abertos e cerradões.";
        [SerializeField] private string conservationStatus = "Pouco Preocupante";

        [Header("Pontuação e Economia")]
        [SerializeField] private int baseScore = 150;
        [SerializeField] private int baseCoins = 15;

        [Header("Parâmetros de Fotografia")]
        [Tooltip("Distância mínima ideal para não cortar o enquadramento (metros)")]
        [SerializeField] private float minIdealDistance = 3.0f;
        [Tooltip("Distância máxima ideal para detalhes nítidos (metros)")]
        [SerializeField] private float maxIdealDistance = 15.0f;
        [Tooltip("Tamanho aproximado do alvo para cálculo de enquadramento (metros)")]
        [SerializeField] private float targetRadius = 1.2f;

        [Header("Representação Visual")]
        [SerializeField] private Sprite icon;

        // Getters públicos
        public string Id => id;
        public string CommonName => commonName;
        public string ScientificName => scientificName;
        public SpeciesCategory Category => category;
        public SpeciesRarity Rarity => rarity;
        public ActivityTime ActivityTime => activityTime;
        public string EducationalDescription => educationalDescription;
        public string HabitatDescription => habitatDescription;
        public string ConservationStatus => conservationStatus;
        public int BaseScore => baseScore;
        public int BaseCoins => baseCoins;
        public float MinIdealDistance => minIdealDistance;
        public float MaxIdealDistance => maxIdealDistance;
        public float TargetRadius => targetRadius;
        public Sprite Icon => icon;
    }
}

