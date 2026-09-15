using UnityEngine;

namespace Cerrado.Environment
{
    /// <summary>
    /// Gerencia a atmosfera volumétrica do Cerrado (névoa matinal e bruma nas baixadas).
    /// Utiliza as texturas WispySmoke com animação de spritesheet 8x8.
    /// </summary>
    public class CerradoAtmosphere : MonoBehaviour
    {
        [Header("Configurações de Névoa")]
        [SerializeField] private bool enableGroundMist = true;
        [SerializeField] private Color mistColor = new Color(0.92f, 0.88f, 0.80f, 0.22f);
        [SerializeField] private float mistAreaSize = 60f;

        private ParticleSystem mistParticles;

        private void Awake()
        {
            if (enableGroundMist && mistParticles == null)
            {
                SetupMistParticleSystem();
            }
        }

        public void SetupMistParticleSystem()
        {
            var existing = transform.Find("Cerrado_Nevoa_Chao");
            if (existing != null)
            {
                mistParticles = existing.GetComponent<ParticleSystem>();
                return;
            }

            var mistObj = new GameObject("Cerrado_Nevoa_Chao");
            mistObj.transform.SetParent(transform);
            mistObj.transform.localPosition = new Vector3(0f, 0.5f, 0f);

            mistParticles = mistObj.AddComponent<ParticleSystem>();
            var main = mistParticles.main;
            main.startLifetime = 12f;
            main.startSpeed = 0.25f;
            main.startSize = new ParticleSystem.MinMaxCurve(6f, 14f);
            main.startColor = mistColor;
            main.maxParticles = 60;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = mistParticles.emission;
            emission.rateOverTime = 4f;

            var shape = mistParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(mistAreaSize, 0.8f, mistAreaSize);

            // Sprite sheet 8x8
            var tsa = mistParticles.textureSheetAnimation;
            tsa.enabled = true;
            tsa.numTilesX = 8;
            tsa.numTilesY = 8;
            tsa.animation = ParticleSystemAnimationType.WholeSheet;
            tsa.cycleCount = 1;

            var colorOverLifetime = mistParticles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(mistColor, 0f), new GradientColorKey(mistColor, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(mistColor.a, 0.3f), new GradientAlphaKey(mistColor.a, 0.7f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = grad;

            var renderer = mistObj.GetComponent<ParticleSystemRenderer>();
            Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ??
                                    Shader.Find("Universal Render Pipeline/Particles/Simple Lit") ??
                                    Shader.Find("Particles/Standard Unlit");

            Material particleMat = new Material(particleShader);
            particleMat.SetFloat("_Surface", 1f); // Transparent
            particleMat.SetFloat("_Blend", 0f); // Alpha
            particleMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            particleMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            particleMat.SetInt("_ZWrite", 0);
            particleMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

#if UNITY_EDITOR
            var smokeTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/TerrainSampleAssets/Textures/VFX/WispySmoke01_8x8.tga");
            if (smokeTex != null)
            {
                particleMat.mainTexture = smokeTex;
                if (particleMat.HasProperty("_BaseMap")) particleMat.SetTexture("_BaseMap", smokeTex);
            }
#endif
            particleMat.color = mistColor;
            if (particleMat.HasProperty("_BaseColor")) particleMat.SetColor("_BaseColor", mistColor);
            renderer.material = particleMat;
        }
    }
}
