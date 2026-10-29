# Tintalú en iOS sin Mac: investigación y hoja de ruta

Fecha: 2026-10-28. Estado: **propuesta**, pendiente de las decisiones del dueño (§6). Hasta que el dueño decida, nada de esto cambia el GDD.
Marcas de evidencia: [V] verificado en una fuente oficial o en el repositorio; [E] estimación o fuente de terceros; [NV] sin verificar.

## 1. Resumen

- **Sí se puede llevar Tintalú a iOS sin comprar un Mac.** No se puede compilar iOS *en* Windows: Unity en Windows exporta el proyecto de Xcode, pero solo Xcode (que solo corre en macOS) lo compila, lo firma y lo sube [V, manual de Unity]. La solución es **alquilar macOS por minutos en la nube** para ese único paso.
- **Ruta recomendada:** Unity exporta el proyecto de Xcode en el runner Linux de GitHub, con la licencia Personal como ya hace `ci.yml` [V]. Un Mac en la nube compila, firma y sube a TestFlight. El iPhone del dueño instala la app desde TestFlight. Con este reparto, el Mac en la nube no necesita licencia de Unity (§3).
- **El atajo que circula** (xtool y similares) no sirve para un juego de Unity y pone en riesgo la cuenta de Apple (§3.4).
- **El trabajo de código es mayor que el de build.** Hoy no hay ni una rama `UNITY_IOS`. Las compras (Play Billing), el guardado en la nube (Play Games), la edad (Play Age Signals), el país legal (SIM y cuenta de Play), los cuatro plugins `.androidlib` y todo el CI son solo Android (§4). La buena noticia es que cada módulo ya tiene su interfaz con una implementación falsa, así que el lado iOS entra en los mismos puntos.
- **Cambio frente al GDD:** 08 §12.2 elegía Codemagic con Unity dentro. Codemagic ya no admite la licencia Unity Personal para CI: exige Plus o Pro [V, artículo de ayuda de Codemagic, 2026-06-29]. Eso responde a la pregunta abierta 5 de 08. La ruta recomendada lo evita, porque Unity no corre en el Mac en la nube.

## 2. Qué se puede hacer desde Windows y qué no

| Paso | ¿En Windows? | Cómo |
|---|---|---|
| Programar, Play Mode, tests EditMode y PlayMode | Sí | Igual que hoy |
| Exportar el proyecto de Xcode | Sí | Módulo **iOS Build Support** de Unity. Falta en el escritorio: hoy solo están `AndroidPlayer` y `windowsstandalonesupport` [V]. En el portátil ARM64 no está confirmado que el módulo exista [NV]. |
| Compilar, firmar y crear el `.ipa` | **No** | Exige Xcode 26 con el SDK de iOS 26, obligatorio para subir desde el 2026-04-28 [V, Apple]. Va en un Mac en la nube. |
| Certificados, perfiles y App ID | Sí | En la web de Apple Developer, o con una clave del API de App Store Connect (`.p8`) desde el CI. El CSR se puede generar con OpenSSL [E]. |
| Subir a App Store Connect | Sí, pero mejor desde el CI | `iTMSTransporter` tiene versión de línea de comandos para Windows y Linux [V, Apple], con problemas de autenticación reportados [E]. Lo normal es subir desde el mismo Mac en la nube. |
| Instalar en el iPhone | Sí | **TestFlight**: el iPhone la descarga, sin cable ni Mac. |
| Simulador de iOS, Instruments, depurador de Xcode | **No** | Solo con un Mac. Sustitutos: Crashlytics, el registro del juego y el Profiler de Unity por red con un build de desarrollo [E]. |
| Automatizar toques como hoy con `adb shell input tap` | **No** | No hay equivalente sin Mac (WebDriverAgent exige Xcode). La automatización por `AF_STATE` y `AF_LAYOUT` sigue siendo de Android; en el iPhone se prueba a mano. |

## 3. Rutas para compilar sin Mac

### 3.1 Recomendada: GitHub Actions con dos trabajos

1. **`export-ios`** (ubuntu-24.04): `game-ci/unity-builder` con `targetPlatform: iOS` y los mismos secretos `UNITY_EMAIL` y `UNITY_PASSWORD` de `build-android` [V en `ci.yml`; el patrón, E]. Sube el proyecto de Xcode como artefacto.
2. **`build-ios`** (runner macOS con Xcode 26):
   - `pod install`: EDM4U genera el Podfile de Firebase y de Google Mobile Ads;
   - `xcodebuild archive` con firma automática mediante la clave del API de App Store Connect;
   - exportación del `.ipa` y subida a TestFlight (`fastlane pilot` o `xcrun altool`).

- **A favor:**
  - el mismo sitio, los mismos secretos y la misma revisión que el resto del CI;
  - sin licencia de Unity en macOS;
  - sin otra cuenta de terceros.
- **En contra:**
  - en un repositorio privado, el minuto de macOS cuesta unos 0,062 USD [V, tabla de precios de GitHub] y consume la cuota gratuita a 10× [E];
  - un archivo de Unity con IL2CPP tarda unos 15-30 min en compilar en Xcode [E], es decir, ≈1-2 USD por build.

### 3.2 Alternativa barata: Linux en GitHub y Xcode en Codemagic

- **En qué cambia:** el primer trabajo es el mismo; el segundo corre en Codemagic, con 500 min gratis al mes en M2 [V, documentación de Codemagic].
- **A favor:** como Unity no corre allí, la regla de la licencia Personal no aplica. Con pocos builds al mes probablemente cuesta 0 USD.
- **En contra:** es otra cuenta y otro sitio con secretos, y hay que pasar el artefacto entre GitHub y Codemagic.

### 3.3 Alternativa oficial: Unity Build Automation

- **A favor:**
  - desde el 2026-03-01, el plan gratuito incluye 100 min de Mac al mes y admite iOS [V, Unity];
  - lo integra todo y acepta la licencia Personal;
  - gestiona la firma con el `.p12` y el perfil.
- **En contra:**
  - importa la `Library` desde cero en sus máquinas, así que 100 min dan para pocos builds;
  - la configuración vive fuera del repositorio;
  - más lenta para iterar.
- **Uso:** solo como respaldo.

### 3.4 Descartadas

- **xtool** ("Xcode multiplataforma" para Linux y WSL):
  - solo compila paquetes de SwiftPM; no compila proyectos de Xcode como los que genera Unity, ni Pods [V, README y foro de Swift];
  - necesita el `Xcode.xip` de Apple instalado en hardware que no es de Apple, y el acuerdo de licencia del Apple Developer Program lo prohíbe [E, OSNews];
  - riesgo para la cuenta: no sirve.
- **macOS en una máquina virtual o "Hackintosh"** en el Ryzen: viola la licencia de macOS y es frágil. Descartado.
- **Herramientas comerciales "sin Mac"** (MacFree y similares): por debajo usan GitHub Actions o Build Automation; no aportan nada que no hagamos con 3.1 [E].
- **Sideloadly o AltStore con un Apple ID gratuito:**
  - la app caduca a los 7 días;
  - no permite compras ni probar los servicios de Apple con la cuenta de pago.
  - Útil, como mucho, para instalar un `.ipa` *ad hoc* ya firmado.

### 3.5 Cuándo sí conviene un Mac

Si un fallo solo se reproduce en el iPhone y Crashlytics no basta (cuelgues nativos, memoria, rendimiento de Metal), se alquila un Mac por horas o por un mes:

- MacinCloud: ≈125 USD/mes [E, 08 §12.2];
- AWS EC2 Mac: se factura un mínimo de 24 h [E].

Comprar un Mac mini M4 se decide solo si estos alquileres se repiten.

## 4. Inventario: lo que hoy es solo Android y su equivalente en iOS

Ruta base: `Assets/_Project/`. Cada fila entra por una interfaz que ya existe y tiene su implementación falsa.

| Área | Hoy (Android) | En iOS | Esfuerzo [E] |
|---|---|---|---|
| **Compras** | Plugin propio `AfBilling.androidlib` (Play Billing 9.0.0, D-040) detrás de `IPlayStorePort`/`IBillingClient` | **StoreKit 2.** Opciones: plugin propio en Swift, en la línea de D-040, o Unity IAP 5 solo en iOS (usa StoreKit 2 [V]; choca con el espíritu de D-040; `ci/check-no-unity-iap.ps1` hoy mira artefactos Android). `PlayBillingClient` tiene forma de Play (token, *acknowledge*, `PENDING`): hace falta otro cliente. | Alto |
| **Verificación en el backend** | `infra/functions/shared/playApi.ts` (Google Play Developer API), `voided-purchases-job` | **App Store Server API** (JWS firmado) y **App Store Server Notifications v2** para reembolsos | Medio |
| **Guardado en la nube** | Play Games Saved Games (D-042) | Play Games no existe en iOS. Opciones: (a) local, sin nube, hasta el guardado en AWS de la 1.2 (D-035); (b) adelantar ese guardado en AWS, que además da el traspaso Android ↔ iOS; (c) Game Center Saved Games. El GDD descarta iCloud KVS (08 §11). | (a) bajo · (b) alto · (c) medio |
| **Identidad** | Firebase Auth anónimo; el token autoriza el API de AWS | Igual; funciona en iOS. Si la 1.1 añade el inicio de sesión con Google (D-056), Apple exige ofrecer también **Sign in with Apple** u otro equivalente que proteja la privacidad (directriz 4.8 [E]). | Bajo |
| **Anuncios** | Google Mobile Ads 11.5.0, UMP y Unity Ads (D-049) | El mismo SDK: hay que ampliar el `#if` y añadir el App ID y los bloques de iOS. Además: **ATT** con aviso previo tras el consentimiento (hoy no hay código ATT), `SKAdNetworkItems` en el `Info.plist` y `NSUserTrackingUsageDescription`. 08 §12.2 todavía habla de MAX: hay que actualizarlo. | Medio |
| **Firebase** | Analytics, Crashlytics, Remote Config, Auth; App Check con Play Integrity (`FirebaseBoot.cs:161`) | Los mismos módulos y `GoogleService-Info.plist` (secreto: lo pone el dueño en el CI, como `GOOGLE_SERVICES_JSON_BASE64`). App Check con **App Attest**; `infra/.../integrity.ts` ya verifica tokens de App Check [V]. | Bajo-medio |
| **Edad** | Pantalla neutral propia y **Play Age Signals** (`AfAgeSignals.androidlib`, D-057) | La pantalla propia se queda. Segunda fuente: **Declared Age Range API** (iOS 26+ [V, Apple]), obligatoria en la práctica donde hay leyes de verificación de edad (Utah desde el 2026-05-06, Louisiana desde el 2026-07-01, Texas en proceso [V, Apple]). Plugin pequeño en Swift; con iOS < 26 se usa `FakeAgeSignals`. | Medio |
| **País legal** (D-057) | País de la SIM, luego país de la cuenta de Play, luego idioma del teléfono | La SIM no sirve en iOS (`CTCarrier` está obsoleto desde iOS 16 y devuelve un valor de relleno [E]). El sustituto natural de la cuenta de Play es el **país de la tienda** (`Storefront.current.countryCode` de StoreKit). | Bajo (con el plugin de StoreKit) |
| **Notificaciones** | `AndroidNotificationScheduler` con Mobile Notifications 2.4.3 | El mismo paquete tiene la parte de iOS: falta el planificador de iOS y el permiso en el momento adecuado | Bajo |
| **Arranque e icono** | `AfSplashHold.androidlib` (D-051) y `AfIcon.androidlib` (D-045) | La pantalla de lanzamiento de iOS (storyboard) no se puede retener: la intro empieza al cargar. El icono va en el catálogo de Xcode, con las variantes oscura y tintada que D-045 deja pendientes; `exportar.py` necesitaría `--ios`. | Bajo-medio |
| **JNI suelto** | `AndroidHaptics`, `AndroidSystemScale`, `AndroidDeviceLocale`, `AndroidAppSettings`, `LaunchOptions`, `BootSteps` (reloj), `GameScreen` (`UnityPlayer`) | Hápticos con `UIImpactFeedbackGenerator`; Dynamic Type para la escala; `NSLocale`; `UIApplication.openSettingsURLString`. Lo demás es solo de depuración. | Bajo-medio |
| **Pantalla** | Zona segura de Android | Notch, Dynamic Island y barra de inicio: revisar la zona segura en el iPhone de pruebas y los 120 Hz de ProMotion (`targetFrameRate`) | Bajo |
| **Build y CI** | `BuildSmoke`, `BuildDev`, `BuildRelease`, `ProjectSetup.ApplyAndroidDefaults`; `ci.yml` y `release.yml`; las compuertas de `ci/` | Nuevos `BuildIos` (exportar el proyecto de Xcode), `ProjectSetup.ApplyIosDefaults` y un post-proceso que escribe en el `Info.plist` y en el proyecto de Xcode las capacidades, el ATT y el SKAdNetwork; `ios.yml`; compuertas propias (tamaño del `.ipa`, `PrivacyInfo.xcprivacy`, sin `com.amazonfill.dev` en release) | Medio |
| **Privacidad** | Data Safety (08 §13.3) | `PrivacyInfo.xcprivacy` propio con las Required Reason APIs (`UserDefaults`/`PlayerPrefs`, hora del sistema…) más los manifiestos de cada SDK; etiquetas de privacidad de App Store coherentes con Data Safety | Bajo-medio |
| **Paquetes de contenido** | PAD previsto, aún no instalado | Sin equivalente directo: On-Demand Resources está obsoleto en iOS 27 (R16, 09); Background Assets o la CDN de AWS. Mientras no haya PAD, todo va dentro del `.ipa`. | Pospuesto |

## 5. Hoja de ruta por fases

Cada fase termina con algo que el dueño ve en su iPhone. Las duraciones son de calendario con agentes [E].

### Fase 0: cuentas y decisiones (dueño, 1-3 semanas, en paralelo con la fase 1)

1. **Apple Developer Program** como organización (la marca es de Parallext: `com.parallext.tintalu`):
   - 99 USD al año [V];
   - la organización necesita un **número D-U-N-S** (gratis; el nombre legal debe coincidir letra por letra con el de la inscripción; Apple puede tardar de 1 día a 1 semana [E]).
2. En App Store Connect:
   - aceptar el acuerdo de **apps de pago** y rellenar los datos fiscales y bancarios (sin eso no hay compras);
   - solicitar el **Small Business Program** (comisión del 15 %);
   - declarar el **estatus de comerciante** de la DSA para vender en la UE.
3. Registrar el Bundle ID **`com.parallext.tintalu`**, el mismo que en Android (08 §13.2), y crear la app.
4. Crear una **clave del API de App Store Connect** (`.p8`, rol App Manager) y guardarla, con su Key ID y su Issuer ID, como secretos del CI. Los agentes no la tocan (regla dura 5).
5. Registrar el UDID del iPhone de pruebas (solo hace falta para builds *ad hoc*; TestFlight no lo pide).
6. Instalar **iOS Build Support** en el Editor 6000.3.25f1 del escritorio desde Unity Hub, para exportar en local; la CLI no instala.
7. Tomar las decisiones de §6.

### Fase 1: "Hola, iPhone" (1-2 semanas)

**Meta:** el juego arranca y se juega en el iPhone desde TestFlight, con todos los SDK en sus implementaciones falsas.

- Añadir `BuildIos.ExportXcode` y `ProjectSetup.ApplyIosDefaults` (versiones mínimas, ARM64, Metal, orientación, nombre visible, número de build).
- Ramas `UNITY_IOS` que, por ahora, eligen las falsas: compras no disponibles, sin nube, sin anuncios.
- Escribir `.github/workflows/ios.yml` con export en Linux, Xcode en macOS y subida a TestFlight (ruta 3.1 o 3.2), lanzado a mano (`workflow_dispatch`).
- Montar el icono provisional y la pantalla de lanzamiento.
- **Prueba en el teléfono:** niveles 1-10, Mapa, Liga sin backend, zona segura, rendimiento y audio.

### Fase 2: plataforma (2-3 semanas)

- **Firebase en iOS:** Analytics, Crashlytics, Remote Config, Auth anónimo; App Check con App Attest; el backend acepta el id de la app iOS (`-c firebaseAppIds`).
- **Notificaciones** con el planificador de iOS.
- **Hápticos, escala de texto, idioma y ajustes**, con su versión de iOS.
- **País legal** desde el país de la tienda, y la pantalla de edad igual que en Android.
- **Icono definitivo** con las variantes de iOS: un trabajo de D-045 para el exportador de la marca.

### Fase 3: monetización (3-4 semanas)

- **Plugin de StoreKit 2:**
  - productos, compra, restaurar y `Transaction.updates`;
  - mismos SKU que Play (08 §13.2 #5).
- **Backend:**
  - `verify-purchase` con el API de App Store Server;
  - notificaciones v2 para reembolsos;
  - libro mayor común para «Sin anuncios» (pregunta abierta 10 de 08).
- **Anuncios en iOS:**
  - App ID y bloques;
  - aviso previo y **ATT**;
  - `SKAdNetworkItems`;
  - UMP;
  - mediación de Unity Ads, cuando se apruebe.
- **Prueba en el teléfono:** compras con una cuenta *sandbox* de TestFlight (sin cobro real), restaurar en un segundo dispositivo, recompensados con SSV.

### Fase 4: cumplimiento y ficha (1-2 semanas, en paralelo con la 3)

- `PrivacyInfo.xcprivacy`; etiquetas de privacidad.
- **Clasificación por edad** con el cuestionario nuevo (4+, 9+, 13+, 16+ y 18+ [V]). La Liga del río comparte el país: hay que revisar las preguntas de interacción entre usuarios y las de redes sociales, obligatorias desde septiembre de 2026 [E]. Brasil sin cajas de botín.
- **Declared Age Range API.**
- `ITSAppUsesNonExemptEncryption = false` (solo HTTPS).
- Capturas de 6,9" y 6,5"; textos en en-US, es-419 y pt-BR desde `docs/store-listing/`.
- Borrado de cuenta en la app (ya existe) y la URL web.

### Fase 5: QA y envío (1-2 semanas)

- La regresión de release (`docs/qa/regresion-release.md`) adaptada a iOS, sobre el build de TestFlight.
- TestFlight interno, solo con el dueño, igual que D-059 en Play.
- Revisión de Apple [E: 90 % en menos de 24 h según 08 §12.2] y publicación manual.

**Total orientativo:** 2-3 meses de calendario desde que la cuenta de Apple está aprobada. El GDD situaba "US + iOS" en octubre-noviembre de 2027 (09); empezar ahora es una decisión del dueño.

## 6. Decisiones que necesita el dueño

| # | Pregunta | Recomendación |
|---|---|---|
| I-1 | ¿Empezamos iOS ya, adelantando la fase 6 del GDD? | Sí con las fases 0 y 1 (cuesta poco y mide el riesgo real); las fases 3 y 4 después de estabilizar la 1.0 en Android |
| I-2 | ¿Dónde se compila el proyecto de Xcode? | **3.1 (GitHub Actions)**; si los minutos de macOS pesan, 3.2 (Codemagic solo para Xcode). Unity Build Automation, de respaldo. |
| I-3 | ¿Con qué se hacen las compras en iOS? | **Plugin propio de StoreKit 2**, por coherencia con D-040 y el libro mayor propio; Unity IAP 5 solo si se prioriza la velocidad |
| I-4 | ¿Qué guardado en la nube hay en iOS antes de la 1.2? | **(a) solo local** en la primera versión de iOS, y el guardado en AWS de D-035 como solución común; Game Center, solo para logros, opcional |
| I-5 | ¿Paquete para el ATT? | Un plugin propio pequeño (unas 30 líneas de Objective-C) mejor que `com.unity.ads.ios-support`: no añade paquetes (regla dura 4) |
| I-6 | ¿Se instala iOS Build Support en el escritorio? (en el portátil ARM64, si existe) | Sí: permite exportar y revisar el proyecto de Xcode sin esperar al CI |
| I-7 | ¿Mismo alcance que Android 1.0 (Liga, anuncios, compras, todo el mundo)? | Sí, salvo PAD; iOS empieza ya con la versión que haya en Play en ese momento |

## 7. Cambios que esto pide en el GDD (cuando el dueño decida)

- **08 §12.2:**
  - la fila "Máquina" asume Codemagic con Unity dentro, que ya no admite la licencia Personal [V]: sustituir por la ruta elegida en I-2;
  - las filas "Privacidad" y la 13.2 #3 hablan de MAX: pasar a AdMob directo (D-049).
- **08, pregunta abierta 5:** queda respondida (la licencia Personal no sirve en Codemagic; se evita exportando en Linux).
- **08 §11 y D-042:** el guardado en la nube de iOS según I-4.
- **09:** el calendario de la fase 6 según I-1.

## 8. Fuentes

- Apple:
  - requisito de Xcode 26 y SDK de iOS 26 desde el 2026-04-28: https://developer.apple.com/news/upcoming-requirements/
  - edad (Declared Age Range, Utah, Louisiana, Brasil, Australia, Singapur): https://developer.apple.com/news/?id=f5zj08ey
  - Transporter en Windows y Linux: https://developer.apple.com/help/app-store-connect/manage-asset-packs/upload-apple-hosted-asset-packs
- Unity:
  - configurar iOS ("Xcode is only available for macOS"; Build Automation como vía sin Mac): https://docs.unity3d.com/Manual/ios-environment-setup.html
  - plan gratuito con 100 min de Mac desde el 2026-03-01: https://unity.com/products/pricing-updates
  - IAP 5 y StoreKit 2: https://unity.com/resources/unity-iap-5-migration-guide
- Codemagic:
  - licencia Personal no admitida: https://help.codemagic.io/articles/2292424011-can-i-make-app-store-builds-using-unity-personal-license-with-codemagic
  - 500 min gratis en M2: https://docs.codemagic.io/getting-started/about-codemagic/
- GitHub, precio del minuto de macOS: https://docs.github.com/en/enterprise-server@3.17/billing/reference/actions-runner-pricing
- xtool:
  - el proyecto: https://github.com/xtool-org/xtool
  - discusión en el foro de Swift: https://forums.swift.org/t/xtool-cross-platform-xcode-replacement-build-ios-apps-on-linux-and-more/79803
  - la licencia, según OSNews: https://www.osnews.com/story/142331/xtool-cross-platform-xcode-replacement-for-linux-windows-and-macos/
- Patrón de GitHub Actions con Unity e iOS:
  - https://dev.to/virtualmaker/automating-unity-builds-to-ios-macos-and-visionos-14km
  - https://circleci.com/blog/unity-mobile-cicd
- AdMob, ATT y SKAdNetwork en Unity: https://developers.google.com/admob/unity/privacy/strategies
- Repositorio:
  - `docs/gdd/08-arquitectura-tecnica-publicacion.md` §11, §12.2 y §13.2, y preguntas abiertas 5 y 10;
  - `docs/gdd/09-roadmap-equipo-kpis-riesgos.md` (fase 6, R16);
  - `docs/dev/decisiones.md` (D-035, D-040, D-042, D-045, D-049, D-056, D-057);
  - `.github/workflows/ci.yml` (licencia Personal en Linux).
