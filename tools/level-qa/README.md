# tools/level-qa

Control de calidad de los paquetes de niveles de Amazon Fill (Tintalú): GDD 08 §2.4 y §4.1 («Validación en
`tools/level-qa`») y 03 §17. Es una consola .NET 10 que recalcula cada nivel con las mismas reglas que el cliente
(`AF.Core` y `AF.Solver`) y falla el CI si algo no cumple. No usa dependencias NuGet: lee el JSON con `System.Text.Json`.
Comparte con `tools/level-generator` el lector de la especificación de la campaña, las métricas y la forma de los
tableros (`Campaign/*.cs` y `Metrics/*.cs`, enlazados en `level-qa.csproj`, no copiados).

El esquema JSON (`schemas/af-level-1.schema.json`) no lo valida `level-qa`: lo hace `python tools/validate_schema.py
--pack` en el mismo paso del CI.

## Uso

```
python tools/validate_schema.py --pack Assets/_Project/Data/Levels/mundo_02_v1.json Assets/_Project/Data/Levels/mundo_03_v1.json Assets/_Project/Data/Levels/mundo_04_v1.json Assets/_Project/Data/Levels/mundo_05_v1.json
dotnet run --project tools/level-qa -c Release -- Assets/_Project/Data/Levels/mundo_02_v1.json Assets/_Project/Data/Levels/mundo_03_v1.json Assets/_Project/Data/Levels/mundo_04_v1.json Assets/_Project/Data/Levels/mundo_05_v1.json --report tools/out/level_qa_report.md
```

Opciones: `--report <informe.md>` (Markdown UTF-8 con cada comprobación y sus fallos) y `--spec <campaign.json>` (por
defecto `tools/level-generator/specs/campaign_v1.json`; se ejecuta desde la raíz del repositorio).

Códigos de salida: 0 = todo cumple (los avisos no fallan); 1 = algún nivel no cumple; 2 = uso incorrecto, sin paquetes
o un archivo ilegible. Sin argumentos sale con 2 (un glob vacío nunca deja pasar la compuerta). La consola es ASCII.

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
| QA-FIELDS | sin `solution`, `safe_path` ni `qa` | 08 §4.1 |
| QA-SIZE | ≤ 100 KB por 100 niveles sin `relief` | 03 §17 #11 |
| QA-EARLY-TRAPS | trampas tempranas dentro del tope: 0 en Normal, 1 en Difícil, sin tope en Muy difícil | D-007 Q16 B |
| QA-BAND | `difficulty.band` = rol de la posición en el ciclo (remanso = comfort) | 03 §12; D-009 |
| QA-LABEL-SLOT | etiquetas solo donde se permiten | D-009; c.3 regla 7 |
| QA-LABEL-ADJ | nunca dos niveles con etiqueta seguidos | c.3 regla 1 |
| QA-AFTER-LABEL | tras una etiqueta, el siguiente con D menor (condición necesaria de «bajar una banda») | c.3 regla 2 |
| QA-LADDER | escalera de D (61+ y 41-59) | c.3 regla 4 |
| QA-REMANSO | p19 puro | c.3 regla 5 |
| QA-PURE-30 | ≥ 30 % de niveles puros por ciclo completo | d.2 regla 1 |
| QA-WINDOW | `bot_fail` en la ventana del slot (< 0,20 en los estrenos) | 03 §17 #3 |
| QA-DEBUT | estrenos con 2 vacíos y un ejemplar; `intro_mechanic` y `skippable` = false solo en el nivel de intro | 02 §9.2 regla 2 |
| QA-LABEL-ACCURACY | aviso: acierto de etiqueta por ciclo < 60 % | c.7 |
| QA-RELIEF-MIN | ≥ 1 variante desde el 31 | 03 §13 |
| QA-RELIEF-VALID | variantes válidas | 03 §13 |
| QA-RELIEF-MINUS | `minus_color` resoluble con el par entero publicado | 03 §13 |
| QA-RELIEF-PLUS | `plus_empty` con exactamente un vacío más | 03 §13 |
| QA-RELIEF-BOT | `bot_fail` de la variante recalculado; baja ≥ 0,20 (base < 0,20: par menor o vacío extra) | 03 §17 #8 |
| QA-REPEAT-SHAPE | forma distinta de los 200 niveles anteriores | 03 §17 #9 |
| QA-REPEAT-HAMMING | Hamming ≥ 25 % con los 20 anteriores | 03 §17 #9 |
| QA-MECH-DEBUT | mecánicas y capacidades con debut anterior o igual al nivel | D-007; 02 §8 |
| QA-MECH-SPEC | mecánicas permitidas por la celda del calendario | especificación |
| QA-MECH-KEY | llave presente; agua del mundo en la Lluvia; llave del tutorial en el estreno | 02 §9.1; D-007 |
| QA-MECH-COUNT | ≤ 2 mecánicas por nivel antes del 300 | 02 §9.2 regla 4 |
| QA-MURKY-FLAG | `murky` coherente con `hidden` | 08 §4.1 |
| QA-MURKY-T1 | solo desde el 100 y en Difícil o Muy difícil | 02 T1 |
| QA-MURKY-LIMITS | ≤ 3 cuias y ≤ 4 dosis ocultas; T4 | 02 T2-T4 |
| QA-MURKY-SAFE | ruta segura en cada estado | 03 §8; 02 T5 |
| QA-MURKY-COMBO | sin Lluvia, Piraña, Liana ni Remolino | Q14 A |
| QA-MURKY-Q9 | estreno del 100: una cuia, 2 dosis ocultas, sin otra mecánica | Q9 |

Reproducibilidad (03 §17 #10): la comprueba `dotnet run --project tools/level-generator -c Release -- --spec … --check`.

## Fixtures negativos: `fixtures/*.json`

Cada fixture es un paquete con un defecto. El nombre empieza por el id de la comprobación que debe fallar
(`QA-PAR__par_distinto.json`). Para pasarlos todos (Git Bash):

```
for f in tools/level-qa/fixtures/QA-*.json; do
  id=$(basename "$f" | sed 's/__.*//')
  out=$(dotnet run --project tools/level-qa -c Release --no-build -- "$f"); code=$?
  echo "$(basename "$f"): exit=$code $(echo "$out" | grep -q "\[$id\]" && echo presente || echo AUSENTE)"
done
```

Todos deben dar `exit=1` y `presente`. `fixtures/schema/` son los fixtures de `validate_schema.py --pack` (uno válido y
uno inválido).
