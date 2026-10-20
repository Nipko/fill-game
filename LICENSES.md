# Licencias de terceros de Amazon Fill (Tintalú) · BORRADOR

- **Qué es:** el registro de todo el material de terceros que entra en el juego (08 §3, `LICENSES.md`: «audio/arte de terceros», research/08 §4.3) y la base de la pantalla de Créditos (`docs/store-listing/creditos.md`). Borrador del bloque B13 de la tanda 5 (2026-10-20). `[PENDIENTE: abogado]` revisa que las licencias permiten el uso comercial, en anuncios y en tráilers, y lo que hay que mostrar al jugador.
- **Regla:** cada pieza nueva de terceros entra con su fila aquí en el mismo PR. Para el audio manda `Assets/_Project/Audio/audio_manifest.json` (`docs/audio/encargo-sonidos.md` §9); este archivo lo resume.
- **Fuera de aquí:** las fuentes de los Datos del río y de las Frutas del río, que no son licencias sino citas, van en la columna `source` de `facts.csv` y en Ajustes › «Sobre la Amazonía» (07 §8.2; 08 §4.7).

## 1. Tipografías (en el build)

| Fuente | Archivos | Licencia | Titular | Texto de la licencia |
|---|---|---|---|---|
| Baloo 2 (Bold y ExtraBold) | `Assets/_Project/Art/Fonts/Baloo2-*.ttf` | SIL Open Font License 1.1 | The Baloo 2 Project Authors (https://github.com/EkType/Baloo2) | `Assets/_Project/Art/Fonts/OFL-Baloo2.txt` |
| Nunito (Regular, Bold y ExtraBold) | `Assets/_Project/Art/Fonts/Nunito-*.ttf` | SIL Open Font License 1.1 | The Nunito Project Authors (https://github.com/googlefonts/nunito) | `Assets/_Project/Art/Fonts/OFL-Nunito.txt` |
| Liberation Sans (la de TextMesh Pro) | `Assets/TextMesh Pro/Fonts/LiberationSans.ttf` | SIL Open Font License 1.1 | Google Corporation (2010); Red Hat, Inc. (2012) | `Assets/TextMesh Pro/Fonts/LiberationSans - OFL.txt` |

La OFL permite el uso comercial y pide que el aviso de copyright y la licencia acompañen a la fuente cuando se distribuye. Propuesta [P]: la pantalla de Créditos enlaza «Licencias de terceros», con el texto de las tres OFL (Q-B13-14). `[PENDIENTE: build-release]` comprobar si Liberation Sans llega al build de release o solo al Editor.

## 2. Audio (173 archivos, entrega 1 de AU3)

- **Estado:** la entrega 1 (`4476796`; D-026 y D-029, pregunta 64) son 173 WAV, **todos con `license` = `cc0`** en el manifiesto, mezclados por «Tintalú Sound Design» a partir de grabaciones de dominio público o CC0. El audio por código queda de respaldo (07 §9).
- **Choque con 07 §9.5:** el GDD decide **música original con cesión total** (buyout mundial y perpetuo, también para anuncios y tráilers) y grabaciones de fauna con licencia comercial perpetua o encargadas; la entrega 1 es CC0. CC0 permite el uso comercial sin atribución, así que no impide publicar, pero la música y la piha siguen pendientes del encargo de 9.5 `[PENDIENTE: dueño]`.
- **Verificación pendiente** `[PENDIENTE: dueño]`: el manifiesto da **una URL por archivo**, pero muchos mezclan varias fuentes (p. ej. «Gota en vidrio (Wikimedia PD) + marimba de chonta (VSCO-2-CE CC0)»). Antes del release, alguien comprueba la licencia de cada fuente citada en `source`, también las que no tienen URL, y que cada observación de iNaturalist tiene el sonido (no solo la foto) en CC0.
- La atribución no es obligatoria con CC0; propuesta [P]: agradecer a los autores en Créditos (Q-B13-14).

Fuentes por URL principal (del manifiesto, agrupadas; columna «Autor», tal cual en `author`):

| URL principal | Archivos | Autor (manifiesto) | Licencia | Ejemplos de `id` |
|---|---|---|---|---|
| https://github.com/sgossner/VSCO-2-CE | 116 | Tintalú Sound Design / VSCO-2-CE & Wikimedia Commons (CC0); Tintalú Sound Design / VSCO-2-CE & Wikimedia Commons (CC0/PD); Tintalú Sound Design / VSCO-2-CE (CC0); Wikimedia Commons (PD) & VSCO-2-CE (CC0) | cc0 | `sfx_select`, `sfx_tint`, `sfx_select_jenipapo`, `sfx_tint_jenipapo` y 112 más |
| https://commons.wikimedia.org/wiki/File:Pouring_wine.ogg | 7 | Tintalú Sound Design / VSCO-2-CE & Wikimedia Commons (CC0) | cc0 | `sfx_pour_start`, `sfx_pour_loop_l1`, `sfx_pour_loop_l2`, `sfx_pour_loop_l3` y 3 más |
| https://www.inaturalist.org/observations/397290245 | 5 | davidbroadland (iNaturalist CC0) & Wikimedia Commons (CC0/PD) / Tintalú Sound Design; davidbroadland (iNaturalist CC0) & Wikimedia Commons (PD); davidbroadland (iNaturalist CC0), Wikimedia Commons (CC0/PD) & VSCO-2-CE (CC0); davidbroadland (iNaturalist CC0), Wikimedia Commons (PD) & VSCO-2-CE (CC0) | cc0 | `amb_w02_fauna_02`, `sfx_skip`, `sfx_river_drop`, `sfx_rosa_surface` y 1 más |
| https://commons.wikimedia.org/wiki/File:433589_jackthemurray_stream-river-water-up-close.wav | 4 | Tintalú Sound Design / VSCO-2-CE & Wikimedia Commons (CC0/PD); Tintalú Sound Design / Wikimedia Commons & Félix Blume (CC0/PD) | cc0 | `amb_w03_water`, `amb_w05_water`, `amb_w06_water`, `amb_w09_water` |
| https://commons.wikimedia.org/wiki/File:Ducks_landing_in_water.ogg | 4 | Wikimedia Commons (CC0/PD) / Tintalú Sound Design; Wikimedia Commons (PD) & VSCO-2-CE (CC0); Wikimedia Commons (PD), ritaclare (iNaturalist CC0) & VSCO-2-CE (CC0) | cc0 | `sfx_capi_paddle`, `sfx_canoe_lost`, `sfx_canoe_back`, `sfx_map_paddle` |
| https://commons.wikimedia.org/wiki/File:Rain_(1).ogg | 4 | Tintalú Sound Design / VSCO-2-CE & Wikimedia Commons (CC0/PD); Tintalú Sound Design / Wikimedia Commons & Félix Blume (CC0/PD) | cc0 | `amb_rain_light`, `amb_rain_medium`, `amb_rain_heavy`, `sfx_mech_lluvia` |
| https://commons.wikimedia.org/wiki/File:Shallow_small_river_with_stony_riverbed.ogg | 4 | Tintalú Sound Design / VSCO-2-CE & Wikimedia Commons (CC0/PD); VSCO-2-CE (CC0) & Wikimedia Commons (PD); Wikimedia Commons (CC0/PD) & VSCO-2-CE (CC0) | cc0 | `sfx_shuffle`, `sfx_map_pequepeque`, `amb_w07_water`, `amb_w08_water` |
| https://commons.wikimedia.org/wiki/File:363120_fractalstudios_water-trickle.wav | 3 | Tintalú Sound Design / VSCO-2-CE & Wikimedia Commons (CC0); Tintalú Sound Design / Wikimedia Commons & Félix Blume (CC0/PD); Wikimedia Commons (CC0/PD) & VSCO-2-CE (CC0) | cc0 | `sfx_river_rise`, `amb_w04_water`, `sfx_mech_barro` |
| https://commons.wikimedia.org/wiki/File:Water_bubbles_chortling.ogg | 3 | Tintalú Sound Design / VSCO-2-CE & Wikimedia Commons (CC0); Tintalú Sound Design / Wikimedia Commons (PD) | cc0 | `sfx_fill`, `sfx_fruit_plip`, `sfx_mech_remolino` |
| https://www.inaturalist.org/observations/169455569 | 2 | kzoebel (iNaturalist CC0) & Wikimedia Commons (PD) / Tintalú Sound Design; kzoebel (iNaturalist CC0), Wikimedia Commons (PD) & VSCO-2-CE (CC0) | cc0 | `sfx_mech_pirana`, `amb_w09_fauna_01` |
| https://commons.wikimedia.org/wiki/File:404114_felix-blume_toucans-singing-in-the-amazonian-rainforest-brazil.ogg | 1 | Félix Blume (Wikimedia CC0) & aleosha (iNaturalist CC0) / Tintalú Sound Design | cc0 | `amb_w02_fauna_01` |
| https://commons.wikimedia.org/wiki/File:410435_felix-blume_two-people-singing-in-the-street-in-a-favela-rio-de-janeiro.ogg | 1 | Félix Blume (Wikimedia Commons CC0) & VSCO-2-CE (CC0) / Tintalú Sound Design | cc0 | `amb_w01_fauna_02` |
| https://commons.wikimedia.org/wiki/File:Ocean_Waves_on_a_Tropical_Beach.ogg | 1 | Tintalú Sound Design / Wikimedia Commons & Félix Blume (CC0/PD) | cc0 | `amb_w01_water` |
| https://commons.wikimedia.org/wiki/File:On_a_pebble_beach.ogg | 1 | Tintalú Sound Design / Wikimedia Commons & Félix Blume (CC0/PD) | cc0 | `amb_w02_water` |
| https://commons.wikimedia.org/wiki/File:Walk_in_the_rainforest.ogg | 1 | Wikimedia Commons (PD) & VSCO-2-CE (CC0) | cc0 | `sfx_streak_shield` |
| https://www.inaturalist.org/observations/114428505 | 1 | kzoebel (iNaturalist CC0) & Wikimedia Commons (PD) / Tintalú Sound Design | cc0 | `amb_w05_fauna_01` |
| https://www.inaturalist.org/observations/234714937 | 1 | carlosmosquera (iNaturalist CC0) / Tintalú Sound Design | cc0 | `amb_w01_fauna_03` |
| https://www.inaturalist.org/observations/234714945 | 1 | carlosmosquera, paularomano & gabrielfialho (iNaturalist CC0) / Tintalú Sound Design | cc0 | `sfx_world_banner` |
| https://www.inaturalist.org/observations/246468305 | 1 | loarie & celimilia (iNaturalist CC0) / Tintalú Sound Design | cc0 | `amb_w06_fauna_01` |
| https://www.inaturalist.org/observations/313524483 | 1 | crebassa (iNaturalist CC0) & Wikimedia Commons (PD) / Tintalú Sound Design | cc0 | `amb_w04_fauna_02` |
| https://www.inaturalist.org/observations/346194395 | 1 | gabrielfialho (iNaturalist CC0) / Tintalú Sound Design | cc0 | `amb_w07_fauna_01` |
| https://www.inaturalist.org/observations/346867530 | 1 | jilldevito & ritaclare (iNaturalist CC0) & VSCO-2-CE (CC0) | cc0 | `sfx_streak_up` |
| https://www.inaturalist.org/observations/367450567 | 1 | gabrielfialho (iNaturalist CC0) / Tintalú Sound Design | cc0 | `amb_w03_fauna_02` |
| https://www.inaturalist.org/observations/395716765 | 1 | gendgrr & bastian6709 (iNaturalist CC0) / Tintalú Sound Design | cc0 | `amb_w01_fauna_01` |
| https://www.inaturalist.org/observations/398455943 | 1 | ferferrari & squidtk (iNaturalist CC0) / Tintalú Sound Design | cc0 | `amb_w06_fauna_03` |
| https://www.inaturalist.org/observations/399384929 | 1 | gabrielfialho & caseyhh (iNaturalist CC0) / Tintalú Sound Design | cc0 | `sfx_monkey_chirp` |
| https://www.inaturalist.org/observations/402089342 | 1 | russnamitz & davidbroadland (iNaturalist CC0) & Félix Blume (Wikimedia CC0) | cc0 | `amb_w03_fauna_01` |
| https://www.inaturalist.org/observations/402547958 | 1 | ritaclare & jilldevito (iNaturalist CC0) / Tintalú Sound Design | cc0 | `amb_w07_fauna_02` |
| https://www.inaturalist.org/observations/403651669 | 1 | morganstickrod, gabrielfialho & ritaclare (iNaturalist CC0) | cc0 | `amb_w08_fauna_01` |
| https://www.inaturalist.org/observations/404591346 | 1 | gabrielfialho (iNaturalist CC0) / Tintalú Sound Design | cc0 | `amb_w04_fauna_01` |
| https://www.inaturalist.org/observations/405295820 | 1 | crebassa (iNaturalist CC0) / Tintalú Sound Design | cc0 | `amb_w06_fauna_02` |

## 3. Paquetes de Unity y motor (en el build)

| Componente | Versión | Licencia | Nota |
|---|---|---|---|
| Unity 6000.3.25f1 (Unity Personal) | `ProjectSettings/ProjectVersion.txt` | Términos de Unity | Licencia del dueño (`CLAUDE.md`, «Humanos») |
| Paquetes de Unity de `Packages/manifest.json` (URP 17.3.0, Input System 1.20.0, uGUI 2.0.0, 2D Animation 13.0.6, Timeline 1.8.13, etc.) | Las del manifiesto | Unity Companion License o términos de Unity [NV] | `[PENDIENTE: build-release]` lista de los que llegan al build de release |
| `com.unity.pipeline` | 0.8.0-exp.1 | Términos de Unity [NV] | Herramienta del Editor; 08 §7 pide comprobar que su servidor de runtime **no** entra en el build de release |

## 4. Código propio y herramientas (no se distribuyen)

| Componente | Licencia | Nota |
|---|---|---|
| AF.Core, AF.Solver y el resto de `Assets/_Project/Scripts/` | Propio | Escrito por agentes de IA para el dueño (`CLAUDE.md`) |
| NUnit 3.14.0 (tests de `dotnet/`) | MIT [NV] | Solo en desarrollo |
| Python `jsonschema` (`tools/`) | MIT [NV] | Solo en desarrollo |

## 5. Arte

| Material | Estado | Licencia |
|---|---|---|
| Arte provisional por código (`ProceduralArt*.cs`, fondos, Mapa) | En el build | Propio |
| Arte final (fondos, skins, TiTino, Capi, Rosa, Mapa, iconos, tienda) | Encargo al artista 2D freelance (07 §11) | `[PENDIENTE: dueño]` contrato con cesión de derechos para el juego, la tienda y los anuncios |
| Skin «Cuia de Santarém» | Encargo pagado, con crédito y consentimiento (07 §8.1) | `[PENDIENTE: dueño]` |

## 6. SDK previstos para la 1.0 (aún no están; 08 §7)

| SDK | Versión fijada | Licencia [NV] |
|---|---|---|
| AppLovin MAX + adaptadores (AdMob, Unity Ads, Meta Audience Network, Mintegral) | 8.6.6 | Términos de cada red |
| Firebase Unity SDK (Analytics, Crashlytics, Remote Config, Auth, App Check, Functions) | 13.17.0 | Apache 2.0 y términos de Firebase [NV] |
| Unity In App Purchasing (+ Authentication 3.7.1 o superior) | 5.4.4 | Términos de Unity |
| Play Games plugin for Unity | 2.3.0 | Apache 2.0 [NV] |
| Addressables for Android 1.1.0 / Addressables 2.10.3 | — | Unity Companion License [NV] |
| External Dependency Manager (EDM4U) | 1.2.189 | Apache 2.0 [NV] |
| Unity Mobile Notifications (aprobado en D-033, Q-M5) | La anota quien la integre | Unity Companion License [NV] |

Al integrar cada SDK, quien lo integra copia aquí su licencia y la de sus dependencias nativas, y confirma si hay que mostrar avisos al jugador.
