# Prueba del dueño en el teléfono (2026-10-11): tareas para el escritorio

El dueño probó el `dev.apk` en el SM-S938B. El APK se compiló en el portátil sobre `80d37fb` o antes; las cuatro causas siguen en `7677085`. Sus decisiones están en **D-011** (`docs/dev/decisiones.md`). Esta hoja da, para cada punto, el síntoma, la causa en el código de hoy, el arreglo, cómo comprobarlo y qué probar luego en el teléfono.

- Es todo Unity: `unity-gameplay-dev`, y después `code-reviewer`.
- Núcleo, solver, packs y Remote Config no cambian.
- Hazlo **antes del paso 2** del traspaso: es lo que el dueño vio en el teléfono.
- P1, P2 y P4 tocan `GameScreen` y pueden ir en una sola tarea. P3 (miquitos) es independiente y puede ir en paralelo, en otro worktree.

## P1 · La Cosecha y la bandeja no se adelantan a la victoria (D-011, punto 1)

**Síntoma.**
- Con el último vertido, el saldo de semillas de arriba ya sube antes de que se vea la victoria. Después, en la pantalla de victoria, la Cosecha cuenta, pero el saldo parece no sumar: ya las tenía.
- Los comodines de la bandeja se apagan en ese mismo instante.

**Causa.**
- `LevelFlow.EvaluateStatus` (`LevelFlow.cs:611`) llama a `ResolveWin` en el instante lógico. `ResolveWin` (`LevelFlow.Victory.cs:113`) cobra la Cosecha con `Transaction.Win`, y eso es correcto (02 §14).
- La vista espera a que acabe la animación (`TickPending`, `LevelFlow.cs:642`) y luego pasa por la celebración (900 ms) antes de la pantalla de victoria.
- `GameScreen.PendingHarvest` (`GameScreen.cs:2228`) solo descuenta la Cosecha en `VictoryScreen` y `AdGate`. Mientras termina el último vertido (`Playing` con la victoria pendiente) y durante `Celebration`, devuelve 0, y el saldo enseña ya la Cosecha.
- `BoosterTray.Refresh` (`BoosterTray.cs:363`) calcula el habilitado con `AcceptsBoardInput` / `CanUndo` y `ExtraCuia`, que pasan a falso o Disabled en cuanto hay victoria pendiente: los botones se ponen grises.

**Arreglo.**
- En `LevelFlow`, una propiedad de solo lectura, por ejemplo `IsResolvingWin => Phase == FlowPhase.Playing && _pending == Outcome.Win`. Ya existe `IsResolving` (`LevelFlow.cs:183`).
- `PendingHarvest`: con `IsResolvingWin` o en `Celebration`, devuelve `flow.HarvestSeeds` entero. En la victoria, igual que hoy.
- `BoosterTray.Refresh`: con `IsResolvingWin`, no repinta (se queda el último estado). Los toques siguen bloqueados por `AcceptsBoardInput`. En `Celebration` se apaga como hoy.
- La Cosecha se sigue cobrando en el instante lógico: no se toca `ResolveWin`.

**Pruebas.**
- **PlayMode, saldo:** ganar un nivel. Desde el toque del último vertido hasta `OnVictoryScreen`, el número del saldo (WalletBar) sigue siendo el de antes de ganar. En la victoria sube con la cuenta de la fila y termina en el saldo de antes más la Cosecha.
- **PlayMode, bandeja:** mientras se resuelve la victoria, la bandeja conserva el habilitado de antes del último toque, y un toque en Marea no hace nada.
- **EditMode:** `IsResolvingWin`.

## P2 · Avisos legibles (D-011, punto 2)

**Síntoma.**
- En el nivel 8 salió un aviso que no dio tiempo a leer: el regalo de Capi, `UiText.ExtraCuiaGift`, de 81 caracteres.
- En un nivel anterior salió la tarjeta «¡Miquito a la vista!» y tampoco se pudo leer entera.

**Causa.**
- Duración fija para cualquier texto: `GameScreen.ToastMs` = 2.600 ms (`GameScreen.cs:82`, usada por `ShowToast`, que sirve para todos los avisos del tablero) y `SightingRule.CardMs` = 2.500 ms (`SightingRule.cs:29`).
- El regalo de Capi sale al cargar el nivel: `LevelFlow.cs:245` llama a `OnExtraCuiaGifted`, y este (`GameScreen.cs:1181`) llama a `ShowToast` en el acto, encima de la tarjeta previa y de la intro con los miquitos.

**Arreglo.**
- **Duración por lectura** en `Toast` y `SightingCard`: `1.000 ms + 55 ms × caracteres visibles`, acotada a 3.000-7.000 ms [P].
  - Una constante y una función compartidas, sin asignaciones.
  - Para la tarjeta cuentan el título, el nombre y el dato.
  - Quedan fuera:
    - el cartel «El río bajó» (`_banner`, 2,0 s, 04 §2.7);
    - el rótulo del intento (`AttemptBannerMs`).
- **Ningún aviso durante LOADING, la tarjeta previa (`LevelCard`) ni la INTRO.**
  - Si llega antes, queda pendiente y sale al habilitarse el toque, en `OnIntroFinished`.
  - Así lo hace ya el regalo de la Luciérnaga: `_revealGiftPending` y `ShowRevealGiftIfReady` en `GameScreen.Reveal.cs`. Reutiliza ese patrón para el regalo de Capi y para cualquier `ShowToast` temprano.
- **Uno cada vez:** si hay un aviso a la vista y llega otro, o la tarjeta del miquito, el segundo espera a que se vaya el primero. No se cortan ni se pisan.
- Siguen sin capturar toques.

**Pruebas.**
- **EditMode:** la función de duración (extremos y el texto de Capi ≈ 5.455 ms).
- **PlayMode, nivel 8:** el aviso de Capi no está visible durante la intro, sale al habilitarse el toque y sigue visible a los 5 s.
- **PlayMode, tarjeta:** ajusta `IntroTests.cs:629`, que hoy avanza `SightingRule.CardMs + 50`.
- **PlayMode, cola:** dos avisos seguidos no se pisan.

## P3 · Miquitos con más presencia, sin frenar el juego (D-011, punto 3)

**Síntoma.** Los miquitos entran y se van tan rápido que no se aprecian.

**Causa.**
- **Entrada:** `IntroChoreography.Schedule` fija `TroopEnterMs = IsLong ? Row1FullMs : 0f` (`IntroChoreography.cs:1400`). En la versión corta (desde el 21) ya están posados en t = 0; en la larga entran cuando la fila 1 ya está llena.
- **Salida:** `FinishMonkeys` (`IntroChoreography.cs:2010`) los saca en cuanto acaban la suelta (ExitStartMs ≥ T), en `ExitMs` = 300 ms. En la corta, la tropa se ve ≈ 1,1 s en total.
- **La tarjeta:** `GameScreen.MaybeShowSighting` (`GameScreen.cs:1763`) la pone en la misma franja que la rama («panorama ≥ 64 dp»).

**Arreglo.**
- **Entrada visible**, también en la versión corta: trepan por la rama desde el follaje de la esquina en ≈ 500 ms mientras el río llena (fases a-b), sin retrasar la primera suelta.
- **T no cambia:** T, las fases a-d, el calendario de la fruta y el instante en que se habilita el toque (`LevelFlow` → `FinishIntro`) siguen igual.
- **Reposo de ≈ 1.500 ms tras la última suelta, ya en juego**, en la cola de la intro (`IntroAnimation`: `_tail`, que hoy ya sigue después de T):
  - un gesto natural de reposo: comer la fruta, rascarse, mirar alrededor o balancear la cola;
  - el gesto se elige de forma determinista por nivel, sobre la variación que ya hay (lado de entrada, quién lidera y reposo; F13 de D-010) y con los rasgos de reposo de la especie;
  - nada de saludar ni señalar (D-010, punto 6), ni mirar o reaccionar a una cuia, a la selección o a un vertido (D-002).
- **Salida** trepando en ≈ 500 ms (`ExitMs`; hoy 300).
- **Al saltar con un toque** (`IntroAnimation.Complete(skipped: true)`), se van como hoy. Al cargar otro nivel (`Clear`), desaparecen en el acto.
- **La tarjeta «¡Miquito a la vista!»** espera a que la tropa haya salido. Encaja con la cola de P2.
- Siguen sin recibir toques (Canvas propio, `raycastTarget = false`) y sin asignaciones por fotograma: arrays preasignados del planificador.
- La cuia que vierte pasa por delante de la tropa (D-006).
- Sin tropa (modo dosel) no cambia nada.

**Pruebas.**
- **Ajusta las que fijan los tiempos de la tropa:** tests del planificador y de `TroopLayer` e `IntroTests`.
- **Calendario:** el de la fruta y T son idénticos a los de antes (mismo nivel y misma semilla).
- **Toque:** el toque se habilita en T.
- **Tropa en juego:** la tropa sigue a la vista ≥ 1.200 ms después de T y fuera a los ~2,2 s.
- **Salto:** saltar la intro la retira.
- **Asignaciones:** 0 B por fotograma.
- **Capturas** de la versión larga (nivel 2) y de la corta (nivel 21): entrada, suelta, reposo y salida.

## P4 · La Cosecha doble avanza sola (D-011, punto 4)

**Síntoma.** Tras «Cosecha doble: x2 por video» y el vídeo, el juego vuelve a la pantalla de victoria y hay que tocar «Siguiente».

**Causa.**
- `GameBootstrap.PressDoubleHarvest` (`GameBootstrap.cs:273`): al recibir la recompensa, solo llama a `flow.ApplyDoubleHarvest()`.
- `GameScreen.OnHarvestDoubled` (`GameScreen.cs:1081`) cuenta de N a 2N y se queda en la victoria.

**Arreglo.**
- Tras `OnHarvestDoubled`:
  - la fila cuenta (400 ms, `GameConfig.HarvestCountMs`);
  - se ve ≈ 600 ms más [P];
  - después, la vista llama a la misma acción que «Siguiente» (`_actions.PressNext()` → `AppFlow.PressNext`), solo si la fase sigue siendo `VictoryScreen`.
- Durante esa espera:
  - «Inicio» y la Cosecha doble quedan inactivos;
  - «Siguiente» puede adelantarla, y entonces no se llama dos veces.
- En AD_GATE no sale el intersticial, porque `AdPolicy` bloquea con `AfterRewarded` 60 s tras un recompensado (`AdPolicy.cs:193`). Compruébalo en la prueba, no lo fuerces.
- Sin recompensa (vídeo cerrado antes de tiempo), nada cambia: se queda en la victoria.

**Pruebas** (PlayMode, nivel ≥ 11 con el anuncio simulado):
- **Con recompensa:** tras la recompensa, el saldo termina en el de antes más 2N y el flujo carga N + 1 sin tocar nada, sin intersticial.
- **Sin recompensa:** se queda en `VictoryScreen`.
- **«Siguiente» durante la espera:** carga N + 1 una sola vez.

## Documentación (con P1-P4)

- **GDD:**
  - 02 §2 (fila de VICTORY_SCREEN) y §6: tras la Cosecha doble, se avanza solo;
  - 07 §6.4: tarjeta con duración por lectura, tropa que entra, reposa y sale.
- **Guion de la intro:** §4.5, §7.1-§7.3 y el criterio A15 (`docs/research/intro-rio-miquitos/guion.md`).
- **Registro de decisiones:**
  - la nota de F3 y F4 de la tabla de D-010 («de 2,5 s»);
  - el estado de D-011 en la tabla de `docs/dev/decisiones.md`, al implementarlo.

## Verificación y entrega

1. La sección 3 del traspaso completa. Si solo se tocó Unity, al menos `unity-compile`, EditMode y PlayMode.
2. Commit y push de `integracion-fase1-fase2`. Explica en el mensaje qué probar en el teléfono:
   - **P1:** nivel 1 o 2: con el último vertido, el saldo no sube hasta la pantalla de victoria y la bandeja no se apaga antes de la celebración.
   - **P2:** nivel 8 (sin haberlo superado: `--ei af_level 8` con un perfil nuevo): el aviso de Capi sale tras la intro y se lee entero. Un nivel con tarjeta de miquito (el primer nivel libre de la especie, 07 §6.4).
   - **P3:** niveles 2 y 21: los miquitos entran, sueltan, se quedan un momento mientras ya se juega y se van.
   - **P4:** nivel 11 o posterior: «Cosecha doble» → vídeo simulado → pasa solo al nivel siguiente.
