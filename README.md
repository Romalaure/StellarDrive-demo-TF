# StellarDrive Demo TF

MelonLoader mod for the StellarDrive demo, installable with [StellarModManager](https://github.com/jollyname/StellarModManager).

## Features

- **Paint tool**: the demo contains the game's paint tool but never hands it out. Take one from
  the TF **tool rack** (see below). It paints walls and floors (game feature) and, with this mod, doors, machines
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
- **TF build tabs** (needs SDModKit): the build menu does not scroll, so the parts are split over
  two tabs of three rows: "TF" (lamps, chairs) and "TF Objets" (camera and devices, infinite parts,
  floor doors).
  - **Lamps**: ceiling light, wall light, spotlight, light strip, recessed spot, industrial
    lantern, light panel, pendant, LED strip (2 m), floor lamp, blinking beacon (red), rotating
    beacon (orange), and corner light, corner strip and corner spotlight, wedge shaped to sit in
    the angle between a ceiling (or floor) and a wall. No plug, always on; painting a lamp sets its
    light color. They adapt to their surroundings: brighter at night and indoors, softer in open
    daylight. The game's shaders only take the sun into account, so lamps light the walls, floors
    and parts around them themselves: per vertex, on the CPU, into overlay meshes, recomputed only
    when the lamp, the hull or nearby parts change (`LampLightStrength`, 1; 0 turns it off).
  - **Surveillance camera**: fix it on a wall, ceiling or hull; it looks away from its surface,
    tilted 30 degrees (rotate it while placing to aim). Press **F9** for the camera tablet: it
    shows the selected camera's live image (left/right arrows switch camera) and **P** takes out
    the build tool with a camera selected to place a new one, **Z** cycles small / large / full
    screen (`TabletSize`). Cameras only render while the tablet is open, no wider than the screen
    shows the image, and without the game's full screen atmosphere and cloud passes, which cost
    more than the scene itself (`CameraFps`, 15; `CameraResolution`, 960; `CameraSky` turns the sky
    back on).
  - **Chairs**: rustic chair, futuristic seat, copilot seat, club armchair, bar stool, bench,
    office chair and shuttle passenger seat. They are built on the game's pilot seat, so you sit in
    them the same way, but they have no effect on the ship: next to no weight, no plug, flight
    controls ignored, never a gyroscope or the flight orientation. `CopilotCanFly` (off by
    default) turns the copilot seat into a real second control seat. Paint them to change their
    color.
  - **Infinite parts**, cloned from the game's own parts so power, fluid networks, chests, saving
    and sync work as usual: infinite generator (20 kW forever, no wood); infinite oxygen and
    ethanol tanks (always full, they feed connected pipes without end); infinite resource chest
    (iron, glass, copper, aluminum, wood, ice, biofuel, water and stellar shards, full stacks that
    refill as you take them, whatever moves the items; the last slot is a bin).
  - **Devices**, used by looking at them and pressing **T** (`UseKey`):
    - **Teleport capsule**: pick another capsule, on this ship or any other, and the host moves
      you there (the same way the game respawns a player at a ship). Needs at least two capsules.
      Capsules can be named; names are synced and saved in `tf-data.json` next to the world.
    - **Radio**: plays the game's music tracks (read from its FMOD sound banks) or the sound of a
      YouTube link. The host decides what each radio plays and every player near it hears it,
      fading with distance (`RadioVolume`). For YouTube, the mod downloads
      [yt-dlp](https://github.com/yt-dlp/yt-dlp) once into `UserData/TF/tools`, fetches the audio
      track and decodes it with Windows Media Foundation into `UserData/TF/radio` (the 8 latest
      are kept, 20 minutes at most). YouTube's fragmented MP4 carries an edit list that makes
      Media Foundation stop at once, so it is stripped before decoding. Only the video id is
      taken from the link.
    - **Wardrobe**: choose your suit's main color and your helmet and backpack color
      (`OutfitPrimary`, `OutfitSecondary`); every player with the mod sees them.
  - **Trapdoors** (2x2 and 1x1) and **horizontal docking doors** (docking downward or upward):
    the game's door and docking door, cloned and snapping like floor tiles. Remove the floor tiles
    and place them in the gap. The game already lays floor parts flat; each door's frame (read by
    the game for its model, colliders, interaction, preview, magnet and docking joint) is recentred
    on its tiles, and docking doors always face down or up, so ships dock one above the other.
- **Tool rack** (TF Objets): use it to take a paint gun or a **schematic tablet**, a new
  inventory item; held in the hand, a left click opens the schematics window.
- **Ship schematics** (host only, from the schematic tablet): copy the nearest ship, with the
  ships docked to it, its cables, TF paint and capsule names, to `UserData/TF/schematics`, then
  build it again in front of you, in this world or another. Uses the game's own developer ship
  export/import; building is free.
- **Third person in flight**: from the pilot seat, **V** (`ThirdPersonKey`) moves the view
  behind the ship, following where you look; the mouse wheel sets the distance
  (`ThirdPersonDistance`).

Settings live in `UserData/MelonPreferences.cfg` under `[TF]`.

The mod also works around a game bug: the host sometimes keeps a removed planet space active,
and every physics tick then throws "Space id N does not exist", which stops ships, players and
physics for that tick. The planet collider update is skipped for that tick instead. Each patch
is applied on its own, so a game update breaking one only turns that feature off.

## Install

**With StellarModManager**: once the mod is listed in the online repository, install it from the
"Online" tab, then press "Install to game". Also install **SDModKit** (needed for the TF build tab parts).

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
