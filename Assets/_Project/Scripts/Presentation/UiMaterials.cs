#nullable enable
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Materiales compartidos de los shaders de UI propios (<c>Assets/_Project/Shaders/Resources/</c>; guía visual v2,
    /// 10.1): se crean en tiempo de ejecución con <c>new Material(shader)</c>, nunca como <c>.mat</c> a mano. Los shaders
    /// están en una carpeta <c>Resources</c>, así entran en el build sin tocar <c>ProjectSettings/</c>. Si un shader faltara,
    /// el material es null y el gráfico usa el material por defecto de uGUI (se ve plano, pero se ve).
    /// </summary>
    public static class UiMaterials
    {
        private static Material? _liquid;
        private static Material? _stream;
        private static Material? _additive;
        private static Material? _glass;
        private static Material? _glassSelected;
        private static bool _loaded;

        /// <summary>Líquido de las cuias (<c>AF/UI/Liquid</c>).</summary>
        public static Material? Liquid
        {
            get
            {
                Load();
                return _liquid;
            }
        }

        /// <summary>Chorro del vertido (<c>AF/UI/Stream</c>).</summary>
        public static Material? Stream
        {
            get
            {
                Load();
                return _stream;
            }
        }

        /// <summary>Brillos aditivos (<c>AF/UI/Additive</c>).</summary>
        public static Material? Additive
        {
            get
            {
                Load();
                return _additive;
            }
        }

        /// <summary>Vidrio frontal en reposo (<c>AF/UI/Glass</c>, <c>_Boost</c> = 1).</summary>
        public static Material? Glass
        {
            get
            {
                Load();
                return _glass;
            }
        }

        /// <summary>Vidrio frontal del origen seleccionado: +10 % de brillo (02 §4.1; guía 4.1). Solo el origen (D-002).</summary>
        public static Material? GlassSelected
        {
            get
            {
                Load();
                return _glassSelected;
            }
        }

        private static void Load()
        {
            if (_loaded)
            {
                return;
            }

            _loaded = true;
            _liquid = Create("AF/UI/Liquid", "AF Liquid");
            _stream = Create("AF/UI/Stream", "AF Stream");
            _additive = Create("AF/UI/Additive", "AF Additive");
            _glass = Create("AF/UI/Glass", "AF Glass");
            _glassSelected = Create("AF/UI/Glass", "AF Glass Selected");
            if (_glassSelected != null)
            {
                _glassSelected.SetFloat("_Boost", 1.10f);
            }
        }

        private static Material? Create(string shaderName, string name)
        {
            Shader? shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogWarning("[AF.UiMaterials] no se encuentra el shader " + shaderName + ": se usa el material por defecto de uGUI.");
                return null;
            }

            return new Material(shader) { name = name, hideFlags = HideFlags.DontSave };
        }
    }
}
