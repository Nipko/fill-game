# Stack técnico y requisitos de publicación — Amazon Fill (Android primero, iOS después)

Fecha del estudio: 2026-10-05. Convención: **[V]** = dato verificado con URL en la sección Fuentes; **[E]** = estimación o inferencia del investigador; **[NV]** = no se pudo verificar en esta ronda.

## Resumen ejecutivo

- **Motor recomendado: Unity 6 (plan Personal)**. Es gratis hasta 200.000 USD de ingresos+financiación, el Runtime Fee está cancelado de forma definitiva y el splash de "Made with Unity" es opcional [V]. Gana por ecosistema de monetización (LevelPlay, AppLovin MAX, Google Mobile Ads, Unity IAP, Firebase, Play Games), que es exactamente lo que un F2P con anuncios necesita.
- **Alternativa: Godot 4.5/4.6**. Builds más pequeños (20-28 MB vs 30-50 MB) y mejor en gama baja con el renderer Compatibility [V], pero sin bindings mantenidos de LevelPlay/MAX ni Firebase oficial; el propio análisis de 2026 concluye que para F2P "live service" Unity sigue siendo mejor opción [V].
- Cocos Creator y Defold producen APKs de 8-15 MB y ~1 MB base respectivamente [V], pero su documentación/comunidad en inglés es débil (Cocos) o el ecosistema de mediación exige trabajo manual (Defold). Flutter+Flame es viable para puzzles pero el shader de líquido y el pipeline de juego son más artesanales.
- **APK vacío de Unity 6 ≈ 32 MB** (IL2CPP, ARM64, stripping) [V]. Para LatAm hay que apuntar a un AAB < 60 MB [E]: ARM64 only, texturas ASTC, audio comprimido, Addressables para packs de fondos.
- **Líquido 2D**: un quad por tubo con shader de N franjas de color + onda senoidal amortiguada en la superficie, máscara del recipiente, menisco con gradiente, burbujas por partículas enmascaradas y chorro con curva Bézier dibujado con LineRenderer. Referencias concretas: breakdown de MinionsArt (Unity Shader Graph) y los shaders "Liquid In Mesh" y "2D Liquid Fill Inside Sphere" de godotshaders.com [V].
- **Animación "sumergir y teñir"**: 1,2-1,8 s en total, lanzada solo cuando el nivel ya está en memoria, saltable con un toque y acortable tras los primeros niveles [E]. Un estudio citado sitúa la satisfacción al 70 % con esperas de ~1,6 s y al 50 % con ~2,7 s [V].
- **Datos**: niveles como JSON con estado explícito (no solo semilla) + metadatos; guardado local + Play Games Saved Games (máx. 3 MB por partida, 800 KB de portada, no cuenta contra cuota del desarrollador) [V]; Firebase Remote Config (3.000 parámetros, 2.000 condiciones, 24 experimentos/rollouts simultáneos) [V] para dificultad y cadencia de anuncios; Firebase Analytics + Crashlytics (gratis) y GameAnalytics (plan gratuito con Remote Configs y A/B) [V].
- **CI Android**: GitHub Actions + game-ci/unity-builder (2.000 min Linux gratis/mes; builds de 15-30 min) [V]. **iOS obliga a macOS con Xcode 26**: Codemagic (500 min gratis/mes; M4 a 0,114 USD/min) o Mac dedicado en la nube (MacinCloud M4 124,99 USD/mes; MacStadium M4.S 149 USD/mes; Scaleway ~0,22 EUR/h) [V].
- **Google Play 2026**: desde el 31-08-2026 toda app nueva o actualización debe apuntar a **API 36 (Android 16)**; extensión posible hasta el 01-11-2026 [V]. Soporte de **páginas de 16 KB** obligatorio para apps que apuntan a Android 15+ desde el 01-11-2025 [V]. **Play Billing Library 8** obligatoria desde el 31-08-2026 [V]. AAB obligatorio desde 2021 [V].
- **La prueba cerrada de 12 testers / 14 días aplica solo a cuentas personales creadas después del 13-11-2023** (texto oficial de Google). La cuenta de organización ya verificada **está exenta** [V].
- Obligatorios en Play: URL de política de privacidad, formulario Data Safety declarando cada SDK (anuncios, analítica, crash), clasificación IARC, declaración de público objetivo. Si se declara público infantil hay que usar solo SDKs de anuncios autocertificados para familias [V]. Recomendación: declarar 13+ y evitar la vía Families [E].
- Consentimiento: UMP SDK (incluido en el plugin GMA) para EEE/Reino Unido; LevelPlay 9.5 unificó la API de consentimiento GDPR en un booleano [V].
- **App Store**: 99 USD/año; desde el 28-04-2026 solo se aceptan builds con **Xcode 26 / iOS 26 SDK**; Privacy Manifest (PrivacyInfo.xcprivacy) con Required Reason APIs; etiquetas de privacidad; ATT antes de leer el IDFA; 90 % de revisiones en < 24 h [V].
- **Localización**: ES (neutro LatAm), PT-BR, EN de salida. Unity Localization 1.5.13 importa/exporta CSV, XLIFF y Google Sheets [V]; Godot usa CSV/PO nativos [V]. Lokalise desde 59 USD/mes [V]; para 300-600 cadenas basta Google Sheets + traductor freelance [E].

## 1. Comparativa de motores (estado 2026)

### 1.1 Licencias y costo

| Motor | Licencia / costo | Fuente |
|---|---|---|
| Unity 6 | Personal gratis hasta 200k USD ingresos+financiación; Pro 2.310 USD/asiento/año (subida del 5 % el 12-01-2026); Enterprise > 25 M USD; Runtime Fee cancelado; splash opcional | [V] unity.com/products/pricing-updates |
| Godot 4.x | MIT, sin costo ni regalías | [V] |
| Flutter + Flame | BSD/MIT, sin costo | [V] |
| Cocos Creator 3.8 | MIT, sin regalías ni splash | [V] engineranked |
| Defold | Gratis, sin regalías, sin umbrales (Defold Foundation) | [V] appradar/sunstrike |

### 1.2 Tamaño de build y gama baja

| Motor | Build base Android | Notas de rendimiento | Fuente |
|---|---|---|---|
| Unity 6 | ~32 MB APK vacío (IL2CPP, ARM64, strip engine code) | URP 2D correcto; runtime pesado en arranque | [V] discussions.unity.com (feb-2025) |
| Godot 4.4 | 20-28 MB | Renderer Compatibility (OpenGL ES 3) "suficiente para 2D" y el de mayor compatibilidad hardware; 2D nativo 15-40 % más rápido en escenas con muchos sprites | [V] engineranked, docs.godotengine.org, dev.to/oceanviewgames |
| Cocos Creator 3.8 | 8-15 MB | 58 fps vs 42 fps de Unity con 500 sprites en gama media (test del reseñador) | [V] engineranked |
| Defold | ~1 MB base; AdMob sin adaptadores lo sube de 5 a 12 MB | Muy ligero; Lua | [V] forum.defold.com |
| Flutter + Flame | ~15-25 MB [E] | 60 fps en gama media; <100 MB RAM en casuales | [V] dev.to/synfinity (parcial) |

### 1.3 Ecosistema de SDKs (lo decisivo para un F2P con anuncios)

| Capacidad | Unity 6 | Godot 4.5+ | Flutter+Flame | Cocos | Defold |
|---|---|---|---|---|---|
| AdMob + UMP | Plugin oficial GMA 11.x [V] | Poing godot-admob-plugin (comunidad, pin de versión) [V] | google_mobile_ads 5.x oficial [V] | SDK propio/China; AdMob vía bridging [E] | Extensión AdMob comunidad [V] |
| Mediación LevelPlay / MAX | Oficial, LevelPlay 9.5.1 (ago-2026) [V]; MAX oficial | Sin bindings mantenidos [V] | Plugins oficiales de MAX y LevelPlay existen [E/NV] | [NV] | Manual [V] |
| IAP | Unity IAP 5.x (Billing 7/8) [V] | Play Billing (Fundación, Godot 4.2+) y StoreKit 2 (Fundación) [V] | in_app_purchase oficial [V] | [NV] | Extensión IAP [E] |
| Analítica / crash | Firebase Unity 13.15 [V], GameAnalytics | GameAnalytics Godot SDK 3.0.1 oficial [V]; sin Firebase oficial, sin Adjust/AppsFlyer [V] | FlutterFire oficial [V] | [NV] | GameAnalytics ext. [E] |
| Cloud save | Play Games plugin 2.x [V] | Plugin PGS comunidad [NV] | games_services pkg [E] | [NV] | [NV] |

### 1.4 Tabla de puntaje (1-5; peso entre paréntesis)

| Criterio (peso) | Unity 6 | Godot 4.5 | Flutter+Flame | Cocos 3.8 | Defold |
|---|---|---|---|---|---|
| 2D + shaders de líquido (3) | 5 | 5 | 3 | 4 | 4 |
| Anuncios y mediación (4) | 5 | 3 | 4 | 2 | 2 |
| IAP (2) | 5 | 4 | 4 | 3 | 3 |
| Analítica / remote config (3) | 5 | 3 | 4 | 2 | 3 |
| Tamaño de build (2) | 2 | 4 | 3 | 5 | 5 |
| Gama baja Android (3) | 3 | 4 | 3 | 4 | 5 |
| Curva / talento disponible (2) | 4 | 4 | 3 | 2 | 2 |
| Costo (1) | 4 | 5 | 5 | 5 | 5 |
| Camino a iOS (2) | 5 | 4 | 4 | 3 | 4 |
| **Total ponderado (máx. 110)** | **94** | **86** | **78** | **70** | **76** |

Los puntajes son juicio del investigador [E] sobre los datos verificados de 1.1-1.3.

### 1.5 Recomendación

**Unity 6 LTS (6000.x), URP 2D, plan Personal.** Razones: (1) toda la cadena anuncios→mediación→IAP→analítica→remote config→cloud save tiene plugins oficiales y actualizados en 2026; (2) el costo es cero hasta 200k USD; (3) el camino a iOS es el más trillado (Xcode project generado, Privacy Manifests resueltos por los SDKs). El costo real es el tamaño: hay que presupuestar trabajo de reducción de build desde el día 1.

**Alternativa: Godot 4.5/4.6** si el equipo prioriza build pequeño y gama baja por encima de eCPM. Riesgos: solo AdMob (sin waterfall/bidding de LevelPlay o MAX), integración de plugins "un día por release", sin Firebase oficial. Si se elige Godot, usar GameAnalytics (tiene Remote Configs y A/B en su plan gratuito [V]) como sustituto de Firebase.

## 2. Render del líquido en 2D

Técnica estándar del género, reconstruida a partir de los shaders referenciados [V] y práctica del investigador [E]:

1. **Recipiente**: sprite del objeto autóctono (tinaja, cuenco, bambú) con capa de "vidrio" semitransparente encima y una **máscara** (SpriteMask en Unity; `CanvasItem` con `clip_children` o textura de máscara en Godot) que recorta el líquido a la silueta interior.
2. **Cuerpo del líquido**: un quad por recipiente con shader que recibe un array de hasta 4-6 colores + alturas acumuladas (`fill[i]`); el fragment shader elige el color por la coordenada `uv.y`. Cambiar franjas = cambiar uniformes, no instanciar sprites.
3. **Superficie con onda**: `y_surface = fill_top + A(t) · sin(uv.x · f + t · s)` con `A(t)` decayendo exponencialmente tras cada vertido (0,5-0,8 s) [E]. MinionsArt alimenta la amplitud desde la velocidad del transform en C# y usa seno en vez de distorsión de normales [V]; "Liquid In Mesh" expone intensidad de onda, espuma y nivel de llenado como uniformes [V].
4. **Menisco y volumen**: línea clara de 2-3 px sobre la superficie (color del líquido +20 % luminosidad) y gradiente oscuro en los bordes laterales para dar curvatura; brillo especular estático en el vidrio.
5. **Burbujas**: 5-10 partículas pequeñas ascendiendo con oscilación horizontal senoidal, dentro de la misma máscara; emitir solo durante el vertido y 1 s después.
6. **Chorro de vertido**: curva Bézier cuadrática desde el labio del recipiente origen al punto de superficie del destino, dibujada con LineRenderer (Unity) / Line2D (Godot), grosor decreciente y UV desplazándose; la franja de destino crece a la vez que la de origen decrece (0,4-0,7 s por vertido; DOTween o Tween nativo) [E]. Un template de Unity con 600 niveles y editor y tutoriales de "liquid pour effect" con LineRenderer confirman que es la técnica habitual [V].
7. **Mezcla de color**: interpolar en HSV u OKLab en lugar de RGB para que el tránsito "agua → tinte de fruta" no pase por grises [E].

### Animación "sumergir y teñir" al cargar el nivel

Secuencia propuesta [E]: (a) 0,3 s: los recipientes caen desde arriba hasta la franja de río del fondo; (b) 0,4 s: se sumergen con ondas concéntricas y salpicadura (partículas), el agua entra como franja casi transparente; (c) 0,3 s: emergen; (d) 0,1 s por franja, solapado: un icono de fruta vuela a cada franja y la tiñe de abajo arriba (la onda de superficie da el efecto "en vivo"). Total 1,2-1,8 s.

Reglas para que no se perciba como carga: el nivel ya debe estar parseado y las texturas residentes antes de disparar (a); mostrar el botón "Saltar" o aceptar toque en cualquier punto desde (a); medir en analítica cuántos usuarios saltan y, si supera ~40 %, bajar la duración a 0,8 s desde el nivel 10 [E]; precargar el nivel N+1 mientras se juega N. El estudio académico citado sobre animaciones de carga encontró satisfacción del 70 % a ~1,6 s y del 50 % a ~2,7 s, y que las animaciones con significado semántico reducen la espera percibida [V].

## 3. Arquitectura de datos

**Formato de nivel (JSON, estado explícito + trazabilidad)** [E]:

```json
{ "id": 1042, "pack": "rio-negro", "tubes": [[0,0,2,1],[1,2,2,0],[],[]],
  "colors": 3, "capacity": 4, "empty": 2, "minMoves": 9,
  "gen": {"seed": 884213, "version": "1.3"}, "tier": "medium" }
```
Guardar el estado explícito (permite ajustar a mano y garantiza solubilidad verificada offline) y conservar semilla+versión del generador solo para reproducir. Empaquetar 100 niveles por archivo en Addressables/StreamingAssets; los packs de fondos por país como Addressables remotos opcionales para no engordar el AAB.

**Guardado**: perfil local en JSON (PlayerPrefs solo para flags) con copia atómica; **Android**: Play Games Services v2 Saved Games (3 MB por partida, 800 KB de portada, sin costo para el estudio; conflicto con `UseLongestPlaytime` o resolución manual por "nivel máximo + timestamp") [V]; **iOS**: iCloud Key-Value Store [E]; **multiplataforma opcional**: Firebase Auth anónimo + Firestore con un documento por usuario [E].

**Remote Config**: Firebase Remote Config — 3.000 parámetros, 2.000 condiciones, 24 A/B tests+rollouts concurrentes, 300 versiones de plantilla [V]. Parámetros mínimos: `interstitial_every_n_levels`, `first_interstitial_level`, `rewarded_undo_cost`, `extra_tube_free_after_fails`, `tint_anim_duration`, `shop_prices_tier`.

**Analítica y crash**: Firebase Analytics + Crashlytics (gratis; Crashlytics captura excepciones C# y crashes nativos) [V]; GameAnalytics como segunda capa con funnels, A/B y Remote Configs en plan gratuito [V]. Eventos clave: `level_start/complete/fail`, `move`, `undo_used`, `booster_used`, `ad_impression` (ILR vía LevelPlay 9.5+ [V]), `iap_purchase`, `anim_skipped`.

## 4. Pipeline

- **Android (GitHub Actions)**: game-ci/unity-builder construye y activa la licencia en Docker; keystore en Base64 como secret; 2.000 min Linux gratis/mes y builds de 15-30 min → 60-100 builds/mes sin costo [V]. Subida a pista interna con `r0adkll/upload-google-play` o fastlane supply [E]. Alternativa: Unity DevOps Build Automation (desde 01-03-2026: asientos ilimitados, 25 GB y más minutos gratis) [V].
- **iOS**: imprescindible macOS con Xcode 26 [V]. Opciones: Codemagic (500 min gratis/mes; M2 0,095 y M4 0,114 USD/min; plan fijo 3.990 USD/año) [V]; MacinCloud M4 dedicado 124,99 USD/mes [V]; MacStadium M4.S 149 USD/mes [V]; Scaleway M4 0,22 EUR/h con mínimo 24 h [V]; o Mac mini M4 propio [E].
- **Firmado**: Play App Signing con clave de subida separada; AAB obligatorio para apps nuevas desde ago-2021 [V]. Godot requiere Gradle build para exportar AAB y la contraseña del keystore debe coincidir con la de la clave [V].

## 5. Requisitos Google Play (vigentes a octubre 2026)

| Requisito | Detalle | Fuente |
|---|---|---|
| Target API | API 36 (Android 16) para apps nuevas y actualizaciones desde 31-08-2026; extensión hasta 01-11-2026 vía Policy Status; apps < API 35 solo visibles en dispositivos ≤ su target | [V] support.google.com/…/11926878 |
| Páginas 16 KB | Obligatorio desde 01-11-2025 si target ≥ Android 15; Unity 6 y SDKs nativos deben estar compilados compatibles | [V] developer.android.com/16kb-page-size |
| Play Billing | PBL 8 obligatoria desde 31-08-2026 (extensión 01-11-2026) → Unity IAP 5.x | [V] dev.to/mr_manushukla; discussions.unity.com |
| Formato | AAB + Play App Signing | [V] |
| Prueba cerrada 12/14 | Solo cuentas personales creadas tras 13-11-2023; **organizaciones exentas** | [V] support.google.com/…/14151465 |
| Data Safety | Declarar cada SDK que recoge/comparte (anuncios, analítica, crash, atribución) | [V] support.google.com/…/10787469 |
| Política de privacidad | URL pública obligatoria antes de completar Data Safety | [V] |
| Público objetivo | Si incluye menores → Families: solo SDKs de anuncios autocertificados y pantalla de edad neutral | [V] support.google.com/…/9867159 |
| Consentimiento | UMP SDK para EEE/UK (política de consentimiento de usuarios de la UE) | [V] developers.google.com/admob/unity/privacy/gdpr |
| Clasificación | Cuestionario IARC en Play Console | [V] (estándar) |
| Experiencia de anuncios | Políticas de Play sobre intersticiales inesperados y cierre | [NV] texto vigente no releído en esta ronda |

## 6. Requisitos App Store (fase 2)

- Apple Developer Program: 99 USD/año [V].
- Desde el 28-04-2026 toda subida a App Store Connect debe compilarse con Xcode 26 y el SDK de iOS 26 [V].
- PrivacyInfo.xcprivacy con Required Reason APIs; cada SDK de la lista de Apple debe incluir su manifiesto y firma [V].
- Etiquetas de privacidad (nutrition labels) coherentes con los SDKs [V].
- ATT: solicitar permiso antes de acceder al IDFA; AdMob/LevelPlay funcionan sin IDFA con menor eCPM [V/E]. LevelPlay exige iOS 13+ y Xcode 16+ (hoy 26+) [V].
- Revisión: Apple declara 90 % en < 24 h; seguimientos de 2026 miden 6-14 h de cola más ~1 h de revisión [V].
- iOS mínimo soportado por Unity 6: [NV] confirmar en la matriz de Unity 6000.x.

## 7. Localización

- Idiomas: ES (neutro, con revisión CO/PE/MX), PT-BR, EN. Segunda ola [E]: FR, DE, IT, ID, TR, RU.
- Herramientas: Unity Localization 1.5.13 (String/Asset Tables, Smart Strings, CSV/XLIFF/Google Sheets, pseudo-localización) [V]; Godot con CSV/PO y `tr()` nativos [V]; Lokalise desde 59 USD/mes [V]; Crowdin con webinar específico de juegos [V].
- Volumen estimado: 300-600 cadenas (UI, tutorial, tienda, nombres de frutas/ríos) [E]. Fichas de tienda localizadas por país en Play Console para BR, CO, PE, EC, BO, VE, MX, AR [E].

## 8. Lista de SDKs concretos (Unity, recomendación principal)

| SDK | Versión (2026) | URL |
|---|---|---|
| Google Mobile Ads Unity (incluye UMP) | 11.x (11.0 → GMA Android 25.0, iOS 13.0, UMP Android 4.0/iOS 3.1; últimas 11.3-11.5) [V, fechas con inconsistencia entre fuentes] | https://github.com/googleads/googleads-mobile-unity/releases |
| LevelPlay Unity | 9.5.1 (2026-08-19), Xcode 26+, iOS 13+ | https://docs.unity.com/en-us/grow/levelplay/sdk/unity/changelog |
| AppLovin MAX (mediación GMA) | plugin de mediación 8.0.3 → adaptador 13.0.1.0 | https://developers.google.com/admob/unity/mediation/applovin |
| Unity In App Purchasing | 5.x (Play Billing 7/8) | https://developer.android.com/google/play/billing/unity |
| Firebase Unity SDK | 13.15.0 (ago-2026), Unity 2021+ | https://firebase.google.com/support/release-notes/unity |
| Play Games plugin for Unity | 2.x (v2 API) | https://github.com/playgameservices/play-games-plugin-for-unity |
| GameAnalytics Unity | última estable | https://docs.gameanalytics.com |
| Unity Localization | 1.5.13 | https://docs.unity3d.com/Packages/com.unity.localization@1.5/manual/index.html |
| CI | game-ci/unity-builder | https://game.ci/docs/github/builder |

Equivalentes Godot: poingstudios/godot-admob-plugin (4.5+), godot-sdk-integrations/godot-google-play-billing (4.2+), Godot StoreKit 2 (Fundación), GameAnalytics/GA-SDK-GODOT 3.0.1 [V]. Flutter: google_mobile_ads 5.x (Flutter 3.27+), in_app_purchase, flame 1.x [V].

## 9. Estructura de carpetas propuesta (Unity)

```
amazon-fill/
  Assets/
    _Project/
      Art/{Containers,Fruits,Backgrounds/{BR,CO,PE,EC,BO,VE,GY,SR},UI,VFX}
      Audio/{Music,SFX}
      Shaders/{Liquid,Glass,River}
      Prefabs/{Tubes,Boosters,UI}
      Scenes/{Boot,Menu,Game}
      Scripts/{Core,Level,Liquid,Boosters,Economy,Ads,IAP,Save,Analytics,RemoteConfig,Localization,UI}
      Data/Levels/pack-0001.json … (Addressables)
      Localization/{Tables,Locales}
      Settings/{URP,Addressables,RemoteConfigDefaults.json}
    Plugins/{Android,iOS}
  Packages/manifest.json
  ProjectSettings/
  ci/{android-build.yml,ios-build.yml,fastlane/}
  docs/{research,design,store-listing/{es,pt-BR,en}}
  tools/level-generator/   (generador + solver offline, salida JSON)
```

## 10. Checklists de publicación

**Android (Google Play)**
1. Target SDK 36, min SDK 23 (Unity 6) [E], ARM64, IL2CPP, 16 KB verificado con `check_elf_alignment`.
2. AAB firmado con clave de subida; Play App Signing activo.
3. Unity IAP 5.x (PBL 8); productos creados y probados con licencia de prueba.
4. UMP configurado (mensaje GDPR + opción de "no vender/compartir" para EE. UU.); AdMob app-ads.txt publicado.
5. Política de privacidad en URL propia; Data Safety completado para GMA, LevelPlay/MAX, Firebase, GameAnalytics, Play Games.
6. Declaración de público objetivo (13+), cuestionario IARC, declaración de anuncios = "Sí".
7. Ficha: 30 caracteres de título, descripción corta/larga en ES/PT-BR/EN, 8 capturas por idioma, gráfico de funciones 1024×500, icono 512, vídeo opcional.
8. Pista interna → cerrada (sin requisito de 12/14 por ser organización) → producción escalonada 5 %→20 %→100 %.
9. Crashlytics activo; umbral de crash de Play 1,09 % como KPI [V].

**iOS (App Store, fase 2)**
1. Cuenta Apple Developer (99 USD/año), certificados y perfiles; Mac/CI con Xcode 26.
2. Build con iOS 26 SDK; PrivacyInfo.xcprivacy propio + de cada SDK; Required Reason APIs declaradas.
3. ATT con pre-prompt; SKAdNetwork IDs de LevelPlay/MAX en Info.plist.
4. Etiquetas de privacidad coherentes con Data Safety de Android.
5. Game Center opcional; iCloud KVS para guardado.
6. Capturas 6,9" y 6,5", descripción localizada, URL de soporte y privacidad.
7. TestFlight interno → externo → revisión.

## Implicaciones para Amazon Fill

1. Arrancar en **Unity 6 LTS + URP 2D** con plan Personal; crear el proyecto ya con target 36, ARM64 only, IL2CPP y Managed Stripping = Medium para medir el AAB desde la semana 1 y mantenerlo < 60 MB [E].
2. Hacer el **shader de líquido como activo central**: un único material con arrays de colores/alturas, onda amortiguada, menisco y máscara por recipiente autóctono; prototiparlo primero con los shaders de godotshaders/MinionsArt como referencia y validarlo en un Android de 2 GB RAM.
3. La animación de "sumergir y teñir" debe correr **después** de tener el nivel en memoria, durar 1,2-1,8 s, ser saltable desde el primer frame y acortarse por Remote Config (`tint_anim_duration`) según la tasa de salto medida.
4. **Niveles en JSON explícito** generados y resueltos offline por una herramienta propia (solver), con `minMoves` y `tier` para que Remote Config pueda recolocar dificultad sin nueva build; packs de fondos por país como Addressables remotos.
5. Monetización: GMA (AdMob) + LevelPlay o MAX como mediación desde el inicio; UMP integrado; Unity IAP 5.x. Parámetros de cadencia de anuncios y precios de comodines en Remote Config.
6. Guardado: Play Games Saved Games con resolución por "nivel máximo + timestamp"; mantener el blob < 100 KB [E] para holgura frente al límite de 3 MB.
7. Aprovechar la **exención de la cuenta de organización**: no hace falta la prueba cerrada de 12/14 días, pero sí conviene una cerrada de 1-2 semanas con 20-50 usuarios LatAm para medir crash rate y FPS en gama baja antes del lanzamiento escalonado.
8. Declarar público 13+ y evitar la ruta Families; revisar que el arte de frutas no sea "atractivo para niños" según los criterios de Play (riesgo de reclasificación).
9. Preparar la política de privacidad y la lista de SDKs declarados una sola vez y reutilizarla en Data Safety (Android) y Privacy Labels (iOS).
10. Para iOS, presupuestar desde ahora Codemagic pay-as-you-go o MacinCloud (~125-150 USD/mes) y mantener Privacy Manifests al día con cada actualización de SDK.

## Preguntas abiertas

- Confirmar el **min SDK** por defecto de Godot 4.5/4.6 (21 vs 24) y el iOS mínimo de Unity 6000.x [NV].
- Fechas exactas de las releases 11.3-11.5 del plugin GMA Unity: las fuentes muestran años inconsistentes [NV].
- Texto vigente de la política de "experiencia de anuncios" de Play (intersticiales inesperados, cierre) [NV].
- Decisión de producto: ¿LevelPlay o MAX como mediación? (afecta a adaptadores, Privacy Manifests y tiempo de integración).
- Decisión de producto: ¿Firestore multiplataforma o PGS + iCloud por separado para el guardado?
- ¿El estudio aceptará declarar 13+ (sin Families) aunque la estética de frutas pueda atraer a menores?
- Presupuesto de localización profesional vs. traducción interna + revisión nativa para PT-BR.
- Si se elige Godot: ¿se acepta renunciar a mediación con bidding (solo AdMob)?

## Fuentes

- https://unity.com/products/pricing-updates
- https://unity.com/blog/terms-update-runtime-fee-cancellation
- https://www.gamedeveloper.com/business/unity-is-killing-its-controversial-runtime-fee
- https://ziva.sh/blogs/godot-mobile
- https://github.com/poingstudios/godot-admob-plugin
- https://github.com/godot-sdk-integrations/godot-google-play-billing
- https://docs.godotengine.org/en/stable/tutorials/rendering/renderers.html
- https://docs.godotengine.org/en/stable/tutorials/export/exporting_for_android.html
- https://docs.godotengine.org/pt-br/4.5/tutorials/i18n/internationalizing_games.html
- https://github.com/GameAnalytics/GA-SDK-GODOT
- https://engineranked.com/article/cocos-creator-review-2026/
- https://dev.to/oceanviewgames/unity-vs-godot-vs-unreal-for-mobile-games-a-practical-comparison-1hj6
- https://dev.to/synfinity-dynamics-pvt-ltd/flutter-game-development-in-2026-can-you-build-real-games-with-flutter-peh
- https://docs.flutter.dev/ads
- https://docs.flame-engine.org/main/tutorials/basic_shader/takeaways.html
- https://appradar.com/blog/mobile-game-engines-development-platforms
- https://forum.defold.com/t/how-to-reduce-the-apk-size-increased-by-the-admob-extension/83145
- https://discussions.unity.com/t/libil2cpp-so-is-too-big/1603857
- https://80.lv/articles/breakdown-setting-up-a-fake-liquid-shader-in-unity/
- https://godotshaders.com/shader/liquid-in-mesh/
- https://godotshaders.com/shader/2d-liquid-fill-inside-sphere/
- https://courses.cs.ut.ee/2023/cg/fall/Main/Project-GlitteryWaterPuzzle
- https://codecanyon.net/item/liquid-sort-water-sort-puzzle-complete-unity-game-unity-ads/36495617
- https://www.academia.edu/126043517/Mobile_Application_Loading_Animation_Design_and_Implementation_Optimization
- https://developer-mdn.apple.com/design/human-interface-guidelines/patterns/loading
- https://developer.android.com/games/pgs/savedgames
- https://developer.android.com/games/pgs/unity/saved-games
- https://firebase.google.com/docs/remote-config/quotas-limits
- https://firebase.google.com/support/release-notes/unity
- https://costbench.com/software/product-analytics/gameanalytics/free-plan/
- https://bugnet.io/blog/unity-diagnostics-vs-crashlytics
- https://game.ci/docs/github/builder
- https://blog.codemagic.io/publishing-unity-android-apps/
- https://www.g2.com/products/codemagic/reviews
- https://pricingsaas.com/companies/macstadium
- https://www.greenmini.nl/compare/macincloud-alternative/
- https://www.whtop.com/plans/scaleway.com/146128
- https://support.google.com/googleplay/android-developer/answer/11926878?hl=en
- https://support.google.com/googleplay/android-developer/answer/14151465
- https://ontest.app/blog/google-play-12-testers-14-days-requirement-explained
- https://primetestlab.com/es/blog/google-play-publishing-requirements-2026
- https://support.google.com/googleplay/android-developer/answer/10787469?hl=en
- https://support.google.com/googleplay/android-developer/answer/9867159?hl=en
- https://developers.google.com/admob/unity/privacy/gdpr
- https://developer.android.com/16kb-page-size
- https://dev.to/mr_manushukla/play-billing-library-8-by-31-august-2026-every-removed-api-and-the-migration-path-58pn
- https://discussions.unity.com/t/urgent-unity-iap-does-not-support-google-play-billing-library-v7-august-2025-deadline/1661548
- https://developer.android.com/google/play/billing/unity
- https://ionic.io/blog/google-play-android-app-bundle-requirement-for-new-apps
- https://github.com/googleads/googleads-mobile-unity/releases
- https://docs.unity.com/en-us/grow/levelplay/sdk/unity/changelog
- https://developers.google.com/admob/unity/mediation/applovin
- https://developer.apple.com/news/?id=6lxhtioi
- https://developer.apple.com/forums/thread/806141
- https://www.choicely.com/tutorials/how-long-does-app-review-take
- https://docs.unity3d.com/Packages/com.unity.localization@1.5/manual/index.html
- https://www.capterra.com/compare/162858-163509/Lokalise-vs-Crowdin
- https://pt.crowdin.com/webinar/game-localization

## Complemento 6: Backend y servicios necesarios por versión del roadmap (v1.0 a v2.0) y comportamiento offline

Fecha: 2026-10-05. Misma convención [V]/[E]/[NV]. Método: el cupo de búsquedas web de la sesión estaba agotado, así que esta sección se construyó leyendo directamente más de 30 páginas oficiales (Firebase, Google Play, Unity, Heroic Labs, Microsoft Learn, Cloudflare, Apple). Las páginas de precios de playfab.com y la tabla de cloud.google.com/firestore no se pudieron leer completas (bloqueo/truncado); se indica dónde se sustituyeron por una fuente oficial equivalente.

**Respuesta corta.** (1) Hasta 1.2 inclusive **no hace falta servidor propio**: basta Firebase en plan Blaze (pago por uso con los mismos cupos gratuitos que Spark) más los servicios gratuitos de plataforma (Play Games Services, Play Asset Delivery, Play Integrity) y, para la liga, Unity Leaderboards (hoy gratis; trae cubetas, tiers y reset por cron nativos). Coste esperado: 0-10 USD/mes en 1.0 y 15-40 USD/mes en 1.2 [E]. (2) Los únicos componentes "de servidor" antes de 2.0 son dos Cloud Functions: el endpoint HTTPS de verificación de anuncios recompensados (SSV) y la verificación de compras contra la Google Play Developer API; ambas caben en el cupo gratuito de 2 M de invocaciones/mes [V]. (3) En 2.0 (equipos + chat) Firebase sigue bastando y es lo más barato (≈150-300 USD/mes a 50 k DAU [E]), pero grupos, moderación y agregación se construyen a mano; Nakama (open source, Apache-2.0) trae grupos, chat, torneos y validación de IAP hechos a cambio de operar un servidor (≈60-120 USD/mes autoalojado [E]; Heroic Cloud sin precio público [NV]); PlayFab Standard parte de 99 USD/mes de cuota base más medidores [V]. (4) Nada de esto rompe el "jugable offline": niveles, reto diario, calendario, racha y álbum deben funcionar sin red y sincronizar después; solo anuncios, IAP, ranking en vivo y chat son intrínsecamente online.

### 6.1 Función por función

| Función (versión) | ¿Servidor? | Opción recomendada | Coste/mes [E] | Sin conexión |
|---|---|---|---|---|
| Niveles, undo, tinaja extra (1.0) | No | JSON en build / Addressables locales | 0 | 100 % jugable |
| Reto diario por fecha (1.0) | No (opcional) | Puzzle determinista: `seed = hash(fecha UTC-5 + salt)` sobre el generador; tabla anual de dificultad en Remote Config (los valores cacheados sirven offline; intervalo mínimo de fetch 12 h [V]). Antimanipulación de reloj: comparar la fecha local con la del servidor del último fetch; si difiere > 24 h el premio queda "pendiente de validar" | 0 | Jugable con la fecha del dispositivo; premio y racha se reconcilian al reconectar |
| Calendario mensual y racha con perdón (1.0) | No | Estado local + copia en guardado en nube | 0 | 100 % |
| Guardado en nube (1.0) | Gestionado, gratis | PGS Saved Games en Android (sin coste, ver §3) + usuario anónimo de Firebase Auth desde 1.0 para tener un UID estable (50 k MAU gratis [V]) | 0 | PGS resuelve el conflicto al reconectar |
| Fondos-país y packs de niveles (1.0+) | Hosting gestionado | **Play Asset Delivery**: gratis, alojado por Google Play, modos install-time / fast-follow / on-demand, integrado con Addressables for Android; packs > 200 MB solo por Wi-Fi [V]. En iOS, On-Demand Resources: gratis, 20 GB totales, 512 MB por tag [V]. Alternativa multiplataforma: Addressables remotos (cualquier host HTTP sirve [V]) en Cloudflare R2 (10 GB gratis, 0,015 USD/GB-mes, egreso 0 [V]) o Unity CCD (50 GB/mes gratis, luego 0,08 USD/GB [V]) | 0 (PAD/ODR) · 0-5 (R2) | Los 3 fondos de 1.0 van install-time; el resto se descarga una vez y se cachea; si falta, fondo por defecto |
| Restauración de compras entre dispositivos (1.0) | Sí, mínimo | Cloud Function que verifica el `purchaseToken` con `purchases.products:get`, lo reconoce (acknowledge) en servidor y guarda el derecho en Firestore bajo el UID; `setObfuscatedAccountId` con ese UID para que Play detecte fraude [V, recomendación oficial]. "Sin anuncios" (no consumible) lo restaura la propia Play en cualquier dispositivo con la misma cuenta Google [E]; las semillas (consumibles) solo se restauran desde el guardado en nube | 0 (dentro de 2 M invocaciones) | La compra exige red; el derecho se cachea localmente tras validar |
| Antifraude de recompensas por anuncio (1.0) | Sí, mínimo | Endpoint HTTPS para **AdMob SSV**: AdMob llama con firma ECDSA, `user_id` y `custom_data`; las claves públicas rotan y no deben cachearse > 24 h [V]. El cliente concede la recompensa de forma provisional y el servidor la confirma o revoca. Añadir Play Integrity (10 000 peticiones/día gratis, ampliable desde Play Console [V]) vía Firebase App Check en Unity (Play Integrity en Android, DeviceCheck/App Attest en iOS; protege Firestore, Functions y Storage [V]) solo en tienda y reclamo de premios | 0 | Sin red no hay anuncio: ofrecer la alternativa con semillas |
| Eventos temporales y Pase Expedición (1.2) | Sí, ligero | Definición de eventos en Remote Config/Firestore precargada; progreso local; derechos del pase en Firestore tras validar la IAP; cierres programados con Cloud Scheduler (3 trabajos gratis por cuenta, 0,10 USD/trabajo/mes, requiere Blaze [V]) | 5-15 | Evento jugable si se descargó antes; el progreso sube al reconectar |
| Liga semanal + clasificaciones (1.2) | Gestionado | **Unity Leaderboards**: `BucketSize` (ligas de N jugadores), `TieringConfig` con estrategia score/rank/percent, `ResetConfig` con cron y `Archive`, `UpdateType` keepBest/keepLatest/aggregate [V]; "gratis por tiempo limitado" [V]; puntuación escrita desde Cloud Code (1 M invocaciones gratis [V]) para que sea autoritativa. Alternativa gratis sin cubetas: PGS Leaderboards (70 por juego, versiones diaria/semanal/total automáticas, protección antimanipulación solo Android, límites de puntuación y `Players.hide` [V]) | 0-10 | Puntos encolados localmente y enviados al reconectar (la cola la implementamos nosotros [E]); se muestra el último ranking cacheado con "actualizado hace X"; premio reclamable hasta 48 h tras el cierre |
| Equipos con chat y moderación (2.0) | Sí | Ver 6.3. Primera versión con **chat de frases predefinidas + emojis**: elimina moderación, obligaciones de UGC y casi todo el fraude [E] | 150-300 (Firebase) · 60-120 (Nakama autoalojado) | Lectura del último estado cacheado; aportes encolados y agregados en servidor |

### 6.2 Precios de referencia verificados

- **Firebase.** Firestore: 50 k lecturas, 20 k escrituras y 20 k borrados al día y 1 GiB gratis por proyecto; en nam5 0,06 USD/100 k lecturas, 0,18 USD/100 k escrituras, 0,02 USD/100 k borrados y 0,18 USD/GB-mes [V, ejemplo de facturación oficial]. Ese ejemplo (app de chat) cuesta 12,14 USD/mes a 5 000 DAU, 292 USD a 100 000 DAU y 2 952 USD a 1 M DAU [V]. Cloud Functions solo en Blaze: 2 M invocaciones/mes gratis, luego 0,40 USD/M; 5 GB de salida gratis, luego 0,12 USD/GB [V]. Auth 50 k MAU gratis; Remote Config, FCM, Analytics, Crashlytics y App Check sin coste [V]. Cloud Storage exige Blaze en proyectos nuevos (plazo 03-02-2026) [V]. Hosting: 10 GB y 360 MB/día en Spark; 0,15 USD/GB de transferencia en Blaze [V]. Offline: las escrituras se encolan y se sincronizan con "última escritura gana"; transacciones y timestamps de servidor exigen red [V].
- **Unity Gaming Services.** Cloud Save 5 GiB, 1 M lecturas y 1 M escrituras/mes gratis (luego 0,50 USD/GiB, 0,025 USD/10 k escrituras, 0,009 USD/10 k lecturas); Cloud Code 1 M invocaciones y 20 h de cómputo gratis, luego 1,50 USD/M; Leaderboards gratis por tiempo limitado; Authentication y Remote Config gratis; CCD 50 GB/mes gratis; Analytics 50 k MAU; Vivox hasta 5 000 PCU gratis [V].
- **Nakama.** Servidor Apache-2.0 con usuarios, almacenamiento, grupos, chat (salas, grupo y directo; persistente; sin filtro de palabrotas integrado: la moderación se hace con hooks del runtime), leaderboards, torneos, validación de compras Google/Apple/Huawei/Unity IAP con detección de recibo repetido (`seenBefore`), runtime Go/Lua/TypeScript; requiere CockroachDB o Postgres; SDKs Unity y Godot [V]. Heroic Cloud se dimensiona por CPU sin precio público; Satori desde 600 USD/mes; soporte Studio Basic 2 000 USD/mes [V]. Autoalojado: droplet 2 vCPU/4 GB a 24 USD/mes en DigitalOcean [V] + Postgres gestionado y backups ≈ 60-120 USD/mes [E].
- **PlayFab.** Planes: Free-to-start (Development Mode: 1 000 usuarios únicos, 1 M eventos, 5 M lecturas de perfil, 50 leaderboards; sustituido por Foundation Mode desde 11-03-2026, en preview y orientado a títulos que publican en Xbox con cuenta de Partner Center), Pay-as-you-go sin cuota base, Standard 99 USD/mes de base, Premium 1 999 USD/mes, Enterprise a consultar [V]. Precios unitarios de los medidores: playfab.com/pricing bloqueó la lectura automática [NV].
- **Google Play.** Play Developer API: 3 000 consultas/min por categoría [V]; Play Integrity 10 000 peticiones/día gratis [V]; PGS, Saved Games y PAD sin coste [V].
- **Moderación de texto (2.0).** Endpoint de moderación de OpenAI gratuito (`omni-moderation-latest`, 13 categorías) [V]; Azure AI Content Safety 5 000 registros de texto/mes gratis, precio del nivel S no visible en la página [V/NV]; PlayFab Party factura la moderación de texto por mensaje [V].

### 6.3 Presupuesto y stack por versión

| Versión | Escala supuesta [E] | Stack | Coste/mes [E] | Equipo backend [E] |
|---|---|---|---|---|
| 1.0 | 5 k DAU / 30 k MAU | Firebase Blaze (Auth anónimo, Analytics, Crashlytics, Remote Config, FCM) + 2 Cloud Functions (SSV e IAP) + PGS Saved Games + PAD | 0-10 USD | Ninguno dedicado; un Unity dev con TypeScript al 20 % |
| 1.1 | 10 k DAU | Igual; álbum en guardado local/nube | 0-10 USD | Igual |
| 1.2 | 20 k DAU / 100 k MAU | + Unity Authentication y Leaderboards (liga por cubetas) con Cloud Code; Firestore para derechos del pase y eventos (≈3 M escrituras y 12 M lecturas/mes ≈ 10 USD tras el cupo gratuito); Scheduler para cierres de liga | 15-40 USD | 0,25 FTE |
| 2.0 | 50 k DAU / 250 k MAU | A) Firestore (grupos, chat, agregados en Functions) ≈ 150 USD interpolando el ejemplo oficial + Functions 10-30 USD + moderación 0-30 USD. B) Nakama autoalojado 60-120 USD + 0,5 FTE DevOps. C) PlayFab Standard 99 USD + medidores | A 150-300 · B 100-200 · C > 150 USD | 0,5-1 FTE live-ops/backend en cualquier opción |

**Decisión Firestore vs PGS + iCloud (pregunta abierta de este documento).** En 1.0, PGS Saved Games como guardado principal en Android (gratis, cero código de servidor) y, desde el día 1, un usuario anónimo de Firebase Auth cuyo UID viaja como `user_id` en SSV y como `obfuscatedAccountId` en Billing; se enlaza con `LinkWithCredentialAsync` a Play Games en Android y a Game Center/Apple en iOS (el SDK de Unity expone `PlayGamesAuthProvider` y `GameCenterAuthProvider` [V, código fuente]). Así en 1.2 Firestore pasa a ser la fuente de verdad multiplataforma de derechos, liga y pase sin migración, y en la fase iOS no hace falta iCloud KVS. Un documento por usuario < 100 KB con escrituras agrupadas cada 3-5 niveles mantiene a 20 k DAU dentro del cupo gratuito de escrituras casi todo el mes [E]. El SDK de Unity de Firestore expone `PersistenceEnabled` y `CacheSizeBytes` (caché por defecto 100 MB) [V, código fuente], pero la documentación general solo nombra Android, Apple y web: probar el comportamiento offline en dispositivo antes de depender de él [V/E].

**Reglas de diseño offline.** (1) Todo lo que da progreso (niveles, reto diario, calendario, racha, álbum) se calcula en cliente y se reconcilia después: "última escritura gana" por campo y máximo para contadores. (2) Todo lo que da valor económico (recompensa por anuncio, compra, premio de liga, aporte a equipo) se concede en servidor o se marca provisional y se revoca si falla la validación; Google indica no conceder nada en estado `PENDING` y retirar con la Voided Purchases API [V]. (3) Mostrar siempre el último estado cacheado con marca temporal en lugar de bloquear pantallas: los 1 estrella del género castigan el "requiere conexión" (05). (4) Cada función online tiene sustituto offline: anuncio → coste en semillas; ranking → mi mejor marca; chat → solo lectura.

**Preguntas abiertas del complemento.** Precio futuro de Unity Leaderboards (hoy "por tiempo limitado") [NV]; precios unitarios de los medidores de PlayFab [NV]; coste real de Heroic Cloud para 2.0 [NV]; si PGS Leaderboards encola puntuaciones offline (no documentado) [NV]; decisión de producto: chat libre con moderación vs frases predefinidas en la primera versión de equipos.

### Fuentes del complemento 6

- https://firebase.google.com/pricing
- https://firebase.google.com/docs/firestore/pricing
- https://firebase.google.com/docs/firestore/billing-example
- https://firebase.google.com/docs/firestore/manage-data/enable-offline
- https://raw.githubusercontent.com/firebase/firebase-unity-sdk/main/firestore/src/FirebaseFirestoreSettings.cs
- https://raw.githubusercontent.com/firebase/firebase-unity-sdk/main/auth/src/swig/auth.i
- https://firebase.google.com/docs/auth/unity/play-games
- https://firebase.google.com/docs/auth/unity/anonymous-auth
- https://firebase.google.com/docs/app-check/unity/default-providers
- https://firebase.google.com/docs/remote-config/get-started?platform=unity
- https://firebase.google.com/docs/functions/schedule-functions
- https://firebase.google.com/docs/storage/faqs-storage-changes-announced-sept-2024
- https://developers.google.com/admob/unity/ssv
- https://developer.android.com/google/play/billing/security
- https://developers.google.com/android-publisher/quotas
- https://developer.android.com/google/play/integrity/overview
- https://developer.android.com/games/pgs/leaderboards
- https://developer.android.com/guide/playcore/asset-delivery
- https://developer.android.com/guide/playcore/asset-delivery/integrate-unity
- https://developer.apple.com/library/archive/documentation/FileManagement/Conceptual/On_Demand_Resources_Guide/PlatformSizesforOn-DemandResources.html
- https://docs.unity3d.com/Packages/com.unity.addressables@2.6/manual/RemoteContentDistribution.html
- https://unity.com/products/gaming-services/pricing
- https://docs.unity.com/en-us/leaderboards
- https://docs.unity.com/en-us/leaderboards/leaderboards-assets
- https://ugs-config-schemas.unity3d.com/v1/leaderboards.schema.json
- https://docs.unity.com/en-us/cloud-save
- https://developers.cloudflare.com/r2/pricing/
- https://github.com/heroiclabs/nakama
- https://heroiclabs.com/pricing/
- https://heroiclabs.com/docs/nakama/concepts/chat/
- https://heroiclabs.com/docs/nakama/concepts/iap-validation/
- https://learn.microsoft.com/en-us/gaming/playfab/pricing/pricing-overview
- https://learn.microsoft.com/en-us/gaming/playfab/pricing/development-mode
- https://learn.microsoft.com/en-us/gaming/playfab/pricing/meters/meters
- https://learn.microsoft.com/en-us/gaming/playfab/pricing/account-upgrades
- https://learn.microsoft.com/en-us/gaming/playfab/get-started/foundation-onboarding
- https://developers.openai.com/api/docs/guides/moderation
- https://azure.microsoft.com/en-us/pricing/details/cognitive-services/content-safety/
- https://www.digitalocean.com/pricing/droplets
