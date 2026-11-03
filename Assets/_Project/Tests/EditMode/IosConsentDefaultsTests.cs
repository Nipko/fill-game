#nullable enable
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml;
using AF.EditorTools;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Lo que el <c>Info.plist</c> de iOS deja apagado hasta el consentimiento (fase M2 de <c>mac-ios.md</c>; 08 §2.3 y §7):
    /// <see cref="BuildIos.ConsentDefaults"/> lleva lo mismo que los <c>meta-data</c> de <c>LauncherManifest.xml</c> en Android, con
    /// el mismo valor, y <see cref="BuildIos.SetPlistBool"/> los escribe sin duplicar claves.
    /// </summary>
    public sealed class IosConsentDefaultsTests
    {
        private const string Manifest = "Assets/Plugins/Android/LauncherManifest.xml";
        private const string AndroidNs = "http://schemas.android.com/apk/res/android";

        private const string Plist = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<plist version=\"1.0\">\n<dict>\n"
            + "  <key>CFBundleName</key>\n  <string>Tintalú (dev)</string>\n</dict>\n</plist>\n";

        [Test]
        public void EveryAndroidConsentMetaData_HasItsIosKey_WithTheSameValue()
        {
            var doc = new XmlDocument();
            doc.Load(Manifest);
            var ns = new XmlNamespaceManager(doc.NameTable);
            ns.AddNamespace("android", AndroidNs);
            var android = new Dictionary<string, bool>(StringComparer.Ordinal);
            foreach (XmlElement meta in doc.SelectNodes("/manifest/application/meta-data", ns)!)
            {
                string name = meta.GetAttribute("name", AndroidNs);
                string value = meta.GetAttribute("value", AndroidNs);
                if (value == "true" || value == "false")
                {
                    android[name] = value == "true";
                }
            }

            var ios = new Dictionary<string, bool>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, bool> entry in BuildIos.ConsentDefaults)
            {
                ios[entry.Key] = entry.Value;
            }

            Assert.That(ios.Keys, Is.EquivalentTo(BuildIos.AndroidMetaDataFor.Keys), "cada clave de iOS dice de qué meta-data sale");
            int mirrored = 0;
            foreach (KeyValuePair<string, string?> pair in BuildIos.AndroidMetaDataFor)
            {
                if (pair.Value == null)
                {
                    Assert.That(ios[pair.Key], Is.False, pair.Key + ": solo de iOS, apagada");
                    continue;
                }

                Assert.That(android.ContainsKey(pair.Value), Is.True, pair.Value + " sigue en el manifiesto de Android");
                Assert.That(ios[pair.Key], Is.EqualTo(android[pair.Value]), pair.Key + " vale lo mismo que " + pair.Value);
                mirrored++;
            }

            foreach (string name in android.Keys)
            {
                if (name.StartsWith("firebase_", StringComparison.Ordinal) || name.StartsWith("google_analytics_", StringComparison.Ordinal)
                    || name == "com.google.android.gms.ads.DELAY_APP_MEASUREMENT_INIT")
                {
                    Assert.That(BuildIos.AndroidMetaDataFor.ContainsValue(name), Is.True, name + ": su clave de iOS falta en ConsentDefaults");
                }
            }

            Assert.That(mirrored, Is.EqualTo(8));
        }

        [Test]
        public void SetPlistBool_AddsTheKeyBeforeTheLastDict()
        {
            string result = BuildIos.SetPlistBool(Plist, "FIREBASE_ANALYTICS_COLLECTION_ENABLED", false);
            StringAssert.Contains("<key>FIREBASE_ANALYTICS_COLLECTION_ENABLED</key>\n  <false/>\n</dict>", result);
            StringAssert.Contains("<key>CFBundleName</key>", result, "lo demás, intacto");
        }

        [Test]
        public void SetPlistBool_ReplacesTheValue_WithoutDuplicatingTheKey()
        {
            string on = BuildIos.SetPlistBool(Plist, "GADDelayAppMeasurementInit", false);
            string result = BuildIos.SetPlistBool(BuildIos.SetPlistBool(on, "GADDelayAppMeasurementInit", true), "GADDelayAppMeasurementInit", true);
            Assert.That(Regex.Matches(result, "<key>GADDelayAppMeasurementInit</key>").Count, Is.EqualTo(1));
            StringAssert.Contains("<key>GADDelayAppMeasurementInit</key>\n    <true/>", result);
            StringAssert.DoesNotContain("<false/>", result);
        }

        [Test]
        public void SetPlistBool_Throws_WithoutADict()
        {
            Assert.Throws<InvalidOperationException>(() => BuildIos.SetPlistBool("<plist></plist>", "FirebaseCrashlyticsCollectionEnabled", false));
        }
    }
}
