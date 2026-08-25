# Ravenfield Tactical AI

Ravenfield Tactical AI is a BepInEx plugin for Ravenfield EA38. It extends the
game's existing AI, cover, squad, damage, and performance systems without
shipping or modifying Ravenfield's proprietary assemblies.

## Tactical behavior

- Splits eligible AI-led attack squads into alternating support and maneuver
  elements.
- Holds the support element while its normal weapon AI provides covering fire.
- Moves the maneuver element in short, serialized bounds toward local cover.
- Gives the elements opposite lateral offsets to form wider firing lines and
  attempt lightweight flanking.
- Releases a failed bound to Ravenfield instead of advancing the other element
  while teammates remain exposed.
- Extends Ravenfield's temporary-cover behavior beyond close-quarters cells.
- Makes ordinary direct infantry firearm hits lethal, with a configurable
  long-range handgun exception.

The F8 panel exposes live controls for bounding overwatch, squad size, bound
distance, failure timeout, defensive cover, infantry lethality, player
lethality, handgun range, AI tick budgets, fire control, and remains budgets.

## Requirements

- Windows
- Ravenfield EA38
- BepInEx 5 installed in the Ravenfield directory
- .NET SDK 8 or newer

The project references Ravenfield and BepInEx assemblies from your own game
installation. Those assemblies are not included in this repository.

## Test

The policy tests do not require Ravenfield:

```powershell
dotnet test .\tests\Ravenfield.AiTick.Tests\Ravenfield.AiTick.Tests.csproj
```

## Build

Set the Ravenfield installation directory for the current shell without adding
it to a project file:

```powershell
$env:RAVENFIELD_GAME_DIR = Read-Host "Ravenfield installation directory"
dotnet build .\src\Ravenfield.AiTick\Ravenfield.AiTick.csproj `
  --configuration Release `
  -p:InstallToGame=false
```

`InstallToGame` defaults to `false`; a normal build never copies files into the
game directory.

The resulting plugin is under
`src\Ravenfield.AiTick\bin\Release\Ravenfield.AiTick.dll`.

## Install for a playtest

1. Close Ravenfield.
2. Back up any existing `BepInEx\plugins\Ravenfield.AiTick.dll`.
3. Copy the new DLL into `BepInEx\plugins`.
4. Start Ravenfield and open the F8 settings panel.
5. Check `BepInEx\LogOutput.log` for `Bounding overwatch activated` and
   `One-hit infantry damage applied` diagnostics.

The current tactical coordinator applies only to AI-led, on-foot attack squads
with at least four available infantry. Vehicle squads, player-issued orders,
scripted paths, and squads close to their objective retain Ravenfield's normal
movement.

## Privacy and repository contents

This repository contains plugin source, engine-independent policy tests, and
build documentation only. It intentionally excludes game files, BepInEx files,
logs, configuration files, save data, screenshots, binaries, absolute local
paths, credentials, and machine-specific metadata.
