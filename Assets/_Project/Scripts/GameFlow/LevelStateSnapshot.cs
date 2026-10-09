#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using AF.Core.Board;
using AF.Core.Rules;
using AF.Core.Session;
using AF.Levels;
using AF.Save;

namespace AF.GameFlow
{
    /// <summary>Pantalla del nivel que se reabre al volver a la app (02 §14; 04 §10.1 con D-008; tarea B2-5).</summary>
    public enum SavedPhase
    {
        /// <summary>LOADING, INTRO o PLAYING (y Sin canoas abierta desde la cabecera): se reabre el tablero, sin intro.</summary>
        Playing = 0,

        /// <summary>STUCK_MODAL: se reabre Aguas estancadas.</summary>
        StuckModal = 1,

        /// <summary>GIVE_UP_CONFIRM: se reabre ¿Seguro? (la canoa aún no se cobró).</summary>
        GiveUpConfirm = 2,

        /// <summary>LEVEL_FAILED (y Sin canoas abierta desde Nivel fallido): se reabre Nivel fallido (el fallo ya se resolvió).</summary>
        LevelFailed = 3,

        /// <summary>
        /// Se salió del nivel al inicio sin ganarlo (X de Nivel fallido, «Volver al inicio» o «Salir» en 1-30): la app abre el
        /// inicio y el nivel recuerda <c>fails_level</c> y la variante aliviada (04 §2.7), no las cuias añadidas (04 §2.2).
        /// </summary>
        Exited = 4,
    }

    /// <summary>Lo que lleva <c>level_state.history</c> además de los campos con nombre propio.</summary>
    public sealed class SavedHistory
    {
        /// <summary>Pantalla que se reabre.</summary>
        public SavedPhase Phase { get; internal set; }

        /// <summary>Cuias añadidas, en orden (persisten al reiniciar: 04 §2.2).</summary>
        public List<AddedCuiaKind> Added { get; } = new List<AddedCuiaKind>(2);

        /// <summary>La recarga gratis de la rama F [P] ya se dio en este nivel.</summary>
        public bool UndoGiftUsed { get; internal set; }

        /// <summary>Capi ya regaló la Cuia extra de este nivel (04 §2.2): no se repite al salir y volver a entrar.</summary>
        public bool ExtraCuiaGifted { get; internal set; }

        /// <summary>
        /// La Balsa de rescate ya se enseñó en este bloqueo (Aguas estancadas o ¿Seguro?): el modal reabierto la vuelve a
        /// ofrecer tal cual, sin contar otra impresión para <c>af_rescue_daily_cap</c> (02 §14; D-008).
        /// </summary>
        public bool BalsaShown { get; internal set; }

        /// <summary>Orígenes de los vertidos aplicados, en orden.</summary>
        public List<int> Sources { get; } = new List<int>(64);

        /// <summary>Destinos de los vertidos aplicados, en orden.</summary>
        public List<int> Targets { get; } = new List<int>(64);
    }

    /// <summary>
    /// Formato de <c>level_state</c> (04 §10.1; 08 §4.3) que escribe y lee la pista B (tarea B2-5; 02 R8 y §14: el nivel en
    /// curso se guarda tras cada vertido y se reabre tal cual, también con Aguas estancadas, ¿Seguro? o Nivel fallido
    /// abiertos, para que cerrar la app no sea una salida gratis del embudo de D-008).
    /// <list type="bullet">
    /// <item><description>Campos con nombre propio: <c>level</c> (número de campaña), <c>seed</c>
    /// (<c>amazonfill:v1:m{mm}:v{v}:n{nnnn}</c>, derivada del <c>id</c> del nivel), <c>variant</c> (<c>base</c> o el
    /// <c>type</c> de la entrada de <c>relief[]</c> servida), <c>fails_level</c>, <c>pours</c>, <c>extra_cuias</c> (todas las
    /// añadidas), <c>rescues_level</c>, <c>undo_free</c> (Marea gratis que queda), <c>revealed_cuias</c> (cuias con dosis
    /// ocultas que ya se destaparon; se rehacen solas al repetir los vertidos) y <c>stuck</c> (embudo abierto).</description></item>
    /// <item><description><c>history</c> (su formato lo fija quien reanuda el nivel, 04 §10.1): <c>h1|F|A|M|V</c>, con F la
    /// pantalla (<c>P</c>, <c>S</c>, <c>G</c>, <c>F</c> o <c>X</c>, <see cref="SavedPhase"/>), A las cuias añadidas en orden
    /// (<c>E</c> Cuia extra, <c>R</c> Cuia de rescate), M las marcas (<c>g</c>: recarga gratis de la rama F ya dada;
    /// <c>c</c>: Capi ya regaló la Cuia extra del nivel, que también se guarda al salir al inicio; <c>b</c>: la Balsa de
    /// rescate ya se enseñó en este bloqueo, solo con <c>S</c> o <c>G</c>, para reabrir el modal tal cual) y V los vertidos aplicados
    /// como pares de dígitos hexadecimales origen-destino (16 cuias como mucho). La fase va aquí porque el booleano
    /// <c>stuck</c> no basta (04 §10.1 con D-008) y <c>AF.Save</c> aún no tiene un campo <c>phase</c>; el regalo, porque el
    /// perfil aún no tiene un campo propio para él.</description></item>
    /// </list>
    /// Sin el tablero: se reconstruye con el nivel, la variante, las cuias añadidas y los vertidos, con las reglas del núcleo.
    /// </summary>
    public static class LevelStateSnapshot
    {
        /// <summary>Versión del formato de <c>history</c>.</summary>
        public const string HistoryVersion = "h1";

        private const char Separator = '|';
        private const char UndoGiftMark = 'g';
        private const char ExtraCuiaGiftMark = 'c';
        private const char BalsaShownMark = 'b';
        private const string HexDigits = "0123456789abcdef";

        /// <summary>
        /// Semilla canónica del nivel (08 §4.1): <c>amazonfill:v1:m{mm}:v{v}:n{nnnn}</c> a partir de su <c>id</c>
        /// <c>m{mm}_v{v}_n{nnnn}</c>; con otro <c>id</c>, <c>amazonfill:v1:id:{id}</c>. Con <c>af_level_offset</c> es la del
        /// tablero, no la del número desplazado: así un cambio de desplazamiento invalida el estado guardado.
        /// </summary>
        public static string SeedOf(LevelDefinition level)
        {
            if (level == null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            string id = level.Id;
            bool canonical = id.Length >= 11 && id[0] == 'm' && id.IndexOf("_v", StringComparison.Ordinal) > 0
                && id.IndexOf("_n", StringComparison.Ordinal) > 0 && id.IndexOf(':') < 0;
            return canonical ? "amazonfill:v1:" + id.Replace('_', ':') : "amazonfill:v1:id:" + id;
        }

        /// <summary><c>variant</c>: <c>base</c> o el <c>type</c> de <c>relief[reliefIndex]</c>.</summary>
        public static string VariantOf(LevelDefinition level, int reliefIndex)
        {
            return reliefIndex >= 0 && reliefIndex < level.Relief.Count ? level.Relief[reliefIndex].Type : "base";
        }

        /// <summary>Índice de <c>relief[]</c> de <paramref name="variant"/> (−1 = <c>base</c>; −2 si el nivel no lo tiene).</summary>
        public static int ReliefIndexOf(LevelDefinition level, string variant)
        {
            if (string.Equals(variant, "base", StringComparison.Ordinal))
            {
                return -1;
            }

            for (int i = 0; i < level.Relief.Count; i++)
            {
                if (string.Equals(level.Relief[i].Type, variant, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -2;
        }

        /// <summary>
        /// Escribe <c>history</c> en <paramref name="sb"/> (que se vacía antes). <paramref name="balsaShown"/> solo se anota con
        /// Aguas estancadas o ¿Seguro? (es del bloqueo en curso).
        /// </summary>
        public static void WriteHistory(
            StringBuilder sb, SavedPhase phase, IReadOnlyList<AddedCuiaKind> added, bool undoGiftUsed, bool extraCuiaGifted, LevelSession? session,
            bool balsaShown = false)
        {
            sb.Clear();
            sb.Append(HistoryVersion).Append(Separator).Append(PhaseCode(phase)).Append(Separator);
            for (int i = 0; i < added.Count; i++)
            {
                sb.Append(added[i] == AddedCuiaKind.Rescue ? 'R' : 'E');
            }

            sb.Append(Separator);
            if (undoGiftUsed)
            {
                sb.Append(UndoGiftMark);
            }

            if (extraCuiaGifted)
            {
                sb.Append(ExtraCuiaGiftMark);
            }

            if (balsaShown && (phase == SavedPhase.StuckModal || phase == SavedPhase.GiveUpConfirm))
            {
                sb.Append(BalsaShownMark);
            }

            sb.Append(Separator);
            if (session == null)
            {
                return;
            }

            for (int i = 0; i < session.AppliedPours; i++)
            {
                Pour pour = session.GetAppliedPour(i);
                sb.Append(HexDigits[pour.Source & 0xF]).Append(HexDigits[pour.Target & 0xF]);
            }
        }

        /// <summary>Lee <c>history</c>; false si no es de este formato o está roto.</summary>
        public static bool TryParseHistory(string? history, out SavedHistory parsed)
        {
            parsed = new SavedHistory();
            if (string.IsNullOrEmpty(history))
            {
                return false;
            }

            string[] parts = history!.Split(Separator);
            if (parts.Length != 5 || !string.Equals(parts[0], HistoryVersion, StringComparison.Ordinal) || parts[1].Length != 1
                || !TryPhase(parts[1][0], out SavedPhase phase))
            {
                return false;
            }

            parsed.Phase = phase;
            for (int i = 0; i < parts[2].Length; i++)
            {
                char c = parts[2][i];
                if (c == 'E')
                {
                    parsed.Added.Add(AddedCuiaKind.Extra);
                }
                else if (c == 'R')
                {
                    parsed.Added.Add(AddedCuiaKind.Rescue);
                }
                else
                {
                    return false;
                }
            }

            string marks = parts[3];
            for (int i = 0; i < marks.Length; i++)
            {
                char mark = marks[i];
                if (mark == UndoGiftMark && !parsed.UndoGiftUsed)
                {
                    parsed.UndoGiftUsed = true;
                }
                else if (mark == ExtraCuiaGiftMark && !parsed.ExtraCuiaGifted)
                {
                    parsed.ExtraCuiaGifted = true;
                }
                else if (mark == BalsaShownMark && !parsed.BalsaShown && (phase == SavedPhase.StuckModal || phase == SavedPhase.GiveUpConfirm))
                {
                    parsed.BalsaShown = true;
                }
                else
                {
                    return false; // marca desconocida o repetida
                }
            }

            string moves = parts[4];
            if (moves.Length % 2 != 0)
            {
                return false;
            }

            for (int i = 0; i < moves.Length; i += 2)
            {
                int source = HexDigits.IndexOf(moves[i]);
                int target = HexDigits.IndexOf(moves[i + 1]);
                if (source < 0 || target < 0 || source == target)
                {
                    return false;
                }

                parsed.Sources.Add(source);
                parsed.Targets.Add(target);
            }

            return true;
        }

        /// <summary>
        /// Cuias con dosis ocultas en <paramref name="initial"/> que ya no tienen las mismas en <paramref name="current"/>
        /// (<c>revealed_cuias</c>). Sin asignaciones si <paramref name="into"/> ya tiene capacidad.
        /// </summary>
        public static void WriteRevealed(BoardState initial, BoardState current, List<int> into)
        {
            into.Clear();
            int n = Math.Min(initial.Count, current.Count);
            for (int i = 0; i < n; i++)
            {
                int before = initial[i].HiddenMask;
                if (before != 0 && current[i].HiddenMask != before)
                {
                    into.Add(i);
                }
            }
        }

        /// <summary>Copia independiente de <paramref name="state"/>.</summary>
        public static LevelStateData Copy(LevelStateData state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var copy = new LevelStateData
            {
                Level = state.Level,
                Seed = state.Seed,
                Variant = state.Variant,
                FailsLevel = state.FailsLevel,
                Pours = state.Pours,
                ExtraCuias = state.ExtraCuias,
                RescuesLevel = state.RescuesLevel,
                UndoFree = state.UndoFree,
                History = state.History,
                Stuck = state.Stuck,
            };
            copy.RevealedCuias.AddRange(state.RevealedCuias);
            return copy;
        }

        /// <summary>Letra de <paramref name="phase"/> en <c>history</c>.</summary>
        public static char PhaseCode(SavedPhase phase)
        {
            switch (phase)
            {
                case SavedPhase.Playing: return 'P';
                case SavedPhase.StuckModal: return 'S';
                case SavedPhase.GiveUpConfirm: return 'G';
                case SavedPhase.LevelFailed: return 'F';
                case SavedPhase.Exited: return 'X';
                default: throw new ArgumentOutOfRangeException(nameof(phase), phase, null);
            }
        }

        private static bool TryPhase(char code, out SavedPhase phase)
        {
            switch (code)
            {
                case 'P': phase = SavedPhase.Playing; return true;
                case 'S': phase = SavedPhase.StuckModal; return true;
                case 'G': phase = SavedPhase.GiveUpConfirm; return true;
                case 'F': phase = SavedPhase.LevelFailed; return true;
                case 'X': phase = SavedPhase.Exited; return true;
                default: phase = SavedPhase.Playing; return false;
            }
        }
    }
}
