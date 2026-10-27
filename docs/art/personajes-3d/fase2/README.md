# Fase 2 de los personajes «3D cartoon, juguete brillante» (D-058): antes y después

Capturas de la fase 2 del encargo `docs/dev/traspaso/spark-personajes-3d.md` §5, tras la aprobación del piloto por el dueño (2026-10-27, «con las opciones recomendadas»), y de su petición del mismo día sobre el delfín («que tenga mejor forma… con ese arco no se ve tan natural»).
- **Cómo se generan:** con la prueba explícita `AF.Tests.PlayMode.Personajes3dFase2SnapshotTests` (y las tres láminas del piloto, `Personajes3dSnapshotTests.CaptureTiTinoSheet`, `CaptureCharacterSheet` y `CaptureSilhouettes`), a 360 dp (1080 px de ancho), con el texto al 100 % y en es-419. Necesitan gráficos: `unity test <repo> --mode PlayMode --architecture arm64 --filter AF.Tests.PlayMode.Personajes3dFase2SnapshotTests`, sin `-nographics`.
- **Qué hay en cada carpeta:**
  - `antes/`: la rama en `a5144bea`, antes de tocar el arte de la fase 2. Las láminas `p3d_10..12` son el «después» del piloto. La lámina del delfín (`f2_08_delfin`) se pintó con el arte de `a5144bea` (commit `c3a928bb`).
  - `despues/`: el resultado de la fase 2.
- **Alto contraste:** las escenas con `_ac` lo llevan encendido. Por la decisión P3D-6 (guía §5) no cambia a los personajes: solo cambia el tablero.

| Captura | Antes | Después | Qué mirar |
|---|---|---|---|
| Lámina de la fauna | [antes](antes/f2_01_fauna.png) | [después](despues/f2_01_fauna.png) | **Los 11 miquitos y la hembra del saquí**, sentados a 84 dp: cada pieza con volumen en tres tonos y barniz, el borde fino de su color en lugar de la tinta, los mechones de lana como bolas de felpa y los ojos con dos reflejos. **La piraña** (despierta y dormida), lacada y con el ojo de juguete. **La luciérnaga** en vuelo, escarabajo lacado con el brillo de las alas; su farol, igual. **Rosa y el bufeo**, con la forma nueva. |
| Lámina de los avatares | [antes](antes/f2_02_avatares.png) | [después](despues/f2_02_avatares.png) | **Los 16, normales y bloqueados**, a 56 dp y a su tamaño real: 32 dp (la Liga) y 24 dp (la victoria), sobre el panel y sobre la selva clara. **Normal:** busto de juguete brillante, el borde fino coloreado en lugar de la tinta de Capi y dos reflejos en los ojos. **Bloqueado:** la misma silueta sólida, sin ojos ni brillos. El disco, el aro, la chapa y los fondos no cambian. |
| Intro con la tropa (nivel 2, y `_ac`) | [antes](antes/f2_03_intro_tropa.png) | [después](despues/f2_03_intro_tropa.png) | Los monos ardilla en la orilla, a su tamaño de juego. |
| Intro con el churuco (nivel 82) | [antes](antes/f2_04_intro_churuco.png) | [después](despues/f2_04_intro_churuco.png) | El reposo colgado del churuco lanudo: la lana en bolas de felpa. |
| Mapa con el bufeo (max 305, y `_ac`) | [antes](antes/f2_05_mapa_bufeo.png) | [después](despues/f2_05_mapa_bufeo.png) | El bufeo guía la canoa a 22 dp, con la forma nueva. |
| Victoria con avatar (nivel 41, y `_ac`) | [antes](antes/f2_06_victoria_avatar.png) | [después](despues/f2_06_victoria_avatar.png) | El avatar (el tucán) a 24 dp en la línea de la Liga, y TiTino en la franja. |
| Liga (y `_ac`) | [antes](antes/f2_07_liga.png) | [después](despues/f2_07_liga.png) | El avatar de cada fila a 32 dp, el disco neutro y el del jugador. |
| **El delfín de río** | [antes](antes/f2_08_delfin.png) | [después](despues/f2_08_delfin.png) | **La forma nueva** que pidió el dueño: cuerpo fusiforme y macizo sin el arco forzado (una curvatura suave del lomo), el melón redondo, el rostro largo y fino, la quilla dorsal baja, la pectoral ancha y la cola horizontal (dos lóbulos, vista algo desde arriba). **Banner:** con el 74 % del alto fuera del agua, la onda, la espuma sobre el corte y el soplo en el respiradero. **Mapa:** Rosa y el bufeo a 32, 22 y 16 dp, sobre el verde y sobre el agua. |
| Lámina de TiTino: las 10 poses | [antes](antes/p3d_10_lamina_titino.png) | [después](despues/p3d_10_lamina_titino.png) | **P3D-2:** la felpa más blanda (mechones más anchos y más bajos, el surco más claro) y más barniz en el pelaje y en la cara: se lee como vinilo lacado y no como felpa grumosa. |
| Lámina de los personajes en grande | [antes](antes/p3d_11_lamina_personajes.png) | [después](despues/p3d_11_lamina_personajes.png) | **P3D-2** en TiTino a 190 dp y su retrato. **P3D-3:** la panza clara de Capi otra vez visible en el banner, y en la morpho el margen azul profundo más ancho y menos barnizado frente al centro claro. Rosa, con la forma nueva. |
| Siluetas a 48, 32 y 24 dp | [antes](antes/p3d_12_siluetas.png) | [después](despues/p3d_12_siluetas.png) | La legibilidad a los tamaños de la Liga y de la victoria. Rosa se reconoce de perfil por el rostro largo y la cola. |

## Qué entra en la fase 2

- **Los miquitos** (`ProceduralArt.Fauna.cs`): las 11 especies del catálogo `miquitos_v0.json` y la paleta de pareja del saquí. Las piezas del atlas (cabeza, torso, extremidad y párpado), con los rasgos de cada especie: el babero del sauim, la máscara del pichico, los ojos ámbar del mono nocturno, el óvalo rojo y el pelo largo del huapo, la cría a la espalda. El bigote blanco del pichico ya no tenía tinta y sigue igual. La cola es una tira por código (`BezierStripGraphic`), así que solo cambia su borde: fino y del color de la cola, no de tinta.
- **La piraña** (`ProceduralArt.Pirana.cs`): el pez, despierto y dormido. La cueva y el bocado son objetos y no cambian.
- **La luciérnaga** (`ProceduralArt.Firefly.cs`): el cuerpo y las alas del vuelo. El farol y su brillo de juego no cambian; el glifo de la bandeja sigue siendo un icono (como el de la morpho, P3D-9).
- **El delfín de río:** Rosa (`ProceduralArt.Mascots.cs`) y el bufeo del Mapa (`ProceduralArt.Flags.cs`), que comparten la silueta. El bufeo pasa a juguete (P3D-10) y los dos cambian de forma por la petición del dueño (la única excepción a «no cambiar siluetas»; mismos tamaños de sprite, API y `ArtSlots`).
- **Los 16 avatares** (`ProceduralArt.Avatars.cs`): el retrato normal, de juguete. El bloqueado, el disco neutro, el aro, la chapa y los fondos no cambian.
- **Los ajustes del piloto aprobados:** P3D-2 (la felpa de TiTino), P3D-3 (la panza de Capi y la morpho) y P3D-4 a (el presupuesto del atlas de TiTino).

**Qué se queda fuera, y por qué:**
- las mecánicas que son objetos (Hoja, Liana, Barro…), la cueva de la piraña y su bocado;
- los glifos de los chips (la rana y el jaguar) y los iconos de la chapa de los avatares: son iconos, no retratos;
- el icono de la morpho y el de la luciérnaga en la bandeja: siguen las reglas de los iconos (07 §7.6; P3D-9);
- la garza blanca del fondo (`ProceduralArt.Scene.cs`, `Heron`) y el ave lejana del muelle (`RiverBird`): son paisaje, no personajes (queda como pregunta para el dueño en el informe de la fase 2);
- la cola de los miquitos: es una tira de vértices que se dibuja cada fotograma, sin textura que sombrear; solo cambia su borde.

## Mediciones (2026-10-27, el Spark: Windows ARM64, Editor arm64)

Con `AF.Tests.EditMode.Personajes3dPerfTests.MeasureCharacterArt` (explícita): cada cifra es el mínimo de 3 generaciones en instancias nuevas, tras el JIT. **Antes:** `a5144bea`. **Después:** la fase 2. El Editor comparte la máquina con otros procesos (un servidor de LLM local, a ratos): entre ejecuciones las cifras varían hasta ×1,5, así que «antes» y «después» se tomaron con la máquina en calma (el control, el retrato bloqueado de avatar, cuyo código no cambia, da 6,6 y 5,9 ms).

| Qué | Antes | Después | Presupuesto |
|---|---|---|---|
| Un retrato de avatar de 192 px (64 dp) | 5,7 ms | 6,4 ms | ≤ 8 ms (`AvatarArtTests`, pasa) |
| Un retrato de avatar de 96 px (32 dp, la Liga) / de 72 px (24 dp) | 1,5 / 1,0 ms | 1,8 / 1,0 ms | — |
| Retrato bloqueado de 192 px (control: el código no cambia) | 6,6 ms | 5,9 ms | — |
| Piezas de un miquito, la peor especie (huapo) | 6,8 ms | 8,6 ms | ≤ 10 ms (`IntroArtTests`, pasa) |
| Piezas de un miquito, la media de las 11 especies | 4,8 ms | 4,8 ms | — |
| Atlas de TiTino (1024 × 512, una vez por sesión) | 60 ms | 55 ms | **≤ 150 ms, nuevo** (P3D-4 a) |
| Retrato de TiTino | 27 ms | 23 ms | — |
| Rosa (forma nueva) | 28 ms | 24 ms | — |
| El bufeo del Mapa (forma nueva, de juguete) | 2,2 ms | 5,8 ms | — (al pedirlo, una vez) |
| La piraña (despierta / dormida) | 1,5 / 1,6 ms | 1,8 / 1,8 ms | — |
| La luciérnaga (cuerpo y alas) | 1,2 ms | 0,9 ms | — |
| La canoa de Capi del Mapa (control) | 16,5 ms | 15,2 ms | — |

- **No se subió ningún presupuesto.** Para que el avatar cupiera en sus 8 ms (con la cuenta completa por píxel salía a 10-15 ms) se cachea: cada parte tiene su tabla de sombreado (`ToyLut`, 25² normales en bytes, ≈ 1,9 KB), creada al pedir la celda y rellenada la primera vez que se pinta; la fauna usa una caché por hilo de 24 tablas de 33² (≈ 78 KB).
- **Memoria:** la de los atlas no cambia (mismos tamaños y formatos). Las tablas suman ≈ 360 KB si se pintan los 16 avatares y ≈ 78 KB por hilo que pinta fauna; no son texturas.
- **Asignaciones por fotograma:** ninguna nueva (el atlas de avatares lo comprueba en `TheAtlas_PaintsTwoPerFrame_WithoutAllocating`).
- Coste de una cuenta completa de `ShadeCore` en el Editor: 90-150 ns por píxel (lo mide la misma prueba).
