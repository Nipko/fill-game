# Informe de `level-qa`

Generado por `dotnet run --project tools/level-qa -c Release -- tools/out/curva_f3b/curva_07_v1.json tools/out/curva_f3b/curva_08_v1.json tools/out/curva_f3b/curva_09_v1.json --spec tools/level-generator/specs/campaign_v3.json --levels 301-700 --report tools/out/curva_f3b/qa_curva2.md`: no se edita a mano.

- Niveles: 400; fallos: **0**; avisos: 281.

| Comprobación | Regla | Fallos |
|---|---|---|
| `QA-STRUCT` | caps, cuias y attrs de la misma longitud; <= 14 cuias generadas (02 R1; 08 sec. 4.1) | 0 |
| `QA-UNITS` | U_c coherente con units_per_color y con las capacidades (02 R3, sec. 8; 03 sec. 17 #5) | 0 |
| `QA-SEALED` | ninguna cuia sellada al inicio (03 sec. 17 #5) | 0 |
| `QA-STUCK` | atascado(S0) = false (02 sec. 9.2 regla 1) | 0 |
| `QA-VALID` | resto de LevelValidation de AF.Core (mecanicas, Barro, Aguas turbias) | 0 |
| `QA-PAR` | par recalculado (A* w = 1, run) igual al publicado; par >= 2K; dentro del par objetivo del rango +-30 % (03 sec. 17 #1-#2), salvo los tableros de estreno de la Tinaja (K <= 6; D-027) y la curva 2, que solo pide par >= 2K (03 sec. 11; D-068) | 0 |
| `QA-TINT-DEBUT` | cada tinte con debut anterior o igual al nivel (03 sec. 17 #4) | 0 |
| `QA-KMAX` | K <= k_max y >= k_min de la cabecera (03 sec. 17 #4) | 0 |
| `QA-RISK` | risk_pairs recalculado igual al publicado y <= max_risk_pairs (03 sec. 4.5) | 0 |
| `QA-CVD-ADJ` | ningun par del simulador de daltonismo (cvd_pairs: 7-10, 5-14) junto en una cuia del tablero base; aviso en las variantes de relief y si ninguna de las 50 permutaciones del generador lo evita midiendo igual (con la reproduccion) (Q-B9-13 bis; 07 sec. 3) | 0 |
| `QA-PALETTE` | palette = tintes presentes en cuias (08 sec. 4.1) | 0 |
| `QA-BOT` | bot_fail recalculado con la semilla del nivel identico al publicado (03 sec. 6) | 0 |
| `QA-LABEL` | etiqueta coherente: >= 0,75 hard y >= 0,90 very_hard, desde el 40 y el 60 (03 sec. 7, 11) | 0 |
| `QA-LABEL-EARLY` | ningun nivel mide >= 0,90 antes del 60 ni >= 0,75 antes del 40, aunque se publique con la etiqueta inferior (contrato 6; 03 sec. 11-12) | 0 |
| `QA-CUIAS` | aviso: cuias del nivel dentro de las cuias totales del rango (03 sec. 11); en la curva 2, QA-T-ROLE | 0 |
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
| `QA-RELIEF-PLUS` | plus_empty = el tablero base con exactamente un vacio de 4 mas (al final; con tinaja o Aguape, en la ranura que deja la pareja o cada grupo en una fila; con la Lancha, k puede subir) (03 sec. 13; D-068) | 0 |
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
| `QA-MURKY-T1` | Aguas turbias solo desde el 100 y en niveles Dificil o Muy dificil (02 T1); la tinaja queda fuera (excepcion de D-019, Q-Ti1 A); en la curva 2 las Normales las comprueba QA-MURKY-NORMAL (D-068) | 0 |
| `QA-MURKY-LIMITS` | como mucho 3 cuias turbias y 4 dosis ocultas; T4 (02 T2-T4) | 0 |
| `QA-MURKY-SAFE` | ruta segura en cada estado de la ruta (03 sec. 8; 02 T5) | 0 |
| `QA-MURKY-COMBO` | sin Lluvia, Pirana, Liana ni Remolino en el nivel (Q14 A; [P] Remolino) | 0 |
| `QA-MURKY-Q9` | estreno del 100: una cuia, 2 dosis ocultas y ninguna otra mecanica (Q9); el estreno en Normal de la curva 2, QA-MURKY-NORMAL | 0 |
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
| `QA-T-ROLE` | curva 2: T y K dentro del tope de su papel (role_caps; 03 sec. 12 y sec. 17 #16); en kind curve, replaces = el id de la curva 1 del mismo slot e id/seed con _c{c}/:c{c}; aviso si difficulty.pressure no es el de las palancas del tablero (D-068) | 0 |
| `QA-MURKY-NORMAL` | curva 2: Normal con turbias solo en comodidad, con 1 cuia y <= 2 dosis ocultas, desde murky_normal.from_level; por ciclo <= comfort_per_cycle comodidades, <= 2 retos y <= 1 hito cada 2 ciclos con turbias (02 sec. 11 T1; 03 sec. 17 #19) | 0 |
| `QA-AGUAPE` | grupos de 2-5 cuias contiguas sin Barro (espeso n = 2: 4-5); rows con >= 11 cuias, que suma las cuias y respeta 02 sec. 5; LevelValidation.ValidateRows en el nivel y en cada relief; una cuia que sella solo recibiendo (dos en el espeso); atascado(S0) = false; el grupo se suelta en la solucion; estreno solo en su nivel; nunca con la tinaja (03 sec. 17 #17) | 0 |
| `QA-LANCHA` | una lancha por nivel; carga de 1-3 dosis visibles sin Barro; k >= vacios + 1; sin Aguas turbias; con la Liana solo desde el 401; estreno solo en su nivel; atascado(S0) = false y la lancha atraca en la solucion (03 sec. 17 #18) | 0 |
| `QA-BOT-ATENTO` | aviso: dentro de cada mundo de la curva 2, bot atento x40 del hito n+1 >= el del hito n (03 sec. 17 #20; solo informe) | 0 |
| `QA-SCHEMA` | JSON valido contra schemas/af-level-1.schema.json con python tools/validate_schema.py --pack; sin Python o sin jsonschema, falla (08 sec. 4.1; 03 sec. 17 #11) | 0 |
| `QA-REPRO` | la especificacion y las semillas reproducen el pack byte a byte: se regenera la campana completa en memoria (03 sec. 17 #10; 08 sec. 2.4 y 4.1) | 0 |
| `QA-BAND-Q` | banda medida (cuantil de D en el pool del rango) acorde al rol: comodidad, estreno, remanso y rampa <= 0,70; recuperacion <= 0,10 o D bajo la comodidad mas facil del ciclo; hito > 0,95 o par - 2K >= media del pool (03 sec. 7; c.3 reglas 3 y 5) | 0 |

## Bot atento ×40 de los retos y los hitos (03 §17 #20; solo informe)

| Nivel | Papel | Etiqueta | Bot casual | Bot atento |
|---|---|---|---|---|
| 306 | reto | hard | 80 % | 83 % |
| 309 | reto | hard | 88 % | 28 % |
| 312 | reto | hard | 85 % | 73 % |
| 315 | reto | hard | 88 % | 100 % |
| 318 | reto | hard | 80 % | 60 % |
| 320 | hito | very_hard | 93 % | 90 % |
| 326 | reto | hard | 75 % | 65 % |
| 329 | reto | normal | 73 % | 40 % |
| 332 | reto | hard | 85 % | 63 % |
| 335 | reto | hard | 88 % | 90 % |
| 338 | reto | hard | 83 % | 93 % |
| 340 | hito | very_hard | 100 % | 100 % |
| 346 | reto | hard | 78 % | 18 % |
| 349 | reto | hard | 80 % | 35 % |
| 352 | reto | normal | 58 % | 25 % |
| 355 | reto | hard | 88 % | 23 % |
| 358 | reto | hard | 83 % | 55 % |
| 360 | hito | very_hard | 100 % | 83 % |
| 366 | reto | hard | 88 % | 83 % |
| 369 | reto | hard | 75 % | 68 % |
| 372 | reto | hard | 85 % | 25 % |
| 375 | reto | hard | 88 % | 28 % |
| 378 | reto | hard | 75 % | 73 % |
| 380 | hito | very_hard | 93 % | 43 % |
| 386 | reto | hard | 85 % | 53 % |
| 389 | reto | hard | 85 % | 50 % |
| 392 | reto | hard | 88 % | 0 % |
| 395 | reto | hard | 85 % | 55 % |
| 398 | reto | hard | 88 % | 73 % |
| 400 | hito | very_hard | 100 % | 100 % |
| 406 | reto | hard | 78 % | 90 % |
| 409 | reto | hard | 75 % | 48 % |
| 412 | reto | hard | 78 % | 38 % |
| 415 | reto | hard | 85 % | 60 % |
| 418 | reto | hard | 88 % | 5 % |
| 420 | hito | very_hard | 100 % | 100 % |
| 426 | reto | hard | 78 % | 60 % |
| 429 | reto | hard | 75 % | 58 % |
| 432 | reto | hard | 75 % | 75 % |
| 435 | reto | hard | 88 % | 98 % |
| 438 | reto | hard | 78 % | 75 % |
| 440 | hito | very_hard | 100 % | 100 % |
| 446 | reto | hard | 88 % | 43 % |
| 449 | reto | normal | 73 % | 40 % |
| 452 | reto | hard | 83 % | 80 % |
| 455 | reto | hard | 78 % | 8 % |
| 458 | reto | hard | 80 % | 45 % |
| 460 | hito | very_hard | 100 % | 85 % |
| 466 | reto | hard | 88 % | 48 % |
| 469 | reto | hard | 83 % | 40 % |
| 472 | reto | hard | 75 % | 3 % |
| 475 | reto | hard | 75 % | 13 % |
| 478 | reto | hard | 85 % | 3 % |
| 480 | hito | very_hard | 100 % | 100 % |
| 486 | reto | hard | 78 % | 13 % |
| 489 | reto | hard | 83 % | 13 % |
| 492 | reto | hard | 85 % | 58 % |
| 495 | reto | hard | 83 % | 55 % |
| 498 | reto | hard | 85 % | 58 % |
| 500 | hito | very_hard | 95 % | 95 % |
| 506 | reto | hard | 88 % | 45 % |
| 509 | reto | normal | 73 % | 70 % |
| 512 | reto | hard | 88 % | 63 % |
| 515 | reto | hard | 88 % | 73 % |
| 518 | reto | hard | 80 % | 90 % |
| 520 | hito | very_hard | 100 % | 73 % |
| 526 | reto | hard | 85 % | 83 % |
| 529 | reto | hard | 80 % | 65 % |
| 532 | reto | hard | 85 % | 63 % |
| 535 | reto | hard | 83 % | 83 % |
| 538 | reto | hard | 80 % | 35 % |
| 540 | hito | very_hard | 98 % | 93 % |
| 546 | reto | hard | 80 % | 98 % |
| 549 | reto | normal | 73 % | 48 % |
| 552 | reto | hard | 88 % | 70 % |
| 555 | reto | hard | 83 % | 85 % |
| 558 | reto | hard | 83 % | 65 % |
| 560 | hito | very_hard | 93 % | 78 % |
| 566 | reto | hard | 75 % | 53 % |
| 569 | reto | hard | 75 % | 35 % |
| 572 | reto | hard | 80 % | 13 % |
| 575 | reto | hard | 80 % | 55 % |
| 578 | reto | hard | 78 % | 23 % |
| 580 | hito | very_hard | 100 % | 90 % |
| 586 | reto | hard | 83 % | 38 % |
| 589 | reto | hard | 88 % | 23 % |
| 592 | reto | hard | 78 % | 90 % |
| 595 | reto | hard | 88 % | 90 % |
| 598 | reto | hard | 88 % | 68 % |
| 600 | hito | very_hard | 100 % | 75 % |
| 606 | reto | hard | 80 % | 53 % |
| 609 | reto | hard | 80 % | 40 % |
| 612 | reto | hard | 88 % | 60 % |
| 615 | reto | hard | 85 % | 35 % |
| 618 | reto | hard | 85 % | 5 % |
| 620 | hito | very_hard | 100 % | 98 % |
| 626 | reto | hard | 88 % | 15 % |
| 629 | reto | hard | 88 % | 23 % |
| 632 | reto | hard | 83 % | 68 % |
| 635 | reto | hard | 85 % | 50 % |
| 638 | reto | hard | 75 % | 48 % |
| 640 | hito | very_hard | 95 % | 93 % |
| 646 | reto | hard | 80 % | 3 % |
| 649 | reto | hard | 83 % | 75 % |
| 652 | reto | hard | 83 % | 93 % |
| 655 | reto | hard | 85 % | 58 % |
| 658 | reto | hard | 85 % | 63 % |
| 660 | hito | very_hard | 98 % | 95 % |
| 666 | reto | hard | 88 % | 13 % |
| 669 | reto | hard | 85 % | 13 % |
| 672 | reto | normal | 70 % | 15 % |
| 675 | reto | hard | 80 % | 55 % |
| 678 | reto | hard | 88 % | 68 % |
| 680 | hito | very_hard | 100 % | 100 % |
| 686 | reto | normal | 73 % | 95 % |
| 689 | reto | hard | 80 % | 0 % |
| 692 | reto | hard | 80 % | 8 % |
| 695 | reto | hard | 88 % | 83 % |
| 698 | reto | hard | 88 % | 85 % |
| 700 | hito | very_hard | 100 % | 100 % |

## Incidencias

- aviso `QA-CVD-ADJ` nivel 306 (`tools/out/curva_f3b/curva_07_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 306 (`tools/out/curva_f3b/curva_07_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 306 (`tools/out/curva_f3b/curva_07_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 307 (`tools/out/curva_f3b/curva_07_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 307 (`tools/out/curva_f3b/curva_07_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 307 (`tools/out/curva_f3b/curva_07_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 311 (`tools/out/curva_f3b/curva_07_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 311 (`tools/out/curva_f3b/curva_07_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 311 (`tools/out/curva_f3b/curva_07_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 312 (`tools/out/curva_f3b/curva_07_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 313 (`tools/out/curva_f3b/curva_07_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 313 (`tools/out/curva_f3b/curva_07_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 313 (`tools/out/curva_f3b/curva_07_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-BAND-Q` nivel 320 (`tools/out/curva_f3b/curva_07_v1.json`): hito en banda reto (q 0.935); cumple el minimo: par - 2K = 3 >= media del pool (2.5)
- aviso `QA-CVD-ADJ` nivel 320 (`tools/out/curva_f3b/curva_07_v1.json`): 2 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 320 (`tools/out/curva_f3b/curva_07_v1.json`): relief plus_empty: 2 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 320 (`tools/out/curva_f3b/curva_07_v1.json`): relief minus_color: 2 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-WINDOW` nivel 329 (`tools/out/curva_f3b/curva_07_v1.json`): bot_fail 0.725 fuera de la ventana [0.75, 0.90) (reto)
- aviso `QA-CVD-ADJ` nivel 343 (`tools/out/curva_f3b/curva_07_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 345 (`tools/out/curva_f3b/curva_07_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 345 (`tools/out/curva_f3b/curva_07_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 345 (`tools/out/curva_f3b/curva_07_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 349 (`tools/out/curva_f3b/curva_07_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 349 (`tools/out/curva_f3b/curva_07_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 349 (`tools/out/curva_f3b/curva_07_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-WINDOW` nivel 352 (`tools/out/curva_f3b/curva_07_v1.json`): bot_fail 0.575 fuera de la ventana [0.75, 0.90) (reto)
- aviso `QA-CVD-ADJ` nivel 359 (`tools/out/curva_f3b/curva_07_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 359 (`tools/out/curva_f3b/curva_07_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 359 (`tools/out/curva_f3b/curva_07_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-BOT-ATENTO` nivel 360 (`tools/out/curva_f3b/curva_07_v1.json`): bot atento 83 % en el hito 360, por debajo del hito 340 (100 %): el pico no crece (para la curacion)
- aviso `QA-CVD-ADJ` nivel 369 (`tools/out/curva_f3b/curva_07_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 369 (`tools/out/curva_f3b/curva_07_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 369 (`tools/out/curva_f3b/curva_07_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 371 (`tools/out/curva_f3b/curva_07_v1.json`): 2 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 371 (`tools/out/curva_f3b/curva_07_v1.json`): relief plus_empty: 2 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 371 (`tools/out/curva_f3b/curva_07_v1.json`): relief minus_color: 2 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 372 (`tools/out/curva_f3b/curva_07_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 372 (`tools/out/curva_f3b/curva_07_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-BOT-ATENTO` nivel 380 (`tools/out/curva_f3b/curva_07_v1.json`): bot atento 43 % en el hito 380, por debajo del hito 360 (83 %): el pico no crece (para la curacion)
- aviso `QA-CVD-ADJ` nivel 383 (`tools/out/curva_f3b/curva_07_v1.json`): 2 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 383 (`tools/out/curva_f3b/curva_07_v1.json`): relief minus_color: 2 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 383 (`tools/out/curva_f3b/curva_07_v1.json`): relief plus_empty: 2 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 384 (`tools/out/curva_f3b/curva_07_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 384 (`tools/out/curva_f3b/curva_07_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 384 (`tools/out/curva_f3b/curva_07_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 386 (`tools/out/curva_f3b/curva_07_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 386 (`tools/out/curva_f3b/curva_07_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 386 (`tools/out/curva_f3b/curva_07_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 389 (`tools/out/curva_f3b/curva_07_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 389 (`tools/out/curva_f3b/curva_07_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 389 (`tools/out/curva_f3b/curva_07_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 393 (`tools/out/curva_f3b/curva_07_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 393 (`tools/out/curva_f3b/curva_07_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 393 (`tools/out/curva_f3b/curva_07_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 395 (`tools/out/curva_f3b/curva_07_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 395 (`tools/out/curva_f3b/curva_07_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 397 (`tools/out/curva_f3b/curva_07_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 397 (`tools/out/curva_f3b/curva_07_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 397 (`tools/out/curva_f3b/curva_07_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 400 (`tools/out/curva_f3b/curva_07_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 400 (`tools/out/curva_f3b/curva_07_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 404 (`tools/out/curva_f3b/curva_07_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 404 (`tools/out/curva_f3b/curva_07_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 404 (`tools/out/curva_f3b/curva_07_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 409 (`tools/out/curva_f3b/curva_07_v1.json`): 2 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 409 (`tools/out/curva_f3b/curva_07_v1.json`): relief plus_empty: 2 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 409 (`tools/out/curva_f3b/curva_07_v1.json`): relief minus_color: 2 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 411 (`tools/out/curva_f3b/curva_07_v1.json`): 2 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 411 (`tools/out/curva_f3b/curva_07_v1.json`): relief plus_empty: 2 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 411 (`tools/out/curva_f3b/curva_07_v1.json`): relief minus_color: 2 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 413 (`tools/out/curva_f3b/curva_07_v1.json`): 2 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 413 (`tools/out/curva_f3b/curva_07_v1.json`): relief minus_color: 2 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 413 (`tools/out/curva_f3b/curva_07_v1.json`): relief plus_empty: 2 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 414 (`tools/out/curva_f3b/curva_07_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 414 (`tools/out/curva_f3b/curva_07_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 414 (`tools/out/curva_f3b/curva_07_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 431 (`tools/out/curva_f3b/curva_08_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 435 (`tools/out/curva_f3b/curva_08_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 436 (`tools/out/curva_f3b/curva_08_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 436 (`tools/out/curva_f3b/curva_08_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 436 (`tools/out/curva_f3b/curva_08_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-BAND-Q` nivel 442 (`tools/out/curva_f3b/curva_08_v1.json`): recuperacion en banda comodidad (q 0.284); cumple el minimo: D -0.33 < comodidad mas facil del ciclo (-0.30)
- aviso `QA-CVD-ADJ` nivel 443 (`tools/out/curva_f3b/curva_08_v1.json`): 3 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 443 (`tools/out/curva_f3b/curva_08_v1.json`): relief plus_empty: 3 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 443 (`tools/out/curva_f3b/curva_08_v1.json`): relief minus_color: 3 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 445 (`tools/out/curva_f3b/curva_08_v1.json`): 2 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 445 (`tools/out/curva_f3b/curva_08_v1.json`): relief plus_empty: 2 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 445 (`tools/out/curva_f3b/curva_08_v1.json`): relief minus_color: 2 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-WINDOW` nivel 449 (`tools/out/curva_f3b/curva_08_v1.json`): bot_fail 0.725 fuera de la ventana [0.75, 0.90) (reto)
- aviso `QA-CVD-ADJ` nivel 454 (`tools/out/curva_f3b/curva_08_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 454 (`tools/out/curva_f3b/curva_08_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 454 (`tools/out/curva_f3b/curva_08_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 456 (`tools/out/curva_f3b/curva_08_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 456 (`tools/out/curva_f3b/curva_08_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-BOT-ATENTO` nivel 460 (`tools/out/curva_f3b/curva_08_v1.json`): bot atento 85 % en el hito 460, por debajo del hito 440 (100 %): el pico no crece (para la curacion)
- aviso `QA-CVD-ADJ` nivel 460 (`tools/out/curva_f3b/curva_08_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 460 (`tools/out/curva_f3b/curva_08_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 460 (`tools/out/curva_f3b/curva_08_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 466 (`tools/out/curva_f3b/curva_08_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 466 (`tools/out/curva_f3b/curva_08_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 472 (`tools/out/curva_f3b/curva_08_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 472 (`tools/out/curva_f3b/curva_08_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 472 (`tools/out/curva_f3b/curva_08_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 480 (`tools/out/curva_f3b/curva_08_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 480 (`tools/out/curva_f3b/curva_08_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 480 (`tools/out/curva_f3b/curva_08_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 484 (`tools/out/curva_f3b/curva_08_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 484 (`tools/out/curva_f3b/curva_08_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 484 (`tools/out/curva_f3b/curva_08_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 491 (`tools/out/curva_f3b/curva_08_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 491 (`tools/out/curva_f3b/curva_08_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 491 (`tools/out/curva_f3b/curva_08_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 493 (`tools/out/curva_f3b/curva_08_v1.json`): 2 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 493 (`tools/out/curva_f3b/curva_08_v1.json`): relief plus_empty: 2 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 493 (`tools/out/curva_f3b/curva_08_v1.json`): relief minus_color: 2 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 494 (`tools/out/curva_f3b/curva_08_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 494 (`tools/out/curva_f3b/curva_08_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 496 (`tools/out/curva_f3b/curva_08_v1.json`): 3 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 496 (`tools/out/curva_f3b/curva_08_v1.json`): relief minus_color: 3 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 496 (`tools/out/curva_f3b/curva_08_v1.json`): relief plus_empty: 3 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 498 (`tools/out/curva_f3b/curva_08_v1.json`): 2 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 498 (`tools/out/curva_f3b/curva_08_v1.json`): relief minus_color: 2 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 498 (`tools/out/curva_f3b/curva_08_v1.json`): relief plus_empty: 2 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-BOT-ATENTO` nivel 500 (`tools/out/curva_f3b/curva_08_v1.json`): bot atento 95 % en el hito 500, por debajo del hito 480 (100 %): el pico no crece (para la curacion)
- aviso `QA-CVD-ADJ` nivel 500 (`tools/out/curva_f3b/curva_08_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 500 (`tools/out/curva_f3b/curva_08_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 503 (`tools/out/curva_f3b/curva_08_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 503 (`tools/out/curva_f3b/curva_08_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 503 (`tools/out/curva_f3b/curva_08_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 508 (`tools/out/curva_f3b/curva_08_v1.json`): 2 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 508 (`tools/out/curva_f3b/curva_08_v1.json`): relief plus_empty: 2 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 508 (`tools/out/curva_f3b/curva_08_v1.json`): relief minus_color: 2 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 509 (`tools/out/curva_f3b/curva_08_v1.json`): 3 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 509 (`tools/out/curva_f3b/curva_08_v1.json`): relief minus_color: 3 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 509 (`tools/out/curva_f3b/curva_08_v1.json`): relief plus_empty: 3 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-WINDOW` nivel 509 (`tools/out/curva_f3b/curva_08_v1.json`): bot_fail 0.725 fuera de la ventana [0.75, 0.90) (reto)
- aviso `QA-CVD-ADJ` nivel 510 (`tools/out/curva_f3b/curva_08_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 510 (`tools/out/curva_f3b/curva_08_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 510 (`tools/out/curva_f3b/curva_08_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 512 (`tools/out/curva_f3b/curva_08_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 512 (`tools/out/curva_f3b/curva_08_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 512 (`tools/out/curva_f3b/curva_08_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 513 (`tools/out/curva_f3b/curva_08_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 513 (`tools/out/curva_f3b/curva_08_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 516 (`tools/out/curva_f3b/curva_08_v1.json`): 4 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 516 (`tools/out/curva_f3b/curva_08_v1.json`): relief minus_color: 4 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 516 (`tools/out/curva_f3b/curva_08_v1.json`): relief plus_empty: 4 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 518 (`tools/out/curva_f3b/curva_08_v1.json`): 2 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 518 (`tools/out/curva_f3b/curva_08_v1.json`): relief plus_empty: 2 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 518 (`tools/out/curva_f3b/curva_08_v1.json`): relief minus_color: 2 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-BOT-ATENTO` nivel 520 (`tools/out/curva_f3b/curva_08_v1.json`): bot atento 73 % en el hito 520, por debajo del hito 500 (95 %): el pico no crece (para la curacion)
- aviso `QA-CVD-ADJ` nivel 521 (`tools/out/curva_f3b/curva_08_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 521 (`tools/out/curva_f3b/curva_08_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 521 (`tools/out/curva_f3b/curva_08_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 524 (`tools/out/curva_f3b/curva_08_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 524 (`tools/out/curva_f3b/curva_08_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 524 (`tools/out/curva_f3b/curva_08_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 525 (`tools/out/curva_f3b/curva_08_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 525 (`tools/out/curva_f3b/curva_08_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 529 (`tools/out/curva_f3b/curva_08_v1.json`): 3 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 529 (`tools/out/curva_f3b/curva_08_v1.json`): relief plus_empty: 3 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 529 (`tools/out/curva_f3b/curva_08_v1.json`): relief minus_color: 3 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 533 (`tools/out/curva_f3b/curva_08_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 533 (`tools/out/curva_f3b/curva_08_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 533 (`tools/out/curva_f3b/curva_08_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 534 (`tools/out/curva_f3b/curva_08_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 534 (`tools/out/curva_f3b/curva_08_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 534 (`tools/out/curva_f3b/curva_08_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 536 (`tools/out/curva_f3b/curva_08_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 536 (`tools/out/curva_f3b/curva_08_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 536 (`tools/out/curva_f3b/curva_08_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 542 (`tools/out/curva_f3b/curva_08_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 542 (`tools/out/curva_f3b/curva_08_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 542 (`tools/out/curva_f3b/curva_08_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 544 (`tools/out/curva_f3b/curva_08_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 544 (`tools/out/curva_f3b/curva_08_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 544 (`tools/out/curva_f3b/curva_08_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 546 (`tools/out/curva_f3b/curva_08_v1.json`): 4 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 546 (`tools/out/curva_f3b/curva_08_v1.json`): relief plus_empty: 4 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 546 (`tools/out/curva_f3b/curva_08_v1.json`): relief minus_color: 4 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 547 (`tools/out/curva_f3b/curva_08_v1.json`): 2 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 547 (`tools/out/curva_f3b/curva_08_v1.json`): relief minus_color: 2 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 547 (`tools/out/curva_f3b/curva_08_v1.json`): relief plus_empty: 2 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-WINDOW` nivel 549 (`tools/out/curva_f3b/curva_08_v1.json`): bot_fail 0.725 fuera de la ventana [0.75, 0.90) (reto)
- aviso `QA-CVD-ADJ` nivel 558 (`tools/out/curva_f3b/curva_08_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 558 (`tools/out/curva_f3b/curva_08_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 558 (`tools/out/curva_f3b/curva_08_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 559 (`tools/out/curva_f3b/curva_08_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 559 (`tools/out/curva_f3b/curva_08_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 559 (`tools/out/curva_f3b/curva_08_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-BOT-ATENTO` nivel 560 (`tools/out/curva_f3b/curva_08_v1.json`): bot atento 78 % en el hito 560, por debajo del hito 540 (93 %): el pico no crece (para la curacion)
- aviso `QA-CVD-ADJ` nivel 562 (`tools/out/curva_f3b/curva_09_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 562 (`tools/out/curva_f3b/curva_09_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 566 (`tools/out/curva_f3b/curva_09_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 566 (`tools/out/curva_f3b/curva_09_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 566 (`tools/out/curva_f3b/curva_09_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 572 (`tools/out/curva_f3b/curva_09_v1.json`): 2 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 572 (`tools/out/curva_f3b/curva_09_v1.json`): relief plus_empty: 2 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 572 (`tools/out/curva_f3b/curva_09_v1.json`): relief minus_color: 2 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 573 (`tools/out/curva_f3b/curva_09_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 573 (`tools/out/curva_f3b/curva_09_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 573 (`tools/out/curva_f3b/curva_09_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 580 (`tools/out/curva_f3b/curva_09_v1.json`): 2 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 580 (`tools/out/curva_f3b/curva_09_v1.json`): relief minus_color: 2 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 592 (`tools/out/curva_f3b/curva_09_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 593 (`tools/out/curva_f3b/curva_09_v1.json`): 2 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 593 (`tools/out/curva_f3b/curva_09_v1.json`): relief minus_color: 2 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 593 (`tools/out/curva_f3b/curva_09_v1.json`): relief plus_empty: 2 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 594 (`tools/out/curva_f3b/curva_09_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 594 (`tools/out/curva_f3b/curva_09_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 595 (`tools/out/curva_f3b/curva_09_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 595 (`tools/out/curva_f3b/curva_09_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 595 (`tools/out/curva_f3b/curva_09_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 598 (`tools/out/curva_f3b/curva_09_v1.json`): 2 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 598 (`tools/out/curva_f3b/curva_09_v1.json`): relief plus_empty: 2 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 598 (`tools/out/curva_f3b/curva_09_v1.json`): relief minus_color: 2 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-BOT-ATENTO` nivel 600 (`tools/out/curva_f3b/curva_09_v1.json`): bot atento 75 % en el hito 600, por debajo del hito 580 (90 %): el pico no crece (para la curacion)
- aviso `QA-CVD-ADJ` nivel 600 (`tools/out/curva_f3b/curva_09_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 600 (`tools/out/curva_f3b/curva_09_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 600 (`tools/out/curva_f3b/curva_09_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 601 (`tools/out/curva_f3b/curva_09_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 601 (`tools/out/curva_f3b/curva_09_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 601 (`tools/out/curva_f3b/curva_09_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 605 (`tools/out/curva_f3b/curva_09_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 618 (`tools/out/curva_f3b/curva_09_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 618 (`tools/out/curva_f3b/curva_09_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 628 (`tools/out/curva_f3b/curva_09_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 628 (`tools/out/curva_f3b/curva_09_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 628 (`tools/out/curva_f3b/curva_09_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 630 (`tools/out/curva_f3b/curva_09_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 630 (`tools/out/curva_f3b/curva_09_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 630 (`tools/out/curva_f3b/curva_09_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 632 (`tools/out/curva_f3b/curva_09_v1.json`): 2 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 632 (`tools/out/curva_f3b/curva_09_v1.json`): relief plus_empty: 2 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 632 (`tools/out/curva_f3b/curva_09_v1.json`): relief minus_color: 2 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 635 (`tools/out/curva_f3b/curva_09_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 635 (`tools/out/curva_f3b/curva_09_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 635 (`tools/out/curva_f3b/curva_09_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-BOT-ATENTO` nivel 640 (`tools/out/curva_f3b/curva_09_v1.json`): bot atento 93 % en el hito 640, por debajo del hito 620 (98 %): el pico no crece (para la curacion)
- aviso `QA-CVD-ADJ` nivel 640 (`tools/out/curva_f3b/curva_09_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 640 (`tools/out/curva_f3b/curva_09_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 641 (`tools/out/curva_f3b/curva_09_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 641 (`tools/out/curva_f3b/curva_09_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 641 (`tools/out/curva_f3b/curva_09_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 642 (`tools/out/curva_f3b/curva_09_v1.json`): 2 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 642 (`tools/out/curva_f3b/curva_09_v1.json`): relief minus_color: 2 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 642 (`tools/out/curva_f3b/curva_09_v1.json`): relief plus_empty: 2 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 649 (`tools/out/curva_f3b/curva_09_v1.json`): 2 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 649 (`tools/out/curva_f3b/curva_09_v1.json`): relief plus_empty: 2 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 649 (`tools/out/curva_f3b/curva_09_v1.json`): relief minus_color: 2 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 651 (`tools/out/curva_f3b/curva_09_v1.json`): 2 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 651 (`tools/out/curva_f3b/curva_09_v1.json`): relief plus_empty: 2 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 651 (`tools/out/curva_f3b/curva_09_v1.json`): relief minus_color: 2 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 652 (`tools/out/curva_f3b/curva_09_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 652 (`tools/out/curva_f3b/curva_09_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 652 (`tools/out/curva_f3b/curva_09_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 655 (`tools/out/curva_f3b/curva_09_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 655 (`tools/out/curva_f3b/curva_09_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 655 (`tools/out/curva_f3b/curva_09_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 660 (`tools/out/curva_f3b/curva_09_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 660 (`tools/out/curva_f3b/curva_09_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 660 (`tools/out/curva_f3b/curva_09_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 661 (`tools/out/curva_f3b/curva_09_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 661 (`tools/out/curva_f3b/curva_09_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 661 (`tools/out/curva_f3b/curva_09_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 672 (`tools/out/curva_f3b/curva_09_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 672 (`tools/out/curva_f3b/curva_09_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-WINDOW` nivel 672 (`tools/out/curva_f3b/curva_09_v1.json`): bot_fail 0.700 fuera de la ventana [0.75, 0.90) (reto)
- aviso `QA-CVD-ADJ` nivel 675 (`tools/out/curva_f3b/curva_09_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 676 (`tools/out/curva_f3b/curva_09_v1.json`): 4 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 676 (`tools/out/curva_f3b/curva_09_v1.json`): relief plus_empty: 4 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 676 (`tools/out/curva_f3b/curva_09_v1.json`): relief minus_color: 4 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 680 (`tools/out/curva_f3b/curva_09_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 680 (`tools/out/curva_f3b/curva_09_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-WINDOW` nivel 686 (`tools/out/curva_f3b/curva_09_v1.json`): bot_fail 0.725 fuera de la ventana [0.75, 0.90) (reto)
- aviso `QA-CVD-ADJ` nivel 690 (`tools/out/curva_f3b/curva_09_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 690 (`tools/out/curva_f3b/curva_09_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 690 (`tools/out/curva_f3b/curva_09_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 697 (`tools/out/curva_f3b/curva_09_v1.json`): 2 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 697 (`tools/out/curva_f3b/curva_09_v1.json`): relief plus_empty: 2 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 697 (`tools/out/curva_f3b/curva_09_v1.json`): relief minus_color: 2 contacto(s) de un par del simulador de daltonismo en una cuia
