using UnityEngine;
using UnityEditor;
using System.IO;
using Cerrado.Data;

namespace Cerrado.EditorTools
{
    public static class CerradoSetupEditor
    {
        [MenuItem("Cerrado/1. Gerar Dados Iniciais das Espécies (GDD)")]
        public static void GenerateInitialSpecies()
        {
            string folderPath = "Assets/Dados/Especies";
            if (!AssetDatabase.IsValidFolder("Assets/Dados"))
            {
                AssetDatabase.CreateFolder("Assets", "Dados");
            }
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder("Assets/Dados", "Especies");
            }

            // 1. Tamanduá-bandeira
            CreateOrUpdateSpecies(
                folderPath, "TamanduaBandeira",
                id: "tamandua_bandeira",
                commonName: "Tamanduá-bandeira",
                scientificName: "Myrmecophaga tridactyla",
                category: SpeciesCategory.Fauna,
                rarity: SpeciesRarity.Comum,
                activity: ActivityTime.Diurno,
                educational: "Mamífero insetívoro característico do Cerrado. Alimenta-se principalmente de formigas e cupins, usando suas garras fortes e língua comprida. É essencial para o controle biológico de insetos.",
                habitat: "Campos abertos e cerradão.",
                status: "Vulnerável (VU)",
                baseScore: 180, baseCoins: 18,
                minDist: 3.5f, maxDist: 14f, radius: 1.4f
            );

            // 2. Lobo-guará
            CreateOrUpdateSpecies(
                folderPath, "LoboGuara",
                id: "lobo_guara",
                commonName: "Lobo-guará",
                scientificName: "Chrysocyon brachyurus",
                category: SpeciesCategory.Fauna,
                rarity: SpeciesRarity.Raro,
                activity: ActivityTime.Noturno,
                educational: "Maior canídeo da América do Sul e símbolo do Cerrado. Possui pernas longas para avistar presas sobre o capim alto. Consome o fruto da lobeira, sendo o maior dispersor de sementes da planta.",
                habitat: "Vegetação campestre e savânica.",
                status: "Vulnerável (VU)",
                baseScore: 400, baseCoins: 45,
                minDist: 5f, maxDist: 20f, radius: 1.2f
            );

            // 3. Ema
            CreateOrUpdateSpecies(
                folderPath, "Ema",
                id: "ema",
                commonName: "Ema",
                scientificName: "Rhea americana",
                category: SpeciesCategory.Fauna,
                rarity: SpeciesRarity.Incomum,
                activity: ActivityTime.Diurno,
                educational: "Maior ave não voadora do continente americano. Corre em altas velocidades pelos campos e tem papel importante na dispersão de frutos nativos.",
                habitat: "Campos limpos e chapadões.",
                status: "Quase Ameaçada (NT)",
                baseScore: 250, baseCoins: 25,
                minDist: 4f, maxDist: 18f, radius: 1.5f
            );

            // 4. Jatobá
            CreateOrUpdateSpecies(
                folderPath, "Jatoba",
                id: "jatoba",
                commonName: "Jatobá-do-cerrado",
                scientificName: "Hymenaea stigonocarpa",
                category: SpeciesCategory.Flora,
                rarity: SpeciesRarity.Comum,
                activity: ActivityTime.Ambos,
                educational: "Árvore marcante com casca grossa e frutos de casca dura riquíssimos em nutrientes. Sua madeira e resina são historicamente valorizadas.",
                habitat: "Cerradão e cerrado típico.",
                status: "Pouco Preocupante (LC)",
                baseScore: 120, baseCoins: 12,
                minDist: 6f, maxDist: 25f, radius: 3.5f
            );

            // 5. Angico
            CreateOrUpdateSpecies(
                folderPath, "Angico",
                id: "angico",
                commonName: "Angico",
                scientificName: "Anadenanthera colubrina",
                category: SpeciesCategory.Flora,
                rarity: SpeciesRarity.Comum,
                activity: ActivityTime.Ambos,
                educational: "Árvore pioneira muito resistente a solos secos e queimadas ocasionais. Sua casca é rica em taninos e atrai diversos polinizadores.",
                habitat: "Áreas pedregosas e cerrado rupestre.",
                status: "Pouco Preocupante (LC)",
                baseScore: 120, baseCoins: 12,
                minDist: 5f, maxDist: 22f, radius: 3.0f
            );

            // 6. Aroeira
            CreateOrUpdateSpecies(
                folderPath, "Aroeira",
                id: "aroeira",
                commonName: "Aroeira-do-sertão",
                scientificName: "Myracrodruon urundeuva",
                category: SpeciesCategory.Flora,
                rarity: SpeciesRarity.Comum,
                activity: ActivityTime.Ambos,
                educational: "Espécie nobre com madeira de altíssima durabilidade e grande resistência ao estresse hídrico da estação seca.",
                habitat: "Matas secas e cerrado sensu stricto.",
                status: "Quase Ameaçada (NT)",
                baseScore: 140, baseCoins: 14,
                minDist: 5f, maxDist: 20f, radius: 2.8f
            );

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Cerrado Engine", "Espécies iniciais criadas com sucesso na pasta 'Assets/Dados/Especies'!", "OK");
        }

        private static void CreateOrUpdateSpecies(
            string folder, string fileName,
            string id, string commonName, string scientificName,
            SpeciesCategory category, SpeciesRarity rarity, ActivityTime activity,
            string educational, string habitat, string status,
            int baseScore, int baseCoins,
            float minDist, float maxDist, float radius)
        {
            string assetPath = $"{folder}/{fileName}.asset";
            SpeciesData asset = AssetDatabase.LoadAssetAtPath<SpeciesData>(assetPath);

            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<SpeciesData>();
                AssetDatabase.CreateAsset(asset, assetPath);
            }

            // Usando SerializedObject para preencher os campos privados serializados
            SerializedObject so = new SerializedObject(asset);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("commonName").stringValue = commonName;
            so.FindProperty("scientificName").stringValue = scientificName;
            so.FindProperty("category").enumValueIndex = (int)category;
            so.FindProperty("rarity").enumValueIndex = (int)rarity;
            so.FindProperty("activityTime").enumValueIndex = (int)activity;
            so.FindProperty("educationalDescription").stringValue = educational;
            so.FindProperty("habitatDescription").stringValue = habitat;
            so.FindProperty("conservationStatus").stringValue = status;
            so.FindProperty("baseScore").intValue = baseScore;
            so.FindProperty("baseCoins").intValue = baseCoins;
            so.FindProperty("minIdealDistance").floatValue = minDist;
            so.FindProperty("maxIdealDistance").floatValue = maxDist;
            so.FindProperty("targetRadius").floatValue = radius;
            so.ApplyModifiedProperties();
        }
    }
}

