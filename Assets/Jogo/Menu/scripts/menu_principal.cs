using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Cerrado.Settings;
using Cerrado.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class menu_principal : MonoBehaviour
{
    [SerializeField] private string nomeDoLevel = "SampleScene";
    [SerializeField] private GameObject painel_menu_principal;
    [SerializeField] private GameObject painel_opcoes;
    [SerializeField] private GameObject painel_extras;

    private void Awake()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        GameSettingsManager.LoadAndApplyAll();
        NormalizeMenuLayoutAtRuntime();

        if (painel_menu_principal != null) painel_menu_principal.SetActive(true);
        if (painel_opcoes != null) painel_opcoes.SetActive(false);
        if (painel_extras != null) painel_extras.SetActive(false);
    }

    private void Update()
    {
        bool escPressed = false;
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            escPressed = true;
        }
#else
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            escPressed = true;
        }
#endif
        if (escPressed)
        {
            if (painel_opcoes != null && painel_opcoes.activeSelf)
            {
                fechar_opcoes();
            }
            else if (painel_extras != null && painel_extras.activeSelf)
            {
                fechar_extras();
            }
        }
    }

    private void NormalizeMenuLayoutAtRuntime()
    {
        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        var scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        // Corrige os containers principais que tinham m_SizeDelta (-1820, -980) na cena original
        FixFullStretchContainer(painel_menu_principal);
        FixFullStretchContainer(painel_opcoes);
        FixFullStretchContainer(painel_extras);
    }

    private static void FixFullStretchContainer(GameObject panel)
    {
        if (panel == null) return;
        var rt = panel.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
        }
    }

    public void jogar()
    {
        Time.timeScale = 1f;
        if (string.IsNullOrEmpty(nomeDoLevel) || nomeDoLevel == "jogo_inicio" || nomeDoLevel == "SampleScene")
        {
            SceneManager.LoadScene("Assets/Scenes/SampleScene.unity");
        }
        else
        {
            SceneManager.LoadScene(nomeDoLevel);
        }
    }

    public void abrir_opcoes()
    {
        if (painel_menu_principal != null) painel_menu_principal.SetActive(false);
        if (painel_extras != null) painel_extras.SetActive(false);
        if (painel_opcoes != null)
        {
            painel_opcoes.SetActive(true);
            var settingsUI = painel_opcoes.GetComponentInChildren<SettingsPanelUI>(true);
            if (settingsUI != null) settingsUI.RefreshUIFromSettings();
        }
    }

    public void fechar_opcoes()
    {
        if (painel_opcoes != null) painel_opcoes.SetActive(false);
        if (painel_extras != null) painel_extras.SetActive(false);
        if (painel_menu_principal != null) painel_menu_principal.SetActive(true);
    }

    public void abrir_extras()
    {
        if (painel_menu_principal != null) painel_menu_principal.SetActive(false);
        if (painel_opcoes != null) painel_opcoes.SetActive(false);
        if (painel_extras != null) painel_extras.SetActive(true);
    }

    public void fechar_extras()
    {
        if (painel_extras != null) painel_extras.SetActive(false);
        if (painel_opcoes != null) painel_opcoes.SetActive(false);
        if (painel_menu_principal != null) painel_menu_principal.SetActive(true);
    }

    public void sair_jogo()
    {
        Debug.Log("Saiu do jogo");
        Application.Quit();
    }
}
