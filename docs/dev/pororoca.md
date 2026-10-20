# Pororoca · Barajar (bloque B2 de la tanda 5)

Nota de diseño e implementación del equipo Spark, rama `spark/t5-b2` (2026-10-20). Fuente: `docs/dev/plan-tanda5.md` §4, fila B2; D-033 (el dueño eligió la (a) en todas las preguntas de la tanda 5; ninguna toca la Pororoca).

**Canon que se implementa:**
- 04 §2.4: la ficha entera (función, regla de uso, desbloqueo, usos gratis, precio, HUD, VFX y casos borde a-g), §3 (tabla), §4 (calendario: «30 · Pororoca · Tarjeta con animación») y §11 (`af_booster_used`, `af_booster_denied` y `af_shuffle_result`).
- 03 §16 (párrafo «Pororoca») y §5 (tabla de usos del solver: 40.000 nodos, w = 2, par' ≤ par + 2).
- 02 §9.2 (columna Pororoca de cada mecánica), §11 T6 y §14 («Pororoca: vacía el historial de Marea (nuevo tablero); conserva stock»).
- 08 §9: `shuffle_unlock_level` = 30, `shuffle_seed_price` = 300, `shuffle_max_per_level` = 1, `shuffle_max_per_day` = 3, `shuffle_par_slack` = 2, `shuffle_budget_nodes` = 40.000 y `shuffle_reveals_murky` = true.
- 07 §7.4 (orden de la bandeja: Marea, Cuia extra, Pista, Barajar y Revelar; 56 dp con 5 ranuras), la fila «Pororoca (Barajar)» de los iconos (ola grande con dos flechas cruzadas, turquesa) y `sfx_shuffle`.
- Glosario §5.1 y §10: «Pororoca», etiqueta «Barajar», ID `shuffle` y el lema «La ola remueve el río sin tocar las cuias llenas.».
- 06 §4.1: en el Reto del río valen los comodines de la campaña, también la Pororoca.

## 1. Qué hay y dónde

| Pieza | Dónde | Cómo llega |
|---|---|---|
| Barajado: reconstrucción inversa, A* de verificación y Permutar + verificar | `Packages/com.amazonfill.core/Runtime/AF.Solver/Shuffle/` (`Shuffler`, `ShuffleOptions`, `ShuffleResult`) | parche `B2-solver.diff` |
| `LevelSession.ApplyShuffle` (revela, vacía Marea y conserva `Initial`) | `AF.Core/Session/LevelSession.cs` | parche `B2-solver.diff` |
| Tests compartidos (NUnit 3.5) y de asignaciones (DotnetOnly) | `Tests/Editor/AF.Solver.Tests/ShufflerTests.cs`, `AF.Core.Tests/LevelSessionShuffleTests.cs`, `dotnet/AF.Core.Tests/DotnetOnly/ShuffleAllocationTests.cs` | parche `B2-solver.diff` |
| Comodín: cifras de 08 §9, oferta del botón, cobro, hoja de ≥ 200 | `Assets/_Project/Scripts/Boosters/ShuffleBooster.cs` (`ShuffleConfig`, `ShuffleOffer`, `ShuffleOutcome`) | **rama** |
| Aviso a la vista | `Scripts/GameFlow/IShuffleListener.cs` | **rama** |
| Envoltorio del solver en el cliente | `Scripts/Boosters/ShuffleWave.cs` | parche `B2-flow.diff` |
| La ola en el flujo, topes, guardado y tarjeta del 30 | `Scripts/GameFlow/LevelFlow.Shuffle.cs` (nuevo) y ganchos en `LevelFlow.cs`, `LevelFlow.Resume.cs` y `LevelStateSnapshot.cs` | parche `B2-flow.diff` |
| `daily.shuffles_today` al cobrar | `Scripts/Economy/Wallet.cs` | parche `B2-flow.diff` |
| Iconos y la ola | `Scripts/Presentation/ProceduralArt.Shuffle.cs` | **rama** |
| Textos, hoja, tarjeta del 30 y la ola de 1,2 s | `Scripts/UI/ShuffleText.cs`, `ShuffleConfirm.cs`, `ShuffleIntroCard.cs`, `ShuffleWaveFx.cs`, `IShuffleActions.cs` | **rama** |
| Ranura de la bandeja | `Scripts/UI/BoosterTray.cs` ✱ | parche `B2-ui.diff` |
| La ola en la pantalla y su hoja | `Scripts/UI/GameScreen.Shuffle.cs` (nuevo) y ganchos en `GameScreen.cs` ✱ | parche `B2-ui.diff` |
| El botón en la campaña y en el Reto | `GameBootstrap.Shuffle.cs` (nuevo) y `DailyChallengeScreen.cs` | parche `B2-ui.diff` |
| Textos nuevos | `docs/dev/traspaso/spark-enganches/B2-strings.csv` (8 claves, `pendiente_asesor`) | el integrador las copia a `strings.csv` |

Lo de la rama compila sin los parches; lo que depende de `AF.Solver.Shuffle` o de `LevelSession.ApplyShuffle` va en los parches, también los archivos nuevos.

## 2. El barajado (AF.Solver)

1. **La ola revela** (04 §2.4 a y g; 02 T6): el barajador trabaja con todas las dosis ocultas visibles y devuelve un tablero sin ocultas; `LevelSession.ApplyShuffle` las marca reveladas (I5).
2. **Cuias fijas** (04 §2.4 y b, g): las selladas, las que llevan Barro y las que aún tienen una mecánica: Hoja o Brote cerrados, Liana atada, Matapi con su contenido, Flor cerrada, Hoja atada [P-1], la Tinaja y su cuia colgada. Una mecánica abierta o suelta deja la cuia normal, así que entra en la ola; la cuia suelta de la tinaja, también. El Remolino participa (04 §2.4 c) y conserva su pareja. La Cuia extra se conserva como vacío (e).
3. **Estado resuelto parcial** (03 §16): cada color de las cuias movibles, reunido en la cuia movible libre más pequeña que lo aloja, del color con más dosis al de menos [P-3]. Si alguno no cabe, se pasa directamente a Permutar.
4. **Paseo inverso** de hasta k = max(12, 3 × colores movibles) movimientos (`InverseMoves`, los del generador). Elige al azar entre los no visitados y se queda con el estado válido más profundo [P-3]. Un estado es válido si tiene los mismos vacíos que ahora, ninguna cuia movible llena y monocolor (04 §2.4: más estricto que «sellada» de 03 §16 [P-3]) y no es el tablero actual. El paseo se da sobre cuias normales de la misma capacidad, y el Remolino lo verifica después el A* con las reglas completas.
5. **Verificación** (04 §2.4; 03 §5): A* con 40.000 nodos y w = 2 sobre el tablero entero, sin poda de fútiles si hay Tinaja. El par de antes y el de después se miden igual. Si el A* no resuelve el tablero de partida dentro del presupuesto, basta con que resuelva el nuevo [P-3].
6. **Respaldo** (04 §2.4): tras 20 intentos inversos, hasta 20 de Permutar + verificar [P-2], con los mismos filtros. Permutar baraja las dosis movibles y las reparte con el mismo número por cuia. Si nada sale: `failed`, sin tablero, y no se cobra.
7. **Determinismo**: solo enteros y el PRNG del núcleo. La semilla es `Shuffler.SeedFor(semilla del nivel, tablero revelado, ordinal)` [P-11]: rehacer la partida guardada da la misma ola.
8. **Sin asignaciones** en el bucle (regla dura 2): búferes propios y el A* por su ruta sin solución. Solo se asigna el `BoardState` del resultado. Medido: 0 bytes una vez calentado (`ShuffleAllocationTests`).
   - Tiempo en el portátil ARM64 (orientativo): K = 10 con 1 vacío, peor de 20 semillas, 3 ms.
   - Por eso corre en el hilo principal [P-10].

## 3. El comodín y el flujo

- **Oferta** (`ShuffleBooster.Evaluate`), en este orden:
  1. la ranura no existe antes del 30;
  2. «no admitida» con Lluvia o Piraña en el nivel, mirando el tablero inicial (04 §2.4 d), o con ocultas y `shuffle_reveals_murky` apagado [P-12];
  3. el tope del nivel;
  4. el del día;
  5. poca agua (< 2 colores movibles);
  6. fuera de PLAYING: también en el modal de bloqueo, con la victoria o el bloqueo pendientes o con la Luciérnaga eligiendo;
  7. lista.
  - Se paga primero con el stock y, si no, con 300 semillas.
  - La hoja de confirmación sale solo al pagar con semillas y con un precio ≥ 200 [P-6].
- **Uso** (`LevelFlow.UseShuffle`, en un lote de guardado): aplicar → guardar → cobrar, como la Luciérnaga.
  - El cobro sube `daily.shuffles_today` en `Wallet.Apply` (parche): vale para el stock y también en el Reto, con la misma cartera y el mismo perfil.
  - Si la ola falla, se avisa y no se cobra.
- **Tope por nivel** [P-4]: cuenta en todos los intentos del nivel y también si se sale al inicio y se vuelve (marca `o` de `level_state`). Vuelve a 0 al cargar otro nivel.
- **Volver a la orilla** [P-5]: vuelve al tablero del nivel (misma semilla, 04 §2.7) con lo revelado visible. La ola no persiste, como un vertido, y el tope sigue gastado.
- **Guardado** (02 R8) [P-11]:
  - `history` h2 sigue siendo h2. V lleva un `*` donde fue cada ola, y las marcas, una `o` por Pororoca del nivel y una `p` si ya se vio la tarjeta.
  - Al reanudar, la ola se vuelve a dar sobre el tablero rehecho. Si ya no sale (por ejemplo, otro presupuesto en Remote Config), el nivel se carga de nuevo, como cualquier estado que no se puede rehacer.
- **Tarjeta del 30** [P-9]: en la campaña, la primera vez que se juega el 30 (`max_level` < 30), al habilitarse el toque y con la vista quieta. Una vez (marca `p`, que se pone al enseñarla). Se cierra con un toque. No sale en el Reto.

## 4. La vista

- **Ranura** «Barajar», la 4.ª, entre Pista y Revelar.
  - Con 5 ranuras miden 56 dp, o más si «Revelar» o «Barajar» no caben a 12 sp (regla L4).
  - Con stock, «+N»; sin stock, el precio; sin saldo, el precio en rojo y «Te faltan {n}», que late al tocar.
  - Gris si se usó en el nivel o hoy, o fuera de juego.
  - Con poca agua, o con Lluvia o Piraña, gris pero el toque enseña el globo de Pista con su texto (04 §2.4 b; P-7).
- **La ola**: 1,2 s, no saltable; un velo transparente captura los toques.
  - Las cuias que cambian se elevan y bajan con su contenido nuevo cuando el frente cruza su centro; las fijas no se mueven.
  - Una fija que solo se revela (la tinaja) cambia sin elevarse. Suena `sfx_shuffle`.
  - Simplificaciones [P-8]: el líquido no gira dentro de cada cuia, no hay háptico (`AF.Audio` no tiene un `HapticKind` para la ola) y con Reducir movimiento es un fundido de la misma duración.
- **Sonda** (solo en desarrollo): `AF_STATE {"event":"shuffle",…}` con `phase`, que puede valer `confirm`, `cancel`, `shuffled`, `failed`, `card` o `denied`.
  - Lleva además el método, `par_before`, `par_after`, `tries`, `revealed_murky`, la vía de pago o el motivo de `af_booster_denied`, y los topes.
  - Es la materia de `af_shuffle_result` y `af_booster_used` cuando llegue `AF.Analytics` (B7).

## 5. Decisiones [P] (se avanzó con la recomendada)

| Id | Qué | Opciones (recomendada primero) |
|---|---|---|
| P-1 | Cuias con Hoja atada y Flor cerrada: fijas | (a) fijas, como las demás mecánicas cerradas; (b) la Hoja atada entra con su filtro |
| P-2 | Intentos de Permutar + verificar (04 §2.4 no los cuenta) | (a) 20, como los inversos; (b) 5 |
| P-3 | Detalles de la reconstrucción: el estado válido más profundo de un paseo de hasta k; el reparto del estado resuelto; «llena y monocolor» como filtro; el par con w = 2 en los dos lados; sin par de partida, basta con resolver | (a) así; (b) exactamente k pasos y rechazar si muere antes |
| P-4 | «1 por nivel»: ¿por intento o por nivel? | (a) por nivel, en todos sus intentos y también al salir y volver, hasta ganar o pasar a otro; (b) por intento (vuelve con Volver a la orilla); (c) se pierde al salir al inicio, como la Cuia extra |
| P-5 | Volver a la orilla tras una Pororoca | (a) al tablero del nivel (misma semilla, 04 §2.7), sin devolver el tope; (b) al tablero barajado |
| P-6 | Hoja de confirmación con el stock | (a) solo al pagar con semillas (la regla es de gastos de semillas); (b) también con el stock |
| P-7 | Texto con Lluvia o Piraña (el GDD no lo da) | (a) globo «La ola no llega con la lluvia ni las pirañas» (`shuffle_unsupported`, pendiente del asesor); (b) solo gris, sin globo |
| P-8 | VFX: sin el giro del líquido, sin háptico y fundido con Reducir movimiento | (a) así hasta el arte y el audio finales; (b) añadir ya un `HapticKind` (toca `AF.Audio`) |
| P-9 | Tarjeta del 30: cuándo y cuántas veces | (a) tras la intro del primer 30 de la campaña, una vez, y se cierra con un toque; (b) como tarjeta previa (LEVEL_CARD) |
| P-10 | La ola en el hilo principal | (a) sí: mide unos ms frente a 1,2 s de animación; (b) en un hilo de trabajo, como Morpho |
| P-11 | Semilla y guardado | (a) semilla del nivel + tablero revelado + ordinal, y `*` en V (h2 sin versión nueva); (b) guardar el tablero barajado entero (h3) |
| P-12 | `shuffle_reveals_murky` = false | (a) la Pororoca no se ofrece en niveles con ocultas; (b) baraja sin tocar las cuias turbias |
| P-13 | Tinaja con dosis (04 §2.4 g: «si falla a menudo, se deshabilita mientras la tinaja tenga dosis [P]») | (a) habilitada, y se mide en `level-qa` en una tarea aparte; (b) deshabilitada ya mientras tenga dosis |
| P-14 | La Pororoca en el Reto del río | (a) sí, con los mismos topes y sin la tarjeta (06 §4.1); (b) no |
| P-15 | Botón de la hoja | (a) «Seguir», del «¿Seguir?» de la línea; (b) «Barajar» |
| P-16 | La ola que falla en un nivel con mecánicas: 03 §9, «Consecuencias (2)», dice que la Pororoca «se desactiva en ese nivel con el texto "La ola no puede entrar aquí"»; 04 §2.4, que «no se cobra y aparece "La ola no pudo remover el río"» | (a) 04 §2.4, el capítulo dueño del comodín: aviso, sin cobro, y se puede reintentar (otra semilla no, porque la ola es determinista: el mismo tablero vuelve a fallar hasta que cambie); (b) 03 §9: se desactiva en el nivel |
| P-18 | Bandeja de 5 ranuras (desde el 30, en los niveles con ocultas: Aguas turbias desde el 100 y la Tinaja) en 360 dp: «Barajar» cabe en 56 dp en es-419, pero «Shuffle» y «Embaralhar» (glosario §10), y «Desfazer» y «Revelar» en pt-BR al 130 %, no caben a 12 sp y se parten a media palabra (TranslatedLocaleOverflowTests, con la Pororoca encendida) | (a) el dueño o el diseño de la guía deciden (etiqueta corta en en y pt-BR, p. ej. «Mix»/«Misturar», o la bandeja de 5 sin etiquetas con la descripción para TalkBack); hasta entonces EN y PT-BR de `shuffle` quedan vacíos; (b) bajar de 12 sp (rompe 02 §12) |
| P-19 | Pruebas PlayMode | (a) la Pororoca se apaga por defecto en los tests PlayMode que no tratan de ella (`GameScreen.ShuffleOverride`, como la pista por inactividad o el rótulo del lugar), para que sus bandejas y avisos sigan como antes; `ShufflePlayModeTests` la enciende; (b) reescribir esas pruebas para 5 ranuras |
| P-17 | Remolino: 02 §9.2 pide «reconstrucción con la transición de remolino» | (a) el paseo inverso va con cuias normales de la misma capacidad y el A* verifica con la transición real del Remolino; (b) movimientos inversos propios del Remolino |

**Preguntas para el dueño** (no bloquean: se avanzó con la (a)): P-4, P-5, P-6, P-13, P-16, P-18 y P-10 (medir en el teléfono el peor caso, la ola que falla: hasta 41 A* de 40.000 nodos, unos 2 s si cada uno roza los 50 ms de 03 §5).

## 6. Hallazgos para el GDD (los lleva el integrador)

- 04 §2.4 no dice qué pasa con Volver a la orilla tras una Pororoca, ni si «1 por nivel» es por intento (P-4, P-5); 02 §14 tampoco.
- 04 §2.4 no da texto para Lluvia y Piraña (P-7) ni el botón de la hoja (P-15).
- 04 §10.1 y 08 §4.3: `level_state.history` lleva ahora la marca `*` de la ola y las marcas `o` y `p` (P-11).
- 04 §2.4: el número de intentos de Permutar (P-2).
- 03 §9, «Consecuencias (2)», frente a 04 §2.4: la ola que falla en un nivel con mecánicas (P-16).
- 08 §8.2: `af_shuffle_result` sale hoy solo como sonda de desarrollo (sin `AF.Analytics`).
