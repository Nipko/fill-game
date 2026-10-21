#nullable enable
using System;
using System.Security.Cryptography;
using System.Text;

namespace AF.Backend
{
    /// <summary>
    /// <c>token_hash</c> del contrato (<c>common.schema.json#/$defs/tokenHash</c>): SHA-256 del <c>purchaseToken</c> en UTF-8,
    /// en hexadecimal en minúsculas, igual que <c>tokenHash()</c> de <c>infra/functions/shared/crypto.ts</c>. El cliente compara
    /// <c>applied_purchases</c> con el libro mayor por este hash (08 §11).
    /// </summary>
    public static class PurchaseTokenHash
    {
        public static string Of(string purchaseToken)
        {
            if (purchaseToken == null)
            {
                throw new ArgumentNullException(nameof(purchaseToken));
            }

            return Sha256Hex(purchaseToken);
        }

        internal static string Sha256Hex(string value)
        {
            byte[] digest;
            using (SHA256 sha = SHA256.Create())
            {
                digest = sha.ComputeHash(Encoding.UTF8.GetBytes(value));
            }

            var sb = new StringBuilder(digest.Length * 2);
            for (int i = 0; i < digest.Length; i++)
            {
                sb.Append("0123456789abcdef"[digest[i] >> 4]).Append("0123456789abcdef"[digest[i] & 0xF]);
            }

            return sb.ToString();
        }
    }
}
