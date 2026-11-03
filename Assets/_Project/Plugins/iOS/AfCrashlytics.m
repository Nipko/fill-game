// Crashlytics de iOS sin pasar por el Firebase de C# (fase M2 de docs/dev/traspaso/mac-ios.md), como el JNI de
// AF.Analytics.Firebase.FirebaseCrashReporting en Android: la recogida apagada (persiste) y los informes sin enviar, borrados
// (B12, banda < 13; T-L6 de D-057). En iOS Firebase no arranca solo: sin el FirebaseApp que crea el de C#, Crashlytics no corre y
// aquí no se hace nada. En Objective-C (no .mm) para importar los módulos de Firebase que enlaza el proyecto de Xcode.

@import FirebaseCore;
@import FirebaseCrashlytics;

void AfCrashlyticsStop(void)
{
    if ([FIRApp defaultApp] == nil)
    {
        return;
    }

    [[FIRCrashlytics crashlytics] setCrashlyticsCollectionEnabled:NO];
    [[FIRCrashlytics crashlytics] deleteUnsentReports];
}

void AfCrashlyticsDeleteUnsent(void)
{
    if ([FIRApp defaultApp] == nil)
    {
        return;
    }

    [[FIRCrashlytics crashlytics] deleteUnsentReports];
}
