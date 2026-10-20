#nullable enable
using System.Globalization;
using System.Text;

namespace AF.UI
{
    /// <summary>
    /// Sonda del avistamiento de fauna (Q11 del guion de la intro, D-029; tarea DC15). Hasta que exista <c>AF.Analytics</c>
    /// (08 §8.2; con Firebase se conectará aquí, como <c>af_fact_page_open</c>), va al log de los builds de desarrollo como
    /// <c>AF_STATE</c>:
    /// <list type="bullet">
    /// <item><c>fauna_sighted</c> (<c>af_fauna_sighted</c> {species_id, level, first}), al quedar a la vista la tarjeta
    /// «¡Miquito a la vista!»: <c>species_id</c> (el id del catálogo de fauna, p. ej. <c>saimiri_collinsi</c>), <c>level</c>
    /// (el nivel en curso) y <c>first</c> (true si es la primera vez que se ve esa especie: entra en <c>fauna.sighted</c> del
    /// perfil; false si ya estaba y la lista no crece).</item>
    /// </list>
    /// Una línea por tarjeta que sale; sin tarjeta no hay avistamiento ni sonda. Solo identificadores, nunca texto visible
    /// (regla de localización).
    /// </summary>
    public sealed partial class DevProbe
    {
        internal void FaunaSighted(string speciesId, int level, bool first)
        {
            string? text = FormatFaunaSighted(speciesId, level, first);
            if (text != null)
            {
                Write(text);
            }
        }

        /// <summary>Texto de la línea <c>AF_STATE fauna_sighted</c>; null si la sonda no escribe.</summary>
        internal string? FormatFaunaSighted(string speciesId, int level, bool first)
        {
            if (!Enabled)
            {
                return null;
            }

            StringBuilder sb = _sb;
            sb.Clear();
            sb.Append("AF_STATE {\"event\":\"fauna_sighted\"");
            AppendTime(sb);
            sb.Append(",\"species_id\":");
            AppendKey(sb, speciesId);
            sb.Append(",\"level\":").Append(level.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"first\":").Append(first ? "true" : "false");
            sb.Append('}');
            return sb.ToString();
        }
    }
}
