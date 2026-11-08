#nullable enable
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.Profiling;

namespace AF.UI
{
    /// <summary>Objetos gráficos vivos del proceso en un momento (<see cref="DevProbe.SampleGfxInventory"/>).</summary>
    public readonly struct GfxInventory
    {
        public GfxInventory(int tex2d, long tex2dBytes, int meshes, long meshBytes, int materials, long materialBytes, int sprites, int canvases,
            long canvasBytes)
        {
            Texture2DCount = tex2d;
            Texture2DBytes = tex2dBytes;
            MeshCount = meshes;
            MeshBytes = meshBytes;
            MaterialCount = materials;
            MaterialBytes = materialBytes;
            SpriteCount = sprites;
            CanvasCount = canvases;
            CanvasBytes = canvasBytes;
        }

        public int Texture2DCount { get; }

        public long Texture2DBytes { get; }

        public int MeshCount { get; }

        public long MeshBytes { get; }

        public int MaterialCount { get; }

        public long MaterialBytes { get; }

        public int SpriteCount { get; }

        public int CanvasCount { get; }

        /// <summary>Lo que Unity cuenta a los lienzos (sus lotes de vértices incluidos).</summary>
        public long CanvasBytes { get; }
    }

    /// <summary>
    /// Lo que el Mapa del río tiene vivo en un momento (<see cref="RiverMapScreen.SampleGfx"/>; observación 5 de la prueba de la 1.1
    /// del 2026-11-07): con el Mapa cerrado, todo a cero salvo <see cref="CanvasBytes"/>, que se queda en lo que pesan los
    /// componentes vacíos (unos 11 KiB en el Editor).
    /// </summary>
    public readonly struct MapGfxSample
    {
        public MapGfxSample(int openCount, long textureBytes, long canvasBytes, int canvases, int renderers, int vertices)
        {
            OpenCount = openCount;
            TextureBytes = textureBytes;
            CanvasBytes = canvasBytes;
            Canvases = canvases;
            Renderers = renderers;
            Vertices = vertices;
        }

        /// <summary>Veces que se abrió el Mapa en la sesión (la vuelta 1, 2, 3… de la medida en el teléfono).</summary>
        public int OpenCount { get; }

        /// <summary>Las texturas propias del Mapa v2: el río vivo, los pueblos, el rig de Capi y la tripulación.</summary>
        public long TextureBytes { get; }

        /// <summary>Lo que Unity cuenta a los lienzos del Mapa (el raíz y los anidados), con sus lotes.</summary>
        public long CanvasBytes { get; }

        /// <summary>Lienzos del Mapa activos.</summary>
        public int Canvases { get; }

        /// <summary><see cref="CanvasRenderer"/> del Mapa activos (con el Mapa cerrado, 0: sus mallas se vacían al desactivarse).</summary>
        public int Renderers { get; }

        /// <summary>Vértices de la geometría por código activa (tierra, cintas del río, várzea, orillas y selva).</summary>
        public int Vertices { get; }
    }

    /// <summary>
    /// El inventario de objetos gráficos de la sonda <c>AF_PERF mem</c> (observación 5 de la prueba de la 1.1 del 2026-11-07:
    /// <c>GL mtrack</c> no vuelve al cerrar el Mapa v2). Cada línea <c>mem</c> (salvo las de <c>level</c> y <c>daily</c>) lleva,
    /// justo antes del detalle, <c>obj_tex2d</c>,
    /// <c>obj_tex2d_kib</c>, <c>obj_mesh</c>, <c>obj_mesh_kib</c>, <c>obj_mat</c>, <c>obj_mat_kib</c>, <c>obj_sprite</c>,
    /// <c>obj_canvas</c> y <c>obj_canvas_kib</c>: los <see cref="Texture2D"/>, <see cref="Mesh"/>, <see cref="Material"/>,
    /// <see cref="Sprite"/> y <see cref="Canvas"/> vivos en el proceso, con lo que Unity les cuenta
    /// (<see cref="Profiler.GetRuntimeMemorySizeLong"/>). Si al cerrar el Mapa estas cifras vuelven a las de la vuelta anterior y
    /// <c>GL mtrack</c> no, lo que queda no es un objeto nuestro. Las del Mapa (<c>map_*</c>) las da
    /// <see cref="FormatMapGfx"/>.
    /// </summary>
    public sealed partial class DevProbe
    {
        /// <summary>Cuenta los objetos gráficos vivos (asigna: solo en los momentos de la sonda).</summary>
        public static GfxInventory SampleGfxInventory()
        {
            Texture2D[] tex = Resources.FindObjectsOfTypeAll<Texture2D>();
            Mesh[] meshes = Resources.FindObjectsOfTypeAll<Mesh>();
            Material[] mats = Resources.FindObjectsOfTypeAll<Material>();
            Canvas[] canvases = Resources.FindObjectsOfTypeAll<Canvas>();
            return new GfxInventory(tex.Length, SizeOf(tex), meshes.Length, SizeOf(meshes), mats.Length, SizeOf(mats),
                Resources.FindObjectsOfTypeAll<Sprite>().Length, canvases.Length, SizeOf(canvases));
        }

        /// <summary>Miembros JSON (con coma delante) del inventario <paramref name="g"/> (puro: no lee nada de Unity).</summary>
        internal static string FormatGfxInventory(in GfxInventory g)
        {
            CultureInfo inv = CultureInfo.InvariantCulture;
            var sb = new StringBuilder(200);
            sb.Append(",\"obj_tex2d\":").Append(g.Texture2DCount.ToString(inv));
            sb.Append(",\"obj_tex2d_kib\":").Append((g.Texture2DBytes / 1024L).ToString(inv));
            sb.Append(",\"obj_mesh\":").Append(g.MeshCount.ToString(inv));
            sb.Append(",\"obj_mesh_kib\":").Append((g.MeshBytes / 1024L).ToString(inv));
            sb.Append(",\"obj_mat\":").Append(g.MaterialCount.ToString(inv));
            sb.Append(",\"obj_mat_kib\":").Append((g.MaterialBytes / 1024L).ToString(inv));
            sb.Append(",\"obj_sprite\":").Append(g.SpriteCount.ToString(inv));
            sb.Append(",\"obj_canvas\":").Append(g.CanvasCount.ToString(inv));
            sb.Append(",\"obj_canvas_kib\":").Append((g.CanvasBytes / 1024L).ToString(inv));
            return sb.ToString();
        }

        /// <summary>
        /// Miembros JSON (con coma delante) de lo que el Mapa tiene vivo (puro): <c>map_open_n</c> (la vuelta), <c>map_tex_kib</c>,
        /// <c>map_canvas</c>, <c>map_canvas_kib</c>, <c>map_renderers</c> y <c>map_verts</c>.
        /// </summary>
        internal static string FormatMapGfx(in MapGfxSample m)
        {
            CultureInfo inv = CultureInfo.InvariantCulture;
            var sb = new StringBuilder(160);
            sb.Append(",\"map_open_n\":").Append(m.OpenCount.ToString(inv));
            sb.Append(",\"map_tex_kib\":").Append((m.TextureBytes / 1024L).ToString(inv));
            sb.Append(",\"map_canvas\":").Append(m.Canvases.ToString(inv));
            sb.Append(",\"map_canvas_kib\":").Append((m.CanvasBytes / 1024L).ToString(inv));
            sb.Append(",\"map_renderers\":").Append(m.Renderers.ToString(inv));
            sb.Append(",\"map_verts\":").Append(m.Vertices.ToString(inv));
            return sb.ToString();
        }

        private static long SizeOf<T>(T[] all) where T : Object
        {
            long bytes = 0;
            for (int i = 0; i < all.Length; i++)
            {
                bytes += Profiler.GetRuntimeMemorySizeLong(all[i]);
            }

            return bytes;
        }
    }
}
