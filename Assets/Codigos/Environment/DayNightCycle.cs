using System;
using UnityEngine;

namespace Cerrado.Environment
{
    public class DayNightCycle : MonoBehaviour
    {
        public static DayNightCycle Instance { get; private set; }

        [Header("Controle de Tempo")]
        [Tooltip("Duração de um dia completo (24h in-game) em minutos reais")]
        [SerializeField] private float dayDurationInMinutes = 12f;
        [Tooltip("Hora inicial do dia (0 a 24)")]
        [Range(0f, 24f)]
        [SerializeField] private float currentHour = 9f; // Começa às 9h da manhã
        [SerializeField] private bool timeProgressionEnabled = true;

        [Header("Iluminação Direcional (Sol / Lua)")]
        [SerializeField] private Light directionalLight;
        [SerializeField] private float maxSunIntensity = 1.3f;
        [SerializeField] private float minMoonIntensity = 0.2f;

        [Header("Gradiente de Cores do Céu do Cerrado")]
        [SerializeField] private Color dawnColor = new Color(1.0f, 0.75f, 0.5f);     // Alvorecer dourado
        [SerializeField] private Color dayColor = new Color(1.0f, 0.98f, 0.90f);     // Meio-dia límpido
        [SerializeField] private Color sunsetColor = new Color(1.0f, 0.45f, 0.2f);   // Pôr do sol alaranjado (estilo Zelda TOTK)
        [SerializeField] private Color nightColor = new Color(0.2f, 0.35f, 0.65f);   // Noite azulada com luar

        // Eventos
        public static event Action<float, bool> OnTimeChanged;
        public static event Action<bool> OnDayNightTransition;

        private bool wasNight;

        public bool IsNight => currentHour < 5.5f || currentHour > 18.5f;
        public float CurrentHour => currentHour;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (directionalLight == null)
            {
                directionalLight = RenderSettings.sun;
            }
        }

        private void Start()
        {
            wasNight = IsNight;
            UpdateLighting(true);
        }

        private void Update()
        {
            if (timeProgressionEnabled && dayDurationInMinutes > 0f)
            {
                // Avança o tempo
                float hoursPerSecond = 24f / (dayDurationInMinutes * 60f);
                currentHour += hoursPerSecond * Time.deltaTime;

                if (currentHour >= 24f)
                {
                    currentHour -= 24f;
                }

                UpdateLighting(false);
            }
        }

        private void UpdateLighting(bool forceUpdate)
        {
            // Rotação da luz (Sol nasce a leste a 6h e se põe a oeste a 18h)
            if (directionalLight != null)
            {
                float sunRotation = ((currentHour - 6f) / 24f) * 360f;
                directionalLight.transform.rotation = Quaternion.Euler(new Vector3(sunRotation, 50f, 0f));

                // Cor e intensidade
                Color targetColor;
                float targetIntensity;

                if (currentHour >= 5f && currentHour < 7f)
                {
                    // Alvorecer
                    float t = (currentHour - 5f) / 2f;
                    targetColor = Color.Lerp(nightColor, dawnColor, t);
                    targetIntensity = Mathf.Lerp(minMoonIntensity, maxSunIntensity * 0.8f, t);
                }
                else if (currentHour >= 7f && currentHour < 16.5f)
                {
                    // Dia
                    targetColor = dayColor;
                    targetIntensity = maxSunIntensity;
                }
                else if (currentHour >= 16.5f && currentHour < 18.5f)
                {
                    // Pôr do sol
                    float t = (currentHour - 16.5f) / 2f;
                    targetColor = Color.Lerp(dayColor, sunsetColor, t);
                    targetIntensity = Mathf.Lerp(maxSunIntensity, maxSunIntensity * 0.6f, t);
                }
                else if (currentHour >= 18.5f && currentHour < 20f)
                {
                    // Crepúsculo entrando na noite
                    float t = (currentHour - 18.5f) / 1.5f;
                    targetColor = Color.Lerp(sunsetColor, nightColor, t);
                    targetIntensity = Mathf.Lerp(maxSunIntensity * 0.6f, minMoonIntensity, t);
                }
                else
                {
                    // Noite
                    targetColor = nightColor;
                    targetIntensity = minMoonIntensity;
                }

                directionalLight.color = targetColor;
                directionalLight.intensity = targetIntensity;
            }

            // Notifica transição dia/noite
            bool currentIsNight = IsNight;
            if (currentIsNight != wasNight || forceUpdate)
            {
                wasNight = currentIsNight;
                OnDayNightTransition?.Invoke(currentIsNight);
                Debug.Log($"[DayNightCycle] Transição de período: {(currentIsNight ? "NOITE INICIADA (Animais noturnos ativos)" : "DIA INICIADO")}");
            }

            OnTimeChanged?.Invoke(currentHour, currentIsNight);
        }

        public void SetTime(float hour)
        {
            currentHour = Mathf.Clamp(hour, 0f, 24f);
            UpdateLighting(true);
        }
    }
}

