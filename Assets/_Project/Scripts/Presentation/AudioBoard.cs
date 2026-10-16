#nullable enable
using AF.Audio;
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Presentation.Mechanics;

namespace AF.Presentation
{
    /// <summary>
    /// Ganchos de audio que leen el tablero (tarea AU1), para que cada gancho de la vista y de la UI sea una línea: la nota de la
    /// fruta de arriba al elegir una cuia, el «toc» de barro de la Tinaja, las capas del lecho según las cuias selladas y el
    /// sonido de cada paso de mecánica. Leen el modelo (no lo cambian) y no asignan memoria.
    /// </summary>
    public static class AudioBoard
    {
        /// <summary>
        /// <c>audio_music_percussion_layers</c> = 2 (08 §9; 07 §9.3): capas de percusión del lecho como mucho. Constante hasta que
        /// exista <c>AF.RemoteConfig</c>.
        /// </summary>
        public const int MusicPercussionLayers = 2;

        /// <summary>
        /// Se eligió la cuia <paramref name="index"/>: <c>sfx_select</c> afinado a la fruta de arriba (07 §9.2; 14 notas), o el
        /// «toc» de barro si es la Tinaja (D-019). La cima siempre se ve (R7); por si acaso, una oculta daría la nota neutra del
        /// laboratorio: el sonido nunca delata lo que no se ve.
        /// </summary>
        public static void Select(BoardState board, int index)
        {
            if ((uint)index >= (uint)board.Count)
            {
                return;
            }

            Cuia cuia = board[index];
            if (cuia.Mechanic == MechanicType.Tinaja)
            {
                AudioCues.Play(SfxId.TinajaSelect);
                return;
            }

            AudioCues.PlayFruit(SfxId.Select, TopTint(cuia));
        }

        /// <summary>
        /// El lecho del mundo <paramref name="world"/> con las capas de percusión que tocan con las cuias selladas de
        /// <paramref name="board"/> (07 §9.3: la primera al sellar la primera cuia y otra cada 25 %).
        /// </summary>
        public static void Music(int world, BoardState? board)
        {
            int layers = 0;
            if (board != null)
            {
                int sealedCount = 0;
                int toSeal = 0;
                for (int i = 0; i < board.Count; i++)
                {
                    if (board.IsSealed(i))
                    {
                        sealedCount++;
                    }
                }

                for (int color = 1; color <= SfxCatalog.FruitCount; color++)
                {
                    if (board.UnitsOf(color) > 0)
                    {
                        toSeal++;
                    }
                }

                layers = AudioMix.PercussionLayers(sealedCount, toSeal, MusicPercussionLayers);
            }

            AudioCues.SetMusic(world, layers);
        }

        /// <summary>
        /// Empieza un paso de mecánica (<see cref="MechanicStepQueue"/>): su sonido de la lista (§3), en el golpe que lo dispara.
        /// La Liana de cuenta, el sellado (lo pone la tapa, <c>CuiaView</c>), la espera de la Lluvia y el mordisco perdido no suenan.
        /// </summary>
        public static void Step(MechanicStepKind kind, MechanicType owner)
        {
            switch (kind)
            {
                case MechanicStepKind.MudWash:
                    AudioCues.Play(SfxId.MechBarro);
                    break;
                case MechanicStepKind.Receive:
                    AudioCues.Play(SfxId.MechRemolino);
                    break;
                case MechanicStepKind.RainDrop:
                case MechanicStepKind.RainEnd:
                    AudioCues.Play(SfxId.MechLluvia);
                    break;
                case MechanicStepKind.Bite:
                    AudioCues.Play(SfxId.MechPirana);
                    break;
                case MechanicStepKind.Open:
                    AudioCues.Play(owner == MechanicType.Brote ? SfxId.MechBrote : owner == MechanicType.HojaAtada ? SfxId.MechHojaAtada
                        : owner == MechanicType.Flor ? SfxId.MechFlor : SfxId.MechHoja);
                    break;
                case MechanicStepKind.Release:
                    AudioCues.Play(owner == MechanicType.Flor ? SfxId.MechFlor : owner == MechanicType.HojaAtada ? SfxId.MechHojaAtada : SfxId.MechLiana);
                    break;
                case MechanicStepKind.Seal:
                    if (owner == MechanicType.Matapi)
                    {
                        AudioCues.Play(SfxId.MechMatapi);
                    }

                    break;
            }
        }

        /// <summary>
        /// Fruta cuya nota suena para una cima de color <paramref name="topColor"/> (1-14), o 0, la nota neutra del laboratorio, si
        /// está oculta (<paramref name="topHidden"/>) o no es una fruta (agua, Barro): el sonido nunca delata lo que no se ve.
        /// Aparte de <see cref="Select"/> porque una cima oculta no se puede construir (R7) y la regla se prueba igual.
        /// </summary>
        public static int NoteTint(int topColor, bool topHidden)
        {
            return !topHidden && topColor >= 1 && topColor <= SfxCatalog.FruitCount ? topColor : 0;
        }

        // Fruta de la dosis de arriba (1-14), o 0 si está vacía, oculta o no es una fruta (agua, Barro).
        private static int TopTint(Cuia cuia)
        {
            return cuia.IsEmpty ? 0 : NoteTint(cuia.TopColor, cuia.IsHiddenAt(cuia.Count - 1));
        }
    }
}
