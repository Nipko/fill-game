# Tintalú: dirección y encargo del icono

23-10-2026 · El dueño comparó cuia que vierte y TiTino y **eligió la dirección de las cuias vertiendo**. Pidió una versión simplificada para el icono, sin texto y con toque amazónico. La exportación final aún no está integrada. El registro de marca está aplazado. Estas propuestas no llevan ® ni acreditan exclusividad.

## 1. Criterio de diseño

Recomiendo comparar primero reconocimiento del juego a 48 px: **cuia** comunica la mecánica; **TiTino** aporta identidad y recuerdo, pero debe conservar una señal clara de vertido. Para la web y el logotipo escrito se puede mantener «Tintalú» aparte. No meter el nombre en el icono ni confundir icono de tienda, icono del launcher y logotipo escrito: son entregas distintas.

Dirección: D-003, «Selva brillante premium»: vidrio, luz, volumen y contraste; fondo azul petróleo/verde selva contenido, líquidos protagonistas. Nada de acuarela, papel, oro biselado o escena diminuta abarrotada. Las propuestas IA son bocetos para conversar, no arte final ni piezas aprobadas por asesor cultural.

## 2. Cuatro conceptos del canon

| Concepto | Imagen | Criterio |
|---|---|---|
| A. Cuia que vierte | Cuia de vidrio inclinada; tres colores de fruta; chorro açaí y banda de río | Recomendación inicial. Comparar simplificación de la cuia y trayectoria del vertido. |
| B. Capi con cuia | Capi de frente con cuia de cuatro tintes; fondo ámbar | Alternativa existente, no elegida para los dos bocetos actuales. |
| C. Encuentro de las Aguas | Diagonal ámbar-té #522500 / beige #B08A5A y cuia con dos dosis | Alternativa paisajística, riesgo de lectura demasiado abstracta. |
| D. TiTino en su rama | Perezoso en rama, antifaz visible, cuia vertiendo como señal secundaria | Pedido del dueño para comparar con A. Rama genérica en exploración; especie y árbol final requieren revisión cultural. |

A usa #EF7D1A (aguaje), #F2B705 (taperebá) y #6A2C91 (açaí), como el encargo de tienda. Una imagen generada no garantiza colorimetría exacta; el artista ajustará los originales finales.

## 3. TiTino

Es **Bradypus variegatus**, perezoso de tres dedos de garganta marrón, no mono. Pose animal en rama, antifaz y anatomía legibles; ojos proporcionados. Sin ropa, accesorios, manos humanas, recipiente que lo contenga, alimentación, rescate o peligro. No presentar al animal como mascota comprable ni prometer beneficios de conservación. La rama genérica evita cerrar una especie de árbol aún pendiente. Fuentes: `encargo-titino-frutas-tinaja.md` y `docs/research/personaje/propuesta-titino.md` g.2.

No considerar que evitar proporciones infantiles resuelva por sí solo las políticas de público infantil: ficha, juego, marketing y destinatarios se evalúan juntos.

## 4. Entregables técnicos

- **Play:** 512 × 512 px, PNG de 32 bits, sRGB, máximo 1024 KB; cuadrado completo, sin redondeado ni sombra exterior incorporados. Google aplica su máscara. Puede haber luz/sombra dentro de la ilustración. [Especificación oficial](https://developer.android.com/distribute/google-play/resources/icon-design-specifications), consultada 23-10-2026.
- **Launcher adaptativo:** primer plano y fondo separados, fondo a sangre; incluir propuesta monocroma para iconos temáticos y previsualizaciones de máscaras circular, cuadrada redondeada y squircle. El encargo existente establece 108 × 108 dp y zona segura 66–72 dp como [NV]; **no tratar todo ese intervalo como garantía de recorte**. Integrador/artista deben confirmar plantilla Android vigente y mantener los rasgos esenciales dentro de la zona central conservadora de 66 dp antes de exportar. [Android adaptive icons](https://developer.android.com/develop/ui/compose/system/icon_design_adaptive). No convertir el PNG de tienda directamente en dos capas idénticas.
- **Maestro:** editable por capas, PNG de alta resolución y PSD si ese es el flujo del artista; transparencia real en primer plano, fondo opaco. Arte definitivo por Git LFS según el proyecto.
- **Evaluación:** miniaturas a 48 y 96 px, fondos claros/oscuros, máscaras, escala de grises y comparación con iconos de juegos próximos sin imitarlos. No declarar estas comprobaciones ejecutadas hasta tener los archivos finales.
- **Procedencia:** registro del autor, herramientas/IA utilizadas, licencias de insumos y cesión/licencia escrita para uso comercial y modificaciones. No registrar la obra como enteramente humana si no lo es.

Evitar texto promocional, rankings, ofertas o insignias falsas; sonrisa/flecha de Amazon; decoración étnica no revisada; gatos, calderos, gemas y otros elementos vetados por la dirección visual. Fuentes del proyecto: `docs/store-listing/assets-tienda.md`, GDD 07 §10.1 y D-003.

## 5. Próximo paso

Dirección A elegida por el dueño: dos cuias-garrafa de vidrio vertiendo, fieles a las capturas del juego, con cuello corto y fibra en boca/base. El icono simplificado elimina nombre, placa, racimos y paisaje; conserva tres tintes, chorro púrpura y dos hojas de palma sobre azul petróleo. Se produjo una imagen con ImageGen y una corrección de inclinación/márgenes; queda local en `Logs/art-concepts/tintalu-icono/` con sus prompts, fuera de Git. Aún faltan exportaciones de tienda, capas adaptativas y monocroma, comprobación a tamaños pequeños/máscaras e integración. No se tocó `Assets/`, no se cambió el nombre de la app y no se presentó ninguna marca.
