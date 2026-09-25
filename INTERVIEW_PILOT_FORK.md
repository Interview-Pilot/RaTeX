# Interview Pilot Android Fork

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
