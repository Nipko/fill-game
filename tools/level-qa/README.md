# tools/level-qa

Control de calidad de los paquetes de niveles de Amazon Fill (Tintalú): GDD 08 §2.4 y §4.1 («Validación en
`tools/level-qa`») y 03 §17. Es una consola .NET 10 que recalcula cada nivel con las mismas reglas que el cliente
(`AF.Core` y `AF.Solver`) y falla el CI si algo no cumple. No usa dependencias NuGet: lee el JSON con `System.Text.Json`.
Comparte con `tools/level-generator` el lector de la especificación de la campaña, las métricas, la forma de los
tableros y la generación (`Campaign/*.cs`, `Metrics/*.cs` y `Generation/*.cs`, enlazados en `level-qa.csproj`, no
copiados).

**Es la compuerta completa en un solo comando** (08 §4.1 «Validación en `tools/level-qa`» y la tabla de 08 §2.4, que le
asignan el esquema y el determinismo por semilla): además de las comprobaciones por nivel, `level-qa`

- valida cada pack contra `schemas/af-level-1.schema.json` lanzando `python tools/validate_schema.py --pack` (QA-SCHEMA;
  la única copia del esquema, y .NET no trae un validador de JSON Schema). Sin Python 3 con `jsonschema`, falla;
- regenera la campaña en memoria con la especificación y comprueba que cada pack sale byte a byte (QA-REPRO, 03 §17 #10).
  D depende del pool de cada rango de 03 §11, así que siempre se regenera el rango completo de la especificación (11-200),
  aunque se compruebe un solo pack: tarda lo que el generador (en Release con 8 hilos en la máquina del dueño, entre 6 y
  13 minutos según la carga);
- con el pool regenerado, mide la banda de cada nivel (cuantil de D) y la compara con su rol (QA-BAND-Q, 03 §7).

Con `--no-reproduce` salta QA-REPRO y QA-BAND-Q (fixtures negativos, pruebas rápidas) y lo deja como aviso; el CI nunca lo
usa.

**Tinaja (M13; D-019, Q-Ti1 A; propuesta de la tinaja c.5; P3 de la tanda 4).** «Hay ocultas» ya no quiere decir «Aguas
turbias»: la tinaja y su cuia son una sola mecánica (`Ti`, un ejemplar; sus capacidades 5 y 2 no son Cuia alta ni Cuia
chica) y solo cuenta `Tu` con ocultas fuera de la tinaja. Sus controles (QA-TINAJA-*, abajo) repiten lo que el esquema no
puede expresar y los filtros del generador con el mismo código y las mismas semillas (`TinajaWalks.Check`: ruta segura, 20
paseos seguros y 30 casuales, «S0 sin trampas» en el estreno y la compuerta de coste de Morpho con el tope y el factor de P1;
en las variantes de alivio, la ruta y los paseos con `TinajaWalks.ReliefOptions` [P], sin la compuerta). Todas las búsquedas
van con las opciones del nivel: el par y las trampas con `SolverOptions.ForLevel` y la ruta segura de Aguas turbias con
`SafeMoves.ForPipeline(modo, nivel)`, sin poda de fútiles en los niveles con tinaja. Los packs sin tinaja dan el mismo
resultado que antes.

## Uso

```
dotnet run --project tools/level-qa -c Release -- Assets/_Project/Data/Levels/mundo_02_v1.json Assets/_Project/Data/Levels/mundo_03_v1.json Assets/_Project/Data/Levels/mundo_04_v1.json Assets/_Project/Data/Levels/mundo_05_v1.json --report tools/out/level_qa_report.md
```

Pack de muestra de la tinaja (P1; `pack_schema` 3, niveles 321-340), con su especificación:

```
dotnet run --project tools/level-qa -c Release -- tools/out/sample_tinaja/mundo_07_v1.json --spec tools/level-generator/specs/sample_tinaja.json
```

Opciones: `--report <informe.md>` (Markdown UTF-8 con cada comprobación y sus fallos), `--spec <campaign.json>` (por
defecto `tools/level-generator/specs/campaign_v1.json`; se ejecuta desde la raíz del repositorio), `--levels a-b` (rango
que se regenera; por defecto el de la especificación, el mismo que el `--levels` por defecto del generador) y
`--no-reproduce`.

Códigos de salida: 0 = todo cumple (los avisos no fallan); 1 = algún nivel no cumple, el esquema falla, el pack no se
reproduce o falta Python; 2 = uso incorrecto, sin paquetes o un archivo ilegible. Sin argumentos sale con 2 (un glob vacío
nunca deja pasar la compuerta). La consola es ASCII.

## Comprobaciones

Cada comprobación tiene un id estable; la consola imprime `FAIL [ID] nivel N: motivo`.

| Id | Qué comprueba | Fuente |
|---|---|---|
| QA-STRUCT | caps, cuias y attrs de la misma longitud; ≤ 14 cuias generadas; niveles sin repetir | 02 R1; 08 §4.1 |
| QA-UNITS | U_c coherente con `units_per_color` y con las capacidades | 02 R3, §8; 03 §17 #5 |
| QA-SEALED | ninguna cuia sellada al inicio | 03 §17 #5 |
| QA-STUCK | `atascado(S0)` = false | 02 §9.2 regla 1 |
| QA-VALID | el resto de `LevelValidation` (mecánicas, Barro, Aguas turbias) | 03 §17 #5-#7 |
| QA-PAR | par recalculado (A* w = 1, `run`) igual al publicado; par ≥ 2K; par objetivo del rango ±30 % | 03 §17 #1-#2 |
| QA-TINT-DEBUT | cada tinte con debut anterior o igual al nivel | 03 §17 #4 |
| QA-KMAX | K entre `k_min` y `k_max` de la cabecera | 03 §17 #4 |
| QA-RISK | `risk_pairs` recalculado y ≤ `max_risk_pairs` | 03 §4.5 |
| QA-PALETTE | `palette` = tintes presentes en `cuias` | 08 §4.1 |
| QA-BOT | `bot_fail` recalculado con la semilla del nivel, idéntico | 03 §6 |
| QA-LABEL | etiqueta coherente (≥ 0,75 hard y ≥ 0,90 very_hard, desde el 40 y el 60) | 03 §7, §11 |
| QA-LABEL-EARLY | ningún nivel mide ≥ 0,90 antes del 60 ni ≥ 0,75 antes del 40 (aunque se publique con la etiqueta inferior) | contrato 6; 03 §11-§12 |
| QA-CUIAS | aviso: cuias del nivel fuera de las cuias totales del rango | 03 §11 |
| QA-FIELDS | sin `solution`, `safe_path` ni `qa` | 08 §4.1 |
| QA-SIZE | ≤ 100 KB por 100 niveles sin `relief` | 03 §17 #11 |
| QA-EARLY-TRAPS | trampas tempranas dentro del tope: 0 en Normal, 1 en Difícil, sin tope en Muy difícil | D-007 Q16 B |
| QA-BAND | `difficulty.band` = rol de la posición en el ciclo (remanso = comfort) | 03 §12; D-009 |
| QA-LABEL-SLOT | etiquetas solo donde se permiten | D-009; c.3 regla 7 |
| QA-LABEL-ADJ | nunca dos niveles con etiqueta seguidos | c.3 regla 1 |
| QA-AFTER-LABEL | tras una etiqueta, el siguiente con D menor (condición necesaria de «bajar una banda») | c.3 regla 2 |
| QA-LADDER | escalera de D (61+ y 41-59); en 21-40, los retos suaves por debajo del hito 40 | c.3 regla 4; 03 §7 |
| QA-REMANSO | p19 puro y más fácil que la media de comodidad: D(p19) < media de D de las comodidades del ciclo que no son estreno | c.4; c.3 regla 5 |
| QA-RAMP | filas 11 y 12: 8 cuias = 6 colores + 2 vacíos, par 15-19 y 16-20, `bot_fail` 0-10 % | 02 §13 |
| QA-PURE-30 | ≥ 30 % de niveles puros por ciclo completo | d.2 regla 1 |
| QA-WINDOW | `bot_fail` en la ventana del slot: falla en recuperación, comodidad, estreno (< 0,20), remanso y rampa; un reto o el hito fuera de su ventana es aviso (se publica con la etiqueta que mide) | 03 §17 #3; 08 §4.1; c.2 |
| QA-DEBUT | estrenos con 2 vacíos sin contar la tinaja ni su cuia (también el de una capacidad fuera de la rampa y del hito: la Cuia chica de 3 en el 22) y un ejemplar (la tinaja con su cuia es uno); `intro_mechanic` y `skippable` = false solo en el nivel de intro | 02 §9.2 regla 2; 02 §8; propuesta de la tinaja c.5 |
| QA-LABEL-ACCURACY | aviso: acierto de etiqueta por ciclo < 60 % | c.7 |
| QA-RELIEF-MIN | ≥ 1 variante desde el 31 | 03 §13 |
| QA-RELIEF-VALID | variantes válidas; con tinaja, también su forma (una, su cuia contigua y en la misma fila, ≤ 12 cuias, T4 y combinaciones) | 03 §13; 02 §9.1 M13 |
| QA-RELIEF-MINUS | `minus_color` resoluble con el par entero publicado y sin quitar una fruta de la tinaja | 03 §13; propuesta de la tinaja c.3 punto 4 |
| QA-RELIEF-PLUS | `plus_empty` = el base con exactamente un vacío de 4 más: al final o, con tinaja, en la ranura que deja la pareja en la misma fila | 03 §13 |
| QA-RELIEF-BOT | `bot_fail` de la variante recalculado; baja ≥ 0,20 (base < 0,20: par menor o vacío extra) | 03 §17 #8 |
| QA-RELIEF-MASK | la variante de un nivel con tinaja la conserva y su `hidden` es la máscara (todas sus capas menos la boca); una variante sin tinaja no lleva `hidden` | 08 §4.1 (decisión del 2026-10-14, opción a de P2a) |
| QA-RELIEF-SAFE | variante con tinaja: ruta segura y paseos seguros y casuales (opciones de Morpho [P], pendiente de decisión; semilla del nivel + `/` + tipo) | 03 §13; propuesta de la tinaja c.3 punto 4 |
| QA-REPEAT-SHAPE | forma distinta de los 200 niveles anteriores | 03 §17 #9 |
| QA-REPEAT-HAMMING | Hamming ≥ 25 % con los 20 anteriores | 03 §17 #9 |
| QA-MECH-DEBUT | mecánicas y capacidades con debut anterior o igual al nivel | D-007; 02 §8 |
| QA-MECH-SPEC | mecánicas permitidas por la celda del calendario | especificación |
| QA-MECH-CELL | cada mecánica y capacidad de la celda está en el nivel; sin turbias por el respaldo de T1, aviso | especificación; 02 §8 |
| QA-GAP-120 | ninguna mecánica ni capacidad (Cuia chica, Cuia alta) pasa más de 120 niveles sin volver, sobre lo publicado; solo cuenta un hueco con todos sus niveles en los paquetes | resumen 6; d.2 regla 4 |
| QA-MECH-KEY | llave presente; agua del mundo en la Lluvia; llave del tutorial en el estreno | 02 §9.1; D-007 |
| QA-MECH-COUNT | ≤ 2 mecánicas por nivel antes del 300 (la tinaja con su cuia cuenta como una) | 02 §9.2 regla 4 |
| QA-MURKY-FLAG | `murky` coherente con `hidden` (Aguas turbias o tinaja) | 08 §4.1 |
| QA-MURKY-T1 | Aguas turbias solo desde el 100 y en Difícil o Muy difícil; la tinaja queda fuera (excepción de D-019) | 02 T1; D-019, Q-Ti1 A |
| QA-MURKY-LIMITS | ≤ 3 cuias y ≤ 4 dosis ocultas; T4 | 02 T2-T4 |
| QA-MURKY-SAFE | ruta segura en cada estado | 03 §8; 02 T5 |
| QA-MURKY-COMBO | sin Lluvia, Piraña, Liana ni Remolino | Q14 A |
| QA-MURKY-Q9 | estreno del 100: una cuia, 2 dosis ocultas, sin otra mecánica | Q9 |
| QA-TINAJA-TIER | dosis de la tinaja por tipo de nivel: estreno, comodidad y práctica 3 (2 ocultas); retos 4-5 (3-4); hito 5 (4) (`Superposition.TinajaDoses`) | D-019, Q-Ti1 A; 02 §9.1 M13 |
| QA-TINAJA-ONE | una sola tinaja y una sola cuia suya | 02 §9.1 M13 regla 1 |
| QA-TINAJA-PAIR | su cuia en la ranura contigua y en la misma fila con cualquiera de las filas del cliente (`Superposition.SameRow`) | 02 §9.1 M13 regla 1; D-006 |
| QA-TINAJA-CUIAS | como mucho 12 cuias con la tinaja, contando la pareja | 02 §5 [P] |
| QA-TINAJA-MASK | `hidden` = todas las capas de la tinaja menos la boca, y nada oculto fuera de ella (sin Aguas turbias) | 02 §9.1 M13 regla 1; propuesta de la tinaja c.4 |
| QA-TINAJA-T4 | T4 en toda la columna de la tinaja | 02 §9.1 M13 regla 1 |
| QA-TINAJA-COMBO | sin Liana, Remolino, Lluvia ni Piraña, y sin Barro dentro de la tinaja o de su cuia (fuera de la pareja, sí) | 02 §9.1 M13, combinaciones |
| QA-TINAJA-SLOT | ninguna tinaja en p1, p2, p19 ni en la rampa | D-019, Q-Ti1 A |
| QA-TINAJA-DEBUT | tablero de estreno con tinaja: sola y un ejemplar, 2 vacíos sin contar la pareja, `bot_fail` < 0,20 y S0 sin trampas (todo vertido útil de S0 es seguro) | 02 §9.2 regla 2; propuesta de la tinaja c.3 y d.3 |
| QA-TINAJA-SAFE | ruta segura, 20 paseos seguros y 30 casuales [P] (`TinajaWalks.Check`, la misma semilla que el generador); el motivo separa «sin vertido seguro» de «presupuesto agotado» (también en los paseos casuales desde FIXG: un `SafeMoves` sin presupuesto con la realidad verdadera resoluble es «presupuesto agotado», no «adivinar») | 03 §8; D-019, Q-Ti1 A |
| QA-TINAJA-MORPHO | coste de Morpho (w = 2, 40.000 nodos por realidad) en S0, la ruta y los paseos: tope de nodos = 150 ms × nodos/ms del PC ÷ factor del teléfono (8× [P] hasta PF1), el mismo que P1 | propuesta de la tinaja c.3 punto 3; 08 §9 |
| QA-SCHEMA | JSON válido contra `schemas/af-level-1.schema.json` (`python tools/validate_schema.py --pack`); sin Python o sin `jsonschema`, falla | 08 §4.1; 03 §17 #11 |
| QA-REPRO | la especificación y las semillas reproducen cada pack byte a byte (campaña regenerada en memoria) | 03 §17 #10; 08 §2.4 y §4.1 |
| QA-BAND-Q | banda medida (cuantil de D en el pool regenerado) acorde al rol: comodidad, estreno, remanso y rampa ≤ 0,70; recuperación ≤ 0,10 o, como mínimo, D por debajo de la comodidad más fácil del ciclo; hito > 0,95 o, como mínimo, par − 2K ≥ la media del pool del rango. Cumplir solo el mínimo es aviso. Un pack que no se reproduce se mide con su D publicada (aproximado) | 03 §7; c.3 reglas 3 y 5 |

Reproducibilidad (03 §17 #10): la comprueba `level-qa` (QA-REPRO). `dotnet run --project tools/level-generator -c Release
-- --spec … --check` hace la misma comparación contra los packs del disco, sin las demás comprobaciones.

## Fixtures negativos: `fixtures/*.json`

Cada fixture es un paquete con un defecto. El nombre empieza por el id de la comprobación que debe fallar
(`QA-PAR__par_distinto.json`). Para pasarlos todos (Git Bash):

```
for f in tools/level-qa/fixtures/QA-*.json; do
  id=$(basename "$f" | sed 's/__.*//')
  case "$f" in *__tinaja_*) spec="--spec tools/level-generator/specs/sample_tinaja.json";; *) spec="";; esac
  out=$(dotnet run --project tools/level-qa -c Release --no-build -- "$f" --no-reproduce $spec); code=$?
  echo "$(basename "$f"): exit=$code $(echo "$out" | grep -q "\[$id\]" && echo presente || echo AUSENTE)"
done
```

Todos deben dar `exit=1` y `presente`. Los de QA-RAMP (11), QA-LABEL-EARLY (58), QA-MECH-CELL (160) y QA-REMANSO
(108, 110 y 119, `remanso_mas_dificil_que_la_comodidad`) son niveles de los packs anteriores a la revisión del
2026-10-09. QA-WINDOW tiene dos: un estreno con `bot_fail` 0,25 y una comodidad (p8 del 48) con el tablero de recuperación
del 42 de esos packs, que el bot no falla nunca (`comodidad_bajo_la_ventana`). QA-GAP-120 no tiene fixture (necesita más de
120 niveles seguidos): se comprueba con esos packs anteriores, en los que falla («Cuia alta no vuelve desde el 78»).
QA-REPRO y QA-BAND-Q tampoco (necesitan la campaña completa): con los packs anteriores a esta revisión de la colocación
(2026-10-09) y el generador actual, `level-qa` sale con 1 y 20 fallos: los cuatro packs no se reproducen (QA-REPRO);
QA-BAND-Q en las recuperaciones 21, 22, 61, 81, 122, 141 y 161, las comodidades 24, 30, 67 y 168 y los hitos 100 y 180
(q 0,85 con par − 2K = 0); QA-LADDER en el reto suave 26 (D 1,26 por encima del hito 40), y QA-DEBUT y QA-WINDOW en el 22
(un vacío libre y `bot_fail` 0,275 en el estreno de la Cuia chica de 3). `fixtures/schema/` son los fixtures de
`validate_schema.py --pack` (uno válido y uno inválido); cualquier fixture con un JSON fuera del esquema daría además
QA-SCHEMA.

Los de la tinaja (`QA-*__tinaja_*.json`) son niveles 321-340 y se pasan con la
especificación de la muestra de P1 (`specs/sample_tinaja.json`, la única con la tinaja en el calendario hasta P4). Salen del
pack de muestra con un defecto (QA-TINAJA-ONE, PAIR, CUIAS, MASK, T4, COMBO, SLOT en p1 y en p19, TIER, QA-RELIEF-MASK y
QA-RELIEF-MINUS) o de tableros que rechazan los filtros del generador: QA-TINAJA-DEBUT
(`tinaja_estreno_s0_sin_seguridad_demostrada`; antes de FIXG, `tinaja_estreno_con_trampa_en_s0`) es un estreno con 10 vertidos
útiles de S0 que no son seguros, ninguno demostrado inseguro: tras cada uno, A* no deja ninguna realidad de S0 demostrada sin
solución con los 20.000 nodos por realidad, así que el motivo dice «0 demostrado(s) inseguro(s) (trampa en S0); 10 sin
seguridad demostrada (presupuesto agotado)», y QA-TINAJA-SAFE no repite ese rechazo de S0 (el generador lo clasifica como
«presupuesto agotado»; lo fija `--self-test tinaja`). La trampa demostrada no tiene fixture: la fija el tablero perdido de b.2
en `--self-test tinaja`; QA-TINAJA-SAFE (`tinaja_presupuesto_agotado`), un candidato del reto 338 de la muestra que el
generador rechazó (el «presupuesto agotado: 1» del 338 en el informe de la muestra; antes de FIXG, «adivinar: 1»): el paseo casual 2 llega tras 1 vertido a un estado en el que
`SafeMoves` agota sus 20.000 nodos por realidad (4 realidades) y la realidad verdadera es resoluble. Es «presupuesto
agotado», no «adivinar»: desde FIXG el motivo acaba en `[presupuesto agotado]` (antes, en `[adivinar]`), con el mismo id.
El informe de la muestra no cuenta ningún otro candidato rechazado por «adivinar», así que el «adivinar» de verdad
(`SafeMovesOutcome.SomeRealitySolvable` con la realidad verdadera resoluble) no tiene fixture: lo fija el test de
clasificación de FIXG; QA-TINAJA-MORPHO, un hito de 12 cuias con 5 dosis en el que Morpho expande 66.496 nodos en S0
(tope 18.750), y QA-RELIEF-SAFE, una variante `minus_color` válida para AF.Core y sin Aguas estancadas al empezar en la que
ninguna realidad tiene solución (con las opciones de alivio, los tableros de prueba que se ganan en la realidad verdadera
siempre tuvieron ruta segura; por eso da también QA-RELIEF-MINUS).
