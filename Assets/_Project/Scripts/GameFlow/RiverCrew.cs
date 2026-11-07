#nullable enable
using AF.Levels;

namespace AF.GameFlow
{
    /// <summary>Quién viaja con Capi en el Mapa del río v2 (D-069, M2-4; propuesta del Mapa v2 §6.2).</summary>
    public enum CrewKind
    {
        /// <summary>El tucán: polizón en la proa (se une en Alter do Chão, nivel 21, mundo 2).</summary>
        Toucan = 0,

        /// <summary>El tucuxi (<i>Sotalia fluviatilis</i>): nada junto a Rosa (se une en el nivel 71, mundo 3).</summary>
        Tucuxi = 1,

        /// <summary>Un miquito de la tropa: solo un cameo desde un árbol de la orilla; nunca sube a la canoa (D-010; §6.1, regla 4).</summary>
        Monkey = 2,
    }

    /// <summary>Cómo va un compañero (propuesta §9.1: <c>bow</c>, <c>stern</c>, <c>swim</c> o <c>fly</c>; en la 1.1, los dos primeros que se usan).</summary>
    public enum CrewMode
    {
        /// <summary>En la proa de la canoa.</summary>
        Bow = 0,

        /// <summary>Nadando en el agua, junto al guía.</summary>
        Swim = 1,

        /// <summary>En un árbol de la orilla (el cameo de los miquitos).</summary>
        Tree = 2,
    }

    /// <summary>Un compañero de la tabla de §6.2: dónde se une (nivel y mundo), cómo va y su tamaño en la vista Mundo.</summary>
    public readonly struct CrewMember
    {
        /// <summary>Crea la fila.</summary>
        public CrewMember(CrewKind kind, int joinLevel, CrewMode mode, float sizeDp)
        {
            Kind = kind;
            JoinLevel = joinLevel;
            World = WorldTable.WorldOf(joinLevel);
            Mode = mode;
            SizeDp = sizeDp;
        }

        /// <summary>Quién es.</summary>
        public CrewKind Kind { get; }

        /// <summary>Se une al ganar este nivel (va a bordo con <c>max_level</c> ≥ este).</summary>
        public int JoinLevel { get; }

        /// <summary>Su mundo: va a bordo mientras la canoa está en él; después se queda en su tramo.</summary>
        public int World { get; }

        /// <summary>Cómo va.</summary>
        public CrewMode Mode { get; }

        /// <summary>Lo que mide en la vista Mundo (dp; el largo del cuerpo).</summary>
        public float SizeDp { get; }
    }

    /// <summary>
    /// La tripulación del Mapa del río v2 (D-069, bloque M2-4, P6 (a); <c>docs/dev/propuesta-mapa-rio-v2.md</c> §6.1-§6.3), en
    /// funciones puras: todo se deduce de <c>progress.max_level</c> y de <c>progress.map_seen_level</c>, sin campo nuevo en el
    /// guardado (§6.1, regla 6), y nada de economía (regla 5).
    /// <list type="bullet">
    /// <item><description><b>A bordo</b> (<see cref="IsAboard"/>, <see cref="Aboard"/>): un compañero que ya se unió va en la canoa
    /// mientras la canoa está en su mundo (el de <c>max_level</c>: con el 40 aún en el 2, así que el tucán llega al Encuentro y sale en
    /// el plano final del cambio de mundo); como mucho <see cref="MaxAboard"/>, los más recientes (regla 7). Rosa o el bufeo nadan
    /// delante aparte.</description></item>
    /// <item><description><b>En su tramo</b> (<see cref="StaysBehind"/>): al cambiar de mundo se queda en el sitio donde se unió y se
    /// le ve allí al volver.</description></item>
    /// <item><description><b>Presentación</b> (<see cref="IntroPending"/>): la primera vez que se abre el Mapa tras pasar su punto
    /// (<c>map_seen_level</c> &lt; su nivel ≤ <c>max_level</c>) y si aún va a bordo.</description></item>
    /// <item><description><b>Miquitos</b> (<see cref="CameoLevel"/>): nunca a bordo; en un viaje, uno saluda desde un árbol de la
    /// orilla a mitad de camino.</description></item>
    /// </list>
    /// Con <c>ui_map_crew</c> apagada (<c>crewOn</c> = false), nadie: el Mapa queda como en M2-3. Sin motor y sin asignaciones.
    /// </summary>
    public static class RiverCrew
    {
        /// <summary>Como mucho a bordo a la vez (§6.1, regla 7), sin contar a Rosa o al bufeo.</summary>
        public const int MaxAboard = 2;

        /// <summary>Nivel en que se une el tucán (Alter do Chão; §6.2 y D-069, P6 (a)).</summary>
        public const int ToucanJoinLevel = 21;

        /// <summary>Nivel en que se une el tucuxi (§6.2 y D-069, P6 (a)).</summary>
        public const int TucuxiJoinLevel = 71;

        /// <summary>Largo del tucán en la vista Mundo (dp; §6.2).</summary>
        public const float ToucanDp = 18f;

        /// <summary>
        /// Largo del tucuxi en la vista Mundo (dp; §6.2: «nada junto a Rosa, 34 dp»). Pendiente del dueño (pregunta 1 del añadido de
        /// M2-4 en D-069): Rosa mide 22 dp en el Mapa y el animal real es más pequeño que el boto (<c>fact.agua_tapajos.15</c>).
        /// </summary>
        public const float TucuxiDp = 34f;

        private static readonly CrewMember[] Members =
        {
            new CrewMember(CrewKind.Toucan, ToucanJoinLevel, CrewMode.Bow, ToucanDp),
            new CrewMember(CrewKind.Tucuxi, TucuxiJoinLevel, CrewMode.Swim, TucuxiDp),
        };

        /// <summary>Compañeros de la tabla (los que suben a bordo; los miquitos no son de la tabla).</summary>
        public static int Count => Members.Length;

        /// <summary>El compañero <paramref name="i"/> de la tabla, por orden de nivel.</summary>
        public static CrewMember At(int i)
        {
            return Members[i];
        }

        /// <summary>El compañero de <paramref name="kind"/> (el tucán o el tucuxi).</summary>
        /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="kind"/> no sube a bordo (los miquitos).</exception>
        public static CrewMember Of(CrewKind kind)
        {
            for (int i = 0; i < Members.Length; i++)
            {
                if (Members[i].Kind == kind)
                {
                    return Members[i];
                }
            }

            throw new System.ArgumentOutOfRangeException(nameof(kind), kind, "los miquitos nunca suben a la canoa (D-010)");
        }

        /// <summary>Los de <paramref name="kind"/> pueden ir a bordo (todos salvo los miquitos, D-010).</summary>
        public static bool BoardsTheCanoe(CrewKind kind)
        {
            return kind != CrewKind.Monkey;
        }

        /// <summary>Ya se unió con <paramref name="maxLevel"/> niveles ganados.</summary>
        public static bool Joined(in CrewMember m, int maxLevel, bool crewOn)
        {
            return crewOn && maxLevel >= m.JoinLevel;
        }

        /// <summary>
        /// Va a bordo con <paramref name="maxLevel"/> niveles ganados: ya se unió y la canoa sigue en su mundo (el de
        /// <paramref name="maxLevel"/>). No mira el tope de <see cref="MaxAboard"/> (eso es <see cref="Aboard"/>).
        /// </summary>
        public static bool IsAboard(in CrewMember m, int maxLevel, bool crewOn)
        {
            return Joined(m, maxLevel, crewOn) && maxLevel <= WorldTable.LastLevel && WorldTable.WorldOf(maxLevel) == m.World;
        }

        /// <summary>Se unió pero ya no va a bordo: se quedó en su tramo al cambiar de mundo (o por el tope de dos).</summary>
        public static bool StaysBehind(in CrewMember m, int maxLevel, bool crewOn)
        {
            return Joined(m, maxLevel, crewOn) && !IsAboardCapped(m.Kind, maxLevel, crewOn);
        }

        /// <summary>
        /// Los que van a bordo con <paramref name="maxLevel"/>, del más reciente al más antiguo, como mucho <see cref="MaxAboard"/> y
        /// nunca un miquito, en <paramref name="into"/> (≥ <see cref="MaxAboard"/>); devuelve cuántos. Sin asignaciones.
        /// </summary>
        public static int Aboard(int maxLevel, bool crewOn, CrewKind[] into)
        {
            int n = 0;
            for (int i = Members.Length - 1; i >= 0 && n < MaxAboard; i--)
            {
                if (IsAboard(Members[i], maxLevel, crewOn))
                {
                    into[n++] = Members[i].Kind;
                }
            }

            return n;
        }

        /// <summary><paramref name="kind"/> va a bordo con <paramref name="maxLevel"/>, con el tope de <see cref="MaxAboard"/>.</summary>
        public static bool IsAboardCapped(CrewKind kind, int maxLevel, bool crewOn)
        {
            if (!BoardsTheCanoe(kind))
            {
                return false;
            }

            int n = 0;
            for (int i = Members.Length - 1; i >= 0 && n < MaxAboard; i--)
            {
                if (IsAboard(Members[i], maxLevel, crewOn))
                {
                    if (Members[i].Kind == kind)
                    {
                        return true;
                    }

                    n++;
                }
            }

            return false;
        }

        /// <summary>
        /// Hay que presentarlo al abrir el Mapa con <paramref name="seenLevel"/> (<c>map_seen_level</c>, o desde dónde se repite el
        /// viaje) y <paramref name="maxLevel"/>: no se había visto el Mapa tras su punto y aún va a bordo [P] (si ya se quedó en su tramo,
        /// no se presenta: no se une a este viaje).
        /// </summary>
        public static bool IntroPending(in CrewMember m, int seenLevel, int maxLevel, bool crewOn)
        {
            return seenLevel < m.JoinLevel && IsAboardCapped(m.Kind, maxLevel, crewOn);
        }

        /// <summary>
        /// Dónde saluda el miquito en un viaje de <paramref name="fromLevel"/> a <paramref name="toLevel"/> (nivel fraccionario): a mitad
        /// de camino [P]; −1 sin viaje o con <c>ui_map_crew</c> apagada.
        /// </summary>
        public static float CameoLevel(float fromLevel, float toLevel, bool crewOn)
        {
            return crewOn && toLevel > fromLevel ? (fromLevel + toLevel) * 0.5f : -1f;
        }
    }
}
