#nullable enable
#if UNITY_IOS && !UNITY_EDITOR
using System;
using System.Runtime.InteropServices;
using UnityEngine;
#endif

namespace AF.Audio.Unity
{
    /// <summary>
    /// Hápticos de iOS (fase M2 de <c>docs/dev/traspaso/mac-ios.md</c>; 07 §9.2), con <c>UIImpactFeedbackGenerator</c> por el
    /// plugin propio <c>Plugins/iOS/AfPlatform.mm</c>, sin permiso: ligero para el sellado y Atrás bloqueado, medio para la
    /// victoria, Capi y el Boto, y uno ligero al 60 % para el corto (los mismos tres de <see cref="HapticStrength"/> que
    /// <see cref="AndroidHaptics"/>). Si el teléfono no tiene motor háptico, iOS no hace nada. Fuera del iPhone, no hace nada.
    /// La crea <see cref="AndroidHaptics.ForPlatform"/>.
    /// </summary>
    public sealed class IosHaptics : IHaptics
    {
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void AfHapticImpact(int strength);

        private bool _failed;
#endif

        /// <inheritdoc />
        public void Vibrate(HapticKind kind)
        {
#if UNITY_IOS && !UNITY_EDITOR
            if (_failed)
            {
                return;
            }

            try
            {
                AfHapticImpact((int)Haptics.StrengthOf(kind));
            }
            catch (Exception e)
            {
                _failed = true; // un solo aviso: sin el símbolo nativo, no hay hápticos
                Debug.LogWarning("AF.Audio: sin hápticos de iOS (" + e.Message + ").");
            }
#else
            _ = kind;
#endif
        }
    }
}
