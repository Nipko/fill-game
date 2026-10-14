#nullable enable
using System;

namespace AF.Core.Mechanics
{
    /// <summary>
    /// Mecánica de una cuia (GDD 02 §9.1; D-007: catálogo de 1-200 con el Matapi nuevo). Es el campo <c>type</c> de
    /// <c>attrs[i]</c> en <c>af-level/1</c> (08 §4.1) y el código de 4 bits de <see cref="Board.Cuia.Packed"/> (bits 39-42).
    /// </summary>
    /// <remarks>
    /// <para>El Barro no es un tipo: es el color 15 dentro del contenido (02 §9.1 M5). Aguas turbias tampoco: es la máscara de
    /// ocultos (02 §11).</para>
    /// <para>Los códigos 8 y 9 son la Tinaja (M13) y su cuia chica (D-019; propuesta
    /// <c>docs/research/cultura-ribera/propuesta-tinaja.md</c>, c.1). El 10 [P] es la Hoja atada, variante de la 1.0 (D-007,
    /// propuesta de D-007 b.3; firma de <c>pack_schema</c> 3, pregunta 8). El Brote de dos hojas no es un tipo: es el Brote con
    /// <c>n</c> = 2 (<see cref="Board.Cuia.MechanicN"/>). Los códigos 11-15 quedan libres para la 1.1 (Aguapé, Palizada, Lancha
    /// y Cocha; Q7 y Q8 b′ de D-007) y las demás variantes. Nadie persiste el valor numérico fuera de la cuia: el JSON usa las
    /// claves de <see cref="MechanicTypes.ToKey"/>.</para>
    /// </remarks>
    public enum MechanicType
    {
        /// <summary>Cuia normal.</summary>
        None = 0,

        /// <summary>M1 Hoja: vacío reservado a la fruta <c>key</c>; cae con su primer vertido (debut 43).</summary>
        Hoja = 1,

        /// <summary>
        /// M2 Brote: cerrado no es origen; germina al recibir su color (debut 163 por D-007). Con <c>n</c> = 2 es el Brote de
        /// dos hojas (variante de la 1.0, debut 343; propuesta de D-007 b.3): germina al recibir 2 dosis de su color.
        /// </summary>
        Brote = 2,

        /// <summary>M3 Liana: atada no es origen ni destino; con fruta (<c>key</c>) o de cuenta (<c>n</c>) (debut 83).</summary>
        Liana = 3,

        /// <summary>M10 Matapi: nunca es origen; lo que entra no sale (debut 63 por D-007).</summary>
        Matapi = 4,

        /// <summary>M4 Remolino: par unido por debajo (debut 103).</summary>
        Remolino = 5,

        /// <summary>M6 Lluvia: cada <c>k</c> vertidos cae una gota del tinte agua <c>key</c> (debut 143).</summary>
        Lluvia = 6,

        /// <summary>M7 Piraña: cada <c>k</c> vertidos muerde la última cuia destino; esta cuia es su cueva (debut 183).</summary>
        Pirana = 7,

        /// <summary>
        /// M13 Tinaja (D-019; propuesta de la tinaja, b.2): vasija de capacidad 5 con 3-5 dosis, todas ocultas salvo la boca;
        /// nunca recibe y solo vierte en su cuia chica (<see cref="Cucharon"/>), una dosis cada vez. Vacía queda inerte. Sin
        /// campos en el JSON: una sola tinaja por nivel (debut 323 [P]).
        /// </summary>
        Tinaja = 8,

        /// <summary>
        /// Su cuia chica (M13; propuesta de la tinaja, b.2 reglas 5-8): Cuia chica de 2, vacía al empezar y contigua a la
        /// tinaja; mientras la tinaja tiene dosis solo recibe de ella (cualquier fruta, dos dosis en capas). Cuando la tinaja
        /// queda vacía se suelta (R10 f) y pasa a ser una Cuia chica normal. Sin campos en el JSON.
        /// </summary>
        Cucharon = 9,

        /// <summary>
        /// Hoja atada (variante de M1 de la 1.0, debut 283; D-007, propuesta de D-007 b.3; 02 §1.2 y §1.4): filtro persistente
        /// de la fruta <c>key</c>. Solo recibe su fruta (vacía o con ella en la cima), sí es origen y nunca se suelta; puede
        /// empezar con dosis cuyo fondo es su fruta. Como destino nunca es fútil (anexo B). Código 10 [P]; clave
        /// <c>"hoja_atada"</c>.
        /// </summary>
        HojaAtada = 10,
    }

    /// <summary>Claves de <c>attrs[i].type</c> en <c>af-level/1</c> (08 §4.1) y su conversión con <see cref="MechanicType"/>.</summary>
    public static class MechanicTypes
    {
        /// <summary>Último código de mecánica definido.</summary>
        public const MechanicType Last = MechanicType.HojaAtada;

        private static readonly string[] Keys = { "none", "hoja", "brote", "liana", "matapi", "remolino", "lluvia", "pirana", "tinaja", "cucharon", "hoja_atada" };

        /// <summary><paramref name="type"/> es un valor definido.</summary>
        public static bool IsDefined(MechanicType type)
        {
            return type >= MechanicType.None && type <= Last;
        }

        /// <summary>
        /// Clave del JSON: <c>"hoja"</c>, <c>"brote"</c>, <c>"liana"</c>, <c>"matapi"</c>, <c>"remolino"</c>, <c>"lluvia"</c>,
        /// <c>"pirana"</c>, <c>"tinaja"</c>, <c>"cucharon"</c> o <c>"hoja_atada"</c>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="type"/> no es una mecánica definida (ni <see cref="MechanicType.None"/>).</exception>
        public static string ToKey(MechanicType type)
        {
            if (type == MechanicType.None || !IsDefined(type))
            {
                throw new ArgumentOutOfRangeException(nameof(type), type, "Mecánica desconocida.");
            }

            return Keys[(int)type];
        }

        /// <summary>
        /// Convierte una clave del JSON (comparación ordinal, sin cultura). Devuelve false con cualquier otro valor
        /// (también <c>"barro"</c>, que no es un <c>type</c>) y deja <paramref name="type"/> = <see cref="MechanicType.None"/>.
        /// </summary>
        public static bool TryParse(string? key, out MechanicType type)
        {
            for (int i = 1; i < Keys.Length; i++)
            {
                if (string.Equals(key, Keys[i], StringComparison.Ordinal))
                {
                    type = (MechanicType)i;
                    return true;
                }
            }

            type = MechanicType.None;
            return false;
        }
    }
}
