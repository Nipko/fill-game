#nullable enable
using System;

namespace AF.Save
{
    /// <summary>
    /// Fusión de los campos del perfil que añade el bloque B8 (parche <c>B8-profile.diff</c>; 08 §4.3 y §11), dentro del caso 7 de
    /// <see cref="CloudMerge"/> (nunca con el caso 8, el de integridad: un blob manipulado no aporta nada):
    /// <list type="bullet">
    /// <item><c>applied_purchases</c>: unión (08 §11, <c>merged.applied_purchases = union(...)</c>; ver Q-B8-1 de la nota);</item>
    /// <item><c>voided_purchases</c> [P]: unión, como <c>applied_purchases</c> (un token retirado en un teléfono no se vuelve a
    /// retirar en el otro);</item>
    /// <item><c>pending_rewards</c> [P]: los del ganador (cada provisional va con la cartera en la que entró: revocar uno del
    /// perdedor restaría algo que el ganador nunca recibió);</item>
    /// <item><c>integrity.fraud_flag</c> [P]: máximo; <c>device_first</c> y <c>hmac</c>, los del ganador (quien guarda vuelve a
    /// firmar);</item>
    /// <item><c>first_app_version</c> y <c>country_first</c> [P]: los del perfil creado antes (<c>created_at</c>), y si ese no los
    /// tiene, los del otro; <c>locale</c>: el del ganador (se pone al día en cada arranque).</item>
    /// </list>
    /// </summary>
    public static partial class CloudMerge
    {
        static partial void MergeAddedFields(Profile merged, Profile winner, Profile loser)
        {
            UnionSorted(merged.AppliedPurchases, loser.AppliedPurchases);
            UnionSorted(merged.VoidedPurchases, loser.VoidedPurchases);
            merged.Integrity.FraudFlag = Math.Max(merged.Integrity.FraudFlag, loser.Integrity.FraudFlag);

            Profile older = loser.CreatedAt < winner.CreatedAt ? loser : winner;
            Profile newer = ReferenceEquals(older, winner) ? loser : winner;
            merged.FirstAppVersion = older.FirstAppVersion ?? newer.FirstAppVersion;
            merged.CountryFirst = older.CountryFirst ?? newer.CountryFirst;
            merged.Locale = winner.Locale ?? loser.Locale;
        }
    }
}
