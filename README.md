# StellarDrive Demo TF

MelonLoader mod for the StellarDrive demo, installable with [StellarModManager](https://github.com/jollyname/StellarModManager).

## Requirements

- StellarDrive Demo with MelonLoader 0.7.x installed
- .NET SDK 8+ (builds a .NET Framework 4.7.2 assembly)

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
