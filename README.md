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
- **Camouflage brush**: in the paint panel, switch from "Couleur unie" to "Camouflage" and sweep
  walls, floors and parts: each one gets a palette color from a 3D pattern at its place in the
  ship. Palettes: forest, desert, arctic, urban, navy, night, autumn, and shades of the brush
  color; styles: spots, stripes, digital; adjustable patch size and "new pattern" button. Plain
  colors are what gets sent and saved, so it syncs like normal paint.
- **Lamps** (needs SDModKit), in a "TF" build tab: ceiling light, wall light, spotlight, light
  strip, recessed spot, industrial lantern, light panel, pendant, LED strip (2 m), floor lamp,
  blinking beacon (red) and rotating beacon (orange). They have no plug and are always on.
  Painting a lamp sets its light color. They adapt to their surroundings: brighter at
  night and indoors, softer in open daylight.
- **Corner lamps** (needs SDModKit): corner light, corner strip and corner spotlight, wedge
  shaped to sit in the angle between a ceiling (or floor) and a wall, lighting the room diagonally.
  Place them with their back against the wall.
- **Infinite parts** (needs SDModKit), in a "TF Infini" build tab, cloned from the game's own
  parts so power, fluid networks, chests, saving and sync work as usual:
  - infinite generator: 20 kW forever, no wood;
  - infinite oxygen tank and infinite ethanol tank: always full, they feed connected pipes
    (thrusters, other tanks) without end;
  - infinite resource chest: iron, glass, copper, aluminum, wood, ice, biofuel, water and stellar
    shards that refill as you take them; the last slot is a bin.
- **Chairs** (needs SDModKit), in a "TF Sièges" build tab: rustic chair, futuristic seat, copilot
  seat, club armchair, bar stool, bench, office chair and shuttle passenger seat. They are built
  on the game's pilot seat, so you sit in them the same way, but they have no effect on the ship:
  next to no weight, no plug, flight controls ignored, never a gyroscope or the flight
  orientation. `CopilotCanFly` (off by default) turns the copilot seat into a real second control
  seat. Paint them to change their color.
- **Mirrors** (needs SDModKit): rear-view mirror and wall mirror with real-time planar reflections.
  Only the nearest mirror in view refreshes (`MirrorCount`, 1), `MirrorMovingFps` (20) times per
  second while you move and `MirrorIdleFps` (2) while you stand still, reflecting the ship,
  players and objects up to `MirrorViewDistance` meters (40) over a sky-colored background
  (`MirrorReflectWorld` adds the planet and scenery), with an image sized to how big the mirror
  looks on screen (up to `MirrorMaxResolution`, 320).

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

1. Bump the version in `Mods/mod.json`, the csproj and `TFMod.cs`.
2. Run `tools/package.ps1`, which produces `dist/StellarDrive-demo-TF.zip`.
3. Create a GitHub release tagged `v<version>` and attach the zip.

StellarModManager reads `Mods/mod.json` from `main` and downloads
`releases/download/v<version>/StellarDrive-demo-TF.zip`.
