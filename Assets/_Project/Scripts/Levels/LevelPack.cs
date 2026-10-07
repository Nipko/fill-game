#nullable enable
using System;
using System.Collections.Generic;

namespace AF.Levels
{
    /// <summary>
    /// Paquete de niveles cargado (GDD 08 §4.1): cabecera del paquete y niveles ordenados por número.
    /// </summary>
    public sealed class LevelPack
    {
        private readonly LevelDefinition[] _levels;

        internal LevelPack(string packId, string kind, int world, string worldId, string waterBase, LevelDefinition[] levelsSorted)
        {
            if (levelsSorted.Length == 0)
            {
                throw new ArgumentException("Un paquete necesita al menos un nivel.", nameof(levelsSorted));
            }

            PackId = packId;
            Kind = kind;
            World = world;
            WorldId = worldId;
            WaterBase = waterBase;
            _levels = levelsSorted;
        }

        /// <summary>Identificador del paquete (p. ej. <c>tutorial_v0</c>).</summary>
        public string PackId { get; }

        /// <summary><c>campaign</c> o <c>daily</c>.</summary>
        public string Kind { get; }

        /// <summary>Mundo del paquete.</summary>
        public int World { get; }

        /// <summary>ID del mundo (<c>w01_estuario</c>…).</summary>
        public string WorldId { get; }

        /// <summary>Agua base del paquete (<c>black</c>, <c>white</c>, <c>clear</c>).</summary>
        public string WaterBase { get; }

        /// <summary>Niveles ordenados por número ascendente.</summary>
        public IReadOnlyList<LevelDefinition> Levels => _levels;

        /// <summary>Número de niveles.</summary>
        public int Count => _levels.Length;

        /// <summary>Primer nivel del paquete.</summary>
        public LevelDefinition First => _levels[0];

        /// <summary>Nivel con número <paramref name="level"/>, o null si el paquete no lo tiene.</summary>
        public LevelDefinition? Find(int level)
        {
            for (int i = 0; i < _levels.Length; i++)
            {
                if (_levels[i].Level == level)
                {
                    return _levels[i];
                }
            }

            return null;
        }

        /// <summary>
        /// Número del nivel que sigue a <paramref name="level"/>; tras el último vuelve al primero
        /// (prototipo v0: tras ganar el 10 se vuelve al 1, docs/dev/prototipo-v0.md c).
        /// </summary>
        public int NextLevelAfter(int level)
        {
            for (int i = 0; i < _levels.Length; i++)
            {
                if (_levels[i].Level > level)
                {
                    return _levels[i].Level;
                }
            }

            return _levels[0].Level;
        }
    }
}
