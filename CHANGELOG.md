# Changelog

All notable changes to this package are documented in this file. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the package uses [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Package skeleton: manifest, runtime assembly, editor assembly and test assembly.
- Name rules of the alternate icons: valid characters, and duplicates regardless of case.
- Project Settings page that declares the alternate icons with their iOS image and Android adaptive icon layers, shows their number and the size of their image files, and checks each icon live: name, missing images, PNG format, size, transparency.
- `AppIcon` API: `IsSupported`, `Icons`, `CurrentIcon`, `PendingIcon`, and `SetIcon(name)`, awaitable, which does nothing for the icon already in place and reports an unknown name as an error.
- Simulation of the change in the editor, logged and kept until the editor quits.
