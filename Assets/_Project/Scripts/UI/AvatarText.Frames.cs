#nullable enable
using AF.LiveOps;
using AF.Localization;

namespace AF.UI
{
    /// <summary>
    /// Los textos de los marcos de país del avatar (Q-M6 de D-033, añadido del 2026-11-08): la fila de los marcos de «Tu avatar», su
    /// lectura de TalkBack y el nombre del avatar con su marco. El nombre de cada marco es el de su mundo (<c>world_wNN_name</c>).
    /// </summary>
    public static partial class AvatarText
    {
        public const string FramesTitleKey = "avatar_frames_title";
        public const string AnimalsTitleKey = "avatar_animals_title";
        public const string FrameNoneKey = "avatar_frame_none";
        public const string FrameNoneA11yKey = "avatar_frame_none_a11y";
        public const string FrameNoneChosenA11yKey = "avatar_frame_none_chosen_a11y";
        public const string FrameCellA11yKey = "avatar_frame_cell_a11y";
        public const string FrameChosenA11yKey = "avatar_frame_chosen_a11y";
        public const string FrameNewA11yKey = "avatar_frame_new_a11y";
        public const string WithFrameA11yKey = "avatar_with_frame_a11y";
        public const string HomeNewFrameA11yKey = "avatar_home_new_frame_a11y";

        /// <summary>Las claves de los marcos (entran en <see cref="AllKeys"/>).</summary>
        public static readonly string[] FrameKeys =
        {
            FramesTitleKey, AnimalsTitleKey, FrameNoneKey, FrameNoneA11yKey, FrameNoneChosenA11yKey, FrameCellA11yKey,
            FrameChosenA11yKey, FrameNewA11yKey, WithFrameA11yKey, HomeNewFrameA11yKey,
        };

        /// <summary>«Marcos de país».</summary>
        public static string FramesTitle => Loc.T(FramesTitleKey);

        /// <summary>«Animales».</summary>
        public static string AnimalsTitle => Loc.T(AnimalsTitleKey);

        /// <summary>El nombre bajo una celda de marco: el del mundo, o «Sin marco».</summary>
        public static string FrameName(AvatarFrameInfo? frame) => frame == null ? Loc.T(FrameNoneKey) : UiText.WorldName(frame.World);

        /// <summary>Lo que lee TalkBack de una celda de marco (null: «Sin marco»).</summary>
        public static string FrameCellA11y(AvatarFrameInfo? frame, bool chosen, bool isNew)
        {
            if (frame == null)
            {
                return Loc.T(chosen ? FrameNoneChosenA11yKey : FrameNoneA11yKey);
            }

            string key = chosen ? FrameChosenA11yKey : (isNew ? FrameNewA11yKey : FrameCellA11yKey);
            return Loc.T(key, LocArgs.Of("world", UiText.WorldName(frame.World)).And("country", UiText.WorldCountry(frame.World)));
        }

        /// <summary>
        /// El nombre del avatar para TalkBack: «Tucán» o, con marco, «Tucán con el marco de Estuario» (va en el <c>{animal}</c> de
        /// las frases del inicio, de Ajustes y de la cabecera).
        /// </summary>
        public static string NameWithFrame(AvatarInfo avatar, AvatarFrameInfo? frame)
        {
            return frame == null ? Name(avatar) : Loc.T(WithFrameA11yKey, LocArgs.Of("animal", Name(avatar)).And("world", UiText.WorldName(frame.World)));
        }

        /// <summary>
        /// «Tu avatar: Tucán con el marco de Estuario. Toca para cambiarlo»; con el punto, la frase de su causa: «Hay un avatar nuevo»
        /// (<paramref name="avatarDot"/>, que manda) o «Hay un marco nuevo» (<paramref name="frameDot"/>).
        /// </summary>
        public static string HomeA11y(AvatarInfo avatar, AvatarFrameInfo? frame, bool avatarDot, bool frameDot)
        {
            string key = avatarDot ? HomeNewA11yKey : (frameDot ? HomeNewFrameA11yKey : HomeA11yKey);
            return Loc.T(key, LocArgs.Of("animal", NameWithFrame(avatar, frame)));
        }

        /// <summary>La cabecera de «Tu avatar» para TalkBack, con el marco si lo lleva.</summary>
        public static string HeaderA11y(AvatarInfo avatar, AvatarFrameInfo? frame, string? nameKey, int nameNum, string? country)
        {
            if (nameKey == null)
            {
                return NameWithFrame(avatar, frame);
            }

            string player = LeagueText.Player(nameKey, nameNum);
            if (country != null)
            {
                player = Loc.T(LeagueText.RowPlayerKey, LocArgs.Of("player", player).And("country", LeagueText.Country(country)));
            }

            return Loc.T(HeaderA11yKey, LocArgs.Of("animal", NameWithFrame(avatar, frame)).And("player", player));
        }
    }
}
