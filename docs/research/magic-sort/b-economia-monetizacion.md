# Magic Sort! (Grand Games) — Frente B: Economía y monetización

**Proyecto:** Amazon Fill · **Fecha:** 2026-10-05 · **Autor:** análisis senior (estilo DoF/Naavik) · **Estado:** investigación documental (no se jugó el build; 16 fuentes primarias/secundarias leídas, ~25 búsquedas)

**Convención:** `[V]` = dato verificado con URL en la sección Fuentes · `[E]` = estimación/inferencia propia · `[NV]` = reportado por una sola fuente o por reseñas de usuarios, no confirmado.

---

## 1. Resumen ejecutivo

Magic Sort es el primer water-sort que monetiza como un match-3 "Dreamificado": presentación de nivel Royal Match, economía de moneda dura con precios copiados del match-3 turco (continuar = 900 monedas; 1.000 monedas = 1,99 USD) y un fail-state instrumentado al milisegundo. El resultado: con el 3,4 % de los DAU de los diez sorting games más grandes capturaba el 43 % de su IAP en 2025 `[V]`. Grand Games pasó de 2,8 M USD de IAP con Magic Sort en 2024 a 45 M USD en 2025 (≈ 79,4 M USD acumulados a mayo 2026) `[V]`, y el portafolio corre a ~500 M USD anualizados (junio 2026) `[V]`.

La lección para Amazon Fill no es "ponle anuncios a un water-sort" sino lo contrario: **cobrar en el momento de la frustración con una moneda que el jugador casi nunca tiene suficiente**, y mantener los anuncios como complemento (~20–25 % del ingreso), no como motor.

---

## 2. Economía de monedas (hard currency)

### 2.1 Lo que sabemos con certeza

| Concepto | Valor | Estado | Fuente |
|---|---|---|---|
| Continuar nivel sin movimientos ("+1 Bottle") | **900 monedas** | `[V]` (5+ reseñas iOS con fecha, 2025) | App Store reviews, iofreeonline |
| Booster "frasco extra" pre-nivel | 900 monedas (mismo precio) | `[V]` reseñas | App Store reviews |
| Herramienta barata (hint/Shuffle según reseña) | 300 monedas | `[NV]` 1 reseña | App Store reviews |
| Undo | se vende con monedas; coste no confirmado | `[NV]` | iofreeonline, Uptodown |
| Bonus de racha (3 niveles seguidos) | **300 monedas** | `[V]` reseñas múltiples | App Store reviews |
| Mini-evento de 10 niveles | hasta 10.000 monedas repartidas por hitos | `[NV]` 1 reseña | App Store reviews |
| Vidas | 5 vidas; ~25 min por vida regenerada | `[V]` 2 reseñas independientes | App Store reviews |
| Vidas infinitas temporales | periodos de 15–30 min como premio; una reseña reporta 13 h ganadas en evento | `[NV]` | App Store reviews |
| Pedir vidas a compañeros de equipo | sí | `[V]` descripción oficial | App Store listing |
| Monedas por nivel normal/hard/super hard | **no publicado** | `[E]` ver 2.2 | — |

Observación clave: **900/1.000/1,99 USD son exactamente los puntos de precio del "continuar" de Royal Match** (dato de conocimiento de mercado, no re-verificado en esta sesión). Grand Games no inventó la economía; la importó entera del match-3 y la pegó a un water-sort.

### 2.2 Ratio de presión (estimación)

Ninguna fuente publica la recompensa base por nivel. Las reseñas convergen en que es pequeña ("rara vez consigues algo significativo"), y la única cifra concreta es el bonus de racha: 300 monedas por 3 victorias seguidas.

| Escenario | Supuesto | Niveles ganados por cada "continuar" (900) |
|---|---|---|
| Sólo bonus de racha | 300 / 3 niveles = 100 monedas/nivel efectivas | **9 niveles sin fallar** |
| Racha + recompensa base baja `[E]` | +20–50 monedas por nivel | 6–8 niveles |
| Jugador que rompe la racha cada 2 niveles `[E]` | sólo base | 18–45 niveles |

El diseño es deliberadamente asimétrico: el bonus de racha se cobra justo cuando el jugador va bien, y el "continuar" se cobra justo cuando va mal **y además le hace perder la racha**. Gamigion resume la tesis: ganas menos de lo que cuesta continuar, por diseño `[V]`. Un revisor de Gamigion gastó 7 € en monedas para mantener una racha de evento y ganó un premio que valía menos que una continuación `[V]`.

Precio efectivo de un "continuar" comprado con dinero `[E]`:

| Paquete | Monedas | USD | Continuaciones | USD / continuar |
|---|---|---|---|---|
| 1000 Coins | 1.000 | 1,99 | 1,1 | 1,80 |
| 5000 Coins | 5.000 | 7,99 | 5,6 | 1,44 |
| 10000 Coins (SKU visto en snapshot mar-2026) | 10.000 | 14,99 | 11,1 | 1,35 |

Un fail cuesta de facto **1,35–1,80 USD**. En un juego de 4,8 sesiones/día `[V]` eso es una palanca brutal.

---

## 3. Catálogo IAP

### 3.1 App Store EE. UU. (verificado 2026-10-05) `[V]`

| SKU | USD | Qué es (lo verificable) |
|---|---|---|
| Starter Bundle | 0,99 | Pack de entrada (contenido no publicado) |
| Special Offer 0.99$ | 0,99 | Oferta contextual |
| 1000 Coins | 1,99 | ≈ 1 continuar |
| Apprentice Bundle | 4,99 | Bundle intermedio |
| Special Offer 4.99 | 4,99 | Oferta contextual |
| Endless Portal 5.99 | 5,99 | Ver 3.3 |
| 5000 Coins | 7,99 | ≈ 5,5 continuar |
| Magician Bundle | 9,99 | Bundle alto |
| Special Offer 9.99$ | 9,99 | Oferta contextual |
| 10000 Coins | 14,99 | Visto en snapshot apppricinglab 2026-03-21, no en la ficha de hoy `[NV]` |
| Sorcerer Bundle | 19,99 | Bundle premium |

Rango 0,99–19,99 USD en iOS; media 7,29 USD (apppricinglab) `[V]`. Un estudio comparativo (Casual Puzzle Intelligence Report 2026) lista "6 bundles, 0,99–99,99 USD", lo que sugiere SKUs de hasta 99,99 en Android o en ofertas dinámicas `[NV]`. **No existe SKU de "quitar anuncios", ni suscripción, ni pase de temporada vendido por dinero, ni alcancía (piggy bank) en la ficha** `[V]`. Contenido exacto de Apprentice/Magician/Sorcerer: no publicado; por convención del género son monedas + boosters (Undo/Shuffle/+1 frasco) + minutos de vidas infinitas `[E]`.

### 3.2 Precios regionales (App Store, verificados hoy) `[V]`

| SKU | EE. UU. | Brasil | México | Colombia | España |
|---|---|---|---|---|---|
| Starter / Special 0.99 | 0,99 USD | R$ 6,90 | 19 MXN | 4.900 COP | 0,99 € |
| 1000 Coins | 1,99 | R$ 12,90 | 39 | 9.900 | 1,99 € |
| Apprentice / Special 4.99 | 4,99 | R$ 29,90 | 99 | 24.900 | 5,99 € |
| Endless Portal | 5,99 | R$ 39,90 | 129 | 29.900 | 6,99 € |
| 5000 Coins | 7,99 | R$ 49,90 | 179 | 39.900 | 8,99 € |
| Magician / Special 9.99 | 9,99 | R$ 59,90 | 199 | 49.900 | 9,99 € |
| Sorcerer | 19,99 | R$ 129,90 | 399 | 99.900 | 22,99 € |

Lectura: Grand Games usa los **tiers automáticos de Apple sin descuento LatAm** (R$ 6,90 ≈ 1,25 USD; 19 MXN ≈ 1,0 USD; 4.900 COP ≈ 1,15 USD). Para un jugador brasileño el "continuar" cuesta en poder adquisitivo 2–3× lo que a uno estadounidense. **Hueco directo para Amazon Fill**: en Google Play el desarrollador fija precios por país.

Volumen relativo iOS (valoraciones): EE. UU. 669 K · Brasil 76 K · México 39 K · España 19 K · Colombia 11 K `[V]`. LatAm iOS ≈ 19 % del volumen de EE. UU.; en Android la proporción será mayor `[E]`.

### 3.3 "Endless Portal" (5,99 USD)

No hay descripción oficial del SKU. Evidencia: la ficha oficial promete "periodos de vidas infinitas" `[V]`; las reseñas describen periodos de 15–30 min de juego ilimitado como premio `[NV]`; el nombre "Endless" encaja con vidas ilimitadas y "Portal" con la temática. **Inferencia `[E]`: pack de tiempo de vidas infinitas (probablemente horas) + algo de moneda, vendido como oferta puntual.** No es "sin anuncios" (no hay ningún SKU que mencione ads) `[V]`.

### 3.4 Special Offers: cuándo aparecen

Tres SKUs genéricos (0,99 / 4,99 / 9,99) sirven de contenedor para ofertas distintas. Momentos documentados: tras fallar sin monedas suficientes (upsell desde la pantalla de continuar) `[V]` Gamigion; tras hitos de nivel y en eventos de racha `[E]`. Google Play: rango de precios por artículo no pudo verificarse (ficha no legible por herramienta); descargas 10 M+, 4,5★ `[V]` (página de desarrollador).

---

## 4. Anuncios

| Pregunta | Respuesta | Estado |
|---|---|---|
| ¿Intersticiales? | Sí, hibrido IAP+IAA | `[V]` Naavik, Braberg |
| ¿Desde qué nivel? | Nivel 22 (Gamigion) / nivel 24 (FriendlyGameDev) → "primeros 20 niveles limpios" | `[V]` dos fuentes, discrepan en 2 niveles |
| Cadencia | Reseñas reportan "tras cada nivel" a partir de ~nivel 50 | `[NV]` agregador de reseñas (mwm.ai) |
| Rewarded: ¿para qué? | Vida extra al agotar las 5 vidas (vídeo largo), monedas, vidas; frascos extra según algunos usuarios | `[V]` vida extra; `[NV]` resto |
| Nº de placements rewarded | **1** (el menor de 23 juegos estudiados) | `[V]` Casual Puzzle Report 2026 |
| ¿Banner? | Sin evidencia de banner en ninguna fuente | `[E]` no |
| ¿Pagar quita anuncios? | No existe SKU; comportamiento post-compra no documentado | `[NV]` |
| Peso de ads en ingreso | ≈ 1 M USD de 4,15–4,35 M USD en 30 días (mayo 2025) → **~23 %** | `[V]` Braberg |
| Queja nº 1 en reseñas negativas | "demasiados anuncios" en 67 % de las negativas; 33 % dicen que el anuncio fuerza el cierre; 5 % que secuestra controles | `[V]` Glotier |

Contraste importante: Deconstructor of Fun describe Magic Sort como juego "pure-IAP" `[V]`, pero los datos de Braberg (≈23 % ads) y las reseñas muestran que **hay** intersticiales y que son la principal fuente de fricción. La lectura correcta: el negocio es IAP; los anuncios son relleno de monetización para no-pagadores tardíos.

---

## 5. End-of-round monetization: el flujo completo

Reconstrucción del flujo (lo verificado + lo inferido):

1. **Detección instantánea** del estado sin solución: el juego calcula que no quedan vertidos legales y lo notifica de inmediato `[V]` Braberg, FriendlyGameDev. No hay contador de movimientos ni timer; el fail es binario y sorpresivo.
2. **Pop-up "+1 Bottle" por 900 monedas** `[V]`. Un frasco vacío extra suele resolver el nivel de inmediato ("de super hard a super easy", reseña) → percepción de valor muy alta `[V]`.
3. **Si hay saldo:** se descuenta y continúa; **la racha se conserva** (eventos Broom Streak / Sky Jump / Jetpack Race dependen de ella) `[V]` Naavik.
4. **Si no hay saldo:** redirección a tienda/oferta contextual: Special Offer 0,99 o 1000 Coins 1,99 (primer precio que cubre 900) `[E]` basado en SKUs y en el testimonio de 7 € gastados para seguir una racha `[V]`.
5. **Rechazar = reiniciar nivel + perder racha + perder una de 5 vidas** `[V]` reseñas. En eventos con multiplicador, perder la racha borra progreso acumulado.
6. Tras varios fails sin pagar, aparece el rewarded de vida extra (vídeo largo) `[V]` reseñas.

Por qué funciona (síntesis Naavik/Braberg/PocketGamer): **pérdida triple** en un botón (nivel, racha, vida: se paga por no perder, no por ganar) `[V]`; **precio ligeramente inaccesible** (900 vs 300 de racha: siempre a "un paquete pequeño" de distancia); **la compra cumple** (el +1 frasco resuelve de verdad). Resultado: continuar/racha genera la mayoría del IAP; 3,3 M USD con 478 K DAU en 30 días `[V]`.

Crítica recogida: la monetización es "reactiva" (frustración tras el flow) y los picos de dificultad de hard/super hard llegan antes de que se forme el hábito de gasto; Knit Out (pase de batalla) y Pixel Flow (eventos de momentum) monetizan "inversión" en vez de "frustración" `[V]` PocketGamer, mayo 2026.

### 5.1 Live-ops ligados a la economía (desbloqueo por nivel) `[V]` Casual Puzzle Report 2026

| Sistema | Nivel de desbloqueo | Tiempo aprox. de juego |
|---|---|---|
| Intersticiales | 22–24 | ~30 min |
| Bono diario | 34 | ~45 min |
| Leaderboard / Weekly Cup | 39 | ~55 min |
| Jetpack Race (sprint) | 80 | — |
| Sky Jump (etiquetado como "pase estacional" en el estudio) | 119 | ~5 h |
| Onboarding sin fails | 1–18; primer pico de fallo en nivel 19 | ~1 h 21 min hasta ese tramo |

Además: equipos (pedir vidas), cartas coleccionables, loot boxes, Nature Collection, eventos nuevos cada ~2 meses `[V]` Naavik.

---

## 6. Cifras de negocio

### 6.1 Ingresos y descargas de Magic Sort

| Periodo | Ingreso | Descargas | Fuente / estado |
|---|---|---|---|
| Lanzamiento (abril 2024) → ene 2025 | ~3 M USD IAP | 593 K instalaciones | `[V]` Gamigion (ene 2025) |
| Año 2024 (IAP) | 2,8 M USD | — | `[V]` InvestGame |
| 30 días a fin de mayo 2025 | 4,15–4,35 M USD (3,3 M IAP + ~1 M ads) | — | `[V]` Braberg |
| Primer año | > 40 M USD | — | `[V]` DoF (feb 2026) |
| Año 2025 (IAP) | **45 M USD** | ~33 M (portafolio) | `[V]` InvestGame |
| Año 2025 (bruto, Sensor Tower) | ~50 M USD (18 % del subgénero Sort) | — | `[V]` Naavik |
| Mar 2025 → mar 2026 | 65,8 M USD | 35,7 M | `[V]` Casual Puzzle Report (Sensor Tower) |
| Acumulado a mayo 2026 (IAP) | **79,4 M USD** (74 % del portafolio) | **58 M** | `[V]` InvestGame |

Grupo Grand Games: > 4 M USD/mes en ene 2025; IAP 2025 = 58 M USD (≈19× YoY); "> 1 M USD diarios" y run-rate ~500 M USD (may–jun 2026); 73 M descargas acumuladas; financiación 3 + 30 + 70 = 103 M USD, valoración ~6× la de la Serie A `[V]` InvestGame, Dealroom, DoF.

### 6.2 Split IAP vs ads, ARPDAU, RPD

| Métrica | Valor | Cálculo / fuente |
|---|---|---|
| Split IAP/ads | **~77–80 % IAP / 20–23 % ads** | `[V]` Braberg (3,3 M vs ~1 M) |
| DAU | 478 K (may 2025) | `[V]` Braberg |
| ARPDAU bruto | **≈ 0,29–0,30 USD** `[E]` | 4,3 M / 30 / 478 K |
| ARPDAU (Sensor Tower, abr 2025–abr 2026) | 0,16 USD | `[V]` PocketGamer — probablemente neto o sólo IAP |
| "ARPU" mensual por DAU | 8,68 USD (vs Hexa Sort 4,28; Bus Out 1,95) | `[V]` Braberg |
| RPD | 1,70 USD (PG) / 1,84 USD (Casual Report) | `[V]` dos cortes de Sensor Tower |
| RPD sólo IAP acumulado | ≈ 1,37 USD `[E]` | 79,4 M / 58 M |
| Sesiones/día | 4,8 | `[V]` PocketGamer |
| Retención | D1 40–50 % (benchmark water-sort); D30 ≈ 8 % benchmark Sort US; Magic Sort "muy por encima" en D30 | `[V]` Naavik |
| D1 en cohortes pagadas | 23 % → 29 % tras optimizar creatividades | `[V]` Artik |

Benchmark de contexto: ARPDAU hybrid-casual 0,15–0,50 USD; casual sólo-ads 0,01–0,05 USD `[V]` Playio/Game Growth Advisor. Magic Sort está en la banda alta del hybrid-casual con DAU modesto.

### 6.3 Países

EE. UU. es el mercado dominante: Android US pasó de 1,3 K a 27,5 K usuarios activos y > 108 K USD semanales en el Q1 2025 `[V]` Sensor Tower. Distribución exacta por país: no publicada `[NV]`. Proxy iOS por valoraciones en 3.2. Localizado a 11 idiomas incl. portugués y español `[V]`.

### 6.4 UA y CPI

| Dato | Valor | Estado |
|---|---|---|
| Canales | Sólo AppLovin y Meta (ene 2025); escala sobre todo en AppLovin; 8 playables en test | `[V]` Naavik, Gamigion |
| CPI Android (ene–mar 2025) | 0,74 → **0,50 USD** | `[V]` Artik |
| CPI iOS (ene–mar 2025) | 3,15 → **2,17 USD** | `[V]` Artik |
| IPM Android / iOS | 3,5 → 6,23 / 0,68 → 1,31 | `[V]` Artik |
| Conceptos creativos ganadores | "gatito pide leche", "rescate del gatito", "carrera contra el tiempo"; playables con umbral de clics 1/3/10/20/∞ | `[V]` Artik |
| Gasto UA | no publicado. **Estimación gruesa `[E]`**: ~20–30 M descargas de Magic Sort en 2025, 60–70 % pagadas, CPI mixto 0,8–1,2 USD → 12–25 M USD (25–50 % del ingreso) | `[E]` |

Naavik recuerda que el casual puzzle es uno de los géneros con CPI más caro; Grand Games compensa con creatividades "mass-market/hypercasual" sobre una mecánica fácil de entender `[V]`.

---

## 7. Comparación con un water-sort tradicional (ad-heavy)

| Dimensión | Magic Sort (Grand Games) | Water Sort Puzzle (IEC Global) | Hexa Sort (Lion) — referencia ads |
|---|---|---|---|
| Modelo | IAP-first + ads complementarios | Ads-first + "No Ads" | Ads-first |
| Catálogo IAP | 10–11 SKUs, 0,99–19,99 USD, sin "no ads" | No Ads 2,99; Premium 4,99; Revival Pack 2,99; No Ads Diamond 4,99; Diamond 1–6 1,99–99,99 `[V]` | — |
| Cadencia de anuncios | Intersticial desde nivel 22–24; "cada nivel" ~L50+ `[NV]` | Intersticial **tras cada nivel, al repetir y al pedir frasco**; 40–60 s `[V]` reseñas | — |
| Monedas por nivel | bajas; racha 300/3 niveles | **cero**; sólo 200 monedas por vídeo rewarded (cosméticos) `[V]` | — |
| Continuar | 900 monedas (+1 frasco) | frasco extra a cambio de rewarded | — |
| Ingreso 30 días (may 2025) | 4,15–4,35 M USD (77 % IAP) | Sensor Tower público: < 5 K USD/mes IAP, ~100 K descargas/mes (recientes); 4 M desc. y 10 K USD en jun 2021 `[NV]` snippet | 13,7 M USD (90 % ads) `[V]` |
| DAU | 478 K | n/d | 3,2 M `[V]` |
| Tamaño | 609 MB iOS / 218 MB APK | 282 MB iOS `[V]` | — |
| Rating iOS | 4,7★ (669 K) | 4,7★ (324 K) `[V]` | — |
| Queja típica | ads intrusivos + "paywall" en hard | "pagué No Ads y sigo viendo anuncios" `[V]` reseñas | — |

Contexto: antes de Magic Sort, toda la categoría water-sort generaba **menos de 2 M USD/año** (DoF) `[V]`. El subgénero Sort completo creció a ~280 M USD en 2025 (+176 % YoY, ~1.000 M descargas) `[V]` Naavik; AppMagic mide +116 % `[V]`. Magic Sort = 18 % del subgénero con una fracción de los DAU de los líderes por ads.

---

## Qué copiar / qué mejorar / qué evitar para Amazon Fill

### Copiar (funciona y está probado)
1. **Economía 900 / 300 / 1,99 USD.** Continuar = 900 monedas; racha de 3 = 300; paquete mínimo que cubre un continuar = 1,99 USD. No reinventar: es el estándar Royal Match → Magic Sort y los jugadores ya lo tienen interiorizado.
2. **Detección instantánea de bloqueo + "+1 recipiente" como oferta única.** Una sola oferta, muy efectiva (resuelve el nivel), en el segundo exacto del fail. Nada de menú de 4 boosters.
3. **Racha como colateral.** Que el "continuar" proteja algo acumulado (racha, multiplicador de evento, cofre en progreso). El jugador paga para no perder, no para ganar.
4. **Primeros ~20 niveles sin anuncios, sin fails** (L1–18 limpios, primer pico en L19). Onboarding largo y tranquilo; intersticiales desde L22–24.
5. **Vidas 5 / 25 min + vidas infinitas temporales** como moneda de evento y de equipo (pedir vidas a compañeros).
6. **Un solo placement rewarded** (vida extra) + intersticiales moderados. Magic Sort tiene el menor número de rewarded de su cohorte y el mayor IAP.
7. **Cadencia de contenido**: +100 niveles por actualización, evento nuevo cada ~2 meses, desbloqueos escalonados (bono diario L34, liga L39, sprint L80, evento largo L119).
8. **Creatividades UA emocionales + playables con umbral de clics** (CPI Android 0,50 USD): equivalente amazónico, "la nutria necesita agua limpia", "rescata al delfín rosado", "carrera contra la crecida".

### Mejorar (donde Magic Sort cojea y Amazon Fill puede ganar)
1. **Precios regionales en Google Play.** Magic Sort cobra tiers Apple sin descuento (1000 monedas = R$ 12,90 / 39 MXN / 9.900 COP). Amazon Fill nace para Brasil, Colombia, Perú y Ecuador: paquete de entrada a 0,49–0,99 USD local y "continuar" a ≈ 0,99 USD local.
2. **Dificultad honesta.** Las reseñas castigan que "super hard" sea imposible sin booster y que las etiquetas engañen: garantizar solución sin booster (solver interno) y etiquetar con verdad.
3. **Monetización de "inversión", no sólo de frustración.** Un pase/colección al estilo Knit Out donde el pago mejora la capacidad de jugar (recipientes ancestrales, cofres del río) además de restaurar la racha.
4. **Transparencia en eventos de racha**: que los no-pagadores puedan completar la racha con habilidad; reservar el pago para acelerar, no para desbloquear. (Queja documentada: "pagué 7 € y el premio valía menos").
5. **Racha persistente entre sesiones** (una reseña perdió una racha de 19 por dormir): tolerancia de 24–48 h o "congelar racha" como recompensa de rewarded/equipo.
6. **Peso de la app**: 609 MB en iOS es un freno en LatAm con datos limitados. Objetivo < 150 MB inicial con descarga diferida de fondos de países.
7. **Rewarded orientado a monedas pequeñas (50–100)** además de vida extra: el no-pagador necesita sentir que la ruta gratis hacia 900 existe (hoy está prácticamente cerrada).

### Evitar
1. **Intersticial tras cada nivel a partir de L50** y anuncios sin botón de cierre: 67 % de las reseñas negativas de Magic Sort son por anuncios; no sacrificar la nota (4,7★) por un 20 % del ingreso.
2. **No vender "sin anuncios" ni tener piggy bank/suscripción**: Magic Sort deja dinero en la mesa; Amazon Fill debe tener al menos un SKU "sin anuncios + bono" a 2,99–4,99 USD equivalente local y una alcancía que capture monedas de niveles.
3. **Depender de dos redes de UA** (AppLovin + Meta): abrir Google Ads, TikTok y Mintegral desde el soft launch en LatAm.
4. **Introducir mecánicas nuevas tarde** (crítica recurrente): planificar un modificador nuevo (recipientes opacos, agua turbia, caudal limitado) cada 40–60 niveles desde el inicio.
5. **El modelo IEC Global** (ads cada nivel, "No Ads" que no funciona, cero monedas por nivel): produce 100 M descargas y < 2 M USD/año de categoría. Es el contraejemplo exacto.

---

## Fuentes

Verificadas en esta sesión (2026-10-05):

- App Store EE. UU. — ficha e IAP: https://apps.apple.com/us/app/magic-sort/id6499209744
- App Store reseñas EE. UU. (costes 900/300, vidas 5/25 min, rachas): https://apps.apple.com/us/app/magic-sort/id6499209744?see-all=reviews
- App Store Brasil: https://apps.apple.com/br/app/magic-sort/id6499209744
- App Store México: https://apps.apple.com/mx/app/magic-sort/id6499209744
- App Store Colombia: https://apps.apple.com/co/app/magic-sort/id6499209744
- App Store España: https://apps.apple.com/es/app/magic-sort/id6499209744
- Google Play — página de desarrollador Grand Games (10 M+, 4,5★): https://play.google.com/store/apps/dev?id=6430038787206489041&hl=en
- Uptodown (APK 218 MB, v0.0.3293, 4-oct-2026): https://com-grandgames-magicsort.en.uptodown.com/android
- App Pricing Lab (11 SKUs, snapshot 2026-03-21): https://apppricinglab.com/app/apple/6499209744
- Felix Braberg, "The Monetization Engine Behind Magic Sort" (30-may-2025): https://felixbraberg.substack.com/p/the-monetization-engine-behind-magic
- Gamigion, republicación del anterior: https://www.gamigion.com/the-monetization-engine-behind-magic-sorts-rise/
- Gamigion, "Magic Sort Analysis: Grand Games' New Treasure" (ads desde L22, UA): https://www.gamigion.com/magic/
- Gamigion / Oxana Fomina, "Game Deconstruction: Magic Sort": https://www.gamigion.com/game-deconstruction-magic-sort-by-grand-games/
- Naavik, "Grand Games and Leveling Up Mobile Casual Puzzle": https://naavik.co/digest/why-peak-games-investors-backed-grand-games/
- Naavik, "How Niche Subgenres are Reshaping the Mobile Puzzle Market" (22-feb-2026): https://naavik.co/digest/how-niche-subgenres-are-reshaping-the-mobile-puzzle-market/
- Deconstructor of Fun, "Sort Puzzles: How a New Subgenre is Born" (6-feb-2026): https://www.deconstructoroffun.com/blog/2026/2/6/sort-puzzles-how-a-new-subgenre-is-born
- Deconstructor of Fun, "Grand Games from $0 to $500M in Two Years" (8-jun-2026): https://www.deconstructoroffun.com/blog/grand-games-from-0-to-500m-in-two-yearsnbsp
- PocketGamer.biz / Oxana Fomina, "One genre, three strategies" (14-may-2026): https://www.pocketgamer.biz/one-genre-three-strategies-how-magic-sort-knit-out-and-pixel-flow-are-redefining-sort-puzzle-monetisation/
- PocketGamer.biz, Serie B 70 M (11-may-2026): https://www.pocketgamer.biz/grand-games-raises-70m-series-b-to-scale-hybrid-casual-gaming/
- InvestGame, Serie B (IAP por título, descargas por título): https://investgame.net/news/grand-games-balderton-backs-70m-series-b-as-turkiye-s-hybrid-casual-momentum-accelerates/
- Dealroom, Serie B ("> 1 M USD diarios"): https://dealroom.co/news/128722-grand-games-raises-70m-as-turkeys-mobile-gaming-machine-keeps-producing/
- TechFundingNews, Serie B (5× YoY): https://techfundingnews.com/turkish-gaming-startup-grand-games-bags-70m-series-b-led-by-balderton-capital-for-hybrid-casual-puzzle-games/
- TheNextWeb, Serie B: https://thenextweb.com/news/istanbuls-grand-games-lands-a-70m-series-b-as-balderton-doubles-down-on-turkeys-puzzle-game-pipeline
- mobilegamer.biz, Data digest (13-may-2026): https://mobilegamer.biz/data-digest-sensor-tower-inmobi-and-smartlys-big-buys-grand-lands-70m-huge-applovin-numbers-more/
- TechCrunch, Serie A (9-ene-2025): https://techcrunch.com/2025/01/09/grand-games-the-turkish-gaming-startup-behind-pouring-game-magic-sort-drinks-up-30m/
- Balderton, Serie A: https://balderton.com/?p=10975
- Sensor Tower, Top hypercasual Android US Q1 2025: https://sensortower.com/blog/2025-q1-android-top-5-hypercasual%20games-revenue-us-643acc7de1714cfff1e77c54
- Casual Puzzle Games Intelligence Report 2026 (RPD 1,84, placements, desbloqueos): https://casual-report.tiiny.site/
- Artik Agency, caso Magic Sort UA (CPI 0,50 / 2,17): https://artik.agency/cases/magic-sort-user-acquisition-case-study
- Glotier, análisis de reseñas: https://glotier.com/games/magic-sort
- mwm.ai, agregador de reseñas: https://mwm.ai/apps/magic-sort/6499209744
- FriendlyGameDev, reseña (ads desde L24): https://friendlygamedev.com/game-reviews/magic-sort/
- iofreeonline, tips (900/300, vidas): https://www.iofreeonline.com/IOS/game/Magic-Sort.html
- Algoryte, "The ultimate sorting game face-off" (120 K USD/día): https://algoryte.substack.com/p/the-ultimate-sorting-game-face-off
- Water Sort Puzzle (IEC Global) App Store — IAP: https://apps.apple.com/us/app/water-sort-puzzle/id1514542157
- JustUseApp, reseñas Water Sort Puzzle (cadencia de ads): https://justuseapp.com/en/app/1514542157/water-sort-puzzle/reviews
- Playio, benchmarks ARPDAU: https://blog.playio.co/arpdau-benchmarks-mobile-games
- GameDev Reports, AppMagic Landscape 2026 (+116 % Sort): https://gamedevreports.substack.com/p/appmagic-mobile-market-landscape

No verificables en esta sesión (páginas con muro o no legibles): ficha Google Play de Magic Sort y de Water Sort Puzzle (rango de precios IAP, flag de ads), páginas de app.sensortower.com, publisher page de AppMagic, vídeos de YouTube (descripciones no accesibles), hilos de Reddit (presupuesto de búsqueda agotado).
