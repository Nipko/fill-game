# Piloto de los personajes «3D cartoon, juguete brillante» (D-058): antes y después

Capturas del piloto de la fase 1 del encargo `docs/dev/traspaso/spark-personajes-3d.md`, para que el dueño apruebe antes de la fase 2.
- **Cómo se generan:** el juego real a 360 dp (1080 × 2340 px), con el texto al 100 % y en es-419, con la prueba explícita `AF.Tests.PlayMode.Personajes3dSnapshotTests`. Necesita gráficos: `unity test <repo> --mode PlayMode --architecture arm64 --filter AF.Tests.PlayMode.Personajes3dSnapshotTests`, sin `-nographics`.
- **Qué hay en cada carpeta:** `antes/` es la rama en `269d1df1`, antes de tocar el arte, y `despues/` es el resultado del piloto.
- **Alto contraste:** las capturas con `_ac` lo llevan encendido. Por decisión [P] no cambia a los personajes (guía §5), así que solo cambia el tablero.
- **La guía de estilo** y su lámina de referencia, generada por el código, están en `docs/art/estilo-personajes-3d.md`.

| Captura | Antes | Después | Qué mirar |
|---|---|---|---|
| Inicio, el muelle (nivel 31, 8:00) | [antes](antes/p3d_01_inicio.png) | [después](despues/p3d_01_inicio.png) | **TiTino colgado:** felpa en mechones redondos, volumen con barniz, ojos con dos reflejos y contorno fino en lugar de la tinta. **La canoa de Capi con Rosa:** Rosa, lacada; Capi, con volumen y sin el trazo grueso. |
| Inicio con Alto contraste | [antes](antes/p3d_01_inicio_ac.png) | [después](despues/p3d_01_inicio_ac.png) | Lo mismo: Alto contraste no cambia a los personajes. |
| Inicio de noche (nivel 58, 22:00) | [antes](antes/p3d_02_inicio_noche.png) | [después](despues/p3d_02_inicio_noche.png) | TiTino dormido (`sloth_sleep`): el párpado cerrado encaja con la cara de juguete, sin costura. |
| Intro del nivel 7 a 1.100 ms (y `_ac`) | [antes](antes/p3d_03_intro.png) | [después](despues/p3d_03_intro.png) | **Sin cambios a propósito:** los miquitos no entran en el piloto (guía §9, pregunta 4). Sirve de control: ningún otro arte cambió. |
| Mapa, «Ver mapa» del 20 a mitad del viaje (y `_ac`) | [antes](antes/p3d_04_mapa_viaje.png) | [después](despues/p3d_04_mapa_viaje.png) | La canoa de Capi con Rosa (Rosa en vinilo rosa con brillo) y la ventanita de casa con TiTino a 56 dp. |
| Victoria, el hito del 40 a 1,6 s (y `_ac`) | [antes](antes/p3d_05_victoria.png) | [después](despues/p3d_05_victoria.png) | TiTino pequeño en la franja de la cabecera: se reconoce por la silueta, el antifaz y la cara clara. |
| Lámina de TiTino: las 10 poses | [antes](antes/p3d_10_lamina_titino.png) | [después](despues/p3d_10_lamina_titino.png) | **Todas las poses,** sobre la selva clara y sobre el panel: el rig y las poses son los mismos; solo cambia cómo se pinta cada pieza. **Garras:** marfil lacado, con la oclusión en la raíz. **Espaldas** (`cheer_big`, `sleep`): el espéculo y las algas, en felpa. |
| Lámina de los personajes en grande | [antes](antes/p3d_11_lamina_personajes.png) | [después](despues/p3d_11_lamina_personajes.png) | TiTino a 190 dp y su retrato (abierto y cerrado), la morpho (a 4 × y a 1,6 ×), Capi en el banner con su sombra de contacto, la canoa, Rosa y la pata de Capi. |
| Siluetas a 48, 32 y 24 dp | [antes](antes/p3d_12_siluetas.png) | [después](despues/p3d_12_siluetas.png) | La legibilidad a los tamaños de la Liga (32 dp) y de la victoria (24 dp), sobre los dos fondos. |

## Qué entra en el piloto

- **TiTino** (`ProceduralArt.SlothRig.cs` y `ProceduralArt.Sloth.cs`): todas las piezas del rig (cabeza, torso de frente y de espaldas, extremidad con las garras y párpado) y el retrato. La hoja y la rama de embaúba son objetos y no cambian.
- **Capi** (`ProceduralArt.Mascots.cs` y `ProceduralArt.MapIcons.cs`):
  - el cuerpo, en el banner de mundo, en la canoa del Mapa y del muelle y en el icono de la pestaña del Mapa;
  - la pata que presenta;
  - la sombra de contacto en el banner y en el casco.
- **El morpho** (`ProceduralArt.Morpho.cs`): las alas y el cuerpo de la mariposa que vuela, con las mismas siluetas y el mismo halo. El icono de la bandeja no cambia (guía §9, pregunta 5).
- **Rosa, la bota rosada** (`ProceduralArt.Mascots.cs`), como segundo animal [P] (guía §9, pregunta 4).
- **El sombreado** (`ProceduralArt.Toy.cs`): `ToyShading` y `ToyTones`, funciones puras con 16 tests EditMode (`ToyShadingTests`). A `ColorMath` se le añade `ScaleOkLightnessChroma`.

## Mediciones (2026-10-27, el Spark: Windows ARM64, Editor arm64)

Todos los tiempos son de reloj y tienen ruido de unos ±20 %. «Antes» se midió con el código del piloto retirado del árbol (un stash temporal), sobre la misma Library.

| Qué | Antes | Después | Presupuesto |
|---|---|---|---|
| Banner de mundo con Capi: en el hilo principal (`WorldBannerArtTests`) | 0,7-1,1 ms | 0,9-1,5 ms | ≤ 8 ms (pasa) |
| Banner de mundo con Capi: en el otro hilo (mejor ronda) | ≈ 240-250 ms | ≈ 310-340 ms | ≤ 500 ms (pasa) |
| Atlas de TiTino (1024 × 512 con mipmaps; `SlothArtMs`, una vez por sesión) | 59,9 ms | 80-102 ms | sin presupuesto en los tests |
| Rama de embaúba (sin cambios: control del ruido) | 37,1 ms | 38,7-51,6 ms | — |
| Memoria de los atlas | la misma | **la misma** (mismos tamaños y formatos) | — |
| Asignaciones por fotograma | — | ninguna nueva (todo se hornea una vez) | — |

- **Lo que se hizo para contener el coste del banner:**
  - la normal del borde de Capi solo se calcula en la franja de su cúpula; más dentro, la cúpula ya mira de frente;
  - el barniz y la luz de borde usan multiplicaciones en lugar de `Mathf.Pow`.
- **El atlas de TiTino** cuesta unos 20-40 ms más, una vez por sesión, la primera vez que se pide. Si hace falta, se puede pintar fuera del hilo principal, como el banner. Queda anotado y no se subió ningún presupuesto.
