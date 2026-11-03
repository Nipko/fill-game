// Lo poco del sistema de iOS que el juego necesita y que Unity no da (fase M2 de docs/dev/traspaso/mac-ios.md, D-067).
// Solo API de la plataforma (UIKit), sin permisos ni SDK. Lo llaman, por [DllImport("__Internal")]:
//   - AF.Audio.Unity.IosHaptics: los hápticos (07 §9.2), como AndroidHaptics en Android;
//   - AF.UI.IosSystemScale: «Reducir movimiento» del sistema (Q-B9-3), como «Quitar animaciones» en Android.
// Las llamadas llegan desde el hilo principal de Unity, que en iOS es el de UIKit.

#import <UIKit/UIKit.h>

extern "C" {

// strength: 0 ligero (sellado, Atrás bloqueado), 1 medio (victoria, Capi, el Boto), 2 corto. Como HapticStrength.
void AfHapticImpact(int strength)
{
    UIImpactFeedbackStyle style = strength == 1 ? UIImpactFeedbackStyleMedium : UIImpactFeedbackStyleLight;
    UIImpactFeedbackGenerator* generator = [[UIImpactFeedbackGenerator alloc] initWithStyle:style];
    [generator prepare];
    if (strength == 2)
    {
        [generator impactOccurredWithIntensity:0.6];
    }
    else
    {
        [generator impactOccurred];
    }
}

// «Reducir movimiento» de Ajustes › Accesibilidad › Movimiento.
bool AfReduceMotionEnabled()
{
    return UIAccessibilityIsReduceMotionEnabled();
}

}
