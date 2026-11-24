using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cerrado.Photography;
using Cerrado.Album;

namespace Cerrado.UI
{
    /// <summary>
    /// Visor fotográfico clássico em branco (estilo viewfinder de câmera/filmadora):
    /// cantoneiras brancas, retículo central '+', indicador '● REC', barra técnica superior
    /// ('HD | 4K | ZOOM' + bateria) e ícone discreto acima do alvo indicando se já foi catalogado ou é novo.
    /// Sem quadrado verde em volta do alvo.
    /// </summary>
    public class PhotoViewfinderUI : MonoBehaviour
    {
        [Header("Elementos do Visor Branco")]
        [SerializeField] private GameObject viewfinderContainer;
        [SerializeField] private RectTransform targetStatusBadge;
        [SerializeField] private Image targetStatusBg;
        [SerializeField] private TMP_Text targetStatusIconText;
        [SerializeField] private TMP_Text targetStatusLabelText;
        [SerializeField] private TMP_Text targetNameText;
        [SerializeField] private Image targetIconImage;
        [SerializeField] private TMP_Text zoomLevelText;
        [SerializeField] private TMP_Text recTimerText;
        [SerializeField] private Image recDotImage;
        [SerializeField] private Image crosshairImage;

        [Header("Efeito de Flash")]
        [SerializeField] private CanvasGroup flashCanvasGroup;
        [SerializeField] private float flashFadeSpeed = 4f;

        [Header("Painel de Resultado da Foto")]
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private RawImage photoThumbnail;
        [SerializeField] private TMP_Text speciesNameResultText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text coinsText;
        [SerializeField] private TMP_Text starsText;
        [SerializeField] private TMP_Text feedbackText;
        [SerializeField] private float resultDisplayDuration = 3.5f;

        [Header("Cores (Estilo Viewfinder Branco)")]
        [SerializeField] private Color activeReticleColor = new Color(1f, 1f, 1f, 0.96f);
        [SerializeField] private Color idleReticleColor = new Color(1f, 1f, 1f, 0.72f);
        [SerializeField] private Color catalogedBadgeColor = new Color(0.14f, 0.52f, 0.48f, 0.88f);
        [SerializeField] private Color newSpeciesBadgeColor = new Color(0.92f, 0.62f, 0.12f, 0.92f);

        private Camera mainCam;
        private PhotoCameraSystem cachedCameraSystem;
        private Coroutine resultCoroutine;
        private float aimElapsedSeconds;

        private void Awake()
        {
            mainCam = Camera.main;
            cachedCameraSystem = FindAnyObjectByType<PhotoCameraSystem>();

            if (viewfinderContainer != null) viewfinderContainer.SetActive(false);
            if (targetStatusBadge != null) targetStatusBadge.gameObject.SetActive(false);
            if (resultPanel != null) resultPanel.SetActive(false);
            if (flashCanvasGroup != null) flashCanvasGroup.alpha = 0f;
        }

        private void OnEnable()
        {
            PhotoCameraSystem.OnAimStateChanged += HandleAimStateChanged;
            PhotoCameraSystem.OnTargetFocusChanged += HandleTargetFocusChanged;
            PhotoCameraSystem.OnPhotoTaken += HandlePhotoTaken;
        }

        private void OnDisable()
        {
            PhotoCameraSystem.OnAimStateChanged -= HandleAimStateChanged;
            PhotoCameraSystem.OnTargetFocusChanged -= HandleTargetFocusChanged;
            PhotoCameraSystem.OnPhotoTaken -= HandlePhotoTaken;
        }

        private void Update()
        {
            if (viewfinderContainer == null || !viewfinderContainer.activeSelf) return;

            if (mainCam == null) mainCam = Camera.main;
            if (cachedCameraSystem == null) cachedCameraSystem = FindAnyObjectByType<PhotoCameraSystem>();

            // Atualiza relógio REC e ponto vermelho piscante
            aimElapsedSeconds += Time.unscaledDeltaTime;
            if (recTimerText != null)
            {
                int totalSec = Mathf.FloorToInt(aimElapsedSeconds);
                int mins = (totalSec / 60) % 60;
                int secs = totalSec % 60;
                int frames = Mathf.FloorToInt((aimElapsedSeconds - totalSec) * 30f);
                recTimerText.text = $"00:{mins:00}:{secs:00}:{frames:00}";
            }
            if (recDotImage != null)
            {
                float pulse = Mathf.PingPong(Time.unscaledTime * 1.8f, 1f);
                Color c = recDotImage.color;
                c.a = Mathf.Lerp(0.35f, 1f, pulse);
                recDotImage.color = c;
            }

            if (cachedCameraSystem != null && zoomLevelText != null)
            {
                float zoomMultiplier = 1f + (cachedCameraSystem.CurrentZoomRatio * 3f);
                zoomLevelText.text = $"HD  |  4K  |  {zoomMultiplier:F1}x";
            }

            // Posiciona APENAS o ícone discreto de status ACIMA do alvo focado (sem quadrado verde!)
            if (targetStatusBadge != null)
            {
                var target = cachedCameraSystem != null ? cachedCameraSystem.CurrentFocusedTarget : null;
                if (target != null && target.Species != null && mainCam != null)
                {
                    float verticalLift = Mathf.Clamp(target.Species.TargetRadius * 0.72f, 0.55f, 3.8f);
                    Vector3 worldAbove = target.FocalPoint + Vector3.up * verticalLift;
                    Vector3 screenPoint = mainCam.WorldToScreenPoint(worldAbove);

                    if (screenPoint.z > 0.1f)
                    {
                        screenPoint.x = Mathf.Clamp(screenPoint.x, 90f, Screen.width - 90f);
                        screenPoint.y = Mathf.Clamp(screenPoint.y, 90f, Screen.height - 90f);
                        targetStatusBadge.position = screenPoint;
                        targetStatusBadge.gameObject.SetActive(true);
                        RefreshTargetStatusVisual(target);
                    }
                    else
                    {
                        targetStatusBadge.gameObject.SetActive(false);
                    }
                }
                else
                {
                    targetStatusBadge.gameObject.SetActive(false);
                }
            }
        }

        private void RefreshTargetStatusVisual(PhotographableTarget target)
        {
            if (target == null || target.Species == null) return;

            bool isCataloged = AlbumManager.Instance != null && AlbumManager.Instance.IsSpeciesDiscovered(target.Species.Id);

            if (targetStatusBg != null)
            {
                targetStatusBg.color = isCataloged ? catalogedBadgeColor : newSpeciesBadgeColor;
            }

            if (targetStatusIconText != null)
            {
                // Ícone limpo compatível com LiberationSans SDF (sem glifos quebrados)
                targetStatusIconText.text = isCataloged ? "OK" : "!";
            }

            if (targetStatusLabelText != null)
            {
                targetStatusLabelText.text = isCataloged ? "CATALOGADA" : "NOVA";
            }
        }

        private void HandleAimStateChanged(bool isAiming)
        {
            if (viewfinderContainer != null)
            {
                viewfinderContainer.SetActive(isAiming);
            }
            if (!isAiming && targetStatusBadge != null)
            {
                targetStatusBadge.gameObject.SetActive(false);
            }
        }

        private void HandleTargetFocusChanged(PhotographableTarget target)
        {
            if (target != null && target.Species != null)
            {
                bool isCataloged = AlbumManager.Instance != null && AlbumManager.Instance.IsSpeciesDiscovered(target.Species.Id);

                if (targetNameText != null)
                {
                    string statusTag = isCataloged ? "[CATALOGADA]" : "[NOVA ESPÉCIE]";
                    targetNameText.text = $"{target.Species.CommonName}  ·  {target.Species.Rarity}  {statusTag}";
                    targetNameText.gameObject.SetActive(true);
                }

                if (targetIconImage != null)
                {
                    targetIconImage.sprite = target.Species.Icon;
                    targetIconImage.preserveAspect = true;
                    targetIconImage.gameObject.SetActive(target.Species.Icon != null);
                }

                if (crosshairImage != null)
                {
                    crosshairImage.color = activeReticleColor;
                }

                RefreshTargetStatusVisual(target);
            }
            else
            {
                if (targetNameText != null) targetNameText.gameObject.SetActive(false);
                if (targetIconImage != null) targetIconImage.gameObject.SetActive(false);
                if (targetStatusBadge != null) targetStatusBadge.gameObject.SetActive(false);
                if (crosshairImage != null) crosshairImage.color = idleReticleColor;
            }
        }

        private void HandlePhotoTaken(PhotoResult result)
        {
            StartCoroutine(PlayFlashEffect());

            if (resultPanel != null)
            {
                if (resultCoroutine != null) StopCoroutine(resultCoroutine);
                resultCoroutine = StartCoroutine(ShowResultPanelRoutine(result));
            }
        }

        private IEnumerator PlayFlashEffect()
        {
            if (flashCanvasGroup == null) yield break;

            flashCanvasGroup.alpha = 1f;
            while (flashCanvasGroup.alpha > 0.01f)
            {
                flashCanvasGroup.alpha = Mathf.MoveTowards(flashCanvasGroup.alpha, 0f, Time.deltaTime * flashFadeSpeed);
                yield return null;
            }
            flashCanvasGroup.alpha = 0f;
        }

        private IEnumerator ShowResultPanelRoutine(PhotoResult result)
        {
            resultPanel.SetActive(true);

            if (photoThumbnail != null && result.PhotoTexture != null)
                photoThumbnail.texture = result.PhotoTexture;

            if (speciesNameResultText != null)
                speciesNameResultText.text = result.Species != null ? result.Species.CommonName : "Paisagem";

            if (scoreText != null)
                scoreText.text = $"Pontos: {result.TotalScore}";

            if (coinsText != null)
                coinsText.text = $"+{result.CoinsEarned} Moedas";

            if (starsText != null)
                starsText.text = new string('*', result.Stars) + new string('.', 5 - result.Stars);

            if (feedbackText != null)
                feedbackText.text = result.FeedbackMessage;

            yield return new WaitForSeconds(resultDisplayDuration);

            resultPanel.SetActive(false);
        }
    }
}
