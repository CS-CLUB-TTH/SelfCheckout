# Add project specific ProGuard rules here.
# By default, the flags in this file are appended to flags specified
# in /Users/user/Library/Android/sdk/tools/proguard/proguard-android.txt
# You can edit the include path and order by changing the proguardFiles
# directive in build.gradle.

# Keep Zebra SDK classes
-keep class com.zebra.** { *; }
-dontwarn com.zebra.**

# Keep JavaScript interface
-keepclassmembers class com.selfcheckout.kiosk.MainActivity$PrintInterface {
    public *;
}

# Keep WebView JavaScript interface annotation
-keepattributes JavascriptInterface
