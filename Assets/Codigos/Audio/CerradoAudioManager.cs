using System.Collections.Generic;
using UnityEngine;
using Cerrado.Settings;

namespace Cerrado.Audio
{
    /// <summary>
    /// Gerencia trilha sonora regional (Sons do Cerrado) e efeitos sonoros/ambiente em canais separados,
    /// respeitando continuamente os sliders independentes de Música e Ambiente/Efeitos (GameSettingsManager).
    /// </summary>
    public class CerradoAudioManager : MonoBehaviour
    {
        public static CerradoAudioManager Instance { get; private set; }

        public const string VolumePrefKey = "Cerrado_Volume";

        [Header("Trilha Ambiente Regional (Sons do Cerrado)")]
        [SerializeField] private List<AudioClip> ambientTracks = new List<AudioClip>();
        [SerializeField, Range(0f, 1f)] private float ambientVolume = 0.28f;

        [Header("Passos na Terra")]
        [SerializeField] private List<AudioClip> dirtFootsteps = new List<AudioClip>();
        [SerializeField, Range(0f, 1f)] private float footstepBaseVolume = 0.48f;

        [Header("Interface (Álbum / Caderno de Campo)")]
        [SerializeField] private List<AudioClip> pageFlipClips = new List<AudioClip>();
        [SerializeField, Range(0f, 1f)] private float uiVolume = 0.58f;

        private AudioSource musicSource;
        private AudioSource sfxSource;
        private int currentTrackIndex = -1;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            GameSettingsManager.EnsureLoaded();

            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = false;
            musicSource.playOnAwake = false;
            musicSource.spatialBlend = 0f;
            musicSource.ignoreListenerPause = true;

            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f;

            ApplyChannelVolumes();
        }

        private void OnEnable()
        {
            GameSettingsManager.OnSettingsChanged += ApplyChannelVolumes;
        }

        private void OnDisable()
        {
            GameSettingsManager.OnSettingsChanged -= ApplyChannelVolumes;
        }

        private void Start()
        {
            ApplyChannelVolumes();
            PlayNextAmbientTrack();
        }

        private void Update()
        {
            if (ambientTracks != null && ambientTracks.Count > 0 && musicSource != null && !musicSource.isPlaying && musicSource.time <= 0.01f)
            {
                PlayNextAmbientTrack();
            }
        }

        public void ApplyChannelVolumes()
        {
            GameSettingsManager.EnsureLoaded();
            AudioListener.volume = 1f;
            if (musicSource != null)
            {
                musicSource.volume = ambientVolume * GameSettingsManager.MusicVolume;
            }
            if (sfxSource != null)
            {
                sfxSource.volume = 1f;
            }
        }

        public static void ApplySavedVolume()
        {
            GameSettingsManager.EnsureLoaded();
            AudioListener.volume = 1f;
            if (Instance != null) Instance.ApplyChannelVolumes();
        }

        public static void SetMasterVolume(float volume)
        {
            GameSettingsManager.SetMusicVolume(volume);
            GameSettingsManager.SetSfxVolume(volume);
        }

        public void PlayNextAmbientTrack()
        {
            if (ambientTracks == null || ambientTracks.Count == 0 || musicSource == null) return;

            if (ambientTracks.Count == 1)
            {
                currentTrackIndex = 0;
            }
            else
            {
                int next = Random.Range(0, ambientTracks.Count);
                if (next == currentTrackIndex) next = (next + 1) % ambientTracks.Count;
                currentTrackIndex = next;
            }

            AudioClip clip = ambientTracks[currentTrackIndex];
            if (clip != null)
            {
                musicSource.clip = clip;
                musicSource.volume = ambientVolume * GameSettingsManager.MusicVolume;
                musicSource.Play();
            }
        }

        public void PlayDirtFootstep(bool isSprinting, bool isCrouching)
        {
            if (dirtFootsteps == null || dirtFootsteps.Count == 0 || sfxSource == null) return;

            AudioClip clip = dirtFootsteps[Random.Range(0, dirtFootsteps.Count)];
            if (clip == null) return;

            float scale = footstepBaseVolume;
            if (isCrouching) scale *= 0.35f;
            else if (isSprinting) scale *= 1.15f;
            else scale *= 0.78f;

            float finalVol = Mathf.Clamp01(scale * GameSettingsManager.SfxVolume);
            if (finalVol <= 0.001f) return;

            sfxSource.pitch = Random.Range(0.92f, 1.08f);
            sfxSource.PlayOneShot(clip, finalVol);
        }

        public void PlayPageFlip()
        {
            if (pageFlipClips == null || pageFlipClips.Count == 0 || sfxSource == null) return;

            AudioClip clip = pageFlipClips[Random.Range(0, pageFlipClips.Count)];
            if (clip == null) return;

            float finalVol = Mathf.Clamp01(uiVolume * GameSettingsManager.SfxVolume);
            if (finalVol <= 0.001f) return;

            sfxSource.pitch = Random.Range(0.96f, 1.05f);
            sfxSource.PlayOneShot(clip, finalVol);
        }
    }
}
