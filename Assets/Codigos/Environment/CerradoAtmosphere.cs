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
        [SerializeField] private bool enableGroundMist = false; // Desativada por padrão para não poluir visão; sutil se ativada
        [SerializeField] private Color mistColor = new Color(0.92f, 0.88f, 0.80f, 0.05f);
        [SerializeField] private float mistAreaSize = 40f;

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
                DestroyImmediate(existing.gameObject);
            }

            var mistObj = new GameObject("Cerrado_Nevoa_Chao");
            mistObj.transform.SetParent(transform);
            mistObj.transform.localPosition = new Vector3(0f, 0.2f, 0f);

            mistParticles = mistObj.AddComponent<ParticleSystem>();
            var main = mistParticles.main;
            main.startLifetime = 8f;
            main.startSpeed = 0.15f;
            main.startSize = new ParticleSystem.MinMaxCurve(1.5f, 3.5f);
            main.startColor = mistColor;
            main.maxParticles = 20;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = mistParticles.emission;
            emission.rateOverTime = 1f;

            var shape = mistParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(mistAreaSize, 0.4f, mistAreaSize);

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
            particleMat.SetOverrideTag("RenderType", "Transparent");
            particleMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            particleMat.DisableKeyword("_SURFACE_TYPE_OPAQUE");
            particleMat.EnableKeyword("_BLENDMODE_ALPHA");

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
