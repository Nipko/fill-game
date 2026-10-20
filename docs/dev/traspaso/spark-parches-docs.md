# Parches de documentación del Reto del río (E11 y E12)

Para el integrador. Encargo `docs/dev/traspaso/spark-reto-del-rio.md` §3.4 («Documentación») y R12; enganches E11 y E12 de `docs/dev/traspaso/spark-estado.md`. El Spark no edita ninguno de estos archivos (encargo §3.2): aquí van los parches exactos, para aplicarlos tal cual.

**Base.** Todos salen de `spark/reto-del-rio` en `92e79fb` y se probaron en un worktree temporal de esa rama: cada uno con `git apply --check` sobre el árbol limpio, y después los ocho aplicados juntos. Resultado: los ocho, **OK**; con los ocho aplicados, `python tools/validate_schema.py` da 103/103 (no cambia `tools/out/schema_example_08_4_1.json`). Si la integración ya movió alguno de estos archivos, `git apply --3way` o el texto de cada parche sirven igual.

**Cómo se aplican.** Cada bloque `diff` de abajo es un parche completo. Para sacarlos del Markdown, desde la raíz del repositorio:
```
python -c "import re,sys;t=open('docs/dev/traspaso/spark-parches-docs.md',encoding='utf-8').read();[open(n,'w',encoding='utf-8',newline='\n').write(b) for n,b in re.findall(r'<!-- parche: (\S+) -->\n~~~~diff\n(.*?)~~~~\n',t,re.S)]"
git apply --check E11-*.diff E12-*.diff && git apply E11-*.diff E12-*.diff
```
(Los mismos ocho, como archivos, quedaron en la carpeta temporal de la sesión del Spark, que puede perderse.)

**Qué cambian y por qué.** Ninguno inventa reglas: cada cambio cita la decisión del dueño (R-Qn, que D-028 registra con su texto literal) o el § de donde sale. Las marcas [P] que quedan son las del GDD o las de la implementación (nota `docs/dev/reto-del-rio.md`), sin decisión del dueño.

| Parche | Archivo | Qué |
|---|---|---|
| `E11-decisiones.diff` | `docs/dev/decisiones.md` | D-028 (el siguiente al último, D-027): la fila del índice y la entrada, con R-Q1-R-Q16, R-Q1b, R-Q2b, R-Q11b, R-Q12b, la aclaración de R-Q12 y la confirmación de `IServerDate` en AF.Platform; fecha 2026-10-19 |
| `E11-03-niveles.diff` | `docs/gdd/03` §4.4, §16, «Preguntas abiertas» 2 y «Dependencias» | 30 candidatos **por fuente** en el Reto (R-Q11b); índice `mod 729` y 2.187 tableros (R-Q2b); B2 con K 8 y B3 sin tope (R-Q1b); paleta por banda (R-Q1); agua y fondo por la fecha (R-Q2); fuentes con respaldos ([P]); ventana del bot como compuerta (R-Q11); avisos de R-Q10; `daily_overrides` por banda (R-Q6) |
| `E11-05-economia.diff` | `docs/gdd/05` §6.1 | Sin intersticial al terminar un Reto (R-Q13). La nota del Spark lo pedía aquí (§1.3) |
| `E11-06-meta.diff` | `docs/gdd/06` §4.1, «Preguntas abiertas» 2 y «Dependencias» | Pool de 729 por banda y el agua por la fecha (R-Q2, R-Q2b); bandas y paleta (R-Q1, R-Q1b); comodines (R-Q8); partida en curso (R-Q3, R-Q4, R-Q16); racha cada 7 y marco fuera (R-Q7) con la Pororoca antes del 30 (R-Q14); `{fecha: {banda: id}}` (R-Q6); reconciliación (R-Q12, su aclaración y R-Q12b); sin intersticial (R-Q13); `fail` (R-Q9, R-Q15); el pseudocódigo |
| `E11-08-arquitectura.diff` | `docs/gdd/08` §3, §4.1, §4.3, §6.1, §6.2, §8.2, §9 y «Dependencias» | Carpeta `Data/Daily/`; fila `kind` con `day 0-728` y `water_base` del mundo de su fecha; bloque `daily_challenge` del perfil (R-Q4, R-Q5, R-Q16; formatos de R7b [P]); pool de 2.187 en §6.1 y §6.2; cuándo salen `level_start` y `af_daily_challenge` en el Reto (R-Q9, R-Q15, R-Q16); tipo de `daily_overrides` (R-Q6) y nota de `live_daily_offset` (R-Q2b) |
| `E12-prototipo-v0.diff` | `docs/dev/prototipo-v0.md` | «Sondas del Reto del río»: el texto de la entrada de R10 de `spark-estado.md`, con `level_start` «al terminar Llenar y teñir, o al reanudar sin intro; una vez por partida» y el campo `"daily"` de E24 |
| `E12-README.diff` | `docs/dev/traspaso/README.md` | Estado del 2026-10-19 del Reto en el §1 (qué hay, dónde, integración, cómo se prueba) y la fila de D-028 en el §7 |
| `E12-CLAUDE.diff` | `CLAUDE.md` | Mapa del repositorio (`Scripts/LiveOps/` con AF.LiveOps; `Data/Daily/daily_pool_v1.json`, generado: no se edita a mano; `level-generator/Daily/` y `level-qa/Daily/`) y los dos comandos del Reto; los recuentos de Unity, con marcadores |

**Notas para el integrador.**
- **Número de la decisión:** D-028, porque el último que existe es D-027 en `spark/reto-del-rio`, en `origin/integracion-fase1-fase2` (`bc65242`) y en `origin/main`. Si la integración ya tiene un D-028, renumera la entrada, su fila y las citas «D-028» de los ocho parches (`sed -i 's/D-028/D-0NN/g' E1*.diff` antes de aplicarlos).
- **Fecha de R-Q1-R-Q10:** D-028 usa el 2026-10-19 (encargo §5 y reloj del Spark) y anota que la primera versión del encargo decía 2026-10-20 (R1, `spark-estado.md`).
- **`sec_clock_max_drift_h`:** la tabla de 08 §9 no tiene columna de módulo, así que no se anota allí que vive en `AF.Platform/ClockSecurityConfig`; lo dicen D-028 («Consecuencias») y la nota §1.2 y §11.
- **El esquema no cambia:** `slot.day` admite 0-729 y el pool usa 0-728 (R-Q2b). La `description` de `slot.day` en `schemas/af-level-1.schema.json` aún dice «3 bandas × 730 = 2.190 tableros», y la de `slot.band`, «K 6-7 / 8-9 / 10-11»: son descripciones, no reglas, pero el esquema es el contrato 21 y el Spark no lo toca (encargo §3.2). Si se cambian, `tools/validate_schema.py` debe seguir en 103/103.
- **`level_start` al reanudar** (08 §8.2) va marcado [P]: lo propuso R10 (revisado y APROBADO), no lo decidió el dueño. Si no se quiere en el GDD, quita ese paréntesis del parche de 08: la sonda funciona igual.
- **`DailyChallengeConfigTests`** lee la tabla de 08 §9 del Markdown y separa las celdas por `|`: los textos nuevos de las filas `daily_overrides` y `live_daily_offset` no llevan `|`, el tipo sigue empezando por `json` y los defaults no cambian. Comprobado leyendo las celdas; el test en Unity no se ejecutó con los parches aplicados.
- **`CLAUDE.md`, recuentos:** los marcadores «EDITMODE_TOTAL», «EDITMODE_OK», «EDITMODE_OMITIDOS», «PLAYMODE_TOTAL», «PLAYMODE_OK» y «PLAYMODE_EXPLICITOS» los pone el integrador con lo que mida tras integrar. Referencia del Spark (ARM64): EditMode 3.057 (3.054 + 3 omitidos) en `8327d23`; PlayMode 1.369 (1.206 + 163) en `8327d23` y 1.373 (1.206 + 167) con las capturas de R11. `dotnet test` sigue en 981 (975 + 6): el Spark no añade tests .NET.
- **README §7:** la tabla acaba en D-026 (falta D-027, que no es del Reto); la fila de D-028 va detrás de D-026.
- **Fuera de estos parches** (no es documentación de destino): la tabla de §6 de `docs/dev/reto-del-rio.md` aún da el día 729 para el 2027-12-31, el 2025-12-31 y el 2026-01-01 con `live_daily_offset` −1; con el pool de 729 días son el 0, el 728 y el 728. Lo arregla el Spark en su nota.

**Comprobación de los comandos que añade `CLAUDE.md`** (en el worktree temporal, `92e79fb`, ARM64, Release):

| Comando | Resultado |
|---|---|
| `dotnet run --project tools/level-generator -c Release -- --daily --spec tools/level-generator/specs/daily_v1.json --out Assets/_Project/Data/Daily --check` | `--check: Assets/_Project/Data/Daily/daily_pool_v1.json identico`, código 0, 49 s (27,9 s de generación con 20 hilos) |
| `dotnet run --project tools/level-qa -c Release -- --daily Assets/_Project/Data/Daily/daily_pool_v1.json` | 2.187 tableros, 21 comprobaciones: 0 fallos y 1.678 avisos (569 `QA-DAILY-REPEAT-SHAPE` y 1.109 `QA-DAILY-EARLY-TRAPS`, los de R-Q10), código 0, 49 s |
| `python tools/validate_schema.py` (con los ocho parches aplicados) | 103/103 PASS |

**Resultado de `git apply --check`** (cada parche, sobre `spark/reto-del-rio` limpio en `92e79fb`):

| Parche | `git apply --check` |
|---|---|
| `E11-decisiones.diff` | OK |
| `E11-03-niveles.diff` | OK |
| `E11-05-economia.diff` | OK |
| `E11-06-meta.diff` | OK |
| `E11-08-arquitectura.diff` | OK |
| `E12-prototipo-v0.diff` | OK |
| `E12-README.diff` | OK |
| `E12-CLAUDE.diff` | OK |
| Los ocho juntos, aplicados en orden | OK; árbol con 8 archivos cambiados, 138 inserciones y 32 borrados |

---

## E11 · GDD y decisiones

### E11-decisiones.diff · `docs/dev/decisiones.md`: D-028

<!-- parche: E11-decisiones.diff -->
~~~~diff
diff --git a/docs/dev/decisiones.md b/docs/dev/decisiones.md
index 1ac5c6b..cf78ed9 100644
--- a/docs/dev/decisiones.md
+++ b/docs/dev/decisiones.md
@@ -33,6 +33,7 @@ # Registro de decisiones del producto
 | D-025 | 2026-10-14 | Un solo formato de cifras, también en los precios | Decidida por el dueño («unificar formatos», tras la prueba de la tanda 4). La regla (región del teléfono, Q8-A) es [P] de este registro. Implementada en FXT (puntos 1 y 2; el 3, lista la función para el adaptador de Unity IAP) |
 | D-026 | 2026-10-16 | Audio provisional por código y permiso `VIBRATE` | Decidida por el dueño («lanza ya AU1 en paralelo a la ola 4, con los arreglos de niveles y graves, y apruebo el permiso VIBRATE para el háptico del sellado»). Implementada en AU1 (tanda 4, ola 4, carril de audio): `AF.Audio` y `AF.Audio.Unity`, los 61 efectos con su nivel, Ajustes de Sonido, Música y Vibración, los hápticos y el permiso en el manifiesto; AU2 (la misma ola) añadió el ambiente por mundo y la música adaptativa provisionales, también por código y detrás del ajuste Música [P]. Integrada en `bd649ac` y llevada a 07 §9 (FXD2, 2026-10-16). Pendiente: la prueba en el teléfono y los encargos al compositor y al diseñador de sonido (07 §9.5) |
 | D-027 | 2026-10-18 | Mundos 7-9: las celdas, los K y los ajustes del generador para poder generarlos | Decidida por el dueño (todas las recomendaciones de la propuesta de los mundos 7-9: 25 celdas, K = 10 en los retos de 421-700, hitos de 14 cuias con K = 12 + 1 vacío, alivio hasta 13 cuias con la Tinaja y QA-PAR sin el par del rango en los estrenos de la Tinaja). Implementada en GEN79 (tanda 4, ola 7): especificación, generador, level-qa y los packs de 301-700 en sus dos variantes. Pendientes: la curación del dueño (123 niveles), el registro en el cliente (C1b) y el reflejo en 02 §5 y 03 §11, §12 y §17 (carril de documentación) |
+| D-028 | 2026-10-19 | Reto del río: pool de 729 días, bandas, premio, anti-reloj y partida | Decidida por el dueño (R-Q1-R-Q10 con «Acepto tus propuestas»; R-Q1b, R-Q11-R-Q16 y R-Q12b con la recomendada; R-Q2b, 729 días; R-Q11b, 30 candidatos por fuente). Implementada por el equipo Spark (R0-R11, `spark/reto-del-rio`; el bloque (a) integrado en `fcbbd94`). Documentación aplicada con E11 (03, 05, 06 y 08). Pendientes: los enganches E1-E25, la integración y la prueba en el teléfono |
 
 ---
 
@@ -2536,3 +2537,75 @@ ## D-027 · Mundos 7-9: las celdas, los K y los ajustes del generador para poder
 - **Especificación y datos** (GEN79): `tools/level-generator/specs/campaign_v2.json`, `combinations_registry_v1.json`, los packs y los informes de arriba y `seasons_v1.json`; regenerados por el texto nuevo de sus informes (sin cambios en sus packs): `tools/out/curation_w06.md` (la lista aparte de los retos e hitos sin su etiqueta) y `tools/out/sample_tinaja_report.md` (el tope del alivio).
 - **Código** (GEN79): `tools/level-generator/Generation/Superposition.cs`, `Metrics/Relief.cs`, `Generation/CampaignReport.cs`, `Generation/CurationReport.cs`, `Generation/SeasonTableWriter.cs` y los `--self-test` (`TinajaSelfTest`, `RegistrySelfTest`, `HolguraSelfTest` y `CurationSelfTest`); `tools/level-qa/Checks/BaseChecks.cs`, `TinajaChecks.cs`, `ReliefChecks.cs` y `RulesSelfTest.cs`, con dos fixtures nuevos.
 - **Documentación** (GEN79): este registro, `tools/level-generator/README.md`, `tools/level-qa/README.md` y `docs/dev/traspaso/README.md`.
+
+---
+
+## D-028 · Reto del río: pool de 729 días, bandas, premio, anti-reloj y partida
+
+- **Fecha:** 2026-10-19. Registrada el 2026-10-19 (enganche E11 del equipo Spark: `docs/dev/traspaso/spark-reto-del-rio.md` §5 y `docs/dev/traspaso/spark-estado.md`).
+- **Estado:** decidida por el dueño en el chat, en cuatro respuestas del 2026-10-19:
+  - **R-Q1-R-Q10:** «Acepto tus propuestas para R-Q1 a R-Q10 del Reto del río; déjalas en el encargo del Spark». La primera versión del encargo las fechaba el 2026-10-20; el encargo §5 y el reloj del Spark dicen 2026-10-19.
+  - **R-Q1b, R-Q11-R-Q16, la aclaración de R-Q12 y R-Q12b:** «aplica las recomendaciones y pon trabajo en paralelo para avanzar mas rapido», con la regla del integrador «si tienen recomendada, vamos con esas» (encargo §5): la opción (a), la recomendada, de cada una.
+  - **R-Q2b:** «Pool de 729 o 738 días» y, después, «729 días (Recomendado)».
+  - **R-Q11b:** «Aceptar (Recomendado)».
+  - **Confirmado por el integrador** (encargo §5): `IServerDate` y `FakeServerDate` van en AF.Platform (08 §2.2), no en AF.LiveOps.
+  - **Implementada** por el equipo Spark (R0-R11) en `spark/reto-del-rio`: el bloque (a) (generador, QA y lector del pool) está integrado en `integracion-fase1-fase2` (`fcbbd94`); el pool publicado (R5, `3c40393`), la lógica (R6-R7b), la partida (R8), las pantallas (R9) y las sondas (R10), pendientes de integrar. Pendientes: los enganches E1-E25 de `spark-estado.md` y la prueba en el teléfono.
+
+**Contexto.**
+- El GDD fija el Reto del río (03 §16, 06 §4.1, 08 §4.1, §4.3, §8.2 y §9), pero deja huecos que el código necesita: la paleta y el agua de cada banda, qué pasa al cruzar la medianoche, si se reanuda, los campos del perfil, el formato de `daily_overrides`, la cadencia de la racha, los comodines, qué es `fail` y qué comprobaciones de la campaña valen en el pool (encargo §5, R-Q1-R-Q10).
+- Al implementarlo salieron más: R-Q1 no cabía en B2 con K = 9 ni en B3 (con los tintes de su tramo hay al menos 3 pares de riesgo; `docs/dev/reto-del-rio.md` §1.1); con 1 vacío el bot falla 0,88-1,00 (03 §4.4), así que la ventana de B2 puede no alcanzarse; la reconciliación con el servidor, el intersticial y la Pororoca antes del nivel 30 no estaban decididos; y con 730 días y 9 mundos (730 mod 9 = 1) el mundo grabado en cada tablero deja de seguir a la fecha en la segunda vuelta del pool.
+
+**Decisión.**
+
+| Id | Decisión |
+|---|---|
+| R-Q1 | Cada banda usa solo los tintes ya estrenados en su tramo de la campaña: B1 (K 6-7) con los de 1-40 y `max_pares_riesgo` 0; B2 (K 8-9) con los de 1-80 y como mucho 1; B3 (K 10-11) con los de 1-200 y como mucho 2 (lo ajusta R-Q1b) |
+| R-Q1b | (a) B2 solo con K = 8 y B3 con `max_pares_riesgo` libre, como la campaña desde el 201 (03 §4.5). B1 sin cambios |
+| R-Q2 | El agua (`water_base`) y el fondo rotan por los mundos 1-9 según la fecha: el agua y el kit de fondo del mundo de ese día |
+| R-Q2b | Pool de **729 días por banda** (81 × 9), 2.187 tableros: el día del pool es `(días_desde(2026-01-01) + live_daily_offset) mod 729` y el mundo del tablero, `1 + (día mod 9)`, que sigue a la fecha en todas las vueltas (con `live_daily_offset` constante, desplazado por él) |
+| R-Q3 | Manda la fecha en que se empezó la partida: se sigue con ese tablero y se cobra ese premio aunque se gane pasada la medianoche |
+| R-Q4 | La partida del Reto se reanuda al volver a abrir la app el mismo día; al cambiar de día se descarta |
+| R-Q5 | El bloque `daily_challenge` del perfil añade la última fecha cobrada, el premio pendiente de validar y los sellos del mes (además de `last_date`, `result` y `streak_days`) |
+| R-Q6 | `daily_overrides` = `{fecha: {banda: id del pool}}` |
+| R-Q7 | La Pororoca sale cada 7 días seguidos ganados (cada racha completa); un día sin ganar rompe la racha; el marco de país de los sellos queda fuera hasta que existan cosméticos |
+| R-Q8 | Los comodines son los de la campaña salvo los que el GDD excluye en el Reto (Cuia de rescate y Salto del Boto), y gastan el stock igual que en la campaña |
+| R-Q9 | `result = fail` en `af_daily_challenge` es salir sin ganar ese día (lo concretan R-Q15 y R-Q16) |
+| R-Q10 | La anti-repetición (03 §17 #9) y el tope de trampas tempranas (03 §17 #3) se miden en el pool y se informan como aviso, sin ser compuerta |
+| R-Q11 | (a) La ventana del bot es compuerta: un día sin candidato dentro de la ventana no se publica hasta ajustar la especificación (fuente o K) |
+| R-Q11b | El «máximo 30 candidatos» de 03 §4.4 se lee como **30 por fuente**, con respaldos en orden. No se relajan vacíos, K, capacidad ni ventanas |
+| R-Q12 | (a) Al reconectar, la reconciliación no retira las semillas cobradas sin red |
+| R-Q12 (aclaración) | (a) Al anular una fecha no se retiran semillas ni la Pororoca |
+| R-Q12b | (a) Un premio pendiente solo se valida si `\|fecha − hoy_servidor\| ≤ 1 día`; los demás se anulan |
+| R-Q13 | (a) Sin intersticial al terminar un Reto |
+| R-Q14 | (a) La Pororoca de la racha entra en el stock y espera al nivel 30, que la desbloquea |
+| R-Q15 | (a) `af_daily_challenge.result = fail` una vez por día, y solo si ese día termina sin victoria |
+| R-Q16 | (a) Salir de la partida del Reto sin ganar la deja reanudable el mismo día; al cambiar de día se descarta (R-Q4) |
+
+**Consecuencias.**
+- **Datos:** `Assets/_Project/Data/Daily/daily_pool_v1.json`, 2.187 tableros y 1.430.135 bytes, generado por `tools/level-generator --daily` desde `tools/level-generator/specs/daily_v1.json` (no se edita a mano), con su informe `tools/out/daily_pool_v1_report.md`. `tools/level-qa --daily` lo da con 0 fallos y 1.678 avisos (los de R-Q10). Las fuentes de cada banda y `k_by_day` siguen siendo [P] de la especificación (03 §16).
+- **Código** (archivos nuevos, sin paquetes, SDK, permisos ni versiones nuevas): `tools/level-generator/Daily/` y `tools/level-qa/Daily/`; `Scripts/LiveOps/` (asmdef AF.LiveOps, sin motor); `DailyPool` y `DailyPoolReader` en AF.Levels; `IServerDate`, `FakeServerDate` y `ClockSecurityConfig` (la fuente única de `sec_clock_max_drift_h`) en AF.Platform; `LevelFlow.Daily.cs` en AF.GameFlow; la tarjeta, la partida, el calendario de sellos y `DevProbe.Daily.cs` en AF.UI.
+- **Formato de nivel:** sin cambios. El esquema admite `slot.day` 0-729 y el pool usa 0-728; el contrato 21 y `pack_schema` no cambian.
+- **Remote Config:** sin claves nuevas; `daily_overrides` cambia de tipo (`{fecha: {banda: id}}`) con el mismo default `{}`.
+- **Analítica:** sin eventos nuevos; R-Q15 y R-Q16 fijan cuándo sale `af_daily_challenge` (en la 1.0 no sale `abandon`). La Pororoca de la racha no tiene evento (08 §8.2 no lo define).
+- **Economía:** sin cambios en `tools/econ_sim.py` ni en las cifras [sim] (05 §3.2 ya cuenta el Reto).
+
+**Qué cambia en el GDD** (E11, en el mismo lote que este registro):
+- 03 §4.4 (R-Q11b) y §16 (729 días y 2.187 tableros, B2 con K 8, paleta, agua, fuentes, compuerta y avisos), con las menciones de «Preguntas abiertas», 2, y «Dependencias»;
+- 05 §6.1 (sin intersticial en el Reto, R-Q13);
+- 06 §4.1 (índice mod 729, bandas, comodines, partida en curso, racha, sustituciones, reconciliación, anuncios y analítica; el pseudocódigo), con «Preguntas abiertas», 2, y «Dependencias»;
+- 08 §3, §4.1 (fila `kind`), §4.3 (bloque `daily_challenge`), §6.1, §6.2, §8.2 (`level_start` y `af_daily_challenge`), §9 (`daily_overrides` y `live_daily_offset`) y «Dependencias».
+
+**Alternativas descartadas.**
+- R-Q1b: (b) subir los topes (B2 a 3, B3 a 4); (c) mantener los topes y bajar K (B2 = 8, B3 = 9); (d) ampliar la paleta de B3 a 1-300 (no basta: el 13 añade 6-13 y 13-14).
+- R-Q2b: que el cliente calcule el mundo con la fecha local (cambia el significado de `water_base`, contrato 21); aceptar el salto en la segunda vuelta; 738 días.
+- R-Q11: (b) publicar el más cercano y avisar en QA; (c) ensanchar la ventana (cambia 03 §16).
+- R-Q12: (b) anular las victorias cobradas sin red con fecha posterior a la del servidor (queda en el código detrás de `DailyReconcileMode`, sin usarse). Aclaración: (b) retirar las semillas que queden sin gastar.
+- R-Q12b: (b) validar todo pendiente con fecha ≤ hoy del servidor.
+- R-Q13: (b) intersticial como en la campaña. R-Q14: (b) otro premio en B1; (c) nada antes del 30. R-Q15: (b) un `fail` por salida. R-Q16: (b) salir cierra la partida.
+
+**Cómo revertir.** Con una decisión nueva que sustituya a D-028. R-Q2b y R-Q11b obligan a regenerar el pool con la especificación cambiada (`--daily`), a cambiar el módulo del índice y a pasar `level-qa --daily`; R-Q12 (b) es una opción del código (`DailyReconcileMode`). Mientras el Reto no se publique, sin coste para el jugador.
+
+**Archivos afectados.**
+- **Herramientas y datos** (Spark, R2-R5): `tools/level-generator/specs/daily_v1.json`, `tools/level-generator/Daily/`, `tools/level-qa/Daily/` y `tools/level-qa/fixtures/daily/`, `tools/out/daily_sample/`, `tools/out/daily_sample_report.md`, `tools/out/daily_pool_v1_report.md` y `Assets/_Project/Data/Daily/daily_pool_v1.json`.
+- **Cliente** (Spark, R6-R11): `Assets/_Project/Scripts/LiveOps/`, `Scripts/Levels/DailyPool*.cs`, `Scripts/Platform/IServerDate.cs`, `FakeServerDate.cs` y `ClockSecurityConfig.cs`, `Scripts/GameFlow/LevelFlow.Daily.cs`, `Scripts/UI/DailyChallenge*.cs`, `DailyStampCalendar.cs`, `DailyLevelStateStore.cs`, `DailyText.cs`, `GameBootstrap.Daily.cs` y `DevProbe.Daily.cs`, y sus tests (`Scripts/LiveOps/Tests/`, `Tests/EditMode/Daily*.cs` y `Tests/PlayMode/Daily*.cs`).
+- **Documentación:** este registro, `docs/dev/reto-del-rio.md` (nota de diseño), `docs/dev/traspaso/spark-estado.md`, los capítulos de «Qué cambia en el GDD», `docs/dev/prototipo-v0.md` (sondas), `docs/dev/traspaso/README.md` y `CLAUDE.md` (E12).
~~~~

### E11-03-niveles.diff · `docs/gdd/03-niveles-generacion-dificultad.md` §4.4 y §16

<!-- parche: E11-03-niveles.diff -->
~~~~diff
diff --git a/docs/gdd/03-niveles-generacion-dificultad.md b/docs/gdd/03-niveles-generacion-dificultad.md
index 52204f0..ea3e9cf 100644
--- a/docs/gdd/03-niveles-generacion-dificultad.md
+++ b/docs/gdd/03-niveles-generacion-dificultad.md
@@ -156,7 +156,7 @@ ### 4.4 Fuente por banda
 | Hito Muy difícil (60+) | 1 | K máx. del rango (13 desde 561, en 14 cuias; 14 solo en el evento Encuentro de las Aguas) | `deep`; alternativa `fill` E=2 con K+1 (nunca por encima de 14 cuias) | ≥ 90 % | Pico del tramo |
 | Tutorial 1-10 | 2 (excepciones de 02 §13: niveles 1 y 6 con 1 vacío) | 2-5 | A mano en el editor + verificación; la tabla de 02 §13 (cuias = colores + vacíos) se copia sin alterar; el par se calcula con `rules_pour_mode` (`run` por defecto, D-005); pares del pack v0 en `run`: 3, 6, 7, 8, 10, 10, 11, 13, 14 y 16 (02 §13) | 0-10 % | Enseña Marea (3; ilimitada hasta el 10, `undo_unlimited_until_level`), Volver a la orilla (6 si hay Aguas estancadas; 8 primer reinicio probable), Cuia extra gratis (8); resto de hitos en 02 §13 (research/05 §Primera sesión) |
 
-Rechazo por ventana: la fuente genera candidatos hasta que `bot_fail` cae en la ventana del slot (máximo 30 candidatos; coste 20-60 ms de bot por candidato). Si ningún candidato entra, la comodidad relaja en este orden: añadir una Cuia chica de 2 unidades como "medio vacío", luego pasar a 2 vacíos; el reto y el hito relajan hacia `fill` con 2 vacíos y K+1. La necesidad quedó medida: los ocho tableros de 1 vacío del lote híbrido dieron fallo de bot 0,88-1,00 sin importar la fuente, así que un slot de comodidad con 1 vacío sin rechazo saldría etiquetado Difícil (sección 18.1).
+Rechazo por ventana: la fuente genera candidatos hasta que `bot_fail` cae en la ventana del slot (máximo 30 candidatos; coste 20-60 ms de bot por candidato; en el Reto del río, 30 **por fuente**, con respaldos en orden y sin las relajaciones de abajo: sección 16, R-Q11b, D-028). Si ningún candidato entra, la comodidad relaja en este orden: añadir una Cuia chica de 2 unidades como "medio vacío", luego pasar a 2 vacíos; el reto y el hito relajan hacia `fill` con 2 vacíos y K+1. La necesidad quedó medida: los ocho tableros de 1 vacío del lote híbrido dieron fallo de bot 0,88-1,00 sin importar la fuente, así que un slot de comodidad con 1 vacío sin rechazo saldría etiquetado Difícil (sección 18.1).
 
 ### 4.5 Elección de tintes
 
@@ -426,7 +426,7 @@ ## 15. KPIs por nivel y umbrales de alarma
 
 ## 16. Reto del río y Río sin fin
 
-**Reto del río** (1.0): pool offline pregenerado e indexado por fecha, `reto(fecha, tramo) = pool[tramo][(días_desde(2026-01-01) + live_daily_offset) mod 730]` (`live_daily_offset`, 08 §9; mismo cálculo que 06 §4.1), con tres tramos por progreso del jugador [P]: A = banda 1 (nivel máximo < 41: K 6-7, 2 vacíos, ventana 40-60 %), B = banda 2 (41-200: K 8-9, 1 vacío, 60-80 %), C = banda 3 (201+: K 10-11, 1 vacío, ≥ 75 %); 3 × 730 = 2.190 tableros (pack `daily_pool_v1`) en el esquema `af-level/1` (`schemas/af-level-1.schema.json`, 08 §4.1) con `kind: daily`, `slot {band, day}` en lugar de `level`, `id` `d{b}_n{nnnn}` (patrón `^d[1-3]_n[0-9]{4}$`) y `seed` `amazonfill:v1:daily:b{b}:d{ddd}` (patrón `^amazonfill:v1:daily:b[1-3]:d[0-9]{3}$`), con b = banda 1-3 y el día 0-729 con ceros a la izquierda (cuatro cifras en el `id`, tres en la `seed`; p. ej. `d2_n0417` y `amazonfill:v1:daily:b2:d417`); `difficulty` es opcional en `kind: daily` y el pool se publica sin métricas, con `relief` vacío (`[]`) y nunca en hexadecimal; ≈ 1,4 MB minificados / ≈ 0,1 MB comprimidos [E]; umbrales de tramo en `lvl_daily_bracket_levels` = `[41, 201]`. Sin mecánicas con disparador ni Aguas turbias en el reto obligatorio; la tarjeta Aguas turbias · Bonus (1.1; `ui_murky_bonus_enabled` = false en 1.0, 08 §9) usa el mismo tablero con 2 cuias turbias verificadas. `daily_overrides` en Remote Config sustituye fechas concretas (fiestas, eventos); el premio se reconcilia con la fecha del servidor del último fetch para evitar el cambio de reloj (research/02 C4.2 [V/E]).
+**Reto del río** (1.0): pool offline pregenerado e indexado por fecha, `reto(fecha, tramo) = pool[tramo][(días_desde(2026-01-01) + live_daily_offset) mod 729]` (`live_daily_offset`, 08 §9; mismo cálculo que 06 §4.1; 729 días por banda desde R-Q2b, D-028), con tres tramos por progreso del jugador [P]: A = banda 1 (nivel máximo < 41: K 6-7, 2 vacíos, ventana 40-60 %), B = banda 2 (41-200: K 8, 1 vacío, 60-80 %; R-Q1b), C = banda 3 (201+: K 10-11, 1 vacío, ≥ 75 %); 3 × 729 = 2.187 tableros (pack `daily_pool_v1`) en el esquema `af-level/1` (`schemas/af-level-1.schema.json`, 08 §4.1) con `kind: daily`, `slot {band, day}` en lugar de `level`, `id` `d{b}_n{nnnn}` (patrón `^d[1-3]_n[0-9]{4}$`) y `seed` `amazonfill:v1:daily:b{b}:d{ddd}` (patrón `^amazonfill:v1:daily:b[1-3]:d[0-9]{3}$`), con b = banda 1-3 y el día 0-728 con ceros a la izquierda (el esquema admite hasta el 729) (cuatro cifras en el `id`, tres en la `seed`; p. ej. `d2_n0417` y `amazonfill:v1:daily:b2:d417`); `difficulty` es opcional en `kind: daily` y el pool se publica sin métricas, con `relief` vacío (`[]`) y nunca en hexadecimal; ≈ 1,4 MB minificados / ≈ 0,1 MB comprimidos [E]; umbrales de tramo en `lvl_daily_bracket_levels` = `[41, 201]`. **Paleta por banda** (R-Q1 y R-Q1b, D-028): solo los tintes ya estrenados en su tramo de la campaña (glosario §9): B1 los de 1-40 con `max_pares_riesgo` 0, B2 los de 1-80 con 1 y B3 los de 1-200 sin tope, como la campaña desde el 201 (sección 4.5); B2 se queda en K = 8 porque con K = 9 esa paleta tiene al menos 3 pares de riesgo. **Agua y fondo** (R-Q2 y R-Q2b): rotan por los mundos 1-9 según la fecha; el tablero del día d del pool lleva el mundo 1 + (d mod 9) y su `water_base` (glosario §8), y como 729 = 81 × 9 el mundo sigue a la fecha en todas las vueltas (con `live_daily_offset` constante, desplazado por él). **Fuente y ventana:** la análoga de la sección 4.4 sin sus relajaciones (que cambiarían los vacíos de la banda), con rechazo por ventana de hasta 30 candidatos **por fuente** y respaldos en orden (R-Q11b): B1 `fill` E=2 → `deep` → `walk` → `deep` de 60.000 nodos; B2 `deep` → `fill` E=1; B3 `deep` [P] (`tools/level-generator/specs/daily_v1.json`). **La ventana del bot es compuerta** (R-Q11): un día sin candidato dentro de ella no se publica hasta ajustar la especificación (en el generador es un error y en `tools/level-qa --daily`, `QA-DAILY-BOT`, un fallo). La anti-repetición (sección 17, #9) y el tope de trampas tempranas (#3) se miden en el pool y se informan como aviso, sin compuerta (R-Q10). Sin mecánicas con disparador ni Aguas turbias en el reto obligatorio; la tarjeta Aguas turbias · Bonus (1.1; `ui_murky_bonus_enabled` = false en 1.0, 08 §9) usa el mismo tablero con 2 cuias turbias verificadas. `daily_overrides` en Remote Config sustituye fechas concretas (fiestas, eventos) con `{fecha: {banda: id del pool}}` (R-Q6; 06 §4.1); el premio se reconcilia con la fecha del servidor del último fetch para evitar el cambio de reloj (research/02 C4.2 [V/E]).
 
 **Río sin fin** (1.2+): generación en el dispositivo por barajado inverso (`walk` con 100 ms de presupuesto; si la complejidad no alcanza el mínimo, `deep` con 10.000 nodos), K = min(12, K del nivel máximo del jugador), vacíos alternando 2/1, sin mecánicas ni turbias, par por A* ponderado (40.000 nodos, w = 2); si tres intentos no producen un tablero válido en 300 ms, se sirve del pool de reserva de 1.000 tableros (research/02 C4.3 [E]). Como el inverso garantiza solución por construcción, no hace falta solver para publicar el tablero; el solver solo mide.
 
@@ -577,7 +577,7 @@ ## Desviaciones de la dirección
 ## Preguntas abiertas para el dueño del producto
 
 1. **Cerrada** (coherencia con 02 R1 y §14): ningún nivel generado supera 14 cuias; los hitos desde el 561 juegan 13 colores + 1 vacío en 14 cuias; K = 14 solo en el evento Encuentro de las Aguas, con Cuia extra y Cuia de rescate deshabilitadas (`rescue_max_per_level` = 0 en ese modo) para no superar las 16 ranuras en pantalla. Queda por confirmar con el dueño únicamente si el evento acepta ese tablero de 15 cuias en tres filas o se juega también con 13 colores.
-2. **Cerrada** para 1.0 (revisión del 06-10): Reto del río con tres tramos por progreso (A/B/C = bandas 1-3, K 6-7 / 8-9 / 10-11) y 2.190 tableros = 3 × 730 (sección 16, 06 §4.1, 08 §4.1), en lugar de un tablero único para todos: los tramos protegen al jugador nuevo a costa de fragmentar la conversación social ("¿resolviste el de hoy?"). Queda como propuesta para 1.2 un tablero único por tramo en la Liga del río.
+2. **Cerrada** para 1.0 (revisión del 06-10): Reto del río con tres tramos por progreso (A/B/C = bandas 1-3, K 6-7 / 8 / 10-11; B2 con K 8 desde R-Q1b) y 2.187 tableros = 3 × 729 (desde R-Q2b; sección 16, 06 §4.1, 08 §4.1; D-028), en lugar de un tablero único para todos: los tramos protegen al jugador nuevo a costa de fragmentar la conversación social ("¿resolviste el de hoy?"). Queda como propuesta para 1.2 un tablero único por tramo en la Liga del río.
 3. **Cerrada** (revisión del 06-10): primer Difícil en el 40 y primer Muy difícil en el 60 (4 + 1 etiquetas en 60 niveles; `lvl_labels_from_level` = 40, `lvl_very_hard_from_level` = 60); 05 y `tools/econ_sim.py` (`FIRST_HARD_LEVEL` = 40, `FIRST_VERY_HARD_LEVEL` = 60) ya usan esta cadencia. Una cadencia más densa tipo Magic Sort (tramo 43-112 "expert") queda como hipótesis para después del soft launch.
 4. Alivio por defecto "+1 vacío" (misma imagen del tablero) frente a "−1 color" (tablero más corto): confirmar la preferencia o pedir A/B solo sobre el tipo de alivio (no sobre su existencia).
 5. **Cerrada**: la sección 9 copia literalmente la columna "Regla exacta" de 02 §9.1 y los debuts de `mech_debut_level`, con la lista única de 02 §9.1 (14 Cuia chica · 27 Cuia alta · 43 Hoja · 63 Matapi · 83 Liana con fruta · 100 Aguas turbias · 103 Remolino · 123 Barro · 143 Lluvia · 163 Brote · 183 Piraña · 323 Tinaja (M13, D-019: en la 1.0 si cumple su condición; si no, el mismo nivel en la 1.1); la Liana de cuenta (con número) es una variante, en el 243; la Cuia alta de 6, en hitos desde el 141); cualquier cambio futuro se hace primero en 02 y se propaga aquí. Con esos valores todos los bloqueadores caen en la posición 3 del ciclo, así que el ajuste de plantilla que se pedía para 34 y 106 ya no existe.
@@ -595,7 +595,7 @@ ## Dependencias con otros capítulos
 | 02 Núcleo jugable, reglas y UX | Predicados de victoria/sellado (sección 3, copiados de R5 y R12), detección de Aguas estancadas (= `movimientos_utiles(S) = ∅`, 02 R13) y `futil` única (02 §1.4), semántica de mecánicas para el solver (sección 9, copiada de 02 §9.1), tabla del tutorial 1-10 (02 §13, copiada sin alterar), límites de Aguas turbias (T2: 4 dosis / 24 realidades) | Catálogo definitivo de mecánicas (02 §9.1; D-007), niveles de debut (lista única de 02 §9.1 y `mech_debut_level`: 14 Cuia chica · 27 Cuia alta · 43 Hoja · 63 Matapi · 83 Liana con fruta · 100 Aguas turbias · 103 Remolino · 123 Barro · 143 Lluvia · 163 Brote · 183 Piraña · 323 Tinaja (M13, D-019: en la 1.0 si cumple su condición; si no, el mismo nivel en la 1.1); la Liana de cuenta (con número) es una variante, en el 243), orden de R10 por etapas y `futil` con Q-B (02 §1.2 y §1.4), T ≤ 14 generadas y layout de hasta 16 cuias en pantalla (02 R1, §5, §14) |
 | 04 Comodines, Canoas y Cuia de rescate | **Definición canónica de fallo (04 §5.2)** y `fails_level` (`restart_confirm_min_pours` = 8), presupuestos de Morpho/Pororoca/Luciérnaga, regla de Morpho en Aguas turbias (solo vertidos seguros; sin seguro → mensaje de 04 §2.3 a, sin cobro), regla "par' ≤ par + `shuffle_par_slack`", Salto del Boto (`skip_min_fails` = 5, `skip_min_gap_levels` = 10), Cuia de rescate desde `rescue_min_level` = 31 | **Cerrada**: topes de comodines en 08 §9 (Marea ilimitada en 1-10 y 3/nivel desde el 11; 1 Cuia extra y 1 Cuia de rescate por nivel) y comportamiento de Cuia extra en la variante aliviada (04 §2.2, caso b: con Cuia extra aplicada se sirve la variante −1 color, nunca +1 vacío). Pendiente: qué se sirve en ese caso cuando `relief[]` solo lleva `plus_empty` (sección 13) |
 | 05 Economía y monetización | Recompensas por etiqueta (20/60/80) y su relación con `bot_fail`; precio fijo de la Cuia de rescate (600) y del Salto del Boto (300); tasas de fallo de la sección 15 para `tools/econ_sim.py` | **Cerrada**: topes de rescate (600 semillas fijas desde el 31, primer rescate del día a 300, máximo 1 por nivel; `rescue_price_bands` = `[[31,600]]`) y metas de ARPDAU (≥ 0,015 LatAm fase 1; ≥ 0,08 US antes de UA; 05 §9) |
-| 06 Meta, retención y live-ops | Reto del río por tramos (bandas K 6-7 / 8-9 / 10-11 de la sección 16, canon) y pool fechado de 2.190 tableros, Aguas turbias · Bonus (1.1), Río sin fin, reglas de eventos sin DDA, Crecida (05 §2.4) y Hoja escudo en la señal de riesgo | Calendario de eventos y niveles especiales (Travesía, Encuentro de las Aguas) |
+| 06 Meta, retención y live-ops | Reto del río por tramos (bandas K 6-7 / 8 / 10-11 de la sección 16, canon) y pool fechado de 2.187 tableros, Aguas turbias · Bonus (1.1), Río sin fin, reglas de eventos sin DDA, Crecida (05 §2.4) y Hoja escudo en la señal de riesgo | Calendario de eventos y niveles especiales (Travesía, Encuentro de las Aguas) |
 | 07 Arte, temática y audio | Layout de 14 cuias generadas + Cuia extra + Cuia de rescate (16 en pantalla; filas de 02 §5, fijadas al cargar con las T generadas (D-006); las cuias añadidas van a la fila más corta), icono de etiqueta, chip y cinta «×3» / «×4» de la etiqueta antes de jugar (sección 12; D-009), tarjeta "El río bajó", "?" de Aguas turbias, Patrones de fruta, fondo "río genérico" en 201-700 (mundos 6-9; Loreto 141-200 por PAD; sección 17) | Restricción final de cuias por pantalla |
 | 08 Arquitectura técnica y publicación | Esquema `af-level/1` (08 §4.1 y `schemas/af-level-1.schema.json`) con la semántica de la sección 10: `attrs` `{type, key, partner, n, k}` sin `barro`, `pack_schema` 2 (D-007: tipo `matapi`, Liana con `key` o `n`, `intro_mechanic` y `par_approx` [P]), `band` = rol del slot, `D` sin límites (sección 7), `par` nulo solo en `plus_empty` y `relief` con al menos una variante desde el 31 (sección 13), `id`/`seed` del pool diario y `difficulty` opcional en `kind: daily` (sección 16); codificación `len` + (tinte − 1) (sección 3), 700 niveles instalados / 701+ en `levels_remote` (08 §6.2), claves de Remote Config (08 §9, única tabla), `AF.Core`/`AF.Solver`/`tools/level-generator`/`tools/level-qa` (08 §2.4), eventos de analítica (taxonomía única de 08 §8.2, con `af_dda_applied` y `af_label_shown`), solver en hilo de fondo, CI de reproducibilidad | Límites de tamaño y formato final de Addressables; esquema de guardado de `fails_level` y variantes |
 | 09 Roadmap, equipo, KPIs y riesgos | Plan de la sección 17 (días por tamaño de equipo), cadencia +100, debuts de la sección 9 para 09 §1.1, lectura del KPI "primer fallo real" de 09 §6.1 (sin reto suave en el 19; sección 12) | Fechas y asignación |
~~~~

### E11-05-economia.diff · `docs/gdd/05-economia-monetizacion.md` §6.1

<!-- parche: E11-05-economia.diff -->
~~~~diff
diff --git a/docs/gdd/05-economia-monetizacion.md b/docs/gdd/05-economia-monetizacion.md
index 5d38fa0..ae55145 100644
--- a/docs/gdd/05-economia-monetizacion.md
+++ b/docs/gdd/05-economia-monetizacion.md
@@ -353,7 +353,7 @@ ### 6.1 Intersticiales por rango de nivel (dirección 13)
 | 22-60 | 1 cada **3** niveles completados | 6 / 10 [P] | ≥150 s desde el anterior; ≥120 s desde el inicio de la sesión [P] | solo en la pantalla de victoria, tras la animación de recompensa, antes del botón Siguiente | el primer intersticial de la vida del jugador nunca en la misma sesión que la instalación (research/05 §Primera sesión) |
 | 61-200 | 1 cada **2** niveles | 6 / 12 [P] | ≥150 s | ídem | 2-3 niveles por dirección; cadencia observada en guías del género (research/04 §1.1 [S]) |
 | 201+ | 1 cada 2 niveles | 6 / 12 | ≥150 s | ídem | sin "cada nivel": 67 % de las reseñas negativas de Magic Sort son por anuncios (research/09 §Anuncios [V]) |
-| Siempre | — | — | — | — | nunca al inicio de nivel, nunca en reintento ni en Volver a la orilla, nunca al rendirse ni en ¿Seguro?, Nivel fallido o Sin canoas, aunque Magic Sort lo haga (D-008), nunca tras tocar "Ver video" o "Comprar", nunca a <60 s de un recompensado [P] (de esta regla depende que el avance solo tras la Cosecha doble de D-011, que pasa por AD_GATE sin tocar Siguiente, no muestre un intersticial: 02 §2 y §6); 24 h de supresión tras cualquier IAP, también `af_infinite_canoes_1h` (research/04 §Reglas [E]); **las Canoas infinitas no suprimen intersticiales** (postura del dueño del 2026-10-08, D-008, como en Magic Sort: ficha §6.5): solo los corta la regla de 24 h tras una compra; 0 para Sin anuncios; cierre habilitado a los 5 s (política Play: ≤15 s, research/04 §1.1 [V]); sin banner ni app open (dirección 13) |
+| Siempre | — | — | — | — | nunca al inicio de nivel, nunca en reintento ni en Volver a la orilla, nunca al terminar un Reto del río, que no tiene intersticial (R-Q13, D-028; 06 §4.1), nunca al rendirse ni en ¿Seguro?, Nivel fallido o Sin canoas, aunque Magic Sort lo haga (D-008), nunca tras tocar "Ver video" o "Comprar", nunca a <60 s de un recompensado [P] (de esta regla depende que el avance solo tras la Cosecha doble de D-011, que pasa por AD_GATE sin tocar Siguiente, no muestre un intersticial: 02 §2 y §6); 24 h de supresión tras cualquier IAP, también `af_infinite_canoes_1h` (research/04 §Reglas [E]); **las Canoas infinitas no suprimen intersticiales** (postura del dueño del 2026-10-08, D-008, como en Magic Sort: ficha §6.5): solo los corta la regla de 24 h tras una compra; 0 para Sin anuncios; cierre habilitado a los 5 s (política Play: ≤15 s, research/04 §1.1 [V]); sin banner ni app open (dirección 13) |
 
 ### 6.2 Anuncios recompensados (5 ubicaciones en 1.0 + Bitácora x2 en 1.1, dirección 13)
 
~~~~

### E11-06-meta.diff · `docs/gdd/06-meta-retencion-liveops.md` §4.1

<!-- parche: E11-06-meta.diff -->
~~~~diff
diff --git a/docs/gdd/06-meta-retencion-liveops.md b/docs/gdd/06-meta-retencion-liveops.md
index db82c22..455e901 100644
--- a/docs/gdd/06-meta-retencion-liveops.md
+++ b/docs/gdd/06-meta-retencion-liveops.md
@@ -110,14 +110,18 @@ ### 4.1 Reto del río (`daily_challenge`)
 | Regla | Valor |
 |---|---|
 | Disponibilidad | Un tablero por día local desde el nivel 10; jugable offline (pool embebido); sin canoas, sin Cuia de rescate, sin efecto sobre la Crecida [P] |
-| Origen del tablero | Pool offline pregenerado de 730 tableros **por banda** (3 bandas → 2.190 tableros validados contra `schemas/af-level-1.schema.json` con `kind: daily`, ≈1,4 MB minificados / ≈0,1 MB comprimidos; 03 §16, 08 §4.1 y §6.1) indexado por fecha (dirección 17; research/02 C4.2 [E], patrón Wordle [V]) |
-| Bandas (canon 03 §16, tramos A/B/C; umbrales `lvl_daily_bracket_levels` = [41, 201]) | B1: nivel máximo <41 → K 6-7, 2 vacíos, ventana de bot 40-60 % · B2: 41-200 → K 8-9, 1 vacío, 60-80 % · B3: 201+ → K 10-11, 1 vacío, ≥75 %. Sin mecánicas con disparador ni Aguas turbias en el reto obligatorio; la variante turbia solo existe en la tarjeta Bonus (1.1). El premio es igual en las tres bandas |
-| Reintentos | Ilimitados, deterministas (misma semilla), Marea y Cuia extra permitidas, Morpho permitida |
+| Origen del tablero | Pool offline pregenerado de 729 tableros **por banda** (3 bandas → 2.187 tableros validados contra `schemas/af-level-1.schema.json` con `kind: daily`, ≈1,4 MB minificados / ≈0,1 MB comprimidos; 03 §16, 08 §4.1 y §6.1) indexado por fecha (dirección 17; research/02 C4.2 [E], patrón Wordle [V]). 729 = 81 × 9 (R-Q2b, D-028): el agua (`water_base`) y el fondo de cada tablero son los del mundo de su fecha y rotan por los mundos 1-9 en todas las vueltas del pool (R-Q2; 03 §16) |
+| Bandas (canon 03 §16, tramos A/B/C; umbrales `lvl_daily_bracket_levels` = [41, 201]) | B1: nivel máximo <41 → K 6-7, 2 vacíos, ventana de bot 40-60 % · B2: 41-200 → K 8 (R-Q1b), 1 vacío, 60-80 % · B3: 201+ → K 10-11, 1 vacío, ≥75 %. Paleta: solo los tintes ya estrenados en el tramo (B1 1-40, B2 1-80, B3 1-200), con `max_pares_riesgo` 0 / 1 / libre (R-Q1 y R-Q1b; 03 §16). Sin mecánicas con disparador ni Aguas turbias en el reto obligatorio; la variante turbia solo existe en la tarjeta Bonus (1.1). El premio es igual en las tres bandas |
+| Reintentos | Ilimitados, deterministas (misma semilla) |
+| Comodines | Los de la campaña (Marea, Cuia extra, Morpho, Pororoca y Luciérnaga) salvo la Cuia de rescate y el Salto del Boto (04 §2.6 e); gastan el stock igual que en la campaña (R-Q8, D-028) |
+| Partida en curso | Manda la fecha en que se empezó: se sigue con ese tablero y se cobra ese premio aunque se gane pasada la medianoche (R-Q3). Salir sin ganar o cerrar la app la deja reanudable el mismo día; al cambiar de día se descarta (R-Q4 y R-Q16; `daily_challenge.in_progress`, 08 §4.3) |
 | Premio [P] | 60 semillas (`live_daily_challenge_reward` = {"seeds":60,"murky_bonus_mult":2}, 08 §9; 3 victorias normales; research/02 C4.2 propone "monedas de 2-3 niveles" [E]) + sello del día en el calendario del Reto; Cosecha doble disponible |
-| Sellos del mes [P] | 20 sellos en un mes calendario → marco de fondo del país del mes (cosmético); 7 Retos en días consecutivos → 1 Pororoca (research/02 C4.2 [E]) |
+| Sellos y racha | Sello del día en el calendario del mes. Cada 7 días seguidos ganados (cada racha completa: días 7, 14, 21…) → 1 Pororoca (`live_daily_streak7_reward`; research/02 C4.2 [E]); un día sin ganar rompe la racha (R-Q7). Si el jugador aún no llegó al nivel 30, que desbloquea la Pororoca (04 §2.4), entra en el stock y espera a ese nivel (R-Q14). El marco de fondo del país por 20 sellos en un mes queda fuera hasta que existan cosméticos (R-Q7) |
 | Aguas turbias · Bonus (1.1) | Desde el nivel 100 la tarjeta del Reto ofrece una segunda versión del mismo tablero con 2 cuias turbias verificadas (mismo valor que 03 §16) y premio doble (120, `murky_bonus_mult` 2); opcional, nunca sustituye al Reto normal (dirección 6). Interruptor `ui_murky_bonus_enabled` (08 §9): apagado en 1.0, encendido en 1.1 |
-| Sustituciones | `daily_overrides` en Remote Config (fiesta nacional, evento): JSON `{fecha: id_tablero}`; cabe un año en un parámetro (research/02 C4.2 [V]) |
-| Anti-reloj | Tablero siempre jugable; premio solo si `fecha_local > ultima_fecha_reclamada`; con red, si `\|fecha_local − fecha_servidor\| > 1 día`, premio "pendiente de validar" hasta reconectar (research/06 C6.1 [E]) |
+| Sustituciones | `daily_overrides` en Remote Config (fiesta nacional, evento): JSON `{fecha: {banda: id del pool}}` (R-Q6; 08 §9), con ids `d{b}_n{nnnn}` del propio pool; cabe un año en un parámetro (research/02 C4.2 [V]) |
+| Anti-reloj | Tablero siempre jugable; premio solo si `fecha_local > ultima_fecha_reclamada`; con red, si `\|fecha_local − fecha_servidor\| > 1 día`, premio "pendiente de validar" hasta reconectar (research/06 C6.1 [E]). Al reconectar, un pendiente solo se valida y se paga si `\|fecha − hoy_servidor\| ≤ 1 día`; los demás se anulan (R-Q12b). Lo cobrado sin red no se retira (R-Q12; 08 §10.1) y anular una fecha nunca retira semillas ni la Pororoca (aclaración de R-Q12) |
+| Anuncios | Sin intersticial al terminar un Reto (R-Q13; 05 §6.1) |
+| Analítica | `af_daily_challenge` (08 §8.2): `win` al reclamar el premio; `fail` una vez por día y solo si ese día termina sin victoria (R-Q9 y R-Q15) |
 
 **Pseudocódigo.**
 
@@ -125,17 +129,18 @@ ### 4.1 Reto del río (`daily_challenge`)
 function reto_de_hoy(jugador):
     banda = 1 if jugador.nivel_max < 41 else 2 if jugador.nivel_max < 201 else 3   # rc.lvl_daily_bracket_levels = [41, 201]
     fecha = fecha_local_dispositivo()
-    if rc.daily_overrides[fecha] existe: return pool_overrides[rc.daily_overrides[fecha]]
-    idx = (dias_desde(2026-01-01, fecha) + rc.live_daily_offset) mod 730
+    if rc.daily_overrides[fecha][banda] es un id del pool: return pool_diario.por_id(rc.daily_overrides[fecha][banda])   # R-Q6: {fecha: {banda: id}}
+    idx = (dias_desde(2026-01-01, fecha) + rc.live_daily_offset) mod 729   # R-Q2b: 729 = 81 × 9
     return pool_diario[banda][idx]          # tablero con semilla y estado explícito en JSON
 
-function reclamar_reto(jugador, fecha):
+function reclamar_reto(jugador, fecha):      # fecha = la del día en que se empezó la partida (R-Q3)
     if fecha <= jugador.ultima_fecha_reclamada: return SIN_PREMIO   # reloj atrasado o ya reclamado
     estado = PENDIENTE if (online and abs(fecha - fecha_servidor) > 1) else CONFIRMADO
+    jugador.racha = jugador.racha + 1 if jugador.ultima_fecha_reclamada == fecha - 1 else 1   # un día sin ganar la rompe (R-Q7)
     conceder(rc.live_daily_challenge_reward.seeds, sello(fecha), estado)      # 60
     jugador.ultima_fecha_reclamada = fecha
-    if sellos_consecutivos(jugador) == 7: conceder(rc.live_daily_streak7_reward)   # 1 Pororoca
-    if sellos_del_mes(jugador) == 20: conceder(marco_pais_del_mes)
+    if jugador.racha mod 7 == 0: conceder(rc.live_daily_streak7_reward)   # 1 Pororoca en cada racha completa (R-Q7); antes del 30, al stock (R-Q14)
+    # el marco del país por 20 sellos del mes queda fuera hasta que existan cosméticos (R-Q7)
 ```
 
 **Pantalla.** Tarjeta en el lobby: "Reto del río · {fecha corta}", icono de la banda (una, dos o tres olas), estado (Jugar / Sellado), racha de días "{n}/7". Al abrir: tablero con el HUD normal; cabecera "Reto del río"; sin barra de Crecida. Al ganar: animación del sello cayendo sobre el calendario del mes (cuadrícula de 28-31), +60, botón Cosecha doble, y si procede la tarjeta "Aguas turbias · Bonus" con premio ×2.
@@ -525,7 +530,7 @@ ## Desviaciones de la dirección
 ## Preguntas abiertas para el dueño del producto
 
 1. **Definición de fallo para la Crecida**. Cerrada: tabla canónica de 04 §5.2. Solo el fallo duro (Volver a la orilla desde Aguas estancadas) rompe la Crecida, salvo Hoja escudo; el reinicio voluntario con ≥8 vertidos es fallo blando (cuenta para `fails_level`, alivio y Salto del Boto, pero no gasta canoa ni rompe la Crecida); salir al Mapa no es fallo. `streak_restart_counts_as_fail` queda en false (08 §9) y deja de ser una elección de diseño pendiente. *Matizada por D-008 (2026-10-08): desde el 31, rendirse y salir del nivel con el menú son fallo duro y rompen la Crecida salvo Hoja escudo; cerrar la app sigue sin ser fallo.*
-2. **Reto del río por bandas**. Cerrada: tres bandas según 03 §16 (K 6-7 / 8-9 / 10-11; 730 × 3 = 2.190 tableros; umbrales `lvl_daily_bracket_levels` = [41, 201]). El tablero único para la conversación social se reconsideraría, si acaso, con las Aldeas de 2.0.
+2. **Reto del río por bandas**. Cerrada: tres bandas según 03 §16 (K 6-7 / 8 / 10-11, con B2 en K 8 desde R-Q1b; 729 × 3 = 2.187 tableros desde R-Q2b, D-028; umbrales `lvl_daily_bracket_levels` = [41, 201]). El tablero único para la conversación social se reconsideraría, si acaso, con las Aldeas de 2.0.
 3. **Regalos de tutorial**. Cerrada: se mantienen como ítems de inventario, 1 Cuia extra regalada en el nivel 8 y 3 Morphos en el 12 (02 §13, 04 §2.2-2.3; excepción de tutorial a la dirección 9 documentada en 04 §Desviaciones y aquí en Desviaciones 3).
 4. **Canasta grande** (día 30 de la Bitácora y cada 100 niveles): confirmar nombre y contenido propuesto (150 semillas + 1 Cuia extra + 1 Morpho + 3 Marea) y añadirlo al glosario.
 5. **Mochila de regreso** 1,99 (oferta de regreso, v1.1): confirmar que entra al catálogo y fijar su nombre en el glosario.
@@ -545,7 +550,7 @@ ## Dependencias con otros capítulos
 | 00 Glosario | Todos los nombres usados | Alta de Canasta grande y Mochila de regreso si se aprueban; numeración canónica de capítulos |
 | 01 Visión y posicionamiento | Calendario de desbloqueos (3), primera sesión (2) y metas de retención (12) | Metas de negocio y fases de mercado (01 §7-8); nombre comercial y riesgos de marca (01 §9) |
 | 02 Núcleo, reglas y UX | Desbloqueos de meta sobre los niveles 1-12 (Cuia extra regalada en el 8, Morpho en el 12, secuencia del nivel 10, pre-prompt de push en la 2.ª sesión y no en el nivel 10) | Tabla canónica de los niveles 1-12 (02 §13: excepciones de 1 vacío en 1 y 6, Marea en el 3, presentación del Reto del río en el 7, Volver a la orilla en el 6/8, Mochila de inicio con `eco_starter_first_show`), copiada sin alterar en 2.2; debuts de mecánicas (lista única de 02 §9.1 y `mech_debut_level`: 14 Cuia chica · 27 Cuia alta · 43 Hoja · 63 Matapi · 83 Liana con fruta · 100 Aguas turbias · 103 Remolino · 123 Barro · 143 Lluvia · 163 Brote · 183 Piraña · 323 Tinaja (M13, D-019: en la 1.0 si cumple su condición; si no, el mismo nivel en la 1.1); la Liana de cuenta (con número) es una variante, en el 243; D-007) y capacidades (02 §8: Cuia chica 14, Cuia alta de 5 desde el 27 y de 6 solo en hitos desde el 141) para el calendario de la sección 3 y las reglas alteradas de Travesía; pantalla de victoria sin auto-avance (`victory_autonext_s` = 0, 02 §6) |
-| 03 Niveles, generación y dificultad | Especificación del pool del Reto del río (730 × 3 bandas = 2.190 tableros), paquetes de Travesía y Encuentro de las Aguas; fallo de bot objetivo por nivel en 1-12 (2.2; par y aritmética de cuias, colores y vacíos según 02 §13) | Pipeline que produce los pools fechados; bandas del Reto (03 §16: K 6-7 / 8-9 / 10-11, `lvl_daily_bracket_levels` = [41, 201]); posiciones de Difícil/Muy difícil/hito del ciclo de D-009 (retos p6, p9, p12, p15 y p18, remanso p19, hito p20); primer Difícil 40 (`lvl_labels_from_level`) y primer Muy difícil 60 (`lvl_very_hard_from_level`), 03 §12 |
+| 03 Niveles, generación y dificultad | Especificación del pool del Reto del río (729 × 3 bandas = 2.187 tableros), paquetes de Travesía y Encuentro de las Aguas; fallo de bot objetivo por nivel en 1-12 (2.2; par y aritmética de cuias, colores y vacíos según 02 §13) | Pipeline que produce los pools fechados; bandas del Reto (03 §16: K 6-7 / 8 / 10-11, `lvl_daily_bracket_levels` = [41, 201]); posiciones de Difícil/Muy difícil/hito del ciclo de D-009 (retos p6, p9, p12, p15 y p18, remanso p19, hito p20); primer Difícil 40 (`lvl_labels_from_level`) y primer Muy difícil 60 (`lvl_very_hard_from_level`), 03 §12 |
 | 04 Comodines, vidas y rescate | Regalos de tutorial, comodines de Bitácora y Canasta, topes de la Crecida, conversión de comodines bloqueados | Definición canónica de fallo (04 §5.2: fallo duro / fallo blando ≥8 vertidos / cerrar la app no es fallo; desde el 31, rendirse y salir del nivel con el menú sí, D-008); Marea ilimitada en 1-10 (`undo_unlimited_until_level` = 10) y 3 por nivel desde el 11; Cuia de rescate desde el 31 (`rescue_min_level`), máximo 1 por nivel; precios finales, topes por día y reglas de inventario (persistencia de Cuia extra al reiniciar) |
 | 05 Economía y monetización | Ventanas de Mochila de inicio, Sin anuncios, Cántaro, Balsa de rescate, Pase Expedición, Mochila de regreso; placement "cofre diario" (1.1); Canoas infinitas (`af_infinite_canoes_1h`) como premio de evento | Escalera de la Crecida (05 §2.4, fila única; `streak_milestones`, `streak_hits_once_per_day`, `streak_daily_cap`); Cuia de rescate a 600 fijo (`rescue_price_bands` = [[31,600]]; primer rescate del día 300); catálogo de SKUs y contenidos (05 §4-5); reglas de intersticiales y topes rewarded (05 §6) y política de supresión tras IAP; metas de ARPDAU (05 §9); textos y bases legales del consentimiento por país (05 §7) |
 | 07 Arte, temática y audio | Pantallas de Bitácora, Mapa del río, Crecida (medidor, hoja), Fruta nueva, Canasta visible, marcos de país, skin de temporada, Cuia del Encuentro dorada; animaciones de TiTino (victoria, Nivel fallido, inicio y Mapa), Capi (regalo, rescate y canoa del Mapa) y Rosa (Mapa); fondos por parada y piezas del Mapa del río (D-013; 07 §2.1 y §11); literal de la toada amazónica sin referencia a Parintins (10 = 07 §2.2) | HUD del nivel (07 §7.4, dueño del HUD: barra vertical de la Crecida en el borde derecho, replicada en grande en la victoria); hojas de estilo; LUT de variantes de luz y lluvia; textos verificados de los Datos del río con el asesor cultural |
~~~~

### E11-08-arquitectura.diff · `docs/gdd/08-arquitectura-tecnica-publicacion.md` §3, §4.1, §4.3, §6, §8.2 y §9

<!-- parche: E11-08-arquitectura.diff -->
~~~~diff
diff --git a/docs/gdd/08-arquitectura-tecnica-publicacion.md b/docs/gdd/08-arquitectura-tecnica-publicacion.md
index f73b329..df12649 100644
--- a/docs/gdd/08-arquitectura-tecnica-publicacion.md
+++ b/docs/gdd/08-arquitectura-tecnica-publicacion.md
@@ -132,7 +132,7 @@ ## 3. Estructura de carpetas del proyecto Unity
                          # AF.Core y AF.Solver ya no viven aquí: están en Packages/com.amazonfill.core
       Data/
         Levels/      mundo_01_v1.json … mundo_09_v1.json (Addressables, grupo levels_install; mundo_10+ en levels_remote)
-        Daily/       daily_pool_v1.json (2.190 tableros = 3 bandas × 730)
+        Daily/       daily_pool_v1.json (2.187 tableros = 3 bandas × 729; lo genera tools/level-generator --daily: no se edita a mano)
         Palette/     tints.json (14 tintes, HEX, patrón, debut, pares de riesgo)
         Map/         river_map_v1.json (af-river-map/1) y river_stops_v1.json (af-river-stops/1): Mapa del río (D-013; sección 4.5)
         Config/      RemoteConfigDefaults.json, iap_catalog.json, ad_placements.json
@@ -226,7 +226,7 @@ ### 4.1 Paquete de niveles (propiedad del capítulo 3; este capítulo fija la si
 |---|---|---|
 | `schema` | string | `af-level/1` en cada nivel; `pack_schema` (int) versiona la estructura del pack: **1** = formato original; **2** = mecánicas de D-007 (contrato 21): tipo `matapi`, Liana con fruta (`key`) o de cuenta (`n`), uno y solo uno, campos obligatorios por tipo (hoja: `key`; remolino: `partner`; lluvia: `key` y `n`), `intro_mechanic` `matapi`, `chica`, `alta` y `murky` [P] y `difficulty.par_approx` [P]; **3** = el 2 más la Tinaja (D-019) y las variantes de la 1.0 con la firma del 2026-10-14 (D-023, punto 5; filas `attrs` e `intro_mechanic`) y la máscara de la tinaja en `relief` (D-023, punto 6). Con `pack_schema` 1 todo valida como antes. Los packs de la campaña v1 (11-200) usan el 2; el ejemplo de esta sección sigue en el 1 y valida igual (`python tools/validate_schema.py`) |
 | `schema_file` (cabecera) | string | `schemas/af-level-1.schema.json`: ruta (desde la raíz del repositorio) del JSON Schema draft 2020-12 canónico contra el que se validó el pack |
-| `kind` (cabecera) | enum | `campaign` (por defecto) / `daily` / `event`; en `daily` (pack `daily_pool_v{n}`, 03 §16) cada tablero lleva `slot: {band 1-3, day 0-729}` en lugar de `level`, `id` y `seed` de formato diario, y `difficulty`, `world` y `lap` opcionales (el pool se publica sin métricas y con `relief` = `[]`); la cabecera `daily` no exige `world`, `world_id`, `lap` ni `water_base` (el pool mezcla mundos; cada tablero declara su `water_base`) |
+| `kind` (cabecera) | enum | `campaign` (por defecto) / `daily` / `event`; en `daily` (pack `daily_pool_v{n}`, 03 §16) cada tablero lleva `slot: {band 1-3, day 0-728}` en lugar de `level` (729 días por banda desde R-Q2b, D-028; el esquema admite hasta el 729), `id` y `seed` de formato diario, y `difficulty`, `world` y `lap` opcionales (el pool se publica sin métricas y con `relief` = `[]`); la cabecera `daily` no exige `world`, `world_id`, `lap` ni `water_base` (el pool mezcla mundos; cada tablero declara su `water_base`, la del mundo de su fecha: 1 + (día mod 9), R-Q2 y R-Q2b; 03 §16) |
 | `id` | string | Campaña y eventos: `m{mm}_v{v}_n{nnnn}` con el mismo padding que la semilla (`m03_v1_n0043`); Reto del río: `d{b}_n{nnnn}` (patrón `^d[1-3]_n[0-9]{4}$`, p. ej. `d2_n0417`). Las variantes aliviadas no tienen id propio: viven en `relief` y se identifican en analítica como `variant ∈ {base, plus_empty, minus_color}` |
 | `seed` | string | Campaña y eventos: `amazonfill:v1:m{mm}:v{v}:n{nnnn}` con ceros a la izquierda; Reto del río: `amazonfill:v1:daily:b{b}:d{ddd}` (patrón `^amazonfill:v1:daily:b[1-3]:d[0-9]{3}$`, p. ej. `amazonfill:v1:daily:b2:d417`); reproduce el tablero `base` en `tools/level-generator` (prueba de determinismo por hash, dirección 7) |
 | `level`, `world`, `lap` | int | Nivel de ruta, mundo 1-13, vuelta (capítulo 3 §10); obligatorios en campaña y eventos. En `daily` no hay `level` (lo sustituye `slot`) y `world` / `lap` son opcionales |
@@ -269,7 +269,9 @@ ### 4.3 Perfil de jugador
                 "content_version": "1.0.0", "map_seen_level": 80 },
   "streak": { "wins": 4, "shield_available": false, "river_full": false },
   "calendar": { "month": "2026-10", "claimed_days": [1,2,3,4,5], "last_claim_date": "2026-10-05" },
-  "daily_challenge": { "last_date": "2026-10-05", "result": "win", "streak_days": 6 },
+  "daily_challenge": { "last_date": "2026-10-05", "result": "win", "streak_days": 6,
+                       "last_claimed_date": "2026-10-05", "pending": [], "offline_claims": [],
+                       "stamps": { "month": "2026-10", "days": [1,2,4,5] }, "in_progress": null },
   "piggy": { "seeds": 1230, "cap": 3000 },
   "entitlements": { "no_ads": false, "starter_bought": false, "pass_until": null, "skins": ["cuia_vidrio"] },
   "settings": { "sound": true, "music": true, "haptics": true, "patterns": "auto", "language": "auto",
@@ -285,6 +287,8 @@ ### 4.3 Perfil de jugador
 
 Reglas: blob <100 KB (research/06 §Implicaciones 6 [E]) frente a 3 MB de Play Games Saved Games [V]; nunca se guardan estadísticas por nivel (van a analítica); `economy_ts` se actualiza en cada mutación de `wallet`/`stock`/`daily`; al pagar el hito 10 de la Crecida `streak.wins` vuelve a 0 y `streak.river_full` (río crecido, sin plazo de reloj) sigue en true hasta el siguiente fallo duro no escudado, también con `streak.wins` = 0 (05 §2.4); `hmac` con clave derivada del dispositivo disuade la edición manual del archivo (no es seguridad real: sección 10.6). **`progress.map_seen_level`** (D-013, 2026-10-13): entero, 0 por defecto, el último nivel cuyo estado del Mapa del río ya vio el jugador; se escribe al cerrar el Mapa y tras la franja de llegada de la victoria, y en la fusión de la sección 11 gana el máximo, como `max_level`. Con él, el icono del inicio lleva un punto si hay una llegada sin ver y el Mapa anima exactamente lo pendiente al abrirse. **No exige migrar el perfil:** el esquema aún no se ha publicado y un campo nuevo con un default razonable se añade sin migración (`Profile.cs`, `CurrentSchema` = 1). Todo lo demás del Mapa (posición, llenado, muestras, miradores, afluentes y banderas) se deduce de `max_level`, `skipped_levels`, `WorldTable` y el catálogo de paradas. **Con D-018 y D-016 (2026-10-13):** `settings.language` pasa a `"auto"` por defecto (seguir al teléfono; la elección del selector de Ajustes manda sobre el teléfono), `settings.sloth_messages` (bool, `true`: el ajuste «Mensajes de TiTino»; apagado, solo gestos) y `sloth.last_line` [P] (8 enteros pequeños, el índice de la última frase de TiTino de cada momento, para no repetirla). Van **en la misma migración** a `profile_schema` 2 (sección 4.4), con el avistamiento de fauna si se aprueba (Q11 del guion de la intro); mientras el esquema no se publique, se añaden con su default sin migrar, como `map_seen_level`. Frutas del río (D-017) no añade nada: lo revelado se deduce de `max_level` y `skipped_levels`.
 
+**`daily_challenge`** (Reto del río; R-Q4, R-Q5 y R-Q16, D-028): `last_date`, `result` (los valores de `af_daily_challenge`, sección 8.2) y `streak_days` (la racha de días seguidos ganados, 06 §4.1), más `last_claimed_date` (la última fecha cobrada: el premio solo se da con una fecha posterior, 06 §4.1), `pending` (los premios pendientes de validar), `offline_claims` (lo cobrado sin red; solo lo usa la rama (b) de R-Q12, que no es la decidida), `stamps` (`{month, days}`: los sellos del mes en curso) e `in_progress` (la partida del Reto empezada hoy, o null; sigue tras salir sin ganar y se descarta al cambiar de día). `pending` y `offline_claims` son listas de `{date, streak}`, con la racha de esa fecha para saber si cerraba una de 7 [P]; `in_progress` es `{date, band, id, level_state}`, con su propio `level_state` (el de 04 §10.1 sigue siendo de la campaña) [P]. El registro y la cartera van en el mismo perfil, para no pagar dos veces la misma fecha. Sin migración, como `map_seen_level`.
+
 ### 4.4 Versionado y migraciones
 
 | Artefacto | Clave de versión | Regla |
@@ -475,7 +479,7 @@ ### 6.1 Presupuesto de descarga inicial [P]
 | Fondos mundos 1-4 (install-time) | ≈4-6 MB | 4 pinturas 1080×2400 ASTC 6×6 + variantes LUT (research/02 C3.4 [E]). **Con D-013, fondos por parada:** 4 kits y 10 héroes de los mundos 1-4 ocupan ≈ 1,7-2,5 MiB (kits en ASTC 8×8 y héroes en 6×6) [cálculo], porque el cielo pasa a hacerse por código; el margen paga el muelle y los dos juegos de follaje (≈ 1,5 MiB). Loreto suma ≈ 0,6-0,8 MiB, y los 4 kits de 201-700, ≈ 1,0-1,8 MiB; mientras Addressables no esté instalado, todo va en el AAB |
 | Mapa del río (D-013) | ≈1,7 MiB | 5 texturas de terreno de 512², el atlas de UI del Mapa (1024², ASTC 4×4, con las siluetas de las muestras horneadas) y los dos JSON (sección 4.5). Install-time. Se mide con `ci/size-gate.ps1` tras integrar los 3 primeros sitios: impacto neto frente al canon, entre −1 y +3 MB [cálculo] |
 | Audio (4 loops + ambientes + SFX) | ≈7 MB; **medido: 16,91 MB** | Medido el 2026-10-17 (AU3) con `AF.EditorTools.AudioAssetSetup.BuildGameAab`, un AAB de release con la escena del juego: los 173 clips de la entrega 1 suman 16,91 MB en un AAB de 51,51 MB (sin SDK ni Addressables). La música pesa 11,54 MB (9 lechos con 3 capas y su variante nocturna, unos 50 min en mono), el agua y la lluvia 4,71 y los efectos, las notas, el chorro, la fauna y los remates 0,66 (desglose en `docs/audio/lista-de-sonidos.md`, sección 9). Supera la estimación, pero cabe en la compuerta. **El AAB de humo (`BuildSmoke`) no lleva el audio** (usa una escena mínima): la compuerta de tamaño del CI debe medir un AAB con la escena del juego (pendiente de B2 y del CI, s30) |
-| Niveles 1-700 (mundos 1-9) + Reto del río 2.190 (3 bandas × 730) + `tints.json` | ≈0,2 MB comprimidos (≈2,5 MB sin comprimir) | sección 4.1; los mundos 10-13 y los paquetes nuevos van por `levels_remote` |
+| Niveles 1-700 (mundos 1-9) + Reto del río 2.187 (3 bandas × 729) + `tints.json` | ≈0,2 MB comprimidos (≈2,5 MB sin comprimir) | sección 4.1; los mundos 10-13 y los paquetes nuevos van por `levels_remote` |
 | Fuentes (latino extendido, 2 familias) | ≈1,5 MB | TextMeshPro atlas. Con D-018, `FontSetup` calcula los glifos desde las tablas de textos (sección 4.7). Para la tanda 2 de idiomas: latino ampliado y cirílico (< 1-2 MB más, en el AAB; títulos rusos con Nunito ExtraBold) y un pack de PAD `fonts_cjk` fast-follow con Noto Sans JP y KR en atlas recortados (2-5 MB por idioma), cada fuente nueva con el visto bueno del dueño; cada pack nuevo entra en esta tabla y en la compuerta del CI |
 | **Total base + install-time** | **≈65-76 MB** | Compuerta de CI: falla si `bundletool get-size total` > 80 MB |
 
@@ -483,7 +487,7 @@ ### 6.2 Grupos de Addressables y asset packs
 
 | Grupo Addressables | Contenido | Modo PAD | Tamaño [P] |
 |---|---|---|---|
-| `levels_install` | `mundo_01…09_v1.json` (niveles 1-700: cubre los ≥600 jugables de la dirección 17; el mundo 9 termina en el 700), `daily_pool_v1.json` (2.190 tableros = 3 bandas × 730), `tints.json` | install-time | ≈0,2 MB comprimidos |
+| `levels_install` | `mundo_01…09_v1.json` (niveles 1-700: cubre los ≥600 jugables de la dirección 17; el mundo 9 termina en el 700), `daily_pool_v1.json` (2.187 tableros = 3 bandas × 729), `tints.json` | install-time | ≈0,2 MB comprimidos |
 | `core_install` | UI, cuias, frutas, mascotas, audio, fondos w01-w04 (con D-013, sus kits y héroes por parada), el Mapa del río y el fondo "río genérico" | install-time | ≈22 MB |
 | `world_w05` … `world_w13` | Fondo + skin + variantes LUT + ambiente del mundo | **on-demand**, pedido al entrar en el último tercio del mundo anterior (p. ej. w05 se pide en el nivel 121). **En 1.0 viaja `world_w05`** (Loreto, 141-200). Con D-013 (2026-10-13; Q14-A), los niveles 201-700 (mundos 6-9) llevan el kit de su mundo (`bg_kit_w06` … `bg_kit_w09`) con el rótulo del sitio, sin héroe: en `world_w06` … `world_w09` cuando haya Addressables y, mientras no esté instalado, dentro del AAB (6.1). El fondo "río genérico" con la LUT de agua del mundo queda de respaldo mientras falte el kit o el pack (glosario §8; 03 §17; 06 §10; 07 §2.1); los héroes y las skins de esos mundos llegan con sus packs, de la 1.1 en adelante | 2-4 MB cada uno |
 | `events_*` | Arte de Encuentro de las Aguas, Temporada de lluvias, Travesía (1.2) | on-demand | 2-3 MB |
@@ -532,7 +536,7 @@ ### 8.2 Tabla de eventos
 | `af_push_prompt` | `step ∈ {pre, native}`, `result` | Primera victoria de la 2.ª sesión con el nivel 10 superado (`push_preprompt_session` / `push_preprompt_min_level`; 06 §8.1) | Opt-in push (cap. 06 §8.1; benchmark Android 69 %, research/05 §5 [V]) |
 | `af_push_sent` / `af_push_open` | `type` (tipo del catálogo de 06 §8.3) | Entrega de la notificación (1.0: notificación local con estado entregado, registrada en la siguiente apertura; 1.2: envío por FCM) / apertura de la app desde la notificación | CTR por tipo ≥1 % en Android (cap. 06 §8.2 y §12) |
 | `af_asset_pack` | `pack`, `state ∈ {requested, downloading, completed, failed, fallback_used}`, `bytes`, `ms` | PAD | Fallos de descarga por país; uso del fondo genérico |
-| `level_start` (Firebase) | `level_name` = `id` del nivel de §4.1 (`m{mm}_v{v}_n{nnnn}`; en el Reto del río, `d{b}_n{nnnn}`), `variant ∈ {base, plus_empty, minus_color}`, `band` | Al terminar Llenar y teñir | Embudo por nivel (cap. 3) |
+| `level_start` (Firebase) | `level_name` = `id` del nivel de §4.1 (`m{mm}_v{v}_n{nnnn}`; en el Reto del río, `d{b}_n{nnnn}`), `variant ∈ {base, plus_empty, minus_color}`, `band` | Al terminar Llenar y teñir (en el Reto del río, también al reanudar una partida sin intro; una vez por partida [P]) | Embudo por nivel (cap. 3) |
 | `af_level_win` | `pours`, `par`, `pours_over_par`, `undo_used`, `boosters_json`, `t_ms`, `reward_seeds`, `double_harvest (bool)`, `rescued (bool)` | Victoria | Intentos medios/medianos, tiempo de paso (cap. 3); ingreso por victoria (cap. 5) |
 | `af_level_fail` | `kind ∈ {hard, soft}`, `canoe_lost`, `streak_broken`, `shield_used`, `relief_served`, `pours`, `t_ms` | Fallo (cap. 4 §5.2) | Curva real de fallos; disparo de "El río bajó" |
 | `af_level_abandon` | `pours`, `t_ms`, `reason ∈ {map, app_background, crash_resume}` | Salida sin resolver que no es fallo. Desde el 31, «Salir» con el menú es fallo duro y se registra como `af_level_fail` (04 §5.2; D-008) | Time to abandon (research/02 §6 [V]) |
@@ -553,7 +557,7 @@ ### 8.2 Tabla de eventos
 | `af_basket_open` | `level`, `contents_json` | Canasta cada 10 niveles | Valor entregado (cap. 5) |
 | `af_calendar_claim` | `day`, `reward_type`, `reward_value`, `days_missed_before` | Bitácora del río | Reclamo ≥70 % de DAU (cap. 1 §3.1) |
 | `af_piggy` | `state ∈ {unlocked, shown_full, bought}`, `seeds_inside` | Cántaro | Conversión de alcancía (cap. 05) |
-| `af_daily_challenge` | `date`, `result ∈ {win, fail, abandon}`, `t_ms`, `murky_bonus (bool)`, `streak_days` | Reto del río | Participación diaria (cap. 06) |
+| `af_daily_challenge` | `date`, `result ∈ {win, fail, abandon}`, `t_ms`, `murky_bonus (bool)`, `streak_days` | Reto del río: `win` al reclamar el premio; `fail` una vez por día y solo si ese día termina sin victoria (R-Q9 y R-Q15, D-028). En la 1.0 no se emite `abandon`: salir sin ganar deja la partida reanudable el mismo día (R-Q16) | Participación diaria (cap. 06) |
 | `af_river_fact_shown` | `fact_id`, `dismissed_ms` | Dato del río | Lectura del contenido educativo. Con D-013, el `fact_id` del dato de llegada es su clave de contenido (`map.stop.<lugar>.dato_NN`, `map.country.<iso2>.dato_NN`, `map.myth.<tema>`; glosario §10); el de la línea de fruta (Frutas del río, D-017) es `<sujeto>.<kk>` (`acai.03`, o `rio_napo.03` en una serie de río [P]; sección 4.6); `dismissed_ms` es el tiempo hasta el toque en «Siguiente» (lectura probable ≥ 4.000 ms en una línea de ≈ 70 caracteres [P]) |
 | `af_fact_page_open` | `tint_id`, `unlocked` | Apertura de la ficha de una fruta, desde la línea de la victoria o desde Ajustes › «Sobre la Amazonía» (D-017, Q4-A, aprobado el 2026-10-13) | Aperturas de la ficha de Frutas del río |
 | `af_map_open` [P] | `source ∈ {l10, world_change, lobby, pause, victory, content_end}`, `stop`, `world` | Apertura del Mapa del río (D-013) | Uso del Mapa; A/B de `ui_map_mode` (D7 y D30 de la cohorte `stops` frente a `worlds`) |
@@ -653,7 +657,7 @@ ## 9. Parámetros Remote Config consolidados
 | `audio_music_percussion_layers` / `audio_ambient_rain_scale` | int / float | 2 / 1.0 | 0-3 / 0-2 | 07 | Capas que entran al sellar cuias; multiplicador de lluvia ambiental |
 | `level_overrides` | json `{slot: level_id}` | `{}` | — | 03 | Sustituir o reordenar niveles sin build (research/02 §7.3 [E]); también por segmento (DDA) |
 | `content_version` | string | "1.0.0" | — | 03 | Catálogo de paquetes publicado en `levels_remote` (sección 6.2); si difiere del local, el cliente descarga y muestra "nuevos niveles" |
-| `daily_overrides` | json `{fecha: level_id}` | `{}` | — | 03 | Sustituciones fechadas del Reto del río (research/02 Complemento 4.2 [V]) |
+| `daily_overrides` | json `{fecha: {banda: id}}` | `{}` | — | 03 | Sustituciones fechadas del Reto del río, por banda y con ids `d{b}_n{nnnn}` del propio pool (R-Q6, D-028; research/02 Complemento 4.2 [V]) |
 | `lvl_daily_bracket_levels` | json | `[41, 201]` | — | 03 | Umbrales de las 3 bandas del Reto del río |
 | `relief_after_fails` / `relief_mode` / `relief_second_step_fails` | int / enum / int | 3 / `auto` / 0 (off) | 2-4 / `auto` · `plus_empty` · `minus_color` / 0, 5-8 | 03 | Alivio determinista "El río bajó" (dirección 7; 03 §13); `relief_mode` ordena `relief[]` en el generador |
 | `lvl_labels_from_level` / `lvl_very_hard_from_level` | int | 40 / 60 | — | 03 | Primer nivel que puede mostrar Difícil / Muy difícil |
@@ -708,7 +712,7 @@ ## 9. Parámetros Remote Config consolidados
 | `live_unlock_level_daily` | int | 10 | 8-12 | 06 | Desbloquea Reto del río, Bitácora, Mapa del río, Canasta y Mercado |
 | `live_unlock_level_league` / `_trail` / `_regatta` / `_meeting` / `_village` | int | 41 / 41 / 51 / 61 / 31 | — | 06 | Desbloqueos de 1.2 y 2.0 |
 | `live_daily_challenge_reward` | json | `{"seeds":60,"murky_bonus_mult":2}` | seeds 40-80 | 06 | Premio del Reto del río; el bonus Aguas turbias paga el doble |
-| `live_daily_offset` / `live_daily_streak7_reward` | int / string | 0 / "shuffle" | — | 06 | Desplazamiento del índice por fecha; premio por 7 Retos seguidos |
+| `live_daily_offset` / `live_daily_streak7_reward` | int / string | 0 / "shuffle" | — | 06 | Desplazamiento del índice por fecha (mod 729; con cualquier valor el mundo del agua y del fondo sigue a la fecha, y si no es múltiplo de 9 queda desplazado por él: R-Q2b); premio por cada 7 Retos seguidos ganados (R-Q7) |
 | `live_events_schedule` / `live_event_active` | json | `[]` / `{}` | — | 08 / 06 | Calendario de eventos 1.2 (`start/end` UTC, `type`, `pack`) y definición del evento activo |
 | `live_comeback_gift` / `live_comeback_offer_enabled` | json / bool | `{"days":7,"seeds":200,"extra_cuia":1,"refill_canoes":true}` / false (1.1: true) | — | 06 | Regalo de regreso y Mochila de regreso |
 | `live_content_end_message` | json | `{"next_batch_eta_days":14}` | 7-21 | 06 | Pantalla "El río sigue creciendo" y push local "Nuevos niveles" (sección 6.2) |
@@ -1086,7 +1090,7 @@ ## Dependencias con otros capítulos
 | 00 Glosario | IDs de código (§11), nombres de las claves `ui_*`, nombre interno Amazon Fill y sus formas (`amazon-fill`, `amazonfill`, `AF.`, `af_`), agua base del shader, HEX de tintes | Convención de archivos y Addressables (sección 3), prefijo `af_` en analítica, `applicationId` `com.<estudio>.tintalu`; defaults de `ui_*` en la tabla única (sección 9) |
 | 01 Visión | Claves `biz_*`, fases de mercado, umbrales de nota y crash para la fase 0 | Umbrales técnicos de la beta cerrada (crash-free ≥99,5 %, AAB ≤80 MB, primera interacción <6 s), coste de servicios por versión (research/06 C6.3 [E]: 0-10 USD/mes en 1.0, 15-40 en 1.2) |
 | 02 Núcleo y reglas | Catálogo exacto de mecánicas y su serialización (`attrs`), máximo de cuias en pantalla, flags `skippable` / `hint_supported`, fases y valores de Llenar y teñir (§4.2) y del vertido (§4.1) | Interfaces `IMechanic`, representación de `BoardState` (len + tinte−1), esquema `af-level/1` y `schemas/af-level-1.schema.json` (sección 4.1, a la que remite 02 §9.3; `attrs` `{type, key, partner, n, k}` sin `barro`, con `matapi` y `pack_schema` 2 desde D-007; ejemplo canónico: nivel 43, debut de Hoja, según la regla transversal 2 de 02 §9.2), claves `anim_*` y `victory_autonext_s` de la tabla única (sección 9), canon de entrega de niveles para el caso borde "Nivel N+1 no listo" (02 §14: los primeros 700 niveles van en el AAB), presupuesto de rendimiento con 16 cuias |
-| 03 Niveles, generación y dificultad | Campos de nivel de 03 §10 integrados en `af-level/1`, métricas `bot_fail/branching/traps/D`, variantes `relief[]`, pool del Reto del río (3 bandas × 730; bandas K 6-7 / 8-9 / 10-11 de 03 §16; `id` `d{b}_n{nnnn}`, `seed` `amazonfill:v1:daily:b{b}:d{ddd}`, sin métricas y con `relief` = `[]`), regla de orden de `relief` (03 §13), primer Difícil 40 / Muy difícil 60 (03 §11/§12) | `tools/level-generator` y `tools/level-qa` como compuerta de CI (nombres canónicos: ensamblados `AF.Core` / `AF.Solver`, carpeta `tools/level-generator`; criterio de alivio de `tools/level-qa` en 4.1: `bot_fail` baja ≥ 0,20 si el base parte de ≥ 0,20; con base < 0,20, como los debuts de 02 §9.2, basta `par` menor en `minus_color` o el vacío extra de `plus_empty`, 03 §17 #8); JSON Schema canónico `schemas/af-level-1.schema.json` (única copia, a la que remiten 03 §10 y §17 #11; `D` sin límites, `par` nulo solo en `plus_empty`, `relief` obligatoria desde el 31, formato diario; 03 §3 usa la codificación len + tinte−1); claves `lvl_*` (incl. `lvl_murky_max_hidden_units` = 4 y `lvl_dda_enabled` = false), `relief_*`, `level_overrides`, `daily_overrides` y `content_version` de la tabla única (sección 9); entrega 1-700 instalados / 701+ remotos (03 §17) |
+| 03 Niveles, generación y dificultad | Campos de nivel de 03 §10 integrados en `af-level/1`, métricas `bot_fail/branching/traps/D`, variantes `relief[]`, pool del Reto del río (3 bandas × 729; bandas K 6-7 / 8 / 10-11 de 03 §16; `id` `d{b}_n{nnnn}`, `seed` `amazonfill:v1:daily:b{b}:d{ddd}`, sin métricas y con `relief` = `[]`), regla de orden de `relief` (03 §13), primer Difícil 40 / Muy difícil 60 (03 §11/§12) | `tools/level-generator` y `tools/level-qa` como compuerta de CI (nombres canónicos: ensamblados `AF.Core` / `AF.Solver`, carpeta `tools/level-generator`; criterio de alivio de `tools/level-qa` en 4.1: `bot_fail` baja ≥ 0,20 si el base parte de ≥ 0,20; con base < 0,20, como los debuts de 02 §9.2, basta `par` menor en `minus_color` o el vacío extra de `plus_empty`, 03 §17 #8); JSON Schema canónico `schemas/af-level-1.schema.json` (única copia, a la que remiten 03 §10 y §17 #11; `D` sin límites, `par` nulo solo en `plus_empty`, `relief` obligatoria desde el 31, formato diario; 03 §3 usa la codificación len + tinte−1); claves `lvl_*` (incl. `lvl_murky_max_hidden_units` = 4 y `lvl_dda_enabled` = false), `relief_*`, `level_overrides`, `daily_overrides` y `content_version` de la tabla única (sección 9); entrega 1-700 instalados / 701+ remotos (03 §17) |
 | 04 Comodines, Canoas y rescate | Reglas de comodines, Canoas y rescate; definición de fallo (04 §5.2: fallo blando con ≥8 vertidos; cerrar la app no es fallo y, desde el 31, salir del nivel con el menú sí, D-008); esquema de inventario (se integra tal cual) | Tabla única de Remote Config (sección 9: `undo_*` incl. `undo_unlimited_until_level` = 10, `extra_cuia_unlock_level` = 8, `hint_unlock_level` = 12 + `hint_tutorial_units` = 3, `hint_idle_from_level` = 3 / `hint_idle_delay_s` = 12, `rescue_price_bands` = `[[31,600]]`, `rescue_first_day_mult`, `rescue_max_per_level` = 1, `canoes_*`…), flujo SSV, `restoreLedger`, fusión de guardado, eventos con prefijo `af_`; Canasta determinista en 04 §2.2/2.3/§3; SKU `af_infinite_canoes_1h` (04 §7) |
 | 05 Economía y monetización | Valores de Bitácora, Canasta, Crecida (05 §2.4), Cántaro, precios, SKUs, cadencia de anuncios, metas de ARPDAU (05 §9: ≥0,015 LatAm fase 1; ≥0,08 US antes de UA), bandas de edad y bloqueo <13 (05 §7) | Tabla única de Remote Config (sección 9: `eco_*` incl. `eco_starter_first_show` = "L12_or_session2", `ads_*`, `streak_milestones` como clave única, `rescue_price_bands` = `[[31,600]]`, SKU `af_infinite_canoes_1h`, `eco_model_version` = "B-2026-10-06e", `consent_under13_policy` = "block" y `consent_age_signal_source` = "self_declared"; 05 referencia sin repetir claves), `af_wallet_snapshot`, `tools/economy-sim`, precios por país en Play Console, Boot con las tres bandas de edad (sección 2.3) |
 | 06 Meta, retención y live-ops | Reglas de Reto del río, Bitácora, Crecida, Canasta, push (permiso en la 2.ª sesión, 06 §8.1) y eventos | Tabla única de Remote Config (sección 9: `live_*`, `push_preprompt_session` / `push_preprompt_min_level`, `league_*`; `eco_basket_cycle` sustituye a `chest_cycle`; `streak_milestones` en lugar de `eco_streak_rewards`; `eco_calendar_double_ad_enabled` = false en 1.0; `ui_murky_bonus_enabled` = false en 1.0 (1.1: true); `victory_autonext_s` = 0), push 1.0 solo locales, eventos de meta de 06 §12 dados de alta en la taxonomía única (sección 8.2), entrega de paquetes por `levels_remote` (06 §10), pool diario 2.190, integración MAX/IAP, `ad_impression` con ILR, Data Safety por SDK |
~~~~

## E12 · Documentación del proyecto

### E12-prototipo-v0.diff · `docs/dev/prototipo-v0.md`: sondas del Reto

<!-- parche: E12-prototipo-v0.diff -->
~~~~diff
diff --git a/docs/dev/prototipo-v0.md b/docs/dev/prototipo-v0.md
index 5850d2d..8bee69d 100644
--- a/docs/dev/prototipo-v0.md
+++ b/docs/dev/prototipo-v0.md
@@ -216,6 +216,13 @@ ## c) Capa Unity (unity-gameplay-dev)
     - `AF_STATE {"event":"idle_hint","t":…,"phase":"pulse","level":5,"from":3,"answer":0}` cuando la pista por inactividad (MO3; desde el 3, tras `hint_idle_delay_s` = 12 s sin toques) pulsa el origen de un vertido seguro, y `"phase":"silent"` cuando Morpho no tiene vertido seguro y no pulsa nada (una vez por tramo sin toques [P]). Nunca enseña el destino ni cobra. Ajustes › «Pista por inactividad» (`settings.idle_hint` [P], id `idle_hint` en `AF_LAYOUT settings` y `key` de `settings_change`) la apaga.
     - `AF_LAYOUT`: la línea `level` suma `hint` (la ranura «Pista», desde el 12, tras Cuia extra; Revelar pasa detrás) y la tarjeta de una respuesta sin vertido escribe `"screen":"hint_answer"` con `close`, `hint_undo` y `hint_restart`.
   - **Para tocar el Mapa con `adb`:** junta las partes de la última sonda `river_map` (FX7) y toca el centro de cada id; `map_l10_skip` completa la secuencia del 10, `map_north_up` gira la Cuenca y `stop_s{nn}` abre la tarjeta de la parada.
+- **Sondas del Reto del río** (R10 del equipo Spark; D-028; `DevProbe.Daily.cs`, formato en `AF.LiveOps/DailyProbeFormat.cs`). Solo en builds de desarrollo, como `AF_STATE` de una línea, con claves e identificadores (nunca texto visible):
+  - `{"event":"daily_card","t":…,"state":"hidden|in_progress|pending_validation|sealed|play","date":"AAAA-MM-DD","band":0-3,"day":…,"board":"d{b}_n{nnnn}"|null,"streak":n,"override":id|null,"override_ignored":bool}`: cada vez que se recalcula la tarjeta del inicio. Con la tarjeta oculta, `band` 0 y `board` null.
+  - `{"event":"level_start","t":…,"level_name":"d{b}_n{nnnn}","variant":"base","band":b}`: al terminar Llenar y teñir, o al reanudar sin intro; una vez por partida (Volver a la orilla no lo repite [P]).
+  - Las líneas de partida de `DevProbe.cs` (`load`, `ready`, `pour`… y las del embudo, la victoria y la animación saltada) llevan en `level` el nivel efectivo del Reto (`max_level + 1`, que no es un nivel de campaña) y, además, `"daily":"d{b}_n{nnnn}"` (enganche E24); en la campaña no llevan `daily`. Un script de `adb` no debe dar por hecho que `level` es un nivel de campaña. Las líneas `idle_hint`, `hint` y `tinaja_tip` de `GameScreen*.cs` van sin la marca: basta la de `load`.
+  - `{"event":"daily_challenge","t":…,"date":…,"result":"win|fail","t_ms":n|null,"murky_bonus":false,"streak_days":n}`: `win` al reclamar el premio; `fail` una vez por día, al descartar la partida de un día no ganado (R-Q15). `t_ms` es lo jugado desde que se abrió la partida, y null si se reanudó o en el `fail` [P].
+  - `{"event":"earn_virtual_currency","t":…,"virtual_currency_name":"seeds","source":"daily_challenge","value":n}`: las semillas del premio, pagadas ahora (también al abrir la app si se confirman cobros pendientes). La Pororoca de la racha no tiene evento (08 §8.2 no lo define).
+  - Para seguirlo: `adb logcat -s Unity | grep -E '"event":"(daily_card|level_start|daily_challenge|earn_virtual_currency)"'`.
 
 ## d) Build de desarrollo (build-release)
 
~~~~

### E12-README.diff · `docs/dev/traspaso/README.md`

<!-- parche: E12-README.diff -->
~~~~diff
diff --git a/docs/dev/traspaso/README.md b/docs/dev/traspaso/README.md
index fbb8e04..c1c728b 100644
--- a/docs/dev/traspaso/README.md
+++ b/docs/dev/traspaso/README.md
@@ -4,6 +4,18 @@ # Traspaso al equipo de escritorio (2026-10-09)
 
 ## 1. Resumen
 
+> *Estado del 2026-10-19 · Reto del río* (equipo Spark, rama `spark/reto-del-rio`; encargo `docs/dev/traspaso/spark-reto-del-rio.md`, estado y enganches en `docs/dev/traspaso/spark-estado.md`, nota de diseño `docs/dev/reto-del-rio.md`; decisiones del dueño en D-028):
+> - **Qué hay:** un tablero por día local desde el nivel 10, sin red; sin canoas, sin Cuia de rescate ni Salto del Boto y sin tocar la Crecida ni el progreso; reintentos ilimitados; 60 semillas y el sello del día; una Pororoca cada 7 días seguidos ganados; la partida se reanuda el mismo día, y el anti-reloj de 06 §4.1 (en la 1.0, sin servidor: `FakeServerDate`, así que todo premio sale confirmado).
+> - **Dónde:**
+>   - el pool: `Assets/_Project/Data/Daily/daily_pool_v1.json` (2.187 tableros = 3 bandas × 729), generado por `tools/level-generator --daily` desde `tools/level-generator/specs/daily_v1.json` (no se edita a mano) y comprobado por `tools/level-qa --daily`; informe en `tools/out/daily_pool_v1_report.md`;
+>   - el cliente: AF.LiveOps (`Scripts/LiveOps/`, sin motor: calendario, premio, racha, anti-reloj y sus claves de Remote Config), `DailyPool` y `DailyPoolReader` en AF.Levels, `IServerDate`, `FakeServerDate` y `ClockSecurityConfig` en AF.Platform, `LevelFlow.Daily.cs` en AF.GameFlow, y la tarjeta, la partida y el calendario de sellos en AF.UI (`DailyChallenge*.cs`, `DailyStampCalendar.cs` y `GameBootstrap.Daily.cs`);
+>   - las sondas: `DevProbe.Daily.cs` (`docs/dev/prototipo-v0.md`, «Sondas del Reto del río»); los textos, en `docs/dev/traspaso/spark-strings-reto.csv` hasta que se copien a `strings.csv` (E5).
+> - **Integración:** el bloque (a), generador, QA y lector del pool, ya está en `integracion-fase1-fase2` (`fcbbd94`). El resto entra con los enganches E1-E25 de `spark-estado.md` («Enganches para el integrador»): sin E1-E3 (tarjeta del inicio, `SetUpDaily()` y el pool en la escena) el Reto no se ve en el juego.
+> - **Cómo se prueba:**
+>   - herramientas: `dotnet run --project tools/level-generator -c Release -- --daily --spec tools/level-generator/specs/daily_v1.json --out Assets/_Project/Data/Daily --check` («identico», código 0) y `dotnet run --project tools/level-qa -c Release -- --daily Assets/_Project/Data/Daily/daily_pool_v1.json` (2.187 tableros, 0 fallos y 1.678 avisos, los de R-Q10); `--self-test daily` en las dos;
+>   - Unity: EditMode (`Scripts/LiveOps/Tests/`, `DailyPool*Tests`, `DailyLevelFlowTests`) y PlayMode (`DailyChallengeTests` y `DailyProbeTests`); las capturas `[Explicit]` de R11 (`DailyChallengeSnapshotTests`, en revisión en `spark/reto-del-rio-r11`) y qué revisar en ellas están en `spark-estado.md`;
+>   - teléfono (con E1-E3 aplicados): desde el nivel 10, la tarjeta «Reto del río · {fecha corta}» en el inicio; jugar y ganar (sello en el calendario, +60, `daily_challenge` con `win` y `earn_virtual_currency`); salir sin ganar y volver (la tarjeta pasa a «Seguir», R-Q16); comprobar que no cambian las canoas, la Crecida ni el nivel de campaña. Las sondas, con `adb logcat -s Unity | grep -E '"event":"(daily_card|level_start|daily_challenge|earn_virtual_currency)"'`.
+>
 > *Estado del 2026-10-18* (tanda 4: **ola 5 cerrada en `9fc15f9`** y probada en el teléfono en `a9b0376` con resultado «cumple», `pruebas-telefono-2026-10-17-tanda4-ola5.md`; **ola 6 en integración**; escrito por M4.6):
 > - **Integrado en la ola 5:**
 >   - el carril del Mapa: las paradas 421-700 (M4.5b, k = 42-69), así que los datos del Mapa cubren ya 1-700;
@@ -429,6 +441,7 @@ ## 7. Decisiones del dueño vigentes (registro en `docs/dev/decisiones.md`)
 | D-024 | Reglas de la Hoja atada (2026-10-14): la capacidad de su llave y una Hoja atada por fruta, con las recomendadas de la revisión de K3a. Documentación aplicada (FIXD) e implementada en el núcleo (FIXK, ola 1 de la tanda 4); el generador y level-qa la heredan por `LevelValidation` |
 | D-025 | Un solo formato de cifras, también en los precios: todo con `Loc.Number` (región del teléfono; sin región, es-CO [P]) y los precios como moneda + importe con ese formato. **Implementada en FXT** (`Loc.Price`; el adaptador de Unity IAP compondrá con `Loc.PriceFromMicros`) |
 | D-026 | Audio provisional por código (2026-10-16): los 61 efectos del laboratorio portados a C# y generados al arrancar, con los arreglos de su revisión (cada uno a su nivel, graves al 50 % por encima de 300 Hz, cola de dosel solo donde va, gestos ≤ 0,2 s y el chorro como bucle que sube con el llenado), Sonido, Música y Vibración en Ajustes y el permiso `VIBRATE` aprobado para los hápticos. **Implementada en AU1**, con el ambiente por mundo y la música adaptativa provisionales de AU2 (detrás del ajuste Música [P]); integrada en `bd649ac` y llevada a 07 §9 (FXD2). Desde AU3 (ola 5, `4476796`), el juego suena con los 173 WAV de la entrega 1 y el audio por código queda de respaldo; la prueba de la ola 5 lo comprobó por sondas (cumple) y falta escucharlo |
+| D-028 | Reto del río (2026-10-19): pool de 729 días por banda (2.187 tableros) para que el agua y el fondo sigan a la fecha; B2 con K 8 y B3 sin tope de pares; la ventana del bot como compuerta, con 30 candidatos por fuente; manda la fecha de inicio y la partida se reanuda el mismo día; Pororoca cada 7 días seguidos ganados (al stock antes del 30); comodines de la campaña salvo Cuia de rescate y Salto del Boto; sin intersticial; `fail` una vez por día; `daily_overrides` = `{fecha: {banda: id}}`. Implementado por el equipo Spark (`spark/reto-del-rio`); pendiente de integrar con los enganches E1-E25 |
 
 ## 8. Cómo trabaja el dueño (preferencias aprendidas)
 
~~~~

### E12-CLAUDE.diff · `CLAUDE.md`: mapa del repositorio y comandos

<!-- parche: E12-CLAUDE.diff -->
~~~~diff
diff --git a/CLAUDE.md b/CLAUDE.md
index fa1ba32..033880d 100644
--- a/CLAUDE.md
+++ b/CLAUDE.md
@@ -26,8 +26,8 @@ ## Mapa del repositorio
 - `schemas/af-level-1.schema.json`: esquema de niveles.
 - `Packages/com.amazonfill.core/`: **única copia** de AF.Core y AF.Solver (`Runtime/`; tests compartidos en `Tests/Editor/`); los asmdef de `Runtime/` con `noEngineReferences: true` y `autoReferenced: false` (los scripts de `Assets/` los referencian desde su propio asmdef).
 - `dotnet/`: `AmazonFill.slnx`, `Directory.Build.props`, `BannedSymbols.txt`, `AF.Core`/`AF.Solver` (.csproj que enlazan los `.cs` del paquete), `AF.Core.Tests` (net10.0, con `DotnetOnly/`); salida en `dotnet/artifacts/` (ignorada).
-- `tools/`: `level-generator/` (.NET 10; `--handmade` genera el pack del tutorial desde `level-generator/specs/tutorial_v0.json` con AF.Core/AF.Solver) y `level-qa/`, `levelgen_proto.py`, `econ_sim.py`, `econ_variants.py`, `validate_schema.py`; salidas en `tools/out/` y `tools/out_inverse/`. Cuando exista, `river-map/` (Mapa del río, D-013; Python 3.14, generadores solo con la biblioteca estándar, 08 §4.5, y `jsonschema` solo en `test_schemas.py`): genera `Data/Map/` y el bloque de filas del Mapa de `Data/Loc/facts.csv`.
-- Proyecto Unity (URP 2D, creado el 2026-10-06): `Assets/_Project/` con `Scripts/` (asmdef AF.Levels y AF.GameFlow sin motor; AF.Presentation y AF.UI; gráficos provisionales por código), `Data/Levels/tutorial_v0.json` (niveles 1-10, generado: no lo edites a mano), `Scenes/Game.unity` (la crea `GameSceneBuilder.CreateGameScene`), `Tests/` (EditMode y PlayMode) y `Editor/CI/` (AF.EditorTools: `ProjectSetup`, `BuildSmoke`, `BuildDev`); `ProjectSettings/`, `Packages/manifest.json` y `packages-lock.json` (este lo regenera Unity: no lo edites a mano). Prototipo v0: `docs/dev/prototipo-v0.md`. Previstos (cuando existan):
+- `tools/`: `level-generator/` (.NET 10; `--handmade` genera el pack del tutorial desde `level-generator/specs/tutorial_v0.json` con AF.Core/AF.Solver; `--daily`, el pool del Reto del río desde `level-generator/specs/daily_v1.json`, con el código en `level-generator/Daily/`) y `level-qa/` (`--daily` comprueba ese pool, con el código en `level-qa/Daily/` y los fixtures en `level-qa/fixtures/daily/`), `levelgen_proto.py`, `econ_sim.py`, `econ_variants.py`, `validate_schema.py`; salidas en `tools/out/` y `tools/out_inverse/`. Cuando exista, `river-map/` (Mapa del río, D-013; Python 3.14, generadores solo con la biblioteca estándar, 08 §4.5, y `jsonschema` solo en `test_schemas.py`): genera `Data/Map/` y el bloque de filas del Mapa de `Data/Loc/facts.csv`.
+- Proyecto Unity (URP 2D, creado el 2026-10-06): `Assets/_Project/` con `Scripts/` (asmdef AF.Levels, AF.GameFlow y AF.LiveOps sin motor; AF.Presentation y AF.UI; gráficos provisionales por código; `Scripts/LiveOps/`: AF.LiveOps, el Reto del río (D-028), con sus tests en `Scripts/LiveOps/Tests/`), `Data/Levels/tutorial_v0.json` (niveles 1-10, generado: no lo edites a mano), `Data/Daily/daily_pool_v1.json` (pool del Reto del río, 2.187 tableros, generado por `tools/level-generator --daily`: no lo edites a mano), `Scenes/Game.unity` (la crea `GameSceneBuilder.CreateGameScene`), `Tests/` (EditMode y PlayMode) y `Editor/CI/` (AF.EditorTools: `ProjectSetup`, `BuildSmoke`, `BuildDev`); `ProjectSettings/`, `Packages/manifest.json` y `packages-lock.json` (este lo regenera Unity: no lo edites a mano). Prototipo v0: `docs/dev/prototipo-v0.md`. Previstos (cuando existan):
   - `Scripts/Localization/`: AF.Localization, la fachada `Loc` sin motor (08 §2.2);
   - `Data/Loc/`: tablas de textos `strings.csv`, `facts.csv` y `glossary.csv` (08 §4.7, contrato 32; reglas en `.claude/rules/localizacion.md`);
   - `Data/Content/`: calendarios de contenido, como `frutas_del_rio_v1.json` (08 §4.6, contrato 31);
@@ -48,13 +48,17 @@ ## Comandos de verificación (obligatorios antes de dar una tarea por terminada)
                                                       # reescribe tools/out/schema_example_08_4_1.json desde 08 §4.1; commitea el cambio si aparece
 python tools/econ_sim.py --runs 300 | diff --strip-trailing-cr -q - tools/out/econ_B.md
                                                       # (Git Bash) solo si tocas economía; sin salida = idéntico
+dotnet run --project tools/level-generator -c Release -- --daily --spec tools/level-generator/specs/daily_v1.json --out Assets/_Project/Data/Daily --check
+                                                      # Reto del río (los dos): solo si tocas el pool o sus herramientas; «identico», código 1 si difiere
+dotnet run --project tools/level-qa -c Release -- --daily Assets/_Project/Data/Daily/daily_pool_v1.json
+                                                      # 2.187 tableros, 0 fallos (los avisos de R-Q10 no cuentan)
 python tools/river-map/stops.py --check               # Mapa del río (los cuatro): solo si tocas el Mapa; código 1 si falla
 python tools/river-map/geometry.py --check            # cuando exista
 python -m unittest discover -s tools/river-map -p "test_*.py"
 python docs/research/mapa-del-rio/generar_mapa.py --check
 pwsh -NoProfile -File scripts/unity-compile.ps1       # ¿compila Unity? errores: solo las 30 primeras líneas "error CS"
-pwsh -NoProfile -File scripts/unity-test.ps1 -Mode EditMode    # 2.861 hoy en integracion-fase1-fase2 (2.858 + 3 omitidos); dotnet test: 981 (975 + 6 omitidos)
-pwsh -NoProfile -File scripts/unity-test.ps1 -Mode PlayMode    # 1.359 (1.196 + 163 explícitos de capturas); antes de cada PR que toque Unity
+pwsh -NoProfile -File scripts/unity-test.ps1 -Mode EditMode    # «EDITMODE_TOTAL» hoy en integracion-fase1-fase2 («EDITMODE_OK» + «EDITMODE_OMITIDOS» omitidos); dotnet test: 981 (975 + 6 omitidos)
+pwsh -NoProfile -File scripts/unity-test.ps1 -Mode PlayMode    # «PLAYMODE_TOTAL» («PLAYMODE_OK» + «PLAYMODE_EXPLICITOS» explícitos de capturas); antes de cada PR que toque Unity
 ```
 - Las pruebas de `tools/` van como fixtures (`tools/level-generator/specs/tests/`, `tools/level-qa/fixtures/`) y como un modo `--self-test` en la propia herramienta; `tools/river-map/` suma sus `test_*.py` (unittest). No hay proyectos de tests de `tools/` en `dotnet/AmazonFill.slnx`.
 - Los scripts de Unity lanzan `unity test <proyecto> --mode <Mode> --architecture arm64 ...` (respaldo: `Unity.exe -batchmode -runTests`). Códigos: 0 verde · 1 errores de compilación · 2 argumentos · 3 no encuentra el proyecto Unity (¿carpeta equivocada?) · 4 falta CLI o Editor · 5 proyecto abierto en un Editor · 6 sin veredicto (lee el log; no reintentes a ciegas) · 8 tests fallidos.
~~~~
