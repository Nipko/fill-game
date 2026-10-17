# Prueba de la ola 4 de la tanda 4 en el teléfono (2026-10-16): resultados para el escritorio

El portátil compiló `integracion-fase1-fase2` en `8e1d65c` («Cierra la ola 4 de la tanda 4…»), instaló el `dev.apk` en el SM-S938B por depuración inalámbrica y siguió el «QUÉ PROBAR EN EL TELÉFONO» de `8e1d65c`, más una regresión corta.

**Resultado: cumple.**
- Cumplen:
  - el audio por sondas (efectos, hápticos, ambiente y música);
  - Ajustes de Sonido, Música y Vibración;
  - el silencio durante el anuncio;
  - el Mapa del río;
  - el viaje de cambio de mundo;
  - el mundo 6 hasta el fin de contenido;
  - los arreglos de las olas 2 y 3;
  - la regresión.
- **No hay defectos.**
- **Escuchar el audio queda para el dueño:** el portátil solo comprueba las sondas, no cómo suena.
- Hay cinco observaciones (al final).

Las capturas, los vídeos y los logcat están en el portátil, en `Logs/device/tanda4o4/` (fuera de git).

## Verificación (portátil ARM64)

| Comando | Resultado |
|---|---|
| `dotnet build dotnet/AmazonFill.slnx -warnaserror` | 0 avisos, 0 errores |
| `dotnet test dotnet/AmazonFill.slnx` | 975 superados, 6 omitidos (981) |
| `python tools/validate_schema.py` | 103/103 PASS |
| `unity run … -executeMethod AF.EditorTools.BuildDev.BuildAndroidDev` | Código 0: `dev.apk` de 66,76 MB (2.º intento; ver nota) |
| `adb install -r Builds/Android/dev.apk` (inalámbrico) | `Success` |

Notas:
- **El primer build falló por licencia** (código 198 del Editor, «No valid Unity Editor license found»; el cliente de licencias decía «Access token is unavailable»):
  - abrir Unity Hub no bastó;
  - el dueño revisó la sesión del Hub y el segundo build pasó;
  - aun así, `unity license status` sigue dando «No se puede acceder al cliente de licencias»: es la CLI beta, porque el Editor sí valida.
- Los tests de Unity no se lanzaron en el portátil, porque tumban el adb del teléfono. El escritorio da los recuentos de `8e1d65c`.
- **Las soluciones A\* del mundo 6** (201, 243, 261-263, 280, 283, 299 y 300) se sacaron en el portátil con `AStarSolver` sobre `mundo_06_v1.json`, y todas ganan en el cliente sin rechazos.

## Resultados

### 1 · Sonido (AU1): las sondas cumplen; falta escucharlo

- **Nivel 1:**
  - efectos: `sfx_select` (con su `tint` por fruta), `sfx_pour_start`/`end`, `sfx_seal`, `sfx_win_toada`, `sfx_confetti` y `sfx_seeds_tick`;
  - hápticos: `haptic_seal` (×2) y `haptic_win`.
- **Aguas estancadas y comodines (40):** `sfx_stuck`, `sfx_undo` (Marea), `sfx_capi_paddle` con `haptic_capi`, `sfx_river_rise`, `sfx_fill`, `sfx_tint`, `sfx_fruit_plip` y `sfx_monkey_chirp`.
- **Atrás en un modal del embudo:** `haptic_back_blocked`, sin efecto de sonido.
- **`audio_build` por arranque en frío:**
  - hilo de trabajo 306-400 ms;
  - **hilo principal 3,3-8,6 ms**, frente al objetivo de < 50 ms;
  - 89 clips y 6.249 KiB.
- **Rechazo no comprobado.** Con adb no salió `reject`: tocar un destino que no admite el vertido cambia la selección (D-002), así que no pude provocar el sonido de madera ni comprobar que no vibra.
- **No probé** los efectos de Morpho, Pororoca, Luciérnaga, Boto ni Volver a la orilla.
- Escuchar si algún efecto cansa o suena fuerte queda para el dueño, con el volumen subido.

### 2 · Ajustes y anuncio: cumple

- **Las filas existen:** Ajustes trae `sound`, `music` y `haptics` (activadas), y la línea `settings` incluye `sound`, `music` y `haptics`.
- **Apagan al momento:**
  - apagar las tres da `settings_change` con `value: false`;
  - en el nivel 3 siguiente: 0 líneas `sfx` y 0 `haptic`;
  - volver a encenderlas devuelve los efectos y los hápticos en el 4.
- **Anuncio simulado** (intersticial tras el 32):
  - mientras dura (cuenta atrás y «Cerrar»), 0 líneas `sfx` y `haptic`;
  - al cerrar, sale `ad_closed` con `completed` y la música vuelve (`music` con `ducked: false`).
- **No probé** el teléfono en silencio.

### 3 · Ambiente y música (AU2): las sondas cumplen; falta escucharlo

- **`ambient` con el agua de cada mundo:** mundo 1 en el 1, 2 en el 11, 3 en el 41, 4 en el 81 y el 95, 5 en el 141 y 6 en el 201.
  - Llueve en el 60 y el 81, y no en los otros.
- **`music`:**
  - al cargar, `layers: 0`;
  - sube a 1 y a 2 al sellar;
  - baja de volumen (`ducked`) durante la victoria.
- **En el 60 (Muy difícil) sale `night: true`**, el lecho más grave.

### 4 · Mapa del río (M3.3, M3.4): cumple, con la observación 3

- **Entrada** con `--ei af_max_level 58`:
  - el inicio trae `map_tab` (botón de la canoa con punto rojo);
  - al tocarlo sale `map_open` y la pantalla `river_map`, con `back`, `view_world`, `view_basin`, `play` y `stop_remaining`.
- **Vista Mundo:**
  - la cabecera dice «Manaos · ¿Por qué el río Negro es negro?»;
  - el río de vidrio va lleno desde el océano hasta Manaos, con la canoa;
  - abajo, «Faltan 3 niveles para Anavilhanas» y «Jugar · Nivel 58».
- **Vista Cuenca:** toda la cuenca, con los países y los rótulos «Mundo 4 · Trapecio · Nivel 81», «Mundo 5…», «Mundo 6 · Oriente · Nivel 201» y «Mundo 7 · Madre de Dios · Nivel 301».
- Archivo: `h_mapa.jpg`.

### 5 · Viaje de cambio de mundo (M3.6): cumple, con la observación 1

- **Banners con viaje:**
  - el 41 tiene `travel: true` y `travel_ms: 3200`;
  - el 81 tiene `travel_ms: 4000` y `flag: CO`.
- En los dos salen la pregunta («¿Por qué el río Negro es negro?» y «¿Un río puede ser frontera?»), la franja del Mapa con la canoa avanzando, Capi y Rosa, y «Ver mapa».
- En el 81, la bandera de Colombia aparece al llegar la canoa.
- **«Ver mapa»** (`world_banner_map`) abre el Mapa con «Jugar · Nivel 41» o 81.
- Archivo: `h_viaje.jpg`.

### 6 · TiTino en la victoria (TI4, TI6a): no sale en ninguna (esperado)

- En las victorias de 261, 262, 263 (3-4 filas) y 280 no aparece TiTino y no hay ninguna línea `sloth`. El QUÉ PROBAR ya avisa: «en tableros de 2+ filas casi nunca cabe».
- En la llegada del 280 («El Coca · Ecuador»), la placa lleva el retrato del perezoso.
- Archivo: `h_titino.jpg`.

### 7 · Mundo 6 (C1a, VA1a): cumple

- **201:** banner del mundo 6 con viaje («Mundo 6: Oriente · Ecuador · Napo + lagunas negras de Cuyabeno»); fruta `cacao.01`.
- **243:** tarjeta de estreno de la Liana de cuenta (`intro: liana_n`): «Las cuentas de la liana dicen cuántas cuias debes sellar para que se suelte».
- **283:** estreno de la Hoja atada (`intro: hoja_atada`): «La hoja atada nunca cae: solo recibe su fruta, pero también puedes verter desde ella».
- **300:**
  - es Muy difícil, con la Hoja atada y la Liana;
  - la tarjeta dice «Si lo superas, llegas a Puerto Heath»;
  - en la victoria sale la llegada «Bolivia te da la bienvenida».
- **Siguiente tras el 300:** `content_end`, «El río sigue creciendo · Río arriba llegan 100 niveles nuevos en unos 14 días».
- Los cinco ganan con 0 rechazos. Archivo: `h_m6.jpg`.

### 8 · Arreglos de las pruebas anteriores (FX4, FX5): cumple

- **El 71 en frío:**
  - `build_ms` de 0,13 ms;
  - el primer uso va aparte, en `warm_ms` (7,38 ms);
  - en el 41 en frío, `build_ms` de 0,06 ms y `warm_ms` de 4,48 ms.
- **Placa del rótulo:**
  - en el 131 y el 191 va 6 dp por encima de los corchos de la fila 1 (`mode: plate`); en el 151 y el 171, de lado (`plate_side`);
  - al elevar una cuia de la fila 1 bajo la placa, esta se oculta en el acto (`stop_label_off`, 1 ms después del `select`), como describe FX4;
  - ya no se rozan.
- **Al 130 %:**
  - «Reiniciar» sale entero;
  - con el perfil limpio, Aguas estancadas trae Puñado y Balsa (`balsa`, `balsa_note`);
  - si la Balsa ya se enseñó hoy, no sale, y la sonda lo dice (`balsa_ok: false`, `balsa_shown_today: 1`).
- **En-XA:** el miquito del 5 sale como «[móñó árdíllá ····]».
- Archivos: `h_arreglos.jpg` y `h_placa2.jpg`.

### 9 · Regresión: cumple

- **Partida automática con la solución A*:** el 1-12 seguidos con «Siguiente» (perfil limpio), y el 19-21, 41, 63, 100, 143 y 183.
- **20/20 ganados**, con el tablero idéntico al pack, 0 rechazos, 0 Aguas estancadas falsas y 0 avisos `step_budget`.
- La línea de fruta y la llegada salen como en la ola 3, con «Siguiente» en `y = 1392`.
- Banner en el 11 y el 41, tarjeta en el 63, 100, 143 y 183, y `bite_demo: animated` en el 183.
- 0 excepciones ni líneas `E Unity` en los logcat de toda la prueba.

## Observaciones

1. **Para el dueño: el banner con viaje no responde a los toques mientras dura el viaje** (3,2 s en el 41 y 4 s en el 81). Solo el toque de después lo cierra. Quien toque para saltarlo no ve respuesta. ¿Debe un toque completar el viaje, como en la franja de llegada?
2. **Para el escritorio: `step_budget` en la victoria del 280** (4 filas, 13 cuias), el único aviso de presupuesto de pasos de toda la prueba.
3. **Para el dueño, en el Mapa:**
   - en la vista Cuenca, los rótulos «Mundo 4 · Trapecio» y «Mundo 6 · Oriente» van casi pegados;
   - dos arrastres por adb (`input swipe`) no movieron la vista Cuenca. Puede ser que esa vista no se arrastre porque cabe entera; el arrastre real lo prueban los tests de M3.3.
4. **Para el escritorio: el rechazo no se puede provocar con adb**, porque un destino inválido cambia la selección. Si hace falta comprobar el sonido de madera, haría falta un extra de desarrollo o un caso en el que el rechazo exista.
5. **El teléfono:** 6 GB libres (antes, 1,5) y batería al 67 % al empezar.

## Respuesta del escritorio (FX6)

- **Observación 2, `step_budget` en la victoria del 280:** no es un tirón de fotogramas ni el trabajo de la victoria (TiTino, la línea de fruta, la llegada o el banner), sino el reloj del presupuesto de los pasos de mecánica.
  - **Reproducción en el Editor** (360 × 780 dp; ahí el tablero sale en 3 filas, D-006, y las filas no influyen): la solución A* del 280, con un vertido cada 0,5 s como el guion del teléfono. Esperando a la vista quieta entre vertidos no sale.
  - **Qué se agotaba:** la solución acaba con cuatro vertidos encadenados por sus cuias: 1 → 12, 1 → 10, 0 → 1 y 0 → 6. Cada uno espera a que el anterior suelte una cuia que comparten. El penúltimo, 0 → 6, sella la 6 y cuenta en la Piraña (Seal y BiteLost, 850 ms de pasos). Llegaba a su caudal 2.630 ms después de su propio toque, y el presupuesto de 2,8 s contaba desde ese toque: le quedaban 170 ms, ni a × 0,35 cabía y saltaba la etapa (e). Por eso sale en la victoria, y no en el resto de la prueba: solo ahí cuatro vertidos seguidos comparten cuias y el último trae pasos.
  - **Línea de antes:** `{"event":"step_budget","age_ms":2630,"planned_ms":850,"left_ms":170,"speed":0.35,"skip_from":"E"}`.
- **Arreglo (FX6):** el presupuesto mide bien su plazo; solo estaba mal el reloj.
  - Sus 2,8 s protegen del corte de la celebración o del modal, que llega como mucho 3.000 ms después del toque que gana o atasca (`MaxViewDrainWaitMs`). Después de ese toque ya no se acepta ninguno, así que es siempre el último.
  - Por eso ahora cuenta desde el último vertido tocado (`BoardAnimator.SinceLastPourMs`) y no desde el propio. Con un solo vertido en la cola, los dos coinciden y nada cambia.
  - En el 280, el plan de 0 → 6 cabe a × 0,80 y se ve entero, sin `step_budget`.
  - La línea `step_budget` lleva ahora también `since_tap_ms`, el reloj del presupuesto (`docs/dev/prototipo-v0.md`).
  - Lo prueba `StepBudgetVictoryTests` (PlayMode): gana el 280 a 0,5 s por vertido y esperando a la vista quieta. Falla si sale `step_budget`, si salta alguna etapa o si algún paso que no saltó por la cola acaba más de 2,8 s después del último toque.
- **Lo que queda, sin aviso:** al ritmo del guion, el vertido que gana (5 → 12) espera detrás de esa cadena, y la celebración lo lleva a su final a los 3 s del último toque (`MaxViewDrainWaitMs`, el tope [P] del tech-lead). Pasaba igual antes de FX6, y ni ese salto ni los de `queue_max_lag_pours` escriben línea. A un ritmo de persona, esperando a la vista quieta, la vista acaba 1,28 s después del último toque y no salta nada.
- **Para probar en el teléfono:** `af_level 280` con la solución A* a 0,5 s por vertido. Debe dar 0 `step_budget`, y en la victoria, `step` del Seal de la 6 y del BiteLost de la Piraña con `"snapped":false`.
