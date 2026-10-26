# Alternate App Icons

A Unity package that changes the **app icon from the game**, on iOS and Android, among alternate icons built into the app: a reward, an event, an icon chosen by the player. The game decides which icon to show; the package changes it.

The package comes with:

- a static API, `AppIcon`, that shows an icon by its name and tells which icon is shown;
- a Project Settings page that declares the icons with their images, and checks them while you work on them, then again at build time;
- build steps that add the icons to the Xcode and Gradle projects;
- a simulation of the change in the editor.

## Requirements

- Unity 6000.0 or later. The package is verified on Unity 6000.3.
- iOS: Xcode, which builds the icons from the asset catalog. Verified with Xcode 27.1, on an iPad with iPadOS 27.
- Android: Android 8.0 (API level 26) or later on the device, the first version with adaptive icons. The app can still support older versions: they keep the default icon. Verified on a Pixel 6 with Android 17.
- The built-in Android JNI module, `com.unity.modules.androidjni`, which the package adds.

## Installation

In the Package Manager, choose **+ › Install package from git URL…** and enter:

```
https://github.com/Omadel/alternate-app-icons.git#v1.0.0
```

Or add this line to the `dependencies` of `Packages/manifest.json`:

```json
"com.javienne.alternate-app-icons": "https://github.com/Omadel/alternate-app-icons.git#v1.0.0"
```

To update, change the tag at the end of the URL. The Package Manager does not list the versions of a git package.

## Quick start

1. Make the images of each icon:
   - iOS: a PNG of 1024 × 1024 px, without transparency;
   - Android: the foreground layer of the adaptive icon, a PNG of 432 × 432 px, over a background colour or a PNG of 432 × 432 px; optionally, a monochrome layer of 432 × 432 px for themed icons.
2. Open **Edit › Project Settings › Alternate App Icons**, click **Add Icon**, name the icon and set its images.
3. Change the icon from the game:

   ```csharp
   using Javienne.AlternateAppIcons;
   using UnityEngine;

   public class GoldenIconReward : MonoBehaviour
   {
       public async void Unlock()
       {
           var result = await AppIcon.SetIcon("Golden");
           if (!result.Succeeded) Debug.Log($"The icon did not change: {result}");
       }
   }
   ```

4. Build for iOS or Android. In the editor, the change is simulated and logged.

## The API

`AppIcon`, in the `Javienne.AlternateAppIcons` namespace:

| Member | What it does |
| --- | --- |
| `IsSupported` | Whether this platform and device can change the icon. False on Android 7.1 and earlier, on iOS when the system does not allow it, in a build without alternate icons, and on other platforms. |
| `Icons` | Names of the alternate icons of the app, in alphabetical order. Empty when `IsSupported` is false. |
| `CurrentIcon` | The icon shown, empty for the default icon. |
| `PendingIcon` | The icon waiting for the app to go to the background, on Android: null when none, empty for the default icon. |
| `SetIcon(name)` | Asks for an icon by its name, or for the default icon with null or an empty name. Names are compared regardless of case. Returns an `Awaitable<IconChangeResult>`. |

`IconChangeResult` holds the `Status` of the request, the `Icon` asked for, empty for the default icon, the `Error` when the request failed, and `Succeeded`, true when the icon is shown or will be:

| Status | Meaning |
| --- | --- |
| `Changed` | The icon is changed. On iOS, the system has shown its alert. |
| `Pending` | Android: the icon will change when the app goes to the background. |
| `AlreadySet` | The icon is already shown: nothing was done. On Android, it also cancels a pending change. |
| `NotSupported` | This platform or device cannot change the icon: nothing was done. |
| `Failed` | The icon was not changed: `Error` says why. An unknown name is logged as an error, a refusal of the system as a warning. On iOS, a request made while another change waits for its result fails, without a log. |

## How the change works

### iOS

- Each alternate icon is an icon set of the asset catalog of the app, shown by `setAlternateIconName`. The package uses no private API.
- **iOS shows an alert at each change**, and the package cannot hide it. Change the icon at a calm moment, while the app is in the foreground: right after the player picks an icon, for example.
- `SetIcon` returns once iOS has called back, after its alert.
- The icon stays when the app quits and when the device restarts.

### Android

- The launcher shows the icon of the entry of the app that is enabled. The build declares an `activity-alias` per alternate icon, and one for the default icon, all leading to the activity of Unity. Changing the icon enables the alias of the new icon, then disables the others.
- **Android closes the app when the icon changes**: disabling the alias that started the app ends its activity. `SetIcon` therefore keeps the icon, with the `Pending` status, and the package applies it when the app goes to the background, where the closure goes unseen. The next launch is a cold start. On a Pixel 6 with Android 17, the process quits after each change.
- A pending change only lives in memory: if the process ends before the app goes to the background, the change is lost. Ask again at the next start if it matters.
- The icon stays when the app restarts and when the device restarts.
- The alias of the default icon shows the icon of the Player Settings, with what the project adds to it, such as a monochrome layer.

### Editor and other platforms

- In the editor, `SetIcon` changes the icon at once and logs it, in Play Mode and out of it. The icon is kept until the editor quits, as a device keeps it when the app restarts. The icons are those of the settings page whose name has no error.
- On other platforms, `IsSupported` is false and requests return `NotSupported`.

## The settings page

**Edit › Project Settings › Alternate App Icons** lists the icons, each in a box with its checks, under the number of icons and the size of their image files for each platform. The settings are saved in `ProjectSettings/AlternateAppIconsSettings.asset`: commit that file with your project.

| Setting | Default | What it does |
| --- | --- | --- |
| Fail Build On Error | Off | Fails an iOS or Android build, as soon as it starts, when an icon has an error that concerns its platform. Otherwise the error is logged as a warning and the build has no alternate icon. |

For each icon:

| Field | What it is |
| --- | --- |
| Name | The name the game passes to `SetIcon`: an ASCII letter, then ASCII letters, digits or underscores. Names must differ, regardless of case. |
| iOS › Image | A PNG of 1024 × 1024 px without transparency. iOS scales it down to every size it needs. |
| Android Adaptive Icon › Foreground | A PNG of 432 × 432 px, the 108 dp layer at the xxxhdpi density. Launchers mask it to their shape and show its central 72 dp. |
| Background | A colour, opaque and white by default, or an opaque PNG of 432 × 432 px. |
| Monochrome | Optional. A PNG of 432 × 432 px, white on a transparent background, that Android 13 and later tint to the theme colours when themed icons are on. Without it, the icon keeps its colours. |

The builds copy the image files as they are: the package does not convert, resize or generate them, and the import settings of the textures do not matter. When the minimum API level of the project is below 26, the page notes that Android 7.1 and earlier keep the default icon.

## Image rules

The page checks each icon whenever its name, an image or an image file changes. The builds run the same checks again.

**Errors**: the build of the platform has no alternate icon, or fails if **Fail Build On Error** is on.

- **The name is empty, invalid, or used by another icon**, regardless of case.
- **An image is missing**: the iOS image, the foreground, or the background when it is an image. The monochrome layer is optional.
- **The file is not a PNG.** The build copies the file as it is.
- **The image has the wrong size**: 1024 × 1024 px for iOS, 432 × 432 px for the Android layers.
- **The iOS image has transparent pixels.** iOS icons are opaque.

**Warnings**: these never fail the build.

- **The iOS image has an alpha channel**, although it is opaque. Apple asks for app icons without one.
- **The Android background image has transparent pixels.** Android expects an opaque background layer.
- **Visible pixels of the monochrome layer are not pure white.** Android only keeps the opacity of this layer.

An error of the name concerns both platforms, an error of the iOS image only the iOS build, an error of an Android layer only the Android build. Any error leaves **every** alternate icon out of the build of its platform, not only the icon at fault, and the build logs one warning that lists the issues of all icons.

## What the builds do

### iOS

After Unity generates the Xcode project, the package checks the icons again, then:

- removes the icon sets of a previous build, for builds that append to the project;
- writes an icon set per icon in `Unity-iPhone/Images.xcassets`, named `AlternateAppIcon-<name>.appiconset`: the image of 1024 px as it is, declared as a single size;
- names these icon sets in the build setting `ASSETCATALOG_COMPILER_ALTERNATE_APPICON_NAMES` of the `Unity-iPhone` target.

Xcode then builds the sizes of each device from the image, and lists the icons in `CFBundleIcons` of the Info.plist, where the game reads them.

### Android

After Unity generates the Gradle project, the package checks the icons again, then, in the `unityLibrary` module:

- copies the layers as they are to `res/mipmap-xxxhdpi/alternate_icon_<name>_foreground.png`, `_background.png` and `_monochrome.png`, the name in lower case;
- writes the adaptive icon `res/mipmap-anydpi-v26/alternate_icon_<name>.xml`, and in `res/values/alternate_app_icons.xml` its background colour and a fallback for Android 7.1 and earlier, never shown;
- in the manifest, moves the launcher category from the activity of Unity to the aliases, declared just after it: `com.javienne.alternateappicons.DefaultIcon`, enabled, with the icon of the Player Settings, and `com.javienne.alternateappicons.icon.<name>` for each icon, disabled.

The activity of Unity keeps its main action and stays exported, so `adb shell am start -n` still starts it. Each build first removes what a previous build wrote. The `launcher` module, where Unity writes the default icon, is not touched.

## Download size

Each icon adds its images to the download. Measured on a game built with and without three alternate icons, with Unity 6000.3:

| Platform | Size of an icon | Why |
| --- | --- | --- |
| iOS | 186 to 257 kB, in each device variant | The icon sets have a single size, so every variant keeps the image of 1024 px of each icon. Here, PNG files of 246 to 324 kB. |
| Android | 47 kB | Foreground and monochrome layers of 432 px over a background colour, on a Pixel 6. |

The settings page shows the size of the image files, which the builds compress again.

## Updating a published app

- Changing the images of an icon is safe.
- **Do not remove or rename an alternate icon in an update.** On Android, the devices that show it lose the launcher entry of the app: its alias is gone, and the alias of the default icon is still disabled. The package enables it again the first time the game uses `AppIcon`, but the player must first open the app another way, from its store page for example. Reading `AppIcon.CurrentIcon` when the game starts makes this repair as early as possible. On iOS, this case was not verified.
- Adding the package to a published Android app moves the launcher entry from the activity of Unity to the alias of the default icon. On a Pixel 6, the updated app stayed in the launcher with its default icon. Home screen shortcuts made before the update were not checked.

## Limits

- iOS shows its alert at each change, and Android closes the app at each change, in the background.
- Each icon has one image on iOS, and one image per layer on Android, at the xxxhdpi density: no dark or tinted variant on iOS 18 and later.
- The build steps expect the projects that Unity generates: the asset catalog `Unity-iPhone/Images.xcassets` in the Xcode project, and an activity with the main action and the launcher category in the manifest of `unityLibrary`. Otherwise the build logs a warning and has no alternate icon.
- Verified with Unity 6000.3 and Xcode 27.1, on an iPad (A16) with iPadOS 27 and on a Pixel 6 with Android 17. Not verified on Android 8 to 16, nor with other launchers.

## Tests

The Edit Mode tests are in `Tests/Editor`. To run them from a project, add the package to the `testables` of `Packages/manifest.json`:

```json
"testables": ["com.javienne.alternate-app-icons"]
```

## License

MIT, see [LICENSE](LICENSE).
