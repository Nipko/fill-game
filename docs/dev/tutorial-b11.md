# Tutorial, Sin canoas y contenido (bloque B11 de la tanda 5)

Bloque B11 de `docs/dev/plan-tanda5.md` §4: el dedo del 2; el dedo de la Cuia extra del 8; el tooltip del 6 y del 8; la tarjeta
de Canoas del 31; el tooltip de la Cosecha doble del 11; «Volver al Mapa» en Sin canoas; la reserva del 20 % para
`level_overrides` (03 §17); V3; `lvl_label_override`; el test a 320 dp; y C10 (arrastre). Rama `spark/t5-b11`.

Fuentes: GDD 02 §3.1 (C10), §5 («Dispositivos»), §10 y §13; 03 §13 y §17; 04 §2.2, §2.7, §7.1-§7.3 y §11; 06 §2.2; 08 §4.1 y
§9. Decisiones: D-033 (Q-N2 (a) y Q-E4 (a)), D-038 (con `biz_feature_lives` apagado, desde el 31 todo como antes del 31, y la
Balsa sin Canoas infinitas), D-028 (P5: el dedo guía es una ayuda explícita compatible con D-002) y D-013, T1-A.

## 1. Qué hace

### 1.1 V3 y Q-N2 (a): `minus_color` desde el 31 (`tools/level-generator`, en la rama)
- Con la Cuia extra aplicada, el tercer fallo sirve la variante de −1 color, nunca la de +1 vacío (04 §2.2, caso b;
  `FailResolve.ChooseRelief`). 18 niveles de 31-700 solo tenían `plus_empty`, así que con la Cuia extra se quedaban sin alivio
  (el caso V3 del plan).
- La primera pasada del generador (A* w = 1 a 100.000, 03 §13) no resolvía ninguna eliminación de un color en esos 18 (hitos y
  retos de 11-14 cuias con mecánicas). **Segunda pasada** (`Relief.MinusColorSecondBudget` = 2.000.000 [P]): si la primera no
  deja ninguna `minus_color` que cumpla el criterio de 03 §17 #8, la misma regla con más presupuesto. El par sigue siendo el
  óptimo exacto (w = 1): level-qa lo recalcula con ese presupuesto (`Relief.MinusColorBudget`, QA-RELIEF-MINUS) y da el mismo
  número, y los niveles que ya tenían `minus_color` no cambian (una búsqueda que acaba dentro de 100.000 es la misma con más).
- **Resultado:** 17 de los 18 la llevan (118, 200, 358, 392, 395, 423, 483, 540, 578, 580, 615, 618, 620, 660, 672, 679 y
  700). En el 358, el 615, el 618, el 672 y el 679, con 2 vacíos libres, la −1 color pasa a `relief[0]` por la regla de orden de
  03 §13 («−1 color cuando el nivel ya tiene 2 vacíos»): el tercer fallo sin Cuia extra también sirve ahora la −1 color. El
  **680** (hito de 14 cuias con K = 13, D-027) solo deja quitar dos frutas y ninguna baja el fallo del bot 0,20: sigue solo con
  `plus_empty` (pregunta Q-B11-3). En la variante «a» de 301-700 (`tools/out/salida_a/`), igual, con el 680 como única excepción.
- **Datos regenerados** (con el generador, nunca a mano): `mundo_04_v1.json`, `mundo_05_v1.json` y `mundo_07_v1.json` a
  `mundo_09_v1.json` (solo cambia el `relief` de esos niveles), `tools/out/salida_a/mundo_07-09_v1.json`,
  `tools/out/campaign_v1_report.md`, `tools/out/curation_w07.md` a `curation_w09.md` y `tools/out/level_qa_report.md`. El
  `mundo_06_v1.json`, el tutorial, las muestras de la Tinaja y de las variantes, el pool del Reto y el registro de combinaciones
  no cambian (`--check`). La tabla de bytes y hashes de `tools/level-generator/README.md` está al día.
- **Cliente:** sin cambios de código (`ChooseRelief` ya elegía la primera `minus_color`); `CampaignPackTests` cuenta las 1.327
  variantes de la «b» (1.328 con la «a») y `ReliefMinusColorTests` ata la regla: desde el 31, todos con `minus_color` salvo el
  680.

### 1.2 Tutorial (02 §13; 06 §2.2)
- **Dedo del 2** (`TutorialFingerSetup`, parche `B11-tutorial`): la lección (una racha de 3 sobre un hueco de 2) no es legal en
  el tablero inicial, lleno, así que el dedo no salía hasta que el jugador, por su cuenta, hacía el vertido que la abría
  (prueba en el teléfono del 2026-10-20, observación 3). Ahora señala antes ese vertido (el primero de la solución si la abre;
  si no, el primero en orden de origen y destino que la deja legal y con solución) y, hecho, la lección [P] (Q-B11-1). Sonda
  `lesson = partial_setup`. Con el paso previo a la vista, la instrucción de TiTino no se va (habla de ese tablero); se va al
  salir la lección, como antes. Otro vertido durante el paso previo no cancela el dedo: vuelve a esperar sobre el tablero
  nuevo [P]. La búsqueda del paso previo corre en el hilo principal, como la del dedo del 1 y la lección del 2: solo cuando
  cambia el tablero, con una comprobación sin solver antes (`TutorialFingerSetup.LessonLegal`), y en el tablero del 2 (5 cuias,
  3 frutas) cada A* es inmediato.
- **Dedo de la Cuia extra del 8** (`TrayFinger`, `GameScreen.TutorialTips.cs`): la misma mano y la misma onda que el dedo del
  tablero, sobre «+1 cuia», tocando en bucle; sale cuando ya se fue el aviso de Capi (y cualquier otro aviso), se juega y la
  bandeja ofrece la del stock sin haber aplicado ninguna en el nivel; se va al usarla (02 §13 y 06 §2.2: «dedo guía en su
  primer uso»). En cada intento del 8 hasta usarla [P] (Q-B11-10). Con Reducir movimiento, mano fija. No captura toques.
- **Tooltip «Reiniciar es gratis…» del 6 y del 8** (`TipBubble`): dentro del panel de Aguas estancadas, en un hueco que el panel deja sobre
  «Volver a la orilla (gratis)» (`GameScreen.RestartTipRoomFor`, `StuckModal.RestartTipRoomDp`, con `RestartTipGapDp` = 4 dp de aire
  con la opción de arriba), con la punta del pico en el canto de arriba del botón y en su eje, sin tapar Marea [P] (observación 3
  de la prueba del 2026-10-22; antes iba encima del panel, con el pico en su borde); una vez por nivel mientras la
  app siga abierta (la primera vez que el modal se ve quieto en ese nivel; no en los reintentos ni al volver a entrar) [P]; se
  va con el modal. Texto canon de 04 §2.7 (`tutorial_restart_free_canoe`). Con
  `biz_feature_lives` apagado, solo la primera frase (`tutorial_restart_free`; D-038: sin canoas en los avisos; Q-B11-2).
- **Tooltip de la Cosecha doble del 11**: en la victoria del nivel de `ads_rv_double_harvest_from_level`, encima del botón y
  con el pico hacia él, en un hueco que el panel de victoria deja sobre el botón (`GameScreen.DoubleTipRoomFor`) para no tapar la
  línea «Cosecha +20» (observación 4 de la prueba del 2026-10-22); el hueco sube el escalón de la victoria compacta (D-032, punto 2; `ChooseVictoryCompact`) hasta el que lo deja caber,
  sin quitar las barras de la Crecida y la Canasta ni la fila de la Bitácora (las decide el presupuesto de siempre); si ni así cabe, el
  hueco que cabe, y el tooltip pisa las barras [P]. En esa victoria, en pantallas bajas, «Siguiente» puede ir algo más arriba que en las
  demás (Q7-A) [P]; cuando el panel ya subió y si el botón sale (con anuncio); se va al pulsarlo, al desactivarse o al dejar
  la victoria. Sin personaje (D-013, T1-A). Si en esa victoria no hay anuncio, no sale nunca (Q-B11-12). Texto [P]
  (Q-B11-4).
- Todo por reglas puras de `AF.GameFlow.TutorialTips` (sin motor; claves, nunca texto), con sondas `AF_STATE tutorial_tip` y
  los rectángulos `tip_restart`, `tip_double_harvest` y `extra_cuia_finger` en `AF_LAYOUT`. Las pruebas que no tratan de ellas
  las apagan (`PlayModeHelpers.NoTutorialTipsUnlessSet`), como el dedo guía.

### 1.3 La tarjeta de las Canoas del 31 (parche `B11-flow`)
- 02 §13: «31 Canoas + Cuia de rescate (tarjeta de 1 pantalla antes del nivel)»; 04 §7.1, «Debut: Nivel 31, con 5/5 y tarjeta».
  Texto de D-033, Q-E4 (a): «Canoas: si te rindes o sales del nivel, pierdes una. Vuelven solas cada 25 minutos.» [P]
  (`tutorial_canoes`).
- `LevelCardInfo.ForCanoes` (junto a `ForStreak`) y `LevelFlow.Canoes.cs`: tarjeta previa de 1 toque de un estreno sin
  etiqueta, como la de la Crecida del 25, con el ID del sistema `LevelCardInfo.CanoesIntro` = `lives` (glosario §6.3) y la canoa
  de la cabecera como icono provisional (`ProceduralArt.CanoesHeroIconKey`; 44 × 26 dp dentro del medallón de 48). En el
  primer nivel con canoas (`rescue_min_level`), solo en la campaña, solo al entrar (ni al reintentar ni al reanudar) y solo si
  las canoas cuentan: **con `biz_feature_lives` apagado (D-038) no sale**. Si el nivel trae su propia tarjeta, esa gana.
- La Cuia de rescate no tiene línea (Q-B11-9). Si el primer nivel con canoas trae su propia tarjeta (por Remote Config o un
  `level_overrides`), la de las Canoas no sale en ningún otro nivel [P].

### 1.4 «Volver al Mapa del río» en Sin canoas (parche `B11-nocanoes`)
- 04 §7.3, wireframe: la última línea es «Volver al Mapa del río» (B2-3 la dejó como «Volver al inicio» y lo anotó). El
  enlace cierra Sin canoas por el camino de siempre (desde el inicio, al inicio; desde un nivel, sale al inicio) y abre el Mapa
  encima del inicio, como su pestaña (`GameBootstrap.PressNoCanoesMap`). Si el Mapa no se puede abrir, se queda en el inicio.
  Deja la elección (`af_no_canoes_choice`, `choice = map`; 04 §11 y 08 §8.2) en `LastNoCanoesChoice` y en la sonda
  `AF_STATE no_canoes_choice`, como la del Reto (E7): ninguna elección de Sin canoas llega aún a `AF.Analytics`, y mandar solo
  esta sesgaría el embudo (para el integrador: todas a la vez). No gasta canoas. `af_map_open` lleva `source = lobby` (08 §8.2
  no tiene un valor para Sin canoas). El enlace llama al arranque por
  `GameBootstrap.Current`, como «Mientras esperas: Jugar el Reto del río» (E7), para no tocar los constructores ni
  `IGameActions`; sin arranque (la pantalla sola en un test) hace lo de siempre. La X de Sin canoas sigue volviendo al inicio
  (`LobbyScreen.BackButton`, el «Volver» de las pruebas, pasa a ser esa X).
- Con `biz_feature_lives` apagado (D-038) Sin canoas no se abre nunca, así que el botón no se ve (prueba
  `NoCanoesMapTests.WithoutLives_NoCanoesNeverOpens_D038`).
- El id de `AF_LAYOUT` sigue siendo `home` y `back_home` sigue en `strings.csv` (Q-B11-8).

### 1.5 `lvl_label_override` (parche `B11-labels`)
- 08 §9: json `{nivel: label}`, default `{}`; 02 §10: «etiqueta recalibrada en vivo con intentos medios por nivel». La etiqueta
  se muestra y paga (03 §7), así que la aplica el catálogo al servir el nivel (`LevelCatalog.Create(..., labels)`): el chip,
  la tarjeta previa, la cinta de «Siguiente» y la Cosecha la leen sin saber de dónde vino.
- `AF.Levels.LevelLabelOverrides` (sin motor): `normal`, `hard` o `very_hard`; una etiqueta que el nivel no puede mostrar se
  descarta (08 §9: `lvl_labels_from_level` = 40 y `lvl_very_hard_from_level` = 60 son el «primer nivel que puede mostrar
  Difícil / Muy difícil»); entradas inválidas, con aviso y sin romper el arranque. Se aplica al nivel servido, sea el publicado
  o su sustituto del kill switch.
- Fuente: `LevelLabelSource` (`{}` en release), como `level_overrides`, hasta que el arranque lea los dos de Remote Config
  (tarea del integrador). `af_label_shown` (`source ∈ {pack, override}`, 08 §8.2) aún no lo emite nadie: cuando se emita,
  `LevelCatalog.IsRelabeled(level)` (y `RelabeledLevels`) dice si la etiqueta vino de la tabla; `IsOverridden` sigue siendo
  solo el kill switch.

### 1.6 C10, el arrastre (parche `B11-drag`)
- 02 §3.1: «Dedo baja en X y sube en Y ≠ X tras desplazarse ≥ 24 dp → X→Y [P]; soltar sobre el fondo cancela»; la decisión se
  toma en el touch-up. `AF.GameFlow.DragPour` (regla pura) y `GameScreen.Drag.cs`: el arrastre es el toque en X y el toque en
  Y por `IGameActions.TapCuia` (el flujo y el núcleo deciden el vertido; C4). Un arrastre sobre una Y a la que no se puede
  verter (`PourRules.IsLegal`, del núcleo) no selecciona nada: Y se sacude con el sonido neutro del rechazo, como en C6, y X baja
  (sin C5) [P]. Si mientras tanto otro dedo seleccionó una cuia (C11), soltar fuera cancela. Entre cuias que se pisan, gana la
  que tiene el punto en su cuerpo; las vistas inactivas no cuentan. `CuiaView` guarda las posiciones
  de pantalla del touch-down y del touch-up. Los 24 dp son del lienzo, que siempre mide 360 dp de ancho, como todo el layout
  (en un teléfono de 412 dp son unos 27,5 dp físicos y en uno de 320, unos 21,3; Q-B11-5).
- Solo arrastra quien pulsó una cuia sin nada seleccionado y que puede ser origen (la que se eleva en el touch-down) [P]
  (Q-B11-11). `drag_pour_enabled` (08 §9, default true): `GameScreen.DragPourEnabled`, aún sin enlace a Remote Config, como
  `tap_switch_selection`.

### 1.7 El tablero a 320 dp (`BoardLayout320Tests`, en la rama)
- 02 §5: «a 320 dp de ancho se admiten 6 cuias por fila (16 cuias = 3 filas 6/5/5)». `BoardLayout` lo cumple a 320 dp con los
  altos de 19,5:9, 20:9 y 16:9: como mucho 6 por fila, ranura ≥ 48 dp (02 §3.3), cuia ≥ 28 dp, 16 cuias en 6/5/5 y, desde 13
  generadas, las añadidas tampoco pasan de 6.
- **Pero** el lienzo del juego mide siempre 360 dp de ancho (`GameScreen.CanvasScale`, D-028/DC3): en un teléfono de 320 dp se
  pinta a 0,89 dp físicos por dp de lienzo, así que ese layout de 320 dp no se usa y el texto de 12 sp sale a 10,7 sp físicos
  (02 §12 pide ≥ 12). Pregunta Q-B11-5.

### 1.8 La reserva del 20 % para `level_overrides` (03 §17): **parada**
- 03 §17, «Generación 1.0: 1.040 niveles (vuelta 1) + 20 % reserva»; 03 §15: un nivel en alarma «se sustituye por otro del pool
  del mismo slot vía `level_overrides`»; D-020: en la 1.0, con `mech_enabled_<id>` = false, los niveles se sustituyen con
  `level_overrides`. Hoy no hay ningún nivel de reserva publicado al que un `level_overrides` pueda apuntar.
  *Actualización del 2026-10-21:* decidida la (a) (D-033, añadido «la reserva del 20 %») y hecha en `tanda5/reserva-20`:
  `pack_schema` 4 con `kind` = `reserve` y `replaces`, y los packs `reserva_03_v1` … `reserva_09_v1` (03 §17).
- No se puede publicar sin cambiar el contrato 21: el `pack_id` solo admite `mundo_NN_vN`, `daily_pool_vN` y `event_*`, el `id`
  de campaña es `m{mm}_v{v}_n{nnnn}` con el número del nivel (dos tableros del mismo slot chocarían) y la `v` es la vuelta (la 2
  es la de 1.041+, 06 §10). Además el catálogo trata cada nivel de un pack como un nivel de campaña. Pregunta Q-B11-7, con
  opciones; nada generado.

## 2. Archivos

**En la rama** (todo nuevo salvo el generador, sus datos y `CampaignPackTests`):
- `tools/level-generator/Metrics/Relief.cs` (segunda pasada), `tools/level-generator/README.md`, `tools/level-qa/Checks/ReliefChecks.cs`
  (solo la descripción de QA-RELIEF-MINUS) y los datos regenerados del §1.1.
- `Scripts/Levels/LevelLabelOverrides.cs`; `Scripts/GameFlow/TutorialTips.cs` y `DragPour.cs`;
  `Scripts/Presentation/ProceduralArt.Canoes.cs`; `Scripts/UI/TipBubble.cs`, `TrayFinger.cs`, `TutorialFingerSetup.cs`,
  `TutorialTipsText.cs`, `LevelLabelSource.cs`, `GameScreen.TutorialTips.cs`, `GameScreen.Drag.cs` y
  `GameBootstrap.NoCanoesMap.cs`. Nada de ello se llama sin los parches.
- Tests: `Tests/EditMode/LevelLabelOverridesTests.cs`, `DragPourTests.cs`, `TutorialTipsTests.cs`, `ReliefMinusColorTests.cs`
  y `BoardLayout320Tests.cs`; `CampaignPackTests.cs` con las cuentas de variantes nuevas.

**Parches** (`docs/dev/traspaso/spark-enganches/`, LF, independientes entre sí; los `.meta` de los tests nuevos van dentro):

| Parche | Archivos | Qué |
|---|---|---|
| `B11-loc.diff` | `Data/Loc/strings.csv`, `Tests/EditMode/LocTablesTests.cs` | 5 claves en su sitio por orden ordinal: `no_canoes_map` (`borrador`), `tutorial_canoes`, `tutorial_double_harvest`, `tutorial_restart_free` y `tutorial_restart_free_canoe` (`pendiente_asesor`, como todas las `tutorial_*`), sin EN ni PT-BR; `TutorialTipsText.AllKeys` en `UiTextKeys` |
| `B11-flow.diff` | `GameFlow/LevelCardInfo.cs`, `GameFlow/LevelFlow.Cards.cs`, `UI/UiText.Cards.cs`, `Presentation/ProceduralArt.Cards.cs`; nuevos `GameFlow/LevelFlow.Canoes.cs` y `Tests/EditMode/CanoesCardFlowTests.cs` | La tarjeta de las Canoas del 31 (§1.3) |
| `B11-labels.diff` | `Levels/LevelCatalog.cs`, `UI/GameBootstrap.cs`; `Tests/EditMode/LevelLabelCatalogTests.cs` | `lvl_label_override` en el catálogo (§1.5); `IsOverridden` mira la lista de los sustituidos (un nivel solo reetiquetado no cuenta como override) |
| `B11-tutorial.diff` | `UI/GameScreen.Cards.cs`, `Tests/PlayMode/PlayModeHelpers.cs`, `Tests/PlayMode/TutorialFingerTests.cs`; `Tests/PlayMode/TutorialTipsHooksTests.cs`, `B11SnapshotTests.cs` | El dedo del 2 con su paso previo, los tooltips y el dedo de la Cuia extra (§1.2); los tests del dedo del 2 al día |
| `B11-drag.diff` | `Presentation/CuiaView.cs`, `UI/GameScreen.cs`; `Tests/PlayMode/DragPourHooksTests.cs` | C10 (§1.6) |
| `B11-nocanoes.diff` | `UI/NoCanoesScreen.cs`, `UI/LobbyScreen.cs`; `Tests/PlayMode/NoCanoesMapTests.cs` | «Volver al Mapa del río» (§1.4); `LobbyScreen.BackButton` (el «Volver» de las pruebas) pasa a ser la X de Sin canoas, que vuelve al inicio como antes |

## 3. Para el integrador
- Enlazar `drag_pour_enabled` y `lvl_label_override` (y `level_overrides`) con Remote Config en el arranque
  (`RcConfigBinder`/`GameBootstrap`); hoy van con su default de 08 §9.
- Si se publica la variante «a», el 680 sigue sin `minus_color` (Q-B11-3).
- `back_home` queda sin uso en Sin canoas (Q-B11-8).
- Las capturas de `B11SnapshotTests` (explícito, con gráficos) son las que citan las filas nuevas de `strings.csv`.

## 4. Cambios propuestos para el GDD
- 02 §13, fila 2: el dedo del 2 señala antes el vertido que abre la lección (Q-B11-1); filas 6 y 8: dónde sale el tooltip y
  que con `biz_feature_lives` apagado lleva solo la primera frase (Q-B11-2); fila 8: el dedo de la Cuia extra en cada intento
  hasta usarla (Q-B11-10).
- 04 §7.1, «Debut»: el texto de la tarjeta de Q-E4 (a) en lugar del obsoleto.
- 03 §13: la segunda pasada de `minus_color` desde el 31 (Q-N2 (a)) con su presupuesto [P] y la excepción del 680 (Q-B11-3).
- 06 §2.2, fila 11: el texto del tooltip de la Cosecha doble (Q-B11-4).
- 02 §5, «Dispositivos»: qué pasa con los teléfonos de 320 dp (Q-B11-5).
- 03 §17: el formato de la reserva (Q-B11-7).

## 5. Preguntas para el dueño (Q-B11-n; la primera opción es la recomendada y la aplicada, salvo en Q-B11-7, que para)

| Id | Pregunta | Opciones |
|---|---|---|
| Q-B11-1 | El dedo del 2 no puede señalar la lección al empezar (no es legal en el tablero inicial) | (a) señalar antes el vertido que la abre y después la lección [aplicada]; (b) dejarlo como DC6: sin dedo hasta que el jugador abra la lección, documentado; (c) cambiar el tablero del 2 para que la lección sea legal al empezar (regenera el tutorial y su par) |
| Q-B11-2 | Tooltip del 6 y del 8 con `biz_feature_lives` apagado: su segunda frase («Desde el nivel 31, quedarte sin vertidos cuesta una canoa») sería falsa | (a) solo la primera frase, «Reiniciar es gratis, siempre, mismo tablero.» [aplicada]; (b) sin tooltip; (c) el texto entero |
| Q-B11-3 | El 680 no tiene ninguna `minus_color` que alivie, ni con la segunda pasada | (a) aceptar la excepción: con la Cuia extra, su tercer fallo sigue con el tablero base (el resto, `plus_empty` como hasta ahora) [aplicada]; (b) en ese nivel, servir `plus_empty` con la Cuia extra (T + 2 = 16 ≤ 16, la (b) de Q-N2 solo aquí); (c) que la curación (Q-N8) cambie el 680 por un candidato con `minus_color` |
| Q-B11-4 | Texto del tooltip de la Cosecha doble del 11 (06 §2.2 no lo fija) | (a) «Con un video, las semillas de este nivel valen el doble.» [aplicada]; (b) «Ve un video y duplica tu Cosecha.»; (c) sin texto: solo un pulso del botón |
| Q-B11-5 | A 320 dp el lienzo se escala a 0,89 (siempre mide 360 dp), contra 02 §5 («a 320 dp se admiten 6 por fila») y 02 §12 (texto ≥ 12 sp físicos) | (a) lienzo al ancho del teléfono entre 320 y 360 dp, con 1 dp físico por dp (el tablero ya cumple a 320: `BoardLayout320Tests`; toca `CanvasScale` de `GameScreen` y `LobbyScreen` y sus pruebas, tarea del escritorio); (b) dejar el lienzo de 360 escalado y corregir 02 §5 y §12 |
| Q-B11-6 | «25 minutos» (`canoes_regen_min`) y «nivel 31» (`rescue_min_level`) van literales en los textos (el revisor lo señala contra «marcadores con nombre» de `.claude/rules/localizacion.md`) | (a) literales, como los fijó el dueño (Q-E4 (a)) y el canon de 04 §2.7: si Remote Config cambia esos valores, hay que revisar los textos [aplicada]; (b) marcadores (`{n}` con plural y `{level}`) con el valor de Remote Config: la tarjeta y el tooltip tendrían que recibir la configuración (hoy `UiText.Tutorial` no la tiene) |
| Q-B11-7 | Reserva del 20 % (03 §17): formato y publicación | (a) un pack de reserva por mundo con, para cada nivel con mecánica (los primeros que necesita el kill switch, D-020) y hasta el 20 %, otro tablero de su mismo slot sin esa mecánica, en la ventana del slot, con ids y `pack_id` propios (cambia `schemas/af-level-1.schema.json`: contrato 21, `pack_schema`) y que el catálogo solo use para `level_overrides`; (b) la reserva como candidatos sin publicar en el informe de curación hasta los packs remotos de la 1.1; (c) sin reserva en la 1.0. **Parada:** nada generado hasta la decisión |
| Q-B11-8 | El enlace de Sin canoas cambia de texto pero no de id | (a) dejar el id `home` de `AF_LAYOUT` y la fila `back_home` (otros usos) [aplicada]; (b) id `map` y quitar `back_home` |
| Q-B11-9 | La tarjeta del 31 (02 §13: «Canoas + Cuia de rescate») solo habla de las canoas | (a) solo las canoas (el texto de Q-E4) [aplicada]; (b) añadir una línea de la Cuia de rescate (texto nuevo) |
| Q-B11-10 | El dedo de la Cuia extra en los reintentos del 8 | (a) en cada intento hasta usarla [aplicada]; (b) solo en el primer intento, como el dedo de 1-2 |
| Q-B11-11 | C10 con otra cuia ya seleccionada, y sobre una Y a la que no se puede verter | (a) solo arrastra la cuia que se elevó en el touch-down sin selección (también si otro dedo selecciona entre medias, C11); un arrastre ilegal no selecciona nada: Y se sacude (C6) aunque pueda ser origen (sin C5) [aplicada]; (b) como dos toques en todo (con selección cambia de origen; sobre una Y ilegal que puede ser origen, C5 la selecciona) |
| Q-B11-12 | El tooltip de la Cosecha doble si en la victoria del 11 no hay anuncio | (a) solo en el 11: sin anuncio, no sale nunca [aplicada]; (b) en la primera victoria desde el 11 con el botón, con una marca nueva en el perfil (`progress`, 08 §4.3) |
| — | Presupuesto de la segunda pasada de `minus_color` | 2.000.000 [P] (el 620 necesita 1.962.635); se ve en §1.1 |

## 6. Riesgos
- El relief[0] de 5 niveles (358, 615, 618, 672 y 679) pasa de +1 vacío a −1 color por la regla de orden de 03 §13: el
  tercer fallo cambia de alivio en esos niveles (sin datos de jugadores aún).
- La segunda pasada cuesta hasta unos 90 s por nivel en la generación (el 620) y level-qa recalcula esos pares con 2.000.000.
- Arte provisional: el icono de la tarjeta de Canoas (la canoa de la cabecera) y los tooltips (el vidrio del bocadillo).
- Sin prueba en el teléfono: el dedo de la Cuia extra junto a la bandeja y los tooltips en 16:9.
- Los textos nuevos, sin EN ni PT-BR.

## 7. Cierre en el escritorio (rama `tanda5/b11-cierre`)

Lo que la nota dejaba «para el integrador» (§3) y las tareas de D-033, añadido de B11 («Quedan para otras tareas»), salvo la
reserva del 20 % y `level_overrides` (otro carril):
- **Remote Config:** `drag_pour_enabled` y `lvl_label_override` (con `lvl_labels_from_level` y `lvl_very_hard_from_level`) se
  leen en `RcGameplayBinder` (`RcConfigs.DragPourEnabled` y `RcConfigs.LevelLabels`). Remote Config se prepara ahora al principio
  de `Start` (`GameBootstrap.SetUpRemoteConfig`), antes de cargar el catálogo, para que la tabla de etiquetas (y, cuando llegue,
  `level_overrides`) se aplique al crearlo. `GameScreen.DragPourEnabled` lee el valor del arranque.
- **Analítica (08 §8.2):** `af_label_shown` con cada intento de un nivel Difícil o Muy difícil, justo tras `level_start`
  (`source = override` con `LevelCatalog.IsRelabeled`, si no `pack`) [P: por intento, como `level_start`]; y todas las
  elecciones de Sin canoas en `af_no_canoes_choice`: `wait` (la X o Atrás), `ad` (al empezar el vídeo), `seeds_refill` (cobrado),
  `iap_infinite` (al empezar la compra), `daily_challenge` y `map`.
- **Sugerencias de la revisión del Spark:** `LevelDefinition.WithLabel` (comparte los arrays del nivel, que es inmutable) en lugar
  de `LevelLabelOverrides.Relabel`; `IGameActions.PressNoCanoesMap` en lugar de `GameBootstrap.Current`; `GameScreen` ya no
  comprueba dos veces `LessonLegal` (`TutorialFingerSetup.TryChooseWhileIllegal`).
- **La fila «Bitácora · elige tu premio»** (`GameScreen.LogbookRow.cs`, `VictoryBars`): la tercera barra, de 48 dp, bajo la Crecida
  y la Canasta; abre la libreta encima de la victoria y se va al sellar. Solo cuenta en la victoria compacta de la victoria que la
  lleva; si con ella hiciera falta el escalón 3 (donde iría tras el chevron), no sale y la libreta sale sola al inicio [P]. Hoy
  sale a 360 × 780 y no a 360 × 640 (medido en `LogbookVictoryRowTests`). No sale en el 10 ni en el Reto.
- **Q-B11-5 (a):** `GameScreen.CanvasScale(width, height, pxPerDp)` con `DisplayMetrics.density` en Android: de 320 a 360 dp
  físicos, el lienzo mide el teléfono (1 dp físico por dp); por debajo, 320; desde 360, como antes; luego el alto mínimo de 640.
  Lo usan la pantalla de nivel, el inicio y las capas de encima (`OverlayCanvas`). `Canvas320Tests` y `Canvas320SnapshotTests`.
- **GDD:** 02 §5 y §13, 03 §13, 04 §7.1, 05 §7 (UMP dentro de MAX; doble confirmación en lugar del PIN) y 08 §7 (el PIN), con
  su decisión citada en cada cambio.
