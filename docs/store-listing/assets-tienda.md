# Encargo de arte: assets de la tienda (icono, gráfico, capturas y vídeo) · BORRADOR

> **Identidad visual D-045 (24-10-2026):** la v2, «El río que se tiñe», es la identidad oficial vigente; la [v1](../art/marca-oficial/v1/README.md) queda como historial. El icono de Play sale de `docs/art/marca-oficial/v2/out/png/google-play-512.png`. El gráfico de funciones se genera por código desde la misma v2 (`out/png/feature-graphic.png`) y es el final de la 1.0 (D-052: el arte por código es el arte final). La aprobación visual no acredita registro de marca (D-047).

Fecha: 2026-10-20 · Autor: equipo Spark (agente; tanda 5, bloque B13) · Para: el dueño, que lo envía al artista 2D freelance, y el asesor cultural (07 §8.2, fila «Ficha de tienda y creativos de UA») · Estado: **puesto al día el 2026-10-30 (D-052 y D-064):** para la 1.0, las capturas y el gráfico salen del arte por código (`graficos/README.md`); lo de abajo para el artista queda para versiones futuras.

Fuentes: 07 §10 (assets de tienda; manda), §1.3 (trade dress excluido), §7 (UI kit), §8 (pautas culturales) y §11 (fila «Tienda»: 1 + 24 + 1 + 2 piezas, 8 días-persona [P]); 08 §13.1 #5 y #6; glosario §1.2-§1.3; D-003 (dirección visual «Selva brillante premium»); D-013 y D-016 (TiTino); `docs/art/direccion-visual-v2.md`; los textos, de `es-419/ficha.md`, `en/ficha.md` y `pt-BR/ficha.md` §6.

Las medidas de Google Play que no están en el GDD van marcadas [NV]: el artista las contrasta con la ayuda de Play Console vigente antes de exportar. Fuera de este encargo: los creativos de UA (07 §10.4), que tienen su propio encargo.

## 0. Antes de empezar

| Condición | Por qué | Quién |
|---|---|---|
| Marca de «Tintalú» | Sin dictamen ni registro por ahora (D-047); el logotipo v2 (D-045) vale para la tienda | Dueño; `docs/legal/marca.md` |
| Búsqueda figurativa del logotipo | Sin parecido con las apps «Tinta» de la App Store (07 §10) | Abogado, cuando el logotipo esté diseñado |
| Las condiciones de TiTino (D-016) | La captura 7, el plano final del vídeo y el concepto D del icono lo usan | **En firme (dueño, 2026-10-30; D-064):** TiTino, su especie y su árbol, sin búsqueda de marca previa (D-047) |
| Gameplay real con el arte final del mundo 1-3 | Las capturas son **gameplay real** (07 §10.2): no se pintan pantallas que el juego no tenga | Artista + integración en el juego |

## 1. Reglas comunes (07 §10, §1.3; D-003)

- **Dirección visual D-003:** vidrio con brillos, madera tallada, hojas y luz de juego; **sin papel, gouache ni acuarela**. 07 §10.2 dice «texto corto superpuesto en papel marfil»: choca con D-003. **Decidido (Q-B13-5 (a), 2026-10-20):** placa de madera clara o de vidrio del UI kit.
- **El icono y las capturas no llevan el nombre** «Tintalú»; la marca solo va en el logotipo del vídeo (07 §10).
- **Prohibido (trade dress de los líderes, 07 §1.3; D-003):** gatos, caldero, oro biselado, gemas, cintas rojas, calavera, silueta de frasco con labio; Capi fijo en el centro inferior con un objeto; la sonrisa o la flecha de Amazon y el naranja #FF9900 sobre negro (07 §10.1).
- **Ficha:** sin «Best», «#1», «Free», emojis ni insignias en el icono (07 §10; glosario §1.1).
- **Cultura (07 §8.1):** sin personas, aldeas identificables, símbolos étnicos ni pueblos; checklist del asset de 07 §8.2 (¿personas o símbolos? ¿decoración propia? ¿lugar de la cuenca o con rótulo? ¿palabras permitidas? ¿lo vio el asesor?).
- **Textos:** con Baloo 2 o Nunito (OFL; 07 §7.1, D-003), contraste ≥ 4,5:1 (D-003), «Tintalú» siempre con tilde (glosario §1.2), sin mayúsculas sostenidas.
- **Entrega:** PNG o PSD por Git LFS (`CLAUDE.md`, «Humanos»; `.gitattributes`). Carpeta: `docs/store-listing/assets/<idioma>/`, fuera de `Assets/` para que no entren en el build (Q-B13-15 (a), decidida el 2026-10-20). Los agentes solo los integran.

## 2. Icono de la app

| Pieza | Especificación | Fuente |
|---|---|---|
| Icono de la ficha | **512 × 512 px**, PNG de 32 bits, ≤ 1 MB [NV]; sin texto | 07 §10; tamaño y peso [NV] |
| Icono adaptativo de Android | Capas de primer plano y fondo de 108 × 108 dp con la zona segura central de 66-72 dp [NV]; **fondo a sangre** | 07 §10.1 |
| Legibilidad | A **48 px** sobre fondo claro y oscuro; la cuia, con cuello y hombro exagerados un 15 % | 07 §10.1 |

**Conceptos** (07 §10.1; experimento de ficha A/B; pregunta 3 de 07: A como control):

| Concepto | Qué dibujar | Estado |
|---|---|---|
| **A. Cuia que vierte** | Cuia inclinada fiel a `CuiaGeometry.cs`, con las dosis del juego; vierte açaí #6A2C91 sobre el agua blanca del Amazonas #B08A5A, que se tiñe desde el impacto río abajo. | Concepto sustituido por la identidad oficial v2, generada por código en `docs/art/marca-oficial/v2/`; se conserva aquí como antecedente. |
| B. Capi con cuia | Capi de frente con una cuia de 4 tintes; fondo ámbar suave | Alternativa de experimento; Capi, en firme (dueño, 2026-10-30; D-064) |
| C. Encuentro de las Aguas | Dos aguas en diagonal (ámbar-té #522500 y beige #B08A5A) que no se mezclan, con una cuia de perfil con dos dosis | Alternativa de experimento. El ámbar-té es el de D-029 (07 §2.1; `direccion-visual-v2.md`, revisión 11) |
| D. TiTino en su rama [P] | TiTino cuelga de su rama sobre una cuia que vierte, cara de frente con el antifaz, legible a 48 px | Solo con arte final y las condiciones de D-016 (sección 0) |

## 3. Gráfico de funciones

| Especificación | Valor |
|---|---|
| Medida | **1024 × 500 px** (07 §10; 08 §13.1 #5), JPEG o PNG de 24 bits sin transparencia [NV] |
| Contenido | Composición horizontal de la identidad v2: icono con cuia y río teñido más el logotipo horizontal generado desde Baloo 2. Salida editable SVG y PNG opaco en `docs/art/marca-oficial/v2/out/feature-graphic.svg` y `out/png/feature-graphic.png`. |
| Texto | Solo el nombre «Tintalú», integrado en el logotipo. Gráfico provisional generado por código; el artista debe entregar el definitivo. |
| Estado | **Final para la 1.0** (D-052: el arte por código es el arte final; la marca no espera al dictamen, D-047). No implica que el signo esté registrado. |
| Zona segura | El icono y el nombre quedan dentro del área central; contrastar el recorte y la superposición de Play antes de sustituir por la pieza final [NV]. |

## 4. Capturas (8 por idioma × 3 idiomas = 24)

- **Medida:** **1080 × 1920 px**, vertical (07 §10.2); JPEG o PNG de 24 bits [NV]. Al menos 4 capturas verticales de 1080 px o más para la recomendación de Play (07 §10).
- **Composición:** gameplay real a pantalla casi completa y una franja superior con el texto corto en su placa (sección 1). Las tres primeras siguen la regla de research/08: recipiente autóctono vertiendo río teñido, fondo de un país, texto corto (07 §10.2).
- **Idiomas:** la misma imagen con el texto en es-419, en y pt-BR; la UI del juego, también en su idioma (se capturan con el juego en ese idioma).
- **Nombres de archivo** [P]: `captura_<nn>_<idioma>.png` (`captura_01_es-419.png`).

| # | Qué muestra (07 §10.2) | es-419 | en | pt-BR | Depende de |
|---|---|---|---|---|---|
| 1 | Gameplay real en Manaos: una cuia vertiendo Açaí con el Encuentro de las Aguas detrás | Ordena el agua del Amazonas | Sort the waters of the Amazon | Classifique as águas da Amazônia | Fondo de Manaos final; en firme (dueño, 2026-10-30; D-064) |
| 2 | Tablero del Estuario con 5 cuias, iconos de fruta visibles | 14 colores de frutas y ríos | 14 colors from fruit and rivers | 14 cores de frutas e rios | Fondo del Estuario; patrones activados; texto de Q-B13-23 (a), decidida el 2026-10-20 |
| 3 | Llenar y teñir a mitad (frutas cayendo) | Mira cómo se tiñe el río | Watch the river take color | Veja o rio ganhar cor | Miquitos finales (en firme, dueño, 2026-10-30; D-064; glosario §3.3) |
| 4 | Mapa del río con la forma real de la cuenca, el río de vidrio a medio llenar, la canoa con la bandera izada y las paradas (D-013) | Remonta el Amazonas hasta donde nace | Follow the Amazon to its source [P] | Suba o Amazonas até onde ele nasce [P] | Arte final del Mapa (ola 8); EN y PT, la propuesta de B13, en firme por D-054 y D-064 (Q-B13-6 (a)) |
| 5 | Nivel Difícil con el badge de la rana dardo y una Marea usada en el bloqueo | Difícil de verdad, justo siempre | Honest hard levels | Difícil de verdade, sempre justo | Badge final |
| 6 | Patrones de fruta activados, con simulación de daltonismo lado a lado | Patrones para todos los ojos | Patterns for every eye | Padrões para todos os olhos | La simulación lado a lado es un montaje: rotularla como simulación [P] |
| 7 | TiTino en su rama celebrando; al fondo, Capi trayendo una cuia y Rosa saltando (D-013) | TiTino te acompaña | Meet TiTino | TiTino te acompanha | Condiciones de D-016: en firme (dueño, 2026-10-30; D-064) |
| 8 | Reto del río + Bitácora del río + Canasta abierta con su contenido visible | Juega sin wifi, vuelve cada día | Play offline, come back daily | Jogue offline, volte todo dia | Bitácora «1 de 3» (D-033) y Canasta en el juego |

Longitud del texto superpuesto: ≤ 36 caracteres en las tres lenguas [P], el mismo límite que las fichas: el texto canónico más largo es el de la captura 4 en ES, «Remonta el Amazonas hasta donde nace» (36); en PT-BR, «Suba o Amazonas até onde ele nasce» (34). La captura 2 usa el texto de Q-B13-23 (a): «14 colores de frutas y ríos» (27), «14 colors from fruit and rivers» (31), «14 cores de frutas e rios» (25).

## 5. Vídeo de la ficha (07 §10.3)

- **Formato:** 30 s con los primeros 15 s autosuficientes para recortar [P]; vertical 1080 × 1920 y horizontal 1920 × 1080; sin voz; música: el tema de menú (07 §10.3). Pregunta 8 de 07 (30 s con corte a 15 s, o solo 15-20 s) sigue abierta.
- **Publicación:** Play pide un enlace de YouTube, público o no listado, sin anuncios ni restricción de edad [NV]; el canal lo decide el dueño (`docs/legal/marca.md` §4: @Tintalu está ocupado); lo decide el dueño si sube un vídeo.
- **Música:** solo una pista con cesión total para anuncios y tráilers (07 §9.5). El tema de menú actual (`mus_menu`, `music/menu.wav`) es CC0 de la entrega 1: **no se usa en el vídeo** hasta que exista la pista con cesión total (`README.md` §3 bis).

| Segundo | Plano | Qué pasa (07 §10.3) | Texto en pantalla |
|---|---|---|---|
| 0-3 | Gameplay cerrado | Una cuia vierte Açaí; sellado con maracá; sin logo | — |
| 3-8 | Gameplay | Tres vertidos encadenados; 2 cuias se sellan; contador de semillas | — |
| 8-12 | Llenar y teñir | Carga del nivel 42 en Manaos, la versión de 1,4 s a cámara lenta ×2 | — |
| 12-15 | Logo | Logotipo «Tintalú» (tilde visible, gota o pincelada de tinte) sobre el Encuentro de las Aguas | Descriptor: «Ordenar agua» / «Water sort» [P] / «Classificar água» [P] |
| 15-19 | Tensión | Nivel Difícil: quedan dos cuias y el agua se aquieta; aparece la Marea y deshace | — |
| 19-23 | Mascotas | Capi llega con una cuia (rescate); Rosa salta al siguiente nivel | — |
| 23-27 | Mundos | La canoa remonta el río de vidrio del Mapa; el fondo pasa por 4 paradas: Belém, Alter do Chão, el Encuentro de las Aguas y Leticia | — |
| 27-30 | Cierre | Tablero resuelto, confeti de guacamayo, TiTino celebrando en su rama; logo | «Sin wifi» / «Play offline» [P] / «Jogue offline» [P] |

Las versiones del descriptor y del cierre en EN y PT-BR son propuesta [P] de B13 (07 §10.3 solo da el ES). Los nombres de lugar, en firme (dueño, 2026-10-30; D-064; glosario §12).

## 6. Resumen de la entrega

| Pieza | Cantidad | Medida |
|---|---|---|
| Icono final (concepto elegido) + adaptativo | 1 + 2 capas | 512 × 512 px; 108 × 108 dp |
| Alternativas históricas del icono (B, C) | 2 | 512 × 512 px |
| Gráfico de funciones provisional, generado desde la v2 | 1 | 1024 × 500 px |
| Capturas | 8 × 3 idiomas = 24 | 1080 × 1920 px |
| Vídeo | 2 orientaciones (+ corte de 15 s) | 1080 × 1920 y 1920 × 1080 |
| Logotipo «Tintalú» | 1 (con variantes claro/oscuro) | Vectorial [P] |

Estimación: 8 días-persona [P] (07 §11, fila «Tienda»), sin contar los conceptos del experimento ni el logotipo.

El icono v2 y el logo horizontal/vertical son derivados reproducibles. El gráfico de funciones generado por código sirve solo como referencia provisional; falta la entrega final del artista.
