#nullable enable
using System;
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Ranuras de arte (guía visual v2, 10.3; <c>docs/art/prompts-chatgpt.md</c> §2): cada elemento visual busca primero el
    /// PNG <c>Assets/_Project/Art/Generated/&lt;ranura&gt;.png</c> y, si no existe, usa el procedural. En el build no se puede
    /// leer <c>Assets/</c> por ruta: un script de Editor versionado (<c>AF.EditorTools.GeneratedArt</c>) aplica los ajustes de
    /// importación y mantiene <see cref="ArtSlotCatalog"/> en <c>Resources/AF/ArtSlotCatalog</c> con los PNG presentes. Si no
    /// hay ningún PNG, el catálogo no existe y todo es procedural. Lista de ranuras: <c>Assets/_Project/Art/Generated/README.md</c>.
    /// </summary>
    public static class ArtSlots
    {
        /// <summary>Ruta del catálogo dentro de <c>Resources</c>.</summary>
        public const string CatalogResource = "AF/ArtSlotCatalog";

        /// <summary>Panorama del Estuario (base; prioridad 1).</summary>
        public const string Panorama = "bg_estuario_panorama";

        /// <summary>Muelle en primer plano (base; prioridad 1).</summary>
        public const string Dock = "bg_estuario_muelle";

        /// <summary>Follaje de la esquina superior izquierda (base; prioridad 1).</summary>
        public const string FoliageTopLeft = "foliage_tl";

        /// <summary>Follaje de la esquina superior derecha (base; prioridad 1).</summary>
        public const string FoliageTopRight = "foliage_tr";

        /// <summary>Follaje de la esquina inferior izquierda (base; prioridad 1).</summary>
        public const string FoliageBottomLeft = "foliage_bl";

        /// <summary>Follaje de la esquina inferior derecha (base; prioridad 1).</summary>
        public const string FoliageBottomRight = "foliage_br";

        /// <summary>Cinta-hoja de victoria (base; prioridad 2).</summary>
        public const string Ribbon = "ui_cinta_hoja";

        /// <summary>CTA de la familia Hoja (opcional).</summary>
        public const string ButtonLeaf = "ui_btn_hoja";

        /// <summary>CTA de la familia Agua (opcional).</summary>
        public const string ButtonWater = "ui_btn_agua";

        /// <summary>Botón secundario de la familia Madera (opcional; sin uso en v0).</summary>
        public const string ButtonWood = "ui_btn_madera";

        /// <summary>Ranura de Deshacer (opcional).</summary>
        public const string SlotWater = "ui_slot_agua";

        /// <summary>Disco de Reiniciar (opcional).</summary>
        public const string DiscWood = "ui_disc_madera";

        /// <summary>Marco del panel del tablero, solo marco (opcional).</summary>
        public const string PanelFrame = "ui_panel_tablero";

        /// <summary>Marco de los modales, solo marco (opcional).</summary>
        public const string ModalPanel = "ui_panel_modal";

        /// <summary>Bandeja cumbuca (opcional).</summary>
        public const string Tray = "ui_bandeja";

        /// <summary>Placa de la insignia de nivel (opcional).</summary>
        public const string BadgePlate = "ui_placa_nivel";

        /// <summary>Tesela de madera oscura (opcional; reservada, aún sin conectar).</summary>
        public const string WoodDarkTile = "wood_dark_tile";

        /// <summary>Tesela de madera de repisa (opcional; reservada, aún sin conectar).</summary>
        public const string WoodLightTile = "wood_light_tile";

        /// <summary>Capi en pose neutra (fase de jugabilidad; se usa si existe).</summary>
        public const string Capi = "capi_idle";

        /// <summary>Prefijo de las frutas de los 14 tintes (<c>fruta_&lt;id del tinte&gt;</c>, 256 × 256; guion de la intro §12).</summary>
        public const string FruitPrefix = "fruta_";

        /// <summary>Prefijo de las ranuras de fauna (<c>fauna_&lt;id&gt;_partes</c> y <c>fauna_&lt;id&gt;_retrato</c>).</summary>
        public const string FaunaPrefix = "fauna_";

        /// <summary>Sufijo de la hoja de piezas de un miquito (1024 × 1024, rejilla de 4 × 4 celdas de 256 px).</summary>
        public const string FaunaPartsSuffix = "_partes";

        /// <summary>Sufijo del retrato de 3/4 de un miquito (512 × 512; tarjeta y Cuaderno).</summary>
        public const string FaunaPortraitSuffix = "_retrato";

        /// <summary>Prefijo de la rama de cada mundo (<c>rama_estuario</c>, 1024 × 256).</summary>
        public const string BranchPrefix = "rama_";

        /// <summary>Liana en tesela para el modelo <c>cuelga</c> (64 × 256).</summary>
        public const string LianaTile = "liana_tile";

        /// <summary>Todas las ranuras fijas, en el orden de la tabla 2 de los prompts, con las 14 frutas.</summary>
        public static readonly string[] All =
        {
            Panorama, Dock, FoliageTopLeft, FoliageTopRight, FoliageBottomLeft, FoliageBottomRight, Ribbon,
            ButtonLeaf, ButtonWater, ButtonWood, SlotWater, DiscWood, PanelFrame, ModalPanel, Tray, BadgePlate,
            WoodDarkTile, WoodLightTile, Capi,
            "fruta_acai", "fruta_aguaje", "fruta_tapereba", "fruta_guanabana", "fruta_bacaba",
            "fruta_cupuacu", "fruta_urucum", "fruta_camu_camu", "fruta_jenipapo", "fruta_agua_tapajos", "fruta_lulo",
            "fruta_araza", "fruta_cacao", "fruta_agua_negro", LianaTile,
        };

        /// <summary>
        /// La ranura existe: una de <see cref="All"/>, la hoja de piezas o el retrato de una especie del catálogo de fauna
        /// (<c>fauna_&lt;id&gt;_partes</c>, <c>fauna_&lt;id&gt;_retrato</c>) o la rama de un mundo (<c>rama_&lt;mundo&gt;</c>).
        /// </summary>
        public static bool IsKnown(string slot)
        {
            if (string.IsNullOrEmpty(slot))
            {
                return false;
            }

            if (Array.IndexOf(All, slot) >= 0)
            {
                return true;
            }

            if (slot.StartsWith(FaunaPrefix, StringComparison.Ordinal))
            {
                bool parts = slot.EndsWith(FaunaPartsSuffix, StringComparison.Ordinal) && slot.Length > FaunaPrefix.Length + FaunaPartsSuffix.Length;
                bool portrait = slot.EndsWith(FaunaPortraitSuffix, StringComparison.Ordinal) && slot.Length > FaunaPrefix.Length + FaunaPortraitSuffix.Length;
                return parts || portrait;
            }

            return slot.StartsWith(BranchPrefix, StringComparison.Ordinal) && slot.Length > BranchPrefix.Length;
        }

        private static ArtSlotCatalog? _catalog;
        private static bool _loaded;

        /// <summary>Ignora los PNG y usa siempre el procedural (capturas de comparación y pruebas).</summary>
        public static bool ForceProcedural { get; set; }

        /// <summary>Sprite pintado de la ranura <paramref name="slot"/>, o null si no hay PNG (se usa el procedural).</summary>
        public static Sprite? Find(string slot)
        {
            if (ForceProcedural)
            {
                return null;
            }

            if (!_loaded)
            {
                _loaded = true;
                _catalog = Resources.Load<ArtSlotCatalog>(CatalogResource);
            }

            return _catalog != null ? _catalog.Find(slot) : null;
        }

        /// <summary>Vuelve a leer el catálogo (tras importar PNG nuevos en el Editor).</summary>
        public static void Reload()
        {
            _loaded = false;
            _catalog = null;
        }
    }
}
