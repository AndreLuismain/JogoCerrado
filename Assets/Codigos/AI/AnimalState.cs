namespace Cerrado.AI
{
    public enum AnimalState
    {
        Idle,    // Parado, descansando ou pastando
        Wander,  // Caminhando tranquilamente pelo bioma
        Alert,   // Percebeu o jogador, para e avalia a situação
        Flee     // Fugindo em velocidade alta para longe do perigo
    }

    public enum AnimalTemperament
    {
        Docil,     // Não se assusta facilmente (ex: Capivara, Tamanduá)
        Arisco,    // Muito assustadiço, foge rápido (ex: Ema, Coelho, Seriema)
        Cauteloso  // Mantém distância segura e se esgueira (ex: Lobo-guará, Onça-parda)
    }
}

