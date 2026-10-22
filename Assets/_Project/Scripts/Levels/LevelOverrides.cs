#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Levels.Json;

namespace AF.Levels
{
    /// <summary>
    /// Mecánica con kill switch: las claves <c>mech_enabled.{hoja,matapi,liana,remolino,barro,lluvia,brote,pirana,tinaja}</c>
    /// de GDD 08 §9, en el orden de la tabla (02 §9.2, regla 3; D-007; D-019).
    /// </summary>
    public enum MechanicSwitch
    {
        /// <summary><c>mech_enabled.hoja</c>: M1 Hoja y su variante Hoja atada [P].</summary>
        Hoja = 0,

        /// <summary><c>mech_enabled.matapi</c>: M10 Matapi.</summary>
        Matapi = 1,

        /// <summary><c>mech_enabled.liana</c>: M3 Liana y su variante Flor de vitória-régia [P].</summary>
        Liana = 2,

        /// <summary><c>mech_enabled.remolino</c>: M4 Remolino.</summary>
        Remolino = 3,

        /// <summary><c>mech_enabled.barro</c>: M5 Barro (el tinte 15 en <c>cuias</c>, no un <c>type</c> de <c>attrs</c>).</summary>
        Barro = 4,

        /// <summary><c>mech_enabled.lluvia</c>: M6 Lluvia.</summary>
        Lluvia = 5,

        /// <summary><c>mech_enabled.brote</c>: M2 Brote (con <c>n</c> = 1 o 2).</summary>
        Brote = 6,

        /// <summary><c>mech_enabled.pirana</c>: M7 Piraña.</summary>
        Pirana = 7,

        /// <summary><c>mech_enabled.tinaja</c>: M13 Tinaja y su cuia chica (<c>cucharon</c>).</summary>
        Tinaja = 8,
    }

    /// <summary>
    /// Estado de los kill switches <c>mech_enabled.*</c> (08 §9; tarea KS1): qué mecánicas están apagadas. El valor por
    /// defecto (<see cref="AllOn"/>) las tiene todas encendidas, como los defaults de 08 §9. Sin motor y sin asignaciones.
    /// </summary>
    /// <remarks>
    /// Las variantes de la 1.0 no tienen clave propia en 08 §9: la Hoja atada cuenta como <see cref="MechanicSwitch.Hoja"/>,
    /// la Flor como <see cref="MechanicSwitch.Liana"/> y el cucharón como <see cref="MechanicSwitch.Tinaja"/> [P]. Aguas turbias,
    /// la Cuia chica y la Cuia alta no tienen kill switch (08 §9).
    /// </remarks>
    public readonly struct MechanicSwitches : IEquatable<MechanicSwitches>
    {
        /// <summary>Número de kill switches de 08 §9.</summary>
        public const int Count = 9;

        /// <summary>Prefijo de las claves de Remote Config (08 §9).</summary>
        public const string KeyPrefix = "mech_enabled.";

        private const int AllBits = (1 << Count) - 1;

        private static readonly string[] Ids = { "hoja", "matapi", "liana", "remolino", "barro", "lluvia", "brote", "pirana", "tinaja" };

        private static readonly string[] Keys =
        {
            KeyPrefix + "hoja", KeyPrefix + "matapi", KeyPrefix + "liana", KeyPrefix + "remolino", KeyPrefix + "barro",
            KeyPrefix + "lluvia", KeyPrefix + "brote", KeyPrefix + "pirana", KeyPrefix + "tinaja",
        };

        // Bit i = la mecánica i está apagada: el default (0) es «todas encendidas».
        private readonly int _off;

        private MechanicSwitches(int off)
        {
            _off = off & AllBits;
        }

        /// <summary>Todas las mecánicas encendidas (los defaults de 08 §9).</summary>
        public static MechanicSwitches AllOn => default;

        /// <summary>Alguna mecánica está apagada.</summary>
        public bool AnyOff => _off != 0;

        /// <summary>Máscara de las mecánicas apagadas (bit <c>(int)</c><see cref="MechanicSwitch"/>).</summary>
        public int OffMask => _off;

        /// <summary>Id de la clave sin el prefijo: <c>"hoja"</c>, <c>"matapi"</c>… (08 §9).</summary>
        public static string IdOf(MechanicSwitch mechanic)
        {
            return Ids[Index(mechanic)];
        }

        /// <summary>Clave completa de Remote Config: <c>"mech_enabled.hoja"</c>… (08 §9).</summary>
        public static string KeyOf(MechanicSwitch mechanic)
        {
            return Keys[Index(mechanic)];
        }

        /// <summary>
        /// Convierte un id (<c>"hoja"</c>) o una clave completa (<c>"mech_enabled.hoja"</c>), sin cultura y sin distinguir
        /// espacios alrededor. Devuelve false con cualquier otro valor.
        /// </summary>
        public static bool TryParse(string? value, out MechanicSwitch mechanic)
        {
            mechanic = MechanicSwitch.Hoja;
            if (value == null)
            {
                return false;
            }

            string id = value.Trim();
            if (id.StartsWith(KeyPrefix, StringComparison.Ordinal))
            {
                id = id.Substring(KeyPrefix.Length);
            }

            for (int i = 0; i < Ids.Length; i++)
            {
                if (string.Equals(id, Ids[i], StringComparison.Ordinal))
                {
                    mechanic = (MechanicSwitch)i;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Apaga las mecánicas de una lista separada por comas (<c>"hoja,matapi"</c>; extra de desarrollo <c>af_mech_off</c>).
        /// Los elementos vacíos se ignoran; los desconocidos se devuelven en <paramref name="unknown"/> (null si no hay) y no
        /// apagan nada.
        /// </summary>
        public MechanicSwitches WithOffList(string? list, out string? unknown)
        {
            unknown = null;
            if (string.IsNullOrWhiteSpace(list))
            {
                return this;
            }

            MechanicSwitches result = this;
            string[] parts = list!.Split(',');
            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i].Trim();
                if (part.Length == 0)
                {
                    continue;
                }

                if (TryParse(part, out MechanicSwitch mechanic))
                {
                    result = result.With(mechanic, false);
                }
                else
                {
                    unknown = unknown == null ? part : unknown + "," + part;
                }
            }

            return result;
        }

        /// <summary>La mecánica <paramref name="mechanic"/> está encendida.</summary>
        public bool IsEnabled(MechanicSwitch mechanic)
        {
            return (_off & (1 << Index(mechanic))) == 0;
        }

        /// <summary>Copia con <paramref name="mechanic"/> encendida o apagada.</summary>
        public MechanicSwitches With(MechanicSwitch mechanic, bool enabled)
        {
            int bit = 1 << Index(mechanic);
            return new MechanicSwitches(enabled ? _off & ~bit : _off | bit);
        }

        /// <summary>Máscara de las mecánicas que usa <paramref name="level"/> en su tablero base que están apagadas (0 si ninguna).</summary>
        public int BlockedMask(LevelDefinition level)
        {
            return _off == 0 ? 0 : UsedMask(level) & _off;
        }

        /// <summary>
        /// Máscara de los kill switches que usa el tablero base de <paramref name="level"/>: <c>attrs</c> y el Barro de
        /// <c>cuias</c>. Las variantes de <c>relief</c> no añaden mecánicas (una cuia vacía más o un color menos, 03 §13).
        /// </summary>
        public static int UsedMask(LevelDefinition level)
        {
            if (level == null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            int mask = 0;
            IReadOnlyList<MechanicSpec> attrs = level.Attrs;
            for (int i = 0; i < attrs.Count; i++)
            {
                int bit = BitOf(attrs[i].Type);
                mask |= bit;
            }

            if (level.HasMud)
            {
                mask |= 1 << (int)MechanicSwitch.Barro;
            }

            return mask;
        }

        /// <summary>Ids de la máscara <paramref name="mask"/> separados por comas, en el orden de 08 §9 (para el registro).</summary>
        public static string Describe(int mask)
        {
            string result = string.Empty;
            for (int i = 0; i < Count; i++)
            {
                if ((mask & (1 << i)) != 0)
                {
                    result = result.Length == 0 ? Keys[i] : result + ", " + Keys[i];
                }
            }

            return result;
        }

        /// <inheritdoc />
        public bool Equals(MechanicSwitches other)
        {
            return _off == other._off;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is MechanicSwitches other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return _off;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return _off == 0 ? "mech_enabled: todas" : "mech_enabled apagadas: " + Describe(_off);
        }

        private static int BitOf(MechanicType type)
        {
            switch (type)
            {
                case MechanicType.Hoja:
                case MechanicType.HojaAtada:
                    return 1 << (int)MechanicSwitch.Hoja;
                case MechanicType.Matapi:
                    return 1 << (int)MechanicSwitch.Matapi;
                case MechanicType.Liana:
                case MechanicType.Flor:
                    return 1 << (int)MechanicSwitch.Liana;
                case MechanicType.Remolino:
                    return 1 << (int)MechanicSwitch.Remolino;
                case MechanicType.Lluvia:
                    return 1 << (int)MechanicSwitch.Lluvia;
                case MechanicType.Brote:
                    return 1 << (int)MechanicSwitch.Brote;
                case MechanicType.Pirana:
                    return 1 << (int)MechanicSwitch.Pirana;
                case MechanicType.Tinaja:
                case MechanicType.Cucharon:
                    return 1 << (int)MechanicSwitch.Tinaja;
                default:
                    return 0;
            }
        }

        private static int Index(MechanicSwitch mechanic)
        {
            int i = (int)mechanic;
            if (i < 0 || i >= Count)
            {
                throw new ArgumentOutOfRangeException(nameof(mechanic), mechanic, "Kill switch desconocido.");
            }

            return i;
        }
    }

    /// <summary>
    /// Tabla <c>level_overrides</c> (08 §9: json <c>{slot: level_id}</c>, default <c>{}</c>; 08 §4.4: los reemplazos de un
    /// nivel publicado solo van por <c>level_overrides</c> o por una vuelta nueva) resuelta a la <see cref="LevelDefinition"/> de
    /// sustitución de cada nivel de campaña (tarea KS1; D-020, pregunta 31 b). Sin motor; inmutable.
    /// </summary>
    /// <remarks>
    /// <para>En la 1.0 solo la usa el kill switch: <see cref="LevelCatalog"/> sirve el override de un nivel cuando el nivel usa una
    /// mecánica apagada (<c>mech_enabled.&lt;id&gt;</c> = false). El formato de respaldo por slot es de la 1.1 (D-020).</para>
    /// <para>Fuente: el valor de Remote Config de <c>level_overrides</c> (<c>RcLevelBinder</c>, en el arranque, antes de crear el
    /// catálogo) o, sin él, <see cref="LocalSource"/>; los tests pasan la suya. Cada <c>level_id</c> se busca en los paquetes
    /// dados, en su orden: los de campaña (<c>m{mm}_v{v}_n{nnnn}</c>, glosario §11) y los de reserva
    /// (<c>m{mm}_v{v}_n{nnnn}_r{k}</c>; D-033, añadido «la reserva del 20 %», Q-B11-7 a; <see cref="LevelPack.IsReserve"/>). Un
    /// tablero de reserva solo vale para su slot (<see cref="LevelDefinition.Level"/>, el del <c>replaces</c>): en otro se
    /// descarta, porque su ventana de dificultad es la de ese slot. Una entrada inválida (slot que no es un entero positivo, id
    /// que no es una cadena o que no está en ningún paquete, reserva en otro slot, slot repetido) no rompe el arranque: se
    /// descarta y se anota en <see cref="Problems"/>.</para>
    /// </remarks>
    public sealed class LevelOverrides
    {
        /// <summary>Clave de Remote Config (08 §9).</summary>
        public const string Key = "level_overrides";

        /// <summary>
        /// Fuente local de <c>level_overrides</c>: el default de 08 §9 (<c>{}</c>), vacía en release. Cuando exista
        /// <c>AF.RemoteConfig</c>, el valor llegará de <c>RC.Get(level_overrides)</c> y esta constante quedará de respaldo.
        /// </summary>
        public const string LocalSource = "{}";

        private static readonly string[] NoProblems = new string[0];

        private readonly int[] _slots;
        private readonly LevelDefinition[] _levels;
        private readonly string[] _problems;

        private LevelOverrides(int[] slots, LevelDefinition[] levels, string[] problems)
        {
            _slots = slots;
            _levels = levels;
            _problems = problems;
        }

        /// <summary>Tabla vacía (el default <c>{}</c> de 08 §9).</summary>
        public static LevelOverrides Empty { get; } = new LevelOverrides(new int[0], new LevelDefinition[0], NoProblems);

        /// <summary>Número de slots con sustitución.</summary>
        public int Count => _slots.Length;

        /// <summary>Slots con sustitución, de menor a mayor.</summary>
        public IReadOnlyList<int> Slots => _slots;

        /// <summary>Entradas descartadas al leer la tabla, una por línea de registro (vacío si no hay).</summary>
        public IReadOnlyList<string> Problems => _problems;

        /// <summary>
        /// Lee <paramref name="json"/> (<c>{"43": "m03_v1_n0044"}</c>; null o vacío = <c>{}</c>) y resuelve cada <c>level_id</c>
        /// en <paramref name="pool"/>. Un JSON roto o que no es un objeto da la tabla vacía con el problema anotado.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="pool"/> es null.</exception>
        public static LevelOverrides Create(string? json, IReadOnlyList<LevelPack> pool)
        {
            if (pool == null)
            {
                throw new ArgumentNullException(nameof(pool));
            }

            if (string.IsNullOrWhiteSpace(json))
            {
                return Empty;
            }

            JsonValue root;
            try
            {
                root = JsonParser.Parse(json!);
            }
            catch (FormatException e)
            {
                return new LevelOverrides(new int[0], new LevelDefinition[0], new[] { Key + ": no es JSON válido (" + e.Message + "); se ignora." });
            }

            if (root.Kind != JsonKind.Object)
            {
                return new LevelOverrides(new int[0], new LevelDefinition[0], new[] { Key + ": se esperaba un objeto {slot: level_id} y hay " + root.Kind + "; se ignora." });
            }

            var slots = new List<int>();
            var levels = new List<LevelDefinition>();
            var problems = new List<string>();
            IReadOnlyList<KeyValuePair<string, JsonValue>> members = root.Members;
            for (int i = 0; i < members.Count; i++)
            {
                string slotText = members[i].Key;
                JsonValue value = members[i].Value;
                if (!TryParseSlot(slotText, out int slot))
                {
                    problems.Add(Key + ": el slot \"" + slotText + "\" no es un número de nivel positivo; se ignora.");
                    continue;
                }

                if (slots.Contains(slot))
                {
                    problems.Add(Key + ": el slot " + slotText + " está repetido; vale el primero.");
                    continue;
                }

                if (value.Kind != JsonKind.String)
                {
                    problems.Add(Key + ": el slot " + slotText + " no lleva un level_id (cadena); se ignora.");
                    continue;
                }

                string id = value.AsString();
                LevelDefinition? level = FindById(pool, id);
                if (level == null)
                {
                    problems.Add(Key + ": el slot " + slotText + " pide " + id + ", que no está en ningún paquete; se ignora.");
                    continue;
                }

                if (level.IsReserve && level.Level != slot)
                {
                    problems.Add(Key + ": el slot " + slotText + " pide " + id + ", que es la reserva del slot "
                        + level.Level.ToString(CultureInfo.InvariantCulture) + " (" + level.Replaces + "); se ignora.");
                    continue;
                }

                slots.Add(slot);
                levels.Add(level);
            }

            int[] slotArray = slots.ToArray();
            LevelDefinition[] levelArray = levels.ToArray();
            Array.Sort(slotArray, levelArray);
            return new LevelOverrides(slotArray, levelArray, problems.Count == 0 ? NoProblems : problems.ToArray());
        }

        /// <summary>Nivel de sustitución del slot <paramref name="slot"/> (nivel de campaña). Sin asignaciones.</summary>
        public bool TryGet(int slot, out LevelDefinition level)
        {
            int i = Array.BinarySearch(_slots, slot);
            if (i < 0)
            {
                level = null!;
                return false;
            }

            level = _levels[i];
            return true;
        }

        private static bool TryParseSlot(string text, out int slot)
        {
            slot = 0;
            if (text.Length == 0 || text.Length > 9)
            {
                return false;
            }

            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] < '0' || text[i] > '9')
                {
                    return false;
                }
            }

            slot = int.Parse(text, NumberStyles.None, CultureInfo.InvariantCulture);
            return slot > 0;
        }

        private static LevelDefinition? FindById(IReadOnlyList<LevelPack> pool, string id)
        {
            for (int p = 0; p < pool.Count; p++)
            {
                LevelPack? pack = pool[p];
                if (pack == null)
                {
                    continue;
                }

                IReadOnlyList<LevelDefinition> levels = pack.Levels;
                for (int i = 0; i < levels.Count; i++)
                {
                    if (string.Equals(levels[i].Id, id, StringComparison.Ordinal))
                    {
                        return levels[i];
                    }
                }
            }

            return null;
        }
    }
}
