#nullable enable
using System;

namespace AF.Levels
{
    /// <summary>El paquete de niveles no cumple el formato que admite el cliente (GDD 08 §4.1).</summary>
    public sealed class LevelFormatException : Exception
    {
        /// <summary>Crea la excepción.</summary>
        public LevelFormatException(string message)
            : base(message)
        {
        }

        /// <summary>Crea la excepción con la causa.</summary>
        public LevelFormatException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
