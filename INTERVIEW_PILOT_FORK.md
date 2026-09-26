# Interview Pilot RaTeX Fork

This fork's Android artifact is based on upstream RaTeX `v0.1.14` and includes
the upstream implicit-geometry color fix plus a hardened mobile native boundary.

The Android artifact must be built with:

```sh
PATH="$HOME/.cargo/bin:$PATH" \
ANDROID_NDK_HOME=/path/to/android-ndk \
bash platforms/android/build-android.sh

cd demo/android
ANDROID_HOME=/path/to/android-sdk \
./gradlew :ratex-android:assembleRelease --no-daemon
```

`platforms/android/build-android.sh` uses the
`interview-pilot-android` Cargo profile. That profile retains release
optimizations while enabling panic unwinding so the C and JNI guards can turn
internal renderer panics into normal errors instead of terminating the host
application. Do not build the Interview Pilot AAR with the upstream `release`
profile, which intentionally uses `panic = "abort"`.

The script builds `arm64-v8a`, `armeabi-v7a`, `x86`, and `x86_64` so the AAR
matches the Android application's complete ABI set.

## Windows

The Windows binding lives under `platforms/windows` in this same fork. It uses
the shared `ratex-ffi` C ABI and DisplayList protocol, with native Win2D and
GDI+ renderers for the two Windows application surfaces.

Build both supported Windows architectures with:

```powershell
.\platforms\windows\build-windows.ps1
```

The script uses the `interview-pilot-windows` Cargo profile for release builds.
That profile preserves release optimization while allowing the C ABI panic
guard to convert an internal Rust panic into a recoverable render error. It
produces native runtime assets for `win-x64` and `win-arm64`.
