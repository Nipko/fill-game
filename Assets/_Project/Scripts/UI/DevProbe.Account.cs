#nullable enable
using System.Text;

namespace AF.UI
{
    /// <summary>
    /// Sonda de «Eliminar cuenta y datos» y de la nube (bloque B8; <c>docs/dev/prototipo-v0.md</c>), solo en desarrollo:
    /// <list type="bullet">
    /// <item><c>AF_STATE {"event":"af_account_delete","t":…,"step":"requested","source":"app"}</c>: los pasos de
    /// <c>af_account_delete</c> de 08 §8.2 (<c>requested</c> al abrir el diálogo, <c>confirmed</c> al tocar «Eliminar»,
    /// <c>done</c> o <c>failed</c> al terminar el borrado local), sin identificadores.</item>
    /// <item><c>AF_STATE {"event":"af_save_sync","t":…,"result":"conflict_cloud_won","reason":"max_level","bytes":1234}</c>: la
    /// fusión con la nube al arrancar (08 §8.2 <c>af_save_sync</c>; <c>reason</c> es de la sonda [P]).</item>
    /// <item><c>AF_STATE {"event":"profile_integrity","t":…,"status":"valid"}</c>: si el perfil leído cuadra con su firma
    /// (<c>valid</c>, <c>unsigned</c> o <c>mismatch</c>; 08 §10.1, sin castigo automático).</item>
    /// <item><c>AF_LAYOUT</c> con <c>"screen":"delete_account"</c> y los ids <c>delete_account_close</c>,
    /// <c>delete_account_keep</c>, <c>delete_account_continue</c> o <c>delete_account_confirm</c>.</item>
    /// </list>
    /// </summary>
    public sealed partial class DevProbe
    {
        /// <summary>Escribe <c>AF_STATE af_account_delete</c> con el paso <paramref name="step"/>.</summary>
        public void AccountDelete(string step)
        {
            if (!Enabled)
            {
                return;
            }

            StringBuilder sb = _sb;
            sb.Clear();
            sb.Append("AF_STATE {\"event\":\"af_account_delete\"");
            AppendTime(sb);
            sb.Append(",\"step\":\"").Append(step).Append('"');
            sb.Append(",\"source\":\"app\"}");
            Write(sb.ToString());
        }

        /// <summary>Escribe <c>AF_STATE af_save_sync</c>.</summary>
        public void SaveSync(string result, string reason, int bytes)
        {
            if (!Enabled)
            {
                return;
            }

            StringBuilder sb = _sb;
            sb.Clear();
            sb.Append("AF_STATE {\"event\":\"af_save_sync\"");
            AppendTime(sb);
            sb.Append(",\"result\":\"").Append(result).Append('"');
            sb.Append(",\"reason\":\"").Append(reason).Append('"');
            sb.Append(",\"bytes\":").Append(bytes).Append('}');
            Write(sb.ToString());
        }

        /// <summary>Escribe <c>AF_STATE profile_integrity</c>.</summary>
        public void ProfileIntegrityStatus(string status)
        {
            if (!Enabled)
            {
                return;
            }

            StringBuilder sb = _sb;
            sb.Clear();
            sb.Append("AF_STATE {\"event\":\"profile_integrity\"");
            AppendTime(sb);
            sb.Append(",\"status\":\"").Append(status).Append("\"}");
            Write(sb.ToString());
        }
    }
}
