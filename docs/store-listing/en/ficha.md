# Ficha de Google Play · en-US (idioma predeterminado) · 1.0

- **Estado:** ficha de la 1.0 en firme (2026-10-30; D-064): Parallext, LLC como estudio; AUTOMATION AI S.A.S como editora en Google Play (D-048); River League según D-046, encendida en producción (D-064). La identidad visual vigente es v2 (D-045); sin dictamen ni registro de marca (D-047). Los textos legales los cerró la investigación propia (D-057). El resto es traducción con IA de la ficha es-419, aceptada tal cual para la 1.0 (D-054; D-064). Antes de enviarla, el dueño comprueba el build y sus opciones de producción.
- **Puesta al día del 2026-10-28 (D-049, D-057 y D-059):** esta es la ficha del **idioma por defecto, en-US** (D-057, P9 (a); 08 §13.1 #5): la ven todos los jugadores cuyo idioma no tiene traducción (Francia, Alemania, Japón, India…). La traducción con IA se acepta tal cual para la 1.0 (D-054; D-064), en lugar de la revisión nativa de `docs/dev/lanzamiento-mundial.md` §3.1. Las traducciones son es-419 (con es-ES como copia) y pt-BR (con pt-PT como copia). La 1.0 sale en todo el mundo salvo RU, BY, VN e ID, al 100 % con publicación administrada (D-057; D-059): la ficha no alude a ninguna prueba ni a países limitados. El paso a paso para pegarla está en `docs/dev/publicar-play-console.md` §5.
- Mismas reglas, marcas y fuentes que `../es-419/ficha.md`. Inglés en imperativo (glosario §10). Los nombres de comodines y sistemas son los EN del glosario §5, §7.1, §10 y §12; lo cultural, en firme (dueño, 2026-10-30; D-064).

## 1. Datos de la app

| Campo | Valor | Origen |
|---|---|---|
| App name (≤ 30) | **Tintalú: Water Sort Puzzle** (26) | glosario §1.3 |
| Developer | **Parallext, LLC** (parallext.com) | D-041 |
| `applicationId`, cuenta de Play Console | Los de `../es-419/ficha.md` §1 (`com.parallext.tintalu`; cuenta de AUTOMATION AI S.A.S) | D-047; D-048 |
| Contact email | `support@tintalu.com` | D-041, añadido del 2026-10-25 |
| Website | `https://tintalu.com/` (conectado el 2026-10-25: D-044, añadido; Play tiene un solo sitio web por app [NV]) | D-041; D-044 |
| Privacy policy | `https://tintalu.com/en/privacy/` (fuente: `infra/web/src/content/en/privacy.html`). **Es la URL que se declara en Play** (D-057, P9 (a); 08 §13.1 #5) | D-041; 08 §13.1 #7 |
| Account deletion | `https://tintalu.com/tintalu/en/delete-account/` (fuente: `infra/web/src/content/en/delete.html`) | 08 §11; Q-B13-1 (a) |

## 2. Short description (≤ 80)

> Sort river water by Amazonian fruit colors. Relaxing sort puzzle, offline.

74 caracteres, medidos en NFC (el glosario §1.3 dice 77). Canon del glosario §1.3.

## 3. Full description (≤ 4.000) · traducción con IA, aceptada (D-054; D-064)

```text
Pour water from one cuia to another until each one holds a single color. Simple and relaxing. In Tintalú, every color comes from a fruit or from the water of a river in the Amazon basin: açaí, aguaje, hog plum, camu camu…

Travel upriver from the Estuary in Brazil through nine worlds in five countries: Brazil, Colombia, Peru, Ecuador and Bolivia. On the River Map, Capi's canoe moves from stop to stop, and the glass river fills with the real water of each place.

How to play
- Tap a cuia, then another, to pour the top water.
- Pour only onto the same color or into an empty cuia.
- Win when every cuia holds a single color.
- No timer and no move limit: play at your own pace.

A fair game
- Every board can be solved without boosters.
- Back to shore is free and always gives you the same board.
- If a level beats you several times, the river drops and makes it easier, and tells you so.
- Hard and Very hard labels tell you the truth before you start.

River boosters
- Tide: undo your last pour.
- Extra Cuia: Capi brings you one more cuia.
- Morpho: the butterfly lands on your next pour.
- Pororoca: the wave stirs the river without touching full cuias.
- Firefly: lights up murky waters.

Something new every day
- River Challenge: a new board every day.
- River Logbook: stamp a page a day and pick your reward.
- Basket: every 10 levels, free, with its contents in plain sight. No surprise boxes.
- Rising River: chain wins to make the river rise.
- River League: from level 41, earn weekly points and view the global and country rankings. Other players can see your automatic pseudonym, avatar, country flag and points.

Who comes along
TiTino, a sloth who lives in his tree where the river meets the sea, cheers you on from home. Capi the capybara brings you cuias and paddles the canoe. Rosa the river dolphin takes you upriver.

For every eye
Turn on fruit patterns: each color has its own design, so you never rely on color alone.

Learn as you play
Every stop on the Map brings a River fact, and River Fruits tells where each color comes from. In Settings, About the Amazon lists the source of every fact.

Play offline
The campaign works without a connection. Optional videos and purchases need internet.

Tintalú is free to play, with ads and optional purchases. Remove the ads between levels with No Ads.

Independent game inspired by the Amazon basin; not affiliated with Amazon.com, Inc.
```

Notas:
- **Taperebá** sale como «hog plum», su nombre EN del glosario §9; los demás tintes citados conservan su nombre (glosario §9).
- «Frutas del río» (`fact_sheet_title`) y «Sobre la Amazonía» (`settings_about_amazon`) van con su EN de la UI: «River Fruits» y «About the Amazon» (D-033, añadido del 2026-10-22, puntos 3 y 4; glosario §10). «River boosters» usa «Boosters», el nombre EN del conjunto de comodines (punto 8; glosario §5).
- **Anuncios y videos:** el build de la 1.0 lleva AdMob (D-049): «Optional videos […] need internet», «with ads» y «Remove the ads between levels with No Ads» se quedan. (Antes dependían de MAX, D-042.) «No Ads» es el nombre del producto (`iap_af_noads_name`).
- **La línea de la River League** va en la ficha: la Liga sale encendida en producción (D-064). Su revisión legal la cerró D-057 (investigación propia); el avatar se añadió el 2026-10-30 (D-056; D-064).
- **River League:** requiere internet y desbloquea en el 41; la descripción y la política de privacidad la declaran como encendida (D-064).
- **En firme (dueño, 2026-10-30; D-064):** Morpho, Pororoca, Firefly, Boto Leap (no se cita), TiTino, Capi, Rosa, «river dolphin» por boto (glosario §12, `fauna.river_dolphin`), «Estuary».
- La línea de independencia en inglés es la traducción de la de 08 §13.1 #5: cerrado por D-057 (investigación propia).

## 4. Palabras clave de apoyo

water sort, color sort, sort puzzle, liquid sort, relaxing games, offline puzzle, jungle, fruit colors, tintalu (glosario §1.3).

## 5. What's new 1.0 (≤ 500) · traducción con IA

```text
Welcome to the river. The first version of Tintalú: travel upriver through nine worlds, play the River Challenge every day and fill your Logbook.
```

## 6. Textos de las 8 capturas (07 §10.2)

| # | Texto EN | Estado |
|---|---|---|
| 1 | Sort the waters of the Amazon | canon 07 §10.2 · en firme (dueño, 2026-10-30; D-064) |
| 2 | 14 colors from fruit and rivers | Q-B13-23 (a), decidida el 2026-10-20 |
| 3 | Watch the river take color | canon 07 §10.2 |
| 4 | Follow the Amazon to its source | Q-B13-6 (a), decidida el 2026-10-20: la propuesta de B13, en firme por D-054 y D-064 |
| 5 | Honest hard levels | canon 07 §10.2 |
| 6 | Patterns for every eye | canon 07 §10.2 |
| 7 | Meet TiTino | canon 07 §10.2 [P] · en firme (dueño, 2026-10-30; D-064) |
| 8 | Play offline, come back daily | canon 07 §10.2 |
