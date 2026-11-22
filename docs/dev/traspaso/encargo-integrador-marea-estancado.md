# Encargo para el integrador: Marea en «No hay más vertidos» (D-014)

2026-11-17. Decisión del dueño en el chat con Spark, literal: «Sí, opción 1 y 3». Cambia dos puntos del «D-014, añadido del 2026-11-06: estancado solo al repetir» (`docs/dev/decisiones.md`). Hay que anotarlo como un añadido nuevo de D-014.

## Lo que vio el dueño

El dueño jugaba en el teléfono un tablero de 2 filas y 10 cuias, con la jaula (Matapí) y sin saldo de semillas. Le salió el modal «No hay más vertidos · Aguas estancadas».

- **Primero:** el modal salió cuando, para él, aún le quedaba un vertido que no había hecho, el blanco entre la segunda cuia de arriba y la jaula.
  - Es lo que manda la regla: ese vertido lleva a un tablero que ya vio en el intento (por otro camino, o con las cuias en otro orden, por el hash canónico).
  - **No se cambia;** se entiende mejor con los arreglos de abajo.
- **Segundo, el fallo:** pulsó «Deshacer · Marea» y el modal volvió a salir al instante, sin dejarle hacer ningún vertido. Además gastó la Marea.
  - **Causa:** «lo deshecho con Marea cuenta como visto» ([P] del añadido del 2026-11-06, I6 de `LevelSession`).
  - Tras deshacer, el único vertido legal del tablero anterior lleva al tablero recién deshecho, que está visto. Así que `Undo()` → `Evaluate` → Stuck otra vez.

## Lo que decidió el dueño

### 1. Deshacer saca de «vistos» el tablero deshecho

- **El cambio:** `LevelSession.Undo()` quita de `_seen` el hash canónico del tablero que deja (el `Current` de antes de deshacer) antes de reevaluar.
  - Con eso, tras una Marea el jugador puede repetir ese vertido u otro.
  - El modal vuelve cuando, otra vez, todo vertido legal lleve a un tablero visto.
- **Ojo con los tableros repetidos en el historial:** si el tablero deshecho también fue `Current` antes en el mismo intento (por ejemplo, ida y vuelta), lo más simple y coherente es llevar un contador por hash en lugar de un `HashSet`. Deshacer resta 1 y el tablero deja de estar visto al llegar a 0. Decide el integrador y anótalo como [P].
- **Sin cambios:** Volver a la orilla (olvida todo), la Pororoca, la Luciérnaga y la restauración de capas, y las sesiones rehechas (Cuia extra y de rescate, reanudar).
- **El 66 de las pruebas** (`StuckLoopTests`, «las Mareas al 11.º, al 10.º y al 9.º»): con el arreglo, cada Marea deja al menos un vertido. El modal solo vuelve cuando el jugador repite. Hay que actualizar la prueba y el texto del añadido.

### 2. El modal solo ofrece Marea cuando deshacer sirve (la opción 3)

- **Cuándo no se ofrece:** cuando deshacer no puede llevar a ningún tablero con un vertido legal a un tablero no visto. Con el punto 1 aplicado, eso pasa si el tablero de antes también está perdido y agotado sin contar el que se deshace, o si no hay historial.
  - En ese caso, «Deshacer · Marea» no sale (o sale en gris con su motivo; elige el integrador según la guía de botones grises de D-061) y el modal deja delante la Cuia extra o de rescate, y «Rendirse».
  - Así nadie gasta una Marea que no saca del atasco.
- **Dónde:**
  - `StuckModalModel` (`StuckUndoKind`), con un dato nuevo del núcleo: algo como `LevelSession.UndoEscapes` o `CanUndoEscape()`, sin asignaciones, que use el mismo `DeadEndProbe` y `_seen`;
  - el modal (`StuckModal.cs`) y sus textos (`UiText`; si hace falta un texto nuevo, con la regla de localización);
  - los PlayMode del modal.
- **Analítica:** si `af_stuck_shown` o `af_stuck_choice` llevan la lista de opciones ofrecidas, que reflejen que Marea no se ofreció. Sin campos nuevos sin pasar por 08 §8.

## Pruebas que deberían quedar

- **Núcleo, compartidas y `DotnetOnly`:**
  - tras Stuck y `Undo()`, `Status` es Playing y hay al menos un vertido legal a un tablero no visto (salvo sin historial);
  - repetir ese vertido devuelve Stuck;
  - la propiedad con `StagnationOracle`, ajustada a la regla nueva;
  - sin asignaciones tras calentar en `Undo()` y en el dato nuevo.
- **Unity:**
  - `StuckLoopTests` con el 66: Marea desde el modal deja jugar;
  - un caso en el que Marea no se ofrece (tablero de antes también perdido y agotado);
  - el modal sin el botón, o en gris, con TalkBack.
- **En el teléfono (Spark):** el tablero de la captura del dueño, o el 66, con Marea desde el modal. Spark lo prueba en el S23 en cuanto esté integrado.

## Documentación que toca

- **`docs/dev/decisiones.md`:** el añadido nuevo de D-014, con la decisión literal del dueño.
- **02:**
  - §1.4: R13 y la nota de «tableros vistos»;
  - §2: la transición STUCK_MODAL → Marea.
- **04:** §2.1 (Marea en el modal) y §5.1.
- **El README y el CHANGELOG del núcleo.**
