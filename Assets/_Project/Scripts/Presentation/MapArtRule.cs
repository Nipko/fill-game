#nullable enable
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Compuerta del arte provisional del Mapa del río (P-M5 de D-023; como <c>FaunaArtRule</c>): las siluetas de héroe, las
    /// banderas simplificadas y el retrato y la ventanita procedurales de TiTino solo salen en el Editor y en los builds de
    /// desarrollo. En release no se pinta nada hasta que exista su PNG (la ranura de <see cref="ArtSlots"/> manda siempre), y
    /// nunca va en la ficha, las capturas ni la UA. La única regla de «hay arte del Mapa», pura y comprobable.
    /// </summary>
    public static class MapArtRule
    {
        /// <summary>
        /// Fuerza <see cref="ShowProvisional"/> (tests: true = como en desarrollo, false = como en release); null = lo que diga el
        /// build.
        /// </summary>
        public static bool? ForceProvisional { get; set; }

        /// <summary>
        /// Si se puede pintar el arte provisional por código: <see cref="ForceProvisional"/> o, sin forzar, el Editor o un build
        /// de desarrollo (<see cref="Debug.isDebugBuild"/>).
        /// </summary>
        public static bool ShowProvisional => ForceProvisional ?? ShowProvisionalIn(Application.isEditor, Debug.isDebugBuild);

        /// <summary>La regla pura: el provisional sale en el Editor o en un build de desarrollo.</summary>
        public static bool ShowProvisionalIn(bool editor, bool developmentBuild)
        {
            return editor || developmentBuild;
        }

        /// <summary>
        /// Si la pieza puede salir: con su PNG final (<paramref name="finalPng"/>), siempre; sin él, solo con el provisional
        /// permitido (<paramref name="showProvisional"/>).
        /// </summary>
        public static bool CanShow(bool showProvisional, bool finalPng)
        {
            return finalPng || showProvisional;
        }
    }
}
