# Salto del Boto: nota del bloque B3

Nota del bloque B3 de la tanda 5 (`docs/dev/plan-tanda5.md` §4; encargo §12 de `docs/dev/traspaso/spark-reto-del-rio.md`).
Reúne lo que el GDD fija, dónde va en el cliente y lo que queda por decidir. **No sustituye al GDD:** ante una contradicción
mandan 04 §2.6, §5.2, §6.1-§6.3, §6.5 y §11; 02 §2 (SKIP_RESOLVE) y §7; 08 §4.3, §8.2 y §9; el glosario §5.1 y §10, y
`docs/dev/crecida-canasta.md` (C11, K8, K9 y E34). Lo marcado [P] es propuesta de esta nota y lo decide el dueño (por
defecto, la recomendada).

Rama `spark/t5-b3`, desde `origin/integracion-fase1-fase2` `9982bdc` (con B1a integrado).

## 1. Reglas (con su fuente)

| # | Regla | Fuente |
|---|---|---|
| S1 | Se ofrece con las tres condiciones a la vez: `fails_level ≥ skip_min_fails` (5; fallos duros y blandos), ≥ `skip_min_gap_levels` (10) niveles desde el último salto y nivel ≥ `skip_min_level` (31); además, `skippable` = true (false en los estrenos). | 04 §2.6 «Regla de uso», §6.1 `skip_eligible`; 08 §9 |
| S2 | Cuesta `skip_seed_price` (300) semillas; sin saldo, el precio en gris y «Te faltan N». Sin anuncio ni usos gratis. | 04 §1, §2.6; 05 §2.2 |
| S3 | Marca el nivel como superado sin recompensa (0 semillas), lo añade a `skipped_levels`, `since_last_skip` = 0, `fails_level` = 0 y carga el siguiente. | 04 §2.6, §6.2 `SKIP`, §5.2; 02 §2 SKIP_RESOLVE |
| S4 | No es un fallo: no cuesta canoa ni toca la Crecida (ni la rompe ni la avanza). | 04 §2.6 a, §5.2; C11 |
| S5 | Cuenta para la Canasta de cada 10 niveles: el salto de un `10k` paga su Canasta. | 04 §2.6; K9, E34 |
| S6 | Aparece en Aguas estancadas (rama D, 3.ª opción, estilo secundario, tras el rescate y las ofertas; también en A' y F «si elegible»), en el menú de pausa (icono de Rosa) y como banner «Rosa puede llevarte río arriba · 300» en la entrada del nivel. | 04 §2.6 «Inventario y HUD», §6.2, §6.3, §6.6 D |
| S7 | Confirmación de una línea; si el nivel tenía Cuia extra aplicada, se pierde sin reembolso (aviso en la confirmación). | 04 §2.6, c |
| S8 | El Reto del río y los eventos no admiten Salto. | 04 §2.6 e; 06 §4.1 |
| S9 | El nivel saltado queda marcado en el Mapa con Rosa; en 1.0 no se rejuega. | 04 §2.6; 06 §4.5 |
| S10 | `af_skip_used` con `fails_level`, `since_last_skip` y `level_tag`. | 04 §11; 08 §8.2 |

## 2. Dónde va

**Archivos nuevos de la rama** (sin tocar nada existente):

- `Scripts/Boosters/SkipBooster.cs`: `SkipConfig` (`skip_min_fails`, `skip_min_gap_levels`, `skip_min_level`, con sus
  `[RemoteConfigKey]`; el precio sigue en `EconomyConfig.SkipSeedPrice`), `SkipInput`, `SkipOffer` (`Hidden`/`Ready`/`Disabled`)
  y `SkipBooster` (`LastSkipLevel`, `LevelsSince`, `IsEligible`, `Evaluate`, `Pay`). Sin motor y sin asignaciones.
- `Scripts/GameFlow/SkipStore.cs`: `ISkipStore` (`SkippedLevels`, `SaveSkip`), `ProfileSkipStore` (perfil: `skipped_levels`,
  `max_level`, `next_level`, `since_last_skip` = 0; no suma `stats.levels_won`) y `MemorySkipStore` (tests).
- `Scripts/GameFlow/LevelFlow.Skip.cs`: `UseSkip`, `SkipOffer`, `SkipLosesExtraCuia`, `Skip()` y `LastSkip` (`SkipResult`).
  `Skip()` hace, **en un solo lote del perfil** y en este orden: `RequireDurable` → cobro (`Transaction.Buy(SeedSink.Skip)`;
  si se rechaza, nada más) → `OnSkipStreak()` (E34: dentro del lote y **antes** de guardar el salto, porque compara con el
  `max_level` anterior) → `ISkipStore.SaveSkip` → borra el `level_state` → `Load(N + 1)`. El cobro, la Canasta, el salto y el
  `level_state` del nivel siguiente salen en una escritura (`SkipFlowTests.SkippingA10k_PaysItsBasket_AndEverythingGoesOutInOneWrite`).
- `Scripts/UI/SkipText.cs` (claves), `SkipUi.cs` (`ISkipActions`, `SkipConfirm` y `SkipBanner`), `GameBootstrap.Skip.cs`
  (`SetUpSkip`, `ISkipActions`, `TickSkipBanner`), `SkipRunner.cs` (avanza el banner) y `DevProbe.Skip.cs` (`af_skip_used`).
- Tests: `SkipBoosterTests`, `SkipFlowTests` y `SkipProbeTests` (EditMode).

**Parches para el integrador** (`docs/dev/traspaso/spark-enganches/`, en LF, probados en un worktree con los dos aplicados):

1. `B3-model.diff`: `StuckModalModel.cs` (`StuckContext.Skip` y `StuckModalView.Skip`, la rama D) y `LevelFlow.Funnel.cs`
   (`BuildStuckContext` rellena `Skip = SkipOffer`); añade `StuckModalSkipTests`.
2. `B3-ui.diff` (después de B3-model): `StuckModal.cs` (la opción «Salto del Boto» con su precio y «Rosa te lleva…», o en gris
   con «Te faltan»), `PauseMenu.cs` (el botón con Rosa y el precio), `GameScreen.cs` (pasa `flow.SkipOffer` a la pausa y su
   acción) y `GameBootstrap.cs` (`SetUpSkip();` tras `SetUpStreak();`); añade `SkipPlayModeTests`.
3. `B3-strings.csv`: las 6 claves nuevas (`stuck_skip`, `skip_tagline`, `skip_confirm_title`, `skip_confirm_extra_cuia`,
   `skip_confirm_cta`, `skip_banner`) con `es-419`, `context`, `max_chars` y `pendiente_asesor`; `en` y `pt-BR` vacías. No toca
   `strings.csv`.

**Perfil:** no hace falta parche. `Profile.cs` y `ProfileJson.cs` ya guardan `progress.since_last_skip` y
`progress.skipped_levels` (08 §4.3); `last_skip_level` se deduce de `skipped_levels` (Q-S1). **Mapa del río:** no hace falta
parche de `RiverMapScreen*`: ya pinta a Rosa en las boyas de los niveles `≤ max_level` que están en `skipped_levels`
(`RefreshBuoyStates`, `RefreshPastRosa`) y la franja de «ponerse al día» ya lleva `skipped`. Lo único que faltaba era que el
salto escribiera los dos campos, que es lo que hace `ProfileSkipStore`.

**`max_level` (aviso del revisor de B1a):** el nivel saltado **sí** cuenta para `max_level`. 04 §2.6 lo marca «como superado»; el
Mapa (`level ≤ max_level` y `IsSkipped` → Rosa), la Canasta deducida (K8) y `FactSchedule.IsLevelRevealed` ya lo leen así. Lo
comprueban `SkipFlowTests` (36 y 40 quedan en `max_level`, `skipped_levels` y `next_level` = N + 1, sin `levels_won`) y la
Canasta no se repaga (el Salto ya no se ofrece en un nivel `≤ max_level`).

## 3. Preguntas (se avanzó con la recomendada, marcada [P])

| Id | Pregunta | Opciones | Recomendada |
|---|---|---|---|
| Q-S1 | 02 §2 dice `last_skip_level = N`; 04 §6.1-§6.2 y 08 §4.3 usan el contador `since_last_skip`. ¿Con qué se mide el hueco? | (a) `last_skip_level` = el mayor de `skipped_levels` por debajo de N, sin campo nuevo; hueco = N − `last_skip_level` (N si no hay salto); `since_last_skip` se pone a 0 al saltar (04 §6.2) y no se lee; `af_skip_used.since_last_skip` = ese hueco · (b) `since_last_skip` como contador que sube con cada nivel superado (parche de `ResolveWin`), y el primer salto pide 10 niveles superados · (c) campo nuevo `progress.last_skip_level` | **(a)** [P]: sin campo nuevo, a prueba de la fusión de la nube (unión de `skipped_levels`) |
| Q-S2 | «≥10 niveles desde el último salto»: ¿se puede saltar el 50 tras saltar el 40? | (a) sí: N − último ≥ 10 («máximo 1 cada 10 niveles», glosario §5.1) · (b) no: 10 niveles superados entre medias (el 51) | **(a)** [P] |
| Q-S3 | El banner «en la pantalla previa del nivel»: ¿dónde y cuándo? La tarjeta previa (LEVEL_CARD) solo sale en los niveles con etiqueta o estreno, y nunca al reintentar | (a) una franja de madera con Rosa arriba del tablero mientras la opción se ofrece y el intento no empezó (tarjeta previa, intro o tablero sin vertidos); un toque abre la confirmación · (b) solo dentro de la tarjeta previa cuando sale · (c) en el inicio, junto a «Jugar» | **(a)** [P] |
| Q-S4 | ¿Y si no hay nivel siguiente (fin del contenido) o no quedan canoas para empezarlo (las infinitas caducaron a mitad)? | (a) el Salto no se ofrece · (b) se ofrece y lleva a «El río sigue creciendo» o a Sin canoas (parche de `AppFlow`) | **(a)** [P]: casos raros, y `AppFlow` es del escritorio |
| Q-S5 | La animación de Rosa (1,5 s, 04 §2.6 «VFX / SFX»; `rosa_jump`, `sfx_skip`) y el dato del boto al llegar | (a) esperan al arte y al audio: hoy el siguiente nivel entra con su intro de siempre · (b) una transición provisional de 1,5 s por código | **(a)** [P] |
| Q-S6 | `stuck_skip` de 04 §6.5 es «Salto del Boto · 300 · Rosa te lleva río arriba, al siguiente nivel» en una cadena | (a) se parte: la etiqueta «Salto del Boto», el precio en su etiqueta de semillas (como «Continuar: +1 cuia») y la línea `skip_tagline` debajo · (b) la cadena entera en el botón | **(a)** [P]: sin cifras en el texto, y la línea se reutiliza en la confirmación |
| Q-S7 | ¿En qué pantallas se puede saltar? | (a) Aguas estancadas, la pausa (INTRO y PLAYING) y el banner; no en ¿Seguro?, Nivel fallido ni Sin canoas · (b) también en Nivel fallido | **(a)** [P]: lo que nombra 04 §2.6 |
| Q-S8 | Textos sin canon: el título de la confirmación, el aviso de la Cuia extra y el CTA | (a) «¿Saltar el nivel?», «Perderás la Cuia extra de este nivel» y «Saltar nivel» (glosario §10) · (b) otros que proponga el asesor | **(a)** [P], `pendiente_asesor` |
| Q-S9 | `skip_min_level`: 05 (tabla de compuertas) dice «con mínimo 31 en RC»; 08 §9 da el rango 20-31 | (a) se corrige 05 a «20-31» o 08 §9 a «31»: lo decide el dueño; el cliente solo exige ≥ 1 y no aplica rangos (eso es de Remote Config) · (b) el cliente fuerza ≥ 31 | **(a)** [P] |

## 4. Riesgos y pendientes

- **Alto del panel de Aguas estancadas:** la rama D suma unos 70 dp. El caso ya registrado de 16:9 al 130 % (rama B por encima
  de la zona segura, `MinTextSizeTests.Stuck_130_16x9_InsideTheSafeArea_KnownIssue`, explícito) empeora si coinciden B y D.
  No se mide aún la rama D en `MinTextSizeTests` (archivo de test existente: no se toca).
- **Analítica:** `af_skip_used` sale solo por la sonda de desarrollo (`AF_STATE`) hasta que exista `AF.Analytics` (B7).
- **Remote Config:** `SkipConfig.Defaults` hasta que haya lector (como `StreakConfig`).
- **Tests con progreso en memoria** (`LaunchOptions.ProgressStoreOverride`): el arranque usa `MemorySkipStore`, que apunta el
  salto y `next_level` en el `MemoryProgressStore` pero no sube su `max_level` (no tiene cómo).
- **Arte:** Rosa sale con el sprite procedural del Mapa (`ProceduralArt.Rosa`); el icono definitivo es del artista.
