# Play Games 2.3.0: guardado en la nube detrás de `ICloudSave` (tanda 5, D-042)

Rama `tanda5/sdk-pgs`, desde `964b64fb`. Lo aprobó el dueño en **D-042** (2026-10-23): «Play Games plugin for Unity 2.3.0 (PGS v2), con inicio de sesión silencioso y Saved Games». Respuestas del dueño a las preguntas de la integración (con la recomendada, como añadido de D-042):

- `play-services-nearby` 18.5.0, que trae el plugin, **no entra**;
- el paquete va como `.tgz` en `Packages/playgames/`, en Git LFS, sin el EDM4U del paquete;
- el tope del blob se queda en los 800 KB de 08 §11;
- `PlayGamesInitProvider`, como viene, salvo que haga falta quitarlo (§6);
- el arranque asíncrono, el borrado pendiente y el reintento de #3379, como se proponían;
- el vínculo de Firebase Auth con `PlayGamesAuthProvider` queda fuera (§10).

Canon:
- 08 §2.3 (arranque: «PGS en segundo plano»; con `<13`, `PlayGames.Disable()`);
- 08 §7 #5, §11 («Nube Android 1.0», «Conflicto», «Eliminación de cuenta»), §13.1 #16 y §13.3;
- D-033 (B8 y el añadido del 2026-10-23: con `<13` no arranca nada);
- `docs/dev/nube-integridad.md` (la fusión de B8, que no cambia).

---

## 1. Qué hace

| Capa | Archivo | Qué decide |
|---|---|---|
| Arranque y sesión de juego | `Scripts/UI/GameBootstrap.CloudSession.cs` (nuevo), con una llamada en `GameBootstrap.cs`, `.Cloud.cs`, `.Account.cs` y `.Consent.cs` | Cuándo arranca la nube (edad), cuánto espera el arranque, qué pasa si la fusión llega tarde, cuándo se sube |
| Tras cada compra | `Scripts/UI/CloudUploadStoreService.cs` (nuevo), bajo el envoltorio de la analítica | Una subida tras cada compra acreditada |
| Marcas persistentes | `Scripts/UI/PlayerPrefsCloudFlags.cs` (teléfono) y `MemoryCloudSaveFlags` (editor y tests) | Borrado pendiente y subida pendiente (§5) |
| Sesión de nube | `Scripts/Save/CloudSession.cs` (nuevo, sin motor) | Sesión silenciosa → borrado pendiente → fusión → subidas. Solo sube tras fusionar |
| Borrado pendiente | `Scripts/Save/CloudErase.cs` (nuevo, sin motor) | Antes de fusionar, comprueba que `af_main` quedó vacío tras borrar la cuenta |
| Fusión | `CloudSync.AtStartup` y `CloudMerge` (B8) | Sin cambios de reglas. Dos añadidos: la subida pendiente de #3379 y el conflicto de Play (§4) |
| Nube real | `Scripts/Save/PlayGamesCloudSave.cs` (nuevo, sin motor) | Sesión, snapshot vacío = no hay, tope de 800 KB, conflicto, #3379, borrado, cierre |
| Puerto | `Scripts/Save/ISavedGamesPort.cs` (nuevo) | Lo mínimo de Saved Games, sin tipos del plugin |
| Puerto real | `Scripts/Save/PlayGames/PlayGamesSavedGamesPort.cs` (asmdef nuevo `AF.Save.PlayGames`, con `Google.Play.Games`) | El plugin. Solo `UNITY_ANDROID && !UNITY_EDITOR` y con `APP_ID` |

En el editor, en los tests y en un build sin `APP_ID` (hoy todos: D-034), `PlayGamesSavedGamesPort.TryCreate()` devuelve null y la nube es `FakeCloudSave.WithoutSession()`: el juego se comporta como antes.

### 1.1 Cuándo arranca la nube

- **Con `<13`**, nunca: ni el arranque con la banda guardada (que se para antes, B12), ni tras elegir `<13` en la pantalla de edad, ni tras corregir la edad en Ajustes (`StopCloudForBlock`).
- **Con la banda 13-17 o 18+ ya guardada**, en el arranque, justo tras leer el perfil y antes de crear la cartera. El arranque espera hasta **2.500 ms [P]** (`CloudBootWaitMs`) con la pantalla de arranque a la vista; si la nube responde antes, el juego empieza ya con el perfil fusionado. La nube falsa responde en el acto: el arranque no gasta fotogramas de más.
- **Sin edad declarada** (primer arranque), tras la pantalla de edad (`FinishConsentAtBoot` → `StartCloudAfterConsent`, con `ConsentFlags.PlayGamesAllowed`). Es el caso de reinstalar o cambiar de teléfono: la partida vuelve de la nube.
- **Inicio de sesión:** solo el silencioso de PGS v2 (`PlayGamesPlatform.Instance.Authenticate`, que usa `isAuthenticated`). No hay botón: el canon no lo pide (08 §2.3 y §11). Si el jugador no usa Play Games en el teléfono, no hay sesión y no pasa nada.

### 1.2 Si la nube llega tarde

Pasa si el arranque dejó de esperar o en el primer arranque. Si la fusión cambia el perfil (gana la nube o aporta campos), `TickCloud` hace lo mismo que el cambio de idioma:
- guarda el fusionado, firmado;
- recarga la escena al inicio con él (`RebuildUi(persist: false)`).

Espera si hay una compra en curso, un borrado de cuenta o la pantalla de consentimiento. La sesión de nube (`CloudSession`) pasa a la escena siguiente con la recarga, ya fusionada.

### 1.3 Subidas (08 §11)

- cada `save_cloud_interval_levels` victorias (Remote Config, 3, entre 1 y 5): `LevelWon` del bus;
- tras cada compra acreditada (`CloudUploadStoreService`);
- al pasar a segundo plano (`OnApplicationPause(true)`).

**Solo tras haber fusionado en esta sesión** (`CloudSessionState.Synced`): subir sin fusionar pisaría una nube con más progreso. Si la fusión no llegó (sin red al arrancar, o Play no respondió), al volver de segundo plano se reintenta (`RetryCloud`). Una subida en vuelo no se duplica: se sube otra vez al terminar, con lo último.

---

## 2. El paquete

- **Fuente oficial:** la release de GitHub `v2.3.0` de `playgameservices/play-games-plugin-for-unity`, que solo publica un `.unitypackage`; tampoco hay `.tgz` en `current-build/`.
  - URL: https://github.com/playgameservices/play-games-plugin-for-unity/releases/download/v2.3.0/GooglePlayGamesPlugin-2.3.0.unitypackage
  - sha256: `1b0ecc5e2fd5437e1c26863f63367f5c983452afc4eeae8a1e5dcaa07b9e7518` (3.417.111 bytes; coincide con el `digest` del asset en GitHub).
- **Lo versionado:** `Packages/playgames/com.google.play.games-2.3.0.tgz` (Git LFS, regla nueva en `.gitattributes`), con `"com.google.play.games": "file:playgames/com.google.play.games-2.3.0.tgz"` en `Packages/manifest.json`.
  - sha256 del `.tgz`: `cf78ba41d27a9ee1fb5cdef6699dd4739a159921000e155937b7efc558e974ce` (2.988.803 bytes).
  - Contenido: la carpeta `Assets/GooglePlayGames/com.google.play.games/` del `.unitypackage` **tal cual**, con sus `.meta` (los GUID de Google), bajo `package/`. Es un paquete UPM (`package.json` 2.3.0).
- **Lo que no entra del `.unitypackage`:**
  - `Assets/ExternalDependencyManager/` (EDM4U **1.2.182**): el proyecto ya tiene EDM4U 1.2.189 de Firebase, y el `package.json` del plugin pide 1.2.182 o superior;
  - `Assets/Plugins/Android/GooglePlayGamesManifest.androidlib/project.properties`: lo crea el Setup con el manifiesto (§8).
- **Rarezas de Google que quedan tal cual** (para que la procedencia se pueda auditar):
  - `current-build/GooglePlayGamesPlugin-2.2.0.unitypackage` (2,8 MB), que Unity no importa desde `Packages/`;
  - `Editor/GooglePlayGamesPlugin_v2.2.1.txt`;
  - `PluginVersion.cs` dice 2.1.0.

### 2.1 Cómo reproducir el `.tgz` (Git Bash, GNU tar 1.35)

```bash
D=$(mktemp -d); cd "$D"
curl -sSL -o p.unitypackage https://github.com/playgameservices/play-games-plugin-for-unity/releases/download/v2.3.0/GooglePlayGamesPlugin-2.3.0.unitypackage
sha256sum p.unitypackage           # 1b0ecc5e...7518
mkdir x && tar -xzf p.unitypackage -C x
P="Assets/GooglePlayGames/com.google.play.games/"
for d in x/*/; do p=$(head -1 "$d/pathname" | tr -d '\r'); case "$p" in "$P"*)
  t="pkg/package/${p#$P}"
  if [ -f "$d/asset" ]; then mkdir -p "$(dirname "$t")"; cp "$d/asset" "$t"; else mkdir -p "$t"; fi
  [ -f "$d/asset.meta" ] && cp "$d/asset.meta" "$t.meta";; esac; done
cd pkg && tar --sort=name --mtime='2026-09-29 00:00:00Z' --owner=0 --group=0 --numeric-owner \
  --mode='u=rwX,go=rX' --format=gnu -cf - package | gzip -n -9 > com.google.play.games-2.3.0.tgz
sha256sum com.google.play.games-2.3.0.tgz   # cf78ba41...74ce
```

---

## 3. Dependencias Android

Las declara el plugin en `Editor/GooglePlayGamesPluginDependencies.xml`, y EDM4U las escribe en `Assets/Plugins/Android/mainTemplate.gradle` al construir:

| Dependencia | Versión | Entra | Notas |
|---|---|---|---|
| `com.google.android.gms:play-services-games-v2` | **22.1.0** | **Sí**: es la dependencia obligatoria del plugin aprobado (D-042) | Sus dependencias Maven (`play-services-base` 18.5.0, `play-services-basement` 18.9.0 y `play-services-tasks` 18.2.0) ya entraban por Firebase, en versiones iguales o mayores (`base` 18.10.1). Su manifiesto **no declara permisos**. Añade `<queries>` (`com.google.android.gms` y `com.android.vending`), `GamesResolutionActivity`, `PlayGamesAppShortcutsActivity` (exportada) y `PlayGamesInitProvider` (§6) |
| `com.google.android.gms:play-services-nearby` | 18.5.0 | **No** (añadido de D-042) | Nearby Connections, que el juego no usa. Su manifiesto trae `ACCESS_NETWORK_STATE` (ya aprobado, D-037) y un servicio **exportado** de Exposure Notifications (`com.google.android.gms.nearby.exposurenotification.WakeUpService`) |
| `gpgs-plugin-support.aar` (dentro del paquete) | la del plugin 2.3.0 | Sí | El puente Java del plugin (`com.google.games.bridge`). Sus tres clases proxy de Nearby solo se cargan si se usa Nearby; sin minify (`AndroidMinifyRelease: 0`), su ausencia no rompe el build |

**La exclusión de Nearby** va en un bloque aislado de `mainTemplate.gradle`, fuera de la zona que reescribe EDM4U: quita la dependencia declarada (`withDependencies`) y cualquier llegada transitiva (`exclude`). Lo vigila `ci/check-manifest.ps1`: el apartado 6 bis falla si algún nombre del manifiesto fusionado empieza por `com.google.android.gms.nearby.`, con dos casos nuevos en su autotest.

Si algún día se activa minify, hará falta `-dontwarn com.google.android.gms.nearby.**` y las reglas de `Proguard/games.txt` del paquete.

---

## 4. Saved Games

- **Un archivo:** `af_main` (`CloudSaveNames.Snapshot`), sin portada ni descripción.
- **Tope:** 800 KB (`CloudSaveNames.MaxBlobBytes`, 08 §11). El perfil apunta a menos de 100 KB (08 §4.3). Uno mayor no se envía (`TooLarge`).
  - Pregunta Q-PGS-1 (§11): 08 §7 #5 dice «3 MB/partida, 800 KB de portada». Se queda en 800 KB, como dice 08 §11, sin tocar el GDD.
- **Contenido:** `CloudMerge.SharedJson`, el perfil sin lo que es solo del dispositivo. Hasta ahora era `purchase_queue`; desde D-042 son también `consent` y `account_code`:
  - **`consent`:** 08 §7 dice que «la banda declarada vive solo en el dispositivo». La fusión ya se quedaba con la del dispositivo (Q-B12-8), pero el blob la llevaba y, con Play Games, subiría a Google;
  - **`account_code`:** el código de borrado es del dispositivo (D-035, añadido de F3-5).

  La firma (`ProfileIntegrity.Payload`) sigue cubriendo lo mismo que se comparte, así que el blob se comprueba igual. Un perfil firmado antes de D-042 (que firmaba también esos dos campos) sigue valiendo (`LegacyPayload`) hasta su próxima escritura.
- **Snapshot vacío = no hay:** abrir crea el archivo si falta (`OpenWithManualConflictResolution`, `ReadCacheOrNetwork`). Uno sin bytes es `NotFound` y se sube el local.
- **Conflicto de Play** (dos versiones del snapshot, por ejemplo dos teléfonos sin red): resolución **manual**. `CloudSync.ResolveSnapshotConflict` lee las dos como perfiles y aplica `CloudMerge.Resolve` (las reglas de 08 §11; nunca `UseLongestPlaytime`). Si una no se puede leer, queda la otra; el resultado va firmado. Después, la fusión normal con el local.
- **Errores:**
  - sin sesión (`AuthError`): se pierde la sesión;
  - sin red o error de Play: `Failed`. Se sigue con el local y nada se sube hasta reintentar.

## 5. Bug #3379 y las marcas persistentes

#3379 (abierto en el plugin): tras guardar sin conexión, Saved Games puede no volver a subir el snapshot. **Mitigación:** una subida que falla, o que se hace sin red (`Application.internetReachability`), deja `ResyncPending`. La sincronización siguiente con red (al arrancar, o al volver de segundo plano con `RetryCloud`) vuelve a subir el perfil aunque la fusión no lo pida (`ICloudResync`). La marca se limpia con una subida hecha con red. No se puede comprobar que Play lo subió de verdad: lo que se hace es volver a hacer un `commit` con red.

Marcas (`ICloudSaveFlags`), fuera del perfil porque tienen que sobrevivir al borrado de la cuenta y no viajan en el blob:

| Marca | En el teléfono (`PlayerPrefs`) | Quién la pone | Quién la quita |
|---|---|---|---|
| Borrado pendiente | `af_cloud_delete_pending` = 1 | «Eliminar cuenta y datos», **tras** borrar `PlayerPrefs` | `CloudErase` cuando `af_main` está vacío |
| Subida pendiente | `af_cloud_resync_pending` = 1 | Una subida fallida o sin red | Una subida hecha con red, o el borrado |

En el editor y en los tests, en memoria (`MemoryCloudSaveFlags`; `GameBootstrap.CloudFlagsOverride`).

## 6. Edad, borrado y `PlayGamesInitProvider`

- **`<13`:** no se crea `PlayGamesPlatform`, no se inicia sesión y no se lee ni se escribe la nube (§1.1). **`PlayGamesInitProvider` se quita** con `tools:node="remove"` en `Assets/Plugins/Android/LauncherManifest.xml`: inicia PGS v2 al arrancar el proceso, y PGS v2 intenta iniciar sesión solo («a sign-in attempt will be made automatically when your game's application started», según la documentación del plugin), antes de saber la edad. Sin el proveedor, el plugin llama a `PlayGamesSdk.initialize` al crear su cliente (`AndroidClient`), y `AF.Save.PlayGames` solo lo crea con 13-17 o 18+ y con `APP_ID`. Quitar un proveedor no añade nada. **Hay que probarlo en el teléfono** cuando haya `APP_ID` (§9).
- **Borrado de la cuenta (08 §11; D-033 B8):** tras borrar lo local, `CloudSession.EraseForAccountDelete`:
  1. pone la marca de borrado pendiente;
  2. pide el borrado de `af_main` si hay sesión;
  3. **cierra** la sesión de nube (`Closed`): ninguna subida más, como el lote del perfil.

  El plugin no avisa del resultado de `delete`, así que el arranque siguiente lo comprueba antes de fusionar (`CloudErase`):
  - snapshot vacío: limpia la marca y sigue;
  - con datos: lo vuelve a borrar y esta sesión no fusiona ni sube nada;
  - sin red: la marca sigue.

  Así, una cuenta borrada sin red no vuelve de la nube.
- **Borrado por la web:** no puede borrar Saved Games, que está en la cuenta de Google del jugador. Lo dicen la política de privacidad y 08 §13.3; lo revisa el abogado (§11, Q-PGS-3).

## 7. Privacidad (08 §13.3)

| Dato | Dónde | Cuándo | Con `<13` |
|---|---|---|---|
| ID de jugador de Play Games | Lo gestiona Google; el juego no lo lee, no lo guarda ni lo manda a su servidor | Con sesión de Play Games | Nunca |
| Partida (`af_main`): progreso, cartera, ajustes y lo demás del perfil, sin `consent`, `account_code` ni `purchase_queue` | Saved Games, en la cuenta de Google del jugador | Al fusionar y en las subidas (§1.3) | Nunca |

- Finalidad: funcionalidad (recuperar la partida al cambiar de teléfono o reinstalar).
- No se comparte con terceros: procesa Google.
- No necesita consentimiento: no es analítica ni publicidad. Lo que la habilita es la banda de edad (13-17 o 18+).
- Sin logros, marcadores, eventos ni Nearby.

Actualizado:
- 08 §13.3 (la fila de Play Games Services);
- la política de privacidad de la web en es-419, en y pt-BR: la fila de la tabla de datos, la de plazos, menores, proveedores y borrado; «Google Play Juegos» sale de «Lo que todavía no está en el juego»;
- la web regenerada con `npm run web:build`.

## 8. Permisos, manifiesto, peso y archivos que genera el plugin

**Medido** en `dev.apk` (`BuildDev`) y en `rehearsal.aab` (`BuildRelease -afRehearsal`) de esta rama:

- **Ningún permiso nuevo.** `check-manifest`, en modo `dev` (con `-RequireBilling` en el ensayo), da OK con la misma lista blanca de antes:
  - `INTERNET` y `ACCESS_NETWORK_STATE`;
  - `VIBRATE` y `WAKE_LOCK`;
  - `BIND_GET_INSTALL_REFERRER_SERVICE` y `POST_NOTIFICATIONS`;
  - `BILLING`;
  - el `DYNAMIC_RECEIVER_NOT_EXPORTED_PERMISSION` propio.
- **El manifiesto fusionado gana:**
  - `<queries>` de `com.google.android.gms` y `com.android.vending`;
  - `GamesResolutionActivity` y `PlayGamesAppShortcutsActivity` (exportada, de Play Games);
  - `GenericResolutionActivity` y `NativeBridgeActivity` (del puente del plugin);
  - la meta-data `com.google.android.gms.games.version`, `com.google.android.gms.games.APP_ID` (vacía, sin `APP_ID`) y `com.google.android.gms.games.unityVersion`.
- **No entran:**
  - `PlayGamesInitProvider`;
  - ningún componente `com.google.android.gms.nearby.*` (`check-manifest` 6 bis);
  - ninguna clase de Nearby en los `.dex` (`check-release-classes -ForbidPackage com.google.android.gms.nearby`: 0).
- **16 KB:** sin `.so` nuevos (18 bibliotecas de 64 bits alineadas en los dos artefactos).
- **Peso:** `rehearsal.aab` 54,33 MB (el anterior de referencia, `rehearsal-before.aab` del 2026-10-21, 53,49 MB) y `dev.apk` 70,51 MB (antes 69,41 MB): **≈ +0,9-1,1 MB**, dentro de los 80 MB.
- **Archivos que escribe el plugin al abrir el Editor** (`GPGSUpgrader`, `[InitializeOnLoad]`, con Android como plataforma activa), aunque no haya configuración:
  - `Assets/Plugins/Android/GooglePlayGamesManifest.androidlib/AndroidManifest.xml`, con el `APP_ID` vacío;
  - `Assets/GooglePlayGames/Resources/PlayGamesSettings.asset`, con su `.meta`;
  - `ProjectSettings/GooglePlayGameSettings.txt`.

  **Se commitean tal como los escribe el plugin**, para que todos los builds (también el del CI) sean iguales, y el Setup del dueño (§9) los sustituye en un PR propio. No se editan a mano.
- **EDM4U** escribe, al construir:
  - en `mainTemplate.gradle` (zona del resolvedor), las líneas de `play-services-games-v2` 22.1.0 y `play-services-nearby` 18.5.0; la segunda la quita el bloque de exclusión;
  - en `settingsTemplate.gradle`, el repositorio `https://maven.google.com/`;
  - en `ProjectSettings/AndroidResolverDependencies.xml`, las dos dependencias.

  Se commitean como los dejó el resolvedor, igual que con D-040.

## 9. Pasos del dueño

Hoy no hacen falta para construir: sin ellos, el build va sin Play Games (nube falsa sin sesión). Cuando exista la app en Play Console (tras el dictamen de marca, D-034):

1. **Play Console › Play Games Services › Configuración:** crear el proyecto de juegos de la app con el `applicationId` definitivo (`com.<estudio>.tintalu`), activar **Saved Games** y crear la credencial Android (OAuth) con el SHA-1 de la clave de firma de Play App Signing y el de la clave de subida (08 §13.1 #16). No hace falta Web Client ID (§10).
2. **Recursos:** en «Configuración › Obtener recursos», copiar el XML de Android (el `games-ids.xml`, con `app_id`).
3. **En el Editor** (con el proyecto abierto y una sola instancia), `Window › Google Play Games › Setup › Android setup`: pegar el XML, sin Web Client ID, y «Setup». El Setup escribe:
   - `Assets/Plugins/Android/GooglePlayGamesManifest.androidlib/AndroidManifest.xml` y `project.properties` (con el `APP_ID`);
   - `Assets/GooglePlayGames/Resources/PlayGamesSettings.asset` (un ScriptableObject que crea Unity: no se edita a mano);
   - sus ajustes de proyecto.

   Son los archivos que ya están commiteados con el `APP_ID` vacío (§8): el Setup los sustituye. Commitear el cambio, con sus `.meta`. El `APP_ID` no es secreto (va en el manifiesto de la app), pero se commitea en un PR propio y revisado.
4. **Probar en el teléfono** (build de desarrollo con una cuenta de prueba de Play Games):
   - inicio de sesión silencioso sin ventana;
   - primera subida;
   - reinstalar y recuperar la partida;
   - conflicto entre dos teléfonos;
   - modo avión (#3379: guardar sin red y volver con red);
   - «Eliminar cuenta y datos» con y sin red;
   - `<13` sin ninguna llamada a Play Games (logcat);
   - que la app no falla al arrancar sin `PlayGamesInitProvider`.
5. **Data Safety:** declarar la fila de Play Games Services de 08 §13.3.

## 10. Fuera de alcance

- **`LinkWithCredentialAsync(PlayGamesAuthProvider)`** (08 §11, fila «Identidad»): necesita el Web Client ID y el código de servidor de Play Games (`RequestServerSideAccess`). Queda como pendiente documentado, con su propia tarea y aprobación.
- Logros, marcadores, eventos y Nearby.
- Un botón para iniciar sesión a mano: el canon no lo pide.

## 11. Preguntas (con la recomendada aplicada)

| Id | Pregunta | Opciones | Recomendada |
|---|---|---|---|
| Q-PGS-1 | 08 §11 fija un tope de 800 KB («sin imagen de portada»), pero 08 §7 #5 da «3 MB/partida, 800 KB de portada»: 800 KB parece el límite de la portada | (a) 800 KB, como 08 §11 (el perfil pesa menos de 100 KB) · (b) 3 MB y corregir 08 §11 | **(a)**, decidida por el dueño en la integración; el GDD no se toca |
| Q-PGS-2 | `consent` y `account_code` subían en el blob (`SharedJson`), aunque 08 §7 dice que la banda vive solo en el dispositivo | (a) quitarlos del blob y de lo firmado, aceptando la firma anterior · (b) dejarlos | **(a)**, aplicada (§4) |
| Q-PGS-3 | El borrado por la web no puede borrar Saved Games | (a) decirlo en la política y en Data Safety (hecho) · (b) además, borrar `af_main` desde la app en el siguiente arranque si el servidor dice que la cuenta se borró | **(a)**, aplicada; (b) necesita un contrato nuevo en `infra/contracts/v1` |
| Q-PGS-4 | ¿Cuánto espera el arranque a Play Games? | (a) 2.500 ms [P] con la pantalla de arranque, y si no, la fusión llega tarde y recarga · (b) no esperar | **(a)**, aplicada (`CloudBootWaitMs`) |
| Q-PGS-5 | Quitar `PlayGamesInitProvider` (§6) | (a) quitarlo, por la edad · (b) dejarlo | **(a)**, aplicada; hay que comprobarlo en el teléfono |

## 12. Pruebas y verificación

- **EditMode** `AF.Save.Tests.PlayGamesCloudTests`, con un puerto falso de Saved Games y la nube falsa diferida:
  - sin `APP_ID` no se llama al SDK;
  - sesión silenciosa;
  - snapshot vacío;
  - tope del blob;
  - `AuthError`;
  - sin red;
  - #3379;
  - conflicto de Play;
  - borrado y cierre;
  - `CloudErase`;
  - `CloudSession`: arranque, subidas cada 3 victorias, nunca antes de fusionar, reintento, borrado pendiente y borrado de la cuenta;
  - el blob sin `consent` ni `account_code`, y la firma anterior.
- **PlayMode** `PlayGamesCloudBootTests` (escena `Game` real):
  - el arranque que espera y empieza con la nube;
  - la fusión tardía que recarga;
  - sin red al arrancar;
  - `<13` en el primer arranque;
  - 18+ en el primer arranque, que recupera la partida;
  - el borrado de la cuenta con la marca pendiente y sin escrituras.
- **Los comandos y sus resultados** van en la descripción del PR.
