# Estado del equipo Spark: el Reto del río

Encargo: `docs/dev/traspaso/spark-reto-del-rio.md` (de `origin/integracion-fase1-fase2`). Nota de diseño: `docs/dev/reto-del-rio.md`.

Orden de este archivo (encargo §9, punto 5): arriba el resumen; después, una entrada por tarea o bloque, de la más reciente a la más antigua; al final, la única sección «Enganches para el integrador» (E1-E20).

---

## Gráficos de la ficha de Play (D-065)

### 2026-10-30 · Fase 3, el vídeo de la ficha (`spark/tienda-graficos`): HECHO

- **Entrega:** `docs/store-listing/video/`, con `README.md` (guion y preguntas) y `manifest.json`.
  - 12 MP4: 30 s y el corte de 15 s, en horizontal 1920 × 1080 (el de Play) y en vertical 1080 × 1920, en es-419, en y pt-BR.
  - H.264 + AAC a 30 fps y −16 LUFS, de 6 a 20 MB cada uno.
- **Contenido:** gameplay real del teléfono, sin voz:
  - el nivel 42: vertido, encadenados, Llenar y teñir y victoria;
  - la intro de la marca con el descriptor;
  - la Marea en un Difícil, el muelle, el Mapa;
  - el cierre con el logotipo v2.
  - La IA solo anima los dos fondos del concepto A (Wan 2.2 i2v).
- **Tomas nuevas en el teléfono** (12, con `screenrecord`), en modo demo.
  - **Incidencia:** en las primeras tomas del muelle y el Mapa salieron notificaciones de WhatsApp. Se borraron al momento, sin usarlas, y se repitieron con No molestar, que activó el dueño.
  - **Al terminar:** el modo demo quedó desactivado. No molestar lo apaga el dueño. El perfil del juego no cambió respecto a la fase 2.
- **Preguntas [P], con la recomendada aplicada** (detalle en el README):
  - **Q-GV-1:** la música es `mus_menu` (CC0), que el encargo admite; se cambia cuando haya pista con cesión total. Hay que mirar Content ID al subirlo.
  - **Q-GV-2:** 30 s más el corte de 15 s.
  - **Q-GV-3:** el cierre usa «Sin wifi, a tu ritmo», sin imperativo.
  - **Q-GV-4:** el horizontal para Play.
- **Cómputo:** 2 vídeos de Wan 2.2 en la RTX 5080 (~2,3 min de GPU). El montaje, unos 2 min por idioma en el Spark.
- **Siguiente:** el dueño los mira, los sube a YouTube y pega el enlace en Play Console.

### 2026-10-30 · Elección del dueño: A como ficha, B y C como variantes de prueba

- **Decisión del dueño (Q-GT-2 resuelta, la recomendada):** el concepto **A «Gameplay héroe»** es la ficha principal; **B** y **C** quedan como variantes de una prueba de ficha (experimento de Play Console) después del lanzamiento.
- Anotado en `graficos-v2/README.md` y en la hoja de contactos `index.html`.
- **Siguiente, para el integrador:** fusionar `spark/tienda-graficos`; poner al día §6 de `../<idioma>/ficha.md` con los rótulos de A (Q-GT-3); subir A en Play (lo sube el dueño). Siguen abiertas para el dueño Q-GT-1, Q-GT-3, Q-GT-4 y Q-GT-5 (las recomendadas ya aplicadas). La fase 3 (vídeo) queda opcional.

### 2026-10-30 · Fase 2, conceptos B «Panorámica del río» y C «Cartel con personajes» (`spark/tienda-graficos`): HECHOS

- **Entrega:** `graficos-v2/B/` y `graficos-v2/C/`, 8 capturas × 3 idiomas, el gráfico de funciones y `manifest.json` cada uno.
  - El README y la hoja de contactos `index.html` ya tienen los tres conceptos.
  - Validadas las 50 piezas nuevas: 1080 × 1920 o 1024 × 500, RGB de 24 bits, menos de 2 MB.
- **B:**
  - una panorámica de Z-Image a 2.560 × 768 (por encima de unas 3 veces más ancho que alto, el modelo se rompe), ampliada a 8.640 px y repartida en las 8 capturas;
  - la canoa de Capi (arte real) avanza en cada captura;
  - pantalla al ~61 % del alto, con el tablero entero en cada captura.
- **C:**
  - las 1-2 como cartel con TiTino, Rosa y Capi alrededor de la pantalla, sin tapar la UI; las 3-8, idénticas a las de A;
  - los personajes son los renders reales del juego (D-058), recortados del fondo plano de `docs/art/personajes-3d/fase2/despues/p3d_11_lamina_personajes.png`.
- **Pregunta [P], con la recomendada aplicada:** **Q-GT-5**, arte real de los personajes en lugar de renders y poses de IA. Así son fieles al juego, y no hace falta añadir Qwen-Image-Edit (~20 GB).
- **Cómputo:** 8 imágenes más en la RTX 5080 (~2 min de GPU); el recorte y el montaje, con Pillow en el Spark.
- **Vista previa privada para el dueño:** https://claude.ai/artifact/77UrrL5iMHp6vQsKgQXxu6 (copias JPEG; los PNG para subir son los del repositorio).
- **Siguiente:** el dueño elige en la hoja de contactos. La recomendada (investigación, Q-GT-2): A como ficha principal; B o C como variante de una prueba de ficha tras el lanzamiento. La fase 3 (vídeo) queda opcional.

### 2026-10-30 · Fase 2, concepto A «Gameplay héroe» en los 3 idiomas (`spark/tienda-graficos`): HECHO

- **Entrega:** `docs/store-listing/graficos-v2/`:
  - `A/<es-419|en|pt-BR>/01-08-*.png` (24 capturas de 1080 × 1920);
  - `A/feature-graphic.png` (1024 × 500);
  - `A/manifest.json`, con la procedencia de las 25 piezas;
  - `README.md`, con qué es, por qué y qué mirar;
  - `index.html`, la hoja de contactos sin dependencias, con selector de idioma.
  - Todas, PNG de 24 bits sin transparencia y de menos de 2 MB.
- **Tomas reales en el teléfono** (SM-S938B):
  - **Build:** con el `com.amazonfill.dev` ya instalado (10:19 del 30-10), posterior al último cambio de código de la integración (06:57). No hizo falta compilar.
  - **Cómo:** adb guiado por `AF_LAYOUT` y `AF_STATE`, con los extras `af_lang`, `af_level`, `af_seeds` y `af_max_level`; los vertidos y la victoria, en fotogramas de `screenrecord`.
  - **Barra de estado:** modo demo de Android durante las tomas; al terminar, `sysui_demo_allowed` volvió a 0, como estaba.
- **Lo que cambió en el perfil del `dev.apk` del teléfono por las tomas** (no se hizo `pm clear`, para no borrar el estado de las pruebas del escritorio):
  - `max_level` 58;
  - semillas 1.310;
  - la página 1 de la Bitácora sellada el 30-10 (+60);
  - el nivel 25 en curso, sin salida.
- **Fondos:** Z-Image Turbo (Apache 2.0) en la 5080.
  - Prompts de pura naturaleza; se descartaron las semillas con casas, barcas o personas.
  - Nombrar en el prompt lo que no se quiere («no houses») las hace salir: Z-Image no tiene prompt negativo.
- **Cómputo:**
  - unas 51 imágenes de ~11 s en la RTX 5080 (~10 min de GPU);
  - el montaje con Pillow, en el Spark (segundos).
- **Preguntas [P], con la recomendada aplicada** (detalle en `graficos-v2/README.md`):
  - **Q-GT-3:** los rótulos nuevos y más cortos;
  - **Q-GT-4:** dejar a la vista la fecha del Reto (UI real) en las 5 y 8.
- **Fuera del repositorio** (encargo: nada de paquetes nuevos): los scripts de las tomas y del montaje, y las grabaciones del teléfono. Las grabaciones de arranque muestran la pantalla de inicio del teléfono, y no se publican.
- **Siguiente:** que el dueño mire A en `index.html`. Los conceptos B y C quedan pendientes, como variantes de experimento (Q-GT-2).

### 2026-10-30 · Fase 1, investigación de las fichas que funcionan (`spark/tienda-graficos`): HECHA

- **Encargo:** `docs/dev/traspaso/spark-graficos-tienda.md` §1. **Rama:** `spark/tienda-graficos`, desde `origin/integracion-fase1-fase2` (`38785473`).
- **Entrega:** `docs/store-listing/investigacion-fichas.md`.
  - 13 fichas de Play vistas en EE. UU., Brasil, México y Colombia, con su tabla por app.
  - Lo que sale de la muestra, y las guías de ASO y de Google contrastadas.
  - 10 conclusiones para Tintalú y lo que no podemos copiar.
- **Las tres conclusiones con más peso:**
  - la captura 1, un tablero real en pleno vertido al 70-80 % del alto, sin marco (hoy ~40 %);
  - rótulos de 2-4 palabras;
  - un momento especial del juego (la cuia que vierte, el Encuentro de las Aguas) ampliado y realzado con IA alrededor.
- **Preguntas [P], con la recomendada aplicada:**
  - **Q-GT-1, gráfico de funciones:** foco en el centro (Google) y logotipo fuera del círculo central del botón de reproducir (encargo §2).
  - **Q-GT-2:** los conceptos B (panorámica) y C (personajes en la 1-2), como variantes de experimento y no como ficha principal. Ningún líder usa panorámica en el teléfono, y el único que abre con personaje es el que menos descargas tiene [NV].
  - **Q-GT-3:** rótulos más cortos que los de §6 de `ficha.md`, en los 3 idiomas. La versión final irá en `graficos-v2/README.md` con el concepto A.
- **Capturas de referencia de terceros:** solo en el equipo de Spark, fuera del repositorio.
- **Cómputo:** ninguno de IA en esta fase.
- **Teléfono:** emparejado por depuración inalámbrica el 2026-10-30 (SM-S938B), listo para las tomas de la fase 2.
- **Siguiente:** concepto A en los 3 idiomas, con las tomas nuevas del `dev.apk`.

## Personajes 3D

### 2026-10-27 · Fase 2 de los personajes en «3D cartoon, juguete brillante», D-058 (`spark/personajes-3d`): HECHA · verificación completa VERDE · code-reviewer: APROBADO (2.ª ronda)

- **Visto bueno del dueño (2026-10-27)**, literal: «Apruebo el piloto de personajes 3D con las opciones recomendadas; ablanda la felpa de TiTino y recupera la panza de Capi y el contraste de la morpho, y sigue con la fase 2.»
  - Quedan decididas P3D-1 a P3D-10 con la recomendada; la tabla está al pie de esta sección.
  - **Petición añadida del dueño**, literal: «en el rediseño, mejoremos adicional al delfin que tenga mejor forma, esta un poco deforme con ese arco no se ve tan natural». Es la única excepción a la regla de «no cambiar siluetas», y solo cubre la silueta del delfín.
- **Rama:** `spark/personajes-3d`. Fusiona `origin/integracion-fase1-fase2` (`5b6c1aa2`, con `tanda6/avatares`) en `ea5025d2`.
- **Commits:**
  - `a5144bea`, `c3a928bb` y `8113adc5`: las capturas «antes», tomadas con el arte de antes, y `Personajes3dFase2SnapshotTests` (explícito);
  - `0e2d4f05` y `eb501998`: el código y la caché del sombreado de los avatares;
  - `ba9fa3a3`: las capturas «después», el README y la guía;
  - `fb71b335` y `3d4ed2c2`: la revisión; la pose del delfín y los menores;
  - el último: este estado.
- **Qué hay:**
  - **Base** (`ProceduralArt.Toy.cs`):
    - `ToyShading.ShadeCore`, la misma cuenta en floats; `Shade` lo envuelve;
    - las piezas `ToyEllipse`, `ToyEllipseOn` y `ToyCapsule`: borde fino, volumen, barniz y oclusión en una sola pasada;
    - `ToyLut`, la tabla del sombreado en bytes: una caché por hilo de 24 tablas para la fauna y una por parte de avatar (publicada con `Volatile`);
    - guardas de división en `CreaseAO` y `ContactShadow`.
  - **Fauna en estilo juguete:**
    - los 11 miquitos del catálogo, con el borde fino en lugar de la tinta, ojos con dos reflejos y la lana en felpa (de la cola solo cambia el borde);
    - la piraña (el pez despierto y dormido);
    - la luciérnaga (cuerpo y alas; el farol no cambia);
    - el bufeo.
  - **El delfín** (Rosa y el bufeo comparten la silueta), según la petición del dueño:
    - cuerpo fusiforme con una curva suave, sin el arco;
    - melón redondo y rostro largo y fino;
    - quilla dorsal baja, pectoral ancha y cola horizontal de dos lóbulos.
  - **El delfín en reposo:** en el banner se ve nadando en la superficie, con la cola hundida.
    - La espuma (40 dp, sin cambios) tapa sola el corte con el agua, que mide 29,3 dp en los píxeles reales.
    - `RosaCutWidth` = 0,29 y `RosaCutCenter` = 0,79; `RosaBlowhole` pasa a (0,30; 0,745).
    - No cambian ni el tamaño del sprite (104 × 66), ni las API, ni los `ArtSlots`.
  - **Los 16 avatares** en estilo juguete, con el borde fino de cada parte en lugar de la tinta de Capi (D-058) y un segundo reflejo en los ojos.
    - El bloqueado sigue en silueta sólida, sin ojos ni brillos.
    - El disco, el aro, la chapa y los fondos no cambian.
  - **Ajustes del piloto:**
    - P3D-2: `SlothClumpU` sube de 16 a 23, con menos surco; el brillo del pelaje pasa de 0,32 a 0,42 y el de la cara de 0,45 a 0,72.
    - P3D-3: la panza de Capi, más clara; en la morpho, el borde azul más ancho y el centro claro más amplio.
    - P3D-4 (a): `Personajes3dPerfTests`, con el atlas de TiTino ≤ 150 ms (`PerfBudget.Ms`).
  - **Los menores del revisor del piloto:** el alfa 0,92 en el test de contraste, sin el `Lerp` sobrante ni `SlothInk`, y los iris leídos de la fuente. Se quitaron también `MascotInk` y `MascotOutlineDp`, que se quedaron sin uso.
  - **Tests nuevos:**
    - `ToyShadingTests` (`ShadeCore` contra las funciones públicas, las piezas, las guardas y `ToyLut`);
    - `AvatarArtTests.TheSilhouette_ReadsAt32And24dp_LockedAndUnlocked`;
    - `RosaWaterCutTests`: que el corte quepa bajo la espuma, medido en los píxeles reales, y el margen transparente.
    - En `IntroArtTests`, el ámbar y el pelo se comprueban por familia de tono, y el umbral de la nariz del huapo pasa de 0,06 a 0,03, con su motivo comentado.
  - **Fuera, a propósito:** la cueva y el bocado de la piraña, las mecánicas que son objetos, los glifos de chip y de chapa, los iconos de la bandeja, y la garza y el ave lejana del fondo (son paisaje: F2-1).
  - **Capturas para el dueño:** 32 PNG por LFS en `docs/art/personajes-3d/fase2/{antes,despues}`:
    - `f2_01` la fauna;
    - `f2_02` los avatares (56, 32 y 24 dp, normales y bloqueados);
    - `f2_03` y `f2_04` la intro;
    - `f2_05` el Mapa con el bufeo;
    - `f2_06` la victoria con avatar;
    - `f2_07` la Liga;
    - `f2_08` el delfín y `f2_08b` el banner en reposo;
    - las escenas también en Alto contraste, y las láminas del piloto rehechas.

    El `README.md` dice qué mirar. La guía tiene una §11 nueva y las [P] marcadas como decididas.
- **Verificación**, con el código final (`3d4ed2c2`):

  | Comando | Resultado |
  |---|---|
  | `dotnet build -warnaserror` / `dotnet test` | 0 avisos / 1.002 (996 + 6) |
  | `validate_schema.py` | 116/116 |
  | `unity-compile` | OK |
  | EditMode | 4.750 (4.743 + 7), 0 fallos |
  | PlayMode (`-TimeoutSec 7200`) | 2.263 (1.934 + 329), 0 fallos |
  | Capturas explícitas de la fase 2 y láminas del piloto | 15/15 |
  | `unity-build.ps1 -Method Dev` | OK, `dev.apk` de 76,8 MB |
  | `check-16kb` / `check-manifest -Mode dev` | OK / OK |
  | `size-gate` | no se aplica: solo acepta `.aab` |
  | Teléfono | sin dispositivo en `adb devices`: sin fps ni arranque |

- **Rendimiento** (en el Editor; mínimo de 3 pasadas):

  | Qué | Antes | Después | Presupuesto |
  |---|---|---|---|
  | Avatar de 192 px | 5,7 ms | 6,4 ms | ≤ 8 ms |
  | Miquito (la peor especie) | 6,8 ms | 8,6 ms | ≤ 10 ms |
  | Atlas de TiTino | 60 ms | 55 ms | ≤ 150 ms (nuevo) |
  | Rosa | 28 ms | 24 ms | no tiene |
  | Bufeo | 2,2 ms | 5,8 ms | no tiene |

  - Sin la caché, el avatar salía a 10-15 ms.
  - No se subió ningún presupuesto.
  - La memoria de los atlas no cambia. Las tablas ocupan ~360 KB con los 16 avatares, más ~78 KB por cada hilo que pinta fauna.
  - No hay asignaciones nuevas por fotograma.
  - **Incidencia:** la máquina estaba compartida con un `llama-server` local y los tiempos variaron hasta ×1,5 entre pasadas. En una pasada cargada fallaron por tiempo `APortrait_TakesLessThan8ms` y el atlas de frutas, que no se tocó. Las suites finales pasaron sin fallos.
- **code-reviewer:**
  - 1.ª ronda: CAMBIOS NECESARIOS por dos bloqueantes:
    - faltaba esta entrada;
    - con la silueta nueva, la espuma de 40 dp no tapaba el corte de 45 dp con el agua en reposo. El comentario y la guía decían, mal, que lo tapaba la onda, y la captura lo escondía al pintar la onda opaca.

    Los dos quedaron resueltos en `fb71b335`, `3d4ed2c2` y esta entrada, junto con los menores: el test de `ShadeCore` dejó de ser tautológico, `Volatile` en `ToyLut`, la sangría, el comentario de 0,03 y el margen del sprite.
  - 2.ª ronda: **APROBADO**. Comprobó a mano el margen de la espuma (unos 5,5 dp a cada lado) y el soplo sobre el contorno del melón. Una observación que no bloquea: en `f2_08b`, el lóbulo de arriba de la cola asoma un poco sobre el agua, a la derecha de la espuma (F2-3).

### Para el dueño (fase 2; no bloquean lo entregado)

| Id | Pregunta | Recomendada [P] |
|---|---|---|
| F2-1 | La garza blanca del fondo (`Scene.Heron`) y el ave lejana del muelle: ¿también en estilo juguete? | Dejarlas como paisaje: no son personajes |
| F2-2 | La cola de los miquitos es una tira plana que se dibuja en cada fotograma: ¿le damos volumen en el shader de la tira? | Dejarla así: el volumen obliga a tocar el shader |
| F2-3 | El delfín en el banner nada en la superficie con la cola hundida; sobre el agua quedan la cabeza, el lomo y la pectoral | Dejarlo así; si quieres más cuerpo fuera, que se mire en el banner real, en movimiento |
| F2-4 | Probar el APK en el teléfono (fps y arranque): no había dispositivo | Reactivar la depuración inalámbrica cuando puedas |

### 2026-10-27 · Piloto (fase 1) de los personajes en «3D cartoon, juguete brillante», D-058 (`spark/personajes-3d`): HECHO · verificación completa VERDE · **aprobado por el dueño el 2026-10-27**

- **Encargo:** `docs/dev/traspaso/spark-personajes-3d.md`.
- **Rama:** `spark/personajes-3d`, desde `origin/integracion-fase1-fase2` en `16c1399d`.
- **Commits:**
  - `269d1df1`: las capturas «antes» y `Personajes3dSnapshotTests` (explícito);
  - `bfc69c56`: el código;
  - `d8f7942a`: la guía y las capturas «después»;
  - `a0872f3b`: las mediciones;
  - el último: este estado y la frase de la guía §0.
- **Qué hay:**
  - `ToyShading` y `ToyTones`, funciones puras de sombreado (`Scripts/Presentation/ProceduralArt.Toy.cs`; 16 tests en `ToyShadingTests`), y `ColorMath.ScaleOkLightnessChroma`;
  - TiTino repintado: las 10 poses del rig, de día y de noche, y el retrato;
  - Capi repintado: el banner, la canoa del Mapa y del muelle, y la pata;
  - Rosa y el morpho repintados;
  - sin cambios en siluetas, tamaños, rig, poses, API ni `ArtSlots`;
  - la guía `docs/art/estilo-personajes-3d.md`, con la lámina generada por el código (`docs/art/personajes-3d/p3d_20_referencia_estilo.png`);
  - **las capturas para el dueño**: 12 pares antes/después por LFS en `docs/art/personajes-3d/piloto/`. Su `README.md` dice qué mirar en cada par. La intro sale idéntica byte a byte; es el control.
- **Verificación:**

  | Comando | Resultado |
  |---|---|
  | `dotnet build -warnaserror` / `dotnet test` | 0 avisos / 1.002 (996 + 6) |
  | `validate_schema.py` | 116/116 |
  | `unity-compile` | OK |
  | EditMode | 4.743 (4.738 + 5), 0 fallos |
  | PlayMode (`-TimeoutSec 7200`) | 2.237 (1.922 + 315), 0 fallos |
  | `unity-build.ps1 -Method Dev` | OK, `dev.apk` de 76,2 MB |
  | `check-16kb` / `check-manifest -Mode dev` | OK / OK |
  | `size-gate` | no se aplica: solo acepta `.aab` |
  | Teléfono | sin dispositivo en `adb devices`: sin fps ni arranque |

- **Rendimiento:**
  - El banner con Capi tarda 0,9-1,5 ms en el hilo principal (tope 8 ms) y 310-340 ms en el otro hilo (tope 500 ms).
  - El atlas de TiTino pasa de 60 a 80-102 ms, una vez por sesión, sin presupuesto.
  - La memoria de los atlas no cambia y no hay asignaciones nuevas por fotograma. No se subió ningún presupuesto.
- **code-reviewer:** CAMBIOS NECESARIOS por un único bloqueante, que faltaba esta entrada; resuelto con esta entrada.
  - El código, los tests, el rendimiento y las capturas, sin hallazgos.
  - Comprobado que el `git stash pop` del autor no perdió ni coló nada.
  - **Menores para la fase 2:**
    - el test de contraste usa el alfa 1 del contorno (con 0,92 sigue dando ≥ 3,8:1);
    - sobra un `Lerp` en `SlothRig.cs:907`;
    - faltan guardas de división en `CreaseAO` y `ContactShadow`;
    - `SlothInk` está sin uso;
    - los hex de los iris están copiados en `ToyStyleSheetTests`.

### Decisiones del piloto (el dueño las resolvió todas el 2026-10-27, con la recomendada)

| Id | Pregunta | Recomendada [P] |
|---|---|---|
| P3D-1 | ¿Se aprueba el piloto y se sigue con la fase 2 (la fauna que falta, el bufeo del Mapa y, cuando se integren, los avatares)? | Mirar las capturas; si no convence, decir qué personaje cambiar |
| P3D-2 | La felpa de TiTino: los mechones redondos aún se distinguen en grande (190 dp y la lámina) y se lee más «felpa grumosa» que «vinilo lacado» | Ablandarla (subir `SlothClumpU`) y dar más brillo a la cara si al dueño le parece grumosa |
| P3D-3 | Aspecto en `p3d_11`: la panza clara de Capi casi desaparece y la morpho pierde el contraste entre el borde azul profundo y el centro claro | Recuperar los dos en la fase 2 |
| P3D-4 | El atlas de TiTino (80-102 ms en el hilo principal, en el Editor; en el teléfono puede ser más) | (a) un `PerfBudget` propio en la fase 2; (b) pintarlo fuera del hilo principal, como el banner, si se mide un tirón en el teléfono |
| P3D-5 a P3D-10 | Las 6 decisiones de la guía §9: ojos a 1,25×, Alto contraste sin cambios en los personajes, oclusión en las garras en lugar de sombra en la rama, Rosa como segundo animal, el icono de la morpho como icono y el bufeo en la fase 2 | Las [P] de la guía |

## 2026-10-25 · Integración de T6, carriles A y B (escritorio)

- **Fusionados** en `integracion-fase1-fase2`: `spark/t6-tests-fiables` (f1cdb634) y `spark/t6-pulido-pantalla` (c6b2f078), junto con `codex/web-marca` (7e09ab38). Conflicto solo en este archivo: se dejaron las dos entradas.
- **Q-T6F-1:** el integrador acepta el gancho `LaunchOptions.ReuseDataReadsOverride` y `GameBootstrap.ReadData` (solo actúa en tests; el code-reviewer comprobó que con null el juego hace lo mismo que antes). Queda como menor un test propio de la caché.
- **code-reviewer:** A y B correctos; los dos bloqueantes eran de la web de Codex (logo y AUTOMATION AI), resueltos en el escritorio (D-048).
- **Verificación** (x86_64): build 0; dotnet test 1.002 (996 + 6 omitidos); esquema 116/116; loc y RC idénticos (254 claves); compila; EditMode 4.538 (4.535 + 3 omitidos), 0 fallos; PlayMode 2.100 (1.806 + 294 omitidos), 0 fallos, **1.792 s (30 min, antes 36-55)**; build de desarrollo OK; infra: typecheck, lint, web:check y 361 tests.

## 2026-10-24 · T6 · Carril A: tests que no dependen de la hora, capturas al día y una suite más rápida (`spark/t6-tests-fiables`): HECHA · verificación completa sin fallos (EditMode completa con el código final) · code-reviewer: APROBADO (2.ª ronda, tras los CAMBIOS NECESARIOS atendidos en `ce0b7d9f`) · Q-T6F-1 pendiente del dueño o del integrador

- **Encargo:** `spark-calidad-pulido.md`, carril A (puntos 1-6). Rama desde `origin/integracion-fase1-fase2` en `645d1856`. Sin empujar.
- **Commits:** `4084c012` (las cinco capturas que fallaban), `9d1be13c` (reloj fijo, guarda —ya con su descripción partida para no denunciarse a sí misma— y velocidad), `319d44f3` (el saludo de TiTino en las capturas del Reto a 640 y el muelle forzado del 12 en 16:9; su mensaje dice también lo de la guarda, pero ese arreglo va en `9d1be13c`), `40a9d5a7` (esta entrada) y `ce0b7d9f` (revisión: `[OneTimeTearDown]` que libera el arte de `ConsentScreenTests`, `DailyTeaserCardTests` y `LogbookScreenTests`; `ReadData` vacía su caché sin el gancho; la guarda mira también `Packages/com.amazonfill.core/Tests`; el comentario de `Shot` en su sitio), más el de estas correcciones.
- **Qué hace:**
  - `Tests/PlayMode/TestClock.cs` (nuevo): reloj de los tests, sábado 2026-10-24 15:00 UTC (10:00 en Bogotá, −5 h), que avanza con el tiempo real y se adelanta a mano (`Advance`); `AF_TEST_CLOCK_SHIFT_H` mueve el ancla (la pasada de 12 h). `PlayModeHelpers.NoRealClockUnlessSet` lo pone en `LaunchOptions.ClockOverride` al cargar la escena si el test no puso otro reloj, y escribe una vez `AF_TEST_CLOCK ancla …`. No hizo falta ningún gancho de reloj nuevo en el juego: `ClockOverride` ya llegaba a todo (la Liga y los topes, por el reloj de telemetría y `GuardedClock`). Tests en `TestClockTests` (5).
  - Guarda `Tests/EditMode/TestClockGuardTests.cs`: ningún `.cs` de `Assets/_Project/Tests/**` ni de las carpetas `Tests` de `Scripts/**` lee `DateTime.UtcNow/Now/Today`, `DateTimeOffset.UtcNow/Now` ni `SystemClock.Instance` fuera de la lista blanca comentada (3 entradas, abajo); falla también si una entrada de la lista blanca deja de usarse.
  - Velocidad (sin `[Explicit]` nuevos ni tests quitados): el arte procedural (unos 3 s por instancia), uno por fixture en `SlothRigTests` (salvo los 3 tests que miden lo que se crea, que usan uno nuevo), `ConsentScreenTests`, `DailyTeaserCardTests` y `LogbookScreenTests`, como ya hacían otros 15 fixtures; los paquetes de niveles y el pool del Reto se leen y validan una vez por pasada en los tests (`LaunchOptions.ReuseDataReadsOverride`, gancho solo de tests: el juego los lee en cada carga como siempre; el catálogo y el servicio del Reto se rehacen en cada carga); sin pila en los `Debug.Log` de PlayMode (como el teléfono de desarrollo, `DevProbe.Initialize`; avisos y errores la conservan; el log pasa de 1,79 M a 113 k líneas); `PlayGamesCloudBootTests` adelanta el reloj en vez de esperar 9 s; `IntroTests` solo construye el mensaje y llama a NUnit cuando algo falla (las mismas comprobaciones) y usa un `HashSet` para la tropa.
- **Tabla del reloj (puntos 1-2):**

| Test (o grupo) | Causa (qué hora o fecha leía) | Arreglo |
|---|---|---|
| Todos los tests de PlayMode que arrancan la escena sin reloj propio (unos 1.700 de 2.064; todos los que usan `PlayModeHelpers.LoadGame/LoadApp…` sin `LaunchOptions.ClockOverride`) | `GameBootstrap` usaba `SystemClock`: la hora local del equipo decidía el saludo y la pose de TiTino (noche 20-6, sol 10-16, saludo de la mañana 6-12), el Dato del día y la frase del día, la fecha del Reto, la semana de la Liga (lunes 12:00 UTC), la regeneración de canoas, `economy_ts`, el Regalo de regreso (días desde `last_seen`) y las ofertas por días desde la instalación | `TestClock` (nuevo, `Tests/PlayMode/TestClock.cs`): sábado 2026-10-24 15:00 UTC (10:00 en Bogotá, −5 h), que avanza con el tiempo real y se adelanta a mano (`Advance`). `PlayModeHelpers.NoRealClockUnlessSet` lo pone en `LaunchOptions.ClockOverride` al cargar la escena si el test no puso otro; `AF_TEST_CLOCK_SHIFT_H` mueve el ancla (pasada de 12 h). Sin gancho nuevo en el juego: `ClockOverride` ya llegaba a todo (Liga y topes por el reloj de telemetría y `GuardedClock`) |
| `AudioQuickTogglesTests.Home_FitsWithoutOverlaps…(640)` | El saludo de la mañana de TiTino (6-12 h locales) apartaba la tarjeta del Reto | Ya arreglado en la integración (espera el saludo); ahora además corre siempre a las 10:00 (con saludo) y, en la pasada de 12 h, a las 22:00 (TiTino duerme, sin saludo) |
| `AudioQuickTogglesTests.LoadRichLobby` (todos sus casos del inicio) | `NextRegenAt = DateTime.UtcNow + 10 min`, con el juego en un `ManualClock` del 16-10 o en el reloj real: el cronómetro de canoas salía con días u horas según la fecha | `NextRegenAt` relativo al reloj del juego (`ClockOverride` o el `TestClock`) — **archivo del carril B**, cambio de una línea |
| `PlayGamesCloudBootTests.ALateCloud_WithAWinWhileWaiting…` | `economy_ts` de la nube con fecha fija (ya arreglado en la integración con `DateTime.UtcNow + 1 día`) | Relativo al `TestClock` (`TestClock.Install().UtcNow + 1 día`) |
| `PlayGamesCloudBootTests.ALateCloud_WithAPurchaseInFlight…` | `economy_ts` = `DateTime.UtcNow + 8 s` y esperaba 9 s reales a que el reloj del sistema lo pasara | Relativo al `TestClock`; en vez de esperar, `clock.Advance` (−9 s por pasada) |
| `TelemetryBootTests.TheColdStart_ComebackGift_ReachesTheAnalytics` | Perfil con `SystemClock.Instance` y `last_seen` = `DateTime.UtcNow − 8 días` frente al reloj del juego | Perfil y `last_seen` con el `TestClock` del test |
| `HttpBackendClientTests` (`WaitForPosts`) | `DateTime.UtcNow` como plazo de un `Monitor.Wait`: mide tiempo transcurrido, no una fecha | En la lista blanca de la guarda (AF.Backend es del sdk-integrator) |
| `LeagueContractTests.TheRequest_…` | `DateTime.UtcNow` como valor cualquiera para un constructor que lanza | En la lista blanca de la guarda |
| `ClockTests.SystemClock_ReturnsUtc_AndASaneLocalOffset` | Prueba el reloj real | En la lista blanca de la guarda (es lo que se prueba) |
| Tests con perfil de fecha fija sin reloj propio (`MapProgressTests`, `RiverMapScreenTests`, `MetaTests`, `ContentEndMapTests`, `AccountAndCloudTests`, `DeleteAccountWaitTests`, `VerifiedPurchasePlayModeTests`, `PlayBillingPlayModeTests`, `Worlds7to9ContentTests`, `BootSplashTests`…) | Perfiles creados el 08-10 a 23-10 frente al reloj real: cuantos más días pasaran, más cerca del Regalo de regreso (7 días) y de otros cambios por fecha | Con el `TestClock`, la distancia es fija (la de hoy, 24-10) |

- **Capturas `[Explicit]` (punto 4)**, con `Unity.exe -batchmode -runTests -testPlatform PlayMode -testFilter "AF\.Tests\.PlayMode\..*SnapshotTests"` (con gráficos): 259 casos; la primera pasada dio 3 fallos, arreglados y repetidos sus fixtures (`DailyChallengeSnapshotTests` 4/4 y `TutorialMorphoSnapshotTests` 1/1).
  - Las cinco que fallaban: **Fx15 `CaptureLevel2AtTheStart`**, el dedo del 2 ya sale al empezar (B11, Q-B11-1): la captura lo enseña; **SlothFail `CaptureRestartNotice`** a 640, el 9 no deja sitio en 16:9 y el aviso no sale (como el test normal): sin captura; **TutorialMorpho** y **UndoTip** a 640, la instrucción sale como aviso de texto (16:9 desde DC3): la captura enseña el aviso; **`VisualSnapshotTests.CaptureLobby`**, el fin del contenido es el 701 (sin el Reto en el build), no el 301.
  - Nueva tras integrar el audio: **`DailyChallengeSnapshotTests` a 640** (es-419 y en-XA): el lunes a las 10:00 TiTino saluda y aparta la tarjeta del Reto mientras se ve (D-046, punto 2); la captura espera al saludo tras cada arranque. La variante del 12 con la franja forzada a 40 dp también va como aviso en 16:9.
  - Comparadas por grupos con las de la tropa (`spark/t6-tropa-3filas`, anteriores al audio), y revisadas a ojo las de cada grupo: 1.547 imágenes; 1.352 idénticas, 168 distintas y 27 nuevas.
    - **Pausa (D-046, punto 2):** `16b_pause` y `dc3_l31_pausa_16x9` llevan la nota y el altavoz junto a la X, con «Pausa» más corto; nuevas `d046_02_pause` y `d046_03_pause_130` (es-419 y en-XA), con Música tachada cuando está apagada. Bien a 640 y a 780.
    - **Inicio (D-046, punto 2):** las ~120 capturas del inicio, del Mercado y de los calendarios del Reto cambian solo en la columna de Música y Sonido bajo el engranaje (x ≈ 925-1.043 px, y ≈ 291-608); a 640 (`b1b_lobby_l29_640_*`, `fnt1_01_inicio`, `l4_130_inicio_*`, `lobby_stop_*`, `season_b_lobby_*`, `r11_01_card_play*`, `home_sloth_fact_130`), además, la tarjeta del Reto se aparta mientras TiTino saluda (antes la pisaba). Nuevas `d046_01_home` y las de la Liga (`league_0*`).
    - **3 filas (D-046, punto 3):** los tableros de 3 filas, la rama y los avisos de la franja (`t6t_*` y las demás de 11-16 cuias) salen idénticos a los de la tropa: la integración con el audio no los toca. Las victorias del 11 cambian menos de un 1 % (`17_victory_l11`, `b1_03_victoria_l11`).
    - **Capturas arregladas:** `fx15_l2_al_empezar` (con el dedo), `dc23_l5_640_*` y `tu1b_l12_*640*` (aviso de texto), `10c_content_end` (el 701) y `ti6b_aviso_modal_780_100`.
- **PlayMode más rápida (punto 5)** (`Logs/playmode.xml`; todas las medidas con el Editor del carril B en marcha en el mismo equipo):
  - antes (línea base): **3.585 s (59,8 min)**. Los 30 más lentos: los recorridos de idiomas (`TranslatedLocaleOverflowTests` 117 s, `PseudoLocOverflowTests` 72 s, `TranslatedScreensOverflowTests` 46 s), `IntroTests` (las dos de la tropa y la rama, 74 s), `FactLineTests`, `RemolinoBarroTests.NoAllocDuringSteps`, `WorldBannerTests`, `PhoneFixes1023Tests.BasinPlates_*` (6 × 12 s)…; por fixture, `SlothRigTests` 246 s (45 × 5 s: un arte procedural por test), `ConsentScreenTests` 79 s y `LogbookScreenTests` 36 s, por lo mismo. En la pasada: 143 artes procedurales (668 s), 2.092 lecturas de los paquetes (213 s) y unos 84.000 `Debug.Log` con su pila;
  - después: **2.287 s (38,1 min), 2.215 s (36,9 min) y 2.431 s (40,5 min)** en las tres pasadas normales; `SlothRigTests` 13 s, `ConsentScreenTests` 3 s, `LogbookScreenTests` 4 s, `DailyTeaserCardTests` 3 s, `PlayGamesCloudBootTests` 13 s (antes 30); las lecturas de los paquetes, 0-3 ms.
  - Queda lento lo que no se tocó: los recorridos de idiomas (22-24 s cada uno; `PseudoLocOverflowTests` es del carril B) y la carga de la escena en cada test (unos 0,3 s de «Integration»).
- **Verificación** (Spark, ARM64, 64 GB; `-Architecture arm64`):
  - línea base antes de tocar nada: EditMode **4.535 (4.532 + 3)** y PlayMode **2.064 (1.770 + 294)**, sin fallos;
  - `dotnet build dotnet/AmazonFill.slnx -warnaserror`: 0 errores, 0 avisos; `dotnet test`: 1.002 (996 + 6); `validate_schema.py`: 116/116;
  - `unity-compile.ps1`: compila;
  - EditMode **4.538 (4.535 + 3)**: 2 fallos, `IntroArtTests.FruitFlight_…_GeneratedWithin10ms` (11,3 ms frente a 10 con el otro Editor en marcha; de tiempo, ya fallaba así otras veces) y la guarda, que se denunciaba a sí misma por su descripción (arreglada en `9d1be13c`); repetidos `TestClockGuardTests`, `IntroArtTests` y `LaunchOptionsTests`: 33/33;
  - EditMode completa con el código final (`ce0b7d9f`): **4.538 (4.535 + 3), sin fallos** (esta vez `IntroArtTests.FruitFlight_…` pasó; es inestable por tiempo y anterior a esta rama: no se tocó su umbral);
  - tras los `[OneTimeTearDown]` de la revisión, PlayMode de los fixtures afectados (`ConsentScreenTests`, `DailyTeaserCardTests`, `LogbookScreenTests` y `SlothRigTests`): **74/74**;
  - PlayMode **2.069 (1.775 + 294)**, tres pasadas seguidas sin fallos (la 1.ª con `9d1be13c`, antes de `319d44f3`, que solo toca capturas explícitas; la 2.ª y la 3.ª con `319d44f3`; los cambios de `ce0b7d9f` se verificaron con la EditMode completa y los 4 fixtures de arriba);
  - PlayMode con el reloj del juego adelantado 12 h (`AF_TEST_CLOCK_SHIFT_H=12`: ancla 2026-10-25 03:00 UTC, las 22:00 en Bogotá, TiTino dormido y sin saludo de la mañana): **2.069 (1.775 + 294), sin fallos**, 3.056 s.
- **Preguntas (recomendada aplicada, [P]):**
  - **Q-T6F-1 (decisión pendiente: la tiene que aceptar el dueño o el integrador):** este carril añade código de producción en AF.UI (`GameBootstrap.ReadData` y `LaunchOptions.ReuseDataReadsOverride`) que solo actúa en los tests: con el gancho a true (lo pone `PlayModeHelpers`), los paquetes de niveles y el pool del Reto se leen una vez por pasada; en el juego el gancho es null y se leen en cada carga, como siempre (y la caché se vacía). ¿Se acepta ese gancho en el código del juego? Alternativa: quitarlo y perder unos 3-4 min por pasada de PlayMode. Recomendada aplicada [P]: aceptarlo así, solo para tests (no cachear también en el juego, para no cambiar su comportamiento en un carril de tests).
  - **Q-T6F-2:** ¿qué hora fija para los tests? Aplicada: las 10:00 en Bogotá del 24-10 (la fecha de los tests más recientes y con el saludo de la mañana, el caso que más pisa); la pasada de 12 h cubre la noche.
  - **Q-T6F-3:** los dos `DateTime.UtcNow` de `Scripts/Backend/Tests/` (un plazo de `Monitor.Wait` y un valor para un constructor que lanza) no dependen de la fecha y son del sdk-integrator: van en la lista blanca, sin tocarlos.
- **Archivos compartidos con el carril B:** `AudioQuickTogglesTests.cs`, una línea (`NextRegenAt` relativo al reloj del juego en `LoadRichLobby`), sin tocar su excepción `KnownAt320`. Nada de la UI.
- **Riesgos:** los tiempos se midieron con el Editor del carril B en marcha (la línea base pudo salir más lenta por eso; las tres pasadas de después también lo tenían); `IntroArtTests.FruitFlight_…` sigue dependiendo de la carga del equipo; un fixture con el arte compartido podría esconder un test que necesite un arte nuevo (los que lo necesitan lo crean); `ProjectSettings/GooglePlayGameSettings.txt` no salió modificado en este worktree.

---

## 2026-10-24 · T6 · Carril B: solapes y safe area pendientes (`spark/t6-pulido-pantalla`): HECHA · verificación completa sin fallos · code-reviewer: APROBADO (2.ª ronda, tras los CAMBIOS NECESARIOS atendidos en `198ca31c`, `31eb486d` y `d432d4ee`)

- **Encargo:** `spark-calidad-pulido.md`, carril B (puntos 1-6). Rama desde `origin/integracion-fase1-fase2` en `645d1856`. Sin empujar.
- **Commits:** `a9ee1e87` (tarjeta del Reto a 320 dp), `470624f8` (Q-T6T-5), `e8316103` (safe area del anuncio y del calendario, con tests), `f78e6dcd` (brillos y polen del río sin activar GameObjects), `8e60aa1e` (esta entrada); tras la revisión, `198ca31c` (Q-T6T-5 solo con Reiniciar en dos líneas), `31eb486d` (tests en que actúan las cotas de zona segura), `d432d4ee` (la Bitácora se recoloca cuando crece la tarjeta del Reto, comentarios y Q-T6P-1/3 en GDD.md) y el que pone al día esta entrada.
- **Qué hace:**
  1. **320 dp, Crecida y tarjeta del Reto:** la tarjeta (y la de la Bitácora, en su columna) no pasa de w − 32 (`DailyChallengeCard.WidthFor/LeftFor`): a 360 dp, 296 centrada como siempre (x 32-328); a 320 dp, **272 dp de x = 16 a 288**, a 2 dp de la Crecida (x 290-316). En la tarjeta estrecha la placa de la banda baja a 44 dp, el texto empieza en 64 y la racha mide 40, así que el estado conserva sus 132 dp [P]. El título y el estado crecen con su texto a su mínimo y la tarjeta con ellos, hacia arriba (02 §12): «Pendiente de validar» al 130 % en es-419 y en-XA pide 103 dp (antes no cabía ni a 360 dp: se salía a lo alto). Sale `KnownAt320`; el inicio a 320 dp no pisa nada al 100 y al 130 %, también con «Liga» (`Home_WithTheLeagueOn_…`), y la tarjeta de la Bitácora entra en el barrido de solapes.
  2. **Q-T6T-5:** si la píldora Reiniciar baja hasta la banda de etiquetas (dos líneas), el contador de Lluvia o Piraña va a su izquierda, a 4 dp [P] (`GameScreen.LayoutHudLabels`). Medido a 360 × 780 en en-XA: el 194 al 100 %, píldora x 292-356 / y 56-88 y contador x 218-288; el 200 al 130 %, píldora x 280-356 / y 47-88 y contador x 238-276 (compacto, 13 sp × 1,3). Marcado resuelto en la pregunta 68 de GDD.md §6.3 y en 02 §5. `RestartPill_360_KnownIssues` pasa ahora en los cuatro casos: deja de ser explícito y se llama `RestartPill_360_At130_ClearsTheLiftedCuia`.
  3. **Safe area (B10):** el parche de StoreSheet ya estaba integrado (B1b): con una barra de 220 px (73,3 dp) «Cancelar» acaba en y = 686,7 de 780 (20 dp sobre la barra) y en 16:9 en 546,7 de 640. AdOverlay y DailyStampCalendar usan los márgenes de `OverlayCanvas` con los mínimos de `GameScreen` (32/24 dp) + 8: el rótulo bajo la zona segura de arriba y «Cerrar»/«Volver al inicio» sobre la de abajo; con las pantallas de siempre no se mueve nada (solo acotan). Medido con 120/220 px: calendario 157-623 a 780 y 87-553 a 640 (límite 558,7). StuckModal en 16:9 al 130 % con 48 dp de barra (el 31 y el 640, rama B, es-419 y en-XA): compacto, entero entre las zonas seguras y con todos sus botones sobre la barra; lo que tapa de las cuias sigue siendo el límite aceptado en D-032 (añadido del 2026-10-20), sin cambio.
  4. **Brillos del río:** no era la primera activación sino cada una: activar el GameObject de una mota o un destello asigna (Recorder de GC.Alloc: 2 y 1 asignaciones en los fotogramas en que nacía una mota de polen en el 131 y el 171). Las motas y los destellos quedan siempre activos y se ocultan con `CanvasRenderer.cull` y alfa 0 (como el `enabled` de FxLayer; habilitar la Image también asignaba). Con eso, **sale `AmbientLayer.Frozen`** de `PhoneFixesTests` y de las pruebas de asignaciones de `StopLabelTests`, que pasan con el ambiente en marcha. `VisualGuardTests` salta las motas recortadas.
  5. **GDD:** 02 §5 (Q-T6T-5 resuelto, 4 dp [P]), GDD.md §6.3 pregunta 68 (4) resuelta, 06 §4.7 fila Lobby (las tarjetas no pasan de w − 32; 272 dp a 320 dp [P]).
- **Revisión del integrador (code-reviewer, CAMBIOS NECESARIOS), atendida:**
  - *Bloqueante 1:* la condición de Q-T6T-5 también se cumplía con Reiniciar en una línea y la banda compacta en TopRowY + 48-49, así que el contador saltaba a la izquierda a 360 dp en cualquier idioma. Ahora la regla (`GameScreen.CounterRightLimit`) solo actúa con la píldora en dos líneas. Tests: `HudLabelTests.CounterRightLimit_OnlyMovesLeftOfATwoLineRestartPill` (banda en 88, 89 y 90 con row = 40; falla sin la condición nueva) y `LongLabelLayoutTests.Es419_360_ThreeRowsWithTheTroopBand_CounterStaysRight`. Este último mira el 194 y el 200 en es-419 al 100 y al 130 %: al 130 % la banda va compacta y el contador sigue en x = W − 8 − ancho (el 194, x 296-352; el 200, x 314-352).
  - *Bloqueante 2:* los tests de zona segura pasaban con el código de antes. Ahora hay un caso nuevo para cada cota, y cada uno falla sin su cota (comprobado quitándola y volviéndola a poner):
    - rótulo del anuncio con 660 px de recorte arriba: baja de y 214 a 228 (zona segura + 8);
    - «Cerrar» en 1080 × 1600 px con 450 px de barra (432 × 640 dp): sube de 435 a 404;
    - calendario con la Cosecha doble en 1080 × 1600 px con 250 px de barra: panel en 66-532 en lugar de centrado en 87.
  - *Sugerencias:* los comentarios de `PhoneFixesTests` y `StopLabelTests` citan el mecanismo real (siempre activas y ocultas con `cull`); `PlaceDailyCard` llama a `PlaceLogbookCard` cuando cambia el alto de la tarjeta del Reto; Q-T6P-1 y Q-T6P-3 entran en la pregunta 68 de GDD.md §6.3 como (5) y (6), con la recomendada aplicada [P].
- **Tests nuevos:** `AudioQuickTogglesTests.Home_At320_TheDailyCardLeavesTheCrecidaClear_AndItsTextsFit` (es-419 y en-XA × 100/130, con los estados Jugar, Pendiente de validar y Seguir) y `Home_At360_TheDailyCardKeepsItsWidth`; `LongLabelLayoutTests.EnXA_360_Restart_ThreeRowsWithTheTroopBand_Counter` (194 y 200 × 100/130); `SafeAreaOverlayTests` (hoja de compra y anuncio, a 780 y 640); `DailyChallengeTests.Calendar_WithAGestureBar_StaysInsideTheSafeArea` (2340 y 1920 px); `StuckCompactTests.Stuck_16x9_At130_WithAGestureBar_InsideTheSafeArea` (31 y 640 × es-419/en-XA); captura explícita `AudioQuickSnapshotTests.CaptureHome320` (`t6p_home320_100/130`).
- **Verificación** (Spark, ARM64, `-Architecture arm64`):
  - línea base antes de tocar nada: EditMode **4.535 (4.532 + 3 omitidos)**; PlayMode **2.064 (1.770 + 294 omitidos)**, sin fallos, 56 min (la primera vuelta de PlayMode compiló ya parte de mis cambios: se paró y se repitió con el árbol limpio);
  - `dotnet build dotnet/AmazonFill.slnx -warnaserror`: 0 avisos; `dotnet test`: 1.002 (996 + 6), sin fallos; `python tools/validate_schema.py`: 116/116;
  - `unity-compile.ps1`: compila; EditMode **4.535 (4.532 + 3)**, sin fallos; PlayMode (`-TimeoutSec 7200`, 57 min) **2.087 (1.793 + 294 omitidos), sin fallos** (+23 en ejecución: 19 nuevos y 4 que dejan de ser explícitos; los 4 explícitos nuevos son la captura).
  - tras la revisión: `dotnet build -warnaserror` 0 avisos; `dotnet test` 1.002 (996 + 6); `validate_schema.py` 116/116; compila; EditMode **4.535 (4.532 + 3)**, sin fallos.
    - PlayMode, 1.ª vuelta (57 min, con el carril A en paralelo): **2.095 (1.800 + 294), 1 fallo**, `IntroTests.IntroTick_AllocatesNothing_AfterStart("noche_702")` en t = 1728. Ese test solo mide `IntroAnimation.Tick`, que este cambio no toca. Repetido solo dos veces (13/13), pasa.
    - PlayMode, 2.ª vuelta completa (55 min): **2.095 (1.801 + 294 omitidos), sin fallos**.
  - Capturas regeneradas y revisadas: `t6p_home320_100/130` en es-419 y en-XA (la tarjeta de 272 dp, nada se pisa) y `l4r_100/130_juego360_l200` en en-XA (el contador de la Piraña a la izquierda de «[Réíñíçíár ···]», bajo Canoas, sin tocarse). Las de 360 dp no cambian por construcción (la tarjeta, los anuncios y el calendario quedan igual; las motas ocultas no se pintan); no se regeneró la colección completa (es del carril A).
- **Preguntas (recomendada aplicada, [P]):**
  - **Q-T6P-1:** a 320 dp la tarjeta del Reto se estrecha (272 dp, no centrada: x 16-288) en lugar de mover la Crecida o encoger la tarjeta por los dos lados. (a) estrecharla hasta w − 32 [aplicada]; (b) centrada de 256 dp (32-288); (c) mover la Crecida bajo la tarjeta (no cabe sin bajar de 48 dp).
  - **Q-T6P-2:** con dos líneas en Reiniciar, el contador va a su izquierda (a 4 dp) en lugar de debajo de ella. (a) a la izquierda [aplicada]; (b) bajar la banda entera bajo la píldora (pisa la franja de la tropa).
  - **Q-T6P-3:** la tarjeta del Reto crece hacia arriba cuando sus textos no caben (hoy solo «Pendiente de validar» al 130 %: 103 dp). (a) crecer [aplicada; 02 §12]; (b) bajar del mínimo de 12 sp × escala (no).
- **Riesgos:** las motas y los destellos ocultos siguen en el lienzo (recortados, sin dibujarse): un barrido de gráficos que no mire `cull` los contaría; la hoja de compra no suma el mínimo de 24 dp de `GameScreen` sin barra (el parche de B10 suma el margen real; sin barra «Cancelar» queda a 20 dp del borde, como antes); el límite de D-032 (Aguas estancadas tapa cuias en 16:9) no cambia.

---

## 2026-10-24 · Integración de T6 carril 2 (escritorio)

- Fusionada en `integracion-fase1-fase2` (`4c67ee85`) sobre la Liga y el audio (`ddcfa7de`); solo chocó este archivo (las dos entradas, conservadas). code-reviewer del escritorio: APROBADO, sin choques con la Liga (la línea de puntos va dentro del panel de victoria, sin la cola de avisos) ni con la pausa de Música y Sonido.
- El riesgo `PlayGamesCloudBootTests.ALateCloud_…` (fallo por la fecha) queda resuelto: `economy_ts` de la nube relativo a hoy, en un commit aparte.
- Verificación en el escritorio (x64): dotnet build 0 avisos; dotnet test 1.002 (996 + 6); `validate_schema.py` 116/116; `apply_translations.py --check` OK; `rc_defaults.py --check` idéntico (254); compila; EditMode 4.535 (4.532 + 3); PlayMode 2.064 (1.770 + 294), sin fallos, 36 min; build de desarrollo OK con `check-manifest -Mode dev`, `check-16kb` y `check-no-unity-iap` OK.
- Queda el punto 7 (en el teléfono, un nivel de 14-16 cuias en el primer intento) y la pregunta 68 de GDD.md §6.3.

---

## 2026-10-24 · T6 · Carril 2: sitio para la tropa con 3 filas (`spark/t6-tropa-3filas`): HECHA con las decisiones del dueño (Q-T6T-3 y Q-T6T-4, 2026-10-24) · verificación completa (un fallo ajeno, por la fecha) · code-reviewer APROBADO · lista para integrar

- **Encargo:** `spark-audio-tablero.md`, carril 2 (puntos 1-7); D-046, punto 3. Rama desde `origin/integracion-fase1-fase2` en `3ab273f9`. Sin empujar (lo empuja el integrador).
- **Commits:** `55318941` (franja de la tropa con 3 filas; el suelo sin bajar, pendiente de Q-T6T-3), `257cc921` (las decisiones del dueño del 2026-10-24: franja de 56 dp, suelo bajado 20 dp y avisos en la franja) `875a8d4d` (la revisión del integrador: 07 §4.1 y §7.4, zona táctil, espera del aviso, globo de Pista, pregunta 68) y este, que solo apunta los hashes.
- **Decisiones del dueño (2026-10-24, en el chat):**
  - **Q-T6T-3 → «Bajar y mover avisos»:** con 3 filas el suelo baja 20 dp [P] (el panel a 11,5 dp de la bandeja); las burbujas «Te faltan {n}» y el aviso del tablero que ya no caben debajo van a la franja de arriba, sin pisar el panel ni la tropa y legibles al 130 % y en en-XA.
  - **Q-T6T-4 → «Franja de 56 dp»:** la franja de la rama pasa a 56 dp [P]; la tropa, a su tamaño, ya no entra en la fila del HUD (el test exige los 0,5 dp de `IntroTests`; la prueba explícita `…StrictlyOffTheHud_KnownIssue` desaparece: ahora es la prueba normal).
  - Q-T6T-2 se queda con la recomendada aplicada; Q-T6T-5, como riesgo.
- **Qué hace:**
  - `BoardLayout` (AF.UI): con 3 filas reserva `TroopBandDp` = 56 dp [P] entre `boardTop` y el panel y baja el suelo `ThreeRowFloorDropDp` = 20 dp [P], encogiendo las cuias lo justo; `BoardLayoutResult.TroopBand` lo dice. Ni la fila 1 elevada ni su cielo entran en el HUD. Si la franja no cabe sin bajar la cuia de `MinCuiaWidth` (28 dp; con la Cuia alta, u ≥ 14 dp de 07 §4.5), el layout de 3 filas es exactamente el de antes. La franja no cambia cuántas filas lleva un nivel (Q-T6T-2) y los tableros de 1 y 2 filas no cambian.
  - `GameScreen.SideTroopFor`: con la franja, la tropa va en su rama (primer intento); la tropa lateral de D-021 queda de respaldo donde no hay franja (16:9).
  - `BoosterTray.SetMissingRoom` y `GameScreen.UpdateMissingRoom`: la burbuja «Te faltan {n}» que pisaría el panel va a la franja, sobre su ranura a lo ancho y 2 dp sobre el panel; no se dibuja mientras la tropa está en la franja.
  - `GameScreen.PlaceToastInTroopBand`: el aviso del tablero que no cabe entre el tablero y la bandeja ni a su alto mínimo va a la franja, de su ancho de siempre, entre la fila del HUD (+2 dp) y el panel (−2 dp con su canto); sobre las burbujas si caben y, si no, las burbujas de la franja no se dibujan mientras dura y vuelven después. Los avisos de texto esperan a que la tropa se vaya (`PumpNotices`), como las tarjetas.
  - `BoardLayout.TroopBandOffForTests` (solo tests): mide el respaldo de antes en lienzos altos (tropa lateral, HUD compacto, excepción de D-028, el compromiso de la pregunta 47 y el muelle del rótulo).
- **Cifras medidas** (dp; borde del HUD en 88):
  - 360 × 780: franja 56; suelo en 640 (bandeja − 20); panel 144-648,5 con 11-15 cuias, a 11,5 de la bandeja; cuia **41,61** (antes 46,9); 16 cuias 39,2 (manda el ancho; panel 154,1-638,4); Cuia alta de 5 y Tinaja 35,30 (antes 39,8); Cuia alta de 6 30,66 (antes 34,6).
  - 360 × 800: panel 144-668,5 (11,5 de la bandeja); cuia 43,99; Cuia alta de 5 y Tinaja 37,32; de 6 32,41.
  - 360 × 720: cuia 34,5 con franja; con la Cuia alta de 6, sin franja (bajaría a 25,4).
  - 360 × 640 y 320 × 640: sin franja (con ella bajaría a 24,9): el layout de antes (30,24; con la Cuia alta, 28) y la tropa lateral o el dosel. 320 × 780: franja, 41,4 con 5 cuias por fila.
  - Paquetes 1-700 a 360 × 780 y 800: los 582 niveles de 3 filas (11 con Cuia alta) con la franja; la cuia más estrecha, 35,30. En 16:9, 281 de 3 filas, ninguno con franja.
  - La tropa en la rama (el 86 y el 143 a 1080 y 720 px, versiones larga y corta): 0 dp dentro de la fila del HUD (con 44 dp entraba hasta 3,6).
  - El 135 tras el rescate, a 360 × 780: «¡Una cuia más!» en la franja, en y = 90-142 (49 dp de placa), a 15 sp × escala, legible al 100 y al 130 % en es-419 y en-XA; las dos burbujas de dos líneas (29,5 dp, en y = 112,5-142) no caben con él y esperan a que se vaya. Sin el aviso, las burbujas en la franja, sobre el panel y bajo la fila del HUD.
- **Tests:** `BoardLayoutTests` (franja con 11-16, Cuia alta y Tinaja; el respaldo en 360 × 640, 320 dp y 360 × 720 con la Cuia alta de 6; las filas no cambian en ningún lienzo; ≤ 10 cuias idénticos a una referencia escrita aparte), `IntroChoreographyTests`, `IntroSideTroopScheduleTests` (todos los niveles de 3 filas de 1-700), `IntroThreeRowBranchTests` (PlayMode: el 86 y el 143 en la rama, franja de 56, 11,5 hasta la bandeja, nada en el HUD con 0,5 dp de tolerancia, nada en el panel, ≥ 8 dp de toda cuia, reintento en dosel; en 16:9, el 240 y el 335 sin franja ni rama), `ThreeRowBandNoticeTests` (PlayMode, nuevo: el aviso y las burbujas del 135 en la franja, al 100 y 130 % en es-419 y en-XA, y las burbujas esperando a la tropa) e `IntroThreeRowBranchSnapshotTests` (capturas `t6t_*`, explícitas). Los tests del HUD compacto, la excepción de D-028, la tropa lateral, el compromiso de la pregunta 47 (`MinTextSizeTests` y `PhoneFixesTests` del 135) y el muelle del rótulo (`StopLabelTests`) miden el respaldo con `TroopBandOffForTests`. En `PhoneFixesTests` y en las pruebas de asignaciones de `StopLabelTests` se congela el ambiente (los brillos del río pasan ahora por la franja y su primera activación asigna).
- **Revisión del integrador (code-reviewer, CAMBIOS NECESARIOS), arreglada:** 07 §4.1 (41,6 × 116,5 dp, u ≈ 20,8) y 07 §7.4, «Área táctil» (116,5 + 32 ≈ 148,5 dp); test de que la zona táctil de las cuias no llega a la bandeja (la más baja acaba en el pie de la última fila, 611,5 dp a 360 × 780, 48,5 dp sobre la bandeja) ni a la fila del HUD; test de la espera del aviso de texto a que la tropa se vaya (el 86); el globo de Pista sin `hint_supported` a la franja cuando pisaría el panel (aplicación de Q-T6T-3 [P]; en el 135, y = 96-142, delante de las burbujas de la franja si no cabe sobre ellas), con test; el aviso vuelve abajo si ni en la franja entera cabe legible (su texto a 12 sp: `Toast.LegibleHeightDp`); la banda de etiquetas compara con las burbujas en el sistema del layout (sin `_boardShift`); las [P] pendientes del dueño (Q-T6T-2, Q-T6T-5, Q-T6T-6 y Q-T6T-7) marcadas en 02 §5 y registradas como la **pregunta 68** de `docs/GDD.md` §6.3; añadido del 2026-10-24 a D-046, punto 3; «donde no cabe la franja» unificado (16:9, con 3 filas la franja cabe desde unos 666 dp de alto, y la Cuia alta de 6 en lienzos de menos de 750 dp) en 02 §5, 07 §6.4 y §7.4 y GDD.md §6.3, fila 22. Los brillos del río no se precalentaron (no es barato comprobar que una activación fuera de pantalla quita la asignación): queda como riesgo y los dos tests siguen con el ambiente quieto.
- **Preguntas que quedan:** Q-T6T-2 (recomendada aplicada: la regla de filas de 11-12 compara con el tablero de antes; se aparta de la letra de D-006); Q-T6T-5 (riesgo: en-XA a dos líneas, la píldora Reiniciar pisa el contador de Lluvia/Piraña); **Q-T6T-6** (nueva, recomendada aplicada) cuando el aviso y las burbujas no caben juntos en la franja, las burbujas esperan a que se vaya el aviso (3-7 s), en lugar de tapar el aviso o las burbujas; **Q-T6T-7** (riesgo) mientras dura, el aviso en la franja tapa las etiquetas de la banda del HUD que caen en ella (dificultad, «?», contador); las burbujas no: la banda de etiquetas se compacta o va pegada bajo la placa para no pisarlas (el 106: compacta al 100 %, pegada bajo la placa al 130 %), y el globo de Pista sin `hint_supported`, que va sobre su ranura, puede pisar el panel con 3 filas (no se movió: no es ni la burbuja ni el aviso).
- **Verificación** (Spark, ARM64, 64 GB; `-Architecture arm64`), con el código final:
  - línea base antes de tocar nada: EditMode **4.447 (4.444 + 3 omitidos)** y PlayMode **1.994 (1.709 + 285 omitidos)**, sin fallos;
  - `dotnet build dotnet/AmazonFill.slnx -warnaserror`: 0 avisos; `dotnet test`: 1.002 (996 + 6), sin fallos; `validate_schema.py`: 116/116;
  - `unity-compile.ps1`: compila; EditMode **4.477 (4.474 + 3)**, sin fallos;
  - tras la revisión del integrador (el último commit): compila; EditMode **4.477 (4.474 + 3)**, sin fallos; PlayMode (`-TimeoutSec 7200`, 49 min) **2.020 (1.730 pasan + 289 omitidos), 1 fallo ajeno al carril** (el mismo de abajo, por la fecha); `dotnet build` 0 avisos, `dotnet test` 1.002 (996 + 6), `validate_schema.py` 116/116; capturas: las mismas 5 fallan que en la línea base y cambian los mismos tableros de 3 filas (157);
  - antes de la revisión, PlayMode (`-TimeoutSec 7200`, 46 min): **2.011 (1.721 pasan + 289 omitidos), 1 fallo ajeno al carril**: `PlayGamesCloudBootTests.ALateCloud_WithAWinWhileWaiting_KeepsTheWin_AndNeverUploadsTheUnmergedLocal`, que también falla solo y depende de la fecha (su `T0` es el 2026-10-23 15:00 UTC y la nube gana por `economy_ts` = T0 + 1 día, que ya ha pasado); el carril no toca Save ni la nube. En la vuelta anterior (antes de las decisiones del dueño) PlayMode dio 2.008 sin fallos.
  - Capturas: las 254 explícitas, antes y después: 157 de tableros de 3 filas cambian como se espera; las 5 que fallan ya fallaban en la línea base.
- **Teléfono (punto 7):** sin teléfono en el Spark (`adb devices` vacío): pendiente del escritorio con un nivel de 14-16 cuias.
- **Revisión:** subagente code-reviewer, dos rondas. 1.ª: CAMBIOS NECESARIOS (07 §4.1 y §7.4 con las cifras viejas, sin test del aviso que espera a la tropa, y Q-T6T-2, 6 y 7 escritas como regla firme en 02 §5), atendidos en `875a8d4d` (con el globo de Pista en la franja como aplicación de Q-T6T-3 [P] y la pregunta 68 de GDD.md §6.3). 2.ª: **APROBADO**; sus sugerencias de documentación (resumen de `Toast.MinHeightDp`, el globo de Pista marcado como extensión en D-046, la nota en D-006 y «< 750» aquí) van en el último commit, sin cambios de código. Queda como sugerencia comprobar en `ThreeRows_TheTouchZones_…` que el lienzo de 720 px se aplicó.
- **Decisiones del dueño (2026-10-24, en el chat):** Q-T6T-3 «Bajar y mover avisos» y Q-T6T-4 «Franja de 56 dp». Pendientes: la pregunta 68 (Q-T6T-2, 6 y 7, recomendadas aplicadas con [P]; Q-T6T-2 se aparta de la letra de D-006) y el riesgo Q-T6T-5.
- **Teléfono (punto 7):** sin teléfono en el Spark (`adb devices` vacío); queda para el escritorio un nivel de 14-16 cuias en el primer intento.
- **Riesgos:** `PlayGamesCloudBootTests.ALateCloud_…` falla por la fecha (tarea aparte; no se toca aquí); la primera activación de los brillos del río en la franja asigna (Unity); las cuias de 3 filas son un 11 % más estrechas que antes a 360 × 780 (las ranuras táctiles no cambian); la suite de PlayMode pasa de 50 a ≈ 55 min: hay que lanzarla con `-TimeoutSec 7200`.

---

## 2026-10-24 · Integración de T6 carril 1 (escritorio)

- Fusionada en `integracion-fase1-fase2` (`666045d0`) sobre la Liga (`73823f7b`), sin conflictos de texto; code-reviewer del escritorio: APROBADO, sin choques con la Liga (el botón «Liga» va abajo a la izquierda y la columna de audio arriba a la derecha).
- Verificación en el escritorio (x64): dotnet build 0 avisos; dotnet test 1.002 (996 + 6); `validate_schema.py` 116/116; `apply_translations.py --check` OK; `rc_defaults.py --check` idéntico (254); compila; EditMode 4.505 (4.502 + 3); build de desarrollo OK (`dev.apk` 72,8 MB; `check-manifest -Mode dev`, `check-16kb` y `check-no-unity-iap` OK).
- PlayMode completo: 2.032, 1 fallo, `Home_FitsWithoutOverlaps…(640)`. El test no esperaba a que acabara el saludo de TiTino, que a 360 × 640 aparta la tarjeta del Reto: dependía de la hora. Arreglado en el test (espera el saludo) y añadido `Home_WithTheLeagueOn_TheLeagueButtonFitsWithoutOverlaps` (640/800/320 al 100 y al 130 %, con el botón «Liga» en la lista de no-solape). `AudioQuickTogglesTests` repetido: 22/22. Total: 2.038 (1.748 + 290).

---

## 2026-10-24 · T6 carril 1 · Música y Sonido con un toque (`spark/t6-audio-directo`): HECHA · verificación completa VERDE · code-reviewer APROBADO · lista para integrar

- **Encargo:** `spark-audio-tablero.md`, carril 1 (D-046, punto 2). Rama desde `origin/integracion-fase1-fase2` en `3ab273f9`.
- **Commits:** `6aa9d6cd` (la funcionalidad, GDD y tests), `2040bfd5` (ids de AF_LAYOUT de la pausa al final; `MapEntriesTests`) y `5a8601b3` (revisión: el Cántaro fuera de la tarjeta del Reto, el inicio a 320 dp, la píldora en dos líneas y el Dato de TiTino).
- **Qué hace:** `AudioToggles` (discos de madera de 40 dp en 48, nota y altavoz por código en `ProceduralArt.Audio`, tachados si están apagados; TalkBack «Música: activada»…). En la pausa, arriba junto a la X (la tarjeta pasa de 300 a 328 dp [P] para que el título quepa). En el inicio, en columna bajo el engranaje (`LobbyScreen`, no `HomeWindow`: Q-T6A-1). `GameBootstrap.ChooseAudio(key, on, source)` con `AudioQuickSettings`: mismo campo que Ajustes, en el acto, y Ajustes, la pausa (también la del Reto) y el inicio enseñan lo mismo. Vibración solo en Ajustes.
- **Disposición del inicio** (dp, x/y de la esquina de arriba a la izquierda, medida en `AudioQuickTogglesTests`):
  - 360 × 640: Ajustes 304,40; Música 304,92; Sonido 304,144; Cántaro 240,140 (56 × 56); tarjeta del Reto 32,204 (296 × 100); Crecida 330,200; Canoas 140,40; ventanita 16,96; píldora 78,112.
  - 360 × 800: lo mismo, con la tarjeta del Reto en 32,300.
  - 320 dp: Ajustes 264,40; Música 264,92; Sonido 264,144; Cántaro 200,140; tarjeta del Reto 12,225; Crecida 290,200; Canoas 132,40 (antes 140, bajo el engranaje); al 130 %, la píldora va en dos líneas (78,96, 178 × 45) y el Cántaro baja a y 145.
  - El Dato de TiTino no pasa del Cántaro; si el saludo o el Dato cruzan la tarjeta del Reto (360 × 640 al 130 %), la tarjeta se aparta mientras se ven.
- **Analítica:** `source ∈ {settings, pause, home}` en 08 §8.2 y `AnalyticsCatalog` (con test); la sonda `AF_STATE settings_change` lo escribe; 08 §8.2 dice que el audio aún solo sale por la sonda.
- **Textos:** `a11y_music_on/off` y `a11y_sound_on/off` en es, en y pt-BR (`borrador`, con `context` y `max_chars`).
- **GDD:** 02 §5, 07 §9.4 y 08 §8.2, con D-046.
- **Verificación** en `5a8601b3` (ARM64, 64 GB, Python 3.14): dotnet build 0 avisos; dotnet test 1.002 (996 + 6); `validate_schema.py` 116/116; `apply_translations.py --check` OK; compila; EditMode **4.453** (4.450 + 3; línea base 4.447); PlayMode **2.012** (1.725 + 287 omitidos; línea base 1.994). Una pasada de PlayMode quedó sin veredicto al llegar a los 3.600 s mientras corría en paralelo la de `spark-t6-tropa`; repetida con `-TimeoutSec 7200`: 2.829 s, verde.
- **Revisión:** subagente code-reviewer, dos rondas. 1.ª: CAMBIOS NECESARIOS (bloqueante: a 360 × 640 el Cántaro, colgado de Sonido, pisaba la esquina y el sello de la tarjeta del Reto; sugerencias: pausa y píldora a 320 dp al 130 %, Dato de TiTino, listas en `EmitLayout`, nota en 08 §8.2), todo atendido en `5a8601b3`. 2.ª: **APROBADO**; da por aceptables la exclusión del par Crecida–tarjeta del Reto a 320 dp (anterior a la rama) y ocultar la tarjeta bajo la nota de TiTino (02 §12; `TickDaily` ya la ocultaba en casos parecidos). Pide al integrador repetir PlayMode al integrar, regenerar las capturas `[Explicit]` de la pausa y el inicio antes de la prueba visual y ver en el teléfono a 360 × 640 cómo se aparta la tarjeta con el saludo.
- **Commit de estado:** `f0fc2dab` y el de esta línea.
- **Preguntas:** Q-T6A-1 (el inicio es `LobbyScreen`, el encargo decía `HomeWindow.cs`; aplicada: `LobbyScreen`); Q-T6A-2 (la columna bajo el engranaje y el Cántaro a la izquierda de Sonido, no de Música: a la altura de Música pisaría la píldora de casa; si la píldora va en dos líneas, el Cántaro baja hasta ella); Q-T6A-3 (`af_settings_change` de audio sigue solo en la sonda, como desde Ajustes; aplicada: sin envío nuevo a la analítica real, anotado en 08 §8.2).
- **Riesgos:** a 320 dp la Crecida (x 290-316) y la tarjeta del Reto (x 12-308) ya se cruzaban antes de D-046 (el test lo excluye y lo dice); la tarjeta del Reto se oculta mientras una nota de TiTino la cruza (comportamiento nuevo en pantallas bajas); el integrador debe casar la columna con el botón «Liga»; el solape Crecida–tarjeta del Reto a 320 dp pide una tarea propia; `ProjectSettings/GooglePlayGameSettings.txt` sale modificado en el worktree solo por finales de línea de Unity y no se commitea.

---

## 2026-10-22 · T5-BILLING · Plugin propio de Play Billing 9.0.0 en lugar de Unity IAP (`spark/t5-billing`): HECHA · verificación completa VERDE (rama y rama + parches) · lista para integrar

- **Encargo:** `spark-reto-del-rio.md` §15; D-040 y su añadido de Q-IAP-1 (2026-10-22): `com.android.billingclient:billing` **9.0.0** directo, el permiso `com.android.vending.BILLING` (ya aprobado) y quitar Unity IAP y sus tres dependencias de Unity Services. Nota: `docs/dev/iap.md` (reescrita).
- **Rama:** `spark/t5-billing`, desde `origin/integracion-fase1-fase2` en `0ad1de46`; una sola fusión al final con `509f6d24` (appcheck-release, FX-130 y la traducción 3), con los parches rehechos encima. Empujada sin `--force`.
- **En la rama:**
  - nuevos: `Assets/_Project/Plugins/Android/AfBilling.androidlib/` (Java, sin Kotlin; `namespace 'com.amazonfill.billing'`; billing 9.0.0 `compileOnly`), `Assets/_Project/Plugins/Editor/AfBillingDependencies.xml` (EDM4U la escribe en `mainTemplate.gradle`), `Scripts/PlayBilling/` (`AF.IAP.PlayBilling`: `NativePlayStorePort`, `IBillingBridge`, `BillingRows`, `BillingCallbacks`, `AndroidBillingBridge` solo con `UNITY_ANDROID && !UNITY_EDITOR`, `PlayBillingFactory`) y sus tests (`AF.IAP.PlayBilling.Tests`: 31 + 3 + 6 casos), `Scripts/IAP/Tests/PlayBillingPurchaseTimeTests.cs` (2);
  - cambiados (sin cambios de contrato de `PlayBillingClient`, `BillingReadyStoreService`, `VerifiedStoreService` ni `ConsentStoreService`): `IPlayStorePort.Acknowledge` y `PlayOrder.PurchasedUtc`; `PlayBillingClient.AcknowledgeLocal` reconoce sin consumir también los consumibles (Q-IAP-2 se reabre, como pedía Q-F3-3 3) y pasa `purchaseTime` a la cola; `VerifiedStoreService.Resume` vuelve a pedir el `deferred_ack` que Play no hizo. Tests cambiados, uno por uno, en `iap.md` §2.1 (+1 test nuevo en `PlayBillingVerifiedTests`).
- **Parches** (`spark-enganches/`, LF, `git -c core.autocrlf=false apply`, en este orden; tocan archivos distintos y aplican limpios sobre la rama):
  1. `BILL-bootstrap.diff`: `AF.UI.asmdef` y `GameBootstrap.Backend.cs` (la tienda real pasa al plugin) y dos comentarios de `GameBootstrap.cs`.
  2. `BILL-manifest.diff` (necesita el 1): quita `com.unity.purchasing` y, con el `packages-lock.json` que regeneró Unity, `services.core` 1.18.0, `services.deployment` 1.7.2 y `deployment.api` 1.1.3; borra `Scripts/UnityIap/` (`AF.IAP.UnityIap`, `AF_UNITY_IAP`) y `Assets/Resources/` (`BillingMode.json`); `mainTemplate.gradle` y `ProjectSettings/AndroidResolverDependencies.xml` tal como los escribió EDM4U.
  3. `BILL-ci.diff`: `ci/check-no-unity-iap.ps1` (nuevo, autotest de 9 casos) y comentarios de `check-manifest.ps1` (BILLING en la lista blanca y `-RequireBilling` en release, sin cambios de lógica).
  4. `BILL-gdd.diff`: 08 §1, §2.2, §7 #3 y #9, §10.1, §10.3, §13.1 #4 y §13.3 (la fila pasa a «Google Play Billing», sin Unity).
  5. `BILL-claude.diff`: recuentos (EditMode 4.315) y mapa de `CLAUDE.md`.
  6. `BILL-rules.diff` (del dueño): `.claude/rules/sdk.md` y `.claude/agents/sdk-integrator.md`.
- **Enganche de texto** (`.github/workflows/ci.yml`): el paso `pwsh -NoProfile -File ci/check-no-unity-iap.ps1 build/Android` tras la compuerta de clases prohibidas (`iap.md` §6).
- **Permisos del manifiesto fusionado** (dev.apk y ensayo, AAB y APK): `INTERNET`, `ACCESS_NETWORK_STATE`, `VIBRATE`, `com.android.vending.BILLING`, `WAKE_LOCK`, `BIND_GET_INSTALL_REFERRER_SERVICE`, `POST_NOTIFICATIONS` y el propio `com.amazonfill.dev.DYNAMIC_RECEIVER_NOT_EXPORTED_PERMISSION`. **Ninguno nuevo.**
- **Verificación** (ARM64, 64 GB; detalle en `iap.md` §6), sobre la fusión: dotnet build 0 avisos; dotnet test 1.002 (996 + 6); `validate_schema.py` 116/116; compila (rama y rama + parches); EditMode **4.315 (4.312 + 3)** en la rama y con los parches (antes, 4.272); PlayMode **1.940 (1.669 + 271)** con los parches, sin fallos; `check-manifest --self-test` 50 OK; `check-no-unity-iap --self-test` 9 OK (y FALLO, como debe, sobre el `dev.apk` de `spark/t5-iap` con Unity IAP); `dev.apk` 70,37 MB con `check-manifest -Mode dev`, `check-no-unity-iap` y 16 KB OK; **ensayo: AAB 54,20 MB frente a 60,23 MB con Unity IAP (−6,03 MB)**, APK 59,63 MB, `size-gate`, `check-manifest -Mode dev -RequireBilling`, `check-no-unity-iap`, `check-release-classes` y 16 KB OK.
- **Revisión:** skill `code-review` con las reglas de `code-reviewer.md` (el subagente no está disponible aquí), seis rondas centradas en no acreditar dos veces ni sin verificación, no consumir sin acreditar y que el plugin solo hable con Play: CAMBIOS NECESARIOS ×5 (deferred_ack que no se repetía, estados de la hoja abierta con desconexiones, reconexión automática, fidelidad del puerto falso), **APROBADO** en la sexta; las sugerencias que quedaban se aplicaron (compras de varias unidades, espera de reconexión, prioridad de códigos de la compuerta) o están en «Riesgos» de la nota.
- **Preguntas** (`iap.md` §4.1): **Q-BILL-1** dependencias Maven de `billing:9.0.0` (las mismas que ya entraban con Unity IAP; ningún permiso; **aprobadas por el dueño el 2026-10-22**: «Apruebo las dependencias Maven de com.android.billingclient:billing 9.0.0 de Q-BILL-1 con las versiones que resuelve Gradle; anótalo para D-040.» El integrador las anota en D-040); Q-BILL-2 registros de diagnóstico de la biblioteca a Google (recomendada aplicada en `BILL-gdd.diff`: es Google Play y va en Data Safety; lo revisa el abogado); Q-BILL-3 una sola oferta por producto (dueño, en Play Console); Q-BILL-4 sin compras de varias unidades (dueño; el plugin las salta); Q-BILL-5 bajar `sec_purchase_deferred_ack_h` a 48 h (recomendada, **no aplicada**: toca 08 §9 y los defaults generados).
- **Modo de ensayo** con licencias de prueba: `iap.md` §3, adaptado (con el caso del `deferred_ack` y la recarga de escena); no ejecutado (D-034).

---

## 2026-10-22 · T5-IAP · Unity IAP 5.4.4 (Play Billing) detrás de `IBillingClient` (`spark/t5-iap`): HECHA · verificación completa VERDE (rama y rama + parches) · lista para integrar

- **Encargo:** `spark-reto-del-rio.md` §14; D-040 (paquete `com.unity.purchasing` 5.4.4 y permiso `com.android.vending.BILLING`). Nota: `docs/dev/iap.md`.
- **Rama:** `spark/t5-iap`, desde `origin/integracion-fase1-fase2` en `741595c7`; una fusión al final con `d9c8e955` (B9 y las notificaciones reales), con los parches rehechos encima. Empujada sin `--force`.
- **En la rama** (archivos nuevos): `Scripts/IAP/PlayStorePort.cs`, `PlayBillingClient.cs`, `BillingReadyStoreService.cs`, `DelegatePlayerUid.cs`, `IStoreDecorator.cs`; `Scripts/UnityIap/` (`AF.IAP.UnityIap`: `UnityIapStorePort.cs`, `PlayBillingFactory.cs`); `Scripts/Backend/Firebase/FirebaseAuthUid.cs`; tests `Scripts/IAP/Tests/FakePlayStorePort.cs`, `PlayBillingClientTests.cs` (37), `PlayBillingVerifiedTests.cs` (19) y `Tests/PlayMode/PlayBillingPlayModeTests.cs` (2). `VerifiedStoreService` no cambia de contrato (solo una propiedad aditiva en su evento, en el parche de analítica).
- **Parches** (`spark-enganches/`, LF, en este orden): `IAP-manifest.diff` (manifiesto, `packages-lock.json`, `Assets/Resources/BillingMode.json` y BILLING en la lista blanca de `ci/check-manifest.ps1` con su autotest y la cita a D-040), `IAP-bootstrap.diff` (`AF.UI.asmdef`, `GameBootstrap.Backend.cs`, una línea en `GameBootstrap.Consent.cs` y otra en `GameBootstrap.cs`), `IAP-analytics.diff` (`VerifiedStoreService.cs`, `ConsentStoreService.cs`, `AnalyticsStoreService.cs` y su test; arregla también un fallo anterior de B12: la analítica no oía lo acreditado tarde con el consentimiento activo), `IAP-price.diff` (Q-IAP-3 (a)) e `IAP-gdd.diff` (08 §7 #3, §10.1 y §13.3, con la fila de Data Safety).
- **Permisos del manifiesto fusionado:** solo uno nuevo, `com.android.vending.BILLING` (de `com.android.billingclient:billing:9.0.0`). Ningún otro.
- **Verificación** (ARM64; detalle en `docs/dev/iap.md` §6): dotnet build 0 avisos; dotnet test 1.002 (996 + 6); `validate_schema.py` 116/116; EditMode 4.264 (4.261 + 3) en la rama y 4.267 (4.264 + 3) con los parches; PlayMode 1.888 (1.633 + 255) en los dos; `check-manifest --self-test` 50 OK; build de humo con los parches: AAB 48,16 MB (+6,58 MB frente a la rama), `size-gate` OK, `check-manifest -Mode dev` OK con PBL 9.0.0, 16 KB OK.
- **Revisión:** skill `code-review` (reglas de `code-reviewer.md`), tres rondas: CAMBIOS NECESARIOS (10 hallazgos), CAMBIOS NECESARIOS (10), **APROBADO** (sugerencias, casi todas aplicadas; las demás, en «Riesgos» de la nota).
- **Preguntas** (`docs/dev/iap.md` §4; en todas va aplicada la recomendada como propuesta, sin decidir): Q-IAP-1 telemetría de compras de Unity IAP a Unity (**bloquea el soft launch** hasta que decidan el dueño y el abogado); Q-IAP-2 `deferred_ack` de consumibles (5.4.4 no lo permite: no se hace); Q-IAP-3 monedas fuera de 05 §4.2; Q-IAP-4 no consumibles con otro UID (409 en cada arranque; tarea del backend); Q-IAP-5 Play solo con la edad decidida; Q-IAP-6 paquetes que arrastra Unity IAP (`services.core` 1.18.0, `services.deployment` 1.7.2, `deployment.api` 1.1.3): **aprobados por el dueño el 2026-10-22** («Apruebo com.unity.services.core 1.18.0, com.unity.services.deployment 1.7.2 y com.unity.services.deployment.api 1.1.3, dependencias de Unity IAP 5.4.4 (Q-IAP-6); anótalo para D-040.»): el integrador los anota en D-040; Q-IAP-7 `BillingMode.json`.
- **Modo de ensayo** con licencias de prueba: `docs/dev/iap.md` §3 (no ejecutado: la app no existe en Play Console, D-034).

---

## 2026-10-21 · B9 · Accesibilidad: Reducir movimiento, Alto contraste, escala de fuente, C9, simulador de daltonismo y TalkBack (`spark/t5-b9`): HECHA · verificación completa VERDE (rama y rama + parches) · lista para integrar

- **Rama:**
  - `spark/t5-b9`, desde `origin/integracion-fase1-fase2` en `6c1406e3`.
  - Una fusión con `bacf72a8` (B11, B12 y la reserva del 20 %) antes de la verificación final. Los parches se rehicieron sobre esa base, con el arrastre C10 de B11 y `OptionRow` de B12.
  - Empujada, sin `--force`.
- **Nota:** `docs/dev/accesibilidad.md`. Recoge el canon, qué hace, los archivos, los parches, los choques, las preguntas Q-B9-1 a Q-B9-17 y los riesgos.
- **Canon:**
  - 02 §3.1 (C9) y §12;
  - 07 §3, reglas 3 y 5, y §7.8;
  - 08 §9 (`ui_reduce_motion_default` y `anim_reduce_motion_factor`), §14.2 y §14.3;
  - D-033, Q-N4 (a).
- **Qué hace:**
  - **Reducir movimiento** (`settings.reduce_motion` [P]: `auto`, `on` u `off`):
    - en `auto`, sigue a Remote Config o [P] al teléfono con «Quitar animaciones»;
    - vertidos y pasos de mecánica × `anim_reduce_motion_factor`, una sola vez;
    - sin sacudida, sin onda del agua y sin confeti;
    - llega al Reto, al Mapa y a la pantalla de arranque.
  - **Alto contraste** (`settings.high_contrast` [P]): el patrón al 35 %. El contorno de 1,5 px queda pendiente: Q-B9-6.
  - **Escala de fuente del sistema** hasta el 130 %:
    - `ISystemScale` con su falsa en el Editor;
    - `AndroidSystemScale`, sobre el módulo de accesibilidad de Unity, que ya estaba en el manifiesto, más JNI.
  - **C9:** 400 ms sin moverse (< 24 dp [P]) muestra los nombres. Al soltar no selecciona. En la Tinaja, solo lo visible.
  - **TalkBack completo:**
    - cuias por ranura con su etiqueta de 02 §12, después la bandeja («Deshacer, 2 restantes») y después lo demás;
    - la selección se anuncia;
    - cada botón, con su etiqueta: la pausa, la X, el engranaje y las filas con marca;
    - los velos «toca para seguir», como «Continuar» [P];
    - lo tapado y lo recortado no se publica;
    - sin paquete nuevo.
  - **Simulador de daltonismo** [P] (Machado 2009 + CIEDE2000):
    - test de EditMode en la suite;
    - captura `[Explicit]` sobre el shader real, con informe.
    - Hallazgos: 7-10 (protanopía) y 5-14 (deuteranopía) quedan cerca y no son pares de riesgo (Q-B9-13).
- **En la rama:** archivos nuevos, sin tocar ninguno del escritorio:
  - `Scripts/Platform/A11yConfig.cs`, `A11yRules.cs`, `ISystemScale.cs` y `FakeSystemScale.cs`;
  - `Scripts/RemoteConfig/Binding/A11yConfigBinder.cs`;
  - `Scripts/UI/A11y/*` (11 archivos);
  - `Scripts/UI/GameScreen.A11y.cs` y `SettingsScreen.A11y.cs`;
  - tests: `Tests/EditMode/A11y/*` (5 tests y el ayudante `B9Loc`), `Tests/PlayMode/A11yScannerTests.cs`, `ColorVisionShaderTests.cs` (`[Explicit]`) y `B9LocPlay.cs`;
  - `docs/dev/traspaso/spark-enganches/B9-strings.csv` (19 claves).
- **Parches** (en `docs/dev/traspaso/spark-enganches/`, en LF, en este orden, con `git -c core.autocrlf=false apply`):
  1. `B9-profile.diff`;
  2. `B9-rc.diff`;
  3. `B9-ui.diff` (necesita el 1 y el 2);
  4. `B9-loc.diff`.
  - El detalle, en la nota §4.
  - Escena y prefabs, sin cambios.
- **Revisión** (skill `code-review` con las reglas de `.claude/agents/code-reviewer.md`; el subagente no estaba disponible):
  - 10 rondas, arregladas en `ea672065`…`064f6c52`:
    - nombres de fruta sin aprobar;
    - doble factor en los pasos de mecánica;
    - `??` sobre objetos de Unity;
    - el Reto un fotograma tarde;
    - el foco de TalkBack;
    - tocar a través de la pausa o de otro lienzo;
    - velos que se van;
    - relés de toque;
    - etiquetas de solo icono;
    - la Luciérnaga con el lector;
    - C9 con varios dedos, velos y touch-ups perdidos;
    - el orden de lectura;
    - tests de la onda, el confeti, el Mapa y los velos.
  - **Última ronda completa: APROBADO.** Las sugerencias no bloqueantes que quedan están en la nota (§2.4 y §6).
- **Verificación:** ARM64, `-Architecture arm64`, un solo Unity a la vez, sobre la base fusionada (`bacf72a8`).

  | Dónde | Comando | Resultado |
  |---|---|---|
  | rama + parches | `dotnet build -warnaserror`; `dotnet test`; `validate_schema.py` | 0 errores y 0 avisos; **1.002 (996 + 6), 0 fallos**; 116/116, sin cambios en `tools/out/schema_example_08_4_1.json` |
  | rama + los cuatro parches | `unity-compile`; EditMode; PlayMode | 0; **4.193 (4.190 + 3), 0 fallos** (7,9 min); **1.855 (1.605 + 250), 0 fallos** (37 min) |
  | rama sola (`4babc262`) | `unity-compile`; EditMode; PlayMode | 0; **4.184 (4.181 + 3), 0 fallos** (4,9 min); **1.840 (1.590 + 250), 0 fallos** (36 min) |
  | rama, `[Explicit]` con gráficos | `unity test . --mode PlayMode --architecture arm64 --filter AF.Tests.PlayMode.ColorVisionShaderTests` | 1/1; capturas e informe en `Logs/snapshots/es-419/cvd/` |

  Sin fallos de tiempo que relanzar.
- **Preguntas para el dueño:** Q-B9-1 a Q-B9-17 (nota §5). En todas se aplicó la (a), la recomendada.
- **Riesgos:**
  - TalkBack sin probar en el teléfono [NV] (marcos, relectura, doble toque);
  - el contorno de 1,5 px de Alto contraste (Q-B9-6);
  - TalkBack y la lista de Ajustes que se desplaza (Q-B9-17);
  - las capturas de las filas nuevas de Ajustes, pendientes.

---

## 2026-10-21 · B12 · Consentimiento y edad, con implementaciones falsas (`spark/t5-b12`): HECHA · verificación completa VERDE · lista para integrar

- **Rama:** `spark/t5-b12`, desde `origin/integracion-fase1-fase2` en `4a9a42a0`.
  - Sin fusiones: la integración no se movió durante el bloque (`git fetch` antes de la verificación final).
  - Empujada, sin `--force`.
- **Nota:** `docs/dev/consentimiento.md`. Recoge:
  - qué hace;
  - los archivos y los parches;
  - lo que queda para los SDK;
  - las preguntas Q-B12-1 a Q-B12-16;
  - los hallazgos para el GDD;
  - las sugerencias abiertas de la revisión (§6).
- **Canon:**
  - 08 §2.3 (arranque), §4.3 (`profile.consent`), §7 («Consentimiento»), §8.1-§8.2 (`consent_profile`, `af_consent` y `af_settings_change`) y §9 (las 5 claves `consent_*` y `biz_*`);
  - 05 §7;
  - 06 §2.1, regla 3;
  - Q-T3 (a) y Q-T4 (a) de D-033.
- **Qué hace:**
  - **Pantalla de edad neutral:** tres bandas iguales y ninguna marcada.
    - `<13`: bloqueo en la 1.0 («Pide a un adulto que instale el juego», sin sesión de juego).
    - `13-17`: `npa`, contenido G y doble confirmación de compras.
    - `18+`: normal.
  - **Consentimiento propio:** finalidades separadas y «Rechazar todo» al mismo nivel que «Aceptar todo».
    - Perfil legal por SIM → región → `default` estricto.
    - UMP decide los anuncios en el EEE, el Reino Unido y Suiza.
  - `profile.consent` como prueba LGPD.
  - **Flags:**
    - a la analítica, con la recogida apagada hasta decidir (B7);
    - a los anuncios: TFCD, `npa`, G y sin inicializar hasta decidir;
    - a la tienda: sin compras con `<13` o sin decidir, falla cerrada.
  - **Compras de 13-17:** segunda confirmación con el precio de la tienda antes de la hoja de Google Play. No se confirma lo que no se puede comprar.
  - **Ajustes:**
    - «Privacidad y datos», que cambia o retira, y con UMP reabre su mensaje;
    - «Corregir mi edad», una vez.
  - **Textos legales:** [P], en `borrador` con la nota «pendiente del abogado» (Q-B12-5).
  - Sin SDK, sin paquetes, sin permisos (la SIM se lee sin `READ_PHONE_STATE`) y sin red.
- **Commits** (del más antiguo al más reciente):
  - `ac8e84bd`: la lógica sin motor, los decoradores, el lector de Remote Config, las pantallas, Ajustes, la sonda y los tests;
  - `1a9d401f`: los parches y la nota;
  - `ed31a5a8`, `aea499f4` y `8008af94`: los arreglos de las revisiones 1, 2 y 3;
  - `9ecacbd0`, `4ebfdb95` y `06f20442`: los parches regenerados tras cada revisión;
  - el commit de esta entrada.
- **En la rama** (archivos nuevos; ningún archivo del escritorio cambia):
  - `Scripts/Platform/ConsentModel.cs`, `ConsentConfig.cs`, `ConsentPolicy.cs` y `ConsentFlow.cs`;
  - `Scripts/Ads/ConsentAdService.cs` y `Scripts/IAP/ConsentStoreService.cs`;
  - `Scripts/RemoteConfig/Binding/ConsentConfigBinder.cs`;
  - `Scripts/UI/ConsentText.cs`, `ConsentScreen.cs`, `AndroidSimCountry.cs`, `SettingsScreen.Consent.cs`, `GameBootstrap.Consent.cs` y `DevProbe.Consent.cs`;
  - tests: `Tests/EditMode/ConsentTests.cs`, `Scripts/Ads/Tests/ConsentAdServiceTests.cs`, `Scripts/IAP/Tests/ConsentStoreTests.cs` y `Tests/PlayMode/ConsentScreenTests.cs`.
- **Parches** (`docs/dev/traspaso/spark-enganches/`, en LF, en este orden, con `git -c core.autocrlf=false apply`):
  1. `B12-profile.diff`: `Profile.Consent`, `ProfileJson` (tolerante), `ProfileConsentStore` y `ProfileConsentTests`.
  2. `B12-rc.diff`: `[RemoteConfigKey]` en `ConsentConfig`, `RcConfigBinder.BoundKeys`, `RcConfigs.Consent` y `RcConfigBinderTests`.
  3. `B12-ui.diff` (necesita el 1 y el 2):
     - en `GameBootstrap.cs`:
       - envolver anuncios y tienda;
       - `BlockedAtBoot`, `SetUpConsent` y `StartConsentAtBoot`/`WaitConsentAtBoot` antes de `AppFlow.Start`;
       - el Atrás;
       - las filas de Ajustes;
     - `GameBootstrap.Consent.cs`, con `_rcConfigs.Consent`;
     - `GameBootstrap.Sessions.cs` (`ConsentHoldsSession`);
     - el comentario de `GameBootstrap.Telemetry.cs`;
     - `SettingsScreen.cs`;
     - `PlayModeHelpers.cs`: `ConsentEnabledOverride ??= false` y `LoadAppWithConsent`;
     - `ConsentBootTests` (PlayMode, la escena real).
  4. `B12-loc.diff`: 23 claves en `strings.csv`, por orden ordinal, y `ConsentText.AllKeys` en `LocTablesTests.UiTextKeys`.
  - Sin parche de `AppFlow`: el paso «Consent» de 08 §2.2 corre con `AppFlow` en `Boot` (nota §2.2).
- **Choques con B8** (`spark/t5-b8`, sin integrar):
  - `SettingsScreen.EmitLayout`: la suma de `n`, a mano: `+ AccountLayoutCount + ConsentLayoutCount`;
  - `LocTablesTests` y `ProfileJson` tocan puntos distintos;
  - `CloudMerge` (B8) debería conservar el `consent` del dispositivo (Q-B12-8).
- **Revisión** (skill `code-review` con las reglas de `.claude/agents/code-reviewer.md`; el subagente no estaba disponible):
  - **1.ª ronda: 10 hallazgos, arreglados:**
    - UMP forzaba `npa`;
    - el país desconocido no era estricto;
    - `<13` desde Ajustes dejaba la sesión viva; ahora recarga la escena y para en `BlockedAtBoot`;
    - sin enlace de UMP en Ajustes;
    - la doble confirmación antes de `CheckPurchasable`;
    - una confirmación tapada se quedaba colgada;
    - el perfil y la versión de la corrección no coincidían con `af_consent`;
    - una sola fuente de `ConsentConfig`;
    - la tienda falla cerrada;
    - código muerto.
  - **2.ª ronda: 10 hallazgos, arreglados:**
    - un perfil legal distinto vuelve a preguntar;
    - 13-17 con UMP sin la restricción;
    - la sonda `npa`/`ump`;
    - los flags una vez por decisión;
    - la tienda recomprueba los flags al responder;
    - los textos con quién habla y la captura pendiente (Q-B12-16);
    - el texto de `consent_ads_ump`.
  - **3.ª ronda: 1 bloqueante y 6 menores, arreglados:**
    - la línea de anuncios sigue a los flags;
    - `Flags` con el perfil y el país de ahora;
    - Play Games solo decidido;
    - las claves de país en mayúsculas;
    - sin avisos con la edad pendiente;
    - la tabla de la nota.
  - **4.ª ronda: APROBADO**, con 8 sugerencias que quedan en la nota §6, la más relevante:
    - `ConsentHoldsSession` no cubre el segundo de la pantalla de arranque de quien debe volver a decidir.
- **Verificación** (ARM64, `-Architecture arm64`; un solo Unity a la vez):

  | Dónde | Comando | Resultado |
  |---|---|---|
  | rama (`06f20442` + entrada) | `dotnet build -warnaserror`; `dotnet test`; `validate_schema.py` | 0 avisos y 0 errores; **1.002 (996 + 6), 0 fallos**; 103/103, sin cambios en `tools/out/schema_example_08_4_1.json` |
  | rama | `unity-compile`; EditMode; PlayMode | 0; **3.821 (3.818 + 3), 0 fallos** (4,9 min); **1.726 (1.489 + 237), 0 fallos** (34,5 min) |
  | rama + los cuatro parches | `unity-compile`; EditMode; PlayMode | 0; **3.830 (3.826 + 3) y 1 fallo de tiempo** (`IntroArtTests.FruitFlight…GeneratedWithin10ms`), que relanzado con filtro da 9/9 (no es de B12); **1.733 (1.496 + 237), 0 fallos** (34,5 min) |

- **Preguntas para el dueño** (se avanzó con la recomendada; detalle y opciones en la nota §4):

  | Id | Pregunta | Recomendada |
  |---|---|---|
  | Q-B12-1 | Pantalla de edad: tres botones o año de nacimiento | (a) tres botones |
  | Q-B12-2 | ¿Repetir la edad al subir los textos? | (a) no, solo las finalidades |
  | Q-B12-3 | Bloqueo `<13` definitivo | (a) sí |
  | Q-B12-4 | Diagnóstico sin interruptor | (a) sí, pendiente del abogado; sin efecto desde D-057 (añadido del 2026-10-27): interruptor en Ajustes (T-L6) |
  | Q-B12-5 | Estado «pendiente del abogado» | (a) `borrador` + nota |
  | Q-B12-6 | Doble confirmación: una pantalla más | (a) |
  | Q-B12-7 | Analítica con 13-17 | (a) se puede aceptar |
  | Q-B12-8 | `consent` en la nube | (a) es del dispositivo |
  | Q-B12-9 | Analítica en UMP | (a) en la pantalla propia |
  | Q-B12-10 | `consent.age_band_corrected` | anotarlo en 08 §4.3 |
  | Q-B12-11 | `af_settings_change.value` | `choices` o `age` |
  | Q-B12-12 | El encargo dice que `INTERNET` no está aprobado; D-036 sí lo aprobó | sin efecto en B12 |
  | Q-B12-13 | Lo más estricto sin país | (a) `default` + `npa` sin elección |
  | Q-B12-14 | Extraer `OptionRow` | (a) al integrar |
  | Q-B12-15 | Otro perfil legal: volver a preguntar | (a) sí |
  | Q-B12-16 | Capturas de los textos nuevos | (a) el integrador con `SnapshotKit` |

- **Riesgos:**
  - **Legal:**
    - todos los textos son borradores sin el abogado;
    - la autenticación de Google Play no se puede forzar desde el cliente [NV];
    - Play Age Signals sin implementar;
    - la neutralidad de la pantalla de edad la valida el abogado.
  - **SDK:** pasar los flags reales a MAX (TFCD, `npa`, G, el formulario de UMP) y al sumidero de Firebase [NV].
  - **Sesión:** la del arranque en frío se cuenta antes de la pantalla de edad (`stats.sessions`) [P].
  - **Bloqueo:** con `<13`, la recarga de la escena reutiliza `RebuildUi`, cuyo `PersistNow` pone «idioma» en el log.
  - **Ajustes en el teléfono:** probar la fila de Privacidad al 130 % y en 16:9 (sin capturas en el Spark).

---

## 2026-10-21 · B11 · Tutorial, Sin canoas y contenido (`spark/t5-b11`): HECHA salvo la reserva del 20 % (parada: Q-B11-7) · verificación completa VERDE (rama y rama + parches) · lista para integrar

- **Rama:** `spark/t5-b11` (local `t5-b11`), desde `origin/integracion-fase1-fase2` en `2ed45753`; fusiona la integración una sola vez, al final: `19caed28` (F3-5, el canon «1 Marea + 30» y D-039) en `abde6db6`, sin conflictos. Empujada sin `--force`.
- **Nota del bloque:** `docs/dev/tutorial-b11.md` (qué hace, archivos, parches, para el integrador, cambios para el GDD, preguntas Q-B11-1 a Q-B11-12 y riesgos).
- **Decisiones del dueño aplicadas:** D-033, Q-N2 (a) (V3: `minus_color` desde el 31 en el generador) y Q-E4 (a) (el texto de la tarjeta de Canoas del 31). **D-038 respetado y cubierto por tests:** con `biz_feature_lives` apagado no hay tarjeta de Canoas (`CanoesCardFlowTests.WithoutLives_NoCard_D038`), el tooltip del 6 y del 8 pierde la frase de las canoas (`TutorialTipsHooksTests.Level6_WithoutLives_TheTipDropsTheCanoeSentence_D038`) y Sin canoas no se abre, así que «Volver al Mapa del río» no hace nada (`NoCanoesMapTests.WithoutLives_NoCanoesNeverOpens_D038`). Nada del embudo de D-038 cambia.
- **Commits:** `6fb7a4bf` (el bloque en la rama), `abde6db6` (la fusión) y el de los parches, esta entrada y la nota al día (el último de la rama) (parches, esta entrada y la nota al día).
- **En la rama:**
  - `tools/level-generator/Metrics/Relief.cs`: segunda pasada de `minus_color` (A* w = 1 a 2.000.000 [P], solo para los colores que agotaron la primera, reutilizando lo ya evaluado) y su README (tabla de bytes y hashes al día); `tools/level-qa/Checks/ReliefChecks.cs`, solo la descripción de QA-RELIEF-MINUS. Datos regenerados con el generador: `mundo_04`, `05`, `07`, `08` y `09_v1.json` (solo el `relief` de 17 niveles; el 680 sigue sin `minus_color`, Q-B11-3), `tools/out/salida_a/mundo_07-09_v1.json`, `campaign_v1_report.md`, `curation_w07-09.md` y `level_qa_report.md`.
  - Código nuevo, sin llamar hasta los parches: `LevelLabelOverrides`, `TutorialTips`, `DragPour`, `ProceduralArt.Canoes.cs`, `TipBubble`, `TrayFinger`, `TutorialFingerSetup`, `TutorialTipsText`, `LevelLabelSource`, `GameScreen.TutorialTips.cs`, `GameScreen.Drag.cs` y `GameBootstrap.NoCanoesMap.cs`.
  - Tests: `LevelLabelOverridesTests`, `DragPourTests`, `TutorialTipsTests`, `ReliefMinusColorTests` y `BoardLayout320Tests` (EditMode); `CampaignPackTests` con las cuentas nuevas de variantes (1.327 en la «b», 1.328 en la «a»).
- **Parches** (`docs/dev/traspaso/spark-enganches/`, LF, independientes; los `.meta` de los archivos nuevos dentro, como los generó Unity; los seis pasan `git apply --check` sobre `abde6db6`):
  1. `B11-loc.diff`: 5 claves en su sitio por orden ordinal (`no_canoes_map` `borrador`; `tutorial_canoes`, `tutorial_double_harvest`, `tutorial_restart_free` y `tutorial_restart_free_canoe` `pendiente_asesor`, como todas las `tutorial_*`), sin EN ni PT-BR, con su captura de referencia; `TutorialTipsText.AllKeys` en `LocTablesTests.UiTextKeys`.
  2. `B11-flow.diff`: `LevelCardInfo.CanoesIntro`/`ForCanoes`, `LevelFlow.Canoes.cs` (nuevo), la tarjeta en `LevelFlow.Cards.TryShowLevelCard`, su texto en `UiText.Cards` y su icono en `ProceduralArt.Cards`; `CanoesCardFlowTests`.
  3. `B11-labels.diff`: `lvl_label_override` en `LevelCatalog.Create` (con `IsRelabeled`/`RelabeledLevels`; `IsOverridden` mira la lista de sustituidos) y en el arranque (`LevelLabelSource`); `LevelLabelCatalogTests`.
  4. `B11-tutorial.diff`: en `GameScreen.Cards.cs`, el paso previo del dedo del 2 y las llamadas a `GameScreen.TutorialTips.cs` (tick, carga y `AF_LAYOUT`); `PlayModeHelpers.NoTutorialTipsUnlessSet`; los tests del dedo del 2 al día (`TutorialFingerTests`); `TutorialTipsHooksTests` y `B11SnapshotTests` (explícito).
  5. `B11-drag.diff`: posiciones del puntero en `CuiaView` y `OnCuiaUp` → `TryDragPour`; `DragPourHooksTests`.
  6. `B11-nocanoes.diff`: «Volver al Mapa del río» en `NoCanoesScreen`; `LobbyScreen.BackButton` pasa a la X de Sin canoas (las pruebas que «vuelven al inicio»); `NoCanoesMapTests`.
- **Parado:** la reserva del 20 % de 03 §17 (Q-B11-7): publicarla cambia el contrato 21 (`pack_id` e `id` del esquema) y el catálogo; nada generado.
- **Revisión** (skill `code-review` con las reglas de `.claude/agents/code-reviewer.md`; no había herramienta para lanzar el subagente): tres rondas con CAMBIOS NECESARIOS y la cuarta **APROBADO**. Arreglado, entre otras cosas: `IsOverridden` ya no confunde un nivel reetiquetado con un override (y `IsRelabeled` para `source = override`); `LobbyScreen.BackButton` en el parche (las 2 pruebas de PlayMode que fallaban); el tooltip de la Cosecha doble espera a que suba el panel y el tooltip mide el texto una vez; el arrastre: con otro dedo (C11) cancela, sobre una Y ilegal no selecciona nada y Y se sacude (C6), elige la cuia por su cuerpo antes que por el área táctil y deja `AF_STATE reject`; la segunda pasada de `minus_color` solo resuelve lo que agotó la primera y no repite el bot; «una vez por nivel» de los tooltips sin borrarse al cargar otro nivel, con su sonda al ocultarse; la tarjeta de Canoas en `LevelCardInfo` junto a la de la Crecida; el enlace de Sin canoas solo cae al camino de siempre sin arranque y la elección `map` no va sola a `AF.Analytics` (como la del Reto, E7). Sugerencias que quedan (no bloquean): `LevelDefinition.WithLabel` dentro de la clase en vez de `Relabel` desde fuera; `PressNoCanoesMap` en `IGameActions` en vez de `GameBootstrap.Current` (como E7); el doble `LessonLegal`.
- **Verificación** (`-Architecture x86_64`, este equipo es AMD64; un solo Unity mío a la vez):

  | Dónde | Comando | Resultado |
  |---|---|---|
  | rama fusionada (`abde6db6`) | `dotnet build dotnet/AmazonFill.slnx -warnaserror --no-incremental` | 0 avisos, 0 errores |
  | rama fusionada | `dotnet test dotnet/AmazonFill.slnx` | 1.002 (996 + 6 omitidos), 0 fallos (2 min 14 s) |
  | rama fusionada | `python tools/validate_schema.py` | 103/103 PASS; `schema_example_08_4_1.json` sin cambios |
  | rama | `level-generator --spec ... --check` (11-200; 201-300; 301-420, 421-560 y 561-700 en la «b» y en la «a»; muestras de la Tinaja y de las variantes) | todos «coincide con el generado» (tras el último cambio de `Relief.cs`) |
  | rama | `level-qa` de los 8 packs (11-200 con `--report`, 201-300, 301-700 «b» y «a»), antes de los dos últimos retoques de `Relief.cs` (que no cambian ningún byte: ver los `--check`) | 0 fallos; avisos 28, 5, 8, 10, 12, 9, 11 y 12 (los mismos que documenta el README) |
  | rama | `level-generator --self-test` y `level-qa --self-test` | todo PASS (código 0) |
  | rama fusionada (`abde6db6`), sin parches | `unity-compile.ps1`; `unity-test.ps1 -Mode EditMode`; `-Mode PlayMode` | 0; **4.012 (4.009 + 3 omitidos), 0 fallos** (5,3 min); **1.778 (1.538 + 240 omitidos), 0 fallos** (39,8 min) |
  | rama fusionada + los 6 parches (aplicados y revertidos) | `unity-compile.ps1`; `unity-test.ps1 -Mode EditMode`; `-Mode PlayMode` | 0; **4.024 (4.021 + 3 omitidos), 0 fallos** (4,8 min); **1.804 (1.557 + 247 omitidos), 0 fallos** (35,9 min) |

  Antes de la fusión, con los parches: EditMode 4.000 (3.997 + 3) y PlayMode 1.790 con 2 fallos (`HomeSlothTests` y `LobbyStopTests`, por «Volver» de Sin canoas; arreglado con `LobbyScreen.BackButton`).
- **Preguntas para el dueño** (nota §5; la recomendada aplicada, [P]): Q-B11-1 dedo del 2 con paso previo; Q-B11-2 tooltip sin la frase de las canoas con las canoas apagadas; Q-B11-3 el 680 sin `minus_color`; Q-B11-4 texto del tooltip de la Cosecha doble; Q-B11-5 teléfonos de 320 dp (el lienzo siempre mide 360); Q-B11-6 «25 minutos» y «nivel 31» literales (el revisor lo señala); **Q-B11-7 reserva del 20 % (parada)**; Q-B11-8 id `home` y `back_home`; Q-B11-9 la tarjeta del 31 sin la Cuia de rescate; Q-B11-10 dedo de la Cuia extra en cada intento; Q-B11-11 arrastre solo sin selección y sin C5; Q-B11-12 tooltip de la Cosecha doble solo en el 11; y el presupuesto de 2.000.000 de la segunda pasada.
- **Para el integrador:** enlazar `drag_pour_enabled`, `lvl_label_override` y `level_overrides` con Remote Config; emitir `af_label_shown` (con `IsRelabeled` para `source`) y `af_no_canoes_choice` de todas las elecciones a la vez; las capturas de `B11SnapshotTests` (explícito, con gráficos) son las que citan las filas nuevas de `strings.csv`.
- **Riesgos:** el `relief[0]` de 358, 615, 618, 672 y 679 pasa a −1 color por la regla de orden de 03 §13; la segunda pasada alarga la generación (hasta ~90 s en el 620); arte provisional (icono de la tarjeta, tooltips); sin prueba en el teléfono; textos sin EN ni PT-BR.

---

## 2026-10-21 · B5 · Mercado, ofertas y Cántaro con la tienda falsa (`spark/t5-b5`): HECHA · verificación completa VERDE (rama y rama + parches) · lista para integrar

- **Rama:** `spark/t5-b5` (local `spark/t5-b5-agent`), desde `origin/integracion-fase1-fase2` en `0d5965cc`. Fusiona la integración **una sola vez**, al final: `0c0c725a` (arreglos de B7 y D-037) en `a06760fa`, sin conflictos. Empujada sin `--force`.
- **Nota del bloque:** `docs/dev/mercado.md` (qué hace, Remote Config, perfil, archivos, parches, cambios para el GDD, pendientes para el integrador, preguntas Q-B5-1 a Q-B5-15 y riesgos).
- **Decisiones del dueño aplicadas (D-033, todas (a)):** Q-E2 (sin icono de oferta temporal en el inicio en la 1.0: hueco libre), Q-E3 (`geo_tier` por la región del teléfono), Q-E5 («Restaurar compras» oculto), Q-E6 (Mercado ya, sin vender skins hasta el arte), Q-M4 (sesiones de B6) y Q-M8 (la pestaña Cuias elige la skin puesta con `SkinService.Select`).
- **Commits:** `130da1de` (Mercado, ofertas y Cántaro), `99d52282` (tests y parches), `b2a1715f`, `74a5fb72`, `a6746ba7`, `b4da97ab` y `4723395c` (cinco rondas de revisión), `1c357d7d` y `b0280303` (nota), `a06760fa` (la fusión), `9c381717` (parches regenerados sobre ella) y el de esta entrada.
- **En la rama** (todo archivo nuevo; ningún archivo del escritorio cambia):
  - `Scripts/IAP/`: `OffersConfig.cs` (las 6 claves de 08 §9 y `OfferRules`), `OffersRecord.cs`, `OffersPiggy.cs` (`PiggyJar`), `OffersScheduler.cs` (`offer_scheduler` de 05 §5) y `OffersTakeover.cs` (cupo de un takeover por sesión); tests en `IAP/Tests/OffersTests.cs`.
  - `Scripts/UI/`: `MarketScreen.cs`, `MarketModel.cs`, `MarketText.cs`, `MarketLobbyIcons.cs`, `OfferCard.cs`, `GeoTierRule.cs`, `GameBootstrap.Market.cs` y `MarketRunner.cs`.
  - Tests: `Tests/EditMode/MarketModelTests.cs`, `Tests/PlayMode/MarketTests.cs` y `MarketSnapshotTests.cs` (explícito: capturas `b5_01` … `b5_07`).
- **Parches** (`docs/dev/traspaso/spark-enganches/`, LF, en este orden; los `.meta` de los archivos nuevos van dentro, como los generó Unity; los cuatro pasan `git apply --check` sobre `9c381717`):
  1. `B5-loc.diff`: 19 claves nuevas en `strings.csv`, en su sitio por orden ordinal (`shop`, `no_ads` y `market_tab_seeds` con el texto del glosario y EN/PT-BR; el resto `pendiente_asesor`), cada una con su captura de referencia; `MarketText.AllKeys` en `LocTablesTests.UiTextKeys`.
  2. `B5-rc.diff`: los `[RemoteConfigKey]` de `OffersConfig` y su enlace en `RcConfigBinder` (`RcConfigs.Offers`), con su test; `GameBootstrap.MarketRc.cs`.
  3. `B5-profile.diff`: `piggy` (`{seeds, cap}` + `last_level`, `full_card` [P]) y `offers` [P] en `Profile`/`ProfileJson`; `IAP/ProfileOffersStore.cs` con sus tests; `GameBootstrap.MarketProfile.cs`.
  4. `B5-bootstrap.diff`: `SetUpMarket();` tras `SetUpSessions();`; el Mercado y la tarjeta en `CommerceOpen`; Atrás (`PressMarketBack`); `geo_tier` por la región en `GameServices` (inicio y Reto) y en la analítica (común y propiedad de usuario); `Tests/PlayMode/MarketHooksTests.cs`.
- **Revisión:** skill `code-review` con las reglas de `.claude/agents/code-reviewer.md` (no había herramienta para lanzar el subagente). Cinco rondas con CAMBIOS NECESARIOS y la sexta **APROBADO**. Lo arreglado, entre otras cosas: el Cántaro con el tope en vigor y sin vaciarse con una compra pendiente; recordatorios de la Mochila y tarjeta de skin que esperan cupo en vez de perderse; Sin anuncios y la skin en el inicio (Q-B5-6); ninguna tarjeta con la salida de la victoria en marcha (Cosecha doble, «Siguiente»); cupo que sobrevive al cambio de idioma; Cántaro solo con la tienda encendida y movido bajo Ajustes (tapaba la ventanita de TiTino); `geo_tier` sin leer extensiones BCP 47; analítica `af_shop_view`, `af_piggy`, `af_skin_unlock` e `af_iap_offer_shown` solo de lo comprable; `AF_LAYOUT` y sonda de compras; claves propias (`market_tab_seeds`, `market_skin_prize`); plurales; día sin `created_at`; y los tests de cada regla. Sugerencias no aplicadas (no bloquean): enviar `unlocked` cuando el Cántaro aparezca en vez de en la victoria del 19 (anotado en la nota §8) y una aserción sin el gancho de `VictoryLeaving`.
- **Verificación** (`-Architecture arm64`; un solo Unity mío a la vez):

  | Dónde | Comando | Resultado |
  |---|---|---|
  | rama (`9c381717`) | `dotnet build dotnet/AmazonFill.slnx -warnaserror` | 0 avisos, 0 errores |
  | rama | `dotnet test dotnet/AmazonFill.slnx` | 1.002 (996 + 6 omitidos), 0 fallos (2 min 9 s) |
  | rama | `python tools/validate_schema.py` | 103/103 PASS; `schema_example_08_4_1.json` sin cambios |
  | rama | `unity-compile.ps1` | 0 |
  | rama | `unity-test.ps1 -Mode EditMode` | **3.805 (3.802 + 3 omitidos), 0 fallos** (4,8 min) |
  | rama | `unity-test.ps1 -Mode PlayMode` | **1.734 (1.494 + 240 omitidos), 0 fallos** (34,4 min) |
  | worktree temporal: `9c381717` + los cuatro parches (borrado al acabar) | compile; EditMode; PlayMode | 0; **3.811 (3.808 + 3), 0 fallos**; **1.740 (1.500 + 240), 0 fallos** (32,5 min) |
  | rama + parches | `MarketSnapshotTests` (explícito, con gráficos: `unity test … -- -force-d3d11`) | 3/3; las 7 capturas revisadas a ojo |

  El fallo conocido `AF.Backend.Tests.ContractExamplesTests.ValidExamplesRoundTrip` («tombstones») ya no sale: lo arregló `0d342bab` en la integración.
- **Preguntas para el dueño** (se aplicó la recomendada, [P]; detalle en la nota §7): Q-B5-1 venta de skins apagada hasta el arte final; Q-B5-2 ninguna tarjeta de pago salvo la Mochila antes del 31 (05 §10); Q-B5-3 las 48 h desde la primera tarjeta; Q-B5-4 recordatorios en la primera victoria desde el 15 / el 19 con cupo; Q-B5-5 el Reto no llena el Cántaro; Q-B5-6 Mochila y Cántaro en la victoria, Sin anuncios y skin en el inicio; Q-B5-7 Cántaro bajo Ajustes; Q-B5-8 campos nuevos del perfil; Q-B5-9 fusión en la nube de `piggy` y `offers` (B8); Q-B5-10 cupo compartido con la `TakeoverQueue` de B4; Q-B5-11 lista T1 de «Europa occidental»; Q-B5-12 Cántaro con compra pendiente; Q-B5-13 la skin comprada se pone; Q-B5-14 contexto analítico de la tarjeta de skin; Q-B5-15 `from` de `af_shop_view`.
- **Para el integrador** (nota §6): con B4, un `ITakeoverBudget` sobre su `TakeoverQueue`; con B8, `piggy` y `offers` en `CloudMerge`; con F3-5, vaciar el Cántaro cuando se acredite una compra pendiente. Para el GDD, la nota §5 (08 §4.3 y §8.2; 05 §5 y §10).
- **Riesgos:** arte provisional (icono del Mercado, Cántaro, filas); comprar el Cántaro y llenarlo son escrituras aparte de la cartera y de la victoria (nota §8); sin prueba en el teléfono; textos nuevos sin EN ni PT-BR salvo los del glosario.

---

## 2026-10-21 · B4 · Bitácora del río «1 de 3» + cola de takeovers + pantalla 2 del 10 (`spark/t5-b4`): HECHA · verificación completa VERDE · lista para integrar

- **Rama:** `spark/t5-b4`, desde `origin/integracion-fase1-fase2` en `d994e586` (avance rápido desde `dbbba004`, la base del trabajo a medias). Recupera los archivos sin commitear que quedaron en el worktree `spark-t5-b4` y los termina en otro worktree (el agente no podía usar aquel), así que la rama local `spark/t5-b4` de ese worktree sigue en `dbbba004` con los archivos sueltos: se pueden borrar. No hizo falta fusionar B1a (ya está en la integración) ni B6. Al final fusiona `origin/integracion-fase1-fase2` en `e65995af` (con B2, B3, B6 y el backend) en `0c183672`, y `B4-3` se regenera sobre ella; después, `ef78baad` (backend de D-035 y el GDD al día) en `da9efb23`, sin conflictos y con los tres parches aplicables.
- **Nota del bloque:** `docs/dev/bitacora.md` (reglas, pantallas, cola, perfil, analítica, RC, `econ_sim`, parches y Q-B1 a Q-B16).
- **Commits** (del más antiguo al más reciente): `9ec7d5b7` (lógica, libreta, tarjeta y arranque), `81a7a0a2` (tests y `B4-strings.csv`; la sustitución nunca repite una opción), `8955ec7a` (la cola sobrevive al cambio de idioma; `LogbookOverride` para los tests), `10f66534` (`econ_sim` «1 de 3», `econ_B.md` y `econ_variants.md`), `5a70c5e0` (la nota), `51f2ce05` (los parches), `73d04afd`, `5050805b` y `c315705d` (las tres rondas de revisión), `8a3dc25f` (esta entrada), `0c183672` (la fusión), `66612d2e` (B4-3 regenerado y Q-B16), `68ab470c` (verificación), `da9efb23` (segunda fusión) y el de esta puesta al día.
- **En la rama** (todo archivo nuevo, salvo `tools/econ_sim.py`, `tools/econ_variants.py` y sus dos salidas, que el encargo §12 asigna a B4):
  - `Scripts/LiveOps/LoginCalendarTable.cs`, `LoginCalendarRules.cs`, `LoginCalendarRecord.cs`, `LoginCalendarService.cs`, `LoginCalendarConfig.cs` y `LoginCalendarProbeFormat.cs`, con sus tests en `Scripts/LiveOps/Tests/LoginCalendar*Tests.cs`;
  - `Scripts/GameFlow/TakeoverQueue.cs` y `LevelFlow.Calendar.cs` (el aviso de nivel terminado, sin llamar hasta B4-2), con `Tests/EditMode/TakeoverQueueTests.cs`;
  - `Scripts/UI/LogbookScreen.cs`, `LogbookCard.cs`, `LogbookText.cs`, `GameBootstrap.Logbook.cs`, `LogbookRunner.cs` y `DevProbe.Logbook.cs` (sin llamar hasta B4-3), con `Tests/PlayMode/LogbookScreenTests.cs` y `LogbookBootstrapTests.cs`;
  - `docs/dev/traspaso/spark-enganches/B4-strings.csv`: 20 claves `logbook_*`; `logbook_item_hint` y `logbook_item_reveal` en `pendiente_asesor` (fauna); `logbook_item_grand_basket` sin EN ni PT-BR (glosario, pregunta abierta 8).
- **Parches** (`docs/dev/traspaso/spark-enganches/`, LF; los tres pasan `git apply --check` sobre la rama; orden y detalle en la nota §7):
  1. **`B4-1-profile.diff`:** `Profile.Calendar` (`CalendarData`) y `ProfileJson` (`calendar` tras `streak`, omitido mientras es null); `LiveOps/ProfileLoginCalendarStore.cs` y sus tests; `LogbookStoreFactory` = `CreateProfileLogbookStore`.
  2. **`B4-2-flow.diff`:** `OnLevelPlayedCalendar(true)` en `LevelFlow.ResolveWin` y `OnFailCalendar(kind)` en `LevelFlow.Fail`; `Tests/EditMode/LevelPlayedHookTests.cs`.
  3. **`B4-3-bootstrap.diff`:** en `GameBootstrap.cs`, `SetUpLogbook();` entre `SetUpSkip();` y `SetUpSessions();`, la libreta antes del Mapa del 10 y Atrás de Android; `NoLogbookUnlessSet()` en `PlayModeHelpers.cs`; `Tests/PlayMode/LogbookHooksTests.cs`.
  4. **Textos:** las filas de `B4-strings.csv` a `strings.csv`.
  - **Choques:** los parches están regenerados sobre `e65995af` (B2, B3 y B6 ya integrados); B7 (`ConnectTelemetry();`) va después de `SetUpLogbook();`. B8 tiene que cubrir `calendar` en la integridad y la fusión del perfil (propuesta [P]: el registro con más avance y la unión de los marcos). El aviso «Bitácora sin sellar» de B6 sigue sin conectar (Q-B16).
- **Revisión (skill `code-review`, con las reglas de `.claude/agents/code-reviewer.md`; no había herramienta para lanzar el subagente):**
  - 1.ª ronda: CAMBIOS NECESARIOS. Verificación sin registrar (esta entrada); tests de la victoria del Reto, de la fila de la victoria, del cambio de idioma y del Mapa del 10 sin condición; `played_date` contra la letra de D-033, punto 6 (Q-B1). Sugerencias: arranque frágil con una tasa de RC que no cuadra, Atrás, medianoche, día 30, copia de `lvl_murky_from_level`, doc de «Salir». Todo arreglado en `73d04afd`.
  - 2.ª ronda: CAMBIOS NECESARIOS. La constante de la Luciérnaga dependía de `DevBench`, que solo existe en desarrollo: el build de release no habría compilado. Arreglado en `5050805b`, con la medianoche resuelta en el fotograma y el desbordamiento de la tasa.
  - 3.ª ronda: **APROBADO**. Sugerencias aplicadas en `c315705d`: desbloqueo con `max_level` ≥ nivel (la Luciérnaga ya no se adelanta al regalo del 100), lo recibido no se borra a medianoche, `catch` más estrecho y un test que ata la constante a 08 §9 y a `DevBench`.
- **Verificación** (`-Architecture arm64`; como mucho un Unity mío a la vez, con el de otro agente en marcha):

  | Dónde | Comando | Resultado |
  |---|---|---|
  | rama fusionada (`66612d2e`) | `dotnet build dotnet/AmazonFill.slnx -warnaserror` | 0 avisos, 0 errores |
  | rama fusionada | `dotnet test dotnet/AmazonFill.slnx` | 1.002 (996 + 6 omitidos), 0 fallos |
  | rama fusionada | `python tools/validate_schema.py` | 103/103 PASS, código 0; `schema_example_08_4_1.json` sin cambios |
  | rama fusionada | `python tools/econ_sim.py --runs 300 \| diff --strip-trailing-cr -q - tools/out/econ_B.md` | idéntico (tras regenerarlo en `10f66534`) |
  | rama fusionada | `unity-compile.ps1` | 0 |
  | rama fusionada | `unity-test.ps1 -Mode EditMode` | **3.581 (3.578 + 3 omitidos), 0 fallos** (43 de B4) |
  | rama fusionada | `unity-test.ps1 -Mode PlayMode` | **1.721 (1.484 + 237 omitidos), 0 fallos** (18 de B4); 34 min |
  | rama fusionada + B4-1 + B4-2 + B4-3 (aplicados en el worktree y revertidos al acabar) | compile; EditMode; PlayMode | 0; **3.587 (3.584 + 3), 0 fallos**; **1.724 (1.487 + 237), 0 fallos**; 32 min |
  | tras la 2.ª fusión (`da9efb23`) | `dotnet build -warnaserror`; `dotnet test`; `validate_schema`; compile; EditMode; PlayMode `Logbook` | 0 avisos; 1.002 (996 + 6); 103/103; 0; **3.652 (3.648 + 3 omitidos), 1 fallo ajeno a B4**: `AF.Backend.Tests.ContractExamplesTests.ValidExamplesRoundTrip` («voided-purchases-job.output.json: clave no admitida 'tombstones'»), del backend recién integrado (el ejemplo del contrato y el lector C# no coinciden; B4 no toca `Scripts/Backend` ni los contratos); 18/18. Los tres parches siguen pasando `--check` |
  | antes de la fusión (`c315705d`) | EditMode; PlayMode | 3.437 (3.434 + 3); 1.702 (1.466 + 236); 0 fallos. Con los parches, en un worktree temporal: 3.442 y 1.699, 0 fallos. En pasadas intermedias fallaron una vez, por tiempo y con dos Unity a la vez, `WorldBannerTests.Banner_TimeoutAndWaitMs_UseTheRealClock`, `IntroArtTests.FruitFlight_…Within10ms` y `TranslatedLocaleOverflowTests` (pt-BR, 130 %), ajenos a B4: los tres pasaron relanzados |

- **Lo marcado [P]** y las **preguntas para el dueño** (Q-B1 a Q-B16, con su recomendada aplicada): `docs/dev/bitacora.md` §8. Las que más pesan:
  - **Q-B1** (`played_date` en `calendar`): va contra la letra de D-033, punto 6, y cambia 08 §4.3: **que el dueño la confirme antes de integrar B4-1**.
  - **Q-B3** (cuánto vale una Marea): D-033, punto 1, contra 06 §4.2, regla 1, y la clave `undo3`. Implementada la de D-033 (30 por Marea); con `econ_sim`, «siempre comodín» deja al casual con más bloqueos (96 % frente al 69 % del canon).
  - **Q-B11:** la fila «Bitácora · elige tu premio» del panel de la victoria la dibuja el escritorio (B1b) con `LogbookVictoryRowDue` y `OpenLogbookFromVictory()`.
- **Para el GDD** (lo anota el integrador; texto en la nota §7): 06 §4.2 y §2.3; 05 §2.3 y §3 (cifras [sim] nuevas: saldo final del casual 1.912 → 2.227, casual con bloqueo sin recursos 74 % → 69 %, `bitacora` del medio 3.577 → 3.974…); 08 §4.3 (`calendar`), §8.2 (`af_calendar_claim` con `choice` y `cycle`) y §9 (`eco_calendar_table_v2`).
- **Riesgos:**
  - `econ_A.md` y `econ_B_tramos.md` ya no se reproducían antes de B4 (Q-B15).
  - Tres pruebas de tiempo real fallaron una vez en pasadas intermedias (tabla de arriba): son inestables con dos Unity a la vez y pasaron relanzadas. Las pasadas finales no tuvieron fallos.
  - El aviso «Bitácora sin sellar» de B6 sigue apagado hasta conectar su contexto (Q-B16).
  - El arte de la libreta y de la tarjeta es provisional (por código).
  - Sin capturas de B4 en `Logs/snapshots/` ni prueba en el teléfono.

## 2026-10-21 · B8 · Nube sin SDK + integridad + anti-fraude local + «Eliminar cuenta» en local (`spark/t5-b8`): HECHA · verificación completa VERDE en `920ec25f`; tras la última fusión, solo .NET y parches · lista para integrar

- **Rama:** `spark/t5-b8`, desde `9eb593c`. Fusiona `origin/integracion-fase1-fase2` cinco veces, sin conflictos:
  - `d994e586` (B14 y B1b) en `e8c90ebd`;
  - `eb863f54` (B1b-Q10) en `89127c19`;
  - `803e9723` (B2, B3, B6 y B13) en `ab0ba8f8`;
  - `d933dcde` (backend F2) en `920ec25f`;
  - `ef78baad` (D-035 en el GDD, F3-1 `AF.Backend`, Firebase dev) en `e2748569`.
  - **Por instrucción del coordinador, no se fusiona más.**
  - Empujada, sin `--force`.
- **Nota de diseño:** `docs/dev/nube-integridad.md`. Recoge:
  - los 8 casos de la fusión, la firma, el anti-reloj, la revocación y «Eliminar cuenta»;
  - lo que queda para el integrador, los SDK y Functions (§8);
  - los parches (§9);
  - las preguntas Q-B8-1 a Q-B8-11.
- **Commits** (del más antiguo al más reciente):
  - `76d5945e`: la nube sin SDK, en archivos nuevos:
    - `ICloudSave` / `FakeCloudSave`;
    - `CloudMerge`, con los 8 casos;
    - `ProfileHmac`;
    - `IDeviceIdentity`;
    - `GuardedClock`;
    - el diálogo de dos pasos de «Eliminar cuenta y datos», con sus tests EditMode;
  - `8aab10fa`: los arreglos de la revisión:
    - id por instalación (`InstallIdentity`, Q-B8-3 (b));
    - el borrado local va antes que la nube;
    - el texto del caso 8;
  - `4dbf5345`: la nota, los tres parches y `B8-strings.csv`;
  - las fusiones `e8c90ebd`, `89127c19`, `ab0ba8f8`, `920ec25f` y `e2748569`;
  - y el commit de esta entrada, con `B8-loc.diff` y la nota puesta al día (Q-B8-11).
- **En la rama** (archivos nuevos, en `Assets/_Project/Scripts/`):
  - `Save/ICloudSave.cs`, `FakeCloudSave.cs`, `CloudMerge.cs` y `ProfileHmac.cs`;
  - `Platform/IDeviceIdentity.cs` y `GuardedClock.cs`;
  - en `UI/`:
    - `InstallIdentity.cs`, `DeleteAccountDialog.cs` y `AccountText.cs`;
    - `SettingsScreen.Account.cs`, `GameBootstrap.Account.cs` y `DevProbe.Account.cs`;
  - sus tests EditMode.
  - Ningún archivo del escritorio cambia en la rama.
  - `InstallIdentity.cs.meta` es el `.meta` que Unity generó para `UnityDeviceIdentity.cs`, renombrado con el archivo en `8aab10fa`: conserva su GUID.
- **Parches** (`docs/dev/traspaso/spark-enganches/`, en LF, en este orden; detalle en la nota §9):
  1. `B8-profile.diff`: `Profile.cs` y `ProfileJson.cs` (campos nuevos), la firma, la identidad, `CloudMerge.Fields` y `CloudSync`;
  2. `B8-economy.diff`: `Canoes`, `Wallet`, `Transaction`, `ProfileStreakStore` y `LocalRevocation`;
  3. `B8-ui.diff`: `SettingsScreen`, `GameBootstrap*` (`GameBootstrap.Cloud.cs`) y `AccountAndCloudTests`;
  4. **`B8-loc.diff`** (nuevo en esta entrada; sustituye a copiar `B8-strings.csv` a mano):
     - las 9 claves van a `strings.csv`, en su sitio por orden ordinal;
     - las claves de `AccountText.Keys` se registran en `LocTablesTests.UiTextKeys`, como hicieron B3 y B6;
     - sin esto, `EveryUiTextKey_Exists_AndEveryStringsRow_IsUsed` falla.
  - Sobre `ef78baad` (lo último fusionado), los cuatro aplican con `git -c core.autocrlf=false apply` en un worktree temporal, y después pasan `apply -R --check`. **No hubo que regenerar ninguno**: B6-profile toca `Profile.cs` y `ProfileJson.cs` en otros puntos.
- **Revisión (code-reviewer):** CAMBIOS NECESARIOS en la 1.ª ronda, arreglados en `8aab10fa`:
  - la huella del HMAC sale de un id aleatorio por instalación en `PlayerPrefs`, no de `SystemInfo.deviceUniqueIdentifier` (Q-B8-3 cambia a (b): sin identificador del dispositivo ni fila nueva de Data Safety);
  - «Eliminar» borra primero el perfil y `PlayerPrefs`, y solo después la nube;
  - el diálogo exige su cancelación explícita;
  - Q-B8-5 cambia a (b): un perfil sin firma o migrado sigue las reglas de siempre.
  - Tras eso, la rama se dio por aprobada («Retoca B8 tras la revisión»). `B8-loc.diff` y Q-B8-11 son de esta entrada y no tuvieron ronda propia.
- **Verificación** (`-Architecture arm64 -TimeoutSec 3600`; tiempos reales). Como mucho hubo 2 Unity a la vez; el otro era de otro agente.

  | Dónde | Comando | Resultado |
  |---|---|---|
  | rama (`e2748569`, con D-035 y F3-1) | `dotnet build -warnaserror`; `dotnet test`; `validate_schema.py`; `git apply` y `apply -R --check` de los cuatro en un temporal | 0 avisos y 0 errores; **1.002 (996 + 6), 0 fallos** (2 min 27 s); 103/103; los cuatro aplican. **Unity no se verificó en este estado**: compile y EditMode empezaron y se cortaron por instrucción del coordinador (cierre sin más rondas) |
  | rama (`ab0ba8f8`) | `dotnet build -warnaserror`; `dotnet test`; `validate_schema.py` | 0 avisos y 0 errores; **1.002 (996 + 6), 0 fallos** (8 min 46 s, con Unity en paralelo); 103/103, código 0 |
  | rama (`920ec25f`) | `dotnet build -warnaserror`; `validate_schema.py` | 0 avisos y 0 errores; 103/103. `dotnet test` no se repite: entre `ab0ba8f8` y `920ec25f` no cambia nada en `dotnet/`, `Packages/`, `tools/` ni `schemas/` |
  | rama (`920ec25f`) | `unity-compile`; EditMode; PlayMode | 0 (34 s); **3.581 (3.578 + 3), 0 fallos** (5,0 min); **1.703 (1.466 + 237), 0 fallos** (33,4 min) |
  | temporal: `920ec25f` + profile, economy y ui | compile; EditMode; PlayMode | 0 (125 s); **3.618 (3.615 + 3), 0 fallos** (5,5 min); **1.706 (1.469 + 237), 0 fallos** (33,0 min) |
  | idem + `B8-loc.diff` | EditMode; PlayMode con filtro (`AccountAndCloudTests`, `PseudoLocOverflowTests`) | **3.618 (3.615 + 3), 0 fallos** (5,3 min); **5/5** (58 s) |

  Antes de la fusión de B2, B3 y B6 (`e8c90ebd`), en la rama: 0 avisos; 981 (975 + 6); 103/103. El worktree temporal se borró al terminar.

- **Lo que trae `ef78baad` y no se probó en Unity con B8:**
  - `AF.Backend` (F3-1), con `RestoreLedger`, `Account` y `VerifyPurchase`;
  - `Platform/ServerDateTracker.cs`, un `IServerDate` real.
  - No hay choque de nombres ni de texto: los parches aplican y `dotnet build` pasa. Aun así, el integrador debe pasar compile, EditMode y PlayMode con los cuatro parches aplicados.
  - En EditMode cabe esperar 1 fallo que no es de B8: `AF.Backend.Tests.ContractExamplesTests.ValidExamplesRoundTrip`. El lector de F3-1 no admite la clave `tombstones` del ejemplo de F2c; sale igual en la rama de B7 sin parches (ver la entrada de B7).

### Preguntas para el dueño (B8; se avanzó con la recomendada, [P])

| Id | Pregunta | Recomendada |
|---|---|---|
| Q-B8-1 | `applied_purchases` por unión (08 §11): una compra del que pierde el conflicto queda «aplicada», pero su saldo se pierde | (a) unión, aplicada porque es canon; **se recomienda cambiar 08 §11 a (b)**: la lista del ganador, y `restoreLedger` vuelve a acreditar lo que falte |
| Q-B8-2 | De qué sale la clave del HMAC | (a) de `device_first`: un blob de otro teléfono se puede comprobar |
| Q-B8-3 | El id del que sale la huella | (b) un id aleatorio por instalación (`af_install_id`), cambiada tras la revisión |
| Q-B8-4 | Qué es `locale` | (a) el idioma de la UI resuelto en cada arranque |
| Q-B8-5 | ¿Un perfil sin firma o migrado es «coherente»? | (b) sí, cambiada tras la revisión: así no se pierde progreso con una nube de un esquema anterior |
| Q-B8-6 / Q-B8-7 | `daily` con fechas distintas; empate total | Gana el bloque del día más reciente; en el empate, el local |
| Q-B8-8 / Q-B8-9 | Revocar Sin anuncios; orden de `ApplyLedger` | Se quita el derecho; orden ordinal de token |
| Q-B8-10 | ¿Se guarda la sospecha para la fusión? | (a) solo medir (08 §10.1: sin castigo automático) |
| **Q-B8-11** (nueva) | Con B6 integrado, cómo se fusionan `settings.push` y `stats.last_seen_at` / `last_win_at`. Hoy se quedan los del ganador | (a) la propuesta de B6: máximo en las fechas, «o» en los pre-prompts y `granted` del dispositivo. **Sin aplicar**: una línea por campo en `CloudMerge.MergeFields` con su caso en `CloudMergeTests` |

Cierra además B1a-Q1 y B1a-Q2 (D-033, (a)) y Q-S1 de B3 (`skipped_levels` por unión, ya en `CloudMerge`).

### Riesgos y pendientes

- **Play Games (dueño):**
  - un marcador de «borrado pendiente» antes de `CloudSync.AtStartup`;
  - el `ICloudSave` real;
  - las subidas periódicas.
- **Backend (D-035: AWS; antes «Functions»):**
  - `restoreLedger` → `LocalRevocation.ApplyLedger`;
  - el SSV → `RegisterProvisional` / `Confirm` / `ExpireStale`;
  - `deleteAccount`;
  - un `IServerDate` real.
  - Los nombres de 08 §10-§11 pueden cambiar con D-035; la interfaz local no depende de ellos.
  - **Nuevo con F3-1:** `ServerDateTracker` (un `IServerDate` real) y el cliente `AF.Backend` ya existen. El paso siguiente es pasar `ServerDateTracker` a `GuardedClock` en lugar de `FakeServerDate.Offline()`, y `RestoreLedger` / `Account` a `LocalRevocation` y a «Eliminar cuenta».
- **B7:**
  - `RevocationConfig.SecSsvGraceH` ← `sec_ssv_grace_h`;
  - `af_save_sync` y `af_account_delete` a la analítica (hoy son sondas `AF_STATE`).
- **Integrador:** `ProfileSaveBatch.Delete()` podría dejar el lote cerrado. Hoy, un callback entre «Eliminar» y la recarga podría volver a escribir `profile.json`.
- **Teléfono:**
  - comprobar `SHA256` / `HMACSHA256` con IL2CPP y el stripping High (B10): `AF_STATE profile_integrity` = `valid` tras un guardado;
  - probar el flujo de «Eliminar cuenta y datos».
- **GDD:**
  - anotar en 08 §4.3 `voided_purchases`, `pending_rewards` e `integrity.fraud_flag` [P];
  - anotar en 08 §11 los 8 casos.
- **Textos:** las 9 claves nacen `pendiente_asesor`, sin `en` ni `pt-BR`.

---

## 2026-10-20 · B7 · `AF.RemoteConfig` + `AF.Analytics` con implementaciones falsas, y el bus `AF.Events` (`spark/t5-b7`): HECHA · verificación completa VERDE · lista para integrar

- **Rama:** `spark/t5-b7`, desde `9eb593c`. Fusiona `origin/integracion-fase1-fase2` cinco veces: `e11b4246` en `5a39eae0`, `d994e586` (B14) en `e058645f`, `eb863f54` (B1b-Q10) en `513acee9`, `803e9723` (B2, B3, B6 y B13) en `671646f9` y `d933dcde` (backend F2) en `4df8a380`. Sin conflictos, salvo esta misma entrada de `spark-estado.md` (se dejan las dos). Empujada.
- **Nota de diseño:** `docs/dev/remote-config-analytics.md`. Recoge qué hay, cómo se lee 08 §9, el bus, la analítica, los ocho parches y las preguntas Q-B7-1 a Q-B7-23.
- **Commits** (del más antiguo al más reciente):
  - `b7f0cdc5`: el generador `tools/remote-config/rc_defaults.py` (+ `test_rc_defaults.py`), `RemoteConfigDefaults.json` (251 claves) y su copia en C#;
  - `cebbd61a`: `AF.Events` y `AF.RemoteConfig` sin motor, con `AF.RemoteConfig.Binding` (las ocho clases de cifras, sin reflexión);
  - `1632f08b`: `AF.Analytics` (catálogo de 08 §8.2 y 04 §11, comunes de 08 §8.1, consentimiento y sumideros), `AF.Analytics.Unity` (logcat `AF_EVENT`) y `AF.Analytics.Adapters` (anuncios e IAP);
  - `46c0450d`: la sesión (Q-M4);
  - `b8895a8d`: los `.meta`;
  - `18f1e0db`: `biz_feature_rescue` y el anti-reloj del Reto en el binder;
  - `7697f094`: los arreglos de la 1.ª revisión;
  - `267da94c`: la nota y los ocho parches;
  - `d2889a40`: los arreglos de la revisión final;
  - `a464e272`: los parches regenerados sobre `e11b424`;
  - `493e4d35`: `RcJson` rechaza los no finitos también en Mono;
  - `60eec9b2`: el escaneo de `[RemoteConfigKey]` recorre todo ensamblado que referencie `AF.Platform` (con un mínimo de 6);
  - `03f47cf4` y `3ab28fde`: `B7-bootstrap.diff` regenerado (ver «Parches»);
  - `e4ecfd7c`: `ComebackConfig` y `PushConfig` (B6) en `NotBoundYet`;
  - las fusiones `e058645f`, `513acee9`, `671646f9` y `4df8a380`;
  - y el commit de esta entrada.
- **En la rama** (todo archivo nuevo; ningún archivo existente cambia):
  - `Scripts/Events/`;
  - `Scripts/RemoteConfig/`, con `Binding/` y `Tests/`;
  - `Scripts/Analytics/`, con `Unity/`, `Adapters/` y `Tests/`;
  - `Data/Config/RemoteConfigDefaults.json` (generado);
  - `tools/remote-config/`.
- **Parches** (`docs/dev/traspaso/spark-enganches/B7-*.diff`, en LF; el detalle y el orden de compilación están en la nota §5): `B7-asmdef`, `B7-wallet`, `B7-levelflow`, `B7-appflow`, `B7-gamescreen`, `B7-rivermap`, `B7-bootstrap` y `B7-gdd`.
  - `B7-bootstrap.diff` se **regeneró dos veces**, porque la línea de `ConnectTelemetry();` va tras la última `SetUp*` de `GameBootstrap.Start`:
    - `03f47cf4`: tras `SetUpMeta();` (B14, en `d994e586`);
    - `3ab28fde`: tras `SetUpSessions();`, con B3 (`SetUpSkip();`) y B6 (`SetUpSessions();`) ya integrados en `803e9723`. Ya no hay choque a mano con `B3-ui` ni `B6-bootstrap`: los dos están aplicados en la integración.
    - Los otros siete parches no cambian.
  - Sobre `d933dcde` (lo último de la integración), los ocho pasan `git -c core.autocrlf=false apply --check` y, aplicados en un worktree temporal, también `apply -R --check`.
- **Arreglo tras la fusión de B6** (`e4ecfd7c`, solo el test): `ComebackConfig` y `PushConfig` (B6, `AF.LiveOps`) tienen `[RemoteConfigKey]`; van a `NotBoundYet` con su motivo, como `ShuffleConfig` (B2) y `SkipConfig` (B3). Las cuatro están ya en la integración: falta su tabla en `RcConfigBinder` y pasarlas a su sistema (ver «Riesgos»).
- **Revisión (code-reviewer):**
  - 1.ª ronda: CAMBIOS NECESARIOS, arreglados en `7697f094`:
    - `TryGet` sin excepción por desbordamiento;
    - tope de `Violations`;
    - referencias Q-B7.
  - Revisión final: arreglos en `d2889a40`:
    - la cobertura del binder por descubrimiento, en vez de una lista a mano;
    - `band` en `level_start` (Q-B7-23);
    - un generador más estricto con las celdas no literales;
    - las notas de aplicación.
  - `493e4d35` y `60eec9b2` endurecen dos puntos de esa revisión: los no finitos en Mono y el alcance del escaneo. **No tuvieron ronda de revisión propia**, así que el integrador puede pedir una pasada corta sobre ellos.
- **Verificación** (`-Architecture arm64 -TimeoutSec 3600`; tiempos reales). Como mucho hubo 2 Unity a la vez; el otro era de otro agente.

  | Dónde | Comando | Resultado |
  |---|---|---|
  | rama (`60eec9b2`, antes de fusionar `d994e586`) | PlayMode | **1.655 (1.425 + 230), 0 fallos**, 30,8 min |
  | rama (`513acee9`) | compile; EditMode; PlayMode | 0 (24 s); **3.473 (3.470 + 3), 0 fallos** (5,1 min); **1.698 (1.461 + 237), 0 fallos** (39,5 min) |
  | temporal: `513acee9` + los ocho parches | compile; EditMode; PlayMode | 0 (158 s); **3.479 (3.476 + 3), 0 fallos** (5,1 min); **1.699 (1.462 + 237), 0 fallos** (32,0 min) |
  | rama (`e4ecfd7c`, con B2, B3, B6 y B13) | `dotnet build -warnaserror`; `dotnet test`; `validate_schema.py` | 0 avisos y 0 errores; **1.002 (996 + 6), 0 fallos** (8 min 35 s, con Unity en paralelo); 103/103, código 0 |
  | rama (`e4ecfd7c`) | `rc_defaults.py --check`; `unittest` de `tools/remote-config` | «identico (251 claves)», código 0; 4 tests OK y `--self-test` OK |
  | rama (`e4ecfd7c`) | `unity-compile`; EditMode; PlayMode | 0 (33 s); **3.617 (3.614 + 3), 0 fallos** (5,9 min); **1.703 (1.466 + 237), 0 fallos** (33,0 min) |
  | temporal: `671646f9` + los ocho parches (`B7-bootstrap` regenerado) | compile; EditMode | 0 (231 s); 3.623: **1 fallo**, `RcConfigBinderTests.EveryTypeWithRemoteConfigKeys_IsBound_OrDeclaredAsNotBoundYet`: `ComebackConfig` (B6) tiene `[RemoteConfigKey]` y no estaba ni enlazada ni en `NotBoundYet`. Arreglado en `e4ecfd7c` (ver abajo) |
  | idem | PlayMode | 1.704: **1 fallo**, `PseudoLocOverflowTests.EnXA_360x640_NoTextOverflows(100)` («Tiempo agotado esperando: victoria», 180 s), con otro Unity y `dotnet test` en paralelo |
  | idem, con el test de `e4ecfd7c` | EditMode; PlayMode con filtro (`PseudoLocOverflowTests`, `TelemetryBootTests`) | **3.623 (3.620 + 3), 0 fallos** (4,8 min); **3/3** (53 s): el fallo era de carga, no de B7 |
  | rama tras fusionar `d933dcde` (`4df8a380`) | `dotnet build -warnaserror`; `validate_schema.py`; `rc_defaults.py --check`; `git apply --check` de los ocho | 0 avisos; 103/103; idéntico; los ocho aplican. Unity no se repite: esa fusión solo trae `infra/`, `.github/`, `ci/check-manifest.ps1`, `CLAUDE.md` y `docs/dev` (0 archivos en `Assets/`, `Packages/`, `ProjectSettings/`, `dotnet/`, `tools/`, `schemas/` y `docs/gdd/`) |

  Antes (sobre `e058645f`, sin B2/B3/B6): temporal con los ocho, 0; 3.479 (3.476 + 3); 1.685 (1.449 + 236), todo sin fallos; y 981 (975 + 6) en `dotnet test`. Los worktrees temporales se borraron al terminar.

### [P] y preguntas para el dueño (B7; se avanzó con la recomendada, la (a))

Lo marcado [P] está en la nota:
- el formato `af-rc-defaults/1`;
- las interpretaciones de las diez celdas no literales de 08 §9;
- los dos ensamblados de composición;
- la congelación por `RcSnapshot`;
- la validación contra el catálogo;
- el sumidero `AF_EVENT`;
- la sesión.

| Id | Pregunta | Recomendada (aplicada) |
|---|---|---|
| Q-T1 | Bus `AF.Events` frente a las interfaces de escucha | (a) bus mínimo sin motor (D-033); las interfaces siguen |
| Q-B7-1 / Q-B7-2 | `AF.RemoteConfig` → `Backend` (no existe) y `AF.Events` sin fila en 08 §2.2 | Sin dependencias hasta `AF.Backend`; añadir la fila de `AF.Events` a 08 §2.2 |
| Q-B7-3 | Cómo leen RC y la analítica los módulos | Dos ensamblados de composición (`AF.RemoteConfig.Binding`, `AF.Analytics.Adapters`) que solo usa `AF.UI` |
| Q-B7-4 / Q-B7-5 | «resto» en los mapas por país; los placements de `ads_rv_cooldown_s_<placement>` | La clave `"*"`; los seis de `ads_rv_cap_<placement>` |
| Q-B7-6 | Defaults que remiten a otra tabla (`eco_calendar_table_v1`, `biz_kpi_*`, premios de liga y regata, `eco_price_tier_<country>`) | `null` + `pending`; cada bloque transcribe la suya a 08 §9 |
| Q-B7-7 | Valor remoto que no pasa `Validate()` | La clase entera vuelve a `Defaults`. Ojo: las `Validate()` no comprueban los rangos de 08 §9 |
| Q-B7-8 | `biz_maintenance_message` | Una clave de `strings.csv` |
| Q-B7-9 | Analítica del Reto | Sigue como sonda `AF_STATE` (R10); su gancho va en un parche aparte |
| Q-B7-10 a Q-B7-13 | `double_harvest`, `af_level_abandon.reason`, comunes y `boosters_json` | Estado en la victoria; solo `map`; a todos los eventos, sin los null; `undo` y `extra_cuia` |
| Q-B7-14 | Qué apaga cada `biz_feature_<sistema>` | `lives` = canoas fuera; `rescue` = sin Cuia de rescate; los demás, con su sistema |
| Q-B7-15 | `anim_speed_factor_late` = 1,0 (D-033, Q-N1) | Lo corrige `B7-gdd.diff`, que regenera el JSON |
| Q-B7-16 a Q-B7-21 | Formato de logcat, `session_id`, booleanos, `rc_variant`, consentimiento sin pantalla y cómo llega el JSON | `AF_EVENT {json}` solo en desarrollo; GUID; `true`/`false`; `biz_experiment_tag`; recogida apagada; JSON compilado como constante |
| Q-B7-22 / Q-B7-23 | `pours` y `t_ms` en los eventos de tablero; `level_start.band` en la campaña | Añadirlos al enganchar los comodines; mandar `difficulty.band` |

### Riesgos y pendientes

- **Ganchos pendientes** (el catálogo ya los admite). Se harán por zonas cuando el escritorio libere los archivos:
  - `af_boot`, `af_consent` y `af_wallet_snapshot`;
  - comodines, modal y canoas;
  - Crecida y Canasta (hoy sondas de B1a) y Reto (R10);
  - Ajustes, etiquetas, mundo y mecánicas;
  - `af_level_load` y `af_error`;
  - el `level_end` del Salto (B3);
  - lo nuevo de B14 (skins, álbum, Datos del río) y de B8 (`af_save_sync`, `af_account_delete`).
- **Cifras de B2, B3 y B6 sin Remote Config todavía:** `ShuffleConfig`, `SkipConfig`, `ComebackConfig` y `PushConfig` ya están en la integración, pero usan sus `Defaults`. Falta su tabla en `RcConfigBinder` (una línea por clave), sacarlas de `NotBoundYet` y pasarlas a su sistema en `GameBootstrap.Telemetry.cs`. Es un retoque pequeño de B7 tras integrarlo; el test de cobertura avisa de cualquier clase nueva.
- **Integración con B8:**
  - B8 pide enlazar `sec_ssv_grace_h` con `RevocationConfig.SecSsvGraceH`.
- **Sin pantalla todavía:** «Actualiza Tintalú» y el aviso de mantenimiento (UI del escritorio y textos nuevos).
- **`CLAUDE.md`**, para el integrador (nota §7): las carpetas nuevas, `RemoteConfigDefaults.json` como generado y los dos comandos de `tools/remote-config/`.
- **Teléfono:**
  - comprobar `AF_EVENT` en logcat con un build de desarrollo;
  - comprobar que el stripping High (B10) no rompe el binder. El binder no usa reflexión; los tests sí.

---

## 2026-10-20 · T5 · B13 · Borradores de publicación (`spark/t5-b13`): HECHA · lista para integrar (solo documentación)

- **Rama:** `spark/t5-b13`, desde `origin/integracion-fase1-fase2` en `9982bdc`. Commits `76d442b`, `9180887` y `b2577df` (revisión).
- **Qué entrega.** Todo es borrador: lo que firma el abogado o decide el dueño va como `[PENDIENTE: dueño]` o `[PENDIENTE: abogado]`, sin datos inventados.
  - En `docs/store-listing/`:
    - `README.md`, con el índice, las longitudes medidas en NFC, las preguntas Q-B13-1..23, los hallazgos de audio en «3 bis» y los enganches del GDD;
    - las fichas de Play en `es-419`, `en` y `pt-BR` (Méx. con OXXO y Bra. con Pix, pendientes de Q-B13-17);
    - `data-safety.md`, `iarc.md` (ClassInd 14 esperado), `privacidad.md` (anexos de Méx. y LGPD) y `eliminar-cuenta.md`;
    - la plantilla `app-ads.txt`;
    - `assets-tienda.md`, el encargo de arte para el artista;
    - `creditos.md`, más las 16 claves `credits_*` en `strings-creditos.csv`.
  - En la raíz, `LICENSES.md`.
  - En `docs/qa/`, `regresion-release.md`, con 60 casos y su fuente.
- **Longitudes en NFC** (título 30 / corta 80 / larga 4.000 / novedades 500 / texto de captura ≤ 36 [P]):
  - es-419: 21 / 78 / 2.487 / 143 / 36;
  - en: 26 / 74 / 2.346 / 145 / 31;
  - pt-BR: 25 / 73 / 2.431 / 134 / 34.
- **Verificación:**
  - `python tools/validate_schema.py`: 103/103;
  - `dotnet build -warnaserror` y `dotnet test`: 0 avisos y 981 (975 + 6). Los pasó el revisor en la primera ronda;
  - `git diff --check` limpio;
  - no toca el GDD, `Assets/` ni `tools/`.
- **code-reviewer:**
  - 1.ª ronda, CAMBIOS NECESARIOS:
    - «cada color es una fruta» (son 12 frutas y 2 aguas);
    - el 77 en lugar de 74;
    - el límite de las capturas contradictorio;
    - un mecanismo de borrado inventado;
  - 2.ª ronda: **APROBADO**.
- **Para el integrador:**
  - pasar las filas `credits_*` a `strings.csv` cuando exista la pantalla de Créditos;
  - corregir el glosario §1.3 (la descripción corta EN mide 74, no 77);
  - llevar al GDD lo que decida el dueño (07 §10.2, 08 §11, 08 §13.3).

### Preguntas y hallazgos para el dueño (no bloquean lo entregado)

Las 23 preguntas, con sus opciones y la recomendada, están en `docs/store-listing/README.md` §3. Las que más pesan:

| Id | Pregunta | Recomendada |
|---|---|---|
| Q-B13-1, -2, -3 | Dominio de la página de borrado, titular legal en Play y correos de contacto, privacidad y DPO | Los fijan el dueño y el abogado; ninguna forma de «Amazon» |
| Q-B13-5 | El texto de las capturas va «en papel marfil» (07 §10.2), que choca con D-003 (sin papel) | Placa de madera clara o de vidrio del kit de UI |
| Q-B13-9 | `com.unity.modules.unityanalytics` está en `Packages/manifest.json` | build-release comprueba en B10 que esté apagado en release |
| Q-B13-20 | Unity IAP 5.4.x necesita Unity Authentication, que no tiene fila en 08 §13.3 | Quien integre IAP la confirma y se añade la fila |
| Q-B13-21 | El borrado por web solo pide el ID visible: quien conozca el ID de otro puede borrar su cuenta (hueco de 08 §11) | ID largo, aleatorio y distinto del UID, con confirmación y límite de peticiones |
| Q-B13-22 | Cómo se borra la partida de Play Games si el borrado llega por web sin el juego instalado | Decir que se borra al abrir el juego, o que el jugador puede borrar su perfil de Play Games |
| Q-B13-23 | La captura 2, «14 frutas, 14 colores» (07 §10.2), frente a 12 frutas + 2 aguas y ≤ 12 colores por nivel en la 1.0 | «14 colores de frutas y ríos» [P] |
| — | Audio: toda la música `mus_*`, incluido `music/menu.wav` (el del vídeo), es CC0, frente al encargo original con cesión total (07 §9.5). `amb_w01_fauna_02` lleva voces humanas y es de Río de Janeiro. El autor «Tintalú Sound Design» usa la marca | El vídeo y los creativos no usan esa música hasta que haya una pista con cesión total; decidir si se mantiene el encargo original; revisarlo con el asesor y el abogado |

## 2026-10-20 · B6 · Sesiones, Regalo de regreso y notificaciones, solo la lógica (`spark/t5-b6`): HECHA · verificación completa VERDE · lista para integrar

- **Rama:** `spark/t5-b6`, desde `9e5d13f`. En `c4322199` fusiona `origin/integracion-fase1-fase2` (`dbbba004`, con FX16a y FX16b). Empujada.
- **Nota de diseño:** `docs/dev/sesiones-notificaciones.md`. Recoge las reglas, los parches, los riesgos del programador real y las preguntas B6-Q1 a B6-Q18.
- **Commits** (del más antiguo al más reciente):
  - `a387b0af`: lógica sin motor y tests;
  - `c08b4c50`: fábrica de almacenes, sin sesión nueva al reconstruir la UI;
  - `93020e1d`: la nota, el CSV y el gancho del programador real;
  - `db587c6f`: los `.meta`;
  - `eec488f7`: B6-profile y B6-bootstrap;
  - `fb8fc1de`: B6-notifications-real;
  - `50a3b0a1`: los arreglos de la revisión;
  - `2ceb2364`: el caso pendiente;
  - `c4322199`: la fusión;
  - y el commit de esta entrada.
- **En la rama** (todo archivo nuevo):
  - `Scripts/Platform/INotificationScheduler.cs` y `FakeNotificationScheduler.cs`;
  - en `Scripts/LiveOps/`:
    - sesiones: `SessionRecord` y `SessionTracker`;
    - regalo: `ComebackConfig`, `ComebackRules` y `ComebackService`;
    - `PushConfig`;
    - pre-prompt: `PushSettingsRecord`, `PushPrePromptRules`, `PushPermissionService` e `IPushPrePromptPresenter`, con `SimulatedPushPrePrompt`;
    - avisos: `NotificationType`, `NotificationContext`, `NotificationPlanner` y `NotificationService`;
    - `SessionProbeFormat`;
  - sus tests EditMode, en `Scripts/LiveOps/Tests/`;
  - `Scripts/UI/GameBootstrap.Sessions.cs` y `SessionRunner.cs`, que no se llaman hasta B6-bootstrap;
  - `docs/dev/traspaso/spark-enganches/B6-strings.csv`, con 21 claves en `pendiente_asesor`.
- **Parches** (`docs/dev/traspaso/spark-enganches/`, en LF). Los tres pasan `git -c core.autocrlf=false apply --check` tras la fusión:
  1. **`B6-profile.diff`:**
     - `Profile.cs` y `ProfileJson.cs`: `settings.push`, que se escribe solo fuera de su default, y `stats.last_seen_at` y `stats.last_win_at` [P], que se omiten mientras son null. Lectura tolerante y sin migración;
     - añade `LiveOps/ProfileSessionStore.cs` y `ProfileSessionStoreTests.cs`.
  2. **`B6-bootstrap.diff`** (después del 1):
     - `SetUpSessions();` en `GameBootstrap.Start`, tras `SetUpStreak();`;
     - `GameBootstrap.SessionsProfile.cs`;
     - `"AF.LiveOps"` en `AF.Tests.EditMode.asmdef`;
     - `GameBootstrapSessionsTests.cs`.
  3. **`B6-notifications-real.diff`** (aparte; no se aplica en las pruebas generales). Antes, anotar la versión en D-033. Trae:
     - el manifiesto;
     - `AF.UI.asmdef`, con la referencia y el `versionDefines` `AF_MOBILE_NOTIFICATIONS`;
     - `AndroidNotificationScheduler.cs`;
     - `GameBootstrap.Notifications.cs`.
  4. **Textos:** las filas de `B6-strings.csv`, a `strings.csv`. Sin ellas no se programa ningún aviso.
- **Choque con B3 y B7 en `GameBootstrap.cs`.** `B3-ui.diff`, `B6-bootstrap.diff` y `B7-bootstrap.diff` añaden cada uno una línea justo después de `SetUpStreak();`. El integrador lo resuelve a mano con este orden:
  ```
  SetUpStreak();
  SetUpSkip();        // B3
  SetUpSessions();    // B6
  ConnectTelemetry(); // B7
  ```
- **Riesgo con B8** (en curso). `B8-profile.diff`, en `spark-t5-b8`, toca `Profile.cs` y `ProfileJson.cs` en otros puntos:
  - Lo probé: aplica (`--check`) encima de B6-profile en el worktree temporal. No hay choque de texto hoy, pero puede haberlo si B8 cambia.
  - Choque de sentido:
    - la firma o integridad del perfil y la fusión en la nube de B8 tienen que cubrir `settings.push` y `stats.last_seen_at` / `last_win_at`;
    - propuesta para la fusión: `last_seen_at` y `last_win_at` por máximo, y `pre_prompt_shown` / `second_prompt_shown` por OR. `granted` sería el del dispositivo, no el de la nube [P].
- **Revisión (code-reviewer):**
  - 1.ª ronda: CAMBIOS NECESARIOS, con 2 bloqueantes, ya arreglados en `50a3b0a1`:
    - Nuevos niveles no salía nunca con el Reto desbloqueado, y Crecida casi nunca. Ahora el orden es Regreso > avisos puntuales > avisos diarios, y un puntual que pierde su día pasa una vez al siguiente;
    - el segundo pre-prompt salía con una racha que ya estaba en 3 o más. Ahora sale solo en la victoria que alcanza el hito 3 después del primero.
  - Sugerencias aplicadas:
    - un aviso sin texto no gasta el cupo del día;
    - el Regreso no se avisa antes de `live_comeback_gift.days`;
    - el arranque no usa el pre-prompt simulado: no gasta los pre-prompts del perfil de pruebas.
  - 2.ª ronda: **APROBADO**.
- **Verificación** (`-Architecture arm64 -TimeoutSec 3600`; como mucho 2 Unity a la vez):

  | Dónde | Comando | Resultado |
  |---|---|---|
  | rama (`50a3b0a1`) | `dotnet build -warnaserror`; `dotnet test`; `validate_schema.py` | 0 avisos; 981 (975 + 6), 0 fallos; 103/103, código 0 |
  | rama (`50a3b0a1`) | `unity-compile`; EditMode; PlayMode | 0; **3.423 (3.420 + 3), 0 fallos** (64 nuevos de B6); **1.589 (1.382 + 207), 0 fallos** |
  | temporal con B6-profile + B6-bootstrap (`50a3b0a1`) | compile; EditMode; PlayMode | 0; **3.432 (3.429 + 3), 0 fallos**; **1.589: 1 fallo, `WorldBannerTravelTests.Travel_MeshesBuildIn8ms` (8,0004 ms frente a un tope de 8 ms, el inestable)**; relanzado con filtro, 13/13 |
  | temporal con el real sin la línea del manifiesto | compile | 0, con los `.meta` del diff; luego revertido |
  | rama tras la fusión (`c4322199`) | `dotnet build -warnaserror`; `dotnet test`; `validate_schema.py`; compile; EditMode | 0 avisos; 981 (975 + 6); código 0; 0; **3.431 (3.428 + 3), 0 fallos**. PlayMode no se repite: la fusión solo trae cambios del escritorio (FX16a/b, BuildRelease) que no tocan el código de B6 |

- **Versión para D-033:** `com.unity.mobile.notifications` **2.4.3**.
  - El repositorio no fija ninguna (08 §7 y §15 solo nombran «Mobile Notifications»).
  - Es la que declara el Editor 6000.3.25f1 instalado (`Editor/Data/Resources/PackageManager/Editor/manifest.json`: `version` y `minimumVersion` 2.4.3). Es un archivo local; no se navegó.
- **Cómo comprobar `POST_NOTIFICATIONS`** tras aplicar el parche real y hacer el APK de desarrollo:
  - `aapt2 dump permissions Builds/Android/dev.apk`, o `bundletool dump manifest` en el AAB;
  - debe listar `android.permission.POST_NOTIFICATIONS`;
  - y **no** `SCHEDULE_EXACT_ALARM`, `USE_EXACT_ALARM` ni `RECEIVE_BOOT_COMPLETED`.
  - En Project Settings › Mobile Notifications: «Schedule at exact time» en `Never`, «Reschedule on Device Restart» apagado y nada que pida el permiso al arrancar.
  - En el teléfono: que no salga ningún diálogo en el arranque en frío.
- **Riesgos del programador real** (detalle en la nota §8):
  - el cuerpo del `#if` no está compilado sin el paquete;
  - faltan los iconos (arte);
  - `af_push_sent` es aproximado;
  - hay que pasar `ci/check-16kb.sh` y `ci/size-gate.ps1`.

### Preguntas para el dueño (B6; se avanzó con la recomendada, marcada [P])

| Id | Pregunta | Recomendada |
|---|---|---|
| B6-Q9 | Con 1 aviso al día y la prioridad literal de 06 §8.2, el Reto listo de cada día haría que Nuevos niveles no saliera nunca y que Crecida te espera casi nunca saliera | (b): Regreso > puntuales (Crecida > canoas > nuevos niveles) > diarios (Reto > Bitácora); un puntual que pierde su día pasa una vez al siguiente |
| B6-Q1 | Con qué se miden los días sin sesión | `stats.last_seen_at` nuevo, en periodos de 24 h |
| B6-Q2 | Qué guarda `settings.push` para el segundo pre-prompt | `pre_prompt_at` + `second_prompt_shown`; sale en la victoria que alcanza el hito 3 después del primero o desde 7 días después |
| B6-Q3 | ¿Se avisa antes de «Avísame»? | No; `granted` solo se baja al abrir |
| B6-Q4 | «Racha de Retos» en la 1.0 | Sí, como variante del Reto listo |
| B6-Q5 | Días del Regreso | D7, D14, D30 y D60; los textos de D14, D30 y D60 son propuestos |
| B6-Q6 | `source` de las semillas del regalo | `event` |
| B6-Q7 | «Una victoria más» aunque falten más | Texto con `{left}` y plural |
| B6-Q8 | Título y canal de Android | «TiTino» y «Avisos del río» |
| B6-Q10 | Reparto sin abrir | 3 por semana; los diarios en días alternos |
| B6-Q11 | Aviso en horas silenciosas | Se mueve a las 09:00 |
| B6-Q12 | Tope diario después del día 7 | 1 al día siempre |
| B6-Q13 | Hora del Reto listo | 09:30 |
| B6-Q14 | Crecida con las 36 h ya pasadas | Sin aviso |
| B6-Q15 | Anti-reloj del regalo | Con un desfase mayor que `sec_clock_max_drift_h`, sin regalo, y ese regalo se pierde. Sin red, cambiar el reloj lo repite |
| B6-Q16 | Cuándo sale la tarjeta del pre-prompt | Tras el panel de la victoria y antes de la Mochila; el disparador se mueve ahí con la tarjeta (UI de otro bloque) |
| B6-Q17 | Valores de `af_push_prompt` | `result` ∈ {`accepted`, `declined`, `granted`, `denied`}; `origin` ∈ {`first`, `second`, `settings`} |
| B6-Q18 | Valores de `type` de los avisos | `daily_ready` … `comeback_d60` |

- **Caso pendiente:**
  - Si el primer pre-prompt sale en la victoria que deja la Crecida en 3, y después se gana un Reto que no mueve la racha, el segundo saldría en esa victoria.
  - Para cerrarlo haría falta guardar la racha del primero (otro campo [P]).
- **Falta UI** (fuera de B6): la tarjeta del pre-prompt, la fila «Avisos» de Ajustes y el aviso del Regalo de regreso (`GameBootstrap.LastComeback`).

## 2026-10-20 · B3 · Salto del Boto (`spark/t5-b3`): HECHA · verificación completa · lista para integrar

- **Rama:** `spark/t5-b3`, desde `origin/integracion-fase1-fase2` `9982bdc` (con B1a). Sin push (lo pidió el encargo de esta tanda).
- **Nota de diseño:** `docs/dev/salto-del-boto.md` (reglas S1-S10, reparto, preguntas Q-S1-Q-S9, riesgos).
- **Commits:** `d8f1fb4` (lógica, vista y tests en archivos nuevos), `62963e9` (almacén según el progreso), `e9440f7` (nota y parches),
  `28a6876` y `bd4be8d` (revisión: el Reto sin Salto, la Cuia extra, la clave `skip`), `ad2ce8b` (.meta y B3-ui.diff regenerado),
  y los dos del arreglo de las mascotas (Rosa se pinta al enseñarla; B3-ui.diff regenerado, `e79f624`).
- **Archivos nuevos:** `Scripts/Boosters/SkipBooster.cs`; `Scripts/GameFlow/SkipStore.cs` y `LevelFlow.Skip.cs`; `Scripts/UI/SkipText.cs`,
  `SkipUi.cs`, `GameBootstrap.Skip.cs`, `SkipRunner.cs` y `DevProbe.Skip.cs`; tests EditMode `SkipBoosterTests`, `SkipFlowTests`,
  `SkipProbeTests` y `SkipDailyTests`.
- **E34 cumplido:** `LevelFlow.Skip()` llama a `OnSkipStreak()` dentro de su `using (Saves())`, tras el cobro y **antes** de
  `ISkipStore.SaveSkip`; el cobro, la Canasta del 10k, el salto y el `level_state` del nivel siguiente salen en **una** escritura
  (`SkipFlowTests.SkippingA10k_PaysItsBasket_AndEverythingGoesOutInOneWrite`). El nivel saltado **cuenta para `max_level`** (sin
  `stats.levels_won`), como pedía el aviso del revisor de B1a.
- **Revisión (code-reviewer):** 1.ª ronda CAMBIOS NECESARIOS (sin test del Reto, sin test del aviso de la Cuia extra, clave del CTA fuera
  del glosario, faltaba Unity con los parches); arreglado. 2.ª ronda: solo faltaba commitear el .meta y el B3-ui.diff regenerado (hecho en
  `ad2ce8b`); «aprobado sin otra ronda de código» con PlayMode completo en verde.

### Enganches para el integrador (en este orden, en LF, en `docs/dev/traspaso/spark-enganches/`)

1. `B3-model.diff`: `StuckModalModel.cs` (`StuckContext.Skip`, `StuckModalView.Skip`: la rama D) y `LevelFlow.Funnel.cs`
   (`BuildStuckContext` rellena `Skip = SkipOffer`); añade `Tests/EditMode/StuckModalSkipTests.cs` (+ .meta).
2. `B3-ui.diff` (necesita el 1): `StuckModal.cs` (la opción con precio y «Rosa te lleva…» o «Te faltan»), `PauseMenu.cs` (botón con Rosa y
   precio), `GameScreen.cs` (pasa `flow.SkipOffer` y `OnPauseSkip`), `GameBootstrap.cs` (`SetUpSkip();` tras `SetUpStreak();`); añade
   `Tests/PlayMode/SkipPlayModeTests.cs` (+ .meta).
3. `B3-strings.csv`: 6 claves (`stuck_skip`, `skip_tagline`, `skip_confirm_title`, `skip_confirm_extra_cuia`, `skip`, `skip_banner`), a
   copiar en `strings.csv`; `pendiente_asesor`.
- Sin parche de `Profile`/`ProfileJson` (ya guardan `since_last_skip` y `skipped_levels`) ni de `RiverMapScreen*` (ya pinta a Rosa con
  `max_level` + `skipped_levels`).
- Prueba: `git apply` de 1 y 2 sobre la rama, `unity-compile`, EditMode y PlayMode (`StuckModalSkipTests`, `SkipPlayModeTests`).
- **Choque con `B7-bootstrap.diff`:** `B3-ui.diff` y `B7-bootstrap.diff` insertan una línea en el mismo punto de `GameBootstrap.cs`,
  justo después de `SetUpStreak();`. Se resuelve a mano dejando primero `SetUpSkip();` y después `ConnectTelemetry();`.
- **Rehecho sobre FX16a/FX16b** (`e11b424`): `B3-ui.diff` se regeneró sobre el `StuckModal.cs` nuevo; la rama D usa las medidas de la
  disposición compacta (`FitCtaLabelCompact`, `PlaceButton`, `HeightOf`, `gap`), como la Cuia de rescate.

### Verificación

| Dónde | Comando | Resultado |
|---|---|---|
| rama | `dotnet build dotnet/AmazonFill.slnx -warnaserror` | 0 avisos, 0 errores |
| rama | `dotnet test dotnet/AmazonFill.slnx` | 981 (975 + 6 omitidos), 0 fallos |
| rama | `python tools/validate_schema.py` | 103/103 PASS, sin cambios en `tools/out/` |
| rama | `unity-compile.ps1 -Architecture arm64` | OK (código 0) |
| rama | EditMode (`-TimeoutSec 3600`) | **3.384 (3.381 + 3 omitidos), 0 fallos** |
| rama | PlayMode (`-TimeoutSec 3600`) | 1.589 (1.380 + 207 omitidos), **2 fallos de tiempo** con otro Editor (B2) en marcha: `WorldBannerTests.Banner_TimeoutAndWaitMs_UseTheRealClock` y `WorldBannerTravelTests.Travel_MeshesBuildIn8ms` (10,63 ms > 8); relanzados con filtro: **30/30 verdes** |
| worktree temporal con B3-model + B3-ui | `git apply` de los dos | aplican limpios |
| ídem | `unity-compile.ps1` | OK |
| ídem | EditMode | **3.389 (3.386 + 3 omitidos), 0 fallos** |
| ídem | PlayMode | 1.ª pasada: 1 fallo real, `WorldBannerTests.Banner_NotOnRetryNorResume` (`MascotsBuilt`): la confirmación, el banner y la pausa pintaban a Rosa al arrancar; arreglado en `e5519fa` y `e79f624` (Rosa solo al enseñarlos). 2.ª pasada: **1.590 (1.383 + 207 omitidos), 0 fallos** |
| rama tras el arreglo | `unity-compile.ps1` | OK |
| retoques tras la aprobación (`cabddef`, `a82a97e`) | rama: `dotnet build`/`test`, `validate_schema`, compile, EditMode, PlayMode | 0 avisos; 981 (975 + 6); 103/103; OK; **3.384 (3.381 + 3), 0 fallos**; **1.589 (1.382 + 207), 0 fallos** |
| ídem | worktree con B3-model + B3-ui: compile, EditMode, PlayMode | OK; **3.389 (3.386 + 3), 0 fallos**; **1.590 (1.383 + 207), 0 fallos** (`SkipPlayModeTests` en verde) |
| tras fusionar `e11b424` (FX16a/FX16b; `c975184`, `be82d8d`) | rama: `dotnet build`/`test`, `validate_schema`, compile, EditMode, PlayMode | 0 avisos; 981 (975 + 6); 103/103; OK; **3.392 (3.389 + 3), 0 fallos**; **1.655 (1.425 + 230), 0 fallos** |
| ídem | worktree con B3-model + B3-ui regenerado: `apply --check` en orden, compile, EditMode, PlayMode | aplican; OK; **3.397 (3.394 + 3), 0 fallos**; **1.656 (1.426 + 230), 0 fallos** (`SkipPlayModeTests` en verde) |

### Decisiones [P] y preguntas para el dueño

Detalle en la nota §3 (todas con la recomendada aplicada):
- **Q-S1:** `last_skip_level` se deduce del mayor de `skipped_levels` (sin campo nuevo); `since_last_skip` se pone a 0 y no se lee. El ejemplo
  de 08 §4.3 no casa con ninguna lectura: hay que corregir el GDD.
- **Q-S2:** hueco N − último ≥ 10 (salto en el 40 → el 50 ya puede).
- **Q-S3:** banner como franja con Rosa sobre el tablero mientras el intento no empezó (tarjeta, intro o sin vertidos).
- **Q-S4:** sin nivel siguiente o sin canoas para empezarlo, el Salto no se ofrece.
- **Q-S5:** la animación de Rosa (1,5 s) y su sonido esperan al arte y al audio.
- **Q-S6:** `stuck_skip` partido en etiqueta + precio + `skip_tagline`.
- **Q-S7:** se ofrece en Aguas estancadas, en la pausa y en el banner; no en ¿Seguro?, Nivel fallido ni Sin canoas.
- **Q-S8:** textos sin canon («¿Saltar el nivel?», «Perderás la Cuia extra de este nivel»), `pendiente_asesor`.
- **Q-S9:** `skip_min_level`: 05 dice «mínimo 31», 08 §9 da 20-31; el cliente solo exige ≥ 1 (el revisor sugiere no bajar de `rescue_min_level`).
- Nueva (revisor): ¿se enseña al jugador la Canasta que paga el salto de un 10k? Hoy solo sale en la sonda.
- **Q-S1, consecuencia:** `since_last_skip` ya no se usa; corregir 02 §2, 04 §6.1-§6.2 y el ejemplo de 08 §4.3, y avisar a B7
  (`af_skip_used.since_last_skip` = nivel − último salto) y a B8 (fusión en la nube).
- **Q-S6, consecuencia:** toca la tabla de 04 §6.5.
- **Pendiente:** medir el primer pintado de Rosa con `MascotsMainMs` (tope de 8 ms, 08 §5.4).
- **Retoque tras la aprobación:** el banner sin saldo se enseña en gris con «Te faltan {n} semillas» (clave existente
  `stuck_rescue_missing`, sin texto nuevo) y no responde al toque.

### Riesgos

- La rama D suma ~70 dp al panel de Aguas estancadas: el caso conocido de 16:9 al 130 % empeora si coinciden B y D (sin medir).
- `af_skip_used` solo en la sonda de desarrollo hasta B7; `SkipConfig.Defaults` hasta el lector de Remote Config.
- Sugerencias del revisor no hechas: id del banner en la AF_LAYOUT `level` (toca `GameScreen`), «Te faltan N» en la pausa sin saldo,
  rama D en un test de tamaños.

---

## 2026-10-20 · B2 · Pororoca (`spark/t5-b2`): HECHA · verificación completa VERDE · lista para integrar

- **Rama:** `spark/t5-b2`, desde `origin/integracion-fase1-fase2` en `9982bdc` (con B1a). Sin empujar (el encargo de B2 dice «no hagas push»).
- **Nota de diseño:** `docs/dev/pororoca.md` (qué va en la rama y qué en parches, el algoritmo, el flujo, la vista, las [P] P-1 a P-19 y los hallazgos para el GDD).
- **Commits:** `eaad940` (código de la rama: `ShuffleBooster`, `IShuffleListener`, `ProceduralArt.Shuffle`, `ShuffleText`, `ShuffleConfirm`, `ShuffleIntroCard`, `ShuffleWaveFx`, `IShuffleActions` y `ShuffleBoosterTests`, con sus `.meta`) y el de la nota, los parches y esta entrada.
- **Revisión (code-reviewer):** 1.ª ronda, CAMBIOS NECESARIOS (faltaban commit, parches y `.meta`; la contradicción 03 §9 / 04 §2.4; tres tests). Arreglado (P-16 y P-17; tests `UnknownStartingPar…`, `OpenedOrReleasedMechanics…` y `MurkyLevel_Firefly_Shuffle_Resume…`; el cobro comprobado; la placa de la tarjeta sin raycast y el bloqueador de la ola con `HitArea`). 2.ª ronda: **APROBADO**, condicionado al commit y a la verificación completa, hechos abajo.
- **Enganches para el integrador** (en `docs/dev/traspaso/spark-enganches/`, LF; `git apply --check` OK). Se aplican **después de fusionar `spark/t5-b2`** y **en este orden**:
  1. `B2-solver.diff`: `Packages/com.amazonfill.core/` (`AF.Solver/Shuffle/` nuevo con `Shuffler`, `ShuffleOptions` y `ShuffleResult`; `LevelSession.ApplyShuffle`; `ShufflerTests` y `LevelSessionShuffleTests`) y `dotnet/AF.Core.Tests/DotnetOnly/ShuffleAllocationTests.cs`, con los `.meta` que generó Unity.
  2. `B2-flow.diff`: `Boosters/ShuffleWave.cs` y `GameFlow/LevelFlow.Shuffle.cs` (nuevos); ganchos de una línea en `LevelFlow.cs` (`Load` y `BeginAttempt`); `LevelFlow.Resume.cs` (rehacer con las olas, `RestoreShuffle`, `WriteHistory`); `LevelStateSnapshot.cs` (marca `*` en V, marcas `o` y `p`; h2 sin versión nueva); `Economy/Wallet.cs` (`ShufflesToday` y `daily.shuffles_today` al cobrar); `Tests/EditMode/ShuffleFlowTests.cs`.
  3. `B2-ui.diff`: `BoosterTray.cs` ✱ (ranura Barajar entre Pista y Revelar, globo, latido, `PaintShuffle`); `GameScreen.cs` ✱ (6 ganchos `Shuffle*`); `GameScreen.Shuffle.cs` y `GameBootstrap.Shuffle.cs` (nuevos); `DailyChallengeScreen.cs` (la Pororoca en el Reto); `Tests/PlayMode/ShufflePlayModeTests.cs` (nuevo), `PlayModeHelpers.cs` (`NoShuffleUnlessSet`, P-19) y `LocTablesTests.cs` (`ShuffleText.AllKeys`).
  4. `B2-strings.csv`: 8 filas para `strings.csv` (`shuffle`, `shuffle_confirm`, `shuffle_confirm_yes`, `shuffle_failed`, `shuffle_low_water`, `shuffle_name`, `shuffle_tagline` y `shuffle_unsupported`), solo `es-419` y `pendiente_asesor`, en orden ordinal. Sin ellas falla `LocTablesTests.EveryUiTextKey_Exists_AndEveryStringsRow_IsUsed`.
- **Verificación:**

  | Dónde | Comando | Resultado |
  |---|---|---|
  | rama (`eaad940`, sin parches) | `dotnet build -warnaserror`; `dotnet test`; `validate_schema.py` | 0 avisos; 981 (975 + 6); 103/103 |
  | rama | `unity-compile -Architecture arm64`; EditMode; PlayMode (`-TimeoutSec 3600`) | OK; **3.368 (3.365 + 3), 0 fallos** (387 s); **1.589 (1.382 + 207), 0 fallos** (2.226 s) |
  | temporal en `9982bdc` + rama + los 3 parches + las 8 filas | `dotnet build -warnaserror`; `dotnet test`; `validate_schema.py` | 0 avisos; **1.002 (996 + 6)**; 103/103 |
  | temporal | `unity-compile`; EditMode; PlayMode | OK; **3.399 (3.396 + 3), 0 fallos** (282 s); **1.593 (1.386 + 207), 0 fallos** (1.863 s) |

  `econ_sim` no se ejecutó: no se toca la economía (los precios y topes son los de 08 §9). `IntroArtTests.FruitFlight…10ms` no falló en ninguna pasada.
- **Decisiones [P]** (con la recomendada; detalle en `pororoca.md` §5): P-1 a P-19. Las que piden al dueño: **P-4** (tope «1 por nivel» en todos los intentos y al salir y volver), **P-5** (Volver a la orilla vuelve al tablero del nivel), **P-6** (sin hoja con el stock), **P-10** (la ola en el hilo principal: medir en el teléfono el peor caso, la que falla), **P-13** (Tinaja con dosis: habilitada y medir en `level-qa`), **P-16** (03 §9 frente a 04 §2.4 en la ola que falla) y **P-18** (bandeja de 5 ranuras en 360 dp: «Shuffle», «Embaralhar», y «Desfazer» y «Revelar» en pt-BR al 130 %, no caben a 12 sp; EN y PT-BR de `shuffle` quedan vacíos).
- **Riesgos:**
  - P-18 es de interfaz: en en y pt-BR la ranura sale sin etiqueta hasta decidirlo.
  - Con P-19 (`GameScreen.ShuffleOverride`, solo vista), los tests PlayMode que no tratan de la Pororoca corren sin ella; la bandeja de 5 ranuras desde el 30 la cubre `ShufflePlayModeTests`.
  - La analítica (`af_shuffle_result`, `af_booster_used`) sale solo como sonda `AF_STATE shuffle` hasta B7.

---

## 2026-10-20 · S2 = B1a · Crecida + Hoja escudo + Canasta (`spark/t5-b1a`): HECHA · verificación completa VERDE · lista para integrar

- **Rama:** `spark/t5-b1a`, desde `a759597`. Fusiona `origin/integracion-fase1-fase2` (`815b2e5`) en `7fbc1cd`.
- **Plan:** `docs/dev/plan-tanda5.md` (S1, en `spark/auditoria-1-0`). **Nota de diseño:** `docs/dev/crecida-canasta.md`, con las reglas C1-C14 y K1-K9, los enganches E26-E35 y las preguntas Q-C1-Q-C14.
- **Tareas y revisiones.** Cada tarea pasó por el code-reviewer; todas quedaron **APROBADAS**, la mayoría en la 2.ª ronda:
  - **A0** (`f68f72c`): la nota.
  - **A1** (`65754d3`): `StreakConfig`, `BasketConfig` y `EconomyJsonLite`. Los defaults se comprueban contra la tabla de 08 §9 leída de verdad.
  - **A2** (`a38653c`): `BasketRules`.
    - Canasta cada 10 niveles: el 10 da solo semillas; el ciclo es `(L/10−2) mod 5`.
    - Canasta grande cada 100: 150 semillas + Cuia extra + Morpho + 3 Marea (GDD.md §6.1 #13).
    - `HitSix`.
  - **A3** (`ead3aa2` y `1a09563`): `StreakRules`.
    - Hitos 3, 6 y 10, una vez al día; tope de 600 con pago parcial; río crecido; Hoja escudo; solo en la campaña y desde el 25.
    - Tests sin asignaciones y un oráculo contra `econ_sim.py`: 5 configuraciones × 24 semillas × 600 pasos.
    - K9: el Salto del Boto también paga la Canasta del 10k (04 §2.6).
  - **A4** (`f226165`, `f03e0ef` y `fb2bd3e`): `StreakRecord`, `IStreakStore`, `InMemoryStreakStore` y el parche S1.
    - Regla conservadora con un registro de un día anterior: puede pagar de menos en el borde de medianoche, nunca dos veces.
    - `Normalize` al leer.
  - **A5** (`cd83385`, `9e3a88d` y `8e978ec`): `StreakService`, con `ApplyWin`, `ApplyHardFail`, `ApplySkip` y `Peek`.
    - Orden: `Load` → `RollDay` → regla → `Save` → `Wallet.Apply`.
    - Fuentes `streak` y `basket`. El kill switch congela la Crecida, pero la Canasta se sigue pagando.
    - La Canasta de un 10k no se repaga, gracias a `maxLevelBefore`.
    - El parche S4.
  - **A6** (`c34cb00`, `701eb56` y `b3258fd`): el parcial `LevelFlow.Streak.cs`.
    - `UseStreak`, `LastStreakResult`, `PeekStreak` y `OnSkipStreak`. `OnSkipStreak` pide `RequireDurable` y va dentro del mismo `Saves()` que el salto.
    - El Reto queda fuera (`!IsDaily`).
    - Con el río crecido no se ofrece el x2 por vídeo.
    - Los parches S2 y S5.
  - **A7** (`546fcf3`, `685bad9` y `04cda21`): `StreakProbeFormat`, `DevProbe.Streak.cs`, `GameBootstrap.Streak.cs`, `StreakProbeRunner` y el parche S3.
    - Líneas `AF_STATE` (solo en desarrollo): `streak_state` y `streak_fail`, más `streak_milestone`, `streak_shield_used`, `free_double_harvest` y `basket_open`.
    - En la victoria salen tras `AF_STATE win` y **antes** de `victory`; en el fallo duro, tras `fail_hard` y `level_failed`.
- **Parches para el integrador** (`docs/dev/traspaso/spark-enganches/`, en LF). Se aplican **en este orden** y **después de fusionar `spark/t5-b1a`**, porque S4 reescribe archivos de la rama:
  1. `S1-profile.diff`: `Profile.cs` (`streak` {`wins`, `river_full`}; `daily.streak_hits` y `daily.streak_seeds`), `ProfileJson.cs` y `DailyCounters.RollOver`; añade `ProfileStreakStore.cs` y `StreakProfileTests.cs`.
  2. `S4-wallet.diff` (E35): `Transaction.DoubleHarvestRiver` (`internal`), sin la compuerta `ads_rv_double_harvest_from_level` y sin gastar `ads_rv_cap_double_harvest`; añade `RiverDoubleHarvestTests`.
  3. `S2-flow.diff` (E29, E30): `LevelFlow.Victory.cs` (`ResolveWin`) y `LevelFlow.Funnel.cs` (`Fail`); añade `StreakFlowHookTests`.
  4. `S5-flow-profile.diff`: test de integración que comprueba una sola escritura del perfil en la victoria y en el fallo duro (necesita S1 y S2).
  5. `S3-bootstrap.diff` (E32): `SetUpStreak();` tras `SetUpDaily();` en `GameBootstrap.cs`, y `GameBootstrap.StreakProfile.cs` (necesita S1).
- **Pendiente de otros bloques:**
  - B3 (`SkipBooster`) debe llamar a `flow.OnSkipStreak()` dentro del `using (Saves())` del salto y antes de guardarlo (E34).
  - B1b: barra, toasts y panel de premios (E33, nota §6.1).
  - B7: `earn_virtual_currency` desde `Wallet.Apply`.
  - Lector de Remote Config: hoy se usan `StreakConfig.Defaults` y `BasketConfig.Defaults`.
  - Volver a correr la simulación de economía con la Canasta grande (05 §3.4).
- **Verificación:**

  | Dónde | Comando | Resultado |
  |---|---|---|
  | rama (`7fbc1cd`) | `dotnet build -warnaserror`; `dotnet test`; `validate_schema.py`; `econ_sim --runs 300` contra `econ_B.md` | 0 avisos; 981 (975 + 6); 103/103; idéntico |
  | rama | `unity-compile`; EditMode; PlayMode (`-TimeoutSec 3600`) | OK; **3.309 (3.306 + 3), 0 fallos**; **1.589 (1.382 + 207), 0 fallos** |
  | worktree temporal en `7fbc1cd` con S1 → S4 → S2 → S5 → S3 | `git apply` de los 5; `unity-compile`; EditMode; PlayMode | todo aplica; OK; **3.359 (3.355 + 3), 1 fallo: `IntroArtTests.FruitFlight…10ms`, el inestable (tope fijo de 10 ms)**; relanzado con filtro, 9/9; **1.589 (1.382 + 207), 0 fallos** |

- **Decisiones [P]** (se avanzó con la opción recomendada; detalle en la nota §7):
  - Q-C1 a Q-C14. Destacan:
    - **Q-C2 (a):** el Reto no lleva el x2 gratis del río. Se aparta de una lectura literal de 04 §2.8; se apoya en 06 §4.3.
    - **Q-C5 (a):** el comodín del hito 6 se calcula con `HitSix(max(level, max_level))`.
    - **Q-C6 (a):** una sola hoja al día.
    - **Q-C13 (a):** las 50 semillas del hito 6 van con `source = streak`, sin `af_basket_open`.
    - **Q-C14 (a):** el x2 del río no pasa por la compuerta del vídeo.
  - **Q-M9 (a):** esquema del perfil; un solo `shield_available`, el de `daily`.
  - **Q-M10:** el módulo `AF.Economy`.
  - **Sonda:** `level` en `streak_milestone` y `streak_shield_used` es solo de la sonda; no está en 08 §8.2.

### Preguntas para el dueño (nuevas; no bloquean lo entregado)

| Id | Pregunta | Recomendada |
|---|---|---|
| B1a-Q1 | Fusión con la nube (08 §11): «`daily` se fusiona con max por clave» no sirve para los campos nuevos. El `max` de `shield_available` devolvería un escudo ya gastado, y `streak_hits` es una lista | `shield_available` = true solo si las dos copias lo tienen; `streak_hits` por unión; `streak_seeds` por max |
| B1a-Q2 | ¿Un cambio que solo toca `streak` cuenta como mutación de economía para `economy_ts`? Hoy no lo actualiza: en un fallo duro sin hoja en 25-30 podría ganar la copia antigua en un conflicto con el mismo `max_level` | Sí: que `streak` actualice `economy_ts` (cambio pequeño en S1 cuando lo decidas) |
| Q-M2 | Diseño del panel único de premios (D-029 #18) | Bloquea B1b y B4 |

## 2026-10-20 · S1 · Auditoría de la 1.0 y plan de la tanda 5 (`spark/auditoria-1-0`): HECHA · lista para integrar (solo documentación)

- **Rama:** `spark/auditoria-1-0`, desde `origin/integracion-fase1-fase2` en `a759597`.
- **Commits:**
  - `ee5bf2e`: el plan;
  - `81c4fd2` y `8c8354e`: la revisión.
- **Archivo:** `docs/dev/plan-tanda5.md`.
- **Qué contiene:**
  - las piezas de la 1.0 en cuatro áreas (núcleo y partida; meta y liveops; economía y monetización; técnica y publicación), con su estado (hecha / a medias / sin empezar) y la evidencia en código y tests;
  - lo que bloquean el dueño o terceros;
  - la tanda 5 en 14 bloques de 1-3 días (B1a, B1b, B2-B14), con sus choques con el escritorio;
  - las preguntas, cada una con su opción recomendada;
  - una sección «Fuera de la tanda 5»;
  - hallazgos para el GDD.
- **Método:** cuatro auditorías de solo lectura sobre el código y los tests reales, y después la síntesis.
- **Verificación:**
  - `python tools/econ_sim.py --runs 300 | diff --strip-trailing-cr -q - tools/out/econ_B.md` → idéntico;
  - `python tools/validate_schema.py` → 103/103;
  - sin builds, por ser solo documentación.
- **code-reviewer:**
  - primera ronda CAMBIOS NECESARIOS:
    - preguntas que reabrían D-020 y D-029 sin decirlo;
    - B1 con preguntas abiertas;
    - piezas sin bloque;
    - citas erróneas;
    - faltaba esta entrada;
  - segunda ronda **APROBADO** (con esta entrada).
- **Resumen de la auditoría:**
  - El núcleo y el meta del Mapa están completos, igual que la lógica de cliente del embudo de D-008, con anuncios y compras simulados.
  - Faltan, con el canon cerrado:
    - Crecida + Hoja escudo, Canasta, Bitácora, Pororoca, Salto del Boto;
    - notificaciones (la lógica) y regalo de regreso;
    - Mercado y ofertas;
    - `AF.RemoteConfig` y `AF.Analytics`;
    - nube y fusión, consentimiento y edad;
    - accesibilidad (TalkBack, escala de fuente, Reducir movimiento, Alto contraste);
    - compuertas de release.
  - Todo lo «online» real está bloqueado por el dueño: SDK, `INTERNET`, cuentas, Addressables y PAD, secretos, keystore y marca.
- **S2 elegido: B1a** (reglas de la Crecida + Hoja escudo + Canasta: perfil, RC, sonda y tests, en archivos nuevos). Cumple los tres criterios del encargo §11:
  - (a) no depende de nada bloqueado;
  - (b) sus preguntas (Q-M9, esquema del perfil; Q-M10, módulo) tienen opción recomendada;
  - (c) no toca los archivos que el escritorio está cambiando ahora. Sus ganchos van a `FailResolve`, `LevelFlow.Victory`, `Profile` y `SlothContext`, como parches probados, porque son archivos de §3.2.
- **Pendiente del dueño:**
  - **antes de cerrar B1a:** Q-M9 y Q-M10. Si no llegan, B1a avanza con las recomendadas, marcadas [P];
  - **antes de B1b y B4:** Q-M2 (diseño del panel único de premios, D-029 #18);
  - **el resto de las preguntas:** en `docs/dev/plan-tanda5.md` §5.

## 2026-10-20 · T2 · Traducción con IA a inglés y portugués (`spark/traduccion-ia`): HECHA · verificación completa VERDE · lista para integrar

- **Rama:** `spark/traduccion-ia`, desde `origin/integracion-fase1-fase2` (`e7475d2`).
  - Fusiona `27a7e9f` en `2dd8866` y `cb0f96f` en `5254a64`.
  - Commits propios: `0a05c6f`, `f73700b`, `d39a6ec`, `f9137ca`, `86dcdc8`, `7dec1d3` y `a9bdd2c`.
- **Regla:** la rama **no edita** `strings.csv` ni `facts.csv`. Lo deja todo para que el integrador lo aplique al cerrar la ola 7.
- **Qué entrega:**
  - `docs/dev/traspaso/spark-traducciones/`: los CSV `key,es-419,en,pt-BR,status_en,status_pt-BR`, con el español con el que se tradujo para detectar cambios, y `pendientes.csv`, con cada celda vacía y su motivo.
  - `tools/loc/apply_translations.py`, solo biblioteca estándar, con `--check` y `--self-test` (46/46).
    - Toca únicamente `en`, `pt-BR` y sus estados.
    - Nunca pisa una celda rellena y nunca escribe en `map.*` sin `stops-preserve.diff`.
    - Retiene las filas de `facts.csv` cuyo español está por debajo de `aprobado_es`.
    - Se niega a aplicar una entrega sin su parche.
    - Aplica lo mismo a `spark-strings-reto.csv`.
  - **Estado** de cada celda traducida: `traducido` (contrato 32, 08 §4.7: vacío → `traducido` → `revisado_nativo`).
  - `Assets/_Project/Tests/PlayMode/TranslatedLocaleOverflowTests.cs`: mide los textos en `en` y `pt-BR`, al 100 % y al 130 %.
- **Parches** (`docs/dev/traspaso/spark-enganches/`, con LF):
  - `glosario.diff`: `glossary.csv` con los tipos nuevos `place` y `fauna`, los EN y PT de comodines, mecánicas, lugares y términos (lo cultural, pendiente del asesor y [P] de T2), y el ajuste de `GlossaryTableTests`;
  - `glosario-gdd.diff`: la sección «12.» de `docs/gdd/00-glosario.md` y la frase de 08 §4.7;
  - `stops-preserve.diff`: `tools/river-map/stops.py` conserva `en`, `pt-BR` y sus estados, con `test_stops_preserve.py`;
  - `LocReleaseGate.diff`: en release, la UI en `en` o `pt-BR` sin `revisado_nativo` bloquea (D-018, Q5-A), con 2 tests;
  - `MapTextTests.diff`.
- **Orden para el integrador:**
  1. Los cinco parches.
  2. Fusiona `spark/reto-cierre` (T1) con E5b.
  3. `python tools/loc/apply_translations.py` (por defecto: `strings.csv` y `strings_p14.csv`).
  4. El script con `strings_mapa.csv`, `strings_glosario.csv`, `strings_mapa_glosario.csv` y `strings_t1.csv` como argumentos.
  5. Las entregas de `facts`. Se pueden pasar ya: no escriben nada hasta que el español llegue a `aprobado_es`.
  6. Si entran las comillas “ ” de los textos largos, regenera los atlas con `FontSetup.ApplyAll`.
- **Recuentos** (celdas `en` / `pt-BR`):

  | Entrega | Cuándo se aplica | en | pt-BR |
  |---|---|---|---|
  | `strings.csv` (incluye R12 y `victory_perfect`) | por defecto | 91 | 95 |
  | `strings_p14.csv` | por defecto (T2-Q2) | 8 | 8 |
  | `strings_mapa.csv` | con `MapTextTests.diff` | 19 | 19 |
  | `strings_glosario.csv` | con `glosario.diff` | 54 | 50 |
  | `strings_mapa_glosario.csv` | con los dos | 2 | 2 |
  | `strings_t1.csv` | cuando estén las claves de T1 | 2 | 2 |
  | `facts.csv`, `facts_glosario.csv`, `facts_mapa.csv` | retenidas (T2-Q6), 370 filas | 201 + 46 + 123 | 201 + 46 + 123 |

  - **Con todo aplicado:**
    - en `strings.csv` quedan 7 claves vacías en `en` y 8 en `pt-BR`: «Amazonas» y «Amazonía» (T2-Q7), Totuma sin glosario, `fact_sheet_title` con su §10 pendiente y el PT de `fauna_sighted_card`, pendiente del asesor;
    - en `facts.csv` quedan 430 por idioma: 128 `map.*.source` (nombres de fuente, pregunta 57 (a)), 185 frases `map.*` y 117 de fruta, río y fauna (lugares, fauna, pueblos o comidas sin glosario, o «Amazonía»).
  - **LocReleaseGate:** 814 problemas sin `LocReleaseGate.diff` y 816 con él (las 2 líneas nuevas, «UI sin `revisado_nativo`» en `en` y en `pt-BR`).
- **Verificación:**

  | Dónde | Comando | Resultado |
  |---|---|---|
  | rama (`a9bdd2c`) | `dotnet build -warnaserror`, `dotnet test`, `validate_schema.py`, `apply_translations.py --self-test` | 0 avisos; 981 (975 + 6); 103/103; 46/46 |
  | rama | `unity-compile`, EditMode, PlayMode (`-TimeoutSec 3600`) | OK; **3.148 (3.145 + 3), 0 fallos**; **1.513 (1.326 + 187), 0 fallos** |
  | worktree temporal con los 5 parches y todas las entregas | EditMode | **3.150 (3.147 + 3), 0 fallos** |
  | ídem (sobre la base anterior `27a7e9f`) | PlayMode, con `TranslatedLocaleOverflowTests` | 1.399 (1.232 + 167), 0 fallos; en y pt-BR al 100 y 130 %, 147 textos sin problemas |
  | copias de `git archive` de la rama y de `cb0f96f` | `git apply` de los 5 parches; las 9 entregas; `--check`; `stops.py --check`; unittest de `river-map` | todo OK; 394 celdas, 370 filas retenidas, `facts.csv` idéntico |

- **code-reviewer:**
  - primera ronda CAMBIOS NECESARIOS: `facts` contra 08 §4.7, y «Amazonía» y regiones sin glosario;
  - segunda ronda CAMBIOS NECESARIOS: `.patch` con CRLF, «Encuentro de las Aguas» y «Kew» sin glosario, y faltaba la parte del GDD;
  - tercera ronda **APROBADO**, a falta de Unity (después, verde).
- **Calidad:** el revisor repasó todas las traducciones en los dos idiomas: tuteo, «você» e imperativo; marcadores, plurales, cifras D-018 y NBSP intactos; ninguna cadena «Amazon»; lo cultural, solo con el glosario.
- **Criterio para la fauna y la fruta genéricas:**
  - sin glosario se traducen las categorías de vocabulario general (mono, ave, pez, ardilla…) y los alimentos de diccionario (piña, mango, café…);
  - solo con el glosario, los nombres de especie o de la región (tapir, tucán, cocona…) y las preparaciones locales (yuca, fariña…).

### Preguntas para el dueño (nuevas; no bloquean lo entregado)

| Id | Pregunta | Qué desbloquea |
|---|---|---|
| T2-Q8 | Entrada del glosario para «Amazonas», «Amazonía» y «Sobre la Amazonía» (con el asesor; el glosario §1 evita la cadena «Amazon» por la marca) | 3 claves de UI, unas 60 frases de `facts.csv` y muchas `map.*` |
| T2-Q9 | Mundo 3, «Manaos»: es invariante y `glossary.csv` no admite dos filas con el mismo es-419. ¿Pasa a «Manaus» en EN y PT? | `fact.agua_negro.10` y `.19`, el dato de *Saguinus bicolor* y `map.stop.manaos.*` |
| T2-Q10 | Textos propuestos para el glosario §10: `fact_sheet_title` «River Fruits» / «Frutas do Rio», las filas de P14 y las de `map_*` | `fact_sheet_title` |
| T2-Q11 | Propuestas [P] para el asesor: PT «Redemoinho» o «Rebojo» (Remolino); EN «Tide» (Marea), «Firefly» / «Vaga-lume» (Luciérnaga); «Noite da Onça»; «boto» como PT de «delfín de río»; «Meeting of Waters» | Validación de `glosario.diff` |
| — | La revisión nativa de `en` y `pt-BR`: con `LocReleaseGate.diff`, un build de release se para hasta que exista | Publicar en `en` y `pt-BR` |

## 2026-10-20 · T2 · Decisiones del dueño sobre las preguntas de la traducción con IA

El dueño respondió, literal: «las preguntas t2 hagamos lo recomendado». Se aplica la opción recomendada de cada una. T2-Q6 y T2-Q7 salieron después, de la revisión de T2, y también se resuelven con la opción recomendada:

| Id | Pregunta | Decisión (la recomendada) |
|---|---|---|
| T2-Q1 | El bloque `map.*` de `facts.csv`: `tools/river-map/stops.py` escribe `en` y `pt-BR` vacíos al regenerar | (a) `stops.py` conserva las columnas `en` y `pt-BR` y sus estados. Va como **parche probado**, porque `tools/river-map/**` es del integrador (encargo §3.2). Se traducen las frases `map.*` que solo usan nombres del glosario |
| T2-Q2 | Las 8 claves de P14 (glosario §10: EN/PT a la espera del dueño) | (a) Se aprueban sus textos y se aplica `strings_p14.csv` |
| T2-Q3 | Lugares, países, ciudades y ríos, comodines (Marea, Morpho, Luciérnaga), mecánicas y fauna sin EN/PT en el glosario | (a) Se añaden al glosario: los exónimos habituales de países, ciudades y ríos, y propuestas para comodines, mecánicas y fauna, con lo cultural en `pendiente_asesor` / propuesta. Va como **parche probado** de `Data/Loc/glossary.csv` y del glosario §10. Las traducciones que dependen de esos nombres, en un archivo aparte que se aplica con ese parche |
| T2-Q4 | Términos que se tradujeron sin entrada en el glosario: Cosecha doble (Double Harvest / Colheita dupla), Aguas estancadas (Still waters / Águas paradas), Muestrario (Samples / Mostruário) y «Sobre la Amazonía» (About the Amazon / Sobre a Amazônia) | (a) Pasan al glosario, en el mismo parche que T2-Q3 |
| T2-Q5 | `LocReleaseGate.Evaluate` no exige `revisado_nativo` en un build de release, aunque 08 §4.7 lo pide | (a) Se añade la regla: en release, una cadena de UI en `en` o `pt-BR` sin `revisado_nativo` es aviso o bloqueo, con el mismo nivel que los demás avisos de la puerta (D-018, Q5-A). Va como **parche probado** |
| T2-Q6 | Frases de `facts.csv` con el español aún en `pendiente_asesor` (08 §4.7: una traducción solo vale con `aprobado_es`; `FactCatalogTests` exige los estados vacíos) | (a) **Retener**: la entrega de `facts.csv` queda guardada y se aplica cuando el asesor apruebe el español. El script no escribe filas por debajo de `aprobado_es` y detecta un español cambiado |
| T2-Q7 | «Amazonía» / «the Amazon» / «a Amazônia» y regiones (trópico americano, América tropical, alta Amazonia…) sin entrada en el glosario; el glosario §1 evita la cadena «Amazon» por la marca de Amazon.com | (a) Se dejan **vacías** y anotadas en `pendientes.csv`, hasta que el dueño decida con el asesor la entrada del glosario |

## 2026-10-20 · T1 · Cierre del Reto (`spark/reto-cierre`): HECHA · verificación completa VERDE · lista para integrar

- **Rama:** `spark/reto-cierre`, desde `origin/integracion-fase1-fase2` (`e7475d2`). Fusiona `27a7e9f` (R12 integrado) en `fea96b7`.
- **Commits:**
  - `c7ec205`: E16;
  - `e5061f2`: E17 («Enganche:»);
  - `bac87a7`: E6, E7 y E21 → E25;
  - `3094557`: los parches;
  - `906e78a`: tablas de prueba, `DailyPoolDisabledOverride` y orden de la sonda;
  - `e68a9dd`: `DailyText.E5bKeys`;
  - `053eecc`: E5b, E6 y E7 regenerados sobre la base nueva;
  - `b1b4628`: ajuste de `FunnelPlayModeTests` en `E7.diff`.
- **Qué hace:**
  - **E16 (aplicado):** level-qa enlaza `../level-generator/Daily/*.cs` (salvo `DailyCommand*.cs`), reproduce el pool en memoria con `DailyRunner` y `DailyPackWriter`, y borra `Daily/Shared/`.
  - **E17 (aplicado):** el README de level-qa documenta `--daily`, y su `--self-test` sin nombre ejecuta key2 y daily.
  - **E6, E7 y E5b:** lo del Spark va en la rama; lo de archivos ajenos, como parches probados (fichas en «Enganches para el integrador»).
  - **E21 → E25:** corregida la referencia en `DailyChallengeScreen.cs`.
- **Verificación:**

  | Dónde | Comando | Resultado |
  |---|---|---|
  | rama | `dotnet build -warnaserror`, `dotnet test`, `validate_schema.py` | 0 avisos; 981 (975 + 6); 103/103 |
  | rama | `level-qa --self-test` (sin nombre y `daily`); `--daily` del pool publicado | todo PASS; 2.187 tableros, **0 fallos**, 1.678 avisos |
  | rama (`e68a9dd`) | `unity-compile`, EditMode, PlayMode | OK; **3.083** (3.079 + 3; 1 fallo, el inestable `IntroArtTests`); **1.404 (1.237 + 167), 0 fallos** |
  | worktree temporal con E5b + E6 + E7, sin `b1b4628` | EditMode, PlayMode | 3.083 (3.080 + 3), 0 fallos; 1.411, **2 fallos** en `FunnelPlayModeTests` del 31, los que arregla `b1b4628` |
  | ídem, con `b1b4628` | `unity-compile` + PlayMode | OK; **1.411 (1.244 + 167), 0 fallos** (25 min), tras la revisión de la licencia por el dueño |

- **code-reviewer:**
  - primera ronda CAMBIOS NECESARIOS: base vieja (tablas duplicadas con E5 y «sin pool» con E3);
  - segunda ronda: código, tests y parches bien; un solo bloqueante, de documentación (esta entrada);
  - tercera ronda: **APROBADO** (a falta de Unity, que después salió en verde).
- **[P] para el dueño:** los de E6 y E7, en sus fichas (recomendación: aceptarlos).
- **Para el integrador:**
  - los parches aplican con `git apply --check` sobre la rama y sobre `origin/integracion-fase1-fase2` en `cb0f96f`;
  - `tools/level-generator/README.md:777` aún dice que QA-DAILY-REPRO regenera «en un proceso hijo»; con E16 es en memoria (archivo del integrador).

## 2026-10-19 · Entrega final: R0-R12 en `spark/reto-del-rio`, verificación completa VERDE

Verificación en `spark-int`, sobre la rama con la parte del Spark de R12 (`ac54d82`) y los parches guardados (`bb5b163`, `fa5209e`):

| Comando | Resultado |
|---|---|
| `dotnet build dotnet/AmazonFill.slnx -warnaserror` | código 0 |
| `dotnet test dotnet/AmazonFill.slnx` | 981 (975 + 6) |
| `python tools/validate_schema.py` | 103/103 |
| `unity-compile.ps1 -Architecture arm64` | OK |
| `unity-test.ps1 -Mode EditMode … -TimeoutSec 3600` | **3.058 (3.055 + 3 omitidos), 0 fallos** |
| `unity-test.ps1 -Mode PlayMode … -TimeoutSec 3600` | **1.383 (1.216 + 167 omitidos), 0 fallos** (39 min) |
| Capturas de R11 (`DailyChallengeSnapshotTests`, sin `-nographics`) | 4/4; `AF_FIT` = 0 textos fuera de su caja en es-419, es-419 16:9, en-XA y en-XA 16:9; `AF_STAMP` = desvío 0,00 dp |

- **Recuentos de referencia del Spark para el integrador:**
  - sin los enganches: EditMode **3.058**, PlayMode **1.383**;
  - con los enganches aplicados (R12): EditMode 3.082 y PlayMode 1.394.
- **Bloques listos para integrar:**
  - (a) ya integrado;
  - (b) el pool;
  - (c) lógica;
  - (d) partida y pantallas;
  - (e) los enganches, con sus parches en `docs/dev/traspaso/spark-enganches/` y en `docs/dev/traspaso/spark-parches-docs.md`.
- **Fuera de alcance o pendiente:**
  - E1, E6 y E7 sin preparar (motivo en la entrada de R12);
  - E16 por regenerar;
  - E19, E20 y E25, para más adelante.
  - Las capturas son de prueba; falta la prueba en el teléfono, que necesita los enganches aplicados en la integración.

## 2026-10-19 · R12 · Enganches para el integrador: parches exactos y PROBADOS · Bloque (e) listo

**Probados contra:** `origin/integracion-fase1-fase2` en **`bc65242`**, con `spark/reto-del-rio` fusionada (R0-R11 y la parte del Spark de R12). Worktree temporal `r12-full`, no empujado, HEAD `c4ba4a1`.

### Dónde están y en qué orden se aplican

Carpeta **`docs/dev/traspaso/spark-enganches/`**: diffs unificados con LF fijado (`.gitattributes` propio).

1. Fusiona `spark/reto-del-rio`. Ya trae la parte del Spark de R12 (`ac54d82`): la tarjeta con el título en dos líneas y 100 dp, los `max_chars` del CSV, `DailyUiLogicTests` con la regla `VisibleLength`, y `DailyProbeFormat.EarnSeeds` con `source` (para E9) y `DailyChallengeService.Config`.
2. Aplica **`R12-all.diff`**: todos los enganches juntos, 26 archivos. `git apply --check` da OK sobre `ac54d82`.

   O bien, uno a uno y en este orden:
   - `E4.diff`;
   - `E2.diff`, `E22.diff` y `E23.diff` (los tres tocan `GameBootstrap.cs`);
   - `E3.diff` y `E3-scene.diff`;
   - `E5.diff` y `E5-fix.diff`;
   - `E9.diff`;
   - `E21.diff` y `E24.diff`;
   - `E-tests.diff`.
3. **Mejor que aplicar `E3-scene.diff`:** regenera `Game.unity` con `AF.UI.Editor.GameSceneBuilder.CreateGameScene` tras aplicar `E3.diff`. El diff de la escena solo añade `dailyPool` (el guid del `.meta` del pool) y renumera los fileID.
4. Después, que Unity genere los `.meta` de los archivos nuevos (`ProfileDailyChallengeStoreTests.cs`, `DailyDoubleHarvestTests.cs`, `DailyHooksTests.cs` y `ProfileDailyChallengeStore.cs`) y commitéalos tal cual.

### Qué hace cada uno

| # | Archivos | Qué |
|---|---|---|
| E2 | `GameBootstrap.cs` | `SetUpDaily();` tras `_lobby.Bind(_app, _services);` |
| E3 | `Editor/GameSceneBuilder.cs` (+ `Game.unity` regenerada) | `DailyPoolPath` y `ConfigureDailyPool(dailyPool)` tras `ConfigureRiverStops`; aviso si falta el pool |
| E4 | `Save/Profile.cs`, `Save/ProfileJson.cs`, nuevo `LiveOps/ProfileDailyChallengeStore.cs`, `GameBootstrap.Daily.cs` (fábrica por defecto) | Bloque `daily_challenge` (nota §10). Se omite mientras es null, así que el golden del perfil no cambia. `CurrentSchema` 1, sin migración (08 §4.3). Un tipo equivocado o un `in_progress` sin `date` o `id` es corrupción. El almacén usa el mismo `Profile` y el mismo lote (`ProfileSaveBatch.Durable`) que la cartera del Reto: nunca dos pagos. 17 tests |
| E5 | `Data/Loc/strings.csv`, `LocTablesTests` (+ tests del Reto) | Las 24 filas de `spark-strings-reto.csv` en orden ordinal (`data-json.md`); `LocTablesTests` añade `DailyText.NewKeys`; los tests del Reto leen ya `strings.csv` |
| E9 | `Economy/Transaction.cs`, `Wallet.cs`, `DailyChallengeScreen.cs`, `DailyStampCalendar.cs`, tests | `Transaction.DoubleHarvestDaily(seeds, level)` [P en la firma: lleva el nivel para el mínimo `ads_rv_double_harvest_from_level` = 11]. Solo con el premio confirmado, una vez por victoria, con el tope diario y el cooldown de `rv_double_harvest` de AF.Ads (compartidos con la campaña [P]) y como mucho seeds × `murky_bonus_mult` (120). Paga solo si el anuncio se vio entero. Sonda `earn_virtual_currency` `source=double_harvest` |
| E21 | `GameScreen.cs` | `if (isShort && !flow.IsDaily)`: el Reto no enseña «Nivel N · intento k» |
| E22 | `GameBootstrap.cs` (opcional) | Atrás cierra el calendario del Reto; con el tablero en juego no hace nada |
| E23 | `GameBootstrap.cs` | Al volver de segundo plano, la vista del Reto salta al modelo, limpia la selección y reinicia la pista por inactividad |
| E24 | `DevProbe.cs` | `AppendDailyBoard(sb, flow);` tras las 4 líneas `"level"`: el Reto añade `"daily":"d{b}_n{nnnn}"` |
| tests | nuevo `Tests/PlayMode/DailyHooksTests.cs` (+ ajustes en `DailyChallengeTests`) | E2, E3, E4, E5, E9, E21-E24 de punta a punta con la escena real: el pool real, el almacén del perfil y las tablas |

### Verificación (r12-full, con todo aplicado)

| Comando | Resultado |
|---|---|
| `dotnet build -warnaserror` | código 0 |
| `dotnet test` | 981 (975 + 6) |
| `validate_schema.py` | 103/103 |
| `unity-compile.ps1 -Architecture arm64` | OK |
| EditMode | **3.082** (3.078 + 3; 1 fallo, el inestable `IntroArtTests.FruitFlight…`) |
| PlayMode | **1.394 (1.227 + 167), 0 fallos**; `PseudoLocOverflowTests` en-XA 130 % pasa tras el arreglo de la tarjeta |
| Capturas de R11 | 31 PNG, `AF_FIT` sin ningún texto fuera de su caja (es-419 a 780 no salió por un error del entorno con la ruta larga; se repite en `spark-int`) |

- **code-reviewer:** primera ronda CAMBIOS NECESARIOS (faltaban la sonda `double_harvest` y el test del anuncio cerrado antes de tiempo, y PlayMode tenía un fallo real). Segunda ronda **APROBADO**.

### Lo que no se preparó

- **E1** (`LobbyScreen`, opcional): no hace falta, porque la tarjeta va en su propio lienzo desde `GameBootstrap.Daily.cs`. Requisito si se mueve: en 16:9, al menos 8 dp bajo el bocadillo de TiTino (`r11_01_card_play_16x9`).
- **E6** (tarjeta de 2 s tras ganar el 7): falta una clave de texto nueva y el diseño de la tarjeta. Punto de enganche: `GameScreen.OnVictoryScreen` con `flow.LevelNumber == 7`.
- **E7** (Sin canoas, opcional): necesita una clave nueva, `af_no_canoes_choice = daily_challenge` y un flujo nuevo en AppFlow.
- **E16** (level-qa enlaza `Daily/`): el parche probado en el bloque (a) quedó desfasado tras R5. Hay que regenerarlo sobre la versión actual (descripción en su ficha).

### Para el dueño (E11; no bloquean)

- El Reto comparte con la campaña el tope de 5 al día y el cooldown de la Cosecha doble [P].
- Cuando exista la Crecida: ¿la Cosecha doble sin video del «río crecido» (04 §2.8) vale también en el Reto? Va con E19 y E25.

## 2026-10-19 · R0-R11 juntas: verificación completa VERDE · Bloque (d) (R8-R11) listo para integrar

- **Qué hay:** la rama principal (R0-R10) más R11, la nota con la tabla de días de 729 corregida (`09b11cd`) y los usos `--days 0-728`.
- **Resultados** (worktree `spark-int`):
  - `dotnet build -warnaserror` OK y `dotnet test` 981;
  - `validate_schema.py` 103/103;
  - `unity-compile` OK;
  - EditMode **3.057** (3.053 + 3, 1 fallo: el de tiempo de `IntroArtTests.FruitFlight…`, inestable y ajeno).
- **PlayMode: sin veredicto (código 6, licencia).** Hacia las 19:15 vuelve el 404 de derechos (`com.unity.editor.headless`). Es la 4.ª caída, de nuevo unas 2 h después de la última revisión del dueño.
- **Por separado sí hay PlayMode en verde:** R10 sobre R9 (1.354) y R11 sobre la integración con R5 (1.373). Falta la combinación de las dos.
- **PlayMode, tras la revisión de la licencia por el dueño:** **1.383 (1.216 + 167 omitidos)**, 0 fallos (43 min). Los omitidos incluyen los `[Explicit]` de capturas.
- `spark/reto-del-rio` avanza hasta esta combinación y se empuja. También entran los parches de documentación E11/E12 aprobados (`docs/dev/traspaso/spark-parches-docs.md`, `571fdc2`) y los textos 0-728 (`7501c5e`), que solo tocan documentos y textos de ayuda.
- **Recuentos de referencia del Spark:** EditMode 3.057; PlayMode 1.383.

## 2026-10-19 · R11 · Capturas y prueba de punta a punta: HECHA (APROBADO en 2.ª ronda; arreglo del sello en `171b404`)

- **Commit:** `41d5beb` en `spark/reto-del-rio-r11` (sale de la integración `8327d23`), más el arreglo del sello en curso.
- **Archivos:**
  - nuevo `Assets/_Project/Tests/PlayMode/DailyChallengeSnapshotTests.cs`: `[Explicit]`, `SnapshotKit`, 360 × 780 dp y 16:9, es-419 y en-XA;
  - arreglos en `DailyChallengeCard.cs` (título de 192 dp y estado en dos líneas) y `DailyStampCalendar.cs` (el sello que cae, por encima de las casillas vecinas; la caja del +60, de 42 dp).
- **Cómo se lanzan:** sin `-nographics` (`unity-test.ps1` lo fuerza):
  ```
  unity test . --mode PlayMode --architecture arm64 --filter AF.Tests.PlayMode.DailyChallengeSnapshotTests --output Logs/r11-snap.xml --non-interactive -- -logFile Logs/r11-snap.log
  ```
  - Los PNG van a `Logs/snapshots/es-419/` y `Logs/snapshots/en-XA/` (ignorado por git).
  - Cada captura deja en el log una línea `AF_FIT` por texto visible. Hoy dicen «0 textos fuera de su caja» en es-419, es-419 16:9, en-XA y en-XA 16:9.
  - Los textos salen de `strings.csv` + `facts.csv` + `docs/dev/traspaso/spark-strings-reto.csv`, como hará E5.
- **Verificación:** capturas 4/4; EditMode 3.057 (3.053 + 3, 1 fallo del test inestable `IntroArtTests`); PlayMode **1.373** (1.206 + 167 omitidos: los 4 `[Explicit]` nuevos), 0 fallos.

### Capturas y qué revisar (40 PNG: 10 × 2 formatos × 2 idiomas; los de 16:9 llevan `_16x9`)

| Archivo | Qué muestra | Qué revisar |
|---|---|---|
| `r11_01_card_play` | Inicio con la tarjeta «Reto del río · 19 oct», Jugar, 1 ola, 0/7 | La fecha corta por región; que el título no toque el sello. **En 16:9 la tarjeta queda pegada al bocadillo de TiTino (0-3 px): requisito de E1** |
| `r11_02_header` | Partida del Reto | Cabecera «Reto del río», sin «Nivel N» ni barra de Crecida. Ajeno: en en-XA, «Reiniciar» se sale del botón (`GameScreen`) |
| `r11_03_card_continue` | Tarjeta «Seguir» tras salir sin ganar | R-Q16 (a) |
| `r11_04_calendar_falling` | Octubre con sellos previos y el del 19 a media caída | Que el sello caiga **centrado en su casilla** (arreglado en `171b404`; `AF_STAMP`: desvío 0,00 dp); el contador del HUD ya marca 60 antes del «+60» |
| `r11_05_calendar_reward` | El sello posado y «+60» con la semilla | El «+60» queda algo a la izquierda del centro [P] |
| `r11_06_card_sealed` | Tarjeta «Sellado», 1/7 | El sello verde de los CTA con el aro crema (sin rojo; guía v2 §7) |
| `r11_07_calendar_month` | Calendario abierto desde la tarjeta sellada | Sin premio ni sello que cae; el hueco del «+60» queda reservado |
| `r11_08_calendar_pending` | Victoria con el servidor +48 h | «Premio pendiente de validar», sin +60, sello translúcido |
| `r11_09_card_pending` | Tarjeta «Pendiente de validar» | En en-XA, en dos líneas sin pisar la racha; ¿se lee bien el sello pendiente (disco claro)? |
| `r11_10_calendar_pororoca_locked` | Racha de 7 (del 12 al 19), +60 y «+1 Pororoca · se desbloquea en el nivel 30» | R-Q14 (a); en 16:9 el panel tapa la parte de arriba de la bandeja de comodines con el velo |

- **Además:** no hay cabecera de días de la semana en el calendario ([P] de R9); en el inicio de estas capturas no sale la pastilla de Canoas, por el perfil de prueba (`Wallet.NewProfile`), no por el Reto.
- **E1 (requisito nuevo):** en 16:9, dejar al menos 8 dp bajo el bocadillo de TiTino, o subir o compactar la tarjeta. Se comprueba con `r11_01_card_play_16x9`.

## 2026-10-19 · R10 · Sondas de desarrollo y analítica: HECHA

- **Commits:** `a99fe5d`, `b1c0a9b` (fusión de R9), `83e43dc` (los `.meta` que generó Unity) y `4025c1a` (arreglos de la revisión), en `spark/reto-del-rio-r10`; fusionados con `--no-ff` en `spark/reto-del-rio` (`8ab5346`).
- **Archivos:**
  - nuevo `Scripts/UI/DevProbe.Daily.cs`, un parcial de `DevProbe` que escribe por `DevProbe.Write` solo con `DevProbe.Enabled`;
  - cambios en `GameBootstrap.Daily.cs`, `DailyChallengeScreen.cs` y `DailyChallengeCard.cs` (archivos del Spark);
  - nuevo `Tests/PlayMode/DailyProbeTests.cs` (10 tests).
- **Sondas:**
  - `daily_challenge`: `win` al reclamar (nunca con `NoReward`); `fail` desde `DayEnded`, una vez por día;
  - `level_start`: al terminar Llenar y teñir, o al reanudar sin intro; una vez por partida ([P]: Volver a la orilla no lo repite);
  - `earn_virtual_currency`: al cobrar, y al abrir la app si se confirman cobros pendientes;
  - `daily_card`: en cada recálculo de la tarjeta.
  - `t_ms` es lo jugado desde que se abrió, y null si se reanudó [P]. La Pororoca no tiene evento (08 §8.2 no lo define).
- **Verificación** (worktree `spark-r10`): compila; EditMode 3.040 (3.036 + 3, 1 fallo del test inestable); **PlayMode 1.354 (1.195 + 159), 0 fallos**; el revisor, `DailyProbeTests` 10/10.
- **code-reviewer:** primera ronda CAMBIOS NECESARIOS (`level_start` salía antes de la intro, contra 08 §8.2, y faltaban tests); segunda ronda **APROBADO**.
- **E10:** no hace falta (todo sale de archivos del Spark). Queda E24 (`DevProbe.cs`, `AppendDailyBoard`).
- **Texto propuesto para `docs/dev/prototipo-v0.md` (E12):**
  > **Sondas del Reto del río** (R10; `DevProbe.Daily.cs`, formato en `AF.LiveOps/DailyProbeFormat.cs`). Solo en builds de desarrollo, como `AF_STATE` de una línea, con claves e identificadores:
  > - `{"event":"daily_card","t":…,"state":"hidden|in_progress|pending_validation|sealed|play","date":"AAAA-MM-DD","band":0-3,"day":…,"board":"d{b}_n{nnnn}"|null,"streak":n,"override":id|null,"override_ignored":bool}`: cada vez que se recalcula la tarjeta del inicio. Con la tarjeta oculta, `band` 0 y `board` null.
  > - `{"event":"level_start","t":…,"level_name":"d{b}_n{nnnn}","variant":"base","band":b}`: al terminar Llenar y teñir, o al reanudar sin intro; una vez por partida. Las líneas de partida (`load`, `ready`, `pour`…) llevan en `level` el nivel efectivo del Reto; con E24, además, `"daily":"d{b}_n{nnnn}"`.
  > - `{"event":"daily_challenge","t":…,"date":…,"result":"win|fail","t_ms":n|null,"murky_bonus":false,"streak_days":n}`: `win` al reclamar el premio; `fail` una vez por día, al descartar la partida de un día no ganado (R-Q15).
  > - `{"event":"earn_virtual_currency","t":…,"virtual_currency_name":"seeds","source":"daily_challenge","value":n}`: las semillas del premio, pagadas ahora.
  >
  > Para seguirlo: `adb logcat -s Unity | grep -E '"event":"(daily_card|level_start|daily_challenge|earn_virtual_currency)"'`.

## 2026-10-19 · R5, decisiones (R-Q12b, R-Q15, R-Q16), formato de sondas y R9 fusionados: verificación completa VERDE

- **Qué entra** en `spark/reto-del-rio`, por avance rápido desde `spark/reto-del-rio-int`:
  - **R5** (`71593ae`, `fbbb935`, `c266a46`, `c903537`, `d1fbe7e`, `0f436ef`): el pool publicado, 729 días × 3 bandas = 2.187 tableros, 1.430.135 bytes. code-reviewer: **APROBADO** (dos rondas).
  - **Rama de decisiones** (`42555cf`, `8115b74`, `f816d1c`, `e46348c`, `d0a9019`): R-Q16, R-Q15, R-Q12b y `DailyProbeFormat`. code-reviewer: **APROBADO** (dos rondas).
  - **R9** (`373c6e2`, `b0ada3a`, `0e7c276`, `fbb9893`, `896d894`, `b9a9e8c`, `d107d7e`): las pantallas. Los dos commits «Enganche:» son los asmdef de §3.3. code-reviewer: **APROBADO** (dos rondas).
  - La nota, al día (`85fe0a3`), y los enganches E21-E25 (`8327d23`).
- **Verificación** (worktree `spark-int`, `8327d23`):

  | Comando | Resultado |
  |---|---|
  | `dotnet build dotnet/AmazonFill.slnx -warnaserror` | código 0 |
  | `dotnet test dotnet/AmazonFill.slnx` | 981 (975 + 6) |
  | `python tools/validate_schema.py` | 103/103 |
  | `level-generator --self-test` (todas) | todo PASS |
  | `level-qa --daily Assets/_Project/Data/Daily/daily_pool_v1.json` | 2.187 tableros, **0 fallos**, 1.678 avisos (R-Q10) |
  | `unity-compile.ps1 -Architecture arm64` | OK |
  | `unity-test.ps1 -Mode EditMode … -TimeoutSec 3600` | **3.057 (3.054 + 3 omitidos)**, 0 fallos |
  | `unity-test.ps1 -Mode PlayMode … -TimeoutSec 3600` | **1.369 (1.206 + 163 omitidos)**, 0 fallos (41 min) |

- **Recuentos de referencia del Spark desde aquí:** EditMode 3.057; PlayMode 1.369. Los del integrador tras el bloque (a) eran 2.861 y 1.359.
- **Bloque (d)** (R8-R11, partida y pantallas): R8 y R9 están dentro; faltan R10 (sondas, en arreglos de revisión) y R11 (capturas, en curso).

## 2026-10-19 · ⚠ La licencia de Unity falla por 3.ª vez (pendiente del dueño)

- **Hacia las 16:45** los batchmode nuevos vuelven a salir con el código 6: `Found 0 entitlement groups`, `'com.unity.editor.headless' was not found`, `No valid Unity Editor license found`. Los que ya estaban en marcha (el PlayMode de `spark/reto-del-rio` y el de `spark-r9`) siguen corriendo.
- **Patrón:** falla entre 1 y 2 h después de cada revisión del dueño (12:10, 13:35 y 16:45). Esta vez no se reinició el proceso `--internal-auth-broker-serve` (el último es de las 14:38), así que la hipótesis de la 2.ª caída no se confirma. Lo más probable es que caduque el token de la sesión y no se renueve para el modo headless. Habría que mirar en Unity Hub si la licencia es una suscripción por sesión o una licencia Personal sin activación offline.
- **Qué queda frenado:** la verificación en Unity de la combinación de R5 con la rama de decisiones (worktree `spark-int`, `85fe0a3`).
  - La parte .NET y de herramientas ya está en verde: build sin avisos, `dotnet test` 981, `validate_schema` 103/103, `--self-test` del generador todo PASS, y `level-qa --daily` del pool de 2.187 tableros con 0 fallos y 1.678 avisos (R-Q10).
  - Las tres piezas pasaron Unity por separado: R5 (EditMode 2.995, 1 fallo del test inestable que pasa al relanzarlo), la rama de decisiones (3.023, ídem) y la rama principal (EditMode 3.006 en verde).
- **No he usado `unity auth` ni `unity license`** (regla dura 5).

## 2026-10-19 · Decisión del dueño: R-Q11b, 30 candidatos por fuente

- **La pregunta:** para que los 729 días de cada banda entren en la ventana del bot, R5 prueba hasta 30 candidatos **por fuente** y, si ninguno entra, pasa a la siguiente.
  - B1: `fill` E=2 → `deep` → `walk` → `deep` de 60.000 nodos, hasta 120 candidatos por día. Solo 23 días necesitan respaldo.
  - B2: `deep` → `fill` E=1, al revés que la tabla de 03 §4.4 para K ≤ 8. Con `fill` primero, 96 días salían por encima de la ventana.
- **El dueño respondió «Aceptar (Recomendado)»:** el «máximo 30 candidatos» de 03 §4.4 se lee como 30 por fuente, con respaldos en orden. No se relajan vacíos, K, capacidad ni ventanas.
- **Para el integrador (E11):** anotarlo en 03 §4.4 o §16 y en `decisiones.md`.
- **Nota sobre R-Q2b:** con `live_daily_offset` ≠ 0 (mod 9) el mundo sigue rotando a diario con la fecha, desplazado por el offset. Propuesta [P]: documentarlo en 08 §9, en la fila de `live_daily_offset` (E11).

## 2026-10-19 · Decisión del dueño: R-Q2b, pool de 729 días

- **El problema:** el agua y el fondo rotan «según la fecha» (R-Q2), pero con un pool de 730 días y 9 mundos (730 mod 9 = 1), un mundo grabado en cada tablero deja de seguir a la fecha a partir de la segunda vuelta.
- **Opciones presentadas:** el cliente calcula el mundo con la fecha local (cambia el significado de `water_base`, contrato 21); aceptar el salto; o un pool múltiplo de 9.
- **El dueño eligió** «Pool de 729 o 738 días» y, después, **«729 días (Recomendado)»**:
  - 81 × 9 = **729 días por banda, 2.187 tableros**;
  - el día del pool va de 0 a 728 y cabe en el esquema actual, sin tocar el contrato 21;
  - el índice pasa a `(días_desde(2026-01-01) + live_daily_offset) mod 729`;
  - el mundo del tablero es `1 + (día mod 9)`, que sigue a la fecha en todas las vueltas (con `live_daily_offset` constante, desplazado por ese offset).
- **Para el integrador (E11):** cambia 03 §16 y 06 §4.1, que dicen 730 y 2.190, y el pool de 08 §4.1 y §6.2.

## Resumen (2026-10-19)

- **Rama:** `spark/reto-del-rio`, en `752bb55`. Ya lleva fusionado `origin/integracion-fase1-fase2` (`6377345`).
- **Tareas hechas** (commits de la tarea · fusión en `spark/reto-del-rio`):

  | Tarea | Commits | Fusión | Revisión |
  |---|---|---|---|
  | R0 · Arranque y línea base | — (base `2db3a07`) | — | — |
  | R1 · Nota de diseño | `3e0063e`, `4968d1a` | (en la rama) | APROBADO (2.ª ronda) |
  | R2 · Especificación y autoprueba | `da6a9bb`, `99d35f7` | `3ca28dc` | APROBADO |
  | R3 · `--daily` del generador | `e8783b9`, `bb1dad2`, `ee48f52` | `c87a181` | APROBADO |
  | R4 · `level-qa --daily` | `32b8cc4`, `cafe8c4`, `ed109a3`, `8afdd0e` | `ba57160` | APROBADO (2.ª ronda) |
  | R6 · Lector del pool | `a724b43`, `28be4a2` | `afb58a2` | APROBADO (tras Unity) |
  | R7a · Calendario | `065d0e6` | `24d3f18` | APROBADO |
  | R7b · Premio, anti-reloj y racha | `a83ec1c`, `880f7dc` | `f0fb4c8` | APROBADO |
  | R8 · Partida del Reto en `LevelFlow` | `49501b4`, `535b92c` | `c0149b6` | APROBADO (Unity en verde) |
  | Enganche E15 + parte de E17 | `752bb55` | (en la rama) | — |

- **Bloques:**
  - **(a)** R0-R4 y R6: **integrado** en `integracion-fase1-fase2` (`fcbbd94`, `0d844cf`, `bc65242`) y traído de vuelta (`6377345`).
  - **(c)** R6-R7b (lector y lógica, sin pantalla): **listo para integrar**, verificado en `0a1b3ac`.
  - **R8:** fusionada en `spark/reto-del-rio` y verde en Unity (`535b92c`); sin integrar todavía.
- **Preguntas para el dueño:** **ninguna abierta.** R-Q1-R-Q10, R-Q1b y R-Q11-R-Q16 están decididas (entrada «Decisiones del dueño»). El integrador las lleva a `docs/dev/decisiones.md` (E11).
- **Qué falta:**
  - **R5** (en curso): R-Q1b en `daily_v1.json` (B2 `k: [8, 8]` sin `deep`, B3 sin tope), R-Q11 como error en el generador, R-Q2 con la fecha y no con el día del pool; rehacer la muestra y su informe; generar y publicar el pool (encargo §9, puntos 1-3).
  - **Aplicar en el código** las decisiones nuevas: R-Q12b en `DailyChallengeRules.Reconcile`; R-Q16 en `LevelFlow.ExitDaily` (hoy borra la partida); R-Q15 en la sonda (R10).
  - El pendiente menor de R7b (entrada de R7b).
  - R9 (pantallas), R10 (sondas), R11 (capturas y punta a punta) y R12 (enganche final: parches de E1-E7, E9, E10 y E12).
  - Enganches pendientes del integrador: E11, E13 (opcional), E14 (opcional), E16, E17 (README), E18 (punto 2), E19 (cuando exista la Crecida), E20.
- **Riesgo del entorno:** la licencia de Unity ha fallado dos veces (12:10 y 13:35, las dos con un `--internal-auth-broker-serve` nuevo de la CLI). Las dos se resolvieron; si vuelve, se avisa al dueño y no se fusiona nada sin pasar Unity.

---

## 2026-10-19 · Enganche E15 y parte de E17: HECHO (`752bb55`)

- **Commit:** `752bb55` «Enganche: incluye daily en el --self-test sin nombre del generador y documenta --daily en las dos herramientas», en `spark/reto-del-rio`. Punto 6 de las notas del integrador (encargo §9).
- **Archivos:** `tools/level-generator/Program.cs`, `tools/level-generator/README.md` y `tools/level-qa/Program.cs`.
  - E15 completo: el `--self-test` sin nombre del generador ejecuta también `daily`; `|daily` en `Usage`, «, holgura o daily» en el mensaje de error; `--daily` en el README y en la ayuda.
  - E17, la parte de `tools/level-qa/Program.cs`: `--daily` y `--self-test [key2|reglas|variante|daily]` en `Usage`, y el mensaje. **Queda el README de level-qa**, ligado a E16 (su texto describe QA-DAILY-REPRO con el proceso hijo de hoy).
  - Parches probados contra `origin/integracion-fase1-fase2` (`bc65242`).
- **Verificación:**
  - `dotnet run --project tools/level-generator -c Release -- --self-test` → **435 PASS, 0 FAIL**, exit 0 (incluye «daily»);
  - builds de `tools/level-generator` y `tools/level-qa` → sin avisos;
  - `dotnet run --project tools/level-qa -- --self-test bogus` → el mensaje de uso nombra «daily».

## 2026-10-19 · El integrador fusionó el bloque (a); fusión de vuelta en `spark/reto-del-rio` (`6377345`)

- **Integración** (encargo §9, «Bloque (a) integrado»): R0-R4 y R6 están en `integracion-fase1-fase2`:
  - `fcbbd94`: integra el bloque (a) (nota de diseño, especificación y generador del pool, QA del pool y lector en AF.Levels);
  - `0d844cf`: pone al día los recuentos;
  - `bc65242`: anota en el encargo la integración y lo que queda por ajustar.
- Revisado sin bloqueantes y verificado en el escritorio x64; la muestra de 30 tableros regenerada allí sale idéntica byte a byte.
- **Recuentos de referencia del integrador:** EditMode **2.861 (2.858 + 3)**, PlayMode **1.359 (1.196 + 163)**, `dotnet test` **981 (975 + 6)**.
- **Fusión:** `6377345` «Fusiona origin/integracion-fase1-fase2 en spark/reto-del-rio (bloque (a) integrado)», como pide el encargo §9.
- **Lo que pidió el integrador** (encargo §9, «Pendiente para ti»): puntos 1-3 → R5 (resumen); punto 4 (quitar «abierta» y [P] de lo decidido) y punto 5 (ordenar este archivo) → esta revisión de la nota y del estado; punto 6 → `752bb55`. También corrigió el punto 1 de §3.3 (E18).

## 2026-10-19 · Decisiones del dueño sobre las preguntas nuevas (R-Q1b, R-Q11-R-Q16)

El dueño respondió, literal: «aplica las recomendaciones y pon trabajo en paralelo para avanzar mas rapido». El integrador, en el encargo §5: «si tienen recomendada, vamos con esas». Se aplica la opción recomendada (la primera) de cada pregunta. El integrador debe llevarlas a `docs/dev/decisiones.md` junto con R-Q1-R-Q10 (E11). El integrador confirmó además que `IServerDate` y `FakeServerDate` van en **AF.Platform** (encargo §5).

| Id | Decisión |
|---|---|
| R-Q1b | (a) **B2 solo con K = 8** (cambia 03 §16 y 06 §4.1, que dan K 8-9) y **B3 con `max_pares_riesgo` libre**, como la campaña desde el 201 (03 §4.5). B1 sin cambios |
| R-Q11 | (a) La ventana del bot **es compuerta**: un día fuera de ventana no se publica hasta ajustar la especificación (fuente o K, que son [P]); QA-DAILY-BOT sigue siendo fallo |
| R-Q12 | (a) Al reconectar no se retira nada de lo cobrado sin red (`KeepOfflineClaims`, el default actual) |
| R-Q12 (aclaración) | (a) Al anular una fecha no se retiran semillas ni la Pororoca |
| R-Q12b | (a) Un pendiente solo se valida si `\|fecha − hoy_servidor\| ≤ 1 día`; los demás se anulan |
| R-Q13 | (a) Sin intersticial al terminar un Reto |
| R-Q14 | (a) La Pororoca de la racha entra en el stock y espera al nivel 30, en que se desbloquea |
| R-Q15 | (a) `af_daily_challenge.result = fail` **una vez por día**, y solo si ese día termina sin victoria |
| R-Q16 | (a) Salir de la partida del Reto sin ganar la deja **reanudable el mismo día**; al cambiar de día se descarta (R-Q4) |

**Estado en el código:** R-Q12, su aclaración, R-Q13 y R-Q14 (el pago) ya los cumple. Pendientes: R-Q1b y R-Q11 en el generador (R5); R-Q12b en `DailyChallengeRules.Reconcile`; R-Q16 en `LevelFlow.ExitDaily`; R-Q15 en la sonda (R10); el aviso de R-Q14 en la UI (R9). Detalle en la nota §1.6.

## 2026-10-19 · R8 · La partida del Reto en AF.GameFlow: HECHA (Unity en verde tras la revisión de la licencia)

- **Commits:** `49501b4` y `535b92c` (arreglos de la revisión), en `spark/reto-del-rio-r8` (sale de R7b). Empujada y, tras pasar Unity, **fusionada** en `spark/reto-del-rio` (`c0149b6`; la verificación se anotó en `9122e71`).
- **Archivos nuevos:** `Assets/_Project/Scripts/GameFlow/LevelFlow.Daily.cs` y `Assets/_Project/Tests/EditMode/DailyLevelFlowTests.cs` (18 tests), con sus `.meta`.
- **Cómo funciona (sin tocar archivos existentes):** `LevelFlow.CreateDaily(...)` monta un flujo propio. **E8 ya no hace falta como estaba previsto.**
  - Catálogo de un solo tablero, numerado con el nivel efectivo `max_level + 1` [P], así que los desbloqueos de `BoosterConfig` funcionan sin cambiar nada.
  - Progreso de solo lectura: ni `SaveWin` ni `SaveNextLevel`, y sin regalos de tutorial.
  - Cartera del Reto **obligatoria**, sobre el mismo `Profile`, con una copia de `EconomyConfig` (`eco_reward_*` = 0; `rescue_min_level` = ∞): sin canoas, sin rescate, sin fallo duro con coste y sin Cosecha.
  - Copia de `BoosterConfig` con la Marea no ilimitada y sin Cuia de rescate.
  - Sin `AdGate`, sin intersticial (R-Q13, decidida (a)) y `StateStore` propio; rechaza el de campaña.
  - Un oyente intermedio entrega el evento `DailyResult` (Win/Exit/Restart) sin depender de AF.LiveOps.
  - Los tests de reflexión fallan si `EconomyConfig` o `BoosterConfig` ganan una clave que no se copia.
- **Verificación:**
  - autor, antes de que fallara la licencia: compila; EditMode 2.991 (2.987 + 3 omitidos y 1 fallo del test inestable `IntroArtTests`, que pasa al relanzarlo); **PlayMode 1.334 (1.175 + 159), 0 fallos**;
  - tras los arreglos (`535b92c`): `dotnet build -warnaserror` sin avisos y un arnés .NET fuera del repositorio con 18/18;
  - **sobre `535b92c`, tras volver la licencia:** `unity-compile` OK; EditMode **2.996**: 2.992 + 3 omitidos y 1 fallo, el de tiempo de `IntroArtTests.FruitFlight…`, que al relanzarlo con `-Filter AF.Tests.EditMode.IntroArtTests` pasa 9/9; **PlayMode 1.334 (1.175 + 159), 0 fallos**. Fusionada en `spark/reto-del-rio`.
- **code-reviewer:** primera ronda CAMBIOS NECESARIOS. Los tres bloqueantes:
  - sin cartera, el Reto caía en el embudo de D-008;
  - el comentario sobre la Crecida era falso;
  - el JSON interno no cumple af-level/1 y no lo decía.

  Segunda ronda: **APROBADO a falta de Unity** (y Unity, después, en verde).
- **[P]:** nivel efectivo `max_level + 1`, congelado al empezar; sin regalos de tutorial; Marea con las 3 gratis y luego el stock; modal de Aguas estancadas con Marea + Volver a la orilla; `DailyAttempt` vuelve a 1 al reanudar.
- **Ya no son [P]:** sin intersticial (R-Q13 (a)). Salir **borra** hoy la partida guardada del Reto, pero R-Q16 (a) pide dejarla **reanudable el mismo día**: pendiente en `ExitDaily` (`StateStore?.Clear()`).
- **Enganches nuevos:** E19 (guarda de la Crecida en `FailResolve`) y E20 (`WithLevel` y `LevelCatalog.ForBoard`), con sus parches en «Enganches para el integrador».
- **Notas para R9 y R10:**
  - `LevelNumber` es el nivel efectivo: no se enseña como «Nivel N» ni dispara tutoriales ni sondas de campaña; se usa `DailyBoardId`.
  - El HUD del Reto se suscribe a la cartera del Reto, y el saldo de la campaña hay que refrescarlo al volver.
  - La UI llama a `ExitDaily`, no a `AppFlow.ExitToLobby`.
  - R-Q15: `DailyPlayOutcome.Exit` llega en cada salida; la sonda emite `fail` una sola vez por día, y solo si el día termina sin victoria.

## 2026-10-19 · ⚠ La licencia de Unity vuelve a fallar (2.ª vez; resuelto: R8 pasó Unity después)

- **Hacia las 13:35** los batchmode vuelven a salir con el código 6, con los mismos errores que la primera vez:
  - `Code 404 … Found 0 entitlement groups and 0 free entitlements`;
  - `'com.unity.editor.headless' was not found`;
  - `No valid Unity Editor license found`.
- **Patrón:** las dos veces, justo antes del fallo, arrancó un proceso nuevo de la Unity CLI, `unity.exe --internal-auth-broker-serve` (PID nuevo a las 12:09 y a las 13:35). Parece que, al renovar la sesión, la CLI se queda sin derechos para el modo headless. La pasada larga que acababa de terminar (bloque c, PlayMode 1.334) salió bien porque arrancó antes.
- **Qué afectó:** la verificación en Unity de R8, que se hizo al volver la licencia (entrada de R8).
- **No he usado `unity auth` ni `unity license`** (regla dura 5).

## 2026-10-19 · Bloque (c) (R6-R7b: lector y lógica del Reto, sin pantalla): listo para integrar

Verificación completa de `spark/reto-del-rio` en `0a1b3ac`:

| Comando | Resultado |
|---|---|
| `dotnet build dotnet/AmazonFill.slnx -warnaserror` | código 0 |
| `dotnet test dotnet/AmazonFill.slnx` | 981 (975 + 6), código 0 |
| `python tools/validate_schema.py` | 103/103 |
| `unity-compile.ps1 -Architecture arm64` | OK |
| `unity-test.ps1 -Mode EditMode … -TimeoutSec 3600` | **2.978 (2.975 + 3 omitidos)**, 0 fallos |
| `unity-test.ps1 -Mode PlayMode … -TimeoutSec 3600` | **1.334 (1.175 + 159 omitidos)**, 0 fallos |

En total, 46 min. El bloque solo añade archivos nuevos: AF.LiveOps y sus tests, `DailyPool*` en AF.Levels y `IServerDate`, `FakeServerDate` y `ClockSecurityConfig` en AF.Platform. Ningún asmdef existente cambia todavía; `AF.UI.asmdef` += `AF.LiveOps` llegará con R9.

## 2026-10-19 · R7b · Premio, anti-reloj y racha: HECHA

- **Commits** (en `spark/reto-del-rio-r7b`, fusionados con `--no-ff` en `spark/reto-del-rio` como `f0fb4c8`):
  - `a83ec1c`: sugerencias de la revisión de R7a;
  - `880f7dc`: R7b.
- **Archivos nuevos:**
  - `Scripts/Platform/IServerDate.cs`, `FakeServerDate.cs` y `ClockSecurityConfig.cs`, donde ahora vive `sec_clock_max_drift_h`. Así es la única fuente y AF.Economy (Canoas) también puede leerla;
  - `Scripts/LiveOps/IDailyChallengeStore.cs`, `DailyChallengeRecord.cs`, `InMemoryDailyChallengeStore.cs`, `DailyChallengeRules.cs` y `DailyChallengeService.cs`;
  - tests (`DailyFixture`, `DailyChallengeRulesTests` 30, `DailyChallengeServiceTests` 18, `DailyChallengeStoreTests` 10).
- **Cambios en R7a:** `DailyOverrides.TryParse` (un valor no válido se queda en `{}`); `DailySlot.RequestedOverrideId` e `IsOverrideIgnored`, para que la sonda anote una sustitución ignorada.
- **Verificación** (worktree `spark-r7b`):
  - `unity-compile.ps1` → OK;
  - EditMode → **2.978 (2.975 + 3 omitidos)**, 0 fallos (70 tests nuevos);
  - `dotnet build -warnaserror` → 0 avisos.
  - El revisor lo repitió: 2.974 + 3 y 1 fallo, el de tiempo de `IntroArtTests.FruitFlight…` (inestable y ajeno); todos los de LiveOps en verde. Además, `dotnet test` 981 y `validate_schema.py` 103/103.
- **code-reviewer:** **APROBADO** a la primera. Comprobó contra 06 §4.1 y 08 §10.1:
  - nunca dos premios por fecha ni premio con el reloj atrasado;
  - «pendiente» solo con red y desfase de más de 24 h;
  - el orden registrar → guardar → pagar;
  - la racha y la Pororoca en los días 7 y 14;
  - los sellos del mes;
  - R-Q3, R-Q4 y R-Q9;
  - R-Q12 detrás de `DailyReconcileMode`, con las dos ramas probadas;
  - los estados de la tarjeta en el orden de la nota §8.
- **[P] de R7b:**
  - `pending` y `offline_claims` son listas de `{date, streak}`: la nota §5 y §10 ya está al día;
  - anular en la reconciliación deja la racha a 0 y quita el sello;
  - salir de un día ya ganado no emite `fail`;
  - `LevelStateSnapshot.Copy` va duplicada en AF.LiveOps, con un test por reflexión.
- **Lo que era [P] y ya está decidido:**
  - R-Q12 por defecto en (a), `KeepOfflineClaims`: **decidida (a)**;
  - anular no retira semillas: **decidida** (aclaración de R-Q12, (a): ni semillas ni la Pororoca);
  - salir sin ganar cierra la partida, que no queda reanudable: **R-Q16 decidió lo contrario** ((a), reanudable el mismo día): pendiente en el código;
  - la validación de pendientes con fecha ≤ hoy del servidor: **R-Q12b (a)** pide `|fecha − hoy_servidor| ≤ 1 día`: pendiente en `Reconcile`.
- **Pendiente menor** (va en un commit posterior):
  - documentar en `IDailyChallengeStore` que el registro y la cartera **tienen que vivir en el mismo `Profile`**, porque si no la garantía de «nunca dos pagos» se pierde (requisito de E4);
  - acotar `offline_claims`;
  - que `Reconcile` guarde solo si cambia algo;
  - la cita del test en `DailyChallengeRecord.cs:66`;
  - para R9: tras anular un pendiente con el reloj adelantado, la tarjeta sale «Sellada».

### Preguntas nuevas para el dueño (de la revisión de R7b): TODAS DECIDIDAS

| Id | Pregunta | Opciones (recomendada primero) | Qué bloqueaba | Estado |
|---|---|---|---|---|
| **R-Q12b** | Al validar un premio pendiente, ¿qué fechas se aceptan? Hoy se paga todo pendiente con fecha ≤ la del servidor, así que con red y el reloj atrasado a días no jugados se acaban pagando esos días. 06 §4.1 compara «> 1 día» | (a) solo si `\|fecha − hoy_servidor\| ≤ 1 día`; las demás se anulan; (b) toda fecha ≤ hoy del servidor (lo de ahora) | La rama de validación de R7b (es una línea) | **DECIDIDA: (a)** |
| **R-Q15** | `af_daily_challenge.result = fail`: ¿una vez por cada salida sin ganar, una por día, o solo si el día termina sin victoria? (R-Q9) | (a) uno por día, solo si ese día no se gana; (b) uno por salida (lo de ahora) | La sonda de R10 | **DECIDIDA: (a)** |
| **R-Q16** | Salir de la partida del Reto sin ganar: ¿la cierra (lo de ahora: solo se reanuda si se cierra la app; R-Q4) o la deja reanudable el mismo día? | (a) reanudable el mismo día; (b) se cierra | R8/R9 | **DECIDIDA: (a)** |
| **R-Q12** (aclaración) | En la rama (b) de R-Q12, ¿se retiran las semillas de una fecha anulada? El código no retira ni semillas ni la Pororoca | (a) no se retira nada; (b) se retiran las que queden sin gastar | Solo la rama (b) | **DECIDIDA: (a)** |

## 2026-10-19 · R7a · Calendario del Reto en AF.LiveOps: HECHA

- **Commit:** `065d0e6`, en `spark/reto-del-rio-r7a`; fusionado con `--no-ff` en `spark/reto-del-rio` (`24d3f18`).
- **Archivos nuevos** en `Assets/_Project/Scripts/LiveOps/`:
  - `AF.LiveOps.asmdef` (sin motor; `autoReferenced: false`; referencia AF.Core, Levels, Platform, Save y Economy);
  - `DailyChallengeConfig.cs` (las 9 claves de 08 §9 con sus defaults);
  - `DailyOverrides.cs` (`{fecha: {banda: id}}`, R-Q6);
  - `DailyCalendar.cs` (día del pool con módulo positivo, banda, desbloqueo, sustituciones y `DailySlot`);
  - `Tests/` (asmdef y 57 tests, entre ellos uno que lee la tabla de 08 §9 del Markdown y compara nombres, tipos y defaults).
- **Verificación** (worktree `spark-r7a`):
  - compila;
  - EditMode → 2.908 (2.905 + 3 omitidos), 0 fallos, en la pasada del revisor; en la del autor, 1 fallo del test inestable `IntroArtTests`, que pasa al relanzarlo;
  - `dotnet test` 981 y `validate_schema.py` 103/103.
- **code-reviewer:** **APROBADO** a la primera, sin bloqueantes. Las sugerencias (`TryParse`, sustitución ignorada visible, `sec_clock_max_drift_h` en AF.Platform) se aplicaron en R7b.
- **Para el integrador:** 08 §9 sigue diciendo `daily_overrides` = `json {fecha: level_id}`; R-Q6 lo cambia a `{fecha: {banda: id}}` (E11). El test solo compara el tipo base `json` y el default `{}`.

## 2026-10-19 · Verificación completa de `spark/reto-del-rio` en `ba57160` (R0-R4 y R6): VERDE

| Comando | Resultado |
|---|---|
| `dotnet build dotnet/AmazonFill.slnx -warnaserror` | código 0, 0 avisos |
| `dotnet test dotnet/AmazonFill.slnx` | 981 (975 + 6 omitidos), código 0 |
| `python tools/validate_schema.py` | 103/103 PASS |
| `dotnet run --project tools/level-generator -- --self-test daily` | todo PASS |
| `dotnet run --project tools/level-qa -- --self-test daily` | todo PASS |
| `scripts/unity-compile.ps1 -Architecture arm64` | OK |
| `scripts/unity-test.ps1 -Mode EditMode -Architecture arm64 -TimeoutSec 3600` | **2.851 (2.848 + 3 omitidos)**, 0 fallos: la línea base de 2.744 más 107 de R6 |
| `scripts/unity-test.ps1 -Mode PlayMode -Architecture arm64 -TimeoutSec 3600` | **1.334 (1.175 + 159 omitidos)**, 0 fallos, igual que la línea base (32 min) |

En total, 45 min. Durante parte del tiempo hubo otro batchmode en un worktree: como mucho 2-3 a la vez, en carpetas distintas, con 64 GB de RAM (el encargo §1 permite 3 con 32 GB o más).

## 2026-10-19 · R6 · Lector del pool en AF.Levels: HECHA

- **Commits:** `a724b43` (el lector y 66 tests) y `28be4a2` (los arreglos de la revisión y 41 tests más), en `spark/reto-del-rio-r6`; fusionados con `--no-ff` en `spark/reto-del-rio` (`afb58a2`).
- **Archivos nuevos:**
  - `Assets/_Project/Scripts/Levels/DailyPool.cs` y `DailyPoolReader.cs`;
  - `Assets/_Project/Tests/EditMode/DailyPoolTestPacks.cs` y `DailyPoolReaderTests.cs` (107 tests);
  - sus `.meta`, generados por Unity.
- **Qué hace:**
  - `DailyPoolReader.Read(json)` es estricto: pide las 3 bandas con 730 días cada una. `Read(json, DailyPoolCoverage.PresentBands)` existe solo para tests y herramientas.
  - Rechaza con `LevelFormatException`: `kind` distinto de `daily`; `pack_schema` fuera de 1-3; `slot`, `id` o `seed` incoherentes; `level`; `solution`, `safe_path` o `qa`; `relief` que no está vacío; `world` fuera de 1-13; bandas incompletas; repetidos.
  - [P] Rechaza también mecánicas, `murky`, `hidden` e `intro_mechanic` (nota §13).
  - `Find(band, day)`, `FindById`, `BandOf` y `DayOf` no asignan memoria (comprobado con `GC.GetAllocatedBytesForCurrentThread`).
  - El revisor leyó los 30 tableros de la muestra de R3 con el lector: todos se construyen y validan. Un tablero literal de R3 (`d1_n0000`) va en un test, para que un cambio de formato del generador rompa el cliente.
- **Verificación** (worktree `spark-r6`, ARM64):
  - `unity-compile.ps1` → OK;
  - `unity-test.ps1 -Mode EditMode -TimeoutSec 3600` → **2.851 (2.848 + 3 omitidos), 0 fallos**;
  - `dotnet build … -warnaserror` → 0 avisos.
- **code-reviewer:** primera ronda CAMBIOS NECESARIOS (faltaban tests de varias reglas y se aceptaba `world` 0). Segunda ronda: código bien, pero la aprobación quedó condicionada a pasar Unity, que estaba bloqueado por la licencia. Pasó en verde tras la revisión del dueño: **APROBADO**.
- **Test inestable** (no es del Spark): `IntroArtTests.FruitFlight_Has14DistinctSprites_InOneTexture_GeneratedWithin10ms` falló una vez por tiempo (10,42 ms con un tope de 10 ms) en este equipo ARM64. Al relanzarlo con filtro pasó.
- **Enganche opcional E14** (extraer los ayudantes que `DailyPoolReader` copia de `LevelPackReader`): en «Enganches para el integrador».

## 2026-10-19 · R4 · QA del pool en `level-qa --daily`: HECHA · Bloque (a) listo para integrar

- **Commits** (en `spark/reto-del-rio-r4`, fusionados con `--no-ff` en `spark/reto-del-rio` como `ba57160`):
  - `32b8cc4`: detalles de la revisión de R3;
  - `cafe8c4`: la QA;
  - `ed109a3`: «Enganche:» del despacho en `tools/level-qa/Program.cs`;
  - `8afdd0e`: arreglos de la revisión.
- **Archivos nuevos:**
  - `tools/level-qa/Daily/DailyChecks.cs` (21 ids `QA-DAILY-*`) y `DailyQaCommand.cs`;
  - `tools/level-qa/Daily/Shared/DailySpec.cs` y `DailyIds.cs`: copias byte a byte de las del generador; la autoprueba falla si divergen (E16 las sustituye por enlaces);
  - `tools/level-qa/fixtures/daily/`: 1 fixture válido y 18 malos.
- **Uso:**
  ```
  dotnet run --project tools/level-qa -- --daily <pack> [--spec <daily_v1.json>] [--partial] [--no-reproduce]
  ```
  `--self-test daily` prueba todos los fixtures.
- **Severidad:**
  - **fallo:** esquema (vía `validate_schema.py --pack`), slot ↔ id/seed, 730 días por banda, K/vacíos/capacidad, ventana del bot (R-Q11, decidida (a): es compuerta), A*, par ≥ 2K, tope de pares **por banda según la especificación** (R-Q1b, decidida (a): B3 sin tope), sin mecánicas ni ocultos, `relief` vacío y campos prohibidos, ≤ 14 cuias, < 2 MB (siempre), y que la semilla reproduzca el tablero entero (proceso hijo del generador, 15 min de tope);
  - **solo aviso (R-Q10):** anti-repetición (forma y Hamming, con la vuelta del ciclo 729 → 0) y trampas tempranas.
- **Detalles:**
  - Con `--partial` una muestra nunca pasa por pool completo: siempre sale el aviso «no es el pool completo».
  - El despacho de `Program.cs` va antes del `--self-test` genérico, por la misma razón que en R2.
- **Verificación:**
  - `level-qa --self-test daily` → 30 PASS; las autopruebas que ya había → todo PASS;
  - QA de la muestra con `--partial` → exit 1: 15 `QA-DAILY-RISK` (los K 9 de B2 y todo B3) y 2 `QA-DAILY-BOT` (`d2_n0002`, `d2_n0006`). Era lo esperado mientras R-Q1b y R-Q11 seguían abiertas; con R-Q1b (a) en la especificación (B2 sin K 9, B3 sin tope) los RISK dejan de aplicarse, y los BOT siguen siendo fallo (R-Q11 (a));
  - QA del pool completo (generado fuera del repositorio) → exit 1:

    | Id | Fallos | Avisos | Por banda |
    |---|---|---|---|
    | QA-DAILY-RISK | 1.095 | — | B2: 365 · B3: 730 |
    | QA-DAILY-BOT | 68 | — | B1: 13 · B2: 55 |
    | QA-DAILY-REPEAT-SHAPE | — | 414 | B1: 133 · B2: 103 · B3: 178 |
    | QA-DAILY-EARLY-TRAPS | — | 1.172 | B1: 353 · B2: 669 · B3: 150 |

    Pasan DAYS, SIZE, SCHEMA y REPRO (los 2.190 tableros se reproducen).
  - builds con `-warnaserror` → 0 avisos; `dotnet test` → 981 (975 + 6); `validate_schema.py` → 103/103.
- **code-reviewer:** primera ronda CAMBIOS NECESARIOS (faltaban los tests de los avisos de R-Q10, y `--partial` apagaba la compuerta de tamaño). Arreglado; segunda ronda **APROBADO**.
- **Para el dueño (R-Q10):** EARLY-TRAPS avisa en 1.172 de 2.190 tableros, porque con 1 vacío y la etiqueta «normal» el tope de la campaña es 0. Si algún día pasa a compuerta, el Reto necesitará su propio tope.

### Bloque (a) (R2-R4: el modo diario del generador y su QA): listo para integrar (ya integrado: `fcbbd94`)

Está en `spark/reto-del-rio` y no toca Unity. Sus enganches (E15-E18) están en «Enganches para el integrador».

## 2026-10-19 · Licencia de Unity: RESUELTO

El dueño revisó la licencia en Unity Hub. Desde entonces Unity compila y pasa los tests (R6 en verde). La entrada de abajo queda como historial.

## 2026-10-19 · ⚠ BLOQUEO: la licencia de Unity dejó de valer en este equipo (resuelto: entrada de arriba)

- **Desde** unas 12:10 (hora local) ningún batchmode arranca: `unity-compile.ps1` y `unity-test.ps1` salen con el código 6.
- **Log** (`.claude/worktrees/spark-r6/Logs/compile.log`):
  - `Error: Code 404 … Found 0 entitlement groups and 0 free entitlements matching requested entitlement ids`;
  - `'com.unity.editor.headless' was not found`;
  - `No valid Unity Editor license found`.
- **Qué no es la causa:** el aviso «Code 10 while verifying Licensing Client signature» ya salía en las pasadas buenas de la línea base (10:55 y 11:33), cuando sí se resolvían los derechos. Unity Hub (3.22.3) y la CLI (1.0.0-beta.13) no han cambiado de versión.
- **Hipótesis:** se retiró o caducó el derecho de la licencia en el servidor. Por ejemplo, el asiento está en uso en otro equipo, o la licencia no incluye `com.unity.editor.headless`.
- **No he usado `unity auth` ni `unity license`** (regla dura 5).
- **Lo que necesitaba del dueño:** revisar la licencia de Unity de este equipo en Unity Hub (Preferencias › Licencias). Cuando volviera a funcionar, repetir las pruebas de Unity pendientes:
  - R6 en el worktree `spark-r6`: compilar y pasar EditMode (esperado 2.851: 2.848 + 3 omitidos);
  - todo lo que hubiera del cliente desde R7.
- **Qué afectaba:** todo el carril del cliente (R6-R11) quedaba sin verificar en Unity. Seguí escribiendo el código y comprobándolo fuera de Unity, sin darlo por terminado ni fusionarlo hasta pasar Unity. El carril de herramientas (R2-R4) no lo necesita.

## 2026-10-19 · R3 · Modo `--daily` del generador: HECHA

- **Commits** (en `spark/reto-del-rio-r3`, fusionados con `--no-ff` en `spark/reto-del-rio` como `c87a181`):
  - `e8783b9`: arreglos menores de la revisión de R2;
  - `bb1dad2`: el modo `--daily`;
  - `ee48f52`: la muestra `tools/out/daily_sample/daily_pool_v1.json` (30 tableros, 20.144 bytes, SHA-256 `61cf6bef…3ac1`) y `tools/out/daily_sample_report.md`. Se commitea como las otras muestras de `tools/out/`.
- **Archivos nuevos** en `tools/level-generator/Daily/`: `DailyRunner.cs`, `DailyTints.cs` (copia adaptada de `TintAssigner`, que no cambia), `DailyPackWriter.cs`, `DailyReport.cs` y `DailyCommand.Generate.cs`.
- **Uso:**
  ```
  dotnet run --project tools/level-generator -- --daily --spec tools/level-generator/specs/daily_v1.json --bands 1,2,3 --days 0-729 --out <dir> [--report <md>] [--check] [--threads N]
  ```
  Códigos de salida: 0 (también con avisos), 1 (un día sin tablero o `--check` distinto), 2 (uso).
- **Verificación:**

  | Comando | Resultado |
  |---|---|
  | `--daily` de la muestra (bandas 1-3, días 0-9) | exit 0; `--check` idéntico con 20 hilos y con 1 |
  | `python tools/validate_schema.py --pack tools/out/daily_sample/daily_pool_v1.json` | PASS |
  | `python tools/validate_schema.py` | 103/103 |
  | `--self-test daily` | todo PASS: 3 días por banda idénticos byte a byte en paralelo y con 1 hilo; la semilla reproduce `d2_n0001` |
  | `--self-test` general | todo PASS |
  | `dotnet build` de `tools/level-generator` y de `dotnet/AmazonFill.slnx` con `-warnaserror` | 0 avisos |
  | `dotnet test` | 981 (975 + 6 omitidos) |

- **Pool completo** (3 × 730), generado fuera del repositorio: 6,1 s con 20 hilos y 1.445.059 bytes. Valida con `--pack` y es reproducible. **No se publica** (R5).
- **Muestra, por banda (10 días cada una):**

  | Banda | bot_fail mín · mediana · máx | par mín · mediana · máx | Fuera de ventana | Pares > tope |
  |---|---|---|---|---|
  | B1 | 0,400 · 0,438 · 0,600 | 13 · 16,5 · 21 | 0 | 0 |
  | B2 | 0,625 · 0,725 · 0,900 | 19 · 20,5 · 25 | 2 (`d2_n0002` 0,85, `d2_n0006` 0,90; los dos K 8 con `fill`) | 5 (los de K 9) |
  | B3 | 0,750 · 0,950 · 1,000 | 21 · 22,5 · 25 | 0 | 10 (todos) |

- **En el pool completo:**
  - B1: 13 días fuera de ventana (mínimo 0,30) y 231 con el respaldo `fill` E=2;
  - B2: 55 fuera de ventana y 365 por encima del tope de pares;
  - B3: 0 fuera de ventana y los 730 por encima del tope de pares.
- **code-reviewer:** APROBADO a la primera. Reprodujo la muestra byte a byte, comprobó el SHA y cruzó las 30 filas del informe con el pack (0 discrepancias). Las sugerencias menores van en R4.
- **[P] de R3:**
  - cabecera con `empties_default` 1 y `max_risk_pairs` igual al máximo real, así que R4 comprueba el tope por banda contra la especificación;
  - `world` en cada tablero (opcional en `daily`);
  - un tablero por línea, como `PackWriter`;
  - `skippable` false (04 §2.6 e) y `hint_supported` true (06 §4.1; R-Q8).
- **Lo que era [P] y ya está decidido:**
  - R-Q1b: si el tope no admite el K, el tablero lleva el **mínimo de pares posible** y queda marcado, sin cambiar K ni paleta. Con R-Q1b (a) (B2 con K = 8, mínimo 1 = tope; B3 sin tope) el caso deja de darse;
  - R-Q11: si ningún candidato entra en la ventana, se guarda **el más cercano** y queda marcado. Con R-Q11 (a) pasa a ser **error** del generador (R5, en curso; encargo §9, punto 2).
- **Datos de R-Q11 (ya decidida):**
  - B3 sale casi siempre ≥ 0,90 (mediana 0,95): entra en «≥ 75 %», pero son tableros muy difíciles;
  - B2 con K 8 y `fill` se pasa de la ventana en 55 de 730 días.

## 2026-10-19 · R2 · Especificación del pool y autoprueba: HECHA

- **Commits:** `da6a9bb` (especificación, lector, ids y autoprueba) y `99d35f7` («Enganche:» del despacho en `tools/level-generator/Program.cs`), en `spark/reto-del-rio-r2`; fusionados con `--no-ff` en `spark/reto-del-rio` (`3ca28dc`).
- **Archivos nuevos:** `tools/level-generator/specs/daily_v1.json`, `tools/level-generator/Daily/DailySpec.cs`, `DailyIds.cs`, `DailyCommand.cs`.
- **Verificación** (en el worktree `spark-r2`):
  - `dotnet run --project tools/level-generator -- --self-test daily` → 86 PASS, código 0;
  - `dotnet run --project tools/level-generator -c Release -- --self-test` (las autopruebas que ya había) → 314 PASS y todas «todo PASS», código 0, 49 s;
  - `dotnet build tools/level-generator -warnaserror` → 0 avisos; `dotnet build dotnet/AmazonFill.slnx -warnaserror` → 0 avisos, código 0;
  - `dotnet test dotnet/AmazonFill.slnx` → 981 (975 + 6 omitidos), código 0;
  - `python tools/validate_schema.py` → 103/103 PASS, sin cambios en `tools/out/schema_example_08_4_1.json`;
  - Unity no aplica (la tarea solo toca `tools/`).
- **code-reviewer:** APROBADO a la primera, sin bloqueantes. Comprobó las bandas de 03 §16, las fuentes de 03 §4.4, el agua del glosario §8, las regex del esquema (la autoprueba las lee del esquema) y los 2.190 ids y semillas. Sugerencias menores (estado de la paleta de B1 y un mensaje de error) van en R3.
- **Desviación del encargo (§3.3, punto 1):** el bloque del despacho va **antes del `--self-test` genérico** (tras `--validate-spec`), no justo antes de `--parity`: el genérico solo reconoce subcomandos fijos y daría error de uso con `--self-test daily`. Son 5 líneas, en su commit «Enganche:». El revisor lo da por correcto y necesario. El integrador ya lo corrigió en el encargo §9 (E18).
- **Errata:** el mensaje de `da6a9bb` dice «43 especificaciones mal formadas»; son 44. No lo reescribo.
- **[P] en `daily_v1.json`:** regla del agua `1 + (día mod 9)` (R-Q2; R5 la cambia a la fecha, encargo §9, punto 3) y `k_by_day` (día par → K menor, impar → mayor). Los tintes y los pares de riesgo se leen de `campaign_v2.json` (`tints_from`), sin copiarlos.
- **Lo que era [P] y ya está decidido:** paleta de B2 y B3 (R-Q1b (a)) y lo que haga el generador con los días fuera de ventana (R-Q11 (a)); los dos se aplican en R5.

## 2026-10-19 · R1 · Nota de diseño: HECHA

- **Commits:** `3e0063e` (la nota) y `4968d1a` (junta dos comentarios duplicados), en `spark/reto-del-rio`.
- **Archivo:** `docs/dev/reto-del-rio.md`.
- **code-reviewer:** primera ronda CAMBIOS NECESARIOS (6 bloqueantes: el contrato del almacén no seguía a `ILevelStateStore`; la ventana del bot era a la vez aviso y compuerta; la reconciliación sin red se daba por regla; las opciones de R-Q1b estaban incompletas; faltaba dónde viven los tests que ven AF.LiveOps; cinco citas imprecisas). Arreglados todos; segunda ronda **APROBADO**.
- **Para el integrador:**
  - `IServerDate` y `FakeServerDate` van en **AF.Platform** (08 §2.2 le da el «`Date` del servidor para el reloj») y no en AF.LiveOps, como decía el encargo §4. **Confirmado** por el integrador (encargo §5). Nota §1.2.
  - Las decisiones R-Q1-R-Q10 llevan fecha 2026-10-20 en la primera versión del encargo; el reloj de este equipo dice 2026-10-19 (el encargo §5 dice ahora 2026-10-19). Confírmalo al llevarlas a `decisiones.md`.
  - Ediciones del GDD que piden R-Q4-R-Q8 (08 §4.3, 08 §9 tipo de `daily_overrides`, 06 §4.1): nota §1.3 y enganche E11.

### Preguntas nuevas para el dueño (nota §1.6): TODAS DECIDIDAS

| Id | Pregunta | Opciones (recomendada primero) | Qué bloqueaba | Estado |
|---|---|---|---|---|
| **R-Q1b** | R-Q1 no se puede cumplir: B2 con K = 9 tiene 3 pares de riesgo (tope 1) y B3, con los tintes de 1-200, al menos 3 con K = 10 y 4 con K = 11 (tope 2). Cálculo en la nota §1.1 | (a) B2 solo con K = 8 y B3 con `max_pares_riesgo` libre, como la campaña desde el 201; (b) subir los topes: B2 a 3 y B3 a 4; (c) mantener los topes y bajar K (B2 = 8, B3 = 9); (d) ampliar la paleta de B3 a 1-300 (no basta) | Paleta de R3 (va como parámetro [P]) y la publicación del pool (R5) | **DECIDIDA: (a)** |
| **R-Q11** | Si en 30 candidatos ningún tablero entra en la ventana del bot de su banda (03 §4.4 midió 0,88-1,00 de fallo con 1 vacío; B2 pide 60-80 %) | (a) la ventana es compuerta: el día no se publica hasta ajustar la especificación; (b) se publica el más cercano y QA avisa; (c) ensanchar la ventana (cambia 03 §16) | R5 | **DECIDIDA: (a)** |
| **R-Q12** | Al reconectar, qué hace la reconciliación con las victorias cobradas sin red (08 §10.1) | (a) nada: no se retiran semillas; (b) anular las de fecha posterior a la del servidor (sello y racha) | Solo esa rama de R7b (irá detrás de una opción) | **DECIDIDA: (a)** |
| **R-Q13** | ¿Intersticial al terminar un Reto? (05 §6.1 solo habla de la campaña) | (a) no; (b) como en la campaña | R8/R9 | **DECIDIDA: (a)** |
| **R-Q14** | La Pororoca de la racha puede llegar a B1 antes del nivel 30, que la desbloquea (04 §2.4) | (a) entra en el stock y espera al 30; (b) otro premio en B1; (c) nada antes del 30 | Solo un aviso en la UI de R9 | **DECIDIDA: (a)** |

## 2026-10-19 · R0 · Arranque y línea base: HECHA (desbloqueada)

El dueño pidió «instala lo necesario para trabajar». Instalado:
- `jsonschema` 4.26.0 en Python 3.14 (`py -3.14 -m pip install jsonschema`);
- .NET SDK 10.0.401 ARM64 (`winget install Microsoft.DotNet.SDK.10 --version 10.0.401 --architecture arm64`; instalador oficial de download.microsoft.com con el hash verificado);
- Unity 6000.3.25f1 ARM64 (changeset `e1dba0a9aba4`) con Android Build Support, OpenJDK, SDK y NDK, con Unity Hub 3.22.3 en modo headless (`-- --headless install --version 6000.3.25f1 --changeset e1dba0a9aba4 --architecture arm64 --module android --childModules`). La Unity CLI no se usó para instalar (`CLAUDE.md`). La licencia ya estaba activa en el equipo.

**Línea base** (`spark/reto-del-rio` en `2db3a07`, igual que `origin/integracion-fase1-fase2` `80b26b4` más el archivo de estado):

| Comando | Resultado | Tiempo |
|---|---|---|
| `dotnet build dotnet/AmazonFill.slnx -warnaserror` | código 0, 0 avisos | 18 s |
| `dotnet test dotnet/AmazonFill.slnx` | 981 (975 + 6 omitidos), código 0 | 3 min 9 s |
| `python tools/validate_schema.py` | 103/103 PASS, código 0, sin cambios | — |
| `pwsh -NoProfile -File scripts/unity-compile.ps1 -Architecture arm64` | OK, código 0 (primera importación incluida) | 8 min |
| `pwsh -NoProfile -File scripts/unity-test.ps1 -Mode EditMode -Architecture arm64` | 2.744 (2.741 + 3 omitidos), 0 fallos, código 0 | 6 min |
| `pwsh -NoProfile -File scripts/unity-test.ps1 -Mode PlayMode -Architecture arm64 -TimeoutSec 3600` | 1.334 (1.175 + 159 omitidos), 0 fallos, código 0 | 22 min |

- Coinciden con los recuentos de `CLAUDE.md`.
- **Aviso:** con el límite por defecto (900 s), PlayMode sale con el código 6 (sin veredicto) a los 15 min, en mitad de `MapMarkersTests`: en este equipo tarda 21 min. Uso `-TimeoutSec 3600`, el valor que el propio script recomienda para una primera importación.
- Máquina: ARM64, 64 GB de RAM. Con 32 GB o más el encargo permite 3 Editores a la vez; me quedo en **2** salvo que haga falta.

## 2026-10-19 · R0 · Primer intento: BLOQUEADA por el entorno (resuelto en la entrada de arriba)

**Rama:** `spark/reto-del-rio`, creada desde `origin/integracion-fase1-fase2` en `80b26b4` («Deja en el encargo del Spark las respuestas del dueño a R-Q1-R-Q10 del Reto del río») y empujada a `origin`. `git lfs pull` hecho sin errores.

**Máquina (sección 1 del encargo):**

| Comprobación | Comando | Resultado |
|---|---|---|
| Equipo | `Win32_ComputerSystem.Model` / `Win32_Processor.Name` | ProArt P14 H7407BA · NVIDIA RTX Spark N1X (20 núcleos) |
| Arquitectura | `$env:PROCESSOR_ARCHITECTURE` | `ARM64` → `-Architecture arm64` / `--architecture arm64` |
| RAM | `(Get-CimInstance Win32_ComputerSystem).TotalPhysicalMemory / 1GB` | 64,0 GB (de sobra: se podrían usar hasta 3 Editores; aún no aplica) |
| Python | `python --version` | 3.14.7 (ARM64) ✔ |
| `jsonschema` | `python -c "import jsonschema"` / `py -3.14 -m pip show jsonschema` | **✘ No instalado** (`ModuleNotFoundError`) |
| .NET SDK | `dotnet --list-sdks` | **✘ Ningún SDK.** Solo el host y los runtimes 8.0.32 (`Microsoft.NETCore.App`, `WindowsDesktop.App`). `global.json` pide 10.0.401 |
| Editor de Unity | `Test-Path "C:\Program Files\Unity\Hub\Editor\6000.3.25f1\Editor\Unity.exe"` | **✘ No existe.** En `Hub\Editor` solo está `6000.3.26f1` (`unity editors` lo da como x86_64). El encargo prohíbe abrir otra versión sobre el proyecto |
| Unity CLI | `unity --version` | 1.0.0-beta.13 ✔ (alias `%LOCALAPPDATA%\Microsoft\WindowsApps\unity.exe`) |
| git LFS | `git lfs version` | 3.7.1 (windows arm64) ✔ |

**Verificación completa de `CLAUDE.md` (paso 3):** no se puede ejecutar ninguno de los comandos obligatorios:
- `dotnet build dotnet/AmazonFill.slnx -warnaserror` y `dotnet test`: falla el arranque del SDK («A compatible .NET SDK was not found. Requested SDK version: 10.0.401»).
- `python tools/validate_schema.py`: necesita `jsonschema`.
- `scripts/unity-compile.ps1` y `scripts/unity-test.ps1` (EditMode y PlayMode): necesitan Unity 6000.3.25f1 ARM64.

No hay línea base: **sin recuentos**. No he instalado nada (regla dura 4 de `CLAUDE.md`; el encargo, sección 1: «no lo instales tú: si falta, pregunta al dueño»).

**Me paro aquí**, como manda R0 («si algo sale en rojo en la base, para y anótalo»). R1-R12 dependen de R0 y no he empezado ninguna.

### Pendiente del dueño (para desbloquear R0; hecho, entrada de arriba)

1. **.NET SDK 10.0.401** para Windows ARM64 en `C:\Program Files\dotnet` (lo fija `global.json`, con `rollForward: latestFeature`).
2. **`jsonschema`** en Python 3.14 (`py -3.14 -m pip install jsonschema`).
3. **Unity 6000.3.25f1** (changeset `e1dba0a9aba4`), Editor **Windows ARM64**, con el módulo Android, en `C:\Program Files\Unity\Hub\Editor\6000.3.25f1\`, desde el Hub (la CLI no instala; `CLAUDE.md`, «Stack fijado»). La licencia la activa el dueño.
   - El 6000.3.26f1 x86_64 que hay instalado **no se usa**: abrirlo sobre el proyecto lo actualizaría.
4. Avisar al Spark cuando esté para repetir R0 y medir la línea base.

### Decisiones del dueño registradas (para que el integrador las lleve a `docs/dev/decisiones.md`)

Las R-Q1 a R-Q10 de la sección 5 del encargo, decididas por el dueño el 2026-10-20 («Acepto tus propuestas para R-Q1 a R-Q10 del Reto del río; déjalas en el encargo del Spark»). Se aplicarán tal cual cuando se desbloquee el trabajo:

| Id | Decisión |
|---|---|
| R-Q1 | Cada banda usa solo los tintes ya estrenados en su tramo de la campaña: B1 (K 6-7) con los de 1-40 y `max_pares_riesgo` 0; B2 (K 8-9) con los de 1-80 y como mucho 1; B3 (K 10-11) con los de 1-200 y como mucho 2 (ajustado después por R-Q1b) |
| R-Q2 | El agua (`water_base`) y el fondo rotan por los mundos 1-9 según la fecha: el agua y el kit de fondo del mundo de ese día |
| R-Q3 | Manda la fecha en que se empezó la partida: se sigue con ese tablero y se cobra ese premio aunque se gane pasada la medianoche |
| R-Q4 | La partida del Reto se reanuda al volver a abrir la app el mismo día; al cambiar de día se descarta |
| R-Q5 | El bloque `daily_challenge` del perfil añade la última fecha cobrada, el premio pendiente de validar y los sellos del mes (además de `last_date`, `result` y `streak_days`) |
| R-Q6 | `daily_overrides` = `{fecha: {banda: id del pool}}` |
| R-Q7 | La Pororoca sale cada 7 días seguidos ganados (cada racha completa); un día sin ganar rompe la racha; el marco de país de los sellos queda fuera hasta que existan cosméticos |
| R-Q8 | Los comodines son los de la campaña salvo los que el GDD excluye en el Reto (Cuia de rescate y Salto del Boto), y gastan el stock igual que en la campaña |
| R-Q9 | `result = fail` en `af_daily_challenge` es salir sin ganar ese día |
| R-Q10 | La anti-repetición (03 §17 #9) y el tope de trampas tempranas (03 §17 #3) se miden en el pool y se informan como aviso, sin ser compuerta |

### Riesgos y notas

- Las fechas: el encargo y estas decisiones van fechados el 2026-10-20; el reloj de esta máquina dice 2026-10-19.
- `git switch -c … origin/integracion-fase1-fase2` deja como upstream la integración; el push con `-u origin spark/reto-del-rio` lo corrige para que ningún `git push` vaya a `integracion-fase1-fase2`.

---

## Enganches para el integrador

Única sección de enganches (encargo §3.4). Cada uno: archivo, parche o descripción exacta, por qué, cómo se prueba y estado. Los que dependen de las pantallas (E1-E7, E9, E10, E12) llevan su parche exacto en **R12**; aquí van descritos.

| # | Estado | Archivo |
|---|---|---|
| E1 | Opcional · no preparado (la tarjeta va en su lienzo; requisito de 16:9 en R12) | `Scripts/UI/LobbyScreen.cs` |
| E2 | **Parche probado** (R12, `spark-enganches/E2.diff`) | `Scripts/UI/GameBootstrap.cs` |
| E3 | **Parche probado** (R12, `E3.diff` + regenerar escena) | `Scripts/UI/Editor/GameSceneBuilder.cs` |
| E4 | **Parche probado** (R12, `E4.diff`) | `Scripts/Save/Profile.cs`, `ProfileJson.cs`, `ProfileMigrations.cs` |
| E5 | **Parche probado** (R12, `E5.diff` + `E5-fix.diff`) | `Data/Loc/strings.csv` |
| E5b | **Parche probado** (T1, `spark-enganches/E5b.diff`): las 2 filas de E6 y E7 | `Data/Loc/strings.csv`, `LocTablesTests` |
| E6 | **Parche probado** (T1, `spark-enganches/E6.diff`; necesita E5b) | `Scripts/UI/GameScreen.cs` |
| E7 | **Parche probado** (T1, `spark-enganches/E7.diff`; necesita E5b) | `Scripts/UI/NoCanoesScreen.cs`, `FunnelPlayModeTests.cs` |
| E8 | **Ya no hace falta** | (iba en `Scripts/GameFlow/LevelFlow*.cs`) |
| E9 | **Parche probado** (R12, `E9.diff`) | `Scripts/Economy/Transaction.cs`, `Wallet.cs` |
| E10 | **Ya no hace falta** (R10 sale de archivos del Spark) | `Scripts/UI/DevProbe.cs` (si hace falta) |
| E11 | **Parches probados** (`spark-parches-docs.md`, 12 diffs, aplican sobre `bc65242`) | `docs/dev/decisiones.md`, `docs/gdd/03`, `05`, `06`, `08` |
| E12 | **Parches probados** (`spark-parches-docs.md`) | `docs/dev/prototipo-v0.md`, `docs/dev/traspaso/README.md`, `CLAUDE.md` |
| E13 | Opcional | `Assets/_Project/Tests/EditMode/AF.Tests.EditMode.asmdef` |
| E14 | Opcional | `Scripts/Levels/LevelPackReader.cs`, `DailyPoolReader.cs` |
| E15 | **Aplicado** (`752bb55`) | `tools/level-generator/Program.cs`, `README.md` |
| E16 | **Aplicado en la rama** `spark/reto-cierre` (`c7ec205`; registro en `spark-enganches/E16.diff`) | `tools/level-qa/level-qa.csproj` y `tools/level-qa/Daily/` |
| E17 | **Aplicado** (`752bb55` y, en `spark/reto-cierre`, `e5061f2`) | `tools/level-qa/Program.cs`, `README.md` |
| E18 | Punto 1 corregido por el integrador (encargo §9); queda el punto 2 | Encargo §3.3 |
| E19 | Pendiente, para cuando exista la Crecida | `Scripts/GameFlow/LevelFlow.Funnel.cs`, `FailResolve.cs` |
| E20 | Pendiente (limpieza) | `Scripts/Levels/LevelDefinition.cs`, `LevelCatalog.cs` |
| E21 | **Parche probado** (R12, `E21.diff`) | `Scripts/UI/GameScreen.cs` (`OnIntroStarted`) |
| E22 | **Parche probado** (R12, `E22.diff`; opcional) | `Scripts/UI/GameBootstrap.cs` (`PressAndroidBack`) |
| E23 | **Parche probado** (R12, `E23.diff`) | `Scripts/UI/GameBootstrap.cs` (`OnApplicationPause`) |
| E24 | **Parche probado** (R12, `E24.diff`) | `Scripts/UI/DevProbe.cs` |
| E25 | Para cuando exista la Crecida | barra de la Crecida en la vista del Reto |

### E1 · Inicio: la tarjeta del Reto (pendiente; parche en R12)

- **Archivo:** `Assets/_Project/Scripts/UI/LobbyScreen.cs`.
- **Qué:** crear `DailyChallengeCard` (clase nueva de R9) en el inicio y refrescarla al mostrarlo. Mientras el sistema no exista en el build, oculta (D-008).
- **Por qué:** 06 §4.1 «Pantalla» y §4.7; encargo §3.4.
- **Prueba:** PlayMode de R9-R11 (los cinco estados de la nota §8).

### E2 · Arranque: `SetUpDaily()` (pendiente; parche en R12)

- **Archivo:** `Assets/_Project/Scripts/UI/GameBootstrap.cs`.
- **Qué:** campo `[SerializeField] TextAsset dailyPool;` y la llamada a `SetUpDaily()` (parcial nuevo `GameBootstrap.Daily.cs`) tras crear perfil, cartera y reloj.
- **Por qué:** encargo §3.4; crea el pool, `DailyChallengeService` y las pantallas, y conecta la victoria.
- **Prueba:** PlayMode de R11 (punta a punta).

### E3 · Escena: el pool en `dailyPool` (pendiente; parche en R12)

- **Archivo:** `Assets/_Project/Scripts/UI/Editor/GameSceneBuilder.cs`.
- **Qué:** asignar `Assets/_Project/Data/Daily/daily_pool_v1.json` a `dailyPool`, como con `river_stops`; el integrador regenera `Game.unity` (regla dura 3).
- **Por qué:** 08 §3; 08 §6.2.
- **Prueba:** regenerar la escena y PlayMode de R11.

### E4 · Perfil: bloque `daily_challenge` y `ProfileDailyChallengeStore` (pendiente; parche en R12)

- **Archivos:** `Assets/_Project/Scripts/Save/Profile.cs`, `ProfileJson.cs` (y `ProfileMigrations.cs` si hiciera falta; 08 §4.3 permite un campo nuevo sin migración mientras el esquema no se publique).
- **Qué:** el bloque de la nota §10 (`last_date`, `result`, `streak_days`, `last_claimed_date`, `pending`, `offline_claims`, `stamps`, `in_progress`) y `ProfileDailyChallengeStore : IDailyChallengeStore`.
- **Requisito:** el registro y la cartera **en el mismo `Profile`**, para no pagar dos veces la misma fecha (R7b, pendiente menor); acotar `offline_claims` (solo sirve a la rama (b) de R-Q12, no la decidida).
- **Por qué:** 08 §4.3; R-Q4, R-Q5, R-Q16.
- **Prueba:** ida y vuelta del JSON del perfil y los tests de `DailyChallengeStoreTests` sobre el almacén del perfil.

### E5 · Textos (pendiente; filas en R9-R12)

- **Archivo:** `Assets/_Project/Data/Loc/strings.csv`.
- **Qué:** copiar las filas de `docs/dev/traspaso/spark-strings-reto.csv` (misma cabecera, `status = borrador`; lo cultural, `pendiente_asesor`).
- **Por qué:** 08 §4.7; `.claude/rules/localizacion.md`.
- **Prueba:** los tests de localización que ya validan `strings.csv`.

### E5b · Las dos filas de texto de E6 y E7 (parche probado; T1)

- **Archivos:** `Assets/_Project/Data/Loc/strings.csv` y `LocTablesTests`.
  - En `strings.csv`, las filas `daily_teaser` y `no_canoes_daily`, en orden ordinal e idénticas a `spark-strings-reto.csv`.
  - En `LocTablesTests`, `keys.AddRange(DailyText.E5bKeys)` en `UiTextKeys`.
- **Parche:** `docs/dev/traspaso/spark-enganches/E5b.diff`.
- **Orden:** va **antes o junto con E6 y E7**. Sin E5b, en release `daily_teaser` y `no_canoes_daily` salen vacíos. Con E5b integrado, las claves de `DailyText.E5bKeys` pasan a `DailyText.NewKeys`.
- **Prueba:** `LocTablesTests` y `DailyUiLogicTests` en EditMode. `Csv_RowsAreInStringsCsv_E5…` admite que falten las dos a la vez (sin E5b) o que estén las dos; nunca una sola.

### E6 · Tarjeta de 2 s tras ganar el 7 (parche probado; T1)

- **Lo del Spark** (en la rama):
  - `Scripts/UI/DailyTeaserCard.cs`, con la placa clara del aviso, sin rojo y sin capturar toques;
  - `Tests/PlayMode/DailyTeaserCardTests.cs`;
  - el texto `daily_teaser` = «Desde el nivel {level}, un tablero nuevo cada día» (06 §2.2, fila 7; 02 §13), con `{level}` = `live_unlock_level_daily`.
- **Parche:** `docs/dev/traspaso/spark-enganches/E6.diff`.
  - En `Scripts/UI/GameScreen.cs`, cuatro puntos: el campo `_dailyTeaser`, `_dailyTeaser?.Tick(dtMs)`, el acceso `internal DailyTeaser` y un bloque al final de `OnVictoryScreen` con `flow.LevelNumber == 7`.
  - El test nuevo `DailyTeaserVictoryTests.cs`, sin `.meta`: lo genera Unity al integrarlo.
- **Prueba:** `DailyTeaserVictoryTests`.
  - La victoria real del 7 la enseña a los 900 ms y durante 2 s.
  - Siguiente e Inicio siguen activos, y Siguiente lleva al 8.
  - Pulsar Siguiente la quita.
  - No sale sin el Reto en el build (`DailyPoolDisabledOverride`) ni en la victoria del 8.
- **[P] para el dueño (recomendación: aceptarlos):**
  - sale en cada victoria del 7 que se vea, sin guardar nada en el perfil;
  - a los 900 ms, centrada sobre las cuias;
  - solo con el Reto en el build y con `live_unlock_level_daily` > 7;
  - no entra en la cola de avisos de D-011;
  - `daily_teaser` va en `pendiente_asesor` porque lo pide el encargo §10 T1.2, aunque el texto no es cultural: el dueño puede bajarlo a `borrador`.

### E7 · Sin canoas: «Mientras esperas: Jugar el Reto del río» (parche probado; T1)

- **Lo del Spark** (en la rama):
  - `Scripts/UI/NoCanoesDailyLink.cs`;
  - en `GameBootstrap.Daily.cs`: `DailyInBuild`, `NoCanoesDailyAvailable`, `PressNoCanoesDaily()` y `LastNoCanoesChoice`;
  - la sonda `AF_STATE no_canoes_choice` con `choice=daily_challenge` (04 §11; 08 §8.2), en `DailyProbeFormat.NoCanoesChoice`;
  - `Tests/PlayMode/NoCanoesDailyTests.cs`.
- **Parche:** `docs/dev/traspaso/spark-enganches/E7.diff`.
  - En `NoCanoesScreen.cs`: el enlace bajo el panel, encima de «Volver al inicio», y el id `daily_challenge` en `VisibleButtons` y en la clave del layout.
  - El test nuevo `NoCanoesDailyLinkTests.cs`, sin `.meta`.
  - **Un test existente**, `FunnelPlayModeTests.AssertNoCanoesLayout`: añade `daily_challenge` entre `infinite_canoes` y `home` cuando el enlace se ve (`b1b4628`). Hace falta porque, con la escena con pool (E3), el 31 muestra el enlace.
- **Prueba:**
  - `NoCanoesDailyLinkTests`: Sin canoas desde el inicio, desde Nivel fallido en el 31 y sin Reto en el build;
  - `NoCanoesDailyTests`;
  - `FunnelPlayModeTests` del 31.
- **[P] para el dueño (recomendación: aceptarlos):**
  - solo se ofrece con la tarjeta en «Jugar» o «Seguir» (con el día sellado o pendiente, la tarjeta abriría el calendario);
  - el toque sale por el camino de «Volver al inicio» y abre el tablero de hoy;
  - sin el chevron «›» del wireframe.

### E8 · Modo Reto en `LevelFlow` (ya no hace falta)

- Iba en `LevelFlow.cs`, `LevelFlow.Victory.cs`, `LevelFlow.Funnel.cs` y `LevelFlow.Resume.cs`: sin `SaveWin` ni `Transaction.Win`, nivel efectivo, Marea no ilimitada, sin canoas ni Cuia de rescate, reanudar.
- **Por qué ya no:** R8 lo hizo sin tocar archivos existentes, con `LevelFlow.CreateDaily(...)` en `LevelFlow.Daily.cs` (catálogo de un tablero con el nivel efectivo, progreso de solo lectura, cartera y configs copiadas, `StateStore` propio). Lo que queda va en E19 y E20.

### E9 · Cosecha doble del Reto (pendiente; parche en R12)

- **Archivos:** `Assets/_Project/Scripts/Economy/Transaction.cs`, `Wallet.cs`.
- **Qué:** una transacción para duplicar el premio del Reto (p. ej. `Transaction.DoubleHarvestDaily(int seeds)`, 60 → 120 [P]), con `source = double_harvest`.
- **Por qué:** `Transaction.DoubleHarvest(int level, RewardTier tier)` duplica la Cosecha de un nivel de campaña y `Transaction.Earn` rechaza `SeedSource.DoubleHarvest` (nota §1.4). 04 §2.8; 06 §4.1. Hasta entonces, el botón del Reto queda desactivado.
- **Prueba:** test de economía del pago doble solo con premio CONFIRMADO.

### E10 · Sondas desde los puntos de evento (pendiente; parche en R12)

- **Archivo:** `Assets/_Project/Scripts/UI/DevProbe.cs` (si hace falta).
- **Qué:** llamar a `DevProbe.Daily.cs` (R10) desde los puntos de evento; `fail` una vez por día (R-Q15).
- **Por qué:** 08 §8.2.
- **Prueba:** líneas `AF_STATE` en el build de desarrollo y los tests de `DailyProbeFormat`.

### E11 · GDD y decisiones (pendiente)

- **Archivos:** `docs/dev/decisiones.md`; `docs/gdd/03` §16; `docs/gdd/05` §6.1; `docs/gdd/06` §4.1; `docs/gdd/08` §4.3, §8.2 y §9.
- **Qué:**
  - llevar a `decisiones.md` R-Q1-R-Q10, R-Q1b, R-Q11-R-Q16 (con la aclaración de R-Q12) y la confirmación de `IServerDate` en AF.Platform; confirmar la fecha de R-Q1-R-Q10 (2026-10-20 en la primera versión del encargo; 2026-10-19 en este equipo);
  - 08 §9: `daily_overrides` pasa de `json {fecha: level_id}` a `{fecha: {banda: id}}` (R-Q6); el default `{}` no cambia;
  - 08 §4.3: el bloque `daily_challenge` de la nota §10 (R-Q4, R-Q5, R-Q16);
  - 06 §4.1: la racha «cada 7» (R-Q7), los comodines (R-Q8), las bandas (R-Q1b), la validación de pendientes (R-Q12b) y `pool_overrides` (R-Q6);
  - 03 §16: B2 con K 8 y B3 sin tope (R-Q1b); la ventana del bot es compuerta (R-Q11);
  - 05 §6.1: sin intersticial en el Reto (R-Q13); 08 §8.2: `fail` una vez por día (R-Q15).
- **Por qué:** `docs/gdd/**` está prohibido para el Spark; el GDD manda (`CLAUDE.md`). Nota §1.3.
- **Prueba:** `DailyChallengeConfigTests` (lee la tabla de 08 §9 y solo compara el tipo base `json` y el default `{}`) sigue en verde; `python tools/validate_schema.py` 103/103.

### E12 · Documentación (pendiente; texto en R12)

- **Archivos:** `docs/dev/prototipo-v0.md`, `docs/dev/traspaso/README.md`, `CLAUDE.md`.
- **Qué:** las sondas nuevas; `Scripts/LiveOps/` y `Data/Daily/` en el mapa; `--daily` y `--self-test daily`; los recuentos nuevos.
- **Por qué:** encargo §3.4.
- **Prueba:** revisión.

### E13 · `AF.Tests.EditMode.asmdef` += `AF.LiveOps` (opcional)

- **Archivo:** `Assets/_Project/Tests/EditMode/AF.Tests.EditMode.asmdef`.
- **Qué:** añadir `"AF.LiveOps"` a `references`.
- **Por qué:** solo si se quieren en EditMode los tests de AF.UI que tocan el Reto; hoy van en `AF.LiveOps.Tests` (EditMode) y en PlayMode (nota §4).
- **Prueba:** `unity-compile.ps1` y EditMode.

### E14 · Extraer los ayudantes de `DailyPoolReader` (opcional)

- **Archivos:** `Assets/_Project/Scripts/Levels/LevelPackReader.cs`, `DailyPoolReader.cs`.
- **Qué:** `DailyPoolReader` copia unas 90 líneas privadas de `LevelPackReader` (`ReadUnits`, `ReadBoard` y los ayudantes). Extraerlas a una clase `internal` compartida de AF.Levels, y hacer `internal` el lector de mecánicas si el Reto llega a usarlas.
- **Por qué:** una sola copia. Mientras tanto, los tests de la copia protegen contra una divergencia.
- **Prueba:** EditMode (`DailyPoolReaderTests`, 107, y los de `LevelPackReader`).

### E15 · `daily` en el `--self-test` sin nombre del generador (APLICADO en `752bb55`)

- **Archivos:** `tools/level-generator/Program.cs` y `README.md`.
- **Parche** (el aplicado; en `ba57160` eran las líneas 133-143):
  ```diff
           int holgura = HolguraSelfTest.Run();
  -        return Math.Max(Math.Max(Math.Max(tinaja, seasons), Math.Max(variants, registry)), Math.Max(curationTest, holgura));
  +        int daily = AmazonFill.Tools.LevelGenerator.Daily.DailyCommand.Run(["--self-test", "daily"]);
  +        return Math.Max(Math.Max(Math.Max(Math.Max(tinaja, seasons), Math.Max(variants, registry)), Math.Max(curationTest, holgura)), daily);
  ```
  - `|daily` en `Usage` (`--self-test [tinaja|…|holgura|daily]`) y «, holgura o daily» en el mensaje de error; `--daily` en la ayuda y en `tools/level-generator/README.md`.
- **Por qué:** encargo §9, punto 6.
- **Prueba:** `dotnet run --project tools/level-generator -c Release -- --self-test` → 435 PASS, 0 FAIL, exit 0, con «daily».

### E16 · `level-qa.csproj` enlaza `tools/level-generator/Daily/` (APLICADO en `spark/reto-cierre`, `c7ec205`; el texto de abajo es el plan original)

- **Archivos:** `tools/level-qa/level-qa.csproj`; `tools/level-qa/Daily/DailyQaCommand.cs` y `DailyChecks.cs` (comentarios); borrar `tools/level-qa/Daily/Shared/DailySpec.cs` y `DailyIds.cs`.
- **Parche completo:** `docs/dev/traspaso/spark-enganches/E16.diff` (idéntico a `c7ec205`, aplicado en `spark/reto-cierre`). Lo de abajo es el plan original:
  ```diff
  --- a/tools/level-qa/level-qa.csproj
  +++ b/tools/level-qa/level-qa.csproj
  @@ -31,4 +31,11 @@
       <Compile Include="../level-generator/Generation/*.cs" Exclude="../level-generator/Generation/SeasonTableWriter.cs" LinkBase="Shared/Generation" />
     </ItemGroup>
  +
  +  <!-- Reto del río (modo diario de level-qa): el mismo lector de la especificación (DailySpec), los mismos ids y semillas (DailyIds)
  +       y la misma generación (DailyRunner, DailyTints y DailyPackWriter) que el generador, enlazados, para que QA-DAILY-REPRO
  +       regenere el pool en memoria. La entrada de la línea de comandos del generador (DailyCommand*.cs) se queda fuera. -->
  +  <ItemGroup>
  +    <Compile Include="../level-generator/Daily/*.cs" Exclude="../level-generator/Daily/DailyCommand*.cs" LinkBase="Shared/Daily" />
  +  </ItemGroup>
   </Project>
  ```
  En `DailyQaCommand.cs`:
  - `Reproduce(pack, spec, tints)` regenera en memoria en lugar de lanzar `dotnet run` del generador:
    ```csharp
    List<int> bands = slots.Select(l => l.Band!.Value).Distinct().Order().ToList();
    List<int> days = slots.Select(l => l.Day!.Value).Distinct().Order().ToList();
    var dailyTints = new DailyTints(tints, spec);
    DailyRun run = DailyRunner.Run(spec, dailyTints, bands, days, Environment.ProcessorCount);
    // run.Errors → «el generador falla: …»; DailyPackWriter.CheckBoard(b) → «el generador rechaza su tablero: …»;
    // sin tableros → «el generador no da ningun tablero».
    JsonNode regen = JsonNode.Parse(PackWriter.Bytes(DailyPackWriter.Write(spec, dailyTints, run.Boards)))!;
    // y la misma comparación de antes: JsonNode.DeepEquals de cada tablero con el regenerado.
    ```
  - `RunChecks` pierde el parámetro `specPath`; se borran `GeneratorProject`, `ReproTimeout`, `TimedOut`, `Ranges`, `RepoRoot` y `RunDotnet`, y los `using` de `System.ComponentModel` y `System.Text`; se añade `using AmazonFill.Tools.LevelGenerator.Generation;`.
  - En `--self-test daily` salen las comprobaciones de identidad de `Daily/Shared/`, la de `Ranges` y la de `RepoRoot`.
  - Los comentarios de cabecera de `DailyQaCommand.cs` y `DailyChecks.cs` pasan a describir la reproducción en memoria.
- **Por qué:** una sola copia de `DailySpec` y `DailyIds`; QA-DAILY-REPRO sin proceso hijo ni tope de 15 min.
- **Prueba:** `dotnet build tools/level-qa -warnaserror` y `dotnet run --project tools/level-qa -- --self-test daily` (todo PASS).

### E17 · `level-qa`: documentar `daily` (APLICADO: `752bb55` y `e5061f2`: README con `--daily` y `daily` en el `--self-test` sin nombre)

- **Archivos:** `tools/level-qa/Program.cs` y `tools/level-qa/README.md` (los dos hechos; el README y el `--self-test` sin nombre con `daily`, en `e5061f2`).
- **Hecho:** en `Usage`, `--daily <daily_pool_v1.json> [--spec <daily_v1.json>] [--partial] [--no-reproduce]` y `--self-test [key2|reglas|variante|daily]`; el mensaje de error con «key2, reglas, variante o daily». Desde `e5061f2`, el `--self-test` sin nombre de level-qa ejecuta `key2` y `daily` y sale con el código mayor.
- **Pendiente:** `--daily` en `tools/level-qa/README.md`. Va con E16: el texto tiene que describir QA-DAILY-REPRO tal como quede (en memoria con E16; hoy, proceso hijo).
- **Prueba:** `dotnet run --project tools/level-qa -- --self-test bogus` → el mensaje de uso nombra «daily» (comprobado).

### E18 · Corrección del encargo §3.3 (punto 1 hecho; queda el punto 2)

- **Archivo:** `docs/dev/traspaso/spark-reto-del-rio.md` §3.3.
- **Punto 1** (`tools/level-generator/Program.cs`): el despacho va «antes del `--self-test` genérico», no «justo antes de `--parity`». **Corregido** por el integrador en el encargo §9.
- **Punto 2** (`tools/level-qa/Program.cs`): cambiar «antes de la lectura de argumentos de la campaña» por «antes del `--self-test` genérico (que rechazaría «daily»)», que es donde está (R4).
- **Por qué:** el `--self-test` genérico solo reconoce subcomandos fijos y daría error de uso con `--self-test daily`.
- **Prueba:** revisión del texto.

### E19 · Guarda de la Crecida en `FailResolve` (pendiente; para cuando exista la Crecida)

- **Archivos:** `Assets/_Project/Scripts/GameFlow/LevelFlow.Funnel.cs` y `FailResolve.cs`.
- **Parche:**
  ```diff
  @@ private void Fail(FailKind kind)
  -        FailOutcome outcome = FailResolve.Resolve(_attempt, kind, LevelNumber, _wallet);
  +        FailOutcome outcome = FailResolve.Resolve(_attempt, kind, LevelNumber, _wallet, campaign: !IsDaily);
  ```
  ```diff
  -        public static FailOutcome Resolve(LevelAttempt attempt, FailKind kind, int level, Wallet? wallet)
  +        public static FailOutcome Resolve(LevelAttempt attempt, FailKind kind, int level, Wallet? wallet, bool campaign = true)
  ...
               canoeLost = wallet.Apply(Transaction.HardFail(level)).CanoeLost;
           }
  +        if (!campaign)
  +        {
  +            return new FailOutcome(kind, attempt.FailsLevel, canoeLost); // 06 §4.3: el Reto no rompe la Crecida ni gasta la Hoja escudo
  +        }
  ```
- **Por qué:** 06 §4.3 («si not nivel.es_campana: return»). Hoy no hace falta: por esa vía no se cuenta nada de campaña (`stats.fails_hard` no cambia, y lo fija un test).
- **Prueba:** el test del reinicio desde el modal del Reto comprueba que la Crecida del perfil no cambia, y un test de campaña, que sí se rompe.

### E20 · `LevelDefinition.WithLevel` y `LevelCatalog.ForBoard` (pendiente; limpieza)

- **Archivos:** `Assets/_Project/Scripts/Levels/LevelDefinition.cs` y `LevelCatalog.cs`.
- **Parche:**
  - `internal LevelDefinition WithLevel(int level) => new LevelDefinition(Id, level, World, WaterBase, _palette, Units, _caps, _cuias, _attrs, IsMurky, _hidden, Par, DifficultyLabel, _relief, IntroMechanic, Skippable, HintSupported, Band, ParApprox);`
  - `public static LevelCatalog ForBoard(LevelDefinition board, int level) => FromPack(new LevelPack("daily_play", "daily", board.World, "daily_play", board.WaterBase, new[] { board.WithLevel(level) }));`
  - Después, `DailyPackFor` (en `LevelFlow.Daily.cs`) se reduce a `LevelCatalog.ForBoard(board, effectiveLevel)`.
- **Por qué:** quita el paso por un JSON interno que no cumple el esquema af-level/1 (lo señaló la revisión de R8).
- **Prueba:** `DailyLevelFlowTests` tal cual, con la comprobación «same».

### E21 · Rótulo «Nivel N · intento k» en el Reto (pendiente; R9)

- **Archivo:** `Assets/_Project/Scripts/UI/GameScreen.cs`, en `OnIntroStarted` (línea ~996).
- **Parche:**
  ```diff
                   StartIntro(flow, durationMs, isShort);
  -                if (isShort)
  +                if (isShort && !flow.IsDaily)
                   {
                       // 02 §2 (INTRO): «En reintento muestra Nivel N · intento k», …
  ```
- **Por qué:** en el Reto, cada Volver a la orilla enseña «Nivel {nivel efectivo} · intento k», y 06 §4.1 pide la cabecera «Reto del río» sin «Nivel N». Hasta que se aplique, el defecto se ve en el teléfono.
- **Prueba:** en `DailyChallengeTests.Restart_IsFreeAndUnlimited_SameBoard`, tras `PressRestart()` y un fotograma, `Assert.That(daily.View!.BannerText, Is.Null.Or.Not.Contains("Nivel"))`. En la campaña, el test de reintento que ya existe sigue viendo «Nivel N · intento k».

### E22 · Atrás con el Reto abierto (opcional; R9)

- **Archivo:** `Assets/_Project/Scripts/UI/GameBootstrap.cs`, en `PressAndroidBack()`, justo después del bloque del Mapa del río (`if (_mapScreen != null && _mapScreen.Visible) { … }`) y antes de `if (_app.Screen == AppScreen.Level)`.
- **Parche:**
  ```diff
  +            // Reto del río (R9): Atrás cierra su calendario como «Inicio»; con el tablero del Reto en juego no hace nada, como en un
  +            // nivel (02 §3; 04 §6.6). HandleBack devuelve false si no hay calendario abierto.
  +            if (_dailyScreen != null && _dailyScreen.IsOpen)
  +            {
  +                return _dailyScreen.HandleBack();
  +            }
  +
               if (_app.Screen == AppScreen.Level)
  ```
- **Por qué:** con el Reto abierto, AppFlow sigue en Lobby, así que sin la guarda Atrás caería en `PressBack()` del inicio, que está oculto. Con ella, Atrás solo cierra el calendario; en el tablero no hace nada, como en la campaña.
- **Prueba:** `AndroidBack_DoesNothingOnTheBoard_ButClosesTheCalendar` cubre la lógica de `HandleBack`. Con el enganche, `boot.PressAndroidBack()` da false con el tablero y true con el calendario, y el inicio sigue oculto mientras se juega.

### E23 · Volver de segundo plano en el Reto (pendiente; R9)

- **Archivo:** `Assets/_Project/Scripts/UI/GameBootstrap.cs`, en `OnApplicationPause` (al volver de segundo plano), justo después del bloque `if (_flow != null && _screen != null && InLevel) { … }`.
- **Parche:**
  ```diff
  +            // Reto del río (R9; 02 §14): su vista salta al modelo y la selección se limpia, como la de campaña.
  +            if (_dailyScreen?.Flow != null && _dailyScreen.View != null)
  +            {
  +                _dailyScreen.View.SnapToModel();
  +                _dailyScreen.View.SettleReveal();
  +                _dailyScreen.View.IdleHintBackFromBackground();
  +                _dailyScreen.Flow.ClearSelection();
  +            }
  ```
- **Por qué:** `InLevel` es false durante el Reto (AppFlow está en Lobby). Sin esto, al volver de segundo plano la vista del Reto se queda a mitad de animación, con la cuia seleccionada y sin reiniciar la cuenta de la pista por inactividad (02 §14; 02 §3.4; 04 §2.3). La persistencia no cambia: `PersistNow` ya guarda el lote del perfil.
- **Prueba:** test PlayMode que abre el Reto, toca una cuia (queda seleccionada), llama a `boot.SendMessage("OnApplicationPause", false)` y afirma `daily.Flow.Selected == -1` y `daily.View.IsViewIdle`.

### E24 · Las sondas de partida del Reto, marcadas (pendiente; R9-R10)

- **Archivo:** `Assets/_Project/Scripts/UI/DevProbe.cs` (prohibido para el Spark; `DevProbe` es `sealed`).
- **Parche:** justo después de cada una de las cuatro líneas `sb.Append(",\"level\":").Append(flow.LevelNumber.ToString(CultureInfo.InvariantCulture));` (hoy en las líneas 92 `AnimSkipped`, 361 `FormatState`, 488 `FormatFunnel` y 585 `FormatVictory`):
  ```diff
  +            AppendDailyBoard(sb, flow);
  ```
  `AppendDailyBoard` ya está en `DevProbe.Daily.cs` (R10): en el Reto añade `,"daily":"d{b}_n{nnnn}"`; en la campaña no añade nada.
- **Por qué:** la vista del Reto usa su propia `DevProbe`, pero las líneas `load`, `ready`, `pour`… salen con `"level"` igual al nivel efectivo (`max_level + 1`), que no es un nivel de campaña. Con la marca se distinguen por adb (08 §8.2 pide `level_name = d{b}_n{nnnn}` para el Reto). Las líneas `Event` de `GameScreen*.cs` (`idle_hint`, `hint`, `tinaja_tip`) quedan sin marca; basta la línea `load`.
- **Prueba:** en PlayMode, abrir el Reto y comprobar que `load`, `ready` y `pour` llevan `"daily":"…"`, y que en la campaña no aparece. Documentarlo en `docs/dev/prototipo-v0.md` (E12), por si algún script de adb da por hecho que `level` es ≥ 1.

### E25 · Sin barra de la Crecida en el Reto (para cuando exista)

- **Qué:** cuando exista la barra de la Crecida en la vista de partida, ocultarla con la misma guarda que E21 (`flow.IsDaily`). 06 §4.1 pide el tablero del Reto sin barra de Crecida, y 06 §4.3 dice que el Reto no la toca. Va junto con E19. En el código, `DailyChallengeScreen.cs` lo anota como «E21»: la numeración correcta es E25.
