#nullable enable
using System;
using System.Globalization;
using System.Text;
using System.Threading;

namespace AF.Core.Tests.DotnetOnly
{
    /// <summary>
    /// Medida de las asignaciones de un bloque de código en el hilo actual, para las pruebas del bucle caliente sin
    /// asignaciones (CLAUDE.md, regla dura 2; .claude/rules/core.md). Solo dotnet: usa
    /// <see cref="GC.GetAllocatedBytesForCurrentThread"/>, que no existe en el perfil de Unity.
    /// </summary>
    /// <remarks>
    /// <para>FX9. Una sola medida tras una sola llamada de calentamiento fallaba de vez en cuando con la suite completa
    /// (3 de 110 ejecuciones de <c>dotnet test</c>): 4.896 bytes en <c>UsefulMovesInSingle</c> (y en
    /// <c>UsefulMovesInRun</c>, en la ola 6), y unos 7.700 bytes de más en <c>WarmSolverInRun</c>. No era el código:</para>
    /// <list type="bullet">
    /// <item>el exceso no se repite al medir otra vez, y cambia de tamaño de una vez a otra, mientras que una asignación
    /// real del código asigna lo mismo en cada pasada;</item>
    /// <item>no aparece con <see cref="HotLoopAllocationTests"/> sola (0 de 40 ejecuciones, también con la CPU cargada), ni en
    /// un bucle sin llamadas al núcleo medido dentro de la suite completa o en un testhost sin ella (0 saltos en 2,3 millones de
    /// ventanas);</item>
    /// <item>no coincide con recolecciones ni con el JIT en el hilo (las ventanas fallidas tienen 0 recolecciones y casi
    /// siempre 0 métodos compilados en el hilo), y las recolecciones que lanzan otros hilos no mueven el contador por hilo
    /// con la GC de estación de trabajo, la del testhost (0 saltos en unas 20.000 ventanas con recolección);</item>
    /// <item>en la ventana fallida del solver, el recorrido del montón entre dos marcadores solo encontró la solución
    /// (<c>Pour[37]</c>, 472 bytes), puesta al principio de una región de gen0 por debajo del marcador anterior (el hilo
    /// cambió de contexto de asignación dentro de la ventana): lo más probable es que el resto sea la contabilidad de ese
    /// contexto en un montón grande y fragmentado tras el resto de la suite, y no del solver.</item>
    /// </list>
    /// <para>Por eso se mide así, sin subir ninguna tolerancia: <see cref="WarmupPasses"/> pasadas completas del mismo
    /// bloque antes de medir (los caminos que solo se recorren dentro de la ventana, como el modo contrario en
    /// <c>UsefulMovesInRun</c>, también se calientan, y el JIT tiene tiempo de subir de nivel), y después hasta
    /// <see cref="MaxAttempts"/> medidas, quedándose con el mínimo. Una asignación real del código aparece en todas las
    /// medidas, así que el mínimo la sigue detectando; la del entorno no se repite. Entre una medida que no baja del
    /// umbral y la siguiente se esperan <see cref="SettleMilliseconds"/> ms para que el compilador de fondo termine.</para>
    /// </remarks>
    internal static class AllocationMeter
    {
        /// <summary>Pasadas completas del bloque antes de la primera medida.</summary>
        public const int WarmupPasses = 3;

        /// <summary>Medidas como mucho; se para en cuanto una baja del umbral.</summary>
        public const int MaxAttempts = 5;

        /// <summary>Espera entre una medida por encima del umbral y la siguiente (el JIT de fondo termina sus niveles).</summary>
        public const int SettleMilliseconds = 50;

        /// <summary>
        /// Calienta <paramref name="body"/> <see cref="WarmupPasses"/> veces y lo mide hasta <see cref="MaxAttempts"/> veces;
        /// devuelve el mínimo. Para en cuanto una medida es menor que <paramref name="below"/> (1 para «0 bytes»; el umbral
        /// del test para «menos de N bytes»).
        /// </summary>
        public static AllocationReading Measure(Action body, long below)
        {
            return Measure(null, body, below);
        }

        /// <summary>
        /// Como <see cref="Measure(Action, long)"/>, con <paramref name="prepare"/> antes de cada pasada (también las de
        /// calentamiento) y fuera de la ventana medida: vaciar un memo, rearmar una bandera de corte.
        /// </summary>
        public static AllocationReading Measure(Action? prepare, Action body, long below)
        {
            if (body == null)
            {
                throw new ArgumentNullException(nameof(body));
            }

            for (int pass = 0; pass < WarmupPasses; pass++)
            {
                prepare?.Invoke();
                body();
            }

            var attempts = new long[MaxAttempts];
            int count = 0;
            long min = long.MaxValue;
            while (count < MaxAttempts && min >= below)
            {
                if (count > 0)
                {
                    Thread.Sleep(SettleMilliseconds);
                }

                prepare?.Invoke();
                long before = GC.GetAllocatedBytesForCurrentThread();
                body();
                long bytes = GC.GetAllocatedBytesForCurrentThread() - before;
                attempts[count++] = bytes;
                min = Math.Min(min, bytes);
            }

            return new AllocationReading(min, attempts, count);
        }
    }

    /// <summary>Resultado de <see cref="AllocationMeter"/>: el mínimo y todas las medidas (para el mensaje del fallo).</summary>
    internal readonly struct AllocationReading
    {
        private readonly long[] _attempts;
        private readonly int _count;

        public AllocationReading(long bytes, long[] attempts, int count)
        {
            Bytes = bytes;
            _attempts = attempts;
            _count = count;
        }

        /// <summary>Bytes asignados: el mínimo de las medidas.</summary>
        public long Bytes { get; }

        /// <summary>Número de medidas hechas.</summary>
        public int Attempts => _count;

        /// <summary>Texto para el mensaje del assert, con el nombre de la ventana y las medidas.</summary>
        public string Describe(string what)
        {
            var text = new StringBuilder(what);
            text.Append(": mínimo de ").Append(_count.ToString(CultureInfo.InvariantCulture)).Append(" medidas (");
            for (int i = 0; i < _count; i++)
            {
                if (i > 0)
                {
                    text.Append(", ");
                }

                text.Append(_attempts[i].ToString(CultureInfo.InvariantCulture));
            }

            return text.Append(" bytes)").ToString();
        }
    }
}
