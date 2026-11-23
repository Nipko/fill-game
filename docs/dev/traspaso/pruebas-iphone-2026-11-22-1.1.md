# Prueba de la 1.1 en el iPhone (2026-11-22)

Prueba de la 1.1 en el iPhone 16 Pro Max (iOS 26.6) según `encargo-mac-prueba-1.1.md`. La hizo el equipo Mac (D-067) **sin tocar el teléfono a mano**: el dueño pidió que los toques los hiciera el agente, como con `adb` en Android. Para eso se añadió `DevRemoteTaps` (abajo). Lo que necesita al dueño está al final.

- **Código:** rama `mac/prueba-1.1`, desde `origin/integracion-fase1-fase2` en `dadcf7e4`, más los dos cambios de esta hoja.
- **Firma:** Apple ID gratuito (equipo `GVKHL2MJMZ`), porque la cuenta de Parallext, LLC aún no está activa. El perfil caduca el 2026-11-29.
- **Registros:** capturas en `Logs/device-ios/` (fuera de git). Las consolas se leyeron con `devicectl device process launch --console`.

## Verificación

| Comando | Resultado |
|---|---|
| `dotnet build dotnet/AmazonFill.slnx -warnaserror` | 0 errores, 0 avisos |
| `dotnet test dotnet/AmazonFill.slnx` | 1.120: 1.114 pasan, 6 omitidos |
| `ProjectSetup.ApplyIosDefaults` (incluye `ValidateIos`) | `OK (iOS)` |
| EditMode (`-Runner batchmode`) | 5.304: 5.296 pasan, 8 omitidos, 0 fallan (antes y después de los cambios de esta hoja) |
| PlayMode (`-Runner batchmode`) | 2.861: 2.446 pasan, 415 omitidos, 0 fallan (29 min 28 s) |
| `BuildIos.ExportXcode` + `xcodebuild` + `devicectl` | exporta, compila, firma e instala |

## Cambios en la rama

1. **`DevRemoteTaps`** (`Assets/_Project/Scripts/UI/DevRemoteTaps.cs`): el `adb shell input tap` de iOS.
   - Solo existe con `DEVELOPMENT_BUILD` y fuera del Editor: release ni lo compila. Solo arranca en iOS.
   - Cada 0,2 s mira `Documents/af_taps.txt`. Si está, lo lee, lo borra y ejecuta sus líneas: `tap x y`, `drag x1 y1 x2 y2 ms` y `wait ms`.
   - Las coordenadas son las de `AF_LAYOUT` (origen arriba a la izquierda). Pulsa con un ratón virtual del Input System, así que todo lo que va por el EventSystem responde.
   - Escribe `AF_TAP {json}` con cada orden y al acabar.
   - El archivo se copia desde la Mac con `xcrun devicectl device copy to --domain-type appDataContainer --domain-identifier com.amazonfill.dev --destination Documents/af_taps.txt`. A veces `devicectl` sale con error aunque la copia llega, porque el juego borra el archivo enseguida: hay que juzgar por `AF_TAP`.
   - Funcionó en toda la prueba: consentimiento, modal de la Marea, Ajustes con arrastre, Bitácora, avatar y Mapa.
2. **`BuildIos.ExportXcodeSimulator`**: exporta a `Builds/iOS/XcodeSim` con el SDK del simulador y devuelve los Player Settings al terminar.
   - **Hoy no sirve:** las librerías C++ de Firebase solo traen simulador x86_64, su arm64 es la del dispositivo, y el simulador de iOS 27.2 ya no acepta x86_64. En arm64 falla el enlace (`libFirebaseCppCrashlytics.a … building for 'iOS-simulator'`); en x86_64, Xcode no encuentra destino.
   - Se deja por si Firebase saca `xcframework` con simulador arm64. Si el integrador prefiere no tenerlo, se puede quitar sin más.

## Resumen

| Punto del encargo | Resultado | Obs. |
|---|---|---|
| **Marea** en el 66 con la secuencia | **cumple**: el modal sale tras el 12.º vertido con «Deshacer · Marea» (3), `undo_escapes` true. Deja jugar (11 vertidos) y repetir `1>2` lo vuelve a sacar con 2 | — |
| **Aguapé** (303) | **cumple**: `sfx_mech_aguape` en juego automático | — |
| **Lancha** (363) | **cumple**: `sfx_mech_lancha`; ganado en 13 (par 13) | — |
| **Fanfarria del estreno de la Liga** | **cumple**: `sting_league_debut` con `league_debut` (`source` lobby). La Dynamic Island no tapa nada | — |
| **Viaje del 40** | **cumple**: `map_journey` 30→40 en 4.500 ms; «Jugar» listo a 4.734 ms; la cámara se acerca y queda en ×4,75 con «Manaos · faltan 10 niveles» | — |
| **Viaje del 80** | **cumple**: `map_open` con `zoom` 6,00; viaje 70→80 en 4.500 ms; listo a 4.736 ms | — |
| **Tucán (21)** | **a medias**: va a bordo con Rosa por la ruta del inicio (viaje del 10) y desde la pausa. **El primer plano «¡Se une al viaje!» no salió en ninguna captura** (una por segundo, por las dos rutas). En la r2c de Android se juzgó por capturas; aquí no se vio | 3 |
| **Tucuxi (51)** | **sin probar**: necesita un perfil nuevo y el borrado de cuenta falla (obs. 2) | 2 |
| **Rótulos en el 71, el 79 y el 521** | **cumple**: no tapan la ventanita de casa; una sola placa de mundo entera («Mundo 4 · Trapecio · Nivel 81» en el 71 y el 79; ninguna en el 521) | — |
| **«Mundo» al acabar el viaje del 10** | **cumple**: tras `map_play_ready` el Mapa está en Mundo | — |
| **Marco de país** (`af_max_level 57`, `af_logbook_day 30`) | **cumple**: la página 30 da `frame_w03` con la canasta grande; se pone solo (`af_avatar_frame_select`, `auto` true) y «Tu avatar» muestra «Manaos» con la bandera de Brasil | 4 |
| **Zona segura** (Mapa v2 y estreno de la Liga) | **cumple** en todas las capturas | — |
| **60 fps en el 386** (3 filas, 11 cuias) | **cumple**: con `af_bench_morpho 1`, tandas de 600 fotogramas a 58,6-60,0 fps, p95 16,69-16,99 ms. Picos sueltos de 114-247 ms (uno por tanda, como mucho) | — |
| **Memoria del Mapa, 3 vueltas** | **cumple**: `sys_mib` tras cada vuelta 393,5 / 392,3 / 392,5; `unity_reserved_mib` 137,1 las tres veces. No crece tras la primera (tabla abajo) | — |
| **ATT y aviso previo** | **a revisar**: el aviso de Apple salió y alguien tocó «Permitir» (`AF_ATT respuesta=Authorized`). El aviso previo propio seguía visible debajo y el agente no tocó «Continuar» | 1 |
| **Anuncios de prueba** | **sin probar** en esta ronda (en M3 se probaron) | — |
| **Compra de prueba** | **sin probar**: el `.storekit` local no se activa en el iPhone y Sandbox necesita la cuenta de pago (encargo §3) | — |
| **Hápticos, Dynamic Type y «Reducir movimiento»** | **para el dueño**: son ajustes del sistema o se notan con la mano. El agente no puede cambiarlos en el iPhone físico | — |
| **IL2CPP «Faster (smaller) builds» en iOS (D-060)** | **no medido**: iOS sigue en `OptimizeSpeed`, porque `ProjectSetup` solo cambia Android. Ver «D-060 en iOS» | — |

## Observaciones

1. **ATT: ¿se abrió el aviso de Apple sin «Continuar»?**
   - Tras «Aceptar todo» (18+), la sonda dio `att_preprompt`. Seis segundos después, la captura muestra el aviso de Apple encima del aviso previo, que seguía visible.
   - El agente solo tocó «Aceptar todo». `AttPrePromptCard.Continue` oculta la tarjeta antes de pedir el permiso, así que con «Continuar» la tarjeta no se vería debajo.
   - Después llegó `AF_ATT respuesta=Authorized`. Ese toque no fue del agente.
   - **Pendiente:** el dueño confirma si tocó algo. Si no, es un fallo de M3: el aviso de Apple se abre solo. Volver a probarlo exige reinstalar, porque iOS no deja borrar la respuesta del ATT.
2. **El borrado de cuenta falla en iOS de desarrollo.**
   - `deleteAccount: BackendError(IntegrityUnavailable)` y «el servidor no confirmó el borrado de la cuenta: no se borra nada».
   - El juego se porta bien: no borra nada y lo dice.
   - **Causa:** el token de depuración de App Check de esta instalación no está dado de alta en Firebase (`fill-dev-e75a2`). Ya estaba pendiente en `mac-estado.md` (M2).
   - **Pendiente del dueño:** darlo de alta. Lo escribe la consola de Xcode al arrancar (`App Check debug token: '…'`). Cambia con cada instalación nueva.
3. **Primer plano del tucán.** Ver el resumen. Hace falta mirarlo con más capturas o con una sonda `crew_joined` que no dependa de `map_journey`. La r2c ya anotó que esa línea no sale en estas aperturas.
4. **Texto cortado en «Tu avatar».** Bajo «Caimán» sale «Mundo 3 ·» sin lo que sigue. Es menor y del código común: conviene mirar si en Android pasa lo mismo.
5. **El consentimiento sale dos veces en iOS.**
   - En el primer arranque el país es el de la región del teléfono (CO, perfil `co_1581`). El del App Store (US) llega asíncrono y solo vale desde el arranque siguiente (M2).
   - En ese segundo arranque el perfil de consentimiento cambia a `default` (US) y el juego vuelve a pedir las elecciones.
   - Con D-057 es correcto, pero el jugador cuya región no coincide con su tienda ve el consentimiento dos veces.
   - En Android el país de Play también llega en el arranque siguiente; conviene comprobar si allí pasa igual.
   - **Decisión del dueño:** dejarlo así, o esperar al país de la tienda antes de la pantalla de edad, con un tope de tiempo.

## Memoria del Mapa (sonda `AF_PERF mem`, MiB)

Desde el nivel 67 (perfil normal): pausa › «Mapa», 9 s en el Mapa y la flecha atrás. Tres vueltas. Xcode › Debug Navigator no se puede leer por comando, así que se usa la sonda.

| Momento | `sys_mib` | `unity_reserved_mib` | `gfx_mib` | `tex_mib` | `mono_heap_mib` |
|---|---|---|---|---|---|
| Nivel 67 al cargar | 397,5 | 130,7 | 59,9 | 36,5 | 55,5 |
| Vuelta 1, Mapa quieto | 399,3 | 142,3 | 70,1 | 42,6 | 55,6 |
| Vuelta 1, de vuelta al nivel | 393,5 | 137,1 | 64,4 | 37,2 | 55,6 |
| Vuelta 2, Mapa quieto | 400,6 | 140,7 | 69,8 | 42,6 | 55,6 |
| Vuelta 2, de vuelta al nivel | 392,3 | 137,1 | 64,5 | 37,2 | 55,6 |
| Vuelta 3, Mapa quieto | 401,1 | 140,7 | 70,0 | 42,6 | 55,6 |
| Vuelta 3, de vuelta al nivel | 392,5 | 137,1 | 64,5 | 37,2 | 55,6 |

## D-060 en iOS

- En Android, «Faster (smaller) builds» bajó la memoria `Code` porque `libil2cpp.so` se mapea entero (G23: de 101-103 MB a 76,4).
- En iOS el código va dentro de `UnityFramework` y el sistema lo pagina igual. El iPhone 16 Pro Max va holgado: unos 400 MiB en el Mapa, sin crecer.
- **Recomendación [P]:** dejar iOS en `OptimizeSpeed` hasta medir en el iPhone más pequeño que se quiera soportar. Si se cambia, va en `ApplyIosDefaults` con su comprobación en `ValidateIos`, y se mide igual que el G23: tamaño de `UnityFramework`, `phys_footprint` y fps del 386 con las dos opciones.

## Lo que queda para el dueño

1. **ATT** (obs. 1): ¿tocaste algo en el teléfono cuando salió el aviso de Apple?
2. **Hápticos:** en un nivel, al sellar una cuia, ¿se nota el toque?
3. **Dynamic Type:** con Ajustes › Pantalla y brillo › Tamaño del texto al máximo, ¿se lee bien el inicio, la pausa y el Mapa, sin cortes?
4. **«Reducir movimiento»** (Ajustes › Accesibilidad › Movimiento): al ganar el 40, ¿el viaje de Capi va sin zoom ni acercamiento?
5. **Orientación** (pendiente de la integración del 2026-11-07): abriendo con el iPhone tumbado, ¿el juego sigue en vertical?
6. **App Check** (obs. 2): dar de alta el token de depuración de iOS en Firebase. Hace falta para el borrado de cuenta, la Liga real y las compras.
7. **Cuenta de pago:** con ella, la compra en Sandbox y el tucuxi con un perfil nuevo.
