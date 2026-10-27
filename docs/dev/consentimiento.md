# Consentimiento y edad (bloque B12 de la tanda 5)

Bloque B12 de `docs/dev/plan-tanda5.md` §4, con las decisiones Q-T3 (a) y Q-T4 (a) de D-033. Rama `spark/t5-b12`. Todo con implementaciones falsas: sin SDK, sin paquetes, sin permisos nuevos y sin red.

**Canon que se implementa**
- 08 §2.3 (secuencia de arranque) y §7 (párrafo «Consentimiento»).
- 05 §7 (filas «LGPD…», «Menores: bandas de edad», «Lei 14.852/2024 art. 17» y «Anunciantes y cierre», D-029).
- 06 §2.1, regla 3.
- 08 §4.3 (`profile.consent`), §8.1 (`consent_profile`), §8.2 (`af_consent` y `af_settings_change`) y §9 (`consent_text_version`, `consent_under13_policy`, `consent_age_signal_source`, `biz_legal_profile_by_country` y `biz_under18_no_personalization`).

---

## 1. Qué hace

### 1.1 Arranque (08 §2.3)

1. Se cortan la pantalla de arranque y el resto de la carga.
2. Si no hay una decisión vigente, se abre la **pantalla de edad neutral**, y después la de **consentimiento**. Va antes del nivel 1 (06 §2.1, regla 3).
3. Hay una decisión vigente cuando `profile.consent` existe, tiene banda, su `text_version` es ≥ `consent_text_version` y su perfil legal es el del país de ahora. Con otro perfil (otra SIM, un viaje) se vuelven a pedir las finalidades con el perfil actual, conservando la banda [P] (Q-B12-15): los flags siempre salen del perfil del país actual.
4. Los flags se aplican antes de `AppFlow.Start`, a:
   - la analítica: `AnalyticsService.SetCollectionEnabled` (08 §2.3: «siempre ANTES de GameFlow.Go»);
   - los anuncios: `ConsentAdService`, el «MAX» sin inicializar hasta la decisión;
   - la tienda: `ConsentStoreService`.
5. **Banda `<13`** (`consent_under13_policy` = `block`):
   - se guarda y se aplica en cualquier arranque posterior;
   - sale la pantalla «Pide a un adulto que instale el juego», sin botones;
   - flags, antes de bloquear: TFCD, sin analítica, sin compras, sin Play Games ni Remote Config personalizado (08 §7);
   - la escena se recarga (como al cambiar de idioma, `RebuildUi`), y el arranque siguiente se para en `BlockedAtBoot` antes de crear los anuncios, la tienda, las pantallas, las sesiones y el audio: no se crea sesión de juego ni se programan avisos locales;
   - si ya estaba guardada, el arranque se para ahí mismo, sin recargar.

### 1.2 Pantalla de edad neutral

- «¿Cuántos años tienes?» y tres botones iguales, en orden: «Menos de 13», «De 13 a 17» y «18 o más».
- Ninguno está marcado, y ningún texto dice qué edad hace falta.
- En el arranque no tiene X, y Atrás no la cierra.

### 1.3 Pantalla de consentimiento (05 §7)

- **Finalidades separadas**:
  - «Anuncios personalizados»;
  - «Analítica para mejorar el juego»;
  - una línea sin interruptor para los informes de fallos: Crashlytics como interés legítimo, 08 §2.3 y pregunta 6 de 08.
- En el arranque, todo apagado al abrir (consentimiento expreso).
- «Rechazar todo» y «Aceptar todo» van uno al lado del otro, con el mismo botón de madera y el mismo tamaño: «Rechazar» al mismo nivel que «Aceptar». Debajo, «Guardar mi elección».
- **Anuncios personalizados** solo se ofrecen con 18+ y fuera de UMP:
  - con 13-17 (`biz_under18_no_personalization`), una línea dice que no son personalizados;
  - sin país conocido, una línea dice que no son personalizados (Q-T3 (a), el tratamiento más estricto; Q-B12-13);
  - con el perfil `ump` (EEE, Reino Unido y Suiza), una línea dice que los decide el aviso de Google, el de UMP que muestra MAX al inicializarse (08 §7 #2). El juego no fuerza `npa`: deciden las cadenas TCF de UMP (`ConsentFlags.AdsByUmp`, `ConsentAdService.UmpDecides`). Desde Ajustes, la línea es un enlace, «Elegir los anuncios en el aviso de Google», que vuelve a abrir el mensaje de UMP para cambiar o retirar el consentimiento (`ShowPrivacyOptions`; con MAX, su formulario de privacidad [NV]).
- **Registro (prueba LGPD)**: `profile.consent` con el perfil legal, `text_version`, la fecha, las dos finalidades y la banda. Se guarda en el acto.

### 1.4 País y perfil legal (Q-T3 (a))

1. País de la SIM: `TelephonyManager.getSimCountryIso()` por el JNI de Unity. Sin permiso: no necesita `READ_PHONE_STATE`. En `AndroidSimCountry`.
2. Si no, la región del teléfono, la de `IDeviceLocale` (`es-CO` → CO; `es-419` no es un país).
3. Si no, ningún país: el perfil es `default` **y** no se ofrecen anuncios personalizados (`npa` sin elección). Ese es «el tratamiento más estricto» de Q-T3 (a) [P] (Q-B12-13).
- El perfil sale de `biz_legal_profile_by_country`:
  - la entrada del país;
  - si no la hay, la de su grupo (`EEE` para los 30 del EEE; `UK` para `GB`);
  - si no, `"*"`.
- Todos los perfiles que no son `ump` ven la misma pantalla propia, con todo apagado al abrir. Un país conocido sin entrada propia (EE. UU., Argentina…) cae en `"*"` = `default` y sí elige los anuncios personalizados.

### 1.5 Flags (`ConsentPolicy`)

| Banda | Analítica | Anuncios | TFCD | Compras | Play Games | RC personalizado |
|---|---|---|---|---|---|---|
| sin decidir | no | `npa`, sin inicializar | no | no | no | no |
| `<13` | no | `npa`, sin inicializar | **sí** | no | no | no |
| `13-17` (con `biz_under18_no_personalization`, el default) | según decisión | `npa` siempre, también en el EEE | no | sí, con doble confirmación | sí | no |
| `13-17` sin `biz_under18_no_personalization` | según decisión | como 18+ (interruptor o UMP) | no | sí, sin doble confirmación | sí | sí |
| `18+` | según decisión | según decisión (sin país: `npa`) | no | sí | sí | sí |
| `18+` con `ump` | según decisión | UMP (sin forzar `npa`) | no | sí | sí | sí |

- `maxAdContentRating` = G en todas las bandas (05 §7, D-029).
- Con `biz_under18_no_personalization` = false, 13-17 se trata como 18+ salvo TFCD, también con UMP.
- Los flags se aplican una vez por decisión: antes de enviar `af_consent` y, si no hubo evento, al cerrar el paso.
- Los flags siempre se calculan con el perfil legal y el país de ahora (`ConsentPolicy.Flags(record, config, perfil, país conocido)`): una decisión de otro perfil cuenta como sin decidir hasta volver a preguntar, y sin país nunca hay anuncios personalizados ni los decide UMP. Play Games, solo con una decisión vigente.
- Con la edad o el consentimiento pendientes, o con el bloqueo, la sesión no cuenta y no se programan avisos locales al pasar a segundo plano (`ConsentHoldsSession`; su gancho, en `GameBootstrap.Sessions.cs`, va en `B12-ui.diff`). La sesión del arranque en frío (`stats.sessions`) sí se cuenta antes de la pantalla de edad [P].
- La sonda `AF_STATE consent` escribe `npa` como lo recibe el proveedor de anuncios y añade `ump`.
- Al subir `consent_text_version` se vuelven a pedir las finalidades, no la edad [P] (Q-B12-2). Mientras tanto, todo sigue apagado.

### 1.6 Compras de 13-17 (Q-T4 (a))

`ConsentStoreService` pone, antes de la hoja de Google Play, una **segunda confirmación en el juego**:
- dice el producto y su precio, tal como los da la tienda (`LocalPrice.ToString`, D-025);
- tiene dos botones: «Cancelar» y «Comprar»;
- «Cancelar» da `Cancelled` sin descontar nada (05 §4.1);
- mientras está abierta, `IsPurchasing` es cierto.

**Autenticación de Google Play.** La pide Play según sus ajustes y los de Family Link: el cliente no la puede forzar [NV, a confirmar con el abogado]. Con la tienda falsa, la hoja «Compra simulada» hace sus veces.

**Sin compras** (`<13` o sin decidir): `StoreUnavailable`, sin abrir nada, e `IsAvailable` = false. La tienda falla cerrada: hasta el primer `Apply`, ninguna compra.

**Nunca se confirma lo imposible.** Antes de la segunda confirmación se pregunta a `StoreLedger.CheckPurchasable` (`Precheck`):
- un producto ya comprado o que no se vende ahora (la Mochila, la Balsa antes del 31…) responde su estado sin pedir confirmación con precio;
- si otra vista tapa la confirmación, se responde «no» y la compra nunca se queda colgada.

### 1.7 Ajustes

Sección nueva tras «Pista por inactividad», con dos enlaces:
- **«Privacidad y datos»**:
  - abre la pantalla de consentimiento con lo guardado marcado y con la X;
  - al guardar: `af_consent` con `step = changed`, `af_settings_change` con `key = consent` y `value = choices`, y los flags nuevos al momento (05 §7: «revocación en Ajustes»).
- **«Corregir mi edad»**:
  - la pantalla neutral otra vez, una sola vez (08 §7 y 05 §7);
  - elegir la misma banda no gasta la corrección;
  - una banda distinta se guarda con `age_band_corrected` [P];
  - pasar a 13-17 apaga los anuncios personalizados;
  - pasar a `<13` bloquea: la escena se recarga y el arranque se para en el bloqueo, sin sesión de juego (como en 1.1);
  - tras usarla, el enlace desaparece.

### 1.8 Analítica

- `af_consent` (08 §8.2) con `profile`, `text_version`, `analytics`, `ads_personalized`, `age_band` y `step`:
  - `shown` al abrir en el arranque;
  - `accepted` (alguna finalidad encendida) o `rejected` (ninguna) al decidir;
  - `changed` desde Ajustes.
- Los flags se aplican antes de enviar el evento: `accepted` ya llega a los sumideros que piden consentimiento (Firebase, cuando exista); `shown` y `rejected`, solo a los locales.
- Con `<13` no hay `af_consent`: no hay analítica.
- Propiedad de usuario `consent_profile` (08 §8.1) con la decisión vigente.
- Sonda de desarrollo:
  - `AF_STATE consent`, con el paso, el perfil, el país y su fuente, la banda y los flags;
  - `AF_LAYOUT` con `"screen":"consent"`.

---

## 2. Archivos

### 2.1 En la rama (nuevos)

- `Assets/_Project/Scripts/Platform/`:
  - `ConsentModel.cs`: `AgeBand`, `LegalProfiles`, `ConsentRecord`, `IConsentStore` y `MemoryConsentStore`;
  - `ConsentConfig.cs`: las 5 claves de 08 §9 con sus defaults y `ProfileFor`;
  - `ConsentPolicy.cs`: `ConsentFlags`, `ConsentPolicy`, `ISimCountry` y `ConsentCountry`;
  - `ConsentFlow.cs`: el recorrido, sin motor.
- `Assets/_Project/Scripts/Ads/ConsentAdService.cs` y `Assets/_Project/Scripts/IAP/ConsentStoreService.cs` (con `IPurchaseConfirmation`).
  - Son carpetas de sdk-integrator, pero el encargo pide aquí los flags hacia anuncios e IAP.
  - Son decoradores sin SDK; no cambian ningún archivo existente.
- `Assets/_Project/Scripts/RemoteConfig/Binding/ConsentConfigBinder.cs`.
- `Assets/_Project/Scripts/UI/`:
  - `ConsentText.cs`, `ConsentScreen.cs`, `AndroidSimCountry.cs`;
  - `SettingsScreen.Consent.cs`, `GameBootstrap.Consent.cs` y `DevProbe.Consent.cs`.
- **Tests**:
  - EditMode: `Assets/_Project/Tests/EditMode/ConsentTests.cs`, `Scripts/Ads/Tests/ConsentAdServiceTests.cs` y `Scripts/IAP/Tests/ConsentStoreTests.cs`;
  - PlayMode: `Assets/_Project/Tests/PlayMode/ConsentScreenTests.cs`.

**Sin los parches**, nada de esto se llama desde el juego: la rama se comporta como la integración.

### 2.2 Parches (`docs/dev/traspaso/spark-enganches/`)

Se aplican en este orden, con `git -c core.autocrlf=false apply`.

1. **`B12-profile.diff`**:
   - `Profile.Consent` (`ConsentRecord?` de `AF.Platform`) y su lectura y escritura en `ProfileJson`:
     - va tras `economy_ts`;
     - la lectura es tolerante: un `consent` roto se vuelve a preguntar, nunca es corrupción del perfil;
     - `age_band_corrected` solo se escribe si es cierto;
   - `AF.Save.ProfileConsentStore`, que guarda en el acto con el almacén durable del lote;
   - `ProfileConsentTests`.
2. **`B12-rc.diff`**:
   - `[RemoteConfigKey]` en `ConsentConfig`;
   - la tabla en `RcConfigBinder.BoundKeys`;
   - `RcConfigs.Consent`;
   - la comprobación en `RcConfigBinderTests`.
3. **`B12-ui.diff`** (necesita el 1, `ProfileConsentStore`, y el 2, `RcConfigs.Consent`):
   - en `GameBootstrap.cs`, las llamadas de `GameBootstrap.Consent.cs`:
     - envolver anuncios y tienda (`WrapConsentAds` y `WrapConsentStore`, por dentro de los adaptadores de analítica de B7: un «Cancelar» de la doble confirmación sale en `af_iap_result`);
     - `BlockedAtBoot` tras crear el arte;
     - `SetUpConsent` tras `ConnectTelemetry`, con las cifras de `_rcConfigs.Consent` (una sola fuente; por eso el 3 necesita también el 2);
     - `StartConsentAtBoot` y `WaitConsentAtBoot` antes de `AppFlow.Start`: sin pantalla que enseñar no hay ningún `yield`;
     - `ConsentBack` en el Atrás;
     - `RefreshConsentRows` al abrir Ajustes;
   - en `GameBootstrap.Telemetry.cs`, solo el comentario del consentimiento;
   - en `GameBootstrap.Sessions.cs`, `ConsentHoldsSession` en `SessionsPaused`: sin avisos locales con la edad pendiente o el bloqueo;
   - en `SettingsScreen.cs`: `BuildConsent`, `LayoutConsent` y `ConsentLayoutIds`;
   - en `PlayModeHelpers.cs`:
     - `GameBootstrap.ConsentEnabledOverride ??= false`, para que los tests de siempre arranquen como antes;
     - `LoadAppWithConsent`;
   - `ConsentBootTests` (PlayMode, la escena real).
4. **`B12-loc.diff`**: las 23 claves en `strings.csv`, en su sitio por orden ordinal, y `ConsentText.AllKeys` en `LocTablesTests.UiTextKeys`.

**Sin parche de `AppFlow`.** El estado «Consent» de 08 §2.2 (`Boot → Consent → Lobby`) es el paso de `ConsentFlow` mientras `AppFlow` sigue en `Boot`:
- en `Boot`, `GameBootstrap.Update` no hace nada y descarta el Atrás, que es lo que pide la pantalla del arranque;
- añadir valores a `AppScreen` habría cambiado `OnAppScreenChanged`, el inicio y el audio sin ganar nada.

**Choques conocidos con B8** (`spark/t5-b8`, aún sin integrar): toca los mismos archivos.
- `SettingsScreen.cs` en `EmitLayout`: la suma de `n`.
- `LocTablesTests.UiTextKeys`: B12 se inserta antes del bloque de la Pororoca y B8 después, así que no deberían pisarse.
- `ProfileJson`: B12 va tras `economy_ts`; B8, en otros puntos.

Si B8 entra antes, la línea de `n` de `EmitLayout` se resuelve a mano: sumar `AccountLayoutCount + ConsentLayoutCount`.

---

## 3. Lo que queda para los SDK (dueño)

- **MAX**: pasar `ChildDirected`, `NonPersonalized` y `MaxAdContentRating` de `ConsentAdService` a la API de su versión [NV]; inicializar en `Apply` con una decisión vigente. El mensaje de UMP lo muestra MAX («Google UMP automation»).
- **Firebase Analytics**: el sumidero real con `RequiresConsent` = true; `firebase_analytics_collection_enabled=false` en el manifiesto hasta el consentimiento (08 §15.3).
- **Play Games y Remote Config personalizado**: leer `ConsentFlags.PlayGamesAllowed` y `RcPersonalization` cuando lleguen.
- **Play Age Signals** (`consent_age_signal_source` = `self_declared+play_age_signals`; T-L3, D-057 y su añadido del 2026-10-27; `tanda6/age-signals`): la parte de C# está hecha y la del teléfono espera a la dependencia.
  - **Sin motor (`AF.Platform/AgeSignals.cs`):** `AgeSignal` (formato del puente `adult`, `range:<inferior>[:<superior>]`, `none`, `error`), `IAgeSignals` con `FakeAgeSignals` y `AgeSignalPolicy`. Bandas: 0-12 → bloqueo como `<13`; 13-15 y 16-17 → 13-17; sin señal o con error → la autodeclarada. Con las dos, **la más estricta** (un 18+ verificado no relaja un 13-17 declarado [P]).
  - **Flujo y flags:** `ConsentFlow.ApplyAgeSignal`, `SignalBand` y `EffectiveBand`; `ConsentPolicy.Flags(…, signalBand)` y `ConsentFlags.BandFromSignal`. La señal **no se guarda** en `profile.consent` (el registro conserva la autodeclarada); el bloqueo por señal se recuerda solo en el proceso, para que la escena recargada arranque bloqueada.
  - **Arranque (`GameBootstrap.AgeSignals.cs`):** se pide una vez por proceso al montar el consentimiento (solo con la segunda fuente) y se recoge cada fotograma; si llega tarde, se vuelven a aplicar los flags y, con 0-12, `BlockSession`. No se espera a la señal antes de jugar (pregunta abierta en el informe de la rama).
  - **T-L4:** `af_consent.age_band` = `ConsentReport.AnalyticsAgeBand`: la autodeclarada, y nada si la señal la corrigió.
  - **En el teléfono (`AF.UI/AndroidAgeSignals.cs`):** JNI a `com.amazonfill.agesignals.AfAgeSignals.request(activity, listener)`. **Pendiente:** el puente Java y la dependencia Maven de Play Age Signals API 0.0.4 (la coordenada no está en el repositorio con fuente). Sin el puente, la llamada da `error` y manda la autodeclarada.
  - El default de 08 §9 sigue en `self_declared` hasta que entre el puente.

---

## 4. Preguntas para el dueño (Q-B12-n)

Se avanzó con la recomendada, marcada [P] en el código.

| Id | Pregunta | Opciones | Recomendada |
|---|---|---|---|
| Q-B12-1 | Forma de la pantalla de edad neutral | (a) tres botones iguales por banda; (b) año de nacimiento con una rueda sin valor inicial, que se clasifica en bandas | **(a)**: cabe en los ≤ 10 s de 06 §2.1 y no pide un dato más fino del necesario. Que lo valide el abogado (neutralidad para Google Play) |
| Q-B12-2 | Al subir `consent_text_version`, ¿se vuelve a pedir la edad? | (a) solo las finalidades, y la banda guardada se conserva; (b) la edad y las finalidades, como el pseudocódigo literal de 08 §2.3 | **(a)**: repetir la edad abriría una segunda corrección, y 08 §7 permite una |
| Q-B12-3 | ¿El bloqueo `<13` es definitivo en el dispositivo? | (a) sí: no se vuelve a preguntar ni con textos nuevos y no hay salida desde la pantalla (solo reinstalar); (b) un enlace «Me equivoqué» que gasta la corrección de edad | **(a)**: 08 §7 («en cualquier arranque posterior»). Con (b), un menor lo usaría |
| Q-B12-4 | Diagnóstico (Crashlytics) en la pantalla | (a) línea informativa sin interruptor (interés legítimo, 08 §2.3); (b) un tercer interruptor | **(a)**, pendiente de la pregunta 6 de 08 al abogado. Con (b), hace falta el campo `consent.diagnostics` en 08 §4.3 |
| Q-B12-5 | Estado de los textos legales en `strings.csv`: el contrato 32 no tiene un estado «pendiente del abogado» | (a) `borrador`, con la nota «pendiente del abogado»; (b) añadir un estado `pendiente_legal` al contrato 32 (cambia 08 §4.7 y `LocReleaseGate`) | **(a)**: no se inventa un valor. Si el dueño quiere bloquear el release por ellos, (b) |
| Q-B12-6 | ¿«Doble confirmación» es una pantalla más o dos? | (a) el toque en «Comprar» del juego + una confirmación con el precio, y después Google Play; (b) dos confirmaciones en el juego | **(a)**, a confirmar con el abogado junto con Q-T4 |
| Q-B12-7 | Con 13-17, ¿se ofrece la analítica? | (a) sí, como finalidad que se puede aceptar (sin audiencias ni anuncios personalizados); (b) apagada siempre | **(a)**: 05 §7 solo prohíbe el perfilado publicitario. A confirmar con el abogado (ECA Digital) |
| Q-B12-8 | `profile.consent` en la nube (08 §11) | (a) es del dispositivo: la fusión conserva el del local, y un teléfono nuevo vuelve a preguntar; (b) viaja con el perfil | **(a)**: 08 §7, «la banda declarada vive solo en el dispositivo». Toca `CloudMerge` de B8: una línea por campo |
| Q-B12-9 | Perfil `ump`: ¿la analítica también por UMP? | (a) la analítica en la pantalla propia y los anuncios por UMP; (b) todo por UMP, con finalidades propias en AdMob › Privacy & messaging | **(a)**, hasta configurar UMP (dueño) |
| Q-B12-10 | Nombre del campo nuevo | `consent.age_band_corrected` (bool) [P] | Anotarlo en 08 §4.3 |
| Q-B12-11 | `af_settings_change.value` con `key = consent` | (a) `choices` o `age`; (b) los valores nuevos | **(a)** [P] |
| Q-B12-12 | El encargo dice que `INTERNET` no está aprobado, pero D-036 (2026-10-20) lo aprobó | — | Sin efecto en B12: no usa red. Corregir el encargo o D-036 |
| Q-B12-13 | ¿Qué es «el tratamiento más estricto» de Q-T3 (a) sin país? | (a) perfil `default` + `npa` sin elección (la analítica se puede aceptar); (b) solo el perfil `default`, con la misma pantalla que un país conocido; (c) tratarlo como el EEE (UMP) | **(a)**, aplicada: un teléfono sin SIM ni región puede estar en el EEE, y sin UMP no hay base para personalizar. Con (c), MAX enseñaría UMP fuera del EEE |
| Q-B12-14 | La UI de la doble confirmación y de las finalidades duplica la fila marcable de Ajustes (`SettingsScreen.OptionRow`, privada) | (a) extraer `OptionRow` a una clase interna común de AF.UI al integrar (toca `SettingsScreen.cs`); (b) dejarlo | **(a)**, para el integrador: la rama no toca `SettingsScreen.cs` |
| Q-B12-15 | Si cambia el perfil legal del país (otra SIM, un viaje), ¿se vuelve a preguntar? | (a) sí, las finalidades con el perfil de ahora y la banda guardada; (b) no: vale el perfil de la primera decisión | **(a)**, aplicada: una decisión del EEE (UMP) no puede valer en Brasil, donde hace falta la pantalla propia (LGPD). Riesgo: un teléfono que pierde la SIM a ratos volvería a preguntar |
| Q-B12-16 | Capturas de referencia de los textos nuevos (`.claude/rules/localizacion.md`) | (a) las saca el integrador con `SnapshotKit` (es-419 y en-XA al 130 %), con una prueba de capturas para la edad, el consentimiento, el bloqueo y la doble confirmación; (b) a mano en el teléfono | **(a)**: en el Spark, las pruebas de capturas fallan en batchmode sin GPU (`RenderTexture.Create failed`). El `context` de cada clave lo dice |

---

## 5. Hallazgos para el GDD

- **08 §4.3**: anotar `consent.age_band_corrected` [P] y que `consent` no viaja en la fusión de 08 §11 (Q-B12-8).
- **08 §2.3**: el pseudocódigo pide de nuevo la edad con cada `consent_text_version` (Q-B12-2) y bloquea dentro del `if` de volver a preguntar; la regla de 08 §7 («en cualquier arranque posterior») exige además comprobar el bloqueo fuera de él.
- **05 §7**, fila «GDPR / UMP»: dice «UMP SDK del plugin GMA», y 08 §7 #2 lo sustituyó por la automatización de UMP de MAX (sin plugin GMA desde el 2026-10-06).
- **05 §7**, fila «Lei 14.852/2024»: dice «PIN opcional de compras», y Q-T4 (a) lo sustituye por la doble confirmación + Google Play.
- **08 §2.2**: `AF.Ads` y `AF.Analytics` dependen de «Consent», que no tiene fila. En el código, el consentimiento vive en `AF.Platform` (`ConsentPolicy`, `ConsentFlow`), como pedía el plan B12 (`S/Platform/Consent*.cs`). Proponer la fila o cambiar «Consent» por «Platform».

---

## 6. Sugerencias de la revisión que quedan abiertas

La 4.ª revisión dio APROBADO. Estas sugerencias no bloquean; se pueden aplicar al integrar.

**Al integrar (2026-10-21, D-033, añadido de B12):** aplicadas la 1 (con su test, `ConsentBootTests.ToDecideAgain_HoldsTheSession_AndBackgroundSchedulesNothing`) y la 7 (`Scripts/UI/OptionRow.cs`); las capturas de Q-B12-16, en `ConsentSnapshotTests`. Las demás siguen abiertas.

1. **`ConsentHoldsSession`**: hoy solo retiene la sesión en los pasos Edad, Finalidades y Bloqueo.
   - Durante el segundo de la pantalla de arranque de quien debe volver a decidir (textos nuevos u otro perfil legal), el flujo está en `None`: pasar a segundo plano en ese instante programaría avisos.
   - Arreglo propuesto: `ConsentBlocked || (ConsentEnabled && flow != null && !flow.Flags.Decided)`.
   - Falta además una prueba que llame a `SessionsPaused(true)` con la pantalla de edad abierta y compruebe que no se programa nada.
2. **`ConsentPolicy.Flags`**: el perfil actual y el país son parámetros opcionales con valores permisivos. Conviene hacerlos obligatorios antes de que los lean los adaptadores de Play Games o de Remote Config personalizado.
3. **Una sola fuente para «decide UMP»**: `ConsentPolicy.AdsByUmp(perfil, banda, config, país conocido)`, usada por `Flags` y por `ConsentFlow.AdsByUmp`. Además, unir las ramas 13-17 y 18+ de `Flags`.
4. **`biz_legal_profile_by_country`**: con las claves en mayúsculas, «br» y «BR» chocan en silencio. Hay que anotar el duplicado como error.
5. **`RcConfigs.Consent`**: pasarlo por el constructor, como `ClockSecurity`, en lugar de un setter `internal`.
6. **El `context` de `consent_purpose_ads`** dice «solo con 18+ fuera de UMP». También sale con 13-17 si `biz_under18_no_personalization` está apagado, y nunca sale sin país.
7. **Q-B12-14**: extraer `OptionRow`.
8. **`PersistNow`**: escribe «idioma» también cuando `RebuildUi` recarga por el bloqueo.
