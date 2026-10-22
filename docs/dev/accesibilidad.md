# B9 · Accesibilidad (tanda 5, Spark)

Bloque B9 de `docs/dev/plan-tanda5.md` §4: Reducir movimiento y Alto contraste al 35 % (D-033, Q-N4 (a)) como ajustes con su campo en el perfil, la escala de fuente del sistema, C9 (mantener pulsado), el test del simulador de daltonismo y TalkBack completo como fija 02 §12. Rama `spark/t5-b9`. Encargo: `docs/dev/traspaso/spark-reto-del-rio.md` §13, punto 3.

Lo marcado [P] es propuesta sin decisión del dueño; cada [P] remite a su pregunta Q-B9-n (§5), con la opción recomendada, que es la implementada.

## 1. Canon

- **02 §3.1, C9:** mantener ≥ 400 ms sobre una cuia sin mover el dedo muestra los nombres de fruta de sus dosis; al soltar no selecciona. En la Tinaja (D-019), solo la boca y las capas ya reveladas.
- **02 §4 y §12, fila «Movimiento»:** con «Reducir movimiento», todas las duraciones × `anim_reduce_motion_factor` (0,5), sin sacudidas, sin confeti y sin la onda del shader. 07 §7.8 añade: Llenar y teñir en fundido y sin rebotes.
- **02 §12, fila «Modo daltónico»; 07 §3, regla 5; D-033, Q-N4 (a):** un solo ajuste «Alto contraste»: el patrón al 35 % y el contorno de dosis a 1,5 px, sin cambiar ningún HEX.
- **02 §12, fila «Tamaño de texto»:** se respeta la escala de fuente del sistema hasta el 130 %; por encima, ese tope.
- **02 §12, fila «Lector de pantalla» (y 07 §7.8):**
  - etiqueta de cada cuia con sus dosis de abajo arriba y los huecos («cuia 3: açaí, açaí, aguaje, 1 hueco»);
  - las ocultas, «turbia»;
  - la Tinaja y su cuia chica, con sus propias formas;
  - todo botón con `contentDescription` = etiqueta funcional + estado («Deshacer, 2 restantes»);
  - el foco recorre las cuias por orden de ranura y después la barra de comodines;
  - la selección se anuncia («açaí seleccionado: elige destino»).
- **07 §3, regla 3; 07 §7.8; 08 §14.3:** QA con simulador de daltonismo de los tres tipos sobre el shader real. El método no está fijado: es [P].
- **08 §9:** `ui_reduce_motion_default` (bool, false) y `anim_reduce_motion_factor` (float, 0,5, rango 0,3-0,7). No hay clave para Alto contraste ni para C9.
- **08 §4.3:** `settings`, con `reduce_motion` y `high_contrast` anotados el 2026-10-22 (tanda de pulido, D-033 punto 12). Los campos son [P] (Q-B9-15).

## 2. Qué hace

### 2.1 Reducir movimiento

- **Ajuste:** «Reducir movimiento» en Ajustes, tras «Pista por inactividad». **En la pantalla es un interruptor de sí o no** (02 §12, fila «Movimiento»), no un selector de tres opciones: enseña el valor en vigor y, al tocarlo, guarda `on` u `off`.
- **Perfil:** `settings.reduce_motion` = `auto` | `on` | `off` [P] (Q-B9-15). `auto` es el estado antes de tocar el interruptor (perfiles nuevos y anteriores a B9); se escribe solo si deja `auto` y se lee de forma tolerante. Desde Ajustes no se vuelve a `auto` (no hay opción «Automático»): una vez tocado, el interruptor manda y la escala de animación del teléfono ya no cuenta.
- **Valor en `auto`:** `ui_reduce_motion_default` o [P] el teléfono con «Quitar animaciones» (Q-B9-3), es decir, la escala de animación del sistema a 0.
- **Aclaración del 2026-10-22** (observación 6 de `docs/dev/traspaso/pruebas-telefono-2026-10-22.md`): los commits de B9 hablan de `auto`/`on`/`off`, que son **los valores del perfil**; lo decidido (Q-B9-3 y Q-B9-15 con su recomendada, D-033) es el interruptor de sí o no con `auto` al arrancar, y eso es lo que hace el cliente. Si el dueño quiere un selector de tres opciones (Automático, Sí, No) en Ajustes, es una pregunta nueva (ver §5).
  - Lo resuelve `A11yRules.ReduceMotion`.
- **A qué llega:**
  - `LevelFlow.ReduceMotion` de la campaña y del Reto: el Reto lo recibe por `DailyChallengeScreen.Prepare` antes de construir su tablero;
  - el Mapa (`ProgressReduceMotion`);
  - la pantalla de arranque.
- **Efectos:**
  - vertidos y elevación × `anim_reduce_motion_factor`;
  - pasos de mecánica × el mismo factor, una sola vez (`BoardAnimator.ReduceMotionFactor`);
  - sin sacudida en el rechazo, sin onda del agua y sin confeti.
- **Lo que ya existía:** la intro en fundido, las partículas, TiTino, la tarjeta previa…, a través de `ReduceMotion`.

### 2.2 Alto contraste

- **Ajuste:** «Alto contraste» en Ajustes.
- **Perfil:** `settings.high_contrast` (bool) [P], escrito solo si está encendido.
- **Efecto:** el patrón del líquido al 35 % (`A11yRules.HighContrastPatternOpacity`) al momento, en campaña y en el Reto. No toca los iconos, que son de Patrones de fruta (Q-B9-5).
- **Contorno de dosis** (Q-B9-6, tanda de pulido del 2026-10-22): una línea de tinta (`ui_tinta`) de 1,5 px de diseño, que el cliente toma como dp [P] (`A11yRules.HighContrastDoseOutlinePx`) centrada en el arco de abajo de cada dosis menos la del fondo, en el shader del líquido (`_DoseOutlineDp`, que fija `LiquidGraphic.ApplyDoseOutline`). Va también entre dos dosis de la misma fruta [P]. Capturas: `DoseOutlineCaptureTests` (`[Explicit]`), en `Logs/snapshots/es-419/a11y/`.

### 2.3 Escala de fuente del sistema

- `ISystemScale` (`AF.Platform`), con dos implementaciones:
  - `FakeSystemScale` en el Editor y en los tests;
  - `AndroidSystemScale` (`AF.UI`, porque necesita el motor), que lee `AccessibilitySettings.fontScale` del módulo `com.unity.modules.accessibility`, ya en el manifiesto, y `ANIMATOR_DURATION_SCALE` por JNI. Sin paquete, plugin ni permiso.
- **Orden de resolución en `LaunchOptions.ResolveTextScale`:**
  1. el gancho de test;
  2. el extra `af_font_scale` de desarrollo;
  3. el sistema, entre 1 y 1,3.
- La escala se lee una vez por arranque (Q-B9-1).
- `ClampTextScale` y la escala del sistema usan la misma regla: `A11yRules.TextScaleFor`.

### 2.4 C9, mantener pulsado

- **Piezas:**
  - `CuiaHoldRelay`: un componente en cada cuia, que no toca `CuiaView`;
  - `CuiaHoldTracker`: la lógica, con el reloj de la pantalla;
  - `CuiaNamesBubble`: el bocadillo de vidrio del juego sobre la cuia.
- **Cuándo salen los nombres:** con 400 ms y menos de 24 dp de movimiento [P] (Q-B9-4).
  - Un dedo que se mueve 24 dp ya es el arrastre (C10).
  - Con dos dedos (C11) no salen.
- **Qué se enseña:** los nombres de arriba abajo, uno por línea [P].
  - Una dosis oculta se ve como «turbia».
  - En la Tinaja, solo la boca y lo revelado.
- **Al soltar:** ni toque ni arrastre. Mantener 400 ms nunca es un toque: tampoco si en ese momento algo tapa el tablero (la pausa, otro lienzo con velo) y los nombres no llegan a salir.
- **Con los nombres a la vista:** un velo que se abre encima los quita; se comprueba cada 250 ms [P] (`GameScreen.HoldCoverCheckMs`), no en cada fotograma.
- **Dedos:** se siguen por su `pointerId`. Un touch-up que se pierde de verdad (la cuia se destruye, la pantalla se oculta o la app va a segundo plano con el dedo abajo) lo cubre `HoldReset` al construir el tablero, al ocultar la pantalla y en `SnapToModel`.
- **Límites conocidos:**
  - si otro dedo toca la misma cuia mientras salen los nombres, su touch-up también se traga, porque `CuiaView.OnCuiaUp` no pasa el `pointerId`;
  - si otro dedo toca otra cuia con los nombres a la vista, ese toque cuenta (selecciona o vierte) y los nombres siguen; C11 solo impide que salgan nombres con dos dedos.

### 2.5 TalkBack

- **Servicio:** `A11yService` publica en `UnityScreenReaderHost`, que usa `AssistiveSupport` de Unity y no necesita paquete. Solo trabaja con el lector encendido.
- **Cuándo publica:** cada 250 ms [P], o al momento tras un toque. Si solo cambian etiquetas, actualiza los nodos sin mover el foco.
- **Fuente propia, la pantalla del nivel** (con el tablero jugable y sin modal encima):
  - las cuias por orden de ranura, con su etiqueta, su estado «seleccionada» y la activación (el mismo toque, también para elegir la cuia de la Luciérnaga);
  - después la barra de comodines (`TraySpeech`: «Deshacer, 2 restantes»);
  - después lo demás del lienzo.
- **Recorrido genérico, el resto** (`A11yScanner`):
  - lienzos de arriba abajo; se para en el primero con un velo;
  - todo `Button`, con su etiqueta: la registrada en `A11yLabels` o sus textos;
  - los relés de «toca para seguir», como «Continuar» [P];
  - los textos sueltos;
  - nada recortado por un visor ni tapado por un velo;
  - orden de lectura por filas.
- **Botones de solo icono, con su etiqueta funcional:** la pausa, la X de los modales (`ModalKit.Close`) y el engranaje. Las filas de Ajustes dicen si están marcadas.
- **Anuncio de la selección:** «Açaí seleccionado: elige destino».
- **Al destruir la escena,** se quita lo publicado.

### 2.6 Simulador de daltonismo

- **Método [P] (Q-B9-10):**
  - `ColorVision`: matrices de Machado, Oliveira y Fernandes (2009) con severidad 1,0, en RGB lineal;
  - distancia CIEDE2000 en CIELAB D65, comprobada con los datos de Sharma (2005).
- **EditMode, en la suite (`ColorVisionTests`):** sobre los HEX de 07 §3, en cada tipo el par más parecido es un par de riesgo de 07 §3. Lo cumple.
- **PlayMode `[Explicit]` (`ColorVisionShaderTests`), sobre el shader real:**
  - 16 cuias con los 14 tintes, al 15, al 20 y al 35 %;
  - deja las capturas, sus versiones simuladas y `cvd_report.md` en `Logs/snapshots/es-419/cvd/`, para la prueba manual de 08 §14.3;
  - necesita gráficos: `unity test <repo> --mode PlayMode --architecture arm64 --filter AF.Tests.PlayMode.ColorVisionShaderTests`. Se ejecutó así en este equipo.
- **Hallazgo:** en el shader, al 35 % y con protanopía, el par más parecido es 7-10 (Bacaba / Agua del Tapajós, ΔE00 2,6), que no es par de riesgo.
  - Sobre los HEX, con protanopía, 7-10 da 4,9 y 5-14 (Achiote / Agua del Río Negro) 6,1.
  - Con deuteranopía, 5-14 da 3,5 (3,7-4,4 en el shader).
  - Pregunta Q-B9-13.

## 3. Archivos

**En la rama** (todos nuevos):
- `Scripts/Platform/`: `A11yConfig.cs` (sin `[RemoteConfigKey]` hasta el parche), `A11yRules.cs`, `ISystemScale.cs` y `FakeSystemScale.cs`.
- `Scripts/RemoteConfig/Binding/A11yConfigBinder.cs`.
- `Scripts/UI/A11y/`:
  - `A11yText.cs`, `CuiaSpeech.cs`, `TraySpeech.cs`;
  - `A11yService.cs`, `A11yScanner.cs`, `A11yLabels.cs`, `ScreenReaderHost.cs`;
  - `AndroidSystemScale.cs`;
  - `CuiaHold.cs`, `CuiaNamesBubble.cs`;
  - `ColorVision.cs`.
- `Scripts/UI/GameScreen.A11y.cs` y `SettingsScreen.A11y.cs`: parciales nuevos que el parche llama.
- **Tests:**
  - `Tests/EditMode/A11y/`: `A11yRulesTests`, `CuiaHoldTrackerTests`, `CuiaSpeechTests`, `ColorVisionTests` y `A11yServiceTests`, más el ayudante `B9Loc`;
  - `Tests/PlayMode/`: `A11yScannerTests` y `ColorVisionShaderTests` (`[Explicit]`), más el ayudante `B9LocPlay`.
- **Textos:** `docs/dev/traspaso/spark-enganches/B9-strings.csv`, 19 claves, todas `borrador` o `pendiente_asesor`. Los tests de la rama las leen de ahí hasta que entre `B9-loc.diff`; después, el integrador puede quitar `B9Loc`, `B9LocPlay` y el CSV.

**Encargo §3.2:** el plan pedía `S/Platform/AndroidSystemScale.cs`, pero `AF.Platform` tiene `noEngineReferences: true`. La interfaz y la falsa van en `Platform` y la lectura de Android, en `AF.UI`, como `AndroidDeviceLocale`.

## 4. Parches para el integrador

Están en `docs/dev/traspaso/spark-enganches/`, en LF, y se aplican en este orden con `git -c core.autocrlf=false apply`:

1. **`B9-profile.diff`:** `ReduceMotionSetting`, `SettingsData.ReduceMotion` y `HighContrast`, `ProfileJson` (solo escribe lo que no es default; lee tolerante) y `A11ySettingsProfileTests`.
2. **`B9-rc.diff`:** `[RemoteConfigKey]` en `A11yConfig`, `RcConfigBinder` (`BoundKeys` y `RcConfigs.A11y`) y `RcConfigBinderTests`.
3. **`B9-ui.diff`** (necesita el 1 y el 2):
   - **`GameScreen.cs`:**
     - el relé de C9 en cada cuia;
     - `OnCuiaUp` se traga el touch-up de C9;
     - `HoldTick`;
     - el factor de Reducir movimiento en los tiempos y en `BoardAnimator`;
     - sin sacudida, sin confeti;
     - `ApplyLevelPatterns`;
     - el anuncio de la selección;
     - `HoldReset` al construir el tablero, en `SnapToModel` y al ocultar la pantalla (`SetVisible(false)`).
   - **`BoardAnimator.cs`:** `ReduceMotionFactor`, `DoseNominalMs` sin el factor y sin onda (`Ripple`) con Reducir movimiento.
   - **`MechanicStepContext.cs`:** documentación.
   - **`SettingsScreen.cs`:** las dos filas, su sitio en el visor (antes de la privacidad de B12) y en la sonda.
   - **`OptionRow.cs`** (común con B12): `A11yLabels`, con su texto y si está marcada.
   - **`LaunchOptions.cs`:** `SystemScaleOverride`, `SystemScale`, la escala del sistema en `ResolveTextScale` y `ClampTextScale` con la regla de B9.
   - **`ModalKit.cs`:** la etiqueta «Cerrar» de la X.
   - **`GameBootstrap.cs`:** `SetUpA11y`, `TickA11y`, `MarkA11y` al abrir Ajustes, la pantalla de arranque con Reducir movimiento y `ClearA11y` en `OnDestroy`.
   - **`GameBootstrap.Daily.cs`:** `Prepare = PrepareDaily`.
   - **`DailyChallengeScreen.cs`:** `Prepare`, antes de `Bind`, `StartDaily` y `ResumeDaily`.
   - **Archivos nuevos:** `GameBootstrap.A11y.cs` y `A11yGameTests` (PlayMode).
4. **`B9-loc.diff`:** las 19 claves en `strings.csv`, por orden ordinal, y `A11yText.AllKeys` en `LocTablesTests.UiTextKeys`.

**Escena y prefabs:** ningún cambio; todo se crea por código.

### 4.1 Choques con B11 y B12

Ya integrados en `bacf72a8`: la rama se fusionó con esa base y los parches están rehechos sobre ella. El arrastre C10 de B11 va detrás de C9 en `OnCuiaUp`, y las filas de B9 van antes de la privacidad de B12, con la suma de la sonda junta. Lo de abajo queda como referencia.

- **B11, `B11-drag.diff`:**
  - toca el mismo `OnCuiaUp`: el arrastre C10 va en un `else if` después del toque.
    - Al juntarlos, el arrastre tampoco puede ocurrir tras C9.
    - Forma: `bool held = HoldSwallows(index); if (inside && !held) { tap } else if (!held && …) { TryDragPour(…) }`.
  - B9 no toca `CuiaView`. C9 escucha con su propio componente; el `IDragHandler` de `CuiaHoldRelay` solo lee, no interfiere con las posiciones que guarda B11 y no le llega ningún arrastre por debajo de 24 dp.
- **B11, `B11-labels.diff` y `B11-tutorial.diff`:** si añaden botones de solo icono, conviene registrarlos con `A11yLabels.Set`. Si añaden velos de toque (`PointerTapRelay`), salen como «Continuar».
- **B12:**
  - `SettingsScreen.EmitLayout`: la suma de `n` se junta a mano (`+ ConsentLayoutCount + A11yRowCount`, como ya pasó con B8);
  - `LocTablesTests.UiTextKeys` y `strings.csv`: puntos distintos;
  - `GameBootstrap.cs`: B9 añade una línea tras `SetUpMarket()`, otra en `Update` y otra en `OnDestroy`. B12 toca el arranque antes de `AppFlow.Start`: no se pisan, salvo por el contexto de los parches.
  - La pantalla de consentimiento de B12 no necesita nada para TalkBack: son `Button` con texto y los recorre el escáner.

## 5. Preguntas para el dueño

En todas se implementó la (a), la recomendada.

| Id | Pregunta | Opciones | Bloquea |
|---|---|---|---|
| Q-B9-1 | Cambiar la fuente del sistema con el juego abierto | (a) se aplica al volver a abrirlo; (b) reconstruir la UI al momento | — |
| Q-B9-2 | Fuente del sistema por debajo del 100 % | (a) se queda al 100 % (texto ≥ 12 sp, 02 §12); (b) seguirla | — |
| Q-B9-3 | «Quitar animaciones» de Android con el ajuste en `auto` | (a) enciende Reducir movimiento; (b) no | — |
| Q-B9-4 | «Sin mover el dedo» en C9 | (a) < 24 dp, el umbral de C10; (b) otra cifra | — |
| Q-B9-5 | ¿Alto contraste enciende también los iconos? | (a) no, son de Patrones de fruta (07 §3, regla 5 literal); (b) sí | — |
| Q-B9-6 | Contorno de dosis de 1,5 px de 07 §3, regla 5 | (a) tarea aparte con el shader y capturas (`A11yRules.HighContrastDoseOutlinePx` ya existe); (b) en B9 | Alto contraste completo |
| Q-B9-7 | Fruta sin nombre aprobado en release (asesor) | (a) el lector dice «fruta» (`a11y_fruit_unnamed`); (b) callar las etiquetas hasta el asesor | — |
| Q-B9-8 | «3 dosis dentro» de la Tinaja | (a) todas, con la boca; (b) sin la boca | — |
| Q-B9-9 | Botón de comodín con precio o video | (a) solo la etiqueta; (b) «Deshacer, 30 semillas» con clave nueva | — |
| Q-B9-10 | Método del simulador y umbral | (a) Machado 2009 + CIEDE2000, informe sin umbral y QA manual (08 §14.3); (b) fijar un ΔE mínimo como compuerta | — |
| Q-B9-11 | «{fruit} seleccionado» con frutas femeninas | (a) el texto de 02 §12 tal cual; (b) neutro («elegiste açaí: elige destino») | Textos |
| Q-B9-12 | El traslado inverso de Marea (250 ms) con Reducir movimiento | (a) × el factor, como dice 02 §4 («todas las duraciones»), en una tarea aparte; (b) fijo, como hoy | — |
| Q-B9-13 | Hallazgo del simulador: 7-10 (protanopía) y 5-14 (deuteranopía) no son pares de riesgo | (a) añadirlos a la lista de 07 §3 y glosario §9 (el generador los limita); (b) dejarla | Generador |
| Q-B9-14 | 08 §14.2 pide anunciar la capacidad y la mecánica; 02 §12 no | (a) añadir el nombre de la mecánica con las claves de sus nombres, en una tarea aparte; (b) como 02 §12 | — |
| Q-B9-15 | Campos del perfil | (a) `settings.reduce_motion` (`auto`/`on`/`off`) y `settings.high_contrast` en 08 §4.3; (b) otros nombres | GDD |
| Q-B9-16 | `af_settings_change.key` | (a) añadir `reduce_motion` y `high_contrast` a 08 §8.2; (b) no enviarlos | Analítica |
| Q-B9-17 | TalkBack y la lista de Ajustes que se desplaza (al 130 % o en pantallas bajas, las últimas filas quedan bajo el corte y no se publican) | (a) añadir la acción de desplazar del lector (`AccessibilityNode.scrolled`) en una tarea aparte; (b) publicar las filas recortadas y desplazar al activarlas | TalkBack en Ajustes |
| Q-B9-18 (2026-10-22, abierta) | «Reducir movimiento» en Ajustes: hoy es sí o no y, una vez tocado, no vuelve a seguir al teléfono (§2.1) | (a) dejarlo así (02 §12 habla de un ajuste; lo decidido en Q-B9-3 y Q-B9-15); (b) selector de tres opciones (Automático, Sí, No) que devuelve `auto`, con una clave de texto nueva | — |

Más [P] sin pregunta propia:
- el orden de los nombres de C9, de arriba abajo;
- el número de la cuia = su índice + 1;
- «Continuar» para los velos de toque;
- 250 ms entre recorridos;
- el 90 % de cobertura para considerar un velo;
- los lienzos con el mismo `sortingOrder` se tratan como grupo, porque Unity no fija cuál va encima: si uno tiene velo, tapa lo de debajo, y para un nodo del lector cuenta como tapa;
- 250 ms entre comprobaciones de si algo tapa los nombres de C9.

## 6. Riesgos y pendientes

- **TalkBack sin probar en el teléfono [NV]:**
  - el origen de coordenadas de los marcos (arriba a la izquierda);
  - que una etiqueta cambiada se relea;
  - el anuncio;
  - el doble toque.
  - Lo prueba el dueño en el teléfono de clase A (plan, §3).
- **Lectura de la escala de animación por JNI [NV]:** se hace una vez por arranque; si falla, da NaN y se ignora.
- **Sin test automático:** la pose de TiTino en la pantalla de arranque con Reducir movimiento (los tests apagan la pantalla de arranque) y el factor de la cuia añadida a mitad de nivel.
- **Snapshot de Alto contraste y de las filas nuevas de Ajustes:** sin capturas en este equipo para las filas (las de daltonismo sí). El integrador puede hacerlas con `SnapshotKit`.
- **El escáner genérico depende de que las pantallas usen `Button`, textos TMP y velos con `raycastTarget`.** Un control nuevo hecho de otra forma no se publica. La sonda `Unlabeled` cuenta los botones sin etiqueta.
- **`ColorVision` vive en `AF.UI`** para que lo vean los tests de EditMode y de PlayMode. El juego no lo referencia y el stripping High (B10) lo quita del player.
- **Hallazgos para el GDD:**
  - 02 §12 debería citar el contorno de 1,5 px de 07 §3;
  - 08 §4.3 y §8.2 necesitan los campos y las claves de Q-B9-15 y Q-B9-16 (anotados el 2026-10-22, tanda de pulido);
  - 08 §14.2 frente a 02 §12, en el contenido de la etiqueta (Q-B9-14; hecho el 2026-10-22: «cuia 3: açaí, 1 hueco. Hoja», con `a11y_cuia_mechanic` y los nombres `mechanic.<id>` de `facts.csv`, que esperan al asesor; la Marea con el factor de Reducir movimiento, Q-B9-12, también).
