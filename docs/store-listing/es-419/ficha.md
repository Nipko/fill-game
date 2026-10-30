# Ficha de Google Play · es-419 (traducción; también es-ES) · 1.0

- **Estado:** ficha de la 1.0 en firme (2026-10-30; D-064): Parallext, LLC como estudio; AUTOMATION AI S.A.S como editora en Google Play (D-048); Liga del río según D-046, encendida en producción (D-064). La identidad visual vigente es v2 (D-045); sin dictamen ni registro de marca (D-047). Los textos legales los cerró la investigación propia (D-057) y los nombres culturales están en firme (D-054; D-064). Antes de enviarla, el dueño comprueba el build y sus opciones de producción.
- **Puesta al día del 2026-10-28 (D-049, D-057 y D-059):** el idioma por defecto de la ficha es **en-US** (`../en/ficha.md`; D-057, P9 (a); 08 §13.1 #5); esta ficha es la traducción **es-419** y, copiada tal cual, la **es-ES** (D-057: «es-ES (copia de es-419)»). La 1.0 sale en todo el mundo salvo RU, BY, VN e ID, directa a producción al 100 % con publicación administrada (D-057; D-059): la ficha no alude a ninguna prueba ni a países limitados. El build lleva anuncios de AdMob (D-049): las frases de anuncios y videos se quedan. El paso a paso para pegarla está en `docs/dev/publicar-play-console.md` §5.
- **Fuentes:** glosario §1.3 (título y descripción corta, canon), 08 §13.1 #5 (límites y la línea de independencia), 07 §10 (capturas y vídeo), 01 §1-§2 (qué se promete), glosario §5, §7 y §8-§9 (nombres).
- **Reglas de redacción:** español latino neutro con tuteo; sin «Best», «#1», «Free», mayúsculas sostenidas ni emojis (glosario §1.1); «Tintalú» siempre con tilde; ningún identificador ni nombre propio con la cadena «amazon» (la palabra solo aparece de forma descriptiva y en la línea de independencia, que exige 08 §13.1 #5).
- **Marcas:** no quedan marcas de pendiente: lo cultural (fauna, lugares, comodines con nombre de fauna o tupí, TiTino, Capi, Rosa, «boto», «Estuario») está en firme (dueño, 2026-10-30; D-064), en la línea de D-054; el asesor cultural, si lo hay, revisa para versiones futuras. Las preguntas, ya cerradas, están en `../README.md`.

## 1. Datos de la app

| Campo | Valor | Origen |
|---|---|---|
| Nombre de la app (≤ 30) | **Tintalú: Ordenar Agua** (21) | glosario §1.3; 08 §13.1 #1 |
| Nombre del desarrollador | **Parallext, LLC** (parallext.com) | D-041 |
| `applicationId` | **`com.parallext.tintalu`** (confirmado por el dueño el 2026-10-25; inmutable tras la primera subida) | D-047; 08 §13.1 #1; `CLAUDE.md`, regla 6 |
| Cuenta de Play Console | La de **AUTOMATION AI S.A.S**, editora en Google Play y beneficiaria de los cobros; Parallext, LLC desarrolla y responde de los datos (D-048, que resuelve la duda de D-041, «Consecuencias») | D-048 |
| Tipo · categoría | Juego · Puzle | 08 §13.1 #1 |
| Precio | Gratis, con compras integradas y **con anuncios** (AdMob directo, D-049; MAX ya no está, añadido de D-049 del 2026-10-27) | 08 §13.1 #1 y #8; D-049 |
| Correo de contacto | **`support@tintalu.com`** (D-041, añadido del 2026-10-25; Q-B13-3 resuelta) | D-041 |
| Sitio web | `https://tintalu.com/` (conectado y comprobado el 2026-10-25: D-044, añadido del 2026-10-25). Fuente: `infra/web/` | D-041; D-044 |
| Política de privacidad | `https://tintalu.com/privacidad/` (fuente: `infra/web/src/content/es-419/privacy.html`) | D-041; 08 §13.1 #7 |
| Eliminación de cuenta | `https://tintalu.com/tintalu/eliminar-cuenta/` (fuente: `infra/web/src/content/es-419/delete.html`) | 08 §11; Q-B13-1 (a) |

## 2. Descripción corta (≤ 80)

> Ordena el agua del río por colores de frutas amazónicas. Relajante y sin wifi.

78 caracteres (Unicode NFC). Canon del glosario §1.3: no cambiar sin decisión del dueño.

## 3. Descripción completa (≤ 4.000)

```text
Vierte el agua de una cuia a otra hasta que cada una quede de un solo color. Así de simple, así de relajante. En Tintalú, cada color sale de una fruta o del agua de un río de la cuenca amazónica: açaí, aguaje, taperebá, camu camu…

Remonta el río desde el Estuario, en Brasil, por nueve mundos de cinco países: Brasil, Colombia, Perú, Ecuador y Bolivia. En el Mapa del río, la canoa de Capi avanza parada a parada y el río de vidrio se llena con el agua real de cada lugar.

Cómo se juega
- Toca una cuia y luego otra para verter el agua de arriba.
- Solo puedes verter sobre el mismo color o en una cuia vacía.
- Ganas cuando cada cuia tiene un solo color.
- Sin límite de tiempo ni de movimientos: juega a tu ritmo.

Un juego justo
- Cada tablero tiene solución sin usar comodines.
- Volver a la orilla es gratis y siempre te devuelve el mismo tablero.
- Si un nivel se te resiste varias veces, el río baja y te lo pone más fácil, y te avisa.
- Las etiquetas Difícil y Muy difícil te dicen la verdad antes de empezar.

Comodines del río
- Marea: deshace tu último vertido.
- Cuia extra: Capi te trae una cuia más.
- Morpho: la mariposa se posa en tu siguiente vertido.
- Pororoca: la ola remueve el río sin tocar las cuias llenas.
- Luciérnaga: ilumina las aguas turbias.

Cada día, algo nuevo
- Reto del río: un tablero nuevo cada día.
- Bitácora del río: sella una página al día y elige tu premio.
- Canasta: cada 10 niveles, gratis y con su contenido a la vista. Nada de cajas sorpresa.
- Crecida: encadena victorias para que suba el río.
- Liga del río: desde el nivel 41, suma puntos cada semana y consulta las clasificaciones global y de tu país. Otros jugadores verán tu seudónimo automático, tu avatar, la bandera y los puntos.

Quién te acompaña
TiTino, un perezoso que vive en su árbol donde el río entra al mar, te anima desde casa. Capi, la capibara, te trae cuias y rema la canoa. Rosa, la boto, te lleva río arriba.

Para todos los ojos
Activa los patrones de fruta: cada color lleva su propio dibujo, para que no dependas solo del color.

Aprende mientras juegas
Cada parada del Mapa trae un Dato del río, y las Frutas del río cuentan de dónde sale cada color. En Ajustes, «Sobre la Amazonía» reúne las fuentes de cada dato.

Juega sin internet
La campaña se juega sin conexión. Los videos opcionales y las compras sí necesitan internet.

Tintalú es gratis, con anuncios y compras opcionales. Puedes quitar los anuncios entre niveles con «Sin anuncios».

Juego independiente inspirado en la cuenca amazónica; sin relación con Amazon.com, Inc.
```

Recuento: `../README.md` §2 (Unicode NFC). Notas de contenido:
- **Anuncios y videos:** el build de la 1.0 lleva AdMob (D-049), así que «Los videos opcionales […] necesitan internet», «con anuncios» y «Puedes quitar los anuncios entre niveles con «Sin anuncios»» se quedan. (Antes, 2026-10-24, dependían de que el build llevara MAX, D-042.)
- **La línea de la Liga** va en la ficha: la Liga sale encendida en producción (recomendación de D-035, añadido del 2026-10-28; D-064). Su revisión legal la cerró D-057 (investigación propia); el avatar se añadió el 2026-10-30 (D-056; D-064).
- **Sin promesas que el juego no cumpla:** cada frase de la descripción se comprueba con la build final antes de enviar (comodines, Reto del río, Bitácora, Canasta, Crecida, Liga, patrones de fruta, Datos del río, Frutas del río y juego sin conexión). La Liga requiere internet, desbloquea en el 41 y sale encendida en producción (D-064). No se nombra ningún producto de compra ni un precio: los da Google Play.
- Los subtítulos van en tipo oración («Cómo se juega»), sin mayúsculas sostenidas (glosario §1.1). Play no admite negritas ni encabezados en la descripción [NV]: un subtítulo es solo una línea propia.
- «Nueve mundos de cinco países» y la Luciérnaga dependen del alcance de la 1.0 (mundos 1-9; glosario §8): se comprueban con la build final.
- No se promete el número de niveles ni el peso de descarga hasta la build de release (Q-B13-7, Q-B13-8).
- **En firme (dueño, 2026-10-30; D-064):** los nombres de comodines con fauna o tupí (Morpho, Pororoca, Luciérnaga, Salto del Boto; glosario §12), TiTino, Capi y Rosa (glosario §3), «boto», «Estuario» y la frase del árbol de TiTino.
- La línea de independencia es la de 08 §13.1 #5, literal, y va también en EN y PT-BR: cerrado por D-057 (investigación propia).

## 4. Palabras clave de apoyo (no son un campo de Play; guían la redacción)

ordenar agua, clasificar colores, puzzle de agua, water sort, sin internet, tubos de colores, selva, frutas, tintalu (glosario §1.3).

## 5. Novedades de la versión 1.0 (≤ 500)

```text
Te damos la bienvenida al río. Primera versión de Tintalú: remonta el río por nueve mundos, juega el Reto del río cada día y llena la Bitácora.
```

Sin género («te da la bienvenida», nunca «¡Bienvenido!»), como `map_country_welcome` (glosario §10).

## 6. Textos de las 8 capturas (07 §10.2; D-065)

Para la 1.0, la ficha usa el **concepto A «Gameplay héroe»** de `../graficos-v2/A/es-419/` (D-065: lo eligió el dueño el 2026-10-30, Q-GT-2). Los rótulos ya van dibujados en cada imagen y son los de Q-GT-3 (más cortos que los de 07 §10.2; recomendada aplicada). Qué enseña cada captura y cómo se hizo: `../graficos-v2/README.md`.

| # | Archivo | Texto ES-419 |
|---|---|---|
| 1 | `01-vertido-encuentro.png` | Vierte y tiñe el río |
| 2 | `02-todo-encaja.png` | ¡Todo encaja! |
| 3 | `03-mapa-del-rio.png` | Remonta el Amazonas |
| 4 | `04-dificil-tarapoto.png` | Difícil, pero justo |
| 5 | `05-titino-capi-rosa.png` | TiTino te acompaña |
| 6 | `06-patrones.png` | Patrones para todos los ojos |
| 7 | `07-bitacora.png` | Un reto nuevo cada día |
| 8 | `08-reto-del-rio.png` | Sin wifi, a tu ritmo |

Los textos anteriores de 07 §10.2 («Ordena el agua del Amazonas», etc.) quedan para las capturas de `../graficos/` de la pista interna y para los conceptos B y C solo si se rehacen.

## 7. Fichas personalizadas (08 §13.1 #6)

- **es-ES:** la misma ficha, copiada sin cambios (D-057). Las capturas de es-419 valen para es-ES.
- **México (es-419, ficha por país; se queda con la salida mundial, 08 §13.1 #6):** misma ficha; si Play ofrece OXXO en México al lanzar, una línea al final de la descripción: «Paga tus compras también en efectivo con OXXO, donde Google Play lo ofrezca.» el dueño lo comprueba en Play Console antes de escribirla (Q-B13-17 (a)).
- Colombia y Perú: sin ficha propia en la 1.0 [P].
