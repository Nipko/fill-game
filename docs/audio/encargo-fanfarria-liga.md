# Tintalú: encargo de la fanfarria del estreno de la Liga

2026-11-09 · Encargo breve para el músico (o el agente) que produzca el audio. Lo decide el dueño: Q-LD-3 del estreno de la Liga (añadido de D-046 del 2026-11-06, `docs/dev/decisiones.md`), respondida con «Encargar audio propio». **El encargo lo hace el dueño.** Hasta que llegue y pase el comprobador, el estreno sigue con la fanfarria de ahora, hecha con efectos que ya existen: `sfx_streak_up` y `sfx_confetti` (Sonido) y el remate `sting_arrival` (Música).

> **Encargado a Gemini (2026-11-09):** el dueño lo pasa al agente de Gemini del equipo Spark con los otros dos audios nuevos de la 1.1 («encarga a gemini los audios nuevos»). El encargo conjunto, con la ficha de esta pieza para el comprobador, está en `docs/audio/encargo-audios-1-1.md`; la entrega llega en la rama `spark/audio-1-1`.

Manda todo lo técnico de `docs/audio/encargo-sonidos.md` (§4 formato, §5 reglas para el teléfono, §8 lo prohibido en la música y §9 licencias y manifiesto) y 07 §9.5 del GDD. Una entrega solo se acepta si pasa `tools/audio-check/check_audio.py` sin ningún FALLO y la escucha del dueño en el teléfono.

## 1. Qué es

Una pantalla completa que sale **una sola vez por jugador**, al desbloquear la Liga del río (tras ganar el 41) o al llegar la 1.1: un velo índigo, rayos de oro que giran despacio, el emblema de la copa que entra con escala y rebote, y una lluvia ligera de hojas. Dice «¡Llegó la Liga del río!» y ofrece «Ver la Liga» o «Luego». El emblema se asienta hacia los 1,1 s y los botones responden desde los 0,7 s.

## 2. La pieza

| Campo | Valor |
| --- | --- |
| Archivo [P] | `music/stingers/sting_league_debut.wav` (relativo a `Assets/_Project/Audio/`), un remate más junto a los 5 de `music/stingers/` |
| Duración | 2,0-3,0 s [P]: más largo que un remate (1-2,5 s) para cubrir la entrada del emblema, y nunca más que la toada de victoria (2-3 s, 07 §9.2) |
| Pico | −16 dBFS ± 2 dB, como los remates (`especificacion-sonidos.json`, `sting_arrival`) |
| Formato | WAV PCM, 48.000 Hz, 16 bits; estéreo permitido (música) |
| Inicio y final | Empieza en los primeros 10 ms, sin clic; termina en silencio (fundido natural, nunca un corte) |
| Energía | Al menos la mitad por encima de 300 Hz y paso alto a 80-100 Hz (el altavoz del teléfono) |
| Licencia | Original, con cesión total de derechos: buyout mundial y perpetuo, que incluye anuncios y tráilers (07 §9.5). Su fila en `audio_manifest.json` |

**Carácter:** una celebración corta y luminosa, de «llegó algo nuevo y grande», en la dirección «Selva brillante premium» (D-003): cálida, limpia y con espacio. Un golpe inicial con el emblema (maracá y curimbó o tambor de marco), una frase ascendente breve de flauta de madera o guitarra acústica con un brillo de gotas afinadas o marimba en notas sueltas, y un acorde final abierto que se apaga con los rayos. Alegre, nunca estridente ni de tragaperras: sin metales de fanfarria militar, sin redoble de casino, sin «¡ganaste!». Sin voces. Lo prohibido de 07 §9.3: ícaros, flautas de Yuruparí, cantos rituales, quena o zampoña, y melodías o colores de Garantido o Caprichoso.

## 3. Dónde se conecta (lo hacemos nosotros)

- **Gancho:** `GameBootstrap.ShowLeagueDebut` (`Assets/_Project/Scripts/UI/GameBootstrap.LeagueDebut.cs`), que hoy toca `AudioCues.Play(SfxId.StreakUp)`, `AudioCues.Play(SfxId.Confetti)` y `AudioCues.Stinger(StingerId.Arrival)`.
- **Al integrarla** [P]: un `StingerId` nuevo en `Assets/_Project/Scripts/Audio/AudioClipIds.cs`, su ficha en `docs/audio/especificacion-sonidos.json` (con la que mide el comprobador) y su clip en la pasada de `AudioAssetSetup.Apply`; el estreno toca ese remate en lugar de `sting_arrival` y `sfx_streak_up`. Como los remates van con el ajuste **Música**, `sfx_confetti` se queda con **Sonido**, para que quien tiene la música apagada oiga algo.
- **Tests:** los de `LeagueDebutTests` que comprueban la fanfarria (`FakeAudioService`) cambian con el gancho.
