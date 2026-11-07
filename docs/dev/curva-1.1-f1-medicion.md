# Curva de la 1.1: medición F1 (topes de cuias por papel con las mecánicas de la 1.0)

- **Fecha:** 2026-11-07. **Autor:** level-pipeline-dev (agente). **Rama:** `v11/curva-f1-medicion`.
- **Encargo:** fase F1 de D-068 (`docs/dev/decisiones.md`; `docs/dev/propuesta-curva-1.1.md` §7): medir, antes de escribir
  código de reglas, si los topes de cuias por papel de §4.1 (P2 A) colocan con las mecánicas de la 1.0, sin Aguapé ni Lancha.
- **Alcance:** mundo 7 completo (301-420), variante publicada «b». El encargo pedía 301-400; se mide hasta el 420 porque la
  cúspide del mundo 7 (P3 A) es el 420 y el índice D y la colocación se calculan por mundo, igual que el pack publicado.
- **Estado:** medida. No cambia el GDD, los packs de `Assets/_Project/Data/` ni las especificaciones de producción.
- **Marcas:** [V] medido aquí; [E] estimado; [P] propuesta, lo decide el dueño.

---

## 1. Resumen para el dueño

1. **Sí colocan.** Con los topes de D-068 los 6 ciclos del mundo 7 tienen colocación, los 6 hitos (cúspide incluida) miden
   Muy difícil y 26 de los 30 retos miden Difícil, frente a 29 de 30 en la curva publicada [V]. No hace falta el Aguapé ni la
   Lancha para que el ciclo funcione; sí harán falta para que los picos **crezcan** (punto 5).
2. **Las cuias bajan de verdad.** Mediana de T de 13 a 10 y de K de 11 a 8; los niveles con 11 cuias o menos pasan de 0 a
   113 de 120 (con retos en K 9) [V].
3. **Retos: mejor K 9 que K 10.** Con K 10 el reto sale con 12 cuias (la cuia a medias de su mecánica suma una): justo el
   «12 como mucho». Con K 9 sale con 10-11, dentro del tope, y coloca igual (26 de 30 en los dos casos) [V].
4. **Lo que falla son 4 retos, casi todos cerca del 75 %.** Con K 9: 329 (Ma, 68 %), 346 (B2+Ln, 68 %), 349 (B2, 73 %) y 369
   (Pi+B2, 43 %) [V]; con K 10 fallan otros cuatro (355, 358, 369 y 412). El 369 falla también en la curva publicada [V]; por
   eso lo atribuimos a su celda y no a los topes [E].
5. **Los hitos siguen saturados.** Todos miden 90-100 % con el bot casual, como hoy; con esta medida no se puede ver si un
   hito es más duro que el anterior. Antes de fijar el `pressure_plan` hace falta el bot atento de §6.4 [V].
6. **Ocultas en Normal:** las 14 comodidades con 1-2 dosis ocultas quedan en su ventana (20-58 %). Con las 4 de los retos son
   18 de 120 niveles (15 %); para llegar al ~20 % falta una comodidad turbia más por ciclo [V].
7. **Ajuste propuesto [P]:** reto en K 9; K 10 solo en los retos de p6 y p9 que no llegan con K 9 (329, 346 y 349); estreno del Brote de dos
   hojas en K 7; recalendarizar el 369; 3 comodidades turbias por ciclo. Detalle en la sección 5.

---

## 2. Qué se generó

### 2.1 Especificaciones experimentales (`tools/level-generator/specs/experimental/`)

No son de producción y no generan packs. Extienden `campaign_v2.json` (`"extends"`) y solo cambian esto:

| Cambio | `campaign_v3_f1.json` | `campaign_v3_f1_k9.json` |
|---|---|---|
| `k_plan` 301-419 | recuperación 8, comodidad 8, reto **10**, hito 11 | igual, reto **9** |
| `k_plan` 420 (cúspide) | hito 12 | hito 12 |
| Remanso (p19) | K de recuperación (8), con su vacío | igual |
| Turbias en comodidad (`murky_normal`, D-068 P7 A) | desde el 301, 1 cuia y 1-2 dosis, hasta 5 niveles turbios por ciclo | igual |
| Celdas | 304, 311, 314, 328, 331, 337, 350, 357, 364, 377, 384, 388, 410 y 414 con turbias | igual |

- Los vacíos, las ventanas, la escalera de D-009, las celdas de mecánicas y las semillas son los de la 1.0.
- T sale de K más los vacíos del papel y las cuias que añade la mecánica; el generador no tiene un «tope de T» propio.
- Las celdas turbias se eligieron para no romper `--validate-spec`: ≥ 30 % de puros y ≥ 3 comodidades puras en p7-p17 en las
  dos variantes; la Hoja atada y el Brote de dos hojas no van con turbias (d.5.1); ninguna es celda de la Tinaja.
- El hito 340 no lleva turbias: su celda tiene la Tinaja en la variante «a» y cambiarla rompe TITULAR-8.

### 2.2 Cambios mínimos en el generador

- **`--measure`** (`Generation/MeasureRunner.cs`): las fases 1-3 de `--spec` (candidatos, índice D y colocación) con el mismo
  código (`CampaignRunner.Generate`), sin alivio ni packs. Un ciclo sin colocación se anota y la medida sigue (`keepGoing`).
- **`murky_normal`** (clave opcional de la especificación): MURKY-SLOT admite turbias en comodidades sin estrella desde
  `from_level`, MURKY-2 sube a `max_per_cycle` y el paso «Tu» pone una sola cuia turbia en comodidad. Sin la clave nada cambia.
- `t_plan` y `k_plan` **por posición** no hicieron falta: el `k_plan` por papel basta para estos topes (el remanso usa la clave
  `recovery` y la cúspide, una fila de un solo nivel, como el 620 y el 680 de D-027).
- Los packs publicados no cambian: `--check` de 11-200 y de los mundos 6-9 sale idéntico (sección 6).

---

## 3. Resultados

Informes completos, nivel a nivel: `tools/out/curva_f1/medida_v2_b_301-420.md` (curva publicada),
`medida_f1_301-420.md` (reto K 10) y `medida_f1_k9_301-420.md` (reto K 9).

La medida de la curva publicada reproduce el pack `mundo_07_v1.json`: los 120 niveles tienen el mismo T, K, dosis ocultas, par
y bot ×40 [V].

### 3.1 Resumen

| Medida | Publicada (v2 b) | F1, reto K 10 | F1, reto K 9 |
|---|---|---|---|
| Ciclos sin colocación | 0 de 6 | 0 de 6 | 0 de 6 |
| Ciclos que necesitan el último recurso (sin exigir la bajada de banda tras una etiqueta) | 1 (401-420) | 0 | 1 (401-420) |
| Retos en su ventana (75-89 %) | 29 de 30 | 26 de 30 | 26 de 30 |
| Retos con algún candidato en su ventana | 29 de 30 | 30 de 30 | 29 de 30 |
| Hitos en su ventana (≥ 90 %) | 6 de 6 | 6 de 6 | 6 de 6 |
| T media / mediana | 12,9 / 13 | 10,6 / 10 | 10,3 / 10 |
| K media / mediana | 10,9 / 11 | 8,7 / 8 | 8,4 / 8 |
| Niveles con T ≤ 11 / ≥ 12 / ≥ 13 | 0 / 120 / 98 | 86 / 34 / 8 | 113 / 7 / 6 |
| Niveles con ocultas (dosis) | 4 (7) | 18 (31) | 18 (32) |
| Fuera del tope de T de su papel | 111 | 5 | 3 |
| Tiempo de la medida (escritorio x64, 24 hilos) | 3 min 56 s | 3 min 49 s | 1 min 39 s |

### 3.2 T, K y bot por papel

Bot = mediana del fallo del bot casual ×40 del tablero elegido. Topes de T: D-068 (P2 A).

| Papel | Tope T | Publicada: T (mediana) · K · bot | F1 K 10: T · K · bot | F1 K 9: T · K · bot | F1 K 9: en ventana · fuera del tope |
|---|---|---|---|---|---|
| Recuperación (p1-p2) | 8-10 | 12 · 10 · 9 % | 10 · 8 · 3 % | 10 · 8 · 3 % | 12 de 12 · 0 |
| Estreno (p3-p5 de debut) | 9-10 | 13,5 · 11 · 5 % | 10,5 · 8 · 1 % | 10,5 · 8 · 1 % | 6 de 6 · 3 |
| Comodidad | 9-11 | 13 · 11 · 45 % | 10 · 8 · 33 % | 10 · 8 · 35 % | 60 de 60 · 0 |
| Reto | 10-11 (12) | 13 · 11 · 83 % | 12 · 10 · 81 % | 11 · 9 · 80 % | 26 de 30 · 0 |
| Remanso (p19) | 8-10 | 12,5 · 10,5 · 35 % | 10 · 8 · 28 % | 10 · 8 · 28 % | 6 de 6 · 0 |
| Hito | 12-13 | 13 · 11 · 100 % | 13 · 11 · 98 % | 13 · 11 · 98 % | 5 de 5 · 0 |
| Cúspide (420) | 13-14 | 13 · 11 · 95 % | 14 · 12 · 100 % | 14 · 12 · 100 % | 1 de 1 · 0 |

Recuento de T:

| Papel | Publicada | F1 K 10 | F1 K 9 |
|---|---|---|---|
| Comodidad | 12: 2 · 13: 58 | 9: 12 · 10: 48 | 9: 12 · 10: 48 |
| Reto | 12: 3 · 13: 24 · 14: 3 | 11: 2 · 12: 26 · 13: 2 | 10: 5 · 11: 24 · 12: 1 |
| Hito y cúspide | 13: 6 | 13: 5 · 14: 1 | 13: 5 · 14: 1 |

Notas:
- **Remanso:** con 1 vacío no entra en su ventana (20-40 %) y el generador lo relaja a 2 vacíos, como hoy: sale con 10 cuias,
  K 8 y 2 vacíos, lo que pide §4.1.
- **Estreno:** el del Brote de dos hojas (343-345) sale con 11 cuias (K 8, 2 vacíos y la cuia a medias del brote) y el bot no
  falla nunca (0 %); los de la Flor (403-405), con 10. Son los 3 «fuera del tope».
- **Comodidades turbias:** las 14 entran en su ventana, con el bot entre el 20 % y el 58 %. Ninguna se publicó sin turbias.
- **Comodidad y recuperación tienen el mismo T y K en K 10 y en K 9** (mismas semillas y mismo K, y los mismos candidatos),
  pero la colocación depende de los retos del ciclo: 7 de los 84 niveles que no son reto (307, 319, 327, 359, 373, 394 y 411)
  eligen otro tablero, y la mediana del bot en comodidad pasa del 33 % al 35 %.

### 3.3 Retos por ciclo

| Ciclo | Publicada: en ventana · T | F1 K 10: en ventana · T | F1 K 9: en ventana · T |
|---|---|---|---|
| 301-320 | 5 de 5 · 13 | 5 de 5 · 11-12 | 5 de 5 · 10-11 |
| 321-340 | 5 de 5 · 13-14 | 5 de 5 · 12 | 4 de 5 · 11 |
| 341-360 | 5 de 5 · 12-13 | 3 de 5 · 12 | 3 de 5 · 10-11 |
| 361-380 | 4 de 5 · 13-14 | 4 de 5 · 12-13 | 4 de 5 · 11-12 |
| 381-400 | 5 de 5 · 13 | 5 de 5 · 12 | 5 de 5 · 11 |
| 401-420 | 5 de 5 · 12-13 | 4 de 5 · 12 | 5 de 5 · 10-11 |

Retos que no miden Difícil (se publicarían sin etiqueta, c.2):

| Reto | Celda | Publicada | F1 K 10 | F1 K 9 | Lectura |
|---|---|---|---|---|---|
| 329 | Ma (D) | 85 % | 75 % | **68 %** | Una sola mecánica, filtro |
| 346 | B2+Ln (D) | 78 % | 85 % | **68 %** | p6, recién estrenado el B2 |
| 349 | B2 (D) | 78 % | 88 % | **73 %** | Una sola mecánica |
| 355 | Re (D) | 88 % | **45 %** | 88 % | Con K 10 el elegido tiene 2 cuias vacías libres |
| 358 | Ln+Re (D) | 88 % | **58 %** | 78 % | Ídem (2 libres en K 10 y en K 9) |
| 369 | Pi+B2 (D) | **45 %** | **35 %** | **43 %** | Falla en los tres: su celda (ya lo avisaba QA-WINDOW en la 1.0) |
| 412 | Fl+Ba (D) | 85 % | **65 %** | 85 % | — |

### 3.4 Hitos

| Hito | Celda | Publicada: T · K · bot · par | F1 K 9: T · K · bot · par |
|---|---|---|---|
| 320 | Ha+Ll+Br (MD) | 13 · 11 · 100 % · 25 | 13 · 11 · 93 % · 27 |
| 340 | Br+Ba (MD) | 13 · 11 · 100 % · 24 | 13 · 11 · 100 % · 26 |
| 360 | B2+Ln (MD) | 13 · 11 · 90 % · 29 | 13 · 11 · 90 % · 29 |
| 380 | Pi+Ho (MD) | 13 · 11 · 100 % · 33 | 13 · 11 · 100 % · 33 |
| 400 | Li+Ha+Ba (MD) | 13 · 11 · 98 % · 34 | 13 · 11 · 98 % · 34 |
| 420 (cúspide) | Fl+B2 (MD) | 13 · 11 · 95 % · 24 | 14 · 12 · 100 % · 29 |

- El hito ya tenía 13 cuias en la 1.0 (K 11 + 1 vacío + la cuia de su mecánica): los topes de hito (12-13) no lo cambian.
- La cúspide sube a 14 cuias con K 12 y coloca.
- Del 320 al 420 el bot va entre 90 % y 100 % sin orden: no mide si los picos crecen (propuesta §2.3 punto 3).

### 3.5 Frente a la curva actual (propuesta §2)

| | Publicada 301-700 (propuesta §2.1-§2.2) | Publicada 301-420 (aquí) | F1 K 9, 301-420 |
|---|---|---|---|
| T mediana | 13 | 13 | 10 |
| T ≥ 13 | 278 de 400 (70 %) | 98 de 120 (82 %) | 6 de 120 (5 %) |
| Reto: T · K · bot | 12 · 10 · 78-85 % | 13 · 11 · 83 % | 11 · 9 · 80 % |
| Comodidad: T · K · bot | 13 · 11 · 34-51 % | 13 · 11 · 45 % | 10 · 8 · 35 % |
| Respiros: T · K | 12-13 · 10-11 | 12-12,5 · 10-10,5 | 10 · 8 |
| Hito: T · bot | 13-14 · 100 % | 13 · 90-100 % | 13 · 90-100 % |
| Niveles con ocultas | 17 de 400 (4 %) | 4 de 120 (3 %) | 18 de 120 (15 %) |

Coincide con la evidencia de §4.1 de la propuesta: los retos de 101-200 (T 11, K 9-10) daban 76-84 %, y aquí los de K 9 y
T 11 dan una mediana del 80 %.

---

## 4. Conclusión

**¿Colocan los retos con K 9-10 y los hitos de 12-13 cuias sin Aguapé ni Lancha? Sí en el mundo 7 (301-420, variante b),**
con la semilla de cada slot de la 1.0. Los mundos 8 y 9 (421-700), con más retos de 3 mecánicas y los hitos de K 12-13 de la
1.0, están sin medir (§5, punto 8): la respuesta no se extiende a ellos sin medirlos.

- Ningún ciclo se queda sin colocación; la escalera de D-009 se cumple como hoy (el último recurso solo en el 401-420, que
  también lo necesita la curva publicada).
- Los retos pierden 3 etiquetas Difícil de 30 (87 % frente a 97 %), y los que fallan quedan casi todos a pocos puntos del
  75 %. Referencia: en 301-700 la 1.0 publica 13 de sus 100 retos sin la etiqueta Difícil (13 %; `tools/level-generator/README.md`, GEN79).
- Los hitos colocan todos, con 13 cuias, y la cúspide con 14.
- **Lo que esta medida no puede decir:** si los picos crecen dentro del mundo. El bot casual satura en los hitos. Para fijar
  el `pressure_plan` hay que tener antes el bot atento de §6.4 (F2, core-dev).

---

## 5. Recomendación de ajuste (§7: «se ajustan los topes antes de escribir código») [P]

1. **Retos en K 9 (10-11 cuias), no en K 10.** Con K 10 casi todos salen con 12 cuias. Con K 9 colocan igual y quedan
   dentro del tope de 10-11.
2. **K 10 solo en los retos de p6 y p9 que no llegan con K 9** (329 Ma, 349 B2 y 346 B2+Ln): son los que se quedan en 68-73 %
   con K 9 y en 75-88 % con K 10. Necesita un K por celda en `campaign_v3.json` (como el «K = 13» de los hitos de 14 cuias,
   HITO-14), que se hace en F3. Alternativa sin K más alto: una palanca más en esos retos (2 dosis turbias con el
   Matapi del 329; en los del Brote de dos hojas no, porque no va con turbias, d.5.1).
3. **Recalendarizar el reto 369 (Pi+B2):** falla en las tres medidas, también en la 1.0.
4. **Hitos: K 11 (13 cuias) y la cúspide K 12 (14 cuias),** como se midió. El `pressure_plan` de los hitos se fija cuando
   exista el bot atento; hasta entonces el escalón de presión solo puede guiar al generador, no comprobarse.
5. **Comodidad y respiros en K 8,** como se midió: 9-10 cuias y todas en su ventana. No hace falta bajar a K 7.
6. **Estreno del Brote de dos hojas en K 7:** con K 8 tiene 11 cuias (por encima del tope de 9-10) y el bot no falla nunca.
7. **Turbias en Normal: 3 comodidades por ciclo** (hoy 2-3 en la medida) para llegar al ~20 %. Ya colocan sin problema.
8. **Medir los mundos 8 y 9 (421-700) en F3,** con la spec definitiva: allí hay más retos de 3 mecánicas y los hitos de K 12-13
   de la 1.0.

---

## 6. Verificación

Comandos ejecutados en el worktree (equipo de escritorio x64, 2026-11-07), con su resultado:

- `dotnet build` de la solución con `-warnaserror`: 0 avisos, 0 errores (y `tools/level-generator` y `tools/level-qa`, igual).
- `dotnet test`: 1.009 correctas, 6 omitidas, 0 con error (1.015).
- `validate_schema.py`: 116/116 PASS.
- `--validate-spec`: 0 fallos en las dos especificaciones experimentales, en `campaign_v2.json` (24 reglas) y en
  `campaign_v1.json` (21); los fixtures de `specs/tests/` dan lo esperado, con los dos nuevos de `murky_normal`.
- `--check` de 11-200 y de los mundos 6, 7, 8 y 9 (variante b): idénticos; `--registry --check`: idéntico (94 entradas).
- `--self-test`: todo PASS (código 0).

Comandos:

```
dotnet build dotnet/AmazonFill.slnx -warnaserror
dotnet test  dotnet/AmazonFill.slnx
py -3.14 tools/validate_schema.py
dotnet run --project tools/level-generator -c Release -- --validate-spec tools/level-generator/specs/experimental/campaign_v3_f1.json
dotnet run --project tools/level-generator -c Release -- --validate-spec tools/level-generator/specs/experimental/campaign_v3_f1_k9.json
dotnet run --project tools/level-generator -c Release -- --validate-spec tools/level-generator/specs/campaign_v2.json
dotnet run --project tools/level-generator -c Release -- --validate-spec tools/level-generator/specs/campaign_v1.json
dotnet run --project tools/level-generator -c Release -- --spec tools/level-generator/specs/campaign_v1.json --levels 11-200 --out Assets/_Project/Data/Levels --check
dotnet run --project tools/level-generator -c Release -- --spec tools/level-generator/specs/campaign_v2.json --levels 201-300 --variant b --out Assets/_Project/Data/Levels --check
dotnet run --project tools/level-generator -c Release -- --spec tools/level-generator/specs/campaign_v2.json --levels 301-420 --variant b --out Assets/_Project/Data/Levels --check
dotnet run --project tools/level-generator -c Release -- --spec tools/level-generator/specs/campaign_v2.json --levels 421-560 --variant b --out Assets/_Project/Data/Levels --check
dotnet run --project tools/level-generator -c Release -- --spec tools/level-generator/specs/campaign_v2.json --levels 561-700 --variant b --out Assets/_Project/Data/Levels --check
dotnet run --project tools/level-generator -c Release -- --registry tools/level-generator/specs/campaign_v2.json --out tools/level-generator/specs/combinations_registry_v1.json --check
dotnet run --project tools/level-generator -c Release -- --self-test
```

### Para reproducir la medida

```
dotnet run --project tools/level-generator -c Release -- --measure --spec tools/level-generator/specs/campaign_v2.json --levels 301-420 --variant b --report tools/out/curva_f1/medida_v2_b_301-420.md
dotnet run --project tools/level-generator -c Release -- --measure --spec tools/level-generator/specs/experimental/campaign_v3_f1.json --levels 301-420 --variant b --report tools/out/curva_f1/medida_f1_301-420.md
dotnet run --project tools/level-generator -c Release -- --measure --spec tools/level-generator/specs/experimental/campaign_v3_f1_k9.json --levels 301-420 --variant b --report tools/out/curva_f1/medida_f1_k9_301-420.md
```

La medida es determinista: repetir la de K 9 en otra carpeta da los mismos bytes [V]. La línea «último recurso» de §3.1 sale
del log de consola de la medida («colocado sin exigir la bajada de banda tras cada etiqueta»), no del informe generado. Si un
ciclo no coloca, `--measure` sigue con el siguiente contra un historial sin ese ciclo: sus cifras serían aproximadas (no pasó
en ninguna de las tres medidas).

---

## 7. Preguntas para el dueño

| # | Pregunta | Opciones | Recomendada |
|---|---|---|---|
| F1-1 | ¿Con cuántos colores van los retos? | **A. K 9 (10-11 cuias), con K 10 solo en los retos de p6 y p9 que no llegan a Difícil con K 9** · B. K 10 en todos (12 cuias) · C. K 9 en todos | **A** |
| F1-2 | ¿Se acepta que algún reto salga sin etiqueta Difícil (hoy 1 de 30 en el mundo 7; con los topes, 3-4)? | **A. Sí, como en la 1.0 (c.2), y la curación del dueño decide** · B. No: se ajustan las celdas hasta 30 de 30 | **A** |
| F1-3 | ¿Se cambia la celda del reto 369 (Pi+B2), que no llega a Difícil en ninguna medida? | **A. Sí, en `campaign_v3.json`** · B. No | **A** |
| F1-4 | Falta el dato de Analytics de cuántos jugadores pasan del 300 (D-068, pendiente del dueño) | — | — |

---

## 8. Fase F3: los mundos 8 y 9 con los topes del dueño (2026-11-07)

- **Autor:** level-pipeline-dev (agente). **Rama:** `v11/curva-f3-pipeline`. **Especificación:** `tools/level-generator/specs/campaign_v3.json`
  (producción, curva 2), con los topes del añadido de D-068 tras F1: comodidad y respiros K 8, retos K 9 (K 10 en 329, 346 y 349),
  hitos K 11, cúspides K 12, estreno del Brote de dos hojas K 7, 3 comodidades turbias por ciclo y la celda nueva del 369.
- **Semillas:** las de la curva 2 de cada slot (`amazonfill:v1:m{mm}:v1:n{nnnn}:c2`), las que usará F5; no las de la 1.0 de la
  sección 3. Sin el Aguapé ni la Lancha (F2): sus slots se miden con la celda del calendario.
- **Aguas turbias de la curva 2 (02 §11, T1):** el estreno en Normal (323-324) con 1 dosis oculta, los retos turbios con 2-4
  dosis en 2 cuias como mucho y los hitos turbios con 3-4 en 3 cuias como mucho.
- **Informes completos:** `tools/out/curva_f3/medida_v3_421-560.md`, `medida_v3_561-700.md` y `medida_v3_301-420.md`; la prueba de
  K 10 en `medida_k10_*.md` (especificación experimental `specs/experimental/campaign_v3_f3_k10.json`).

### 8.1 Resumen [V]

| Medida | Mundo 8 (421-560) | Mundo 9 (561-700) | Mundo 7 (301-420), con las semillas `:c2` |
|---|---|---|---|
| Ciclos sin colocación | 0 de 7 | 0 de 7 | **1 de 6 (401-420)** |
| Ciclos que necesitan el último recurso | 0 | 0 | — |
| Retos en su ventana (75-89 %) | 32 de 35 | **29 de 35** | 23 de 25 colocados |
| Hitos en su ventana (≥ 90 %) | 7 de 7 (cúspide incluida) | **6 de 7** (el 660, 73 %) | 5 de 5 colocados |
| T media / mediana | 10,3 / 10 | 10,3 / 10 | 10,4 / 10 |
| K media / mediana | 8,4 / 8 | 8,4 / 8 | 8,4 / 8 |
| Niveles con T ≤ 11 / ≥ 12 / ≥ 13 | 132 / 8 / 5 | 130 / 10 / 5 | 91 / 9 / 5 (de 100 colocados) |
| Fuera del tope de T y K de su papel | 0 | 0 | 0 |
| Niveles con ocultas (dosis) | 28, el 20 % (51) | 27, el 19 % (48) | 15 de 100 (24) |
| Comodidades turbias publicadas con turbias | 21 de 21 | 21 de 21 | — |

Por papel (mundos 8 y 9): comodidad T 9-11 (mediana 10), K 8 y bot 35 %; reto T 10-12 (mediana 11), K 9 y bot 80-83 %; hito
T 12-13 y K 11, con el bot entre el 90 % y el 100 % salvo el 660 (73 %); cúspide 560 con 13 cuias (K 12, 100 %) y 700 con 14 (K 12,
93 %); recuperación y remanso con 10 cuias y K 8.

**Colocan.** Los 14 ciclos de los mundos 8 y 9 colocan con los topes del dueño, sin el último recurso. **Pierden etiquetas:** el
mundo 8 publica 3 de sus 35 retos sin Difícil y el 9, 6 retos y el hito 660 (en la curva publicada eran 4 y 8 retos; sección 3 y
`tools/level-generator/README.md`). El mundo 7 con las semillas de la curva 2 no coloca el ciclo 401-420 (8.3).

### 8.2 Retos que no miden Difícil (K 9) y la prueba con K 10

| Reto | Celda | Bot (K 9) | Bot con K 10 (prueba) | Lectura |
|---|---|---|---|---|
| 429 | Ma+Re | 73 % | **80 %** | K 10 lo arregla |
| 452 | Pi+Br | 45 % | — (K 10 no cabe en 12 cuias) | La Piraña con el Brote |
| 466 | Li+B2 | 73 % | 70 % | K 10 no ayuda |
| 566 | Ma+Re | 73 % | **75 %** | K 10 lo arregla, justo |
| 592 | Pi+B2 | 63 % | — (no cabe) | La Piraña con el Brote de dos hojas |
| 658 | Ha+Re | 28 % | 28 % | Elegido por la escalera del ciclo 641-660 (con el hito 660 en el 73 %) |
| 672 | Li+Re | 70 % | sin colocar | Con K 10 el ciclo 661-680 deja de colocar |
| 686 | Ma+Ho | 73 % | **88 %** | K 10 lo arregla |
| 698 | Ha+B2 | 73 % | **83 %** | K 10 lo arregla |

- Con K 9 miden Difícil, entre otros, el 632 (80 %), el 646 (Pi+Br+Ha, 80 %), el 649 (83 %) y el 652 (83 %); con K 10 siguen en su
  ventana (88 %, 80 %, 78 % y 80 %), así que no lo necesitan.
- Con K 10 en los retos de la prueba (429, 466, 566, 632, 649, 652, 672, 686 y 698), el mundo 8 pasa a 33 de 35 retos en su
  ventana y el 9 deja el ciclo 661-680 sin colocar (por el 672). Sin el 466 ni el 672, se espera 33 de 35 en el 8 y 31-32 de 35
  en el 9 [E].
- **La Piraña con el Brote no llega a Difícil con K 9** en dos de los tres retos (452: 45 %; 592: 63 %; el 646, con la Hoja atada,
  sí: 80 %), como el 369 de F1 (Pi+B2, 43 %). Con K 10 no caben en el tope del reto: K 10 + 1 vacío + la cueva + la cuia a medias
  del Brote = 13 cuias. Es su celda, no el tope [E].
- **El ciclo 641-660** coloca con el reto 658 en el 28 % y el hito 660 (Br+Ha+Re) en el 73 %: la escalera se cierra con dos
  slots fuera de su ventana (c.2). La curación del dueño (F5) los vería sin chip.

### 8.3 El mundo 7 con las semillas de la curva 2: 401-420 no coloca

Con las semillas `:c2`, el ciclo 401-420 no tiene colocación, ni con el último recurso. La medida de F1 (semillas de la 1.0) ya
lo colocaba solo con el último recurso (sección 3.1). Diagnóstico [V] (stderr de `--spec`): el reto 418 (Ba+Tu) tiene 1 candidato
en su ventana de 360 intentos (2 de 720 con K 10), y la escalera de D-009 no cierra: el 409 (la Flor sola) solo tiene candidatos
con D ≥ 0,96, y el 415 (Fl+Ll), ninguno por encima de 0,96. Pruebas: 418 en Br+Tu (17 candidatos en ventana) y 415 y 418 con K 10:
el ciclo sigue sin colocar. Es la escalera del ciclo de estreno de la Flor, no los topes [E]. Los demás ciclos del mundo 7 colocan; el
369 (Pi+Ma, la celda nueva) mide 65 %, y el 346 y el 349 con K 10 miden 85 % y 80 %.

### 8.4 Escalón de presión de los hitos [V]

Sin el Aguapé, en los mundos 8 y 9 solo el hito 480 llega al escalón de `pressure_plan` (5 de 5): el 440 da 3 de 4, el 500 3 de
6, el 540 4 de 7, el 560 5 de 8, el 640 3 de 7 y el 700 5 de 9 (tablas de los informes; en el mundo 7, solo el 320). Las palancas
de la 1.0 dan 3-5 escalones y el plan pide 4-9: el hueco es el que deben llenar las congeladas (el Aguapé y el espeso, 0-2
escalones) y las ocultas en los hitos. Con la escala de 03 §12, el plan de los mundos 8 y 9 no se alcanza sin F2 [E].

### 8.5 Propuestas [P]

1. **K 10 en los retos 429, 566, 686 y 698** (`k_cells`), como F1-1. No en 466 ni en 672.
2. **Cambiar la celda de los retos 452 (Pi+Br) y 592 (Pi+B2)**, como se hizo con el 369, o aceptarlos sin la etiqueta Difícil (F1-2).
3. **Revisar el ciclo 641-660** (el reto 658 y el hito 660 fuera de su ventana) en la curación de F5, o cambiar la celda del 658.
4. **Rehacer el ciclo 401-420 de la curva 2** antes de F5: la escalera del estreno de la Flor (409 Fl, 412 Fl+Ba, 415 Fl+Ll) y el
   reto turbio 418. Opciones: cambiar el 418 a Br+Tu y el 409 a un reto con otra palanca, o reordenar los retos de la Flor. Se prueba
   con `--measure` antes de tocar la especificación.
5. **Revisar `pressure_plan` de los mundos 8 y 9 cuando existan el Aguapé** y el bot atento (03 §17 #20): con las mecánicas de la
   1.0, los hitos se quedan en 3-5.

### 8.6 Para reproducir

```
dotnet run --project tools/level-generator -c Release -- --measure --spec tools/level-generator/specs/campaign_v3.json --levels 421-560 --report tools/out/curva_f3/medida_v3_421-560.md
dotnet run --project tools/level-generator -c Release -- --measure --spec tools/level-generator/specs/campaign_v3.json --levels 561-700 --report tools/out/curva_f3/medida_v3_561-700.md
dotnet run --project tools/level-generator -c Release -- --measure --spec tools/level-generator/specs/campaign_v3.json --levels 301-420 --report tools/out/curva_f3/medida_v3_301-420.md
dotnet run --project tools/level-generator -c Release -- --measure --spec tools/level-generator/specs/experimental/campaign_v3_f3_k10.json --levels 421-560 --report tools/out/curva_f3/medida_k10_421-560.md
dotnet run --project tools/level-generator -c Release -- --measure --spec tools/level-generator/specs/experimental/campaign_v3_f3_k10.json --levels 561-700 --report tools/out/curva_f3/medida_k10_561-700.md
dotnet run --project tools/level-generator -c Release -- --measure --spec tools/level-generator/specs/experimental/campaign_v3_f3_k10.json --levels 301-420 --report tools/out/curva_f3/medida_k10_301-420.md
```

Pool de prueba (no son los packs finales de F5): `tools/out/curva_f3/curva_08_v1.json` (140 niveles, `kind` = `curve`,
`pack_schema` 5) y su reserva `reserva_curva_08_v1.json` (28 tableros), con level-qa en 0 fallos (`qa_curva_08.md` y
`qa_reserva_curva_08.md`; QA-REPRO y QA-RES-REPRO reproducen los dos packs byte a byte). El estreno de las turbias en Normal se
comprobó aparte, generando 321-340 fuera del repositorio: el 323 y el 324 salen con 1 dosis oculta y level-qa da 0 fallos.

## 9. Fase F3b: el Aguapé, el espeso y la Lancha con el núcleo de F2 (2026-11-07)

- **Autor:** level-pipeline-dev (agente). **Rama:** `v11/curva-f3b-superposicion` (desde a528db54, con F0, F1, F2 y F3).
- **Especificación:** `tools/level-generator/specs/campaign_v3.json`, con el Aguapé (`Ag`), el espeso (`A2`) y la Lancha (`La`) en
  la tabla de mecánicas y en el calendario (ya no hay `f2_mechanics` ni `f2_slots`) y las respuestas recomendadas de 9.4 [P].
- **Semillas:** las de la curva 2 (`:c2`). **Pool de prueba (no son los packs de F5):** `tools/out/curva_f3b/curva_0{7,8,9}_v1.json`
  (400 niveles) y `reserva_curva_0{7,8,9}_v1.json` (80 tableros), con el informe de generación (`curva_report.md`), el de curación
  para el dueño (`curacion_curva2.md`) y level-qa en 0 fallos (`qa_curva2.md`, con el bot atento, y `qa_reserva_curva2.md`). El
  pool de F3 (`tools/out/curva_f3/`) era sin la 1.1 y ya no se reproduce con `campaign_v3.json`.

### 9.1 Cómo se colocan [V]

- **Aguapé:** un grupo de 2-3 cuias [P] (el espeso, 4-5 con `n` = 2) sin otra mecánica, sin Barro y sin empezar selladas (las
  llenas mezcladas sí: la mata las congela), con una cuia (dos con frutas distintas en el espeso) que sella solo recibiendo. El
  espeso lleva dos vacíos bajo la mata [P], y el Aguapé de un reto o un hito con la Hoja, el Matapi o la Lluvia, un vacío más [P]:
  sin eso no hay cuia que selle solo recibiendo. El orden visual deja cada grupo contiguo y en una fila; con 11 cuias o más el
  nivel escribe `rows` (las 3 filas de D-006: 4/4/3, 4/4/4, 5/4/4, 5/5/4) [P] y la variante `plus_empty`, unas que no partan
  ningún grupo. Se verifica con `BoardBuilder`, `LevelValidation`, A* y la solución del par repetida: cada grupo se suelta.
- **Lancha:** «saca una cuia del tablero base y la sube a la carga»: 1-3 dosis de las cimas pasan a una cuia de carga nueva, con
  `k` = vacíos + 1 [P] (3 en el estreno, el del texto del tutorial). **Hallazgo [V]:** para atracar, las T − 1 − k cuias que quedan
  tienen que guardar las 4K − c dosis de fuera de la carga, así que **T ≥ K + k + 1**: la Lancha necesita la carga, la holgura y
  sus k vacías, y `k` = vacíos + 1 es el único `k` posible con una cuia de holgura. Consecuencias: su estreno con 2 vacíos solo cabe
  en el tope de 10 cuias con K 6 (`role_caps.debut.k` pasa a 6-8 y `k_cells` lleva K 6 en 363-365) [P]; en un reto con K 9 llega a
  12 cuias; y no cabe en ningún hito de K 11 (14 cuias > 13) ni en la cúspide de K 12.
- **Alivio:** las variantes no parten grupos y suben `k` lo justo; un candidato con Aguapé o Lancha sin ninguna variante que baje el
  bot 0,20 (03 §17 #8) se descarta al medirlo [P] (sin eso, 6 niveles del pool se quedaban sin alivio y no se escribía ningún pack).
- **Reparto:** el Aguapé titular del ciclo 301-320 con 8 niveles y después 2-4 por ciclo (retos de la 1.0 que no llegaban a Difícil
  y hitos como palanca); el espeso en 347-348 y en 10 hitos (420, 500, 520, 540, 560, 600, 640, 660, 680 y 700); la Lancha titular
  del ciclo 361-380 con 8 y después 2 por ciclo, sobre todo en comodidad. `--validate-spec`: 27 reglas, 0 fallos, 3 avisos PRESSURE
  (660, 680 y 700). Las primeras medidas (`medida_base_*.md`, sin las respuestas de 9.4) llevaron a quitar el Aguapé de los retos donde
  no ayudaba (466 con el espeso, 50 %; 509 y 558, Fl+Ag, 65-70 %; 672; 686, 60 %) y la Lancha de los retos 398, 449 y 572 (48-70 %).

### 9.2 El pool de prueba 301-700 [V]

| Medida | Mundo 7 (301-420) | Mundo 8 (421-560) | Mundo 9 (561-700) | F3, sin la 1.1 (8 / 9) |
|---|---|---|---|---|
| Ciclos que colocan | 6 de 6 | 7 de 7 | 7 de 7 | 7 / 7 (el 7: 5 de 6) |
| Retos con Difícil | **28 de 30** | **32 de 35** | **33 de 35** | 32 / 29 de 35 |
| Hitos con Muy difícil | **6 de 6** | **7 de 7** | **7 de 7** | 7 / 6 de 7 (660: 73 %) |
| T media / mediana | 10,4 / 10 | 10,4 / 10 | 10,4 / 10 | 10,3 / 10,3 |
| K media | 8,3 | 8,4 | 8,4 | 8,4 |
| Niveles con T ≤ 11 / ≥ 12 / ≥ 13 | 104 / 16 / 6 | 127 / 13 / 7 | 128 / 12 / 7 | 132 / 8 / 5 y 130 / 10 / 5 |
| Niveles con Aguapé / espeso / Lancha | 22 / 3 / 11 | 14 / 4 / 14 | 17 / 5 / 13 | — |
| Niveles con `rows` | 13 | 9 | 14 | — |
| Niveles con ocultas | 19 | 28 | 27 | 28 / 27 |

Retos sin chip: 329 (Ma, K 10, 72 %) y 352 (B2+Re, 57 %) en el 7; 449 (Br+Re, 72 %), 509 (Fl, 72 %) y 549 (La+Ln, 72 %) en el 8; 672
(Li+Re, 70 %) y 686 (Ma+Ho, 72 %) en el 9. Son 2-3 por mundo, dentro de lo que aceptó F1-2 (3-4).

### 9.3 Los retos y los hitos que no colocaban, y el escalón [V]

| Reto | Celda en F3 | Bot en F3 (K 9 / K 10) | Celda en F3b | Bot en el pool |
|---|---|---|---|---|
| 429 | Ma+Re | 73 % / 80 % | Ag+Ma+Re | **75 %** |
| 452 | Pi+Br | 45 % / no cabe | Pi+Br+Ag | **82 %** |
| 466 | Li+B2 | 73 % / 70 % | Li+B2 (con el espeso: 50 %) | **88 %** |
| 566 | Ma+Re | 73 % / 75 % | Ag+Ma+Re | **75 %** |
| 592 | Pi+B2 | 63 % / no cabe | Pi+B2+Ag | **78 %** |
| 646, 649, 652 | Pi+Br+Ha, Pi+Ha, Br+Re | 80 %, 83 %, 83 % | igual; 652 Ag+Br+Re | **80 %, 82 %, 82 %** |
| 658 | Ha+Re | 28 % / 28 % | Ag+Ha+Re | **85 %** |
| 672 | Li+Re | 70 % / sin colocar | Li+Re (con el Aguapé: 68 %) | 70 % |
| 686 | Ma+Ho | 73 % / 88 % | Ma+Ho (con el Aguapé: 60 %; con K 10 en F3b: 68 %) | 72 % |
| 698 | Ha+B2 | 73 % / 83 % | Ag+Ha+B2 | **88 %** |
| 409, 412, 415, 418 | Fl, Fl+Ba, Fl+Ll, Ba+Tu | ciclo sin colocar | Fl+Ll, Fl+La, Fl, Br+Tu | **75 %, 78 %, 85 %, 88 %** |

Escalón de presión de los hitos (plan / tablero) y bot atento ×40 (03 §17 #20; solo informe):

| Hito | Celda | Plan | Tablero | Bot | Bot atento |
|---|---|---|---|---|---|
| 320 | Ha+Ll+Br | 3 | 5 | 93 % | 90 % |
| 340 | Ag+Br+Ba | 4 | 5 | 100 % | 100 % |
| 360 | B2+Ln+Ag | 5 | 6 | 100 % | 83 % |
| 380 | Pi+Ba+Ag | 5 | 5 | 93 % | 43 % |
| 400 | Li+Ha+Ag | 6 | 6 | 100 % | 100 % |
| 420 | Fl+Ba+A2 | 7 | 8 | 100 % | 100 % |
| 440 | Ag+Ho+Re | 4 | 5 | 100 % | 100 % |
| 460 | Pi+Ba+Ag | 5 | 5 | 100 % | 85 % |
| 480 | Li+B2+Ho | 5 | 5 | 100 % | 100 % |
| 500 | Re+Ha+A2 | 6 | 6 | 95 % | 95 % |
| 520 | Li+Ha+A2 | 7 | 7 | 100 % | 73 % |
| 540 | Ln+Ha+A2 | 7 | 7 | 98 % | 93 % |
| 560 | Fl+Re+A2 | 8 | 8 | 93 % | 78 % |
| 580 | Ba+B2+Ag | 5 | 5 | 100 % | 90 % |
| 600 | Ha+Ba+A2 | 6 | 6 | 100 % | 75 % |
| 620 | Ln+Ll+Ag | 6 | 6 | 100 % | 98 % |
| 640 | Li+Ba+A2 | 7 | 7 | 95 % | 93 % |
| 660 | Ln+Ha+A2 | 8 | **7** | 98 % | 95 % |
| 680 | Li+Ha+A2 | 8 | **7** | 100 % | 100 % |
| 700 | Fl+Ha+A2 | 9 | **8** | 100 % | 100 % |

**Lectura.** El Aguapé y el espeso sí arreglan los hitos: los 20 colocan con Muy difícil y 17 llegan al escalón del plan (en F3,
solo el 320 y el 480 en los mundos 8-9); los tres que no (660, 680, 700) se quedan a 1 escalón, porque el 8 y el 9 piden ocultas
(que no van con la Hoja atada) o un K que la cúspide no tiene. En los retos el Aguapé arregla los que se quedaban por la celda
(452, 592, 658) y lleva justo al 75 % los de 73 % (429, 566), pero **no ayuda donde ya hay un filtro que ocupa el vacío** (686) ni con
la Liana (672), y el espeso en un reto lo hace más fácil (sus dos vacíos). El bot atento no crece hito a hito en 8 sitios (360, 380,
460, 500, 520, 560, 600 y 640; aviso QA-BOT-ATENTO): el 380 (Pi+Ba+Ag, 43 %) es el más bajo; para la curación.

### 9.4 Las 5 preguntas de F3: respuesta recomendada [P]

Aplicadas en `campaign_v3.json` en un commit aparte («Aplica en campaign_v3 las respuestas recomendadas…»); el dueño solo dice sí o no.

1. **K 10 en 429, 566, 686 y 698: no.** El Aguapé lleva a Difícil el 429 (75 %), el 566 (75 %) y el 698 (88 %, mejor que el 83 % de
   K 10), sin cuia de más (la pregunta de la curva es «menos cuias»). El 686 no llega ni con K 10 (68 % en F3b; el 88 % de F3 era otra
   colocación) ni con el Aguapé (60 %): va sin chip (F1-2) y la curación decide.
2. **452 y 592: el Aguapé como tercera mecánica** (Pi+Br+Ag 82 %, Pi+B2+Ag 78 %). No hace falta cambiar la celda ni K.
3. **Ciclo 641-660: el Aguapé en el 658 (Ag+Ha+Re, 85 %) y el 652 (Ag+Br+Re, 82 %) y el espeso en el hito 660 (Ln+Ha+A2, 98 %).**
   Así el ciclo entero queda en su ventana (sin el 652, el 646 y el 652 salían a 25 % y 55 %).
4. **Ciclo 401-420: los retos de la Flor reordenados (409 Fl+Ll y 415 Fl) y el 418 en Br+Tu.** El ciclo coloca, con los cinco
   retos en Difícil (75-88 %) y la cúspide 420 (Fl+Ba+A2) en el 100 %. La Flor conserva sus 8 niveles.
5. **Las Aguas turbias no cuentan en el «0-1 mecánicas» de la comodidad (03 §12)**, que es guía del calendario: son un modificador
   con su propia palanca y las 3 comodidades turbias por ciclo (F1-1) van con un filtro. Sí cuentan en MAX-2 (2 en una comodidad),
   como hasta ahora. Sin cambio de celdas; queda en la nota de `mechanics_per_level`.

### 9.5 Preguntas para el dueño

1. Las 5 respuestas de 9.4, ya aplicadas como [P].
2. **La Lancha y el tope de cuias (hallazgo de 9.1):** ¿se acepta K 6 en su estreno (363-365, el tope de estreno pasa a K 6-8), o se
   prefiere 11 cuias con K 7? ¿Y que no vaya en hitos (con K 11 serían 14 cuias) ni en la cúspide?
3. **La Lancha en un ciclo de aguas bajas:** el 361-380 es de temporada B en la 1.0 y la Lancha de aguas altas; se exime a su estreno
   de SEASONS (`season_debut_exempt`) [P] en lugar de cambiar la temporada del ciclo.
4. **El escalón 8-9 de 660, 680 y 700:** con el espeso llegan a 7, 7 y 8. ¿Se baja el plan a 7-7-8 o se ponen ocultas en esos hitos
   (sin la Hoja atada)?
5. **Decisiones [P] de la herramienta:** grupos de 2-3 cuias (4-5 el espeso); el espeso con dos vacíos bajo la mata; un vacío más
   con la Hoja, el Matapi o la Lluvia; `rows` de 3 filas con 11-14 cuias; `k` = vacíos + 1; el Aguapé fuera de «mecánicas
   distintas» en el escalón; candidatos sin alivio descartados; los estrenos de la 1.1 primero en la reserva de la curva.

### 9.6 Para reproducir

```
dotnet run --project tools/level-generator -c Release -- --validate-spec tools/level-generator/specs/campaign_v3.json
dotnet run --project tools/level-generator -c Release -- --spec tools/level-generator/specs/campaign_v3.json --levels 301-700 --out tools/out/curva_f3b --report tools/out/curva_f3b/curva_report.md --curation tools/out/curva_f3b/curacion_curva2.md
dotnet run --project tools/level-generator -c Release -- --reserve tools/out/curva_f3b/curva_07_v1.json tools/out/curva_f3b/curva_08_v1.json tools/out/curva_f3b/curva_09_v1.json --spec tools/level-generator/specs/campaign_v3.json --out tools/out/curva_f3b --report tools/out/curva_f3b/reserva_curva_report.md
dotnet run --project tools/level-qa -c Release -- tools/out/curva_f3b/curva_07_v1.json tools/out/curva_f3b/curva_08_v1.json tools/out/curva_f3b/curva_09_v1.json --spec tools/level-generator/specs/campaign_v3.json --levels 301-700 --report tools/out/curva_f3b/qa_curva2.md
dotnet run --project tools/level-qa -c Release -- --reserve tools/out/curva_f3b/reserva_curva_07_v1.json tools/out/curva_f3b/reserva_curva_08_v1.json tools/out/curva_f3b/reserva_curva_09_v1.json --campaign tools/out/curva_f3b/curva_07_v1.json tools/out/curva_f3b/curva_08_v1.json tools/out/curva_f3b/curva_09_v1.json --spec tools/level-generator/specs/campaign_v3.json --report tools/out/curva_f3b/qa_reserva_curva2.md
dotnet run --project tools/level-generator -c Release -- --measure --spec tools/level-generator/specs/experimental/campaign_v3_f3b_q.json --levels 561-700 --report tools/out/curva_f3b/medida_q_561-700.md
```

Las `medida_base_*.md` se midieron con el reparto anterior a las respuestas de 9.4 (commit a043f849) y las `medida_q_*.md` con
`specs/experimental/campaign_v3_f3b_q.json` (las respuestas más K 10 en 429 y 686); con la especificación de hoy ya no salen igual.
