using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cerrado.Photography;

namespace Cerrado.UI
{
    public class PhotoViewfinderUI : MonoBehaviour
    {
        [Header("Elementos do Visor")]
        [SerializeField] private GameObject viewfinderContainer;
        [SerializeField] private RectTransform focusFrame;
        [SerializeField] private TMP_Text targetNameText;
        [SerializeField] private TMP_Text zoomLevelText;
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

        [Header("Cores")]
        [SerializeField] private Color lockedFocusColor = new Color(0.2f, 0.9f, 0.3f, 0.9f);
        [SerializeField] private Color idleFocusColor = new Color(1f, 1f, 1f, 0.5f);

        private Camera mainCam;
        private Coroutine resultCoroutine;

        private void Awake()
        {
            mainCam = Camera.main;

            if (viewfinderContainer != null)
            {
                viewfinderContainer.SetActive(false);
            }

            if (resultPanel != null)
            {
                resultPanel.SetActive(false);
            }

            if (flashCanvasGroup != null)
            {
                flashCanvasGroup.alpha = 0f;
            }
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
            // Se o visor estiver ativo e houver alvo focado, posiciona a caixa de foco sobre o alvo na tela
            if (viewfinderContainer != null && viewfinderContainer.activeSelf && focusFrame != null)
            {
                var cameraSystem = FindAnyObjectByType<PhotoCameraSystem>();
                if (cameraSystem != null && cameraSystem.CurrentFocusedTarget != null && mainCam != null)
                {
                    Vector3 screenPoint = mainCam.WorldToScreenPoint(cameraSystem.CurrentFocusedTarget.FocalPoint);
                    focusFrame.position = screenPoint;
                    focusFrame.gameObject.SetActive(true);

                    if (zoomLevelText != null)
                    {
                        float zoomMultiplier = 1f + (cameraSystem.CurrentZoomRatio * 3f);
                        zoomLevelText.text = $"{zoomMultiplier:F1}x";
                    }
                }
                else
                {
                    if (focusFrame != null)
                    {
                        focusFrame.gameObject.SetActive(false);
                    }
                }
            }
        }

        private void HandleAimStateChanged(bool isAiming)
        {
            if (viewfinderContainer != null)
            {
                viewfinderContainer.SetActive(isAiming);
            }
        }

        private void HandleTargetFocusChanged(PhotographableTarget target)
        {
            if (target != null && target.Species != null)
            {
                if (targetNameText != null)
                {
                    targetNameText.text = $"{target.Species.CommonName} [{target.Species.Rarity}]";
                    targetNameText.gameObject.SetActive(true);
                }

                if (crosshairImage != null)
                {
                    crosshairImage.color = lockedFocusColor;
                }
            }
            else
            {
                if (targetNameText != null)
                {
                    targetNameText.gameObject.SetActive(false);
                }

                if (crosshairImage != null)
                {
                    crosshairImage.color = idleFocusColor;
                }
            }
        }

        private void HandlePhotoTaken(PhotoResult result)
        {
            // Dispara efeito de Flash
            StartCoroutine(PlayFlashEffect());

            // Mostra o painel de avaliação
            if (resultPanel != null)
            {
                if (resultCoroutine != null)
                {
                    StopCoroutine(resultCoroutine);
                }
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
            {
                photoThumbnail.texture = result.PhotoTexture;
            }

            if (speciesNameResultText != null)
            {
                speciesNameResultText.text = result.Species != null ? result.Species.CommonName : "Paisagem";
            }

            if (scoreText != null)
            {
                scoreText.text = $"Pontos: {result.TotalScore}";
            }

            if (coinsText != null)
            {
                coinsText.text = $"+{result.CoinsEarned} Moedas";
            }

            if (starsText != null)
            {
                starsText.text = new string('★', result.Stars) + new string('☆', 5 - result.Stars);
            }

            if (feedbackText != null)
            {
                feedbackText.text = result.FeedbackMessage;
            }

            yield return new WaitForSeconds(resultDisplayDuration);

            resultPanel.SetActive(false);
        }
    }
}

