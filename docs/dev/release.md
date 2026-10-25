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
| Repositorio (pestaña *Variables*; no es secreta) | Variable | `AF_RELEASE_APPLICATION_ID` | **`com.parallext.tintalu`** (D-047, confirmado por el dueño el 2026-10-25) | Ya puede crearse (D-047 retiró la espera al dictamen) |

Los cuatro `ANDROID_*` **no** se crean como secretos del repositorio, solo en el Environment. Si GitHub no tiene el Environment `release`, lo crea vacío en la primera ejecución; el job `preflight` fallaría entonces con 5 (faltan los secretos), sin llegar a Unity.

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
| | `size-gate -RequireBundletool`, `check-16kb` con `CHECK16_REQUIRE_ZIPALIGN=1`, `check-manifest -Mode release -RequireBilling`, `check-release-classes`, `check-no-unity-iap` | las compuertas de release de 08 §12.1 etapa 4 |

## 5. Cómo se protege la clave

- Los secretos solo entran como `${{ secrets.X }}` en `env:` o `with:`, nunca dentro de un `run:` ni con `echo`. Los scripts nunca imprimen sus valores, y GitHub los enmascara en el log.
- El keystore se decodifica con `umask 077` en `$RUNNER_TEMP`, fuera del repositorio. En el build va a `$RUNNER_TEMP/_github_home`, que game-ci monta como `/root` en su contenedor. Se borra al acabar con `if: always()`. No se usa la entrada `androidKeystoreBase64` de game-ci, porque escribiría el keystore dentro del proyecto.
- `BuildRelease` lee la firma de las variables `ANDROID_KEYSTORE_NAME`, `ANDROID_KEYSTORE_PASS`, `ANDROID_KEYALIAS_NAME` y `ANDROID_KEYALIAS_PASS`; los argumentos de game-ci son el respaldo. La pone en los Player Settings solo durante el build y después la restaura. Unity no guarda las contraseñas en `ProjectSettings/`, y `ProjectSetup.Validate` sigue exigiendo `useCustomKeystore = false`.
- **La caché de `Library` solo se restaura en el release y nunca se guarda.** Unity escribe la configuración de firma, contraseñas incluidas, en el proyecto Gradle de `Library/Bee`, y una caché guardada la podría leer cualquier workflow del repositorio.
- El AAB firmado es un artefacto privado de la ejecución. Nada se publica.

## 6. Pendiente de comprobar en la primera ejecución

- Que game-ci/unity-builder v6.0.0 monta `$RUNNER_TEMP/_github_home` como `/root`, como en las versiones anteriores, y pasa `ANDROID_KEYSTORE_*` al contenedor. Si no lo hace, `BuildRelease` sale con 9 («no existe el keystore»): falla cerrado y no produce un AAB sin firmar.
- El tiempo del job `build`, unos 30-60 min de los 2.000 al mes, como `build-android`.
