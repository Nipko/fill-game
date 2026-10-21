#nullable enable
using System;

namespace AF.LiveOps
{
    /// <summary>
    /// <c>settings.push</c> (08 §4.3: <c>{"pre_prompt_shown": true, "granted": true}</c>) con dos campos nuevos [P] para el segundo y
    /// último pre-prompt de 06 §8.1 («al alcanzar el hito 3 de la Crecida o a los 7 días, lo que ocurra primero»): la hora del
    /// primero (<c>pre_prompt_at</c>) y si ya salió el segundo (<c>second_prompt_shown</c>). Nota
    /// <c>docs/dev/sesiones-notificaciones.md</c> §3 y pregunta B6-Q2. Solo datos: las reglas son de <see cref="PushPrePromptRules"/>
    /// y <see cref="PushPermissionService"/>.
    /// </summary>
    public sealed class PushSettingsRecord
    {
        /// <summary><c>pre_prompt_shown</c>: el primer pre-prompt ya salió (y se contestó).</summary>
        public bool PrePromptShown { get; set; }

        /// <summary>
        /// <c>granted</c>: el jugador quiere avisos («Avísame», o encendido en Ajustes) y el sistema los deja enseñar (último estado
        /// conocido; al abrir se baja a false si el teléfono ya no los permite) [P].
        /// </summary>
        public bool Granted { get; set; }

        /// <summary><c>pre_prompt_at</c> [P]: cuándo salió el primer pre-prompt, UTC (para «a los 7 días»).</summary>
        public DateTime? PrePromptAt { get; set; }

        /// <summary><c>second_prompt_shown</c> [P]: ya salió el segundo y último pre-prompt; después, solo Ajustes.</summary>
        public bool SecondPromptShown { get; set; }

        /// <summary>Copia.</summary>
        public PushSettingsRecord Clone()
        {
            return new PushSettingsRecord
            {
                PrePromptShown = PrePromptShown,
                Granted = Granted,
                PrePromptAt = PrePromptAt,
                SecondPromptShown = SecondPromptShown,
            };
        }
    }

    /// <summary>
    /// Dónde vive <c>settings.push</c>: en el juego, el perfil (parche B6-profile, <c>ProfilePushSettingsStore</c>); en los tests y en
    /// desarrollo sin el parche, <see cref="InMemoryPushSettingsStore"/>. <see cref="Save"/> no lanza por un fallo de escritura.
    /// </summary>
    public interface IPushSettingsStore
    {
        /// <summary>El registro vivo (nunca null).</summary>
        PushSettingsRecord Load();

        /// <summary>Guarda <paramref name="record"/>.</summary>
        void Save(PushSettingsRecord record);
    }

    /// <summary><see cref="IPushSettingsStore"/> en memoria.</summary>
    public sealed class InMemoryPushSettingsStore : IPushSettingsStore
    {
        private PushSettingsRecord _record;

        /// <summary>Crea el almacén con <paramref name="initial"/> (o vacío).</summary>
        public InMemoryPushSettingsStore(PushSettingsRecord? initial = null)
        {
            _record = initial ?? new PushSettingsRecord();
        }

        /// <summary>Guardados pedidos.</summary>
        public int SaveCount { get; private set; }

        /// <inheritdoc />
        public PushSettingsRecord Load()
        {
            return _record;
        }

        /// <inheritdoc />
        public void Save(PushSettingsRecord record)
        {
            _record = record ?? throw new ArgumentNullException(nameof(record));
            SaveCount++;
        }
    }
}
