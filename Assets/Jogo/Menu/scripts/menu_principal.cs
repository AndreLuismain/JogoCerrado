using UnityEngine;
using UnityEngine.SceneManagement;

public class menu_principal : MonoBehaviour
{
    [SerializeField] private string nomeDoLevel = "SampleScene";
    [SerializeField] private GameObject painel_menu_principal;
    [SerializeField] private GameObject painel_opcoes;
    [SerializeField] private GameObject painel_extras;

    public void jogar()
    {
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
        painel_menu_principal.SetActive(false);
        painel_opcoes.SetActive(true);
    }

    public void fechar_opcoes()
    {
        painel_menu_principal.SetActive(true);
        painel_opcoes.SetActive(false);
    }

    public void abrir_extras()
    {
        painel_menu_principal.SetActive(false);
        painel_extras.SetActive(true);
    }

    public void fechar_extras()
    {
        painel_menu_principal.SetActive(true);
        painel_extras.SetActive(false);
    }

    public void sair_jogo()
    {
        Debug.Log("Saiu do jogo");
        Application.Quit();
    }
}
