# Encargo para el Spark: rediseño de los personajes en «3D cartoon, juguete brillante»

Fecha: 2026-10-27. Lo escribe el integrador (Claude, en el escritorio). Decisión: D-058 (`docs/dev/decisiones.md`).

El dueño lo pidió así: «quiero que hagamos rediseño de los personajes, demosle mas estilo, como 3D cartoon, CGI estilizado, character y glozy toy aestethic, manda esa encargo a spark».

Respondió además a tres preguntas, en los tres casos con la opción recomendada:
- **Alcance:** todos los personajes.
- **Técnica:** por código, en pseudo-3D. Sin Blender ni IA de imágenes.
- **Entrega:** primero un piloto, que el dueño aprueba antes de seguir.

## 1. Qué es

Hay que dar a los personajes el aspecto de un **personaje CGI estilizado de juguete brillante**:
- formas redondas y macizas, con proporciones de juguete (cabeza grande, cuerpo compacto);
- volumen claro, materiales de vinilo o plástico lacado y brillos especulares;
- un acabado «premium» de juego móvil actual.

Todo **se sigue dibujando por código**, como hoy (D-052). El 3D es una ilusión: sombreado volumétrico sobre las formas de distancia con signo (SDF) que ya se usan. No hay modelos 3D, renders ni imágenes generadas.

## 2. Personajes y dónde están

En `Assets/_Project/Scripts/Presentation/`:

| Personaje | Archivos | Notas |
|---|---|---|
| **TiTino** (perezoso, protagonista) | `ProceduralArt.Sloth.cs`, `ProceduralArt.SlothRig.cs` | Tiene animación por rig y variante de noche (D-050, D-053). El rig y sus poses no cambian: solo cambia cómo se pinta cada pieza. |
| **Capi** (capibara del Mapa y del muelle) | `ProceduralArt.Mascots.cs`, `ProceduralArt.Dock.cs`, `ProceduralArt.Scene.cs`, `ProceduralArt.MapIcons.cs` | Se ve en el viaje del Mapa (D-053) y en el inicio «el muelle del río» (D-050). |
| **Fauna** (intro, Mapa y avistamientos) | `ProceduralArt.Fauna.cs`, `ProceduralArt.Morpho.cs`, `ProceduralArt.Pirana.cs`, `ProceduralArt.Firefly.cs` | Las mecánicas que son objetos (Hoja, Liana, Barro…) **no** entran: no son personajes. |
| **Los 16 avatares del jugador** | `ProceduralArt.Avatars.cs` (rama `tanda6/avatares`) | **Todavía no están en la integración.** Entran en la fase 2, cuando el integrador avise en este archivo (§8). |

Lee primero:
- `docs/art/direccion-visual-v2.md`, la dirección «Selva brillante premium» (D-003). El estilo nuevo la **cumple**: vidrio, madera y luz de juego, sin papel ni acuarela. Lo que cambia es el acabado de los personajes.
- `docs/art/encargo-titino-frutas-tinaja.md` y `docs/dev/decisiones.md`: D-003, D-050, D-051, D-052, D-053, D-056 y D-058.

## 3. Guía de estilo (la escribes tú en la fase 1)

Entregable: `docs/art/estilo-personajes-3d.md`. Lo mínimo que tiene que fijar:

- **Luz:** una luz principal arriba a la izquierda, igual en todos los personajes, coherente con la luz de juego de D-003. Relleno suave y una luz de borde (rim) cálida por detrás.
- **Sombreado:**
  - degradado de volumen en tres tonos (luz, medio y sombra) sobre cada pieza;
  - oclusión ambiental en las uniones (cuello, brazos, orejas);
  - sombra de contacto suave en el suelo o la rama.
- **Material «juguete»:**
  - capa de barniz: un brillo especular nítido y pequeño, más un brillo ancho y suave;
  - el pelo de TiTino y de Capi, como felpa estilizada: mechones grandes y redondos, no pelos sueltos.
- **Ojos:** grandes, con dos reflejos (uno grande y otro pequeño) y un iris con degradado. Son lo que más «vende» el look.
- **Contorno:** sin la línea de tinta gruesa de ahora. Como mucho, un borde oscuro fino y coloreado para separar del fondo. Este cambio sustituye el «contorno de tinta de Capi» de los avatares (D-058).
- **Paleta:** la de cada personaje en el glosario §9 y en D-045, con más saturación en los brillos. Comprueba el contraste del modo **Alto contraste** y la variante de **noche** de TiTino.
- **Proporciones:** las de cada personaje, «achuchadas». La silueta tiene que seguir siendo reconocible a 32 dp (Liga) y a 24 dp (victoria).

Pon en la guía una lámina de referencia **generada por el propio código**: un PNG de captura, no dibujado a mano.

## 4. Fase 1: piloto (y para y espera la aprobación)

1. La guía de estilo de §3.
2. **TiTino** completo: todas las poses del rig, de día y de noche.
3. **Capi**: muelle y Mapa.
4. **Dos animales de la fauna**: el morpho y otro que elijas.
5. **Capturas** para el dueño, a 360 dp, al 100 % y en Alto contraste, **antes y después**:
   - inicio, intro, Mapa con el viaje de Capi, victoria;
   - una lámina con cada personaje en grande.

   Van en `docs/art/personajes-3d/piloto/`. Los PNG van por Git LFS, según `.gitattributes`.
6. Apunta el estado en `docs/dev/traspaso/spark-estado.md` (sección «Personajes 3D»), sube **solo** tu rama y **para**. El dueño mira las capturas y decide antes de la fase 2.

## 5. Fase 2: el resto (solo con el visto bueno del dueño)

- **La fauna que falta.**
- **Los 16 avatares, con su silueta de bloqueado** (D-056). Solo cuando el integrador avise en §8 de que `tanda6/avatares` ya está integrada.
- **Cualquier otro sitio donde salga un personaje.** Busca los usos de los métodos de arte.

## 6. Reglas (las de `CLAUDE.md`, y además)

- **Rama:** `spark/personajes-3d`, desde `origin/integracion-fase1-fase2`, en tu propio worktree. Nunca empujes `integracion-fase1-fase2` ni `main`: el integrador revisa y fusiona.
- **Nada nuevo:** ni paquetes, ni SDK, ni shaders nuevos de Shader Graph sin preguntar, ni herramientas externas de imagen. Todo en C# 9 dentro de `ProceduralArt.*`, con las ayudas de `ColorMath` y de lo que ya existe.
- **Sin Bloom** (UUM-150232; CLAUDE.md).
- **No cambies:** ni las APIs públicas de arte, ni los tamaños, ni los `ArtSlots`. Si existe un PNG del artista (`ArtSlotCatalog`), sigue mandando él.
- **Rendimiento:**
  - los tests de tiempo ya existentes siguen pasando, con `PerfBudget.Ms(...)` (está en la integración; no fijes milisegundos a mano);
  - cero asignaciones por fotograma donde hoy las hay, y la misma memoria de los atlas, o justifica la diferencia;
  - si un retrato pasa de su presupuesto, cachea o baja la resolución interna antes de subir el presupuesto, y pregunta.
- **YAML de Unity:** no edites a mano ninguno: escenas, prefabs, assets ni `.meta`.
- **Editor:** usa uno solo por carpeta.
- **Tests:**
  - los snapshot existentes pueden cambiar de imagen, pero no de tamaño ni de layout;
  - añade tests nuevos donde haya lógica nueva (por ejemplo, una función de sombreado pura).

## 7. Verificación (antes de cada entrega)

Los comandos de `CLAUDE.md`:
- `dotnet build -warnaserror` y `dotnet test`;
- `validate_schema.py`;
- `unity-compile`;
- **EditMode y PlayMode completos** (PlayMode con `-TimeoutSec 7200`);
- `unity-build.ps1 -Method Dev`, con `size-gate` y `check-16kb` sobre el APK.

Informa de los resultados en `spark-estado.md`. Si puedes, prueba el APK en un teléfono y anota fps y tiempos de arranque.

## 8. Avisos del integrador

- *(2026-10-27)* Encargo abierto. Los avatares (`tanda6/avatares`) aún no están integrados: el piloto no los toca.
- *(2026-10-27)* `tanda6/avatares` ya está en `integracion-fase1-fase2` (`ProceduralArt.Avatars.cs`, D-056). Entra en la fase 2, tras aprobar el piloto. Haz pull antes.
