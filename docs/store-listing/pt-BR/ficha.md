# Ficha de Google Play · pt-BR · BORRADOR

- **Estado:** rascunho da ficha 1.0, atualizado em 24-10-2026 (Parallext, LLC como estúdio; AUTOMATION AI S.A.S como editora no Google Play, D-048; Liga do Rio conforme D-046). A identidade visual vigente é a v2 (D-045); sem parecer nem registro de marca por enquanto (D-047). Nada será publicado sem revisão do responsável, revisão jurídica dos textos e conferência da build e das opções de produção. O restante é tradução com IA da ficha es-419, pendente de revisor nativo.
- Mismas reglas, marcas y fuentes que `../es-419/ficha.md`. Portugués con «você» (glosario §10). Nombres PT del glosario §5, §7.1, §9, §10 y §12; lo cultural, **pendiente de revisión** del asesor.

## 1. Datos de la app

| Campo | Valor | Origen |
|---|---|---|
| Nome do app (≤ 30) | **Tintalú: Classificar Água** (25) | glosario §1.3 |
| Desenvolvedor | **Parallext, LLC** (parallext.com) | D-041 |
| `applicationId`, correo, cuenta de Play Console | Los de `../es-419/ficha.md` §1 | D-041; 08 §13.1 |
| Site | `https://tintalu.com/` (aún sin conectar: D-041) | D-041 |
| Política de privacidade | `https://tintalu.com/pt-br/privacidade/` (fuente: `infra/web/src/content/pt-BR/privacy.html`, con encarregado Nir Levin Bermúdez Perea y canal LGPD privacy@tintalu.com, D-041, añadido del 2026-10-25) | D-041; 08 §13.1 #2 y #7 |
| Exclusão de conta | `https://tintalu.com/tintalu/pt-br/excluir-conta/` (fuente: `infra/web/src/content/pt-BR/delete.html`) | 08 §11; Q-B13-1 (a) |

## 2. Descrição curta (≤ 80)

> Classifique a água pelas cores das frutas amazônicas. Relaxante, offline.

73 caracteres. Canon del glosario §1.3.

## 3. Descrição completa (≤ 4.000) · traducción con IA, pendiente del revisor nativo

```text
Despeje a água de uma cuia para outra até que cada uma fique com uma só cor. Simples e relaxante. Em Tintalú, cada cor vem de uma fruta ou da água de um rio da bacia amazônica: açaí, buriti, taperebá, camu-camu…

Suba o rio a partir do Estuário, no Brasil, por nove mundos em cinco países: Brasil, Colômbia, Peru, Equador e Bolívia. No Mapa do Rio, a canoa do Capi avança de parada em parada, e o rio de vidro se enche com a água real de cada lugar.

Como jogar
- Toque em uma cuia e depois em outra para despejar a água de cima.
- Só dá para despejar sobre a mesma cor ou em uma cuia vazia.
- Você vence quando cada cuia tiver uma só cor.
- Sem limite de tempo nem de jogadas: jogue no seu ritmo.

Um jogo justo
- Todo tabuleiro tem solução sem usar reforços.
- Voltar à margem é grátis e sempre devolve o mesmo tabuleiro.
- Se um nível resistir várias vezes, o rio baixa e fica mais fácil, e o jogo avisa.
- As etiquetas Difícil e Muito difícil dizem a verdade antes de começar.

Reforços do rio
- Maré: desfaz o seu último despejo.
- Cuia Extra: o Capi traz mais uma cuia.
- Morpho: a borboleta pousa no seu próximo despejo.
- Pororoca: a onda mexe o rio sem tocar nas cuias cheias.
- Vaga-lume: ilumina as águas turvas.

Todo dia, uma novidade
- Desafio do Rio: um tabuleiro novo a cada dia.
- Diário do Rio: carimbe uma página por dia e escolha o seu prêmio.
- Cesta: a cada 10 níveis, grátis e com o conteúdo à vista. Nada de caixas-surpresa.
- Enchente: emende vitórias para o rio subir.
- Liga do Rio: a partir do nível 41, some pontos por semana e consulte as classificações global e do seu país. Outros jogadores poderão ver seu pseudônimo automático, a bandeira do país e seus pontos. [[ABOGADO: revisar]]

Quem vai com você
TiTino, um bicho-preguiça que vive na sua árvore onde o rio encontra o mar, torce por você de casa. Capi, a capivara, traz cuias e rema a canoa. E Rosa, a boto, leva você rio acima.

Para todos os olhos
Ative os padrões de fruta: cada cor tem o seu próprio desenho, para você não depender só da cor.

Aprenda enquanto joga
Cada parada do Mapa traz uma Curiosidade do rio, e Frutas do Rio conta de onde vem cada cor. Em Configurações, Sobre a Amazônia reúne as fontes de cada curiosidade.

Jogue offline
A campanha funciona sem conexão. Os vídeos opcionais e as compras precisam de internet.

Tintalú é grátis, com anúncios e compras opcionais. Tire os anúncios entre os níveis com Sem anúncios.

[PENDIENTE: dueño] Revisão cultural de [nome do assessor cultural], com o consentimento dele ou dela para ser citado.

Jogo independente inspirado na bacia amazônica; sem relação com a Amazon.com, Inc.
```

Notas:
- **Liga do Rio:** precisa de internet, é liberada no nível 41 e só deve constar se a build e a configuração de produção a habilitarem. Conferir a descrição e os avisos de privacidade com essa build antes do envio.
- **Aguaje** sale como «buriti», su nombre PT del glosario §9; **camu camu** como «camu-camu».
- «Nível», como la UI (`level_n` = «Nível {n}», glosario §10). «Fase» es la forma habitual del género en PT-BR [P]: si el revisor nativo la prefiere para la ficha, se cambia en las tres frases.
- «Reforços» / «Reforços do rio» por «comodines»: el nombre PT del conjunto (D-033, añadido del 2026-10-22, punto 8; glosario §5).
- `fact_sheet_title` y `settings_about_amazon` van con su PT de la UI: «Frutas do Rio» y «Sobre a Amazônia» (puntos 3 y 4; glosario §10).
- **Anúncios e vídeos (D-042):** «Os vídeos opcionais […] precisam de internet», «com anúncios» y «Tire os anúncios entre os níveis com Sem anúncios» solo valen si el build que se publica lleva MAX; si no, se quitan, como en `../es-419/ficha.md` §3.
- **Pendiente de revisión** (asesor): Morpho, Pororoca, Vaga-lume, TiTino, Capi, Rosa, «a boto», «Estuário».
- `[PENDIENTE: abogado]`: la línea de independencia en PT-BR es traducción propuesta [P] de la de 08 §13.1 #5; la revisa también el abogado brasileño.

## 4. Palabras clave de apoyo

classificar água, ordenar cores, water sort, quebra-cabeça, sem internet, tubos coloridos, floresta, frutas, tintalu (glosario §1.3).

## 5. Novidades da versão 1.0 (≤ 500) · traducción con IA

```text
Boas-vindas ao rio. A primeira versão de Tintalú: suba o rio por nove mundos, jogue o Desafio do Rio todo dia e preencha o seu Diário.
```

## 6. Textos de las 8 capturas (07 §10.2)

| # | Texto PT-BR | Estado |
|---|---|---|
| 1 | Classifique as águas da Amazônia | canon 07 §10.2 · pendiente de revisión |
| 2 | 14 cores de frutas e rios | Q-B13-23 (a), decidida el 2026-10-20 |
| 3 | Veja o rio ganhar cor | canon 07 §10.2 |
| 4 | Suba o Amazonas até onde ele nasce | Q-B13-6 (a), decidida el 2026-10-20: la propuesta de B13 hasta el revisor nativo |
| 5 | Difícil de verdade, sempre justo | canon 07 §10.2 |
| 6 | Padrões para todos os olhos | canon 07 §10.2 |
| 7 | TiTino te acompanha | canon 07 §10.2 [P] · pendiente de revisión |
| 8 | Jogue offline, volte todo dia | canon 07 §10.2 |

## 7. Ficha personalizada de Brasil (08 §13.1 #6)

Con Pix: si Play ofrece Pix en Brasil al lanzar, una línea al final de la descripción: «Pague suas compras também com Pix, onde o Google Play oferecer.» `[PENDIENTE: dueño]` comprobar en Play Console antes de escribirla (Q-B13-17 (a)). La ficha de Brasil entra con la fase 5 (01 §8.1; 09).
