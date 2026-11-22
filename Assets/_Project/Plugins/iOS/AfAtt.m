// El aviso de rastreo de Apple (App Tracking Transparency) con un plugin propio (fase M3 de docs/dev/traspaso/mac-ios.md; I-5 de
// D-067: sin com.unity.ads.ios-support). Solo AppTrackingTransparency, de la plataforma: sin SDK. En Objective-C (no .mm) para que
// los módulos de Clang enlacen el framework solos. El texto del aviso es NSUserTrackingUsageDescription (Info.plist e
// InfoPlist.strings de cada idioma, que escribe AF.EditorTools.BuildIos). Lo llama AF.UI.IosTracking por [DllImport("__Internal")].

@import AppTrackingTransparency;

/// Estado actual: 0 sin decidir, 1 restringido, 2 denegado, 3 autorizado (ATTrackingManagerAuthorizationStatus).
int AfAttStatus(void)
{
    return (int)[ATTrackingManager trackingAuthorizationStatus];
}

typedef void (*AfAttCallback)(int status);

/// Enseña el aviso del sistema (solo la primera vez: después iOS responde el estado guardado) y llama a `done` en el hilo principal.
/// Solo con la app activa: iOS 17.4+ responde «sin decidir» sin enseñar nada si no lo está. Aquí siempre llega tras un toque
/// («Continuar» de AF.UI.AttPrePromptCard), con la app delante.
void AfAttRequest(AfAttCallback done)
{
    [ATTrackingManager requestTrackingAuthorizationWithCompletionHandler:^(ATTrackingManagerAuthorizationStatus status) {
        dispatch_async(dispatch_get_main_queue(), ^{
            if (done != NULL)
            {
                done((int)status);
            }
        });
    }];
}
