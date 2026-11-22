# Prueba de la 1.1 en el teléfono, segunda ronda (2026-11-09, a medias)

El portátil sigue `guion-prueba-telefono-1.1-r2.md` en `integracion-fase1-fase2`, en el commit `f3a4252c`, con el SM-S938B por Wi-Fi. Capturas y logcat en `Logs/device/v11r2/` (fuera de git).

**Estado:** a medias. Se hicieron el bloque 0, el punto 1 (el viaje) y el 2.1 (el tucán). El 2.2, el tucuxi, quedó con el 51 ganado, a la espera de que el dueño abriera el Mapa. Del 2.3 en adelante, y los bloques 3 a 7, quedan pendientes.

## Resumen

| Punto | Estado | Observaciones |
|---|---|---|
| 0. Preparación y build | cumple | — |
| 1. El viaje de Capi | el tiempo cumple; con el zoom alto se ve demasiado rápido | 1 |
| 2.1 El tucán con primer plano | el remo se ve; los puntos de la ruta tapan al tucán | 2 |
| 2.2 El tucuxi | a medias | — |
| 2.3-2.6, 3-7 | pendientes | — |
| El río (el dueño, de pasada) | detalles toscos y uniones de los brazos marcadas | 3 |

## 0. Preparación

- En `git log` están las cuatro integraciones que pide el guion: `e5e7257d` (`v11/mapa-j1-memoria`), `a24d5aa6` (`v11/mapa-j1-arreglos`), `6409bf05` (`v11/miquitos-segundo-gesto`) y `194aadcc` (`v11/marco-de-pais`).
- `scripts/unity-build.ps1 -Method Dev` da `[AF.BuildDev] OK`, con un `dev.apk` de 75,52 MB, instalado con `adb install -r`.
- Perfil limpio con `pm clear`, 18+ y el consentimiento aceptado.

## 1. El viaje de Capi (observación 3 de la primera ronda)

- **El tiempo cumple.** Al ganar el 70 desde el 69 y pulsar «Ver mapa»:
  - `map_journey` `end` da `ms` 4.500, `per_buoy_ms` 450 y `elapsed_ms` 4.511;
  - `map_play_ready` da `ready_ms` 4.745 sin tocar.
  - Con el 80 desde el 79, lo mismo.
  - Sin probar: el toque que lo acelera ×6.
- **El dueño, del viaje del 70:** «sí está bien».
- **El del 80 se ve demasiado rápido.** Cruza al mundo 4: `map_open` da `zoom` 15,0, frente a 4,0 en el del 70, y con el mismo tiempo la canoa cruza la pantalla mucho más deprisa. El dueño, literal: «este es muy rápido, o se ve rápido por el efecto zoom… si parece un error al ir tan rápido, pero es porque el zoom». Propone que suba sin tanto zoom y que, al llegar, se acerque un par de segundos para ver el detalle y luego vuelva a la vista principal (observación 1).
- **Preguntas abiertas de M2-3:** sobre la cámara y el ritmo de los gestos, el dueño solo contestó «el anterior fue bien». Del viaje con zoom alto no se pudo juzgar.

## 2. La tripulación con primer plano

1. **Tucán** (`pm clear`, `af_max_level 21`; el juego entra en el 22 y el dueño sale con la pausa › «Salir», lo que abre el Mapa en la vista Cuenca, y pasa a Mundo):
   - **El dueño:** «los puntos son muy marcados, no deja ver bien el tucán si es que está ahí; el remo sí, pero el tucán parece que lo tapan los puntos» (observación 2).
   - En la captura (`tucan.png`, vista Cuenca) se ve la hilera de puntos blancos muy marcada junto a la canoa.
   - El registro no trae ninguna línea del primer plano en esa apertura (`map_open` `source` l10, `view` basin).
2. **Tucuxi:** el 51 se ganó desde el 50, tras `pm clear`, pero la victoria del 51 no tiene «Ver mapa»: trae `victory_logbook` en su lugar. Queda abrir el Mapa desde el inicio y mirar el primer plano.

## Observaciones

1. **Media (el dueño). Con el zoom alto, el viaje de 4,5 s parece un error de lo rápido que va.** Pasa al cruzar de mundo (`zoom` 15, el 80). Propuesta del dueño: el viaje con menos zoom y, al llegar, un acercamiento de un par de segundos para ver el detalle, y vuelta a la vista principal.
2. **Media (el dueño). Los puntos de la ruta están muy marcados y tapan al tucán.** El remo ya se ve.
3. **Media (el dueño, gráficos). El río tiene detalles toscos.** Literal: «debería estar más suavizado, más real; se ve que se pone la pieza que une un brazo o una bifurcación muy marcada y mala». Queda por mirar de cerca en las capturas del punto 3.
