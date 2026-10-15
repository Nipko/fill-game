# Capítulo 8 · Arquitectura técnica y publicación

Fecha: 2026-10-06 (actualizado el mismo día con el plan de instalación aprobado y las decisiones del dueño; revisión cruzada del 2026-10-06; primera versión 2026-10-05; actualizado el 2026-10-08 con D-005 y D-006: secciones 2.2, 5.2, 9 y 14.1; y con D-008, bucle de partida como Magic Sort: secciones 2.2, 2.3, 8.2 y la columna «Uso» de tres filas de 9, sin claves nuevas; actualizado el 2026-10-09 con D-010, «Llenar y teñir» en lugar de «Sumergir y teñir»: sección 5.3 y el nombre en la columna «Uso» de 9, sin claves, defaults ni tiempos nuevos; actualizado el 2026-10-11 con D-007, mecánicas del río, y D-009, dificultad escalonada, decididas por el dueño el 2026-10-08: secciones 2.2, 4.1 (solo el texto; el ejemplo no cambia), 4.4, 5.2, 6.2, 8.2 y 9, donde `mech_enabled` y `mech_debut_level` ganan `matapi` y el 163 pasa a ser el estreno del Brote por el contrato 1, más las preguntas abiertas 11 y 12 y la fila 02 de Dependencias; actualizado el 2026-10-12 (paso 2 del traspaso) con **D-011** (tropa de la intro: sección 5.3; avance tras la Cosecha doble: columna «Uso» de `victory_autonext_s` en 9) y **D-012** (guardado por vertido con escritor en segundo plano y su objetivo: secciones 5.4 y 11; patrones: secciones 5.1 y 5.2 y columna «Uso» de dos filas de 9, sin claves nuevas), con la lista única de estrenos de 02 §9.1 en la fila de `mech_debut_level`, la sección 5.2 ([P] de los tiempos de revelado de Aguas turbias, 1.200 y 600 ms) y las preguntas abiertas 11-13; actualizado el 2026-10-13 con el estado de la tanda 2 (D-012 implementada entera en `94905b8`): sección 5.1, donde el icono de fruta del prototipo lo pinta el shader del líquido, con la pregunta 44 de GDD.md §6.3; actualizado el 2026-10-13 (tanda 3) con **D-013**, el Mapa del río: secciones 2.2, 3, 4.3 (el campo `progress.map_seen_level`), 4.5 (nueva: `af-river-map/1` y `af-river-stops/1`, contratos 29 y 30), 6.1, 6.2 (fila `world_w05` … `world_w13`: los kits de 201-700), 8.2 (eventos del Mapa), 9 (claves del Mapa, con la pregunta 45 de GDD.md §6.3), 11 y 13.1 (comprobación 17); actualizado el 2026-10-14 (tanda 3, T54) con **D-015**, la excepción de 3 muestras de la pregunta 44 (sección 5.1), **D-016**, TiTino (secciones 4.3, `settings.sloth_messages` y `sloth.last_line`; 8.2, `af_settings_change`; 9, las claves `ui_sloth_*`), **D-017**, Frutas del río (secciones 4.6, nueva: `af-facts/1`, contrato 31; 8.2, `af_fact_page_open` y el `fact_id` de la línea de fruta; 9, la cadencia de la línea de fruta, con 0 = apagada y 2 = una cada dos niveles), **D-018**, idiomas (secciones 2.2, `AF.Localization` con la fachada `Loc` y Unity Localization 1.5.13 en la fase 3; 2.3; 2.4, sin `tools/loc-sync`; 4.3, `settings.language` = `auto`; 4.7, nueva: tablas de textos, contrato 32; 6.1, fuentes; 8.1, `device_language` y `app_language`; y, con D-017, la pregunta abierta 14, nueva: las claves de las tablas de textos, pregunta 49 de GDD.md §6.3), **D-019**, la Tinaja (secciones 4.1, 4.4, 5.1, 8.2 y 9: `mech_*` con `tinaja`, `ui_tinaja_show_contents` y la nota de que `lvl_murky_max_hidden_units` no rige la Tinaja), **D-020**, las preguntas 31, 32 y 37 (secciones 5.4 y 9, y Preguntas abiertas 11 y 12), y el presupuesto de texturas de los kits de D-013 (sección 5.4); actualizado el 2026-10-14 (T58) con **D-022** (secciones 4.6 y 9: la línea de fruta con su clave propia, `ui_fruit_line_every_n_levels`, pregunta 45; 4.7: la forma de las claves y las columnas de las tablas de textos, pregunta 49; Preguntas abiertas 14, cerrada) y el 323 en la copia de la lista única de estrenos (sección 9, `mech_debut_level`; revisión de T54); actualizado el 2026-10-14 (D1) con **D-023**, las decisiones de detalle de la tanda 4 (secciones 4.5: los extras [P] de P-M1, la codificación en base64 con deltas, los esquemas, los nombres por convención con la clave de `country_name` y los textos del Mapa en `facts.csv`; 4.7: LF y orden ordinal; y 14.2: la pseudolocalización `en-XA` al 35 % [P]; y, con FIXD, 4.1: la firma de las variantes de `pack_schema` 3 en las filas `schema`, `attrs` e `intro_mechanic`, D-023, punto 5; la frase de la máscara de la tinaja en la fila `relief`, D-023, punto 6, la cambia P1); actualizado el 2026-10-14 (FIXD) con **D-024**, las reglas de la Hoja atada (sección 4.1, fila `attrs`), y con los pendientes de la revisión de T58: la regla de la cadencia de la línea de fruta (secciones 4.6 y 9, con el default `1` sin marca en su celda, porque las pruebas leen esa columna; el [P] pasa a la columna «Uso») y, en la sección 4.7, la visibilidad por idioma y, en la compuerta de release, la revisión nativa de las cadenas de UI de los idiomas distintos del español, por D-018, Q5-A, y D-022, punto 6); actualizado el 2026-10-15 (M2.8, tanda 4) con lo que faltaba de P-M1 (D-023) tras las olas 1 y 2: sección 4.5, el estado de los dos formatos, la forma de `labels`, `viewpoints` y `passing` que fija `schemas/af-river-map-1.schema.json`, `class`, `src` y `land.zones` con `first` y `count` [P], los puntos uint16 del marco, `at` y `leg` obligatorios y el sentido de `s`, que crece río abajo [P]: el ejemplo medía `s` desde el mar en el cauce, aunque su propio `leg` de iquitos iba en el otro sentido, y se alinea con la geometría que genera `tools/river-map/geometry.py` (M2.6), con la parada n = 14 y la zona de tierra como en `river_stops_v1.json` y `river_map_v1.json`, sin cambiar ningún esquema) · Estado: **aprobado por el dueño para producción** · Proyecto Amazon Fill (nombre comercial Tintalú desde el 2026-10-06; sustituye a Aquazonia, descartado por la solicitud idéntica INPI 938306200 en Brasil) · Convención de evidencia: **[V]** verificado en el informe citado, **[E]** estimación del informe, **[R]** reseñas, **[NV]** no verificado, **[P]** propuesta de este capítulo a calibrar en soft launch. "Plan" es `docs/setup/plan-instalacion.md` (pasos `s01`-`s32`) y "setup/a" … "setup/f", los informes de `docs/setup/research/` (a: Unity en Windows ARM64; b: Google Play 2026 y versiones; c: agentes programadores y CI; d: .NET y núcleo compartido; e: Godot; f: marca). "(dirección N)" remite a las decisiones de dirección vinculantes. Nombres según `docs/gdd/00-glosario.md`; IDs de código con prefijo `af_`; nombre interno Amazon Fill solo en carpeta (`amazon-fill`), repositorio (`Nipko/fill-game`), código (`com.amazonfill.core`, ensamblados `AF.*`) y proyectos Firebase (`amazon-fill-dev` y `amazon-fill-prod`), con las formas del glosario §11; **ID de aplicación público `com.<estudio>.tintalu`** (Android `applicationId` y App ID de iOS: sin la cadena "amazon", dirección 1 y glosario §1; sección 13.1; los builds de prueba usan `com.amazonfill.dev` hasta el dictamen de marca).

## Resumen

Tintalú se construye en **Unity 6.3 LTS 6000.3.25f1 (plan Personal), C#, URP 2D y Shader Graph**, con el editor ARM64 nativo en la máquina de desarrollo (sección 16) y los builds de referencia en CI Linux, y con Godot 4.8 documentado como alternativa si el prototipo del líquido no alcanza 60 fps en el dispositivo de referencia en tres semanas. La arquitectura separa un núcleo puro en C# (reglas + solver en el paquete embebido `Packages/com.amazonfill.core`, compilado también con .NET 10 desde `dotnet/` y compartido con el pipeline offline de niveles) de catorce módulos con dependencias en una sola dirección, un formato de nivel JSON con estado explícito y variantes de alivio pregeneradas (esquema canónico `af-level/1`, formalizado en `schemas/af-level-1.schema.json`), y un perfil de jugador versionado que viaja a Play Games Saved Games. El binario inicial queda en **≤80 MB** con ARM64, IL2CPP, ASTC y el arte de los mundos 5-13 por Play Asset Delivery (en 1.0 solo Loreto, 141-200; los niveles 201-700 usan el fondo "río genérico" hasta que lleguen sus packs); los niveles 1-700 (mundos 1-9) y el pool del Reto del río viajan en el AAB y los paquetes posteriores llegan por Addressables remotos sin pasar por la tienda; nueve SDKs de terceros (frente a los 30 rastreadores del líder) cubren mediación MAX (con AdMob y UMP dentro, sin plugin Google Mobile Ads), IAP con Play Billing 8 o superior (PBL 9 con Unity IAP 5.4.4), Firebase y Play Games. Cinco Cloud Functions (SSV de anuncios recompensados, verificación de compras, reacreditación del libro mayor, job diario de reembolsos y eliminación de cuenta) son todo el servidor hasta la versión 1.2. La sección 9 es la **única tabla de Remote Config del GDD**; los demás capítulos la referencian sin repetir claves. CI con GitHub Actions + game-ci (licencia Personal por `UNITY_EMAIL`/`UNITY_PASSWORD`) construye y, tras el dictamen de marca, publica a las pistas de Play; iOS llega después por Codemagic con Xcode 26, ATT y Privacy Manifest.

## 1. Decisión de motor

### 1.1 Comparación

| Criterio (peso de research/06 §1.4) | Unity 6 LTS | Godot 4.5/4.6 | Lectura para Tintalú |
|---|---|---|---|
| Licencia | Personal gratis hasta 200.000 USD de ingresos + financiación; Runtime Fee cancelado; splash opcional (research/06 §1.1 [V]) | MIT, sin costo (research/06 §1.1 [V]) | Coste cero en ambos durante todo el primer año |
| Build base Android | ≈32 MB APK vacío con IL2CPP, ARM64 y stripping (research/06 §1.2 [V]) | 20-28 MB (research/06 §1.2 [V]) | Unity obliga a presupuestar tamaño desde la semana 1 (sección 6) |
| Gama baja | URP 2D correcto; arranque pesado (research/06 §1.2 [V]) | Renderer Compatibility (GLES3) "suficiente para 2D"; 2D nativo 15-40 % más rápido con muchos sprites (research/06 §1.2 [V]) | Un tablero tiene ≤16 cuias: la ventaja de Godot no se activa |
| Mediación de anuncios | MAX y LevelPlay oficiales (research/06 §1.3 [V]) | Solo AdMob por plugin comunitario; sin bindings mantenidos de MAX/LevelPlay (research/06 §1.3 [V]) | La dirección 13 exige MAX con 4 redes: solo Unity lo cumple sin trabajo nativo |
| IAP | Unity IAP 5.x con Billing 7/8 (research/06 §1.3 [V]) | Play Billing (Fundación, 4.2+) y StoreKit 2 (research/06 §1.3 [V]) | Paridad |
| Analítica / Remote Config / crash | Firebase Unity 13.15 (research/06 §1.3 [V]) | Sin Firebase oficial; GameAnalytics 3.0.1 con Remote Configs y A/B (research/06 §1.3 [V]) | La dirección 20 fija Firebase; en Godot habría que sustituirlo |
| Cloud save | Play Games plugin 2.x oficial (research/06 §1.3 [V]) | Plugin PGS comunitario [NV] (research/06 §1.3) | — |
| Camino a iOS | Proyecto Xcode generado; Privacy Manifests resueltos por los SDKs (research/06 §1.5 [V]) | Exportador iOS correcto; sin SDKs de mediación (research/06 §1.4 [E]) | — |
| Puntaje ponderado (máx. 110) | **94** | **86** | research/06 §1.4 [E] |

### 1.2 Decisión y plan de contingencia

**Unity 6.3 LTS, versión exacta `6000.3.25f1`, changeset `e1dba0a9aba4`** (24-09-2026; soporte LTS hasta diciembre de 2027), **plan Personal, URP 2D, Shader Graph, C#** (dirección 20; research/06 §1.5 [V]; setup/b §2.1 y §7 [V]; setup/a §7 [V]; plan, §Decisión de motor). Razón decisiva: toda la cadena anuncios → mediación → IAP → analítica → Remote Config → cloud save tiene plugins oficiales mantenidos en 2026 (research/06 §1.5 [V]); el precio es el tamaño, que la sección 6 resuelve.

- **Por qué esta versión:** admite target API 36, genera `libunity.so` y `libil2cpp.so` alineadas a 16 KB en todos sus parches, trae "SDK Platform 37" desde el 6000.3.22f1 y AGP 9.0.0, tiene IAP 5.4.4 como versión publicada para 6000.3 y game-ci publica `unityci/editor:ubuntu-6000.3.25f1-android-3` (setup/b §2-§4 y §7 [V]).
- **Descartadas:** 6000.6.4f1 (versión de actualización, no LTS; es la que instala `winget install Unity.Unity.6000`, que no se usa) y 6000.0.x (soporte hasta el 16-10-2026) (setup/b §2.1 [V]).
- **Fijación:** `ProjectSettings/ProjectVersion.txt` (`m_EditorVersionWithRevision: 6000.3.25f1 (e1dba0a9aba4)`); el CI y los scripts la leen de ahí. Los parches 6000.3.x posteriores (p. ej. 6000.3.26f1, con AGP 9.1.1) entran solo en un PR dedicado y con el CI en verde (plan; setup/b §7 [V]).
- **Instalación:** Unity Hub 3.22.2 y Unity CLI 1.0.0-beta.12 (MSIX arm64 por winget) y `unity install 6000.3.25f1 --changeset e1dba0a9aba4 --module android --child-modules` (plan s04, s17-s18). El editor es **ARM64 nativo**; los detalles de la máquina, la compuerta del día 1 y los planes B están en la sección 16.

**Alternativa Godot 4.8** (dirección 20; plan C, sin instalar): se activa solo si se cumple uno de tres disparadores [P]: (a) el prototipo del shader de líquido (sección 5) no corre a 60 fps estables en el dispositivo de referencia de clase A (sección 5.4) al cabo de 3 semanas de trabajo; (b) Unity cambia las condiciones del plan Personal antes del soft launch; (c) el editor ARM64 resulta inutilizable de forma persistente después de probar los planes B1 y B2 de la sección 16 (plan, §Riesgos). En Godot el stack sería: renderer Compatibility, AdMob como mediador (plugin de Poing Studios, sin mediación con bidding), Play Billing de godot-sdk-integrations, GameAnalytics como sustituto de Firebase para analítica, Remote Configs y A/B, y PAD solo en modo install-time (research/06 §1.5 y §8 [V]; setup/e [V]). Versiones de referencia: Godot 4.7.2 estable (18-08-2026) y 4.8 en desarrollo, que sube el mínimo de C# a net10.0; C# en Android sigue siendo experimental (setup/e [V]). Coste del cambio tras el mes 2: se reescriben presentación, UI y capa de SDKs; el núcleo y el solver, escritos en C# puro sin `UnityEngine` en netstandard2.1 (sección 2), se reutilizan en Godot .NET (C# net10 en 4.8) o, si se elige GDScript, sirven de especificación ejecutable con sus tests; el núcleo en netstandard2.1 y .NET 10 abarata el cambio (plan, §Riesgos) [NV exportación Android con C# en producción].

## 2. Arquitectura de módulos

### 2.1 Principios

1. **Núcleo puro**: `AF.Core` y `AF.Solver` son ensamblados C# sin referencias a `UnityEngine`; se compilan también en la herramienta offline `tools/level-generator` (sección 2.4) para que el generador, el validador de QA y el cliente ejecuten exactamente las mismas reglas (research/02 Complemento 4.6 [E]: "un único módulo solver sirve a pista, Barajar, Río sin fin y pipeline offline"). **Mecanismo (2026-10-06):** una sola copia del código en el paquete UPM embebido `Packages/com.amazonfill.core/` (`Runtime/AF.Core` con asmdef `noEngineReferences: true`, `Runtime/AF.Solver`, tests compartidos en `Tests/Editor`). Los `.csproj` de `dotnet/` (`netstandard2.1`, `LangVersion 9.0`, `TreatWarningsAsErrors`, BannedApiAnalyzers) enlazan esos `.cs` con `<Compile Include>`, así que el SDK de .NET 10 rechaza en el PC del agente lo mismo que Unity (sintaxis posterior a C# 9 y APIs posteriores a netstandard2.1); `tools/level-generator` y `tools/level-qa` (`net10.0`) los referencian. Los tests se escriben en el subconjunto de NUnit 3.5 que trae Unity y corren con NUnit 3.14.0 en `dotnet test` y en EditMode (setup/d §1, §4-§5 [V]; plan, §Andamiaje).
2. **Dependencias en una dirección**: cada módulo es un Assembly Definition (`.asmdef`) y solo referencia a los de la fila superior de la tabla 2.2; la UI nunca es referenciada por nadie.
3. **Comunicación por eventos tipados** (`AF.Events`, bus síncrono en memoria): `PourExecuted`, `LevelWon`, `StuckDetected`, `SeedsChanged`, `AdRewardGranted`, etc. Analytics, Economy y UI se suscriben; Core no conoce a nadie.
4. **Todo lo online tiene sustituto offline** (research/06 C6.1 [V]): niveles, Reto del río, Bitácora del río, Crecida y Cuaderno de campo funcionan sin red; anuncios, IAP, liga y Aldea son online por naturaleza y degradan a "precio en semillas" o "último estado cacheado con marca temporal".
5. **Determinismo**: el estado del nivel es un valor inmutable (`BoardState`) y cada vertido produce un nuevo estado; el historial de Marea es una lista de estados comprimidos (dirección 7; capítulo 4 §2.1). Entre CoreCLR (herramientas) y Mono/IL2CPP (cliente) están prohibidos `System.Random`, `string.GetHashCode()`, `DateTime.Now` y LINQ en el núcleo, y no se confía en el orden de `Dictionary`/`HashSet` (setup/d [V]). El PRNG es propio del núcleo; la paridad con el prototipo Python se mide sobre los estados exportados (hash y par), no sobre la secuencia aleatoria (decisión del 2026-10-06; 09 R26).
6. **SDK detrás de interfaces con implementaciones falsas en el editor**: Firebase, MAX, IAP y Play Games solo se tocan a través de `AF.Analytics`, `AF.RemoteConfig`, `AF.Backend`, `AF.Ads`, `AF.IAP` y `AF.Save`, con implementaciones falsas en el editor (`#if UNITY_ANDROID && !UNITY_EDITOR` o inyección de dependencias). Así el Play Mode funciona en el editor ARM64 aunque las DLL x64 de escritorio de Firebase no carguen; los SDK reales se prueban en dispositivo y en la pista interna (setup/b §5 y §Recomendación 5 [V]; sección 16).

### 2.2 Módulos, responsabilidades y dependencias

| Módulo (asmdef) | Responsabilidad | Depende de | Entrega a | Versión |
|---|---|---|---|---|
| `AF.Core` | `BoardState`, `Cuia` (capacidad 1-6, `len` + 4 bits por unidad = tinte−1, de modo que caben los 16 índices de K_motor; máscara de ocultos; mecánica), reglas de vertido (sobre vacío o mismo color con espacio; cantidad según `PourMode`: la racha superior contigua que cabe en `Run`, el valor por defecto por decisión del dueño del 2026-10-08 (D-005, que sustituye a D-001), o una dosis en `Single` (solo pruebas); clave `rules_pour_mode`, sección 9; 02 §1.2), sello de Cuia sellada, detección de victoria, `legalNonFutileMoves` (capítulo 4 §5.1), mecánicas Hoja/Matapi/Liana/Remolino/Lluvia/Brote/Piraña (D-007) como `IMechanic` con hooks `OnBeforePour`/`OnAfterPour`, canonicalización (cuias ordenadas) y hash de 64 bits. Desde D-007: las mecánicas son instancias sin estado en una tabla estática indexada por el código de mecánica de la cuia (bits 39-42 de `Cuia.Packed`; todo el estado vive en la cuia, así que el estado canónico basta para el solver y para Marea; los códigos 8-15 quedan libres para la 1.1); el Barro no es un `IMechanic` sino el color 15 del contenido; R10 por etapas, `futil` con Q-B y los contadores con Q18 B viven en `AF.Core.Rules.BoardRules`, la única implementación que aplica vertidos (02 §1.2 y §1.4) | — | Todos | 1.0 |
| `AF.Solver` | A* ponderado con heurística `max(cortes de color, dispersión)`, poda estándar y presupuesto de nodos parametrizable; bot casual ×40 (85/15, tope 400 pasos); barajado inverso con filtros anti-trivialidad; enumeración de realidades para Aguas turbias (≤4 dosis ocultas → ≤24 realidades, canon de 02 T2 y `lvl_murky_max_hidden_units` = 4; memo por estado canónico y presupuesto de nodos por realidad; sin Pista parcial) (research/02 §2.2, §7.1, Complemento 4.4-4.5 [V/E]) | Core | Boosters (Morpho, Pororoca, verificación "la compra cumple"), Levels (validación), tools | 1.0 |
| `AF.Levels` (LevelLoader) | Carga de paquetes `mundo_{nn}_v{vuelta}.json` desde Addressables (grupo `levels_install` para los mundos 1-9; `levels_remote` en Unity CCD/R2 para el 10 en adelante y las reposiciones, anunciados por `content_version`); verifica `pack_schema` y `generator.version` antes de activar un paquete remoto; índice nivel → paquete; selección de variante (`base` o la entrada de `relief[]` que corresponda, sección 4.1); aplicación de `level_overrides`; precarga de N+1 mientras se juega N; pool del Reto del río (3 bandas × 730) indexado por fecha. Con D-013, `RiverStopCatalog` (sin motor) lee `river_stops_v1.json` (sección 4.5), lo contrasta con `WorldTable` y da `StopOf(level)` sin asignaciones. | Core, RemoteConfig, Save | GameFlow, UI, Presentation | 1.0 |
| `AF.Boosters` | Marea, Cuia extra, Morpho, Pororoca, Luciérnaga, Salto del Boto, Volver a la orilla, Cuia de rescate; cupos por nivel, stock, topes diarios y árbol de decisión de Aguas estancadas (capítulo 4 §6.2) | Core, Solver, Economy, Ads, RemoteConfig, Analytics | UI (HUD, modal) | 1.0 |
| `AF.Economy` | Cartera de semillas y Canoas (regeneración 25 min con reloj validado), stock de comodines, Crecida y Hoja escudo, Bitácora del río, Canasta, Cántaro, Cosecha doble, Mochila de inicio y Sin anuncios como derechos; toda mutación pasa por `Wallet.Apply(Transaction)` que emite `earn_virtual_currency` / `spend_virtual_currency`; las Canoas infinitas evitan gastar canoas, no los anuncios (D-008) | Save, RemoteConfig, Analytics, Platform (`IClock`) | Boosters, LiveOps, UI | 1.0 |
| `AF.Ads` | Envoltorio de AppLovin MAX: inicialización tras consentimiento, precarga, ubicaciones (`rv_double_harvest`, `rv_undo_refill`, `rv_hint`, `rv_reveal`, `rv_extra_canoe`, `rv_daily_chest`, `interstitial_post_level`), compuertas de cadencia (nivel ≥22, solo tras victoria, cada N niveles, ≥150 s, tope por sesión y día), topes y cooldowns por ubicación, concesión provisional + SSV, `ad_impression` con ILR. Nunca en el embudo de fallo (al reintentar, al rendirse, en Nivel fallido ni en Sin canoas) y las Canoas infinitas no suprimen intersticiales (05 §6.1; D-008). **Fase 2 (D-008):** interfaz con una implementación simulada, sin SDK ni red, que el dueño aprobó el 2026-10-08; MAX entra después, detrás de la misma interfaz | RemoteConfig, Consent, Analytics, Platform (`IClock`) | Boosters, Economy, GameFlow | 1.0 |
| `AF.IAP` | Unity IAP 5.4.4 (Play Billing Library 9.0.0; Play exige PBL 8 o superior desde el 31-08-2026 y PBL 9 vale hasta el 31-08-2028, setup/b §4 [V]): catálogo `af_*`, precios localizados desde la tienda, compra → verificación en Cloud Function → acreditación; restauración de no consumibles y del libro mayor de consumibles; `setObfuscatedAccountId` = UID de Firebase. **Fase 2 (D-008):** compras simuladas sin Play Billing detrás de la misma interfaz: los 13 SKU de 05 §4.1 con sus precios locales, acreditados con `Wallet.Apply`; una compra fallida o cancelada nunca descuenta | Economy, Save, Backend, Analytics | Mercado, modal Aguas estancadas, Sin canoas | 1.0 |
| `AF.Save` | Perfil JSON local con escritura atómica y copia de seguridad; migraciones `profile_schema n → n+1`; Play Games Saved Games con resolución "nivel máximo + `economy_ts`"; Firebase Auth anónimo y vínculo con Play Games; **eliminación de cuenta** (llama a `deleteAccount`, borra el snapshot `af_main` de Saved Games y el perfil local; sección 11); (1.2) documento Firestore por usuario como fuente de verdad multiplataforma. **Fase 2 (D-008):** solo el perfil local, sin Play Games, Firebase ni red; guarda el nivel en curso tras cada vertido, con la pantalla del embudo abierta (02 §14) | Backend, Platform (`IClock`) | Economy, Levels, LiveOps | 1.0 |
| `AF.Analytics` | Suscriptor del bus; traduce eventos de dominio a Firebase Analytics (eventos recomendados + `af_*`), Crashlytics (no fatales con claves de contexto) y, opcionalmente, GameAnalytics (`biz_ga_enabled`, apagado por defecto); añade los parámetros comunes de la sección 8.1; respeta el consentimiento | Consent, RemoteConfig | — | 1.0 |
| `AF.RemoteConfig` | Firebase Remote Config con **defaults compilados** (`RemoteConfigDefaults.json`), caché, fetch con intervalo mínimo de 12 h (research/02 Complemento 4.2 [V]), acceso tipado `RC.Get<T>(key)`, congelación de valores al abrir cada pantalla (capítulo 4 §10.2), `rc_variant` para experimentos, origen `Default/Cached/Remote` | Backend | Todos | 1.0 |
| `AF.LiveOps` | Reto del río (pool por fecha + `daily_overrides` y `live_daily_offset`), Bitácora del río, Crecida, Dato del río, calendario de eventos (`live_events_schedule`); 1.2: Regata, Travesía, Encuentro de las Aguas, Temporada de lluvias, Liga del río (Unity Leaderboards), Pase Expedición | Economy, Levels, RemoteConfig, Platform (`IClock`), Save | UI, Analytics | 1.0 / 1.2 |
| `AF.Localization` | **Fachada `Loc`** (D-018, 2026-10-13): `Loc.T(key)`, `Loc.T(key, args)`, `Loc.Number(n)` con el formato de la región del teléfono y el evento `Loc.Changed`, sin motor, sobre las tablas de textos CSV del repositorio (sección 4.7, contrato 32), con el subconjunto de plurales de Smart Strings; `UiText` queda como acceso tipado. Idioma: elección guardada → teléfono (`es-*` → ES-419, `pt-*` → PT-BR, el resto → EN) → EN. Idioma falso `en-XA` en las pruebas de capturas. **En la fase 3, Unity Localization 1.5.13** (aprobado por el dueño en D-018; trae Addressables) detrás de la misma fachada: un método de Editor versionado genera las String Tables desde el CSV, y `LocalizedTmpFont` da la fuente por idioma. Hasta entonces, sin paquete | — | UI | 1.0 |
| `AF.Presentation` | Shader de líquido (controlador de `MaterialPropertyBlock` por cuia), chorro Bézier, burbujas, Llenar y teñir, TiTino, Capi y Rosa, VFX/SFX de comodines, agua base por mundo. Con D-013 (Mapa del río): `StopBackdrop` (compone el fondo de cada parada: kit, héroe, piezas por código y LUT, con el reflejo del héroe por código y el fundido), `RiverRibbonGraphic` (`MaskableGraphic` con tira de triángulos y color por vértice, como `WaterStripGraphic`: vidrio, lleno y frente), `LandGraphic` y `PolylineCursor` (posición por longitud de arco, sin asignaciones), sin shader propio | Core (solo lectura), RemoteConfig | UI | 1.0 |
| `AF.UI` | Pantallas (Boot, Consentimiento, Inicio (Lobby, 06 §4.7), Mapa del río (`RiverMapScreen` de D-013: construida por código en `Game.unity`, como `LobbyScreen`, en superposición sobre el inicio, la pausa o la victoria), Nivel, Aguas estancadas, ¿Seguro? y Nivel fallido (D-008), Sin canoas, Mercado, Ajustes con "Eliminar cuenta y datos", Bitácora, Canasta), HUD con regla icono + etiqueta funcional, cola de un solo takeover por sesión | Todos vía interfaces | — | 1.0 |
| `AF.Platform` | Puentes nativos: Play Asset Delivery (Addressables for Android), App Check / Play Integrity, Unity Mobile Notifications (locales; en 1.0 también los mensajes "remotos" del capítulo 6 §8.3 se programan en el dispositivo al cerrar la app: "Nuevos niveles" con `live_content_end_message.next_batch_eta_days`, "Regreso" D7/D14/D30 y "Premio por reclamar" al salir con un premio pendiente; FCM en 1.2), ATT y Privacy Manifest en iOS, `Date` del servidor para el reloj. Reloj inyectable `IClock` (UTC), con la implementación del sistema y una manual para tests: es el único sitio donde se lee la hora del dispositivo (D-008, fase 2) | — | Save, Economy, Ads, LiveOps | 1.0 |
| `AF.GameFlow` | Máquina de estados: `Boot → Consent → Lobby → LevelLoad → LevelCard → Playing → Stuck → GiveUpConfirm → LevelFailed → NoCanoes / Win → AdGate → Next` (02 §2; embudo de D-008: GiveUpConfirm, LevelFailed y NoCanoes solo desde el 31; `LevelCard`, la tarjeta previa de 1 toque de D-009 en los niveles con etiqueta o con estreno, entre la carga y la intro, que no sale al reanudar ni, por defecto, al reintentar [P]); decide cuándo corre Llenar y teñir (solo con el nivel en memoria, dirección 18) y cuándo el intersticial (solo tras victoria). No depende de la UI (principio 2): la UI lo observa por eventos (`AF.Events`). Con D-013, `RiverJourney` (funciones puras sobre `max_level`, `skipped_levels`, `map_seen_level` y `WorldTable`: parada, llenado, bocas pasadas, banderas, km, «Faltan N» y lo pendiente de animar) y `MapMoments` (qué toca en cada victoria: franja de llegada, Mapa automático o nada, según las claves `ui_map_*` de la sección 9), sin motor. | Levels, Boosters, Ads, Economy | UI | 1.0 |

### 2.3 Secuencia de arranque (presupuesto: primera interacción <20 s, objetivo <6 s [P])

```
Boot():
  t0 = now
  Localization.LoadLocale(saved or system or "en")          # síncrono, <50 ms; D-018: manda la elección guardada (settings.language, o su copia en PlayerPrefs, que se lee antes que el perfil); medir los 50 ms con el paquete en el teléfono
  profile = Save.LoadLocal() ?? Profile.New()                 # atómico; migraciones n→n+1
  RC.LoadDefaults(); RC.ActivateCached(); RC.FetchAsync(min_interval=12h)   # no bloquea
  Firebase.InitAsync(analytics_collection=false)              # Analytics apagado hasta consentimiento
  Crashlytics.Enable()                                        # diagnóstico; ver pregunta abierta 6
  if profile.consent is null or profile.consent.version < RC.consent_text_version:
      age_band = UI.ShowAgeNeutral()                          # bandas {<13, 13-17, 18+} (05 §7); solo en dispositivo, sin ID; fuente: consent_age_signal_source
      if age_band == "<13":                                   # bloqueo en 1.0: consent_under13_policy = "block" (05 §7; GDD §6 #15; consulta al abogado antes de la Fase 5)
          Ads.SetChildDirected(true)                          # TagForChildDirectedTreatment vía MAX (y sus adaptadores)
          Analytics.SetEnabled(false)                         # Analytics apagado (o solo eventos sin ID ni user_id)
          PlayGames.Disable(); IAP.Disable(); RC.PersonalizationOff()   # sin Play Games sign-in, sin IAP, sin RC personalizado
          UI.ShowBlocked("Pide a un adulto que instale el juego"); return   # no se crea sesión de juego
      UI.ShowConsent(biz_legal_profile_by_country[geo], age_band)   # BR/CO/PE/MX propia; EEE/UK/CH vía UMP
      if age_band == "13-17": Ads.SetNpa(true); Ads.SetMaxContentRating("G"); IAP.PurchasePin(on)   # biz_under18_no_personalization
  Analytics.SetEnabled(profile.consent.analytics)             # siempre ANTES de GameFlow.Go (06 §8 lo exige explícito)
  Ads.InitAsync(profile.consent, age_band)                    # MAX arranca solo tras consentir; precarga RV/interstitial según nivel
  Auth.SignInAnonymousAsync().Then(Save.CloudSyncAsync)       # PGS en segundo plano; conflicto → sección 11
  Levels.PreloadAsync(profile.progress.max_level + 1)         # JSON + atlas del mundo
  if first_run: GameFlow.Go(LevelLoad(1)) else GameFlow.Go(Lobby)   # pantalla de inicio (06 §4.7; D-008)
  Analytics.Log("af_boot", cold_start_ms = now - t0, first_interaction_ms, rc_source)
```

### 2.4 Herramientas fuera del cliente

| Herramienta | Lenguaje | Función | Entrada / salida |
|---|---|---|---|
| `tools/level-generator` | C# (consola .NET 10, `net10.0`; `ProjectReference` a `dotnet/AF.Core` y `dotnet/AF.Solver`, que compilan los `.cs` de `Packages/com.amazonfill.core`; setup/d §4 [V]) | Generación híbrida inversa + solver + bot (research/02 §7.1 [V/E]); puntuación `D`; colocación en el ciclo de 20; variantes `relief[]` (lista ordenada por prioridad); validación de debuts de tinte y `max_pares_riesgo` | `spec.json` → `mundo_{nn}_v{vuelta}.json` + `report.csv` |
| `tools/level-qa` | C# (consola .NET 10, mismas referencias que `tools/level-generator`) | Re-resuelve cada paquete, comprueba etiquetas (bot ≥75 % / ≥90 %), Aguas turbias con ≥1 vertido seguro, par, determinismo por semilla; falla el CI si algo no cumple | paquetes → código de salida + informe |
| `tools/economy-sim` | Python 3.14 | Parte de `tools/econ_sim.py` y `tools/econ_variants.py`, ya en el repositorio (05 §3: fuente de toda cifra [sim], salidas en `tools/out/econ_*.md`). Simula perfiles (ligero/medio/intenso) N días con las tablas de Remote Config y reporta saldo por tramo y "bloqueados sin recursos" (research/04 C1 §5 [E]) | `RemoteConfigDefaults.json` + paquetes → CSV |
| `tools/loc-sync` | — | **Retirada por D-018 (Q4-A):** los textos viven en un CSV en el repositorio (sección 4.7) y a los traductores se les pasa una copia; Google Sheets exigiría credenciales que los agentes no manejan (regla dura 5 de `CLAUDE.md`). Las String Tables las genera un método de Editor desde el CSV (fase 3) | — |
| `functions/` | TypeScript (Firebase Cloud Functions, 2.ª gen.) | **1.0 (cinco)**: `rewardedSsv` (HTTPS, callback S2S de MAX y SSV de AdMob; sección 10.2), `verifyPurchase` (callable + App Check; sección 10.3), `restoreLedger` (callable + App Check: devuelve los tokens del libro mayor `users/{uid}/purchases` ausentes de `applied_purchases` para reacreditarlos al restaurar; sección 11), `voidedPurchasesJob` (Cloud Scheduler diario: Voided Purchases API → marca `voided` en el libro mayor y encola la retirada de semillas/derechos; sección 10.1), `deleteAccount` (callable + App Check: borra `users/{uid}` y `rewards/*` del UID, elimina el usuario de Auth y conserva el libro mayor de compras **anonimizado** el plazo fiscal; sección 11). **1.2**: `leaderboardSubmit`, `eventClose`, `rcProposalJob` (Cloud Scheduler semanal → consulta BigQuery → propuesta de `level_overrides` con la API de administración de Remote Config, publicada solo tras aprobación humana; capítulo 9 §6.2) | HTTPS / callable / scheduler |

## 3. Estructura de carpetas del proyecto Unity

Basada en research/06 §9 con ensamblados por módulo y separación entre arte instalado y arte por PAD, y actualizada el 2026-10-06 con el andamiaje del plan de instalación (plan, §Andamiaje; setup/c §3, setup/d §4 [V]). La raíz del repositorio es el proyecto Unity; Unity solo importa `Assets/` y `Packages/`, así que el resto de carpetas no le afecta. `Assets/`, `ProjectSettings/` y `Packages/manifest.json` se crean después de activar la licencia Personal (plan s15-s20).

```
amazon-fill/
  CLAUDE.md  global.json  .gitignore  .gitattributes  .editorconfig   # global.json fija el SDK 10.0.401
  .claude/{settings.json, agents/, rules/, skills/unity-cli/}         # 7 subagentes, reglas por ruta, permisos
  .github/workflows/{ci.yml, unity.yml}                               # GitHub solo lee workflows aquí (sección 12.1)
  Assets/
    _Project/
      Art/
        Cuias/            # cuia-garrafa base + 13 skins (atlas por skin)
        Fruits/           # 14 iconos + 14 patrones (atlas único 2048², ASTC 4×4)
        Mascots/          # Capi, Rosa (spritesheets)
        UI/               # atlas UI, iconos de comodines, marcos Difícil/Muy difícil
        VFX/
        Backgrounds/
          w01_estuario/ … w04_trapecio/     # install-time (v1.0)
          w05_loreto/ … w13_origen/         # on-demand (PAD)
      Audio/{Music,SFX,Ambience}
      Shaders/{Liquid,Glass,River,Murky}
      Prefabs/{Cuias,Boosters,UI,Mascots}
      Scenes/{Boot,Map,Game}
      Scripts/
        Levels/ Boosters/ Economy/ Ads/ IAP/ Save/ Analytics/ RemoteConfig/
        LiveOps/ Localization/ Presentation/ UI/ Platform/ GameFlow/ Events/
                         # AF.Core y AF.Solver ya no viven aquí: están en Packages/com.amazonfill.core
      Data/
        Levels/      mundo_01_v1.json … mundo_09_v1.json (Addressables, grupo levels_install; mundo_10+ en levels_remote)
        Daily/       daily_pool_v1.json (2.190 tableros = 3 bandas × 730)
        Palette/     tints.json (14 tintes, HEX, patrón, debut, pares de riesgo)
        Map/         river_map_v1.json (af-river-map/1) y river_stops_v1.json (af-river-stops/1): Mapa del río (D-013; sección 4.5)
        Config/      RemoteConfigDefaults.json, iap_catalog.json, ad_placements.json
      Localization/{Tables,Locales}
      Settings/{URP,Addressables,AddressablesForAndroid,Input}
      Editor/CI/     # ProjectSetup.ApplyAndroidDefaults, BuildSmoke.BuildAndroid (compuerta del día 1)
      Tests/{EditMode,PlayMode}
    Plugins/{Android,iOS}            # manifests, link.xml, PrivacyInfo.xcprivacy
    ExternalDependencyManager/       # EDM4U 1.2.189 que trae Firebase; pasar a com.unity.external-dependency-manager 2.1.0 cuando migren los SDK
    MaxSdk/                          # plugin MAX 8.6.6 + adaptadores (AdMob, Unity Ads, Meta, Mintegral); sin plugin GMA
    GooglePlayPlugins/               # Play Integrity
  Packages/
    manifest.json                    # versiones fijadas; editarlo exige aprobación del dueño
    com.amazonfill.core/             # ÚNICA copia del núcleo (paquete embebido)
      package.json
      Runtime/AF.Core/   AF.Core.asmdef (noEngineReferences: true), Polyfills/IsExternalInit.cs
      Runtime/AF.Solver/ AF.Solver.asmdef
      Tests/Editor/AF.Core.Tests/ AF.Solver.Tests/   # tests compartidos: subconjunto NUnit 3.5 + C# 9
  ProjectSettings/                   # ProjectVersion.txt = 6000.3.25f1 (e1dba0a9aba4); solo se cambia desde el Editor
  dotnet/                            # Directory.Build.props, BannedSymbols.txt, AmazonFill.slnx
    AF.Core/ AF.Solver/              # netstandard2.1, LangVersion 9; enlazan los .cs del paquete
    AF.Core.Tests/                   # net10.0, NUnit 3.14.0
    artifacts/                       # bin/obj (ignorada por git)
  tools/                             # Directory.Build.props propio
    level-generator/ level-qa/       # net10.0, referencian dotnet/AF.Core y dotnet/AF.Solver
    *.py                             # prototipo, simulación de la economía y validate_schema.py (Python 3.14)
    economy-sim/ loc-sync/           # más adelante (sección 2.4)
  scripts/{unity-test.ps1, unity-compile.ps1}   # envoltorios que usan los agentes (versión leída de ProjectVersion.txt)
  ci/{size-gate.ps1, check-16kb.sh}  # compuertas que llaman los workflows
  codemagic.yaml                     # Fase 6 (iOS)
  functions/                         # Cloud Functions (TypeScript), más adelante
  schemas/af-level-1.schema.json     # JSON Schema canónico del formato de nivel af-level/1 (sección 4.1)
  docs/{GDD.md, gdd, research, setup, legal, store-listing/{es-419,pt-BR,en}}
  LICENSES.md                        # audio/arte de terceros (research/08 §4.3)
```

Los `Directory.Build.props` van en `dotnet/` y `tools/`, nunca en la raíz, porque allí afectarían a los `.csproj` que genera Unity; el `.gitignore` excluye solo los `/*.csproj`, `/*.sln` y `/*.slnx` de la raíz (plan, §Andamiaje).

Convenciones: carpetas en inglés y snake_case para datos; Addressables con etiquetas `world:w{nn}`, `levels:m{nn}`, `install`, `on_demand`; assets de 2.2 MB o más solo en grupos PAD.

## 4. Formatos de datos y versionado

### 4.1 Paquete de niveles (propiedad del capítulo 3; este capítulo fija la sintaxis)

Regla de la dirección 17: estado explícito más semilla y versión del generador para reproducir; nunca generación en cliente para la campaña (research/02 §5 y §Implicaciones 1 [V]). **Esquema único `af-level/1`**, formalizado como JSON Schema (draft 2020-12) en **`schemas/af-level-1.schema.json`** (raíz del repositorio y única copia; archivo canónico generado desde esta sección: `tools/level-generator` lo emite, `tools/level-qa` y `AF.Levels` validan contra él y `python tools/validate_schema.py` comprueba el ejemplo de esta sección y los casos límite de campaña y Reto del río; cualquier cambio que altere la validación sube `pack_schema`, los editoriales de `description` no): cabecera de pack de este capítulo + campos de nivel del capítulo 3 §10. Esta sección es el canon del formato (incidencia 12): 02 §9.3, 03 §10 y 04 §10.1 remiten aquí ("ver 08 §4.1") y solo conservan un ejemplo si es idéntico al de esta sección; no definen variantes propias. Resuelve las tres versiones anteriores (02: `units[]` + `mech{type,color}` + `hidden_mask`; 03: `relief{default,…}`; 08 anterior: `u` hexadecimal + `hidden` entero) así: `cuias` = listas de enteros 1-16, `attrs[].key` (no `color`), `hidden` = lista `[cuia, capa]` (admite ocultos no contiguos), `relief` = **lista** de tableros completos con `type`. Semilla canónica en todo el GDD (glosario §11, capítulos 2, 3, 4 y 8): `amazonfill:v1:m{mm}:v{v}:n{nnnn}` **con ceros a la izquierda** (ej. `amazonfill:v1:m03:v1:n0057`); el test de reproducibilidad por hash exige el padding exacto. El pool del Reto del río (`kind: daily`, 03 §16) tiene formato propio: `id` `d{b}_n{nnnn}` y `seed` `amazonfill:v1:daily:b{b}:d{ddd}`, con b = banda 1-3 y el día 0-729 con ceros a la izquierda (p. ej. `d2_n0417` / `amazonfill:v1:daily:b2:d417`).

```json
{
  "pack_schema": 1,
  "schema_file": "schemas/af-level-1.schema.json",
  "pack_id": "mundo_03_v1", "kind": "campaign",
  "generator": { "tool": "level-generator", "version": "1.3.0", "commit": "a1b2c3d", "built_at": "2026-10-05T12:00:00Z" },
  "world": 3, "world_id": "w03_manaos", "lap": 1, "water_base": "black",
  "pool": ["acai","aguaje","tapereba","guanabana","bacaba","agua_tapajos","camu_camu","urucum","agua_negro"],
  "k_min": 7, "k_max": 8, "max_risk_pairs": 1, "cap_default": 4, "empties_default": 1,
  "cycle20": true,
  "levels": [
    {
      "schema": "af-level/1",
      "id": "m03_v1_n0043", "level": 43, "world": 3, "lap": 1,
      "seed": "amazonfill:v1:m03:v1:n0043",
      "gen": { "tool": "level-generator", "version": "1.3.0", "source": "walk", "depth": 20 },
      "water_base": "black",
      "palette": ["guanabana","tapereba","aguaje","urucum","bacaba","acai","agua_tapajos"],
      "units_per_color": 4,
      "caps":  [4, 4, 4, 4, 4, 4, 4, 4, 4],
      "cuias": [[8,1,3,5], [7,10,4,8], [4,3,10,7], [], [5,7,1,4], [3,8,5,10], [10,5,7,1], [1,4,8,3], []],
      "attrs": [null, null, null, { "type": "hoja", "key": 4 }, null, null, null, null, null],
      "murky": false, "hidden": [],
      "difficulty": { "label": "normal", "bot_fail": 0.0, "par": 23, "branching": 2.39, "traps": 0.0, "complexity": 56, "D": -0.76, "band": "comfort" },
      "risk_pairs": 1,
      "intro_mechanic": "hoja", "skippable": false, "hint_supported": true,
      "relief": [
        { "type": "minus_color", "removed": 1, "caps": [4, 4, 4, 4, 4, 4, 4, 4, 4],
          "cuias": [[8,3,5], [7,10,4,8], [4,3,10,7], [], [5,7,4], [3,8,5,10], [10,5,7], [4,8,3], []],
          "attrs": [null, null, null, { "type": "hoja", "key": 4 }, null, null, null, null, null],
          "par": 19, "bot_fail": 0.0 },
        { "type": "plus_empty", "caps": [4, 4, 4, 4, 4, 4, 4, 4, 4, 4],
          "cuias": [[8,1,3,5], [7,10,4,8], [4,3,10,7], [], [5,7,1,4], [3,8,5,10], [10,5,7,1], [1,4,8,3], [], []],
          "attrs": [null, null, null, { "type": "hoja", "key": 4 }, null, null, null, null, null, null],
          "par": null, "bot_fail": 0.0 }
      ]
    }
  ]
}
```

El ejemplo canónico es el **nivel 43, debut de Hoja** (posición 3 del ciclo de 20, comodidad: `band` = `comfort`) y cumple la regla transversal 2 de 02 §9.2: fallo del bot < 20 %, 2 vacíos, una sola mecánica y un solo ejemplar (`intro_mechanic` = "hoja", `skippable` = false). La llave es Aguaje (índice 4), como en el texto del tutorial de 02 §9.1, y Aguaje y Achiote (par de riesgo 4-5) no quedan adyacentes en ninguna cuia (03 §4.5). Por tener 2 vacíos, `relief[0]` es la variante −1 color (03 §13: sale Guanábana, la eliminación resoluble de par más alto) y `relief[1]` la de +1 vacío, publicada con `par` nulo. Par, ramificación, trampas, complejidad y `bot_fail` salen del A* y del bot casual ×40 (03 §5-§6) con la regla de Hoja; `D` es ilustrativo (03 §7: suma de z-scores, negativa en un tablero fácil). En un tablero de debut el alivio no puede bajar `bot_fail` 0,20 porque ya parte de < 0,20: se nota en el par (23 → 19) y en el vacío extra, que es lo que exige `tools/level-qa` en ese caso (párrafo de validación más abajo; 03 §17 #8).

| Campo | Tipo | Regla |
|---|---|---|
| `schema` | string | `af-level/1` en cada nivel; `pack_schema` (int) versiona la estructura del pack: **1** = formato original; **2** = mecánicas de D-007 (contrato 21): tipo `matapi`, Liana con fruta (`key`) o de cuenta (`n`), uno y solo uno, campos obligatorios por tipo (hoja: `key`; remolino: `partner`; lluvia: `key` y `n`), `intro_mechanic` `matapi`, `chica`, `alta` y `murky` [P] y `difficulty.par_approx` [P]; **3** = el 2 más la Tinaja (D-019) y las variantes de la 1.0 con la firma del 2026-10-14 (D-023, punto 5; filas `attrs` e `intro_mechanic`) y la máscara de la tinaja en `relief` (D-023, punto 6). Con `pack_schema` 1 todo valida como antes. Los packs de la campaña v1 (11-200) usan el 2; el ejemplo de esta sección sigue en el 1 y valida igual (`python tools/validate_schema.py`) |
| `schema_file` (cabecera) | string | `schemas/af-level-1.schema.json`: ruta (desde la raíz del repositorio) del JSON Schema draft 2020-12 canónico contra el que se validó el pack |
| `kind` (cabecera) | enum | `campaign` (por defecto) / `daily` / `event`; en `daily` (pack `daily_pool_v{n}`, 03 §16) cada tablero lleva `slot: {band 1-3, day 0-729}` en lugar de `level`, `id` y `seed` de formato diario, y `difficulty`, `world` y `lap` opcionales (el pool se publica sin métricas y con `relief` = `[]`); la cabecera `daily` no exige `world`, `world_id`, `lap` ni `water_base` (el pool mezcla mundos; cada tablero declara su `water_base`) |
| `id` | string | Campaña y eventos: `m{mm}_v{v}_n{nnnn}` con el mismo padding que la semilla (`m03_v1_n0043`); Reto del río: `d{b}_n{nnnn}` (patrón `^d[1-3]_n[0-9]{4}$`, p. ej. `d2_n0417`). Las variantes aliviadas no tienen id propio: viven en `relief` y se identifican en analítica como `variant ∈ {base, plus_empty, minus_color}` |
| `seed` | string | Campaña y eventos: `amazonfill:v1:m{mm}:v{v}:n{nnnn}` con ceros a la izquierda; Reto del río: `amazonfill:v1:daily:b{b}:d{ddd}` (patrón `^amazonfill:v1:daily:b[1-3]:d[0-9]{3}$`, p. ej. `amazonfill:v1:daily:b2:d417`); reproduce el tablero `base` en `tools/level-generator` (prueba de determinismo por hash, dirección 7) |
| `level`, `world`, `lap` | int | Nivel de ruta, mundo 1-13, vuelta (capítulo 3 §10); obligatorios en campaña y eventos. En `daily` no hay `level` (lo sustituye `slot`) y `world` / `lap` son opcionales |
| `gen` | objeto | `{tool, version, source ∈ walk/deep/fill, depth/candidates}` (capítulo 3 §10) |
| `water_base` | enum | `black` / `white` / `clear` (glosario §11) |
| `palette` | lista de IDs de tinte | Tintes presentes en el nivel (K = longitud); sirve a la validación de debuts y de `k_max`; **no define índices** (ver `cuias`) |
| `units_per_color` | int u objeto | 4; objeto `{"13": 5}` para fruta rara |
| `caps` | lista int | Capacidad por cuia (1-6) en orden visual (Cuia chica 1-3, Cuia alta 5-6; dirección 4) |
| `cuias` | lista de listas de int | Capas de abajo arriba; cada unidad es el **índice de motor** de `tints.json`: 1-14 tintes, **15 = Barro**, 16 reservado (K_motor = 16, dirección 3); `[]` = cuia vacía. En memoria `BoardState` empaqueta cada cuia como `len` (3 bits, 0-6) + un nibble por unidad con el valor **tinte − 1** (0-15): así caben los 16 índices sin reservar el 0 para "sin unidad" (la codificación anterior "0 = vacío + 1-15" solo admitía 15 tintes y dejaba al 16 sin representación). El JSON nunca usa la forma empaquetada ni la hexadecimal; la compacidad la aporta Addressables (LZ4/LZMA) |
| `attrs` | lista (misma longitud que `cuias`; `null` sin mecánica) | `{type ∈ hoja/brote/liana/remolino/lluvia/pirana, key, partner, n, k}` y, con `pack_schema` 2, también `matapi` (sin campos) (capítulo 3 §9; campos por mecánica en 02 §9.1): `key` = tinte llave de la Hoja, fruta de la Liana con fruta o tinte agua de la Lluvia; `partner` = índice 0-based de la pareja del Remolino; `n` = sellos necesarios (Liana de cuenta) o gotas programadas (Lluvia); `k` = ritmo en vertidos (Lluvia, Piraña; si falta rigen `lluvia_k_pours` / `pirana_k_pours`). **Con `pack_schema` 3 (D-019; lo hace T56):** `tinaja` en la tinaja y `cucharon` en su cuia, sin campos (una sola tinaja por nivel: cada pieza encuentra a la otra por su tipo). **Con `pack_schema` 3 y la firma del 2026-10-14 (D-023, punto 5),** también las variantes de la 1.0 (02 §9.1): `hoja_atada` con `key` (la fruta que recibe; su cuia tiene la capacidad `U_c` de esa fruta [P] y hay como mucho una Hoja atada por fruta, D-024), `flor` con `key` y `key2`, distintas (las dos frutas que la abren), y `brote` con `n` ∈ {1, 2} (las hojas del Brote de dos hojas; sin `n`, el Brote de siempre). El Barro no es un `attr` (`type` no admite `barro`): es la unidad 15 dentro de `cuias`. El catálogo exacto lo fija el capítulo de núcleo (glosario §6.5); las mecánicas de la 1.1 (Aguapé, Palizada, Lancha y Cocha) traerán tipos y campos nuevos (la propuesta de D-007 sugiere `group` y `keys` [P], y `oneway` para la variante Casiquiare) y subirán `pack_schema` (sección 4.4) |
| `murky`, `hidden` | bool, lista `[cuia, capa]` | Posiciones ocultas de Aguas turbias (índices 0-based); solo si `murky = true`; máx. `lvl_murky_max_cuias` (3) cuias y `lvl_murky_max_hidden_units` (**4**) unidades → ≤24 realidades que el solver enumera (dirección 6; canon 02 T2, incidencia 8; sin Pista parcial). **Tinaja (D-019):** también marcan sus capas ocultas (todas menos la boca), con un tope propio de 4 que no lee `lvl_murky_max_hidden_units`; el formato no cambia, pero sí el significado: todo lo que hoy lee «hay ocultos» como Aguas turbias tiene que distinguir la tinaja (level-qa, el informe del generador y unos 15 scripts de Unity) |
| `difficulty` | objeto | `{label, bot_fail, par, branching, traps, complexity, D, band}` (research/02 §7.1); obligatorio en campaña y eventos, opcional en `daily` (03 §16); `label` deriva de `bot_fail` (≥0,75 `hard`, ≥0,90 `very_hard`, dirección 5); `D` es el índice de 03 §7 (suma ponderada de z-scores del pool del rango: número sin límites, puede ser negativo); `band ∈ comfort / challenge / recovery / milestone` es el rol del slot en el ciclo de 20 (03 §12), no el cuantil medido: el remanso (p19) y los estrenos se escriben `comfort` [P]. Con `pack_schema` 2, `par_approx` (bool, opcional [P]) marcaría un par que no es el óptimo exacto; si falta, el par es exacto, y hoy ninguna mecánica lo necesita (la Piraña deja el par exacto con Q-C; 03 §9) |
| `risk_pairs` | int | Pares de riesgo presentes; ≤ `max_risk_pairs` de la cabecera |
| `intro_mechanic`, `skippable`, `hint_supported` | string o null / bool / bool | Flags de cliente: mecánica que debuta en el nivel (`hoja`, `brote`, `liana`, `remolino`, `barro` (en el 123), `lluvia` o `pirana`; con `pack_schema` 2, también `matapi` (63), `chica` (14), `alta` (27) y `murky` (100) [P], con los ids de `mech_debut_level` y de Aguas turbias; formato ratificado el 2026-10-13, pregunta 30 de GDD.md §6.3; con `pack_schema` 3, también `tinaja` (323), D-019, y, con la firma del 2026-10-14 (D-023, punto 5), `liana_n` [P] (243), `hoja_atada` (283), `brote_n2` [P] (343) y `flor` (403), según 02 §9.1) o `null`; Salto del Boto y Morpho permitidos (los niveles de debut no se saltan: `skippable` = false, 04 §2.6). El cliente arma la tarjeta previa del estreno con `intro_mechanic` (02 §10) |
| `relief` | **lista** de tableros (obligatoria desde el nivel 31: el esquema exige `minItems` 1 con `if`/`then`; `[]` antes y en `daily`) | Cada entrada `{type ∈ plus_empty/minus_color, caps, cuias, attrs?, hidden? (solo con la tinaja), removed? (índice del tinte quitado, solo minus_color), par, bot_fail}` es un tablero completo pregenerado (+1 vacío o −1 color); `par` es entero ≥1, o `null` solo en `plus_empty` (variante que el pipeline no resuelve, 03 §13). El orden es la prioridad: el cliente sirve `relief[0]` al tercer fallo consecutivo (`relief_after_fails`) con el banner "El río bajó" (`anim_river_lowered_banner_ms`, 2.000 ms) y `relief[1]` si existe un segundo alivio (`relief_second_step_fails`); el generador ordena la lista con la regla del capítulo 3 §13 (`relief_mode`). En un nivel de Aguas turbias las variantes van sin dosis ocultas [P] (`tools/level-generator`, `Relief`): `relief_board` solo admite `hidden` con `pack_schema` 3 y si y solo si su `attrs` contiene la tinaja (D-019; decidido por el dueño el 2026-10-14), y entonces es obligatorio y lleva la máscara de la tinaja en la variante, todas sus capas menos la boca (el mismo formato de `hidden`, como mucho 4 pares; que sea exactamente esa máscara lo comprueba `tools/level-qa`). Dirección 7; capítulo 4 §2.7 |
| `solution`, `safe_path`, `qa` | — | Solo pipeline; se eliminan del pack publicado (capítulo 3 §10) y `tools/level-qa` falla si aparecen. Morpho usa el solver en cliente (`hint_budget_nodes`, por realidad en Aguas turbias; al agotarse no se cobra y no hay Pista parcial), no una solución almacenada |

Validación en `tools/level-qa` (falla el CI): JSON inválido contra `schemas/af-level-1.schema.json` (que ya exige los formatos de `id` y `seed` de campaña y de Reto del río, `relief` no vacía desde el nivel 31 y `par` nulo solo en `plus_empty`), más la igualdad de longitud de `caps`, `cuias` y `attrs`, que el esquema no puede expresar; tinte con `level_debut` posterior al nivel; `risk_pairs` > `max_risk_pairs`; K > `k_max`; `par < 2·K`; `label` incoherente con `bot_fail`; Aguas turbias sin vertido seguro en alguna realidad; alguna variante de `relief` irresoluble o, si el tablero `base` tiene `bot_fail` ≥ 0,20, con `bot_fail` que no baja ≥ 0,20 (con base < 0,20, como los debuts de 02 §9.2, basta `par` menor en `minus_color` o el vacío extra de `plus_empty`; 03 §17 #8); `bot_fail` fuera de la ventana del slot (< 0,20 en debuts); semilla que no reproduce el tablero `base` (research/02 C3.5 [E]; capítulo 3 §17). Tamaño [V-proto, capítulo 3 §10]: 567-715 B por nivel minificado con métricas (mediana 642 B); 100 niveles = 62,9 KB minificados / 4,2 KB gzip. Con los dos tableros de `relief` ≈1,3-1,5 KB por nivel [E] → los 700 niveles instalados (mundos 1-9) ≈1 MB sin comprimir, ≈0,07 MB en el AAB (Addressables LZ4/LZMA); el pool del Reto del río (2.190 tableros = 3 bandas × 730, sin `relief`) añade ≈1,4 MB / ≈0,1 MB comprimido [E]; cada paquete remoto de 100 niveles ≈150 KB / ≈10 KB.

### 4.2 Paleta global (`tints.json`)

Tabla `tinte_id → {index_motor, hex, pattern, icon, name_es/en/pt, world_debut, level_debut}` con los 14 tintes del glosario §9 y la lista de 10 pares de riesgo; `water_base ∈ {black:#4A2C14, white:#B08A5A, clear:#2E8B8B}` (research/07 §2 [E]). El shader lee los HEX de aquí en tiempo de carga; cambiar un color no exige tocar materiales.

### 4.3 Perfil de jugador

Extiende el esquema de inventario del capítulo 4 §10.1 (que se mantiene tal cual bajo `wallet`, `stock`, `daily`, `progress`, `level_state`, `applied_purchases`).

```json
{
  "profile_schema": 1,
  "uid": "firebase-anon-uid", "created_at": "2026-10-05T13:10:00Z",
  "first_app_version": "1.0.0", "locale": "es-419", "country_first": "CO",
  "consent": { "profile": "co_1581", "text_version": 1, "at": "2026-10-05T13:10:20Z",
               "analytics": true, "ads_personalized": false, "age_band": "18+" },
  "economy_ts": "2026-10-05T14:03:11Z",
  "wallet": { "seeds": 1240, "canoes": { "count": 4, "next_regen_at": "…", "infinite_until": null } },
  "stock": { "undo": 6, "hint": 2, "reveal": 1, "extra_cuia": 0, "shuffle": 0 },
  "daily": { "…capítulo 4 §10.1…" },
  "progress": { "max_level": 87, "world": 4, "since_last_skip": 14, "skipped_levels": [63],
                "content_version": "1.0.0", "map_seen_level": 80 },
  "streak": { "wins": 4, "shield_available": false, "river_full": false },
  "calendar": { "month": "2026-10", "claimed_days": [1,2,3,4,5], "last_claim_date": "2026-10-05" },
  "daily_challenge": { "last_date": "2026-10-05", "result": "win", "streak_days": 6 },
  "piggy": { "seeds": 1230, "cap": 3000 },
  "entitlements": { "no_ads": false, "starter_bought": false, "pass_until": null, "skins": ["cuia_vidrio"] },
  "settings": { "sound": true, "music": true, "haptics": true, "patterns": "auto", "language": "auto",
                "sloth_messages": true, "push": { "pre_prompt_shown": true, "granted": true } },
  "sloth": { "last_line": [0,0,0,0,0,0,0,0] },
  "album": { "fruits": [1,3,4,7,8,10], "fauna": [], "rivers": [1,2,3] },
  "level_state": { "…capítulo 4 §10.1…" },
  "applied_purchases": ["GPA.3312-…"],
  "stats": { "levels_won": 86, "fails_hard": 9, "rescues": 2, "rv_watched": 31, "sessions": 57 },
  "integrity": { "hmac": "…", "device_first": "…" }
}
```

Reglas: blob <100 KB (research/06 §Implicaciones 6 [E]) frente a 3 MB de Play Games Saved Games [V]; nunca se guardan estadísticas por nivel (van a analítica); `economy_ts` se actualiza en cada mutación de `wallet`/`stock`/`daily`; al pagar el hito 10 de la Crecida `streak.wins` vuelve a 0 y `streak.river_full` (río crecido, sin plazo de reloj) sigue en true hasta el siguiente fallo duro no escudado, también con `streak.wins` = 0 (05 §2.4); `hmac` con clave derivada del dispositivo disuade la edición manual del archivo (no es seguridad real: sección 10.6). **`progress.map_seen_level`** (D-013, 2026-10-13): entero, 0 por defecto, el último nivel cuyo estado del Mapa del río ya vio el jugador; se escribe al cerrar el Mapa y tras la franja de llegada de la victoria, y en la fusión de la sección 11 gana el máximo, como `max_level`. Con él, el icono del inicio lleva un punto si hay una llegada sin ver y el Mapa anima exactamente lo pendiente al abrirse. **No exige migrar el perfil:** el esquema aún no se ha publicado y un campo nuevo con un default razonable se añade sin migración (`Profile.cs`, `CurrentSchema` = 1). Todo lo demás del Mapa (posición, llenado, muestras, miradores, afluentes y banderas) se deduce de `max_level`, `skipped_levels`, `WorldTable` y el catálogo de paradas. **Con D-018 y D-016 (2026-10-13):** `settings.language` pasa a `"auto"` por defecto (seguir al teléfono; la elección del selector de Ajustes manda sobre el teléfono), `settings.sloth_messages` (bool, `true`: el ajuste «Mensajes de TiTino»; apagado, solo gestos) y `sloth.last_line` [P] (8 enteros pequeños, el índice de la última frase de TiTino de cada momento, para no repetirla). Van **en la misma migración** a `profile_schema` 2 (sección 4.4), con el avistamiento de fauna si se aprueba (Q11 del guion de la intro); mientras el esquema no se publique, se añaden con su default sin migrar, como `map_seen_level`. Frutas del río (D-017) no añade nada: lo revelado se deduce de `max_level` y `skipped_levels`.

### 4.4 Versionado y migraciones

| Artefacto | Clave de versión | Regla |
|---|---|---|
| Perfil | `profile_schema` (int) | Solo avanza; `Migrations[n](profile) → profile'` encadenadas al cargar; antes de migrar se escribe `profile.bak`; si una migración lanza excepción se conserva el perfil anterior y se registra `af_error{module=save}` |
| Paquetes de niveles | `pack_schema` (estructura, validada contra `schemas/af-level-1.schema.json`) + `generator.version` + `content_version` (catálogo publicado en `levels_remote`, anunciado por Remote Config) | Un paquete nuevo nunca cambia el tablero `base` de un nivel ya publicado (dirección 7: determinismo); reemplazos solo vía `level_overrides` o nuevo número de vuelta; el cliente rechaza un paquete remoto con `pack_schema` mayor al que conoce. La Tinaja (D-019) sube `pack_schema` a 3 en los dos casos de su Q-Ti3 (1.0 o 1.1): los packs 11-200 siguen con `pack_schema` 2 y el cliente acepta los dos (T56). Por eso las mecánicas de la 1.1 (Aguapé, Palizada, Lancha y Cocha; D-007, Q8 b′) llegan **en un build**: exigen código en `AF.Core` y un `pack_schema` nuevo, y sustituyen niveles de 201-700 con `level_overrides` o con una vuelta nueva; si el jugador ya pasó el debut de una de ellas, se le sirve una vez su tablero de debut antes de usarla (03 §9) |
| Remote Config | `rc_schema` (int, parámetro) | El cliente ignora claves con esquema mayor al que conoce y usa defaults |
| Datos de SDK | Versiones en `Packages/manifest.json` y `MaxSdk/` | Actualización trimestral con prueba en la matriz QA (sección 14) |
| Cloud Functions | `api_version` en la ruta (`/v1/ssv`) | Compatibilidad con la versión mínima de app soportada (`biz_min_app_version`) |

### 4.5 Mapa del río: `af-river-map/1` y `af-river-stops/1` (D-013; contratos 29 y 30)

Dos formatos nuevos, aprobados por el dueño el 2026-10-13 con el lote de documentación del Mapa del río (`docs/research/mapa-del-rio/propuesta-mapa-del-rio.md`, sección 11.2). Son contratos 29 y 30 (GDD.md §7.4); esta sección fija su sintaxis, con los extras [P] de P-M1 firmados el 2026-10-14 (D-023). Con ella son su copia única los esquemas `schemas/af-river-stops-1.schema.json` (tarea M1.1) y `schemas/af-river-map-1.schema.json` (M2.6), que valida `tools/river-map/test_schemas.py` con `jsonschema`, nunca `tools/validate_schema.py`, que es solo de `af-level/1`. **Estado (tanda 4, ola 2, `0f67f1f`):** `tools/river-map/stops.py` genera `river_stops_v1.json` con las paradas 0-19 de 1-200, y el cliente lo lee (`RiverStopCatalog`, M1.2) para el fondo por parada, la llegada y el rótulo del lugar; `tools/river-map/geometry.py` genera `river_map_v1.json` desde `rio-amazonas.json` de respaldo (M2.6), y el cliente aún no lo dibuja (fase F2 de la propuesta y siguientes). Siguen `.claude/rules/data-json.md` (UTF-8 sin BOM, LF y orden de claves estable) y no van en Remote Config, como el catálogo de fauna.

**`Assets/_Project/Data/Map/river_map_v1.json` (geometría)** [P]:

```json
{
  "format": "af-river-map/1",
  "frame": { "rotation": "cw90", "lat": [-18.0, 7.0], "lon": [-80.0, -47.0], "lat0": -5.0 },
  "lines": [
    { "id": "amazonas", "kind": "main", "class": 1, "src": "ne10m", "pts": "<base64 uint16 x,y delta>",
      "water": [{ "s0": 0.0, "s1": 0.085, "type": "andean" }, { "s0": 0.085, "s1": 1.0, "type": "white" }],
      "splits": [{ "id": "encontro", "s0": 0.709, "s1": 0.722, "left": "black", "right": "white" }] },
    { "id": "xingu", "kind": "passby", "class": 2, "src": "ne10m", "water": [{ "s0": 0, "s1": 1, "type": "clear" }],
      "mouth": { "on": "amazonas", "s": 0.926 }, "fill_on_pass": 1.0, "pts": "..." }
  ],
  "land": { "verts": "<base64>", "tris": "<base64 uint16>", "zones": [{ "id": "land", "tile": "canopy", "first": 0, "count": 38 }] },
  "insets": [{ "id": "orinoco", "rect": [0.78, 0.05, 0.98, 0.30], "lines": ["orinoco"], "link": "casiquiare" }],
  "home": { "on": "para", "s": 0.728 },
  "labels": [{ "country": "PE", "xy": [26214, 6752] }],
  "viewpoints": [{ "id": "isla_de_marajo", "xy": [44620, 60408], "reveal_on": 10, "fact": "map.stop.marajo.dato_01" }],
  "passing": [{ "id": "obidos", "line": "amazonas", "s": 0.831, "fact": "map.stop.obidos.dato_01" }]
}
```

- **Proyección:** equirrectangular con el factor cos(lat0), lat0 = −5°, girada 90° en el sentido de las agujas del reloj y sin espejo (Andes arriba, Atlántico abajo y norte a la derecha; 06 §4.5): la forma no se deforma, solo gira.
- **`lines[].kind`:** `main` (el cauce y el ramal del Pará), `tributary` (afluente de un mundo), `passby` (afluente de paso, que se llena entero al pasar su boca) o `local` (río local de una parada, solo en la vista Mundo). `class` es el grosor [P] (1, el cauce y el ramal; 2, los afluentes; 3, los locales) y `src`, el origen de la línea: `ne10m` (Natural Earth), `hydrorivers` o `rio_amazonas`, el respaldo de la investigación mientras no lleguen los otros dos (M2.6). `water[]` da el tipo de agua por tramo (`white`, `black`, `clear` y, en El origen, `andean` [P]), en tramos seguidos de 0 a 1, o vacío si el agua está sin comprobar ([NV]: la línea se dibuja solo en vidrio); `splits[]`, las franjas de dos aguas de Santarém y del Encuentro; `mouth`, la línea anfitriona y la `s` de la boca; `fill_on_pass`, la fracción que se llena al pasar la boca. Las líneas `main` no llevan `mouth` ni `fill_on_pass`, y las demás llevan siempre `mouth` y `fill_on_pass` y nunca `splits`.
- **`land.zones[]`:** `{id, tile, first, count}`; `first` y `count` [P] (M2.6) son los triángulos de la zona, seguidos en `tris`. En la 1.0, una sola zona (`land`, con el mosaico `canopy`) hasta la máscara del artista. `insets` puede ir vacío: la 1.0 no lleva recuadros.
- **Extras [P] de P-M1 (D-023), obligatorios en la raíz (pueden ir vacíos):**
  - `labels[]`, los rótulos de los países en su región (sin fronteras en tierra, Q8-A de D-013): `{country, xy}`, con el ISO 3166-1 alfa-2 y el punto del rótulo; el texto es `map.country.<iso2>.name`;
  - `viewpoints[]`, los miradores, un «?» fuera de la ruta: `{id, xy, reveal_on, fact}`, con `reveal_on` = el nivel cuya victoria lo revela (sin estado nuevo en el perfil) y `fact` = la clave del dato que enseña al tocarlo, o null; su nombre es `map.stop.<id>.name`;
  - `passing[]`, los puntos de paso, un nombre en el río que sale al pasar durante un viaje: `{id, line, s, fact}`, con `fact` = la clave de su dato, o null; su nombre es `map.stop.<id>.name`, y los citan los `leg.via` de las paradas;
  - las claves de `fact` son del bloque `map.*` de `facts.csv`: `map.stop.<id>.dato_NN`, `map.country.<iso2>.dato_NN`, `map.myth.<tema>` o `map.general.<tema>`.
- **Codificación [P] (P-M1):** `pts`, `verts` y `tris` van en base64 (RFC 4648) de enteros uint16 little-endian. En `pts` y `verts`, el primer par (x, y) es absoluto y los demás son deltas módulo 65.536; `tris` son índices absolutos.
- **Puntos y `s` [P] (M2.6):** cada punto es un par uint16 (X, Y) = (round(65535 · x), round(65535 · y)) del marco, con x = (lat − lat_min) / (lat_max − lat_min) e y = (lon − lon_min) / (lon_max − lon_min); así van también los `xy` de `labels` y `viewpoints`. En pantalla, px = x · W y py = y · W · aspecto, con aspecto = (lon_max − lon_min) · cos(lat0) / (lat_max − lat_min). `s` es la fracción de la longitud de una línea medida en pantalla (X / 65535 e Y / 65535 · aspecto) desde su primer punto. Los puntos de cada línea van de la cabecera a la boca (el cauce, del Mismi al Atlántico; el ramal del Pará, de Gurupá a la bahía), así que **`s` crece río abajo** [P], en el Mapa y en las paradas (`at` y `leg`): un tramo río arriba va de una `s` mayor a otra menor.
- **Origen:** lo genera `tools/river-map/` (Python 3.14; los generadores, solo con la biblioteca estándar, y las pruebas de los esquemas, con `jsonschema`, que ya está en el stack; D-023) desde Natural Earth 1:10m (vista Cuenca) y HydroRIVERS (vista Mundo), descargados por el dueño en `tools/river-map/source/` con su SHA-256, y su `--check` valida cada punto de control de `rio-amazonas.json` a ≤ 2 dp de su vista (≤ 15 km en la vista Cuenca), que los tramos del cauce midan al menos la línea recta, que las rutas no crucen tierra y que los km no retrocedan. Mientras no llegue la geometría de producción, la pantalla se puede construir con `rio-amazonas.json` de respaldo (la forma correcta a escala de cuenca, sin meandros).
- **Tamaño:** unos 70-90 KB con las 100 paradas [cálculo].

**`Assets/_Project/Data/Map/river_stops_v1.json` (paradas y tramos)** [P]: una entrada por parada (100 en total; 20 en 1-200 y 70 en 1-700), con la ruta de cada tramo para que la canoa siga el agua real, con ida y vuelta por las confluencias. La raíz [P] (P-M1) es `{ "format": "af-river-stops/1", "source": "docs/research/mapa-del-rio/rio-amazonas.json", "stops": [ … ] }`, en orden de `n`; `source` es la ruta de la fuente desde la que `tools/river-map/stops.py` genera el archivo. Una parada:

```json
{ "n": 14, "levels": [141, 150], "world": 5, "id": "iquitos", "kind": "gate", "country": "PE",
  "country_name": null, "at": { "line": "amazonas", "s": 0.348 }, "km_from_sea": 3700, "km_home": 3729,
  "sample": "white", "flag_raise": "PE",
  "arrival_fact": "map.country.pe.dato_01", "facts": [],
  "world_question": "map.world.w05.question", "answers_world_question": false,
  "bg": { "kit": "bg_kit_w05", "hero": "bg_hero_iquitos", "code": null, "lut": null },
  "leg": { "path": [{ "line": "amazonas", "s0": 0.420, "s1": 0.348 }], "via": ["pebas", "boca_del_napo"], "mouths": ["napo", "nanay", "itaya"] } }
```

- **`at` y `leg`** son obligatorios desde que existe `river_map_v1.json` (M2.6) y van sobre sus líneas: `at`, el punto del río de la parada (en n = 0, el pie de Belém sobre el ramal del Pará); `leg`, el tramo desde la parada anterior, con `path` (tramos de línea, de `s0` a `s1` en el sentido del viaje; en n = 0, uno de longitud cero), `via` (los ids de `passing` que cruza) y `mouths` (los afluentes cuya boca se llena por primera vez). `facts` son las claves de los «Otro dato» de la tarjeta de la parada (vacío si no lleva).
- **`n`** es la k de la parada (que cubre los niveles `10k+1` … `10k+10`; su id visible es `s{nn}`, glosario §11); **`kind`** ∈ {`gate` (gran parada), `stop` (parada), `close` (cierre de mundo)} [P].
- **Extras [P] de P-M1 (D-023):**
  - `km_home`: los km por agua hasta la casa de TiTino, en el estuario, para «Casa · a unos {km} km» (`map_home_distance`); 0 en n = 0. `km_from_sea` es null en n = 0;
  - `answers_world_question`: true en la única parada de cada mundo cuyo dato de llegada responde la pregunta del mundo (el ✓ de la cabecera del Mapa), siempre después de la parada que la plantea;
  - `country_name`: la clave del nombre de país de la placa cuando no basta con el de `country` (en n = 13, «Colombia y Perú», `map.stop.rio_frontera.country`); null en las demás. Cuando no es null, es siempre `map.stop.<id>.country`, con el id de la parada (nombres por convención, abajo);
  - `flag_raise`: la bandera que se iza al llegar; BR en n = 0 (la del primer Mapa) y después solo al cambiar de país (en 1-200, CO en n = 8 y PE en n = 14); null en las demás;
  - `sample` ∈ {`white`, `black`, `clear`, `clear_white`, `black_white`}: el agua de la mini cuia del Muestrario;
  - `bg.code`: `codigo:<pieza>` (pieza por código, como las líneas entre aguas de Santarém y del Encuentro) o `kit:<pieza>` (parada solo con el kit), o null; en 1-200, cada parada lleva uno y solo uno de `bg.hero` y `bg.code`;
  - `bg.pieces`: las piezas del kit que lleva la parada, cuando se pinten los kits; su forma la fija el esquema al añadirlo (aún no está en él).
- **Nombres por convención [P] (P-M1, con la forma de claves de la pregunta 49 de GDD.md §6.3, (a)):** tres claves sin campo. El nombre del sitio es `map.stop.<id>.name`; su río, `map.stop.<id>.river`; y el nombre de su país, `map.country.<iso2>.name` (o la clave de `country_name`, si no es null). Dos campos siguen la misma forma, y el esquema y el `--check` la exigen: cuando no son null, `country_name` es `map.stop.<id>.country`, con el id de la parada, y `world_question`, `map.world.w{nn}.question`, con el mundo de la parada. Son claves de contenido, con puntos (sección 4.7; glosario §10-§11).
- **Datos de llegada:** `arrival_fact` es null en n = 0; en n = 1 es `map.stop.estuario.dato_03`, el Dato del río 1 del Mapa del 10 (06 §2.3), y en n = 20, `map.myth.source`, el dato de la victoria del 200 (Q16-A de D-013).
- **Textos:** solo claves (`arrival_fact`, `facts`, `world_question` y las de los nombres), nunca el texto. Los textos, con su fuente y su estado `pendiente_asesor`, van en `facts.csv` (sección 4.7, contrato 32), en un bloque de filas `map.*` que solo regenera `tools/river-map/stops.py`; el Mapa no tiene archivo de textos propio (D-023). El dato mide ≤ 90 caracteres en ES, la pregunta ≤ 50 y el nombre ≤ 32 [P].
- **Validación** (`RiverStopCatalog`, AF.Levels, sin motor; y el `--check` de la herramienta, que además regenera en memoria y falla si lo commiteado está desfasado respecto a `rio-amazonas.json`): toda parada empieza en un nivel `10k+1` y respeta `WorldTable`; cada fondo tiene su ranura o su respaldo; cada dato y cada pregunta existen y miden lo que deben; cada etiqueta [V] o [E] lleva a una fuente con URL; ningún id, nombre ni ranura lleva «Belén» (D-013, punto 11). `StopOf(level)` no asigna memoria.

### 4.6 Frutas del río: `af-facts/1` (D-017; contrato 31)

Formato nuevo, aprobado por el dueño el 2026-10-13 con el lote de documentación de Frutas del río (`docs/research/frutas-del-rio/propuesta-frutas-del-rio.md`, sección 6). Es el contrato 31 (GDD.md §7.5). Sin implementar (F1-F2 de la propuesta). Sigue `.claude/rules/data-json.md` y no va en Remote Config.

**`Assets/_Project/Data/Content/frutas_del_rio_v1.json` (calendario)** [P]:

```json
{
  "format": "af-facts/1",
  "version": 1,
  "max_chars_es": 90,
  "series": [
    { "subject": "acai",         "kind": "tint", "from_level": 1,  "lines": 20, "step": 1 },
    { "subject": "agua_tapajos", "kind": "tint", "from_level": 21, "lines": 20, "step": 1 }
  ],
  "sources": { "A1": { "title": "Embrapa, Plantas para o Futuro – Região Norte, cap. açaí", "url": "https://…" } }
}
```

- **`series`:** `subject` es el id del tinte del glosario §9 (`acai`, `urucum`, `agua_negro`…), que no cambia aunque cambie el nombre visible; `kind` ∈ {`tint`} [P] (más adelante, ríos y especies con el mismo formato); `step` 1 = una línea por nivel y 2 = una cada dos niveles (las series de especie de 10 líneas, sin cambiar el formato).
- **Elección de la línea** (determinista, sin azar, reloj, diccionarios ni asignaciones por nivel): la serie con `from_level ≤ n < from_level + lines × step`; si no hay, no hay línea. Con `d = n − from_level`, si `d` no es múltiplo de `step`, no hay línea; si lo es, `k = d / step + 1`. Se muestra según `ui_fruit_line_every_n_levels` [P] (sección 9): con 1, en cada victoria; con N > 1, solo si `n` es múltiplo de N; con 0, nunca (propuesta de Frutas, 6.4, punto 3); y solo si en esa victoria no manda una línea de más prioridad: frase de TiTino en sus tres hitos > dato de llegada > línea de fruta (02 §6; pregunta 45 de GDD.md §6.3, decidida el 2026-10-14: (a); D-022). Una línea desplazada no sale en ninguna victoria: queda revelada en la ficha. Una línea está revelada (ficha) si su nivel es ≤ `progress.max_level` o está en `progress.skipped_levels`. Si la clave no existe en el idioma del jugador, la línea no se muestra.
- **Textos:** solo en la tabla de textos largos (sección 4.7), nunca en el calendario. El `fact_id` de `af_river_fact_shown` es `<tinte>.<kk>` (`acai.03`) y nunca se reutiliza: si el asesor veta una línea, la nueva ocupa la misma posición en una versión nueva del calendario.
- **Validación** (prueba EditMode, propuesta 6.6): los bloques cubren el tramo declarado sin huecos ni solapes (1-320 en la versión 1); ninguna serie empieza antes del debut de su tinte; cada clave existe con texto ES de ≤ 90 caracteres; cada línea lleva una fuente del catálogo, su marca [V] o [E] (ninguna [NV]) y su estado; todos los caracteres están en el atlas.

### 4.7 Tablas de textos (D-018; contrato 32)

Formato nuevo, aprobado por el dueño el 2026-10-13 con la propuesta de idiomas (`docs/research/idiomas/propuesta-idiomas.md`, secciones 3.3-3.4). Es el contrato 32 (GDD.md §7.5). Sin implementar (niveles 1-2 de la propuesta).

- **Archivos:** `Assets/_Project/Data/Loc/strings.csv` (UI, tutorial, tienda, Ajustes, modales, avisos, notificaciones y etiquetas del lector de pantalla), `facts.csv` (textos largos: datos de fruta, de fauna y del Mapa del río) y `glossary.csv` (término, tipo, un valor por idioma y notas; se deriva del glosario §9-§10). UTF-8 sin BOM, LF y RFC 4180, con las filas en orden ordinal por clave [P] (`sorted()` en Python y `StringComparer.Ordinal` en C#; `.claude/rules/data-json.md`; D-023). Las filas `map.*` de `facts.csv` las regenera solo `tools/river-map/stops.py` (sección 4.5).
- **Claves** (pregunta 49 de GDD.md §6.3, decidida el 2026-10-14: (a); D-022): dos familias. Las de UI, en `snake_case` y sin puntos (glosario §10; regla 1 de la propuesta de idiomas); las de contenido largo (datos de fruta, del Mapa y de fauna), con puntos, como las aprobó D-013 y las fija Frutas del río: `fact.<tinte>.<kk>`, `fact.<tinte>.tag`, `tint.<tinte>.name`, `map.stop.<lugar>.dato_NN` (sección 4.5) y `fauna.bradypus_variegatus.dato_<nn>`. La fauna que ya existe en los datos conserva `fauna_<id>_dato`. El `fact_id` de `af_river_fact_shown` es el de D-013 (la clave entera del dato de llegada) o el de Frutas (`<tinte>.<kk>`; sección 8.2).
- **Columnas** (`strings.csv` y `facts.csv`, con la misma cabecera; `glossary.csv` sigue con la suya; D-022): `key,es-419,en,pt-BR,context,max_chars,status,status_en,status_pt-BR,notes`, más la columna `args` [P] al final, con los valores de las cifras de los textos largos (Q8-A de idiomas: el texto lleva `{n}` y el valor va en `args`). `context` es el contexto para el traductor (dónde sale, quién habla, qué es cada marcador, la fuente del dato y una captura); `max_chars`, el límite en español, y para los demás idiomas la regla de 3.7 de la propuesta de idiomas (el doble en textos cortos y +30-40 % en los largos). **Una columna de estado por idioma:** `status` (español: `borrador` → `pendiente_asesor` → `aprobado_es`) y `status_en`, `status_pt-BR` (y `status_fr`… al añadir otro: vacío → `traducido` → `revisado_nativo`). Una traducción solo vale con `status` en `aprobado_es`; si el asesor cambia el español, los estados de los demás idiomas vuelven a vacío (propuesta de Frutas, 6.1). En la tanda 2, una columna de texto y otra de estado por idioma nuevo.
- **Reglas** (propuesta de idiomas, 3.2): una frase completa por clave, con marcadores con nombre (`{n}`, `{level}`, `{price}`), nunca pegada a trozos ni con `Replace`; plurales con la sintaxis `{n:plural:…|…}` y las categorías CLDR (nunca `n == 1`); cifras con `Loc.Number` (en los datos de fruta, con `{n}`; D-018, Q8-A); precios solo de la tienda; sistema métrico sin convertir y U+00A0 entre cifra y unidad; sin `ToUpper` ni `ToLower` sobre texto visible.
- **Compuerta de release** [P]: un build con el `applicationId` definitivo falla si una cadena de UI de un idioma ofrecido está vacía, si una cadena de UI de un idioma ofrecido distinto del español no tiene su `status_<idioma>` = `revisado_nativo` (D-018, Q5-A: nunca traducción automática sin revisión en algo que vea el jugador; D-022, punto 6) o si una cadena cultural no está en `aprobado_es`; en español, la compuerta solo exige `aprobado_es` a las cadenas culturales. Un dato sin traducir no se muestra (D-018, Q10-A). **Visibilidad por idioma** (textos largos de `facts.csv`: datos de fruta, del Mapa y de fauna): en español, un dato solo se muestra con `status` = `aprobado_es`; en otro idioma, solo si además su `status_<idioma>` = `revisado_nativo` (no hay columna `status_es-419`: en español rige `status`); sin traducir o sin revisar, no se muestra (D-018, Q10-A; D-022, punto 6; propuesta de Frutas, 6.3), así que un borrador por IA en `traducido` nunca llega al jugador. Las cadenas de UI no se ocultan: una sin revisar en un idioma ofrecido distinto del español detiene el build de release (la compuerta de arriba; D-018, Q10-A: la UI incompleta detiene un build de release). Los builds de prueba solo avisan.
- **Fase 3:** las String Tables de Unity Localization 1.5.13 se generan desde estas tablas con un método de Editor versionado y se commitean como generadas; una prueba comprueba que coinciden.

## 5. Render del líquido y presupuesto de rendimiento

### 5.1 Composición de una cuia (de atrás hacia delante)

| Capa | Técnica | Draw calls típicos |
|---|---|---|
| Fondo de río (agua base del mundo) | Sprite + shader `River` con desplazamiento de UV; preset `water_base` (negra #4A2C14 / blanca #B08A5A / clara #2E8B8B, research/07 §2 [E]) | 1 |
| Vidrio trasero y pie de fibra | Sprite del atlas de skin | batch 1 |
| **Líquido** | Quad con material `Liquid` (Shader Graph, Sprite Unlit) recortado por **Sprite Mask** de la cavidad interior (research/06 §2 [V]); `MaterialPropertyBlock` por cuia | 1 por cuia (14-16) |
| Iconos y patrones de fruta por dosis | El patrón va siempre en el shader `Liquid` (5.2): al 15 % sin Patrones de fruta y al 20 % con él. Los iconos son sprites del atlas `Fruits` (procedurales [P] hasta el arte del artista) colocados por el controlador según `fill[i]`: con Patrones de fruta, en cada dosis visible; sin él, solo en la cuia levantada, mientras lo está. Nunca en las dosis ocultas, en el Barro ni en el agua base (07 §3, reglas 1 y 2; D-012). En el prototipo uGUI, desde la tanda 2 (B3, `94905b8`) [P], el icono no es un sprite: lo pinta el propio shader del líquido (`AF_UI_Liquid.shader`), con un atlas propio de 14 siluetas procedurales (`_IconAtlas`). Eso suma una muestra y unas 53 instrucciones por camino en las bandas con icono: 2 muestras por píxel, salvo la banda que se revela con icono, que lleva 3 durante ≈ 140 ms (patrón, icono que entra y «?» que se disuelve), excepción aceptada por el dueño el 2026-10-13 (D-015; pregunta 44 de GDD.md §6.3). Deja el shader por encima de las ≤ 40 ALU de la guía visual v2 §8. **La medida en un teléfono de clase A (GPU ≤ 10 ms y 60 fps estables, sección 5.4) es una acción del dueño**, que tiene que conseguir el teléfono; hasta entonces, nada nuevo en el líquido | batch 1-2 |
| Agua parda de Aguas turbias | Rama del mismo shader: las dosis marcadas en la máscara de bits `_HiddenMask` (un bit por capa, hasta 6; admite ocultos no contiguos, como `hidden` de 4.1) se pintan con `_MurkyColor` = #8A7B6A (07 §4.3) + trama diagonal al 25 % y sprite "?". **La Tinaja (D-019) no usa esta rama:** su vasija es opaca (barro, 07 §4.3), así que sus ocultas no se pintan con `_MurkyColor` ni con «?»; solo se ven la boca y las bandas reveladas | 0 extra |
| Vidrio frontal, brillo especular estático, marcas de dosis | Sprite semitransparente del atlas de skin | batch 1 |
| Tapa de calabaza (Cuia sellada) | Sprite + partículas cortas | batch |
| Chorro de vertido | `LineRenderer` 12-16 puntos sobre Bézier cuadrática (research/06 §2 [V]) | 1 mientras dura |
| Burbujas | ParticleSystem 5-10 partículas con `Visible Inside Mask` (research/06 §2 [E]) | 1 mientras dura |

### 5.2 Shader `Liquid` (Shader Graph)

Propiedades: `_Count` (int ≤6), `_Fill0…_Fill5` (altura acumulada normalizada de cada dosis), `_Col0…_Col5` (RGBA), `_WaveAmp`, `_WaveFreq` (6-8), `_WaveSpeed`, `_Phase`, `_MeniscusH` (alto de la cara superior del líquido: una elipse de 0,40 u, ≈ 0,4 de dosis, del ancho de la cavidad y más clara que el tinte; D-006. Normalizado al quad: 0,40 u / alto del quad, 0,10 en la cuia de 4, con 4,00 u. En el shader uGUI del prototipo, `AF_UI_Liquid.shader`, ya aplicado en U3, es la global `_AFTopFaceU` = 0,40 u, en u absolutos, que fija `LiquidGraphic.ApplyShaderGeometry` desde `CuiaGeometry.TopFaceU`), `_EdgeDarken` (0,25), `_HiddenMask` (máscara de 6 bits, entero 0-63: bit i = dosis i oculta; 0 = sin ocultos; los ocultos pueden ser no contiguos, 4.1 y 07 §4.3), `_MurkyColor` (#8A7B6A, agua parda de 07 §4.3), `_PatternAtlas`, `_PatternOpacity` (0,15 sin Patrones de fruta y `ui_pattern_opacity`, 0,20, con él; D-012). Shader Graph no expone arrays: seis pares de propiedades cubren Cuia alta de 6 (glosario §4).

```
frag(uv):
  top   = _Fill[_Count-1]
  surf  = top + _WaveAmp * sin(uv.x * _WaveFreq + _Time * _WaveSpeed + _Phase)   # research/06 §2 [V]
  if uv.y > surf: return transparent
  i = step(_Fill0, uv.y) + step(_Fill1, uv.y) + … + step(_Fill4, uv.y)             # índice de dosis sin ramas
  hidden = bit(_HiddenMask, i)                                                       # bit(m, i) = fmod(floor(m / exp2(i)), 2) == 1: sin operaciones de bits (GLES3, Shader Graph)
  col = hidden ? _MurkyColor : _Col[i]                                               # ocultos no contiguos (hidden [cuia, capa] de 4.1); #8A7B6A, 07 §4.3
  if not hidden and not barro: col = lerp(col, col * pattern(_PatternAtlas, i, uv), _PatternOpacity)   # 0,15 sin Patrones de fruta (hasta el 40 por defecto) y `ui_pattern_opacity` (0,20) con él; se fija por código al cargar el nivel [P] (D-012; 07 §3, regla 1)
  if elipse_superior(uv, surf, _MeniscusH): col = lighten(col, aclarado(tinte))     # cara superior en elipse, +20 % (+25-30 % en tintes oscuros; 07 §3, regla 4; D-006)
  col *= 1 - _EdgeDarken * smoothstep(0.35, 0.5, abs(uv.x - 0.5))                   # curvatura lateral
  return col
```

Animación en C# (`LiquidController`): al verter, la dosis destino crece y la origen decrece al ritmo de `anim_pour_dose_ms` (280 ms por dosis; D-006) más la cola `anim_pour_tail_ms` (80 ms), claves de la sección 9 con los valores del capítulo 2 §4.1 (research/06 §2 [E] sugería 0,4-0,7 s en total; el 0,12 s por dosis del capítulo 7 §5.2 y la antigua `anim_pour_duration_ms` quedan absorbidos); `_WaveAmp` arranca en 0,03 y decae `exp(-t/0,25)` hasta 0,5-0,8 s; mezcla de color durante Llenar y teñir en OKLab para no pasar por grises (research/06 §2 [E]). El mismo controlador compone `_HiddenMask` de cada cuia a partir de `hidden` (bit = capa) y apaga el bit de una dosis cuando se revela (1.200 ms [P] por la regla y 600 ms [P] con Luciérnaga desde D-007: `MurkyMarks.RevealRuleMs` y `RevealFireflyMs`; 02 §4.1 y 07 §4.3). Coste: 1 muestra de textura (patrón) + ~30 ALU por píxel; la cuia ocupa <6 % de la pantalla → <1,5 ms de GPU en Adreno 610 para 16 cuias [P].

### 5.3 Llenar y teñir

| Fase | 1,4 s (niveles 1-20) | 0,8 s (desde el 21) | Qué ocurre |
|---|---|---|---|
| a · El río crece y desborda | 300 ms | 150 ms | Cuias vacías y secas en su ranura, sin caída; una capa aclara la franja de río (+12 L*) y sus destellos se deslizan hacia el panel; una lámina de agua desborda el canto superior del marco |
| b · Chorritos y llenado | 400 ms | 250 ms | Tiras de agua por el marco y las repisas y un chorrito por cuia no vacía; cada cuia se llena desde el fondo solo hasta sus dosis, con su propio retardo (no hay nivel de llenado global); todo lleno antes de 700 / 400 ms |
| c · Se calma | 250 ms | 150 ms | Los chorritos se cortan y la madera se escurre; `_WaveAmp` decreciente; la tropa de miquitos ya ha entrado trepando durante a-b (D-011) |
| d · Teñido | 100 ms por racha, solapado (≤450 ms) | 250 ms en total | Una fruta por racha visible (≤ 24; si no, una por cuia con cascada de 25 ms) cae con g = 12.000 dp/s²; al impacto, anillo y floración OKLab de 100 ms; filas de arriba abajo y, en la cuia, de abajo arriba; ≤ 20 partículas |

D-010 (2026-10-08) sustituye a «Sumergir y teñir» y cambia solo lo que se ve en cada fase; no cambia claves, defaults ni tiempos. D-011 (2026-10-11) tampoco: la tropa entra trepando en ≈ 500 ms durante a-b, reposa ≈ 1.500 ms tras la última suelta, ya en juego, y sale en ≈ 500 ms; son constantes [P] del cliente (`IntroChoreography.EnterMs`, `RestMs` y `ExitMs`), sin claves, y T, el calendario de la fruta y el instante en que se habilita el toque no cambian (02 §4.2). Implementación: un planificador puro y determinista (`IntroChoreography`, en `AF.Presentation`) calcula una vez, al arrancar, el calendario de agua, frutas y tropa en arrays preasignados (presupuesto [P]: ≤ 2 ms en clase A, sin medir en un dispositivo de esa clase; 0 B por fotograma después); la especie de miquito sale de un catálogo de datos (`Assets/_Project/Data/Fauna/miquitos_v0.json`, lector en `AF.Levels`; 07 §6.4), nunca de Remote Config; todas las capas de la intro van en Canvas anidados propios con `raycastTarget = false`, para no reconstruir el Canvas raíz; presupuesto en 07 §5.1. Las fases y totales son los del capítulo 2 §4.2 (dueño de la secuencia: 0,30 + 0,40 + 0,25 + ≤0,45 solapado = 1,4 s; versión corta 0,15 + 0,25 + 0,15 + 0,25 = 0,80 s); el capítulo 7 §5.1 y el roadmap (día 6) se leen con estas cifras. Reglas (dirección 18; research/06 §2 [E/V]): solo arranca con el nivel parseado y los atlas residentes; cualquier toque desde el primer fotograma la salta (`af_anim_skipped`); si la tasa de salto supera `anim_tint_skip_alarm_pct` (40 %) en 1-20, `anim_tint_duration_ms_early` baja a 1.200 (mínimo de la dirección 18; nunca a 1.000 ni a 800 en 1-20); el estudio citado sitúa la satisfacción en 70 % a ~1,6 s y 50 % a ~2,7 s (research/06 §2 [V]). La duración vive en Remote Config con las claves de la sección 9 (`anim_tint_duration_ms_early` 1.400 / `anim_tint_duration_ms_late` 800 / `anim_tint_fast_from_level` 21 / `anim_tint_skippable`), que sustituyen a `tint_anim_ms_*` (02) y `tint_anim_duration_*` (07). La pantalla de victoria **no avanza sola**: `victory_autonext_s` = 0 (int; canon único de 02 §6, 06 §4.7 y 07 §5.4, incidencia 10) y el intersticial (AD_GATE) exige un toque explícito en "Siguiente"; cualquier valor > 0 solo adelantaría la pantalla de victoria y **nunca dispara AD_GATE sin toque** (el intersticial por temporizador queda prohibido por diseño).

### 5.4 Presupuesto de rendimiento (gama baja)

Dispositivo de referencia clase A [E a partir de research/08 §5.2]: Android 8-10, 3 GB RAM, Mali-G52 o Adreno 610, pantalla 20:9 (360×800 a 412×915 CSS). Objetivo 60 fps, mínimo 30 (dirección 20).

| Métrica | Presupuesto [P] | Cómo se mide |
|---|---|---|
| Frame | ≤16,6 ms (60 fps); CPU hilo principal ≤8 ms; GPU ≤10 ms | Unity Profiler por USB; `af_perf_sample` (1 de cada 20 sesiones) |
| Guardado por vertido | En el hilo principal, p95 < 5 ms y ninguno > 8 ms (D-012, decisión 5). **El vertido que gana el nivel, que escribe en síncrono, cuenta** (pregunta 37 de GDD.md §6.3, decidida el 2026-10-13: (b); D-020): en el teléfono, el 2026-10-13, 216 vertidos con un máximo de 4,83 ms y un p95 de 1,48 ms; si un día alguno pasa, se entrega la victoria al escritor en el toque y se confirma en síncrono al abrir la pantalla de victoria | `AF_PERF` en builds de desarrollo (`save_ms`, solo el hilo principal; `bg_ms`, el escritor; `sync`), ≥ 200 vertidos en 3 niveles en el teléfono |
| Draw calls en Game | ≤120 (SetPass ≤40) con 16 cuias, chorro, burbujas y HUD | Frame Debugger |
| Memoria | RSS total ≤400 MB; texturas residentes ≤120 MB; Mono heap ≤60 MB | Memory Profiler; `af_perf_sample.mem_mb` |
| Carga de nivel | <300 ms desde toque hasta primer fotograma de Llenar y teñir (N+1 precargado) | `af_level_load.ms` |
| Arranque frío | <6 s a primera interacción (requisito duro <20 s, capítulo 1 §8.1) | `af_boot.first_interaction_ms` |
| Tamaño de texturas | ASTC 6×6 para fondos y UI (≈1 MB por fondo 1080×2400), ASTC 4×4 para iconos y texto; sin mipmaps en UI; atlas 2048². Excepción: los kits de mundo de los fondos por parada (`bg_kit_w{nn}`) van en ASTC 8×8 (D-013; propuesta del Mapa del río, 10.7; sección 6.1) | Build report |
| Audio | Música Vorbis q0,4 en streaming (loop 2-3 min ≈1,5 MB); SFX Vorbis/ADPCM mono, "Decompress on load" | Build report |
| Batería | `targetFrameRate = 60` en juego; `OnDemandRendering` a 30 fps en Mapa del río y Mercado | Prueba de 60 min (sección 14) |
| Gráficos | Lista de APIs: OpenGLES3 primero, Vulkan después [P, validar en Mali-G52/Adreno 610]; menos variantes y menor binario | QA matriz |
| Build | IL2CPP, ARM64 solo, Managed Stripping **High** con `link.xml` para Firebase/IAP/MAX (research/06 §Implicaciones 1 proponía Medium [E]; High se prueba primero), Strip Engine Code, Incremental GC | CI |

## 6. Peso: AAB ≤80 MB con Addressables + Play Asset Delivery

### 6.1 Presupuesto de descarga inicial [P]

| Componente | Tamaño estimado | Nota |
|---|---|---|
| Runtime Unity + IL2CPP ARM64 + managed | ≈28-32 MB | APK vacío ≈32 MB (research/06 §1.2 [V]); stripping High recorta algo |
| SDKs nativos (MAX + 4 adaptadores, Firebase Analytics/Crashlytics/RC/Auth, Play Billing 9, Play Games, PAD, App Check) | ≈12-18 MB | A medir en la semana 1 del proyecto |
| Atlas UI + comodines + marcos + mascotas | ≈6 MB | ASTC 6×6 |
| Cuia base + 4 skins de 1.0 + 14 frutas/patrones | ≈5 MB | |
| Fondos mundos 1-4 (install-time) | ≈4-6 MB | 4 pinturas 1080×2400 ASTC 6×6 + variantes LUT (research/02 C3.4 [E]). **Con D-013, fondos por parada:** 4 kits y 10 héroes de los mundos 1-4 ocupan ≈ 1,7-2,5 MiB (kits en ASTC 8×8 y héroes en 6×6) [cálculo], porque el cielo pasa a hacerse por código; el margen paga el muelle y los dos juegos de follaje (≈ 1,5 MiB). Loreto suma ≈ 0,6-0,8 MiB, y los 4 kits de 201-700, ≈ 1,0-1,8 MiB; mientras Addressables no esté instalado, todo va en el AAB |
| Mapa del río (D-013) | ≈1,7 MiB | 5 texturas de terreno de 512², el atlas de UI del Mapa (1024², ASTC 4×4, con las siluetas de las muestras horneadas) y los dos JSON (sección 4.5). Install-time. Se mide con `ci/size-gate.ps1` tras integrar los 3 primeros sitios: impacto neto frente al canon, entre −1 y +3 MB [cálculo] |
| Audio (4 loops + ambientes + SFX) | ≈7 MB | |
| Niveles 1-700 (mundos 1-9) + Reto del río 2.190 (3 bandas × 730) + `tints.json` | ≈0,2 MB comprimidos (≈2,5 MB sin comprimir) | sección 4.1; los mundos 10-13 y los paquetes nuevos van por `levels_remote` |
| Fuentes (latino extendido, 2 familias) | ≈1,5 MB | TextMeshPro atlas. Con D-018, `FontSetup` calcula los glifos desde las tablas de textos (sección 4.7). Para la tanda 2 de idiomas: latino ampliado y cirílico (< 1-2 MB más, en el AAB; títulos rusos con Nunito ExtraBold) y un pack de PAD `fonts_cjk` fast-follow con Noto Sans JP y KR en atlas recortados (2-5 MB por idioma), cada fuente nueva con el visto bueno del dueño; cada pack nuevo entra en esta tabla y en la compuerta del CI |
| **Total base + install-time** | **≈65-76 MB** | Compuerta de CI: falla si `bundletool get-size total` > 80 MB |

### 6.2 Grupos de Addressables y asset packs

| Grupo Addressables | Contenido | Modo PAD | Tamaño [P] |
|---|---|---|---|
| `levels_install` | `mundo_01…09_v1.json` (niveles 1-700: cubre los ≥600 jugables de la dirección 17; el mundo 9 termina en el 700), `daily_pool_v1.json` (2.190 tableros = 3 bandas × 730), `tints.json` | install-time | ≈0,2 MB comprimidos |
| `core_install` | UI, cuias, frutas, mascotas, audio, fondos w01-w04 (con D-013, sus kits y héroes por parada), el Mapa del río y el fondo "río genérico" | install-time | ≈22 MB |
| `world_w05` … `world_w13` | Fondo + skin + variantes LUT + ambiente del mundo | **on-demand**, pedido al entrar en el último tercio del mundo anterior (p. ej. w05 se pide en el nivel 121). **En 1.0 viaja `world_w05`** (Loreto, 141-200). Con D-013 (2026-10-13; Q14-A), los niveles 201-700 (mundos 6-9) llevan el kit de su mundo (`bg_kit_w06` … `bg_kit_w09`) con el rótulo del sitio, sin héroe: en `world_w06` … `world_w09` cuando haya Addressables y, mientras no esté instalado, dentro del AAB (6.1). El fondo "río genérico" con la LUT de agua del mundo queda de respaldo mientras falte el kit o el pack (glosario §8; 03 §17; 06 §10; 07 §2.1); los héroes y las skins de esos mundos llegan con sus packs, de la 1.1 en adelante | 2-4 MB cada uno |
| `events_*` | Arte de Encuentro de las Aguas, Temporada de lluvias, Travesía (1.2) | on-demand | 2-3 MB |
| `levels_remote` | `mundo_10…13_v1.json` (701-1.040), vueltas siguientes, los paquetes de +100 niveles y las reposiciones; anunciados por `content_version` | Addressables remotos en Unity CCD (50 GB/mes gratis, research/06 C6.1 [V]) o Cloudflare R2 (10 GB gratis, egreso 0 [V]); el cliente verifica `pack_schema` y `generator.version` antes de activar | ≈10 KB comprimidos por paquete de 100 niveles |

Mecánica: Addressables for Android (`com.unity.addressables.android`) mapea grupos a asset packs del AAB; PAD es gratuito, alojado por Google Play, con límites de 4 GB base + install-time, 30 GB on-demand, 100 packs (research/02 Complemento 4.6 [V]); packs >200 MB solo por Wi-Fi (research/06 C6.1 [V]). Si el pack no está al llegar al mundo, se juega con el fondo "río genérico" (research/02 C3.3 [E]) y se reintenta; nunca se bloquea la partida. **Entrega de niveles (canon para 02 §14, 03 §17 y 06 §10)**: los paquetes de +100 niveles cada 1-2 semanas (dirección 17) se publican en `levels_remote` **sin build ni revisión de tienda** y se anuncian con `content_version`; el cliente descarga el paquete siguiente al entrar en el último tercio del paquete actual, lo valida (`pack_schema`, `generator.version`, hash) y solo entonces lo activa; si al llegar al nivel N+1 el paquete aún no está, muestra la pantalla "El río sigue creciendo" del capítulo 6 §9 con `live_content_end_message`. Una actualización de app solo **consolida** en `levels_install` lo ya publicado (el líder actualiza la app con cada lote, research/09 §Por qué ganó 5 [V]; aquí la tienda no marca la cadencia). `level_overrides` sigue sirviendo para sustituir niveles sueltos. La excepción son las mecánicas nuevas: un paquete remoto con un `pack_schema` que el cliente no conoce se rechaza, así que las de la 1.1 llegan con su build (sección 4.4; D-007).

## 7. Lista de SDKs

Nueve integraciones de terceros frente a los 30 rastreadores y 20 permisos de Magic Sort (research/09 §Técnico [V]); cada una tiene fila en Data Safety (sección 13.3). Sin SDK de atribución en 1.0 (se añade uno solo al activar UA pagada en US, fase 3; pregunta abierta 2). Sin Facebook SDK: Meta entra solo como adaptador de MAX. **Sin Firebase Messaging en 1.0** (Desviación 2): todos los push de 1.0 son locales, incluidos los que el capítulo 6 §8.2 clasifica como remotos; FCM entra en 1.2 con su fila en Data Safety.

Versiones fijadas el 2026-10-06 con setup/b §5 y su tabla final [V] (fuente primaria enlazada en el informe); cualquier cambio de versión entra por PR revisado por el dueño (`.claude/rules/sdk.md`).

| # | SDK | Versión fijada (2026-10-06) | Función | URL |
|---|---|---|---|---|
| 1 | AppLovin MAX Unity Plugin + adaptadores Google (AdMob), Unity Ads, Meta Audience Network, Mintegral | **8.6.6** (15-09-2026; SDK Android 13.6.4) (setup/b §5 [V]). Adaptadores según el Integration Manager, validados uno a uno en CI: un adaptador puede traer `.so` sin alinear a 16 KB (#521) o conflictos de dependencias (#593) [V]; Quality Service desactivable si rompe el build con AGP 9 (#611) | Mediación con bidding, rewarded e intersticial, ILR → `ad_impression`, flujo de consentimiento (dirección 13) | https://github.com/AppLovin/AppLovin-MAX-Unity-Plugin/releases/tag/release_8_6_6 |
| 2 | Google AdMob + UMP **dentro de MAX** | **Sin plugin Google Mobile Ads Unity** (decisión del 2026-10-06): AdMob entra como adaptador de MAX y UMP con la automatización de MAX ("Google UMP automation"), que muestra el mensaje en regiones del RGPD y pasa las cadenas TCF v2 a las redes; el mensaje se configura en AdMob › Privacy & messaging. Evita el conflicto de `namespace` del plugin GMA con AGP 9 (#4157) (setup/b §5 [V]; versión mínima de MAX para la automatización [NV]). Si algún día hiciera falta el plugin: 11.5.0 | Red AdMob dentro de MAX; consentimiento UMP para EEE/UK/CH; SSV de AdMob | https://support.applovin.com/en/max/unity/overview/terms-and-privacy-policy-flow |
| 3 | Unity In App Purchasing | **5.4.4** (02-10-2026; publicada para 6000.3) con Play Billing Library **9.0.0**; Play exige PBL 8 o superior desde el 31-08-2026 y PBL 9 vale hasta el 31-08-2028; requiere `com.unity.services.authentication` 3.7.1 o superior (setup/b §4 [V]) | Catálogo `af_*`, precios localizados, restauración | https://docs.unity3d.com/6000.3/Documentation/Manual/com.unity.purchasing.html |
| 4 | Firebase Unity SDK: Analytics, Crashlytics, Remote Config, Auth, App Check; Functions (cliente); Firestore (1.2); Messaging (1.2) | **13.17.0** (17-09-2026; BoM Android 34.19.0, trae EDM4U 1.2.189; 16 KB estable desde 12.10.0). Solo los módulos necesarios. Sus bibliotecas de escritorio son solo x64: no cargan en el Play Mode del editor ARM64, de ahí las implementaciones falsas (sección 2.1, principio 6) (setup/b §3 y §5 [V]) | Analítica, crashes, configuración remota, UID anónimo, integridad, llamadas a Cloud Functions | https://github.com/firebase/firebase-unity-sdk/releases/tag/v13.17.0 |
| 5 | Play Games plugin for Unity (PGS v2) | **2.3.0** (29-09-2026; Saved Games con soporte oficial; minSdk 24 o superior). Bug abierto: tras guardar sin conexión, Saved Games puede no volver a subir (#3379) (setup/b §5 [V]) | Inicio de sesión, Saved Games (3 MB/partida, 800 KB de portada [V]), logros | https://github.com/playgameservices/play-games-plugin-for-unity/releases/tag/v2.3.0 |
| 6 | Addressables for Android (Play Asset Delivery) + Play Integrity (vía App Check) | Addressables for Android **1.1.0** sobre Addressables **2.10.3**, ambas publicadas para 6000.3 (setup/b §5 [V]); Play Integrity: la que pida App Check [NV] | Entrega de mundos 5-13; integridad en tienda y reclamo de premios (10.000 peticiones/día gratis, research/06 C6.1 [V]) | https://docs.unity3d.com/6000.3/Documentation/Manual/com.unity.addressables.android.html |
| 7 | External Dependency Manager | EDM4U **1.2.189**, el que trae Firebase. Google lo deprecó en la 1.2.190 y archiva el repositorio el 26-10-2026; se pasa a `com.unity.external-dependency-manager` **2.1.0** cuando Firebase y MAX dejen de incluir EDM4U (setup/b §5 [V]) | Resolución de dependencias Gradle/CocoaPods | https://docs.unity3d.com/6000.3/Documentation/Manual/com.unity.external-dependency-manager.html |
| 8 | Unity Localization | 1.5.13 (research/06 §8 [V]) | String/Asset Tables, Smart Strings, CSV/Google Sheets | https://docs.unity3d.com/Packages/com.unity.localization@1.5/manual/index.html |
| 9 | Unity Gaming Services: Authentication + Leaderboards (+ Cloud Code) | Gratis "por tiempo limitado" (research/06 C6.2 [V]) · **v1.2** (Authentication ya entra en 1.0 como dependencia de IAP 5.4.x) | Liga del río por cubetas con reset por cron (research/06 C6.1 [V]) | https://docs.unity.com/en-us/leaderboards |
| — | Paquetes Unity de primera parte | Addressables 2.10.3, URP 2D, Shader Graph, Input System, TextMeshPro, Mobile Notifications, Test Framework; `com.unity.pipeline` 0.8.0-exp.1 (puente de la Unity CLI con el Editor, experimental; trae también un servidor de runtime, así que hay que comprobar que no entra en los builds de release [E]; setup/c §1.1 [V]) | — | Package Manager |
| — | CI | `game-ci/unity-test-runner@v4.4.0` y `game-ci/unity-builder@v6.0.0` con `cliVersion: v0.1.72`; imagen `unityci/editor:ubuntu-6000.3.25f1-android-3` (setup/c §5.1 [V]; setup/a §7 [V]) | Build y pruebas en GitHub Actions (sección 12.1) | https://game.ci/docs/github/builder |
| — | Opcional, apagado en 1.0 | GameAnalytics Unity (research/06 §8 [V]) | Segunda capa de funnels si Firebase no basta | https://docs.gameanalytics.com |

Consentimiento: UMP cubre solo EEE/Reino Unido/Suiza y estados de EE. UU. (research/08 C5.4 [V]); para BR/CO/PE/MX la pantalla es propia (`biz_legal_profile_by_country`, capítulo 1) con finalidades separadas, "rechazar" al mismo nivel que "aceptar" y registro de prueba; sin consentimiento, anuncios no personalizados (`npa`). La pantalla de edad neutral previa (sección 2.3) clasifica en **tres bandas {<13, 13-17, 18+}** (05 §7; GDD §6 #15): **<13** → bloqueo en 1.0 con la pantalla "Pide a un adulto que instale el juego" (sin consentimiento parental verificable): antes de bloquear y en cualquier arranque posterior `TagForChildDirectedTreatment = true` vía MAX y sus adaptadores (sin plugin GMA desde el 2026-10-06; el flag exacto según la versión de MAX [NV]), Analytics apagado o solo eventos sin ID ni `user_id`, sin Play Games sign-in, sin IAP ni Remote Config personalizado; **13-17** → `npa` + `maxAdContentRating = G`, PIN de compras activado, sin perfilado (`biz_under18_no_personalization`); **18+** → flujo normal. La banda declarada vive solo en el dispositivo (`consent.age_band`, sin ID) y se corrige una vez desde Ajustes (research/08 C5.1 y C5.4 [V]; bandas y bloqueo [P] de 05, con consulta al abogado antes de la Fase 5). MAX se inicializa solo después y recibe los flags de consentimiento y edad que su versión exponga.

## 8. Taxonomía de eventos de analítica

### 8.1 Convenciones

- Eventos recomendados de Firebase se usan con su nombre (`level_start`, `level_end`, `ad_impression`, `purchase`, `earn_virtual_currency`, `spend_virtual_currency`, `tutorial_begin`, `tutorial_complete`) para aprovechar los paneles; todo evento propio lleva prefijo `af_` (glosario §11). Los eventos del capítulo 4 §11 se leen con ese prefijo (`af_booster_used`, `af_stuck_shown`…).
- Parámetros comunes adjuntados por `AF.Analytics` a todo evento de juego: `level`, `world`, `difficulty`, `murky`, `attempt`, `seeds_balance`, `canoes`, `streak`, `geo_tier`, `no_ads`, `app_version`, `content_version`, `rc_variant`, `session_id`, `net ∈ {online, offline}`.
- Límites de Firebase Analytics a respetar: número de eventos distintos y parámetros por evento acotados (≈500 nombres y 25 parámetros por evento [NV, confirmar en la documentación vigente]); los parámetros comunes ocupan 15, así que cada evento añade ≤10 propios.
- Propiedades de usuario: `geo_tier`, `payer_class ∈ {none, light, mid, whale}`, `consent_profile`, `device_class ∈ {A, B, C}`, `install_phase` (`biz_market_phase` en la instalación), `first_world_reached` y, con D-018 (2026-10-13, Q11-A, aprobado), `device_language` (el idioma del teléfono) y `app_language` (el del juego), para decidir la tanda 3 de idiomas.

### 8.2 Tabla de eventos

| Evento | Parámetros específicos | Cuándo | KPI / capítulo que lo usa |
|---|---|---|---|
| `af_boot` | `cold_start_ms`, `first_interaction_ms`, `rc_source`, `migrated_from_schema` | Fin de la secuencia Boot | Primera interacción <20 s (cap. 1 §8.1) |
| `af_consent` | `profile`, `text_version`, `analytics`, `ads_personalized`, `age_band`, `step ∈ {shown, accepted, rejected, changed}` | Pantalla de consentimiento y Ajustes | Legal (sección 13 de este capítulo y 01 §9); tasa de rechazo por país y por banda de edad |
| `af_push_prompt` | `step ∈ {pre, native}`, `result` | Primera victoria de la 2.ª sesión con el nivel 10 superado (`push_preprompt_session` / `push_preprompt_min_level`; 06 §8.1) | Opt-in push (cap. 06 §8.1; benchmark Android 69 %, research/05 §5 [V]) |
| `af_push_sent` / `af_push_open` | `type` (tipo del catálogo de 06 §8.3) | Entrega de la notificación (1.0: notificación local con estado entregado, registrada en la siguiente apertura; 1.2: envío por FCM) / apertura de la app desde la notificación | CTR por tipo ≥1 % en Android (cap. 06 §8.2 y §12) |
| `af_asset_pack` | `pack`, `state ∈ {requested, downloading, completed, failed, fallback_used}`, `bytes`, `ms` | PAD | Fallos de descarga por país; uso del fondo genérico |
| `level_start` (Firebase) | `level_name` = `id` del nivel de §4.1 (`m{mm}_v{v}_n{nnnn}`; en el Reto del río, `d{b}_n{nnnn}`), `variant ∈ {base, plus_empty, minus_color}`, `band` | Al terminar Llenar y teñir | Embudo por nivel (cap. 3) |
| `af_level_win` | `pours`, `par`, `pours_over_par`, `undo_used`, `boosters_json`, `t_ms`, `reward_seeds`, `double_harvest (bool)`, `rescued (bool)` | Victoria | Intentos medios/medianos, tiempo de paso (cap. 3); ingreso por victoria (cap. 5) |
| `af_level_fail` | `kind ∈ {hard, soft}`, `canoe_lost`, `streak_broken`, `shield_used`, `relief_served`, `pours`, `t_ms` | Fallo (cap. 4 §5.2) | Curva real de fallos; disparo de "El río bajó" |
| `af_level_abandon` | `pours`, `t_ms`, `reason ∈ {map, app_background, crash_resume}` | Salida sin resolver que no es fallo. Desde el 31, «Salir» con el menú es fallo duro y se registra como `af_level_fail` (04 §5.2; D-008) | Time to abandon (research/02 §6 [V]) |
| `level_end` (Firebase) | `success`, `level_name` (mismo valor que en `level_start`: `id` de §4.1, `m{mm}_v{v}_n{nnnn}` o `d{b}_n{nnnn}`) | Victoria, fallo duro o salto | Paneles estándar |
| `af_anim_skipped` | `anim ∈ {tint, pour, rescue}`, `at_ms`, `duration_cfg_ms` | Toque durante animación | Tasa de salto ≥40 % → bajar duración (dirección 18) |
| `af_river_lowered` | `variant ∈ {plus_empty, minus_color}`, `fails_level` | Al servir `relief` | Eficacia del alivio (victoria en el intento siguiente) |
| `af_dda_applied` | `reason ∈ {churn_risk}`, `variant ∈ {pool_low_D}` (`level` va en los parámetros comunes) | Al cargar un slot de comodidad sustituido por la DDA personal (03 §14; solo con `lvl_dda_enabled` = true, 1.2) | Intervenciones por ciclo de 20 (máx. 2) y retención de los intervenidos (cap. 03 §14) |
| `af_mechanic_intro` | `mech` (los valores de `intro_mechanic`, sección 4.1: `chica`, `alta`, `hoja`, `matapi`, `liana`, `murky`, `remolino`, `barro`, `lluvia`, `brote`, `pirana`; D-007; y `tinaja`, D-019), `completed (bool)`, `attempts` | Nivel con `intro_mechanic` | Comprensión de cada estreno (cap. 2 núcleo); con los intentos medios por mecánica en su ciclo de debut (03 §15; D-009) |
| `af_label_shown` | `label ∈ {hard, very_hard}`, `source ∈ {pack, override}` (`lvl_label_override`) | Etiqueta Difícil o Muy difícil mostrada al cargar el nivel (desde el 40 y el 60) | Calibración de etiquetas: intentos medios y abandono por etiqueta (cap. 03 §15; dirección 5) |
| `af_booster_used` / `af_booster_denied` / `af_hint_result` / `af_shuffle_result` | Según capítulo 4 §11 | Uso de comodines | Capítulo 4 |
| `af_stuck_shown` / `af_stuck_choice` / `af_stuck_outcome` / `af_rescue_unsolvable` / `af_skip_used` | Según capítulo 4 §11. Con D-008, `af_stuck_choice` gana los valores `give_up`, `confirm_play_on` y `quit` [P] | Modal Aguas estancadas y ¿Seguro? (D-008) | Conversión del rescate por rama y geo, también en la segunda oferta; "rescate → victoria" >85 % |
| `af_canoes_depleted` / `af_no_canoes_choice` / `af_canoes_wait_return` / `af_infinite_canoes_start` / `_end` | Según capítulo 4 §11 | Canoas | Retorno tras esperar (<60 % → revisar 25 min) |
| `af_streak_milestone` | `milestone ∈ {3, 6, 10}`, `reward_type`, `reward_value` | Hito de Crecida | Peso de la racha en el ingreso (objetivo ≈27 %, research/04 C1 [E]) |
| `af_streak_shield_used` | `streak_at_use` | Fallo duro con Hoja escudo | Uso diario del escudo |
| `earn_virtual_currency` (Firebase) | `virtual_currency_name = seeds`, `source ∈ {win, hard, very_hard, double_harvest, streak, calendar, basket, daily_challenge, piggy, iap, refund, event}`, `value` | Toda fuente | Fuentes reales (cap. 5) |
| `spend_virtual_currency` (Firebase) | `virtual_currency_name = seeds`, `item_name ∈ {undo_refill, extra_cuia, hint, shuffle, reveal, skip, continue_cuia, canoes_refill, skin_*}`, `value` | Todo sumidero | Sumideros reales; ratio de presión realizado (cap. 4 §11) |
| `af_wallet_snapshot` | `seeds`, `stock_json`, `max_level`, `days_since_install` | Primera sesión de cada día | Saldo medio al cerrar tramos 50/100/200 y "bloqueados sin recursos" (research/04 C1 §5 [E]) |
| `af_basket_open` | `level`, `contents_json` | Canasta cada 10 niveles | Valor entregado (cap. 5) |
| `af_calendar_claim` | `day`, `reward_type`, `reward_value`, `days_missed_before` | Bitácora del río | Reclamo ≥70 % de DAU (cap. 1 §3.1) |
| `af_piggy` | `state ∈ {unlocked, shown_full, bought}`, `seeds_inside` | Cántaro | Conversión de alcancía (cap. 05) |
| `af_daily_challenge` | `date`, `result ∈ {win, fail, abandon}`, `t_ms`, `murky_bonus (bool)`, `streak_days` | Reto del río | Participación diaria (cap. 06) |
| `af_river_fact_shown` | `fact_id`, `dismissed_ms` | Dato del río | Lectura del contenido educativo. Con D-013, el `fact_id` del dato de llegada es su clave de contenido (`map.stop.<lugar>.dato_NN`, `map.country.<iso2>.dato_NN`, `map.myth.<tema>`; glosario §10); el de la línea de fruta (Frutas del río, D-017) es `<tinte>.<kk>` (`acai.03`; sección 4.6); `dismissed_ms` es el tiempo hasta el toque en «Siguiente» (lectura probable ≥ 4.000 ms en una línea de ≈ 70 caracteres [P]) |
| `af_fact_page_open` | `tint_id`, `unlocked` | Apertura de la ficha de una fruta, desde la línea de la victoria o desde Ajustes › «Sobre la Amazonía» (D-017, Q4-A, aprobado el 2026-10-13) | Aperturas de la ficha de Frutas del río |
| `af_map_open` [P] | `source ∈ {l10, world_change, lobby, pause, victory, content_end}`, `stop`, `world` | Apertura del Mapa del río (D-013) | Uso del Mapa; A/B de `ui_map_mode` (D7 y D30 de la cohorte `stops` frente a `worlds`) |
| `af_map_close` [P] | `dwell_ms`, `stops_tapped`, `view_changes`, `north_up` | Cierre del Mapa del río (D-013) | Interés por el Mapa |
| `af_stop_arrive` [P] | `stop_id`, `world`, `level`, `view ∈ {victory, map}`, `skipped` | Llegada a una parada del Mapa del río, en la victoria del `10k` o al ponerse al día en el Mapa (D-013) | Llegadas vistas o saltadas |
| `af_fruit_discovered` | `tint_id` | Primer nivel en que aparece un tinte (tarjeta "Fruta nueva") | Fruta nueva y Cuaderno de campo (cap. 06 §4.5-§4.6) |
| `af_skin_unlock` | `skin_id`, `source ∈ {world_milestone, iap, event, pass}` | Desbloqueo de una skin de cuia | Skins por hito del Mapa del río (cap. 06 §4.5); compras `af_skin_*` (cap. 05) |
| `af_comeback_gift` | `days_away` | Entrega del Regalo de regreso (`live_comeback_gift`, ≥7 días sin sesión) | Reactivación por segmento y D7 de los reactivados (cap. 06 §9 y §12) |
| `af_event_join` / `af_event_milestone` / `af_event_claim` (1.2) | `event_id`, `n` (hito o premio) | Inscripción, hito alcanzado y premio reclamado en un evento | Participación, finalización y premio reclamado (cap. 06 §5 y §12) |
| `af_league_rank` (1.2) | `week`, `bucket_rank`, `tier` | Cierre semanal de la Liga del río | % de elegibles con ≥50 puntos y distribución de tiers (cap. 06 §6 y §12) |
| `af_world_enter` | `world`, `days_since_install`, `pack_ready (bool)` | Primer nivel de cada mundo | Velocidad de viaje; PAD a tiempo |
| `af_ad_request` | `placement`, `format ∈ {rewarded, interstitial}`, `result ∈ {filled, no_fill, error}`, `ms`, `network` | Carga de anuncio | Fill rate por país (cap. 05) |
| `af_ad_shown` | `placement`, `format`, `gap_since_last_s`, `levels_since_last`, `session_count`, `day_count` | Impresión mostrada | Cumplimiento de cadencia (dirección 13) |
| `ad_impression` (Firebase, desde ILR de MAX) | `ad_platform`, `ad_source`, `ad_format`, `ad_unit_name`, `value`, `currency` | Cada impresión | ARPDAU de anuncios por país (cap. 05 §9; research/06 §3 [V]) |
| `af_ad_reward` | `placement`, `granted ∈ {provisional, confirmed, revoked}`, `latency_s` | Concesión y SSV | "Rewarded que no paga" = 0 (research/09 §Anuncios [V]) |
| `af_iap_offer_shown` | `sku`, `context ∈ {stuck, no_canoes, shop, starter_window, noads_prompt, piggy}`, `price_local`, `currency` | Oferta visible | Conversión por contexto (cap. 05) |
| `af_iap_result` | `sku`, `result ∈ {ok, cancel, pending, failed, verify_failed, deferred_ack}`, `context`, `ms` | Fin del flujo de compra | Embudo de compra; fallos de verificación |
| `purchase` (Firebase) | `transaction_id`, `value`, `currency`, `items`, `context` | Compra verificada | Ingreso IAP por SKU y contexto |
| `af_entitlement_restored` | `type ∈ {no_ads, ledger_consumables, pass}`, `count` | Restauración | Quejas "pagué y no tengo" = 0 |
| `af_shop_view` | `tab`, `from` | Mercado | Tráfico de tienda |
| `af_settings_change` | `key ∈ {patterns, sound, music, haptics, language, consent, sloth_messages}` (`sloth_messages`, el ajuste «Mensajes de TiTino», D-016), `value` | Ajustes | Uso de Patrones de fruta (dirección 3) |
| `af_account_delete` | `step ∈ {requested, confirmed, done, failed}`, `source ∈ {app, web}` | Ajustes › Eliminar cuenta y datos (sección 11) | Cumplimiento de la política de eliminación de cuentas de Play; sin parámetros personales |
| `af_save_sync` | `result ∈ {ok, conflict_local_won, conflict_cloud_won, merged, failed}`, `bytes`, `ms` | Sincronización | Conflictos por usuario; tamaño del blob |
| `af_level_load` | `ms`, `from_cache (bool)`, `pack_id` | Carga de nivel | <300 ms (sección 5.4) |
| `af_perf_sample` | `fps_avg`, `fps_p5`, `frame_ms_p95`, `mem_mb`, `device_class`, `gpu`, `thermal ∈ {none, light, moderate, severe}` | 1 de cada 20 sesiones, tras 3 niveles | 60 fps en clase A; crash/ANR (sección 14) |
| `af_error` | `module`, `code`, `fatal (bool)` | Excepción controlada | Crashlytics no fatales |

Esta tabla es la **taxonomía única** de analítica: los demás capítulos usan exactamente estos nombres y parámetros (`af_stuck_shown` / `af_stuck_choice` / `af_stuck_outcome`, `af_daily_challenge`, `af_calendar_claim`, `af_basket_open`, `af_river_fact_shown`, `af_push_prompt`, `af_streak_shield_used`, `level_end {success, level_name}`…) y quedan dados de alta aquí `af_dda_applied` y `af_label_shown` (03 §14-§15) y los eventos de meta de 06 §12. Un evento nuevo (logros en 1.1, Aldeas en 2.0) se da de alta en esta tabla antes de usarse.

KPIs derivados que alimenta esta tabla: D1/D7/D30 por cohorte (Firebase), intentos medianos por nivel y abandono por nivel (cap. 3), bloqueos y conversión del rescate (cap. 4), saldo por tramo y ratio de presión realizado (cap. 5), eCPM e ILR por país, conversión por contexto y primera compra (cap. 05), participación en Reto del río, Bitácora y Crecida (cap. 06), sesiones/DAU y duración mediana (cap. 1 §3.1).

## 9. Parámetros Remote Config consolidados

Fuente única: `Assets/_Project/Data/Config/RemoteConfigDefaults.json` (defaults compilados) y la plantilla de Firebase. Límites: 3.000 parámetros, 2.000 condiciones, 24 experimentos simultáneos, 300 versiones de plantilla, 1 M de caracteres en valores, fetch mínimo 12 h (research/06 §3 y research/02 Complemento 4.2 [V]).

**Esta sección es la única tabla de Remote Config del GDD.** Los capítulos 00-07 y 09 describen el comportamiento y remiten aquí para la clave, el tipo, el default compilado y el rango; ninguno repite claves ni valores (las tablas "Parámetros configurables" de esos capítulos pasan a ser una referencia a 08 §9). Un parámetro, un nombre, un default: `RemoteConfigDefaults.json` se genera a partir de esta tabla y `tools/economy-sim` y las pruebas PlayMode la leen tal cual.

**Regla de nombres.** Prefijo por familia: `biz_` compuertas, kill switches y registro de negocio (01, 09); `rules_` variantes de las reglas del núcleo (02; nace el 2026-10-06 con `rules_pour_mode`, D-001; `futile_rule_enabled` conserva su nombre); `anim_` tiempos y densidad de animación (02, 07); `ui_` y `audio_` ajustes de presentación (00, 07); `lvl_` niveles y dificultad (03); `ads_` intersticiales y recompensados (05); `eco_` fuentes y sumideros de semillas, ofertas y packs (05); `live_` meta y live-ops (06); `rc_`, `save_`, `sec_`, `perf_` técnicas de este capítulo; `consent_` consentimiento y edad (08 y 05). Las familias que ya eran únicas conservan su nombre sin prefijo: comodines y rescate (`undo_*`, `extra_cuia_*`, `hint_*`, `shuffle_*`, `reveal_*`, `skip_*`, `restart_*`, `rescue_*`, `stuck_*`, `af_rescue_*`, `canoes_*`), `streak_*`, `relief_*`, `mech_*`, `push_*`, `league_*` / `regatta_*`, y las claves citadas con nombre propio por la revisión cruzada (`level_overrides`, `daily_overrides`, `content_version`, `victory_autonext_s`, `hint_budget_nodes`). Las claves de versiones anteriores que esta tabla absorbe se listan al final como **alias retirados**: no existen en `RemoteConfigDefaults.json` y deben borrarse del capítulo que aún las nombre. "Dueño" es el capítulo que justifica el valor; el rango es el admisible sin nueva revisión de diseño.

| Clave | Tipo | Default | Rango | Dueño | Uso |
|---|---|---|---|---|---|
| `rc_schema` | int | 1 | — | 08 | Versión del esquema de claves; el cliente ignora esquemas mayores |
| `consent_text_version` | int | 1 | — | 08 | Fuerza re-consentimiento al cambiar textos legales |
| `consent_under13_policy` | string | "block" | block · parental_consent | 05 | Banda <13 de la pantalla de edad neutral (05 §7; sección 2.3): `block` = bloqueo en 1.0 con "Pide a un adulto que instale el juego", TFCD, Analytics sin ID y sin Play Games ni IAP; `parental_consent` queda reservado (consentimiento parental verificable, sin implementar en 1.0) |
| `consent_age_signal_source` | string | "self_declared" | self_declared · self_declared+play_age_signals | 05 | Fuente de la banda de edad: declaración en la pantalla neutral; la segunda opción añade Play Age Signals API (compuerta de la Fase 5, 09 §2.3; 05 §7) |
| `biz_market_phase` | string | `closed_beta` | `closed_beta` · `soft_launch_latam` · `soft_launch_br` · `global` · `ios` | 01 | Fase de mercado; habilita funciones por fase y segmenta cohortes (absorbe `launch_phase` de 09) |
| `biz_country_enabled` | map país→bool | CO, PE, MX = true; resto false | — | 01 | Compuerta por país además de la distribución de Play |
| `biz_min_app_version` | string | "1.0.0" | — | 08 | Por debajo, pantalla "Actualiza Tintalú" (compras y liga bloqueadas) |
| `biz_maintenance_mode` / `biz_maintenance_message` | bool / string | false / "" | — | 09 | Bloquea anuncios e IAP durante incidentes; el juego offline sigue |
| `biz_iap_enabled` / `biz_ads_enabled` / `biz_interstitials_enabled` / `biz_rewarded_enabled` | bool | true | — | 01 | Kill switches: tienda, todos los anuncios (los recompensados pasan a cobrar semillas), solo intersticiales (**único** interruptor de intersticiales), solo recompensados |
| `biz_feature_<sistema>` (`daily_challenge`, `login_calendar`, `win_streak`, `lives`, `rescue`, `murky`, `piggy`, `shop`) | bool | true | — | 09 | Kill switch por sistema (R2, R13 del capítulo 9) |
| `biz_legal_profile_by_country` | map país→string | BR `br_lgpd_eca` · CO `co_1581` · PE `pe_29733` · MX `mx_lfpdppp` · EEE/UK/CH `ump` · resto `default` | — | 01 | Pantalla de consentimiento y tratamiento de edad (sección 7) |
| `biz_under18_no_personalization` | bool | true | — | 01 | Bandas de edad {<13, 13-17, 18+} de la pantalla neutral (05 §7; sección 2.3): 13-17 → `npa`, `maxAdContentRating = G` y PIN de compras; <13 → bloqueo en 1.0 con TFCD, Analytics sin ID, sin Play Games ni IAP (el bloqueo no depende de esta clave sino de `consent_under13_policy`) |
| `biz_disclaimer_in_store_copy` / `biz_store_title_variant` / `biz_store_icon_variant` | bool / string / string | true / "A" / "A" | — | 01 | Solo registro: correlación con la ficha activa (absorben `store_title_variant` del glosario y `store_icon_variant` de 07) |
| `biz_ua_paid_enabled_by_country` / `biz_ua_test_active` | map país→bool / bool | todos false / false | — | 01 / 09 | UA pagada en US solo con D7 ≥15 % y ARPDAU ≥0,08 (dirección 19); marca de cohortes de prueba |
| `biz_experiment_tag` | string | "" | — | 09 | Cohorte de experimento activa; viaja en analítica como `rc_variant`; nunca sobre Cuia de rescate ni Canoas |
| `biz_kpi_targets` / `biz_kpi_alert_thresholds` | json | 01 §KPIs / 09 tabla 6.1 | — | 01 / 09 | Solo paneles; no cambian el juego |
| `biz_ga_enabled` | bool | false | — | 08 | Segunda capa GameAnalytics |
| `tap_switch_selection` / `drag_pour_enabled` | bool | true / true | — | 02 | Controles C5 y C10 |
| `queue_max_lag_pours` | int | 2 | 1-3 | 02 | Cola visual de toques por cuia |
| `futile_rule_enabled` | bool | true | — | 02 | Excluir vertidos fútiles en la detección de Aguas estancadas |
| `rules_pour_mode` | string | "run" | run · single | 02 | Cantidad por vertido (D-005, decisión del dueño del 2026-10-08 que sustituye a D-001; 02 §1.2, R9; `PourMode { Run, Single }` en `AF.Core`): `run` = la racha visible que cabe, con vertido parcial (por defecto, como Magic Sort); `single` = una dosis por vertido, solo pruebas. Se aplica al cargar el nivel, nunca a mitad (el historial de Marea y el par dependen del modo); un valor desconocido se trata como `run` con un aviso en el log. Los pares de los packs se calculan en `run` (03 §5) |
| `stuck_modal_delay_ms` | int | 400 | 200-800 | 02 | Espera antes del modal de bloqueo |
| `restart_confirm_min_pours` | int | 8 | 5-12 | 04 | Confirmación de Volver a la orilla y umbral de fallo blando |
| `mech_enabled.{hoja,matapi,liana,remolino,barro,lluvia,brote,pirana,tinaja}` | bool | true | — | 02 | Kill switch por mecánica (D-007 añade `matapi`; D-019, `tinaja`). En la 1.0, con false, los niveles que la usan se sustituyen con `level_overrides`, y el formato de respaldo por slot se fija con el build de la 1.1 (02 §9.2, regla 3; pregunta 31 de GDD.md §6.3, decidida el 2026-10-13: (b); D-020). Hoy el cliente no lee esta clave (tarea de código) |
| `mech_debut_level.{chica,alta,hoja,matapi,liana,remolino,barro,lluvia,brote,pirana,tinaja}` | int | chica 14, alta 27, hoja 43, matapi 63, liana 83, remolino 103, barro 123, lluvia 143, brote 163, pirana 183, tinaja 323 | — | 02 | Copia exacta de la lista única de 02 §9.1 (incidencia 1; contrato 1 con D-007 y D-019): 14 Cuia chica · 27 Cuia alta · 43 Hoja · 63 Matapi · 83 Liana con fruta · 100 Aguas turbias · 103 Remolino · 123 Barro · 143 Lluvia · 163 Brote · 183 Piraña · 323 Tinaja (M13, D-019: en la 1.0 si cumple su condición; si no, el mismo nivel en la 1.1); la Liana de cuenta (con número) es una variante, en el 243. La Cuia alta de 6 sale solo en hitos desde el 141 (02 §8); el estreno de Aguas turbias lo fija `lvl_murky_from_level`; antes de D-007, el estreno del 63 era el del Brote. Informativo para el cliente; el pool ya viene ordenado. Hoy ni el cliente ni los packs la leen: los packs marcan cada estreno con `intro_mechanic` (sección 4.1) y el generador toma los debuts del calendario versionado (`tools/level-generator/specs/campaign_v1.json`) |
| `lluvia_k_pours` / `pirana_k_pours` | int | 3 / 4 | 2-5 | 02 | Ritmo por defecto de Lluvia y Piraña si el JSON no lo fija |
| `anim_tint_duration_ms_early` / `anim_tint_duration_ms_late` | int | 1400 / 800 | 1200-1500 / 600-1000 | 02 | Llenar y teñir (dirección 18; sección 5.3); si la tasa de salto en 1-20 supera `anim_tint_skip_alarm_pct`, bajar a 1200 (nunca por debajo) |
| `anim_tint_fast_from_level` / `anim_tint_skippable` | int / bool | 21 / true | 11-30 / nunca false en producción | 02 | Cambio a la versión corta; saltable al primer toque |
| `anim_tint_skip_alarm_pct` | float | 0.40 | — | 07 | Umbral de alarma de `af_anim_skipped` |
| `anim_pour_dose_ms` / `anim_pour_tail_ms` | int | 280 / 80 | 80-300 / 50-120 | 02 | Caudal por dosis y cola (sección 5.2; D-006: ≈ 285 ms por dosis en Magic Sort) |
| `anim_lift_ms` / `anim_travel_ms` / `anim_return_ms` | int | 100 / 300 / 300 | — | 02 | Duraciones base del vertido (02 §4.1; D-006) |
| `anim_speed_factor_early` / `anim_speed_factor_late` / `anim_fast_from_level` | float / float / int | 1.0 / 0.8 / 21 | 0.7-1.2 / 0.6-1.0 / 11-30 | 02 | Velocidad global de animación |
| `anim_celebration_ms` | int | 900 | 600-1200 | 02 | Celebración de victoria |
| `anim_river_lowered_banner_ms` | int | 2000 | 1500-2500 | 04 | Banner "El río bajó" al servir `relief[0]` (dirección 7) |
| `anim_confetti_max_particles` | int | 60 | 20-120 | 07 | Densidad en gama baja |
| `anim_reduce_motion_factor` | float | 0.5 | 0.3-0.7 | 02 | Multiplicador con "reducir movimiento" |
| `victory_autonext_s` | int | 0 | 0 (cualquier valor > 0 adelanta la pantalla pero **nunca dispara AD_GATE sin toque**) | 02 | Auto-avance de la pantalla de victoria apagado (incidencia 10; mismo valor en 02 §6, 06 §4.7 y 07 §5.4): el intersticial exige un toque explícito en "Siguiente". Con D-011, tras ver entero el vídeo de la Cosecha doble se avanza solo por AD_GATE, sin intersticial porque lo bloquea la regla de 60 s tras un recompensado (05 §6.1) |
| `ui_patterns_default_from_level` | int | 41 | 1-80 | 00 | Toggle Patrones activado por defecto (dirección 3). Mientras no exista la pantalla de Ajustes, el cliente usa el default como constante [P] (D-012) |
| `ui_pattern_opacity` | float | 0.20 | 0.15-0.35 | 07 | `_PatternOpacity` del shader (sección 5.2) con Patrones de fruta activado; sin él (hasta el nivel 40 por defecto), el patrón va sutil al 15 % (07 §3, regla 1; D-012) |
| `ui_show_difficulty_subtitles` / `ui_river_fact_every_n_levels` / `ui_murky_bonus_enabled` / `ui_mascot_rescue_anim` | bool / int / bool / bool | true / 10 / false (1.1: true) / true | — | 00 | "Aguas bravas" / "Noche de jaguar"; cadencia de la llegada del Mapa del río con su dato, cada 10 niveles, la cadencia de paradas de D-013 (pregunta 45 de GDD.md §6.3, decidida el 2026-10-14: (a); D-022); tarjeta Aguas turbias · Bonus, contenido de 1.1 (02 T7, 06 §4.1); Capi en la Cuia de rescate |
| `ui_fruit_line_every_n_levels` | int | 1 | 0 (apagada) · 1 · 2 (una cada dos niveles; D-017, Q2-A) | 06 | Cadencia de la línea de fruta de Frutas del río (D-017), con clave propia por la pregunta 45, (a) (D-022); default [P]. Con 1, en cada victoria; con N > 1, solo si el nivel `n` es múltiplo de N; con 0, nunca (sección 4.6). Una sola línea de texto por victoria, con prioridad: frase de TiTino en sus tres hitos > dato de llegada > línea de fruta (02 §6); la línea desplazada queda en la ficha. Hasta que exista Remote Config en el cliente, constante de `GameConfig` [P] |
| `ui_very_hard_night_lut` | bool | true | — | 07 | Variante nocturna del fondo en Muy difícil |
| `ui_tinaja_show_contents` | bool | false | — | 02 | La Tinaja enseña qué frutas guarda, sin el orden (D-019, Q-Ti5-A: apagada; se enciende si el soft launch muestra quejas de suerte). Al solver no le cambia nada |
| `ui_sloth_enabled` / `ui_sloth_fail_enabled` | bool / bool | true / true | — | 07 | TiTino (D-016): interruptor general, para apagarlo en remoto, y su aparición en Nivel fallido [P] |
| `ui_sloth_win_line_every_n` / `ui_sloth_lines_per_session_max` | int / int | 4 / 4 | — | 07 | TiTino (D-016): frase en 1 de cada N victorias (salvo sus tres hitos, en los que habla siempre) y tope de frases por sesión [P]. Una sola línea de texto por victoria (T6-A) |
| `ui_map_mode` | string | `stops` | `stops` · `worlds` | 06 | Mapa del río de D-013: `stops`, 100 paradas (una cada 10 niveles); `worlds`, solo las grandes paradas (la primera de cada mundo), sin boyas intermedias: una por mundo, las 13 del canon anterior (Q1-C de la propuesta del Mapa). Es un filtro de la misma pantalla y del mismo catálogo, no una segunda implementación; interruptor y A/B [P] |
| `ui_map_autopen` | string | `l10_world` | `l10` · `l10_world` | 06 | Cuándo sale solo el Mapa (D-013, Q4-D): `l10_world`, en el 10 y con el viaje de mundo dentro del banner de los cambios de mundo; `l10`, solo en el 10 (el cambio de mundo, como cualquier llegada, en la victoria) [P] |
| `ui_bg_per_stop` | bool | true | — | 07 | Fondo por parada (D-013, Q5-A); false = el kit del mundo con el héroe de su gran parada, fijo en todo el mundo. Es un cambio de búsqueda en el catálogo, sin arte ni código aparte [P] |
| `ui_stop_label` | bool | true | — | 07 | Rótulo del lugar en el primer nivel de cada parada (D-013; reglas de lectura de D-011) [P] |
| `ui_victory_view` | bool | true | — | 02 | Victoria con vista (D-013, Q6-A): el panel y las cuias se desvanecen en 250 ms y se ve el sitio [P] |
| `anim_map_world_ms` | int | 6000 | 3000-8000 | 06 | Tope del viaje de mundo en la pantalla del Mapa del río (D-013): al acabarse el contenido y al abrirlo con «Ver mapa». Dentro del banner de mundo manda el tope de ≤ 4 s de Q4-D, no esta clave. Las demás duraciones del Mapa son constantes [P]: tramo de 1,2 s, fundido de 250 ms y bandera de 0,8 s [P] |
| `ui_map_quiz` | string | `off` | `off` · `victory` · `map` | 06 | «¿Verdad o mito?» del Mapa del río: experimento de la 1.1, apagado en la 1.0 (D-013, Q12-A); en la 1.0, el dato simple [P] |
| `ui_max_takeovers_per_session` | int | 1 | 1 | 05 | Un solo pop-up fuera de hitos por sesión |
| `ui_haptics_default_on` / `ui_reduce_motion_default` | bool | true / false | — | 02 / 07 | Ajustes iniciales |
| `audio_music_default_on` / `audio_sfx_default_on` | bool | true / true | — | 07 | Ajustes iniciales |
| `audio_music_percussion_layers` / `audio_ambient_rain_scale` | int / float | 2 / 1.0 | 0-3 / 0-2 | 07 | Capas que entran al sellar cuias; multiplicador de lluvia ambiental |
| `level_overrides` | json `{slot: level_id}` | `{}` | — | 03 | Sustituir o reordenar niveles sin build (research/02 §7.3 [E]); también por segmento (DDA) |
| `content_version` | string | "1.0.0" | — | 03 | Catálogo de paquetes publicado en `levels_remote` (sección 6.2); si difiere del local, el cliente descarga y muestra "nuevos niveles" |
| `daily_overrides` | json `{fecha: level_id}` | `{}` | — | 03 | Sustituciones fechadas del Reto del río (research/02 Complemento 4.2 [V]) |
| `lvl_daily_bracket_levels` | json | `[41, 201]` | — | 03 | Umbrales de las 3 bandas del Reto del río |
| `relief_after_fails` / `relief_mode` / `relief_second_step_fails` | int / enum / int | 3 / `auto` / 0 (off) | 2-4 / `auto` · `plus_empty` · `minus_color` / 0, 5-8 | 03 | Alivio determinista "El río bajó" (dirección 7; 03 §13); `relief_mode` ordena `relief[]` en el generador |
| `lvl_labels_from_level` / `lvl_very_hard_from_level` | int | 40 / 60 | — | 03 | Primer nivel que puede mostrar Difícil / Muy difícil |
| `lvl_label_thresholds` | json | `{"hard":0.75,"very_hard":0.90}` | — | 03 | Solo lectura/registro; las etiquetas vienen en el paquete (dirección 5) |
| `lvl_label_override` | json `{nivel: label}` | `{}` | — | 03 | Reetiquetado por intentos medios en vivo (dirección 5) |
| `lvl_murky_from_level` / `lvl_murky_max_cuias` / `lvl_murky_max_hidden_units` | int | 100 / 3 / 4 | 90-120 / 2-3 / 2-4 | 03 | Aguas turbias (dirección 6; incidencia 8): máximo 4 dosis ocultas → ≤24 realidades que enumera `AF.Solver`; sin Pista parcial; validación en cliente, no generación. No rige la Tinaja (D-019): el núcleo usa para ella un tope propio de 4 ocultas (`LevelValidation.TinajaMaxHiddenUnits`) |
| `lvl_dda_enabled` / `lvl_dda_risk_window` / `lvl_dda_risk_fails` / `lvl_dda_max_per_cycle` | bool / int / int / int | **false** / 5 / 4 / 2 | — | 03 | Dificultad adaptativa (03 §14): **apagada en 1.0** (incidencia 13; 03/06/09); se enciende por Remote Config tras el soft launch |
| `lvl_kpi_alarm_attempts` | json | `{"normal":2.5,"hard":4,"very_hard":6}` | — | 03 | Umbrales de alarma de intentos medios (03 §15; capítulo 1 §8.1) |
| `lvl_endless_gen_budget_ms` / `lvl_endless_fallback_pool` | int / bool | 100 / true | — | 03 | Río sin fin (1.2) |
| `undo_free_per_level` / `undo_unlimited_until_level` / `undo_refill_amount` / `undo_refill_seed_price` | int | 3 / 10 / 3 / 30 | 3-5 / 0-20 / 3-5 / 20-50 | 04 | Marea (dirección 9): ilimitada en 1-10 (excepción de tutorial) y 3 por nivel desde el 11 (02 §13, 04 §2.1) |
| `extra_cuia_unlock_level` / `extra_cuia_seed_price` / `extra_cuia_max_per_level` | int | 8 / 150 / 1 | 6-10 / 100-200 / 1-2 | 04 | Cuia extra (dirección 9): 1 regalada en el 8 (excepción de tutorial, 02 §13) |
| `hint_unlock_level` / `hint_tutorial_units` / `hint_seed_price` | int | 12 / 3 / 100 | 10-15 / 3 / 60-120 | 04 | Morpho (dirección 9): debut en el 12 con **3 Morphos regalados** (excepción de tutorial; 02 §13, 04 §2.3, 06 §2.2) |
| `hint_idle_from_level` / `hint_idle_delay_s` | int / int | 3 / 12 | 1-5 / 8-20 (0 = apagada) | 04 | Pista gratuita por inactividad en **todos los niveles desde el 3, sin tope superior** (02 §3.4, 04 §2.3; incidencia 9): tras 12 s sin toques pulsa un origen de la ruta del solver; no muestra el destino ni cobra; el interruptor de Ajustes equivale a `hint_idle_delay_s` = 0 en el dispositivo |
| `hint_budget_nodes` / `hint_weight` / `hint_timeout_ms` | int / float / int | 40000 / 2.0 / 150 | 20000-80000 / — / 100-300 | 03 | Solver en cliente (Morpho y pista por inactividad; por realidad en Aguas turbias); al agotarse no se cobra; sin Pista parcial |
| `shuffle_unlock_level` / `shuffle_seed_price` / `shuffle_max_per_level` / `shuffle_max_per_day` | int | 30 / 300 / 1 / 3 | 25-40 / 200-400 / 1-2 / 2-5 | 04 | Pororoca (dirección 9) |
| `shuffle_par_slack` / `shuffle_budget_nodes` / `shuffle_reveals_murky` | int / int / bool | 2 / 40000 / true | 1-3 / — / — | 03 | Par nuevo ≤ par + slack; la ola revela las capas ocultas |
| `reveal_seed_price` | int | 120 | 100-150 | 04 | Luciérnaga (dirección 9) |
| `skip_seed_price` / `skip_min_fails` / `skip_min_gap_levels` / `skip_min_level` | int | 300 / 5 / 10 / 31 | 250-400 / 4-6 / 8-15 / 20-31 | 04 | Salto del Boto (dirección 9) |
| `rescue_min_level` | int | 31 | 25-40 | 04 | Debut de Canoas, Cuia de rescate y Salto del Boto (direcciones 10-11); antes, fallar solo reinicia gratis. Desde aquí, el embudo de D-008: el pie del modal es «Rendirse» y salir del nivel con el menú cuesta una canoa (04 §5.2 y §6) |
| `rescue_price_bands` | json | `[[31,600]]` | 600 fijo (un solo tramo; sin 750/900 ni cohortes geográficas) | 05 | Precio de la Cuia de rescate: **600 semillas fijas desde el nivel 31** (dirección 10; incidencia 4). Clave única: no existe `rescue_base_price`; el ajuste LatAm va en el precio local de los IAP (Play Console), nunca en semillas |
| `rescue_first_day_mult` | float | 0.5 | 0.5 | 05 | Primer rescate del día a **300** semillas (dirección 10) |
| `rescue_max_per_level` / `rescue_check_solvable` | int / bool | 1 / true | 1 / — | 04 | Máximo **1 rescate por nivel**: 14 + Cuia extra + Cuia de rescate ≤ 16 ranuras (02 §7); verificación "la compra cumple" (02, 04 y 05 ya en 1). La oferta repetida en ¿Seguro? es el mismo rescate: si se compra ahí, cuenta para este tope (D-008) |
| `stuck_order_t1` / `stuck_order_t3` / `stuck_show_shield_status` | string / string / bool | "undo,rescue,balsa,pack" / "undo_ad,rescue,pack,balsa" / true | — | 04 | Orden de opciones del modal por geo; línea de Hoja escudo |
| `af_rescue_daily_cap` / `af_rescue_hide_after_declines` | int | 1 / 2 | 1 / 2-3 | 04 | Balsa de rescate: tope diario (dirección 14) y ocultación 48 h |
| `canoes_max` / `canoes_regen_min` | int | 5 / 25 | 5 / 20-30 | 04 | Canoas (dirección 11) |
| `canoes_refill_seeds_enabled` / `canoes_refill_seed_price` | bool / int | true / 200 | — / 150-300 | 04 | Llenar canoas con semillas |
| `streak_shield_per_day` | int | 1 | 1-2 | 04 | Hoja escudo (dirección 12) |
| `streak_milestones` | json | `{"3":300,"6":"chest","10":"extra_cuia+rio_crecido"}` | hito 3: 200-300; 6 y 10 fijos | 05 | Escalera canónica de la Crecida (05 §2.4; dirección 12; incidencia 5): 3 → 300 semillas · 6 → Canasta · 10 → 1 Cuia extra + río crecido (sin semillas). Al pagar el hito 10 la barra vuelve a 0 y el río crecido sigue activo hasta el siguiente fallo duro no escudado, también con la barra a 0 (05 §2.4; así lo simula `tools/econ_sim.py`). **Clave única** en 05, 06 y 08: `eco_streak_rewards` no existe |
| `streak_daily_cap` / `streak_hits_once_per_day` / `streak_unlock_level` | int / bool / int | 600 / true / 25 | 300-900 / — / 20-40 | 05 | Tope diario 600: suma **solo** el hito 3 (300) y las 50 semillas de la Canasta del hito 6; el bonus del río crecido y los comodines de los hitos 6 y 10 no cuentan; **cada hito se paga una vez por día local** (05 §2.4); debut de la Crecida |
| `streak_reminder_hours` / `streak_restart_counts_as_fail` | int / bool | 36 / false | 24-48 / — | 06 | Push local "Crecida te espera" (la Crecida no caduca); Volver a la orilla voluntario no rompe la racha |
| `ads_first_interstitial_level` | int | 22 | ≥22 | 05 | Dirección 13: ninguno antes del 22; solo tras victoria, nunca en reintento |
| `ads_interstitial_every_n_levels` | json | `[[22,3],[61,2]]` | n ≥2 | 05 | Cadencia: cada 3 niveles en 22-60, cada 2 desde el 61 (05 §6.1); el nivel del segundo par es el único "paso a cada 2" (sin clave aparte) |
| `ads_interstitial_min_gap_s` / `ads_interstitial_session_warmup_s` | int | 150 / 120 | ≥150 / ≥60 | 05 | Separación mínima (dirección 13) y calentamiento de sesión |
| `ads_interstitial_session_cap` / `ads_interstitial_daily_cap` | int | 6 / 12 | ≤6 / ≤14 | 05 | Topes por sesión (dirección 13) y por día [P] |
| `ads_interstitial_suppress_after_iap_h` | int | 24 | 12-48 | 05 | Sin intersticiales tras cualquier compra (research/04 §Reglas [E]), también tras `af_infinite_canoes_1h`. Es lo único que los corta durante unas Canoas infinitas, que por sí mismas no los quitan (D-008) |
| `ads_rv_double_harvest_from_level` | int | 11 | 10-15 | 05 | Primer recompensado voluntario: Cosecha doble (dirección 13) |
| `ads_rv_cap_<placement>` | int | double_harvest 5 · undo_refill 4 · hint 3 · reveal 3 · extra_canoe 3 · daily_chest 1 (ubicación apagada en 1.0: `eco_calendar_double_ad_enabled`) | 1-5 | 05 | Topes diarios por ubicación (dirección 13); sustituye a los `*_ad_cap_day` por comodín de 04 |
| `ads_rv_cooldown_s_<placement>` | int | 60 (extra_canoe 90) | 60-120 | 05 | Cooldown por ubicación (dirección 13) |
| `ads_rv_global_cap_day` | int | 15 | 10-20 | 04 | Tope global de recompensados |
| `ads_rv_max_wait_fill_ms` | int | 1500 | 500-3000 | 08 | Espera máxima de carga antes de ocultar el botón de video |
| `ads_ilr_to_firebase` | bool | true | — | 08 | Enviar `ad_impression` con valor desde MAX |
| `eco_reward_normal` / `eco_reward_hard` / `eco_reward_very_hard` | int | 20 / 60 / 80 | 15-30 / 45-90 / 60-120 | 05 | Ingreso por victoria (dirección 12) |
| `eco_calendar_table_v1` | json | tabla 05 §2.3 (≈3.000 semillas + 12 entradas de comodín + 2 Canastas + 1 Canasta grande) | suma 2.500-3.500 | 05 | Bitácora del río (dirección 12); reemplazable sin build |
| `eco_calendar_locked_booster_seeds` / `eco_calendar_double_ad_enabled` | json / bool | `{"undo3":30,"other":100}` / **false** (1.1: true) | — | 06 | Conversión de comodines bloqueados; ubicación `rv_daily_chest` (doblar el premio de la Bitácora con video) **apagada en 1.0** (incidencia 13; 05 §6.2 y 09 §3.1 la sitúan en 1.1: cinco ubicaciones de recompensados en 1.0) |
| `eco_basket_cycle` | json | `["undo3","hint","extra_cuia","undo3","hint"]` | — | 06 | Canasta cada 10 niveles: ciclo **determinista y visible** antes de abrir (dirección 15; IARC "ítems aleatorios: no"); sin probabilidades |
| `eco_basket_seeds` | int | 50 | 30-50 | 05 | Semillas de cada Canasta (también la primera) |
| `eco_piggy_from_level` / `eco_piggy_fill` / `eco_piggy_cap` / `eco_piggy_min_buy` | int / json / int / int | 20 / `[15,30,40]` / 3000 / 1000 | 15-30 / — / 2000-4000 / — | 05 | Cántaro (dirección 14; determinista, tope visible) |
| `eco_starter_window_levels` / `eco_starter_timer_h` / `eco_starter_first_show` | json / int / string | `[10,20]` / 48 / "L12_or_session2" | — | 05 | Mochila de inicio (dirección 14) |
| `eco_noads_bonus_seeds` / `eco_noads_card_trigger` | int / string | 500 / "3_interstitials_or_d2" | 300-600 / — | 05 | Bono y disparador de la tarjeta Sin anuncios (dirección 14) |
| `eco_seed_pack_amounts` | json | `{"af_coins_2":1000,"af_coins_3":2800,"af_coins_4":6000,"af_coins_5":13000,"af_coins_6":36000}` | — | 05 | Glosario §2.1; espejo de `iap_catalog.json`; SKU de Canoas infinitas 1 h = `af_infinite_canoes_1h` |
| `eco_price_tier_<country>` / `eco_model_version` | string | tabla 05 §4.2 / "B-2026-10-06e" | — | 05 | Solo registro: los precios viven en Play Console; `eco_model_version` identifica el modelo de la simulación de 05 (escenario B de `tools/econ_sim.py`, corrida del 2026-10-06; e = `on_fail` con la regla del contrato 19: un fallo duro con la barra a 0 apaga el río crecido) para correlacionarlo con `tools/economy-sim` |
| `live_unlock_level_daily` | int | 10 | 8-12 | 06 | Desbloquea Reto del río, Bitácora, Mapa del río, Canasta y Mercado |
| `live_unlock_level_league` / `_trail` / `_regatta` / `_meeting` / `_village` | int | 41 / 41 / 51 / 61 / 31 | — | 06 | Desbloqueos de 1.2 y 2.0 |
| `live_daily_challenge_reward` | json | `{"seeds":60,"murky_bonus_mult":2}` | seeds 40-80 | 06 | Premio del Reto del río; el bonus Aguas turbias paga el doble |
| `live_daily_offset` / `live_daily_streak7_reward` | int / string | 0 / "shuffle" | — | 06 | Desplazamiento del índice por fecha; premio por 7 Retos seguidos |
| `live_events_schedule` / `live_event_active` | json | `[]` / `{}` | — | 08 / 06 | Calendario de eventos 1.2 (`start/end` UTC, `type`, `pack`) y definición del evento activo |
| `live_comeback_gift` / `live_comeback_offer_enabled` | json / bool | `{"days":7,"seeds":200,"extra_cuia":1,"refill_canoes":true}` / false (1.1: true) | — | 06 | Regalo de regreso y Mochila de regreso |
| `live_content_end_message` | json | `{"next_batch_eta_days":14}` | 7-21 | 06 | Pantalla "El río sigue creciendo" y push local "Nuevos niveles" (sección 6.2) |
| `live_world_bg_variant_override` | string | "" | lluvia · noche · alba | 07 | Fuerza una variante de LUT (eventos) |
| `push_preprompt_session` / `push_preprompt_min_level` / `push_quiet_hours` / `push_daily_cap_d0_d7` / `push_weekly_cap_lapsed` | int / int / string / int / int | 2 / 10 / "22-09" / 1 / 3 | 2-3 / 8-12 / — / — / — | 06 | Pre-prompt de push en la **primera victoria de la 2.ª sesión** con el nivel 10 ya superado (06 §8.1; incidencia 15; sustituye a `push_preprompt_level`), horas silenciosas y topes de notificaciones (todas locales en 1.0) |
| `league_bucket_size` / `regatta_bucket_size` / `league_points` / `league_prizes` / `regatta_prizes` / `league_anticheat_max_wins_per_hour` | int / int / json / json / json / int | 50 / 20 / `{"normal":10,"hard":20,"very_hard":30,"daily":20,"trail":10}` / tablas 06 §6 y §5.2 / 40 | — | 06 | Liga del río y Regata (1.2) |
| `save_cloud_interval_levels` / `save_conflict_policy` | int / string | 3 / "max_level_then_ts" | 1-5 / — | 08 | Subidas a Saved Games; sección 11 |
| `sec_clock_max_drift_h` / `sec_ssv_grace_h` / `sec_appcheck_enforce` / `sec_purchase_deferred_ack_h` | int / int / bool / int | 24 / 24 / true / 72 | 12-48 / 12-48 / — / 48-72 | 08 | Anti-reloj (única clave global; absorbe `canoes_clock_max_drift_h`), ventana SSV, App Check, acknowledge local [P] |
| `perf_sample_rate` / `perf_target_fps_menus` | float / int | 0.05 / 30 | 0-0.2 / 30-60 | 08 | Fracción de sesiones con `af_perf_sample`; `OnDemandRendering` en Mapa del río y Mercado |

**Alias retirados** (no existen en `RemoteConfigDefaults.json`; el capítulo que aún los nombre debe sustituirlos por la clave de la tabla): `tint_anim_ms_*`, `tint_anim_fast_from_level`, `tint_anim_skippable` (02) y `tint_anim_duration_*`, `tint_anim_switch_level`, `tint_anim_skip_enabled`, `anim_skip_alarm_pct`, `pour_dose_duration` (07) → `anim_tint_*` / `anim_pour_*`; `pour_dose_ms`, `pour_tail_ms`, `lift_ms`, `travel_ms`, `return_ms`, `celebration_ms` (02) → `anim_*`; `anim_pour_duration_ms` (08 anterior) → `anim_pour_dose_ms` + `anim_pour_tail_ms`; `victory_autoadvance_s`, `victory_autoadvance_with_offer` (07) → `victory_autonext_s`; `idle_nudge_s`, `idle_nudge_from_level` (02) y `hint_idle_enabled`, `hint_idle_until_level` (08 anterior: pista por inactividad solo en 1-10) → `hint_idle_from_level` + `hint_idle_delay_s`; `victory_autonext_s` en forma JSON `{"with_offer","no_offer"}` (08 anterior) → `victory_autonext_s` int = 0; `rescue_price_bands` con tramos `[101,750],[201,900]` (08 anterior) → `[[31,600]]`; `push_preprompt_level` (08 anterior, 06 anterior) → `push_preprompt_session` + `push_preprompt_min_level`; `eco_streak_rewards` (05/06 anteriores) → `streak_milestones`; `level_label_override`, `murky_from_level`, `murky_max_cuias`, `murky_max_hidden_doses` (02), `murky_max_hidden_units`, `label_thresholds`, `labels_from_level`, `very_hard_from_level`, `dda_*`, `daily_bracket_levels`, `kpi_alarm_attempts`, `endless_*`, `shuffle_max_par_delta` (03), `daily_bands` (06) → `lvl_*` / `shuffle_par_slack`; `content_pack_latest` (09) → `content_version`; `relief_fails_threshold` (04), `skip_after_fails` (02, 03, 05), `extra_cuia_tutorial_level` (02), `rv_hint_from_level` (05) → `relief_after_fails`, `skip_min_fails`, `extra_cuia_unlock_level`, `hint_unlock_level`; `rescue_price`, `rescue_base_price`, `rescue_first_of_day_mult` (02 anterior, 04), `rescue_from_level`, `lives_max`, `lives_regen_min`, `lives_from_level`, `rescue_iap_daily_cap`, `rescue_order_t1/t3`, `extra_cuia_price`, `hint_price`, `reveal_price`, `shuffle_price`, `skip_price`, `undo_recharge_price`, `shuffle_per_level`, `shuffle_per_day` (05), `unlock_level_lives`, `unlock_level_streak` (06) → `rescue_*`, `canoes_*`, `af_rescue_daily_cap`, `stuck_order_*`, `*_seed_price`, `shuffle_max_per_*`, `rescue_min_level`, `streak_unlock_level`; `undo_refill_ad_cap_day`, `hint_ad_cap_day`, `reveal_ad_cap_day`, `canoes_extra_ad_cap_day` y sus `_cooldown_s`, `rv_global_cap_day` (04), `rv_cap_*`, `rv_cooldown_s_*`, `rv_global_daily_cap` (05) → `ads_rv_*`; `first_interstitial_level`, `interstitial_*` (05), `interstitials_enabled`, `rewarded_enabled` (09), `ads_interstitial_late_from_level` (08 anterior) → `ads_*` / `biz_interstitials_enabled` / `biz_rewarded_enabled`; `reward_*`, `chest_seeds`, `chest_first_seeds`, `calendar_table_v1`, `noads_*`, `starter_*`, `piggy_*`, `offer_takeovers_per_session`, `price_tier_*`, `econ_model_version` (05), `chest_cycle`, `logbook_*`, `max_takeovers_per_session`, `starter_first_show` (06), `eco_basket_table`, `eco_noads_bonus_seeds` = 300 (08 anterior) → `eco_*` / `ui_max_takeovers_per_session`; `streak_hit3_seeds`, `streak_hit10_seeds` (08 anterior), `streak_daily_cap_seeds`, `streak_shields_per_day` (06) → `streak_milestones`, `streak_daily_cap`, `streak_shield_per_day`; `unlock_level_*`, `daily_offset`, `daily_reward_seeds`, `daily_murky_bonus_multiplier`, `daily_streak7_reward`, `event_active`, `comeback_*`, `content_end_message` (06), `daily_challenge_seeds` (05), `live_daily_challenge_reward` = 50 (08 anterior), `world_bg_variant_override` (07) → `live_*`; `haptics_default_on`, `reduce_motion_factor` (02), `reduce_motion_default`, `pattern_opacity_default`, `very_hard_night_lut`, `music_percussion_layers`, `ambient_rain_probability_scale`, `store_icon_variant` (07), `store_title_variant` (00) → `ui_*` / `audio_*` / `anim_*` / `biz_*`; `min_supported_version`, `maintenance_mode`, `maintenance_message`, `feature_*`, `launch_phase`, `experiment_tag`, `ua_test_active`, `kpi_alert_thresholds` (09) → `biz_*`; `canoes_clock_max_drift_h` (04) → `sec_clock_max_drift_h`; `hint_solver_budget_nodes`, `diff_attempts_alarm_mult` (08 anterior) → `hint_budget_nodes`, `lvl_kpi_alarm_attempts`; SKU `af_infinite_canoes_60` (04, 08 anterior) → `af_infinite_canoes_1h`.

## 10. Seguridad

### 10.1 Modelo de amenazas y respuesta

| Amenaza | Respuesta | Evidencia |
|---|---|---|
| Recompensa de anuncio sin ver el anuncio (cliente parcheado o replay) | Concesión **provisional** en cliente + confirmación en servidor: callback S2S de MAX para rewarded (URL del Cloud Function `/v1/ssv` configurada en el panel de MAX, con `user_id = UID` y `custom_data = placement\|nonce`) [NV detalle de MAX]; para la red AdMob directa, **SSV de AdMob** con firma ECDSA cuyas claves públicas rotan y no deben cachearse >24 h (research/06 C6.1 [V]). Si en `sec_ssv_grace_h` (24) no llega la confirmación, se revoca (stock no baja de 0) y se marca `fraud_flag` (capítulo 4 §10.2) | research/06 C6.1 [V] |
| Compra falsa o recibo reutilizado | Cloud Function `verifyPurchase`: valida el `purchaseToken` con `purchases.products:get` de la Google Play Developer API, hace `acknowledge` en servidor, escribe el token en el libro mayor `users/{uid}/purchases/{token}` y solo entonces el cliente acredita; `setObfuscatedAccountId` = UID para que Play detecte fraude; nada se concede en estado `PENDING`; el job programado `voidedPurchasesJob` (Cloud Scheduler, diario) consulta la Voided Purchases API, marca `voided` en el libro mayor y el cliente retira semillas (sin bajar de 0) o derechos en el siguiente `restoreLedger` (research/06 C6.1 [V]). Si el servidor no responde en 72 h el cliente hace `acknowledge` local para evitar el reembolso automático y marca `deferred_ack` [P] | research/06 C6.1 [V]; cuota Play Developer API 3.000 consultas/min [V] |
| Llamadas a Functions desde fuera de la app | Firebase **App Check** con Play Integrity en Android (10.000 peticiones/día gratis) y DeviceCheck/App Attest en iOS, exigido (`enforce`) en `verifyPurchase`, `restoreLedger`, `deleteAccount`, reclamo de premios y, en 1.2, escritura de liga | research/06 C6.1 [V] |
| Manipulación del reloj (Canoas, Bitácora, Reto del río, topes diarios) | `Clock.ServerOffset` estimado con la cabecera `Date` del último fetch de Remote Config o respuesta de Functions; si `\|device − server\| > sec_clock_max_drift_h` (24): las Canoas no regeneran, el premio diario queda "pendiente de validar" y los topes diarios usan la fecha del servidor; sin red se acepta la fecha local y se reconcilia al reconectar | research/06 C6.1 [E]; research/02 Complemento 4.2 [E]; capítulo 4 §7.1 |
| Edición del perfil local (semillas infinitas) | HMAC con clave derivada del dispositivo + `economy_ts`; en conflicto con la nube gana el blob coherente; `af_wallet_snapshot` detecta saltos imposibles (>5.000 semillas/día sin compra → `payer_class = suspect`, sin castigo automático) [P]. Aceptado: en 1.0 la economía de semillas es **autoritativa en cliente**; lo que vale dinero real (compras, recompensas por anuncio) sí se verifica en servidor | research/06 C6.1 regla 2 [V] |
| Ingeniería inversa del binario | IL2CPP + Strip Engine Code; ninguna clave secreta en el cliente (la SDK key de MAX y la configuración de Firebase son públicas por diseño; lo que protege es App Check) | research/06 §1 [V] |
| Liga del río (1.2) | Puntuación escrita desde Cloud Code (autoritativa), nunca desde el cliente; `UpdateType = keepBest` | research/06 C6.1 [V] |

### 10.2 Flujo SSV (rewarded)

```
Cliente:  nonce = uuid(); MAX.SetUserId(uid); MAX.ShowRewarded(placement, customData=placement+"|"+nonce)
          OnRewardReceived → Economy.GrantProvisional(reward, nonce); Save.Local(); Analytics(af_ad_reward granted=provisional)
Servidor: POST /v1/ssv (callback de MAX o SSV de AdMob)
          verificar firma/secreto; rechazar nonce repetido (doc rewards/{nonce} ya existe)
          escribir rewards/{nonce} = {uid, placement, ts, network}
Cliente:  en el siguiente CloudSync: confirmar nonces pendientes (<24 h) → granted=confirmed;
          nonce sin confirmar tras 24 h → Economy.Revoke(nonce) (no baja de 0); fraud_flag++
```

### 10.3 Verificación de compras

```
Cliente:  Unity IAP → OnPurchaseComplete(product) → estado "verifying"; UI muestra "Confirmando…" (≤3 s)
          Functions.Call("verifyPurchase", {token, sku, obfuscatedAccountId=uid})  # App Check obligatorio
Servidor: purchases.products.get(packageName, sku, token)
          if purchaseState != PURCHASED (PENDING) → {status:"pending"} (no conceder)
          if ya en users/{uid}/purchases/{token} → {status:"duplicate"}
          acknowledge (y consume si consumible) → escribir libro mayor {sku, ts, price, currency, country}
          actualizar users/{uid}/entitlements (no_ads, pass_until, skins)
          → {status:"ok", grant:{seeds, items}}
Cliente:  ok → Wallet.Apply; applied_purchases += token; Save.Local+Cloud
          pending → Unity IAP mantiene la transacción; reintento en cada arranque
          error de red → cola local con backoff; a las 72 h acknowledge local + deferred_ack [P]
```

## 11. Guardado local y en la nube

| Aspecto | Especificación |
|---|---|
| Local | `profile.json` en `Application.persistentDataPath`, escritura atómica (temporal + `Flush(true)` + `File.Replace`), copia `profile.bak` del estado anterior; `PlayerPrefs` solo para flags de arranque (research/06 §3 [E]). Guardado tras cada vertido (D-008; 02 R8), al terminar cada nivel, en cada mutación de cartera, al pasar a segundo plano y al cerrar la app. **Guardado por vertido** (T-D; D-012, decisión 5, con el visto bueno del dueño del 2026-10-11 para tocar `AF.Save`): el hilo principal serializa el perfil (< 1 ms) y entrega los bytes a un único escritor en segundo plano con cola de 1, en la que gana el último, que hace la misma escritura atómica; al terminar el nivel, en cada mutación de cartera, al pasar a segundo plano y al cerrar la app se espera al escritor (tope de 1.000 ms [P]) y se escribe en síncrono; si el escritor falla, el perfil sigue en memoria y se reintenta en el siguiente guardado. Un corte a mitad de escritura deja el perfil principal o el `.bak`, nunca uno corrupto. Objetivo en la sección 5.4 |
| Identidad | Firebase Auth anónimo desde el primer arranque (UID estable; 50 k MAU gratis, research/06 C6.1 [V]); vínculo con Play Games mediante `LinkWithCredentialAsync(PlayGamesAuthProvider)` al iniciar sesión en PGS; en iOS, `GameCenterAuthProvider`/Apple (research/06 C6.3 [V]) |
| Nube Android 1.0 | Play Games Services v2 **Saved Games**, snapshot único `af_main`, sin imagen de portada (límite 800 KB [V]); subida cada `save_cloud_interval_levels` (3) niveles, tras cada compra y al pasar a segundo plano; descarga al arrancar con sesión |
| Conflicto | Resolución **manual** (no `UseLongestPlaytime`): gana el blob con mayor `progress.max_level`; a igual nivel, el `economy_ts` más reciente; consumibles **nunca se suman**; `daily` se fusiona con `max` por clave; `applied_purchases` es la unión (research/06 §3 [V]; capítulo 4 §10.2). Tras resolver, el cliente llama a `restoreLedger` (callable con App Check) y vuelve a acreditar cualquier token del libro mayor ausente de `applied_purchases` (y retira los marcados `voided`) |
| Eliminación de cuenta (política de Play; LGPD art. 18 y Ley 1581) | Dos vías con el mismo resultado: **en la app**, Ajustes › "Eliminar cuenta y datos" con confirmación en dos pasos y aviso de que las compras no se reembolsan; **en la web**, URL pública `https://<dominio del estudio>/tintalu/eliminar-cuenta` que pide el ID de jugador visible en Ajustes y ejecuta el mismo flujo. Secuencia: `deleteAccount` (callable + App Check) borra `users/{uid}` y `rewards/*` del UID, conserva el libro mayor de compras **anonimizado** (tokens y montos sin UID) el plazo fiscal, y elimina el usuario de Firebase Auth; el cliente borra el snapshot `af_main` de Saved Games, `profile.json`, `profile.bak` y `PlayerPrefs`, y vuelve al estado de primera ejecución. Analytics recibe `af_account_delete` sin identificadores; no se conserva ningún dato de analítica vinculado al UID más allá de la retención ya configurada en Firebase |
| Nube 1.2 y iOS | Documento Firestore `users/{uid}` (<100 KB, escrituras agrupadas cada 3-5 niveles) como fuente de verdad multiplataforma de derechos, liga y Pase Expedición, sin migración porque el UID existe desde 1.0; `PersistenceEnabled` del SDK de Unity para caché offline, a probar en dispositivo (research/06 C6.3 [V/E]). Sin iCloud Key-Value Store |
| Offline | Todo lo que da progreso se calcula en cliente y se reconcilia después ("última escritura gana" por campo, `max` para contadores); lo que da valor económico se concede provisional o en servidor (research/06 C6.1 reglas 1-2 [V]) |
| Cambio de dispositivo a mitad de nivel | `level_state` viaja en el blob; si el nivel ya no existe en el pool local se recarga por número con la misma semilla (capítulo 4 §10.2) |

```
ResolveConflict(local, cloud):
  if local.progress.max_level != cloud.progress.max_level:
      winner = argmax(max_level); loser = other
  else:
      winner = argmax(economy_ts); loser = other
  merged = winner.clone()
  merged.daily = max_by_key(local.daily, cloud.daily)        # topes de video y rescate no se duplican
  merged.applied_purchases = union(local.applied_purchases, cloud.applied_purchases)
  merged.album = union(...); merged.entitlements = or(...)
  merged.progress.map_seen_level = max(local.progress.map_seen_level, cloud.progress.map_seen_level)   # D-013
  Analytics(af_save_sync result = winner==local ? conflict_local_won : conflict_cloud_won)
  return merged
```

## 12. CI/CD y plan para iOS

### 12.1 Android: GitHub Actions + game-ci

Actualizado el 2026-10-06 con el plan (s30-s32) y setup/a §9, setup/b §7 y setup/c §5 [V]. Los workflows viven en `.github/workflows/` (GitHub no lee otra carpeta): `ci.yml` con el job `core` en cada PR y `unity.yml` con los tests de Unity y el AAB. Los mantiene el subagente `build-release`; `ci/` guarda solo los scripts de las compuertas.

| Etapa | Qué hace | Herramienta | Tiempo [E] |
|---|---|---|---|
| 0 · Núcleo (cada PR) | `dotnet build -warnaserror` y `dotnet test` sobre `dotnet/AmazonFill.slnx`; `python tools/validate_schema.py`; sin Unity ni licencia | `actions/setup-dotnet` con .NET 10 | 2-3 min (setup/c §5.4 [E]) |
| 1 · Pruebas | EditMode (reglas, solver, migraciones; los tests compartidos del paquete se repiten aquí cuando cambia `Packages/com.amazonfill.core`) y PlayMode (flujo de bloqueo, cadencia de anuncios simulada); solo en PR listos (no borradores) y en `main` | `game-ci/unity-test-runner@v4.4.0`, `cliVersion: v0.1.72` | 5-8 min |
| 2 · QA de niveles | `tools/level-qa` sobre todos los paquetes modificados; `tools/economy-sim` si cambió `RemoteConfigDefaults.json` | .NET 10 / Python 3.14 | 2-10 min |
| 3 · Build | AAB ARM64, IL2CPP, `BUILD_NUMBER` incremental, en la imagen `unityci/editor:ubuntu-6000.3.25f1-android-3`; la versión del Editor sale de `ProjectVersion.txt`. **Licencia Personal por `UNITY_EMAIL` + `UNITY_PASSWORD`** de una cuenta Unity dedicada al CI y sin 2FA (estrategia `personal` de game-ci/cli desde v0.1.63); **no hay `UNITY_LICENSE` ni `.ulf`**: Unity eliminó la activación manual para Personal y un `.ulf` no funciona en runners efímeros (setup/a §9 [V]; setup/c §5.2 [V]). Los jobs que activan Unity van en serie (`concurrency: unity-personal-seat`) para no pedir dos asientos a la vez. Keystore de subida en Base64 (`ANDROID_KEYSTORE_BASE64`, `ANDROID_KEYSTORE_PASS`, `ANDROID_KEYALIAS_NAME`, `ANDROID_KEYALIAS_PASS`) | `game-ci/unity-builder@v6.0.0`, `cliVersion: v0.1.72` (Docker) | 15-30 min (research/06 §4 [V]) |
| 4 · Compuertas | `bundletool get-size total` ≤80 MB (`ci/size-gate.ps1`); alineación de 16 KB con `zipalign -c -P 16 -v 4` sobre los APK que genera `bundletool` y `check_elf_alignment.sh` sobre las `.so` (`ci/check-16kb.sh`); `targetSdk == 36` (fijo, nunca "Automatic (highest installed)", porque el módulo instala la plataforma 37; la compuerta sube a **37 antes del 31-08-2027**, 09 §2.3 y R11) y `minSdk == 26`; `com.android.billingclient` ≥ 8 en el árbol de Gradle; el `applicationId` de prueba `com.amazonfill.dev` (antes `com.estudio.aquazonia`) no puede llegar a una build de release (setup/b §Recomendación 3 y 6 [V/E]; plan, §Reglas de uso) | scripts en `ci/` | 1 min |
| 5 · Publicación | **Bloqueada hasta el dictamen de marca** (`docs/legal/marca.md`): ningún AAB con el `applicationId` definitivo `com.<estudio>.tintalu` a Play Console, tampoco a la pista interna; los builds de prueba usan `com.amazonfill.dev`, que la compuerta 4 no deja llegar a un build de release. Después: pista **interna** automática en cada merge a `main`; **cerrada** y **abierta/producción** por despliegue manual con aprobación del dueño | `r0adkll/upload-google-play` o fastlane supply (research/06 §4 [E]) | 2 min |
| 6 · Backend | `firebase deploy --only functions,firestore:rules` en cambios de `functions/` | Firebase CLI | 3 min |

Cupo: 2.000 min Linux gratis/mes en repositorios privados del plan Free y 10 GiB de almacenamiento y de ancho de banda de Git LFS, en el que cuentan las descargas desde Actions; ≈40-60 pipelines con build al mes (setup/c §5.4 [V/E]; 09 R28). Ahorro: CI de Unity solo en PR listos y en `main`, filtros de rutas, `cancel-in-progress`, cachés de `Library` y `.git/lfs`, y pruebas locales obligatorias antes del PR. Alternativa documentada: Unity Build Automation (research/06 §4 [V]; 09 R22).

```yaml
# .github/workflows/unity.yml (extracto ilustrativo; el job core vive en ci.yml)
on:
  pull_request: { types: [opened, synchronize, ready_for_review],
                  paths: ['Assets/**', 'Packages/**', 'ProjectSettings/**', '.github/workflows/**'] }
  push: { branches: [main] }
jobs:
  unity-tests:
    if: github.event.pull_request.draft != true
    runs-on: ubuntu-latest
    concurrency: { group: unity-personal-seat, cancel-in-progress: false }   # un asiento Personal a la vez
    steps:
      - uses: actions/checkout@v4
        with: { lfs: true }
      - uses: game-ci/unity-test-runner@v4.4.0
        env: { UNITY_EMAIL: ${{ secrets.UNITY_EMAIL }}, UNITY_PASSWORD: ${{ secrets.UNITY_PASSWORD }} }
        with: { testMode: all, cliVersion: v0.1.72 }           # sin unityVersion: se lee de ProjectVersion.txt
  build-android:
    needs: unity-tests
    if: github.ref == 'refs/heads/main'
    runs-on: ubuntu-latest
    concurrency: { group: unity-personal-seat, cancel-in-progress: false }
    steps:
      - uses: actions/checkout@v4
        with: { lfs: true }
      - uses: game-ci/unity-builder@v6.0.0
        env: { UNITY_EMAIL: ${{ secrets.UNITY_EMAIL }}, UNITY_PASSWORD: ${{ secrets.UNITY_PASSWORD }} }
        with: { targetPlatform: Android, cliVersion: v0.1.72, androidExportType: androidAppBundle,
                androidKeystoreName: user.keystore, androidKeystoreBase64: ${{ secrets.ANDROID_KEYSTORE_BASE64 }},
                androidKeystorePass: ${{ secrets.ANDROID_KEYSTORE_PASS }}, androidKeyaliasName: ${{ secrets.ANDROID_KEYALIAS_NAME }},
                androidKeyaliasPass: ${{ secrets.ANDROID_KEYALIAS_PASS }}, androidVersionCode: ${{ github.run_number }} }
      - run: pwsh ci/size-gate.ps1 build/Android/*.aab 80
      - run: bash ci/check-16kb.sh build/Android/*.aab
      # Subida a la pista interna (r0adkll/upload-google-play): desactivada hasta el dictamen de marca
      # y hasta fijar el applicationId definitivo (sección 13.1 #1; docs/legal/marca.md).
```

Pistas y tránsito: interna (equipo, minutos) → cerrada (20-50 testers LatAm, 1-2 semanas; la cuenta de organización está **exenta** del requisito de 12 testers / 14 días, research/06 §5 [V]) → abierta opcional → producción escalonada 5 % → 20 % → 100 % (research/06 §10 [V/E]). Firmado: Play App Signing con clave de subida separada; AAB obligatorio (research/06 §4 [V]); la clave de subida debe valer 25 años o más y caducar después del 22-10-2033 (setup/b §6 [V]). La primera subida, también a la pista interna, espera al dictamen de marca (sección 13.1 #1).

### 12.2 iOS (fase 4)

| Elemento | Decisión | Evidencia |
|---|---|---|
| Cuenta | Apple Developer Program 99 USD/año; Small Business Program 15 % | research/06 §6 [V]; research/04 §2 [V] |
| Máquina | **Codemagic** (500 min gratis/mes; M4 a 0,114 USD/min) para builds de Unity → Xcode; sin Mac propio en 1.0. **Verificar antes de la fase 4 que Codemagic acepta la licencia Unity Personal** para la activación en su máquina; si exigiera Unity Pro, ≈2.200 USD/asiento/año [NV, cifra no verificada en ningún informe] y la alternativa pasa a ser el Mac mini M4 propio (pregunta abierta 5). Alternativas: MacinCloud M4 124,99 USD/mes, MacStadium M4.S 149 USD/mes, Mac mini M4 propio | research/06 §4 [V]; licencia [NV] |
| Toolchain | Desde el 28-04-2026 toda subida exige Xcode 26 / iOS 26 SDK; iOS mínimo soportado por Unity 6: [NV] confirmar en la matriz 6000.x | research/06 §6 [V/NV] |
| Privacidad | `PrivacyInfo.xcprivacy` propio con Required Reason APIs + manifiestos de cada SDK; etiquetas de privacidad coherentes con Data Safety; **ATT** con pre-prompt después del consentimiento GDPR (MAX funciona sin IDFA con menor eCPM); `SKAdNetworkItems` de las redes de MAX en `Info.plist` | research/06 §6 y §10 [V/E]; research/04 §2 [V] |
| Guardado | Firestore (sección 11), Game Center opcional para logros; sin iCloud KVS | research/06 C6.3 [V/E] |
| Distribución | TestFlight interno → externo → revisión (90 % en <24 h) | research/06 §6 [V] |
| CI | `ci/ios-build.yml` en Codemagic: Unity export → `xcodebuild` → App Store Connect API key → TestFlight | research/06 §4 [V] |

## 13. Checklists de publicación

### 13.1 Android (Google Play Console), paso a paso

| # | Paso | Detalle | Evidencia |
|---|---|---|---|
| 1 | Crear la app | **Condición previa (2026-10-06): dictamen favorable del abogado de marcas sobre Tintalú** (búsqueda en el INPI en las clases 9, 41 y 28; dictamen en México frente a EL TINTAL, IMPI reg. 2586508, clase 41; estrategia frente a TINTA en EE. UU.; `docs/legal/nombres-candidatos.md` §5). Aquazonia, el nombre anterior, se descartó ese día por la solicitud idéntica INPI 938306200 (clase 41). Hasta el dictamen no se sube a Google Play ningún AAB con el `applicationId` definitivo, tampoco a la pista interna, y los builds de prueba usan `com.amazonfill.dev`, que el CI no deja llegar a un build de release, así que antes del dictamen no se sube nada a Play (`docs/legal/marca.md`). Nombre "Tintalú: Ordenar Agua" (ES-419 como idioma predeterminado; EN "Tintalú: Water Sort Puzzle", PT-BR "Tintalú: Classificar Água"; tilde en todo lo visible), tipo Juego, gratis; **`applicationId` `com.<estudio>.tintalu`** (inmutable tras la primera subida; es público en la URL de la ficha, en Firebase, en MAX y en los informes de redes, por lo que no lleva la cadena "amazon": dirección 1 y glosario §1; el nombre interno Amazon Fill queda solo en carpeta, repositorio, código (`com.amazonfill.*`, ensamblados `AF.*`), prefijo `af_` y proyectos Firebase, con las formas del glosario §11); decisión de día 1 (pregunta abierta 1); nombre de desarrollador sin ninguna forma de "Amazon" | glosario §1.3; research/08 §4.2 [V] |
| 2 | Dominio y legales | Dominios propios, a registrar el 2026-10-06: **`tintalu.com`**, `tintalu.app` (registro de Google; exige HTTPS), `tintalu.co` y `tintalu.com.br` (este, a través del abogado brasileño: Registro.br exige CPF o CNPJ); los seis TLD consultados, incluidos `.games` y `.net`, figuraban libres según RDAP (`docs/legal/nombres-candidatos.md` §4.2). Canónico: `tintalu.com` [P]. Los dominios `aquazonia.*` dejan de ser los del juego. En el canónico: política de privacidad (ES/PT/EN, DPO y canal LGPD, aviso simplificado MX), términos, **página pública de eliminación de cuenta** (`/tintalu/eliminar-cuenta`, sección 11: la exige la política de eliminación de cuentas de Play y sin ella no se completa Data Safety) y `app-ads.txt` con las líneas de MAX, AdMob, Unity Ads, Meta y Mintegral | research/06 §10 [V]; research/08 C5.4 [V] |
| 3 | Play App Signing | Activar; subir clave de subida (`amazonfill-upload.jks`); guardar certificado de subida en gestor de secretos | research/06 §4 [V] |
| 4 | Configuración de la app | Target API **36** fijo en Player Settings (obligatorio desde 31-08-2026, extensión hasta 01-11-2026), minSdk 26, páginas de **16 KB** verificadas, Play Billing **8 o superior** (9.0.0 con IAP 5.4.4), `appCategory = Game` en el manifiesto fusionado (exime de las reglas de pantallas grandes de Android 16), retroceso predictivo probado y UI dentro del *safe area* (setup/b §1 y §Recomendación 3 [V]); hito siguiente en el calendario de CI: **target API 37 antes del 31-08-2027** [E] (09 §2.3) | research/06 §5 [V]; API 37 [E] |
| 5 | Ficha principal | Título ≤30, descripción corta ≤80, descripción ≤4.000 con la línea "Juego independiente inspirado en la cuenca amazónica; sin relación con Amazon.com, Inc."; icono 512; gráfico 1024×500; ≥8 capturas por idioma (ES-419, PT-BR, EN); vídeo 15-20 s con Llenar y teñir; sin "Best/#1/Free" ni emojis | research/08 §3.2 [V]; research/06 §10 [V] |
| 6 | Fichas personalizadas y experimentos | Ficha BR (Pix, portugués) y MX (OXXO); experimento de icono (cuia vs fruta) en hasta 5 idiomas | research/08 §3.2 [V] |
| 7 | Contenido de la app · política de privacidad y eliminación de cuenta | URL pública de la política antes de Data Safety; en "Eliminación de datos y cuenta" declarar la URL web de la sección 11 y que la app ofrece el borrado en Ajustes › "Eliminar cuenta y datos" (la app crea cuenta: Auth anónimo + Play Games Saved Games) | research/06 §5 [V]; política de eliminación de cuentas de Play |
| 8 | Contenido · anuncios | "Sí, contiene anuncios" | research/06 §10 [V] |
| 9 | Contenido · acceso | Sin login obligatorio; instrucciones para probar el Mercado con licencia de prueba | — |
| 10 | Contenido · clasificación IARC | Publicidad **sí**, compras **sí**, ítems aleatorios **no**, juegos de azar simulados **no**, interacción de usuarios **no** (1.0); esperado ClassInd 14 por las IAP | research/08 C5.2-C5.5 [V/E] |
| 11 | Contenido · público objetivo y edad | **13+**, sin programa Families; estética de frutas revisada para no ser "atractiva para niños" según Play. Pantalla de edad neutral en el primer arranque (sección 2.3) con **bandas {<13, 13-17, 18+}** (05 §7; GDD §6 #15): **<13 bloqueado en 1.0** ("Pide a un adulto que instale el juego"; antes de bloquear: `TagForChildDirectedTreatment`, Analytics sin ID, sin Play Games sign-in ni IAP); **13-17**: `npa`, `maxAdContentRating = G`, PIN de compras; segunda fuente de edad (Play Age Signals API) y consulta al abogado brasileño (ECA Digital art. 8) en la compuerta de la Fase 5 (09 §2.3) | research/06 §5 y §Implicaciones 8 [V/E]; research/08 C5.1 y C5.4 [V]; dirección 13; bandas y bloqueo [P] |
| 12 | Contenido · Data Safety | Una fila por SDK (tabla 13.3); encriptación en tránsito sí; eliminación de datos: **en la app y en la web** (sección 11), además del canal de soporte | research/06 §5 [V] |
| 13 | Contenido · noticias, COVID, gobierno, finanzas, salud | "No" en todos | — |
| 14 | Monetización · productos | Crear `af_starter`, `af_coins_2…6`, `af_rescue`, `af_noads`, `af_piggy`, `af_infinite_canoes_1h` (ID canónico, incidencia 16: 05 §4.1, 04 §7 y 00 §11 lo adoptan; `af_infinite_canoes_60` no existe), `af_skin_*` (y `af_pass` en 1.2); precios manuales por país para BR, MX, CO, PE, AR, CL, EC (≈50 % del ancla con redondeo local; las plantillas desaparecieron el 27-10-2025) | dirección 14; research/04 §3.3 [V] |
| 15 | Monetización · testers de licencia | Cuentas del equipo para compras de prueba | research/06 §10 [V] |
| 16 | Play Games Services | Proyecto, credenciales OAuth, Saved Games activado, logros básicos | research/06 §3 [V] |
| 17 | Play Asset Delivery | Verificar en el explorador del bundle el pack `world_w05`, los kits de los mundos 6-9 de D-013 (`bg_kit_w06` … `bg_kit_w09`, en `world_w06` … `world_w09` o, mientras no haya Addressables, en el AAB; los héroes y las skins de los mundos 6-13 entran en versiones posteriores) y el fondo "río genérico" install-time, de respaldo (6.2); tamaño base + install-time ≤80 MB | sección 6 |
| 18 | Play Integrity | Activar API y vincular con el proyecto de Firebase (App Check) | research/06 C6.1 [V] |
| 19 | Pista interna | Subir AAB desde CI; informe previo al lanzamiento sin crashes ni problemas de accesibilidad bloqueantes | — |
| 20 | Prueba cerrada | 20-50 testers de CO/PE/MX durante 1-2 semanas; medir crash rate, FPS y legibilidad en 360 px | research/06 §Implicaciones 7 [E] |
| 21 | Métricas de calidad | Crash percibido por usuario <1,09 % (umbral de Play); ANR percibido por debajo del umbral vigente de Play [NV cifra] | research/06 §10 [V] |
| 22 | Producción escalonada | 5 % → 20 % → 100 % en CO/PE/MX; países según `biz_country_enabled` y distribución de Play | capítulo 1 §8.1 |
| 23 | Tras publicar | Rellenar `app-ads.txt` en la ficha (sitio web del desarrollador), activar informes de Crashlytics y alertas de Play, archivar la versión de todos los textos legales mostrados | research/06 §10 [V] |

### 13.2 iOS (fase 4)

1. Cuenta Apple Developer (99 USD/año), App ID `com.<estudio>.tintalu` (mismo identificador que Android; sin la cadena "amazon"), certificados y perfiles gestionados por Codemagic.
2. Build con Xcode 26 / iOS 26 SDK; `PrivacyInfo.xcprivacy` propio y de cada SDK; Required Reason APIs declaradas (research/06 §6 [V]).
3. ATT con pre-prompt tras el consentimiento; `SKAdNetworkItems` de MAX en `Info.plist` (research/06 §10 [V]).
4. Etiquetas de privacidad coherentes con Data Safety; clasificación por edad: 13+ global; Brasil autoclasificado A12/A14 sin "Loot boxes" (research/08 C5.2 [V]).
5. IAP en App Store Connect con los mismos SKUs y precios por país; Small Business Program.
6. Capturas 6,9" y 6,5"; descripción localizada; URL de soporte y privacidad (research/06 §10 [V]).
7. TestFlight interno → externo → revisión.

### 13.3 Data Safety por SDK [E, a confirmar con la guía de cada SDK]

| SDK | Datos recogidos | Finalidad | Compartido con terceros | Eliminación |
|---|---|---|---|---|
| AppLovin MAX + adaptadores (AdMob, Unity Ads, Meta, Mintegral) | ID de publicidad, IP aproximada, interacciones con anuncios, diagnóstico | Publicidad, análisis de fraude | Sí (redes de la mediación) | Reinicio del ID de publicidad por el usuario; sin vínculo con el UID |
| Firebase Analytics | ID de instancia, interacciones en la app, país, modelo | Analítica | No | Retención configurada en Firebase; `resetAnalyticsData()` al eliminar la cuenta |
| Firebase Crashlytics | Registros de fallos, estado del dispositivo | Diagnóstico | No | Retención de 90 días de Firebase |
| Firebase Remote Config / Auth / App Check / Functions | ID de instancia, UID anónimo, veredicto de integridad, libro mayor `users/{uid}` | Funcionalidad, seguridad | No | **En app y web** (`deleteAccount`, sección 11); libro mayor anonimizado el plazo fiscal |
| Unity IAP / Play Billing | Historial de compras (token) | Funcionalidad | Procesado por Google Play | Gestionado por Google Play |
| Play Games Services | ID de jugador, datos de partida guardada | Funcionalidad | No | **En app y web**: el cliente borra el snapshot `af_main`; el jugador puede además borrar su perfil de Play Games |
| Unity Mobile Notifications | Ninguno (local) | — | — | — |
| Firebase Messaging (solo 1.2) | Token FCM | Funcionalidad (push remoto) | No | Con la cuenta; token eliminado en `deleteAccount` |

## 14. Plan de QA

### 14.1 Matriz de dispositivos LatAm [P a partir de research/08 §5.2 [V/E]]

| Clase | Dispositivos de referencia (3 vendors dominantes: Samsung, Motorola, Xiaomi) | Android | RAM / GPU | Uso |
|---|---|---|---|---|
| A · gama baja (referencia de rendimiento) | Moto G (serie 2x/3x de 2022-23), Galaxy A0x/A1x, Redmi 9/10 o Poco C | 8-11 | 3 GB · Mali-G52 / Adreno 610 | 60 fps objetivo, 30 mínimo; memoria; batería 60 min |
| B · gama media | Galaxy A2x/A3x, Moto G5x, Redmi Note 11-13 | 12-15 | 4-6 GB | Regresión funcional completa |
| C · gama alta / reciente | Galaxy S o A5x, Pixel reciente | 16 | 8 GB | Target API 36, 16 KB, permisos de notificación |
| Pantallas | 360×800 (20:9), 412×915, 390×844, 414×896, 1 tablet 16:10 | — | — | Filas de 02 §5 legibles (3 filas de ≤ 5 desde 11 cuias en 19,5-20:9, 6/5/5 con 16; 2 de hasta 7 en 16:9; D-006); safe areas |
| Emuladores | API 26 y API 36 x86_64 con páginas de 16 KB | — | — | Arranque, PAD, migraciones |

### 14.2 Pruebas automatizadas

| Suite | Qué comprueba | Dónde corre |
|---|---|---|
| Reglas (`AF.Core`) | Vertido legal/ilegal, vierte lo que cabe, sello, victoria, `legalNonFutileMoves` con y sin ocultos, cada mecánica con Marea (capítulo 4 §2.1), canonicalización | EditMode, cada PR |
| Solver | Óptimo en 50 tableros de referencia; presupuesto 40.000 nodos <50 ms en PC (research/02 §2.2 [E]); bot casual reproducible por semilla | EditMode |
| Niveles (`tools/level-qa`) | Todo paquete: resoluble, par, etiqueta coherente con bot ≥75 %/≥90 %, debuts de tinte, `max_risk_pairs`, Aguas turbias con ≥1 vertido seguro en todas las realidades, `relief` resoluble, determinismo por semilla | CI, cada cambio de paquete |
| Flujo de bloqueo | Árbol de decisión del capítulo 4 §6.2 con 12 contextos (nivel <31, sin saldo T1/T3, sin fill, irresoluble, 5 fallos…) | PlayMode |
| Anuncios | Simulador de sesión: ningún intersticial antes del 22, nunca al inicio ni en reintento, ≥150 s, tope 6/sesión, supresión 24 h tras IAP, topes por ubicación y cooldowns | PlayMode con `IAdNetwork` falso |
| Economía (`tools/economy-sim`) | 30 días × 3 perfiles: saldo al nivel 50/100/200 y "bloqueados sin recursos" dentro de los objetivos (≤3 % / ≈8 % / 15-20 %, research/04 C1 §5 [E]) | CI al cambiar defaults |
| Guardado | Migraciones 1→n sobre perfiles de versiones anteriores; conflictos local/nube (8 casos); escritura atómica con corte de proceso | EditMode |
| Localización | Pseudo-localización `en-XA` (+30 % de longitud como mínimo; la implementación alarga un 35 % [P], dentro del +30-40 % de la propuesta de idiomas, §3.7, con corchetes y solo letras de Latin-1; D-023), textos que desbordan en 360 px, plurales ES/PT/EN | PlayMode captura |
| Accesibilidad (02 §12, 07 §7.8) | TalkBack recorre el tablero: cada cuia anuncia posición, capacidad, capas visibles/ocultas y mecánica, y la bandeja de comodines anuncia icono + etiqueta funcional; fuente del sistema al 130 % sin desbordes ni solapes en 360 px (HUD, modal Aguas estancadas, Mercado); "reducir movimiento" acorta Llenar y teñir; interruptor de pista por inactividad respetado | PlayMode captura + dispositivo clase A con TalkBack |
| Rendimiento | Escena con 16 cuias + chorro + burbujas 60 s: fps p5 ≥55 en clase A; memoria estable (sin crecimiento >10 MB en 30 niveles) | Dispositivo físico, nightly |
| Build | Tamaño ≤80 MB, 16 KB, API 36, firmas | CI |

### 14.3 Pruebas manuales por fase

- **Pista interna**: IAP con licencia de prueba (compra, cancelación, `PENDING` con método diferido, reembolso → Voided Purchases), restauración en segundo dispositivo, Sin anuncios cruzado con rewarded activo, PAD con Wi-Fi apagado, consentimiento por país (cambiar región con VPN), ATT (iOS).
- **Prueba cerrada LatAm** (20-50 testers): crash-free sessions ≥99,5 % [P], FPS en clase A, legibilidad de 14 tintes con simulador de daltonismo sobre el shader real (research/07 §Preguntas abiertas [E]), embudo 1-30, comprensión de "El río bajó" y de las etiquetas Difícil/Muy difícil.
- **Regresión de release**: lista de 60 casos (HUD, modal, Mercado, Bitácora, Canasta, Reto del río, cambio de idioma, notificaciones locales, segundo plano a mitad de vertido, pérdida de red durante compra y durante anuncio).

## 15. Instalación inicial del entorno (Windows 11 del dueño)

Actualizado el 2026-10-06. La fuente es el plan de instalación aprobado (`docs/setup/plan-instalacion.md`), con los comandos completos y quién hace cada paso (`s01`-`s32`); esta sección resume el estado y conserva los pasos de servicios que vienen después. La máquina y sus riesgos están en la sección 16.

### 15.1 Estado al 2026-10-06

| Componente | Estado | Paso del plan |
|---|---|---|
| .NET SDK 10.0.401 (win-arm64, en `C:\Program Files\dotnet`) | Instalado y verificado; `global.json` fija 10.0.401 con `rollForward: latestFeature` | s03, s08 |
| Unity Hub 3.22.2 y Unity CLI 1.0.0-beta.12 (`unity`), MSIX arm64 | Instalados y verificados | s04 |
| Git 2.53 + Git LFS 3.7.1 (global), `gh` autenticado, Python 3.14 con `jsonschema`, Node 24, JDK 17 (Temurin, para `keytool`) | Instalados y verificados | s01-s02 |
| `adb` 37.0.0 (`platform-tools` en el PATH de usuario) | Instalado y verificado | s06 |
| Repositorio git (rama `main`, `core.longpaths`) | Commit inicial `88d3331` subido al remoto `origin`, el repositorio privado `github.com/Nipko/fill-game` | s07, s13-s14 |
| Editor 6000.3.25f1 (`e1dba0a9aba4`) ARM64 + Android Build Support (OpenJDK 17.0.18+8, SDK y NDK r27c, plataformas 34-37) | Instalado en `C:\Program Files\Unity\Hub\Editor\6000.3.25f1\Editor\Unity.exe`; corre nativo en ARM64 | s17-s18 |
| Login de Unity y licencia Personal | Hecho; licencia activa (el Licensing Client la valida en cada arranque del Editor) | s15-s16 |
| Driver Adreno ≥31.0.170.0 (hoy 31.0.137.0) | Pendiente del dueño | s05 |
| Proyecto Unity URP 2D (`Assets/`, `ProjectSettings/`, `Packages/manifest.json` y `packages-lock.json`) | Creado el 2026-10-06 en la raíz, con `com.unity.pipeline` 0.8.0-exp.1, el merge driver `UnityYAMLMerge`, los hooks de la Unity CLI, las skills locales `unity-cli` y `unity-pipeline` y los Player Settings por script; EditMode 35/35 en verde | s19-s25 |
| **Compuerta del día 1** (sección 16.2) | **Superada** el 2026-10-06 en la parte de los agentes (`docs/setup/compuerta-dia-1.md`); faltan las pruebas del dueño en el teléfono (s27) y en Play Mode (s28) | s26-s28 |

Variables de entorno: `ANDROID_HOME` y `ANDROID_SDK_ROOT` no se definen (Unity usa el SDK, el NDK y el JDK de su módulo, la misma cadena que el CI; el NDK 27.1 de Android Studio no sirve porque Unity exige r27c); `JAVA_HOME` sigue en Temurin para `keytool`; `.claude/settings.json` (cuando el dueño apruebe `.claude/settings.proposed.json`, s12) define solo para los agentes `UNITY_NON_INTERACTIVE=1`, `UNITY_NO_BANNER=1`, `DOTNET_CLI_TELEMETRY_OPTOUT=1` y `DOTNET_NOLOGO=1` (plan, §Pasos de instalación).

### 15.2 Pasos de la puesta en marcha

| # | Paso | Quién | Verificación | Estado (2026-10-06) |
|---|---|---|---|---|
| 1 | `unity auth login` y `unity license activate --personal --accept-eula` | Dueño | `unity license status` | Hecho |
| 2 | Proyecto: `unity projects create amazon-fill --path ../_unity-create --editor-version 6000.3.25f1 --template com.unity.template.universal-2d`, copia a la raíz, `unity pipeline install`, primera importación con EditMode, `unity vcs doctor --fix` / `merge-setup` / `hooks install`, skill local | Agentes | `ProjectVersion.txt`; `merge-setup --check` → 0 | Hecho |
| 3 | Player Settings por script (`AF.EditorTools.ProjectSetup.ApplyAndroidDefaults`): Product Name provisional `Amazon Fill (dev)`, `applicationId` de prueba `com.amazonfill.dev`, minSdk 26, **target 36 fijo**, IL2CPP solo ARM64, Strip Engine Code, Managed Stripping High, ASTC, Build App Bundle; el script no fija Company Name. Hasta el dictamen de marca los builds de prueba conservan esos valores provisionales: el Product Name `Tintalú`, el Company Name y el `applicationId` definitivo `com.<estudio>.tintalu` van en el build de release | Agentes | minSdk 26, target 36, IL2CPP ARM64 | Hecho; comprobado en el APK de la compuerta |
| 4 | **Compuerta del día 1** (sección 16.2) | Agentes + dueño (teléfono y Play Mode) | 3 de 3 builds en verde; "Verification successful" | Superada en la parte de los agentes (s26); faltan s27 y s28 (dueño) |
| 5 | Revisar `.claude/settings.proposed.json` (al aprobarse se renombra a `.claude/settings.json`), primer commit, repositorio privado en GitHub (`Nipko/fill-game`) | Dueño + agentes | Ningún secreto en `git ls-files`; `visibility` = PRIVATE | Commit inicial `88d3331` en el repositorio privado; falta la revisión de los permisos (s12) |
| 6 | CI: job `core`; secretos `UNITY_EMAIL` y `UNITY_PASSWORD` de una cuenta Unity dedicada sin 2FA (`gh secret set`, siempre el dueño); tests de Unity y AAB en `main` | Agentes + dueño (secretos) | `gh run list` en verde | Pendiente (s30-s32) |

### 15.3 Servicios y SDK (Fases 1-2)

| # | Paso | Comando / acción | Verificación |
|---|---|---|---|
| 1 | Paquetes Unity | Package Manager con versiones fijadas: Addressables 2.10.3, Addressables for Android 1.1.0, Localization 1.5.x, Input System, TextMeshPro, Mobile Notifications, Test Framework, In App Purchasing 5.4.4 (con Authentication 3.7.1 o superior). Editar `Packages/manifest.json` exige aprobación del dueño | `Packages/manifest.json` con las versiones fijadas; `Window › Asset Management › Addressables` abre |
| 2 | Firebase | Proyectos `amazon-fill-dev` y `amazon-fill-prod` (el dueño crea las cuentas); app Android con el paquete y el SHA-1 de depuración; `google-services.json` fuera del repositorio (lo ignora git y los agentes no pueden leerlo); Firebase Unity SDK **13.17.0** solo con `FirebaseAnalytics`, `FirebaseCrashlytics`, `FirebaseRemoteConfig`, `FirebaseAuth`, `FirebaseAppCheck` y `FirebaseFunctions`; `firebase_analytics_collection_enabled=false` en el manifiesto hasta el consentimiento; implementaciones falsas en el editor (sección 2.1, principio 6) | Primer `first_open` desde un build en dispositivo (con consentimiento aceptado) |
| 3 | MAX | Cuenta AppLovin (dueño), app y unidades (`rewarded`, `interstitial`); plugin MAX **8.6.6**; en `AppLovin › Integration Manager`, adaptadores Google, Unity Ads, Meta y Mintegral; automatización de Google UMP activada y mensaje del RGPD en AdMob › Privacy & messaging; **sin plugin Google Mobile Ads** | Mediation Debugger en el dispositivo con las 4 redes "Ready"; compuerta de 16 KB en verde con todos los adaptadores |
| 4 | Play Games + Integrity | Play Games plugin **2.3.0** y Play Integrity; `Window › Google Play Games › Setup` con los recursos de Play Console; Saved Games activado en Play Console antes de programarlo; SHA-1 de la clave de firma de Play y de la de subida | Inicio de sesión silencioso en un build de la pista interna (tras el dictamen de marca) |
| 5 | Herramientas | `tools/level-generator` y `tools/level-qa` en .NET 10 (ya en el andamiaje, sección 3); `npm i -g firebase-tools`; `firebase login` (dueño); `firebase init functions` (TypeScript) en `functions/` | `dotnet test dotnet/AmazonFill.slnx` en verde; `firebase projects:list` muestra los dos proyectos; runtime de Cloud Functions con Node 24 [NV]: usar la versión LTS de Node que Functions soporte al desplegar |
| 6 | Keystore de subida | `keytool -genkeypair -v -keystore amazonfill-upload.jks -alias upload -keyalg RSA -keysize 2048 -validity 10000` (lo ejecuta y custodia el dueño; caducidad posterior al 22-10-2033); fuera del repositorio; Base64 → secretos `ANDROID_KEYSTORE_*` | Play Console acepta el certificado al activar Play App Signing |
| 7 | Secretos de publicación | `PLAY_SERVICE_ACCOUNT` (cuenta de servicio de Play Console con permiso de publicación en la pista interna), solo después del dictamen de marca | Primera subida a la pista interna desde CI |
| 8 | Dispositivos | Un teléfono de clase A y uno de clase B (sección 14.1) con depuración USB; "Mostrar actualizaciones de pantalla GPU" | Unity Profiler conectado por USB; `adb devices` lo lista |

Hito técnico 1 (semanas 1-3, antes de cualquier otra cosa): prototipo del shader de líquido con 16 cuias, vertido con chorro, Llenar y teñir y agua base por mundo a 60 fps en el dispositivo de clase A (dirección 20; capítulo 1 §10 riesgo 10), sin Bloom del 2D Renderer (sección 16.3). Hito 2 (semanas 1-3, en paralelo y sin necesidad de Unity): `AF.Core` + `AF.Solver` con pruebas en `dotnet test` y `tools/level-generator` produciendo el primer `mundo_01_v1.json` (estimación de research/02 Complemento 4.6: 2-3 días solver, 1-2 días generador, 1 día indexado por fecha [E]). El reparto entre subagentes y el calendario están en el capítulo 9 §0.

## 16. Máquina de desarrollo Windows ARM64

Añadida el 2026-10-06 (plan, §Decisión de motor y §Riesgos; setup/a [V]). La máquina del dueño, donde trabajan los agentes, es un portátil **Windows 11 Home 25H2 ARM64 con Snapdragon X Plus (X1P42100), GPU Adreno X1-45 y 15,6 GB de RAM** (setup/a, cabecera [V, comprobación local]).

### 16.1 Qué corre nativo y qué emulado

| Pieza | Binario | Ejecución en Snapdragon X | Fuente |
|---|---|---|---|
| Editor 6000.3.25f1 | ARM64 (`UnitySetupArm64-6000.3.25f1.exe`), soportado oficialmente; algunos componentes internos siguen siendo x64 (Package Manager, Enlighten) | Nativo | setup/a §1 y §4 [V] |
| Módulo Android Build Support | El mismo módulo que en x64; trae OpenJDK 17.0.18+8 y NDK r27c como **binarios x64** | Emulado por Prism (Windows 11 24H2+) | setup/a §3-§4 [V] |
| Gradle, build-tools 36, platform-tools | x64 / JVM x64 | Emulado | setup/a §4 [V] |
| Hub 3.22.2, Unity CLI beta.12, .NET SDK 10.0.401 | ARM64 | Nativo | setup/a §2 [V]; setup/d §2 [V] |
| API gráfica del editor | DX12 por defecto, DX11 con `-force-d3d11`; **Vulkan no está soportado** en Windows on Arm (sí en el juego en Android) | — | setup/a §5 [V] |

Unity dice que los builds para plataformas sin SDK nativo ARM64 "might still work with x86 emulation" y nadie ha publicado un build IL2CPP de Android hecho desde un Snapdragon X [NV] (setup/a §4). Por eso el editor ARM64 queda **para editar, Play Mode y tests**; la compuerta del día 1 decide si los builds locales sirven (resultado del 2026-10-06: sirven para iterar, sección 16.2), y **el build de referencia para publicar es siempre el del CI Linux x64** (plan, §Decisión de motor).

### 16.2 Compuerta del día 1

Se pasa antes de escribir gameplay, con el proyecto URP 2D vacío (plan s25-s28; setup/a §Recomendación 5 [V/E]):

| Prueba | Criterio de aceptación | Quién | Resultado (2026-10-06) |
|---|---|---|---|
| Player Settings | minSdk 26, target 36, IL2CPP solo ARM64, AAB (`AF.EditorTools.ProjectSetup.ApplyAndroidDefaults`) | Agentes | OK: el APK lleva minSdk 26, target 36, solo `arm64-v8a` y `com.amazonfill.dev` (`aapt2 dump badging`) |
| Build Android | Tres veces `AF.EditorTools.BuildSmoke.BuildAndroid` (AAB + APK): **3 de 3 en verde** | Agentes | **3 de 3 en verde** (código de salida 0, sin errores) |
| Tiempos | Build IL2CPP limpio e incremental registrados; aceptable si el limpio tarda **<15 min** [E] | Agentes | Limpio: 26,5 min (25,4 el AAB; el clang del NDK emulado se lleva 13). Incrementales: 6,8-7,2 min. **Supera los 15 min orientativos**: el dueño decide si son vinculantes |
| Gradle | Versión registrada (9.1.0 según la tabla de Unity o 9.3.1 según las notas del 25f1; setup/b §2.2 [V]) | Agentes | **Gradle 9.3.1 + AGP 9.0.0** (bundletool 1.17.2) |
| Páginas de 16 KB | `zipalign -c -P 16 -v 4` sobre el APK: "Verification successful" | Agentes | OK: "Verification successful"; `ci/check-16kb.sh` da 14 de 14 `.so` alineadas a 16 KB (APK y AAB) |
| Dispositivo | `adb install -r Builds/Android/smoke.apk` y la app arranca (`pm list packages`) | Dueño (USB) | Pendiente (s27) |
| Editor y Shader Graph | `unity open .` y `unity open . --args -force-d3d11`: 30 min de Play Mode con cada uno, sin cuelgues ni artefactos | Dueño | Pendiente (s28) |

**Resultado (2026-10-06): superada en la parte de los agentes (s26).** El detalle, los tiempos por fase, los tamaños y los SHA-256 están en `docs/setup/compuerta-dia-1.md`. El AAB de humo pesa 35,12 MB frente a los 80 MB de `ci/size-gate.ps1`. Consecuencias: el editor ARM64 sirve también para builds locales de iteración (los incrementales tardan unos 7 min), y el build de referencia para publicar sigue siendo el del CI Linux x64 (B2, s32). B1 no acortaría el build limpio, porque el cuello de botella es el clang del NDK, emulado con cualquiera de los dos editores; B3 solo recortaría Gradle (unos 3,8 min). Gradle 9.3.1 + AGP 9.0.0 cuenta para la compatibilidad de los SDK (09 R25).

### 16.3 Planes B y reglas de uso

- **B1 · Editor x64 bajo Prism solo para builds:** misma versión (`UnitySetup64-6000.3.25f1.exe` + Android Support en otra carpeta, registrado con `unity editors add`); se sigue editando con el ARM64. Que `-a x86_64` funcione en Windows no está verificado [NV]. También sirve para depurar un SDK con DLL x64 en el editor (setup/a §6 [V]).
- **B2 · Builds solo en CI Linux:** `unityci/editor:ubuntu-6000.3.25f1-android-3` con game-ci; el editor local queda para edición, Play Mode y tests. Los agentes lanzan el build con `gh` y descargan el AAB/APK como artefacto (setup/a §7 [V]; setup/b §Riesgos 2).
- **B3 · JDK 17 ARM64 nativo** (Microsoft OpenJDK 17, MSI aarch64) para Gradle, solo si el cuello de botella es Gradle y tras la compuerta; va contra la recomendación por defecto de Unity de usar su JDK (setup/a §4 [V/E]).
- **Cuelgues del sistema:** driver Adreno ≥31.0.170.0 (paquete de Qualcomm del 03-09-2026); desactivar Windows › Sistema › Pantalla › Gráficos › "Automatic super resolution"; `-force-d3d11` (setup/a §5 [V]; 09 R18).
- **Render:** sin Bloom del 2D Renderer en 1.0 (pantalla negra, UUM-150232, en la lista del 6000.3.25f1); artefactos de URP/Render Graph (UUM-92264) → driver nuevo y `-force-d3d11` (setup/a §5 y setup/b §Riesgos 6 [V]; 09 R19).
- **SDK con DLL x64 en el editor** (Firebase, EDM4U, herramientas de MAX): implementaciones falsas en el editor (sección 2.1, principio 6); los SDK reales se prueban en dispositivo y en CI (09 R20).
- **RAM:** como máximo 2 Editores abiertos y Android Studio cerrado mientras Unity compila; los agentes de C# puro no abren Editor (09 R23).
- **Un solo `adb`:** el del PATH de usuario (37.0.0); no mezclarlo con el del módulo de Unity (36.0.0) (09 R24).
- **Ruta del proyecto sin espacios** (`C:\Users\Nipko-lab\Desktop\projects\amazon-fill`) y la misma mayúscula de unidad en local y en CI, por fallos de EDM4U y PGS con espacios (setup/b §Recomendación 8 [V]).
- **Godot (plan C)** solo si salta un disparador de la sección 1.2, incluido un editor ARM64 inutilizable de forma persistente tras B1 y B2.

## Parámetros configurables (Remote Config)

La **única tabla de Remote Config del GDD** está en la sección 9 de este capítulo (clave, tipo, default compilado, rango y capítulo dueño); este capítulo no la repite y los demás capítulos la referencian sin volver a listar claves ni valores. Las claves técnicas propias de este capítulo (`rc_*`, `consent_text_version`, `save_*`, `sec_*`, `perf_*`, `biz_min_app_version`, `biz_ga_enabled`, `ads_rv_max_wait_fill_ms`, `ads_ilr_to_firebase`, `live_events_schedule`) figuran en esa misma tabla con dueño "08"; `consent_under13_policy` y `consent_age_signal_source` están dadas de alta con dueño "05" (05 §7).

## Desviaciones de la dirección

Ninguna. Precisiones que la dirección no fijaba y este capítulo cierra:

1. **Fuente de verdad multiplataforma en Firestore desde 1.2** (sección 11) además de Play Games Saved Games (dirección 20); iOS no usará iCloud KVS. Motivo: research/06 C6.3 [V/E] y el libro mayor de compras ya exigido por la dirección 20 (Cloud Functions de verificación).
2. **Notificaciones locales (Unity Mobile Notifications) en 1.0 y FCM en 1.2**: reduce un SDK con datos personales (LGPD, dirección 15) y cubre "Reto del río listo" y "Crecida en riesgo" sin servidor (research/05 §5 [V]).
3. **Managed Stripping High** en lugar del Medium de research/06 §Implicaciones 1 [E], con `link.xml`; se revierte a Medium si algún SDK falla por reflexión.
4. **Sin SDK de atribución en 1.0** (Adjust/AppsFlyer); entra en la fase 3 con la UA pagada en US (dirección 19). La dirección 20 no lo menciona; research/09 §Técnico [E] lo atribuye al líder.
5. **Economía de semillas autoritativa en cliente en 1.0** (sección 10.1): coherente con "sin servidor propio hasta 1.2" (research/06 C6.1 [V]); lo que vale dinero real se verifica en servidor.
6. **`applicationId` `com.<estudio>.tintalu`** (secciones 12.1, 13.1, 13.2 y 15): la dirección 1 permite "Amazon Fill" en carpeta y código, pero el ID de paquete es público (URL de la ficha, Firebase, MAX, informes de redes) e inmutable, así que sigue la regla del nombre comercial; el nombre interno Amazon Fill queda en la carpeta (`amazon-fill`), el código (`com.amazonfill.core`, ensamblados `AF.*`), el prefijo `af_`, el `applicationId` de prueba `com.amazonfill.dev` y los proyectos Firebase (`amazon-fill-dev` y `amazon-fill-prod`); el repositorio es `Nipko/fill-game` (formas canónicas en el glosario §11).
7. **Niveles instalados 1-700 (mundos 1-9) y 701+ por `levels_remote`** (sección 6.2): la dirección 17 pide ≥600 jugables al lanzar y +100 cada 1-2 semanas; como los paquetes son por mundo y el mundo 9 termina en el 700 (glosario §8), el corte en 600 se redondea al mundo completo: 700 instalados, ≥600 jugables con QA completa (así lo recogen 03 §17, 06 §10 y 09). La cadencia no depende de la revisión de la tienda.
8. **Push "remotos" emulados en local en 1.0** (secciones 2.2 `AF.Platform` y 7): "Nuevos niveles" se programa al cerrar la app con `live_content_end_message.next_batch_eta_days` cuando el jugador alcanzó el último nivel publicado; "Regreso" D7/D14/D30 se agendan al pasar a segundo plano y se cancelan al abrir; "Premio por reclamar" se agenda al salir con un premio pendiente. FCM y los push segmentados por evento llegan en 1.2 (Desviación 2).
9. **Codificación de `BoardState` len + (tinte − 1)** (sección 4.1): mantiene K_motor = 16 (dirección 3) en 4 bits por unidad; la forma "0 = sin unidad" de 03 §3 y la hexadecimal `1-e/f` de la versión anterior de este capítulo solo representaban 15 tintes.
10. **Cinco Cloud Functions en 1.0** (sección 2.4) en lugar de dos: la reacreditación del libro mayor (04 §10.2), la retirada de reembolsos (Voided Purchases API) y la eliminación de cuenta exigida por la política de Play y por LGPD/Ley 1581 no caben en el cliente. Sigue sin haber servidor de juego propio.
11. **Intersticiales cada 2 niveles desde el 61** (`ads_interstitial_every_n_levels`, 05 §6.1), no desde el 101 como proponía la versión anterior de este capítulo: la dirección 13 fija "cada 2-3 niveles" y el capítulo de economía es el dueño del valor; se calibra en vivo con `af_ad_shown` y la nota de tienda.
12. **Plan de instalación del 2026-10-06** (no contradice la dirección 20; la concreta): versión exacta 6000.3.25f1 en lugar de "6000.x LTS vigente"; .NET 10 en lugar de .NET 8 (que pierde soporte el 10-11-2026, setup/d [V]); núcleo en el paquete embebido `Packages/com.amazonfill.core` + `dotnet/` en lugar de `Assets/_Project/Scripts/Core|Solver`; CI con `UNITY_EMAIL`/`UNITY_PASSWORD` sin `UNITY_LICENSE`; sin plugin Google Mobile Ads (AdMob y UMP dentro de MAX); target API fijo en 36; workflows en `.github/workflows/` y no en `ci/` (secciones 1.2, 2.1, 3, 7, 12.1 y 16).

## Preguntas abiertas para el dueño del producto

**Estado al 2026-10-06** (el dueño aceptó las recomendaciones del GDD y del plan): la **1** sigue **en curso**: nombre comercial **Tintalú** (decisión del dueño del 2026-10-06, con riesgo de marca medio aceptado; sustituye a Aquazonia, descartado por la solicitud idéntica INPI 938306200); dominios `tintalu.com`, `tintalu.app`, `tintalu.co` y `tintalu.com.br`; el `applicationId` definitivo `com.<estudio>.tintalu` se fija tras el dictamen del abogado de marcas (INPI, México frente a EL TINTAL y EE. UU. frente a TINTA) y hasta entonces no se sube a Play ningún AAB con él; los builds de prueba usan `com.amazonfill.dev` (`docs/legal/marca.md`; `docs/legal/nombres-candidatos.md`). La **7** queda **cerrada**: 6000.3.25f1, con parches 6000.3.x solo en PR dedicado y CI en verde (sección 1.2). La **9** queda **cerrada**: Godot está evaluado como plan C en setup/e y no se instala (sección 1.2). La **6** se decide con la recomendación de GDD §6 #15 (Crashlytics como interés legítimo; Analytics tras el consentimiento) hasta la opinión legal. Las **2-5, 8 y 10** se resuelven con la opción que este capítulo ya aplica por defecto (sin SDK de atribución hasta la fase 3; GameAnalytics apagado; Unity CCD o R2 al publicar el primer paquete remoto; Codemagic con verificación de la licencia antes de la Fase 6; dos administradores por servicio; Sin anuncios entre tiendas según GDD §6 #17).

1. **ID de aplicación** (`com.<estudio>.tintalu`, fijado en la sección 13.1 #1 sin la cadena "amazon"): es inmutable tras la primera subida y hace falta el dominio del estudio (también para la política de privacidad, la página de eliminación de cuenta y `app-ads.txt`). ¿Qué dominio y qué nombre de desarrollador se usarán? Debe cerrarse el día 1 del proyecto (capítulo 9 §8).
2. **SDK de atribución**: ¿se acepta no tenerlo hasta la fase 3 (menos SDKs, menos Data Safety) y medir el soft launch solo con Firebase + Google Ads?
3. **GameAnalytics como segunda capa** (`biz_ga_enabled`): ¿se activa desde la beta cerrada para funnels más rápidos o se espera a necesitarlo?
4. **Hosting remoto de niveles**: Unity CCD (integrado con Addressables) o Cloudflare R2 (más barato a escala, egreso 0); ambos gratis en 1.0.
5. **iOS**: Codemagic pay-as-you-go (sin Mac) frente a Mac mini M4 propio; decide el presupuesto de la fase 4 y quién depura en dispositivo iOS. Incluye verificar si Codemagic acepta la licencia Unity Personal; si exige Pro (≈2.200 USD/asiento/año [NV]), el Mac mini propio pasa a ser la opción por defecto (sección 12.2).
6. **Crashlytics y analítica antes del consentimiento**: este capítulo trata Crashlytics como diagnóstico (interés legítimo; GDD §6 #15) y lo activa en Boot; Analytics espera al consentimiento. Consulta al abogado para BR/CO/PE/MX, antes de la Fase 5, con dos preguntas: (a) Crashlytics como interés legítimo; (b) **analítica pseudonimizada por interés legítimo** (05 §7, última fila, y 05 pregunta 12): ¿puede Firebase Analytics registrar eventos de juego sin `user_id` ni ID de publicidad antes del consentimiento? Si la respuesta es sí, Boot arranca Analytics en modo sin ID; si es no, el embudo por nivel de 03 §15 y 09 solo cubre a quienes aceptan analítica.
7. **Cerrada (2026-10-06): versión exacta de Unity 6 LTS.** 6000.3.25f1 (6.3 LTS, changeset `e1dba0a9aba4`), fijada en `ProjectSettings/ProjectVersion.txt`, de donde la lee el CI; durante 1.0 solo entran parches 6000.3.x, cada uno en un PR dedicado y con el CI en verde (`docs/setup/plan-instalacion.md`).
8. **Cuentas de servicio**: quién administra Play Console, Firebase, AppLovin y GitHub (mínimo dos administradores por servicio para no bloquear la publicación).
9. **Asesor técnico de Godot**: ¿se encarga una estimación de porte en paralelo (1-2 días) para tener el plan B cuantificado antes del hito técnico 1, o se espera al disparador?
10. **Reconciliación de Sin anuncios Android ↔ iOS** vía libro mayor (capítulo 4, pregunta 8): afecta al diseño de `entitlements` en Firestore.
11. **Respaldos del kill-switch** (D-007): **cerrada (2026-10-13):** en la 1.0, `mech_enabled.<id>` = false sustituye los niveles con `level_overrides`, y el formato de respaldo por slot se fija con el build de la 1.1 (GDD §6.3, pregunta 31, (b); D-020). Tarea de código: el cliente aún no lee `mech_enabled`.
12. **Temporada del ciclo en el pack** (D-009): **cerrada (2026-10-13):** la LUT de aguas altas o bajas (07 §2.1) sale de una tabla del cliente por ciclo, copiada del calendario versionado; el pack no cambia (GDD §6.3, pregunta 32, (b); D-020). Tarea de código pendiente.
13. **Claves de las mecánicas de la 1.1** (D-007; propuesta, §(g)): `mech_debut_level` y `mech_enabled` de Aguapé, Palizada, Lancha y Cocha, y los defaults de `lancha_*` y `cocha_k_pours`, no están en la sección 9: se dan de alta con el build de la 1.1, que trae su código y su `pack_schema`. Hasta entonces, sus debuts (223, 263, 303 y 583) solo están en 02 §9.1. No bloquea.
14. **Claves de las tablas de textos y columnas de estado** (D-017 y D-018; secciones 4.6-4.7): **cerrada (2026-10-14):** dos familias de claves (UI en `snake_case`; contenido largo con puntos) y una columna de estado por idioma, con la cabecera de Frutas y la columna `args` [P] (GDD §6.3, pregunta 49, (a); D-022). Sección 4.7.

## Dependencias con otros capítulos

| Capítulo | Este capítulo necesita | Este capítulo entrega |
|---|---|---|
| 00 Glosario | IDs de código (§11), nombres de las claves `ui_*`, nombre interno Amazon Fill y sus formas (`amazon-fill`, `amazonfill`, `AF.`, `af_`), agua base del shader, HEX de tintes | Convención de archivos y Addressables (sección 3), prefijo `af_` en analítica, `applicationId` `com.<estudio>.tintalu`; defaults de `ui_*` en la tabla única (sección 9) |
| 01 Visión | Claves `biz_*`, fases de mercado, umbrales de nota y crash para la fase 0 | Umbrales técnicos de la beta cerrada (crash-free ≥99,5 %, AAB ≤80 MB, primera interacción <6 s), coste de servicios por versión (research/06 C6.3 [E]: 0-10 USD/mes en 1.0, 15-40 en 1.2) |
| 02 Núcleo y reglas | Catálogo exacto de mecánicas y su serialización (`attrs`), máximo de cuias en pantalla, flags `skippable` / `hint_supported`, fases y valores de Llenar y teñir (§4.2) y del vertido (§4.1) | Interfaces `IMechanic`, representación de `BoardState` (len + tinte−1), esquema `af-level/1` y `schemas/af-level-1.schema.json` (sección 4.1, a la que remite 02 §9.3; `attrs` `{type, key, partner, n, k}` sin `barro`, con `matapi` y `pack_schema` 2 desde D-007; ejemplo canónico: nivel 43, debut de Hoja, según la regla transversal 2 de 02 §9.2), claves `anim_*` y `victory_autonext_s` de la tabla única (sección 9), canon de entrega de niveles para el caso borde "Nivel N+1 no listo" (02 §14: los primeros 700 niveles van en el AAB), presupuesto de rendimiento con 16 cuias |
| 03 Niveles, generación y dificultad | Campos de nivel de 03 §10 integrados en `af-level/1`, métricas `bot_fail/branching/traps/D`, variantes `relief[]`, pool del Reto del río (3 bandas × 730; bandas K 6-7 / 8-9 / 10-11 de 03 §16; `id` `d{b}_n{nnnn}`, `seed` `amazonfill:v1:daily:b{b}:d{ddd}`, sin métricas y con `relief` = `[]`), regla de orden de `relief` (03 §13), primer Difícil 40 / Muy difícil 60 (03 §11/§12) | `tools/level-generator` y `tools/level-qa` como compuerta de CI (nombres canónicos: ensamblados `AF.Core` / `AF.Solver`, carpeta `tools/level-generator`; criterio de alivio de `tools/level-qa` en 4.1: `bot_fail` baja ≥ 0,20 si el base parte de ≥ 0,20; con base < 0,20, como los debuts de 02 §9.2, basta `par` menor en `minus_color` o el vacío extra de `plus_empty`, 03 §17 #8); JSON Schema canónico `schemas/af-level-1.schema.json` (única copia, a la que remiten 03 §10 y §17 #11; `D` sin límites, `par` nulo solo en `plus_empty`, `relief` obligatoria desde el 31, formato diario; 03 §3 usa la codificación len + tinte−1); claves `lvl_*` (incl. `lvl_murky_max_hidden_units` = 4 y `lvl_dda_enabled` = false), `relief_*`, `level_overrides`, `daily_overrides` y `content_version` de la tabla única (sección 9); entrega 1-700 instalados / 701+ remotos (03 §17) |
| 04 Comodines, Canoas y rescate | Reglas de comodines, Canoas y rescate; definición de fallo (04 §5.2: fallo blando con ≥8 vertidos; cerrar la app no es fallo y, desde el 31, salir del nivel con el menú sí, D-008); esquema de inventario (se integra tal cual) | Tabla única de Remote Config (sección 9: `undo_*` incl. `undo_unlimited_until_level` = 10, `extra_cuia_unlock_level` = 8, `hint_unlock_level` = 12 + `hint_tutorial_units` = 3, `hint_idle_from_level` = 3 / `hint_idle_delay_s` = 12, `rescue_price_bands` = `[[31,600]]`, `rescue_first_day_mult`, `rescue_max_per_level` = 1, `canoes_*`…), flujo SSV, `restoreLedger`, fusión de guardado, eventos con prefijo `af_`; Canasta determinista en 04 §2.2/2.3/§3; SKU `af_infinite_canoes_1h` (04 §7) |
| 05 Economía y monetización | Valores de Bitácora, Canasta, Crecida (05 §2.4), Cántaro, precios, SKUs, cadencia de anuncios, metas de ARPDAU (05 §9: ≥0,015 LatAm fase 1; ≥0,08 US antes de UA), bandas de edad y bloqueo <13 (05 §7) | Tabla única de Remote Config (sección 9: `eco_*` incl. `eco_starter_first_show` = "L12_or_session2", `ads_*`, `streak_milestones` como clave única, `rescue_price_bands` = `[[31,600]]`, SKU `af_infinite_canoes_1h`, `eco_model_version` = "B-2026-10-06e", `consent_under13_policy` = "block" y `consent_age_signal_source` = "self_declared"; 05 referencia sin repetir claves), `af_wallet_snapshot`, `tools/economy-sim`, precios por país en Play Console, Boot con las tres bandas de edad (sección 2.3) |
| 06 Meta, retención y live-ops | Reglas de Reto del río, Bitácora, Crecida, Canasta, push (permiso en la 2.ª sesión, 06 §8.1) y eventos | Tabla única de Remote Config (sección 9: `live_*`, `push_preprompt_session` / `push_preprompt_min_level`, `league_*`; `eco_basket_cycle` sustituye a `chest_cycle`; `streak_milestones` en lugar de `eco_streak_rewards`; `eco_calendar_double_ad_enabled` = false en 1.0; `ui_murky_bonus_enabled` = false en 1.0 (1.1: true); `victory_autonext_s` = 0), push 1.0 solo locales, eventos de meta de 06 §12 dados de alta en la taxonomía única (sección 8.2), entrega de paquetes por `levels_remote` (06 §10), pool diario 2.190, integración MAX/IAP, `ad_impression` con ILR, Data Safety por SDK |
| 07 Arte y audio | Atlas por mundo ≤2-4 MB, fondos 1080×2400, 14 patrones, cuia con cavidad recta para la máscara, agua parda de Aguas turbias (`_MurkyColor` = #8A7B6A, ocultos por `_HiddenMask`; 07 §4.3), orden canónico de la bandeja de comodines (07 §7.4) para la regla icono + etiqueta del HUD | Presupuesto de texturas (ASTC 6×6/4×4), estructura de capas de la cuia, especificación del shader y de Llenar y teñir, claves `anim_*`, `ui_*` y `audio_*` de la tabla única (sección 9) |
| §13 Checklists de publicación (ASO y legal, interno a este capítulo) + 01 Visión §9 | Textos legales por país, versión de consentimiento, política de privacidad y `app-ads.txt` (01 §9 fija el posicionamiento y la ficha; no existe un capítulo "ASO y legal") | Pantalla de consentimiento propia + UMP con bandas de edad, respuestas del cuestionario IARC, lista de SDKs para Data Safety y Privacy Labels, página de eliminación de cuenta |
| 09 Roadmap y producción | Equipo de agentes (09 §0: 7 subagentes, máximo 2 Editores, puertas humanas; antes 1-2 programadores + artista, dirección 21); riesgos de la máquina y del CI R17-R28; hitos de calendario (target API 37 antes del 31-08-2027 [E]; compuerta de la Fase 5 con Play Age Signals y consulta legal) | Secuencia de integración (hitos técnicos 1 y 2), tiempos de CI, coste de iOS (incl. verificación de la licencia Unity en Codemagic), lista de instalación del entorno, claves `biz_*` de la tabla única (sección 9) y decisión de día 1 del `applicationId` (sección 13.1 #1) |
