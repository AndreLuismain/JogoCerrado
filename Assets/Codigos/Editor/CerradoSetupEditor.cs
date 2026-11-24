using UnityEngine;
using UnityEditor;
using System.IO;
using Cerrado.Data;

namespace Cerrado.EditorTools
{
    public static class CerradoSetupEditor
    {
        public static bool SuppressDialogs = false;

        [MenuItem("Cerrado/1. Gerar Dados Iniciais das Espécies (GDD)")]
        public static void GenerateInitialSpecies()
        {
            string folderPath = "Assets/Dados/Especies";
            if (!AssetDatabase.IsValidFolder("Assets/Dados"))
            {
                AssetDatabase.CreateFolder("Assets", "Dados");
            }
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder("Assets/Dados", "Especies");
            }

            // 1. Tamanduá-bandeira
            CreateOrUpdateSpecies(
                folderPath, "TamanduaBandeira",
                id: "tamandua_bandeira",
                commonName: "Tamanduá-bandeira",
                scientificName: "Myrmecophaga tridactyla",
                category: SpeciesCategory.Fauna,
                rarity: SpeciesRarity.Comum,
                activity: ActivityTime.Diurno,
                educational: "Mamífero insetívoro característico do Cerrado. Alimenta-se principalmente de formigas e cupins, usando suas garras fortes e língua comprida. É essencial para o controle biológico de insetos.",
                habitat: "Campos abertos e cerradão.",
                status: "Vulnerável (VU)",
                baseScore: 180, baseCoins: 18,
                minDist: 3.5f, maxDist: 14f, radius: 1.4f,
                iconFileName: "tamandua_bandeira.png"
            );

            // 2. Lobo-guará
            CreateOrUpdateSpecies(
                folderPath, "LoboGuara",
                id: "lobo_guara",
                commonName: "Lobo-guará",
                scientificName: "Chrysocyon brachyurus",
                category: SpeciesCategory.Fauna,
                rarity: SpeciesRarity.Raro,
                activity: ActivityTime.Noturno,
                educational: "Maior canídeo da América do Sul e símbolo do Cerrado. Possui pernas longas para avistar presas sobre o capim alto. Consome o fruto da lobeira, sendo o maior dispersor de sementes da planta.",
                habitat: "Vegetação campestre e savânica.",
                status: "Vulnerável (VU)",
                baseScore: 400, baseCoins: 45,
                minDist: 5f, maxDist: 20f, radius: 1.2f,
                iconFileName: "lobo_guara.png"
            );

            // 3. Ema
            CreateOrUpdateSpecies(
                folderPath, "Ema",
                id: "ema",
                commonName: "Ema",
                scientificName: "Rhea americana",
                category: SpeciesCategory.Fauna,
                rarity: SpeciesRarity.Incomum,
                activity: ActivityTime.Diurno,
                educational: "Maior ave não voadora do continente americano. Corre em altas velocidades pelos campos e tem papel importante na dispersão de frutos nativos.",
                habitat: "Campos limpos e chapadões.",
                status: "Quase Ameaçada (NT)",
                baseScore: 250, baseCoins: 25,
                minDist: 4f, maxDist: 18f, radius: 1.5f,
                iconFileName: "ema.png"
            );

            // 4. Jatobá
            CreateOrUpdateSpecies(
                folderPath, "Jatoba",
                id: "jatoba",
                commonName: "Jatobá-do-cerrado",
                scientificName: "Hymenaea stigonocarpa",
                category: SpeciesCategory.Flora,
                rarity: SpeciesRarity.Comum,
                activity: ActivityTime.Ambos,
                educational: "Árvore marcante com casca grossa e frutos de casca dura riquíssimos em nutrientes. Sua madeira e resina são historicamente valorizadas.",
                habitat: "Cerradão e cerrado típico.",
                status: "Pouco Preocupante (LC)",
                baseScore: 120, baseCoins: 12,
                minDist: 6f, maxDist: 25f, radius: 3.5f,
                iconFileName: "jatoba.png"
            );

            // 5. Angico
            CreateOrUpdateSpecies(
                folderPath, "Angico",
                id: "angico",
                commonName: "Angico",
                scientificName: "Anadenanthera colubrina",
                category: SpeciesCategory.Flora,
                rarity: SpeciesRarity.Comum,
                activity: ActivityTime.Ambos,
                educational: "Árvore pioneira muito resistente a solos secos e queimadas ocasionais. Sua casca é rica em taninos e atrai diversos polinizadores.",
                habitat: "Áreas pedregosas e cerrado rupestre.",
                status: "Pouco Preocupante (LC)",
                baseScore: 120, baseCoins: 12,
                minDist: 5f, maxDist: 22f, radius: 3.0f,
                iconFileName: "angico.png"
            );

            // 6. Aroeira
            CreateOrUpdateSpecies(
                folderPath, "Aroeira",
                id: "aroeira",
                commonName: "Aroeira-do-sertão",
                scientificName: "Myracrodruon urundeuva",
                category: SpeciesCategory.Flora,
                rarity: SpeciesRarity.Comum,
                activity: ActivityTime.Ambos,
                educational: "Espécie nobre com madeira de altíssima durabilidade e grande resistência ao estresse hídrico da estação seca.",
                habitat: "Matas secas e cerrado sensu stricto.",
                status: "Quase Ameaçada (NT)",
                baseScore: 140, baseCoins: 14,
                minDist: 5f, maxDist: 20f, radius: 2.8f,
                iconFileName: "aroeira.png"
            );

            // 7. Jacaré-de-papo-amarelo
            CreateOrUpdateSpecies(
                folderPath, "JacarePapoAmarelo",
                id: "jacare_papo_amarelo",
                commonName: "Jacaré-de-papo-amarelo",
                scientificName: "Caiman latirostris",
                category: SpeciesCategory.Fauna,
                rarity: SpeciesRarity.Incomum,
                activity: ActivityTime.Ambos,
                educational: "Réptil semi-aquático de focinho largo encontrado em veredas, lagoas e rios que cortam o Cerrado. Atua no equilíbrio ecológico controlando populações de peixes e invertebrados.",
                habitat: "Veredas, brejos e margens de rios do Cerrado.",
                status: "Pouco Preocupante (LC)",
                baseScore: 260, baseCoins: 28,
                minDist: 4f, maxDist: 16f, radius: 1.4f,
                iconFileName: "jacare_do_papo_amarelo.png"
            );

            // 8. Onça-parda (Suçuarana)
            CreateOrUpdateSpecies(
                folderPath, "OncaParda",
                id: "onca_parda",
                commonName: "Onça-parda (Suçuarana)",
                scientificName: "Puma concolor",
                category: SpeciesCategory.Fauna,
                rarity: SpeciesRarity.MuitoRaro,
                activity: ActivityTime.Noturno,
                educational: "Segundo maior felino das Américas e predador de topo no Cerrado. Extremamente ágil e furtiva, regula as populações de herbívoros e indica alta qualidade ambiental do ecossistema.",
                habitat: "Chapadas, matas de galeria e cerradão.",
                status: "Vulnerável (VU)",
                baseScore: 550, baseCoins: 65,
                minDist: 6f, maxDist: 24f, radius: 1.3f,
                iconFileName: "onca_parda.png"
            );

            // 9. Tatu-peba
            CreateOrUpdateSpecies(
                folderPath, "TatuPeba",
                id: "tatu_peba",
                commonName: "Tatu-peba",
                scientificName: "Euphractus sexcinctus",
                category: SpeciesCategory.Fauna,
                rarity: SpeciesRarity.Comum,
                activity: ActivityTime.Diurno,
                educational: "Mamífero de carapaça articulada com seis cintas móveis. Suas tocas escavadas no solo do Cerrado servem de abrigo térmico contra queimadas para dezenas de outras espécies.",
                habitat: "Campos sujos e bordas de cerradão.",
                status: "Pouco Preocupante (LC)",
                baseScore: 170, baseCoins: 18,
                minDist: 2.5f, maxDist: 12f, radius: 0.9f,
                iconFileName: "tatu_peba.png"
            );

            // 10. Tapeti (Coelho-do-mato)
            CreateOrUpdateSpecies(
                folderPath, "CoelhoTapeti",
                id: "coelho_tapeti",
                commonName: "Tapeti (Coelho-do-mato)",
                scientificName: "Sylvilagus brasiliensis",
                category: SpeciesCategory.Fauna,
                rarity: SpeciesRarity.Comum,
                activity: ActivityTime.Noturno,
                educational: "Único lagomorfo nativo do Brasil. De hábitos crepusculares e noturnos, camufla-se no capim seco e desempenha papel vital na dispersão de gramíneas e na teia alimentar do Cerrado.",
                habitat: "Campos limpos, campos sujos e bordas de mata.",
                status: "Em Perigo (EN)",
                baseScore: 190, baseCoins: 20,
                minDist: 2.5f, maxDist: 11f, radius: 0.8f,
                iconFileName: "tapiti.png"
            );

            // 11. Sempre-viva Chuveirinho
            CreateOrUpdateSpecies(
                folderPath, "Chuveirinho",
                id: "chuveirinho",
                commonName: "Sempre-viva Chuveirinho",
                scientificName: "Paepalanthus sp.",
                category: SpeciesCategory.Flora,
                rarity: SpeciesRarity.Incomum,
                activity: ActivityTime.Ambos,
                educational: "Planta herbácea emblemática dos campos rupestres do Cerrado. Suas inflorescências esféricas brancas parecem pequenos chuveiros e mantêm a forma mesmo após secas.",
                habitat: "Campos rupestres e chapadas de altitude.",
                status: "Vulnerável (VU)",
                baseScore: 210, baseCoins: 22,
                minDist: 2.0f, maxDist: 12f, radius: 1.2f,
                iconFileName: "chuveirinho.png"
            );

            // 12. Árvore Tortuosa Seca do Cerrado
            CreateOrUpdateSpecies(
                folderPath, "ArvoreSeca",
                id: "arvore_seca_cerrado",
                commonName: "Árvore Tortuosa do Cerrado",
                scientificName: "Qualea grandiflora",
                category: SpeciesCategory.Flora,
                rarity: SpeciesRarity.Comum,
                activity: ActivityTime.Ambos,
                educational: "Símbolo da fisionomia savânica do Cerrado: seus galhos retorcidos e casca cortiçosa espessa (súber) conferem isolamento térmico e proteção às gemas durante queimadas naturais.",
                habitat: "Cerrado típico (sensu stricto) e cerrado ralo.",
                status: "Pouco Preocupante (LC)",
                baseScore: 130, baseCoins: 14,
                minDist: 4f, maxDist: 20f, radius: 2.8f,
                iconFileName: "pausanto.png"
            );

            // 13. Veado-campeiro
            CreateOrUpdateSpecies(
                folderPath, "VeadoCampeiro",
                id: "veado_campeiro",
                commonName: "Veado-campeiro",
                scientificName: "Ozotoceros bezoarticus",
                category: SpeciesCategory.Fauna,
                rarity: SpeciesRarity.Raro,
                activity: ActivityTime.Diurno,
                educational: "Cervídeo ágil adaptado aos campos abertos e veredas do Cerrado. Extremamente veloz ao detectar perigo e excelente indicador da conservação de áreas campestres.",
                habitat: "Campos limpos, campos sujos e chapadões.",
                status: "Vulnerável (VU)",
                baseScore: 360, baseCoins: 38,
                minDist: 5f, maxDist: 20f, radius: 1.3f,
                iconFileName: "veado_campeiro.png"
            );

            // 14. Ipê-amarelo
            CreateOrUpdateSpecies(
                folderPath, "IpeAmarelo",
                id: "ipe_amarelo",
                commonName: "Ipê-amarelo-do-cerrado",
                scientificName: "Handroanthus ochraceus",
                category: SpeciesCategory.Flora,
                rarity: SpeciesRarity.Incomum,
                activity: ActivityTime.Ambos,
                educational: "Árvore icônica que floresce intensamente na estação seca, cobrindo sua copa retorcida de flores douradas que atraem abelhas nativas e beija-flores.",
                habitat: "Cerrado típico e bordas de mata seca.",
                status: "Pouco Preocupante (LC)",
                baseScore: 165, baseCoins: 16,
                minDist: 5f, maxDist: 22f, radius: 3.0f,
                iconFileName: "ipe_amarelo.png"
            );

            // 15. Pequizeiro
            CreateOrUpdateSpecies(
                folderPath, "Pequizeiro",
                id: "pequi",
                commonName: "Pequizeiro",
                scientificName: "Caryocar brasiliense",
                category: SpeciesCategory.Flora,
                rarity: SpeciesRarity.Incomum,
                activity: ActivityTime.Ambos,
                educational: "Árvore símbolo da cultura e gastronomia do Cerrado. Seus frutos amarelos aromáticos alimentam a fauna e sustentam comunidades extrativistas.",
                habitat: "Cerrado sensu stricto e cerradão.",
                status: "Pouco Preocupante (LC)",
                baseScore: 160, baseCoins: 16,
                minDist: 5f, maxDist: 22f, radius: 3.0f,
                iconFileName: "pequi.png"
            );

            // 16. Barbatimão
            CreateOrUpdateSpecies(
                folderPath, "Barbatimao",
                id: "barbatimao",
                commonName: "Barbatimão",
                scientificName: "Stryphnodendron adstringens",
                category: SpeciesCategory.Flora,
                rarity: SpeciesRarity.Incomum,
                activity: ActivityTime.Ambos,
                educational: "Espécie medicinal nativa de casca rugosa muito rica em taninos cicatrizantes, resistente ao fogo e característica das fisionomias abertas do bioma.",
                habitat: "Campos cerrados e cerradões.",
                status: "Pouco Preocupante (LC)",
                baseScore: 135, baseCoins: 14,
                minDist: 4f, maxDist: 18f, radius: 2.6f,
                iconFileName: "barbatimao.png"
            );

            // --- FAUNA COMPLEMENTAR (10 Espécies Adicionais do Bioma Cerrado -> Total 18 Fauna) ---

            // 17. Tamanduá-mirim
            CreateOrUpdateSpecies(
                folderPath, "TamanduaMirim",
                id: "tamandua_mirim",
                commonName: "Tamanduá-mirim",
                scientificName: "Tamandua tetradactyla",
                category: SpeciesCategory.Fauna,
                rarity: SpeciesRarity.Incomum,
                activity: ActivityTime.Ambos,
                educational: "Conhecido como mixila ou tamanduá-de-colete pelo desenho escuro no dorso. É ágil tanto no chão quanto em árvores, caçando formigas e cupins com sua cauda preênsil.",
                habitat: "Cerrado típico, matas de galeria e bordas florestais.",
                status: "Pouco Preocupante (LC)",
                baseScore: 220, baseCoins: 24,
                minDist: 3f, maxDist: 14f, radius: 0.9f,
                iconFileName: "tamandua_mirim.png"
            );

            // 18. Seriema
            CreateOrUpdateSpecies(
                folderPath, "Seriema",
                id: "seriema",
                commonName: "Seriema",
                scientificName: "Cariama cristata",
                category: SpeciesCategory.Fauna,
                rarity: SpeciesRarity.Comum,
                activity: ActivityTime.Diurno,
                educational: "Ave terrestre símbolo do Brasil Central, famosa pelo canto vibrante que ecoa pelos campos e pelo topete característico. Controla populações de serpentes, insetos e pequenos vertebrados.",
                habitat: "Campos abertos e cerrados ralos.",
                status: "Pouco Preocupante (LC)",
                baseScore: 170, baseCoins: 17,
                minDist: 3.5f, maxDist: 16f, radius: 0.9f,
                iconFileName: "seriema.png"
            );

            // 19. Macaco-prego
            CreateOrUpdateSpecies(
                folderPath, "MacacoPrego",
                id: "macaco_prego",
                commonName: "Macaco-prego-do-cerrado",
                scientificName: "Sapajus libidinosus",
                category: SpeciesCategory.Fauna,
                rarity: SpeciesRarity.Incomum,
                activity: ActivityTime.Diurno,
                educational: "Primata extremamente inteligente, famoso por utilizar pedras como ferramentas para quebrar frutos duros como o coco-indaiá e sementes do Cerrado.",
                habitat: "Cerradão, matas secas e bordas de mata de galeria.",
                status: "Quase Ameaçada (NT)",
                baseScore: 260, baseCoins: 28,
                minDist: 3.5f, maxDist: 15f, radius: 0.8f,
                iconFileName: "macaco_prego.png"
            );

            // 20. Teiú
            CreateOrUpdateSpecies(
                folderPath, "Teiu",
                id: "teiu",
                commonName: "Lagarto Teiú",
                scientificName: "Salvator merianae",
                category: SpeciesCategory.Fauna,
                rarity: SpeciesRarity.Comum,
                activity: ActivityTime.Diurno,
                educational: "Um dos maiores lagartos das Américas. Onívoro, ajuda a dispersar sementes de frutas nativas e controla pragas e pequenos roedores durante os dias quentes.",
                habitat: "Campos abertos, trilhas ensolaradas e bordas de mata.",
                status: "Pouco Preocupante (LC)",
                baseScore: 160, baseCoins: 16,
                minDist: 2.5f, maxDist: 12f, radius: 0.8f,
                iconFileName: "teiu.png"
            );

            // 21. Carcará
            CreateOrUpdateSpecies(
                folderPath, "Carcara",
                id: "carcara",
                commonName: "Carcará",
                scientificName: "Caracara plancus",
                category: SpeciesCategory.Fauna,
                rarity: SpeciesRarity.Incomum,
                activity: ActivityTime.Diurno,
                educational: "Ave de rapina ágil e oportunista muito adaptada aos campos abertos do Cerrado. Caminha bastante pelo solo em busca de insetos, anfíbios e carcaças, atuando na limpeza ambiental.",
                habitat: "Campos limpos, cerrados abertos e chapadões.",
                status: "Pouco Preocupante (LC)",
                baseScore: 230, baseCoins: 24,
                minDist: 3.5f, maxDist: 16f, radius: 0.7f,
                iconFileName: "carcara.png"
            );

            // 22. Coruja-buraqueira
            CreateOrUpdateSpecies(
                folderPath, "CorujaBuraqueira",
                id: "coruja_buraqueira",
                commonName: "Coruja-buraqueira",
                scientificName: "Athene cunicularia",
                category: SpeciesCategory.Fauna,
                rarity: SpeciesRarity.Incomum,
                activity: ActivityTime.Ambos,
                educational: "Diferente da maioria das corujas, é facilmente observada durante o dia vigiando seus ninhos cavados no solo ou em cupinzeiros abandonados.",
                habitat: "Campos limpos e áreas abertas de capim baixo.",
                status: "Pouco Preocupante (LC)",
                baseScore: 240, baseCoins: 25,
                minDist: 3f, maxDist: 14f, radius: 0.55f,
                iconFileName: "coruja_buraqueira.png"
            );

            // 23. Jiboia
            CreateOrUpdateSpecies(
                folderPath, "Jiboia",
                id: "jiboia",
                commonName: "Jiboia-do-cerrado",
                scientificName: "Boa constrictor",
                category: SpeciesCategory.Fauna,
                rarity: SpeciesRarity.Raro,
                activity: ActivityTime.Ambos,
                educational: "Serpente não peçonhenta de grande porte que caça por constrição. Sua camuflagem perfeita no solo seco e galhos a torna uma predadora essencial no equilíbrio de roedores.",
                habitat: "Cerradão, matas secas e áreas com vegetação densa.",
                status: "Pouco Preocupante (LC)",
                baseScore: 340, baseCoins: 36,
                minDist: 3f, maxDist: 13f, radius: 1.0f,
                iconFileName: "jiboia.png"
            );

            // 24. Cascavel
            CreateOrUpdateSpecies(
                folderPath, "Cascavel",
                id: "cascavel",
                commonName: "Cascavel",
                scientificName: "Crotalus durissus",
                category: SpeciesCategory.Fauna,
                rarity: SpeciesRarity.Raro,
                activity: ActivityTime.Ambos,
                educational: "Serpente peçonhenta típica de áreas secas e abertas do Cerrado, reconhecida pelo guizo na ponta da cauda que usa como aviso sonoro para afastar ameaças.",
                habitat: "Campos abertos, solos secos e pedregosos.",
                status: "Pouco Preocupante (LC)",
                baseScore: 350, baseCoins: 38,
                minDist: 3.5f, maxDist: 13f, radius: 0.85f,
                iconFileName: "cascavel.png"
            );

            // 25. Galito
            CreateOrUpdateSpecies(
                folderPath, "Galito",
                id: "galito",
                commonName: "Galito",
                scientificName: "Alectrurus tricolor",
                category: SpeciesCategory.Fauna,
                rarity: SpeciesRarity.Incomum,
                activity: ActivityTime.Diurno,
                educational: "Pequena ave campestre ameaçada, com plumagem contrastante e cauda marcante. Depende diretamente da conservação dos campos nativos de capim alto do Cerrado.",
                habitat: "Campos limpos e campos úmidos de capim nativo.",
                status: "Vulnerável (VU)",
                baseScore: 270, baseCoins: 28,
                minDist: 2.5f, maxDist: 12f, radius: 0.45f,
                iconFileName: "galito.png"
            );

            // 26. Rato-de-espinho
            CreateOrUpdateSpecies(
                folderPath, "RatoDeEspinho",
                id: "rato_de_espinho",
                commonName: "Rato-de-espinho",
                scientificName: "Clyomys laticeps",
                category: SpeciesCategory.Fauna,
                rarity: SpeciesRarity.Comum,
                activity: ActivityTime.Noturno,
                educational: "Pequeno roedor nativo do Cerrado que cava tocas rasas no solo arenoso e possui pelos rígidos semelhantes a espinhos no dorso para proteção contra predadores.",
                habitat: "Campos cerrados e veredas secas.",
                status: "Pouco Preocupante (LC)",
                baseScore: 155, baseCoins: 15,
                minDist: 2f, maxDist: 10f, radius: 0.4f,
                iconFileName: "rato_de_espinho.png"
            );

            // --- FLORA COMPLEMENTAR (12 Espécies Nativas do Cerrado -> Total 20 Flora) ---

            // 27. Araticum
            CreateOrUpdateSpecies(
                folderPath, "Araticum",
                id: "araticum",
                commonName: "Araticum-do-cerrado",
                scientificName: "Annona crassiflora",
                category: SpeciesCategory.Flora,
                rarity: SpeciesRarity.Incomum,
                activity: ActivityTime.Ambos,
                educational: "Árvore frutífera nativa cujos frutos aromáticos e adocicados (marolo) são muito apreciados pela fauna do Cerrado, como lobos-guará, antas e primatas.",
                habitat: "Cerrado típico e cerradão.",
                status: "Pouco Preocupante (LC)",
                baseScore: 155, baseCoins: 15,
                minDist: 4.5f, maxDist: 20f, radius: 2.5f,
                iconFileName: "araticum.png"
            );

            // 28. Cajuzinho-do-campo
            CreateOrUpdateSpecies(
                folderPath, "CajuzinhoDoCampo",
                id: "cajuzinho_do_campo",
                commonName: "Cajuzinho-do-campo",
                scientificName: "Anacardium humile",
                category: SpeciesCategory.Flora,
                rarity: SpeciesRarity.Comum,
                activity: ActivityTime.Ambos,
                educational: "Arbusto rasteiro com caule subterrâneo profundo adaptado ao fogo. Seus pequenos cajus vermelhos e doces são ricos em vitamina C e alimentam aves e mamíferos.",
                habitat: "Campos sujos e cerrados abertos.",
                status: "Pouco Preocupante (LC)",
                baseScore: 130, baseCoins: 13,
                minDist: 2f, maxDist: 12f, radius: 1.0f,
                iconFileName: "cajuzinho_do_campo.png"
            );

            // 29. Canela-de-ema
            CreateOrUpdateSpecies(
                folderPath, "CanelaDeEma",
                id: "canela_de_ema",
                commonName: "Canela-de-ema",
                scientificName: "Vellozia squamata",
                category: SpeciesCategory.Flora,
                rarity: SpeciesRarity.Raro,
                activity: ActivityTime.Ambos,
                educational: "Planta ornamental endêmica de campos rupestres, com tronco fibroso resistente a incêndios e belas flores lilases que desabrocham após as chuvas.",
                habitat: "Campos rupestres e solos pedregosos.",
                status: "Pouco Preocupante (LC)",
                baseScore: 220, baseCoins: 22,
                minDist: 2.5f, maxDist: 14f, radius: 1.2f,
                iconFileName: "canela_de_ema.png"
            );

            // 30. Capim-flechinha
            CreateOrUpdateSpecies(
                folderPath, "CapimFlechinha",
                id: "capim_flechinha",
                commonName: "Capim-flechinha",
                scientificName: "Tristachya leiostachya",
                category: SpeciesCategory.Flora,
                rarity: SpeciesRarity.Comum,
                activity: ActivityTime.Ambos,
                educational: "Gramínea nativa alta que dá o tom dourado característico às savanas do Cerrado, servindo de abrigo e alimento para aves campestres e pequenos mamíferos.",
                habitat: "Campos limpos e campos cerrados.",
                status: "Pouco Preocupante (LC)",
                baseScore: 100, baseCoins: 10,
                minDist: 2f, maxDist: 10f, radius: 0.9f,
                iconFileName: "capim_flechinha.png"
            );

            // 31. Catuaba
            CreateOrUpdateSpecies(
                folderPath, "Catuaba",
                id: "catuaba",
                commonName: "Catuaba-do-cerrado",
                scientificName: "Anemopaegma arvense",
                category: SpeciesCategory.Flora,
                rarity: SpeciesRarity.Incomum,
                activity: ActivityTime.Ambos,
                educational: "Subarbusto medicinal nativo de flores tubulares claras e frutos característicos, muito visitado por abelhas nativas polinizadoras.",
                habitat: "Campos cerrados e solos arenosos.",
                status: "Pouco Preocupante (LC)",
                baseScore: 145, baseCoins: 15,
                minDist: 2f, maxDist: 12f, radius: 1.0f,
                iconFileName: "catuaba.png"
            );

            // 32. Cinzeiro (Pau-tucano)
            CreateOrUpdateSpecies(
                folderPath, "Cinzeiro",
                id: "cinzeiro",
                commonName: "Cinzeiro (Pau-tucano)",
                scientificName: "Vochysia tucanorum",
                category: SpeciesCategory.Flora,
                rarity: SpeciesRarity.Comum,
                activity: ActivityTime.Ambos,
                educational: "Árvore nativa do Cerrado de floração amarela intensa que atrai abelhas, beija-flores e borboletas, sendo muito importante na recuperação de áreas degradadas.",
                habitat: "Cerrado sensu stricto e cerradão.",
                status: "Pouco Preocupante (LC)",
                baseScore: 125, baseCoins: 12,
                minDist: 5f, maxDist: 22f, radius: 3.0f,
                iconFileName: "cinzeiro.png"
            );

            // 33. Copaíba
            CreateOrUpdateSpecies(
                folderPath, "Copaiba",
                id: "copaiba",
                commonName: "Copaíba",
                scientificName: "Copaifera langsdorffii",
                category: SpeciesCategory.Flora,
                rarity: SpeciesRarity.Raro,
                activity: ActivityTime.Ambos,
                educational: "Árvore frondosa conhecida pelo valioso óleo-resina medicinal extraído de seu tronco. Suas sementes com arilo alaranjado são muito procuradas por tucanos e macacos.",
                habitat: "Cerradão e matas de galeria.",
                status: "Pouco Preocupante (LC)",
                baseScore: 210, baseCoins: 22,
                minDist: 5.5f, maxDist: 25f, radius: 3.4f,
                iconFileName: "copaiba.png"
            );

            // 34. Gravatá (Caraguatá)
            CreateOrUpdateSpecies(
                folderPath, "Gravata",
                id: "gravata",
                commonName: "Gravatá-do-cerrado",
                scientificName: "Bromelia balansae",
                category: SpeciesCategory.Flora,
                rarity: SpeciesRarity.Incomum,
                activity: ActivityTime.Ambos,
                educational: "Bromélia terrestre robusta com folhas espinhosas que formam rosetas vermelhas na floração. Acumula água da chuva e serve de abrigo para pequenos anfíbios e insetos.",
                habitat: "Sub-bosque de cerradão e bordas de mata.",
                status: "Pouco Preocupante (LC)",
                baseScore: 150, baseCoins: 15,
                minDist: 2f, maxDist: 12f, radius: 1.0f,
                iconFileName: "gravata.png"
            );

            // 35. Indaiá
            CreateOrUpdateSpecies(
                folderPath, "Indaia",
                id: "indaia",
                commonName: "Palmeira Indaiá",
                scientificName: "Attalea geraensis",
                category: SpeciesCategory.Flora,
                rarity: SpeciesRarity.Incomum,
                activity: ActivityTime.Ambos,
                educational: "Palmeira nativa do Cerrado de folhas longas em leque e cachos de frutos nutritivos muito consumidos por macacos-prego, araras e roedores.",
                habitat: "Campos cerrados e cerradão.",
                status: "Pouco Preocupante (LC)",
                baseScore: 165, baseCoins: 16,
                minDist: 4f, maxDist: 20f, radius: 2.5f,
                iconFileName: "indaia.png"
            );

            // 36. Lobeira (Fruta-do-lobo)
            CreateOrUpdateSpecies(
                folderPath, "Lobeira",
                id: "lobeira",
                commonName: "Lobeira (Fruta-do-lobo)",
                scientificName: "Solanum lycocarpum",
                category: SpeciesCategory.Flora,
                rarity: SpeciesRarity.Incomum,
                activity: ActivityTime.Ambos,
                educational: "Arbusto símbolo da interação ecológica do Cerrado: seus frutos grandes são o principal alimento do lobo-guará, que por sua vez dispersa suas sementes com maior taxa de germinação.",
                habitat: "Campos abertos e cerrados antropizados ou naturais.",
                status: "Pouco Preocupante (LC)",
                baseScore: 175, baseCoins: 18,
                minDist: 3f, maxDist: 16f, radius: 1.8f,
                iconFileName: "lobeira.png"
            );

            // 37. Pau-santo
            CreateOrUpdateSpecies(
                folderPath, "PauSanto",
                id: "pau_santo",
                commonName: "Pau-santo",
                scientificName: "Kielmeyera coriacea",
                category: SpeciesCategory.Flora,
                rarity: SpeciesRarity.Raro,
                activity: ActivityTime.Ambos,
                educational: "Árvore clássica do Cerrado com casca suberosa espessa (cortiça natural) que isola o tronco contra o calor das queimadas naturais da estação seca.",
                habitat: "Cerrado sensu stricto e campos cerrados.",
                status: "Pouco Preocupante (LC)",
                baseScore: 205, baseCoins: 20,
                minDist: 4.5f, maxDist: 20f, radius: 2.5f,
                iconFileName: "pausanto.png"
            );

            // 38. Sucupira
            CreateOrUpdateSpecies(
                folderPath, "Sucupira",
                id: "sucupira",
                commonName: "Sucupira-branca",
                scientificName: "Pterodon emarginatus",
                category: SpeciesCategory.Flora,
                rarity: SpeciesRarity.Incomum,
                activity: ActivityTime.Ambos,
                educational: "Árvore nativa de floração rosada/arroxeada espetacular e sementes aromáticas usadas tradicionalmente na medicina popular, além de madeira muito resistente.",
                habitat: "Cerrado típico e cerradão.",
                status: "Pouco Preocupante (LC)",
                baseScore: 170, baseCoins: 17,
                minDist: 5f, maxDist: 22f, radius: 3.0f,
                iconFileName: "sucupira.png"
            );

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!Application.isBatchMode && !SuppressDialogs)
            {
                EditorUtility.DisplayDialog("Cerrado Engine", "Espécies iniciais criadas com sucesso na pasta 'Assets/Dados/Especies'!", "OK");
            }
        }

        private static void CreateOrUpdateSpecies(
            string folder, string fileName,
            string id, string commonName, string scientificName,
            SpeciesCategory category, SpeciesRarity rarity, ActivityTime activity,
            string educational, string habitat, string status,
            int baseScore, int baseCoins,
            float minDist, float maxDist, float radius,
            string iconFileName)
        {
            string assetPath = $"{folder}/{fileName}.asset";
            SpeciesData asset = AssetDatabase.LoadAssetAtPath<SpeciesData>(assetPath);

            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<SpeciesData>();
                AssetDatabase.CreateAsset(asset, assetPath);
            }

            // Usando SerializedObject para preencher os campos privados serializados
            SerializedObject so = new SerializedObject(asset);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("commonName").stringValue = commonName;
            so.FindProperty("scientificName").stringValue = scientificName;
            so.FindProperty("category").enumValueIndex = (int)category;
            so.FindProperty("rarity").enumValueIndex = (int)rarity;
            so.FindProperty("activityTime").enumValueIndex = (int)activity;
            so.FindProperty("educationalDescription").stringValue = educational;
            so.FindProperty("habitatDescription").stringValue = habitat;
            so.FindProperty("conservationStatus").stringValue = status;
            so.FindProperty("baseScore").intValue = baseScore;
            so.FindProperty("baseCoins").intValue = baseCoins;
            so.FindProperty("minIdealDistance").floatValue = minDist;
            so.FindProperty("maxIdealDistance").floatValue = maxDist;
            so.FindProperty("targetRadius").floatValue = radius;

            if (!string.IsNullOrEmpty(iconFileName))
            {
                Sprite iconSp = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Dados/IconesEspecies/{iconFileName}");
                if (iconSp != null)
                {
                    so.FindProperty("icon").objectReferenceValue = iconSp;
                }
            }

            so.ApplyModifiedProperties();
        }
    }
}

