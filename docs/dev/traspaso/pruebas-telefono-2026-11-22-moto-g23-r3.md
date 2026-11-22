# Prueba en el Moto G23, tercera ronda (2026-11-22)

Tercera medida de memoria y rendimiento en el Moto G23 (clase A, 08 §5.4; 3,7 GB, Mali-G52 MC2), según `encargo-spark-moto-g23-r3.md` y `docs/dev/memoria.md` §11.4. Mide los dos cambios del añadido de D-060 del 2026-11-17: IL2CPP «Faster (smaller) builds» y el lector JSON con menos basura. No la hace Spark: la hace el escritorio, con el dueño delante.

- **Código:** `integracion-fase1-fase2` en el commit `70990fd6`, que incluye `6b3c7493`.
- **Conexión:** por Wi‑Fi (`adb connect 192.168.1.16:42765`), siempre con `-s`. El S23 Ultra seguía por cable para otra sesión y no se tocó.
- **Registros:** capturas, logcat y volcados en `Logs/device/g23r3/` (fuera de git).

## Builds

- **De desarrollo:** el `dev.apk` de `main` (`70990fd6`) ya compilado en `Builds/Android/dev.apk`.
- **Ensayo de release:**
  - compilado con `scripts/unity-build.ps1 -Method Release -Rehearsal -Apk -Architecture x86_64` en el worktree `o7-core`, en `70990fd6` con la cabeza separada; el log da `[AF.BuildRelease] OK`;
  - `rehearsal.apk` de **52,2 MB** (la r2 tenía 58,2), con un `libil2cpp.so` de 32,3 MB sin comprimir;
  - instalado con `-r` sobre el `dev.apk` y compilado con `cmd package compile -m speed-profile -f`; `dexopt` da `status=verify`.
- **Perfil nuevo:**
  - el ensayo de la r2 estaba firmado con la clave de Spark, así que no admitía el `dev.apk` de este equipo, y hubo que hacer `adb uninstall --user 0`;
  - el perfil se rehízo con `--ei af_max_level 66`, «18+» y «Rechazar todo»;
  - el ensayo comparte la clave de depuración de este equipo con el `dev.apk`, así que el `-r` conservó el perfil;
  - el perfil queda en el 67, con 3/5 canoas y 0 semillas;
  - **el 67 sale con 10 cuias en 2 filas** (en la r2, 3 filas).

## Resumen

| Punto | Resultado |
|---|---|
| **PSS del ensayo (D-060)** | **447,4** en el inicio recién abierto; **474,4-480,6** desde el Mapa. Ver la tabla |
| `Code` del ensayo | 76,4 MB en el inicio y el Mapa (r2: 101-103) y 98,1 tras el Mapa |
| `mono_heap_mib` (dev) | **45,8-50,0** en el arranque y el Mapa (r2: 73,1); **77,9** tras abrir el Reto |
| El 340 en juego automático | ganado en 27 vertidos; 59,5-60,2 fps, p95 16,80-17,08 ms |
| El 386 en juego automático | ganado en 23 vertidos; 59,5-60,2 fps, p95 16,86-17,01 ms; máximos de 66,6 y 99,2 ms en dos ventanas |
| Morpho (37 tableros, 3 pesados) | **factor 8,56** (primera ronda: 8,08); máximo 96,3 ms, sin cortes |
| Arranque (dev, segunda apertura) | `splash_ms` 6.368 y 6.154 (r2: 6.029-6.074); `levels_load` 292-314 ms |
| Reto (dev) | `open_ms` 713, con `pool_read` true |

## Memoria del ensayo de release (D-060)

`dumpsys meminfo -d` tras un minuto quieto en cada sitio (`rel-1-inicio.txt` … `rel-4-nivel67.txt`). Capturas: de `13-rel-inicio.png` a `16-rel-nivel67.png`. Cifras en MB.

| Momento | PSS | r2 | Δ | `Code` | `.so mmap` | `.apk mmap` | `Graphics` | `Native Heap` | `Unknown` |
|---|---|---|---|---|---|---|---|---|---|
| Inicio, recién abierto | **447,4** | 469,9 | −22,5 | 76,4 | 60,1 | 56,5 | 74,5 | 25,7 | 177,2 |
| Mapa, vuelta 1 | **474,4** | 479,2 | −4,8 | 76,4 | 60,1 | 56,5 | 83,6 | 26,2 | 192,1 |
| Inicio, vuelta 1 | **475,4** | 472,4 | +3,0 | 98,1 | 54,1 | 70,0 | 79,7 | 26,1 | 192,1 |
| Nivel 67 | **480,6** | 430,1 | (+50,5) | 98,1 | 54,1 | 69,9 | 81,7 | 26,7 | 194,6 |

- **El nivel 67 no es comparable con la r2:**
  - la r2 lo midió tras tres vueltas al Mapa, cuando Android ya había soltado el código (`Code` 47);
  - aquí se midió tras una vuelta, con `Code` en 98.
- **«Smaller» se nota al principio:**
  - `Code` baja de 102,6 a 76,4 MB en el inicio;
  - `.so mmap`, donde está `libil2cpp.so`, queda en 54-60 MB.
- **Tras el Mapa, `Code` vuelve a 98 MB, pero no por el `.so`:**
  - lo que crece es `.apk mmap` (de 56,5 a 70,0 MB): los datos de Unity que se leen mapeados del APK;
  - el `.so mmap` baja a 54,1.
- **`Unknown` es ahora la partida más grande** (la memoria nativa de Unity, privada y sucia):
  - 177,2 MB en el inicio;
  - sube 15 MB al abrir el Mapa y no vuelve a bajar.
- **`Graphics`** queda en 74,5-83,6 MB, como en la r2 (75-83).
- **Swap:** casi nada (0,3-0,5 MB de `SWAP PSS`), así que no había presión de memoria.
- **Arranque del ensayo con `am start -W`, en frío:** `TotalTime` 758 ms en la primera apertura tras instalar y 299 ms tras un `force-stop`. La r2 midió 654 ms en caliente.

## El heap gestionado (dev)

Las líneas `AF_PERF mem` están en `dev-open*.log` y `dev-mapa-reto.log`.

| Momento | `mono_heap_mib` | `peak_mono_heap_mib` | `sys_mib` |
|---|---|---|---|
| Arranque, primera apertura (edad y consentimiento) | 71,1 | 71,1 | 570,4 |
| Arranque, segunda apertura | 50,0 | 50,0 | 549,7 |
| Arranque, tercera apertura | 45,8 | 45,8 | 543,7 |
| Inicio (tras salir del 67) | 45,8 | 45,8 | 511,8 |
| Mapa (viaje de Capi) | 45,8 | 45,8 | 483,6 |
| Reto del río, abierto | **77,9** | 77,9 | 479,4 |

- **`heap_steps_mib`** de la línea `boot`, en el orden de `boot_steps`: `profile`, `catalog`, `services`, `sloth`, `ads`, `game`, `screens` y `systems`.
  - tercera apertura: `[12,7, 17,1, 34,3, 34,3, 34,3, 34,3, 45,8, 45,8]`;
  - segunda apertura: `[12,8, 17,3, 34,0, 49,8, 50,0, 50,0, 50,0, 50,0]`.
- **El bloque `catalog` sube solo 4,4-4,5 MiB:** el lector nuevo funciona.
  - Ahora los saltos están en `services` (+17 MiB, los lienzos del arte por código) y en `sloth` o `screens` (+11-16 MiB, según la apertura).
  - El `GC.Collect()` tras el catálogo de §11.3 ya no hace falta: el heap queda por debajo de 60 sin él.
- **El Reto sube el heap a 77,9 MiB al leer el pool** (`pool_read` true, `open_ms` 713). Con IL2CPP ese heap no se devuelve: después del Reto, la sesión queda por encima de los 73,1 de la r2.

## Fotogramas (dev, juego automático con el bench)

Juego automático con `--ei af_bench_morpho 1 --ei af_level N --ez af_capture_autoplay true`. Las cifras son de las ventanas `AF_PERF frames` de 600 fotogramas con `morpho_running` false (`dev-340-bench.log`, `dev-386-bench.log`).

- **El 340** (3 filas, 13 cuias, «Muy difícil»):
  - la tarjeta del nivel se cierra sola (`card_dismissed`, `read_ms` 2.506) y se gana en 27 vertidos, también sin el bench (`dev-340.log`);
  - 13 ventanas: 59,5-60,2 fps y p95 de 16,80-17,08 ms;
  - máximos de 17-34 ms, salvo una ventana de 116,8 ms, la de la victoria.
- **El 386** (3 filas, 11 cuias, Aguas turbias):
  - se gana en 23 vertidos;
  - 14 ventanas: 59,5-60,2 fps y p95 de 16,86-17,01 ms;
  - máximos de 17,9-33,7 ms, salvo dos: 66,6 ms y 99,2 ms (la de la victoria);
  - no hay parones largos.
- **Frente a la r2:**
  - el p95 sube 0,1-0,2 ms como mucho, lejos del umbral de 1 ms;
  - el máximo del 386 (99,2 ms) sale en la ventana de la victoria; la r2 dio 65,9.

## Morpho (dev)

Las dos muestras de la Tinaja se metieron con `adb push`, como en la primera ronda, en `files/af_bench/` (`pack3_sample.json` y `sample_tinaja/mundo_07_v1.json`). Lanzado con `--ei af_bench_morpho 1 --ei af_level 106` (`dev-morpho.log`).

- **`morpho_summary`:**
  - 37 tableros, 3 pesados, **116,76 nodos/ms**;
  - **factor 8,56**, frente a 8,08 y 123,7 nodos/ms en la primera ronda (2026-11-15);
  - máximo 96,3 ms, 0 cortados a 150 ms.
- **Los pesados:**

  | Nivel | Nodos | Tiempo |
  |---|---|---|
  | 329 | 8.298 | 73,4 ms |
  | 332 | 10.356 | 80,9 ms |
  | 340 | 10.606 | 96,3 ms (primera ronda: 83,3) |

- **Lectura:**
  - Morpho va un 6 % más lento por nodo, en línea con la indirección de «smaller»;
  - el peor caso sigue con un tercio de margen sobre el corte de 150 ms;
  - `MorphoDeviceFactor` (8× [P]) queda corto por poco.
- **No se pudo comparar en el mismo teléfono con el `dev.apk` de la r2:** el Moto tenía instalado el ensayo de Spark y este equipo no tiene ese APK. La referencia es la primera ronda.
- **Sin las muestras**, el bench mide 29 tableros y ninguno pesado (factor 8,46-8,58), y no se puede comparar.

## Arranque (dev)

| Apertura | `splash_ms` | `load_ms` | `levels_load` |
|---|---|---|---|
| Primera, tras instalar (con la intro de la marca) | 9.299 | 7.761 | 314 ms |
| Segunda | 6.368 | 6.368 | 296 ms |
| Tercera | 6.154 | 6.154 | 292 ms |

- **Frente a la r2:** la segunda apertura queda igual o 100-300 ms más lenta (r2: 6.029-6.074).
- **El bloque `catalog`** acaba a los 4.116-4.171 ms. Con 700 niveles en 9 paquetes, `levels_load` tarda 292-314 ms, y no hay cifra de la r2 en el teléfono con la que compararlo.

## Observaciones

1. **Baja (entorno, no del juego). El servidor de adb se reinició varias veces durante la prueba**, seguramente por la sesión del S23. Cada reinicio cortaba la conexión Wi‑Fi con el Moto, que se volvía a conectar con `adb connect`. No afectó a las medidas: cada volcado se tomó con el teléfono conectado.
2. **Informativa. Al abrir, el ensayo entró en el nivel 106 del bench anterior**: es lo que hace `af_level` en el `dev.apk`. Salir de ese nivel y del 67 costó una canoa cada vez, como está previsto.

## Sin probar

- las vueltas 2-5 al Mapa con el ensayo (cuándo suelta Android el código), que en la r2 bajaban a 414-435 MB;
- el `mono_heap_mib` en el inicio después del Reto;
- el Morpho del `dev.apk` de la r2 en el mismo teléfono;
- la Marea en el S23 Ultra (punto 7 del encargo, opcional), porque el S23 lo usaba otra sesión.

## Estado del teléfono

- queda instalado el ensayo de release de `70990fd6`, con el perfil en el 67;
- la pantalla vuelve a apagarse a los 10 minutos, como antes de la prueba;
- se borraron las muestras de `af_bench`;
- la depuración inalámbrica sigue activa.

## Lectura para D-060 (decide el dueño)

**El ensayo solo cumple en el inicio recién abierto: 447,4 MB, 2,6 MB por debajo del tope.** Desde la primera apertura del Mapa se pasa del tope de 450 MB por **24-31 MB** (474,4 / 475,4 / 480,6).

- **Lo que bajó y lo que no:**
  - «smaller» quitó unos 26 MB de `Code` y el lector nuevo, unos 27 MiB de heap gestionado;
  - pero el Mapa sube 15 MB la memoria nativa de Unity, y los datos mapeados del APK suben 13 MB, y nada de eso vuelve a bajar.
- **Las dos salidas:**
  - **subir el presupuesto de clase A a unos 485 MB** cubre el peor caso medido (480,6);
  - **mantener los 450 MB** exige recortar unos 31 MB de la memoria nativa del Mapa o de los datos mapeados del APK, porque ya no viene del `libil2cpp`.
- **Respuesta del dueño (2026-11-22, a la vista de esta hoja):** «deja asi la memoria». No se hacen más recortes para la 1.1. El cambio de D-060 en `decisiones.md`, con el tope nuevo, queda para el integrador.
