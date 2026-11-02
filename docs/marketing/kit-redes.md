# Kit de redes de Tintalú (S3 de D-066)

Escrito por Spark el 2026-11-01.
- Encargo: `docs/dev/traspaso/spark-marketing.md` §4.
- Estrategia: `estrategia.md`.
- El arte final está en `kit/`, con su origen en `kit/manifest.json`.
- Lo genera `kit/generar_kit.py`, del repositorio `fill-game-marketing`. Las plantillas están en `fabrica/plantillas.py` del mismo repositorio. Se regenera en el Spark con:

```
C:\AI\ComfyUI_windows_portable\python_embeded\python.exe kit\generar_kit.py --juego <repo del juego>
```

Convención: [P] es una propuesta a calibrar; [NV], no verificado; [dueño], espera su respuesta.

## 1. Cuentas

- **Nombre:** `@tintalugame` en TikTok, Instagram, YouTube y Facebook. Donde no quepa, `@tintalu.game` (Q-MK-2).
- **Correo de las cuentas:** `support@tintalu.com`, con la verificación en dos pasos. **Las crea el dueño.**
- **Nombre visible:** «Tintalú», con tilde. La tilde está en el logotipo y en el juego. En el texto de búsqueda se usa «Tintalu», como la ficha de Play.
- **Categoría** (donde la pidan): Videojuego o App.
- **Las cuentas no piden datos a nadie** (§0.6).

## 2. Imagen

| Pieza | Archivo | Medida | Notas |
|---|---|---|---|
| Foto de perfil, en todas las redes | `kit/perfil/perfil-1080.png` | 1080 × 1080 | El icono v2 (D-045). El recorte circular de las redes no toca la cuia ni el río, que están dentro de la zona segura del icono adaptativo |
| Banner de YouTube | `kit/banners/youtube-2560x1440.png` | 2560 × 1440 | El logotipo cabe en la franja visible en todos los dispositivos (1546 × 423, centrada) |
| Portada de Facebook | `kit/banners/facebook-1640x624.png` | 1640 × 624 | El logotipo, en el centro, que se ve en móvil y en escritorio |
| Cabecera de X | `kit/banners/x-1500x500.png` | 1500 × 500 | Para cuando se abra X (encargo §2: más adelante) |
| Tarjeta final | `kit/tarjeta-final/<idioma>-<9x16, 1x1 o 16x9>.png` | 1080 × 1920, 1080 × 1080 y 1920 × 1080 | 9 archivos: el logotipo vertical, «Disponible en Google Play» y «Búscalo como "Tintalu"», en es-419, pt-BR y en |
| Portada de carrusel | `kit/carrusel/portada-<idioma>.png` | 1080 × 1350 (4:5) | La plantilla: cada carrusel cambia el título y el fondo |
| Guías de zona segura | `kit/plantillas/zona-segura-<formato>.png` | por formato | Solo para revisar; no se publican |
| Muestras | `kit/plantillas/muestra-9x16-<idioma>.png` y `muestra-9x16-guia.png` | 1080 × 1920 | Rótulo de gancho y subtítulo sobre una toma real del nivel 42 |

**Fondos de IA:** son los de la ficha (Z-Image Turbo, Apache 2.0, D-065), solo de paisaje. Sin personas ni gameplay inventado (§0.1).

**La insignia oficial «Disponible en Google Play»** no se usa todavía. Hay que descargarla de Google y cumplir sus normas de marca, y la descarga pide el permiso del dueño. Hasta entonces, la tarjeta final lleva el texto en Baloo 2. **Q-MK-S3-1** (recomendada: descargarla y usarla en la tarjeta final, con el permiso del dueño).

## 3. Paleta y tipografía

Todo sale de `docs/art/direccion-visual-v2.md` y de la identidad v2:

| Token | Color | Uso en redes |
|---|---|---|
| Crema | #FFF4DC | Texto de rótulos, subtítulos y tarjeta final |
| Madera de la placa | #5C3A1E, con canto #3C2412 y filete #8C6034 | Placa del gancho, como en la ficha (concepto A) |
| Petróleo | #14444A / panel #142C36 | Velos sobre el fondo y fondo de la tarjeta final |
| Tinta | #102228 | Contorno de los subtítulos |
| Açaí, taperebá y aguaje | #6A2C91, #F2B705 y #EF7D1A | Acentos, solo cuando hablan de esa fruta |

- **Tipografía:** Baloo 2 ExtraBold (OFL), la del juego, para todo lo que va en pantalla. Nunca serifas (dirección visual, regla 7).
- **Sin papel, acuarela ni grano.** Vidrio, madera y luz de juego (D-003).

## 4. Plantillas de vídeo

Las aplica `fabrica/montar.py`. Las medidas están en `fabrica/plantillas.py` (`FORMATOS`).

| Formato | Medida | Zona segura (arriba, derecha, abajo, izquierda) | Dónde |
|---|---|---|---|
| 9:16 | 1080 × 1920 | 220, 160, 420, 64 px [NV] | TikTok, Reels, Shorts y Facebook Reels: unión de sus interfaces |
| 1:1 | 1080 × 1080 | 60 en cada lado | Feed de Instagram y Facebook |
| 4:5 | 1080 × 1350 | 60 en cada lado | Carruseles |
| 16:9 | 1920 × 1080 | 96 en cada lado | YouTube (vídeo largo y el de la ficha) |

Las medidas de 9:16 son orientativas. Se comprueban con una captura de cada red en la primera semana de publicación, y se ajustan en `FORMATOS`.

- **Estructura de una pieza:**
  1. **0-2 s:** el juego en movimiento desde el primer fotograma (S1, conclusión 9). Si hay gancho, va en la placa de madera, arriba de la zona segura, sin tapar las cuias.
  2. **El cuerpo:** la toma real del modo de captura (E1) con la pantalla del juego entera sobre su copia desenfocada. Nunca se recorta el tablero.
  3. **Los subtítulos:** van quemados, en crema con contorno de tinta, en la parte baja de la zona segura. Una frase corta cada vez.
  4. **El final:** 1,5-2 s de tarjeta final, salvo en los bucles de *satisfying*. Esos van sin tarjeta, para que el bucle sea limpio, y la llamada va en la descripción.
- **Audio:**
  - el sonido del juego (los efectos y la música, todo CC0 o propio; `Assets/_Project/Audio/audio_manifest.json`), sincronizado con las líneas `AF_CAPTURE`;
  - a −14 LUFS;
  - nunca sonidos «virales» con derechos (§0.5);
  - la voz en off, con TTS neutra (Q-MK-6), solo cuando haya un modelo con licencia comercial instalado (S0).
- **Etiqueta de IA:** si una pieza lleva un plano de IA realista (un fondo, la pulpa de una fruta), se marca con la etiqueta de contenido sintético de la red (§0.4). Los fondos ilustrados de estilo juego no son realistas, pero en la duda se etiqueta.

## 5. Biografías

Una cuenta por red para los tres idiomas (Q-MK-S2-1), así que las biografías van en español y portugués, con el inglés solo donde cabe.

| Red | Texto | Caracteres |
|---|---|---|
| TikTok (≤ 80) | Puzle de ordenar agua en el Amazonas · Jogo de classificar água | 63 |
| Instagram (≤ 150) | Ordena el agua del río por colores de frutas amazónicas.<br>Classifique a água pelas cores das frutas amazônicas.<br>Sin wifi · Sem wi-fi · Offline | 141 |
| Facebook, «Información» (≤ 255) | Tintalú es un puzle de ordenar agua: vierte, tiñe el río con frutas amazónicas y remonta el Amazonas a tu ritmo. Tintalú é um jogo de classificar água: despeje, tinja o rio com frutas amazônicas e suba o Amazonas no seu ritmo. | 226 |
| YouTube, descripción del canal | La descripción corta de la ficha en los tres idiomas (es-419, pt-BR y en, `docs/store-listing/<idioma>/ficha.md` §2), seguida de `https://tintalu.com/` | — |

Las descripciones cortas de la ficha son canon (glosario §1.3), así que las de YouTube se copian tal cual. Las demás biografías son [P] y pueden cambiar sin tocar la ficha. Sin «#1», «el mejor», «gratis» ni «sin anuncios» (§0.7). «Sin wifi» es cierto: el juego se juega sin conexión (ficha §2).

**Enlace de la biografía**, uno por red, con UTM (E4):

```
https://play.google.com/store/apps/details?id=com.parallext.tintalu&referrer=utm_source%3D<red>%26utm_medium%3Dorganic%26utm_campaign%3Dbio
```

- `<red>` es `tiktok`, `instagram`, `youtube` o `facebook`.
- Que Play Console o Firebase lean ese *referrer* lo comprueba el escritorio (E3) [NV].

## 6. Tono

- **Cálido, curioso y del río.** Se habla como quien enseña un sitio que quiere: «Mira cómo se tiñe», «¿Ves la jugada?», «El açaí tiñe de morado».
- **Sin gritos:** nada de «¡¡INCREÍBLE!!», mayúsculas sostenidas ni cuentas atrás falsas.
- **Honesto:** si un tablero es difícil, se dice que es difícil y se enseña que tiene solución. Nada de «solo el 1 % lo resuelve» sin la cifra de Firebase (encargo §2, gancho 3).
- **Tuteo** en español latino neutro (como la ficha) y «você» en portugués.
- **Emojis:** como mucho uno por texto, y nunca en lugar de una palabra.
- **Respuestas a la comunidad:** las del encargo §7 (solo las preguntas frecuentes con textos aprobados; lo demás, a la cola del dueño).

## 7. Hashtags

De 3 a 5 por pieza: la marca, el género y uno del pilar. El informe semanal (`tendencias/`) los pone al día.

| Grupo | es-419 | pt-BR | en |
|---|---|---|---|
| Marca (siempre) | #tintalu | #tintalu | #tintalu |
| Género | #watersort #juegosdepuzzle #puzzle | #watersort #jogosdepuzzle #quebracabeça | #watersort #sortpuzzle #puzzlegame |
| Satisfying | #satisfying #asmr | #satisfatório #asmr | #satisfying #oddlysatisfying |
| Reto | #reto #acertijo | #desafio | #puzzlechallenge |
| Viaje | #amazonas #amazonia #rioamazonas | #amazônia #rioamazonas | #amazon #amazonrainforest |
| Frutas | #acai #camucamu #frutasamazonicas | #açaí #cupuaçu #frutasamazônicas | #acai #amazonfruits |

- **Sin hashtags de otros juegos** (#magicsort…): sería aprovecharse de una marca ajena.
- **Sin hashtags infantiles** (§0.6).

## 8. Lo que falta

| Qué | Quién |
|---|---|
| Crear las cuentas y subir la foto de perfil, los banners y las biografías de este kit | dueño (Q-MK-2) |
| La insignia oficial de Google Play (Q-MK-S3-1) | dueño (permiso de descarga) |
| Comprobar las zonas seguras con una captura real de cada red | Spark, en la primera semana de publicación |
| Plantilla de carrusel interior (una imagen por fruta, con el Dato del río) | Spark, con el primer carrusel de la semana 46 |
