# Prueba en el Moto G23, segunda ronda (2026-11-17)

Spark completa en el Moto G23 (clase A, 08 §5.4) el bloque 4 del encargo r2b: lo que quedó sin probar en la primera ronda (`pruebas-telefono-2026-11-15-moto-g23.md`).

- **Código:** `integracion-fase1-fase2` en el commit `455a05f1`, con los arreglos del Mapa de las rondas r2b a r2e y el del juego automático con la tarjeta del nivel.
- **Conexión:** por cable USB, con el teléfono cargando.
- **Registros:** capturas y logcat en `Logs/device/g23r2/` (fuera de git).

## Builds

- **De desarrollo:** `dev.apk` de `scripts/unity-build.ps1 -Method Dev` (`[AF.BuildDev] OK`), instalado con `-r` sobre el perfil de la primera ronda.
- **Ensayo de release:** `rehearsal.apk` de 58,2 MB, de `-Method Release -Rehearsal -Apk` (`[AF.BuildRelease] OK`). Instalado con `-r` y compilado con `cmd package compile -m speed-profile -f`; `dexopt` da `status=verify`.

## Resumen

| Punto | Resultado |
|---|---|
| Segunda apertura (dev, proceso cerrado con `force-stop`) | `splash_ms` 6.029 y 6.074 (la primera tras instalar, 7.872). En la primera ronda, la primera apertura del todo tardaba 10.116 |
| El 340 en juego automático (observación 3 de la primera ronda) | **cumple**: `card_dismissed` (`level_card`, `read_ms` 2.505) y ganado en 27 vertidos; 59,3-60,7 fps, p95 16,8-16,9 ms |
| Los parones del 386 (observación 2), sin tocar el teléfono | **no se repiten**: ganado en 23 vertidos; 59,1-60,7 fps, máximo 65,9 ms; `audio_focus` siempre `gain` |
| Memoria del Mapa v2, cinco vueltas (dev) | **estable**: vueltas 3-5 a ±1,1 MB; `mono_heap_mib` fijo en 73,1; `map_after` a 0 |
| Memoria del ensayo de release (D-060) | 470-479 MB con todo en RAM; 414-434 MB cuando Android libera el código. Ver la tabla |
| Arranque del ensayo en caliente | `am start -W`: `TotalTime` 654 ms |

## Memoria del Mapa v2 (dev, desde el nivel 67)

| Vuelta | PSS (MB) | `Code` | `Graphics` | `Native Heap` |
|---|---|---|---|---|
| 0 (base) | 548,2 | 147,7 | 74,8 | 34,0 |
| 1 | 531,2 | 81,2 | 84,7 | 15,5 |
| 2 | 520,7 | 64,1 | 83,2 | 18,1 |
| 3 | 491,1 | 43,1 | 83,2 | 18,2 |
| 4 | 491,7 | 43,9 | 83,2 | 18,3 |
| 5 | 492,3 | 43,8 | 83,2 | 18,4 |

- **Por qué baja al principio:** con 3,7 GB, Android va soltando el código mapeado (`Code` de 148 a 43 MB). Desde la vuelta 3 el PSS queda fijo.
- **`Graphics`** sube unos 8-10 MB con la primera apertura y no vuelve a crecer.
- **`map_after`:** `map_tex_kib`, `map_renderers` y `map_verts` a 0 en las cinco vueltas, con `obj_tex2d` 183 y `os_graphics_mib` 81,2.

## Memoria del ensayo de release (D-060)

`dumpsys meminfo -d` tras un minuto quieto en cada sitio (`rel-*.txt`). Capturas de cada paso en `hoja-rel.jpg`. El perfil está en el 67, con 3 filas en el nivel.

| Momento | PSS (MB) | `Code` | `Graphics` | `Native Heap` |
|---|---|---|---|---|
| Inicio, recién abierto | **469,9** | 102,6 | 75,4 | 29,9 |
| Mapa, vuelta 1 | **479,2** | 101,4 | 82,4 | 30,0 |
| Inicio, vuelta 1 | **472,4** | 98,3 | 79,7 | 21,8 |
| Mapa, vuelta 2 | 422,0 | 52,0 | 80,0 | 16,0 |
| Inicio, vuelta 2 | 434,8 | 68,3 | 80,0 | 16,1 |
| Mapa, vuelta 3 | 433,5 | 65,4 | 80,0 | 16,2 |
| Inicio, vuelta 3 | 413,7 | 42,8 | 80,0 | 15,7 |
| Nivel 67 | 430,1 | 47,0 | 80,9 | 17,2 |

- **Frente al presupuesto de clase A (≤ 450 MB):** con todo en memoria (inicio y primera vuelta) se pasa por 20-29 MB. Cuando Android suelta el código, queda por debajo (414-435).
- **Frente a la primera ronda** (2026-11-15: ensayo sin compilar, nivel 106, Mapa sin arreglos), que daba 484-516 MB: el Mapa baja de 516 a 479 y el nivel, de 484-508 a 430-472.
- **`Graphics`** queda en 75-83 MB, frente a los 85-102 de entonces.
- **Lectura para D-060 (decide el dueño):**
  - el exceso que queda es casi todo `Code` (el `libil2cpp` mapeado), que Android recupera bajo presión;
  - no hubo cierres por memoria;
  - subir el presupuesto de clase A a ~480 MB cubriría el peor caso medido; dejarlo en 450 exige recortar unos 30 MB.

## Observaciones

Ninguna nueva. Las observaciones 2 y 3 de la primera ronda quedan cerradas.

## Sin probar

- el 340 jugado a mano (con el juego automático ya cierra la tarjeta y se gana);
- el tiempo de GPU por separado;
- el viaje de Capi en el Moto.

## Estado del teléfono

- queda instalado el ensayo de release;
- la pantalla vuelve a apagarse a los 10 minutos y No molestar está quitado, como estaba antes de la prueba.
