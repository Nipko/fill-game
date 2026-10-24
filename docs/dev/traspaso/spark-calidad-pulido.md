# Encargo al equipo Spark: tests fiables y pulido de pantalla (2026-10-24)

Lo escribió el integrador (escritorio). El dueño pidió que Spark avanzara en lo pendiente mientras el escritorio integra la tanda 6. No hay decisión nueva: son arreglos de calidad de lo que ya está en el GDD y en D-046.

**Reglas:** las de siempre, en `spark-reto-del-rio.md` §1 y §2:
- `CLAUDE.md` entero;
- ramas `spark/*`, sin push force; el escritorio integra;
- la verificación completa antes de cada push, con tu arquitectura y tu Python;
- cada carril pasa por **code-reviewer** hasta APROBADO.

Nada nuevo sin el dueño: ni paquetes, ni SDK, ni permisos, ni cambios en `ProjectSettings`. Si hace falta decidir algo, anota la pregunta con la recomendada aplicada y [P].

**Base:** `origin/integracion-fase1-fase2` con la tropa de 3 filas ya integrada (el commit «Integra spark/t6-tropa-3filas…» o posterior). Mide tu línea base de EditMode y PlayMode antes de tocar nada. PlayMode ya tarda ~55 min: lánzala con `-TimeoutSec 7200`.

**Dos carriles independientes, un worktree para cada uno** (siembra la `Library` con robocopy). Para no pisaros:
- el carril A toca sobre todo tests (y, si hace falta, un gancho de reloj mínimo);
- el carril B toca la UI y sus tests;
- si los dos necesitan el mismo test, que lo cambie el B y el A lo deje para el final.

## Carril A · `spark/t6-tests-fiables`: tests que no dependen de la hora, capturas al día y una suite más rápida

**Por qué:** el 2026-10-24 fallaron dos tests solo por la hora a la que se ejecutaron:
- `AudioQuickTogglesTests.Home_FitsWithoutOverlaps…(640)`: el saludo de TiTino apartaba la tarjeta del Reto;
- `PlayGamesCloudBootTests.ALateCloud_WithAWinWhileWaiting…`: el `economy_ts` de la nube era una fecha fija que se quedó en el pasado.

Los dos ya están arreglados en la integración. Lo más probable es que haya más.

1. **Auditoría del reloj:**
   - busca en `Assets/_Project/Tests/**` y en `Assets/_Project/Scripts/**/Tests/**` los usos de `DateTime.UtcNow`, `DateTime.Now`, `DateTime.Today`, `SystemClock`, `Time.realtimeSinceStartup` y las fechas fijas (`new DateTime(2026, …)`) que acaban comparándose con el reloj real del juego;
   - fíjate también en lo que depende de la hora local: saludo y poses de TiTino por hora, cambio de día del Reto, regeneración de canoas, la semana de la Liga (lunes a las 12:00 UTC) y `economy_ts`.
2. **Para cada caso:**
   - pásalo a reloj fijo (`ManualClock`, `LaunchOptions.ClockOverride`, `LeagueClockOverride`…) o haz la fecha relativa a hoy, como en el arreglo de `PlayGamesCloudBootTests`;
   - deja una tabla en `spark-estado.md` con test, causa y arreglo.
   - Si un sistema lee el reloj real sin gancho para los tests, añade el gancho más pequeño posible, del mismo estilo que los `…Override` de `GameBootstrap`, y dilo en la tabla.
3. **Guardia:** un test de EditMode que lea los `.cs` de tests y falle si aparece `DateTime.UtcNow`, `DateTime.Now` o `DateTime.Today` fuera de una lista blanca comentada (los casos en que el reloj real es justo lo que se prueba).
4. **Las capturas `[Explicit]`:**
   - las 5 que ya fallaban en tu línea base de la tropa: averigua por qué y arréglalas o regenéralas;
   - regenera las de la pausa y el inicio que cambió Música y Sonido (D-046, punto 2) y las de 3 filas que cambió la tropa (punto 3);
   - revísalas una a una y anota en `spark-estado.md` qué cambió en cada grupo.
5. **PlayMode más rápida:**
   - saca de `Logs/playmode.xml` los 30 tests más lentos y lo que más tiempo se lleva (cargas de la app, esperas fijas, `WaitForSeconds`);
   - aplica lo que no quite cobertura: compartir una carga entre casos de un mismo fixture cuando el test no la ensucia, cambiar esperas fijas por `WaitFor` con condición, avanzar el reloj a mano en lugar de esperar animaciones;
   - el objetivo es volver a unos 40 min, sin `[Explicit]` nuevos ni tests quitados: el número de tests no baja.
   - Anota el antes y el después.
6. **Verificación:**
   - la completa;
   - PlayMode dos veces seguidas, verde las dos;
   - si puedes, una tercera con el reloj del juego adelantado 12 h por `LaunchOptions` (no cambies el reloj de Windows).

## Carril B · `spark/t6-pulido-pantalla`: solapes y safe area pendientes

1. **320 dp, la Crecida y la tarjeta del Reto:** se cruzan desde antes de D-046 (x 290-316 frente a 12-308; es la excepción `KnownAt320` de `AudioQuickTogglesTests`).
   - Arréglalo y quita la excepción del test.
   - La regla es que nada se pisa en el inicio a 320 dp, al 100 % y al 130 % (02 §5 y §12), también con el botón «Liga» encendido (`Home_WithTheLeagueOn_…`).
2. **Q-T6T-5** (riesgo de la tropa, pregunta 68 de `docs/GDD.md` §6.3): en `en-XA` a dos líneas, la píldora Reiniciar pisa el contador de la Lluvia o la Piraña.
   - Arréglalo sin bajar de 12 sp.
   - Añade un test con `en-XA` y las dos mecánicas.
   - Marca el riesgo como resuelto en la pregunta 68.
3. **Safe area** (auditoría B10, `docs/dev/traspaso/b10-safe-area-y-retroceso.md`):
   - **StoreSheet:** «Cancelar» queda a unos 16 dp del borde físico. Tienes un parche propuesto en `b10-storesheet-safe-area.patch`: revísalo y aplícalo o rehazlo.
   - **AdOverlay y DailyStampCalendar:** que usen los márgenes de `OverlayCanvas`/`GameScreen.ReadScreen`.
   - **StuckModal a 16:9 con el texto al 130 %:** el desborde conocido de su test.
   - Tests con barra de gestos simulada (márgenes de safe area distintos de 0) para cada uno.
4. **Los brillos del río en la franja de la tropa:** su primera activación asigna memoria.
   - Precaliéntalos (activarlos una vez fuera de pantalla al cargar, o lo que se mida mejor).
   - Si lo consigues, quita `AmbientLayer.Frozen` de `PhoneFixesTests` y de las pruebas de asignaciones de `StopLabelTests`.
   - Si no es posible, déjalo escrito como riesgo, con la medida.
5. **GDD:** donde cambie una cifra [P] (02 §5, 07 §7.4), ponla al día citando D-046 o B10.
6. **Verificación:** la completa, más las capturas afectadas regeneradas y revisadas.

## Al terminar

Anota cada carril en `docs/dev/traspaso/spark-estado.md`, con el formato de siempre:
- los commits;
- la verificación con sus resultados;
- las preguntas con la recomendada aplicada;
- los riesgos.

Empuja tus dos ramas. El escritorio las revisa e integra.
