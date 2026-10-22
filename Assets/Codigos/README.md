# Arquitetura de Sistemas C# — EICerrado (`Assets/Codigos`)

Este diretório concentra todos os scripts **C#** do projeto **EICerrado**, organizados em namespaces modulares (`Cerrado.*`) que separam Inteligência Artificial, Sistema Óptico/Fotográfico, Álbum Pedagógico, Economia, Missões, Persistência JSON/JPG, Ambiente e Interface de Usuário (UI).

---

## Mapeamento de Subpastas e Módulos

| Subpasta | Namespace | Scripts Principais | Responsabilidade e Conceitos Aplicados |
|---|---|---|---|
| **[`AI/`](./AI/)** | `Cerrado.AI` | `AnimalAI.cs`, `AnimalSpawner.cs`, `AnimalState.cs`, `AnimalDustTrail.cs` | Máquina de Estados Finitos (FSM: `Idle`, `Wander`, `Alert`, `Flee`), navegação autônoma via `NavMeshAgent`, perfis de temperamento (`Docil`, `Cauteloso`, `Arisco`) e detecção espacial do jogador. |
| **[`Photography/`](./Photography/)** | `Cerrado.Photography` | `PhotoCameraSystem.cs`, `PhotoEvaluator.cs`, `CameraCapture.cs`, `PhotographableTarget.cs`, `PhotoResult.cs` | Simulação de câmera com zoom óptico (interpolação de FOV), *Raycasting* para verificação de oclusão, avaliação de enquadramento/distância/foco e renderização de `RenderTexture` para `Texture2D`. |
| **[`Album/`](./Album/)** | `Cerrado.Album` | `AlbumManager.cs`, `AlbumEntry.cs` | Gerenciamento do catálogo de espécies descobertas, cálculo de progresso percentual de catalogação do bioma e eventos de desbloqueio. |
| **[`Data/`](./Data/)** | `Cerrado.Data` | `SpeciesData.cs`, `SpeciesRarity.cs` | Arquitetura orientada a dados com `ScriptableObject` para definir nome científico, curiosidades ecológicas, raridade (`Comum`, `Incomum`, `Raro`, `Lendario`) e recompensa base. |
| **[`Economy/`](./Economy/)** | `Cerrado.Economy` | `EquipmentManager.cs`, `ShopItemData.cs`, `EquipmentType.cs` | Sistema econômico e loja de upgrades (Lentes Teleobjetivas, Botas Furtivas, Estabilizadores de Foco) que modificam dinamicamente atributos do jogador e da câmera. |
| **[`Quests/`](./Quests/)** | `Cerrado.Quests` | `QuestManager.cs`, `QuestData.cs` | Sistema de missões pedagógicas encadeadas baseadas em eventos (`OnPhotoTaken`), com objetivos de captura por espécie, raridade ou pontuação mínima. |
| **[`Persistence/`](./Persistence/)** | `Cerrado.Persistence` | `SaveSystem.cs`, `SaveData.cs` | Serialização em JSON (`cerrado_save.json`) do estado de jogo (moedas, equipamentos, missões, álbum) e gravação das fotografias capturadas em disco no formato `.jpg` (`EncodeToJPG`). |
| **[`Environment/`](./Environment/)** | `Cerrado.Environment` | `DayNightCycle.cs`, `CerradoAtmosphere.cs`, `TerrainTreeToggle.cs`, `SceneAnnotation.cs` | Ciclo dinâmico de dia/noite, iluminação atmosférica do Cerrado e otimização de vegetação no terreno. |
| **[`Exploration/`](./Exploration/)** | `Cerrado.Exploration` | `AreaBarrier.cs` | Desbloqueio progressivo de regiões do mapa condicionado ao nível de catalogação do álbum ou conclusão de missões. |
| **[`Interaction/`](./Interaction/)** | `Cerrado.Interaction` | `GuideNPC.cs`, `ShopNPC.cs` | Interação contextual com NPCs (Guia Ambiental e Comerciante de Equipamentos). |
| **[`Player/`](./Player/)** | `Cerrado.Player` | `PlayerController.cs` | Movimentação em primeira/terceira pessoa, controle de furtividade (agachamento que reduz o raio de detecção da IA dos animais) e integração com o `InputSystem`. |
| **[`UI/`](./UI/)** | `Cerrado.UI` | `PhotoViewfinderUI.cs`, `AlbumUI.cs`, `AlbumSlotUI.cs`, `ShopUI.cs`, `ShopItemSlotUI.cs`, `QuestHUDUI.cs` | Interfaces reativas desacopladas por eventos C# (`event Action<...>`) para o visor da câmera, álbum enciclopédico, loja e HUD de missões. |
| **[`Editor/`](./Editor/)** | `Cerrado.Editor` | `CerradoSetupEditor.cs`, `CerradoSceneSetupEditor.cs`, `CerradoQuestSetupEditor.cs`, `CerradoShopSetupEditor.cs`, `SceneAnnotationEditor.cs` | Ferramentas customizadas do Unity Editor (`EditorWindow` / `CustomEditor`) para automação de setup de cena, geração de *ScriptableObjects* e configuração rápida de missões/loja. |
| **[`MenuPrincipal.cs`](./MenuPrincipal.cs)** & **[`../Jogo/Menu/scripts/menu_principal.cs`](../Jogo/Menu/scripts/menu_principal.cs)** | Global | `MenuPrincipal.cs`, `menu_principal.cs` | Controle de fluxo do Menu Inicial (`menu_cena.unity`), alternância de subpainéis (Opções/Volume e Extras/Créditos) e carregamento da cena principal (`Assets/Scenes/SampleScene.unity`). |
