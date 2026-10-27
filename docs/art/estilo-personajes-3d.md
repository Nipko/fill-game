# Guía de estilo de los personajes: «3D cartoon, juguete brillante»

Fecha: 2026-10-27. La escribe el equipo Spark en la fase 1, el piloto, del encargo `docs/dev/traspaso/spark-personajes-3d.md` (D-058).

**Estado:** piloto entregado y **pendiente de la aprobación del dueño**. Sus decisiones [P] valen hasta que el dueño decida otra cosa.

## 0. Resumen

- **Qué es.** Los personajes tienen el acabado de un **juguete de vinilo lacado**: volumen claro, luz de juego, una capa de barniz con doble brillo, ojos con dos reflejos y un contorno fino del color de cada personaje.
- **Cómo se hace.** Todo se sigue dibujando **por código**, sobre las formas de distancia con signo (SDF) que ya existían (D-052). No hay modelos 3D, renders, IA de imágenes, Shader Graph, paquetes nuevos ni Bloom.
- **Dónde está el sombreado.** En la clase pura `ToyShading`, en `Assets/_Project/Scripts/Presentation/ProceduralArt.Toy.cs`, con sus tests en `Tests/EditMode/ToyShadingTests.cs`. Cada pieza da su normal aparente; `ToyShading` hace el resto.
- **Qué sigue igual.** Siguen igual:
  - la dirección «Selva brillante premium» (D-003): vidrio, madera y luz de juego, sin papel ni tinta;
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
| Pelaje de TiTino (felpa con barniz suave) | 0,32 |
| Cara de TiTino | 0,45 |
| Pelo de Capi | 0,45 |
| Nariz de TiTino, alas y cuerpo de la morpho | 1,0 |
| Piel de Rosa (vinilo mojado, el más brillante) | 0,9 |
| Garras (marfil) | 0 en el hueso, con su propio brillo a lo largo del lomo |

- **En la felpa**, el barniz se calcula con la normal de la **forma**, sin el relieve de los mechones (`Shade(…, glossNormal)`). Así es una mancha limpia y no se rompe en chispas.

### 3.2 Felpa estilizada (TiTino y Capi)

- `ToyShading.PlushClumps` hace **mechones grandes y redondos**: bultos suaves (1 − r²)² en una rejilla al tresbolillo, cada uno con su tamaño, que se suman donde se solapan. No hay pelos sueltos ni vetas.
- Devuelve la altura y la pendiente. La pendiente **inclina la normal** (`Tilt`) y el surco suma un poco de oclusión.

| Personaje | Mechón | Inclinación | Oclusión del surco |
|---|---|---|---|
| TiTino | 16 unidades (unos 10 por cabeza) | 0,14 | 0,14 |
| Capi (pelo corto) | 1/18 del lado | 0,06 | 0,06 |

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

Los tres son **1,25 veces** los de antes [P]: ver §9, pregunta 1.

### 3.4 Contorno

- **Sin la línea de tinta gruesa** (07 §6.1, el «2 px #2B2118 al 80 %», y el «contorno de tinta de Capi»: los sustituye D-058).
- El contorno es **un borde fino del color oscuro de cada personaje** (`ToyTones.Outline`):

| Personaje | Antes | Ahora | Color |
|---|---|---|---|
| TiTino | 2,6 unidades de la celda de 128 | 1,5 unidades | pardo grisáceo muy oscuro |
| Capi | 1,35 dp | 0,8 dp, con un mínimo de 1,6 px en la canoa de 20 dp | pardo oscuro |
| Rosa | 1,35 dp | 0,8 dp | granate |
| Morpho | margen azul profundo | margen de unos 4 px con un filo azul muy oscuro de 1 px | azul |

- **Qué se queda con su tinta:** los objetos, que no son personajes: la rama de embaúba, la hoja que cae, la canoa y sus cuias. También el icono de la morpho en la bandeja, que sigue las reglas de los iconos (07 §7.6: trazo de 2,5 dp).

## 4. Paleta

- **Los colores base no cambian:** los del glosario y la propuesta del personaje (c.4), D-045 y las constantes de cada archivo (`SlothFur`, `CapiFur`, `RosaPink`, `MorphoFlightBlue`…).
- **La saturación extra va en los tonos** (§2.1): +12 % de croma en la luz y +20 % en la sombra, y la luz de borde cálida.
- **La morpho** sigue a ΔE00 ≥ 10 de los 14 tintes con sus constantes de siempre (dirección visual v2 §2.4, regla 1; `MorphoUiTests`). El blanco del barniz va en puntos de ≤ 2 dp, la excepción de la regla.

## 5. Alto contraste [P]

- **Decisión:** Alto contraste **no cambia a los personajes**. Hoy solo cambia el tablero: el patrón del líquido al 35 % y el contorno de las dosis (B9).
- **Por qué no hace falta:** los personajes no llevan información de juego, y el contorno fino ya los separa del fondo.
- **Lo comprueba** `ToyShadingTests.Outline_StaysVisible_OnTheLightJungle_AndOnThePanel`:
  - el contorno de TiTino, Capi, Rosa y la morpho, sobre la selva clara: ≥ 3:1 (el mínimo de los glifos);
  - el material, sobre el panel azul petróleo: ≥ 2:1.
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

## 9. Preguntas abiertas para el dueño

1. **El tamaño de los ojos.** La propuesta del personaje (c.4) pide para TiTino «ojos pequeños con un brillo», y D-058 pide «ojos grandes, con dos reflejos».
   - a) **[P] 1,25 veces, dentro del antifaz.** Se conserva la especie (las bandas oscuras de *Bradypus*) y ya se leen los dos reflejos.
   - b) 1,5 veces, más de dibujo animado, aunque el ojo se come las bandas.
   - c) Los de antes, solo con el iris y los reflejos nuevos.
2. **Alto contraste.**
   - a) **[P] Sin cambios en los personajes** (§5).
   - b) Engrosar el contorno fino al doble con Alto contraste. Pide hornear una segunda variante de cada atlas: el doble de memoria.
3. **La sombra de contacto de TiTino,** que cuelga de su rama.
   - a) **[P] Oclusión en la mano y en la raíz de las garras,** que se cierran sobre la rama, sin cambiar el rig.
   - b) Una sombra suave sobre la rama bajo cada mano, que exige una pieza nueva en el rig (`SlothRig`).
   - c) Nada.
4. **El segundo animal del piloto.**
   - a) **[P] Rosa, la bota rosada.** Sale junto a Capi en el muelle, en el viaje del Mapa, en el banner de mundo, en la pausa y en Saltar nivel, así que antes y después se ven en las mismas escenas. Es piel lisa sin pelo, la mejor prueba del vinilo lacado, que completa la felpa de TiTino y Capi y el esmalte de la morpho.
   - b) La piraña. Sale en un solo sitio, la mecánica.
   - c) Un miquito, como el mono ardilla de la intro. Comparte el dibujo con las demás especies de miquitos, así que el cambio entraría en todas a la vez y conviene dejarlo para la fase 2.
5. **El icono de la morpho en la bandeja.**
   - a) **[P] Se queda como icono** (07 §7.6), con su contorno de 2,5 dp. El rediseño va en la mariposa que vuela.
   - b) Pasarlo también a juguete, en la fase 2, con los demás iconos.
6. **El bufeo del Mapa** (`MapBufeo`, la silueta de Rosa con otra paleta).
   - a) **[P] Pasa en la fase 2** con la fauna que falta.
   - b) Ya.

## 10. Capturas del piloto

En `docs/art/personajes-3d/piloto/`: `antes/` y `despues/`, con su `README.md`. Las genera `AF.Tests.PlayMode.Personajes3dSnapshotTests`, una prueba explícita que necesita gráficos (360 dp, 1080 × 2340 px, al 100 %, en es-419). La lámina de esta guía la genera `AF.Tests.EditMode.ToyStyleSheetTests`.
