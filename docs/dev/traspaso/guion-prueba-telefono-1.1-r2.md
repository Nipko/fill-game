# Guion de la prueba en el teléfono: la 1.1, segunda ronda

Lo escribió el integrador el 2026-11-08. Es para el portátil, con el SM-S938B por Wi-Fi: por cable, el teléfono se cae de `adb`.

**Qué se prueba:**

- lo que se arregló después de la primera ronda (`pruebas-telefono-2026-11-07-1.1.md`);
- lo que quedó sin ver allí;
- lo nuevo de la 1.1.

La hoja va en `docs/dev/traspaso/pruebas-telefono-<fecha>-1.1-r2.md`. Capturas y logcat en `Logs/device/v11r2/`.

## 0. Preparación

```
git pull origin integracion-fase1-fase2
pwsh -NoProfile -File scripts/unity-build.ps1 -Method Dev
adb install -r Builds/Android/dev.apk
```

- **En `git log` tienen que estar las integraciones de:**
  - `v11/mapa-j1-memoria`;
  - `v11/mapa-j1-arreglos`;
  - `v11/miquitos-segundo-gesto`;
  - y, si el dueño lo confirma (Q-M6 frente a Q-AV-11), `v11/marco-de-pais`. Si no está, salta el punto 5.
- `<act>` = `com.amazonfill.dev/com.unity3d.player.UnityPlayerGameActivity`.
- **Perfil limpio:** `pm clear` borra el progreso; lo repone `--ei af_max_level N`. Cada compañero de la tripulación se une una sola vez por perfil, así que para repetir algo hay que hacer otro `pm clear`.

## 1. El viaje de Capi (observación 3)

Con `--ei af_max_level 69 --ei af_level 70`, gana el 70 y pulsa «Ver mapa».

- **Duración:** el viaje dura unos 4,5 s. La línea `map_journey` `end` dice `ms` 4500 y `per_buoy_ms` 450.
- **Sin tocar:** «Jugar» sale al llegar la canoa, a unos 4,75 s.
- **Con un toque:** el viaje va ×6 y «Jugar» sale a unos 0,75 s.
- **Gestos:** ¿se ven ahora remar, saludar al pasar un pueblo, el remo en alto y la muestra amarrada? ¿Y la burbuja?
- **Preguntas abiertas de M2-3, que ahora sí se pueden contestar:**
  - ¿marea la cámara que sigue a la canoa?
  - ¿el ritmo de los gestos se siente bien?
  - ¿4,5 s es buen tiempo, o mejor más corto o más largo?

## 2. La tripulación con primer plano (observación 8)

1. **Tucán:** `pm clear` y `--ei af_max_level 21`. Abre el Mapa.
   - La cámara se acerca ×2,5 a la canoa (en unos 0,9 s): sale el tucán, el chip «¡Se une al viaje! · Tucán» y el ♥ de Capi. Luego vuelve suave (en unos 1,5 s).
   - **¿Se distingue ahora el tucán?** Es la pregunta 5 de M2-4.
   - **¿Se ve el remo?**
2. **Tucuxi:** `pm clear` y `--ei af_max_level 50 --ei af_level 51`. Gánalo y pulsa «Ver mapa»; si no hay viaje, abre el Mapa.
   - El mismo primer plano, con el tucuxi pequeño junto a Rosa.
   - **¿Se reconoce que es un tucuxi?**
   - La línea `map_journey` `end` dice `crew_joined` `tucuxi`.
3. **Miquito:** en un viaje sin compañero nuevo (por ejemplo, ganar el 60 con el tucuxi ya a bordo), el miquito saluda desde un árbol, con el primer plano. ¿Se distingue?
4. **Llegada:** gana un nivel terminado en 0 con el tucuxi a bordo. Salta el tucuxi y Rosa solo asoma.
5. **Cambio de mundo en el 40 (sin ver en la primera ronda):** `pm clear` y `--ei af_max_level 39 --ei af_level 40`.
   - Gana el 40. El perfil tiene que llegar al 41 por primera vez.
   - En el plano final del banner, Capi, el tucán y la bandera.
6. **Reducir movimiento:** sin primer plano animado, y el chip igual.

## 3. Rótulos, «Faltan N» y el río recorrido (observaciones 2, 4 y 9)

Abre el Mapa con cada `af_max_level` y haz una captura:

| `af_max_level` | Qué mirar |
|---|---|
| 56 | Anavilhanas, entero y sin la canoa encima; el tramo recorrido y el agua negra |
| 66 y 67 | Coari, entero (antes se leía «Coa») |
| 71 | Leticia con «faltan 10» dentro del rótulo y **sin placa arriba a la izquierda** |
| 96 | Amacayacu, entero |

- **El tramo recorrido** es un tinte del agua con las puntas difuminadas. ¿Ya no se ve raro? ¿Queda algún bloque oscuro de borde recto? En Manaos, el agente avisó de que se ve un degradado, pero no desaparece del todo: mírala.
- **La píldora «Premio del mundo · faltan N»** no pisa «Andes ↑», ni la canoa, ni los pueblos.
- **Placa de respaldo:** en tramos largos de los mundos 7 y 8, todavía puede salir la placa arriba a la izquierda (6 casos de 138). Anota si la ves y con qué `af_max_level`.

## 4. Memoria del Mapa v2 (observación 5): tres vueltas frente a la 1.0

Con `pm clear` y `--ei af_mem_os 1 --ei af_max_level 66`, en OpenGL ES 3:

1. Un minuto quieto en el inicio. Luego `adb shell dumpsys meminfo -d com.amazonfill.dev`.
2. Tres vueltas seguidas, cada una así:
   - abre el Mapa y espera la línea `AF_PERF` `map_settled`;
   - ciérralo y espera `map_after`;
   - un minuto en el inicio;
   - `dumpsys meminfo -d`.
3. Apunta en cada vuelta:
   - de `dumpsys`: TOTAL PSS, `GL mtrack`, `Native Heap` y `Private Other`;
   - de `map_settled` y `map_after`: los `obj_*`, los `map_*`, `mono_heap_mib`, `os_graphics_mib`, `os_native_mib` y `os_private_other_mib`.
4. Repite los pasos 1 a 3 con el Mapa de la 1.0: `pm clear` y lo mismo, más `--ez af_map_v1 true`.

**Criterio:**

- **Se acepta** si en las vueltas 2 y 3 el PSS y `GL mtrack` quedan a ±3 MB de la primera, y `map_after` da `map_tex_kib`, `map_renderers` y `map_verts` a 0, con los mismos `obj_*` en las tres vueltas.
- **Es fuga** si sube más de 3 MB por vuelta.
- Anota lo que el v2 suma frente al de la 1.0.

## 5. El marco de país (Q-M6; solo si `v11/marco-de-pais` está integrado)

1. **El marco de la Bitácora:** `--ei af_max_level 57 --ei af_logbook_day 30`. Abre la libreta, sella la casilla 30 y comprueba:
   - el retrato con el marco de Manaos;
   - «Ver tu avatar»;
   - la fila «Marcos de país».
2. **Todos los marcos:** `--es af_frame_grant all` da los 9.
   - Elige uno, luego «Sin marco».
   - El punto del inicio se apaga al verlos.
3. **Qué mirar:**
   - ¿se lee el marco a 40 dp en el inicio y a 24 dp en la línea de la Liga?
   - ¿la placa con la bandera tapa demasiado al animal?
   - TalkBack dice «… con el marco de …».
4. **Los extras** se vuelven a aplicar en cada arranque: relanza sin ellos para ver lo que pasa después.

## 6. El segundo gesto de los miquitos (F13)

`adb shell am start -S -n <act> --ei af_max_level <N-1> --ei af_level <N>`, en el primer intento, con la tropa en la rama.

| Nivel | Especie y gesto | Comparar con |
|---|---|---|
| 251 | tocón, `duo`: las dos cabezas arriba y el pecho al compás | 252 |
| 361 | pichico, `brincos`: dos brincos en ola | 362 |
| 491 y 631 | maquisapa, `colgado_de_la_cola`: colgado de la cola y meciéndose | 492 y 632 |

- **Qué mirar:**
  - tras la última fruta, toda la tropa hace el gesto durante unos 1,5 s y se va a su hora;
  - nada pisa el HUD;
  - el toque responde enseguida.
- **Sonda:** `adb logcat -s Unity | grep troop_gesture` da `gesture` distinto de `first` y `perch` `branch`.
- **Forzar el gesto:** `--ei af_troop_gesture 2` lo fuerza en cualquier nivel de la especie, y `1` fuerza el gesto de siempre.
- **Reducir movimiento:** un solo miquito, quieto en la pose.
- **Pregunta para el dueño:** ¿se notan? ¿Le valen los gestos?

## 7. Play Games

Solo si el dueño ya dio de alta la huella SHA-1 de la clave de depuración del portátil en la consola de Play Games, para `com.amazonfill.dev`. El inicio de sesión silencioso tiene que entrar, sin `ApiException: 10`.

## 8. Qué devolver

- Cumple o no cumple en 1-7, con su captura o su línea de logcat.
- Las respuestas a las preguntas: la cámara, el ritmo, el tiempo del viaje, el tucán, el tucuxi, el miquito, los gestos de los miquitos y, si está integrado, el marco.
- La tabla de memoria de las tres vueltas, del v2 y de la 1.0.
- Las observaciones numeradas.

No pegues tokens, UID ni URL firmadas.
