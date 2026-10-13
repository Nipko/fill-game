# Capítulo 5: Economía y monetización

Fecha: 2026-10-06 (actualizado el 2026-10-08 con la decisión del dueño **D-008**, bucle de partida como Magic Sort: §2.4, §3.4, §5, §6.1, §6.3 y §10, y con la nota de la pregunta P17 en §4.1, §5, Conflictos y Dependencias; registro en `docs/dev/decisiones.md`; nota del 2026-10-11, precisada el 2026-10-12, en §2.1 y §3.1 sobre **D-009**, que reordena el ciclo de 20: las cifras [sim] de este capítulo siguen siendo las de la corrida del 2026-10-06 hasta que el dueño apruebe las regeneradas, GDD.md §7.3, V5; actualizado el 2026-10-12 (paso 2 del traspaso) con **D-011**, Cosecha doble que avanza sola por AD_GATE sin intersticial: §6.1, y con **D-012**, banner de mundo como tarjeta simple: el sitio de la oferta del pack de skin queda pendiente de la pregunta 39 de GDD.md §6.3 en §4.1, §5 y Dependencias) · GDD de Amazon Fill (nombre comercial: **Tintalú**) · Convención de evidencia: **[V]** verificado en el informe citado, **[E]** estimación del informe, **[R]** reseñas, **[S]** cifra de terceros no confirmada, **[NV]** no verificado, **[P]** propuesta de este capítulo a calibrar en soft launch. "(dirección N)" remite a las decisiones de dirección vinculantes. Los números marcados **[sim]** salen de `tools/econ_sim.py` tal como está en el repositorio (300 corridas por perfil, semilla 2026, corrida del 2026-10-06; escenario B salvo indicación; modelo `B-2026-10-06e`, donde e = `on_fail` con la regla del contrato 19: un fallo duro con la barra a 0 apaga el río crecido, como en §2.4), que integra la curva canónica del ciclo de 20 de 03 §12 (40 = primer Difícil, 60 = primer Muy difícil), la tabla de fallo derivada de 03 §15, los regalos del tutorial y la Canasta de ciclo fijo; la sensibilidad de 3.6 sale de `tools/econ_variants.py` (200 corridas por perfil, misma semilla). Salidas completas: `tools/out/econ_B.md`, `econ_A.md`, `econ_B_tramos.md` y `econ_variants.md`. Las claves de Remote Config que cita este capítulo son las de la tabla única de 08 §9, que fija tipo, default y rango.

## Resumen

Tintalú usa una sola moneda blanda, las **semillas** (1.000 = 1,99 USD), que se ganan jugando (20/60/80 por victoria, Crecida, Bitácora del río, Canasta) y se gastan sobre todo en la **Cuia de rescate** del modal Aguas estancadas, el motor de ingresos probado por Magic Sort pero con ruta gratuita, Marea usable en el bloqueo y precio único de **600 semillas** (dirección 10; primer rescate del día a mitad de precio; máximo 1 por nivel). La simulación de 500 niveles con las tasas de fallo derivadas de 03 §15 muestra que la dirección literal deja al no pagador medio con 2,4 rescates pagables al día y el ratio de presión de referencia en 6,6-7,0:1; con el único ajuste de este capítulo (hitos de la Crecida pagados una vez al día, con la escalera canónica 3 → 300 semillas · 6 → Canasta · 10 → 1 Cuia extra + río crecido) el ratio de referencia sube a **7,2-7,7:1** y el casual queda en 9,4-10,1:1 sin tocar las cifras fijadas por la dirección. El escalado del rescate a 750/900 llevaría la referencia a 10,2:1 en 101-500, pero con las tasas de 03 bloquea sin recursos al casual en el 100 % de las corridas y al medio en el 45 %, con neto diario negativo: queda **pendiente de validación** con el embudo del soft launch y no entra en 1.0. Ningún precio único equilibra a la vez al casual (termina el 500 con 1.842 semillas y al menos un bloqueo sin recursos en el 73 % de las corridas) y al medio (15.510 semillas sin uso): la propuesta es lanzar con 600 fijo y absorber el excedente con sumideros opcionales por semillas (3.6; decisión pendiente del dueño del producto, pregunta 2). El catálogo IAP tiene 13 SKU en 1.0 (0,99-49,99 USD) y 14 con el Pase Expedición de 1.2, con precios por país al ~50 % del ancla de EE. UU. en BR/MX/CO/PE/AR, 60 % en CL, 70 % en EC y paridad en EUR; ninguna compra es aleatoria (ECA Digital, Lei 15.211/2025). Los anuncios empiezan con un recompensado voluntario en el nivel 11 y el primer intersticial en el 22, solo tras victoria, cada 2-3 niveles, ≥150 s, tope 6 por sesión. Ingreso estimado por 1.000 DAU: ≈15 USD/día en LatAm-heavy (ARPDAU 0,015) y 48-95 USD/día en US-heavy (0,048 base, 0,095 objetivo), con la UA pagada en EE. UU. condicionada a ARPDAU ≥0,08 y D7 ≥15 %.

## 1. Principios de la economía

1. **Una escala, un ancla.** 1.000 semillas = 1,99 USD → 1 semilla ≈ 0,002 USD (dirección 12; ancla de Magic Sort y Royal Match, research/04 C1 §1 [V]). Todo precio en semillas se lee en centavos dividiendo por 5.
2. **El dinero se gana en el bloqueo, no en la tienda.** En Magic Sort más del 50 % del IAP sale de la pantalla de derrota (research/09 §Por qué ganó 2 [V]); la oferta tras fallar es el SKU que convierte al primer pagador en el género (27-34 % de primeras compras en Color Block Jam y Pixel Flow, research/03 C2.1 [V]). Tintalú copia el momento y corrige las tres quejas estructurales del líder: comodines guardados inservibles, tablero que cambia al reintentar y ausencia de ruta gratuita (research/09 §Mejorar [V]).
3. **Ruta gratuita real.** El no pagador medio gana semillas suficientes para unas dos Cuias de rescate al día a precio completo y el casual para 0,7 (2,03 y 0,69 [sim], sección 3), nunca cero como en Magic Sort (45:1 sin racha, research/09 §Economía [E]). La presión de pago viene de la **segunda** Cuia de rescate del día a precio completo y de los niveles Difícil/Muy difícil, no de la escasez absoluta.
4. **LatAm ads-first, EE. UU. híbrido.** En mercados emergentes los anuncios aportan 55-70 % del ingreso y en EE. UU. el IAP 77-90 % (research/04 §1.5 [V]). La economía es la misma; lo que cambia por país es el precio local y el orden de las opciones del modal.
5. **Nada aleatorio de pago.** Brasil prohíbe las cajas de recompensa de pago en juegos de acceso probable por menores (Lei 15.211/2025 art. 20, en vigor desde 17-03-2026, research/08 C5.1 [V]); la clasificación es por build, así que la regla vale para todo el juego (dirección 15).
6. **Honestidad medible.** Etiquetas Difícil/Muy difícil verdaderas, reintento determinista, variante aliviada comunicada ("El río bajó"), Sin anuncios que quita todos los intersticiales. Cada guardarraíl de la sección 10 tiene un límite técnico en Remote Config y una prueba automatizada.

### 1.1 Ratio de presión (métrica canónica de este capítulo)

```
ratio_presion(tramo, perfil) = precio_nominal_cuia_de_rescate(tramo)
                              / ingreso_efectivo_por_victoria(tramo, perfil)

ingreso_efectivo_por_victoria = (victorias + Cosecha doble + Crecida + Bitácora + Canasta + Reto del río)
                                / número_de_victorias          # solo fuentes gratuitas, sin IAP

Banda objetivo (research/09 §Mejorar): 10 ≤ ratio ≤ 15 para la referencia = media de los perfiles no pagadores.
Guardas: casual ≤ 15 (nunca "imposible"), hábil ≥ 7 (nunca trivial).
Guarda complementaria [P, 2026-10-06]: la ratio nominal no ve la FRECUENCIA de fallo, así que se lee junto con
  bloqueos_sin_recursos(medio no pagador) = 0 hasta el nivel 500 y neto_diario(medio, 101-500) ≥ 0 [sim].
  Si subir el precio mete la referencia en banda rompiendo esta guarda, manda la guarda (principio 3).
```

Magic Sort está en 45:1 con la recompensa base y en ≈9:1 con racha perfecta (research/09 §Economía [E]); Hexa Sort y Screw Jam, con 20-100 niveles por comodín, acumulan reseñas negativas por economía (research/03 §4 [R]). La banda 10-15:1 es el punto medio entre "se nota" y "se puede".

## 2. Mapa de fuentes y sumideros de semillas

### 2.1 Fuentes

| Fuente | Cantidad | Frecuencia | Nivel de debut | Tope | Evidencia |
|---|---|---|---|---|---|
| Victoria normal | 20 | cada nivel sin etiqueta: todos en 1-39, 16-17 de cada 20 en 41-60 y 14 de cada 20 desde el 61 (03 §12) | 1 | — | dirección 12; Magic Sort 10-20 [R] (research/09) |
| Victoria **Difícil** | 60 | hito del 40 (primer Difícil, 03 §11/§12; niveles 1-39 sin etiquetas); en 41-59, 2-3 de los 5 retos según el bot (el modelo usa 3: p6, p14 y p19; 03 §12; con D-009 son p6, p12 y p18, nota de §3.1); desde el 61, los 5 retos de cada 20 | 40 | — | dirección 12; proporción ×3 de Royal Match (research/04 C1 §1 [S]) |
| Victoria **Muy difícil** | 80 | 1 de cada 20 (hito) desde el 60 (primer Muy difícil, 03 §11/§12) | 60 | — | dirección 12 |
| **Cosecha doble** (x2 por anuncio recompensado) | +100 % de la victoria | voluntario, tras la victoria | 11 | 5/día (04 §3); sin anuncio mientras dure "río crecido" | dirección 13; placement de mayor adopción del género (research/03 §2 [V]) |
| **Crecida** hito 3 | 300 | una vez por día al encadenar 3 victorias | 25 | ver 2.4; tope diario 600 | dirección 12; Magic Sort 300/3 [V reseñas] (research/magic-sort/b §2.1) |
| Crecida hito 6 | **Canasta** (contenido visible: 50 semillas + 1 comodín del ciclo fijo de 06 §4.4; ≈116 semillas-equivalentes) | una vez por día | 25 | las 50 semillas cuentan en el tope diario 600; el comodín no | glosario §6.3; 06 §4.3; research/04 C1 §3 [E] |
| Crecida hito 10 | 1 Cuia extra + "río crecido" (Cosecha doble sin anuncio hasta el siguiente fallo duro no escudado); la barra vuelve a 0; sin semillas | una vez por día | 25 | no cuenta en el tope diario (ni la Cuia extra ni el bonus del río crecido) | [P]; equivalente al Super Light Ball de Royal Match (research/04 C1 §1 [V]); fila canónica compartida con glosario §6.3, 01, 04, 06, 08 y 09 |
| **Bitácora del río** (calendario de 30 casillas) | 50-650 por casilla, 3.000 semillas + 12 comodines al mes | 1 casilla/día jugado, reclamo secuencial sin reinicio | día 1 (desde el nivel 10) | 1/día | dirección 12; tabla 2.3; patrón Unity Daily Rewards (research/04 C1 §3 [V]) |
| **Canasta** (cofre cada 10 niveles) | 50 semillas + 1 comodín visible desde el nivel 20 (ciclo fijo Marea ×3 → Morpho → Cuia extra → Marea ×3 → Morpho); la del nivel 10 solo 50 semillas; Canasta grande cada 100 | cada 10 niveles | 10 | — | dirección 12; contenido visible por dirección 15; ciclo de 06 §4.4 (`eco_basket_cycle`) |
| **Reto del río** (reto diario) | 60 semillas (admite Cosecha doble); 7 días seguidos → 1 Pororoca | 1/día | 10 | 1/día | 06 §4.1 [P] (`live_daily_challenge_reward`) |
| Hitos de mundo | skin de cuia del mundo (cosmético, sin semillas) | al cerrar cada mundo desde el 2 (Cuia de Santarém al 40, Cuia del Encuentro al 80, Totuma al 140; la Cuia de vidrio del mundo 1 es la skin base) | 40 | — | dirección 14; 06 §4.5 |
| Compra de semillas (IAP) | 1.000-36.000 | a demanda | Mercado desde el 10; Puñado en el modal desde el 31 | — | dirección 14 |
| Cántaro (alcancía) | hasta 3.000 acumuladas por 2,99 USD | compra única cuando está ≥1.000 | 20 | 1 compra por llenado | dirección 14 |

Regla heredada de Dream Games: ninguna fuente gratuita entrega Cuias de rescate como premio fijo (Super Duke fue retirado por canibalizar la compra de movimientos, research/04 C1 §1 [V]). La racha paga semillas y comodines de preparación.

### 2.2 Sumideros

| Sumidero | Precio en semillas | USD equiv. | Condición | Victorias normales para pagarlo | Evidencia |
|---|---|---|---|---|---|
| **Marea** (+3 Deshacer) | 30 (o anuncio) | 0,06 | tras agotar los 3 gratis del nivel (ilimitada en los niveles 1-10, `undo_unlimited_until_level` = 10; 3 por nivel desde el 11); usable en Aguas estancadas | 1,5 | dirección 9; excepción de tutorial de 04 |
| **Morpho** (pista) | 100 (o anuncio) | 0,20 | siempre | 5 | dirección 9 |
| **Luciérnaga** (revelar cuia turbia) | 120 (o anuncio) | 0,24 | solo Aguas turbias (≥100) | 6 | dirección 9 (rango 100-150) |
| **Cuia extra** (planificada) | 150 | 0,30 | solo mientras quede vertido legal; 1 por nivel con semillas; persiste al reiniciar; 1 unidad regalada en el nivel 8 (tutorial, 04) | 7,5 | dirección 9 |
| **Pororoca** (barajar) | 300 | 0,60 | 1 por nivel, 3 por día | 15 | dirección 9 |
| **Salto del Boto** (saltar) | 300 | 0,60 | tras 5 fallos en el nivel; máx. 1 cada 10 niveles | 15 | dirección 9 |
| **Cuia de rescate** (desde el nivel 31) | **600** (300 el primero del día) | 1,20 / 0,60 | en Aguas estancadas; **máx. 1 por nivel** (`rescue_max_per_level` = 1: 14 cuias + 1 Cuia extra + 1 Cuia de rescate = 16, el máximo de pantalla de 02 R1/§14); en el 2.º bloqueo del mismo nivel el modal ofrece Marea y Volver a la orilla | 30 / 15 | dirección 10; precio único en todos los tramos y geos (ver Desviaciones: el escalado 750/900 queda pendiente de validación) |
| Volver a la orilla | 0 | 0 | ilimitado, determinista | 0 | dirección 2 y 7 |

Convivencia Cuia extra 150 / Cuia de rescate 600: la Cuia extra es un comodín *planificado* que desaparece de la barra al quedarse sin vertidos; el sobreprecio ×4 del rescate es el patrón del género (research/04 C1 §1 [V]) y se compensa con el descuento del primer rescate del día y con Marea usable en el bloqueo. Tope total de cuias añadidas por nivel = 2 (1 Cuia extra + 1 Cuia de rescate), nunca 17 cuias en pantalla.

### 2.3 Bitácora del río (30 casillas, 3.000 semillas)

Reclamo secuencial: la casilla no reclamada sigue disponible al día siguiente; exige ≥1 nivel jugado ese día; el 53 % de las semillas va en los hitos 7/14/21/30 (research/04 C1 §3 [E]). Nombres canónicos del glosario.

| Día | Premio | Día | Premio | Día | Premio |
|---|---|---|---|---|---|
| 1 | 50 | 11 | 100 | 21 | **400 + Canasta** |
| 2 | Marea ×3 | 12 | 1 Morpho | 22 | 100 |
| 3 | 50 | 13 | 100 | 23 | 1 Cuia extra |
| 4 | 1 Morpho | 14 | **300 + 1 Cuia extra** | 24 | 100 |
| 5 | 100 | 15 | 50 | 25 | 1 Morpho |
| 6 | 1 Cuia extra | 16 | 100 | 26 | 150 |
| 7 | **250 + Canasta** | 17 | Marea ×3 | 27 | Marea ×3 |
| 8 | 50 | 18 | 100 | 28 | 150 |
| 9 | 50 | 19 | Marea ×3 | 29 | 1 Luciérnaga |
| 10 | Marea ×3 | 20 | 150 | 30 | **650 + Canasta grande + marco de país** (el del mundo donde está el jugador, 06 §4.2) |

La casilla del día admite **Cosecha doble** (anuncio recompensado `rv_daily_chest`, 1/día) solo en casillas con semillas; esta ubicación entra en **1.1** (`eco_calendar_double_ad_enabled` = false en 1.0, 08 §9; 06 §4.2).

### 2.4 Crecida y Hoja escudo (reglas exactas)

```
on_victory(level):
  if level < 25: return                          # streak_unlock_level (08 §9)
  streak += 1
  if streak == 3  and not claimed_today[3]:  grant 300 seeds (cap_today ≤ 600); claimed_today[3] = true
  if streak == 6  and not claimed_today[6]:  grant Canasta (50 seeds, cap_today ≤ 600, + 1 comodín del ciclo fijo); claimed_today[6] = true
  if streak >= 10:
    if not claimed_today[10]: grant 1 Cuia extra; claimed_today[10] = true
    rio_crecido = true; streak = 0               # la barra vuelve a 0; el río crecido sigue hasta el siguiente fallo duro no escudado
  # cap_today (streak_daily_cap = 600) suma solo las 300 del hito 3 y las 50 de la Canasta del hito 6;
  # el bonus del río crecido (Cosecha doble sin anuncio) y los comodines de los hitos 6 y 10 no cuentan

on_hard_fail(level):                             # fallo duro (04 §5.2): Volver a la orilla desde Aguas estancadas (1-30); desde el 31, rendirse o salir del nivel con el menú (D-008)
  if streak >= 1 and shield_available_today: shield_available_today = false   # Hoja escudo: 1 fallo perdonado al día, con Crecida ≥1 (04 §8)
  else: streak = 0; rio_crecido = false
  # no tocan la Crecida (04 §5.2): fallo blando (reinicio voluntario con ≥8 vertidos), reinicio con <8 vertidos,
  # cerrar la app (con o sin modal abierto), salir con el menú en 1-30 y Salto del Boto;
  # una Cuia de rescate aceptada NO es fallo: conserva la racha (colateral, research/09 §Por qué ganó 3)

reset_daily(): claimed_today = {}; shield_available_today = true; cap_today = 0
tolerancia: la racha no expira por reloj; solo por fallo duro no escudado (research/09 §Mejorar: queja de rachas perdidas de noche)
```

**Escalera canónica de hitos (fila única para todos los capítulos):** 3 → 300 semillas · 6 → Canasta (contenido visible) · 10 → 1 Cuia extra + río crecido; cada hito una vez al día local (`streak_hits_once_per_day` = true); tope diario 600 semillas (`streak_daily_cap`), que suma solo las 300 del hito 3 y las 50 de la Canasta del hito 6: el bonus del río crecido (Cosecha doble sin anuncio) y los comodines de los hitos 6 y 10 no cuentan. Esta fila sustituye a las tres versiones que circulaban (glosario/01/06/08/09 con 300 semillas en el hito 10 y repetible; 04 con "Canasta o Canoas infinitas 15 min" en el hito 6; este capítulo con una Cuia extra en el hito 6).

Cada hito se paga **una vez por día**: evita que la Crecida se convierta en un grifo de 300 semillas por cada tres victorias (escenario A de la simulación: 107-116 semillas por victoria para los perfiles con racha [sim]) y mantiene el valor de la Hoja escudo. Al pagar el hito 10 la barra vuelve a 0; el río crecido sigue activo hasta el siguiente fallo duro no escudado (fallo según 04 §5.2). Si la barra vuelve a llegar a 10 el mismo día, se vacía otra vez y el río crecido sigue (o vuelve a quedar) activo, sin otra Cuia extra. Con la barra a 0, un fallo duro apaga el río crecido sin gastar la Hoja escudo (rama `else` de `on_hard_fail`). Es exactamente la regla que simula `tools/econ_sim.py` (`on_fail`, modelo `B-2026-10-06e`). Clave única `streak_milestones` (08 §9); el alias `eco_streak_rewards` no existe.

## 3. Simulación: `tools/econ_sim.py`

### 3.1 Supuestos

**Curva.** Ciclo canónico de 20 niveles de 03 §12 desde el 21, con posición `p = ((n − 1) mod 20) + 1`: p1-p2 recuperación (fallo ×0,5), retos en p6, p10, p14, p17 y p19, hito en p20 y las 12 posiciones restantes de comodidad; nunca dos Difícil seguidos y el hito siempre precedido de un reto (03 §11/§12; research/02 §7.2 [V]). Los niveles 1-20 son la rampa de tutorial a mano, sin etiquetas. **21-39 sin etiquetas**: sus retos (26, 30, 34, 37 y 39) se modelan como reto suave (`challenge`), con fallo medio entre comodidad y Difícil [P]. **40 = primer Difícil** (hito). **41-59**: 3 de los 5 retos son Difícil (p6, p14 y p19) y 2 quedan sin etiqueta (p10 y p17, como `challenge`); 03 §12 admite 2-3 según la medición del bot y el modelo usa 3 (4 Difícil en los primeros 60 niveles). **60 = primer Muy difícil**. **Desde el 61**, los 5 retos son Difícil y el hito Muy difícil (claves `lvl_labels_from_level` / `lvl_very_hard_from_level` en 08 §9). Con esta cadencia la victoria base suma 15.940 semillas en 500 niveles, igual para todos los perfiles [sim].

*Nota del 2026-10-11 (D-009, decisión del dueño del 2026-10-08).* El ciclo pasa a retos en p6, p9, p12, p15 y p18, con un remanso puro en p19 (03 §12); en 41-59 llevan etiqueta p6, p12 y p18, y p9 y p15 quedan como `challenge`; los retos suaves de 21-39 pasan a 26, 29, 32, 35 y 38. La cuenta de etiquetas no cambia. `tools/econ_sim.py` ya usa el ciclo nuevo (`CHALLENGE_POSITIONS` = {6, 9, 12, 15, 18}; tarea P-2, `0faf51e`) y `tools/out/econ_*.md` se regeneró con él, pero las cifras [sim] de este capítulo siguen siendo las de la corrida del 2026-10-06 hasta que el dueño apruebe las nuevas (GDD.md §7.3, V5; pregunta 25 de GDD.md §6.3). Los ratios de referencia no cambian (7,7 / 7,2:1) y la mayoría de las celdas se mueven poco (por ejemplo, saldo final del casual 1.842 → 1.912, primer bloqueo sin recursos del casual en el nivel 249 → 260, ingreso por victoria 61,4 → 61,5 y 97,7 → 97,6), pero algunas cambian más: el primer bloqueo sin recursos del medio pasa del nivel 87 al 44 (en el 1 % de las corridas; antes, el 2 %) y el del pagador ligero, del 311 al 282 (corridas afectadas, 10 → 14 %); en el escenario A, el del pagador pasa del 110 al 258. Una selección de las celdas que cita el GDD está en `docs/dev/decisiones.md` (D-009, Consecuencias); la lista completa, celda a celda, es el diff de `0faf51e` en `tools/out/econ_*.md`.

**Tasa de fallo por intento del jugador casual de referencia.** Derivada de los intentos medios objetivo de 03 §15 (normal 1,2-1,5 · Difícil 1,6-2,2 · Muy difícil 2,5-3,5; ancla Royal Match 1,2 / 1,6 / 2,5, research/02 §5 [V]) con `p_fail = 1 − 1/intentos_medios`, repartiendo cada rango de 03 en tres tramos crecientes (31-100 el mínimo, 201+ el máximo); vive en `FAIL_BASE` de `tools/econ_sim.py`. Cuando el bot casual ×40 y el embudo del soft launch den intentos medidos por banda, volver a correr el script (03 §15 "Coherencia bot ↔ humanos").

| Banda | Normal | Difícil | Muy difícil | Intentos medios implícitos (N / D / MD) | Etiqueta |
|---|---|---|---|---|---|
| 1-20 | 2 % | — | — | 1,02 | [P]: niveles ajustados a mano; alarma de abandono >5 % en 1-10 (03 §15) |
| 21-30 | 10 % | — | — | 1,11 | [P] interpolación hacia el 31; sin etiquetas (03 §12: retos suaves que se fallan y reinician gratis) |
| 31-100 | **17 %** | **38 %** (el 40 es el primer Difícil) | **60 %** (el 60 es el primer Muy difícil) | 1,2 / 1,6 / 2,5 | 03 §15 (mínimo del rango objetivo) |
| 101-200 | **25 %** | **45 %** | **65 %** | 1,33 / 1,8 / 2,9 | 03 §15 (centro del rango) |
| 201-500 | **33 %** | **55 %** | **71 %** | 1,5 / 2,2 / 3,5 | 03 §15 (máximo del rango) |

Retos sin etiqueta (`challenge`: 21-39 y p10/p17 de 41-59): media de comodidad y Difícil de su banda, 24 % en 21-30 y 27,5 % en 31-59 [P]. Modificadores: habilidad `p' = 1 − (1 − p)^m` (casual m = 1,25; medio y pagador 1,0; hábil 0,6); Cuia extra o Cuia de rescate activa ×0,40 (un escalón entero de dificultad, research/02 §7.3 [V]); variante aliviada tras 3 fallos ×0,50 (dirección 7); Morpho ×0,80 [P]; Marea en el bloqueo salva el intento con probabilidad 0,30 [P]; "rescate → victoria" 0,90 (objetivo >85 %, research/03 C2.5). Canoas: 5, se pierden solo al reiniciar tras bloqueo desde el 31, se regeneran entre sesiones (25 min × 5 < separación de sesiones); Canoa extra por anuncio (3/día). Topes rewarded de 04 §3: Cosecha doble 5, Marea 4, Morpho 3, Canoa extra 3 por día; 15/día global. Regalos del tutorial (04 §4): Marea ilimitada en 1-10 y 3 Morphos al inventario en el 12 (la Cuia extra del 8 se gasta en su nivel y no se modela). Comodines gratuitos: Canasta cada 10 niveles con 50 semillas + 1 comodín del ciclo fijo `eco_basket_cycle` (Marea ×3 → Morpho → Cuia extra → Marea ×3 → Morpho), Canasta del hito 6 de la Crecida con el mismo ciclo y los 12 comodines de la Bitácora (2.3); el inventario se gasta antes que los anuncios y las semillas. Cuia de rescate 600 fija desde el 31 (`rescue_price_bands` = `[[31,600]]`), 300 la primera del día.

**Perfiles.**

| Parámetro | Casual sin racha | Medio no pagador (ref.) | Hábil con racha | Pagador ligero |
|---|---|---|---|---|
| Multiplicador de fallo | 1,25 | 1,00 | 0,60 | 1,00 |
| Sesiones/día × intentos | 2 × 4 | 2 × 7 | 3 × 6 | 3 × 5 |
| Ve Cosecha doble | 25 % | 50 % | 70 % | 50 % |
| Acepta Cuia de rescate si tiene saldo | 50 % | 60 % | 60 % | 80 % |
| Compra Cuia extra en Difícil (antes / tras fallar) | 5 / 15 % | 15 / 35 % | 20 / 40 % | 25 / 50 % |
| Sostiene la Crecida | **no** (ingreso de racha = 0, piso del no pagador) | sí | sí | sí |
| Juega el Reto del río | 30 % | 60 % | 80 % | 60 % |
| Compras | — | — | — | Mochila de inicio (primera victoria con nivel ≥12 en una sesión ≥2; el modelo la compra el día 2 al llegar al 12), Puñado si quiere rescate sin saldo (60 %), Balsa de rescate (30 %, 1/día), Cántaro al llenarse (50 %); 3 días entre compras; 8 USD/30 días |

### 3.2 Resultados

**Comparación de escenarios [sim, corrida 2026-10-06 del script del repositorio; `tools/out/econ_A.md` y `tools/out/econ_B.md`].** A = dirección literal (Crecida repetible hasta 600/día con 300 semillas en el hito 10; Cuia de rescate 600 fija). B = economía de este capítulo (cada hito de la Crecida se paga una vez al día; hito 10 = Cuia extra + río crecido; Cuia de rescate **600 fija**, dirección 10). Todo lo demás es idéntico en ambos: hito 6 de la Crecida = Canasta (50 semillas + comodín del ciclo fijo), Canasta 50 + comodín cada 10 niveles, Reto del río 60, Cosecha doble 5/día, Bitácora 3.000/mes, topes de recompensados de 04. La variante B+tramos (rescate 600/750/900) se muestra aparte, más abajo.

| Métrica | A casual | A medio | A hábil | **B casual** | **B medio** | **B hábil** | **B pagador** |
|---|---|---|---|---|---|---|---|
| Días hasta el nivel 500 | 74,6 | 40,2 | 30,2 | 74,6 | 40,2 | 30,2 | 36,0 |
| Corridas que terminan el 500 | 100 % | 100 % | 100 % | 100 % | 100 % | 100 % | 100 % |
| Intentos totales | 593 | 556 | 536 | 593 | 556 | 536 | 533 |
| Victorias/día | 6,7 | 12,4 | 16,6 | 6,7 | 12,4 | 16,6 | 13,9 |
| Ingreso gratuito total (semillas) | 30.709 | 57.953 | 53.688 | 30.709 | 48.857 | 46.486 | 48.621 |
| Ingreso efectivo por victoria | 61,4 | 115,9 | 107,4 | **61,4** | **97,7** | **93,0** | 97,2 |
| Saldo final (semillas) | 1.842 | 23.815 | 32.107 | 1.842 | 15.510 | 26.408 | 8.393 |
| Cuias de rescate pagables con ingreso gratuito | 51,2 | 96,6 | 89,5 | 51,2 | 81,4 | 77,5 | 81,0 |
| Cuias de rescate pagables por día | 0,69 | 2,40 | 2,96 | 0,69 | 2,03 | 2,57 | 2,25 |
| Cuias de rescate usadas (de ellas a −50 %) | 72,5 (48,9) | 72,1 (33,0) | 45,0 (23,1) | 72,5 (48,9) | 72,1 (33,0) | 45,0 (23,1) | 87,5 (31,7) |
| Bloqueos sin recursos por corrida (corridas afectadas) | 5,9 (73 %) | 0,0 (1 %) | 0 | 5,9 (73 %) | 0,0 (2 %) | 0 | 0,4 (10 %) |
| Nivel del primer bloqueo sin recursos | 249 | 57 (en el 1 % de corridas afectadas) | — | 249 | 87 (en el 2 % de corridas afectadas) | — | 311 |
| Saltos del Boto | 0,0 | 0,0 | 0,0 | 0,0 | 0,0 | 0,0 | 0,0 |
| Anuncios recompensados/día | 1,7 | 2,3 | 2,6 | 1,7 | 2,4 | 2,8 | 1,6 |
| USD gastados | — | — | — | — | — | — | 3,54 |
| **Ratio de presión 31-100** | 10,1 | 5,1 | 5,7 | **10,1** | 6,2 | 6,8 | 6,3 |
| **Ratio de presión 101-500** | 9,4 | 5,0 | 5,3 | **9,4** | 5,9 | 6,2 | 5,6 |
| **Referencia (media de 3 no pagadores)** | 7,0 / 6,6 | | | **7,7 / 7,2** | | | |

Lectura: con la dirección literal el no pagador medio gana 116 semillas por victoria y puede pagar 2,4 Cuias de rescate al día; la Cuia de rescate deja de ser una decisión. El ajuste de la Crecida (una vez por hito y día) baja el ingreso del medio a 98 por victoria y sube la referencia a 7,2-7,7:1, todavía por debajo de la banda nominal 10-15:1; pero con las tasas de 03 §15 el casual ya gasta más de lo que gana en 101-500 (neto −11/día, 5,9 bloqueos sin recursos por corrida en el 73 % de las corridas, el primero hacia el nivel 249) y la presión real la fija la frecuencia de fallo, no el precio nominal: el casual usa 72,5 rescates en 500 niveles, tantos como el medio (72,1), con un 37 % menos de ingreso gratuito (30.709 frente a 48.857). Para el casual, A y B coinciden porque no cobra Crecida: la diferencia entre escenarios la marcan los perfiles con racha. Aun con el ajuste, el medio termina el 500 con 15.510 semillas y el hábil con 26.408, frente a 1.842 del casual: B separa a los perfiles por saldo mucho más que por precio (sensibilidad en 3.6). La cadencia canónica (40 primer Difícil, 60 primer Muy difícil, 3 retos Difícil en 41-59) fija la victoria base en 15.940 semillas por corrida para todos los perfiles.

**B frente a B+tramos (rescate 600/750/900) [sim].** Mismas semillas y 300 corridas por perfil (`tools/out/econ_B_tramos.md`); solo cambia `rescue_price_bands`. En 31-100 el precio es 600 en ambos casos y los ratios no cambian (referencia 7,7:1).

| Perfil | Ratio 101-500 (B → B+tramos) | Neto/día 101-500 | Bloqueos sin recursos por corrida (corridas afectadas) | Nivel del primer bloqueo | Saldo final |
|---|---|---|---|---|---|
| Casual sin racha | 9,4 → **13,4** | −11 → −25 | 5,9 (73 %) → **21,3 (100 %)** | 249 → **198** | 1.842 → 895 |
| Medio no pagador (ref.) | 5,9 → 8,5 | +316 → **−34** | 0,0 (2 %) → **3,1 (45 %)** | 87 → 352 | 15.510 → 4.224 |
| Hábil con racha | 6,2 → 8,9 | +851 → +508 | 0 → 0 | — → — | 26.408 → 18.120 |
| Pagador ligero | 5,6 → 7,8 | +124 → −112 | 0,4 (10 %) → 12,9 (90 %) | 311 → 305 | 8.393 → 1.536 (USD 3,54 → 7,70) |
| **Referencia** | **7,2 → 10,2** | | | | |

Lectura: el escalado mete la referencia nominal de 101-500 en banda (10,2:1), pero solo rompiendo la guarda complementaria de 1.1: el no pagador medio pasa a neto negativo (−34/día) y se bloquea sin recursos en el 45 % de las corridas, y el casual acumula ≈21 bloqueos con el muro hacia el nivel 198 (dentro de los 180-220 que estimaba research/04 C1 §5, citado en 01 §7.2, pero en el 100 % de las corridas). Por eso `rescue_price_bands` queda en `[[31,600]]` y los tramos 750/900 pasan a hipótesis pendiente (Desviación 1; sensibilidad completa en 3.6): solo se reconsiderarán si el embudo del soft launch mide intentos medios por debajo de los objetivos de 03 §15 o si el saldo medio al nivel 200 supera 8.000 semillas.

**Ingreso por fuente y gasto por sumidero, escenario B (semillas en toda la corrida) [sim].**

| Fuente / sumidero | Casual sin racha | Medio no pagador | Hábil con racha | Pagador ligero |
|---|---|---|---|---|
| Victorias (20/60/80) | 15.940 | 15.940 | 15.940 | 15.940 |
| Cosecha doble | 3.938 | 12.129 | 13.504 | 13.670 |
| Crecida (300 del hito 3 + 50 de la Canasta del hito 6) | 0 | 13.276 | 10.124 | 12.038 |
| Bitácora del río | 7.026 | 3.577 | 3.004 | 3.229 |
| Canasta (cada 10 niveles) | 2.500 | 2.500 | 2.500 | 2.500 |
| Reto del río | 1.306 | 1.434 | 1.415 | 1.244 |
| **Ingreso gratuito total** | **30.709** | **48.857** | **46.486** | **48.621** |
| IAP (Mochila 500, Cántaro 2.050, Puñado 137, Balsa 47) | — | — | — | 2.734 |
| Gasto en Cuia de rescate | 28.809 | 33.345 | 20.062 | 42.958 |
| Gasto en Cuia extra / Morpho / Salto del Boto | 56 / 0 / 2 | 0 / 0 / 1 | 12 / 4 / 0 | 4 / 0 / 0 |

El gasto en comodines con semillas es casi nulo en todos los perfiles: Canasta, Crecida, Bitácora y regalos del tutorial cubren la demanda (fija) de Cuia extra y Morpho del modelo, y el rescate es prácticamente el único sumidero activo.

**Presupuesto por fase, escenario B (semillas/día) [sim].**

| Fase | Perfil | Días | Victorias | Ingreso/día | Gasto/día | Neto/día | Rescates | Bloqueos sin recursos | Saldo al cerrar |
|---|---|---|---|---|---|---|---|---|---|
| 1-30 | Casual sin racha | 4,2 | 30 | 243 | 0 | +243 | 0 | 0 | 1.008 |
| 1-30 | Medio no pagador | 3,0 | 30 | 477 | 0 | +477 | 0 | 0 | 1.432 |
| 1-30 | Hábil con racha | 2,0 | 30 | 688 | 0 | +688 | 0 | 0 | 1.376 |
| 1-30 | Pagador ligero | 2,7 | 30 | 733 | 0 | +733 | 0 | 0 | 1.902 |
| 31-100 | Casual sin racha | 10,0 | 70 | 416 | 266 | +149 | 7,1 | 0,3 | 2.488 |
| 31-100 | Medio no pagador | 5,0 | 70 | 1.356 | 579 | +777 | 7,0 | 0,0 | 5.331 |
| 31-100 | Hábil con racha | 4,0 | 70 | 1.535 | 422 | +1.113 | 4,2 | 0,0 | 5.844 |
| 31-100 | Pagador ligero | 4,6 | 70 | 1.483 | 828 | +654 | 8,4 | 0,0 | 4.840 |
| 101-500 | Casual sin racha | 60,4 | 400 | 423 | 434 | **−11** | 65,3 | 5,7 | 1.842 |
| 101-500 | Medio no pagador | 32,2 | 400 | 1.263 | 947 | +316 | 65,1 | 0,0 | 15.510 |
| 101-500 | Hábil con racha | 24,2 | 400 | 1.612 | 761 | +851 | 40,7 | 0,0 | 26.408 |
| 101-500 | Pagador ligero | 28,7 | 400 | 1.489 | 1.365 | +124 | 79,1 | 0,3 | 8.393 |

El ingreso/día del pagador ligero incluye las semillas compradas (Mochila de inicio en 1-30; Cántaro, Puñado y Balsa después).

### 3.3 Ajustes adoptados (de A a B)

| # | Ajuste | Qué fija la dirección | Efecto medido |
|---|---|---|---|
| 1 | Cuia de rescate **600 fija** en todos los tramos y geos, primer rescate del día −50 %, máx. 1 por nivel (`rescue_price_bands` = `[[31,600]]`, clave única compartida con 02 §7 y 04 §6.2; `rescue_base_price` desaparece) | "600 por defecto, rango 450-900" (dirección 10): se aplica el valor por defecto | referencia 7,7 / 7,2 (31-100 / 101-500); medio y hábil prácticamente sin bloqueos sin recursos hasta el 500 (medio: 0,0 por corrida, 2 % de corridas). El escalado 600/750/900 se aparca: con la tabla de 03 §15 bloquea al casual en el 100 % de las corridas y al medio en el 45 % (ver 3.2 y 3.6) |
| 2 | Cada hito de la Crecida se paga una vez al día con la escalera canónica 3 → 300 · 6 → Canasta · 10 → 1 Cuia extra + río crecido (sin 300 semillas en el hito 10) | "300 por racha de 3; escudo 1 fallo/día" (dirección 12): se conserva | ingreso por victoria del medio 115,9 → 97,7; rescates pagables por día del medio 2,40 → 2,03; referencia de 7,0 / 6,6 a 7,7 / 7,2 (31-100 / 101-500). Sigue siendo el ajuste imprescindible: con 300 en el hito 10 y hitos repetibles el medio termina el 500 con 23.815 semillas sin uso (15.510 con el ajuste) |

Se descartaron dos recortes más (Canasta 30 en vez de 50; Reto del río 20 semillas + Morpho en vez de 60) porque contradicen los valores que fija 06 (§4.1 y §4.4); con la tabla de 03 §15, recortar fuentes empeora además el neto ya negativo del casual en 101-500. 3.6 lo cuantifica: incluso un recorte mayor (V3: Crecida 3 → 150 y Bitácora a 1.500 semillas/mes) solo lleva la referencia a 8,8 / 8,2:1 y deja al casual con al menos un bloqueo sin recursos en el 95 % de las corridas. Los topes de recompensados son los de 04 §3 (Cosecha doble 5/día, global 15/día); bajar Cosecha doble a 3/día se descartó por el mismo motivo (conflicto con 04).

### 3.4 Qué no modela la simulación

El embudo de D-008 (2026-10-08): la segunda oferta de rescate en ¿Seguro? y la canoa que cuesta salir del nivel desde el 31 no están modeladas (contrato 18); las cifras [sim] y el ratio de presión se mantienen como referencia y se recalibran con el embudo real del soft launch [P]. Abandono (todos los perfiles juegan hasta el 500), Aguas turbias y Luciérnaga, Pororoca, eventos v1.2, Sin anuncios y Pase Expedición, variación de fill de anuncios, la elasticidad del precio del rescate sobre la aceptación (p fija por perfil) y la de la demanda de comodines al saldo (probabilidades fijas; ver el límite en 3.6). Tampoco modela las Canastas de la Bitácora (días 7 y 21) ni la Canasta grande (día 30 de 2.3 y cada 100 niveles; contenido pendiente, 06 pregunta 4); además entrega ya en la Canasta del nivel 10 el primer comodín del ciclo (06 §4.4: la del 10 solo lleva 50 semillas y el ciclo empieza en el 20), no aplica el tope de 1 Cuia de rescate por nivel cuando la primera no basta (menos del 1 % de los rescates) y trata la Balsa de rescate como 1.000 semillas + 1 Cuia extra sin rescate aplicado (0,05 Balsas por corrida del pagador). No se espera que ninguna de estas simplificaciones cambie las conclusiones de 3.2 y 3.6 (sin cuantificar en el repositorio), pero la Canasta grande, si se aprueba, reduciría los bloqueos del casual y obliga a volver a correr el script. El "pagador ligero" compra sobre todo Mochila y Cántaro (3,54 USD en 36 días): con la economía B un jugador de habilidad media que sostiene la Crecida casi nunca necesita el Puñado antes del nivel 500; la conversión del rescate IAP vendrá de los perfiles casual (primer bloqueo sin recursos ≈ nivel 249, ≈ día 37 interpolando los días por fase de 3.2 [E], porque el script no informa del día; en el 73 % de las corridas, 5,9 bloqueos por corrida) y de los muros Muy difícil, no del jugador medio. El excedente de los perfiles con racha (15.510 semillas el medio y 26.408 el hábil al nivel 500) es la señal de que falta un sumidero (3.6 y pregunta abierta 2). El muro de pago que research/04 C1 §5 situaba en 180-220 queda en el modelo en ≈249 con 600 fijo y en ≈198 con el escalado 750/900; el embudo por nivel del soft launch manda sobre ambos.

### 3.5 Presupuesto diario del no pagador por fase (síntesis para el productor)

| Fase | Ingreso/día (casual · medio) | Gasto/día | Qué puede pagar | Bloqueados sin recursos objetivo | Palanca si se desvía |
|---|---|---|---|---|---|
| 1-30 | 243 · 477 | 0 (sin rescate ni Canoas; sin etiquetas Difícil antes del 40) | nada que comprar; acumula ≈1.000-1.430 al llegar al 31 | 0 % | — |
| 31-100 | 416 · 1.356 | 266 · 579 | casual: ≈0,7 rescates/día (el primero a 300); medio: ≈1,4/día (300 + 600) | ≤3 % al nivel 50; ≈8 % al 100 (research/04 C1 §5 [E]) | si >5 % al 50: bajar fallo de Difícil, no el precio |
| 101+ | 423 · 1.263 | 434 · 947 | casual: ≈1,1 rescates/día y neto ≈ −11/día (vive del saldo acumulado hasta el 100 y de la Bitácora; primer bloqueo sin recursos ≈ nivel 249); medio: ≈2/día con neto +316 | 15-20 % al 200; 20-25 % desde el 201 (research/04 C1 §5 [E]) | si saldo medio al 100 >4.000 o bloqueados al 200 <10 %: recortar primero Cosecha doble a 3/día, después Canasta a 30; subir el rescate a 750/900 solo si además el medio conserva neto ≥0 y 0 bloqueos (guarda complementaria de 1.1). Si los bloqueados del casual al 250 superan el 30 %: revisión de diseño antes de tocar las fuentes (el rango admisible de `rescue_first_day_mult` en 08 §9 es 0,5: primer rescate del día a 300); cualquier cambio de precio sigue el procedimiento de 04 §9 |

### 3.6 Sensibilidad: precio del rescate frente a fuentes gratuitas

**Ocho variantes del escenario B [sim].** `tools/econ_variants.py` corre el escenario B cambiando el precio del rescate y las fuentes gratuitas (200 corridas por perfil, semilla 2026; salida en `tools/out/econ_variants.md`, transcrita aquí con coma decimal). Ratio de referencia = media de los 3 no pagadores; "casual con bloqueo" = corridas del casual con al menos un bloqueo sin recursos. V0 es el canon y difiere de 3.2 solo por usar 200 corridas en vez de 300.

| Variante | Ratio ref. 31-100 | Ratio ref. 101-500 | Saldo final medio | Saldo final casual | Casual con bloqueo sin recursos | Rescates/día medio |
|---|---|---|---|---|---|---|
| V0 canon: rescate 600 fijo (1.º del día 300) | 7,7:1 | 7,2:1 | 15.686 | 1.876 | 70 % | 1,78 |
| V1 rescate 900 fijo (1.º del día 450) | 11,6:1 | 10,7:1 | 3.357 | 921 | 100 % | 1,65 |
| V2 rescate 750 fijo (1.º del día 375) | 9,6:1 | 8,9:1 | 8.048 | 1.075 | 100 % | 1,75 |
| V3 600 fijo + Crecida 3→150 + Bitácora 1.500/mes | 8,8:1 | 8,2:1 | 8.487 | 1.026 | 95 % | 1,77 |
| V4 750 fijo + Crecida 3→150 + Bitácora 1.500/mes | 11,0:1 | 10,3:1 | 3.067 | 851 | 100 % | 1,66 |
| V5 900 fijo + Crecida 3→150 + Bitácora 1.500/mes | 13,2:1 | 12,4:1 | 1.564 | 792 | 100 % | 1,46 |
| V6 600 fijo sin descuento del 1.º del día | 7,7:1 | 7,1:1 | 6.627 | 760 | 100 % | 1,74 |
| V7 750 fijo + victoria 15/45/60 + Crecida 3→150 | 12,4:1 | 11,4:1 | 1.627 | 730 | 100 % | 1,52 |

Lectura: con el canon (V0) el casual termina el 500 con 1.876 semillas, apenas tres rescates, y el 70 % de sus corridas sufre al menos un bloqueo sin recursos, mientras el medio acumula 15.686 aun usando 1,78 rescates al día: el mismo precio es escaso para uno y sobra para el otro, y ningún precio único equilibra ambos perfiles. Subir el rescate o recortar fuentes acerca la referencia a la banda 10-15:1 (solo V1, V4, V5 y V7 entran en ambos tramos, con 10,3-13,2:1), pero en todas ellas el casual tiene al menos un bloqueo sin recursos en el 100 % de las corridas y termina con 730-921 semillas, y las que se quedan fuera de banda (V2 a 750; V3 con recortes a 600) ya lo bloquean en el 100 % y el 95 %. Quitar el descuento del primer rescate del día (V6) no mueve el ratio nominal, que solo mira el precio completo, pero lleva al casual al 100 % y deja el saldo del medio en menos de la mitad (6.627): el descuento es lo que libra del bloqueo al 30 % de las corridas del casual. Recomendación [P]: lanzar con 600 fijo, que protege al casual y la retención (en el escenario LatAm-heavy la publicidad aporta el 73 % del ingreso neto, §8, así que exprimir el rescate rinde poco), y atacar el acaparamiento del jugador medio con sumideros opcionales que no afectan al casual (skins de cuia y de mundo canjeables por semillas, rango orientativo 3.000-8.000; Cuia extra y Morpho ofrecidos antes del nivel), vigilando el saldo medio al nivel 200 con el umbral de alarma ya definido en este capítulo (8.000 semillas; 3.2 y Desviación 1), que el medio del modelo cruza entre el cierre del 100 (5.331) y el 500 (15.510), y ajustando por Remote Config antes de reconsiderar los tramos. Límite del modelo: la demanda de comodines del simulador es inelástica (probabilidades fijas por perfil; en toda la corrida el medio no gasta ninguna semilla en Cuia extra ni en Morpho, 3.2), así que un jugador con saldo alto gastaría más de lo simulado y el acaparamiento real será menor que el de V0. La decisión (sumidero cosmético por semillas: sí o no y rango de precio) queda como pregunta abierta 2; este apartado no cambia el catálogo IAP ni ninguna decisión de diseño.

## 4. Catálogo IAP

### 4.1 SKU (precios tier-1 en USD; IDs del glosario §11)

| SKU id | Nombre de tienda ES (EN · PT) | USD | Contenido exacto | Cuándo y dónde aparece | Límite |
|---|---|---|---|---|---|
| `af_starter` | **Mochila de inicio** (Starter Pack · Mochila Inicial) | 0,99 | 500 semillas + 2 Cuia extra + 3 Morpho (valor nominal 1.100 semillas ≈ 2,19 USD, −55 %) | Ventana 10-20 (`eco_starter_window_levels`): en el Mercado desde el nivel 10; **primera tarjeta** en el Mapa del río en la **primera victoria con nivel ≥12 dentro de una sesión ≥2** (`eco_starter_first_show` = "L12_or_session2"; 06 es dueño de la primera sesión: cero ofertas de pago en ella); desaparece al pasar el nivel 20; temporizador 48 h desde la primera vista | 1 por cuenta (dirección 14) |
| `af_coins_2` | **Puñado de semillas** (Handful · Punhado) | 1,99 | 1.000 semillas | Mercado; upsell del modal Aguas estancadas sin saldo (dirección 10) | — |
| `af_coins_3` | **Bolsa de semillas** (Bag · Saco pequeno) | 4,99 | 2.800 (+12 %) | Mercado | — |
| `af_coins_4` | **Saco de semillas** (Sack · Saco) | 9,99 | 6.000 (+20 %) | Mercado | — |
| `af_coins_5` | **Costal de semillas** (Big Sack · Costal) | 19,99 | 13.000 (+30 %) | Mercado | — |
| `af_coins_6` | **Cosecha de semillas** (Harvest · Colheita) | 49,99 | 36.000 (+44 %) | Mercado | — |
| `af_rescue` | **Balsa de rescate** (Rescue Raft · Balsa de Resgate) | 4,99 | Cuia de rescate aplicada al instante + 1.000 semillas + 1 Cuia extra (al stock) + Canoas infinitas 30 min (contenido único, cerrado con 04 §6.2 `BALSA_499` y 09 §8 decisión 16; valor nominal ≈ 1.950 semillas ≈ 3,88 USD con el rescate a 600, antes 2.250 con el rescate a 900; vende el momento, research/04 C1 §1). Frase de tienda `stuck_balsa_499`: "Todo para seguir: una cuia ahora, 1.000 semillas, una Cuia extra y canoas infinitas 30 min" | **Disparador único en todos los geos:** Aguas estancadas, nivel ≥31, saldo < precio del rescate (600 o 300 con el descuento del día). T1/T3 solo cambian el orden Balsa/Puñado en la segunda pantalla del modal (6.3); no hay variante "desde el 2.º fallo" (research/03 C2.2 queda como evidencia del momento, no como regla) | 1 impresión/día (`af_rescue_daily_cap`); tras 2 rechazos → oculta 48 h (`af_rescue_hide_after_declines`) |
| `af_noads` | **Sin anuncios** (No Ads · Sem anúncios) | 4,99 | Elimina todos los intersticiales para siempre; conserva recompensados (marcados "opcional"); +500 semillas | Mercado desde el nivel 22; tarjeta contextual tras el 3.er intersticial visto o en D2 (research/05 §Primera sesión); 1 tarjeta/día | No consumible; restaurable |
| `af_piggy` | **Cántaro** (Clay Jar · Pote de Barro) | 2,99 | Exactamente las semillas acumuladas (visible, 1.000-3.000) | Icono en el HUD desde el nivel 20; se llena +15/+30/+40 por victoria normal/Difícil/Muy difícil; comprable desde 1.000; "¡Lleno!" a 3.000 | 1 compra por llenado; se vacía y vuelve a llenarse |
| `af_infinite_canoes_1h` | **Canoas infinitas 1 h** (Endless Canoes 1 h · Canoas infinitas 1 h) | 1,99 | 60 min de Canoas infinitas | Al quedarse sin Canoas tras rechazar Canoa extra dos veces en el día; también en Mercado. *Pendiente de P17 (D-008): 04 §7.3 la pone siempre en Sin canoas (Conflictos)* | 1 impresión contextual/día |
| `af_skin_m02` … `af_skin_m04` | **Pack Cuia de Santarém / Cuia del Encuentro / Totuma** | 1,99 c/u | Skin de cuia del mundo + fondo del mundo como marco del Mapa del río + 300 semillas | Mercado al entrar al mundo; desde el mundo 3 (nivel 41), también una oferta al entrar al mundo cuyo sitio está pendiente (pregunta 39 de GDD.md §6.3: con D-012 el banner de mundo es una tarjeta simple); en el mundo 2, solo en el Mercado (06); la skin también se gana gratis al completar el mundo (40, 80 y 140; dirección 14, 06 §4.5). El mundo 1 no tiene pack: la Cuia de vidrio es la skin base gratuita (00 §4, 06 §4.5, entitlement `cuia_vidrio` de 08 §4.3) | 1 por skin; desaparece al ganarla |
| `af_pass` | **Pase Expedición** (Expedition Pass · Passe Expedição) · v1.2 | 6,99 | 14 días; pista gratuita + premium: 4.000 semillas, 6 Cuia extra, 6 Morpho, 3 × Canoas infinitas 1 h, skin exclusiva, Hoja escudo extra diaria | Mapa del río al inicio de cada temporada de 14 días (dirección 14) | 1 por temporada; compra única, no suscripción |

Reservados (no en 1.0): `af_coins_1` 0,99 (450) y `af_coins_7` 99,99 (80.000) de research/04 como SKU de prueba; `af_noads_bundle` 9,99 (Sin anuncios + 3.000 semillas) para quien rechazó 4,99 dos veces (research/04 §Catálogo [E]). Comparables: pack mínimo 1,99 = ≈40 % de primeras compras en Color Block Jam [V]; Cántaro 2,99 Gardenscapes / 4,99 Hexa Sort (3.er IAP más popular) [V]; "quitar anuncios" 4,99-7,99 rara vez top-seller [V] (research/04 §3.2).

Reglas de tienda: precio local con impuestos visible antes de comprar; "Restaurar compras" en Mercado y Ajustes desde el día 1 (queja n.º 2 de los líderes, research/04 §Otras decisiones [V]); verificación de recibo en Cloud Functions antes de acreditar (research/06 §Backend [V]); compra fallida nunca descuenta; cada compra suprime intersticiales 24 h.

### 4.2 Precios localizados

Google Play convierte por tipo de cambio sin ajustar poder adquisitivo (brecha vs PPP 30-60 % en BR/MX); las plantillas de precios desaparecieron el 27-10-2025 y cada país se fija a mano (research/04 §3.3 [V]). Magic Sort cobra tiers de Apple sin descuento (1.000 monedas = R$ 12,90 / 39 MXN / 9.900 COP), es decir, 2-3× en poder adquisitivo para un brasileño (research/09 §Catálogo IAP [V]): aquí está la ventaja. Nivel objetivo: BR/MX/CO/PE/AR ≈50 % del ancla US (PricePush [V]), CL 60 % [P], EC 70 % con piso de 0,99 por ser dolarizado [P], EUR en paridad (research/04 §3.3). Redondeo psicológico local (…90 / …900 / 9 / 19).

| USD | BRL | MXN | COP | PEN | ARS (equiv. USD, se recotiza mensualmente) | CLP | USD (EC) | EUR |
|---|---|---|---|---|---|---|---|---|
| 0,99 | 2,49 | 9 | 1.900 | 1,90 | 0,49 | 590 | 0,99 | 0,99 |
| 1,99 | 4,99 | 19 | 3.900 | 3,90 | 0,99 | 1.190 | 1,49 | 1,99 |
| 2,99 | 7,90 | 29 | 5.900 | 5,90 | 1,49 | 1.790 | 1,99 | 2,99 |
| 4,99 | 12,90 | 45 | 9.900 | 9,90 | 2,49 | 2.990 | 3,49 | 4,99 |
| 6,99 | 17,90 | 65 | 13.900 | 12,90 | 3,49 | 3.990 | 4,99 | 6,99 |
| 9,99 | 24,90 | 89 | 18.900 | 18,90 | 4,99 | 5.990 | 6,99 | 9,99 |
| 19,99 | 49,90 | 169 | 36.900 | 37,90 | 9,99 | 11.990 | 13,99 | 19,99 |
| 49,99 | 129,90 | 429 | 94.900 | 94,90 | 24,99 | 29.990 | 34,99 | 49,99 |

Columnas BRL/MXN/COP/PEN/ARS/EUR: research/04 §3.3 [E] extendidas; CLP y EC [P]. Resultado buscado: el Puñado (1.000 semillas) cuesta ≈1 USD local en BR/CO/PE/MX, así que una Cuia de rescate de 600 vale ≈0,60 USD local y la primera del día ≈0,30 (research/09 §Mejorar). El ajuste LatAm se hace aquí, en el precio local del Puñado, nunca en el precio en semillas del rescate (6.3). Revisar ARS cada mes y el resto cada trimestre; comprobar en Play Console el precio mínimo permitido por país antes de publicar (0,99 USD → 9 MXN / R$ 2,49 pueden quedar bajo el mínimo [NV]).

**Métodos de pago de Google Play por país** (research/08 §5.3 [V]; AR, CL y EC [NV], confirmar en Play Console).

| País | Métodos | Implicación |
|---|---|---|
| Brasil | Visa, Mastercard, Amex, Elo; **Pix**; PicPay; Mercado Pago; PayPal; saldo y gift cards (sin boleto ni carrier billing) | Pix hace viable el Puñado a R$ 4,99 como compra impulsiva; ficha personalizada de BR menciona Pix |
| México | Tarjetas; carrier Telcel (pospago) y AT&T; PayPal; Mercado Pago; **OXXO y 7-Eleven** (efectivo, 3 días) | El efectivo no sirve para suscripciones: ningún SKU es suscripción |
| Colombia | Visa, MC; carrier Claro; **PSE** (Nequi, Nubank, bancos); **Efecty**; saldo | PSE/Efecty solo compras únicas: consumibles y packs |
| Perú | Visa, MC, Visa Electron; carrier Claro (pospago) y Entel | Carrier billing favorece 0,99-1,99 |
| Argentina / Chile / Ecuador | [NV] | Confirmar; precio en ARS recotizado mensualmente |

Fiscalidad (para el productor): Google Play 15 % hasta 1 M USD/año [V]; Google remite IVA en BR, MX, CL y CO [V]; Brasil retiene IRRF + CIDE ≈25 % a desarrolladores pagados en moneda ≠ BRL [S] (research/04 §2): confirmar con contador; si se confirma, el IAP neto de BR baja ≈25 % y el precio BR puede subir un escalón (R$ 5,90 por Puñado) en v1.1.

## 5. Ofertas contextuales

| Oferta | Disparador | Dónde | Frecuencia / tope | Expira | Evidencia |
|---|---|---|---|---|---|
| **Mochila de inicio** 0,99 | primera victoria con nivel ≥12 dentro de una sesión ≥2 (`eco_starter_first_show` = "L12_or_session2"); nunca en la primera sesión | tarjeta en Mapa del río; Mercado desde el nivel 10 (ventana 10-20) | 1 tarjeta; recordatorio en el nivel 15 y 19 | nivel 20 o 48 h desde la 1.ª vista | starter 0,99 de Magic Sort [V] (research/09 §Catálogo IAP); ventana de dirección 14; primera sesión sin ofertas (06 §2) |
| **Cuia de rescate** (semillas) | Aguas estancadas, nivel ≥31 | modal Aguas estancadas, CTA "Continuar: +1 cuia" · 600 semillas (300 la primera del día); **segunda oferta** en ¿Seguro? [P] tras «Rendirse», al mismo precio y con las mismas condiciones, y no aparece si el rescate no está disponible (D-008, 2026-10-08). Es el mismo bloqueo: no es takeover | sin tope diario con saldo; **máx. 1 por nivel** (el 2.º bloqueo del mismo nivel ofrece Marea y el pie: «Rendirse» desde el 31) | con el bloqueo | dirección 10; D-008 |
| **Puñado** 1,99 → **Balsa de rescate** 4,99 | Aguas estancadas sin saldo para el rescate, nivel ≥31 (disparador único; T1/T3 solo invierten el orden Balsa/Puñado) | mismo modal, segunda pantalla | Balsa 1 impresión/día, oculta 48 h tras 2 rechazos; Puñado siempre | con el modal | research/03 C2.2 [E]; fail offers 4,99-5,99 [V] |
| **Sin anuncios** 4,99 | 3.er intersticial visto o D2 (lo primero) | tarjeta tras la pantalla de victoria; Mercado permanente | 1 tarjeta/día; tras 3 rechazos, solo Mercado | — | research/05 §Primera sesión [E]; "pagué No Ads y sigo viendo anuncios" es la queja n.º 1 (research/08 §6 [V]) |
| **Cántaro** 2,99 | lleno (3.000) o ≥1.000 y toque en el icono | HUD de victoria; Mercado | 1 tarjeta al llenarse | — | Hexa Sort abre su alcancía en el nivel 7 [V] (research/04 C1 §2) |
| **Canoas infinitas 1 h** 1,99 | 0 Canoas y 2 rechazos de Canoa extra en el día (*pendiente de P17, D-008: 04 §7.3 la pone siempre en Sin canoas, sin cuenta atrás; Conflictos*) | modal de Canoas | 1/día | 10 min | Endless Portal 5,99 de Magic Sort [E] |
| **Skins** 1,99 | entrada a un mundo nuevo desde el mundo 3 (nivel 41); en el mundo 2 solo en el Mercado (el mundo 1 no tiene pack: Cuia de vidrio es la skin base) | Mercado y, desde el 41, una oferta al entrar al mundo cuyo sitio está pendiente (pregunta 39 de GDD.md §6.3): con D-012 el banner de mundo es una tarjeta simple | 1 oferta por mundo | al ganar la skin por hito | dirección 14; 06 (sin ofertas de pago al entrar en los dos primeros mundos) |
| **Pase Expedición** 6,99 (v1.2) | inicio de temporada; nivel ≥60 | Mapa del río | 1 tarjeta por temporada + recordatorio al 50 % de la pista gratuita | fin de temporada | pases estándar 6,99-9,99 [V] (research/04 §3.2) |

```
offer_scheduler(session):
  takeovers_shown = 0
  prioridad = [rescue_modal (solo en bloqueo), starter, noads, piggy_full, skin_world, pass]
  for evento in session.events:
    if evento is Aguas_estancadas: show(rescue_modal); continue       # nunca cuenta como takeover; tampoco ¿Seguro?, Nivel fallido ni Sin canoas del mismo bloqueo (D-008)
    if takeovers_shown >= 1: continue                                 # máximo 1 takeover por sesión (research/09 §Mejorar)
    for oferta in prioridad[1:]:
      if oferta.trigger(evento) and oferta.cooldown_ok() and not oferta.rejected_twice_recently():
        show(oferta); takeovers_shown += 1; break
  # Ninguna oferta de pago antes del nivel 10 ni en la primera sesión (Mochila: primera victoria ≥12 en sesión ≥2);
  # ninguna oferta de rescate ni Canoas antes del 31 (dirección 10-11). Tope: ui_max_takeovers_per_session = 1 (08 §9).
```

## 6. Anuncios

### 6.1 Intersticiales por rango de nivel (dirección 13)

| Niveles | Cadencia | Tope sesión / día | Separación mínima | Momento | Notas |
|---|---|---|---|---|---|
| 1-21 | **ninguno** | — | — | — | luna de miel; Magic Sort empieza en el 22 (research/09 §Anuncios [V]); retrasar el primero mejoró D1 un 5-8 % (research/04 §1.1 [V]) |
| 22-60 | 1 cada **3** niveles completados | 6 / 10 [P] | ≥150 s desde el anterior; ≥120 s desde el inicio de la sesión [P] | solo en la pantalla de victoria, tras la animación de recompensa, antes del botón Siguiente | el primer intersticial de la vida del jugador nunca en la misma sesión que la instalación (research/05 §Primera sesión) |
| 61-200 | 1 cada **2** niveles | 6 / 12 [P] | ≥150 s | ídem | 2-3 niveles por dirección; cadencia observada en guías del género (research/04 §1.1 [S]) |
| 201+ | 1 cada 2 niveles | 6 / 12 | ≥150 s | ídem | sin "cada nivel": 67 % de las reseñas negativas de Magic Sort son por anuncios (research/09 §Anuncios [V]) |
| Siempre | — | — | — | — | nunca al inicio de nivel, nunca en reintento ni en Volver a la orilla, nunca al rendirse ni en ¿Seguro?, Nivel fallido o Sin canoas, aunque Magic Sort lo haga (D-008), nunca tras tocar "Ver video" o "Comprar", nunca a <60 s de un recompensado [P] (de esta regla depende que el avance solo tras la Cosecha doble de D-011, que pasa por AD_GATE sin tocar Siguiente, no muestre un intersticial: 02 §2 y §6); 24 h de supresión tras cualquier IAP, también `af_infinite_canoes_1h` (research/04 §Reglas [E]); **las Canoas infinitas no suprimen intersticiales** (postura del dueño del 2026-10-08, D-008, como en Magic Sort: ficha §6.5): solo los corta la regla de 24 h tras una compra; 0 para Sin anuncios; cierre habilitado a los 5 s (política Play: ≤15 s, research/04 §1.1 [V]); sin banner ni app open (dirección 13) |

### 6.2 Anuncios recompensados (5 ubicaciones en 1.0 + Bitácora x2 en 1.1, dirección 13)

| Placement id | Nombre | Debut | Recompensa | Tope/día | Cooldown | Sin fill |
|---|---|---|---|---|---|---|
| `rv_double_harvest` | **Cosecha doble** | 11 | x2 semillas del nivel | 5 | 60 s | botón oculto |
| `rv_undo_refill` | Marea +3 | 11 (primer agotamiento de los 3 gratis; ilimitado en 1-10) | +3 Deshacer | 4 | 60 s | 30 semillas |
| `rv_hint` | Morpho | 12 (3 gratis al desbloquear) | 1 pista | 3 | 60 s | 100 semillas |
| `rv_reveal` | Luciérnaga | ~100 (Aguas turbias) | revela 1 cuia turbia | 3 | 60 s | 120 semillas |
| `rv_extra_canoe` | **Canoa extra** | 31 | +1 Canoa | 3 | 90 s | esperar 25 min / Canoas infinitas |
| `rv_daily_chest` **(1.1)** | Bitácora x2 | día 2 (nivel ≥10); apagada en 1.0 (`eco_calendar_double_ad_enabled` = false) | duplica las semillas de la casilla del día | 1 | — | nada |

IDs, topes y cooldowns de los cinco primeros placements son los de 04 §3 (claves `ads_rv_cap_<placement>` / `ads_rv_cooldown_s_<placement>` en 08 §9); `rv_daily_chest` lo añade este capítulo (dirección 13: "cofre diario") y entra en **1.1**: en 1.0 hay cinco ubicaciones activas. Tope global 15 recompensados/día (`ads_rv_global_cap_day`, 04 §3 [P]) (9 de cada 10 jugadores interactúa y quienes lo hacen compran 4× más IAP, research/04 §1.2 [V]; 5-8 por 20-50 min, research/03 §5 [V]). Precarga al iniciar el nivel y renovación cada hora; `no_fill` o carga >1,5 s (`ads_rv_max_wait_fill_ms`) → ocultar el botón y mostrar el precio en semillas de la tabla (research/03 C2.2 [V SDK / E precio]); verificación SSV en Cloud Functions antes de acreditar (research/06 §Backend). Duración objetivo ≤30 s (Guru recibe quejas por vídeos de 1:20-3 min, research/08 §6 [V]).

### 6.3 Mediación y segmentación

AppLovin MAX con AdMob, Unity Ads, Meta Audience Network y Mintegral (dirección 13); la mediación añade 20-40 % frente a una sola red (research/04 §2 [S]). Segmentación por `geo_tier`: T1 (US, CA, Europa occidental, AU/JP/KR) y T3 (LatAm salvo CL/UY) solo cambian el **orden** del modal sin saldo (T1: Balsa primero; T3: Puñado primero) y el tope de intersticiales en Q4 (+1 escalón si D1 no cae >1 pp, research/04 §Reglas [E]). La economía en semillas no se segmenta (dirección: no someter la economía núcleo a A/B, research/09 §Mejorar; 03 §14): ni el precio del rescate (600 en todos los países; sin cohorte de 450 en CO/PE/MX: 09 §8 decisión 15), ni el disparador de la Balsa, ni las recompensas. El ajuste por poder adquisitivo va en el precio local del Puñado (4.2).

```
can_show_interstitial(player, session, now):               # claves y defaults: 08 §9 (familia ads_*)
  if not RC.biz_interstitials_enabled: return false
  if player.no_ads or player.level < RC.ads_first_interstitial_level: return false
  if now - player.last_iap < RC.ads_interstitial_suppress_after_iap_h: return false
  if session.interstitials >= RC.ads_interstitial_session_cap or player.interstitials_today >= RC.ads_interstitial_daily_cap: return false
  if now - session.last_interstitial < RC.ads_interstitial_min_gap_s: return false
  if now - session.start < RC.ads_interstitial_session_warmup_s: return false
  if now - session.last_rewarded < 60s: return false
  if session.is_first_session_after_install: return false
  if player.levels_since_interstitial < RC.ads_interstitial_every_n_levels(level): return false
  return context == VICTORY_SCREEN          # jamás en LEVEL_START, RETRY, RESTART, STUCK_MODAL, GIVE_UP_CONFIRM, LEVEL_FAILED ni NO_CANOES (D-008); solo tras toque en Siguiente (victory_autonext_s = 0)
  # Única entrada en AD_GATE sin tocar Siguiente: el avance tras ver entero el vídeo de la Cosecha doble (D-011, punto 4);
  # ahí no sale intersticial porque lo bloquea la regla de 60 s tras un recompensado (línea last_rewarded)
  # Las Canoas infinitas no aparecen en esta función: no suprimen intersticiales (D-008); af_infinite_canoes_1h sí activa la regla de 24 h como cualquier compra
```

## 7. Cumplimiento

| Ámbito | Obligación | Implementación en Tintalú | Evidencia |
|---|---|---|---|
| GDPR / UMP (EEE, UK, CH) | CMP certificada + IAB TCF para anuncios personalizados; sin consentimiento, "limited ads" | UMP SDK del plugin GMA; mensaje GDPR antes del primer anuncio; opción "no vender/compartir" para EE. UU. | research/04 §2 [V]; research/06 [V] |
| ATT (iOS, después) | permiso antes de leer el IDFA | pre-prompt tras la primera victoria; GDPR primero, ATT después; opt-in esperado ≈38-39 % en juegos | research/04 §2 [V] |
| LGPD (BR), Ley 1581 (CO), LFPDPPP 2025 (MX), Ley 29733 (PE) | consentimiento específico/expreso, finalidades separadas, prueba y revocación; UMP **no** cubre LatAm | pantalla de consentimiento propia en el primer arranque (anuncios personalizados / analítica / diagnóstico; "Rechazar" al mismo nivel que "Aceptar"); log de fecha, versión, país y decisión; sin consentimiento → `npa=1`; revocación en Ajustes; política de privacidad PT-BR con DPO | research/08 C5.4 [V] |
| Menores: bandas de edad | sin perfilado publicitario a <18 en Brasil (arts. 22/26 ECA Digital); Play exige pantalla de edad neutral en audiencia mixta y, con público objetivo 13+, no recoger identificadores de quien declare <13; COPPA en EE. UU. desde la Fase 6 | Pantalla de edad neutral en el primer arranque (08 §2.3 Boot) → bandas **{<13, 13-17, 18+}**. **<13**: no se implementa consentimiento parental verificable, así que la banda se **bloquea** con la pantalla "Pide a un adulto que instale el juego" y no se crea sesión de juego; antes de bloquear y en cualquier arranque posterior: `TagForChildDirectedTreatment = true` en GMA/MAX, Analytics apagado (o solo eventos sin ID ni `user_id`), sin Play Games sign-in, sin IAP, sin Remote Config personalizado. **13-17**: anuncios no personalizados (`npa=1`), `maxAdContentRating = G`, PIN de compras activado, sin perfilado ni audiencias. **18+**: flujo de consentimiento normal. La edad declarada se guarda solo en el dispositivo (sin ID) y se puede corregir una vez desde Ajustes con la misma pantalla neutral. No se declara público infantil ni Families (dirección 13) | research/08 C5.1 y C5.4 [V]; research/04 §2 [V]; bandas y bloqueo <13 [P] |
| Verificación de edad (BR) | ECA Digital art. 8 [NV, a confirmar por el abogado brasileño]: la autodeclaración no puede ser el único mecanismo de verificación en servicios con probable acceso de menores; 09 R11 la lista solo como "señal de edad" | La autodeclaración es la primera fuente. Segunda fuente: **Play Age Signals API** (o la señal de edad que Play exponga en 2027) cuando esté disponible en la cuenta; si Play indica <13 o una banda más restrictiva que la declarada, prevalece la de Play y se aplica la fila anterior; si no hay señal, se registra `age_signal_source = self_declared` para que el abogado valore si basta. Tarea en la compuerta de la Fase 5 y partida en el presupuesto legal (09 §2.3) | research/08 C5.1 [V]; [P]; CROSS 08 §13.1 #11 y 09 §2.3 |
| ECA Digital art. 20 (loot boxes) | prohibidas las compras aleatorias en juegos de acceso probable por menores | ninguna compra aleatoria: Canasta gratuita con contenido visible; Cántaro determinista (cantidad exacta mostrada); sin ruleta; Cuaderno de campo sin sobres de pago; bundles con contenido listado | research/08 C5.1-C5.3 [V]; dirección 15 |
| Google Play Payments | odds para ítems aleatorios de pago; programas de fidelidad a ratio fijo | no aplica (nada aleatorio); Cántaro a ratio fijo | research/08 C5.3 [V] |
| IARC / ClassInd | descriptores correctos | cuestionario: publicidad sí, compras sí, ítems aleatorios **no**, azar simulado **no** → ClassInd 14 esperado, Everyone/Teen en Play; sin descriptor de azar (dirección 15) | research/08 C5.2 [E a partir de V] |
| Lei 14.852/2024 art. 17 | compras restringidas a niños por defecto | PIN opcional de compras en Ajustes (activado si la edad declarada es <18) | research/08 C5.1 [V] |
| Play Billing | PBL 8 obligatoria desde 31-08-2026 | Unity IAP 5.x; verificación de recibos en Cloud Functions; "Restaurar compras" | research/06 [V] |
| Minimización de SDK (LGPD) | — | Las 9 integraciones de 08 §7 (MAX + 4 adaptadores, GMA+UMP, Unity IAP, Firebase Analytics/Crashlytics/RC/Auth/App Check/Functions, Play Games, Google Play Plugins PAD/Integrity, EDM4U, Unity Localization; UGS solo en 1.2), cada una con su fila de Data Safety (08 §13.3); sin acceso a contactos (Magic Sort: 30 trackers) | research/09 §Técnico [V]; dirección 15 |
| Analítica pseudonimizada antes del consentimiento (BR/CO/PE/MX) | LGPD art. 7 X / Ley 1581 / LFPDPPP / Ley 29733: ¿cabe el interés legítimo para eventos de juego sin identificador? [NV] | **Consulta al abogado** (misma consulta que 08 pregunta 6): si procede, Analytics arranca en Boot solo con eventos sin `user_id` ni ID de publicidad y el embudo por nivel de 03 §15 y 09 no depende del opt-in; mientras no haya opinión, Analytics espera al consentimiento y solo Crashlytics corre como diagnóstico (08 §2.3). Afecta a la calibración de `econ_sim.py` (3.1) y a las metas de la sección 9 | [P]; CROSS 08 pregunta 6 |

## 8. Ingresos por 1.000 DAU

Supuestos comunes [P]: 85 % Android en LatAm; 2,4 intersticiales por DAU (≈55 % de los DAU han pasado el nivel 22 y ven ≈4/día con la cadencia de 6.1; sin banner ni app open, por eso por debajo de las 3,0 + 25 banners de research/04 §Estimación); 1,5-1,6 recompensados por DAU (5 ubicaciones en 1.0, topes de 6.2; Bitácora x2 solo desde 1.1); IAP neto de la comisión del 15 %; anuncios netos de red. eCPM Android por región de Appodeal Q4-2024 (research/04 §1.4 [V]): intersticial LatAm 1,3-1,9 → 1,6; US 9,6; Europa 3,6; rewarded LatAm 1,8; US 9,0; Europa 5,1; "resto" 1,0/1,2 [E]. La fila "objetivo" usa el eCPM de juegos de EE. UU. de Tenjin×CAS Q2-24 (intersticial 16,15; rewarded 30,25 [V]) y la conversión que exige que el rescate alcance el 25 % del IAP (research/04 §Estimación: sensibilidad "puede duplicarse").

| Escenario | Mix | eCPM blend int / rew | Ads USD/día | Pagadores diarios | ARPPU | IAP bruto → neto | **Total neto/día** | **ARPDAU** | % ads |
|---|---|---|---|---|---|---|---|---|---|
| **LatAm-heavy** (soft launch CO/PE/MX → BR) | 80 % LatAm · 15 % US/EU · 5 % resto | 2,77 / 2,85 | 10,9 | 0,18 % | 2,60 USD (precios al 50 %) | 4,7 → 4,0 | **14,9** (≈450 USD/mes) | **0,015** | 73 % |
| **US-heavy base** | 70 % US · 20 % EU · 10 % resto | 7,54 / 7,44 | 29,3 | 0,40 % | 5,50 | 22,0 → 18,7 | **48,0** (≈1.440 USD/mes) | **0,048** | 61 % |
| **US-heavy objetivo** (gate de UA) | ídem, eCPM Tenjin US; rescate 25 % del IAP; iOS en el mix | 12,12 / 22,31 | 64,8 | 0,55 % | 6,50 | 35,8 → 30,4 | **95,2** (≈2.860 USD/mes) | **0,095** | 68 % |

Sensibilidades: Q4 +20-40 % en anuncios (research/04 §1.4 [V]); cada −0,5 intersticiales/DAU cuesta ≈1,4 USD/día en LatAm y ≈3,8 en US base; cada +0,1 pp de conversión diaria con ARPPU 5,5 suma ≈4,7 USD/día netos. Conversión diaria >0,5 % ya es buena fuera de RPG/estrategia (research/04 §3.2 [V]); Magic Sort reporta ARPDAU 0,16 (Sensor Tower) a 0,29-0,30 (calculado) [V/E] con un modelo IAP-first y UA pagada (research/09 §Ficha). Umbral de equipo: 50 K DAU LatAm-heavy ≈ 22 K USD/mes; 50 K DAU US-heavy base ≈ 72 K USD/mes.

## 9. Metas de ARPDAU por fase

| Fase | Mercados | ARPDAU objetivo (neto) | Condición de paso | Evidencia |
|---|---|---|---|---|
| Soft launch 1 (semanas 1-6) | CO, PE, MX | ≥0,015 meta; piso 0,012 [P] | D1 ≥30 %, D7 ≥10 % (dirección 19); fill rewarded ≥90 %; ≥25 % del ingreso en IAP | tabla 8 |
| Soft launch 2 (semanas 7-14) | + BR | 0,018-0,025 [P] | D30 ≥4 %; conversión Pix medida; sin caída de D7 al activar intersticiales desde el 22 | research/04 §1.5 [V]: emergentes 55-70 % ads |
| Lanzamiento US orgánico | US, CA, UK | ≥0,05 [P] | rating ≥4,6; bloqueados sin recursos al 200 entre 10 y 20 % | tabla 8 fila base |
| UA pagada en US | US | **≥0,08** y D7 ≥15 % (dirección 19) | LTV D90 ≥1 USD, CPI ≤50-70 % del LTV (research/04 §4 [V]) | fila objetivo; CPI puzzle Android ≈2 USD, LatAm 0,14-0,60 [V] |
| v1.2 (Pase Expedición, Liga del río, Regata) | global | +20-30 % sobre v1.0 → 0,10-0,12 US-heavy [P] | pases 6,99-9,99 son estándar hybridcasual (research/04 §3.2 [V]) | — |

## 10. Guardarraíles anti-churn

| Nunca | Por qué | Cómo se garantiza |
|---|---|---|
| Intersticial en reintento, en Volver a la orilla, al inicio de nivel, en el modal Aguas estancadas o en el resto del embudo de D-008 (al rendirse, en ¿Seguro?, en Nivel fallido o en Sin canoas), aunque Magic Sort lo haga (t = 196) | queja n.º 1 del género; política de Play | `can_show_interstitial` solo acepta `VICTORY_SCREEN`; prueba unitaria por contexto |
| Intersticial antes del nivel 22 ni en la sesión de instalación | onboarding de Magic Sort (research/09 §Qué copiar 6 [V]) | `ads_first_interstitial_level` con mínimo 22 en RC (08 §9) |
| Cambiar el tablero sin avisar | queja estructural de Magic Sort | reintento con la misma semilla; variante aliviada solo tras 3 fallos y con "El río bajó" (dirección 7); test de determinismo |
| Cuia de rescate, Balsa de rescate, Canoas o cualquier oferta de pago antes del nivel 31 (Mochila de inicio en la ventana 10-20, nunca en la primera sesión, es la única excepción) | dirección 10-11 | `rescue_min_level` (debut común de Canoas, Cuia de rescate y Salto del Boto) y `skip_min_level` con mínimo 31 en RC (08 §9) |
| Más de 1 Cuia de rescate en el mismo nivel, o más de 16 cuias en pantalla | 02 R1/§14: 14 + 1 Cuia extra + 1 Cuia de rescate = 16 es el máximo que cabe a 360 × 780 px | `rescue_max_per_level` = 1 con rango 1 en RC; en el 2.º bloqueo el modal omite "Continuar" y ofrece Marea y el pie (Volver a la orilla en 1-30; «Rendirse» desde el 31, D-008), y ¿Seguro? tampoco lo ofrece; prueba unitaria del layout con 16 cuias |
| Precio del rescate o disparador de la Balsa distintos por país | la economía núcleo no se segmenta (research/09 §Mejorar; 03 §14) | `rescue_price_bands` sin condición de geo en RC; solo `stuck_order_t1` / `stuck_order_t3` depende de `geo_tier` |
| Vender giros, cofres o cualquier contenido aleatorio | ECA Digital art. 20 | revisión legal de cada SKU nuevo; lista blanca de tipos de SKU |
| Subir precios de comodines sin subir recompensas en la misma versión | causa n.º 1 de reseñas 1-2★ por economía (research/03 §4 [V]) | cambio de precio en RC exige cambio de `eco_reward_normal` / `eco_reward_hard` / `eco_reward_very_hard` en el mismo rollout; notas de versión públicas |
| Cobrar el rescate antes de dejar usar Marea | queja de Magic Sort (research/09 §Comodines [V]) | orden fijo del modal: Marea → Cuia de rescate → pie (Volver a la orilla en 1-30; «Rendirse» desde el 31, D-008) |
| Más de 1 takeover por sesión fuera del bloqueo | pop-ups apilados de Magic Sort | `offer_scheduler`. Las pantallas del embudo de D-008 (¿Seguro?, Nivel fallido y Sin canoas) son parte del bloqueo, no takeovers |
| Racha que expira por reloj | "perdí una racha de 19 por dormir" (research/09 §Mejorar [R]) | la Crecida solo cae por fallo duro no escudado (04 §5.2) |
| Rewarded que no paga o que redirige a la tienda | 33 % de negativas de Magic Sort | SSV + acreditación local provisional si el servidor no responde en 5 s; reintento |
| Anunciar "Sin anuncios" y seguir mostrando alguno | queja n.º 3 del género (research/05 §6 [V]) | Sin anuncios = 0 intersticiales; los recompensados llevan etiqueta "opcional" |
| Etiquetas Difícil/Muy difícil falsas o niveles sin solución sin comodín | reseñas de Magic Sort (research/09 §Mejorar) | solver obligatorio en pipeline; bot casual ×40 fija la etiqueta |

## Parámetros configurables (Remote Config)

**Ver 08 §9.** Es la única tabla de Remote Config del GDD (clave, tipo, default compilado, rango y capítulo dueño); este capítulo no repite claves ni valores: los valores de diseño que justifican cada default están en las secciones indicadas abajo y 08 §9 los registra. Los alias de versiones anteriores de este capítulo ya no existen (`reward_*`, `chest_seeds`, `chest_first_seeds`, `chest_cycle`, `calendar_table_v1`, `daily_challenge_seeds`, `rescue_base_price`, `rescue_from_level`, `rescue_iap_daily_cap`, `rescue_order_t1/t3`, `extra_cuia_price`, `hint_price`, `reveal_price`, `shuffle_price`, `skip_price`, `undo_recharge_price`, `shuffle_per_level/day`, `skip_after_fails`, `lives_*`, `first_interstitial_level`, `interstitial_*`, `rv_cap_*`, `rv_cooldown_s_*`, `rv_global_daily_cap`, `rv_hint_from_level`, `starter_*`, `noads_card_trigger`, `piggy_*`, `offer_takeovers_per_session`, `price_tier_*`, `econ_model_version` sin prefijo, `eco_streak_rewards`).

| Sección de este capítulo | Claves canónicas (nombres exactos de 08 §9; dueño entre paréntesis si no es 05) |
|---|---|
| 2.1 Fuentes | `eco_reward_normal` / `eco_reward_hard` / `eco_reward_very_hard`; `eco_calendar_table_v1`; `eco_calendar_double_ad_enabled` (06; false en 1.0, `rv_daily_chest` en 1.1); `eco_basket_seeds`; `eco_basket_cycle` (06); `live_daily_challenge_reward` (06); `live_unlock_level_daily` (06); `ads_rv_double_harvest_from_level`; `lvl_labels_from_level` / `lvl_very_hard_from_level` (03: 40 / 60, base del debut de las victorias Difícil y Muy difícil) |
| 2.4 Crecida | `streak_milestones` (**clave única**, misma en 06 §4.3 y 08 §9; sin `eco_streak_rewards`), `streak_daily_cap`, `streak_hits_once_per_day`, `streak_unlock_level`, `streak_shield_per_day` (04) |
| 2.2 / 3.3 / 5 Cuia de rescate y Balsa | `rescue_price_bands` (**clave única** con 02 §7 y 04 §6.2; valor canónico `[[31,600]]`, ver 3.3), `rescue_first_day_mult`, `rescue_max_per_level` (04; 1, rango 1), `rescue_min_level` (04; debut común de Canoas, Cuia de rescate y Salto del Boto), `stuck_order_t1` / `stuck_order_t3` (04; orden Balsa/Puñado por `geo_tier`), `af_rescue_daily_cap` / `af_rescue_hide_after_declines` (04) |
| 2.2 Comodines y Canoas (04) | `undo_free_per_level`, `undo_unlimited_until_level`, `undo_refill_amount`, `undo_refill_seed_price`, `extra_cuia_unlock_level`, `extra_cuia_seed_price`, `extra_cuia_max_per_level`, `hint_unlock_level`, `hint_seed_price`, `reveal_seed_price`, `shuffle_seed_price`, `shuffle_max_per_level`, `shuffle_max_per_day`, `skip_seed_price`, `skip_min_fails`, `skip_min_gap_levels`, `skip_min_level`, `relief_after_fails` (03), `canoes_max`, `canoes_regen_min` |
| 4.1 / 5 Catálogo y ofertas | `eco_seed_pack_amounts`; `eco_starter_window_levels`, `eco_starter_timer_h`, `eco_starter_first_show`; `eco_noads_bonus_seeds`, `eco_noads_card_trigger`; `eco_piggy_from_level`, `eco_piggy_fill`, `eco_piggy_cap`, `eco_piggy_min_buy`; `ui_max_takeovers_per_session`; `eco_price_tier_<country>` (solo registro: los precios viven en Play Console); `biz_iap_enabled` (01) |
| 6.1 Intersticiales | `biz_interstitials_enabled` (01; único interruptor), `ads_first_interstitial_level`, `ads_interstitial_every_n_levels`, `ads_interstitial_min_gap_s`, `ads_interstitial_session_warmup_s`, `ads_interstitial_session_cap`, `ads_interstitial_daily_cap`, `ads_interstitial_suppress_after_iap_h`; `victory_autonext_s` (02; 0: el intersticial solo tras toque en Siguiente) |
| 6.2 Recompensados | `biz_rewarded_enabled` (01), `ads_rv_cap_<placement>`, `ads_rv_cooldown_s_<placement>`, `ads_rv_global_cap_day` (04), `ads_rv_max_wait_fill_ms` (08) |
| 3 Simulación | `eco_model_version` = **`B-2026-10-06e`** (e = `on_fail` con la regla del contrato 19: un fallo duro con la barra a 0 apaga el río crecido). **Cerrada (06-10):** 08 §9 lo registra como default. Identifica el escenario B del script del repositorio: curva canónica de 03 §12, tabla de fallo de 03 §15, rescate 600 fijo, hito 6 = Canasta, regalos del tutorial, Canasta de ciclo fijo y `on_fail` según §2.4 |
| 7 Edad y consentimiento | `biz_legal_profile_by_country`, `biz_under18_no_personalization` (01), `consent_text_version` (08); `consent_under13_policy` (`block` en 1.0 · `parental_consent` reservado: bloqueo con "Pide a un adulto que instale el juego" + TFCD + Analytics sin ID) y `consent_age_signal_source` (`self_declared` · `self_declared+play_age_signals`). **Cerrada (06-10):** ambas dadas de alta en 08 §9 (familia `consent_`, dueño 05) |

## Desviaciones de la dirección

1. **PENDIENTE DE VALIDACIÓN, no aplicada en 1.0: precio de la Cuia de rescate por tramo (600 / 750 / 900) en lugar del único valor por defecto de 600.** El escalado 600/750/900 bloquea sin recursos al casual en el 100 % de las corridas (21,3 bloqueos, muro hacia el 198) y al medio en el 45 % con neto −34/día (3.2), aunque lleve el ratio de referencia de 101-500 de 7,2 a 10,2:1; eso viola el principio 3 (ruta gratuita real), y 3.6 muestra que ningún precio único, ni los recortes de fuentes, equilibran a la vez al casual y al medio. Por eso 1.0 sale con el valor por defecto de la dirección 10 (`rescue_price_bands` = `[[31,600]]`) y el escalado queda como hipótesis: se reconsiderará solo si el embudo del soft launch mide intentos medios por debajo de los objetivos de 03 §15 o un saldo medio al nivel 200 >8.000 semillas, y siempre respetando la guarda complementaria de 1.1 (medio sin bloqueos y con neto ≥0). Mientras tanto el ratio nominal de referencia queda en 7,2-7,7:1, por debajo de la banda 10-15:1 de research/09 §Mejorar, que este capítulo acepta como consecuencia de aplicar la dirección literal y de leer la presión por frecuencia de fallo y no solo por precio nominal.
2. Ninguna otra. "Cada hito de la Crecida se paga una vez al día" (`streak_hits_once_per_day` = true) y "hito 10 sin semillas" ajustan la propuesta de research/04 C1 §3 [E] y la tabla de hitos de 06 §4.3, no la dirección (que fija 300 por racha de 3 y el escudo, ambos conservados); el hito 6 es la Canasta del glosario §6.3 (contenido visible), no una Cuia extra. La cantidad de la Canasta, el premio del Reto del río y los topes de recompensados se toman de 04 y 06 y están dentro de lo que la dirección deja abierto. La Mochila de inicio se muestra por primera vez en la primera victoria con nivel ≥12 dentro de una sesión ≥2 aunque la ventana de la dirección 14 abra en el 10 (cero ofertas de pago en la primera sesión, 06). El bloqueo de la banda <13 (7) desarrolla la dirección 13 ("no declarar público infantil"), no la contradice.

## Conflictos detectados con capítulos escritos en paralelo

Los capítulos 01, 02, 04 y 06 se redactaron a la vez que este. Donde difieren, este capítulo propone la resolución y el dueño del producto arbitra.

| Tema | Otro capítulo | Este capítulo | Resolución propuesta |
|---|---|---|---|
| Precio de la Cuia de rescate | 01 §tabla de precios, 02 §7, 04 §6.2 (`rescue_base_price`), 06 y 09 §1.1: 600 fijo (300 el 1.º del día) · 09 §8 decisión 15: por tramo + 450 como cohorte de prueba en CO/PE/MX | **600 fijo** (`rescue_price_bands` = `[[31,600]]`); sin cohorte geográfica | **Resuelto (06-10)**: una sola clave `rescue_price_bands` = `[[31,600]]` en 02 §7, 04 §6.2, 05 y 08 §9, sin `rescue_base_price` en ningún capítulo; 09 §8 decisión 15: sin segmentación geográfica del precio en semillas (el ajuste LatAm va en el precio local del Puñado) |
| Rescates por nivel y cuias en pantalla | 02 R1/§14: máximo 16 cuias, `rescue_max_per_level` = 1 · 04 §2.2/§3/§6.2: máx. 2/nivel y "1 Cuia extra + hasta 2 Cuias de rescate = 3" (= 17 cuias) | `rescue_max_per_level` = **1**; tope total de cuias añadidas = 2 | **Resuelto (06-10)**: 1 por nivel (02 manda por layout); 04 §2.2 fija el tope total de cuias añadidas por nivel en 2 (1 Cuia extra + 1 Cuia de rescate), su rama A' (2.º bloqueo del mismo nivel) ofrece solo Marea y Volver a la orilla, y `rescue_max_per_level` = 1 rige en 04 §3, §6.2 y 08 §9 |
| Balsa de rescate: contenido y disparador | 04 (versión previa): rescate + 800 semillas + Canoas infinitas 30 min + 3 Marea; disparo al 1.er bloqueo sin saldo si `balsa_ok` · glosario §5.1: "semillas, una cuia y canoas infinitas" · 05 (versión previa): T1 desde el 1.er fallo, T3 "desde el 2.º" | rescate aplicado + 1.000 semillas + 1 Cuia extra + Canoas infinitas 30 min; disparador único (sin saldo para el rescate, nivel ≥31, 1 impresión/día, oculta 48 h tras 2 rechazos; T1/T3 solo cambian el orden) | **Resuelto (06-10)**: 04 §6.2 `BALSA_499` adopta el contenido de 05/09 y 04 §6.1/§6.3 el disparador único (T1/T3 solo cambian el orden); el glosario §5.1 usa la frase "Todo para seguir: una cuia ahora, 1.000 semillas, una Cuia extra y canoas infinitas 30 min" |
| Verificación y bandas de edad | 08 §2.3 Boot y §13.1 #11: pantalla neutral de autodeclaración como único mecanismo; 09 R11: "señal de edad" | bandas {<13, 13-17, 18+} con bloqueo de <13 + TFCD; Play Age Signals API como segunda fuente en BR (7) | **Resuelto (06-10)**: 08 §2.3 (Boot) y §13.1 #11 recogen las bandas y el bloqueo <13 con TFCD; 09 §2.3 (compuerta de la Fase 5) y §5 (partida legal) incluyen la señal de edad de Play y la consulta al abogado sobre el art. 8; 08 §9 da de alta `consent_under13_policy` = "block" y `consent_age_signal_source` = "self_declared" |
| Escalera de hitos de la Crecida | glosario §6.3, 01 §4 y §7.2, 06 §4.3 (RC `streak_milestones`), 08 (RC `eco_streak_rewards`) y 09 §1.1 #16: hito 10 = 300 semillas + río crecido, hitos repetibles hasta 600/día · 04 §7.1/§8: hito 6 = "Canasta (o Canoas infinitas 15 min)" · versión anterior de este capítulo: hito 6 = 1 Cuia extra | **Fila única adoptada:** 3 → 300 semillas · 6 → Canasta (contenido visible) · 10 → 1 Cuia extra + río crecido; `streak_hits_once_per_day` = true; tope diario 600 | **Resuelto (06-10)**: fila canónica de §2.1/§2.4 con **clave única `streak_milestones`** (`eco_streak_rewards` no existe), ya copiada en glosario §6.3, 01, 04 §8, 06 §4.3, 08 §9 y 09 §1.1 #16; tras el hito 10 la barra vuelve a 0 y el río crecido sigue hasta el siguiente fallo duro no escudado, y el tope diario suma solo el hito 3 y las 50 de la Canasta (§2.4). Necesaria para el ratio: con 300 en el hito 10 y hitos repetibles (escenario A de 3.2) la referencia baja de 7,7 / 7,2 a 7,0 / 6,6:1 y el medio acumula 23.815 semillas sin uso [sim] |
| ARPDAU objetivo LatAm | 01 §métricas: 0,025-0,035 en fases 1-2 (toma los ≈23 USD/1.000 DAU de research/04, que incluyen banner y app open) | 0,015 base sin banner ni app open (dirección 13); 0,018-0,025 en fase 2 | **Resuelto (06-10)**: 01 y 06 §12 adoptan la tabla de la sección 9 como fuente única (≥0,015 LatAm fase 1; 0,018-0,025 fase 2; ≥0,05 US orgánico; ≥0,08 US antes de UA) |
| Primer nivel Difícil | 03 §11/§12 y 06: 40 (y 60 primer Muy difícil) · versión anterior de este capítulo (§2.1/§3.1): "30 y 40 Difícil" | 40 y 60 | **Cerrada (2026-10-06; incidencia 6)**: §2.1 y §3.1 corregidos y simulación recorrida con el script del repositorio, que integra la curva canónica de 03 §12 (modelo `B-2026-10-06e`) |
| Mochila de inicio: primera tarjeta | 02 §13 "si llega antes" · 06 "lo que ocurra después" · versión anterior de este capítulo: "tras la victoria del 10" | primera victoria con nivel ≥12 dentro de una sesión ≥2; ventana 10-20 | **Cerrada (2026-10-06; incidencia 11)**: `eco_starter_first_show` = "L12_or_session2" en §4.1 y §5; 02 §13 adopta el mismo literal |
| Banda del ratio de presión | 01 §dependencias (versión previa): "≈8-12:1" | banda objetivo 10-15:1 (§1.1; research/09 §Mejorar); referencia [sim] 7,7 / 7,2:1 (31-100 / 101-500), aceptada por debajo de la banda en 1.0 (Desviación 1) | **Resuelto (06-10)**: 01 (Dependencias, fila 05) adopta la banda 10-15:1 de §1.1 con la referencia actual 7,2-7,7:1 [sim, 3.2]; con el rescate a 600 fijo no hay tramo introductorio que documentar (Desviación 1) |
| Ingreso efectivo por victoria usado para "victorias para pagarlo" | 04 §3/§9: 38 semillas/victoria | 61 (casual) · 98 (medio) [sim 2026-10-06: 61,4 y 97,7] | **Resuelto (06-10)**: 04 §3 y §9 usan 61 (casual) y 98 (medio): un rescate de 600 = 9,8 victorias del casual, 6,1 del medio; 300 con descuento = 4,9 / 3,1 |
| Canoas infinitas 1 h en Sin canoas | 04 §7.3 (wireframe y orden de opciones, sin cuenta atrás de oferta); la listan sin condición 02 §2, 06 §4.7 y el paso 4 de D-008. Así lo implementa el código de la pista B | §4.1 y §5: solo al quedarse sin Canoas tras rechazar Canoa extra dos veces en el día; 1 impresión contextual al día que caduca a los 10 min; siempre en el Mercado | **Abierto (2026-10-08): P17** de `docs/dev/decisiones.md` (D-008). Propuesta [P]: siempre visible, como 04 §7.3, y se corrigen §4.1 y §5. Decide el dueño; se alinea en el lote Dc4 y el código no cambia hasta la respuesta |
| Tope de Cosecha doble | 04 §3: 5/día | 5/día (alineado) | — |
| Canasta, Reto del río, Bitácora | 06 §4.1, §4.2, §4.4 | iguales (alineados) | — |

## Preguntas abiertas para el dueño del producto

1. **Cerrada (06-10): Cuia de rescate a 600 fija en 1.0** (dirección 10; `rescue_price_bands` = `[[31,600]]`, 300 el primer rescate del día, máx. 1 por nivel, sin tramos 750/900 ni cohorte geográfica), aceptando un ratio nominal de referencia de 7,2-7,7:1, por debajo de la banda 10-15:1, a cambio de (casi) cero bloqueos sin recursos del no pagador medio (0,0 por corrida; 2 % de corridas [sim]). La misma revisión cerró la escalera de la Crecida, la Mochila de inicio, el primer Difícil y la banda del ratio en 01 (ver Conflictos). Sigue abierto solo aprobar el criterio de reconsideración del escalado 750/900 tras el soft launch (Desviación 1).
2. **Sumidero cosmético por semillas (decisión que pide 3.6): ¿sí o no, y a qué rango de precio?** Con el rescate a 600 fijo el medio no pagador termina el nivel 500 con 15.510 semillas sin uso y el hábil con 26.408, mientras el casual acaba con 1.842 [sim 2026-10-06]; ningún precio único del rescate equilibra ambos perfiles (3.6), y el medio del modelo ya cierra el nivel 100 con 5.331 semillas, por encima de la palanca "saldo medio al 100 >4.000" de 3.5. Propuesta [P] de 3.6: skins de cuia y de mundo canjeables por semillas en un rango orientativo de 3.000-8.000 (el backlog de 09 §3.1 tiene marcos del Mapa del río a 2.000-5.000), más Cuia extra y Morpho ofrecidos antes del nivel; decidir también si entra en 1.0 o en v1.1. Hasta que se decida no cambia el catálogo IAP (las skins siguen a 1,99 USD y gratis al completar el mundo, 4.1) ni ninguna otra decisión de diseño.
3. **Cerrada (2026-10-06).** Contenido de la Balsa de rescate (rescate aplicado + 1.000 semillas + 1 Cuia extra + Canoas infinitas 30 min) y disparador único (sin saldo para el rescate, nivel ≥31, 1 impresión/día, oculta 48 h tras 2 rechazos; T1/T3 solo cambian el orden Balsa/Puñado) fijados en §4.1 y §5, alineados con 04 §6.2 y 09 §8 decisión 16.
4. Confirmar la Mochila de inicio (500 semillas + 2 Cuia extra + 3 Morpho por 0,99) frente a una variante de 2,99 con más contenido (Screwdom vende su beginner bundle a 9,99 [V]).
5. Precios mínimos por país en Play Console [NV]: si 9 MXN o R$ 2,49 no están permitidos, ¿se sube la Mochila a 19 MXN / R$ 4,99 o se elimina el escalón de 0,99 en esos países?
6. Sin anuncios: 4,99 fijo o prueba 6,99 en una cohorte (research/04 propone el test); y si incluye, además del bono de 500 semillas, una Cuia extra diaria (research/05 §Implicaciones 4 lo sugiere; la dirección 14 solo habla de bono de semillas).
7. Retención fiscal en Brasil (IRRF + CIDE ≈25 % [S]): confirmar con contador; afecta al precio BR y al umbral de 1 M USD de comisión del 15 %.
8. Tasas de fallo por banda: desde el 2026-10-06 este capítulo usa las derivadas de los intentos medios objetivo de 03 §15 y la cadencia 40/60 de 03 §11/§12 (3.1). Volver a correr `econ_sim.py` con los intentos medidos del bot casual ×40 (03 §6) y, en soft launch, con el embudo real; si el casual real falla menos que el objetivo, revisar la Desviación 1. El script del repositorio, `tools/econ_sim.py`, ya integra la tabla de 03 §15 (`FAIL_BASE`) y la curva canónica de 03 §12 (`difficulty_tag`) y, junto con `tools/econ_variants.py` para 3.6, reproduce todas las cifras [sim] de este capítulo; `tools/economy-sim` (08) debe partir de él.
9. **Cerrada (06-10).** Con el precio único de 600 (`rescue_price_bands` = `[[31,600]]`, sin tramos) el descuento del primer rescate del día es siempre 300 y el mensaje "primer rescate a 300" es constante. Si en el futuro se reactivaran tramos (Desviación 1), habrá que decidir entonces si el descuento sigue al 50 % del tramo o se congela en 300.
10. Canoas infinitas 1 h a 1,99 como SKU suelto en 1.0, o solo dentro de la Balsa de rescate y el Pase Expedición hasta medir el uso de Canoa extra. (El ID es `af_infinite_canoes_1h` en todos los capítulos, incidencia 16; la pregunta es solo si se vende suelto.)
11. Banda <13 de la pantalla de edad: 1.0 sale con el bloqueo "Pide a un adulto que instale el juego" (`consent_under13_policy` = "block" en 08 §9; propuesta de 7, sin consentimiento parental); queda por decidir si 1.1 implementa consentimiento parental verificable para no perder instalaciones en BR. Y ¿basta la Play Age Signals API como segunda fuente frente al art. 8 del ECA Digital? (abogado brasileño, compuerta de la Fase 5).
12. Analítica pseudonimizada por interés legítimo (7, última fila): ¿puede Firebase Analytics registrar eventos de juego sin identificador antes del consentimiento en BR/CO/PE/MX? Misma consulta al abogado que 08 pregunta 6; si la respuesta es no, el embudo por nivel que calibra `econ_sim.py` y las metas de la sección 9 solo cubre a quienes aceptan analítica, y hay que corregir el sesgo.

## Dependencias con otros capítulos

| Capítulo | Toma de este capítulo | Devuelve a este capítulo |
|---|---|---|
| 00 Glosario | nombres de SKU, comodines, eventos y sistemas; IDs `af_*` (`af_infinite_canoes_1h` como ID canónico, §11; packs de skin `af_skin_m02` … `af_skin_m04`, sin pack del mundo 1, 4.1) | Sin pendientes (Resuelto 06-10) |
| 01 Visión y posicionamiento | metas de ARPDAU por fase (sección 9), umbral de UA en US, mix ads/IAP por región; banda del ratio de presión (§1.1) | Sin pendientes (Resuelto 06-10): 01 adoptó la sección 9 (≥0,015 LatAm fase 1; ≥0,08 US antes de UA) y la banda 10-15:1 de §1.1 con la referencia actual 7,2-7,7:1 [sim] (01, Dependencias fila 05) |
| 02 Núcleo, reglas y UX | orden del modal Aguas estancadas (Marea → Cuia de rescate → Volver a la orilla en 1-30 o «Rendirse» desde el 31, con el embudo de D-008); persistencia de la cuia extra al reiniciar; Marea usable en el bloqueo (ilimitada en 1-10); máximo 16 cuias en pantalla (R1/§14); tutorial 1-12 (§13) con la Mochila en "L12_or_session2" | detección de "sin vertidos legales" (§1.4). **Cerrada (06-10):** `rescue_price_bands` = `[[31,600]]` como clave única en §7 (sin `rescue_base_price`), `rescue_max_per_level` = 1 y AD_GATE solo tras toque en Siguiente (`victory_autonext_s` = 0) |
| 03 Niveles, generación y dificultad | cadencia del ciclo de 20, calendario de etiquetas (§11/§12: 40 primer Difícil, 60 primer Muy difícil) e intentos medios objetivo de §15 (base de la tabla de fallo de 3.1) | intentos medidos por banda del bot casual ×40 y del embudo del soft launch para recalibrar 3.1 y volver a correr `econ_sim.py` |
| 04 Comodines, vidas y rescate | precios de los 6 comodines y excepciones de tutorial (Marea ilimitada en 1-10, Cuia extra regalada en el 8, 3 Morphos en el 12), IDs y topes de recompensados (6.2), variante aliviada tras 3 fallos, Salto del Boto tras 5, definición de fallo (§5.2), contenido de la Balsa (`BALSA_499`), IDs `af_rescue_daily_cap` / `af_rescue_hide_after_declines`, SKU `af_infinite_canoes_1h` (§7) | Pendiente (2026-10-08): P17, cuándo sale `af_infinite_canoes_1h` en Sin canoas (04 §7.3 frente a §4.1 y §5; Conflictos). El resto, sin pendientes (Resuelto 06-10) |
| 06 Meta, retención y live-ops | Bitácora del río (tabla 2.3), Canasta 50 + comodín (`eco_basket_cycle`), Reto del río 60 (`live_daily_challenge_reward`), Cántaro, Pase Expedición; primera sesión sin ofertas de pago; ofertas de skin al entrar al mundo solo desde el mundo 3 (sitio pendiente: pregunta 39 de GDD.md §6.3); `rv_daily_chest` en 1.1 | calendario de eventos v1.2 (Regata, Travesía, Encuentro de las Aguas, Temporada de lluvias) con premios que respeten el tope de la Crecida |
| 07 Arte, temática y audio | iconos de semilla, Canoa, Cántaro, Balsa de rescate, Mochila de inicio; animación de Capi en la Cuia de rescate; precio del rescate "600" en §5.5/§7.5 | hojas de estilo del Mercado y del modal |
| 08 Arquitectura técnica y publicación | tabla única de Remote Config (§9) con las claves de este capítulo; SKUs de Unity IAP y `iap_catalog.json`; placements de MAX; las 5 Cloud Functions (SSV, verificación de compras, `restoreLedger`, `voidedPurchasesJob`, `deleteAccount`); pantalla de consentimiento propia (§2.3 Boot); checklist de publicación §13 (textos IARC/ClassInd, consentimiento LatAm, disclaimer "sin relación con Amazon.com"); bandas de edad {<13, 13-17, 18+} con bloqueo de <13, TFCD y Analytics sin ID (7) para el Boot de §2.3 y la checklist §13.1 #11 | **Cerrada (06-10)** en §9: `eco_model_version` = "B-2026-10-06e" y alta de `consent_under13_policy` ("block") y `consent_age_signal_source` ("self_declared"). Límites de Remote Config (3.000 parámetros), tiempos de precarga, costes de backend; textos legales finales; integración de Play Age Signals API cuando esté disponible; opinión del abogado sobre analítica por interés legítimo (su pregunta 6 = pregunta 12 de este capítulo); `tools/economy-sim` con la tabla de 03 §15 y la cadencia 40/60 (pregunta 8) |
| 09 Roadmap, equipo, KPIs y riesgos | eventos `af_stuck_shown` / `af_stuck_choice` / `af_stuck_outcome`, `spend_virtual_currency`, `purchase` y `ad_impression` (taxonomía única de 08 §8.2); metas de ARPDAU por fase (sección 9); precio único del rescate sin cohorte geográfica; SKUs por versión (1.0: 13 SKU, todos los de 4.1 salvo Pase Expedición, sin los reservados `af_coins_1`, `af_coins_7` ni `af_noads_bundle`; 1.2: Pase; 2.0: Prestar una canoa) | embudo por nivel, bloqueados sin recursos reales, intentos medios por banda, calibración de `econ_sim.py`; estimación de esfuerzo de tienda, consentimiento y backend |
| 01 §9 Marca | nombre comercial Tintalú (sustituye a Aquazonia desde el 2026-10-06) y disclaimer | opinión legal sobre la marca (los textos de ficha viven en 08 §13; no existe un capítulo "ASO y legal") |
