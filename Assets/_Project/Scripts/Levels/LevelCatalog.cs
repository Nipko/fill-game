#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;

namespace AF.Levels
{
    /// <summary>Un hueco del catálogo: los niveles <see cref="From"/>-<see cref="To"/> (ambos incluidos) no están en ningún paquete.</summary>
    public readonly struct LevelGap
    {
        /// <summary>Crea el hueco.</summary>
        public LevelGap(int from, int to)
        {
            From = from;
            To = to;
        }

        /// <summary>Primer nivel que falta.</summary>
        public int From { get; }

        /// <summary>Último nivel que falta.</summary>
        public int To { get; }

        /// <inheritdoc />
        public override string ToString()
        {
            return From == To
                ? From.ToString(CultureInfo.InvariantCulture)
                : From.ToString(CultureInfo.InvariantCulture) + "-" + To.ToString(CultureInfo.InvariantCulture);
        }
    }

    /// <summary>
    /// Catálogo de niveles de campaña: el índice nivel → paquete de GDD 08 §2.2 (<c>AF.Levels</c>) sobre uno o varios
    /// <see cref="LevelPack"/> (en la escena, <c>tutorial_v0</c> y <c>mundo_02</c> … <c>mundo_09</c>: 1-300 desde C1a y 1-700
    /// desde C1b, con los mundos 7-9 en la variante <see cref="PublishedVariant"/>). Resuelve cada número de nivel con su
    /// definición y su paquete, dice cuál es el siguiente disponible y detecta huecos y repetidos. Si un nivel no está en el
    /// catálogo (después del último o en un hueco), quien juega ve «El río sigue creciendo» (08 §6.2; 06 §9): el fin de
    /// contenido es dinámico, el último nivel publicado (<see cref="LastLevel"/>; el 700 con el mundo 9), y no hay ningún número
    /// fijo que cambiar al publicar otro paquete.
    /// </summary>
    /// <remarks>
    /// <para><see cref="LevelOffset"/> (solo builds de desarrollo, extra <c>af_level_offset</c> [P], tarea B1-5) desplaza el
    /// número de campaña: con 30, el nivel 31 se juega con el tablero del nivel 1 del paquete. Sirve para probar las
    /// compuertas de 11, 22 y 31 con los tableros del tutorial. En producción vale 0.</para>
    /// <para>Kill switch de mecánicas (tarea KS1; D-020, pregunta 31 b; 08 §2.2 y §9): con <c>mech_enabled_&lt;id&gt;</c> =
    /// false (<see cref="MechanicSwitches"/>), un nivel cuyo tablero base usa esa mecánica se sustituye al crear el catálogo por
    /// su entrada de <see cref="LevelOverrides"/> (<c>level_overrides</c>), si la hay y no usa a su vez una mecánica apagada. Si
    /// no la hay, se sirve el publicado y, la primera vez que se carga (<see cref="Serve"/>), se escribe un aviso [P]: el
    /// formato de respaldo por slot es de la 1.1. Con todas las mecánicas encendidas ningún nivel cambia, haya o no
    /// <c>level_overrides</c> [P]. <see cref="Find"/> y <see cref="Serve"/> devuelven ya el nivel servido; <see cref="PackOf"/>,
    /// el paquete del publicado.</para>
    /// <para>Los paquetes de reserva (<see cref="LevelPack.IsReserve"/>, <c>reserva_{mm}_v{v}</c>; D-033, añadido «la reserva del
    /// 20 %», Q-B11-7 a) nunca son niveles de campaña: <see cref="Create(IReadOnlyList{LevelPack}, int, LevelOverrides?, MechanicSwitches, Action{string}?, LevelLabelOverrides?)"/>
    /// los rechaza. Sus tableros solo llegan como sustitutos, por <see cref="LevelOverrides"/>.</para>
    /// <para>Inmutable salvo la marca de «aviso ya escrito». Las búsquedas son binarias y no asignan memoria.</para>
    /// </remarks>
    public sealed class LevelCatalog
    {
        /// <summary>Desplazamiento máximo admitido (cubre de sobra los 1.040 niveles de la vuelta 1, 06 §10).</summary>
        public const int MaxLevelOffset = 100000;

        /// <summary>
        /// Variante «a» de los mundos 7-9 (301-700; <c>campaign_v2.json</c>, D-023 punto 10 y D-027): con la Tinaja, que estrena
        /// en el 323 (D-019). Mientras no se publique, el generador la escribe con <c>--variant a</c> en
        /// <c>tools/out/salida_a/</c> (fuera de <c>Assets/</c>; GEN79), y los tests la leen de ahí.
        /// </summary>
        public const string VariantA = "a";

        /// <summary>
        /// Variante «b» de los mundos 7-9: la salida B de D-019 (Q-Ti3), sin la Tinaja, con el respaldo de cada celda (d.5.4).
        /// </summary>
        public const string VariantB = "b";

        /// <summary>
        /// Variante de los mundos 7-9 que se publica (tarea C1b; d.5.8): la del <c>published_variant</c> de
        /// <c>tools/level-generator/specs/campaign_v2.json</c>, con la que el generador escribe <c>Data/Levels/mundo_07_v1.json</c>
        /// a <c>mundo_09_v1.json</c> (los que enlaza <c>GameSceneBuilder.LevelPackPaths</c>) y se versiona el registro de
        /// combinaciones; un test EditMode comprueba que coinciden. La condición de D-019 (Q-Ti3; 03 §11; C1b, «Condición de la
        /// Tinaja»): se publica la «a» si TJ4 y la compuerta de PF1 (el factor de Morpho medido en un teléfono de clase A) están
        /// en verde; si no, la «b». Mientras la compuerta de PF1 no conste en verde, la «b» [P] (C1c, 2026-10-19: TJ4 en verde y PF1
        /// sin medir; pregunta abierta en D-019 de <c>docs/dev/decisiones.md</c>). Para publicar la Tinaja,
        /// todo antes de fusionar: <c>published_variant</c> = "a", regenerar el registro (<c>--registry</c>) y los tres paquetes
        /// en <c>Data/Levels/</c>, este valor a <see cref="VariantA"/> y regenerar la escena con
        /// <c>GameSceneBuilder.CreateGameScene</c>.
        /// </summary>
        public const string PublishedVariant = VariantB;

        /// <summary>La escena publica la Tinaja (estreno en el 323): <see cref="PublishedVariant"/> es <see cref="VariantA"/>.</summary>
        public static bool TinajaPublished => string.Equals(PublishedVariant, VariantA, StringComparison.Ordinal);

        private readonly LevelPack[] _packs;
        private readonly int[] _numbers;
        private readonly LevelDefinition[] _definitions;
        private readonly int[] _packOf;
        private readonly LevelGap[] _gaps;
        private readonly LevelDefinition[] _published;
        private readonly string?[]? _fallbackWarnings;
        private readonly bool[]? _warned;
        private readonly int[] _overridden;
        private readonly int[] _fallbacks;
        private readonly int[] _relabeled;
        private readonly Action<string>? _warn;

        private LevelCatalog(
            LevelPack[] packs,
            int[] numbers,
            LevelDefinition[] definitions,
            int[] packOf,
            LevelGap[] gaps,
            int levelOffset,
            LevelDefinition[] published,
            string?[]? fallbackWarnings,
            int[] overridden,
            int[] fallbacks,
            int[] relabeled,
            MechanicSwitches switches,
            Action<string>? warn)
        {
            _packs = packs;
            _numbers = numbers;
            _definitions = definitions;
            _packOf = packOf;
            _gaps = gaps;
            LevelOffset = levelOffset;
            _published = published;
            _fallbackWarnings = fallbackWarnings;
            _warned = fallbackWarnings == null ? null : new bool[fallbackWarnings.Length];
            _overridden = overridden;
            _fallbacks = fallbacks;
            _relabeled = relabeled;
            Switches = switches;
            _warn = warn;
        }

        /// <summary>Kill switches con los que se creó el catálogo (<see cref="MechanicSwitches.AllOn"/> por defecto).</summary>
        public MechanicSwitches Switches { get; }

        /// <summary>Niveles servidos con su override de <c>level_overrides</c> por una mecánica apagada, de menor a mayor.</summary>
        public IReadOnlyList<int> OverriddenLevels => _overridden;

        /// <summary>
        /// Niveles que usan una mecánica apagada y no tienen override válido: se sirve el publicado con aviso [P]. De menor a
        /// mayor.
        /// </summary>
        public IReadOnlyList<int> FallbackLevels => _fallbacks;

        /// <summary>
        /// Niveles servidos con la etiqueta de <c>lvl_label_override</c> en lugar de la del paquete (B11), de menor a mayor: el
        /// <c>source = override</c> de <c>af_label_shown</c> (08 §8.2) frente a <c>pack</c>.
        /// </summary>
        public IReadOnlyList<int> RelabeledLevels => _relabeled;

        /// <summary>Desplazamiento del número de campaña respecto al de los paquetes (0 en producción).</summary>
        public int LevelOffset { get; }

        /// <summary>Paquetes, en el orden en que se dieron.</summary>
        public IReadOnlyList<LevelPack> Packs => _packs;

        /// <summary>Número de niveles.</summary>
        public int Count => _numbers.Length;

        /// <summary>Primer nivel de campaña disponible.</summary>
        public int FirstLevel => _numbers[0];

        /// <summary>Último nivel de campaña disponible.</summary>
        public int LastLevel => _numbers[_numbers.Length - 1];

        /// <summary>Huecos entre <see cref="FirstLevel"/> y <see cref="LastLevel"/>, de menor a mayor (vacío si no hay).</summary>
        public IReadOnlyList<LevelGap> Gaps => _gaps;

        /// <summary>Los niveles de <see cref="FirstLevel"/> a <see cref="LastLevel"/> están todos.</summary>
        public bool IsContiguous => _gaps.Length == 0;

        /// <summary>Agua base del primer nivel (07 §2.1): la del arte que se genera al arrancar.</summary>
        public string WaterBase => _packs[_packOf[0]].WaterBase;

        /// <summary>Catálogo de un solo paquete, sin desplazamiento.</summary>
        public static LevelCatalog FromPack(LevelPack pack)
        {
            if (pack == null)
            {
                throw new ArgumentNullException(nameof(pack));
            }

            return Create(new[] { pack });
        }

        /// <summary>
        /// Crea el catálogo con <paramref name="packs"/>. Un número de nivel repetido en dos paquetes (o dos veces en el
        /// mismo, que ya rechaza <see cref="LevelPackReader"/>) es un error de datos.
        /// </summary>
        /// <exception cref="ArgumentException">No hay paquetes o alguno es null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="levelOffset"/> fuera de 0..<see cref="MaxLevelOffset"/>.</exception>
        /// <exception cref="LevelFormatException">Un nivel está en dos paquetes.</exception>
        public static LevelCatalog Create(IReadOnlyList<LevelPack> packs, int levelOffset = 0)
        {
            return Create(packs, levelOffset, null, MechanicSwitches.AllOn, null);
        }

        /// <summary>
        /// Crea el catálogo con <paramref name="packs"/> y aplica el kill switch (tarea KS1): cada nivel cuyo tablero base usa una
        /// mecánica apagada en <paramref name="switches"/> se sustituye por su entrada de <paramref name="overrides"/> si la hay y
        /// no usa a su vez una mecánica apagada; si no, se sirve el publicado y <see cref="Serve"/> escribe en
        /// <paramref name="warn"/> un aviso [P] la primera vez que lo carga. Los avisos se componen aquí, una vez: cargar un nivel
        /// no asigna memoria. Con <paramref name="labels"/> (<c>lvl_label_override</c>, 08 §9; tarea B11), cada nivel servido lleva
        /// la etiqueta de la tabla (<see cref="LevelLabelOverrides.Apply"/>): la que se muestra y paga (03 §7).
        /// </summary>
        /// <exception cref="ArgumentException">No hay paquetes o alguno es null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="levelOffset"/> fuera de 0..<see cref="MaxLevelOffset"/>.</exception>
        /// <exception cref="LevelFormatException">Un nivel está en dos paquetes, o un paquete es de reserva (D-033, Q-B11-7 a: solo
        /// se sirve desde <c>level_overrides</c>).</exception>
        public static LevelCatalog Create(
            IReadOnlyList<LevelPack> packs,
            int levelOffset,
            LevelOverrides? overrides,
            MechanicSwitches switches,
            Action<string>? warn,
            LevelLabelOverrides? labels = null)
        {
            return Create(packs, levelOffset, overrides, switches, warn, labels, null, CurveSelection.Off, null);
        }

        /// <summary>
        /// Como <see cref="Create(IReadOnlyList{LevelPack}, int, LevelOverrides?, MechanicSwitches, Action{string}?, LevelLabelOverrides?)"/>,
        /// con la curva 2 de la 1.1 (D-068, P1 B; 08 §4.4): cada nivel de 301-700 desde <see cref="CurveSelection.FromLevel"/> con
        /// <see cref="CurveSelection.Enabled"/> se sirve con el tablero de <paramref name="curvePacks"/> (<c>kind</c> =
        /// <c>curve</c>) cuyo <c>replaces</c> es el id publicado del slot, si lo hay. El kill switch va después, sobre el tablero que
        /// toca al jugador: si usa una mecánica apagada, (1) su entrada de <c>level_overrides</c> si es de su misma curva y no está
        /// bloqueada (08 §9: «se aplica encima de la curva que toque al jugador»); con la curva 2, además, (2) el tablero de
        /// <paramref name="curveReservePacks"/> (<c>reserva_curva_</c>) cuyo <c>replaces</c> es el suyo [P] y (3) el de la curva 1
        /// del slot, si no está bloqueado y tampoco su override [P]; y si nada vale, el aviso de siempre. Los paquetes de la curva
        /// fuera de 301-700 o sin slot publicado se ignoran con un aviso.
        /// </summary>
        /// <exception cref="ArgumentException">No hay paquetes o alguno es null.</exception>
        /// <exception cref="LevelFormatException">Un nivel está en dos paquetes, o un paquete es de reserva o de la curva (solo se
        /// sirven en lugar de otro).</exception>
        public static LevelCatalog Create(
            IReadOnlyList<LevelPack> packs,
            int levelOffset,
            LevelOverrides? overrides,
            MechanicSwitches switches,
            Action<string>? warn,
            LevelLabelOverrides? labels,
            IReadOnlyList<LevelPack>? curvePacks,
            CurveSelection curve,
            IReadOnlyList<LevelPack>? curveReservePacks)
        {
            if (packs == null || packs.Count == 0)
            {
                throw new ArgumentException("El catálogo necesita al menos un paquete.", nameof(packs));
            }

            if (levelOffset < 0 || levelOffset > MaxLevelOffset)
            {
                throw new ArgumentOutOfRangeException(nameof(levelOffset), levelOffset, "Desplazamiento fuera de 0.." + MaxLevelOffset.ToString(CultureInfo.InvariantCulture) + ".");
            }

            var packArray = new LevelPack[packs.Count];
            int total = 0;
            for (int p = 0; p < packs.Count; p++)
            {
                packArray[p] = packs[p] ?? throw new ArgumentException("Un paquete es null.", nameof(packs));
                if (packArray[p].IsReserve)
                {
                    throw new LevelFormatException(packArray[p].PackId + " es un paquete de reserva: sus tableros solo se sirven desde "
                        + LevelOverrides.Key + ", nunca como niveles de campaña (D-033, Q-B11-7 a).");
                }

                if (packArray[p].IsCurve)
                {
                    throw new LevelFormatException(packArray[p].PackId + " es un paquete de la curva 2: sus tableros solo se sirven en lugar de los "
                        + "de la curva 1 desde progress.curve2_from_level, nunca como niveles de campaña (D-068; 08 §4.4).");
                }

                total += packArray[p].Count;
            }

            var numbers = new int[total];
            var entries = new int[total];
            var definitions = new LevelDefinition[total];
            var packOf = new int[total];
            int n = 0;
            for (int p = 0; p < packArray.Length; p++)
            {
                IReadOnlyList<LevelDefinition> levels = packArray[p].Levels;
                for (int i = 0; i < levels.Count; i++)
                {
                    numbers[n] = levels[i].Level + levelOffset;
                    entries[n] = n;
                    definitions[n] = levels[i];
                    packOf[n] = p;
                    n++;
                }
            }

            // Orden por número de campaña: entries lleva el índice original para reordenar definiciones y paquetes.
            Array.Sort(numbers, entries);
            var sortedDefinitions = new LevelDefinition[total];
            var sortedPackOf = new int[total];
            for (int i = 0; i < total; i++)
            {
                sortedDefinitions[i] = definitions[entries[i]];
                sortedPackOf[i] = packOf[entries[i]];
            }

            var gaps = new List<LevelGap>();
            for (int i = 1; i < total; i++)
            {
                if (numbers[i] == numbers[i - 1])
                {
                    throw new LevelFormatException("El nivel " + numbers[i].ToString(CultureInfo.InvariantCulture) + " está en los paquetes "
                        + packArray[sortedPackOf[i - 1]].PackId + " y " + packArray[sortedPackOf[i]].PackId + ".");
                }

                if (numbers[i] > numbers[i - 1] + 1)
                {
                    gaps.Add(new LevelGap(numbers[i - 1] + 1, numbers[i] - 1));
                }
            }

            // La curva 2 (D-068): el tablero que toca al jugador en cada slot, antes del kill switch.
            LevelDefinition[] served = sortedDefinitions;
            var curve2 = new List<int>();
            if (curvePacks != null && curvePacks.Count > 0)
            {
                Dictionary<string, LevelDefinition> byReplaces = IndexByReplaces(curvePacks, isCurve: true, warn);
                served = (LevelDefinition[])sortedDefinitions.Clone();
                var used = new HashSet<string>(StringComparer.Ordinal);
                for (int i = 0; i < total; i++)
                {
                    if (!byReplaces.TryGetValue(sortedDefinitions[i].Id, out LevelDefinition curved))
                    {
                        continue;
                    }

                    used.Add(sortedDefinitions[i].Id);
                    if (!CurveSelection.InRange(numbers[i]))
                    {
                        warn?.Invoke("curva 2: " + curved.Id + " es del nivel " + numbers[i].ToString(CultureInfo.InvariantCulture)
                            + ", fuera de 301-700; se ignora (D-068, P1 B).");
                        continue;
                    }

                    if (curve.UsesCurve2(numbers[i]))
                    {
                        served[i] = curved;
                        curve2.Add(numbers[i]);
                    }
                }

                // Los huérfanos, en el orden de los paquetes (un registro determinista).
                for (int p = 0; p < curvePacks.Count; p++)
                {
                    IReadOnlyList<LevelDefinition> levels = curvePacks[p] == null || !curvePacks[p].IsCurve ? NoLevels : curvePacks[p].Levels;
                    for (int i = 0; i < levels.Count; i++)
                    {
                        string? replaces = levels[i].Replaces;
                        if (replaces != null && !used.Contains(replaces) && ReferenceEquals(byReplaces[replaces], levels[i]))
                        {
                            warn?.Invoke("curva 2: " + levels[i].Id + " sustituye a " + replaces + ", que no está en el catálogo; se ignora.");
                        }
                    }
                }
            }

            // Kill switch (KS1): con todo encendido no se mira nada y ningún nivel cambia.
            string?[]? warnings = null;
            var overridden = new List<int>();
            var fallbacks = new List<int>();
            if (switches.AnyOff)
            {
                LevelDefinition[] chosen = served;
                served = (LevelDefinition[])chosen.Clone();
                LevelOverrides table = overrides ?? LevelOverrides.Empty;
                Dictionary<string, LevelDefinition>? curveReserve = curveReservePacks != null && curveReservePacks.Count > 0
                    ? IndexByReplaces(curveReservePacks, isCurve: false, warn)
                    : null;
                for (int i = 0; i < total; i++)
                {
                    LevelDefinition current = chosen[i];
                    int blocked = switches.BlockedMask(current);
                    if (blocked == 0)
                    {
                        continue;
                    }

                    LevelDefinition? substitute = SubstituteFor(current, numbers[i], table, switches, out string reason);
                    if (substitute == null && current.Curve != LevelDefinition.Curve1)
                    {
                        // La curva 2 [P]: su reserva (reserva_curva_) y, si tampoco, el tablero de la curva 1 del slot o su override.
                        if (curveReserve != null && curveReserve.TryGetValue(current.Id, out LevelDefinition reserve) && switches.BlockedMask(reserve) == 0)
                        {
                            substitute = reserve;
                        }
                        else
                        {
                            LevelDefinition first = sortedDefinitions[i];
                            substitute = switches.BlockedMask(first) == 0 ? first : SubstituteFor(first, numbers[i], table, switches, out _);
                            if (substitute == null)
                            {
                                reason += "; tampoco hay respaldo en la reserva de la curva 2 ni en la curva 1";
                            }
                        }
                    }

                    if (substitute != null)
                    {
                        served[i] = substitute;
                        overridden.Add(numbers[i]);
                        continue;
                    }

                    warnings ??= new string?[total];
                    warnings[i] = "nivel " + numbers[i].ToString(CultureInfo.InvariantCulture) + " (" + current.Id + "): usa "
                        + MechanicSwitches.Describe(blocked) + " = false y " + reason
                        + "; se sirve el publicado [P] (D-020).";
                    fallbacks.Add(numbers[i]);
                }
            }

            // lvl_label_override (B11): sobre el nivel servido, sea el publicado o su override.
            var relabeled = new List<int>();
            if (labels != null && labels.Count > 0)
            {
                LevelDefinition[] before = served;
                served = labels.Apply(served, numbers);
                for (int i = 0; i < total; i++)
                {
                    if (!ReferenceEquals(served[i], before[i]))
                    {
                        relabeled.Add(numbers[i]);
                    }
                }
            }

            return new LevelCatalog(
                packArray,
                numbers,
                served,
                sortedPackOf,
                gaps.ToArray(),
                levelOffset,
                sortedDefinitions,
                warnings,
                overridden.ToArray(),
                fallbacks.ToArray(),
                relabeled.ToArray(),
                switches,
                warn)
            {
                Curve = curve,
                _curve2 = curve2.ToArray(),
            };
        }

        // Entrada de level_overrides del slot que vale para current (KS1): de su misma curva (08 §9) y sin mecánica apagada. Null
        // si no la hay, con el motivo en reason.
        private static LevelDefinition? SubstituteFor(LevelDefinition current, int slot, LevelOverrides table, MechanicSwitches switches, out string reason)
        {
            if (!table.TryGet(slot, out LevelDefinition replacement))
            {
                reason = "no tiene override en " + LevelOverrides.Key;
                return null;
            }

            if (replacement.Curve != current.Curve)
            {
                reason = "su override " + replacement.Id + " es de la curva " + replacement.Curve.ToString(CultureInfo.InvariantCulture)
                    + " y el jugador va por la " + current.Curve.ToString(CultureInfo.InvariantCulture);
                return null;
            }

            int stillBlocked = switches.BlockedMask(replacement);
            if (stillBlocked != 0)
            {
                reason = "su override " + replacement.Id + " también usa " + MechanicSwitches.Describe(stillBlocked) + " = false";
                return null;
            }

            reason = string.Empty;
            return replacement;
        }

        // Tableros de los paquetes indexados por su replaces: los de la curva 2 (kind curve) o los de su reserva (reserva_curva_).
        // Un paquete de otro tipo o un replaces repetido se ignora con un aviso (vale el primero).
        private static Dictionary<string, LevelDefinition> IndexByReplaces(IReadOnlyList<LevelPack> packs, bool isCurve, Action<string>? warn)
        {
            var result = new Dictionary<string, LevelDefinition>(StringComparer.Ordinal);
            for (int p = 0; p < packs.Count; p++)
            {
                LevelPack? pack = packs[p];
                if (pack == null)
                {
                    continue;
                }

                bool ok = isCurve ? pack.IsCurve : pack.IsReserve;
                if (!ok)
                {
                    warn?.Invoke(pack.PackId + " no es un paquete " + (isCurve ? "de la curva 2 (kind curve)" : "de reserva de la curva 2") + "; se ignora.");
                    continue;
                }

                IReadOnlyList<LevelDefinition> levels = pack.Levels;
                for (int i = 0; i < levels.Count; i++)
                {
                    LevelDefinition level = levels[i];
                    if (level.Replaces == null || (!isCurve && level.Curve == LevelDefinition.Curve1))
                    {
                        continue; // la reserva de la curva 1 no sustituye tableros de la curva 2
                    }

                    if (result.ContainsKey(level.Replaces))
                    {
                        warn?.Invoke(level.Id + ": " + level.Replaces + " ya tiene otro tablero en " + pack.PackId + "; vale el primero.");
                        continue;
                    }

                    result.Add(level.Replaces, level);
                }
            }

            return result;
        }

        private static readonly LevelDefinition[] NoLevels = new LevelDefinition[0];

        private int[] _curve2 = new int[0];

        /// <summary>Selector de la curva 2 con el que se creó el catálogo (<see cref="CurveSelection.Off"/> por defecto; D-068).</summary>
        public CurveSelection Curve { get; private set; }

        /// <summary>
        /// Niveles cuyo tablero sale de la curva 2 (antes del kill switch), de menor a mayor: los de 301-700 con tablero de la curva 2
        /// desde <see cref="CurveSelection.FromLevel"/> con <c>lvl_curve2_enabled</c> (D-068).
        /// </summary>
        public IReadOnlyList<int> Curve2Levels => _curve2;

        /// <summary>El nivel de campaña <paramref name="level"/> está en el catálogo.</summary>
        public bool Contains(int level)
        {
            return IndexOf(level) >= 0;
        }

        /// <summary>
        /// Definición servida del nivel de campaña <paramref name="level"/> (el override del kill switch si lo hay; KS1), o null si
        /// no está. No escribe avisos: para cargar el nivel, <see cref="Serve"/>.
        /// </summary>
        public LevelDefinition? Find(int level)
        {
            int i = IndexOf(level);
            return i < 0 ? null : _definitions[i];
        }

        /// <summary>
        /// Carga del nivel de campaña <paramref name="level"/> (LOADING o reanudación): la definición servida, como
        /// <see cref="Find"/>, y, si el nivel usa una mecánica apagada sin override, el aviso [P] la primera vez (KS1). Sin
        /// asignaciones: el aviso ya está compuesto.
        /// </summary>
        public LevelDefinition? Serve(int level)
        {
            int i = IndexOf(level);
            if (i < 0)
            {
                return null;
            }

            if (_fallbackWarnings != null && _warned != null && !_warned[i])
            {
                string? warning = _fallbackWarnings[i];
                if (warning != null)
                {
                    _warned[i] = true;
                    _warn?.Invoke(warning);
                }
            }

            return _definitions[i];
        }

        /// <summary>Definición publicada del nivel de campaña <paramref name="level"/> (sin overrides), o null si no está.</summary>
        public LevelDefinition? FindPublished(int level)
        {
            int i = IndexOf(level);
            return i < 0 ? null : _published[i];
        }

        /// <summary>
        /// El nivel de campaña <paramref name="level"/> se sirve con su override de <c>level_overrides</c> (KS1). Un nivel solo
        /// reetiquetado por <c>lvl_label_override</c> (B11) no cuenta: la lista de los sustituidos, no la identidad del nivel.
        /// Sin asignaciones.
        /// </summary>
        public bool IsOverridden(int level)
        {
            return Array.BinarySearch(_overridden, level) >= 0;
        }

        /// <summary>
        /// El nivel de campaña <paramref name="level"/> se sirve con la etiqueta de <c>lvl_label_override</c> (B11), no con la del
        /// paquete: <c>af_label_shown</c> con <c>source = override</c> (08 §8.2). Sin asignaciones.
        /// </summary>
        public bool IsRelabeled(int level)
        {
            return Array.BinarySearch(_relabeled, level) >= 0;
        }

        /// <summary>Paquete que contiene el nivel de campaña <paramref name="level"/> (el del publicado), o null si no está.</summary>
        public LevelPack? PackOf(int level)
        {
            int i = IndexOf(level);
            return i < 0 ? null : _packs[_packOf[i]];
        }

        /// <summary>Siguiente nivel de campaña (<paramref name="level"/> + 1) si está en el catálogo; si no, null (fin del contenido o hueco).</summary>
        public int? NextLevelAfter(int level)
        {
            if (level >= int.MaxValue)
            {
                return null;
            }

            int next = level + 1;
            return Contains(next) ? next : (int?)null;
        }

        /// <summary>Huecos como texto (<c>"3, 7-9"</c>) para el registro; vacío si no hay.</summary>
        public string DescribeGaps()
        {
            if (_gaps.Length == 0)
            {
                return string.Empty;
            }

            var parts = new string[_gaps.Length];
            for (int i = 0; i < _gaps.Length; i++)
            {
                parts[i] = _gaps[i].ToString();
            }

            return string.Join(", ", parts);
        }

        private int IndexOf(int level)
        {
            int i = Array.BinarySearch(_numbers, level);
            return i < 0 ? -1 : i;
        }
    }
}
