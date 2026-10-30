# Vídeo de la ficha de Play (fase 3 de D-065)

Vídeo de 30 s sin voz, con gameplay real del teléfono, en es-419, en y pt-BR. Encargo: `docs/dev/traspaso/spark-graficos-tienda.md` §3. Guion de base: 07 §10.3 y `../assets-tienda.md` §5. Sigue el estilo del concepto A, el elegido por el dueño (`../graficos-v2/README.md`). Procedencia completa en `manifest.json`.

## Archivos

| Archivo | Medida | Para qué |
|---|---|---|
| `tintalu-<idioma>-horizontal-30s.mp4` | 1920 × 1080, 30 s | **El de la ficha de Play** (recomendada, Q-GV-4): Play lo enseña en el sitio del gráfico de funciones, que es horizontal [NV] |
| `tintalu-<idioma>-vertical-30s.mp4` | 1080 × 1920, 30 s | Shorts, redes y creativos |
| `tintalu-<idioma>-*-15s.mp4` | las dos | El corte de 15 s (Q-GV-2): los primeros 15 s se sostienen solos y acaban en el logotipo |

Los 12: H.264 + AAC a 30 fps, audio a −16 LUFS, entre 6 y 20 MB. `<idioma>` es `es-419`, `en` o `pt-BR`. La UI del juego y los textos van en cada idioma.

**Para publicarlo:** el dueño lo sube a YouTube, como público o no listado y sin anuncios, y pega el enlace en Play Console (encargo §3). El canal lo decide el dueño (`docs/legal/marca.md` §4).

## Guion

| Segundos | Plano | Qué se ve (todo es captura real, salvo donde se dice) | Texto |
|---|---|---|---|
| 0-3 | Vertido | Nivel 42 (Encuentro de las Aguas): una cuia vierte açaí y otra se sella con su anillo. Arranca acercado y se abre. Sin logotipo | — |
| 3-8 | Encadenados | Los últimos vertidos del 42 seguidos: tres cuias se sellan, con el contador de semillas arriba | — |
| 8-12 | Llenar y teñir | La carga del nivel 42, a cámara lenta ×2 | — |
| 12-15 | Logotipo | La intro de la marca (D-051): la cuia vierte, el río se tiñe y sale «Tintalú». En horizontal va sobre el amanecer animado con IA | «Ordenar agua» · «Water sort» · «Classificar água» |
| 15-19 | Difícil y Marea | Nivel 129 (Difícil): un vertido y la Marea que lo deshace (el contador baja de 3 a 2) | — |
| 19-23 | El muelle | TiTino en su rama con su Dato del río, Capi en la canoa y Rosa en el río | — |
| 23-27 | El Mapa | El Mapa del río: la canoa en Manaos, y el paso de la vista Mundo a la Cuenca con los países | — |
| 27-28,5 | Victoria | El confeti y «¡Perfecto!» con TiTino | — |
| 28,5-30 | Cierre | El logotipo v2 sobre el Encuentro de las Aguas animado con IA (vertical) o sobre el amanecer (horizontal) | «Sin wifi, a tu ritmo» · «Offline, at your pace» · «Sem wi-fi, no seu ritmo» |

**Lo que cambia respecto a la tabla de `../assets-tienda.md` §5:**
- **Tensión (15-19):** se ve la Marea en un Difícil, pero no la escena de «quedan dos cuias y el agua se aquieta».
- **Mascotas (19-23):** es el muelle real. No hay plano de Capi trayendo una cuia de rescate ni de Rosa saltando al siguiente nivel.
- **Mundos (23-27):** es el Mapa real. No se ven los fondos de las 4 paradas.
- Esos planos se pueden grabar si el dueño los quiere: se hacen con las mismas herramientas.

## Cómo se hizo

1. **Tomas:** con `adb shell screenrecord` en el teléfono de pruebas (Samsung SM-S938B, `dev.apk` del 2026-10-30), a 1080 × 2340 y 60 fps.
   - El teléfono juega solo, guiado por `AF_LAYOUT` y `AF_STATE`.
   - Durante las tomas: modo demo de Android y No molestar.
   - **Al terminar:** el modo demo quedó desactivado. No molestar lo activó el dueño, y el dueño lo apaga.
2. **Los píxeles del juego no se tocan:**
   - solo se recortan, siempre por encima de la marca «Development Build»;
   - se escalan;
   - en «Llenar y teñir», se ralentizan.
3. **IA:** solo los fondos.
   - Los dos fondos del concepto A (Z-Image), animados con Wan 2.2 14B imagen a vídeo (Apache 2.0) en la RTX 5080.
   - Prompts de pura naturaleza: el agua fluye, las nubes pasan.
   - La IA no genera personajes, UI ni texto.
4. **Montaje:** con Pillow y FFmpeg en el equipo de Spark.
   - Rótulos en Baloo 2 ExtraBold, en la placa de madera de las capturas.
   - El logotipo, de la identidad v2.
   - Los tres idiomas usan las mismas tomas y el mismo corte, sincronizados con la victoria de cada grabación.

## Preguntas [P], con la recomendada aplicada

- **Q-GV-1, la música:** lleva `mus_menu` (`music/menu.wav`, CC0) más dos efectos del juego (`sfx_fill` y `sfx_confetti`, CC0).
  - El encargo admite CC0 («sin música de terceros que no sea CC0»). `../README.md` §3 bis recomendaba no usarla hasta que haya una pista con cesión total.
  - **Recomendada:** usarla ahora y cambiar la pista cuando exista la de cesión total, porque el vídeo se vuelve a montar en minutos.
  - **Riesgo:** una pista CC0 puede tener reclamaciones de Content ID de terceros en YouTube. Al subirlo, mira «Restricciones» en YouTube Studio.
  - **Alternativa:** música propia generada con IA (ACE-Step, MIT), que pide bajar un modelo nuevo.
- **Q-GV-2, la duración:** 30 s, con el corte de 15 s aparte. La pregunta 8 de 07 sigue abierta.
- **Q-GV-3, los textos:**
  - el descriptor es el de §5 («Ordenar agua»);
  - el cierre usa el rótulo de la captura 8 del concepto A en lugar de «Play offline» / «Jogue offline», porque esos son imperativos y Play no admite llamadas a la acción en los gráficos.
- **Q-GV-4, la orientación:** el horizontal para Play y el vertical para lo demás.

## Lo que conviene mirar

- **Fechas a la vista:** el muelle enseña la tarjeta «Reto del río · 30 oct», como la captura 5 del concepto A.
- **El Dato del río** del muelle («Probablemente ve sin colores…») es el que salía ese día. El asesor cultural ya revisa estos datos en el juego.
- **Revisión cultural** (07 §8.2): los fondos animados son los del concepto A (el Encuentro de las Aguas y un amanecer en el río), sin personas, aldeas ni barcas.
- **Los rótulos** están revisados en la imagen en los tres idiomas (tildes, «ú» de Tintalú, «á» de água).
