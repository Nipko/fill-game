# Informe de `level-qa --reserve`

Generado por `dotnet run --project tools/level-qa -c Release -- --reserve Assets/_Project/Data/Levels/reserva_curva_07_v1.json Assets/_Project/Data/Levels/reserva_curva_08_v1.json Assets/_Project/Data/Levels/reserva_curva_09_v1.json --campaign Assets/_Project/Data/Levels/curva_07_v1.json Assets/_Project/Data/Levels/curva_08_v1.json Assets/_Project/Data/Levels/curva_09_v1.json --spec tools/level-generator/specs/campaign_v3.json --report tools/out/curva2/qa_reserva_curva2.md`: no se edita a mano.

- Tableros de reserva: 80; fallos: **0**; avisos: 73.

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
| `QA-RELIEF-MIN` | relief con al menos una variante desde el nivel 31 (03 sec. 13; 08 sec. 4.1) | 0 |
| `QA-RELIEF-VALID` | cada variante es un tablero valido (BoardBuilder y LevelValidation; con tinaja, su forma: una, su cuia contigua y en la misma fila, <= 13 cuias (D-027), T4 y combinaciones) | 0 |
| `QA-RELIEF-MINUS` | minus_color resoluble (A* w = 1 a 2.000.000: la segunda pasada del generador, Q-N2 (a); la primera, a 100.000) con el par entero publicado y sin el color removed, que nunca es una fruta de la tinaja (03 sec. 13; propuesta de la tinaja c.3 punto 4) | 0 |
| `QA-RELIEF-PLUS` | plus_empty = el tablero base con exactamente un vacio de 4 mas (al final; con tinaja o Aguape, en la ranura que deja la pareja o cada grupo en una fila; con la Lancha, k puede subir) (03 sec. 13; D-068) | 0 |
| `QA-RELIEF-BOT` | bot_fail de la variante recalculado; baja >= 0,20, o con base < 0,20 par menor (minus_color) o el vacio extra (plus_empty) (03 sec. 17 #8) | 0 |
| `QA-RELIEF-MASK` | variante de un nivel con tinaja: la conserva y su hidden es la mascara (todas sus capas menos la boca); variante sin tinaja: sin hidden (08 sec. 4.1) | 0 |
| `QA-RELIEF-SAFE` | variante con tinaja: ruta segura y paseos seguros y casuales (03 sec. 13; propuesta de la tinaja c.3 punto 4; opciones de Morpho [P]) | 0 |
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
| `QA-T-ROLE` | curva 2: T y K dentro del tope de su papel (role_caps; 03 sec. 12 y sec. 17 #16); en kind curve, replaces = el id de la curva 1 del mismo slot e id/seed con _c{c}/:c{c}; aviso si difficulty.pressure no es el de las palancas del tablero (D-068) | 0 |
| `QA-RES-HEADER` | pack_schema 4, kind reserve, pack_id reserva_{mm}_v1 del mundo del pack de campana (contrato 21); en la curva 2, pack_schema 5 y reserva_curva_{mm}_v1 (D-068) | 0 |
| `QA-RES-SLOT` | replaces = el id de campana (en la curva 2, el de la curva: _c2) del mismo level y mundo, presente en el pack de campana; id = replaces + _r1; seed = la del slot + :r1 | 0 |
| `QA-RES-MECH` | el sustituido usa una mecanica con kill switch (mech_enabled_*, 08 sec. 9); el de reserva no lleva ninguna mecanica ni Barro (tampoco su alivio), ni ocultas ni intro_mechanic [P] | 0 |
| `QA-RES-WINDOW` | bot_fail en la ventana del slot, etiqueta permitida en el slot y band del slot (03 sec. 12; D-009) | 0 |
| `QA-RES-COUNT` | como mucho el 20 % de los niveles del mundo; aviso si hay menos que min(tope, niveles con mecanica) | 0 |
| `QA-RES-D` | sin difficulty.D (la reserva no entra en el pool del rango, 03 sec. 7) | 0 |
| `QA-RES-REPRO` | el pack se regenera byte a byte desde el pack de campana y la especificacion (03 sec. 17 #10) | 0 |
| `QA-SCHEMA` | JSON valido contra schemas/af-level-1.schema.json con python tools/validate_schema.py --pack; sin Python o sin jsonschema, falla (08 sec. 4.1; 03 sec. 17 #11) | 0 |

## Incidencias

- aviso `QA-CVD-ADJ` nivel 305 (`Assets/_Project/Data/Levels/reserva_curva_07_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 305 (`Assets/_Project/Data/Levels/reserva_curva_07_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 305 (`Assets/_Project/Data/Levels/reserva_curva_07_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 309 (`Assets/_Project/Data/Levels/reserva_curva_07_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 309 (`Assets/_Project/Data/Levels/reserva_curva_07_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 309 (`Assets/_Project/Data/Levels/reserva_curva_07_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 312 (`Assets/_Project/Data/Levels/reserva_curva_07_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 312 (`Assets/_Project/Data/Levels/reserva_curva_07_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 312 (`Assets/_Project/Data/Levels/reserva_curva_07_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 317 (`Assets/_Project/Data/Levels/reserva_curva_07_v1.json`): 2 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 317 (`Assets/_Project/Data/Levels/reserva_curva_07_v1.json`): relief minus_color: 2 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 317 (`Assets/_Project/Data/Levels/reserva_curva_07_v1.json`): relief plus_empty: 2 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 320 (`Assets/_Project/Data/Levels/reserva_curva_07_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 320 (`Assets/_Project/Data/Levels/reserva_curva_07_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 320 (`Assets/_Project/Data/Levels/reserva_curva_07_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 326 (`Assets/_Project/Data/Levels/reserva_curva_07_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 326 (`Assets/_Project/Data/Levels/reserva_curva_07_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 326 (`Assets/_Project/Data/Levels/reserva_curva_07_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 369 (`Assets/_Project/Data/Levels/reserva_curva_07_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 369 (`Assets/_Project/Data/Levels/reserva_curva_07_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 369 (`Assets/_Project/Data/Levels/reserva_curva_07_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 426 (`Assets/_Project/Data/Levels/reserva_curva_08_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 426 (`Assets/_Project/Data/Levels/reserva_curva_08_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 432 (`Assets/_Project/Data/Levels/reserva_curva_08_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 432 (`Assets/_Project/Data/Levels/reserva_curva_08_v1.json`): relief minus_color: 2 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 432 (`Assets/_Project/Data/Levels/reserva_curva_08_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 446 (`Assets/_Project/Data/Levels/reserva_curva_08_v1.json`): 2 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 446 (`Assets/_Project/Data/Levels/reserva_curva_08_v1.json`): relief minus_color: 2 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 446 (`Assets/_Project/Data/Levels/reserva_curva_08_v1.json`): relief plus_empty: 2 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 449 (`Assets/_Project/Data/Levels/reserva_curva_08_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 449 (`Assets/_Project/Data/Levels/reserva_curva_08_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 449 (`Assets/_Project/Data/Levels/reserva_curva_08_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 455 (`Assets/_Project/Data/Levels/reserva_curva_08_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 455 (`Assets/_Project/Data/Levels/reserva_curva_08_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 455 (`Assets/_Project/Data/Levels/reserva_curva_08_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 463 (`Assets/_Project/Data/Levels/reserva_curva_08_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 463 (`Assets/_Project/Data/Levels/reserva_curva_08_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 463 (`Assets/_Project/Data/Levels/reserva_curva_08_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 466 (`Assets/_Project/Data/Levels/reserva_curva_08_v1.json`): 2 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 466 (`Assets/_Project/Data/Levels/reserva_curva_08_v1.json`): relief minus_color: 2 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 466 (`Assets/_Project/Data/Levels/reserva_curva_08_v1.json`): relief plus_empty: 2 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 483 (`Assets/_Project/Data/Levels/reserva_curva_08_v1.json`): 2 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 483 (`Assets/_Project/Data/Levels/reserva_curva_08_v1.json`): relief minus_color: 2 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 483 (`Assets/_Project/Data/Levels/reserva_curva_08_v1.json`): relief plus_empty: 2 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 514 (`Assets/_Project/Data/Levels/reserva_curva_08_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 569 (`Assets/_Project/Data/Levels/reserva_curva_09_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 569 (`Assets/_Project/Data/Levels/reserva_curva_09_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 569 (`Assets/_Project/Data/Levels/reserva_curva_09_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 571 (`Assets/_Project/Data/Levels/reserva_curva_09_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 571 (`Assets/_Project/Data/Levels/reserva_curva_09_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 571 (`Assets/_Project/Data/Levels/reserva_curva_09_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 578 (`Assets/_Project/Data/Levels/reserva_curva_09_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 578 (`Assets/_Project/Data/Levels/reserva_curva_09_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 578 (`Assets/_Project/Data/Levels/reserva_curva_09_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 580 (`Assets/_Project/Data/Levels/reserva_curva_09_v1.json`): 2 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 585 (`Assets/_Project/Data/Levels/reserva_curva_09_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 585 (`Assets/_Project/Data/Levels/reserva_curva_09_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 585 (`Assets/_Project/Data/Levels/reserva_curva_09_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 591 (`Assets/_Project/Data/Levels/reserva_curva_09_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 591 (`Assets/_Project/Data/Levels/reserva_curva_09_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 591 (`Assets/_Project/Data/Levels/reserva_curva_09_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 595 (`Assets/_Project/Data/Levels/reserva_curva_09_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 595 (`Assets/_Project/Data/Levels/reserva_curva_09_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 595 (`Assets/_Project/Data/Levels/reserva_curva_09_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 609 (`Assets/_Project/Data/Levels/reserva_curva_09_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 609 (`Assets/_Project/Data/Levels/reserva_curva_09_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 609 (`Assets/_Project/Data/Levels/reserva_curva_09_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 612 (`Assets/_Project/Data/Levels/reserva_curva_09_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 612 (`Assets/_Project/Data/Levels/reserva_curva_09_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 612 (`Assets/_Project/Data/Levels/reserva_curva_09_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 652 (`Assets/_Project/Data/Levels/reserva_curva_09_v1.json`): 1 contacto(s) de un par del simulador de daltonismo en una cuia del tablero base; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)
- aviso `QA-CVD-ADJ` nivel 652 (`Assets/_Project/Data/Levels/reserva_curva_09_v1.json`): relief minus_color: 1 contacto(s) de un par del simulador de daltonismo en una cuia
- aviso `QA-CVD-ADJ` nivel 652 (`Assets/_Project/Data/Levels/reserva_curva_09_v1.json`): relief plus_empty: 1 contacto(s) de un par del simulador de daltonismo en una cuia
