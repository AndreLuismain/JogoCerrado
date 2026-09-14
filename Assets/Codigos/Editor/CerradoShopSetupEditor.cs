using UnityEngine;
using UnityEditor;
using System.IO;
using Cerrado.Economy;

namespace Cerrado.EditorTools
{
    public static class CerradoShopSetupEditor
    {
        [MenuItem("Cerrado/2. Gerar Itens Iniciais da Loja (GDD)")]
        public static void GenerateInitialShopItems()
        {
            string folderPath = "Assets/Dados/Loja";
            if (!AssetDatabase.IsValidFolder("Assets/Dados"))
            {
                AssetDatabase.CreateFolder("Assets", "Dados");
            }
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder("Assets/Dados", "Loja");
            }

            // 1. Lente Teleobjetiva
            CreateOrUpdateItem(
                folderPath, "LenteTeleobjetiva",
                id: "lens_telephoto_300",
                itemName: "Lente Teleobjetiva 300mm",
                description: "Amplia o alcance de zoom da câmera, permitindo enquadrar espécies tímidas a distâncias seguras.",
                price: 100,
                type: EquipmentType.TelephotoLens,
                zoomBonus: 5f,
                nightVision: false,
                focusBonus: 0f,
                backpackSlots: 0
            );

            // 2. Lente Super Teleobjetiva
            CreateOrUpdateItem(
                folderPath, "LenteSuperTele",
                id: "lens_super_600",
                itemName: "Lente Super-Tele 600mm",
                description: "Zoom de altíssimo alcance para fotografar predadores como Onça-parda e Lobo-guará sem ser notado.",
                price: 250,
                type: EquipmentType.TelephotoLens,
                zoomBonus: 9f,
                nightVision: false,
                focusBonus: 0f,
                backpackSlots: 0
            );

            // 3. Estabilizador de Foco
            CreateOrUpdateItem(
                folderPath, "EstabilizadorFoco",
                id: "stabilizer_mk1",
                itemName: "Estabilizador Óptico de Imagem",
                description: "Compensa movimentos rápidos e vento. Aumenta a tolerância do sensor de foco em animais velozes como a Ema.",
                price: 120,
                type: EquipmentType.FocusStabilizer,
                zoomBonus: 0f,
                nightVision: false,
                focusBonus: 0.15f,
                backpackSlots: 0
            );

            // 4. Módulo Noturno
            CreateOrUpdateItem(
                folderPath, "SensorNoturno",
                id: "sensor_night_vision",
                itemName: "Sensor Noturno Infravermelho",
                description: "Equipamento essencial para documentar a fauna de hábitos noturnos sem precisar de flash assustador.",
                price: 180,
                type: EquipmentType.NightVisionModule,
                zoomBonus: 0f,
                nightVision: true,
                focusBonus: 0f,
                backpackSlots: 0
            );

            // 5. Mochila Expandida
            CreateOrUpdateItem(
                folderPath, "MochilaExpandida",
                id: "backpack_expanded",
                itemName: "Mochila de Campo Reforçada",
                description: "Aumenta o espaço para coletar amostras botânicas, folhas e sementes do Cerrado durante as expedições.",
                price: 80,
                type: EquipmentType.BackpackUpgrade,
                zoomBonus: 0f,
                nightVision: false,
                focusBonus: 0f,
                backpackSlots: 4
            );

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Cerrado Engine", "Itens da loja criados com sucesso na pasta 'Assets/Dados/Loja'!", "OK");
        }

        private static void CreateOrUpdateItem(
            string folder, string fileName,
            string id, string itemName, string description,
            int price, EquipmentType type,
            float zoomBonus, bool nightVision, float focusBonus, int backpackSlots)
        {
            string assetPath = $"{folder}/{fileName}.asset";
            ShopItemData asset = AssetDatabase.LoadAssetAtPath<ShopItemData>(assetPath);

            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<ShopItemData>();
                AssetDatabase.CreateAsset(asset, assetPath);
            }

            SerializedObject so = new SerializedObject(asset);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("itemName").stringValue = itemName;
            so.FindProperty("itemDescription").stringValue = description;
            so.FindProperty("price").intValue = price;
            so.FindProperty("type").enumValueIndex = (int)type;
            so.FindProperty("zoomBonusFOV").floatValue = zoomBonus;
            so.FindProperty("enablesNightVision").boolValue = nightVision;
            so.FindProperty("focusToleranceBonus").floatValue = focusBonus;
            so.FindProperty("extraBackpackSlots").intValue = backpackSlots;
            so.ApplyModifiedProperties();
        }
    }
}

