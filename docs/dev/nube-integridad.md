# Nube sin SDK, integridad, anti-fraude local y «Eliminar cuenta» en local (bloque B8 de la tanda 5)

Nota de diseño del bloque B8 (`docs/dev/plan-tanda5.md` §4, fila B8; D-033). Rama `spark/t5-b8`, desde `origin/integracion-fase1-fase2`
`9eb593c`. Fuentes: GDD 08 §4.3 (perfil, `economy_ts`, `integrity`, `locale`, `first_app_version`, `country_first`,
`applied_purchases`), 08 §10.1-§10.3 (anti-reloj, SSV, compras), 08 §11 (guardado local y en la nube, `ResolveConflict`, eliminación de
cuenta), 08 §14.2 («conflictos local/nube (8 casos)»), 04 §7.1 («Reloj» de las canoas) y 04 §10.1-§10.2 (inventario y sincronización).
Lo que el GDD no fija va marcado [P] y, si es una decisión, como pregunta en §7 con su recomendada aplicada.

Sin SDK, sin paquetes, sin permisos y sin secretos: la nube es una interfaz con implementación falsa (08 §2.1, principio 6), la firma usa
`System.Security.Cryptography` de netstandard2.1 y las sales son constantes públicas (no es seguridad real: 08 §10.1).

## 1. Qué hay y dónde

| Pieza | Archivo | Dónde llega |
|---|---|---|
| Nube del perfil (`af_main`) | `Scripts/Save/ICloudSave.cs`, `FakeCloudSave.cs` | rama |
| Fusión pura, 8 casos | `Scripts/Save/CloudMerge.cs` (campos de hoy) + `CloudMerge.Fields.cs` (campos de B8) | rama + `B8-profile.diff` |
| Cuentas del HMAC | `Scripts/Save/ProfileHmac.cs` | rama |
| Firma del perfil | `Scripts/Save/ProfileIntegrity.cs`, `SigningProfileStore.cs` | `B8-profile.diff` |
| Campos nuevos del perfil | `Profile.cs`, `ProfileJson.cs` | `B8-profile.diff` |
| `first_app_version`, `locale`, `country_first` | `Scripts/Save/ProfileIdentity.cs` | `B8-profile.diff` |
| Sincronización al arrancar | `Scripts/Save/CloudSync.cs` | `B8-profile.diff` |
| Identidad del dispositivo | `Scripts/Platform/IDeviceIdentity.cs` (+ `ManualDeviceIdentity`), `Scripts/UI/InstallIdentity.cs` | rama |
| Anti-reloj | `Scripts/Platform/GuardedClock.cs` (`IClockTrust`); `Canoes.cs` y `Wallet.cs` | rama + `B8-economy.diff` |
| `applied_purchases` y revocación | `Transaction.cs`, `Wallet.cs`; `Scripts/Economy/LocalRevocation.cs` | `B8-economy.diff` |
| B1a-Q2 (`streak` marca `economy_ts`) | `ProfileStreakStore.cs` | `B8-economy.diff` |
| «Eliminar cuenta y datos» | `Scripts/UI/DeleteAccountDialog.cs`, `AccountText.cs`, `SettingsScreen.Account.cs`, `GameBootstrap.Account.cs`, `DevProbe.Account.cs` | rama + `B8-ui.diff` |
| Arranque: firma, nube, identidad y reloj | `Scripts/UI/GameBootstrap.Cloud.cs`; llamadas en `GameBootstrap.cs`, `GameBootstrap.Streak.cs`, `SettingsScreen.cs` | `B8-ui.diff` |
| Textos | `docs/dev/traspaso/spark-enganches/B8-strings.csv` (9 claves, `pendiente_asesor`) | CSV aparte |

La rama compila y pasa sus tests sola: lo que necesita los campos nuevos del perfil o toca archivos reservados (encargo §3.2) va en los
parches, con sus tests.

## 2. La fusión con la nube: los 8 casos (`CloudMerge.Resolve`)

08 §14.2 pide probar «conflictos local/nube (8 casos)» sin enumerarlos. La lista [P] sale de 08 §11 (`ResolveConflict` y fila
«Conflicto»), 04 §10.2 y 08 §10.1:

1. **Sin nube** (o blob ilegible): se queda el local y se sube (`ok`).
2. **Idénticos**: nada (`ok`).
3. **Distinto `progress.max_level`**: gana el mayor, con su cartera y su `level_state`.
4. **Mismo `max_level`**: gana el `economy_ts` más reciente; sin fecha cuenta como la más antigua.
5. **Empate total**: gana el local [P].
6. **Consumibles** (semillas, `stock`, canoas): siempre los del ganador, **nunca se suman** (04 §10.2).
7. **Campos que se fusionan** sobre el ganador:
   - `daily`: si es el mismo `date_local`, `max` por clave, con **B1a-Q1 (a)** (D-033): `shield_available` solo si los dos lo tienen,
     `streak_hits` por unión y `streak_seeds` por máximo; si las fechas difieren, el bloque del día más reciente entero [P] (los contadores
     de ayer no cuentan hoy). `af_rescue_declined_days` y `af_rescue_hidden_until`, por máximo siempre (no son del día);
   - `entitlements`: `no_ads` y `starter_bought` por «o», `skins` por unión (`merged.entitlements = or(...)`);
   - `fauna`: unión con el nivel más bajo (`merged.album = union(...)`);
   - `progress.map_seen_level` por máximo (D-013), `undo_tip_seen` por «o» (DC23) y `skipped_levels` por unión (**Q-S1 de B3**:
     `last_skip_level` se deduce de `max(skipped_levels)` y `since_last_skip` ya no se lee, así que no se fusiona);
   - `stats` por máximo [P] y `ads.last_iap` por máximo [P];
   - con el parche: `applied_purchases` del ganador (D-033, Q-B8-1; 08 §11: las que falten las devuelve `restoreLedger`), `voided_purchases` del ganador [P], `integrity.fraud_flag` por máximo [P],
     `pending_rewards` del ganador [P], `first_app_version` y `country_first` del perfil creado antes [P] y `locale` del ganador.
8. **Integridad**: si uno de los dos está manipulado (su firma no cuadra) y el otro no, gana el coherente (08 §10.1: «en conflicto con la nube gana el blob coherente») y no se une
   nada del otro [P]: un blob manipulado no aporta derechos, compras ni contadores.

Lo demás (`settings`, `sloth`, `streak`, `daily_challenge`, `level_state` y el resto de `progress`) es del ganador: va en el mismo blob
que su cartera, así un hito de la Crecida o un cobro del Reto nunca se pagan dos veces. `af_save_sync.result` es `ok`,
`conflict_local_won` o `conflict_cloud_won` (08 §11); `merged` queda para la nube de la 1.2.

**`CloudSync.AtStartup`** (08 §11: «descarga al arrancar con sesión»): sin sesión no hace nada; lee `af_main`; un blob de un esquema más
nuevo no se lee ni se pisa (08 §4.4) y uno anterior se migra; comprueba las dos firmas, fusiona, firma el resultado y lo sube si cambió.
En el arranque (`GameBootstrap.Cloud.cs`) va justo tras leer el perfil, antes de crear la cartera: si gana la nube, el juego empieza ya con
el fusionado y se guarda. Con el perfil en solo lectura (08 §4.4) no se fusiona ni se sube.

## 3. Integridad (`integrity.hmac`)

- **Lo firmado:** el JSON del perfil (`ProfileJson.Serialize`, determinista) con `integrity.hmac` = null. Las claves nuevas se omiten en su
  default, así un perfil anterior a B8 se escribe igual que antes.
- **La clave:** SHA-256 de una sal pública + `integrity.device_first`, que es la huella (SHA-256 con sal, 32 hexadecimales) del id del
  instalación que creó el perfil (un id aleatorio, `af_install_id` en PlayerPrefs; nunca un identificador del dispositivo). Al restaurar en otro teléfono,
  `device_first` no cambia: cualquier dispositivo puede comprobar un blob de la nube (Q-B8-2).
- **Cuándo:** `SigningProfileStore` firma antes de cada escritura del lote (en `Serialize`, en el hilo principal, así el escritor de
  segundo plano solo ve bytes); el arranque comprueba la firma del perfil leído **antes** de la primera escritura (que lo volvería a firmar).
- **Qué pasa si no cuadra:** nada visible (08 §10.1: sin castigo automático). Pierde los conflictos con la nube frente a uno que cuadra y
  sale en la sonda (`AF_STATE profile_integrity`). `payer_class = suspect` llega con la analítica (B7).
- Un perfil **sin firma** o **migrado** (la migración cambia lo firmado, así que no se puede comprobar) no entra en el caso 8: sigue
  las reglas de siempre [P] (Q-B8-5, cambiada tras la revisión). Solo una firma que no cuadra en un perfil sin migrar es «no coherente».
- Al arrancar, la firma se comprueba nada más leer el perfil (antes de los extras de desarrollo `af_seeds` y `af_max_level`) y, si no
  cuadra, suma `integrity.fraud_flag` (solo se mide). La sospecha no se guarda para la fusión: tras la primera escritura el perfil vuelve a
  cuadrar (pregunta Q-B8-10).

## 4. Anti-reloj y revocación local

- **`GuardedClock`** (08 §10.1, «Manipulación del reloj»): envuelve el reloj del dispositivo y `IServerDate`. Sin red, o con un desfase
  dentro de `sec_clock_max_drift_h` (24 h), es el reloj del dispositivo. Con más desfase, su hora es la del servidor, así los topes
  diarios (`daily.date_local`: vídeos, rescates, Pororocas, intersticiales, la Balsa y los hitos de la Crecida) usan la fecha del servidor,
  y la cartera congela las canoas (`Wallet.CanoesFrozen`: ni regeneran ni pierden el cronómetro; 04 §7.1). Se da a la cartera, los
  anuncios, la tienda, la Crecida y la cartera del Reto (`LevelFlow.CreateDailyWallet`); **no** a las reglas del Reto del río, que ya contrasta él mismo (aplicaría el desfase dos veces). Hoy el servidor es
  `FakeServerDate.Offline()`: el juego se comporta igual que antes.
- **Recompensas provisionales** (08 §10.2): `LocalRevocation.RegisterProvisional` apunta lo concedido con su `nonce` en
  `pending_rewards` [P]; `Confirm` lo da por bueno; `ExpireStale` revoca lo que siga sin confirmar tras `sec_ssv_grace_h` (24 h), sin bajar
  de 0, y suma `integrity.fraud_flag` [P]. **En la 1.0 sin servidor nada se registra** (si no, todo se revocaría a las 24 h).
- **Compras** (08 §10.3 y §11): `Transaction.Purchase(..., purchaseToken)` apunta el token en `applied_purchases` en la misma escritura y
  rechaza uno ya apuntado (`RejectReason.AlreadyApplied`). `LocalRevocation.ApplyLedger` aplica lo que devuelva `restoreLedger`: vuelve a
  acreditar lo que falte y retira lo anulado (`Transaction.Revoke`: semillas y stock sin bajar de 0, y Sin anuncios), apuntándolo en
  `voided_purchases` [P] para no retirarlo dos veces (la cartera rechaza retirar un token ya retirado, `RejectReason.AlreadyApplied`); en
  orden ordinal de token. Las Canoas infinitas ya corridas no se retiran [P].

## 5. `first_app_version`, `locale` y `country_first`

- `first_app_version`: `Application.version` en la primera ejecución; un perfil anterior a B8 lo deja en null (no se inventa).
- `country_first`: la región de dos letras de la etiqueta del teléfono en la primera ejecución (Q-T3 (a): sin SIM en el cliente), o null.
- `locale`: el idioma de la UI resuelto en cada arranque (Q-B8-4). No se guarda aparte: sale con la siguiente escritura.

## 6. «Eliminar cuenta y datos» (08 §11)

Ajustes › «Eliminar cuenta y datos» (un enlace al final de la lista) → paso 1 (qué se borra y «Las compras no se reembolsan.»;
«Conservar mis datos» o «Continuar») → paso 2 («¿Eliminar todo?»; «Eliminar»). La X y el Atrás de Android cierran sin borrar. «Eliminar»
hace la parte del cliente: borra `profile.json` y `profile.bak` (`IProfileStore.Delete()`, por el lote, que antes cancela el escritor de
segundo plano), `PlayerPrefs` (con el `af_install_id`) y, si todo eso salió bien, `af_main` de la nube (sin Play Games, la nube falsa sin
sesión no hace nada), y recarga la escena
sin el perfil de la sesión: primera ejecución, directo al nivel 1 (08 §2.3). Si el borrado local falla, sigue en el paso 2 con «No se
pudo eliminar» y no se borra nada más. Sonda: `AF_STATE af_account_delete` con `requested`, `confirmed`, `done` o `failed` (08 §8.2).
**Falta, con Functions y App Check:** `deleteAccount` en el servidor, el ID de jugador visible en Ajustes (para la vía web) y el envío de
`af_account_delete` a la analítica.

### 6.1 La espera (prueba del dueño del 2026-10-23, rama `tanda5/borrado-espera`)

«Está tosco, sin mostrar nada si cargando o procesando»: `POST /v1/account/delete` tarda varios segundos. Ahora:

- **Mientras el servidor borra** (`AccountDeleteInFlight`; paso `Deleting` de `DeleteAccountDialog`): «Eliminando tu cuenta y tus datos…»
  con tres puntos de espera (`WaitDots`, una vuelta de 1.200 ms [P]; quietos y encendidos con Reducir movimiento), «Eliminar» y la X
  desactivados, un segundo toque que no pide otro borrado y el Atrás de Android ignorado (`DeleteAccountBusy`; `BackBlocked` da el
  háptico de Atrás bloqueado). TalkBack lo anuncia.
- **Al terminar bien** (paso `Done`): lo local se borra y se ve «Tu cuenta y tus datos se eliminaron.» en una tarjeta sin cabecera ni
  botones durante 1.800 ms [P]; luego la tarjeta se funde y el velo se oscurece en 300 ms [P] (× `anim_reduce_motion_factor` con Reducir
  movimiento) y se recarga la escena como primera ejecución. Mientras, el resto del arranque no avanza (`TickAccount`): nada escribe el
  perfil ni llama al servidor. TalkBack lo anuncia.
- **Al fallar:** «No se pudo eliminar. Inténtalo de nuevo.» y el botón dice «Reintentar» (antes, «Eliminar»); la X sigue cerrando sin
  borrar. TalkBack anuncia el fallo.
- **Otras esperas de red revisadas:**
  - la verificación de una compra en el Mercado y en Aguas estancadas: dice «Confirmando…», como pide 08 §10.3 (antes decía «Pendiente
    de confirmar» también mientras se preguntaba), y en el Mercado ningún precio responde mientras (`IsPurchasing`); si el servidor no
    responde, «Pendiente de confirmar» como antes. TalkBack lo anuncia;
  - la restauración al arrancar (`restoreLedger` y `Resume`): va en segundo plano y no deja nada quieto ni bloquea la UI; lo que acredita
    aparece en el saldo. Sin cambio;
  - el código de borrado en Ajustes (`GetAccountCode`): su línea sale vacía hasta que llega, sin bloquear Ajustes. Sin cambio; si se
    quiere, una línea «Obteniendo el código…» es una tarea pequeña aparte;
  - la pantalla de arranque no espera a la red (se corta al terminar la carga) y no lleva texto (06 §2.1).
- Textos nuevos (`strings.csv`, es-419, en y pt-BR, `borrador`): `delete_account_deleting`, `delete_account_done`, `delete_account_retry`
  e `iap_verifying` (el texto de 08 §10.3). Capturas: `DeleteAccountSnapshotTests` (`[Explicit]`, con gráficos), en
  `Logs/snapshots/<idioma>/delete_account_0N_*.png` al 100 y al 130 % y `Logs/snapshots/es-419/market_verifying.png`.
- Tests: `DeleteAccountWaitTests` (PlayMode, con un servidor falso con retardo: espera, botones, Atrás, TalkBack, confirmación, fundido,
  reintento, Reducir movimiento y encaje al 130 % en los tres idiomas), `BackendF35Tests.Market_WhileTheServerVerifies_SaysConfirming_AndNoPriceAnswers`,
  `PurchaseNotesTests.WhileTheServerAnswers_TheLivePurchaseSaysConfirming` y `WaitDotsTests` (EditMode).

## 7. Preguntas para el dueño (con la recomendada aplicada)

| Id | Pregunta | Opciones | Recomendada |
|---|---|---|---|
| Q-B8-1 | 08 §11 fija `applied_purchases = union(...)`. Con la unión, una compra acreditada en el teléfono que **pierde** el conflicto queda «aplicada» pero su saldo se pierde con la cartera del perdedor, y `restoreLedger` ya no la vuelve a acreditar | (a) unión, como dice 08 §11 · (b) la lista del ganador, y `restoreLedger` vuelve a acreditar lo que falte (coherente con «consumibles del ganador») | **(b)** decidida por el dueño el 2026-10-21 (D-033, Q-B8-1): aplicada en `CloudMerge` y en 08 §11; `voided_purchases` [P] la sigue (del ganador) |
| Q-B8-2 | ¿De qué sale la clave del HMAC? «Derivada del dispositivo» no dice qué pasa con un blob de otro teléfono | (a) de `device_first` (la huella del que creó el perfil), comprobable en cualquier teléfono · (b) del teléfono actual: un blob de otro no se puede comprobar y la regla «gana el coherente» no sirve con la nube | **(a)** [P] |
| Q-B8-3 | El id del que sale la huella | (a) `SystemInfo.deviceUniqueIdentifier` (ANDROID_ID) con sal · (b) un id aleatorio por instalación en `PlayerPrefs` (`af_install_id`; Eliminar cuenta lo borra) | **(b)** [P], cambiada tras la revisión: la misma protección (la clave sale de la huella que va en el perfil) sin identificador del dispositivo ni fila nueva de Data Safety (08 §13.3) |
| Q-B8-4 | ¿Qué es `locale`? | (a) el idioma de la UI resuelto en cada arranque (`es-419`, como el ejemplo) · (b) la etiqueta del teléfono (`es-CO`) | **(a)** [P] |
| Q-B8-5 | ¿Un perfil sin firma o migrado es «coherente»? | (a) no (quitar el `hmac` sería tan fácil como editar) · (b) sí: solo una firma que no cuadra en un perfil sin migrar pierde por integridad | **(b)** [P], cambiada tras la revisión: con (a), una nube firmada con un esquema anterior perdería frente a un local con mucho menos nivel (pérdida de progreso) |
| Q-B8-6 | `daily` con fechas distintas: «`max` por clave» mezclaría contadores de dos días | (a) el bloque del día más reciente · (b) `max` igual | **(a)** [P] |
| Q-B8-7 | Empate total (mismo `max_level` y `economy_ts`) | (a) gana el local · (b) gana la nube | **(a)** [P] |
| Q-B8-8 | Revocar una compra anulada que daba Sin anuncios | (a) se quita el derecho · (b) se conserva | **(a)** [P] (08 §10.1: «retira semillas … o derechos») |
| Q-B8-9 | Orden de `ApplyLedger` cuando hay que acreditar y retirar a la vez | (a) orden ordinal de token, lo que toque a cada uno · (b) primero acreditar y luego retirar | **(a)** [P]: determinista; con (b) se retiraría más |

| Q-B8-10 | ¿Se guarda la sospecha de un perfil editado para la fusión? Hoy se mide (`fraud_flag`) y la primera escritura lo vuelve a firmar: editar sin red, jugar y conectar después gana como coherente | (a) solo medir (como hoy) · (b) un indicador persistente en `integrity` que la fusión trate como no coherente | **(a)** [P] (08 §10.1: sin castigo automático; la economía de semillas es autoritativa en cliente) |
| Q-B8-11 | Con B6 integrado, ¿cómo se fusionan `settings.push` y `stats.last_seen_at` / `last_win_at`? Hoy se quedan los del ganador | (a) `last_seen_at` y `last_win_at` por máximo; `pre_prompt_shown` y `second_prompt_shown` por «o»; `pre_prompt_at` el más antiguo; `granted` el del dispositivo (propuesta de B6) · (b) todo del ganador (como hoy) | **(a)**, **sin aplicar**: llegó con la fusión de B6 tras la revisión de B8; es una línea por campo en `CloudMerge.MergeFields` con su caso en `CloudMergeTests` |

Preguntas de otros bloques que este cierra:
- **B1a-Q1** (D-033, (a)): aplicada en `CloudMerge.MergeDaily`.
- **B1a-Q2** (D-033, (a)): `ProfileStreakStore.WriteTo` marca cambio también cuando solo cambia `streak` (`B8-economy.diff`).
- **Q-S1 de B3** ((a)): `skipped_levels` por unión; `since_last_skip` no se fusiona.

## 8. Lo que queda para el integrador o para los SDK

- Aplicar los parches en orden (§9), con `B8-loc.diff` para los textos (en vez de copiar `B8-strings.csv` a mano).
- Con B6 ya integrado: `CloudMerge` no fusiona `settings.push` ni `stats.last_seen_at` / `last_win_at` (se quedan los del ganador). B6 propone `last_seen_at` y `last_win_at` por máximo y `pre_prompt_shown` / `second_prompt_shown` por «o», con `granted` del dispositivo [P]: pendiente de decidir (Q-B8-11).
- Con Play Games (dueño): un marcador persistente de «borrado pendiente» que se procese antes de `CloudSync.AtStartup` (si el `Delete`
  de la nube falla, el arranque siguiente descargaría la cuenta borrada); la implementación real de `ICloudSave` y las subidas periódicas (cada `save_cloud_interval_levels` niveles,
  tras cada compra y al pasar a segundo plano), con `CloudSync.Upload`; esperar el `Load` asíncrono en el arranque.
- Con Functions: `restoreLedger` → `LocalRevocation.ApplyLedger`; `rewardedSsv` → `RegisterProvisional`/`Confirm`/`ExpireStale`;
  `deleteAccount`; `IServerDate` real con la cabecera `Date`.
- B7: enlazar `RevocationConfig.SecSsvGraceH` con Remote Config (`sec_ssv_grace_h`) y llevar `af_save_sync` y `af_account_delete` a la
  analítica.
- Integrador: valorar que `ProfileSaveBatch.Delete()` deje el lote cerrado y rechace escrituras (hoy se corta `PersistNow` y la escena se
  recarga en el acto; un callback entre «Eliminar» y la recarga podría volver a escribir `profile.json`). `ProfileSaveBatch.cs` está
  reservado.
- Teléfono: comprobar `SHA256`/`HMACSHA256` con IL2CPP y el stripping High (`AF_STATE profile_integrity` = `valid` tras un guardado).
- GDD: anotar en 08 §4.3 `voided_purchases`, `pending_rewards` e `integrity.fraud_flag` [P], y en 08 §11 los 8 casos de §2.

## 9. Parches (en LF, en `docs/dev/traspaso/spark-enganches/`), en este orden

1. **`B8-profile.diff`**: `Profile.cs` y `ProfileJson.cs` (los campos nuevos); añade `ProfileIntegrity.cs`, `SigningProfileStore.cs`,
   `ProfileIdentity.cs`, `CloudMerge.Fields.cs`, `CloudSync.cs` y `Save/Tests/ProfileB8Tests.cs` (con sus .meta).
2. **`B8-economy.diff`** (necesita el 1): `Canoes.cs`, `Wallet.cs`, `Transaction.cs` y `ProfileStreakStore.cs`, y el test existente
   `StreakProfileTests.Store_OnlyStreakChange_LeavesEconomyTs`, que pasa a `..._MarksEconomyTs` (B1a-Q2 (a) invierte lo que comprobaba); añade
   `Economy/LocalRevocation.cs` y `Tests/EditMode/WalletB8Tests.cs` (con sus .meta).
3. **`B8-ui.diff`** (necesita el 1): `SettingsScreen.cs`, `GameBootstrap.cs`, `GameBootstrap.Streak.cs` y `GameBootstrap.Daily.cs`, y una línea de
   `ResumeTests.cs` y de `SyncSaveTests.cs` (el almacén del lote es ahora `SigningProfileStore`: comprueban su `Inner`); añade
   `UI/GameBootstrap.Cloud.cs` y `Tests/PlayMode/AccountAndCloudTests.cs` (con sus .meta).
4. **`B8-strings.csv`**: 9 claves a `strings.csv`. Desde que `LocTablesTests.EveryUiTextKey_Exists_AndEveryStringsRow_IsUsed` exige que
   toda fila la use la UI (y `strings.csv` va en orden ordinal de clave), copiarlas a mano no basta: **`B8-loc.diff`** (necesita el 3)
   las mete en su sitio y registra las claves de `AccountText.Keys` en `LocTablesTests.UiTextKeys`, como B3 y B6. Aplicar `B8-loc.diff`
   en vez de copiar el CSV.

Prueba: `git apply` de 1, 2, 3 y `B8-loc.diff` sobre la rama, `unity-compile`, EditMode (`ProfileB8Tests`, `WalletB8Tests`, `LocTablesTests`) y
PlayMode (`AccountAndCloudTests`, `PseudoLocOverflowTests`).
