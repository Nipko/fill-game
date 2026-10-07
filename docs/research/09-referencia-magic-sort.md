# Referencia principal: Magic Sort! (Grand Games)

**Proyecto:** Amazon Fill · **Fecha:** 2026-10-05 · **Documento:** síntesis de los frentes A (núcleo y niveles), B (economía y monetización) y C (meta, live-ops, UX, tienda, técnico).

**Convención:** **[V]** verificado con URL en Fuentes · **[E]** estimación o inferencia · **[R]** procede de reseñas de usuarios (fiable como tendencia, no como cifra) · **[NV]** una sola fuente o no confirmable. Donde los tres informes discrepan se indica qué fuente pesa más y por qué. Los sitios de guías documentan **una** variante de cada nivel; Magic Sort sirve variantes distintas por versión, plataforma y cohorte, así que los números por nivel son ilustrativos.

---

## Ficha

| Campo | Dato | Estado |
|---|---|---|
| Editor | Grand Games A.Ş. (Estambul). ~75 empleados. Financiación 3 + 30 + 70 = 103 M USD; Serie B de 70 M USD liderada por Balderton (may-2026). | [V] |
| Identificadores | Android `com.grandgames.magicsort` · iOS `id6499209744` · Título de tienda "Magic Sort! – Water Sorting Puzzle" | [V] |
| Lanzamiento | Ficha iOS: **28-may-2024**; APK Android desde jun-2024. Gamigion fecha el arranque en abril 2024 (probable soft launch). Pesa la ficha de tienda como registro primario. | [V] |
| Ingresos | IAP: 2,8 M USD (2024) → **45 M USD (2025)** → **79,4 M USD acumulados a may-2026** (InvestGame). Bruto 2025 ≈ 50 M USD = 18 % del subgénero Sort (Sensor Tower vía Naavik). >40 M USD el primer año (DoF). 65,8 M USD mar-2025→mar-2026. Pico 120 K USD/día (mar-2025). Las cifras difieren por periodo y base (IAP vs bruto), no se contradicen. | [V] |
| Descargas | Google Play "10 M+"; AndroidRank estima 25,4 M; **58 M acumuladas** (InvestGame, may-2026); 35,7 M en mar-2025→mar-2026. Grupo Grand: 73 M. Solo 593 K hasta ene-2025: el salto vino con la UA de 2025. | [V] |
| Valoración | iOS 4,7★ (669 K en EE. UU.; ~1,5 M globales). Android 4,5★ (~560 K reseñas; 72 % 5★ / 3 % 1★). Brasil y México dan la mejor nota del mundo (4,8★). | [V] |
| Tamaño | iOS **609,2 MB** (v32.92, 4-oct-2026). Android XAPK 218-234 MB (v0.0.3293). Requiere Android 8.0+. | [V] |
| Motor | **Unity** (las ofertas de empleo exigen Unity + POO); IL2CPP y atlas retina probables como causa del peso iOS. | [V] / [E] |
| Países | EE. UU. dominante (#4 Casual gratis hoy; #73 top grossing juegos US, jul-2026). Fuerte en GB/CA/DE; LatAm con BR #9 Puzzle y MX #10 Ingenio; flojo en Japón (#33). Turquía fue el primer mercado. 11 idiomas, incl. ES y PT. Reparto exacto por país no publicado. | [V] |
| Modelo | **IAP-first híbrido**: ~77-80 % IAP / 20-23 % ads (may-2025). Sin SKU "sin anuncios", sin suscripción, sin alcancía. Intersticiales desde el nivel 22. | [V] |
| Métricas | 478 K DAU (may-2025); 4,8 sesiones/día; "ARPU" mensual por DAU 8,68 USD; RPD 1,70-1,84 USD; ARPDAU 0,16 USD (Sensor Tower, abr-2025→abr-2026); D30 "muy por encima" del 8 % de referencia del género. | [V] |

---

## Por qué ganó

1. **Importó la economía del match-3 a un género que solo vivía de anuncios → capturó el dinero que nadie cobraba.** Antes de Magic Sort la categoría water-sort facturaba <2 M USD/año (DoF). Con precios idénticos a Royal Match (continue = 900 monedas; 1.000 monedas = 1,99 USD) obtuvo el **43 % del IAP del top-10 sort con el 3,4 % de su DAU** (Braberg/Sensor Tower) **[V]**.
2. **Fail state binario e instantáneo con una sola oferta ("+1 frasco") → más del 50 % del IAP sale de la pantalla de derrota.** Sin contador ni temporizador: el juego detecta que no queda vertido legal y ofrece el frasco en ese segundo; el frasco resuelve de verdad, así que la compra "cumple". 3,3 M USD de IAP en 30 días con 478 K DAU **[V: Naavik, Braberg, Gamigion]**.
3. **La racha como colateral → el jugador paga por no perder, no por ganar.** Rechazar el continue cuesta nivel + vida + racha de evento (Broom Streak, Sky Jump, multiplicador ×2→×100). Una analista de Gamigion gastó 7 € para no romper una racha cuyo premio valía menos que una continuación **[V]**.
4. **Producción "Dreamificada" → CPI de hipercasual con monetización de casual.** Arte y *juice* nivel Royal Match, tap-tap con encolado y legibilidad en pantallas pequeñas. CPI Android 0,74 → **0,50 USD**, iOS 3,15 → 2,17 USD; IPM Android 3,5 → 6,23; D1 pagado 23 → 29 % **[V: Artik]**.
5. **Cadencia industrial de contenido → el ingreso escaló con los niveles.** 25 versiones iOS entre el 21-feb y el 4-oct-2026, todas "New 100 levels!" (≈ 1 cada 9 días); evento nuevo cada ~2 meses. Gamigion atribuye el salto de 80-100 K a 120 K USD/día (mar-2025) a "actualizar rigurosamente con más y más niveles" **[V]**.
6. **Onboarding premium → hábito antes de la primera fricción.** Cero anuncios hasta el nivel 22, cero fails hasta el 18 (primer pico en el 19, ~1 h 21 min), easing en el 26, 60 niveles completables el primer día. Sesiones de 1-3 min → 4,8 sesiones/día **[V: Casual Puzzle Report, Gamigion, PocketGamer]**.
7. **UA concentrada y "pegada al producto" → escala con dos redes.** Solo AppLovin + Meta en ene-2025, 8 playables idénticos al juego, conceptos emocionales con el gato ("pide leche", "rescate", "carrera contra el tiempo"), umbrales de clics 1/3/10/20/∞ **[V: Naavik, Artik, Mobidictum]**.
8. **Meta ligero pero completo → retención sin coste de decoración.** Tres leaderboards, equipos con vidas compartidas, cartas, cofres, caldero. El CEO admite live-ops "sub-optimizados" (DoF, jun-2026): la ventana para superarles está en meta, eventos justos y onboarding, no en el core **[V]**.

---

## Deconstrucción

### Núcleo y reglas

| Elemento | Magic Sort | Estado |
|---|---|---|
| Control | Tap en frasco origen → tap en destino. Sin arrastre. Los toques se **encolan** sin esperar la animación (elogio recurrente). | [V] / [R] |
| Vertido | Solo el bloque superior contiguo del mismo color. Destino válido: vacío **o** mismo color arriba, con espacio; si no cabe todo, vierte lo que cabe. | [V] |
| Capacidad | 4 unidades estándar. Frascos de 1, 2 y 3 unidades y frascos "altos" forman parte del diseño de niveles. | [V] |
| Victoria | Todos los frascos monocolor o vacíos. Sin estrellas ni penalización por movimientos. | [V] |
| Límites | **Ningún** límite de movimientos ni tiempo en niveles estándar. | [V] |
| Escala | Frascos 3 (N1) → ~14 (N30); 10-13 en el medio juego. Colores 2 → 10 (N30) → 8-11 (N200-500). Vacíos iniciales normalmente 1; 2-3 en niveles de respiro; a veces 0 "limpios" o huecos con candado. | [V] |
| Fail state | Se pierde cuando no queda **ningún vertido legal**. Oferta inmediata "+1 frasco" por 900 monedas; rechazar = −1 vida, reinicio y pérdida de racha. | [V] |
| Dificultad adaptativa | Al fallar el N26, el segundo intento **quita un color** (Naavik). Las guías publican "Version 1/2/5" del mismo nivel; los jugadores se quejan de que "el puzle no recarga igual". | [V] |
| Generación | Plantillas + generación procedural + solver que garantiza solubilidad y fija el "target moves" (17, 26, 34, 50 en guías); variante por intento y por cohorte A/B. | [E] |

### Tipos de nivel y mecánicas

| Mecánica (nombre en guías) | Comportamiento | Nivel aprox. de introducción | Estado |
|---|---|---|---|
| Frasco de capacidad reducida (1-3 u.) | Buffer; obliga a decidir qué color "le pertenece". | ~14 | [V]. El frente C describe el N14 como "colores bloqueados bajo capas"; pesa el frente A, que cruza levelsolve y magicsort.app con niveles nombrados (frasco gris de 1 unidad). |
| Frascos cubiertos / "shell" / residuo | Columna tapada que se vuelve utilizable al recibir el color correcto; "almacén semicerrado". | 25-60 (L25, L43, L60, L100) | [V] |
| **Frasco "?" (magic bottle)** | Vacío sin color asignado: el primer color vertido lo fija; un segundo color distinto lo bloquea. Variante: contenido oculto que se revela al destapar. | **~70** (6 a la vez). Las reseñas sitúan el muro de "colores ocultos" en ~100: pesa la guía (nivel documentado); la reseña mide percepción. | [V] / [R] |
| Paneles de madera (doors) | Ocultan una segunda fase del tablero; "el puzle es cronometrar la revelación". | 150 | [V] |
| Corchos por color (corked) | Tapa con color-llave; se abre al verter ese color encima; puede llevar contador de vertidos. | 221 (6 a la vez) | [V] |
| Huecos con candado dorado | Vacíos bloqueados que se liberan a mitad de nivel. | 221 | [V] |
| Cortina de teatro | Oculta frascos extra; cae sola al haber N vacíos en la fila. | 331 | [V] |
| Caja regalo | Se abre con cualquier vertido; debajo hay un frasco multicolor; el color equivocado lo deja varado. | 400 | [V] |
| Caja fuerte con candado de color | Como corcho, pero la llave puede salir de **otra** caja: cadenas de desbloqueo. | 442 | [V] |
| Caja central con temporizador; tubos grandes y estrechos | Citados en la guía del nivel 1000 (super hard). | ~1000 | [V: una guía] |
| Portales, slime, hielo/frost, llaves (key tags), spray, wraps, star flasks | Nombrados en la App Store Story y en guías sin nivel exacto. | — | [NV] |
| Niveles de respiro "puros" (sin gimmicks, 2 vacíos) | Verificados tras tramos duros. | 24, 200, 280, 420 | [V] |

**Juicio de la crítica [V: Gamigion]:** "las mecánicas nuevas se introducen tarde y carecen de novedad". Entre el 100 y el 220 el juego vive de frascos cubiertos y tamaño variable; el primer bloqueador realmente nuevo (corchos) llega al ~221.

### Dificultad y niveles hard / super hard

- **Etiquetas en UI**: "Hard" y "Super Hard" con icono de **calavera** (grand-screen) **[V]**. Color de marco/fondo sin fuente textual; **[E]** morado y rojo oscuro por convención Royal Match.
- **Cadencia**: no publicada. Proxy: levelsolve clasifica (rating comunitario, no etiqueta del juego) 442 niveles en 15 fáciles, 61 medios, **202 hard y 164 expert**, con el tramo **43-112 íntegramente expert** **[V]**. Muros según reseñas: ~70-99 y ~200-300 **[R]**. **[E]** 1 Hard cada 5-8 niveles y 1 Super Hard cada 15-25.
- **Qué cambia**: 9-11 colores, un solo vacío real, varios frascos "?" a la vez, menos frascos ancla, bloqueadores encadenados. Percepción: "no hay estrategia, solo azar"; un usuario reporta 16 intentos en un nivel **[R]**.
- **Recompensa extra**: más monedas y avance de evento según reseñas; cifra no verificada (**[E]** ×2-×3).
- **Diente de sierra por diseño**: tramos expert largos intercalados con respiros puros **[V]**.
- **Consecuencia medida**: el ARPDAU ha caído significativamente desde el pico (Sensor Tower vía Gamigion) **[V]**; Gamigion lo atribuye a que el juego "fuerza a pagar en vez de motivar" y a que la suerte pesa más que en match-3.

### Comodines y continuar

| Ítem | Coste | Cuándo se puede usar | Estado |
|---|---|---|---|
| **+1 Bottle (continuar)** | **900 monedas** | Solo en el instante del bloqueo | [V: 5+ reseñas fechadas, Naavik, Gamigion] |
| +1 Bottle como booster pre-nivel | 900 monedas | Antes de empezar | [V: reseñas] |
| Undo ("Wizard's Gear") | Se compra con monedas; coste no confirmado | **Solo antes** del bloqueo | [NV] |
| Shuffle ("Wizard's Gear") | ~300 monedas ("herramienta barata", una reseña) | Solo antes del bloqueo | [NV] |
| Bundles de comodines | Cantidades iguales de Undo / Shuffle / +1 Bottle | Tienda | [V: Gamigion shop] |
| Vidas | 5 corazones; recarga ~25-30 min por vida; vida por rewarded (vídeo largo) y por petición al equipo | Al fallar sin pagar | [V] / [R] |
| Vidas infinitas | 15-30 min como premio; 6-48 h en bundles; "Endless Portal" 5,99 USD (**[E]** horas de vidas infinitas + monedas) | Eventos, tienda | [E] / [NV] |

Queja estructural **[V: Glotier, reseñas]**: al quedarte sin movimientos **no puedes usar Undo ni Shuffle guardados**; la única salida es pagar el continue, así que el jugador gasta boosters "a ciegas" antes del bloqueo.

### Economía de monedas

- **Ingreso por victoria**: no publicado. Reseñas: ~10-20 monedas por nivel **[R]**; **300 por racha de 3 victorias** (varias reseñas independientes); mini-evento de 10 niveles con hasta 10.000 monedas **[NV]**. La recompensa diaria "favorece vidas sobre monedas" aunque las monedas son el cuello de botella **[R]**.
- **Ratio de presión**: 900:20 ≈ **45 victorias por continue** sin racha; con racha perfecta (100 monedas/nivel efectivas) ≈ 9 niveles; rompiendo la racha cada 2 niveles, 18-45 **[E]**.
- **Precio efectivo**: 1,80 USD (1.000 / 1,99) → 1,44 (5.000 / 7,99) → 1,35 (10.000 / 14,99). **Un fail cuesta de facto 1,35-1,80 USD** **[E]**.
- **Asimetría deliberada**: la racha paga cuando el jugador va bien; el continue cobra cuando va mal **y además rompe la racha** **[V: Gamigion]**. La ruta gratuita hacia 900 está casi cerrada: el único rewarded da vidas, no monedas.
- **Sin saldo**: la "Special Offer 0,99" aparece como upsell desde la pantalla de continuar **[V: Gamigion]**; otros momentos **[E]**.

### Catálogo IAP

Verificado en App Store el 2026-10-05 **[V]**. Google Play no legible por herramienta (10 M+, 4,5★ confirmados en la página del desarrollador).

| SKU | EE. UU. | Brasil | México | Colombia | España | Contenido |
|---|---|---|---|---|---|---|
| Starter Bundle / Special Offer 0.99 | 0,99 USD | R$ 6,90 | 19 MXN | 4.900 COP | 0,99 € | Pack de entrada / oferta contextual; contenido no publicado |
| 1000 Coins | 1,99 | R$ 12,90 | 39 | 9.900 | 1,99 € | ≈ 1 continue |
| Apprentice Bundle / Special Offer 4.99 | 4,99 | R$ 29,90 | 99 | 24.900 | 5,99 € | Bundle intermedio **[E]** monedas + boosters + vidas infinitas |
| Endless Portal | 5,99 | R$ 39,90 | 129 | 29.900 | 6,99 € | **[E]** tiempo de vidas infinitas; **[NV]** si incluye niveles sin fin |
| 5000 Coins | 7,99 | R$ 49,90 | 179 | 39.900 | 8,99 € | ≈ 5,5 continues |
| Magician Bundle / Special Offer 9.99 | 9,99 | R$ 59,90 | 199 | 49.900 | 9,99 € | Bundle alto |
| 10000 Coins | 14,99 (snapshot mar-2026, no en ficha US hoy) · 19,99 CAD en Canadá | — | — | — | — | **[NV]** SKU rotativo o regional |
| Sorcerer Bundle | 19,99 | R$ 129,90 | 399 | 99.900 | 22,99 € | Bundle premium |

- Rango iOS 0,99-19,99 USD; precio medio 7,29 USD (App Pricing Lab) **[V]**. Un informe sectorial cita "6 bundles, 0,99-99,99 USD" **[NV]**.
- **No existe** SKU "quitar anuncios", suscripción, pase de pago ni alcancía **[V]**.
- **Precios regionales**: tiers automáticos de Apple sin descuento LatAm (R$ 6,90 ≈ 1,25 USD; 19 MXN ≈ 1,0 USD; 4.900 COP ≈ 1,15 USD): el continue cuesta a un brasileño 2-3× en poder adquisitivo. Europa sobre paridad (Sorcerer 22,99 €) **[V]**.
- Valoraciones iOS: EE. UU. 669 K · BR 76 K · MX 39 K · ES 19 K · CO 11 K → LatAm iOS ≈ 19 % de EE. UU.; en Android será mayor **[E]**.

### Anuncios

| Pregunta | Respuesta | Estado |
|---|---|---|
| Primer intersticial | **Nivel 22** (Gamigion) / nivel 24 (FriendlyGameDev). Pesa Gamigion: analista que jugó y documentó la sesión; la diferencia de 2 niveles es compatible con A/B. | [V] |
| Cadencia posterior | "Tras cada nivel" a partir de ~L50; 30 s para reintentar | [NV: agregadores de reseñas] |
| Rewarded | **Un solo placement** (vida extra al agotar las 5 vidas), el menor de 23 juegos estudiados (Casual Puzzle Report). Monedas/frascos por rewarded reportados solo por usuarios. | [V] / [NV] |
| Banner | Sin evidencia | [E: no] |
| Peso en ingreso | ≈ 1 M USD de 4,15-4,35 M en 30 días (may-2025) → **~23 %** | [V: Braberg] |
| ¿Pagar quita anuncios? | No hay SKU; comportamiento post-compra no documentado | [NV] |
| Queja | **67 % de las reseñas negativas citan anuncios**; 33 % dicen que el anuncio fuerza el cierre; 5 % que secuestra controles; rewarded que redirige a tienda sin premiar | [V: Glotier] |

**Contradicción resuelta:** DoF llama a Magic Sort "pure-IAP"; Braberg/Sensor Tower miden ~23 % de ingreso publicitario. Pesa Braberg (cifra medida frente a calificativo editorial): IAP-first con anuncios como relleno para no pagadores tardíos.

### Meta y live-ops

| Evento / sistema | Reglas (lo verificado + lo inferido) | Cadencia / desbloqueo |
|---|---|---|
| **Broom Streak** | Racha en solitario con hitos; cada victoria avanza la escoba; fallar reinicia **[V]**. **[E]** expira con el día (una racha de 19 se reinició "de la noche a la mañana"). | Aparece temprano (<L60, Gamigion) **[V]**; **[E]** permanente rotativo |
| **Magic Streak** | Racha competitiva con leaderboard (Naavik) **[V]**; **[E]** cofres por posición. | [NV] |
| **Jetpack Race** | Carrera competitiva por niveles completados; **[E]** contra bots/jugadores, 24-48 h, patrón "Sky Race" de Royal Match. | Desbloqueo **L80** (Casual Puzzle Report) **[V]** |
| **Sky Jump** | Racha sin fallar; caída al fallar; premio final único (la analista gastó 7 € para sostenerla) **[V]**. | Desbloqueo **L119**; el estudio lo etiqueta "pase estacional" **[V]** |
| **Treasure Drill** | Minijuego citado en reseñas como pop-up; **[E]** taladro por capas con cofres. | [NV] |
| Multiplicador de racha | ×2 → ×5 → ×10 → ×25 → ×100 en pantalla de victoria **y** en la barra del evento (Gamigion "UI detail") **[V]**. | Permanente |
| Reto diario / daily puzzle | Listado por Naavik **[V]**; **[E]** 1 nivel especial/día con cofre. | Diario |
| Bono diario / login | Calendario que favorece vidas sobre monedas **[R]**. | Desbloqueo **L34** **[V]** |
| Leaderboards | Semanal (Weekly Cup), por país y global; puntos por niveles completados **[V]**. Tamaño de liga y premios por tier **[NV]**. | Desbloqueo **L39** **[V]** |
| Cartas coleccionables y cofres | Las cartas salen de cofres y dan recompensas; ficha iOS declara "Contains Loot Boxes" **[V]**. Temporadas, intercambio y probabilidades **[NV]**. | [NV] |
| Ingredientes místicos → caldero | Se recogen en nivel y se "cocinan" en boosters más fuertes **[V]**. Recetas **[NV]**. | Permanente |
| Nuevo evento | — | **Cada ~2 meses** (Naavik) **[V]** |
| Meta decorativo (mapa, áreas, mansión) | **No existe**; Naavik lo describe como meta "ligero, para motivar el leaderboard" **[V]**. | — |

**Discrepancia resuelta:** Gamigion dice que los eventos "aparecen pronto" (antes del L60); el Casual Puzzle Report mide desbloqueos en L80 (Jetpack) y L119 (Sky Jump). Pesan los niveles del informe (medición con tiempo de juego); la frase de Gamigion aplica a Broom Streak y los widgets, y ambas son compatibles con A/B. Patrón común: **todos los eventos castigan el fallo** y los no pagadores no sostienen rachas **[V: Gamigion, reseñas]**.

### Social

- **Equipos**: **compartir vidas** y "participar en desafíos para conseguir monedas y tiempo infinito" (ficha); chat (rating 13+). Las reseñas elogian equipos activos que regalan vidas y cartas **[V]**. Torneo de equipos no confirmado **[NV]**.
- **Leaderboards** triples (semanal / país / global) con progreso diario en los tres **[V]**.
- **Cartas coleccionables** como recompensa social y de cofres **[V]**.
- Privacidad iOS declara datos de **contactos** → **[E]** invitaciones a equipos.

### Onboarding (primera sesión)

| Nivel | Frascos / colores / vacíos | Hito |
|---|---|---|
| 1 | 3 / 2 / 1 | Tutorial de 2 toques: "termina el frasco casi lleno antes de aparcar" |
| 3 | 5 / 3 / 1 | Enseña a reservar un vacío hasta el final |
| 6-10 | 6-8 / 4-5 / 1 | Primer "color problema" repartido; N10 frasco ayudante pequeño |
| 14 | 9 / 5-6 / 1 | **Primera novedad** (frasco de 1 unidad; ver nota en tabla de mecánicas) |
| 16, 24, 28 | 10-13 / 6-9 / 2-3 | Niveles de respiro; ritmo diente de sierra desde el inicio |
| 18-19 | — | Último nivel sin fails (L18); **primer pico de fallo L19** tras ~1 h 21 min **[V: Casual Report]** |
| 22 | 11 / 8 / 1 | **Primer intersticial** |
| 25 | 10-12 / 6-9 / 1 | "Shell bottles" ocupan una fila |
| 26 | 12 / 9 / 1 | **Easing dinámico**: al reintentar, −1 color |
| 30 | 14 / 10 / 1 | Primer pico claro |
| 34 / 39 | — | Bono diario / leaderboard (Weekly Cup) |

Tiempos por nivel según guías: 15 s-1,5 min (N1-5), 2-6 min (N6-10), 4-8 min (N11-15), 8-25 min (N26-30) **[V]**. La analista de Gamigion completó **60 niveles el primer día** **[V]**. Undo y Shuffle existen desde el inicio ("Wizard's Gear") **[V]**. Nivel de la primera oferta y del primer leaderboard: **[NV]**; **[E]** ventana 10-20. Broom Streak y pop-ups de minijuego aparecen antes del L60 **[V]**.

### Arte, UX y ficha de tienda

- **Dreamification** (DoF): "paleta, estilo de UI, *juice*, el look completo" de Royal Match aplicado a un género de anuncios **[V]**. **[E]** a partir de capturas: saturación alta y luz cálida de cuento, vidrio con refracción y líquido que gira al verter ("oddly satisfying"), botones biselados dorados, fondos de bosque/castillo, tipografía redondeada.
- **Personajes**: tres gatos traviesos como guías de capítulo (App Store Story); un gato que prepara magia en un **caldero** al pie de la pantalla y reacciona a los vertidos; el caldero es también el sistema de boosters **[V]**. Capítulos con escobas, portales y slime **[V]**.
- **Feedback**: respuesta inmediata, encolado de toques, legible en pantallas pequeñas, sonido ASMR **[V/R]**. Animación **[E]** ~0,3-0,5 s; cierre de frasco **[E]** tapón/halo + partículas; haptics **[E]** no documentados.
- **Victoria**: monedas, multiplicador ×2→×100, barras de todos los eventos activos, reto diario y cartas **[V]**. **Derrota**: continue inmediato aunque haya boosters en inventario; el layout cambia al reintentar **[V]**.
- **HUD [E]**: nivel arriba; monedas y vidas en cabecera; frascos en 2-3 filas; barra inferior Undo / Shuffle / +1 y gato-caldero; en el lobby, widgets laterales por evento (origen de la queja de "pop-ups").
- **Ficha**: título corto + subtítulo localizado ("Water Sorting Puzzle" / "Rompecabezas de Agua" / "Quebra-Cabeça d'Água"); descripción por bloques con cabeceras (Spellbinding Levels · Wizard's Gear · Vivid Splashes · Unique Twists · Boosts & Surprises · Mystical Ingredients); iOS 13+ (concursos, loot boxes, publicidad, chat) vs Android "Everyone"; 11 idiomas; ~25 capturas **[V]**. Keywords **[E]**: water sort, color sort, sorting puzzle, magic, relax, brain.
- **Creativos UA**: 8 playables (AppLovin + Meta); abrir en gameplay, varios vertidos satisfactorios, fail state para tensión, mano guía, cierre con selección de niveles (Mobidictum) **[V]**.

### Técnico

- **Unity**; builds Android 0.0.3xxx y iOS 3x.xx semanales desde ago-2024 con nota boilerplate "New 100 levels! Bug Fixes & Improvements" **[V]**.
- **Peso**: 609 MB iOS vs 218-234 MB Android. **[E]** binario universal sin compresión + IL2CPP, atlas retina por evento, 1.200+ niveles embebidos sin streaming, audio localizado, SDKs; sin *on-demand resources*. Coste: fricción de instalación en gama baja y LatAm.
- **SDKs/privacidad**: Exodus detecta **30 trackers** (sin nombres) y 20 permisos (Ad ID, AdServices, Billing, Install Referrer, Exact Alarm + Notifications, Foreground Service) **[V]**. **[E]** pila: AppLovin MAX con mediación, Adjust o AppsFlyer, Firebase, Facebook SDK.
- **A/B tests visibles** (dos iPhones iguales con funciones distintas) **[V]**. Bugs recurrentes: rewarded que no paga, anuncios que no cierran, crashes que cuestan vidas **[R]**.
- **Volumen de niveles**: guías documentan 1.038-1.158; si la nota semanal es literal el total supera los 2.000-3.000 **[E]**.

---

## Qué copiar tal cual

1. **Reglas canónicas** (capacidad 4, bloque superior, destino vacío o mismo color, victoria por monocolor, sin límite de movimientos ni tiempo): la "calma" que hace tolerable el fail state. → Tinajas, totumas y calabazas de 4 unidades con agua teñida por frutas (açaí, camu camu, cupuaçu, aguaje).
2. **Tap-tap con encolado de inputs y animaciones que nunca frenan**: el elogio UX más repetido y la base de sesiones de 1-3 min. → Idéntico; el agua se aclara al cerrar un recipiente.
3. **Fail state instantáneo + una sola oferta "+1 recipiente"**, sin menú de boosters: >50 % del IAP del líder porque la compra resuelve de verdad. → "+1 tinaja" ofrecida por la nutria guía en el instante del bloqueo.
4. **Racha como colateral con multiplicador ×2→×100 en victoria y en la barra del evento**: hace tangible lo que se pierde al fallar. → Evento "Crecida del río": cada victoria sube el agua.
5. **Puntos de precio 900 / 300 / 1,99 USD** (continue / racha de 3 / pack mínimo): ya interiorizados por el público del match-3. → Mismas cifras en "semillas", con precio local por país en Google Play.
6. **Primeros ~20 niveles sin anuncios ni fails** (L1-18 limpios, pico L19, intersticial L22): hábito antes de la fricción. → Igual; medir D1 y primer fail.
7. **Vidas 5 / ~25 min + vidas infinitas temporales + equipos que comparten vidas**: social barato y querido. → "Aldeas" que comparten agua/vidas.
8. **Un solo rewarded (vida extra) + intersticiales moderados**: menor nº de rewarded de su cohorte y mayor IAP. → Igual, más un rewarded de monedas pequeñas (ver mejoras).
9. **Frascos de tamaño variable desde ~L14**: profundidad sin nuevas reglas. → Cestas y calabazas pequeñas (1-3 u.), tinajas altas.
10. **Diente de sierra explícito**: Hard / Super Hard señalizados + respiros puros con 2 vacíos. → Etiquetas "Rápido"/"Remolino" con icono de piraña/anaconda.
11. **Easing adaptativo temprano** (−1 color al reintentar el N26): palanca anti-churn verificada. → Igual, pero comunicado ("el río bajó un color").
12. **100 niveles semanales con pipeline procedural + solver**, evento nuevo cada ~2 meses y desbloqueos escalonados (L34 bono diario, L39 liga, L80 sprint, L119 evento largo). → Generador + solver desde el día 1.
13. **Tres leaderboards (semanal / país / global)**. → Banderas de países amazónicos; la liga por país encaja con BR/CO/PE/EC.
14. **Ingredientes recogidos en nivel que se "cocinan" en boosters**: progreso convertido en comodines sin tocar la moneda dura. → Olla de barro / maloca.
15. **Cartas coleccionables desde cofres**. → Fauna y flora amazónica con valor educativo.
16. **Pila técnica y UA**: Unity, AppLovin MAX + Meta, 6-8 playables idénticos al juego, creativos emocionales con mascota ("la nutria necesita agua limpia", "rescata al delfín rosado", "carrera contra la crecida"), umbrales de clics 1/3/10/20/∞. → 11 idiomas desde el día 1 con **pt-BR y es-419 como tier 1**.
17. **Ficha**: título corto + subtítulo con "sort / ordenar agua", descripción por bloques, 13+ solo si hay chat. → "Amazon Fill – Ordena el agua del río".

---

## Qué copiar pero mejorar

| Queja o debilidad detectada | Nuestra mejora concreta |
|---|---|
| **Colores ocultos = azar** ("adivinas; si fallas no hay movimientos", insistente en BR/PT) | El solver garantiza **≥1 vertido seguro** con ocultos; pistas parciales (silueta, "1 de 3 revelado"); mini-comodín barato "Luciérnaga" que revela uno. |
| **Boosters guardados inservibles en la derrota** | **Undo permitido en el bloqueo**; 1 Undo gratis cada N niveles; el continue convive con el inventario. |
| **900 por continue vs 10-20 por victoria** (45:1); "forzar en vez de motivar"; ARPDAU en caída | Ratio **10-15:1**; continue variable **300-600** según dificultad; primer continue del día con descuento; rewarded de **50-100 monedas** para que exista ruta gratis. |
| **El layout cambia al reintentar** | **Misma semilla en el reintento inmediato**; variante nueva solo tras 3 fallos; comunicar el ajuste. |
| **Mecánicas tarde y sin novedad** (primer bloqueador nuevo ~L221; "?" en L70) | Los frentes proponen una mecánica cada 15-20 (C) o 40-60 niveles (A, B); adoptamos **una cada 20-30 hasta el L150 y luego cada 40-60**, con lógica amazónica legible: hoja que tapa (cubierto), semilla que germina con su color (corcho), liana que ata (candado), remolino (portal), barro, lluvia que llena una tinaja, piraña que "roba" un color. |
| **Rachas que expiran de noche y solo las sostiene quien paga**; premio < coste del continue | **Escudo de racha** (1 fallo perdonado al día); tolerancia 24-48 h; premio de racha **siempre** mayor que un continue; el pago acelera, no desbloquea. |
| **Hard/Super Hard "imposibles sin booster"; etiquetas que engañan** | Solver garantiza solución sin booster; etiqueta honesta con tiempo medio; recompensa visible ×2/×3. |
| **Tiers Apple sin descuento LatAm** (1.000 monedas = R$ 12,90 / 39 MXN / 9.900 COP) | **Precios por país en Google Play**: entrada 0,49-0,99 USD local; continue ≈ 0,99 USD local en BR, CO, PE, EC. |
| **Sin "sin anuncios", suscripción ni alcancía** | SKU **"sin anuncios + bono"** a 2,99-4,99 USD local; alcancía que capture monedas de niveles; Endless Portal transparente. |
| **Recompensa diaria da vidas cuando faltan monedas** | Calendario de 7 días con monedas en los días 3, 5 y 7. |
| **Pop-ups apilados en el lobby** | **Máximo un takeover por sesión**; widgets laterales silenciosos. |
| **Todos los eventos son competitivos y castigan el fallo** | **Evento cooperativo de aldea** (llenar el río comunal) y reto creativo sin leaderboard. |
| **Sin meta decorativo** (el CEO admite live-ops "sub-optimizados") | **Reconstruir aldea/ecosistema por país** con el progreso; diferenciador de UA y palanca de D30 (Hexa Sort >15 % vs <10 % del género). |
| **609 MB en iOS** | **<150 MB inicial en Android, <200 MB en iOS**; niveles y arte de eventos bajo demanda. |
| **Dos redes de UA y un solo mercado fuerte** (Grand tardó un año en salir de Turquía) | Google Ads, TikTok y Mintegral desde el soft launch; LatAm como mercado semilla. |
| **Notas de versión boilerplate; A/B tests opacos** | Publicar cambios de balance; no someter a A/B la economía núcleo (continue, vidas). |
| **Monetización solo "reactiva"** vs Knit Out / Pixel Flow ("inversión") | Pase o colección donde el pago mejora la capacidad de jugar (recipientes ancestrales, cofres del río), además de restaurar la racha. |

---

## Qué NO copiar

- **Nombre y marca**: "Magic Sort", "Sihirli Sıralama", el sufijo "Sort!"; nombres de eventos y sistemas (Broom Streak, Magic Streak, Jetpack Race, Sky Jump, Treasure Drill, Endless Portal, Wizard's Gear, Mystical Ingredients). Riesgo de confusión y reclamación de marca.
- **Trade dress y arte**: gatos magos, caldero, escobas, portales morados, varitas; paleta y UI dorada biselada reconocible de Royal Match; botellas de vidrio con la misma silueta; calavera para Hard. Riesgo de reclamación por imitación visual (no verificado litigio para este caso). Amazon Fill debe identificarse por recipientes autóctonos, luz de selva y mascota propia.
- **Texto de ficha, capturas y vídeos**: redactar la ficha desde cero; no copiar bloques ni composición de capturas.
- **Prácticas que destruyen la nota**: intersticial tras cada nivel desde ~L50 y anuncios sin cierre (67 % de las negativas); rewarded que no premia; crashes que cuestan vidas.
- **Fail state injusto**: cobrar el continue antes de permitir comodines del inventario; cambiar el puzle al reintentar sin avisar.
- **Colores ocultos como RNG puro** y Super Hard por fuerza bruta: "no hay estrategia posible".
- **Eventos pay-to-win**: rachas que se reinician solas con premio inferior al gasto; multiplicadores cuyo fallo borra el progreso del no pagador.
- **Economía 900/20 sin ruta gratuita**: el líder ya muestra caída de ARPDAU por este desequilibrio.
- **Loot boxes sin probabilidades publicadas y chat sin moderación** (rating 13+): exposición regulatoria en Brasil y otros mercados objetivo; publicar probabilidades y moderar o eliminar el chat.
- **30 trackers y acceso a contactos**: exposición bajo LGPD (Brasil) y leyes de datos LatAm; minimizar SDKs y no pedir contactos.
- **Modelo IEC Global** (ads cada nivel, "No Ads" que no funciona, cero monedas por nivel): 100 M descargas y <2 M USD/año de categoría.
- **609 MB y un único mercado** durante el primer año.

---

## Lo que no pudimos verificar

- Recompensa base por victoria (10-20 monedas) y bonus de Hard/Super Hard: solo reseñas y convención **[R]/[E]**.
- Coste de Undo y Shuffle (300 monedas según una sola reseña) **[NV]**.
- Contenido de Starter, Apprentice, Magician y Sorcerer; qué desbloquea Endless Portal (vidas infinitas vs niveles sin fin) **[E]/[NV]**.
- SKU de 10.000 monedas: 14,99 USD (snapshot mar-2026) vs 19,99 CAD; ausente en la ficha US hoy **[NV]**. Rango IAP en Google Play (ficha no legible) **[NV]**.
- Cadencia exacta de Hard/Super Hard y color de fondo; la distribución 15/61/202/164 es rating comunitario de levelsolve **[E]**.
- Nivel de introducción de portales, slime, hielo, llaves, spray y wraps; nivel de la primera oferta y del primer leaderboard **[NV]**.
- Reglas completas, duración, tamaño de liga y premios de cada evento; probabilidades de cofres; temporadas de cartas; recetas del caldero **[NV]**.
- Cadencia de intersticiales tras el L22 ("cada nivel" desde ~L50) **[NV]**.
- Duración de animaciones, haptics y efecto de frasco completado **[E]**.
- Total real de niveles (>2.000-3.000 si la nota semanal es literal) **[E]**.
- ARPDAU: 0,16 USD (Sensor Tower) vs 0,29-0,30 USD calculado desde Braberg para may-2025; pesa Sensor Tower como medición, pero las bases (neto/bruto, IAP/total) no están publicadas **[E]**.
- Gasto en UA (12-25 M USD en 2025, estimación gruesa) y reparto de ingresos por país **[E]**.
- Lista nominal de los 30 SDKs; pila publicitaria concreta **[E]**.
- Fecha de lanzamiento: 28-may-2024 (ficha iOS) vs abril 2024 (Gamigion); pesa la ficha, sin descartar soft launch previo.
- Ningún frente jugó el build: los tres son investigación documental.

---

## Fuentes

**Analistas e industria**
- Deconstructor of Fun, "Sort Puzzles: How a New Subgenre is Born" (6-feb-2026): https://www.deconstructoroffun.com/blog/2026/2/6/sort-puzzles-how-a-new-subgenre-is-born
- Deconstructor of Fun, "Grand Games from $0 to $500M in Two Years" (8-jun-2026): https://www.deconstructoroffun.com/blog/grand-games-from-0-to-500m-in-two-yearsnbsp
- Naavik, "Grand Games and Leveling Up Mobile Casual Puzzle" (19-ene-2025): https://naavik.co/digest/why-peak-games-investors-backed-grand-games/
- Naavik, "How Niche Subgenres are Reshaping the Mobile Puzzle Market" (22-feb-2026): https://naavik.co/digest/how-niche-subgenres-are-reshaping-the-mobile-puzzle-market/
- Gamigion, "Game Deconstruction: Magic Sort by Grand Games": https://www.gamigion.com/game-deconstruction-magic-sort-by-grand-games/
- Gamigion, "Magic Sort Analysis: Grand Games' New Treasure" (16-ene-2025): https://www.gamigion.com/magic/
- Gamigion, "What's Wrong With Magic Sort's Game Shop?": https://www.gamigion.com/whats-wrong-with-magic-sorts-game-shop/
- Gamigion, "Loved this little UI detail in Magic Sort!": https://www.gamigion.com/loved-this-little-ui-detail-in-magic-sort/
- Gamigion, "The Monetization Engine Behind Magic Sort's Rise": https://www.gamigion.com/the-monetization-engine-behind-magic-sorts-rise/
- Gamigion, "Magic Sort continues to scale to $120K per day": https://www.gamigion.com/magic-sort-by-grand-games-continues-to-scale-to-120k-per-day/
- Gamigion, "Pixel Flow, Magic Sort, Knit Out: Why Players Can NOT Stop": https://www.gamigion.com/pixel-flow-magic-sort-knit-out-why-players-can-not-stop/
- Gamigion, "Sort Puzzle September WINNERS?": https://www.gamigion.com/sort-puzzle-september-winners/
- Gamigion, "Hexa Away Breakdown: 6th Biggest Sort Puzzler": https://www.gamigion.com/hexa-away-breakdown-6th-biggest-sort-puzzler/
- Gamigion, "The 'Why' Behind Grand Games' $30M Investment": https://www.gamigion.com/the-why-behind-grand-games-30m-investment/
- Gamigion, "How is it going, Grand Games?": https://www.gamigion.com/how-is-it-going-grand-games/
- Felix Braberg, "The Monetization Engine Behind Magic Sort" (30-may-2025): https://felixbraberg.substack.com/p/the-monetization-engine-behind-magic
- PocketGamer.biz, "One genre, three strategies" (14-may-2026): https://www.pocketgamer.biz/one-genre-three-strategies-how-magic-sort-knit-out-and-pixel-flow-are-redefining-sort-puzzle-monetisation/
- Mobidictum, "Pixel Flow and Peers: Deconstructing Winning Creatives in Sort Puzzle Games" (ago-2026): https://mobidictum.com/pixel-flow-deconstructing-creatives-sort-puzzles/
- Algoryte, "The ultimate sorting game face-off": https://algoryte.substack.com/p/the-ultimate-sorting-game-face-off
- Casual Puzzle Games Intelligence Report 2026: https://casual-report.tiiny.site/
- Artik Agency, caso de UA de Magic Sort: https://artik.agency/cases/magic-sort-user-acquisition-case-study
- Sensor Tower, Top hypercasual Android US Q1 2025: https://sensortower.com/blog/2025-q1-android-top-5-hypercasual%20games-revenue-us-643acc7de1714cfff1e77c54
- Playio, benchmarks ARPDAU: https://blog.playio.co/arpdau-benchmarks-mobile-games
- GameDev Reports, AppMagic Landscape 2026: https://gamedevreports.substack.com/p/appmagic-mobile-market-landscape

**Empresa y financiación**
- TechCrunch, Serie A (9-ene-2025): https://techcrunch.com/2025/01/09/grand-games-the-turkish-gaming-startup-behind-pouring-game-magic-sort-drinks-up-30m/
- Balderton, Serie A: https://www.balderton.com/news/grand-games-raises-30m-series-a-to-build-genre-defining-mobile-games/ · https://balderton.com/?p=10975
- PocketGamer.biz, Serie B (11-may-2026): https://www.pocketgamer.biz/grand-games-raises-70m-series-b-to-scale-hybrid-casual-gaming/
- InvestGame, Serie B: https://investgame.net/news/grand-games-balderton-backs-70m-series-b-as-turkiye-s-hybrid-casual-momentum-accelerates/
- Dealroom, Serie B: https://dealroom.co/news/128722-grand-games-raises-70m-as-turkeys-mobile-gaming-machine-keeps-producing/
- TechFundingNews, Serie B: https://techfundingnews.com/turkish-gaming-startup-grand-games-bags-70m-series-b-led-by-balderton-capital-for-hybrid-casual-puzzle-games/
- TheNextWeb, Serie B: https://thenextweb.com/news/istanbuls-grand-games-lands-a-70m-series-b-as-balderton-doubles-down-on-turkeys-puzzle-game-pipeline
- mobilegamer.biz, data digest (13-may-2026): https://mobilegamer.biz/data-digest-sensor-tower-inmobi-and-smartlys-big-buys-grand-lands-70m-huge-applovin-numbers-more/
- Grand Games web y empleo: https://grand.gs/ · https://grand.gs/careers/ · https://jobs.lever.co/grand/800e31c8-10af-40eb-9026-701cce7b8c8a · https://jobs.lever.co/grand/4e9fa2b2-abf8-47f3-a4e2-149c42f27a5f

**Fichas de tienda**
- App Store EE. UU.: https://apps.apple.com/us/app/magic-sort/id6499209744
- App Store EE. UU., reseñas: https://apps.apple.com/us/app/magic-sort/id6499209744?see-all=reviews
- App Store Reino Unido (historial de versiones): https://apps.apple.com/gb/app/magic-sort/id6499209744
- App Store Australia, reseñas: https://apps.apple.com/au/app/magic-sort/id6499209744?see-all=reviews
- App Store España: https://apps.apple.com/es/app/magic-sort/id6499209744
- App Store Brasil: https://apps.apple.com/br/app/magic-sort/id6499209744
- App Store México: https://apps.apple.com/mx/app/magic-sort/id6499209744
- App Store Colombia: https://apps.apple.com/co/app/magic-sort/id6499209744
- App Store TR/DE/FR/JP/KR/CA (rankings y precios): https://apps.apple.com/{tr,de,fr,jp,kr,ca}/app/magic-sort/id6499209744
- App Store Story (consejos oficiales): https://apps.apple.com/au/iphone/story/id1873942358
- Google Play, ficha: https://play.google.com/store/apps/details?id=com.grandgames.magicsort&hl=en_US
- Google Play, página del desarrollador: https://play.google.com/store/apps/dev?id=6430038787206489041&hl=en
- APKCombo: https://apkcombo.com/magic-sort/com.grandgames.magicsort/
- Uptodown: https://com-grandgames-magicsort.en.uptodown.com/android
- AndroidRank: https://www.androidrank.org/application/magic_sort/com.grandgames.magicsort
- App Pricing Lab (snapshot IAP 2026-03-21): https://apppricinglab.com/app/apple/6499209744
- Exodus Privacy (trackers y permisos): https://reports.exodus-privacy.eu.org/en/reports/com.grandgames.magicsort/latest/
- Water Sort Puzzle (IEC Global), App Store: https://apps.apple.com/us/app/water-sort-puzzle/id1514542157
- JustUseApp, reseñas de Water Sort Puzzle: https://justuseapp.com/en/app/1514542157/water-sort-puzzle/reviews

**Guías, reseñas y agregadores**
- levelsolve (mecánicas por nivel, distribución de dificultad): https://levelsolve.com/magic-sort/ y páginas /level/1, 10, 14, 25, 43, 60, 70, 100, 150, 180, 200, 221, 280, 331, 400, 420, 442
- magicsort.app: https://magicsort.app/blog/complete-walkthrough-magic-sort-levels-1-30 · https://magicsort.app/blog/master-magic-sort-beginners-guide · https://magicsort.app/blog/advanced-strategies-magic-sort · https://magicsort.app/levels
- sortpuzzlehelp: https://sortpuzzlehelp.com/magic-sort-answers/
- magicsortlevel, nivel 1000: https://magicsortlevel.com/levels/1000
- iofreeonline (900/300, vidas): https://www.iofreeonline.com/IOS/game/Magic-Sort.html
- FriendlyGameDev, reseña (ads desde L24): https://friendlygamedev.com/game-reviews/magic-sort/
- Glotier (estadística de reseñas, jul-2026): https://glotier.com/games/magic-sort
- mwm.ai (agregador): https://mwm.ai/apps/magic-sort/6499209744
- grand-screen.com (etiquetas Hard con calavera): https://grand-screen.com/games/magic-sort/
- pg.io (editor review): https://pg.io/store-game/magic-sort
- YouTube, Broom Streak: https://www.youtube.com/watch?v=Cp41UN1xKjc · Endless Portal: https://www.youtube.com/watch?v=5PyYQM850_A · niveles 1-10: https://www.youtube.com/watch?v=_LHy9l8uK6Q

**No verificable con fuentes públicas en los tres frentes:** ficha de Google Play (rango IAP), app.sensortower.com, publisher page de AppMagic, descripciones de vídeos de YouTube, hilos de Reddit.
