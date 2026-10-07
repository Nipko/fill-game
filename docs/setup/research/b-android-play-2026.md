# Requisitos de Google Play 2026 y versiones compatibles para Aquazonia (Unity, Android y SDK)

Fecha: 2026-10-06 · Proyecto Amazon Fill (Aquazonia) · Tema B de la investigación de arranque.
Convención: **[V]** verificada en la fuente enlazada (URL en la misma fila o en §Fuentes), **[E]** estimación o inferencia razonada, **[NV]** no se pudo verificar. Todas las versiones y changesets de Unity salen de la API pública de releases de Unity y de las notas de versión oficiales, no de memoria.

---

## Respuesta corta

- **Target API.** Desde el **31-08-2026**, las apps nuevas y las actualizaciones deben apuntar a **Android 16 (API 36)**. Se puede pedir una prórroga hasta el **01-11-2026**. En noviembre de 2026 la exigencia sigue siendo API 36 [V]. Google ya anunció que la **API 37** será obligatoria en **agosto de 2027** [V]. El día exacto no está publicado; por el patrón anual será probablemente el 31-08-2027 [E].
- **Versión de Unity elegida: Unity 6.3 LTS, parche `6000.3.25f1`** (24-09-2026, changeset `e1dba0a9aba4`) [V]. Tiene soporte LTS hasta diciembre de 2027 [V]. Admite target 36 [V], incluye "SDK Platform 37" desde el `6000.3.22f1` [V] y trae **AGP 9.0.0** [V]. Unity 6.0 LTS queda descartada porque su soporte termina en octubre de 2026 [V]. Unity 6.6 tampoco conviene: es una versión de actualización que solo se soporta hasta la siguiente [V].
- **Páginas de 16 KB.** Todos los parches de Unity 6.3 generan `libunity.so` y `libil2cpp.so` alineadas (Unity lo confirma desde el `6000.3.0f1`). En 6.0 esto empieza en el `6000.0.38f1` [V]. El riesgo real está en las `.so` de terceros, sobre todo los adaptadores de mediación [V], así que hace falta una comprobación automática en CI.
- **Play Billing.** Desde el 31-08-2026 las apps nuevas y las actualizaciones deben usar **PBL 8 o superior** (prórroga hasta el 01-11-2026) [V]. Unity IAP **5.0.0** fue la primera versión con PBL 8.0.0, la 5.2.0 trae 8.3.0 y la **5.4.0+ trae PBL 9.0.0** [V]. La **5.4.4** figura como versión publicada para Unity 6000.3 [V] y PBL 9 sigue aceptado hasta agosto de 2028 [V].
- **SDK de anuncios, analítica y juegos (versiones de hoy):**
  - AppLovin MAX Unity **8.6.6** (15-09-2026) [V]
  - Firebase Unity **13.17.0** (17-09-2026) [V]
  - Play Games plugin **2.3.0** (29-09-2026; incluye Saved Games) [V]
  - Addressables **2.10.3** con Addressables for Android **1.1.0**, ambos publicados para 6000.3 [V]
  - Google Mobile Ads Unity **11.5.0** [V]: **no hace falta instalarlo**. MAX ya integra AdMob como adaptador y puede gestionar Google UMP él solo [V].
- **EDM4U está deprecado.** La versión 1.2.190 (29-09-2026) lo marca así y el repositorio se archiva el **26-10-2026** [V]. Lo sustituye `com.unity.external-dependency-manager` **2.1.0**, ya publicado para 6000.3 [V]. Mientras un SDK (Firebase 13.17 trae EDM4U 1.2.189) siga incluyéndolo, hay que mantenerlo [V].
- **Máquina Windows ARM64.** El `6000.3.25f1` tiene instalador nativo ARM64 con el módulo **Android Build Support** (OpenJDK 17.0.18+8, NDK r27c, plataformas 34-37) [V]. Las bibliotecas de escritorio de Firebase solo se compilan para **x64** [V], y Unity no da soporte a binarios de terceros sin versión nativa para Windows on Arm [V]. Por eso **Firebase no cargará en el Play Mode del editor ARM64** [E]. A la build de Android no le afecta [E].
- **Cuenta de organización.** El requisito de 12 testers durante 14 días es solo para cuentas **personales** creadas después del 13-11-2023 [V]. La prueba interna admite 100 testers y se puede empezar sin terminar la ficha [V]. Mientras la app esté solo en la pista interna no se pide Data Safety [V]. Play App Signing es obligatorio con AAB [V]. La clave de subida debe valer 25 años o más y caducar después del 22-10-2033 [V].
- **El NDK de Android Studio no sirve para Unity.** En la máquina hay instalado el NDK `27.1.12297006` (r27b) [V]. Unity 6.x exige **r27c `27.2.12479018`** [V]. Hay que instalar el SDK, el NDK y el JDK que gestiona Unity desde su módulo, y no reutilizar los de Android Studio.

---

## 1. Target API de Google Play

| Fecha | Exigencia para apps nuevas y actualizaciones (teléfono y tableta) | Estado | Fuente |
|---|---|---|---|
| Hoy (06-10-2026) | targetSdk **36** (Android 16) | [V] | Play Console Help 11926878 |
| Prórroga | Se puede pedir hasta el **01-11-2026** | [V] | ídem |
| Noviembre de 2026 | Sigue siendo **36**; no hay anuncio de cambio antes de 2027 | [V] (la página no menciona otro hito) | ídem |
| Apps ya publicadas | Deben apuntar a 35 o más para seguir visibles a usuarios nuevos en Android más reciente | [V] | ídem |
| Agosto de 2027 | targetSdk **37** (Android 17) obligatorio para apps nuevas y actualizaciones | [V] (blog de Android Developers, feb-2026) | android-developers.googleblog.com/2026/02 |
| Día exacto de 2027 | 31-08-2027, con prórroga probable hasta el 01-11-2027 | [E] (patrón anual) | — |
| Android 17 estable | Publicado el 16-06-2026 | [V] (fuente secundaria: Wikipedia) | en.wikipedia.org/wiki/Android_17 |

**Cambios de Android 16 (target 36) que afectan a un juego Unity:**

| Cambio | Impacto en Aquazonia | Estado |
|---|---|---|
| En pantallas de 600 dp o más se ignoran `screenOrientation`, `resizableActivity` y las relaciones de aspecto. **Los juegos (`android:appCategory="game"`) están exentos**, y la exención sigue con API 37 | Comprobar que el manifiesto fusionado lleva `appCategory="game"`. Unity 6 lo activa por defecto en proyectos nuevos (`PlayerSettings.Android.appCategory = Game`) | [V] behavior-changes-16 + blog 2026/02 · [V] Unity ScriptReference appCategory |
| Ya no se puede desactivar edge-to-edge | La UI debe respetar el *safe area* (barras del sistema y recortes de pantalla) | [V] |
| Retroceso predictivo activo por defecto: no llega `KEYCODE_BACK` y no se llama a `onBackPressed` | Configurar `PlayerSettings.Android.predictiveBackSupport` y probar el botón "atrás" en los modales | [V] behavior-changes-16 · [V] Unity ScriptReference |

---

## 2. Unity 6.x: soporte, target 36/37, Gradle y AGP

### 2.1 Ramas de Unity 6 a 06-10-2026

| Rama | Tipo | Último parche (fecha, changeset) | Soporte hasta | Target 36 | Target 37 | Android Build Support en Windows ARM64 |
|---|---|---|---|---|---|---|
| 6.0 (6000.0) | LTS | 6000.0.84f1 (16-09-2026, `78ab6fc243d5`) [V] | "hasta octubre de 2026" (endoflife.date indica el 16-10-2026) [V] | Sí [V] | No verificado [NV] | Sí [V] |
| **6.3 (6000.3)** | **LTS** | **6000.3.25f1 (24-09-2026, `e1dba0a9aba4`)** [V] | **diciembre de 2027** [V] | **Sí** [V] | En el enum (`AndroidApiLevel37`) y con "SDK Platform 37" desde el 6000.3.22f1 [V]. La página de requisitos solo garantiza 35 y 36 [V]. Hay un informe de fallo de build con 37 en el 6000.3.23f1 [V] | **Sí** [V] |
| 6.5 (6000.5) | Actualización | 6000.5.11f1 (02-09-2026) [V] | Sin soporte desde el 31-08-2026 [V] | Sí | — | Sí [V] |
| 6.6 (6000.6) | Actualización | 6000.6.4f1 (01-10-2026, `12bfff696524`) [V] | Hasta que salga la siguiente versión [V] | Sí | Sí: el 6000.6.3f1 corrige "Failed to find target android-37" [V] | Sí [V] |
| 6.7 (6000.7) | Beta | 6000.7.0b3 (05-10-2026) [V] | Se espera que sea la próxima LTS [NV] | — | — | Sí [V] |

### 2.2 Gradle y AGP dentro de Unity 6.3

| Parches 6000.3 | Gradle | AGP | Fuente |
|---|---|---|---|
| 6000.3.1f1 – 6000.3.16f1 | 8.13 | 8.10.0 | [V] tabla de compatibilidad de Unity 6.3 |
| 6000.3.17f1 – 6000.3.25f1 | 9.1.0 según la tabla. **Las notas del 6000.3.25f1 dicen que se actualizó de 9.1.0 a 9.3.1** | 9.0.0 | [V] tabla + [V] notas 6000.3.17f1 ("Upgraded Gradle to 9.1.0 and AGP to 9.0.0") y 6000.3.25f1 |
| 6000.3.26f1+ (aún no publicado a 06-10-2026) | 9.3.1 | 9.1.1 | [V] tabla. [V] La API de releases no lista el 26f1 |

La tabla y las notas no coinciden en la versión de Gradle del 25f1. Hay que comprobarla el primer día en *Preferences › External Tools* o en el log de la build. En cualquier caso, AGP 9.0.0 supera de sobra el mínimo de 8.5.1 que exige la alineación de 16 KB [V].

**Qué cambia con AGP 9 en Unity** (hilo oficial de Unity) [V]:
- Las plantillas Gradle personalizadas ya no admiten `targetSdkVersion`, `minSdkVersion`, `lintOptions` ni `packagingOptions`.
- Cada `.androidlib` necesita un `namespace` propio.
- `proguard-android.txt` pasa a ser `proguard-android-optimize.txt`.
- En el 6000.3.21f1 se suavizó la aplicación estricta de reglas de R8 de AGP 9 [V].

### 2.3 Dependencias Android de Unity 6.3 frente a lo instalado en la máquina

| Componente | Lo que exige o trae Unity 6000.3.25f1 | Lo que hay instalado (comprobado hoy, solo lectura) | Acción |
|---|---|---|---|
| NDK | **r27c (27.2.12479018)** [V] | 27.1.12297006 (r27b), desde Android Studio [V] | Usar el NDK del módulo de Unity, **no** el de Android Studio |
| JDK | OpenJDK 17. El módulo trae **17.0.18+8** [V] | Temurin 17.0.19 (JAVA_HOME) [V] | Usar el JDK del módulo para tener la misma versión que el CI. Temurin sirve para `keytool` |
| Build-tools | 36.0.0 [V] | 35.0.0, 36.0.0, 36.1.0, 37.0.0 [V] | Lo pone el módulo |
| Plataformas SDK | El módulo ARM64 incluye 34, 35, 36 y 37 [V] | android-36, android-36.1 [V] | Lo pone el módulo |
| Platform-tools | 36.0.0 en el 25f1; 37.0.1 desde el 26f1 [V] | 37.0.0 [V] | Indiferente |
| Command-line tools | 16.0 [V] | Ninguna [V] | Lo pone el módulo |
| CMake | 3.22.1 [V] | 3.22.1 [V] | — |
| IDs de módulo (API de Unity, Windows ARM64) | `android`, `android-sdk-ndk-tools`, `android-open-jdk-17.0.18+8`, `android-ndk-r27c` [V] | — | Útiles para instalar por CLI |

---

## 3. Páginas de memoria de 16 KB

| Aspecto | Dato | Estado |
|---|---|---|
| Requisito de Play | Las apps que apuntan a API 35 o más deben admitir 16 KB en dispositivos de 64 bits. Para apps y builds nuevas rige desde el 01-11-2025. La guía oficial dice hoy que **desde el 01-02-2027** no se podrán publicar actualizaciones sin 16 KB | [V] developer.android.com/guide/practices/page-sizes. [V] Unity Discussions (cita de Google para el 01-11-2025) |
| Para Aquazonia | La app es nueva y apunta a 36, así que **la primera subida ya tiene que estar alineada** | [E] (aplicación directa del requisito) |
| Unity 6.0 | Desde el **6000.0.38f1** | [V] hilo oficial de Unity |
| Unity 6.1 o superior | Todas las versiones | [V] ídem |
| Unity 6.3 | Todos los parches: "6000.3.0f1 is fully 16KB supported" (personal de Unity), incluidas `libil2cpp.so` y `libunity.so` | [V] |
| Unity 2022.3 (referencia) | Desde el 2022.3.56f1 | [V] |
| Plugins nativos de terceros | Basta **una sola** `.so` sin alinear para que Play rechace el AAB, aunque Unity y AGP estén al día. Ejemplos: plugins antiguos de vibración y la `libquack.so` de HyprMX en MAX | [V] Unity Discussions 1707589. [V] AppLovin issue #521 |
| Firebase Unity | Soporte de 16 KB desde la 12.6.0; estable desde la **12.10.0** | [V] respuesta del mantenedor en firebase-unity-sdk #1335 |
| Cómo comprobarlo | APK Analyzer (columna *Alignment*) o `zipalign -c -P 16 -v 4 <apk>` (debe acabar en "Verification successful"). `check_elf_alignment.sh` funciona en Linux y macOS (sirve en el runner Ubuntu del CI) | [V] page-sizes |
| NDK | r27 o anterior necesita las opciones de enlazador `-Wl,-z,max-page-size=16384`; r28 y posteriores alinean por defecto. Solo importa si algún día compilamos `.so` propias; Aquazonia es C# puro | [V] page-sizes · [E] aplicabilidad |

---

## 4. Play Billing Library y Unity In-App Purchasing

| Versión de PBL | Plazo para apps nuevas y actualizaciones | Prórroga | Fuente |
|---|---|---|---|
| 7 | Sin validez desde el **31-08-2026** (por eso hoy se exige PBL 8 o superior) | 01-11-2026 | [V] deprecation-faq |
| 8 | Válida hasta el 31-08-2027 | 01-11-2027 | [V] |
| 9 | Válida hasta el 31-08-2028 | 01-11-2028 | [V] |

| Unity IAP (`com.unity.purchasing`) | Fecha | PBL que incluye | Notas | Fuente |
|---|---|---|---|---|
| 4.12.0 | 04-06-2024 | 6.2.1 | No cumple | [V] changelog |
| **5.0.0** | 07-08-2025 | **8.0.0** | Primera versión que cumple el requisito de 2026 | [V] |
| 5.2.0-pre.1 / 5.2.0 | 26-01-2026 / 23-02-2026 | 8.3.0 | — | [V] |
| **5.4.0** | 29-06-2026 | **9.0.0** | Unity mínima: **2022.3**. Requiere `com.unity.services.authentication` 3.7.1 o superior | [V] |
| **5.4.4** | 02-10-2026 | 9.0.0 | **Publicada ("released") para Unity 6000.3**. Avisa si falta la licencia de Google Play | [V] manual 6000.3 + changelog |

Conclusión: con **IAP 5.4.4 sobre 6000.3.25f1** se cumple el requisito de PBL 8 o superior con margen hasta 2028 [V]. El GDD dice "Billing 8"; conviene cambiarlo a "Billing 8 o superior (9.0.0 con IAP 5.4.x)".

**Requisitos de Play Console para las compras integradas** [V Play Console Help 1153481 y developer.android.com/google/play/billing/getting-ready]:
- Para poder crear productos hay que haber publicado antes, en alguna pista (la interna sirve), una build con la Billing Library y el permiso `com.android.vending.BILLING`.
- Hace falta un perfil de pagos vinculado y una cuenta de comerciante.

---

## 5. Compatibilidad de los SDK con Unity 6.3 (AGP 9)

| SDK | Última versión (fecha) | Qué trae | Compatibilidad con Unity 6.3 y AGP 9 | Estado |
|---|---|---|---|---|
| **AppLovin MAX Unity Plugin** | **8.6.6** (15-09-2026) | SDK de Android 13.6.4 e iOS 13.6.4 | El plugin pide Unity 2019.4 o superior. Hay issues abiertas por usuarios de 6000.3.x (p. ej. 6000.3.12 y 6000.3.1) que son de adaptadores, no del núcleo. En 6000.4.10 (AGP 9) se informó un fallo de build con **Quality Service** activado (#611, cerrado por inactividad y sin solución publicada) | [V] releases + docs AppLovin · [V] #611 · [E] "sin bloqueo conocido en el núcleo" |
| Adaptadores MAX (AdMob, Unity Ads, Meta, Mintegral) | Los que proponga el Integration Manager | — | Un adaptador puede traer `.so` sin alinear a 16 KB (caso HyprMX) o conflictos de dependencias (Unity Ads 4.17 con Install Referrer, #593). Hay que validarlos uno a uno en el CI | [V] #521, #593 · [NV] alineación de Meta y Mintegral |
| **Google UMP** | Lo gestiona MAX ("Google UMP automation", opcional) | El SDK de MAX muestra UMP en regiones del RGPD y pasa los TCF v2 a las redes | Evita instalar un segundo plugin. El mensaje del RGPD se configura en *AdMob › Privacy & messaging* | [V] AppLovin Terms & Privacy Policy Flow · [NV] versión mínima del SDK para la automatización |
| Google Mobile Ads Unity (solo si hiciera falta) | **11.5.0** (03-09-2026) | SDK Android "Next-Gen" 1.4.0. UMP Android 4.0.0 desde la 11.0.0 | Con AGP 9, el `namespace` duplicado `com.google.unity.ads` rompía la build (#4157). El mantenedor dice que se corrige en la **11.2.0**. Sigue abierto el aviso de `packagingOptions` (#4106). Ya migró a la EDM de Unity (PR #4317, fusionada el 25-09-2026, todavía no publicada) | [V] |
| **Firebase Unity SDK** | **13.17.0** (17-09-2026) | BoM de Android 34.19.0, C++ 13.13.0, **EDM4U 1.2.189** | Pide Unity 2021 LTS o superior (la próxima versión subirá el mínimo a 2022.3). minSdk 23 o superior. No hay issues abiertas sobre AGP 9 | [V] release + setup + readme · [E] "sin bloqueo AGP 9" (no hay issues) |
| Firebase en el **editor Windows ARM64** | — | Las bibliotecas de escritorio (*desktop workflow*, beta, solo para desarrollo) se compilan con `-A x64` | No hay DLL nativa ARM64. Unity "no soporta paquetes con binarios de terceros sin soporte nativo para Windows on Arm". Consecuencia: en Play Mode del editor ARM64, `FirebaseApp` no cargará (DllNotFound o BadImageFormat). El APK/AAB para Android no se ve afectado porque lleva `.so` arm64-v8a | [V] `build_zips.py` + system requirements de Unity · [E] síntoma exacto |
| **Google Play Games plugin for Unity** | **2.3.0** (29-09-2026) | play-services-games-v2 22.1.0. Saved Games con soporte oficial | minSdk 24 o superior desde la 2.2.0. Fallaba con rutas con espacios en Unity 6.5 (#3391, cerrado). **Bug abierto: tras guardar sin conexión, Saved Games puede no volver a subir** (#3379) | [V] |
| **Addressables** | **2.10.3** (publicada para 6000.3) | — | La 2.11.x y la 3.x/4.x son compatibles pero no son la "released" de 6000.3 | [V] manual 6000.3 |
| **Addressables for Android (PAD)** | **1.1.0** (publicada para 6000.3) | Empaqueta los grupos de Addressables en *asset packs*, con *Texture Compression Targeting* | — | [V] manual 6000.3 |
| **EDM4U** (Google) | **1.2.190** (29-09-2026): "This package is deprecated" | — | Se archiva el **26-10-2026**. Fallos abiertos en Unity 6.4+: URIs inválidas si la ruta del proyecto tiene **espacios** (#775) y fallos si cambia la mayúscula de la letra de unidad (#776) | [V] |
| **EDM de Unity** (`com.unity.external-dependency-manager`) | **2.1.0** (publicada para 6000.3) | Fork oficial que lee los mismos `*Dependencies.xml` | Unity recomienda mantener EDM4U "si vino con un SDK" hasta que ese SDK migre | [V] manual 6000.3 + blog de Unity (29-09-2026) |

---

## 6. Primera subida desde una cuenta de organización

| Paso o requisito | Detalle | Estado |
|---|---|---|
| Prueba cerrada obligatoria (12 testers durante 14 días) | Solo para cuentas **personales** creadas después del 13-11-2023. Nuestra cuenta es de organización y no entra en ese supuesto | [V] alcance (answer 14151465) · [E] exención (la página no nombra las cuentas de organización) |
| Prueba interna | Hasta **100 testers**. Se puede iniciar **antes de completar la configuración** de la app. "Puede no estar sujeta" a la revisión estándar | [V] answer 9845334 |
| Data Safety | Obligatoria en las pistas cerrada, abierta y producción. **No se pide** mientras la app esté solo en la interna. Hay que declarar lo que recogen los SDK de terceros (MAX, Firebase) | [V] answer 10787469 |
| Política de privacidad | Obligatoria junto a Data Safety (aunque la app no recoja datos) y en *App content* | [V] answer 10787469 y 9859455 |
| *App content* | Política de privacidad, declaración de anuncios, acceso para revisores, público objetivo, clasificación de contenido y declaraciones de permisos | [V] answer 9859455 |
| Formato y firma | AAB con **Play App Signing obligatorio** para apps nuevas. Google guarda la clave de firma de la app; nosotros solo la **clave de subida**, que se puede restablecer si se pierde | [V] developer.android.com/studio/publish/app-signing |
| Clave de subida (keytool) | Validez de 25 años o más y **caducidad posterior al 22-10-2033**. Comando de referencia de la guía: `keytool -genkey -v -keystore <archivo>.jks -keyalg RSA -keysize 2048 -validity 10000 -alias <alias>` | [V] ídem |
| `keytool` en esta máquina | `C:\Program Files\Eclipse Adoptium\jdk-17.0.19.10-hotspot\bin\keytool.exe` está en el PATH | [V] comprobado hoy |
| Exigencias técnicas desde la primera subida | targetSdk 36, 16 KB y PBL 8 o superior | [V] §1, §3 y §4 |
| Compras integradas | Hay que subir antes una build con Billing; perfil de pagos y cuenta de comerciante | [V] §4 |
| Play Games Services | Activar Saved Games en Play Console antes de programarlo. Registrar el SHA-1 de la clave de firma de la app (Play) y el de la clave de subida en las credenciales | [V] activación (saved-games) · [E] SHA-1 de las dos claves |

**Nota para un equipo de agentes.** La contraseña del keystore y el inicio de sesión con Unity ID para activar la licencia Personal son pasos que hace **el dueño**. Los agentes no deben escribir contraseñas reales ni subir el `.jks` al repositorio. El CI lo lee desde *GitHub Secrets* en base64, y el agente solo hace referencia al nombre del secreto [E] (práctica recomendada).

---

## 7. Qué versión de Unity cumple todo con menos riesgo

| Criterio | 6000.0.84f1 (6.0 LTS) | **6000.3.25f1 (6.3 LTS)** | 6000.6.4f1 (6.6) |
|---|---|---|---|
| Target 36 | Sí | **Sí** | Sí |
| Camino a target 37 (agosto de 2027) | No verificado | En el enum y con SDK 37 desde el 22f1; estabilidad no verificada | Sí (corrección en el 6.6.3f1) |
| 16 KB (motor e IL2CPP) | Sí (≥0.38f1) | **Sí** | Sí |
| IAP 5.4.4 con PBL 9 | Sí | **Sí ("released")** | Sí |
| Soporte restante | **Termina en octubre de 2026** | **Hasta diciembre de 2027** | Hasta que salga 6.7 |
| Editor ARM64 con módulo Android | Sí | **Sí** | Sí |
| Imagen de game-ci para Android | No comprobado | **Sí: `unityci/editor:ubuntu-6000.3.25f1-android-3` (24-09-2026)** | No comprobado |
| Riesgo de cambios entre parches | Bajo, pero la rama está acabando | **Bajo (LTS)** | Alto (versión de actualización) |

**Veredicto: Unity `6000.3.25f1` (changeset `e1dba0a9aba4`)**, fijada en `ProjectSettings/ProjectVersion.txt` y en el CI. Se pasará a parches posteriores de 6.3 (p. ej. el `6000.3.26f1`, que trae AGP 9.1.1) solo cuando el CI esté en verde con ese parche.

---

## Recomendación para este equipo y esta máquina

1. **Instalación** (la hará el agente de instalación; aquí solo se documenta):
   - Unity Hub `3.22.2`, paquete winget `Unity.UnityHub` con instalador **arm64 msix** [V `winget show`].
   - **No usar** `winget install Unity.Unity.6000`: instala la `6000.6.4f1`, que no es LTS [V `winget search`].
   - Instalar el editor `6000.3.25f1` ARM64 con el módulo `android` y sus hijos (`android-sdk-ndk-tools`, `android-open-jdk-17.0.18+8`). Puede hacerse desde Hub con `unityhub://6000.3.25f1/e1dba0a9aba4` o con la Unity CLI: `unity install 6000.3.25f1 -c e1dba0a9aba4 -m android --cm` [V sintaxis de la referencia de Unity CLI, que Unity marca como experimental].
   - La Hub CLI está deprecada desde Hub 3.18 [V].
   - La activación de la licencia Personal requiere que el dueño inicie sesión.
2. **SDK, NDK y JDK gestionados por Unity**: en *External Tools*, marcar "installed with Unity" en los tres. Así se usa el mismo NDK r27c que el CI (game-ci). No hay que definir ANDROID_HOME para Unity [E]. El NDK 27.1 de Android Studio no cumple el requisito de Unity [V].
3. **Player Settings**:
   - minSdk 26 y **Target API fijado en 36**, sin "Automatic (highest installed)". Con el modo automático, el módulo, que instala la plataforma 37, podría elegir 37 [E], y hay un informe de fallo de build con 37 en 6.3 [V].
   - IL2CPP, solo ARM64.
   - `appCategory = Game` (comprobar el manifiesto).
   - `predictiveBackSupport` probado.
   - UI dentro del *safe area*.
4. **Paquetes y SDK con versión fija**:
   - IAP **5.4.4**.
   - Addressables **2.10.3** y Addressables for Android **1.1.0**.
   - AppLovin MAX **8.6.6** con los adaptadores AdMob, Unity Ads, Meta y Mintegral, y la automatización de Google UMP.
   - Firebase **13.17.0**, solo los módulos necesarios.
   - Play Games **2.3.0**.
   - **No instalar** el plugin GMA Unity: con MAX basta para AdMob y UMP, y se evitan conflictos de namespace con AGP 9. Hay que cambiar en el GDD 08 la línea `ExternalDependencyManager/ # EDM4U (Firebase, GMA)`.
   - Mantener el EDM4U 1.2.189 que trae Firebase. Pasar a `com.unity.external-dependency-manager` 2.1.0 cuando Firebase y MAX dejen de incluir EDM4U.
5. **Arquitectura pensada para el editor ARM64.** Todo acceso a Firebase pasa por interfaces de `AF.Analytics`, `AF.RemoteConfig` y `AF.Backend`, con implementaciones falsas en el editor (`#if UNITY_ANDROID && !UNITY_EDITOR` o inyección de dependencias). Así el Play Mode funciona en ARM64 aunque las DLL x64 de Firebase no carguen. Firebase se prueba en dispositivo y en la prueba interna.
6. **Compuertas de CI** (ya previstas en el GDD 08 §14):
   - Comprobar 16 KB con `zipalign -c -P 16 -v 4` sobre los APK que genera `bundletool` y con `check_elf_alignment.sh` en Ubuntu.
   - `targetSdk == 36` y `minSdk == 26`.
   - Que el AAB declare PBL ≥ 8: comprobar la versión de `com.android.billingclient` en el árbol de dependencias de Gradle [E].
   - Tamaño ≤ 80 MB.
   - Imagen `unityci/editor:ubuntu-6000.3.25f1-android-3`.
7. **Primer día (prueba rápida en esta máquina):**
   - Crear un proyecto URP 2D vacío y generar un AAB IL2CPP ARM64.
   - Medir cuánto tarda; el NDK r27c de Windows funciona con emulación x64 (Prism) [E].
   - Verificar la versión de Gradle (9.1.0 o 9.3.1).
   - Pasar el AAB por las compuertas de 16 KB y subirlo a la prueba interna.
8. **Ruta del proyecto sin espacios.** La actual (`C:\Users\Nipko-lab\Desktop\projects\amazon-fill`) no tiene [V], y hay fallos de EDM4U y de PGS con espacios en Unity 6.4+ [V]. Tampoco debe cambiar la mayúscula de la letra de unidad en el CI (#776).
9. **Calendario anual.** En el primer trimestre de 2027, probar target 37 en el último parche de 6.3. Si falla, evaluar 6.7 si sale como LTS [NV]. El plazo es agosto de 2027 [V].

---

## Riesgos y plan B

| # | Riesgo | Probabilidad e impacto | Mitigación / plan B |
|---|---|---|---|
| 1 | Firebase no carga en el editor ARM64 | Alta / bajo con fakes | Fakes en el editor (punto 5). Plan B: ejecutar el editor x64 de 6000.3.25f1 con emulación solo para depurar Firebase [E]. Plan C: depurar en dispositivo |
| 2 | La build IL2CPP de Android en un host ARM64 es lenta o falla (el NDK y Gradle son x64 o JVM) | Media / alto | Prueba rápida el primer día. Plan B: compilar siempre en GitHub Actions (Ubuntu x64 con game-ci), lo que los agentes pueden lanzar con `gh`. Plan C: editor x64 emulado |
| 3 | AGP 9 rompe algún plugin (namespace, `packagingOptions`, R8) | Media / medio | Añadir `namespace` en los `.androidlib`, actualizar SDK (GMA 11.2 o superior si algún día se usa) y desactivar Quality Service de MAX si falla (#611). Plan B: fijar el 6000.3.16f1 (AGP 8.10); se pierden el SDK 37 y las correcciones posteriores, así que solo como último recurso |
| 4 | Una `.so` de un adaptador no está alineada a 16 KB | Media / bloquea la subida | Compuerta de CI y quitar o actualizar el adaptador culpable antes de subir |
| 5 | EDM4U archivado el 26-10-2026 | Segura / bajo | Seguir con la versión de Firebase y migrar a la EDM de Unity 2.1.0 cuando los SDK migren |
| 6 | Problema conocido en 6.3: **pantalla negra con Bloom y el 2D Renderer** (UUM-150232, en la lista del 6000.3.25f1) | Media / medio (afecta a URP 2D) | No usar Bloom del 2D Renderer en 1.0, o validarlo en el prototipo del líquido en la semana 1. Si el efecto es imprescindible, hacer el brillo en Shader Graph |
| 7 | Bug abierto de Saved Games tras guardar sin conexión (#3379) | Media / medio | El perfil local manda (GDD 08). Reintentar la subida al volver la conexión y probar el flujo sin conexión en QA |
| 8 | Target 37 inestable en 6.3 cuando haga falta (2027) | Media / alto en 2027 | Probar en el primer trimestre de 2027. Plan B: 6.7 LTS (si se confirma) o 6.6 o posterior |
| 9 | Diferencia de versión de Gradle entre la documentación y las notas del 25f1 | Baja / bajo | Verificarla el primer día y no fijar versiones de Gradle en plantillas propias |
| 10 | Prórrogas de Play (01-11-2026) y requisitos que cambien antes del lanzamiento | Baja / medio | Revisar la página de target API y la de PBL antes de cada subida a producción |
| 11 | Que la exención de cuenta de organización (prueba cerrada) no aplique | Baja / medio (14 días de retraso) | Iniciar una prueba cerrada en paralelo con el equipo y familiares si Play Console la pidiera [E] |

---

## Tabla final: componente → versión mínima → versión recomendada → fuente

| Componente | Versión mínima (para cumplir hoy) | Versión recomendada (06-10-2026) | Fuente |
|---|---|---|---|
| targetSdk (Play) | 36 [V] | 36 (37 antes de agosto de 2027 [V]) | support.google.com/googleplay/android-developer/answer/11926878 · android-developers.googleblog.com/2026/02/prepare-your-app-for-resizability-and.html |
| minSdk | Unity 6.3 admite 25 o más [V] | 26 (GDD) | docs.unity3d.com/6000.3/Documentation/Manual/system-requirements.html |
| Unity Editor | 6000.3.0f1 (16 KB) / 6000.3.22f1 (SDK 37) [V] | **6000.3.25f1 (`e1dba0a9aba4`) ARM64** | services.api.unity.com/unity/editor/release/v1/releases?version=6000.3.25f1 · unity.com/releases/unity-6/support |
| Gradle / AGP | AGP 8.5.1 o superior para 16 KB [V] | El que trae Unity: AGP 9.0.0 y Gradle 9.3.1 (o 9.1.0) | docs.unity3d.com/6000.3/Documentation/Manual/android-gradle-version-compatibility.html |
| NDK | r27c 27.2.12479018 [V] | r27c (módulo de Unity) | docs.unity3d.com/6000.3/Documentation/Manual/android-supported-dependency-versions.html |
| JDK | 17 [V] | OpenJDK 17.0.18+8 (módulo) | ídem + API de releases |
| Play Billing Library | 8 [V] | 9.0.0 (a través de IAP) | developer.android.com/google/play/billing/deprecation-faq |
| Unity IAP | 5.0.0 [V] | **5.4.4** | docs.unity3d.com/6000.3/Documentation/Manual/com.unity.purchasing.html |
| AppLovin MAX Unity | [NV] mínima para AGP 9 | **8.6.6** | github.com/AppLovin/AppLovin-MAX-Unity-Plugin/releases/tag/release_8_6_6 |
| Google UMP | A través de MAX (automatización opcional) [V] | MAX 8.6.6 con UMP | support.applovin.com/en/max/unity/overview/terms-and-privacy-policy-flow |
| GMA Unity (opcional) | 11.2.0 (corrige el namespace con AGP 9) [V] | No instalar (11.5.0 si hiciera falta) | github.com/googleads/googleads-mobile-unity/issues/4157 |
| Firebase Unity | 12.10.0 (16 KB estable) [V] | **13.17.0** | github.com/firebase/firebase-unity-sdk/releases/tag/v13.17.0 |
| Play Games plugin | 2.0.0 (PGS v2) [E] | **2.3.0** | github.com/playgameservices/play-games-plugin-for-unity/releases/tag/v2.3.0 |
| Addressables | 2.10.3 [V] | 2.10.3 | docs.unity3d.com/6000.3/Documentation/Manual/com.unity.addressables.html |
| Addressables for Android (PAD) | 1.1.0 [V] | 1.1.0 | docs.unity3d.com/6000.3/Documentation/Manual/com.unity.addressables.android.html |
| EDM | EDM4U 1.2.189 (incluido en Firebase) [V] | Pasar a `com.unity.external-dependency-manager` 2.1.0 cuando migren los SDK | docs.unity3d.com/6000.3/Documentation/Manual/com.unity.external-dependency-manager.html |
| Imagen de CI | — | `unityci/editor:ubuntu-6000.3.25f1-android-3` [V] | hub.docker.com/r/unityci/editor/tags |
| Unity Hub | — | 3.22.2 arm64 (winget `Unity.UnityHub`) [V] | `winget show --id Unity.UnityHub` (06-10-2026) |
| Clave de subida | RSA 2048, caducidad posterior al 22-10-2033 [V] | Validez de 10.000 días o más | developer.android.com/studio/publish/app-signing |

---

## Fuentes

Google Play y Android
- Target API: https://support.google.com/googleplay/android-developer/answer/11926878?hl=en
- API 37 en agosto de 2027 y exención de juegos: https://android-developers.googleblog.com/2026/02/prepare-your-app-for-resizability-and.html
- Cambios de comportamiento de Android 16: https://developer.android.com/about/versions/16/behavior-changes-16
- Android 17 (fuente secundaria, fecha de publicación): https://en.wikipedia.org/wiki/Android_17
- 16 KB: https://developer.android.com/guide/practices/page-sizes
- Calendario de PBL: https://developer.android.com/google/play/billing/deprecation-faq
- Pruebas obligatorias para cuentas personales nuevas: https://support.google.com/googleplay/android-developer/answer/14151465?hl=en
- Pruebas internas, cerradas y abiertas: https://support.google.com/googleplay/android-developer/answer/9845334?hl=en
- Data Safety: https://support.google.com/googleplay/android-developer/answer/10787469?hl=en
- App content: https://support.google.com/googleplay/android-developer/answer/9859455?hl=en
- Firma de apps: https://developer.android.com/studio/publish/app-signing
- Productos integrados: https://support.google.com/googleplay/android-developer/answer/1153481?hl=en · https://developer.android.com/google/play/billing/getting-ready
- Play Games Saved Games en Unity: https://developer.android.com/games/pgs/unity/saved-games · https://developer.android.com/games/pgs/unity/unity-start

Unity
- Soporte de Unity 6: https://unity.com/releases/unity-6/support · https://endoflife.date/unity
- API de releases (versiones, changesets y módulos ARM64): https://services.api.unity.com/unity/editor/release/v1/releases?version=6000.3.25f1&architecture=ARM64&platform=WINDOWS
- Notas de versión:
  - 6000.3.25f1: https://storage.googleapis.com/live-platform-resources-prd/templates/assets/6000_3_25f1_46dff63af8/6000_3_25f1_46dff63af8.md · https://unity.com/releases/editor/whats-new/6000.3.25f1
  - 6000.3.22f1 (SDK Platform 37): https://storage.googleapis.com/live-platform-resources-prd/templates/assets/6000_3_22f1_2dfd8e2e65/6000_3_22f1_2dfd8e2e65.md
  - 6000.3.21f1 (R8 y AGP 9): https://storage.googleapis.com/live-platform-resources-prd/templates/assets/6000_3_21f1_7a96a7e1f9/6000_3_21f1_7a96a7e1f9.md
  - 6000.3.17f1 (Gradle 9.1 y AGP 9.0): https://storage.googleapis.com/live-platform-resources-prd/templates/assets/6000_3_17f1_9e5650ca3f/6000_3_17f1_9e5650ca3f.md
  - 6000.6.3f1 (target 37): https://unity.com/releases/editor/whats-new/6000.6.3f1
- Gradle y AGP: https://docs.unity3d.com/6000.3/Documentation/Manual/android-gradle-version-compatibility.html
- Dependencias Android: https://docs.unity3d.com/6000.3/Documentation/Manual/android-supported-dependency-versions.html
- Requisitos Android: https://docs.unity3d.com/6000.3/Documentation/Manual/android-requirements-and-compatibility.html · https://docs.unity3d.com/6000.6/Documentation/Manual/android-requirements-and-compatibility.html
- Requisitos del sistema (Windows Arm64): https://docs.unity3d.com/6000.3/Documentation/Manual/system-requirements.html
- Enum de niveles de API: https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AndroidSdkVersions.html
- appCategory: https://docs.unity3d.com/ScriptReference/PlayerSettings.Android-appCategory.html
- Retroceso predictivo: https://docs.unity3d.com/6000.3/Documentation/ScriptReference/PlayerSettings.Android-predictiveBackSupport.html
- Hilo de AGP 9: https://discussions.unity.com/t/android-gradle-9-1-0-agp-9-0-0-update-in-unity-6-0/1709062
- Fallo de build con target 37 en 6000.3.23f1: https://discussions.unity.com/t/unable-to-complete-the-build-for-android-17-unityexception-required-api-level-37/1735038
- 16 KB en Unity: https://discussions.unity.com/t/android-unity-engine-support-for-16-kb-memory-page-sizes-android-15/1589588 · https://discussions.unity.com/t/unity-6000-3-0f1-google-play-rejects-aab-due-to-16-kb-memory-page-size-agp-8-5-already-in-use/1707589
- IAP:
  - Changelog: https://docs.unity3d.com/Packages/com.unity.purchasing@5.4/changelog/CHANGELOG.html · https://github.com/needle-mirror/com.unity.purchasing/blob/master/CHANGELOG.md
  - Versión publicada para 6000.3: https://docs.unity3d.com/6000.3/Documentation/Manual/com.unity.purchasing.html
- Addressables: https://docs.unity3d.com/6000.3/Documentation/Manual/com.unity.addressables.html · https://docs.unity3d.com/6000.3/Documentation/Manual/com.unity.addressables.android.html
- EDM de Unity: https://docs.unity3d.com/6000.3/Documentation/Manual/com.unity.external-dependency-manager.html · https://unity.com/blog/external-dependency-manager
- Unity CLI: https://docs.unity.com/en-us/unity-cli/unity-cli-reference · Hub CLI deprecada: https://docs.unity.com/en-us/hub/hub-cli

SDK de terceros
- AppLovin MAX:
  - Release 8.6.6: https://github.com/AppLovin/AppLovin-MAX-Unity-Plugin/releases/tag/release_8_6_6
  - Integración: https://support.applovin.com/en/max/unity/overview/integration
  - Flujo de términos y privacidad (UMP): https://support.applovin.com/en/max/unity/overview/terms-and-privacy-policy-flow
  - Privacidad: https://support.applovin.com/en/max/unity/overview/privacy
  - Issues: https://github.com/AppLovin/AppLovin-MAX-Unity-Plugin/issues/521 · /issues/593 · /issues/611
- Google Mobile Ads Unity:
  - Releases: https://github.com/googleads/googleads-mobile-unity/releases/tag/v11.5.0 · /releases/tag/v11.0.0
  - Issues y PR: https://github.com/googleads/googleads-mobile-unity/issues/4157 · /issues/4106 · /pull/4317
- Firebase Unity:
  - Release 13.17.0: https://github.com/firebase/firebase-unity-sdk/releases/tag/v13.17.0
  - Script de build (solo x64 en Windows): https://github.com/firebase/firebase-unity-sdk/blob/main/scripts/build_scripts/build_zips.py
  - Desktop workflow: https://github.com/firebase/firebase-unity-sdk/blob/main/docs/readme.md
  - 16 KB: https://github.com/firebase/firebase-unity-sdk/issues/1335
  - Configuración: https://firebase.google.com/docs/unity/setup
- Play Games plugin:
  - Release 2.3.0: https://github.com/playgameservices/play-games-plugin-for-unity/releases/tag/v2.3.0
  - Issues: https://github.com/playgameservices/play-games-plugin-for-unity/issues/3379 · /issues/3391
- EDM4U:
  - Repositorio: https://github.com/googlesamples/unity-jar-resolver
  - Release 1.2.190: https://github.com/googlesamples/unity-jar-resolver/releases/tag/v1.2.190
  - Issues: https://github.com/googlesamples/unity-jar-resolver/issues/775 · /issues/776
- game-ci (imágenes Docker): https://hub.docker.com/r/unityci/editor/tags?name=6000.3.25f1

Comprobaciones locales de solo lectura (06-10-2026)
- `winget show --id Unity.UnityHub` (3.22.2, msix arm64)
- `winget search "Unity 6"` (Unity.Unity.6000 → 6000.6.4f1)
- Listado de `%LOCALAPPDATA%\Android\Sdk`: NDK 27.1.12297006, plataformas 36 y 36.1, build-tools 35 a 37, sin cmdline-tools
- `Get-Command keytool`
