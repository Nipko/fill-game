#nullable enable
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Regla del arte por código (D-052, que retira P-M5 de D-023; como <c>FaunaArtRule</c>): TiTino (retrato, rig, ventanita
    /// y casa del Mapa), las siluetas de héroe, las banderas simplificadas, la tinaja, el cucharón y los miquitos son el arte
    /// final y salen en todos los builds, release incluido, y también en la ficha, las capturas y la UA. La ranura de
    /// <see cref="ArtSlots"/> sigue mandando: si llega un PNG, sustituye al dibujo por código. La única regla de «hay arte»,
    /// pura y comprobable.
    /// </summary>
    public static class MapArtRule
    {
        /// <summary>
        /// Fuerza <see cref="ShowProvisional"/> (tests: true = con el arte por código, false = sin él, para recorrer el camino de
        /// «solo el PNG» o «nada»); null = lo que diga la regla (<see cref="ShowProvisionalIn"/>: siempre sí).
        /// </summary>
        public static bool? ForceProvisional { get; set; }

        /// <summary>
        /// Si se puede pintar el arte por código: <see cref="ForceProvisional"/> o, sin forzar, la regla de
        /// <see cref="ShowProvisionalIn"/> para este build.
        /// </summary>
        public static bool ShowProvisional => ForceProvisional ?? ShowProvisionalIn(Application.isEditor, Debug.isDebugBuild);

        /// <summary>
        /// La regla pura: el arte por código sale en cualquier build, en el Editor, en desarrollo y en release (D-052). Los
        /// parámetros se quedan para que la regla diga explícitamente qué pasa en cada build y los tests lo comprueben.
        /// </summary>
        public static bool ShowProvisionalIn(bool editor, bool developmentBuild)
        {
            return true;
        }

        /// <summary>
        /// Si la pieza puede salir: con su PNG final (<paramref name="finalPng"/>), siempre (y manda sobre el dibujo); sin él, con
        /// el arte por código permitido (<paramref name="showProvisional"/>).
        /// </summary>
        public static bool CanShow(bool showProvisional, bool finalPng)
        {
            return finalPng || showProvisional;
        }
    }
}
