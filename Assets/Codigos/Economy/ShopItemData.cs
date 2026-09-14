using UnityEngine;

namespace Cerrado.Economy
{
    [CreateAssetMenu(fileName = "NovoItemLoja", menuName = "Cerrado/Item da Loja", order = 2)]
    public class ShopItemData : ScriptableObject
    {
        [Header("Identificação")]
        [SerializeField] private string id = "item_id";
        [SerializeField] private string itemName = "Nome do Item";
        [TextArea(2, 5)]
        [SerializeField] private string itemDescription = "Descrição detalhada dos benefícios deste item.";
        [SerializeField] private int price = 100;
        [SerializeField] private EquipmentType type = EquipmentType.TelephotoLens;
        [SerializeField] private Sprite icon;

        [Header("Modificadores de Atributo")]
        [Tooltip("Reduz o campo de visão mínimo da câmera (permite maior zoom de aproximação)")]
        [SerializeField] private float zoomBonusFOV = 10f; // Ex: reduz minZoom de 20 para 10

        [Tooltip("Habilita sensor noturno para fotos claras mesmo no escuro")]
        [SerializeField] private bool enablesNightVision = false;

        [Tooltip("Aumenta a tolerância de foco no visor da câmera (facilita travar a mira em animais velozes)")]
        [SerializeField] private float focusToleranceBonus = 0.12f;

        [Tooltip("Espaços adicionais na mochila para coletar sementes e amostras")]
        [SerializeField] private int extraBackpackSlots = 4;

        public string Id => id;
        public string ItemName => itemName;
        public string ItemDescription => itemDescription;
        public int Price => price;
        public EquipmentType Type => type;
        public Sprite Icon => icon;
        public float ZoomBonusFOV => zoomBonusFOV;
        public bool EnablesNightVision => enablesNightVision;
        public float FocusToleranceBonus => focusToleranceBonus;
        public int ExtraBackpackSlots => extraBackpackSlots;
    }
}

