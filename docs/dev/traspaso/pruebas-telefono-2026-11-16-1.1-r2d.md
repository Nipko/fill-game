# Prueba de la 1.1 en el teléfono, ronda r2d (2026-11-16, noche)

Spark comprueba en el teléfono los arreglos de la prueba r2c (añadido de D-069 del 2026-11-16, tarde; rama `v11/mapa-r2c-arreglos`), sobre `integracion-fase1-fase2` en el commit `8f8fadd2`.

- **Build:** `dev.apk` de `scripts/unity-build.ps1 -Method Dev`, con `[AF.BuildDev] OK`.
- **Teléfono:** SM-S918B (S23 Ultra) por cable.
- **Registros:** capturas y logcat en `Logs/device/v11r2d/` (fuera de git).

## Resumen

| Arreglo | Resultado | Obs. |
|---|---|---|
| «Mundo» tras el viaje del 10 | **cumple**: el primer toque cambia de vista (521 y 132, perfil nuevo, `source` l10) | — |
| El rótulo bajo la ventanita de casa (521) | **cumple** por la ruta del viaje del 10: «Trinidad · faltan 9 niveles» libre. Abriendo de golpe en Mundo, el rótulo de la parada siguiente no sale | 1 |
| La placa del mundo siguiente cortada en el borde | **cumple**: «Mundo 4 · Trapecio · Nivel 81» entera en el 71 y el 79 | — |
| El 79 («Leticia · Premio del mundo · falta 1») | bien en la vista final, pero mal en dos aperturas | 2 |

Capturas: `hoja-a.jpg` (521, 71 y 79 abiertos de golpe en Mundo), `hoja-b.jpg` (521 y 132 tras el viaje del 10), `hoja-c.jpg` y `hoja-d.jpg` (79).

## Observaciones

1. **Baja. En el 521, abriendo el Mapa directamente en Mundo, no sale el rótulo de la parada siguiente.**
   - El perfil ya había visto el Mapa: `map_open` con `view` world y `zoom` 4,00.
   - Ya no queda bajo la ventanita, pero tampoco aparece, ni el encuadre se aleja para que quepa (`hoja-a.jpg`, el primero).
   - Por la ruta del viaje del 10 sí sale.
   - Es el [P] del integrador («los rótulos sin sitio libre no salen»), pero aquí afecta al de la parada siguiente, el que lleva «faltan N».
2. **Media. El 79 sale distinto según cómo se abre el Mapa.**
   - **Abierto de golpe en Mundo** (`hoja-a.jpg`, el tercero):
     - el rótulo junto de Leticia no está;
     - sale una píldora «Premio del mundo» sin «falta 1», pegada a «Lago de Tefé»;
     - una boya queda sobre «Niv·el 81».
   - **Pulsando «Mundo» justo al acabar el viaje del 10** (`hoja-c.jpg`, el primero): ningún rótulo ni placa a los 3 s.
   - **Al volver a pulsar «Mundo» desde Cuenca** (`hoja-d.jpg`): todo correcto, con «Leticia · Premio del mundo · falta 1», la placa entera, Lago de Tefé, Coari y Codajás.
   - Parece que los rótulos no se recolocan bien cuando el selector acaba la secuencia del 10.

## Sin probar

Lo mismo que en la r2c:

- los tres audios (el dueño);
- el miquito, la llegada, el cambio de mundo en el 40 y Reducir movimiento;
- el marco completo;
- Play Games;
- el Moto G23.
