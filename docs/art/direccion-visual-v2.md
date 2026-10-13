# Dirección visual v2: «Selva brillante premium»

Fecha: 2026-10-07 · Autor: dirección de arte (agente) · Base: `main` en `ef676a4` (prototipo v0.1) · Estado: **guía de implementación de la fase visual de D-003** (V1-V8 de `docs/reviews/2026-10-07/evaluacion-claude.md`). No edita el GDD: la reescritura de 07 y GDD.md llega en el lote de documentación de P10, después de que el dueño acepte las capturas.

Revisión 2 (2026-10-07): aplica las observaciones del crítico de arte, verificadas una a una contra el código, `ProjectSettings/` y la captura `docs/reviews/2026-10-07/visual/actual-nivel-7.png`. Cambian el panel (azul petróleo; la variante cálida que se preparó para la comparación de F1 se retiró con D-004), el espacio de mezcla de los brillos, el reparto entre arte procedural y PNG, el HUD, la cinta, la tipografía y los prompts. La sección 13 resume qué se aplicó, qué se matizó y por qué.

Revisión 3 (2026-10-07, D-004): el dueño eligió el panel azul petróleo (C16), confirmó los 38° del vertido (C17) y adoptó el tope del ancho de la cuia según el número de cuias (C18). Se retira la variante B y se recalculan 3.3, 4.1 y la sección 11. En la implementación se añadieron dos propuestas [P]: 36 dp de margen superior en los tableros de una fila (3.2) y el techo del vertido (6.4).

Revisión 4 (2026-10-08, D-005 y D-006): el dueño adoptó el vertido por tramo (D-005) y el tablero, la cuia y el vertido de Magic Sort (D-006). La cuia pasa a ≈ 1:2,8 (5,6 u) con la cavidad a 0,80 del ancho y la cara superior en elipse; el vertido deja la boca sobre el eje del destino a 0,2-0,6 u, se inclina hasta 92° − 9° × dosis que quedan y se dibuja delante de todo, sin techo; desde 11 cuias, 2 o 3 filas según la que dé la cuia más ancha. Se retiran el margen de 36 dp y el techo, y se recalculan 3.2, 3.3, 4.1, 4.2, 6 y la sección 11.

Revisión 5 (2026-10-09, D-010): «Llenar y teñir» sustituye a «Sumergir y teñir». Se retira el agua de inmersión por fila (6.11 y C9) y la intro pasa a su propia capa (tabla de capas, fila 6); el canon está en 07 §5.1 y §6.4.

Revisión 6 (2026-10-12, D-012): la sacudida de rechazo pasa a ±8 dp, 3 ciclos y 240 ms (6.3; canon en 02 §4.1 y 07 §4.3). El marco del panel bajo el HUD con 3 filas se queda como está (3.2).

Fuentes: `docs/dev/decisiones.md` (D-001 a D-006), GDD 07 (§3 paleta, §4 cuia, §5 storyboards, §6 mascotas, §7 UI kit, §8 pautas culturales), GDD 02 (§3.3, §4, §5, §12), GDD 08 §5 (shader y rendimiento), glosario §0, §3, §9 y §10, `docs/research/09-referencia-magic-sort.md` y `docs/research/magic-sort/c-meta-liveops-ux.md` §4, la revisión del 2026-10-07 (material de entrada, verificado contra el código) y las capturas `Logs/device/v01-fix-*.png` y `docs/reviews/2026-10-07/visual/*.png`.

Convenciones:
- **dp**: unidad del lienzo de UI (en el SM-S938B, 1 dp = 3 px).
- **u**: altura de una dosis, igual a la mitad del ancho de la cuia (07 §4.1). Las coordenadas de la cuia van en u, con x de 0 (izquierda) a 2 y y de 0 (pie) a 5,6 en la estándar (D-006).
- HEX en sRGB. El contraste es el de WCAG 2.1. L* y ΔE00 se calculan en CIELAB D65.
- **[P]**: propuesta de esta guía, pendiente de calibrar con capturas.
- Lo que la guía no menciona sigue como está en el GDD.

## 0. Resumen

1. **Qué cambia.** Se pasa de «lámina pintada sobre papel» a una escena de juego con volumen, brillo y respuesta. La pantalla tiene un panorama luminoso del Estuario arriba, un muelle de madera abajo y follaje en las esquinas. En el centro hay un **panel azul petróleo de río, oscuro y quieto**, enmarcado en madera media rojiza: dentro, cada fila de cuias apoya en una repisa de madera tallada con sombra de contacto. El verde oliva de v0.1 desaparece (ΔE00 ≥ 12, VA-04c).
2. **Qué no cambia.** Los 14 tintes y sus HEX, la cuia-garrafa con cavidad recta y 4 dosis iguales, los patrones (15 %, 20 % y 35 %), D-005 (vertido por tramo, como Magic Sort; sustituye a D-001), D-002 (sin resaltar destinos), las posiciones del HUD de 07 §7.4 (ahora sí respetadas: la insignia de nivel va en el centro-izquierda, 3.5), las pautas culturales de 07 §8 y el límite de 60 partículas.
3. **Dónde vive la legibilidad.** Se garantiza con contraste **local**: cavidad opaca oscura, panel oscuro de croma bajo detrás de las cuias y vidrio con contorno claro. Los brillos sobre el líquido se fijan por su **resultado** (ΔL*), no por su alfa, porque el proyecto mezcla en espacio lineal (2.5). Fuera del panel, la escena puede ser saturada y luminosa.
4. **Cómo se implementa.** Se reparte por naturaleza (10.2 y 10.3):
   - **Por código**, las piezas geométricas o de material, donde el código alcanza la calidad de los líderes: vidrio, líquido, panel, repisas, botones, bandeja, placa y partículas. Texturas procedurales con `PixelCanvas`, shaders de UI opcionales en `.shader` de texto y fuentes TTF ya aprobadas.
   - **Con PNG pintados desde el principio**, las piezas orgánicas: panorama, muelle, follaje y cinta-hoja (y después Capi y las frutas). El dueño los genera en paralelo con F1 (`docs/art/prompts-chatgpt.md`). El procedural de esas piezas es solo un respaldo mínimo, y el dueño no revisa la escena hasta tenerlos integrados.

   Sin paquetes nuevos, sin Bloom y sin editar YAML de Unity.
5. **Conflictos con el canon.** La sección 11 recoge 19 conflictos y preguntas para el dueño. El principal: el encargo pedía una barra inferior con Deshacer, Pista y Reiniciar, y 07 §7.4 lo impide. Esta guía sigue el GDD. C16-C18 quedaron resueltas el 2026-10-07 con D-004, incluido el tapado en tableros de una fila (C17).

## 1. Principios: qué la hace «juego» y no «libro»

| # | Principio | Qué significa aquí | Regla medible |
|---|---|---|---|
| 1 | **Volumen** | Todo lo tocable tiene grosor: cara, canto inferior, contorno y sombra proyectada. Lo plano es fondo o está deshabilitado | Botones con canto de 6 dp y sombra (5.1). El estado deshabilitado es el único botón plano |
| 2 | **Brillo** | El vidrio, el líquido, la madera barnizada y los botones tienen luz especular coherente, siempre desde arriba a la izquierda a 35° (07 §1.2) | Franja especular de la cuia con un pico de alfa de 0,55 sobre el vidrio. Sobre el líquido, ΔL* ≤ +12 en los tintes de L* < 40 y nunca dos brillos en el mismo punto (4.1, 4.2 y 2.5). Lustre en el 6-28 % superior de cada botón (5.1) |
| 3 | **Saturación controlada** | Escena saturada y luminosa fuera del panel. Dentro del panel, tres niveles: relleno oscuro y de croma bajo; repisas de valor medio, que nunca compiten con las cuias; y los tintes, que son lo único claro **y** saturado a la vez | Fuera del panel, media de S (HSV) ≥ 0,30 y de V ≥ 0,55 (VA-03). Relleno del panel: L* medio entre 15 y 30, desviación típica ≤ 3 y C* ≤ 16 (VA-04). Cara superior de las repisas: L* ≤ 65 (VA-04b) |
| 4 | **Profundidad** | Cinco planos: cielo y orilla lejana → río → muelle → panel con repisas → cuias, FX y UI. Hay sombras de contacto en cada apoyo | Orden de capas de 3.4. Cada cuia en reposo tiene una sombra de contacto sobre la repisa (4.1) |
| 5 | **Respuesta** | Cada toque tiene eco visual en ≤ 1 fotograma: elevación con rebote, sacudida, salpicadura, destello al sellar, estallido al ganar | Duraciones y curvas de la sección 6. El toque nunca espera a una animación (02 §3.2) |
| 6 | **Vida** | La escena respira sin distraer: polen, destellos del río, follaje que se mece y una garza ocasional, siempre fuera del panel | ≤ 16 partículas de ambiente, nunca dentro del panel (6.12) |
| 7 | **Nada de libro** | Sin papel, grano, bordes rasgados, tinta, acuarela que se corre ni tipografía con serifas | Lista «Evitar» (7). Los paneles claros son madera pulida con canto y lustre, no papel |

## 2. Paleta

### 2.1 Tintes del juego: sin cambios

Los 14 tintes de glosario §9 y 07 §3 conservan su HEX, patrón, icono y regla de uso. En el render solo se permite:

- **Luminosidad por dosis:** degradado vertical de ±4 % de L (OKLab) dentro de cada dosis, con el centro exacto (4.2).
- **Brillos superpuestos:** franja especular, brillo vertical del líquido y cara superior en elipse (el antiguo menisco; 07 §3, regla 4; D-006), siempre fuera del centro de la dosis. Dentro de la cavidad se pintan como color opaco precalculado, no como blanco con alfa (2.5 y 4.2).
- **Croma:** no se toca. El píxel central de cada dosis (mediana de un parche de 4 × 4 dp, x = 1,0 u) debe quedar a ΔE00 ≤ 3 del HEX canónico (VA-05).
- Los HEX de los tintes siguen prohibidos en la UI (07 §3, regla 6).

### 2.2 Entorno (Estuario, amanecer)

| Token | HEX | L* | Uso |
|---|---|---|---|
| `cielo_alto` | #7CCBE8 | 78 | Cielo, parte superior |
| `cielo_medio` | #FFC994 | 85 | Cielo, tercio medio |
| `cielo_horizonte` | #FFE4A8 | 92 | Cielo junto al horizonte, halo del sol |
| `sol` | #FFF6D6 | 97 | Disco del sol, a la izquierda, sobre el horizonte |
| `nube_luz` / `nube_sombra` | #FFF0DC / #F3AE8A | 96 / 77 | Nubes |
| `orilla_lejana` | #7FB0A2 | 68 | Línea de selva con bruma |
| `palmera_lejana` | #4E8C78 | 54 | Siluetas de palmeras de açaí |
| `selva_media` / `selva_luz` / `selva_sombra` | #2C7A4B / #63B548 / #1C5236 | 46 / 67 / 31 | Vegetación de la orilla |
| `rio_reflejo` | #F6CF92 | 85 | Reflejo del cielo junto al horizonte |
| `rio_medio` / `rio_base` / `rio_profundo` | #CDA06A / **#B08A5A** / #80603E | 69 / 60 / 43 | Río; `rio_base` es el agua base blanca del Estuario (07 §2.1) |
| `destello` | #FFF4D2 | 96 | Brillos del agua (aditivos) |
| `hoja_oscura` / `hoja_media` / `hoja_clara` / `hoja_brillo` | #145A35 / #23824A / #5DBB3F / #C6EF6B | 33 / 48 / 68 / 89 | Follaje del marco |
| `nervadura` | #0E3F25 | 23 | Nervios de las hojas |
| `heliconia` | #E2483A | 53 | Un único acento floral por racimo de follaje |
| `muelle_luz` / `muelle_base` / `muelle_sombra` | #A87A50 / #80583A / #5A3D28 | 55 / 41 / 29 | Tablas del muelle |
| `ranura` | #2A1A10 | 11 | Juntas entre tablas, ranuras talladas |
| `fibra_luz` / `fibra_base` / `fibra_sombra` | #EED29A / #C49A5C / #8A6436 | 85 / 66 / 45 | Cuerda y fibra de la bandeja. El borde y el pie de la cuia siguen con `Palette.Fiber` (#A8834F, 07 §4.6) |
| `panel_alto` / `panel_bajo` | **#234452 / #142C36** | 27 / 17 | Panel del tablero «azul petróleo de río», elegido por el dueño (D-004): degradado vertical. C* 14 / 11 |
| `panel_charco` | Resultado: +3 a +4 L* | — | Charco de luz suave arriba a la izquierda del panel, del tono de `cielo_horizonte` (4.6). Se fija por su resultado, no por su alfa (2.5) |
| `cavidad` | **#102228** (opaca) | 12 | Cavidad vacía de la cuia (D-003): el tono medio del panel × 0,6, para que el vidrio parezca transparente sobre él |
| `marco` | **#7C4E3A** | 38 | Marco del panel: madera media rojiza, distinta del muelle (ΔE00 5,6 frente a `muelle_base`, más el contorno y la sombra de 4.6) |
| `marco_bisel` | #D29B6E | 68 | Bisel iluminado del marco (línea de 1,5 dp) |
| `madera_oscura` | #36261C | 17 | Bandeja, placa del nivel y marco de los modales |
| `repisa_sup` | **#C4935E** | 64 | Cara superior de la repisa (tope L* ≤ 65, VA-04b) |
| `repisa_frente` → `repisa_frente_inf` | **#866650 → #6E523E** | 46 → 37 | Frente de la repisa (degradado) |
| `repisa_canto` | #F2C38A | 82 | Bisel iluminado (línea de 1,5 dp) |
| `vidrio_borde` | #EAF7F3 | 96 | Contorno y fresnel del vidrio |
| `scrim` | #0B2A33 | 15 | Velo bajo la barra de estado (alfa ≤ 0,35) |

### 2.3 UI

| Familia | `_luz` (0 %) | `_base` (25 %) | `_fondo` (100 %) | `_canto` | `_contorno` | Uso |
|---|---|---|---|---|---|---|
| **Hoja** (primario) | #86D25A | #2F7D32 | #236A29 | #17501E | #0F3A15 | Siguiente, Continuar, Jugar |
| **Agua** (Marea) | #8ADBE6 | #13788F | #0F6275 | #0A4A59 | #073640 | Deshacer en la bandeja y en el modal |
| **Madera** (secundario) | #BF8F62 | #7E5638 | #46302A | #2E2018 | #1E140E | Reiniciar, botones secundarios, placa del nivel |

| Token | HEX | Uso |
|---|---|---|
| `crema` | #FFF8E6 | Texto y glifos sobre botones, placas y madera |
| `tinta` | #2B2118 | Texto sobre paneles claros (= `ui_tinta` de 07 §7.2) |
| `panel_claro` → `panel_claro_fondo` | #FBF0D9 → #F2E4C6 | Superficie de modales: madera clara pulida, nunca papel |
| `deshabilitado` / `deshabilitado_texto` / `deshabilitado_glifo` / `deshabilitado_contorno` | #A9A39A / #3A3530 / #4A4540 / #7C766D | Estado deshabilitado |
| `ambar`, `indigo`, `rojo_aviso` | #C98A2B, #24305E, #9E3B2E | Sin cambios respecto a 07 §7.2 (Difícil, Muy difícil, errores) |

### 2.4 Reglas de color

1. **Distancia a los tintes.** Ninguna superficie de más de 2 dp que toque o rodee una cuia (panel, repisa, cavidad, marco) queda a menos de ΔE00 10 de un tinte. El resto de tokens de UI y entorno, a ΔE00 ≥ 5. Comprobado con todos los tokens de 2.2 y 2.3. Mínimos de las superficies que rodean las cuias:

   | Superficie | ΔE00 mínimo | Tinte más cercano |
   |---|---|---|
   | `panel_alto` / `panel_bajo` | 14,1 / 14,1 | Huito |
   | `cavidad` | 17,0 | Huito |
   | `marco` | **10,2** (el mínimo) | Cacao |
   | `repisa_sup` | 13,0 | Aguaje |
   | `repisa_frente` / `repisa_frente_inf` | 11,7 / 10,7 | Agua del Río Negro / Cacao |

   Excepciones:
   - líneas de ≤ 2 dp (biseles, nervios);
   - los tonos de cuerda `fibra_*` (`fibra_luz` queda a 3,0 de Copoazú);
   - la fibra de la cuia, que es de 07 §4.6;
   - los tokens de 07 §7.2 que no cambian. `indigo` (#24305E) queda a ΔE00 3,0 de Huito: pregunta C13.
2. **Texto ≥ 4,5:1** contra el relleno que tiene **detrás**, medido sin contar el contorno del texto (07 §7.8 y D-003). Iconos y glifos ≥ 3:1. El texto grande en negrita con contorno oscuro podría bajar a 3:1 si el dueño lo aprueba (pregunta C14); hasta entonces rige 4,5:1. Valores comprobados:

   | Texto / relleno | Contraste |
   |---|---|
   | `crema` sobre `hoja_base` y `agua_base` | 4,83 y 4,82 |
   | `crema` sobre `hoja_fondo` y `agua_fondo` | 6,26 y 6,55 |
   | `crema` sobre `madera_base` | 6,05 |
   | `crema` sobre `madera_oscura` | 13,66 |
   | `crema` sobre la franja de texto de la cinta-hoja (`hoja_base` → `hoja_fondo`, 5.5) | 4,83-6,26 |
   | `tinta` sobre `panel_claro_fondo` | 12,52 |
   | `madera_base` sobre `panel_claro_fondo` | 5,10 |
   | `deshabilitado_texto` sobre `deshabilitado` | 4,85 |
   | `deshabilitado_glifo` sobre `deshabilitado` | 3,79 |

   Pares **prohibidos** detrás de texto: `crema` sobre `hoja_clara` #5DBB3F (2,29:1) y sobre `hoja_media` #23824A (4,54:1, al límite y sin margen para el ruido de un PNG).

   Por eso el texto de un botón ocupa la franja vertical del **25 al 85 %** de la cara, donde el relleno está entre `_base` y `_fondo` (≥ 4,83:1). Por encima del 25 % el contraste cae deprisa: al 20 %, 3,85:1 con el degradado horneado y 3,57:1 con un degradado por vértices (que la GPU interpola en lineal). Al 30 % es de 4,92:1.
3. **Contraste compuesto de D-003** (umbrales [P] de P9). Con los cinco tintes del Estuario sobre `cavidad` (L* 12,0):
   - ΔL* mínimo de 19,6 (Açaí);
   - Guanábana frente a cavidad vacía: ΔL* 86,2;
   - contorno del vidrio (`vidrio_borde` con alfa 0,90 compuesto en lineal) frente al panel: 8,6:1 arriba y 12,0:1 abajo.

   Todo cumple los umbrales de ΔL* ≥ 15, ΔL* ≥ 50 y ≥ 3:1. Huito y Cacao (mundos futuros) siguen por debajo de 15: pregunta C8.
4. **Zonas de saturación.**
   - Escena: libre, con la media del principio 1.
   - **Relleno del panel:** L* entre 15 y 30 y **C* ≤ 16** (CIELAB). Se usa C* y no la S de HSV, que no sirve en colores oscuros: `panel_alto` (#234452) tiene S 0,57 en HSV pero C* 14,2, un croma bajo, del orden del oliva de v0.1 (11,3). El límite solo se aplica al relleno.
   - **Repisas:** cara superior con L* ≤ 65 y frente con L* ≤ 46, para que sus franjas claras no compitan con las cuias. Se comprueba con una captura en escala de grises (VA-04b).
   - Cuias: tintes. UI: familias de 2.3.
5. **Sin oro.** Ningún amarillo metálico en marcos, botones ni texto. El único ámbar de UI es `ambar` (Difícil y precios futuros) y la luz dorada pertenece al cielo, no a la UI.
6. **Lejos del oliva de v0.1.** El relleno del panel queda a ΔE00 ≥ 12 del fondo de v0.1 detrás de las cuias (muestras #313D2E, #263024 y #2E392B del crítico y #333F30, #313C2F y #323D2F de esta guía, todas de `actual-nivel-7.png`). El panel queda a 13,4 como mínimo (`panel_bajo`); el panel anterior (#2E4339 / #22332C) quedaba a 3,9-5,4, a simple vista el mismo color (VA-04c).

### 2.5 Espacio de mezcla: los alfas no son portables

El proyecto usa espacio de color **lineal** (`ProjectSettings.asset`: `m_ActiveColorSpace: 1`). Hay dos mezclas distintas:

| Dónde se mezcla | Espacio | Ejemplo |
|---|---|---|
| **[GPU]**: una capa (`Image`, `RawImage`, sprite) compuesta por Unity sobre lo que tiene detrás | Lineal | `GlassFront` sobre el líquido, sombra de contacto sobre la repisa, velo de estado, atenuado |
| **[horno]**: dos capas mezcladas dentro de una misma textura con `PixelCanvas.Blend` | sRGB (gamma) | Lustre de un botón sobre su cara, veta sobre la madera, fresnel sobre el tinte del vidrio |

El mismo alfa da resultados muy distintos:
- blanco al 0,28 sobre Açaí: L* 64 en lineal y 51 en gamma (Açaí tiene 32; Aguaje, 64);
- franja principal, secundaria y brillo vertical apilados (≈ 0,38): Açaí llega a L* 71, casi Bacaba;
- negro al 0,45 sobre la repisa anterior (#DDAA6E, L* 73): 57 en lineal y 42 en gamma.

Reglas:
1. En esta guía, **un alfa sin marca es [GPU]**. Los alfas de las capas internas de un sprite (botones, bandeja, placa, madera, hojas, cuerda, y sus sombras cuando van horneadas en él) son [horno].
2. Los efectos que tocan la legibilidad se fijan **por su resultado**, medido en la captura del teléfono:
   - brillos sobre el líquido: ΔL* ≤ +12 en los tintes de L* < 40 y nunca dos brillos en el mismo punto: a cada altura, cada x lleva como mucho uno (4.1 y 4.2);
   - sombra de contacto: ΔL* ≥ 15 en su centro frente a la cara superior de la repisa (4.1);
   - charco de luz del panel: +3 a +4 L* (4.6).
3. Si un efecto horneado tiene que igualar a uno de GPU, se hornea con una mezcla lineal (un `BlendLinear` en `PixelCanvas` [P]: convertir a lineal, mezclar y volver a sRGB) o se calibra el alfa contra la captura. Es la misma causa por la que `Palette.PatternMark` precalcula el color de las marcas en sRGB y las pinta opacas.
4. Todo valor de esta sección se **calibra con capturas del teléfono** (`adb exec-out screencap -p`), no del Editor (VA-05b).

## 3. Composición de la pantalla

### 3.1 Lienzo de referencia

- **Referencia:** 360 × 800 dp (9:20).
- **Casos de control:** 360 × 780 (SM-S938B, 1080 × 2340 px) y 360 × 640 (16:9).
- **Zonas seguras:** `safeTop` = max(32, inserto) y `safeBottom` = max(24, inserto), como en `GameScreen.ApplyScreenMetrics`.
- **Tabletas:** la escena cubre todo el lienzo, el panel se limita a 480 dp de ancho y la cuia no supera tope(T) (02 §5; D-004).

```
 y (dp)   360 × 800, nivel 7 (filas de 4 y 3; cuias de 48 × 134,4 dp; D-006)
   0 ┌────────────────────────────────────────┐  cielo del panorama; velo scrim 0-72
  32 │ barra de estado                        │
  40 │(pausa)╭ Nivel 7 ╮ [semillas]    (⟲)  │  fila del HUD, 48 dp: insignia x 56-192 · Semillas x 196-288 (futuras) · Reiniciar x 292-352
  88 │ follaje · sol · palmeras · orilla · río │  panorama visible: 88-172 (con 780 dp de alto, 88-162)
 172 │╔══════════════════════════════════════╗│  panel azul petróleo, marco de 6 dp en `marco`, x 8-352
     │║    ▯      ▯      ▯      ▯            ║│  fila 1, pie en y ≈ 344,5
     │║▀▀▀▀▀▀▀▀▀▀▀▀▀▀ repisa ▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀║│
     │║       ▯      ▯      ▯                ║│  fila 2, pie en y ≈ 519
     │║▀▀▀▀▀▀▀▀▀▀▀▀▀▀ repisa ▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀║│
 556 │╚══════════════════════════════════════╝│
     │ Capi     tablas del muelle     cuerda  │  556-680: muelle; Capi 64×56
 680 │            ╭[≈ Deshacer]╮              │  bandeja ajustada al contenido (v0: 96 × 80)
 760 │ follaje          orilla        follaje │
 800 └────────────────────────────────────────┘
```

### 3.2 Fórmulas (dp; y medida desde arriba)

```
hudY        = safeTop + 8                 // fila superior de 48 dp (07 §7.4)
hudBottom   = hudY + 48
trayBottom  = H − safeBottom − 16
trayTop     = trayBottom − 80             // bandeja de 80 dp (07 §7.4)
boardTop    = hudBottom                   // = BoardTop de GameScreen
boardBottom = trayTop − 40                // margen de 40 dp (02 §5)
shelfDrop   = 15                          // la repisa termina 15 dp bajo la línea de pie
footMax     = boardBottom − shelfDrop     // sustituye a FootLine = BoardBottom − 30 de la banda de río
cuiaW, filas, ranuras  = BoardLayout.Compute(n, alto_u, W, boardTop, footMax)   // filas de 02 §5 (D-006: 2 o 3 desde 11 cuias, fijas en el nivel), con el tope del ancho según el número de cuias (D-004)
cuiaH       = 2,8 · cuiaW                 // 5,6 u (D-006)
B           = filas·cuiaH + (filas − 1)·40 + 16 + 15        // bloque: elevación de la fila 1 + filas + huecos + repisa
              B ≤ (boardBottom − boardTop) − 27             // la fila 1 elevada queda ≥ 8 dp bajo el canto interior del marco [P]; si no, cuiaW baja
libre       = (boardBottom − boardTop) − B
blockTop    = boardTop + libre / 2        // centrado (D-003): fin del anclaje inferior
panel       = x: (W − min(W, 480))/2 + 8 … simétrico; y: blockTop − m … blockTop + B + 22   // antes 14 (sección 14)
              m = 22 en todas las filas (PanelMargin); sale PanelMarginTopOneRow, que existía solo por el techo del vertido (D-006)
              si panel.top < hudBottom − 8: se recorta a hudBottom − 8 (el marco pasa bajo el HUD); la cuia solo se encoge por el límite de B
```

De dónde salen los 27 dp (cálculo): con el panel recortado a `hudBottom − 8`, su canto interior queda en `boardTop + 5,5` (marco de 13,5 dp). Para dejar 8 dp hasta la fila 1 elevada (`blockTop = boardTop + libre / 2`), hace falta libre / 2 ≥ 13,5, es decir, libre ≥ 27. Sin recorte, los 22 dp de margen ya dejan 8,5 dp.

`BoardLayout` sigue calculando ranuras, anchos y el paso de fila (`cuiaH + 16 + 24`) de 02 §5. Solo cambia el ancla vertical, que pasa a ser `blockTop`, y la línea de pie máxima. Hay que actualizar `BoardLayoutTests`, que hoy fija `FootY` = 590 (V1). `ShiftToClear` sigue funcionando sobre el mismo `_layout`.

### 3.3 Casos medidos (calculados con las fórmulas de 3.2)

Banda libre = `trayTop − panel.bottom`, el espacio del muelle entre el panel y la bandeja.

Recalculada en la ronda 2 de la revisión visual (sección 14) con el margen del panel de **22 dp** (`BoardLayout.PanelMargin`; antes 14): el marco tallado mide 13,5 dp y la cuia elevada tocaba su canto interior. Ahora queda a 8,5 dp. Recalculada otra vez con D-006 (2026-10-08): cuia de 5,6 u (alto = 2,8 × ancho), **22 dp** de margen superior en todos los tableros (sale `PanelMarginTopOneRow`, que solo existía por el techo del vertido) y filas de 02 §5. Con 3 filas y 11-15 cuias a 780 dp, el ancho lo fija el límite de B (3.2): 46,9 dp, con el panel recortado bajo el HUD (y = 80) y la fila 1 elevada a 8 dp del canto interior. La comprueban los casos de `BoardLayoutTests` que actualiza U2.

| Caso | Cuias (ancho × alto dp) | Lienzo | Panel (y) | Alto del panel | Banda libre | Capi visible |
|---|---|---|---|---|---|---|
| Nivel 1 (T = 3, una fila) | 64 × 179,2 | 360 × 780 | 226,9-481,1 | 254,2 | 178,9 | Sí |
| | | 360 × 800 | 236,9-491,1 | 254,2 | 188,9 | Sí |
| | | 360 × 640 | 156,9-411,1 | 254,2 | 108,9 | Sí |
| T = 4 (una fila; ningún nivel del tutorial) | 56 × 156,8 | 360 × 780 | 238,1-469,9 | 231,8 | 190,1 | Sí |
| Niveles 2, 3 y 6 (T = 5) | 47,04 × 131,7 | 360 × 780 | 250,6-457,4 | 206,7 | 202,6 | Sí |
| Niveles 4-10 (T = 6-7, dos filas) | 48 × 134,4 | 360 × 780 | 162,1-545,9 | 383,8 | 114,1 | Sí |
| | | 360 × 800 | 172,1-555,9 | 383,8 | 124,1 | Sí |
| | | 360 × 640 | 92,1-475,9 | 383,8 | 44,1 | No |
| T = 11-15 (3 filas) | 46,9 × 131,3 | 360 × 780 | 80 (recortado)-628,5 | 548,5 | 31,5 | No |
| T = 11-12 (3 filas) | 48 × 134,4 | 360 × 800 | 84,9-643,1 | 558,2 | 36,9 | No |
| T = 13-15 (3 filas) | 47,04 × 131,7 | 360 × 800 | 88,9-639,1 | 550,2 | 40,9 | No |
| T = 16 (3 filas: 6/5/5) | 39,2 × 109,8 | 360 × 780 | 111,9-596,1 | 484,3 | 63,9 | No |
| | | 360 × 800 | 121,9-606,1 | 484,3 | 73,9 | Sí |

- A 360 × 800 los casos no listados suben 10 dp (el ancho no cambia).
- A 360 × 640 ganan las 2 filas con 11-14 cuias (39,2 dp con 6 por fila; 33,6 × 94,1 con 7) y con 15-16 las 3 filas bajan a 30,2 dp (02 §5). Sus paneles, y el estrés con Cuia alta (la regla de filas mide con su alto [P], 02 §5), se miden con los tests de U2.

Lectura:
- El borde superior del panel va de 80 a 250,6 dp según el nivel. Lo que se ve del panorama **entre el HUD y el panel** puede ser solo una franja de 74 dp: en el nivel 7 con 780 dp de alto, de y 88 a 162,1. Por eso el horizonte, las palmeras de açaí y el río pardo tienen que caer dentro de esa franja (3.9). Con 11-15 cuias en 3 filas no queda franja: el panel empieza bajo el HUD.
- Bajo el panel, el muelle llena la banda libre. No queda ninguna franja vacía: cielo, río, muelle, follaje y Capi la ocupan (VA-02).

### 3.4 Capas, de atrás adelante

| # | Capa | Ancla y tamaño | Interactiva |
|---|---|---|---|
| 0 | Panorama (cielo, orilla lejana, río) | Arriba, a sangre; 360 × 540 dp (`bg_estuario_panorama`) | No |
| 1 | Ambiente lejano: bruma, destellos del río, garza | Sobre el panorama | No |
| 2 | Muelle (tablas, viga frontal, postes) | Abajo, a sangre; 360 × 360 dp; borde superior de la viga frontal en `H − 288`, porque el 20 % superior del PNG es transparente (0,2 × 360 = 72 dp) (`bg_estuario_muelle`) | No |
| 3 | Follaje de esquina: arriba, detrás del HUD; abajo, detrás de la bandeja | 3.8 | No |
| 4 | Sombra proyectada del panel, relleno (por código) y marco (procedural o PNG solo de marco) | 3.2 y 4.6 | No |
| 5 | Repisas y sombras de contacto | Una por fila (4.5) | No |
| 6 | Cuias (vidrio trasero, líquido, vidrio delantero, marcas) y chorros. Mientras vierte, el cuerpo del origen y su chorro pasan a la capa 11 (D-006). El agua, las frutas y los anillos de la intro (D-010) van en un Canvas anidado propio delante de las cuias, y la rama y la tropa, en su propia capa (`TroopLayer`); todo sin capturar toques | `CuiaView`, `PourStream`, `IntroAnimation`, `TroopLayer` | Sí, ranuras |
| 7 | FX: salpicaduras, burbujas, destellos de sellado | Canvas anidado propio (8) | No |
| 8 | Capi y accesorios del muelle | 3.7 | No |
| 9 | Velo de la barra de estado, HUD y bandeja | 3.5 y 3.6 | Sí |
| 10 | Polen, en un Canvas anidado propio | Fuera del rectángulo del panel | No |
| 11 | Capa de vertido (`PourFront`): mientras vierte, el cuerpo del origen y su chorro se dibujan aquí, delante de las vecinas, del marco del panel, del HUD y del polen (D-006; 6.4). En reposo está vacía | Alineada con el tablero (sigue a `ShiftToClear`) | No |
| 12 | Superposiciones: atenuado, modal Aguas estancadas, victoria (panel, cinta, botón) y estallido | 5.3, 5.5 y 6 | Sí, botones |

Todas las capas no interactivas llevan `raycastTarget = false` (V7). La única captura de toques de fondo sigue siendo `BackgroundCatcher` (C7).

### 3.5 HUD superior (07 §7.4: posiciones del GDD)

La fila sigue el reparto de 07 §7.4 (360 dp de ancho; x en dp). La versión anterior de esta guía centraba la insignia y no dejaba sitio a Pausa ni a Semillas.

| Zona de 07 §7.4 | x | Contenido |
|---|---|---|
| Izquierda | 8-56 | Pausa, 48 × 48 (fuera de v0: ranura reservada, no se dibuja) |
| Centro-izquierda | 56-192 | **Insignia de nivel**, 136 dp como máximo |
| Centro-derecha | 196-288 | Reserva de 92 dp para la píldora de Semillas (fuera de v0): icono de 20 dp y cifra de 16 dp, «12.345» mide 52 dp en Nunito ExtraBold |
| Derecha | 292-352 | Reiniciar, área táctil de 60 × 48. La píldora de Canoas (desde el 31) no cabe: pregunta C15 |

En v0 solo se dibujan la insignia y Reiniciar, pero en estas posiciones: así nada se mueve cuando lleguen Pausa y Semillas.

| Elemento | Posición y tamaño | Diseño |
|---|---|---|
| Velo de estado | y 0 → `safeTop + 40`; `scrim` con alfa 0,35 → 0 | Que los iconos blancos del sistema se lean sobre el cielo claro |
| **Insignia de nivel** | x 56-192, en `hudY + 2`. Placa de 120 × 44 y dos hojas de 8 dp que asoman por los extremos: **136 × 52 es el máximo del conjunto** y la placa no crece | Placa de `madera_oscura` con veta, bisel superior de 2 dp (`madera_luz`, alfa 0,7 [horno]), ranura tallada a 4 dp del borde y sombra (y + 3 dp, desenfoque 4, alfa 0,35). Texto «Nivel {n}» (glosario `level_n`): Baloo 2 ExtraBold, `crema`, contorno de 2,5 dp `madera_contorno`. Futuro: chip de dificultad debajo (07 §7.7) |
| Texto de la insignia | Caja de 106 dp (la placa menos 7 dp por lado) | Tamaño = el menor entre 26 dp × escala de texto del sistema y el mayor que quepa en la caja con el contorno; **mínimo 20 dp**. Medido con Baloo 2 ExtraBold: «Nivel 7» ocupa 78,5 dp a 26 (102 dp al 130 %); «Nivel 100», 107,7 dp, baja a 24,4 dp; «Level 100» (EN), a 23,8; «Nivel 1000» cabe a 20 dp. Nunca se recorta (07 §7.8) |
| **Reiniciar** (Volver a la orilla) | Derecha; área táctil de 60 × 48 en x = W − 8 − 60 | **Disco** de 40 dp, familia Madera, con glifo `crema` de 26 dp: flecha circular con ola (07 §7.6). Píldora de 60 × 16 dp superpuesta 8 dp al pie del disco, con «Reiniciar» en Nunito ExtraBold 12, `crema`, sobre `madera_oscura` (13,7:1). Todo cabe en los 48 dp de la fila |

### 3.6 Bandeja inferior

- **v0 (niveles 1-10):** solo existe la ranura de **Deshacer · Marea**. Según 07 §7.4, Cuia extra llega en el 8 y queda fuera de v0 (`docs/dev/prototipo-v0.md`), Pista en el 12 y Barajar en el 30, y antes de su desbloqueo la ranura no existe.
- **Ancho ajustado al contenido** [P]: `16 + n·64 + (n − 1)·8 + 16` dp, con un mínimo de 96 y un máximo de 328. Centrada, de 80 dp de alto, entre `trayTop` y `trayBottom`. Con una ranura mide 96 dp. Así no queda la barra marrón vacía de la captura actual (pregunta C3).
- **Cuerpo:** bandeja «cumbuca» de `madera_oscura` con veta y radio de 24 dp. Lleva un rebaje interior (sombra interior de 4 dp, negro 0,40), un ribete de cuerda de 4 dp en el canto superior (4.7) y sombra proyectada (y + 4, desenfoque 8, negro 0,35).
- **Ranura de Deshacer:** 64 × 72 dp. La cara es un **cuadrado redondeado** (radio 18) de 64 × 64 dp, familia Agua, con un canto de 6 dp. Arriba, el glifo `crema` de 32 dp: ola que retrocede con una flecha curva a la izquierda que ocupa ≥ 40 % del glifo (07 §7.6). Abajo, dentro de la cara, «Deshacer» en Nunito ExtraBold 12, `crema`, contorno de 1,5 dp `agua_contorno`. El texto cae entre el 65 y el 87 % de la cara, donde el contraste es ≥ 4,8:1.
- **Siluetas distintas.** Cada control tiene silueta propia, además de su glifo:

  | Control | Silueta | Familia |
  |---|---|---|
  | Reiniciar | Disco | Madera |
  | Deshacer | Cuadrado redondeado | Agua |
  | Pista · Morpho, desde el 12 (fuera de v0) | **Ala** [P]: cuadrado redondeado con el borde superior derecho en curva de ala de mariposa. No una hoja, que se confundiría con la mecánica Hoja y con Hoja escudo | Azul morpho #2F6FD6 de 07 §7.6 |
  | Cuia extra (8) | Cuadrado redondeado | Madera |
  | Barajar (30) | Cuadrado redondeado | Agua oscura |

  Cuia extra, Barajar y la silueta exacta del ala de Pista se definirán en la fase de detalle (sección 12).

> **No se adopta la «barra inferior con tres botones (Deshacer, Pista, Reiniciar)»** que pedía el encargo de esta tarea. 07 §7.4 y 02 §5 sitúan Reiniciar en la fila superior y prohíben comodines fuera de la bandeja. D-003 mantiene esas posiciones, y la evaluación del 2026-10-07 descartó el HUD del concepto por ese mismo motivo. Ver C1 en la sección 11.

### 3.7 Capi en la pantalla de juego

Está fuera de v0: es la prioridad 8 de la fase de jugabilidad. Se fija aquí su sitio para no tener que rehacer la composición.

- **Lugar:** el muelle, abajo a la izquierda (02 §5: «orilla inferior izquierda»). Nunca centrado abajo ni con un objeto fijo (glosario §3.1).
- **Tamaño y caja:** 64 × 56 dp [P], en x 12-76 y desde `trayTop − 48` hasta `trayTop + 8`. La bandeja tapa 8 dp de las patas, lo que da profundidad.
- **Visibilidad:** solo si `trayTop − panel.bottom ≥ 64`. Si no, no aparece durante el juego (3.3).
- **Guardas:** `raycastTarget = false`, fuera de las ranuras y del chorro. Mira al jugador o al río, **nunca a una cuia** (V7). Nada en él varía con la selección ni con la legalidad (D-002).
- **Animación:** parpadeo cada 3-6 s (120 ms), respiración (escala Y 1,00 ↔ 1,02, periodo 3 s) y un gesto esporádico cada 20-40 s (02 §5).

### 3.8 Follaje y accesorios

| Pieza | Caja (dp) | Reglas |
|---|---|---|
| Esquina superior izquierda | x −20…140, y 0…150 (pivote en la esquina) | Detrás del HUD. Su borde inferior no baja de `min(150, panel.top − 8)`. La parte densa no pasa de x 80 ni de y 96, para no tapar el sol (3.9) |
| Esquina superior derecha | x 220…380, y 0…130 | Ídem. El disco de Reiniciar queda delante. La parte densa no baja de y 65 ni entra a la izquierda de x 284, para no tapar las palmeras. Puede llevar **un racimo de açaí colgado de la fronda** (el icono más reconocible del Estuario), siempre unido a ella, por encima de y 110 y lejos del tablero: nunca un fruto suelto, que parecería un coleccionable |
| Esquinas inferiores | 140 × 140 en cada esquina inferior | Delante del muelle y detrás de la bandeja y de Capi |
| Lianas laterales [P] | Franjas de ≤ 8 dp en x 0-8 y W − 8…W | Solo a lo largo del panel. A partir del nivel 25 la derecha deja sitio a la barra de la Crecida (x 348-356, 07 §7.4) |
| Accesorio del muelle | Poste de amarre con cuerda enrollada: 40 × 48 en x W − 56, apoyado en `trayTop + 4` | Visible con la misma condición que Capi. Nada de cestas (se confundirían con la Canasta), canoas (Canoas) ni frutas (coleccionables) |

**Exclusión, para toda decoración.** Nada a menos de 8 dp del contorno de una cuia. Nada dentro de un área táctil de cuia: ranura × (alto + 32 dp) de 02 §3.3. Nada dentro de un botón. Nada en la trayectoria del chorro (VA-08).

### 3.9 Contenido del panorama por alturas

Se mide en dp sobre el panorama mostrado a 360 dp de ancho (540 dp de alto).

| Franja (dp) | Contenido | Por qué |
|---|---|---|
| 0-88 | Cielo tranquilo (degradado `cielo_alto` → `cielo_medio`) y nubes suaves | Detrás del HUD: poco detalle |
| 70-118 | Coronas de palmeras de açaí en **dos grupos centrales**, en x 125-185 y x 220-280, fuera de la parte densa del follaje de las esquinas (3.8). Sol en x ≈ 108 e y ≈ 103 (radio de unos 28 dp), medio velado por la bruma (luz desde arriba a la izquierda, 07 §1.2) | Asoma en la franja de 74 dp del nivel 7 (y 88-162; D-006) |
| ≈ 118 | Horizonte: orilla lejana (`orilla_lejana`, `palmera_lejana`) con bruma baja (07 §2.1: «alba dorada, bruma baja») | Gesto geográfico único |
| 118-160 | Río pardo ancho y calmo (`rio_reflejo` → `rio_medio`), con el reflejo del sol y de las palmeras | Es lo que se ve en el peor caso de dos filas: tiene que leerse como un río amazónico, no como agua azul |
| 160-260 | Más río, con franjas de sol bajo el disco | Visible en los niveles de una fila |
| 260-540 | Agua abierta, más oscura y casi sin detalle | Queda detrás del panel |

Comprobación: en las capturas `02_board_l7` (780 dp) y `02b_board_l1`, la franja entre el HUD y el panel enseña al menos dos coronas de açaí y río pardo (VA-16).

## 4. Materiales

### 4.1 Vidrio de la cuia (geometría de `CuiaGeometry` tras D-006)

Geometría de 07 §4.1 (D-006): cuia de 2,00 × 5,60 u (≈ 1:2,8); cavidad recta de 1,60 × 4,00 u (0,80 del ancho) en y = 0,60-4,60; pie 0,30, base 0,30, hombro 0,40 (4,60-5,00), cuello 0,35 (5,00-5,35) y borde de fibra 0,25 u (5,35-5,60); labio en y = 5,35 u.

Las capas se hornean en `CuiaBack` y `CuiaFront`, y la GPU compone `CuiaFront` sobre el líquido en **lineal** (2.5). Por eso `CuiaFront` no lleva brillos blancos con alfa sobre la cavidad ni sobre el líquido: en lineal, un blanco al 0,07 ya sube Açaí 12 puntos de L*, y al 0,05 sube Huito casi 15. Dentro de la cavidad, los brillos los pinta la capa del líquido (4.2) o la de la cavidad (4.3) como color opaco precalculado, con la misma geometría, de modo que la franja se lee continua.

| Capa | Parámetros |
|---|---|
| Contorno exterior | Ancho 0,07 u (mínimo 1,2 dp), `vidrio_borde` alfa 0,90 (8,6-12,0:1 frente al panel) |
| Fresnel interior | Banda de 0,14 u hacia dentro desde el contorno, alfa 0,45 → 0 (smoothstep). Solo sobre la pared de vidrio, no sobre la cavidad |
| Tinte del cuerpo | #CFEFE6 alfa 0,10 en paredes, hombro y cuello (equivale a `glass_tint` de 07 §4.6) |
| Franja especular principal | Geometría fija de 07 §4.2: de (0,25 u; 4,60 u) a (0,55 u; 1,00 u), ancho 0,18 u (como mucho x 0,16-0,64 u). Perfil gaussiano (σ = 0,045 u). Pico de alfa 0,55 sobre el vidrio fuera de la cavidad (cuello y hombro). Dentro de la cavidad: 4.2 y 4.3. Nunca cruza x = 1,0 u, el centro de las dosis |
| Franja secundaria | Paralela, desplazada +0,16 u en x, ancho 0,05 u (como mucho x 0,39-0,74 u), alfa 0,30 sobre el vidrio. En cada punto no coincide con la principal (quedan 0,045 u entre ellas) |
| Punto especular del hombro | Centro en (1,55 u; techo de la cavidad + 0,22 u), radio 0,16 u, alfa 0,70 → 0 |
| Cáustica de la base | Media luna en y 0,34-0,52 u, x 0,45-1,55 u, blanco alfa 0,30 |
| Base gruesa (0,30-0,60 u) | #0E1A16 alfa 0,40 y una línea de luz de 1 dp en y = 0,32 u (alfa 0,35) |
| Pared de la cavidad | Línea de 1 dp blanca, alfa 0,22, en el borde interior (línea de ≤ 2 dp) |
| Marcas de dosis | Las de 07 §4.1, dibujadas **encima** de todos los brillos: ningún brillo tapa una marca (V4) |
| Selección | Brillo del vidrio +10 % (02 §4.1) con un segundo sprite, `GlassSelect`, que repite franjas y punto a 0,10 veces el alfa de `CuiaFront`, o con un material. `Image.color` no sirve: sus canales llegan como máximo a 1. Solo en el origen (D-002) |
| Sombra de contacto | Elipse de 1,90 × 0,28 u centrada en el pie (y = −0,02 u), negro → 0. Se fija por su **resultado: ΔL* ≥ 15 en el centro** frente a `repisa_sup` sin sombra. Alfa de partida 0,55, que da ΔL* 18,8 en lineal (el 0,45 anterior daba unos 16 y la cuia apenas «apoyaba»). Con la cuia elevada, la sombra se queda en la repisa: 1,50 × 0,22 u y ΔL* de 6 a 8 (alfa de partida 0,25: ΔL* 7,4) |

**Resolución.** Se hornea a `ceil(u_dp × scaleFactor)` px/u para cada tamaño de cuia en pantalla. En el SM-S938B (3 px/dp) son 72 px/u con u = 24 dp y 96 px/u con u = 32 dp (cuias de 64 dp, T ≤ 3, D-004); en pantallas de 4 px/dp, hasta 128 px/u (`ProceduralArt.MaxCuiaPixelsPerU`). El aro y la tapa del sellado se generan para u = 32 a 128 px/u. Coste: unos 0,7 MB por textura de la cuia de 4 a 128 px/u (256 × 717 px con D-006, RGBA32; 1 MB la Cuia alta de 6), con tres texturas por tamaño (vidrio trasero, delantero y marcas) y 2 o 3 tamaños por nivel [P]. Sin fresnel por píxel en tiempo real (08 §5.4).

### 4.2 Líquido

| Parámetro | Valor |
|---|---|
| Color | HEX canónico en el centro de cada dosis (2.1) |
| Degradado por dosis | +4 % de L (OKLab) arriba y −4 % abajo, con el centro exacto. Refuerza la unidad «una dosis» (1 dosis = 1 u) |
| **Franjas sobre el líquido** | Continuación de las franjas de 4.1, pintada por la capa del líquido como **color opaco precalculado por tinte** (el tinte con +ΔL* en CIELAB, igual que `Palette.PatternMark`); el alfa solo da forma al borde gaussiano. Principal: ΔL* +12 en los tintes de L* < 40 (Açaí #6A2C91 → #8A4AB1, ΔE00 10,2; Camu camu #9E1B22 → #C3403C, ΔE00 10,6) y +10 en el resto; Guanábana, sin franja. Secundaria: la mitad de ΔL* |
| Brillo vertical | Franja en x 0,76-0,88 u en toda la altura del líquido, ΔL* +6 [P], también como color precalculado. Se mueve desde x 0,50-0,72 u para no coincidir con ninguna franja: la secundaria llega a 0,74 u y el parche de VA-05 empieza en 0,92 u |
| **Un brillo por punto** | En cada punto del líquido hay como mucho un brillo: principal, secundaria o vertical. Nunca se apilan (apilados en lineal, Açaí llegaba a L* 71) |
| Oscurecido lateral | 0,20 u en cada pared, negro 20 % (07 §4.2) |
| Cara superior (antes «menisco») | Elipse de 0,40 u de alto (≈ 0,4 de dosis, como Magic Sort; D-006) en la dosis superior, con el aclarado de 07 §3 (regla 4: +20 %; +25-30 % en tintes de L* < 35). Sustituye a la franja de 0,08 u con línea clara de 1 dp y curvatura cóncava de 0,04 u. Aplicada en U3: en `AF_UI_Liquid.shader` es la global `_AFTopFaceU` = 0,40 u (`CuiaGeometry.TopFaceU`; equivale al `_MeniscusH` = 0,10 del quad de 4 u del Shader Graph de 1.0, 08 §5.2). La elipse se centra en la superficie, con el ancho de la cavidad; su mitad trasera sube sobre la superficie. Toda la elipse lleva el color aclarado, con una línea clara de 1 dp (blanco al 35 %) en su borde trasero |
| Onda tras verter | A₀ = 0,03 u, decae con exp(−t / 0,25 s) (08 §5.2); longitud de onda 1,30 u y fase 2,5 u/s. En reposo la superficie está quieta |
| Burbujas | 5-10 de 0,06-0,10 u, blanco alfa 0,40 con borde de 1 px. Suben a 1,5 ± 0,5 u/s, solo durante el vertido y 1 s después (07 §4.2) |
| Patrón | 15 % siempre en v0; 20 % con Patrones de fruta; 35 % en Alto contraste (07 §3). Las marcas se dibujan **encima** de las franjas y siguen visibles en ellas (VA-05b) |
| Sellado | +15 % de luminosidad durante 100 ms (07 §5.3) |

### 4.3 Cavidad

- **Color:** `cavidad` #102228 **opaca** (el tono medio del panel × 0,6), con alfa 1,0 en lugar del 0,62 actual de `ProceduralArt.cs:322` (D-003). Tiene la viñeta lateral de 4.1 y no deja ver el panel a través, pero comparte su tono: el vidrio parece transparente sobre él.
- **Brillos en la cavidad vacía:** las franjas de 4.1 continúan como color opaco, `cavidad` + 8 L* (#213339) [P].
- **Por encima de la cavidad:** solo el líquido, las marcas y los brillos.

### 4.4 Madera tallada (marco del panel, bandeja, placa y botones de madera)

Todos los alfas de esta tabla son [horno] (2.5).

| Parámetro | Valor |
|---|---|
| Base | Token de la pieza: `marco` (marco del panel), `madera_oscura` (bandeja, placa y modales) o familia Madera (botones) |
| Veta | Líneas horizontales con periodo aleatorio de 7-11 dp (semilla fija). Ondulación de ±1,5 dp cada 40-80 dp. Multiplicar 0,86-0,90 |
| Nudos | Como mucho 1 cada 160 dp de largo: elipse de 10 × 5 dp, multiplicar 0,80 |
| Bisel superior | 2 dp de `madera_luz`, `marco_bisel` o `repisa_canto`, alfa 0,70 |
| Bisel inferior | 2 dp de `ranura`, alfa 0,60 |
| Ranura tallada (solo marcos) | 1,5 dp oscura (`ranura`, alfa 0,55) y debajo 1 dp clara (`madera_luz`, alfa 0,35), a 4 dp del borde |
| Barniz | Franja difusa en el 30 % superior, blanco alfa 0,08 |
| Oclusión | 6 dp de negro 0,30 → 0 donde la madera toca otra superficie |
| Tallas | Solo hojas lisas en las esquinas (≤ 16 × 16 dp). Nada de patrones geométricos ni motivos étnicos (07 §8.1) |

### 4.5 Repisa (una por fila)

| Parte | Medida | Color |
|---|---|---|
| Largo | De `panel.innerLeft + 6` a `panel.innerRight − 6` | — |
| Cara superior | y = pie − 3 … pie + 3 (6 dp) | `repisa_sup` (L* 64) con veta suave. Tope de L* ≤ 65 también con la veta y el barniz (VA-04b) |
| Bisel | 1,5 dp en pie + 3 | `repisa_canto`, alfa 0,80 |
| Frente | y = pie + 3 … pie + 15 (12 dp) | Degradado `repisa_frente` → `repisa_frente_inf` (L* 46 → 37), veta 4.4 |
| Canto inferior | 2 dp | `ranura`, alfa 0,60 |
| Extremos | Radio de 4 dp y una hoja tallada de 10 dp | — |
| Sombra sobre el panel | 6 dp bajo el frente, negro 0,35 → 0 | — |

Entra en el paso de fila de 02 §5 sin cambiarlo. La repisa de una fila termina en pie + 15 y la cuia elevada de la fila siguiente empieza en pie + 24: quedan 9 dp libres.

La repisa anterior (#DDAA6E, L* 73, S 0,50) era más clara que Açaí, Camu camu, Aguaje y Bacaba, y casi tan saturada como Copoazú: en una captura en gris, sus franjas competían con las cuias. La nueva baja 9 puntos de L* y conserva la separación con el pie de fibra gracias a la sombra de contacto.

### 4.6 Panel del tablero («panel oscuro y quieto»)

| Parte | Valor |
|---|---|
| Relleno | Degradado vertical `panel_alto` → `panel_bajo`, **sin textura ni ruido**. Se dibuja **por código**: degradado por vértices o una textura RGBA32 pequeña; nunca un PNG generado por IA ni ASTC, que en degradados oscuros dan ruido y bandas. Desviación típica de L* ≤ 3 y C* ≤ 16 (VA-04) |
| Charco de luz | Mancha radial suave arriba a la izquierda: centro en el 15 % del ancho y el 10 % del alto del panel, radio del 45 % del ancho, tono `cielo_horizonte`. Se fija por su resultado: **+3 a +4 L*** en el centro y 0 en el borde (sobre este fondo, eso es un alfa lineal de solo 0,017). Entra en el límite de σ L* ≤ 3 y da al panel la luz de 07 §1.2 |
| Sombra interior | 16 dp desde el marco, negro 0,35 → 0 |
| Marco | 6 dp de `marco` (#7C4E3A, madera media rojiza, L* 38) tallado (4.4), radio exterior de 20 dp e interior de 14 dp. Bisel de 1,5 dp de `marco_bisel` en los cantos superiores, contorno exterior de 1,5 dp de `ranura` y tallas de hoja solo en las 4 esquinas. Con L* 38 frente a un relleno de L* 17-27, el panel se lee como un objeto. Antes, `madera_oscura` (L* 17) sobre un relleno de L* 20-26 formaba una sola masa oscura |
| Sombra proyectada | y + 6 dp, desenfoque 10 dp, negro 0,35, por código. Separa el marco del muelle (ΔE00 5,6 entre `marco` y `muelle_base`) |
| Nada dentro del panel | Salvo repisas, cuias, chorros, FX de juego y, durante la intro, el agua, las frutas y los anillos de Llenar y teñir (D-010; la rama y la tropa quedan fuera del panel) |

**Panel elegido (D-004, 2026-10-07).** El dueño eligió en su teléfono la variante A. La B, «caoba lacada» (#44302C → #372521, cavidad #251A17), queda descartada y se retira del código, de las capturas y de los prompts.

| Variante | Relleno | Cavidad | ΔE00 frente a v0.1 | ΔE00 mínimo frente a los tintes | Contorno del vidrio | Lectura |
|---|---|---|---|---|---|---|
| **Azul petróleo de río** | #234452 → #142C36 | #102228 | 13,4 | 14,1 (Huito) | 8,6-12,0:1 | Agua profunda de río: fría, contrasta con la madera cálida y con la luz dorada de la escena |

Cumple 2.4 y VA-04. La desviación típica de L* (degradado 27 → 17) queda al límite y se mide en la captura del teléfono.

### 4.7 Cuerda y fibra

- **Cuerda** (ribete de la bandeja y poste del muelle): grosor de 4-5 dp. Hebras a 30° con un periodo de 1,2 × el grosor, en tres tonos (`fibra_luz`, `fibra_base` y `fibra_sombra`). Brillo de 1 dp por hebra y sombra de 1,5 dp debajo (alfa 0,35).
- **Borde y pie de la Cuia de vidrio:** fibra de arumã ocre (07 §4.6, `Palette.Fiber` #A8834F). Trenzado en chevrones de 45° con periodo de 0,12 u y los mismos tres tonos relativos.

### 4.8 Hojas

- **Especies de referencia:** fronda de açaí (pinnada y colgante), filodendro, hoja en flecha de orilla e inflorescencia de heliconia. Como mucho un acento rojo por racimo.
- **Excluidas:** palmera de coco, monstera y cualquier imagen de «playa».
- **Relleno:** degradado del tallo a la punta, de `hoja_oscura` a `hoja_media`.
- **Nervios:** nervio central de 1,5 dp (`nervadura`) con una línea clara al lado. Nervios laterales a 30-40° cada 8-12 dp, de 1 dp, alfa 0,25.
- **Luz:** luz de borde de 1,5 dp de `hoja_brillo` en los bordes orientados arriba a la izquierda.
- **Sombra horneada:** (+3, +4) dp, desenfoque 4, negro 0,25.

### 4.9 Cielo, río, muelle y follaje: PNG desde el principio, con un respaldo mínimo

Son piezas orgánicas. `PixelCanvas` solo tiene círculo, cápsula, rectángulo redondeado, arco y triángulo, sin trazados Bézier, degradados ni desenfoque. Con esas primitivas, un follaje de filodendro, unas frondas de açaí o un muelle con veta quedan como clip-art y no llegan al acabado de los líderes. Por eso:
- **Lo definitivo de la fase visual son los PNG** de `docs/art/prompts-chatgpt.md`: `bg_estuario_panorama`, `bg_estuario_muelle` y `foliage_*` (prioridad 1) y `ui_cinta_hoja` (prioridad 2). El dueño los genera en paralelo con F1.
- **El respaldo procedural es mínimo** y solo sirve para no bloquear el desarrollo ni las capturas automáticas. **Nunca se enseña al dueño como propuesta** (VA-16):
  - cielo: degradado de tres paradas (2.2) y sol de 28 dp de radio con halo de 120 dp (`cielo_horizonte`, 0,5 → 0);
  - orilla: banda de `orilla_lejana` con 6-9 siluetas simples de palmera de `palmera_lejana` (tronco de cápsula y corona de 6 cápsulas colgantes), en los dos grupos centrales de 3.9;
  - río: degradado `rio_reflejo` → `rio_medio` → `rio_base` → `rio_profundo` y 4-6 franjas de luz de cápsula (`destello`, alfa 0,25-0,45) bajo el sol;
  - muelle: tablas lisas de 18 a 30 dp con juntas de 2 dp de `ranura`, viga frontal de 14 dp con su borde superior en `H − 288` y dos postes de 16 dp detrás del panel;
  - follaje: sin respaldo; las esquinas quedan vacías hasta que llegue el PNG.
- **Resolución del respaldo:** a la mitad (≤ 540 × 1200 px), escalado: son superficies suaves (8).
- **Caché:** lo procedural que se genere al arrancar (respaldo de escena, maderas, botones, vidrio) se guarda en `Application.persistentDataPath` con una clave de versión (versión del juego + hash de los parámetros) y se regenera solo si cambia [P]. Así no se paga la generación en cada arranque en gama baja (8).

## 5. Kit de UI

### 5.1 Anatomía de un botón con relieve (de atrás adelante)

Todas las capas se hornean en el sprite del botón: sus alfas son [horno] (2.5).

| # | Capa | Valor |
|---|---|---|
| 1 | Sombra proyectada | Misma forma, y + 4 dp, desenfoque 6 dp, negro alfa 0,30 |
| 2 | Canto (grosor) | Misma forma desplazada y + 6 dp, `*_canto` |
| 3 | Contorno | 2 dp de `*_contorno` alrededor de la cara y el canto |
| 4 | Cara | Degradado vertical: 0 % `*_luz`, 25 % `*_base`, 100 % `*_fondo` |
| 5 | Brillo interior | 1,5 dp en el borde superior interno, blanco alfa 0,35 |
| 6 | Lustre | Elipse en el 6-28 % superior de la cara, con 8 dp de margen lateral, blanco alfa 0,25 → 0 |
| 7 | Contenido | Icono o texto centrado al 58 % de la altura de la cara. El texto queda dentro de la franja del **25 al 85 %** (2.4) |

| Estado | Cambio |
|---|---|
| Pulsado | La cara y el contenido bajan 4 dp (el canto visible pasa de 6 a 2 dp), escala 0,97, en 80 ms ease-out. Sombra: offset 2, desenfoque 3 |
| Soltado | Vuelve en 140 ms con easeOutBack (c = 1,5). La acción se dispara al soltar, como hoy |
| Deshabilitado | **Plano**: cara `deshabilitado`, sin canto, sin sombra, sin lustre y sin degradado; contorno de 1,5 dp `deshabilitado_contorno`; glifo `deshabilitado_glifo` (3,79:1) y texto `deshabilitado_texto` (4,85:1). Conserva su silueta y su etiqueta (V5) |

Es la frontera que pregunta P8: degradados, relieve, sombras suaves y contorno de texto no dorado en madera, agua y hoja. Sin oro, gemas ni cintas rojas.

**Botón primario más vivo (pendiente de C14).** Con 4,5:1 sin contar el contorno, la cara de Hoja pasa el 75 % de su alto entre #2F7D32 y #236A29 (L* 39-46) y sale oscura frente a los verdes del género. Si el dueño acepta 3:1 para el texto en negrita de ≥ 19 dp con contorno oscuro de ≥ 2 dp (C14), los CTA de Hoja pasan a la familia **Hoja viva** [P]. El texto de ≤ 16 dp sigue con 4,5:1 y con la familia Hoja.

| Familia | `_luz` | `_base` | `_fondo` | `_canto` | `_contorno` | `crema` en la franja de texto |
|---|---|---|---|---|---|---|
| Hoja viva [P] | #9BE06A | #3E9B35 | #2E8A2F | #17501E | #0F3A15 | 3,33-4,14:1 |

`hoja_base` viva queda a ΔE00 8,8 de Lulo, por encima del mínimo de 5 de 2.4.

### 5.2 Tamaños y radios

| Control | Tamaño (dp) | Radio | Familia |
|---|---|---|---|
| CTA de modal y victoria (Siguiente, Deshacer · Marea) | min(W − 48, 312) × 56 (02 §3.3) | 20 | Hoja (Siguiente) y Agua (Marea) |
| Ranura de la bandeja | 64 × 72; cara de 64 × 64 | 18 | Agua (Deshacer) |
| Reiniciar en el HUD | Disco de 40, área táctil de 60 × 48 | — | Madera |
| Enlace «Volver a la orilla (gratis)» | Texto Nunito Bold 16 subrayado, área de 280 × 48 | — | `madera_base` sobre `panel_claro` (5,1:1) |

### 5.3 Paneles

| Panel | Medidas | Diseño |
|---|---|---|
| **Victoria** | 328 × 148 anclado abajo (`H − safeBottom − 8 − 148`) | Marco de 8 dp de `madera_oscura` tallada, superficie `panel_claro` → `panel_claro_fondo`, lustre del 8 % arriba y radio de 24. Sin grano ni bordes irregulares. La superficie y la sombra van por código; el marco puede ser procedural o el PNG de solo marco (`ui_panel_modal`). La cinta asoma 34 dp por encima (5.5). Siguiente en `panel.top + 52`. `ShiftToClear` debe usar `panel.top − 34` como borde para que la cinta no tape la última fila |
| **Aguas estancadas** | 328 × 232 anclado abajo en v0 (07 §7.5 fija 300 contando el botón de rescate, que llega en el 31; hoy 220) | El mismo marco. Placa de cabecera de `madera_oscura` de 296 × 40 con «No hay más vertidos» (Baloo 2 ExtraBold 22, `crema`). Subtítulo «Aguas estancadas» (Nunito Bold 14, `madera_base`). Franja ilustrada de **agua quieta** de 296 × 20: degradado `rio_base` → `rio_profundo`, 3 reflejos horizontales y 1 hoja flotando, sin animación. CTA «Deshacer · Marea» (Agua) y el enlace al pie |
| **Atenuado** | Pantalla completa | Negro alfa 0,30 (cuias al 70 %, 07 §5.5). Desaturación de la escena (6.10) |

### 5.4 Insignia de nivel

Ver 3.5. **136 × 52 dp es el máximo del conjunto** (placa de 120 × 44 más las dos hojas de 8 dp) y la placa no crece: si el texto no cabe, se reduce hasta 20 dp. Las hojas de los extremos forman parte del sprite y no se escalan.

### 5.5 Cinta de victoria

- **Forma:** hoja grande de heliconia o bananera **verde**, horizontal, de 336 × 64 dp, con extremos en punta. No es una cinta de tela ni es roja (07 §1.3). Es una pieza orgánica: PNG `ui_cinta_hoja` desde el principio, con una hoja simple por código como respaldo (4.9).
- **Franja de texto:** la banda central donde va el texto (unos 256 × 40 dp) es de `hoja_base` → `hoja_fondo` de la familia Hoja (#2F7D32 → #236A29): `crema` da de 4,83 a 6,26:1. Nada de `hoja_clara` (2,29:1) ni de `hoja_media` (4,54:1) detrás del texto.
- **Nervio:** fuera del eje del texto, a lo largo del tercio superior de la hoja; si cruza la franja de texto, va del mismo tono que ella. Las nervaduras laterales no entran en la franja.
- **Luz de borde:** solo en el canto superior (`hoja_brillo`), nunca en la franja de texto.
- **Texto:** «¡Nivel completado!» (`level_complete`, glosario §10) en Baloo 2 ExtraBold 26, `crema`, contorno de 3 dp `hoja_contorno`. Medido con la fuente: 223 dp a 26 dp (229 con el contorno), dentro de los 256 dp útiles. EN «Level complete!» mide 188 dp y PT-BR «Nível concluído!», 190.
- **Escala de texto al 130 %:** «¡Nivel completado!» pasaría a 290 dp y no cabe. Regla: tamaño = el menor entre 26 dp × escala y el mayor que quepa en 250 dp con el contorno (≈ 29 dp en ES); **mínimo 22 dp**. Si un idioma futuro no cabe a 22 dp, el texto pasa a dos líneas y la hoja crece hasta 88 dp de alto. Nunca se recorta (07 §7.8).
- **Copy alternativo:** el texto «¡Genial!» que pedía el encargo no está en el glosario (pregunta C2).

### 5.6 Tipografía

Las fuentes están en `Assets/_Project/Art/Fonts/` (OFL). Hoy se usa `LegacyRuntime.ttf` y hay que sustituirla.

| Uso | Fuente | Tamaño (dp) | Color | Contorno | Sombra |
|---|---|---|---|---|---|
| Cinta de victoria, banner de mundo | Baloo 2 ExtraBold | 26 (banner: 40) | `crema` | 3 dp de `hoja_contorno` (banner: `madera_contorno`) | y + 2 dp, negro 0,35 |
| Insignia de nivel | Baloo 2 ExtraBold | 26 (mínimo 20, 3.5) | `crema` | 2,5 dp de `madera_contorno` | y + 2 dp, negro 0,35 |
| Título de modal (sobre placa) | Baloo 2 ExtraBold | 22 | `crema` | 2 dp de `madera_contorno` | y + 1,5 dp, negro 0,30 |
| Etiqueta de CTA | Baloo 2 Bold | 22 | `crema` | 2 dp del `*_contorno` de la familia | y + 1,5 dp, negro 0,30 |
| Etiqueta de ranura o píldora | Nunito ExtraBold | 12 (mínimo, 07 §7.8) | `crema` | 1,5 dp del contorno de la familia | — |
| Subtítulo de modal | Nunito Bold | 14 | `madera_base` sobre `panel_claro` | — | — |
| Texto corrido | Nunito Regular | 14 | `tinta` | — | — |
| Cifras (Semillas y Canoas, futuras) | Nunito ExtraBold | 16 | `crema` | 2 dp | Dígitos de ancho fijo (`<mspace>` en TMP) |

Reglas:
- Mayúscula inicial, nunca MAYÚSCULAS (07 §7.3).
- Contornos de color sólido oscuro, nunca degradados ni dorados (07 §7.1).
- Ningún texto con alfa (ver `Palette.PatternMark`).
- Escala de texto del sistema hasta el 130 % sin recortes (07 §7.8), con las reglas de reducción de 3.5 y 5.5.

**Implementación con TextMeshPro** (viene con uGUI en Unity 6). El contorno y la sombra son el Outline y el Underlay del material SDF. Lo que sale bien en el Editor puede salir mal en el teléfono por dos motivos:
1. **Margen del SDF.** Un contorno de 3 dp sobre texto de 26 dp es 0,115 em, y la sombra desplazada 2 dp añade 0,077 em. `TMP_FontAsset.CreateFontAsset` usa por defecto un muestreo de 90 pt con un margen de 9 px (0,10 em): el contorno saldría recortado. Margen del atlas: **≥ 0,15 em** en los estilos solo con contorno y **≥ 0,20 em** en los que llevan contorno y sombra (≥ 14 y ≥ 18 px a 90 pt). Se elige el tamaño de muestreo para que el atlas quepa en 2048 × 1024 por fuente.
2. **Variantes de shader eliminadas en el build.** En el shader Distance Field (`TMP_SDF.shader`), `UNDERLAY_ON` es `shader_feature`; en la variante Mobile (`TMP_SDF-Mobile.shader`) lo son `OUTLINE_ON` y `UNDERLAY_ON`. Comprobado en los TMP Essential Resources de `com.unity.ugui@f3ea87521141`. Un material creado en tiempo de ejecución no hace que esas variantes entren en el build de Android: el texto saldría sin sombra (o sin contorno) en el teléfono mientras los tests del Editor pasan.

Por eso:
- Los recursos esenciales de TMP se importan por script.
- Un **método de Editor versionado** (`AF.EditorTools`, junto a `ProjectSetup`) crea los font assets con **atlas estático** (glifos de ES, EN y PT-BR) y **un material por estilo** de contorno y sombra de la tabla. Crear assets con un método de Editor está permitido (CLAUDE.md, regla 3); editarlos a mano, no.
- Esos materiales se referencian desde algo que entra en el build (por ejemplo `Resources/`), para que sus variantes no se eliminen.
- `UI.Text` con `Outline` y `Shadow` **no** es un respaldo equivalente: con 3 dp el contorno se ve escalonado. Solo vale para depurar, nunca para las capturas de VA.
- El contorno y la sombra se verifican en una captura de `dev.apk` en el teléfono (VA-07).

### 5.7 Iconos

- **Estilo de 07 §7.6:** glifo de 3 tonos como máximo, trazo de 2,5 dp, legible a 24 dp.
- **Sobre botones:** en `crema`, con una sombra interior de 1 dp del `*_canto`. Los glifos son **siluetas sólidas**, no líneas finas.
- **Marea:** el icono deja de usar #3BB8D9 (HEX del tinte Agua del Tapajós). Pasa a `crema` sobre la familia Agua (pregunta C11).

## 6. Efectos y animación

Base: 02 §4.1, 07 §5 y `GameConfig`. Todas las duraciones se multiplican por `anim_speed_factor_*` (02 §4). Ninguna animación bloquea el toque (02 §3.2).

| # | Evento | Qué se ve | Duración | Curva |
|---|---|---|---|---|
| 6.1 | **Selección** | El origen sube **16 dp con rebote**: pico de unos 17,6 dp (+10 %) hacia t ≈ 0,58 y asiento en 16. El vidrio sube +10 % de brillo con el sprite `GlassSelect` y la sombra de contacto se encoge (4.1). Solo cambia el origen (D-002) | 100 ms (`anim_lift_ms`; D-006) | easeOutBack (c = 1,70158) |
| 6.2 | Deselección | Baja a su sitio | 100 ms | easeInQuad |
| 6.3 | Rechazo | Sacudida de ±8 dp, 3 ciclos, sin amortiguar, como antes: seno de amplitud constante (D-012, decisión del dueño del 2026-10-09, que sustituye a ±6 dp y 2 ciclos en 120 ms). Sin rojo, sin texto y sin cambiar de color | 240 ms | Seno |
| 6.4 | Traslado e inclinación | Pose de D-006 (medida en Magic Sort): arco de 0,4 u hasta dejar el centro de la boca sobre el eje del destino (±0,1 u), a 0,85 u del borde [P], con el punto más bajo de la boca a 0,30-0,51 u (0,2-0,6 u); llega a 38° y gira alrededor de la boca hasta 92° − 9° × dosis que quedan al terminar cada dosis (65° con 3, frente a los ≈ 66° que midió Magic Sort; ≈ 92° con 0). El cuerpo queda del lado del que viene (en la misma columna, hacia el centro del tablero); si ese lado saliera del lienzo entre 38° y el ángulo final, al otro lado [P]. Escala 1,00. Mientras vierte se dibuja **delante de todo** (vecinas, marco del panel y HUD), sin techo: se retiran `GameScreen.PourCeilingFor`, `PourCavityDp` y `PourCeilingAirDp`. El líquido no rota | 300 ms | easeInOutCubic |
| 6.5 | **Chorro** | Bézier desde el canto interior del lado bajo de la boca (0,34 u del eje, `CuiaView.PourEdgeU`) hasta la superficie, de 0,16 a 0,10 u de grosor. Núcleo del tinte con +10 % de L y una línea especular central de 1 dp (blanco 0,40) con UV a 3 u/s. Arranca al 70 % del traslado. Vierte la racha que cabe (`run`, D-005): 280 ms por dosis + 80 ms de cola | 360 ms con 1 dosis (280 + 80); 920 ms con 3 | Lineal |
| 6.6 | **Salpicadura** [P] | Al tocar la superficie: 4-6 gotas de 0,06-0,10 u del tinte con borde claro, lanzadas a 1,5-2,5 u/s hacia arriba (±35°) con gravedad de 18 u/s². Un anillo elíptico crece de 0,4 a 1,3 u con alfa 0,40 → 0, y empieza la onda de 4.2 | 250 ms las gotas y 300 ms el anillo | Balística / easeOutCubic |
| 6.7 | Retorno | Vuelta directa a la ranura con un rebote de 0,03 u (el de Magic Sort no se copia: no está confirmado; D-006). De la salida al reposo, 870 ms con 1 dosis | 300 ms | easeOutQuad |
| 6.8 | **Sellado** (07 §5.3) | t = 0: líquido +15 % durante 100 ms. t = 0,10 s: la tapa de calabaza (#C9A36B con veta y brillo) cae desde 0,6 u en 150 ms (easeOutQuad), con un mini-rebote de 0,03 u en 100 ms. t = 0,25 s: aro de luz aditivo (escala 0,8 → 1,25, alfa 0,8 → 0 en 200 ms), destello en estrella de 40 × 40 dp (0 → 1,2 → 0 en 250 ms) y 6-8 gotas | 450 ms | Indicadas |
| 6.9 | **Victoria** (07 §5.4) | t = 0,30 s: ola de saltos de 0,15 u (escalón de 40 ms; cada salto 240 ms). t = 0,50 s: **estallido** de ≤ 60 partículas (24 plumas roja y azul de guacamayo, 24 hojas `hoja_media`/`hoja_clara` y 12 pétalos de heliconia) durante 1,2 s, desde los dos lados del panel en un cono de ±25°, con gravedad, giro de 180-540 °/s y aleteo de ±15°. t = 0,80 s: el panel sube en 250 ms (easeOutCubic). t = 0,90 s: la **cinta-hoja** pasa de escala 0,6 a 1,08 y a 1,0 en 300 ms (easeOutBack). t = 1,40 s: **Siguiente** aparece (0,9 → 1,0, 150 ms) y respira (1,00 ↔ 1,03, periodo 1,4 s). Sin auto-avance (`victory_autonext_s` = 0) | 1,4 s hasta el CTA | Indicadas |
| 6.10 | **Bloqueo** (07 §5.5) | t = 0,20 s: superficies quietas; escena −20 % de saturación, ya sea con `_Saturation` en el material de escena o con un velo gris #6B6B6B al 20 %; niebla del 12 % sobre el panel. t = 0,45 s: el modal temático (5.3) sube en 250 ms; cuias al 70 % | 400 ms antes del modal (R15) | easeOutCubic |
| 6.11 | **Llenar y teñir** (02 §4.2; D-010) | *Sustituye desde D-010 (2026-10-08) al agua de inmersión por fila de «Sumergir y teñir», que se retira junto con `ImmersionWater`.* Fases a-d iguales (1,4 s / 0,8 s, saltable). Las cuias no caen: están vacías y secas en su ranura. El río del panorama crece (capa de +12 L* y destellos que se deslizan hacia el panel), desborda el canto superior del marco con una lámina de agua, corre en tiras de 5 dp por el borde interior del marco y por la cara de las repisas, y cae en un chorrito por cuia no vacía (0,16 → 0,10 u) que la llena desde el fondo solo hasta sus dosis; las vacías quedan secas. Después, una fruta por racha cae de la mano de un miquito (o del follaje, en modo dosel) y la racha florece en 100 ms con un anillo. Agua del preset del mundo con los destellos del río (6 × 2 dp). Detalle: 07 §5.1 y §6.4 | 02 §4.2 | Las de 07 §5.1 |
| 6.12 | **Ambiente** | Polen y motas de luz: 8 de 2-4 dp, `destello` alfa 0,3-0,6, aditivas; derivan a 6-12 dp/s alejándose de la luz y viven 6-10 s. Destellos del río: 6 de 6 × 2 dp que titilan (alfa 0 → 0,7 → 0 en 0,8-1,4 s). Bruma: banda que se desplaza 4 dp/s. Follaje: balanceo de ±1,5° en el tallo con periodo de 4-6 s, desfasado. Garza: un vuelo cada 25-45 s que cruza el cielo en 6 s. **Nunca dentro del rectángulo del panel** | Bucles | Seno o lineal |

**Presupuesto de partículas.** Como mucho 60 simultáneas (07 §1.2), contando las ≤ 16 de ambiente. El ambiente se suspende en CELEBRATION, VICTORY_SCREEN y STUCK_MODAL, de modo que el estallido dispone de las 60.

**Guardas de D-002.** Nada del ambiente, los FX ni Capi depende de la selección ni de la legalidad. Las suspensiones dependen solo de la fase.

**Reducir movimiento** (07 §7.8, cuando exista): sin ambiente, elevación sin rebote, sin estallido ni ola de saltos, paneles con fundido de 150 ms. Las capturas de prueba congelan el ambiente con semilla y tiempo fijos.

## 7. Evitar

| Categoría | Qué no aparece | Fuente |
|---|---|---|
| Trade dress de líderes | Gatos magos, caldero, escobas, varitas, portales morados, slime. UI dorada biselada, marcos metálicos, gemas, coronas, cintas rojas. Calavera como dificultad. Castillo o bosque místico. Frasco cilíndrico con labio. HUD de deshacer/pista/+tubo de IEC/Guru. Paleta de 12 colores de IEC. Tipografía de un líder | 07 §1.3; research/09 «Qué NO copiar»; glosario §0.3 |
| Render | **Bloom del 2D Renderer** (UUM-150232), postproceso y desenfoque a pantalla completa. Los brillos son sprites aditivos o shaders de UI. Brillos blancos con alfa apilados sobre el líquido (2.5) | D-003; CLAUDE.md; 4.2 |
| Ruido detrás de las cuias | Texturas, follaje, partículas, reflejos o ilustraciones dentro del panel. Panel translúcido que deje ver la escena. Relleno del panel sacado de un PNG generado o comprimido en ASTC | 4.6; VA-04 |
| Papel y cuaderno | Pergamino, grano de papel, bordes rasgados, espirales, tinta, acuarela que se corre, láminas botánicas, serifas | Decisión del dueño (D-003) |
| Oscuridad general | Velos oscuros globales, noche por defecto, viñetas de más del 25 % en la escena. Verde oliva uniforme como el de v0.1, también **dentro** del panel | Principio 3; VA-03; VA-04c |
| Pistas encubiertas | Brillos, pulsos, flechas, contornos o miradas de fauna hacia una cuia que no sea el origen. Animaciones que dependan de la legalidad | D-002; V7 |
| Confusión de sistemas | Canoas junto al HUD (es el icono de Canoas). Cestas (Canasta). Frutas sueltas junto al tablero (parecen coleccionables); la única fruta de la escena es el racimo de açaí unido a la fronda superior derecha (3.8). Hojas sobre una cuia (mecánica Hoja). Tapas de hoja (07 §4.4) | V7; glosario §4 y §6.5 |
| Cultura | Personas, aldeas identificables, malocas, kené y grafismos étnicos, pictografías, pintura corporal, mitología, ropa o accesorios en la fauna | 07 §8.1; glosario §0.3 |
| Lugar equivocado | Palmeras de coco, playa, mar azul caribeño, montañas, templos, ruinas | 07 §2.1 y §8.1 |
| Color | HEX de tintes en la UI. Rojo **en la UI** fuera de `rojo_aviso` (la heliconia del follaje y las plumas del confeti son escena y FX, no UI). Contornos de texto dorados o con degradado. Texto con alfa. Texto sobre `hoja_clara` u `hoja_media` | 07 §3, regla 6; 07 §7.1; 2.4 |

## 8. Presupuesto de rendimiento

La referencia es la clase A de 08 §5.4 (Mali-G52 o Adreno 610, 3 GB). La clase B es la gama media de 08 §14.1 (Galaxy A2x/A3x, Redmi Note 11-13, 4-6 GB). Esta tabla es el subpresupuesto de la capa visual dentro de los límites de 08 §5.4.

| Recurso | Presupuesto | Cómo se mide |
|---|---|---|
| GPU de escena, panel, HUD y FX | ≤ 6 ms en clase A y ≤ 3 ms en clase B (de los 10 ms de GPU) | Profiler por USB |
| CPU de ambiente y FX | ≤ 1 ms por frame | Profiler |
| Draw calls | Escena, marco, HUD y bandeja ≤ 30, con SetPass ≤ 10 (de 120 y 40). Las cuias con material propio cuentan aparte | Frame Debugger |
| Capas a pantalla completa | ≤ 3: panorama, muelle y atenuado del modal. Follaje: ≤ 4 sprites de ≤ 160 × 160 dp | Inspección |
| Overdraw medio | ≤ 2,5× | Vista de overdraw o RenderDoc |
| Partículas | ≤ 60 simultáneas, ambiente ≤ 16 (6) | Contador de `DevProbe` [P] |
| Brillos aditivos | ≤ 8 a la vez, cada uno ≤ 96 × 96 dp | — |
| Shaders | Sin postproceso ni Bloom. Líquido ≤ 40 ALU y ≤ 2 muestras (08 §5.2). Vidrio horneado | — |
| Texturas pintadas | ASTC 6×6 en fondos y 4×4 en UI, sin mipmaps en UI y atlas de 2048² (08 §5.4). Los degradados oscuros (relleno del panel, interiores de modales) no van en PNG ni en ASTC: por código (4.6) | Build report |
| Texturas procedurales | RGBA32 ≤ 16 MB en total, incluidas las de la cuia a 72-128 px/u (4.1). Respaldos de escena a media resolución (≤ 540 × 1200 px) y generados una sola vez, no por nivel | Memory Profiler |
| Generación procedural al arrancar | ≤ 300 ms de CPU en clase A. Solo se generan piezas geométricas o de material (10.3), sin desenfoques por píxel, y se guardan en caché en `persistentDataPath` con clave de versión (4.9): desde el segundo arranque, solo se leen | Stopwatch y línea de log en builds de desarrollo [P] |
| Atlas de fuentes | Estáticos, Alpha8, ≤ 2048 × 1024 por fuente y 5 fuentes como máximo (5.6) | Memory Profiler |
| Reconstrucción de Canvas | Ambiente y partículas en **Canvas anidados propios**. Con el tablero quieto, `Canvas.BuildBatch` + `SendWillRenderCanvases` ≤ 0,5 ms/frame, y el Canvas del tablero no se reconstruye (comentario de 08 §5.4 en `CuiaView`) | Profiler |
| Frecuencia | 60 fps en juego (08 §5.4). [P]: 30 fps tras 10 s sin toques ni animaciones del tablero (solo ambiente) | — |
| Memoria | Sin crecimiento de más de 10 MB en 30 niveles (08 §14.2) | Memory Profiler |

El teléfono del dueño (SM-S938B) es de gama alta: **no** valida este presupuesto. Las cifras de clase A y B quedan pendientes de un dispositivo de esas clases.

## 9. Criterios de aceptación visuales

Capturas:
- `VisualSnapshotTests`, con las actuales (`02_board_l7`, `03_selected_l7`, `04_pouring_l7`, `06_celebration_l1`, `07_victory_l1` y `08_stuck_l6`) más las nuevas [P] `02b_board_l1`, `09_stress_14`, `10_stress_16` y `11_stress_tall`, y `02d_board_l2`, `02e_board_l4` y `04b_pouring_l1` (D-004);
- en el teléfono, `adb exec-out screencap -p`;
- con el ambiente congelado;
- a 1080 × 2340 (19,5:9) y 1080 × 2400 (9:20), es decir, a 3 px/dp;
- los criterios de color (VA-04 a VA-07) se miden en las capturas **del teléfono** con `dev.apk`: el Editor no reproduce la compresión ni las variantes de shader del build (2.5 y 5.6).

Las máscaras de panel, HUD y bandeja salen de `AF_LAYOUT` o del layout del test.

| ID | Criterio | Medida |
|---|---|---|
| VA-01 | Bloque centrado: la diferencia entre el hueco superior y el inferior de la zona de tablero es ≤ 2 dp en los casos de 3.3 | `BoardLayoutTests` (EditMode) |
| VA-02 | Sin espacio muerto: ningún bloque de 48 × 48 dp fuera del panel, el HUD, la bandeja y el cielo (y < horizonte) tiene desviación típica de L* < 1,0. Toda franja horizontal de ≥ 96 dp contiene un elemento de escena | Script de las capturas L1 y L7 |
| VA-03 | Escena brillante: fuera del panel, el HUD y la bandeja, la media de V (HSV) es ≥ 0,55 y la de S ≥ 0,30, y como mucho el 10 % de los píxeles tiene V < 0,25 | Script |
| VA-04 | Panel quieto: dentro del panel, sin cuias ni repisas y a más de 16 dp del marco, L* medio entre 15 y 30, desviación típica ≤ 3 y C* medio ≤ 16 | Script |
| VA-04b | Repisas discretas: la cara superior tiene L* ≤ 65 y el frente ≤ 46. En la captura en escala de grises, ninguna franja de repisa se lee más clara que las dosis de Aguaje o Bacaba | Script y revisión de la captura en gris |
| VA-04c | Adiós al oliva: el relleno del panel queda a ΔE00 ≥ 12 de las muestras del fondo de v0.1 (2.4, regla 6). VA-03 no lo detecta, porque solo mide fuera del panel | Script sobre `02_board_l7` |
| VA-05 | Tintes exactos: la mediana de un parche de 4 × 4 dp en el centro de cada dosis está a ΔE00 ≤ 3 del HEX | Script sobre `02_board_l7` |
| VA-05b | Brillos medidos: en la banda de la franja especular sobre Açaí y Camu camu, ΔL* ≤ +12 y ΔE00 ≤ 20 frente al HEX, y las marcas del patrón siguen visibles. En ningún punto del líquido se apilan dos brillos. La sombra de contacto baja la cara de la repisa ≥ 15 L* en su centro | Captura del teléfono de un nivel de prueba con esos tintes [P]; test EditMode sobre la geometría de las franjas |
| VA-06 | Contraste compuesto (D-003, P9): tintes del Estuario frente a cavidad ΔL* ≥ 15; Guanábana frente a cavidad vacía ΔL* ≥ 50; contorno frente al panel a 4 dp ≥ 3:1 | Test EditMode de contraste (V4) y muestreo de la captura |
| VA-07 | Todo texto ≥ 4,5:1 frente al relleno de la franja del 25-85 % y frente a la franja de texto de la cinta-hoja (o 3:1 en el texto grande si se aprueba C14); glifos ≥ 3:1. El contorno y la sombra del texto se ven en la captura de `dev.apk` en el teléfono, no solo en el Editor | Test EditMode sobre los pares de tokens de 2.4 y captura del teléfono |
| VA-08 | Decoraciones: `raycastTarget = false`; ningún rectángulo decorativo cruza una ranura × (alto + 32 dp), un botón ni el rectángulo del chorro; controles ≥ 48 dp | Test PlayMode nuevo [P] y `AF_LAYOUT` |
| VA-09 | D-002: entre `02_board_l7` y `03_selected_l7`, ningún píxel cambia fuera de la columna de la ranura seleccionada (tolerancia ΔRGB ≤ 2) | Diferencia de capturas; `SelectionShowsNoTargetsTests` en verde |
| VA-10 | D-005: en `04_pouring_l7` el chorro lleva la racha que mueve el vertido (`run`); `PourChainTests` (encadenado de `single`) en verde | PlayMode |
| VA-11 | Siluetas: Reiniciar es un disco y Deshacer un cuadrado redondeado, y en la captura en gris se distinguen sin leer la etiqueta | Revisión de la captura |
| VA-12 | Estados: Deshacer deshabilitado es plano (desviación típica de L* de la cara ≤ 1, sin canto ni sombra) y conserva la etiqueta legible. Al pulsar, la cara baja 4 dp | Captura del nivel 1 recién cargado y test de UI |
| VA-13 | Victoria a t = 1,40 s: cinta completa y sin texto recortado, también con la escala de texto al 130 % (5.5); Siguiente entero por encima de `safeBottom + 8`; ≤ 60 partículas | `07_victory_l1` y contador |
| VA-14 | Bloqueo: cuias visibles al 70 %; la saturación media de la escena baja un 15-25 % respecto al reposo | `08_stuck_l6` frente a `02_board_l7` |
| VA-15 | Estrés: con 14 cuias (3 filas de 46,9 dp a 780 dp; 2 filas de 33,6 dp a 640 dp; D-006), 16 cuias en 3 filas y Cuia alta, ni el panel, ni el HUD, ni la bandeja ni las cuias se solapan, y Capi se oculta si la banda es < 64 dp | `09`-`11` y test de layout |
| VA-16 | Lista de comprobación del dueño: nada de la sección 7 visible, ninguna canoa junto al HUD, ningún papel o cuaderno; capturas antes y después de los niveles 1 y 7 en el mismo estado (V8). Prueba de 5 s: «¿juego o libro?». Se hace **solo con los PNG orgánicos integrados** (panorama, muelle y follaje), nunca con el respaldo procedural. Entre el HUD y el panel se ven palmeras de açaí y río pardo (3.9) | Revisión del dueño |
| VA-17 | Rendimiento en clase A: p5 de fps ≥ 55 durante 60 s con 16 cuias, vertido y ambiente; presupuestos de 8 | Dispositivo de clase A (pendiente) |
| VA-18 | Regresión: los comandos de verificación de CLAUDE.md en verde (dotnet, EditMode y PlayMode) | Scripts de `scripts/` |
| VA-19 | HUD de 07 §7.4: la insignia ocupa x 56-192 y no pasa de 136 dp; queda libre la reserva de Semillas (x 196-288); «Nivel 100» y la escala al 130 % no recortan texto (3.5) | Test de layout y captura |

## 10. Implementación

### 10.1 Mapa al código (unity-gameplay-dev, con un Editor como máximo)

| Sección de la guía | Archivos |
|---|---|
| 2 Paleta | `Presentation/Palette.cs`: tokens nuevos; los de 07 §7.2 se conservan hasta P10. `Presentation/ColorMath.cs`: aclarado por ΔL* para los brillos opacos (4.2) |
| 2.5 Mezcla | `Presentation/PixelCanvas.cs`: `BlendLinear` [P] para lo horneado que deba igualar a la GPU |
| 3 Composición | `UI/BoardLayout.cs` (ancla centrada y `footMax`), `UI/GameScreen.cs` (capas, panel, repisas, HUD en las posiciones de 3.5, bandeja, Capi, `ShiftToClear`), `Tests/EditMode/BoardLayoutTests.cs` |
| 4 Materiales | `Presentation/ProceduralArt.cs`: panel (relleno por código y marco), repisa, madera y botones; vidrio a `ceil(u_dp × scaleFactor)` px/u, sin brillos con alfa sobre la cavidad; cavidad opaca; respaldo mínimo de escena y caché (4.9). `Presentation/CuiaView.cs`: brillos opacos del líquido y de la cavidad, sombra de contacto y `GlassSelect` |
| 5 UI | `UI/UiBuilder.cs` (botón por capas, estados y fuentes TMP), `UI/UiText.cs` (sin textos nuevos sin glosario). Método de Editor nuevo en `Editor/CI/` (AF.EditorTools) que crea los font assets y los materiales de texto (5.6) |
| 6 Efectos | `Presentation/PourStream.cs` (línea especular y salpicadura), `ConfettiLayer.cs` (plumas, hojas y pétalos), `IntroAnimation.cs` (agua de inmersión por fila en lugar de `_river`), `BoardAnimator.cs` (curva de elevación); nuevo `AmbientLayer` en un Canvas anidado |
| 9 Aceptación | `Tests/PlayMode/VisualSnapshotTests.cs` (capturas nuevas y ambiente congelado), test EditMode de contraste (D-003, VA-04c y VA-05b) y test PlayMode de decoraciones |

**Shaders opcionales:**
- Ruta: `.shader` de texto (ShaderLab/HLSL) en `Assets/_Project/` derivados de `UI/Default` (stencil y `UNITY_UI_CLIP_RECT`). Sirven para el líquido, la saturación de la escena y los brillos aditivos.
- Materiales: se crean en tiempo de ejecución con `new Material(shader)`, nunca como `.mat` a mano. Ojo con las palabras clave `shader_feature`: si un shader propio las usa, pasa lo mismo que con TMP (5.6).
- Inclusión en el build: sin editar `ProjectSettings/` a mano, por ejemplo con `Resources.Load<Shader>`. Lo decide unity-gameplay-dev con el tech-lead.

### 10.2 Orden

El orden anterior ponía la escena procedural (F2) antes de los PNG (F7): el dueño habría juzgado clip-art antes de ver el arte pintado, con riesgo alto de otro «no me gusta». Ahora los PNG orgánicos se piden desde el principio y el dueño solo revisa la escena con ellos integrados.

| Paso | Quién | Contenido | Criterio |
|---|---|---|---|
| F0 | Dueño, en paralelo desde ya | Generar los PNG de prioridad 1 con el flujo de la clave de estilo (`prompts-chatgpt.md`, sección 1): primero el panorama, después muelle y follaje | Comprobaciones de la sección 4 de los prompts |
| F1 | Agente | Composición: centrado, panel azul petróleo, repisas y sombras de contacto. **Revisión del dueño:** hecha el 2026-10-07 en el teléfono: eligió el panel azul petróleo (D-004) | VA-01, VA-04, VA-04b, VA-04c, VA-15 |
| F2 | Agente | Respaldo mínimo de escena (4.9), solo para no bloquear. No se enseña al dueño | VA-02 (con el respaldo) |
| F3 | Agente | Vidrio, cavidad opaca y líquido, calibrados con capturas del teléfono | VA-05, VA-05b, VA-06 |
| F4 | Agente | Kit de UI, HUD de 3.5 y fuentes TMP con el método de Editor | VA-07, VA-11, VA-12, VA-19 |
| F5 | Agente | FX y ambiente | VA-13, VA-14, presupuesto de 8 |
| F6 | Agente | Integrar los PNG de F0 (panorama, muelle, follaje y cinta-hoja), guardas y capturas antes y después. **Revisión del dueño** de la escena completa | VA-02, VA-03, VA-08, VA-09, VA-10, VA-16 |
| F7 | Agente, si el dueño lo pide | PNG opcionales de material o UI (maderas, botones, marcos, bandeja, placa) para compararlos con lo procedural. Si el dueño aprueba `capi_idle.png`, valorar adelantar a Capi **estático** en el muelle (3.7) | Mismas capturas, VA-02 a VA-07 de nuevo |

### 10.3 Ranuras de arte

Cada pieza expone un nombre de ranura igual al del PNG de `docs/art/prompts-chatgpt.md`. Si existe `Assets/_Project/Art/Generated/<ranura>.png`, se usa; si no, el procedural. Lo que cambia es cuál de los dos es la base:

| Naturaleza | Ranuras | Base | Alternativa |
|---|---|---|---|
| **Orgánica** | `bg_estuario_panorama`, `bg_estuario_muelle`, `foliage_tl`, `foliage_tr`, `foliage_bl`, `foliage_br`, `ui_cinta_hoja` | **PNG** (prioridad 1 y 2) | Respaldo mínimo por código (4.9), nunca para revisión |
| Geométrica o de material | `ui_btn_hoja`, `ui_btn_agua`, `ui_btn_madera`, `ui_slot_agua`, `ui_disc_madera`, `ui_panel_tablero` (solo marco), `ui_panel_modal` (solo marco), `ui_bandeja`, `ui_placa_nivel`, `wood_dark_tile`, `wood_light_tile` | **Procedural** | PNG opcional (prioridad 3) |
| Siempre por código | Relleno del panel y de los modales, vidrio, líquido, cavidad, repisas, sombras, partículas | Procedural | — |
| Fase de jugabilidad | `capi_idle`, `fruta_*` | PNG | — |

Los ajustes de importación (Sprite, bordes 9-slice, ASTC y mipmaps) los aplica un método o `AssetPostprocessor` de Editor versionado, **nunca** editando el `.meta` a mano (CLAUDE.md, regla 3). El arte final sigue siendo del artista (CLAUDE.md, «Humanos»).

## 11. Conflictos con el canon y preguntas para el dueño

| # | Tema | Situación | Propuesta |
|---|---|---|---|
| C1 | Barra inferior con tres botones (Deshacer, Pista, Reiniciar) | La pedía el encargo de esta tarea, pero contradice 07 §7.4 y 02 §5 (Reiniciar en la fila superior; comodines solo en la bandeja; la ranura de Pista no existe antes del 12) y D-003 («posiciones del HUD sin cambios») | **Se sigue el GDD.** Si el dueño quiere esa barra, hace falta una decisión nueva del registro que edite 07 §7.4 y 02 §5. Riesgo: el HUD de IEC/Guru, que 07 §1.3 prohíbe copiar, es deshacer/pista/+tubo |
| C2 | «¡Genial!» en la cinta | No existe en el glosario §10. El canon es `level_complete` = «¡Nivel completado!» | Usar el canon. Si se quiere «¡Genial!», crear la clave con su ES, EN y PT-BR en el glosario |
| C3 | Ancho de la bandeja | 07 §7.4 dice «ancho 328, centrada» y que, antes del desbloqueo, la ranura no existe y la bandeja se centra | Leer 328 como **máximo** y ajustar el ancho al contenido (96 dp en v0). Confirmar |
| C4 | Capi durante el juego | 07 §6.1 fija 96-140 dp solo en banners y modales; 02 §5 lo sitúa en la orilla inferior izquierda | 64 × 56 dp en el muelle, con las condiciones de 3.7 [P] |
| C5 | Relieve de los botones | Lo cubre P8, todavía abierta | La guía aplica la recomendación de P8 (5.1) |
| C6 | Umbrales de contraste compuesto | Los cubre P9, todavía abierta | Usarlos ya en el test (VA-06) |
| C7 | 07 §4.2 y §7.2 | Brillos del vidrio más intensos (franja con pico de 0,55 frente a «marfil 18 %») y tokens de UI nuevos frente a «solo los 8 tokens» (07 §1.2) | Llevarlo al lote de documentación de P10 junto con el resto de 07 §1-§7 |
| C8 | Tintes oscuros de mundos futuros | Con la cavidad #102228, Huito (141) se queda en ΔL* 9,5 y Cacao (201) en 13,9, por debajo de 15. El Estuario cumple | Antes del mundo 5, decidir entre un borde interior claro de la cavidad o una cavidad más clara por mundo, y medirlo. Sus brillos ya son opacos y por ΔL* (4.2), así que no se disparan |
| C9 | Banda de río de Sumergir y teñir | 07 §5.1 y 07 §7.4 hablan de la «banda de río al fondo del tablero» | Sustituirla por el agua de inmersión por fila (6.11) en P10. *Cerrada por D-010 (2026-10-08): ni banda ni agua de inmersión; el río del panorama llena las cuias (6.11; 07 §5.1, ya editado)* |
| C10 | Brillo del sellado | 02 §4.1 dice «el agua se aclara un 10 %» y 07 §5.3 «+15 % durante 0,10 s» | Usar 07 §5.3 (el capítulo de arte) y unificar en P10 |
| C11 | Icono de Marea | 07 §7.6 lo pinta con #3BB8D9, el HEX del tinte Agua del Tapajós, contra 07 §3 regla 6 (`Palette.IconTurquoise`) | Glifo `crema` sobre la familia Agua (5.7) |
| C12 | Rendimiento | El único teléfono de pruebas es de gama alta | Conseguir un dispositivo de clase A o B para VA-17 |
| C13 | `ui_indigo` frente a Huito | El token #24305E de 07 §7.2 (Muy difícil, desde el 60) queda a ΔE00 3,0 del tinte Huito #1F2F6B (desde el 141). Choca con el espíritu de 07 §3, regla 6 | En P10, mover `ui_indigo` (por ejemplo a un índigo más claro o más violeta) y medirlo de nuevo. No afecta a v0 |
| C14 | Contraste del texto grande | D-003 mantiene «texto con contraste ≥ 4,5:1» y 07 §7.8 lo mide sin contar el contorno. Así los CTA de Hoja salen oscuros (5.1). WCAG 2.1 (1.4.3) pide solo 3:1 al texto grande: ≥ 18 pt, o ≥ 14 pt en negrita (≈ 18,7 px CSS, ≈ 19 dp) | Pregunta nueva para el registro de decisiones, junto a P8 y P9 (P11): ¿se aplica 3:1 al texto en negrita de ≥ 19 dp con contorno oscuro de ≥ 2 dp, manteniendo 4,5:1 para el de ≤ 16 dp? Si sí, familia Hoja viva (5.1). Hasta entonces, 4,5:1 |
| C15 | Fila superior llena desde el 31 | 07 §7.4 pone en 344 dp útiles Pausa, «Nivel {n}», Semillas, Canoas y Reiniciar. Con la insignia de 136 dp y la reserva de Semillas de 92 dp (3.5), la píldora de Canoas no cabe | Decidir en P10 o en la fase de detalle: Canoas solo con icono y cifra bajo Reiniciar, Semillas solo en el mapa, o una insignia más estrecha a partir del 31. No afecta a v0 |
| C16 | Tono del panel | El panel de la primera versión de esta guía repetía el oliva de v0.1 (ΔE00 3,9-5,4) | **Resuelta (D-004, 2026-10-07):** variante A, azul petróleo de río. La B se retira del código (`Palette.PanelB`, `PanelVariantB`), de las capturas (`02c_board_l7_panelB`) y de los prompts. |
| C17 | Inclinación del vertido | 02 §4.1 y 07 §5.2 fijaban 38° y el labio a 0,6 u sobre el borde del destino (con D-004, a 0,6-1,6 u). Con 38° el eje del origen cruza la ranura vecina (paso de 3,5 u) a 3,5 / tan 38° ≈ 4,5 u bajo el labio: con dos filas no existe ninguna pose que deje libre a la vecina (en `04_pouring_l7` el origen tapa las 2,5 dosis de arriba de la cuia 6 durante ≤ 400 ms), y el líquido sale de una boca seca 1,25 u por encima de la superficie del origen | **Resuelta (D-004):** se mantienen los 38° con una elevación ≤ 1 u (labio a 0,6-1,6 u del borde del destino) y se acepta que, en dos filas, el origen tape la parte alta de la vecina durante ≤ 400 ms. Se descartan los 75° fijos y la inclinación dinámica. También se acepta el tapado de la cuia del medio en los vertidos cruzados de una fila (captura `04b_pouring_l1`): el dueño respondió «38° está bien» después de ver la captura del teléfono `v2-tel-rafaga-1` (nivel 1, vertido 0 → 2 con la cuia del medio tapada) (D-004, Consecuencias). **Resuelta por D-006 (2026-10-08): pose medida en Magic Sort** (boca sobre el eje del destino a 0,2-0,6 u, 38° al llegar y hasta 92° − 9° × dosis que quedan, delante de todo y sin techo; 6.4). |
| C18 | Ancho máximo de la cuia | 02 §5 fijaba `MaxCuiaWidth` = 48 dp. En el nivel 1 a 780 dp quedaban 196,5 dp de muelle vacío entre el panel y la bandeja (el 25 % de la pantalla) y 3 cuias de 48 dp en ranuras de 112 dp | **Resuelta (D-004):** tope(T) = 64 dp con T ≤ 3, 56 con T = 4-5 y 48 con T ≥ 6 (`BoardLayout.MaxCuiaWidthFor`). En el nivel 1 a 780 dp, el panel pasa a 281 dp de alto (206,5-487,5), con 36 dp de margen superior en una fila [P] (3.2), y la banda libre a 172,5 dp (la estimación anterior de ≈ 140 dp era errónea). En una fila, el techo del vertido es el canto interior del marco [P] (6.4). Cambian 02 §5 y 07 §4.1. Las filas de 11-16 cuias las fija D-006 (2026-10-08): 3 filas de ≤ 5 desde 11 cuias si dan la cuia más ancha, fijas en el nivel; el techo y el margen de 36 dp se retiran (3.2 y 3.3). |
| C19 | Criterio [P] de la familia parda | La ronda 2 propone «como mucho el 40 % de la pantalla en el tono 15-50° con S > 0,2». Medido tras la ronda 2: 45 % en `02_board_l7` y 57 % en `02b_board_l1`. Lo que falta es casi todo muelle (madera y luz cálida, que el dueño eligió) y depende de C18 y de los PNG de F0 | Sigue abierta: mantenerlo como objetivo [P] y medirlo de nuevo con las cuias de D-004 (`02b_board_l1`, `02d_board_l2`) y con el muelle pintado (F6). |

Afirmaciones de la revisión del 2026-10-07 verificadas para esta guía:
- **Anclaje inferior:** confirmado (`BoardLayout.cs`: `float top = footLine - blockH`).
- **Banda de río única de 60 dp:** confirmada (`GameScreen.PlaceRiver`).
- **Cavidad translúcida:** confirmada, alfa 0,62 en `ProceduralArt.cs:322`.
- **Regla de L* de 07 §1.2 imposible:** confirmada. Los L* de los 14 tintes, ordenados, nunca se separan más de 14,3 puntos, menos que los 50 de una ventana de ±25, así que no queda ningún fondo válido.
- **Frutos con aspecto de cítrico en el concepto:** los prompts los describen ahora botánicamente.
- **Cita de IPHAN:** no la ha comprobado nadie (los agentes no navegan) y no se usa.

## 12. Después de lo visual (registrado, sin implementar)

Cuando el dueño acepte las capturas de V8 empieza la **fase de jugabilidad, recursos y detalle de cada elemento**, con el backlog priorizado de `docs/reviews/2026-10-07/evaluacion-claude.md` §3: retirar las firmas heredadas, guardar a mitad de nivel, una instrucción por lección, tarjeta «Fruta nueva», icono de fruta al seleccionar, audio, medición de toques y rechazos, métricas del generador, Capi y el lote de documentación.

Piezas de arte de esa fase que esta guía ya deja preparadas, sin implementarlas:
- `fruta_*.png` para la tarjeta «Fruta nueva»;
- `capi_idle.png` (prioridad 8);
- las siluetas de las ranuras de Cuia extra, Pista (ala, no hoja) y Barajar (3.6);
- Capi estático en el muelle, que puede adelantarse a F7 si el dueño aprueba `capi_idle.png` (10.2). Desde la revisión visual del 2026-10-07 no hay Capi procedural: sin su PNG, no se dibuja;
- en el panel de victoria, una fila de 44 dp para «Semillas +N» (el panel pasa de 118 a ~162 dp) y Capi celebrando fuera del tablero (07 §5.4, t = 0,60 / 0,80 s);
- el detalle por elemento: skin de la cuia, tapa, overlays de mecánicas y banner de mundo.

## 13. Revisión del crítico de arte (2026-10-07): qué se aplicó y qué se matizó

Las 12 observaciones (3 altas, 6 medias y 3 bajas) se verificaron antes de adoptarlas: contra el código (`PixelCanvas.cs`, `ProceduralArt.cs`, `CuiaView.cs`), `ProjectSettings.asset`, el GDD (07 §7.4), los recursos de TMP del paquete `com.unity.ugui@f3ea87521141`, la captura `docs/reviews/2026-10-07/visual/actual-nivel-7.png` y las fuentes de `Assets/_Project/Art/Fonts/`. Los colores se calcularon en CIELAB D65 (ΔE00) y WCAG 2.1, con la mezcla lineal o gamma que corresponda. **Ninguna observación se descarta entera**; las desviaciones son las de la columna «Matices».

| # | Sev. | Observación | Verificación | Dónde se aplica | Matices |
|---|---|---|---|---|---|
| 1 | Alta | El panel repetía el oliva de v0.1 | Confirmada. Muestras de la captura: #333F30, #313C2F, #323D2F; el panel anterior quedaba a ΔE00 3,9-5,4 | 0.1, 2.2, 2.4 (reglas 1, 4 y 6), 4.3, 4.6, VA-04c, F1, C16 | Se añade una variante B (caoba lacada) para la comparación de F1 (retirada con D-004). El contorno del vidrio da 10,0-10,7:1 con alfa 0,90 compuesto en lineal (el crítico daba 9,2-9,9:1). El límite S ≤ 0,35 se sustituye por C* ≤ 16, porque el azul propuesto tiene S 0,56 en HSV con el mismo croma que el oliva |
| 2 | Alta | Alfas pensados en gamma con un proyecto lineal | Confirmada: `m_ActiveColorSpace: 1`; `PixelCanvas.Blend` mezcla valores sRGB. Cifras reproducidas: blanco 0,28 sobre Açaí, L* 64 (lineal) y 51 (gamma); negro 0,45 sobre #DDAA6E, L* 57 y 42 | 2.5, 4.1, 4.2, 4.3, VA-05b | En lugar de bajar el alfa del blanco sobre el líquido (haría falta ≤ 0,07 para Açaí y ≤ 0,05 para Huito, invisible en los tintes claros), los brillos dentro de la cavidad pasan a ser colores opacos precalculados por tinte, como `Palette.PatternMark`. Se cumple el mismo criterio de resultado en cualquier espacio de mezcla |
| 3 | Alta | El arte orgánico procedural no llega al nivel de los líderes | Confirmada: `PixelCanvas` solo tiene círculo, cápsula, rectángulo redondeado, arco y triángulo | 0.4, 4.9, 8, 10.2, 10.3; prompts, secciones 1 y 2 | La cinta-hoja también se trata como orgánica (es una hoja). Las teselas de madera siguen como PNG opcional: la veta por código es geométrica y basta |
| 4 | Media | Botones primarios oscuros por exigir 4,5:1 | Confirmada: al 30 % el contraste es 4,92:1 (no 3,8), y al 20 %, 3,85:1 (3,57 con degradado por vértices). #5DBB3F 2,29:1, #43A23A 3,06:1, #3E9B35 3,33:1 | 2.4 (regla 2), 5.1, C14 | No se adopta 3:1 directamente: D-003 mantiene 4,5:1 entre lo que «no cambia», así que lo decide el dueño (C14). El umbral de WCAG para texto en negrita es 14 pt ≈ 18,7 px, es decir ≈ 19 dp, no 18 |
| 5 | Media | Contorno y sombra de TMP que fallan en el teléfono | Confirmada: `CreateFontAsset(font)` usa 90 pt y margen 9. En `TMP_SDF.shader`, `UNDERLAY_ON` es `shader_feature`; en `TMP_SDF-Mobile.shader` lo son `OUTLINE_ON` y `UNDERLAY_ON` | 5.6, 8, 10.1, VA-07 | En el shader Distance Field completo el contorno no depende de ninguna palabra clave: lo que se pierde ahí es la sombra. Margen más estricto que el propuesto: ≥ 0,20 em en los estilos con sombra |
| 6 | Media | El HUD centrado contradice 07 §7.4 | Confirmada con el texto de 07 §7.4 | 3.1, 3.5, 5.4, VA-19, C15 | Insignia en x 56-192 (136 dp) para dejar 92 dp a Semillas. Canoas sigue sin sitio: se registra como C15 |
| 7 | Media | La cinta-hoja no cumple el contraste ni cabe al 130 % | Confirmada: 2,29:1 sobre `hoja_clara` y 4,54:1 sobre `hoja_media`. Medido con la fuente, «¡Nivel completado!» ocupa 223 dp a 26 (el crítico estimaba 245) y 290 dp al 130 %: sigue sin caber en 256 | 2.4, 5.5, 7, VA-07, VA-13; prompt 3.4 | — |
| 8 | Media | Estilo incoherente entre recursos de ChatGPT | Aceptada (07 §1.1) | Prompts, secciones 1 y 4 | — |
| 9 | Media | Especificaciones de los prompts incoherentes | Confirmada: botones pedidos a 3:1 y llevados a 384 × 192 (2:1); con el 92 % de ocupación quedan unos 5 dp de margen | Prompts 3.5 y 3.8-3.11, y tabla 2; 4.6, 8 | Los botones usan un 9-slice solo horizontal y se muestran a su alto nativo. La misma incoherencia aparecía en la bandeja (512 × 256 estirada a 80 dp, con un borde inferior menor que su radio) y en la placa (184 dp): se corrigen también |
| 10 | Baja | Vidrio horneado por debajo de la densidad del teléfono | Confirmada: `CuiaPixelsPerU = 64`; hacen falta 72 px/u en el SM-S938B | 4.1, 6.1, 8 | — |
| 11 | Baja | Contradicciones de redacción | Confirmadas las tres | 1 (principio 3), 2.2, 2.4, 3.4, 4.5, 7, VA-04b; prompt 3.2 | La viga se unifica en `H − 288`, el valor que da el PNG |
| 12 | Baja | Identidad amazónica floja en la franja visible | Confirmada: en el nivel 7 a 780 dp solo se ven 72 dp de panorama | 3.8, 3.9, 3.6, 10.2, 12; prompts 3.1, 3.3 y 3.16 | El racimo de açaí solo en la esquina superior derecha, unido a la fronda y lejos del tablero |

Todas las cifras de esta guía son de cálculo. Antes de cerrar cada paso se calibran con capturas del teléfono (2.5, 9).

## 14. Ronda 2 de la revisión visual (2026-10-07): capturas del teléfono

Los críticos visuales revisaron las capturas del teléfono de la versión con las secciones 1-13 aplicadas. Sus observaciones se verificaron contra el código antes de aplicarlas. Lo que contradice el canon quedó como pregunta (C17-C19; C17 y C18 se resolvieron con D-004 y C19 sigue abierta) y lo demás se aplicó, con los matices de la tabla. Las cifras «medido» salen de las capturas del Editor a 1080 × 2340 (`VisualSnapshotTests`); falta repetirlas con `dev.apk` en el teléfono (VA-05b).

| Observación | Sev. | Dónde | Qué se hizo | Matices y medidas |
|---|---|---|---|---|
| El río se lee como arena (L* 57 y C* 27, igual que el muelle) | Alta | `ProceduralArt.Scene.cs` | Reflejo de Fresnel del cielo; reflejos verticales de las palmeras (1,5 dp, alfa 0,40, 0,7 × el alto) con la corona; ondas en perspectiva (filas en horizonte + 1,2·i^1,7, de 8 a 40 dp y de 0,75 a 2 dp, línea clara `cielo_horizonte` al 55 % y oscura `rio_profundo` al 30 % 1 dp más abajo); columna de destellos aditivos de 24-32 dp; reflejos de las orillas con corte suave; panorama a 2,25 px/dp | Fresnel 0,85·exp(−t/22) + 0,10 en lugar de 0,70: con 0,70 el agua junto al horizonte medía L* 75-78. Se añaden franjas de viento y racimos en las ondas (repartidas por igual, se leían como marcas de playa), y el reflejo de la orilla lejana baja del 45 al 32 %. Medido en `02_board_l7`: río L* 66 (σ 11,8), L* 80 de 3 a 9 dp bajo el horizonte |
| Agua de inmersión opaca | Alta | `ImmersionWater` | Alfa 0,55 (abajo 0,62), banda superior de 0,35 u a +12 L* y 3 líneas de brillo | El relleno de las cavidades al 70 % es de 07 §5.1 y no cambia |
| Nivel 1: muelle vacío | Alta | — | Ya estaban el follaje inferior de 170 dp, el poste en W − 56 y Capi solo con su PNG | C18, resuelta con D-004: cuias de 64 dp y banda libre de 172,5 dp a 780 dp. La luz moteada del muelle evita bloques planos (VA-02: 0 bloques) |
| Vertido: labio alto, boca seca, tapado de la vecina, chorro en cuña | Alta | `BoardAnimator`, `PourStream`, `StreamGraphic`, `CuiaView` | Elevación ≤ 1 u (labio a 0,6-1,6 u); el centro del labio puede alejarse 0,25, 0,45 o 0,65 u del eje del destino; el origen no entra en la silueta del destino; muestreo de la silueta con los bordes; chorro de 0,16 a 0,10 u (07 §5.2) con remates redondos, control a 0,35 u hacia fuera del labio y final en la superficie (o el fondo) dentro de la boca | 38° se mantienen (C17, confirmado por el dueño en D-004). Sin vecinas en juego, el labio queda a 0,85 u y fuera de la boca del destino; en `04_pouring_l7` (3 → 5) el origen sigue tapando la parte alta de la cuia 6. *Sustituido por D-006 (2026-10-08): pose medida en Magic Sort, sin techo ni margen de 36 dp.* |
| Familia parda del 48-59 % y sin luz que brille | Alta | Panorama, muelle, marco | Brillo aditivo del sol (#FFF6D6, 120 dp, 0,5) y 3 rayos al 0,12 en el cielo; muelle con la cara a L* 67 en el tercio superior (`DockSunlit` #C99A6A), juntas en `ranura` al 95 %, 7 racimos de motas de luz aditivas (#FFE4A8 al 18-25 %, 46-90 dp) y degradado cálido desde arriba a la izquierda en lugar de las bandas diagonales; bisel del marco de 2 dp al 90 % | Medido: L* > 85 fuera del panel, 5,0 % (L7) y 9,8 % (L1); familia parda, 45 % y 57 % (pregunta C19) |
| La victoria no celebra | Media | `GameScreen` | Velo de 0,30 con un hueco bajo los rayos, brillo radial #FFE4A8 (0,5 → 0, 180 dp) y 12 rayos aditivos `destello` al 35 % (260 dp, 8°/s), detrás del tablero; ≤ 12 hojas y pétalos caen hasta el CTA tras el estallido; cinta de 344 × 72 dp con Baloo 2 de 30 (mínimo 22); brillo de 0,25 detrás de cada cuia sellada solo en la victoria | Medido en `07_victory_l1`: la banda del muelle sube 6,1 L* (antes bajaba 17,6) y la pantalla baja 2 L* (antes 24). La cinta asoma 38 dp (5.3 decía 34) |
| Confeti en racimos sobre las cuias; aro gris | Media | `ConfettiLayer`, `CuiaView` | Nacimiento en x = −8 y W + 8, y = panel.top + 40; 600-850 dp/s, 60-75° ± 8°, gravedad 1100 dp/s², arrastre 1,5/s; aro #FFE4A8 con 1 − t² y destello en estrella también en la celebración | Plumas roja y azul en los dos lados (antes cada lado tenía un color). La captura `06_celebration_l1` pasa de 0,6 a 0,9 s para enseñar el estallido en vuelo |
| La cuia elevada toca el marco | Media | `BoardLayout` | `PanelMargin` de 14 a 22 dp; tabla 3.3 recalculada | Queda a 8,5 dp del canto interior (22,5 en una fila desde la revisión de D-004, con 36 dp de margen superior; 3.2). *Sustituido por D-006 (2026-10-08): pose medida en Magic Sort, sin techo ni margen de 36 dp.* |
| Vidrio sin brillo de caramelo | Media | `ProceduralArt.CuiaFront` | Franja blanca de 0,08 u al 85 % en la pared izquierda (arco al 80 % en el hombro), luz de borde de 0,05 u al 50 % en la derecha, cáustica al 45 % | `CuiaPixelsPerUFor` ya horneaba a ceil(u × escala) = 72 px/u en el SM-S938B: esa parte de la observación era de una versión anterior |
| Se ve el respaldo procedural | Media | Follaje | Follaje a 3 px/dp con borde suavizado | La escena sigue siendo el respaldo: no se enseña al dueño como propuesta hasta integrar los PNG de F0 (4.9, VA-16) |
