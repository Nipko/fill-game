# Monetización probada en puzzles tipo water-sort: anuncios + IAP (2025-2026)

Fecha de investigación: 2026-10-05. Convención: **[V]** = dato verificado con URL en "Fuentes"; **[E]** = estimación o inferencia propia; **[S]** = cifra citada en resúmenes de terceros que no pude confirmar en la fuente primaria.

## Resumen ejecutivo

- El género sort/water-sort es **ad-dependiente por defecto pero migra a híbrido**: Hexa Sort ~70 % ads / 30 % IAP [S]; Magic Sort! lo invirtió (79,5 % IAP, 4,15 M USD en 30 días, 478 K DAU, ARPU 8,68 USD) [V]. Sensor Tower mide hybridcasual "lifestyle & puzzle" en 59 % IAP / 41 % ads [V].
- Puzzle concentra el **53 % de todo el ingreso publicitario** de juegos móviles (Sort ≈7 % de ese bloque). En emergentes (Brasil incluido) los anuncios aportan 55-70 % del ingreso; en US/CA/JP/KR el IAP domina 77-90 % [V].
- Cadencia de intersticiales consensuada en 2026: **primer anuncio tras 60-90 s**, **≥2-3 min entre intersticiales**, **4-6 por sesión**, nunca al inicio de nivel (política de Google Play). Retrasar el primero mejoró D1 un 5-8 % sin pérdida relevante [V].
- Rewarded: mayor eCPM (tier-1 15-30 USD; LatAm Android ~1,8-3,6) y 30-60 % del ingreso por ads. Ubicaciones estándar en water-sort: vasija extra, deshacer, pista, x2 monedas, cofre diario [V/S].
- eCPM Android (Appodeal Q4-2024): Norteamérica rewarded ~9,0 / intersticial ~9,6 / banner ~0,5; Europa 5,1 / 3,6; **LatAm 1,8 / 1,3 / ~0,1**; iOS LatAm 1,5-2× más. Q4 sube 20-40 % sobre Q1 [V].
- IAP: los líderes listan **0,99-99,99 USD por artículo** (Water Sort Puzzle de IEC Global, 100 M+; Hexa Sort, 10 M+) y hasta 199,99 (Magic Sort!); un clon de 50 M+ (Guru Puzzle Game) vive con un único IAP de **7,99 USD** [V, Google Play 05/10/2026].
- Precio mediano de oferta en el top-10 Puzzle US: **1,99 USD** (medio 5,69); las ofertas LiveOps más vendidas caen en 6-15 USD y casi nada ≥50 USD [V, AppMagic 2025].
- La **oferta tras fallar** es el SKU más rentable del hybridcasual puzzle: 4,99 USD en Color Block Jam (>20 % del IAP, 27 % de primeras compras); 5,99 en Pixel Flow (sort: 33 % del IAP, ~14 M de 44,5 M USD) [V, AppMagic 03/2026]. El pack de 1,99 capta ~40 % de primeras compras.
- "Quitar anuncios" rara vez entra al top de ventas; lo superan los tickets para saltar anuncios y el pase de temporada (estándar desde mitad de 2025, 6,99-9,99 USD). Alcancía 1,99-4,99 USD [V].
- Benchmarks: ARPDAU casual ads-only 0,01-0,05; casual/puzzle híbrido 0,03-0,10; hybridcasual líder 0,15-0,50 USD. Conversión diaria a pagador >0,5 % ya es buena fuera de RPG/estrategia [V].
- CPI 2025-26: puzzle ~3,00 iOS / 2,00 Android; casual Android US 1,50-3,50; **LatAm 0,14-0,60** (Adjust: 0,14 medio, +40 %). ROAS D30 casual 47 % iOS / 15 % Android [V]. UA pagada solo cierra en LatAm con IPM alto; en US exige ARPDAU ≥0,08.
- Mediación: MAX, LevelPlay y AdMob >90 % del mercado; AppLovin 36 % y AdMob 29 % del ingreso publicitario de juegos [V/S]. Android-first sin Unity: AdMob mediation con bidding o MAX.
- Cumplimiento: CMP certificado + IAB TCF en EEE/UK/Suiza (UMP SDK lo cubre); ATT iOS opt-in gaming ~38-39 %; **no declarar público infantil** en Play Console salvo aceptar solo SDKs auto-certificados [V]. Comisiones: Google Play 15 % hasta 1 M USD/año; Apple SBP 15 % con proceeds ≤1 M [V].
- Propuesta: 15 SKU (0,99-99,99 USD) con oferta de rescate 4,99, alcancía 2,99, sin-anuncios 4,99 (test 6,99), pase 6,99; intersticial cada 3 niveles (6-20) y cada 2 (21+) con tope 6/sesión; 6 ubicaciones rewarded. Ingreso estimado por 1.000 DAU: **~23 USD/día LatAm-heavy vs ~76 USD/día US-heavy** [E].

## 1. Anuncios: cadencia, ubicaciones y formatos

### 1.1 Intersticiales

| Parámetro | Referencia probada | Fuente |
|---|---|---|
| Primer intersticial | 60-90 s de juego (≈ tras nivel 3-5 en water-sort); "luna de miel" sin anuncios en Color Block Jam | Playio/AdReact 2026 [V]; Gamesforum 2025 [V] |
| Separación mínima | 2-3 min (4 min en mid-core) | Playio/AdReact [V] |
| Tope por sesión | 4-6 impresiones | Playio/AdReact [V] |
| Tope diario (casual/híbrido) | 5-7 por usuario/día, empezando bajo y subiendo con test de 7-14 días | CAS.AI [V] |
| Práctica observada en el género | Hexa Sort: intersticial tras cada nivel + banner inferior [S]; guías de desarrollo water-sort: cada 2 niveles completados [S] | Gamigion/MAF; Capermint |
| Coste de pasarse | +6-7 % de churn por intersticial disruptivo; 52 % abandonaría tras anuncios repetidos (Deloitte + AdMob, 7.000 jugadores, 2025) | Playio [V] |
| Política Google Play | Prohibidos los intersticiales inesperados, al inicio de nivel/segmento, antes de la pantalla de carga; deben poder cerrarse a los 15 s | Google Play Ads policy [V] |

### 1.2 Rewarded (recompensados)

- 9 de cada 10 jugadores interactúa con rewarded; quienes lo hacen retienen 2-7× más y compran 4× más IAP (Unity, vía Mistplay) [V]. Cadencia: 3-4 por usuario/día como formato principal en casual/híbrido, con precarga obligatoria [V, CAS.AI].
- Ubicaciones estándar del género (síntesis de teardown y fichas de tienda): (1) **+1 recipiente** (la más usada y la que las reseñas de Water Sort Puzzle citan como bloqueante cuando falla) [V, reseñas Play]; (2) deshacer extra tras 3 gratis; (3) pista; (4) x2 monedas al completar nivel; (5) cofre/ruleta diaria; (6) continuar/revivir en modos con vidas o tiempo; (7) alquiler temporal de un slot (Coin Sort "Rent-a-Slot", 30 s de anuncio por ~1 min de slot) [V, Knitout].

### 1.3 Banner, app open y nativos

- Banner: eCPM tier-1 0,5-1,5 USD; LatAm ~0,10 [V]; CAS.AI solo lo recomienda con sesiones ≥5-7 min. Probar banner adaptativo vs ninguno midiendo D1/D7.
- App open: AdMob aconseja mostrarlo desde la pantalla de carga en arranque frío, nunca en el primer uso, y añadirlo al final [V]; pico US 10,51 USD (03/2024) [V].
- Nativo/inmersivo: sin benchmarks neutrales; CAS.AI estima +4-5 % de ingreso [V].

### 1.4 eCPM de referencia (USD, por 1.000 impresiones)

| Región / formato | Rewarded Android | Rewarded iOS | Intersticial Android | Intersticial iOS | Banner |
|---|---|---|---|---|---|
| Norteamérica (Appodeal Q4-24) | ~9,0 | ~13,6 | ~9,6 | ~10,4 | 0,4-0,5 |
| US solo juegos (Tenjin×CAS Q2-24) | 30,25 | 24,39 | 16,15 | 23,17 | 1,18 / 0,99 |
| Europa (Appodeal Q4-24) | ~5,1 | ~8,9 | ~3,6 | ~5,3 | n/d |
| LatAm (Appodeal Q4-24) | ~1,8 | ~3,4 | ~1,3 (1,90 en informe 2025) | ~2,2 (3,75) | ~0,10 |
| Brasil (varios, 2024-25) | 3,63 [S] | n/d | 1,69-3,67 [S] | n/d | n/d |
| México (Appodeal dic-24) | n/d | n/d | 1,75 [S] | n/d | n/d |
| Rangos tier-1 (Playwire 2025) | 15-30 | 15-30 | 5-8 (vídeo 12-25, AdReact 2026) | 5-8 | 0,5-1,5 |
| Medias TopOn H1-25 (todos los formatos) | EU/NA 8,90 · LatAm 2,18 · India 0,10 | | | | |

España y resto de hispanohablantes europeos: no encontré desglose por país; **[E]** usar 60-80 % de la media europea (intersticial Android ~2,5-3,5; rewarded ~3,5-5). Estacionalidad: Q4 +20-40 % vs Q1; US iOS rewarded cae de ~18 USD en diciembre a 11-12,5 a mediados de enero [V, Playio].

### 1.5 Reparto de ingresos ads vs IAP en el género

| Juego / segmento | Ads | IAP | Fuente |
|---|---|---|---|
| Hexa Sort (hybridcasual sort) | ~70 % | ~30 % | Gamigion/MAF [S] |
| Magic Sort! (water-sort, 30 días 2025) | 20,5 % | 79,5 % | Braberg [V] |
| Hybridcasual lifestyle & puzzle | 41 % | 59 % | Sensor Tower SoG 2026 [V] |
| Casual medio / puzzle lógico | 50 % / 30 % | 50 % / 70 % | CAS.AI [V] |
| Mercados emergentes (IN, ID, BR) vs maduros (US, CA, JP, KR) | 55-70 % / 10-23 % | 30-45 % / 77-90 % | Sensor Tower [V] |

Lectura: con audiencia LatAm el juego será 65-80 % ads; con audiencia US puede ser 50/50 si la oferta tras fallar y la vasija extra de pago funcionan.

## 2. Plataformas de mediación y cumplimiento

| Plataforma | Cuándo elegirla | Datos |
|---|---|---|
| Google AdMob mediation (bidding) | Android-first, equipo pequeño, ya en ecosistema Google; UMP integrado | 25 % del ingreso Android (Tenjin Q2-26); 29 % global [V] |
| AppLovin MAX | Máxima densidad de demanda, campañas ROAS de AppLovin; 73 % de los top descargados [S] | 44 % del ingreso iOS, 23 % Android (Tenjin) [V] |
| Unity LevelPlay | Motor Unity, soporte mid-tier mejor; 25 % de los top grossing [S] | 15+ redes, bidding híbrido [V, AdReact 05/2026] |

Redes a incluir vía bidding: AdMob, AppLovin, Unity Ads, Mintegral, Pangle, Meta Audience Network, Liftoff/Vungle, Chartboost, InMobi, DT Exchange; la mediación suele añadir 20-40 % de ingreso frente a una sola red [S].

Requisitos:
- **GDPR/UMP**: CMP certificado por Google + IAB TCF para anuncios personalizados en EEE/UK (16/01/2024) y Suiza (31/07/2024); sin consentimiento solo "limited ads". UMP SDK es la CMP gratuita de Google; en iOS mostrar GDPR y luego ATT [V].
- **ATT (iOS)**: opt-in gaming 37,9 % (Q1-25) → ~39 % (Q1-26); US ~32 %, Brasil 51,4 % [V, Adjust vía resúmenes].
- **Familias/COPPA**: si el público objetivo incluye niños, solo SDKs auto-certificados (AdMob bloquea el resto), pantalla de edad neutral en audiencia mixta y flags `setTagForChildDirectedTreatment` / `MAX_AD_CONTENT_RATING_G`; el programa de auto-certificación cerró inscripciones el 31/10/2024 [V]. Recomendación: rating "Everyone" pero público objetivo 13+, sin "Designed for Families".
- **Comisiones**: Google Play 15 % hasta 1 M USD/año, 30 % después, suscripciones 15 %; nueva estructura (AU/EEE/JP/UK/US) para instalaciones nuevas 10 % + 5 % billing fee [V]. Apple SBP 15 % si proceeds ≤1 M USD el año anterior; nuevos desarrolladores califican automáticamente; cuentas asociadas (>50 % control) se suman [V].
- **Impuestos**: Google remite IVA en Brasil, México, Chile y Colombia; Brasil retiene IRRF + CIDE a desarrolladores pagados en moneda ≠ BRL (≈25 % según terceros [S]); IVA MX 16 %, AR/CL/CO/PE 18-22 % [V/S]. El ingreso de redes publicitarias llega neto, sin comisión de tienda.

## 3. IAP: catálogo y precios del género

### 3.1 Fichas de Google Play (consultadas 05/10/2026, US)

| Juego | Desarrollador | Descargas | Reseñas / nota | Rango IAP por artículo |
|---|---|---|---|---|
| Water Sort Puzzle | IEC Global Pty Ltd | 100 M+ | 1,34 M / 4,6 | 0,99 - 99,99 USD; evento "hasta 80 % off en bundle" |
| Water Sort Puzzle - Sort Color | Guru Puzzle Game | 50 M+ | 610 K / 4,8 | 7,99 USD (único artículo) |
| Magic Sort! | Grand Games | 10 M+ | 561 K | 0,99 - 199,99 USD |
| Hexa Sort | GameBrain | 10 M+ | 460 K | 0,99 - 99,99 USD |

### 3.2 Precios observados por tipo de SKU

| Tipo | Rango observado | Ejemplos verificados |
|---|---|---|
| Pack de monedas pequeño | 1,99 USD (mediana del género) | Color Block Jam, Screwdom, All in Hole "coin pack 1" 1,99 [V]; 40 % de primeras compras en CBJ [V] |
| Escalera de monedas | 1,99 → 7,99 → 14,99 → 19,99 (CBJ); 1,99 / 6,99 / 7,99 / 9,99 / 19,99 (All in Hole); máx. 99,99-199,99 en listados | AppMagic [V]; Play [V] |
| Oferta tras fallar | 4,99-5,99 USD | CBJ 4,99 (>20 % IAP); Pixel Flow 5,99 (33 %); Screwdom "keep up" 4,99 (14,5 %); Coin Sort 19,4 % del IAP [V] |
| Starter / beginner bundle | 2,99-9,99 USD | Screwdom "beginner's bundle" 9,99 [V]; guías: 1-3 USD [S] |
| Quitar anuncios | 4,99-7,99 USD | Coin Sort 6,99 [V]; Guru 7,99 [V]; en hybridcasual rara vez top-seller [V] |
| Tickets saltar anuncio | 2,99 / 6,49 / 14,99 / 17,99 | Mob Control [V]; Coin Sort "Skip-It pass" desde 1,99 [V] |
| Alcancía | 1,99-4,99 USD | Hexa Sort 4,99 (3.er IAP más popular); Gardenscapes 2,99 [V] |
| Pase de temporada | 4,99-9,99 USD | Screwdom 6,99; Block Blast "Block Pass" 9,99; Gardenscapes 4,99 [V] |
| Suscripción VIP sin anuncios | 1,99-7,99 USD/semana; 5,99-9,99 USD/mes | Hunter Assassin 6,99/sem (supera a "quitar anuncios") [V]; Wheel of Fortune 9,99/mes [V] |
| Ofertas LiveOps | 6-15 USD (grueso de ingresos top puzzle) | AppMagic 2025 [V] |

Conversión: puzzle casual >0,5 % de conversión diaria ya es bueno; RPG/estrategia 1,5 % [V]. El 32 % de pagadores de hybridcasual en App Store US supera 100 USD de ARPPU al día 90 (vs 22 % en 2024) [V]. Store conversion (visita→instalación) puzzle: 2 % iOS / 16,7 % Google Play [V].

### 3.3 Precios localizados

Google Play auto-convierte por tipo de cambio sin ajustar poder adquisitivo (brecha vs PPP 30-60 % en BR/MX); las plantillas de precios desaparecieron el 27/10/2025 y un override manual congela el país [V]. Niveles PricePush: CO/MX/BR/CL ~50 % del ancla US; AR en USD; ES ~70 %; DE/FR/IT/UK ~100 % [V].

| SKU USD | COP [E] | MXN [E] | BRL [E] | PEN [E] | ARS | EUR (DE/FR) [E] | EUR (ES) |
|---|---|---|---|---|---|---|---|
| 0,99 | 1.900 | 9 | 2,49 | 1,90 | equiv. 0,49 USD | 0,99 | 0,99 (tienda no diferencia) |
| 1,99 | 3.900 | 19 | 4,99 | 3,90 | equiv. 0,99 USD | 1,99 | 1,99 |
| 4,99 | 9.900 | 45 | 12,90 | 9,90 | equiv. 2,49 USD | 4,49-4,99 | 4,49-4,99 |
| 9,99 | 18.900 | 89 | 24,90 | 18,90 | equiv. 4,99 USD | 8,99-9,99 | 8,99-9,99 |
| 19,99 | 36.992 (PricePush) | 172,50 (PricePush) | 49,49 (PricePush) | 37,90 | equiv. 9,99 USD | 16,99 (PricePush) | 11,89 (PricePush) |

Columnas [E]: ratio PricePush de 19,99 USD (≈50 % PPP) con redondeo psicológico; revisar FX trimestralmente. España: igualar Eurozona pese al 70 % de PricePush.

## 4. Benchmarks de negocio

| Métrica | Valor | Fuente |
|---|---|---|
| ARPDAU casual ads-only / hypercasual / casual-puzzle híbrido / hybridcasual líder | 0,01-0,05 / 0,03-0,08 / 0,03-0,10 / 0,15-0,50 USD | gamegrowthadvisor, Juego Studio [V]; Coin Sort 0,42 (4,2× media puzzle) [V] |
| Retención mediana mercado D1 / D7 / D30 | ~22 % / 3,4-3,9 % / 0,7-0,8 %; top cuartil D1 25-33 %, D7 7-8 % | GameAnalytics vía GGA [V] |
| Retención hybridcasual líder | D7 ~20 %, D30 ~10 % (Color Block Jam) | Gamesforum, GGA [V] |
| Hypercasual | D1 <25 %, D7 <8 %; CPI 1,50 Android / 2,50 iOS vs LTV ~2,80 USD | Gamesforum 2025 [V] |
| CPI puzzle | ~3,00 iOS / ~2,00 Android (global); US Android casual 1,50-3,50; Europa Occ. 0,60-1,50; LatAm/SEA 0,15-0,60 | FoxData/Business of Apps, GGA [V] |
| CPI Adjust 2026 | Global 0,56 USD (+30 %); NA 1,68; Europa 0,53; LatAm 0,14 (+40 %); hybridcasual Android 0,95 (desde 0,54) | FoxData [V] |
| CPI casual Liftoff (feb-24→feb-25) | iOS 1,41 / Android 0,14; ROAS D30 casual 47 % iOS / 15 % Android | Liftoff [V] |
| LTV D30 / D90 water-sort | No publicado. [E] ads-only LatAm: 0,02 USD ARPDAU × ~6 días activos en 30 d ≈ 0,12 USD; US-heavy híbrido: 0,08 × ~8 ≈ 0,65 USD D30 y ~0,9-1,1 USD D90 | Cálculo propio sobre ARPDAU y retención medianas |
| Regla de viabilidad UA | CPI ≤ 30-70 % del LTV proyectado; LTV/CPI ≥1,5× | GGA, FoxData [V] |

Implicación: con LTV D90 ~0,15-0,25 USD en LatAm la UA pagada solo cierra con CPI <0,15 USD; en US necesita LTV D90 ≥1 USD (economía híbrida con oferta tras fallar). Orgánico, ASO y cross-promo deben ser el motor inicial.

## Implicaciones para Amazon Fill

### Catálogo IAP propuesto (USD, ancla US; LatAm ≈50 %)

| SKU | Precio | Contenido | Nota |
|---|---|---|---|
| `af_coins_1` "Puñado de semillas" | 0,99 | 450 monedas | Entrada ultra-baja para LatAm; mediana del género es 1,99 |
| `af_coins_2` | 1,99 | 1.000 monedas | SKU gancho de primera compra (≈40 % de primeras compras en CBJ) |
| `af_coins_3` | 4,99 | 2.800 (+12 %) | |
| `af_coins_4` | 9,99 | 6.000 (+20 %) | |
| `af_coins_5` | 19,99 | 13.000 (+30 %) | |
| `af_coins_6` | 49,99 | 36.000 (+44 %) | |
| `af_coins_7` | 99,99 | 80.000 (+60 %) | Techo igual al de los líderes |
| `af_rescue` "Oferta de rescate" | 4,99 (test 5,99) | +1 vasija permanente para el nivel, 5 deshacer, 800 monedas | Se dispara sin movimientos válidos; máx. 1/día; en LatAm mostrar antes la alternativa rewarded |
| `af_starter` "Kit del explorador" | 2,99 | 1.500 monedas, 3 vasijas, 5 deshacer, 3 pistas (valor ~7 USD) | Una vez, visible niveles 8-30, 48 h de temporizador |
| `af_noads` "Río sin interrupciones" | 4,99 (test 6,99) | Quita intersticial, banner y app open; conserva rewarded; +300 monedas | Guru 7,99 y Coin Sort 6,99 marcan techo |
| `af_noads_bundle` | 9,99 | Sin anuncios + 3.000 monedas + 5 vasijas | Variante para quien ya rechazó 4,99 |
| `af_piggy` "Tinaja" | 2,99 (test 4,99) | Alcancía: +15 monedas/nivel, tope 3.000; compra solo llena | Introducir tras nivel 20 |
| `af_pass` "Pase Amazonas" | 6,99 | Pase de 14 días: recompensas por niveles + fondo exclusivo de país + 2 tickets saltar anuncio/día | Estándar hybridcasual; suscripción VIP semanal (3,99) como test posterior |
| `af_skip_tickets` | 2,99 / 6,49 | 10 / 30 tickets para saltar intersticial | Supera a "quitar anuncios" en hybridcasual |
| `af_skin_pais` | 1,99 | Pack cosmético (vasija + fondo de Colombia/Brasil/Perú/Ecuador/Bolivia/Venezuela) | Diferencial temático, rotación mensual |

### Reglas de anuncios por rango de nivel

| Niveles | Intersticial | Rewarded | Banner / App open |
|---|---|---|---|
| 1-5 | Ninguno (luna de miel) | Oculto hasta nivel 4; desde nivel 4 "+1 vasija" visible | Ninguno |
| 6-20 | Cada 3 niveles completados, solo en pantalla de victoria tras la animación de teñido; ≥90 s desde inicio de sesión y ≥150 s desde el último; tope 4/sesión | +1 vasija, deshacer extra (tras 3 gratis), pista, x2 monedas | Banner adaptativo A/B (50 % cohortes) solo Android |
| 21-100 | Cada 2 niveles; ≥120 s entre intersticiales; tope 6/sesión; también en reintento tras fallo con 2 s de delay | + cofre diario, ruleta | App open solo en retorno caliente (>60 s fuera), nunca a <120 s de un intersticial, desactivado para pagadores |
| 101+ | Cada 2 niveles o cada nivel si pasaron ≥180 s; tope 6/sesión, 12/día | + alquiler temporal de slot (30 s por 90 s) | Igual; probar MREC en victoria vs banner |
| Cualquier nivel | Suprimir 24 h tras cualquier IAP; nunca al inicio de nivel ni tras tocar "ver anuncio"/"comprar"; cierre a 15 s | Tope 5/día/usuario, cooldown 60-90 s, precarga siempre | |

Segmentación: en geos tier-3 (LatAm, excepto Chile/Uruguay) el fallo muestra primero rewarded "+1 vasija" y la oferta de rescate solo al segundo fallo; en tier-1 la oferta de pago va primero. Q4: subir temporalmente el tope de intersticiales un escalón si D1 no cae >1 pp.

### Estimación de ingresos por 1.000 DAU [E]

Supuestos comunes: 3,0 intersticiales/DAU, 1,2 rewarded/DAU, 25 banners/DAU, 0,6 app open/DAU; 85 % Android. Pagador diario y ticket medio según geo. Ingreso ads neto de red (sin comisión de tienda); IAP neto 15 %.

| Escenario | Mix | eCPM blend (int / rew / banner / open) | Ads USD/día | IAP USD/día (bruto → neto) | Total neto/día | ARPDAU |
|---|---|---|---|---|---|---|
| LatAm-heavy | 80 % LatAm, 15 % US/EU, 5 % resto | 2,8 / 4,3 / 0,18 / 1,7 | ≈19 | 0,15 % × 3,0 USD = 4,5 → 3,8 | **≈23 USD** (~690 USD/mes) | 0,023 |
| US-heavy | 70 % US, 20 % Europa, 10 % resto | 8,2 / 14,4 / 0,51 / 5,0 | ≈57 | 0,40 % × 5,5 USD = 22 → 18,7 | **≈76 USD** (~2.280 USD/mes) | 0,076 |

Sensibilidad: Q4 +20-40 % en ads; si la oferta de rescate alcanza el 25 % del IAP como en Rollic, el IAP US-heavy puede duplicarse (ARPDAU ~0,10-0,12). Umbral de equipo: 50 K DAU LatAm-heavy ≈ 35 K USD/mes; 50 K DAU US-heavy ≈ 115 K USD/mes.

### Otras decisiones concretas

1. Stack: AdMob mediation con bidding (AdMob, AppLovin, Unity Ads, Mintegral, Pangle, Meta, Liftoff) + UMP SDK; migrar a MAX si >60 % del ingreso es tier-1 y habrá UA con AppLovin.
2. Play Console: público objetivo 13+, rating "Everyone", sin Families; nada de dinero real (los clones "earn money" son riesgo de política).
3. Restaurar compras entre dispositivos (Play Games sign-in) desde el día 1 y permitir pagar "+1 vasija" con monedas cuando no haya fill de rewarded: son las dos quejas más repetidas en las reseñas del líder.
4. Precios por país desde el lanzamiento; revisar ARS mensualmente y CO/MX/BR/PE trimestralmente.

## Preguntas abiertas

- Reparto exacto ads/IAP de Water Sort Puzzle (IEC Global) y de Magic Sort! por geo: solo hay estimaciones de terceros; requiere acceso a Sensor Tower/AppMagic.
- Catálogo IAP detallado (nombre y precio de cada SKU) de los 4 líderes: Google Play solo publica el rango; se necesita captura in-game o AppMagic "IAP Revenue Distribution".
- Precio exacto de "No Ads" en Hexa Sort y conversión real de "quitar anuncios" en water-sort (no publicado en ninguna fuente localizada).
- eCPM 2025-26 por país para Colombia, Perú, Argentina y España: Appodeal publica los gráficos como imágenes; solo verifiqué LatAm agregado, Brasil y México.
- Retención withholding Brasil (25 %) y tratamiento fiscal en Colombia para un desarrollador colombiano cobrando de Google Ireland/Google Asia Pacific: confirmar con contador.
- Decisión del dueño: ¿monetizar primero como ads-heavy (tipo Hexa Sort) o apostar desde el inicio por la oferta de rescate/vidas (tipo Magic Sort/Pixel Flow), que exige una curva de dificultad con fallos deliberados?
- ¿Modo con vidas/temporizador? Sin presión de fallo, la oferta de rescate pierde la mitad de su potencia (Happy Screw Trip: revive pack 5 % del IAP).

## Fuentes

- https://blog.playio.co/mobile-game-ecpm-benchmarks-2026
- https://blog.playio.co/interstitial-ads-mobile-games
- https://cas.ai/blog/hybrid-monetization-in-mobile-games-a-practical-guide/
- https://appodeal.com/wp-content/uploads/2025/03/Appodeal-The-Latest-eCPM-Report-2025.pdf
- https://tenjin.com/blog/ad-mon-gaming-2026/
- https://gamedevreports.substack.com/p/sensor-tower-mobile-game-ad-monetization
- https://gamedevreports.substack.com/p/sensor-tower-state-of-gaming-2026
- https://gamegrowthadvisor.com/blog/2026-03-17-mobile-game-kpis-benchmarks-2026/
- https://gamegrowthadvisor.com/blog/2026-04-16-hybrid-casual-game-design-strategy-2026/
- https://felixbraberg.substack.com/p/the-monetization-engine-behind-magic
- https://appmagic.rocks/blog/fail-offers
- https://investgame.net/wp-content/uploads/2025/11/EN_Monetization_Report_2025.pdf
- https://investgame.net/wp-content/uploads/2025/07/Gamesforum-Intelligence-Hypercasual-Gaming-Report.pdf
- https://www.knitout.net/articles/hybrid-casual-monetization-coin-sort-revenue-blueprint.html
- https://business.mistplay.com/resources/piggy-banks-in-games
- https://business.mistplay.com/resources/rewarded-ads-stats/
- https://business.mistplay.com/resources/app-conversion-rates/
- https://www.blog.udonis.co/mobile-marketing/mobile-games/in-app-purchases
- https://www.blog.udonis.co/mobile-marketing/mobile-games/subscription-monetization
- https://www.juegostudio.com/blog/arpdau-benchmarks-by-game-genre
- https://foxdata.com/en/blogs/2026-mobile-game-user-acquisition-cost-benchmarks-how-much-should-you-spend/
- https://liftoff.ai/2025-casual-gaming-apps-report/
- https://sensortower.com/blog/2025-q4-ios-top-5-puzzle-games-units-us-64bc1644e1714cfff1e615fc
- https://adreact.com/blog/unity-ads-vs-applovin-max-mediation-comparison/
- https://www.globalgamesforum.com/news/max-vs-levelplay-9-facts-about-the-mediation-space-in-2025
- https://www.gamigion.com/hexa-sort-segment-growth-challenges-and-lessons/
- https://www.adjust.com/blog/att-opt-in-rates-2025
- https://support.google.com/admob/answer/13554116?hl=en-gb
- https://support.google.com/admob/answer/6223431?hl=en
- https://support.google.com/googleplay/android-developer/answer/12955712
- https://support.google.com/googleplay/android-developer/answer/9857753?hl=en
- https://support.google.com/googleplay/android-developer/answer/112622?hl=en
- https://support.google.com/googleplay/android-developer/answer/6334373?hl=en
- https://support.google.com/googleplay/android-developer/answer/9384608?hl=en
- https://developer.apple.com/app-store/small-business-program/
- https://pricepush.app/blog/app-price-localization-cheat-sheet
- https://pricepush.app/blog/google-play-iap-pricing-by-country
- https://play.google.com/store/apps/details?id=com.gma.water.sort.puzzle&hl=en_US&gl=US
- https://play.google.com/store/apps/details?id=water.sort.puzzle.game.color.sorting.free&hl=en_US&gl=US
- https://play.google.com/store/apps/details?id=com.grandgames.magicsort&hl=en_US&gl=US
- https://play.google.com/store/apps/details?id=com.gamebrain.hexasort&hl=en_US&gl=US
- https://appfollow.io/blog/mobile-game-ads-formats-monetization
- https://www.capermint.com/blog/water-sort-puzzle-bottle-game/

## Complemento 1: Economía unificada de la moneda blanda (fuentes, sumideros, valor en USD, calendario y racha con cifras)

Fecha: 2026-10-05. Misma convención [V]/[E]/[S]; además **[D]** = cifra ya fijada en otro documento interno (03, 05, 08, 09) que aquí se respeta, y **[R]** = reporte de jugadores. Método: el cupo de búsquedas de la sesión estaba agotado, así que se usaron 8 consultas vía Brave Search y ~25 lecturas directas (App Store US, iTunes Search API, Naavik, Deconstructor of Fun, GameRefinery, Mistplay, Unity, Dream Games). Ningún juego del género publica su tabla de economía: las cifras de los líderes son teardowns o reportes de jugadores, y el modelo diario de §4-5 es un diseño propio para calibrar en soft launch, no un benchmark.

### 1. Una sola escala: la semilla

| Regla | Valor | Origen |
|---|---|---|
| Ancla de precio | **1.000 semillas = 1,99 USD → 1 semilla ≈ 0,002 USD (0,2 ¢)** | Pack mínimo de Magic Sort (1.000 Coins 1,99) [V, App Store]; Royal Match usa la misma ancla; ya fijado en 04/08/09 [D] |
| Unidad de ingreso | **20 semillas por victoria normal** (≈0,04 USD) | 03 [D]; Magic Sort 10-20 [R]; Hexa Sort ~5 [R]; Royal Match ~50 con continue de 900 [S] |
| Bonus por dificultad | Difícil **60** (×3) · Muy difícil **80** (×4) | Royal Match añade +100/+150 sobre una base de ~50 [S, r/RoyalMatch]; proporción copiada, cifra [E] |
| Rescate en bloqueo ("Continuar") | **900** (≈1,79 USD) | 08/09 [D]; Magic Sort 900 [V]; Royal Match 900 por 5 movimientos ≈ 2 USD, el más caro del género, y escala a 1.500 por 10 [V Naavik / S Reddit] |
| Primer rescate del día | **450** (≈0,90 USD); Continuar no se ofrece antes del nivel 31 | [E]; recoge la banda 300-600 propuesta en 09 |

Cómo conviven **Cuia extra 150** (03) y **Continuar 900** (08) sin canibalizarse: la Cuia extra es un comodín *planificado*, comprable solo mientras queda ≥1 vertido legal y máximo una por semillas por nivel (la primera del nivel va por anuncio [D 03]). Al quedarse sin vertidos, la Cuia desaparece de la barra y el modal de bloqueo ofrece solo tres salidas: anuncio "una cuia más" (1/día; primero en LatAm, 04), **Continuar 900/450** o reinicio gratis determinista. El sobreprecio ×6 del rescate es el patrón del género; la diferencia con Magic Sort (sin ruta gratuita, 09) es el anuncio diario y el descuento del primer rescate. El SKU de rescate de 04 (4,99 USD = 800 semillas + continuar del nivel, valor 900, + 5 Pororoca, 50) suma 1.750 semillas-equivalentes ≈ 3,48 USD nominales: vende el momento, no el valor, como la oferta tras fallar de Color Block Jam [V, §3.2].

Lección que fija qué puede regalar una racha: Dream Games probó "Super Duke" (+3 movimientos tras 5 victorias seguidas) y lo retiró porque canibalizaba la compra de movimientos extra; Butler's Gift (hasta 3 boosters de inicio tras 3 victorias) y Super Light Ball (10 victorias seguidas → light ball ×2, +1,5× de ingresos) sí funcionaron [V, Naavik]. Regla: **ninguna fuente gratuita entrega Cuias extra ni Continuar como premio fijo; la racha paga semillas y comodines de preparación**.

### 2. Sumideros: los 6 comodines y la alcancía en la misma escala

| Sumidero | Semillas | USD equiv. | Niveles normales (20) para pagarlo | Niveles de ingreso efectivo (~75/nivel, §4) |
|---|---|---|---|---|
| Pororoca (+3 deshacer) | 30 | 0,06 | 1,5 | 0,4 |
| Guacamayo guía (pista) | 100 | 0,20 | 5 | 1,3 |
| Ojo de Jaguar (revelar ocultos) | 120 | 0,24 | 6 | 1,6 |
| Cuia extra (pre-bloqueo) | 150 | 0,30 | 7,5 | 2 |
| Aguacero (barajar) | 200 | 0,40 | 10 | 2,7 |
| Canoa rápida (saltar, condicionada) | 300 | 0,60 | 15 | 4 |
| **Continuar** (bloqueo) | **900 / 450** | 1,79 / 0,90 | 45 / 22 | **12 / 6** |
| Alcancía "Tinaja" (04) | +15 por victoria, tope 3.000 | 3.000 ≈ 5,97 nominal por 2,99 (−50 %) | 200 victorias ≈ 17 días del jugador medio | Rango del género 1,99-4,99; Hexa Sort la abre en el nivel 7 [V, Mistplay] |

El ratio "niveles de ingreso efectivo por rescate" queda en 6-12, dentro del 10-15:1 que propone 09 y lejos del 45:1 de Magic Sort.

### 3. Fuentes: valor de cada casilla, tramo y segmento

**Calendario "Reto del río", 30 casillas [E].** Reclamo secuencial sin reinicio al faltar un día (el patrón de Unity Daily Rewards: la casilla no reclamada sigue disponible al día siguiente y hay bono si se reclaman ≥28 de 31 [V]); exige ≥1 nivel jugado ese día. Total mensual **3.000 semillas (≈5,97 USD = 3,3 rescates) + 14 comodines**; el 53 % de las semillas va en los hitos 7/14/21/30 (modelo "zigzag con hitos" descrito por Mistplay y Game Developer [V]).

| Día | Premio | Día | Premio | Día | Premio |
|---|---|---|---|---|---|
| 1 | 50 | 11 | 100 | 21 | **400 + Canasta** |
| 2 | 3 Pororoca | 12 | 1 Guacamayo | 22 | 100 |
| 3 | 50 | 13 | 100 | 23 | 1 Cuia extra |
| 4 | 1 Guacamayo | 14 | **300 + 1 Cuia extra** | 24 | 100 |
| 5 | 100 | 15 | 50 | 25 | 1 Guacamayo |
| 6 | 1 Cuia extra | 16 | 100 | 26 | 150 |
| 7 | **250 + Canasta** | 17 | 3 Pororoca | 27 | 3 Pororoca |
| 8 | 50 | 18 | 100 | 28 | 150 |
| 9 | 50 | 19 | 3 Pororoca | 29 | 1 Ojo de Jaguar |
| 10 | 3 Pororoca | 20 | 150 | 30 | **650 + Canasta grande + marco de fondo-país** |

**Racha de victorias "Crecida"** (se reinicia al fallar; 1 escudo diario perdona un fallo [D 05/09]; desbloqueo nivel 25): 3 victorias → **300 semillas** [D 03/08]; 6 → Canasta (comodín, sin semillas); 10 → **300 semillas + "río crecido"** (Cosecha doble sin anuncio mientras dure la racha; se pierde al fallar: es el equivalente económico del Super Light Ball). Tope diario 600 semillas. Las rachas están en el 45 % de los top puzzle [V, GameRefinery oct-2024]. **Racha de días** (05): +10 % de semillas por victoria cada 3 días consecutivos, tope +30 %, hoja de protección 1/semana.

**Rueda del río** (1 giro gratis + 1 por anuncio; desbloqueo nivel 15) — 8 segmentos y pesos [E]: 20 semillas 30 % · 50 semillas 25 % · 3 Pororoca 15 % · 100 semillas 12 % · 1 Guacamayo 8 % · 1 Cuia extra 6 % · 300 semillas 3 % · 1.000 semillas 1 %. Valor esperado por giro **≈50 semillas + ≈21 en comodines (≈0,14 USD)**, frente a 0,002-0,004 USD que paga una vista rewarded en LatAm (eCPM 1,8-3,6, §1.4): cara en semillas, barata en dólares, como debe ser un sumidero de anuncios. Hexa Sort vende el giro a 1,99 USD [V, App Store]; nosotros no lo vendemos.

**Canasta** (cofre cada 10 niveles): la primera (nivel 10) 50 semillas [D 05]; después **50 semillas + 1 comodín** (3 Pororoca 60 % · Guacamayo 25 % · Cuia extra 15 %; valor esperado del comodín ≈66 semillas).

### 4. Presupuesto diario del no pagador [E]

Supuestos: Difícil cada 6 niveles y Muy difícil cada 20 (09); tope 5 rewarded/día (04) repartidos en 3 Cosecha doble + 1 cuia por anuncio + 1 giro; Crecida y Canasta con las probabilidades de §3; calendario promediado (100/día).

| Fuente | Ligero (6 victorias/día) | **Medio (12)** | Intenso (25) |
|---|---|---|---|
| Victorias (20 / 60 / 80) | 165 | 330 | 685 |
| Cosecha doble (x2 por anuncio) | 20 | 100 | 150 |
| Crecida (esperado) | 150 | 240 | 500 (tope 600) |
| Calendario (promedio) | 100 | 100 | 100 |
| Rueda del río | 40 | 65 | 80 |
| Canasta | 30 | 60 | 125 |
| **Total semillas/día** | **≈505 (1,00 USD)** | **≈895 (1,78 USD)** | **≈1.640 (3,26 USD)** |

Regla memorizable: **el jugador medio gana ≈900 semillas/día = un Continuar, o seis Cuias extra**. El ingreso efectivo por victoria (≈75) es el usado en §2.

### 5. Balance por tramo y tasa objetivo de "bloqueado sin recursos" [E]

Definición operativa: jugador que falla un nivel y no tiene ni cuia por anuncio (tope agotado o sin fill) ni ≥450/900 semillas. No encontré benchmark público de tasa de fallo por nivel (Supersonic solo da guía cualitativa [V]; King y Dream no publican curvas), así que las cifras son objetivos para el embudo por nivel que 05 ya exige medir. La mitigación probada es la de Royal Match: tras varios fallos seguidos regala más movimientos [S], que en nuestro caso es "aliviar un color tras 3 fallos" (03).

| Tramo | Fallo por intento objetivo (normal / difícil / muy difícil) | Fallos/día (medio) | Gasto/día | Neto/día | Saldo al cerrar el tramo | **Bloqueados sin recursos (objetivo)** |
|---|---|---|---|---|---|---|
| 1-50 | 5 % / 25 % / 40 % | 0,8 | 150-200 (sin Continuar hasta el 31) | +500 | ≈2.000 (2 rescates) | **≤3 %** de quienes llegan al 50 |
| 51-100 | 8 % / 35 % / 50 % | 2,0 | 600-650 (0,5 Continuar/día) | +250 | ≈3.000 | **≈8 %** al 100 |
| 101-200 | 12 % / 40 % / 60 % | 3,5 | 1.300-1.450 (1,5 Continuar/día) | −450 | se agota hacia el 180-220 | **15-20 %** al 200: primer muro real; Magic Sort tiene el suyo en 70-99 y 200-300 (09) |
| 201+ | mantener la curva; escalar Continuar 900 → 1.200 solo si D30 no cae | 4+ | ≈ ingreso ×1,5 | negativo | — | 20-25 %, con rescate 4,99 máx. 1/día (04) |

Lectura para el programador de economía: el saldo es deliberadamente positivo hasta el nivel 100 (ads-first en LatAm, 04/05) y pasa a negativo en 101-200 por la cadencia de dificultad, no por subir precios (subir precios con recompensa fija es la causa n.º 1 de reseñas de 1-2 estrellas, 03). Palancas de calibración: si el saldo medio al nivel 100 supera 4.000 o los bloqueados al 200 bajan del 10 %, recortar primero la Crecida (300 → 200) y después el calendario; si al 50 los bloqueados superan el 5 %, bajar la tasa de fallo de los niveles difíciles, no el precio. Para el GDD: el primer muro de pago esperado del no pagador medio está en el **nivel 180-220 (día 15-18)**, no antes.

### 6. Verificación de los packs IAP de 04 en esta escala

| SKU (04) | Semillas | USD | ¢ por 10 semillas | Rescates (900) que compra |
|---|---|---|---|---|
| af_coins_1 | 450 | 0,99 | 2,2 | 0,5 |
| af_coins_2 | 1.000 | 1,99 | 2,0 | 1,1 |
| af_coins_3 | 2.800 | 4,99 | 1,8 | 3,1 |
| af_coins_4 | 6.000 | 9,99 | 1,7 | 6,7 |
| af_coins_5 | 13.000 | 19,99 | 1,5 | 14 |
| af_coins_6 | 36.000 | 49,99 | 1,4 | 40 |
| af_coins_7 | 80.000 | 99,99 | 1,25 | 89 |

Comparables verificados en App Store US (05/10/2026): Magic Sort 1.000/1,99 y 5.000/7,99 (1,6 ¢) [V]; Hexa Sort Coin Pack 1,99 / 7,99 / 14,99 (tamaños no publicados), Piggy Bank 4,99, Spin Wheel 1,99, Safety Net 4,99 [V]; IEC Global vende diamantes (Diamond 1-6: 1,99 / 7,99 / 16,99 / 29,99 / 54,99 / 99,99) y sus monedas blandas solo compran cosméticos [V/S]; Tripledot Get Color 1,99 / 9,99 / 17,99 con monedas también cosméticas [V/S]; Guru solo Remove Ads 7,99 [V]. Conclusión: solo Magic Sort monetiza la moneda blanda en el género; IEC, Guru y Tripledot la dejan cosmética. Nuestra escalera copia la de Magic Sort/Royal Match con la ventaja habitual por tamaño (2,2 → 1,25 ¢).

### Decisiones pendientes del complemento

1. Continuar fijo en 900 (08/09) o variable 600/900 por dificultad (09): el modelo asume 900 con primer rescate del día a 450; probar 600 en una cohorte.
2. Crecida de 300 por 3 victorias convierte la racha en el 27 % del ingreso del jugador medio (85 % en Magic Sort): confirmar que el tope diario de 600 es aceptable para el dueño del producto.
3. Alcancía a +15/victoria (04) frente a un porcentaje del ingreso efectivo (20 %): la tasa de llenado real de Hexa Sort y Gardenscapes no es pública [NV].
4. Valores del calendario de Hexa Sort y de cualquier water-sort: no publicados; la tabla de §3 es propia [E].

### Fuentes del complemento

- https://apps.apple.com/us/app/water-sort-puzzle/id1514542157 (IEC Global, lista de IAP)
- https://apps.apple.com/us/app/magic-sort/id6499209744 (Magic Sort, lista de IAP)
- https://apps.apple.com/us/app/hexa-sort/id6463127238 (Hexa Sort, lista de IAP)
- https://apps.apple.com/us/app/water-sort-puzzle-get-color/id1569776448 (Tripledot, lista de IAP)
- https://apps.apple.com/us/app/water-sort-puzzle-sort-color/id1597897013 (Guru, IAP y monedas cosméticas)
- https://itunes.apple.com/search?term=water+sort+puzzle&entity=software&country=us (identificadores de apps)
- https://naavik.co/deep-dives/royal-match/ (900 monedas ≈ 2 USD, escalera 4,35/6,65/8,95 USD, Butler's Gift)
- https://naavik.co/digest/royal-match-finding-success-through-iteration/ (Super Light Ball 10 victorias, +1,5× ingresos; Super Duke retirado)
- https://www.deconstructoroffun.com/blog/2021/3/21/royal-match-the-new-king-from-turkey (bonus levels 300-700 monedas)
- https://www.deconstructoroffun.com/blog/2024/8/5/win-streak-the-gift-that-keeps-on-giving (tramos de 3-5 victorias, preservación de racha)
- https://www.gamerefinery.com/mobile-game-market-review-october-2024/ (win streaks en 45 % del top puzzle)
- https://www.gamerefinery.com/feature-spotlight-progression-daily-rewards/ (calendarios progresivos en ~50 % del top grossing iOS)
- https://business.mistplay.com/resources/daily-login-rewards (calendarios 7/14/30, progresivo vs zigzag, 4 modelos de perdón)
- https://business.mistplay.com/resources/piggy-banks-in-games (alcancía 1,99-4,99; Hexa Sort nivel 7)
- https://www.gamedeveloper.com/business/the-science-craft-of-designing-daily-rewards----and-why-ftp-games-need-them (Puzzle & Dragons acumulativo, Warframe por tramos)
- https://docs.unity.com/en-us/services/solutions/daily-rewards (reclamo secuencial sin reinicio; bono con ≥28 de 31 días)
- https://dev.to/hiroshi_takamura_c851fe71/game-economy-balancing-how-to-tune-rewards-costs-and-progression-2ale (regla coste = ingreso diario × días objetivo)
- https://www.blog.udonis.co/mobile-marketing/mobile-games/balanced-mobile-game-economy (pinch point; topes de rewarded Archero/Hay Day)
- https://supersonic.com/learn/blog/difficulty-curves/ (sin benchmark numérico de fallo por nivel)
- https://dreamgames.helpshift.com/hc/en/3-royal-match/faq/21-how-can-i-obtain-and-use-coins/ (fuentes y usos de monedas en Royal Match)
- https://oldcynic.com/royal-match-tips-and-tricks-cheats-for-new-players (Team Battle 6.000, Lava Quest 10.000, Super Light Ball)
- https://medium.com/ironsource-levelup/design-deep-dive-02-royal-match-948f7af96f04 (bonus levels 300-450; solo resumen de búsqueda [S])
- https://www.reddit.com/r/RoyalMatch/comments/1f2sia1/post_match_extra_coins/ (bonus +100/+150 difícil/muy difícil; solo resumen [S])
- https://www.reddit.com/r/iosgaming/comments/15vpa50/royal_match_review_mobile_puzzle_game_2023/ (900 → 1.500 por 10 movimientos; solo resumen [S])
- https://playbite.com/q/what-are-the-coins-for-in-water-sort-puzzle y https://www.appgamer.com/get-color-water-sort-puzzle/answers/71191-what-can-you-use-coins-for (monedas cosméticas en IEC y Get Color; resúmenes [S])
- Documentos internos: docs/research/03-comodines-boosters.md, 05-retencion-liveops-meta.md, 08-competencia-aso-legal.md, 09-referencia-magic-sort.md
