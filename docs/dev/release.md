# Release firmado de Android (paso s32)

Fuentes: `docs/setup/plan-instalacion.md` (s31-s32), GDD 08 §12.1 (etapas 3-5) y §13.1 #1 y #3, D-034 y D-041; reglas en `.claude/rules/ci.md` y `CLAUDE.md` (reglas duras 5 y 6).

El workflow `.github/workflows/release.yml` construye el AAB de release **firmado con la clave de subida**, lo pasa por las compuertas de release y lo deja como artefacto de la ejecución. **No lo sube a Google Play**: la subida la hace el dueño a mano en Play Console, y la primera espera al dictamen de marca (08 §13.1 #1).

## 1. Qué crea el dueño en GitHub

Los agentes no crean secretos ni variables (`gh secret` está bloqueado). Todo se crea en **Settings** del repositorio `Nipko/fill-game`.

**Primero, el Environment `release`** (Settings › Environments › New environment):

- **Deployment branches and tags:** *Selected branches*, solo `main`.
- **Required reviewers** (opcional, recomendado): tú. Cada ejecución espera tu aprobación antes de recibir los secretos.

Los dos jobs de `release.yml` declaran `environment: release`. Es la barrera que no depende del contenido del workflow: los secretos del Environment solo llegan a jobs que lo declaran, en `main` y, con revisor, tras tu aprobación. Un workflow modificado en otra rama no puede leerlos.

| Dónde | Tipo | Nombre | Valor | Cuándo |
|---|---|---|---|---|
| Repositorio (Secrets and variables › Actions) | Secreto | `UNITY_EMAIL` | Correo de la cuenta Unity dedicada al CI, sin 2FA (ya lo usa `ci.yml`) | Ya previsto en s30 |
| Repositorio | Secreto | `UNITY_PASSWORD` | Su contraseña (ya la usa `ci.yml`) | Ya previsto en s30 |
| **Environment `release`** | Secreto | `ANDROID_KEYSTORE_BASE64` | El keystore de subida en Base64 (§2) | Al crear la clave de subida |
| Environment `release` | Secreto | `ANDROID_KEYSTORE_PASS` | Contraseña del keystore | Ídem |
| Environment `release` | Secreto | `ANDROID_KEYALIAS_NAME` | Alias de la clave de subida (recomendado `tintalu-upload`) | Ídem |
| Environment `release` | Secreto | `ANDROID_KEYALIAS_PASS` | Contraseña de la clave (con PKCS12, la misma que la del keystore) | Ídem |
| Environment `release` | Secreto | `GOOGLE_SERVICES_JSON_BASE64` | El `google-services.json` de prod (proyecto `tintalu`, app `com.parallext.tintalu`) en Base64 (§2 bis) | Ya puede crearse (añadido de D-035 del 2026-10-27) |
| Repositorio (pestaña *Variables*; no es secreta) | Variable | `AF_RELEASE_APPLICATION_ID` | **`com.parallext.tintalu`** (D-047, confirmado por el dueño el 2026-10-25) | Ya puede crearse (D-047 retiró la espera al dictamen) |

Los cuatro `ANDROID_*` y `GOOGLE_SERVICES_JSON_BASE64` **no** se crean como secretos del repositorio, solo en el Environment (siete secretos en total con los dos de Unity). Si GitHub no tiene el Environment `release`, lo crea vacío en la primera ejecución; el job `preflight` fallaría entonces con 5 (faltan los secretos), sin llegar a Unity.

Sin `AF_RELEASE_APPLICATION_ID`, con `com.amazonfill.dev` o con un ID que lleve «amazon», el workflow falla en el primer job, sin pedir la licencia de Unity. Mientras la variable no exista, el job `build-android` de `ci.yml` sigue construyendo un ensayo con la identidad de prueba. Cuando exista, ese job construye el release con el ID definitivo, firmado con la clave de depuración, y solo `release.yml` lo firma con la de subida.

## 2. Clave de subida (la genera el dueño, en su equipo)

La clave de subida debe valer 25 años o más y caducar después del 22-10-2033 (08 §12.1; setup/b §6). Con `keytool` (Temurin 17 está en el PATH):

```
keytool -genkeypair -v -storetype PKCS12 -keystore tintalu-upload.p12 -alias tintalu-upload -keyalg RSA -keysize 2048 -validity 10000 -dname "CN=Parallext LLC, O=Parallext LLC, C=US"
```

- `keytool` pide la contraseña. No la escribas en la línea de comandos.
- Guarda el archivo y la contraseña en tu gestor de secretos, con una copia de seguridad fuera de este equipo.
- **Nunca** guardes el archivo dentro del repositorio, aunque `.gitignore` ya ignora `*.p12`, `*.jks`, `*.keystore`, `*.pfx` y `*.pepk`.
- Si pierdes la clave de subida, Play App Signing permite pedir un restablecimiento. Si pierdes la de firma de la app, no hay arreglo: por eso la custodia Google (§3).
- Datos del `-dname`: los de Parallext, LLC (D-041). Si el abogado o la cuenta de Play cambian el titular, ajústalos antes de generar la clave.

Para el secreto `ANDROID_KEYSTORE_BASE64`, en PowerShell:

```
[Convert]::ToBase64String([IO.File]::ReadAllBytes("C:\ruta\tintalu-upload.p12")) | Set-Clipboard
```

Pega el valor en el secreto y vacía el portapapeles. No lo guardes en un archivo.

## 2 bis. `google-services.json` de prod (secreto `GOOGLE_SERVICES_JSON_BASE64`)

Sin él, el AAB de release sale sin Firebase: el juego usa `UnavailableBackendClient` (la tienda no vende) y no hay Analytics, Crashlytics, Remote Config ni Auth (`docs/dev/firebase-cliente.md` §3). Es el archivo que descargaste de la consola de Firebase, proyecto `tintalu` (número 459140125724), app Android `com.parallext.tintalu` (ID `1:459140125724:android:810c83a65d0ff611f07bd9`). Crea el secreto en **Settings › Environments › release › Environment secrets › Add secret**, con el nombre `GOOGLE_SERVICES_JSON_BASE64` y como valor el archivo en Base64, en una sola línea:

- Git Bash: `base64 -w0 /c/ruta/google-services.json | clip` (sin `-w0`, `base64` parte la salida en líneas de 76 caracteres).
- PowerShell: `[Convert]::ToBase64String([IO.File]::ReadAllBytes("C:\ruta\google-services.json")) | Set-Clipboard`

Pega el valor en el secreto y vacía el portapapeles; no lo guardes en un archivo. Si regeneras el `google-services.json` (por ejemplo, al añadir el SHA de Play App Signing a la app de Firebase), vuelve a crear el secreto: el archivo cambia aunque el ID de app siga igual.

Qué hace el workflow con él:

- **`preflight`:** `ci/release-preflight.sh` lo decodifica en memoria y comprueba, sin imprimir el contenido, que es JSON con `project_info.project_number` y que tiene un cliente Android con `package_name` igual a `AF_RELEASE_APPLICATION_ID` cuyo `mobilesdk_app_id` es de ese proyecto (código 7 si no; 8 si falta python). Solo imprime el número de proyecto y el ID de app, que son públicos, y los pasa al job `build`.
- **`build`:** lo escribe con `umask 077` en `Assets/_LocalSecrets/google-services.json` (la ruta de `docs/dev/firebase-cliente.md` §2; el paso falla si `git check-ignore` deja de cubrirla) justo antes del paso de Unity. El editor de Firebase genera de él `Assets/Plugins/Android/FirebaseApp.androidlib` con el `applicationId` que fija `BuildRelease`. Tras el build se comprueba que esa carpeta existe y `ci/check-firebase-config.sh` lee del AAB `google_app_id` y `gcm_defaultSenderId` (con `bundletool dump resources`) y los compara con el ID de app y el número de proyecto del preflight. Nunca lee `google_api_key`. Al acabar, con `if: always()`, se borran el archivo, `FirebaseApp.androidlib` y `StreamingAssets/google-services-desktop.json` (con sus `.meta`).
- No cae en ninguna caché: la de `Library` solo se restaura y la de LFS solo guarda `.git/lfs`. El artefacto solo lleva el `.aab`.
- **Dentro del AAB sí va:** el editor de Firebase copia el JSON entero a `StreamingAssets/google-services-desktop.json` y Unity mete StreamingAssets en el AAB (`base/assets/`), como en cualquier juego de Unity con Firebase. Su `api_key` ya va también en los recursos de toda app Android con Firebase y Google la trata como identificador público (la protegen App Check y las restricciones de la clave en Google Cloud), así que no es un secreto que se filtre; lo secreto de verdad (cuentas de servicio, keystore) nunca pasa por aquí.

## 3. Play App Signing (08 §13.1 #3)

1. Al crear la app en Play Console (ya sin esperar al dictamen, D-047), deja activado **Play App Signing** con la clave de firma de la app **generada por Google**: es la opción recomendada.
2. La primera subida de un AAB firmado con tu clave de subida registra esa clave como clave de subida.
3. En **Integridad de la app › Firma de apps** aparecen dos certificados:
   - el de firma de la app, que va a Firebase (SHA-1 y SHA-256 de la app Android de prod), a Play Games Services (credencial OAuth) y a App Check (08 §13.1 #16 y #18);
   - el de subida, que debe coincidir con el SHA-256 que imprime el job `preflight` («SHA-256 del certificado de subida»).

## 4. Lanzar un release

1. **Actions › Release firmado › Run workflow**, rama `main`; con otra rama, el workflow falla.
2. `versionName`: `MAYOR.MENOR.PARCHE`, por ejemplo `1.0.0`.
3. `versionCode`: vacío aplica la regla `MAYOR*10000 + MENOR*100 + PARCHE`; `1.0.0` da 10000. Para reconstruir la misma versión hace falta un código mayor: Play rechaza un `versionCode` ya subido. Con MENOR o PARCHE por encima de 99, indícalo a mano.
4. Al terminar, descarga el artefacto `android-aab-release-<versionName>-<versionCode>`, que se guarda 30 días, y súbelo tú a Play Console.

### Qué comprueba

| Job | Paso | Falla si |
|---|---|---|
| `preflight` (sin Unity, ~1 min) | `ci/release-preflight.sh` | rama distinta de `main` (3); `applicationId` ausente, provisional, con «amazon» o mal formado (4); falta algún secreto (5); versión inválida (6) |
| | `ci/check-release-signature.sh --keystore-only` | el keystore no se abre con la contraseña o no tiene el alias (4) |
| `build` (Unity, `unity-personal-seat`) | `BuildRelease.BuildAndroid -afRequireSigning` | firma incompleta o keystore inexistente (código 9 del método); identidad (7); textos (6), etc. |
| | `ci/check-release-signature.sh` | AAB sin firma, con la clave de depuración o con otra clave |
| | Identidad y versión del manifiesto | `package`, `versionCode` o `versionName` distintos de los pedidos |
| `preflight` | `ci/release-preflight.sh` (google-services.json) | `GOOGLE_SERVICES_JSON_BASE64` falta (5), no es Base64 o JSON, no tiene `project_number`, no tiene el cliente de `AF_RELEASE_APPLICATION_ID` o su ID de app es de otro proyecto (7); falta python (8) |
| `build` | `FirebaseApp.androidlib` existe | el editor de Firebase no generó la configuración |
| | `ci/check-firebase-config.sh` | el AAB no lleva `google_app_id` y `gcm_defaultSenderId`, o no son los del preflight (1); bundletool no lee el AAB (3) |
| | `size-gate -RequireBundletool`, `check-16kb` con `CHECK16_REQUIRE_ZIPALIGN=1`, `check-manifest -Mode release -RequireBilling`, `check-release-classes`, `check-no-unity-iap` | las compuertas de release de 08 §12.1 etapa 4 |

## 5. Cómo se protege la clave

- Los secretos solo entran como `${{ secrets.X }}` en `env:` o `with:`, nunca dentro de un `run:` ni con `echo`. Los scripts nunca imprimen sus valores, y GitHub los enmascara en el log.
- El keystore se decodifica con `umask 077` en `$RUNNER_TEMP`, fuera del repositorio. En el build va a `$GITHUB_WORKSPACE/.af-signing/` (ignorado por git, fuera de `Assets/` y de toda caché), que el contenedor de game-ci ve en `/github/workspace/.af-signing/`: game-ci v6 no monta `$RUNNER_TEMP/_github_home` como `/root` (primera ejecución del 2026-10-27, código 9). Se borra al acabar con `if: always()`. No se usa la entrada `androidKeystoreBase64` de game-ci, porque escribiría el keystore dentro del proyecto.
- `BuildRelease` lee la firma de las variables `ANDROID_KEYSTORE_NAME`, `ANDROID_KEYSTORE_PASS`, `ANDROID_KEYALIAS_NAME` y `ANDROID_KEYALIAS_PASS`; los argumentos de game-ci son el respaldo. La pone en los Player Settings solo durante el build y después la restaura. Unity no guarda las contraseñas en `ProjectSettings/`, y `ProjectSetup.Validate` sigue exigiendo `useCustomKeystore = false`.
- **La caché de `Library` solo se restaura en el release y nunca se guarda.** Unity escribe la configuración de firma, contraseñas incluidas, en el proyecto Gradle de `Library/Bee`, y una caché guardada la podría leer cualquier workflow del repositorio.
- El AAB firmado es un artefacto privado de la ejecución. Nada se publica.

## 6. Pendiente de comprobar en la primera ejecución

- **Resuelto el 2026-10-27:** game-ci/unity-builder v6.0.0 **no** monta `$RUNNER_TEMP/_github_home` como `/root` (la primera ejecución salió con 9, «no existe el keystore»); el keystore va ahora en `.af-signing/` dentro del workspace. Sí pasa `ANDROID_KEYSTORE_*` al contenedor. Si no lo hace, `BuildRelease` sale con 9 («no existe el keystore»): falla cerrado y no produce un AAB sin firmar.
- **Pendiente desde el 2026-10-27 (`google-services.json`):** que el editor de Firebase regenere `FirebaseApp.androidlib` dentro del contenedor de game-ci con el `applicationId` de release. Lo hace con `generate_xml_from_google_services_json.py`, que necesita Python en la imagen `unityci/editor`, y si solo lo generara con el ID del proyecto (`com.amazonfill.dev`, que no está en el archivo de prod) fallaría. En los dos casos el AAB sale sin `google_app_id` y la compuerta de Firebase falla cerrada: no se publica un release sin Firebase.
- El tiempo del job `build`, unos 30-60 min de los 2.000 al mes, como `build-android`.

## 7. Primera ejecución completa (2026-10-27)

- El build de Unity terminó bien: `[AF.BuildRelease] OK`, AAB de 59,18 MB, firmado con la clave de subida y con `LocReleaseGate` en OK. Aun así, game-ci v6 salió con 1 al devolver la licencia Personal («Serial number unavailable for ULF return»), y su `validateBuild` cortó el job antes de las compuertas.
- Desde entonces, el paso de game-ci lleva `continue-on-error: true`. El veredicto lo dan el paso «Un solo AAB» y las compuertas siguientes, que un AAB ausente o a medias no pasa.

