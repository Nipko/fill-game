#nullable enable
using AF.GameFlow;
using AF.Save;
using NUnit.Framework;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// El bocadillo <c>map_l10_intro</c> del Mapa del 10 en el árbitro de TiTino (tarea DC5; D-028, pregunta 60 b de GDD §6.3;
    /// D-016, punto 5): fuera del tope de <c>ui_sloth_lines_per_session_max</c> frases (ni suma a la sesión ni toca
    /// <c>sloth.last_line</c>) y dentro de «nunca en dos pantallas seguidas». En el inicio que lo sigue, el saludo del día que
    /// tocaba no sale ni se gasta (ni <c>sloth.last_greeting_date</c> ni <c>sloth.last_seen_date</c>): queda para la visita
    /// siguiente. Ninguna otra frase cambia de regla.
    /// </summary>
    public sealed class SlothMapIntroTests
    {
        private const string Today = "2026-10-19";
        private const string Yesterday = "2026-10-18";
        private const string TwoDaysAgo = "2026-10-17";
        private const string WeekAgo = "2026-10-12";

        private static int s_sink;

        [Test]
        public void MapIntro_DoesNotSpendASessionLine_ButCountsAsAScreenThatSpoke()
        {
            var data = new SlothData();
            var session = new SlothSession(data, true, true, true, 1, 4);
            int[] lastBefore = (int[])data.LastLine.Clone();

            session.RecordMapIntro();

            Assert.That(session.LinesThisSession, Is.EqualTo(0), "el bocadillo no gasta ninguna de las 4 frases");
            Assert.That(data.LastLine, Is.EqualTo(lastBefore), "ni es una sloth_line_*: sloth.last_line intacto");
            Assert.That(session.PreviousScreenSpoke, Is.True, "cuenta como pantalla en la que TiTino habló");
            Assert.That(session.PreviousScreenMapIntro, Is.True);
            SlothContext c = session.Context;
            Assert.That(c.PreviousScreenSpoke && c.PreviousScreenMapIntro, Is.True, "el contexto lo lleva");
            Assert.That(c.LinesThisSession, Is.EqualTo(0));
        }

        [Test]
        public void HomeAfterTheMapIntro_MorningGreeting_IsNotShownNorSpent_AndComesOnTheNextVisit()
        {
            var data = new SlothData { LastSeenDate = Yesterday };
            var session = new SlothSession(data, true, true, true, 4, 4);
            session.RecordMapIntro();

            // El inicio tras cerrar el Mapa del 10: el saludo de la mañana tocaba, pero se aplaza.
            SlothContext c = session.Context.WithHome(session.HomeFacts(Today, 8, 11));
            SlothLine home = SlothLines.ForHome(in c);
            Assert.That(home.Deferred, Is.True, "el saludo que tocaba, aplazado");
            Assert.That(home.Greeting, Is.False, "no cuenta como el saludo del día");
            Assert.That(home.Speaks, Is.False, "sin frase");
            Assert.That(home.Gesture, Is.EqualTo(SlothGesture.None), "ni sloth_wave");
            session.Record(in home, in c);
            Assert.That(data.LastGreetingDate, Is.Null, "sloth.last_greeting_date no se escribe");
            Assert.That(data.LastSeenDate, Is.EqualTo(Yesterday), "ni sloth.last_seen_date: hoy sigue sin abrirse");
            Assert.That(session.LinesThisSession, Is.EqualTo(0), "contador intacto");
            Assert.That(session.PreviousScreenSpoke, Is.False, "el inicio sin frase corta la racha");
            Assert.That(session.PreviousScreenMapIntro, Is.False);

            // La visita siguiente al inicio (un nivel y vuelta, sin frase en medio): ahora sí, con su frase.
            SlothContext next = session.Context.WithHome(session.HomeFacts(Today, 9, 12));
            SlothLine greet = SlothLines.ForHome(in next);
            Assert.That(greet.Deferred, Is.False);
            Assert.That(greet.Greeting, Is.True, "el saludo de la mañana, en la visita siguiente");
            Assert.That(greet.Key, Is.EqualTo("sloth_line_return_04"));
            Assert.That(greet.Gesture, Is.EqualTo(SlothGesture.Wave));
            session.Record(in greet, in next);
            Assert.That(data.LastGreetingDate, Is.EqualTo(Today));
            Assert.That(data.LastSeenDate, Is.EqualTo(Today));
            Assert.That(session.LinesThisSession, Is.EqualTo(1));
        }

        [Test]
        public void HomeAfterTheMapIntro_ReturnGreeting_StaysForTheNextVisit()
        {
            var data = new SlothData { LastSeenDate = WeekAgo };
            var session = new SlothSession(data, true, true, true, 4, 4);
            session.RecordMapIntro();
            SlothContext c = session.Context.WithHome(session.HomeFacts(Today, 20, 11));
            SlothLine home = SlothLines.ForHome(in c);
            Assert.That(home.Deferred && !home.Greeting && !home.Speaks && home.Gesture == SlothGesture.None, Is.True);
            session.Record(in home, in c);
            Assert.That(data.LastSeenDate, Is.EqualTo(WeekAgo), "los días fuera siguen contando");
            Assert.That(data.LastGreetingDate, Is.Null);

            SlothContext next = session.Context.WithHome(session.HomeFacts(Today, 20, 11));
            SlothLine greet = SlothLines.ForHome(in next);
            Assert.That(greet.Greeting && greet.Speaks, Is.True, "el de regreso, en la visita siguiente");
            Assert.That(greet.Key!.StartsWith("sloth_line_return_", System.StringComparison.Ordinal), Is.True);
            Assert.That(greet.Key, Is.Not.EqualTo("sloth_line_return_04"), "el de regreso, no el de la mañana");
        }

        [Test]
        public void HomeAfterTheMapIntro_WithNoGreetingDue_IsAnOrdinaryVisit()
        {
            // Sin saludo pendiente (ya saludó hoy) no hay nada que aplazar: la visita se apunta como siempre.
            var data = new SlothData { LastSeenDate = Today, LastGreetingDate = Today };
            var session = new SlothSession(data, true, true, true, 4, 4);
            session.RecordMapIntro();
            SlothContext c = session.Context.WithHome(session.HomeFacts(Today, 10, 11));
            SlothLine home = SlothLines.ForHome(in c);
            Assert.That(home.Deferred || home.Greeting || home.Speaks || home.Gesture != SlothGesture.None, Is.False);

            data.LastGreetingDate = Yesterday;
            data.LastSeenDate = Today; // ya abrió hoy y son las 20:00: ni regreso ni mañana
            SlothContext evening = session.Context.WithHome(session.HomeFacts(Today, 20, 11));
            SlothLine none = SlothLines.ForHome(in evening);
            Assert.That(none.Deferred || none.Greeting, Is.False);
            var fresh = new SlothData { LastSeenDate = TwoDaysAgo };
            var other = new SlothSession(fresh, true, true, true, 4, 4);
            other.RecordMapIntro();
            SlothContext dusk = other.Context.WithHome(other.HomeFacts(Today, 20, 11));
            SlothLine visit = SlothLines.ForHome(in dusk);
            Assert.That(visit.Deferred, Is.False, "dos días fuera y de noche: no toca saludo");
            other.Record(in visit, in dusk);
            Assert.That(fresh.LastSeenDate, Is.EqualTo(Today), "la apertura se apunta como en cualquier visita");
        }

        [Test]
        public void MapIntro_BlocksTheLineOfTheNextScreen_ButNotItsGesture_AndTheSessionKeepsItsFourLines()
        {
            // Dos pantallas seguidas: tras el bocadillo, la pantalla siguiente (aquí Nivel fallido) solo con el gesto.
            var data = new SlothData();
            var session = new SlothSession(data, true, true, true, 1, 4);
            session.RecordMapIntro();
            SlothContext f1 = session.Context.WithFail(new SlothFailFacts(true, false, 1, true));
            SlothLine blocked = SlothLines.ForFail(45, in f1);
            Assert.That(blocked.Speaks, Is.False, "nunca frase en dos pantallas seguidas");
            Assert.That(blocked.Gesture, Is.EqualTo(SlothGesture.Encourage), "el gesto, sí");
            Assert.That(blocked.Deferred, Is.False, "solo el saludo del inicio se aplaza");
            session.Record(in blocked, in f1);
            Assert.That(session.PreviousScreenMapIntro, Is.False, "el aviso dura una pantalla");

            // La sesión sigue teniendo sus 4 frases: el bocadillo no gastó ninguna.
            for (int i = 0; i < 4; i++)
            {
                SlothContext fc = session.Context.WithFail(new SlothFailFacts(true, false, 2 + i, true));
                SlothLine line = SlothLines.ForFail(46 + i, in fc);
                Assert.That(line.Speaks, Is.True, "frase " + (i + 1) + " de 4");
                session.Record(in line, in fc);
                SlothLine pause = new SlothLine(SlothScreen.Fail, SlothGesture.None, SlothMoment.Fail, SlothData.NoLine, null, 0, false);
                session.Record(in pause, in fc); // una pantalla sin frase en medio
            }

            Assert.That(session.LinesThisSession, Is.EqualTo(4));
            SlothContext fifth = session.Context.WithFail(new SlothFailFacts(true, false, 9, true));
            Assert.That(SlothLines.ForFail(51, in fifth).Speaks, Is.False, "la quinta, no: el tope sigue siendo 4");
        }

        [Test]
        public void WithoutTheMapIntro_HomeRulesAreUnchanged()
        {
            // Ninguna otra frase cambia de regla: tras una pantalla con frase normal, el saludo sale con la mano y se gasta.
            var data = new SlothData { LastSeenDate = Yesterday };
            var session = new SlothSession(data, true, true, true, 1, 4);
            SlothContext f = session.Context.WithFail(new SlothFailFacts(true, false, 1, true));
            SlothLine spoke = SlothLines.ForFail(45, in f);
            Assert.That(spoke.Speaks, Is.True);
            session.Record(in spoke, in f);
            SlothContext c = session.Context.WithHome(session.HomeFacts(Today, 8, 46));
            Assert.That(c.PreviousScreenMapIntro, Is.False);
            SlothLine home = SlothLines.ForHome(in c);
            Assert.That(home.Deferred, Is.False);
            Assert.That(home.Greeting && !home.Speaks && home.Gesture == SlothGesture.Wave, Is.True, "solo la mano, y gastado");
            session.Record(in home, in c);
            Assert.That(data.LastGreetingDate, Is.EqualTo(Today));

            // El constructor sin el aviso deja el comportamiento de siempre.
            var plain = new SlothContext(true, true, true, 4, 4, 0, false, null);
            Assert.That(plain.PreviousScreenMapIntro || plain.PreviousScreenSpoke, Is.False);
            var map = new SlothContext(true, true, true, 4, 4, 0, false, null, true);
            Assert.That(map.PreviousScreenSpoke, Is.True, "el aviso del Mapa implica pantalla con frase");
        }

        [Test]
        public void DeferredGreeting_IsDeterministic_AndAllocatesNothing()
        {
            var data = new SlothData { LastSeenDate = WeekAgo };
            var session = new SlothSession(data, true, true, true, 4, 4);
            session.RecordMapIntro();
            SlothContext c = session.Context.WithHome(session.HomeFacts(Today, 8, 11));
            SlothLine a = SlothLines.ForHome(in c);
            SlothLine b = SlothLines.ForHome(in c);
            Assert.That(a.Deferred == b.Deferred && a.Greeting == b.Greeting && a.Index == b.Index && a.Gesture == b.Gesture, Is.True);

            TestDelegate call = () =>
            {
                int acc = 0;
                for (int i = 0; i < 64; i++)
                {
                    SlothLine line = SlothLines.ForHome(in c);
                    acc += line.Deferred ? 1 : 0;
                }

                s_sink = acc;
            };
            call(); // calentamiento
            Assert.That(call, Is.Not.AllocatingGCMemory());
            Assert.That(s_sink, Is.EqualTo(64));
        }
    }
}
