using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Cerrado.Photography;

namespace Cerrado.Settings
{
    /// <summary>
    /// Gerenciador centralizado e persistente (PlayerPrefs) de Gráficos (4 perfis URP),
    /// Áudio separado (Música vs Ambiente/Efeitos), Sensibilidade do Mouse (0.1..10.0) e Tutorial.
    /// Sincroniza Menu Inicial, Menu de Pausa e efeitos dinâmicos (Depth of Field sutil na mira em Muito Alta).
    /// </summary>
    public class GameSettingsManager : MonoBehaviour
    {
        public const string QualityPrefKey = "Cerrado_QualityLevel";
        public const string MusicVolPrefKey = "Cerrado_MusicVolume";
        public const string SfxVolPrefKey = "Cerrado_SfxVolume";
        public const string MouseSensPrefKey = "Cerrado_MouseSensitivity";
        public const string TutorialSeenPrefKey = "TutorialVisto";

        public static readonly string[] QualityLevelNames = { "Baixa", "Média", "Alta", "Muito Alta" };

        public static event Action OnSettingsChanged;

        private static GameSettingsManager instance;
        private static bool initialized;

        public static int QualityLevel { get; private set; } = 3; // Padrão: Muito Alta (Showcase RTX 5070)
        public static float MusicVolume { get; private set; } = 0.75f;
        public static float SfxVolume { get; private set; } = 0.85f;
        public static float MouseSensitivity { get; private set; } = 1.0f;

        private bool isAimingCamera;
        private DepthOfField cachedDof;
        private Volume cachedGlobalVolume;
        private PhotoCameraSystem cachedPhotoCam;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            EnsureInstance();
            LoadAndApplyAll();
        }

        public static GameSettingsManager EnsureInstance()
        {
            if (instance != null) return instance;

            var existing = FindAnyObjectByType<GameSettingsManager>();
            if (existing != null)
            {
                instance = existing;
                return instance;
            }

            GameObject go = new GameObject("_GameSettingsManager");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<GameSettingsManager>();
            return instance;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
            PhotoCameraSystem.OnAimStateChanged += HandleAimStateChanged;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            PhotoCameraSystem.OnAimStateChanged -= HandleAimStateChanged;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            isAimingCamera = false;
            cachedDof = null;
            cachedGlobalVolume = null;
            cachedPhotoCam = null;
            ApplyQualityToCurrentScene();
        }

        private void HandleAimStateChanged(bool aiming)
        {
            isAimingCamera = aiming;
            UpdateAimDepthOfField();
        }

        private void Update()
        {
            if (isAimingCamera && QualityLevel >= 3)
            {
                UpdateAimDepthOfField();
            }
        }

        public static void LoadAndApplyAll()
        {
            EnsureInstance();

            int maxQuality = Mathf.Max(0, QualitySettings.names.Length - 1);
            int defaultQuality = Mathf.Min(3, maxQuality);
            QualityLevel = Mathf.Clamp(PlayerPrefs.GetInt(QualityPrefKey, defaultQuality), 0, maxQuality);

            // Migração suave caso o usuário já tivesse a chave antiga Cerrado_Volume
            float legacyVol = PlayerPrefs.GetFloat("Cerrado_Volume", 0.80f);
            MusicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicVolPrefKey, Mathf.Clamp01(legacyVol * 0.9f)));
            SfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxVolPrefKey, legacyVol));
            MouseSensitivity = Mathf.Clamp(PlayerPrefs.GetFloat(MouseSensPrefKey, 1.0f), 0.1f, 10.0f);

            initialized = true;
            ApplyQualityInternal(QualityLevel);
            AudioListener.volume = 1f;
            OnSettingsChanged?.Invoke();
        }

        public static void EnsureLoaded()
        {
            if (!initialized) LoadAndApplyAll();
        }

        public static void SetQualityLevel(int level)
        {
            EnsureLoaded();
            int maxQuality = Mathf.Max(0, QualitySettings.names.Length - 1);
            int clamped = Mathf.Clamp(level, 0, maxQuality);
            QualityLevel = clamped;
            PlayerPrefs.SetInt(QualityPrefKey, clamped);
            PlayerPrefs.Save();

            ApplyQualityInternal(clamped);
            OnSettingsChanged?.Invoke();
        }

        public static void SetMusicVolume(float volume)
        {
            EnsureLoaded();
            MusicVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(MusicVolPrefKey, MusicVolume);
            PlayerPrefs.Save();

            AudioListener.volume = 1f;
            OnSettingsChanged?.Invoke();
        }

        public static void SetSfxVolume(float volume)
        {
            EnsureLoaded();
            SfxVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(SfxVolPrefKey, SfxVolume);
            PlayerPrefs.Save();

            AudioListener.volume = 1f;
            OnSettingsChanged?.Invoke();
        }

        public static void SetMouseSensitivity(float sensitivity)
        {
            EnsureLoaded();
            MouseSensitivity = Mathf.Clamp(sensitivity, 0.1f, 10.0f);
            PlayerPrefs.SetFloat(MouseSensPrefKey, MouseSensitivity);
            PlayerPrefs.Save();

            OnSettingsChanged?.Invoke();
        }

        public static bool HasSeenTutorial()
        {
            return PlayerPrefs.GetInt(TutorialSeenPrefKey, 0) != 0;
        }

        public static void SetTutorialSeen(bool seen)
        {
            PlayerPrefs.SetInt(TutorialSeenPrefKey, seen ? 1 : 0);
            PlayerPrefs.Save();
        }

        private static void ApplyQualityInternal(int level)
        {
            if (QualitySettings.names != null && level >= 0 && level < QualitySettings.names.Length)
            {
                QualitySettings.SetQualityLevel(level, true);
            }

            // Texturas Full Res em Média/Alta/Muito Alta; Half Res em Baixa
            QualitySettings.globalTextureMipmapLimit = (level == 0) ? 1 : 0;
            QualitySettings.anisotropicFiltering = (level >= 2) ? AnisotropicFiltering.ForceEnable : AnisotropicFiltering.Enable;

            switch (level)
            {
                case 0: // Baixa
                    QualitySettings.lodBias = 0.75f;
                    QualitySettings.shadowDistance = 40f;
                    break;
                case 1: // Média
                    QualitySettings.lodBias = 1.05f;
                    QualitySettings.shadowDistance = 80f;
                    break;
                case 2: // Alta
                    QualitySettings.lodBias = 1.50f;
                    QualitySettings.shadowDistance = 150f;
                    break;
                default: // Muito Alta (Showcase RTX 5070)
                    QualitySettings.lodBias = 2.00f;
                    QualitySettings.shadowDistance = 250f;
                    break;
            }

            if (instance != null)
            {
                instance.ApplyQualityToCurrentScene();
            }
        }

        private void ApplyQualityToCurrentScene()
        {
            // Ajusta distância de renderização da câmera principal conforme o perfil
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.farClipPlane = QualityLevel switch
                {
                    0 => 180f,
                    1 => 260f,
                    2 => 380f,
                    _ => 520f
                };
            }

            // Ajusta Volume de pós-processamento da cena (Motion Blur SEMPRE desligado)
            if (cachedGlobalVolume == null)
            {
                var volumes = FindObjectsByType<Volume>(FindObjectsSortMode.None);
                foreach (var v in volumes)
                {
                    if (v != null && v.isGlobal && v.profile != null)
                    {
                        cachedGlobalVolume = v;
                        break;
                    }
                }
            }

            if (cachedGlobalVolume == null || cachedGlobalVolume.profile == null) return;
            VolumeProfile prof = cachedGlobalVolume.profile;

            if (prof.TryGet<MotionBlur>(out var mb))
            {
                mb.active = false;
                mb.intensity.overrideState = false;
                mb.intensity.value = 0f;
            }

            if (prof.TryGet<Tonemapping>(out var tone))
            {
                tone.active = true;
                tone.mode.overrideState = true;
                tone.mode.value = TonemappingMode.ACES;
            }

            if (prof.TryGet<Bloom>(out var bloom))
            {
                bloom.active = QualityLevel >= 1;
                bloom.threshold.overrideState = true;
                bloom.threshold.value = 0.95f;
                bloom.intensity.overrideState = true;
                bloom.intensity.value = QualityLevel switch
                {
                    1 => 0.22f,
                    2 => 0.30f,
                    3 => 0.34f,
                    _ => 0f
                };
            }

            if (prof.TryGet<Vignette>(out var vignette))
            {
                vignette.active = QualityLevel >= 2;
                vignette.intensity.overrideState = true;
                vignette.intensity.value = (QualityLevel >= 3) ? 0.22f : 0.18f;
            }

            if (!prof.TryGet<DepthOfField>(out cachedDof))
            {
                cachedDof = prof.Add<DepthOfField>(true);
            }
            if (cachedDof != null)
            {
                cachedDof.mode.overrideState = true;
                cachedDof.mode.value = DepthOfFieldMode.Bokeh;
                cachedDof.aperture.overrideState = true;
                cachedDof.aperture.value = 4.5f;
                cachedDof.focalLength.overrideState = true;
                cachedDof.focalLength.value = 42f;
                cachedDof.focusDistance.overrideState = true;
                cachedDof.focusDistance.value = 14f;
                cachedDof.active = false;
            }

            UpdateAimDepthOfField();
        }

        private void UpdateAimDepthOfField()
        {
            if (cachedDof == null) return;

            bool enableDof = (QualityLevel >= 3) && isAimingCamera;
            cachedDof.active = enableDof;
            if (!enableDof) return;

            if (cachedPhotoCam == null) cachedPhotoCam = FindAnyObjectByType<PhotoCameraSystem>();
            float targetFocusDist = 14f;

            if (cachedPhotoCam != null && cachedPhotoCam.CurrentFocusedTarget != null && Camera.main != null)
            {
                targetFocusDist = Vector3.Distance(Camera.main.transform.position, cachedPhotoCam.CurrentFocusedTarget.FocalPoint);
            }
            else if (Camera.main != null && Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out RaycastHit hit, 80f))
            {
                targetFocusDist = hit.distance;
            }

            cachedDof.focusDistance.value = Mathf.Lerp(cachedDof.focusDistance.value, Mathf.Clamp(targetFocusDist, 2.0f, 65f), Time.unscaledDeltaTime * 8f);
        }
    }
}
