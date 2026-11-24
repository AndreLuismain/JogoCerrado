using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.TextCore.LowLevel;
using TMPro;

namespace Cerrado.EditorTools
{
    /// <summary>
    /// Gera os TMP_FontAsset (SDF) das fontes Rubik, Fredoka (Display/Títulos/Botões) e Lexend (Leitura/HUD)
    /// com suporte completo à acentuação em português (À-ÿ, ç, ã, õ, é, ê, á, â, í, ó, ú + pontuação),
    /// configura o Default Font Asset no TMP Settings.asset e aplica a hierarquia tipográfica nas cenas sem cortes.
    /// </summary>
    public static class CerradoFontSetupEditor
    {
        public const string SdfFolder = "Assets/Fontes/SDF";
        public const string DisplayRubikAssetPath = "Assets/Fontes/SDF/Rubik_Bold_SDF.asset";
        public const string DisplayFredokaAssetPath = "Assets/Fontes/SDF/Fredoka_SemiBold_SDF.asset";
        public const string ReadingLexendAssetPath = "Assets/Fontes/SDF/Lexend_Regular_SDF.asset";
        public const string ReadingLexendSemiBoldPath = "Assets/Fontes/SDF/Lexend_SemiBold_SDF.asset";

        [MenuItem("Cerrado/10. Gerar Fontes TMP (Rubik, Fredoka e Lexend) e Aplicar nas Cenas")]
        public static void GenerateAndConfigureProjectFonts()
        {
            EnsureFolder("Assets/Fontes");
            EnsureFolder(SdfFolder);

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            string fullPtBrCharset = BuildFullPortugueseCharset();

            TMP_FontAsset rubikDisplay = CreateOrUpdateSdfFontAsset(
                "Assets/Fontes/Rubik/Rubik-Bold.ttf",
                DisplayRubikAssetPath,
                "Rubik_Bold_SDF",
                fullPtBrCharset
            );

            TMP_FontAsset fredokaDisplay = CreateOrUpdateSdfFontAsset(
                "Assets/Fontes/Fredoka/Fredoka-SemiBold.ttf",
                DisplayFredokaAssetPath,
                "Fredoka_SemiBold_SDF",
                fullPtBrCharset
            );

            TMP_FontAsset lexendReading = CreateOrUpdateSdfFontAsset(
                "Assets/Fontes/Lexend/Lexend-Regular.ttf",
                ReadingLexendAssetPath,
                "Lexend_Regular_SDF",
                fullPtBrCharset
            );

            CreateOrUpdateSdfFontAsset(
                "Assets/Fontes/Lexend/Lexend-SemiBold.ttf",
                ReadingLexendSemiBoldPath,
                "Lexend_SemiBold_SDF",
                fullPtBrCharset
            );

            ConfigureTmpGlobalSettings(lexendReading, rubikDisplay, fredokaDisplay);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("[CerradoFonts] TMP_FontAssets (Rubik_Bold_SDF, Fredoka_SemiBold_SDF, Lexend_Regular_SDF) gerados com acentuação PT-BR completa e Default Font configurado!");
        }

        private static string BuildFullPortugueseCharset()
        {
            var sb = new StringBuilder(512);
            // ASCII visível completo (espaço a ~)
            for (int c = 32; c <= 126; c++)
            {
                sb.Append((char)c);
            }
            // Suplemento Latin-1 completo (U+00A0 a U+00FF): À-ÿ, ç, Ç, ã, Ã, õ, Õ, á, é, í, ó, ú, â, ê, ô, à, ü, °, «, », ±, ×, ÷
            for (int c = 160; c <= 255; c++)
            {
                sb.Append((char)c);
            }
            // Pontuação tipográfica usada nos textos do jogo (travessão —, meia-risca –, reticências …, aspas curvas, bullet •)
            sb.Append("–—―‘’‚“”„•…‰‹›€™");
            return sb.ToString();
        }

        private static TMP_FontAsset CreateOrUpdateSdfFontAsset(
            string ttfPath, string targetAssetPath, string assetName, string charset)
        {
            Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
            if (sourceFont == null)
            {
                AssetDatabase.ImportAsset(ttfPath, ImportAssetOptions.ForceSynchronousImport);
                sourceFont = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
            }
            if (sourceFont == null)
            {
                Debug.LogError($"[CerradoFonts] Fonte TTF não encontrada em: {ttfPath}");
                return null;
            }

            // Se já existe e possui atlas válido + glifos PT-BR, apenas garante os caracteres e retorna
            TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(targetAssetPath);
            if (existing != null && existing.atlasTexture != null && existing.material != null)
            {
                existing.TryAddCharacters(charset, out _);
                EditorUtility.SetDirty(existing);
                EditorUtility.SetDirty(existing.material);
                return existing;
            }

            if (existing != null)
            {
                AssetDatabase.DeleteAsset(targetAssetPath);
            }

            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
                sourceFont,
                56,
                6,
                GlyphRenderMode.SDFAA,
                1024,
                1024,
                AtlasPopulationMode.Dynamic,
                false
            );

            if (fontAsset == null)
            {
                Debug.LogError($"[CerradoFonts] Falha ao criar TMP_FontAsset para {ttfPath}");
                return null;
            }

            fontAsset.name = assetName;
            fontAsset.isMultiAtlasTexturesEnabled = false;

            // Pré-popula toda a tabela de glifos em português no atlas 1024x1024
            fontAsset.TryAddCharacters(charset, out string missingChars);
            if (!string.IsNullOrEmpty(missingChars))
            {
                Debug.LogWarning($"[CerradoFonts] Alguns símbolos especiais ausentes em {assetName}: {missingChars}");
            }

            AssetDatabase.CreateAsset(fontAsset, targetAssetPath);

            if (fontAsset.atlasTextures != null)
            {
                for (int i = 0; i < fontAsset.atlasTextures.Length; i++)
                {
                    Texture2D atlasTex = fontAsset.atlasTextures[i];
                    if (atlasTex != null && !EditorUtility.IsPersistent(atlasTex))
                    {
                        atlasTex.name = $"{assetName} Atlas {i}";
                        AssetDatabase.AddObjectToAsset(atlasTex, fontAsset);
                    }
                }
            }

            if (fontAsset.material != null && !EditorUtility.IsPersistent(fontAsset.material))
            {
                fontAsset.material.name = $"{assetName} Material";
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }

            EditorUtility.SetDirty(fontAsset);
            if (fontAsset.material != null) EditorUtility.SetDirty(fontAsset.material);
            AssetDatabase.SaveAssets();

            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(targetAssetPath);
        }

        private static void ConfigureTmpGlobalSettings(
            TMP_FontAsset defaultReadingFont, TMP_FontAsset displayRubik, TMP_FontAsset displayFredoka)
        {
            const string tmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
            UnityEngine.Object[] settingsObjs = AssetDatabase.LoadAllAssetsAtPath(tmpSettingsPath);
            if (settingsObjs == null || settingsObjs.Length == 0 || defaultReadingFont == null) return;

            var so = new SerializedObject(settingsObjs[0]);
            var propDefault = so.FindProperty("m_defaultFontAsset");
            if (propDefault != null)
            {
                propDefault.objectReferenceValue = defaultReadingFont;
            }

            // CRÍTICO: impede que o BuildPlayer limpe os glifos pré-gravados nos TMP_FontAssets dinâmicos!
            var propClearOnBuild = so.FindProperty("m_ClearDynamicDataOnBuild");
            if (propClearOnBuild != null)
            {
                propClearOnBuild.boolValue = false;
            }

            // Mantém LiberationSans SDF como fallback seguro para símbolos raros
            TMP_FontAsset libSans = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            var propFallbacks = so.FindProperty("m_fallbackFontAssets");
            if (propFallbacks != null && propFallbacks.isArray && libSans != null)
            {
                propFallbacks.ClearArray();
                propFallbacks.InsertArrayElementAtIndex(0);
                propFallbacks.GetArrayElementAtIndex(0).objectReferenceValue = libSans;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settingsObjs[0]);
        }

        /// <summary>
        /// Aplica a fonte de Display (Rubik Bold / Fredoka SemiBold) em títulos, botões e cabeçalhos,
        /// e a fonte de Leitura (Lexend Regular) em HUD, textos científicos, missões, diálogos e controles,
        /// garantindo contraste alto e prevenção contra corte/overflow.
        /// </summary>
        public static void ApplyTypographyToActiveScene()
        {
            TMP_FontAsset displayRubik = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DisplayRubikAssetPath);
            TMP_FontAsset displayFredoka = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DisplayFredokaAssetPath);
            TMP_FontAsset readingLexend = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ReadingLexendAssetPath);
            TMP_FontAsset readingSemiBold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ReadingLexendSemiBoldPath);

            if (displayRubik == null || readingLexend == null)
            {
                GenerateAndConfigureProjectFonts();
                displayRubik = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DisplayRubikAssetPath);
                displayFredoka = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DisplayFredokaAssetPath);
                readingLexend = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ReadingLexendAssetPath);
                readingSemiBold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ReadingLexendSemiBoldPath);
            }
            if (displayRubik == null || readingLexend == null) return;

            var allTexts = UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int displayCount = 0;
            int readingCount = 0;

            foreach (var tmp in allTexts)
            {
                if (tmp == null) continue;

                string objName = tmp.gameObject.name.ToLowerInvariant();
                string parentName = tmp.transform.parent != null ? tmp.transform.parent.name.ToLowerInvariant() : "";
                bool insideButton = tmp.GetComponentInParent<Button>() != null && tmp.GetComponentInParent<TMP_Dropdown>() == null;
                bool insideInput = tmp.GetComponentInParent<TMP_InputField>() != null;
                bool insideDropdown = tmp.GetComponentInParent<TMP_Dropdown>() != null;

                bool isFriendlyDisplay =
                    objName == "titulo_eicerrado" ||
                    objName == "num" ||
                    parentName.Contains("badge_num");

                bool isDisplayHeaderOrButton =
                    !insideInput && !insideDropdown && (
                        insideButton ||
                        objName.StartsWith("titulo") ||
                        objName.Contains("header") ||
                        objName == "titulocaderno" ||
                        objName == "moedastexto" ||
                        objName == "nomeitem" ||
                        objName == "precoitem" ||
                        objName == "texto_limite" ||
                        objName == "texto_x"
                    );

                if (isFriendlyDisplay && displayFredoka != null)
                {
                    tmp.font = displayFredoka;
                    displayCount++;
                }
                else if (isDisplayHeaderOrButton)
                {
                    tmp.font = displayRubik;
                    displayCount++;
                }
                else if (objName.StartsWith("label_") || objName.StartsWith("pct_") || objName.Contains("status") || objName == "subtitulo")
                {
                    tmp.font = readingSemiBold != null ? readingSemiBold : readingLexend;
                    readingCount++;
                }
                else
                {
                    tmp.font = readingLexend;
                    readingCount++;
                }

                // Garante zero corte/estouro de caixa em botões, cabeçalhos e células compactas
                if (!insideInput)
                {
                    tmp.overflowMode = TextOverflowModes.Overflow;
                    if (insideButton || objName.StartsWith("titulo") || objName == "titulocaderno" || objName == "nomeitem")
                    {
                        float baseSize = tmp.fontSize;
                        tmp.enableAutoSizing = true;
                        tmp.fontSizeMax = baseSize;
                        tmp.fontSizeMin = Mathf.Max(12f, Mathf.Floor(baseSize * 0.68f));
                        tmp.textWrappingMode = (objName == "texto_limite") ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
                    }
                }

                // Contraste reforçado (Outline escuro sutil) em textos sobrepostos ao mundo/câmera/HUD
                bool isHudOverlay =
                    objName == "listatexto" ||
                    objName == "textoatalhos" ||
                    objName.Contains("zoom") ||
                    objName.Contains("rec") ||
                    objName == "textoprompt" ||
                    objName.Contains("status");

                if (isHudOverlay && tmp is TextMeshProUGUI)
                {
                    var outline = tmp.gameObject.GetComponent<Outline>();
                    if (outline == null) outline = tmp.gameObject.AddComponent<Outline>();
                    outline.effectColor = new Color(0.05f, 0.04f, 0.02f, 0.85f);
                    outline.effectDistance = new Vector2(1.2f, -1.2f);
                }

                EditorUtility.SetDirty(tmp);
            }

            Debug.Log($"[CerradoFonts] Tipografia aplicada na cena '{UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}': {displayCount} textos Display (Rubik/Fredoka) e {readingCount} textos Leitura/HUD (Lexend).");
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath)) return;
            string parent = Path.GetDirectoryName(folderPath).Replace("\\", "/");
            string name = Path.GetFileName(folderPath);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
