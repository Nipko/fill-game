#nullable enable
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using AF.UI;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// «La intro nace del logo» (añadido de D-051 del 2026-10-28): la vista del icono que enseña el primer fotograma de la intro
    /// (<see cref="BrandIntroMorph"/>, <c>Resources/AF/Brand/af_icon_vista.bytes</c>) es, byte a byte, la del icono oficial
    /// (<c>docs/art/marca-oficial/v2/out/png/android-vista-432.png</c>, de <c>exportar.py</c>). Si el icono cambia, se copia otra vez.
    /// Sin Git LFS (el CI que no baja los binarios), la de <c>docs/</c> es un puntero: se compara su SHA-256.
    /// </summary>
    public sealed class BrandIconVistaTests
    {
        private const string Resource = "Assets/_Project/Resources/AF/Brand/af_icon_vista.bytes";
        private const string Official = "docs/art/marca-oficial/v2/out/png/android-vista-432.png";
        private const string LfsHeader = "version https://git-lfs.github.com/spec/v1";

        [Test]
        public void TheIntroIcon_IsTheOfficialIconVista_ByteForByte()
        {
            Assert.That(BrandIntroMorph.IconResource, Is.EqualTo("AF/Brand/af_icon_vista"), "Resources.Load de " + Resource);
            byte[] mine = File.ReadAllBytes(Resource);
            Assert.That(mine.Length, Is.GreaterThan(8));
            Assert.That(mine[0] == 0x89 && mine[1] == (byte)'P' && mine[2] == (byte)'N' && mine[3] == (byte)'G', Is.True, "un PNG de verdad, no un puntero de LFS");
            byte[] official = File.ReadAllBytes(Official);
            string head = Encoding.ASCII.GetString(official, 0, Math.Min(official.Length, LfsHeader.Length));
            if (head == LfsHeader)
            {
                string pointer = Encoding.ASCII.GetString(official);
                StringAssert.Contains("oid sha256:" + Sha256(mine), pointer, "el puntero de LFS de " + Official + " es el de esta copia");
                StringAssert.Contains("size " + mine.Length, pointer);
                return;
            }

            Assert.That(mine, Is.EqualTo(official), Resource + " debe ser una copia exacta de " + Official);
        }

        private static string Sha256(byte[] data)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(data);
                var sb = new StringBuilder(hash.Length * 2);
                foreach (byte b in hash)
                {
                    sb.Append(b.ToString("x2"));
                }

                return sb.ToString();
            }
        }
    }
}
