---
paths:
  - "Assets/_Project/Scripts/{Ads,IAP,PlayBilling,Analytics,RemoteConfig,Save,Platform,Consent,Backend}/**"
  - "Assets/_Project/Plugins/**"
  - "Assets/_Project/Data/Config/**"
  - "Assets/Plugins/**"
  - "Assets/GoogleMobileAds/**"
  - "Assets/Firebase/**"
  - "Assets/GooglePlayGames/**"
  - "Assets/GooglePlayPlugins/**"
  - "Assets/ExternalDependencyManager/**"
  - "Packages/manifest.json"
  - "functions/**"
---

# SDK de terceros, consentimiento y servicios

Fuentes: GDD 08 §2.3, §7-§11 y §13.3; 05 §4-§7; `docs/setup/plan-instalacion.md` ("Cambios en el GDD", punto 5); `docs/setup/research/b-android-play-2026.md`.

- **Todo cambio aquí va en un PR revisado y aprobado por el dueño** (además del code-reviewer). Ningún SDK, adaptador, paquete, permiso Android ni subida de versión sin esa aprobación previa.
- **Versiones fijadas** (plan aprobado 2026-10-06): Play Billing Library 9.0.0 (`com.android.billingclient:billing`) con el plugin propio `AfBilling.androidlib`, que sustituye a Unity IAP 5.4.4 (D-040, añadido de Q-IAP-1; `docs/dev/iap.md`); Firebase 13.17.0 solo con los módulos necesarios; Play Games 2.3.0; Addressables 2.10.3 y Addressables for Android 1.1.0. **Google Mobile Ads 11.5.0** (`play-services-ads` 25.4.0, UMP 4.0.0) para AdMob directo (D-049), con mediación de **Unity Ads** por el adaptador 3.21.1 (`com.google.ads.mediation:unity` 4.20.1.0, SDK 4.20.1; añadido de D-049 del 2026-10-27). **AppLovin MAX salió del proyecto** (añadido de D-049 del 2026-10-27; compuerta `ci/check-no-applovin.ps1`): si AppLovin abre cuentas, entra como red de mediación dentro de AdMob, nunca como SDK aparte. **Play Age Signals** `com.google.android.play:age-signals` 0.0.4 con el plugin propio `AfAgeSignals.androidlib` (D-057). EDM4U el que trae Firebase (archivado el 26-10-2026; migración por PR aparte). Versiones exactas, nunca rangos ni "latest".
- **Interfaces y fakes:** cada SDK vive detrás de su interfaz de AF.Ads, AF.IAP, AF.Analytics, AF.RemoteConfig, AF.Save o AF.Platform, con implementación falsa para el editor. El editor ARM64 no carga las DLL x64 de Firebase, ni EDM4U: el Play Mode debe funcionar sin ellas.
- **Consentimiento antes que nada:** Analytics apagado y Google Mobile Ads sin inicializar hasta el consentimiento; pantalla de edad neutral con bandas <13 (bloqueo en 1.0), 13-17 (`npa`, contenido G, PIN de compras) y 18+ (08 §2.3 y §7).
- **Secretos:** ninguna clave, `google-services.json`, keystore ni cuenta de servicio en el repositorio, en el código, en logs ni en el chat. Los coloca el dueño (archivos ignorados o secretos de CI). Si faltan, la tarea se detiene y se pide al dueño. **Excepción (D-049, decisión del dueño del 2026-10-26):** el App ID y los IDs de bloque de AdMob no son secretos (van dentro del APK y en `app-ads.txt` se publica el ID de editor) y viven como constantes en `GoogleAdUnits` y en el asset de ajustes del plugin, siempre con los bloques de prueba de Google en los builds con `com.amazonfill.dev`.
- **Servidor:** compras verificadas en Cloud Function (`verifyPurchase`) y recompensas por SSV (`rewardedSsv`) antes de acreditar de forma definitiva (08 §10).
- **Cada SDK nuevo o actualizado:** fila de Data Safety (08 §13.3), `bash ci/check-16kb.sh` sobre sus `.so` (los adaptadores de mediación son el riesgo habitual), impacto en `ci/size-gate.ps1` y `namespace` en los `.androidlib` (AGP 9).
- **Publicación:** ningún AAB a Google Play (ni a la pista interna) con el `applicationId` definitivo `com.<estudio>.tintalu` hasta tener el dictamen de marca; los builds de prueba usan `com.amazonfill.dev`, que es provisional y nunca puede llegar a un build de release (compuerta del CI). Regla operativa aparte: los agentes no suben nada a Google Play; publica el dueño.
