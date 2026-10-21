#nullable enable

namespace AF.GameFlow
{
    /// <summary>
    /// Las ayudas del tutorial que faltaban (tarea B11; 02 §13 y 06 §2.2), como reglas puras y deterministas, sin motor y sin
    /// asignaciones; devuelven claves de <c>strings.csv</c>, nunca texto (regla de localización). La vista las enseña
    /// (<c>GameScreen.TutorialTips.cs</c>):
    /// <list type="bullet">
    /// <item><description><b>Tooltip «Reiniciar es gratis…»</b> en los niveles 6 y 8 (<see cref="RestartTipLevels"/>), sobre
    /// «Volver a la orilla (gratis)» del modal Aguas estancadas: 02 §13, fila 6 («Si entra en Aguas estancadas, primer modal con
    /// Marea (ilimitada) y Volver a la orilla; tooltip "Reiniciar es gratis, siempre, mismo tablero. Desde el nivel 31, quedarte
    /// sin vertidos cuesta una canoa" (texto canon del cap. 04 §2.7)») y fila 8 («enseñanza de Volver a la orilla con el mismo
    /// tooltip del nivel 6»). Sale una vez por nivel mientras la app siga abierta: la primera vez que el modal se ve quieto en ese
    /// nivel, y no en los reintentos ni al volver a entrar desde el inicio [P]. Con
    /// <c>biz_feature_lives</c> apagado (D-038: «sin canoas … ni en los avisos») la segunda frase sería falsa, así que sale solo la
    /// primera (<see cref="RestartTipKey"/>; pregunta Q-B11-2).</description></item>
    /// <item><description><b>Tooltip de la Cosecha doble</b> en la victoria del nivel de <c>ads_rv_double_harvest_from_level</c>
    /// (11): 06 §2.2, fila 11 («botón Cosecha doble ("Ver video · x2") presentado una sola vez con un tooltip sin personaje»;
    /// D-013, T1-A). Sale si el botón sale (con anuncio disponible), sobre él, hasta que se pulsa o se deja la victoria. El texto es
    /// [P] (pregunta Q-B11-4).</description></item>
    /// <item><description><b>Dedo guía de la Cuia extra</b> en el nivel de <c>extra_cuia_unlock_level</c> (8): 02 §13, fila 8, y
    /// 06 §2.2 («dedo guía en su primer uso»); 04 §2.2 (Capi regala 1 unidad). Una mano sobre el botón «+1 cuia» de la bandeja,
    /// cuando ya se fue el aviso de Capi, hasta que se usa (<see cref="ShowsExtraCuiaFinger"/>). Es una ayuda explícita y puntual,
    /// como el dedo de 1-2 (D-028 con P5; compatible con D-002): no señala ninguna cuia.</description></item>
    /// </list>
    /// </summary>
    public static class TutorialTips
    {
        /// <summary>Clave del tooltip con las dos frases del canon de 04 §2.7 (con canoas).</summary>
        public const string RestartFreeCanoeKey = "tutorial_restart_free_canoe";

        /// <summary>Clave del tooltip con solo la primera frase (con <c>biz_feature_lives</c> apagado, D-038).</summary>
        public const string RestartFreeKey = "tutorial_restart_free";

        /// <summary>Clave del tooltip de la Cosecha doble del 11 [P].</summary>
        public const string DoubleHarvestKey = "tutorial_double_harvest";

        /// <summary>Niveles con el tooltip «Reiniciar es gratis…» (02 §13, filas 6 y 8).</summary>
        public static readonly int[] RestartTipLevels = { 6, 8 };

        /// <summary>Las claves de los tooltips (tests: todas en <c>strings.csv</c>). Una copia nueva en cada llamada.</summary>
        public static string[] Keys => new[] { RestartFreeCanoeKey, RestartFreeKey, DoubleHarvestKey };

        /// <summary>
        /// <paramref name="level"/> lleva el tooltip «Reiniciar es gratis…» sobre el modal Aguas estancadas (6 y 8). Sin
        /// asignaciones.
        /// </summary>
        public static bool ShowsRestartTip(int level)
        {
            for (int i = 0; i < RestartTipLevels.Length; i++)
            {
                if (RestartTipLevels[i] == level)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Clave del tooltip «Reiniciar es gratis…»: con <paramref name="livesEnabled"/> (<c>biz_feature_lives</c>), las dos frases
        /// del canon (04 §2.7); apagado, solo la primera, porque desde el 31 quedarse sin vertidos ya no cuesta canoa (D-038).
        /// </summary>
        public static string RestartTipKey(bool livesEnabled)
        {
            return livesEnabled ? RestartFreeCanoeKey : RestartFreeKey;
        }

        /// <summary>
        /// La victoria de <paramref name="level"/> presenta la Cosecha doble con su tooltip: es el nivel de
        /// <paramref name="doubleHarvestFromLevel"/> (<c>ads_rv_double_harvest_from_level</c>, 11) y es de campaña. Si el botón no
        /// sale (sin anuncio o sin tope), la vista no lo enseña.
        /// </summary>
        public static bool ShowsDoubleHarvestTip(int level, int doubleHarvestFromLevel, bool isDaily)
        {
            return !isDaily && doubleHarvestFromLevel > 0 && level == doubleHarvestFromLevel;
        }

        /// <summary>
        /// El dedo guía señala «+1 cuia» en <paramref name="level"/>: es el nivel de <paramref name="extraCuiaUnlockLevel"/>
        /// (<c>extra_cuia_unlock_level</c>, 8), de campaña, sin la Cuia extra aplicada aún en el nivel
        /// (<paramref name="extraCuiasApplied"/> = 0: «en su primer uso») y con al menos una en el stock
        /// (<paramref name="stock"/>, la que regala Capi). Sin asignaciones.
        /// </summary>
        public static bool ShowsExtraCuiaFinger(int level, int extraCuiaUnlockLevel, bool isDaily, int extraCuiasApplied, int stock)
        {
            return !isDaily && extraCuiaUnlockLevel > 0 && level == extraCuiaUnlockLevel && extraCuiasApplied == 0 && stock > 0;
        }
    }
}
