# Trabajo semanal de tendencias (S1 de D-066, versión 1)

Cada **lunes**, Spark pone al día la biblioteca de referencias, las puntúa y escribe aquí el informe de la semana: `AAAA-SS.md`, con la semana ISO. Encargo: `docs/dev/traspaso/spark-marketing.md` §3. Primer informe: `2026-44.md`. Base del análisis: `../investigacion-creatividades.md`.

## Dónde vive cada cosa

| Qué | Dónde | Por qué |
|---|---|---|
| Biblioteca (`biblioteca.json`), semillas semanales, notas y fotogramas | Equipo de Spark, `C:\AI\marketing\tendencias\` | Son referencias de terceros (§3 y §11 del encargo). No entran en el repositorio |
| Script `tendencias.py` | Ídem. Pasará al repositorio privado `fill-game-marketing` cuando el dueño lo cree (Q-MK-7) | Solo usa la biblioteca estándar de Python |
| El informe | Aquí, en `docs/marketing/tendencias/` | Es lo que sirve para decidir |

## Qué hace cada lunes

1. **Interruptor de parada:** si existe `C:\AI\marketing\tendencias\PAUSA`, no se hace nada. El script sale con el código 3.
2. **Recogida** (navegando, por las vías públicas de cada plataforma, sin raspar contra sus términos y sin descargar vídeos):
   - **Meta Ad Library:**
     - anuncios activos de vídeo en BR, MX y CO, ordenados por impresiones;
     - búsquedas fijas: `magic sort`, `water sort`, `ball sort`, `nut sort`, `goods sort`, `sort puzzle`, y las de cada competidor nuevo que aparezca;
     - de cada anuncio, 8 fotogramas en el navegador (0,2 a 50 s), el texto, la llamada a la acción, la fecha de inicio y el número de anuncios que usan la creatividad.
   - **YouTube:** Shorts orgánicos con `water sort`, `color sort asmr` y las búsquedas de la semana anterior.
   - **TikTok Creative Center** (Top Ads, Trends y Top Products para juegos de puzle en BR, MX y CO) y **Google Ads Transparency Center:** cuando el dueño deje abierta en el navegador del Spark su sesión de TikTok for Business (Q-MK-2). Sin sesión, TikTok solo enseña 4 anuncios.
3. **Ficha de cada referencia** (en `semilla-AAAA-SS.json`):
   - la fuente, el ID, el anunciante, el mercado, el inicio, las variantes y la duración;
   - **los ganchos** según la taxonomía G1-G12 y X (`../investigacion-creatividades.md`);
   - si es adaptable honestamente: ✓, ~ o ✗.
4. **Puntuación e informe:**
   ```
   python tendencias.py importar semilla-AAAA-SS.json --visto AAAA-MM-DD
   python tendencias.py informe --semana AAAA-SS --salida <repo>/docs/marketing/tendencias/AAAA-SS.md
   ```
   - **Puntuación de pago (0-100):**
     - longevidad: hasta 60 puntos, a 180 días activo;
     - variantes: hasta 25;
     - duración ≤ 30 s: 15;
     - duración ≤ 45 s: 8.
   - **Orgánico:** por vistas.
   - Los ganchos vetados por §0 (X) salen aparte.
5. **A mano**, en el informe:
   - «Formatos y sonidos en alza»;
   - **5 ideas** para Tintalú, cada una con su gancho, su escena real del juego y su pilar;
   - los hashtags por idioma y pilar, cuando estén las cuentas (S3).
6. **Entrega:** el informe va en la rama `spark/marketing-*`, con commit y push, y una línea en `spark-estado.md`.

## Cómo se pone en marcha cada semana [P]

- **Recomendada:** una tarea programada de Claude en el Spark, los lunes a las 9:00 (hora local del equipo), que sigue esta receta con el navegador.
- **Hace falta el permiso del dueño:** es una tarea periódica en su equipo.
- **Mientras no exista,** se lanza a mano: «haz las tendencias de la semana».

## Límites de la versión 1

- **Sin las API oficiales.** La API de Meta Ad Library pide verificar la identidad del dueño. La de datos de YouTube pide una clave del dueño. Se pasará a ellas en la versión 2 si hacen falta.
- **Sin cifras de rendimiento de terceros:** Meta no publica impresiones de anuncios comerciales. La longevidad y las variantes son la mejor señal pública. Las cifras de AppMagic o Sensor Tower irían [NV].
- **El audio de los anuncios de terceros no se analiza:** la vista va sin sonido.
