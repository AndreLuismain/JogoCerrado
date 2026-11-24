# EICerrado — Jogo Educativo 3D de Exploração e Fotografia Ambiental (Beta 1.0)

**EICerrado** (*Pra lá do Cerrado — Expedição Fotográfica*) é um jogo educativo 3D em 1ª pessoa de exploração em mundo aberto e simulação fotográfica desenvolvido no **Unity 6 (Universal Render Pipeline — URP)** em **C#**, voltado à conscientização ambiental sobre a biodiversidade do **Cerrado brasileiro**. Desenvolvido no contexto acadêmico da **Universidade de São Paulo (USP)**, o projeto combina catalogação científica, IA comportamental de fauna silvestre e renderização PBR otimizada.

---

## Destaques da Versão Beta (`v1.0.0-beta`)

* **38 Espécies Nativas Catalogáveis com Ícones (18 Fauna + 20 Flora):**
  * **Fauna (18):** Tamanduá-bandeira, Tamanduá-mirim, Lobo-guará, Onça-parda (Suçuarana animada com 53 *BlendShapes*), Ema, Seriema, Veado-campeiro, Jacaré-do-papo-amarelo, Tatu-peba, Tapiti (Coelho-do-mato), Macaco-prego, Teiú, Carcará, Coruja-buraqueira, Galito, Rato-de-espinho, Jiboia e Cascavel.
  * **Flora (20):** Ipê-amarelo, Pequizeiro, Jatobá, Barbatimão, Angico, Aroeira, Araticum, Cajuzinho-do-campo, Canela-de-ema, Capim-flechinha, Catuaba, Cinzeiro, Copaíba, Gravatá, Indaiá, Lobeira, Pau-santo, Sucupira, Sempre-viva Chuveirinho e Árvore Tortuosa do Cerrado.
* **IA Naturalista + `FootPlanter` + Ciclo de Borda:**
  * Máquina de estados (`Idle`, `Wander`, `Alert`, `Flee`) sobre `NavMesh` estendida até o cinturão florestal (`126x120m`), alinhamento suave de patas e inclinação ao relevo (`FootPlanter.cs`) e ciclo contínuo de travessia da cerca com reentrada natural pelo lado oposto da mata.
  * **Biólogo Guia (`Prof. Silva`):** NPC Humanoid animado (`CharacterIdle` / conversa) que olha suavemente para o explorador, orienta as 6 missões ecológicas e gerencia a loja de equipamentos fotográficos.
* **Renderização URP Customizada & 4 Perfis Gráficos (`Baixa`, `Média`, `Alta`, `Muito Alta`):**
  * **Shaders HLSL URP dedicados:** `CerradoTerrainBlendURP.shader` (transição orgânica *World-Space FBM Noise* entre terra seca e grama + mascaramento físico com tufos, arbustos, pedras e palhada) e `CerradoFoliageWindURP.shader` (translucidez solar *subsurface*, dupla face, *Alpha Clipping* BC7/Kaiser e vento orgânico em espaço de mundo).
  * **Perfil Showcase (`Muito Alta` — RTX 5070):** *Render Scale* `1.25x`, `MSAA 4x`, Sombras `4096` *Soft High* (`250m`, 4 cascatas), **SSAO em alta qualidade** (`DepthNormals`), **Depth of Field (Bokeh)** dinâmico na mira fotográfica, `ReflectionProbe` + `LightProbeGroup`, *Bloom*, *Tonemapping ACES* e *Vignette* (*Motion Blur* desativado).
* **Tipografia Acessível (`TextMeshPro` SDF PT-BR Completo) & Onboarding:**
  * Hierarquia tipográfica com **Rubik / Fredoka** para títulos, botões e cabeçalhos e **Lexend** como fonte padrão de leitura científica e HUD, com suporte integral à acentuação em português.
  * **Configurações Persistentes (`GameSettingsManager`):** seletor de qualidade gráfica em tempo real, sliders independentes de *Volume da Música* e *Ambiente / Efeitos* e campo numérico digitável de *Sensibilidade do Mouse* (`0.1` a `10.0`, com redução proporcional durante o zoom óptico).
  * **Tutorial Interativo na Primeira Entrada (`TutorialOverlayUI`):** guia rápido em 3 passos ao iniciar a expedição (reabrível a qualquer momento no menu de Opções).

---

## Índice dos Módulos de Código (`Assets/Codigos`)

Documentação complementar dos scripts em **[`Assets/Codigos/README.md`](./Assets/Codigos/README.md)**.

| Módulo / Diretório | Namespace | Resumo Técnico |
|---|---|---|
| **[`Assets/Codigos/Photography/`](./Assets/Codigos/Photography/)** | `Cerrado.Photography` | Câmera fotográfica com zoom óptico, detecção por *Raycast*, cálculo de pontuação/estrelas e captura em `Texture2D`. |
| **[`Assets/Codigos/AI/`](./Assets/Codigos/AI/)** | `Cerrado.AI` | `AnimalAI` (FSM + ciclo de borda), `AnimalSpawner`, `FootPlanter` (alinhamento ao terreno) e `OncaBlendShapeController` (53 *BlendShapes*). |
| **[`Assets/Codigos/Album/`](./Assets/Codigos/Album/)** | `Cerrado.Album` | Caderno de campo científico com filtros (`Todas`, `Fauna`, `Flora`), busca de espécies catalogadas e registro da melhor captura. |
| **[`Assets/Codigos/Settings/`](./Assets/Codigos/Settings/)** | `Cerrado.Settings` | `GameSettingsManager` persistente (`PlayerPrefs`) sincronizando 4 perfis URP, DoF na mira, volumes separados e sensibilidade. |
| **[`Assets/Codigos/Audio/`](./Assets/Codigos/Audio/)** | `Cerrado.Audio` | `CerradoAudioManager` com trilha regional (*Sons do Cerrado*), passos dinâmicos na terra e sons de interface em canais independentes. |
| **[`Assets/Codigos/UI/`](./Assets/Codigos/UI/)** | `Cerrado.UI` | Visor fotográfico clássico branco (`PhotoViewfinderUI`), `AlbumUI`, `ShopUI`, `QuestHUDUI`, `PauseMenuUI`, `SettingsPanelUI` e `TutorialOverlayUI`. |
| **[`Assets/Codigos/Data/`](./Assets/Codigos/Data/)** | `Cerrado.Data` | `ScriptableObjects` (`SpeciesData`, `QuestData`, `ShopItemData`) desacoplando dados biológicos e pedagógicos da lógica. |
| **[`Assets/Codigos/Persistence/`](./Assets/Codigos/Persistence/)** | `Cerrado.Persistence` | Salvamento local em JSON (`cerrado_save.json`) e exportação das fotos capturadas em `.jpg` (`Application.persistentDataPath`). |
| **[`Assets/Codigos/Editor/`](./Assets/Codigos/Editor/)** | `Cerrado.EditorTools` | Automação completa de cenas (`CerradoSceneSetupEditor`), migração/validação URP (`CerradoMigrationEditor`), fontes (`CerradoFontSetupEditor`) e build/ZIP (`CerradoBetaBuildEditor`). |

---

## Estrutura Principal de Recursos (`Assets/`)

* **`Assets/Jogo/Menu/menu_cena.unity`** — Cena inicial (índice `0` no Build Settings) com Menu Principal, Configurações completas e Créditos institucionais USP/EIC.
* **`Assets/Scenes/SampleScene.unity`** — Cena principal da Reserva Biológica do Cerrado (índice `1`), com terreno orgânico mesclado, 92 entidades ativas (44 animais + 48 espécimes catalogáveis de flora), ~3.500 elementos de vegetação com *GPU Instancing* e perímetro florestal de 4 camadas.
* **`Assets/Dados/Shaders/`** — Shaders URP HLSL customizados (`CerradoTerrainBlendURP.shader` e `CerradoFoliageWindURP.shader`).
* **`Assets/Fontes/`** — Famílias tipográficas **Rubik**, **Fredoka** e **Lexend** (`.ttf` + licenças `OFL.txt`) e ativos SDF gerados em `Assets/Fontes/SDF/`.
* **`Assets/Settings/`** — Perfis URP dedicados (`URP_Baixa`, `URP_Media`, `URP_Alta`, `URP_MuitoAlta`), *Renderers* com SSAO escalonado e *Volume Profiles*.

---

## Como Executar ou Gerar a Build

### Jogando a Build Windows x64 (Pronta para Uso)
1. Extraia o arquivo compactado **`Builds/EICerrado_Beta_Win64.zip`** (ou `.rar`) em qualquer pasta do computador.
2. Execute **`EICerrado.exe`** com dois cliques.

### Abrindo e Compilando no Unity Editor
1. Instale o **Unity 6 (`6000.6.0f1` ou compatível 6000.x LTS)** com suporte a **Windows Build Support (Mono/IL2CPP)** via Unity Hub.
2. Abra a pasta raiz `JogoCerrado` no Unity Hub.
3. Para validar ou regerar automaticamente a build Windows x64 + pacote `.zip` + instalador Inno Setup, utilize o menu superior:
   * **`Cerrado -> 7. Gerar Build Beta Windows x64 + ZIP`**
   * **`Cerrado -> 9. Validar Migração e Cenas (Diagnóstico)`**

### Controles Principais
* **Caminhar / Correr / Agachar (Furtivo):** `W, A, S, D` | `Shift` | `Ctrl`
* **Câmera Fotográfica:** Segurar `Botão Direito (RMB)` para mirar | `Scroll` para Zoom | `Botão Esquerdo (LMB)` para fotografar
* **Álbum do Cerrado:** `TAB` ou `J`
* **Interagir com o Biólogo Guia / Loja:** `E`
* **Pausa / Configurações:** `ESC` ou `P`

---

## Créditos Institucionais

* **Universidade de São Paulo (USP)** — Projeto Educativo de Exploração e Fotografia Ambiental no Cerrado (*EICerrado*).
