using UnityEngine;

namespace Cerrado.AI
{
    /// <summary>
    /// Emite tufos de poeira avermelhada do solo do Cerrado quando o animal corre ou foge.
    /// Utiliza a textura WispySmoke com animação de spritesheet 8x8.
    /// </summary>
    public class AnimalDustTrail : MonoBehaviour
    {
        [Tooltip("Cor da terra avermelhada do Cerrado")]
        [SerializeField] private Color dustColor = new Color(0.72f, 0.42f, 0.24f, 0.40f);
        
        private ParticleSystem dustSystem;
        private AnimalAI animalAI;

        private void Awake()
        {
            animalAI = GetComponent<AnimalAI>();
            SetupDustParticleSystem();
        }

        private void Update()
        {
            if (dustSystem == null) return;

            var emission = dustSystem.emission;
            if (animalAI != null && animalAI.CurrentState == AnimalState.Flee)
            {
                emission.rateOverTime = 9f;
            }
            else
            {
                emission.rateOverTime = 0f;
            }
        }

        private void SetupDustParticleSystem()
        {
            var existing = transform.Find("Poeira_Cerrado");
            if (existing != null)
            {
                dustSystem = existing.GetComponent<ParticleSystem>();
                return;
            }

            var dustObj = new GameObject("Poeira_Cerrado");
            dustObj.transform.SetParent(transform);
            dustObj.transform.localPosition = new Vector3(0f, 0.05f, -0.2f);

            dustSystem = dustObj.AddComponent<ParticleSystem>();
            var main = dustSystem.main;
            main.startLifetime = 1.2f;
            main.startSpeed = 0.4f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.5f, 1.3f);
            main.startColor = dustColor;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 30;

            var emission = dustSystem.emission;
            emission.rateOverTime = 0f;

            var shape = dustSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.35f;

            var tsa = dustSystem.textureSheetAnimation;
            tsa.enabled = true;
            tsa.numTilesX = 8;
            tsa.numTilesY = 8;
            tsa.animation = ParticleSystemAnimationType.WholeSheet;

            var col = dustSystem.colorOverLifetime;
            col.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(dustColor, 0f), new GradientColorKey(dustColor, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.4f, 0f), new GradientAlphaKey(dustColor.a, 0.4f), new GradientAlphaKey(0f, 1f) }
            );
            col.color = grad;

            var sizeOverLife = dustSystem.sizeOverLifetime;
            sizeOverLife.enabled = true;
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0f, 0.35f);
            curve.AddKey(1f, 1.1f);
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, curve);

            var renderer = dustObj.GetComponent<ParticleSystemRenderer>();
            Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ??
                                    Shader.Find("Universal Render Pipeline/Particles/Simple Lit") ??
                                    Shader.Find("Particles/Standard Unlit");

            Material mat = new Material(particleShader);
            mat.SetFloat("_Surface", 1f); // Transparent
            mat.SetFloat("_Blend", 0f); // Alpha
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

#if UNITY_EDITOR
            var tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/TerrainSampleAssets/Textures/VFX/WispySmoke02_8x8.tga");
            if (tex != null)
            {
                mat.mainTexture = tex;
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            }
#endif
            mat.color = dustColor;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", dustColor);
            renderer.material = mat;
        }
    }
}
