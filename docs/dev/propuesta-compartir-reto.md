# Propuesta: compartir el Reto del río (al estilo Wordle)

- **Fecha:** 2026-11-07.
- **Origen:** `docs/marketing/promocion-organica.md` §3.1. El dueño eligió esta palanca la primera («Compartir el Reto»).
- **Estado:** propuesta. **No cambia el GDD ni el código hasta que el dueño responda §7.** Después, el GDD lo edita el carril de documentación y el código, el escritorio.
- Convención: [V] comprobado en el repositorio o en una fuente primaria · [NV] sin confirmar · [P] propuesta.

## 1. Resumen para el dueño

Al ganar el Reto del río, un botón **«Compartir»** abre el menú de compartir de Android con un texto corto, sin destripar la solución, y un enlace para instalar el juego. Por ejemplo, en es-419:

```
Tintalú · Reto del río · 7 nov 🌊🌊
🟪🟧🟩🟦🟥🟨🟫⬜
✅ 14 vertidos · sin ayudas · racha 5
tintalu.com/r
```

- **Por qué esta palanca:**
  - el Reto es diario y el mismo para todos los de una misma banda, así que se presta a comparar y a picarse;
  - el texto se pega en WhatsApp, que en Brasil y LatAm usa casi todo el mundo;
  - cada mensaje es una invitación con enlace a quien conoce al que la manda;
  - es lo que hizo crecer a Wordle.
- **Coste técnico bajo:**
  - **sin SDK, paquete ni permiso nuevos**: el texto se comparte con un `Intent` de Android lanzado desde C#, como ya hace `Assets/_Project/Scripts/UI/AndroidAppSettings.cs` [V];
  - una ruta corta en tintalu.com que redirige a Play con su UTM;
  - un evento de analítica y una clave de Remote Config para apagarlo.
- **Versión propuesta:** la 1.1, si cabe sin retrasar D-068 y D-069; si no, la 1.2.

## 2. Lo que ya existe [V]

- **El Reto del río** (06 §4.1, D-031): un tablero por día local desde el nivel 10, en 3 bandas según el nivel máximo (B1 < 41, B2 41-200, B3 201+).
  - Dos jugadores de la misma banda tienen el mismo tablero ese día.
  - De bandas distintas, no: es la decisión cerrada 2 de 06, «Reto del río por bandas».
- **Al ganar:** el calendario de sellos (`DailyStampCalendar`) con el sello que cae, el premio y «Cosecha doble». Ahí iría el botón.
- **Lo que el cliente ya sabe al ganar:**
  - la fecha y la banda (`DailyChallengeScreen`, `_levelStartBand`);
  - el tiempo de partida (`t_ms`);
  - la racha (`streak_days`);
  - la paleta del tablero.
  - El número de vertidos de la partida y si se usaron comodines no está a mano en la pantalla. Se cuenta igual que `_playMs` [P].
- **Analítica:** `af_daily_challenge` (08 §8.2). No hay ningún evento de compartir.
- **Web:** tintalu.com, con el enlace a Play y `utm_source=tintalu.com` (añadido de D-041).

## 3. Qué se comparte

### 3.1 El texto (una plantilla por idioma, en `strings.csv`)

| Línea | Contenido | Nota |
|---|---|---|
| 1 | `Tintalú · {Reto del río} · {fecha corta} {olas}` | Las olas (🌊, 🌊🌊 o 🌊🌊🌊) son el icono de la banda de la tarjeta (06 §4.1), así quien lo recibe sabe si es su mismo tablero |
| 2 | Un cuadrado de color por tinte del tablero, en orden de cuia | La paleta del día, sin la solución: un tablero resuelto es una cuia por color, así que no destripa nada |
| 3 | `✅ {n} vertidos · {sin ayudas | n ayudas} · racha {r}` | La racha solo si es ≥ 2 |
| 4 | `tintalu.com/r` | Sin parámetros a la vista; el UTM lo pone la redirección (§4) |

- **Colores:** 9 cuadrados de emoji (🟥🟧🟨🟩🟦🟪🟫⬛⬜). Cada tinte se asigna al más cercano por su color de la paleta (07), con una tabla fija en el código y sus tests. Con más de 9 tintes se repite alguno: el cuadrado es decorativo, no es una pista.
- **Lo que nunca va:**
  - el nombre del jugador, su avatar, el país o el puesto de la Liga;
  - ningún dato de la cuenta: el público es de 13+ (05 §7);
  - premios por compartir. Compartir no da semillas ni nada: sería «compartir para ganar» y envenenaría el canal.
- **Idioma:** el de la app (`settings.language`). Los textos nuevos pasan por la revisión de idiomas de D-054, como los demás.

### 3.2 Dónde está el botón

- En el calendario de sellos **tras ganar**, junto a «Cosecha doble», con el mismo estilo de botón secundario.
- En el calendario que se abre desde la tarjeta cuando el Reto de hoy ya está **sellado**, para quien lo comparte más tarde.
- **Nunca** tras perder ni en la campaña, en esta versión. La llegada a una parada del Mapa del río sería la siguiente, con imagen (§6).

### 3.3 Imagen: más adelante

Una imagen con la silueta del tablero resuelto y el logotipo llama más que el texto, pero pide servir un archivo a otra app: un `ContentProvider` propio en un `.androidlib`, como el de `AfAudioFocus`, y probar que WhatsApp, Instagram y Telegram la aceptan. Se propone para la versión siguiente, cuando el texto tenga datos (§7, P1).

## 4. El enlace: `tintalu.com/r`

- **Qué hace:** redirige (302) a la ficha de Play con `referrer=utm_source%3Dshare%26utm_medium%3Dreto%26utm_campaign%3Ddaily`. Play pasa ese *referrer* al instalar, y Firebase y Play Console atribuyen la instalación a «compartir el Reto» (`estrategia.md` §6.2).
- **Por qué una ruta propia y no el enlace de Play:**
  - el mensaje queda corto;
  - cuando llegue iOS (D-067), la misma ruta mandará a cada uno a su tienda sin cambiar el juego;
  - si un día hace falta, se puede cambiar el destino sin publicar una versión.
- **Dónde:** `infra/web`, como el resto de la web (CloudFront). Con sus tests y desplegado por el dueño con `cdk deploy AfProdWeb --exclusively` y el contexto de prod (añadido de D-041).

## 5. Medición y control

- **Evento nuevo** en 08 §8.2: `af_share {surface: "daily", date, band, result: "win", pours, aids}`.
  - Se emite al tocar «Compartir».
  - Android no dice a qué app se envió sin un receptor en el manifiesto, así que se mide lo que llega por el enlace y no el destino [P].
- **Clave nueva** en 08 §9: `ui_share_daily_enabled` (bool, default `true`) para apagarlo sin publicar una versión.
- **Panel:**
  - toques en Compartir ÷ Retos ganados;
  - instalaciones con `utm_source=share` (Play Console, Adquisición);
  - D1 y D7 de esa cohorte (E5).

## 6. Impacto técnico (para el escritorio)

| Pieza | Qué | Dónde |
|---|---|---|
| Texto | Constructor puro del mensaje (fecha, banda, paleta, vertidos, ayudas, racha y textos), con tests EditMode y la tabla de colores | `Scripts/LiveOps/` (AF.LiveOps, sin motor) |
| Compartir | `IShareSheet`, con `AndroidShareSheet` (`Intent.ACTION_SEND`, `text/plain`, `Intent.createChooser`, desde C# con `AndroidJavaObject`, como `AndroidAppSettings`) y una implementación falsa para el Editor y los tests | `Scripts/UI/` o `Scripts/Platform/` |
| Botón | «Compartir» en `DailyStampCalendar` (tras ganar y desde la tarjeta sellada), con TalkBack, a 320 dp y al 130 %, y en es-419, en, pt-BR y en-XA | `Scripts/UI/` |
| Contadores | Vertidos y ayudas de la partida del Reto. Si se reanuda, se guardan en `daily_challenge.in_progress`, como el `level_state` | `DailyChallengeScreen` y el perfil (08 §4.3) |
| Textos | `daily_share_*` en `strings.csv` | `Data/Loc/` |
| Analítica y RC | `af_share` en `AnalyticsCatalog`; `ui_share_daily_enabled` en 08 §9 y `rc_defaults.py` | AF.Analytics y AF.RemoteConfig |
| Web | La ruta `/r` con su redirección y sus tests | `infra/web` |
| GDD | 06 §4.1 (Pantalla y la fila de Analítica), 08 §8.2 y 08 §9 | Carril de documentación |

- **No cambia:**
  - el núcleo, el Reto, sus bandas, el pool ni el premio;
  - el manifiesto: sin permisos ni actividades nuevas, así que la compuerta `ci/check-manifest.ps1` no se toca.
- **Verificación:**
  - los tests EditMode y PlayMode de siempre;
  - `rc_defaults.py --check`;
  - los tests de `infra/web`;
  - en el teléfono: ganar un Reto, compartir a WhatsApp, abrir el enlace desde otro teléfono y llegar a la ficha.

## 7. Preguntas para el dueño

| # | Pregunta | Opciones | Recomendada |
|---|---|---|---|
| P1 | ¿Qué se comparte en la primera versión? | **A. Texto con los cuadrados de color** · B. Imagen · C. Las dos | **A** (B en la siguiente, con datos) |
| P2 | ¿Qué cifras lleva el texto? | **A. Vertidos, ayudas y racha** · B. Solo «lo resolví» · C. A + el tiempo | **A** (el tiempo premia la prisa y el Reto no la pide) |
| P3 | ¿Dónde está el botón? | **A. Tras ganar y en el calendario del Reto sellado** · B. Solo tras ganar | **A** |
| P4 | ¿A dónde va el enlace? | **A. `tintalu.com/r`, que redirige a Play con el UTM** · B. Directo a Play | **A** (corto y listo para iOS) |
| P5 | ¿Premio por compartir? | **A. No** · B. Sí, pequeño | **A** |
| P6 | ¿Interruptor en Remote Config? | **A. Sí, `ui_share_daily_enabled` = true** · B. No | **A** |
| P7 | ¿En qué versión? | **A. La 1.1 si no la retrasa; si no, la 1.2** · B. La 1.1 sí o sí · C. La 1.2 | **A** |
| P8 | ¿Se reconsideran las bandas para que todos tengan el mismo tablero? | **A. No; las olas del texto dicen la banda** · B. Sí | **A** (la decisión cerrada 2 de 06 se mantiene) |

## 8. Fuentes

- El patrón de Wordle (la tarjeta sin destripes como motor de crecimiento): `docs/marketing/promocion-organica.md` §3.1 y sus fuentes.
- El uso de WhatsApp en Brasil: la misma, §4.1 [NV, fuentes de proveedores].
- El código citado: `DailyChallengeScreen.cs`, `DailyStampCalendar.cs` y `AndroidAppSettings.cs` en `origin/main` del 2026-11-07.
