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
using UnityEngine.AI;

namespace Cerrado.EditorTools
{
    public static class CerradoSceneSetupEditor
    {
        [MenuItem("Cerrado/4. Montar Cena de Teste Completa (SampleScene)")]
        public static void BuildCompleteTestScene()
        {
            if (!EditorUtility.DisplayDialog("Cerrado Engine", 
                "Deseja configurar a cena com Player, Terreno do Cerrado, Vegetação Nativa, Modelos 3D, IA, Diálogos do Guia, Loja e UI completa?", 
                "Sim, Configurar Tudo!", "Cancelar"))
            {
                return;
            }

            Undo.SetCurrentGroupName("Cerrado: Montagem Completa da Cena");
            int undoGroup = Undo.GetCurrentGroup();

            try
            {
                // 1. Chão e Terreno Estilizado do Cerrado
                GameObject ground = SetupGround();

                // 2. Luz e Atmosfera (Céu e Sol)
                Light sun = SetupLighting();

                // 3. Gerenciadores Centrais (Álbum, Missões, Loja, Dia/Noite)
                SetupManagers(sun);

                // 4. Player e Câmera
                GameObject player = SetupPlayer();
                var playerController = player.GetComponent<PlayerController>();

                // 5. NPC Guia e Comerciante
                GameObject npc = SetupNPC(new Vector3(0f, 0f, 3.5f));
                var guideNPC = npc.GetComponent<GuideNPC>();
                var shopNPC = npc.GetComponent<ShopNPC>();

                // 6. Modelos 3D, Vegetação e Marcadores de Cenário
                SetupEntities();
                SetupVegetation();
                SetupSceneBookmarks();

                // 7. UI Canvas Completa (Visor, Diálogos do Guia, Álbum, Loja, Missões)
                SetupUI(playerController, guideNPC, shopNPC);

                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

                EditorUtility.DisplayDialog("Cerrado Engine", 
                    "Cena do Cerrado montada com sucesso!\n\n" +
                    "• Terreno verde-savana com vegetação e capim nativo\n" +
                    "• Atmosfera com névoa volumétrica (WispySmoke) e poeira nos animais\n" +
                    "• Marcadores de cenário para navegação rápida de câmera na Scene View\n" +
                    "• Biólogo Guia posicionado à sua frente ([E] para iniciar o tutorial)\n" +
                    "• Animais (Tamanduá, Ema, Lobo-guará) e árvores (Jatobá)\n" +
                    "• Interface completa: Visor com zoom, Diálogo com opções, Álbum (Tab) e Missões no HUD\n\n" +
                    "Basta apertar PLAY no topo do Unity para testar!", "OK");
            }
            finally
            {
                Undo.CollapseUndoOperations(undoGroup);
            }
        }

        [MenuItem("Cerrado/5. Aplicar Novos Efeitos e Marcadores no Cenário Atual")]
        public static void ApplyAtmosphereAndBookmarksToCurrentScene()
        {
            Undo.SetCurrentGroupName("Cerrado: Aplicar Efeitos e Marcadores");
            int undoGroup = Undo.GetCurrentGroup();

            try
            {
                // 1. Corrigir materiais rosa (incompatíveis com URP) em todos os modelos da cena
                int fixedMats = FixAllPinkMaterialsInScene();

                // 2. Atmosfera e Névoa
                GameObject ambObj = GameObject.Find("_Ambiente");
                if (ambObj == null) ambObj = new GameObject("_Ambiente");
                var atm = GetOrAddComponent<CerradoAtmosphere>(ambObj);
                atm.SetupMistParticleSystem();
                GetOrAddComponent<TerrainTreeToggle>(ambObj);

                // 3. Poeira nos Animais
                var animals = Object.FindObjectsByType<AnimalAI>(FindObjectsInactive.Include);
                int count = 0;
                foreach (var a in animals)
                {
                    GetOrAddComponent<AnimalDustTrail>(a.gameObject);
                    count++;
                }

                // 4. Marcadores de Cenário (SceneAnnotation)
                SetupSceneBookmarks();

                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

                EditorUtility.DisplayDialog("Cerrado Engine",
                    $"Novos efeitos aplicados com sucesso no cenário atual!\n\n" +
                    $"• {fixedMats} materiais incompatíveis (rosa) corrigidos para shaders nativos URP Lit\n" +
                    $"• Névoa volumétrica do Cerrado (WispySmoke) configurada em '_Ambiente'\n" +
                    $"• Rastro de poeira avermelhada (AnimalDustTrail) adicionado a {count} animais\n" +
                    $"• Marcadores de cenário (SceneAnnotation) criados em '_Marcadores_Cenário'\n" +
                    $"• Utilitário TerrainTreeToggle adicionado para alternar folhagem em edição", "OK");
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
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null || mats[i].shader == null || 
                        mats[i].shader.name.StartsWith("Hidden/InternalErrorShader") ||
                        mats[i].shader.name == "Hidden/DefaultErrorShader" ||
                        !mats[i].shader.name.Contains("Universal Render Pipeline"))
                    {
                        string objName = r.gameObject.name.ToLower();
                        Transform p = r.transform.parent;
                        while (p != null && !objName.Contains("tamandua") && !objName.Contains("ema") && !objName.Contains("lobo") && !objName.Contains("jatoba") && !objName.Contains("npc"))
                        {
                            objName += " " + p.gameObject.name.ToLower();
                            p = p.parent;
                        }

                        if (objName.Contains("tamandua"))
                            mats[i] = GetOrCreateMaterial("Mat_Tamandua_Bandeira", new Color(0.46f, 0.36f, 0.26f));
                        else if (objName.Contains("ema"))
                            mats[i] = GetOrCreateMaterial("Mat_Ema", new Color(0.42f, 0.42f, 0.46f));
                        else if (objName.Contains("lobo"))
                            mats[i] = GetOrCreateMaterial("Mat_Lobo_Guara", new Color(0.82f, 0.44f, 0.18f));
                        else if (objName.Contains("jatoba"))
                            mats[i] = GetOrCreateMaterial("Mat_Jatoba", new Color(0.35f, 0.48f, 0.20f));
                        else if (objName.Contains("npc"))
                            mats[i] = GetOrCreateMaterial("Mat_NPC_Pesquisador", new Color(0.30f, 0.45f, 0.65f));
                        else
                            mats[i] = GetOrCreateMaterial("Mat_Elemento_Cerrado", new Color(0.55f, 0.50f, 0.40f));

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

        public static void FixModelMaterials(GameObject root, Color fallbackColor, string matName)
        {
            if (root == null) return;

            Material sharedMat = GetOrCreateMaterial(matName, fallbackColor);
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                if (r is ParticleSystemRenderer) continue;

                var mats = r.sharedMaterials;
                if (mats == null || mats.Length == 0)
                {
                    r.sharedMaterial = sharedMat;
                    EditorUtility.SetDirty(r);
                    continue;
                }

                bool modified = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null || mats[i].shader == null || 
                        mats[i].shader.name.StartsWith("Hidden/InternalErrorShader") ||
                        mats[i].shader.name == "Hidden/DefaultErrorShader" ||
                        !mats[i].shader.name.Contains("Universal Render Pipeline"))
                    {
                        mats[i] = sharedMat;
                        modified = true;
                    }
                }
                if (modified)
                {
                    r.sharedMaterials = mats;
                    EditorUtility.SetDirty(r);
                }
            }
        }

        private static T GetOrAddComponent<T>(GameObject obj) where T : Component
        {
            if (obj == null) return null;
            if (obj.TryGetComponent<T>(out var existing))
            {
                return existing;
            }
            return obj.AddComponent<T>();
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
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Diffuse");
                mat = new Material(shader);
                mat.name = name;
                mat.color = color;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
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

        private static GameObject SetupGround()
        {
            GameObject ground = GameObject.Find("Chao_Cerrado");
            if (ground == null)
            {
                ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = "Chao_Cerrado";
                ground.transform.position = Vector3.zero;
                ground.transform.localScale = new Vector3(8f, 1f, 8f); // 80x80 metros
            }

            // Material com cor de savana/capim seco do Cerrado
            var renderer = ground.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = GetOrCreateMaterial("Mat_Chao_Cerrado", new Color(0.48f, 0.55f, 0.30f));
            }

            return ground;
        }

        private static Light SetupLighting()
        {
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

            sun.intensity = 1.35f;
            sun.color = new Color(1f, 0.95f, 0.82f); // Luz dourada acolhedora
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // Atmosfera suave
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.48f, 0.68f, 0.95f);
            RenderSettings.ambientEquatorColor = new Color(0.85f, 0.72f, 0.50f);
            RenderSettings.ambientGroundColor = new Color(0.38f, 0.28f, 0.18f);

            GameObject ambObj = GameObject.Find("_Ambiente") ?? new GameObject("_Ambiente");
            GetOrAddComponent<CerradoAtmosphere>(ambObj);
            GetOrAddComponent<TerrainTreeToggle>(ambObj);

            return sun;
        }

        private static void SetupManagers(Light sun)
        {
            GameObject managers = GameObject.Find("_Managers");
            if (managers == null)
            {
                managers = new GameObject("_Managers");
            }

            // 1. AlbumManager
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

            // 2. EquipmentManager
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

            // 3. QuestManager
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

            // 4. DayNightCycle
            var dayNight = GetOrAddComponent<DayNightCycle>(managers);
            var soDayNight = new SerializedObject(dayNight);
            soDayNight.FindProperty("directionalLight").objectReferenceValue = sun;
            soDayNight.FindProperty("dayDurationInMinutes").floatValue = 10f;
            soDayNight.ApplyModifiedProperties();
        }

        private static GameObject SetupPlayer()
        {
            GameObject player = GameObject.Find("Player");
            if (player == null)
            {
                player = new GameObject("Player");
                player.tag = "Player";
            }
            player.transform.position = new Vector3(0, 1.1f, 0);

            var cc = GetOrAddComponent<CharacterController>(player);
            cc.height = 1.8f;
            cc.radius = 0.4f;
            cc.center = new Vector3(0, 0.9f, 0);

            var playerController = GetOrAddComponent<PlayerController>(player);

            // Câmera do Player
            Camera cam = player.GetComponentInChildren<Camera>();
            if (cam == null)
            {
                var mainCamObj = GameObject.Find("Main Camera");
                if (mainCamObj != null)
                {
                    mainCamObj.transform.SetParent(player.transform);
                    mainCamObj.transform.localPosition = new Vector3(0, 1.6f, 0);
                    mainCamObj.transform.localRotation = Quaternion.identity;
                    cam = mainCamObj.GetComponent<Camera>();
                }
                else
                {
                    GameObject camObj = new GameObject("Main Camera");
                    camObj.transform.SetParent(player.transform);
                    camObj.transform.localPosition = new Vector3(0, 1.6f, 0);
                    camObj.transform.localRotation = Quaternion.identity;
                    cam = camObj.AddComponent<Camera>();
                    camObj.AddComponent<AudioListener>();
                }
            }

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

        private static GameObject SetupNPC(Vector3 position)
        {
            GameObject npcObj = GameObject.Find("NPC_Pesquisador");
            if (npcObj == null)
            {
                GameObject npcPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Modelos/Fauna/tamandua npc.blend");
                if (npcPrefab != null)
                {
                    npcObj = (GameObject)PrefabUtility.InstantiatePrefab(npcPrefab);
                    npcObj.name = "NPC_Pesquisador";
                    FixModelMaterials(npcObj, new Color(0.25f, 0.45f, 0.70f), "Mat_NPC_Pesquisador");
                }
                else
                {
                    npcObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    npcObj.name = "NPC_Pesquisador";
                    var rnd = npcObj.GetComponent<MeshRenderer>();
                    if (rnd != null) rnd.sharedMaterial = GetOrCreateMaterial("Mat_NPC_Pesquisador", new Color(0.25f, 0.45f, 0.70f));
                }
            }

            npcObj.transform.position = position;
            npcObj.transform.rotation = Quaternion.Euler(0, 180f, 0); // Encarando o player

            var col = GetOrAddComponent<CapsuleCollider>(npcObj);
            col.radius = 0.5f;
            col.height = 1.7f;
            col.center = new Vector3(0, 0.85f, 0);

            GetOrAddComponent<ShopNPC>(npcObj);
            GetOrAddComponent<GuideNPC>(npcObj);

            return npcObj;
        }

        private static void SetupEntities()
        {
            GameObject entitiesGroup = GameObject.Find("_Entidades");
            if (entitiesGroup == null) entitiesGroup = new GameObject("_Entidades");

            // 1. Tamanduá-bandeira
            SetupAnimal(
                entitiesGroup.transform,
                "Tamandua_Bandeira",
                "Assets/Modelos/Fauna/tamandua_bandeira.blend",
                "Assets/Dados/Especies/TamanduaBandeira.asset",
                new Vector3(4f, 0f, 7f),
                AnimalTemperament.Docil, 1.8f, 4.5f, 0.5f, 1.2f,
                new Color(0.48f, 0.35f, 0.22f)
            );

            // 2. Ema
            SetupAnimal(
                entitiesGroup.transform,
                "Ema",
                "Assets/Modelos/Fauna/ema.blend",
                "Assets/Dados/Especies/Ema.asset",
                new Vector3(-6f, 0f, 10f),
                AnimalTemperament.Arisco, 2.8f, 8.0f, 0.4f, 1.8f,
                new Color(0.32f, 0.32f, 0.38f)
            );

            // 3. Lobo-guará
            SetupAnimal(
                entitiesGroup.transform,
                "Lobo_Guara",
                "Assets/Modelos/Fauna/lobo guará.fbx",
                "Assets/Dados/Especies/LoboGuara.asset",
                new Vector3(8f, 0f, 14f),
                AnimalTemperament.Cauteloso, 2.4f, 6.5f, 0.45f, 1.3f,
                new Color(0.78f, 0.42f, 0.18f) // Dourado-avermelhado
            );

            // 4. Jatobá (Flora)
            SetupPlant(
                entitiesGroup.transform,
                "Jatoba",
                "Assets/Modelos/Flora/jatoba_remake.blend",
                "Assets/Dados/Especies/Jatoba.asset",
                new Vector3(0f, 0f, 15f)
            );
        }

        private static void SetupVegetation()
        {
            GameObject vegGroup = GameObject.Find("_Vegetacao");
            if (vegGroup != null) return; // Já criada

            vegGroup = new GameObject("_Vegetacao");

            string[] prefabPaths = new[]
            {
                "Assets/TerrainSampleAssets/Prefabs/GrassDry_A.prefab",
                "Assets/TerrainSampleAssets/Prefabs/GrassDry_B.prefab",
                "Assets/TerrainSampleAssets/Prefabs/Grass_A.prefab",
                "Assets/TerrainSampleAssets/Prefabs/BushDry_A.prefab",
                "Assets/TerrainSampleAssets/Prefabs/Heather_A.prefab"
            };

            List<GameObject> prefabs = new List<GameObject>();
            foreach (var path in prefabPaths)
            {
                var p = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (p != null) prefabs.Add(p);
            }

            if (prefabs.Count == 0) return;

            // Espalha tufos de vegetação natural ao redor do cenário
            Random.InitState(42);
            for (int i = 0; i < 35; i++)
            {
                Vector2 circle = Random.insideUnitCircle * 35f;
                if (circle.magnitude < 3f) continue; // Não encobrir o player no início

                Vector3 pos = new Vector3(circle.x, 0f, circle.y);
                GameObject chosenPrefab = prefabs[Random.Range(0, prefabs.Count)];
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(chosenPrefab, vegGroup.transform);
                instance.transform.position = pos;
                instance.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
                float scale = Random.Range(0.8f, 1.4f);
                instance.transform.localScale = Vector3.one * scale;
            }

            // Montanhas no horizonte
            GameObject mountainPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TerrainSampleAssets/Models/SkyboxMountains01.fbx");
            if (mountainPrefab != null)
            {
                GameObject mountain = (GameObject)PrefabUtility.InstantiatePrefab(mountainPrefab, vegGroup.transform);
                mountain.name = "Horizonte_Serra";
                mountain.transform.position = new Vector3(0, -2f, 75f);
                mountain.transform.localScale = new Vector3(6f, 5f, 6f);
            }
        }

        private static void SetupSceneBookmarks()
        {
            GameObject bookmarksGroup = GameObject.Find("_Marcadores_Cenário");
            if (bookmarksGroup != null) return;

            bookmarksGroup = new GameObject("_Marcadores_Cenário");

            CreateBookmark(bookmarksGroup.transform, "Marcador_01_Acampamento", "1. Acampamento Base & Biólogo Guia", 1, new Vector3(0f, 1.5f, 1.5f), Quaternion.Euler(12f, 0f, 0f));
            CreateBookmark(bookmarksGroup.transform, "Marcador_02_Tamandua", "2. Savana dos Tamanduás-Bandeira", 2, new Vector3(4f, 1.5f, 4.5f), Quaternion.Euler(15f, 25f, 0f));
            CreateBookmark(bookmarksGroup.transform, "Marcador_03_Ema", "3. Trilha das Emas & Gramíneas", 3, new Vector3(-6f, 1.5f, 7.5f), Quaternion.Euler(15f, -20f, 0f));
            CreateBookmark(bookmarksGroup.transform, "Marcador_04_LoboGuara", "4. Habitat do Lobo-Guará (Colinas)", 4, new Vector3(8f, 2f, 11f), Quaternion.Euler(15f, 15f, 0f));
            CreateBookmark(bookmarksGroup.transform, "Marcador_05_Jatoba", "5. Bosque de Jatobás Centenários", 5, new Vector3(0f, 2.5f, 10f), Quaternion.Euler(15f, 0f, 0f));
        }

        private static void CreateBookmark(Transform parent, string name, string headline, int id, Vector3 pos, Quaternion rot)
        {
            GameObject bookmark = new GameObject(name);
            bookmark.transform.SetParent(parent);
            bookmark.transform.position = pos;
            bookmark.transform.rotation = rot;

            var annotation = bookmark.AddComponent<SceneAnnotation>();
            annotation.headline = headline;
            annotation.id = id;
        }

        private static void SetupAnimal(
            Transform parent, string name, string modelPath, string speciesPath,
            Vector3 position, AnimalTemperament temperament, float walkSpeed, float fleeSpeed,
            float colliderRadius, float colliderHeight, Color placeholderColor)
        {
            if (GameObject.Find(name) != null) return;

            GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            GameObject animalObj;
            if (modelPrefab != null)
            {
                animalObj = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab, parent);
                animalObj.name = name;
                FixModelMaterials(animalObj, placeholderColor, "Mat_" + name);
            }
            else
            {
                animalObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                animalObj.name = name;
                animalObj.transform.SetParent(parent);
                var rnd = animalObj.GetComponent<MeshRenderer>();
                if (rnd != null) rnd.sharedMaterial = GetOrCreateMaterial("Mat_" + name, placeholderColor);
            }

            animalObj.transform.position = position;

            var col = GetOrAddComponent<CapsuleCollider>(animalObj);
            col.radius = colliderRadius;
            col.height = colliderHeight;
            col.center = new Vector3(0, colliderHeight * 0.5f, 0);

            var agent = GetOrAddComponent<NavMeshAgent>(animalObj);
            agent.radius = colliderRadius;
            agent.height = colliderHeight;
            agent.speed = walkSpeed;

            var ai = GetOrAddComponent<AnimalAI>(animalObj);
            var soAI = new SerializedObject(ai);
            soAI.FindProperty("temperament").enumValueIndex = (int)temperament;
            soAI.FindProperty("walkSpeed").floatValue = walkSpeed;
            soAI.FindProperty("fleeSpeed").floatValue = fleeSpeed;
            soAI.ApplyModifiedProperties();

            GetOrAddComponent<AnimalDustTrail>(animalObj);

            var target = GetOrAddComponent<PhotographableTarget>(animalObj);
            var speciesData = AssetDatabase.LoadAssetAtPath<SpeciesData>(speciesPath);
            var soTarget = new SerializedObject(target);
            soTarget.FindProperty("speciesData").objectReferenceValue = speciesData;
            soTarget.FindProperty("focalPointOffset").vector3Value = new Vector3(0, colliderHeight * 0.5f, 0);
            soTarget.ApplyModifiedProperties();
        }

        private static void SetupPlant(Transform parent, string name, string modelPath, string speciesPath, Vector3 position)
        {
            if (GameObject.Find(name) != null) return;

            GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            GameObject plantObj;
            if (modelPrefab != null)
            {
                plantObj = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab, parent);
                plantObj.name = name;
                FixModelMaterials(plantObj, new Color(0.35f, 0.48f, 0.20f), "Mat_" + name);
            }
            else
            {
                // Árvore estilizada composta (Tronco + Copa)
                plantObj = new GameObject(name);
                plantObj.transform.SetParent(parent);

                GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                trunk.name = "Tronco";
                trunk.transform.SetParent(plantObj.transform);
                trunk.transform.localPosition = new Vector3(0, 1.75f, 0);
                trunk.transform.localScale = new Vector3(0.6f, 1.75f, 0.6f);
                var rndTrunk = trunk.GetComponent<MeshRenderer>();
                if (rndTrunk != null) rndTrunk.sharedMaterial = GetOrCreateMaterial("Mat_Tronco", new Color(0.38f, 0.25f, 0.15f));

                GameObject foliage = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                foliage.name = "Copa";
                foliage.transform.SetParent(plantObj.transform);
                foliage.transform.localPosition = new Vector3(0, 4.2f, 0);
                foliage.transform.localScale = new Vector3(3.5f, 2.5f, 3.5f);
                var rndFol = foliage.GetComponent<MeshRenderer>();
                if (rndFol != null) rndFol.sharedMaterial = GetOrCreateMaterial("Mat_Copa", new Color(0.25f, 0.52f, 0.22f));
            }

            plantObj.transform.position = position;

            var col = GetOrAddComponent<CapsuleCollider>(plantObj);
            col.radius = 0.8f;
            col.height = 4f;
            col.center = new Vector3(0, 2f, 0);

            var target = GetOrAddComponent<PhotographableTarget>(plantObj);
            var speciesData = AssetDatabase.LoadAssetAtPath<SpeciesData>(speciesPath);
            var soTarget = new SerializedObject(target);
            soTarget.FindProperty("speciesData").objectReferenceValue = speciesData;
            soTarget.FindProperty("focalPointOffset").vector3Value = new Vector3(0, 2.5f, 0);
            soTarget.ApplyModifiedProperties();
        }

        private static void SetupUI(PlayerController playerController, GuideNPC guideNPC, ShopNPC shopNPC)
        {
            GameObject canvasObj = GameObject.Find("UI_Canvas");
            if (canvasObj == null)
            {
                canvasObj = new GameObject("UI_Canvas");
                var canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }

            // 1. Visor da Câmera
            SetupViewfinderUI(canvasObj.transform);

            // 2. HUD de Missões
            SetupQuestHUDUI(canvasObj.transform);

            // 3. Diálogos do Guia / Onboarding
            SetupDialogueUI(canvasObj.transform, guideNPC, shopNPC);

            // 4. Painel do Álbum (Tab)
            SetupAlbumUI(canvasObj.transform, playerController);

            // 5. Painel da Loja (E)
            SetupShopUI(canvasObj.transform, playerController);
        }

        private static void SetupDialogueUI(Transform canvas, GuideNPC guideNPC, ShopNPC shopNPC)
        {
            GameObject dialogueRoot = GetOrCreateChild(canvas, "HUD_Dialogo_Guia");

            // Prompt flutuante "[E] Falar com o Biólogo"
            GameObject prompt = GetOrCreateChild(dialogueRoot.transform, "Prompt_Interacao");
            var rectPrompt = GetOrAddComponent<RectTransform>(prompt);
            rectPrompt.anchorMin = new Vector2(0.5f, 0.25f);
            rectPrompt.anchorMax = new Vector2(0.5f, 0.25f);
            rectPrompt.sizeDelta = new Vector2(480, 45);

            var imgPromptBg = GetOrAddComponent<Image>(prompt);
            imgPromptBg.color = new Color(0f, 0f, 0f, 0.65f);

            GameObject txtPrompt = GetOrCreateChild(prompt.transform, "TextoPrompt");
            var tmpPrompt = GetOrAddComponent<TextMeshProUGUI>(txtPrompt);
            tmpPrompt.fontSize = 20;
            tmpPrompt.fontStyle = FontStyles.Bold;
            tmpPrompt.alignment = TextAlignmentOptions.Center;
            tmpPrompt.text = "[E] Falar com o Biólogo / Iniciar Expedição";
            SetFullStretch(txtPrompt.GetComponent<RectTransform>());

            // Caixa de diálogo no rodapé
            GameObject panel = GetOrCreateChild(dialogueRoot.transform, "Painel_Conversa");
            var rectPanel = GetOrAddComponent<RectTransform>(panel);
            rectPanel.anchorMin = new Vector2(0.5f, 0);
            rectPanel.anchorMax = new Vector2(0.5f, 0);
            rectPanel.pivot = new Vector2(0.5f, 0);
            rectPanel.anchoredPosition = new Vector2(0, 30);
            rectPanel.sizeDelta = new Vector2(1000, 220);

            var imgPanelBg = GetOrAddComponent<Image>(panel);
            imgPanelBg.color = new Color(0.1f, 0.12f, 0.1f, 0.95f);

            // Nome do Biólogo
            GameObject txtSpeaker = GetOrCreateChild(panel.transform, "NomePalestrante");
            var tmpSpeaker = GetOrAddComponent<TextMeshProUGUI>(txtSpeaker);
            tmpSpeaker.fontSize = 24;
            tmpSpeaker.fontStyle = FontStyles.Bold;
            tmpSpeaker.color = new Color(0.95f, 0.78f, 0.35f); // Dourado
            var rectSpeaker = txtSpeaker.GetComponent<RectTransform>();
            rectSpeaker.anchoredPosition = new Vector2(40, -25);
            rectSpeaker.sizeDelta = new Vector2(400, 35);

            // Texto do Diálogo
            GameObject txtBody = GetOrCreateChild(panel.transform, "CorpoTexto");
            var tmpBody = GetOrAddComponent<TextMeshProUGUI>(txtBody);
            tmpBody.fontSize = 20;
            var rectBody = txtBody.GetComponent<RectTransform>();
            rectBody.anchoredPosition = new Vector2(40, -70);
            rectBody.sizeDelta = new Vector2(920, 90);

            // Botão Avançar / Continuar
            GameObject btnNextObj = GetOrCreateChild(panel.transform, "Btn_Avancar");
            var rectBtnNext = GetOrAddComponent<RectTransform>(btnNextObj);
            rectBtnNext.anchorMin = new Vector2(1, 0);
            rectBtnNext.anchorMax = new Vector2(1, 0);
            rectBtnNext.pivot = new Vector2(1, 0);
            rectBtnNext.anchoredPosition = new Vector2(-40, 20);
            rectBtnNext.sizeDelta = new Vector2(160, 40);

            var btnNext = GetOrAddComponent<Button>(btnNextObj);
            var imgBtnNext = GetOrAddComponent<Image>(btnNextObj);
            imgBtnNext.color = new Color(0.2f, 0.65f, 0.3f);

            GameObject txtBtnNext = GetOrCreateChild(btnNextObj.transform, "Texto");
            var tmpBtnNext = GetOrAddComponent<TextMeshProUGUI>(txtBtnNext);
            tmpBtnNext.text = "Avançar [E] >";
            tmpBtnNext.fontSize = 18;
            tmpBtnNext.alignment = TextAlignmentOptions.Center;
            SetFullStretch(txtBtnNext.GetComponent<RectTransform>());

            btnNext.onClick.RemoveAllListeners();
            if (guideNPC != null)
            {
                btnNext.onClick.AddListener(guideNPC.AdvanceDialogue);
            }

            // Botão Abrir Loja
            GameObject btnShopObj = GetOrCreateChild(panel.transform, "Btn_Loja");
            var rectBtnShop = GetOrAddComponent<RectTransform>(btnShopObj);
            rectBtnShop.anchorMin = new Vector2(1, 0);
            rectBtnShop.anchorMax = new Vector2(1, 0);
            rectBtnShop.pivot = new Vector2(1, 0);
            rectBtnShop.anchoredPosition = new Vector2(-220, 20);
            rectBtnShop.sizeDelta = new Vector2(210, 40);

            var btnShop = GetOrAddComponent<Button>(btnShopObj);
            var imgBtnShop = GetOrAddComponent<Image>(btnShopObj);
            imgBtnShop.color = new Color(0.7f, 0.45f, 0.15f);

            GameObject txtBtnShop = GetOrCreateChild(btnShopObj.transform, "Texto");
            var tmpBtnShop = GetOrAddComponent<TextMeshProUGUI>(txtBtnShop);
            tmpBtnShop.text = "Loja de Câmeras 🛒";
            tmpBtnShop.fontSize = 18;
            tmpBtnShop.alignment = TextAlignmentOptions.Center;
            SetFullStretch(txtBtnShop.GetComponent<RectTransform>());

            btnShop.onClick.RemoveAllListeners();
            if (guideNPC != null)
            {
                btnShop.onClick.AddListener(guideNPC.OpenShopFromGuide);
            }

            panel.SetActive(false);
            prompt.SetActive(false);

            // Serializa no GuideNPC
            if (guideNPC != null)
            {
                var soGuide = new SerializedObject(guideNPC);
                soGuide.FindProperty("promptUI").objectReferenceValue = prompt;
                soGuide.FindProperty("dialoguePanel").objectReferenceValue = panel;
                soGuide.FindProperty("dialogueSpeakerText").objectReferenceValue = tmpSpeaker;
                soGuide.FindProperty("dialogueBodyText").objectReferenceValue = tmpBody;
                soGuide.ApplyModifiedProperties();
            }

            // Serializa no ShopNPC
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
            var viewfinderUI = GetOrAddComponent<PhotoViewfinderUI>(viewfinderRoot);

            GameObject container = GetOrCreateChild(viewfinderRoot.transform, "Viewfinder_Container");
            var rectContainer = GetOrAddComponent<RectTransform>(container);
            SetFullStretch(rectContainer);

            // Retículo Central
            GameObject crosshair = GetOrCreateChild(container.transform, "Crosshair");
            var imgCrosshair = GetOrAddComponent<Image>(crosshair);
            imgCrosshair.color = new Color(1f, 1f, 1f, 0.6f);
            var rectCross = crosshair.GetComponent<RectTransform>();
            rectCross.sizeDelta = new Vector2(8f, 8f);

            // Caixa de Foco Dinâmico
            GameObject focusBox = GetOrCreateChild(container.transform, "FocusFrame");
            var imgFocus = GetOrAddComponent<Image>(focusBox);
            imgFocus.color = new Color(0.2f, 0.9f, 0.3f, 0.7f);
            var rectFocus = focusBox.GetComponent<RectTransform>();
            rectFocus.sizeDelta = new Vector2(90f, 90f);

            // Texto do Alvo
            GameObject txtTarget = GetOrCreateChild(container.transform, "TargetNameText");
            var tmpTarget = GetOrAddComponent<TextMeshProUGUI>(txtTarget);
            tmpTarget.fontSize = 24;
            tmpTarget.alignment = TextAlignmentOptions.Center;
            tmpTarget.text = "Espécie Identificada";
            var rectTxtTarget = txtTarget.GetComponent<RectTransform>();
            rectTxtTarget.anchoredPosition = new Vector2(0, -70);
            rectTxtTarget.sizeDelta = new Vector2(500, 40);

            // Zoom Text
            GameObject txtZoom = GetOrCreateChild(container.transform, "ZoomText");
            var tmpZoom = GetOrAddComponent<TextMeshProUGUI>(txtZoom);
            tmpZoom.fontSize = 22;
            tmpZoom.alignment = TextAlignmentOptions.BottomRight;
            tmpZoom.text = "1.0x";
            var rectTxtZoom = txtZoom.GetComponent<RectTransform>();
            rectTxtZoom.anchorMin = new Vector2(1, 0);
            rectTxtZoom.anchorMax = new Vector2(1, 0);
            rectTxtZoom.anchoredPosition = new Vector2(-60, 60);

            // Flash Overlay
            GameObject flashObj = GetOrCreateChild(viewfinderRoot.transform, "FlashOverlay");
            var flashImg = GetOrAddComponent<Image>(flashObj);
            flashImg.color = Color.white;
            var flashGroup = GetOrAddComponent<CanvasGroup>(flashObj);
            flashGroup.alpha = 0f;
            SetFullStretch(flashObj.GetComponent<RectTransform>());

            // Painel de Resultado
            GameObject resultObj = GetOrCreateChild(viewfinderRoot.transform, "Resultado_Foto");
            var resultImg = GetOrAddComponent<Image>(resultObj);
            resultImg.color = new Color(0.1f, 0.1f, 0.1f, 0.88f);
            var rectResult = resultObj.GetComponent<RectTransform>();
            rectResult.anchorMin = new Vector2(0.5f, 0.5f);
            rectResult.anchorMax = new Vector2(0.5f, 0.5f);
            rectResult.sizeDelta = new Vector2(520, 380);

            GameObject thumbObj = GetOrCreateChild(resultObj.transform, "Miniatura");
            var rawThumb = GetOrAddComponent<RawImage>(thumbObj);
            var rectThumb = thumbObj.GetComponent<RectTransform>();
            rectThumb.anchoredPosition = new Vector2(0, 70);
            rectThumb.sizeDelta = new Vector2(320, 180);

            GameObject txtResultTitle = GetOrCreateChild(resultObj.transform, "TituloResultado");
            var tmpResultTitle = GetOrAddComponent<TextMeshProUGUI>(txtResultTitle);
            tmpResultTitle.fontSize = 26;
            tmpResultTitle.fontStyle = FontStyles.Bold;
            tmpResultTitle.alignment = TextAlignmentOptions.Center;
            txtResultTitle.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -45);

            GameObject txtResultScore = GetOrCreateChild(resultObj.transform, "Pontuacao");
            var tmpResultScore = GetOrAddComponent<TextMeshProUGUI>(txtResultScore);
            tmpResultScore.fontSize = 22;
            tmpResultScore.alignment = TextAlignmentOptions.Center;
            txtResultScore.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -80);

            GameObject txtResultStars = GetOrCreateChild(resultObj.transform, "Estrelas");
            var tmpResultStars = GetOrAddComponent<TextMeshProUGUI>(txtResultStars);
            tmpResultStars.fontSize = 28;
            tmpResultStars.color = Color.yellow;
            tmpResultStars.alignment = TextAlignmentOptions.Center;
            txtResultStars.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -115);

            GameObject txtResultFeed = GetOrCreateChild(resultObj.transform, "Feedback");
            var tmpResultFeed = GetOrAddComponent<TextMeshProUGUI>(txtResultFeed);
            tmpResultFeed.fontSize = 18;
            tmpResultFeed.alignment = TextAlignmentOptions.Center;
            txtResultFeed.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -150);

            resultObj.SetActive(false);

            var soVF = new SerializedObject(viewfinderUI);
            soVF.FindProperty("viewfinderContainer").objectReferenceValue = container;
            soVF.FindProperty("focusFrame").objectReferenceValue = rectFocus;
            soVF.FindProperty("targetNameText").objectReferenceValue = tmpTarget;
            soVF.FindProperty("zoomLevelText").objectReferenceValue = tmpZoom;
            soVF.FindProperty("crosshairImage").objectReferenceValue = imgCrosshair;
            soVF.FindProperty("flashCanvasGroup").objectReferenceValue = flashGroup;
            soVF.FindProperty("resultPanel").objectReferenceValue = resultObj;
            soVF.FindProperty("photoThumbnail").objectReferenceValue = rawThumb;
            soVF.FindProperty("speciesNameResultText").objectReferenceValue = tmpResultTitle;
            soVF.FindProperty("scoreText").objectReferenceValue = tmpResultScore;
            soVF.FindProperty("starsText").objectReferenceValue = tmpResultStars;
            soVF.FindProperty("feedbackText").objectReferenceValue = tmpResultFeed;
            soVF.ApplyModifiedProperties();
        }

        private static void SetupQuestHUDUI(Transform canvas)
        {
            GameObject hudRoot = GetOrCreateChild(canvas, "HUD_Missoes");
            var questHUD = GetOrAddComponent<QuestHUDUI>(hudRoot);

            GameObject questBox = GetOrCreateChild(hudRoot.transform, "PainelMissoes");
            var rectBox = GetOrAddComponent<RectTransform>(questBox);
            rectBox.anchorMin = new Vector2(0, 1);
            rectBox.anchorMax = new Vector2(0, 1);
            rectBox.pivot = new Vector2(0, 1);
            rectBox.anchoredPosition = new Vector2(25, -25);
            rectBox.sizeDelta = new Vector2(360, 220);

            var imgBg = GetOrAddComponent<Image>(questBox);
            imgBg.color = new Color(0f, 0f, 0f, 0.45f);

            GameObject txtList = GetOrCreateChild(questBox.transform, "ListaTexto");
            var tmpList = GetOrAddComponent<TextMeshProUGUI>(txtList);
            tmpList.fontSize = 17;
            SetFullStretch(txtList.GetComponent<RectTransform>());

            GameObject banner = GetOrCreateChild(hudRoot.transform, "BannerConclusao");
            var rectBanner = GetOrAddComponent<RectTransform>(banner);
            rectBanner.anchorMin = new Vector2(0.5f, 1);
            rectBanner.anchorMax = new Vector2(0.5f, 1);
            rectBanner.pivot = new Vector2(0.5f, 1);
            rectBanner.anchoredPosition = new Vector2(0, -30);
            rectBanner.sizeDelta = new Vector2(600, 90);

            var bannerImg = GetOrAddComponent<Image>(banner);
            bannerImg.color = new Color(0.1f, 0.6f, 0.2f, 0.92f);

            GameObject txtBannerTitle = GetOrCreateChild(banner.transform, "Titulo");
            var tmpBannerTitle = GetOrAddComponent<TextMeshProUGUI>(txtBannerTitle);
            tmpBannerTitle.fontSize = 24;
            tmpBannerTitle.fontStyle = FontStyles.Bold;
            tmpBannerTitle.alignment = TextAlignmentOptions.Center;
            txtBannerTitle.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -20);

            GameObject txtBannerReward = GetOrCreateChild(banner.transform, "Recompensa");
            var tmpBannerReward = GetOrAddComponent<TextMeshProUGUI>(txtBannerReward);
            tmpBannerReward.fontSize = 18;
            tmpBannerReward.alignment = TextAlignmentOptions.Center;
            txtBannerReward.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -50);

            banner.SetActive(false);

            var soHUD = new SerializedObject(questHUD);
            soHUD.FindProperty("questsContainer").objectReferenceValue = questBox;
            soHUD.FindProperty("questListText").objectReferenceValue = tmpList;
            soHUD.FindProperty("completionBanner").objectReferenceValue = banner;
            soHUD.FindProperty("completionTitleText").objectReferenceValue = tmpBannerTitle;
            soHUD.FindProperty("completionRewardText").objectReferenceValue = tmpBannerReward;
            soHUD.ApplyModifiedProperties();
        }

        private static void SetupAlbumUI(Transform canvas, PlayerController playerController)
        {
            GameObject albumRoot = GetOrCreateChild(canvas, "Modal_Album");
            var albumUI = GetOrAddComponent<AlbumUI>(albumRoot);

            var rectRoot = GetOrAddComponent<RectTransform>(albumRoot);
            rectRoot.sizeDelta = new Vector2(1100, 720);
            var imgBg = GetOrAddComponent<Image>(albumRoot);
            imgBg.color = new Color(0.08f, 0.12f, 0.08f, 0.95f);

            GameObject headerObj = GetOrCreateChild(albumRoot.transform, "HeaderProgresso");
            var tmpProgress = GetOrAddComponent<TextMeshProUGUI>(headerObj);
            tmpProgress.fontSize = 28;
            tmpProgress.text = "ÁLBUM E COMPÊNDIO DO CERRADO (TAB para fechar)";
            headerObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 320);

            GameObject grid = GetOrCreateChild(albumRoot.transform, "GridContainer");
            var gridRect = GetOrAddComponent<RectTransform>(grid);
            gridRect.anchoredPosition = new Vector2(-220, -30);
            gridRect.sizeDelta = new Vector2(600, 560);
            var glg = GetOrAddComponent<GridLayoutGroup>(grid);
            glg.cellSize = new Vector2(170, 160);
            glg.spacing = new Vector2(15, 15);

            GameObject slotTemplate = GetOrCreateChild(albumRoot.transform, "Template_Slot");
            GetOrAddComponent<AlbumSlotUI>(slotTemplate);
            GetOrAddComponent<Button>(slotTemplate);
            var slotImg = GetOrAddComponent<Image>(slotTemplate);
            slotImg.color = new Color(0.2f, 0.25f, 0.2f, 0.8f);
            slotTemplate.SetActive(false);

            GameObject details = GetOrCreateChild(albumRoot.transform, "PainelDetalhes");
            var detRect = GetOrAddComponent<RectTransform>(details);
            detRect.anchoredPosition = new Vector2(340, -30);
            detRect.sizeDelta = new Vector2(360, 560);

            GameObject txtDetName = GetOrCreateChild(details.transform, "NomeEspecie");
            var tmpDetName = GetOrAddComponent<TextMeshProUGUI>(txtDetName);
            tmpDetName.fontSize = 24;
            tmpDetName.fontStyle = FontStyles.Bold;
            txtDetName.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 240);

            GameObject txtDetEdu = GetOrCreateChild(details.transform, "Educativo");
            var tmpDetEdu = GetOrAddComponent<TextMeshProUGUI>(txtDetEdu);
            tmpDetEdu.fontSize = 17;
            txtDetEdu.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 100);
            txtDetEdu.GetComponent<RectTransform>().sizeDelta = new Vector2(340, 200);

            albumRoot.SetActive(false);

            var soAlbumUI = new SerializedObject(albumUI);
            soAlbumUI.FindProperty("albumPanel").objectReferenceValue = albumRoot;
            soAlbumUI.FindProperty("playerController").objectReferenceValue = playerController;
            soAlbumUI.FindProperty("progressPercentageText").objectReferenceValue = tmpProgress;
            soAlbumUI.FindProperty("gridContainer").objectReferenceValue = grid.transform;
            soAlbumUI.FindProperty("slotPrefab").objectReferenceValue = slotTemplate;
            soAlbumUI.FindProperty("detailsPanel").objectReferenceValue = details;
            soAlbumUI.FindProperty("detailCommonName").objectReferenceValue = tmpDetName;
            soAlbumUI.FindProperty("detailEducational").objectReferenceValue = tmpDetEdu;
            soAlbumUI.ApplyModifiedProperties();
        }

        private static void SetupShopUI(Transform canvas, PlayerController playerController)
        {
            GameObject shopRoot = GetOrCreateChild(canvas, "Modal_Loja");
            var shopUI = GetOrAddComponent<ShopUI>(shopRoot);

            var rectRoot = GetOrAddComponent<RectTransform>(shopRoot);
            rectRoot.sizeDelta = new Vector2(900, 650);
            var imgBg = GetOrAddComponent<Image>(shopRoot);
            imgBg.color = new Color(0.12f, 0.1f, 0.08f, 0.95f);

            GameObject txtCoins = GetOrCreateChild(shopRoot.transform, "MoedasTexto");
            var tmpCoins = GetOrAddComponent<TextMeshProUGUI>(txtCoins);
            tmpCoins.fontSize = 26;
            tmpCoins.text = "Suas Moedas: 0 (ESC para fechar)";
            txtCoins.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 280);

            GameObject container = GetOrCreateChild(shopRoot.transform, "ItensContainer");
            var rectCont = GetOrAddComponent<RectTransform>(container);
            rectCont.sizeDelta = new Vector2(800, 480);
            var vlg = GetOrAddComponent<VerticalLayoutGroup>(container);
            vlg.spacing = 12;

            GameObject slotItem = GetOrCreateChild(shopRoot.transform, "Template_ItemLoja");
            GetOrAddComponent<ShopItemSlotUI>(slotItem);
            GetOrAddComponent<Button>(slotItem);
            var imgItem = GetOrAddComponent<Image>(slotItem);
            imgItem.color = new Color(0.2f, 0.18f, 0.15f, 0.8f);
            slotItem.GetComponent<RectTransform>().sizeDelta = new Vector2(780, 80);
            slotItem.SetActive(false);

            shopRoot.SetActive(false);

            var soShop = new SerializedObject(shopUI);
            soShop.FindProperty("shopPanel").objectReferenceValue = shopRoot;
            soShop.FindProperty("playerController").objectReferenceValue = playerController;
            soShop.FindProperty("playerCoinsText").objectReferenceValue = tmpCoins;
            soShop.FindProperty("itemsContainer").objectReferenceValue = container.transform;
            soShop.FindProperty("itemSlotPrefab").objectReferenceValue = slotItem;
            soShop.ApplyModifiedProperties();
        }

        private static GameObject GetOrCreateChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null) return child.gameObject;

            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
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
