# Encargo al equipo Spark: Música y Sonido con un toque, y sitio para la tropa con 3 filas (2026-10-23)

Lo escribió el integrador (escritorio). La decisión es **D-046**, puntos 2 y 3, en `docs/dev/decisiones.md`. Léela entera antes de empezar. El dueño pidió que fuera en paralelo y en el Spark, para no gastar disco en el escritorio.

**Reglas:** las de siempre, en `spark-reto-del-rio.md` §1 y §2: `CLAUDE.md` entero, las ramas `spark/*`, sin push force, el escritorio integra, y la verificación completa antes de cada push con la arquitectura y el Python de tu máquina. Cada carril pasa por **code-reviewer** hasta APROBADO.

**Base:** `origin/integracion-fase1-fase2` en `a656cca4` o posterior. Mide tu línea base de EditMode y PlayMode antes de tocar nada: hoy son 4.447 y 1.994 en el escritorio.

**Dos carriles independientes, un worktree para cada uno** (siembra la `Library` con robocopy):

## Carril 1 · `spark/t6-audio-directo`: Música y Sonido con un toque (D-046, punto 2)

1. **Menú de pausa** (`Assets/_Project/Scripts/UI/PauseMenu.cs`):
   - dos botones redondos con icono arriba, junto a la X, **Música** (nota) y **Sonido** (altavoz);
   - un toque los enciende o apaga, y el icono tachado indica apagado;
   - área táctil ≥ 48 dp;
   - iconos dibujados por código, como el resto del arte provisional.
2. **Inicio** (`HomeWindow.cs`): los mismos dos botones junto al engranaje de Ajustes. **Ojo:** el escritorio añade en paralelo el botón «Liga» al inicio (D-046, punto 1). Deja el sitio de los tuyos junto al engranaje y no toques el resto del inicio; los choques los resuelve el integrador.
3. **El mismo ajuste que Ajustes:**
   - guardan en `settings.music` y `settings.sound` por el mismo camino que las filas de `SettingsScreen.Audio.cs` (`GameBootstrap.ChooseAudio`) y se aplican en el acto;
   - al volver a Ajustes, las filas muestran lo mismo, y al revés;
   - `af_settings_change` sale igual que desde Ajustes, con un campo `source` (`pause` o `home`) si 08 §8.2 lo admite. Si no lo admite, añádelo a 08 §8.2 y a `AnalyticsCatalog` con su test.
4. **Vibración:** sigue solo en Ajustes. Nada en la barra de juego.
5. **Accesibilidad:**
   - TalkBack lee, por ejemplo, «Música: activada» y «Sonido: desactivado»;
   - claves nuevas en `strings.csv` en es, en y pt-BR, con `context` y `max_chars` (`.claude/rules/localizacion.md`);
   - al 130 % caben;
   - con Alto contraste se distinguen.
6. **Tests:**
   - EditMode: el estado compartido con Ajustes;
   - PlayMode: tocar en pausa apaga la música y Ajustes lo refleja; tocar en el inicio; TalkBack.
7. **GDD:** 02 §5 (la pausa lleva los dos botones) y 07 §9.4 (se apagan con un toque desde la pausa y el inicio), citando D-046.

## Carril 2 · `spark/t6-tropa-3filas`: sitio para la tropa con 3 filas (D-046, punto 3)

**Lo que pasa hoy** (medido por el escritorio a 360 × 780):
- `BoardTop` = 88 y `BoardBottom` = `TrayTop` − 40 = 620;
- con 3 filas, `BoardLayout.CuiaWidthFor` encoge las cuias por alto hasta llenar el tablero (unos 47 dp de ancho), así que el panel pasa bajo el HUD;
- no queda franja para la rama, y la intro (`IntroChoreography`) va con el dosel o, 1 de cada 3 veces, con la tropa lateral (D-021).

**Qué hacer:**
1. **Bajar el suelo del tablero** unos 20 dp (`GameScreen.BoardBottom`), dejando unos 12 dp entre el panel y la bandeja. Mídelo y deja la cifra con [P] en 02 §5 y 07 §7.4.
2. **Con 3 filas, reservar arriba una franja ≥ 44 dp para la rama de la tropa,** encogiendo las cuias lo justo (unos 44 dp a 360 × 780 [P]). Ni la fila 1 ni su cielo entran ya en el HUD: se retira esa parte de la excepción de D-028.
   - Nunca por debajo de `MinCuiaWidth`, ni del mínimo de 14 dp de u de 07 §4.5 con la Cuia alta.
   - Con la Tinaja (12 cuias como mucho) también.
   - 360 × 640 y 320 dp siguen funcionando; donde no quepa la franja, vuelve el comportamiento de hoy, la tropa lateral o el dosel, y lo dice un test.
3. **La tropa en su rama en todos los niveles de 3 filas,** en el primer intento, como en los de 1 y 2 filas. La tropa lateral de D-021 pasa a ser solo el respaldo cuando no cabe la franja.
4. **Los tableros de 10 cuias o menos no cambian,** y lo prueba un test (mismo layout que antes).
5. **Tests:**
   - `BoardLayoutTests`, `BoardLayout320Tests`, `Canvas16x9BoardFitTests` y los de la intro, con 11-16 cuias, Cuia alta y Tinaja;
   - las capturas de PlayMode que cambien, regeneradas y revisadas una a una.
6. **GDD:** 02 §5 (regla del tablero con 3 filas y margen sobre la bandeja), 07 §7.4 y la nota en D-021 y D-028 de que D-046 las matiza.
7. **En el teléfono** (si el Spark tiene uno): un nivel de 14-16 cuias en el primer intento, con la tropa en la rama, nada bajo el HUD y la bandeja sin toques accidentales.

## Al terminar

Anota cada carril en `docs/dev/traspaso/spark-estado.md`, con el formato de siempre:
- los commits;
- la verificación completa con sus resultados;
- las preguntas con la recomendada aplicada;
- los riesgos.

Empuja tus dos ramas. El escritorio las revisa e integra.
