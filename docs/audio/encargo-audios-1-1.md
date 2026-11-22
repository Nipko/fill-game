# Tintalú: encargo de los audios nuevos de la 1.1

2026-11-09 · Encargo para el agente de Gemini del equipo Spark, el mismo que hizo la entrega 1 (`ac837b0`, 173 WAV). Lo pide el dueño en el chat: «encarga a gemini los audios nuevos». Son las **tres piezas** que la 1.1 estrena y que hoy suenan con un efecto prestado:

| Pieza | Cuándo suena | Hoy suena | Ficha |
| --- | --- | --- | --- |
| `sfx_mech_aguape` | Se suelta el Aguapé: la mata deja libres sus cuias | `sfx_mech_remolino` (el Remolino) | §2.1 |
| `sfx_mech_lancha` | Atraca la Lancha con su fruta | `sfx_capi_paddle` (el remo de Capi) | §2.2 |
| `sting_league_debut` | Estreno de la Liga del río, una vez por jugador | `sfx_streak_up`, `sfx_confetti` y `sting_arrival` | §2.3 |

La Palizada y la Cocha **no** entran: pasan a la 1.2 (D-068, P9 B). La tripulación del Mapa (tucán y tucuxi) no lleva sonido en la 1.1.

**Lo que manda:**
- lo técnico de `docs/audio/encargo-sonidos.md`: §4 formato, §5 reglas para el teléfono, §8 lo prohibido en la música y §9 licencias y manifiesto;
- el GDD, 07 §9 (audio) y 07 §9.3 (lo prohibido);
- para la fanfarria, todo `docs/audio/encargo-fanfarria-liga.md`.

Si este texto y el GDD no coinciden, manda el GDD.

## 1. Cómo se juega cada momento

- **El Aguapé** (02 §9.1 M8; D-068; debut en el nivel 303). Es una mata flotante de jacinto de agua que sujeta un grupo de 2 a 5 cuias contiguas: reciben agua, pero no la dan, hasta que una del grupo se sella. Entonces la mata se aparta y el grupo queda libre. El GDD lo llama «agua que se abre». La variante «Aguapé espeso» pide dos sellos y suena igual cuando se suelta. En los textos es «aguapé» o «mata», **nunca hielo ni escarcha**: nada de crujidos de hielo ni brillos de cristal helado.
- **La Lancha** (02 §9.1 M11; D-068; debut en el 363). Es una lancha pequeña de madera con motor de popa, que trae de 1 a 3 dosis de fruta. **Atraca** cuando el tablero tiene k cuias vacías. No es una canoa a remo, y en el juego la Canoa es la vida. Tampoco es el barco de recreio de dos pisos. Suena una vez por atraque, y tiene que distinguirse del remo de Capi, que suena en la cuia extra y en la de rescate.
- **El estreno de la Liga.** Lo describe `encargo-fanfarria-liga.md` §1: una pantalla completa que sale una sola vez, con rayos de oro, el emblema de la copa que entra con rebote y una lluvia de hojas.

## 2. Las fichas

Copia estas tres fichas tal cual en `docs/audio/especificacion-sonidos.json`: las dos primeras al final de la lista `sfx` y la tercera al final de la lista `music`, detrás de los remates. El comprobador mide cada pieza contra su ficha. Si crees que algún número no sirve, no lo cambies: dilo en el resumen (§4).

### 2.1 `sfx_mech_aguape`

```json
{"id": "sfx_mech_aguape", "file": "sfx/sfx_mech_aguape.wav", "category": "mecanicas_personajes", "when": "Se suelta el Aguapé: la mata deja libres sus cuias", "gdd": "Agua que se abre (02 §9.1 M8, regla 9)", "duration_text": "0,6 s", "direction": "**Roce húmedo de hojas flotantes de aguapé que se apartan + agua que se abre con un «shhh-plash» suave y una burbuja redonda** al quedar libre el grupo. Nunca hielo, escarcha ni cristal que se rompe.", "hook_today": "Paso «se suelta» del Aguapé (Loosen); hoy reutiliza sfx_mech_remolino", "peak_dbfs": -20.0, "peak_tolerance_db": 1.5, "gesture": false, "canopy_tail": false, "fruit_tuned": false, "max_total_s": 1.0, "min_energy_above_300hz": 0.5, "mono": true}
```

### 2.2 `sfx_mech_lancha`

```json
{"id": "sfx_mech_lancha", "file": "sfx/sfx_mech_lancha.wav", "category": "mecanicas_personajes", "when": "Atraca la Lancha con su fruta", "gdd": "La lancha atraca (02 §9.1 M11)", "duration_text": "0,9 s", "direction": "**Motor de popa pequeño que llega y baja de vueltas («puc-puc» breve y lejano) + golpe sordo de madera contra el muelle + chapoteo de la ola que llega a la orilla.** Cálido y cercano; nunca remo, que es Capi.", "hook_today": "Paso «atraca» de la Lancha (Dock); hoy reutiliza sfx_capi_paddle", "peak_dbfs": -19.0, "peak_tolerance_db": 1.5, "gesture": false, "canopy_tail": false, "fruit_tuned": false, "max_total_s": 1.3, "min_energy_above_300hz": 0.5, "mono": true}
```

### 2.3 `sting_league_debut`

```json
{"id": "sting_league_debut", "file": "music/stingers/sting_league_debut.wav", "when": "Estreno de la Liga del río (una vez por jugador)", "loop": false, "min_s": 2.0, "max_s": 3.0, "peak_dbfs": -16.0, "peak_tolerance_db": 2.0, "stereo_allowed": true}
```

El carácter y la instrumentación están en `encargo-fanfarria-liga.md` §2. Resumen: una celebración corta y luminosa, con un golpe de maracá y curimbó o tambor de marco, una frase ascendente de flauta de madera o guitarra acústica, un brillo de gotas afinadas o de marimba, y un acorde abierto que se apaga. Sin metales militares, sin redoble de casino y sin voces.

## 3. Reglas

- **Formato:** WAV PCM a 48.000 Hz y 16 bits, como la entrega 1 tras `to16bit.py`. Los efectos, en mono; el remate puede ser estéreo.
- **Inicio y final:** suena desde los primeros 10 ms, sin clic, y termina con un fundido, nunca con un corte.
- **Energía:** al menos la mitad por encima de 300 Hz, con paso alto a 80-100 Hz, porque se oye por el altavoz del teléfono.
- **Licencias:** las de `encargo-sonidos.md` §9, con una fila por pieza en `Assets/_Project/Audio/audio_manifest.json`, con el formato de la entrega 1.
  - Con grabaciones CC0 o de dominio público, cita su URL.
  - Si generas con IA (por ejemplo, Lyria), usa `ia-uso-comercial`, con el nombre y la versión de la herramienta en `source`, y solo si sus términos permiten el uso comercial sin reclamar derechos sobre la salida.
  - Prohibido samplear música existente.
- **Lo prohibido de 07 §9.3:** ícaros, flautas de Yuruparí, cantos rituales, quena o zampoña, y melodías o colores de Garantido o Caprichoso.
- **No toques** el código C#, los `.meta` de Unity (los crea Unity al importar), los otros 173 WAV ni las fichas que ya existen.
- **Conectar las piezas lo hace el integrador:**
  - `SfxId` y `StingerId` en `AudioClipIds.cs`;
  - `AudioBoard.Step`, para el Aguapé (Loosen) y la Lancha (Dock);
  - `GameBootstrap.ShowLeagueDebut`;
  - `AudioAssetSetup`;
  - los tests.

## 4. Qué entregar

En una rama **`spark/audio-1-1`** creada desde `integracion-fase1-fase2`:

1. `Assets/_Project/Audio/sfx/sfx_mech_aguape.wav`, `Assets/_Project/Audio/sfx/sfx_mech_lancha.wav` y `Assets/_Project/Audio/music/stingers/sting_league_debut.wav`, por Git LFS como los demás WAV;
2. las tres fichas de §2 en `docs/audio/especificacion-sonidos.json`;
3. tres filas nuevas en `Assets/_Project/Audio/audio_manifest.json`;
4. `docs/audio/entrega-1-1.md`: qué usaste para cada pieza, su licencia, lo que mide el comprobador y cualquier duda.

**Antes de entregar,** ejecuta el comprobador:

```
py -3.14 tools/audio-check/check_audio.py --root Assets/_Project/Audio
```

Tiene que dar **176 de 176 sin ningún FALLO**: las 173 de la entrega 1 y estas tres. Los avisos no cuentan, pero anótalos en el resumen.

## 5. Después

- **El integrador** comprueba la rama con el comprobador y revisa las licencias en sus registros públicos. Luego conecta las piezas y pasa las pruebas completas.
- **El dueño** las escucha en el teléfono. Una pieza que no le convenza vuelve a Gemini con su comentario. Mientras tanto, el juego sigue sonando con el efecto prestado.
