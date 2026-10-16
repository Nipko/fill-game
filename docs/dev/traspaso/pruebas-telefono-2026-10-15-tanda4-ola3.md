# Prueba de la ola 3 de la tanda 4 en el teléfono (2026-10-15): resultados para el escritorio

El portátil compiló `integracion-fase1-fase2` en `5ead50d` (ola 3 cerrada en `1c4f7f9`), instaló el `dev.apk` en el SM-S938B por depuración inalámbrica y siguió el «QUÉ PROBAR EN EL TELÉFONO» de `1c4f7f9`.

**Resultado: casi todo cumple.**
- Cumplen:
  - los arreglos de la ola 1 (Atrás en frío, Ajustes abierta al cambiar de idioma y los precios de D-025);
  - la línea de fruta;
  - el inicio con el sitio;
  - la victoria con vista;
  - las tarjetas de los hitos;
  - la fila de TiTino;
  - en-XA;
  - la regresión.
- **Hay dos defectos, los dos al 130 %:**
  - la Balsa de rescate desaparece del modal Aguas estancadas;
  - «Reiniciar» se parte a media palabra en el HUD.
- Además hay cuatro observaciones (al final).

Las capturas, los vídeos y los logcat están en el portátil, en `Logs/device/tanda4o3/` (fuera de git).

## Verificación (portátil ARM64)

| Comando | Resultado |
|---|---|
| `dotnet build dotnet/AmazonFill.slnx -warnaserror` | 0 avisos, 0 errores |
| `dotnet test dotnet/AmazonFill.slnx` | 975 superados, 6 omitidos (981) |
| `python tools/validate_schema.py` | 103/103 PASS |
| `unity run … -executeMethod AF.EditorTools.BuildDev.BuildAndroidDev` | Código 0: `dev.apk` de 66,34 MB |
| `adb install -r Builds/Android/dev.apk` (inalámbrico) | `Success` |

Notas:
- **Los tests de Unity no se lanzaron en el portátil**, porque tumban el adb del teléfono. El escritorio da EditMode 2.504 y PlayMode 699 en `1c4f7f9`.
- **El build deja dos cambios en el árbol mientras dura**, que desaparecen al terminar: `ProjectSettings.asset` (`preloadedAssets`) y `Assets/Resources/PerformanceTestRun*.json`. Al acabar, el árbol queda limpio.
- Las partidas se jugaron con las soluciones A* de `Logs/validacion/soluciones.json`. Los packs 1-200 no cambian.

## Resultados

### 1 · Arreglos de la ola 1 (FXT, D-025): cumple

- **Atrás tras un arranque en frío** (`force-stop`; luego ganar el 2 e ir al inicio):
  - cierra Ajustes a la primera, en 2 de 2 arranques (`settings_closed`);
  - el defecto 1 de la prueba de la ola 1 queda resuelto.
- **Elegir un idioma deja Ajustes abierta:**
  - con en-XA salen `loc` (`source: saved`) y una nueva línea `settings` con `language: en-XA`, la pantalla en en-XA y la opción marcada, sin `settings_closed`;
  - lo mismo al volver a es-419;
  - después, Atrás la cierra.
- **Precios (D-025)** con el teléfono en en-US:
  - «Puñado de semillas 1,000 · COP 3,900» y «Balsa de rescate COP 9,900», con el mismo formato que las cifras;
  - la Compra simulada da «COP 3,900».
- Archivos: `h_precio.jpg` y `h_ajustes.jpg`.

### 2 · Línea de fruta (F1F2, F3): cumple

- **Cada victoria lleva su línea** (`fruit_line`), con `k` correlativo:
  - del 1 al 19, `acai.01` … `acai.19`;
  - el 21 empieza `agua_tapajos.01`, y el 41, `urucum.01`;
  - el 63 lleva `agua_negro.03`; el 86, `cupuacu.06`; el 143, `jenipapo.03`; el 183, `tapereba.03`.
- **En las victorias con llegada sale la llegada, no la fruta:** en el 20, el 90, el 100 y el 110 hay `arrival` y no `fruit_line`.
- En pantalla, «Dato del río · Açaí 5/20» y el texto bajo los botones.
- **«Siguiente» siempre en el mismo sitio:** `y = 1392` en las 20 victorias de la regresión, con fruta o con llegada.
- Al pulsar Siguiente sale `river_fact_shown` con el `fact_id` de la línea. Su `dismissed_ms` (1,6-2,7 s) es el del guion automático, no el de una persona.
- No he podido comprobar «sin sonido» por adb.

### 3 · Inicio con el sitio (M2.1): cumple, con la observación 1

- **En el 58** (perfil limpio, ganar el 57 y pulsar Inicio):
  - fondo de Manaos;
  - «Faltan 3 niveles para Anavilhanas»;
  - la ventanita de casa con «Casa · a unos 1,480 km»;
  - ids `stop_remaining` y `home_window` en `AF_LAYOUT lobby`.
- **Con 0 canoas** (`--ei af_canoes 0`), el inicio sigue enseñando la ventanita. Al pulsar Jugar sale Sin canoas, y con él encima la ventanita desaparece; al cerrarlo vuelve. Es el comportamiento que documenta `prototipo-v0.md` («ninguno de los dos con Sin canoas…»).
- Archivos: `h_inicio.jpg` y `h_canoas0.jpg`.

### 4 · Victoria con vista (M2.2): cumple, con la observación 2

- **En el 86** (3 filas, Trapecio), y en el 90 y el 110 (llegadas), el tablero desaparece y queda el fondo bajo el panel de victoria.
- **En el 90 el fondo ya es Leticia al atardecer.** El 91 carga con `change: same`; el 111, con Puerto Nariño y también `same`; el 87, con `same`.
- Archivo: `h_vista.jpg`.

### 5 · Tarjetas de los hitos (M2.5): cumple

| Nivel | Texto | `hito` | Silueta |
|---|---|---|---|
| 40 | «Si lo superas, llegas a Encuentro de las Aguas» | `encontro` | `code` |
| 60 | «Si lo superas, llegas a Anavilhanas» | `anavilhanas` | `shape` |

En las dos, `hito_omitted: false` e id `level_card_hito` en el layout. Archivo: `h_hitos.jpg`.

### 6 · TiTino (TI3): cumple

La fila «Mensajes de TiTino» sale en Ajustes (id `sloth_messages`, activa por defecto). En en-XA es «[Méñsájés dé TíTíñó ······]».

### 7 · en-XA y 130 % (L4): cumple en en-XA; dos defectos al 130 %

- **En-XA** (`--es af_lang en-XA`, `loc` con `source: dev`). No se sale ningún texto en:
  - la bandeja: «[Déshácér ··]» y «[+1 çúíá ··]», ya dentro de sus botones, frente al hallazgo de la ola 1;
  - la tarjeta del 40 con el hito;
  - Aguas estancadas, la Compra simulada y la victoria (con fruta y con llegada);
  - el aviso del miquito del 5;
  - el inicio.
- **El aviso de Capi del 8 no se ve en la captura** (a 0,8 s de `ready`), así que no está comprobado.
- **Al 130 %** (`--ei af_font_scale 130`): la tarjeta, la victoria, la Compra simulada, el inicio y el aviso del miquito caben. Hay dos defectos, abajo.
- Archivos: `h_enxa.jpg` y `h_f130.jpg`.

### 8 · Regresión: cumple

- **Partida automática con la solución A*:** el 1-12 seguidos con «Siguiente» (perfil limpio), y el 19-21, 41, 63, 100, 143 y 183.
- **20/20 ganados**, con el tablero idéntico al pack, 0 rechazos, 0 Aguas estancadas falsas y 0 avisos `step_budget`.
- Banner en el 11 y el 41.
- Tarjeta previa en el 63, 100, 143 y 183; el 183 sigue con `bite_demo: animated`.
- 0 excepciones ni líneas `E Unity` en los cuatro logcat.

## Defectos

1. **Al 130 % la Balsa de rescate desaparece de Aguas estancadas.**
   - **Qué pasa:** en el 40 con 0 semillas, al 100 % y en en-XA salen las dos ofertas, Puñado y Balsa. Al 130 % solo queda Puñado:
     - `AF_LAYOUT stuck` trae `undo_modal`, `rescue`, `handful` y `give_up`, sin `balsa`;
     - también falta la línea «Todo para seguir: semillas, una cuia y canoas infinitas».
   - **Esperado:** el comentario de `StuckModal` dice que Puñado y Balsa van una debajo de otra si a media anchura no caben. Ninguna oferta debería desaparecer.
   - Archivo: `f130_estancadas_40.png`.
2. **Al 130 % «Reiniciar» se parte a media palabra en el HUD:** sale «Reinici / ar» bajo el icono, en el 5, el 8 y el 40 (`f130_aviso_8.png`, `f130_tablero_40.png`). En en-XA, «[Réiníçíár ···]» parte entre palabras y no se ve mal.

## Observaciones

1. **Para el dueño: la ventanita de casa con 0 canoas.** El QUÉ PROBAR dice que con 0 canoas desaparece, pero solo se va con Sin canoas encima, como dice la sonda. ¿Debe ocultarse también en el inicio con 0 canoas?
2. **Para el dueño: la victoria con vista enseña poco sitio.** Con el tablero fuera, la silueta queda en la banda del horizonte, a la altura del HUD. Debajo hasta el panel solo hay agua y orilla lisas (`h_vista.jpg`). Va con el hallazgo de la ola 2 (silueta invisible con 3 filas).
3. **Para el escritorio: en en-XA, el nombre del miquito sale sin pseudolocalizar.** La tarjeta «¡Miquito a la vista!» del 5 dice «squirrel monkey», sin corchetes ni acentos; en español dice «mono ardilla». Ese texto no pasa por `Loc`, o sale de otra tabla.
4. **El teléfono:** almacenamiento al 100 % (1,5 GB libres).

## Seguimiento del escritorio (FX5, ola 4)

- **Defecto 1, causa sin confirmar (hipótesis):** en el Editor, con perfil limpio, la Balsa sale en el 40 con 0 semillas al 100 % y al 130 %, en es-419 y en-XA (`Fx5Tests.Stuck40_NoSeeds_HandfulAndBalsaWithItsLine`), y el modal no depende de la escala de texto para ofrecerla. La explicación más probable es el tope diario de la Balsa (04 §6.2: `af_rescue_daily_cap` = 1, 08 §9): si la pasada del 130 % fue otro bloqueo del mismo día sin borrar los datos, la tienda ya no la ofrece. Pero no encaja del todo: con el tope, la segunda de las pasadas del 100 % y de en-XA también la habría perdido, salvo que entre ellas se hiciera `pm clear` y antes del 130 % no. Ningún logcat de esta prueba lo confirma.
- **Qué probar en el teléfono (próxima prueba):**
  - `adb shell pm clear com.amazonfill.dev` justo antes de la pasada del 130 %;
  - abrir el 40 con 0 semillas y la fuente al 130 % (`--ei af_level 40 --ei af_seeds 0 --ei af_font_scale 130`) y bloquearse;
  - leer en `AF_STATE stuck_modal` los campos `balsa_ok`, `balsa_shown_today` y `balsa_hidden`, y en `AF_LAYOUT stuck` los ids `balsa` y `balsa_note`.
- **Cómo leerlo:**
  - si con perfil limpio salen `balsa` y `balsa_note` (`balsa_ok: true`, `balsa_shown_today: 1`), el defecto queda resuelto, y la causa era el tope. Hay que avisar al dueño de que en el juego real la Balsa sale solo una vez al día (04 §6.2), para que no se tome por un defecto;
  - si con perfil limpio falta la Balsa, el defecto sigue abierto: anotar la línea `AF_STATE stuck_modal` entera.
