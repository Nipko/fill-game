# Prueba de la ola 7 (parte C) de la tanda 4 en el teléfono (2026-10-20)

El portátil compiló `integracion-fase1-fase2` en `8af0f2c`, instaló el `dev.apk` en el SM-S938B por depuración inalámbrica y siguió el «QUÉ PROBAR EN EL TELÉFONO» de ese commit, más la muestra de `24ffbd4`.

**Resultado:**
- cumplen el Reto del río, los ríos suaves de la vista Mundo, TiTino, «¡Perfecto!», Revelar sin stock, el dedo del 43, la fuente del dato, el agua ámbar-té, el tooltip de Marea, los avistamientos y la regresión (**43/43 ganados**);
- hay 4 observaciones: el final del 700, el «Jugar · Nivel 701», el dedo del 2 y el nombre de la especie en la tarjeta de parada;
- quedan sin probar el tablero del día siguiente, el saludo de la mañana de TiTino y la Tinaja del 323.

Capturas y logcat en el portátil: `Logs/device/ola7c/` (fuera de git).

## Verificación (portátil ARM64)

| Comando | Resultado |
|---|---|
| `dotnet build dotnet/AmazonFill.slnx -warnaserror` | 0 avisos, 0 errores |
| `dotnet test dotnet/AmazonFill.slnx` | 975 superados, 6 omitidos (981) |
| `python tools/validate_schema.py` | 103/103 PASS |
| `pwsh -NoProfile -File scripts/unity-compile.ps1` | Compila |
| `unity run … BuildDev.BuildAndroidDev` | `[AF.BuildDev] OK`: `dev.apk` de 64,57 MB, 562 s; árbol limpio |
| `adb install -r Builds/Android/dev.apk` (inalámbrico) | `Success` |

## Resultados

- **Reto del río (punto 1): cumple.**
  - Tras la regresión, el inicio muestra «Reto del río · 20 oct · Jugar · 0/7» (`daily_card` `play`, banda 3, `d3_n0292`).
  - Al tocarla abre el tablero «Reto del río» (`level_start` con `d3_n0292`, `variant: base`, banda 3; nivel efectivo 701).
  - Salir por la pausa tras 5 vertidos: la tarjeta pasa a `in_progress` y, al volver, la partida se reanuda con los 5 vertidos.
  - Al ganar: `daily_challenge` `win`, `earn_virtual_currency` +60 (1.520 → 1.580), calendario con el sello en el 20 y «Cosecha doble: x2 por vídeo».
  - Cosecha doble con el anuncio simulado: +60 más (`source: double_harvest`), «+120» y 1.640.
  - En el inicio, «Sellado · 1/7» con racha 1. Al tocar la tarjeta se abre el calendario, y **Atrás lo cierra**.
  - Canoas (5/5) y nivel de campaña sin cambios.
  - No probado: el tablero del día siguiente.
- **Ríos suaves en la vista Mundo (punto 2): cumple.** Con max 58, 120, 300 y 700, los ríos son curvas suaves, sin las esquinas de la hoja del 2026-10-19 (observación 1). Con max 700 la canoa está en «El nevado Mismi» y el Muestrario en 73/73. Sobre el viaje final, ver la observación 1.
- **TiTino (punto 3): cumple.** Bocadillo en la franja de la victoria, con frases distintas:
  - 40: «¡Mundo completo! Cuánto color trajiste.»;
  - 60: «¡Tu primer Muy difícil! Con calma lo lograste.»;
  - 140: «Un mundo entero, a tu ritmo. ¡Cuánto color!»;
  - 700: «¡Otro mundo completo! El río brilla contigo.».
  El bocadillo dura unos 3 s. No probado: el saludo de la mañana (prueba a la 1 de la madrugada).
- **«¡Perfecto!» (punto 4): cumple.** Sale en todas las victorias sin la Cuia de rescate.
- **Revelar sin stock (punto 5): cumple.** Con el stock a 0 (la unidad de regalo se gastó en el 100), Revelar en el 135 abre directamente el vídeo `rv_reveal`.
- **Dedo guía (punto 6): cumple en el 1 y el 43; en el 2, ver la observación 3.** En el 43, tras la tarjeta de debut de la Hoja, el dedo señala 5 → 1 (`lesson: hoja`) y aparece el id `tutorial_finger`. En el 323 (variante b) no sale la Tinaja, como anunciaba el commit.
- **Fuente del dato (punto 7): cumple.** En las paradas 4 y 5 del Mapa, «Fuente: Wikipedia» va bajo el dato y sobre «Otro dato». Las paradas bloqueadas (6 y 7) solo muestran «Nivel 61/71» y la silueta.
- **Agua negra (punto 8): cumple.** En el mundo 3 (41-80, `water: black`) el río del fondo es ámbar-té (RGB 154, 115, 60), frente al azul del mundo 5.
- **Tooltip de Marea (punto 9): cumple.** En la regresión con perfil limpio salió en el 5 como respaldo (`tutorial_undo`), porque la partida automática no hace vertidos malos en el 3. Después, dos vertidos que no acercan en el 3 ya no lo muestran.
- **Rótulo del lugar (punto 10): cumple a la vista.** El lugar va en placa en la franja de la victoria y en las tarjetas del Mapa.
- **Avistamientos (punto 11): cumple.** `fauna_sighted` `saimiri_collinsi` en el 5 con `first: true` en la regresión, y `first: false` al repetir el 5.
- **Regresión (punto 12 y muestra de `24ffbd4`): cumple.**
  - Partida automática con la solución A* (modo `run`) en 43 niveles: 1-12, 41, 63, 100, 143, 183, 243, 283, 301, 323, 340, 343, 360, 400, 403, 421, 460, 509, 560, 561, 600, 640 y 691-700. **43/43 ganados.**
  - Tablero igual al pack y 0 excepciones ni líneas `E Unity` en todos los logcat.
  - Banners de viaje en el 11, 41, 301 («Mundo 7: Madre de Dios»), 421 y 561 («Mundo 9: Ucayali»).
  - Tarjetas previas en el 63, 100, 143, 183, 243, 283, 340, 343 (Brote), 360, 400, 403, 460, 509, 560, 600, 640 y el tramo final.
  - Hay 3 rechazos sin efecto en el 340, 403 y 460: el toque que cierra la tarjeta cae sobre una cuia.

## Observaciones y pendientes

1. **Final del 700.**
   - Al ganarlo: «¡Perfecto!», TiTino y solo «Siguiente» e «Inicio», sin «Ver mapa» ni llegada.
   - «Siguiente» abre «El río sigue creciendo» (`content_end`) y enseguida el Mapa (`map_open` con `source: content_end`), con la canoa ya en el nevado Mismi.
   - En capturas cada 2 s no se ve el viaje de la canoa: si debe haberlo, hace falta vídeo para confirmarlo.
   - La cabecera del Mapa dice «Ucayali» con la canoa en el Mismi.
2. **«Jugar · Nivel 701» en el inicio** tras ganar el 700. Al tocarlo sale bien «El río sigue creciendo · Río arriba llegan 100 niveles nuevos en unos 14 días» con «Volver», pero el botón promete un nivel que no existe. ¿Otro texto cuando se acaba el contenido?
3. **Dedo guía del 2:** no salió, ni con perfil limpio ni al repetir el nivel. En el 2 solo sale la banda «tutorial_keep_one_empty». Si debía salir, falta; si no, hay que corregir el «QUÉ PROBAR».
4. **Tarjeta de parada:** la especie sale como «sauim-de-coleira», en minúsculas y con guiones, como un identificador. ¿Falta el nombre visible?
5. **Sin probar:**
   - el tablero del Reto del día siguiente;
   - el saludo de la mañana de TiTino;
   - la Tinaja del 323 (pendiente de la compuerta PF1);
   - el audio (pendiente del dueño).
