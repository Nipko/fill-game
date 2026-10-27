# Avisos de licencia de los componentes de terceros (T-L13)

- **Estado:** inventario del 2026-10-27 (tarea T-L13 de `investigacion-legal-mundial.md` §11; D-057, añadido del 2026-10-27). Lo hizo el carril `tanda6/web-legal` sin navegar: cada licencia sale de un archivo que está en el repositorio, en la caché de paquetes de Unity, en la caché de Gradle o en el Editor instalado, y la columna «Fuente» dice cuál.
- **Para qué sirve:** es la lista de la que salen la sección «Software de terceros» de `infra/web/src/content/*/credits.html` (§9, fila 41) y, cuando exista, la pantalla «Créditos y licencias» del juego (sección 5).
- **Marcas:** [NV] = no verificado con el texto de la licencia (solo por el nombre del componente o por conocimiento general); [E] = estimación. Lo que lleva [NV] se cierra leyendo el archivo indicado o el aviso del fabricante antes de la 1.0.
- **Qué entra en el build:** se comprobó con `Builds/Android/dev.apk` del 2026-10-26 (con D-049): `META-INF/*.version`, `*.properties` de Play Services y Firebase, `lib/arm64-v8a/*.so` y la raíz del APK. Las dependencias de Android son las de `Assets/Plugins/Android/mainTemplate.gradle` (las escribe EDM4U), con `play-services-nearby` excluido (D-042) y `firebase-appcheck-debug` fuera de release (D-033, punto 9).

## 1. SDK fijados

| Componente | Versión | Licencia | Fuente en el repositorio | ¿En el build? |
|---|---|---|---|---|
| Unity (motor y reproductor Android, IL2CPP) | 6000.3.25f1 | Términos de Unity (Unity Terms of Service y Unity Software Additional Terms) [NV] | `ProjectSettings/ProjectVersion.txt`; el texto no está en el repositorio | Sí (`libunity.so`, `libil2cpp.so`, `libmain.so`) |
| Paquetes de Unity: URP 17.3.0 (y `render-pipelines.core`, `shadergraph`), Input System 1.20.0, uGUI 2.0.0 (con TextMesh Pro), Timeline 1.8.13, 2D (`2d.animation`, `2d.spriteshape`, `2d.common`, `2d.sprite`, `2d.tilemap`), Burst, Collections, Mathematics | las de `Packages/packages-lock.json` | **Unity Companion License** («for Unity-dependent projects»); Burst: Unity Companion License para el código fuente y **Unity Package Distribution License** para lo demás | `LICENSE.md` de cada paquete en `Library/PackageCache/<paquete>@<hash>/` (la `Library` de la copia principal del escritorio, sin versionar) | Sí (Burst: `lib_burst_generated.so`) |
| Unity Mobile Notifications | 2.4.3 | Unity Companion License | `Library/PackageCache/com.unity.mobile.notifications@*/LICENSE.md` | Sí |
| Firebase para Unity: App, Analytics, App Check, Auth, Crashlytics, Remote Config | 13.17.0 | **Apache 2.0**, con el aviso de **MiniJSON** (MIT, Copyright (c) 2013 Calvin Rien) al final | `Packages/firebase/com.google.firebase.*-13.17.0.tgz` → `package/LICENSE.md` | Sí (C#, `libFirebaseCpp*.so` y las AAR `*-unity`) |
| Bibliotecas de Android de Firebase: `firebase-analytics` 23.2.0, `firebase-auth` 24.2.0, `firebase-config` 23.1.0, `firebase-appcheck` y `-playintegrity` 19.4.1, `firebase-crashlytics-ndk` 20.1.1, `firebase-common` 22.2.1 | las de `mainTemplate.gradle` | **Android Software Development Kit License** (https://developer.android.com/studio/terms) | POM en `~/.gradle/caches/modules-2/files-2.1/com.google.firebase/firebase-analytics/23.2.0/…/firebase-analytics-23.2.0.pom` (fuera del repositorio; se comprobó esa; las demás, [NV] por el mismo origen) | Sí |
| Google Mobile Ads para Unity (D-049) | 11.5.0 | **Apache 2.0** (`LICENSE.md` y `GoogleMobileAds/LICENSE`) | `Packages/googleads/com.google.ads.mobile-11.5.0.tgz` | Sí |
| Google Mobile Ads SDK (`play-services-ads`) y UMP (`user-messaging-platform`) | 25.4.0 y 4.0.0 | Android Software Development Kit License | POM de cada una en la caché de Gradle; versiones en `GoogleMobileAdsDependencies.xml` y `GoogleUmpDependencies.xml` del `.tgz` | Sí |
| Unity Ads en la mediación de AdMob (añadido de D-049 del 2026-10-27): plugin de Unity `com.google.ads.mobile.mediation.unity`, adaptador `com.google.ads.mediation:unity` y SDK `com.unity3d.ads:unity-ads` | 3.21.1, 4.20.1.0 y 4.20.1 | Plugin: **Apache 2.0** (`package/LICENSE`); adaptador: **Apache-2.0** (POM); SDK: **«Unity Ads License»** (POM: `https://unity.com/legal`), propietaria | `Packages/googleads/com.google.ads.mobile.mediation.unity-3.21.1.tgz`; POM `unity-4.20.1.0.pom` y `unity-ads-4.20.1.pom` en la caché de Gradle | Sí (`com/unity3d/ads/…`, `com/google/ads/mediation/unity/…` y `libunitycoherencenative.so`) |
| Google Play Billing Library (plugin propio `AfBilling.androidlib`, D-040) | 9.0.0 | Android Software Development Kit License | `Assets/_Project/Plugins/Editor/AfBillingDependencies.xml`; POM `billing-9.0.0.pom` en la caché de Gradle | Sí (`billing.properties`) |
| `AfBilling.androidlib` y `AfIcon.androidlib` | — | Código propio de Parallext, LLC | `Assets/_Project/Plugins/Android/` | Sí |
| Google Play Games para Unity (D-042) | 2.3.0 | Apache 2.0 [NV]: el `.tgz` **no trae LICENSE**; el plugin se publica como código abierto Apache 2.0 | `Packages/playgames/com.google.play.games-2.3.0.tgz` (solo `package.json`, autor Google LLC) | Sí |
| Play Games Services v2 (`play-services-games-v2`) | 22.1.0 | Android Software Development Kit License | `GooglePlayGamesPluginDependencies.xml`; POM en la caché de Gradle | Sí. `play-services-nearby` 18.5.0 **no** (excluido en `mainTemplate.gradle`) |
| External Dependency Manager for Unity (EDM4U) | 1.2.189 | **Apache 2.0**, con el aviso de MiniJSON (MIT) | `Packages/firebase/com.google.external-dependency-manager-1.2.189.tgz` → `package/LICENSE.md` | Solo Editor (resuelve las dependencias; no entra código suyo en el APK) [E] |
| ~~AppLovin MAX para Unity~~ (retirado del proyecto: D-049, añadido del 2026-10-27) | antes 8.6.6 (`applovin-sdk` 13.6.4) | AppLovin SDK EULA (propietaria) | Ya no está: salieron `Packages/applovin/`, `Assets/MaxSdk/` y `AF.Ads.Max` | **No**: `ci/check-no-applovin.ps1` (CI y release) comprueba que `dev.apk` y el AAB no llevan `com/applovin/…`, `libapplovin-native-crash-reporter.so` ni su `okhttp3` renombrado |

## 2. Bibliotecas que traen los SDK (en el APK)

| Componente | Licencia | Fuente | Nota |
|---|---|---|---|
| AndroidX (activity, appcompat, core, fragment, lifecycle, room, work, webkit, datastore, credentials, privacysandbox.ads y las demás de `META-INF/*.version`, 48 en total) | Apache 2.0 [NV] | `META-INF/androidx.*.version` de `dev.apk`; POM en la caché de Gradle | Las traen Google Mobile Ads, Firebase y Play Games |
| Kotlin (stdlib) y kotlinx.coroutines (`core`, `android`, `play-services`) | Apache 2.0 [NV] | `kotlin/*.kotlin_builtins` y `META-INF/kotlinx_coroutines_*.version` del APK | — |
| Play Services comunes (`play-services-base`, `-basement`, `-tasks`, `-measurement*`, `-ads-identifier`, `-appset`, `-auth*`, `-location`, `-stats`, `integrity`, `recaptcha`, `googleid`…) | Android Software Development Kit License [NV salvo `play-services-ads` y `-games-v2`, comprobadas] | `*.properties` en la raíz del APK | — |
| MiniJSON (Calvin Rien) | MIT | Final de `LICENSE.md` de Firebase 13.17.0 y de EDM4U 1.2.189 | — |
| Lo que trae Unity Ads 4.20.1: `com.unity3d.coherence:coherencelib` 0.1.0 (`libunitycoherencenative.so`), `okhttp` 3.12.13 con `okio`, `protobuf-kotlin-lite` 3.25.6 | coherencelib: «Unity Ads License» (POM `coherencelib-0.1.0.pom`); okhttp y okio: Apache 2.0 [NV]; protobuf: BSD-3-Clause [NV] | POM en la caché de Gradle (`com.unity3d.coherence`, `com.squareup.okhttp3`, `com.google.protobuf`) | okhttp aquí va sin renombrar (no es la copia de AppLovin) |
| Android Game Development Kit: GameActivity (`libgame.so`) y Frame Pacing (`libswappywrapper.so`) | Apache 2.0 [NV] | `lib/arm64-v8a/` del APK | Los añade Unity |
| `libc++_shared.so` (LLVM, del NDK r27c) | Apache 2.0 con excepciones de LLVM [NV] | `lib/arm64-v8a/` del APK | — |
| Firebase C++ (`libFirebaseCpp*.so`) y Crashlytics NDK (`libcrashlytics*.so`) | Apache 2.0 y las de sus dependencias internas [NV] | `lib/arm64-v8a/` del APK | Sus avisos de terceros no están en el repositorio |
| IL2CPP: Boehm GC (`bdwgc`, con `libatomic_ops`), zlib, xxHash y las bibliotecas de clases de Mono | Licencia tipo MIT de Boehm GC, zlib, BSD-2 (xxHash) y MIT (Mono) [NV] | `C:\Users\USER\Unity\Hub\Editor\6000.3.25f1\Editor\Data\il2cpp\external\` (`bdwgc/libatomic_ops/COPYING`, `zlib`, `xxHash`, `mono`), fuera del repositorio | Entran en `libil2cpp.so` |
| Componentes de terceros de los paquetes de Unity | URP: FXAA 3.11 (licencia propia de NVIDIA); `render-pipelines.core`: RadeonRays (MIT), Bullet (zlib), Sobol (MIT); Burst: LLVM (Apache 2.0 con excepciones), Mono.Cecil (MIT), xxHash y Smash (BSD-2), musl, SLEEF, mimalloc…; `2d.animation`: Clipper (Boost), Eigen y libigl (MPL 2.0); `2d.spriteshape`: LibTessDotNet (SGI B 2.0); `2d.common`: cdt2d (MIT) | `Third Party Notices.md` de cada paquete en `Library/PackageCache/` | Muchos son solo de Editor o de compilación (Burst compila con LLVM; Eigen y libigl son del editor de huesos) [E]: confirmar cuáles llegan al reproductor |

## 3. Fuentes tipográficas

| Fuente | Licencia | Fuente en el repositorio | ¿En el build? |
|---|---|---|---|
| Baloo 2 (The Baloo 2 Project Authors) | SIL OFL 1.1 | `Assets/_Project/Art/Fonts/OFL-Baloo2.txt` | Sí |
| Nunito (The Nunito Project Authors) | SIL OFL 1.1 | `Assets/_Project/Art/Fonts/OFL-Nunito.txt` | Sí |
| Liberation Sans (TextMesh Pro) | SIL OFL 1.1 | `Assets/TextMesh Pro/Fonts/LiberationSans - OFL.txt` | Por confirmar (`credits.html` lo deja como `[[DATO DEL DUEÑO]]`) |

La OFL exige que la licencia acompañe a la fuente cuando se distribuye: hoy los `.txt` están en el repositorio, no en el APK.

## 4. Lo que tiene que mostrar el juego o la web

- **Apache 2.0** (Firebase para Unity, GMA para Unity, Play Games para Unity, EDM4U, AndroidX, Kotlin, AGDK): una copia de la licencia y el `NOTICE` de quien lo tenga (§4 a y d). La web enlaza el texto y nombra los componentes.
- **MIT** (MiniJSON, Mono, cdt2d…): el aviso de copyright y el de permiso. La web nombra a MiniJSON y a Calvin Rien y enlaza el texto MIT; falta el texto literal de cada aviso en el juego.
- **Android SDK License** y **Unity Ads License**: no piden aviso al usuario final [E]; se nombran por transparencia (Unity Ads, en `credits.html` con el enlace a `https://unity.com/legal`).
- **Unity Companion License y términos de Unity**: Unity publica su propia lista de avisos de terceros del motor [NV]; se enlaza desde la pantalla del juego.
- **OFL 1.1**: el texto de la OFL con cada fuente.

## 5. Pendientes

1. **La pantalla «Créditos y licencias» del juego no existe** (no hay clave `settings_credits` ni vista en `Scripts/UI/`). §9, fila 41, la daba por hecha; por eso la web dice que los avisos están «en esta página» y no en el juego. Para la 1.0 haría falta (tarea de unity-gameplay-dev, con textos por `strings.csv`):
   - una entrada «Créditos y licencias» en Ajustes, junto a «Privacidad y datos»;
   - una vista con scroll que muestre los créditos y, por componente, el nombre, la licencia y el aviso literal (Apache 2.0 con los `NOTICE`, MIT con su copyright, OFL con su texto), leídos de un archivo generado (por ejemplo, `Data/Legal/licencias.txt`) que salga de esta lista y de los `LICENSE` del repositorio, no escrito a mano;
   - o, como mínimo, un enlace a `credits.html` de la web y el plugin `oss-licenses` de Google Play Services, si el dueño lo aprueba (es una dependencia nueva: regla dura 4).
2. ~~AppLovin MAX va en el APK aunque no sirva anuncios~~ **Resuelto (D-049, añadido del 2026-10-27):** el dueño decidió quitarlo; MAX salió del proyecto entero (plugin, `applovin-sdk`, `AF.Ads.Max`) y con él `libapplovin-native-crash-reporter.so` y el `okhttp3` renombrado con su `NOTICE`. La compuerta `ci/check-no-applovin.ps1` lo vigila en la CI y en el release. Si AppLovin abre cuentas, entra como red de mediación de AdMob, con su adaptador, y entonces tendrá su fila aquí.
3. Cerrar los [NV] de la sección 2 con los POM y `NOTICE` reales (por ejemplo, con un informe de dependencias de Gradle del build de release en el CI) y confirmar qué componentes de terceros de los paquetes de Unity llegan al reproductor.
4. Confirmar si Liberation Sans entra en el build de release.
