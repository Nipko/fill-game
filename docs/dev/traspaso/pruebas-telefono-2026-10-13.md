# Prueba de la tanda 2 en el teléfono (2026-10-13): resultados para el escritorio

El portátil compiló `main` en `72a266a` («Pone al día los recuentos de tests de CLAUDE.md tras la tanda 2»), que incluye la tanda 2 integrada en `94905b8` y el arreglo de B3 (`8c7cfa2`). Instaló el `dev.apk` en el SM-S938B (Android 16, por cable) y recorrió «Qué probar en el teléfono (tanda 2)» del traspaso.

- **Cómo se probó:** `AF_STATE` y `AF_LAYOUT` de logcat, toques con `adb shell input tap` y capturas directas al PC con `exec-out screencap`. Las animaciones cortas se midieron en vídeos de `screenrecord`, cuyos fotogramas saca el decodificador de Windows (`Windows.Media.Editing`), sin instalar nada.
- **Perfil:** limpio (`pm clear`) donde la hoja lo pide.

**Resultado: la tanda 2 cumple.** No hay defectos de código nuevos. Hay seis observaciones (al final), dos de ellas para el dueño: el aviso que tapa las burbujas «Te faltan» (pregunta 41) y cómo se lee el Remolino.

Las capturas, los vídeos y los logcat están en el portátil, en `Logs/device/tanda2/` (fuera de git).

## Verificación (portátil ARM64)

| Comando | Resultado |
|---|---|
| `dotnet build dotnet/AmazonFill.slnx -warnaserror` | 0 avisos, 0 errores |
| `dotnet test dotnet/AmazonFill.slnx` | 714 superados, 4 omitidos (718) |
| `python tools/validate_schema.py` | 40/40 PASS |
| `pwsh -NoProfile -File scripts/unity-compile.ps1 -TimeoutSec 3600` | Compila (código 0) |
| `unity run … -executeMethod AF.EditorTools.BuildDev.BuildAndroidDev` | `[AF.BuildDev] OK`: `dev.apk` de 62,88 MB, 199 s de build del APK, 1 aviso |
| `adb install -r Builds/Android/dev.apk` | `Success` |
| `pwsh -NoProfile -File scripts/unity-test.ps1 -Mode EditMode` | 1.506 tests: 1.503 pasan, 0 fallan, 3 omitidos (178 s). `WorldBannerArtTests` pasa en ARM64, incluido `BannerArt_OffTheMainThread_MainThreadFitsTheFrame` (presupuesto de 8 ms); `BannerArt_Raster_FitsItsBudget` está entre los omitidos |
| `pwsh -NoProfile -File scripts/unity-test.ps1 -Mode PlayMode -TimeoutSec 3600` | 433 tests: 387 pasan, 0 fallan, 46 omitidos (461 s) |

Unity no reserializó las fuentes TMP durante el build: el árbol quedó limpio.

## Resultados

### 1 · Avisos de 3-7 s, de uno en uno (A2; D-011, punto 2): cumple

- **Regalo de Capi en el 8** (perfil limpio, sin tocar durante la intro):
  - `extra_cuia_gift` sale en la carga (27,84) y `ready` llega a las 29,27;
  - durante la intro, el aviso no está (en la franja se ve la madera);
  - entra con `ready` (+0,07 s) y está entero a +0,21 s;
  - sigue a la vista a +5,12 s, se desvanece a +5,39 s y ya no está a +5,53 s: **≈ 5,45 s** (la hoja pide 5.455 ms).
- **«¡Miquito a la vista!» en el 5** (perfil limpio, primer intento):
  - la tropa reposa hasta +1,03 s y se va entre +1,24 y +1,46 s;
  - a +1,65 s la rama está vacía, y la tarjeta entra a +1,83 s;
  - se desvanece a +6,5 s: **≈ 4,9 s** (la hoja pide 4.960 ms);
  - un toque en una cuia con la tarjeta a la vista da `select 0`: no captura toques.
  - Archivo: `a2_card5_tropa.jpg`.
- **Luciérnaga en el 100** (perfil limpio):
  - `reveal` con `phase: gift_toast`, `stock: 1`;
  - «+1 Luciérnaga» sale después de la tarjeta y de la intro.
- **De uno en uno** (135, perfil limpio, `--ei af_seeds 300 --es af_ads_fill no_fill`):
  - atasco y rescate; sale el aviso «¡Una cuia más! Sigue desde aquí»;
  - 1,2 s después se toca Revelar sin stock ni semillas → `reveal missing`;
  - «Te faltan 120 semillas» **espera a que se vaya el primero** (≈ 3 s), entra sin pisarlo y dura ≈ 3 s;
  - archivo: `q2_l135_hoja.jpg`.
  - Con vídeo disponible, Revelar sin stock ni semillas abre `rv_reveal` en lugar de «Te faltan»: para esta prueba hace falta `af_ads_fill no_fill` (observación 3).
- **Nada encima de la victoria:** sin probar en el teléfono (observación 2). La tarjeta del miquito no vuelve en la pantalla de victoria del 5.

### 2 · Patrones e iconos de fruta (B3; D-012, decisión 4): cumple

- **`AF_STATE load`:**
  - 40 y 7: `"patterns":{"on":false,"opacity":0.15}`;
  - 63, 100 y 123: `"patterns":{"on":true,"opacity":0.2}`.
- **`AF_ART fruit_icons`:** entre 1,0 y 1,9 ms en cada primera carga de sesión (el límite es 30 ms). `AF_ART procedural`: entre 69 y 162 ms.
- **63:** cada dosis visible lleva su icono, centrado y derecho. En un vertido, los iconos de la dosis que queda en el origen giran con la banda inclinada.
- **40:** sin icono y con el patrón sutil.
- **7:** sin iconos en reposo. Al levantar la cuia 0, solo ella muestra los iconos; las demás no cambian.
- **100:** las dosis turbias llevan solo la trama y el «?», sin icono.
- **123 (3 filas):**
  - el Barro va sin icono (oliva con burbujas);
  - los iconos, de ≈ 10 dp, se leen a tamaño real.
- **Para el artista (07 §3):**
  - a tamaño real, la Guanábana (blanca) y el Aguaje (naranja) no se confunden: los separa el color, aunque las dos formas son de fruta redonda con textura;
  - la hoja hundida se lee como hoja o semilla.
  - El icono aparece y desaparece sin fundido al levantar la cuia: si molesta, lo dice el dueño.
- **Archivos:** `b_hoja.jpg`, `b_l123_zoom.png`, `b_l63_zoom.png` y `b_l63_vertido_hoja.jpg`.

### 3 · Sacudida de rechazo (B3; D-012, decisión 3): cumple

Medida en vídeo, fotograma a fotograma (pasos de 12 ms; desplazamiento horizontal a 540 px de ancho, ×2 en el teléfono):

| Toque | `AF_STATE` | Duración | Ciclos | Amplitud |
|---|---|---|---|---|
| Matapi del 63 (cuia 8) | `reject`, `tap 8` | 1,366 → 1,594 s ≈ 230-240 ms | 3 | +11/+12 y −10/−11 px → ±22-24 px (8 dp) |
| Cuia vacía del 63 (3) | `reject`, `tap 3` | 3,044 → 3,284 s ≈ 240 ms | 3 | ±11-12 px → ±22-24 px |
| Hoja cerrada del 43 (1) | `reject`, `tap 1` | 1,452 → 1,680 s ≈ 230 ms | 3 | ±11-12 px → ±22-24 px |

- Las sacudidas van **sin amortiguar** (los picos no decrecen) y vuelven a 0 exacto.
- En el 63, **el embudo del Matapi se ensancha a la vez** y vuelve a su tamaño al terminar (`c_l63_embudo.png`).
- **Un toque válido en plena sacudida funciona:** 200 ms después del rechazo de la cuia vacía, el toque en la 6 da `select 6`, y el de la 3, `pour [6, 3, 1]`.

### 4 · Banner de mundo (A4; D-012, decisión 2): cumple

| Nivel | Entrada | `world_banner` | `world_banner_art` | Cierre |
|---|---|---|---|---|
| 41 | Directa, perfil limpio | `world 3`, «Mundo 3: Manaos · Brasil (Amazonas) · Negro + Solimões», `then: intro` | `async`, `wait_ms` 74, `mascots_ms` 55,1, `main_ms` 0,8, `fallback: none` | `world_banner_closed`, `next: intro`; `ready` 1,1 s después |
| 11 | «Siguiente» tras ganar el 10 | «Mundo 2: Tapajós · Brasil (Pará) · Tapajós», `intro` | `wait_ms` 90, `mascots_ms` 67,5, `main_ms` 0,3, `none` | `next: intro` |
| 81 | «Siguiente» tras ganar el 80 | «Mundo 4: Trapecio · Colombia · Amazonas / Putumayo», `intro` | `wait_ms` 111, `mascots_ms` 88,5, `main_ms` 2,3, `none` | `next: intro` |
| 141 | «Siguiente» tras ganar el 140 | «Mundo 5: Loreto · Perú · Marañón-Ucayali», `intro` | `wait_ms` 66, `mascots_ms` 74,9, `main_ms` 0,3, `none` | `next: intro` |

- **42:** ninguna línea `world_banner`.
- **Solo en el primer intento:** en el 41 no vuelve a salir tras Reiniciar, ni tras `force-stop` y reabrir.
- **`main_ms`:** ≤ 2,3 ms (el límite es 8).
- **`wait_ms`:** 66-111 ms también al entrar con «Siguiente» (observación 4).
- **En pantalla** (`d41_banner.png`): Capi (capibara) presenta con la pata, Rosa asoma, y se ve el encuentro de las aguas con «Brasil (Amazonas) / Negro + Solimões».

### 5 · Tutorial de la Piraña en el 183 (A4; D-012, decisión 1, D): cumple

- **`level_card`:** `kind: debut`, `intro: pirana`, `bite_demo: animated` y `lines: 2`.
- **La demostración:**
  - el chorro entra desde arriba en la cuia de la derecha;
  - el cardumen sale de la cueva (la cuia chica de la izquierda) y se lleva la dosis de la cima a la cueva;
  - dura ≈ 2 s, termina con la tarjeta abierta y no deja anillos quietos.
- **Texto, en 2 líneas:** «Cada cuatro vertidos, la piraña muerde la cuia que acabas de llenar. Sellarla a tiempo evita el mordisco.».
- **Un toque** cierra la tarjeta y llega `ready` tras la intro.
- **Archivo:** `e_l183_hoja.jpg`.

### 6 · Pendientes de la tanda 1

- **Observación 1, `AfterRewarded`: cumple.** El 63 se ganó a 137 s de la carga, con la Cosecha doble. Le siguieron `double_harvest`, `auto_next`, `ad_gate` y `ad_check` con `blocked:AfterRewarded`; no hubo intersticial.
- **Observación 4, la Piraña con la cueva llena:** sigue sin probar en el teléfono.
- **Pregunta 37, T-D hasta ≥ 200 vertidos:**

| Medida | Vertidos | Mediana | p95 | Máximo |
|---|---|---|---|---|
| `save_ms` sin `sync` | 205 | 0,14 ms | 0,27 ms | 1,99 ms |
| `save_ms`, todas | 216 (14 niveles, 11 `sync`) | 0,14 ms | 1,48 ms | 4,83 ms |
| `bg_ms` (hilo de fondo) | — | 3,35 ms | 5,14 ms | 18,87 ms |

  Ninguno pasa de 8 ms en el hilo principal.

## Observaciones

1. **El aviso tapa las burbujas «Te faltan» de la bandeja** (pregunta 41, punto 4). En el 135 y el 31, mientras se ve un aviso, las burbujas «Te faltan 150» de «+1 cuia» y «Te faltan 120» de Revelar quedan debajo, y asoman al irse. Con 3 filas, el aviso tapa además la repisa de abajo y la ficha «?×N» de la cuia turbia, aunque no las cuias (`q_l135_aviso.png` y `q2_l135_hoja.jpg`). **Decide el dueño.**
2. **«Nada encima de la victoria» no se puede forzar en el teléfono.** Ganando lo más rápido posible, la vista tarda 2-3 s en terminar los vertidos encolados, así que los avisos (3-5,5 s) y la tarjeta (≈ 4,9 s) ya se han ido cuando empieza la celebración. Se probó en el 5, con la tarjeta, y en el 31, con el aviso del rescate. Sí se comprobó que no vuelven en la pantalla de victoria. Lo cubre el test PlayMode de `8bb898e`.
3. **Revelar sin stock ni semillas abre el vídeo** (`rv_reveal`) si hay anuncio. «Te faltan {n}» de Revelar solo sale sin vídeo. Para la prueba de cola hace falta `--es af_ads_fill no_fill`: conviene añadirlo a la hoja.
4. **`wait_ms` del banner al entrar con «Siguiente»:** 66-111 ms, cuando la hoja espera ≈ 0 porque el arte se pide antes. Es poco y no se nota, pero sugiere que el arte no se pide al ganar el 10, el 80 y el 140, o que la medida incluye otra cosa.
5. **El Remolino parece un fallo** (lo vio el dueño mirando la pantalla durante las pruebas). En el 140 se tocó como destino la cuia 4 con Remolino, que estaba llena y con otra cima (`[1, 1, 14, 3]`), y el agua fue al fondo de su pareja, la 1 (`recv 1`). Es la regla: 02 §1, línea `si remolino(B)` de la legalidad, y R9: «al fondo de la pareja si B es Remolino».
   - Para descartar un fallo del motor se revisaron los 216 vertidos registrados en las dos tandas, contra el estado anterior: ninguno incumple las reglas; los únicos 4 «raros» son estos del Remolino.
   - **Pregunta para el dueño:** si desde fuera parece que se vierte en una cuia llena, ¿se refuerza la animación? Por ejemplo, que el chorro entre visiblemente en la espiral y la pareja se ilumine al recibir.
6. **Conexión del teléfono** (para quien pruebe en el portátil). adb perdió el teléfono varias veces por tres causas:
   - los tests de Unity (el adb del Editor reinicia el servidor);
   - GlideX (ASUS), que se queda la conexión USB;
   - el almacenamiento del teléfono **al 100 %** (3,4 GB libres de 456 GB), con el servicio adb del teléfono colgado («cannot get serial number (31)») tras ráfagas de capturas de 10 MB.

   **Remedio:**
   - cerrar GlideX;
   - no lanzar tests de Unity con el teléfono en uso;
   - capturas directas al PC (`exec-out screencap -p`) y vídeos de `screenrecord` reducidos, que se bajan y se borran al momento;
   - liberar espacio en el teléfono.

## Defecto encontrado por el dueño: bucle sin «Aguas estancadas» en el 66 (alta)

**Síntoma (dueño, 2026-10-13):** «hay un error, no hay forma de seguir y queda en un loop».

**Reproducción.** En el nivel 66, primer intento, los vertidos `3>6 4>3 7>4 0>5 1>5 2>0 2>1 5>2 1>5 2>1 0>7 1>2` (sacados de logcat; capturas en `Logs/device/loop/`, fuera de git) dejan este tablero:

```
0 [3,8,10]  1 [4,7]  2 [10,7,7,7]  3 sellada  4 sellada
5 (cap 2) [4,4]  6 [3]  7 [8,8,10,10]  8 [3,3,8,1]  9 [1,1,1,4]
```

**Causa:**
- Reproducido con `LevelSession` del núcleo, el estado es `Playing` con dos vertidos legales, `2>1` y `7>0`.
- `AStarSolver` lo da por `Unsolvable` tras expandir solo 4 estados.
- Esos dos vertidos son vaivenes parciales (los tres rosas entre 1 y 2, el azul entre 0 y 7). No son fútiles según 02 R13 y el anexo B, así que `atascado(S)` nunca se cumple y no sale Aguas estancadas.
- El jugador da vueltas sin rescate ni rendición; solo le quedan Deshacer y Reiniciar a mano.
- Es el caso que deja abierta la pregunta 15 (02 R16: el anexo B es falso ya en el juego base, CHANGELOG del núcleo C1-2). Ya pasa en un nivel sin mecánicas activas.

**Magic Sort** (ficha §2.8) solo declara el bloqueo sin vertidos legales; si detecta bucles: no observado.

**Para decidir (dueño)** y luego **arreglar (core-dev y unity-gameplay-dev):**
- **Opción recomendada:** declarar Aguas estancadas cuando ningún vertido útil lleva a un estado nuevo, es decir, cuando el cierre de estados alcanzables con los vertidos útiles es pequeño y no contiene victoria. Se comprueba con una búsqueda acotada (p. ej., ≤ 64 estados, sin asignaciones) tras cada vertido.
- **Alternativa:** que R13 use el solver con un presupuesto pequeño.
- **Tests:** este tablero del 66 como caso de regresión (EditMode y núcleo) y P1 (R14) intacta.

> **Nota del 2026-10-13 (revisión de T50; D-014).** El 66 no es un nivel sin mecánicas. Tiene capacidades mixtas, con una Cuia chica de 2 en la 5, y una Hoja en la 6, que abre el primer vertido (`3>6`). Con capacidades mixtas, la opción recomendada declararía el bloqueo ya tras el vertido 6 (`2>0`), cuando el tablero todavía se gana pasando por un fútil (`6>5`, cuando `5>2` vacía la Cuia chica; tras el vertido 8, una línea ganadora empieza por él), y eso choca con «P1 (R14) intacta». El tablero solo es irresoluble desde el vertido 9 (`1>5`). Va al dueño como pregunta 46 de `docs/GDD.md` §6.3.
