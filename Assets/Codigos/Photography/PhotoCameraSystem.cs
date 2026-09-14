using System;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Cerrado.Photography
{
    public class PhotoCameraSystem : MonoBehaviour
    {
        [Header("Referências da Câmera")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private LayerMask occlusionLayers = ~0; // Camadas que bloqueiam visão (terreno, rochas, árvores)

        [Header("Configurações do Modo Fotografia")]
        [Tooltip("Campo de visão normal da câmera")]
        [SerializeField] private float normalFOV = 60f;
        [Tooltip("Campo de visão mínimo (zoom máximo)")]
        [SerializeField] private float minZoomFOV = 12f;
        [Tooltip("Campo de visão base ao mirar a câmera")]
        [SerializeField] private float baseAimFOV = 35f;
        [SerializeField] private float zoomSensitivity = 5f;
        [SerializeField] private float fovTransitionSpeed = 10f;

        [Header("Detecção de Espécies")]
        [Tooltip("Alcance máximo do sensor de foco da câmera")]
        [SerializeField] private float maxDetectionDistance = 60f;
        [SerializeField] private float focusTolerance = 0.35f; // Quão próximo do centro do visor para travar foco

        [Header("Áudio e Efeitos")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip shutterSound;

        // Eventos
        public static event Action<PhotoResult> OnPhotoTaken;
        public static event Action<bool> OnAimStateChanged;
        public static event Action<PhotographableTarget> OnTargetFocusChanged;

        // Estados
        private bool isAiming;
        private float currentTargetFOV;
        private PhotographableTarget currentFocusedTarget;
        private readonly List<PhotographableTarget> detectedTargetsCache = new List<PhotographableTarget>();

        public void UpgradeCamera(float minZoomDecrease, float focusToleranceIncrease)
        {
            minZoomFOV = Mathf.Max(4f, minZoomFOV - minZoomDecrease);
            focusTolerance = Mathf.Clamp(focusTolerance + focusToleranceIncrease, 0.1f, 0.8f);
        }

        public bool IsAiming => isAiming;
        public PhotographableTarget CurrentFocusedTarget => currentFocusedTarget;
        public float CurrentZoomRatio => Mathf.InverseLerp(baseAimFOV, minZoomFOV, playerCamera != null ? playerCamera.fieldOfView : baseAimFOV);

        private void Awake()
        {
            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>();
                if (playerCamera == null)
                {
                    playerCamera = Camera.main;
                }
            }

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            if (playerCamera != null)
            {
                normalFOV = playerCamera.fieldOfView;
                currentTargetFOV = normalFOV;
            }
        }

        private void Update()
        {
            HandleInput();
            HandleZoomAndFOV();

            if (isAiming)
            {
                ScanForTargets();
            }
            else if (currentFocusedTarget != null)
            {
                currentFocusedTarget = null;
                OnTargetFocusChanged?.Invoke(null);
            }
        }

        private void HandleInput()
        {
            bool aimInput = false;
            bool shootInput = false;
            float scrollDelta = 0f;

#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                aimInput = Mouse.current.rightButton.isPressed;
                shootInput = Mouse.current.leftButton.wasPressedThisFrame;
                scrollDelta = Mouse.current.scroll.ReadValue().y;
            }

            if (Keyboard.current != null && Keyboard.current.cKey.isPressed)
            {
                aimInput = true;
            }
#else
            aimInput = Input.GetMouseButton(1) || Input.GetKey(KeyCode.C);
            shootInput = Input.GetMouseButtonDown(0);
            scrollDelta = Input.mouseScrollDelta.y;
#endif

            // Transição de estado de mira (Viewfinder)
            if (aimInput != isAiming)
            {
                isAiming = aimInput;
                currentTargetFOV = isAiming ? baseAimFOV : normalFOV;
                OnAimStateChanged?.Invoke(isAiming);
            }

            // Controle de Zoom óptico pelo Scroll do mouse
            if (isAiming && Mathf.Abs(scrollDelta) > 0.01f)
            {
                float zoomStep = Mathf.Sign(scrollDelta) * zoomSensitivity;
                currentTargetFOV = Mathf.Clamp(currentTargetFOV - zoomStep, minZoomFOV, baseAimFOV);
            }

            // Disparo da Foto
            if (isAiming && shootInput)
            {
                TakePhoto();
            }
        }

        private void HandleZoomAndFOV()
        {
            if (playerCamera == null) return;
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, currentTargetFOV, Time.deltaTime * fovTransitionSpeed);
        }

        /// <summary>
        /// Varre o cenário para encontrar animais e plantas dentro do campo de visão da câmera
        /// </summary>
        private void ScanForTargets()
        {
            if (playerCamera == null) return;

            PhotographableTarget bestTarget = null;
            float closestDistanceToCenter = float.MaxValue;

            // Busca rápida de alvos ativos na cena
            PhotographableTarget[] allTargets = FindObjectsByType<PhotographableTarget>();

            Vector3 camPos = playerCamera.transform.position;

            foreach (var target in allTargets)
            {
                if (target == null || !target.CanBePhotographed) continue;

                float dist = Vector3.Distance(camPos, target.FocalPoint);
                if (dist > maxDetectionDistance) continue;

                // Converte para coordenadas do Viewport (0 a 1 em X e Y, Z positivo à frente)
                Vector3 viewPos = playerCamera.WorldToViewportPoint(target.FocalPoint);
                if (viewPos.z <= 0) continue; // Atrás da câmera

                // Checa se está dentro do visor retangular
                if (viewPos.x >= 0.1f && viewPos.x <= 0.9f && viewPos.y >= 0.1f && viewPos.y <= 0.9f)
                {
                    // Checa obstrução física
                    if (!target.HasLineOfSight(camPos, occlusionLayers)) continue;

                    float distFromCenter = Vector2.Distance(new Vector2(viewPos.x, viewPos.y), new Vector2(0.5f, 0.5f));
                    if (distFromCenter < closestDistanceToCenter && distFromCenter <= focusTolerance)
                    {
                        closestDistanceToCenter = distFromCenter;
                        bestTarget = target;
                    }
                }
            }

            if (bestTarget != currentFocusedTarget)
            {
                currentFocusedTarget = bestTarget;
                OnTargetFocusChanged?.Invoke(currentFocusedTarget);
            }
        }

        /// <summary>
        /// Realiza o clique fotográfico, avalia a qualidade e gera a foto
        /// </summary>
        public void TakePhoto()
        {
            if (playerCamera == null) return;

            // Toca som de obturador
            if (audioSource != null && shutterSound != null)
            {
                audioSource.PlayOneShot(shutterSound);
            }

            // Captura o frame atual em Texture2D
            Texture2D snapshot = CameraCapture.CaptureFrame(playerCamera, 960, 540);

            // Avalia a foto com as regras do GDD (integrado com o ciclo Dia/Noite)
            bool isNight = Cerrado.Environment.DayNightCycle.Instance != null && Cerrado.Environment.DayNightCycle.Instance.IsNight;
            PhotoResult result = PhotoEvaluator.EvaluatePhoto(playerCamera, currentFocusedTarget, isNight, occlusionLayers);
            result.PhotoTexture = snapshot;

            Debug.Log($"[PhotoCameraSystem] Foto capturada! Espécie: {(result.Species != null ? result.Species.CommonName : "Paisagem")} | Score: {result.TotalScore} | Moedas: +{result.CoinsEarned} | Estrelas: {result.Stars}");

            // Notifica todos os ouvintes (UI, Álbum, Sistema de Missões, Economia)
            OnPhotoTaken?.Invoke(result);
        }
    }
}

