# Guía de estilo de los personajes: «3D cartoon, juguete brillante»

Fecha: 2026-10-27. La escribe el equipo Spark en la fase 1, el piloto, del encargo `docs/dev/traspaso/spark-personajes-3d.md` (D-058).

**Estado:** el dueño aprobó el piloto el 2026-10-27 con todas las opciones recomendadas (P3D-1 a P3D-10); las [P] de §9 pasan a decididas. La fase 2 (la fauna que falta, el delfín con su forma nueva y los 16 avatares) está en §11.

## 0. Resumen

- **Qué es.** Los personajes tienen el acabado de un **juguete de vinilo lacado**: volumen claro, luz de juego, una capa de barniz con doble brillo, ojos con dos reflejos y un contorno fino del color de cada personaje.
- **Cómo se hace.** Todo se sigue dibujando **por código**, sobre las formas de distancia con signo (SDF) que ya existían (D-052). No hay modelos 3D, renders, IA de imágenes, Shader Graph, paquetes nuevos ni Bloom.
- **Dónde está el sombreado.** En la clase pura `ToyShading`, en `Assets/_Project/Scripts/Presentation/ProceduralArt.Toy.cs`, con sus tests en `Tests/EditMode/ToyShadingTests.cs`. Cada pieza da su normal aparente; `ToyShading` hace el resto.
- **Qué sigue igual.** Siguen igual:
  - la dirección «Selva brillante premium» (D-003): vidrio, madera y luz de juego, sin papel ni acuarela;
  - las siluetas, los tamaños, los pivotes y las celdas de los atlas;
  - el rig y sus poses, las APIs públicas de arte y los `ArtSlots`. El PNG del artista, si existe, sigue mandando.

![Lámina de referencia generada por ToyShading](personajes-3d/p3d_20_referencia_estilo.png)

**Lámina de referencia**, generada por el código con `AF.Tests.EditMode.ToyStyleSheetTests`, una prueba explícita. Tiene tres filas de esferas, una por material:
1. el pelaje de TiTino sobre la selva clara;
2. el vinilo de Rosa sobre el panel azul petróleo;
3. el esmalte de la morpho.

En cada fila, las capas se suman de izquierda a derecha:
1. los tres tonos;
2. el rebote frío y la luz de borde;
3. la oclusión ambiental y la sombra de contacto;
4. el barniz con su doble brillo;
5. la felpa.

Abajo están los ojos de juguete con el iris de TiTino, de Capi y de Rosa. A su lado, los cuatro tonos (sombra, medio, luz y contorno) de TiTino, Capi, Rosa y la morpho.

## 1. Luz

Una sola luz para todos los personajes, igual en todas las pantallas:

| Luz | Dirección | Valor en el código |
|---|---|---|
| **Principal** | arriba a la izquierda y algo de frente, la luz de juego de D-003 (35° sobre la horizontal, como el vidrio) | `ToyShading.KeyLight` = (−0,48; 0,66; 0,58) normalizada |
| **Relleno** | luz envolvente: la sombra nunca llega al negro | `Wrap` = 0,3 en `Diffuse` |
| **Rebote frío** | desde abajo, el río y la selva, solo en el lado de la sombra | `BounceCool` #8DB6C4, hasta el 20 % |
| **Borde (rim)** | cálida, desde atrás arriba a la derecha; aparece en los cantos (fresnel³) | `RimFrom` (0,72; 0,69), `RimWarm` #FFE3B3, hasta el 55 % |

Las normales van en el espacio de la vista: x a la derecha, y arriba y z hacia quien mira. Las piezas que giran con el rig (las extremidades de TiTino) usan `FacingLight`: un cilindro con el centro hacia la luz, así que la luz no tiene lado y se leen igual en cualquier pose.

## 2. Sombreado

### 2.1 Volumen en tres tonos

- **Los tonos de cada material** se calculan **una vez** con `ToyShading.Tones(color, contraste)`, nunca por píxel. Se calculan en OKLab:
  - **sombra:** el 66 % de la luminosidad, con un 20 % más de croma y un 14 % hacia el frío (#3A3D6E);
  - **medio:** el color base;
  - **luz:** el 116 % de la luminosidad, con un 12 % más de croma y un 10 % hacia el cálido (#FFE9C2);
  - **contorno:** el 42 % de la luminosidad, con un 25 % más de croma y alfa 0,92.

  Las sombras saturadas y frías, y las luces cálidas, son lo que da el acabado de CGI estilizado frente a un oscurecer y aclarar en gris.
- **`ThreeTone`** pasa de sombra a medio entre una difusa de 0,12 y 0,45, y de medio a luz entre 0,62 y 0,95: un terminador marcado y suave, sin bandas duras.
- **Los parches de color dentro de una pieza** se mezclan en los tonos con `ToyTones.Lerp`: las algas, la panza, la garganta, el hocico o el espéculo. Así la parte de otro color también tiene sus tres tonos.
- **Las normales aparentes** salen de la forma que ya existía:
  - cúpula (`Normal(nx, ny)`) para la cabeza y el torso;
  - cúpula desde el borde (`EdgeDome`, con la normal del gradiente de la SDF) para Capi y su pata;
  - cilindro a lo largo del lomo para Rosa;
  - placa baja para las alas de la morpho.

### 2.2 Oclusión ambiental

`ToyShading.CreaseAO(a, b, k)` es la diferencia normalizada entre `min` y `smin` de dos piezas: la «muesca» que rellena una unión suave. Vale 1 donde las superficies se cruzan y 0 lejos. Oscurece el color hasta un 50 % y apaga el barniz y la luz de borde. En el piloto se aplica en:
- **TiTino:**
  - el pelo alrededor de la máscara de la cara y bajo las bandas de los ojos;
  - el cuello, en lo alto de los dos torsos y bajo la barbilla del retrato;
  - el hombro de cada extremidad y el pliegue del codo;
  - la mano y la raíz de las garras: **las garras se cierran sobre la rama** [P], ver §9, pregunta 3;
  - el surco entre los mechones.
- **Capi:** la unión de la cabeza con el cuerpo (`CapiParts`). Los pliegues del muslo y de la pata eran trazos de tinta y ahora son sombras suaves del color del pelo.
- **Rosa:** la unión de la cabeza con el cuerpo.
- **Morpho:** la sombra que proyecta el ala de abajo sobre la de arriba, y la del cuerpo sobre la raíz de las alas.

### 2.3 Sombra de contacto

`ToyShading.ContactShadow` es un núcleo oscuro con una caída suave en elipse.
- **Capi en el banner:** dos capas sobre la arena, una ancha al 24 % y un núcleo estrecho al 42 % bajo las patas.
- **Capi en la canoa:** una sombra suave donde se hunde en el casco.
- **TiTino** cuelga y no tiene suelo: ver §9, pregunta 3.

## 3. Material «juguete»

### 3.1 La capa de barniz: doble brillo

- **Brillo nítido y pequeño** (`VarnishSharp`): Blinn-Phong de exponente 48 con el canto definido. Es la mancha blanca de juguete.
- **Brillo ancho y suave** (`VarnishBroad`): exponente 6, al 50 %.
- Se suman en modo pantalla, hacia el blanco, con la intensidad `gloss` de cada material:

| Material | `gloss` |
|---|---|
| Pelaje de TiTino (felpa con barniz suave; 0,32 en el piloto, P3D-2) | 0,42 |
| Cara de TiTino (0,45 en el piloto, P3D-2) | 0,72 |
| Pelo de Capi | 0,45 |
| Nariz de TiTino, alas y cuerpo de la morpho | 1,0 |
| Piel de Rosa y del bufeo (vinilo mojado, el más brillante) | 0,9 |
| Pelo de los miquitos / su cara / el óvalo del huapo | 0,42 / 0,62 / 0,85 |
| Escamas de la piraña | 0,8 |
| Cuerpo de la luciérnaga (escarabajo lacado) | 1,0 |
| Avatares: las formas con borde / los parches / las de color plano | 0,6 / 0,45 / 0,3 |
| Garras (marfil) | 0 en el hueso, con su propio brillo a lo largo del lomo |

- **En la felpa**, el barniz se calcula con la normal de la **forma**, sin el relieve de los mechones (`Shade(…, glossNormal)`). Así es una mancha limpia y no se rompe en chispas.

### 3.2 Felpa estilizada (TiTino y Capi)

- `ToyShading.PlushClumps` hace **mechones grandes y redondos**: bultos suaves (1 − r²)² en una rejilla al tresbolillo, cada uno con su tamaño, que se suman donde se solapan. No hay pelos sueltos ni vetas.
- Devuelve la altura y la pendiente. La pendiente **inclina la normal** (`Tilt`) y el surco suma un poco de oclusión.

| Personaje | Mechón | Inclinación | Oclusión del surco |
|---|---|---|---|
| TiTino | 23 unidades (16 en el piloto; P3D-2, más blanda) | 0,085 (0,14) | 0,07 (0,14) |
| Capi (pelo corto) | 1/18 del lado | 0,06 | 0,06 |
| Miquitos | los mechones son piezas: bolas de felpa de 9-11 px alrededor de la cabeza y del cuerpo de los lanudos | — | la oclusión de la pieza de encima (3 px) |

- El borde de mechones de TiTino (`SlothTuft`) se conserva: la silueta es la misma.
- Los mechones finos del dibujo anterior (`SlothStrand`) se retiran, porque eran pelo realista y no felpa.

### 3.3 Ojos (lo que más vende el look)

`DrawToyEye` y `ToyShading.Iris` dibujan el ojo así:
- el **iris en degradado**: más oscuro arriba, por la sombra del párpado, y más claro abajo a la derecha, por la luz que entra;
- la pupila y el anillo oscuro del borde;
- un contorno fino algo más oscuro que el iris;
- **dos reflejos**: el grande arriba a la izquierda, hacia la luz principal (blanco al 97 %), y el pequeño abajo a la derecha (al 80 %).

| Personaje | Iris | Tamaño |
|---|---|---|
| TiTino | pardo cálido #6A4730 | 6,4 unidades (antes 5,1) |
| Capi | #4A2E1C | 0,026 del lado (antes 0,021) |
| Rosa | #3A2230 | 0,0275 del alto (antes 0,022) |

Los tres son **1,25 veces** los de antes (decidido por el dueño el 2026-10-27: §9, pregunta 1). En la fase 2, los miquitos, la piraña, el bufeo y los avatares llevan el mismo ojo (`DrawToyEye` o, en los avatares, el iris en degradado con un segundo reflejo pequeño), con su tamaño de siempre.

### 3.4 Contorno

- **Sin la línea de tinta gruesa** (07 §6.1, el «2 px #2B2118 al 80 %», y el «contorno de tinta de Capi»: los sustituye D-058).
- El contorno es **un borde fino del color oscuro de cada personaje** (`ToyTones.Outline`):

| Personaje | Antes | Ahora | Color |
|---|---|---|---|
| TiTino | 2,6 unidades de la celda de 128 | 1,5 unidades | pardo grisáceo muy oscuro |
| Capi | 1,35 dp | 0,8 dp, con un mínimo de 1,6 px en la canoa de 20 dp | pardo oscuro |
| Rosa | 1,35 dp | 0,8 dp | granate |
| Morpho | margen azul profundo | margen de unos 5-7 px (P3D-3) con un filo azul muy oscuro de 1 px | azul |
| Miquitos | 2 px de tinta al 80 % | 1,3 px de la celda de 128 | el oscuro de cada pieza |
| Cola de los miquitos | 0,03 S de tinta | 0,016 S | el oscuro de la cola |
| Piraña | 3 px de 96 por u | 1,8 px | el oscuro del lomo y de las aletas |
| Bufeo | 1,35 dp de tinta | el de Rosa a su escala, con un mínimo de 1,2 px | gris azulado oscuro |
| Avatares (normal) | la tinta de Capi, máx(1,25; N/96) px | máx(0,9; N/150) px | el oscuro de cada parte |

- **Qué se queda con su tinta:** los objetos, que no son personajes: la rama de embaúba, la hoja que cae, la canoa y sus cuias. También el icono de la morpho en la bandeja, que sigue las reglas de los iconos (07 §7.6: trazo de 2,5 dp).

## 4. Paleta

- **Los colores base no cambian:** los del glosario y la propuesta del personaje (c.4), D-045 y las constantes de cada archivo (`SlothFur`, `CapiFur`, `RosaPink`, `MorphoFlightBlue`…).
- **La saturación extra va en los tonos** (§2.1): +12 % de croma en la luz y +20 % en la sombra, y la luz de borde cálida.
- **La morpho** sigue a ΔE00 ≥ 10 de los 14 tintes con sus constantes de siempre (dirección visual v2 §2.4, regla 1; `MorphoUiTests`). El blanco del barniz va en puntos de ≤ 2 dp, la excepción de la regla.

## 5. Alto contraste [P]

- **Decisión (del dueño, 2026-10-27):** Alto contraste **no cambia a los personajes**. Hoy solo cambia el tablero: el patrón del líquido al 35 % y el contorno de las dosis (B9).
- **Por qué no hace falta:** los personajes no llevan información de juego, y el contorno fino ya los separa del fondo.
- **Lo comprueba** `ToyShadingTests.Outline_StaysVisible_OnTheLightJungle_AndOnThePanel`:
  - el contorno de TiTino, Capi, Rosa y la morpho, sobre la selva clara: ≥ 3:1 (el mínimo de los glifos);
  - el material, sobre el panel azul petróleo: ≥ 2:1;
  - en la fase 2, también el bufeo y el plateado de la piraña, con el alfa real del contorno (0,92).
- **Los avatares** (fase 2) lo comprueba `AvatarArtTests.TheSilhouette_ReadsAt32And24dp_LockedAndUnlocked`, a 32 dp (96 px) y a 24 dp (72 px): la silueta ocupa entre el 12 % y el 80 % del disco; la del bloqueado contrasta con su disco ≥ 1,4:1 (medido: 1,59-2,07:1) y el borde del normal se separa del disco de justo fuera ≥ 1,5:1 (medido: 1,82-4,14:1).
- **Capturas:** las escenas `_ac` del piloto (§10) salen con Alto contraste encendido.

## 6. Noche

- **No hay variante de color de noche:** el cielo del inicio sigue de día (D-050, añadido del 2026-10-26: el dueño no quiso ambiente nocturno).
- **La noche de TiTino es su pose:** `sloth_sleep` con los párpados cerrados y el «Zzz».
- **El acabado es el mismo de día y de noche:** las bandas de los ojos y el párpado comparten `SlothBandColor` y `SlothFaceNormal`, así que el párpado cerrado encaja con la cara sin costura.
- **Captura:** `p3d_02_inicio_noche`.

## 7. Proporciones y silueta

- **Las proporciones son las de siempre.** Las siluetas no cambian para no tocar celdas, pivotes, cajas ni las pruebas de solapes. El aire «achuchado» sale del volumen redondo, no de deformar.
- **Legibilidad a 32 dp (Liga) y a 24 dp (victoria):** `p3d_12_siluetas` muestra a TiTino colgado (48, 32 y 24 dp) y su retrato (32 y 24 dp), la canoa de Capi (32 y 24 dp de alto), Rosa (48 y 32 dp) y la morpho.
  - Sobre los dos fondos se reconocen por la silueta, el antifaz y la mancha clara de la cara.
  - A 24 dp la felpa desaparece, como se busca, y queda el volumen.

## 8. Rendimiento

- **Los atlas tienen los mismos tamaños y la misma memoria:**
  - piezas de TiTino: 1024 × 512 RGBA32 con mipmaps;
  - rama: 1024 × 256;
  - retrato: 168 × 168;
  - Capi: banner y canoa;
  - Rosa: 312 × 198;
  - pata: 144 × 72;
  - morpho: alas y cuerpo con su halo.
- **Todo se hornea una vez,** como antes. No hay asignaciones nuevas por fotograma: el sombreado solo se usa al crear las texturas.
- **Los tonos se calculan una vez por material.** Van en clases anidadas (`SlothToy`, `CapiToy`, `RosaToy` y `MorphoToy`) que se inicializan al pintar por primera vez, porque el orden de los campos estáticos entre archivos parciales no está definido.
- **Los tiempos medidos** están en el `README.md` del piloto (`docs/art/personajes-3d/piloto/`).

## 9. Las preguntas del piloto (decididas por el dueño el 2026-10-27)

El dueño aprobó el piloto «con las opciones recomendadas»: todas las [P] de abajo quedan **decididas por el dueño el 2026-10-27** (P3D-5 a P3D-10 en `spark-estado.md`).

1. **El tamaño de los ojos.** La propuesta del personaje (c.4) pide para TiTino «ojos pequeños con un brillo», y D-058 pide «ojos grandes, con dos reflejos».
   - a) **Decidido por el dueño 2026-10-27: 1,25 veces, dentro del antifaz.** Se conserva la especie (las bandas oscuras de *Bradypus*) y ya se leen los dos reflejos.
   - b) 1,5 veces, más de dibujo animado, aunque el ojo se come las bandas.
   - c) Los de antes, solo con el iris y los reflejos nuevos.
2. **Alto contraste.**
   - a) **Decidido por el dueño 2026-10-27: sin cambios en los personajes** (§5).
   - b) Engrosar el contorno fino al doble con Alto contraste. Pide hornear una segunda variante de cada atlas: el doble de memoria.
3. **La sombra de contacto de TiTino,** que cuelga de su rama.
   - a) **Decidido por el dueño 2026-10-27: oclusión en la mano y en la raíz de las garras,** que se cierran sobre la rama, sin cambiar el rig.
   - b) Una sombra suave sobre la rama bajo cada mano, que exige una pieza nueva en el rig (`SlothRig`).
   - c) Nada.
4. **El segundo animal del piloto.**
   - a) **Decidido por el dueño 2026-10-27: Rosa, la bota rosada.** Sale junto a Capi en el muelle, en el viaje del Mapa, en el banner de mundo, en la pausa y en Saltar nivel, así que antes y después se ven en las mismas escenas. Es piel lisa sin pelo, la mejor prueba del vinilo lacado, que completa la felpa de TiTino y Capi y el esmalte de la morpho.
   - b) La piraña. Sale en un solo sitio, la mecánica.
   - c) Un miquito, como el mono ardilla de la intro. Comparte el dibujo con las demás especies de miquitos, así que el cambio entraría en todas a la vez y conviene dejarlo para la fase 2.
5. **El icono de la morpho en la bandeja.**
   - a) **Decidido por el dueño 2026-10-27: se queda como icono** (07 §7.6), con su contorno de 2,5 dp. El rediseño va en la mariposa que vuela.
   - b) Pasarlo también a juguete, en la fase 2, con los demás iconos.
6. **El bufeo del Mapa** (`MapBufeo`, la silueta de Rosa con otra paleta).
   - a) **Decidido por el dueño 2026-10-27: pasa en la fase 2** con la fauna que falta (hecho, §11).
   - b) Ya.

## 10. Capturas del piloto

En `docs/art/personajes-3d/piloto/`: `antes/` y `despues/`, con su `README.md`. Las genera `AF.Tests.PlayMode.Personajes3dSnapshotTests`, una prueba explícita que necesita gráficos (360 dp, 1080 × 2340 px, al 100 %, en es-419). La lámina de esta guía la genera `AF.Tests.EditMode.ToyStyleSheetTests`.

## 11. Fase 2 (2026-10-27)

Con el piloto aprobado, el mismo lenguaje llega a todos los personajes. Capturas, antes y después, en `docs/art/personajes-3d/fase2/` (su `README.md` dice qué mirar).

### 11.1 Los ajustes del piloto

- **P3D-2, la felpa de TiTino:** mechones de 23 unidades (16), que inclinan la normal 0,085 (0,14) y oscurecen el surco 0,07 (0,14); barniz del pelaje 0,42 (0,32) y de la cara 0,72 (0,45). Se lee como vinilo lacado, no como felpa grumosa.
- **P3D-3:** la panza de Capi con poco contraste (0,4) y algo más clara, para que se lea aunque mire hacia abajo; en la morpho, el margen azul profundo más ancho (de 4,6 a 7 px del canto) y con menos barniz, y el centro claro llega más lejos desde la raíz.
- **P3D-4 a:** el atlas de TiTino tiene su propio presupuesto, `Personajes3dPerfTests.TiTinoAtlasBudgetMs` = 150 ms con `PerfBudget.Ms` (§11.5).

### 11.2 Las piezas de juguete

Para la fauna, que se dibujaba con primitivas (círculos, elipses y cápsulas con su tinta), `ProceduralArt.Toy.cs` añade piezas que hacen todo en una pasada: `ToyEllipse` (una cúpula), `ToyEllipseOn` (un parche que sigue la cúpula de la pieza de debajo: la cara, el hocico, un babero) y `ToyCapsule` (un cilindro con las puntas en cúpula). Cada una pinta el borde fino del color oscuro de la pieza, el relleno con los tres tonos, el rebote, la luz de borde y el barniz y, si se pide, la oclusión que deja sobre lo que ya estaba pintado alrededor (las uniones de la cabeza con las orejas y los mechones).

### 11.3 El delfín de río (Rosa y el bufeo)

El dueño pidió el 2026-10-27 «que tenga mejor forma, está un poco deforme con ese arco»; es la única excepción a «no cambiar siluetas». Mismo sprite (104 × 66 dp), mismas API y `ArtSlots`:
- **el cuerpo,** fusiforme y macizo, sube unos 25° hacia la cabeza con una curvatura suave del lomo (una Bézier casi recta), en lugar del arco;
- **el melón** redondo en la frente, con el cuello del boto marcado detrás;
- **el rostro** largo y fino, algo caído hacia la punta;
- **la quilla dorsal** baja, una cresta a lo largo del lomo, no una aleta alta;
- **la pectoral** ancha bajo el pecho, hacia atrás;
- **la cola horizontal,** con sus dos lóbulos barridos hacia atrás (vista algo desde arriba), dentro del sprite: en el Mapa se ve entero.

**Los anclajes que dependían del arco,** al día: el respiradero (`RosaBlowhole`, de (0,42; 0,765) a (0,30; 0,685)), donde nace el soplo; y el corte con el agua del banner (`RosaCutCenter` de 0,81 a 0,635 y `RosaCutWidth` de 0,225 a 0,435), donde va la espuma. La onda (72 dp, centrada en Rosa) tapa los dos extremos del corte. La boca y el ojo se mueven con la cabeza, igual en Rosa y en el bufeo.

### 11.4 Los avatares

El retrato normal pasa a juguete con el mismo motor de partes (`DrawToyPart`): cada parte con su normal (la cúpula de la elipse, el cilindro de la cápsula, un cojín desde el borde para el polígono), los tonos calculados una vez al crear las tablas (`AvPart.Toy`), el borde fino de su color en lugar de la tinta de Capi (D-058) y, en los ojos, el iris en degradado con un segundo reflejo pequeño. Los parches sin borde (manchas, máscaras) van más planos y siguen a la pieza de debajo. **El bloqueado no cambia:** silueta sólida, sin ojos ni brillos (avatares.md §2.1).

### 11.5 Rendimiento

- **`ShadeCore`** hace la cuenta de `Shade` en floats, con las luces como constantes (`ToyShadingTests` comprueba que son las mismas): `Shade` es ahora un envoltorio.
- **`ToyLut`:** la tabla del sombreado de un material (normales en una rejilla, en bytes, interpoladas). La fauna usa una caché por hilo de 24 tablas de 33² (≈ 78 KB por hilo que pinta; no son texturas). Solo se crea para piezas de más de 1.800 px o si ya está en la caché; la diferencia con la cuenta completa es de menos de 0,01 por canal de media.
- **La memoria de los atlas no cambia** (mismos tamaños y formatos) y no hay asignaciones nuevas por fotograma: la caché de tablas se crea entera la primera vez que se pinta (el atlas de avatares comprueba que pintar celdas no asigna).
- **Las tablas de los avatares:** cada parte tiene la suya (`AvPart.Lut`, 25², ≈ 1,9 KB), creada al pedir la celda del atlas y rellenada al pintarla la primera vez; así el retrato de 192 px cabe en sus 8 ms sin subir el presupuesto.
- Los tiempos medidos, antes y después, en `docs/art/personajes-3d/fase2/README.md`.
