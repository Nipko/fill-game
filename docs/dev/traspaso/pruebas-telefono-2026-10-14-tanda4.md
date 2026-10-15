# Prueba de la ola 1 de la tanda 4 en el teléfono (2026-10-14): resultados para el escritorio

El portátil compiló `integracion-fase1-fase2` en `1593e20` («Cierra la ola 1 de la tanda 4…»), instaló el `dev.apk` en el SM-S938B por depuración inalámbrica y siguió el «QUÉ PROBAR EN EL TELÉFONO» de ese commit.

**Resultado: casi todo cumple.**
- Ajustes, el idioma y su persistencia, la pseudolocalización, los Patrones de fruta, los textos de 12 sp y la regresión funcionan.
- **Hay un defecto:** la primera pulsación de Atrás tras cada arranque no cierra Ajustes.
- **Y un hallazgo de en-XA:** las etiquetas de la bandeja se salen de sus botones.
- Además hay dos observaciones (al final).

Las capturas y los logcat están en el portátil, en `Logs/device/tanda4/` (fuera de git).

## Verificación (portátil ARM64)

| Comando | Resultado |
|---|---|
| `dotnet build dotnet/AmazonFill.slnx -warnaserror` | 0 avisos, 0 errores |
| `dotnet test dotnet/AmazonFill.slnx` | 975 superados, 6 omitidos (981) |
| `python tools/validate_schema.py` | 103/103 PASS |
| `pwsh -NoProfile -File scripts/unity-compile.ps1 -TimeoutSec 3600` | Compila (código 0) |
| `unity run … -executeMethod AF.EditorTools.BuildDev.BuildAndroidDev` | `[AF.BuildDev] OK`: `dev.apk` de 63,01 MB, 215 s de build del APK, 1 aviso; árbol limpio |
| `adb install -r Builds/Android/dev.apk` (inalámbrico) | `Success` |

Notas:
- Tras el build, adb volvió a anunciar el teléfono con otro nombre de servicio: `adb-R5CY92CG81P-toTYBb (2)._adb-tls-connect._tcp`. Hay que usar el que liste `adb devices`.
- Los tests de Unity no se lanzaron en el portátil, porque tumban el adb del teléfono. El escritorio da EditMode 2.052 y PlayMode 528 en `1593e20`.

## Resultados

### 1 · Ajustes (L3): cumple, salvo el defecto 1

- **Se abre desde el botón de engranaje del inicio** (`AF_LAYOUT lobby`, id `settings`):
  - `AF_STATE settings`: `language: auto`, `patterns: auto` y `offered: ["es-419", "en-XA"]`;
  - ids `close`, `lang_auto`, `lang_es-419`, `lang_en-XA`, `patterns_auto`, `patterns_on` y `patterns_off`.
  - En pantalla: Idioma (Automático, Español y Pseudolocalización (en-XA)) y Patrones de fruta (Automático, Siempre y Nunca).
- **Idioma:**
  - con el perfil limpio, `loc` da `locale: es-419`, `source: fallback`, `device: en-US` y `numbers: en` (el teléfono está en inglés de EE. UU.);
  - al elegir en-XA llegan `settings_change` y `loc` con `locale: en-XA` y `source: saved`, y toda la UI se rehace entre corchetes y con acentos («[Júgár · Ñívél 2 ····]»);
  - **tras `force-stop` y reabrir sigue en en-XA** (`source: saved`), por encima del idioma del teléfono;
  - volver a es-419 deja el inicio como antes («Jugar · Nivel 2»).
- **Patrones de fruta:**
  - «Siempre» da `settings_change` con `on: true` y opacidad 0,20, y **el 7 carga con patrones** e iconos en todas las dosis;
  - «Nunca» da `on: false` y 0,15, y **el 63 carga sin ellos**;
  - «Automático» vuelve al comportamiento por nivel.

### 2 · Textos de al menos 12 sp (A1): cumple

En español, a tamaño real, nada se corta ni se desborda en:
- la tarjeta «¡Miquito a la vista!» del 5;
- el aviso de Capi del 8;
- Aguas estancadas del 40;
- la «Compra simulada» del Puñado de semillas;
- ¿Seguro?, Nivel fallido y Sin canoas.

Archivo: `p1_hoja.jpg`.

### 3 · En-XA como prueba de desbordes (B1): cumple, salvo el hallazgo 2

En el inicio, Ajustes, la tarjeta previa del 40 («[Dífíçíl ··]», «[Águás brávás ····]») y del 100 («[Múý dífíçíl ···]», con el texto de Aguas turbias y la Luciérnaga), la victoria, Aguas estancadas y la compra simulada:
- todo sale entre corchetes y con acentos;
- **ningún carácter «□»**;
- los textos largos parten en dos líneas sin salirse.

Archivos: `enXA_hoja.jpg` y `enXA_bandeja.png`.

### 4 · Regresión (sin cambios visibles esperados): cumple

- **Partida automática con la solución A*:** el 1-12 seguidos con «Siguiente» (perfil limpio), y el 41, 63, 100, 143 y 183.
- **17/17 ganados**, con el tablero idéntico al pack, 0 rechazos, 0 Aguas estancadas falsas y 0 avisos `step_budget`.
- Banner de mundo en el 11 y el 41.
- Tarjeta previa en el 63, 100, 143 y 183; el 183 sigue con `bite_demo: animated`.
- 0 excepciones ni líneas `E Unity` en logcat.

## Defecto y hallazgos

1. **Defecto: la primera pulsación de Atrás tras cada arranque no cierra Ajustes.**
   - **Qué pasa:**
     - con la app recién abierta (tras `force-stop` o `pm clear`), en el inicio, se abre Ajustes y se pulsa Atrás (`adb shell input keyevent 4`): no pasa nada ni sale `settings_closed`;
     - la segunda pulsación sí lo cierra;
     - en la misma sesión, las siguientes aperturas se cierran a la primera.
   - **Frecuencia:** 4 de 4 arranques (dos en el guion y dos a mano).
   - **Comparación:** en la fase 1, Atrás cerró a la primera la «Compra simulada» de un nivel abierto con `am start -S`. Así que la tecla llega, pero en el inicio se pierde la primera.
   - **Sospecha:** el Input System crea el dispositivo del teclado con el primer evento de tecla, y ese evento no cuenta como pulsación. Otra posibilidad es que el primer Atrás vaya a otro manejador del inicio.
   - **Arreglo:** que el cierre con Atrás no dependa del primer evento, con un test PlayMode que simule un arranque en frío.
2. **Hallazgo de en-XA: las etiquetas de la bandeja se salen de sus botones.** Con textos ≈ 30-40 % más largos, «[Déshácér ··]» desborda los dos lados de su botón, y «[+1 çúíá ··]» y «[Révélár ··]» llegan al borde. Con inglés o portugués reales pasará lo mismo. Hace falta autoajuste o dos líneas, sin bajar de 12 sp.
3. **Observación: elegir un idioma cierra Ajustes solo,** porque la UI se rehace, y no sale `AF_STATE settings_closed`. Si es lo que se quiere, conviene emitir `settings_closed` (la sonda lo promete al cerrar). Si no, Ajustes debería seguir abierto en el idioma nuevo.
4. **Observación para el dueño: separadores de miles mezclados.** Con el teléfono en en-US, las cifras siguen la región del teléfono (Q8-A; `numbers: en`), pero los precios de la tienda van en pesos colombianos. Resultado: «Puñado de semillas 1,000 · COP 3.900», con coma y punto en el mismo botón. ¿Se acepta, o las cifras del juego deben seguir el formato de la moneda?
5. **El teléfono sigue con el almacenamiento casi lleno** (1,8 GB libres el 2026-10-14).
