#nullable enable
using AF.Audio;
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Levels;
using AF.Presentation.Mechanics;

namespace AF.Presentation
{
    /// <summary>
    /// Ganchos de audio que leen el tablero (tarea AU1), para que cada gancho de la vista y de la UI sea una línea: la nota de la
    /// fruta de arriba al elegir una cuia, el «toc» de barro de la Tinaja, las capas del lecho según las cuias selladas y el
    /// sonido de cada paso de mecánica. Desde AU2, también la variante nocturna del lecho en Muy difícil y el ambiente del nivel.
    /// Leen el modelo (no lo cambian) y no asignan memoria.
    /// </summary>
    public static class AudioBoard
    {
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
        /// Se cargó el nivel <paramref name="level"/> (tarea AU2): su lecho (<see cref="Music"/>) y su ambiente, el agua de su
        /// mundo con la lluvia que le toca y su fauna (07 §9.1; <c>AF_STATE ambient</c>).
        /// </summary>
        public static void LevelLoaded(LevelDefinition level, BoardState board)
        {
            Music(level, board);
            AudioCues.LevelAmbient(WorldOf(level), level.Level);
        }

        /// <summary>
        /// El lecho del mundo del nivel <paramref name="level"/> con las capas de percusión que tocan con las cuias selladas de
        /// <paramref name="board"/> (07 §9.3: la primera al sellar la primera cuia y otra cada 25 %, hasta
        /// <c>audio_music_percussion_layers</c>) y, si el nivel es Muy difícil, su variante nocturna.
        /// </summary>
        public static void Music(LevelDefinition level, BoardState? board)
        {
            int layers = board != null ? PercussionLayers(board, AudioConfig.MusicPercussionLayers) : 0;
            AudioCues.SetMusic(WorldOf(level), layers, IsNight(level));
        }

        /// <summary>
        /// Capas de percusión de <paramref name="board"/>: cuias selladas sobre las frutas que hay que sellar (una por fruta del
        /// tablero), con <see cref="AudioMix.PercussionLayers"/> y el tope <paramref name="maxLayers"/>.
        /// </summary>
        public static int PercussionLayers(BoardState board, int maxLayers)
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

            return AudioMix.PercussionLayers(sealedCount, toSeal, maxLayers);
        }

        /// <summary>El nivel lleva la variante nocturna del lecho: es Muy difícil (07 §9.3; glosario §6.1).</summary>
        public static bool IsNight(LevelDefinition level)
        {
            return level.DifficultyLabel == LevelDefinition.LabelVeryHard;
        }

        // Mundo del nivel (el de la primera vuelta; 0 fuera de ella: sin lecho ni ambiente).
        private static int WorldOf(LevelDefinition level)
        {
            return level.Level >= 1 && level.Level <= WorldTable.LastLevel ? WorldTable.WorldOf(level.Level) : 0;
        }

        /// <summary>
        /// Empieza un paso de mecánica (<see cref="MechanicStepQueue"/>): su sonido de la lista (§3), en el golpe que lo dispara.
        /// La Liana de cuenta, el sellado (lo pone la tapa, <c>CuiaView</c>), la espera de la Lluvia y el mordisco perdido no suenan.
        /// La Tinaja (TJ3): el revelado de su boca suena <c>sfx_tinaja_reveal</c> (el de Aguas turbias no tiene sonido de paso) y la
        /// suelta, <c>sfx_tinaja_release</c> con <c>haptic_tinaja_release</c> en el paso de su cuia (Unhook); sacar suena en la
        /// cola (<c>BoardAnimator</c>).
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
                case MechanicStepKind.Reveal:
                    if (owner == MechanicType.Tinaja)
                    {
                        AudioCues.Play(SfxId.TinajaReveal); // la gota del revelado de la boca (02 §4.1; TJ3)
                    }

                    break;
                case MechanicStepKind.Unhook:
                    // La suelta (02 §4.1): roce de fibra y «toc» hueco con su háptico ligero, una vez; el vuelco de la tinaja (Upend),
                    // que va a la vez, no suena aparte.
                    AudioCues.Play(SfxId.TinajaRelease);
                    AudioCues.Haptic(HapticKind.TinajaRelease);
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
