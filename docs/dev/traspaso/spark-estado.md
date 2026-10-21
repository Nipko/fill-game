# Estado del equipo Spark: el Reto del río

Encargo: `docs/dev/traspaso/spark-reto-del-rio.md` (de `origin/integracion-fase1-fase2`). Nota de diseño: `docs/dev/reto-del-rio.md`.

Orden de este archivo (encargo §9, punto 5): arriba el resumen; después, una entrada por tarea o bloque, de la más reciente a la más antigua; al final, la única sección «Enganches para el integrador» (E1-E20).

---

## 2026-10-21 · B7 · `AF.RemoteConfig` + `AF.Analytics` con implementaciones falsas, y el bus `AF.Events` (`spark/t5-b7`): HECHA · verificación completa VERDE salvo 1 fallo del backend del escritorio · lista para integrar

- **Rama:** `spark/t5-b7`, desde `9eb593c`. Empujada, sin `--force`.
  - Fusiona `origin/integracion-fase1-fase2` seis veces:
    - `e11b4246` en `5a39eae0`;
    - `d994e586` (B14) en `e058645f`;
    - `eb863f54` (B1b-Q10) en `513acee9`;
    - `803e9723` (B2, B3, B6 y B13) en `671646f9`;
    - `d933dcde` (backend F2) en `4df8a380`;
    - `ef78baad` (D-035 en el GDD, F3-1 `AF.Backend`, Firebase dev) en `3f02eaaf`.
  - Solo hubo un conflicto, en esta misma entrada de `spark-estado.md` (se quedaron las dos).
  - **Por instrucción del coordinador, no se fusiona más:** el escritorio sigue moviendo la integración.
- **Nota de diseño:** `docs/dev/remote-config-analytics.md`. Recoge qué hay, cómo se lee 08 §9, el bus, la analítica, los parches y las preguntas Q-B7-1 a Q-B7-23.
- **Commits** (del más antiguo al más reciente):
  - `b7f0cdc5`: el generador `tools/remote-config/rc_defaults.py` (+ `test_rc_defaults.py`), `RemoteConfigDefaults.json` (251 claves) y su copia en C#;
  - `cebbd61a`: `AF.Events` y `AF.RemoteConfig` sin motor, con `AF.RemoteConfig.Binding` (las ocho clases de cifras, sin reflexión);
  - `1632f08b`: `AF.Analytics` (catálogo de 08 §8.2 y 04 §11, comunes de 08 §8.1, consentimiento y sumideros), `AF.Analytics.Unity` (logcat `AF_EVENT`) y `AF.Analytics.Adapters` (anuncios e IAP);
  - `46c0450d`: la sesión (Q-M4);
  - `b8895a8d`: los `.meta`;
  - `18f1e0db`: `biz_feature_rescue` y el anti-reloj del Reto en el binder;
  - `7697f094`: los arreglos de la 1.ª revisión;
  - `267da94c`: la nota y los parches;
  - `d2889a40`: los arreglos de la revisión final;
  - `a464e272`: los parches regenerados sobre `e11b424`;
  - `493e4d35`: `RcJson` rechaza los no finitos también en Mono;
  - `60eec9b2`: el escaneo de `[RemoteConfigKey]` recorre todo ensamblado que referencie `AF.Platform` (con un mínimo de 6);
  - `03f47cf4` y `3ab28fde`: `B7-bootstrap.diff` regenerado (ver «Parches»);
  - `e4ecfd7c`: `ComebackConfig` y `PushConfig` (B6) en `NotBoundYet`;
  - `dd7f0270`: `RemoteConfigDefaults` regenerado desde el 08 §9 de D-035 y `B7-gdd.diff` retirado;
  - `a54d5c0d`: `VerificationQueueConfig` (F3-1) en `NotBoundYet`;
  - las fusiones;
  - y el commit de esta entrada.
- **En la rama** (archivos nuevos; solo cambian los suyos):
  - `Scripts/Events/`;
  - `Scripts/RemoteConfig/`, con `Binding/` y `Tests/`;
  - `Scripts/Analytics/`, con `Unity/`, `Adapters/` y `Tests/`;
  - `Data/Config/RemoteConfigDefaults.json` (generado);
  - `tools/remote-config/`.
- **Parches** (`docs/dev/traspaso/spark-enganches/B7-*.diff`, en LF). Son **siete**: `B7-asmdef`, `B7-wallet`, `B7-levelflow`, `B7-appflow`, `B7-gamescreen`, `B7-rivermap` y `B7-bootstrap`. El orden de compilación está en la nota §5.
  - `B7-bootstrap.diff` se **regeneró dos veces**, porque `ConnectTelemetry();` va tras la última `SetUp*` de `GameBootstrap.Start`:
    - `03f47cf4`: tras `SetUpMeta();` (B14);
    - `3ab28fde`: tras `SetUpSessions();`, ya con B3 y B6 aplicados en la integración. Ya no hay choque a mano con `B3-ui` ni `B6-bootstrap`.
  - **`B7-gdd.diff` se retiró** (`dd7f0270`). La integración ya corrigió `anim_speed_factor_late` = 1,0 en 08 §9 y 02 §4 (`f4f4bdee`, con D-035), así que Q-B7-15 queda cerrada.
    - La rama regeneró el JSON y su C# desde la tabla nueva. Con D-035, 08 §9 añade `biz_backend_enabled` y quita `sec_appcheck_enforce`; siguen siendo 251 claves.
  - Sobre `ef78baad` (lo último fusionado), los siete aplican con `git -c core.autocrlf=false apply` en un worktree temporal y, aplicados, pasan `apply -R --check`.
- **Cifras sin enlazar** (`NotBoundYet` del test de cobertura, cada una con su motivo):
  - `ShuffleConfig` (B2);
  - `SkipConfig` (B3);
  - `ComebackConfig` y `PushConfig` (B6);
  - `VerificationQueueConfig` (F3-1, `AF.Backend`, `sec_purchase_deferred_ack_h`).
  - Las cinco están ya en la integración y usan sus `Defaults`. Las enlaza el integrador: una tabla en `RcConfigBinder` y una referencia del asmdef de `Binding` a `AF.Backend`.
- **Revisión (code-reviewer):**
  - 1.ª ronda: CAMBIOS NECESARIOS, arreglados en `7697f094`:
    - `TryGet` sin excepción por desbordamiento;
    - tope de `Violations`;
    - referencias Q-B7.
  - Revisión final: arreglos en `d2889a40`:
    - la cobertura del binder por descubrimiento;
    - `band` en `level_start`;
    - un generador más estricto;
    - las notas de aplicación.
  - **Sin ronda de revisión propia:** `493e4d35`, `60eec9b2` y todo lo posterior a las fusiones (parches regenerados, `NotBoundYet`, JSON regenerado). Conviene una pasada corta del integrador.
- **Verificación** (`-Architecture arm64 -TimeoutSec 3600`; tiempos reales). Como mucho hubo 2 Unity a la vez; el otro era de otro agente.

  | Dónde | Comando | Resultado |
  |---|---|---|
  | rama (`dd7f0270`, con D-035 y F3-1) | `dotnet build -warnaserror`; `dotnet test`; `validate_schema.py` | 0 avisos y 0 errores; **1.002 (996 + 6), 0 fallos** (2 min 25 s); 103/103, código 0 |
  | rama (`dd7f0270`) | `rc_defaults.py --check`; `unittest` de `tools/remote-config` | «identico (251 claves)», código 0; 4 tests OK. Antes de regenerar, los dos fallaban, como deben: «DIFIERE», por el 08 §9 de D-035 |
  | temporal: `dd7f0270` + los siete parches | `unity-compile`; EditMode | 0 (172 s); 3.694: **2 fallos** (ver abajo) |
  | idem | PlayMode | **1.704 (1.467 + 237), 0 fallos** (37,6 min) |
  | rama (`a54d5c0d`) | `unity-compile`; EditMode | 0 (23 s); 3.688: **1 fallo**, `ContractExamplesTests.ValidExamplesRoundTrip` (ver abajo) |
  | rama (`e4ecfd7c`, con B2, B3, B6 y B13, sin el backend) | compile; EditMode; PlayMode | 0; **3.617 (3.614 + 3), 0 fallos**; **1.703 (1.466 + 237), 0 fallos** (33,0 min) |
  | temporal: `671646f9` + parches, con el test de `e4ecfd7c` | EditMode; PlayMode (completo y luego con filtro) | **3.623 (3.620 + 3), 0 fallos**. PlayMode 1.704: 1 fallo de carga en `PseudoLocOverflowTests.EnXA_360x640_NoTextOverflows(100)` («Tiempo agotado esperando: victoria», con otro Unity y `dotnet test` en paralelo); relanzado con filtro, **3/3** |
  | rama (`60eec9b2`) y temporales anteriores (`e058645f`, `513acee9`) | compile; EditMode; PlayMode | todo en verde: PlayMode 1.655 (1.425 + 230), 1.685 y 1.699; EditMode 3.479 |

- **Los 2 fallos de la última EditMode:**
  1. **`RcConfigBinderTests.EveryTypeWithRemoteConfigKeys_IsBound_OrDeclaredAsNotBoundYet`**, solo en el temporal:
     - `AF.Backend.Verification.VerificationQueueConfig` llegó con F3-1 con un `[RemoteConfigKey]`, y el test exige enlazarla o declararla;
     - **arreglado** en `a54d5c0d`: va a `NotBoundYet`; en la rama ya no falla.
  2. **`AF.Backend.Tests.ContractExamplesTests.ValidExamplesRoundTrip`**, en el temporal y en la rama:
     - mensaje: «voided-purchases-job.output.json: no se lee (voidedPurchasesJob.output: clave no admitida 'tombstones'.)»;
     - **no es de B7**: B7 no toca `AF.Backend` ni `infra/`. El ejemplo del contrato (`infra/contracts/v1`, F2c, «lápida voided») trae `tombstones`, y el lector del cliente de F3-1 no la admite;
     - **hallazgo para el integrador del escritorio**: no se arregla aquí. Debería fallar igual en `integracion-fase1-fase2`.
  - La EditMode de la rama tras `a54d5c0d` da 3.688 = 3.684 + 3 omitidos + ese único fallo.

  Los worktrees temporales se borraron al terminar.

### [P] y preguntas para el dueño (B7; se avanzó con la recomendada, la (a))

Lo marcado [P] está en la nota:
- el formato `af-rc-defaults/1`;
- las interpretaciones de las diez celdas no literales de 08 §9;
- los dos ensamblados de composición;
- la congelación por `RcSnapshot`;
- la validación contra el catálogo;
- el sumidero `AF_EVENT`;
- la sesión.

| Id | Pregunta | Recomendada (aplicada) |
|---|---|---|
| Q-T1 | Bus `AF.Events` frente a las interfaces de escucha | (a) bus mínimo sin motor (D-033); las interfaces siguen |
| Q-B7-1 | 08 §2.2: `AF.RemoteConfig` → `Backend` | (a) sin dependencia mientras no haga falta. `AF.Backend` existe desde F3-1, pero RC no lo usa |
| Q-B7-2 | `AF.Events` sin fila en 08 §2.2 | Añadir su fila |
| Q-B7-3 | Cómo leen RC y la analítica los módulos | Dos ensamblados de composición (`AF.RemoteConfig.Binding`, `AF.Analytics.Adapters`) que solo usa `AF.UI` |
| Q-B7-4 / Q-B7-5 | «resto» en los mapas por país; los placements de `ads_rv_cooldown_s_<placement>` | La clave `"*"`; los seis de `ads_rv_cap_<placement>` |
| Q-B7-6 | Defaults que remiten a otra tabla | `null` + `pending`; cada bloque transcribe la suya a 08 §9 |
| Q-B7-7 | Valor remoto que no pasa `Validate()` | La clase entera vuelve a `Defaults`. Ojo: las `Validate()` no comprueban los rangos de 08 §9 |
| Q-B7-8 | `biz_maintenance_message` | Una clave de `strings.csv` |
| Q-B7-9 | Analítica del Reto | Sigue como sonda `AF_STATE`; su gancho va en un parche aparte |
| Q-B7-10 a Q-B7-13 | `double_harvest`, `af_level_abandon.reason`, comunes y `boosters_json` | Estado en la victoria; solo `map`; a todos los eventos, sin los null; `undo` y `extra_cuia` |
| Q-B7-14 | Qué apaga cada `biz_feature_<sistema>` | `lives` = canoas fuera; `rescue` = sin Cuia de rescate; los demás, con su sistema |
| Q-B7-15 | `anim_speed_factor_late` | **Cerrada**: la corrigió la integración |
| Q-B7-16 a Q-B7-21 | Formato de logcat, `session_id`, booleanos, `rc_variant`, consentimiento sin pantalla y cómo llega el JSON | `AF_EVENT {json}` solo en desarrollo; GUID; `true`/`false`; `biz_experiment_tag`; recogida apagada; JSON compilado como constante |
| Q-B7-22 / Q-B7-23 | `pours` y `t_ms` en los eventos de tablero; `level_start.band` en la campaña | Añadirlos al enganchar los comodines; mandar `difficulty.band` |

### Riesgos y pendientes

- **Ganchos pendientes** (el catálogo ya los admite):
  - `af_boot`, `af_consent` y `af_wallet_snapshot`;
  - comodines, modal y canoas;
  - Crecida, Canasta y Reto;
  - Ajustes, etiquetas, mundo y mecánicas;
  - `af_level_load` y `af_error`;
  - el `level_end` del Salto;
  - lo de B14 y B8;
  - **nuevo:** las claves de D-035, que el binder no lee (`biz_backend_enabled`).
- **Cinco clases de cifras sin enlazar** (ver arriba).
- **Sin pantalla todavía:** «Actualiza Tintalú» y el aviso de mantenimiento.
- **`CLAUDE.md`**, para el integrador (nota §7): las carpetas nuevas, `RemoteConfigDefaults.json` como generado y los comandos de `tools/remote-config/`. Si 08 §9 vuelve a cambiar, hay que regenerar (`python tools/remote-config/rc_defaults.py`); el `--check` lo detecta.
- **Teléfono:**
  - comprobar `AF_EVENT` en logcat;
  - comprobar que el stripping High no rompe el binder.

---

## 2026-10-20 · T5 · B13 · Borradores de publicación (`spark/t5-b13`): HECHA · lista para integrar (solo documentación)

- **Rama:** `spark/t5-b13`, desde `origin/integracion-fase1-fase2` en `9982bdc`. Commits `76d442b`, `9180887` y `b2577df` (revisión).
- **Qué entrega.** Todo es borrador: lo que firma el abogado o decide el dueño va como `[PENDIENTE: dueño]` o `[PENDIENTE: abogado]`, sin datos inventados.
  - En `docs/store-listing/`:
    - `README.md`, con el índice, las longitudes medidas en NFC, las preguntas Q-B13-1..23, los hallazgos de audio en «3 bis» y los enganches del GDD;
    - las fichas de Play en `es-419`, `en` y `pt-BR` (Méx. con OXXO y Bra. con Pix, pendientes de Q-B13-17);
    - `data-safety.md`, `iarc.md` (ClassInd 14 esperado), `privacidad.md` (anexos de Méx. y LGPD) y `eliminar-cuenta.md`;
    - la plantilla `app-ads.txt`;
    - `assets-tienda.md`, el encargo de arte para el artista;
    - `creditos.md`, más las 16 claves `credits_*` en `strings-creditos.csv`.
  - En la raíz, `LICENSES.md`.
  - En `docs/qa/`, `regresion-release.md`, con 60 casos y su fuente.
- **Longitudes en NFC** (título 30 / corta 80 / larga 4.000 / novedades 500 / texto de captura ≤ 36 [P]):
  - es-419: 21 / 78 / 2.487 / 143 / 36;
  - en: 26 / 74 / 2.346 / 145 / 31;
  - pt-BR: 25 / 73 / 2.431 / 134 / 34.
- **Verificación:**
  - `python tools/validate_schema.py`: 103/103;
  - `dotnet build -warnaserror` y `dotnet test`: 0 avisos y 981 (975 + 6). Los pasó el revisor en la primera ronda;
  - `git diff --check` limpio;
  - no toca el GDD, `Assets/` ni `tools/`.
- **code-reviewer:**
  - 1.ª ronda, CAMBIOS NECESARIOS:
    - «cada color es una fruta» (son 12 frutas y 2 aguas);
    - el 77 en lugar de 74;
    - el límite de las capturas contradictorio;
    - un mecanismo de borrado inventado;
  - 2.ª ronda: **APROBADO**.
- **Para el integrador:**
  - pasar las filas `credits_*` a `strings.csv` cuando exista la pantalla de Créditos;
  - corregir el glosario §1.3 (la descripción corta EN mide 74, no 77);
  - llevar al GDD lo que decida el dueño (07 §10.2, 08 §11, 08 §13.3).

### Preguntas y hallazgos para el dueño (no bloquean lo entregado)

Las 23 preguntas, con sus opciones y la recomendada, están en `docs/store-listing/README.md` §3. Las que más pesan:

| Id | Pregunta | Recomendada |
|---|---|---|
| Q-B13-1, -2, -3 | Dominio de la página de borrado, titular legal en Play y correos de contacto, privacidad y DPO | Los fijan el dueño y el abogado; ninguna forma de «Amazon» |
| Q-B13-5 | El texto de las capturas va «en papel marfil» (07 §10.2), que choca con D-003 (sin papel) | Placa de madera clara o de vidrio del kit de UI |
| Q-B13-9 | `com.unity.modules.unityanalytics` está en `Packages/manifest.json` | build-release comprueba en B10 que esté apagado en release |
| Q-B13-20 | Unity IAP 5.4.x necesita Unity Authentication, que no tiene fila en 08 §13.3 | Quien integre IAP la confirma y se añade la fila |
| Q-B13-21 | El borrado por web solo pide el ID visible: quien conozca el ID de otro puede borrar su cuenta (hueco de 08 §11) | ID largo, aleatorio y distinto del UID, con confirmación y límite de peticiones |
| Q-B13-22 | Cómo se borra la partida de Play Games si el borrado llega por web sin el juego instalado | Decir que se borra al abrir el juego, o que el jugador puede borrar su perfil de Play Games |
| Q-B13-23 | La captura 2, «14 frutas, 14 colores» (07 §10.2), frente a 12 frutas + 2 aguas y ≤ 12 colores por nivel en la 1.0 | «14 colores de frutas y ríos» [P] |
| — | Audio: toda la música `mus_*`, incluido `music/menu.wav` (el del vídeo), es CC0, frente al encargo original con cesión total (07 §9.5). `amb_w01_fauna_02` lleva voces humanas y es de Río de Janeiro. El autor «Tintalú Sound Design» usa la marca | El vídeo y los creativos no usan esa música hasta que haya una pista con cesión total; decidir si se mantiene el encargo original; revisarlo con el asesor y el abogado |

## 2026-10-20 · B6 · Sesiones, Regalo de regreso y notificaciones, solo la lógica (`spark/t5-b6`): HECHA · verificación completa VERDE · lista para integrar

- **Rama:** `spark/t5-b6`, desde `9e5d13f`. En `c4322199` fusiona `origin/integracion-fase1-fase2` (`dbbba004`, con FX16a y FX16b). Empujada.
- **Nota de diseño:** `docs/dev/sesiones-notificaciones.md`. Recoge las reglas, los parches, los riesgos del programador real y las preguntas B6-Q1 a B6-Q18.
- **Commits** (del más antiguo al más reciente):
  - `a387b0af`: lógica sin motor y tests;
  - `c08b4c50`: fábrica de almacenes, sin sesión nueva al reconstruir la UI;
  - `93020e1d`: la nota, el CSV y el gancho del programador real;
  - `db587c6f`: los `.meta`;
  - `eec488f7`: B6-profile y B6-bootstrap;
  - `fb8fc1de`: B6-notifications-real;
  - `50a3b0a1`: los arreglos de la revisión;
  - `2ceb2364`: el caso pendiente;
  - `c4322199`: la fusión;
  - y el commit de esta entrada.
- **En la rama** (todo archivo nuevo):
  - `Scripts/Platform/INotificationScheduler.cs` y `FakeNotificationScheduler.cs`;
  - en `Scripts/LiveOps/`:
    - sesiones: `SessionRecord` y `SessionTracker`;
    - regalo: `ComebackConfig`, `ComebackRules` y `ComebackService`;
    - `PushConfig`;
    - pre-prompt: `PushSettingsRecord`, `PushPrePromptRules`, `PushPermissionService` e `IPushPrePromptPresenter`, con `SimulatedPushPrePrompt`;
    - avisos: `NotificationType`, `NotificationContext`, `NotificationPlanner` y `NotificationService`;
    - `SessionProbeFormat`;
  - sus tests EditMode, en `Scripts/LiveOps/Tests/`;
  - `Scripts/UI/GameBootstrap.Sessions.cs` y `SessionRunner.cs`, que no se llaman hasta B6-bootstrap;
  - `docs/dev/traspaso/spark-enganches/B6-strings.csv`, con 21 claves en `pendiente_asesor`.
- **Parches** (`docs/dev/traspaso/spark-enganches/`, en LF). Los tres pasan `git -c core.autocrlf=false apply --check` tras la fusión:
  1. **`B6-profile.diff`:**
     - `Profile.cs` y `ProfileJson.cs`: `settings.push`, que se escribe solo fuera de su default, y `stats.last_seen_at` y `stats.last_win_at` [P], que se omiten mientras son null. Lectura tolerante y sin migración;
     - añade `LiveOps/ProfileSessionStore.cs` y `ProfileSessionStoreTests.cs`.
  2. **`B6-bootstrap.diff`** (después del 1):
     - `SetUpSessions();` en `GameBootstrap.Start`, tras `SetUpStreak();`;
     - `GameBootstrap.SessionsProfile.cs`;
     - `"AF.LiveOps"` en `AF.Tests.EditMode.asmdef`;
     - `GameBootstrapSessionsTests.cs`.
  3. **`B6-notifications-real.diff`** (aparte; no se aplica en las pruebas generales). Antes, anotar la versión en D-033. Trae:
     - el manifiesto;
     - `AF.UI.asmdef`, con la referencia y el `versionDefines` `AF_MOBILE_NOTIFICATIONS`;
     - `AndroidNotificationScheduler.cs`;
     - `GameBootstrap.Notifications.cs`.
  4. **Textos:** las filas de `B6-strings.csv`, a `strings.csv`. Sin ellas no se programa ningún aviso.
- **Choque con B3 y B7 en `GameBootstrap.cs`.** `B3-ui.diff`, `B6-bootstrap.diff` y `B7-bootstrap.diff` añaden cada uno una línea justo después de `SetUpStreak();`. El integrador lo resuelve a mano con este orden:
  ```
  SetUpStreak();
  SetUpSkip();        // B3
  SetUpSessions();    // B6
  ConnectTelemetry(); // B7
  ```
- **Riesgo con B8** (en curso). `B8-profile.diff`, en `spark-t5-b8`, toca `Profile.cs` y `ProfileJson.cs` en otros puntos:
  - Lo probé: aplica (`--check`) encima de B6-profile en el worktree temporal. No hay choque de texto hoy, pero puede haberlo si B8 cambia.
  - Choque de sentido:
    - la firma o integridad del perfil y la fusión en la nube de B8 tienen que cubrir `settings.push` y `stats.last_seen_at` / `last_win_at`;
    - propuesta para la fusión: `last_seen_at` y `last_win_at` por máximo, y `pre_prompt_shown` / `second_prompt_shown` por OR. `granted` sería el del dispositivo, no el de la nube [P].
- **Revisión (code-reviewer):**
  - 1.ª ronda: CAMBIOS NECESARIOS, con 2 bloqueantes, ya arreglados en `50a3b0a1`:
    - Nuevos niveles no salía nunca con el Reto desbloqueado, y Crecida casi nunca. Ahora el orden es Regreso > avisos puntuales > avisos diarios, y un puntual que pierde su día pasa una vez al siguiente;
    - el segundo pre-prompt salía con una racha que ya estaba en 3 o más. Ahora sale solo en la victoria que alcanza el hito 3 después del primero.
  - Sugerencias aplicadas:
    - un aviso sin texto no gasta el cupo del día;
    - el Regreso no se avisa antes de `live_comeback_gift.days`;
    - el arranque no usa el pre-prompt simulado: no gasta los pre-prompts del perfil de pruebas.
  - 2.ª ronda: **APROBADO**.
- **Verificación** (`-Architecture arm64 -TimeoutSec 3600`; como mucho 2 Unity a la vez):

  | Dónde | Comando | Resultado |
  |---|---|---|
  | rama (`50a3b0a1`) | `dotnet build -warnaserror`; `dotnet test`; `validate_schema.py` | 0 avisos; 981 (975 + 6), 0 fallos; 103/103, código 0 |
  | rama (`50a3b0a1`) | `unity-compile`; EditMode; PlayMode | 0; **3.423 (3.420 + 3), 0 fallos** (64 nuevos de B6); **1.589 (1.382 + 207), 0 fallos** |
  | temporal con B6-profile + B6-bootstrap (`50a3b0a1`) | compile; EditMode; PlayMode | 0; **3.432 (3.429 + 3), 0 fallos**; **1.589: 1 fallo, `WorldBannerTravelTests.Travel_MeshesBuildIn8ms` (8,0004 ms frente a un tope de 8 ms, el inestable)**; relanzado con filtro, 13/13 |
  | temporal con el real sin la línea del manifiesto | compile | 0, con los `.meta` del diff; luego revertido |
  | rama tras la fusión (`c4322199`) | `dotnet build -warnaserror`; `dotnet test`; `validate_schema.py`; compile; EditMode | 0 avisos; 981 (975 + 6); código 0; 0; **3.431 (3.428 + 3), 0 fallos**. PlayMode no se repite: la fusión solo trae cambios del escritorio (FX16a/b, BuildRelease) que no tocan el código de B6 |

- **Versión para D-033:** `com.unity.mobile.notifications` **2.4.3**.
  - El repositorio no fija ninguna (08 §7 y §15 solo nombran «Mobile Notifications»).
  - Es la que declara el Editor 6000.3.25f1 instalado (`Editor/Data/Resources/PackageManager/Editor/manifest.json`: `version` y `minimumVersion` 2.4.3). Es un archivo local; no se navegó.
- **Cómo comprobar `POST_NOTIFICATIONS`** tras aplicar el parche real y hacer el APK de desarrollo:
  - `aapt2 dump permissions Builds/Android/dev.apk`, o `bundletool dump manifest` en el AAB;
  - debe listar `android.permission.POST_NOTIFICATIONS`;
  - y **no** `SCHEDULE_EXACT_ALARM`, `USE_EXACT_ALARM` ni `RECEIVE_BOOT_COMPLETED`.
  - En Project Settings › Mobile Notifications: «Schedule at exact time» en `Never`, «Reschedule on Device Restart» apagado y nada que pida el permiso al arrancar.
  - En el teléfono: que no salga ningún diálogo en el arranque en frío.
- **Riesgos del programador real** (detalle en la nota §8):
  - el cuerpo del `#if` no está compilado sin el paquete;
  - faltan los iconos (arte);
  - `af_push_sent` es aproximado;
  - hay que pasar `ci/check-16kb.sh` y `ci/size-gate.ps1`.

### Preguntas para el dueño (B6; se avanzó con la recomendada, marcada [P])

| Id | Pregunta | Recomendada |
|---|---|---|
| B6-Q9 | Con 1 aviso al día y la prioridad literal de 06 §8.2, el Reto listo de cada día haría que Nuevos niveles no saliera nunca y que Crecida te espera casi nunca saliera | (b): Regreso > puntuales (Crecida > canoas > nuevos niveles) > diarios (Reto > Bitácora); un puntual que pierde su día pasa una vez al siguiente |
| B6-Q1 | Con qué se miden los días sin sesión | `stats.last_seen_at` nuevo, en periodos de 24 h |
| B6-Q2 | Qué guarda `settings.push` para el segundo pre-prompt | `pre_prompt_at` + `second_prompt_shown`; sale en la victoria que alcanza el hito 3 después del primero o desde 7 días después |
| B6-Q3 | ¿Se avisa antes de «Avísame»? | No; `granted` solo se baja al abrir |
| B6-Q4 | «Racha de Retos» en la 1.0 | Sí, como variante del Reto listo |
| B6-Q5 | Días del Regreso | D7, D14, D30 y D60; los textos de D14, D30 y D60 son propuestos |
| B6-Q6 | `source` de las semillas del regalo | `event` |
| B6-Q7 | «Una victoria más» aunque falten más | Texto con `{left}` y plural |
| B6-Q8 | Título y canal de Android | «TiTino» y «Avisos del río» |
| B6-Q10 | Reparto sin abrir | 3 por semana; los diarios en días alternos |
| B6-Q11 | Aviso en horas silenciosas | Se mueve a las 09:00 |
| B6-Q12 | Tope diario después del día 7 | 1 al día siempre |
| B6-Q13 | Hora del Reto listo | 09:30 |
| B6-Q14 | Crecida con las 36 h ya pasadas | Sin aviso |
| B6-Q15 | Anti-reloj del regalo | Con un desfase mayor que `sec_clock_max_drift_h`, sin regalo, y ese regalo se pierde. Sin red, cambiar el reloj lo repite |
| B6-Q16 | Cuándo sale la tarjeta del pre-prompt | Tras el panel de la victoria y antes de la Mochila; el disparador se mueve ahí con la tarjeta (UI de otro bloque) |
| B6-Q17 | Valores de `af_push_prompt` | `result` ∈ {`accepted`, `declined`, `granted`, `denied`}; `origin` ∈ {`first`, `second`, `settings`} |
| B6-Q18 | Valores de `type` de los avisos | `daily_ready` … `comeback_d60` |

- **Caso pendiente:**
  - Si el primer pre-prompt sale en la victoria que deja la Crecida en 3, y después se gana un Reto que no mueve la racha, el segundo saldría en esa victoria.
  - Para cerrarlo haría falta guardar la racha del primero (otro campo [P]).
- **Falta UI** (fuera de B6): la tarjeta del pre-prompt, la fila «Avisos» de Ajustes y el aviso del Regalo de regreso (`GameBootstrap.LastComeback`).

## 2026-10-20 · B3 · Salto del Boto (`spark/t5-b3`): HECHA · verificación completa · lista para integrar

- **Rama:** `spark/t5-b3`, desde `origin/integracion-fase1-fase2` `9982bdc` (con B1a). Sin push (lo pidió el encargo de esta tanda).
- **Nota de diseño:** `docs/dev/salto-del-boto.md` (reglas S1-S10, reparto, preguntas Q-S1-Q-S9, riesgos).
- **Commits:** `d8f1fb4` (lógica, vista y tests en archivos nuevos), `62963e9` (almacén según el progreso), `e9440f7` (nota y parches),
  `28a6876` y `bd4be8d` (revisión: el Reto sin Salto, la Cuia extra, la clave `skip`), `ad2ce8b` (.meta y B3-ui.diff regenerado),
  y los dos del arreglo de las mascotas (Rosa se pinta al enseñarla; B3-ui.diff regenerado, `e79f624`).
- **Archivos nuevos:** `Scripts/Boosters/SkipBooster.cs`; `Scripts/GameFlow/SkipStore.cs` y `LevelFlow.Skip.cs`; `Scripts/UI/SkipText.cs`,
  `SkipUi.cs`, `GameBootstrap.Skip.cs`, `SkipRunner.cs` y `DevProbe.Skip.cs`; tests EditMode `SkipBoosterTests`, `SkipFlowTests`,
  `SkipProbeTests` y `SkipDailyTests`.
- **E34 cumplido:** `LevelFlow.Skip()` llama a `OnSkipStreak()` dentro de su `using (Saves())`, tras el cobro y **antes** de
  `ISkipStore.SaveSkip`; el cobro, la Canasta del 10k, el salto y el `level_state` del nivel siguiente salen en **una** escritura
  (`SkipFlowTests.SkippingA10k_PaysItsBasket_AndEverythingGoesOutInOneWrite`). El nivel saltado **cuenta para `max_level`** (sin
  `stats.levels_won`), como pedía el aviso del revisor de B1a.
- **Revisión (code-reviewer):** 1.ª ronda CAMBIOS NECESARIOS (sin test del Reto, sin test del aviso de la Cuia extra, clave del CTA fuera
  del glosario, faltaba Unity con los parches); arreglado. 2.ª ronda: solo faltaba commitear el .meta y el B3-ui.diff regenerado (hecho en
  `ad2ce8b`); «aprobado sin otra ronda de código» con PlayMode completo en verde.

### Enganches para el integrador (en este orden, en LF, en `docs/dev/traspaso/spark-enganches/`)

1. `B3-model.diff`: `StuckModalModel.cs` (`StuckContext.Skip`, `StuckModalView.Skip`: la rama D) y `LevelFlow.Funnel.cs`
   (`BuildStuckContext` rellena `Skip = SkipOffer`); añade `Tests/EditMode/StuckModalSkipTests.cs` (+ .meta).
2. `B3-ui.diff` (necesita el 1): `StuckModal.cs` (la opción con precio y «Rosa te lleva…» o «Te faltan»), `PauseMenu.cs` (botón con Rosa y
   precio), `GameScreen.cs` (pasa `flow.SkipOffer` y `OnPauseSkip`), `GameBootstrap.cs` (`SetUpSkip();` tras `SetUpStreak();`); añade
   `Tests/PlayMode/SkipPlayModeTests.cs` (+ .meta).
3. `B3-strings.csv`: 6 claves (`stuck_skip`, `skip_tagline`, `skip_confirm_title`, `skip_confirm_extra_cuia`, `skip`, `skip_banner`), a
   copiar en `strings.csv`; `pendiente_asesor`.
- Sin parche de `Profile`/`ProfileJson` (ya guardan `since_last_skip` y `skipped_levels`) ni de `RiverMapScreen*` (ya pinta a Rosa con
  `max_level` + `skipped_levels`).
- Prueba: `git apply` de 1 y 2 sobre la rama, `unity-compile`, EditMode y PlayMode (`StuckModalSkipTests`, `SkipPlayModeTests`).
- **Choque con `B7-bootstrap.diff`:** `B3-ui.diff` y `B7-bootstrap.diff` insertan una línea en el mismo punto de `GameBootstrap.cs`,
  justo después de `SetUpStreak();`. Se resuelve a mano dejando primero `SetUpSkip();` y después `ConnectTelemetry();`.
- **Rehecho sobre FX16a/FX16b** (`e11b424`): `B3-ui.diff` se regeneró sobre el `StuckModal.cs` nuevo; la rama D usa las medidas de la
  disposición compacta (`FitCtaLabelCompact`, `PlaceButton`, `HeightOf`, `gap`), como la Cuia de rescate.

### Verificación

| Dónde | Comando | Resultado |
|---|---|---|
| rama | `dotnet build dotnet/AmazonFill.slnx -warnaserror` | 0 avisos, 0 errores |
| rama | `dotnet test dotnet/AmazonFill.slnx` | 981 (975 + 6 omitidos), 0 fallos |
| rama | `python tools/validate_schema.py` | 103/103 PASS, sin cambios en `tools/out/` |
| rama | `unity-compile.ps1 -Architecture arm64` | OK (código 0) |
| rama | EditMode (`-TimeoutSec 3600`) | **3.384 (3.381 + 3 omitidos), 0 fallos** |
| rama | PlayMode (`-TimeoutSec 3600`) | 1.589 (1.380 + 207 omitidos), **2 fallos de tiempo** con otro Editor (B2) en marcha: `WorldBannerTests.Banner_TimeoutAndWaitMs_UseTheRealClock` y `WorldBannerTravelTests.Travel_MeshesBuildIn8ms` (10,63 ms > 8); relanzados con filtro: **30/30 verdes** |
| worktree temporal con B3-model + B3-ui | `git apply` de los dos | aplican limpios |
| ídem | `unity-compile.ps1` | OK |
| ídem | EditMode | **3.389 (3.386 + 3 omitidos), 0 fallos** |
| ídem | PlayMode | 1.ª pasada: 1 fallo real, `WorldBannerTests.Banner_NotOnRetryNorResume` (`MascotsBuilt`): la confirmación, el banner y la pausa pintaban a Rosa al arrancar; arreglado en `e5519fa` y `e79f624` (Rosa solo al enseñarlos). 2.ª pasada: **1.590 (1.383 + 207 omitidos), 0 fallos** |
| rama tras el arreglo | `unity-compile.ps1` | OK |
| retoques tras la aprobación (`cabddef`, `a82a97e`) | rama: `dotnet build`/`test`, `validate_schema`, compile, EditMode, PlayMode | 0 avisos; 981 (975 + 6); 103/103; OK; **3.384 (3.381 + 3), 0 fallos**; **1.589 (1.382 + 207), 0 fallos** |
| ídem | worktree con B3-model + B3-ui: compile, EditMode, PlayMode | OK; **3.389 (3.386 + 3), 0 fallos**; **1.590 (1.383 + 207), 0 fallos** (`SkipPlayModeTests` en verde) |
| tras fusionar `e11b424` (FX16a/FX16b; `c975184`, `be82d8d`) | rama: `dotnet build`/`test`, `validate_schema`, compile, EditMode, PlayMode | 0 avisos; 981 (975 + 6); 103/103; OK; **3.392 (3.389 + 3), 0 fallos**; **1.655 (1.425 + 230), 0 fallos** |
| ídem | worktree con B3-model + B3-ui regenerado: `apply --check` en orden, compile, EditMode, PlayMode | aplican; OK; **3.397 (3.394 + 3), 0 fallos**; **1.656 (1.426 + 230), 0 fallos** (`SkipPlayModeTests` en verde) |

### Decisiones [P] y preguntas para el dueño

Detalle en la nota §3 (todas con la recomendada aplicada):
- **Q-S1:** `last_skip_level` se deduce del mayor de `skipped_levels` (sin campo nuevo); `since_last_skip` se pone a 0 y no se lee. El ejemplo
  de 08 §4.3 no casa con ninguna lectura: hay que corregir el GDD.
- **Q-S2:** hueco N − último ≥ 10 (salto en el 40 → el 50 ya puede).
- **Q-S3:** banner como franja con Rosa sobre el tablero mientras el intento no empezó (tarjeta, intro o sin vertidos).
- **Q-S4:** sin nivel siguiente o sin canoas para empezarlo, el Salto no se ofrece.
- **Q-S5:** la animación de Rosa (1,5 s) y su sonido esperan al arte y al audio.
- **Q-S6:** `stuck_skip` partido en etiqueta + precio + `skip_tagline`.
- **Q-S7:** se ofrece en Aguas estancadas, en la pausa y en el banner; no en ¿Seguro?, Nivel fallido ni Sin canoas.
- **Q-S8:** textos sin canon («¿Saltar el nivel?», «Perderás la Cuia extra de este nivel»), `pendiente_asesor`.
- **Q-S9:** `skip_min_level`: 05 dice «mínimo 31», 08 §9 da 20-31; el cliente solo exige ≥ 1 (el revisor sugiere no bajar de `rescue_min_level`).
- Nueva (revisor): ¿se enseña al jugador la Canasta que paga el salto de un 10k? Hoy solo sale en la sonda.
- **Q-S1, consecuencia:** `since_last_skip` ya no se usa; corregir 02 §2, 04 §6.1-§6.2 y el ejemplo de 08 §4.3, y avisar a B7
  (`af_skip_used.since_last_skip` = nivel − último salto) y a B8 (fusión en la nube).
- **Q-S6, consecuencia:** toca la tabla de 04 §6.5.
- **Pendiente:** medir el primer pintado de Rosa con `MascotsMainMs` (tope de 8 ms, 08 §5.4).
- **Retoque tras la aprobación:** el banner sin saldo se enseña en gris con «Te faltan {n} semillas» (clave existente
  `stuck_rescue_missing`, sin texto nuevo) y no responde al toque.

### Riesgos

- La rama D suma ~70 dp al panel de Aguas estancadas: el caso conocido de 16:9 al 130 % empeora si coinciden B y D (sin medir).
- `af_skip_used` solo en la sonda de desarrollo hasta B7; `SkipConfig.Defaults` hasta el lector de Remote Config.
- Sugerencias del revisor no hechas: id del banner en la AF_LAYOUT `level` (toca `GameScreen`), «Te faltan N» en la pausa sin saldo,
  rama D en un test de tamaños.

---

## 2026-10-20 · B2 · Pororoca (`spark/t5-b2`): HECHA · verificación completa VERDE · lista para integrar

- **Rama:** `spark/t5-b2`, desde `origin/integracion-fase1-fase2` en `9982bdc` (con B1a). Sin empujar (el encargo de B2 dice «no hagas push»).
- **Nota de diseño:** `docs/dev/pororoca.md` (qué va en la rama y qué en parches, el algoritmo, el flujo, la vista, las [P] P-1 a P-19 y los hallazgos para el GDD).
- **Commits:** `eaad940` (código de la rama: `ShuffleBooster`, `IShuffleListener`, `ProceduralArt.Shuffle`, `ShuffleText`, `ShuffleConfirm`, `ShuffleIntroCard`, `ShuffleWaveFx`, `IShuffleActions` y `ShuffleBoosterTests`, con sus `.meta`) y el de la nota, los parches y esta entrada.
- **Revisión (code-reviewer):** 1.ª ronda, CAMBIOS NECESARIOS (faltaban commit, parches y `.meta`; la contradicción 03 §9 / 04 §2.4; tres tests). Arreglado (P-16 y P-17; tests `UnknownStartingPar…`, `OpenedOrReleasedMechanics…` y `MurkyLevel_Firefly_Shuffle_Resume…`; el cobro comprobado; la placa de la tarjeta sin raycast y el bloqueador de la ola con `HitArea`). 2.ª ronda: **APROBADO**, condicionado al commit y a la verificación completa, hechos abajo.
- **Enganches para el integrador** (en `docs/dev/traspaso/spark-enganches/`, LF; `git apply --check` OK). Se aplican **después de fusionar `spark/t5-b2`** y **en este orden**:
  1. `B2-solver.diff`: `Packages/com.amazonfill.core/` (`AF.Solver/Shuffle/` nuevo con `Shuffler`, `ShuffleOptions` y `ShuffleResult`; `LevelSession.ApplyShuffle`; `ShufflerTests` y `LevelSessionShuffleTests`) y `dotnet/AF.Core.Tests/DotnetOnly/ShuffleAllocationTests.cs`, con los `.meta` que generó Unity.
  2. `B2-flow.diff`: `Boosters/ShuffleWave.cs` y `GameFlow/LevelFlow.Shuffle.cs` (nuevos); ganchos de una línea en `LevelFlow.cs` (`Load` y `BeginAttempt`); `LevelFlow.Resume.cs` (rehacer con las olas, `RestoreShuffle`, `WriteHistory`); `LevelStateSnapshot.cs` (marca `*` en V, marcas `o` y `p`; h2 sin versión nueva); `Economy/Wallet.cs` (`ShufflesToday` y `daily.shuffles_today` al cobrar); `Tests/EditMode/ShuffleFlowTests.cs`.
  3. `B2-ui.diff`: `BoosterTray.cs` ✱ (ranura Barajar entre Pista y Revelar, globo, latido, `PaintShuffle`); `GameScreen.cs` ✱ (6 ganchos `Shuffle*`); `GameScreen.Shuffle.cs` y `GameBootstrap.Shuffle.cs` (nuevos); `DailyChallengeScreen.cs` (la Pororoca en el Reto); `Tests/PlayMode/ShufflePlayModeTests.cs` (nuevo), `PlayModeHelpers.cs` (`NoShuffleUnlessSet`, P-19) y `LocTablesTests.cs` (`ShuffleText.AllKeys`).
  4. `B2-strings.csv`: 8 filas para `strings.csv` (`shuffle`, `shuffle_confirm`, `shuffle_confirm_yes`, `shuffle_failed`, `shuffle_low_water`, `shuffle_name`, `shuffle_tagline` y `shuffle_unsupported`), solo `es-419` y `pendiente_asesor`, en orden ordinal. Sin ellas falla `LocTablesTests.EveryUiTextKey_Exists_AndEveryStringsRow_IsUsed`.
- **Verificación:**

  | Dónde | Comando | Resultado |
  |---|---|---|
  | rama (`eaad940`, sin parches) | `dotnet build -warnaserror`; `dotnet test`; `validate_schema.py` | 0 avisos; 981 (975 + 6); 103/103 |
  | rama | `unity-compile -Architecture arm64`; EditMode; PlayMode (`-TimeoutSec 3600`) | OK; **3.368 (3.365 + 3), 0 fallos** (387 s); **1.589 (1.382 + 207), 0 fallos** (2.226 s) |
  | temporal en `9982bdc` + rama + los 3 parches + las 8 filas | `dotnet build -warnaserror`; `dotnet test`; `validate_schema.py` | 0 avisos; **1.002 (996 + 6)**; 103/103 |
  | temporal | `unity-compile`; EditMode; PlayMode | OK; **3.399 (3.396 + 3), 0 fallos** (282 s); **1.593 (1.386 + 207), 0 fallos** (1.863 s) |

  `econ_sim` no se ejecutó: no se toca la economía (los precios y topes son los de 08 §9). `IntroArtTests.FruitFlight…10ms` no falló en ninguna pasada.
- **Decisiones [P]** (con la recomendada; detalle en `pororoca.md` §5): P-1 a P-19. Las que piden al dueño: **P-4** (tope «1 por nivel» en todos los intentos y al salir y volver), **P-5** (Volver a la orilla vuelve al tablero del nivel), **P-6** (sin hoja con el stock), **P-10** (la ola en el hilo principal: medir en el teléfono el peor caso, la que falla), **P-13** (Tinaja con dosis: habilitada y medir en `level-qa`), **P-16** (03 §9 frente a 04 §2.4 en la ola que falla) y **P-18** (bandeja de 5 ranuras en 360 dp: «Shuffle», «Embaralhar», y «Desfazer» y «Revelar» en pt-BR al 130 %, no caben a 12 sp; EN y PT-BR de `shuffle` quedan vacíos).
- **Riesgos:**
  - P-18 es de interfaz: en en y pt-BR la ranura sale sin etiqueta hasta decidirlo.
  - Con P-19 (`GameScreen.ShuffleOverride`, solo vista), los tests PlayMode que no tratan de la Pororoca corren sin ella; la bandeja de 5 ranuras desde el 30 la cubre `ShufflePlayModeTests`.
  - La analítica (`af_shuffle_result`, `af_booster_used`) sale solo como sonda `AF_STATE shuffle` hasta B7.

---

## 2026-10-20 · S2 = B1a · Crecida + Hoja escudo + Canasta (`spark/t5-b1a`): HECHA · verificación completa VERDE · lista para integrar

- **Rama:** `spark/t5-b1a`, desde `a759597`. Fusiona `origin/integracion-fase1-fase2` (`815b2e5`) en `7fbc1cd`.
- **Plan:** `docs/dev/plan-tanda5.md` (S1, en `spark/auditoria-1-0`). **Nota de diseño:** `docs/dev/crecida-canasta.md`, con las reglas C1-C14 y K1-K9, los enganches E26-E35 y las preguntas Q-C1-Q-C14.
- **Tareas y revisiones.** Cada tarea pasó por el code-reviewer; todas quedaron **APROBADAS**, la mayoría en la 2.ª ronda:
  - **A0** (`f68f72c`): la nota.
  - **A1** (`65754d3`): `StreakConfig`, `BasketConfig` y `EconomyJsonLite`. Los defaults se comprueban contra la tabla de 08 §9 leída de verdad.
  - **A2** (`a38653c`): `BasketRules`.
    - Canasta cada 10 niveles: el 10 da solo semillas; el ciclo es `(L/10−2) mod 5`.
    - Canasta grande cada 100: 150 semillas + Cuia extra + Morpho + 3 Marea (GDD.md §6.1 #13).
    - `HitSix`.
  - **A3** (`ead3aa2` y `1a09563`): `StreakRules`.
    - Hitos 3, 6 y 10, una vez al día; tope de 600 con pago parcial; río crecido; Hoja escudo; solo en la campaña y desde el 25.
    - Tests sin asignaciones y un oráculo contra `econ_sim.py`: 5 configuraciones × 24 semillas × 600 pasos.
    - K9: el Salto del Boto también paga la Canasta del 10k (04 §2.6).
  - **A4** (`f226165`, `f03e0ef` y `fb2bd3e`): `StreakRecord`, `IStreakStore`, `InMemoryStreakStore` y el parche S1.
    - Regla conservadora con un registro de un día anterior: puede pagar de menos en el borde de medianoche, nunca dos veces.
    - `Normalize` al leer.
  - **A5** (`cd83385`, `9e3a88d` y `8e978ec`): `StreakService`, con `ApplyWin`, `ApplyHardFail`, `ApplySkip` y `Peek`.
    - Orden: `Load` → `RollDay` → regla → `Save` → `Wallet.Apply`.
    - Fuentes `streak` y `basket`. El kill switch congela la Crecida, pero la Canasta se sigue pagando.
    - La Canasta de un 10k no se repaga, gracias a `maxLevelBefore`.
    - El parche S4.
  - **A6** (`c34cb00`, `701eb56` y `b3258fd`): el parcial `LevelFlow.Streak.cs`.
    - `UseStreak`, `LastStreakResult`, `PeekStreak` y `OnSkipStreak`. `OnSkipStreak` pide `RequireDurable` y va dentro del mismo `Saves()` que el salto.
    - El Reto queda fuera (`!IsDaily`).
    - Con el río crecido no se ofrece el x2 por vídeo.
    - Los parches S2 y S5.
  - **A7** (`546fcf3`, `685bad9` y `04cda21`): `StreakProbeFormat`, `DevProbe.Streak.cs`, `GameBootstrap.Streak.cs`, `StreakProbeRunner` y el parche S3.
    - Líneas `AF_STATE` (solo en desarrollo): `streak_state` y `streak_fail`, más `streak_milestone`, `streak_shield_used`, `free_double_harvest` y `basket_open`.
    - En la victoria salen tras `AF_STATE win` y **antes** de `victory`; en el fallo duro, tras `fail_hard` y `level_failed`.
- **Parches para el integrador** (`docs/dev/traspaso/spark-enganches/`, en LF). Se aplican **en este orden** y **después de fusionar `spark/t5-b1a`**, porque S4 reescribe archivos de la rama:
  1. `S1-profile.diff`: `Profile.cs` (`streak` {`wins`, `river_full`}; `daily.streak_hits` y `daily.streak_seeds`), `ProfileJson.cs` y `DailyCounters.RollOver`; añade `ProfileStreakStore.cs` y `StreakProfileTests.cs`.
  2. `S4-wallet.diff` (E35): `Transaction.DoubleHarvestRiver` (`internal`), sin la compuerta `ads_rv_double_harvest_from_level` y sin gastar `ads_rv_cap_double_harvest`; añade `RiverDoubleHarvestTests`.
  3. `S2-flow.diff` (E29, E30): `LevelFlow.Victory.cs` (`ResolveWin`) y `LevelFlow.Funnel.cs` (`Fail`); añade `StreakFlowHookTests`.
  4. `S5-flow-profile.diff`: test de integración que comprueba una sola escritura del perfil en la victoria y en el fallo duro (necesita S1 y S2).
  5. `S3-bootstrap.diff` (E32): `SetUpStreak();` tras `SetUpDaily();` en `GameBootstrap.cs`, y `GameBootstrap.StreakProfile.cs` (necesita S1).
- **Pendiente de otros bloques:**
  - B3 (`SkipBooster`) debe llamar a `flow.OnSkipStreak()` dentro del `using (Saves())` del salto y antes de guardarlo (E34).
  - B1b: barra, toasts y panel de premios (E33, nota §6.1).
  - B7: `earn_virtual_currency` desde `Wallet.Apply`.
  - Lector de Remote Config: hoy se usan `StreakConfig.Defaults` y `BasketConfig.Defaults`.
  - Volver a correr la simulación de economía con la Canasta grande (05 §3.4).
- **Verificación:**

  | Dónde | Comando | Resultado |
  |---|---|---|
  | rama (`7fbc1cd`) | `dotnet build -warnaserror`; `dotnet test`; `validate_schema.py`; `econ_sim --runs 300` contra `econ_B.md` | 0 avisos; 981 (975 + 6); 103/103; idéntico |
  | rama | `unity-compile`; EditMode; PlayMode (`-TimeoutSec 3600`) | OK; **3.309 (3.306 + 3), 0 fallos**; **1.589 (1.382 + 207), 0 fallos** |
  | worktree temporal en `7fbc1cd` con S1 → S4 → S2 → S5 → S3 | `git apply` de los 5; `unity-compile`; EditMode; PlayMode | todo aplica; OK; **3.359 (3.355 + 3), 1 fallo: `IntroArtTests.FruitFlight…10ms`, el inestable (tope fijo de 10 ms)**; relanzado con filtro, 9/9; **1.589 (1.382 + 207), 0 fallos** |

- **Decisiones [P]** (se avanzó con la opción recomendada; detalle en la nota §7):
  - Q-C1 a Q-C14. Destacan:
    - **Q-C2 (a):** el Reto no lleva el x2 gratis del río. Se aparta de una lectura literal de 04 §2.8; se apoya en 06 §4.3.
    - **Q-C5 (a):** el comodín del hito 6 se calcula con `HitSix(max(level, max_level))`.
    - **Q-C6 (a):** una sola hoja al día.
    - **Q-C13 (a):** las 50 semillas del hito 6 van con `source = streak`, sin `af_basket_open`.
    - **Q-C14 (a):** el x2 del río no pasa por la compuerta del vídeo.
  - **Q-M9 (a):** esquema del perfil; un solo `shield_available`, el de `daily`.
  - **Q-M10:** el módulo `AF.Economy`.
  - **Sonda:** `level` en `streak_milestone` y `streak_shield_used` es solo de la sonda; no está en 08 §8.2.

### Preguntas para el dueño (nuevas; no bloquean lo entregado)

| Id | Pregunta | Recomendada |
|---|---|---|
| B1a-Q1 | Fusión con la nube (08 §11): «`daily` se fusiona con max por clave» no sirve para los campos nuevos. El `max` de `shield_available` devolvería un escudo ya gastado, y `streak_hits` es una lista | `shield_available` = true solo si las dos copias lo tienen; `streak_hits` por unión; `streak_seeds` por max |
| B1a-Q2 | ¿Un cambio que solo toca `streak` cuenta como mutación de economía para `economy_ts`? Hoy no lo actualiza: en un fallo duro sin hoja en 25-30 podría ganar la copia antigua en un conflicto con el mismo `max_level` | Sí: que `streak` actualice `economy_ts` (cambio pequeño en S1 cuando lo decidas) |
| Q-M2 | Diseño del panel único de premios (D-029 #18) | Bloquea B1b y B4 |

## 2026-10-20 · S1 · Auditoría de la 1.0 y plan de la tanda 5 (`spark/auditoria-1-0`): HECHA · lista para integrar (solo documentación)

- **Rama:** `spark/auditoria-1-0`, desde `origin/integracion-fase1-fase2` en `a759597`.
- **Commits:**
  - `ee5bf2e`: el plan;
  - `81c4fd2` y `8c8354e`: la revisión.
- **Archivo:** `docs/dev/plan-tanda5.md`.
- **Qué contiene:**
  - las piezas de la 1.0 en cuatro áreas (núcleo y partida; meta y liveops; economía y monetización; técnica y publicación), con su estado (hecha / a medias / sin empezar) y la evidencia en código y tests;
  - lo que bloquean el dueño o terceros;
  - la tanda 5 en 14 bloques de 1-3 días (B1a, B1b, B2-B14), con sus choques con el escritorio;
  - las preguntas, cada una con su opción recomendada;
  - una sección «Fuera de la tanda 5»;
  - hallazgos para el GDD.
- **Método:** cuatro auditorías de solo lectura sobre el código y los tests reales, y después la síntesis.
- **Verificación:**
  - `python tools/econ_sim.py --runs 300 | diff --strip-trailing-cr -q - tools/out/econ_B.md` → idéntico;
  - `python tools/validate_schema.py` → 103/103;
  - sin builds, por ser solo documentación.
- **code-reviewer:**
  - primera ronda CAMBIOS NECESARIOS:
    - preguntas que reabrían D-020 y D-029 sin decirlo;
    - B1 con preguntas abiertas;
    - piezas sin bloque;
    - citas erróneas;
    - faltaba esta entrada;
  - segunda ronda **APROBADO** (con esta entrada).
- **Resumen de la auditoría:**
  - El núcleo y el meta del Mapa están completos, igual que la lógica de cliente del embudo de D-008, con anuncios y compras simulados.
  - Faltan, con el canon cerrado:
    - Crecida + Hoja escudo, Canasta, Bitácora, Pororoca, Salto del Boto;
    - notificaciones (la lógica) y regalo de regreso;
    - Mercado y ofertas;
    - `AF.RemoteConfig` y `AF.Analytics`;
    - nube y fusión, consentimiento y edad;
    - accesibilidad (TalkBack, escala de fuente, Reducir movimiento, Alto contraste);
    - compuertas de release.
  - Todo lo «online» real está bloqueado por el dueño: SDK, `INTERNET`, cuentas, Addressables y PAD, secretos, keystore y marca.
- **S2 elegido: B1a** (reglas de la Crecida + Hoja escudo + Canasta: perfil, RC, sonda y tests, en archivos nuevos). Cumple los tres criterios del encargo §11:
  - (a) no depende de nada bloqueado;
  - (b) sus preguntas (Q-M9, esquema del perfil; Q-M10, módulo) tienen opción recomendada;
  - (c) no toca los archivos que el escritorio está cambiando ahora. Sus ganchos van a `FailResolve`, `LevelFlow.Victory`, `Profile` y `SlothContext`, como parches probados, porque son archivos de §3.2.
- **Pendiente del dueño:**
  - **antes de cerrar B1a:** Q-M9 y Q-M10. Si no llegan, B1a avanza con las recomendadas, marcadas [P];
  - **antes de B1b y B4:** Q-M2 (diseño del panel único de premios, D-029 #18);
  - **el resto de las preguntas:** en `docs/dev/plan-tanda5.md` §5.

## 2026-10-20 · T2 · Traducción con IA a inglés y portugués (`spark/traduccion-ia`): HECHA · verificación completa VERDE · lista para integrar

- **Rama:** `spark/traduccion-ia`, desde `origin/integracion-fase1-fase2` (`e7475d2`).
  - Fusiona `27a7e9f` en `2dd8866` y `cb0f96f` en `5254a64`.
  - Commits propios: `0a05c6f`, `f73700b`, `d39a6ec`, `f9137ca`, `86dcdc8`, `7dec1d3` y `a9bdd2c`.
- **Regla:** la rama **no edita** `strings.csv` ni `facts.csv`. Lo deja todo para que el integrador lo aplique al cerrar la ola 7.
- **Qué entrega:**
  - `docs/dev/traspaso/spark-traducciones/`: los CSV `key,es-419,en,pt-BR,status_en,status_pt-BR`, con el español con el que se tradujo para detectar cambios, y `pendientes.csv`, con cada celda vacía y su motivo.
  - `tools/loc/apply_translations.py`, solo biblioteca estándar, con `--check` y `--self-test` (46/46).
    - Toca únicamente `en`, `pt-BR` y sus estados.
    - Nunca pisa una celda rellena y nunca escribe en `map.*` sin `stops-preserve.diff`.
    - Retiene las filas de `facts.csv` cuyo español está por debajo de `aprobado_es`.
    - Se niega a aplicar una entrega sin su parche.
    - Aplica lo mismo a `spark-strings-reto.csv`.
  - **Estado** de cada celda traducida: `traducido` (contrato 32, 08 §4.7: vacío → `traducido` → `revisado_nativo`).
  - `Assets/_Project/Tests/PlayMode/TranslatedLocaleOverflowTests.cs`: mide los textos en `en` y `pt-BR`, al 100 % y al 130 %.
- **Parches** (`docs/dev/traspaso/spark-enganches/`, con LF):
  - `glosario.diff`: `glossary.csv` con los tipos nuevos `place` y `fauna`, los EN y PT de comodines, mecánicas, lugares y términos (lo cultural, pendiente del asesor y [P] de T2), y el ajuste de `GlossaryTableTests`;
  - `glosario-gdd.diff`: la sección «12.» de `docs/gdd/00-glosario.md` y la frase de 08 §4.7;
  - `stops-preserve.diff`: `tools/river-map/stops.py` conserva `en`, `pt-BR` y sus estados, con `test_stops_preserve.py`;
  - `LocReleaseGate.diff`: en release, la UI en `en` o `pt-BR` sin `revisado_nativo` bloquea (D-018, Q5-A), con 2 tests;
  - `MapTextTests.diff`.
- **Orden para el integrador:**
  1. Los cinco parches.
  2. Fusiona `spark/reto-cierre` (T1) con E5b.
  3. `python tools/loc/apply_translations.py` (por defecto: `strings.csv` y `strings_p14.csv`).
  4. El script con `strings_mapa.csv`, `strings_glosario.csv`, `strings_mapa_glosario.csv` y `strings_t1.csv` como argumentos.
  5. Las entregas de `facts`. Se pueden pasar ya: no escriben nada hasta que el español llegue a `aprobado_es`.
  6. Si entran las comillas “ ” de los textos largos, regenera los atlas con `FontSetup.ApplyAll`.
- **Recuentos** (celdas `en` / `pt-BR`):

  | Entrega | Cuándo se aplica | en | pt-BR |
  |---|---|---|---|
  | `strings.csv` (incluye R12 y `victory_perfect`) | por defecto | 91 | 95 |
  | `strings_p14.csv` | por defecto (T2-Q2) | 8 | 8 |
  | `strings_mapa.csv` | con `MapTextTests.diff` | 19 | 19 |
  | `strings_glosario.csv` | con `glosario.diff` | 54 | 50 |
  | `strings_mapa_glosario.csv` | con los dos | 2 | 2 |
  | `strings_t1.csv` | cuando estén las claves de T1 | 2 | 2 |
  | `facts.csv`, `facts_glosario.csv`, `facts_mapa.csv` | retenidas (T2-Q6), 370 filas | 201 + 46 + 123 | 201 + 46 + 123 |

  - **Con todo aplicado:**
    - en `strings.csv` quedan 7 claves vacías en `en` y 8 en `pt-BR`: «Amazonas» y «Amazonía» (T2-Q7), Totuma sin glosario, `fact_sheet_title` con su §10 pendiente y el PT de `fauna_sighted_card`, pendiente del asesor;
    - en `facts.csv` quedan 430 por idioma: 128 `map.*.source` (nombres de fuente, pregunta 57 (a)), 185 frases `map.*` y 117 de fruta, río y fauna (lugares, fauna, pueblos o comidas sin glosario, o «Amazonía»).
  - **LocReleaseGate:** 814 problemas sin `LocReleaseGate.diff` y 816 con él (las 2 líneas nuevas, «UI sin `revisado_nativo`» en `en` y en `pt-BR`).
- **Verificación:**

  | Dónde | Comando | Resultado |
  |---|---|---|
  | rama (`a9bdd2c`) | `dotnet build -warnaserror`, `dotnet test`, `validate_schema.py`, `apply_translations.py --self-test` | 0 avisos; 981 (975 + 6); 103/103; 46/46 |
  | rama | `unity-compile`, EditMode, PlayMode (`-TimeoutSec 3600`) | OK; **3.148 (3.145 + 3), 0 fallos**; **1.513 (1.326 + 187), 0 fallos** |
  | worktree temporal con los 5 parches y todas las entregas | EditMode | **3.150 (3.147 + 3), 0 fallos** |
  | ídem (sobre la base anterior `27a7e9f`) | PlayMode, con `TranslatedLocaleOverflowTests` | 1.399 (1.232 + 167), 0 fallos; en y pt-BR al 100 y 130 %, 147 textos sin problemas |
  | copias de `git archive` de la rama y de `cb0f96f` | `git apply` de los 5 parches; las 9 entregas; `--check`; `stops.py --check`; unittest de `river-map` | todo OK; 394 celdas, 370 filas retenidas, `facts.csv` idéntico |

- **code-reviewer:**
  - primera ronda CAMBIOS NECESARIOS: `facts` contra 08 §4.7, y «Amazonía» y regiones sin glosario;
  - segunda ronda CAMBIOS NECESARIOS: `.patch` con CRLF, «Encuentro de las Aguas» y «Kew» sin glosario, y faltaba la parte del GDD;
  - tercera ronda **APROBADO**, a falta de Unity (después, verde).
- **Calidad:** el revisor repasó todas las traducciones en los dos idiomas: tuteo, «você» e imperativo; marcadores, plurales, cifras D-018 y NBSP intactos; ninguna cadena «Amazon»; lo cultural, solo con el glosario.
- **Criterio para la fauna y la fruta genéricas:**
  - sin glosario se traducen las categorías de vocabulario general (mono, ave, pez, ardilla…) y los alimentos de diccionario (piña, mango, café…);
  - solo con el glosario, los nombres de especie o de la región (tapir, tucán, cocona…) y las preparaciones locales (yuca, fariña…).

### Preguntas para el dueño (nuevas; no bloquean lo entregado)

| Id | Pregunta | Qué desbloquea |
|---|---|---|
| T2-Q8 | Entrada del glosario para «Amazonas», «Amazonía» y «Sobre la Amazonía» (con el asesor; el glosario §1 evita la cadena «Amazon» por la marca) | 3 claves de UI, unas 60 frases de `facts.csv` y muchas `map.*` |
| T2-Q9 | Mundo 3, «Manaos»: es invariante y `glossary.csv` no admite dos filas con el mismo es-419. ¿Pasa a «Manaus» en EN y PT? | `fact.agua_negro.10` y `.19`, el dato de *Saguinus bicolor* y `map.stop.manaos.*` |
| T2-Q10 | Textos propuestos para el glosario §10: `fact_sheet_title` «River Fruits» / «Frutas do Rio», las filas de P14 y las de `map_*` | `fact_sheet_title` |
| T2-Q11 | Propuestas [P] para el asesor: PT «Redemoinho» o «Rebojo» (Remolino); EN «Tide» (Marea), «Firefly» / «Vaga-lume» (Luciérnaga); «Noite da Onça»; «boto» como PT de «delfín de río»; «Meeting of Waters» | Validación de `glosario.diff` |
| — | La revisión nativa de `en` y `pt-BR`: con `LocReleaseGate.diff`, un build de release se para hasta que exista | Publicar en `en` y `pt-BR` |

## 2026-10-20 · T2 · Decisiones del dueño sobre las preguntas de la traducción con IA

El dueño respondió, literal: «las preguntas t2 hagamos lo recomendado». Se aplica la opción recomendada de cada una. T2-Q6 y T2-Q7 salieron después, de la revisión de T2, y también se resuelven con la opción recomendada:

| Id | Pregunta | Decisión (la recomendada) |
|---|---|---|
| T2-Q1 | El bloque `map.*` de `facts.csv`: `tools/river-map/stops.py` escribe `en` y `pt-BR` vacíos al regenerar | (a) `stops.py` conserva las columnas `en` y `pt-BR` y sus estados. Va como **parche probado**, porque `tools/river-map/**` es del integrador (encargo §3.2). Se traducen las frases `map.*` que solo usan nombres del glosario |
| T2-Q2 | Las 8 claves de P14 (glosario §10: EN/PT a la espera del dueño) | (a) Se aprueban sus textos y se aplica `strings_p14.csv` |
| T2-Q3 | Lugares, países, ciudades y ríos, comodines (Marea, Morpho, Luciérnaga), mecánicas y fauna sin EN/PT en el glosario | (a) Se añaden al glosario: los exónimos habituales de países, ciudades y ríos, y propuestas para comodines, mecánicas y fauna, con lo cultural en `pendiente_asesor` / propuesta. Va como **parche probado** de `Data/Loc/glossary.csv` y del glosario §10. Las traducciones que dependen de esos nombres, en un archivo aparte que se aplica con ese parche |
| T2-Q4 | Términos que se tradujeron sin entrada en el glosario: Cosecha doble (Double Harvest / Colheita dupla), Aguas estancadas (Still waters / Águas paradas), Muestrario (Samples / Mostruário) y «Sobre la Amazonía» (About the Amazon / Sobre a Amazônia) | (a) Pasan al glosario, en el mismo parche que T2-Q3 |
| T2-Q5 | `LocReleaseGate.Evaluate` no exige `revisado_nativo` en un build de release, aunque 08 §4.7 lo pide | (a) Se añade la regla: en release, una cadena de UI en `en` o `pt-BR` sin `revisado_nativo` es aviso o bloqueo, con el mismo nivel que los demás avisos de la puerta (D-018, Q5-A). Va como **parche probado** |
| T2-Q6 | Frases de `facts.csv` con el español aún en `pendiente_asesor` (08 §4.7: una traducción solo vale con `aprobado_es`; `FactCatalogTests` exige los estados vacíos) | (a) **Retener**: la entrega de `facts.csv` queda guardada y se aplica cuando el asesor apruebe el español. El script no escribe filas por debajo de `aprobado_es` y detecta un español cambiado |
| T2-Q7 | «Amazonía» / «the Amazon» / «a Amazônia» y regiones (trópico americano, América tropical, alta Amazonia…) sin entrada en el glosario; el glosario §1 evita la cadena «Amazon» por la marca de Amazon.com | (a) Se dejan **vacías** y anotadas en `pendientes.csv`, hasta que el dueño decida con el asesor la entrada del glosario |

## 2026-10-20 · T1 · Cierre del Reto (`spark/reto-cierre`): HECHA · verificación completa VERDE · lista para integrar

- **Rama:** `spark/reto-cierre`, desde `origin/integracion-fase1-fase2` (`e7475d2`). Fusiona `27a7e9f` (R12 integrado) en `fea96b7`.
- **Commits:**
  - `c7ec205`: E16;
  - `e5061f2`: E17 («Enganche:»);
  - `bac87a7`: E6, E7 y E21 → E25;
  - `3094557`: los parches;
  - `906e78a`: tablas de prueba, `DailyPoolDisabledOverride` y orden de la sonda;
  - `e68a9dd`: `DailyText.E5bKeys`;
  - `053eecc`: E5b, E6 y E7 regenerados sobre la base nueva;
  - `b1b4628`: ajuste de `FunnelPlayModeTests` en `E7.diff`.
- **Qué hace:**
  - **E16 (aplicado):** level-qa enlaza `../level-generator/Daily/*.cs` (salvo `DailyCommand*.cs`), reproduce el pool en memoria con `DailyRunner` y `DailyPackWriter`, y borra `Daily/Shared/`.
  - **E17 (aplicado):** el README de level-qa documenta `--daily`, y su `--self-test` sin nombre ejecuta key2 y daily.
  - **E6, E7 y E5b:** lo del Spark va en la rama; lo de archivos ajenos, como parches probados (fichas en «Enganches para el integrador»).
  - **E21 → E25:** corregida la referencia en `DailyChallengeScreen.cs`.
- **Verificación:**

  | Dónde | Comando | Resultado |
  |---|---|---|
  | rama | `dotnet build -warnaserror`, `dotnet test`, `validate_schema.py` | 0 avisos; 981 (975 + 6); 103/103 |
  | rama | `level-qa --self-test` (sin nombre y `daily`); `--daily` del pool publicado | todo PASS; 2.187 tableros, **0 fallos**, 1.678 avisos |
  | rama (`e68a9dd`) | `unity-compile`, EditMode, PlayMode | OK; **3.083** (3.079 + 3; 1 fallo, el inestable `IntroArtTests`); **1.404 (1.237 + 167), 0 fallos** |
  | worktree temporal con E5b + E6 + E7, sin `b1b4628` | EditMode, PlayMode | 3.083 (3.080 + 3), 0 fallos; 1.411, **2 fallos** en `FunnelPlayModeTests` del 31, los que arregla `b1b4628` |
  | ídem, con `b1b4628` | `unity-compile` + PlayMode | OK; **1.411 (1.244 + 167), 0 fallos** (25 min), tras la revisión de la licencia por el dueño |

- **code-reviewer:**
  - primera ronda CAMBIOS NECESARIOS: base vieja (tablas duplicadas con E5 y «sin pool» con E3);
  - segunda ronda: código, tests y parches bien; un solo bloqueante, de documentación (esta entrada);
  - tercera ronda: **APROBADO** (a falta de Unity, que después salió en verde).
- **[P] para el dueño:** los de E6 y E7, en sus fichas (recomendación: aceptarlos).
- **Para el integrador:**
  - los parches aplican con `git apply --check` sobre la rama y sobre `origin/integracion-fase1-fase2` en `cb0f96f`;
  - `tools/level-generator/README.md:777` aún dice que QA-DAILY-REPRO regenera «en un proceso hijo»; con E16 es en memoria (archivo del integrador).

## 2026-10-19 · Entrega final: R0-R12 en `spark/reto-del-rio`, verificación completa VERDE

Verificación en `spark-int`, sobre la rama con la parte del Spark de R12 (`ac54d82`) y los parches guardados (`bb5b163`, `fa5209e`):

| Comando | Resultado |
|---|---|
| `dotnet build dotnet/AmazonFill.slnx -warnaserror` | código 0 |
| `dotnet test dotnet/AmazonFill.slnx` | 981 (975 + 6) |
| `python tools/validate_schema.py` | 103/103 |
| `unity-compile.ps1 -Architecture arm64` | OK |
| `unity-test.ps1 -Mode EditMode … -TimeoutSec 3600` | **3.058 (3.055 + 3 omitidos), 0 fallos** |
| `unity-test.ps1 -Mode PlayMode … -TimeoutSec 3600` | **1.383 (1.216 + 167 omitidos), 0 fallos** (39 min) |
| Capturas de R11 (`DailyChallengeSnapshotTests`, sin `-nographics`) | 4/4; `AF_FIT` = 0 textos fuera de su caja en es-419, es-419 16:9, en-XA y en-XA 16:9; `AF_STAMP` = desvío 0,00 dp |

- **Recuentos de referencia del Spark para el integrador:**
  - sin los enganches: EditMode **3.058**, PlayMode **1.383**;
  - con los enganches aplicados (R12): EditMode 3.082 y PlayMode 1.394.
- **Bloques listos para integrar:**
  - (a) ya integrado;
  - (b) el pool;
  - (c) lógica;
  - (d) partida y pantallas;
  - (e) los enganches, con sus parches en `docs/dev/traspaso/spark-enganches/` y en `docs/dev/traspaso/spark-parches-docs.md`.
- **Fuera de alcance o pendiente:**
  - E1, E6 y E7 sin preparar (motivo en la entrada de R12);
  - E16 por regenerar;
  - E19, E20 y E25, para más adelante.
  - Las capturas son de prueba; falta la prueba en el teléfono, que necesita los enganches aplicados en la integración.

## 2026-10-19 · R12 · Enganches para el integrador: parches exactos y PROBADOS · Bloque (e) listo

**Probados contra:** `origin/integracion-fase1-fase2` en **`bc65242`**, con `spark/reto-del-rio` fusionada (R0-R11 y la parte del Spark de R12). Worktree temporal `r12-full`, no empujado, HEAD `c4ba4a1`.

### Dónde están y en qué orden se aplican

Carpeta **`docs/dev/traspaso/spark-enganches/`**: diffs unificados con LF fijado (`.gitattributes` propio).

1. Fusiona `spark/reto-del-rio`. Ya trae la parte del Spark de R12 (`ac54d82`): la tarjeta con el título en dos líneas y 100 dp, los `max_chars` del CSV, `DailyUiLogicTests` con la regla `VisibleLength`, y `DailyProbeFormat.EarnSeeds` con `source` (para E9) y `DailyChallengeService.Config`.
2. Aplica **`R12-all.diff`**: todos los enganches juntos, 26 archivos. `git apply --check` da OK sobre `ac54d82`.

   O bien, uno a uno y en este orden:
   - `E4.diff`;
   - `E2.diff`, `E22.diff` y `E23.diff` (los tres tocan `GameBootstrap.cs`);
   - `E3.diff` y `E3-scene.diff`;
   - `E5.diff` y `E5-fix.diff`;
   - `E9.diff`;
   - `E21.diff` y `E24.diff`;
   - `E-tests.diff`.
3. **Mejor que aplicar `E3-scene.diff`:** regenera `Game.unity` con `AF.UI.Editor.GameSceneBuilder.CreateGameScene` tras aplicar `E3.diff`. El diff de la escena solo añade `dailyPool` (el guid del `.meta` del pool) y renumera los fileID.
4. Después, que Unity genere los `.meta` de los archivos nuevos (`ProfileDailyChallengeStoreTests.cs`, `DailyDoubleHarvestTests.cs`, `DailyHooksTests.cs` y `ProfileDailyChallengeStore.cs`) y commitéalos tal cual.

### Qué hace cada uno

| # | Archivos | Qué |
|---|---|---|
| E2 | `GameBootstrap.cs` | `SetUpDaily();` tras `_lobby.Bind(_app, _services);` |
| E3 | `Editor/GameSceneBuilder.cs` (+ `Game.unity` regenerada) | `DailyPoolPath` y `ConfigureDailyPool(dailyPool)` tras `ConfigureRiverStops`; aviso si falta el pool |
| E4 | `Save/Profile.cs`, `Save/ProfileJson.cs`, nuevo `LiveOps/ProfileDailyChallengeStore.cs`, `GameBootstrap.Daily.cs` (fábrica por defecto) | Bloque `daily_challenge` (nota §10). Se omite mientras es null, así que el golden del perfil no cambia. `CurrentSchema` 1, sin migración (08 §4.3). Un tipo equivocado o un `in_progress` sin `date` o `id` es corrupción. El almacén usa el mismo `Profile` y el mismo lote (`ProfileSaveBatch.Durable`) que la cartera del Reto: nunca dos pagos. 17 tests |
| E5 | `Data/Loc/strings.csv`, `LocTablesTests` (+ tests del Reto) | Las 24 filas de `spark-strings-reto.csv` en orden ordinal (`data-json.md`); `LocTablesTests` añade `DailyText.NewKeys`; los tests del Reto leen ya `strings.csv` |
| E9 | `Economy/Transaction.cs`, `Wallet.cs`, `DailyChallengeScreen.cs`, `DailyStampCalendar.cs`, tests | `Transaction.DoubleHarvestDaily(seeds, level)` [P en la firma: lleva el nivel para el mínimo `ads_rv_double_harvest_from_level` = 11]. Solo con el premio confirmado, una vez por victoria, con el tope diario y el cooldown de `rv_double_harvest` de AF.Ads (compartidos con la campaña [P]) y como mucho seeds × `murky_bonus_mult` (120). Paga solo si el anuncio se vio entero. Sonda `earn_virtual_currency` `source=double_harvest` |
| E21 | `GameScreen.cs` | `if (isShort && !flow.IsDaily)`: el Reto no enseña «Nivel N · intento k» |
| E22 | `GameBootstrap.cs` (opcional) | Atrás cierra el calendario del Reto; con el tablero en juego no hace nada |
| E23 | `GameBootstrap.cs` | Al volver de segundo plano, la vista del Reto salta al modelo, limpia la selección y reinicia la pista por inactividad |
| E24 | `DevProbe.cs` | `AppendDailyBoard(sb, flow);` tras las 4 líneas `"level"`: el Reto añade `"daily":"d{b}_n{nnnn}"` |
| tests | nuevo `Tests/PlayMode/DailyHooksTests.cs` (+ ajustes en `DailyChallengeTests`) | E2, E3, E4, E5, E9, E21-E24 de punta a punta con la escena real: el pool real, el almacén del perfil y las tablas |

### Verificación (r12-full, con todo aplicado)

| Comando | Resultado |
|---|---|
| `dotnet build -warnaserror` | código 0 |
| `dotnet test` | 981 (975 + 6) |
| `validate_schema.py` | 103/103 |
| `unity-compile.ps1 -Architecture arm64` | OK |
| EditMode | **3.082** (3.078 + 3; 1 fallo, el inestable `IntroArtTests.FruitFlight…`) |
| PlayMode | **1.394 (1.227 + 167), 0 fallos**; `PseudoLocOverflowTests` en-XA 130 % pasa tras el arreglo de la tarjeta |
| Capturas de R11 | 31 PNG, `AF_FIT` sin ningún texto fuera de su caja (es-419 a 780 no salió por un error del entorno con la ruta larga; se repite en `spark-int`) |

- **code-reviewer:** primera ronda CAMBIOS NECESARIOS (faltaban la sonda `double_harvest` y el test del anuncio cerrado antes de tiempo, y PlayMode tenía un fallo real). Segunda ronda **APROBADO**.

### Lo que no se preparó

- **E1** (`LobbyScreen`, opcional): no hace falta, porque la tarjeta va en su propio lienzo desde `GameBootstrap.Daily.cs`. Requisito si se mueve: en 16:9, al menos 8 dp bajo el bocadillo de TiTino (`r11_01_card_play_16x9`).
- **E6** (tarjeta de 2 s tras ganar el 7): falta una clave de texto nueva y el diseño de la tarjeta. Punto de enganche: `GameScreen.OnVictoryScreen` con `flow.LevelNumber == 7`.
- **E7** (Sin canoas, opcional): necesita una clave nueva, `af_no_canoes_choice = daily_challenge` y un flujo nuevo en AppFlow.
- **E16** (level-qa enlaza `Daily/`): el parche probado en el bloque (a) quedó desfasado tras R5. Hay que regenerarlo sobre la versión actual (descripción en su ficha).

### Para el dueño (E11; no bloquean)

- El Reto comparte con la campaña el tope de 5 al día y el cooldown de la Cosecha doble [P].
- Cuando exista la Crecida: ¿la Cosecha doble sin video del «río crecido» (04 §2.8) vale también en el Reto? Va con E19 y E25.

## 2026-10-19 · R0-R11 juntas: verificación completa VERDE · Bloque (d) (R8-R11) listo para integrar

- **Qué hay:** la rama principal (R0-R10) más R11, la nota con la tabla de días de 729 corregida (`09b11cd`) y los usos `--days 0-728`.
- **Resultados** (worktree `spark-int`):
  - `dotnet build -warnaserror` OK y `dotnet test` 981;
  - `validate_schema.py` 103/103;
  - `unity-compile` OK;
  - EditMode **3.057** (3.053 + 3, 1 fallo: el de tiempo de `IntroArtTests.FruitFlight…`, inestable y ajeno).
- **PlayMode: sin veredicto (código 6, licencia).** Hacia las 19:15 vuelve el 404 de derechos (`com.unity.editor.headless`). Es la 4.ª caída, de nuevo unas 2 h después de la última revisión del dueño.
- **Por separado sí hay PlayMode en verde:** R10 sobre R9 (1.354) y R11 sobre la integración con R5 (1.373). Falta la combinación de las dos.
- **PlayMode, tras la revisión de la licencia por el dueño:** **1.383 (1.216 + 167 omitidos)**, 0 fallos (43 min). Los omitidos incluyen los `[Explicit]` de capturas.
- `spark/reto-del-rio` avanza hasta esta combinación y se empuja. También entran los parches de documentación E11/E12 aprobados (`docs/dev/traspaso/spark-parches-docs.md`, `571fdc2`) y los textos 0-728 (`7501c5e`), que solo tocan documentos y textos de ayuda.
- **Recuentos de referencia del Spark:** EditMode 3.057; PlayMode 1.383.

## 2026-10-19 · R11 · Capturas y prueba de punta a punta: HECHA (APROBADO en 2.ª ronda; arreglo del sello en `171b404`)

- **Commit:** `41d5beb` en `spark/reto-del-rio-r11` (sale de la integración `8327d23`), más el arreglo del sello en curso.
- **Archivos:**
  - nuevo `Assets/_Project/Tests/PlayMode/DailyChallengeSnapshotTests.cs`: `[Explicit]`, `SnapshotKit`, 360 × 780 dp y 16:9, es-419 y en-XA;
  - arreglos en `DailyChallengeCard.cs` (título de 192 dp y estado en dos líneas) y `DailyStampCalendar.cs` (el sello que cae, por encima de las casillas vecinas; la caja del +60, de 42 dp).
- **Cómo se lanzan:** sin `-nographics` (`unity-test.ps1` lo fuerza):
  ```
  unity test . --mode PlayMode --architecture arm64 --filter AF.Tests.PlayMode.DailyChallengeSnapshotTests --output Logs/r11-snap.xml --non-interactive -- -logFile Logs/r11-snap.log
  ```
  - Los PNG van a `Logs/snapshots/es-419/` y `Logs/snapshots/en-XA/` (ignorado por git).
  - Cada captura deja en el log una línea `AF_FIT` por texto visible. Hoy dicen «0 textos fuera de su caja» en es-419, es-419 16:9, en-XA y en-XA 16:9.
  - Los textos salen de `strings.csv` + `facts.csv` + `docs/dev/traspaso/spark-strings-reto.csv`, como hará E5.
- **Verificación:** capturas 4/4; EditMode 3.057 (3.053 + 3, 1 fallo del test inestable `IntroArtTests`); PlayMode **1.373** (1.206 + 167 omitidos: los 4 `[Explicit]` nuevos), 0 fallos.

### Capturas y qué revisar (40 PNG: 10 × 2 formatos × 2 idiomas; los de 16:9 llevan `_16x9`)

| Archivo | Qué muestra | Qué revisar |
|---|---|---|
| `r11_01_card_play` | Inicio con la tarjeta «Reto del río · 19 oct», Jugar, 1 ola, 0/7 | La fecha corta por región; que el título no toque el sello. **En 16:9 la tarjeta queda pegada al bocadillo de TiTino (0-3 px): requisito de E1** |
| `r11_02_header` | Partida del Reto | Cabecera «Reto del río», sin «Nivel N» ni barra de Crecida. Ajeno: en en-XA, «Reiniciar» se sale del botón (`GameScreen`) |
| `r11_03_card_continue` | Tarjeta «Seguir» tras salir sin ganar | R-Q16 (a) |
| `r11_04_calendar_falling` | Octubre con sellos previos y el del 19 a media caída | Que el sello caiga **centrado en su casilla** (arreglado en `171b404`; `AF_STAMP`: desvío 0,00 dp); el contador del HUD ya marca 60 antes del «+60» |
| `r11_05_calendar_reward` | El sello posado y «+60» con la semilla | El «+60» queda algo a la izquierda del centro [P] |
| `r11_06_card_sealed` | Tarjeta «Sellado», 1/7 | El sello verde de los CTA con el aro crema (sin rojo; guía v2 §7) |
| `r11_07_calendar_month` | Calendario abierto desde la tarjeta sellada | Sin premio ni sello que cae; el hueco del «+60» queda reservado |
| `r11_08_calendar_pending` | Victoria con el servidor +48 h | «Premio pendiente de validar», sin +60, sello translúcido |
| `r11_09_card_pending` | Tarjeta «Pendiente de validar» | En en-XA, en dos líneas sin pisar la racha; ¿se lee bien el sello pendiente (disco claro)? |
| `r11_10_calendar_pororoca_locked` | Racha de 7 (del 12 al 19), +60 y «+1 Pororoca · se desbloquea en el nivel 30» | R-Q14 (a); en 16:9 el panel tapa la parte de arriba de la bandeja de comodines con el velo |

- **Además:** no hay cabecera de días de la semana en el calendario ([P] de R9); en el inicio de estas capturas no sale la pastilla de Canoas, por el perfil de prueba (`Wallet.NewProfile`), no por el Reto.
- **E1 (requisito nuevo):** en 16:9, dejar al menos 8 dp bajo el bocadillo de TiTino, o subir o compactar la tarjeta. Se comprueba con `r11_01_card_play_16x9`.

## 2026-10-19 · R10 · Sondas de desarrollo y analítica: HECHA

- **Commits:** `a99fe5d`, `b1c0a9b` (fusión de R9), `83e43dc` (los `.meta` que generó Unity) y `4025c1a` (arreglos de la revisión), en `spark/reto-del-rio-r10`; fusionados con `--no-ff` en `spark/reto-del-rio` (`8ab5346`).
- **Archivos:**
  - nuevo `Scripts/UI/DevProbe.Daily.cs`, un parcial de `DevProbe` que escribe por `DevProbe.Write` solo con `DevProbe.Enabled`;
  - cambios en `GameBootstrap.Daily.cs`, `DailyChallengeScreen.cs` y `DailyChallengeCard.cs` (archivos del Spark);
  - nuevo `Tests/PlayMode/DailyProbeTests.cs` (10 tests).
- **Sondas:**
  - `daily_challenge`: `win` al reclamar (nunca con `NoReward`); `fail` desde `DayEnded`, una vez por día;
  - `level_start`: al terminar Llenar y teñir, o al reanudar sin intro; una vez por partida ([P]: Volver a la orilla no lo repite);
  - `earn_virtual_currency`: al cobrar, y al abrir la app si se confirman cobros pendientes;
  - `daily_card`: en cada recálculo de la tarjeta.
  - `t_ms` es lo jugado desde que se abrió, y null si se reanudó [P]. La Pororoca no tiene evento (08 §8.2 no lo define).
- **Verificación** (worktree `spark-r10`): compila; EditMode 3.040 (3.036 + 3, 1 fallo del test inestable); **PlayMode 1.354 (1.195 + 159), 0 fallos**; el revisor, `DailyProbeTests` 10/10.
- **code-reviewer:** primera ronda CAMBIOS NECESARIOS (`level_start` salía antes de la intro, contra 08 §8.2, y faltaban tests); segunda ronda **APROBADO**.
- **E10:** no hace falta (todo sale de archivos del Spark). Queda E24 (`DevProbe.cs`, `AppendDailyBoard`).
- **Texto propuesto para `docs/dev/prototipo-v0.md` (E12):**
  > **Sondas del Reto del río** (R10; `DevProbe.Daily.cs`, formato en `AF.LiveOps/DailyProbeFormat.cs`). Solo en builds de desarrollo, como `AF_STATE` de una línea, con claves e identificadores:
  > - `{"event":"daily_card","t":…,"state":"hidden|in_progress|pending_validation|sealed|play","date":"AAAA-MM-DD","band":0-3,"day":…,"board":"d{b}_n{nnnn}"|null,"streak":n,"override":id|null,"override_ignored":bool}`: cada vez que se recalcula la tarjeta del inicio. Con la tarjeta oculta, `band` 0 y `board` null.
  > - `{"event":"level_start","t":…,"level_name":"d{b}_n{nnnn}","variant":"base","band":b}`: al terminar Llenar y teñir, o al reanudar sin intro; una vez por partida. Las líneas de partida (`load`, `ready`, `pour`…) llevan en `level` el nivel efectivo del Reto; con E24, además, `"daily":"d{b}_n{nnnn}"`.
  > - `{"event":"daily_challenge","t":…,"date":…,"result":"win|fail","t_ms":n|null,"murky_bonus":false,"streak_days":n}`: `win` al reclamar el premio; `fail` una vez por día, al descartar la partida de un día no ganado (R-Q15).
  > - `{"event":"earn_virtual_currency","t":…,"virtual_currency_name":"seeds","source":"daily_challenge","value":n}`: las semillas del premio, pagadas ahora.
  >
  > Para seguirlo: `adb logcat -s Unity | grep -E '"event":"(daily_card|level_start|daily_challenge|earn_virtual_currency)"'`.

## 2026-10-19 · R5, decisiones (R-Q12b, R-Q15, R-Q16), formato de sondas y R9 fusionados: verificación completa VERDE

- **Qué entra** en `spark/reto-del-rio`, por avance rápido desde `spark/reto-del-rio-int`:
  - **R5** (`71593ae`, `fbbb935`, `c266a46`, `c903537`, `d1fbe7e`, `0f436ef`): el pool publicado, 729 días × 3 bandas = 2.187 tableros, 1.430.135 bytes. code-reviewer: **APROBADO** (dos rondas).
  - **Rama de decisiones** (`42555cf`, `8115b74`, `f816d1c`, `e46348c`, `d0a9019`): R-Q16, R-Q15, R-Q12b y `DailyProbeFormat`. code-reviewer: **APROBADO** (dos rondas).
  - **R9** (`373c6e2`, `b0ada3a`, `0e7c276`, `fbb9893`, `896d894`, `b9a9e8c`, `d107d7e`): las pantallas. Los dos commits «Enganche:» son los asmdef de §3.3. code-reviewer: **APROBADO** (dos rondas).
  - La nota, al día (`85fe0a3`), y los enganches E21-E25 (`8327d23`).
- **Verificación** (worktree `spark-int`, `8327d23`):

  | Comando | Resultado |
  |---|---|
  | `dotnet build dotnet/AmazonFill.slnx -warnaserror` | código 0 |
  | `dotnet test dotnet/AmazonFill.slnx` | 981 (975 + 6) |
  | `python tools/validate_schema.py` | 103/103 |
  | `level-generator --self-test` (todas) | todo PASS |
  | `level-qa --daily Assets/_Project/Data/Daily/daily_pool_v1.json` | 2.187 tableros, **0 fallos**, 1.678 avisos (R-Q10) |
  | `unity-compile.ps1 -Architecture arm64` | OK |
  | `unity-test.ps1 -Mode EditMode … -TimeoutSec 3600` | **3.057 (3.054 + 3 omitidos)**, 0 fallos |
  | `unity-test.ps1 -Mode PlayMode … -TimeoutSec 3600` | **1.369 (1.206 + 163 omitidos)**, 0 fallos (41 min) |

- **Recuentos de referencia del Spark desde aquí:** EditMode 3.057; PlayMode 1.369. Los del integrador tras el bloque (a) eran 2.861 y 1.359.
- **Bloque (d)** (R8-R11, partida y pantallas): R8 y R9 están dentro; faltan R10 (sondas, en arreglos de revisión) y R11 (capturas, en curso).

## 2026-10-19 · ⚠ La licencia de Unity falla por 3.ª vez (pendiente del dueño)

- **Hacia las 16:45** los batchmode nuevos vuelven a salir con el código 6: `Found 0 entitlement groups`, `'com.unity.editor.headless' was not found`, `No valid Unity Editor license found`. Los que ya estaban en marcha (el PlayMode de `spark/reto-del-rio` y el de `spark-r9`) siguen corriendo.
- **Patrón:** falla entre 1 y 2 h después de cada revisión del dueño (12:10, 13:35 y 16:45). Esta vez no se reinició el proceso `--internal-auth-broker-serve` (el último es de las 14:38), así que la hipótesis de la 2.ª caída no se confirma. Lo más probable es que caduque el token de la sesión y no se renueve para el modo headless. Habría que mirar en Unity Hub si la licencia es una suscripción por sesión o una licencia Personal sin activación offline.
- **Qué queda frenado:** la verificación en Unity de la combinación de R5 con la rama de decisiones (worktree `spark-int`, `85fe0a3`).
  - La parte .NET y de herramientas ya está en verde: build sin avisos, `dotnet test` 981, `validate_schema` 103/103, `--self-test` del generador todo PASS, y `level-qa --daily` del pool de 2.187 tableros con 0 fallos y 1.678 avisos (R-Q10).
  - Las tres piezas pasaron Unity por separado: R5 (EditMode 2.995, 1 fallo del test inestable que pasa al relanzarlo), la rama de decisiones (3.023, ídem) y la rama principal (EditMode 3.006 en verde).
- **No he usado `unity auth` ni `unity license`** (regla dura 5).

## 2026-10-19 · Decisión del dueño: R-Q11b, 30 candidatos por fuente

- **La pregunta:** para que los 729 días de cada banda entren en la ventana del bot, R5 prueba hasta 30 candidatos **por fuente** y, si ninguno entra, pasa a la siguiente.
  - B1: `fill` E=2 → `deep` → `walk` → `deep` de 60.000 nodos, hasta 120 candidatos por día. Solo 23 días necesitan respaldo.
  - B2: `deep` → `fill` E=1, al revés que la tabla de 03 §4.4 para K ≤ 8. Con `fill` primero, 96 días salían por encima de la ventana.
- **El dueño respondió «Aceptar (Recomendado)»:** el «máximo 30 candidatos» de 03 §4.4 se lee como 30 por fuente, con respaldos en orden. No se relajan vacíos, K, capacidad ni ventanas.
- **Para el integrador (E11):** anotarlo en 03 §4.4 o §16 y en `decisiones.md`.
- **Nota sobre R-Q2b:** con `live_daily_offset` ≠ 0 (mod 9) el mundo sigue rotando a diario con la fecha, desplazado por el offset. Propuesta [P]: documentarlo en 08 §9, en la fila de `live_daily_offset` (E11).

## 2026-10-19 · Decisión del dueño: R-Q2b, pool de 729 días

- **El problema:** el agua y el fondo rotan «según la fecha» (R-Q2), pero con un pool de 730 días y 9 mundos (730 mod 9 = 1), un mundo grabado en cada tablero deja de seguir a la fecha a partir de la segunda vuelta.
- **Opciones presentadas:** el cliente calcula el mundo con la fecha local (cambia el significado de `water_base`, contrato 21); aceptar el salto; o un pool múltiplo de 9.
- **El dueño eligió** «Pool de 729 o 738 días» y, después, **«729 días (Recomendado)»**:
  - 81 × 9 = **729 días por banda, 2.187 tableros**;
  - el día del pool va de 0 a 728 y cabe en el esquema actual, sin tocar el contrato 21;
  - el índice pasa a `(días_desde(2026-01-01) + live_daily_offset) mod 729`;
  - el mundo del tablero es `1 + (día mod 9)`, que sigue a la fecha en todas las vueltas (con `live_daily_offset` constante, desplazado por ese offset).
- **Para el integrador (E11):** cambia 03 §16 y 06 §4.1, que dicen 730 y 2.190, y el pool de 08 §4.1 y §6.2.

## Resumen (2026-10-19)

- **Rama:** `spark/reto-del-rio`, en `752bb55`. Ya lleva fusionado `origin/integracion-fase1-fase2` (`6377345`).
- **Tareas hechas** (commits de la tarea · fusión en `spark/reto-del-rio`):

  | Tarea | Commits | Fusión | Revisión |
  |---|---|---|---|
  | R0 · Arranque y línea base | — (base `2db3a07`) | — | — |
  | R1 · Nota de diseño | `3e0063e`, `4968d1a` | (en la rama) | APROBADO (2.ª ronda) |
  | R2 · Especificación y autoprueba | `da6a9bb`, `99d35f7` | `3ca28dc` | APROBADO |
  | R3 · `--daily` del generador | `e8783b9`, `bb1dad2`, `ee48f52` | `c87a181` | APROBADO |
  | R4 · `level-qa --daily` | `32b8cc4`, `cafe8c4`, `ed109a3`, `8afdd0e` | `ba57160` | APROBADO (2.ª ronda) |
  | R6 · Lector del pool | `a724b43`, `28be4a2` | `afb58a2` | APROBADO (tras Unity) |
  | R7a · Calendario | `065d0e6` | `24d3f18` | APROBADO |
  | R7b · Premio, anti-reloj y racha | `a83ec1c`, `880f7dc` | `f0fb4c8` | APROBADO |
  | R8 · Partida del Reto en `LevelFlow` | `49501b4`, `535b92c` | `c0149b6` | APROBADO (Unity en verde) |
  | Enganche E15 + parte de E17 | `752bb55` | (en la rama) | — |

- **Bloques:**
  - **(a)** R0-R4 y R6: **integrado** en `integracion-fase1-fase2` (`fcbbd94`, `0d844cf`, `bc65242`) y traído de vuelta (`6377345`).
  - **(c)** R6-R7b (lector y lógica, sin pantalla): **listo para integrar**, verificado en `0a1b3ac`.
  - **R8:** fusionada en `spark/reto-del-rio` y verde en Unity (`535b92c`); sin integrar todavía.
- **Preguntas para el dueño:** **ninguna abierta.** R-Q1-R-Q10, R-Q1b y R-Q11-R-Q16 están decididas (entrada «Decisiones del dueño»). El integrador las lleva a `docs/dev/decisiones.md` (E11).
- **Qué falta:**
  - **R5** (en curso): R-Q1b en `daily_v1.json` (B2 `k: [8, 8]` sin `deep`, B3 sin tope), R-Q11 como error en el generador, R-Q2 con la fecha y no con el día del pool; rehacer la muestra y su informe; generar y publicar el pool (encargo §9, puntos 1-3).
  - **Aplicar en el código** las decisiones nuevas: R-Q12b en `DailyChallengeRules.Reconcile`; R-Q16 en `LevelFlow.ExitDaily` (hoy borra la partida); R-Q15 en la sonda (R10).
  - El pendiente menor de R7b (entrada de R7b).
  - R9 (pantallas), R10 (sondas), R11 (capturas y punta a punta) y R12 (enganche final: parches de E1-E7, E9, E10 y E12).
  - Enganches pendientes del integrador: E11, E13 (opcional), E14 (opcional), E16, E17 (README), E18 (punto 2), E19 (cuando exista la Crecida), E20.
- **Riesgo del entorno:** la licencia de Unity ha fallado dos veces (12:10 y 13:35, las dos con un `--internal-auth-broker-serve` nuevo de la CLI). Las dos se resolvieron; si vuelve, se avisa al dueño y no se fusiona nada sin pasar Unity.

---

## 2026-10-19 · Enganche E15 y parte de E17: HECHO (`752bb55`)

- **Commit:** `752bb55` «Enganche: incluye daily en el --self-test sin nombre del generador y documenta --daily en las dos herramientas», en `spark/reto-del-rio`. Punto 6 de las notas del integrador (encargo §9).
- **Archivos:** `tools/level-generator/Program.cs`, `tools/level-generator/README.md` y `tools/level-qa/Program.cs`.
  - E15 completo: el `--self-test` sin nombre del generador ejecuta también `daily`; `|daily` en `Usage`, «, holgura o daily» en el mensaje de error; `--daily` en el README y en la ayuda.
  - E17, la parte de `tools/level-qa/Program.cs`: `--daily` y `--self-test [key2|reglas|variante|daily]` en `Usage`, y el mensaje. **Queda el README de level-qa**, ligado a E16 (su texto describe QA-DAILY-REPRO con el proceso hijo de hoy).
  - Parches probados contra `origin/integracion-fase1-fase2` (`bc65242`).
- **Verificación:**
  - `dotnet run --project tools/level-generator -c Release -- --self-test` → **435 PASS, 0 FAIL**, exit 0 (incluye «daily»);
  - builds de `tools/level-generator` y `tools/level-qa` → sin avisos;
  - `dotnet run --project tools/level-qa -- --self-test bogus` → el mensaje de uso nombra «daily».

## 2026-10-19 · El integrador fusionó el bloque (a); fusión de vuelta en `spark/reto-del-rio` (`6377345`)

- **Integración** (encargo §9, «Bloque (a) integrado»): R0-R4 y R6 están en `integracion-fase1-fase2`:
  - `fcbbd94`: integra el bloque (a) (nota de diseño, especificación y generador del pool, QA del pool y lector en AF.Levels);
  - `0d844cf`: pone al día los recuentos;
  - `bc65242`: anota en el encargo la integración y lo que queda por ajustar.
- Revisado sin bloqueantes y verificado en el escritorio x64; la muestra de 30 tableros regenerada allí sale idéntica byte a byte.
- **Recuentos de referencia del integrador:** EditMode **2.861 (2.858 + 3)**, PlayMode **1.359 (1.196 + 163)**, `dotnet test` **981 (975 + 6)**.
- **Fusión:** `6377345` «Fusiona origin/integracion-fase1-fase2 en spark/reto-del-rio (bloque (a) integrado)», como pide el encargo §9.
- **Lo que pidió el integrador** (encargo §9, «Pendiente para ti»): puntos 1-3 → R5 (resumen); punto 4 (quitar «abierta» y [P] de lo decidido) y punto 5 (ordenar este archivo) → esta revisión de la nota y del estado; punto 6 → `752bb55`. También corrigió el punto 1 de §3.3 (E18).

## 2026-10-19 · Decisiones del dueño sobre las preguntas nuevas (R-Q1b, R-Q11-R-Q16)

El dueño respondió, literal: «aplica las recomendaciones y pon trabajo en paralelo para avanzar mas rapido». El integrador, en el encargo §5: «si tienen recomendada, vamos con esas». Se aplica la opción recomendada (la primera) de cada pregunta. El integrador debe llevarlas a `docs/dev/decisiones.md` junto con R-Q1-R-Q10 (E11). El integrador confirmó además que `IServerDate` y `FakeServerDate` van en **AF.Platform** (encargo §5).

| Id | Decisión |
|---|---|
| R-Q1b | (a) **B2 solo con K = 8** (cambia 03 §16 y 06 §4.1, que dan K 8-9) y **B3 con `max_pares_riesgo` libre**, como la campaña desde el 201 (03 §4.5). B1 sin cambios |
| R-Q11 | (a) La ventana del bot **es compuerta**: un día fuera de ventana no se publica hasta ajustar la especificación (fuente o K, que son [P]); QA-DAILY-BOT sigue siendo fallo |
| R-Q12 | (a) Al reconectar no se retira nada de lo cobrado sin red (`KeepOfflineClaims`, el default actual) |
| R-Q12 (aclaración) | (a) Al anular una fecha no se retiran semillas ni la Pororoca |
| R-Q12b | (a) Un pendiente solo se valida si `\|fecha − hoy_servidor\| ≤ 1 día`; los demás se anulan |
| R-Q13 | (a) Sin intersticial al terminar un Reto |
| R-Q14 | (a) La Pororoca de la racha entra en el stock y espera al nivel 30, en que se desbloquea |
| R-Q15 | (a) `af_daily_challenge.result = fail` **una vez por día**, y solo si ese día termina sin victoria |
| R-Q16 | (a) Salir de la partida del Reto sin ganar la deja **reanudable el mismo día**; al cambiar de día se descarta (R-Q4) |

**Estado en el código:** R-Q12, su aclaración, R-Q13 y R-Q14 (el pago) ya los cumple. Pendientes: R-Q1b y R-Q11 en el generador (R5); R-Q12b en `DailyChallengeRules.Reconcile`; R-Q16 en `LevelFlow.ExitDaily`; R-Q15 en la sonda (R10); el aviso de R-Q14 en la UI (R9). Detalle en la nota §1.6.

## 2026-10-19 · R8 · La partida del Reto en AF.GameFlow: HECHA (Unity en verde tras la revisión de la licencia)

- **Commits:** `49501b4` y `535b92c` (arreglos de la revisión), en `spark/reto-del-rio-r8` (sale de R7b). Empujada y, tras pasar Unity, **fusionada** en `spark/reto-del-rio` (`c0149b6`; la verificación se anotó en `9122e71`).
- **Archivos nuevos:** `Assets/_Project/Scripts/GameFlow/LevelFlow.Daily.cs` y `Assets/_Project/Tests/EditMode/DailyLevelFlowTests.cs` (18 tests), con sus `.meta`.
- **Cómo funciona (sin tocar archivos existentes):** `LevelFlow.CreateDaily(...)` monta un flujo propio. **E8 ya no hace falta como estaba previsto.**
  - Catálogo de un solo tablero, numerado con el nivel efectivo `max_level + 1` [P], así que los desbloqueos de `BoosterConfig` funcionan sin cambiar nada.
  - Progreso de solo lectura: ni `SaveWin` ni `SaveNextLevel`, y sin regalos de tutorial.
  - Cartera del Reto **obligatoria**, sobre el mismo `Profile`, con una copia de `EconomyConfig` (`eco_reward_*` = 0; `rescue_min_level` = ∞): sin canoas, sin rescate, sin fallo duro con coste y sin Cosecha.
  - Copia de `BoosterConfig` con la Marea no ilimitada y sin Cuia de rescate.
  - Sin `AdGate`, sin intersticial (R-Q13, decidida (a)) y `StateStore` propio; rechaza el de campaña.
  - Un oyente intermedio entrega el evento `DailyResult` (Win/Exit/Restart) sin depender de AF.LiveOps.
  - Los tests de reflexión fallan si `EconomyConfig` o `BoosterConfig` ganan una clave que no se copia.
- **Verificación:**
  - autor, antes de que fallara la licencia: compila; EditMode 2.991 (2.987 + 3 omitidos y 1 fallo del test inestable `IntroArtTests`, que pasa al relanzarlo); **PlayMode 1.334 (1.175 + 159), 0 fallos**;
  - tras los arreglos (`535b92c`): `dotnet build -warnaserror` sin avisos y un arnés .NET fuera del repositorio con 18/18;
  - **sobre `535b92c`, tras volver la licencia:** `unity-compile` OK; EditMode **2.996**: 2.992 + 3 omitidos y 1 fallo, el de tiempo de `IntroArtTests.FruitFlight…`, que al relanzarlo con `-Filter AF.Tests.EditMode.IntroArtTests` pasa 9/9; **PlayMode 1.334 (1.175 + 159), 0 fallos**. Fusionada en `spark/reto-del-rio`.
- **code-reviewer:** primera ronda CAMBIOS NECESARIOS. Los tres bloqueantes:
  - sin cartera, el Reto caía en el embudo de D-008;
  - el comentario sobre la Crecida era falso;
  - el JSON interno no cumple af-level/1 y no lo decía.

  Segunda ronda: **APROBADO a falta de Unity** (y Unity, después, en verde).
- **[P]:** nivel efectivo `max_level + 1`, congelado al empezar; sin regalos de tutorial; Marea con las 3 gratis y luego el stock; modal de Aguas estancadas con Marea + Volver a la orilla; `DailyAttempt` vuelve a 1 al reanudar.
- **Ya no son [P]:** sin intersticial (R-Q13 (a)). Salir **borra** hoy la partida guardada del Reto, pero R-Q16 (a) pide dejarla **reanudable el mismo día**: pendiente en `ExitDaily` (`StateStore?.Clear()`).
- **Enganches nuevos:** E19 (guarda de la Crecida en `FailResolve`) y E20 (`WithLevel` y `LevelCatalog.ForBoard`), con sus parches en «Enganches para el integrador».
- **Notas para R9 y R10:**
  - `LevelNumber` es el nivel efectivo: no se enseña como «Nivel N» ni dispara tutoriales ni sondas de campaña; se usa `DailyBoardId`.
  - El HUD del Reto se suscribe a la cartera del Reto, y el saldo de la campaña hay que refrescarlo al volver.
  - La UI llama a `ExitDaily`, no a `AppFlow.ExitToLobby`.
  - R-Q15: `DailyPlayOutcome.Exit` llega en cada salida; la sonda emite `fail` una sola vez por día, y solo si el día termina sin victoria.

## 2026-10-19 · ⚠ La licencia de Unity vuelve a fallar (2.ª vez; resuelto: R8 pasó Unity después)

- **Hacia las 13:35** los batchmode vuelven a salir con el código 6, con los mismos errores que la primera vez:
  - `Code 404 … Found 0 entitlement groups and 0 free entitlements`;
  - `'com.unity.editor.headless' was not found`;
  - `No valid Unity Editor license found`.
- **Patrón:** las dos veces, justo antes del fallo, arrancó un proceso nuevo de la Unity CLI, `unity.exe --internal-auth-broker-serve` (PID nuevo a las 12:09 y a las 13:35). Parece que, al renovar la sesión, la CLI se queda sin derechos para el modo headless. La pasada larga que acababa de terminar (bloque c, PlayMode 1.334) salió bien porque arrancó antes.
- **Qué afectó:** la verificación en Unity de R8, que se hizo al volver la licencia (entrada de R8).
- **No he usado `unity auth` ni `unity license`** (regla dura 5).

## 2026-10-19 · Bloque (c) (R6-R7b: lector y lógica del Reto, sin pantalla): listo para integrar

Verificación completa de `spark/reto-del-rio` en `0a1b3ac`:

| Comando | Resultado |
|---|---|
| `dotnet build dotnet/AmazonFill.slnx -warnaserror` | código 0 |
| `dotnet test dotnet/AmazonFill.slnx` | 981 (975 + 6), código 0 |
| `python tools/validate_schema.py` | 103/103 |
| `unity-compile.ps1 -Architecture arm64` | OK |
| `unity-test.ps1 -Mode EditMode … -TimeoutSec 3600` | **2.978 (2.975 + 3 omitidos)**, 0 fallos |
| `unity-test.ps1 -Mode PlayMode … -TimeoutSec 3600` | **1.334 (1.175 + 159 omitidos)**, 0 fallos |

En total, 46 min. El bloque solo añade archivos nuevos: AF.LiveOps y sus tests, `DailyPool*` en AF.Levels y `IServerDate`, `FakeServerDate` y `ClockSecurityConfig` en AF.Platform. Ningún asmdef existente cambia todavía; `AF.UI.asmdef` += `AF.LiveOps` llegará con R9.

## 2026-10-19 · R7b · Premio, anti-reloj y racha: HECHA

- **Commits** (en `spark/reto-del-rio-r7b`, fusionados con `--no-ff` en `spark/reto-del-rio` como `f0fb4c8`):
  - `a83ec1c`: sugerencias de la revisión de R7a;
  - `880f7dc`: R7b.
- **Archivos nuevos:**
  - `Scripts/Platform/IServerDate.cs`, `FakeServerDate.cs` y `ClockSecurityConfig.cs`, donde ahora vive `sec_clock_max_drift_h`. Así es la única fuente y AF.Economy (Canoas) también puede leerla;
  - `Scripts/LiveOps/IDailyChallengeStore.cs`, `DailyChallengeRecord.cs`, `InMemoryDailyChallengeStore.cs`, `DailyChallengeRules.cs` y `DailyChallengeService.cs`;
  - tests (`DailyFixture`, `DailyChallengeRulesTests` 30, `DailyChallengeServiceTests` 18, `DailyChallengeStoreTests` 10).
- **Cambios en R7a:** `DailyOverrides.TryParse` (un valor no válido se queda en `{}`); `DailySlot.RequestedOverrideId` e `IsOverrideIgnored`, para que la sonda anote una sustitución ignorada.
- **Verificación** (worktree `spark-r7b`):
  - `unity-compile.ps1` → OK;
  - EditMode → **2.978 (2.975 + 3 omitidos)**, 0 fallos (70 tests nuevos);
  - `dotnet build -warnaserror` → 0 avisos.
  - El revisor lo repitió: 2.974 + 3 y 1 fallo, el de tiempo de `IntroArtTests.FruitFlight…` (inestable y ajeno); todos los de LiveOps en verde. Además, `dotnet test` 981 y `validate_schema.py` 103/103.
- **code-reviewer:** **APROBADO** a la primera. Comprobó contra 06 §4.1 y 08 §10.1:
  - nunca dos premios por fecha ni premio con el reloj atrasado;
  - «pendiente» solo con red y desfase de más de 24 h;
  - el orden registrar → guardar → pagar;
  - la racha y la Pororoca en los días 7 y 14;
  - los sellos del mes;
  - R-Q3, R-Q4 y R-Q9;
  - R-Q12 detrás de `DailyReconcileMode`, con las dos ramas probadas;
  - los estados de la tarjeta en el orden de la nota §8.
- **[P] de R7b:**
  - `pending` y `offline_claims` son listas de `{date, streak}`: la nota §5 y §10 ya está al día;
  - anular en la reconciliación deja la racha a 0 y quita el sello;
  - salir de un día ya ganado no emite `fail`;
  - `LevelStateSnapshot.Copy` va duplicada en AF.LiveOps, con un test por reflexión.
- **Lo que era [P] y ya está decidido:**
  - R-Q12 por defecto en (a), `KeepOfflineClaims`: **decidida (a)**;
  - anular no retira semillas: **decidida** (aclaración de R-Q12, (a): ni semillas ni la Pororoca);
  - salir sin ganar cierra la partida, que no queda reanudable: **R-Q16 decidió lo contrario** ((a), reanudable el mismo día): pendiente en el código;
  - la validación de pendientes con fecha ≤ hoy del servidor: **R-Q12b (a)** pide `|fecha − hoy_servidor| ≤ 1 día`: pendiente en `Reconcile`.
- **Pendiente menor** (va en un commit posterior):
  - documentar en `IDailyChallengeStore` que el registro y la cartera **tienen que vivir en el mismo `Profile`**, porque si no la garantía de «nunca dos pagos» se pierde (requisito de E4);
  - acotar `offline_claims`;
  - que `Reconcile` guarde solo si cambia algo;
  - la cita del test en `DailyChallengeRecord.cs:66`;
  - para R9: tras anular un pendiente con el reloj adelantado, la tarjeta sale «Sellada».

### Preguntas nuevas para el dueño (de la revisión de R7b): TODAS DECIDIDAS

| Id | Pregunta | Opciones (recomendada primero) | Qué bloqueaba | Estado |
|---|---|---|---|---|
| **R-Q12b** | Al validar un premio pendiente, ¿qué fechas se aceptan? Hoy se paga todo pendiente con fecha ≤ la del servidor, así que con red y el reloj atrasado a días no jugados se acaban pagando esos días. 06 §4.1 compara «> 1 día» | (a) solo si `\|fecha − hoy_servidor\| ≤ 1 día`; las demás se anulan; (b) toda fecha ≤ hoy del servidor (lo de ahora) | La rama de validación de R7b (es una línea) | **DECIDIDA: (a)** |
| **R-Q15** | `af_daily_challenge.result = fail`: ¿una vez por cada salida sin ganar, una por día, o solo si el día termina sin victoria? (R-Q9) | (a) uno por día, solo si ese día no se gana; (b) uno por salida (lo de ahora) | La sonda de R10 | **DECIDIDA: (a)** |
| **R-Q16** | Salir de la partida del Reto sin ganar: ¿la cierra (lo de ahora: solo se reanuda si se cierra la app; R-Q4) o la deja reanudable el mismo día? | (a) reanudable el mismo día; (b) se cierra | R8/R9 | **DECIDIDA: (a)** |
| **R-Q12** (aclaración) | En la rama (b) de R-Q12, ¿se retiran las semillas de una fecha anulada? El código no retira ni semillas ni la Pororoca | (a) no se retira nada; (b) se retiran las que queden sin gastar | Solo la rama (b) | **DECIDIDA: (a)** |

## 2026-10-19 · R7a · Calendario del Reto en AF.LiveOps: HECHA

- **Commit:** `065d0e6`, en `spark/reto-del-rio-r7a`; fusionado con `--no-ff` en `spark/reto-del-rio` (`24d3f18`).
- **Archivos nuevos** en `Assets/_Project/Scripts/LiveOps/`:
  - `AF.LiveOps.asmdef` (sin motor; `autoReferenced: false`; referencia AF.Core, Levels, Platform, Save y Economy);
  - `DailyChallengeConfig.cs` (las 9 claves de 08 §9 con sus defaults);
  - `DailyOverrides.cs` (`{fecha: {banda: id}}`, R-Q6);
  - `DailyCalendar.cs` (día del pool con módulo positivo, banda, desbloqueo, sustituciones y `DailySlot`);
  - `Tests/` (asmdef y 57 tests, entre ellos uno que lee la tabla de 08 §9 del Markdown y compara nombres, tipos y defaults).
- **Verificación** (worktree `spark-r7a`):
  - compila;
  - EditMode → 2.908 (2.905 + 3 omitidos), 0 fallos, en la pasada del revisor; en la del autor, 1 fallo del test inestable `IntroArtTests`, que pasa al relanzarlo;
  - `dotnet test` 981 y `validate_schema.py` 103/103.
- **code-reviewer:** **APROBADO** a la primera, sin bloqueantes. Las sugerencias (`TryParse`, sustitución ignorada visible, `sec_clock_max_drift_h` en AF.Platform) se aplicaron en R7b.
- **Para el integrador:** 08 §9 sigue diciendo `daily_overrides` = `json {fecha: level_id}`; R-Q6 lo cambia a `{fecha: {banda: id}}` (E11). El test solo compara el tipo base `json` y el default `{}`.

## 2026-10-19 · Verificación completa de `spark/reto-del-rio` en `ba57160` (R0-R4 y R6): VERDE

| Comando | Resultado |
|---|---|
| `dotnet build dotnet/AmazonFill.slnx -warnaserror` | código 0, 0 avisos |
| `dotnet test dotnet/AmazonFill.slnx` | 981 (975 + 6 omitidos), código 0 |
| `python tools/validate_schema.py` | 103/103 PASS |
| `dotnet run --project tools/level-generator -- --self-test daily` | todo PASS |
| `dotnet run --project tools/level-qa -- --self-test daily` | todo PASS |
| `scripts/unity-compile.ps1 -Architecture arm64` | OK |
| `scripts/unity-test.ps1 -Mode EditMode -Architecture arm64 -TimeoutSec 3600` | **2.851 (2.848 + 3 omitidos)**, 0 fallos: la línea base de 2.744 más 107 de R6 |
| `scripts/unity-test.ps1 -Mode PlayMode -Architecture arm64 -TimeoutSec 3600` | **1.334 (1.175 + 159 omitidos)**, 0 fallos, igual que la línea base (32 min) |

En total, 45 min. Durante parte del tiempo hubo otro batchmode en un worktree: como mucho 2-3 a la vez, en carpetas distintas, con 64 GB de RAM (el encargo §1 permite 3 con 32 GB o más).

## 2026-10-19 · R6 · Lector del pool en AF.Levels: HECHA

- **Commits:** `a724b43` (el lector y 66 tests) y `28be4a2` (los arreglos de la revisión y 41 tests más), en `spark/reto-del-rio-r6`; fusionados con `--no-ff` en `spark/reto-del-rio` (`afb58a2`).
- **Archivos nuevos:**
  - `Assets/_Project/Scripts/Levels/DailyPool.cs` y `DailyPoolReader.cs`;
  - `Assets/_Project/Tests/EditMode/DailyPoolTestPacks.cs` y `DailyPoolReaderTests.cs` (107 tests);
  - sus `.meta`, generados por Unity.
- **Qué hace:**
  - `DailyPoolReader.Read(json)` es estricto: pide las 3 bandas con 730 días cada una. `Read(json, DailyPoolCoverage.PresentBands)` existe solo para tests y herramientas.
  - Rechaza con `LevelFormatException`: `kind` distinto de `daily`; `pack_schema` fuera de 1-3; `slot`, `id` o `seed` incoherentes; `level`; `solution`, `safe_path` o `qa`; `relief` que no está vacío; `world` fuera de 1-13; bandas incompletas; repetidos.
  - [P] Rechaza también mecánicas, `murky`, `hidden` e `intro_mechanic` (nota §13).
  - `Find(band, day)`, `FindById`, `BandOf` y `DayOf` no asignan memoria (comprobado con `GC.GetAllocatedBytesForCurrentThread`).
  - El revisor leyó los 30 tableros de la muestra de R3 con el lector: todos se construyen y validan. Un tablero literal de R3 (`d1_n0000`) va en un test, para que un cambio de formato del generador rompa el cliente.
- **Verificación** (worktree `spark-r6`, ARM64):
  - `unity-compile.ps1` → OK;
  - `unity-test.ps1 -Mode EditMode -TimeoutSec 3600` → **2.851 (2.848 + 3 omitidos), 0 fallos**;
  - `dotnet build … -warnaserror` → 0 avisos.
- **code-reviewer:** primera ronda CAMBIOS NECESARIOS (faltaban tests de varias reglas y se aceptaba `world` 0). Segunda ronda: código bien, pero la aprobación quedó condicionada a pasar Unity, que estaba bloqueado por la licencia. Pasó en verde tras la revisión del dueño: **APROBADO**.
- **Test inestable** (no es del Spark): `IntroArtTests.FruitFlight_Has14DistinctSprites_InOneTexture_GeneratedWithin10ms` falló una vez por tiempo (10,42 ms con un tope de 10 ms) en este equipo ARM64. Al relanzarlo con filtro pasó.
- **Enganche opcional E14** (extraer los ayudantes que `DailyPoolReader` copia de `LevelPackReader`): en «Enganches para el integrador».

## 2026-10-19 · R4 · QA del pool en `level-qa --daily`: HECHA · Bloque (a) listo para integrar

- **Commits** (en `spark/reto-del-rio-r4`, fusionados con `--no-ff` en `spark/reto-del-rio` como `ba57160`):
  - `32b8cc4`: detalles de la revisión de R3;
  - `cafe8c4`: la QA;
  - `ed109a3`: «Enganche:» del despacho en `tools/level-qa/Program.cs`;
  - `8afdd0e`: arreglos de la revisión.
- **Archivos nuevos:**
  - `tools/level-qa/Daily/DailyChecks.cs` (21 ids `QA-DAILY-*`) y `DailyQaCommand.cs`;
  - `tools/level-qa/Daily/Shared/DailySpec.cs` y `DailyIds.cs`: copias byte a byte de las del generador; la autoprueba falla si divergen (E16 las sustituye por enlaces);
  - `tools/level-qa/fixtures/daily/`: 1 fixture válido y 18 malos.
- **Uso:**
  ```
  dotnet run --project tools/level-qa -- --daily <pack> [--spec <daily_v1.json>] [--partial] [--no-reproduce]
  ```
  `--self-test daily` prueba todos los fixtures.
- **Severidad:**
  - **fallo:** esquema (vía `validate_schema.py --pack`), slot ↔ id/seed, 730 días por banda, K/vacíos/capacidad, ventana del bot (R-Q11, decidida (a): es compuerta), A*, par ≥ 2K, tope de pares **por banda según la especificación** (R-Q1b, decidida (a): B3 sin tope), sin mecánicas ni ocultos, `relief` vacío y campos prohibidos, ≤ 14 cuias, < 2 MB (siempre), y que la semilla reproduzca el tablero entero (proceso hijo del generador, 15 min de tope);
  - **solo aviso (R-Q10):** anti-repetición (forma y Hamming, con la vuelta del ciclo 729 → 0) y trampas tempranas.
- **Detalles:**
  - Con `--partial` una muestra nunca pasa por pool completo: siempre sale el aviso «no es el pool completo».
  - El despacho de `Program.cs` va antes del `--self-test` genérico, por la misma razón que en R2.
- **Verificación:**
  - `level-qa --self-test daily` → 30 PASS; las autopruebas que ya había → todo PASS;
  - QA de la muestra con `--partial` → exit 1: 15 `QA-DAILY-RISK` (los K 9 de B2 y todo B3) y 2 `QA-DAILY-BOT` (`d2_n0002`, `d2_n0006`). Era lo esperado mientras R-Q1b y R-Q11 seguían abiertas; con R-Q1b (a) en la especificación (B2 sin K 9, B3 sin tope) los RISK dejan de aplicarse, y los BOT siguen siendo fallo (R-Q11 (a));
  - QA del pool completo (generado fuera del repositorio) → exit 1:

    | Id | Fallos | Avisos | Por banda |
    |---|---|---|---|
    | QA-DAILY-RISK | 1.095 | — | B2: 365 · B3: 730 |
    | QA-DAILY-BOT | 68 | — | B1: 13 · B2: 55 |
    | QA-DAILY-REPEAT-SHAPE | — | 414 | B1: 133 · B2: 103 · B3: 178 |
    | QA-DAILY-EARLY-TRAPS | — | 1.172 | B1: 353 · B2: 669 · B3: 150 |

    Pasan DAYS, SIZE, SCHEMA y REPRO (los 2.190 tableros se reproducen).
  - builds con `-warnaserror` → 0 avisos; `dotnet test` → 981 (975 + 6); `validate_schema.py` → 103/103.
- **code-reviewer:** primera ronda CAMBIOS NECESARIOS (faltaban los tests de los avisos de R-Q10, y `--partial` apagaba la compuerta de tamaño). Arreglado; segunda ronda **APROBADO**.
- **Para el dueño (R-Q10):** EARLY-TRAPS avisa en 1.172 de 2.190 tableros, porque con 1 vacío y la etiqueta «normal» el tope de la campaña es 0. Si algún día pasa a compuerta, el Reto necesitará su propio tope.

### Bloque (a) (R2-R4: el modo diario del generador y su QA): listo para integrar (ya integrado: `fcbbd94`)

Está en `spark/reto-del-rio` y no toca Unity. Sus enganches (E15-E18) están en «Enganches para el integrador».

## 2026-10-19 · Licencia de Unity: RESUELTO

El dueño revisó la licencia en Unity Hub. Desde entonces Unity compila y pasa los tests (R6 en verde). La entrada de abajo queda como historial.

## 2026-10-19 · ⚠ BLOQUEO: la licencia de Unity dejó de valer en este equipo (resuelto: entrada de arriba)

- **Desde** unas 12:10 (hora local) ningún batchmode arranca: `unity-compile.ps1` y `unity-test.ps1` salen con el código 6.
- **Log** (`.claude/worktrees/spark-r6/Logs/compile.log`):
  - `Error: Code 404 … Found 0 entitlement groups and 0 free entitlements matching requested entitlement ids`;
  - `'com.unity.editor.headless' was not found`;
  - `No valid Unity Editor license found`.
- **Qué no es la causa:** el aviso «Code 10 while verifying Licensing Client signature» ya salía en las pasadas buenas de la línea base (10:55 y 11:33), cuando sí se resolvían los derechos. Unity Hub (3.22.3) y la CLI (1.0.0-beta.13) no han cambiado de versión.
- **Hipótesis:** se retiró o caducó el derecho de la licencia en el servidor. Por ejemplo, el asiento está en uso en otro equipo, o la licencia no incluye `com.unity.editor.headless`.
- **No he usado `unity auth` ni `unity license`** (regla dura 5).
- **Lo que necesitaba del dueño:** revisar la licencia de Unity de este equipo en Unity Hub (Preferencias › Licencias). Cuando volviera a funcionar, repetir las pruebas de Unity pendientes:
  - R6 en el worktree `spark-r6`: compilar y pasar EditMode (esperado 2.851: 2.848 + 3 omitidos);
  - todo lo que hubiera del cliente desde R7.
- **Qué afectaba:** todo el carril del cliente (R6-R11) quedaba sin verificar en Unity. Seguí escribiendo el código y comprobándolo fuera de Unity, sin darlo por terminado ni fusionarlo hasta pasar Unity. El carril de herramientas (R2-R4) no lo necesita.

## 2026-10-19 · R3 · Modo `--daily` del generador: HECHA

- **Commits** (en `spark/reto-del-rio-r3`, fusionados con `--no-ff` en `spark/reto-del-rio` como `c87a181`):
  - `e8783b9`: arreglos menores de la revisión de R2;
  - `bb1dad2`: el modo `--daily`;
  - `ee48f52`: la muestra `tools/out/daily_sample/daily_pool_v1.json` (30 tableros, 20.144 bytes, SHA-256 `61cf6bef…3ac1`) y `tools/out/daily_sample_report.md`. Se commitea como las otras muestras de `tools/out/`.
- **Archivos nuevos** en `tools/level-generator/Daily/`: `DailyRunner.cs`, `DailyTints.cs` (copia adaptada de `TintAssigner`, que no cambia), `DailyPackWriter.cs`, `DailyReport.cs` y `DailyCommand.Generate.cs`.
- **Uso:**
  ```
  dotnet run --project tools/level-generator -- --daily --spec tools/level-generator/specs/daily_v1.json --bands 1,2,3 --days 0-729 --out <dir> [--report <md>] [--check] [--threads N]
  ```
  Códigos de salida: 0 (también con avisos), 1 (un día sin tablero o `--check` distinto), 2 (uso).
- **Verificación:**

  | Comando | Resultado |
  |---|---|
  | `--daily` de la muestra (bandas 1-3, días 0-9) | exit 0; `--check` idéntico con 20 hilos y con 1 |
  | `python tools/validate_schema.py --pack tools/out/daily_sample/daily_pool_v1.json` | PASS |
  | `python tools/validate_schema.py` | 103/103 |
  | `--self-test daily` | todo PASS: 3 días por banda idénticos byte a byte en paralelo y con 1 hilo; la semilla reproduce `d2_n0001` |
  | `--self-test` general | todo PASS |
  | `dotnet build` de `tools/level-generator` y de `dotnet/AmazonFill.slnx` con `-warnaserror` | 0 avisos |
  | `dotnet test` | 981 (975 + 6 omitidos) |

- **Pool completo** (3 × 730), generado fuera del repositorio: 6,1 s con 20 hilos y 1.445.059 bytes. Valida con `--pack` y es reproducible. **No se publica** (R5).
- **Muestra, por banda (10 días cada una):**

  | Banda | bot_fail mín · mediana · máx | par mín · mediana · máx | Fuera de ventana | Pares > tope |
  |---|---|---|---|---|
  | B1 | 0,400 · 0,438 · 0,600 | 13 · 16,5 · 21 | 0 | 0 |
  | B2 | 0,625 · 0,725 · 0,900 | 19 · 20,5 · 25 | 2 (`d2_n0002` 0,85, `d2_n0006` 0,90; los dos K 8 con `fill`) | 5 (los de K 9) |
  | B3 | 0,750 · 0,950 · 1,000 | 21 · 22,5 · 25 | 0 | 10 (todos) |

- **En el pool completo:**
  - B1: 13 días fuera de ventana (mínimo 0,30) y 231 con el respaldo `fill` E=2;
  - B2: 55 fuera de ventana y 365 por encima del tope de pares;
  - B3: 0 fuera de ventana y los 730 por encima del tope de pares.
- **code-reviewer:** APROBADO a la primera. Reprodujo la muestra byte a byte, comprobó el SHA y cruzó las 30 filas del informe con el pack (0 discrepancias). Las sugerencias menores van en R4.
- **[P] de R3:**
  - cabecera con `empties_default` 1 y `max_risk_pairs` igual al máximo real, así que R4 comprueba el tope por banda contra la especificación;
  - `world` en cada tablero (opcional en `daily`);
  - un tablero por línea, como `PackWriter`;
  - `skippable` false (04 §2.6 e) y `hint_supported` true (06 §4.1; R-Q8).
- **Lo que era [P] y ya está decidido:**
  - R-Q1b: si el tope no admite el K, el tablero lleva el **mínimo de pares posible** y queda marcado, sin cambiar K ni paleta. Con R-Q1b (a) (B2 con K = 8, mínimo 1 = tope; B3 sin tope) el caso deja de darse;
  - R-Q11: si ningún candidato entra en la ventana, se guarda **el más cercano** y queda marcado. Con R-Q11 (a) pasa a ser **error** del generador (R5, en curso; encargo §9, punto 2).
- **Datos de R-Q11 (ya decidida):**
  - B3 sale casi siempre ≥ 0,90 (mediana 0,95): entra en «≥ 75 %», pero son tableros muy difíciles;
  - B2 con K 8 y `fill` se pasa de la ventana en 55 de 730 días.

## 2026-10-19 · R2 · Especificación del pool y autoprueba: HECHA

- **Commits:** `da6a9bb` (especificación, lector, ids y autoprueba) y `99d35f7` («Enganche:» del despacho en `tools/level-generator/Program.cs`), en `spark/reto-del-rio-r2`; fusionados con `--no-ff` en `spark/reto-del-rio` (`3ca28dc`).
- **Archivos nuevos:** `tools/level-generator/specs/daily_v1.json`, `tools/level-generator/Daily/DailySpec.cs`, `DailyIds.cs`, `DailyCommand.cs`.
- **Verificación** (en el worktree `spark-r2`):
  - `dotnet run --project tools/level-generator -- --self-test daily` → 86 PASS, código 0;
  - `dotnet run --project tools/level-generator -c Release -- --self-test` (las autopruebas que ya había) → 314 PASS y todas «todo PASS», código 0, 49 s;
  - `dotnet build tools/level-generator -warnaserror` → 0 avisos; `dotnet build dotnet/AmazonFill.slnx -warnaserror` → 0 avisos, código 0;
  - `dotnet test dotnet/AmazonFill.slnx` → 981 (975 + 6 omitidos), código 0;
  - `python tools/validate_schema.py` → 103/103 PASS, sin cambios en `tools/out/schema_example_08_4_1.json`;
  - Unity no aplica (la tarea solo toca `tools/`).
- **code-reviewer:** APROBADO a la primera, sin bloqueantes. Comprobó las bandas de 03 §16, las fuentes de 03 §4.4, el agua del glosario §8, las regex del esquema (la autoprueba las lee del esquema) y los 2.190 ids y semillas. Sugerencias menores (estado de la paleta de B1 y un mensaje de error) van en R3.
- **Desviación del encargo (§3.3, punto 1):** el bloque del despacho va **antes del `--self-test` genérico** (tras `--validate-spec`), no justo antes de `--parity`: el genérico solo reconoce subcomandos fijos y daría error de uso con `--self-test daily`. Son 5 líneas, en su commit «Enganche:». El revisor lo da por correcto y necesario. El integrador ya lo corrigió en el encargo §9 (E18).
- **Errata:** el mensaje de `da6a9bb` dice «43 especificaciones mal formadas»; son 44. No lo reescribo.
- **[P] en `daily_v1.json`:** regla del agua `1 + (día mod 9)` (R-Q2; R5 la cambia a la fecha, encargo §9, punto 3) y `k_by_day` (día par → K menor, impar → mayor). Los tintes y los pares de riesgo se leen de `campaign_v2.json` (`tints_from`), sin copiarlos.
- **Lo que era [P] y ya está decidido:** paleta de B2 y B3 (R-Q1b (a)) y lo que haga el generador con los días fuera de ventana (R-Q11 (a)); los dos se aplican en R5.

## 2026-10-19 · R1 · Nota de diseño: HECHA

- **Commits:** `3e0063e` (la nota) y `4968d1a` (junta dos comentarios duplicados), en `spark/reto-del-rio`.
- **Archivo:** `docs/dev/reto-del-rio.md`.
- **code-reviewer:** primera ronda CAMBIOS NECESARIOS (6 bloqueantes: el contrato del almacén no seguía a `ILevelStateStore`; la ventana del bot era a la vez aviso y compuerta; la reconciliación sin red se daba por regla; las opciones de R-Q1b estaban incompletas; faltaba dónde viven los tests que ven AF.LiveOps; cinco citas imprecisas). Arreglados todos; segunda ronda **APROBADO**.
- **Para el integrador:**
  - `IServerDate` y `FakeServerDate` van en **AF.Platform** (08 §2.2 le da el «`Date` del servidor para el reloj») y no en AF.LiveOps, como decía el encargo §4. **Confirmado** por el integrador (encargo §5). Nota §1.2.
  - Las decisiones R-Q1-R-Q10 llevan fecha 2026-10-20 en la primera versión del encargo; el reloj de este equipo dice 2026-10-19 (el encargo §5 dice ahora 2026-10-19). Confírmalo al llevarlas a `decisiones.md`.
  - Ediciones del GDD que piden R-Q4-R-Q8 (08 §4.3, 08 §9 tipo de `daily_overrides`, 06 §4.1): nota §1.3 y enganche E11.

### Preguntas nuevas para el dueño (nota §1.6): TODAS DECIDIDAS

| Id | Pregunta | Opciones (recomendada primero) | Qué bloqueaba | Estado |
|---|---|---|---|---|
| **R-Q1b** | R-Q1 no se puede cumplir: B2 con K = 9 tiene 3 pares de riesgo (tope 1) y B3, con los tintes de 1-200, al menos 3 con K = 10 y 4 con K = 11 (tope 2). Cálculo en la nota §1.1 | (a) B2 solo con K = 8 y B3 con `max_pares_riesgo` libre, como la campaña desde el 201; (b) subir los topes: B2 a 3 y B3 a 4; (c) mantener los topes y bajar K (B2 = 8, B3 = 9); (d) ampliar la paleta de B3 a 1-300 (no basta) | Paleta de R3 (va como parámetro [P]) y la publicación del pool (R5) | **DECIDIDA: (a)** |
| **R-Q11** | Si en 30 candidatos ningún tablero entra en la ventana del bot de su banda (03 §4.4 midió 0,88-1,00 de fallo con 1 vacío; B2 pide 60-80 %) | (a) la ventana es compuerta: el día no se publica hasta ajustar la especificación; (b) se publica el más cercano y QA avisa; (c) ensanchar la ventana (cambia 03 §16) | R5 | **DECIDIDA: (a)** |
| **R-Q12** | Al reconectar, qué hace la reconciliación con las victorias cobradas sin red (08 §10.1) | (a) nada: no se retiran semillas; (b) anular las de fecha posterior a la del servidor (sello y racha) | Solo esa rama de R7b (irá detrás de una opción) | **DECIDIDA: (a)** |
| **R-Q13** | ¿Intersticial al terminar un Reto? (05 §6.1 solo habla de la campaña) | (a) no; (b) como en la campaña | R8/R9 | **DECIDIDA: (a)** |
| **R-Q14** | La Pororoca de la racha puede llegar a B1 antes del nivel 30, que la desbloquea (04 §2.4) | (a) entra en el stock y espera al 30; (b) otro premio en B1; (c) nada antes del 30 | Solo un aviso en la UI de R9 | **DECIDIDA: (a)** |

## 2026-10-19 · R0 · Arranque y línea base: HECHA (desbloqueada)

El dueño pidió «instala lo necesario para trabajar». Instalado:
- `jsonschema` 4.26.0 en Python 3.14 (`py -3.14 -m pip install jsonschema`);
- .NET SDK 10.0.401 ARM64 (`winget install Microsoft.DotNet.SDK.10 --version 10.0.401 --architecture arm64`; instalador oficial de download.microsoft.com con el hash verificado);
- Unity 6000.3.25f1 ARM64 (changeset `e1dba0a9aba4`) con Android Build Support, OpenJDK, SDK y NDK, con Unity Hub 3.22.3 en modo headless (`-- --headless install --version 6000.3.25f1 --changeset e1dba0a9aba4 --architecture arm64 --module android --childModules`). La Unity CLI no se usó para instalar (`CLAUDE.md`). La licencia ya estaba activa en el equipo.

**Línea base** (`spark/reto-del-rio` en `2db3a07`, igual que `origin/integracion-fase1-fase2` `80b26b4` más el archivo de estado):

| Comando | Resultado | Tiempo |
|---|---|---|
| `dotnet build dotnet/AmazonFill.slnx -warnaserror` | código 0, 0 avisos | 18 s |
| `dotnet test dotnet/AmazonFill.slnx` | 981 (975 + 6 omitidos), código 0 | 3 min 9 s |
| `python tools/validate_schema.py` | 103/103 PASS, código 0, sin cambios | — |
| `pwsh -NoProfile -File scripts/unity-compile.ps1 -Architecture arm64` | OK, código 0 (primera importación incluida) | 8 min |
| `pwsh -NoProfile -File scripts/unity-test.ps1 -Mode EditMode -Architecture arm64` | 2.744 (2.741 + 3 omitidos), 0 fallos, código 0 | 6 min |
| `pwsh -NoProfile -File scripts/unity-test.ps1 -Mode PlayMode -Architecture arm64 -TimeoutSec 3600` | 1.334 (1.175 + 159 omitidos), 0 fallos, código 0 | 22 min |

- Coinciden con los recuentos de `CLAUDE.md`.
- **Aviso:** con el límite por defecto (900 s), PlayMode sale con el código 6 (sin veredicto) a los 15 min, en mitad de `MapMarkersTests`: en este equipo tarda 21 min. Uso `-TimeoutSec 3600`, el valor que el propio script recomienda para una primera importación.
- Máquina: ARM64, 64 GB de RAM. Con 32 GB o más el encargo permite 3 Editores a la vez; me quedo en **2** salvo que haga falta.

## 2026-10-19 · R0 · Primer intento: BLOQUEADA por el entorno (resuelto en la entrada de arriba)

**Rama:** `spark/reto-del-rio`, creada desde `origin/integracion-fase1-fase2` en `80b26b4` («Deja en el encargo del Spark las respuestas del dueño a R-Q1-R-Q10 del Reto del río») y empujada a `origin`. `git lfs pull` hecho sin errores.

**Máquina (sección 1 del encargo):**

| Comprobación | Comando | Resultado |
|---|---|---|
| Equipo | `Win32_ComputerSystem.Model` / `Win32_Processor.Name` | ProArt P14 H7407BA · NVIDIA RTX Spark N1X (20 núcleos) |
| Arquitectura | `$env:PROCESSOR_ARCHITECTURE` | `ARM64` → `-Architecture arm64` / `--architecture arm64` |
| RAM | `(Get-CimInstance Win32_ComputerSystem).TotalPhysicalMemory / 1GB` | 64,0 GB (de sobra: se podrían usar hasta 3 Editores; aún no aplica) |
| Python | `python --version` | 3.14.7 (ARM64) ✔ |
| `jsonschema` | `python -c "import jsonschema"` / `py -3.14 -m pip show jsonschema` | **✘ No instalado** (`ModuleNotFoundError`) |
| .NET SDK | `dotnet --list-sdks` | **✘ Ningún SDK.** Solo el host y los runtimes 8.0.32 (`Microsoft.NETCore.App`, `WindowsDesktop.App`). `global.json` pide 10.0.401 |
| Editor de Unity | `Test-Path "C:\Program Files\Unity\Hub\Editor\6000.3.25f1\Editor\Unity.exe"` | **✘ No existe.** En `Hub\Editor` solo está `6000.3.26f1` (`unity editors` lo da como x86_64). El encargo prohíbe abrir otra versión sobre el proyecto |
| Unity CLI | `unity --version` | 1.0.0-beta.13 ✔ (alias `%LOCALAPPDATA%\Microsoft\WindowsApps\unity.exe`) |
| git LFS | `git lfs version` | 3.7.1 (windows arm64) ✔ |

**Verificación completa de `CLAUDE.md` (paso 3):** no se puede ejecutar ninguno de los comandos obligatorios:
- `dotnet build dotnet/AmazonFill.slnx -warnaserror` y `dotnet test`: falla el arranque del SDK («A compatible .NET SDK was not found. Requested SDK version: 10.0.401»).
- `python tools/validate_schema.py`: necesita `jsonschema`.
- `scripts/unity-compile.ps1` y `scripts/unity-test.ps1` (EditMode y PlayMode): necesitan Unity 6000.3.25f1 ARM64.

No hay línea base: **sin recuentos**. No he instalado nada (regla dura 4 de `CLAUDE.md`; el encargo, sección 1: «no lo instales tú: si falta, pregunta al dueño»).

**Me paro aquí**, como manda R0 («si algo sale en rojo en la base, para y anótalo»). R1-R12 dependen de R0 y no he empezado ninguna.

### Pendiente del dueño (para desbloquear R0; hecho, entrada de arriba)

1. **.NET SDK 10.0.401** para Windows ARM64 en `C:\Program Files\dotnet` (lo fija `global.json`, con `rollForward: latestFeature`).
2. **`jsonschema`** en Python 3.14 (`py -3.14 -m pip install jsonschema`).
3. **Unity 6000.3.25f1** (changeset `e1dba0a9aba4`), Editor **Windows ARM64**, con el módulo Android, en `C:\Program Files\Unity\Hub\Editor\6000.3.25f1\`, desde el Hub (la CLI no instala; `CLAUDE.md`, «Stack fijado»). La licencia la activa el dueño.
   - El 6000.3.26f1 x86_64 que hay instalado **no se usa**: abrirlo sobre el proyecto lo actualizaría.
4. Avisar al Spark cuando esté para repetir R0 y medir la línea base.

### Decisiones del dueño registradas (para que el integrador las lleve a `docs/dev/decisiones.md`)

Las R-Q1 a R-Q10 de la sección 5 del encargo, decididas por el dueño el 2026-10-20 («Acepto tus propuestas para R-Q1 a R-Q10 del Reto del río; déjalas en el encargo del Spark»). Se aplicarán tal cual cuando se desbloquee el trabajo:

| Id | Decisión |
|---|---|
| R-Q1 | Cada banda usa solo los tintes ya estrenados en su tramo de la campaña: B1 (K 6-7) con los de 1-40 y `max_pares_riesgo` 0; B2 (K 8-9) con los de 1-80 y como mucho 1; B3 (K 10-11) con los de 1-200 y como mucho 2 (ajustado después por R-Q1b) |
| R-Q2 | El agua (`water_base`) y el fondo rotan por los mundos 1-9 según la fecha: el agua y el kit de fondo del mundo de ese día |
| R-Q3 | Manda la fecha en que se empezó la partida: se sigue con ese tablero y se cobra ese premio aunque se gane pasada la medianoche |
| R-Q4 | La partida del Reto se reanuda al volver a abrir la app el mismo día; al cambiar de día se descarta |
| R-Q5 | El bloque `daily_challenge` del perfil añade la última fecha cobrada, el premio pendiente de validar y los sellos del mes (además de `last_date`, `result` y `streak_days`) |
| R-Q6 | `daily_overrides` = `{fecha: {banda: id del pool}}` |
| R-Q7 | La Pororoca sale cada 7 días seguidos ganados (cada racha completa); un día sin ganar rompe la racha; el marco de país de los sellos queda fuera hasta que existan cosméticos |
| R-Q8 | Los comodines son los de la campaña salvo los que el GDD excluye en el Reto (Cuia de rescate y Salto del Boto), y gastan el stock igual que en la campaña |
| R-Q9 | `result = fail` en `af_daily_challenge` es salir sin ganar ese día |
| R-Q10 | La anti-repetición (03 §17 #9) y el tope de trampas tempranas (03 §17 #3) se miden en el pool y se informan como aviso, sin ser compuerta |

### Riesgos y notas

- Las fechas: el encargo y estas decisiones van fechados el 2026-10-20; el reloj de esta máquina dice 2026-10-19.
- `git switch -c … origin/integracion-fase1-fase2` deja como upstream la integración; el push con `-u origin spark/reto-del-rio` lo corrige para que ningún `git push` vaya a `integracion-fase1-fase2`.

---

## Enganches para el integrador

Única sección de enganches (encargo §3.4). Cada uno: archivo, parche o descripción exacta, por qué, cómo se prueba y estado. Los que dependen de las pantallas (E1-E7, E9, E10, E12) llevan su parche exacto en **R12**; aquí van descritos.

| # | Estado | Archivo |
|---|---|---|
| E1 | Opcional · no preparado (la tarjeta va en su lienzo; requisito de 16:9 en R12) | `Scripts/UI/LobbyScreen.cs` |
| E2 | **Parche probado** (R12, `spark-enganches/E2.diff`) | `Scripts/UI/GameBootstrap.cs` |
| E3 | **Parche probado** (R12, `E3.diff` + regenerar escena) | `Scripts/UI/Editor/GameSceneBuilder.cs` |
| E4 | **Parche probado** (R12, `E4.diff`) | `Scripts/Save/Profile.cs`, `ProfileJson.cs`, `ProfileMigrations.cs` |
| E5 | **Parche probado** (R12, `E5.diff` + `E5-fix.diff`) | `Data/Loc/strings.csv` |
| E5b | **Parche probado** (T1, `spark-enganches/E5b.diff`): las 2 filas de E6 y E7 | `Data/Loc/strings.csv`, `LocTablesTests` |
| E6 | **Parche probado** (T1, `spark-enganches/E6.diff`; necesita E5b) | `Scripts/UI/GameScreen.cs` |
| E7 | **Parche probado** (T1, `spark-enganches/E7.diff`; necesita E5b) | `Scripts/UI/NoCanoesScreen.cs`, `FunnelPlayModeTests.cs` |
| E8 | **Ya no hace falta** | (iba en `Scripts/GameFlow/LevelFlow*.cs`) |
| E9 | **Parche probado** (R12, `E9.diff`) | `Scripts/Economy/Transaction.cs`, `Wallet.cs` |
| E10 | **Ya no hace falta** (R10 sale de archivos del Spark) | `Scripts/UI/DevProbe.cs` (si hace falta) |
| E11 | **Parches probados** (`spark-parches-docs.md`, 12 diffs, aplican sobre `bc65242`) | `docs/dev/decisiones.md`, `docs/gdd/03`, `05`, `06`, `08` |
| E12 | **Parches probados** (`spark-parches-docs.md`) | `docs/dev/prototipo-v0.md`, `docs/dev/traspaso/README.md`, `CLAUDE.md` |
| E13 | Opcional | `Assets/_Project/Tests/EditMode/AF.Tests.EditMode.asmdef` |
| E14 | Opcional | `Scripts/Levels/LevelPackReader.cs`, `DailyPoolReader.cs` |
| E15 | **Aplicado** (`752bb55`) | `tools/level-generator/Program.cs`, `README.md` |
| E16 | **Aplicado en la rama** `spark/reto-cierre` (`c7ec205`; registro en `spark-enganches/E16.diff`) | `tools/level-qa/level-qa.csproj` y `tools/level-qa/Daily/` |
| E17 | **Aplicado** (`752bb55` y, en `spark/reto-cierre`, `e5061f2`) | `tools/level-qa/Program.cs`, `README.md` |
| E18 | Punto 1 corregido por el integrador (encargo §9); queda el punto 2 | Encargo §3.3 |
| E19 | Pendiente, para cuando exista la Crecida | `Scripts/GameFlow/LevelFlow.Funnel.cs`, `FailResolve.cs` |
| E20 | Pendiente (limpieza) | `Scripts/Levels/LevelDefinition.cs`, `LevelCatalog.cs` |
| E21 | **Parche probado** (R12, `E21.diff`) | `Scripts/UI/GameScreen.cs` (`OnIntroStarted`) |
| E22 | **Parche probado** (R12, `E22.diff`; opcional) | `Scripts/UI/GameBootstrap.cs` (`PressAndroidBack`) |
| E23 | **Parche probado** (R12, `E23.diff`) | `Scripts/UI/GameBootstrap.cs` (`OnApplicationPause`) |
| E24 | **Parche probado** (R12, `E24.diff`) | `Scripts/UI/DevProbe.cs` |
| E25 | Para cuando exista la Crecida | barra de la Crecida en la vista del Reto |

### E1 · Inicio: la tarjeta del Reto (pendiente; parche en R12)

- **Archivo:** `Assets/_Project/Scripts/UI/LobbyScreen.cs`.
- **Qué:** crear `DailyChallengeCard` (clase nueva de R9) en el inicio y refrescarla al mostrarlo. Mientras el sistema no exista en el build, oculta (D-008).
- **Por qué:** 06 §4.1 «Pantalla» y §4.7; encargo §3.4.
- **Prueba:** PlayMode de R9-R11 (los cinco estados de la nota §8).

### E2 · Arranque: `SetUpDaily()` (pendiente; parche en R12)

- **Archivo:** `Assets/_Project/Scripts/UI/GameBootstrap.cs`.
- **Qué:** campo `[SerializeField] TextAsset dailyPool;` y la llamada a `SetUpDaily()` (parcial nuevo `GameBootstrap.Daily.cs`) tras crear perfil, cartera y reloj.
- **Por qué:** encargo §3.4; crea el pool, `DailyChallengeService` y las pantallas, y conecta la victoria.
- **Prueba:** PlayMode de R11 (punta a punta).

### E3 · Escena: el pool en `dailyPool` (pendiente; parche en R12)

- **Archivo:** `Assets/_Project/Scripts/UI/Editor/GameSceneBuilder.cs`.
- **Qué:** asignar `Assets/_Project/Data/Daily/daily_pool_v1.json` a `dailyPool`, como con `river_stops`; el integrador regenera `Game.unity` (regla dura 3).
- **Por qué:** 08 §3; 08 §6.2.
- **Prueba:** regenerar la escena y PlayMode de R11.

### E4 · Perfil: bloque `daily_challenge` y `ProfileDailyChallengeStore` (pendiente; parche en R12)

- **Archivos:** `Assets/_Project/Scripts/Save/Profile.cs`, `ProfileJson.cs` (y `ProfileMigrations.cs` si hiciera falta; 08 §4.3 permite un campo nuevo sin migración mientras el esquema no se publique).
- **Qué:** el bloque de la nota §10 (`last_date`, `result`, `streak_days`, `last_claimed_date`, `pending`, `offline_claims`, `stamps`, `in_progress`) y `ProfileDailyChallengeStore : IDailyChallengeStore`.
- **Requisito:** el registro y la cartera **en el mismo `Profile`**, para no pagar dos veces la misma fecha (R7b, pendiente menor); acotar `offline_claims` (solo sirve a la rama (b) de R-Q12, no la decidida).
- **Por qué:** 08 §4.3; R-Q4, R-Q5, R-Q16.
- **Prueba:** ida y vuelta del JSON del perfil y los tests de `DailyChallengeStoreTests` sobre el almacén del perfil.

### E5 · Textos (pendiente; filas en R9-R12)

- **Archivo:** `Assets/_Project/Data/Loc/strings.csv`.
- **Qué:** copiar las filas de `docs/dev/traspaso/spark-strings-reto.csv` (misma cabecera, `status = borrador`; lo cultural, `pendiente_asesor`).
- **Por qué:** 08 §4.7; `.claude/rules/localizacion.md`.
- **Prueba:** los tests de localización que ya validan `strings.csv`.

### E5b · Las dos filas de texto de E6 y E7 (parche probado; T1)

- **Archivos:** `Assets/_Project/Data/Loc/strings.csv` y `LocTablesTests`.
  - En `strings.csv`, las filas `daily_teaser` y `no_canoes_daily`, en orden ordinal e idénticas a `spark-strings-reto.csv`.
  - En `LocTablesTests`, `keys.AddRange(DailyText.E5bKeys)` en `UiTextKeys`.
- **Parche:** `docs/dev/traspaso/spark-enganches/E5b.diff`.
- **Orden:** va **antes o junto con E6 y E7**. Sin E5b, en release `daily_teaser` y `no_canoes_daily` salen vacíos. Con E5b integrado, las claves de `DailyText.E5bKeys` pasan a `DailyText.NewKeys`.
- **Prueba:** `LocTablesTests` y `DailyUiLogicTests` en EditMode. `Csv_RowsAreInStringsCsv_E5…` admite que falten las dos a la vez (sin E5b) o que estén las dos; nunca una sola.

### E6 · Tarjeta de 2 s tras ganar el 7 (parche probado; T1)

- **Lo del Spark** (en la rama):
  - `Scripts/UI/DailyTeaserCard.cs`, con la placa clara del aviso, sin rojo y sin capturar toques;
  - `Tests/PlayMode/DailyTeaserCardTests.cs`;
  - el texto `daily_teaser` = «Desde el nivel {level}, un tablero nuevo cada día» (06 §2.2, fila 7; 02 §13), con `{level}` = `live_unlock_level_daily`.
- **Parche:** `docs/dev/traspaso/spark-enganches/E6.diff`.
  - En `Scripts/UI/GameScreen.cs`, cuatro puntos: el campo `_dailyTeaser`, `_dailyTeaser?.Tick(dtMs)`, el acceso `internal DailyTeaser` y un bloque al final de `OnVictoryScreen` con `flow.LevelNumber == 7`.
  - El test nuevo `DailyTeaserVictoryTests.cs`, sin `.meta`: lo genera Unity al integrarlo.
- **Prueba:** `DailyTeaserVictoryTests`.
  - La victoria real del 7 la enseña a los 900 ms y durante 2 s.
  - Siguiente e Inicio siguen activos, y Siguiente lleva al 8.
  - Pulsar Siguiente la quita.
  - No sale sin el Reto en el build (`DailyPoolDisabledOverride`) ni en la victoria del 8.
- **[P] para el dueño (recomendación: aceptarlos):**
  - sale en cada victoria del 7 que se vea, sin guardar nada en el perfil;
  - a los 900 ms, centrada sobre las cuias;
  - solo con el Reto en el build y con `live_unlock_level_daily` > 7;
  - no entra en la cola de avisos de D-011;
  - `daily_teaser` va en `pendiente_asesor` porque lo pide el encargo §10 T1.2, aunque el texto no es cultural: el dueño puede bajarlo a `borrador`.

### E7 · Sin canoas: «Mientras esperas: Jugar el Reto del río» (parche probado; T1)

- **Lo del Spark** (en la rama):
  - `Scripts/UI/NoCanoesDailyLink.cs`;
  - en `GameBootstrap.Daily.cs`: `DailyInBuild`, `NoCanoesDailyAvailable`, `PressNoCanoesDaily()` y `LastNoCanoesChoice`;
  - la sonda `AF_STATE no_canoes_choice` con `choice=daily_challenge` (04 §11; 08 §8.2), en `DailyProbeFormat.NoCanoesChoice`;
  - `Tests/PlayMode/NoCanoesDailyTests.cs`.
- **Parche:** `docs/dev/traspaso/spark-enganches/E7.diff`.
  - En `NoCanoesScreen.cs`: el enlace bajo el panel, encima de «Volver al inicio», y el id `daily_challenge` en `VisibleButtons` y en la clave del layout.
  - El test nuevo `NoCanoesDailyLinkTests.cs`, sin `.meta`.
  - **Un test existente**, `FunnelPlayModeTests.AssertNoCanoesLayout`: añade `daily_challenge` entre `infinite_canoes` y `home` cuando el enlace se ve (`b1b4628`). Hace falta porque, con la escena con pool (E3), el 31 muestra el enlace.
- **Prueba:**
  - `NoCanoesDailyLinkTests`: Sin canoas desde el inicio, desde Nivel fallido en el 31 y sin Reto en el build;
  - `NoCanoesDailyTests`;
  - `FunnelPlayModeTests` del 31.
- **[P] para el dueño (recomendación: aceptarlos):**
  - solo se ofrece con la tarjeta en «Jugar» o «Seguir» (con el día sellado o pendiente, la tarjeta abriría el calendario);
  - el toque sale por el camino de «Volver al inicio» y abre el tablero de hoy;
  - sin el chevron «›» del wireframe.

### E8 · Modo Reto en `LevelFlow` (ya no hace falta)

- Iba en `LevelFlow.cs`, `LevelFlow.Victory.cs`, `LevelFlow.Funnel.cs` y `LevelFlow.Resume.cs`: sin `SaveWin` ni `Transaction.Win`, nivel efectivo, Marea no ilimitada, sin canoas ni Cuia de rescate, reanudar.
- **Por qué ya no:** R8 lo hizo sin tocar archivos existentes, con `LevelFlow.CreateDaily(...)` en `LevelFlow.Daily.cs` (catálogo de un tablero con el nivel efectivo, progreso de solo lectura, cartera y configs copiadas, `StateStore` propio). Lo que queda va en E19 y E20.

### E9 · Cosecha doble del Reto (pendiente; parche en R12)

- **Archivos:** `Assets/_Project/Scripts/Economy/Transaction.cs`, `Wallet.cs`.
- **Qué:** una transacción para duplicar el premio del Reto (p. ej. `Transaction.DoubleHarvestDaily(int seeds)`, 60 → 120 [P]), con `source = double_harvest`.
- **Por qué:** `Transaction.DoubleHarvest(int level, RewardTier tier)` duplica la Cosecha de un nivel de campaña y `Transaction.Earn` rechaza `SeedSource.DoubleHarvest` (nota §1.4). 04 §2.8; 06 §4.1. Hasta entonces, el botón del Reto queda desactivado.
- **Prueba:** test de economía del pago doble solo con premio CONFIRMADO.

### E10 · Sondas desde los puntos de evento (pendiente; parche en R12)

- **Archivo:** `Assets/_Project/Scripts/UI/DevProbe.cs` (si hace falta).
- **Qué:** llamar a `DevProbe.Daily.cs` (R10) desde los puntos de evento; `fail` una vez por día (R-Q15).
- **Por qué:** 08 §8.2.
- **Prueba:** líneas `AF_STATE` en el build de desarrollo y los tests de `DailyProbeFormat`.

### E11 · GDD y decisiones (pendiente)

- **Archivos:** `docs/dev/decisiones.md`; `docs/gdd/03` §16; `docs/gdd/05` §6.1; `docs/gdd/06` §4.1; `docs/gdd/08` §4.3, §8.2 y §9.
- **Qué:**
  - llevar a `decisiones.md` R-Q1-R-Q10, R-Q1b, R-Q11-R-Q16 (con la aclaración de R-Q12) y la confirmación de `IServerDate` en AF.Platform; confirmar la fecha de R-Q1-R-Q10 (2026-10-20 en la primera versión del encargo; 2026-10-19 en este equipo);
  - 08 §9: `daily_overrides` pasa de `json {fecha: level_id}` a `{fecha: {banda: id}}` (R-Q6); el default `{}` no cambia;
  - 08 §4.3: el bloque `daily_challenge` de la nota §10 (R-Q4, R-Q5, R-Q16);
  - 06 §4.1: la racha «cada 7» (R-Q7), los comodines (R-Q8), las bandas (R-Q1b), la validación de pendientes (R-Q12b) y `pool_overrides` (R-Q6);
  - 03 §16: B2 con K 8 y B3 sin tope (R-Q1b); la ventana del bot es compuerta (R-Q11);
  - 05 §6.1: sin intersticial en el Reto (R-Q13); 08 §8.2: `fail` una vez por día (R-Q15).
- **Por qué:** `docs/gdd/**` está prohibido para el Spark; el GDD manda (`CLAUDE.md`). Nota §1.3.
- **Prueba:** `DailyChallengeConfigTests` (lee la tabla de 08 §9 y solo compara el tipo base `json` y el default `{}`) sigue en verde; `python tools/validate_schema.py` 103/103.

### E12 · Documentación (pendiente; texto en R12)

- **Archivos:** `docs/dev/prototipo-v0.md`, `docs/dev/traspaso/README.md`, `CLAUDE.md`.
- **Qué:** las sondas nuevas; `Scripts/LiveOps/` y `Data/Daily/` en el mapa; `--daily` y `--self-test daily`; los recuentos nuevos.
- **Por qué:** encargo §3.4.
- **Prueba:** revisión.

### E13 · `AF.Tests.EditMode.asmdef` += `AF.LiveOps` (opcional)

- **Archivo:** `Assets/_Project/Tests/EditMode/AF.Tests.EditMode.asmdef`.
- **Qué:** añadir `"AF.LiveOps"` a `references`.
- **Por qué:** solo si se quieren en EditMode los tests de AF.UI que tocan el Reto; hoy van en `AF.LiveOps.Tests` (EditMode) y en PlayMode (nota §4).
- **Prueba:** `unity-compile.ps1` y EditMode.

### E14 · Extraer los ayudantes de `DailyPoolReader` (opcional)

- **Archivos:** `Assets/_Project/Scripts/Levels/LevelPackReader.cs`, `DailyPoolReader.cs`.
- **Qué:** `DailyPoolReader` copia unas 90 líneas privadas de `LevelPackReader` (`ReadUnits`, `ReadBoard` y los ayudantes). Extraerlas a una clase `internal` compartida de AF.Levels, y hacer `internal` el lector de mecánicas si el Reto llega a usarlas.
- **Por qué:** una sola copia. Mientras tanto, los tests de la copia protegen contra una divergencia.
- **Prueba:** EditMode (`DailyPoolReaderTests`, 107, y los de `LevelPackReader`).

### E15 · `daily` en el `--self-test` sin nombre del generador (APLICADO en `752bb55`)

- **Archivos:** `tools/level-generator/Program.cs` y `README.md`.
- **Parche** (el aplicado; en `ba57160` eran las líneas 133-143):
  ```diff
           int holgura = HolguraSelfTest.Run();
  -        return Math.Max(Math.Max(Math.Max(tinaja, seasons), Math.Max(variants, registry)), Math.Max(curationTest, holgura));
  +        int daily = AmazonFill.Tools.LevelGenerator.Daily.DailyCommand.Run(["--self-test", "daily"]);
  +        return Math.Max(Math.Max(Math.Max(Math.Max(tinaja, seasons), Math.Max(variants, registry)), Math.Max(curationTest, holgura)), daily);
  ```
  - `|daily` en `Usage` (`--self-test [tinaja|…|holgura|daily]`) y «, holgura o daily» en el mensaje de error; `--daily` en la ayuda y en `tools/level-generator/README.md`.
- **Por qué:** encargo §9, punto 6.
- **Prueba:** `dotnet run --project tools/level-generator -c Release -- --self-test` → 435 PASS, 0 FAIL, exit 0, con «daily».

### E16 · `level-qa.csproj` enlaza `tools/level-generator/Daily/` (APLICADO en `spark/reto-cierre`, `c7ec205`; el texto de abajo es el plan original)

- **Archivos:** `tools/level-qa/level-qa.csproj`; `tools/level-qa/Daily/DailyQaCommand.cs` y `DailyChecks.cs` (comentarios); borrar `tools/level-qa/Daily/Shared/DailySpec.cs` y `DailyIds.cs`.
- **Parche completo:** `docs/dev/traspaso/spark-enganches/E16.diff` (idéntico a `c7ec205`, aplicado en `spark/reto-cierre`). Lo de abajo es el plan original:
  ```diff
  --- a/tools/level-qa/level-qa.csproj
  +++ b/tools/level-qa/level-qa.csproj
  @@ -31,4 +31,11 @@
       <Compile Include="../level-generator/Generation/*.cs" Exclude="../level-generator/Generation/SeasonTableWriter.cs" LinkBase="Shared/Generation" />
     </ItemGroup>
  +
  +  <!-- Reto del río (modo diario de level-qa): el mismo lector de la especificación (DailySpec), los mismos ids y semillas (DailyIds)
  +       y la misma generación (DailyRunner, DailyTints y DailyPackWriter) que el generador, enlazados, para que QA-DAILY-REPRO
  +       regenere el pool en memoria. La entrada de la línea de comandos del generador (DailyCommand*.cs) se queda fuera. -->
  +  <ItemGroup>
  +    <Compile Include="../level-generator/Daily/*.cs" Exclude="../level-generator/Daily/DailyCommand*.cs" LinkBase="Shared/Daily" />
  +  </ItemGroup>
   </Project>
  ```
  En `DailyQaCommand.cs`:
  - `Reproduce(pack, spec, tints)` regenera en memoria en lugar de lanzar `dotnet run` del generador:
    ```csharp
    List<int> bands = slots.Select(l => l.Band!.Value).Distinct().Order().ToList();
    List<int> days = slots.Select(l => l.Day!.Value).Distinct().Order().ToList();
    var dailyTints = new DailyTints(tints, spec);
    DailyRun run = DailyRunner.Run(spec, dailyTints, bands, days, Environment.ProcessorCount);
    // run.Errors → «el generador falla: …»; DailyPackWriter.CheckBoard(b) → «el generador rechaza su tablero: …»;
    // sin tableros → «el generador no da ningun tablero».
    JsonNode regen = JsonNode.Parse(PackWriter.Bytes(DailyPackWriter.Write(spec, dailyTints, run.Boards)))!;
    // y la misma comparación de antes: JsonNode.DeepEquals de cada tablero con el regenerado.
    ```
  - `RunChecks` pierde el parámetro `specPath`; se borran `GeneratorProject`, `ReproTimeout`, `TimedOut`, `Ranges`, `RepoRoot` y `RunDotnet`, y los `using` de `System.ComponentModel` y `System.Text`; se añade `using AmazonFill.Tools.LevelGenerator.Generation;`.
  - En `--self-test daily` salen las comprobaciones de identidad de `Daily/Shared/`, la de `Ranges` y la de `RepoRoot`.
  - Los comentarios de cabecera de `DailyQaCommand.cs` y `DailyChecks.cs` pasan a describir la reproducción en memoria.
- **Por qué:** una sola copia de `DailySpec` y `DailyIds`; QA-DAILY-REPRO sin proceso hijo ni tope de 15 min.
- **Prueba:** `dotnet build tools/level-qa -warnaserror` y `dotnet run --project tools/level-qa -- --self-test daily` (todo PASS).

### E17 · `level-qa`: documentar `daily` (APLICADO: `752bb55` y `e5061f2`: README con `--daily` y `daily` en el `--self-test` sin nombre)

- **Archivos:** `tools/level-qa/Program.cs` y `tools/level-qa/README.md` (los dos hechos; el README y el `--self-test` sin nombre con `daily`, en `e5061f2`).
- **Hecho:** en `Usage`, `--daily <daily_pool_v1.json> [--spec <daily_v1.json>] [--partial] [--no-reproduce]` y `--self-test [key2|reglas|variante|daily]`; el mensaje de error con «key2, reglas, variante o daily». Desde `e5061f2`, el `--self-test` sin nombre de level-qa ejecuta `key2` y `daily` y sale con el código mayor.
- **Pendiente:** `--daily` en `tools/level-qa/README.md`. Va con E16: el texto tiene que describir QA-DAILY-REPRO tal como quede (en memoria con E16; hoy, proceso hijo).
- **Prueba:** `dotnet run --project tools/level-qa -- --self-test bogus` → el mensaje de uso nombra «daily» (comprobado).

### E18 · Corrección del encargo §3.3 (punto 1 hecho; queda el punto 2)

- **Archivo:** `docs/dev/traspaso/spark-reto-del-rio.md` §3.3.
- **Punto 1** (`tools/level-generator/Program.cs`): el despacho va «antes del `--self-test` genérico», no «justo antes de `--parity`». **Corregido** por el integrador en el encargo §9.
- **Punto 2** (`tools/level-qa/Program.cs`): cambiar «antes de la lectura de argumentos de la campaña» por «antes del `--self-test` genérico (que rechazaría «daily»)», que es donde está (R4).
- **Por qué:** el `--self-test` genérico solo reconoce subcomandos fijos y daría error de uso con `--self-test daily`.
- **Prueba:** revisión del texto.

### E19 · Guarda de la Crecida en `FailResolve` (pendiente; para cuando exista la Crecida)

- **Archivos:** `Assets/_Project/Scripts/GameFlow/LevelFlow.Funnel.cs` y `FailResolve.cs`.
- **Parche:**
  ```diff
  @@ private void Fail(FailKind kind)
  -        FailOutcome outcome = FailResolve.Resolve(_attempt, kind, LevelNumber, _wallet);
  +        FailOutcome outcome = FailResolve.Resolve(_attempt, kind, LevelNumber, _wallet, campaign: !IsDaily);
  ```
  ```diff
  -        public static FailOutcome Resolve(LevelAttempt attempt, FailKind kind, int level, Wallet? wallet)
  +        public static FailOutcome Resolve(LevelAttempt attempt, FailKind kind, int level, Wallet? wallet, bool campaign = true)
  ...
               canoeLost = wallet.Apply(Transaction.HardFail(level)).CanoeLost;
           }
  +        if (!campaign)
  +        {
  +            return new FailOutcome(kind, attempt.FailsLevel, canoeLost); // 06 §4.3: el Reto no rompe la Crecida ni gasta la Hoja escudo
  +        }
  ```
- **Por qué:** 06 §4.3 («si not nivel.es_campana: return»). Hoy no hace falta: por esa vía no se cuenta nada de campaña (`stats.fails_hard` no cambia, y lo fija un test).
- **Prueba:** el test del reinicio desde el modal del Reto comprueba que la Crecida del perfil no cambia, y un test de campaña, que sí se rompe.

### E20 · `LevelDefinition.WithLevel` y `LevelCatalog.ForBoard` (pendiente; limpieza)

- **Archivos:** `Assets/_Project/Scripts/Levels/LevelDefinition.cs` y `LevelCatalog.cs`.
- **Parche:**
  - `internal LevelDefinition WithLevel(int level) => new LevelDefinition(Id, level, World, WaterBase, _palette, Units, _caps, _cuias, _attrs, IsMurky, _hidden, Par, DifficultyLabel, _relief, IntroMechanic, Skippable, HintSupported, Band, ParApprox);`
  - `public static LevelCatalog ForBoard(LevelDefinition board, int level) => FromPack(new LevelPack("daily_play", "daily", board.World, "daily_play", board.WaterBase, new[] { board.WithLevel(level) }));`
  - Después, `DailyPackFor` (en `LevelFlow.Daily.cs`) se reduce a `LevelCatalog.ForBoard(board, effectiveLevel)`.
- **Por qué:** quita el paso por un JSON interno que no cumple el esquema af-level/1 (lo señaló la revisión de R8).
- **Prueba:** `DailyLevelFlowTests` tal cual, con la comprobación «same».

### E21 · Rótulo «Nivel N · intento k» en el Reto (pendiente; R9)

- **Archivo:** `Assets/_Project/Scripts/UI/GameScreen.cs`, en `OnIntroStarted` (línea ~996).
- **Parche:**
  ```diff
                   StartIntro(flow, durationMs, isShort);
  -                if (isShort)
  +                if (isShort && !flow.IsDaily)
                   {
                       // 02 §2 (INTRO): «En reintento muestra Nivel N · intento k», …
  ```
- **Por qué:** en el Reto, cada Volver a la orilla enseña «Nivel {nivel efectivo} · intento k», y 06 §4.1 pide la cabecera «Reto del río» sin «Nivel N». Hasta que se aplique, el defecto se ve en el teléfono.
- **Prueba:** en `DailyChallengeTests.Restart_IsFreeAndUnlimited_SameBoard`, tras `PressRestart()` y un fotograma, `Assert.That(daily.View!.BannerText, Is.Null.Or.Not.Contains("Nivel"))`. En la campaña, el test de reintento que ya existe sigue viendo «Nivel N · intento k».

### E22 · Atrás con el Reto abierto (opcional; R9)

- **Archivo:** `Assets/_Project/Scripts/UI/GameBootstrap.cs`, en `PressAndroidBack()`, justo después del bloque del Mapa del río (`if (_mapScreen != null && _mapScreen.Visible) { … }`) y antes de `if (_app.Screen == AppScreen.Level)`.
- **Parche:**
  ```diff
  +            // Reto del río (R9): Atrás cierra su calendario como «Inicio»; con el tablero del Reto en juego no hace nada, como en un
  +            // nivel (02 §3; 04 §6.6). HandleBack devuelve false si no hay calendario abierto.
  +            if (_dailyScreen != null && _dailyScreen.IsOpen)
  +            {
  +                return _dailyScreen.HandleBack();
  +            }
  +
               if (_app.Screen == AppScreen.Level)
  ```
- **Por qué:** con el Reto abierto, AppFlow sigue en Lobby, así que sin la guarda Atrás caería en `PressBack()` del inicio, que está oculto. Con ella, Atrás solo cierra el calendario; en el tablero no hace nada, como en la campaña.
- **Prueba:** `AndroidBack_DoesNothingOnTheBoard_ButClosesTheCalendar` cubre la lógica de `HandleBack`. Con el enganche, `boot.PressAndroidBack()` da false con el tablero y true con el calendario, y el inicio sigue oculto mientras se juega.

### E23 · Volver de segundo plano en el Reto (pendiente; R9)

- **Archivo:** `Assets/_Project/Scripts/UI/GameBootstrap.cs`, en `OnApplicationPause` (al volver de segundo plano), justo después del bloque `if (_flow != null && _screen != null && InLevel) { … }`.
- **Parche:**
  ```diff
  +            // Reto del río (R9; 02 §14): su vista salta al modelo y la selección se limpia, como la de campaña.
  +            if (_dailyScreen?.Flow != null && _dailyScreen.View != null)
  +            {
  +                _dailyScreen.View.SnapToModel();
  +                _dailyScreen.View.SettleReveal();
  +                _dailyScreen.View.IdleHintBackFromBackground();
  +                _dailyScreen.Flow.ClearSelection();
  +            }
  ```
- **Por qué:** `InLevel` es false durante el Reto (AppFlow está en Lobby). Sin esto, al volver de segundo plano la vista del Reto se queda a mitad de animación, con la cuia seleccionada y sin reiniciar la cuenta de la pista por inactividad (02 §14; 02 §3.4; 04 §2.3). La persistencia no cambia: `PersistNow` ya guarda el lote del perfil.
- **Prueba:** test PlayMode que abre el Reto, toca una cuia (queda seleccionada), llama a `boot.SendMessage("OnApplicationPause", false)` y afirma `daily.Flow.Selected == -1` y `daily.View.IsViewIdle`.

### E24 · Las sondas de partida del Reto, marcadas (pendiente; R9-R10)

- **Archivo:** `Assets/_Project/Scripts/UI/DevProbe.cs` (prohibido para el Spark; `DevProbe` es `sealed`).
- **Parche:** justo después de cada una de las cuatro líneas `sb.Append(",\"level\":").Append(flow.LevelNumber.ToString(CultureInfo.InvariantCulture));` (hoy en las líneas 92 `AnimSkipped`, 361 `FormatState`, 488 `FormatFunnel` y 585 `FormatVictory`):
  ```diff
  +            AppendDailyBoard(sb, flow);
  ```
  `AppendDailyBoard` ya está en `DevProbe.Daily.cs` (R10): en el Reto añade `,"daily":"d{b}_n{nnnn}"`; en la campaña no añade nada.
- **Por qué:** la vista del Reto usa su propia `DevProbe`, pero las líneas `load`, `ready`, `pour`… salen con `"level"` igual al nivel efectivo (`max_level + 1`), que no es un nivel de campaña. Con la marca se distinguen por adb (08 §8.2 pide `level_name = d{b}_n{nnnn}` para el Reto). Las líneas `Event` de `GameScreen*.cs` (`idle_hint`, `hint`, `tinaja_tip`) quedan sin marca; basta la línea `load`.
- **Prueba:** en PlayMode, abrir el Reto y comprobar que `load`, `ready` y `pour` llevan `"daily":"…"`, y que en la campaña no aparece. Documentarlo en `docs/dev/prototipo-v0.md` (E12), por si algún script de adb da por hecho que `level` es ≥ 1.

### E25 · Sin barra de la Crecida en el Reto (para cuando exista)

- **Qué:** cuando exista la barra de la Crecida en la vista de partida, ocultarla con la misma guarda que E21 (`flow.IsDaily`). 06 §4.1 pide el tablero del Reto sin barra de Crecida, y 06 §4.3 dice que el Reto no la toca. Va junto con E19. En el código, `DailyChallengeScreen.cs` lo anota como «E21»: la numeración correcta es E25.
