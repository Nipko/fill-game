# Guion de la prueba completa del juego en el teléfono

Lo escribió el integrador (escritorio) el 2026-10-25, a petición del dueño: **probar el juego entero, no solo lo nuevo**. Se mira cómo se comporta, los gráficos y las imágenes, los niveles, los comodines, el uso normal, perder y reintentar muchas veces, y todo lo que se pueda probar, **tarde lo que tarde**. Es para el portátil, con el SM-S938B (Android 16) y con el dueño al lado en las partes que necesitan ojos, oídos o ajustes del sistema.

**Punto de partida:** `integracion-fase1-fase2` en `cefbb9b7` o posterior. La última prueba en el teléfono fue sobre `a656cca4` (2026-10-23).

**Lo que entró desde entonces y nunca se ha visto en el teléfono:**
- Mercado oculto sin precios de Play y aviso de «tienda no disponible» (`6914b06f`);
- cliente de la Liga del río contra el backend de dev (`00a596df`, `ebe904ef`; su guion propio, `guion-prueba-telefono-liga.md`, se sigue entero en el bloque 12);
- Música y Sonido con un toque, en el inicio y en la pausa (`666045d0`);
- la franja de la tropa con 3 filas: suelo bajado 20 dp y avisos en la franja (`4c67ee85`);
- pulido de T6 (`f93652e9`):
  - tarjeta del Reto a 320 dp;
  - contador de Lluvia o Piraña junto a Reiniciar en dos líneas;
  - zona segura del anuncio y del calendario;
  - brillos del río sin asignaciones;
- tests fiables de T6 (`45529ef4`), que no cambian el juego pero sí las capturas.

**Lo que nunca se ha probado en ninguna prueba:**
- la pantalla 16:9;
- la fuente de Android de verdad;
- TalkBack;
- el modo avión;
- cambiar la hora;
- las skins de los hitos;
- la Hoja escudo;
- el Cántaro lleno;
- la ola de la Pororoca con Lluvia o Piraña;
- el `rehearsal.apk`;
- Remote Config aplicado;
- el audio escuchado de verdad.

Aquí entra todo.

## Reglas de la prueba

- **Orden:** sigue los bloques en orden. Entre bloques puede pasar un día; la hoja se escribe a medida y se commitea al final de cada jornada. Así, si se corta, el escritorio ya tiene lo hecho.
- **Hoja de resultados:** `docs/dev/traspaso/pruebas-telefono-<fecha>-completa.md`, con el formato de las anteriores:
  - verificación;
  - resultados por bloque: **cumple**, **no cumple** o **sin probar**, con la captura o la línea de logcat;
  - observaciones numeradas.

  Cada observación lleva gravedad: **bloqueante** (rompe, pierde progreso o dinero, cuelga), **alta** (se ve mal o confunde a cualquiera), **media** o **baja** (detalle).
- **Capturas y logcat:** van en `Logs/device/completa/<bloque>/`, fuera de git. El nombre de cada captura dice qué es: `b05-hoja-43-tutorial.png`.
- **El logcat de toda la prueba**, en otra consola y por jornadas:
  ```
  adb logcat -v time > Logs/device/completa/logcat-<fecha>-<n>.txt
  ```
  Filtra siempre las líneas de `DebugAppCheckProvider`, «debug secret», «debug token», `appcheck:debugtokens` y `eyJ`, y comprueba al final de cada jornada que no queda ninguna (incidente del 2026-10-22).
- **Nada de secretos en la hoja:** ni tokens, ni UID, ni URL firmadas, ni el contenido de `Assets/_LocalSecrets/`.
- **Ajustes del sistema del teléfono:**
  - `wm size` y `wm density`, el tamaño de fuente, TalkBack, el modo avión, la hora, el ahorro de energía y la navegación por gestos o por botones **los cambia el dueño o se cambian con su sí en el momento**;
  - se anota el valor de antes y **se deja como estaba** al terminar cada bloque (`adb shell wm size reset`, `adb shell wm density reset`, hora automática otra vez).
- **Depuración inalámbrica:** si `adb devices` sale vacío, pide al dueño que la reactive y no insistas.
- **Excepciones:** «0 `E/Unity`» es un criterio de **todos** los bloques. Al final de cada bloque:
  ```
  adb logcat -d | grep -E "E Unity|E/Unity|Exception|MissingMethod|TypeLoad|NullReference"
  ```
  Cada excepción es una observación, con su traza.
- **Sondas:** `AF_LAYOUT`, `AF_STATE`, `AF_EVENT` y `AF_PERF`, descritas en `docs/dev/prototipo-v0.md`. Para tocar, usa la última línea `AF_LAYOUT` de la pantalla que está a la vista.
- **Jugador automático:** el de las pruebas anteriores, con la solución A* y en modo `run`. Para los bloques de pérdida y de uso, **juega a mano** o con vertidos malos a propósito: el jugador automático nunca se bloquea.
- **Reiniciar con vertidos hechos** pide confirmación («¿Volver a la orilla?», con Seguir jugando / Deshacer · Marea / Volver a la orilla): tenlo en cuenta en el jugador automático.
- **`GooglePlayGameSettings.txt` modificado tras un build:** si `git diff ProjectSettings/GooglePlayGameSettings.txt` no muestra cambios de contenido, ejecuta una vez `git add --renormalize ProjectSettings/GooglePlayGameSettings.txt` y `git checkout -- ProjectSettings/GooglePlayGameSettings.txt`: el árbol de trabajo pasa al fin de línea nativo que fija `.gitattributes` (`text !eol`, de `6914b06f`) y deja de salir.
- **Actividad:** `com.amazonfill.dev/com.unity3d.player.UnityPlayerGameActivity`. Los extras (`af_level`, `af_max_level`, `af_lang`, `af_font_scale`, `af_fake_store`, `af_league`, `af_league_fake`, `af_dev_push`, `af_dev_reject`, `af_mech_off`, `af_ads_fill`, `af_seeds`, `af_canoes`, `af_rules_pour_mode`) están en `LaunchOptions.cs` y en `prototipo-v0.md`. Úsalos para llegar rápido a cada caso, pero haz **al menos un recorrido natural** (bloque 2).

## 0. Preparación y builds (≈ 1 h 30 min)

1. **Actualiza y verifica:**
   ```
   git pull origin integracion-fase1-fase2
   dotnet build dotnet/AmazonFill.slnx -warnaserror
   dotnet test dotnet/AmazonFill.slnx
   python tools/validate_schema.py
   ```
2. **Build de desarrollo**, con `Assets/_LocalSecrets/google-services.json` como el 23 (sin abrirlo; comprueba solo que existe con `git status --ignored`):
   ```
   pwsh -NoProfile -File scripts/unity-build.ps1 -Method Dev
   pwsh -NoProfile -File ci/check-manifest.ps1 Builds/Android/dev.apk -Mode dev
   pwsh -NoProfile -File ci/check-no-unity-iap.ps1 Builds/Android/dev.apk
   bash ci/check-16kb.sh Builds/Android/dev.apk
   ```
   - Anota el tamaño del `dev.apk`. El escritorio midió unos 72,6 MB el 23.
   - El build deja `ProjectSettings/GooglePlayGameSettings.txt` cambiado solo en los finales de línea: descártalo. Si no aparece, es que el arreglo de `6914b06f` funciona; anótalo.
3. **Build de ensayo de release** (sin firmar con la clave de subida, que no existe todavía):
   ```
   pwsh -NoProfile -File scripts/unity-build.ps1 -Method Release -Rehearsal -Apk
   ```
   Sobre el AAB y el APK que deja, en `Builds/Android/`:
   ```
   pwsh -NoProfile -File ci/size-gate.ps1 <aab>
   bash ci/check-16kb.sh <aab>
   pwsh -NoProfile -File ci/check-manifest.ps1 <aab> -Mode dev
   pwsh -NoProfile -File ci/check-release-classes.ps1 <aab>
   pwsh -NoProfile -File ci/check-no-unity-iap.ps1 <aab>
   ```
   Anota los tamaños: el AAB pesó 55,94 MB en el escritorio, con un tope de 80. El ensayo se instala en el bloque 20.
4. **Instala el de desarrollo con un perfil limpio:**
   ```
   adb install -r Builds/Android/dev.apk
   adb shell pm clear com.amazonfill.dev
   adb logcat -c
   ```
5. **Estado del teléfono:** anota la versión de Android, el tamaño de pantalla (`adb shell wm size`), la densidad, la escala de fuente, el tipo de navegación (gestos o botones), la región, el idioma y el nivel de batería.

## 1. Primer arranque y primera sesión (niveles 1-12) (≈ 1 h)

Con el perfil limpio y **a mano**, como un jugador nuevo. El dueño mira la pantalla. Guía: 06 §2.2, la primera sesión minuto a minuto.

1. **Arranque en frío:** mide el tiempo con `adb shell am start -W -n com.amazonfill.dev/com.unity3d.player.UnityPlayerGameActivity` (anota `TotalTime`).
   - Pantalla de carga, sin pantalla negra larga ni parpadeos.
   - En logcat, `[AF.Ads] proveedor: not_configured` y ninguna línea de AppLovin ni de Play Games antes de la edad.
2. **Edad:** «¿Cuántos años tienes?» con tres botones iguales. Atrás no la salta. Elige 18+.
3. **Consentimiento:** «Tu privacidad», con las dos casillas sin marcar y «Rechazar todo» y «Aceptar todo» al mismo nivel. Elige «Aceptar todo».
4. **Niveles 1-10:**
   - **1:** el dedo guía sobre el primer vertido; el vertido por tramo (D-005); la elevación del origen, sin resaltar destinos (D-002); el sellado al llenar una cuia de un color; la victoria.
   - **2:** el dedo sale tras el primer vertido, cuando la lección ya es posible (DC6). La banda de TiTino.
   - **3:** el tooltip de Marea tras un vertido que no acerca. Marea ilimitada hasta el 10.
   - **6:** el tooltip «Reiniciar es gratis…» encima de Aguas estancadas, con el pico sobre «Volver a la orilla (gratis)».
   - **7:** la tarjeta del Reto del río ~1 s después de la victoria, bajo TiTino, sin que la tape.
   - **8:** la Cuia extra regalada, con el dedo sobre «+1 cuia». El dedo espera a que se vaya el aviso de Capi (no sale con otro aviso a la vista): no la uses antes. Úsala y comprueba que el tablero se recoloca con N + 1 cuias.
   - **10:** la secuencia de la victoria (06 §2.3, cuatro pantallas como mucho, cada una se cierra con un toque): Canasta (+50), Bitácora con tres premios (elige uno y se sella), «Seguir» abre el Mapa del río (`source: l10`) y vuelta al inicio con «Bitácora · Sellada hoy».
5. **Niveles 11 y 12:**
   - **11:** banner del mundo 2, Tapajós (`world_banner`, con su arte, y `wait_ms` ≈ 0 si se pidió en la victoria del 10). Cosecha doble con el tooltip, sin tapar «Cosecha +20»; vídeo simulado y +20.
   - **12:** Morpho, con 3 regalados y su tarjeta. Usa una pista: señala un vertido que lleva a la solución.
6. **Segunda sesión:** `am force-stop` y vuelve a abrir.
   - Vuelves al inicio con el progreso intacto.
   - En la primera victoria, con el 10 superado, sale el pre-aviso de notificaciones al pulsar «Siguiente». Esta vez di «Sí»: sale el diálogo nativo de Android 13+ y lo aceptas (`af_push_prompt`).
   - En la primera victoria con nivel ≥ 12 en la sesión 2 sale la Mochila de inicio (05 §5) o no sale porque la tienda no está disponible. Anota cuál de las dos y si tiene sentido.
7. **Lo que mira el dueño:** que se entiende sin leer, que el ritmo no aburre ni agobia y que nada parece roto o provisional. Sus frases van tal cual como observaciones.

## 2. Recorrido natural de la campaña (1-60 a mano y con el jugador automático) (≈ 3-4 h)

Sigue con el perfil del bloque 1, sin `af_max_level`: es la prueba de que el progreso se encadena bien.

1. **Del 13 al 60**, alternando: un tramo a mano (unos 10 niveles por hora, con fallos reales) y otro con el jugador automático.
2. **En cada nivel**, de paso:
   - la línea `AF_STATE load` y `win`;
   - la tarjeta previa cuando toca;
   - el fondo de la parada;
   - el rótulo del lugar;
   - TiTino en la victoria si cabe;
   - las semillas: `earn_virtual_currency` con la cantidad de 05.
3. **Hitos del camino**, con una captura de cada uno:
   - **14:** Cuia chica de 2, con su tutorial de una pantalla (estreno de 02 §8). **18:** de 1. **22:** de 3. Estas dos no son estrenos (la lista única de 02 §9.1 solo tiene el 14 y el 27), así que salen sin tutorial.
   - **20:** primera Canasta con comodín y el Cántaro visible.
   - **22 en adelante:** intersticial simulado tras la victoria: 1 cada 3 niveles, ≥ 150 s desde el anterior, nunca en un reintento ni tras rendirse. Anota cada intersticial con su nivel y su hora.
   - **25:** tarjeta de la Crecida y la barra. Gana 3, 6 y 10 seguidos y anota los hitos (`streak_milestone` 3 = +300, 6 = cofre, 10 = Cuia extra y río crecido).
   - **27:** Cuia alta de 5, con su tutorial.
   - **30:** tarjeta de la Pororoca.
   - **31:** tarjeta de las Canoas. Desde aquí, **cada fallo cuesta una canoa** (bloque 7).
   - **40:** primer Difícil, con el chip y la cinta «×3» en Jugar y en Siguiente, la tarjeta previa de la rana dardo, el premio de 60 y la **skin Cuia de Santarém** al ganarlo. Comprueba que se ve en el tablero.
   - **41:** banner de Manaos, los Patrones de fruta activados (`patterns.on` true, opacidad 0,20) y el botón «Liga» en el inicio si la Liga está encendida por Remote Config (si no, en el bloque 12).
   - **43:** estreno de la Hoja (bloque 5).
   - **60:** primer Muy difícil: el jaguar, «Noche de jaguar», el premio de 80 con la cinta «×4» y la variante nocturna de la música.
4. **El Mapa en cada parada** (cada 10 niveles): la llegada, el dato del río, la fruta nueva, la canoa que avanza y «Premio del mundo · faltan N».

## 3. Núcleo del juego: tablero, vertido y gestos (≈ 1 h)

Con `af_level` para ir directo.

1. **Vertido por tramo (D-005):**
   - una racha de 3 en un hueco de 2 vierte 2 y deja 1;
   - verter en una cuia vacía;
   - verter sobre el mismo color;
   - un vertido ilegal sobre otro color o una cuia llena, que cambia la selección (C5);
   - con `--ei af_dev_reject 1`: rechazo con sacudida y `sfx_illegal`, sin háptico.
2. **Selección:** tocar el origen lo eleva y tocarlo otra vez lo baja. Tocar una cuia vacía sin nada elegido es un rechazo (C2). No se resalta ningún destino (D-002).
3. **Gestos:**
   - arrastrar de una cuia a otra vierte;
   - soltar en el fondo cancela;
   - mantener pulsada una cuia muestra el bocadillo con sus frutas, sin seleccionarla;
   - dos dedos a la vez sobre dos cuias: no se cuelga ni hace dos vertidos raros;
   - toques muy rápidos, 10 por segundo durante 10 s sobre cuias distintas: sin excepciones; las ráfagas de `step_budget` valen, no son un fallo.
4. **Modo `single`:** con `--es af_rules_pour_mode single`, el 5: cada vertido mueve una dosis.
5. **Disposición del tablero** (D-004, D-006 y la franja de la tropa). Una captura por caso, con el ancho de cuia de `AF_LAYOUT`:

   | Caso | Niveles |
   |---|---|
   | T ≤ 3 cuias | 1 |
   | 5-7 cuias | 2, 7 |
   | 8-10 cuias, 2 filas | 11, 22, 63 |
   | 11 cuias, 3 filas | 86 |
   | 12 cuias | 123 |
   | 13 cuias | 240 |
   | 14 cuias | 335, 343, 600, 700 |
   | Cuia alta de 6 | 160 |

   - En los de 3 filas, la **tropa** entra por su franja de 56 dp, sin pisar la fila del HUD ni el panel, y el panel queda a ~11,5 dp de la bandeja.
   - **15 y 16 cuias:** en un nivel de 14 cuias (el 335), añade la Cuia extra y después rescata (bloque 7) para llegar a 16. Deben quedar 6/5/5, sin cuias fuera de la pantalla y todas tocables.
6. **Patrones de fruta:** Ajustes › Patrones en Automático (el 41 activo, el 30 inactivo), Siempre y Nunca. Se aplica al momento, también al nivel abierto.
7. **Pista por inactividad** (Ajustes, `settings_idle_hint`): sin tocar durante el tiempo configurado, ¿sale la pista? Con el ajuste apagado, no.

## 4. Comodines (≈ 2 h)

Para cada uno: con saldo, sin saldo, con vídeo simulado y, donde aplique, en el bloqueo. Usa `--ei af_seeds <n>` para fijar el saldo. Guía: 04 §2 y §3.

1. **Marea (deshacer):**
   - ilimitada en 1-10;
   - 3 gratis por nivel desde el 11, con el contador visible;
   - agotada: recarga por vídeo simulado (`rv_undo_refill`) y por 30 semillas;
   - con `--ei af_ads_fill 0` (sin relleno): solo por 30 semillas, sin el botón de vídeo (rama E);
   - funciona dentro de Aguas estancadas;
   - el tope de vídeos del día (4) y la espera de 60 s.
2. **Cuia extra:**
   - con stock;
   - sin stock, por 150 semillas;
   - una por nivel;
   - **se conserva al reiniciar** el nivel;
   - no se puede usar en el bloqueo, donde pasa a ser la Cuia de rescate.
3. **Morpho (pista):**
   - con stock, por 100 semillas y por vídeo (`rv_hint`, 3 al día);
   - no se puede usar en el bloqueo;
   - en Aguas turbias (100, 106) solo propone vertidos seguros o no cobra;
   - en un nivel con `hint_supported` falso: el globo de Pista sale en su sitio (en el 135, en la franja si pisaría el panel).
4. **Pororoca (barajar):**
   - en el 30: confirmación por 300 semillas, `shuffle` `shuffled` y el botón en gris en ese nivel;
   - una por nivel y tres al día;
   - **en un nivel con Lluvia (143) o con Piraña (183): deshabilitada**, con su motivo;
   - en un nivel con mecánicas (43, 63): baraja solo lo libre. Si falla, sale «La ola no pudo remover el río», no cobra y se puede repetir;
   - salir y volver al nivel después de barajar: el tablero barajado se conserva o no, según 04 §2.4. Anota cuál pasa.
5. **Luciérnaga (revelar):**
   - en el 100 regala 1 y su tarjeta combinada con Aguas turbias;
   - revela una cuia turbia y **lo revelado se conserva** al reiniciar;
   - por 120 semillas y por vídeo (`rv_reveal`).
6. **Salto del Boto:** sale 5 veces del 32 (`fails_level: 5`). Comprueba el banner de Rosa y «Salto del Boto · 300» en la pausa y en Aguas estancadas (rama D). Al confirmar, cobra 300, carga el 33, registra `af_skip_used` y no da premio. **No vuelve a ofrecerse hasta 10 niveles después.**
7. **Volver a la orilla (reiniciar):** gratis siempre, con el mismo tablero. En 1-30, sin canoa. Desde el 31, desde la pausa, **¿cuesta canoa?** Comprueba lo que dice 04 §2.7 y anótalo.
8. **Cosecha doble:**
   - el botón ×2 en la victoria desde el 11, que duplica las semillas del nivel;
   - tope de 5 al día;
   - con la Crecida en 10, `free_double_harvest` sin botón;
   - en el Reto del río: comprueba si existe y qué hace.
9. **Música y Sonido con un toque (nuevo):**
   - en la pausa, los discos junto a la X;
   - en el inicio, en columna bajo el engranaje;
   - apagar uno en la pausa lo apaga en Ajustes y en el inicio, y viceversa;
   - tachados cuando están apagados;
   - `AF_STATE settings_change` con `source` `pause`, `home` o `settings`;
   - la Vibración solo está en Ajustes.

## 5. Mecánicas del río: cada estreno y una combinación (≈ 2 h 30 min)

En cada estreno:
- el **tutorial de una pantalla**, que hace de tarjeta previa;
- que se entiende sin leer el GDD;
- que la mecánica hace lo que dice 02 §9.1;
- las líneas `AF_STATE step` de sus pasos, sin `snapped` raros;
- ganar el nivel a mano **y** con el jugador automático.

Luego, un nivel posterior de esa mecánica mezclado con otra.

| Mecánica | Estreno | Después |
|---|---|---|
| Hoja | 43 | 52 |
| Matapi | 63 | 72 |
| Liana con fruta | 83 | 92 |
| Aguas turbias + Luciérnaga | 100 | 115, 135 |
| Remolino | 103 | 112 |
| Barro | 123 | un reto de 121-140 |
| Lluvia (contador) | 143 | 152 |
| Brote | 163 | 172 |
| Piraña, con el mordisco animado (`bite_demo`) | 183 | 192 |
| Liana de cuenta | 243 | 318 |
| Hoja atada | 283 | 354 |
| Brote de dos hojas | 343 | 420 |
| Flor de vitória-régia | 403 | 498 |

**Además:**
- **Contador de Lluvia o Piraña junto a Reiniciar en dos líneas** (nuevo): en el 194 y el 200 con `--es af_lang en-XA` al 100 y al 130 %, el contador va a la izquierda de la píldora, sin tocarla. En es-419, a la derecha.
- **Interruptores de emergencia:** `--es af_mech_off hoja` en el 43, que se juega el nivel publicado con su aviso.
- **Tinaja (323):** no va en la campaña v1 (variante B de D-019). Anota qué sale en el 323.

## 6. Niveles: muestra amplia con el jugador automático (≈ 3-4 h, sin vigilancia)

Con el jugador automático, perfil con `af_max_level` alto y `af_level` en cada uno. Cuenta los ganados y las excepciones.

1. **Todos los del 1 al 200,** uno tras otro.
2. **Del 201 al 700, uno de cada cinco:** 205, 210… 700.
3. **Todos los del 691 al 700, y el viaje final del 700:**
   - `map_journey` con `by: time`;
   - la pantalla de fin de contenido (`content_end`), con un texto que tiene sentido;
   - qué pasa al tocar «Siguiente» después.
4. **Cada 50 niveles:** `adb shell dumpsys meminfo com.amazonfill.dev` (anota el `TOTAL PSS`) y la temperatura (`adb shell dumpsys battery`). La memoria no debe crecer sin parar.
5. **Resultado:** N/N ganados, los que fallaron con su `id` y la línea `AF_STATE` final. Cualquier nivel que el jugador automático no gane es una observación alta.

## 7. Perder, rendirse y reintentar muchas veces (≈ 2 h 30 min)

**A mano**, con vertidos malos a propósito. Guía: 04 §6 (ramas) y §7 (canoas), y D-008. Una captura de cada modal.

1. **Rama C (antes del 31):** bloquéate en el 25 y en el 30. Sale Aguas estancadas con Deshacer y «Volver a la orilla (gratis)», sin canoa y sin «El río bajó».
2. **Rama A (desde el 31, con saldo):** bloquéate en el 35 con 1.000 semillas.
   - Sale «Continuar: +1 cuia» por **300** (el primero del día) y Deshacer, con «Rendirse» al pie y «Travesía perdida: −1 canoa».
   - Rescata: la cuia de rescate entra y se puede ganar.
   - Bloquéate otra vez en otro nivel: ahora cuesta **600**.
3. **Rama S (¿Seguro?):** «Rendirse», y sale ¿Seguro? con «Perderás 1 canoa» y el rescate otra vez. X o «Rendirse» → Nivel fallido, con −1 canoa.
4. **Rama A′ (segundo bloqueo del mismo nivel tras rescatar):** solo Deshacer y el pie. No hay un segundo rescate.
5. **Rama B (sin saldo):** con `--ei af_seeds 0`:
   - «Continuar» en gris con «Te faltan N»;
   - la oferta del Puñado o la Balsa: sin tienda, ¿qué sale?;
   - con `--ez af_fake_store true`, la compra simulada y el rescate.
   - En niveles de 3 filas (el 86), la burbuja «Te faltan N» va a la franja si pisaría el panel.
6. **Rama D (5 fallos):** ya vista en el bloque 4.6.
7. **Rama F (irresoluble con +1):** si encuentras un bloqueo en el que el rescate no vale, no se ofrece y sale `af_rescue_unsolvable`.
8. **Rama G (1 canoa):** «Travesía perdida: tu última canoa». Tras rendirte, «Reintentar» lleva a Sin canoas.
9. **Sin canoas:**
   - el cronómetro;
   - «Canoa extra» por vídeo simulado (3 al día, cada 90 s);
   - «Llenar canoas» y «Canoas infinitas 1 h» (sin tienda: ¿qué sale?; con la tienda falsa: compra y canoas infinitas, rama H, en la que rendirse va directo a Nivel fallido);
   - «Mientras esperas: Jugar el Reto del río», que no gasta canoas;
   - «Volver al Mapa del río»;
   - Atrás vibra (`haptic_back_blocked`) y no cierra.
10. **Regeneración de canoas:** con 3 de 5 canoas, espera 25 min (o cambia la hora en el bloque 15) y comprueba que sube una. La ventanita del inicio muestra el tiempo.
11. **Reintentar muchas veces el mismo nivel:** en un Difícil (el 46 o el 66), **falla y reintenta 10 veces seguidas**.
    - Desde el fallo que diga 03 §13, «El río bajó» trae una variante aliviada (`relief`), más fácil y con el mismo aspecto.
    - Tras 5 fallos, Rosa y el Salto del Boto.
    - Las canoas bajan una por fallo.
    - Nada se acumula mal: los contadores, la memoria y el tiempo de carga siguen igual al décimo intento que al primero.
12. **Salir del nivel:** desde la pausa, «Salir» en el 35. Sale ¿Salir? y, al confirmar, desde el 31 cuenta como fallo y cuesta una canoa. Antes del 31 no cuesta nada.
13. **La Crecida al perder:**
    - con racha y Hoja escudo, el primer fallo lo cubre el escudo (toast) y la racha sigue;
    - el segundo fallo la rompe;
    - un rescate comprado protege la racha (04 §8).

## 8. Economía, Mercado y Cántaro (≈ 1 h 30 min)

1. **Saldo:**
   - cada ganancia y cada gasto de semillas sale en el HUD y en `earn_virtual_currency` o `spend_virtual_currency`, con su cantidad;
   - premios de victoria: 20 en Normal, 60 en Difícil y 80 en Muy difícil (`eco_reward_*` de 08 §9), dobles con el río crecido;
   - el saldo nunca baja de 0 ni cambia al reabrir la app.
2. **Mercado sin tienda (nuevo, `6914b06f`):** con el build normal, con Firebase y Play Billing reales, la app no está en Play Console.
   - El icono del Mercado y el Cántaro **ocultos** en el inicio.
   - Si alguna pantalla lleva a una compra (rescate sin saldo, Sin canoas, Mochila de inicio), sale el aviso de «tienda no disponible», no un toque mudo como el 23.
3. **Mercado con la tienda falsa**, con `--ez af_fake_store true`:
   - las pestañas Semillas, Comodines, Cuias (skins) y Sin anuncios, con los precios de la tienda falsa;
   - comprar el Puñado: `iap_verification` `verifying` → `credited` y +1.000 una sola vez;
   - comprar un comodín y una skin, y ponerse la skin;
   - **Sin anuncios:** después no salen intersticiales, pero los vídeos con premio siguen;
   - reabrir la app: `ledger_restore` sin abonos repetidos;
   - al 130 % en es, en y pt-BR: las pestañas en dos líneas, las filas que crecen y el desplazamiento.
4. **Cántaro** (con la tienda falsa):
   - visible desde el 20;
   - se llena con las victorias;
   - con ≥ 1.000, tocar el icono abre la oferta;
   - **lleno (3.000):** sale una tarjeta, una sola vez. Para llegar rápido, gana muchos niveles con el jugador automático o pide al escritorio un extra si no hay forma.
5. **Banda 13-17:**
   - cada compra pide «¿Confirmas la compra?» con el precio;
   - en Privacidad no está el interruptor de anuncios.
6. **Tarjetas de oferta:**
   - Sin anuncios tras el tercer intersticial;
   - la skin del mundo tras la primera victoria del mundo 3.

   Como mucho **1 oferta por sesión** (`ui_max_takeovers_per_session`). Ninguna oferta antes del 10 ni en la primera sesión.

## 9. Anuncios simulados (≈ 45 min)

1. **Vídeos con premio**, en sus cinco sitios: Cosecha doble, recarga de Marea, Morpho, Luciérnaga y Canoa extra.
   - Cada uno: `af_ad_request` `filled`, `af_ad_shown` y `af_ad_reward`, y el premio una sola vez.
   - Cerrar el vídeo antes de tiempo: sin premio (si el simulado lo permite).
   - Con el teléfono en silencio: el juego se calla durante el anuncio y vuelve después.
   - Zona segura del anuncio (nuevo): el rótulo y «Cerrar» no quedan bajo la barra de gestos ni bajo la cámara.
2. **Intersticiales:**
   - la cadencia del bloque 2.3;
   - el tope de 6 por sesión;
   - nunca en el Reto del río, en un reintento, tras rendirse ni en Sin canoas.
3. **Topes:** el global de 15 vídeos al día. Al llegar, los botones de vídeo desaparecen o se apagan con un motivo.
4. **Sin relleno:** con `--ei af_ads_fill 0`, sin botones de vídeo ni pantallas vacías.

## 10. Bitácora, Canastas, Crecida y Dato del río (≈ 1 h)

1. **Bitácora:**
   - la página de hoy se sella una vez al día con un premio a elegir entre tres;
   - las casillas especiales: Cuia extra en el día 6, Morpho en el 4 y Luciérnaga en el 29 (05 §2.3);
   - la página del día siguiente, cambiando la fecha (bloque 15);
   - al 130 % y en los tres idiomas.
2. **Canastas:** en el 10 (50), el 30 y el 60 (Morpho), el 40 y el 90 (Cuia extra) y el 100 (la grande). Animación, premio y saldo.
3. **Crecida:**
   - la barra y sus hitos;
   - el escudo;
   - el río crecido tras el 10 (¿se ve en el fondo?);
   - la racha rota al perder (bloque 7.13).
4. **Dato del río:**
   - una sola línea por victoria;
   - Ajustes › «Datos del río» muestra el dato con su fuente;
   - Ajustes › «Sobre la Amazonía» (`settings_about_amazon`).

## 11. Mapa del río, frutas, banners y skins (≈ 1 h 30 min)

1. **Mapa:**
   - las vistas Mundo y Cuenca, con «Norte arriba»;
   - la canoa de Capi con Rosa delante;
   - la ventanita de casa de TiTino;
   - la cabecera con la pregunta del mundo, en los tres idiomas y sin textos vacíos (observación 1 del 23);
   - «Jugar · Nivel N» siempre a la vista;
   - el Muestrario de frutas;
   - las placas de la Cuenca **al 130 % en inglés**, que se montaban (observación 2 del 23);
   - Atrás cierra el Mapa.
2. **Con `af_max_level`** en 25, 58, 140, 200, 400 y 700: el Mapa se pone al día, con el viaje animado (y sin él con Reducir movimiento: `by: reduce`).
3. **Banners de mundo** en el 11, el 41, el 81 y el 141, y en el 201, el 301, el 421 y el 561 si existen: su arte, el texto y que salen solo en el primer intento.
4. **Skins de los hitos** al ganar el 40, el 80 y el 140: la tarjeta, la skin puesta en el tablero y en el Mercado.
5. **Frutas del río:** la tarjeta «Fruta nueva» la primera vez que sale cada fruta (`af_fruit_discovered`) y su línea en la victoria.
6. **Fondos:**
   - en 1-200, el de la parada;
   - en 201-700, el kit del mundo con el rótulo;
   - ninguno rosa, negro o vacío.

   Una captura de un nivel de cada mundo, del 1 al 9.

## 12. Reto del río y Liga del río (≈ 2 h)

1. **Reto del río:**
   - se desbloquea en el 10;
   - la tarjeta del inicio: Jugar, Pendiente de validar y Seguir;
   - el tablero del día;
   - el rótulo «Nivel N · intento k»;
   - ganarlo, que sella el calendario;
   - perderlo y reintentar, sin gastar canoas;
   - Atrás dentro del Reto;
   - irse a segundo plano en mitad y volver;
   - **el calendario de sellos** y la zona segura del calendario (nuevo);
   - la racha de 7 Retos seguidos regala una Pororoca (cambiando la fecha, en el bloque 15);
   - **a 320 dp** (bloque 16): la tarjeta de 272 dp que no pisa la Crecida.
2. **Liga del río:** sigue **entero** `guion-prueba-telefono-liga.md` (primer contacto, una victoria, sin conexión, con 13-17 y con <13), contra el backend de dev. Además:
   - con `--ez af_league true --ez af_league_fake true`: la lista de 100, el país, el percentil y que el botón «Liga» casa con la columna de audio del inicio sin solaparse;
   - la Liga al 130 % y en los tres idiomas;
   - las banderas;
   - TalkBack sobre la lista (bloque 17).

## 13. Ajustes, privacidad y cuenta (≈ 1 h)

Recorre **cada fila** de Ajustes y comprueba que hace lo que dice, al momento y tras reabrir la app:
- Sonido, Música y Vibración;
- Idioma: Automático, Español, English, Português y en-XA;
- Patrones;
- Pista por inactividad;
- Mensajes de TiTino (`settings_sloth_messages`);
- Reducir movimiento;
- Alto contraste;
- Avisos (encendido, apagado y bloqueado desde el teléfono, con «Abrir ajustes del teléfono»);
- Privacidad y datos;
- Corregir mi edad;
- Datos del río y Sobre la Amazonía;
- Código de cuenta (en grupos con guiones);
- Eliminar cuenta y datos.

**Además:**
1. **Privacidad:** apagar la analítica hace que las líneas pasen a `AF_EVENT_UNSENT`. Encenderla las devuelve a `AF_EVENT`.
2. **Corregir mi edad:** a 13-17, una sola vez; después desaparece.
3. **Idiomas:** cambia a cada uno y recorre el inicio, un nivel, la victoria, el Mapa, la Bitácora, el Reto, la Liga, el Mercado (con la tienda falsa) y Ajustes. **Ningún texto vacío, cortado o en otro idioma.** Con en-XA, todo entre corchetes y nada recortado.
4. **Eliminar cuenta: se hace al final de la prueba, en el bloque 21**, no aquí. Aquí solo se abre, se ve la espera visible y la nota de Play Juegos, y se cancela.

## 14. Notificaciones (≈ 45 min, más esperas)

1. Con `--ei af_dev_push 2`: el primer aviso llega a los 60 s de pasar a segundo plano, con su texto y su icono.
   - Tocarlo abre el juego en el sitio correcto.
   - En los tres idiomas.
2. **Avisos reales sin el extra:** deja la app cerrada con el Reto sin jugar y comprueba al día siguiente si llega «tu Reto del río está listo» (06 §8.3), sin pasar del tope del día.
3. **Con <13:** ningún aviso ni diálogo de permiso.

## 15. Tiempo y fechas (≈ 1 h; el dueño cambia la hora)

Con la hora automática apagada **por el dueño**, y encendida otra vez al terminar:
1. **Día siguiente** (+1 día):
   - Bitácora nueva;
   - Reto nuevo;
   - la racha del Reto sigue;
   - topes de vídeos y de rescate a 300 reiniciados;
   - la Crecida sigue (no caduca de noche).
2. **+2 días sin jugar:** el Regalo de regreso (06 §9).
3. **+7 días seguidos jugando el Reto:** la Pororoca de regalo.
4. **Hora hacia atrás (−1 día):** el anti-reloj de las canoas y de la Bitácora no regala nada ni rompe el perfil (`GuardedClock`).
5. **Hora del día:** TiTino dormido de noche (20-6), con sol entre las 10 y las 16 y el saludo de la mañana entre las 6 y las 12.
6. **Semana de la Liga:** al pasar del lunes a las 12:00 UTC, la semana cambia.

## 16. Pantallas y tamaños (≈ 1 h 30 min; el dueño autoriza `wm`)

Con `adb shell wm size` y `wm density`, anotando los valores de antes y **reseteándolos al final**:
1. **16:9:** `wm size 1080x1920`.
   - El inicio, los niveles de 3 filas (el 240 y el 335, sin franja y con la tropa lateral), el 640 y su panel;
   - Aguas estancadas (el 31 y el 640, al 130 %);
   - la hoja de compra, el calendario y el anuncio.
2. **320 dp de ancho:** `wm density` hasta que `AF_LAYOUT` dé ~320 dp.
   - El inicio con la tarjeta del Reto de 272 dp y la Crecida al lado;
   - la píldora en dos líneas al 130 %;
   - la columna de audio y la Liga.
3. **Pantalla alta** (360 × 800): el inicio y un nivel de 3 filas.
4. **Navegación con 3 botones** en lugar de gestos: ningún botón bajo la barra.
5. **Rotación:** girar el teléfono no gira el juego ni lo rompe.
6. **Modo ventana o pantalla dividida**, si One UI lo deja: anota qué pasa.

## 17. Accesibilidad (≈ 1 h 30 min; el dueño enciende TalkBack)

1. **TalkBack:**
   - el inicio, un nivel (cada cuia anuncia sus frutas), la victoria, Ajustes, la Liga y el Mercado;
   - los discos de audio: «Música: activada»…;
   - las listas (Q-B9-17);
   - ¿se puede jugar un nivel fácil solo con TalkBack?
2. **Fuente del sistema al 130 % y al máximo** (Ajustes de Android, no el extra): el juego la toma o no; anótalo. Con `af_font_scale 130`, las pantallas del bloque 13.3 sin cortes.
3. **Reducir movimiento:**
   - la victoria sin confeti ni hojas;
   - el viaje del Mapa saltado;
   - el banner más rápido;
   - el mordisco de la Piraña quieto.
4. **Alto contraste:** el patrón a 0,35 y que se distinguen los pares de riesgo.
5. **Daltonismo:** con el simulador de Android (Opciones de desarrollador › Simular espacio de color), en deuteranopía y protanopía, ¿se distinguen las frutas de un nivel del 41 en adelante, con los patrones? Captura.

## 18. Robustez: interrupciones, segundo plano y red (≈ 2 h)

1. **Segundo plano** (botón de inicio y volver):
   - en mitad de un vertido;
   - durante el sellado;
   - en la victoria;
   - con un anuncio simulado;
   - con la hoja de compra falsa abierta;
   - en Aguas estancadas;
   - en el viaje del Mapa;
   - en la carga.

   Al volver, todo sigue donde estaba, sin premios duplicados ni perdidos.
2. **Matar la app** (`am force-stop`) en esos mismos momentos y reabrir:
   - el nivel en curso se recupera (o se reinicia según el GDD);
   - el saldo es el mismo;
   - `profile_integrity: valid`;
   - en Nivel fallido o Sin canoas, la app vuelve a Nivel fallido o al nivel.
3. **Interrupciones del sistema:**
   - una llamada (el dueño se llama desde otro teléfono);
   - una alarma;
   - una notificación de otra app;
   - bajar la cortina;
   - apagar la pantalla y encenderla.

   El audio se pausa y vuelve.
4. **Modo avión** (el dueño):
   - arranque sin red (`"net":"offline"` en los comunes) y jugar 5 niveles;
   - el Reto sin red;
   - la Liga sin red (bloque 12);
   - el Mercado con la tienda falsa sin red;
   - volver la red: `restoreLedger`, la cola de la Liga y Remote Config.
5. **Red mala:** con el Wi-Fi a medias, si se puede, que ninguna pantalla se quede esperando sin salida.
6. **Ahorro de energía y batería baja** (el dueño activa el modo ahorro): el juego sigue fluido y sin cortes de audio.
7. **Atrás de Android en cada pantalla:**
   - inicio (¿sale de la app?, ¿pide confirmación?);
   - nivel (abre la pausa);
   - cada modal;
   - Mapa, Reto, Liga, Ajustes y Mercado;
   - pantallas de edad y consentimiento (no se saltan).

   Doble Atrás rápido: sin excepciones.
8. **Actualizar encima:** instala el `dev.apk` del 23 (si lo conservas, o compílalo en `a656cca4`), juega 20 niveles, instala el de hoy con `adb install -r` **sin** `pm clear` y comprueba que el progreso, el saldo y los ajustes siguen. Si no tienes el viejo, anótalo como sin probar.

## 19. Rendimiento, memoria, batería y temperatura (≈ 1 h 30 min, casi sin vigilancia)

0. **Desglose de memoria (lo pide el análisis del escritorio de la jornada 1, observación 8):** en los niveles 75 y 200 de la sesión larga, guarda **entero** `adb shell dumpsys meminfo -d com.amazonfill.dev` (no solo el TOTAL PSS) en `Logs/device/completa/b19/meminfo-<nivel>-<variante>.txt`. Anota en la hoja Java Heap, Native Heap, Code, Graphics, Private Other y System, y del detalle `.so mmap`, `.dex mmap`, `GL mtrack`, `EGL mtrack` y `Unknown`. Repítelo en tres variantes para aislar las causas:
   - el `dev.apk` con 18+ (anuncios de prueba de Google, si ya lleva AdMob);
   - el mismo `dev.apk` con un perfil <13 (ningún SDK de anuncios arranca);
   - el `rehearsal.apk` (sin modo Development) con 18+.
   Referencia: 08 §5.4 fija RSS ≤ 400 MB, texturas ≤ 120 MB y heap gestionado ≤ 60 MB.
1. **Sesión larga:** 60 min seguidos de juego (jugador automático más ratos a mano). Cada 15 min, anota:
   - `dumpsys meminfo` (`TOTAL PSS`);
   - `dumpsys battery` (nivel y temperatura);
   - `dumpsys gfxinfo com.amazonfill.dev` (fotogramas lentos, si da datos con Unity).
2. **`AF_PERF`:** `save_ms` y `bg_ms` de los vertidos (objetivo de `prototipo-v0.md`) y `world_banner_art`.
3. **Morpho en el teléfono:** `--ei af_bench_morpho 1 --ei af_level 106` y anota las líneas `AF_PERF morpho` (presupuesto: 150 ms).
4. **Arranque:** en frío y en caliente (`am start -W`), tres veces cada uno.
5. **Tamaño en el teléfono:** Información de la app › Almacenamiento (app, datos y caché) al principio y al final de la prueba.
6. **Sensación:** el dueño dice si en algún momento se calentó, se trabó o fue lento.

## 20. Build de ensayo de release (≈ 45 min)

1. `adb uninstall com.amazonfill.dev` (antes, apunta tu progreso: se pierde) e instala el APK del ensayo.
2. **Comprueba:**
   - no hay líneas `AF_LAYOUT`, `AF_STATE`, `AF_EVENT` ni `AF_PERF`;
   - los extras de desarrollo **no hacen nada**: `--ei af_level 300`, `--ez af_fake_store true` y `--ei af_seeds 99999` se ignoran;
   - el nombre de la app y el icono;
   - la edad y el consentimiento;
   - los niveles 1-12 a mano, sin excepciones;
   - Firebase en logcat sin errores, si el ensayo lleva `google-services.json`.
3. Vuelve a instalar el `dev.apk` para el bloque 21.

## 21. Cierre: eliminar la cuenta de verdad (≈ 20 min)

Con el perfil de dev de toda la prueba y el backend de dev (`google-services.json`):
1. Ajustes › «Eliminar cuenta y datos» › paso 1 › paso 2.
   - La nota de Play Juegos.
   - **La espera visible** mientras borra (arreglo de `tanda5/borrado-espera`).
   - `af_account_delete` `requested` → `confirmed` → `done`.
   - El juego vuelve a la primera ejecución, con la pantalla de edad.
2. Tras el borrado: la Liga ya no te lista (tu seudónimo desaparece de la pestaña Global tras recargar) y el saldo vuelve a 0.
3. Comprueba que el logcat de toda la prueba no lleva secretos (regla de arriba).

## 22. Gráficos e imágenes: revisión del dueño (≈ 1 h, se puede repartir por bloques)

Con las capturas de toda la prueba y el teléfono en la mano, el dueño marca lo que se ve mal. La guía es la dirección visual «Selva brillante premium» (D-003, `docs/art/direccion-visual-v2.md`):
- **Vidrio de las cuias:** el líquido con su brillo, las dosis separadas y el chorro que cae en el eje del destino (D-006).
- **Sellado:** la tapa o el efecto, y el confeti de la victoria.
- **TiTino:** sus poses, que no tape textos y que no salga en tableros de 2 o más filas cuando no cabe.
- **La tropa de miquitos:** su entrada y su franja.
- **Fondos de cada parada y cada mundo:** brillos y polen del río, noche y lluvia.
- **Tarjetas, banners, skins y el icono v2:** el inicio y el cajón de apps, Recientes y el icono temático monocromo.
- **Textos:** tamaño mínimo, contraste y sin cortes.
- **Nada provisional que no deba estarlo:** rectángulos sin arte, colores planos de relleno o textos en inglés de desarrollo.

**Cada cosa que no guste** va como observación con su captura, aunque sea de gusto: así se decide después.

## 23. Audio: escucha del dueño (≈ 45 min)

Con auriculares y sin ellos. La lista de qué escuchar está en `prototipo-v0.md`, «Qué escuchar (AU3)»:
- el agua de cada mundo y la lluvia ligera, media y fuerte;
- un animal cada 20-45 s;
- en `amb_w01_fauna_02` (niveles 1-10) no se entiende ninguna voz;
- el lecho de música con sus capas al sellar, sin desfasarse tras varios bucles;
- la variante nocturna en el 60, el 80 y el 100;
- el chorro que sube de tono;
- la nota de cada fruta al elegirla;
- el tema del menú;
- el volumen relativo: ningún efecto molesta ni tapa la música;
- en los mundos 10-13, el audio por código, que suena peor por diseño;
- en logcat, todas las líneas `sfx` con `"src":"clip"` en 1-700: `grep synth` debería salir vacío en los mundos 1-9.

## Qué devolver

1. **La hoja** `pruebas-telefono-<fecha>-completa.md` con:
   - la verificación del bloque 0;
   - una tabla resumen: bloque, cumple, no cumple o sin probar, y el número de observaciones;
   - el detalle por bloque;
   - las observaciones numeradas con su gravedad.
2. **Commit y push** de la hoja al final de cada jornada, en `integracion-fase1-fase2`, con el mensaje «Añade la prueba completa en el teléfono (jornada N)». Solo la hoja, nada más.
3. **Al terminar**, un resumen de las 10 observaciones más graves arriba de la hoja.

Con la regla vigente (correcciones en la misma ola), el escritorio lanza los arreglos en cuanto llega cada jornada, sin esperar al final.
