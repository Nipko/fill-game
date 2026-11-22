# Encargo al equipo Mac: probar la 1.1 en el iPhone (2026-11-22)

El dueño, en el chat con el integrador: «envia a mac lo necesario con todo lo actualizado para probar».

## 1. Qué cambió desde tu última entrega

- **Tu trabajo de iOS ya está integrado.** `mac/m2-sistema` y `mac/m3-storekit` (Firebase, StoreKit 2, anuncios de Google, el aviso ATT y la verificación de compras del App Store en el servidor) entraron en `integracion-fase1-fase2` y en `main` junto con toda la 1.1. En `strings.csv`, el único conflicto fueron las filas `att_*`, que quedaron en su orden.
- **Lo nuevo de la 1.1 que no has visto** (el detalle, en `docs/dev/decisiones.md`):
  - el Mapa v2 (D-069 y sus añadidos hasta el 2026-11-17): Capi rema, la tripulación (tucán en el 21 y tucuxi en el 51), los pueblos, el zoom, los rótulos y las placas;
  - la curva 2 de los niveles 301-700, con el Aguapé y la Lancha (D-068);
  - la Liga del río con su estreno y la fanfarria (Q-LD-3);
  - el marco de país del día 30 en el avatar (Q-M6);
  - el segundo gesto de los miquitos (F13);
  - la Marea que saca del atasco y solo se ofrece si sirve (añadido de D-014 del 2026-11-17);
  - los tres audios nuevos de Gemini, aceptados por el dueño;
  - IL2CPP «Faster (smaller) builds» y el tope de densidad del arte (D-060). Este punto lo aplica `ProjectSetup` en Android: comprueba si conviene igual en iOS y dilo en tu entrega.
- **Lo probado en Android** (S23 Ultra, S25 Ultra y Moto G23): las hojas `docs/dev/traspaso/pruebas-telefono-2026-11-1*.md` y `pruebas-telefono-2026-11-22-moto-g23-r3.md`.

## 2. Qué hacer

1. `git fetch` y una rama `mac/prueba-1.1` desde `origin/integracion-fase1-fase2`. Verifica antes de nada:
   - `dotnet build dotnet/AmazonFill.slnx -warnaserror`;
   - `dotnet test`;
   - EditMode y PlayMode en la Mac (`scripts/unity-test.ps1 … -Runner batchmode`, como en M1);
   - `ApplyIosDefaults` y `ValidateIos`.

   Apunta los totales.
2. Exporta y compila para el iPhone como en `mac-setup.md` §6 (`BuildIos.ExportXcode`, `xcodebuild` y `devicectl`), con el `GoogleService-Info.plist` de desarrollo que ya tienes. Firma con la cuenta de Parallext, LLC si ya está activa; si no, con el Apple ID gratuito.
3. **Prueba en el iPhone 16 Pro Max** con el dueño. Usa los extras de desarrollo por variables de entorno (`af_level`, `af_max_level`, `af_league`, `af_league_fake`, `af_logbook_day`, `af_capture_autoplay`; ver `docs/dev/prototipo-v0.md`):
   - **Marea:** en el 66, la secuencia `3>6 4>3 7>4 0>5 1>5 2>0 2>1 5>2 1>5 2>1 0>7 1>2`. El modal «No hay más vertidos» sale tras el 12.º. «Deshacer · Marea» deja jugar, y repetir `1>2` lo vuelve a sacar con un uso menos;
   - **los tres audios, en el juego:** el Aguapé en el 303 y la Lancha en el 363 (con `af_capture_autoplay`), y la fanfarria con el estreno de la Liga: `af_max_level 45`, `af_league true` y `af_league_fake true`, al entrar al inicio. Recuerda que el modo de captura no guarda el perfil ni saca el estreno;
   - **el Mapa v2:**
     - el viaje del 40 y del 80, con zoom ×6 y el acercamiento al llegar;
     - el tucán (21) y el tucuxi (51), con su primer plano, por la ruta del inicio y desde la pausa;
     - en el 71, el 79 y el 521, los rótulos, que no tapen la ventanita de casa, y una sola placa entera;
     - «Mundo» justo al acabar el viaje del 10;
   - **el marco de país:** `af_max_level 57` y `af_logbook_day 30`;
   - **lo propio de iOS con la 1.1:**
     - la Dynamic Island y la zona segura en el Mapa v2 y en el estreno de la Liga;
     - los hápticos al sellar;
     - «Reducir movimiento» en el viaje de Capi (sin zoom ni acercamiento);
     - Dynamic Type;
     - el aviso ATT y los anuncios de prueba;
     - una compra de prueba con StoreKit (sandbox o el `.storekit` local);
   - **rendimiento y memoria:** 60 fps en los tableros de 3 filas del 386; la memoria del Mapa en 3 vueltas (Xcode › Debug Navigator), sin que crezca tras la primera.
4. **Entrega** como siempre (`mac-ios.md` §5):
   - la hoja `docs/dev/traspaso/pruebas-iphone-2026-11-XX-1.1.md`;
   - el estado al día en `mac-estado.md`;
   - los arreglos que haga falta en la misma rama, con sus tests;
   - push de `mac/prueba-1.1` sin force.

   El escritorio integra.

## 3. Lo que sigue siendo del dueño

- la cuenta de Apple Developer de Parallext, LLC y la app en App Store Connect;
- la app de iOS en AdMob;
- el `GoogleService-Info.plist` de producción;
- la capacidad App Attest.

Si algo de esto falta para una prueba, anótala como «sin probar» y sigue.

## 4. Frase para pegar en Claude Code de la Mac

> Lee `docs/dev/traspaso/encargo-mac-prueba-1.1.md` y `CLAUDE.md`. Eres el equipo Mac de Tintalú (D-067). Haz `git fetch`, crea `mac/prueba-1.1` desde `origin/integracion-fase1-fase2`, verifica, compila para el iPhone y prueba la 1.1 conmigo siguiendo §2. Anota la hoja y el estado, y haz push de la rama.
