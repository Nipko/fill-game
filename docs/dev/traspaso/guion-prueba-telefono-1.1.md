# Guion de la prueba en el teléfono: la 1.1 (F6 de D-068 y el Mapa v2 de D-069)

Lo escribió el integrador el 2026-11-07. Es para el portátil, con el SM-S938B por depuración inalámbrica. En el escritorio el teléfono no está autorizado y el dueño no lo tiene a mano.

**Qué se prueba:** lo que trae la 1.1 en `integracion-fase1-fase2`:

- la curva 2 de 301-700 (D-068, F5);
- el Mapa del río v2 completo (D-069, M2-1 a M2-4);
- el estreno de la Liga y Aguas estancadas solo al repetir;
- la pantalla de borde a borde, la orientación y R8.

El A/B de F6 (`lvl_curve2_enabled` al 50 %) no se prueba aquí: lo configura el dueño en Firebase al publicar. En el teléfono solo se ve el lado por defecto, con la curva 2 encendida.

**Antes de compilar:** en `git log` tiene que estar el commit que integra `v11/mapa-m2-4b-ajustes`. Trae el tucuxi de unos 18 dp, que se une en el 51 (Manaos), y a Rosa que solo asoma (D-069, respuesta del dueño). Si aún no está, haz la prueba igual y salta el punto 2.4.

La hoja va en `docs/dev/traspaso/pruebas-telefono-<fecha>-1.1.md`, con el formato de las jornadas. Capturas y logcat en `Logs/device/v11/`, fuera de git.

## 0. Preparación

```
git pull origin integracion-fase1-fase2
pwsh -NoProfile -File scripts/unity-build.ps1 -Method Dev
adb install -r Builds/Android/dev.apk
adb shell pm clear com.amazonfill.dev
adb logcat -c
```

En otra consola: `adb logcat -v time > Logs/device/v11/logcat.txt`.

La actividad es `com.amazonfill.dev/com.unity3d.player.UnityPlayerGameActivity`. Abajo se escribe `<act>`.

Con el perfil limpio, contesta 18+ y acepta el consentimiento la primera vez.

## 1. La curva 2 (301-700)

Con un perfil nuevo, la curva 2 está encendida por defecto. Arranca cada nivel con:

```
adb shell am start -n <act> --ei af_max_level <N> --ei af_level <N>
```

En cada nivel:

- juégalo hasta ganar o hasta que veas claro cómo funciona;
- haz una captura;
- anota en el logcat el `id` de `AF_STATE`, que debe acabar en `_c2`.

| Nivel | Qué mirar |
|---|---|
| 303 | Estreno del Aguapé: se entiende sin texto, la planta se ve y bloquea lo que debe |
| 347 | Estreno del Aguapé espeso |
| 363 | Estreno de la Lancha: 10 cuias, K 6; se entiende cómo se llena y cuándo zarpa |
| 306, 349 | Retos con el chip «Difícil» |
| 352, 672 | Retos sin chip (se aceptaron como Normal): ¿se sienten fáciles o justos? |
| 380, 560, 700 | Hitos, «Muy difícil». El 380 es el de bot atento más bajo: ¿se siente hito? |
| 374 o 640 | Tablero de 3 filas: nada se pisa ni se sale |
| 323 | Dosis ocultas (Aguas turbias) en Normal |

- **Sensación general:** del 301 al 320 seguidos, ¿la dificultad sube con ritmo, sin muros?
- **En todo el bloque,** 0 líneas `E/Unity`. Las cuias de cada tablero tienen que caber sin tocarse a 360 dp.

## 2. El Mapa del río v2

Abre el Mapa desde el inicio. Los niveles se fijan con `--ei af_max_level <N>`, y cada viaje sale de ganar un nivel terminado en 0 (`--ei af_level <N>`, después Ver mapa).

1. **El río vivo (M2-1), con `af_max_level 66`:**
   - pantalla completa, con corriente, orillas, selva y boyas;
   - el Mapa ocupa unos dos tercios de la pantalla y el conmutador y el Muestrario van en una sola fila;
   - zoom y desplazamiento a 60 fps a ojo, sin tirones.
2. **Pueblos de paso (M2-2):**
   - los rótulos de los pueblos se leen;
   - al tocar un pueblo, su ficha;
   - en la vista Cuenca, las viñetas de las ciudades.
3. **Capi al remo (M2-3):**
   - con `af_max_level 69 --ei af_level 70`, gana el 70 y abre el Mapa;
   - mira el viaje: Capi rema, saluda al pasar un pueblo, levanta el remo y amarra la muestra al llegar;
   - una burbuja sin texto (♪, ♥, !, ? o una cuia), una como mucho;
   - **¿Marea la cámara que sigue a la canoa?** ¿El ritmo de los gestos se siente bien? Las dos son preguntas abiertas del dueño: contesta en la hoja.
   - «Faltan N» va dentro del rótulo de la parada siguiente, en dos líneas, y no en una placa arriba a la izquierda.
4. **La tripulación (M2-4):**
   - **Tucán:** con `af_max_level 21` (el 21 recién ganado), al abrir el Mapa llega y se pone en la proa; sale el chip «¡Se une al viaje! · Tucán» 2 s y Capi hace ♥. **¿Se distingue el tucán (18 dp) junto a Capi y el remo?** Es la pregunta 5 de M2-4.
   - **Tucuxi:** con `af_max_level 51`, llega y nada junto a Rosa, más pequeño que ella. Gana un nivel terminado en 0 para ver la llegada: salta el tucuxi y Rosa solo asoma.
   - **Miquito:** en un viaje sin compañero nuevo, uno saluda desde un árbol de la orilla.
   - **Cambio de mundo:** gana el 40 (`af_max_level 39 --ei af_level 40`). En el plano final del banner salen Capi, el tucán y la bandera.
5. **Reducir movimiento** (Ajustes del juego): sin cámara ni bucles, el gesto y la burbuja quietos 1,2 s y el chip igual.
6. **TalkBack:**
   - los rótulos de los pueblos se leen, con «Faltan N» en la mini cuia de la parada siguiente;
   - al unirse un compañero, se anuncia «Se une al viaje: {especie}»;
   - los gestos no se leen.
7. **Memoria:**
   - con `--ei af_mem_os 1` y el Mapa abierto en reposo, anota la línea `AF_PERF {"event":"mem"` (con `capi_tex_kib` y `crew_tex_kib`) y `adb shell dumpsys meminfo -d com.amazonfill.dev`;
   - **compara con el Mapa de la 1.0:** el v2 suma unos +6 MiB de texturas;
   - repítelo tras cerrar el Mapa, en el inicio: las texturas tienen que soltarse.

## 3. Lo demás de la 1.1

1. **El estreno de la Liga:**
   - con `pm clear` y `--ez af_league true --ez af_league_fake true --ei af_max_level 41`, gana el 42;
   - en el primer momento tranquilo del inicio sale el estreno, una sola vez;
   - no vuelve al relanzar.
2. **Aguas estancadas solo al repetir:**
   - en el primer intento de un nivel, aunque te quedes sin movimientos útiles, no sale el aviso;
   - al repetir un nivel ya ganado, sí.
3. **Borde a borde:**
   - el fondo llega bajo la barra de estado y la de navegación;
   - nada interactivo queda debajo de ellas ni del recorte de la cámara (inicio, nivel, Mapa, Mercado, Ajustes).
4. **Orientación:**
   - en el teléfono, el juego no gira aunque actives la rotación automática y pongas el teléfono de lado;
   - en el arranque, la intro también va en vertical.
   - Si hay una tableta o un plegable a mano, el juego gira en ella con la interfaz en columna. Es opcional: anota si no lo hay.

## 4. R8: humo del ensayo de release

```
pwsh -NoProfile -File scripts/unity-build.ps1 -Method Release -Rehearsal
```

Instala el APK del ensayo (`Builds/Android/`, el que deje el método) y recorre:

- arranque con la intro;
- consentimiento;
- un nivel ganado;
- un anuncio de prueba;
- el Mercado abierto (precios de Play o «no disponible», sin cierre);
- Play Games (inicio de sesión silencioso);
- un aviso programado que llega;
- la Liga abierta;
- Ajustes › Borrar cuenta, solo hasta la pantalla de confirmación, sin confirmar.

**Cumple** si no hay ningún cierre ni `ClassNotFoundException`, `NoSuchMethodError` o `AndroidJavaException` en el logcat. Si sale alguno, copia la línea entera (sin tokens): falta una regla keep.

## 5. Qué devolver

La hoja con:

- cumple o no cumple en 1-4, con su captura o su línea de logcat;
- las respuestas a las preguntas abiertas: si marea la cámara, el ritmo de los gestos, si se distingue el tucán y la sensación de la curva 2;
- las cifras de memoria del Mapa v2 frente a la 1.0;
- las observaciones numeradas.

No pegues tokens, UID ni URL firmadas.
