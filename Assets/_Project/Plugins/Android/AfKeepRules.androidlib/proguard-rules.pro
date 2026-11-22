# Reglas keep de R8 que no trae ningun AAR (D-059, anadido del 2026-11-06). Cada bloque dice por que hace falta.

# --- Play Games 2.3.0 ---
# El plugin de Unity llama por JNI (AndroidJavaClass/AndroidJavaObject y AndroidJavaProxy) a play-services-games-v2 y a su
# puente com.google.games.bridge (gpgs-plugin-support.aar). Ninguno de los dos AAR trae reglas de consumidor: el plugin las
# publica en Packages/com.google.play.games/Proguard/games.txt y pide copiarlas a mano. Estas son las suyas, sin
# com.google.android.gms.nearby.** (Nearby no entra en el build: anadido de D-042, mainTemplate.gradle).
-keep class com.google.android.gms.games.leaderboard.** { *; }
-keep class com.google.android.gms.games.snapshot.** { *; }
-keep class com.google.android.gms.games.achievement.** { *; }
-keep class com.google.android.gms.games.event.** { *; }
-keep class com.google.android.gms.games.stats.** { *; }
-keep class com.google.android.gms.games.* { *; }
-keep class com.google.android.gms.common.api.ResultCallback { *; }
-keep class com.google.android.gms.signin.** { *; }
-keep class com.google.android.gms.dynamic.** { *; }
-keep class com.google.android.gms.dynamite.** { *; }
-keep class com.google.android.gms.tasks.** { *; }
-keep class com.google.android.gms.security.** { *; }
-keep class com.google.android.gms.base.** { *; }
-keep class com.google.android.gms.actions.** { *; }
-keep class com.google.games.bridge.** { *; }
-keep class com.google.android.gms.common.ConnectionResult { *; }
-keep class com.google.android.gms.common.GooglePlayServicesUtil { *; }
-keep class com.google.android.gms.common.api.** { *; }
-keep class com.google.android.gms.common.data.DataBufferUtils { *; }
# El C# del plugin 2.3.0 tambien nombra AuthScope (RequestServerSideAccess) y PlayerGameEvent/PlayDuration, que viven en
# subpaquetes que games.txt no cubre (games.* es un solo nivel).
-keep class com.google.android.gms.games.gamessignin.** { *; }
-keep class com.google.android.gms.games.playergameevent.** { *; }
# Las tres clases proxy de Nearby del puente (ConnectionLifecycleCallbackProxy, EndpointDiscoveryCallbackProxy,
# PayloadCallbackProxy) nombran clases de play-services-nearby, que no entra en el build (anadido de D-042): sin esto R8
# las daria por clases que faltan. Solo se cargan si se usa Nearby, y el juego no lo usa.
-dontwarn com.google.android.gms.nearby.**
