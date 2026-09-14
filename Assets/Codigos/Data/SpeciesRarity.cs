namespace Cerrado.Data
{
    public enum SpeciesRarity
    {
        Comum,       // Fácil de encontrar (Ex: Coelho, Tatu, plantas básicas)
        Incomum,     // Requer exploração (Ex: Ema, Seriema, Teiú)
        Raro,        // Aparece em condições específicas (Ex: Lobo-guará, Veado-campeiro)
        MuitoRaro    // Recorrência muito rara (Ex: Onça-parda, Jacaré-papo-amarelo)
    }

    public enum SpeciesCategory
    {
        Fauna, // Animal
        Flora  // Planta / Vegetação
    }

    public enum ActivityTime
    {
        Diurno,   // Ativo durante o dia
        Noturno,  // Ativo durante a noite
        Ambos     // Presente tanto de dia quanto de noite (ou plantas)
    }
}

