# Encargo para Spark: seguir la prueba de la 1.1 (r2b)

2026-11-15 · El dueño, en el chat: «iniciemos pruebas, sube para que spark se encargue». Spark continúa desde donde lo dejó el portátil: la segunda ronda (`pruebas-telefono-2026-11-09-1.1-r2.md`) llegó hasta el tucán. Suma también lo que se integró después y lo que quedó sin probar en el Moto G23 (`pruebas-telefono-2026-11-15-moto-g23.md`).

## 0. Preparación

- Compila `integracion-fase1-fase2` en su último commit, con `pwsh -NoProfile -File scripts/unity-build.ps1 -Method Dev`. El veredicto sale del log (`[AF.BuildDev] OK`), no del código de salida de `unity run`. Instala el APK con `adb install -r`.
- **Teléfono:** el Moto G23 (clase A, 08 §5.4). Si tienes otro, apunta modelo, Android y GPU de cada uno.
- Los extras de desarrollo se pasan como en el guion r2: `adb shell am start -S -n com.amazonfill.dev/com.unity3d.player.UnityPlayerGameActivity --ei af_max_level N --ei af_level M`. Se vuelven a aplicar en cada arranque: relanza sin ellos para ver lo que pasa después.
- Captura cada punto con `adb exec-out screencap -p` y guarda logcat (`AF_EVENT`, `AF_STATE`, `AF_PERF`) en `Logs/device/v11r2b/`, fuera de git.

## 1. Lo que falta del guion r2

Sigue `guion-prueba-telefono-1.1-r2.md`:

- **§1, el viaje de Capi, en el Moto G23.** El del 70 y el del 80, este último cruzando de mundo con el zoom alto. Apunta `map_journey` y `map_play_ready`, y si el viaje va fluido (`AF_PERF frames`). La observación 1 de la r2 ya dice que el del 80 se ve demasiado rápido: no la repitas, solo mide.
- **§2.2-2.6,** la tripulación:
  - tucuxi: la victoria del 51 no trae «Ver mapa» sino la Bitácora, así que abre el Mapa desde el inicio;
  - el miquito, la llegada, el cambio de mundo en el 40 y «Reducir movimiento».
- **§3,** los rótulos, «Faltan N» y el río recorrido.
- **§4,** la memoria del Mapa v2 frente a la 1.0, con sus tres vueltas. En un teléfono de 3,7 GB es la medida que más importa.
- **§5,** el marco de país (Q-M6).
- **§6,** el segundo gesto de los miquitos (F13).
- **§7,** Play Games: solo si la huella SHA-1 de la clave de depuración de Spark está dada de alta. Si no, «sin probar».

## 2. Los tres audios nuevos (Gemini, D-054 y Q-LD-3)

Que el dueño los escuche en el teléfono: es la condición para aceptarlos.

1. **Aguapé** (`sfx_mech_aguape`): `--ei af_max_level 302 --ei af_level 303`. Sella una cuia del grupo del aguapé. Al soltarse tiene que sonar el agua que se abre y la burbuja, no el remolino de antes.
2. **Lancha** (`sfx_mech_lancha`): `--ei af_max_level 362 --ei af_level 363`. Deja las cuias vacías que pide para que atraque. Tiene que sonar el motor pequeño, el golpe contra el muelle y la ola, no el remo de Capi.
3. **Fanfarria de la Liga** (`sting_league_debut`): `pm clear`, `--ei af_max_level 40 --ei af_level 41`. Gana el 41: sale el estreno de la Liga con la fanfarria (unos 2,5 s) y el confeti. Apunta `af_league_debut`.
   - Repítelo con la **Música apagada** en Ajustes: solo suena el confeti.
   - Repítelo con el **Sonido apagado**: solo suena la fanfarria.

Para cada pieza, la impresión del dueño en una frase. Una que no le convenza vuelve a Gemini con su comentario.

## 3. Los textos revisados (revisión 4 de Gemini)

Con `--es af_lang en` y luego `--es af_lang pt-BR`; vuelve con `--es af_lang es-419`.

- **Mapa:**
  - los pueblos nuevos con su nombre (Manacapuru, Codajás, São Paulo de Olivença, Porto Velho…) y su río en la ficha;
  - el rótulo «Premio del mundo» con su «faltan N».
- **Tripulación:** el cartel «Joining the trip! · Toucan» en inglés.
- **Tutoriales:**
  - el del Aguapé, en el 303;
  - el de la Lancha, en el 363. En portugués: «A lancha atraca…».
- **Marcos de país:** las etiquetas de la sección.
- **El estreno de la Liga.**

Apunta cualquier texto cortado, partido a mitad de palabra, en otro idioma o que no se entienda.

## 4. Lo que quedó sin probar en el Moto G23

1. **Memoria con el ensayo de release.** Es la observación 1 del Moto G23: el `dev.apk` pasa los 450 MB.
   - Compila `pwsh -NoProfile -File scripts/unity-build.ps1 -Method Release -Rehearsal`, que deja `com.amazonfill.dev` y es instalable.
   - Repite las medidas: `dumpsys meminfo` en la pantalla de la edad, en el Mapa y en el 340, y `peak_mono_heap_mib`.
   - Con esto se decide D-060.
   - **Desde `5b60e69e`** el arte por código lleva el tope de densidad del teléfono y el inicio suelta su arte mientras se juega (`docs/dev/memoria.md` §9). Sigue la lista de §9.5: ART compilado (`cmd package compile -m speed-profile`), la orilla, el nivel 106, el Mapa, tres vueltas al Mapa con el v2 y con la 1.0, `Code`, `peak_mono_heap_mib`, las líneas `AF_ART home_scene` y `AF_ART procedural`, y el tope que sale en pantalla dividida. Que el dueño mire si el arte a 2 px/dp se ve bien.
2. **La segunda apertura:** cierra y vuelve a abrir. Apunta `boot` `splash_ms` y `load_ms`.
3. **El 340 jugado a mano.** Apunta si el panel de dificultad se cierra bien y los fotogramas.
4. **Los parones del 386** (observación 2): repítelo sin tocar el teléfono y mira si vuelven.

## 5. Qué devolver

En una rama `spark/prueba-1.1-r2b` creada desde `integracion-fase1-fase2`, la hoja `docs/dev/traspaso/pruebas-telefono-2026-11-1X-1.1-r2b.md` (con su fecha), con el formato de las anteriores:
- cumple o no cumple en cada punto, con su captura o su línea de logcat;
- la impresión del dueño sobre los tres audios;
- la tabla de memoria, del dev y del ensayo;
- las observaciones numeradas, con su gravedad.

No pegues tokens, UID ni URL firmadas.

**En paralelo, el escritorio** arregla las observaciones 1 y 2 de la r2, el viaje con zoom alto y los puntos que tapan al tucán, y la 3 del Moto G23, el juego automático que no cierra el panel de dificultad. Esos arreglos van en la ronda siguiente; esta prueba no los espera.
