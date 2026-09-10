# EICerrado 📸🌿

**EICerrado** é um jogo educativo de exploração em mundo aberto desenvolvido na engine Unity, focado na conscientização ambiental sobre o bioma Cerrado brasileiro. Criado como um projeto de Iniciação Científica (IC) na Universidade de São Paulo, o jogador assume o papel de um fotógrafo da natureza com a missão de documentar a biodiversidade local. O projeto é voltado para estudantes de 12 a 18 anos, unindo gameplay investigativo com rigor pedagógico.

## Sobre o Projeto

O desenvolvimento adota uma abordagem de *collection-based gameplay* e simulação fotográfica. Através da exploração, o jogador interage com a fauna e flora nativas, preenchendo um Álbum de Fotos interativo que atua como o principal instrumento de educação ambiental e registro de progresso. O ecossistema digital foi projetado para oferecer recompensas baseadas na descoberta de espécies catalogadas por raridade, incentivando a exploração ativa do ambiente.

## Mecânicas Principais

* **Sistema de Fotografia:** O núcleo do gameplay, utilizando lógica programática para validar o enquadramento, a distância e a visibilidade das espécies.
* **Álbum de Fotos Educativo:** Um registro de progressão contendo as fotos capturadas pelo jogador e informações detalhadas sobre a biodiversidade.
* **Economia e Progressão:** A captura de imagens gera moedas e pontuação, permitindo que o jogador adquira melhores equipamentos e interaja com NPCs.
* **Catálogo de Espécies:** O ecossistema já conta com fauna nativa, incluindo a Capivara, Tamanduá-bandeira e Tamanduá-mirim, além de flora característica como o Ipê-amarelo, Sucupira e Aroeira.

## Arquitetura Técnica e Equipe

O projeto utiliza **Unity (C#)** com foco estrito em modularidade para evitar conflitos de versão no trabalho em equipe.
* **Gerenciamento de Dados:** Uso de *ScriptableObjects* para armazenar os status, a raridade e os dados pedagógicos da fauna e flora, desacoplando o conteúdo da lógica de programação.
* **Inteligência Artificial:** A I.A. dos animais utiliza Máquinas de Estado (FSM) integradas ao NavMesh da Unity para navegação autônoma pelo terreno.
* **Organização da Equipe:** Arquitetura de sistemas, programação (C#), mecânicas centrais e modelagem base (Blender) conduzidas por André Luís da Silva Souza. O design de níveis, detalhamento do terreno e posicionamento dos *Prefabs* ficam sob a responsabilidade do parceiro de desenvolvimento.
