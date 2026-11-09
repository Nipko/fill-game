# Entrega de audio 1.1 (`spark/audio-1-1`)

**Rama de entrega:** `spark/audio-1-1` (creada desde `integracion-fase1-fase2`, `8d06aeb3`)  
**Encargos cubiertos:** `docs/audio/encargo-audios-1-1.md` y `docs/audio/encargo-fanfarria-liga.md`  
**Resultado de `check_audio.py`:**

```text
Comprobados 176 de 176 archivos: 176 sin fallos; 0 fallos y 0 avisos en total.
```

---

## 1. Resumen técnico y medidas verificadas (`check_audio.py`)

Los 3 archivos WAV están masterizados a **48.000 Hz, 16-bit PCM**, sin desplazamiento de continua (`DC offset <= 0.000002`), con ataque limpio dentro de los primeros `5 ms` y cola final en silencio absoluto (`-240,0 dBFS` en los últimos `5 ms`), alineados con las otras 173 piezas del catálogo 1.0 tras la conversión a 16 bits.

| ID | Ruta en `Assets/_Project/Audio/` | Formato | Duración (`s`) | Pico (`dBFS`) | Energía `> 300 Hz` | Silencio inicial (`ms`) | Cola final `5 ms` (`dBFS`) | Licencia |
|---|---|---|---|---|---|---|---|---|
| `sfx_mech_aguape` | `sfx/sfx_mech_aguape.wav` | 48 kHz · 16-bit · Mono (`1 ch`) | `0,600 s` (objetivo `0,60 s`, máx. `1,00 s`) | `-20,00 dBFS` (`-20,0 ± 1,5`) | `94,5 %` (mín. `50 %`) | `1,40 ms` (máx. `5 ms`) | `-240,0 dBFS` | `cc0` |
| `sfx_mech_lancha` | `sfx/sfx_mech_lancha.wav` | 48 kHz · 16-bit · Mono (`1 ch`) | `0,900 s` (objetivo `0,90 s`, máx. `1,30 s`) | `-19,00 dBFS` (`-19,0 ± 1,5`) | `90,2 %` (mín. `50 %`) | `3,62 ms` (máx. `5 ms`) | `-240,0 dBFS` | `cc0` |
| `sting_league_debut` | `music/stingers/sting_league_debut.wav` | 48 kHz · 16-bit · Estéreo (`2 ch`) | `2,550 s` (rango `2,00–3,00 s`) | `-16,00 dBFS` (`-16,0 ± 2,0`) | `99,1 %` (mín. `50 %`) | `2,17 ms` (máx. `5 ms`) | `-240,0 dBFS` | `cc0` |

---

## 2. Composición acústica y fuentes por pieza

### 2.1. `sfx_mech_aguape` (`sfx/sfx_mech_aguape.wav`)
- **Función en el juego:** despeje de la mata de aguapé (camalote / jacinto de agua) al llenar una fila o columna adyacente (`MechanicEvents.Cleared(Aguape)` -> `AudioManager.PlayAguapeClear`).
- **Diseño sonoro (3 fases orgánicas en `0,60 s`, cero hielo/cristal):**
  1. **Roce húmedo de hojas carnosas apartándose (`0,00–0,28 s`):** follaje húmedo de bosque lluvioso (`Walk_in_the_rainforest.ogg`, filtrado paso-banda `380–4.600 Hz`) articulado con un roce corto de semillas húmedas (`cabasa2.wav` y `shaker2.wav` de VSCO-2-CE).
  2. **Agua que se abre con un «shhh-plash» suave (`0,05–0,46 s`):** chapoteo real en superficie de río (`Ducks_landing_in_water.ogg` y `433589_stream-river-water.wav`, filtrado `320–4.200 Hz`) sin transientes vítreos ni crujidos secos.
  3. **Burbuja única, cálida y redonda (`0,16–0,44 s`):** burbuja grave-media extraída de `Water_bubbles_chortling.ogg` y `Pouring_water_bottles.ogg` (`260–1.900 Hz`), reforzada con el cuerpo resonante de madera húmeda de marimba baja (`C3_mp.wav`) y tambor de hendidura (`slitdrum0_mp.wav`) para dar calidez táctil en altavoz de teléfono.
- **Licencia y atribución (`audio_manifest.json`):** `cc0` — grabaciones de campo en Dominio Público / CC0 de Wikimedia Commons (`Walk_in_the_rainforest.ogg`, `Ducks_landing_in_water.ogg`, `Water_bubbles_chortling.ogg`, `Pouring_water_bottles.ogg`, `433589_stream-river-water.wav`) y muestras acústicas CC0 de VSCO-2-CE (`cabasa2.wav`, `shaker2.wav`, `slitdrum0_mp.wav`, `Marimba/C3_mp.wav`).

### 2.2. `sfx_mech_lancha` (`sfx/sfx_mech_lancha.wav`)
- **Función en el juego:** entrega del pedido de la lancha (cajón de mercado ribereño junto al tablero) y salida con su carga (`MechanicEvents.Cleared(Lancha)` -> `AudioManager.PlayLanchaDeliver`).
- **Diseño sonoro (3 fases ribereñas en `0,90 s`, diferenciado del golpe de remo de Capi):**
  1. **Llegada de motor fueraborda pequeño desacelerando (`0,00–0,34 s`):** cuatro pistonadas cortas en desaceleración (`puc-puc` a `0,00 s`, `0,075 s`, `0,160 s` y `0,255 s`) construidas con percusión orgánica apagada (`slitdrum1_mp.wav` de VSCO-2-CE + `curimbo_mut_lo_mp.wav` y `tumba_tap.wav` de FreePats CC0) sobre turbulencia de agua de río (`Shallow_small_river.ogg`).
  2. **Toque sordo de casco de madera contra el muelle (`0,31–0,55 s`):** golpe seco y maderoso de tablón hueco (`logdrum_lo.wav` + `slitdrum0_mp.wav` de VSCO-2-CE + `cajon_soft_1.wav` de FreePats CC0) con un brevísimo roce de amarra de madera (`guiro_short.wav`).
  3. **Estela de agua que lame la orilla al partir (`0,36–0,88 s`):** ola corta de orilla y chapoteo de estela (`On_a_pebble_beach.ogg`, `Ocean_Waves_on_a_Tropical_Beach.ogg` y `Ducks_landing_in_water.ogg`, filtrado `320–4.200 Hz`) que se desvanece suavemente.
- **Licencia y atribución (`audio_manifest.json`):** `cc0` — grabaciones de campo en Dominio Público / CC0 de Wikimedia Commons (`Shallow_small_river.ogg`, `On_a_pebble_beach.ogg`, `Ocean_Waves_on_a_Tropical_Beach.ogg`, `Ducks_landing_in_water.ogg`) y muestras CC0 de VSCO-2-CE (`logdrum_lo.wav`, `slitdrum0_mp.wav`, `slitdrum1_mp.wav`, `guiro_short.wav`) y FreePats (`curimbo_mut_lo_mp.wav`, `tumba_tap.wav`, `cajon_soft_1.wav`).

### 2.3. `sting_league_debut` (`music/stingers/sting_league_debut.wav`)
- **Función en el juego:** fanfarria corta de bienvenida a la primera liga semanal (`FirstLeagueCelebrationPopup`, disparada una sola vez por instalación tras completar el nivel 15).
- **Diseño musical y sincronía con la animación del popup (`2,55 s`, estéreo):**
  1. **Golpe inicial del emblema (`0,00 s`):** ataque cálido y festivo de maracá (`fp_maraca_1` de FreePats + `maraca1.wav` de VSCO-2-CE) y curimbó / tambor de marco con mano (`curimbo_open_lo_pp` + `frame_hand_mp1` + `slitdrum1_mp`), acompañado por un pulso sutil de semillas (`egg_soft_1`) en `0,26 s`, `0,52 s` y `0,78 s`.
  2. **Frase ascendente luminosa en pentatónica mayor de Do (`0,00–0,95 s`):** progresión ascendente de 6 notas (`C4 -> D4 -> E4 -> G4 -> A4 -> C5`) en guitarra acústica de cuerdas pulsadas (`Guitar-Acoustic` de FreePats CC0) y flauta de madera expresiva (`Flute-exp` de VSCO-2-CE CC0), adornada con notas sueltas a la octava de marimba de chonta (`Marimba` de VSCO-2-CE), arpa (`Harp` de VSCO-2-CE) y gotas de agua afinadas (`Pouring_wine.ogg`).
  3. **Asentamiento del emblema y acorde abierto final (`1,04–2,55 s`):** coincide con el fin del rebote (`EaseOutBack`) del emblema al segundo `~1,05–1,10 s`; abre un acorde luminoso de Do mayor pentatónico con sexta y novena (`C4 - G4 - A4 - D5 - E5 - G5 - C6`) en guitarra, arpa, marimba y flauta con un toque suave de maracá y tambor de marco, que se apaga gradualmente junto con los rayos dorados hasta silencio completo.
- **Restricciones verificadas (07 §9.3 y `encargo-fanfarria-liga.md`):** cero metales militares (trompetas/trombones), cero redoble de tambor o platillos de casino, cero sintetizadores EDM/8-bit, cero voces/gritos, filtrado paso-alto a `95 Hz` y `99,1 %` de energía útil sobre `300 Hz` para reproducción nítida en altavoz de teléfono sin auriculares.
- **Licencia y atribución (`audio_manifest.json`):** `cc0` — muestras instrumentales acústicas CC0 de VSCO-2-CE (`Flute-exp`, `Marimba`, `Harp`, `maraca1`, `slitdrum1_mp`) y FreePats (`Guitar-Acoustic`, `curimbo_open_lo_pp`, `frame_hand_mp1`, `maraca_1`, `egg_soft_1`), más gota de agua en Dominio Público (`Pouring_wine.ogg`, Wikimedia Commons).

---

## 3. Archivos incluidos en esta rama (`spark/audio-1-1`)

1. `Assets/_Project/Audio/sfx/sfx_mech_aguape.wav` *(Git LFS)*
2. `Assets/_Project/Audio/sfx/sfx_mech_lancha.wav` *(Git LFS)*
3. `Assets/_Project/Audio/music/stingers/sting_league_debut.wav` *(Git LFS)*
4. `docs/audio/especificacion-sonidos.json` *(3 fichas nuevas añadidas al final de `sfx` y `music`: 176 entradas en total)*
5. `Assets/_Project/Audio/audio_manifest.json` *(3 filas nuevas de licencia `cc0`: 176 entradas en total)*
6. `docs/audio/entrega-1-1.md` *(este informe)*

---

## 4. Observaciones sobre la ficha técnica

- **Profundidad de bits (`16-bit PCM`):** el documento `encargo-fanfarria-liga.md` menciona en su tabla «24 bits PCM (igual que los demás WAV de `Assets/_Project/Audio/music/stingers/`)», mientras que `docs/audio/especificacion-sonidos.json` (línea 6, tras `to16bit.py` del cierre 1.0) y los 173 WAV actuales del repositorio están en **16 bits PCM (`48.000 Hz`)** y `check_audio.py` exige `16 bits`. Siguiendo la regla de `encargo-audios-1-1.md` («Con el mismo formato que las otras 173 piezas del catálogo») y el validador `check_audio.py`, las 3 piezas se entregaron en `48.000 Hz, 16-bit PCM`, pasando las 176 comprobaciones con `0 fallos y 0 avisos`.
- **Ningún archivo fuera del alcance tocado:** no se modificaron scripts C#, archivos `.meta` de Unity ni ninguno de los 173 WAV existentes.
