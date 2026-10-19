# Prueba de la ola 7 (parte A) de la tanda 4 en el teléfono (2026-10-19)

El portátil compiló `integracion-fase1-fase2` en `6523058`, instaló el `dev.apk` en el SM-S938B por depuración inalámbrica y siguió el «QUÉ PROBAR EN EL TELÉFONO» de ese commit.

**Resultado:**
- cumplen la regresión, las líneas partidas de la sonda, el Mapa desde la pausa con «ponerse al día», la vista Cuenca y el Mapa con `af_max_level 560`;
- hay una observación sobre la vista Mundo;
- quedan sin probar la temporada, TiTino tras una pista y la fluidez del banner (al final).

Capturas y logcat en el portátil: `Logs/device/ola7/` (fuera de git).

## Verificación (portátil ARM64)

| Comando | Resultado |
|---|---|
| `dotnet build dotnet/AmazonFill.slnx -warnaserror` | 0 avisos, 0 errores |
| `dotnet test dotnet/AmazonFill.slnx` | 975 superados, 6 omitidos (981) |
| `python tools/validate_schema.py` | 103/103 PASS |
| `pwsh -NoProfile -File scripts/unity-compile.ps1 -TimeoutSec 3600` | Compila |
| `unity run … BuildDev.BuildAndroidDev` | `[AF.BuildDev] OK`: `dev.apk` de 64,81 MB (igual que en el escritorio); árbol limpio |
| `adb install -r Builds/Android/dev.apk` (inalámbrico) | `Success` |

## Resultados

- **Regresión (punto 9): cumple.**
  - Partida automática con la solución A*: el 1-12, y el 41, 63, 100, 143, 183, 243 y 283; **19/19 ganados**.
  - Tablero idéntico al pack y 0 rechazos.
  - Banner en el 11 y el 41.
  - Tarjeta previa en el 63, 100, 143, 183, 243 y 283.
  - 0 excepciones ni líneas `E Unity` en logcat.
  - Novedades del flujo, que obligaron a ajustar el guion (no son fallos):
    - al ganar el 10 sale la secuencia del Mapa, que espera un toque antes del 11;
    - el banner del 41 es ahora de viaje, con el mapa, la canoa y «Ver mapa», y solo se cierra cuando ya admite toques.
- **Líneas partidas (punto 7): cumple.** Con `af_max_level 560`, la `AF_LAYOUT` del Mapa llega en 2 partes, y la de Ajustes (28 botones) también en partes. Juntadas con `part`/`parts`, salen enteras: **0 líneas JSON rotas**.
- **Mapa desde la pausa del 41 con max 40 (puntos 2 y 4): cumple, con la observación 1.**
  - `map_open` con `source: pause`, `view: world` y la parada 4.
  - «Ponerse al día» lleva la canoa a Manaos.
  - La ventanita de casa («Casa · a unos 1,470 km») y las placas quedan dentro del encuadre, sin cortes.
  - `AF_LAYOUT river_map` trae `back`, `view_world`, `view_basin`, `play`, `stop_remaining`, `home_window`, las paradas y `samples`.
  - Archivo: `mapa_hoja.jpg`.
- **Vista Cuenca: cumple.** Ríos con meandros y afluentes detallados. Las placas de los mundos siguientes (Loreto, Ucayali, Madre de Dios, Trapecio, Oriente y Madidi) van apiladas a la izquierda, enteras y sin tapar nada.
- **Mapa con `af_max_level 560`: cumple.** Abre en el Ucayali con «Volver a la canoa», «Jugar · Nivel 561» y el Muestrario 60/73.

## Observaciones y pendientes

1. **Vista Mundo: los ríos se ven como líneas quebradas** (tramos rectos con esquinas marcadas), no como meandros suaves. Entra en la pregunta del commit sobre las tolerancias de la vista Mundo (M5.1). Para decidir con las capturas: ¿se suaviza en esa vista?
2. **El fallo conocido** del frente de agua tras «ponerse al día» (`FrontRibbonAt` sin `FitRibbon`) no se distingue a simple vista en la captura del 41: haría falta una captura ampliada del último tramo.
3. **Sin probar esta vez:**
   - la temporada en la tarjeta de parada y en la del hito (punto 5);
   - TiTino en la victoria tras usar una pista (punto 6);
   - la fluidez del banner del 41 y del 81, medida en vídeo (punto 3);
   - el segundo build seguido sin crecer (punto 8).
   La Tinaja (punto 1) llega con C1c.
