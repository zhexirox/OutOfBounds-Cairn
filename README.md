# OutOfBounds

A MelonLoader mod for **Cairn** that removes invisible map boundaries, allowing free exploration beyond the playable area.

## Features

- **Automatically active** — just install and play
- **Removes all boundary zones** — NoGoArea and NoGoAreasManager deactivated
- **No teleport-back** — walk, climb, and explore freely past the edges
- **Zero configuration** — no hotkeys, no settings
- **Lightweight** — batched scanning with minimal performance impact

## Installation

1. Install [MelonLoader](https://melonwiki.xyz/) v0.7.2 (Nightly builds only)
2. Place `OutOfBounds.dll` in your `Cairn/Mods/` folder
3. Launch the game

## How It Works

Cairn uses invisible trigger zones (`NoGoArea`) managed by a `NoGoAreasManager` to define the playable area. When the player enters these zones, they get teleported back and shown a warning.

This mod finds all `NoGoArea` and `NoGoAreasManager` components in loaded scenes and deactivates their GameObjects entirely — no triggers, no teleports, no warnings.

### Performance

Cairn streams dozens of sub-scenes as you move through the mountain. A naive approach (scanning every scene load) causes FPS drops because the game can load 10-30 scenes in a single burst.

**Our solution:** Batched deferred scanning.

- Each scene load resets a 0.5s timer
- The scan only runs **once**, after the last scene in a burst finishes loading
- Already-known boundary objects are tracked by `instanceID` and skipped
- Result: a burst of 30 scene loads = **1 scan**, not 30

### Why Deactivate GameObjects Instead of Patching Methods?

Harmony patches require `typeof()` on game types, which causes `BadImageFormatException` in IL2CPP games. Deactivating GameObjects via IL2CPP reflection avoids this entirely and is the safest approach for Cairn's IL2CPP runtime.

## Technical Details

- **Game**: Cairn (Unity 6, IL2CPP)
- **Mod Loader**: MelonLoader 0.7.2
- **Target Components**: `NoGoArea`, `NoGoAreasManager`
- **Detection**: `MonoBehaviour.GetIl2CppType().Name` matching
- **Deactivation**: `GameObject.SetActive(false)`

## Building from Source

```bash
cd Cairn/ModSource/OutOfBounds
dotnet build -c Release
copy bin\Release\OutOfBounds.dll ..\..\Mods\
```

## Bug Reports & Feedback

Found a bug or something feels off? Please [open an issue](https://github.com/zhexirox/OutOfBounds-Cairn/issues) — any feedback helps improve the mod!

## License

MIT License — Feel free to use, modify, and distribute.
