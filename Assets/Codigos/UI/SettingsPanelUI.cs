using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cerrado.Settings;

namespace Cerrado.UI
{
    /// <summary>
    /// Componente reutilizável da tela de Configurações (Gráficos, Volume da Música, Volume Ambiente/Efeitos,
    /// Sensibilidade do Mouse digitável via TMP_InputField e botão opcional "Ver Tutorial").
    /// Usado de forma idêntica no Menu Principal (menu_cena) e no Menu de Pausa (SampleScene).
    /// </summary>
    public class SettingsPanelUI : MonoBehaviour
    {
        [Header("Qualidade Gráfica")]
        [SerializeField] private TMP_Dropdown qualityDropdown;
        [SerializeField] private TMP_Text qualityHintText;

        [Header("Áudio Separado")]
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private TMP_Text musicPercentText;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private TMP_Text sfxPercentText;

        [Header("Sensibilidade do Mouse (Digitável 0.1 a 10.0)")]
        [SerializeField] private TMP_InputField sensitivityInputField;
        [SerializeField] private TMP_Text sensitivityHintText;

        [Header("Tutorial (Opcional)")]
        [SerializeField] private Button showTutorialButton;
        [SerializeField] private TMP_Text tutorialFeedbackText;

        private bool isRefreshingUI;
        public event Action OnRequestOpenTutorial;

        private void Awake()
        {
            GameSettingsManager.EnsureLoaded();
            SetupDropdownOptions();
            BindListeners();
            RefreshUIFromSettings();
        }

        private void OnEnable()
        {
            GameSettingsManager.EnsureLoaded();
            GameSettingsManager.OnSettingsChanged += RefreshUIFromSettings;
            RefreshUIFromSettings();
            if (tutorialFeedbackText != null) tutorialFeedbackText.text = "";
        }

        private void OnDisable()
        {
            GameSettingsManager.OnSettingsChanged -= RefreshUIFromSettings;
        }

        private void SetupDropdownOptions()
        {
            if (qualityDropdown == null) return;
            qualityDropdown.ClearOptions();
            qualityDropdown.AddOptions(new List<string>(GameSettingsManager.QualityLevelNames));
        }

        private void BindListeners()
        {
            if (qualityDropdown != null)
            {
                qualityDropdown.onValueChanged.RemoveAllListeners();
                qualityDropdown.onValueChanged.AddListener(HandleQualityChanged);
            }

            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.minValue = 0f;
                musicVolumeSlider.maxValue = 1f;
                musicVolumeSlider.onValueChanged.RemoveAllListeners();
                musicVolumeSlider.onValueChanged.AddListener(HandleMusicSliderChanged);
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.minValue = 0f;
                sfxVolumeSlider.maxValue = 1f;
                sfxVolumeSlider.onValueChanged.RemoveAllListeners();
                sfxVolumeSlider.onValueChanged.AddListener(HandleSfxSliderChanged);
            }

            if (sensitivityInputField != null)
            {
                sensitivityInputField.contentType = TMP_InputField.ContentType.DecimalNumber;
                sensitivityInputField.characterLimit = 5;
                sensitivityInputField.onEndEdit.RemoveAllListeners();
                sensitivityInputField.onEndEdit.AddListener(HandleSensitivityEndEdit);
            }

            if (showTutorialButton != null)
            {
                showTutorialButton.onClick.RemoveAllListeners();
                showTutorialButton.onClick.AddListener(HandleShowTutorialClicked);
            }
        }

        public void RefreshUIFromSettings()
        {
            isRefreshingUI = true;
            try
            {
                if (qualityDropdown != null)
                {
                    int idx = Mathf.Clamp(GameSettingsManager.QualityLevel, 0, Mathf.Max(0, qualityDropdown.options.Count - 1));
                    qualityDropdown.SetValueWithoutNotify(idx);
                    qualityDropdown.RefreshShownValue();
                }

                UpdateQualityHint(GameSettingsManager.QualityLevel);

                if (musicVolumeSlider != null)
                    musicVolumeSlider.SetValueWithoutNotify(GameSettingsManager.MusicVolume);
                if (musicPercentText != null)
                    musicPercentText.text = $"{Mathf.RoundToInt(GameSettingsManager.MusicVolume * 100f)}%";

                if (sfxVolumeSlider != null)
                    sfxVolumeSlider.SetValueWithoutNotify(GameSettingsManager.SfxVolume);
                if (sfxPercentText != null)
                    sfxPercentText.text = $"{Mathf.RoundToInt(GameSettingsManager.SfxVolume * 100f)}%";

                if (sensitivityInputField != null && !sensitivityInputField.isFocused)
                {
                    sensitivityInputField.SetTextWithoutNotify(GameSettingsManager.MouseSensitivity.ToString("0.0", CultureInfo.InvariantCulture));
                }

                if (sensitivityHintText != null)
                {
                    sensitivityHintText.text = "Valor de 0.1 a 10.0 (padrao: 1.0)";
                }
            }
            finally
            {
                isRefreshingUI = false;
            }
        }

        private void HandleQualityChanged(int index)
        {
            if (isRefreshingUI) return;
            GameSettingsManager.SetQualityLevel(index);
            UpdateQualityHint(index);
        }

        private void UpdateQualityHint(int level)
        {
            if (qualityHintText == null) return;
            qualityHintText.text = level switch
            {
                0 => "Escala 80% | Sombras 1024 (40m) | SSAO Desligado | Desempenho Maximo",
                1 => "Escala 100% | Sombras 2048 (80m) | SSAO Leve | Bloom + ACES",
                2 => "Escala 100% | MSAA 2x | Sombras 4096 (150m) | SSAO Medio + Vignette",
                _ => "Escala 125% | MSAA 4x | Sombras 4096 Soft (250m) | SSAO Alto + DoF na Mira"
            };
        }

        private void HandleMusicSliderChanged(float val)
        {
            if (isRefreshingUI) return;
            GameSettingsManager.SetMusicVolume(val);
            if (musicPercentText != null)
                musicPercentText.text = $"{Mathf.RoundToInt(val * 100f)}%";
        }

        private void HandleSfxSliderChanged(float val)
        {
            if (isRefreshingUI) return;
            GameSettingsManager.SetSfxVolume(val);
            if (sfxPercentText != null)
                sfxPercentText.text = $"{Mathf.RoundToInt(val * 100f)}%";
        }

        private void HandleSensitivityEndEdit(string rawText)
        {
            if (isRefreshingUI) return;

            string normalized = (rawText ?? "").Trim().Replace(',', '.');
            if (float.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
            {
                float clamped = Mathf.Clamp(parsed, 0.1f, 10.0f);
                GameSettingsManager.SetMouseSensitivity(clamped);
                if (sensitivityInputField != null)
                {
                    sensitivityInputField.SetTextWithoutNotify(clamped.ToString("0.0", CultureInfo.InvariantCulture));
                }
            }
            else
            {
                // Restaura o valor válido anterior se o texto estiver vazio ou inválido
                if (sensitivityInputField != null)
                {
                    sensitivityInputField.SetTextWithoutNotify(GameSettingsManager.MouseSensitivity.ToString("0.0", CultureInfo.InvariantCulture));
                }
            }
        }

        private void HandleShowTutorialClicked()
        {
            GameSettingsManager.SetTutorialSeen(false);

            if (OnRequestOpenTutorial != null)
            {
                OnRequestOpenTutorial.Invoke();
                return;
            }

            var tut = FindAnyObjectByType<TutorialOverlayUI>(FindObjectsInactive.Include);
            if (tut != null)
            {
                tut.ShowTutorial(true);
            }
            else if (tutorialFeedbackText != null)
            {
                tutorialFeedbackText.text = "Tutorial reativado! Ele aparecera ao iniciar o jogo.";
            }
        }
    }
}
