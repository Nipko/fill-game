# Trabajo semanal de tendencias (S1 de D-066, versión 1)

Cada **lunes**, Spark pone al día la biblioteca de referencias, las puntúa y escribe aquí el informe de la semana: `AAAA-SS.md`, con la semana ISO. Encargo: `docs/dev/traspaso/spark-marketing.md` §3. Primer informe: `2026-44.md`. Base del análisis: `../investigacion-creatividades.md`.

## Dónde vive cada cosa

| Qué | Dónde | Por qué |
|---|---|---|
| Biblioteca (`biblioteca.json`), semillas semanales, notas y fotogramas | Equipo de Spark, `C:\AI\marketing\tendencias\` | Son referencias de terceros (§3 y §11 del encargo). No entran en el repositorio |
| Script `tendencias.py` | Repositorio privado `fill-game-marketing` (Q-MK-7), carpeta `tendencias/`. Clon en el Spark: `C:\AI\marketing\fill-game-marketing\`. Lee y escribe los datos en `C:\AI\marketing\tendencias\` (o en la carpeta de la variable `TINTALU_TENDENCIAS`) | Solo usa la biblioteca estándar de Python. Los datos son de terceros y no entran en git |
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
   cd C:\AI\marketing\fill-game-marketing\tendencias
   python tendencias.py importar C:\AI\marketing\tendencias\semilla-AAAA-SS.json --visto AAAA-MM-DD
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

## Cómo se pone en marcha cada semana

- **Tarea programada de Claude en el Spark:** `tintalu-tendencias-lunes`, los lunes a las 9:00 (hora local del equipo). La creó Spark el 2026-10-31 con el permiso del dueño. Sigue esta receta con el navegador integrado.
- **Dónde entrega:**
  - el informe va a la rama `spark/marketing-tendencias`, en su propio worktree;
  - la línea de estado, a `spark-estado.md`.
- **Cuándo corre:** solo con la app abierta; si estaba cerrada, corre al abrirla.
- **Se para** creando `C:\AI\marketing\tendencias\PAUSA`, o desactivándola en la sección «Scheduled» de la app.

## Versión 2 (2026-11-05): cifras reales y más géneros

El dueño pidió cifras de lo que funciona, no solo longevidad, y mirar también los juegos parecidos. Primera pasada: `2026-45-ampliado.md`.

- **Alcance real en Meta:**
  - Se busca con `country=ES`. De los anuncios servidos en la UE, Meta publica el alcance (personas distintas) con el desglose por país, edad y sexo, en «See ad details» › «Transparency by location».
  - En BR y MX ese dato no existe.
  - Los anuncios agrupados («N ads use this creative») solo dan el alcance abriendo cada versión por su ID (`/ads/library/?id=…`).
  - El script guarda `alcance_ue`, `ue_mujeres`, `ue_edad`, `ue_paises` y `genero`, y el informe lo saca en «Cifras medidas».
- **TikTok Top Ads:**
  - Con la sesión de TikTok for Business que dejó abierta el dueño en el navegador integrado; yo no la abro ni meto credenciales.
  - Se busca por palabra en Brasil (`sort puzzle`, `puzzle game`, `water sort`, `screw`, `jam`, `match 3d`, `wool`) y se leen el número de «me gusta», el presupuesto y el objetivo de la respuesta de la propia página.
  - El script guarda `likes`, `presupuesto` y `texto`.
  - Si la sesión caducó, se anota y se sigue.
- **Géneros añadidos a Meta:** `magic sort`, `screw jam`, `puzzle game` (que trae bloques y tornillos: Block Blast, Ivy Color Block, Take Off Bolts), además de los de siempre.
- **YouTube:** la pestaña tiene que estar en primer plano (`tabs_select`). En segundo plano la página sale en blanco, que fue el fallo del 2026-11-02.
- **Pendiente:**
  - Google Ads Transparency, entrando por la página de cada anunciante;
  - Instagram, que pide sesión;
  - ver los vídeos de TikTok, no solo su texto.

## Límites de la versión 1

- **Sin las API oficiales.** La API de Meta Ad Library pide verificar la identidad del dueño. La de datos de YouTube pide una clave del dueño. Se pasará a ellas en la versión 2 si hacen falta.
- **Sin cifras de rendimiento de terceros:** Meta no publica impresiones de anuncios comerciales. La longevidad y las variantes son la mejor señal pública. Las cifras de AppMagic o Sensor Tower irían [NV].
- **El audio de los anuncios de terceros no se analiza:** la vista va sin sonido.
