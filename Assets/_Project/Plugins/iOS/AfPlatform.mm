// Lo poco del sistema de iOS que el juego necesita y que Unity no da (fase M2 de docs/dev/traspaso/mac-ios.md, D-067).
// Solo API de la plataforma (UIKit y Foundation), sin permisos ni SDK. Lo llaman, por [DllImport("__Internal")]:
//   - AF.Audio.Unity.IosHaptics: los hápticos (07 §9.2), como AndroidHaptics en Android;
//   - AF.UI.IosSystemScale: «Reducir movimiento» del sistema (Q-B9-3), como «Quitar animaciones» en Android;
//   - AF.Backend.Firebase.FirebaseBoot: los identificadores públicos de GoogleService-Info.plist (AfBundlePlistString).
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

// Un texto de un plist del paquete de la app (Xcode los copia en binario: así se lee en los dos formatos), o NULL. Lo usa
// AF.Backend.Firebase.FirebaseBoot para los identificadores públicos de GoogleService-Info.plist. La cadena es nueva (malloc): la
// libera el marshaling de IL2CPP.
const char* AfBundlePlistString(const char* resource, const char* key)
{
    NSString* path = [[NSBundle mainBundle] pathForResource:[NSString stringWithUTF8String:resource] ofType:@"plist"];
    if (path == nil)
    {
        return NULL;
    }

    NSDictionary* plist = [NSDictionary dictionaryWithContentsOfFile:path];
    id value = [plist objectForKey:[NSString stringWithUTF8String:key]];
    if (![value isKindOfClass:[NSString class]])
    {
        return NULL;
    }

    const char* utf8 = [(NSString*)value UTF8String];
    char* copy = (char*)malloc(strlen(utf8) + 1);
    strcpy(copy, utf8);
    return copy;
}

}
