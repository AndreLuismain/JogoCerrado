# EICerrado — Jogo Educativo 3D de Exploração e Fotografia Ambiental

**EICerrado** é um jogo 3D educativo de exploração em mundo aberto e simulação fotográfica desenvolvido na engine **Unity (Universal Render Pipeline — URP)** em **C#**, voltado à conscientização ambiental sobre a fauna e a flora do bioma **Cerrado brasileiro**. Desenvolvido no contexto acadêmico da **Universidade de São Paulo (USP)** para estudantes de 12 a 18 anos, o projeto combina mecânicas de *collection-based gameplay*, Inteligência Artificial comportamental e catalogação científica.

---

## Índice de Navegação dos Módulos de Código (`Assets/Codigos`)

Documentação detalhada dos scripts disponível em **[`Assets/Codigos/README.md`](./Assets/Codigos/README.md)**.

| Módulo / Diretório | Namespace | Resumo Técnico e Conceitos de Engenharia |
|---|---|---|
| **[`Assets/Codigos/Photography/`](./Assets/Codigos/Photography/)** | `Cerrado.Photography` | Sistema de câmera com zoom óptico (FOV dinâmico), *Raycasting* anti-oclusão, avaliação matemática de enquadramento/distância e captura em `RenderTexture`/`Texture2D`. |
| **[`Assets/Codigos/AI/`](./Assets/Codigos/AI/)** | `Cerrado.AI` | Máquina de Estados Finitos (FSM: `Idle`, `Wander`, `Alert`, `Flee`) integrada ao `NavMeshAgent`, com sensibilidade furtiva e perfis de temperamento (`Docil`, `Cauteloso`, `Arisco`). |
| **[`Assets/Codigos/Album/`](./Assets/Codigos/Album/)** | `Cerrado.Album` | Catálogo enciclopédico reativo que registra a melhor fotografia de cada espécie, pontuação e progresso de descoberta do bioma. |
| **[`Assets/Codigos/Data/`](./Assets/Codigos/Data/)** | `Cerrado.Data` | Modelagem *Data-Driven* com `ScriptableObject` (`SpeciesData`) para atributos biológicos, nome científico, raridade e valor pedagógico. |
| **[`Assets/Codigos/Economy/`](./Assets/Codigos/Economy/)** | `Cerrado.Economy` | Sistema de economia e upgrades de equipamento (lentes teleobjetivas, estabilizadores de foco e botas furtivas). |
| **[`Assets/Codigos/Quests/`](./Assets/Codigos/Quests/)** | `Cerrado.Quests` | Motor de missões orientado a eventos (`OnPhotoTaken`) que guia a investigação ecológica do jogador. |
| **[`Assets/Codigos/Persistence/`](./Assets/Codigos/Persistence/)** | `Cerrado.Persistence` | Persistência local em JSON (`cerrado_save.json`) e exportação das fotos capturadas pelo jogador em arquivos `.jpg` no disco (`Application.persistentDataPath`). |
| **[`Assets/Codigos/Environment/`](./Assets/Codigos/Environment/)** | `Cerrado.Environment` | Ciclo dinâmico de dia e noite (`DayNightCycle`), atmosfera do Cerrado e gerenciamento de vegetação. |
| **[`Assets/Codigos/Player/`](./Assets/Codigos/Player/)** | `Cerrado.Player` | Controlador de movimentação 3D, mecânica de furtividade (redução de ruído/detecção pela fauna) e integração com o Unity Input System. |
| **[`Assets/Codigos/Interaction/`](./Assets/Codigos/Interaction/)** | `Cerrado.Interaction` | Sistema de interação com NPCs (Guia Ambiental e Loja de Equipamentos). |
| **[`Assets/Codigos/Exploration/`](./Assets/Codigos/Exploration/)** | `Cerrado.Exploration` | Barreiras de progressão (`AreaBarrier`) desbloqueadas conforme o avanço no Álbum de Espécies. |
| **[`Assets/Codigos/UI/`](./Assets/Codigos/UI/)** | `Cerrado.UI` | HUD do visor fotográfico, interface do Álbum, Loja e rastreador de missões desacoplados via *Observer Pattern* (`event Action`). |
| **[`Assets/Jogo/Menu/scripts/`](./Assets/Jogo/Menu/scripts/)** | Global / `Cerrado` | Controlador do Menu Principal (`menu_principal.cs` e `MenuPrincipal.cs`), gerenciamento de painéis de Opções, Extras/Créditos e transição de cenas (`SceneManager`) para a expedição principal. |
| **[`Assets/StarterAssets/`](./Assets/StarterAssets/)** | `StarterAssets` | Controladores base em terceira pessoa (`ThirdPersonController`), integração com `InputSystem` (`StarterAssetsInputs`) e prefabs utilitários de movimentação e câmera. |
| **[`Assets/Codigos/Editor/`](./Assets/Codigos/Editor/)** | `Cerrado.Editor` | Ferramentas customizadas de automação para o Unity Editor (geração e configuração de cenas, missões e itens de loja). |

---

## Estrutura de Cenas e Recursos do Projeto (`Assets/`)

| Diretório / Recurso | Descrição Técnica |
|---|---|
| **[`Assets/Jogo/Menu/`](./Assets/Jogo/Menu/)** | Cena de abertura (`menu_cena.unity`) intitulada *"Pra lá do Cerrado"*, equipada com painel principal, menu de configurações de volume, tela de créditos/extras, logos institucionais (`logo_eic.png`, `logousppbsite.png`), *skybox* customizado e fontes tipográficas (`Rockwell`, `Times New Roman` e `Playfair Display SDF`). |
| **[`Assets/Scenes/SampleScene.unity`](./Assets/Scenes/SampleScene.unity)** | Cena principal de gameplay no bioma Cerrado, contendo terreno estilizado, vegetação nativa com recorte alfa (*Alpha Clipping*), névoa volumétrica rasteira (`CerradoAtmosphere`), fauna com IA e rastro de poeira (`AnimalDustTrail`), pontos de interesse (`SceneAnnotation`), NPC Biólogo Guia e interface completa. |
| **[`Assets/Jogo/Cutscene Inicial/`](./Assets/Jogo/Cutscene Inicial/)** & **[`Jogo Inicio/`](./Assets/Jogo/Jogo%20Inicio/)** | Cenas complementares de introdução e transição configuradas no `EditorBuildSettings.asset`. |
| **[`Assets/Dados/`](./Assets/Dados/)** | *ScriptableObjects* de espécies (`Angico`, `Aroeira`, `Jatoba`, `Ema`, `LoboGuara`, `TamanduaBandeira`), itens da loja (`LenteTeleobjetiva`, `LenteSuperTele`, `EstabilizadorFoco`, `SensorNoturno`, `MochilaExpandida`), missões pedagógicas e materiais URP do Cerrado. |
| **[`Assets/TerrainSampleAssets/`](./Assets/TerrainSampleAssets/)** | Biblioteca de *assets* ambientais para esculpir e povoar o bioma: camadas de terreno (`TerrainLayers` e texturas `.tif`), pincéis de estampagem topográfica (`TerrainBrushes` e *heightmaps*), *Subgraphs* de *ShaderGraph* (vento, folhagem e água) e prefabs de rochas, arbustos, árvores e gramíneas convertidos para **URP Lit**. |
| **[`Assets/TextMesh Pro/`](./Assets/TextMesh%20Pro/)** | Recursos essenciais do *TextMeshPro* (`TMP Settings.asset`), fontes SDF dinâmicas e estáticas (`LiberationSans SDF`, `Inter`, `PlayfairDisplay`), *shaders* de texto e pacotes de ícones/sprites. |

---

## Arquitetura de Software e Padrões de Projeto

```mermaid
flowchart LR
    subgraph INPUT ["Jogador & Câmera"]
        PC["PlayerController<br/>Movimentação & Furtividade"]
        CAM["PhotoCameraSystem<br/>Zoom FOV & Raycast"]
        EVAL["PhotoEvaluator<br/>Cálculo de Score e Foco"]
    end

    subgraph WORLD ["Ecossistema Cerrado"]
        AI["AnimalAI (FSM + NavMesh)<br/>Idle / Wander / Alert / Flee"]
        SO["SpeciesData (ScriptableObject)<br/>Fauna & Flora Nativa"]
    end

    subgraph CORE ["Sistemas de Progressão"]
        ALB["AlbumManager<br/>Catálogo & Descobertas"]
        QST["QuestManager<br/>Missões Pedagógicas"]
        ECO["EquipmentManager<br/>Loja & Upgrades"]
        SAVE["SaveSystem<br/>JSON + Fotos JPG"]
    end

    PC --> CAM
    CAM --> EVAL
    AI --- SO
    EVAL -->|Evento OnPhotoTaken| ALB
    EVAL -->|Evento OnPhotoTaken| QST
    ALB --> SAVE
    QST --> ECO
    ECO -->|Upgrade de Lente/Furtividade| CAM
    ECO -->|Upgrade de Furtividade| PC
```

1. **Observer Pattern (Arquitetura Orientada a Eventos):**
   * O sistema fotográfico (`PhotoCameraSystem`) publica eventos estáticos (`OnPhotoTaken`, `OnAimStateChanged`, `OnTargetFocusChanged`). O `AlbumManager`, o `QuestManager` e as classes de `UI` reagem às capturas sem acoplamento direto entre os componentes.
2. **Data-Driven Design com `ScriptableObjects`:**
   * Novas espécies da fauna (Capivara, Tamanduá-bandeira, Tamanduá-mirim, Lobo-guará) e da flora (Ipê-amarelo, Sucupira, Aroeira), bem como missões (`QuestData`) e itens de loja (`ShopItemData`), são criados como *assets* independentes no Inspector, evitando alterações em código para expansão de conteúdo.
3. **Máquina de Estados Finitos (FSM) na IA da Fauna:**
   * Cada animal avalia continuamente a distância até o jogador e se o jogador está em modo furtivo (`IsCrouching`), alternando entre patrulha orgânica (`Wander`), estado de atenção (`Alert`) e evasão em direção oposta (`Flee`) sobre a malha de navegação (`NavMesh`).

---

## Como Executar o Projeto no Unity Editor

### Pré-requisitos
* **Unity Hub** instalado.
* **Unity Editor 6000.0+ (Unity 6 LTS)** ou **2022.3 LTS+** com suporte a **Universal Render Pipeline (URP)**.

### Passo a Passo
1. Clone o repositório:
   ```powershell
   git clone https://github.com/AndreLuismain/JogoCerrado.git
   ```
2. Abra o **Unity Hub**, clique em **Add -> Add project from disk** e selecione a pasta `JogoCerrado`.
3. No Unity Editor, você pode iniciar o jogo de duas formas:
   * **Via Menu Principal:** Abra `Assets/Jogo/Menu/menu_cena.unity`, pressione **Play** e clique em **Jogar** para carregar a expedição no Cerrado (`SampleScene`), ou navegue pelas telas de **Configurar** e **Extras**.
   * **Direto na Cena Principal:** Abra `Assets/Scenes/SampleScene.unity` (ou utilize o menu de automação customizado na barra superior `Cerrado -> 4. Montar Cena de Teste Completa (SampleScene)` / `5. Aplicar Novos Efeitos e Marcadores no Cenário Atual`).
4. Pressione **Play** no Editor:
   * **Movimentação:** `W`, `A`, `S`, `D` | **Correr:** `Shift` | **Agachar (Furtivo):** `Ctrl` ou `C`
   * **Modo Fotografia:** Botão Direito do Mouse (`RMB`) para mirar | `Scroll` para Zoom Óptico | Botão Esquerdo (`LMB`) para fotografar
   * **Álbum Enciclopédico:** Tecla `Tab` ou `J`
   * **Interagir com NPCs (Guia / Loja):** Tecla `E`

---

## Autoria e Responsabilidades Técnicas

* **André Luís da Silva Souza** ([@AndreLuismain](https://github.com/AndreLuismain) — Sistemas de Informação, ICMC-USP): Arquitetura de software, programação de todos os sistemas em C# (IA, fotografia, álbum, missões, economia, persistência e ferramentas de Editor) e modelagem 3D base no Blender.
