# La fábrica de contenido de Tintalú (S4 de D-066)

Escrito por Spark el 2026-11-01.
- Encargo: `docs/dev/traspaso/spark-marketing.md` §4 y §5.
- El código está en el repositorio privado `fill-game-marketing`, carpeta `fabrica/` (Q-MK-7), clonado en el Spark en `C:\AI\marketing\fill-game-marketing\`.
- Las tomas, las piezas y los briefs viven fuera de git, en `C:\AI\marketing\`. Al repositorio del juego solo llegan este documento, el kit (`kit/`) y los informes.

## 1. El circuito

```
brief (JSON) ─► tomar.py (teléfono, modo de captura E1) ─► montar.py (plantillas + sonido del juego)
            ─► calidad.py (§5 del encargo) ─► cola.py (aprobación del dueño, PAUSA) ─► publicación (S7)
```

| Paso | Script | Qué hace | Probado |
|---|---|---|---|
| Toma | `tomar.py <nombre> -s <segundos> -- <extras af_capture*>` | <ul><li>Lanza el juego con los extras de E1 y graba la pantalla con `screenrecord`.</li><li>Guarda las líneas `AF_CAPTURE` con su segundo de vídeo, en `<nombre>.json`.</li><li>**No graba si «No molestar» está apagado.**</li></ul> | Sí, en el SM-S938B el 2026-11-01 con un dev.apk con E1 compilado en el Spark: nivel 42 en los tres idiomas, con 13 vertidos y la victoria por toma |
| Montaje | `montar.py <brief.json>` | Encuadre, gancho, subtítulos, tarjeta final y sonido (§3 y §4 abajo), por idioma y formato | Sí: `e1-l42-se-tine` en los 3 idiomas (30,8 s) con los efectos colocados por las líneas `AF_CAPTURE` (13 vertidos y la victoria) |
| Control de calidad | `calidad.py <pieza>` | Las pruebas de §5 abajo; deja `calidad.json` y el estado | Sí: las 3 salidas pasan; además, detectó que pt-BR y en usaban la toma en español (§5) |
| Cola | `cola.py estado / aprobar / rechazar / hoja / publicables` | Estados, aprobación, hoja de contactos local e interruptor de parada | Sí, con autoprueba |

- **Intérprete:** el Python de ComfyUI, que trae Pillow y numpy: `C:\AI\ComfyUI_windows_portable\python_embeded\python.exe`.
- **ffmpeg:** el de `C:\AI\ffmpeg\`.
- **Autopruebas:** cada script tiene `--self-test`, salvo el generador del kit.

### Antes de la primera toma en un teléfono

- **El dev.apk tiene que llevar E1** (`596f6931` o posterior). Se compila con `scripts/unity-build.ps1 -Method Dev`; en el Spark hace falta la licencia de Unity activa.
- **Hay que abrir el juego una vez sin extras `af_capture*`** y responder la edad y el consentimiento. Los responde el dueño, no un agente. El modo de captura lee el perfil pero no lo guarda: con un perfil vacío, cada toma se quedaría en esa pantalla (`tomar.py` lo avisa).
- **«No molestar»**, activo.
- **Sincronía:**
  - `t_video` = la hora de la línea de logcat − (la hora del teléfono al lanzar `screenrecord` + **0,35 s**). Ese 0,35 es lo que tarda en arrancar `screenrecord`: se midió en el SM-S938B con los fotogramas del primer vertido;
  - con otro teléfono, se vuelve a medir y se pasa con `--ajuste`;
  - **el volcado de logcat es al final de la grabación.** Leído en vivo, se perdían las líneas.
- **La toma sigue al menos 9 s después de ganar.** El panel de victoria («¡Perfecto!», la cosecha y el Dato del río) se queda quieto unos 2,6 s después de `autoplay_end`, y debe leerse 4 s o más antes de la tarjeta final. El control de calidad lo exige (`victoria_visible`).
- **El build de Unity reinicia adb**, y la depuración inalámbrica se cae: hay que reactivarla en el teléfono.

## 2. El brief

Un JSON por pieza en `C:\AI\marketing\briefs\`. Ejemplo real (la pieza de prueba, recortado):

```json
{
 "id": "prueba-l42-se-tine",
 "pilar": "satisfying", "gancho_ids": ["G2"], "objetivo": "organico",
 "etiqueta_ia": false, "planos_ia": [],
 "idiomas": ["es-419", "pt-BR", "en"], "formatos": ["9x16"],
 "toma": {"archivo": ".../v_full42_es.mp4", "tamano": [1080, 2340], "desde": 5.3, "hasta": 31.5,
          "velocidad": 1.0, "slowmo": 100, "eventos": null, "nivel": 42},
 "por_idioma": {"pt-BR": {"toma": {"archivo": ".../v_full42_pt.mp4", "desde": 5.2, "hasta": 30.8}}},
 "gancho_texto": {"es-419": "Mira cómo se tiñe", "pt-BR": "Veja como se tinge", "en": "Watch it tint"},
 "gancho_ventana": [0, 3.0],
 "subtitulos": [{"desde": 4.0, "hasta": 8.0, "texto": {"es-419": "El agua del río se ordena…"}}],
 "efectos": "auto",
 "musica": {"id": "mus_w03_bed", "volumen_db": -6},
 "tarjeta_final": true, "tarjeta_fondo": "C:/AI/marketing/fondos/muelle_1088x1920_707.png",
 "descripcion": {"es-419": "Vierte, ordena y tiñe el río. #tintalu #watersort #satisfying"}
}
```

- **`toma.eventos`:** el `.json` de `tomar.py`. Con `"efectos": "auto"`, los efectos salen de sus líneas `AF_CAPTURE`. Si la toma no tiene eventos (por ejemplo, las anteriores a E1), se da una lista: `[{"t": 25.5, "id": "sfx_win_toada"}]`.
- **`por_idioma`:** cambia campos para un idioma. **Cada idioma lleva su toma, con el juego en ese idioma.** Solo una toma sin ningún texto del juego (`af_capture_no_hud`, sin carteles) se marca `"toma_neutra": true` y sirve para los tres.
- **`slowmo`:** el `af_capture_slowmo` con que se grabó. Estira los desfases del sonido. **`velocidad`:** la cámara lenta del montaje.
- **`musica` y `efectos`:** ids de `Assets/_Project/Audio/audio_manifest.json` (todo CC0). No se acepta otro audio.
- **`cifra_fuente`:** obligatoria si un texto lleva un porcentaje (encargo §2, gancho 3). Sin ella, el control de calidad lo rechaza.

## 3. Montaje

- **Encuadre:** la pantalla del juego **entera**, escalada a la altura del formato, sobre una copia desenfocada con velo petróleo. Nunca se recorta el tablero ni se tocan los píxeles del juego (§0.1). `toma.recorte` solo quita bordes que no son del juego.
- **Rótulos:**
  - el gancho, en la placa de madera, arriba de la zona segura y con fundido de 0,15 s;
  - los subtítulos, abajo de la zona segura.
  
  Los dos con las plantillas de `kit-redes.md` §4.
- **Tarjeta final:** fundido de 0,3 s y 1,8 s de tarjeta del idioma (`kit/tarjeta-final/`). Los bucles de *satisfying* la apagan (`"tarjeta_final": false`).
- **Salida:** H.264 a 30 fps (CRF 18, yuv420p, `faststart`) y AAC a 192 kbps, en 48 kHz y estéreo.

### 3.1 Efectos de los guiones que funcionan (`fabrica/efectos.py`, 2026-11-05)

Salen de `linea-de-produccion.md` §5. Los usa `montar_himno.py` y son opcionales en el brief:

| Efecto | En el brief | Qué hace | Referencia |
|---|---|---|---|
| **H5 · palabras** | `"palabras": [{"desde": 2.5, "hasta": 3.5, "texto": {"es-419": "¡Tiñe!"}}]` | Una o dos palabras grandes en crema con contorno, en el tercio alto de la zona segura. Entran con un «pop» (135 % → 100 % en 0,18 s) y se apagan en 0,12 s. Se ponen en los golpes del himno (0 · 2,5 · 7,6 · 12,6 · 15,2 · 17,7) | Hole and Fill |
| **H1 · contador** | `"contador": {"etiqueta": {"es-419": "Vertidos"}, "modo": "vertidos"}` (o `"quedan"`) | Placa de madera arriba a la izquierda, con el número de vertidos de la pieza (o los que faltan, el campo `left`). Late al llegar cada vertido. Lee los eventos `pour` del `.json` de cada toma, así que el número es real | Block Blast, el tubo de Magic Sort |
| **H2 · marea** | en un plano: `"entrada": {"tipo": "marea", "dur": 0.45, "color": "auto"}` | El agua del color del plano nuevo sube por la pantalla con una franja ondulada y lo descubre. Con `auto`, el color es el más saturado del agua (no el de la madera) | Block Blast en TikTok (cambio de color de fase) |

`python efectos.py --self-test` prueba los tres efectos. `python efectos.py --muestra <carpeta>` deja un PNG de cada uno para revisarlo. Las cajas de las palabras y del contador van al manifiesto (`que`: `palabra` o `contador`), así `calidad.py` comprueba que están en la zona segura y que no llevan frases prohibidas.

## 4. Sonido

`screenrecord` no graba el audio y E1 no lo añade, así que la fábrica **vuelve a poner el sonido del juego**:
- **El lecho:** el tema del mundo (`mus_wNN_bed`) en bucle, con fundidos y a −6 a −8 dB.
- **Cada vertido** (la línea `AF_CAPTURE pour`, que se escribe con el segundo toque):

  | Momento | Efecto |
  |---|---|
  | −0,26 s | `sfx_select` (el primer toque) |
  | +0,22 s | `sfx_pour_start` |
  | +0,30 s | `sfx_pour_loop` (0,40 s) |
  | +0,68 s | `sfx_pour_end` |

  Los desfases salen de una toma de E1 del 2026-11-01: la cuia viaja unos 0,22 s y el chorro dura hasta unos 0,68 s. Se estiran con `slowmo` y con `velocidad`. **Es una aproximación** al sonido real: el sellado y el teñido aún no tienen línea `AF_CAPTURE` y no suenan.
- **La victoria** (`autoplay_end` con `won`): `sfx_win_toada`, +0,35 s.
- **La mezcla:** a −14 LUFS, con un pico real ≤ −1,5 dBTP (`loudnorm`).
- **Para el escritorio:** sería mejor una salida de audio en el modo de captura (el audio real, o líneas `AF_CAPTURE` del sellado y del teñido). Va en «Enganches para el integrador» de `spark-estado.md`.

## 5. Control de calidad (§5 del encargo)

| Prueba | Regla | Si falla |
|---|---|---|
| Técnica | La medida del formato, H.264 a 30 fps, una pista de audio, ≤ 60 s y ≤ 100 MB [P] | Vuelve a montaje |
| Sonido | −14 ± 1,5 LUFS y pico ≤ −1 dBFS | Vuelve a montaje |
| Movimiento | Algo se mueve en los 2 primeros segundos (diferencia entre fotogramas ≥ 1,5) | Vuelve a montaje |
| Textos | Sin las frases de §0.7 en los tres idiomas: «#1», «el mejor», «gratis para siempre», «sin anuncios», «gana dinero», «para niños», «IQ»… Sin porcentajes sin `cifra_fuente` | Vuelve a montaje |
| Zona segura | Cada rótulo dentro de la zona segura del formato | Vuelve a montaje |
| Derechos | Todo el audio sale del manifiesto del juego, con licencia CC0 o propia | Vuelve a montaje |
| Manifiesto | `pilar`, `objetivo` y `etiqueta_ia`; si hay `planos_ia`, `etiqueta_ia` es `true` | Vuelve a montaje |
| Idioma de la toma | Una toma por idioma, salvo `toma_neutra` | Vuelve a montaje |
| Victoria visible | Si la pieza llega a la victoria, el panel queda ≥ 4 s en pantalla [P] | Vuelve a montaje (o se graba una toma más larga) |
| Visión | Comparar los planos con capturas reales usando Qwen3-VL-8B | **Pendiente:** el modelo no está instalado (Q-MK-S0-2). La suple la revisión de la hoja de contactos |

- **Estados:** si una pieza falla 2 veces seguidas, pasa a `revision_humana`.
- **El manifiesto de cada pieza** (`piezas/<id>/manifest.json`) lleva:
  - el brief;
  - por salida: la toma y su SHA-256, los rótulos con su caja, cada sonido con su tiempo y su licencia, y el SHA-256 del vídeo;
  - el control de calidad y el historial de estados.

## 6. Cola y aprobación

- **Estados:**
  - `borrador`
  - → `calidad_ok`, `calidad_fallo` o `revision_humana`
  - → `aprobado` o `rechazado`
  - → `publicado`
- **Aprobación:** solo se aprueba lo que está en `calidad_ok`. Las 2 primeras semanas aprueba el dueño cada lote (Q-MK-5).
- **La hoja de contactos local:** `cola.py hoja` (fotogramas cada 4 s, rótulos y control de calidad). Al dueño le llega como **Artifact privado** con los vídeos (S5).
- **Interruptor de parada:** si existe `C:\AI\marketing\PAUSA`, `cola.py publicables` no devuelve nada. La publicación (S7) solo publica lo que devuelva esa orden, así que el interruptor la para en el acto (§0.9).

## 7. Lo que falta

| Qué | Cuándo |
|---|---|
| Formatos 1:1 y 16:9 con piezas reales (el código ya los admite) | Con el lote 1 |
| Voz en off con TTS neutra (Q-MK-6) | Cuando haya un TTS con licencia comercial instalado (Q-MK-S0-2) |
| La revisión con el modelo de visión | Con Qwen3-VL-8B (Q-MK-S0-2) |
| La publicación por la API de Buffer (S7) | Con las cuentas y el token del dueño |
