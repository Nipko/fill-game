#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;

namespace AF.Levels
{
    /// <summary>
    /// Pool del Reto del río (<c>daily_challenge</c>; 03 §16; 06 §4.1; 08 §4.1, <c>kind</c> = <c>daily</c>): un tablero por
    /// banda (1-3) y día del pool (0-728), leído por <see cref="DailyPoolReader"/>. Nota de diseño
    /// <c>docs/dev/reto-del-rio.md</c> §3, fila <c>AF.Levels</c>.
    /// </summary>
    /// <remarks>
    /// <para>Las consultas (<see cref="Find"/>, <see cref="FindById"/>, <see cref="HasBand"/>) no asignan memoria: un array de
    /// <see cref="Days"/> tableros por banda y una tabla por id construida al leer, que solo se consulta (nunca se recorre para
    /// decidir nada).</para>
    /// <para>Cada tablero es un <see cref="LevelDefinition"/> con <see cref="LevelDefinition.Level"/> = 0 (el Reto no tiene
    /// número de nivel de campaña), <see cref="LevelDefinition.Band"/> = null (la banda del Reto es <see cref="BandOf"/>, no la
    /// del ciclo de 20) y <see cref="LevelDefinition.Par"/> = <c>difficulty.par</c> si el paquete lo trae, o 0 (el pool se
    /// publica sin métricas, esquema <c>$defs/difficulty</c>).</para>
    /// </remarks>
    public sealed class DailyPool
    {
        /// <summary>Primera banda (nivel máximo &lt; 41; <c>lvl_daily_bracket_levels</c>, 03 §16).</summary>
        public const int MinBand = 1;

        /// <summary>Última banda (nivel máximo ≥ 201).</summary>
        public const int MaxBand = 3;

        /// <summary>
        /// Días del pool por banda: 729 = 81 × 9 (R-Q2b, decidida por el dueño el 2026-10-19; 03 §16 decía 730), para que el mundo
        /// del tablero, 1 + (día mod 9), siga a la fecha en todas las vueltas; 3 × 729 = 2.187 tableros. La única constante del
        /// tamaño del pool en el cliente: <c>DailyCalendar.PoolDays</c> y el lector la usan.
        /// </summary>
        public const int Days = 729;

        /// <summary>Valor de <c>kind</c> de un pool (08 §4.1).</summary>
        public const string KindDaily = "daily";

        // _boards[band - 1][day]; una banda ausente (solo con DailyPoolCoverage.PresentBands) es null.
        private readonly LevelDefinition?[]?[] _boards;
        private readonly Dictionary<string, LevelDefinition> _byId;
        private readonly Dictionary<LevelDefinition, int> _slotOf;

        internal DailyPool(string packId, int packSchema, LevelDefinition?[]?[] boards, Dictionary<string, LevelDefinition> byId, Dictionary<LevelDefinition, int> slotOf)
        {
            if (boards.Length != MaxBand)
            {
                throw new ArgumentException("Hace falta una entrada por banda.", nameof(boards));
            }

            PackId = packId;
            PackSchema = packSchema;
            _boards = boards;
            _byId = byId;
            _slotOf = slotOf;
            int count = 0;
            int bands = 0;
            for (int b = 0; b < boards.Length; b++)
            {
                LevelDefinition?[]? band = boards[b];
                if (band != null)
                {
                    count += band.Length;
                    bands++;
                }
            }

            Count = count;
            BandCount = bands;
        }

        /// <summary><c>pack_id</c> (p. ej. <c>daily_pool_v1</c>).</summary>
        public string PackId { get; }

        /// <summary><c>pack_schema</c> del paquete (1-<see cref="LevelPackReader.SupportedPackSchema"/>).</summary>
        public int PackSchema { get; }

        /// <summary>Número de tableros (2.187 con las 3 bandas).</summary>
        public int Count { get; }

        /// <summary>Bandas presentes (3 en un pool leído en modo estricto).</summary>
        public int BandCount { get; }

        /// <summary>El pool trae la banda <paramref name="band"/> (siempre completa, con sus <see cref="Days"/> días).</summary>
        public bool HasBand(int band)
        {
            return band >= MinBand && band <= MaxBand && _boards[band - 1] != null;
        }

        /// <summary>
        /// Tablero de la banda <paramref name="band"/> (1-3) y el día del pool <paramref name="day"/> (0-728), o null si la banda
        /// o el día están fuera de rango o la banda no está en el pool. No asigna memoria.
        /// </summary>
        public LevelDefinition? Find(int band, int day)
        {
            if (band < MinBand || band > MaxBand || day < 0 || day >= Days)
            {
                return null;
            }

            LevelDefinition?[]? boards = _boards[band - 1];
            return boards == null ? null : boards[day];
        }

        /// <summary>
        /// Tablero con el id <c>d{b}_n{nnnn}</c> (03 §16), o null si el pool no lo tiene (también con null o un texto que no
        /// es un id). Comparación ordinal, sin asignar memoria (la usan las sustituciones <c>daily_overrides</c>; nota §6).
        /// </summary>
        public LevelDefinition? FindById(string? id)
        {
            if (id == null)
            {
                return null;
            }

            return _byId.TryGetValue(id, out LevelDefinition? board) ? board : null;
        }

        /// <summary>
        /// Banda (1-3) de un tablero de este pool, o 0 si no es suyo. No asigna memoria.
        /// </summary>
        public int BandOf(LevelDefinition board)
        {
            return _slotOf.TryGetValue(board, out int slot) ? slot / Days + MinBand : 0;
        }

        /// <summary>
        /// Día del pool (0-728) de un tablero de este pool, o −1 si no es suyo. No asigna memoria.
        /// </summary>
        public int DayOf(LevelDefinition board)
        {
            return _slotOf.TryGetValue(board, out int slot) ? slot % Days : -1;
        }

        /// <summary>
        /// Id <c>d{b}_n{nnnn}</c> del tablero (03 §16; esquema, rama <c>daily</c>), con el día en cuatro cifras: <c>d2_n0417</c>.
        /// Copia de <c>tools/level-generator/Daily/DailyIds.Id</c>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Banda fuera de 1-3 o día fuera de 0-728.</exception>
        public static string IdOf(int band, int day)
        {
            CheckSlot(band, day);
            return "d" + band.ToString(CultureInfo.InvariantCulture) + "_n" + day.ToString("D4", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Semilla <c>amazonfill:v1:daily:b{b}:d{ddd}</c> del tablero (03 §16), con el día en tres cifras:
        /// <c>amazonfill:v1:daily:b2:d417</c>. Copia de <c>tools/level-generator/Daily/DailyIds.Seed</c>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Banda fuera de 1-3 o día fuera de 0-728.</exception>
        public static string SeedOf(int band, int day)
        {
            CheckSlot(band, day);
            return "amazonfill:v1:daily:b" + band.ToString(CultureInfo.InvariantCulture) + ":d" + day.ToString("D3", CultureInfo.InvariantCulture);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return "DailyPool(" + PackId + ", " + BandCount.ToString(CultureInfo.InvariantCulture) + " bandas, "
                + Count.ToString(CultureInfo.InvariantCulture) + " tableros)";
        }

        private static void CheckSlot(int band, int day)
        {
            if (band < MinBand || band > MaxBand)
            {
                throw new ArgumentOutOfRangeException(nameof(band), "banda " + band.ToString(CultureInfo.InvariantCulture) + " fuera de 1-3 (03 §16).");
            }

            if (day < 0 || day >= Days)
            {
                throw new ArgumentOutOfRangeException(nameof(day), "día " + day.ToString(CultureInfo.InvariantCulture) + " fuera de 0-" + (Days - 1).ToString(CultureInfo.InvariantCulture) + " (03 §16; R-Q2b).");
            }
        }
    }
}
