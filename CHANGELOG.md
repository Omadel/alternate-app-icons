# Changelog

All notable changes to this package are documented in this file. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the package uses [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Package skeleton: manifest, runtime assembly, editor assembly and test assembly.
- Name rules of the alternate icons: valid characters, and duplicates regardless of case.
- Project Settings page that declares the alternate icons with their iOS image and Android adaptive icon layers, shows their number and the size of their image files, and checks each icon live: name, missing images, PNG format, size, transparency.
- `AppIcon` API: `IsSupported`, `Icons`, `CurrentIcon`, `PendingIcon`, and `SetIcon(name)`, awaitable, which does nothing for the icon already in place and reports an unknown name as an error.
- Simulation of the change in the editor, logged and kept until the editor quits.
- Android build step: checks the icons, writes their adaptive icons into the Unity library module, and declares an activity alias per icon in its manifest, the launcher entry moving to the alias of the default icon. An error leaves every alternate icon out of the build, or fails it with Fail Build On Error.
- Android platform: enables the alias of the requested icon when the app goes to the background, on Android 8.0 and later.
- iOS build step: checks the icons, writes a single size icon set per icon into the asset catalog of the Xcode project, and names them in `ASSETCATALOG_COMPILER_ALTERNATE_APPICON_NAMES`, so that Xcode builds them and lists them in the Info.plist.
- iOS platform: `setAlternateIconName` through a native plugin, with the icons read from the Info.plist.
- Builds for iOS and Android fail at their start when an icon has an error and Fail Build On Error is on.
- `AppIcon.Icons` lists the icons in alphabetical order on every platform.
