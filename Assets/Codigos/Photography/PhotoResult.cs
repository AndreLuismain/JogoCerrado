using System;
using UnityEngine;
using Cerrado.Data;

namespace Cerrado.Photography
{
    [System.Serializable]
    public class PhotoResult
    {
        public SpeciesData Species;
        public float FramingScore;      // 0 a 1 (centralização no visor)
        public float DistanceScore;     // 0 a 1 (adequação de distância)
        public float ClarityScore;      // 0 a 1 (visibilidade e foco)
        public int TotalScore;          // Pontuação final calculada
        public int Stars;               // 1 a 5 estrelas
        public int CoinsEarned;         // Moedas obtidas
        public bool IsNightPhoto;       // Se foi tirada à noite
        public bool IsNewDiscovery;     // Primeira vez fotografando esta espécie
        public string FeedbackMessage;  // Dica/elogio para o jogador
        public Texture2D PhotoTexture;  // Imagem capturada
        public DateTime Timestamp;      // Momento em que a foto foi tirada

        public PhotoResult()
        {
            Timestamp = DateTime.Now;
        }

        public string GetRarityLabel()
        {
            if (Species == null) return "Paisagem";
            return Species.Rarity.ToString();
        }
    }
}

