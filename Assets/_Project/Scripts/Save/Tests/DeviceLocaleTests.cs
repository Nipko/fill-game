#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using AF.Platform;
using NUnit.Framework;
using UnityEngine;

namespace AF.Save.Tests
{
    /// <summary>
    /// Idioma del teléfono de <c>AF.Platform</c> (S12; D-018 Q7-A y Q8-A): <see cref="ManualDeviceLocale"/> solo cambia a
    /// mano, <see cref="SystemDeviceLocale"/> lee la cultura de .NET (la de la interfaz y, si no trae región, la regional
    /// del mismo idioma) y es la única lectura de la cultura del dispositivo en <c>AF.Platform</c> y <c>AF.Save</c>.
    /// </summary>
    public sealed class DeviceLocaleTests
    {
        // Se componen por partes para que este archivo no se denuncie a sí mismo.
        private static readonly string[] CultureReads = { "Current" + "Culture", "Current" + "UICulture" };

        [Test]
        public void ManualDeviceLocale_OnlyChangesWhenTold()
        {
            var phone = new ManualDeviceLocale("pt-BR");
            IDeviceLocale locale = phone;

            Assert.That(locale.LanguageTag, Is.EqualTo("pt-BR"));
            Assert.That(locale.LanguageTag, Is.EqualTo("pt-BR"), "leerlo dos veces no cambia nada");

            phone.LanguageTag = "es";
            Assert.That(locale.LanguageTag, Is.EqualTo("es"), "un teléfono sin región");

            using (CultureScope.Use(new CultureInfo("en-US")))
            {
                Assert.That(locale.LanguageTag, Is.EqualTo("es"), "no depende de la cultura del equipo");
            }

            Assert.That(new ManualDeviceLocale().LanguageTag, Is.EqualTo(string.Empty), "por defecto, el sistema no la da");
            Assert.Throws<ArgumentNullException>(() => new ManualDeviceLocale(null!));
            Assert.Throws<ArgumentNullException>(() => phone.LanguageTag = null!);
            Assert.That(phone.LanguageTag, Is.EqualTo("es"));
        }

        [TestCase("es-CO")]
        [TestCase("pt-BR")]
        [TestCase("en-US")]
        [TestCase("es-MX")]
        public void SystemDeviceLocale_ReadsTheCultureOfTheThread(string tag)
        {
            using (CultureScope.Use(new CultureInfo(tag)))
            {
                Assert.That(SystemDeviceLocale.Instance.LanguageTag, Is.EqualTo(tag));
            }
        }

        [Test]
        public void SystemDeviceLocale_WithTheInvariantCulture_GivesNoTag()
        {
            using (CultureScope.Use(CultureInfo.InvariantCulture))
            {
                Assert.That(SystemDeviceLocale.Instance.LanguageTag, Is.EqualTo(string.Empty));
            }
        }

        [Test]
        public void SystemDeviceLocale_TakesTheRegionFromTheRegionalCultureOfTheSameLanguage()
        {
            var es = new CultureInfo("es");
            var esCo = new CultureInfo("es-CO");
            var enUs = new CultureInfo("en-US");

            Assert.That(SystemDeviceLocale.TagOf(es, esCo), Is.EqualTo("es-CO"), "interfaz sin región: la regional del mismo idioma");
            Assert.That(SystemDeviceLocale.TagOf(es, enUs), Is.EqualTo("es"), "otro idioma: no se mezcla");
            Assert.That(SystemDeviceLocale.TagOf(enUs, esCo), Is.EqualTo("en-US"), "la interfaz con región manda");
            Assert.That(SystemDeviceLocale.TagOf(CultureInfo.InvariantCulture, esCo), Is.EqualTo("es-CO"));
            Assert.That(SystemDeviceLocale.TagOf(null, null), Is.EqualTo(string.Empty));

            CultureInfo ui = Thread.CurrentThread.CurrentUICulture;
            CultureInfo regional = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentUICulture = es;
                Thread.CurrentThread.CurrentCulture = esCo;
                Assert.That(SystemDeviceLocale.Instance.LanguageTag, Is.EqualTo("es-CO"));
            }
            finally
            {
                Thread.CurrentThread.CurrentUICulture = ui;
                Thread.CurrentThread.CurrentCulture = regional;
            }
        }

        [Test]
        public void OnlySystemDeviceLocale_ReadsTheDeviceCulture()
        {
            string scriptsDir = Path.Combine(Application.dataPath, "_Project", "Scripts");
            var offenders = new List<string>();
            foreach (string module in new[] { "Platform", "Save" })
            {
                string testsDir = Path.GetFullPath(Path.Combine(scriptsDir, module, "Tests")) + Path.DirectorySeparatorChar;
                foreach (string file in Directory.GetFiles(Path.Combine(scriptsDir, module), "*.cs", SearchOption.AllDirectories))
                {
                    if (Path.GetFileName(file) == "SystemDeviceLocale.cs"
                        || Path.GetFullPath(file).StartsWith(testsDir, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string text = File.ReadAllText(file);
                    foreach (string read in CultureReads)
                    {
                        if (text.Contains(read))
                        {
                            offenders.Add(Path.GetFileName(file) + ": " + read);
                        }
                    }
                }
            }

            Assert.That(offenders, Is.Empty, "el resto usa IDeviceLocale o InvariantCulture");
        }
    }
}
