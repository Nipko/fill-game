# Gráficos de la ficha de Google Play (1.0)

Capturas de teléfono de la ficha, hechas con el juego real (D-052: el arte por código es el arte final y vale para la ficha, las capturas y la UA). Las sube el dueño a mano en Play Console; los agentes no publican nada.

- **Fuente de lo que sale en cada una:** 07 §10.2 y `../assets-tienda.md` §4.
- **Texto superpuesto:** la tabla §6 de `../<idioma>/ficha.md`, leída tal cual por el test (no hay claves de `strings.csv` para estos textos). Va sobre la placa de madera del kit de UI (Q-B13-5 (a)), con Baloo 2 ExtraBold en crema.
- **Medida:** 1080 × 1920 px, vertical, PNG de 24 bits sin transparencia (07 §10.2; formato de Play [NV]).
- **Composición de las 01-08:** la pantalla del juego a 360 × 640 dp (16:9) reducida en un marco de madera bajo la placa, sobre el petróleo de la marca (#14444A, D-045). Las 09 y 10 son la pantalla de 16:9 a sangre, sin texto.

## Qué hay en cada una

| Archivo | Pantalla real | Hueco de 07 §10.2 | Texto (es-419) |
|---|---|---|---|
| `01-vertido-encuentro.png` | Nivel 42 (parada del Encuentro de las Aguas, mundo 3), tras 12 vertidos de la solución, una cuia vertiendo Açaí con el chorro ya en la superficie del destino | 1 | Ordena el agua del Amazonas |
| `02-estuario.png` | Nivel 6 (Estuario, Belém): tablero de 5 cuias con los patrones de fruta en «Siempre» | 2 | 14 colores de frutas y ríos |
| `03-llenar-y-tenir.png` | Nivel 25 (Alter do Chão): «Llenar y teñir» a 1.050 ms de 1.400, con las frutas cayendo y las cuias de arriba tiñéndose | 3 | Mira cómo se tiñe el río |
| `04-mapa-del-rio.png` | Mapa del río, vista del mundo 3, con la canoa entre Manaos y Anavilhanas (próximo nivel 58) | 4 | Remonta el Amazonas hasta donde nace |
| `05-nivel-dificil.png` | Nivel 58 (Difícil, con la Hoja) tras 4 vertidos de la solución | 5 | Difícil de verdad, justo siempre |
| `06-patrones.png` | Nivel 75 (Tefé), 10 cuias con los patrones automáticos, tras 5 vertidos | 6 | Patrones para todos los ojos |
| `07-titino-muelle.png` | El muelle del río (D-050) en el nivel 31 a las 8:00, con TiTino en su rama y su saludo, y las tarjetas del Reto y la Bitácora | 7 | TiTino te acompaña |
| `08-reto-del-rio.png` | Reto del río ganado: el calendario del mes con el sello del día y su premio | 8 | Juega sin wifi, vuelve cada día |
| `09-mercado.png` | Extra sin texto: el Mercado, pestaña Semillas | — | — |
| `10-liga.png` | Extra sin texto: la Liga del río (D-046), pestaña Global, con la API falsa del test | — | — |

Las tres carpetas (`es-419/`, `en/`, `pt-BR/`) tienen los mismos archivos, con el juego y el texto en ese idioma.

**Diferencias con 07 §10.2 (pendientes del dueño):**
- 5: sin la Marea usada en el bloqueo; solo el nivel Difícil con su chip y su cabecera.
- 6: sin la simulación de daltonismo lado a lado (es un montaje, no gameplay; `assets-tienda.md` §4 lo deja [P]); es un tablero grande con los patrones.
- 7: TiTino en el muelle, no celebrando en la victoria (a 16:9, la victoria con la ranura del Dato del río no deja sitio a TiTino).
- 8: el calendario del Reto; la Bitácora se ve en la 07 y la Canasta no sale.
- 1: a 16:9 el tablero de 2 filas tapa el fondo del Encuentro de las Aguas; solo asoma el cielo.
- Play admite hasta 8 capturas de teléfono por idioma [NV]: las 09 y 10 son opcionales, para cambiar alguna de las ocho si el dueño lo prefiere.

**Lo que conviene mirar antes de subirlas:**
- 04: a 16:9, la línea «Premio del mundo · faltan 23» del Mapa asoma cortada bajo la cabecera (es así en el juego; la vista de la cuenca, peor: amontona las placas de los mundos con la ficha de casa).
- 08: la cabecera del Reto lleva encima el confeti de la victoria, y el calendario ofrece «Cosecha doble: x2 por video» (botón del juego, no un anuncio).
- 09: los precios son los del Mercado sin Play del editor (pesos colombianos en los tres idiomas); en Play cada jugador ve los de su país.
- 10: los rivales, los puntos y los avatares son los de la API falsa de los tests, no jugadores reales.

## Cómo regenerarlas

Con el proyecto cerrado en el Editor y gráficos (sin `-nographics`), desde la raíz del repositorio:

```
unity test <repo> --mode PlayMode --architecture x86_64 --filter AF.Tests.PlayMode.StoreListingSnapshotTests --output Logs/store.xml --timeout 3600 -- -logFile Logs/store.log
```

(`--architecture arm64` en el portátil.) El test es `[Explicit]` y de la categoría `StoreListing`, así que la suite normal no lo ejecuta. Escribe aquí los 30 PNG y deja en el log una línea `AF_STORE` por captura (texto, tamaño y archivo). Fija la hora, el perfil, el idioma y la pantalla simulada; usa los presentadores inmediatos de anuncios y compras, y solo pinta los lienzos del juego, así que no salen el FPS, datos de depuración ni banners de anuncios de prueba. Comprueba también que el texto cabe en su placa (≤ 36 caracteres) y que no hay claves de localización crudas a la vista.

El código: `Assets/_Project/Tests/PlayMode/StoreListingSnapshotTests.cs`. Si cambia un texto de la ficha (`../<idioma>/ficha.md` §6), basta con volver a lanzarlo.

## Icono y gráfico destacado

No están aquí: salen de la identidad v2 (`docs/art/marca-oficial/v2/`), generados por código.

| Pieza | Archivo | Comprobado (2026-10-28) |
|---|---|---|
| Icono de la ficha | `docs/art/marca-oficial/v2/out/png/google-play-512.png` | 512 × 512, PNG de 8 bits por canal, RGB sin transparencia, 126 KB; sin texto (07 §10) |
| Gráfico destacado | `docs/art/marca-oficial/v2/out/png/feature-graphic.png` | 1024 × 500, PNG RGB de 24 bits sin transparencia, 114 KB (07 §10; `assets-tienda.md` §3) |

Los dos SVG de origen coinciden byte a byte con los que genera hoy `generar.py` (variante oficial `dos-meandros`), así que no hizo falta regenerar los PNG.
