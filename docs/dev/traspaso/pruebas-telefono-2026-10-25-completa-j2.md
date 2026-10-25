# Prueba completa del juego en el teléfono (2026-10-25, jornada 2)

El portátil sigue el encargo de la jornada 2 de `guion-prueba-telefono-completa.md` sobre `integracion-fase1-fase2` en `7e8ac97a`, posterior a `7815342d`, con el SM-S938B por depuración inalámbrica. Esta entrega cubre:
- el bloque 0 (verificación y builds);
- N1 (lo nuevo, con el `dev.apk`);
- N2 (D-052 en el ensayo de release).

Los pendientes de la jornada 1 (los 3 puntos del bloque 3, el bloque 4 salvo el 4.9 y los bloques 5 y 7 a 23) siguen en la jornada 3.

Capturas y logcat en el portátil: `Logs/device/completa-j2/` (fuera de git). El logcat se guarda filtrando `DebugAppCheckProvider`, «debug secret», «debug token», `appcheck:debugtokens` y `eyJ`.

## Resumen

| Bloque | Estado | Observaciones |
|---|---|---|
| 0. Preparación y builds | cumple | — |
| N1.1 Arranque con la marca (D-051) | cumple; falta Reducir movimiento del sistema | — |
| N1.2 El inicio, «el muelle del río» (D-050) | cumple, con 2 observaciones | 1, 2 |
| N1.3 Volver al inicio muchas veces | cumple | — |
| N1.4 Anuncios de prueba de Google (D-049) | cumple, con un incidente | 3 |
| N1.5 D-052 en desarrollo | cumple | — |
| N2. D-052 en el ensayo de release | cumple, con 1 observación | 4 |

## Verificación (bloque 0)

| Comando | Resultado |
|---|---|
| `git pull origin integracion-fase1-fase2` | `7e8ac97a` («Encarga al portátil la jornada 2 de la prueba completa en el teléfono») |
| `dotnet build dotnet/AmazonFill.slnx -warnaserror` | 0 errores |
| `dotnet test dotnet/AmazonFill.slnx` | 996 superados, 6 omitidos (1.002) |
| `python tools/validate_schema.py` | 116/116 PASS |
| `git status --ignored` en `Assets/_LocalSecrets/` | `google-services.json` presente e ignorado (no se abrió) |
| `pwsh -NoProfile -File scripts/unity-build.ps1 -Method Dev` | Código 0, `[AF.BuildDev] OK` en 958 s: **`dev.apk` de 75,98 MB** (72,46 MiB) |
| `ci/check-manifest.ps1 dev.apk -Mode dev` | OK (Play Billing 9.0.0) |
| `ci/check-no-unity-iap.ps1 dev.apk` | OK |
| `bash ci/check-16kb.sh dev.apk` | OK: 19 bibliotecas |
| `pwsh -NoProfile -File scripts/unity-build.ps1 -Method Release -Rehearsal -Apk` | Código 0, `[AF.BuildRelease] OK`: **`rehearsal.aab` de 59,01 MB** (907 s) y **`rehearsal.apk` de 64,52 MB** (43 s) |
| `ci/size-gate.ps1 rehearsal.aab` | OK: 59,01 MB ≤ 80 MB. Crece 3,07 MB frente a los 55,94 por Google Mobile Ads |
| `bash ci/check-16kb.sh`, `ci/check-manifest.ps1 -Mode dev`, `ci/check-release-classes.ps1` y `ci/check-no-unity-iap.ps1` sobre el AAB y el APK del ensayo | OK los cuatro, en los dos artefactos |
| `git status` tras los builds | limpio: `GooglePlayGameSettings.txt` ya no queda modificado |

El aviso `[AF.LocReleaseGate]` sigue contando 1.087 problemas de textos (no bloquea con `com.amazonfill.dev`; está respondido en la hoja de la jornada 1).

**Estado del teléfono:**
- Android 16, 1080 × 2340, densidad 420 (zoom del teléfono, igual que en la jornada 1), fuente 1,0;
- «No molestar» apagado;
- batería al 62 % y 35,9 °C.
- Al empezar, `wm size` tenía un override de 1080 × 1920 que no puso el portátil. Con el sí del dueño se hizo `wm size reset` y quedó en la física.

## N1. Lo nuevo, con el `dev.apk`

1. **Arranque (D-051): cumple.**
   - Cinco arranques en frío (`force-stop` y lanzar):

     | Arranque | `intro_ms` | `load_ms` | `shown_ms` | `cut` | `frames` |
     |---|---|---|---|---|---|
     | 1 (primera apertura) | 1.121 | 1.121 | 649 | true | 3 |
     | 2 | 719 | 719 | 266 | true | 3 |
     | 3 | 729 | 729 | 272 | true | 3 |
     | 4 | 738 | 738 | 273 | true | 3 |
     | 5 | 756 | 756 | 281 | true | 3 |

     `intro_ms` = `load_ms` en todos (la intro no alarga la carga) y `anim_ms` = 650.
   - En las capturas no sale «Made with Unity» ni hay destello blanco. La pantalla del sistema en petróleo, con el icono, empalma con la edad o con el inicio.
   - En la primera apertura, `boot` lleva `sloth: sloth_wake`: TiTino despierta después de la intro.
   - **Sin probar:**
     - Reducir movimiento de Accesibilidad, que lo cambia el dueño;
     - el toque durante la intro, que dura unos 270 ms y no se puede cazar con `adb`.
2. **El inicio, «el muelle del río» (D-050): cumple, con las observaciones 1 y 2.** Capturas `n1/n1-inicio-*`.
   - **Antes del 10** (nivel 1-5): la caja de cuias en lugar de tarjetas (cumple). La línea `lobby` lleva `play`, `settings`, `music`, `sound`, `stop_remaining`, `home_window` y `titino`. **La canoa de Capi con Rosa y el poste «Santarém · 10 levels left» ya se dibujan** (observación 1).
   - **TiTino:** colgado de su rama y con bocadillo («Shall we play? I'll watch from my branch.», «There's a new River Challenge today!», «10 levels to your next Basket»). Nunca habla de compras. Al tocarlo cambia de postura (`sloth_look`, sin sonda propia) y vuelve a sacar el bocadillo; antes del 10 solo tiene un mensaje.
   - **Desde el 10** (max 58, 140 y 701): `voyage_canoe`, `voyage_stop`, `stop_remaining`, `crecida` (el poste con la racha) y `lobby_cards` con `daily_card` y `logbook_card`. Tocar la canoa abre el Mapa (`map_open` `source: lobby`).
   - **El río:** el color del agua cambia con el mundo (café en el 4 y el 140, negro en el 58), con nenúfares, un pez que salta y un ave. Con max 701 (mundo 10): «Play · River Challenge» y sin poste de parada.
   - **Letra al 130 % en es, en y pt-BR:** las tarjetas del día y el letrero de la parada caben, sin textos cortados ni `[clave]` (`n1-inicio-130-*.png`).
   - **Sin probar:** la noche (bloque 15) y el 16:9 (bloque 16).
3. **Volver al inicio muchas veces: cumple.** Tres ciclos de nivel → pausa → salir y de Mapa → atrás (max 20): 4 `lobby`, 3 `map_open` y 3 `map_close`, con 0 `E/Unity`. Los parpadeos y saltos los juzga el dueño a la vista.
4. **Anuncios de prueba de Google (D-049): cumple, con el incidente de la observación 3.**
   - Al arrancar: `[AF.Ads] proveedor: google (bloques de prueba); max: not_configured` (23 arranques). Nunca «produccion».
   - Perfil 18+ con país `US` (simulado): no sale el mensaje de UMP. Su SDK solo escribe su estado (`UserMessagingPlatform`) al decidir el consentimiento. No se pudo probar con Colombia.
   - **Cosecha doble del 11:**
     - el vídeo de prueba de Google con la etiqueta «Test Ad» se ve entero y luego sale «Reward granted» con la X;
     - al cerrarlo: `ad_impression` (AdMob), `af_ad_request` `filled`, `af_ad_shown`, `af_ad_reward` y +20 una sola vez;
     - no se probó cerrarlo antes de tiempo.
   - **Intersticial:** tras el 23, `ad_check` `interstitial_post_level` `allowed`.
   - **Perfil <13** (`pm clear` y edad <13): bloqueado, con 0 líneas del SDK de Google Ads o de UMP del proceso. Solo sale la línea del proveedor, al arrancar y antes de la edad.
   - **Sin probar:** sin red (modo avión, con el dueño).
   - **Nuevo en el flujo:** tras el consentimiento de un perfil limpio, el juego va al inicio y no directo al nivel 1 (en el ensayo sí va al nivel 1).
5. **D-052 en desarrollo: cumple.**
   - El 323 sale con la Matapi y su tejido.
   - El Mapa (max 140) lleva las siluetas de las paradas, las banderas, la ventanita de casa y la placa del mundo.

## N2. D-052 en el ensayo de release: cumple, con 1 observación

Con `rehearsal.apk`, tras desinstalar el `dev.apk` (comparten `com.amazonfill.dev`). Sin sondas: todo por capturas (`n2/`), jugando por coordenadas con las soluciones A*.

1. **Arranque:** sin «Made with Unity». La intro muestra la gota de açaí cayendo en la cuia y **TiTino colgado de su rama**, el arte por código que antes no salía en release (`n2-arranque-2.png`). Después, la edad.
2. **Edad y consentimiento:** iguales que en desarrollo. Tras «Accept all» va directo al nivel 1, con la banda de TiTino.
3. **Del 1 al 10 jugados y ganados:**
   - el dedo guía en el 2 y «Monkey spotted!» en el 5;
   - la Cuia extra en el 8 y la Canasta 10/10 en el 10 (250 → 310 con la Bitácora);
   - la Bitácora con sus tres premios y la página sellada;
   - el primer Mapa con la canoa, la bandera y la casa de TiTino en la vista Cuenca;
   - en el inicio, TiTino con su bocadillo, el río del Tapajós, la canoa y las tarjetas del día;
   - en el 12, **la tropa de miquitos** arriba del tablero.
4. **Sin huecos de arte** en lo visto: ningún rectángulo vacío ni color plano.
5. **Anuncios:** la Cosecha doble del 11 sirve el vídeo de prueba con **«Test Ad»**, y al cerrarlo con la X da +20 (330 → 350). No hay bloqueo.
6. **Logcat del proceso:**
   - 0 líneas `AF_*`;
   - el único error es `E/firebase: clazz`, que es la clase de depuración de App Check que se quita en release (`firebase-cliente.md` lo anuncia);
   - el resto son trazas de Google Play Services (`FlagStore`).
7. **Sin probar:** los extras de desarrollo ignorados en el ensayo (bloque 20).

## Observaciones

1. **Baja. La canoa de Capi y el poste de la próxima parada se dibujan antes del nivel 10** (nivel 1-5, `n1-inicio-pre10-*.png`), aunque no se pueden tocar (no hay `voyage_canoe`). El guion dice «solo desde el nivel 10».
2. **Baja. Al tocar a TiTino antes del 10 repite el mismo mensaje**, porque solo tiene uno. El guion habla del «mensaje siguiente». Con el Reto y la Canasta disponibles sí rota.
3. **Incidente (alta, de la prueba, no del juego). Un toque del jugador automático cayó sobre el intersticial de prueba del 23** y abrió la página del anunciante en Chrome (developers.google.com, AdMob). Pasó porque el jugador seguía vertiendo el 24 mientras el anuncio estaba en pantalla.
   - Es un anuncio de prueba (sin ingresos ni riesgo para la cuenta), pero la regla es no hacer clics propios.
   - Desde entonces, en las pruebas con anuncios reales se va paso a paso, con una captura antes de cada toque.
   - Los recorridos automáticos usarán `--ei af_ads_fill 0/1` (simulado), como pide el guion.
   - Al volver se pulsó Atrás una vez dentro de Chrome. Después se trajo el juego al frente con `am start`.
4. **Media. En el ensayo de release se ve el icono del Mercado** abajo a la izquierda. Al abrirlo dice «Purchases aren't available right now» con los precios en gris. No hay toque mudo, pero según `6914b06f` el icono y el Cántaro deberían estar ocultos sin precios de Play. En el `dev.apk` (max 58 y 140) el icono no salía.
