# Prueba en un teléfono de gama baja: Moto G23 (2026-11-15)

Spark compila `integracion-fase1-fase2` en el commit `f722ae08` y lo prueba en un Motorola moto g23. Es el primer teléfono real de clase A (08 §5.4: Mali-G52 o Adreno 610). Capturas y logcat en `Logs/device/motog23/` (fuera de git).

## El teléfono

- **Modelo y sistema:** moto g23 (`penangf`), Android 13, `arm64-v8a`.
- **Memoria:** 3,7 GB de RAM.
- **Gráficos:** Mali-G52 MC2 con OpenGL ES 3.2 (`OpenGLES3`).
- **Pantalla:** 720 × 1600, densidad 306 (280 de fábrica), y el juego renderiza a 720 × 1600.
- **Conexión:** primero por USB y luego por Wi-Fi (`adb tcpip 5555`), porque el cable fallaba.
- **Instalación:** `dev.apk` de 75,5 MB (`scripts/unity-build.ps1 -Method Dev` da `[AF.BuildDev] OK`), instalado con `adb install -r`. El perfil es nuevo: el dueño contesta 18+ y el consentimiento.

## Resumen

| Punto | Resultado |
|---|---|
| Arranque en frío, primera vez | `boot` `splash_ms` 10.116 y `load_ms` 8.747, con `services` a los 6.463 ms. La intro va fluida: `frame_p95_ms` 20,0, `frame_max_ms` 34,2, sin pausas |
| Tutorial, niveles 1-10 (jugado por el dueño) | sin cierres, ANR ni excepciones. El dueño: «pinta bien» |
| Mapa (vista Cuenca, mundo 2) | `map_frames`: 59,3 fps, p95 16,9 ms, máximo 100 ms; `map_load` 28 ms |
| 424 (2 filas, 9 cuias), juego automático | ganado en 17 vertidos; 60 fps, p95 ≤ 16,9 ms; un máximo aislado de 99 ms |
| 386 (3 filas, 11 cuias), juego automático | ganado en 23 vertidos; 60 fps, p95 ≤ 17,0 ms; picos aislados de 82-147 ms; dos parones (observación 2) |
| 340 (3 filas, 13 cuias, Tinaja y Aguas turbias, «Muy difícil») | el automático no empieza (observación 3). Con el panel delante: 46 ventanas de 600 fotogramas, todas a 59,7-60,8 fps |
| Morpho (`af_bench_morpho`, 37 tableros) | **factor 8,08** frente a los 8 supuestos (`MorphoDeviceFactor`); 123,7 nodos/ms; máximo 83,3 ms (el 340, 10.606 nodos); ninguno cortado a 150 ms |
| Memoria | por encima del presupuesto de 08 §5.4 (observación 1) |

## Morpho

Es la medida que pide `DevBench` para sustituir el 8× [P] de `MorphoDeviceFactor`: solo vale la de un teléfono de clase A, y esta lo es.

- **La línea:** `morpho_summary` da `levels` 37, `heavy` 3, `nodes_per_ms` 123,74, `factor` 8,08, `max_ms` 83,33, `cancelled` 0, `gpu` Mali-G52 MC2.
- **Las consultas pesadas:**

  | Reto | Nodos | Tiempo |
  |---|---|---|
  | 329 | 8.298 | 72,7 ms |
  | 332 | 10.356 | 80,4 ms |
  | 340 | 10.606 | 83,3 ms |

- **Lo que significa:** el supuesto se sostiene, con margen frente a los 150 ms de `hint_timeout_ms`.

## Fotogramas (shader del líquido en tableros de 3 filas)

- **Cómo se midió:** las ventanas `AF_PERF frames` con `morpho_running` `false`.
- **Resultado:**
  - la media va de 16,4 a 16,8 ms y el p95 de 16,7 a 17,2 ms;
  - los fotogramas de más de 33 ms son de 0 a 15 por cada 600.
- **Lectura frente al GDD:** cumple los 60 fps estables de 08 §5.4. El tiempo de GPU (≤ 10 ms) no se midió por separado.

## Observaciones

1. **Alta. La memoria pasa el presupuesto de 08 §5.4 (D-060), con el `dev.apk`.**
   - **TOTAL PSS:**
     - 463,8 MB en la pantalla de la edad;
     - 519,5 MB en el 340, con 256 MB de swap PSS, porque Android ya comprime;
     - el presupuesto es ≤ 450 MB.
   - **Heap gestionado:** `peak_mono_heap_mib` 71,2 tras el tutorial y el Mapa, frente a ≤ 60.
   - **Texturas:** cumplen, de 35 a 50 MB frente a 120.
   - **Pico de residente (`peak_sys_mib`):** 546 MB.
   - **Matiz:** según `docs/dev/memoria.md`, el `dev.apk` pesa más que el release (en el SM-S938B, 605-620 MB frente a 545 MB del ensayo), y D-060 se juzga con el ensayo. Por eso se midió también el ensayo (sección siguiente): **sigue por encima**.
   - **Cierres por memoria:** `lowmemorykiller` no cerró el juego en ningún momento.
2. **Media, por confirmar. Dos parones en el 386:** fotogramas de 81,5 s y de 30,4 s.
   - Coinciden con `audio_focus` `none` (t 61.350 → 142.627 y 160.243 → 187.783): otra app tomó el foco, así que parece que el juego pasó a segundo plano y no que se colgara.
   - La prueba corrió justo después de pasar del cable a la Wi-Fi, cuando el dueño pudo desenchufar o tocar el teléfono. Si nadie lo tocó, hay que investigarlo.
3. **Media (modo de captura, E1). `af_capture_autoplay` no cierra el panel de dificultad.**
   - En el 340 («Muy difícil», «Noche de jaguar») el juego automático espera tras el panel y no vierte nada en 7 min.
   - No afecta al jugador, pero impide medir el juego y grabar piezas de marketing en los niveles con ese panel.
   - De paso: `af_capture_start_ms` mayor de 10.000 se recorta al tope (`AF_CAPTURE error`); es lo esperado.
4. **Baja. La primera apertura tarda unos 10 s hasta la pantalla de la edad.** Los servicios se llevan hasta los 6,5 s. Falta medir la segunda apertura.

## La memoria con el ensayo de release

- **El build:** `scripts/unity-build.ps1 -Method Release -Rehearsal -Apk` da `[AF.BuildRelease] OK`, con un `rehearsal.apk` de 58,2 MB.
- **La instalación:** con `adb install -r` sobre el `dev.apk`, conservando el perfil. Arranca en el nivel 106 (3 filas, 11 cuias), donde lo dejó el bench.
- **El arranque en caliente:** `am start -W` da `TotalTime` 573 ms.
- **Cómo se midió:** el ensayo no tiene la sonda, así que la medida es `dumpsys meminfo` tras un minuto quieto en cada sitio. Los volcados están en `Logs/device/motog23/rel-*-meminfo.txt`.

| Momento | TOTAL PSS | Code | Graphics | Native Heap |
|---|---|---|---|---|
| Nivel 106, al arrancar | **486,4 MB** | 114,3 | 84,9 | 29,5 |
| Mapa del río | **516,2 MB** | 115,4 | 101,0 | — |
| Nivel 106, tras volver del Mapa | **508,2 MB** | 109,4 | 101,0 | 26,2 |
| Nivel 106, un minuto más | **484,4 MB** | 100,4 | 102,0 | 22,9 |

- **Por encima del presupuesto en todos los momentos:** de 34 a 66 MB sobre los ≤ 450 MB de 08 §5.4 (D-060).
  - En el Samsung, el ensayo dio 545 MB en la jornada 6. Aquí sale algo menos, pero no basta.
  - En el nivel 75, también en el Samsung, dio 530 MB en E0 y 494 MB en E1 (`memoria.md`).
- **Lo que más pesa:**
  - **`Code`** (100-115 MB): según `memoria.md`, varía ±20 MB con el estado de compilación de ART, y aquí el APK estaba recién instalado.
  - **`Graphics`** (85-102 MB): sube con el Mapa y no baja al volver al nivel.
- **Decisión pendiente del dueño:** subir el presupuesto de clase A o atacar la memoria. Una opción ya medida es E1 de `memoria.md`, −36 MB de `GL mtrack`. La segunda medida con ART ya compilado (tras un día de uso o con `cmd package compile -m speed-profile`) diría cuánto de `Code` es transitorio.
- **Sin medir:** la pantalla de inicio (la orilla). El toque de «Volver a la orilla» no entró y la última medida se tomó en el nivel.

## Sin probar

- el 340 jugado a mano;
- el viaje de Capi (con zoom bajo y con zoom alto, el de la observación 1 de la segunda ronda de la 1.1);
- la segunda apertura en frío;
- el ensayo con ART compilado y en la orilla;
- el tiempo de GPU por separado.
