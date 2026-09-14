using UnityEngine;

namespace Cerrado.Photography
{
    public static class CameraCapture
    {
        /// <summary>
        /// Captura a imagem renderizada por uma câmera em uma Texture2D com a resolução desejada.
        /// </summary>
        public static Texture2D CaptureFrame(Camera sourceCamera, int width = 960, int height = 540)
        {
            if (sourceCamera == null)
            {
                Debug.LogWarning("[CameraCapture] Nenhuma câmera informada para captura.");
                return null;
            }

            // Cria um RenderTexture temporário com a resolução do álbum
            RenderTexture rt = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.Default);
            RenderTexture previousTarget = sourceCamera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;

            sourceCamera.targetTexture = rt;
            sourceCamera.Render();

            RenderTexture.active = rt;

            // Cria a textura e lê os pixels da câmera
            Texture2D capturedPhoto = new Texture2D(width, height, TextureFormat.RGB24, false);
            capturedPhoto.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            capturedPhoto.Apply();

            // Restaura o estado anterior da câmera para não bugar o URP
            sourceCamera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            RenderTexture.ReleaseTemporary(rt);

            return capturedPhoto;
        }
    }
}

