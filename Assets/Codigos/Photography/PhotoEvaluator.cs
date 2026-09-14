using UnityEngine;
using Cerrado.Data;

namespace Cerrado.Photography
{
    public static class PhotoEvaluator
    {
        public static PhotoResult EvaluatePhoto(
            Camera camera, 
            PhotographableTarget target, 
            bool isNight, 
            LayerMask occlusionLayers)
        {
            var result = new PhotoResult();

            // Se não há alvo focado, é apenas foto de paisagem
            if (target == null || !target.CanBePhotographed || camera == null)
            {
                result.Species = null;
                result.FramingScore = 0.5f;
                result.DistanceScore = 0.5f;
                result.ClarityScore = 1f;
                result.TotalScore = 20;
                result.Stars = 1;
                result.CoinsEarned = 2;
                result.FeedbackMessage = "Bela foto de paisagem do Cerrado!";
                return result;
            }

            result.Species = target.Species;
            result.IsNightPhoto = isNight;

            // 1. Avaliação de Enquadramento (centralização no visor)
            Vector3 viewportPos = camera.WorldToViewportPoint(target.FocalPoint);
            if (viewportPos.z <= 0)
            {
                // Alvo atrás da câmera
                result.FramingScore = 0f;
            }
            else
            {
                Vector2 centerOffset = new Vector2(viewportPos.x - 0.5f, viewportPos.y - 0.5f);
                float distanceFromCenter = centerOffset.magnitude; // 0 no centro, ~0.7 nas pontas
                result.FramingScore = Mathf.Clamp01(1f - (distanceFromCenter / 0.45f));
            }

            // 2. Avaliação de Distância
            float currentDistance = Vector3.Distance(camera.transform.position, target.FocalPoint);
            float minIdeal = target.Species.MinIdealDistance;
            float maxIdeal = target.Species.MaxIdealDistance;

            if (currentDistance >= minIdeal && currentDistance <= maxIdeal)
            {
                result.DistanceScore = 1f;
            }
            else if (currentDistance < minIdeal)
            {
                // Muito perto (cortando o alvo)
                result.DistanceScore = Mathf.Clamp01(currentDistance / Mathf.Max(0.1f, minIdeal));
            }
            else
            {
                // Muito longe (alvo pequeno na foto)
                float excess = currentDistance - maxIdeal;
                float falloff = maxIdeal * 1.5f;
                result.DistanceScore = Mathf.Clamp01(1f - (excess / falloff));
            }

            // 3. Avaliação de Clareza / Visibilidade (Line of Sight)
            bool hasDirectSight = target.HasLineOfSight(camera.transform.position, occlusionLayers);
            result.ClarityScore = hasDirectSight ? 1.0f : 0.25f;

            // 4. Multiplicador de Raridade (especificado no GDD)
            float rarityMultiplier = target.Species.Rarity switch
            {
                SpeciesRarity.Comum => 1.0f,
                SpeciesRarity.Incomum => 1.5f,
                SpeciesRarity.Raro => 2.5f,
                SpeciesRarity.MuitoRaro => 4.0f,
                _ => 1.0f
            };

            // 5. Bônus por condição especial (noturno)
            float specialBonus = 1.0f;
            if (target.Species.ActivityTime == ActivityTime.Noturno && isNight)
            {
                specialBonus = 1.35f;
            }

            // 6. Qualidade combinada e Estrelas (1 a 5)
            float quality = (result.FramingScore * 0.45f) + 
                            (result.DistanceScore * 0.35f) + 
                            (result.ClarityScore * 0.20f);

            if (quality >= 0.88f) result.Stars = 5;
            else if (quality >= 0.72f) result.Stars = 4;
            else if (quality >= 0.52f) result.Stars = 3;
            else if (quality >= 0.32f) result.Stars = 2;
            else result.Stars = 1;

            // 7. Pontuação e Moedas
            float rawScore = target.Species.BaseScore * rarityMultiplier * quality * specialBonus;
            result.TotalScore = Mathf.Max(10, Mathf.RoundToInt(rawScore));

            float rawCoins = target.Species.BaseCoins * rarityMultiplier * (result.Stars / 3.0f) * specialBonus;
            result.CoinsEarned = Mathf.Max(1, Mathf.RoundToInt(rawCoins));

            // 8. Mensagem de Feedback
            result.FeedbackMessage = GenerateFeedbackMessage(result, currentDistance, minIdeal, maxIdeal, hasDirectSight);

            return result;
        }

        private static string GenerateFeedbackMessage(
            PhotoResult result, 
            float distance, 
            float minIdeal, 
            float maxIdeal, 
            bool hasSight)
        {
            if (!hasSight)
            {
                return "Atenção: A visão do alvo estava parcialmente bloqueada por obstáculos!";
            }

            if (result.Stars == 5)
            {
                return "Foto Perfeita! Enquadramento e distância impecáveis!";
            }

            if (distance > maxIdeal)
            {
                return $"Boa foto, mas aproxime-se mais para capturar melhores detalhes ({distance:F1}m).";
            }

            if (distance < minIdeal)
            {
                return "Muito perto! Dê alguns passos para trás para não cortar o alvo.";
            }

            if (result.FramingScore < 0.6f)
            {
                return "Tente centralizar melhor o animal no visor da câmera.";
            }

            return "Ótimo registro fotográfico!";
        }
    }
}

