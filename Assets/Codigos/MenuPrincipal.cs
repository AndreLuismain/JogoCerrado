using UnityEngine;
using UnityEngine.SceneManagement; // Biblioteca necessária para trocar de telas/cenas

public class MenuPrincipal : MonoBehaviour
{
    // Função para iniciar o jogo
    public void Jogar()
    {
        SceneManager.LoadScene("Assets/Scenes/SampleScene.unity");
    }

    // Função para retornar ao menu principal
    public void VoltarAoMenu()
    {
        SceneManager.LoadScene("Assets/Jogo/Menu/menu_cena.unity");
    }

    // Função opcional para fechar o jogo
    public void Sair()
    {
        Debug.Log("Saindo do jogo..."); // Mostra um aviso no Console do Unity
        Application.Quit(); // Fecha o jogo (funciona apenas no jogo compilado, não no editor)
    }
}
