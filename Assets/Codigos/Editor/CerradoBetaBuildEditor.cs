using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace Cerrado.EditorTools
{
    [InitializeOnLoad]
    public static class CerradoBetaBuildEditor
    {
        private const string AutoSetupFlagPath = "Temp/cerrado_beta_autoconfig_v15.done";
        private const string AutoFullBuildTriggerPath = "Temp/cerrado_trigger_full_build_v15.req";

        static CerradoBetaBuildEditor()
        {
            EditorApplication.delayCall += TryAutoConfigureOnEditorLoad;
        }

        private static void TryAutoConfigureOnEditorLoad()
        {
            if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                return;
            }

            bool runAutoSetup = !File.Exists(AutoSetupFlagPath);
            bool runAutoBuild = File.Exists(AutoFullBuildTriggerPath);
            if (!runAutoSetup && !runAutoBuild)
            {
                return;
            }

            try
            {
                if (runAutoBuild && File.Exists(AutoFullBuildTriggerPath))
                {
                    File.Delete(AutoFullBuildTriggerPath);
                }

                Debug.Log("[CerradoBetaBuild] Aplicando pacote final da Beta (Folhagem Translúcida + Vento, Perfil RTX 5070 Muito Alta e Validação Completa)...");
                ConfigureProjectAndScenesOnly();
                ResetGameViewZoomTo1x();
                Directory.CreateDirectory("Temp");
                File.WriteAllText(AutoSetupFlagPath, DateTime.Now.ToString("O"));
                Debug.Log("[CerradoBetaBuild] Correções da Beta aplicadas com sucesso!");

                if (runAutoBuild)
                {
                    Debug.Log("[CerradoBetaBuild] Gatilho automático detectado: iniciando Build Beta Windows x64 + geração de ZIP + Instalador...");
                    ConfigureBuildSettingsAndBuildExecutable(interactiveDialog: false);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CerradoBetaBuild] Erro na auto-configuração/build: {ex}");
            }
        }

        [MenuItem("Cerrado/7. Gerar Build Beta Windows x64 + ZIP")]
        public static void MenuBuildBeta()
        {
            RunFullBetaPipeline(interactiveDialog: true);
        }

        [MenuItem("Cerrado/8. Reaplicar Visual do Menu Principal e Cenas (Sem Build)")]
        public static void ConfigureProjectAndScenesOnly()
        {
            Debug.Log("[CerradoBetaBuild] 0/5 Gerando e configurando fontes tipográficas TMP (Rubik, Fredoka e Lexend PT-BR)...");
            CerradoFontSetupEditor.GenerateAndConfigureProjectFonts();

            Debug.Log("[CerradoBetaBuild] 1/5 Configurando Assets Migrados (URP, Prefabs, Skybox, Folhagem HQ e Áudio)...");
            CerradoMigrationEditor.PrepareMigratedAssetsAndURP();

            Debug.Log("[CerradoBetaBuild] 2/5 Configurando TextureImporters (NormalMaps, Sprites UI e Texturas FBX/Blend)...");
            ConfigureAllNewTextureImporters();

            Debug.Log("[CerradoBetaBuild] 3/5 Gerando ScriptableObjects (38 Espécies com Ícones, 5 Itens da Loja e 6 Missões)...");
            GenerateAllDataSilent();

            Debug.Log("[CerradoBetaBuild] 4/5 Montando Cena Principal da Beta (SampleScene.unity) + Tipografia...");
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
            CerradoSceneSetupEditor.BuildCompleteTestSceneSilent();
            CerradoFontSetupEditor.ApplyTypographyToActiveScene();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

            Debug.Log("[CerradoBetaBuild] 5/5 Reconstruindo Cena do Menu Principal (menu_cena.unity) + Tipografia + Build Settings...");
            UpgradeMainMenuScene();
            EnsureBuildSettingsOrder();
            ResetGameViewZoomTo1x();

            Directory.CreateDirectory("Temp");
            File.WriteAllText(AutoSetupFlagPath, DateTime.Now.ToString("O"));

            Debug.Log("[CerradoBetaBuild] Executando Validação Final de Migração e Cenas...");
            CerradoMigrationEditor.ValidateMigrationAndScene();
        }

        public static void RunBatchBuildAndZip()
        {
            RunFullBetaPipeline(false);
        }

        public static void RunFullBetaPipeline(bool interactiveDialog = false)
        {
            ConfigureProjectAndScenesOnly();

            Debug.Log("[CerradoBetaBuild] 5/6 Configurando Build Settings, compilando executável Windows 64-bit e gerando pacote .ZIP...");
            ConfigureBuildSettingsAndBuildExecutable(interactiveDialog);
        }

        private static void ResetGameViewZoomTo1x()
        {
            try
            {
                Type gvType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView");
                if (gvType == null) return;

                var windows = Resources.FindObjectsOfTypeAll(gvType);
                MethodInfo snapZoom = gvType.GetMethod("SnapZoom", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                foreach (var w in windows)
                {
                    if (snapZoom != null)
                    {
                        snapZoom.Invoke(w, new object[] { 1f });
                    }
                    if (w is EditorWindow ew)
                    {
                        ew.Repaint();
                    }
                }
            }
            catch
            {
            }
        }

        public static void ConfigureAllNewTextureImporters()
        {
            string[] normalMapFiles = new[]
            {
                "Poliigon_GrassPatchyGround_4585_Normal.png",
                "Bark_Podocarpus-macrophyllus_Normal.jpg",
                "Bark_Rough_Normal.jpg",
                "Fern_Normal.jpg",
                "FBX_JacarePapoAmarelo_1.png",
                "FBX_loboguara_1.png",
                "FBX_ArvoreSeca_1.jpg",
                "FBX_chuveirinho_1.jpg",
                "Blend_jatoba_remake_1.png",
                "Blend_jatoba_remake_3.png",
                "Blend_angico_remake_1.png",
                "Blend_angico_remake_3.png",
                "Blend_aroeira_remake_1.png",
                "Blend_aroeira_remake_4.png"
            };
            var normalSet = new HashSet<string>(normalMapFiles, StringComparer.OrdinalIgnoreCase);

            string[] alphaCutoutFiles = new[]
            {
                "Fern_Diffuse.png",
                "Tree_Branch.png",
                "FBX_chuveirinho_0.png",
                "FBX_chuveirinho_2.png",
                "Blend_jatoba_remake_2.png",
                "Blend_angico_remake_2.png",
                "Blend_angico_remake_5.png",
                "Blend_aroeira_remake_2.png",
                "Blend_aroeira_remake_3.png"
            };
            var alphaSet = new HashSet<string>(alphaCutoutFiles, StringComparer.OrdinalIgnoreCase);

            string[] pbrGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Dados/Texturas" });
            foreach (string guid in pbrGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("Ambiente/evening_field_8k") || path.Contains("IconesEspecies")) continue;
                string fn = Path.GetFileName(path);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                bool isNormal = normalSet.Contains(fn) || fn.ToLower().Contains("normal");
                bool isAlpha = alphaSet.Contains(fn);
                TextureImporterType targetType = isNormal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                bool changed = false;

                if (importer.textureType != targetType)
                {
                    importer.textureType = targetType;
                    changed = true;
                }
                if (importer.wrapMode != TextureWrapMode.Repeat)
                {
                    importer.wrapMode = TextureWrapMode.Repeat;
                    changed = true;
                }
                if (isAlpha && !importer.alphaIsTransparency)
                {
                    importer.alphaIsTransparency = true;
                    changed = true;
                }

                if (changed)
                {
                    importer.SaveAndReimport();
                }
            }

            string[] uiFolders = new[] { "Assets/Jogo/UI/Sprites", "Assets/Jogo/Menu/sprites" };
            string[] uiGuids = AssetDatabase.FindAssets("t:Texture2D", uiFolders);
            foreach (string guid in uiGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                bool isMultiSheet = path.EndsWith("icones_jogo_cerrado_sheet.png");
                SpriteImportMode targetMode = isMultiSheet ? SpriteImportMode.Multiple : SpriteImportMode.Single;

                if (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != targetMode)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = targetMode;
                    importer.alphaIsTransparency = true;
                    importer.mipmapEnabled = false;
                    importer.maxTextureSize = 2048;
                    importer.SaveAndReimport();
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void GenerateAllDataSilent()
        {
            CerradoSetupEditor.SuppressDialogs = true;
            try
            {
                CerradoSetupEditor.GenerateInitialSpecies();
                CerradoShopSetupEditor.GenerateInitialShopItems();
                CerradoQuestSetupEditor.GenerateInitialQuests();
            }
            finally
            {
                CerradoSetupEditor.SuppressDialogs = false;
            }
        }

        /// <summary>
        /// Reconstrói completamente a hierarquia de UI do Menu Principal (menu_cena.unity)
        /// eliminando o deslocamento do painel de Opções, os quadrados vazios de madeira e o texto EXTRAS solto.
        /// </summary>
        private static void UpgradeMainMenuScene()
        {
            string menuScenePath = "Assets/Jogo/Menu/menu_cena.unity";
            if (!File.Exists(menuScenePath)) return;

            var scene = EditorSceneManager.OpenScene(menuScenePath, OpenSceneMode.Single);

            var canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
            var menuCtrl = UnityEngine.Object.FindAnyObjectByType<menu_principal>();
            if (canvas == null || menuCtrl == null) return;

            // 1. CanvasScaler 1920x1080 (Match 0.5)
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
            }

            // 2. Limpa todos os filhos antigos do Canvas para construir um layout 100% limpo e centralizado
            var oldChildren = new List<GameObject>();
            foreach (Transform child in canvas.transform)
            {
                oldChildren.Add(child.gameObject);
            }
            foreach (var old in oldChildren)
            {
                UnityEngine.Object.DestroyImmediate(old);
            }

            // Carrega Sprites
            Sprite spBg = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Jogo/Menu/sprites/menu_2.jpg");
            if (spBg == null) spBg = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Jogo/Menu/sprites/menu_4.jpg");
            Sprite spLogoTitulo = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Jogo/UI/Sprites/logo_titulo_eicerrado.png");
            Sprite spJogar = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Jogo/UI/Sprites/btn_jogar.png");
            Sprite spOpcoes = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Jogo/UI/Sprites/btn_opcoes.png");
            Sprite spSair = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Jogo/UI/Sprites/btn_sair.png");
            Sprite spVoltar = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Jogo/UI/Sprites/btn_voltar.png");
            Sprite spBarraEscura = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Jogo/UI/Sprites/barra_madeira_escura.png");
            Sprite spBarraClara = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Jogo/UI/Sprites/barra_madeira_clara.png");
            Sprite spLogoEic = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Jogo/Menu/sprites/logo_eic.png");
            Sprite spLogoUsp = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Jogo/Menu/sprites/logousppbsite.png");

            // 3. Fundo Fotográfico Full-Screen (RaycastTarget = false)
            GameObject bgObj = CreateUIImage(canvas.transform, "Fundo_Cerrado_Beta", spBg, new Color(0.84f, 0.80f, 0.74f, 1f), Vector2.zero, Vector2.zero, false);
            SetFullStretch(bgObj.GetComponent<RectTransform>());

            // 4. Plaquinha Bege/Amadeirada de Rodapé para as Logos Institucionais (USP / EIC)
            // Segue a mesma paleta das placas de JOGAR, OPÇÕES e SAIR (borda marrom amadeirada + fundo bege claro)
            GameObject molduraLogos = CreateUIImage(canvas.transform, "Moldura_Rodape_Logos", spBarraEscura, new Color(0.32f, 0.20f, 0.11f, 0.98f), new Vector2(-690f, -452f), new Vector2(440f, 112f), false);
            GameObject placaLogos = CreateUIImage(molduraLogos.transform, "Placa_Bege_Logos", spBarraClara, new Color(0.95f, 0.89f, 0.74f, 1f), Vector2.zero, new Vector2(416f, 92f), false);

            GameObject logoEicObj = CreateUIImage(placaLogos.transform, "Logo_EIC", spLogoEic, Color.white, new Vector2(-102f, 0f), new Vector2(175f, 70f), false);
            logoEicObj.GetComponent<Image>().preserveAspect = true;

            GameObject logoUspObj = CreateUIImage(placaLogos.transform, "Logo_USP", spLogoUsp, Color.white, new Vector2(102f, 0f), new Vector2(175f, 70f), false);
            logoUspObj.GetComponent<Image>().preserveAspect = true;

            // =========================================================================
            // 5. PAINEL 1: MENU PRINCIPAL
            // =========================================================================
            GameObject painelPrincipal = CreateUIPanel(canvas.transform, "Painel_Menu_Principal");

            // Placa de Título no Topo com Texto "EICerrado!" em Bege Claro (alto contraste sobre a placa marrom escura)
            GameObject placaTitulo = CreateUIImage(painelPrincipal.transform, "Placa_Titulo", spBarraEscura, Color.white, new Vector2(0f, 325f), new Vector2(960f, 165f), false);
            TMP_Text txtTituloPrincipal = CreateUIText(
                placaTitulo.transform,
                "Titulo_EICerrado",
                "EICerrado!",
                68f,
                FontStyles.Bold,
                new Color(0.97f, 0.91f, 0.76f, 1f), // Bege claro combinando com o subtítulo e com os botões JOGAR/OPÇÕES/SAIR
                new Vector2(0f, 18f),
                new Vector2(860f, 88f)
            );
            var titleOutline = txtTituloPrincipal.gameObject.AddComponent<Outline>();
            titleOutline.effectColor = new Color(0.18f, 0.10f, 0.05f, 0.92f);
            titleOutline.effectDistance = new Vector2(2.5f, -2.5f);

            TMP_Text txtSubtitulo = CreateUIText(
                placaTitulo.transform,
                "Subtitulo",
                "Pra lá do Cerrado — Expedição Fotográfica",
                24f,
                FontStyles.Bold,
                new Color(0.95f, 0.87f, 0.68f, 1f),
                new Vector2(0f, -44f),
                new Vector2(840f, 36f)
            );
            var subOutline = txtSubtitulo.gameObject.AddComponent<Outline>();
            subOutline.effectColor = new Color(0.16f, 0.09f, 0.04f, 0.85f);
            subOutline.effectDistance = new Vector2(1.5f, -1.5f);

            // Botões Principais Centralizados (JOGAR, OPÇÕES, SAIR)
            Button btnJogar = CreateSpriteButton(painelPrincipal.transform, "Btn_Jogar", spJogar, "JOGAR", new Vector2(0f, 95f), new Vector2(380f, 125f));
            UnityEditor.Events.UnityEventTools.AddPersistentListener(btnJogar.onClick, menuCtrl.jogar);

            Button btnOpcoes = CreateSpriteButton(painelPrincipal.transform, "Btn_Opcoes", spOpcoes, "OPÇÕES", new Vector2(0f, -55f), new Vector2(380f, 125f));
            UnityEditor.Events.UnityEventTools.AddPersistentListener(btnOpcoes.onClick, menuCtrl.abrir_opcoes);

            Button btnSair = CreateSpriteButton(painelPrincipal.transform, "Btn_Sair", spSair, "SAIR", new Vector2(0f, -205f), new Vector2(380f, 125f));
            UnityEditor.Events.UnityEventTools.AddPersistentListener(btnSair.onClick, menuCtrl.sair_jogo);

            // Botão único e limpo de CRÉDITOS no canto inferior direito (sem texto solto nem barra duplicada)
            GameObject btnCreditosObj = CreateUIImage(painelPrincipal.transform, "Btn_Creditos", spBarraEscura, Color.white, new Vector2(760f, -455f), new Vector2(240f, 70f), true);
            Button btnCreditos = btnCreditosObj.AddComponent<Button>();
            CreateUIText(btnCreditosObj.transform, "Texto", "CRÉDITOS", 24f, FontStyles.Bold, new Color(1f, 0.95f, 0.82f), Vector2.zero, new Vector2(220f, 50f));
            UnityEditor.Events.UnityEventTools.AddPersistentListener(btnCreditos.onClick, menuCtrl.abrir_extras);

            // =========================================================================
            // 6. PAINEL 2: OPÇÕES COMPLETAS (Gráficos 4 Níveis, Música, Ambiente/SFX, Sensibilidade Digitável e Tutorial)
            // =========================================================================
            GameObject painelOpcoes = CreateUIPanel(canvas.transform, "Painel_Opcoes");

            var (_, btnVoltarOpcoes) = CerradoSettingsUIBuilder.BuildWoodSettingsCard(
                painelOpcoes.transform,
                "Card_Opcoes",
                "Pressione [ESC] ou clique em Voltar para retornar ao menu principal"
            );
            UnityEditor.Events.UnityEventTools.AddPersistentListener(btnVoltarOpcoes.onClick, menuCtrl.fechar_opcoes);

            painelOpcoes.SetActive(false);

            // =========================================================================
            // 7. PAINEL 3: CRÉDITOS / EXTRAS (Centralizado e com Botão Voltar Funcional)
            // =========================================================================
            GameObject painelExtras = CreateUIPanel(canvas.transform, "Painel_Extras");

            GameObject cardExtras = CreateUIImage(painelExtras.transform, "Card_Extras", null, new Color(0.08f, 0.11f, 0.08f, 0.96f), Vector2.zero, new Vector2(920f, 600f), true);
            CreateUIImage(cardExtras.transform, "Header_Extras", null, new Color(0.14f, 0.20f, 0.14f, 1f), new Vector2(0f, 250f), new Vector2(920f, 100f), false);
            CreateUIText(cardExtras.transform, "Titulo_Extras", "CRÉDITOS — EICERRADO", 40f, FontStyles.Bold, new Color(0.96f, 0.84f, 0.35f), new Vector2(0f, 250f), new Vector2(760f, 60f));

            string creditosTexto =
                "<b>Universidade de São Paulo (USP)</b>\n" +
                "Jogo Educativo 3D de Exploração e Fotografia Ambiental no Cerrado\n\n" +
                "<b>Equipe de Desenvolvimento:</b>\n" +
                "- Lucas Cordeiro Gatti\n" +
                "- Yuri de Oliveira Oba";
            CreateUIText(cardExtras.transform, "Corpo_Creditos", creditosTexto, 24f, FontStyles.Normal, new Color(0.92f, 0.95f, 0.90f), new Vector2(0f, 35f), new Vector2(800f, 280f));

            Button btnVoltarExtras = CreateSpriteButton(cardExtras.transform, "Btn_Voltar_Extras", spVoltar, "VOLTAR", new Vector2(0f, -205f), new Vector2(320f, 105f));
            UnityEditor.Events.UnityEventTools.AddPersistentListener(btnVoltarExtras.onClick, menuCtrl.fechar_extras);

            painelExtras.SetActive(false);

            // 8. Vincula os 3 painéis no componente menu_principal
            var soMenu = new SerializedObject(menuCtrl);
            soMenu.FindProperty("nomeDoLevel").stringValue = "SampleScene";
            soMenu.FindProperty("painel_menu_principal").objectReferenceValue = painelPrincipal;
            soMenu.FindProperty("painel_opcoes").objectReferenceValue = painelOpcoes;
            soMenu.FindProperty("painel_extras").objectReferenceValue = painelExtras;
            soMenu.ApplyModifiedProperties();

            CerradoFontSetupEditor.ApplyTypographyToActiveScene();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static GameObject CreateUIPanel(Transform parent, string name)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform));
            panel.transform.SetParent(parent, false);
            SetFullStretch(panel.GetComponent<RectTransform>());
            return panel;
        }

        private static GameObject CreateUIImage(Transform parent, string name, Sprite sprite, Color color, Vector2 anchoredPos, Vector2 sizeDelta, bool raycastTarget)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;

            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = raycastTarget;
            return go;
        }

        private static TMP_Text CreateUIText(Transform parent, string name, string text, float fontSize, FontStyles style, Color color, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;

            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static Button CreateSpriteButton(Transform parent, string name, Sprite sprite, string fallbackText, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            GameObject go = CreateUIImage(parent, name, sprite, Color.white, anchoredPos, sizeDelta, true);
            var img = go.GetComponent<Image>();
            if (sprite != null)
            {
                img.preserveAspect = true;
            }
            else
            {
                img.color = new Color(0.22f, 0.45f, 0.22f, 1f);
                CreateUIText(go.transform, "Texto", fallbackText, 32f, FontStyles.Bold, Color.white, Vector2.zero, sizeDelta);
            }
            return go.AddComponent<Button>();
        }

        private static Slider CreateCleanSlider(Transform parent, string name, Vector2 anchoredPos, Vector2 sizeDelta, float defaultVal)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(Slider));
            root.transform.SetParent(parent, false);
            var rt = root.GetComponent<RectTransform>();
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;

            GameObject bg = CreateUIImage(root.transform, "Background", null, new Color(0.18f, 0.22f, 0.18f, 1f), Vector2.zero, sizeDelta, true);
            SetFullStretch(bg.GetComponent<RectTransform>());

            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(root.transform, false);
            var rtFillArea = fillArea.GetComponent<RectTransform>();
            SetFullStretch(rtFillArea);

            GameObject fill = CreateUIImage(fillArea.transform, "Fill", null, new Color(0.85f, 0.68f, 0.22f, 1f), Vector2.zero, Vector2.zero, false);
            var rtFill = fill.GetComponent<RectTransform>();
            SetFullStretch(rtFill);

            var slider = root.GetComponent<Slider>();
            slider.fillRect = rtFill;
            slider.targetGraphic = bg.GetComponent<Image>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = defaultVal;
            return slider;
        }

        private static void SetFullStretch(RectTransform rect)
        {
            if (rect == null) return;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void EnsureBuildSettingsOrder()
        {
            var scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene("Assets/Jogo/Menu/menu_cena.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/SampleScene.unity", true)
            };
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void ConfigureBuildSettingsAndBuildExecutable(bool interactiveDialog = false)
        {
            EnsureBuildSettingsOrder();

            PlayerSettings.companyName = "USP - EICerrado";
            PlayerSettings.productName = "EICerrado";
            PlayerSettings.bundleVersion = "1.0.0-beta";

            string buildDir = "Builds/Beta_EICerrado";
            if (!Directory.Exists(buildDir))
            {
                Directory.CreateDirectory(buildDir);
            }

            string exePath = Path.Combine(buildDir, "EICerrado.exe");
            var buildOptions = new BuildPlayerOptions
            {
                scenes = new[]
                {
                    "Assets/Jogo/Menu/menu_cena.unity",
                    "Assets/Scenes/SampleScene.unity"
                },
                locationPathName = exePath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[CerradoBetaBuild] BUILD BETA CONCLUÍDA COM SUCESSO! Executável: {Path.GetFullPath(exePath)} ({report.summary.totalSize / (1024 * 1024)} MB)");

                // Remove pastas temporárias de símbolos DoNotShip para manter o pacote .zip e o instalador enxutos
                CleanDoNotShipFolders(buildDir);

                // Compacta automaticamente toda a pasta Builds/Beta_EICerrado em Builds/EICerrado_Beta_Win64.zip
                string zipPath = CreateDistributableZip(buildDir, "Builds/EICerrado_Beta_Win64.zip");

                // Compila o instalador Inno Setup (Builds/installer_beta.iss -> Builds/Instalador_EICerrado_Beta.exe)
                CompileInnoSetupInstaller(exePath, zipPath, interactiveDialog);
            }
            else
            {
                Debug.LogError($"[CerradoBetaBuild] Falha na build: {report.summary.result} (Erros: {report.summary.totalErrors})");
            }
        }

        private static void CleanDoNotShipFolders(string buildDir)
        {
            if (!Directory.Exists(buildDir)) return;
            foreach (string subDir in Directory.GetDirectories(buildDir))
            {
                string dirName = Path.GetFileName(subDir);
                if (dirName.IndexOf("DoNotShip", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    dirName.IndexOf("BackUpThisFolder", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    try
                    {
                        Directory.Delete(subDir, true);
                        Debug.Log($"[CerradoBetaBuild] Pasta temporária de debug removida antes de compactar: {dirName}");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[CerradoBetaBuild] Não foi possível remover pasta {dirName}: {ex.Message}");
                    }
                }
            }
        }

        private static string CreateDistributableZip(string sourceBuildDir, string targetZipRelativePath)
        {
            string fullSourceDir = Path.GetFullPath(sourceBuildDir);
            string fullZipPath = Path.GetFullPath(targetZipRelativePath);
            string parentDir = Path.GetDirectoryName(fullZipPath);
            if (!string.IsNullOrEmpty(parentDir) && !Directory.Exists(parentDir))
            {
                Directory.CreateDirectory(parentDir);
            }

            if (File.Exists(fullZipPath))
            {
                File.Delete(fullZipPath);
            }

            Debug.Log($"[CerradoBetaBuild] Compactando '{fullSourceDir}' em '{fullZipPath}'...");
            ZipFile.CreateFromDirectory(fullSourceDir, fullZipPath, System.IO.Compression.CompressionLevel.Optimal, includeBaseDirectory: false);

            var zipInfo = new FileInfo(fullZipPath);
            double sizeMb = zipInfo.Exists ? (zipInfo.Length / (1024.0 * 1024.0)) : 0.0;
            Debug.Log($"[CerradoBetaBuild] PACOTE ZIP GERADO COM SUCESSO: {fullZipPath} ({sizeMb:F1} MB)");

            // Se o WinRAR CLI (Rar.exe) estiver instalado no PC, gera também Builds/EICerrado_Beta_Win64.rar ao lado do .zip
            string rarExe = @"C:\Program Files\WinRAR\Rar.exe";
            if (!File.Exists(rarExe)) rarExe = @"C:\Program Files (x86)\WinRAR\Rar.exe";
            if (File.Exists(rarExe))
            {
                try
                {
                    string fullRarPath = Path.ChangeExtension(fullZipPath, ".rar");
                    if (File.Exists(fullRarPath)) File.Delete(fullRarPath);

                    var psiRar = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = rarExe,
                        Arguments = $"a -r -ep1 -m3 \"{fullRarPath}\" \"{fullSourceDir}\\*\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };
                    using (var pRar = System.Diagnostics.Process.Start(psiRar))
                    {
                        pRar.WaitForExit();
                        if (pRar.ExitCode == 0 && File.Exists(fullRarPath))
                        {
                            double rarMb = new FileInfo(fullRarPath).Length / (1024.0 * 1024.0);
                            Debug.Log($"[CerradoBetaBuild] PACOTE RAR GERADO COM SUCESSO: {fullRarPath} ({rarMb:F1} MB)");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[CerradoBetaBuild] Aviso ao gerar pacote .rar opcional: {ex.Message}");
                }
            }

            return fullZipPath;
        }

        private static void CompileInnoSetupInstaller(string builtExePath, string builtZipPath, bool interactiveDialog)
        {
            string[] issScripts = new[]
            {
                Path.GetFullPath("Builds/installer_beta.iss"),
                Path.GetFullPath("Installer/EICerrado_Beta_Setup.iss")
            };

            string[] candidateIsccPaths = new[]
            {
                Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), @"Programs\Inno Setup 6\ISCC.exe"),
                @"C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
                @"C:\Program Files\Inno Setup 6\ISCC.exe"
            };

            string isccExe = null;
            foreach (var candidate in candidateIsccPaths)
            {
                if (File.Exists(candidate))
                {
                    isccExe = candidate;
                    break;
                }
            }

            string lastBuiltInstaller = null;
            if (string.IsNullOrEmpty(isccExe))
            {
                Debug.LogWarning("[CerradoBetaBuild] ISCC.exe (Inno Setup 6) não encontrado nos caminhos padrão. O arquivo .ZIP já está pronto para distribuição.");
            }
            else
            {
                foreach (string issPath in issScripts)
                {
                    if (!File.Exists(issPath)) continue;

                    Debug.Log($"[CerradoBetaBuild] 6/6 Compilando instalador Windows (.exe) via Inno Setup: {issPath}...");
                    var psi = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = isccExe,
                        Arguments = $"\"{issPath}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };

                    using (var proc = System.Diagnostics.Process.Start(psi))
                    {
                        string stdout = proc.StandardOutput.ReadToEnd();
                        string stderr = proc.StandardError.ReadToEnd();
                        proc.WaitForExit();

                        if (proc.ExitCode == 0)
                        {
                            string expectedInstaller = issPath.IndexOf("installer_beta.iss", StringComparison.OrdinalIgnoreCase) >= 0
                                ? Path.GetFullPath("Builds/Instalador_EICerrado_Beta.exe")
                                : Path.GetFullPath("Builds/Installer/EICerrado_Beta_Setup.exe");
                            lastBuiltInstaller = expectedInstaller;
                            Debug.Log($"[CerradoBetaBuild] INSTALADOR .EXE GERADO COM SUCESSO: {expectedInstaller}");
                        }
                        else
                        {
                            Debug.LogError($"[CerradoBetaBuild] Erro ao compilar instalador Inno Setup ({issPath}, Código {proc.ExitCode}):\n{stdout}\n{stderr}");
                        }
                    }
                }
            }

            if (interactiveDialog && !Application.isBatchMode)
            {
                EditorUtility.RevealInFinder(builtZipPath);
                EditorUtility.DisplayDialog("Cerrado Engine — Build Beta, ZIP & Instalador",
                    $"Pacote Beta gerado com sucesso!\n\n" +
                    $"- Executavel: {Path.GetFullPath(builtExePath)}\n" +
                    $"- Arquivo ZIP: {builtZipPath}\n" +
                    (lastBuiltInstaller != null ? $"- Instalador: {lastBuiltInstaller}" : ""), "OK");
            }
        }
    }
}
