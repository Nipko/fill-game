# Prueba de la tanda 5 en el teléfono (2026-10-22)

El portátil compiló `integracion-fase1-fase2` en `3e1745fc`, igual que `main`, e instaló el `dev.apk` en el SM-S938B por depuración inalámbrica. Recorrió los «QUÉ PROBAR EN EL TELÉFONO» desde la última prueba (`8af0f2c`), que son 23 commits: la ola 7 de la tanda 4, T1 y T2, B1a, B10, FX16, B14, B1b-Q10, B13, B6, B3, B2, B7, B4, B5, B8, F3-5, D-038, B12, B11 y B9.

**Resultado:**
- cumple casi todo lo que se puede probar por `adb`:
  - la regresión (**32/32 ganados**, también con el stripping High) y las líneas `AF_EVENT`;
  - la edad y el consentimiento, la banda 13-17, el bloqueo de <13 y Eliminar cuenta y datos;
  - las Canastas y la Crecida, la Bitácora, el Mercado con la tienda verificada, la Pororoca, el Salto del Boto y las Canoas;
  - los dedos y tooltips de B11, el arrastre y el mantener pulsado, Reducir movimiento, Alto contraste y la fuente al 130 %;
  - el viaje final del 700;
- hay 7 observaciones;
- el inglés y el portugués no se pueden probar porque el juego no los ofrece (observación 1).

Capturas y logcat en el portátil: `Logs/device/t5/` (fuera de git).

## Verificación (portátil ARM64)

| Comando | Resultado |
|---|---|
| `dotnet build dotnet/AmazonFill.slnx -warnaserror` | 0 errores |
| `dotnet test dotnet/AmazonFill.slnx` | 996 superados, 6 omitidos (1.002) |
| `python tools/validate_schema.py` | 116/116 PASS |
| `pwsh -NoProfile -File scripts/unity-compile.ps1` | Sin veredicto: el servidor del pipeline respondió 503 mientras el Editor reimportaba (ver la observación 7) |
| `unity run … BuildDev.BuildAndroidDev` | La CLI salió con 6, pero el Editor terminó el build: `[AF.BuildDev] OK`, `dev.apk` de 66,37 MB en 694 s. El único error es de licencias («Access token is unavailable») |
| `adb install -r Builds/Android/dev.apk` (inalámbrico) | `Success` |

El árbol quedó limpio: el único cambio, `ProjectSettings/AndroidResolverDependencies.xml`, era de finales de línea y se descartó.

## Resultados

- **Regresión y stripping High (ola 7 punto 7, B10 punto 7): cumple.**
  - Partida automática con la solución A* en 1-12, 41, 63, 100, 143, 183, 243, 283, 301, 323, 340, 343, 400, 421, 460, 509, 561, 600, 640, 695 y 700: **32/32 ganados**.
  - 0 excepciones, `MissingMethod`, `TypeLoad` o `E Unity` en todos los logcat de la sesión.
  - Los packs nuevos `reserva_0*` repiten los números de nivel: el cliente carga el de `mundo_0*`.
- **Edad y consentimiento (B12): cumple.**
  - Con perfil limpio, tras la carga sale «¿Cuántos años tienes?» con tres botones iguales; Atrás no la salta.
  - Con 18+ sale «Tu privacidad» con las dos casillas sin marcar, «Rechazar todo» y «Aceptar todo» al mismo nivel y «Guardar mi elección»; después, el nivel 1. En logcat, `consent` `done` con los flags.
  - En Ajustes › «Privacidad y datos», apagar la analítica tiene efecto al momento (`af_consent` `changed`, `analytics: false`).
  - «Corregir mi edad» a 13-17 sale una sola vez (después desaparece). Con 13-17:
    - Privacidad no muestra el interruptor de anuncios («Con tu edad, los anuncios no son personalizados»);
    - una compra del Mercado pide «¿Confirmas la compra?» con el precio antes de la hoja simulada.
  - Con <13, «Pide a un adulto que instale el juego», sin juego debajo. Atrás no hace nada, al reabrir sigue bloqueado y no sale ninguna línea `AF_EVENT`.
- **Eliminar cuenta y datos (B8, F3-5): cumple.**
  - Ajustes muestra el código de borrado de 26 caracteres.
  - Paso 1: Atrás cierra sin borrar. Paso 2 («¿Eliminar todo?» › Eliminar): `af_account_delete` `requested`, `confirmed` y `done`, y el juego vuelve a la primera ejecución con la pantalla de edad.
  - `profile_integrity: valid` al arrancar tras guardar.
- **Mercado y tienda verificada (B5, F3-5): cumple.**
  - El icono del inicio abre el Mercado con pestañas y los precios de la tienda falsa (`COP 3,900`…), sin «Restaurar compras».
  - Compra simulada del Puñado: `iap_verification` `verifying` → `credited` y +1.000 una sola vez.
  - Tras reiniciar, `ledger_restore` sin reabonos y el mismo saldo.
- **Bitácora (B4): cumple.**
  - La victoria del 10 lleva la Canasta (+50). «Siguiente» va al inicio con la Bitácora abierta. Al tocar la página de hoy salen tres premios (+60 / Marea ×2 / +30 y Marea ×1).
  - Al elegir, la página queda sellada, el premio llega al saldo (`calendar_claim`) y «Seguir» abre el Mapa (`source: l10`). Atrás vuelve al inicio, con «Bitácora · Sellada hoy» y sin repetir la libreta.
- **Canasta y Crecida (B1a): cumple.**
  - Hay `basket_open` en el 10 (50) y en el 100 (la grande).
  - En la Crecida, `streak_milestone` 3 (+300), 6 (cofre) y 10 (Cuia extra y río crecido). Después sale `free_double_harvest` en las victorias siguientes, sin el botón ×2.
- **Pororoca (B2): cumple.**
  - La tarjeta sale al entrar en el 30 y «Barajar» (300) va en la bandeja.
  - Al usarla: confirmación con 300 semillas, `shuffle` `shuffled` y el botón queda en gris en ese nivel.
- **Canoas, Salto del Boto y Sin canoas (B11, B3, D-038): cumple.**
  - La tarjeta de las Canoas sale antes del 31.
  - Salir 5 veces del 32 (`fails_level: 5`) deja el banner de Rosa sobre el tablero («Rosa puede llevarte río arriba · 300») y «Salto del Boto · 300» en la pausa. Al confirmar, cobra 300, carga el 33 y registra `af_skip_used`.
  - «Sin canoas» muestra «Mientras esperas: Jugar el Reto del río», que abre el Reto sin gastar canoas, y «Volver al Mapa del río», que abre el Mapa sobre el inicio.
- **Tutorial (B11, ola 7 punto 6): cumple, con las observaciones 3 y 4.**
  - En el 2, el dedo señala 0 → 3 y, hecho el vertido, la racha 2 → 0.
  - En el 8, el dedo va sobre «+1 cuia» y se va al usarla.
  - En el 6, el tooltip «Reiniciar es gratis…» sale encima de Aguas estancadas, con las dos frases (las Canoas están activas por defecto), y no se repite en el mismo nivel.
  - En el 11, el tooltip de la Cosecha doble sale y se va al pulsarla. Con el anuncio simulado, +20.
- **Reto del río tras el 7 (T1): cumple, con la observación 2.** A ~1 s de la victoria sale «Desde el nivel 10, un tablero nuevo cada día».
- **Gestos (C9 y C10): cumple.**
  - Mantener pulsada una cuia muestra un bocadillo con los nombres de sus frutas, sin seleccionarla.
  - Arrastrar 3 → 2 vierte; soltar en el fondo cancela; soltar sobre una ilegal da `reject`.
- **Accesibilidad (B9): cumple, con la observación 6.**
  - Con «Reducir movimiento», la victoria sale sin confeti ni hojas y el viaje del Mapa se salta (`by: reduce`).
  - Con «Alto contraste», el patrón pasa a 0,35.
  - Con `af_font_scale 130`, la victoria del 20 y Ajustes caben sin cortes.
- **Mapa (B14, ola 7 puntos 4 y 5): cumple.**
  - Con `af_max_level 25` salen «Premio del mundo · faltan 15», los puntos de paso y las marcas.
  - Viaje final del 700: `map_journey` con `by: time` y `elapsed_ms: 1515`. El río del último tramo va sin codos.
  - Ajustes › «Datos del río» muestra el dato con su fuente.
- **Analítica (B7, D-037): cumple, con la observación 5.**
  - Hay líneas `AF_EVENT`: `level_start`, `af_level_win`, `level_end`, `earn_virtual_currency`, `af_map_open`/`close`, `af_stop_arrive`, `af_river_fact_shown`, `af_fruit_discovered` y las de compras y anuncios.
  - Llevan los comunes y `"net":"online"`. No hay ninguna línea cortada.

## Observaciones

1. **El inglés y el portugués no se ofrecen** en Ajustes › Idioma: solo Automático, Español y en-XA.
   - En `strings.csv` faltan 118 cadenas en `en` y 119 en `pt-BR` de 438, y solo se ofrece un idioma con toda la UI completa.
   - `--es af_lang en` vuelve a español (`loc` con `source: fallback`).
   - Hay que traducir lo que entró después de T2 para poder probarlo.
2. **Tarjeta del Reto tras el 7:** el perezoso de TiTino de la victoria tapa parte del texto («De[…]ivel 10»).
3. **Tooltip «Reiniciar es gratis…» del 6:** el pico apunta al borde superior del panel, no al botón «Volver a la orilla (gratis)».
4. **Tooltip de la Cosecha doble del 11:** tapa la línea «Cosecha +20» del panel.
5. **`AF_EVENT` con la analítica apagada:** siguen saliendo líneas (por ejemplo, `af_map_open`) con `analytics: false`. Con <13 no sale ninguna. ¿Es solo el registro de desarrollo, o se recogería también en Firebase?
6. **«Reducir movimiento»** es un interruptor de sí o no; el commit hablaba de auto/on/off.
7. **Build en el portátil:**
   - `unity-compile.ps1` no da veredicto mientras el Editor reimporta tras un pull grande (503 del pipeline);
   - avisa de que `com.unity.pipeline` «usa los nombres de comando antiguos» (`unity pipeline upgrade`);
   - `unity run` devolvió 6 aunque el build terminó bien.
   Sigue la cabecera «Ucayali» del Mapa con la canoa en el Mismi (hoja del 2026-10-20).

## Sin probar

- Lo que cambia ajustes del sistema del teléfono, que no toco desde `adb`:
  - la pantalla 16:9 (`wm size`) de FX16a y FX16b, incluido el panel del 640;
  - el tamaño de fuente de Android;
  - TalkBack;
  - el modo avión (`"net":"offline"`);
  - cambiar la hora (anti-reloj de las canoas, Regalo de regreso y tablero del día siguiente).
- La skin al ganar el 40, el 80 y el 140; la Hoja escudo; el Cántaro lleno; y la ola de la Pororoca con Lluvia o Piraña o tras salir y volver.
- El `rehearsal.apk` de B10 (no se compiló).
- El audio (pendiente del dueño).
