using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using Cerrado.Data;
using Cerrado.Player;
using Cerrado.Photography;
using Cerrado.Album;
using Cerrado.Persistence;
using Cerrado.Economy;
using Cerrado.AI;
using Cerrado.Interaction;
using Cerrado.Environment;
using Cerrado.Quests;
using Cerrado.UI;
using Cerrado.Audio;
using UnityEngine.AI;
using UnityEngine.Rendering;

namespace Cerrado.EditorTools
{
    public static class CerradoSceneSetupEditor
    {
        [MenuItem("Cerrado/4. Montar Cena de Teste Completa (SampleScene)")]
        public static void BuildCompleteTestScene()
        {
            if (!EditorUtility.DisplayDialog("Cerrado Engine", 
                "Deseja configurar a cena com Player, Terreno Placeholder único (sem Z-fighting), 12 Espécies em modelos 3D reais (.fbx), IA com NavMesh, Diálogos do Guia, Loja, Pausa e Álbum estilo Caderno de Campo?", 
                "Sim, Configurar Tudo!", "Cancelar"))
            {
                return;
            }

            BuildCompleteTestSceneSilent();

            EditorUtility.DisplayDialog("Cerrado Engine", 
                "Cena Beta do Cerrado montada com sucesso!\n\n" +
                "• Superfície única em '--- TERRENO_PLACEHOLDER ---' (zero Z-fighting) e NavMesh assada\n" +
                "• 12 Espécies + NPC Guia com modelos 3D reais (.fbx) — zero cápsulas ou árvores primitivas\n" +
                "• Materiais URP Lit PBR para animais (incluindo Onça-parda, Jacaré, Tatu, Coelho, Lobo, Ema e Tamanduá) e árvores (tronco + copa alpha cutout)\n" +
                "• Novo Álbum estilo Caderno de Campo Científico (Tab), Loja (E) e Pausa (ESC/P)\n\n" +
                "Basta apertar PLAY no topo do Unity para testar!", "OK");
        }

        public static void BuildCompleteTestSceneSilent()
        {
            Undo.SetCurrentGroupName("Cerrado: Montagem Completa da Cena Beta");
            int undoGroup = Undo.GetCurrentGroup();

            try
            {
                // 0. Remove qualquer primitiva antiga solta (cápsulas azuis, árvores estilo Roblox Tronco/Copa, planos duplicados)
                CleanupLegacyPrimitivesInScene();

                // 1. Espaço Reservado do Terreno (--- TERRENO_PLACEHOLDER ---) com superfície ÚNICA + NavMesh Funcional
                GameObject ground = SetupTerrainPlaceholder();

                // 2. Luz e Atmosfera (Skybox evening_field_8k, Sol fim de tarde, Fog e Volume Profile ACES sem Motion Blur)
                CerradoMigrationEditor.PrepareMigratedAssetsAndURP();
                Light sun = SetupLighting();

                // 3. Gerenciadores Centrais (Álbum, Missões, Loja, Dia/Noite)
                SetupManagers(sun);

                // 4. Player e Câmera
                GameObject player = SetupPlayer();
                var playerController = player.GetComponent<PlayerController>();

                // 5. NPC Guia e Comerciante (modelo 3D real tamandua_npc.fbx, sem cápsula azul)
                GameObject npc = SetupNPC(new Vector3(0f, 0f, 4.2f));
                var guideNPC = npc.GetComponent<GuideNPC>();
                var shopNPC = npc.GetComponent<ShopNPC>();

                // 6. Modelos 3D Reais (12 Espécies), Vegetação, Limites da Reserva e Marcadores de Cenário
                SetupEntities();
                SetupVegetation();
                SetupMapBoundaries();
                SetupSceneBookmarks();
                AlignRemainingSceneObjectsToTerrain();

                // 7. Corrige qualquer material incompatível restante na cena para URP Lit e aplica Translucidez + Vento nas folhagens
                FixAllPinkMaterialsInScene();
                CerradoMigrationEditor.UpgradeAllFoliageAndGrassMaterialsToShowcase();
                AssetDatabase.SaveAssets();

                // 8. UI Canvas Completa (Visor, Diálogos do Guia, Álbum Caderno de Campo, Loja, Missões e Menu de Pausa)
                SetupUI(playerController, guideNPC, shopNPC);

                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            }
            finally
            {
                Undo.CollapseUndoOperations(undoGroup);
            }
        }

        /// <summary>
        /// Remove cápsulas primitivas antigas, árvores de cilindro+esfera ("Tronco"/"Copa") e planos duplicados.
        /// </summary>
        private static void CleanupLegacyPrimitivesInScene()
        {
            var allFilters = Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include);
            var toDestroy = new List<GameObject>();

            foreach (var mf in allFilters)
            {
                if (mf == null || mf.gameObject == null) continue;
                string meshName = mf.sharedMesh != null ? mf.sharedMesh.name.ToLower() : "";
                string goName = mf.gameObject.name.ToLower();

                // Remove árvores "Roblox" (Tronco cilíndrico + Copa esférica)
                if (goName == "tronco" || goName == "copa")
                {
                    if (mf.transform.parent != null && !toDestroy.Contains(mf.transform.parent.gameObject))
                    {
                        toDestroy.Add(mf.transform.parent.gameObject);
                    }
                    else if (!toDestroy.Contains(mf.gameObject))
                    {
                        toDestroy.Add(mf.gameObject);
                    }
                    continue;
                }

                // Remove qualquer Capsule primitiva do Unity (exceto se for filho do Player)
                if (meshName.Contains("capsule"))
                {
                    if (mf.transform.root != null && mf.transform.root.name == "Player") continue;
                    if (!toDestroy.Contains(mf.gameObject))
                    {
                        toDestroy.Add(mf.gameObject);
                    }
                }
            }

            foreach (var go in toDestroy)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
            }
        }

        [MenuItem("Cerrado/5. Aplicar Novos Efeitos e Marcadores no Cenário Atual")]
        public static void ApplyAtmosphereAndBookmarksToCurrentScene()
        {
            Undo.SetCurrentGroupName("Cerrado: Aplicar Efeitos e Marcadores");
            int undoGroup = Undo.GetCurrentGroup();

            try
            {
                int fixedMats = FixAllPinkMaterialsInScene();

                GameObject ambObj = GameObject.Find("_Ambiente");
                if (ambObj == null) ambObj = new GameObject("_Ambiente");
                var atm = GetOrAddComponent<CerradoAtmosphere>(ambObj);
                atm.SetupMistParticleSystem();
                GetOrAddComponent<TerrainTreeToggle>(ambObj);

                var animals = Object.FindObjectsByType<AnimalAI>(FindObjectsInactive.Include);
                int count = 0;
                foreach (var a in animals)
                {
                    GetOrAddComponent<AnimalDustTrail>(a.gameObject);
                    count++;
                }

                SetupSceneBookmarks();

                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

                EditorUtility.DisplayDialog("Cerrado Engine",
                    $"Novos efeitos aplicados com sucesso no cenário atual!\n\n" +
                    $"• {fixedMats} materiais atualizados para shaders nativos URP Lit\n" +
                    $"• Névoa volumétrica do Cerrado (WispySmoke) configurada em '_Ambiente'\n" +
                    $"• Rastro de poeira avermelhada (AnimalDustTrail) adicionado a {count} animais\n" +
                    $"• Marcadores de cenário (SceneAnnotation) criados em '_Marcadores_Cenário'", "OK");
            }
            finally
            {
                Undo.CollapseUndoOperations(undoGroup);
            }
        }

        [MenuItem("Cerrado/6. Corrigir Materiais Rosa (Shaders URP)")]
        public static void FixMaterialsMenu()
        {
            int count = FixAllPinkMaterialsInScene();
            EditorUtility.DisplayDialog("Cerrado Engine", $"{count} materiais incompatíveis foram convertidos para materiais URP Lit com sucesso!", "OK");
        }

        public static int FixAllPinkMaterialsInScene()
        {
            var renderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include);
            int fixedCount = 0;
            foreach (var r in renderers)
            {
                if (r is ParticleSystemRenderer) continue;

                var mats = r.sharedMaterials;
                if (mats == null || mats.Length == 0) continue;

                bool modified = false;
                string objName = r.gameObject.name.ToLower();
                Transform p = r.transform.parent;
                while (p != null)
                {
                    objName += " " + p.gameObject.name.ToLower();
                    p = p.parent;
                }

                for (int i = 0; i < mats.Length; i++)
                {
                    bool isGrassOrBush = objName.Contains("grass") || objName.Contains("grama") || objName.Contains("bush") || objName.Contains("arbusto") || objName.Contains("heather") || objName.Contains("fern") || objName.Contains("plant");
                    bool hasWrongOpaque = mats[i] != null && mats[i].name == "Mat_Elemento_Cerrado" && isGrassOrBush;
                    bool isDefaultUntexturedWhite = mats[i] != null && (mats[i].name.Contains("Default") || mats[i].mainTexture == null) &&
                                                    (objName.Contains("onca") || objName.Contains("ema") || objName.Contains("jacare") || objName.Contains("tatu") || objName.Contains("lobo") || objName.Contains("tamandua") || objName.Contains("coelho"));

                    if (mats[i] == null || mats[i].shader == null || 
                        mats[i].shader.name.StartsWith("Hidden/InternalErrorShader") ||
                        mats[i].shader.name == "Hidden/DefaultErrorShader" ||
                        !mats[i].shader.name.Contains("Universal Render Pipeline") ||
                        hasWrongOpaque ||
                        isDefaultUntexturedWhite)
                    {
                        mats[i] = ResolveContextualMaterial(objName, i);
                        modified = true;
                        fixedCount++;
                    }
                }
                if (modified)
                {
                    r.sharedMaterials = mats;
                    EditorUtility.SetDirty(r);
                }
            }
            AssetDatabase.SaveAssets();
            return fixedCount;
        }

        private static Material ResolveContextualMaterial(string objName, int submeshIndex = 0)
        {
            if (objName.Contains("tamandua"))
                return GetOrCreateTexturedMaterial("Mat_Tamandua_Bandeira", Color.white, "Assets/Dados/Texturas/Textura_base_Tamandua.png");
            if (objName.Contains("coelho") || objName.Contains("tapeti"))
                return GetOrCreateTexturedMaterial("Mat_Coelho_Tapeti", Color.white, "Assets/Dados/Texturas/coelho_textura.png");
            if (objName.Contains("ema"))
                return GetOrCreateTexturedMaterial("Mat_Ema", Color.white, "Assets/Dados/Texturas/FBX_Ema_0.png");
            if (objName.Contains("lobo"))
                return GetOrCreateTexturedMaterial("Mat_Lobo_Guara", Color.white, "Assets/Dados/Texturas/FBX_loboguara_0.png", "Assets/Dados/Texturas/FBX_loboguara_1.png");
            if (objName.Contains("jacare"))
                return GetOrCreateTexturedMaterial("Mat_Jacare_Papo_Amarelo", Color.white, "Assets/Dados/Texturas/FBX_JacarePapoAmarelo_0.png", "Assets/Dados/Texturas/FBX_JacarePapoAmarelo_1.png");
            if (objName.Contains("onca") || objName.Contains("onça"))
                return GetOrCreateTexturedMaterial("Mat_Onca_Parda", new Color(0.95f, 0.89f, 0.78f), "Assets/Dados/Texturas/BaseColorOnca.jpg", null, false, 4f);
            if (objName.Contains("tatu"))
                return GetOrCreateTexturedMaterial("Mat_Tatu_Peba", Color.white, "Assets/Dados/Texturas/FBX_TatuPeba_0.png");
            if (objName.Contains("chuveirinho"))
            {
                if (submeshIndex == 0)
                    return GetOrCreateTexturedMaterial("Mat_Chuveirinho_Caule", Color.white, "Assets/Dados/Texturas/FBX_chuveirinho_0.png", "Assets/Dados/Texturas/FBX_chuveirinho_1.jpg", true);
                return GetOrCreateTexturedMaterial("Mat_Chuveirinho_Flor", Color.white, "Assets/Dados/Texturas/FBX_chuveirinho_2.png", null, true);
            }
            if (objName.Contains("arvoreseca") || objName.Contains("arvore_seca"))
                return GetOrCreateTexturedMaterial("Mat_ArvoreSeca_Casca", Color.white, "Assets/Dados/Texturas/FBX_ArvoreSeca_0.png", "Assets/Dados/Texturas/FBX_ArvoreSeca_1.jpg");
            if (objName.Contains("jatoba"))
            {
                if (submeshIndex == 0)
                    return GetOrCreateTexturedMaterial("Mat_Jatoba_Casca", Color.white, "Assets/Dados/Texturas/Blend_jatoba_remake_0.png", "Assets/Dados/Texturas/Blend_jatoba_remake_1.png");
                return GetOrCreateTexturedMaterial("Mat_Jatoba_Folhas", Color.white, "Assets/Dados/Texturas/Blend_jatoba_remake_2.png", "Assets/Dados/Texturas/Blend_jatoba_remake_3.png", true);
            }
            if (objName.Contains("angico"))
            {
                if (submeshIndex == 0)
                    return GetOrCreateTexturedMaterial("Mat_Angico_Casca", Color.white, "Assets/Dados/Texturas/Blend_angico_remake_0.png", "Assets/Dados/Texturas/Blend_angico_remake_1.png");
                return GetOrCreateTexturedMaterial("Mat_Angico_Folhas", Color.white, "Assets/Dados/Texturas/Blend_angico_remake_2.png", "Assets/Dados/Texturas/Blend_angico_remake_3.png", true);
            }
            if (objName.Contains("aroeira"))
            {
                if (submeshIndex == 0)
                    return GetOrCreateTexturedMaterial("Mat_Aroeira_Casca", Color.white, "Assets/Dados/Texturas/Blend_aroeira_remake_0.png", "Assets/Dados/Texturas/Blend_aroeira_remake_1.png");
                return GetOrCreateTexturedMaterial("Mat_Aroeira_Folhas", Color.white, "Assets/Dados/Texturas/Blend_aroeira_remake_3.png", "Assets/Dados/Texturas/Blend_aroeira_remake_4.png", true);
            }
            if (objName.Contains("grass") || objName.Contains("grama"))
            {
                Material capim = AssetDatabase.LoadAssetAtPath<Material>("Assets/Dados/Materiais/Mat_Capim_Cerrado.mat");
                return capim != null ? capim : GetOrCreateMaterial("Mat_Capim_Cerrado", new Color(0.75f, 0.65f, 0.35f));
            }
            if (objName.Contains("bush") || objName.Contains("arbusto") || objName.Contains("heather") || objName.Contains("fern") || objName.Contains("plant"))
            {
                Material arbusto = AssetDatabase.LoadAssetAtPath<Material>("Assets/Dados/Materiais/Mat_Arbusto_Cerrado.mat");
                return arbusto != null ? arbusto : GetOrCreateMaterial("Mat_Arbusto_Cerrado", new Color(0.45f, 0.52f, 0.25f));
            }

            return GetOrCreateMaterial("Mat_Elemento_Cerrado", new Color(0.55f, 0.50f, 0.40f));
        }

        public static void ApplyAnimalMaterial(GameObject root, string matName, Color tint, string diffuseTexPath, string normalTexPath = null)
        {
            if (root == null) return;
            Material mat = GetOrCreateTexturedMaterial(matName, tint, diffuseTexPath, normalTexPath, false);

            var renderers = root.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                if (r is ParticleSystemRenderer) continue;
                int count = Mathf.Max(1, r.sharedMaterials != null ? r.sharedMaterials.Length : 1);
                Material[] newMats = new Material[count];
                for (int i = 0; i < count; i++)
                {
                    newMats[i] = mat;
                }
                r.sharedMaterials = newMats;
                EditorUtility.SetDirty(r);
            }
        }

        public static void ApplyPlantMultiMaterials(GameObject root, Material trunkOrStemMat, Material foliageOrFlowerMat)
        {
            if (root == null) return;
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                if (r is ParticleSystemRenderer) continue;
                var oldMats = r.sharedMaterials;
                int count = (oldMats != null && oldMats.Length > 0) ? oldMats.Length : 1;
                Material[] newMats = new Material[count];

                for (int i = 0; i < count; i++)
                {
                    if (foliageOrFlowerMat == null)
                    {
                        newMats[i] = trunkOrStemMat;
                    }
                    else if (count == 1)
                    {
                        string rName = r.gameObject.name.ToLower();
                        bool isLeafObj = rName.Contains("leaf") || rName.Contains("leaves") || rName.Contains("folha") || rName.Contains("copa") || rName.Contains("flower") || rName.Contains("flor");
                        newMats[i] = isLeafObj ? foliageOrFlowerMat : trunkOrStemMat;
                    }
                    else
                    {
                        string mName = (oldMats[i] != null ? oldMats[i].name : "").ToLower();
                        bool isFoliageSlot = (i > 0) || mName.Contains("leaf") || mName.Contains("leaves") || mName.Contains("folha") || mName.Contains("branch") || mName.Contains("flower") || mName.Contains("flor");
                        newMats[i] = isFoliageSlot ? foliageOrFlowerMat : trunkOrStemMat;
                    }
                }

                r.sharedMaterials = newMats;
                EditorUtility.SetDirty(r);
            }
        }

        private static T GetOrAddComponent<T>(GameObject obj) where T : Component
        {
            if (obj == null) return null;
            T existing = obj.GetComponent<T>();
            if (existing != null)
            {
                return existing;
            }
            return obj.AddComponent<T>();
        }

        public static Material GetOrCreateTexturedMaterial(string name, Color tint, string diffusePath, string normalPath = null, bool alphaClip = false, float tileScale = 1f)
        {
            Material mat = GetOrCreateMaterial(name, tint);
            Texture2D diff = !string.IsNullOrEmpty(diffusePath) ? AssetDatabase.LoadAssetAtPath<Texture2D>(diffusePath) : null;
            Texture2D norm = !string.IsNullOrEmpty(normalPath) ? AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath) : null;

            if (diff != null)
            {
                mat.mainTexture = diff;
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", diff);
                if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", diff);
            }

            Vector2 scaleVec = new Vector2(tileScale, tileScale);
            mat.mainTextureScale = scaleVec;
            if (mat.HasProperty("_BaseMap")) mat.SetTextureScale("_BaseMap", scaleVec);
            if (mat.HasProperty("_MainTex")) mat.SetTextureScale("_MainTex", scaleVec);
            if (mat.HasProperty("_BumpMap")) mat.SetTextureScale("_BumpMap", scaleVec);
            if (mat.HasProperty("_BaseMap_ST")) mat.SetVector("_BaseMap_ST", new Vector4(tileScale, tileScale, 0f, 0f));

            if (norm != null && mat.HasProperty("_BumpMap"))
            {
                mat.SetTexture("_BumpMap", norm);
                if (mat.HasProperty("_BumpScale")) mat.SetFloat("_BumpScale", 1.15f);
                mat.EnableKeyword("_NORMALMAP");
            }

            if (alphaClip)
            {
                if (mat.HasProperty("_AlphaClip")) mat.SetFloat("_AlphaClip", 1f);
                if (mat.HasProperty("_Cutoff")) mat.SetFloat("_Cutoff", 0.35f);
                if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", 0f);
                mat.EnableKeyword("_ALPHATEST_ON");
                mat.renderQueue = 2450;
            }
            else
            {
                if (mat.HasProperty("_AlphaClip")) mat.SetFloat("_AlphaClip", 0f);
                if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", 2f);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.renderQueue = -1;
            }

            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.12f);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material GetOrCreateMaterial(string name, Color color)
        {
            string dir = "Assets/Dados/Materiais";
            if (!AssetDatabase.IsValidFolder("Assets/Dados/Materiais"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Dados")) AssetDatabase.CreateFolder("Assets", "Dados");
                AssetDatabase.CreateFolder("Assets/Dados", "Materiais");
            }

            string path = $"{dir}/{name}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                mat = new Material(shader);
                mat.name = name;
                mat.color = color;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.12f);
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.color = color;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                EditorUtility.SetDirty(mat);
            }
            return mat;
        }

        private static Collider activeTerrainCollider;
        private static readonly List<(Vector3 a, Vector3 b)> terrainGrassDirtBoundaryEdges = new List<(Vector3 a, Vector3 b)>();

        /// <summary>
        /// Substitui o plano placeholder antigo pelo modelo 3D definitivo "terreno_NOVO" dentro de "--- TERRENO_PLACEHOLDER ---",
        /// aplicando materiais URP Lit PBR (Poliigon Grass + NormalMap e GroundDirtWeeds), MeshCollider e NavMesh sobre o relevo.
        /// </summary>
        private static GameObject SetupTerrainPlaceholder()
        {
            activeTerrainCollider = null;

            GameObject oldPlaceholder = GameObject.Find("--- TERRENO_PLACEHOLDER ---");
            if (oldPlaceholder != null)
            {
                var rootMr = oldPlaceholder.GetComponent<MeshRenderer>();
                if (rootMr != null) Object.DestroyImmediate(rootMr);
                var rootMf = oldPlaceholder.GetComponent<MeshFilter>();
                if (rootMf != null) Object.DestroyImmediate(rootMf);
                var rootMc = oldPlaceholder.GetComponent<Collider>();
                if (rootMc != null) Object.DestroyImmediate(rootMc);
            }
            else
            {
                oldPlaceholder = new GameObject("--- TERRENO_PLACEHOLDER ---");
            }
            oldPlaceholder.transform.position = Vector3.zero;
            oldPlaceholder.transform.rotation = Quaternion.identity;
            oldPlaceholder.transform.localScale = Vector3.one;

            // Desativa qualquer Terrain antigo do Unity e move para o slot inativo
            var existingTerrains = Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include);
            if (existingTerrains.Length > 0)
            {
                GameObject wipSlot = GetOrCreateChild(oldPlaceholder.transform, "Terreno_Definitivo_WIP_Encaixar_Aqui");
                foreach (var t in existingTerrains)
                {
                    if (t != null && t.gameObject != null)
                    {
                        t.transform.SetParent(wipSlot.transform, true);
                        t.gameObject.SetActive(false);
                    }
                }
                wipSlot.SetActive(false);
            }

            // Remove completamente o plano antigo e instâncias anteriores para não haver Z-fighting
            var allTransforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);
            foreach (var tr in allTransforms)
            {
                if (tr != null && (tr.name == "Chao_Cerrado" || tr.name == "Chao_Cerrado_Superficie_Unica" || tr.name == "Chao_Cerrado_Terreno_NOVO"))
                {
                    Object.DestroyImmediate(tr.gameObject);
                }
            }

            // Configura filtro anisotrópico alto e NormalMap nas texturas do terreno_NOVO para máxima nitidez
            string[] terrainTexPaths = new[]
            {
                "Assets/Dados/Texturas/Poliigon_GrassPatchyGround_4585_BaseColor.jpg",
                "Assets/Dados/Texturas/Poliigon_GrassPatchyGround_4585_Normal.png",
                "Assets/Dados/Texturas/GroundDirtWeedsPatchy004_COL_2K.jpg"
            };
            foreach (var tp in terrainTexPaths)
            {
                var imp = AssetImporter.GetAtPath(tp) as TextureImporter;
                if (imp != null)
                {
                    bool isNorm = tp.Contains("Normal");
                    TextureImporterType targetType = isNorm ? TextureImporterType.NormalMap : TextureImporterType.Default;
                    if (imp.textureType != targetType || imp.anisoLevel != 9 || imp.wrapMode != TextureWrapMode.Repeat)
                    {
                        imp.textureType = targetType;
                        imp.wrapMode = TextureWrapMode.Repeat;
                        imp.filterMode = FilterMode.Trilinear;
                        imp.anisoLevel = 9;
                        imp.maxTextureSize = 2048;
                        imp.SaveAndReimport();
                    }
                }
            }

            // Materiais URP Lit PBR para os 2 slots do terreno_NOVO (Tiling 88x88 -> ~1 tile a cada 1.9m com micro-relevo visível)
            // Slot 0: Grama/Capim dourado-esverdeado vivo do Cerrado, Smoothness baixo (0.07) sem brilho plástico
            Material grassMat = GetOrCreateTexturedMaterial(
                "Mat_Terreno_Grama_Poliigon",
                new Color(0.95f, 0.88f, 0.62f),
                "Assets/Dados/Texturas/Poliigon_GrassPatchyGround_4585_BaseColor.jpg",
                "Assets/Dados/Texturas/Poliigon_GrassPatchyGround_4585_Normal.png",
                false,
                88f
            );
            if (grassMat.HasProperty("_Smoothness")) grassMat.SetFloat("_Smoothness", 0.07f);
            if (grassMat.HasProperty("_BumpScale")) grassMat.SetFloat("_BumpScale", 1.55f);
            EditorUtility.SetDirty(grassMat);

            // Slot 1: Terra seca/avermelhada-ocre árida do Cerrado com textura rugosa, Smoothness baixo (0.06)
            Material dirtMat = GetOrCreateTexturedMaterial(
                "Mat_Terreno_Terra_Trilha",
                new Color(0.84f, 0.64f, 0.45f),
                "Assets/Dados/Texturas/GroundDirtWeedsPatchy004_COL_2K.jpg",
                "Assets/Dados/Texturas/Poliigon_GrassPatchyGround_4585_Normal.png",
                false,
                88f
            );
            if (dirtMat.HasProperty("_Smoothness")) dirtMat.SetFloat("_Smoothness", 0.06f);
            if (dirtMat.HasProperty("_BumpScale")) dirtMat.SetFloat("_BumpScale", 1.25f);
            EditorUtility.SetDirty(dirtMat);

            // Cria material orgânico URP de World-Space Noise Blend entre Grama e Terra Seca (elimina linha reta entre faces!)
            Material blendedTerrainMat = GetOrCreateOrganicTerrainBlendMaterial();
            terrainGrassDirtBoundaryEdges.Clear();

            GameObject terrainPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Modelos/Terreno/terreno_NOVO.fbx");
            if (terrainPrefab == null)
            {
                terrainPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Modelos/Terreno/terreno_NOVO.blend");
            }

            GameObject ground;
            if (terrainPrefab != null)
            {
                ground = (GameObject)PrefabUtility.InstantiatePrefab(terrainPrefab, oldPlaceholder.transform);
                ground.name = "Chao_Cerrado_Terreno_NOVO";
                ground.transform.localPosition = Vector3.zero;
                ground.transform.localRotation = Quaternion.identity;
                ground.transform.localScale = Vector3.one;
                ground.isStatic = true;

                // 1. Clona a malha do terreno, detecta as arestas exatas entre submesh de grama (0) e terra (>=1),
                //    e grava gradiente suave de distância na Vertex Color R (0=grama, 0.5=fronteira, 1=terra)
                var filters = ground.GetComponentsInChildren<MeshFilter>(true);
                foreach (var mf in filters)
                {
                    if (mf != null && mf.sharedMesh != null)
                    {
                        Mesh blendedMesh = BuildBlendedTerrainMeshAndDetectBoundary(mf);
                        if (blendedMesh != null)
                        {
                            mf.sharedMesh = blendedMesh;
                            EditorUtility.SetDirty(mf);
                        }

                        var mc = GetOrAddComponent<MeshCollider>(mf.gameObject);
                        mc.convex = false;
                        mc.sharedMesh = mf.sharedMesh;
                        activeTerrainCollider = mc;
                    }
                }

                // 2. Aplica o material único de mistura orgânica World-Space em todos os slots do terreno
                var renderers = ground.GetComponentsInChildren<MeshRenderer>(true);
                foreach (var r in renderers)
                {
                    r.gameObject.isStatic = true;
                    int slotCount = (r.sharedMaterials != null && r.sharedMaterials.Length > 1) ? r.sharedMaterials.Length : 1;
                    Material[] mats = new Material[slotCount];
                    for (int i = 0; i < slotCount; i++)
                    {
                        mats[i] = blendedTerrainMat != null ? blendedTerrainMat : (i == 0 ? grassMat : dirtMat);
                    }
                    r.sharedMaterials = mats;
                    EditorUtility.SetDirty(r);
                }
            }
            else
            {
                ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = "Chao_Cerrado_Terreno_NOVO";
                ground.transform.SetParent(oldPlaceholder.transform, false);
                ground.transform.localPosition = Vector3.zero;
                ground.transform.localRotation = Quaternion.identity;
                ground.transform.localScale = new Vector3(17f, 1f, 17f);
                ground.isStatic = true;
                var mr = ground.GetComponent<MeshRenderer>();
                if (mr != null) mr.sharedMaterial = blendedTerrainMat != null ? blendedTerrainMat : grassMat;
                activeTerrainCollider = ground.GetComponent<Collider>();
            }

            Physics.SyncTransforms();
            BakePlaceholderNavMesh(ground);

            return ground;
        }

        private static Material GetOrCreateOrganicTerrainBlendMaterial()
        {
            Shader blendShader = Shader.Find("Universal Render Pipeline/Cerrado/TerrainOrganicBlend");
            if (blendShader == null)
            {
                Debug.LogWarning("[CerradoSceneSetup] Shader 'Universal Render Pipeline/Cerrado/TerrainOrganicBlend' ainda não encontrado; usando fallback URP Lit.");
                return null;
            }

            if (!AssetDatabase.IsValidFolder("Assets/Dados/Materiais"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Dados")) AssetDatabase.CreateFolder("Assets", "Dados");
                AssetDatabase.CreateFolder("Assets/Dados", "Materiais");
            }

            const string matPath = "Assets/Dados/Materiais/Mat_Terreno_Cerrado_Blend.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(blendShader) { name = "Mat_Terreno_Cerrado_Blend" };
                AssetDatabase.CreateAsset(mat, matPath);
            }
            else if (mat.shader != blendShader)
            {
                mat.shader = blendShader;
            }

            Texture2D grassTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Dados/Texturas/Poliigon_GrassPatchyGround_4585_BaseColor.jpg");
            Texture2D dirtTex  = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Dados/Texturas/GroundDirtWeedsPatchy004_COL_2K.jpg");
            Texture2D normTex  = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Dados/Texturas/Poliigon_GrassPatchyGround_4585_Normal.png");

            if (grassTex != null) mat.SetTexture("_GrassMap", grassTex);
            if (dirtTex != null) mat.SetTexture("_DirtMap", dirtTex);
            if (normTex != null) mat.SetTexture("_BumpMap", normTex);

            mat.SetColor("_GrassColor", new Color(0.93f, 0.86f, 0.60f, 1f));
            mat.SetColor("_DirtColor", new Color(0.85f, 0.66f, 0.47f, 1f));
            mat.SetFloat("_BumpScale", 1.25f);
            mat.SetFloat("_WorldTiling", 0.52f);
            mat.SetFloat("_Smoothness", 0.07f);
            mat.SetFloat("_NoiseScaleMacro", 0.14f);
            mat.SetFloat("_NoiseScaleMicro", 0.52f);
            mat.SetFloat("_NoiseStrength", 0.34f);
            mat.SetFloat("_EdgeSoftness", 0.28f);
            mat.SetFloat("_PatchVariation", 0.18f);
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Vector3Int QuantizeTerrainPos(Vector3 p)
        {
            return new Vector3Int(Mathf.RoundToInt(p.x * 50f), Mathf.RoundToInt(p.y * 50f), Mathf.RoundToInt(p.z * 50f));
        }

        private static float DistancePointToSegmentXZ(Vector3 p, Vector3 a, Vector3 b)
        {
            float px = p.x - a.x, pz = p.z - a.z;
            float dx = b.x - a.x, dz = b.z - a.z;
            float lenSq = dx * dx + dz * dz;
            if (lenSq < 1e-6f) return Mathf.Sqrt(px * px + pz * pz);
            float t = Mathf.Clamp01((px * dx + pz * dz) / lenSq);
            float cx = a.x + t * dx - p.x;
            float cz = a.z + t * dz - p.z;
            return Mathf.Sqrt(cx * cx + cz * cz);
        }

        private static Mesh BuildBlendedTerrainMeshAndDetectBoundary(MeshFilter mf)
        {
            Mesh src = mf.sharedMesh;
            if (src == null) return null;

            Vector3[] verts = src.vertices;
            int vCount = verts.Length;
            if (vCount == 0) return src;

            Matrix4x4 l2w = mf.transform.localToWorldMatrix;
            Vector3[] worldVerts = new Vector3[vCount];
            var qKeys = new Vector3Int[vCount];
            for (int i = 0; i < vCount; i++)
            {
                worldVerts[i] = l2w.MultiplyPoint3x4(verts[i]);
                qKeys[i] = QuantizeTerrainPos(worldVerts[i]);
            }

            var grassKeys = new HashSet<Vector3Int>();
            var dirtKeys = new HashSet<Vector3Int>();
            var grassEdges = new HashSet<(Vector3Int, Vector3Int)>();
            var dirtEdges = new HashSet<(Vector3Int, Vector3Int)>();
            var keyToWorld = new Dictionary<Vector3Int, Vector3>();

            for (int i = 0; i < vCount; i++)
            {
                if (!keyToWorld.ContainsKey(qKeys[i])) keyToWorld[qKeys[i]] = worldVerts[i];
            }

            (Vector3Int, Vector3Int) MakeEdge(Vector3Int u, Vector3Int v)
            {
                if (u.x != v.x) return u.x < v.x ? (u, v) : (v, u);
                if (u.z != v.z) return u.z < v.z ? (u, v) : (v, u);
                return u.y <= v.y ? (u, v) : (v, u);
            }

            int subCount = src.subMeshCount;
            for (int s = 0; s < subCount; s++)
            {
                int[] tris = src.GetTriangles(s);
                bool isDirt = s >= 1;
                var vertSet = isDirt ? dirtKeys : grassKeys;
                var edgeSet = isDirt ? dirtEdges : grassEdges;

                for (int t = 0; t + 2 < tris.Length; t += 3)
                {
                    Vector3Int k0 = qKeys[tris[t]];
                    Vector3Int k1 = qKeys[tris[t + 1]];
                    Vector3Int k2 = qKeys[tris[t + 2]];
                    vertSet.Add(k0);
                    vertSet.Add(k1);
                    vertSet.Add(k2);
                    edgeSet.Add(MakeEdge(k0, k1));
                    edgeSet.Add(MakeEdge(k1, k2));
                    edgeSet.Add(MakeEdge(k2, k0));
                }
            }

            var localBoundaryEdges = new List<(Vector3 a, Vector3 b)>();
            foreach (var edge in grassEdges)
            {
                if (dirtEdges.Contains(edge))
                {
                    localBoundaryEdges.Add((keyToWorld[edge.Item1], keyToWorld[edge.Item2]));
                }
            }

            // Fallback caso a malha tenha arestas adjacentes com vértices próximos sem coincidir exatamente
            if (localBoundaryEdges.Count == 0 && grassKeys.Count > 0 && dirtKeys.Count > 0)
            {
                var sharedKeys = new List<Vector3Int>();
                foreach (var k in grassKeys)
                {
                    if (dirtKeys.Contains(k)) sharedKeys.Add(k);
                }
                foreach (var e in grassEdges)
                {
                    if (dirtKeys.Contains(e.Item1) && dirtKeys.Contains(e.Item2))
                    {
                        localBoundaryEdges.Add((keyToWorld[e.Item1], keyToWorld[e.Item2]));
                    }
                }
            }

            terrainGrassDirtBoundaryEdges.AddRange(localBoundaryEdges);

            // Calcula peso orgânico suave por vértice baseado na distância real até a linha divisória (largura total ~8.4m)
            const float blendHalfWidth = 4.2f;
            Color[] colors = new Color[vCount];
            for (int i = 0; i < vCount; i++)
            {
                Vector3Int k = qKeys[i];
                bool inGrass = grassKeys.Contains(k);
                bool inDirt = dirtKeys.Contains(k);

                float w;
                if (localBoundaryEdges.Count == 0)
                {
                    w = inDirt ? 1f : 0f;
                }
                else if (inGrass && inDirt)
                {
                    w = 0.5f;
                }
                else
                {
                    Vector3 wp = worldVerts[i];
                    float minDist = float.MaxValue;
                    for (int e = 0; e < localBoundaryEdges.Count; e++)
                    {
                        float d = DistancePointToSegmentXZ(wp, localBoundaryEdges[e].a, localBoundaryEdges[e].b);
                        if (d < minDist) minDist = d;
                    }
                    float sign = inDirt ? 1f : -1f;
                    float t = Mathf.Clamp01(0.5f + sign * (minDist / blendHalfWidth));
                    w = t * t * (3f - 2f * t);
                }

                colors[i] = new Color(w, 0f, 0f, 1f);
            }

            Mesh blended = Object.Instantiate(src);
            blended.name = "Terreno_NOVO_Blended";
            blended.colors = colors;
            if (blended.tangents == null || blended.tangents.Length != vCount)
            {
                blended.RecalculateTangents();
            }

            const string blendedAssetPath = "Assets/Modelos/Terreno/Terreno_NOVO_Blended.asset";
            if (AssetDatabase.LoadAssetAtPath<Mesh>(blendedAssetPath) != null)
            {
                AssetDatabase.DeleteAsset(blendedAssetPath);
            }
            AssetDatabase.CreateAsset(blended, blendedAssetPath);
            AssetDatabase.SaveAssets();

            Debug.Log($"[CerradoSceneSetup] Terreno orgânico gerado: {vCount} vértices suavizados e {localBoundaryEdges.Count} arestas de fronteira terra/grama detectadas.");
            return AssetDatabase.LoadAssetAtPath<Mesh>(blendedAssetPath);
        }

        private static void BakePlaceholderNavMesh(GameObject groundObj)
        {
            NavMesh.RemoveAllNavMeshData();

            var sources = new List<NavMeshBuildSource>();
            var meshFilters = groundObj.GetComponentsInChildren<MeshFilter>(true);
            foreach (var mf in meshFilters)
            {
                if (mf != null && mf.sharedMesh != null)
                {
                    var src = new NavMeshBuildSource
                    {
                        shape = NavMeshBuildSourceShape.Mesh,
                        sourceObject = mf.sharedMesh,
                        transform = mf.transform.localToWorldMatrix,
                        area = 0
                    };
                    sources.Add(src);
                }
            }

            NavMeshBuildSettings buildSettings = NavMesh.GetSettingsByID(0);
            buildSettings.agentSlope = 46f; // Max Slope ~45-46° para os animais subirem e descerem os morros do terreno_NOVO sem travar
            buildSettings.agentClimb = 0.5f;
            // Volume de bake estendido além da cerca para os animais atravessarem até o cinturão de árvores (126x120m)
            Bounds localBounds = new Bounds(new Vector3(0f, 4f, 6.5f), new Vector3(126f, 28f, 120f));
            NavMeshData navData = NavMeshBuilder.BuildNavMeshData(
                buildSettings,
                sources,
                localBounds,
                Vector3.zero,
                Quaternion.identity
            );

            if (navData != null)
            {
                string navAssetPath = "Assets/Scenes/SampleScene_NavMesh.asset";
                var existing = AssetDatabase.LoadAssetAtPath<NavMeshData>(navAssetPath);
                if (existing != null)
                {
                    AssetDatabase.DeleteAsset(navAssetPath);
                }
                AssetDatabase.CreateAsset(navData, navAssetPath);
                AssetDatabase.SaveAssets();
                NavMesh.AddNavMeshData(navData);

                // Persiste m_NavMeshData no NavMeshSettings da cena para funcionar em Play Mode e na Build .exe
#pragma warning disable CS0618
                if (UnityEditor.AI.NavMeshBuilder.navMeshSettingsObject != null)
                {
                    var soNavSettings = new SerializedObject(UnityEditor.AI.NavMeshBuilder.navMeshSettingsObject);
                    var propNavData = soNavSettings.FindProperty("m_NavMeshData");
                    if (propNavData != null)
                    {
                        propNavData.objectReferenceValue = navData;
                        soNavSettings.ApplyModifiedPropertiesWithoutUndo();
                    }
                }
#pragma warning restore CS0618
            }
        }

        private static Light SetupLighting()
        {
            Material skyMat = CerradoMigrationEditor.SetupEveningSkyboxMaterial();
            if (skyMat != null)
            {
                RenderSettings.skybox = skyMat;
            }

            Light sun = RenderSettings.sun;
            if (sun == null)
            {
                var lightObj = GameObject.Find("Directional Light");
                if (lightObj != null) sun = lightObj.GetComponent<Light>();
            }

            if (sun == null)
            {
                GameObject sunObj = new GameObject("Directional Light");
                sun = sunObj.AddComponent<Light>();
                sun.type = LightType.Directional;
            }

            // Iluminação quente de fim de tarde do Cerrado harmonizada com o HDRI evening_field_8k
            sun.intensity = 1.35f;
            sun.color = new Color(1.0f, 0.80f, 0.56f);
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(24f, -38f, 0f);
            RenderSettings.sun = sun;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.60f, 0.66f, 0.80f);
            RenderSettings.ambientEquatorColor = new Color(0.86f, 0.72f, 0.52f);
            RenderSettings.ambientGroundColor = new Color(0.40f, 0.31f, 0.21f);

            // Fog exponencial suave
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = 0.0025f;
            RenderSettings.fogColor = new Color(0.76f, 0.67f, 0.55f);

            // Garante Global Volume URP com SampleSceneProfile (ACES, Bloom sutil, Vignette, Motion Blur DESLIGADO)
            GameObject volObj = GameObject.Find("Global Volume");
            if (volObj == null)
            {
                volObj = new GameObject("Global Volume");
            }
            var volume = GetOrAddComponent<Volume>(volObj);
            volume.isGlobal = true;
            volume.priority = 1f;
            volume.weight = 1f;
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Settings/SampleSceneProfile.asset");
            if (profile != null)
            {
                volume.sharedProfile = profile;
            }

            GameObject ambObj = GameObject.Find("_Ambiente");
            if (ambObj == null) ambObj = new GameObject("_Ambiente");
            var atm = GetOrAddComponent<CerradoAtmosphere>(ambObj);
            atm.SetupMistParticleSystem();
            GetOrAddComponent<TerrainTreeToggle>(ambObj);

            // ReflectionProbe em tempo real (OnAwake) cobrindo a reserva para reflexos naturais nas copas, olhos e superfícies
            GameObject reflObj = GetOrCreateChild(ambObj.transform, "ReflectionProbe_Cerrado");
            reflObj.transform.position = new Vector3(0f, SampleGroundHeight(Vector3.zero) + 6f, 6f);
            var reflProbe = GetOrAddComponent<ReflectionProbe>(reflObj);
            reflProbe.mode = UnityEngine.Rendering.ReflectionProbeMode.Realtime;
            reflProbe.refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.OnAwake;
            reflProbe.timeSlicingMode = UnityEngine.Rendering.ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
            reflProbe.resolution = 256;
            reflProbe.size = new Vector3(140f, 55f, 140f);
            reflProbe.intensity = 1.0f;
            reflProbe.boxProjection = false;

            // LightProbeGroup distribuído pela savana para iluminação indireta volumétrica suave nos animais e no Biólogo Guia
            GameObject lpgObj = GetOrCreateChild(ambObj.transform, "LightProbeGroup_Cerrado");
            lpgObj.transform.position = Vector3.zero;
            var lpg = GetOrAddComponent<LightProbeGroup>(lpgObj);
            var probePositions = new List<Vector3>();
            float[] gridX = { -32f, -16f, 0f, 16f, 32f };
            float[] gridZ = { -22f, -6f, 8f, 22f, 36f };
            foreach (float px in gridX)
            {
                foreach (float pz in gridZ)
                {
                    float gy = SampleGroundHeight(new Vector3(px, 0f, pz));
                    probePositions.Add(new Vector3(px, gy + 0.8f, pz));
                    probePositions.Add(new Vector3(px, gy + 3.2f, pz));
                }
            }
            lpg.probePositions = probePositions.ToArray();

            return sun;
        }

        private static void SetupManagers(Light sun)
        {
            GameObject managers = GameObject.Find("_Managers");
            if (managers == null)
            {
                managers = new GameObject("_Managers");
            }

            var albumManager = GetOrAddComponent<AlbumManager>(managers);
            var speciesAssets = new List<SpeciesData>();
            string[] speciesGuids = AssetDatabase.FindAssets("t:SpeciesData", new[] { "Assets/Dados/Especies" });
            foreach (var guid in speciesGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<SpeciesData>(path);
                if (asset != null) speciesAssets.Add(asset);
            }
            var soAlbum = new SerializedObject(albumManager);
            var propSpecies = soAlbum.FindProperty("allRegisteredSpecies");
            propSpecies.ClearArray();
            for (int i = 0; i < speciesAssets.Count; i++)
            {
                propSpecies.InsertArrayElementAtIndex(i);
                propSpecies.GetArrayElementAtIndex(i).objectReferenceValue = speciesAssets[i];
            }
            soAlbum.ApplyModifiedProperties();

            var equipManager = GetOrAddComponent<EquipmentManager>(managers);
            var shopAssets = new List<ShopItemData>();
            string[] shopGuids = AssetDatabase.FindAssets("t:ShopItemData", new[] { "Assets/Dados/Loja" });
            foreach (var guid in shopGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<ShopItemData>(path);
                if (asset != null) shopAssets.Add(asset);
            }
            var soEquip = new SerializedObject(equipManager);
            var propShop = soEquip.FindProperty("allAvailableEquipment");
            propShop.ClearArray();
            for (int i = 0; i < shopAssets.Count; i++)
            {
                propShop.InsertArrayElementAtIndex(i);
                propShop.GetArrayElementAtIndex(i).objectReferenceValue = shopAssets[i];
            }
            soEquip.ApplyModifiedProperties();

            var questManager = GetOrAddComponent<QuestManager>(managers);
            var questAssets = new List<QuestData>();
            string[] questGuids = AssetDatabase.FindAssets("t:QuestData", new[] { "Assets/Dados/Missoes" });
            foreach (var guid in questGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<QuestData>(path);
                if (asset != null) questAssets.Add(asset);
            }
            var soQuest = new SerializedObject(questManager);
            var propQuests = soQuest.FindProperty("allQuests");
            propQuests.ClearArray();
            for (int i = 0; i < questAssets.Count; i++)
            {
                propQuests.InsertArrayElementAtIndex(i);
                propQuests.GetArrayElementAtIndex(i).objectReferenceValue = questAssets[i];
            }
            soQuest.ApplyModifiedProperties();

            var dayNight = GetOrAddComponent<DayNightCycle>(managers);
            var soDayNight = new SerializedObject(dayNight);
            soDayNight.FindProperty("directionalLight").objectReferenceValue = sun;
            soDayNight.FindProperty("dayDurationInMinutes").floatValue = 10f;
            soDayNight.FindProperty("currentHour").floatValue = 17.0f;
            soDayNight.ApplyModifiedProperties();

            // Gerenciador de Áudio (Trilha regional Sons do Cerrado + Passos na terra + Virar página do Álbum)
            var audioMgr = GetOrAddComponent<CerradoAudioManager>(managers);
            var soAudio = new SerializedObject(audioMgr);
            AssignAudioClipArray(soAudio.FindProperty("ambientTracks"), "Assets/Dados/Audio/Musica");
            AssignAudioClipArray(soAudio.FindProperty("dirtFootsteps"), "Assets/Dados/Audio/Passos");
            AssignAudioClipArray(soAudio.FindProperty("pageFlipClips"), "Assets/Dados/Audio/UI");
            soAudio.ApplyModifiedProperties();
        }

        private static void AssignAudioClipArray(SerializedProperty prop, string folder)
        {
            if (prop == null || !AssetDatabase.IsValidFolder(folder)) return;
            prop.ClearArray();
            string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { folder });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip != null)
                {
                    int idx = prop.arraySize;
                    prop.InsertArrayElementAtIndex(idx);
                    prop.GetArrayElementAtIndex(idx).objectReferenceValue = clip;
                }
            }
        }

        private static GameObject SetupPlayer()
        {
            GameObject player = GameObject.Find("Player");
            if (player == null)
            {
                player = new GameObject("Player");
                player.tag = "Player";
            }
            float playerGroundY = SampleGroundHeight(Vector3.zero);
            player.transform.position = new Vector3(0f, playerGroundY + 0.12f, 0f);
            player.transform.rotation = Quaternion.identity;

            var cc = GetOrAddComponent<CharacterController>(player);
            cc.height = 1.8f;
            cc.radius = 0.4f;
            cc.center = new Vector3(0, 0.9f, 0);

            var playerController = GetOrAddComponent<PlayerController>(player);

            Camera cam = player.GetComponentInChildren<Camera>();
            if (cam == null)
            {
                var mainCamObj = GameObject.Find("Main Camera");
                if (mainCamObj != null)
                {
                    mainCamObj.transform.SetParent(player.transform);
                    cam = mainCamObj.GetComponent<Camera>();
                }
                else
                {
                    GameObject camObj = new GameObject("Main Camera");
                    camObj.transform.SetParent(player.transform);
                    cam = camObj.AddComponent<Camera>();
                    camObj.AddComponent<AudioListener>();
                }
            }

            // Garante sempre a câmera inicial nivelada em 0° olhando reto para o horizonte
            cam.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            cam.transform.localRotation = Quaternion.identity;

            cam.tag = "MainCamera";
            cam.nearClipPlane = 0.1f;

            var photoCam = GetOrAddComponent<PhotoCameraSystem>(player);

            var soPlayer = new SerializedObject(playerController);
            soPlayer.FindProperty("playerCamera").objectReferenceValue = cam;
            soPlayer.FindProperty("characterController").objectReferenceValue = cc;
            soPlayer.ApplyModifiedProperties();

            var soPhoto = new SerializedObject(photoCam);
            soPhoto.FindProperty("playerCamera").objectReferenceValue = cam;
            soPhoto.ApplyModifiedProperties();

            return player;
        }

        /// <summary>
        /// Substitui sempre qualquer NPC_Pesquisador antigo pelo modelo 3D real tamandua_npc.fbx
        /// com postura natural (sem T-pose), pés tocando o solo (baseOffset = 0) e movimentação ativa na NavMesh.
        /// </summary>
        private static GameObject SetupNPC(Vector3 position)
        {
            GameObject oldNpc = GameObject.Find("NPC_Pesquisador");
            if (oldNpc != null)
            {
                Object.DestroyImmediate(oldNpc);
            }

            string modelPath = "Assets/Modelos/NPC/Prefabs/PFB_Biologo_Guia.prefab";
            GameObject npcPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            bool isHumanBiologist = npcPrefab != null;
            if (npcPrefab == null)
            {
                modelPath = "Assets/Modelos/Fauna/tamandua_npc.fbx";
                npcPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            }

            float groundY = SampleGroundHeight(position);
            GameObject npcRoot = new GameObject("NPC_Pesquisador");
            npcRoot.transform.position = new Vector3(position.x, groundY, position.z);
            npcRoot.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            npcRoot.transform.localScale = Vector3.one;

            GameObject visualObj = (GameObject)PrefabUtility.InstantiatePrefab(npcPrefab, npcRoot.transform);
            visualObj.name = "Modelo_3D";
            if (!isHumanBiologist)
            {
                ApplyAnimalMaterial(visualObj, "Mat_Tamandua_Bandeira", Color.white, "Assets/Dados/Texturas/Textura_base_Tamandua.png");
            }

            NormalizeVisualChildTransform(npcRoot, visualObj, modelPath, 1.76f, 0.0f, false);

            var col = GetOrAddComponent<CapsuleCollider>(npcRoot);
            col.radius = 0.45f;
            col.height = 1.76f;
            col.center = new Vector3(0f, 0.88f, 0f);

            // Biólogo Prof. Silva fica na base recebendo o jogador, com animações CharacterIdle e TalkingOnThePhone
            Animator npcAnim = visualObj.GetComponentInChildren<Animator>(true);
            if (isHumanBiologist)
            {
                if (npcAnim == null) npcAnim = visualObj.AddComponent<Animator>();
                Avatar charAvatar = AssetDatabase.LoadAssetAtPath<Avatar>("Assets/Modelos/NPC/Migrados/CharacterAvatar.asset");
                if (charAvatar != null) npcAnim.avatar = charAvatar;
                npcAnim.applyRootMotion = false;

                AnimationClip idleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Modelos/NPC/Migrados/Animacoes/Character_Idle.anim");
                AnimationClip talkClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Modelos/NPC/Migrados/Animacoes/Character_Talk.anim");
                if (idleClip != null)
                {
                    var s = AnimationUtility.GetAnimationClipSettings(idleClip);
                    s.loopTime = true;
                    AnimationUtility.SetAnimationClipSettings(idleClip, s);
                }
                if (talkClip != null)
                {
                    var s = AnimationUtility.GetAnimationClipSettings(talkClip);
                    s.loopTime = true;
                    AnimationUtility.SetAnimationClipSettings(talkClip, s);
                }

                if (!AssetDatabase.IsValidFolder("Assets/Dados/Animacoes"))
                {
                    if (!AssetDatabase.IsValidFolder("Assets/Dados")) AssetDatabase.CreateFolder("Assets", "Dados");
                    AssetDatabase.CreateFolder("Assets/Dados", "Animacoes");
                }
                string ctrlPath = "Assets/Dados/Animacoes/AnimCtrl_Biologo_Guia.controller";
                if (AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(ctrlPath) != null)
                    AssetDatabase.DeleteAsset(ctrlPath);

                var ctrl = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
                ctrl.AddParameter("IsTalking", AnimatorControllerParameterType.Bool);
                var sm = ctrl.layers[0].stateMachine;
                var stIdle = sm.AddState("CharacterIdle");
                stIdle.motion = idleClip;
                sm.defaultState = stIdle;

                var stTalk = sm.AddState("TalkingOnThePhone");
                stTalk.motion = talkClip != null ? talkClip : idleClip;

                var toTalk = stIdle.AddTransition(stTalk);
                toTalk.hasExitTime = false;
                toTalk.duration = 0.25f;
                toTalk.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0f, "IsTalking");

                var toIdle = stTalk.AddTransition(stIdle);
                toIdle.hasExitTime = false;
                toIdle.duration = 0.25f;
                toIdle.AddCondition(UnityEditor.Animations.AnimatorConditionMode.IfNot, 0f, "IsTalking");

                EditorUtility.SetDirty(ctrl);
                npcAnim.runtimeAnimatorController = ctrl;
            }
            else
            {
                ConfigureAnimalAnimations(npcRoot, visualObj, "NPC_Pesquisador", modelPath);
            }

            GetOrAddComponent<ShopNPC>(npcRoot);
            var guide = GetOrAddComponent<GuideNPC>(npcRoot);
            if (npcAnim != null)
            {
                var soGuide = new SerializedObject(guide);
                soGuide.FindProperty("npcAnimator").objectReferenceValue = npcAnim;
                soGuide.ApplyModifiedProperties();
            }

            return npcRoot;
        }

        private static void SetupEntities()
        {
            GameObject entitiesGroup = GameObject.Find("_Entidades");
            if (entitiesGroup != null)
            {
                Object.DestroyImmediate(entitiesGroup);
            }
            entitiesGroup = new GameObject("_Entidades");

            const string animDir = "Assets/Modelos/Fauna/Migrados/Animacoes/";
            Random.InitState(2026);
            var occupiedSpots = new List<Vector2> { new Vector2(0f, 0f), new Vector2(0f, 4.2f) };

            Vector3 PickSpot(Vector3 preferred, float minDist, float maxRadiusX = 36f, float maxRadiusZMin = -27f, float maxRadiusZMax = 40f)
            {
                Vector2 pref2 = new Vector2(preferred.x, preferred.z);
                bool ok = true;
                foreach (var o in occupiedSpots)
                {
                    if (Vector2.Distance(o, pref2) < minDist) { ok = false; break; }
                }
                if (ok)
                {
                    occupiedSpots.Add(pref2);
                    return preferred;
                }
                for (int tries = 0; tries < 60; tries++)
                {
                    float rx = Random.Range(-maxRadiusX, maxRadiusX);
                    float rz = Random.Range(maxRadiusZMin, maxRadiusZMax);
                    Vector2 p = new Vector2(rx, rz);
                    if (p.magnitude < 5.5f || Vector2.Distance(p, new Vector2(0f, 4.2f)) < 4.5f) continue;
                    bool clear = true;
                    foreach (var o in occupiedSpots)
                    {
                        if (Vector2.Distance(o, p) < minDist) { clear = false; break; }
                    }
                    if (clear)
                    {
                        occupiedSpots.Add(p);
                        return new Vector3(rx, 0f, rz);
                    }
                }
                occupiedSpots.Add(pref2);
                return preferred;
            }

            void SpawnFaunaGroup(
                int count, string baseName, string modelPath, string speciesPath, Vector3 firstPos,
                AnimalTemperament temp, float walkSpd, float fleeSpd, float colRad, float colH, float targetMax,
                string idleClip, string walkClip, string runClip = null, float idleAnimSpeed = 1.0f, bool isOncaBlendShape = false)
            {
                for (int i = 0; i < count; i++)
                {
                    string instName = i == 0 ? baseName : $"{baseName}_{i + 1:00}";
                    Vector3 pos = i == 0
                        ? PickSpot(firstPos, 3.2f)
                        : PickSpot(new Vector3(Random.Range(-35f, 35f), 0f, Random.Range(-25f, 38f)), 3.8f);
                    SetupAnimal(
                        entitiesGroup.transform, instName, baseName, modelPath, speciesPath,
                        pos, temp, walkSpd, fleeSpd, colRad, colH, targetMax,
                        Color.white, null, null, idleClip, walkClip, runClip, idleAnimSpeed, isOncaBlendShape, reuseExistingController: i > 0
                    );
                }
            }

            void SpawnFloraGroup(
                int count, string baseName, string modelPath, string speciesPath, Vector3 firstPos,
                float height, Material m0 = null, Material m1 = null, bool isLowPlant = false)
            {
                for (int i = 0; i < count; i++)
                {
                    string instName = i == 0 ? baseName : $"{baseName}_{i + 1:00}";
                    Vector3 pos = i == 0
                        ? PickSpot(firstPos, isLowPlant ? 2.8f : 5.0f)
                        : PickSpot(new Vector3(Random.Range(-37f, 37f), 0f, Random.Range(-27f, 40f)), isLowPlant ? 3.2f : 5.5f);
                    // Variedade natural de portes para árvores: ~25% Pequenas/Mudas (0.45-0.70x), ~50% Médias (0.85-1.15x), ~25% Grandes/Centenárias (1.30-1.65x)
                    float h = isLowPlant
                        ? (i == 0 ? height : height * Random.Range(0.85f, 1.18f))
                        : height * SampleNaturalTreeScaleTier();
                    SetupPlant(entitiesGroup.transform, instName, modelPath, speciesPath, pos, h, m0, m1, isLowPlant);
                }
            }

            // =========================================================================
            // FAUNA COMPLETA DO CERRADO (18 Espécies — Comum: 3-4 exemplares | Acima de Comum: 2 exemplares)
            // =========================================================================

            // --- COMUNS (3 a 4 exemplares cada — velocidades sincronizadas com a cadência da animação) ---
            SpawnFaunaGroup(3, "Tamandua_Bandeira", "Assets/Modelos/Fauna/Prefabs/PFB_Tamandua_Bandeira.prefab", "Assets/Dados/Especies/TamanduaBandeira.asset",
                new Vector3(6f, 0f, 9.5f), AnimalTemperament.Docil, 1.55f, 3.6f, 0.45f, 0.85f, 1.85f,
                animDir + "Tamandua_Idle.anim", animDir + "Tamandua_Walk.anim");

            SpawnFaunaGroup(3, "Tatu_Peba", "Assets/Modelos/Fauna/Prefabs/PFB_Tatu_Peba.prefab", "Assets/Dados/Especies/TatuPeba.asset",
                new Vector3(-4.5f, 0f, 6.8f), AnimalTemperament.Cauteloso, 1.55f, 3.9f, 0.28f, 0.35f, 0.55f,
                animDir + "Tatu_Idle.anim", animDir + "Tatu_Walk.anim");

            SpawnFaunaGroup(4, "Coelho_Tapeti", "Assets/Modelos/Fauna/Prefabs/PFB_Tapiti.prefab", "Assets/Dados/Especies/CoelhoTapeti.asset",
                new Vector3(5.5f, 0f, 5.8f), AnimalTemperament.Arisco, 1.90f, 5.2f, 0.25f, 0.38f, 0.45f,
                animDir + "Tapiti_Idle.anim", animDir + "Tapiti_Walk.anim", null, 0.10f);

            SpawnFaunaGroup(3, "Seriema", "Assets/Modelos/Fauna/Prefabs/PFB_Seriema.prefab", "Assets/Dados/Especies/Seriema.asset",
                new Vector3(-9.5f, 0f, 9.5f), AnimalTemperament.Cauteloso, 1.80f, 4.8f, 0.35f, 0.90f, 0.95f,
                animDir + "Seriema_Idle.anim", animDir + "Seriema_Walk.anim");

            SpawnFaunaGroup(3, "Teiu", "Assets/Modelos/Fauna/Prefabs/PFB_Teiu.prefab", "Assets/Dados/Especies/Teiu.asset",
                new Vector3(9.5f, 0f, 7.0f), AnimalTemperament.Cauteloso, 1.65f, 4.4f, 0.30f, 0.28f, 1.10f,
                animDir + "Teiu_Idle.anim", animDir + "Teiu_Walk.anim");

            SpawnFaunaGroup(3, "Rato_De_Espinho", "Assets/Modelos/Fauna/Prefabs/PFB_Rato_De_Espinho.prefab", "Assets/Dados/Especies/RatoDeEspinho.asset",
                new Vector3(-6.2f, 0f, 4.8f), AnimalTemperament.Arisco, 1.70f, 4.2f, 0.20f, 0.22f, 0.36f,
                animDir + "Rato_Walk.anim", animDir + "Rato_Walk.anim", null, 0.20f);

            // --- INCOMUNS (2 exemplares cada) ---
            SpawnFaunaGroup(2, "Ema", "Assets/Modelos/Fauna/Prefabs/PFB_Ema.prefab", "Assets/Dados/Especies/Ema.asset",
                new Vector3(-7.5f, 0f, 13.0f), AnimalTemperament.Arisco, 2.10f, 5.8f, 0.42f, 1.42f, 1.42f,
                animDir + "Ema_Idle.anim", animDir + "Ema_Walk.anim");

            SpawnFaunaGroup(2, "Jacare_Papo_Amarelo", "Assets/Modelos/Fauna/Prefabs/PFB_Jacare_Papo_Amarelo.prefab", "Assets/Dados/Especies/JacarePapoAmarelo.asset",
                new Vector3(-12.5f, 0f, 8.2f), AnimalTemperament.Docil, 1.05f, 2.8f, 0.48f, 0.45f, 1.85f,
                animDir + "Jacare_Idle.anim", animDir + "Jacare_Walk.anim");

            SpawnFaunaGroup(2, "Tamandua_Mirim", "Assets/Modelos/Fauna/Prefabs/PFB_Tamandua_Mirim.prefab", "Assets/Dados/Especies/TamanduaMirim.asset",
                new Vector3(13.5f, 0f, 10.5f), AnimalTemperament.Docil, 1.40f, 3.3f, 0.35f, 0.62f, 1.15f,
                animDir + "TamanduaMirim_Idle.anim", animDir + "TamanduaMirim_Walk.anim");

            SpawnFaunaGroup(2, "Macaco_Prego", "Assets/Modelos/Fauna/Prefabs/PFB_Macaco_Prego.prefab", "Assets/Dados/Especies/MacacoPrego.asset",
                new Vector3(11.0f, 0f, 19.5f), AnimalTemperament.Cauteloso, 1.85f, 4.8f, 0.32f, 0.68f, 0.85f,
                animDir + "Macaco_Idle.anim", animDir + "Macaco_Walk.anim");

            SpawnFaunaGroup(2, "Carcara", "Assets/Modelos/Fauna/Prefabs/PFB_Carcara.prefab", "Assets/Dados/Especies/Carcara.asset",
                new Vector3(-14.0f, 0f, 17.0f), AnimalTemperament.Cauteloso, 1.65f, 4.6f, 0.30f, 0.58f, 0.68f,
                animDir + "Carcara_Idle.anim", animDir + "Carcara_Walk.anim");

            SpawnFaunaGroup(2, "Coruja_Buraqueira", "Assets/Modelos/Fauna/Prefabs/PFB_Coruja_Buraqueira.prefab", "Assets/Dados/Especies/CorujaBuraqueira.asset",
                new Vector3(3.5f, 0f, 14.0f), AnimalTemperament.Cauteloso, 1.50f, 4.4f, 0.25f, 0.42f, 0.45f,
                animDir + "Coruja_Idle.anim", animDir + "Coruja_Fly.anim", null, 0.20f);

            SpawnFaunaGroup(2, "Galito", "Assets/Modelos/Fauna/Prefabs/PFB_Galito.prefab", "Assets/Dados/Especies/Galito.asset",
                new Vector3(-3.0f, 0f, 11.0f), AnimalTemperament.Arisco, 1.60f, 4.2f, 0.22f, 0.32f, 0.38f,
                animDir + "Galito_Idle.anim", animDir + "Galito_Idle.anim", null, 0.85f);

            // --- RAROS (2 exemplares cada) ---
            SpawnFaunaGroup(2, "Lobo_Guara", "Assets/Modelos/Fauna/Prefabs/PFB_Lobo_Guara.prefab", "Assets/Dados/Especies/LoboGuara.asset",
                new Vector3(9.5f, 0f, 15.0f), AnimalTemperament.Cauteloso, 1.65f, 4.3f, 0.42f, 0.95f, 1.25f,
                animDir + "Lobo_Idle.anim", animDir + "Lobo_Walk.anim", null, 0.10f);

            SpawnFaunaGroup(2, "Veado_Campeiro", "Assets/Modelos/Fauna/Prefabs/PFB_Veado_Campeiro.prefab", "Assets/Dados/Especies/VeadoCampeiro.asset",
                new Vector3(-13.5f, 0f, 15.0f), AnimalTemperament.Arisco, 2.05f, 5.9f, 0.40f, 1.15f, 1.35f,
                animDir + "Veado_Idle.anim", animDir + "Veado_Walk.anim", animDir + "Veado_Run.anim");

            SpawnFaunaGroup(2, "Jiboia", "Assets/Modelos/Fauna/Prefabs/PFB_Jiboia.prefab", "Assets/Dados/Especies/Jiboia.asset",
                new Vector3(16.5f, 0f, -6.5f), AnimalTemperament.Docil, 0.90f, 2.2f, 0.38f, 0.25f, 2.10f,
                animDir + "Jiboia_Walk.anim", animDir + "Jiboia_Walk.anim", null, 0.15f);

            SpawnFaunaGroup(2, "Cascavel", "Assets/Modelos/Fauna/Prefabs/PFB_Cascavel.prefab", "Assets/Dados/Especies/Cascavel.asset",
                new Vector3(-16.0f, 0f, -9.0f), AnimalTemperament.Cauteloso, 0.95f, 2.3f, 0.34f, 0.25f, 1.65f,
                animDir + "Cascavel_Walk.anim", animDir + "Cascavel_Walk.anim", null, 0.15f);

            // --- MUITO RARO (2 exemplares — Suçuarana com 53 BlendShapes + OncaBlendShapeController!) ---
            SpawnFaunaGroup(2, "Onca_Parda", "Assets/Modelos/Fauna/Prefabs/PFB_Onca_Parda.prefab", "Assets/Dados/Especies/OncaParda.asset",
                new Vector3(13.0f, 0f, 18.5f), AnimalTemperament.Arisco, 2.10f, 5.8f, 0.42f, 0.85f, 1.76f,
                null, null, null, 1.0f, isOncaBlendShape: true);

            // =========================================================================
            // FLORA NATIVA COMPLETA DO CERRADO (20 Espécies — Comum: 3 exemplares | Acima de Comum: 2 exemplares)
            // =========================================================================

            // --- COMUNS (3 a 4 exemplares cada) ---
            SpawnFloraGroup(3, "Jatoba", "Assets/Modelos/Flora/Prefabs/PFB_Jatoba.prefab", "Assets/Dados/Especies/Jatoba.asset", new Vector3(0f, 0f, 17.5f), 7.2f);
            SpawnFloraGroup(3, "Angico", "Assets/Modelos/Flora/Prefabs/PFB_Angico.prefab", "Assets/Dados/Especies/Angico.asset", new Vector3(-12f, 0f, 19.5f), 6.6f);
            SpawnFloraGroup(3, "Aroeira", "Assets/Modelos/Flora/Prefabs/PFB_Aroeira.prefab", "Assets/Dados/Especies/Aroeira.asset", new Vector3(13.5f, 0f, 12.5f), 6.4f);
            SpawnFloraGroup(3, "Cinzeiro", "Assets/Modelos/Flora/Prefabs/PFB_Cinzeiro.prefab", "Assets/Dados/Especies/Cinzeiro.asset", new Vector3(-20f, 0f, -4f), 6.2f);
            SpawnFloraGroup(3, "Cajuzinho_Do_Campo", "Assets/Modelos/Flora/Prefabs/PFB_Cajuzinho_Do_Campo.prefab", "Assets/Dados/Especies/CajuzinhoDoCampo.asset", new Vector3(7.5f, 0f, -4.5f), 1.15f, null, null, true);
            SpawnFloraGroup(4, "Capim_Flechinha", "Assets/Modelos/Flora/Prefabs/PFB_Capim_Flechinha.prefab", "Assets/Dados/Especies/CapimFlechinha.asset", new Vector3(-5.5f, 0f, 9.2f), 1.10f, null, null, true);

            Material chuvStem = GetOrCreateTexturedMaterial("Mat_Chuveirinho_Caule", Color.white, "Assets/Dados/Texturas/FBX_chuveirinho_0.png", "Assets/Dados/Texturas/FBX_chuveirinho_1.jpg", true);
            Material chuvFlower = GetOrCreateTexturedMaterial("Mat_Chuveirinho_Flor", Color.white, "Assets/Dados/Texturas/FBX_chuveirinho_2.png", null, true);
            SpawnFloraGroup(3, "Chuveirinho", "Assets/Modelos/Flora/chuveirinho.fbx", "Assets/Dados/Especies/Chuveirinho.asset", new Vector3(3.5f, 0f, 6.2f), 1.25f, chuvStem, chuvFlower, true);

            Material arvoreSecaBark = GetOrCreateTexturedMaterial("Mat_ArvoreSeca_Casca", Color.white, "Assets/Dados/Texturas/FBX_ArvoreSeca_0.png", "Assets/Dados/Texturas/FBX_ArvoreSeca_1.jpg", false);
            SpawnFloraGroup(3, "Arvore_Seca_Cerrado", "Assets/Modelos/Flora/ArvoreSeca.fbx", "Assets/Dados/Especies/ArvoreSeca.asset", new Vector3(-9.5f, 0f, 15.5f), 5.8f, arvoreSecaBark, null, false);

            // --- INCOMUNS & RAROS (2 exemplares cada) ---
            SpawnFloraGroup(2, "Ipe_Amarelo", "Assets/Modelos/Flora/Prefabs/PFB_Ipe_Amarelo.prefab", "Assets/Dados/Especies/IpeAmarelo.asset", new Vector3(7.5f, 0f, 24f), 6.8f);
            SpawnFloraGroup(2, "Pequizeiro", "Assets/Modelos/Flora/Prefabs/PFB_Pequi.prefab", "Assets/Dados/Especies/Pequizeiro.asset", new Vector3(-18.5f, 0f, 10.5f), 6.2f);
            SpawnFloraGroup(2, "Barbatimao", "Assets/Modelos/Flora/Prefabs/PFB_Barbatimao.prefab", "Assets/Dados/Especies/Barbatimao.asset", new Vector3(17.5f, 0f, -6.5f), 5.6f);
            SpawnFloraGroup(2, "Araticum", "Assets/Modelos/Flora/Prefabs/PFB_Araticum.prefab", "Assets/Dados/Especies/Araticum.asset", new Vector3(-24f, 0f, 22f), 5.4f);
            SpawnFloraGroup(2, "Canela_De_Ema", "Assets/Modelos/Flora/Prefabs/PFB_Canela_De_Ema.prefab", "Assets/Dados/Especies/CanelaDeEma.asset", new Vector3(19f, 0f, 6f), 1.65f, null, null, true);
            SpawnFloraGroup(2, "Catuaba", "Assets/Modelos/Flora/Prefabs/PFB_Catuaba.prefab", "Assets/Dados/Especies/Catuaba.asset", new Vector3(-11f, 0f, -12f), 1.25f, null, null, true);
            SpawnFloraGroup(2, "Copaiba", "Assets/Modelos/Flora/Prefabs/PFB_Copaiba.prefab", "Assets/Dados/Especies/Copaiba.asset", new Vector3(25f, 0f, 24f), 7.6f);
            SpawnFloraGroup(2, "Gravata", "Assets/Modelos/Flora/Prefabs/PFB_Gravata.prefab", "Assets/Dados/Especies/Gravata.asset", new Vector3(-4.5f, 0f, -8.5f), 1.05f, null, null, true);
            SpawnFloraGroup(2, "Indaia", "Assets/Modelos/Flora/Prefabs/PFB_Indaia.prefab", "Assets/Dados/Especies/Indaia.asset", new Vector3(16f, 0f, 29f), 5.5f);
            SpawnFloraGroup(2, "Lobeira", "Assets/Modelos/Flora/Prefabs/PFB_Lobeira.prefab", "Assets/Dados/Especies/Lobeira.asset", new Vector3(-16f, 0f, 28f), 2.8f, null, null, true);
            SpawnFloraGroup(2, "Pau_Santo", "Assets/Modelos/Flora/Prefabs/PFB_Pau_Santo.prefab", "Assets/Dados/Especies/PauSanto.asset", new Vector3(26f, 0f, -15f), 5.2f);
            SpawnFloraGroup(2, "Sucupira", "Assets/Modelos/Flora/Prefabs/PFB_Sucupira.prefab", "Assets/Dados/Especies/Sucupira.asset", new Vector3(-25f, 0f, -16f), 6.5f);

            // Garante componente AnimalSpawner ativo em _Managers para coordenar população e camadas da cerca
            GameObject managersObj = GameObject.Find("_Managers");
            if (managersObj != null)
            {
                var spawner = GetOrAddComponent<AnimalSpawner>(managersObj);
                var soSp = new SerializedObject(spawner);
                soSp.FindProperty("maxPopulation").intValue = 48;
                soSp.ApplyModifiedProperties();
            }
        }

        private static void SetupVegetation()
        {
            GameObject vegGroup = GameObject.Find("_Vegetacao");
            if (vegGroup != null)
            {
                Object.DestroyImmediate(vegGroup);
            }

            vegGroup = new GameObject("_Vegetacao");

            string[] grassPrefabPaths = new[]
            {
                "Assets/TerrainSampleAssets/Prefabs/GrassDry_A.prefab",
                "Assets/TerrainSampleAssets/Prefabs/GrassDry_B.prefab",
                "Assets/TerrainSampleAssets/Prefabs/GrassDry_C.prefab",
                "Assets/TerrainSampleAssets/Prefabs/Grass_A.prefab",
                "Assets/TerrainSampleAssets/Prefabs/Grass_B.prefab",
                "Assets/TerrainSampleAssets/Prefabs/Grass_C.prefab",
                "Assets/TerrainSampleAssets/Prefabs/Grass_D.prefab"
            };
            string[] shrubPrefabPaths = new[]
            {
                "Assets/TerrainSampleAssets/Prefabs/BushDry_A.prefab",
                "Assets/TerrainSampleAssets/Prefabs/BushDry_B.prefab",
                "Assets/TerrainSampleAssets/Prefabs/Bush_A.prefab",
                "Assets/TerrainSampleAssets/Prefabs/Bush_B.prefab",
                "Assets/TerrainSampleAssets/Prefabs/Heather_A.prefab",
                "Assets/TerrainSampleAssets/Prefabs/Fern_A.prefab",
                "Assets/TerrainSampleAssets/Prefabs/Plant_A.prefab"
            };

            List<GameObject> grassPrefabs = new List<GameObject>();
            foreach (var path in grassPrefabPaths)
            {
                var p = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (p != null) grassPrefabs.Add(p);
            }
            List<GameObject> shrubPrefabs = new List<GameObject>();
            foreach (var path in shrubPrefabPaths)
            {
                var p = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (p != null) shrubPrefabs.Add(p);
            }

            Random.InitState(42);
            if (grassPrefabs.Count > 0)
            {
                // 1. Campo denso e vivo de capim dourado/esverdeado (1450 tufos espalhados com clareiras naturais de terra seca)
                int placedBase = 0;
                for (int i = 0; i < 1850 && placedBase < 1450; i++)
                {
                    float rx = Random.Range(-41f, 41f);
                    float rz = Random.Range(-30f, 43f);
                    if (new Vector2(rx, rz).magnitude < 3.0f || Vector2.Distance(new Vector2(rx, rz), new Vector2(0f, 4.2f)) < 2.2f)
                        continue;

                    // Ruído coerente deixa ~18% de clareiras naturais de terra avermelhada
                    float patchNoise = Mathf.PerlinNoise((rx + 80f) * 0.085f, (rz + 80f) * 0.085f);
                    if (patchNoise < 0.24f) continue;

                    Vector3 pos = new Vector3(rx, 0f, rz);
                    SampleGroundSurface(pos, out float groundY, out Vector3 groundNormal);
                    pos.y = groundY - 0.02f;

                    bool pickShrub = shrubPrefabs.Count > 0 && Random.value < 0.16f;
                    GameObject chosenPrefab = pickShrub
                        ? shrubPrefabs[Random.Range(0, shrubPrefabs.Count)]
                        : grassPrefabs[Random.Range(0, grassPrefabs.Count)];

                    GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(chosenPrefab, vegGroup.transform);
                    instance.transform.position = pos;
                    Quaternion slopeAlign = Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(Vector3.up, groundNormal), 0.7f);
                    instance.transform.rotation = slopeAlign * Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                    float scale = pickShrub ? Random.Range(0.95f, 1.55f) : Random.Range(1.15f, 2.05f);
                    instance.transform.localScale = Vector3.one * scale;
                    placedBase++;
                }

                // 2. 130 agrupamentos densos orgânicos (moitas cheias de Cerrado: capim alto + arbustos ao redor)
                for (int c = 0; c < 130; c++)
                {
                    Vector2 clusterCenter = new Vector2(Random.Range(-39f, 39f), Random.Range(-28f, 41f));
                    if (clusterCenter.magnitude < 4.2f) continue;

                    int clumpCount = Random.Range(9, 14);
                    for (int k = 0; k < clumpCount; k++)
                    {
                        Vector2 offset = Random.insideUnitCircle * 3.1f;
                        Vector3 pos = new Vector3(clusterCenter.x + offset.x, 0f, clusterCenter.y + offset.y);
                        SampleGroundSurface(pos, out float groundY, out Vector3 groundNormal);
                        pos.y = groundY - 0.02f;

                        GameObject chosenPrefab = (k == 0 && shrubPrefabs.Count > 0)
                            ? shrubPrefabs[Random.Range(0, shrubPrefabs.Count)]
                            : grassPrefabs[Random.Range(0, grassPrefabs.Count)];

                        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(chosenPrefab, vegGroup.transform);
                        instance.transform.position = pos;
                        Quaternion slopeAlign = Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(Vector3.up, groundNormal), 0.7f);
                        instance.transform.rotation = slopeAlign * Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                        instance.transform.localScale = Vector3.one * (k == 0 ? Random.Range(1.15f, 1.75f) : Random.Range(1.25f, 2.25f));
                    }
                }

                // 3. Mascaramento físico denso exatamente sobre a linha divisória entre Terra e Grama do terreno_NOVO
                SpawnTerrainSeamMaskingVegetation(vegGroup.transform, grassPrefabs, shrubPrefabs);
            }

            // Espalha 28 árvores nativas diversificadas do Cerrado extras pela área jogável (portes Pequeno/Médio/Grande + inclinação orgânica)
            string[] treeModels = new[]
            {
                "Assets/Modelos/Flora/Prefabs/PFB_Ipe_Amarelo.prefab",
                "Assets/Modelos/Flora/Prefabs/PFB_Pequi.prefab",
                "Assets/Modelos/Flora/Prefabs/PFB_Barbatimao.prefab",
                "Assets/Modelos/Flora/Prefabs/PFB_Angico.prefab",
                "Assets/Modelos/Flora/Prefabs/PFB_Jatoba.prefab",
                "Assets/Modelos/Flora/Prefabs/PFB_Aroeira.prefab",
                "Assets/Modelos/Flora/Prefabs/PFB_Cinzeiro.prefab",
                "Assets/Modelos/Flora/Prefabs/PFB_Copaiba.prefab",
                "Assets/Modelos/Flora/Prefabs/PFB_Indaia.prefab",
                "Assets/Modelos/Flora/Prefabs/PFB_Sucupira.prefab",
                "Assets/Modelos/Flora/Prefabs/PFB_Pau_Santo.prefab",
                "Assets/Modelos/Flora/Prefabs/PFB_Araticum.prefab",
                "Assets/Modelos/Flora/ArvoreSeca.fbx"
            };

            Vector3[] extraTreePositions = new[]
            {
                new Vector3(-22f, 0f, 12f), new Vector3(24f, 0f, 15f), new Vector3(-18f, 0f, 28f), new Vector3(19f, 0f, 32f),
                new Vector3(-26f, 0f, -14f), new Vector3(22f, 0f, -16f), new Vector3(4f, 0f, 34f), new Vector3(-8f, 0f, -22f),
                new Vector3(-31f, 0f, 3f), new Vector3(30f, 0f, 4f), new Vector3(12f, 0f, -24f), new Vector3(-28f, 0f, 24f),
                new Vector3(34f, 0f, 22f), new Vector3(-35f, 0f, -20f), new Vector3(34f, 0f, -22f), new Vector3(-34f, 0f, 34f),
                new Vector3(31f, 0f, 36f), new Vector3(-12f, 0f, 38f), new Vector3(11f, 0f, 39f), new Vector3(-2f, 0f, -27f),
                new Vector3(21f, 0f, 3f), new Vector3(-21f, 0f, -2f), new Vector3(28f, 0f, -8f), new Vector3(-29f, 0f, 14f),
                new Vector3(14f, 0f, -13f), new Vector3(-15f, 0f, -18f), new Vector3(29f, 0f, 12f), new Vector3(-33f, 0f, -8f)
            };

            Material secaBark = GetOrCreateTexturedMaterial("Mat_ArvoreSeca_Casca", Color.white, "Assets/Dados/Texturas/FBX_ArvoreSeca_0.png", "Assets/Dados/Texturas/FBX_ArvoreSeca_1.jpg", false);

            for (int i = 0; i < extraTreePositions.Length; i++)
            {
                int idx = i % treeModels.Length;
                string modelPath = treeModels[idx];
                GameObject tPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
                if (tPrefab == null) continue;

                Vector3 pos = extraTreePositions[i];
                float groundY = SampleGroundHeight(pos);

                GameObject tRoot = new GameObject($"Arvore_Savana_{i + 1:00}");
                tRoot.transform.SetParent(vegGroup.transform, false);
                tRoot.transform.position = new Vector3(pos.x, groundY, pos.z);
                tRoot.transform.rotation = SampleNaturalTreeRotation();
                tRoot.transform.localScale = Vector3.one;

                GameObject tVisual = (GameObject)PrefabUtility.InstantiatePrefab(tPrefab, tRoot.transform);
                tVisual.name = "Modelo_3D";

                if (modelPath.EndsWith("ArvoreSeca.fbx", System.StringComparison.OrdinalIgnoreCase))
                {
                    ApplyPlantMultiMaterials(tVisual, secaBark, null);
                }

                // Porte variado (~25% Pequenas 0.45-0.70x, ~50% Médias 0.85-1.15x, ~25% Grandes 1.30-1.65x) com tronco cravado no solo
                float variedHeight = 6.0f * SampleNaturalTreeScaleTier();
                NormalizeVisualChildTransform(tRoot, tVisual, modelPath, variedHeight, 0.22f, true);

                var col = GetOrAddComponent<CapsuleCollider>(tRoot);
                col.radius = Mathf.Clamp(variedHeight * 0.09f, 0.35f, 0.85f);
                col.height = Mathf.Clamp(variedHeight * 0.65f, 2.2f, 5.0f);
                col.center = new Vector3(0f, col.height * 0.5f, 0f);
            }

            GameObject mountainPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TerrainSampleAssets/Models/SkyboxMountains01.fbx");
            if (mountainPrefab != null)
            {
                GameObject mountain = (GameObject)PrefabUtility.InstantiatePrefab(mountainPrefab, vegGroup.transform);
                mountain.name = "Horizonte_Serra";
                mountain.transform.position = new Vector3(0, -2f, 88f);
                mountain.transform.localScale = new Vector3(6f, 5f, 6f);
            }
        }

        /// <summary>
        /// Constrói um perímetro visível e físico coerente com o Cerrado ao redor da área jogável da Beta
        /// (cercas rústicas de madeira, cinturão de arbustos/árvores, placas de limite da reserva e BoxColliders invisíveis).
        /// </summary>
        private static void SetupMapBoundaries()
        {
            GameObject oldBounds = GameObject.Find("_Limites_Reserva_Cerrado");
            if (oldBounds != null)
            {
                Object.DestroyImmediate(oldBounds);
            }

            GameObject boundsRoot = new GameObject("_Limites_Reserva_Cerrado");

            const float minX = -43f;
            const float maxX = 43f;
            const float minZ = -33f;
            const float maxZ = 46f;
            float centerX = (minX + maxX) * 0.5f;
            float centerZ = (minZ + maxZ) * 0.5f;
            float widthX = maxX - minX;
            float depthZ = maxZ - minZ;

            // 1. Barreiras Físicas (BoxColliders invisíveis contínuos nas 4 bordas, cobrindo toda a altura do relevo)
            GameObject collidersGroup = new GameObject("Barreiras_Fisicas_Colisores");
            collidersGroup.transform.SetParent(boundsRoot.transform, false);

            CreateInvisibleWall(collidersGroup.transform, "Barreira_Norte", new Vector3(centerX, 6.5f, maxZ), new Vector3(widthX + 4f, 18f, 1.2f));
            CreateInvisibleWall(collidersGroup.transform, "Barreira_Sul", new Vector3(centerX, 6.5f, minZ), new Vector3(widthX + 4f, 18f, 1.2f));
            CreateInvisibleWall(collidersGroup.transform, "Barreira_Leste", new Vector3(maxX, 6.5f, centerZ), new Vector3(1.2f, 18f, depthZ + 4f));
            CreateInvisibleWall(collidersGroup.transform, "Barreira_Oeste", new Vector3(minX, 6.5f, centerZ), new Vector3(1.2f, 18f, depthZ + 4f));

            // 2. Cerca Rústica de Madeira da Reserva ao redor de todo o perímetro
            Material woodMat = GetOrCreateTexturedMaterial(
                "Mat_Madeira_Cerca_Reserva",
                new Color(0.58f, 0.44f, 0.30f),
                "Assets/Dados/Texturas/Bark_Rough_Diffuse.jpg",
                "Assets/Dados/Texturas/Bark_Rough_Normal.jpg",
                false,
                2f
            );

            GameObject fenceGroup = new GameObject("Cerca_Rustica_Perimetro");
            fenceGroup.transform.SetParent(boundsRoot.transform, false);

            BuildFenceLine(fenceGroup.transform, new Vector3(minX, 0f, maxZ), new Vector3(maxX, 0f, maxZ), woodMat, "Norte");
            BuildFenceLine(fenceGroup.transform, new Vector3(maxX, 0f, maxZ), new Vector3(maxX, 0f, minZ), woodMat, "Leste");
            BuildFenceLine(fenceGroup.transform, new Vector3(maxX, 0f, minZ), new Vector3(minX, 0f, minZ), woodMat, "Sul");
            BuildFenceLine(fenceGroup.transform, new Vector3(minX, 0f, minZ), new Vector3(minX, 0f, maxZ), woodMat, "Oeste");

            // 3. Placas Indicativas de Limite da Reserva nos 4 lados
            GameObject signsGroup = new GameObject("Placas_Limite_Reserva");
            signsGroup.transform.SetParent(boundsRoot.transform, false);

            CreateReserveBoundarySign(signsGroup.transform, "Placa_Limite_Norte", new Vector3(0f, 0f, maxZ - 0.6f), Quaternion.Euler(0f, 0f, 0f), woodMat);
            CreateReserveBoundarySign(signsGroup.transform, "Placa_Limite_Sul", new Vector3(0f, 0f, minZ + 0.6f), Quaternion.Euler(0f, 180f, 0f), woodMat);
            CreateReserveBoundarySign(signsGroup.transform, "Placa_Limite_Leste", new Vector3(maxX - 0.6f, 0f, centerZ), Quaternion.Euler(0f, 90f, 0f), woodMat);
            CreateReserveBoundarySign(signsGroup.transform, "Placa_Limite_Oeste", new Vector3(minX + 0.6f, 0f, centerZ), Quaternion.Euler(0f, -90f, 0f), woodMat);

            // 4. Cinturão Denso de Árvores e Arbustos ao longo de todo o perímetro externo (bloqueia a visão do fim do mapa)
            GameObject bushPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TerrainSampleAssets/Prefabs/BushDry_A.prefab");
            GameObject heatherPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TerrainSampleAssets/Prefabs/Heather_A.prefab");
            GameObject grassDryPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TerrainSampleAssets/Prefabs/GrassDry_A.prefab");

            GameObject borderVegGroup = new GameObject("Cinturao_Mata_Densa_Borda");
            borderVegGroup.transform.SetParent(boundsRoot.transform, false);
            Random.InitState(1337);

            // 4a. Parede densa de arbustos e capim logo atrás da cerca (3 camadas a cada 2.0m)
            if (bushPrefab != null || heatherPrefab != null)
            {
                float[] bushBands = { 1.1f, 2.7f, 4.4f };
                for (int b = 0; b < bushBands.Length; b++)
                {
                    float d = bushBands[b];
                    float stag = (b % 2 == 1) ? 1.0f : 0f;
                    for (float x = minX - 7f; x <= maxX + 7f; x += 2.1f)
                    {
                        SpawnBorderBush(borderVegGroup.transform, bushPrefab, heatherPrefab, new Vector3(x + stag + Random.Range(-0.4f, 0.4f), 0f, maxZ + d + Random.Range(-0.35f, 0.35f)));
                        SpawnBorderBush(borderVegGroup.transform, bushPrefab, heatherPrefab, new Vector3(x + stag + Random.Range(-0.4f, 0.4f), 0f, minZ - d + Random.Range(-0.35f, 0.35f)));
                    }
                    for (float z = minZ - 4f; z <= maxZ + 4f; z += 2.1f)
                    {
                        SpawnBorderBush(borderVegGroup.transform, bushPrefab, heatherPrefab, new Vector3(maxX + d + Random.Range(-0.35f, 0.35f), 0f, z + stag + Random.Range(-0.4f, 0.4f)));
                        SpawnBorderBush(borderVegGroup.transform, bushPrefab, heatherPrefab, new Vector3(minX - d + Random.Range(-0.35f, 0.35f), 0f, z + stag + Random.Range(-0.4f, 0.4f)));
                    }
                }
            }

            // 4b. Cinturão de 4 CAMADAS densas e intercaladas de Árvores Nativas do Cerrado com variedade de portes (bloqueia 100% do horizonte)
            string[] borderTreePaths = new[]
            {
                "Assets/Modelos/Flora/Prefabs/PFB_Ipe_Amarelo.prefab",
                "Assets/Modelos/Flora/Prefabs/PFB_Pequi.prefab",
                "Assets/Modelos/Flora/Prefabs/PFB_Barbatimao.prefab",
                "Assets/Modelos/Flora/Prefabs/PFB_Angico.prefab",
                "Assets/Modelos/Flora/Prefabs/PFB_Jatoba.prefab",
                "Assets/Modelos/Flora/Prefabs/PFB_Aroeira.prefab",
                "Assets/Modelos/Flora/Prefabs/PFB_Cinzeiro.prefab",
                "Assets/Modelos/Flora/Prefabs/PFB_Copaiba.prefab",
                "Assets/Modelos/Flora/Prefabs/PFB_Sucupira.prefab",
                "Assets/Modelos/Flora/Prefabs/PFB_Pau_Santo.prefab",
                "Assets/Modelos/Flora/Prefabs/PFB_Araticum.prefab",
                "Assets/Modelos/Flora/ArvoreSeca.fbx"
            };
            Material secaBark = GetOrCreateTexturedMaterial("Mat_ArvoreSeca_Casca", Color.white, "Assets/Dados/Texturas/FBX_ArvoreSeca_0.png", "Assets/Dados/Texturas/FBX_ArvoreSeca_1.jpg", false);

            var borderTreePositions = new List<(Vector3 pos, int layer)>();
            float[] layerOffsets = { 2.3f, 5.2f, 8.3f, 11.6f };
            float[] layerStep = { 4.2f, 4.0f, 3.8f, 3.8f };
            for (int layer = 0; layer < layerOffsets.Length; layer++)
            {
                float dist = layerOffsets[layer];
                float step = layerStep[layer];
                float stagger = (layer % 2 == 1) ? (step * 0.5f) : 0f;
                float cornerPad = dist + 2.5f;

                for (float x = minX - cornerPad; x <= maxX + cornerPad; x += step)
                {
                    borderTreePositions.Add((new Vector3(x + stagger + Random.Range(-0.8f, 0.8f), 0f, maxZ + dist + Random.Range(-0.7f, 0.7f)), layer));
                    borderTreePositions.Add((new Vector3(x + stagger + Random.Range(-0.8f, 0.8f), 0f, minZ - dist + Random.Range(-0.7f, 0.7f)), layer));
                }
                for (float z = minZ - 1f; z <= maxZ + 1f; z += step)
                {
                    borderTreePositions.Add((new Vector3(maxX + dist + Random.Range(-0.7f, 0.7f), 0f, z + stagger + Random.Range(-0.8f, 0.8f)), layer));
                    borderTreePositions.Add((new Vector3(minX - dist + Random.Range(-0.7f, 0.7f), 0f, z + stagger + Random.Range(-0.8f, 0.8f)), layer));
                }
            }

            for (int i = 0; i < borderTreePositions.Count; i++)
            {
                int tIdx = i % borderTreePaths.Length;
                string mPath = borderTreePaths[tIdx];
                GameObject tPref = AssetDatabase.LoadAssetAtPath<GameObject>(mPath);
                if (tPref == null) continue;

                Vector3 pos = borderTreePositions[i].pos;
                int layer = borderTreePositions[i].layer;
                float gY = SampleGroundHeight(pos);

                GameObject btRoot = new GameObject($"Arvore_Borda_{i + 1:000}");
                btRoot.transform.SetParent(borderVegGroup.transform, false);
                btRoot.transform.position = new Vector3(pos.x, gY, pos.z);
                btRoot.transform.rotation = SampleNaturalTreeRotation();
                btRoot.transform.localScale = Vector3.one;

                GameObject btVis = (GameObject)PrefabUtility.InstantiatePrefab(tPref, btRoot.transform);
                btVis.name = "Modelo_3D";

                if (mPath.EndsWith("ArvoreSeca.fbx", System.StringComparison.OrdinalIgnoreCase))
                {
                    ApplyPlantMultiMaterials(btVis, secaBark, null);
                }

                // Distribuição natural de portes (Pequenas/Mudas, Médias, Grandes/Centenárias); última camada mantém copa alta para fechar horizonte
                float scaleTier = SampleNaturalTreeScaleTier();
                if (layer == 3 && scaleTier < 0.85f) scaleTier = Random.Range(0.95f, 1.55f);
                float treeHeight = 6.2f * scaleTier;
                NormalizeVisualChildTransform(btRoot, btVis, mPath, treeHeight, 0.28f, true);
            }
        }

        private static void CreateInvisibleWall(Transform parent, string name, Vector3 pos, Vector3 size)
        {
            GameObject wall = new GameObject(name);
            wall.transform.SetParent(parent, false);
            wall.transform.position = pos;
            var box = wall.AddComponent<BoxCollider>();
            box.size = size;
            wall.isStatic = true;
            int fenceLayer = LayerMask.NameToLayer("FenceBoundary");
            if (fenceLayer >= 0) wall.layer = fenceLayer;
        }

        private static void BuildFenceLine(Transform parent, Vector3 start, Vector3 end, Material woodMat, string sideLabel)
        {
            float totalDist = Vector3.Distance(start, end);
            const float step = 3.2f;
            int segments = Mathf.Max(1, Mathf.RoundToInt(totalDist / step));
            Vector3 dir = (end - start).normalized;
            float segLen = totalDist / segments;
            Quaternion segRot = Quaternion.LookRotation(dir, Vector3.up);

            for (int i = 0; i < segments; i++)
            {
                Vector3 postPos = start + dir * (i * segLen);
                postPos.y = SampleGroundHeight(postPos);

                Vector3 nextPostPos = start + dir * ((i + 1) * segLen);
                nextPostPos.y = SampleGroundHeight(nextPostPos);

                // Mourão vertical de madeira plantado no relevo do terreno_NOVO
                GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cube);
                post.name = $"Mourao_{sideLabel}_{i:00}";
                post.transform.SetParent(parent, false);
                post.transform.position = postPos + new Vector3(0f, 0.62f, 0f);
                post.transform.rotation = segRot * Quaternion.Euler(Random.Range(-2.0f, 2.0f), Random.Range(-6f, 6f), Random.Range(-2.0f, 2.0f));
                post.transform.localScale = new Vector3(0.18f, 1.45f, 0.18f);
                post.isStatic = true;
                var postCol = post.GetComponent<Collider>();
                if (postCol != null) Object.DestroyImmediate(postCol);
                var postMr = post.GetComponent<MeshRenderer>();
                if (postMr != null) postMr.sharedMaterial = woodMat;

                // Duas travessas acompanhando a inclinação do relevo entre este mourão e o próximo
                Vector3 spanVec = nextPostPos - postPos;
                float spanDist = spanVec.magnitude;
                Quaternion railRot = spanDist > 0.01f ? Quaternion.LookRotation(spanVec.normalized, Vector3.up) : segRot;
                Vector3 railMid = (postPos + nextPostPos) * 0.5f;

                for (int r = 0; r < 2; r++)
                {
                    float railY = (r == 0) ? 1.02f : 0.52f;
                    GameObject rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    rail.name = $"Travessa_{sideLabel}_{i:00}_{r}";
                    rail.transform.SetParent(parent, false);
                    rail.transform.position = railMid + new Vector3(0f, railY, 0f);
                    rail.transform.rotation = railRot;
                    rail.transform.localScale = new Vector3(0.09f, 0.11f, spanDist + 0.08f);
                    rail.isStatic = true;
                    var railCol = rail.GetComponent<Collider>();
                    if (railCol != null) Object.DestroyImmediate(railCol);
                    var railMr = rail.GetComponent<MeshRenderer>();
                    if (railMr != null) railMr.sharedMaterial = woodMat;
                }
            }
        }

        private static void CreateReserveBoundarySign(Transform parent, string name, Vector3 pos, Quaternion rot, Material woodMat)
        {
            pos.y = SampleGroundHeight(pos);
            GameObject signRoot = new GameObject(name);
            signRoot.transform.SetParent(parent, false);
            signRoot.transform.position = pos;
            signRoot.transform.rotation = rot;

            // Suportes da placa
            for (int s = -1; s <= 1; s += 2)
            {
                GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                leg.name = s < 0 ? "Poste_Esq" : "Poste_Dir";
                leg.transform.SetParent(signRoot.transform, false);
                leg.transform.localPosition = new Vector3(s * 0.95f, 1.0f, 0f);
                leg.transform.localScale = new Vector3(0.16f, 2.1f, 0.16f);
                var legCol = leg.GetComponent<Collider>();
                if (legCol != null) Object.DestroyImmediate(legCol);
                var legMr = leg.GetComponent<MeshRenderer>();
                if (legMr != null) legMr.sharedMaterial = woodMat;
            }

            // Tábua principal da placa
            GameObject board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.name = "Tabua_Placa";
            board.transform.SetParent(signRoot.transform, false);
            board.transform.localPosition = new Vector3(0f, 1.62f, -0.08f);
            board.transform.localScale = new Vector3(2.4f, 0.78f, 0.10f);
            var boardCol = board.GetComponent<Collider>();
            if (boardCol != null) Object.DestroyImmediate(boardCol);
            var boardMr = board.GetComponent<MeshRenderer>();
            if (boardMr != null) boardMr.sharedMaterial = woodMat;

            // Texto World-Space na face frontal da placa
            GameObject canvasGo = new GameObject("Canvas_Placa", typeof(RectTransform), typeof(Canvas));
            canvasGo.transform.SetParent(signRoot.transform, false);
            var c = canvasGo.GetComponent<Canvas>();
            c.renderMode = RenderMode.WorldSpace;
            var rt = canvasGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(460f, 145f);
            rt.localPosition = new Vector3(0f, 1.62f, -0.14f);
            rt.localRotation = Quaternion.identity;
            rt.localScale = new Vector3(0.005f, 0.005f, 0.005f);

            GameObject txtGo = new GameObject("Texto_Limite", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            txtGo.transform.SetParent(canvasGo.transform, false);
            SetFullStretch(txtGo.GetComponent<RectTransform>());
            var tmp = txtGo.GetComponent<TextMeshProUGUI>();
            tmp.text = "<b>RESERVA BIOLÓGICA DO CERRADO</b>\n<size=78%>— LIMITE DA ÁREA DE PESQUISA —</size>";
            tmp.fontSize = 30f;
            tmp.color = new Color(0.97f, 0.91f, 0.76f, 1f);
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
        }

        private static float SampleNaturalTreeScaleTier()
        {
            float roll = Random.value;
            if (roll < 0.25f) return Random.Range(0.45f, 0.70f);  // ~25% Pequenas / Mudas
            if (roll < 0.75f) return Random.Range(0.85f, 1.15f);  // ~50% Médias (porte padrão)
            return Random.Range(1.30f, 1.65f);                    // ~25% Grandes / Centenárias
        }

        private static Quaternion SampleNaturalTreeRotation()
        {
            return Quaternion.Euler(
                Random.Range(-3.0f, 3.0f),
                Random.Range(0f, 360f),
                Random.Range(-3.0f, 3.0f)
            );
        }

        /// <summary>
        /// Espalha uma faixa orgânica e densa de tufos de capim, matinhos baixos, pedras pequenas e folhas secas
        /// exatamente sobre as arestas detectadas entre terra e grama no terreno_NOVO.
        /// </summary>
        private static void SpawnTerrainSeamMaskingVegetation(Transform vegRoot, List<GameObject> grassPrefabs, List<GameObject> shrubPrefabs)
        {
            if (terrainGrassDirtBoundaryEdges.Count == 0 || grassPrefabs == null || grassPrefabs.Count == 0)
            {
                return;
            }

            GameObject seamGroup = new GameObject("_Mascaramento_Borda_TerraGrama");
            seamGroup.transform.SetParent(vegRoot, false);

            var rockPrefabs = new List<GameObject>();
            foreach (var rp in new[]
            {
                "Assets/TerrainSampleAssets/Prefabs/Rocks/Rock_A_01.prefab",
                "Assets/TerrainSampleAssets/Prefabs/Rocks/Rock_A_02.prefab",
                "Assets/TerrainSampleAssets/Prefabs/Rocks/Rock_B_01.prefab",
                "Assets/TerrainSampleAssets/Prefabs/Rocks/Rock_C_01.prefab"
            })
            {
                var p = AssetDatabase.LoadAssetAtPath<GameObject>(rp);
                if (p != null) rockPrefabs.Add(p);
            }

            GameObject dryLeafPatchPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TerrainSampleAssets/Prefabs/GrassDry_C.prefab")
                ?? grassPrefabs[0];
            Material pebbleFallbackMat = GetOrCreateTexturedMaterial(
                "Mat_Pedra_Cerrado_Borda",
                new Color(0.62f, 0.52f, 0.42f),
                "Assets/Dados/Texturas/GroundDirtWeedsPatchy004_COL_2K.jpg",
                "Assets/Dados/Texturas/Poliigon_GrassPatchyGround_4585_Normal.png",
                false,
                2f
            );

            Random.InitState(777);
            int spawned = 0;
            const int maxMaskingProps = 720;

            for (int e = 0; e < terrainGrassDirtBoundaryEdges.Count && spawned < maxMaskingProps; e++)
            {
                Vector3 a = terrainGrassDirtBoundaryEdges[e].a;
                Vector3 b = terrainGrassDirtBoundaryEdges[e].b;
                Vector3 mid = (a + b) * 0.5f;

                // Foca no interior e área visível da reserva (dentro do cinturão de árvores)
                if (mid.x < -46f || mid.x > 46f || mid.z < -36f || mid.z > 50f) continue;

                float segLen = Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
                int steps = Mathf.Max(1, Mathf.CeilToInt(segLen / 0.85f));

                Vector3 edgeDir = (b - a);
                edgeDir.y = 0f;
                if (edgeDir.sqrMagnitude < 1e-4f) edgeDir = Vector3.forward;
                edgeDir.Normalize();
                Vector3 perp = new Vector3(-edgeDir.z, 0f, edgeDir.x);

                for (int s = 0; s < steps && spawned < maxMaskingProps; s++)
                {
                    float t = (s + Random.Range(0.1f, 0.9f)) / steps;
                    Vector3 basePt = Vector3.Lerp(a, b, t);

                    // 2 elementos por passo cobrindo os dois lados da linha de transição (±1.8m)
                    int perStep = (segLen > 1.2f) ? 2 : 1;
                    for (int k = 0; k < perStep && spawned < maxMaskingProps; k++)
                    {
                        float lateral = Random.Range(-1.85f, 1.85f);
                        Vector3 pos = basePt + perp * lateral + edgeDir * Random.Range(-0.35f, 0.35f);

                        // Mantém livre o ponto exato de spawn do jogador e do Prof. Silva
                        if (new Vector2(pos.x, pos.z).magnitude < 2.4f || Vector2.Distance(new Vector2(pos.x, pos.z), new Vector2(0f, 4.2f)) < 2.2f)
                            continue;

                        SampleGroundSurface(pos, out float gY, out Vector3 gNorm);
                        Quaternion slopeAlign = Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(Vector3.up, gNorm), 0.8f);
                        float roll = Random.value;

                        if (roll < 0.62f)
                        {
                            // Tufo denso de capim seco/verde na costura
                            GameObject gp = grassPrefabs[Random.Range(0, grassPrefabs.Count)];
                            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(gp, seamGroup.transform);
                            inst.transform.position = new Vector3(pos.x, gY - 0.03f, pos.z);
                            inst.transform.rotation = slopeAlign * Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                            inst.transform.localScale = Vector3.one * Random.Range(1.20f, 2.15f);
                        }
                        else if (roll < 0.82f && shrubPrefabs != null && shrubPrefabs.Count > 0)
                        {
                            // Matinho baixo / arbusto rasteiro de transição
                            GameObject sp = shrubPrefabs[Random.Range(0, shrubPrefabs.Count)];
                            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(sp, seamGroup.transform);
                            inst.transform.position = new Vector3(pos.x, gY - 0.03f, pos.z);
                            inst.transform.rotation = slopeAlign * Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                            inst.transform.localScale = Vector3.one * Random.Range(0.75f, 1.35f);
                        }
                        else if (roll < 0.92f && rockPrefabs.Count > 0)
                        {
                            // Pedra pequena parcialmente enterrada na borda da terra (sem colisor para não travar patas/jogador)
                            GameObject rp = rockPrefabs[Random.Range(0, rockPrefabs.Count)];
                            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(rp, seamGroup.transform);
                            foreach (var col in inst.GetComponentsInChildren<Collider>(true))
                            {
                                if (col != null) Object.DestroyImmediate(col);
                            }
                            foreach (var mr in inst.GetComponentsInChildren<MeshRenderer>(true))
                            {
                                if (mr == null) continue;
                                var ms = mr.sharedMaterials;
                                bool needsFix = false;
                                for (int mi = 0; mi < ms.Length; mi++)
                                {
                                    if (ms[mi] == null || ms[mi].shader == null || !ms[mi].shader.name.Contains("Universal Render Pipeline"))
                                    {
                                        ms[mi] = pebbleFallbackMat;
                                        needsFix = true;
                                    }
                                }
                                if (needsFix) mr.sharedMaterials = ms;
                            }
                            inst.transform.position = new Vector3(pos.x, gY - 0.04f, pos.z);
                            inst.transform.rotation = slopeAlign * Quaternion.Euler(Random.Range(-15f, 15f), Random.Range(0f, 360f), Random.Range(-15f, 15f));
                            float sx = Random.Range(0.14f, 0.34f);
                            inst.transform.localScale = new Vector3(sx, sx * Random.Range(0.55f, 0.85f), sx * Random.Range(0.8f, 1.2f));
                        }
                        else
                        {
                            // Folhas secas / palhada rasteira rente ao solo
                            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(dryLeafPatchPrefab, seamGroup.transform);
                            inst.transform.position = new Vector3(pos.x, gY - 0.02f, pos.z);
                            inst.transform.rotation = slopeAlign * Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                            float rad = Random.Range(1.35f, 2.25f);
                            inst.transform.localScale = new Vector3(rad, Random.Range(0.22f, 0.38f), rad);
                        }

                        spawned++;
                    }
                }
            }

            Debug.Log($"[CerradoSceneSetup] Mascaramento físico da transição terra/grama: {spawned} tufos, matinhos, pedras e folhas secas aplicados sobre {terrainGrassDirtBoundaryEdges.Count} arestas.");
        }

        private static void SpawnBorderBush(Transform parent, GameObject bushPrefab, GameObject heatherPrefab, Vector3 pos)
        {
            GameObject prefab = (Random.value > 0.35f && bushPrefab != null) ? bushPrefab : (heatherPrefab != null ? heatherPrefab : bushPrefab);
            if (prefab == null) return;
            pos.y = SampleGroundHeight(pos);
            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            inst.transform.position = pos;
            inst.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            inst.transform.localScale = Vector3.one * Random.Range(1.55f, 2.55f);
        }

        private static void SetupSceneBookmarks()
        {
            GameObject bookmarksGroup = GameObject.Find("_Marcadores_Cenário");
            if (bookmarksGroup != null)
            {
                Object.DestroyImmediate(bookmarksGroup);
            }

            bookmarksGroup = new GameObject("_Marcadores_Cenário");

            CreateBookmark(bookmarksGroup.transform, "Marcador_01_Acampamento", "1. Acampamento Base & Biólogo Guia", 1, new Vector3(0f, 1.5f, 1.5f), Quaternion.Euler(12f, 0f, 0f));
            CreateBookmark(bookmarksGroup.transform, "Marcador_02_Tamandua", "2. Savana dos Tamanduás, Tatu e Coelho", 2, new Vector3(4f, 1.5f, 4.5f), Quaternion.Euler(15f, 25f, 0f));
            CreateBookmark(bookmarksGroup.transform, "Marcador_03_Ema_Jacare", "3. Vereda das Emas e Jacaré-de-papo-amarelo", 3, new Vector3(-7f, 1.5f, 7.5f), Quaternion.Euler(15f, -25f, 0f));
            CreateBookmark(bookmarksGroup.transform, "Marcador_04_Lobo_Onca", "4. Habitat do Lobo-Guará e Onça-parda", 4, new Vector3(11f, 2f, 14f), Quaternion.Euler(15f, 18f, 0f));
            CreateBookmark(bookmarksGroup.transform, "Marcador_05_Flora", "5. Bosque Nativo (Jatobá, Angico, Aroeira, Chuveirinho)", 5, new Vector3(0f, 2.5f, 11f), Quaternion.Euler(15f, 0f, 0f));
        }

        private static void CreateBookmark(Transform parent, string name, string headline, int id, Vector3 pos, Quaternion rot)
        {
            GameObject bookmark = new GameObject(name);
            bookmark.transform.SetParent(parent);
            float groundY = SampleGroundHeight(pos);
            bookmark.transform.position = new Vector3(pos.x, groundY + pos.y, pos.z);
            bookmark.transform.rotation = rot;

            var annotation = bookmark.AddComponent<SceneAnnotation>();
            annotation.headline = headline;
            annotation.id = id;
        }

        /// <summary>
        /// Realinha qualquer AnimalSpawner ou barreira existente na cena ao novo relevo do terreno_NOVO.
        /// </summary>
        private static void AlignRemainingSceneObjectsToTerrain()
        {
            var spawners = Object.FindObjectsByType<AnimalSpawner>(FindObjectsInactive.Include);
            foreach (var sp in spawners)
            {
                if (sp == null) continue;
                Vector3 p = sp.transform.position;
                p.y = SampleGroundHeight(p);
                sp.transform.position = p;
                EditorUtility.SetDirty(sp.transform);
            }

            var allBehaviours = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include);
            foreach (var mb in allBehaviours)
            {
                if (mb == null) continue;
                if (mb.GetType().Name == "AreaBarrier")
                {
                    Vector3 p = mb.transform.position;
                    p.y = SampleGroundHeight(p);
                    mb.transform.position = p;
                    EditorUtility.SetDirty(mb.transform);
                }
            }
        }

        private static float SampleGroundHeight(Vector3 pos)
        {
            SampleGroundSurface(pos, out float y, out _);
            return y;
        }

        private static void SampleGroundSurface(Vector3 pos, out float y, out Vector3 normal)
        {
            Ray ray = new Ray(new Vector3(pos.x, 85f, pos.z), Vector3.down);
            if (activeTerrainCollider != null && activeTerrainCollider.Raycast(ray, out RaycastHit directHit, 170f))
            {
                y = directHit.point.y;
                normal = directHit.normal;
                return;
            }

            var hits = Physics.RaycastAll(ray, 170f);
            foreach (var h in hits)
            {
                if (h.collider != null && (h.collider.name.Contains("Chao_Cerrado") || h.collider.name.Contains("Terreno_NOVO") || h.collider.name.Contains("TERRENO")))
                {
                    y = h.point.y;
                    normal = h.normal;
                    return;
                }
            }
            y = 0f;
            normal = Vector3.up;
        }

        /// <summary>
        /// Escala proporcionalmente o modelo filho 3D ("Modelo_3D") mantendo o nó pai (rootObj)
        /// com localScale = (1,1,1) e position.y = groundY sobre a NavMesh.
        /// Evita double-scale no SkinnedMeshRenderer e garante que árvores e patas toquem o solo.
        /// </summary>
        private static void NormalizeVisualChildTransform(
            GameObject rootObj,
            GameObject visualObj,
            string modelPath,
            float targetMaxDimension,
            float verticalSinkOffset,
            bool isTreeOrPlant)
        {
            if (rootObj == null || visualObj == null) return;

            string lowerPath = (modelPath ?? "").ToLower();

            // 1. Para FBX legados (Onça-parda, Coelho, Tamanduá NPC) cujas patas foram niveladas no Blender:
            float knownAnimalNativeRef = -1f;
            if (!lowerPath.EndsWith(".prefab"))
            {
                if (lowerPath.Contains("oncaparda")) knownAnimalNativeRef = 5.730f; // Comprimento nativo 5.73m -> target 1.76m (~0.84m altura no ombro)
                else if (lowerPath.Contains("tamandua_npc")) knownAnimalNativeRef = 4.805f; // Altura nativa 4.805m -> target 1.72m
                else if (lowerPath.Contains("/ema.fbx")) knownAnimalNativeRef = 0.615f;
                else if (lowerPath.Contains("coelho")) knownAnimalNativeRef = 6.654f; // Altura nativa 6.654m -> target 0.38m
            }

            if (knownAnimalNativeRef > 0f)
            {
                float scale = targetMaxDimension / knownAnimalNativeRef;
                visualObj.transform.localScale = Vector3.one * scale;
                // Leve contato de 2.5cm das patas com o solo para nunca haver vão entre a pata e a sombra
                visualObj.transform.localPosition = new Vector3(0f, -0.025f - verticalSinkOffset, 0f);
                return;
            }

            // 2. Para todos os modelos migrados (PFB_* em Fauna/Flora) e .FBX de Flora:
            Bounds totalBounds = ComputeExactWorldBounds(visualObj, isTreeOrPlant, lowerPath);
            if (totalBounds.size.sqrMagnitude < 1e-8f) return;

            // Escala árvores pela altura (bounds.size.y) e animais migrados pelo maior eixo (bounds.size.max)
            float refDimension = isTreeOrPlant
                ? Mathf.Max(0.001f, totalBounds.size.y)
                : Mathf.Max(0.001f, Mathf.Max(totalBounds.size.x, Mathf.Max(totalBounds.size.y, totalBounds.size.z)));
            if (refDimension > 1e-5f)
            {
                float scaleFactor = targetMaxDimension / refDimension;
                visualObj.transform.localScale *= scaleFactor;
            }

            Physics.SyncTransforms();
            totalBounds = ComputeExactWorldBounds(visualObj, isTreeOrPlant, lowerPath);

            // Para árvores/plantas, calcula o min.y especificamente do tronco/caule para que nenhuma folha baixa levante o tronco
            float baseMinY = totalBounds.min.y;
            if (isTreeOrPlant)
            {
                var allRenderers = visualObj.GetComponentsInChildren<Renderer>(true);
                if (allRenderers.Length > 1)
                {
                    bool foundTrunk = false;
                    float trunkMinY = float.MaxValue;
                    foreach (var mr in allRenderers)
                    {
                        if (mr == null || !mr.gameObject.activeInHierarchy) continue;
                        string rn = mr.gameObject.name.ToLower();
                        if (!rn.Contains("folha") && !rn.Contains("leaf") && !rn.Contains("leaves") && !rn.Contains("branch") && !rn.Contains("flor") && !rn.Contains("pequis"))
                        {
                            if (mr.bounds.min.y < trunkMinY)
                            {
                                trunkMinY = mr.bounds.min.y;
                                foundTrunk = true;
                            }
                        }
                    }
                    if (foundTrunk)
                    {
                        baseMinY = trunkMinY;
                    }
                }
            }

            float groundY = rootObj.transform.position.y;
            float offsetX = rootObj.transform.position.x - totalBounds.center.x;
            float offsetZ = rootObj.transform.position.z - totalBounds.center.z;
            float offsetY = (groundY - verticalSinkOffset) - baseMinY;

            visualObj.transform.position += new Vector3(offsetX, offsetY, offsetZ);
        }

        private static Bounds ComputeExactWorldBounds(GameObject visualObj, bool isTreeOrPlant, string lowerPath)
        {
            if (isTreeOrPlant && lowerPath.Contains("arvoreseca"))
            {
                Vector3 euler = visualObj.transform.localEulerAngles;
                if (Mathf.Abs(Mathf.DeltaAngle(euler.x, 0f)) < 15f)
                {
                    visualObj.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                }
            }

            var allRenderers = visualObj.GetComponentsInChildren<Renderer>(true);
            Bounds totalBounds = new Bounds(visualObj.transform.position, Vector3.zero);
            bool hasBounds = false;

            foreach (var r in allRenderers)
            {
                if (r == null || !r.gameObject.activeInHierarchy || r is ParticleSystemRenderer) continue;

                // Força atualização síncrona do AABB do SkinnedMeshRenderer no Edit Mode após mudança de transform/escala
                Bounds rb = r.bounds;
                if (r is SkinnedMeshRenderer smr)
                {
                    if ((smr.bones == null || smr.bones.Length == 0) && smr.sharedMesh != null)
                    {
                        // Malha pura de BlendShapes sem ossos (ex.: Onça-parda): calcula bounds reais a partir do sharedMesh
                        Bounds mb = smr.sharedMesh.bounds;
                        smr.localBounds = mb;
                        Vector3 c = mb.center;
                        Vector3 e = mb.extents;
                        Matrix4x4 m = smr.transform.localToWorldMatrix;
                        rb = new Bounds(m.MultiplyPoint3x4(c + new Vector3(-e.x, -e.y, -e.z)), Vector3.zero);
                        for (int xi = -1; xi <= 1; xi += 2)
                            for (int yi = -1; yi <= 1; yi += 2)
                                for (int zi = -1; zi <= 1; zi += 2)
                                    rb.Encapsulate(m.MultiplyPoint3x4(c + new Vector3(e.x * xi, e.y * yi, e.z * zi)));
                    }
                    else
                    {
                        smr.localBounds = smr.localBounds;
                        rb = r.bounds;
                    }
                }

                if (!hasBounds) { totalBounds = rb; hasBounds = true; }
                else totalBounds.Encapsulate(rb);
            }

            return totalBounds;
        }

        /// <summary>
        /// Conecta um AnimatorController funcional com os AnimationClips reais (Idle / Walk / Run)
        /// controlado pela velocidade do NavMeshAgent e adiciona FootPlanter para alinhamento ao relevo.
        /// </summary>
        private static void ConfigureAnimalAnimations(
            GameObject rootObj, GameObject visualObj, string animalName, string modelPath,
            string idleClipPath = null, string walkClipPath = null, string runClipPath = null,
            float idleAnimSpeed = 1.0f, bool isBlendShapeAnimal = false, bool reuseExistingController = false)
        {
            if (rootObj == null || visualObj == null) return;

            // Remove antigo ProceduralAnimalMotion se existir
            var legacyProc = rootObj.GetComponent<ProceduralAnimalMotion>();
            if (legacyProc != null)
            {
                Object.DestroyImmediate(legacyProc);
            }

            // Suçuarana (Onça-parda) usa malha com 53 BlendShapes + OncaBlendShapeController!
            if (isBlendShapeAnimal)
            {
                var smr = visualObj.GetComponentInChildren<SkinnedMeshRenderer>(true);
                if (smr != null && smr.sharedMesh != null && smr.sharedMesh.blendShapeCount > 0)
                {
                    var bsCtrl = GetOrAddComponent<OncaBlendShapeController>(rootObj);
                    var soBs = new SerializedObject(bsCtrl);
                    soBs.FindProperty("skinnedMeshRenderer").objectReferenceValue = smr;
                    soBs.ApplyModifiedProperties();

                    var fpOnca = GetOrAddComponent<FootPlanter>(rootObj);
                    var soFpOnca = new SerializedObject(fpOnca);
                    soFpOnca.FindProperty("visualModel").objectReferenceValue = visualObj.transform;
                    soFpOnca.FindProperty("hasSkeletalClips").boolValue = true;
                    soFpOnca.ApplyModifiedProperties();
                    return;
                }
            }

            var clips = new List<AnimationClip>();
            if (!string.IsNullOrEmpty(idleClipPath))
            {
                var c = AssetDatabase.LoadAssetAtPath<AnimationClip>(idleClipPath);
                if (c != null) clips.Add(c);
            }
            if (!string.IsNullOrEmpty(walkClipPath))
            {
                var c = AssetDatabase.LoadAssetAtPath<AnimationClip>(walkClipPath);
                if (c != null) clips.Add(c);
            }
            if (!string.IsNullOrEmpty(runClipPath))
            {
                var c = AssetDatabase.LoadAssetAtPath<AnimationClip>(runClipPath);
                if (c != null) clips.Add(c);
            }

            if (clips.Count == 0 && !string.IsNullOrEmpty(modelPath) && !modelPath.EndsWith(".prefab", System.StringComparison.OrdinalIgnoreCase))
            {
                var subAssets = AssetDatabase.LoadAllAssetsAtPath(modelPath);
                foreach (var sub in subAssets)
                {
                    if (sub is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                    {
                        clips.Add(clip);
                    }
                }
            }

            bool isFallbackGenerated = false;
            if (clips.Count == 0)
            {
                isFallbackGenerated = true;
                EnsureFallbackLocomotionClips(animalName, out AnimationClip fbIdle, out AnimationClip fbWalk);
                if (fbIdle != null) clips.Add(fbIdle);
                if (fbWalk != null) clips.Add(fbWalk);
            }

            Animator animator = visualObj.GetComponentInChildren<Animator>();
            bool hasValidClips = clips.Count > 0;

            if (hasValidClips)
            {
                if (animator == null)
                {
                    animator = visualObj.AddComponent<Animator>();
                }

                AnimationClip idleClip = !string.IsNullOrEmpty(idleClipPath) ? AssetDatabase.LoadAssetAtPath<AnimationClip>(idleClipPath) : clips[0];
                AnimationClip walkClip = !string.IsNullOrEmpty(walkClipPath) ? AssetDatabase.LoadAssetAtPath<AnimationClip>(walkClipPath) : (clips.Count > 1 ? clips[1] : clips[0]);
                AnimationClip runClip = !string.IsNullOrEmpty(runClipPath) ? AssetDatabase.LoadAssetAtPath<AnimationClip>(runClipPath) : null;

                foreach (var c in clips)
                {
                    if (c == null) continue;
                    string cn = c.name.ToLower();
                    if (string.IsNullOrEmpty(idleClipPath) && cn.Contains("idle")) idleClip = c;
                    else if (string.IsNullOrEmpty(walkClipPath) && (cn.Contains("walk") || cn.Contains("action"))) walkClip = c;
                    else if (string.IsNullOrEmpty(runClipPath) && cn.Contains("run")) runClip = c;

                    var settings = AnimationUtility.GetAnimationClipSettings(c);
                    if (!settings.loopTime)
                    {
                        settings.loopTime = true;
                        AnimationUtility.SetAnimationClipSettings(c, settings);
                    }
                }

                if (!AssetDatabase.IsValidFolder("Assets/Dados/Animacoes"))
                {
                    if (!AssetDatabase.IsValidFolder("Assets/Dados")) AssetDatabase.CreateFolder("Assets", "Dados");
                    AssetDatabase.CreateFolder("Assets/Dados", "Animacoes");
                }

                string ctrlPath = $"Assets/Dados/Animacoes/AnimCtrl_{animalName}.controller";
                var ctrl = reuseExistingController
                    ? AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(ctrlPath)
                    : null;

                if (ctrl == null)
                {
                    var existingCtrl = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(ctrlPath);
                    if (existingCtrl != null)
                    {
                        AssetDatabase.DeleteAsset(ctrlPath);
                    }

                    ctrl = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
                    ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
                    ctrl.AddParameter("NormSpeed", AnimatorControllerParameterType.Float);
                    ctrl.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsAlert", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsFleeing", AnimatorControllerParameterType.Bool);

                    var rootStateMachine = ctrl.layers[0].stateMachine;
                    var stateIdle = rootStateMachine.AddState("Idle");
                    stateIdle.motion = idleClip;
                    stateIdle.speed = idleAnimSpeed;
                    rootStateMachine.defaultState = stateIdle;

                    UnityEditor.Animations.AnimatorState stateMove;
                    if (runClip != null)
                    {
                        // Blend Tree baseado na velocidade real do NavMeshAgent (Walk <-> Run, ex: Veado-campeiro)
                        var tree = new UnityEditor.Animations.BlendTree
                        {
                            name = "LocomotionBlendTree",
                            blendType = UnityEditor.Animations.BlendTreeType.Simple1D,
                            blendParameter = "Speed",
                            useAutomaticThresholds = false
                        };
                        tree.AddChild(walkClip, 1.5f);
                        tree.AddChild(runClip, 5.2f);
                        AssetDatabase.AddObjectToAsset(tree, ctrl);

                        stateMove = rootStateMachine.AddState("Locomotion");
                        stateMove.motion = tree;
                        stateMove.speed = 1.0f;
                    }
                    else
                    {
                        stateMove = rootStateMachine.AddState("Walk");
                        stateMove.motion = walkClip;
                        stateMove.speed = 1.0f;
                        stateMove.speedParameterActive = true;
                        stateMove.speedParameter = "NormSpeed";
                    }

                    var toWalk = stateIdle.AddTransition(stateMove);
                    toWalk.hasExitTime = false;
                    toWalk.duration = 0.2f;
                    toWalk.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.08f, "Speed");

                    var toIdle = stateMove.AddTransition(stateIdle);
                    toIdle.hasExitTime = false;
                    toIdle.duration = 0.2f;
                    toIdle.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.08f, "Speed");

                    EditorUtility.SetDirty(ctrl);
                }
                animator.runtimeAnimatorController = ctrl;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

                var ai = rootObj.GetComponent<AnimalAI>();
                if (ai != null)
                {
                    var soAI = new SerializedObject(ai);
                    soAI.FindProperty("animator").objectReferenceValue = animator;
                    soAI.ApplyModifiedProperties();
                }
            }

            var footPlanter = GetOrAddComponent<FootPlanter>(rootObj);
            var soFP = new SerializedObject(footPlanter);
            soFP.FindProperty("visualModel").objectReferenceValue = visualObj.transform;
            soFP.FindProperty("hasSkeletalClips").boolValue = hasValidClips && !isFallbackGenerated;
            soFP.ApplyModifiedProperties();
        }

        private static void EnsureFallbackLocomotionClips(string animalName, out AnimationClip idleClip, out AnimationClip walkClip)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Dados/Animacoes"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Dados")) AssetDatabase.CreateFolder("Assets", "Dados");
                AssetDatabase.CreateFolder("Assets/Dados", "Animacoes");
            }

            string idlePath = $"Assets/Dados/Animacoes/Anim_Idle_{animalName}.anim";
            string walkPath = $"Assets/Dados/Animacoes/Anim_Walk_{animalName}.anim";

            idleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(idlePath);
            if (idleClip == null)
            {
                idleClip = new AnimationClip { name = $"Idle_{animalName}" };
                var curve = AnimationCurve.EaseInOut(0f, 0f, 1.5f, 0f);
                idleClip.SetCurve("", typeof(Animator), "IdlePhase", curve);
                var settings = AnimationUtility.GetAnimationClipSettings(idleClip);
                settings.loopTime = true;
                AnimationUtility.SetAnimationClipSettings(idleClip, settings);
                AssetDatabase.CreateAsset(idleClip, idlePath);
            }

            walkClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(walkPath);
            if (walkClip == null)
            {
                walkClip = new AnimationClip { name = $"Walk_{animalName}" };
                var curve = AnimationCurve.Linear(0f, 0f, 0.8f, 1f);
                walkClip.SetCurve("", typeof(Animator), "WalkPhase", curve);
                var settings = AnimationUtility.GetAnimationClipSettings(walkClip);
                settings.loopTime = true;
                AnimationUtility.SetAnimationClipSettings(walkClip, settings);
                AssetDatabase.CreateAsset(walkClip, walkPath);
            }
        }

        private static void SetupAnimal(
            Transform parent, string name, string controllerBaseName, string modelPath, string speciesPath,
            Vector3 position, AnimalTemperament temperament, float walkSpeed, float fleeSpeed,
            float colliderRadius, float colliderHeight, float targetMaxSize,
            Color tintColor, string diffuseTexPath, string normalTexPath = null,
            string idleClipPath = null, string walkClipPath = null, string runClipPath = null,
            float idleAnimSpeed = 1.0f, bool isBlendShapeAnimal = false, bool reuseExistingController = false)
        {
            GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (modelPrefab == null)
            {
                Debug.LogWarning($"[CerradoSceneSetup] Modelo 3D '{modelPath}' ainda não importado pelo AssetDatabase.");
                return;
            }

            float groundY = SampleGroundHeight(position);
            Vector3 snappedPos = new Vector3(position.x, groundY, position.z);
            if (NavMesh.SamplePosition(snappedPos, out NavMeshHit navHit, 12f, NavMesh.AllAreas))
            {
                snappedPos = new Vector3(navHit.position.x, SampleGroundHeight(navHit.position), navHit.position.z);
            }

            // Container raiz limpo com escala (1,1,1) e posição Y = groundY cravada na NavMesh
            GameObject animalRoot = new GameObject(name);
            animalRoot.transform.SetParent(parent, false);
            animalRoot.transform.position = snappedPos;
            animalRoot.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            animalRoot.transform.localScale = Vector3.one;

            int animalLayer = LayerMask.NameToLayer("Animal");
            if (animalLayer >= 0) animalRoot.layer = animalLayer;

            GameObject visualObj = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab, animalRoot.transform);
            visualObj.name = "Modelo_3D";
            if (!modelPath.Contains("/Prefabs/") && !string.IsNullOrEmpty(diffuseTexPath))
            {
                ApplyAnimalMaterial(visualObj, "Mat_" + controllerBaseName, tintColor, diffuseTexPath, normalTexPath);
            }

            NormalizeVisualChildTransform(animalRoot, visualObj, modelPath, targetMaxSize, 0.0f, false);

            var col = GetOrAddComponent<CapsuleCollider>(animalRoot);
            col.radius = colliderRadius;
            col.height = colliderHeight;
            col.center = new Vector3(0f, colliderHeight * 0.5f, 0f);

            var agent = GetOrAddComponent<NavMeshAgent>(animalRoot);
            agent.radius = colliderRadius;
            agent.height = colliderHeight;
            agent.speed = walkSpeed;
            agent.angularSpeed = 150f;
            agent.acceleration = 4.5f;
            agent.autoBraking = true;
            agent.baseOffset = 0f; // Garante que nenhum animal flutue acima do solo!
            var soAgent = new SerializedObject(agent);
            if (soAgent.FindProperty("m_BaseOffset") != null)
            {
                soAgent.FindProperty("m_BaseOffset").floatValue = 0f;
                soAgent.ApplyModifiedPropertiesWithoutUndo();
            }

            // Garante ausência de conflito de Rigidbody com NavMeshAgent
            var rbs = animalRoot.GetComponentsInChildren<Rigidbody>(true);
            foreach (var rb in rbs)
            {
                if (rb != null)
                {
                    rb.isKinematic = true;
                    rb.useGravity = false;
                }
            }

            var ai = GetOrAddComponent<AnimalAI>(animalRoot);
            var soAI = new SerializedObject(ai);
            soAI.FindProperty("temperament").enumValueIndex = (int)temperament;
            soAI.FindProperty("walkSpeed").floatValue = walkSpeed;
            soAI.FindProperty("fleeSpeed").floatValue = fleeSpeed;
            soAI.ApplyModifiedProperties();

            ConfigureAnimalAnimations(animalRoot, visualObj, controllerBaseName, modelPath, idleClipPath, walkClipPath, runClipPath, idleAnimSpeed, isBlendShapeAnimal, reuseExistingController);

            GetOrAddComponent<AnimalDustTrail>(animalRoot);

            var target = GetOrAddComponent<PhotographableTarget>(animalRoot);
            var speciesData = AssetDatabase.LoadAssetAtPath<SpeciesData>(speciesPath);
            var soTarget = new SerializedObject(target);
            soTarget.FindProperty("speciesData").objectReferenceValue = speciesData;
            soTarget.FindProperty("focalPointOffset").vector3Value = new Vector3(0f, colliderHeight * 0.5f, 0f);
            soTarget.ApplyModifiedProperties();
        }

        private static void SetupPlant(
            Transform parent, string name, string modelPath, string speciesPath, Vector3 position,
            float targetMaxSize, Material trunkOrStemMat, Material foliageOrFlowerMat, bool isSmallPlant = false)
        {
            GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (modelPrefab == null)
            {
                Debug.LogWarning($"[CerradoSceneSetup] Modelo 3D '{modelPath}' ainda não importado pelo AssetDatabase.");
                return;
            }

            float groundY = SampleGroundHeight(position);

            GameObject plantRoot = new GameObject(name);
            plantRoot.transform.SetParent(parent, false);
            plantRoot.transform.position = new Vector3(position.x, groundY, position.z);
            plantRoot.transform.rotation = isSmallPlant
                ? Quaternion.Euler(0f, Random.Range(0f, 360f), 0f)
                : SampleNaturalTreeRotation();
            plantRoot.transform.localScale = Vector3.one;

            GameObject visualObj = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab, plantRoot.transform);
            visualObj.name = "Modelo_3D";
            if (!modelPath.Contains("/Prefabs/") && trunkOrStemMat != null)
            {
                ApplyPlantMultiMaterials(visualObj, trunkOrStemMat, foliageOrFlowerMat);
            }

            // Afunda levemente a base do tronco no solo (~0.20m para árvores inclinadas/grandes, ~0.04m para plantas pequenas)
            float sinkOffset = isSmallPlant ? 0.04f : 0.20f;
            NormalizeVisualChildTransform(plantRoot, visualObj, modelPath, targetMaxSize, sinkOffset, true);

            var col = GetOrAddComponent<CapsuleCollider>(plantRoot);
            col.radius = isSmallPlant ? 0.45f : Mathf.Clamp(targetMaxSize * 0.11f, 0.40f, 0.95f);
            col.height = isSmallPlant ? 1.2f : Mathf.Clamp(targetMaxSize * 0.72f, 2.0f, 5.5f);
            col.center = new Vector3(0f, col.height * 0.5f, 0f);

            var target = GetOrAddComponent<PhotographableTarget>(plantRoot);
            var speciesData = AssetDatabase.LoadAssetAtPath<SpeciesData>(speciesPath);
            var soTarget = new SerializedObject(target);
            soTarget.FindProperty("speciesData").objectReferenceValue = speciesData;
            soTarget.FindProperty("focalPointOffset").vector3Value = new Vector3(0f, isSmallPlant ? 0.6f : 2.3f, 0f);
            soTarget.ApplyModifiedProperties();
        }

        private static void SetupUI(PlayerController playerController, GuideNPC guideNPC, ShopNPC shopNPC)
        {
            GameObject existingCanvas = GameObject.Find("UI_Canvas");
            if (existingCanvas != null)
            {
                Object.DestroyImmediate(existingCanvas);
            }

            GameObject canvasObj = new GameObject("UI_Canvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }

            // 1. Visor da Câmera
            SetupViewfinderUI(canvasObj.transform);

            // 2. HUD de Missões + Indicadores de Atalhos
            SetupQuestHUDUI(canvasObj.transform);

            // 3. Diálogos do Guia / Onboarding
            SetupDialogueUI(canvasObj.transform, guideNPC, shopNPC);

            // 4. Painel do Álbum Estilo Caderno de Campo Científico (Tab / J)
            var albumUI = SetupAlbumUI(canvasObj.transform, playerController);

            // 5. Painel da Loja (Via Guia)
            var shopUI = SetupShopUI(canvasObj.transform, playerController);

            // 6. Painel de Tutorial Rápido (primeira entrada no jogo / botão Ver Tutorial)
            var tutorialUI = CerradoSettingsUIBuilder.BuildFirstTimeTutorialOverlay(canvasObj.transform, playerController);

            // 7. Menu de Pausa (ESC / P) com Opções completas e retorno ao Menu Principal
            SetupPauseMenuUI(canvasObj.transform, playerController, albumUI, shopUI, tutorialUI);
        }

        private static Sprite LoadUISprite(string fileName)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Jogo/UI/Sprites/{fileName}.png");
        }

        private static void ApplyWoodSpriteIfAvailable(Image img, string spriteName, Color fallbackColor)
        {
            if (img == null) return;
            Sprite sp = LoadUISprite(spriteName);
            if (sp != null)
            {
                img.sprite = sp;
                img.color = Color.white;
            }
            else
            {
                img.color = fallbackColor;
            }
        }

        private static void SetupDialogueUI(Transform canvas, GuideNPC guideNPC, ShopNPC shopNPC)
        {
            GameObject dialogueRoot = GetOrCreateChild(canvas, "HUD_Dialogo_Guia");

            GameObject prompt = GetOrCreateChild(dialogueRoot.transform, "Prompt_Interacao");
            var rectPrompt = GetOrAddComponent<RectTransform>(prompt);
            rectPrompt.anchorMin = new Vector2(0.5f, 0.25f);
            rectPrompt.anchorMax = new Vector2(0.5f, 0.25f);
            rectPrompt.sizeDelta = new Vector2(540, 52);

            var imgPromptBg = GetOrAddComponent<Image>(prompt);
            imgPromptBg.color = new Color(0.08f, 0.12f, 0.08f, 0.88f);

            GameObject txtPrompt = GetOrCreateChild(prompt.transform, "TextoPrompt");
            var tmpPrompt = GetOrAddComponent<TextMeshProUGUI>(txtPrompt);
            tmpPrompt.fontSize = 20;
            tmpPrompt.fontStyle = FontStyles.Bold;
            tmpPrompt.alignment = TextAlignmentOptions.Center;
            tmpPrompt.text = "[E] Falar com o Biólogo / Loja de Equipamentos";
            SetFullStretch(txtPrompt.GetComponent<RectTransform>());

            GameObject panel = GetOrCreateChild(dialogueRoot.transform, "Painel_Conversa");
            var rectPanel = GetOrAddComponent<RectTransform>(panel);
            rectPanel.anchorMin = new Vector2(0.5f, 0);
            rectPanel.anchorMax = new Vector2(0.5f, 0);
            rectPanel.pivot = new Vector2(0.5f, 0);
            rectPanel.anchoredPosition = new Vector2(0, 25);
            rectPanel.sizeDelta = new Vector2(1060, 230);

            var imgPanelBg = GetOrAddComponent<Image>(panel);
            imgPanelBg.color = new Color(0.09f, 0.13f, 0.09f, 0.96f);

            GameObject txtSpeaker = GetOrCreateChild(panel.transform, "NomePalestrante");
            var tmpSpeaker = GetOrAddComponent<TextMeshProUGUI>(txtSpeaker);
            tmpSpeaker.fontSize = 24;
            tmpSpeaker.fontStyle = FontStyles.Bold;
            tmpSpeaker.color = new Color(0.96f, 0.82f, 0.35f);
            var rectSpeaker = txtSpeaker.GetComponent<RectTransform>();
            rectSpeaker.anchoredPosition = new Vector2(0, 72);
            rectSpeaker.sizeDelta = new Vector2(940, 35);

            GameObject txtBody = GetOrCreateChild(panel.transform, "CorpoTexto");
            var tmpBody = GetOrAddComponent<TextMeshProUGUI>(txtBody);
            tmpBody.fontSize = 20;
            tmpBody.color = Color.white;
            var rectBody = txtBody.GetComponent<RectTransform>();
            rectBody.anchoredPosition = new Vector2(0, 10);
            rectBody.sizeDelta = new Vector2(940, 85);

            GameObject btnNextObj = GetOrCreateChild(panel.transform, "Btn_Avancar");
            var rectBtnNext = GetOrAddComponent<RectTransform>(btnNextObj);
            rectBtnNext.anchorMin = new Vector2(1, 0);
            rectBtnNext.anchorMax = new Vector2(1, 0);
            rectBtnNext.pivot = new Vector2(1, 0);
            rectBtnNext.anchoredPosition = new Vector2(-45, 22);
            rectBtnNext.sizeDelta = new Vector2(180, 44);

            var btnNext = GetOrAddComponent<Button>(btnNextObj);
            var imgBtnNext = GetOrAddComponent<Image>(btnNextObj);
            imgBtnNext.color = new Color(0.20f, 0.58f, 0.28f);

            GameObject txtBtnNext = GetOrCreateChild(btnNextObj.transform, "Texto");
            var tmpBtnNext = GetOrAddComponent<TextMeshProUGUI>(txtBtnNext);
            tmpBtnNext.text = "Avançar [E] >";
            tmpBtnNext.fontSize = 18;
            tmpBtnNext.fontStyle = FontStyles.Bold;
            tmpBtnNext.alignment = TextAlignmentOptions.Center;
            SetFullStretch(txtBtnNext.GetComponent<RectTransform>());

            btnNext.onClick.RemoveAllListeners();
            if (guideNPC != null)
            {
                UnityEditor.Events.UnityEventTools.AddPersistentListener(btnNext.onClick, guideNPC.AdvanceDialogue);
            }

            GameObject btnShopObj = GetOrCreateChild(panel.transform, "Btn_Loja");
            var rectBtnShop = GetOrAddComponent<RectTransform>(btnShopObj);
            rectBtnShop.anchorMin = new Vector2(1, 0);
            rectBtnShop.anchorMax = new Vector2(1, 0);
            rectBtnShop.pivot = new Vector2(1, 0);
            rectBtnShop.anchoredPosition = new Vector2(-240, 22);
            rectBtnShop.sizeDelta = new Vector2(220, 44);

            var btnShop = GetOrAddComponent<Button>(btnShopObj);
            var imgBtnShop = GetOrAddComponent<Image>(btnShopObj);
            imgBtnShop.color = new Color(0.72f, 0.48f, 0.16f);

            GameObject txtBtnShop = GetOrCreateChild(btnShopObj.transform, "Texto");
            var tmpBtnShop = GetOrAddComponent<TextMeshProUGUI>(txtBtnShop);
            tmpBtnShop.text = "Loja de Câmeras";
            tmpBtnShop.fontSize = 18;
            tmpBtnShop.fontStyle = FontStyles.Bold;
            tmpBtnShop.alignment = TextAlignmentOptions.Center;
            SetFullStretch(txtBtnShop.GetComponent<RectTransform>());

            btnShop.onClick.RemoveAllListeners();
            if (guideNPC != null)
            {
                UnityEditor.Events.UnityEventTools.AddPersistentListener(btnShop.onClick, guideNPC.OpenShopFromGuide);
            }

            panel.SetActive(false);
            prompt.SetActive(false);

            if (guideNPC != null)
            {
                var soGuide = new SerializedObject(guideNPC);
                soGuide.FindProperty("promptUI").objectReferenceValue = prompt;
                soGuide.FindProperty("dialoguePanel").objectReferenceValue = panel;
                soGuide.FindProperty("dialogueSpeakerText").objectReferenceValue = tmpSpeaker;
                soGuide.FindProperty("dialogueBodyText").objectReferenceValue = tmpBody;
                soGuide.ApplyModifiedProperties();
            }

            if (shopNPC != null)
            {
                var soShop = new SerializedObject(shopNPC);
                soShop.FindProperty("interactionPromptUI").objectReferenceValue = prompt;
                soShop.ApplyModifiedProperties();
            }
        }

        private static void SetupViewfinderUI(Transform canvas)
        {
            GameObject viewfinderRoot = GetOrCreateChild(canvas, "Visor_Fotografia");
            SetFullStretch(GetOrAddComponent<RectTransform>(viewfinderRoot));
            var viewfinderUI = GetOrAddComponent<PhotoViewfinderUI>(viewfinderRoot);

            // Reconstrói o container limpo (remove qualquer FocusFrame verde antigo!)
            Transform oldContainer = viewfinderRoot.transform.Find("Viewfinder_Container");
            if (oldContainer != null) Object.DestroyImmediate(oldContainer.gameObject);

            GameObject container = new GameObject("Viewfinder_Container");
            container.transform.SetParent(viewfinderRoot.transform, false);
            SetFullStretch(GetOrAddComponent<RectTransform>(container));

            Color whiteCrisp = new Color(1f, 1f, 1f, 0.92f);
            Color whiteSoft  = new Color(1f, 1f, 1f, 0.72f);

            // 1. Cantoneiras Brancas Clássicas nos 4 Cantos da Tela (estilo Viewfinder)
            CreateViewfinderCorner(container.transform, "Canto_TL", new Vector2(0, 1), new Vector2(54, -44), 1f, -1f, whiteCrisp);
            CreateViewfinderCorner(container.transform, "Canto_TR", new Vector2(1, 1), new Vector2(-54, -44), -1f, -1f, whiteCrisp);
            CreateViewfinderCorner(container.transform, "Canto_BL", new Vector2(0, 0), new Vector2(54, 44), 1f, 1f, whiteCrisp);
            CreateViewfinderCorner(container.transform, "Canto_BR", new Vector2(1, 0), new Vector2(-54, 44), -1f, 1f, whiteCrisp);

            // 2. Retículo Central Branco Limpo com '+' no Meio (SEM quadrado verde!)
            GameObject crosshairObj = new GameObject("Reticle_Center");
            crosshairObj.transform.SetParent(container.transform, false);
            var rectCross = GetOrAddComponent<RectTransform>(crosshairObj);
            rectCross.anchorMin = new Vector2(0.5f, 0.5f);
            rectCross.anchorMax = new Vector2(0.5f, 0.5f);
            rectCross.sizeDelta = new Vector2(32f, 32f);
            var imgCrosshair = GetOrAddComponent<Image>(crosshairObj);
            imgCrosshair.color = new Color(1f, 1f, 1f, 0.01f); // invisível, apenas container/cor controlada
            imgCrosshair.raycastTarget = false;

            CreateWhiteLine(crosshairObj.transform, "Plus_H", Vector2.zero, new Vector2(26f, 2.5f), whiteCrisp);
            CreateWhiteLine(crosshairObj.transform, "Plus_V", Vector2.zero, new Vector2(2.5f, 26f), whiteCrisp);
            // Pequenas cantoneiras internas brancas sutis ao redor do retículo
            CreateViewfinderCorner(crosshairObj.transform, "Inner_TL", new Vector2(0.5f, 0.5f), new Vector2(-58f, 40f), 1f, -1f, whiteSoft, 18f, 2f);
            CreateViewfinderCorner(crosshairObj.transform, "Inner_TR", new Vector2(0.5f, 0.5f), new Vector2(58f, 40f), -1f, -1f, whiteSoft, 18f, 2f);
            CreateViewfinderCorner(crosshairObj.transform, "Inner_BL", new Vector2(0.5f, 0.5f), new Vector2(-58f, -40f), 1f, 1f, whiteSoft, 18f, 2f);
            CreateViewfinderCorner(crosshairObj.transform, "Inner_BR", new Vector2(0.5f, 0.5f), new Vector2(58f, -40f), -1f, 1f, whiteSoft, 18f, 2f);

            // 3. Indicador Superior Esquerdo: Ponto Vermelho '●' + 'REC' + Timecode
            GameObject recGroup = new GameObject("Indicador_REC");
            recGroup.transform.SetParent(container.transform, false);
            var rRec = GetOrAddComponent<RectTransform>(recGroup);
            rRec.anchorMin = new Vector2(0, 1);
            rRec.anchorMax = new Vector2(0, 1);
            rRec.pivot = new Vector2(0, 1);
            rRec.anchoredPosition = new Vector2(82, -62);
            rRec.sizeDelta = new Vector2(280, 44);

            GameObject recDotObj = new GameObject("RecDot");
            recDotObj.transform.SetParent(recGroup.transform, false);
            var imgRecDot = GetOrAddComponent<Image>(recDotObj);
            imgRecDot.color = new Color(0.95f, 0.16f, 0.16f, 1f);
            imgRecDot.raycastTarget = false;
            var rDot = recDotObj.GetComponent<RectTransform>();
            rDot.anchorMin = new Vector2(0, 0.5f);
            rDot.anchorMax = new Vector2(0, 0.5f);
            rDot.anchoredPosition = new Vector2(10, 0);
            rDot.sizeDelta = new Vector2(14, 14);

            GameObject recLblObj = new GameObject("RecText");
            recLblObj.transform.SetParent(recGroup.transform, false);
            var tmpRecLbl = GetOrAddComponent<TextMeshProUGUI>(recLblObj);
            tmpRecLbl.text = "REC";
            tmpRecLbl.fontSize = 22;
            tmpRecLbl.fontStyle = FontStyles.Bold;
            tmpRecLbl.color = whiteCrisp;
            tmpRecLbl.alignment = TextAlignmentOptions.Left;
            tmpRecLbl.raycastTarget = false;
            var rRecLbl = recLblObj.GetComponent<RectTransform>();
            rRecLbl.anchorMin = new Vector2(0, 0.5f);
            rRecLbl.anchorMax = new Vector2(0, 0.5f);
            rRecLbl.anchoredPosition = new Vector2(54, 0);
            rRecLbl.sizeDelta = new Vector2(68, 30);

            GameObject recTimeObj = new GameObject("RecTimer");
            recTimeObj.transform.SetParent(recGroup.transform, false);
            var tmpRecTime = GetOrAddComponent<TextMeshProUGUI>(recTimeObj);
            tmpRecTime.text = "00:00:00:00";
            tmpRecTime.fontSize = 18;
            tmpRecTime.color = whiteSoft;
            tmpRecTime.alignment = TextAlignmentOptions.Left;
            tmpRecTime.raycastTarget = false;
            var rRecTime = recTimeObj.GetComponent<RectTransform>();
            rRecTime.anchorMin = new Vector2(0, 0.5f);
            rRecTime.anchorMax = new Vector2(0, 0.5f);
            rRecTime.anchoredPosition = new Vector2(168, 0);
            rRecTime.sizeDelta = new Vector2(150, 28);

            // 4. Indicadores Superiores Direitos: 'HD | 4K | 1.0x' + Ícone de Bateria Branco
            GameObject txtZoom = new GameObject("ZoomText");
            txtZoom.transform.SetParent(container.transform, false);
            var tmpZoom = GetOrAddComponent<TextMeshProUGUI>(txtZoom);
            tmpZoom.fontSize = 20;
            tmpZoom.fontStyle = FontStyles.Bold;
            tmpZoom.color = whiteCrisp;
            tmpZoom.alignment = TextAlignmentOptions.Right;
            tmpZoom.text = "HD  |  4K  |  1.0x";
            tmpZoom.raycastTarget = false;
            var rectTxtZoom = txtZoom.GetComponent<RectTransform>();
            rectTxtZoom.anchorMin = new Vector2(1, 1);
            rectTxtZoom.anchorMax = new Vector2(1, 1);
            rectTxtZoom.pivot = new Vector2(1, 1);
            rectTxtZoom.anchoredPosition = new Vector2(-150, -68);
            rectTxtZoom.sizeDelta = new Vector2(260, 32);

            // Ícone de bateria discreto em branco
            GameObject batObj = new GameObject("Icone_Bateria");
            batObj.transform.SetParent(container.transform, false);
            var rBat = GetOrAddComponent<RectTransform>(batObj);
            rBat.anchorMin = new Vector2(1, 1);
            rBat.anchorMax = new Vector2(1, 1);
            rBat.pivot = new Vector2(1, 1);
            rBat.anchoredPosition = new Vector2(-86, -71);
            rBat.sizeDelta = new Vector2(48, 22);
            CreateWhiteLine(batObj.transform, "Bat_Top", new Vector2(0, 10), new Vector2(44, 2f), whiteCrisp);
            CreateWhiteLine(batObj.transform, "Bat_Bot", new Vector2(0, -10), new Vector2(44, 2f), whiteCrisp);
            CreateWhiteLine(batObj.transform, "Bat_L", new Vector2(-21, 0), new Vector2(2f, 20f), whiteCrisp);
            CreateWhiteLine(batObj.transform, "Bat_R", new Vector2(21, 0), new Vector2(2f, 20f), whiteCrisp);
            CreateWhiteLine(batObj.transform, "Bat_Tip", new Vector2(24, 0), new Vector2(4f, 10f), whiteCrisp);
            CreateWhiteLine(batObj.transform, "Bat_Fill1", new Vector2(-12, 0), new Vector2(8f, 12f), whiteCrisp);
            CreateWhiteLine(batObj.transform, "Bat_Fill2", new Vector2(-1, 0), new Vector2(8f, 12f), whiteCrisp);
            CreateWhiteLine(batObj.transform, "Bat_Fill3", new Vector2(10, 0), new Vector2(8f, 12f), whiteCrisp);

            // 5. Ícone Discreto ACIMA do Alvo Catalogável (SEM quadrado verde em volta!)
            GameObject statusBadgeObj = new GameObject("StatusAlvo_Discreto");
            statusBadgeObj.transform.SetParent(container.transform, false);
            var rStatusBadge = GetOrAddComponent<RectTransform>(statusBadgeObj);
            rStatusBadge.sizeDelta = new Vector2(138f, 30f);
            var imgStatusBg = GetOrAddComponent<Image>(statusBadgeObj);
            imgStatusBg.color = new Color(0.92f, 0.62f, 0.12f, 0.92f);
            imgStatusBg.raycastTarget = false;

            GameObject statusIconObj = new GameObject("IconeStatus");
            statusIconObj.transform.SetParent(statusBadgeObj.transform, false);
            var tmpStatusIcon = GetOrAddComponent<TextMeshProUGUI>(statusIconObj);
            tmpStatusIcon.fontSize = 15;
            tmpStatusIcon.fontStyle = FontStyles.Bold;
            tmpStatusIcon.color = Color.white;
            tmpStatusIcon.alignment = TextAlignmentOptions.Center;
            tmpStatusIcon.text = "!";
            tmpStatusIcon.raycastTarget = false;
            var rSIcon = statusIconObj.GetComponent<RectTransform>();
            rSIcon.anchoredPosition = new Vector2(-46f, 0f);
            rSIcon.sizeDelta = new Vector2(32f, 26f);

            GameObject statusLblObj = new GameObject("TextoStatus");
            statusLblObj.transform.SetParent(statusBadgeObj.transform, false);
            var tmpStatusLbl = GetOrAddComponent<TextMeshProUGUI>(statusLblObj);
            tmpStatusLbl.fontSize = 13;
            tmpStatusLbl.fontStyle = FontStyles.Bold;
            tmpStatusLbl.color = Color.white;
            tmpStatusLbl.alignment = TextAlignmentOptions.Left;
            tmpStatusLbl.text = "NOVA";
            tmpStatusLbl.raycastTarget = false;
            var rSLbl = statusLblObj.GetComponent<RectTransform>();
            rSLbl.anchoredPosition = new Vector2(16f, 0f);
            rSLbl.sizeDelta = new Vector2(96f, 26f);
            statusBadgeObj.SetActive(false);

            // 6. Barra Inferior Discreta em Branco (Info da Câmera + Espécie em Foco)
            GameObject bottomInfo = new GameObject("InfoBase_Camera");
            bottomInfo.transform.SetParent(container.transform, false);
            var tmpBottom = GetOrAddComponent<TextMeshProUGUI>(bottomInfo);
            tmpBottom.text = "ISO 200   ·   f/2.8   ·   1/500   ·   [SCROLL] ZOOM   ·   [CLIQUE ESQ] FOTOGRAFAR";
            tmpBottom.fontSize = 15;
            tmpBottom.color = whiteSoft;
            tmpBottom.alignment = TextAlignmentOptions.Center;
            tmpBottom.raycastTarget = false;
            var rBottom = bottomInfo.GetComponent<RectTransform>();
            rBottom.anchorMin = new Vector2(0.5f, 0);
            rBottom.anchorMax = new Vector2(0.5f, 0);
            rBottom.anchoredPosition = new Vector2(0, 62);
            rBottom.sizeDelta = new Vector2(920, 28);

            GameObject txtTarget = new GameObject("TargetNameText");
            txtTarget.transform.SetParent(container.transform, false);
            var tmpTarget = GetOrAddComponent<TextMeshProUGUI>(txtTarget);
            tmpTarget.fontSize = 21;
            tmpTarget.fontStyle = FontStyles.Bold;
            tmpTarget.color = whiteCrisp;
            tmpTarget.alignment = TextAlignmentOptions.Center;
            tmpTarget.text = "";
            tmpTarget.raycastTarget = false;
            var rectTxtTarget = txtTarget.GetComponent<RectTransform>();
            rectTxtTarget.anchorMin = new Vector2(0.5f, 0);
            rectTxtTarget.anchorMax = new Vector2(0.5f, 0);
            rectTxtTarget.anchoredPosition = new Vector2(0, 102);
            rectTxtTarget.sizeDelta = new Vector2(680, 34);
            txtTarget.SetActive(false);

            GameObject targetIconObj = new GameObject("TargetIcon");
            targetIconObj.transform.SetParent(container.transform, false);
            var imgTargetIcon = GetOrAddComponent<Image>(targetIconObj);
            imgTargetIcon.preserveAspect = true;
            imgTargetIcon.raycastTarget = false;
            var rectTargetIcon = targetIconObj.GetComponent<RectTransform>();
            rectTargetIcon.anchorMin = new Vector2(0.5f, 0);
            rectTargetIcon.anchorMax = new Vector2(0.5f, 0);
            rectTargetIcon.anchoredPosition = new Vector2(0, 148);
            rectTargetIcon.sizeDelta = new Vector2(46, 46);
            targetIconObj.SetActive(false);

            GameObject flashObj = GetOrCreateChild(viewfinderRoot.transform, "FlashOverlay");
            var flashImg = GetOrAddComponent<Image>(flashObj);
            flashImg.color = Color.white;
            flashImg.raycastTarget = false;
            var flashGroup = GetOrAddComponent<CanvasGroup>(flashObj);
            flashGroup.alpha = 0f;
            flashGroup.blocksRaycasts = false;
            SetFullStretch(flashObj.GetComponent<RectTransform>());

            GameObject resultObj = GetOrCreateChild(viewfinderRoot.transform, "Resultado_Foto");
            var resultImg = GetOrAddComponent<Image>(resultObj);
            resultImg.color = new Color(0.08f, 0.12f, 0.08f, 0.95f);
            var rectResult = resultObj.GetComponent<RectTransform>();
            rectResult.anchorMin = new Vector2(0.5f, 0.5f);
            rectResult.anchorMax = new Vector2(0.5f, 0.5f);
            rectResult.sizeDelta = new Vector2(560, 410);

            GameObject thumbObj = GetOrCreateChild(resultObj.transform, "Miniatura");
            var rawThumb = GetOrAddComponent<RawImage>(thumbObj);
            var rectThumb = thumbObj.GetComponent<RectTransform>();
            rectThumb.anchoredPosition = new Vector2(0, 75);
            rectThumb.sizeDelta = new Vector2(330, 185);

            GameObject txtResultTitle = GetOrCreateChild(resultObj.transform, "TituloResultado");
            var tmpResultTitle = GetOrAddComponent<TextMeshProUGUI>(txtResultTitle);
            tmpResultTitle.fontSize = 26;
            tmpResultTitle.fontStyle = FontStyles.Bold;
            tmpResultTitle.alignment = TextAlignmentOptions.Center;
            var rTitle = txtResultTitle.GetComponent<RectTransform>();
            rTitle.anchoredPosition = new Vector2(0, -45);
            rTitle.sizeDelta = new Vector2(500, 36);

            GameObject txtResultScore = GetOrCreateChild(resultObj.transform, "Pontuacao");
            var tmpResultScore = GetOrAddComponent<TextMeshProUGUI>(txtResultScore);
            tmpResultScore.fontSize = 22;
            tmpResultScore.alignment = TextAlignmentOptions.Center;
            var rScore = txtResultScore.GetComponent<RectTransform>();
            rScore.anchoredPosition = new Vector2(-95, -82);
            rScore.sizeDelta = new Vector2(220, 32);

            GameObject txtResultCoins = GetOrCreateChild(resultObj.transform, "MoedasGanhas");
            var tmpResultCoins = GetOrAddComponent<TextMeshProUGUI>(txtResultCoins);
            tmpResultCoins.fontSize = 22;
            tmpResultCoins.color = new Color(1f, 0.88f, 0.3f);
            tmpResultCoins.alignment = TextAlignmentOptions.Center;
            var rCoins = txtResultCoins.GetComponent<RectTransform>();
            rCoins.anchoredPosition = new Vector2(105, -82);
            rCoins.sizeDelta = new Vector2(220, 32);

            GameObject txtResultStars = GetOrCreateChild(resultObj.transform, "Estrelas");
            var tmpResultStars = GetOrAddComponent<TextMeshProUGUI>(txtResultStars);
            tmpResultStars.fontSize = 28;
            tmpResultStars.color = Color.yellow;
            tmpResultStars.alignment = TextAlignmentOptions.Center;
            var rStars = txtResultStars.GetComponent<RectTransform>();
            rStars.anchoredPosition = new Vector2(0, -118);
            rStars.sizeDelta = new Vector2(400, 36);

            GameObject txtResultFeed = GetOrCreateChild(resultObj.transform, "Feedback");
            var tmpResultFeed = GetOrAddComponent<TextMeshProUGUI>(txtResultFeed);
            tmpResultFeed.fontSize = 18;
            tmpResultFeed.alignment = TextAlignmentOptions.Center;
            var rFeed = txtResultFeed.GetComponent<RectTransform>();
            rFeed.anchoredPosition = new Vector2(0, -158);
            rFeed.sizeDelta = new Vector2(500, 45);

            resultObj.SetActive(false);

            var soVF = new SerializedObject(viewfinderUI);
            soVF.FindProperty("viewfinderContainer").objectReferenceValue = container;
            soVF.FindProperty("targetStatusBadge").objectReferenceValue = rStatusBadge;
            soVF.FindProperty("targetStatusBg").objectReferenceValue = imgStatusBg;
            soVF.FindProperty("targetStatusIconText").objectReferenceValue = tmpStatusIcon;
            soVF.FindProperty("targetStatusLabelText").objectReferenceValue = tmpStatusLbl;
            soVF.FindProperty("targetNameText").objectReferenceValue = tmpTarget;
            soVF.FindProperty("targetIconImage").objectReferenceValue = imgTargetIcon;
            soVF.FindProperty("zoomLevelText").objectReferenceValue = tmpZoom;
            soVF.FindProperty("recTimerText").objectReferenceValue = tmpRecTime;
            soVF.FindProperty("recDotImage").objectReferenceValue = imgRecDot;
            soVF.FindProperty("crosshairImage").objectReferenceValue = imgCrosshair;
            soVF.FindProperty("flashCanvasGroup").objectReferenceValue = flashGroup;
            soVF.FindProperty("resultPanel").objectReferenceValue = resultObj;
            soVF.FindProperty("photoThumbnail").objectReferenceValue = rawThumb;
            soVF.FindProperty("speciesNameResultText").objectReferenceValue = tmpResultTitle;
            soVF.FindProperty("scoreText").objectReferenceValue = tmpResultScore;
            soVF.FindProperty("coinsText").objectReferenceValue = tmpResultCoins;
            soVF.FindProperty("starsText").objectReferenceValue = tmpResultStars;
            soVF.FindProperty("feedbackText").objectReferenceValue = tmpResultFeed;
            soVF.ApplyModifiedProperties();
        }

        private static void CreateWhiteLine(Transform parent, string name, Vector2 pos, Vector2 size, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        private static void CreateViewfinderCorner(
            Transform parent, string name, Vector2 anchor, Vector2 offset, float dirX, float dirY,
            Color color, float length = 54f, float thickness = 3.5f)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var rt = GetOrAddComponent<RectTransform>(root);
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.anchoredPosition = offset;
            rt.sizeDelta = Vector2.zero;

            CreateWhiteLine(root.transform, "H", new Vector2(dirX * (length * 0.5f), 0f), new Vector2(length, thickness), color);
            CreateWhiteLine(root.transform, "V", new Vector2(0f, dirY * (length * 0.5f)), new Vector2(thickness, length), color);
        }

        private static void SetupQuestHUDUI(Transform canvas)
        {
            GameObject hudRoot = GetOrCreateChild(canvas, "HUD_Missoes");
            SetFullStretch(GetOrAddComponent<RectTransform>(hudRoot));
            var questHUD = GetOrAddComponent<QuestHUDUI>(hudRoot);

            GameObject questBox = GetOrCreateChild(hudRoot.transform, "PainelMissoes");
            var rectBox = GetOrAddComponent<RectTransform>(questBox);
            rectBox.anchorMin = new Vector2(0, 1);
            rectBox.anchorMax = new Vector2(0, 1);
            rectBox.pivot = new Vector2(0, 1);
            rectBox.anchoredPosition = new Vector2(20, -20);
            rectBox.sizeDelta = new Vector2(430, 270);

            // Paleta amadeirada/terrosa semitransparente com borda sutil em tom madeira/bege
            var imgBg = GetOrAddComponent<Image>(questBox);
            imgBg.sprite = null;
            imgBg.color = new Color(0.22f, 0.14f, 0.08f, 0.84f);

            var boxOutline = GetOrAddComponent<Outline>(questBox);
            boxOutline.effectColor = new Color(0.84f, 0.68f, 0.42f, 0.78f);
            boxOutline.effectDistance = new Vector2(2f, -2f);

            // Friso superior amadeirado/dourado de acabamento
            GameObject topTrim = GetOrCreateChild(questBox.transform, "Friso_Superior_Madeira");
            var rectTrim = GetOrAddComponent<RectTransform>(topTrim);
            rectTrim.anchorMin = new Vector2(0f, 1f);
            rectTrim.anchorMax = new Vector2(1f, 1f);
            rectTrim.pivot = new Vector2(0.5f, 1f);
            rectTrim.anchoredPosition = Vector2.zero;
            rectTrim.sizeDelta = new Vector2(0f, 6f);
            var imgTrim = GetOrAddComponent<Image>(topTrim);
            imgTrim.color = new Color(0.84f, 0.68f, 0.42f, 0.90f);
            imgTrim.raycastTarget = false;

            GameObject txtList = GetOrCreateChild(questBox.transform, "ListaTexto");
            var tmpList = GetOrAddComponent<TextMeshProUGUI>(txtList);
            tmpList.fontSize = 16;
            tmpList.color = new Color(0.98f, 0.94f, 0.84f, 1f);
            var rectList = txtList.GetComponent<RectTransform>();
            rectList.anchorMin = Vector2.zero;
            rectList.anchorMax = Vector2.one;
            rectList.offsetMin = new Vector2(16, 12);
            rectList.offsetMax = new Vector2(-16, -14);

            GameObject quickBar = GetOrCreateChild(hudRoot.transform, "Atalhos_HUD");
            var rectQuick = GetOrAddComponent<RectTransform>(quickBar);
            rectQuick.anchorMin = new Vector2(1, 1);
            rectQuick.anchorMax = new Vector2(1, 1);
            rectQuick.pivot = new Vector2(1, 1);
            rectQuick.anchoredPosition = new Vector2(-20, -20);
            rectQuick.sizeDelta = new Vector2(360, 72);
            var imgQuickBg = GetOrAddComponent<Image>(quickBar);
            imgQuickBg.color = new Color(0.22f, 0.14f, 0.08f, 0.84f);
            var quickOutline = GetOrAddComponent<Outline>(quickBar);
            quickOutline.effectColor = new Color(0.84f, 0.68f, 0.42f, 0.78f);
            quickOutline.effectDistance = new Vector2(2f, -2f);

            GameObject iconAlbumObj = GetOrCreateChild(quickBar.transform, "IconeAlbum");
            var rectIconAlbum = GetOrAddComponent<RectTransform>(iconAlbumObj);
            rectIconAlbum.anchoredPosition = new Vector2(-135, 0);
            rectIconAlbum.sizeDelta = new Vector2(54, 48);
            var imgIconAlbum = GetOrAddComponent<Image>(iconAlbumObj);
            ApplyWoodSpriteIfAvailable(imgIconAlbum, "icon_album", Color.white);

            GameObject iconCamObj = GetOrCreateChild(quickBar.transform, "IconeCamera");
            var rectIconCam = GetOrAddComponent<RectTransform>(iconCamObj);
            rectIconCam.anchoredPosition = new Vector2(-72, 0);
            rectIconCam.sizeDelta = new Vector2(54, 34);
            var imgIconCam = GetOrAddComponent<Image>(iconCamObj);
            ApplyWoodSpriteIfAvailable(imgIconCam, "icon_camera", Color.white);

            GameObject txtControls = GetOrCreateChild(quickBar.transform, "TextoAtalhos");
            var tmpControls = GetOrAddComponent<TextMeshProUGUI>(txtControls);
            tmpControls.fontSize = 14;
            tmpControls.text = "<b>[TAB]</b> Álbum\n<b>[RMB]</b> Câmera | <b>[Ctrl]</b> Furtivo\n<b>[ESC]</b> Pausa / Menu";
            var rectTxtCtrl = txtControls.GetComponent<RectTransform>();
            rectTxtCtrl.anchoredPosition = new Vector2(55, 0);
            rectTxtCtrl.sizeDelta = new Vector2(190, 62);

            GameObject banner = GetOrCreateChild(hudRoot.transform, "BannerConclusao");
            var rectBanner = GetOrAddComponent<RectTransform>(banner);
            rectBanner.anchorMin = new Vector2(0.5f, 1);
            rectBanner.anchorMax = new Vector2(0.5f, 1);
            rectBanner.pivot = new Vector2(0.5f, 1);
            rectBanner.anchoredPosition = new Vector2(0, -30);
            rectBanner.sizeDelta = new Vector2(620, 95);

            var bannerImg = GetOrAddComponent<Image>(banner);
            bannerImg.color = new Color(0.12f, 0.45f, 0.20f, 0.95f);

            GameObject txtBannerTitle = GetOrCreateChild(banner.transform, "Titulo");
            var tmpBannerTitle = GetOrAddComponent<TextMeshProUGUI>(txtBannerTitle);
            tmpBannerTitle.fontSize = 24;
            tmpBannerTitle.fontStyle = FontStyles.Bold;
            tmpBannerTitle.alignment = TextAlignmentOptions.Center;
            var rBTitle = txtBannerTitle.GetComponent<RectTransform>();
            rBTitle.anchoredPosition = new Vector2(0, 14);
            rBTitle.sizeDelta = new Vector2(580, 34);

            GameObject txtBannerReward = GetOrCreateChild(banner.transform, "Recompensa");
            var tmpBannerReward = GetOrAddComponent<TextMeshProUGUI>(txtBannerReward);
            tmpBannerReward.fontSize = 18;
            tmpBannerReward.alignment = TextAlignmentOptions.Center;
            var rBRew = txtBannerReward.GetComponent<RectTransform>();
            rBRew.anchoredPosition = new Vector2(0, -18);
            rBRew.sizeDelta = new Vector2(580, 28);

            banner.SetActive(false);

            var soHUD = new SerializedObject(questHUD);
            soHUD.FindProperty("questsContainer").objectReferenceValue = questBox;
            soHUD.FindProperty("questListText").objectReferenceValue = tmpList;
            soHUD.FindProperty("completionBanner").objectReferenceValue = banner;
            soHUD.FindProperty("completionTitleText").objectReferenceValue = tmpBannerTitle;
            soHUD.FindProperty("completionRewardText").objectReferenceValue = tmpBannerReward;
            soHUD.ApplyModifiedProperties();
        }

        /// <summary>
        /// Constrói o Álbum Enciclopédico com visual moderno estilo Caderno de Campo Científico
        /// (painel escuro translúcido em tons terrosos/verde-escuro do Cerrado, sem madeira esticada).
        /// </summary>
        private static AlbumUI SetupAlbumUI(Transform canvas, PlayerController playerController)
        {
            GameObject albumControllerObj = GetOrCreateChild(canvas, "Modal_Album");
            SetFullStretch(GetOrAddComponent<RectTransform>(albumControllerObj));
            var albumUI = GetOrAddComponent<AlbumUI>(albumControllerObj);

            // Janela central estilo Caderno de Campo Científico (verde-floresta/terroso escuro translúcido)
            GameObject albumWindow = GetOrCreateChild(albumControllerObj.transform, "Janela_Album");
            var rectWindow = GetOrAddComponent<RectTransform>(albumWindow);
            rectWindow.sizeDelta = new Vector2(1280, 800);
            var imgBg = GetOrAddComponent<Image>(albumWindow);
            imgBg.sprite = null;
            imgBg.color = new Color(0.08f, 0.11f, 0.08f, 0.96f);

            // Barra Superior de Cabeçalho
            GameObject topBar = GetOrCreateChild(albumWindow.transform, "Barra_Topo_Album");
            var rTopBar = GetOrAddComponent<RectTransform>(topBar);
            rTopBar.anchoredPosition = new Vector2(0, 360);
            rTopBar.sizeDelta = new Vector2(1280, 80);
            var imgTopBar = GetOrAddComponent<Image>(topBar);
            imgTopBar.color = new Color(0.13f, 0.18f, 0.12f, 1f);

            // Linha divisória dourada sutil sob o cabeçalho
            GameObject goldLine = GetOrCreateChild(albumWindow.transform, "Linha_Divisoria_Topo");
            var rGold = GetOrAddComponent<RectTransform>(goldLine);
            rGold.anchoredPosition = new Vector2(0, 320);
            rGold.sizeDelta = new Vector2(1280, 3);
            var imgGold = GetOrAddComponent<Image>(goldLine);
            imgGold.color = new Color(0.83f, 0.68f, 0.22f, 0.85f);

            GameObject titleLabelObj = GetOrCreateChild(topBar.transform, "TituloCaderno");
            var tmpTitleLabel = GetOrAddComponent<TextMeshProUGUI>(titleLabelObj);
            tmpTitleLabel.fontSize = 24;
            tmpTitleLabel.fontStyle = FontStyles.Bold;
            tmpTitleLabel.color = new Color(0.96f, 0.93f, 0.82f);
            tmpTitleLabel.alignment = TextAlignmentOptions.Left;
            tmpTitleLabel.text = "CADERNO DE CAMPO — BIOMA CERRADO";
            var rTitleLbl = titleLabelObj.GetComponent<RectTransform>();
            rTitleLbl.anchoredPosition = new Vector2(-360, 0);
            rTitleLbl.sizeDelta = new Vector2(500, 40);

            GameObject headerObj = GetOrCreateChild(topBar.transform, "HeaderProgresso");
            var tmpProgress = GetOrAddComponent<TextMeshProUGUI>(headerObj);
            tmpProgress.fontSize = 20;
            tmpProgress.fontStyle = FontStyles.Bold;
            tmpProgress.color = new Color(0.55f, 0.88f, 0.55f);
            tmpProgress.alignment = TextAlignmentOptions.Center;
            tmpProgress.text = "0.0% Concluído";
            var rHeader = headerObj.GetComponent<RectTransform>();
            rHeader.anchoredPosition = new Vector2(40, 0);
            rHeader.sizeDelta = new Vector2(220, 36);

            GameObject countObj = GetOrCreateChild(topBar.transform, "ContadorEspecies");
            var tmpCount = GetOrAddComponent<TextMeshProUGUI>(countObj);
            tmpCount.fontSize = 18;
            tmpCount.color = new Color(0.82f, 0.86f, 0.80f);
            tmpCount.alignment = TextAlignmentOptions.Center;
            var rCount = countObj.GetComponent<RectTransform>();
            rCount.anchoredPosition = new Vector2(265, 0);
            rCount.sizeDelta = new Vector2(250, 36);

            GameObject coinsObj = GetOrCreateChild(topBar.transform, "MoedasAlbum");
            var tmpCoins = GetOrAddComponent<TextMeshProUGUI>(coinsObj);
            tmpCoins.fontSize = 21;
            tmpCoins.fontStyle = FontStyles.Bold;
            tmpCoins.color = new Color(0.98f, 0.84f, 0.32f);
            tmpCoins.alignment = TextAlignmentOptions.Right;
            var rCoins = coinsObj.GetComponent<RectTransform>();
            rCoins.anchoredPosition = new Vector2(495, 0);
            rCoins.sizeDelta = new Vector2(220, 36);

            // Barra de Abas de Filtro (Todas | Fauna | Flora) + Dica de fechamento
            Button tabAllBtn = CreateAlbumFilterTab(albumWindow.transform, "Tab_Todas", "TODAS (38)", new Vector2(-520, 285));
            Button tabFaunaBtn = CreateAlbumFilterTab(albumWindow.transform, "Tab_Fauna", "FAUNA (18)", new Vector2(-355, 285));
            Button tabFloraBtn = CreateAlbumFilterTab(albumWindow.transform, "Tab_Flora", "FLORA (20)", new Vector2(-190, 285));

            GameObject hintCloseObj = GetOrCreateChild(albumWindow.transform, "DicaFechar");
            var tmpHintClose = GetOrAddComponent<TextMeshProUGUI>(hintCloseObj);
            tmpHintClose.fontSize = 15;
            tmpHintClose.color = new Color(0.70f, 0.76f, 0.70f);
            tmpHintClose.alignment = TextAlignmentOptions.Right;
            tmpHintClose.text = "Pressione <b>[TAB]</b> ou <b>[ESC]</b> para fechar";
            var rHint = hintCloseObj.GetComponent<RectTransform>();
            rHint.anchoredPosition = new Vector2(430, 285);
            rHint.sizeDelta = new Vector2(360, 30);

            // Grade Rolável de Cards das Espécies (Esquerda — suporta todos os 38 cards com ScrollRect!)
            GameObject gridBg = GetOrCreateChild(albumWindow.transform, "Fundo_Grade");
            var rGridBg = GetOrAddComponent<RectTransform>(gridBg);
            rGridBg.anchoredPosition = new Vector2(-230, -55);
            rGridBg.sizeDelta = new Vector2(760, 620);
            var imgGridBg = GetOrAddComponent<Image>(gridBg);
            imgGridBg.color = new Color(0.06f, 0.08f, 0.06f, 0.75f);

            // Remove antigo GridContainer filho direto se existir para recriar dentro do ScrollRect
            Transform oldDirectGrid = albumWindow.transform.Find("GridContainer");
            if (oldDirectGrid != null) Object.DestroyImmediate(oldDirectGrid.gameObject);

            var scrollRect = GetOrAddComponent<ScrollRect>(gridBg);
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 36f;

            GameObject viewport = GetOrCreateChild(gridBg.transform, "Viewport");
            var rVp = GetOrAddComponent<RectTransform>(viewport);
            rVp.anchorMin = Vector2.zero;
            rVp.anchorMax = Vector2.one;
            rVp.offsetMin = new Vector2(12, 12);
            rVp.offsetMax = new Vector2(-12, -12);
            GetOrAddComponent<RectMask2D>(viewport);

            GameObject grid = GetOrCreateChild(viewport.transform, "GridContainer");
            var gridRect = GetOrAddComponent<RectTransform>(grid);
            gridRect.anchorMin = new Vector2(0f, 1f);
            gridRect.anchorMax = new Vector2(1f, 1f);
            gridRect.pivot = new Vector2(0.5f, 1f);
            gridRect.anchoredPosition = Vector2.zero;
            gridRect.sizeDelta = new Vector2(0f, 600f);
            var glg = GetOrAddComponent<GridLayoutGroup>(grid);
            glg.cellSize = new Vector2(172, 186);
            glg.spacing = new Vector2(14, 14);
            glg.childAlignment = TextAnchor.UpperLeft;
            glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount = 4;

            var csf = GetOrAddComponent<ContentSizeFitter>(grid);
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = rVp;
            scrollRect.content = gridRect;

            // Template limpo e organizado do Card de Espécie (AlbumSlotUI)
            GameObject slotTemplate = GetOrCreateChild(albumWindow.transform, "Template_Slot");
            var slotUI = GetOrAddComponent<AlbumSlotUI>(slotTemplate);
            var slotBtn = GetOrAddComponent<Button>(slotTemplate);
            var slotImg = GetOrAddComponent<Image>(slotTemplate);
            slotImg.color = new Color(0.14f, 0.18f, 0.14f, 0.96f);

            GameObject slotThumbObj = GetOrCreateChild(slotTemplate.transform, "Thumb");
            var slotThumb = GetOrAddComponent<RawImage>(slotThumbObj);
            var rSlotThumb = slotThumbObj.GetComponent<RectTransform>();
            rSlotThumb.anchoredPosition = new Vector2(0, 32);
            rSlotThumb.sizeDelta = new Vector2(154, 96);

            GameObject slotUndiscObj = GetOrCreateChild(slotTemplate.transform, "Undiscovered");
            var imgUndisc = GetOrAddComponent<Image>(slotUndiscObj);
            imgUndisc.color = new Color(0.09f, 0.11f, 0.09f, 0.95f);
            var rSlotUndisc = slotUndiscObj.GetComponent<RectTransform>();
            rSlotUndisc.anchoredPosition = new Vector2(0, 32);
            rSlotUndisc.sizeDelta = new Vector2(154, 96);

            GameObject qMarkObj = GetOrCreateChild(slotUndiscObj.transform, "Interrogacao");
            var tmpQMark = GetOrAddComponent<TextMeshProUGUI>(qMarkObj);
            tmpQMark.text = "?";
            tmpQMark.fontSize = 42;
            tmpQMark.fontStyle = FontStyles.Bold;
            tmpQMark.color = new Color(0.35f, 0.44f, 0.35f, 0.8f);
            tmpQMark.alignment = TextAlignmentOptions.Center;
            SetFullStretch(qMarkObj.GetComponent<RectTransform>());

            GameObject slotIconObj = GetOrCreateChild(slotTemplate.transform, "IconeEspecieSlot");
            var imgSlotIcon = GetOrAddComponent<Image>(slotIconObj);
            imgSlotIcon.preserveAspect = true;
            imgSlotIcon.raycastTarget = false;
            imgSlotIcon.color = Color.white;
            var rSlotIcon = slotIconObj.GetComponent<RectTransform>();
            rSlotIcon.anchoredPosition = new Vector2(54, 58);
            rSlotIcon.sizeDelta = new Vector2(36, 36);

            GameObject badgeBgObj = GetOrCreateChild(slotTemplate.transform, "BadgeFundo");
            var imgBadgeBg = GetOrAddComponent<Image>(badgeBgObj);
            imgBadgeBg.color = new Color(0.25f, 0.35f, 0.25f, 0.95f);
            var rBadgeBg = badgeBgObj.GetComponent<RectTransform>();
            rBadgeBg.anchoredPosition = new Vector2(0, -28);
            rBadgeBg.sizeDelta = new Vector2(154, 20);

            GameObject badgeTxtObj = GetOrCreateChild(badgeBgObj.transform, "BadgeTexto");
            var tmpBadge = GetOrAddComponent<TextMeshProUGUI>(badgeTxtObj);
            tmpBadge.fontSize = 11;
            tmpBadge.fontStyle = FontStyles.Bold;
            tmpBadge.color = Color.white;
            tmpBadge.alignment = TextAlignmentOptions.Center;
            SetFullStretch(badgeTxtObj.GetComponent<RectTransform>());

            GameObject slotNameObj = GetOrCreateChild(slotTemplate.transform, "Nome");
            var tmpSlotName = GetOrAddComponent<TextMeshProUGUI>(slotNameObj);
            tmpSlotName.fontSize = 14;
            tmpSlotName.fontStyle = FontStyles.Bold;
            tmpSlotName.color = new Color(0.95f, 0.95f, 0.90f);
            tmpSlotName.alignment = TextAlignmentOptions.Center;
            var rSlotName = slotNameObj.GetComponent<RectTransform>();
            rSlotName.anchoredPosition = new Vector2(0, -52);
            rSlotName.sizeDelta = new Vector2(160, 24);

            GameObject slotStarsObj = GetOrCreateChild(slotTemplate.transform, "Estrelas");
            var tmpSlotStars = GetOrAddComponent<TextMeshProUGUI>(slotStarsObj);
            tmpSlotStars.fontSize = 15;
            tmpSlotStars.color = new Color(1f, 0.84f, 0.15f);
            tmpSlotStars.alignment = TextAlignmentOptions.Center;
            var rSlotStars = slotStarsObj.GetComponent<RectTransform>();
            rSlotStars.anchoredPosition = new Vector2(0, -74);
            rSlotStars.sizeDelta = new Vector2(160, 20);

            var soSlot = new SerializedObject(slotUI);
            soSlot.FindProperty("button").objectReferenceValue = slotBtn;
            soSlot.FindProperty("photoThumbnail").objectReferenceValue = slotThumb;
            soSlot.FindProperty("speciesIconBadge").objectReferenceValue = imgSlotIcon;
            soSlot.FindProperty("undiscoveredPlaceholder").objectReferenceValue = slotUndiscObj;
            soSlot.FindProperty("nameText").objectReferenceValue = tmpSlotName;
            soSlot.FindProperty("starsText").objectReferenceValue = tmpSlotStars;
            soSlot.FindProperty("rarityBadge").objectReferenceValue = tmpBadge;
            soSlot.FindProperty("rarityBadgeBackground").objectReferenceValue = imgBadgeBg;
            soSlot.ApplyModifiedProperties();

            slotTemplate.SetActive(false);

            // Painel Lateral Direito de Ficha Científica (PainelDetalhes)
            GameObject details = GetOrCreateChild(albumWindow.transform, "PainelDetalhes");
            var detRect = GetOrAddComponent<RectTransform>(details);
            detRect.anchoredPosition = new Vector2(390, -55);
            detRect.sizeDelta = new Vector2(440, 620);
            var detBg = GetOrAddComponent<Image>(details);
            detBg.color = new Color(0.12f, 0.16f, 0.12f, 0.98f);

            GameObject detPhotoFrame = GetOrCreateChild(details.transform, "MolduraFoto");
            var rFrame = GetOrAddComponent<RectTransform>(detPhotoFrame);
            rFrame.anchoredPosition = new Vector2(0, 185);
            rFrame.sizeDelta = new Vector2(392, 208);
            var imgFrame = GetOrAddComponent<Image>(detPhotoFrame);
            imgFrame.color = new Color(0.06f, 0.08f, 0.06f, 1f);

            GameObject detPhotoObj = GetOrCreateChild(details.transform, "FotoDetalhe");
            var detPhoto = GetOrAddComponent<RawImage>(detPhotoObj);
            var rDetPhoto = detPhotoObj.GetComponent<RectTransform>();
            rDetPhoto.anchoredPosition = new Vector2(0, 185);
            rDetPhoto.sizeDelta = new Vector2(380, 196);

            GameObject detIconObj = GetOrCreateChild(details.transform, "IconeEspecieDetalhe");
            var imgDetIcon = GetOrAddComponent<Image>(detIconObj);
            imgDetIcon.preserveAspect = true;
            imgDetIcon.raycastTarget = false;
            imgDetIcon.color = Color.white;
            var rDetIcon = detIconObj.GetComponent<RectTransform>();
            rDetIcon.anchoredPosition = new Vector2(166, 260);
            rDetIcon.sizeDelta = new Vector2(46, 46);

            GameObject txtDetName = GetOrCreateChild(details.transform, "NomeEspecie");
            var tmpDetName = GetOrAddComponent<TextMeshProUGUI>(txtDetName);
            tmpDetName.fontSize = 24;
            tmpDetName.fontStyle = FontStyles.Bold;
            tmpDetName.color = new Color(0.98f, 0.94f, 0.82f);
            tmpDetName.alignment = TextAlignmentOptions.Center;
            var rDetName = txtDetName.GetComponent<RectTransform>();
            rDetName.anchoredPosition = new Vector2(0, 58);
            rDetName.sizeDelta = new Vector2(400, 32);

            GameObject txtDetSci = GetOrCreateChild(details.transform, "NomeCientifico");
            var tmpDetSci = GetOrAddComponent<TextMeshProUGUI>(txtDetSci);
            tmpDetSci.fontSize = 17;
            tmpDetSci.color = new Color(0.68f, 0.86f, 0.68f);
            tmpDetSci.alignment = TextAlignmentOptions.Center;
            var rDetSci = txtDetSci.GetComponent<RectTransform>();
            rDetSci.anchoredPosition = new Vector2(0, 30);
            rDetSci.sizeDelta = new Vector2(400, 26);

            GameObject txtDetRarity = GetOrCreateChild(details.transform, "Raridade");
            var tmpDetRarity = GetOrAddComponent<TextMeshProUGUI>(txtDetRarity);
            tmpDetRarity.fontSize = 15;
            tmpDetRarity.fontStyle = FontStyles.Bold;
            tmpDetRarity.color = new Color(0.96f, 0.80f, 0.30f);
            tmpDetRarity.alignment = TextAlignmentOptions.Center;
            var rDetRarity = txtDetRarity.GetComponent<RectTransform>();
            rDetRarity.anchoredPosition = new Vector2(0, 4);
            rDetRarity.sizeDelta = new Vector2(400, 24);

            GameObject txtDetEdu = GetOrCreateChild(details.transform, "Educativo");
            var tmpDetEdu = GetOrAddComponent<TextMeshProUGUI>(txtDetEdu);
            tmpDetEdu.fontSize = 15.5f;
            tmpDetEdu.color = new Color(0.92f, 0.94f, 0.90f);
            tmpDetEdu.alignment = TextAlignmentOptions.TopLeft;
            var rDetEdu = txtDetEdu.GetComponent<RectTransform>();
            rDetEdu.anchoredPosition = new Vector2(0, -88);
            rDetEdu.sizeDelta = new Vector2(390, 140);

            GameObject txtDetHab = GetOrCreateChild(details.transform, "Habitat");
            var tmpDetHab = GetOrAddComponent<TextMeshProUGUI>(txtDetHab);
            tmpDetHab.fontSize = 15;
            tmpDetHab.color = new Color(0.80f, 0.88f, 0.78f);
            tmpDetHab.alignment = TextAlignmentOptions.Left;
            var rDetHab = txtDetHab.GetComponent<RectTransform>();
            rDetHab.anchoredPosition = new Vector2(0, -176);
            rDetHab.sizeDelta = new Vector2(390, 30);

            GameObject txtDetCons = GetOrCreateChild(details.transform, "Conservacao");
            var tmpDetCons = GetOrAddComponent<TextMeshProUGUI>(txtDetCons);
            tmpDetCons.fontSize = 15;
            tmpDetCons.color = new Color(0.95f, 0.78f, 0.48f);
            tmpDetCons.alignment = TextAlignmentOptions.Left;
            var rDetCons = txtDetCons.GetComponent<RectTransform>();
            rDetCons.anchoredPosition = new Vector2(0, -206);
            rDetCons.sizeDelta = new Vector2(390, 28);

            GameObject txtDetScore = GetOrCreateChild(details.transform, "MelhorScore");
            var tmpDetScore = GetOrAddComponent<TextMeshProUGUI>(txtDetScore);
            tmpDetScore.fontSize = 16;
            tmpDetScore.color = new Color(0.85f, 0.90f, 0.85f);
            tmpDetScore.alignment = TextAlignmentOptions.Center;
            var rDetScore = txtDetScore.GetComponent<RectTransform>();
            rDetScore.anchoredPosition = new Vector2(0, -245);
            rDetScore.sizeDelta = new Vector2(390, 26);

            GameObject txtDetStars = GetOrCreateChild(details.transform, "EstrelasDetalhe");
            var tmpDetStars = GetOrAddComponent<TextMeshProUGUI>(txtDetStars);
            tmpDetStars.fontSize = 24;
            tmpDetStars.color = new Color(1f, 0.84f, 0.15f);
            tmpDetStars.alignment = TextAlignmentOptions.Center;
            var rDetStars = txtDetStars.GetComponent<RectTransform>();
            rDetStars.anchoredPosition = new Vector2(0, -278);
            rDetStars.sizeDelta = new Vector2(390, 30);

            albumWindow.SetActive(false);

            var soAlbumUI = new SerializedObject(albumUI);
            soAlbumUI.FindProperty("albumPanel").objectReferenceValue = albumWindow;
            soAlbumUI.FindProperty("playerController").objectReferenceValue = playerController;
            soAlbumUI.FindProperty("progressPercentageText").objectReferenceValue = tmpProgress;
            soAlbumUI.FindProperty("progressCountText").objectReferenceValue = tmpCount;
            soAlbumUI.FindProperty("coinsDisplayText").objectReferenceValue = tmpCoins;
            soAlbumUI.FindProperty("tabAllButton").objectReferenceValue = tabAllBtn;
            soAlbumUI.FindProperty("tabFaunaButton").objectReferenceValue = tabFaunaBtn;
            soAlbumUI.FindProperty("tabFloraButton").objectReferenceValue = tabFloraBtn;
            soAlbumUI.FindProperty("gridContainer").objectReferenceValue = grid.transform;
            soAlbumUI.FindProperty("slotPrefab").objectReferenceValue = slotTemplate;
            soAlbumUI.FindProperty("detailsPanel").objectReferenceValue = details;
            soAlbumUI.FindProperty("detailPhoto").objectReferenceValue = detPhoto;
            soAlbumUI.FindProperty("detailSpeciesIcon").objectReferenceValue = imgDetIcon;
            soAlbumUI.FindProperty("detailCommonName").objectReferenceValue = tmpDetName;
            soAlbumUI.FindProperty("detailScientificName").objectReferenceValue = tmpDetSci;
            soAlbumUI.FindProperty("detailRarity").objectReferenceValue = tmpDetRarity;
            soAlbumUI.FindProperty("detailEducational").objectReferenceValue = tmpDetEdu;
            soAlbumUI.FindProperty("detailHabitat").objectReferenceValue = tmpDetHab;
            soAlbumUI.FindProperty("detailConservation").objectReferenceValue = tmpDetCons;
            soAlbumUI.FindProperty("detailBestScore").objectReferenceValue = tmpDetScore;
            soAlbumUI.FindProperty("detailStars").objectReferenceValue = tmpDetStars;
            soAlbumUI.ApplyModifiedProperties();

            return albumUI;
        }

        private static Button CreateAlbumFilterTab(Transform parent, string name, string label, Vector2 anchoredPos)
        {
            GameObject tabObj = GetOrCreateChild(parent, name);
            var rTab = GetOrAddComponent<RectTransform>(tabObj);
            rTab.anchoredPosition = anchoredPos;
            rTab.sizeDelta = new Vector2(150, 38);

            var imgTab = GetOrAddComponent<Image>(tabObj);
            imgTab.color = new Color(0.18f, 0.26f, 0.18f, 1f);

            var btnTab = GetOrAddComponent<Button>(tabObj);

            GameObject txtObj = GetOrCreateChild(tabObj.transform, "Texto");
            var tmp = GetOrAddComponent<TextMeshProUGUI>(txtObj);
            tmp.text = label;
            tmp.fontSize = 15;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = new Color(0.95f, 0.95f, 0.88f);
            tmp.alignment = TextAlignmentOptions.Center;
            SetFullStretch(txtObj.GetComponent<RectTransform>());

            return btnTab;
        }

        private static ShopUI SetupShopUI(Transform canvas, PlayerController playerController)
        {
            GameObject shopControllerObj = GetOrCreateChild(canvas, "Modal_Loja");
            SetFullStretch(GetOrAddComponent<RectTransform>(shopControllerObj));
            var shopUI = GetOrAddComponent<ShopUI>(shopControllerObj);

            GameObject shopWindow = GetOrCreateChild(shopControllerObj.transform, "Janela_Loja");
            var rectWindow = GetOrAddComponent<RectTransform>(shopWindow);
            rectWindow.sizeDelta = new Vector2(960, 680);
            var imgBg = GetOrAddComponent<Image>(shopWindow);
            imgBg.color = new Color(0.09f, 0.12f, 0.09f, 0.96f);

            GameObject txtCoins = GetOrCreateChild(shopWindow.transform, "MoedasTexto");
            var tmpCoins = GetOrAddComponent<TextMeshProUGUI>(txtCoins);
            tmpCoins.fontSize = 26;
            tmpCoins.fontStyle = FontStyles.Bold;
            tmpCoins.color = new Color(1f, 0.88f, 0.35f);
            tmpCoins.alignment = TextAlignmentOptions.Center;
            tmpCoins.text = "Suas Moedas: 0";
            var rCoins = txtCoins.GetComponent<RectTransform>();
            rCoins.anchoredPosition = new Vector2(-120, 285);
            rCoins.sizeDelta = new Vector2(520, 40);

            GameObject closeBtnObj = GetOrCreateChild(shopWindow.transform, "Btn_FecharLoja");
            var rClose = GetOrAddComponent<RectTransform>(closeBtnObj);
            rClose.anchoredPosition = new Vector2(340, 285);
            rClose.sizeDelta = new Vector2(180, 56);
            var closeBtn = GetOrAddComponent<Button>(closeBtnObj);
            var closeImg = GetOrAddComponent<Image>(closeBtnObj);
            ApplyWoodSpriteIfAvailable(closeImg, "btn_voltar", new Color(0.6f, 0.25f, 0.15f));

            GameObject container = GetOrCreateChild(shopWindow.transform, "ItensContainer");
            var rectCont = GetOrAddComponent<RectTransform>(container);
            rectCont.anchoredPosition = new Vector2(0, -30);
            rectCont.sizeDelta = new Vector2(860, 520);
            var vlg = GetOrAddComponent<VerticalLayoutGroup>(container);
            vlg.spacing = 12;
            vlg.childControlHeight = false;
            vlg.childControlWidth = false;

            GameObject slotItem = GetOrCreateChild(shopWindow.transform, "Template_ItemLoja");
            var slotUI = GetOrAddComponent<ShopItemSlotUI>(slotItem);
            var imgItem = GetOrAddComponent<Image>(slotItem);
            imgItem.color = new Color(0.14f, 0.18f, 0.14f, 0.94f);
            slotItem.GetComponent<RectTransform>().sizeDelta = new Vector2(840, 88);

            GameObject itemTitleObj = GetOrCreateChild(slotItem.transform, "NomeItem");
            var tmpItemTitle = GetOrAddComponent<TextMeshProUGUI>(itemTitleObj);
            tmpItemTitle.fontSize = 20;
            tmpItemTitle.fontStyle = FontStyles.Bold;
            var rITitle = itemTitleObj.GetComponent<RectTransform>();
            rITitle.anchoredPosition = new Vector2(-150, 18);
            rITitle.sizeDelta = new Vector2(480, 28);

            GameObject itemDescObj = GetOrCreateChild(slotItem.transform, "DescricaoItem");
            var tmpItemDesc = GetOrAddComponent<TextMeshProUGUI>(itemDescObj);
            tmpItemDesc.fontSize = 15;
            tmpItemDesc.color = new Color(0.85f, 0.88f, 0.82f);
            var rIDesc = itemDescObj.GetComponent<RectTransform>();
            rIDesc.anchoredPosition = new Vector2(-150, -16);
            rIDesc.sizeDelta = new Vector2(480, 36);

            GameObject itemPriceObj = GetOrCreateChild(slotItem.transform, "PrecoItem");
            var tmpItemPrice = GetOrAddComponent<TextMeshProUGUI>(itemPriceObj);
            tmpItemPrice.fontSize = 20;
            tmpItemPrice.fontStyle = FontStyles.Bold;
            tmpItemPrice.color = new Color(1f, 0.88f, 0.35f);
            tmpItemPrice.alignment = TextAlignmentOptions.Center;
            var rIPrice = itemPriceObj.GetComponent<RectTransform>();
            rIPrice.anchoredPosition = new Vector2(175, 0);
            rIPrice.sizeDelta = new Vector2(140, 34);

            GameObject buyBtnObj = GetOrCreateChild(slotItem.transform, "BtnComprar");
            var rBuy = GetOrAddComponent<RectTransform>(buyBtnObj);
            rBuy.anchoredPosition = new Vector2(325, 0);
            rBuy.sizeDelta = new Vector2(145, 46);
            var buyBtn = GetOrAddComponent<Button>(buyBtnObj);
            var buyImg = GetOrAddComponent<Image>(buyBtnObj);
            buyImg.color = new Color(0.18f, 0.55f, 0.24f);

            GameObject buyTxtObj = GetOrCreateChild(buyBtnObj.transform, "TextoBotao");
            var tmpBuyTxt = GetOrAddComponent<TextMeshProUGUI>(buyTxtObj);
            tmpBuyTxt.fontSize = 16;
            tmpBuyTxt.fontStyle = FontStyles.Bold;
            tmpBuyTxt.alignment = TextAlignmentOptions.Center;
            tmpBuyTxt.text = "COMPRAR";
            SetFullStretch(buyTxtObj.GetComponent<RectTransform>());

            var soItemSlot = new SerializedObject(slotUI);
            soItemSlot.FindProperty("itemNameText").objectReferenceValue = tmpItemTitle;
            soItemSlot.FindProperty("descriptionText").objectReferenceValue = tmpItemDesc;
            soItemSlot.FindProperty("priceText").objectReferenceValue = tmpItemPrice;
            soItemSlot.FindProperty("buyButton").objectReferenceValue = buyBtn;
            soItemSlot.FindProperty("buyButtonText").objectReferenceValue = tmpBuyTxt;
            soItemSlot.ApplyModifiedProperties();

            slotItem.SetActive(false);
            shopWindow.SetActive(false);

            var soShop = new SerializedObject(shopUI);
            soShop.FindProperty("shopPanel").objectReferenceValue = shopWindow;
            soShop.FindProperty("playerController").objectReferenceValue = playerController;
            soShop.FindProperty("playerCoinsText").objectReferenceValue = tmpCoins;
            soShop.FindProperty("closeButton").objectReferenceValue = closeBtn;
            soShop.FindProperty("itemsContainer").objectReferenceValue = container.transform;
            soShop.FindProperty("itemSlotPrefab").objectReferenceValue = slotItem;
            soShop.ApplyModifiedProperties();

            return shopUI;
        }

        private static void SetupPauseMenuUI(Transform canvas, PlayerController playerController, AlbumUI albumUI, ShopUI shopUI, TutorialOverlayUI tutorialUI)
        {
            Transform oldPause = canvas.Find("Modal_Pausa");
            if (oldPause != null) Object.DestroyImmediate(oldPause.gameObject);

            GameObject pauseControllerObj = GetOrCreateChild(canvas, "Modal_Pausa");
            SetFullStretch(GetOrAddComponent<RectTransform>(pauseControllerObj));
            var pauseUI = GetOrAddComponent<PauseMenuUI>(pauseControllerObj);

            // Janela principal de Pausa na identidade amadeirada/bege do jogo
            GameObject pauseWindow = GetOrCreateChild(pauseControllerObj.transform, "Janela_Pausa");
            var rWin = GetOrAddComponent<RectTransform>(pauseWindow);
            rWin.sizeDelta = new Vector2(560, 620);
            var imgWin = GetOrAddComponent<Image>(pauseWindow);
            imgWin.color = new Color(0.22f, 0.14f, 0.08f, 0.96f);
            var winOut = GetOrAddComponent<Outline>(pauseWindow);
            winOut.effectColor = new Color(0.84f, 0.68f, 0.42f, 0.92f);
            winOut.effectDistance = new Vector2(3f, -3f);

            GameObject headerBadge = GetOrCreateChild(pauseWindow.transform, "Placa_Pausado");
            var rHeader = GetOrAddComponent<RectTransform>(headerBadge);
            rHeader.anchoredPosition = new Vector2(0, 222);
            rHeader.sizeDelta = new Vector2(340, 110);
            var imgHeader = GetOrAddComponent<Image>(headerBadge);
            ApplyWoodSpriteIfAvailable(imgHeader, "btn_pausado", new Color(0.35f, 0.22f, 0.12f));

            GameObject btnResumeObj = GetOrCreateChild(pauseWindow.transform, "Btn_Continuar");
            var rResume = GetOrAddComponent<RectTransform>(btnResumeObj);
            rResume.anchoredPosition = new Vector2(0, 96);
            rResume.sizeDelta = new Vector2(290, 90);
            var btnResume = GetOrAddComponent<Button>(btnResumeObj);
            var imgResume = GetOrAddComponent<Image>(btnResumeObj);
            ApplyWoodSpriteIfAvailable(imgResume, "btn_jogar", new Color(0.25f, 0.55f, 0.25f));

            GameObject btnOptObj = GetOrCreateChild(pauseWindow.transform, "Btn_Opcoes");
            var rOpt = GetOrAddComponent<RectTransform>(btnOptObj);
            rOpt.anchoredPosition = new Vector2(0, -8);
            rOpt.sizeDelta = new Vector2(290, 90);
            var btnOpt = GetOrAddComponent<Button>(btnOptObj);
            var imgOpt = GetOrAddComponent<Image>(btnOptObj);
            ApplyWoodSpriteIfAvailable(imgOpt, "btn_opcoes", new Color(0.45f, 0.34f, 0.18f));

            GameObject btnMenuObj = GetOrCreateChild(pauseWindow.transform, "Btn_MenuPrincipal");
            var rMenu = GetOrAddComponent<RectTransform>(btnMenuObj);
            rMenu.anchoredPosition = new Vector2(0, -112);
            rMenu.sizeDelta = new Vector2(290, 90);
            var btnMenu = GetOrAddComponent<Button>(btnMenuObj);
            var imgMenu = GetOrAddComponent<Image>(btnMenuObj);
            ApplyWoodSpriteIfAvailable(imgMenu, "btn_voltar", new Color(0.55f, 0.42f, 0.2f));

            GameObject btnQuitObj = GetOrCreateChild(pauseWindow.transform, "Btn_Sair");
            var rQuit = GetOrAddComponent<RectTransform>(btnQuitObj);
            rQuit.anchoredPosition = new Vector2(0, -216);
            rQuit.sizeDelta = new Vector2(290, 90);
            var btnQuit = GetOrAddComponent<Button>(btnQuitObj);
            var imgQuit = GetOrAddComponent<Image>(btnQuitObj);
            ApplyWoodSpriteIfAvailable(imgQuit, "btn_sair", new Color(0.65f, 0.22f, 0.18f));

            // Subpainel de Opções In-Game (idêntico ao Menu Inicial)
            var (settingsUI, btnOptBack) = CerradoSettingsUIBuilder.BuildWoodSettingsCard(
                pauseControllerObj.transform,
                "Janela_Opcoes_Pausa",
                "Pressione [ESC] ou clique em Voltar para retornar ao menu de pausa"
            );

            pauseWindow.SetActive(false);
            settingsUI.gameObject.SetActive(false);

            var soPause = new SerializedObject(pauseUI);
            soPause.FindProperty("pausePanel").objectReferenceValue = pauseWindow;
            soPause.FindProperty("optionsPanel").objectReferenceValue = settingsUI.gameObject;
            soPause.FindProperty("settingsPanelUI").objectReferenceValue = settingsUI;
            soPause.FindProperty("tutorialOverlayUI").objectReferenceValue = tutorialUI;
            soPause.FindProperty("playerController").objectReferenceValue = playerController;
            soPause.FindProperty("albumUI").objectReferenceValue = albumUI;
            soPause.FindProperty("shopUI").objectReferenceValue = shopUI;
            soPause.FindProperty("resumeButton").objectReferenceValue = btnResume;
            soPause.FindProperty("optionsButton").objectReferenceValue = btnOpt;
            soPause.FindProperty("optionsBackButton").objectReferenceValue = btnOptBack;
            soPause.FindProperty("mainMenuButton").objectReferenceValue = btnMenu;
            soPause.FindProperty("quitButton").objectReferenceValue = btnQuit;
            soPause.ApplyModifiedProperties();
        }

        private static GameObject GetOrCreateChild(Transform parent, string name)
        {
            bool isUI = (parent is RectTransform) || (parent != null && parent.GetComponentInParent<Canvas>() != null);
            Transform child = parent != null ? parent.Find(name) : null;
            if (child != null)
            {
                if (isUI && child.GetComponent<RectTransform>() == null)
                {
                    Object.DestroyImmediate(child.gameObject);
                }
                else
                {
                    return child.gameObject;
                }
            }

            GameObject obj = isUI ? new GameObject(name, typeof(RectTransform)) : new GameObject(name);
            if (parent != null)
            {
                obj.transform.SetParent(parent, false);
            }
            return obj;
        }

        private static void SetFullStretch(RectTransform rect)
        {
            if (rect == null) return;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
