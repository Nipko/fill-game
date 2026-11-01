# Encargo al equipo Mac: Tintalú en iOS (2026-10-31)

Lo escribió el integrador (escritorio) por decisión del dueño (**D-067**), el día del lanzamiento de la 1.0 en Google Play. Palabras del dueño: «antes de empezar con publicidad paga […] vamos a centrarnos en generar contenido y en la versión para iOS, porque si nos va bien nos verán en ambos SOs […] ya tengo una Mac mini para empezar el desarrollo».

La base técnica es `docs/dev/ios-hoja-de-ruta.md`. Este encargo la adapta a que **ahora hay una Mac** y fija el orden. Léelos los dos enteros, con `CLAUDE.md`, antes de tocar nada.

## 1. Quién eres y qué reglas te obligan

**Tu papel.**
- Eres la sesión de Claude Code de la **Mac mini** del dueño: Apple silicon y 24 GB de RAM. Pruebas en su **iPhone 16 Pro Max**.
- Lideras la versión de iOS. El escritorio es el **único integrador**: revisa, verifica y fusiona tus ramas `mac/*` en `integracion-fase1-fase2` y en `main`. Tú solo empujas tus ramas.

**Reglas.**
- `CLAUDE.md` entero y sus **reglas duras**:
  - el núcleo sin motor y determinista;
  - nada de YAML de Unity a mano;
  - nada nuevo sin aprobación del dueño: ni paquetes, ni SDK, ni permisos, ni versiones;
  - nunca secretos;
  - nada a las tiendas;
  - un Editor por carpeta;
  - git sin force;
  - ante una ambigüedad, preguntas.
- Las reglas por ruta de `.claude/rules/`.
- Las decisiones abiertas se anotan con la recomendada aplicada y [P] (regla del dueño: «si tienen recomendada vamos con esa»).

**Lo que en la Mac cambia respecto a `CLAUDE.md`:**
- El Editor es el **6000.3.25f1 para macOS (Apple silicon)**, con **iOS Build Support** (además de Android Build Support si se quiere compilar Android ahí).
- Las rutas y los scripts `.ps1` del escritorio son de Windows. En la Mac se usa `unity` (la CLI, si está) o `/Applications/Unity/Hub/Editor/6000.3.25f1/Unity.app/Contents/MacOS/Unity -batchmode`.
- PowerShell 7 existe en macOS (`brew install powershell`). Si los scripts de `scripts/` y `ci/` no corren tal cual, haz versiones `.sh` en una rama aparte. No cambies el comportamiento en Windows.
- **Xcode 26** con el SDK de iOS 26 (lo exige Apple para subir desde el 2026-04-28).
- **CocoaPods**: EDM4U genera el Podfile de Firebase y Google Mobile Ads.

## 2. Lo que prepara el dueño en la Mac (fase 0, antes de que empieces)

1. **Xcode 26**, desde la App Store, y abrirlo una vez para aceptar la licencia y descargar el componente de iOS.
2. **Unity Hub**, con su cuenta de Unity y la licencia Personal; desde el Hub, el **Editor 6000.3.25f1** con **iOS Build Support** (y Android, opcional).
3. **Homebrew** y, con él, `git`, `git-lfs`, `gh`, `cocoapods`, `powershell` y el **.NET SDK 10** (el de `global.json`).
4. **Claude Code** en la Mac, con su cuenta.
5. Clonar `github.com/Nipko/fill-game` con LFS (`git lfs install`, después `git clone` y `git lfs pull`), y `gh auth login`.
6. **El iPhone 16 Pro Max:**
   - Ajustes › Privacidad y seguridad › **Modo de desarrollador** activado. Aparece después de conectarlo a Xcode por primera vez.
   - En Xcode › Settings › Accounts, su **Apple ID gratuito**. Con él, Xcode instala el juego en el iPhone por cable: la firma caduca a los 7 días y se reinstala con un clic.
7. **Más adelante**, cuando llegue el D-U-N-S de Parallext:
   - la cuenta de **Apple Developer como organización**, Parallext, LLC [P] (99 US$/año);
   - el Bundle ID `com.parallext.tintalu`;
   - la clave del API de App Store Connect (`.p8`), que la pone el dueño, nunca un agente;
   - el acuerdo de apps de pago con los datos fiscales y bancarios;
   - el Small Business Program (15 %).

   Hasta entonces se trabaja con el Apple ID gratuito: todo menos TestFlight, los productos reales de App Store Connect y la publicación.

## 3. Decisiones (las de §6 de la hoja de ruta, adaptadas a la Mac)

Decididas con la recomendada (D-067):

| # | Decisión |
|---|---|
| I-1 | **Se empieza iOS ya**, con todas las fases, y Android 1.0 sigue en producción. La publicidad pagada espera a que iOS esté publicado (D-067) |
| I-2 | **Se compila en la Mac mini:** Unity exporta el proyecto de Xcode y Xcode compila, firma y lo instala en el iPhone. Para TestFlight y la tienda, se archiva y se sube desde la misma Mac. Más adelante, si conviene, la Mac puede ser un *runner* propio (*self-hosted*) de GitHub Actions para `ios.yml` [P]; la ruta de GitHub o Codemagic de la hoja de ruta queda de respaldo |
| I-3 | Compras con un **plugin propio de StoreKit 2** en Swift, como D-040 en Android. Unity IAP no entra |
| I-4 | **Guardado solo local** en la primera versión de iOS; el guardado común Android ↔ iOS llega con el de AWS (D-035) |
| I-5 | **ATT con un plugin propio** pequeño (sin `com.unity.ads.ios-support`) |
| I-6 | iOS Build Support en la Mac (obligatorio); en el escritorio, opcional |
| I-7 | **El mismo alcance que la 1.0 de Android:** Liga, anuncios, compras y todo el mundo; sin PAD |
| I-8 | **La cuenta de Apple, de Parallext, LLC** cuando tenga su D-U-N-S. **Nada de abrirla como persona**: en la tienda saldría el nombre personal y pasarla después a empresa es lento |

## 4. Plan de tareas

Cada fase termina con algo que el dueño ve en su iPhone.

| Fase | Qué | Necesita la cuenta de pago |
|---|---|---|
| **M0** | Comprobar la Mac (versiones de Xcode, Unity, .NET y CocoaPods), compilar el proyecto en el Editor de macOS y correr EditMode y PlayMode. Informe en `docs/dev/mac-setup.md` | no |
| **M1 «Hola, iPhone»** | `BuildIos.ExportXcode`, `ProjectSetup.ApplyIosDefaults` (iOS 15 mínimo [P], ARM64, Metal, orientación vertical, nombre, build), ramas `UNITY_IOS` que eligen las implementaciones falsas (sin compras, sin nube, sin anuncios), icono provisional y pantalla de lanzamiento. **Se juega en el iPhone por cable:** niveles 1-10, Mapa, zona segura (Dynamic Island), 120 Hz, audio | no |
| **M2 Plataforma** | Firebase iOS (Analytics, Crashlytics, Remote Config, Auth; App Check con App Attest), notificaciones, hápticos, escala de texto, idioma, ajustes, país legal desde la tienda (`Storefront`), la pantalla de edad igual, y el icono definitivo con las variantes de iOS (`exportar.py --ios`, D-045) | no. El `GoogleService-Info.plist` lo pone el dueño |
| **M3 Monetización** | Plugin de StoreKit 2 (con un `.storekit` local para probar sin cuenta), verificación en el backend (App Store Server API y notificaciones v2; `infra/`, solo validar sin credenciales), Google Mobile Ads iOS con el aviso previo y ATT, `SKAdNetworkItems`, UMP y Unity Ads | las pruebas *sandbox* y los productos reales, sí |
| **M4 Cumplimiento** | `PrivacyInfo.xcprivacy`, Declared Age Range API, `ITSAppUsesNonExemptEncryption`, las capturas de 6,9" con el iPhone 16 Pro Max y los textos de la ficha | sí, para rellenarla |
| **M5 QA y envío** | Regresión de release en iOS, TestFlight solo para el dueño, envío a revisión. **Publica el dueño** | sí |

**Orden:** M0 → M1 → M2 → M3 → M4/M5. Las tres primeras no esperan a la cuenta de Apple.

**Lo que no se toca:** el comportamiento de Android. Todo lo nuevo va detrás de `#if UNITY_IOS` o en ensamblados y plugins propios de iOS. Los tests de Android tienen que seguir en verde. Si algo pide cambiar el GDD (08 §11, §12.2, §13), anótalo en `docs/dev/decisiones.md` bajo D-067, y el integrador lo lleva al GDD.

## 5. Cómo entregas

- Ramas `mac/<tema>` desde `origin/integracion-fase1-fase2`, empujadas a `origin`, sin force.
- Estado en `docs/dev/traspaso/mac-estado.md`: arriba el resumen y después una entrada por tarea, la más reciente primero. Al final, «Enganches para el integrador»: lo que el escritorio tiene que fusionar o probar.
- Cada entrega lleva los comandos que ejecutaste y sus resultados exactos (CLAUDE.md, «Comandos de verificación»).
- Las capturas y vídeos del iPhone, en `Logs/device-ios/` (ignorado). En el repositorio, solo lo que haga falta para decidir.

## 6. Frase para pegar en Claude Code de la Mac

> Lee `docs/dev/traspaso/mac-ios.md` y `docs/dev/ios-hoja-de-ruta.md` enteros (y `CLAUDE.md`). Eres el equipo Mac que lleva Tintalú a iOS (D-067). Empieza por M0 en la rama `mac/m0-setup` desde `origin/integracion-fase1-fase2`, aplica las recomendadas salvo lo que dependa del dueño, y anota el avance en `docs/dev/traspaso/mac-estado.md`.
