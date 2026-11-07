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
