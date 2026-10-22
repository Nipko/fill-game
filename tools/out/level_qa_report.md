# Informe de `level-qa`

Generado por `dotnet run --project tools/level-qa -c Release -- Assets/_Project/Data/Levels/mundo_02_v1.json Assets/_Project/Data/Levels/mundo_03_v1.json Assets/_Project/Data/Levels/mundo_04_v1.json Assets/_Project/Data/Levels/mundo_05_v1.json --report tools/out/level_qa_report.md`: no se edita a mano.

- Niveles: 190; fallos: **0**; avisos: 170.

| Comprobación | Regla | Fallos |
|---|---|---|
| `QA-STRUCT` | caps, cuias y attrs de la misma longitud; <= 14 cuias generadas (02 R1; 08 sec. 4.1) | 0 |
| `QA-UNITS` | U_c coherente con units_per_color y con las capacidades (02 R3, sec. 8; 03 sec. 17 #5) | 0 |
| `QA-SEALED` | ninguna cuia sellada al inicio (03 sec. 17 #5) | 0 |
| `QA-STUCK` | atascado(S0) = false (02 sec. 9.2 regla 1) | 0 |
| `QA-VALID` | resto de LevelValidation de AF.Core (mecanicas, Barro, Aguas turbias) | 0 |
| `QA-PAR` | par recalculado (A* w = 1, run) igual al publicado; par >= 2K; dentro del par objetivo del rango +-30 % (03 sec. 17 #1-#2), salvo los tableros de estreno de la Tinaja (K <= 6; D-027) | 0 |
| `QA-TINT-DEBUT` | cada tinte con debut anterior o igual al nivel (03 sec. 17 #4) | 0 |
| `QA-KMAX` | K <= k_max y >= k_min de la cabecera (03 sec. 17 #4) | 0 |
| `QA-RISK` | risk_pairs recalculado igual al publicado y <= max_risk_pairs (03 sec. 4.5) | 0 |
| `QA-CVD-ADJ` | ningun par del simulador de daltonismo (cvd_pairs: 7-10, 5-14) junto en una cuia del tablero base; aviso en las variantes de relief y si ninguna de las 50 permutaciones del generador lo evita (con la reproduccion) (Q-B9-13 bis; 07 sec. 3) | 0 |
| `QA-PALETTE` | palette = tintes presentes en cuias (08 sec. 4.1) | 0 |
| `QA-BOT` | bot_fail recalculado con la semilla del nivel identico al publicado (03 sec. 6) | 0 |
| `QA-LABEL` | etiqueta coherente: >= 0,75 hard y >= 0,90 very_hard, desde el 40 y el 60 (03 sec. 7, 11) | 0 |
| `QA-LABEL-EARLY` | ningun nivel mide >= 0,90 antes del 60 ni >= 0,75 antes del 40, aunque se publique con la etiqueta inferior (contrato 6; 03 sec. 11-12) | 0 |
| `QA-CUIAS` | aviso: cuias del nivel dentro de las cuias totales del rango (03 sec. 11) | 0 |
| `QA-FIELDS` | sin solution, safe_path ni qa (08 sec. 4.1) | 0 |
| `QA-SIZE` | pack <= 100 KB por 100 niveles sin relief (03 sec. 17 #11) | 0 |
| `QA-EARLY-TRAPS` | trampas tempranas dentro del tope de la etiqueta (Q16 B) | 0 |
| `QA-BAND` | difficulty.band igual al rol de la posicion (03 sec. 12; remanso = comfort) | 0 |
| `QA-LABEL-SLOT` | etiquetas solo donde se permiten (41-59: p6, p12, p18; 61+: retos e hito; 40: hito) (D-009) | 0 |
| `QA-LABEL-ADJ` | nunca dos niveles con etiqueta seguidos (c.3 regla 1) | 0 |
| `QA-AFTER-LABEL` | tras cada etiqueta, el siguiente con D menor (condicion necesaria de bajar una banda; c.3 regla 2) | 0 |
| `QA-LADDER` | escalera: 61+ D(p6) <= D(p9) <= D(p12) <= D(p15) <= D(p18) < D(p20); 41-59 D(p6) <= D(p12) <= D(p18) < D(p20), D(p9) < D(p6), D(p15) < D(p12) (c.3 regla 4); 21-40: los retos suaves por debajo del hito 40 | 0 |
| `QA-REMANSO` | p19 es un remanso puro y mas facil que la media de comodidad: D(p19) < media de D de las comodidades del ciclo sin estreno (c.4; c.3 regla 5) | 0 |
| `QA-RAMP` | filas 11-12 de 02 sec. 13: cuias = 6 colores + 2 vacios, par 15-19 y 16-20, bot_fail 0-10 % | 0 |
| `QA-PURE-30` | al menos un 30 % de niveles puros por ciclo completo (d.2 regla 1) | 0 |
| `QA-WINDOW` | bot_fail en la ventana del slot (03 sec. 17 #3; 08 sec. 4.1): falla en recuperacion, comodidad, estreno (< 0,20), remanso y rampa; un reto o un hito fuera de su ventana es aviso (c.2: se publica con la etiqueta que mide) | 0 |
| `QA-DEBUT` | tableros de estreno con 2 vacios (tambien los de capacidad fuera de la rampa y del hito, 02 sec. 8) y un solo ejemplar de la mecanica; intro_mechanic y skippable = false solo en el nivel de intro (02 sec. 9.2 regla 2) | 0 |
| `QA-LABEL-ACCURACY` | aviso: acierto de etiqueta por ciclo < 60 % (c.7) | 0 |
| `QA-HITO-14` | K = 13 solo en un hito con K = 13 en el k_plan de la especificacion, con 14 cuias de 4: 13 colores + 1 vacio de holgura (capacidad - unidades = 4, sin el Barro; puede ir repartido en cuias a medias), sin capacidades, cueva de la Pirana ni Tinaja (03 sec. 1, 11-12; d.5.1 [P]); las 14 cuias con K <= 12 (la cueva de la Pirana) no son de esta comprobacion y el tope, de QA-STRUCT | 0 |
| `QA-RELIEF-MIN` | relief con al menos una variante desde el nivel 31 (03 sec. 13; 08 sec. 4.1) | 0 |
| `QA-RELIEF-VALID` | cada variante es un tablero valido (BoardBuilder y LevelValidation; con tinaja, su forma: una, su cuia contigua y en la misma fila, <= 13 cuias (D-027), T4 y combinaciones) | 0 |
| `QA-RELIEF-MINUS` | minus_color resoluble (A* w = 1 a 2.000.000: la segunda pasada del generador, Q-N2 (a); la primera, a 100.000) con el par entero publicado y sin el color removed, que nunca es una fruta de la tinaja (03 sec. 13; propuesta de la tinaja c.3 punto 4) | 0 |
| `QA-RELIEF-PLUS` | plus_empty = el tablero base con exactamente un vacio de 4 mas (al final; con tinaja, en la ranura que deja la pareja en la misma fila) (03 sec. 13) | 0 |
| `QA-RELIEF-BOT` | bot_fail de la variante recalculado; baja >= 0,20, o con base < 0,20 par menor (minus_color) o el vacio extra (plus_empty) (03 sec. 17 #8) | 0 |
| `QA-RELIEF-MASK` | variante de un nivel con tinaja: la conserva y su hidden es la mascara (todas sus capas menos la boca); variante sin tinaja: sin hidden (08 sec. 4.1) | 0 |
| `QA-RELIEF-SAFE` | variante con tinaja: ruta segura y paseos seguros y casuales (03 sec. 13; propuesta de la tinaja c.3 punto 4; opciones de Morpho [P]) | 0 |
| `QA-REPEAT-SHAPE` | forma distinta de la de los 200 niveles anteriores (03 sec. 17 #9) | 0 |
| `QA-REPEAT-HAMMING` | Hamming >= 25 % con los 20 niveles anteriores (03 sec. 17 #9) | 0 |
| `QA-MECH-DEBUT` | cada mecanica y capacidad con debut anterior o igual al nivel (D-007; 02 sec. 8) | 0 |
| `QA-MECH-SPEC` | las mecanicas del nivel estan en su celda del calendario (especificacion) | 0 |
| `QA-MECH-CELL` | cada mecanica y capacidad de la celda esta en el nivel (sin turbias por T1: aviso) (especificacion; 02 sec. 8) | 0 |
| `QA-GAP-120` | ninguna mecanica ni capacidad pasa mas de 120 niveles sin volver, sobre los niveles publicados (resumen, punto 6; d.2 regla 4) | 0 |
| `QA-MECH-KEY` | llave presente en el tablero (Hoja y Liana con fruta); agua del mundo en la Lluvia; llave del tutorial en el estreno (02 sec. 9.1; D-007) | 0 |
| `QA-MECH-COUNT` | mecanicas por nivel dentro del tope de la especificacion (mechanics_per_level; 02 sec. 9.2 regla 4: 2 antes del 300 y 3 despues, 3 solo en retos, desde el 301, y en hitos, desde el 320; 03 sec. 12) | 0 |
| `QA-MURKY-FLAG` | murky = true si y solo si hay posiciones en hidden (08 sec. 4.1) | 0 |
| `QA-MURKY-T1` | Aguas turbias solo desde el 100 y en niveles Dificil o Muy dificil (02 T1); la tinaja queda fuera (excepcion de D-019, Q-Ti1 A) | 0 |
| `QA-MURKY-LIMITS` | como mucho 3 cuias turbias y 4 dosis ocultas; T4 (02 T2-T4) | 0 |
| `QA-MURKY-SAFE` | ruta segura en cada estado de la ruta (03 sec. 8; 02 T5) | 0 |
| `QA-MURKY-COMBO` | sin Lluvia, Pirana, Liana ni Remolino en el nivel (Q14 A; [P] Remolino) | 0 |
| `QA-MURKY-Q9` | estreno del 100: una cuia, 2 dosis ocultas y ninguna otra mecanica (Q9) | 0 |
| `QA-TINAJA-TIER` | dosis de la tinaja por tipo de nivel: estreno, comodidad y practica 3 (2 ocultas); retos 4-5 (3-4); hito 5 (4) (D-019, Q-Ti1 A; 02 sec. 9.1 M13) | 0 |
| `QA-TINAJA-ONE` | una sola tinaja y una sola cuia suya por nivel (02 sec. 9.1 M13 regla 1) | 0 |
| `QA-TINAJA-PAIR` | su cuia en la ranura contigua y en la misma fila con cualquiera de las filas del cliente (02 sec. 9.1 M13 regla 1; D-006) | 0 |
| `QA-TINAJA-CUIAS` | como mucho 12 cuias en un nivel con tinaja, contando la pareja (02 sec. 5 [P]) | 0 |
| `QA-TINAJA-MASK` | hidden = todas las capas de la tinaja menos la boca, y nada oculto fuera de ella (02 sec. 9.1 M13 regla 1; propuesta de la tinaja c.4) | 0 |
| `QA-TINAJA-T4` | T4 en toda la columna de la tinaja: dos dosis vecinas nunca son de la misma fruta (02 sec. 9.1 M13 regla 1) | 0 |
| `QA-TINAJA-COMBO` | sin Liana, Remolino, Lluvia ni Pirana en el nivel y sin Barro en la tinaja o su cuia (02 sec. 9.1 M13, combinaciones) | 0 |
| `QA-TINAJA-SLOT` | ninguna tinaja en p1, p2, p19 ni en la rampa (D-019, Q-Ti1 A) | 0 |
| `QA-TINAJA-DEBUT` | estreno con tinaja: sola y un ejemplar, 2 vacios sin contar la pareja, bot_fail < 0,20 y S0 sin trampas (02 sec. 9.2 regla 2; propuesta de la tinaja c.3 y d.3) | 0 |
| `QA-TINAJA-SAFE` | ruta segura, 20 paseos seguros y 30 casuales [P] (03 sec. 8; D-019, Q-Ti1 A; propuesta de la tinaja c.3 punto 3) | 0 |
| `QA-TINAJA-MORPHO` | coste de Morpho (w = 2, 40.000 nodos por realidad) en S0, la ruta y los paseos dentro de hint_timeout_ms con el factor del telefono [P] (propuesta de la tinaja c.3 punto 3; 08 sec. 9) | 0 |
| `QA-VAR-HA` | Hoja atada: su fruta llave esta en el tablero y el nivel no empieza en Aguas estancadas (propuesta de D-007 b.3; 03 sec. 17 #6; 02 sec. 9.2 regla 1) | 0 |
| `QA-VAR-B2` | Brote de dos hojas: espacio >= 2 al empezar, con 1-2 dosis (propuesta de D-007 b.3; 02 R10 b) | 0 |
| `QA-VAR-FLOR` | Flor de vitoria-regia: key != key2, las dos frutas en el tablero, sin dosis suyas dentro y con casa fuera de ella a la vez; una Hoja atada, un Matapi con dosis o la Lluvia solo son casa de su fruta, y la Liana, otra Flor o la Tinaja, de ninguna (b.3; D-023 punto 5; D-024) | 0 |
| `QA-VAR-LN` | Liana de cuenta: n de 1 a 2 (02 sec. 9.1 M3; b.3) | 0 |
| `QA-VAR-DEBUT` | estreno de una variante de la 1.0: sola (sin otra mecanica ni capacidad) y un ejemplar (02 sec. 9.2 regla 2) | 0 |
| `QA-SCHEMA` | JSON valido contra schemas/af-level-1.schema.json con python tools/validate_schema.py --pack; sin Python o sin jsonschema, falla (08 sec. 4.1; 03 sec. 17 #11) | 0 |
| `QA-REPRO` | la especificacion y las semillas reproducen el pack byte a byte: se regenera la campana completa en memoria (03 sec. 17 #10; 08 sec. 2.4 y 4.1) | 0 |
| `QA-BAND-Q` | banda medida (cuantil de D en el pool del rango) acorde al rol: comodidad, estreno, remanso y rampa <= 0,70; recuperacion <= 0,10 o D bajo la comodidad mas facil del ciclo; hito > 0,95 o par - 2K >= media del pool (03 sec. 7; c.3 reglas 3 y 5) | 0 |

## Incidencias

- aviso `QA-BAND-Q` nivel 22 (`Assets/_Project/Data/Levels/mundo_02_v1.json`): recuperacion en banda comodidad (q 0.267); cumple el minimo: D -0.34 < comodidad mas facil del ciclo (-0.30)
- aviso `QA-CUIAS` nivel 31 (`Assets/_Project/Data/Levels/mundo_02_v1.json`): 8 cuias (K = 7); el rango 31-60 pide 9-11 (03 sec. 11)
- aviso `QA-CUIAS` nivel 36 (`Assets/_Project/Data/Levels/mundo_02_v1.json`): 8 cuias (K = 7); el rango 31-60 pide 9-11 (03 sec. 11)
- aviso `QA-CUIAS` nivel 37 (`Assets/_Project/Data/Levels/mundo_02_v1.json`): 8 cuias (K = 7); el rango 31-60 pide 9-11 (03 sec. 11)
- aviso `QA-CUIAS` nivel 41 (`Assets/_Project/Data/Levels/mundo_03_v1.json`): 8 cuias (K = 6); el rango 31-60 pide 9-11 (03 sec. 11)
- aviso `QA-CUIAS` nivel 42 (`Assets/_Project/Data/Levels/mundo_03_v1.json`): 8 cuias (K = 6); el rango 31-60 pide 9-11 (03 sec. 11)
- aviso `QA-CVD-ADJ` nivel 46 (`Assets/_Project/Data/Levels/mundo_03_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita (reproducido)
- aviso `QA-CVD-ADJ` nivel 46 (`Assets/_Project/Data/Levels/mundo_03_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 46 (`Assets/_Project/Data/Levels/mundo_03_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 47 (`Assets/_Project/Data/Levels/mundo_03_v1.json`): 3 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita (reproducido)
- aviso `QA-CVD-ADJ` nivel 47 (`Assets/_Project/Data/Levels/mundo_03_v1.json`): relief plus_empty: 3 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 47 (`Assets/_Project/Data/Levels/mundo_03_v1.json`): relief minus_color: 3 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CUIAS` nivel 48 (`Assets/_Project/Data/Levels/mundo_03_v1.json`): 8 cuias (K = 7); el rango 31-60 pide 9-11 (03 sec. 11)
- aviso `QA-CVD-ADJ` nivel 48 (`Assets/_Project/Data/Levels/mundo_03_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita (reproducido)
- aviso `QA-CVD-ADJ` nivel 48 (`Assets/_Project/Data/Levels/mundo_03_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 48 (`Assets/_Project/Data/Levels/mundo_03_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CUIAS` nivel 51 (`Assets/_Project/Data/Levels/mundo_03_v1.json`): 8 cuias (K = 7); el rango 31-60 pide 9-11 (03 sec. 11)
- aviso `QA-CVD-ADJ` nivel 52 (`Assets/_Project/Data/Levels/mundo_03_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita (reproducido)
- aviso `QA-CVD-ADJ` nivel 52 (`Assets/_Project/Data/Levels/mundo_03_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 52 (`Assets/_Project/Data/Levels/mundo_03_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CUIAS` nivel 53 (`Assets/_Project/Data/Levels/mundo_03_v1.json`): 8 cuias (K = 7); el rango 31-60 pide 9-11 (03 sec. 11)
- aviso `QA-CUIAS` nivel 54 (`Assets/_Project/Data/Levels/mundo_03_v1.json`): 8 cuias (K = 7); el rango 31-60 pide 9-11 (03 sec. 11)
- aviso `QA-CVD-ADJ` nivel 54 (`Assets/_Project/Data/Levels/mundo_03_v1.json`): 3 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita (reproducido)
- aviso `QA-CVD-ADJ` nivel 54 (`Assets/_Project/Data/Levels/mundo_03_v1.json`): relief plus_empty: 3 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 54 (`Assets/_Project/Data/Levels/mundo_03_v1.json`): relief minus_color: 3 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CUIAS` nivel 55 (`Assets/_Project/Data/Levels/mundo_03_v1.json`): 8 cuias (K = 7); el rango 31-60 pide 9-11 (03 sec. 11)
- aviso `QA-CVD-ADJ` nivel 55 (`Assets/_Project/Data/Levels/mundo_03_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita (reproducido)
- aviso `QA-CVD-ADJ` nivel 55 (`Assets/_Project/Data/Levels/mundo_03_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 55 (`Assets/_Project/Data/Levels/mundo_03_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CUIAS` nivel 56 (`Assets/_Project/Data/Levels/mundo_03_v1.json`): 8 cuias (K = 7); el rango 31-60 pide 9-11 (03 sec. 11)
- aviso `QA-CVD-ADJ` nivel 57 (`Assets/_Project/Data/Levels/mundo_03_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita (reproducido)
- aviso `QA-CVD-ADJ` nivel 57 (`Assets/_Project/Data/Levels/mundo_03_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 58 (`Assets/_Project/Data/Levels/mundo_03_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita (reproducido)
- aviso `QA-CVD-ADJ` nivel 58 (`Assets/_Project/Data/Levels/mundo_03_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 58 (`Assets/_Project/Data/Levels/mundo_03_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CUIAS` nivel 59 (`Assets/_Project/Data/Levels/mundo_03_v1.json`): 8 cuias (K = 7); el rango 31-60 pide 9-11 (03 sec. 11)
- aviso `QA-CUIAS` nivel 61 (`Assets/_Project/Data/Levels/mundo_03_v1.json`): 9 cuias (K = 7); el rango 61-100 pide 10-12 (03 sec. 11)
- aviso `QA-CUIAS` nivel 62 (`Assets/_Project/Data/Levels/mundo_03_v1.json`): 9 cuias (K = 7); el rango 61-100 pide 10-12 (03 sec. 11)
- aviso `QA-CVD-ADJ` nivel 65 (`Assets/_Project/Data/Levels/mundo_03_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita (reproducido)
- aviso `QA-CVD-ADJ` nivel 65 (`Assets/_Project/Data/Levels/mundo_03_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 65 (`Assets/_Project/Data/Levels/mundo_03_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 66 (`Assets/_Project/Data/Levels/mundo_03_v1.json`): 2 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita (reproducido)
- aviso `QA-CVD-ADJ` nivel 66 (`Assets/_Project/Data/Levels/mundo_03_v1.json`): relief plus_empty: 2 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 66 (`Assets/_Project/Data/Levels/mundo_03_v1.json`): relief minus_color: 2 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-WINDOW` nivel 66 (`Assets/_Project/Data/Levels/mundo_03_v1.json`): bot_fail 0.725 fuera de la ventana [0.75, 0.90) (reto)
- aviso `QA-CVD-ADJ` nivel 68 (`Assets/_Project/Data/Levels/mundo_03_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita (reproducido)
- aviso `QA-CVD-ADJ` nivel 68 (`Assets/_Project/Data/Levels/mundo_03_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 68 (`Assets/_Project/Data/Levels/mundo_03_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 71 (`Assets/_Project/Data/Levels/mundo_03_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita (reproducido)
- aviso `QA-CVD-ADJ` nivel 71 (`Assets/_Project/Data/Levels/mundo_03_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 71 (`Assets/_Project/Data/Levels/mundo_03_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CUIAS` nivel 73 (`Assets/_Project/Data/Levels/mundo_03_v1.json`): 9 cuias (K = 8); el rango 61-100 pide 10-12 (03 sec. 11)
- aviso `QA-WINDOW` nivel 75 (`Assets/_Project/Data/Levels/mundo_03_v1.json`): bot_fail 0.725 fuera de la ventana [0.75, 0.90) (reto)
- aviso `QA-CUIAS` nivel 76 (`Assets/_Project/Data/Levels/mundo_03_v1.json`): 9 cuias (K = 8); el rango 61-100 pide 10-12 (03 sec. 11)
- aviso `QA-CVD-ADJ` nivel 78 (`Assets/_Project/Data/Levels/mundo_03_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita (reproducido)
- aviso `QA-CVD-ADJ` nivel 78 (`Assets/_Project/Data/Levels/mundo_03_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 78 (`Assets/_Project/Data/Levels/mundo_03_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 79 (`Assets/_Project/Data/Levels/mundo_03_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita (reproducido)
- aviso `QA-CVD-ADJ` nivel 79 (`Assets/_Project/Data/Levels/mundo_03_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CUIAS` nivel 81 (`Assets/_Project/Data/Levels/mundo_04_v1.json`): 9 cuias (K = 7); el rango 61-100 pide 10-12 (03 sec. 11)
- aviso `QA-CVD-ADJ` nivel 81 (`Assets/_Project/Data/Levels/mundo_04_v1.json`): 3 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita (reproducido)
- aviso `QA-CVD-ADJ` nivel 81 (`Assets/_Project/Data/Levels/mundo_04_v1.json`): relief minus_color: 3 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 81 (`Assets/_Project/Data/Levels/mundo_04_v1.json`): relief plus_empty: 3 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CUIAS` nivel 82 (`Assets/_Project/Data/Levels/mundo_04_v1.json`): 9 cuias (K = 7); el rango 61-100 pide 10-12 (03 sec. 11)
- aviso `QA-CVD-ADJ` nivel 82 (`Assets/_Project/Data/Levels/mundo_04_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita (reproducido)
- aviso `QA-CVD-ADJ` nivel 82 (`Assets/_Project/Data/Levels/mundo_04_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 82 (`Assets/_Project/Data/Levels/mundo_04_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 86 (`Assets/_Project/Data/Levels/mundo_04_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita (reproducido)
- aviso `QA-CVD-ADJ` nivel 86 (`Assets/_Project/Data/Levels/mundo_04_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 86 (`Assets/_Project/Data/Levels/mundo_04_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CUIAS` nivel 88 (`Assets/_Project/Data/Levels/mundo_04_v1.json`): 9 cuias (K = 8); el rango 61-100 pide 10-12 (03 sec. 11)
- aviso `QA-CUIAS` nivel 90 (`Assets/_Project/Data/Levels/mundo_04_v1.json`): 9 cuias (K = 8); el rango 61-100 pide 10-12 (03 sec. 11)
- aviso `QA-CUIAS` nivel 94 (`Assets/_Project/Data/Levels/mundo_04_v1.json`): 9 cuias (K = 8); el rango 61-100 pide 10-12 (03 sec. 11)
- aviso `QA-CUIAS` nivel 96 (`Assets/_Project/Data/Levels/mundo_04_v1.json`): 9 cuias (K = 8); el rango 61-100 pide 10-12 (03 sec. 11)
- aviso `QA-CVD-ADJ` nivel 97 (`Assets/_Project/Data/Levels/mundo_04_v1.json`): 2 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita (reproducido)
- aviso `QA-CVD-ADJ` nivel 97 (`Assets/_Project/Data/Levels/mundo_04_v1.json`): relief minus_color: 2 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 97 (`Assets/_Project/Data/Levels/mundo_04_v1.json`): relief plus_empty: 2 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 98 (`Assets/_Project/Data/Levels/mundo_04_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-BAND-Q` nivel 100 (`Assets/_Project/Data/Levels/mundo_04_v1.json`): hito en banda reto (q 0.899); cumple el minimo: par - 2K = 4 >= media del pool (3.0)
- aviso `QA-CVD-ADJ` nivel 101 (`Assets/_Project/Data/Levels/mundo_04_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita (reproducido)
- aviso `QA-CVD-ADJ` nivel 101 (`Assets/_Project/Data/Levels/mundo_04_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 101 (`Assets/_Project/Data/Levels/mundo_04_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 104 (`Assets/_Project/Data/Levels/mundo_04_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita (reproducido)
- aviso `QA-CVD-ADJ` nivel 104 (`Assets/_Project/Data/Levels/mundo_04_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 104 (`Assets/_Project/Data/Levels/mundo_04_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 107 (`Assets/_Project/Data/Levels/mundo_04_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita (reproducido)
- aviso `QA-CVD-ADJ` nivel 107 (`Assets/_Project/Data/Levels/mundo_04_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 107 (`Assets/_Project/Data/Levels/mundo_04_v1.json`): relief minus_color: 2 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 109 (`Assets/_Project/Data/Levels/mundo_04_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita (reproducido)
- aviso `QA-CVD-ADJ` nivel 109 (`Assets/_Project/Data/Levels/mundo_04_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 109 (`Assets/_Project/Data/Levels/mundo_04_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 113 (`Assets/_Project/Data/Levels/mundo_04_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita (reproducido)
- aviso `QA-CVD-ADJ` nivel 113 (`Assets/_Project/Data/Levels/mundo_04_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 113 (`Assets/_Project/Data/Levels/mundo_04_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 115 (`Assets/_Project/Data/Levels/mundo_04_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita (reproducido)
- aviso `QA-CVD-ADJ` nivel 115 (`Assets/_Project/Data/Levels/mundo_04_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 115 (`Assets/_Project/Data/Levels/mundo_04_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 116 (`Assets/_Project/Data/Levels/mundo_04_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita (reproducido)
- aviso `QA-CVD-ADJ` nivel 116 (`Assets/_Project/Data/Levels/mundo_04_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 116 (`Assets/_Project/Data/Levels/mundo_04_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 117 (`Assets/_Project/Data/Levels/mundo_04_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita (reproducido)
- aviso `QA-CVD-ADJ` nivel 117 (`Assets/_Project/Data/Levels/mundo_04_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 117 (`Assets/_Project/Data/Levels/mundo_04_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 118 (`Assets/_Project/Data/Levels/mundo_04_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita (reproducido)
- aviso `QA-CVD-ADJ` nivel 118 (`Assets/_Project/Data/Levels/mundo_04_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 118 (`Assets/_Project/Data/Levels/mundo_04_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 120 (`Assets/_Project/Data/Levels/mundo_04_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita (reproducido)
- aviso `QA-CVD-ADJ` nivel 120 (`Assets/_Project/Data/Levels/mundo_04_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 120 (`Assets/_Project/Data/Levels/mundo_04_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 130 (`Assets/_Project/Data/Levels/mundo_04_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita (reproducido)
- aviso `QA-CVD-ADJ` nivel 130 (`Assets/_Project/Data/Levels/mundo_04_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 131 (`Assets/_Project/Data/Levels/mundo_04_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita (reproducido)
- aviso `QA-CVD-ADJ` nivel 131 (`Assets/_Project/Data/Levels/mundo_04_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 131 (`Assets/_Project/Data/Levels/mundo_04_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 133 (`Assets/_Project/Data/Levels/mundo_04_v1.json`): 2 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita (reproducido)
- aviso `QA-CVD-ADJ` nivel 133 (`Assets/_Project/Data/Levels/mundo_04_v1.json`): relief plus_empty: 2 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 133 (`Assets/_Project/Data/Levels/mundo_04_v1.json`): relief minus_color: 2 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 136 (`Assets/_Project/Data/Levels/mundo_04_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita (reproducido)
- aviso `QA-CVD-ADJ` nivel 136 (`Assets/_Project/Data/Levels/mundo_04_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 136 (`Assets/_Project/Data/Levels/mundo_04_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 139 (`Assets/_Project/Data/Levels/mundo_04_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita (reproducido)
- aviso `QA-CVD-ADJ` nivel 139 (`Assets/_Project/Data/Levels/mundo_04_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 139 (`Assets/_Project/Data/Levels/mundo_04_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 146 (`Assets/_Project/Data/Levels/mundo_05_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita (reproducido)
- aviso `QA-CVD-ADJ` nivel 146 (`Assets/_Project/Data/Levels/mundo_05_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 146 (`Assets/_Project/Data/Levels/mundo_05_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 148 (`Assets/_Project/Data/Levels/mundo_05_v1.json`): 3 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita (reproducido)
- aviso `QA-CVD-ADJ` nivel 148 (`Assets/_Project/Data/Levels/mundo_05_v1.json`): relief minus_color: 3 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 148 (`Assets/_Project/Data/Levels/mundo_05_v1.json`): relief plus_empty: 3 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 149 (`Assets/_Project/Data/Levels/mundo_05_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita (reproducido)
- aviso `QA-CVD-ADJ` nivel 149 (`Assets/_Project/Data/Levels/mundo_05_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 149 (`Assets/_Project/Data/Levels/mundo_05_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 150 (`Assets/_Project/Data/Levels/mundo_05_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita (reproducido)
- aviso `QA-CVD-ADJ` nivel 150 (`Assets/_Project/Data/Levels/mundo_05_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 150 (`Assets/_Project/Data/Levels/mundo_05_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 152 (`Assets/_Project/Data/Levels/mundo_05_v1.json`): 2 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita (reproducido)
- aviso `QA-CVD-ADJ` nivel 152 (`Assets/_Project/Data/Levels/mundo_05_v1.json`): relief plus_empty: 2 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 152 (`Assets/_Project/Data/Levels/mundo_05_v1.json`): relief minus_color: 2 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 156 (`Assets/_Project/Data/Levels/mundo_05_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita (reproducido)
- aviso `QA-CVD-ADJ` nivel 156 (`Assets/_Project/Data/Levels/mundo_05_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 156 (`Assets/_Project/Data/Levels/mundo_05_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-WINDOW` nivel 166 (`Assets/_Project/Data/Levels/mundo_05_v1.json`): bot_fail 0.675 fuera de la ventana [0.75, 0.90) (reto)
- aviso `QA-CVD-ADJ` nivel 169 (`Assets/_Project/Data/Levels/mundo_05_v1.json`): 2 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita (reproducido)
- aviso `QA-CVD-ADJ` nivel 169 (`Assets/_Project/Data/Levels/mundo_05_v1.json`): relief plus_empty: 2 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 169 (`Assets/_Project/Data/Levels/mundo_05_v1.json`): relief minus_color: 2 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 179 (`Assets/_Project/Data/Levels/mundo_05_v1.json`): 2 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita (reproducido)
- aviso `QA-CVD-ADJ` nivel 179 (`Assets/_Project/Data/Levels/mundo_05_v1.json`): relief minus_color: 2 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 179 (`Assets/_Project/Data/Levels/mundo_05_v1.json`): relief plus_empty: 2 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 180 (`Assets/_Project/Data/Levels/mundo_05_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita (reproducido)
- aviso `QA-CVD-ADJ` nivel 180 (`Assets/_Project/Data/Levels/mundo_05_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 180 (`Assets/_Project/Data/Levels/mundo_05_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 183 (`Assets/_Project/Data/Levels/mundo_05_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita (reproducido)
- aviso `QA-CVD-ADJ` nivel 183 (`Assets/_Project/Data/Levels/mundo_05_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 183 (`Assets/_Project/Data/Levels/mundo_05_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 189 (`Assets/_Project/Data/Levels/mundo_05_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita (reproducido)
- aviso `QA-CVD-ADJ` nivel 189 (`Assets/_Project/Data/Levels/mundo_05_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 189 (`Assets/_Project/Data/Levels/mundo_05_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-WINDOW` nivel 189 (`Assets/_Project/Data/Levels/mundo_05_v1.json`): bot_fail 0.725 fuera de la ventana [0.75, 0.90) (reto)
- aviso `QA-CVD-ADJ` nivel 192 (`Assets/_Project/Data/Levels/mundo_05_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita (reproducido)
- aviso `QA-CVD-ADJ` nivel 192 (`Assets/_Project/Data/Levels/mundo_05_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 192 (`Assets/_Project/Data/Levels/mundo_05_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 195 (`Assets/_Project/Data/Levels/mundo_05_v1.json`): 2 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita (reproducido)
- aviso `QA-CVD-ADJ` nivel 195 (`Assets/_Project/Data/Levels/mundo_05_v1.json`): relief plus_empty: 2 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 195 (`Assets/_Project/Data/Levels/mundo_05_v1.json`): relief minus_color: 2 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 198 (`Assets/_Project/Data/Levels/mundo_05_v1.json`): 2 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita (reproducido)
- aviso `QA-CVD-ADJ` nivel 198 (`Assets/_Project/Data/Levels/mundo_05_v1.json`): relief plus_empty: 2 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 198 (`Assets/_Project/Data/Levels/mundo_05_v1.json`): relief minus_color: 3 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 200 (`Assets/_Project/Data/Levels/mundo_05_v1.json`): 2 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita (reproducido)
- aviso `QA-CVD-ADJ` nivel 200 (`Assets/_Project/Data/Levels/mundo_05_v1.json`): relief plus_empty: 2 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 200 (`Assets/_Project/Data/Levels/mundo_05_v1.json`): relief minus_color: 2 contacto(s) de un par del simulador de daltonismo en una cuia
