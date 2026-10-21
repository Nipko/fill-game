#nullable enable
using NUnit.Framework;

namespace AF.Save.Tests
{
    /// <summary>
    /// Las cuentas del HMAC del perfil (bloque B8; 08 §4.3 y §10.1): huella del dispositivo sin el id en claro, clave derivada de
    /// la huella, HMAC-SHA-256 en hexadecimal (con un vector de RFC 4231 para comprobar el algoritmo) y comparación en tiempo
    /// constante.
    /// </summary>
    public sealed class ProfileHmacTests
    {
        [Test]
        public void Sign_IsHmacSha256_Rfc4231Case2()
        {
            // RFC 4231, caso 2: clave "Jefe", datos "what do ya want for nothing?".
            byte[] key = System.Text.Encoding.ASCII.GetBytes("Jefe");
            Assert.That(ProfileHmac.Sign("what do ya want for nothing?", key),
                Is.EqualTo("5bdcc146bf60754e6a042426089575c75a003f089d2739839dec58b964ec3843"));
        }

        [Test]
        public void Fingerprint_IsStable_Lowercase32Hex_AndNeverTheId()
        {
            string a = ProfileHmac.DeviceFingerprint("android-1234");
            Assert.That(a, Is.EqualTo(ProfileHmac.DeviceFingerprint("android-1234")));
            Assert.That(ProfileHmac.IsFingerprint(a), Is.True);
            Assert.That(a.Length, Is.EqualTo(ProfileHmac.FingerprintLength));
            Assert.That(a, Does.Not.Contain("1234"));
            Assert.That(ProfileHmac.DeviceFingerprint("android-1235"), Is.Not.EqualTo(a));
            Assert.That(ProfileHmac.IsFingerprint(ProfileHmac.DeviceFingerprint(string.Empty)), Is.True, "sin id también hay huella");
        }

        [Test]
        public void Key_DependsOnTheFingerprint()
        {
            string f1 = ProfileHmac.DeviceFingerprint("a");
            string f2 = ProfileHmac.DeviceFingerprint("b");
            Assert.That(ProfileHmac.DeriveKey(f1), Has.Length.EqualTo(32));
            Assert.That(ProfileHmac.DeriveKey(f1), Is.EqualTo(ProfileHmac.DeriveKey(f1)));
            Assert.That(ProfileHmac.Sign("x", ProfileHmac.DeriveKey(f1)), Is.Not.EqualTo(ProfileHmac.Sign("x", ProfileHmac.DeriveKey(f2))));
        }

        [Test]
        public void Sign_ChangesWithAnyCharacter_AndHasTheSignatureShape()
        {
            byte[] key = ProfileHmac.DeriveKey(ProfileHmac.DeviceFingerprint("d"));
            string s1 = ProfileHmac.Sign("{\"seeds\": 100}", key);
            string s2 = ProfileHmac.Sign("{\"seeds\": 900}", key);
            Assert.That(ProfileHmac.IsSignature(s1), Is.True);
            Assert.That(s1, Is.Not.EqualTo(s2));
            Assert.That(ProfileHmac.Sign("ñá€🌴", key), Is.EqualTo(ProfileHmac.Sign("ñá€🌴", key)), "UTF-8, determinista");
        }

        [TestCase("abc", "abc", true)]
        [TestCase("abc", "abd", false)]
        [TestCase("abc", "ab", false)]
        [TestCase(null, "abc", false)]
        [TestCase("abc", null, false)]
        public void Matches_ComparesWholeStrings(string? a, string? b, bool expected)
        {
            Assert.That(ProfileHmac.Matches(a, b), Is.EqualTo(expected));
        }

        [TestCase(null, false)]
        [TestCase("", false)]
        [TestCase("0123456789abcdef0123456789abcdef", true)]
        [TestCase("0123456789ABCDEF0123456789abcdef", false)]
        [TestCase("0123456789abcdef0123456789abcde", false)]
        [TestCase("g123456789abcdef0123456789abcdef", false)]
        public void IsFingerprint_OnlyLowercaseHexOfTheRightLength(string? text, bool expected)
        {
            Assert.That(ProfileHmac.IsFingerprint(text), Is.EqualTo(expected));
        }
    }
}
