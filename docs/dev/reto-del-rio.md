# Reto del río (`daily_challenge`): nota de diseño

Tarea R1 del encargo `docs/dev/traspaso/spark-reto-del-rio.md` (equipo Spark), rama `spark/reto-del-rio`. Estado y verificaciones: `docs/dev/traspaso/spark-estado.md`.

**Fuentes que mandan** (orden de `CLAUDE.md`):
- `docs/GDD.md`: §2 #9 y #21; §7.2, incidencia 5; contrato 21.
- `docs/gdd/03` §4.4, §4.5, §13, §14, §16 y §17.
- `docs/gdd/06` §2.2, §2.3, §4.1, §4.3 y §4.7.
- `docs/gdd/05` §2.1, §3.1 y §3.2.
- `docs/gdd/04` §2.1-§2.8, §5.2, §7.1, §7.3 y §11.
- `docs/gdd/08` §2.1, §2.2, §3, §4.1, §4.3, §6.2, §8.2, §9 y §10.1.
- Glosario (00): §8, §9 y §11.
- Esquema `schemas/af-level-1.schema.json`.

Leyenda: **[P]** = propuesta de esta nota o del GDD, sin decisión del dueño. Las R-Qn son decisiones del dueño (sección 15): R-Q1-R-Q10 del encargo §5, y R-Q1b y R-Q11-R-Q16, decididas con la opción recomendada (el dueño: «aplica las recomendaciones»; el integrador: «si tienen recomendada, vamos con esas»).

---

## 1. Discrepancias (leer primero)

### 1.1 R-Q1 no se podía cumplir en B2 con K = 9 ni en B3. **Decidido: R-Q1b (a)**

Pares de riesgo de 03 §4.5: 4-5, 5-6, 6-13, 13-14, 14-4, 3-12, 12-11, 11-10, 8-9, 1-2. Debuts del glosario §9. El recuento es sobre el conjunto de tintes, como `RiskCount` de `tools/level-generator/Generation/TintAssigner.cs`.

| Banda | Tintes estrenados en su tramo (R-Q1) | Pares presentes | K (03 §16) | Pares mínimos con ese K | Tope R-Q1 | ¿Cabe? |
|---|---|---|---|---|---|---|
| B1 | 1-40: 1, 3, 4, 6, 7, 8, 10 (7 tintes) | ninguno | 6-7 | 0 | 0 | Sí |
| B2 | 1-80: los anteriores + 5 (debut 41) + 14 (debut 61) = 9 | 4-5, 5-6, 14-4 | 8-9 | K = 8: 1 (sin el 4 o sin el 5) · **K = 9: 3** | 1 | Solo con K = 8 |
| B3 | 1-200: los anteriores + 2 (81) + 11 (111) + 9 (141) = 12 | los 3 de B2 + 11-10, 8-9 y 1-2 = 6 | 10-11 | **K = 10: 3 · K = 11: 4** | 2 | No |

03 §4.5 lo anticipa: «El conjunto máximo de tintes mutuamente seguros es 7», y la campaña deja `max_pares_riesgo` libre desde el 201. 03 §11 recoge que en 101-200 la campaña juega como mucho K = 9 con tope 2.

**Decisión R-Q1b (a):** **B2 solo con K = 8** (cambia 03 §16 y 06 §4.1, que dan K 8-9) y **B3 con `max_pares_riesgo` libre** (cambia el tope 2 de R-Q1; es lo que hace la campaña desde el 201, 03 §4.5). B1 sin cambios. Conserva la paleta de R-Q1 y la dificultad de B3.

Opciones descartadas: (b) subir los topes (B2 a 3, B3 a 4); (c) mantener los topes y bajar K (B2 = 8, B3 = 9; B3 quedaría por debajo del K de los hitos, 03 §11); (d) ampliar la paleta de B3 a 1-300 (no basta: el 13 añade 6-13 y 13-14).

**Aplicación:** en `tools/level-generator/specs/daily_v1.json`, B2 con `k: [8, 8]` y sin la regla `deep` de K = 9, y B3 sin tope de pares; luego se rehacen la muestra y su informe (encargo §9, punto 1, antes de R5). `level-qa --daily` ya mide el tope por banda contra la especificación (R4), así que no cambia.

### 1.2 `IServerDate` va en AF.Platform (manda el GDD). **Confirmado por el integrador**

El encargo (§4, «Servidor») la ponía en `AF.LiveOps`, pero 08 §2.2 asigna a `AF.Platform` el «`Date` del servidor para el reloj» (y lo entrega a LiveOps), y 08 §10.1 habla de `Clock.ServerOffset`. Va en `Assets/_Project/Scripts/Platform/IServerDate.cs` y `FakeServerDate.cs` (archivos nuevos, R7b). El integrador lo confirmó en el encargo §5 («van en AF.Platform (08 §2.2), no en AF.LiveOps»). AF.LiveOps ya referencia a AF.Platform.

En el mismo ensamblado vive `ClockSecurityConfig.cs` (R7b), **única fuente** de `sec_clock_max_drift_h` (sección 11): así AF.Economy (Canoas, 04 §7.1 «Reloj») también podrá leerla.

### 1.3 Decisiones del dueño que el GDD aún no recoge (las lleva el integrador; `docs/gdd/**` está prohibido para el Spark)

- **R-Q6** cambia el tipo de `daily_overrides`: en 08 §9, de `json {fecha: level_id}` a `{fecha: {banda: id}}`; en 06 §4.1 («Sustituciones» y el `pool_overrides` del pseudocódigo), igual. El default `{}` no cambia.
- **R-Q5 y R-Q4** amplían el bloque `daily_challenge` de 08 §4.3:
  - R-Q5: última fecha cobrada, premio pendiente de validar y sellos del mes;
  - R-Q4: reanudar necesita además un campo de partida en curso que R-Q5 no nombra. Propuesta [P] `in_progress` (sección 10).
- **R-Q7** cambia el pseudocódigo de 06 §4.1: `sellos_consecutivos == 7` pasa a «cada racha completa de 7»; el marco de los 20 sellos queda fuera.
- **R-Q8** amplía la fila «Reintentos» de 06 §4.1 («Marea y Cuia extra permitidas, Morpho permitida»): se suman la Pororoca y la Luciérnaga. Se mantienen las exclusiones del GDD (Cuia de rescate, 06 §4.1; Salto del Boto, 04 §2.6 e).
- **R-Q1b** cambia 03 §16 y 06 §4.1 «Bandas»: B2 pasa de K 8-9 a K 8, y B3 queda sin tope de pares de riesgo (R-Q1 decía 2).
- **R-Q11** fija en 03 §16 que la ventana del bot es compuerta: un día sin candidato dentro de ella no se publica.
- **R-Q12, su aclaración y R-Q12b** concretan la reconciliación de 06 §4.1 «Anti-reloj» y 08 §10.1 (sección 7.2).
- **R-Q13** añade a 05 §6.1 que el Reto no tiene intersticial.
- **R-Q15** concreta `result = fail` de 08 §8.2 (sección 12); **R-Q16** concreta la salida de R-Q4 (sección 9).

### 1.4 Huecos del encargo que esta nota cubre como [P]

- **`DailyChallengeService`:** el encargo §3.4 lo nombra, pero ninguna tarea lo crea. Va a `AF.LiveOps` como fachada, en R7b.
- **Cosecha doble del Reto:** 06 §4.1 la ofrece y 04 §2.8 la define como duplicar las semillas de la victoria; en el Reto, 60 → 120 [P]. Pero `Transaction.DoubleHarvest(int level, RewardTier tier)` duplica la Cosecha de un nivel de campaña y `Transaction.Earn` rechaza `SeedSource.DoubleHarvest`. Hace falta un enganche en `Economy/Transaction.cs` y `Wallet.cs` (E9).
- **Modo Reto en `LevelFlow`:** la victoria llama a `_progress.SaveWin(LevelNumber, …)` y `Transaction.Win(LevelNumber, …)` (`LevelFlow.Victory.cs`), y los desbloqueos de comodines dependen del número de nivel (`BoosterConfig`: Cuia extra 8, Morpho 12, Pororoca 30; Marea ilimitada hasta `undo_unlimited_until_level` = 10). La nota preveía enganches en `GameFlow/` (E8), pero **R8 lo resolvió sin tocar archivos existentes**: `LevelFlow.CreateDaily(...)` monta un flujo propio (sección 9). **E8 ya no hace falta como estaba**; quedan E19 (guarda de la Crecida) y E20 (limpieza del catálogo).
- **Patrón de las sondas:** `DevProbe.Fauna.cs` está en la rama `tanda4/o7-dcu2`, que aún no está en la integración. Se usa `DevProbe.Map.cs` como patrón.

### 1.5 Coherencias comprobadas (no son discrepancias)

- 06 §4.1 compara fechas («> 1 día») y 08 §10.1 compara relojes (`|device − server| > sec_clock_max_drift_h`, 24 h); 04 §7.1 «Reloj» la llama «la misma regla que el Reto del río». El código sigue 08 §10.1, con la clave en horas de 08 §9.
- 08 §2.2 pone el «pool del Reto … indexado por fecha» en AF.Levels y en AF.LiveOps. Reparto: AF.Levels indexa por (banda, día); AF.LiveOps pasa de fecha a día, con `live_daily_offset` y `daily_overrides`.
- Ejemplo del encargo: 2026-10-19 − 2026-01-01 = 291 días; con offset 0, el día 291.

### 1.6 Decisiones posteriores (antes preguntas)

Salieron de esta nota (R-Q1b, R-Q11-R-Q14) y de la revisión de R7b (R-Q12b, R-Q15, R-Q16, la aclaración de R-Q12). Todas decididas con la opción (a), la recomendada (sección 15). Ya no queda ninguna pregunta abierta para el dueño.

| Id | Hueco | Decisión | Estado en el código |
|---|---|---|---|
| R-Q1b | Paleta de B2 con K = 9 y de B3 (1.1) | (a) B2 solo con K = 8; B3 con `max_pares_riesgo` libre; B1 sin cambios | Hecho en R5: B2 `k [8, 8]`; B3 `max_risk_pairs: "libre"`; la muestra y el pool, regenerados |
| R-Q11 | Ningún candidato de 30 entra en la ventana del bot de su banda (03 §16; 03 §4.4 midió fallo 0,88-1,00 con 1 vacío) | (a) La ventana **es compuerta**: el día no se publica hasta ajustar la especificación (fuente o K, que son [P]) | Hecho: `QA-DAILY-BOT` es fallo (R4) y, en el generador, un día sin candidato en la ventana es un **error** y no se escribe el pack (R5) |
| R-Q12 | Qué hace la reconciliación con las victorias cobradas **sin red** (08 §10.1) | (a) Nada: no se retira nada de lo cobrado sin red (la economía es autoritativa en el cliente en la 1.0, 08 §10.1 «Edición del perfil local») | Hecho: `DailyReconcileMode.KeepOfflineClaims`, el default (R7b). La rama (b) sigue detrás de la opción, con sus tests |
| R-Q12 (aclaración) | En la rama (b), ¿se retiran las semillas de una fecha anulada? | (a) Al anular una fecha no se retiran semillas ni la Pororoca | Hecho: el código no retira ninguna de las dos |
| R-Q12b | Qué fechas pendientes se validan (06 §4.1 compara «> 1 día») | (a) Solo si `\|fecha − hoy_servidor\| ≤ 1 día`; las demás se anulan | Hecho: `Reconcile` valida a ≤ 1 día (`PendingWindowDays`) y anula el resto; la racha se corta en la última fecha anulada (sección 7.2) |
| R-Q13 | ¿Intersticial al terminar un Reto? (05 §6.1 solo habla de la campaña) | (a) No hay | Hecho: el flujo del Reto no tiene `AdGate` (R8) |
| R-Q14 | La Pororoca de la racha puede llegar en B1 antes del nivel 30, que la desbloquea (04 §2.4) | (a) Entra en el stock y espera al nivel 30 | Hecho en el pago (`Transaction.GrantStock`, R7b); falta el aviso en la UI (R9) |
| R-Q15 | ¿Cuántos `af_daily_challenge.result = fail`? (R-Q9) | (a) **Uno por día**, y solo si ese día termina sin victoria | Hecho: `DailyChallengeService.DiscardStale` emite el único `fail` (evento `DayEnded`) al descartar la partida de un día anterior sin victoria y posterior a `last_date`; `abandon` ya no se emite (sección 12) |
| R-Q16 | Salir de la partida del Reto sin ganar | (a) La deja **reanudable el mismo día**; al cambiar de día se descarta (R-Q4) | Hecho: `ExitDaily` guarda en síncrono (también el modal de Aguas estancadas) y `RecordExitWithoutWin` conserva `InProgress` |

---

## 2. Qué es y qué reglas tiene

| Regla | Valor | Fuente |
|---|---|---|
| Disponibilidad | Un tablero por día local, desde el nivel 10, jugable sin red (pool embebido) | 06 §4.1 «Disponibilidad»; 08 §2.1, principio 4; 08 §9 `live_unlock_level_daily` = 10 |
| Desbloqueo visible | El inicio lo muestra desbloqueado tras ganar el 10 | 06 §2.3, pantallas 3 y 4 |
| Presentación | Tarjeta de 2 s tras ganar el 7: «Desde el nivel 10, un tablero nuevo cada día» | 06 §2.2, nivel 7; 02 §13 |
| Oculto si no existe | Mientras el sistema no exista en el build, su entrada queda oculta (D-008) | 06 §4.7 |
| Índice | `reto(fecha, banda) = pool[banda][(días_desde(2026-01-01) + live_daily_offset) mod 729]` (R-Q2b; el GDD dice 730) | 03 §16; 06 §4.1, pseudocódigo; R-Q2b |
| Pool | 2.187 tableros = 3 × 729 (R-Q2b; el GDD dice 2.190 = 3 × 730), pack `daily_pool_v1`, `Data/Daily/daily_pool_v1.json`, grupo `levels_install` | 03 §16; 08 §3; 08 §6.2 |
| Bandas | B1: nivel máximo < 41, K 6-7, 2 vacíos, bot 40-60 %. B2: 41-200, K 8 (R-Q1b; 03 §16 decía 8-9), 1 vacío, 60-80 %. B3: 201+, K 10-11, 1 vacío, ≥ 75 %. Umbrales `lvl_daily_bracket_levels` = [41, 201] | 03 §16 [P]; 06 §4.1 «Bandas»; R-Q1b |
| Contenido | Sin mecánicas con disparador ni Aguas turbias; premio igual en las tres bandas | 03 §16; 06 §4.1 |
| Reintentos | Ilimitados y deterministas (misma semilla) | 06 §4.1; 03 §13 |
| Comodines | Marea, Cuia extra y Morpho (GDD) + Pororoca y Luciérnaga (R-Q8); sin Cuia de rescate ni Salto del Boto | 06 §4.1; 04 §2.6 e; R-Q8 |
| Canoas | No las requiere y sigue disponible con 0 canoas | 04 §7.1 |
| Crecida | No la toca | 06 §4.3 «Qué cuenta»; 06 §4.1 [P] |
| DDA | No se aplica | 03 §14 «Qué NO se toca» |
| Alivio | No hay: `relief` = `[]` | 03 §16; 08 §4.1, fila `relief` |
| Premio | 60 semillas (`live_daily_challenge_reward.seeds`) + sello del día; admite Cosecha doble | 06 §4.1 [P]; 05 §2.1, fila «Reto del río»; 08 §9 |
| Racha | 7 días seguidos ganados → 1 Pororoca (`live_daily_streak7_reward` = `"shuffle"`) a `shuffle_stock` | 06 §4.1 [P]; 04 §2.4; R-Q7 |
| Anti-reloj | Tablero siempre jugable. Premio solo si `fecha > ultima_fecha_reclamada`. Con red y desfase > `sec_clock_max_drift_h`, «pendiente de validar». Sin red se acepta la fecha local y se reconcilia al reconectar | 06 §4.1 «Anti-reloj»; 08 §10.1; 04 §7.1 «Reloj» |
| Pantalla | Tarjeta «Reto del río · {fecha corta}», icono de banda (1-3 olas), Jugar / Sellado, racha «{n}/7». Tablero con el HUD normal, cabecera «Reto del río», sin barra de Crecida. Al ganar, el sello cae sobre el calendario del mes (28-31 casillas), +60 y Cosecha doble | 06 §4.1 «Pantalla» |
| Aguas turbias · Bonus | 1.1 (`ui_murky_bonus_enabled` = false): fuera del alcance | 06 §4.1; 08 §9 |
| Economía | Ya simulada (05 §3.1-§3.2; `tools/out/econ_B.md`). No se toca `econ_sim.py` | 05 §3.2 |
| Servidor | Nada: las cinco Cloud Functions de la 1.0 no incluyen el Reto | 08 §2.4; 08 §2.1, principio 4 |

---

## 3. Módulos y archivos (todos nuevos)

| Módulo | Archivo | Qué contiene | Tarea | Fuente |
|---|---|---|---|---|
| `AF.Levels` | `Scripts/Levels/DailyPool.cs` | Tableros por banda (1-3) y día (0-729): `Find(band, day)` y `FindById(id)`, sin asignaciones en la consulta (arrays por banda y tabla por id construida al leer) | R6 | 08 §2.2, fila AF.Levels |
| `AF.Levels` | `Scripts/Levels/DailyPoolReader.cs` | Lee `daily_pool_v1.json` con `Levels/Json/JsonParser`. Rechaza con `LevelFormatException`: `kind` ≠ `daily`; `slot` incoherente con `id`/`seed`; bandas incompletas o días repetidos; `pack_schema` > `LevelPackReader.SupportedPackSchema`. Construye `LevelDefinition` con su constructor `internal` (mismo ensamblado); no reutiliza `LevelPackReader.ReadLevel`, que exige `level` y `difficulty` | R6 | 08 §4.1, filas `kind`, `seed`, `level`, `difficulty`; esquema, `allOf` de `daily` |
| `AF.LiveOps` | `Scripts/LiveOps/AF.LiveOps.asmdef` | `noEngineReferences: true`, `autoReferenced: false` | R7a | 08 §2.2; 08 §3 |
| `AF.LiveOps` | `DailyChallengeConfig.cs` | Las 9 claves de la sección 11, con `[RemoteConfigKey]` y defaults compilados, al estilo de `EconomyConfig` | R7a | 08 §9 |
| `AF.LiveOps` | `DailyCalendar.cs` | Fecha → día del pool, nivel máximo → banda, desbloqueo, `daily_overrides` (sección 6) | R7a | 03 §16; 06 §4.1 |
| `AF.LiveOps` | `DailyChallengeRules.cs` | `Claim`, `Reconcile`, racha y sellos (sección 7) | R7b | 06 §4.1; 08 §10.1 |
| `AF.LiveOps` | `IDailyChallengeStore.cs`, `InMemoryDailyChallengeStore.cs`, `DailyChallengeRecord.cs` | Almacén del bloque `daily_challenge` (sección 5) | R7b | 08 §4.3; R-Q5 |
| `AF.Platform` | `Scripts/Platform/IServerDate.cs`, `FakeServerDate.cs`, `ClockSecurityConfig.cs` | Hora del servidor detrás de una interfaz (1.2) y `sec_clock_max_drift_h` (fuente única) | R7b | 08 §2.1, principio 6; 08 §2.2; 08 §10.1 |
| `AF.LiveOps` [P] | `DailyChallengeService.cs` | Fachada: estado de la tarjeta, tablero de hoy, empezar, reanudar o descartar, reclamar. La usa AF.UI | R7b | 08 §2.2, «Entrega a: UI» |
| `AF.LiveOps` | `DailyProbeFormat.cs` | Formato sin motor de las líneas `AF_STATE` del Reto (sección 12), para probarlo en EditMode (sección 4) | R10 | 08 §8.2 |
| `AF.LiveOps` | `Tests/AF.LiveOps.Tests.asmdef` + tests | Patrón de `Scripts/Save/Tests/AF.Save.Tests.asmdef` | R7a/b | encargo §4 |
| `AF.GameFlow` | `Scripts/GameFlow/LevelFlow.Daily.cs` | Parcial de `LevelFlow`: cargar un `LevelDefinition` del pool en modo Reto y devolver el resultado al llamador, sin canoas, Crecida, alivio, Salto, Cuia de rescate ni progreso de campaña | R8 | 06 §4.1; 04 §7.1; 04 §2.6 e |
| `AF.UI` | `DailyChallengeCard.cs` | Tarjeta del inicio (sección 8) | R9 | 06 §4.1; 06 §4.7 |
| `AF.UI` | `DailyChallengeScreen.cs` | Cabecera «Reto del río» sobre el tablero, sin barra de Crecida | R9 | 06 §4.1 |
| `AF.UI` | `DailyStampCalendar.cs` | Cuadrícula del mes, sello que cae, +60 y Cosecha doble (anuncio simulado existente) | R9 | 06 §4.1 |
| `AF.UI` | `GameBootstrap.Daily.cs` | Parcial con `SetUpDaily()`: crea el pool, el servicio y las pantallas, y conecta la victoria | R9 | encargo §3.4 |
| `AF.UI` [P] | `DailyLevelStateStore.cs` | Implementa `AF.GameFlow.ILevelStateStore` sobre `IDailyChallengeStore` (`InProgress`, R-Q4). Vive en AF.UI porque es el único ensamblado que ve GameFlow y LiveOps | R8/R9 | 08 §2.1, principio 2 |
| `AF.UI` | `DevProbe.Daily.cs` | Sondas `AF_STATE` (sección 12) | R10 | 08 §8.2 |
| Herramientas | `tools/level-generator/Daily/DailySpec.cs`, `DailyIds.cs`, `DailyCommand.cs`, `DailyRunner.cs`, `DailyPackWriter.cs`, `DailyReport.cs`; `specs/daily_v1.json` | Generación del pool (sección 13); envuelve `Generation/*` sin cambiarlo | R2-R3 | 03 §16 |
| Herramientas | `tools/level-qa/Daily/DailyQaCommand.cs`, `DailyChecks.cs` (`QA-DAILY-*`); `fixtures/daily/` | QA del pool | R4 | 03 §17 |
| Datos | `Assets/_Project/Data/Daily/daily_pool_v1.json` (+ `.meta` de Unity) | El pool | R5 | 08 §3; 08 §6.2 |

---

## 4. Grafo de dependencias de asmdef

**Hoy** (asmdef de runtime de `Assets/_Project/Scripts/**`; se omiten los de Editor y de tests):

| asmdef | `noEngineReferences` | `references` |
|---|---|---|
| AF.Platform | true | [] |
| AF.Localization | true | [] |
| AF.Audio | true | [] |
| AF.Save | true | [AF.Platform] |
| AF.Levels | true | [AF.Core] |
| AF.Economy | true | [AF.Platform, AF.Save] |
| AF.Ads | true | [AF.Platform, AF.Save] |
| AF.IAP | true | [AF.Platform, AF.Save, AF.Economy, AF.Localization] |
| AF.Boosters | true | [AF.Core, AF.Solver, AF.Platform, AF.Save, AF.Economy] |
| AF.GameFlow | true | [AF.Core, AF.Levels, AF.Platform, AF.Save, AF.Economy, AF.Boosters] |
| AF.Presentation | false | [AF.Core, AF.Levels, AF.Audio, UnityEngine.UI] |
| AF.UI | false | [AF.Core, AF.Solver, AF.Levels, AF.GameFlow, AF.Presentation, AF.Platform, AF.Save, AF.Economy, AF.Boosters, AF.Ads, AF.IAP, AF.Localization, AF.Audio, AF.Audio.Unity, UnityEngine.UI, Unity.InputSystem, Unity.TextMeshPro] |

No existen `AF.RemoteConfig`, `AF.Analytics` ni `AF.Events`; `RemoteConfigKeyAttribute` vive en AF.Platform.

**Nuevo:**

```
AF.LiveOps  (noEngineReferences: true, autoReferenced: false)
  references: [AF.Core, AF.Levels, AF.Platform, AF.Save, AF.Economy]

AF.LiveOps.Tests (patrón de AF.Save.Tests: Editor, nunit.framework.dll, UNITY_INCLUDE_TESTS)
  references: [AF.Core, AF.Levels, AF.Platform, AF.Save, AF.Economy, AF.LiveOps,
               UnityEngine.TestRunner, UnityEditor.TestRunner]

AF.UI               += "AF.LiveOps"   (encargo §3.3, punto 3)
AF.Tests.PlayMode   += "AF.LiveOps"   (encargo §3.3, punto 4, si R9-R11 lo necesitan)
```

- 08 §2.2 pide para AF.LiveOps «Economy, Levels, RemoteConfig, Platform (`IClock`), Save». RemoteConfig no existe todavía (el atributo está en Platform). AF.Core se añade porque `LevelDefinition` expone tipos del núcleo, que «entrega a todos» (08 §2.2).
- **Sin ciclos:** ninguno de los referenciados referencia a LiveOps; solo lo referencian AF.UI y los tests.
- GameFlow y LiveOps no se ven entre sí. Como nadie referencia a la UI (08 §2.1, principio 2) y los dos «entregan a» UI (08 §2.2), es AF.UI quien los une.
- **Dónde van los tests.** `AF.Tests.EditMode.asmdef` no referencia AF.LiveOps, y el encargo no permite editarlo (§3.2 y §3.3). Por eso:
  - la lógica de LiveOps y el **formato de las sondas** (una clase sin motor `DailyProbeFormat` en AF.LiveOps, que `DevProbe.Daily.cs` solo llama) se prueban en `AF.LiveOps.Tests` (EditMode);
  - lo que vive en AF.UI (`DailyLevelStateStore`, pantallas, `DevProbe.Daily.cs`) se prueba en PlayMode, cuyo asmdef sí puede ganar `AF.LiveOps` (§3.3, punto 4);
  - enganche opcional E13 para el integrador: `AF.Tests.EditMode.asmdef` += "AF.LiveOps", si prefiere esos tests en EditMode.
- La Pororoca de la racha se paga con `Transaction.GrantStock(BoosterKind.Shuffle, 1)` (`BoosterKind` está en AF.Economy): LiveOps no necesita AF.Boosters.

---

## 5. Interfaces propuestas (C# 9; `#nullable enable` como el resto del cliente)

Las fechas se guardan como clave `yyyy-MM-dd` de `ClockExtensions.LocalDateKey()` / `FormatDateKey()` (`Platform/IClock.cs`), que se ordena igual como texto que como fecha (como en `Save/DailyCounters.IsStale`).

```csharp
// AF.Platform (1.2; 08 §2.2, §10.1)
namespace AF.Platform
{
    /// <summary>Clock.ServerOffset de 08 §10.1: servidor − dispositivo, con la cabecera Date del último fetch
    /// de Remote Config. False = sin red o sin fetch (se acepta la fecha local y se reconcilia luego).</summary>
    public interface IServerDate
    {
        bool TryGetServerOffset(out TimeSpan serverMinusDevice);
    }

    /// <summary>Para el editor y los tests: sin red, o con un desfase fijado por el test (08 §2.1, principio 6).</summary>
    public sealed class FakeServerDate : IServerDate
    {
        public static FakeServerDate Offline();
        public static FakeServerDate Online(TimeSpan serverMinusDevice);
        public void GoOffline();
        public void GoOnline(TimeSpan serverMinusDevice);
        public bool TryGetServerOffset(out TimeSpan serverMinusDevice);
    }
}

// AF.LiveOps
namespace AF.LiveOps
{
    /// <summary>Valores de af_daily_challenge.result (08 §8.2).</summary>
    public enum DailyResult { None = 0, Win = 1, Fail = 2, Abandon = 3 }

    /// <summary>Partida en curso (R-Q3: manda la fecha de inicio; R-Q4 y R-Q16: se reanuda el mismo día, también tras salir). [P] la forma</summary>
    public sealed class DailyPlayInProgress
    {
        public string DateKey { get; set; } = "";   // fecha local de inicio
        public int Band { get; set; }               // 1-3, congelada al empezar
        public string BoardId { get; set; } = "";   // d{b}_n{nnnn} (03 §16)
        public LevelStateData State { get; set; } = new LevelStateData();   // formato de level_state (04 §10.1)
    }

    /// <summary>Bloque daily_challenge del perfil (08 §4.3 + R-Q5).</summary>
    public sealed class DailyChallengeRecord
    {
        public string? LastDate { get; set; }                 // last_date (08 §4.3)
        public DailyResult Result { get; set; }               // result (08 §4.3)
        public int StreakDays { get; set; }                   // streak_days (08 §4.3)
        public string? LastClaimedDate { get; set; }          // R-Q5: ultima_fecha_reclamada (06 §4.1)
        public List<DailyClaimEntry> PendingClaims { get; } = new List<DailyClaimEntry>();   // R-Q5 [P]: {fecha, racha} pendientes de validar
        public List<DailyClaimEntry> OfflineClaims { get; } = new List<DailyClaimEntry>();   // [P] cobros sin red; solo para la rama (b) de R-Q12 (no es la decidida)
        public string? StampsMonth { get; set; }              // R-Q5: yyyy-MM
        public List<int> StampDays { get; } = new List<int>();            // R-Q5: días sellados (1-31)
        public DailyPlayInProgress? InProgress { get; set; }  // R-Q4 [P]
    }

    /// <summary>En el juego, sobre el perfil (enganche E4); en los tests, InMemoryDailyChallengeStore. Mismo patrón que AF.GameFlow.ILevelStateStore / ProfileLevelStateStore: Save es void y,
    /// si la escritura falla, el cambio sigue en memoria (PendingSave) y sale con el siguiente guardado o con Flush.</summary>
    public interface IDailyChallengeStore
    {
        DailyChallengeRecord Load();                 // nunca null: la instancia viva
        void Save(DailyChallengeRecord record);
        bool PendingSave { get; }
        bool Flush();                                // true si ya no queda nada pendiente
    }
}
```

**Con lo que ya existe:**
- **Reloj:** solo `IClock` (`UtcNow`, `LocalOffset`) y `LocalDate()` / `LocalDateKey()`; nada de `DateTime.Now` (08 §2.1, principio 5). Tests con `ManualClock`.
- **Cartera:** todo pago por `Wallet.Apply(Transaction)` (08 §2.2, fila Economy):
  - premio: `Transaction.Earn(SeedSource.DailyChallenge, seeds)` (`daily_challenge`; 08 §8.2);
  - Pororoca: `Transaction.GrantStock(BoosterKind.Shuffle, 1)` → `profile.stock.shuffle` (el `shuffle_stock` de 04 §2.4 y §10.1);
  - gasto en el Reto: `Transaction.UseStock(kind)`, como en la campaña (R-Q8).
- **Orden de escritura [P]:** primero se registra el cobro en el `DailyChallengeRecord` y luego `Wallet.Apply`. Sobre el perfil, el registro y la cartera son el mismo `Profile`: si la escritura falla, los dos cambios quedan en memoria (`PendingSave`) y salen juntos en el siguiente guardado, así que nunca se paga dos veces la misma fecha.

**Firmas de la lógica (R7a/R7b) [P]:**

```csharp
public static class DailyCalendar
{
    public static readonly DateTime Epoch = new DateTime(2026, 1, 1);   // 03 §16
    public const int PoolDays = DailyPool.Days;                          // 729 (R-Q2b; 03 §16 dice 730)
    public static int DaysSinceEpoch(DateTime localDate);                // puede ser negativo
    public static int PoolDay(DateTime localDate, int offset);           // módulo positivo, 0-729
    public static int BandFor(int maxLevel, IReadOnlyList<int> thresholds);   // [41, 201] → 1/2/3
    public static bool IsUnlocked(int maxLevel, DailyChallengeConfig config);
}

public enum ClaimOutcome { NoReward = 0, Confirmed = 1, Pending = 2 }

public sealed class DailyChallengeRules
{
    public DailyChallengeRules(IDailyChallengeStore store, Wallet wallet, IClock clock, IServerDate server, DailyChallengeConfig config);
    public ClaimOutcome Claim(string startDateKey);   // reclamar_reto (sección 7)
    public int Reconcile();                           // pendientes confirmadas o anuladas
    public int StreakShown();                         // «{n}/7»
}
```

---

## 6. Banda, día y sustituciones

**Banda** (06 §4.1; 03 §16; `lvl_daily_bracket_levels` = [41, 201]): banda = 1 si `nivel_max` < 41; 2 si < 201; 3 en otro caso.
- `nivel_max` = `progress.max_level` del perfil (08 §4.3), que `Profile.cs` (`Progress.MaxLevel`) define como el nivel más alto superado [P].
- Tests: 40 → B1, 41 → B2, 200 → B2, 201 → B3.
- La banda se congela al empezar la partida. El premio es por fecha, no por banda (06 §4.1).

**Desbloqueo** (08 §9; 06 §2.3): visible = `biz_feature_daily_challenge` && `max_level` ≥ `live_unlock_level_daily` (10) && pool cargado y válido.

**Día del pool** (03 §16; 06 §4.1): `d = (fecha_local − 2026-01-01).Days`; `idx = (d + live_daily_offset) mod 729` (R-Q2b), con módulo positivo. `fecha_local` = `clock.LocalDate()`.

| Fecha | Offset | Día |
|---|---|---|
| 2026-01-01 | 0 | 0 |
| 2026-10-19 | 0 | 291 |
| 2027-12-30 | 0 | 728 |
| 2027-12-31 | 0 | 0 (vuelta: 729 días) |
| 2028-01-01 | 0 | 1 |
| 2025-12-31 | 0 | 728 |
| 2026-01-01 | −1 | 728 |

**Sustituciones** (R-Q6; 03 §16; 06 §4.1): `daily_overrides` = `{"2026-12-25": {"1": "d1_n0100", "2": "d2_n0033"}}`.
- Si existe `[fecha][banda]` y el id está en el pool (`DailyPool.FindById`), se sirve ese tablero; si no, el cálculo normal (y la sonda lo anota).
- Una banda que falta usa el cálculo normal [P]; se admite un id de otra banda [P].

**Tablero de hoy:** `DailyPool.Find(banda, idx)` o el sustituido; con `InProgress` del mismo día, `InProgress.BoardId` (R-Q4).

---

## 7. `reclamar_reto` adaptado a R-Q3, R-Q5 y R-Q7

Original en 06 §4.1. Cambios: la fecha que se reclama es la de **inicio** (R-Q3); el desfase en horas con `sec_clock_max_drift_h` (08 §9, §10.1); la Pororoca en cada racha completa (R-Q7); sin marco de los 20 sellos (R-Q7).

### 7.1 Reclamar

```
function reclamar_reto(r = store.Load(), fecha = InProgress.DateKey):       # R-Q3
    if r.LastClaimedDate != null and fecha <= r.LastClaimedDate:
        return SIN_PREMIO                                 # reloj atrasado o ya cobrado (06 §4.1)
    online = server.TryGetServerOffset(out off)
    estado = PENDIENTE si online y |off| > sec_clock_max_drift_h horas, si no CONFIRMADO   # 06 §4.1; 08 §10.1
    r.StreakDays = (r.LastClaimedDate == fecha − 1 día) ? r.StreakDays + 1 : 1             # R-Q7
    r.LastClaimedDate = fecha                             # también si queda pendiente (06 §4.1)
    r.LastDate = fecha; r.Result = Win
    sellar(r, fecha)                                      # si cambia el mes: StampsMonth = mes, StampDays = []
    if estado == PENDIENTE: r.PendingClaims.Add({fecha, r.StreakDays})
    r.InProgress = null
    store.Save(r)
    if estado == CONFIRMADO:
        wallet.Apply(Transaction.Earn(SeedSource.DailyChallenge, reward.seeds))          # 60
        if r.StreakDays % 7 == 0:                                                        # R-Q7
            wallet.Apply(Transaction.GrantStock(BoosterKind.Shuffle, 1))                 # 04 §2.4
    return estado
```

`fecha − 1 día` se calcula sobre `DateTime` (ParseExact, InvariantCulture), nunca sobre el texto. Con `SIN_PREMIO` la victoria se muestra sin +60 ni sello nuevo y la racha no cambia.

### 7.2 Reconciliar (08 §10.1; R-Q12, su aclaración y R-Q12b)

```
function reconciliar(r = store.Load()):          # al obtener hora de servidor
    if not server.TryGetServerOffset(out off) or |off| > sec_clock_max_drift_h: return 0
    hoy_srv = fecha local de (clock.UtcNow + off + clock.LocalOffset)
    for {fecha, racha} in r.PendingClaims:
        if |fecha − hoy_srv| <= 1 día: pagar 60 (+ Pororoca si esa fecha cerró una racha de 7)   # R-Q12b (a)
        else: anular (sin semillas ni Pororoca; se quita el sello y la racha se corta en esa fecha; LastClaimedDate no retrocede)
    r.PendingClaims.Clear(); r.OfflineClaims.Clear(); store.Save(r)
```

- **R-Q12b (a):** un pendiente solo se valida si `|fecha − hoy_servidor| ≤ 1 día` (la comparación de 06 §4.1, «> 1 día»); los demás se anulan. **Pendiente en el código:** `DailyChallengeRules.Reconcile` paga hoy todo pendiente con fecha ≤ hoy del servidor (es una línea).
- **R-Q12 (a):** las victorias **sin red** se pagan al momento (08 §10.1: «sin red se acepta la fecha local») y la reconciliación **no retira nada** de lo cobrado sin red. Es `DailyReconcileMode.KeepOfflineClaims`, el default (R7b); la rama (b), que anula las fechas cobradas sin red posteriores a la del servidor, se queda detrás de la opción con sus tests.
- **Aclaración de R-Q12 (a):** al anular una fecha (un pendiente o, en la rama (b), un cobro sin red) no se retiran semillas ni la Pororoca.
- [P] Al anular, la racha vuelve a 0 (R7b).
- En la 1.0 `IServerDate` es siempre `FakeServerDate.Offline()`: todo sale CONFIRMADO y la reconciliación no se ejecuta.

### 7.3 Racha mostrada «{n}/7» [P]

- Racha viva = `LastClaimedDate` ∈ {hoy, ayer}; si no, n = 0.
- Hoy sellado: n = ((StreakDays − 1) mod 7) + 1 (el día de la Pororoca se ve 7/7).
- Hoy sin sellar y racha viva: n = StreakDays mod 7.

---

## 8. Estados de la tarjeta del inicio (06 §4.1 «Pantalla»; 06 §4.7)

En este orden:

| # | Estado | Condición | Qué muestra o hace | Fuente |
|---|---|---|---|---|
| 1 | **Oculta** | `biz_feature_daily_challenge` = false, `max_level` < `live_unlock_level_daily`, o pool ausente o inválido | Nada (D-008) | 06 §4.7; 08 §9 |
| 2 | **En curso** | `InProgress.DateKey` == hoy (también tras salir sin ganar) | Reanuda la partida [P: «Seguir»] | R-Q4; R-Q16 |
| 3 | **Pendiente de validar** | Hoy está en `PendingClaims` | Sello pendiente, sin +60 todavía [P] | 06 §4.1; 08 §10.1 |
| 4 | **Sellado** | `LastClaimedDate` == hoy y no pendiente | Sello; tocar abre el calendario del mes [P] | 06 §4.1 |
| 5 | **Jugar** | En otro caso | Tablero de hoy; con el reloj atrasado se juega sin premio [P] | 06 §4.1 |

- Siempre: «Reto del río · {fecha corta}» (formato de la región vía `Loc`, 08 §2.2), olas de la banda (1-3) y «{n}/7».
- Al cambiar de día con la app abierta, la tarjeta se recalcula al volver al inicio; la partida empezada ayer sigue (R-Q3).
- Al reabrir otro día o al volver al inicio, `InProgress` de un día anterior se descarta (`DiscardStale`, R-Q4); si ese día no se ganó y es posterior a `last_date`, es su único `result = fail` (R-Q15). Salir sin ganar deja la partida reanudable el mismo día (R-Q16). La UI se suscribe a `DayEnded` antes del primer `DiscardStale` del arranque.
- Con el reloj atrasado, una partida de fecha futura se descarta sin informar [P]: si ese día no se vuelve a jugar, no tendrá `fail`.
- Con la Pororoca en el stock antes del nivel 30 (R-Q14), la UI avisa de que espera a su desbloqueo [P: el texto].
- Textos: solo claves, en `docs/dev/traspaso/spark-strings-reto.csv`, `status = borrador`.

---

## 9. La partida del Reto (`LevelFlow.Daily.cs`, R8)

| Aspecto | Regla en el Reto | Fuente |
|---|---|---|
| Canoas | Ni se piden ni se gastan; se empieza con 0 | 04 §7.1 |
| Aguas estancadas | Modal sin Cuia de rescate ni Salto del Boto: Marea y Volver a la orilla [P] | 06 §4.1; 04 §2.6 e |
| Fallo | Sin efecto: sin canoas, Crecida ni alivio; `fails_level` se ignora. Cuando exista la Crecida, guarda E19 en `FailResolve` | 04 §5.2; 06 §4.3; 03 §16 |
| Reintentos | Ilimitados, mismo tablero | 06 §4.1; 03 §13 |
| Progreso de campaña | No cambia `max_level`, la Canasta, el Mapa ni las Frutas del río [P] | 06 §4.3 «Solo niveles de campaña» |
| Salir | Sin canoa; la partida queda **reanudable el mismo día** (R-Q16; `ExitDaily` la guarda en síncrono); `af_daily_challenge.result = fail` **una vez por día**, solo si ese día termina sin victoria (R-Q15) | 04 §7.1; R-Q9; R-Q15; R-Q16 |
| Marea | 3 gratis por tablero (`undo_free_per_level`) + stock; no ilimitada [P] | 04 §2.1; 08 §9 |
| Cuia extra, Morpho, Pororoca, Luciérnaga | Como en la campaña, con stock (R-Q8); desbloqueo con el nivel efectivo `max_level + 1` [P]; topes de la campaña (la Pororoca, 3 al día compartidas con la campaña). La Luciérnaga no aparece (no hay ocultos) | 04 §2.2-§2.5; R-Q8 |
| Cosecha doble | Desde `ads_rv_double_harvest_from_level` (nivel efectivo) [P]: 60 → 120; solo con premio CONFIRMADO [P]; necesita E9 | 06 §4.1; 04 §2.8 |
| Intersticial | No hay | 05 §6.1; R-Q13 |
| Resultado | `LevelFlow` devuelve victoria o salida a AF.UI, que llama a `DailyChallengeRules.Claim` | 08 §2.1, principio 2 |

**Cómo lo hace R8** (sin tocar archivos existentes; E8 ya no hace falta como estaba): `LevelFlow.CreateDaily(...)` en `LevelFlow.Daily.cs` monta un flujo propio.
- Catálogo de un solo tablero, numerado con el nivel efectivo `max_level + 1` [P], congelado al empezar: los desbloqueos de `BoosterConfig` funcionan sin cambios. E20 quita el paso por un JSON interno que no cumple af-level/1 (`LevelDefinition.WithLevel` y `LevelCatalog.ForBoard`).
- Progreso de solo lectura: ni `SaveWin` ni `SaveNextLevel`, y sin regalos de tutorial [P].
- Cartera del Reto obligatoria, sobre el mismo `Profile`, con una copia de `EconomyConfig` (`eco_reward_*` = 0; `rescue_min_level` = ∞): sin canoas, rescate, fallo duro con coste ni Cosecha.
- Copia de `BoosterConfig` con la Marea no ilimitada y sin Cuia de rescate.
- Sin `AdGate` (R-Q13) y con `StateStore` propio (rechaza el de campaña).
- El evento `DailyResult` (Win/Exit/Restart) llega a la UI sin depender de AF.LiveOps.
- [P] Modal de Aguas estancadas con Marea + Volver a la orilla; `DailyAttempt` vuelve a 1 al reanudar.

---

## 10. Bloque del perfil (R-Q5 y R-Q4; enganche E4)

```json
"daily_challenge": {
  "last_date": "2026-10-19",
  "result": "win",
  "streak_days": 6,
  "last_claimed_date": "2026-10-19",
  "pending": [],
  "offline_claims": [],
  "stamps": { "month": "2026-10", "days": [1, 2, 5, 19] },
  "in_progress": null
}
```

- `last_date`, `result` y `streak_days` son de 08 §4.3; `result` con los valores de 08 §8.2.
- `last_claimed_date`, `pending` y `stamps`: R-Q5. `pending` y `offline_claims` [P] son listas de `{date, streak}` (R7b): sin la racha, la reconciliación no sabría si una fecha cerraba una racha de 7. `offline_claims` solo sirve a la rama (b) de R-Q12, que no es la decidida, y hay que acotarla en E4 (en la 1.0 nunca se reconcilia). `in_progress` [P] por R-Q4 y R-Q16: `{date, band, id, level_state}` (04 §10.1); sigue ahí tras salir sin ganar, hasta que cambia el día.
- Sin migración: mientras el esquema no se publique, «un campo nuevo con un default razonable se añade sin migración» (08 §4.3).

---

## 11. Remote Config (08 §9)

| Clave | Tipo | Default | Uso |
|---|---|---|---|
| `biz_feature_daily_challenge` | bool | true | Interruptor |
| `live_unlock_level_daily` | int | 10 | Desbloqueo |
| `lvl_daily_bracket_levels` | json | `[41, 201]` | Bandas |
| `live_daily_offset` | int | 0 | Índice del día |
| `daily_overrides` | json (R-Q6: `{fecha: {banda: id}}`) | `{}` | Sustituciones |
| `live_daily_challenge_reward` | json | `{"seeds":60,"murky_bonus_mult":2}` | Premio |
| `live_daily_streak7_reward` | string | `"shuffle"` | Racha de 7 |
| `ui_murky_bonus_enabled` | bool | false | Bonus de la 1.1 |
| `sec_clock_max_drift_h` | int | 24 | Anti-reloj. Clave global: vive en `AF.Platform/ClockSecurityConfig` (fuente única; `DailyChallengeConfig` no la declara y AF.Economy la podrá leer para Canoas) |

Los alias retirados (`daily_offset`, `daily_reward_seeds`, `daily_bands`…) no se usan. R7a lleva un test contra 08 §9, como `EconomyConfigTests`, que cubre también `ClockSecurityConfig`.

---

## 12. Analítica y sondas (08 §8.2; R10)

Sin `AF.Analytics`: líneas `AF_STATE` en `DevProbe.Daily.cs`, con el patrón de `DevProbe.Map.cs`. Solo identificadores.
- `af_daily_challenge {date, result, t_ms, murky_bonus = false, streak_days}`: `win` al reclamar; `fail` **una vez por día**, y solo si ese día termina sin victoria (R-Q9 y R-Q15): lo emite `DailyChallengeService.DayEnded` al descartar la partida de un día anterior no ganado y posterior a `last_date`, así que con el reloj atrasado no se repite ni sale para un día ganado; `abandon` ya no se emite (R-Q15 y R-Q16), aunque `DailyProbeFormat` sabe escribirlo. En el `fail`, `t_ms` = null (el registro no guarda el tiempo jugado) y `streak_days` = la racha que ese día rompe [P]. Formato: `DailyProbeFormat` (AF.LiveOps, sin motor).
- `level_start {level_name = d{b}_n{nnnn}, variant = base, band}` (08 §8.2).
- `earn_virtual_currency {virtual_currency_name = seeds, source = daily_challenge, value}`; la Cosecha doble, `source = double_harvest` (E9).
- Estado de la tarjeta como `AF_STATE`, para jugar con `adb`.

---

## 13. Generador y QA del pool (R2-R5)

| Banda | K | Vacíos | Ventana del bot | Fuente [P] (análoga a 03 §4.4) |
|---|---|---|---|---|
| B1 | 6-7 | 2 | 40-60 % | `fill` E=2 → `deep` → `walk` → `deep` de 60.000 nodos (R5) |
| B2 | 8 (R-Q1b) | 1 | 60-80 % | `deep` → `fill` E=1 (R5; con `fill` primero, 96 días quedaban por encima de la ventana) |
| B3 | 10-11 | 1 | ≥ 75 % | `deep` |

- Rechazo por ventana: como mucho **30 candidatos por fuente**, con respaldos en orden (R-Q11b, decisión del dueño: así se lee el «máximo 30 candidatos» de 03 §4.4). Sin las relajaciones de 03 §4.4 (Cuia chica o 2 vacíos), que cambiarían los vacíos de la banda.
- **R-Q11 (a): la ventana del bot es compuerta.** Un día sin candidato dentro de la ventana no se publica hasta ajustar la especificación (fuente o K, que son [P]):
  - en el generador es un **error**: el pack no se escribe (R5);
  - en `level-qa --daily`, `QA-DAILY-BOT` sigue siendo **fallo** (03 §17 #3), así que el pool publicado (R5) tiene 0 días fuera de ventana.
- K del tablero: día par → el K menor de la banda, día impar → el mayor [P] (en B2, con K = 8, ya no varía).
- Capacidades 4, sin Cuia chica ni alta [P]: con capacidad 4 y vacíos enteros, T = K + vacíos (06 §2.1, regla 4; 02 §13); 02 R1 fija T ≤ 14 y la capacidad 4 por defecto.
- Paleta: R-Q1 con R-Q1b (1.1): B1 1-40 con tope 0; B2 1-80 con tope 1 (K = 8); B3 1-200 sin tope de pares.
- Agua y fondo (R-Q2): rotan por los mundos 1-9 **según la fecha**. Cada tablero lleva `world` y `water_base` del glosario §8 (w1 blanca, w2 clara, w3 negra, w4-w9 blanca); el cliente toma el kit de fondo de ese mundo. Fórmula: `1 + (día del pool mod 9)`. Con el pool de **729 días** (R-Q2b, 81 × 9) sigue a la fecha en todas las vueltas; con `live_daily_offset` ≢ 0 (mod 9), desplazada por el offset de forma constante. La especificación exige que el módulo divida a los días.
- Cada tablero: `slot {band, day}`, `id` `d{b}_n{nnnn}`, `seed` `amazonfill:v1:daily:b{b}:d{ddd}`, `relief: []`, sin `difficulty`, `solution`, `safe_path` ni `qa`, sin mecánicas ni ocultos. JSON minificado con orden estable; cabecera `kind: daily`, `pack_id` `daily_pool_v1`.
- `QA-DAILY-*` (03 §17): esquema (#11); `slot` ↔ `id`/`seed`; 729 días por banda (R-Q2b); K, vacíos y ventana (#3); A* (#1); `par ≥ 2K` (#2); sin mecánicas ni ocultos (#6, #7); `relief` vacío; la semilla reproduce el tablero (#10); ≤ 14 cuias (#5). Anti-repetición (#9) y trampas tempranas (#3), solo aviso (R-Q10).
- Tamaño < 2 MB sin comprimir (compuerta del encargo, R5; 03 §16 estima ≈ 1,4 MB [E]).

---

## 14. Enganches (no los aplica el Spark salvo los commits «Enganche:»; parches exactos y estado en `spark-estado.md`, sección «Enganches para el integrador»)

| # | Archivo | Qué haría | Fuente |
|---|---|---|---|
| E1 | `Scripts/UI/LobbyScreen.cs` | Crear `DailyChallengeCard` en el inicio y refrescarla al mostrarlo | 06 §4.1; 06 §4.7 |
| E2 | `Scripts/UI/GameBootstrap.cs` | Campo `[SerializeField] TextAsset dailyPool;` y llamada a `SetUpDaily()` tras crear perfil, cartera y reloj | encargo §3.4 |
| E3 | `Scripts/UI/Editor/GameSceneBuilder.cs` | Asignar `Data/Daily/daily_pool_v1.json` a `dailyPool`; el integrador regenera `Game.unity` | 08 §3; regla dura 3 |
| E4 | `Scripts/Save/Profile.cs`, `ProfileJson.cs` | Bloque `daily_challenge` (sección 10) y `ProfileDailyChallengeStore : IDailyChallengeStore` | 08 §4.3; R-Q5 |
| E5 | `Data/Loc/strings.csv` | Copiar las filas de `spark-strings-reto.csv` | 08 §4.7 |
| E6 | Victoria del nivel 7 | Tarjeta de 2 s «Desde el nivel 10, un tablero nuevo cada día» | 06 §2.2; 02 §13 |
| E7 | `Scripts/UI/NoCanoesScreen.cs` (opcional) | «Mientras esperas: Jugar el Reto del río» y `af_no_canoes_choice = daily_challenge` | 04 §7.3; 04 §11 |
| E8 (ya no hace falta) | Iba en `Scripts/GameFlow/LevelFlow*.cs` | **Ya no hace falta como estaba:** R8 hizo el modo Reto sin enganches (`LevelFlow.CreateDaily`, sección 9). Lo que queda va en E19 y E20 | 06 §4.1; 04 §7.1; R-Q4; R-Q8 |
| E9 | `Scripts/Economy/Transaction.cs`, `Wallet.cs` | Cosecha doble del Reto (p. ej. `Transaction.DoubleHarvestDaily(int seeds)`) | 04 §2.8; 06 §4.1 |
| E10 | `Scripts/UI/DevProbe.cs` (si hace falta) | Llamar a `DevProbe.Daily.cs` desde los puntos de evento | 08 §8.2 |
| E11 | `docs/gdd/06` §4.1, `docs/gdd/08` §4.3 y §9, `docs/dev/decisiones.md` | Llevar R-Q1..R-Q10 y la sección 1.3 al GDD | `CLAUDE.md` |
| E12 | `docs/dev/prototipo-v0.md`, `docs/dev/traspaso/README.md`, `CLAUDE.md` | Sondas nuevas; `Scripts/LiveOps/` y `Data/Daily/` en el mapa; `--daily` y `--self-test daily` | encargo §3.4 |
| E13 (opcional) | `Assets/_Project/Tests/EditMode/AF.Tests.EditMode.asmdef` | += "AF.LiveOps", si se quieren en EditMode los tests de AF.UI que tocan el Reto (sección 4) | encargo §3.2-§3.3 |
| E14 (opcional) | `Scripts/Levels/LevelPackReader.cs`, `DailyPoolReader.cs` | Extraer a una clase `internal` compartida los ayudantes que `DailyPoolReader` copia | R6 |
| E15 (aplicado, `752bb55`) | `tools/level-generator/Program.cs`, `README.md` | `daily` en el `--self-test` sin nombre; `--daily` documentado | encargo §9, punto 6 |
| E16 | `tools/level-qa/level-qa.csproj` | Enlazar `tools/level-generator/Daily/` y regenerar en memoria en QA-DAILY-REPRO | R4 |
| E17 (aplicado en parte, `752bb55`) | `tools/level-qa/Program.cs`, `README.md` | `daily` en `Usage` y en el mensaje; falta el README (va con E16) | R4 |
| E18 | Encargo §3.3, puntos 1 y 2 | «Antes del `--self-test` genérico» (el integrador ya corrigió el punto 1 en §9) | R2; R4 |
| E19 | `Scripts/GameFlow/LevelFlow.Funnel.cs`, `FailResolve.cs` | Cuando exista la Crecida: el fallo del Reto no la rompe ni gasta la Hoja escudo | 06 §4.3 |
| E20 | `Scripts/Levels/LevelDefinition.cs`, `LevelCatalog.cs` | `WithLevel` y `LevelCatalog.ForBoard`, sin el JSON interno no conforme | R8 |

Las ediciones de `AF.UI.asmdef`, `AF.Tests.PlayMode.asmdef` y los despachos de los dos `Program.cs` son las de §3.3 del encargo, en commits «Enganche:»; E15 y E17 son enganches aplicados del mismo modo, a petición del integrador (encargo §9, punto 6).

---

## 15. Decisiones del dueño

R-Q1-R-Q10: encargo §5 (fechadas el 2026-10-20 en la primera versión del encargo; el reloj de este equipo decía 2026-10-19). R-Q1b y R-Q11-R-Q16: 2026-10-19, la opción recomendada (a) de cada una (encargo §5, respuestas del integrador; `spark-estado.md`). El integrador las lleva a `docs/dev/decisiones.md` (E11).

| Id | Hueco | Decidido | Dónde aplica |
|---|---|---|---|
| R-Q1 | Paleta y `max_pares_riesgo` (03 §4.5, §16) | Tintes estrenados en el tramo: B1 1-40 tope 0; B2 1-80 tope 1; B3 1-200 tope 2. Ajustado por R-Q1b | R2, R5 |
| R-Q2 | `water_base` y fondo (08 §4.1) | Rotan por los mundos 1-9 según la fecha (fórmula [P], sección 13) | R5, R9 |
| R-Q3 | Partida que cruza la medianoche (06 §4.1) | Manda la fecha de inicio | Secciones 7 y 8 |
| R-Q4 | Reanudar (04 §5.2; 08 §4.3) | El mismo día sí; otro día se descarta | Secciones 8 y 10 |
| R-Q5 | Campos del perfil (08 §4.3) | Añade última fecha cobrada, pendiente y sellos del mes | Sección 10, E4 |
| R-Q6 | `daily_overrides` (08 §9) | `{fecha: {banda: id del pool}}` | Sección 6 |
| R-Q7 | Racha y sellos (06 §4.1) | Pororoca cada 7 días seguidos ganados; un día sin ganar rompe; marco fuera | Sección 7 |
| R-Q8 | Comodines (06 §4.1) | Los de la campaña menos Cuia de rescate y Salto del Boto, con stock | Sección 9 |
| R-Q9 | `result = fail` (08 §8.2) | Salir sin ganar ese día; lo concretan R-Q15 (uno por día, solo si el día termina sin victoria) y R-Q16 (salir deja la partida reanudable) | Sección 12 |
| R-Q10 | Anti-repetición y trampas tempranas (03 §17) | Medir y avisar, sin compuerta | R4 |
| R-Q1b | R-Q1 no cabe en B2 con K = 9 ni en B3 (1.1) | (a) B2 solo con K = 8 (cambia 03 §16 y 06 §4.1, que dan K 8-9) y B3 con `max_pares_riesgo` libre, como la campaña desde el 201 (03 §4.5). B1 sin cambios | `daily_v1.json`, R5 |
| R-Q11 | Ningún candidato en la ventana del bot (03 §16) | (a) La ventana del bot es compuerta: un día fuera de ventana no se publica hasta ajustar la especificación (fuente o K, que son [P]); QA-DAILY-BOT sigue siendo fallo | Sección 13; R4, R5 |
| R-Q12 | Reconciliación de lo cobrado sin red (08 §10.1) | (a) Al reconectar no se retira nada de lo cobrado sin red (`KeepOfflineClaims`, el default) | Sección 7.2; R7b |
| R-Q12 (aclaración) | Semillas de una fecha anulada | (a) Al anular una fecha no se retiran semillas ni la Pororoca | Sección 7.2; R7b |
| R-Q12b | Qué pendientes se validan (06 §4.1) | (a) Un pendiente solo se valida si `\|fecha − hoy_servidor\| ≤ 1 día`; los demás se anulan | Sección 7.2; `DailyChallengeRules.Reconcile` |
| R-Q13 | Intersticial (05 §6.1) | (a) Sin intersticial al terminar un Reto | Sección 9; R8 |
| R-Q14 | Pororoca antes del nivel 30 (04 §2.4) | (a) La Pororoca de la racha entra en el stock y espera al nivel 30, en que se desbloquea | Sección 8; R7b, R9 |
| R-Q15 | Cuántos `result = fail` (08 §8.2) | (a) `af_daily_challenge.result = fail` una vez por día, y solo si ese día termina sin victoria | Sección 12; R10 |
| R-Q16 | Salir sin ganar | (a) La partida del Reto queda reanudable el mismo día; al cambiar de día se descarta (R-Q4) | Secciones 8-10; R8, R9 |
| R-Q2b | Agua «según la fecha» con 730 mod 9 = 1 | Pool de **729 días** por banda (81 × 9), 2.187 tableros | Secciones 2, 6 y 13; R5 |
| R-Q11b | ¿«Máximo 30 candidatos» por día o por fuente? (03 §4.4) | 30 **por fuente**, con respaldos en orden | Sección 13; R5 |

**Confirmado por el integrador:** `IServerDate` y `FakeServerDate` van en AF.Platform (sección 1.2).

---

## 16. Riesgos

| Riesgo | Mitigación |
|---|---|
| R-Q1 no cabía en B2 (K 9) ni en B3 (1.1) | Decidido R-Q1b (a); queda aplicarlo en `daily_v1.json` y rehacer la muestra antes de R5; el cliente prueba con packs construidos en código |
| Con 1 vacío, 03 §4.4 midió fallo de bot 0,88-1,00 con cualquier fuente: la ventana 60-80 % de B2 puede no alcanzarse (en el pool completo de R3, B2 tenía 55 días fuera y B1 13) | R-Q11 (a): es compuerta, así que esos días se arreglan en la especificación (fuente o K [P]) antes de publicar; R3 mide los rechazos por banda en `DailyReport` |
| El modo Reto en `LevelFlow` necesitaba archivos existentes (E8) | Resuelto en R8 sin enganches; quedan E19 (Crecida, cuando exista) y E20 (limpieza) |
| La Cosecha doble del Reto no cabe en `Transaction` (E9) | Enganche; hasta entonces, el botón del Reto queda desactivado y anotado |
| Fondos de los mundos 5-9 en la 1.0 (PAD o «río genérico», 08 §6.2) | El fondo cae al «río genérico» si el kit no está instalado |
| Pororoca de la racha antes de su desbloqueo del 30 (B1 = 10-40) | R-Q14 (a): entra en el stock y no se usa hasta el 30; la UI de R9 lo avisa |
| Peso del pool ≈ 1,4 MB [E] | Compuerta < 2 MB en R5 y `ci/size-gate.ps1` |
