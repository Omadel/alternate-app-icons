# Changelog

All notable changes to this package are documented in this file. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the package uses [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.1] - 2026-10-27

### Fixed

- Android build step: the activity of Unity keeps its launcher filter in the manifest of the Unity library module, so that the build steps of other packages that look for the main activity by its launcher category find it, even when they run after this one. The manifest of the launcher module now removes this filter from the merged manifest, where the launcher entry is still on the aliases only.

## [1.0.0] - 2026-10-26

First release, for Unity 6000.0 and later.

### Added

- `AppIcon` API: `IsSupported`, `Icons`, `CurrentIcon`, `PendingIcon`, and `SetIcon(name)`, awaitable, with the status of the request: `Changed`, `Pending`, `AlreadySet`, `NotSupported` or `Failed`.
  - Names are compared regardless of case, and listed in alphabetical order on every platform.
  - Asking for the icon already shown does nothing; an unknown name fails and is logged as an error.
- iOS platform: `setAlternateIconName` through a native plugin, with the icons read from the Info.plist of the app.
- Android platform, Android 8.0 and later: the alias of the requested icon is enabled when the app goes to the background. The first time the game uses `AppIcon`, the alias of the default icon is enabled again if no alias is.
- Simulation of the change in the editor, logged and kept until the editor quits.
- Project Settings page, Project › Alternate App Icons, saved in `ProjectSettings/AlternateAppIconsSettings.asset`:
  - each icon with its name, its iOS image and the layers of its Android adaptive icon: foreground, background colour or image, optional monochrome;
  - the number of icons and the size of their image files;
  - Fail Build On Error.
- Checks of each icon, live on the settings page and again at build time:
  - errors: name empty, invalid or duplicate regardless of case, missing image, not a PNG file, wrong size (1024 px for iOS, 432 px for Android), transparent pixels in the iOS image;
  - warnings: alpha channel in an opaque iOS image, transparent pixels in the Android background, visible pixels not pure white in the monochrome layer.

  An error concerns the build of its platform: it leaves every alternate icon out of that build, or fails the build from its start with Fail Build On Error. Warnings never fail the build.
- iOS build step: an icon set per icon in the asset catalog of the Xcode project, named in `ASSETCATALOG_COMPILER_ALTERNATE_APPICON_NAMES`, so that Xcode builds them and lists them in the Info.plist.
- Android build step: the adaptive icon of each icon in the Unity library module, and an activity alias per icon in its manifest, the launcher entry moving to the alias of the default icon.
- README, with the measured download size of an icon.
- Edit Mode tests for the names, the checks, the reading of the PNG files, the logic of the API, the aliases, the manifest, the resources, the checks at build time, and the iOS and Android build steps.

[1.0.1]: https://github.com/Omadel/alternate-app-icons/releases/tag/v1.0.1
[1.0.0]: https://github.com/Omadel/alternate-app-icons/releases/tag/v1.0.0
