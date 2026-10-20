#nullable enable
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace AF.UI
{
    /// <summary>
    /// Sondas de la meta de B14 (tanda 5). Hasta que exista <c>AF.Analytics</c> (08 §8.2), van al log de los builds de desarrollo
    /// como <c>AF_STATE</c>, como las de B1a:
    /// <list type="bullet">
    /// <item><c>fruit_discovered</c> (<c>af_fruit_discovered {tint_id}</c>, 08 §8.2: «primer nivel en que aparece un tinte»), una
    /// línea por tinte nuevo al cargar un nivel de campaña: <c>tint_id</c> (el id del glosario §9, <c>acai</c>…) y, solo en la
    /// sonda, <c>tint</c> (su número de motor), <c>level</c> y <c>album</c> (<c>album.fruits</c> del perfil tras apuntarlo; 08 §4.3).</item>
    /// <item><c>skin_unlock</c> (<c>af_skin_unlock {skin_id, source}</c>, 08 §8.2), al ganar la skin de un mundo: <c>skin_id</c>,
    /// <c>source</c> (<c>world_milestone</c>) y, solo en la sonda, <c>level</c> y <c>equipped</c> (Q-M8 (a): siempre true).</item>
    /// <item><c>skin_state</c>, al arrancar: <c>equipped</c> (la skin que llevan las cuias), <c>owned</c> (<c>entitlements.skins</c>)
    /// y <c>backfilled</c> (las de mundos ya cerrados que se dieron al arrancar; 0 casi siempre).</item>
    /// </list>
    /// Solo claves e identificadores, nunca texto visible (regla de localización).
    /// </summary>
    public sealed partial class DevProbe
    {
        internal void FruitDiscovered(string tintId, int tint, int level, IReadOnlyList<int> album)
        {
            string? text = FormatFruitDiscovered(tintId, tint, level, album);
            if (text != null)
            {
                Write(text);
            }
        }

        /// <summary>Texto de la línea <c>AF_STATE fruit_discovered</c>; null si la sonda no escribe.</summary>
        internal string? FormatFruitDiscovered(string tintId, int tint, int level, IReadOnlyList<int> album)
        {
            if (!Enabled)
            {
                return null;
            }

            StringBuilder sb = _sb;
            sb.Clear();
            sb.Append("AF_STATE {\"event\":\"fruit_discovered\"");
            AppendTime(sb);
            sb.Append(",\"tint_id\":");
            AppendKey(sb, tintId);
            sb.Append(",\"tint\":").Append(tint.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"level\":").Append(level.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"album\":[");
            for (int i = 0; i < album.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(',');
                }

                sb.Append(album[i].ToString(CultureInfo.InvariantCulture));
            }

            sb.Append("]}");
            return sb.ToString();
        }

        internal void SkinUnlock(string skinId, string source, int level, bool equipped)
        {
            string? text = FormatSkinUnlock(skinId, source, level, equipped);
            if (text != null)
            {
                Write(text);
            }
        }

        /// <summary>Texto de la línea <c>AF_STATE skin_unlock</c>; null si la sonda no escribe.</summary>
        internal string? FormatSkinUnlock(string skinId, string source, int level, bool equipped)
        {
            if (!Enabled)
            {
                return null;
            }

            StringBuilder sb = _sb;
            sb.Clear();
            sb.Append("AF_STATE {\"event\":\"skin_unlock\"");
            AppendTime(sb);
            sb.Append(",\"skin_id\":");
            AppendKey(sb, skinId);
            sb.Append(",\"source\":");
            AppendKey(sb, source);
            sb.Append(",\"level\":").Append(level.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"equipped\":").Append(equipped ? "true" : "false");
            sb.Append('}');
            return sb.ToString();
        }

        internal void SkinState(string equipped, IReadOnlyList<string> owned, int backfilled)
        {
            string? text = FormatSkinState(equipped, owned, backfilled);
            if (text != null)
            {
                Write(text);
            }
        }

        /// <summary>Texto de la línea <c>AF_STATE skin_state</c>; null si la sonda no escribe.</summary>
        internal string? FormatSkinState(string equipped, IReadOnlyList<string> owned, int backfilled)
        {
            if (!Enabled)
            {
                return null;
            }

            StringBuilder sb = _sb;
            sb.Clear();
            sb.Append("AF_STATE {\"event\":\"skin_state\"");
            AppendTime(sb);
            sb.Append(",\"equipped\":");
            AppendKey(sb, equipped);
            sb.Append(",\"owned\":[");
            for (int i = 0; i < owned.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(',');
                }

                AppendKey(sb, owned[i]);
            }

            sb.Append("],\"backfilled\":").Append(backfilled.ToString(CultureInfo.InvariantCulture));
            sb.Append('}');
            return sb.ToString();
        }
    }
}
