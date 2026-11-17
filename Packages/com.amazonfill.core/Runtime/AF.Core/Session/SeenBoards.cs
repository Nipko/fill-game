#nullable enable
using System.Collections.Generic;

namespace AF.Core.Session
{
    /// <summary>
    /// Vista de solo lectura de los tableros vistos en el intento (I6 de <see cref="LevelSession"/>; D-014 con sus añadidos del
    /// 2026-11-06 y del 2026-11-17) para <see cref="DeadEndProbe.IsStagnant"/>: un contador por hash canónico de 64 bits, visto si
    /// es mayor que 0. Para preguntar qué pasaría tras una Marea sin tocar el contador (<see cref="LevelSession.UndoEscapes"/>), la
    /// vista puede restar 1 al tablero que se deja y dar por visto el tablero al que se vuelve. Es un <c>readonly struct</c> que
    /// solo lee el diccionario (<see cref="Dictionary{TKey, TValue}.TryGetValue"/> sobre <c>ulong</c>): no asigna ni hace boxing.
    /// Nunca se decide iterándolo (CLAUDE.md, regla dura 1).
    /// </summary>
    internal readonly struct SeenBoards
    {
        private readonly Dictionary<ulong, int>? _counts;
        private readonly ulong _leaving;
        private readonly ulong _back;
        private readonly bool _afterUndo;

        private SeenBoards(Dictionary<ulong, int> counts, ulong leaving, ulong back, bool afterUndo)
        {
            _counts = counts;
            _leaving = leaving;
            _back = back;
            _afterUndo = afterUndo;
        }

        /// <summary>Los tableros vistos tal cual (contador mayor que 0).</summary>
        public static SeenBoards Of(Dictionary<ulong, int> counts)
        {
            return new SeenBoards(counts, 0UL, 0UL, false);
        }

        /// <summary>
        /// Los tableros vistos como quedarían tras una Marea: el tablero <paramref name="leaving"/> (el que se deshace) con 1 menos
        /// y el tablero <paramref name="back"/> (al que se vuelve, que queda en pantalla) visto.
        /// </summary>
        public static SeenBoards AfterUndo(Dictionary<ulong, int> counts, ulong leaving, ulong back)
        {
            return new SeenBoards(counts, leaving, back, true);
        }

        /// <summary>El tablero de hash canónico <paramref name="hash"/> está visto.</summary>
        public bool Contains(ulong hash)
        {
            if (_counts == null)
            {
                return false;
            }

            if (_afterUndo && hash == _back)
            {
                return true;
            }

            if (!_counts.TryGetValue(hash, out int count))
            {
                return false;
            }

            return !_afterUndo || hash != _leaving || count > 1;
        }
    }
}
