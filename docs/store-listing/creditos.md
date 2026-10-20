# Pantalla de Créditos · BORRADOR

- **Estado:** borrador del bloque B13 (2026-10-20): contenido y textos de la pantalla de Créditos. La pantalla en el juego no existe todavía; la hace el carril que tenga Ajustes (los archivos `SettingsScreen*` son del escritorio, `docs/dev/traspaso/spark-reto-del-rio.md` §3.2).
- **Canon:** 07 §8.2: «pantalla de créditos con nombre del asesor y de los artistas regionales» (1-2 artistas de Belém/Santarém, Pucallpa, Leticia o Tena) y una sección «Sobre la Amazonía» en Ajustes con la fuente de cada Dato del río; 07 §9.5 y §8.1 (Cuia de Santarém, con crédito); `LICENSES.md`.
- **Textos:** las claves nuevas van en `strings-creditos.csv` (misma cabecera que `strings.csv`; `status` = `borrador`), para que el integrador las pase a `Assets/_Project/Data/Loc/strings.csv` (08 §4.7; `.claude/rules/localizacion.md`). Los **nombres propios no son textos**: van como datos, sin traducir, y solo con el consentimiento de cada persona.

## 1. Dónde está

Ajustes › «Créditos» (`credits_open`), junto a «Sobre la Amazonía» (`settings_about_amazon`, glosario §10) [P]. Se puede leer con TalkBack: cada sección es un encabezado (02 §12) [P].

## 2. Contenido, en este orden [P]

| # | Sección (clave) | Contenido | Quién lo da |
|---|---|---|---|
| 1 | Título (`credits_title`) | «Tintalú» con su logotipo cuando exista | — |
| 2 | Un juego de (`credits_section_studio`) | `[PENDIENTE: dueño — nombre del estudio o del titular]` | Dueño |
| 3 | Dirección y producción (`credits_section_production`) | `[PENDIENTE: dueño]` | Dueño |
| 4 | Programación (`credits_section_code`) | `[PENDIENTE: dueño]` cómo se acredita el código escrito por agentes de IA (Q-B13-13) | Dueño |
| 5 | Arte (`credits_section_art`) | `[PENDIENTE: dueño — artista 2D freelance]` | Dueño, con el consentimiento del artista |
| 6 | Asesoría cultural (`credits_section_cultural`) | `[PENDIENTE: dueño — nombre del asesor cultural]` (obligatorio, 07 §8.2) | Dueño, con el consentimiento del asesor |
| 7 | Artistas de la región (`credits_section_regional`) | `[PENDIENTE: dueño]` 1-2 artistas regionales y la Cuia de Santarém (07 §8.1-§8.2) | Dueño |
| 8 | Sonido y música (`credits_section_audio`) | Hoy, la línea `credits_audio_cc0` y los autores del manifiesto (`LICENSES.md` §2); con el encargo de 07 §9.5, el compositor y el diseñador de sonido | Dueño |
| 9 | Tipografías (`credits_section_fonts`) | «Baloo 2 · The Baloo 2 Project Authors» y «Nunito · The Nunito Project Authors», SIL Open Font License 1.1 (`LICENSES.md` §1) | — |
| 10 | Revisión de textos (`credits_section_translation`) | `[PENDIENTE: dueño]` revisores nativos de EN y PT-BR, cuando existan | Dueño |
| 11 | Agradecimientos (`credits_section_thanks`) | `[PENDIENTE: dueño]` testers de la prueba cerrada (08 §13.1 #20), con permiso | Dueño |
| 12 | Licencias de terceros (`credits_licenses`) | Botón que abre el texto de las licencias (OFL de las fuentes y las de los SDK; Q-B13-14) | — |
| 13 | Pie (`credits_independent`) | «Juego independiente inspirado en la cuenca amazónica; sin relación con Amazon.com, Inc.» (08 §13.1 #5) | `[PENDIENTE: abogado]` |
| 14 | Versión (`credits_version`) | «Versión {version}» | — |

## 3. Reglas

- **Consentimiento:** ningún nombre de persona sin su permiso por escrito `[PENDIENTE: dueño]`; quien no quiera salir, sale como su rol.
- **Fuentes de los datos:** no van en Créditos; van en «Sobre la Amazonía» y en la tarjeta de cada parada (`map_card_source`; 07 §8.2; D-028).
- **Sin «Amazon» en nombres:** el estudio y el desarrollador no llevan ninguna forma de «Amazon» (08 §13.1 #1).
- **Lo cultural:** la línea de agradecimiento a las comunidades o regiones, si la hay, la redacta el asesor (`pendiente_asesor`).
