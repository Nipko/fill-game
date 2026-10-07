# Magic Sort! (Grand Games) — Frente C: Meta, live-ops, social, UX, arte y ficha de tienda

**Fecha:** 2026-10-05 · **Proyecto:** Amazon Fill · **Alcance:** todo lo que rodea al core loop (eventos, social, onboarding, arte/UI, ASO, técnico, voz del jugador).
**Leyenda:** `[V]` dato verificado con URL (ver Fuentes) · `[E]` estimación o inferencia del analista · `[NC]` no confirmable con fuentes públicas.

---

## 1. Resumen ejecutivo

- Magic Sort es un water-sort "dreamificado": core 100 % estándar, envuelto en producción nivel Royal Match y un meta ligero pero completo (3 leaderboards, equipos con vidas compartidas, cartas, cofres/loot boxes, eventos de racha). `[V]` Naavik, DoF.
- Grand Games lo define como juego **pure-IAP** (los anuncios existen pero empiezan en el nivel 22 y son secundarios). Más del 50 % del IAP viene del *end-of-round* (comprar un recipiente extra para no perder la racha). `[V]` Naavik, Gamigion, DoF.
- Cadencia industrial: **100 niveles nuevos cada semana** (iOS 32.92 → 32.40 → 31.63 → 31.27 en 4 semanas; Android builds 3011→3293 en 6 semanas) y **un evento nuevo cada ~2 meses**. `[V]` App Store, Uptodown, Naavik.
- Escala actual: 10 M+ instalaciones Google Play (AndroidRank estima 25,4 M), 50 M+ descargas del grupo, Magic Sort #4 Casual gratis en US App Store hoy, >40 M USD primer año, run-rate del estudio 500 M USD/año (jun-2026). `[V]`
- El propio CEO reconoce que sus live-ops están "sub-optimizados" y que los equipos "corren el juego con energía Supercell 2012": **la ventana para ganarles está en meta, eventos justos y onboarding, no en el core**. `[V]` DoF jun-2026.
- Voz del jugador (900 K+ reseñas combinadas): 4,7★ iOS / 4,5★ Android, pero 67 % de las negativas citan anuncios y 25 % pay-to-win; la queja estructural es que el *continue* cuesta 900 monedas mientras los eventos pagan 300, y que los *boosters* guardados no sirven en la pantalla de derrota. `[V]` Glotier, App Store reviews.

---

## 2. Meta y live-ops

### 2.1 Eventos

| Evento | Tipo | Qué sabemos `[V]` | Reglas inferidas `[E]` |
|---|---|---|---|
| **Broom Streak** | Racha solo / hitos | Para jugadores solitarios; recompensas por hitos; aparece en los primeros niveles; existe tutorial de terceros (jun-2026). | Cada victoria consecutiva avanza la escoba; fallar reinicia la racha (reseñas: una racha de 19 se reinició "de la noche a la mañana" → probablemente expira con el día). Premios escalonados en monedas/boosters. |
| **Magic Streak** | Racha competitiva + leaderboard | Nombrado por Naavik como evento con leaderboard. | Puntos por racha, ranking frente a ~N jugadores, cofres por posición. |
| **Jetpack Race** | Carrera competitiva | Leaderboard; reseñas lo citan como pop-up recurrente. | Equivalente al "Sky Race"/"Lightning Rush" de Royal Match: carrera contra bots/jugadores por nivel completado, 24-48 h. |
| **Sky Jump** | Racha | Requiere racha sin fallar; la analista de Gamigion gastó 7 € en monedas para no romperla y el premio valió menos que una continuación. | Saltos de plataforma por victoria; caída al fallar; premio final único. |
| **Treasure Drill** | Minijuego/colección | Citado en reseñas como pop-up de minijuego. | Taladro que baja por capas con cada victoria, cofres en profundidad. |
| **Endless Portal** | IAP 5,99 USD (6,99 € / 9,99 AUD / 299,99 ₺) | Figura en la lista de IAP de todas las tiendas; tutorial de terceros. | Vidas infinitas por tiempo (patrón "Infinite Lives" de Royal Match) o acceso a niveles sin fin. `[NC]` cuál de los dos. |

**Cadencia** `[V]`: nuevo evento cada ~2 meses (Naavik, ene-2025). **Patrón común** `[V]`: todos los eventos castigan el fallo; Gamigion y las reseñas coinciden en que los no pagadores no sostienen rachas y perciben que "pagar es la única forma de ganar el evento". Las edades de la ficha iOS (13+: "Frequent Contests", "Contains Loot Boxes", "Advertising", "Messaging and Chat") confirman concursos frecuentes, cofres aleatorios y chat. `[V]`

### 2.2 Social, colección y hábito

| Sistema | Estado | Detalle `[V]` | Hueco `[E]/[NC]` |
|---|---|---|---|
| Leaderboards | Sí, 3 | Semanal, por país y global; el jugador progresa a diario en los tres (puntuación por niveles completados). `[V]` Naavik | Tamaño de liga, premios por *tier* `[NC]`. |
| Equipos | Sí | Unirse a equipos para **compartir vidas** y "participar en desafíos para conseguir monedas y tiempo infinito" (ficha). Chat (rating 13+). Reseñas elogian equipos activos que regalan vidas. `[V]` | Torneo de equipos tipo "Team Battle" `[NC]`; parece limitado a vidas + chat. |
| Cartas coleccionables | Sí | "Collectible cards which in turn give you even more rewards" (ficha/reseñas). Las cartas salen de cofres. `[V]` | Álbum por temporada, duración, intercambio `[NC]`. |
| Cofres / loot boxes | Sí | Ficha iOS declara "Contains Loot Boxes"; "reward chests" en la ficha MX. `[V]` | Probabilidades publicadas `[NC]`. |
| Ingredientes místicos → caldero | Sí | Se recogen ingredientes durante niveles para "preparar boosts más fuertes" en un caldero (ficha, review pg.io). Es el "coleccionable in-round" que convierte progreso en boosters. `[V]` | Cantidades/recetas `[NC]`. |
| Reto diario / daily puzzle | Sí | Naavik lo lista como "daily puzzle mechanics". `[V]` | Formato (1 nivel especial/día con cofre) `[E]`. |
| Recompensa diaria / login | Sí | Reseñas: el calendario "favorece vidas sobre monedas" aunque las monedas son el cuello de botella. `[V]` | Estructura de 7 días `[E]`. |
| Vidas | Sí | Sistema de vidas; se pierden al fallar; equipos y anuncios recompensados las repone; crashes hacen perder vidas (queja). `[V]` | |
| Logros / niveles secretos | Parcial | Sitios de guías mencionan niveles ocultos tras hitos. `[V]` débil | |
| Decoración / "áreas" meta | **No** | Ninguna fuente ni la ficha mencionan mapa decorativo, mansión o áreas. El meta es "ligero, para motivar el leaderboard" (Naavik). `[V]` | Oportunidad para Amazon Fill (ver sección final). |

---

## 3. Onboarding: primera sesión (niveles 1-30)

**Curva de contenido** `[V]` (magicsort.app, guía 1-30):

| Nivel | Botellas / colores / vacías | Hito |
|---|---|---|
| 1 | 3 / 2 / 1 | "Tu primer vertido": tutorial táctil de 2 toques. |
| 3 | 5 / 3 / 1 | Se enseña a reservar una botella vacía hasta el final. |
| 6-10 | 6→8 / 4→5 / 1 | Primer "color problema" repartido en varias botellas (n.5). |
| 14 | 9 / 6 / 1 | **Primera mecánica nueva**: colores "bloqueados" bajo capas. |
| 16, 24, 28 | 10-13 / 6-9 / 2-3 | Niveles-respiro con más vacías (ritmo diente de sierra). |
| 20-23 | 11 / 8 / 1 | Meseta de dificultad; aquí se cruza el nivel 22. |
| 30 | 14 / 10 / 1 | Primer pico claro. |

**Hitos de monetización/meta** `[V]`:
- **Nivel 22: primer anuncio** intersticial (Gamigion). Antes, cero anuncios: posicionamiento "premium casual".
- **Nivel 26: easing dinámico**: al fallar y reintentar, el juego quita un color (Naavik). Es la palanca anti-churn temprana.
- **Eventos (Broom Streak, Jetpack Race, Sky Jump) aparecen "early in progression"** (Gamigion), antes del nivel 60; la analista jugó 60 niveles el día 1 en estado de flow.
- **Primer fail-state monetizado**: pantalla de derrota con oferta "+1 botella" por monedas (Gamigion). Reseñas sitúan el precio en **900 monedas** (≈1,80 USD con el pack de 1.000 por 1,99). `[V]`
- **Comodines**: Undo y Shuffle existen desde el inicio como "Wizard's Gear"; reseñas critican que hay que usarlos **antes** de quedarse sin movimientos, no en la derrota. `[V]`
- Primera oferta (Starter Bundle 0,99) y primer leaderboard: `[NC]` nivel exacto; `[E]` ventana 10-20 por patrón del género.
- **Hard / Super Hard**: etiquetas con calaveras (grand-screen). Un sitio de soluciones cataloga el tramo **43-112 como "expert"** (su propio rating), y las reseñas marcan el **nivel ~100** como frontera "pay-to-play" por la llegada de **colores ocultos** (signos de interrogación), tapones, cajas con candado encadenadas, cortinas de teatro y cajas-regalo (levelsolve, sortpuzzlehelp). `[V]`

---

## 4. Arte y UX

**Qué significa "Dreamification" aquí** `[V]` DoF (feb-2026): "paleta de color, estilo de UI, *juice*, el look completo" de Royal Match aplicado a un género de anuncios; DoF añade que Magic Sort "solo necesita unos cuantos eventos y ofertas más para parecer casi igual que Royal Match". En concreto `[E]` a partir de descripciones y capturas: saturación alta y luz cálida tipo cuento; botellas de vidrio con refracción y líquido que "gira suavemente" al verter (reseñas: "oddly satisfying"); UI de botones biselados/dorados con brillo; fondos de bosque/castillo místico; tipografía redondeada.

- **Mascota** `[V]`: un **gato** que "prepara magia en un caldero en la parte inferior" de la pantalla (pg.io/mwm). No hay mago humano protagonista; el caldero es también el sistema de boosters (ingredientes → poción). Rol de la mascota: reacciona a vertidos y da feedback, no narra una historia.
- **Feedback** `[V]`: niveles rápidos, respuesta inmediata, se pueden **encolar toques** sin esperar la animación (elogio recurrente); legible "incluso en pantallas pequeñas"; sonido ASMR relajante.
- **HUD** `[E]`: contador de nivel arriba, monedas y vidas en cabecera, botellas en 2-3 filas centradas, barra inferior con Undo / Shuffle / botella extra y el gato-caldero; en el lobby, widgets laterales por evento activo (de ahí la queja de "pop-ups de minijuegos").
- **Hard level** `[V]` etiqueta + calavera; `[E]` fondo tintado (morado/rojo) y música tensa, patrón Royal Match. `[NC]` color exacto.
- **Pantallas de derrota** `[V]`: oferta inmediata de pagar para continuar aunque tengas boosters en inventario (queja #1). La disposición del nivel **cambia** al reintentar (queja: impide aprender la solución).
- **Creativos de UA** `[V]`: 8 playables en AppLovin + Meta en ene-2025 (sin Google, Mintegral, Moloco ni TikTok); el género gana con creativos "pegados al producto": abrir en gameplay, varios vertidos satisfactorios, fail-state para tensión, mano guía, cierre con selección de niveles (Mobidictum, ago-2026). `[E]` El look premium es lo que separa su CPI del water-sort genérico: los playables se parecen al juego real y retienen.

---

## 5. Ficha de tienda / ASO

| Campo | iOS `[V]` | Google Play `[V]` |
|---|---|---|
| Título / subtítulo | Magic Sort! — *Water Sorting Puzzle* | Magic Sort! (TR: "Sihirli Sıralama") |
| Subtítulos locales | ES *Rompecabezas de Agua* · DE *Wassersortierpuzzle* · PT-BR *Quebra-Cabeça d'Água* · FR *Puzzle de Tri d'Eau* · JP マジックソート/水のソートパズル · KR 마법의 정렬/물 정렬 퍼즐 | Descripción con cabeceras emoji: Quick Game Guide · Incredible Features · Why Magic Sort? · Ready to Launch Some Color! |
| Bloques de la descripción | Spellbinding Levels · Wizard's Gear (Undo/Shuffle) · Vivid Splashes · Unique Twists (botellas congeladas/encapuchadas) · Boosts & Surprises (caldero) · Mystical Ingredients | Igual + "hidden colors", "super hard", equipos para vidas y desafíos |
| Valoración | 4,7★ · 669 K (US); 1,5 M ratings globales (mwm.ai) | 4,5★ · 560 K reseñas (543 K verificadas); AndroidRank 4,53 sobre 482 K: 72 % 5★, 16 % 4★, 7 % 3★, 1,5 % 2★, 3 % 1★ |
| Descargas | — | 10 M+ oficial (abr-2026); 25,4 M estimadas |
| Tamaño | 609,2 MB | XAPK 218-234 MB; Android 8.0+ |
| Edad | 13+ (concursos frecuentes, loot boxes, publicidad, chat) | Everyone; "contiene anuncios" |
| Idiomas | 11: EN, NL, FR, DE, IT, JA, KO, PT, RU, ES, TR | — |
| Categoría | Puzzle (compite en Casual) | Puzzle |
| Lanzamiento | 28-may-2024 | APK desde jun-2024 |
| Palabras clave `[E]` | water sort, color sort, sorting puzzle, magic, relax, brain | idem; "sort" en título + subtítulo |

**Dónde rankea hoy (App Store, top gratis)** `[V]`: US #4 Casual · GB #5 Casual (4,6★/67 K) · CA #5 Casual (4,6★/53 K) · ES #6 Puzzle (4,6★/19 K) · DE #7 Casual (4,5★/46,8 K) · BR #9 Puzzle (**4,8★**/76 K) · FR #9 Parties rapides (4,5★/45 K) · MX #10 Ingenio (**4,8★**/39 K) · KR #11 Puzzle (4,6★/16 K) · AU #18 Casual (4,6★/27 K) · JP #33 Puzzle (4,3★/47 K) · TR #67 Puzzle (4,5★/29 K). Lectura: fuerte en anglosajones y **LatAm (BR/MX con la mejor nota)**, flojo en Japón; Turquía fue su primer mercado (ene-2025) y hoy está saturado. Glotier: **#73 top grossing juegos US** (jul-2026).

**Precios IAP** `[V]`: Starter 0,99 · 1.000 monedas 1,99 · Apprentice 4,99 · Endless Portal 5,99 · 5.000 monedas 7,99 · Magician 9,99 · Sorcerer 19,99 · 10.000 monedas 19,99 (CA) · Special Offers 0,99/4,99/9,99. Precios locales por encima de paridad en EU (Sorcerer 22,99 €) y por debajo en BR (R$ 129,90 ≈ 24 USD) / MX ($399 ≈ 21 USD); TR 999,99 ₺. Sin suscripción ni "no ads" (Gamigion lo recomienda).

**Capturas/vídeo** `[E]`: 25 capturas en el agregador pg.io; patrón del género: 1) botellas + gato, 2) "Relax & sort", 3) mecánicas nuevas, 4) eventos, 5) equipos. `[NC]` vídeo de ficha.

---

## 6. Técnico

- **Motor: Unity** `[V]`: la oferta "Game Developer" de Grand (Lever) exige 2+ años de Unity y POO. Builds Android numeradas 0.0.3xxx (semanal), iOS 3x.xx.
- **Peso** `[V]` 609 MB iOS vs 218-234 MB XAPK Android. `[E]` Causas: binario universal iOS sin compresión + IL2CPP, atlas de arte a resolución retina para cada evento/temporada, 1.200+ niveles embebidos (no se descargan por streaming), audio localizado, y SDKs. Android se beneficia de splits por ABI y compresión. Grand no usa *on-demand resources*; coste: fricción de instalación en gama baja/LatAm.
- **SDKs** `[V]` Exodus (v0.0.3162, 15-sep-2026): **30 trackers** detectados (nombres no listados en la fuente) y 20 permisos: Ad ID, AdServices (attribution, custom audience, topics), Billing, Install Referrer, Exact Alarm + Notifications (recordatorios de vidas/eventos), Foreground Service. App Privacy iOS: rastreo con identificadores y uso; datos vinculados incluyen **contactos** y "contact info" (`[E]` invitaciones a equipos/amigos). `[E]` Pila probable: AppLovin MAX con mediación (Meta, Unity Ads, ironSource, Mintegral, Google), Adjust o AppsFlyer, Firebase, Facebook SDK.
- **Frecuencia de updates** `[V]`: semanal en ambas tiendas, siempre "New 100 levels! Bug Fixes & Improvements" desde ago-2024; A/B tests visibles (reseña: dos iPhones iguales con funciones distintas).

---

## 7. Reseñas: temas

**10 quejas (meta/UX/monetización)** `[V]` síntesis de App Store US/ES/BR/AU, Google Play EN/ES/PT/TR, Glotier, mwm.ai:
1. **Anuncios** (67 % de las negativas; "uno por nivel", 30 s para reintentar; sube la frecuencia tras nivel ~20).
2. **Continue a 900 monedas** vs premios de evento de 300: economía percibida como trampa.
3. **Boosters guardados inservibles en la derrota**: hay que gastarlos a ciegas antes.
4. **Colores ocultos tras el nivel ~100** → "suerte, no lógica" (BR/PT muy insistente).
5. **Picos hard/super hard "imposibles sin pagar"**; niveles "easy" tan duros como "hard".
6. **El layout cambia al reintentar**: no se puede aprender.
7. **Eventos de racha** que se reinician solos y favorecen a quien paga; 7 € para un premio menor.
8. **Pop-ups**: ofertas y minijuegos (Jetpack Race, Treasure Drill) interrumpen el lobby.
9. **Anuncios recompensados rotos**: redirigen a la tienda sin dar el premio; anuncios que no cierran; crashes que cuestan vidas.
10. **Recompensa diaria desalineada**: da vidas cuando faltan monedas; sin opción de pagar para quitar anuncios/suscripción.

**5 elogios**: relajante y "satisfying" (vertidos, sonido); **rápido**, permite encolar movimientos; **menos anuncios forzados que la competencia** al inicio; **equipos generosos** con vidas y cartas que dan recompensas; gráficos que corren fluidos en móviles viejos y, para muchos, "lo mejor del género" frente a clones de Royal Match.

---

## Qué copiar / qué mejorar / qué evitar para Amazon Fill

**Copiar (está probado)**
- La **arquitectura Dreamification**: core estándar + arte premium + *juice* + cadencia semanal de 100 niveles. En Amazon Fill: luz de selva, agua de río con refracción real, tintes de frutas (açaí, camu camu, cupuaçu, aguaje) como "paleta" y recipientes autóctonos (tinajas, calabazas, totumas) con las mismas siluetas legibles que las botellas.
- **Cero anuncios hasta ~nivel 20** y easing dinámico al reintentar (nivel 26). Copiar tal cual y medir.
- **Fail-state monetizado con recipiente extra** como fuente #1 de IAP; **3 leaderboards** (semanal/país/global: perfecto para "países amazónicos" con banderas), equipos con vidas compartidas, ingredientes in-round que se cocinan en boosters (en vez de caldero, una **olla de barro / maloca**), cartas coleccionables (fauna/flora amazónica, con valor educativo real).
- **Pila técnica**: Unity, AppLovin MAX + Meta para UA con 6-8 playables idénticos al juego, 11 idiomas desde el día 1 (añadir **pt-BR y es-419 como tier 1**: BR/MX son sus mejores notas y nosotros tenemos ventaja cultural).
- **Ficha**: título corto + subtítulo con "sort/ordenar agua", bloques de descripción con cabeceras, 13+ solo si hay chat.

**Mejorar (donde los jugadores lo piden y Grand admite debilidad)**
- **Economía justa del evento**: que el premio de racha siempre supere el coste de una continuación; **rachas con "escudo"** (1 fallo perdonado por día) y que no caduquen de noche.
- **Boosters usables en la derrota** y precio del continue variable por dificultad (300-600, no 900 fijo).
- **Mismo layout al reintentar** (semilla fija) en niveles hard: convierte frustración en maestría; compensar la pérdida de IAP con el escudo de racha.
- **Mecánicas antes y con ritmo**: Magic Sort estrena la primera en el nivel 14 y los colores ocultos en el ~100; introducir una por cada 15-20 niveles con tema (lianas que bloquean, piraña que "roba" un color, lluvia que llena una tinaja).
- **Eventos no competitivos**: reto creativo/temporal (Gamigion), evento cooperativo de equipo (llenar el río de la aldea), que hoy no existen.
- **Meta de decoración** que Magic Sort no tiene: reconstruir una aldea/ecosistema por país, aprovechando la temática Amazonas como diferenciador de UA y retención D30 (Hexa Sort >15 % D30 vs <10 % del género).
- **Suscripción / "sin anuncios"** y Endless Portal transparente.
- **Peso**: objetivo <200 MB iOS con descarga por streaming de niveles y arte de eventos; gama baja LatAm lo agradecerá.

**Evitar**
- Anuncios forzados tras cada nivel y anuncios recompensados que no pagan (queja #1 y #9; destruye nota en BR/TR).
- Pop-ups apilados de minijuegos en el lobby; máximo un *takeover* por sesión.
- Colores ocultos como mecánica de RNG puro; si se usan, revelarlos con una regla aprendible.
- A/B tests que dejan a dos jugadores de la misma casa con juegos distintos sin explicación.
- Quedarse en 2 redes de UA y en un solo mercado fuerte: Grand tardó un año en salir de Turquía.
- Lanzar con live-ops "sub-optimizados": planificar calendario de 6 meses (evento cada 3-4 semanas rotando 3 plantillas) desde el soft launch.

---

## Fuentes

**Análisis de industria**
- Naavik, "Grand Games and Leveling Up Mobile Casual Puzzle" (19-ene-2025): https://naavik.co/digest/why-peak-games-investors-backed-grand-games/
- Gamigion, "Game Deconstruction: Magic Sort by Grand Games" (O. Fomina): https://www.gamigion.com/game-deconstruction-magic-sort-by-grand-games/
- Gamigion, "Magic Sort Analysis: Grand Games' New Treasure" (A. Yazıcı, 16-ene-2025): https://www.gamigion.com/magic/
- Gamigion, "The 'Why' Behind Grand Games' $30M Investment": https://www.gamigion.com/the-why-behind-grand-games-30m-investment/
- Gamigion, "How is it going, Grand Games?": https://www.gamigion.com/how-is-it-going-grand-games/
- Deconstructor of Fun, "Sort Puzzles: How a New Subgenre is Born" (6-feb-2026): https://www.deconstructoroffun.com/blog/2026/2/6/sort-puzzles-how-a-new-subgenre-is-born
- Deconstructor of Fun, "Grand Games from $0 to $500M in Two Years" (jun-2026): https://www.deconstructoroffun.com/blog/grand-games-from-0-to-500m-in-two-yearsnbsp
- Mobidictum, "Pixel Flow and Peers: Deconstructing Winning Creatives in Sort Puzzle Games" (ago-2026): https://mobidictum.com/pixel-flow-deconstructing-creatives-sort-puzzles/

**Empresa y financiación**
- TechCrunch (9-ene-2025): https://techcrunch.com/2025/01/09/grand-games-the-turkish-gaming-startup-behind-pouring-game-magic-sort-drinks-up-30m/
- Balderton (9-ene-2025): https://www.balderton.com/news/grand-games-raises-30m-series-a-to-build-genre-defining-mobile-games/
- PocketGamer.biz, Series B 70 M (11-may-2026): https://www.pocketgamer.biz/grand-games-raises-70m-series-b-to-scale-hybrid-casual-gaming/
- TNW, Series B: https://thenextweb.com/news/istanbuls-grand-games-lands-a-70m-series-b-as-balderton-doubles-down-on-turkeys-puzzle-game-pipeline
- MobileGamer.biz, data digest: https://mobilegamer.biz/data-digest-sensor-tower-inmobi-and-smartlys-big-buys-grand-lands-70m-huge-applovin-numbers-more/
- Grand Games web y careers (Unity en oferta "Game Developer"): https://grand.gs/ · https://grand.gs/careers/ · https://jobs.lever.co/grand/800e31c8-10af-40eb-9026-701cce7b8c8a · https://jobs.lever.co/grand/4e9fa2b2-abf8-47f3-a4e2-149c42f27a5f

**Fichas de tienda**
- App Store US: https://apps.apple.com/us/app/magic-sort/id6499209744 (y reseñas: `?see-all=reviews`)
- App Store ES/TR/GB/DE/BR/MX/FR/JP/KR/AU/CA: https://apps.apple.com/{es,tr,gb,de,br,mx,fr,jp,kr,au,ca}/app/magic-sort/id6499209744
- Google Play (EN/ES-419/PT-BR/TR): https://play.google.com/store/apps/details?id=com.grandgames.magicsort&hl=en_US
- AndroidRank: https://www.androidrank.org/application/magic_sort/com.grandgames.magicsort
- Uptodown (versiones Android): https://com-grandgames-magicsort.en.uptodown.com/android
- APKCombo: https://apkcombo.com/magic-sort/com.grandgames.magicsort/
- Exodus Privacy (trackers/permisos): https://reports.exodus-privacy.eu.org/en/reports/com.grandgames.magicsort/latest/

**Agregadores de reseñas y guías**
- Glotier (jul-2026): https://glotier.com/games/magic-sort
- mwm.ai: https://mwm.ai/apps/magic-sort/6499209744
- grand-screen.com: https://grand-screen.com/games/magic-sort/
- pg.io (editor review): https://pg.io/store-game/magic-sort
- magicsort.app, guía niveles 1-30: https://magicsort.app/blog/complete-walkthrough-magic-sort-levels-1-30
- levelsolve.com (mecánicas, dificultad): https://levelsolve.com/magic-sort/
- sortpuzzlehelp.com: https://sortpuzzlehelp.com/magic-sort-answers/
- YouTube "Tutorial Workspace": Broom Streak https://www.youtube.com/watch?v=Cp41UN1xKjc · Endless Portal https://www.youtube.com/watch?v=5PyYQM850_A · Bamsi Gaming, niveles 1-10: https://www.youtube.com/watch?v=_LHy9l8uK6Q

**No verificable con fuentes públicas (NC):** reglas exactas y premios de cada evento, qué desbloquea Endless Portal, duración de temporadas de cartas, probabilidades de cofres, color del fondo en niveles hard, lista nominal de los 30 SDKs, nivel exacto de la primera oferta y del primer leaderboard.
