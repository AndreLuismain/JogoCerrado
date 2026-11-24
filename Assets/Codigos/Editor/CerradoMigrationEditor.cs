using System;
using System.Collections.Generic;
using System.IO;
using Cerrado.AI;
using Cerrado.Audio;
using Cerrado.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Cerrado.EditorTools
{
    public static class CerradoMigrationEditor
    {
        private const string ImportTempFolder = "Assets/Modelos/_ImportTemp";

        [MenuItem("Cerrado/0. Preparar Assets Migrados (URP, Prefabs, Skybox, Grama e Áudio)")]
        public static void PrepareMigratedAssetsAndURP()
        {
            EnsurePhysicsLayersAndCollisions();
            ConfigureMigratedTextureImporters();
            EnsureFaunaFbxAreGeneric();
            SetupEveningSkyboxMaterial();
            ConvertAllSourcePrefabsToCleanURP();
            FixBlendShapePrefabBounds();
            CleanupTerrainGrassMaterials();
            UpgradeAllFoliageAndGrassMaterialsToShowcase();
            ConfigureVolumeProfilesAndPipeline();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[CerradoMigration] Assets migrados configurados com sucesso em URP!");
        }

        public static void EnsurePhysicsLayersAndCollisions()
        {
            UnityEngine.Object[] tagManagerAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (tagManagerAssets != null && tagManagerAssets.Length > 0)
            {
                var soTag = new SerializedObject(tagManagerAssets[0]);
                var layersProp = soTag.FindProperty("layers");
                if (layersProp != null && layersProp.isArray)
                {
                    SetLayerIfEmptyOrNamed(layersProp, 8, "Animal");
                    SetLayerIfEmptyOrNamed(layersProp, 9, "FenceBoundary");
                    soTag.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            int animalLayer = LayerMask.NameToLayer("Animal");
            int fenceLayer = LayerMask.NameToLayer("FenceBoundary");
            if (animalLayer >= 0 && fenceLayer >= 0)
            {
                Physics.IgnoreLayerCollision(animalLayer, fenceLayer, true);
            }
        }

        private static void SetLayerIfEmptyOrNamed(SerializedProperty layersProp, int index, string layerName)
        {
            if (index >= layersProp.arraySize) return;
            var elem = layersProp.GetArrayElementAtIndex(index);
            if (elem != null && (string.IsNullOrEmpty(elem.stringValue) || elem.stringValue == layerName))
            {
                elem.stringValue = layerName;
            }
        }

        private static void EnsureFaunaFbxAreGeneric()
        {
            string[] fbxPaths = new[]
            {
                "Assets/Modelos/Fauna/OncaParda.fbx",
                "Assets/Modelos/Fauna/coelho.fbx",
                "Assets/Modelos/Fauna/tamandua_npc.fbx"
            };
            foreach (string path in fbxPaths)
            {
                if (!File.Exists(path)) continue;
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer != null && importer.animationType == ModelImporterAnimationType.Legacy)
                {
                    importer.animationType = ModelImporterAnimationType.Generic;
                    importer.SaveAndReimport();
                }
            }
        }

        private static void ConfigureMigratedTextureImporters()
        {
            // 1. Cubemap HDRI evening_field_8k
            string skyTexPath = "Assets/Dados/Texturas/Ambiente/evening_field_8k.png";
            if (File.Exists(skyTexPath))
            {
                var skyImp = AssetImporter.GetAtPath(skyTexPath) as TextureImporter;
                if (skyImp != null)
                {
                    bool changed = false;
                    if (skyImp.textureShape != TextureImporterShape.TextureCube)
                    {
                        skyImp.textureShape = TextureImporterShape.TextureCube;
                        changed = true;
                    }
                    if (skyImp.generateCubemap != TextureImporterGenerateCubemap.AutoCubemap)
                    {
                        skyImp.generateCubemap = TextureImporterGenerateCubemap.AutoCubemap;
                        changed = true;
                    }
                    var soSky = new SerializedObject(skyImp);
                    var seamlessProp = soSky.FindProperty("m_SeamlessCubemap");
                    if (seamlessProp != null && !seamlessProp.boolValue)
                    {
                        seamlessProp.boolValue = true;
                        soSky.ApplyModifiedPropertiesWithoutUndo();
                        changed = true;
                    }
                    if (skyImp.maxTextureSize != 2048)
                    {
                        skyImp.maxTextureSize = 2048;
                        changed = true;
                    }
                    if (!skyImp.sRGBTexture)
                    {
                        skyImp.sRGBTexture = true;
                        changed = true;
                    }
                    if (changed) skyImp.SaveAndReimport();
                }
            }

            // 2. Texturas de Fauna, Flora e Folhagem (BC7 / Alta Qualidade, MipMaps Kaiser + Preserve Coverage, Aniso 16)
            if (AssetDatabase.IsValidFolder("Assets/Dados/Texturas"))
            {
                string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Dados/Texturas" });
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (path.Contains("Ambiente/evening_field_8k") || path.Contains("IconesEspecies")) continue;

                    string fn = Path.GetFileName(path).ToLowerInvariant();
                    var imp = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (imp == null) continue;

                    bool isNormal = fn.Contains("normal") || fn.EndsWith("_1.png") && (fn.Contains("blend_") || fn.Contains("jacare") || fn.Contains("lobo"))
                                    || fn == "fbx_arvoreseca_1.jpg" || fn == "fbx_chuveirinho_1.jpg"
                                    || fn == "blend_jatoba_remake_3.png" || fn == "blend_angico_remake_3.png" || fn == "blend_aroeira_remake_4.png";
                    bool isAlphaFoliage = !isNormal && (
                        fn.Contains("hair") || fn.Contains("flower") || fn.Contains("folha") || fn.Contains("flor") ||
                        fn.Contains("fern") || fn.Contains("semente") || fn.Contains("leaf") || fn.Contains("branch") ||
                        fn.Contains("frutinha") || fn.Contains("grass") || fn.Contains("bush") ||
                        fn.Contains("heather") || fn.Contains("plant") || fn.Contains("chuveirinho") ||
                        fn == "blend_jatoba_remake_2.png" || fn == "blend_angico_remake_2.png" ||
                        fn == "blend_angico_remake_5.png" || fn == "blend_aroeira_remake_2.png" || fn == "blend_aroeira_remake_3.png");
                    bool isFoliageOrFlora = isAlphaFoliage || path.Contains("/Flora/") || path.Contains("/Grama/");

                    TextureImporterType targetType = isNormal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                    int targetAniso = isFoliageOrFlora ? 16 : 8;
                    TextureImporterCompression targetComp = isFoliageOrFlora ? TextureImporterCompression.CompressedHQ : imp.textureCompression;

                    bool changed = false;
                    if (imp.textureType != targetType) { imp.textureType = targetType; changed = true; }
                    if (imp.wrapMode != TextureWrapMode.Repeat) { imp.wrapMode = TextureWrapMode.Repeat; changed = true; }
                    if (isAlphaFoliage && !imp.alphaIsTransparency) { imp.alphaIsTransparency = true; changed = true; }
                    if (!imp.mipmapEnabled) { imp.mipmapEnabled = true; changed = true; }
                    if (isFoliageOrFlora && imp.mipmapFilter != TextureImporterMipFilter.KaiserFilter)
                    {
                        imp.mipmapFilter = TextureImporterMipFilter.KaiserFilter;
                        changed = true;
                    }
                    if (isAlphaFoliage && !imp.mipMapsPreserveCoverage)
                    {
                        imp.mipMapsPreserveCoverage = true;
                        imp.alphaTestReferenceValue = 0.40f;
                        changed = true;
                    }
                    if (imp.anisoLevel != targetAniso) { imp.anisoLevel = targetAniso; changed = true; }
                    if (isFoliageOrFlora && imp.textureCompression != targetComp)
                    {
                        imp.textureCompression = targetComp;
                        changed = true;
                    }

                    // Override explícito Standalone BC7 para folhas/gramas sem compressão destrutiva DXT5
                    if (isAlphaFoliage)
                    {
                        var standaloneSettings = imp.GetPlatformTextureSettings("Standalone");
                        if (!standaloneSettings.overridden || standaloneSettings.format != TextureImporterFormat.BC7 || standaloneSettings.maxTextureSize < 2048)
                        {
                            standaloneSettings.overridden = true;
                            standaloneSettings.name = "Standalone";
                            standaloneSettings.maxTextureSize = 2048;
                            standaloneSettings.format = TextureImporterFormat.BC7;
                            standaloneSettings.compressionQuality = 100;
                            imp.SetPlatformTextureSettings(standaloneSettings);
                            changed = true;
                        }
                    }

                    if (changed) imp.SaveAndReimport();
                }
            }

            // 3. Ícones individuais das espécies -> Sprites UI
            if (AssetDatabase.IsValidFolder("Assets/Dados/IconesEspecies"))
            {
                string[] iconGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Dados/IconesEspecies" });
                foreach (string guid in iconGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var imp = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (imp == null) continue;

                    if (imp.textureType != TextureImporterType.Sprite ||
                        imp.spriteImportMode != SpriteImportMode.Single ||
                        !imp.alphaIsTransparency ||
                        imp.mipmapEnabled ||
                        imp.maxTextureSize != 512)
                    {
                        imp.textureType = TextureImporterType.Sprite;
                        imp.spriteImportMode = SpriteImportMode.Single;
                        imp.alphaIsTransparency = true;
                        imp.mipmapEnabled = false;
                        imp.maxTextureSize = 512;
                        imp.SaveAndReimport();
                    }
                }
            }
        }

        public static Material SetupEveningSkyboxMaterial()
        {
            EnsureFolder("Assets/Dados/Materiais");
            string matPath = "Assets/Dados/Materiais/Mat_Skybox_Cerrado_Entardecer.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            Shader skyShader = Shader.Find("Skybox/Cubemap");
            if (skyShader == null) return mat;

            if (mat == null)
            {
                mat = new Material(skyShader);
                mat.name = "Mat_Skybox_Cerrado_Entardecer";
                AssetDatabase.CreateAsset(mat, matPath);
            }
            else if (mat.shader != skyShader)
            {
                mat.shader = skyShader;
            }

            Cubemap cube = AssetDatabase.LoadAssetAtPath<Cubemap>("Assets/Dados/Texturas/Ambiente/evening_field_8k.png");
            if (cube != null && mat.HasProperty("_Tex"))
            {
                mat.SetTexture("_Tex", cube);
            }
            if (mat.HasProperty("_Tint")) mat.SetColor("_Tint", new Color(0.55f, 0.52f, 0.48f, 1f));
            if (mat.HasProperty("_Exposure")) mat.SetFloat("_Exposure", 1.05f);
            if (mat.HasProperty("_Rotation")) mat.SetFloat("_Rotation", 35f);

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void ConvertAllSourcePrefabsToCleanURP()
        {
            if (!AssetDatabase.IsValidFolder(ImportTempFolder))
            {
                return;
            }

            EnsureFolder("Assets/Dados/Materiais/Migrados");
            EnsureFolder("Assets/Modelos/Fauna/Prefabs");
            EnsureFolder("Assets/Modelos/Flora/Prefabs");

            // Materiais de Fauna (URP Lit, Smoothness ~0.09)
            Material matLobo = CreateURPMaterial("Migrados/Mat_Migrado_LoboGuara", Color.white,
                "Assets/Dados/Texturas/Migrados/Fauna/Lobo_color.png",
                "Assets/Dados/Texturas/Migrados/Fauna/NormalLobo.png", false, 0.09f);
            Material matLoboPelo = CreateURPMaterial("Migrados/Mat_Migrado_LoboGuara_Pelo", Color.white,
                "Assets/Dados/Texturas/Migrados/Fauna/HairStrip3.png", null, true, 0.08f, 0.30f);
            Material matTamandua = CreateURPMaterial("Migrados/Mat_Migrado_Tamandua", Color.white,
                "Assets/Dados/Texturas/Migrados/Fauna/Textura_base_Tamandua.png", null, false, 0.09f);
            Material matEma = CreateURPMaterial("Migrados/Mat_Migrado_Ema", Color.white,
                "Assets/Dados/Texturas/Migrados/Fauna/ema_color.png", null, false, 0.09f);
            Material matTatu = CreateURPMaterial("Migrados/Mat_Migrado_TatuPeba", Color.white,
                "Assets/Dados/Texturas/Migrados/Fauna/Armadillo_BaseColor_1.png",
                "Assets/Dados/Texturas/Migrados/Fauna/Normal_Armadillo_0.png", false, 0.11f);
            Material matJacare = CreateURPMaterial("Migrados/Mat_Migrado_Jacare", Color.white,
                "Assets/Dados/Texturas/Migrados/Fauna/Aligator_Dif_Tex.png",
                "Assets/Dados/Texturas/Migrados/Fauna/NormalMap_6_1.png", false, 0.12f);
            Material matVeado = CreateURPMaterial("Migrados/Mat_Migrado_Veado", Color.white,
                "Assets/Dados/Texturas/Migrados/Fauna/Veado_basecolor.png", null, false, 0.09f);

            // Materiais de Flora Nativa (URP Lit, Alpha Clipping para folhas/flores/frutos)
            Material matIpeBark = CreateURPMaterial("Migrados/Mat_Migrado_Ipe_Casca", Color.white,
                "Assets/Dados/Texturas/Migrados/Flora/Bark_Malus-domestica_Diffuse_2.png",
                "Assets/Dados/Texturas/Migrados/Flora/Bark_Malus-domestica_Normal.png", false, 0.08f);
            Material matIpeFlor = CreateURPMaterial("Migrados/Mat_Migrado_Ipe_Flor", Color.white,
                "Assets/Dados/Texturas/Migrados/Flora/Flower_Verbascum_densiflorum_Diffuse_0.png",
                "Assets/Dados/Texturas/Migrados/Flora/Flower_Verbascum_densiflorum_Normal_1.png", true, 0.08f, 0.35f);

            Material matPequiBark = CreateURPMaterial("Migrados/Mat_Migrado_Pequi_Casca", Color.white,
                "Assets/Dados/Texturas/Migrados/Flora/Bark_Cedrus-deodara_Diffuse.png",
                "Assets/Dados/Texturas/Migrados/Flora/Bark_Cedrus-deodara_Normal_1.png", false, 0.08f);
            Material matPequiFolha = CreateURPMaterial("Migrados/Mat_Migrado_Pequi_Folha", Color.white,
                "Assets/Dados/Texturas/Migrados/Flora/Folha_pequizeiro_1.png",
                "Assets/Dados/Texturas/Migrados/Flora/Folha_normal_1.png", true, 0.09f, 0.35f);

            Material matBarbaBark = CreateURPMaterial("Migrados/Mat_Migrado_Barbatimao_Casca", Color.white,
                "Assets/Dados/Texturas/Migrados/Flora/Bark_Malus-domestica_Diffuse.png",
                "Assets/Dados/Texturas/Migrados/Flora/Bark_Malus-domestica_Normal_1.png", false, 0.08f);
            Material matBarbaFolha = CreateURPMaterial("Migrados/Mat_Migrado_Barbatimao_Folha", Color.white,
                "Assets/Dados/Texturas/Migrados/Flora/folha_barbatimao.png", null, true, 0.09f, 0.35f);

            Material matAngicoBark = CreateURPMaterial("Migrados/Mat_Migrado_Angico_Casca", Color.white,
                "Assets/Dados/Texturas/Migrados/Flora/Bark_Rough_Diffuse_0.png",
                "Assets/Dados/Texturas/Migrados/Flora/Bark_Rough_Normal.png", false, 0.08f);
            Material matAngicoFolha = CreateURPMaterial("Migrados/Mat_Migrado_Angico_Folha", Color.white,
                "Assets/Dados/Texturas/Migrados/Flora/Fern_Diffuse.png",
                "Assets/Dados/Texturas/Migrados/Flora/Fern_Normal.png", true, 0.09f, 0.35f);
            Material matAngicoSemente = CreateURPMaterial("Migrados/Mat_Migrado_Angico_Semente", Color.white,
                "Assets/Dados/Texturas/Migrados/Flora/semente.png", null, true, 0.09f, 0.35f);

            Material matJatobaBark = CreateURPMaterial("Migrados/Mat_Migrado_Jatoba_Casca", Color.white,
                "Assets/Dados/Texturas/Migrados/Flora/Bark_Malus-domestica_Diffuse_0.png",
                "Assets/Dados/Texturas/Migrados/Flora/Bark_Malus-domestica_Normal_0.png", false, 0.08f);
            Material matJatobaFolha = CreateURPMaterial("Migrados/Mat_Migrado_Jatoba_Folha", Color.white,
                "Assets/Dados/Texturas/Migrados/Flora/Leaf_Quercus-agrifolia_Diffuse_1.png",
                "Assets/Dados/Texturas/Migrados/Flora/Leaf_Quercus-agrifolia_Normal.png", true, 0.09f, 0.35f);
            Material matJatobaFruto = CreateURPMaterial("Migrados/Mat_Migrado_Jatoba_Fruto", new Color(0.36f, 0.23f, 0.12f),
                null, null, false, 0.14f);

            Material matAroeiraBark = CreateURPMaterial("Migrados/Mat_Migrado_Aroeira_Casca", Color.white,
                "Assets/Dados/Texturas/Migrados/Flora/Bark_Podocarpus-macrophyllus_Diffuse.png",
                "Assets/Dados/Texturas/Migrados/Flora/Bark_Podocarpus-macrophyllus_Normal.png", false, 0.08f);
            Material matAroeiraFolha = CreateURPMaterial("Migrados/Mat_Migrado_Aroeira_Folha", Color.white,
                "Assets/Dados/Texturas/Migrados/Flora/Leaf_Quercus-agrifolia_Diffuse.png",
                "Assets/Dados/Texturas/Migrados/Flora/Leaf_Quercus-agrifolia_Normal_1.png", true, 0.09f, 0.35f);
            Material matAroeiraFruto = CreateURPMaterial("Migrados/Mat_Migrado_Aroeira_Fruto", Color.white,
                "Assets/Dados/Texturas/Migrados/Flora/FrutinhaVermelha.png", null, true, 0.12f, 0.35f);

            // Converte Fauna
            ConvertAnimalSourcePrefab("Lobo-guara.prefab", "Assets/Modelos/Fauna/Prefabs/PFB_Lobo_Guara.prefab", "Lobo-guara",
                renderer =>
                {
                    string n = renderer.gameObject.name.ToLower();
                    return n.Contains("pelo") ? new[] { matLoboPelo } : new[] { matLobo };
                });

            ConvertAnimalSourcePrefab("Tamandua-bandeira.prefab", "Assets/Modelos/Fauna/Prefabs/PFB_Tamandua_Bandeira.prefab", "Tamandua-bandeira",
                _ => new[] { matTamandua });

            ConvertAnimalSourcePrefab("Ema.prefab", "Assets/Modelos/Fauna/Prefabs/PFB_Ema.prefab", "Ema",
                _ => new[] { matEma });

            ConvertAnimalSourcePrefab("Tatu-peba.prefab", "Assets/Modelos/Fauna/Prefabs/PFB_Tatu_Peba.prefab", "Tatu-peba",
                _ => new[] { matTatu });

            ConvertAnimalSourcePrefab("Jacare-do-papo-amarelo.prefab", "Assets/Modelos/Fauna/Prefabs/PFB_Jacare_Papo_Amarelo.prefab", "Jacare-do-papo-amarelo",
                _ => new[] { matJacare });

            ConvertAnimalSourcePrefab("Veado-campeiro.prefab", "Assets/Modelos/Fauna/Prefabs/PFB_Veado_Campeiro.prefab", "VeadoCampeiro",
                _ => new[] { matVeado });

            // Converte Flora Nativa
            ConvertFloraSourcePrefab("Ipe-amarelo.prefab", "Assets/Modelos/Flora/Prefabs/PFB_Ipe_Amarelo.prefab",
                renderer => new[] { matIpeBark, matIpeFlor });

            ConvertFloraSourcePrefab("Pequi.prefab", "Assets/Modelos/Flora/Prefabs/PFB_Pequi.prefab",
                renderer =>
                {
                    string n = renderer.gameObject.name.ToLower();
                    return (n.Contains("folha") || n.Contains("pequis")) ? new[] { matPequiFolha } : new[] { matPequiBark };
                });

            ConvertFloraSourcePrefab("Barbatimao.prefab", "Assets/Modelos/Flora/Prefabs/PFB_Barbatimao.prefab",
                renderer =>
                {
                    string n = renderer.gameObject.name.ToLower();
                    return n.Contains("folha") ? new[] { matBarbaFolha } : new[] { matBarbaBark };
                });

            ConvertFloraSourcePrefab("Angico.prefab", "Assets/Modelos/Flora/Prefabs/PFB_Angico.prefab",
                renderer =>
                {
                    string n = renderer.gameObject.name.ToLower();
                    return n.Contains("folha") ? new[] { matAngicoFolha, matAngicoSemente } : new[] { matAngicoBark };
                });

            ConvertFloraSourcePrefab("Jatoba.prefab", "Assets/Modelos/Flora/Prefabs/PFB_Jatoba.prefab",
                renderer =>
                {
                    string n = renderer.gameObject.name.ToLower();
                    return n.Contains("folha") ? new[] { matJatobaFolha, matJatobaFruto } : new[] { matJatobaBark };
                });

            ConvertFloraSourcePrefab("Aroeira.prefab", "Assets/Modelos/Flora/Prefabs/PFB_Aroeira.prefab",
                renderer =>
                {
                    string n = renderer.gameObject.name.ToLower();
                    return n.Contains("folha") ? new[] { matAroeiraFolha, matAroeiraFruto } : new[] { matAroeiraBark };
                });

            // --- NOVOS PREFABS DA EXPANSÃO BETA (Biólogo Guia + Suçuarana 53 BlendShapes + 11 Fauna + 12 Flora) ---
            ConvertExpandedBetaPrefabs();

            // Remove pasta temporária de importação para zero scripts ausentes no projeto
            if (AssetDatabase.IsValidFolder(ImportTempFolder))
            {
                AssetDatabase.DeleteAsset(ImportTempFolder);
            }
        }

        private static void ConvertExpandedBetaPrefabs()
        {
            EnsureFolder("Assets/Modelos/NPC/Prefabs");
            const string fTex = "Assets/Dados/Texturas/Migrados/Fauna";
            const string flTex = "Assets/Dados/Texturas/Migrados/Flora";
            const string npcTex = "Assets/Dados/Texturas/Migrados/NPC";

            // 1. Biólogo Guia (Prof. Silva — Rig Humanoid com CharacterAvatar.asset)
            if (File.Exists($"{ImportTempFolder}/BiologoGuia.prefab"))
            {
                Material matSkinHead = CreateURPMaterial("Migrados/Mat_NPC_SkinHead", Color.white, $"{npcTex}/Std_Skin_Head_Diffuse.png", $"{npcTex}/Std_Skin_Head_Normal.png", false, 0.14f);
                Material matSkinBody = CreateURPMaterial("Migrados/Mat_NPC_SkinBody", Color.white, $"{npcTex}/Std_Skin_Body_Diffuse.png", $"{npcTex}/Std_Skin_Body_Normal.png", false, 0.14f);
                Material matSkinArm  = CreateURPMaterial("Migrados/Mat_NPC_SkinArm",  Color.white, $"{npcTex}/Std_Skin_Arm_Diffuse.png",  $"{npcTex}/Std_Skin_Arm_Normal.png",  false, 0.14f);
                Material matShirt    = CreateURPMaterial("Migrados/Mat_NPC_Shirt",    new Color(0.95f, 0.94f, 0.90f), $"{npcTex}/Blusa.png", $"{npcTex}/Fit_shirts_Normal.png", false, 0.08f);
                Material matSweater  = CreateURPMaterial("Migrados/Mat_NPC_Sweater",  new Color(0.28f, 0.38f, 0.24f), null, $"{npcTex}/Layered_sweater_Normal.png", false, 0.08f);
                Material matPants    = CreateURPMaterial("Migrados/Mat_NPC_Pants",    new Color(0.32f, 0.28f, 0.22f), $"{npcTex}/DefaultMaterial_baseColor.png", $"{npcTex}/DefaultMaterial_normal.png", false, 0.08f);
                Material matBoots    = CreateURPMaterial("Migrados/Mat_NPC_Boots",    Color.white, $"{npcTex}/Boots_Diffuse.png", $"{npcTex}/Boots_Normal.png", false, 0.16f);
                Material matHat      = CreateURPMaterial("Migrados/Mat_NPC_Hat",      Color.white, $"{npcTex}/Hat_initialShadingGroup_BaseColor.png", null, false, 0.08f);
                Material matEye      = CreateURPMaterial("Migrados/Mat_NPC_Eye",      Color.white, $"{npcTex}/Std_Eye_L_Diffuse.png", $"{npcTex}/Std_Eye_L_Normal.png", false, 0.65f);
                Material matTeeth    = CreateURPMaterial("Migrados/Mat_NPC_Teeth",    Color.white, $"{npcTex}/Std_Upper_Teeth_Diffuse.png", $"{npcTex}/Std_Upper_Teeth_Normal.png", false, 0.35f);
                Material matTongue   = CreateURPMaterial("Migrados/Mat_NPC_Tongue",   Color.white, $"{npcTex}/Std_Tongue_Diffuse.png", $"{npcTex}/Std_Tongue_Normal.png", false, 0.25f);
                Material matPhone    = CreateURPMaterial("Migrados/Mat_NPC_Phone",    new Color(0.10f, 0.11f, 0.13f), null, null, false, 0.55f);

                ConvertBiologoGuiaPrefab("BiologoGuia.prefab", "Assets/Modelos/NPC/Prefabs/PFB_Biologo_Guia.prefab", r =>
                {
                    string n = r.gameObject.name.ToLowerInvariant();
                    if (n.Contains("hat")) return new[] { matHat };
                    if (n.Contains("boot")) return new[] { matBoots };
                    if (n.Contains("shirt")) return new[] { matShirt };
                    if (n.Contains("sweater")) return new[] { matSweater };
                    if (n.Contains("cal")) return new[] { matPants };
                    if (n.Contains("eye")) return new[] { matEye };
                    if (n.Contains("teeth")) return new[] { matTeeth };
                    if (n.Contains("tongue")) return new[] { matTongue };
                    if (n.Contains("celular")) return new[] { matPhone };
                    // CC_Base_Body tem múltiplos submeshes (cabeça, corpo, braços, unhas, cílios)
                    return new[] { matSkinHead, matSkinBody, matSkinArm, matSkinBody, matSkinBody, matSkinHead };
                });
            }

            // 2. Suçuarana / Onça-parda (53 BlendShapes da malha OncaParda.asset)
            if (File.Exists($"{ImportTempFolder}/Onca-parda.prefab"))
            {
                Material matOnca = CreateURPMaterial("Migrados/Mat_Migrado_OncaParda", new Color(0.96f, 0.91f, 0.82f),
                    $"{fTex}/BaseColorOnca.png", null, false, 0.10f);
                ConvertOncaBlendShapePrefab("Onca-parda.prefab", "Assets/Modelos/Fauna/Prefabs/PFB_Onca_Parda.prefab", matOnca);
            }

            // 3. 11 Espécies de Fauna Adicionais / Atualizadas (Rigs + Animações Reais)
            Material matTamMirim = CreateURPMaterial("Migrados/Mat_Migrado_TamanduaMirim", Color.white, $"{fTex}/tamanduaMirim_basecolor.png", null, false, 0.09f);
            Material matSeriema  = CreateURPMaterial("Migrados/Mat_Migrado_Seriema",       Color.white, $"{fTex}/BaseColor_Seriema.png", null, false, 0.09f);
            Material matTapiti   = CreateURPMaterial("Migrados/Mat_Migrado_Tapiti",        Color.white, $"{fTex}/color_tapiti.png", null, false, 0.09f);
            Material matMacaco   = CreateURPMaterial("Migrados/Mat_Migrado_MacacoPrego",   Color.white, $"{fTex}/Monkey_diffuse.png", $"{fTex}/Monkey_normal.png", false, 0.10f);
            Material matTeiu     = CreateURPMaterial("Migrados/Mat_Migrado_Teiu",          Color.white, $"{fTex}/Aligator_Dif_Tex_0.png", $"{fTex}/NormalMap (7).png", false, 0.14f);
            Material matCarcara  = CreateURPMaterial("Migrados/Mat_Migrado_Carcara",       Color.white, $"{fTex}/carcara_basecolor.png", null, false, 0.09f);
            Material matCoruja   = CreateURPMaterial("Migrados/Mat_Migrado_Coruja",        Color.white, $"{fTex}/Coruja_baseColor.png", $"{fTex}/Owl_MI_normal.png", false, 0.09f);
            Material matJiboia   = CreateURPMaterial("Migrados/Mat_Migrado_Jiboia",        Color.white, $"{fTex}/jiboia_basecolor.png", null, false, 0.15f);
            Material matCascavel = CreateURPMaterial("Migrados/Mat_Migrado_Cascavel",      Color.white, $"{fTex}/Cascavel_1.png", null, false, 0.14f);
            Material matGalito   = CreateURPMaterial("Migrados/Mat_Migrado_Galito",        Color.white, $"{fTex}/Galito_basecolor.png", null, false, 0.09f);
            Material matRato     = CreateURPMaterial("Migrados/Mat_Migrado_RatoEspinho",   Color.white, $"{fTex}/rato_base.png", null, false, 0.09f);

            ConvertAnimalSourcePrefab("TamanduaMirim.prefab",     "Assets/Modelos/Fauna/Prefabs/PFB_Tamandua_Mirim.prefab",    "TamanduaMirim", _ => new[] { matTamMirim });
            ConvertAnimalSourcePrefab("Seriema.prefab",           "Assets/Modelos/Fauna/Prefabs/PFB_Seriema.prefab",           "Seriema",       _ => new[] { matSeriema });
            ConvertAnimalSourcePrefab("Tapiti.prefab",            "Assets/Modelos/Fauna/Prefabs/PFB_Tapiti.prefab",            "Tapiti",        _ => new[] { matTapiti });
            ConvertAnimalSourcePrefab("Macaco-prego.prefab",      "Assets/Modelos/Fauna/Prefabs/PFB_Macaco_Prego.prefab",      "Macaco-prego",  _ => new[] { matMacaco });
            ConvertAnimalSourcePrefab("Teiu.prefab",              "Assets/Modelos/Fauna/Prefabs/PFB_Teiu.prefab",              "Teiu",          _ => new[] { matTeiu });
            ConvertAnimalSourcePrefab("Carcara.prefab",           "Assets/Modelos/Fauna/Prefabs/PFB_Carcara.prefab",           "Carcara",       _ => new[] { matCarcara });
            ConvertAnimalSourcePrefab("Coruja-buraqueira.prefab", "Assets/Modelos/Fauna/Prefabs/PFB_Coruja_Buraqueira.prefab", "Coruja",        _ => new[] { matCoruja });
            ConvertAnimalSourcePrefab("Jiboia.prefab",            "Assets/Modelos/Fauna/Prefabs/PFB_Jiboia.prefab",            "jiboia",        _ => new[] { matJiboia });
            ConvertAnimalSourcePrefab("Cascavel.prefab",          "Assets/Modelos/Fauna/Prefabs/PFB_Cascavel.prefab",          "Cascavel",      _ => new[] { matCascavel });
            ConvertAnimalSourcePrefab("Galito.prefab",            "Assets/Modelos/Fauna/Prefabs/PFB_Galito.prefab",            "Galito",        _ => new[] { matGalito });
            ConvertAnimalSourcePrefab("Rato-de-espinho.prefab",   "Assets/Modelos/Fauna/Prefabs/PFB_Rato_De_Espinho.prefab",   "Clyomys",       _ => new[] { matRato });

            // 4. 12 Espécies de Flora Nativa Adicionais (Todas em URP Lit com AlphaClip em folhas/flores/frutos)
            Material matAratBark  = CreateURPMaterial("Migrados/Mat_Migrado_Araticum_Casca", Color.white, $"{flTex}/Bark_Rough_Diffuse.png", $"{flTex}/Bark_Rough_Normal_0.png", false, 0.08f);
            Material matAratLeaf  = CreateURPMaterial("Migrados/Mat_Migrado_Araticum_Folha", Color.white, $"{flTex}/Leaf_Quercus-agrifolia_Diffuse_3.png", $"{flTex}/Leaf_Quercus-agrifolia_Normal_3.png", true, 0.09f);
            Material matAratFruit = CreateURPMaterial("Migrados/Mat_Migrado_Araticum_Fruto", Color.white, $"{flTex}/Fruto_Araticum.png", null, false, 0.14f);
            ConvertFloraSourcePrefab("Araticum.prefab", "Assets/Modelos/Flora/Prefabs/PFB_Araticum.prefab", r =>
            {
                string n = r.gameObject.name.ToLowerInvariant();
                return n.Contains("folha") ? new[] { matAratLeaf, matAratFruit } : new[] { matAratBark };
            });

            Material matCajuBark  = CreateURPMaterial("Migrados/Mat_Migrado_Cajuzinho_Casca", new Color(0.45f, 0.35f, 0.25f), null, $"{flTex}/Bark_Aesculus-hippocastanum_Normal_1.png", false, 0.08f);
            Material matCajuLeaf  = CreateURPMaterial("Migrados/Mat_Migrado_Cajuzinho_Folha", Color.white, $"{flTex}/Leaf_Zamioculcas_Diffuse.png", $"{flTex}/Leaf_Zamioculcas_Normal.png", true, 0.09f);
            Material matCajuFruit = CreateURPMaterial("Migrados/Mat_Migrado_Cajuzinho_Fruto", Color.white, $"{flTex}/Atlas_Mushrooms_Diffuse.png", $"{flTex}/Atlas_Mushrooms_Normal.png", true, 0.14f);
            ConvertFloraSourcePrefab("Cajuzinho-do-campo.prefab", "Assets/Modelos/Flora/Prefabs/PFB_Cajuzinho_Do_Campo.prefab", r =>
            {
                string n = r.gameObject.name.ToLowerInvariant();
                if (n.Contains("fruto")) return new[] { matCajuFruit };
                return n.Contains("folha") ? new[] { matCajuLeaf } : new[] { matCajuBark };
            });

            Material matCanBark = CreateURPMaterial("Migrados/Mat_Migrado_CanelaEma_Casca", Color.white, $"{flTex}/Bark_Aesculus-hippocastanum_Diffuse.png", $"{flTex}/Bark_Aesculus-hippocastanum_Normal.png", false, 0.08f);
            Material matCanLeaf = CreateURPMaterial("Migrados/Mat_Migrado_CanelaEma_Folha", Color.white, $"{flTex}/Leaf_Sabal-palmetto_Diffuse.png", $"{flTex}/Leaf_Sabal-palmetto_Normal.png", true, 0.09f);
            Material matCanFlor = CreateURPMaterial("Migrados/Mat_Migrado_CanelaEma_Flor",  new Color(0.75f, 0.55f, 0.92f), $"{flTex}/Leaf_Rhododendron_Diffuse.png", $"{flTex}/Leaf_Rhododendron_Normal.png", true, 0.09f);
            ConvertFloraSourcePrefab("Canela-de-ema.prefab", "Assets/Modelos/Flora/Prefabs/PFB_Canela_De_Ema.prefab", r =>
            {
                string n = r.gameObject.name.ToLowerInvariant();
                return n.Contains("folha") ? new[] { matCanLeaf, matCanFlor } : new[] { matCanBark };
            });

            Material matCapim = CreateURPMaterial("Migrados/Mat_Migrado_CapimFlechinha", new Color(0.96f, 0.89f, 0.62f),
                "Assets/Dados/Texturas/Migrados/Grama/Grass_C_BaseColor.png",
                "Assets/Dados/Texturas/Migrados/Grama/Grass_C_Normal.png", true, 0.08f, 0.35f);
            ConvertFloraSourcePrefab("Capim-flechinha.prefab", "Assets/Modelos/Flora/Prefabs/PFB_Capim_Flechinha.prefab", _ => new[] { matCapim });

            Material matCatLeaf = CreateURPMaterial("Migrados/Mat_Migrado_Catuaba_Folha", Color.white, $"{flTex}/Leaf_Rhododendron_Diffuse_0.png", $"{flTex}/Leaf_Rhododendron_Normal_0.png", true, 0.09f);
            Material matCatFlor = CreateURPMaterial("Migrados/Mat_Migrado_Catuaba_Flor",  Color.white, $"{flTex}/FrutoCatuaba.png", null, true, 0.10f);
            ConvertFloraSourcePrefab("Catuaba.prefab", "Assets/Modelos/Flora/Prefabs/PFB_Catuaba.prefab", r =>
            {
                string n = r.gameObject.name.ToLowerInvariant();
                if (n.Contains("flor")) return new[] { matCatFlor };
                return n.Contains("folha") ? new[] { matCatLeaf } : new[] { matCajuBark };
            });

            Material matCinzBark = CreateURPMaterial("Migrados/Mat_Migrado_Cinzeiro_Casca", Color.white, $"{flTex}/Bark_Picea-rubens_Diffuse.png", $"{flTex}/Bark_Picea-rubens_Normal.png", false, 0.08f);
            Material matCinzLeaf = CreateURPMaterial("Migrados/Mat_Migrado_Cinzeiro_Folha", Color.white, $"{flTex}/Leaf_Aesculus-californica_Diffuse.png", $"{flTex}/Leaf_Aesculus-californica_Normal.png", true, 0.09f);
            Material matCinzFlor = CreateURPMaterial("Migrados/Mat_Migrado_Cinzeiro_Flor",  Color.white, $"{flTex}/base.png", $"{flTex}/Leaf_Taxus-baccata_Normal.png", true, 0.09f);
            ConvertFloraSourcePrefab("Cinzeiro.prefab", "Assets/Modelos/Flora/Prefabs/PFB_Cinzeiro.prefab", r =>
            {
                string n = r.gameObject.name.ToLowerInvariant();
                return n.Contains("folha") ? new[] { matCinzLeaf, matCinzFlor } : new[] { matCinzBark };
            });

            Material matCopBark = CreateURPMaterial("Migrados/Mat_Migrado_Copaiba_Casca", Color.white, $"{flTex}/Bark_Hibiscus_Diffuse.png", $"{flTex}/Bark_Hibiscus_Normal.png", false, 0.08f);
            Material matCopLeaf = CreateURPMaterial("Migrados/Mat_Migrado_Copaiba_Folha", Color.white, $"{flTex}/Leaf_Quercus-agrifolia_Diffuse_0.png", $"{flTex}/Leaf_Quercus-agrifolia_Normal_0.png", true, 0.09f);
            Material matCopFrut = CreateURPMaterial("Migrados/Mat_Migrado_Copaiba_Fruto", new Color(0.75f, 0.30f, 0.14f), null, null, false, 0.15f);
            ConvertFloraSourcePrefab("Copaiba.prefab", "Assets/Modelos/Flora/Prefabs/PFB_Copaiba.prefab", r =>
            {
                string n = r.gameObject.name.ToLowerInvariant();
                return n.Contains("folha") ? new[] { matCopLeaf, matCopFrut } : new[] { matCopBark };
            });

            Material matGravLeaf = CreateURPMaterial("Migrados/Mat_Migrado_Gravata_Folha", Color.white, $"{flTex}/Leaf_Podocarpus-macrophyllus_Diffuse.png", $"{flTex}/Leaf_Podocarpus-macrophyllus_Normal.png", true, 0.09f);
            Material matGravFrut = CreateURPMaterial("Migrados/Mat_Migrado_Gravata_Fruto", Color.white, $"{flTex}/D_NQ_NP_860770-MLB41330340593_042020-O-sementes-de-caraguata-gravata-do-cerrado-p-plantio.png", null, false, 0.12f);
            ConvertFloraSourcePrefab("Gravata.prefab", "Assets/Modelos/Flora/Prefabs/PFB_Gravata.prefab", r =>
            {
                string n = r.gameObject.name.ToLowerInvariant();
                return n.Contains("fruto") ? new[] { matGravFrut } : new[] { matGravLeaf };
            });

            Material matIndBark  = CreateURPMaterial("Migrados/Mat_Migrado_Indaia_Casca",  Color.white, $"{flTex}/Bark_Sabal-palmetto_thorns_Diffuse.png", $"{flTex}/Bark_Sabal-palmetto_thorns_Normal.png", false, 0.08f);
            Material matIndLeaf1 = CreateURPMaterial("Migrados/Mat_Migrado_Indaia_Folha1", Color.white, $"{flTex}/Leaf_Areca_Diffuse.png", $"{flTex}/Leaf_Areca_Normal.png", true, 0.09f);
            Material matIndLeaf2 = CreateURPMaterial("Migrados/Mat_Migrado_Indaia_Folha2", Color.white, $"{flTex}/Leaf_Areca2_Diffuse.png", $"{flTex}/Leaf_Areca_Normal.png", true, 0.09f);
            ConvertFloraSourcePrefab("Indaia.prefab", "Assets/Modelos/Flora/Prefabs/PFB_Indaia.prefab", r =>
            {
                string n = r.gameObject.name.ToLowerInvariant();
                return n.Contains("folha") ? new[] { matIndLeaf1, matIndLeaf2 } : new[] { matIndBark };
            });

            Material matLobBark  = CreateURPMaterial("Migrados/Mat_Migrado_Lobeira_Casca",  Color.white, $"{flTex}/Bark_Aesculus-hippocastanum_Diffuse.png", $"{flTex}/Bark_Aesculus-hippocastanum_Normal_0.png", false, 0.08f);
            Material matLobLeaf1 = CreateURPMaterial("Migrados/Mat_Migrado_Lobeira_Folha1", Color.white, $"{flTex}/Leaf_Quercus-agrifolia_Diffuse_2.png", $"{flTex}/Leaf_Quercus-agrifolia_Normal_2.png", true, 0.09f);
            Material matLobLeaf2 = CreateURPMaterial("Migrados/Mat_Migrado_Lobeira_Folha2", Color.white, $"{flTex}/Leaf_Plant_Tomato_RedRobin_Diffuse.png", $"{flTex}/Leaf_Plant_Tomato_RedRobin_Normal.png", true, 0.09f);
            Material matLobFruto = CreateURPMaterial("Migrados/Mat_Migrado_Lobeira_Fruto",  new Color(0.42f, 0.56f, 0.24f), null, null, false, 0.14f);
            ConvertFloraSourcePrefab("Lobeira.prefab", "Assets/Modelos/Flora/Prefabs/PFB_Lobeira.prefab", r =>
            {
                string n = r.gameObject.name.ToLowerInvariant();
                return n.Contains("folha") ? new[] { matLobLeaf1, matLobLeaf2, matLobFruto } : new[] { matLobBark };
            });

            Material matPauBark = CreateURPMaterial("Migrados/Mat_Migrado_PauSanto_Casca", Color.white, $"{flTex}/Bark_Malus-domestica_Diffuse_1.png", $"{flTex}/Bark_Malus-domestica_Normal_2.png", false, 0.08f);
            Material matPauLeaf = CreateURPMaterial("Migrados/Mat_Migrado_PauSanto_Folha", Color.white, $"{flTex}/Leaf_Buxus-semprevirens_Diffuse.png", $"{flTex}/Leaf_Buxus-semprevirens_Normal.png", true, 0.09f);
            ConvertFloraSourcePrefab("Pau-santo.prefab", "Assets/Modelos/Flora/Prefabs/PFB_Pau_Santo.prefab", r =>
            {
                string n = r.gameObject.name.ToLowerInvariant();
                return n.Contains("folha") ? new[] { matPauLeaf } : new[] { matPauBark };
            });

            Material matSucBark = CreateURPMaterial("Migrados/Mat_Migrado_Sucupira_Casca", Color.white, $"{flTex}/Bark_Prunus-cerasifera_Diffuse.png", $"{flTex}/Bark_Prunus-cerasifera_Normal.png", false, 0.08f);
            Material matSucLeaf = CreateURPMaterial("Migrados/Mat_Migrado_Sucupira_Folha", Color.white, $"{flTex}/Leaf_Fraxinus-excelsior_Diffuse.png", $"{flTex}/Leaf_Fraxinus-excelsior_Normal.png", true, 0.09f);
            Material matSucFlor = CreateURPMaterial("Migrados/Mat_Migrado_Sucupira_Flor",  Color.white, $"{flTex}/Flor_Sucupira.png", $"{flTex}/Flower_Verbascum_densiflorum_Normal.png", true, 0.09f);
            ConvertFloraSourcePrefab("Sucupira.prefab", "Assets/Modelos/Flora/Prefabs/PFB_Sucupira.prefab", r =>
            {
                string n = r.gameObject.name.ToLowerInvariant();
                if (n.Contains("flor")) return new[] { matSucFlor };
                return n.Contains("folha") ? new[] { matSucLeaf } : new[] { matSucBark };
            });
        }

        private static void ConvertBiologoGuiaPrefab(string srcFileName, string dstPrefabPath, Func<Renderer, Material[]> matResolver)
        {
            string srcPath = $"{ImportTempFolder}/{srcFileName}";
            GameObject srcAsset = AssetDatabase.LoadAssetAtPath<GameObject>(srcPath);
            if (srcAsset == null) return;

            GameObject inst = UnityEngine.Object.Instantiate(srcAsset);
            try
            {
                inst.name = Path.GetFileNameWithoutExtension(dstPrefabPath);
                StripAllNonVisualComponents(inst, keepAnimator: true);

                var anim = inst.GetComponent<Animator>();
                Avatar charAvatar = AssetDatabase.LoadAssetAtPath<Avatar>("Assets/Modelos/NPC/Migrados/CharacterAvatar.asset");
                if (anim != null && charAvatar != null)
                {
                    anim.avatar = charAvatar;
                    anim.applyRootMotion = false;
                }

                foreach (var r in inst.GetComponentsInChildren<Renderer>(true))
                {
                    if (r is ParticleSystemRenderer) continue;
                    string rn = r.gameObject.name.ToLowerInvariant();
                    // Desativa overlays semitransparentes de tearline/occlusion que ficariam opacos no URP Lit
                    if (rn.Contains("occlusion") || rn.Contains("tearline"))
                    {
                        r.gameObject.SetActive(false);
                        continue;
                    }
                    Material[] desired = matResolver(r);
                    int len = Mathf.Max(1, r.sharedMaterials != null ? r.sharedMaterials.Length : desired.Length);
                    Material[] finalMats = new Material[len];
                    for (int i = 0; i < len; i++)
                    {
                        finalMats[i] = i < desired.Length ? desired[i] : desired[desired.Length - 1];
                    }
                    r.sharedMaterials = finalMats;
                }

                inst.transform.localPosition = Vector3.zero;
                inst.transform.localRotation = Quaternion.identity;
                if (File.Exists(dstPrefabPath)) AssetDatabase.DeleteAsset(dstPrefabPath);
                PrefabUtility.SaveAsPrefabAsset(inst, dstPrefabPath);
            }
            finally
            {
                if (inst != null) UnityEngine.Object.DestroyImmediate(inst);
            }
        }

        private static void ConvertOncaBlendShapePrefab(string srcFileName, string dstPrefabPath, Material matOnca)
        {
            string srcPath = $"{ImportTempFolder}/{srcFileName}";
            GameObject srcAsset = AssetDatabase.LoadAssetAtPath<GameObject>(srcPath);
            if (srcAsset == null) return;

            GameObject inst = UnityEngine.Object.Instantiate(srcAsset);
            try
            {
                SkinnedMeshRenderer oncaSmr = null;
                foreach (var smr in inst.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    if (smr != null && smr.sharedMesh != null && smr.sharedMesh.blendShapeCount > 0)
                    {
                        oncaSmr = smr;
                        break;
                    }
                }

                GameObject cleanRoot = new GameObject(Path.GetFileNameWithoutExtension(dstPrefabPath));
                if (oncaSmr != null)
                {
                    GameObject visualChild = oncaSmr.gameObject;
                    visualChild.transform.SetParent(cleanRoot.transform, false);
                    StripAllNonVisualComponents(visualChild, keepAnimator: false);
                    oncaSmr.sharedMaterials = new[] { matOnca };
                    oncaSmr.updateWhenOffscreen = true;
                    if (oncaSmr.sharedMesh != null)
                    {
                        oncaSmr.localBounds = oncaSmr.sharedMesh.bounds;
                    }
                }

                cleanRoot.transform.localPosition = Vector3.zero;
                cleanRoot.transform.localRotation = Quaternion.identity;
                if (File.Exists(dstPrefabPath)) AssetDatabase.DeleteAsset(dstPrefabPath);
                PrefabUtility.SaveAsPrefabAsset(cleanRoot, dstPrefabPath);
                UnityEngine.Object.DestroyImmediate(cleanRoot);
            }
            finally
            {
                if (inst != null) UnityEngine.Object.DestroyImmediate(inst);
            }
        }

        private static void FixBlendShapePrefabBounds()
        {
            const string oncaPrefabPath = "Assets/Modelos/Fauna/Prefabs/PFB_Onca_Parda.prefab";
            if (!File.Exists(oncaPrefabPath)) return;

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(oncaPrefabPath);
            if (prefabRoot == null) return;
            try
            {
                bool changed = false;
                foreach (var smr in prefabRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    if (smr != null && smr.sharedMesh != null && smr.sharedMesh.blendShapeCount > 0)
                    {
                        if (smr.localBounds.size.sqrMagnitude < 0.001f)
                        {
                            smr.localBounds = smr.sharedMesh.bounds;
                            changed = true;
                        }
                        if (!smr.updateWhenOffscreen)
                        {
                            smr.updateWhenOffscreen = true;
                            changed = true;
                        }
                    }
                }
                if (changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, oncaPrefabPath);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            // Corrige o eixo frontal da Jiboia (rotação Y = -80° original do prefab para cabeça apontar em +Z)
            const string jiboiaPrefabPath = "Assets/Modelos/Fauna/Prefabs/PFB_Jiboia.prefab";
            if (File.Exists(jiboiaPrefabPath))
            {
                GameObject jiboiaRoot = PrefabUtility.LoadPrefabContents(jiboiaPrefabPath);
                if (jiboiaRoot != null)
                {
                    try
                    {
                        Quaternion desiredRot = Quaternion.Euler(0f, -80f, 0f);
                        if (Quaternion.Angle(jiboiaRoot.transform.localRotation, desiredRot) > 0.5f)
                        {
                            jiboiaRoot.transform.localRotation = desiredRot;
                            PrefabUtility.SaveAsPrefabAsset(jiboiaRoot, jiboiaPrefabPath);
                        }
                    }
                    finally
                    {
                        PrefabUtility.UnloadPrefabContents(jiboiaRoot);
                    }
                }
            }
        }

        private static void ConvertAnimalSourcePrefab(string srcFileName, string dstPrefabPath, string preferredAnimatorGoName, Func<Renderer, Material[]> matResolver)
        {
            string srcPath = $"{ImportTempFolder}/{srcFileName}";
            if (!File.Exists(srcPath)) return;
            GameObject srcAsset = AssetDatabase.LoadAssetAtPath<GameObject>(srcPath);
            if (srcAsset == null)
            {
                return;
            }

            GameObject inst = UnityEngine.Object.Instantiate(srcAsset);
            try
            {
                StripGameCerradoHelpers(inst);

                // Determina qual GameObject detém o Animator esquelético principal
                GameObject targetRoot = inst;
                var animators = inst.GetComponentsInChildren<Animator>(true);
                foreach (var a in animators)
                {
                    if (a != null && a.gameObject.name.Equals(preferredAnimatorGoName, StringComparison.OrdinalIgnoreCase))
                    {
                        targetRoot = a.gameObject;
                        break;
                    }
                }

                if (targetRoot != inst)
                {
                    targetRoot.transform.SetParent(null, true);
                }

                targetRoot.name = Path.GetFileNameWithoutExtension(dstPrefabPath);
                StripAllNonVisualComponents(targetRoot, keepAnimator: true);

                foreach (var r in targetRoot.GetComponentsInChildren<Renderer>(true))
                {
                    if (r is ParticleSystemRenderer) continue;
                    Material[] desired = matResolver(r);
                    int len = Mathf.Max(1, r.sharedMaterials != null ? r.sharedMaterials.Length : desired.Length);
                    Material[] finalMats = new Material[len];
                    for (int i = 0; i < len; i++)
                    {
                        finalMats[i] = i < desired.Length ? desired[i] : desired[desired.Length - 1];
                    }
                    r.sharedMaterials = finalMats;
                }

                targetRoot.transform.localPosition = Vector3.zero;
                if (targetRoot == inst)
                {
                    targetRoot.transform.localRotation = Quaternion.identity;
                }

                if (File.Exists(dstPrefabPath)) AssetDatabase.DeleteAsset(dstPrefabPath);
                PrefabUtility.SaveAsPrefabAsset(targetRoot, dstPrefabPath);

                if (targetRoot != inst) UnityEngine.Object.DestroyImmediate(targetRoot);
            }
            finally
            {
                if (inst != null) UnityEngine.Object.DestroyImmediate(inst);
            }
        }

        private static void ConvertFloraSourcePrefab(string srcFileName, string dstPrefabPath, Func<Renderer, Material[]> matResolver)
        {
            string srcPath = $"{ImportTempFolder}/{srcFileName}";
            if (!File.Exists(srcPath)) return;
            GameObject srcAsset = AssetDatabase.LoadAssetAtPath<GameObject>(srcPath);
            if (srcAsset == null)
            {
                return;
            }

            GameObject inst = UnityEngine.Object.Instantiate(srcAsset);
            try
            {
                inst.name = Path.GetFileNameWithoutExtension(dstPrefabPath);
                StripGameCerradoHelpers(inst);
                StripAllNonVisualComponents(inst, keepAnimator: false);

                foreach (var r in inst.GetComponentsInChildren<Renderer>(true))
                {
                    if (r is ParticleSystemRenderer) continue;
                    Material[] desired = matResolver(r);
                    int len = Mathf.Max(1, r.sharedMaterials != null ? r.sharedMaterials.Length : desired.Length);
                    Material[] finalMats = new Material[len];
                    for (int i = 0; i < len; i++)
                    {
                        finalMats[i] = i < desired.Length ? desired[i] : desired[desired.Length - 1];
                    }
                    r.sharedMaterials = finalMats;
                }

                inst.transform.localPosition = Vector3.zero;
                inst.transform.localRotation = Quaternion.identity;

                if (File.Exists(dstPrefabPath)) AssetDatabase.DeleteAsset(dstPrefabPath);
                PrefabUtility.SaveAsPrefabAsset(inst, dstPrefabPath);
            }
            finally
            {
                if (inst != null) UnityEngine.Object.DestroyImmediate(inst);
            }
        }

        private static void StripGameCerradoHelpers(GameObject root)
        {
            var toDelete = new List<GameObject>();
            foreach (var tr in root.GetComponentsInChildren<Transform>(true))
            {
                if (tr == null || tr.gameObject == root) continue;
                string n = tr.gameObject.name;
                if (n.StartsWith("FramingPoint") || n == "Kernel" || n == "MinimapCanvas" ||
                    n == "TrackingIndicator" || n == "Constraints" || n == "Rig IK" || n == "LocatorEffector")
                {
                    toDelete.Add(tr.gameObject);
                }
            }
            foreach (var go in toDelete)
            {
                if (go != null) UnityEngine.Object.DestroyImmediate(go);
            }
        }

        private static void StripAllNonVisualComponents(GameObject root, bool keepAnimator)
        {
            foreach (var tr in root.GetComponentsInChildren<Transform>(true))
            {
                if (tr == null) continue;
                var go = tr.gameObject;
                go.layer = 0;
                go.tag = "Untagged";

                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);

                foreach (var comp in go.GetComponents<Component>())
                {
                    if (comp == null) continue;
                    if (comp is Transform || comp is MeshFilter || comp is MeshRenderer || comp is SkinnedMeshRenderer) continue;
                    if (keepAnimator && comp is Animator && go == root)
                    {
                        var anim = (Animator)comp;
                        anim.applyRootMotion = false;
                        continue;
                    }
                    UnityEngine.Object.DestroyImmediate(comp);
                }
            }
        }

        private static void CleanupTerrainGrassMaterials()
        {
            if (!AssetDatabase.IsValidFolder("Assets/TerrainSampleAssets/Materials")) return;

            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null) return;

            const string gramDir = "Assets/Dados/Texturas/Migrados/Grama";

            var grassSpecs = new Dictionary<string, (string baseTex, string normalTex, Color tint)>
            {
                { "grassdry", ("GrassDry_BaseColor.png", "GrassDry_Normal.png", new Color(0.98f, 0.91f, 0.68f, 1f)) },
                { "grass_c",  ("Grass_C_BaseColor.png",  "Grass_C_Normal.png",  new Color(0.96f, 0.89f, 0.60f, 1f)) },
                { "grass",    ("Grass_A_BaseColor.png",  "Grass_A_Normal.png",  new Color(0.94f, 0.90f, 0.62f, 1f)) },
                { "bushdry",  ("BushDry_BaseColor.png",  "BushDry_Normal.png",  new Color(0.96f, 0.89f, 0.68f, 1f)) },
                { "bush",     ("Bush_A_BaseColor.png",   "Bush_A_Normal.png",   new Color(0.90f, 0.89f, 0.66f, 1f)) },
                { "heather",  ("Heather_BaseColor.png",  "Heather_Normal.png",  new Color(0.94f, 0.88f, 0.72f, 1f)) },
                { "fern",     ("Fern_Diffuse.png",       "Fern_Normal.png",     new Color(0.88f, 0.90f, 0.62f, 1f)) },
                { "plant",    ("Plant_BaseColor.png",    "Plant_Normal.png",    new Color(0.92f, 0.92f, 0.70f, 1f)) },
            };

            string[] matGuids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/TerrainSampleAssets/Materials" });
            foreach (string guid in matGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null) continue;

                string n = mat.name.ToLowerInvariant();
                if (!grassSpecs.TryGetValue(n, out var spec)) continue;

                mat.shader = urpLit;

                Texture2D diff = AssetDatabase.LoadAssetAtPath<Texture2D>($"{gramDir}/{spec.baseTex}");
                Texture2D norm = AssetDatabase.LoadAssetAtPath<Texture2D>($"{gramDir}/{spec.normalTex}");

                mat.color = spec.tint;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", spec.tint);
                if (diff != null)
                {
                    mat.mainTexture = diff;
                    if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", diff);
                }
                if (norm != null && mat.HasProperty("_BumpMap"))
                {
                    mat.SetTexture("_BumpMap", norm);
                    if (mat.HasProperty("_BumpScale")) mat.SetFloat("_BumpScale", 0.85f);
                    mat.EnableKeyword("_NORMALMAP");
                }

                // Desativa AlphaToMask (reforçado com MSAA=1) + AlphaClip double-sided + GPU Instancing
                if (mat.HasProperty("_AlphaToMask")) mat.SetFloat("_AlphaToMask", 0f);
                if (mat.HasProperty("_AlphaClip")) mat.SetFloat("_AlphaClip", 1f);
                if (mat.HasProperty("_Cutoff")) mat.SetFloat("_Cutoff", 0.35f);
                if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", 0f);
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.08f);
                if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
                mat.EnableKeyword("_ALPHATEST_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.enableInstancing = true;
                mat.renderQueue = 2450;
                EditorUtility.SetDirty(mat);
            }
        }

        /// <summary>
        /// Atualiza todos os materiais de folhagem de árvores, arbustos, flores e gramíneas para o shader
        /// Universal Render Pipeline/Cerrado/FoliageTranslucencyWind (translucidez solar + vento orgânico + dupla face + smoothness baixo).
        /// </summary>
        public static void UpgradeAllFoliageAndGrassMaterialsToShowcase()
        {
            Shader foliageShader = Shader.Find("Universal Render Pipeline/Cerrado/FoliageTranslucencyWind");
            if (foliageShader == null) return;

            string[] searchFolders = new[]
            {
                "Assets/TerrainSampleAssets/Materials",
                "Assets/Dados/Materiais"
            };

            string[] matGuids = AssetDatabase.FindAssets("t:Material", searchFolders);
            foreach (string guid in matGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null) continue;

                string ln = mat.name.ToLowerInvariant();
                if (ln.Contains("loboguara") || ln.Contains("terreno") || ln.Contains("skybox") || ln.Contains("npc"))
                    continue;

                bool isGroundGrass = path.StartsWith("Assets/TerrainSampleAssets/Materials") ||
                                     ln.Contains("capim") || ln.Contains("arbusto") || ln.Contains("chuveirinho") ||
                                     ln.Contains("gravata_folha");
                bool isTreeFoliage = ln.Contains("folha") || ln.Contains("flor") || ln.Contains("semente") || ln.Contains("frutinha");

                if (!isGroundGrass && !isTreeFoliage) continue;

                Texture baseTex = mat.HasProperty("_BaseMap") ? mat.GetTexture("_BaseMap") : mat.mainTexture;
                Texture bumpTex = mat.HasProperty("_BumpMap") ? mat.GetTexture("_BumpMap") : null;
                Color baseCol = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : mat.color;

                mat.shader = foliageShader;
                if (baseTex != null)
                {
                    mat.SetTexture("_BaseMap", baseTex);
                    mat.mainTexture = baseTex;
                }
                if (bumpTex != null)
                {
                    mat.SetTexture("_BumpMap", bumpTex);
                    mat.SetFloat("_BumpScale", 0.85f);
                    mat.EnableKeyword("_NORMALMAP");
                }
                else
                {
                    mat.DisableKeyword("_NORMALMAP");
                }

                mat.SetColor("_BaseColor", baseCol);
                mat.color = baseCol;
                mat.SetFloat("_Cutoff", isGroundGrass ? 0.37f : 0.40f);
                mat.SetFloat("_Smoothness", isGroundGrass ? 0.16f : 0.20f);
                mat.SetFloat("_Metallic", 0f);
                mat.SetFloat("_Cull", 0f); // Dupla face (Render Face = Both)
                mat.SetFloat("_AlphaClip", 1f);
                mat.SetFloat("_AlphaToMask", 0f);
                mat.SetFloat("_NormalUpBlend", isGroundGrass ? 0.40f : 0.32f);
                mat.SetColor("_TranslucencyColor", isGroundGrass
                    ? new Color(0.95f, 0.90f, 0.48f, 1f)
                    : new Color(0.86f, 0.96f, 0.42f, 1f));
                mat.SetFloat("_TranslucencyStrength", isGroundGrass ? 0.68f : 0.84f);
                mat.SetFloat("_TranslucencyDistortion", 0.35f);
                mat.SetFloat("_TranslucencyPower", 3.4f);
                mat.SetFloat("_WindIsGroundGrass", isGroundGrass ? 1.0f : 0.0f);
                mat.SetFloat("_WindAmplitude", isGroundGrass ? 0.042f : 0.036f);
                mat.SetFloat("_WindFrequency", isGroundGrass ? 1.55f : 1.35f);
                mat.SetFloat("_WindFlutter", isGroundGrass ? 0.012f : 0.016f);
                mat.enableInstancing = true;
                mat.renderQueue = 2450;
                EditorUtility.SetDirty(mat);
            }
        }

        public static void ConfigureVolumeProfilesAndPipeline()
        {
            ConfigureFourQualityTiersAndURPAssets();

            string[] profilePaths = new[]
            {
                "Assets/Settings/SampleSceneProfile.asset",
                "Assets/Settings/DefaultVolumeProfile.asset"
            };

            foreach (string pp in profilePaths)
            {
                VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(pp);
                if (profile == null) continue;

                if (pp.EndsWith("SampleSceneProfile.asset"))
                {
                    if (!profile.TryGet<Tonemapping>(out var tone))
                        tone = profile.Add<Tonemapping>(true);
                    tone.active = true;
                    tone.mode.overrideState = true;
                    tone.mode.value = TonemappingMode.ACES;

                    if (!profile.TryGet<Bloom>(out var bloom))
                        bloom = profile.Add<Bloom>(true);
                    bloom.active = true;
                    bloom.threshold.overrideState = true;
                    bloom.threshold.value = 0.95f;
                    bloom.intensity.overrideState = true;
                    bloom.intensity.value = 0.34f;
                    bloom.scatter.overrideState = true;
                    bloom.scatter.value = 0.55f;

                    if (!profile.TryGet<Vignette>(out var vignette))
                        vignette = profile.Add<Vignette>(true);
                    vignette.active = true;
                    vignette.intensity.overrideState = true;
                    vignette.intensity.value = 0.22f;
                    vignette.smoothness.overrideState = true;
                    vignette.smoothness.value = 0.35f;

                    // Depth of Field (Bokeh) configurado e mantido inativo até o jogador mirar na qualidade Muito Alta
                    if (!profile.TryGet<DepthOfField>(out var dof))
                        dof = profile.Add<DepthOfField>(true);
                    dof.active = false;
                    dof.mode.overrideState = true;
                    dof.mode.value = DepthOfFieldMode.Bokeh;
                    dof.aperture.overrideState = true;
                    dof.aperture.value = 4.5f;
                    dof.focalLength.overrideState = true;
                    dof.focalLength.value = 42f;
                    dof.focusDistance.overrideState = true;
                    dof.focusDistance.value = 14f;
                }

                // Obrigatório: Motion Blur desligado em TODOS os perfis
                if (profile.TryGet<MotionBlur>(out var mb))
                {
                    mb.active = false;
                    mb.intensity.overrideState = false;
                    mb.intensity.value = 0f;
                }

                EditorUtility.SetDirty(profile);
            }
        }

        /// <summary>
        /// Cria e configura os 4 perfis URP (Baixa, Média, Alta, Muito Alta — Showcase RTX 5070)
        /// com seus respectivos Renderers (SSAO desligado/leve/médio/alto) e os registra no QualitySettings.
        /// </summary>
        private static void ConfigureFourQualityTiersAndURPAssets()
        {
            const string srcRpPath = "Assets/Settings/PC_RPAsset.asset";
            const string srcRendPath = "Assets/Settings/PC_Renderer.asset";
            if (!File.Exists(srcRpPath) || !File.Exists(srcRendPath)) return;

            // Garante GPU Resident Drawer desativado também no PC_RPAsset base
            var baseRp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(srcRpPath);
            if (baseRp != null)
            {
                var soBase = new SerializedObject(baseRp);
                var grdBase = soBase.FindProperty("m_GPUResidentDrawerMode");
                if (grdBase != null) { grdBase.intValue = 0; soBase.ApplyModifiedPropertiesWithoutUndo(); }
                EditorUtility.SetDirty(baseRp);
            }

            var tierSpecs = new[]
            {
                // (qualityName, suffix, renderScale, msaa, shadowRes, shadowDist, cascades, softShadows, softQuality, ssaoOn, ssaoDownsample, ssaoIntensity, ssaoRadius, ssaoSamples, ssaoBlur, mipLimit, lodBias, aniso)
                (name: "Baixa",      suffix: "Baixa",     scale: 0.80f, msaa: 1, sRes: 1024, sDist: 40f,  casc: 2, soft: 0, softQ: 1, ssao: false, ssaoDown: true,  ssaoInt: 0.00f, ssaoRad: 0.25f, ssaoSamp: 0, ssaoBlur: 2, mip: 1, lod: 0.75f, aniso: 1),
                (name: "Media",      suffix: "Media",     scale: 1.00f, msaa: 1, sRes: 2048, sDist: 80f,  casc: 2, soft: 1, softQ: 1, ssao: true,  ssaoDown: true,  ssaoInt: 0.25f, ssaoRad: 0.25f, ssaoSamp: 0, ssaoBlur: 2, mip: 0, lod: 1.05f, aniso: 1),
                (name: "Alta",       suffix: "Alta",      scale: 1.00f, msaa: 2, sRes: 4096, sDist: 150f, casc: 4, soft: 1, softQ: 2, ssao: true,  ssaoDown: false, ssaoInt: 0.42f, ssaoRad: 0.35f, ssaoSamp: 1, ssaoBlur: 1, mip: 0, lod: 1.50f, aniso: 2),
                (name: "Muito Alta", suffix: "MuitoAlta", scale: 1.25f, msaa: 4, sRes: 4096, sDist: 250f, casc: 4, soft: 1, softQ: 3, ssao: true,  ssaoDown: false, ssaoInt: 0.55f, ssaoRad: 0.45f, ssaoSamp: 2, ssaoBlur: 0, mip: 0, lod: 2.00f, aniso: 2)
            };

            var rpAssets = new UniversalRenderPipelineAsset[tierSpecs.Length];

            for (int i = 0; i < tierSpecs.Length; i++)
            {
                var spec = tierSpecs[i];
                string rendPath = $"Assets/Settings/Renderer_{spec.suffix}.asset";
                string rpPath = $"Assets/Settings/URP_{spec.suffix}.asset";

                if (!File.Exists(rendPath)) AssetDatabase.CopyAsset(srcRendPath, rendPath);
                if (!File.Exists(rpPath)) AssetDatabase.CopyAsset(srcRpPath, rpPath);
                AssetDatabase.ImportAsset(rendPath, ImportAssetOptions.ForceSynchronousImport);
                AssetDatabase.ImportAsset(rpPath, ImportAssetOptions.ForceSynchronousImport);

                // Configura SSAO no RendererData específico deste nível
                ScriptableRendererData rendData = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(rendPath);
                if (rendData != null)
                {
                    foreach (var feat in rendData.rendererFeatures)
                    {
                        if (feat == null) continue;
                        if (feat.name.Contains("AmbientOcclusion") || feat.GetType().Name.Contains("ScreenSpaceAmbientOcclusion"))
                        {
                            feat.SetActive(spec.ssao);
                            var soFeat = new SerializedObject(feat);
                            var pSettings = soFeat.FindProperty("m_Settings");
                            if (pSettings != null)
                            {
                                var pSrc = pSettings.FindPropertyRelative("Source");
                                if (pSrc != null) pSrc.intValue = (i >= 2) ? 0 : 1; // DepthNormals em Alta/Muito Alta para sombras de contato precisas nas copas e grama
                                var pDown = pSettings.FindPropertyRelative("Downsample");
                                if (pDown != null) pDown.boolValue = spec.ssaoDown;
                                var pInt = pSettings.FindPropertyRelative("Intensity");
                                if (pInt != null) pInt.floatValue = spec.ssaoInt;
                                var pDirStr = pSettings.FindPropertyRelative("DirectLightingStrength");
                                if (pDirStr != null) pDirStr.floatValue = 0.24f;
                                var pRad = pSettings.FindPropertyRelative("Radius");
                                if (pRad != null) pRad.floatValue = spec.ssaoRad;
                                var pSamp = pSettings.FindPropertyRelative("Samples");
                                if (pSamp != null) pSamp.intValue = spec.ssaoSamp;
                                var pBlur = pSettings.FindPropertyRelative("BlurQuality");
                                if (pBlur != null) pBlur.intValue = spec.ssaoBlur;
                            }
                            soFeat.ApplyModifiedPropertiesWithoutUndo();
                            EditorUtility.SetDirty(feat);
                        }
                    }
                    EditorUtility.SetDirty(rendData);
                }

                // Configura o UniversalRenderPipelineAsset deste nível
                var rp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(rpPath);
                if (rp != null)
                {
                    rp.renderScale = spec.scale;
                    rp.msaaSampleCount = spec.msaa;
                    rp.shadowDistance = spec.sDist;
                    rp.shadowCascadeCount = spec.casc;

                    var soRp = new SerializedObject(rp);
                    var pRendList = soRp.FindProperty("m_RendererDataList");
                    if (pRendList != null && pRendList.isArray && pRendList.arraySize > 0 && rendData != null)
                    {
                        pRendList.GetArrayElementAtIndex(0).objectReferenceValue = rendData;
                    }
                    var pShadowRes = soRp.FindProperty("m_MainLightShadowmapResolution");
                    if (pShadowRes != null) pShadowRes.intValue = spec.sRes;
                    var pAddShadowRes = soRp.FindProperty("m_AdditionalLightsShadowmapResolution");
                    if (pAddShadowRes != null) pAddShadowRes.intValue = Mathf.Min(spec.sRes, 2048);
                    var pSoft = soRp.FindProperty("m_SoftShadowsSupported");
                    if (pSoft != null) pSoft.boolValue = spec.soft != 0;
                    var pSoftQ = soRp.FindProperty("m_SoftShadowQuality");
                    if (pSoftQ != null) pSoftQ.intValue = spec.softQ;
                    // Ajusta Depth Bias e Normal Bias para não cortar sombras de folhas pequenas em Alta / Muito Alta
                    var pDepthBias = soRp.FindProperty("m_ShadowDepthBias");
                    if (pDepthBias != null) pDepthBias.floatValue = (i >= 2) ? 0.04f : 0.10f;
                    var pNormalBias = soRp.FindProperty("m_ShadowNormalBias");
                    if (pNormalBias != null) pNormalBias.floatValue = (i >= 2) ? 0.22f : 0.45f;
                    var pGrd = soRp.FindProperty("m_GPUResidentDrawerMode");
                    if (pGrd != null) pGrd.intValue = 0; // Mantém desativado em todos os perfis (estabilidade DX11)
                    soRp.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(rp);
                    rpAssets[i] = rp;
                }
            }

            // Registra os 4 níveis no ProjectSettings/QualitySettings.asset (com Muito Alta = índice 3 padrão!)
            UnityEngine.Object[] qsAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset");
            if (qsAssets != null && qsAssets.Length > 0)
            {
                var soQs = new SerializedObject(qsAssets[0]);
                var propList = soQs.FindProperty("m_QualitySettings");
                if (propList != null && propList.isArray)
                {
                    while (propList.arraySize < tierSpecs.Length)
                    {
                        propList.InsertArrayElementAtIndex(Mathf.Max(0, propList.arraySize - 1));
                    }
                    while (propList.arraySize > tierSpecs.Length)
                    {
                        propList.DeleteArrayElementAtIndex(propList.arraySize - 1);
                    }

                    for (int i = 0; i < tierSpecs.Length; i++)
                    {
                        var spec = tierSpecs[i];
                        var elem = propList.GetArrayElementAtIndex(i);
                        elem.FindPropertyRelative("name").stringValue = spec.name;
                        elem.FindPropertyRelative("customRenderPipeline").objectReferenceValue = rpAssets[i];
                        elem.FindPropertyRelative("shadowDistance").floatValue = spec.sDist;
                        elem.FindPropertyRelative("shadowCascades").intValue = spec.casc;
                        elem.FindPropertyRelative("globalTextureMipmapLimit").intValue = spec.mip;
                        elem.FindPropertyRelative("lodBias").floatValue = spec.lod;
                        elem.FindPropertyRelative("anisotropicTextures").intValue = spec.aniso;
                        elem.FindPropertyRelative("antiAliasing").intValue = spec.msaa == 1 ? 0 : spec.msaa;
                        var excl = elem.FindPropertyRelative("excludedTargetPlatforms");
                        if (excl != null && excl.isArray) excl.ClearArray();
                    }

                    var curProp = soQs.FindProperty("m_CurrentQuality");
                    if (curProp != null) curProp.intValue = 3; // Muito Alta (Showcase RTX 5070)
                    soQs.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            if (rpAssets[3] != null)
            {
                GraphicsSettings.defaultRenderPipeline = rpAssets[3];
            }
            QualitySettings.SetQualityLevel(3, true);
        }

        private static Material CreateURPMaterial(
            string subPath, Color tint, string diffusePath, string normalPath,
            bool alphaClip, float smoothness, float cutoff = 0.35f)
        {
            string fullPath = $"Assets/Dados/Materiais/{subPath}.mat";
            EnsureFolder(Path.GetDirectoryName(fullPath).Replace("\\", "/"));

            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(fullPath);
            if (mat == null)
            {
                mat = new Material(urpLit);
                mat.name = Path.GetFileNameWithoutExtension(fullPath);
                AssetDatabase.CreateAsset(mat, fullPath);
            }
            else if (mat.shader != urpLit)
            {
                mat.shader = urpLit;
            }

            Texture2D diff = !string.IsNullOrEmpty(diffusePath) ? AssetDatabase.LoadAssetAtPath<Texture2D>(diffusePath) : null;
            Texture2D norm = !string.IsNullOrEmpty(normalPath) ? AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath) : null;

            mat.color = tint;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", tint);
            if (diff != null)
            {
                mat.mainTexture = diff;
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", diff);
            }
            if (norm != null && mat.HasProperty("_BumpMap"))
            {
                mat.SetTexture("_BumpMap", norm);
                if (mat.HasProperty("_BumpScale")) mat.SetFloat("_BumpScale", 1.0f);
                mat.EnableKeyword("_NORMALMAP");
            }
            else
            {
                mat.DisableKeyword("_NORMALMAP");
            }

            if (alphaClip)
            {
                if (mat.HasProperty("_AlphaClip")) mat.SetFloat("_AlphaClip", 1f);
                if (mat.HasProperty("_Cutoff")) mat.SetFloat("_Cutoff", cutoff);
                if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", 0f);
                if (mat.HasProperty("_AlphaToMask")) mat.SetFloat("_AlphaToMask", 0f);
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

            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void EnsureFolder(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath) || AssetDatabase.IsValidFolder(folderPath)) return;
            string parent = Path.GetDirectoryName(folderPath).Replace("\\", "/");
            string name = Path.GetFileName(folderPath);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }
            AssetDatabase.CreateFolder(parent, name);
        }

        [MenuItem("Cerrado/9. Validar Migração e Cenas (Diagnóstico)")]
        public static void ValidateMigrationAndScene()
        {
            var issues = new List<string>();

            if (AssetDatabase.IsValidFolder(ImportTempFolder) || Directory.Exists(ImportTempFolder))
                issues.Add("_ImportTemp ainda existe em Assets/Modelos/_ImportTemp.");

            string[] expectedFonts = {
                CerradoFontSetupEditor.DisplayRubikAssetPath,
                CerradoFontSetupEditor.DisplayFredokaAssetPath,
                CerradoFontSetupEditor.ReadingLexendAssetPath
            };
            foreach (string ef in expectedFonts)
            {
                var fa = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(ef);
                if (fa == null || fa.atlasTexture == null)
                    issues.Add($"TMP_FontAsset ausente ou sem atlas: {ef}");
            }
            if (TMPro.TMP_Settings.defaultFontAsset == null || !TMPro.TMP_Settings.defaultFontAsset.name.Contains("Lexend"))
                issues.Add($"TMP Default Font Asset inesperado: {(TMPro.TMP_Settings.defaultFontAsset != null ? TMPro.TMP_Settings.defaultFontAsset.name : "null")}");
            else
                Debug.Log($"[CerradoValidation] TMP Default Font Asset: {TMPro.TMP_Settings.defaultFontAsset.name} (Rubik/Fredoka/Lexend ativos)");

            string[] expectedRpPaths = { "Assets/Settings/URP_Baixa.asset", "Assets/Settings/URP_Media.asset", "Assets/Settings/URP_Alta.asset", "Assets/Settings/URP_MuitoAlta.asset" };
            foreach (string erp in expectedRpPaths)
            {
                if (AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(erp) == null)
                    issues.Add($"URP Asset de qualidade ausente: {erp}");
            }
            if (QualitySettings.names == null || QualitySettings.names.Length < 4)
                issues.Add($"Esperado 4 níveis de qualidade (Baixa/Media/Alta/Muito Alta), encontrados {(QualitySettings.names != null ? QualitySettings.names.Length : 0)}.");
            else
                Debug.Log($"[CerradoValidation] QualitySettings (4 perfis): {string.Join(", ", QualitySettings.names)} | Atual: {QualitySettings.names[QualitySettings.GetQualityLevel()]}");

            foreach (string pp in new[] { "Assets/Settings/SampleSceneProfile.asset", "Assets/Settings/DefaultVolumeProfile.asset" })
            {
                var prof = AssetDatabase.LoadAssetAtPath<VolumeProfile>(pp);
                if (prof == null)
                {
                    issues.Add($"VolumeProfile ausente: {pp}");
                    continue;
                }
                if (prof.TryGet<MotionBlur>(out var mb) && mb.active)
                    issues.Add($"MotionBlur ainda ativo em {pp}");

                if (pp.EndsWith("SampleSceneProfile.asset"))
                {
                    if (!prof.TryGet<Tonemapping>(out var t) || !t.active || t.mode.value != TonemappingMode.ACES)
                        issues.Add("Tonemapping ACES ausente/inativo em SampleSceneProfile");
                    if (!prof.TryGet<Bloom>(out var b) || !b.active)
                        issues.Add("Bloom inativo em SampleSceneProfile");
                    if (!prof.TryGet<Vignette>(out var v) || !v.active)
                        issues.Add("Vignette inativa em SampleSceneProfile");
                }
            }

            string[] speciesGuids = AssetDatabase.FindAssets("t:SpeciesData", new[] { "Assets/Dados/Especies" });
            int iconsPresent = 0;
            foreach (string g in speciesGuids)
            {
                var sd = AssetDatabase.LoadAssetAtPath<SpeciesData>(AssetDatabase.GUIDToAssetPath(g));
                if (sd != null && sd.Icon != null) iconsPresent++;
            }
            Debug.Log($"[CerradoValidation] SpeciesData SOs: {speciesGuids.Length} | Com Icone: {iconsPresent}");
            if (speciesGuids.Length < 38)
                issues.Add($"Esperado 38 SpeciesData SOs, encontrados {speciesGuids.Length}.");

            int animalLayerIdx = LayerMask.NameToLayer("Animal");
            int fenceLayerIdx = LayerMask.NameToLayer("FenceBoundary");
            if (animalLayerIdx < 0 || fenceLayerIdx < 0)
                issues.Add($"Layers de borda ausentes: Animal={animalLayerIdx}, FenceBoundary={fenceLayerIdx}");
            else if (!Physics.GetIgnoreLayerCollision(animalLayerIdx, fenceLayerIdx))
                issues.Add("Physics.GetIgnoreLayerCollision(Animal, FenceBoundary) está falso!");

            string[] grassMats = AssetDatabase.FindAssets("t:Material", new[] { "Assets/TerrainSampleAssets/Materials" });
            foreach (string g in grassMats)
            {
                string mp = AssetDatabase.GUIDToAssetPath(g);
                var m = AssetDatabase.LoadAssetAtPath<Material>(mp);
                if (m == null) continue;
                string ln = m.name.ToLowerInvariant();
                if ((ln.Contains("grass") || ln.Contains("bush") || ln.Contains("heather"))
                    && (m.shader == null || !m.shader.name.Contains("Universal Render Pipeline") || !m.enableInstancing))
                {
                    issues.Add($"Material de vegetação sem URP/GPU Instancing: {mp}");
                }
            }

            // Verifica SampleScene.unity
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
            if (RenderSettings.skybox == null || !RenderSettings.skybox.name.Contains("Skybox"))
                issues.Add($"Skybox inesperado em SampleScene: {(RenderSettings.skybox != null ? RenderSettings.skybox.name : "null")}");
            if (!RenderSettings.fog || RenderSettings.fogMode != FogMode.Exponential || Mathf.Abs(RenderSettings.fogDensity - 0.0025f) > 0.0005f)
                issues.Add($"Fog inesperada: enabled={RenderSettings.fog}, mode={RenderSettings.fogMode}, density={RenderSettings.fogDensity}");

            int missingScripts = 0;
            int brokenShaders = 0;
            foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (var tr in root.GetComponentsInChildren<Transform>(true))
                {
                    int ms = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(tr.gameObject);
                    if (ms > 0)
                    {
                        missingScripts += ms;
                        issues.Add($"MissingScript ({ms}) em SampleScene: {tr.name}");
                    }
                    var r = tr.GetComponent<Renderer>();
                    if (r != null)
                    {
                        foreach (var mat in r.sharedMaterials)
                        {
                            if (mat == null)
                            {
                                brokenShaders++;
                                issues.Add($"Slot de material nulo em {tr.name}");
                                continue;
                            }
                            string sn = mat.shader != null ? mat.shader.name : "null";
                            if (sn == "null" || sn.Contains("Error") || sn.Contains("NatureManufacture") || sn.Contains("R.A.M"))
                            {
                                brokenShaders++;
                                issues.Add($"Material/Shader inválido '{mat.name}' ({sn}) em {tr.name}");
                            }
                        }
                    }
                }
            }

            var audioMgr = UnityEngine.Object.FindAnyObjectByType<CerradoAudioManager>();
            if (audioMgr == null)
            {
                issues.Add("CerradoAudioManager ausente em SampleScene.");
            }
            else
            {
                var soAud = new SerializedObject(audioMgr);
                int ambCount = soAud.FindProperty("ambientTracks").arraySize;
                int footCount = soAud.FindProperty("dirtFootsteps").arraySize;
                int flipCount = soAud.FindProperty("pageFlipClips").arraySize;
                Debug.Log($"[CerradoValidation] AudioManager -> ambientTracks={ambCount}, dirtFootsteps={footCount}, pageFlipClips={flipCount}");
                if (ambCount < 3 || footCount < 10 || flipCount < 2)
                    issues.Add($"Clipes de áudio incompletos no CerradoAudioManager: amb={ambCount}, foot={footCount}, flip={flipCount}");
            }

            GameObject terrainObj = GameObject.Find("Chao_Cerrado_Terreno_NOVO");
            Collider terrainCol = terrainObj != null ? terrainObj.GetComponentInChildren<Collider>() : null;

            GameObject vegObj = GameObject.Find("_Vegetacao");
            if (vegObj != null)
                Debug.Log($"[CerradoValidation] Vegetacao total filhos: {vegObj.transform.childCount}");

            GameObject entidades = GameObject.Find("_Entidades");
            if (entidades == null)
            {
                issues.Add("Container _Entidades não encontrado!");
            }
            else
            {
                foreach (Transform child in entidades.transform)
                {
                    var rends = child.GetComponentsInChildren<Renderer>(true);
                    Bounds b = new Bounds();
                    bool hasActiveRend = false;
                    foreach (var r in rends)
                    {
                        if (r == null || !r.gameObject.activeInHierarchy || r is ParticleSystemRenderer) continue;
                        Bounds rb = r.bounds;
                        if (r is SkinnedMeshRenderer smr && (smr.bones == null || smr.bones.Length == 0) && smr.sharedMesh != null)
                        {
                            Bounds mb = smr.sharedMesh.bounds;
                            Vector3 c = mb.center;
                            Vector3 e = mb.extents;
                            Matrix4x4 m = smr.transform.localToWorldMatrix;
                            rb = new Bounds(m.MultiplyPoint3x4(c + new Vector3(-e.x, -e.y, -e.z)), Vector3.zero);
                            for (int xi = -1; xi <= 1; xi += 2)
                                for (int yi = -1; yi <= 1; yi += 2)
                                    for (int zi = -1; zi <= 1; zi += 2)
                                        rb.Encapsulate(m.MultiplyPoint3x4(c + new Vector3(e.x * xi, e.y * yi, e.z * zi)));
                        }
                        if (!hasActiveRend) { b = rb; hasActiveRend = true; }
                        else b.Encapsulate(rb);
                    }
                    if (!hasActiveRend)
                    {
                        issues.Add($"Entidade sem Renderer ativo: {child.name}");
                        continue;
                    }

                    float groundDiff = float.NaN;
                    Ray downRay = new Ray(child.position + Vector3.up * 5f, Vector3.down);
                    if (terrainCol != null && terrainCol.Raycast(downRay, out RaycastHit hit, 25f))
                        groundDiff = b.min.y - hit.point.y;

                    var ai = child.GetComponent<AnimalAI>();
                    var agent = child.GetComponent<NavMeshAgent>();
                    var fp = child.GetComponent<FootPlanter>();
                    var oncaBs = child.GetComponentInChildren<OncaBlendShapeController>(true);
                    var anim = child.GetComponentInChildren<Animator>(true);
                    bool onNav = agent == null || NavMesh.SamplePosition(child.position, out _, 2.5f, NavMesh.AllAreas);
                    int clips = anim != null && anim.runtimeAnimatorController != null ? anim.runtimeAnimatorController.animationClips.Length : 0;

                    if (child.name == "Onca_Parda" || child.name == "Lobo_Guara" || child.name == "Cascavel" || child.name == "Jiboia")
                    {
                        Transform vis = child.Find("Modelo_3D");
                        float visYaw = vis != null ? vis.localEulerAngles.y : 0f;
                        Debug.Log($"[CerradoValidation] Destaque {child.name}: size=({b.size.x:F2}, {b.size.y:F2}, {b.size.z:F2}), minY-ground={groundDiff:F3}m, visYaw={visYaw:F1}deg, oncaBS={oncaBs != null}, animClips={clips}, onNav={onNav}");
                    }

                    float maxDim = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
                    if (ai != null && (b.size.y < 0.06f || maxDim < 0.18f)) issues.Add($"Animal microscópico/achatado (h={b.size.y:F3}m, max={maxDim:F3}m): {child.name}");
                    if (ai != null && !float.IsNaN(groundDiff) && Mathf.Abs(groundDiff) > 0.35f) issues.Add($"Animal desalinhado do solo (minY-ground={groundDiff:F3}m): {child.name}");
                    if (ai != null && clips == 0 && oncaBs == null) issues.Add($"Animal sem AnimationClips nem BlendShapeController: {child.name}");
                    if (ai != null && fp == null) issues.Add($"Animal sem FootPlanter: {child.name}");
                    if (agent != null && !onNav) issues.Add($"Agente fora da NavMesh: {child.name}");
                }
                Debug.Log($"[CerradoValidation] Total entidades em _Entidades: {entidades.transform.childCount}");

                GameObject npcObj = GameObject.Find("NPC_Pesquisador");
                if (npcObj != null)
                {
                    var npcAnim = npcObj.GetComponentInChildren<Animator>(true);
                    int npcClips = npcAnim != null && npcAnim.runtimeAnimatorController != null ? npcAnim.runtimeAnimatorController.animationClips.Length : 0;
                    Debug.Log($"[CerradoValidation] NPC_Pesquisador (Prof. Silva): Animator={npcAnim != null}, Avatar={(npcAnim != null && npcAnim.avatar != null)}, animClips={npcClips}");
                    if (npcClips < 2) issues.Add($"NPC_Pesquisador sem animações Idle/Talk (clips={npcClips})");
                }
            }

            if (UnityEngine.Object.FindAnyObjectByType<Cerrado.UI.SettingsPanelUI>(FindObjectsInactive.Include) == null)
                issues.Add("SettingsPanelUI ausente em SampleScene (Menu de Pausa).");
            if (UnityEngine.Object.FindAnyObjectByType<Cerrado.UI.TutorialOverlayUI>(FindObjectsInactive.Include) == null)
                issues.Add("TutorialOverlayUI ausente em SampleScene.");
            if (UnityEngine.Object.FindAnyObjectByType<ReflectionProbe>() == null)
                issues.Add("ReflectionProbe ausente em SampleScene.");
            if (UnityEngine.Object.FindAnyObjectByType<LightProbeGroup>() == null)
                issues.Add("LightProbeGroup ausente em SampleScene.");

            int foliageWindMatCount = 0;
            foreach (string g in AssetDatabase.FindAssets("t:Material", new[] { "Assets/Dados/Materiais", "Assets/TerrainSampleAssets/Materials" }))
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g));
                if (m != null && m.shader != null && m.shader.name == "Universal Render Pipeline/Cerrado/FoliageTranslucencyWind")
                    foliageWindMatCount++;
            }
            Debug.Log($"[CerradoValidation] Materiais de folhagem/grama com Translucidez + Vento: {foliageWindMatCount}");
            if (foliageWindMatCount < 20)
                issues.Add($"Poucos materiais usando FoliageTranslucencyWind ({foliageWindMatCount}).");

            // Verifica menu_cena.unity e mantém menu_cena.unity aberta como cena inicial do jogo!
            EditorSceneManager.OpenScene("Assets/Jogo/Menu/menu_cena.unity", OpenSceneMode.Single);
            if (UnityEngine.Object.FindAnyObjectByType<Cerrado.UI.SettingsPanelUI>(FindObjectsInactive.Include) == null)
                issues.Add("SettingsPanelUI ausente em menu_cena.unity.");
            foreach (var tmp in UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (tmp != null && !string.IsNullOrEmpty(tmp.text) && tmp.text.IndexOf("André", StringComparison.OrdinalIgnoreCase) >= 0)
                    issues.Add($"Nome removido ainda presente em menu_cena ({tmp.name}).");
            }
            foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (var tr in root.GetComponentsInChildren<Transform>(true))
                {
                    int ms = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(tr.gameObject);
                    if (ms > 0) issues.Add($"MissingScript ({ms}) em menu_cena: {tr.name}");
                }
            }

            // Garante que menu_cena.unity está no índice 0 e SampleScene.unity no índice 1 das Build Settings
            if (EditorBuildSettings.scenes == null || EditorBuildSettings.scenes.Length < 2 ||
                EditorBuildSettings.scenes[0].path != "Assets/Jogo/Menu/menu_cena.unity" ||
                !EditorBuildSettings.scenes[0].enabled ||
                EditorBuildSettings.scenes[1].path != "Assets/Scenes/SampleScene.unity" ||
                !EditorBuildSettings.scenes[1].enabled)
            {
                issues.Add("EditorBuildSettings não tem menu_cena.unity no índice 0 e SampleScene.unity no índice 1!");
            }

            if (issues.Count == 0)
            {
                Debug.Log("[CerradoValidation] SUCESSO TOTAL: 0 erros, 0 shaders quebrados, Fauna/Flora/Skybox/Audio/Volume validados!");
            }
            else
            {
                foreach (string iss in issues) Debug.LogError($"[CerradoValidation] FALHA: {iss}");
            }
        }
    }
}
