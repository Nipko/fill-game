# Tintalú — identidad oficial v1

> **Sustituida por la v2 (D-045); historial, no usar.** Se conserva sin modificar como registro de la decisión anterior. La identidad vigente está en [marca oficial v2](../v2/README.md).

**Aprobada por el dueño el 23-10-2026.** Su instrucción: «listo eso, cuadremos todo para el logo oficial sea ese, los iconos y demas», seguida de «version para ios». Esta elección sustituye los bocetos anteriores de cuencos, dos cuias y TiTino como emblema principal. Se conserva el último icono con meandro y río más intenso, no la variante previa de río casi recto.

## Archivos principales

| Uso | Archivo |
|---|---|
| Original aprobado, sin modificar | `source/tintalu-master-aprobado.png` |
| Google Play | `store/google-play-512.png` — 512 × 512, PNG RGBA, sRGB, menos de 1 MB |
| App Store / icono iOS principal | `store/app-store-1024.png` — 1024 × 1024, PNG RGB sin alpha |
| Xcode, iPhone/iPad | `ios/Assets.xcassets/AppIcon.appiconset/` — catálogo Single Size con apariencias normal, oscura y tintada |
| Android adaptativo | `android/layers/` y `android/res/` — primer plano transparente, fondo petróleo y máscara monocroma |
| Android clásico | `android/res/mipmap-*/ic_launcher.png` — 48, 72, 96, 144 y 192 px |
| Logo con nombre | `logo/tintalu-horizontal-fondo-claro.png`, `logo/tintalu-horizontal-fondo-oscuro.png`; equivalentes verticales |
| Web | `web/` — PNG de 16 a 512 px, favicon ICO, apple-touch-icon de 180 px, manifest y ejemplo de head |
| Avatar | `social/avatar-1024.png` |
| Revisión visual | `preview/identidad-y-recortes.png` — montada con los archivos reales, no con una maqueta generativa |
| Verificación | `validation.json`, `manifest-sha256.json` |

El símbolo aprobado es el identificador principal. Las composiciones con el nombre usan **Baloo 2 ExtraBold**, fuente existente del juego, con su licencia OFL adjunta. «Fondo claro» significa letras oscuras para usarlas sobre un fondo claro; «fondo oscuro», letras claras. Ambos PNG tienen transparencia. No confundir estos archivos con el logotipo ilustrado anterior, que queda como boceto histórico.

## Rasgos que deben conservarse

- Una sola cuia de vidrio alargada, cuerpo recto, hombros y cuello corto abierto, con fibras en boca y base.
- Dosis **transversales**: amarillo en base, naranja al centro y púrpura junto al cuello. Nunca franjas longitudinales.
- Meandro ámbar detrás. El río se vuelve púrpura **desde el impacto del chorro hacia abajo**; aguas arriba sigue ámbar.
- Fondo azul petróleo; sin paisaje detallado, animales ni texto dentro del icono.
- Nombre escrito **Tintalú**, con tilde. No añadir ®: el registro de marca sigue aplazado.

No estirar, recolorear el río aguas arriba, invertir las dosis ni generar de nuevo el maestro para cambiar de tamaño. El maestro es una copia idéntica por SHA-256 del archivo aprobado. Los recortes con transparencia y monocromo son adaptaciones técnicas generadas con ImageGen; la exportación de tamaños, color, catálogos y composiciones se realiza de forma determinista con `export.mjs`.

## Integración Android / Unity

Se entrega preparado para integrar, **no instalado**. Mantener el applicationId actual y las decisiones independientes de publicación.

- En Unity, usar `store/google-play-512.png` como icono por defecto/legacy, según las casillas de tamaño del editor.
- Para Android Adaptive, cargar **background-432.png** y **foreground-432.png** en sus respectivas capas; usar **monochrome-432.png** donde el exportador admita la capa temática. No poner la misma imagen en las dos capas.
- Las capas representan 108 × 108 dp a 4×. El símbolo se coloca dentro de un radio de 32 dp, con margen respecto a la zona segura de 66 dp. La lámina de QA simula el viewport central y dos máscaras.
- Para proyectos Android nativos, `android/res/` aporta recursos por densidad y XML para API 26/33. El fragmento de manifest es orientativo, no un archivo completo. **En Unity, no copiarlo a ciegas sobre los recursos generados**: asignar con Player Settings o la automatización del integrador y verificar el APK final.

## Integración iOS / iPadOS

1. En un proyecto Xcode existente, copiar el conjunto `AppIcon.appiconset` dentro de su catálogo `Assets.xcassets`, sin reemplazar el catálogo entero que contiene otros recursos. En un proyecto nuevo se puede incorporar el catálogo suministrado.
2. Seleccionar `AppIcon` como App Icons Source. El esquema usa **Single Size**, 1024 × 1024 para iOS universal; Xcode genera los tamaños necesarios.
3. Incluye `Any` RGB opaco, `Dark` con símbolo transparente y `Tinted` en escala de grises. El sistema gestiona otros tratamientos; no se ofrecen como iconos alternativos seleccionables por el usuario.
4. En Unity, usar el PNG principal en Player Settings de iOS; tras exportar, integrar/verificar las apariencias en Xcode si la versión de Unity no las conserva automáticamente.
5. No redondear las esquinas en los archivos de entrega: el sistema aplica la máscara. Los PNG de `preview/` son ejemplos de recorte y **no deben subirse a las tiendas**.

El catálogo es una entrega raster válida para el flujo de asset catalog documentado por Apple. **No es un proyecto `.icon` de Icon Composer ni una comprobación de efectos Liquid Glass.** La compilación con `actool`/Xcode y la prueba en iPhone quedan para el entorno macOS del integrador; no se han ejecutado en este equipo Windows.

## Web y tamaños pequeños

Copiar `web/` a una ruta de assets y adaptar el prefijo del `head-snippet.html`. No convertirlo automáticamente en una PWA instalable: el manifest usa `display: browser` y no añade service worker. `icon-180.png` es el apple-touch-icon.

Los 16 px conservan la combinación de colores y silueta general; no permiten leer fibras o patrones. A 48/96 px se distinguen cuia, vertido y meandro. Para cabeceras, preferir el logo horizontal y dar espacio alrededor. No usar los archivos de icono de 16/32 px en materiales grandes.

## Procedencia y reproducción

- Ilustración creada con ImageGen mediante iteraciones y aprobada por el dueño; no atribuirla a un artista humano ni declararla marca registrada.
- `prompts.md` conserva los prompts de la última edición del maestro y de las dos adaptaciones.
- Maestro raster de 1254 × 1254. Los logos son PNG; **no se entrega un vector editable ni PSD por capas**. No presentar una conversión SVG que incruste el PNG como vector original.
- Licencia tipográfica: `licenses/OFL-Baloo2.txt`; fuente en el repositorio bajo Git LFS.
- Exportar con Node y `sharp` 0.34+: `node docs/art/marca-oficial/v1/export.mjs`. Puede indicarse el módulo mediante `SHARP_MODULE`. Descargar antes el TTF con Git LFS; el script rechaza un puntero LFS en lugar de usar otra fuente silenciosamente.
- `validation.json` comprueba dimensiones, canales, peso y referencias del catálogo. `manifest-sha256.json` inventaría el paquete. No sustituye validación de la tienda ni prueba en dispositivos.

## Fuentes técnicas

Consultadas el 23-10-2026:

- [Google Play: especificaciones del icono](https://developer.android.com/distribute/google-play/resources/icon-design-specifications).
- [Android: capas, zona segura e iconos temáticos](https://developer.android.com/develop/ui/compose/system/icon_design_adaptive).
- [Apple: catálogo y apariencias de iconos](https://developer.apple.com/documentation/xcode/configuring-your-app-icon).
- [Apple: guía visual de iconos](https://developer.apple.com/design/human-interface-guidelines/app-icons).

**Estado:** paquete de identidad y exports preparado y comprobado localmente. Integración en Unity/Xcode/web y prueba física pendientes; no se ha publicado ni desplegado nada con esta entrega.
