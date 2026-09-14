using UnityEngine;
using UnityEditor;
using System.IO;
using Cerrado.Quests;
using Cerrado.Data;

namespace Cerrado.EditorTools
{
    public static class CerradoQuestSetupEditor
    {
        [MenuItem("Cerrado/3. Gerar Missões Iniciais do Guia (GDD)")]
        public static void GenerateInitialQuests()
        {
            string folderPath = "Assets/Dados/Missoes";
            if (!AssetDatabase.IsValidFolder("Assets/Dados"))
            {
                AssetDatabase.CreateFolder("Assets", "Dados");
            }
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder("Assets/Dados", "Missoes");
            }

            // Carrega espécies para conectar aos objetivos
            SpeciesData tamandua = AssetDatabase.LoadAssetAtPath<SpeciesData>("Assets/Dados/Especies/TamanduaBandeira.asset");
            SpeciesData ema = AssetDatabase.LoadAssetAtPath<SpeciesData>("Assets/Dados/Especies/Ema.asset");
            SpeciesData lobo = AssetDatabase.LoadAssetAtPath<SpeciesData>("Assets/Dados/Especies/LoboGuara.asset");
            SpeciesData jatoba = AssetDatabase.LoadAssetAtPath<SpeciesData>("Assets/Dados/Especies/Jatoba.asset");

            // 1. Missão 1: Tamanduá
            CreateOrUpdateQuest(
                folderPath, "MissaoTamandua",
                id: "quest_tamandua",
                title: "O Gigante Insetívoro",
                description: "Encontre o Tamanduá-bandeira nos campos do Cerrado e registre uma foto de no mínimo 2 estrelas.",
                targetSpecies: tamandua,
                minStars: 2,
                nightOnly: false,
                rewardCoins: 60,
                unlockedArea: ""
            );

            // 2. Missão 2: Ema
            CreateOrUpdateQuest(
                folderPath, "MissaoEma",
                id: "quest_ema",
                title: "A Veloz do Cerrado",
                description: "Aproxime-se agachado (Ctrl) e fotografe uma Ema com no mínimo 3 estrelas de nitidez.",
                targetSpecies: ema,
                minStars: 3,
                nightOnly: false,
                rewardCoins: 85,
                unlockedArea: ""
            );

            // 3. Missão 3: Lobo Guará Noturno
            CreateOrUpdateQuest(
                folderPath, "MissaoLoboNoturno",
                id: "quest_lobo_noite",
                title: "O Fantasma da Noite",
                description: "Aguarde o anoitecer no Cerrado e documente o raro Lobo-guará sob o luar. Recompensa o acesso à Chapada!",
                targetSpecies: lobo,
                minStars: 2,
                nightOnly: true,
                rewardCoins: 160,
                unlockedArea: "area_chapada"
            );

            // 4. Missão 4: Jatobá Majestoso
            CreateOrUpdateQuest(
                folderPath, "MissaoJatoba",
                id: "quest_jatoba",
                title: "Árvores Nativas: O Jatobá",
                description: "Fotografe a imponente copa de um Jatobá com enquadramento perfeito (4 estrelas ou mais).",
                targetSpecies: jatoba,
                minStars: 4,
                nightOnly: false,
                rewardCoins: 75,
                unlockedArea: ""
            );

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Cerrado Engine", "Missões criadas com sucesso na pasta 'Assets/Dados/Missoes'!", "OK");
        }

        private static void CreateOrUpdateQuest(
            string folder, string fileName,
            string id, string title, string description,
            SpeciesData targetSpecies, int minStars, bool nightOnly, int rewardCoins, string unlockedArea)
        {
            string assetPath = $"{folder}/{fileName}.asset";
            QuestData asset = AssetDatabase.LoadAssetAtPath<QuestData>(assetPath);

            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<QuestData>();
                AssetDatabase.CreateAsset(asset, assetPath);
            }

            SerializedObject so = new SerializedObject(asset);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("title").stringValue = title;
            so.FindProperty("description").stringValue = description;
            so.FindProperty("targetSpecies").objectReferenceValue = targetSpecies;
            so.FindProperty("minStarsRequired").intValue = minStars;
            so.FindProperty("requiresNight").boolValue = nightOnly;
            so.FindProperty("rewardCoins").intValue = rewardCoins;
            so.FindProperty("unlockedAreaId").stringValue = unlockedArea;
            so.ApplyModifiedProperties();
        }
    }
}

