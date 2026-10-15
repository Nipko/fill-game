# Prueba de la ola 2 de la tanda 4 en el teléfono (2026-10-15): resultados para el escritorio

El portátil compiló `integracion-fase1-fase2` en `0f67f1f` («Cierra la ola 2 de la tanda 4…»), instaló el `dev.apk` en el SM-S938B por depuración inalámbrica y siguió el «QUÉ PROBAR EN EL TELÉFONO» de ese commit.

**Resultado: cumple.**
- Los fondos por parada, la llegada en la victoria, el rótulo del lugar, la tropa lateral (D-021), Capi fuera del muelle y la regresión funcionan.
- **No hay defectos.**
- Hay cuatro observaciones (al final). La más importante para el dueño: el rótulo del lugar tapa la silueta mientras se ve.

Las capturas, los vídeos y los logcat están en el portátil, en `Logs/device/tanda4o2/` (fuera de git).

## Verificación (portátil ARM64)

| Comando | Resultado |
|---|---|
| `dotnet build dotnet/AmazonFill.slnx -warnaserror` | 0 avisos, 0 errores |
| `dotnet test dotnet/AmazonFill.slnx` | 975 superados, 6 omitidos (981) |
| `python tools/validate_schema.py` | 103/103 PASS |
| `unity run … -executeMethod AF.EditorTools.BuildDev.BuildAndroidDev` | Código 0: `dev.apk` de 66,20 MB |
| `adb install -r Builds/Android/dev.apk` (inalámbrico) | `Success` |
| `StopSheetSnapshotTests` (PlayMode, explícito, con gráficos) | 1/1 superado (40 s); compila sin errores |

Notas:
- Los tests de Unity se lanzaron solo al final, porque tumban el adb del teléfono. EditMode y PlayMode completos no se repitieron en el portátil: el escritorio da EditMode 2.231 y PlayMode 612 en `0f67f1f`.
- Las partidas se jugaron con las soluciones A* de `Logs/validacion/soluciones.json`. Los packs 1-200 no cambian entre `1593e20` y `0f67f1f`.

## Resultados

### 1 · Fondo por parada (M1.4, M2.4): cumple, con la observación 2

Con perfil limpio (`pm clear`) y `af_level N`, cada nivel sale con su parada, su silueta provisional y el agua del mundo:

| Nivel | Parada | `id` | Héroe (`hero_src`) | Agua | LUT | `build_ms` |
|---|---|---|---|---|---|---|
| 1 | 0 | belem | `two_spires` (shape) | white | – | 4,69 |
| 11 | 1 | santarem | pieza por código | clear | – | 4,18 |
| 21 | 2 | alter_do_chao | shape | clear | – | 4,26 |
| 31 | 3 | flona_tapajos | shape | clear | – | 4,55 |
| 41 | 4 | encontro | pieza por código | black | – | 5,40 |
| 51 | 5 | manaos | `dome` (shape) | black | – | 5,53 |
| 71 | 7 | tefe | shape | black | – | **8,39** |
| 81 | 8 | leticia | shape | white | – | 5,28 |
| 91 | 9 | leticia_atardecer | shape | white | dusk | 6,12 |
| 101 | 10 | amacayacu | shape | white | – | 3,77 |
| 121 | 12 | tarapoto | shape | white | – | 3,97 |
| 131 | 13 | rio_frontera | `kit:rio_ancho_con_baliza` | white | – | 3,81 |
| 151 | 15 | itaya | `rafts` (shape) | white | – | 4,02 |
| 171 | 17 | nauta | `kit:orilla_de_nauta` | white | – | 6,03 |
| 191 | 19 | pacaya_alba | shape | white | dawn | 3,86 |

- Todos los saltos con `af_level` dan `change: cut` y `fade: false`, como se espera en una primera carga.
- Entre niveles de la misma parada (`Siguiente` del 1 al 2) sale `change: same` y `build_ms: 0`.
- **El 71 (Tefé) pasa el presupuesto:** 8,39 ms frente a ≤ 8 ms. Los demás quedan entre 3,8 y 6,1 ms.
- Archivos: `p1_hoja_a.jpg` (con el rótulo) y `p1_hoja_b.jpg` (sin él).

### 2 · Llegada a la parada en la victoria (M1.6): cumple

| Victoria | Llegada | `fact_id` | `welcome` | `shown_dp` / `slot_dp` | Siguiente → `load` |
|---|---|---|---|---|---|
| 10 | no (correcto) | – | – | – | 0,25 s |
| 19 (referencia) | no | – | – | – | 0,24 s |
| 20 | Alter do Chão | `map.stop.alter_do_chao.dato_01` | false | 101 / 139 | 0,25 s |
| 40 | Encontro | `map.stop.manaos.dato_01` | false | 137 / 139 | 0,20 s |
| 80 | Leticia | `map.country.co.dato_01` | **true** | 139 / 139 | 0,29 s |

- La franja empieza con el panel (`arrival start`) y termina sola a los 1.200 ms (`complete`, `by: time`).
- Después salen `stop_arrive` (`skipped: false`, `view: victory`) y, al pulsar Siguiente, `river_fact_shown` con el mismo `fact_id`.
- En pantalla:
  - la canoa recorre la barra del río y llega a la mini cuia;
  - sale la placa del lugar con TiTino y el dato;
  - en el 80, «Colombia te da la bienvenida».
- **Siguiente es igual de rápido** con llegada o sin ella: 0,20-0,29 s hasta `load`.
- El 10 no lleva llegada, y su Siguiente abre el banner del mundo 2.
- Archivo: `p2_hoja_llegada.jpg`.

### 3 · Rótulo del lugar (M2.3): cumple, con las observaciones 1 y 3

- **Sale en todos los niveles pedidos, en el primer intento:**
  - en 31, 51, 71, 91, 101 y 121, sobre el horizonte (`mode: horizon`, 2 filas);
  - en 131, 151, 171 y 191, como placa de madera bajo el HUD (`mode: plate`, 3 filas).
- **Duración:** 3.000 ms (3.035 en el 31, el texto más largo).
- **Textos:**
  - «Floresta Nacional do Tapajós · Brasil», «Manaos · Brasil», «Lago de Tefé · Brasil»;
  - «Leticia al atardecer · Colombia», «Amacayacu · Colombia», «Lagos de Tarapoto · Colombia»;
  - «El río frontera · Colombia y Perú», «Río Itaya (Iquitos) · Perú», «Nauta · Perú», «Pacaya-Samiria al alba · Perú».
- **Con la placa a la vista se puede jugar:** tocar una cuia de la fila 1 la selecciona en 131, 171 y 191, a 0,5, 1,4 y 2,2 s del rótulo.
- **En el 151 no hay defecto.** La cuia 0 está vacía, así que el toque da `reject`, que es lo correcto.
- Archivos: `p3_rotulo_bandas.jpg` y `p3b_placa_sel.jpg`.

### 4 · Tropa lateral (D-021, TL1): cumple

Vídeo de la intro (`screenrecord`) en los tableros de 11 cuias del mundo 4 y del mundo 5:

| Nivel | Lado | Esperado |
|---|---|---|
| 86 (primero de 3 filas del mundo 4) | sí: los miquitos cuelgan de lianas a los dos lados y lanzan la fruta | sí |
| 89 | no: la fruta cae desde arriba | no |
| 95 | sí | sí (1 de cada 3) |
| 143 (primero de 3 filas del mundo 5) | sí, con la especie roja del mundo 5 | sí |
| 144 | no | no |

- La tropa no tapa cuias: queda en el marco, fuera de la cavidad.
- **En el 95 y el 143, los miquitos ya cuelgan a los lados antes de que caiga la fruta,** mientras se cierra la tarjeta previa.
- Archivos: `p4_lados_l0xx.jpg`, `p4_l086_lados.jpg` y los `p4_l*.mp4`.

### 5 · Capi fuera del muelle: cumple

En ninguna captura del tablero (los 15 niveles de la sección 1) aparece Capi fijo en el muelle.

### 6 · Regresión: cumple

- **Partida automática con la solución A*:** el 1-12 seguidos con «Siguiente» (perfil limpio), y el 41, 63, 100, 143 y 183.
- **17/17 ganados**, con el tablero idéntico al pack, 0 rechazos, 0 Aguas estancadas falsas y 0 avisos `step_budget`.
- Banner de mundo en el 11 y el 41.
- Tarjeta previa en el 63, 100, 143 y 183; el 183 sigue con `bite_demo: animated`.
- 0 excepciones ni líneas `E Unity` en logcat.

### 7 · Hoja VA-20 (dueño y asesor)

- **Generada en el portátil:** `StopSheetSnapshotTests` (batchmode con D3D11, filtro de nombre completo) escribió `Logs/snapshots/es-419/02f_board_stops_sheet.png` (5 × 4, 523 KB), lista para el dueño y el asesor.
- **Contenido:** las 20 composiciones, con borde ámbar en las de asesor obligatorio (7 Tefé, 11 Puerto Nariño y 15 Itaya).
- **Para el dueño, al revisarla:**
  - la 17 (Nauta, `kit:orilla_de_nauta`) apenas se distingue del fondo;
  - la 01 y la 04 son piezas por código (dos aguas), sin silueta.

## Observaciones

1. **Para el dueño: el rótulo del lugar tapa la silueta mientras se ve** (pregunta 3 del QUÉ PROBAR).
   - Con 1-2 filas, el texto va centrado justo encima del héroe: en Manaos tapa la cúpula, y en Tefé, las casas. Lo mismo pasa con el perfil de Leticia y los árboles de Amacayacu.
   - Son 3 s, en el primer intento, que es justo cuando el jugador llega al lugar.
   - Comparación lado a lado en `p3_rotulo_bandas.jpg`.
   - Hay dos opciones:
     - subir el rótulo por encima de la silueta;
     - dejarlo como placa también con 1-2 filas.
2. **Para el dueño: con 3 filas la silueta no se ve nunca.** De la banda del horizonte solo asoma una franja de cielo bajo el HUD, porque el panel del tablero ocupa el resto. Pasa en los niveles de 11 cuias (131, 151, 171 y 191).
3. **Para el escritorio (menor): la placa y la cuia elevada se rozan.**
   - Con la placa a la vista, la cuia 0 elevada llega hasta el borde inferior de la placa, y el corcho la toca en 131 y 191.
   - En el 171, la placa cubre en parte el emblema de la Hoja de la cuia 1.
4. **Para el escritorio:**
   - **`build_ms` de Tefé (71):** 8,39 ms, por encima de los 8 ms del presupuesto. Es una sola medida en frío; los demás quedan por debajo de 6,2.
   - **Clave del dato del 40:** la llegada al Encuentro de las Aguas (parada 4) usa la clave `map.stop.manaos.dato_01`, aunque el texto sí habla del Encuentro. ¿Es la clave que se quería?
5. **El teléfono:** batería al 15 % durante la prueba y almacenamiento al 100 % (1,8 GB libres).
