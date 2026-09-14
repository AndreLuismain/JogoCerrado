using UnityEngine;
using UnityEngine.SceneManagement; // Biblioteca necessária para trocar de telas/cenas

public class MenuPrincipal : MonoBehaviour
{
    // Função para iniciar o jogo
    public void Jogar()
    {
        // O Unity vai carregar a cena chamada "Fase1"
        // (Lembre-se de criar uma cena com esse exato nome depois)
        SceneManager.LoadScene("Fase1"); 
    }

    // Função opcional para fechar o jogo
    public void Sair()
    {
        Debug.Log("Saindo do jogo..."); // Mostra um aviso no Console do Unity
        Application.Quit(); // Fecha o jogo (funciona apenas no jogo compilado, não no editor)
    }
}