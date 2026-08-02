# RaylibHello.Android

Minimal `net10.0-android` NativeActivity that loads **`libnovolis_raylib_android.so`** from `Novolis.Raylib.Native` (`android-arm64`).

Desktop Hello remains at [`../RaylibHello`](../RaylibHello) (`RayGame.Run`).

## Build (ProjectRef)

```powershell
dotnet build d:\novolis\novolis-dogfooding\apps\RaylibHello.Android\RaylibHello.Android.csproj -p:NovolisUseProjectReferences=true -r android-arm64
```

Signed APK (debug):

`d:\novolis\novolis-dogfooding\artifacts\bin\RaylibHello.Android\debug_android-arm64\com.novolis.raylibhello-Signed.apk`

## Run

```powershell
adb install -r d:\novolis\novolis-dogfooding\artifacts\bin\RaylibHello.Android\debug_android-arm64\com.novolis.raylibhello-Signed.apk
```

Expect a navy clear + “Novolis Raylib Android” text.

Requires GPR `Novolis.Raylib.Native` that includes `android-arm64` natives after merge, or sibling ProjectRef mode as above.
