#nullable enable

namespace AF.GameFlow
{
    /// <summary>
    /// Avisos opcionales de <see cref="LevelFlow"/> para la Luciérnaga · Revelar (04 §2.5; 07 §7.6). El flujo los consulta con
    /// <c>listener as IRevealListener</c>, así que <see cref="ILevelFlowListener"/> no cambia. Se llaman de forma síncrona,
    /// con el modelo ya actualizado.
    /// </summary>
    public interface IRevealListener
    {
        /// <summary>
        /// Se regalaron <paramref name="units"/> Luciérnagas al stock: el estreno de Aguas turbias (nivel 100; 04 §2.5 «regala
        /// 1 Luciérnaga al stock con la tarjeta»). Llega en LOADING, tras <see cref="ILevelFlowListener.OnLevelLoaded"/> y antes
        /// de la tarjeta previa o de la intro.
        /// </summary>
        void OnRevealGifted(int units);

        /// <summary>
        /// Entra (<paramref name="selecting"/> = true) o sale del modo de selección (04 §2.5: «las cuias turbias pulsan»; la
        /// única excepción a D-002). Sale al cancelar (tocar fuera, otra cuia o cualquier otra acción; sin coste) y también al
        /// revelar, justo antes de <see cref="OnRevealed"/>.
        /// </summary>
        void OnRevealSelection(bool selecting);

        /// <summary>
        /// La Luciérnaga reveló las capas <paramref name="layerMask"/> (bit k = capa k desde el fondo) de la cuia
        /// <paramref name="cuia"/>. Cuando llega, lo revelado ya está aplicado, guardado y cobrado (04 §2.5 d).
        /// </summary>
        void OnRevealed(int cuia, int layerMask);
    }
}
