# Prueba completa del juego en el teléfono (2026-10-27, jornada 4)

El portátil sigue el encargo de la jornada 4 de `guion-prueba-telefono-completa.md` (`af685401`, ampliado con N4b en `be5a8f39`). Prueba `integracion-fase1-fase2` en `30d26487` con el SM-S938B por depuración inalámbrica, con emparejamiento explícito por IP. Esta entrega cubre el bloque 0, N4 y casi todo N4b.

Siguen pendientes:
- los 3 puntos del bloque 3;
- el bloque 4 salvo el 4.9;
- los bloques 5 y 7 a 23, con lo que quedó de N1: Reducir movimiento del sistema, la mañana y el mediodía con la hora cambiada, el 16:9 y los anuncios sin red o cerrados antes de tiempo.

Capturas y logcat en el portátil: `Logs/device/completa-j4/` (fuera de git). El logcat se lee filtrando `DebugAppCheckProvider`, «debug secret», «debug token», `appcheck:debugtokens` y `eyJ`. Se borró una captura que salía con la barra de estado del teléfono, con notificaciones personales.

**Nota sobre la jornada 3:** los anuncios simulados no se activaron entonces. Mi herramienta mandó `af_ads_fill` como entero (`--ei af_ads_fill 1`) y el juego lo ignora, porque espera texto: `--es af_ads_fill fill|no_fill|error`. Desde esta jornada se manda como texto.

## Resumen

| Bloque | Estado | Observaciones |
|---|---|---|
| 0. Preparación y builds | cumple | — |
| N4.1 Intro de la marca entera | cumple, con 1 observación | 1 |
| N4.2 TiTino de noche | cumple | — |
| N4.3 Viaje de Capi en el Mapa | cumple | — |
| N4.4 / N4b.10 Ensayo de release | cumple | — |
| N4b.1 Icono y logo | cumple en lo probado | — |
| N4b.2 Personajes 3D (D-058) | cumple en lo probado | — |
| N4b.3 Avatares del jugador | cumple, con 2 observaciones | 4, 5 |
| N4b.4 Informes de fallos | cumple, con 1 observación | 3 |
| N4b.5 AdMob con Unity Ads y sin MAX | cumple, con 1 observación | 2 |
| N4b.6 Play Age Signals | cumple | — |
| N4b.7 Países y decimales | cumple | — |
| N4b.8 Textos de Gemini rev2 | cumple | — |
| N4b.9 Web legal | no probado (es opcional y lo hace el dueño) | — |

## Verificación (bloque 0)

| Comando | Resultado |
|---|---|
| `git pull origin integracion-fase1-fase2` | `30d26487` («Integra tanda6/ci-pipefail-grep…») |
| `dotnet build dotnet/AmazonFill.slnx -warnaserror` | 0 errores |
| `dotnet test dotnet/AmazonFill.slnx` | 996 superados, 6 omitidos (1.002) |
| `python tools/validate_schema.py` | 116/116 PASS |
| `pwsh -NoProfile -File scripts/unity-build.ps1 -Method Dev` | Código 0 en 1.008 s: **`dev.apk` de 76,94 MB** (antes 76,07) |
| `ci/check-manifest.ps1 -Mode dev`, `ci/check-no-unity-iap.ps1`, `ci/check-no-applovin.ps1` y `bash ci/check-16kb.sh` sobre el `dev.apk` | OK los cuatro. **`AdsSdkInitializer`: no aparece** |
| `pwsh -NoProfile -File scripts/unity-build.ps1 -Method Release -Rehearsal -Apk` | Código 0: **`rehearsal.aab` de 59,81 MB** (antes 59,10) y **`rehearsal.apk` de 65,32 MB** (antes 64,62) |
| `size-gate`, `check-16kb.sh`, `check-manifest.ps1 -Mode dev`, `check-release-classes.ps1`, `check-no-unity-iap.ps1` y `check-no-applovin.ps1` sobre el AAB y el APK del ensayo | OK todos. `AdsSdkInitializer`: no aparece |
| `git status` tras los builds | limpio |

**Estado del teléfono:**
- 1080 × 2340 sin override;
- «No molestar» quitado;
- batería del 34 % al 21 % durante la jornada, a 27-31 °C.

## N4. Lo nuevo de la jornada 4

1. **Intro de la marca entera: cumple, con la observación 1.**
   - **Primera apertura** (`pm clear`), `brand_intro`:
     - `intro_ms` 2.625, `shown_ms` 2.500, `cut` false, `frames` 88;
     - `first_frame_ms` 111, `start_ms` 120, `emblem_ms` 93, `emblem_cache` miss;
     - `yields` 4, `loading_ms` 0, `wait_capped` false.
   - **Primera apertura**, `boot`: `load_ms` 1.301, `hold_ms` 2.432, `sloth_wake`.
   - **Primera apertura**, `boot_steps`: awake 93, profile 261, catalog 356, services 747, sloth 776, ads 844, game 950, screens 987, systems 1.301.
   - **La intro ya se ve entera:** 2,5 s animados con 88-117 fotogramas (en la jornada 3, 3 fotogramas y `cut` true). Se tiñe el río, entra «Tintalú» con un fundido y sale TiTino.
   - **Ráfaga de capturas** de la primera apertura (`n4/burst_intro/`):
     - la pantalla del sistema con el icono (0,56-0,70 s);
     - **petróleo liso, sin icono, de 0,83 a 1,80 s** (≈ 1,0 s; observación 1; en la jornada 3 eran 1,7 s);
     - la escena animada;
     - la pantalla de edad a los 5,3 s.
   - **Cinco arranques en frío** (en todos: `shown_ms` 2.500, `cut` false, `emblem_cache` hit, `emblem_ms` 10-15, `wait_capped` false):

     | Arranque | `TotalTime` | `intro_ms` | `frames` | `load_ms` |
     |---|---|---|---|---|
     | 1 | 318 ms | 2.538 | 116 | 750 |
     | 2 | 327 ms | 2.527 | 117 | 721 |
     | 3 | 306 ms | 2.526 | 115 | 752 |
     | 4 | 304 ms | 2.531 | 114 | 764 |
     | 5 | 320 ms | 2.539 | 116 | 749 |

     `load_ms` baja a 721-764 ms (790-826 en la jornada 3): **se recupera la subida de la observación 2 de la jornada 3.**
   - **Volver de segundo plano:** la intro no se repite (0 `brand_intro`).
   - **Inicio pulsado durante la intro de la primera apertura y vuelta:** llega a la pantalla de edad con 0 `E/Unity` (`intro_ms` 4.328, `frames` 9).
   - **Primera apertura completa:**
     - edad a los 4,6 s;
     - nivel 1 listo a los 9,7 s;
     - **primer toque a los 11,0 s** (objetivo, < 20 s).
   - **Caché:** 8,5 MB en el almacenamiento externo de la app (`afart`).
2. **TiTino de noche: cumple.** Era hora real, 22:32; no hizo falta cambiar la hora.
   - Al abrir el inicio:
     - `titino_sleep` con `asleep` true y `cause` shown;
     - TiTino tumbado con «Zzz» y **sin bocadillo**;
     - `titino_asleep` true en `AF_LAYOUT lobby`.
   - **Tocarlo:**
     - `asleep` false, `cause` tap, `note` fact;
     - dice un dato del perezoso y a los ~5 s vuelve a dormirse (`cause` sleep).
   - **Con «Mensajes de TiTino» apagado** (`settings_change` `sloth_messages` false):
     - se despierta sin hablar (`note` none) y vuelve a dormirse a los 4 s;
     - después se volvió a encender (`settings_change` `sloth_messages` true).
   - **Sin probar:** la mañana y el mediodía, porque piden cambiar la hora (lo hace el dueño, bloque 15).
3. **Viaje de Capi en el Mapa: cumple.**
   - **Mapa del 10** (perfil limpio, del 1 al 10 jugando):
     - tras «Siguiente» sale sola la Bitácora, y «Continuar» abre el Mapa (`map_open` `source` l10);
     - `map_play_ready`: `first_move_ms` 266, `ready_ms` 1.748, `by` time; un solo `stop_arrive` (s01);
     - en la ráfaga (`n4/capi10m/`), la canoa sube desde el Atlántico hasta Santarém;
     - «Jugar · Nivel 11» y «←» salen al llegar, a 1,9 s.
   - **Atrás** cierra el Mapa (`map_close`) y vuelve al inicio sin atascos.
   - **«Ver mapa» en la victoria del 20:**
     - `map_journey` de 10 a 20, de 1.500 ms;
     - con un toque durante el viaje: `taps` 1 y `ready_ms` 1.531, frente a los 1.748 sin toque.
     - El toque llegó hacia los 1,3 s (la ráfaga de capturas retrasa el script), y la cuenta cuadra con el ×2 de `JourneyTapSpeed`: **acelera, no salta**.
     - Ojo al leer `elapsed_ms`: mide tiempo de viaje, no de pared, así que sale ~1.500 con toque o sin él.
     - Atrás vuelve a la victoria.
   - **Segundo plano a mitad del viaje** («Ver mapa» del 30, Inicio de Android a los 0,9 s y vuelta a los 3 s): `map_journey` `end` y `map_play_ready` con `by` resume, y al volver se ven «←» y «Atrás» (`n4-capi-bg-vuelta.png`).
   - **Sin probar:** TalkBack (lo enciende el dueño).
4. **El ensayo de release (`rehearsal.apk`; también N4b.10): cumple.**
   - Primer arranque:
     - `[AF.Ads] proveedor: google (bloques de prueba)`;
     - edad, y el consentimiento con la frase de los informes de fallos;
     - nivel 1 con la banda de TiTino.
   - Del 1 al 10 ganados. Después:
     - la Bitácora y el Mapa del 10;
     - en las capturas, durante el viaje no se ven «Jugar» ni «←», y al llegar sí;
     - el inicio del 11 con el disco del avatar (tortuga) y su punto;
     - «Your avatar» con la rejilla.

     **Ningún hueco de arte** en el icono, el emblema, los personajes del inicio y del Mapa, el avatar ni la rejilla (`n4r/`).
   - Ajustes lleva «Send crash reports», encendido.
   - Logcat del proceso:
     - **0 líneas `AF_*`**;
     - como errores, solo `E/Unity` y `E/firebase` con `clazz` (la clase de depuración de App Check, que se quita en release), lo esperado.
   - Después se volvió a instalar el `dev.apk`.

## N4b. Lo añadido el 2026-10-27

1. **Icono y logo: cumple en lo probado.**
   - En «Información de la app» (`n4r/r-icono-info-recorte.png`, recortada sin la barra de estado), el icono lleva la máscara de One UI:
     - la cuia vierte açaí;
     - el río tiene **dos meandros**: el violeta y el camino café con leche;
     - fondo petróleo, sin bordes blancos.
   - El emblema de la intro se ve en la ráfaga.
   - **Sin probar:** el monocromo y el cajón de apps. No se fotografían pantallas personales; los mira el dueño.
2. **Personajes 3D (D-058): cumple en lo probado.**
   - TiTino (dormido, despierto, en la victoria colgado de la rama) y Capi en el Mapa se ven con volumen y sin contorno grueso.
   - Los 16 avatares se reconocen a 112 dp (cabecera) y en la rejilla.
   - En la Liga, a 32 dp, se reconocen por la silueta.
   - En la línea de la Liga de la victoria, a 24 dp (`n4-victoria-liga-61.png`), la tortuga se reconoce.
   - **Fluidez** con `--ei af_bench_morpho 1` (`AF_PERF frames`, 600 fotogramas):
     - nivel 2: 59,77 fps, `p95_ms` 16,65, `max_ms` 33,3, `late` 3;
     - nivel 3: 59,97 fps, `p95_ms` 16,65, `max_ms` 33,29, `late` 1;
     - nivel 82: 60,08 fps, `p95_ms` 16,65, `max_ms` 16,66, `late` 0.

     En los tres sale `morpho_running` false.
   - **Memoria:**
     - `AF_PERF mem` en el inicio: `sys_mib` 497-652;
     - en el Mapa: `sys_mib` 638, `tex_mib` 48;
     - tras el 82, PSS de 537,7 MB (RSS 653,9 MB), por debajo de los 591,5 MB de N3.
   - **Sin probar:**
     - la comparación una a una con `docs/art/personajes-3d/piloto/despues/` y `fase2/despues/`;
     - la tropa y el churuco de cerca, Rosa y la morpho;
     - Alto contraste.

     Las capturas quedan en `n4/` para la mirada del dueño en el bloque 22.
3. **Avatares del jugador: cumple, con las observaciones 4 y 5.**
   - **Perfil limpio:**
     - en el primer arranque se asigna uno de los libres: `avatar` con `cause` assign, `id` turtle y `chosen` false;
     - del 1 al 9 no hay inicio ni disco;
     - al ganar el 10, el inicio del 11 lleva el disco `avatar` con el punto.
   - **«Tu avatar»:**
     - `cause` open;
     - cabecera grande y rejilla de 4 columnas, con los 6 libres primero y luego los 10 de hito en silueta, con su chapa y el texto corto.
   - **Elegir uno libre** (guacamayo): `af_avatar_select` (`free` true, `first` true) y `avatar` con `cause` select y `chosen` true. Al cerrar (`cause` close), el disco del inicio cambia y el punto se va.
   - **Tocar uno bloqueado:** sale un globo con la frase entera del hito y no se elige.
     - Globo del jaguar: «Win your first Very Hard level.».
     - Globo del martín pescador: «Gana el Reto del río 7 días seguidos.», «Win the River Challenge 7 days in a row.» y «Vença o Desafio do Rio 7 dias seguidos.».
   - **Desbloqueo por hito** con `af_max_level`:
     - con 60: `cause` unlock, `unlocked` [dart_frog, jaguar], `new` true;
     - con 80: [caiman, dart_frog, jaguar];
     - en los dos, el punto en el inicio y la chapa «New» en la rejilla (`n4-unlock-60-*`, `-80-*`).
   - **Ajustes:**
     - la fila «Avatar» está arriba del todo (`avatar_row`);
     - abre la misma pantalla (`cause` open), y Atrás la cierra (`cause` close).
   - **Liga** (`--ez af_league true --ez af_league_fake true`, `--ei af_max_level 60`):
     - en la fila fija de abajo sale tu avatar (tortuga);
     - en el top, avatares a 32 dp o el disco neutro.
     - Tocar **el disco de tu fila** abre «Tu avatar» (`league_open`, y `avatar` con `cause` open). Tocar el resto de la fila no hace nada (observación 4).
     - La fila propia dice «Tú» dos veces (observación 5).
     - Al ganar el 61: `league_win` con 10 puntos y la línea «+10 puntos de Liga» con el avatar a 24 dp.
   - **Al 130 %** (`--ei af_font_scale 130`): la rejilla pasa a 3 columnas y los nombres caben en dos líneas («Martín pescador · 7 Retos seguidos», «Oso hormiguero · Mundo 4 · Trapecio»).
   - **Sin probar:** 320 dp, TalkBack y Reducir movimiento (los cambia el dueño).
4. **Informes de fallos: cumple, con la observación 3.**
   - **Primer arranque** (`pm clear`): la pantalla de consentimiento dice «Crash reports help us improve the game. You can turn them off in Settings.» (`n4-consent-frase.png`). La frase también sale en el ensayo.
   - **Ajustes › «Send crash reports»:**
     - está bajo «Privacy and data» (`crash_reports`) y encendido por defecto;
     - al apagarlo, `settings_change` con `crash_reports` y `false`;
     - **apagado y relanzado** (`force-stop` y abrir), sigue apagado (`n4-fallos-relanzado-apagado.png`).
   - Al relanzar, el lado Java de Crashlytics se sigue inicializando («Initializing Firebase Crashlytics 20.1.1»). El juego aplica el ajuste después, desde `FirebaseAnalyticsSink` (`IsCrashlyticsCollectionEnabled`).
   - Para volver a encenderlo hubo un toque de más (observación 3); el `pm clear` posterior dejó el interruptor encendido, por defecto.
   - **Sin probar:** TalkBack.
5. **Anuncios, AdMob con Unity Ads y sin MAX: cumple, con la observación 2.**
   - **Proveedor:** `[AF.Ads] proveedor: google (bloques de prueba)` en el `dev.apk` y en el `rehearsal.apk`.
   - **Unity Ads no arranca antes de la edad:**
     - con `pm clear` y `logcat -c` no hay ninguna línea `UnityAds` hasta contestar;
     - la primera sale al pulsar «Accept all», a las 22:31:40.657 (escribe `UnityAdsStorage-*.json`), antes de `consent` `done` (.692).
   - **AppLovin:** 0 líneas en todo el arranque, antes y después del consentimiento.
   - **Intersticial de prueba**, paso a paso y con captura antes del toque:
     - en el paso del 28 al 29 sale «This is an interstitial test ad»;
     - se cerró con la X, sin tocar el anuncio;
     - `ad_impression`: `ad_platform` AdMob, `ad_source` «Reservation campaign», `ad_format` interstitial, `ad_unit_name` interstitial_post_level;
     - además, `af_ad_request` filled y `af_ad_shown`.
     - Antes, `ad_check` daba `blocked:BelowFirstLevel` hasta el 20 y `blocked:Warmup` del 26 al 27.
   - **Recompensado de prueba** (Doble cosecha en la victoria del 31), paso a paso:
     - vídeo de Google Ads; **no se tocó «Install»**;
     - a los ~30 s, «Reward granted» y la X;
     - `ad_impression` con `ad_source` «Reservation campaign», `ad_format` rewarded y `ad_unit_name` rv_double_harvest;
     - `af_ad_reward` `granted` provisional, `earn_virtual_currency` +20 de `double_harvest` y `double_harvest` con `harvest` 40;
     - el juego pasa solo al 32.
   - Como dice el guion, con los bloques de prueba no entra la mediación de Unity Ads.
   - **Ad Inspector:** no se probó, porque el dueño no registró el teléfono como dispositivo de prueba.
6. **Play Age Signals: cumple.**
   - Primer arranque con 18+, `consent` `done`:
     - `age_band` 18+, `band_from_signal` false;
     - `country` US, `country_source` sim;
     - analytics true, npa false, tfcd false, rating G.
   - El servicio de PlayCore AgeSignalsService se enlaza al arrancar.
   - No sale «no se pudo pedir la señal de edad». Sin señal, se usa la banda autodeclarada, como se esperaba.
7. **Países y decimales: cumple.**
   - **Nombres de país en la Liga** (top global; capturas `n4-liga-es-*`, `n4-liga-en-*` y `n4-liga-pt-BR-*`):
     - los de fuera de la tabla salen con su nombre y una chapa con el código en lugar de bandera: España/Spain/Espanha, Alemania/Germany/Alemanha, Francia/France/França;
     - Surinam/Suriname y Guyana/Guiana, con bandera;
     - en pt-BR, «Colômbia» y «Peru».
   - **Decimales:** con `--ez af_fake_store true`, el Mercado en COP sale sin decimales (`n4-mercado-*`): «COP 3,900» (Puñado), «COP 9,900» (Bolsa), «COP 5,900» (Cántaro).
   - El separador de miles es la coma en los tres idiomas («COP 3,900», «1,000 sementes», «1,980» en la Liga). Es lo diseñado: `LocNumber` toma el formato de la región del teléfono (`persist.sys.locale` en-US), no del idioma del juego.
   - **El país legal:** `sim` (US). Ver `play` pide quitar la SIM; lo decide el dueño.
8. **Textos de Gemini rev2: cumple.** Con `--es af_lang en` y `pt-BR`, más `es-419` para comparar:
   - «Your avatar» / «Seu avatar»: cabecera, celdas («Rising River 10», «Enchente 10», «7 Desafios seguidos»…) y globo;
   - la fila «Avatar» de Ajustes;
   - «Send crash reports» / «Enviar relatórios de falhas»;
   - «Privacy and data» / «Privacidade e dados», y «Correct my age» / «Corrigir minha idade»;
   - en pt-BR, la Liga sin puntos: «Vença um nível para entrar na Liga».

   Ningún texto cortado, vacío ni en otro idioma.

## Observaciones

1. **Alta. En la primera apertura sigue habiendo ≈ 1,0 s de petróleo liso, sin icono**, entre la pantalla del sistema (0,56-0,70 s) y la escena (desde 1,80 s). Mejora frente a los 1,7 s de la jornada 3, y la intro ya se ve entera (2,5 s). Pero el guion pide anotar como alta un color plano que se nota, y un segundo se nota. Solo pasa en la primera apertura: con la caché del emblema (hit), los arranques siguientes no lo muestran en la ráfaga.
2. **Baja. El plugin de Google Mobile Ads lanza `FileNotFoundException: Could not load the file 'GoogleMobileAdsNative.Common'`** desde `MobileAdsClient.Initialize` en cada arranque con anuncios.
   - El propio plugin la manda por su telemetría (`RCS JSPB payload … Batch sent successfully`).
   - El juego sigue sin problema y los anuncios se ven.
   - Para el escritorio: ver si falta un ensamblado de los anuncios nativos o si basta con excluirlo.
3. **Media. «Corregir mi edad» ofrece «Menor de 13» y con un toque bloquea el perfil para siempre**, sin confirmar.
   - Pasó por un toque fuera de sitio: la lista de Ajustes estaba en movimiento y la fila de debajo de «Enviar informes de fallos» es «Corregir mi edad».
   - El toque abrió la pantalla de edad, y el siguiente cayó en «Under 13»: `consent` `blocked`, «Ask an adult to install the game», también al relanzar. Solo `pm clear` lo deshace.
   - El guion (bloque 21, «Corregir mi edad: a 13-17, una sola vez») hace pensar que la corrección solo debería bajar a 13-17, o al menos confirmar antes de bloquear.
   - Lo decide el dueño.
4. **Baja. En la Liga solo el disco de tu fila abre «Tu avatar»**; el guion dice «tocar tu fila». El resto de la fila no responde.
5. **Baja. La fila propia de la Liga dice «Tú» dos veces**: «Tú» como nombre y «Tú» como chapa, delante de «Gana un nivel para entrar en la Liga». Pasa también en en («You · You») y pt-BR («Você · Você»).

## Notas de la herramienta (no son defectos del juego)

- Durante el nivel 10 el proceso dejó de escribir en logcat. Tras relanzar la app volvió a escribir; no se repitió.
- El jugador automático relanza la app al empezar y pierde los extras. Por eso, del 21 al 28 los anuncios fueron los de prueba de Google y no `no_fill`; de ahí salió el intersticial real del 28, que se cerró paso a paso.

## Sin probar en esta jornada

- **Bloques pendientes:** los 3 puntos del bloque 3, el bloque 4 salvo el 4.9, y los bloques 5 y 7-23.
- **Lo que hace el dueño:**
  - TalkBack;
  - Reducir movimiento del sistema;
  - 320 dp y el 130 % del sistema (se usó `af_font_scale`);
  - la mañana y el mediodía de TiTino;
  - el icono monocromo y el cajón de apps;
  - Ad Inspector;
  - la web legal (N4b.9);
  - la cuenta sin SIM para `country_source` play.
- La comparación de los personajes con las capturas de referencia de D-058, Alto contraste, y Rosa y la morpho de cerca.
