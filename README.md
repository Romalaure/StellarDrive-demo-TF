# StellarDrive Demo TF

MelonLoader mod for the StellarDrive demo, installable with [StellarModManager](https://github.com/jollyname/StellarModManager).

## Features

- **Paint tool**: the demo contains the game's paint tool but never hands it out. Press **F8** in a
  world to get one. It paints walls and floors (game feature) and, with this mod, doors, machines
  and every other placed part. Part colors are saved in `tf-paint.json` next to the world save
  (save from the pause menu: quitting does not save) and synced to every player who has the mod.
- **Spray gun model**: replaces the placeholder paint tool model with a spray gun whose cup and
  nozzle show the selected color.

- **Paint HUD**: a card while holding the paint tool; right click opens one panel replacing the
  game's paint menu, with a saturation/value square and hue bar, quick colors, hex input, wall side
  mode (one face or both), finishes (gloss, metal, glow) for placed parts, and presets saved in
  `UserData/TF/paint-presets.json`.
- **Lamps** (needs SDModKit): ceiling light, wall light, spotlight and light strip in a new "TF"
  build tab. Lit while no signal cable is plugged in; with one they follow the signal (0 = off,
  1 = full). Painting a lamp sets its light color. They adapt to their surroundings: brighter at
  night and indoors, softer in open daylight.
- **Mirrors** (needs SDModKit): rear-view mirror and wall mirror with real-time planar reflections.
  Only mirrors near you and facing you render; resolution and distance are in the settings.

Settings live in `UserData/MelonPreferences.cfg` under `[TF]`.

## Install

**With StellarModManager**: once the mod is listed in the online repository, install it from the
"Online" tab, then press "Install to game". Also install **SDModKit** (needed for lamps and mirrors).

**By hand**: download `StellarDrive-demo-TF.zip` from the
[latest release](https://github.com/Romalaure/StellarDrive-demo-TF/releases/latest) and copy
`Mods/StellarDriveDemoTF.dll` into the game's `Mods` folder (MelonLoader 0.7 required).

Every player in a multiplayer session needs the mod.

## Requirements

- StellarDrive Demo with MelonLoader 0.7.x installed
- .NET SDK 8+ (builds a .NET Framework 4.7.2 assembly)
- SDModKit installed in the game's `Mods` folder (referenced at build time, optional at runtime)

## Build

```
dotnet build src/StellarDriveDemoTF/StellarDriveDemoTF.csproj
```

The DLL is copied into the game's `Mods` folder after each build. The game path defaults to
`G:\SteamLibrary\steamapps\common\StellarDrive Demo`; override it with `-p:GamePath=...` or a
`Directory.Build.props.user` file.

Game and MelonLoader assemblies are referenced from the local install and are never committed.

## Release

1. Bump `version` in `Mods/mod.json` and in `Core.cs`.
2. Run `tools/package.ps1`, which produces `dist/StellarDrive-demo-TF.zip`.
3. Create a GitHub release tagged `v<version>` and attach the zip.

StellarModManager reads `Mods/mod.json` from `main` and downloads
`releases/download/v<version>/StellarDrive-demo-TF.zip`.
