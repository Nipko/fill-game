# Cuestionario de clasificación IARC (Google Play) · BORRADOR

- **Estado:** borrador actualizado el 2026-10-24 para la Liga prevista en la 1.0 (D-046). Lo completa el dueño en Play Console (Contenido de la app › Clasificación de contenido, 08 §13.1 #10) y lo revisa el abogado brasileño por ClassInd (09, fase 5). La clasificación se responde para la build y sus funciones disponibles; confirmar la configuración de producción antes del envío.
- **Canon:** publicidad **sí** solo si MAX está en el build; compras **sí**; ítems aleatorios **no**; juegos de azar simulados **no**. D-046 incorpora una clasificación pública con seudónimo, país/bandera y puntos semanales, sin chat ni texto libre: ya no corresponde reutilizar la respuesta «sin interacción de usuarios» de la ficha anterior a la Liga. El IARC asigna clasificaciones e indicadores interactivos por región a partir del cuestionario; confirmar las respuestas frente a la pregunta exacta visible en Play Console y con el abogado. [IARC: cómo funciona](https://globalratings.iarcservices.com/how-iarc-works/); [Google Play: clasificación de contenido](https://support.google.com/googleplay/android-developer/answer/9859655?hl=en).
- Las preguntas exactas las da Play Console [NV]; este borrador las agrupa por los temas habituales del cuestionario IARC para juegos. Las respuestas sin canon llevan su justificación y, si dudan, una pregunta en `README.md`.

## 1. Datos generales

| Campo | Respuesta |
|---|---|
| Desarrollador | **Parallext, LLC** (D-041) |
| Correo de contacto para IARC | `[PENDIENTE: dueño]`: dato de Parallext, LLC que falta en D-041 (Q-B13-3 (a): buzón del dominio canónico) |
| Categoría | Juego |
| Público objetivo (aparte, en «Público objetivo y contenido») | **13+**, sin Families; pantalla de edad neutral con bandas {<13 bloqueado, 13-17, 18+} (08 §13.1 #11; 05 §7) |

## 2. Contenido

| Tema [NV] | Respuesta | Justificación (GDD) |
|---|---|---|
| Violencia | **No** | Puzle de vertido sin personajes heridos. La Piraña (mecánica del 183) muerde una cuia, no a un personaje (07 §7.7; glosario §6.5); el jaguar y la rana dardo son solo iconos de etiqueta, «nunca veneno jugable» (glosario §3.3). Q-B13-11 (a), decidida el 2026-10-20 |
| Sangre, gore | No | — |
| Miedo, terror | No | «Noche de jaguar» es un subtítulo de dificultad con fondo nocturno, sin sustos ni música tensa (07 §9.3; glosario §12) |
| Contenido sexual, desnudez | No | — |
| Lenguaje soez, humor grosero | No | Textos revisados por el asesor; TiTino solo anima (glosario §3.4) |
| Drogas, alcohol, tabaco | **No** | La Tinaja guarda **solo agua**; prohibidos masato, chicha, aguardiente y cualquier bebida (07 §8.1); sin ayahuasca (07 §8.1) |
| Discriminación | No | Sin pueblos como personajes ni símbolos étnicos (07 §8.1) |
| Juegos de azar simulados | **No** (canon) | Ni ruleta, ni tragaperras, ni apuestas (05 §7) |
| Juegos de azar con dinero real | No | — |

## 3. Funciones interactivas y compras

| Tema [NV] | Respuesta | Justificación |
|---|---|---|
| Los usuarios pueden interactuar o intercambiar contenido | **Sí (recomendada si la Liga está habilitada; [[ABOGADO: revisar]])** | La clasificación permite que otros jugadores vean el seudónimo automático, la bandera del país y los puntos. No hay chat, mensajería ni contenido libre. Si la pregunta exacta en Play limita «interactuar o intercambiar contenido» a voz, texto, imágenes o audio aportados por usuarios, contestar **No** a ese alcance estrecho y confirmar el indicador interactivo correspondiente con el abogado. La Liga es función prevista en 1.0 (D-046), no 1.2. |
| Comparte la ubicación del usuario con otros usuarios | **Sí provisional, a nivel de país**; [[ABOGADO: revisar el alcance exacto]] | La clasificación pública muestra el país/bandera, calculado desde la conexión o la región del teléfono. No se obtiene GPS ni ubicación precisa. Si la pregunta exacta de Play se limita a ubicación física actual o precisa, contestar **No** a ese alcance y conservar la divulgación del país aproximado en Seguridad de los datos. |
| Compras digitales | **Sí** (canon) | Catálogo `af_*` (05 §4.1; 08 §13.1 #14) |
| Compras de elementos aleatorios (cajas de botín) | **No** (canon) | Canasta gratuita, determinista y con el contenido a la vista (`eco_basket_cycle`, 08 §9); Cántaro a cantidad exacta; bundles con contenido listado; Cuaderno de campo (1.1) sin sobres (05 §7). Contestar «sí» por error llevaría a ClassInd 18 en Brasil (09 R2) |
| Acceso a internet sin restricciones (navegador) | No | El juego no abre páginas web dentro de la app [P]. Si Ajustes enlaza la política o la eliminación de cuenta, se abren en el navegador del sistema |
| Contiene anuncios | **Sí** (canon; 08 §13.1 #8), **solo si el build que se clasifica lleva MAX** (D-042). Hoy el juego no muestra anuncios: un build sin MAX contesta «No» y se repite el cuestionario al añadirlos | Anuncios ajustados a la clasificación: `maxAdContentRating = G` en todas las bandas y bloqueo de apuestas, dinero real, trading y mercados de predicción (05 §7, D-029) |

## 4. Resultado esperado

- **ClassInd (Brasil): 14**, por las compras (08 §13.1 #10; research/08 C5.2 [V/E]).
- **Resto:** no anticipar una etiqueta final; IARC asigna la clasificación regional según las respuestas de la build. «Everyone / Teen» queda como orientación previa [NV], no como resultado garantizado.
- **Brasil en iOS (fase 4):** autoclasificado A12/A14 sin «Loot boxes» (08 §13.2 #4).

## 5. Disparadores de una nueva clasificación

- Cualquier SKU o premio aleatorio (prohibido; 09 R2).
- Habilitar la Liga en producción si el cuestionario enviado se respondió como si no existiera; revisar también cualquier cambio en qué datos se muestran a otros jugadores. La función está prevista para la 1.0 (D-046), con backend aún solo en dev y el interruptor de producción apagado por defecto.
- Aldeas y mensajes entre jugadores (2.0).
- Mecánicas o arte con violencia, miedo o sustancias (revisión del asesor y del dueño antes del build).
