# Prueba de la 1.1 en el teléfono, ronda r2c (2026-11-16, tarde)

Spark comprueba en el teléfono los arreglos de la prueba r2b (añadido de D-069 del 2026-11-16, ramas `v11/mapa-r2b-arreglos` y `v11/memoria-mapa-heap`), sobre `integracion-fase1-fase2` en el commit `28b383d6`.

- **Build:** `dev.apk` de `scripts/unity-build.ps1 -Method Dev`, con `[AF.BuildDev] OK`.
- **Teléfono:** SM-S918B (S23 Ultra) por cable, con perfil nuevo en cada bloque (`pm clear`, 18+ y «Rechazar todo»).
- **Registros:** capturas y logcat en `Logs/device/v11r2c/` (fuera de git).

## Resumen

| Punto de «Por mirar en el teléfono» | Resultado | Obs. |
|---|---|---|
| El compañero, ruta A: el Mapa del 10 que se abre solo desde el inicio (tucuxi, 51) | **cumple** | — |
| El compañero, ruta B: el Mapa del 10 cerrado antes de verlo (tucuxi) | **cumple** | — |
| El compañero, ruta C: el Mapa desde la pausa (tucán, 21) | **cumple** | — |
| El Mapa de Bolivia y las placas | **cumple**, con un solape | 1 |
| La memoria con cinco vueltas | **cumple** | — |
| `AF_LAYOUT logbook` | **cumple** | — |

## El compañero que se une

- **Ruta A** (`pm clear`; se gana el 51 en juego automático; «Inicio»; «Seguir» en la Bitácora):
  - el Mapa abre en Cuenca (`source` l10), hace la llegada a la parada 5 y pasa a Mundo;
  - presenta al tucuxi en primer plano, con «¡Se une al viaje! · Tucuxi»;
  - el tucuxi pequeño se distingue junto a Rosa;
  - durante el primer plano no se ven rótulos ni placas;
  - capturas en `hoja-tucuxi-A.jpg`.
- **Ruta B** (lo mismo, pero cerrando el Mapa del 10 a los 2,9 s):
  - `map_close` da `map_seen_level` 50;
  - al volver a abrir el Mapa desde el inicio (`source` lobby, vista Mundo), presenta al tucuxi;
  - capturas en `hoja-tucuxi-C.jpg`.
- **Ruta C** (`pm clear`, `af_max_level 21`; el juego entra en el 22; pausa › «Mapa»):
  - presenta al tucán con «¡Se une al viaje! · Tucán» y el ♥ de Capi;
  - ya no hay puntos de la ruta encima del tucán (observación 2 de la r2);
  - capturas en `hoja-tucan.jpg`.
- **Nota:** en estas aperturas no sale ninguna línea `crew_joined`. Sin `map_journey`, la sonda no tiene dónde escribirla. Se juzga por las capturas.

## El Mapa más limpio

Capturas en `hoja-a.jpg` a `hoja-f.jpg` y `33-mundo-521-manual.png`. Con `af_max_level` de un salto, el Mapa abre ahora en Cuenca con el viaje del 10; la vista Mundo se pidió con su botón.

- **Una sola placa de mundo:** en Cuenca solo sale la del mundo siguiente («Mundo 5 · Loreto · Nivel 141» en el 132, «Mundo 9 · Ucayali · Nivel 561» en Bolivia). Antes eran hasta seis apiladas.
- **Banderas:** como mucho tres en el mástil (441-521).
- **Boyas y rótulos:**
  - en el 71 y el 79 ya no hay boyas encima de «Leticia · Premio del mundo · faltan N»;
  - Lago de Tefé, Coari y Codajás salen limpios.
- **Bolivia (521):** ya no salen las paradas futuras con su cestita y su línea. Siguen las muestras ganadas, con sus líneas: el mapa aún va cargado, pero se lee.
- **Placa en el borde:** la del mundo siguiente queda medio cortada en el borde izquierdo («…ndo 4 · Trapecio · Nivel 81», en el 71 y el 79).

## Memoria del Mapa v2, cinco vueltas (S23)

- **Cómo se midió:** `pm clear` con `af_mem_os 1` y `af_max_level 66`. Como en la r2b, desde el nivel 67 con la pausa › «Mapa» y la flecha atrás, y un minuto quieto antes de cada `dumpsys meminfo -d`.

| Vuelta | PSS (MB) | `GL mtrack` (MB) | `Native Heap` (MB) |
|---|---|---|---|
| 0 (base, nivel 67) | 580,3 | 39,7 | 33,1 |
| 1 | 610,4 | 39,7 | 32,6 |
| 2 | 610,1 | 39,7 | 32,6 |
| 3 | 610,0 | 39,7 | 32,6 |
| 4 | 610,7 | 39,7 | 32,7 |
| 5 | 609,1 | 39,7 | 32,6 |

- **Las vueltas 2 a 5** quedan a ±1,6 MB de la 1: **cumple el criterio de ±3 MB**.
- **El heap gestionado:** `mono_heap_mib` se queda en 70,6 en las cinco vueltas. En la r2b subía de 59,8 a 78,3; ya no crece.
- **`map_after`:** `map_tex_kib`, `map_renderers` y `map_verts` a 0, con los mismos `obj_tex2d` (185), `obj_mesh` (547) y `os_graphics_mib` (129,8) en las cinco vueltas.
- **El salto de la primera apertura** (+30 MB) incluye la reserva de 8 MiB que acepta el [P] del integrador.

## Observaciones

1. **Baja. En el 521, en la vista Mundo, el rótulo «Trinidad · faltan N niveles» queda debajo de la ventanita de casa:** se lee «Trinidad … iveles» (`33-mundo-521-manual.png`). La píldora del Premio ya la esquiva; el rótulo de la parada siguiente, no.
2. **Baja. El primer toque en «Mundo» justo al acabar el viaje del 10 no entra.** Con un segundo de margen tras `map_play_ready`, en el 132-521 el Mapa siguió en Cuenca. Más tarde, el mismo toque sí cambió de vista. Puede ser la ficha del perezoso que tapa el Mapa al empezar; conviene mirarlo, porque el jugador también lo pulsará pronto.
3. **Aviso de la prueba.** En una captura del 441 salió otra app del teléfono delante del juego (se borró). El juego volvió al frente sin más; no parece cosa del juego.

## Sin probar en esta ronda

- los tres audios (el dueño);
- el miquito, la llegada, el cambio de mundo en el 40 y Reducir movimiento;
- el marco de país completo;
- Play Games;
- el Moto G23 (bloque 4 del encargo r2b).
