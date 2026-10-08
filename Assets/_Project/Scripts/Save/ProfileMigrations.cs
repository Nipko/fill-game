#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using AF.Save.Json;

namespace AF.Save
{
    /// <summary>
    /// Migraciones del perfil <c>profile_schema n → n+1</c>, encadenadas al cargar (GDD 08 §4.4): cada paso transforma el
    /// documento JSON del esquema <c>n</c> en el del <c>n+1</c>, y al final el documento queda en
    /// <see cref="TargetSchema"/>. <see cref="LocalProfileStore"/> escribe <c>profile.bak</c> antes de migrar y, si un paso
    /// lanza una excepción, conserva el archivo anterior sin tocarlo.
    /// <para>
    /// Hoy el esquema es 1 y no hay pasos. Para publicar el esquema 2: subir <see cref="Profile.CurrentSchema"/>, adaptar
    /// <see cref="Profile"/> y <see cref="ProfileJson"/>, y registrar en <see cref="Current"/> el paso
    /// <c>Step(1, MigrateV1ToV2)</c>, con su test de ida (documento v1 → v2).
    /// </para>
    /// </summary>
    public sealed class ProfileMigrations
    {
        private readonly Dictionary<int, Action<JsonNode>> _steps = new Dictionary<int, Action<JsonNode>>();

        /// <summary>Las migraciones de esta build: hasta <see cref="Profile.CurrentSchema"/>.</summary>
        public static ProfileMigrations Current { get; } = new ProfileMigrations(Profile.CurrentSchema);

        /// <summary>Crea un juego de migraciones hasta <paramref name="targetSchema"/>.</summary>
        /// <param name="targetSchema">Esquema final.</param>
        /// <param name="steps">Pasos <c>(n, paso)</c>: cada paso convierte, en el sitio, un documento del esquema <c>n</c> al <c>n+1</c>.</param>
        internal ProfileMigrations(int targetSchema, params KeyValuePair<int, Action<JsonNode>>[] steps)
        {
            if (targetSchema < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(targetSchema));
            }

            TargetSchema = targetSchema;
            for (int i = 0; i < steps.Length; i++)
            {
                int from = steps[i].Key;
                if (from < 0 || from >= targetSchema || _steps.ContainsKey(from))
                {
                    throw new ArgumentException("Paso de migración inválido o repetido desde el esquema " + from.ToString(CultureInfo.InvariantCulture) + ".", nameof(steps));
                }

                _steps.Add(from, steps[i].Value ?? throw new ArgumentNullException(nameof(steps)));
            }
        }

        /// <summary>Esquema al que llegan las migraciones.</summary>
        public int TargetSchema { get; }

        /// <summary>Un paso <c>n → n+1</c> para el constructor.</summary>
        internal static KeyValuePair<int, Action<JsonNode>> Step(int fromSchema, Action<JsonNode> migrate)
        {
            return new KeyValuePair<int, Action<JsonNode>>(fromSchema, migrate);
        }

        /// <summary>
        /// Lleva <paramref name="root"/> de <paramref name="fromSchema"/> a <see cref="TargetSchema"/>, paso a paso y en
        /// orden, y actualiza <c>profile_schema</c> tras cada paso.
        /// </summary>
        /// <exception cref="InvalidOperationException">El esquema es mayor que el destino o falta un paso.</exception>
        /// <exception cref="Exception">Cualquier excepción de un paso se propaga tal cual.</exception>
        internal void Migrate(JsonNode root, int fromSchema)
        {
            if (fromSchema > TargetSchema)
            {
                throw new InvalidOperationException("No se migra hacia atrás: esquema " + fromSchema.ToString(CultureInfo.InvariantCulture)
                    + " > " + TargetSchema.ToString(CultureInfo.InvariantCulture) + ".");
            }

            for (int schema = fromSchema; schema < TargetSchema; schema++)
            {
                if (!_steps.TryGetValue(schema, out var step))
                {
                    throw new InvalidOperationException("No hay migración desde profile_schema " + schema.ToString(CultureInfo.InvariantCulture) + ".");
                }

                step(root);
                root.Set(ProfileJson.SchemaKey, JsonNode.FromInt(schema + 1));
            }
        }
    }
}
