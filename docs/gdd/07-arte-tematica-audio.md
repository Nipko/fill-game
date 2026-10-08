# Dirección de arte, temática y audio

**Proyecto:** Amazon Fill (nombre interno) · **Nombre comercial:** Tintalú (decisión del dueño del 2026-10-06; sustituye a Aquazonia, descartado por la solicitud idéntica INPI 938306200 en Brasil) · **Capítulo:** 07 del GDD · **Fecha:** 2026-10-06 (revisión cruzada; actualizado el 2026-10-07 con la decisión del dueño **D-004**: tope del ancho de la cuia según el número de cuias, §4.1, y vertido a 38° con el labio a 0,6-1,6 u del borde del destino, §5.2; registro en `docs/dev/decisiones.md`) · **Estado:** borrador para aprobación del dueño del producto.

Convención de evidencia: **[V]** verificado en el informe citado · **[E]** estimación del informe · **[R]** reseñas de usuarios · **[NV]** no verificado · **[P]** propuesta de este capítulo a calibrar en soft launch. "(dirección N)" remite al punto N de las decisiones de dirección vinculantes. Nombres de juego, moneda, vidas, comodines, eventos, mundos y mascotas según `docs/gdd/00-glosario.md`. Unidad de medida de la cuia: **u** = altura de una dosis de líquido (sección 4). Unidad de UI: **dp** sobre una pantalla de referencia de 360 × 780 dp (19,5:9).

## Resumen

Tintalú se ve como una lámina de expedición botánica pintada en acuarela y vista a través de vidrio: fondos desaturados y oscuros de la cuenca real (13 mundos, 3 aguas base), cuias de vidrio con cavidad recta y los 14 tintes de fruta como lo más saturado de la pantalla (research/07 §8). El capítulo fija la tabla de mundos ampliada (paisaje, hora, agua, frutas, skin, fauna, instrumento), la paleta con L* y pares de riesgo, la cuia en unidades para que un artista la dibuje y un programador la enmascare, los storyboards de Sumergir y teñir (1,4 s / 0,8 s, saltable), vertido, sellado, victoria y Aguas estancadas, las hojas de Capi y Rosa, el UI kit con contrastes medidos, las pautas culturales operativas, el plan de audio por mundo, los assets de tienda y UA y el conteo de arte de la 1.0. Todo lo que no es trade dress propio de Royal Match / Magic Sort (gatos, caldero, oro biselado, calavera, misma silueta de frasco) queda prohibido por lista.

## 1. Concepto visual

### 1.1 Luz, materiales y saturación

**Estilo: "acuarela botánica sobre vidrio"** (research/07 §8 [E]). Los fondos son pinturas planas con grano de papel, luz difusa y una paleta deliberadamente baja en saturación y valor (verdes oliva, azules pizarra, ocres de orilla), porque el único elemento que debe gritar en la pantalla son los 14 tintes de fruta dentro de las cuias. Esto ataca el problema histórico del género, fondos ruidosos que compiten con los tubos (research/07 §8 [E]), y lo que las reseñas de Magic Sort elogian de verdad no es su castillo sino que es "legible incluso en pantallas pequeñas" (research/magic-sort/c §4 [V]). La luz es siempre la de un día húmedo: sin sol duro ni sombras negras; la fuente principal viene desde arriba-izquierda a 35° para que el brillo del vidrio y el menisco sean consistentes en todas las cuias y skins.

**Materiales**: tres familias y ninguna más. (1) Vidrio: la cuia-garrafa, con refracción insinuada por un brillo diagonal estático y un gradiente lateral oscuro, no por shader de refracción real (research/06 §2.4 [E]). (2) Fibra y madera: borde y pie de las cuias (arumã trenzado, calabaza ennegrecida, madera clara), la bandeja de comodines (cumbuca, research/07 §4 [V]) y los marcos de los modales. (3) Papel: todos los paneles de UI son marfil con grano, como páginas de un cuaderno de campo; el Cuaderno de campo y la Bitácora del río son literalmente ese papel. El metal dorado, las gemas y los biselados quedan excluidos (sección 1.3).

**Saturación por capas**. La pantalla se organiza en tres planos con saturación creciente: fondo (S ≤ 35 %, V ≤ 55 % en HSV), capa media (banda de río, UI de papel y madera, S ≤ 45 %) y primer plano (líquido en las cuias, S 56-98 % según el tinte; sección 3). El agua base de cada mundo (negra #4A2C14, blanca #B08A5A, clara #2E8B8B, research/07 §2 [E]) pertenece a la capa media: es translúcida y solo se ve como banda de río y como relleno inicial durante Sumergir y teñir, nunca compite con un tinte.

**Qué significa "premium" aquí**. No oro ni partículas en todas partes, sino cuatro cosas medibles: líquido con menisco, onda amortiguada y burbujas (research/06 §2 [V/E]); animaciones que nunca frenan el toque siguiente (research/01 Especificación 24 [V]); un momento de autor por nivel, Sumergir y teñir (dirección 18); y coherencia de materiales entre juego, tienda y creativos, de forma que el playable se vea idéntico al juego, el factor que separó el CPI de Magic Sort del water-sort genérico (research/09 §Por qué ganó 4 [V]; research/magic-sort/c §4 [E]).

### 1.2 Reglas medibles de la dirección de arte [P]

| Regla | Valor | Cómo se verifica |
|---|---|---|
| Saturación del fondo | S ≤ 35 %, V ≤ 55 % (HSV) en el 90 % de los píxeles de la zona de tablero | Histograma en Photoshop/Krita sobre el recorte del tablero antes de aprobar un fondo |
| Contraste tinte / fondo | Cada tinte debe diferir del fondo en ≥ 25 puntos de L* en la zona de tablero | Script de QA sobre el fondo y la tabla de la sección 3 |
| Luz | Fuente principal arriba-izquierda, 35°; brillo del vidrio fijo en la misma posición en todas las skins | Hoja de estilo de la cuia (sección 4) |
| Grano de papel | Textura de 2-3 % de opacidad sobre fondos y paneles; nunca sobre el líquido | Capa global en el material de UI |
| Densidad de VFX | ≤ 60 partículas simultáneas en gama baja (research/01 §3 [E] habla de "efectos satisfactorios", no de densidad) | Perfil en dispositivo de referencia de 3-4 GB (GDD 01 §3.3) |
| Paleta de UI | Solo los 8 tokens de la sección 7.2 fuera del líquido | Lint de colores en los prefabs de UI |
| Antropomorfismo | Fauna de trazo simple, sin ropa ni accesorios (research/07 §8 [E]) | Revisión del dueño + asesor cultural (sección 8) |

### 1.3 Premium sin copiar el trade dress de Royal Match / Magic Sort

Las reglas de un juego no son protegibles, su presentación sí (*Tetris v. Xio*, *Spry Fox v. LOLApps*, research/08 §4.1 [V]); Apple 4.1 "Copycats" y la política de PI de Play retiran por queja del titular (research/08 §4.1 [V]). Lista cerrada de lo que **no** aparece en Tintalú y qué hacemos en su lugar:

| Elemento ajeno | Fuente que lo identifica | Qué hacemos en Tintalú |
|---|---|---|
| Gatos magos, tres gatos guía de capítulo, gato con caldero al pie de la pantalla | research/09 §Qué NO copiar [V]; research/magic-sort/c §4 [V] | Capi (capibara) y Rosa (boto); Capi nunca fijo en el centro inferior con un objeto (glosario §3.1) |
| Caldero, ingredientes místicos, escobas, varitas, portales morados, slime | research/09 §Qué NO copiar [V] | Fenómenos naturales y fauna como comodines (Marea, Pororoca, Morpho, Luciérnaga) |
| UI de botones biselados dorados con brillo, gemas, cintas rojas | research/magic-sort/c §4 [E]; research/09 §Arte [E] | Botones "piedra de río" planos en madera y verde hoja sobre papel marfil (sección 7.3) |
| Saturación alta y luz cálida "de cuento" con fondos de bosque místico/castillo | research/09 §Arte [E] | Fondos desaturados de lugares reales, luz húmeda difusa (1.1) |
| Botellas de vidrio con la misma silueta (cilindro con labio) | research/09 §Qué NO copiar [V]; research/08 §4.1 (silueta de IEC/Guru) [E] | Cuia-garrafa con hombro, cuello corto, pie de fibra (sección 4) |
| Calavera como icono de Hard / Super Hard; fondo morado/rojo de nivel difícil | research/09 §Dificultad [V]/[E] | Rana dardo (Difícil) y jaguar (Muy difícil) en marcos ámbar e índigo (sección 7.7) |
| Multiplicador ×2→×100 dibujado como medidor dorado | research/09 §Meta [V] | La Crecida es una barra vertical de nivel de agua en el borde derecho del tablero (7.4) |
| HUD de IEC/Guru: deshacer / pista / +tubo en la misma posición y con los mismos iconos | research/08 §4.1 [E] | Bandeja cumbuca con iconos propios y orden propio (sección 7.4) |
| Paleta exacta de 12 colores de IEC; fuente tipográfica reconocible de un líder | research/08 §4.1 [E] | 14 tintes de frutas reales con HEX propios; tipografías OFL (sección 7.1) |
| Composición de capturas, textos de ficha y vídeo de la competencia | research/09 §Qué NO copiar [V] | Ficha redactada desde cero (sección 10) |
| Sonrisa/flecha de Amazon, naranja #FF9900 sobre negro | research/08 §4.2 [V] | Icono con cuia y río; semilla ámbar sobre marfil (glosario §2.1) |

## 2. Mundos: tabla canónica ampliada

Canon de orden, país, río y rangos: research/02 C3.3 [E] vía glosario §8 (dirección 16). Paisajes: research/07 §1 [V]. Agua base: 3 presets de shader, no 13 (research/07 §Implicaciones 2); el HEX es el del preset (research/07 §2 [E]). Hora del día, fauna de ambiente e instrumento son dirección de arte [P] salvo cita. Las variantes lluvia/noche/alba/crecida/vaciante se producen por LUT de iluminación, partículas y altura de la banda de agua sobre la misma pintura (research/02 C3.4 [E]); la columna "Hora" fija la pintura base de la vuelta 1.

### 2.1 Paisaje, hora, agua y entrega

| # | Mundo · país | Niveles | Paisaje de fondo (2-3 referencias reales, research/07 §1 [V]) | Hora del día [P] | Agua base (preset · HEX) | Entrega |
|---|---|---|---|---|---|---|
| 1 | **Estuario** · Brasil (Pará) | 1-10 | Ver-o-Peso (Belém, mercado de hierro de 1901) visto desde el agua; orillas de Marajó con búfalos lejanos; palmeras de açaí | Alba dorada, bruma baja | Blanca · #B08A5A | AAB (install-time) |
| 2 | **Tapajós** · Brasil (Pará) | 11-40 | Playa de Alter do Chão (Ilha do Amor) con arena blanca; encuentro del Tapajós azul con el Amazonas lechoso frente a Santarém | Mediodía luminoso, cielo alto | Clara · #2E8B8B | AAB |
| 3 | **Manaos** · Brasil (Amazonas) | 41-80 | Encuentro de las Aguas (línea negra/beige en diagonal); archipiélago de Anavilhanas; silueta lejana del Teatro Amazonas | Tarde, luz rasante | Negra · #4A2C14 | AAB |
| 4 | **Trapecio** · Colombia | 81-140 | Malecón de Leticia; lagos de Puerto Nariño / Amacayacu; tepuyes de Chiribiquete solo como silueta (sin pictografías) | Mañana nublada tras lluvia | Blanca · #B08A5A | AAB |
| 5 | **Loreto** · Perú | 141-200 | Barrio flotante de Belén (Iquitos) y Casa de Fierro; "selva de espejos" de Pacaya-Samiria; confluencia Marañón-Ucayali en Nauta | Atardecer reflejado en agua quieta | Blanca · #B08A5A | PAD on-demand (1.0) |
| 6 | **Oriente** · Ecuador | 201-300 | Lagunas negras de Cuyabeno; dosel de Yasuní; río Napo ancho | Crepúsculo y noche temprana | Blanca · #B08A5A (fondo nocturno) | PAD (1.1); en 1.0 fondo "río genérico" [P] |
| 7 | **Madre de Dios** · Perú | 301-420 | Collpa de guacamayos (pared de arcilla); lago Sandoval con palmeras aguaje; río Tambopata | Amanecer (hora de la collpa) | Blanca · #B08A5A | PAD (1.1); en 1.0 fondo "río genérico" [P] |
| 8 | **Madidi** · Bolivia | 421-560 | Madidi de 180 a 5.760 m (selva con Andes al fondo); cascada Arcoiris de Noel Kempff (88 m); meseta Huanchaca | Mediodía con niebla de cascada | Blanca · #B08A5A (hitos: clara) | PAD (1.1); en 1.0 fondo "río genérico" [P] |
| 9 | **Ucayali** · Perú | 561-700 | Pucallpa y el Ucayali meándrico; Shanay-Timpishka (río hirviente, ~95 °C) con vapor; Pongo de Manseriche | Mañana con vapor | Blanca · #B08A5A (partículas de vapor en especiales) | PAD (después de 1.1, 09 §3.1 P3); hasta entonces, fondo "río genérico" [P] |
| 10 | **Alto Negro** · Venezuela / Brasil | 701-820 | Canal del Casiquiare; Serranía de la Neblina y Pico da Neblina solo como silueta lejana (sagrado yanomami, research/07 §1) | Noche estrellada | Negra · #4A2C14 | PAD (después de 1.1, 09 §3.1 P3) |
| 11 | **Escudo de Guayana** · Guyana / Surinam / Guayana Francesa | 821-920 | Sabana del Rupununi y montes Kanuku; inselberg Voltzberg; río Maroni | Tarde de sabana, cielo enorme | Blanca · #B08A5A | PAD (después de 1.1, 09 §3.1 P3) |
| 12 | **Invitados del Orinoco** · Venezuela / Colombia | 921-960 | Caño Cristales rojo (jul-nov); tepuyes de Canaima y Salto Ángel (979 m) al fondo; rótulo permanente "vecinos del Orinoco" (research/07 §1 [V]) | Mediodía | Clara · #2E8B8B | PAD (después de 1.1, 09 §3.1 P3) |
| 13 | **El origen** · Perú (Andes) | 961-1.000 | Cañón del Marañón (3.000 m); nevados y puna; el río como hilo entre rocas | Alba fría de altura | Blanca · #B08A5A con LUT fría [P] | PAD (después de 1.1, 09 §3.1 P3) |

**Fondos de la 1.0** (canon compartido con glosario §8, 03 §17 y 08 §6.2): fondos 1-4 install-time + Loreto (141-200) por PAD; "río genérico" para 201-700 (mundos 6-9) hasta que lleguen sus packs. El mismo "río genérico" cubre cualquier mundo cuyo pack aún no esté descargado al llegar a él (08 §6.2); la lista de piezas está en la sección 11.

### 2.2 Frutas, skin, fauna e instrumento

| # | Mundo | Tintes que debutan (glosario §8) | Skin de cuia (glosario §4) | Fauna de ambiente (research/07 §5 [V dato]; uso [P]) | Instrumento / género del lecho musical |
|---|---|---|---|---|---|
| 1 | Estuario | Açaí, Aguaje (1), Taperebá (2), Guanábana (4), Bacaba (7) | Cuia de vidrio (base) | Garzas en la orilla; familia de capibaras (hogar de Capi) | Carimbó: tambor curimbó y maracás (Pará, Patrimonio IPHAN 2014, research/07 §6.3 [V]) |
| 2 | Tapajós | Agua del Tapajós (11), Camu camu (21) | Cuia de Santarém (cumatê negro, grabado propio) | Tucanes; primera aparición de Rosa (boto) | Carimbó de playa, tempo más lento, maracá y guitarra [P] |
| 3 | Manaos | Urucum (41), Agua del Río Negro (61) | Cuia del Encuentro (banda bicolor ámbar/beige) | Botos en el Encuentro; guacamayos cruzando | Toada amazónica de estilo propio, sin referencia a Parintins ni a sus asociaciones (Garantido/Caprichoso, glosario §3.3); mismo literal que 06 §10 [P] |
| 4 | Trapecio | Copoazú (81), Lulo (111) | Totuma (calabaza clara con asa de cuero) | Perezoso en una rama; morphos | Guitarra acústica y flauta de caña sin carácter ritual [P] |
| 5 | Loreto | Huito (141) | Huingo (borde de chambira) · 1.1 | Pirarucú que sale a respirar (dato de conservación, research/07 §5 [V]) | Cumbia amazónica suave, tempo bajo (género de Pucallpa/Iquitos, research/07 §6.3 [V género]; uso [P]) |
| 6 | Oriente | Cacao (201) | Borde de Pastaza (engobe con diseño propio) · 1.1 | Luciérnagas, ranas dardo sobre hojas | Percusión de semillas y cuencos de agua, ambiente nocturno [P] |
| 7 | Madre de Dios | Arazá (301): paleta completa | Pilche (pie de madera) · 1.1 | Guacamayos en la collpa (research/07 §1 [V]); nutrias gigantes en el lago [P] | Guitarra y cajón ligero [P] |
| 8 | Madidi | — | Tutuma (cordón de lana) · 1.1 | Aves de la transición Andes-selva (1.254 especies, research/07 §1 [V]) | Charango suave y bombo [P] |
| 9 | Ucayali | — (hitos con 13 colores; 14 solo en el evento Encuentro de las Aguas) | Tinaja (cerámica de cuello corto, patrón propio) · 1.1 | Paiche; vapor del río hirviente | Cumbia amazónica / chicha: Farfisa y guitarra con wah-wah (Pucallpa, research/07 §6.3 [V]) |
| 10 | Alto Negro | — | Tapara (borde oscuro) · 1.1 | Luciérnagas, ranas; estrellas reflejadas | Lecho nocturno: pad grave y maracá lento [P] |
| 11 | Escudo de Guayana | — | Calabaza de asa (fibra del Rupununi) · 1.1 | Hoatzin (ave nacional de Guyana, research/07 §5 [V]); arapaima | Percusión de calabaza y voces sin letra [P] |
| 12 | Invitados del Orinoco | — (skin de evento) | Skin de temporada "Río de cinco colores" | Guacamayos sobre tepuyes | Arpa y maracas de los llanos del Orinoco (fuera de la cuenca, coherente con el rótulo) [P] |
| 13 | El origen | — (hitos con 13 colores; 14 solo en el evento Encuentro de las Aguas) | Cántaro andino (cerámica roja) · 1.1 | Siluetas de aves de altura; sin fauna en primer plano | Quena y zampoña, permitidas solo aquí (research/07 §6.3 [V]) |

Reglas transversales: (1) el banner de entrada a cada mundo muestra "Mundo {n}: {nombre} · {país} · {río}" con Capi presentando (glosario §10); (2) ninguna aldea identificable, ninguna persona, ninguna pictografía (research/07 §1 [V]); (3) el mundo 12 lleva el rótulo "vecinos del Orinoco" en el banner y en el Dato del río; (4) desde la vuelta 2 (nivel 1.001) las variantes de LUT reciclan las 13 pinturas (research/02 C3.4 [E]).

## 3. Paleta de 14 tintes

HEX, patrón e icono de research/07 §3.2 [E] vía glosario §9; L* calculado por este capítulo sobre el HEX (CIELAB, D65). La paleta escalona luminosidad dentro de cada familia de tono porque una paleta de 14 nunca es segura solo por color (Tol califica su esquema de 14 de "razonablemente" seguro; Okabe-Ito pide formas y patrones, research/07 §3.2 [V]).

| # motor | ID | Tinte | HEX | L* | Patrón (textura al 15-20 % en la dosis) | Icono de accesibilidad | Pares de riesgo | Debut (mundo · nivel) | Regla de uso en arte |
|---|---|---|---|---|---|---|---|---|---|
| 1 | `guanabana` | Guanábana (blanco) | #FAFAF7 | 98 | liso | fruto espinoso | 2 (baja visión) | 1 · 4 | Siempre con contorno interior de 1 px #2B2118 al 35 %; la cavidad vacía es más oscura que el marfil de UI (4.2) para que el blanco lea |
| 2 | `cupuacu` | Copoazú (marfil) | #E6D3A3 | 85 | puntos finos | vaina ovalada | 1 | 4 · 81 | Nunca junto a 1 antes del 201; menisco +20 % satura, no aclara |
| 3 | `tapereba` | Taperebá (amarillo dorado) | #F2B705 | 78 | puntos grandes | ciruela | 12 | 1 · 2 | No usar su HEX en UI (la semilla se distingue por forma, glosario §2.1) |
| 4 | `aguaje` | Aguaje (naranja) | #EF7D1A | 64 | escamas | fruto escamoso | 5, 14 | 1 · 1 | Tinte del tutorial junto a Açaí (contraste L* 64 vs 32) |
| 5 | `urucum` | Achiote (bermellón) | #CF3E1C | 48 | triángulos | cápsula erizada | 4, 6 | 3 · 41 | Debuta con el patrón ya obligatorio (nivel 41) |
| 6 | `camu_camu` | Camu camu (carmín) | #9E1B22 | 34 | rayas verticales | cereza | 5, 13 | 2 · 21 | Menisco a +25 % de luminosidad (tinte oscuro) |
| 7 | `bacaba` | Bacaba (rosa lila) | #E58FC6 | 70 | ondas | racimo | — | 1 · 7 | Sin pares; libre |
| 8 | `acai` | Açaí (violeta) | #6A2C91 | 32 | rombos | bol (tigela) | 9 | 1 · 1 | Tinte del tutorial; icono del juego (sección 10.1) |
| 9 | `jenipapo` | Huito (índigo oscuro) | #1F2F6B | 22 | cuadrícula | fruto con sombra | 8 | 5 · 141 | Menisco a +30 %; patrón en marfil al 20 % |
| 10 | `agua_tapajos` | Agua del Tapajós (turquesa) | #3BB8D9 | 70 | líneas onduladas | gota | 11 | 2 · 11 | No usar sobre el preset de agua clara (#2E8B8B) sin la banda de río atenuada |
| 11 | `lulo` | Lulo (verde) | #4EB265 | 65 | chevrons | fruto peludo | 10, 12 | 4 · 111 | Evitar fondo con verdes de L* 55-75 en la zona de tablero |
| 12 | `araza` | Arazá (lima) | #BBCC33 | 79 | medias lunas | fruto aterciopelado | 3, 11 | 7 · 301 | Último debut: paleta completa |
| 13 | `cacao` | Cacao (chocolate) | #5C3317 | 26 | cruzado | mazorca | 6, 14 | 6 · 201 | Menisco a +30 %; nunca junto al agua parda de Aguas turbias sin el "?" |
| 14 | `agua_negro` | Agua del Río Negro (ámbar) | #A9672E | 50 | moteado | hoja hundida | 4, 13 | 3 · 61 | Distinto del preset de agua negra (#4A2C14) por 29 puntos de L* |

Reglas de aplicación (dirección 3; research/07 §3.2 [E]; research/02 C3.2 [E]):

1. **Patrón**: textura del tinte dentro de la dosis a 15-20 % de opacidad (research/07 §8). Hasta el nivel 40 se dibuja sutil (15 %); desde el 41 el toggle Patrones de fruta arranca activado y la opacidad sube a 20 % con el icono de fruta centrado en cada dosis a 0,45 u de alto; el toggle existe siempre en Ajustes. Valores canónicos para todo el GDD: **20 %** con Patrones de fruta activo (`ui_pattern_opacity`, clave de 08 §9) y **35 %** solo en Alto contraste (regla 5); cualquier capítulo que cite otra cifra remite a esta.
2. **Icono al tocar**: al seleccionar una cuia, cada dosis muestra su icono de fruta durante la elevación aunque el toggle esté apagado (coste cero, ayuda a los pares 4-5 y 8-9).
3. **Pares de riesgo**: `max_pares_riesgo` 0 (1-40), 1 (41-80), 2 (81-200), libre desde 201; el validador del generador lo aplica (research/02 C3.5 [E]). El arte no decide esto, pero el QA de arte simula protanopía, deuteranopía y tritanopía sobre el shader real antes de aprobar cada tinte (research/07 Preguntas abiertas).
4. **Menisco**: línea de 2-3 px con el color del tinte a +20 % de luminosidad (research/06 §2.4 [E]); +25-30 % en los tintes de L* < 35 (6, 8, 9, 13) para que el borde superior se vea.
5. **Modo alto contraste** (Ajustes): sube la opacidad del patrón a 35 % y el contorno de dosis a 1,5 px; no cambia los HEX.
6. **Prohibido** usar los HEX de los tintes en UI, botones o texto; la UI vive en los tokens de la sección 7.2.

## 4. Diseño de la cuia

### 4.1 Silueta y proporciones

Cuia-garrafa: silueta de cabaça alargada, hombro suave, cuello corto, base redondeada, vidrio transparente con borde y pie de fibra trenzada o madera (research/07 §4 [V]; glosario §4). El truco de producción es que la **cavidad interior es un cilindro recto** y solo la pared exterior es curva: las 4 dosis miden exactamente lo mismo (research/07 §4 [E]). Proporción exterior 1:3 (ancho:alto) para dos filas de hasta 7 cuias a 360 × 780 dp (research/07 §4 [E]).

| Zona (de abajo arriba) | Altura (u) | Rango y (u) | Ancho exterior (u) | Ancho interior (u) | Notas para el artista |
|---|---|---|---|---|---|
| Pie de fibra | 0,30 | 0,00-0,30 | 1,60 (anillo) | — | Aro trenzado; cambia por skin |
| Base de vidrio | 0,30 | 0,30-0,60 | 1,60 → 1,90 | 1,30 (fondo plano de la cavidad en y = 0,60) | Vidrio más grueso: aquí vive el gradiente oscuro de "fondo de botella" |
| **Cavidad (4 dosis)** | **4,00** | **0,60-4,60** | 1,90 → 2,00 (máx. en y ≈ 4,2) | **1,30 constante** | 4 marcas de dosis grabadas en y = 1,60 / 2,60 / 3,60 / 4,60, 1 px, marfil al 25 % |
| Hombro | 0,60 | 4,60-5,20 | 2,00 → 1,00 | 1,30 → 0,80 | Curva suave; zona de la banda decorativa de cada skin |
| Cuello | 0,55 | 5,20-5,75 | 1,00 | 0,80 | Vidrio; el labio de vertido está en y = 5,75 |
| Borde de fibra / madera | 0,25 | 5,75-6,00 | 1,10 (anillo) | 0,80 | Aro; cambia por skin |
| **Total** | **6,00** | | **2,00** | | Ratio 1:3 |

Tamaños en pantalla (canon de layout: GDD 02 §5, margen lateral total de 24 dp): ranura = (360 − 24) / cuias_por_fila; ancho de cuia = clamp(0,70 × ranura, 28, tope(T)) dp, donde tope(T) = 64 dp si el tablero tiene T ≤ 3 cuias, 56 dp si T = 4-5 y 48 dp si T ≥ 6 (T cuenta todas las cuias en pantalla, incluidas Cuia extra y Cuia de rescate; D-004, decisión del dueño del 2026-10-07); alto = 3 × ancho; **u = ancho / 2**. Con 7 por fila: ranura 48 dp, cuia **34 × 102 dp, u = 17 dp**. Con 6 por fila: ranura 56 dp, cuia 39 × 117 dp, u ≈ 19,5 dp. Con 5 por fila: ranura 67,2 dp, cuia ≈ 47 × 141 dp, u ≈ 23,5 dp (el tope no actúa). Con 3 o 4 por fila y T ≥ 6: cuia 48 × 144 dp, u = 24 dp. Con T = 4 (una fila de 4): cuia 56 × 168 dp, u = 28 dp. Con T ≤ 3: cuia 64 × 192 dp, **u = 32 dp** (la máxima). Todas las medidas en u de este capítulo se convierten con la u de la fila en la que se dibuja la cuia. La fuente se dibuja en vector o a 4× del tamaño máximo (256 × 768 px por cuia de 4) y se exportan atlas a 1×, 2× y 3×. Estos valores se confirman en la prueba de legibilidad 2 × 7 de la fase 0 (roadmap); si cambian, se actualizan aquí y en 02 §5 a la vez, nunca en un solo capítulo.

### 4.2 Zona de capas, menisco, brillo y sombra

- **Líquido**: un quad por cuia que cubre exactamente la cavidad (1,30 × 4,00 u), recortado por SpriteMask con la silueta interior (research/06 §2.1-2.2 [V/E]). El shader recibe hasta 6 colores y alturas acumuladas; cambiar dosis = cambiar uniformes.
- **Fondo de cavidad vacía**: la cavidad sin líquido se pinta con el fondo del mundo multiplicado por 0,82 y una viñeta lateral de 0,15 u al 30 % de negro, para que Guanábana (#FAFAF7) y Copoazú lean sobre cualquier fondo (sección 3, regla 1).
- **Superficie**: onda senoidal con amplitud `A(t)` que decae en 0,5-0,8 s tras cada vertido (research/06 §2.3 [E]); menisco de 2-3 px (regla 4 de la sección 3).
- **Brillo del vidrio**: franja diagonal fija de 0,18 u de ancho desde (0,25 u, 5,0 u) hasta (0,55 u, 1,0 u), marfil al 18 %; segundo brillo puntual en el hombro derecho al 10 %. Idéntico en todas las skins.
- **Gradiente lateral**: 0,20 u en cada borde de la cavidad, negro al 20 %, para dar curvatura (research/06 §2.4 [E]).
- **Burbujas**: 5-10 partículas de 0,06-0,10 u dentro de la máscara, solo durante el vertido y 1 s después (research/06 §2.5 [E]).
- **Patrón e icono**: textura del tinte y, con el toggle, icono de 0,45 u centrado en cada dosis (sección 3).

### 4.3 Estados visuales

| Estado | Nombre (glosario) | Cambio visual | Duración |
|---|---|---|---|
| Reposo | — | Como 4.2 | — |
| Seleccionada (origen) | — | La cuia sube 16 dp (02 §4.1; ≈0,94 u con 7 por fila y 0,5 u con la cuia de 64 dp), sombra bajo el pie se separa, iconos de fruta visibles en cada dosis; ninguna otra cuia cambia: no se resalta ni se sugiere ningún destino (D-002, decisión del dueño del 2026-10-06; 02 §3.1) | 0,12 s subida (`anim_lift_ms`, 02 §4.1 / 08 §9) |
| Vertiendo | — | Inclinación 38° hacia el destino; el líquido no rota con el vaso (shader, research/01 §3 [V]); chorro Bézier desde el labio | Ver 5.2 |
| Vertido ilegal | — | La cuia tocada hace una sacudida horizontal ±6 dp, 2 ciclos, 120 ms (02 §4.1) y el origen, si lo hay, baja a su sitio (02 §3.1, C2 y C5-C6); sonido neutro; sin texto (research/01 Especificación 9 [V]) | 0,12 s |
| Completa | **Cuia sellada** | Tapa de calabaza, aro de luz, no seleccionable; vidrio y líquido quedan igual (no se desatura) | Ver 5.3 |
| Capas ocultas | **Aguas turbias** | Dosis oculta = agua parda opaca #8A7B6A con trama diagonal al 25 % y "?" marfil de 0,5 u (glosario §6.2). En el shader `Liquid` (08 §5.1-5.2) las dosis ocultas se marcan con la máscara de bits `_HiddenMask` (hasta 6 capas, una por dosis; los ocultos pueden ser no contiguos) y se pintan con `_MurkyColor` = #8A7B6A; este capítulo es la fuente de ambos valores. Al revelarse por la regla del nivel la trama se disuelve y la dosis se aclara hasta su tinte en 300 ms (Revelado, 02 §4.1); con Luciérnaga rige la secuencia de 04 §2.5 | — |
| Bloqueada por mecánica | Hoja / Liana / Brote… | Overlay por mecánica; lo fija el capítulo de núcleo | — |
| Bloqueo del tablero | **Aguas estancadas** | Superficies quietas, fondo −20 % de saturación, niebla fina | Ver 5.5 |

### 4.4 Sellado (Cuia sellada)

Tapa de calabaza: casquete de madera clara (#C9A36B [P]) de 1,10 u de diámetro y 0,35 u de alto, con una veta y un brillo; apoya sobre el borde de fibra. **No es una hoja**, para no colisionar con la mecánica Hoja (glosario §4). Sonido de maracá y háptico ligero (research/01 Especificación 10 [V]; research/07 §6.3 [V]).

### 4.5 Variantes de capacidad

| Variante (glosario) | Capacidad | Altura total (u) | Cavidad (u) | Cómo se reconoce | Uso |
|---|---|---|---|---|---|
| Cuia chica | 1 | 3,00 | 1,00 | Misma base, hombro, cuello y borde; una sola marca; el hombro empieza en y = 1,60 | Buffer de una dosis; debut de la capacidad 1 en el nivel 18 (02 §8; dirección 4) |
| Cuia chica | 2 | 4,00 | 2,00 | Dos marcas | Debut de la Cuia chica: nivel 14 (02 §8) |
| Cuia chica | 3 | 5,00 | 3,00 | Tres marcas | Desde el nivel 22 (02 §8) |
| Cuia (estándar) | 4 | 6,00 | 4,00 | Cuatro marcas | Base |
| Cuia alta | 5 | 7,00 | 5,00 | Cinco marcas; cuello idéntico, cuerpo más largo | Desde el nivel 27 (canon 02 §8) |
| Cuia alta | 6 | 8,00 | 6,00 | Seis marcas | Solo niveles hito desde el 141 (primero: 160; canon 02 §8) |

La fila que contiene una Cuia alta alinea todas las cuias por el **pie**, no por el centro, para que el ojo compare alturas de líquido. Las Cuias chicas se alinean también por el pie. Los niveles con Cuia alta se limitan a 2 filas (GDD 02 §5); si aun así la fila (8,0 u de alto más 1,0 u de elevación) no cabe en el paso de fila, u baja por debajo del valor de 4.1 hasta un mínimo de 14 dp (cuia de 28 dp de ancho, el mínimo de 02 §5).

### 4.6 Skins: qué cambia y qué no

Las skins se ganan por hitos y se venden en packs de 1,99 USD (dirección 14). Para que las variantes de capacidad funcionen con cualquier skin, la cuia se construye por capas:

| Capa del prefab | Varía con | Qué contiene |
|---|---|---|
| `glass_body_{cap}` | Capacidad (6 sprites) | Vidrio, brillo, gradiente lateral, marcas de dosis |
| `liquid_quad` | Nada | Shader con máscara interior |
| `rim_{skin}` y `foot_{skin}` | Skin (2 sprites por skin) | Borde y pie: fibra, madera, cumatê, cuero |
| `band_{skin}` | Skin (1 sprite por skin, zona del hombro 4,60-5,20 u) | Decoración propia; nunca cubre la cavidad |
| `glass_tint_{skin}` | Skin (color) | Tinte del vidrio ≤ 8 % de opacidad (p. ej., ámbar en Cuia de Santarém) |
| `cap_{skin}` | Skin (1 sprite) | Tapa de sellado |

| Skin 1.0 | Mundo | Rim / pie | Banda del hombro | Tinte del vidrio | Tapa |
|---|---|---|---|---|---|
| Cuia de vidrio | 1 Estuario | Fibra de arumã trenzada, ocre | Ninguna | 0 % | Madera clara |
| Cuia de Santarém | 2 Tapajós | Calabaza ennegrecida (cumatê), con grabado geométrico **propio** (no copiado de las cuias de Aritapera, research/07 §4 [NV registro]) | Línea fina marfil incisa | Ámbar 6 % | Cumatê negro con incisión |
| Cuia del Encuentro | 3 Manaos | Madera oscura | Banda bicolor ámbar/beige en diagonal que no se mezcla (Negro + Solimões) | 0 % | Dos tonos |
| Totuma | 4 Trapecio | Calabaza clara con asa de cuero (el asa es decorativa, en el hombro derecho, 0,4 u) | Cuero | Verdoso 5 % | Calabaza clara |

Las nueve skins restantes (glosario §4) siguen la misma plantilla en 1.1. Regla: una skin jamás cambia la cavidad, las marcas, la posición del labio ni el brillo.

## 5. Storyboards

### 5.1 Sumergir y teñir (carga de nivel)

Reglas de la dirección 18: 1,2-1,5 s completa en los niveles 1-20 (**1,4 s** = `anim_tint_duration_ms_early` 1.400), **0,8 s** desde el 21 (`anim_tint_duration_ms_late` 800; el cambio lo fija `anim_tint_fast_from_level` = 21), saltable con un toque desde el primer fotograma (`anim_tint_skippable`, nunca false en producción), solo arranca con el nivel ya en memoria; el agua base cambia por mundo. Claves, tipos (int, ms) y defaults: 08 §9. **Fases y totales: canon 02 §4.2** (0,30 + 0,40 + 0,25 + ≤ 0,45 solapado = 1,4 s; versión corta 0,15 + 0,25 + 0,15 + 0,25 = 0,80 s); este capítulo detalla qué se ve y se oye en cada fase. Satisfacción medida del 70 % con esperas de ~1,6 s y 50 % con ~2,7 s; las animaciones con significado semántico reducen la espera percibida (research/06 §2 [V]). El esquema a-b-c-d es el de research/06 §Animación [E]; el teñido por fruta, el de research/07 §8 [E].

| Fase | Versión 1,4 s | Versión 0,8 s | Qué se ve | Audio |
|---|---|---|---|---|
| 0. Estado inicial | t = 0,00 | 0,00 | HUD visible con número de nivel y etiqueta; banda de río en reposo al pie del tablero; cuias vacías 2,0 u por encima de su posición final, alfa 100 %. Toque en cualquier punto = salto | Lecho del mundo ya sonando |
| a. Caída | 0,00-0,30 | 0,00-0,15 | Las cuias caen con ease-in hasta su posición; escalonado de hasta 0,015 s / 0,008 s de izquierda a derecha, repartido para que la última aterrice al cerrar la fase | Golpes suaves de madera, uno por cuia, −12 dB |
| b. Inmersión | 0,30-0,70 | 0,15-0,40 | La banda de río sube 1,5 u y cubre hasta el hombro; ondas concéntricas en cada cuia; 6 partículas de salpicadura (≤ 20 en total, 08 §5.3); el agua base del mundo entra en la cavidad y llena las 4 u (o la capacidad) con la onda de superficie activa, color del preset (negra / blanca / clara) al 70 % de opacidad | `sfx_submerge` (chapuzón grave + burbujas) |
| c. Emersión | 0,70-0,95 | 0,40-0,55 | La banda baja a su altura de reposo; 3-4 gotas caen de cada cuia; el agua base queda quieta | Goteo |
| d. Teñir | 0,95-1,40 (≤ 0,45 s, solapado) | 0,55-0,80 (0,25 s en total) | Por cuia, un icono de fruta por dosis cae desde fuera de pantalla y se detiene a la altura de su dosis (la de arriba primero); cada franja se tiñe en **0,10 s** (una franja = 0,10 s, 02 §4.2): el tinte florece desde el centro de la dosis hacia los lados con interpolación OKLab para no pasar por grises (research/06 §2.7 [E]) y el icono se funde en el mismo tramo. Escalonado entre dosis de la misma cuia 0,08 s / 0,04 s (menor si la cuia tiene 5-6 dosis) y entre cuias el margen restante, repartido para que la última dosis termine exactamente en t = 1,40 / 0,80. En Aguas turbias, la dosis oculta recibe una gota parda y el "?" | `sfx_tint` por dosis (tono corto, afinado por tinte: 14 notas de una escala pentatónica) |
| e. Listo | 1,40 | 0,80 | Onda de superficie decae 0,5 s; input habilitado desde el fotograma final de d | — |

```pseudo
procedure PlayLoadAnim(level, rc):                   // level ya parseado, texturas residentes
    require level.loaded and textures.resident
    early = level.index < rc.anim_tint_fast_from_level            // 21 (08 §9)
    T = early ? rc.anim_tint_duration_ms_early : rc.anim_tint_duration_ms_late   // 1400 / 800 ms
    [A, B, C, D] = early ? [300, 400, 250, 450] : [150, 250, 150, 250]           // fases a-d (02 §4.2); A+B+C+D = T
    n = level.cuias.count
    timeline = new Timeline()
    for i, cuia in enumerate(level.cuias):
        timeline.add(min(15, A*0.3/max(1,n-1)) * i, Drop(cuia, 2.0u, A, easeIn))
    timeline.add(A, RiverRise(1.5u, B)); timeline.add(A, FillBase(all, waterPreset(level.world), B*0.7))
    timeline.add(A+B, RiverFall(C)); timeline.add(A+B, Drips(all))
    franja = 100                                                                 // una franja = 100 ms
    maxCap = max(c.capacity for c in level.cuias)
    dDose = min(early ? 80 : 40, (D - franja) / max(1, maxCap-1))                // escalonado entre dosis
    dCuia = (D - franja - dDose*(maxCap-1)) / max(1, n-1)                        // reparte el margen entre cuias
    for i, cuia in enumerate(level.cuias):
        for d in reversed(range(cuia.capacity)):                                 // de arriba hacia abajo
            t = A+B+C + dCuia*i + dDose*(cuia.capacity-1-d)
            timeline.add(t, FruitDropAndBloom(cuia, d, tint(cuia,d), franja, OKLab))   // la última termina en T
    timeline.add(T, EnableInput)
    onAnyTap if rc.anim_tint_skippable: timeline.Complete()                      // salta al estado final en 1 frame
              crossfade(fruitIcons.alpha -> 0, 100ms); RiverFall(150ms); EnableInput
              analytics.log("af_anim_skipped", {anim: "tint", at_ms: t_skip, duration_cfg_ms: T})   // taxonomía de 08 §8.2; level va en los parámetros comunes
    timeline.play()
```

Reglas: si `af_anim_skipped` supera `anim_tint_skip_alarm_pct` (40 %) en los niveles 1-20, `anim_tint_duration_ms_early` baja a **1.200 ms** por Remote Config (mínimo de la dirección 18; nunca por debajo; research/06 §Animación [E]); el nivel N+1 se precarga mientras se juega N; con "Reducir movimiento" activo la animación se reemplaza por un fundido de 0,25 s. El banner de mundo (primer nivel de cada mundo) se muestra **antes** de esta animación, no encima.

### 5.2 Vertido

Tiempos: canon 02 §4.1 (horquillas de research/01 §3 [E]; ninguna fuente publica los de los líderes) con las claves `anim_lift_ms` 120 / `anim_travel_ms` 200 / `anim_pour_dose_ms` 100 / `anim_pour_tail_ms` 80 / `anim_return_ms` 160, defaults en 08 §9; todas se multiplican por `anim_speed_factor_early/late` y por `anim_reduce_motion_factor` (02 §4). El estado lógico se actualiza al instante y el siguiente toque se encola (research/01 Especificación 24 [V]).

| t (s) | Fotograma | Detalle |
|---|---|---|
| 0,00 | Toque en origen | La cuia sube 16 dp (02 §4.1; ≈0,94 u con 7 por fila y 0,5 u con la cuia de 64 dp) en 0,12 s (`anim_lift_ms`, ease-out); iconos de fruta visibles |
| 0,12 | Toque en destino | La cuia viaja hasta dejar el labio entre 0,6 y 1,6 u por encima del borde del destino (0,6 u más una elevación de 0 a 1 u, la que menos tapa a las otras cuias; D-004) e inclina 38° en 0,20 s (`anim_travel_ms`; arco, no línea recta); con 38° y dos filas el origen puede tapar la parte alta de una cuia vecina mientras vierte (≤ 400 ms), lo que el dueño aceptó (D-004); la onda del origen se agita |
| 0,26 | Chorro | Arranca al 70 % del traslado (02 §4.1). Bézier cuadrática desde el labio (y = 5,75 u) hasta la superficie del destino con LineRenderer, grosor 0,16 → 0,10 u, UV desplazándose (research/06 §2.6 [E]); color del tinte; la dosis de destino crece mientras la de origen decrece, **0,10 s por dosis + 0,08 s de cola** (`anim_pour_dose_ms` / `anim_pour_tail_ms`: 1 dosis 0,18 s, 4 dosis 0,48 s); burbujas en el destino |
| 0,26 + caudal | Fin de caudal | Si el destino se completa → 5.3; onda de superficie en destino, amplitud 0,08 u decayendo 0,6 s |
| +0,16 | Retorno | El origen vuelve a su sitio en 0,16 s (`anim_return_ms`) con un rebote de 0,03 u; iconos se ocultan |
| — | Interrupción y cola | Si llega otro toque durante el vertido, el modelo ya está actualizado y la vista lo persigue con una cola visual de hasta 2 vertidos por cuia (`queue_max_lag_pours` = 2, 02 §3.2): dos vertidos con cuias disjuntas se animan en paralelo, uno que toca una cuia ocupada espera a que esa cuia termine y, si una cuia acumula más de 2 animaciones pendientes, las anteriores saltan a su fotograma final; nunca se bloquea el input |

### 5.3 Sellado

| t (s) | Fotograma |
|---|---|
| 0,00 | La cuarta dosis llega; el líquido completo brilla +15 % de luminosidad durante 0,10 s |
| 0,10 | La tapa de calabaza cae desde 0,6 u por encima del borde en 0,15 s, ease-out con mini-rebote de 0,03 u |
| 0,25 | Aro de luz marfil recorre el borde en 0,20 s; 6-8 partículas gota saltan y caen; sonido de maracá y háptico ligero (research/01 Especificación 10 [V]) |
| 0,45 | Estado Cuia sellada: no seleccionable, sin desaturación; si Capi está en pantalla (banner o tutorial), mira hacia la cuia 0,3 s |

### 5.4 Victoria

Celebración con confeti, sonido y háptico, recompensa y botón "Siguiente". **Sin auto-avance por defecto**: la pantalla espera el toque explícito en Siguiente (`victory_autonext_s` = **0**, int; semántica en 02 §6, default en 08 §9; el mismo valor rige en 02, 06, 07 y 08). Research/01 Especificación 12 [E] proponía auto-avance a los 2-3 s; se descarta porque con auto-avance AD_GATE (02 §2) dispararía el intersticial sin toque del jugador, que lo viviría como "anuncio tras cada nivel" (la queja n.º 1 del líder, research/09 [R]), y porque la Cosecha doble perdería visibilidad. El intersticial solo puede abrirse tras el toque en Siguiente, nunca por temporizador.

| t (s) | Fotograma |
|---|---|
| 0,00 | Input bloqueado; el último sellado termina (5.3) |
| 0,30 | Todas las cuias dan un salto de 0,15 u en ola de izquierda a derecha (0,04 s de escalón) |
| 0,50 | Confeti de plumas rojas y azules de guacamayo (glosario §3.3), 1,2 s, ≤ 60 partículas (`anim_confetti_max_particles`); toada corta de victoria (2-3 s, research/07 §6.3 [E]) |
| 0,60 | Capi entra desde la orilla izquierda (0,4 s) y celebra en bucle (`capi_cheer`), fuera del tablero |
| 0,80 | Panel de papel "¡Nivel completado!" sube desde abajo (0,25 s): semillas ganadas con contador de 0,6 s, bonus Difícil / Muy difícil, la barra de la Crecida del HUD (7.4) replicada en grande y avanzando un escalón (desde el 25, `streak_unlock_level`; hitos y recompensas según 05 §2.4; tras pagar el hito 10 vuelve a 0), Canasta si toca (cada 10), Dato del río si toca |
| 1,40 | Botones: Cosecha doble (desde el 11, con etiqueta "Ver video") y Siguiente; **sin auto-avance por defecto**: la pantalla permanece hasta el toque en Siguiente (`victory_autonext_s` = 0, int; 02 §6 / 08 §9). Si Remote Config fijara un valor > 0, el temporizador solo correría sin oferta de Cosecha doble visible y nunca activaría AD_GATE: el intersticial exige el toque explícito en Siguiente |

### 5.5 Aguas estancadas y El río bajó

| t (s) | Fotograma |
|---|---|
| 0,00 | El motor detecta que no queda vertido legal (research/01 §Implicaciones 8) |
| 0,20 | Las superficies se aquietan (A → 0), el fondo baja 20 % de saturación, una niebla fina al 12 % cruza el tablero; zumbido grave de agua quieta |
| 0,45 | El modal sube desde abajo (0,25 s) y ocupa el tercio inferior; las cuias siguen visibles y atenuadas al 70 %, para que el jugador vea qué deshacer. Titular "No hay más vertidos" (glosario §5.1). Orden: **Marea** (si queda cupo, con contador; ilimitada en 1-10), **Cuia de rescate** (**600 semillas fijas** desde el nivel 31, `rescue_price_bands` = [[31,600]], canon 04 §6.2 / 05; el primer rescate del día cuesta 300 con el 600 tachado; máximo 1 por nivel), **Volver a la orilla** como enlace al pie |
| 0,45-0,85 | Si `ui_mascot_rescue_anim`: Capi llega remando desde la izquierda, se detiene junto al modal con una cuia en alto (`capi_rescue`), nunca tapa botones; copy "¡Capi llega al rescate con una cuia más! Sigue desde aquí." |
| Sin saldo | Los botones de precio cambian a Puñado 1,99 y Balsa de rescate 4,99 (máx. 1/día); ninguna de estas ofertas existe antes del nivel 31 (`rescue_min_level`; dirección 10) |
| El río bajó | Al servir la variante aliviada tras 3 fallos (`relief_after_fails`; qué cuenta como fallo: tabla de 04 §5.2) (dirección 7): la banda de río baja 0,3 u en 0,3 s, aparece un banner de papel de 2,0 s (`anim_river_lowered_banner_ms`) con "El río bajó: esta vez hay un color menos" / "…una cuia más" e icono (−1 fruta o +1 cuia); Rosa asoma y exhala al fondo (sonido de exhalación, research/07 §6.3 [V]) |

## 6. Mascotas

### 6.1 Capi (capibara, mascota principal)

**Personalidad**: guía amable, tranquila, servicial; comunica amistad y compartir (research/07 §5 [V dato, E uso]; dirección 22). Nunca regaña, nunca llora para vender: en el bloqueo se preocupa y actúa (trae una cuia), no culpa.

**Hoja de modelo** [P]: 2,5 cabezas de alto, cuerpo redondo de perfil cuadrado como el animal real, hocico ancho, ojos pequeños y oscuros con un brillo, orejas chicas; pelaje #8B6A3E, panza #C8A872, nariz #4A3222; trazo de contorno 2 px #2B2118 al 80 %; sin ropa, sin collares, sin pintura, sin accesorios étnicos (research/07 §8 [E]). Puede cargar cuias en la cabeza o entre las patas y remar una canoa. Tamaño en pantalla: 96-140 dp en banners y modales; nunca en el tablero durante el juego.

| ID de pose | Nombre | Dónde aparece | Fotogramas / loop |
|---|---|---|---|
| `capi_idle` | Reposo (respira, parpadea) | Mapa del río, Mercado, Bitácora | 24 f loop |
| `capi_present` | Presenta con la pata | Banner de mundo, tutorial (nivel 1-3), Reto del río | 18 f |
| `capi_carry` | Trae una cuia en la cabeza | Cuia extra (compra), icono del comodín | 20 f |
| `capi_rescue` | Llega remando con una cuia en alto | Modal Aguas estancadas (desde el 31) | 30 f entrada + 12 f loop |
| `capi_cheer` | Salta y aplaude | Victoria (desde la orilla) | 16 f loop |
| `capi_think` | Se rasca la cabeza, mira el tablero | Primeros 2 s del bloqueo antes del 31 (sin rescate) | 14 f |
| `capi_shrug` | Encoge hombros y señala "otra vez" | Volver a la orilla tras un bloqueo | 12 f |
| `capi_sleep` | Dormita | Idle largo (> 30 s) en el Mapa del río | 20 f loop |
| `capi_wave` | Saluda | Pantalla de arranque, Bitácora día nuevo | 12 f |
| `capi_point` | Señala a la derecha | Tutorial (dedo guía es una mano; Capi acompaña), botón Siguiente en hitos | 10 f |
| `capi_basket` | Carga la Canasta | Apertura de Canasta cada 10 niveles | 18 f |
| `capi_stamp` | Sella la página con la pata | Reclamo diario de la Bitácora del río | 12 f |

**Reacciones**: bloqueo → `capi_think` (antes del 31) o `capi_rescue` (desde el 31); victoria → `capi_cheer`; Difícil / Muy difícil completado → `capi_cheer` con salto más alto y plumas extra; fallo con Volver a la orilla → `capi_shrug`. **Límites**: nunca fijo en el centro inferior con un objeto (glosario §3.1); nunca cocina, mezcla ni "prepara" nada (caldero); nunca aparece junto a símbolos sagrados ni con pintura corporal.

### 6.2 Rosa (boto, mascota secundaria)

**Personalidad**: curiosa, rápida, ligada a avanzar; aparece y desaparece como el animal real, que sale a respirar. Hembra; la leyenda del boto encantado se omite por completo (glosario §3.2).

**Hoja de modelo** [P]: cuerpo de delfín de río con el hocico largo y la frente abombada reales, aleta dorsal baja; rosa #E8A0B4 con dorso #C98A98 y vientre #F3D3DB; ojo pequeño; sin pestañas ni rasgos humanos; sin accesorios. Tamaño 80-120 dp.

| ID de pose | Nombre | Dónde aparece |
|---|---|---|
| `rosa_surface` | Asoma y exhala (sonido real de exhalación, research/07 §6.3 [V]) | Banner de cambio de mundo; El río bajó |
| `rosa_jump` | Arco completo con salpicadura | Salto del Boto (saltar nivel), transición al siguiente nivel |
| `rosa_swim` | Nada a lo largo de la línea del río | Mapa del río (guía la posición actual) |
| `rosa_fact` | Se detiene junto a una tarjeta de papel | Dato del río (conservación: boto En Peligro, pirarucú, Tapajós y minería, research/07 §2 y §5 [V]) |
| `rosa_idle` | Flota con ondas | Fondo del Mapa del río |
| `rosa_dive` | Se sumerge | Cierre de banners |

**Límites**: Rosa nunca aparece en el modal de bloqueo ni en pantallas de pago (su papel es avanzar, no vender); nunca se representa como humano ni con sombrero (iconografía del encantado).

### 6.3 Elenco de fondo

Guacamayos (confeti de victoria), nutria gigante (chillido al subir un hito de Crecida), morpho (Morpho), luciérnaga (Luciérnaga), rana dardo (icono Difícil), jaguar (icono Muy difícil, solo animal), perezoso (idle del Mapa del río), hoatzin (Volver a la orilla en el mundo 11), tucán (vertido del tutorial) (glosario §3.3). Descartados: anaconda como personaje, jaguar-hombre, seres mitológicos, pueblos indígenas como personajes o skins (research/07 §5-6.1 [V]).

## 7. UI kit

### 7.1 Tipografía [P]

| Rol | Fuente (licencia) | Peso | Tamaños (dp) | Notas |
|---|---|---|---|---|
| Display (número de nivel, titulares, "¡Nivel completado!") | Baloo 2 (SIL OFL 1.1) | ExtraBold | 40 / 28 / 22 | Redondeada pero no la de ningún líder (research/08 §4.1); cubre ã, ç, ñ, tildes |
| Texto y HUD | Nunito (SIL OFL 1.1) | SemiBold / Regular | 16 / 14 / 12 (mín.) | Números tabulares (`tnum`) en contadores |
| Botón | Nunito | Bold | 16 | Mayúscula inicial, nunca MAYÚSCULAS |
| Etiqueta de dificultad | Nunito | Bold | 12-14 | Con icono a la izquierda |

Prohibido: contornos gruesos con degradado dorado, biselados y sombras duras sobre el texto (sección 1.3). Los textos van en tinta #2B2118 sobre papel marfil (contraste 13,3:1) o en marfil sobre madera / verde / índigo (≥ 5:1).

### 7.2 Tokens de color de UI [P] (contrastes calculados por este capítulo)

| Token | HEX | Uso | Contraste del texto |
|---|---|---|---|
| `ui_papel` | #F4EBDA | Paneles, modales, tarjetas | Tinta sobre papel 13,3:1 |
| `ui_tinta` | #2B2118 | Texto principal, contornos | — |
| `ui_madera` | #6B4A2B | Marcos, bandeja cumbuca, botón secundario | Marfil sobre madera 6,7:1 |
| `ui_oliva` | #3E5C3A | Fondo de HUD translúcido (70 %), separadores | Marfil sobre oliva 6,3:1 |
| `ui_verde_boton` | #3F6B33 | Botón primario (Jugar, Siguiente, Continuar) | Marfil sobre verde 5,3:1 |
| `ui_ambar` | #C98A2B | Marco y etiqueta Difícil, precios en semillas | Tinta sobre ámbar 5,4:1 |
| `ui_indigo` | #24305E | Marco y etiqueta Muy difícil | Marfil sobre índigo 10,7:1 |
| `ui_rojo_aviso` | #9E3B2E | Solo para "canoa volcada" y errores de compra | Marfil sobre rojo ≥ 6:1 |

Ninguno coincide con un tinte de la sección 3 (el ámbar de UI difiere de Agua del Río Negro #A9672E y de Taperebá #F2B705 en luminosidad y saturación; aun así, el precio en semillas siempre va con el icono de semilla).

### 7.3 Botones "piedra de río"

| Propiedad | Primario | Secundario | Con precio | Con anuncio |
|---|---|---|---|---|
| Forma | Rectángulo redondeado, radio 14 dp, plano, borde inferior 2 dp más oscuro (sin bisel, sin degradado) | Igual, relleno madera | Igual, relleno papel con borde madera 1,5 dp | Igual, relleno papel |
| Alto | 48 dp | 40 dp | 48 dp | 48 dp |
| Contenido | Texto marfil; icono opcional 24 dp a la izquierda | Texto marfil | Texto tinta + pill ámbar con icono semilla 20 dp y cifra | Texto tinta + icono "Ver video" 20 dp |
| Pressed | Escala 0,96 en 80 ms; borde inferior desaparece | ídem | ídem | ídem |
| Disabled | Alfa 40 %, sin borde inferior | ídem | Cifra en rojo aviso si no hay saldo | "Sin video disponible" en gris |

### 7.4 HUD del nivel (360 × 780 dp)

| Zona | Posición y tamaño | Contenido |
|---|---|---|
| Fila superior | y = safe + 8, alto 48 | Izquierda: Pausa (40 dp). Centro-izquierda: "Nivel {n}" (Display 28) con etiqueta de dificultad debajo (7.7). Centro-derecha: pill Semillas (icono 20 + cifra). Derecha: pill Canoas (visible desde el 31; antes, oculto) y Reiniciar / Volver a la orilla (icono 40 dp) |
| Barra de evento | Bajo la fila superior, alto 32 dp (02 §5) | Reservada a los eventos de 1.2 (06 §5): colapsada mientras no haya evento activo, es decir, siempre en la 1.0. Ni la Crecida ni la Hoja escudo van aquí |
| Borde derecho | x = 348-356, y = 120-560 | **Barra vertical de la Crecida** (nivel de agua que sube de abajo arriba, 8 dp de ancho), visible desde el nivel 25 (`streak_unlock_level`): sube un escalón por victoria, con muescas en los hitos 3 · 6 · 10 (05 §2.4); al pagar el hito 10 vuelve a 0 y el río crecido sigue activo hasta el siguiente fallo duro no escudado, que también la vacía (04 §5.2); Hoja escudo, cuando está disponible, como hoja gigante al pie de la barra. Es la única posición de la Crecida en el HUD (02 §5 y 06 §4.3 la copian); la pantalla de victoria la replica en grande (5.4) |
| Tablero | y = 120-620 | Banda de río al fondo (alto 60 dp en reposo); 2 filas de cuias centradas (3 filas solo si > 14 cuias); alineación por el pie |
| Bandeja cumbuca | y = 660-740, alto 80, ancho 328, centrada | **Orden canónico** de izquierda a derecha (este capítulo es la fuente; 02 §5 y 04 §2 lo copian tal cual, sin posiciones propias): [Deshacer · **Marea**; sin contador en los niveles 1-10 (ilimitada, `undo_unlimited_until_level` = 10) y contador "3/3" desde el 11] [+1 cuia · **Cuia extra**] [Pista · **Morpho**] [Barajar · **Pororoca**] (+ [Revelar · **Luciérnaga**] solo en niveles Aguas turbias). Todos los comodines viven en esta bandeja, en una sola fila: ninguno flota fuera de ella. Con 4 slots cada uno mide 64 dp; con los 5 (Aguas turbias) pasan a 56 dp. Antes del desbloqueo de un comodín (02 §5 y §13: Marea visible desde el 1 y enseñada en el 3; Cuia extra 8 con 1 unidad regalada; Morpho 12 con 3 unidades regaladas; Pororoca 30; Luciérnaga ≥ 100) su ranura no existe y la bandeja se centra conservando el orden relativo. Etiqueta funcional de 12 dp bajo el icono; nombre temático en el tooltip de primer uso (glosario §0.2) |
| Área táctil | — | Todo control ≥ 48 dp; la zona táctil de la cuia es el ancho completo de su ranura (48 dp con 7 por fila) × el alto de la cuia + 32 dp por encima para alcanzarla elevada (102 + 32 = 134 dp con 7 por fila), según 02 §3.3 |

El orden y la iconografía difieren de IEC/Guru (deshacer / pista / +tubo) por diseño (research/08 §4.1 [E]).

### 7.5 Modal Aguas estancadas

Panel de papel de 328 × 300 dp anclado abajo (ver 5.5), marco madera 2 dp, titular Display 22 "No hay más vertidos", subtítulo 14 dp "Aguas estancadas". Botones de arriba abajo: Deshacer (secundario; sin contador en 1-10 porque Marea es ilimitada, con "Marea · 2 restantes" desde el 11), Continuar: +1 cuia (primario con precio **600 fijo** desde el 31, `rescue_price_bands` = [[31,600]], máximo 1 por nivel; el primer rescate del día muestra "300" con 600 tachado), enlace de texto "Volver a la orilla (gratis)". Capi a la izquierda del panel, fuera de él. Antes del nivel 31 el panel solo muestra Deshacer y Volver a la orilla (dirección 10).

### 7.6 Iconos de comodines y sistemas

Estilo común: glifo de 32 dp en slot de 48 dp (64 en la bandeja, 96 en el Mercado), relleno plano con 3 tonos máximo (tinta, color principal, marfil de brillo), trazo 2,5 dp, legible a 24 dp. Conceptos del glosario §5.1:

| Icono | Concepto | Color principal | Notas de legibilidad |
|---|---|---|---|
| Marea (Deshacer) | Ola que retrocede con flecha curva a la izquierda sobre la cresta | Turquesa UI #3BB8D9 atenuado al 80 % | La flecha ocupa ≥ 40 % del glifo; contador en pill ámbar |
| Cuia extra (+1 cuia) | Capi cargando una cuia vacía sobre la cabeza | Pelaje de Capi | Cuia en silueta reconocible (hombro + cuello) |
| Morpho (Pista) | Mariposa azul con destello sobre dos cuias | Azul morpho #2F6FD6 | Destello en marfil |
| Pororoca (Barajar) | Ola grande con dos flechas cruzadas | Turquesa | Diferente de Marea por tamaño de ola y flechas cruzadas |
| Luciérnaga (Revelar) | Luciérnaga encendida sobre una cuia oscura | Amarillo verdoso #D8E24A | Solo visible en Aguas turbias |
| Salto del Boto (Saltar) | Rosa saltando sobre una flecha a la derecha | Rosa boto | Aparece solo tras 5 fallos (`skip_min_fails`) y desde el nivel 31 (`skip_min_level`); qué cuenta como fallo: 04 §5.2 |
| Volver a la orilla (Reiniciar) | Flecha circular con una orilla y una ola | Madera + turquesa | Icono de 40 dp en la fila superior |
| Cuia de rescate | Capi llegando en canoa con una cuia en alto | Pelaje + madera | Solo en el modal |
| Cosecha doble (x2) | Dos semillas con una cesta | Ámbar semilla | Etiqueta "x2" en el botón |
| Semillas | Semilla ovalada ámbar con veta y brillo, borde marrón (glosario §2.1) | #D9A441 [P] | Nunca #FF9900 sobre negro |
| Canoas | Canoa de madera de lado, proa a la derecha, con línea de agua; vacía = gris volcada | Madera | Contador "5/5" |
| Hoja escudo | Hoja gigante de *Victoria amazonica* | Verde | Al pie de la barra de Crecida |
| Canasta | Cesta de fibra con el contenido asomando | Fibra | El contenido se dibuja real (dirección 15) |
| Reto del río, Bitácora del río, Cuaderno de campo, Mercado, Mapa del río, Ajustes, Dato del río, Patrones de fruta | Remo con sol, cuaderno con sello, cuaderno con hoja prensada, puesto de Ver-o-Peso, río con 13 puntos, engranaje de madera, gota con "i", cuia con patrón | Tokens UI | Todos de la misma familia de trazo |

### 7.7 Etiquetas Difícil / Muy difícil y Aguas turbias

| Etiqueta | Badge en HUD y tarjeta previa | Icono | Marco del tablero | Fondo | Subtítulo opcional (`ui_show_difficulty_subtitles`) |
|---|---|---|---|---|---|
| Difícil | Pill ámbar #C98A2B, texto tinta 12-14 dp | Rana dardo en colores de advertencia (naranja/negro) | 3 dp ámbar alrededor del tablero | Pintura base del mundo | "Aguas bravas" |
| Muy difícil | Pill índigo #24305E, texto marfil | Jaguar (cabeza de perfil con rosetas; solo animal) | 3 dp índigo | Variante **noche** de la pintura del mundo (LUT de research/02 C3.4) [P] | "Noche de jaguar" |
| Aguas turbias (modificador) | Chip adicional gris-pardo con "?" | "?" marfil | — | — | — |
| Aguas turbias · Bonus **(1.1)** | Tarjeta separada con "x2" en el Reto del río o en victoria; oculta en la 1.0 (`ui_murky_bonus_enabled` = false; true en 1.1, 08 §9) | "?" + dos semillas | — | — | — |

Sin calavera en ningún caso (research/09 §Qué NO copiar [V]). Recompensas (20 / 60 / 80 semillas) se muestran en la tarjeta previa y en la victoria.

### 7.8 Accesibilidad

Canon de accesibilidad: 02 §12; este capítulo aporta la parte visual. Controles ≥ 48 dp; texto ≥ 12 dp con contraste ≥ 4,5:1; Patrones de fruta (toggle, por defecto desde el 41; opacidad 20 %, sección 3); Alto contraste (sección 3, regla 5); Reducir movimiento (sustituye Sumergir y teñir por fundido, quita confeti y rebotes; `ui_reduce_motion_default`, `anim_reduce_motion_factor`); Vibración on/off; QA con simulador de daltonismo en los tres tipos sobre el shader real (research/07 §3.2 y Preguntas abiertas [E]); ninguna información depende solo del color (las etiquetas llevan texto e icono; el bloqueo lleva texto).

Filas replicadas de 02 §12 (mismo texto que el canon):

| Requisito | Especificación |
|---|---|
| Lector de pantalla | TalkBack/VoiceOver: toda cuia expone una etiqueta accesible con sus dosis de abajo arriba y los huecos ("cuia 3: açaí, açaí, aguaje, 1 hueco"; las dosis ocultas se leen "turbia"); todo botón lleva `contentDescription` con etiqueta funcional + estado ("Deshacer, 2 restantes"); el foco recorre las cuias por orden de ranura y después la barra de comodines; la selección se anuncia ("açaí seleccionado: elige destino") [P] |
| Tamaño de texto | Respeta la escala de fuente del sistema hasta el 130 % sin desbordes ni truncados a 360 dp (los contenedores crecen; nunca se recorta un precio ni una etiqueta de comodín); por encima del 130 % se fija ese tope. Prueba de desborde con fuente grande en la suite de QA del cap. 08 §14.2 [P] |
| Pista por inactividad | Interruptor "Pista por inactividad" en Ajustes (activado por defecto); apagado equivale a `hint_idle_delay_s` = 0 en el dispositivo y no afecta a Morpho (02 §3.4) [P] |

Implicaciones para el UI kit: los botones "piedra de río" (7.3), las pills del HUD y la bandeja cumbuca (7.4) crecen en alto con la fuente hasta el 130 % sin recortar precios ni etiquetas (el alto de 48 dp es mínimo, no fijo); cada icono de 7.6 lleva `contentDescription` con su etiqueta funcional y estado; el interruptor de la pista por inactividad vive en Ajustes junto a Patrones de fruta, Alto contraste, Reducir movimiento y Vibración.

## 8. Pautas de sensibilidad cultural operativas

Base: research/07 §6.1 [V hechos / E reglas]; dirección 15; glosario §0.3.

### 8.1 Qué sí y qué no

| Sí | No |
|---|---|
| Paisaje, agua, plantas, frutas, fauna; mercados y arquitectura pública (Ver-o-Peso, Casa de Fierro, Teatro Amazonas) | Personas, aldeas identificables, malocas, pueblos indígenas como personajes, enemigos o skins |
| Léxico tupí y quechua de uso común ya integrado al portugués/español: cuia, maracá, igarapé, jacaré, tucano, açaí, capivara, piranha, yaku (research/07 §6.1 [V]) | Nombres de deidades, rituales, clanes; "chamán", "pajé", "curaca"; ayahuasca |
| Objetos de uso cotidiano como skins (cuia, totuma, tinaja) con decoración **propia** | Kené shipibo (Patrimonio Cultural del Perú, RD 540-2008), diseños de mokawa copiados, pintura corporal con patrones étnicos, máscaras, flautas de Yuruparí (research/07 §6.1 [V]) |
| Tepuyes de Chiribiquete y Pico da Neblina como siluetas lejanas | Pictografías de Chiribiquete; Neblina en primer plano (sagrado yanomami) |
| Pueblos nombrados con su nombre propio solo en Datos del río informativos | Pueblos no contactados (Tagaeri-Taromenane, Chiribiquete) en cualquier texto o imagen |
| Géneros musicales festivos y populares: carimbó, toadas, cumbia amazónica, maracá | Ícaros, cantos rituales, flautas sagradas; quena/zampoña fuera del mundo 13 |
| Fauna con comportamiento real (boto que exhala, capibara en grupo) | Leyenda del boto encantado, anaconda como "madre" de diseños, jaguar-hombre |
| Hitos no amazónicos con rótulo "vecinos del Orinoco" | Presentarlos como río Amazonas |
| Nheengatu solo con revisión pagada de un hablante (research/07 §6.1 [V]) | Palabras de lenguas indígenas sin revisión |

### 8.2 Proceso con asesoría remunerada y crédito

| Hito | Qué revisa el asesor cultural (tarifa pactada, 3-5 % del presupuesto de arte, research/07 §Implicaciones 7 [E]) | Entregable |
|---|---|---|
| Brief de arte (antes de bocetar) | Este capítulo, la lista 8.1 y los nombres del glosario (Capi, Rosa, cuia, skins) | Acta con cambios |
| Bocetos del mundo 1 Estuario | Fondos, Cuia de vidrio, Capi | Aprobación o lista de ajustes |
| Arte final del mundo 1 y UI kit | Iconos, Mercado (Ver-o-Peso), Bitácora | Aprobación |
| Cada mundo nuevo (2-13) y cada skin | Pintura, skin, fauna, instrumento, Datos del río del mundo | Aprobación por mundo |
| Ficha de tienda y creativos de UA | Capturas, vídeo, 5 conceptos | Aprobación |
| Audio | Brief al compositor (géneros permitidos y prohibidos) y entregas | Aprobación |

Crédito en pantalla: pantalla de créditos con nombre del asesor y de los artistas regionales (1-2 artistas de Belém/Santarém, Pucallpa, Leticia o Tena, research/07 §6.1 [E]) y una sección "Sobre la Amazonía" en Ajustes con la fuente de cada Dato del río; mención en la descripción larga de la ficha. Checklist por asset antes de aprobar: (1) ¿hay personas o símbolos? (2) ¿la decoración es propia? (3) ¿el lugar es de la cuenca o lleva rótulo? (4) ¿el texto usa palabras de la lista permitida? (5) ¿lo vio el asesor?

## 9. Audio

### 9.1 Paisaje sonoro por mundo

Firma sonora del Amazonas: piha gritona (>110 dB, marcador de selva en el cine), lluvia (2.100-3.000 mm/año), coro del alba, exhalación del boto, vocalizaciones de nutria gigante (research/07 §6.3 [V]). El ambiente es un lavado generativo de 3 capas por mundo: agua del tipo del río + lluvia variable + fauna (research/07 §6.3 [E]).

| Mundo | Capa agua | Capa lluvia (probabilidad por nivel) | Capa fauna (eventos aleatorios cada 20-45 s) |
|---|---|---|---|
| 1 Estuario | Río ancho lento, marea | 20 % | Garzas, mercado lejano sin voces inteligibles, piha ocasional |
| 2 Tapajós | Olas de playa suaves | 10 % | Tucanes, piha, exhalación de Rosa |
| 3 Manaos | Dos corrientes (grave + agudo) | 25 % | Botos, guacamayos |
| 4 Trapecio | Río tras lluvia, goteo | 50 % | Morphos (silencio), perezoso (nada), grillos diurnos |
| 5 Loreto | Agua quieta de espejo | 30 % | Pirarucú que respira (burbuja grave), piha |
| 6 Oriente | Laguna negra nocturna | 40 % | Ranas, grillos, luciérnagas (sin sonido), búho lejano |
| 7 Madre de Dios | Río medio | 20 % | Guacamayos (collpa), nutrias |
| 8 Madidi | Cascada lejana | 30 % | Coro de aves de altura |
| 9 Ucayali | Vapor (siseo) + río | 20 % | Paiche, piha |
| 10 Alto Negro | Río negro lento | 15 % | Noche: ranas, insectos, agua |
| 11 Escudo de Guayana | Viento de sabana + río | 10 % | Hoatzin (graznido áspero) |
| 12 Invitados del Orinoco | Río rápido sobre piedra | 10 % | Guacamayos, viento de tepuy |
| 13 El origen | Torrente de altura, viento frío | 10 % | Viento; aves lejanas |

### 9.2 SFX

| ID | Disparador | Descripción | Duración |
|---|---|---|---|
| `sfx_select` | Toque en origen | Toque de agua corto, afinado al tinte de la dosis superior (14 notas pentatónicas) | 0,12 s |
| `sfx_pour_start` / `sfx_pour_loop` / `sfx_pour_end` | Vertido | Chorro de agua en recipiente de vidrio; el loop sube de tono con el llenado | 0,2 / loop / 0,2 s |
| `sfx_illegal` | Vertido ilegal | Sonido neutro de madera, sin tono negativo (research/01 Especificación 9 [V]) | 0,15 s |
| `sfx_seal` | Cuia sellada | Maracá (dos sacudidas) + tapa de madera | 0,45 s |
| `sfx_haptic_seal` | Cuia sellada | Háptico ligero (research/01 Especificación 10 [V]) | 20 ms |
| `sfx_submerge` / `sfx_emerge` / `sfx_tint` | Sumergir y teñir | Chapuzón grave + burbujas; goteo; nota por dosis | 0,4 / 0,3 / 0,1 s |
| `sfx_win_toada` | Victoria | Toada corta de percusión y voz sin letra (research/07 §6.3 [E]) | 2-3 s |
| `sfx_confetti` | Victoria | Aleteo de guacamayos | 1,0 s |
| `sfx_seeds_tick` | Contador de semillas | Semillas cayendo en cesta, 1 por 10 semillas, máx. 20 ticks | 0,05 s c/u |
| `sfx_stuck` | Aguas estancadas | Zumbido grave de agua quieta, sin golpe dramático | 1,5 s |
| `sfx_capi_paddle` | Cuia de rescate | Remo en agua + chapoteo de Capi | 0,8 s |
| `sfx_undo` | Marea | Ola que retrocede, reversa del chorro | 0,4 s |
| `sfx_shuffle` | Pororoca | Ola grande, remolino, 14 notas rápidas | 0,9 s |
| `sfx_hint` | Morpho | Aleteo + destello (campanita de madera) | 0,5 s |
| `sfx_reveal` | Luciérnaga | Chispa + burbuja que se aclara | 0,5 s |
| `sfx_skip` | Salto del Boto | Salto, chapuzón, exhalación | 1,2 s |
| `sfx_restart` | Volver a la orilla | Agua que se recoge + madera | 0,5 s |
| `sfx_river_drop` | El río bajó | Nivel de agua que baja + exhalación de Rosa | 1,0 s |
| `sfx_basket_open` | Canasta | Fibra que se abre + frutas | 0,8 s |
| `sfx_streak_up` / `sfx_streak_shield` | Crecida / Hoja escudo | Chillido de nutrias (hito) / hoja que se despliega | 0,8 / 0,6 s |
| `sfx_ui_tap` / `sfx_ui_back` / `sfx_ui_buy` | UI | Madera seca / madera grave / semillas en cesta | 0,08-0,4 s |
| `sfx_canoe_lost` / `sfx_canoe_back` | Canoas | Canoa que vuelca (suave) / remo | 0,6 s |
| `sfx_fact` | Dato del río | Página de papel | 0,3 s |

### 9.3 Música

Sistema adaptativo de tres capas por mundo (research/07 §6.3 [E]): (1) **lecho** ambiental de 60-90 s en loop sin pulso marcado, con el instrumento de la tabla 2.2 en notas largas; (2) **percusión suave** que entra al sellar la primera cuia y gana una capa cada 25 % de cuias selladas (volumen, no tempo); (3) **toada de victoria** de 2-3 s. Menús, Mercado y pantalla de Crecida usan una cumbia amazónica ligera a 90-100 BPM (Farfisa, guitarra con wah-wah suave, research/07 §6.3 [V género]). Difícil mantiene el lecho; Muy difícil usa la variante nocturna del lecho (menos agudos, más graves), nunca "música tensa" de reloj. Prohibidos en el brief del compositor: ícaros, flautas de Yuruparí, cantos rituales, quena/zampoña fuera del mundo 13, melodías o colores de Garantido/Caprichoso (research/07 §6.3 [V]; glosario §3.3).

### 9.4 Mezcla y ajustes [P]

Música integrada a −20 LUFS, lecho ambiental a −26 LUFS, SFX con picos ≤ −6 dBTP; ducking de −6 dB del lecho durante `sfx_win_toada` y el modal; Sonido, Música y Vibración activados por defecto con la música 6 dB por debajo de los SFX; respeta el modo silencio del sistema; todo el audio se detiene durante anuncios y se reanuda con fundido de 0,5 s.

### 9.5 Licencias y originalidad

| Elemento | Decisión | Razón |
|---|---|---|
| Música (13 lechos, capas de percusión, toada, tema de menú, stingers) | **Original**, encargo con cesión total de derechos (buyout mundial, perpetuo, incluye anuncios y tráilers), compuesta "en el estilo de" sin samplear grabaciones | Carimbó, toadas y chicha grabados tienen derechos de autor e intérprete; la cesión total evita reclamaciones en UA (research/05 Preguntas abiertas: costes de música y ASMR [E]) |
| Grabaciones de fauna (piha, boto, nutria, hoatzin, ranas) | Biblioteca con licencia comercial perpetua o grabación de campo encargada; nada CC BY-NC | Las bases de grabaciones de aves suelen ser NC [E]; la piha es la firma del juego y vale el encargo |
| Agua, lluvia, vidrio, madera, maracá | Grabación propia (foley) + biblioteca comercial | Barato y controlable |
| Instrumentos | Grabación de músicos regionales cuando sea posible (curimbó, maracá, Farfisa) | Coherente con la contratación regional (sección 8.2) |
| Voces | Ninguna voz hablada; coros sin letra solo en la toada | Evita localización de audio y lecturas culturales |

## 10. Assets de tienda y creativos de UA

Reglas de ficha: título ≤ 30 caracteres, prohibidos "Best", "#1", "Free", MAYÚSCULAS, emojis e insignias en el icono; ≥ 4 capturas verticales de 1080 px+ para recomendación; experimentos de ficha por idioma (research/08 §3.2 [V]). Icono 512 px, gráfico de funciones 1024 × 500, 8 capturas por idioma (research/06 §10 checklist [V]). Los iconos dominantes del género son 2-3 tubos saturados sobre fondo liso, sin texto (research/08 §3.1 [E]); el nuestro debe leerse igual de rápido y distinguirse por el objeto.

**Título y logotipo (decisión del dueño del 2026-10-06).** Títulos de ficha: EN "Tintalú: Water Sort Puzzle" (26 caracteres), ES "Tintalú: Ordenar Agua" (21) y PT-BR "Tintalú: Classificar Água" (25), con la tilde en todo lo visible, porque sin ella se lee "tinTAlu"; la forma ASCII "Tintalu" queda para dominios, usuarios de redes, IDs y búsquedas (`docs/legal/nombres-candidatos.md` §4.1). El nombre evoca el teñido del agua con frutas (Sumergir y teñir); el agua y la selva los ponen el subtítulo, el arte y los mundos. **Logotipo:** "Tintalú" con la tilde visible y una gota o pincelada de tinte, sin parecido con la marca "Tinta" de la App Store ("Tinta: Color Logic Puzzle" y "Tinta: Coloring Book & Paint"); la búsqueda figurativa del logotipo la hace el abogado de marcas cuando esté diseñado (`docs/legal/nombres-candidatos.md` §5.1). El icono y las capturas no llevan el nombre (10.1-10.2); la marca aparece en el título y en el logotipo del vídeo (10.3).

### 10.1 Icono: tres conceptos (A/B por experimento de ficha)

| Concepto | Descripción | Pros | Riesgos |
|---|---|---|---|
| **A. Cuia que vierte** (recomendado; glosario §1.2) | Una cuia de vidrio inclinada vierte un chorro de Açaí (#6A2C91) sobre una banda de río blanca; dentro, 3 dosis: Aguaje, Taperebá, Açaí; fondo oliva desaturado; sin texto | Dice "water sort" al instante y "Amazonía" por el objeto; el chorro es el gesto del juego | La silueta de la cuia debe leerse a 48 px: cuello y hombro exagerados un 15 % |
| **B. Capi con cuia** | Capi de frente sostiene una cuia con 4 tintes; fondo ámbar suave | Mascota = marca; ideal para UA | Si Capi no gusta, arrastra el icono; menos "género" |
| **C. Encuentro de las Aguas** | Dos aguas en diagonal (ámbar #4A2C14 y beige #B08A5A) que no se mezclan, con una cuia de perfil en el centro con dos dosis | Único en la estantería; la metáfora exacta del juego | Menos reconocible como water-sort para quien busca el género |

Reglas comunes: sin sonrisa/flecha ni naranja #FF9900 sobre negro (research/08 §4.2 [V]); legible a 48 px sobre fondo claro y oscuro; versión adaptativa de Android con el fondo a sangre.

### 10.2 Ocho capturas (1080 × 1920, con texto corto superpuesto en papel marfil)

| # | Qué muestra | Texto ES / EN / PT |
|---|---|---|
| 1 | Gameplay real en Manaos: una cuia vertiendo Açaí con el Encuentro de las Aguas detrás (hook de research/07 §Implicaciones 10) | "Ordena el agua del Amazonas" / "Sort the waters of the Amazon" / "Classifique as águas da Amazônia" |
| 2 | Tablero del Estuario con 5 cuias, iconos de fruta visibles | "14 frutas, 14 colores" / "14 fruits, 14 colors" / "14 frutas, 14 cores" |
| 3 | Sumergir y teñir a mitad (frutas cayendo) | "Mira cómo se tiñe el río" / "Watch the river take color" / "Veja o rio ganhar cor" |
| 4 | Mapa del río con 13 paradas y banderas de país | "13 mundos, 8 países" / "13 worlds, 8 countries" / "13 mundos, 8 países" |
| 5 | Nivel Difícil con badge rana dardo y una Marea usada en el bloqueo | "Difícil de verdad, justo siempre" / "Honest hard levels" / "Difícil de verdade, sempre justo" |
| 6 | Patrones de fruta activados, con simulación lado a lado | "Patrones para todos los ojos" / "Patterns for every eye" / "Padrões para todos os olhos" |
| 7 | Capi trayendo una cuia; Rosa saltando | "Capi y Rosa te acompañan" / "Meet Capi and Rosa" / "Capi e Rosa te acompanham" |
| 8 | Reto del río + Bitácora del río + Canasta abierta con contenido visible | "Juega sin wifi, vuelve cada día" / "Play offline, come back daily" / "Jogue offline, volte todo dia" |

Las 3 primeras siguen la regla de research/08 §Implicaciones 3 (recipiente autóctono vertiendo río teñido, fondo de un país, texto corto). Gráfico de funciones 1024 × 500: concepto A del icono ampliado con el Encuentro de las Aguas.

### 10.3 Vídeo de ficha (30 s)

Research/08 §Implicaciones 3 recomienda 15-20 s; se produce a 30 s con los primeros 15 s autosuficientes para recortar [P]. Vertical 1080 × 1920 y horizontal 1920 × 1080; sin voz; música: tema de menú.

| Segundo | Plano | Qué pasa |
|---|---|---|
| 0-3 | Gameplay cerrado | Una cuia vierte Açaí; sellado con maracá; sin logo todavía |
| 3-8 | Gameplay | Tres vertidos seguidos encadenados; 2 cuias se sellan; contador de semillas |
| 8-12 | Sumergir y teñir | Carga del nivel 42 en Manaos, versión 1,4 s a cámara lenta ×2 |
| 12-15 | Logo | Logotipo "Tintalú" (tilde visible, gota o pincelada de tinte; sección 10) sobre el Encuentro de las Aguas; descriptor "Ordenar agua" |
| 15-19 | Tensión | Nivel Difícil: dos cuias quedan y el agua se aquieta; aparece Marea y deshace |
| 19-23 | Mascotas | Capi llega con una cuia (rescate); Rosa salta al siguiente nivel |
| 23-27 | Mundos | Barrido rápido de 4 fondos (Estuario, Tapajós, Manaos, Trapecio) con el Mapa del río |
| 27-30 | Cierre | Tablero resuelto, confeti de guacamayo, logo + "Sin wifi" |

### 10.4 Cinco conceptos de creativos de UA con la mascota

Patrón ganador del subgénero: abrir en gameplay, varios vertidos satisfactorios, fail-state para tensión, mano guía, cierre con selección de niveles; playables idénticos al juego (research/09 §Por qué ganó 7 [V: Mobidictum, Artik]); conceptos emocionales con mascota ("rescate", "carrera contra el tiempo") funcionaron para el líder (research/09 §Por qué ganó 7 [V]). Sin creativos dirigidos a niños (research/08 C5.5 [V]).

| # | Concepto | Guion (15-30 s / playable) | KPI de corte |
|---|---|---|---|
| 1 | **Capi trae una cuia** | Tablero a punto de bloquearse; mano guía falla; Capi llega remando con la cuia; se resuelve. Cierre: "Siempre hay una salida" | IPM, CPI; umbral de clics 1/3/10 |
| 2 | **Dos aguas que no se mezclan** | Plano del Encuentro de las Aguas real (pintura) → corte al tablero del mundo 3 con Agua del Río Negro y Agua del Tapajós; vertidos satisfactorios; "Esto pasa de verdad en Manaos" | CTR; retención D1 de la cohorte |
| 3 | **El río bajó** | Tres fallos seguidos en el mismo tablero (misma semilla); Rosa asoma; banner "El río bajó" y el nivel se resuelve; "Justo siempre" | CPI en jugadores de 35+; D7 |
| 4 | **¿Puedes con Noche de jaguar?** | Nivel Muy difícil nocturno; dos vertidos correctos y uno incorrecto; Marea deshace en el bloqueo; "Sin trucos, sin calaveras" | IPM en tier-1 |
| 5 | **Rosa te lleva río abajo** | Rosa salta entre 4 fondos de mundos mientras el jugador completa niveles cortos; cierre con el Mapa del río; "13 mundos, 8 países, sin wifi" | CPI en BR/MX |

Cada concepto se produce como playable (abre en el nivel 3-5 real) y como vídeo de 15 y 30 s; 6-8 variantes por red (AppLovin, Meta, Google Ads, TikTok, Mintegral, research/09 §Mejorar [V]).

## 11. Lista de assets de la 1.0 (conteo estimado)

Alcance de la 1.0 (canon de 2.1): fondos 1-4 install-time + Loreto (141-200) por PAD; "río genérico" para 201-700 (mundos 6-9) hasta que lleguen sus packs [P] (research/02 C3.3 [E]); 4 skins (glosario §4); estimaciones en días-persona de un artista 2D freelance [P].

| Categoría | Ítems | Conteo | Días-persona [P] |
|---|---|---|---|
| Fondos | Pinturas base Estuario, Tapajós, Manaos, Trapecio, Loreto, río genérico | 6 | 18 (3/fondo) |
| Variantes de fondo | LUT noche (Muy difícil) y lluvia por fondo; partículas de lluvia y vapor | 12 LUT + 2 sistemas | 4 |
| Banda de río | 3 presets de agua (negra, blanca, clara) con shader y 1 sprite de orilla por fondo | 3 + 6 | 3 |
| Cuia | Cuerpos de vidrio por capacidad (1-6) | 6 | 3 |
| Skins | Rim + pie + banda + tapa por skin (Cuia de vidrio, Santarém, Encuentro, Totuma) | 16 | 4 |
| Líquido | Shader (menisco, onda, patrón, burbujas, agua parda turbia) | 1 shader + 14 texturas de patrón | 4 (con programador) |
| Frutas | Icono de accesibilidad por tinte (2 tamaños) + sprite de fruta cayendo | 14 × 3 | 5 |
| Mascotas | Capi 12 poses, Rosa 6 poses (animación 2D por huesos o frames) | 18 animaciones | 14 |
| Elenco de fondo | Guacamayo (confeti), nutria, tucán, perezoso, rana dardo, jaguar (iconos), hoatzin (1.1) | 6 | 3 |
| UI | Tokens, botones (4 tipos × 4 estados), paneles de papel (6), bandeja cumbuca, HUD, badges de dificultad, modal Aguas estancadas, banner de mundo, Bitácora, Canasta, Mercado, Mapa del río (13 paradas), Ajustes | ~70 piezas | 12 |
| Iconos | Comodines (9), sistemas (10), monedas (2), banderas de 8 países, misc. | ~40 | 5 |
| VFX | Confeti, salpicadura, gotas, burbujas, aro de sellado, niebla, destello Morpho, chispa Luciérnaga, ola Marea/Pororoca, salto Rosa | 11 sistemas | 5 |
| Tipografía | Baloo 2 + Nunito (OFL) con atlas latino extendido | 2 | 0,5 |
| Audio SFX | Tabla 9.2 | ~35 archivos | 6 (diseñador de sonido) |
| Audio música | Lechos mundos 1-5 + genérico, 2 capas de percusión por lecho, toada, tema de menú, 6 stingers | 6 + 12 + 1 + 1 + 6 | 12 (compositor) |
| Ambientes | Capas agua/lluvia/fauna para 6 fondos | 18 loops | 3 |
| Tienda | Icono (3 conceptos → 1 final + adaptativo), 8 capturas × 3 idiomas, gráfico de funciones, vídeo 30 s (2 orientaciones) | 1 + 24 + 1 + 2 | 8 |
| UA | 5 conceptos × (playable + vídeo 15 s + vídeo 30 s) | 15 | 10 (con programador para playables) |
| **Total arte 2D** (sin audio ni playables) | | | **≈ 85 días-persona ≈ 17 semanas de 1 artista** |

Con dos artistas (o artista + animador) baja a ~9-10 semanas de calendario; los fondos 6-8 de la 1.1, los 9-13 posteriores y las 9 skins suman ≈ 35 días más (research/02 C3.4 conteo [E]).

## Parámetros configurables (Remote Config)

**Ver 08 §9**: es la única tabla de Remote Config del GDD (clave, tipo, default compilado, rango y dueño); este capítulo no repite defaults ni rangos. Claves que usa este capítulo, con el nombre canónico de 08 §9 y la sección que describe su comportamiento:

| Sección | Claves (nombres de 08 §9) |
|---|---|
| Sumergir y teñir (5.1) | `anim_tint_duration_ms_early` / `anim_tint_duration_ms_late` (int, ms), `anim_tint_fast_from_level`, `anim_tint_skippable`, `anim_tint_skip_alarm_pct` (dueño 07) |
| Vertido y sellado (4.3, 5.2) | `anim_lift_ms`, `anim_travel_ms`, `anim_pour_dose_ms`, `anim_pour_tail_ms`, `anim_return_ms`, `anim_speed_factor_early` / `anim_speed_factor_late`, `queue_max_lag_pours` (= 2, cola visual por cuia; dueño 02) |
| Victoria (5.4) | `victory_autonext_s` = **0** (int; sin auto-avance, valor común a 02, 06, 07 y 08), `anim_celebration_ms`, `anim_confetti_max_particles` (dueño 07) |
| Aguas estancadas y El río bajó (5.5) | `ui_mascot_rescue_anim`, `rescue_min_level`, `rescue_price_bands` (= [[31,600]], 600 fijo), `relief_after_fails`, `anim_river_lowered_banner_ms` |
| Paleta y accesibilidad (3, 7.8) | `ui_pattern_opacity` (20 % con Patrones de fruta; 35 % solo en Alto contraste: canon de la sección 3, dueño 07), `ui_patterns_default_from_level`, `ui_reduce_motion_default` (dueño 07), `anim_reduce_motion_factor`, `ui_haptics_default_on`, `hint_idle_delay_s` |
| HUD, bandeja y etiquetas (7.4, 7.6, 7.7) | `streak_unlock_level` (barra de la Crecida; dueño 05), `undo_unlimited_until_level`, `undo_free_per_level`, `skip_min_fails`, `skip_min_level`, `ui_show_difficulty_subtitles`, `ui_murky_bonus_enabled` (false en 1.0; true en 1.1), `ui_very_hard_night_lut` (dueño 07) |
| Audio (9) | `audio_music_default_on`, `audio_sfx_default_on`, `audio_music_percussion_layers`, `audio_ambient_rain_scale` (dueño 07) |
| Mundos y tienda (2, 10) | `live_world_bg_variant_override` (dueño 07), `biz_store_icon_variant` (solo registro) |

Alias retirados de versiones anteriores de este capítulo (no existen en `RemoteConfigDefaults.json`; la lista general de alias retirados del GDD está en 08 §9): `tint_anim_duration_early` / `tint_anim_duration_late`, `tint_anim_switch_level`, `tint_anim_skip_enabled`, `anim_skip_alarm_pct`, `pour_dose_duration`, `victory_autoadvance_s`, `victory_autoadvance_with_offer`, `pattern_opacity_default`, `confetti_max_particles`, `reduce_motion_default`, `haptics_default_on`, `very_hard_night_lut`, `music_percussion_layers`, `ambient_rain_probability_scale`, `world_bg_variant_override`, `store_icon_variant`.

## Desviaciones de la dirección

Ninguna. Precisiones que no contradicen la dirección: (1) la 1.0 añade a los fondos 1-4 install-time (dirección 16) el de Loreto (141-200) por Play Asset Delivery y un "río genérico" para los niveles 201-700 (mundos 6-9) hasta que lleguen sus packs, para cubrir los ≥600 niveles jugables de la dirección 17 (700 instalados), como propone research/02 C3.3 [E]; (2) la duración completa de Sumergir y teñir se fija en 1,4 s dentro del rango 1,2-1,5 s de la dirección 18; (3) el teñido florece desde el centro de cada dosis (research/06 y research/07 difieren en la dirección del gradiente; ninguno es dirección vinculante); (4) la pantalla de victoria no auto-avanza (`victory_autonext_s` = 0, int; semántica en 02 §6 y default en 08 §9) aunque research/01 Especificación 12 [E] sugería 2-3 s: con auto-avance, AD_GATE abriría el intersticial sin toque del jugador, contra el espíritu de la dirección 13 (anuncio solo tras completar nivel, nunca vivido como "anuncio tras cada nivel").

## Preguntas abiertas para el dueño del producto

1. Confirmar el estilo "acuarela botánica sobre vidrio" frente a un vector plano más barato (≈ −30 % de días de fondo) antes del primer boceto.
2. Cerrada: fondos de la 1.0 = mundos 1-4 install-time + Loreto (141-200) por PAD; "río genérico" para 201-700 (mundos 6-9) hasta que lleguen sus packs (2.1 y sección 11; glosario §8, 03 §17, 08 §6.2). Pintar los mundos 6-9 antes del lanzamiento (+12 días-persona) queda descartado para la 1.0.
3. Icono de lanzamiento: ¿A (cuia que vierte) como control y B/C en experimentos de ficha desde el soft launch?
4. Presupuesto y modelo de la asesoría cultural (tarifa fija vs porcentaje, 3-5 % del arte) y de los 1-2 artistas regionales; en qué hito entran (recomendado: antes de los bocetos del mundo 1).
5. Música original con cesión total (≈ 12 días de compositor) frente a biblioteca con licencia: este capítulo recomienda original por los derechos en UA.
6. Nombres Capi y Rosa: confirmar o cambiar antes de la hoja de modelo final (pendiente búsqueda de marca y revisión cultural, glosario §3).
7. Tipografías Baloo 2 + Nunito: aprobar o encargar una fuente propia para el logotipo.
8. Vídeo de ficha de 30 s con corte a 15 s, o solo 15-20 s como sugiere research/08.
9. Cerrada: `victory_autonext_s` = 0 (int) en 02, 06, 07 y 08 (incidencia 10 de GDD §7): la pantalla de victoria no auto-avanza y el intersticial solo se dispara tras el toque en Siguiente. La prueba de un valor > 0 solo para compradores de Sin anuncios queda, si acaso, como experimento opcional de soft launch, no como pregunta de diseño.
10. Variante nocturna del fondo en Muy difícil: ¿se activa desde la 1.0 o se mide primero si confunde con el modo noche de la vuelta 3?

## Dependencias con otros capítulos

Numeración canónica de capítulos: la del glosario (00 Glosario · 01 Visión · 02 Núcleo · 03 Niveles · 04 Comodines · 05 Economía · 06 Meta · 07 Arte · 08 Técnica · 09 Roadmap). No existe un capítulo "ASO y legal": la ficha, IARC y los textos legales viven en 08 §13 y el nombre comercial y sus riesgos de marca en 01 §9.

| Capítulo | Toma de este capítulo | Devuelve a este capítulo |
|---|---|---|
| 00 Glosario | Hojas de estilo e iconos finales; HEX de UI | Nombres, IDs, frases de UI; numeración canónica de capítulos |
| 01 Visión y posicionamiento | Icono, capturas y vídeo (10) como expresión del posicionamiento; lista de trade dress excluido (1.3) | Resultado de la búsqueda de marca y decisión sobre la variante de ficha (01 §9); dispositivos de referencia para el presupuesto de VFX (01 §3.3) |
| 02 Núcleo, reglas y UX | Estados visuales de la cuia (4.3), storyboards de vertido y sellado (5.2-5.3), variantes de capacidad (4.5), orden canónico de la bandeja y posición de la barra de la Crecida (7.4: vertical en el borde derecho; la barra de evento bajo la cabecera queda para los eventos de 1.2), opacidad canónica del patrón (3) | Catálogo de mecánicas con sus debuts (02 §9.1 y `mech_debut_level`: Hoja 43, Brote 63, Liana 83, Remolino 103, Barro 123, Lluvia 143, Piraña 183) que necesitan overlay y SFX propios; Cuia alta 27 (cap 5) y cap 6 en hitos desde el 141 (02 §8); fases y totales de Sumergir y teñir (02 §4.2); duraciones base del vertido, elevación de 16 dp y sacudida de rechazo ±6 dp / 2 ciclos / 120 ms (02 §4.1) y cola visual de hasta 2 vertidos por cuia (`queue_max_lag_pours` = 2, 02 §3.2); tutorial 1-12 (02 §13); filas de accesibilidad (02 §12) |
| 03 Niveles, generación y dificultad | Paleta con L* y pares de riesgo (3), variante nocturna de Muy difícil, tabla de mundos (2) | Niveles exactos de debut de cada tinte, `max_pares_riesgo`; primer Difícil 40 y primer Muy difícil 60 (03 §11/§12); bandas del Reto del río (03 §16) |
| 04 Comodines, vidas y rescate | Iconos y animaciones de los 6 comodines, Volver a la orilla, Cuia de rescate (5.5, 7.6); orden de la bandeja (7.4) | Precios y topes que se muestran en los botones (defaults en 08 §9); Cuia de rescate a 600 fijo desde el 31 (`rescue_price_bands` = [[31,600]], primer rescate del día 300, máximo 1 por nivel); definición canónica de fallo (04 §5.2) para `capi_shrug`, Salto del Boto y El río bajó; Marea ilimitada en 1-10, 1 Cuia extra regalada en el 8, 3 Morphos en el 12 |
| 05 Economía y monetización | Botones con precio y "Ver video" (7.3), capturas y vídeo (10), Cosecha doble en victoria (5.4) | Catálogo de SKUs (incluido `af_infinite_canoes_1h`) y ubicaciones rewarded; hitos de la Crecida que anima la pantalla de victoria (05 §2.4: 3 → 300 · 6 → Canasta · 10 → 1 Cuia extra + río crecido; cada hito una vez al día local; tope diario 600, que solo suma el hito 3 y las 50 semillas de la Canasta del hito 6; tras el hito 10 la barra vuelve a 0 y el río crecido sigue activo hasta el siguiente fallo duro no escudado; clave `streak_milestones`; debut en el 25, `streak_unlock_level`) |
| 06 Meta, retención y live-ops | Iconos de Reto del río, Bitácora, Canasta, Crecida, Hoja escudo; posición de la barra de la Crecida en el HUD (7.4: vertical, borde derecho); variantes de fondo para Temporada de lluvias; literal de la toada del mundo 3 (2.2 = 06 §10) | Calendario de eventos que exige skins y LUT |
| 08 Arquitectura técnica y publicación | Shader de líquido (4.2) con la máscara `_HiddenMask` y `_MurkyColor` = #8A7B6A de Aguas turbias (4.3), atlas 1×/2×/3×, PAD por mundo (2.1), comportamiento de las claves `anim_*`, `ui_*`, `audio_*` que usa este capítulo; para §13: icono, capturas, vídeo (10) y lista de trade dress excluido (1.3) | Tabla única de Remote Config con nombres, defaults y rangos (08 §9); límites de memoria y de partículas en gama baja; formato de Addressables (700 niveles instalados, mundos 1-9; fondos w01-w04 install-time y w05+ on-demand, 08 §6.2); textos legales de la ficha, ClassInd/IARC y Data Safety (08 §13) |
| 09 Roadmap, equipo, KPIs y riesgos | Conteo de assets y días-persona (11) y fondos de la 1.0 (2.1: 1-4 install-time + Loreto 141-200 por PAD; "río genérico" para 201-700, mundos 6-9) | Fechas de hitos de arte por mundo; decisión 1 vs 2 artistas |
