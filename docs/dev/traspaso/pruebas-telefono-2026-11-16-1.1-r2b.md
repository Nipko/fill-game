# Prueba de la 1.1 en el teléfono, ronda r2b (2026-11-16)

Spark sigue `encargo-spark-prueba-1.1-r2b.md` sobre `integracion-fase1-fase2` en el commit `06ca42c3`: `dev.apk` de `scripts/unity-build.ps1 -Method Dev`, con `[AF.BuildDev] OK`. Capturas y logcat en `Logs/device/v11r2b/` (fuera de git).

## Los teléfonos

- **SM-S938B (S25 Ultra):** Android 16, Adreno, 1080 × 2340, 11 GB, por Wi-Fi (depuración inalámbrica).
  - El APK del portátil estaba firmado con otra clave de depuración: se copió el perfil (`profile.json`), se desinstaló, se instaló el de Spark y se restauró el perfil (18+ y nivel 51).
  - **Sale de la prueba a media mañana:** la depuración inalámbrica se apagó sola dos veces y el teléfono se quedó sin batería de noche.
- **SM-S918B (S23 Ultra):** Android 16, Adreno (OpenGL ES 3.2), 1080 × 2316, 7 GB, por cable. Hace casi toda la prueba.
  - Perfil nuevo en cada bloque (`pm clear`), con 18+ y «Rechazar todo», contestados por Spark (pruebas autónomas, por indicación del dueño).
- **El Moto G23** no se usó en esta ronda (el dueño pidió los Samsung), así que el bloque 4 del encargo queda pendiente.

## Resumen

| Punto | Resultado | Obs. |
|---|---|---|
| Segunda apertura (S25) | `boot` `splash_ms` 3.811 y 3.899, `first_open` false. **Cumple** | — |
| 1. Viaje de Capi, 70 (S25) | `map_journey` 4.500 ms (`per_buoy_ms` 450, `elapsed_ms` 4.511); «Jugar» listo a 4.745 ms sin tocar. **Cumple** | — |
| 1. Viaje de Capi, 80, cruzando al mundo 4 (S23) | `map_open` `zoom` 6,00 con `world_zoom` 15,00: **el arreglo del zoom alto está**; 4.515 ms, listo a 4.749 ms. **Cumple** | 4 |
| 2.2 Tucuxi (S23) | tras ganar el 51 y abrir el Mapa desde el inicio, **no se ve el primer plano ni hay `crew_joined`** | 1 |
| 2.3-2.6 Miquito, llegada, cambio de mundo en el 40, Reducir movimiento | sin probar | — |
| 3. Rótulos y «Faltan N» (S23, 20 `af_max_level`) | **cumple** lo que pide el guion, con detalles de solapes | 3, 5, 6 |
| 3. Zoom en los tramos largos | 291 ×1,79 · 296 ×2,08 · 441 ×2,22 · 426 ×2,89 · 451 ×2,09 · 481 ×3,40: la canoa y la parada siguiente se ven a la vez. **Cumple** | 6 |
| 4. Memoria del Mapa v2 frente a la 1.0 (S23) | `map_after` limpio y `GL mtrack` fijo, pero el PSS sube ~10 MB en dos de las tres vueltas | 2 |
| 5. Marco de país | a medias: el retrato del inicio sale con el marco (`frame_w09`) | 7 |
| 6. Segundo gesto de los miquitos (S23) | los tres gestos salen y el nivel siguiente vuelve a `first`. **Cumple** | — |
| 7. Play Games | sin probar (la huella de la clave de depuración de Spark no está dada de alta) | — |
| Encargo §2, los tres audios | sin probar: necesitan el oído del dueño | — |
| Encargo §3, textos en `en` y `pt-BR` | Mapa del 71 y tutoriales del Aguapé (303) y de la Lancha (363), enteros y en su idioma. **Cumple** | — |
| Encargo §4, el Moto G23 | sin probar en esta ronda | — |

## 1. El viaje de Capi

- **El 70, en el S25:** como en la r2. Al abrir desde la victoria, el botón dice «Back»: es lo previsto, porque vuelve a la victoria.
- **El 80, en el S23, con el arreglo `e74b0eb2`:** el viaje usa `zoom` 6,00 en lugar del 15,00 del mundo. La canoa ya no cruza la pantalla de golpe.
  - Capturas: `s23/50-viaje80-*.png`.
  - Durante el viaje se ve la observación 4.

## 2. La tripulación

- **Tucuxi (2.2):** `pm clear`, `af_max_level 50`, `af_level 51` con juego automático.
  - Se gana el 51 (14 vertidos). La victoria trae la Bitácora, no «Ver mapa».
  - Desde el inicio, el Mapa abre en la vista **Cuenca** (`map_open` `source` l10, `view` basin, `zoom` 1,00), con la llegada a la parada 5 (`stop_arrive` `level` 50).
  - No hay ninguna línea `map_journey` ni `crew_joined`, y en las capturas no aparece el primer plano (`s23/23-tucuxi-*.png`).
  - En la r2, el portátil vio lo mismo con el tucán (vista Cuenca y sin línea del primer plano). Observación 1.
- **El tucán (2.1)** no se repitió: ya lo vio la r2.

## 3. Rótulos, «Faltan N» y el zoom

Capturas `s23/30-mapa-<N>.png` y las hojas `s23/hoja-*.jpg` (todas abiertas desde la pausa).

- **Rótulos enteros y en su sitio:**
  - **56:** Anavilhanas entero y sin la canoa encima.
  - **66 y 67:** Coari entero.
  - **96:** Amacayacu entero.
- **«Premio del mundo» con el rótulo junto:**
  - **71:** «Leticia · Premio del mundo · faltan 9» en un solo rótulo, sin la píldora aparte ni la placa arriba a la izquierda.
  - **79:** «falta 1».
  - **32:** Encuentro de las Aguas, «faltan 8».
  - **132:** Iquitos, «faltan 8».
- **64:** Tefé con «faltan 6 niveles» en su rótulo y la píldora «Premio del mundo · faltan 16» aparte, como pide el guion.
- **Ciudad con hito:** se ve la viñeta de Manaos en color en el 56 y el 57. La de Leticia y la de Iquitos no se distinguen en las capturas (79 y 140); revísese a mano.
- **Placa de respaldo:** en el 451 no sale, con `zoom` 2,09. El 456 tiene captura sin revisar.
- **Zoom en los tramos largos:** los `af_max_level` 291, 296, 441, 426 y 481 abren entre ×1,79 y ×3,40, y la canoa y la parada siguiente con «faltan N» se ven a la vez. El 521 abre a ×4,00.
- **Rótulos cortados en el borde de la pantalla** con el zoom lejano (296: «Font», «olivia»; 291: «Ecu», «Colomb», «amaturá»): se esperan al estar en el borde.

## 4. Memoria del Mapa v2 frente a la 1.0 (S23)

- **Cómo se midió:** `pm clear` con `af_mem_os 1` y `af_max_level 66`.
- **Desvío del guion:** la base y cada vuelta se miden **en el nivel 67** (con la pausa › «Mapa» y la flecha atrás), no en el inicio. Desde el inicio, la Bitácora y el viaje del 10 se cruzaban con los toques automáticos (observación 7).
- **Las medidas:** `dumpsys meminfo -d` tras un minuto quieto; volcados en `s23/mem-v2-*.txt` y `s23/mem-v1-*.txt`, sondas en `mem-*-probes.txt`.

| Vuelta | v2 PSS (MB) | 1.0 PSS (MB) | `GL mtrack` (MB, los dos) |
|---|---|---|---|
| 0 (base, nivel 67) | 594,5 | 541,3 | 39,7 |
| 1 | 604,5 | 557,7 | 39,7 |
| 2 | 604,8 | 557,8 | 39,7 |
| 3 | 615,5 | 556,0 | 39,7 |

- **Lo que cumple:** `map_after` da `map_tex_kib`, `map_renderers` y `map_verts` a 0 en las tres vueltas, con los mismos `obj_tex2d` (191) y `obj_mesh` (553), y con `os_graphics_mib` fijo en 135,3.
- **Lo que no cumple el criterio de ±3 MB:**
  - el PSS sube unos 10 MB de la vuelta 0 a la 1 y otros 10,8 MB de la 2 a la 3;
  - el salto de la 3 coincide con el heap gestionado: `mono_heap_mib` pasa de 59,8 a 78,3.
  - No parece una fuga del Mapa (los objetos y las texturas se liberan), pero hay que confirmarlo.
- **El Mapa de la 1.0 queda estable:** 557,7 → 557,8 → 556,0.
- **Lo que suma el v2:** ~48-59 MB de PSS y +23,5 MB de `os_graphics_mib` (135,3 frente a 111,8). En `map_settled`, el v2 tiene `map_tex_kib` 5.456.

## 6. El segundo gesto de los miquitos (S23, juego automático)

| Nivel | `gesture` | `chosen` | Monos |
|---|---|---|---|
| 251 | `duo` | true | 2 |
| 252 | `first` | false | 2 |
| 361 | `brincos` | true | 3 |
| 362 | `first` | false | 3 |
| 491 | `colgado_de_la_cola` | true | 3 |
| 492 | `first` | false | 3 |

- `perch` `branch` en todos.
- Los seis niveles se ganan sin incidencias.
- Sin juicio visual del dueño: capturas en `s23/60-tropa-*.png`.

## Textos en `en` y `pt-BR` (encargo §3)

Hoja `s23/hoja-textos.jpg`:

- **El Mapa del 71:**
  - en inglés, «Leticia · World prize · 9 left», «World 4 · Amazon Trapeze · Level 81», «Home · about 2,210 km»;
  - en portugués, «Prêmio do mundo · faltam 9», «Mundo 4 · Trapézio · Nível 81», «Casa · a cerca de 2.210 km», «Bacia» y «Mostruário».
- **Tutoriales:**
  - el Aguapé (303) y la Lancha (363) completos en los dos idiomas;
  - en portugués, «A lancha atraca quando você tiver 3 cuias vazias: ela traz fruta para terminar.».
  - Nada cortado ni partido. El texto del Aguapé sale con letra más pequeña que el de la Lancha porque es más largo.
- **Sin probar:** el cartel de la tripulación en inglés, las etiquetas de los marcos y el estreno de la Liga.

## Observaciones

1. **Media. El compañero que se une no se ve al abrir el Mapa tras su nivel.**
   - Con el tucuxi (51), el Mapa abre en Cuenca con el viaje del 10 (`source` l10) y no escribe `crew_joined`.
   - La r2 vio lo mismo con el tucán.
   - Hay que mirar si el primer plano solo sale en la vista Mundo o con «Ver mapa», y qué abre la victoria del 51.
2. **Media. La memoria del Mapa v2 sube ~10 MB en dos de las tres vueltas.**
   - Las sondas del Mapa salen limpias; el salto de la vuelta 3 sigue al heap gestionado (`mono_heap_mib` 59,8 → 78,3).
   - El v2 suma ~50 MB de PSS frente a la 1.0.
   - En un teléfono de 3,7 GB (el Moto G23, ya por encima de los 450 MB) pesa.
3. **Baja. Las boyas de la ruta se pintan encima de los rótulos:** «Let·icia», «Fonte B·oa», «Niv·el 81» (71 y 79).
4. **Baja. En el viaje del 80, la placa «Casa · a unos 3.170 km» tapa la píldora «Premio del mundo · faltan 7»** (`s23/50-viaje80-a.png`).
5. **Baja. Las placas «Mundo N · … · Nivel N» de los mundos siguientes se apilan.**
   - Seis seguidas en la vista Cuenca tras cerrar la Bitácora (`s23/41-inicio-mem.png`).
   - Tres o cuatro en la vista Mundo en el 132, el 140 y el 291.
   - Tapan el mapa de Perú y Bolivia.
6. **Baja. El Mapa de Bolivia (441-521) va muy cargado.**
   - Muchas mini cuias con sus líneas, y la pila de cinco banderas junto a la canoa.
   - En el 451, el rótulo «Orillas de Rurrenabaque · faltan 9 niveles» queda lejos de su parada (`s23/30-mapa-451.png`).
7. **Baja (herramientas de prueba). La Bitácora que sale sola en el inicio no escribe `AF_LAYOUT`.** Los toques automáticos no la ven, y su «Seguir» abre el Mapa con el viaje pendiente. Para las pruebas autónomas convendría que escribiera su `AF_LAYOUT`.
8. **Pregunta. Un perfil que ya tenía consentimiento vuelve a pedirlo.** En el S25, el perfil restaurado (`consent.profile` default) vuelve a mostrar la privacidad porque la SIM da Colombia (`co_1581`). ¿Es lo previsto al cambiar de país?
9. **Aviso. Una compra simulada.** Durante la medida de memoria, los toques automáticos de Spark cayeron en el panel «sin canoas» del nivel 67 y compraron `af_infinite_canoes_1h` en la tienda falsa de desarrollo (`purchase_sim`, sin dinero real). Es un fallo del guion de Spark, no del juego.

## Sin probar

- los tres audios (el dueño);
- la tripulación: el miquito, la llegada, el cambio de mundo en el 40 y Reducir movimiento;
- el marco de país completo;
- Play Games;
- todo el bloque 4 en el Moto G23 (el ensayo de release, el 340 a mano, los parones del 386).
