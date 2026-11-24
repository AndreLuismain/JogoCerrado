using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Cerrado.UI;
using Cerrado.Player;

namespace Cerrado.Environment
{
    /// <summary>
    /// Garante em tempo de execução na Beta que o terreno placeholder tenha colisão segura,
    /// que o Álbum (TAB/J) escute entradas mesmo em cenas antigas e que o Menu de Pausa (ESC/P) esteja sempre disponível.
    /// </summary>
    public static class CerradoBetaBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnAfterSceneLoad()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.name.ToLower().Contains("menu")) return;

            EnsureTerrainPlaceholderSafety();
            EnsureUIAndPauseMenuSafety();
        }

        private static void EnsureTerrainPlaceholderSafety()
        {
            GameObject placeholder = GameObject.Find("--- TERRENO_PLACEHOLDER ---") ?? GameObject.Find("Chao_Cerrado");
            if (placeholder == null)
            {
                placeholder = GameObject.CreatePrimitive(PrimitiveType.Plane);
                placeholder.name = "--- TERRENO_PLACEHOLDER ---";
                placeholder.transform.position = Vector3.zero;
                placeholder.transform.localScale = new Vector3(16f, 1f, 16f);
            }
            else if (placeholder.GetComponent<Collider>() == null && placeholder.GetComponentInChildren<Collider>() == null)
            {
                placeholder.AddComponent<BoxCollider>();
            }
        }

        private static void EnsureUIAndPauseMenuSafety()
        {
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            if (Object.FindAnyObjectByType<PauseMenuUI>(FindObjectsInactive.Include) == null)
            {
                GameObject pauseRoot = new GameObject("Modal_Pausa_Runtime");
                pauseRoot.transform.SetParent(canvas.transform, false);
                var pauseRect = pauseRoot.AddComponent<RectTransform>();
                pauseRect.anchorMin = Vector2.zero;
                pauseRect.anchorMax = Vector2.one;
                pauseRect.offsetMin = Vector2.zero;
                pauseRect.offsetMax = Vector2.zero;

                GameObject pauseWin = new GameObject("Janela_Pausa");
                pauseWin.transform.SetParent(pauseRoot.transform, false);
                var winRect = pauseWin.AddComponent<RectTransform>();
                winRect.sizeDelta = new Vector2(460, 380);
                var winImg = pauseWin.AddComponent<Image>();
                winImg.color = new Color(0.14f, 0.1f, 0.07f, 0.96f);

                CreateRuntimeButton(pauseWin.transform, "Btn_Continuar", "CONTINUAR (ESC)", new Vector2(0, 75), new Color(0.22f, 0.55f, 0.25f), out Button btnResume);
                CreateRuntimeButton(pauseWin.transform, "Btn_MenuPrincipal", "MENU PRINCIPAL", new Vector2(0, -15), new Color(0.55f, 0.42f, 0.18f), out Button btnMenu);
                CreateRuntimeButton(pauseWin.transform, "Btn_Sair", "SAIR DO JOGO", new Vector2(0, -105), new Color(0.65f, 0.22f, 0.18f), out Button btnQuit);

                pauseWin.SetActive(false);

                var pauseUI = pauseRoot.AddComponent<PauseMenuUI>();
                btnResume.onClick.AddListener(pauseUI.ResumeGame);
                btnMenu.onClick.AddListener(pauseUI.ReturnToMainMenu);
                btnQuit.onClick.AddListener(pauseUI.QuitGame);
            }
        }

        private static void CreateRuntimeButton(Transform parent, string name, string label, Vector2 pos, Color bgColor, out Button btn)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);
            var rect = btnObj.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(300, 62);
            var img = btnObj.AddComponent<Image>();
            img.color = bgColor;
            btn = btnObj.AddComponent<Button>();

            GameObject txtObj = new GameObject("Texto");
            txtObj.transform.SetParent(btnObj.transform, false);
            var txtRect = txtObj.AddComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.offsetMin = Vector2.zero;
            txtRect.offsetMax = Vector2.zero;
            var tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 22;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
        }
    }
}
