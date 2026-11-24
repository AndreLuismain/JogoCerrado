using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using Cerrado.UI;
using Cerrado.Settings;
using Cerrado.Player;

namespace Cerrado.EditorTools
{
    /// <summary>
    /// Construtor compartilhado da tela de Configurações completa (identidade visual amadeirada/bege do Cerrado)
    /// e do painel flutuante de Tutorial Passo a Passo, garantindo visual idêntico no Menu Inicial e na Pausa In-Game.
    /// </summary>
    public static class CerradoSettingsUIBuilder
    {
        private static readonly Color WoodFrameBg    = new Color(0.22f, 0.14f, 0.08f, 0.96f);
        private static readonly Color WoodHeaderBg   = new Color(0.32f, 0.20f, 0.11f, 1.00f);
        private static readonly Color WoodBorderGold = new Color(0.84f, 0.68f, 0.42f, 0.92f);
        private static readonly Color BeigeInnerCard = new Color(0.95f, 0.90f, 0.78f, 0.97f);
        private static readonly Color BeigeFieldBg   = new Color(0.97f, 0.93f, 0.83f, 1.00f);
        private static readonly Color DarkWoodText   = new Color(0.20f, 0.12f, 0.06f, 1.00f);
        private static readonly Color CreamLightText = new Color(0.98f, 0.94f, 0.84f, 1.00f);
        private static readonly Color GoldTitleText  = new Color(0.98f, 0.87f, 0.46f, 1.00f);
        private static readonly Color MutedWoodText  = new Color(0.42f, 0.29f, 0.16f, 1.00f);

        /// <summary>
        /// Constrói o card completo de Configurações (Gráficos 4 níveis, Música, Ambiente/Efeitos,
        /// Sensibilidade digitável 0.1..10.0, Ver Tutorial e Voltar) dentro do container especificado.
        /// Retorna (SettingsPanelUI, backButton).
        /// </summary>
        public static (SettingsPanelUI settingsUI, Button backButton) BuildWoodSettingsCard(
            Transform parent, string cardName, string footerHintText)
        {
            Sprite spVoltar = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Jogo/UI/Sprites/btn_voltar.png");
            Sprite spBarraEscura = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Jogo/UI/Sprites/barra_madeira_escura.png");

            GameObject card = CreateRect(parent, cardName, Vector2.zero, new Vector2(980f, 700f));
            var cardImg = card.AddComponent<Image>();
            cardImg.color = WoodFrameBg;
            cardImg.raycastTarget = true;
            var cardOutline = card.AddComponent<Outline>();
            cardOutline.effectColor = WoodBorderGold;
            cardOutline.effectDistance = new Vector2(3f, -3f);

            var settingsUI = card.AddComponent<SettingsPanelUI>();

            // Cabeçalho amadeirado
            GameObject header = CreateRect(card.transform, "Header_Opcoes", new Vector2(0f, 300f), new Vector2(980f, 96f));
            var headerImg = header.AddComponent<Image>();
            headerImg.color = WoodHeaderBg;
            headerImg.raycastTarget = false;

            GameObject trim = CreateRect(card.transform, "Friso_Dourado", new Vector2(0f, 251f), new Vector2(980f, 4f));
            var trimImg = trim.AddComponent<Image>();
            trimImg.color = WoodBorderGold;
            trimImg.raycastTarget = false;

            CreateText(header.transform, "Titulo_Opcoes", "CONFIGURAÇÕES", 40f, FontStyles.Bold, GoldTitleText, Vector2.zero, new Vector2(760f, 60f), TextAlignmentOptions.Center);

            // Miolo bege claro (identidade visual amadeirada/bege)
            GameObject bodyCard = CreateRect(card.transform, "Corpo_Bege_Opcoes", new Vector2(0f, 28f), new Vector2(916f, 410f));
            var bodyImg = bodyCard.AddComponent<Image>();
            bodyImg.color = BeigeInnerCard;
            bodyImg.raycastTarget = false;
            var bodyOutline = bodyCard.AddComponent<Outline>();
            bodyOutline.effectColor = new Color(0.55f, 0.39f, 0.22f, 0.85f);
            bodyOutline.effectDistance = new Vector2(2f, -2f);

            // =========================================================================
            // 1. QUALIDADE GRÁFICA (Dropdown 4 níveis: Baixa, Média, Alta, Muito Alta)
            // =========================================================================
            CreateText(bodyCard.transform, "Label_Graficos", "Qualidade Gráfica", 26f, FontStyles.Bold, DarkWoodText, new Vector2(-235f, 150f), new Vector2(360f, 40f), TextAlignmentOptions.Left);
            TMP_Dropdown qualityDropdown = CreateWoodDropdown(bodyCard.transform, "Dropdown_Qualidade", new Vector2(180f, 150f), new Vector2(400f, 48f));
            TMP_Text qualityHint = CreateText(bodyCard.transform, "Hint_Qualidade", "Escala 125% | MSAA 4x | Sombras 4096 Soft (250m) | SSAO Alto + DoF na Mira", 15f, FontStyles.Italic, MutedWoodText, new Vector2(0f, 108f), new Vector2(850f, 26f), TextAlignmentOptions.Center);

            CreateDivider(bodyCard.transform, new Vector2(0f, 84f), 850f);

            // =========================================================================
            // 2. VOLUME DA MÚSICA (Slider + Porcentagem)
            // =========================================================================
            CreateText(bodyCard.transform, "Label_Musica", "Volume da Música", 25f, FontStyles.Bold, DarkWoodText, new Vector2(-235f, 42f), new Vector2(360f, 40f), TextAlignmentOptions.Left);
            Slider musicSlider = CreateWoodSlider(bodyCard.transform, "Slider_Musica", new Vector2(130f, 42f), new Vector2(310f, 30f), 0.75f);
            TMP_Text musicPct = CreateText(bodyCard.transform, "Pct_Musica", "75%", 24f, FontStyles.Bold, DarkWoodText, new Vector2(355f, 42f), new Vector2(95f, 40f), TextAlignmentOptions.Right);

            // =========================================================================
            // 3. VOLUME AMBIENTE / EFEITOS (Slider + Porcentagem)
            // =========================================================================
            CreateText(bodyCard.transform, "Label_SFX", "Ambiente / Efeitos", 25f, FontStyles.Bold, DarkWoodText, new Vector2(-235f, -30f), new Vector2(360f, 40f), TextAlignmentOptions.Left);
            Slider sfxSlider = CreateWoodSlider(bodyCard.transform, "Slider_SFX", new Vector2(130f, -30f), new Vector2(310f, 30f), 0.85f);
            TMP_Text sfxPct = CreateText(bodyCard.transform, "Pct_SFX", "85%", 24f, FontStyles.Bold, DarkWoodText, new Vector2(355f, -30f), new Vector2(95f, 40f), TextAlignmentOptions.Right);

            CreateDivider(bodyCard.transform, new Vector2(0f, -72f), 850f);

            // =========================================================================
            // 4. SENSIBILIDADE DO MOUSE (TMP_InputField DecimalNumber 0.1..10.0)
            // =========================================================================
            CreateText(bodyCard.transform, "Label_Sensibilidade", "Sensibilidade do Mouse", 25f, FontStyles.Bold, DarkWoodText, new Vector2(-235f, -124f), new Vector2(380f, 40f), TextAlignmentOptions.Left);
            TMP_InputField sensInput = CreateWoodNumericInput(bodyCard.transform, "Input_Sensibilidade", new Vector2(85f, -124f), new Vector2(165f, 48f), "1.0");
            TMP_Text sensHint = CreateText(bodyCard.transform, "Hint_Sensibilidade", "Valor de 0.1 a 10.0 (padrao: 1.0)", 17f, FontStyles.Italic, MutedWoodText, new Vector2(298f, -124f), new Vector2(245f, 44f), TextAlignmentOptions.Left);

            // Feedback + Dica inferior
            TMP_Text tutFeedback = CreateText(card.transform, "Feedback_Tutorial", "", 18f, FontStyles.Bold, GoldTitleText, new Vector2(0f, -200f), new Vector2(860f, 28f), TextAlignmentOptions.Center);
            CreateText(card.transform, "Dica_ESC", footerHintText, 17f, FontStyles.Italic, new Color(0.90f, 0.84f, 0.72f, 0.90f), new Vector2(0f, -228f), new Vector2(860f, 26f), TextAlignmentOptions.Center);

            // =========================================================================
            // 5. RODAPÉ: BOTÃO "VER TUTORIAL" + BOTÃO "VOLTAR"
            // =========================================================================
            GameObject btnTutObj = CreateRect(card.transform, "Btn_VerTutorial", new Vector2(-210f, -290f), new Vector2(280f, 74f));
            var tutImg = btnTutObj.AddComponent<Image>();
            if (spBarraEscura != null) { tutImg.sprite = spBarraEscura; tutImg.color = Color.white; }
            else tutImg.color = WoodHeaderBg;
            var tutOutline = btnTutObj.AddComponent<Outline>();
            tutOutline.effectColor = WoodBorderGold;
            tutOutline.effectDistance = new Vector2(2f, -2f);
            Button btnTut = btnTutObj.AddComponent<Button>();
            CreateText(btnTutObj.transform, "Texto", "VER TUTORIAL", 22f, FontStyles.Bold, CreamLightText, Vector2.zero, new Vector2(250f, 50f), TextAlignmentOptions.Center);

            GameObject btnBackObj = CreateRect(card.transform, "Btn_Voltar_Opcoes", new Vector2(185f, -290f), new Vector2(280f, 88f));
            var backImg = btnBackObj.AddComponent<Image>();
            if (spVoltar != null)
            {
                backImg.sprite = spVoltar;
                backImg.color = Color.white;
                backImg.preserveAspect = true;
            }
            else
            {
                backImg.color = new Color(0.55f, 0.36f, 0.18f, 1f);
                CreateText(btnBackObj.transform, "Texto", "VOLTAR", 26f, FontStyles.Bold, CreamLightText, Vector2.zero, new Vector2(240f, 54f), TextAlignmentOptions.Center);
            }
            Button btnBack = btnBackObj.AddComponent<Button>();

            // Mantém o Dropdown no topo da hierarquia do corpo para que a lista expandida nunca fique atrás dos sliders!
            qualityDropdown.transform.SetAsLastSibling();

            // Vincula todas as referências serializadas em SettingsPanelUI
            var so = new SerializedObject(settingsUI);
            so.FindProperty("qualityDropdown").objectReferenceValue = qualityDropdown;
            so.FindProperty("qualityHintText").objectReferenceValue = qualityHint;
            so.FindProperty("musicVolumeSlider").objectReferenceValue = musicSlider;
            so.FindProperty("musicPercentText").objectReferenceValue = musicPct;
            so.FindProperty("sfxVolumeSlider").objectReferenceValue = sfxSlider;
            so.FindProperty("sfxPercentText").objectReferenceValue = sfxPct;
            so.FindProperty("sensitivityInputField").objectReferenceValue = sensInput;
            so.FindProperty("sensitivityHintText").objectReferenceValue = sensHint;
            so.FindProperty("showTutorialButton").objectReferenceValue = btnTut;
            so.FindProperty("tutorialFeedbackText").objectReferenceValue = tutFeedback;
            so.ApplyModifiedPropertiesWithoutUndo();

            return (settingsUI, btnBack);
        }

        /// <summary>
        /// Constrói o painel flutuante de Tutorial Passo a Passo exibido na primeira entrada na SampleScene
        /// (com botão 'X' visível no canto superior direito, 3 blocos visuais claros e botão 'Começar Expedição').
        /// </summary>
        public static TutorialOverlayUI BuildFirstTimeTutorialOverlay(Transform canvas, PlayerController playerController)
        {
            Transform existing = canvas.Find("Modal_Tutorial");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            GameObject modalRoot = new GameObject("Modal_Tutorial", typeof(RectTransform));
            modalRoot.transform.SetParent(canvas, false);
            SetFullStretch(modalRoot.GetComponent<RectTransform>());
            var tutorialUI = modalRoot.AddComponent<TutorialOverlayUI>();

            // Fundo escurecido semitransparente cobrindo a tela
            GameObject windowObj = new GameObject("Janela_Tutorial", typeof(RectTransform));
            windowObj.transform.SetParent(modalRoot.transform, false);
            SetFullStretch(windowObj.GetComponent<RectTransform>());
            var scrimImg = windowObj.AddComponent<Image>();
            scrimImg.color = new Color(0.05f, 0.04f, 0.02f, 0.62f);
            scrimImg.raycastTarget = true;

            // Card flutuante amadeirado/bege central
            GameObject card = CreateRect(windowObj.transform, "Card_Tutorial", Vector2.zero, new Vector2(1020f, 670f));
            var cardImg = card.AddComponent<Image>();
            cardImg.color = new Color(0.22f, 0.14f, 0.08f, 0.94f);
            var cardOutline = card.AddComponent<Outline>();
            cardOutline.effectColor = WoodBorderGold;
            cardOutline.effectDistance = new Vector2(3f, -3f);

            // Barra de topo
            GameObject header = CreateRect(card.transform, "Header_Tutorial", new Vector2(0f, 286f), new Vector2(1020f, 96f));
            var headerImg = header.AddComponent<Image>();
            headerImg.color = WoodHeaderBg;
            headerImg.raycastTarget = false;

            GameObject trim = CreateRect(card.transform, "Friso_Tutorial", new Vector2(0f, 237f), new Vector2(1020f, 4f));
            var trimImg = trim.AddComponent<Image>();
            trimImg.color = WoodBorderGold;
            trimImg.raycastTarget = false;

            CreateText(header.transform, "Titulo_Tutorial", "GUIA RÁPIDO DA EXPEDIÇÃO — CERRADO", 33f, FontStyles.Bold, GoldTitleText, new Vector2(-28f, 8f), new Vector2(860f, 48f), TextAlignmentOptions.Center);
            CreateText(header.transform, "Subtitulo_Tutorial", "Aprenda os controles essenciais para explorar e fotografar a biodiversidade", 17f, FontStyles.Italic, CreamLightText, new Vector2(-28f, -26f), new Vector2(860f, 26f), TextAlignmentOptions.Center);

            // BOTÃO 'X' BEM VISÍVEL NO CANTO SUPERIOR DIREITO
            GameObject btnXObj = CreateRect(card.transform, "Btn_Fechar_X", new Vector2(468f, 290f), new Vector2(58f, 58f));
            var btnXImg = btnXObj.AddComponent<Image>();
            btnXImg.color = new Color(0.74f, 0.22f, 0.16f, 1f);
            var btnXOut = btnXObj.AddComponent<Outline>();
            btnXOut.effectColor = CreamLightText;
            btnXOut.effectDistance = new Vector2(2f, -2f);
            Button btnCloseX = btnXObj.AddComponent<Button>();
            CreateText(btnXObj.transform, "Texto_X", "X", 30f, FontStyles.Bold, Color.white, Vector2.zero, new Vector2(54f, 54f), TextAlignmentOptions.Center);

            // 3 Cartões de Passo a Passo em tom bege claro com teclas em destaque
            CreateTutorialStepCard(
                card.transform, "Passo_1_Explorar", new Vector2(0f, 134f),
                "1", "EXPLORAR E FURTIVIDADE",
                "Use <b>[W, A, S, D]</b> para caminhar pelo Cerrado, segure <b>[SHIFT]</b> para correr e aperte <b>[CTRL]</b> para <b>agachar (modo furtivo)</b> e se aproximar sem assustar animais ariscos."
            );

            CreateTutorialStepCard(
                card.transform, "Passo_2_Fotografar", new Vector2(0f, -6f),
                "2", "FOTOGRAFAR ESPÉCIES",
                "Segure o <b>[Botão Direito / RMB]</b> para abrir o visor da câmera, gire o <b>[Scroll do Mouse]</b> para ajustar o zoom óptico e clique com o <b>[Botão Esquerdo / LMB]</b> para fotografar fauna e flora."
            );

            CreateTutorialStepCard(
                card.transform, "Passo_3_Album", new Vector2(0f, -146f),
                "3", "ÁLBUM, MISSÕES E BIÓLOGO GUIA",
                "Pressione <b>[TAB]</b> ou <b>[J]</b> para abrir o <b>Álbum do Cerrado</b> e acompanhar sua coleção. Aperte <b>[E]</b> perto do Biólogo Guia (Prof. Silva) para receber missões e abrir a loja de lentes."
            );

            // Botão "COMEÇAR EXPEDIÇÃO" + Dica
            GameObject btnStartObj = CreateRect(card.transform, "Btn_Comecar_Expedicao", new Vector2(0f, -264f), new Vector2(360f, 68f));
            var btnStartImg = btnStartObj.AddComponent<Image>();
            btnStartImg.color = new Color(0.24f, 0.50f, 0.22f, 1f);
            var btnStartOut = btnStartObj.AddComponent<Outline>();
            btnStartOut.effectColor = WoodBorderGold;
            btnStartOut.effectDistance = new Vector2(2f, -2f);
            Button btnStart = btnStartObj.AddComponent<Button>();
            CreateText(btnStartObj.transform, "Texto", "COMEÇAR EXPEDIÇÃO", 24f, FontStyles.Bold, CreamLightText, Vector2.zero, new Vector2(340f, 52f), TextAlignmentOptions.Center);

            CreateText(card.transform, "Hint_Fechar", "Clique no [X] ou pressione [ESC] para fechar — reabra quando quiser no menu Opções", 15f, FontStyles.Italic, new Color(0.90f, 0.84f, 0.72f, 0.88f), new Vector2(0f, -314f), new Vector2(920f, 24f), TextAlignmentOptions.Center);

            windowObj.SetActive(false);

            var soTut = new SerializedObject(tutorialUI);
            soTut.FindProperty("tutorialPanel").objectReferenceValue = windowObj;
            soTut.FindProperty("playerController").objectReferenceValue = playerController;
            soTut.FindProperty("closeXButton").objectReferenceValue = btnCloseX;
            soTut.FindProperty("startExpeditionButton").objectReferenceValue = btnStart;
            soTut.ApplyModifiedPropertiesWithoutUndo();

            return tutorialUI;
        }

        private static void CreateTutorialStepCard(Transform parent, string name, Vector2 pos, string stepNum, string title, string body)
        {
            GameObject row = CreateRect(parent, name, pos, new Vector2(944f, 122f));
            var bg = row.AddComponent<Image>();
            bg.color = BeigeInnerCard;
            bg.raycastTarget = false;
            var outline = row.AddComponent<Outline>();
            outline.effectColor = new Color(0.55f, 0.39f, 0.22f, 0.85f);
            outline.effectDistance = new Vector2(2f, -2f);

            // Badge numérico à esquerda
            GameObject badge = CreateRect(row.transform, "Badge_Num", new Vector2(-416f, 0f), new Vector2(68f, 68f));
            var badgeImg = badge.AddComponent<Image>();
            badgeImg.color = WoodHeaderBg;
            badgeImg.raycastTarget = false;
            var badgeOut = badge.AddComponent<Outline>();
            badgeOut.effectColor = WoodBorderGold;
            badgeOut.effectDistance = new Vector2(2f, -2f);
            CreateText(badge.transform, "Num", stepNum, 32f, FontStyles.Bold, GoldTitleText, Vector2.zero, new Vector2(64f, 64f), TextAlignmentOptions.Center);

            // Título e descrição
            CreateText(row.transform, "Titulo", title, 22f, FontStyles.Bold, new Color(0.32f, 0.19f, 0.08f, 1f), new Vector2(40f, 34f), new Vector2(800f, 32f), TextAlignmentOptions.Left);
            TMP_Text bodyTmp = CreateText(row.transform, "Descricao", body, 18f, FontStyles.Normal, DarkWoodText, new Vector2(40f, -14f), new Vector2(800f, 66f), TextAlignmentOptions.Left);
            bodyTmp.textWrappingMode = TextWrappingModes.Normal;
        }

        private static TMP_Dropdown CreateWoodDropdown(Transform parent, string name, Vector2 pos, Vector2 size)
        {
            GameObject ddObj = CreateRect(parent, name, pos, size);
            var ddBg = ddObj.AddComponent<Image>();
            ddBg.color = WoodHeaderBg;
            var ddOut = ddObj.AddComponent<Outline>();
            ddOut.effectColor = WoodBorderGold;
            ddOut.effectDistance = new Vector2(2f, -2f);

            var dropdown = ddObj.AddComponent<TMP_Dropdown>();
            dropdown.targetGraphic = ddBg;

            // Texto selecionado
            TMP_Text captionTxt = CreateText(ddObj.transform, "Label", "Muito Alta", 22f, FontStyles.Bold, CreamLightText, new Vector2(-18f, 0f), new Vector2(size.x - 56f, size.y - 8f), TextAlignmentOptions.Left);
            CreateText(ddObj.transform, "Arrow", "v", 20f, FontStyles.Bold, GoldTitleText, new Vector2(size.x * 0.5f - 24f, 0f), new Vector2(32f, 32f), TextAlignmentOptions.Center);

            // Template do menu suspenso
            GameObject templateObj = CreateRect(ddObj.transform, "Template", new Vector2(0f, -2f), new Vector2(0f, 184f));
            var tRect = templateObj.GetComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0f, 0f);
            tRect.anchorMax = new Vector2(1f, 0f);
            tRect.pivot = new Vector2(0.5f, 1f);
            tRect.anchoredPosition = new Vector2(0f, -2f);
            tRect.sizeDelta = new Vector2(0f, 184f);

            var tImg = templateObj.AddComponent<Image>();
            tImg.color = new Color(0.20f, 0.13f, 0.07f, 0.99f);
            var tOut = templateObj.AddComponent<Outline>();
            tOut.effectColor = WoodBorderGold;
            tOut.effectDistance = new Vector2(2f, -2f);

            var scrollRect = templateObj.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            GameObject viewportObj = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewportObj.transform.SetParent(templateObj.transform, false);
            SetFullStretch(viewportObj.GetComponent<RectTransform>());
            viewportObj.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);
            viewportObj.GetComponent<Mask>().showMaskGraphic = false;

            GameObject contentObj = new GameObject("Content", typeof(RectTransform));
            contentObj.transform.SetParent(viewportObj.transform, false);
            var cRect = contentObj.GetComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0f, 1f);
            cRect.anchorMax = new Vector2(1f, 1f);
            cRect.pivot = new Vector2(0.5f, 1f);
            cRect.anchoredPosition = Vector2.zero;
            cRect.sizeDelta = new Vector2(0f, 44f);

            scrollRect.viewport = viewportObj.GetComponent<RectTransform>();
            scrollRect.content = cRect;

            GameObject itemObj = new GameObject("Item", typeof(RectTransform), typeof(Toggle));
            itemObj.transform.SetParent(contentObj.transform, false);
            var itemRect = itemObj.GetComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0f, 0.5f);
            itemRect.anchorMax = new Vector2(1f, 0.5f);
            itemRect.sizeDelta = new Vector2(0f, 44f);

            GameObject itemBg = new GameObject("Item Background", typeof(RectTransform), typeof(Image));
            itemBg.transform.SetParent(itemObj.transform, false);
            SetFullStretch(itemBg.GetComponent<RectTransform>());
            itemBg.GetComponent<Image>().color = new Color(0.26f, 0.17f, 0.09f, 1f);

            GameObject itemCheck = new GameObject("Item Checkmark", typeof(RectTransform), typeof(Image));
            itemCheck.transform.SetParent(itemObj.transform, false);
            var chkRect = itemCheck.GetComponent<RectTransform>();
            chkRect.anchorMin = new Vector2(0f, 0.5f);
            chkRect.anchorMax = new Vector2(0f, 0.5f);
            chkRect.anchoredPosition = new Vector2(18f, 0f);
            chkRect.sizeDelta = new Vector2(12f, 12f);
            itemCheck.GetComponent<Image>().color = GoldTitleText;

            TMP_Text itemLabel = CreateText(itemObj.transform, "Item Label", "Opcao", 21f, FontStyles.Bold, CreamLightText, new Vector2(14f, 0f), new Vector2(-36f, 0f), TextAlignmentOptions.Left);
            var ilRect = itemLabel.GetComponent<RectTransform>();
            ilRect.anchorMin = Vector2.zero;
            ilRect.anchorMax = Vector2.one;
            ilRect.offsetMin = new Vector2(36f, 2f);
            ilRect.offsetMax = new Vector2(-10f, -2f);

            var toggle = itemObj.GetComponent<Toggle>();
            toggle.targetGraphic = itemBg.GetComponent<Image>();
            toggle.graphic = itemCheck.GetComponent<Image>();
            toggle.isOn = true;

            templateObj.SetActive(false);

            dropdown.template = tRect;
            dropdown.captionText = captionTxt;
            dropdown.itemText = itemLabel;
            dropdown.ClearOptions();
            dropdown.AddOptions(new List<string>(GameSettingsManager.QualityLevelNames));
            dropdown.value = 3;
            dropdown.RefreshShownValue();

            return dropdown;
        }

        private static Slider CreateWoodSlider(Transform parent, string name, Vector2 pos, Vector2 size, float defaultVal)
        {
            GameObject root = CreateRect(parent, name, pos, size);
            var slider = root.AddComponent<Slider>();

            GameObject bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(root.transform, false);
            SetFullStretch(bg.GetComponent<RectTransform>());
            bg.GetComponent<Image>().color = new Color(0.32f, 0.21f, 0.12f, 0.92f);

            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(root.transform, false);
            SetFullStretch(fillArea.GetComponent<RectTransform>());

            GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            SetFullStretch(fill.GetComponent<RectTransform>());
            fill.GetComponent<Image>().color = new Color(0.84f, 0.64f, 0.22f, 1f);

            GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(root.transform, false);
            SetFullStretch(handleArea.GetComponent<RectTransform>());

            GameObject handle = CreateRect(handleArea.transform, "Handle", Vector2.zero, new Vector2(22f, size.y + 8f));
            var hImg = handle.AddComponent<Image>();
            hImg.color = CreamLightText;
            var hOut = handle.AddComponent<Outline>();
            hOut.effectColor = WoodHeaderBg;
            hOut.effectDistance = new Vector2(2f, -2f);

            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.handleRect = handle.GetComponent<RectTransform>();
            slider.targetGraphic = hImg;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = defaultVal;
            return slider;
        }

        private static TMP_InputField CreateWoodNumericInput(Transform parent, string name, Vector2 pos, Vector2 size, string defaultValue)
        {
            GameObject inputObj = CreateRect(parent, name, pos, size);
            var bg = inputObj.AddComponent<Image>();
            bg.color = BeigeFieldBg;
            var outline = inputObj.AddComponent<Outline>();
            outline.effectColor = WoodHeaderBg;
            outline.effectDistance = new Vector2(2f, -2f);

            var inputField = inputObj.AddComponent<TMP_InputField>();
            inputField.targetGraphic = bg;
            inputField.contentType = TMP_InputField.ContentType.DecimalNumber;
            inputField.characterLimit = 5;

            GameObject textArea = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
            textArea.transform.SetParent(inputObj.transform, false);
            var taRect = textArea.GetComponent<RectTransform>();
            taRect.anchorMin = Vector2.zero;
            taRect.anchorMax = Vector2.one;
            taRect.offsetMin = new Vector2(12f, 4f);
            taRect.offsetMax = new Vector2(-12f, -4f);

            GameObject placeholderObj = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
            placeholderObj.transform.SetParent(textArea.transform, false);
            SetFullStretch(placeholderObj.GetComponent<RectTransform>());
            var phTmp = placeholderObj.GetComponent<TextMeshProUGUI>();
            phTmp.text = "1.0";
            phTmp.fontSize = 24f;
            phTmp.fontStyle = FontStyles.Italic;
            phTmp.color = new Color(0.50f, 0.40f, 0.30f, 0.65f);
            phTmp.alignment = TextAlignmentOptions.Center;

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(textArea.transform, false);
            SetFullStretch(textObj.GetComponent<RectTransform>());
            var txtTmp = textObj.GetComponent<TextMeshProUGUI>();
            txtTmp.text = defaultValue;
            txtTmp.fontSize = 25f;
            txtTmp.fontStyle = FontStyles.Bold;
            txtTmp.color = DarkWoodText;
            txtTmp.alignment = TextAlignmentOptions.Center;

            inputField.textViewport = taRect;
            inputField.textComponent = txtTmp;
            inputField.placeholder = phTmp;
            inputField.text = defaultValue;
            inputField.caretColor = DarkWoodText;
            inputField.customCaretColor = true;
            inputField.selectionColor = new Color(0.84f, 0.68f, 0.42f, 0.45f);

            return inputField;
        }

        private static void CreateDivider(Transform parent, Vector2 pos, float width)
        {
            GameObject div = CreateRect(parent, "Divisoria", pos, new Vector2(width, 2f));
            var img = div.AddComponent<Image>();
            img.color = new Color(0.62f, 0.48f, 0.30f, 0.45f);
            img.raycastTarget = false;
        }

        private static GameObject CreateRect(Transform parent, string name, Vector2 pos, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return go;
        }

        private static TMP_Text CreateText(
            Transform parent, string name, string text, float fontSize, FontStyles style,
            Color color, Vector2 pos, Vector2 size, TextAlignmentOptions align)
        {
            GameObject go = CreateRect(parent, name, pos, size);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static void SetFullStretch(RectTransform rect)
        {
            if (rect == null) return;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
