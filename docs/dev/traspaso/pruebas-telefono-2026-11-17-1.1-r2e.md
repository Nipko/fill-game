# Prueba de la 1.1 en el teléfono, ronda r2e (2026-11-17)

Spark comprueba en el teléfono los arreglos de la prueba r2d (añadido de D-069 del 2026-11-17, rama `v11/mapa-r2d-rotulos`), sobre `integracion-fase1-fase2` en el commit `9fe394ac`.

- **Build:** `dev.apk` de `scripts/unity-build.ps1 -Method Dev`, con `[AF.BuildDev] OK`.
- **Teléfono:** SM-S918B (S23 Ultra) por cable, con perfil nuevo (`pm clear`, 18+ y «Rechazar todo»).
- **Registros:** capturas y logcat en `Logs/device/v11r2e/` (fuera de git).

## Resumen

| Arreglo | Resultado |
|---|---|
| «Mundo» justo al acabar el viaje del 10, en el 79 (con el tucuxi por presentar) | **cumple** |
| La parada siguiente en el 521 | **cumple** por la ruta del viaje del 10 |
| Abrir el Mapa directamente en Mundo con lo visto atrasado | **no reproducido** (ver abajo) |

## Detalle

- **El 79, pulsando «Mundo» al acabar el viaje del 10** (`hoja-a.jpg`, las dos primeras capturas):
  - a 1,5 s se ve el primer plano del tucuxi, sin rótulos;
  - a 4 s vuelven «Leticia · Premio del mundo · falta 1», la placa «Mundo 4 · Trapecio · Nivel 81» entera, Lago de Tefé, Coari y Codajás.
  - En la r2d no había ningún rótulo a los 3 s.
- **El 521, el 71 y el 79, pulsando «Mundo» tras el viaje del 10** (`hoja-a.jpg`, las tres últimas):
  - «Trinidad · faltan 9 niveles» libre, fuera de la ventanita de casa;
  - en el 71, «Leticia · Premio del mundo · faltan 9» con la placa entera;
  - en el 79, el rótulo junto con «falta 1».
- **Abrir directamente en Mundo con lo visto atrasado:** no se pudo reproducir.
  - Con el perfil en `map_seen_level` 521 (cerrando el Mapa con la flecha), cada arranque con `af_max_level` vuelve a abrir el Mapa con el viaje del 10 en Cuenca (`hoja-b.jpg`), no en Mundo.
  - En la r2d se dio con un perfil que venía de las rondas anteriores.
  - Lo cubren los tests del integrador (`Fx10Tests`); en el teléfono queda sin comprobar.

## Observaciones

Ninguna nueva.

## Sin probar

Lo mismo que en la r2d:

- los tres audios (el dueño);
- el miquito, la llegada, el cambio de mundo en el 40 y Reducir movimiento;
- el marco completo;
- Play Games;
- el Moto G23.
