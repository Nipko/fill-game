# Cuestionario de clasificación IARC (Google Play) · 1.0

- **Estado:** actualizado el 2026-10-30 para la 1.0, con la Liga encendida en producción (D-046; D-064). Lo completa el dueño en Play Console (Contenido de la app › Clasificación de contenido, 08 §13.1 #10); lo que quedaba para el abogado lo cerró la investigación propia (D-057). La clasificación se responde para la build y sus funciones disponibles; confirmar la configuración de producción antes del envío.
- **Canon:** publicidad **sí** solo si MAX está en el build; compras **sí**; ítems aleatorios **no**; juegos de azar simulados **no**. D-046 incorpora una clasificación pública con seudónimo, país/bandera y puntos semanales, sin chat ni texto libre: ya no corresponde reutilizar la respuesta «sin interacción de usuarios» de la ficha anterior a la Liga. El IARC asigna clasificaciones e indicadores interactivos por región a partir del cuestionario; confirmar las respuestas frente a la pregunta exacta visible en Play Console. [IARC: cómo funciona](https://globalratings.iarcservices.com/how-iarc-works/); [Google Play: clasificación de contenido](https://support.google.com/googleplay/android-developer/answer/9859655?hl=en).
- **Nota del 2026-10-28 (D-041, D-049 y D-057):** el build de la 1.0 lleva anuncios de AdMob (D-049; MAX salió del build), así que «Contiene anuncios» es **Sí**; el correo de contacto para IARC es `support@tintalu.com` (D-041, añadido del 2026-10-25); y el país de la Liga no sale de la conexión sino del país legal del teléfono (SIM, cuenta de Play o región; T-L12, D-057). Lo de abajo que dice otra cosa es historia. Respuestas listas para pegar: `docs/dev/publicar-play-console.md` §9.
- Las preguntas exactas las da Play Console [NV]; este borrador las agrupa por los temas habituales del cuestionario IARC para juegos. Las respuestas sin canon llevan su justificación y, si dudan, una pregunta en `README.md`.

## 1. Datos generales

| Campo | Respuesta |
|---|---|
| Desarrollador | **Parallext, LLC** (D-041) |
| Correo de contacto para IARC | `support@tintalu.com` (D-041, añadido del 2026-10-25; Q-B13-3 cerrada) |
| Categoría | Juego |
| Público objetivo (aparte, en «Público objetivo y contenido») | **13+**, sin Families; pantalla de edad neutral con bandas {<13 bloqueado, 13-17, 18+} (08 §13.1 #11; 05 §7) |

## 2. Contenido

| Tema [NV] | Respuesta | Justificación (GDD) |
|---|---|---|
| Violencia | **No** | Puzle de vertido sin personajes heridos. La Piraña (mecánica del 183) muerde una cuia, no a un personaje (07 §7.7; glosario §6.5); el jaguar y la rana dardo son solo iconos de etiqueta, «nunca veneno jugable» (glosario §3.3). Q-B13-11 (a), decidida el 2026-10-20 |
| Sangre, gore | No | — |
| Miedo, terror | No | «Noche de jaguar» es un subtítulo de dificultad con fondo nocturno, sin sustos ni música tensa (07 §9.3; glosario §12) |
| Contenido sexual, desnudez | No | — |
| Lenguaje soez, humor grosero | No | Textos en firme para la 1.0 (D-054; D-064); TiTino solo anima (glosario §3.4) |
| Drogas, alcohol, tabaco | **No** | La Tinaja guarda **solo agua**; prohibidos masato, chicha, aguardiente y cualquier bebida (07 §8.1); sin ayahuasca (07 §8.1) |
| Discriminación | No | Sin pueblos como personajes ni símbolos étnicos (07 §8.1) |
| Juegos de azar simulados | **No** (canon) | Ni ruleta, ni tragaperras, ni apuestas (05 §7) |
| Juegos de azar con dinero real | No | — |

## 3. Funciones interactivas y compras

| Tema [NV] | Respuesta | Justificación |
|---|---|---|
| Los usuarios pueden interactuar o intercambiar contenido | **Sí** (la Liga sale encendida, D-064; cerrado por D-057, investigación propia) | La clasificación permite que otros jugadores vean el seudónimo automático, el avatar (un animal del juego), la bandera del país y los puntos. No hay chat, mensajería ni contenido libre. Si la pregunta exacta en Play limita «interactuar o intercambiar contenido» a voz, texto, imágenes o audio aportados por usuarios, contestar **No** a ese alcance estrecho. La Liga está en la 1.0 (D-046). |
| Comparte la ubicación del usuario con otros usuarios | **Sí, a nivel de país** (cerrado por D-057, investigación propia) | La clasificación pública muestra el país/bandera: el país legal del teléfono (SIM, cuenta de Play o región; T-L12), nunca la conexión. No se obtiene GPS ni ubicación precisa. Si la pregunta exacta de Play se limita a ubicación física actual o precisa, contestar **No** a ese alcance y conservar la divulgación del país aproximado en Seguridad de los datos. |
| Compras digitales | **Sí** (canon) | Catálogo `af_*` (05 §4.1; 08 §13.1 #14) |
| Compras de elementos aleatorios (cajas de botín) | **No** (canon) | Canasta gratuita, determinista y con el contenido a la vista (`eco_basket_cycle`, 08 §9); Cántaro a cantidad exacta; bundles con contenido listado; Cuaderno de campo (1.1) sin sobres (05 §7). Contestar «sí» por error llevaría a ClassInd 18 en Brasil (09 R2) |
| Acceso a internet sin restricciones (navegador) | No | El juego no abre páginas web dentro de la app [P]. Si Ajustes enlaza la política o la eliminación de cuenta, se abren en el navegador del sistema |
| Contiene anuncios | **Sí** (canon; 08 §13.1 #8): el build de la 1.0 lleva AdMob, con Unity Ads en su mediación (D-049) | Anuncios ajustados a la clasificación: `maxAdContentRating = G` en todas las bandas y bloqueo de apuestas, dinero real, trading y mercados de predicción (05 §7, D-029) |

## 4. Resultado esperado

- **ClassInd (Brasil): 14**, por las compras (08 §13.1 #10; research/08 C5.2 [V/E]).
- **Resto:** no anticipar una etiqueta final; IARC asigna la clasificación regional según las respuestas de la build. «Everyone / Teen» queda como orientación previa [NV], no como resultado garantizado.
- **Brasil en iOS (fase 4):** autoclasificado A12/A14 sin «Loot boxes» (08 §13.2 #4).

## 5. Disparadores de una nueva clasificación

- Cualquier SKU o premio aleatorio (prohibido; 09 R2).
- Cualquier cambio en qué datos de la Liga se muestran a otros jugadores. La Liga sale encendida en producción en la 1.0 (D-046; D-064), y el cuestionario se responde con ella.
- Aldeas y mensajes entre jugadores (2.0).
- Mecánicas o arte con violencia, miedo o sustancias (revisión del asesor y del dueño antes del build).
