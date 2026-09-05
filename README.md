# Bus Puzzle

Unity + C# mobile casual puzzle MVP.

## MVP Concept

Bus Puzzle is an original bus dispatch puzzle prototype. Buses sit on a compact lower grid with a white arrow on top. The player taps a bus, and it tries to drive straight in the arrow direction. If another bus blocks its path, it bumps and returns. If the path is open, it moves into a station slot under the passenger rotary.

Passenger units are not a fixed queue. Four-person units circulate around the top rotary from the left/right waiting areas. When a bus of the matching color is parked in the open boarding area, the matching unit boards as it reaches the lower gate of the rotary.

Passenger units represent four people. Bus sizes are:

- Small: 1 board cell, 16 people, 4 passenger units
- Medium: 2 board cells, 28 people, 7 passenger units
- Large: 3 board cells, 40 people, 10 passenger units

The station starts with four active slots. Four extra ad slots can be unlocked one at a time by tapping a locked station `+` and watching the rewarded station-slot ad, up to eight active slots for the current stage.

The passenger rotary scales from level data:

- Small road preset: up to 30 passenger units
- Medium road preset: up to 35 passenger units
- Large road preset: up to 40 passenger units

## Asset Direction

The MVP uses Unity primitives only:

- Passenger units: four colored capsules in a long vertical unit that keeps walking around the rotary
- Buses: colored cube bodies, simple wheels, white direction arrows, visible unit markers
- Board: size-aware top passenger rotary with visible lanes/rails, four open station slots, four locked ad slots, and a lower bus puzzle yard
- UI: runtime-created Unity UI buttons and status text

This keeps the first version playable without paid assets. Later, the visual layer can be replaced with Blender models, animations, particles, and mobile polish while keeping the core rules and level data intact.

## Project Structure

```text
Assets/BusPuzzle/
  Scenes/MainGame.unity
  Resources/Levels/
  Scripts/
    Board/
    Core/
    Data/
    Ads/
    UI/
Packages/
ProjectSettings/
```

## How To Run

1. Open this repository with Unity `6000.2.6f2` or a compatible Unity 6 editor.
2. Open `Assets/BusPuzzle/Scenes/MainGame.unity`.
3. Press Play.

The first playable loop is implemented in `GameManager`: tap buses to dispatch them along their arrows, manage the station slots, and let matching passenger units board automatically when they walk past the rotary gate. Use Restart/Next from the on-screen UI.

Release level assets can be checked in the editor with `Bus Puzzle/Levels/Validate Level Assets`. Generated stages can be rebuilt with `Bus Puzzle/Levels/Rebuild Generated Stage Set`; that command writes verified, fully solvable stages to `Assets/BusPuzzle/Resources/Levels/Generated/`, keeps the hand-tuned `Level01`-`Level03` development assets untouched, and updates `LevelSequence.asset` for asset-based builds. The current release pack ships 200 prebuilt generated stages.

Play mode loads `Assets/BusPuzzle/Resources/Levels/Generated/GeneratedLevelSequence.asset` first. `StageGenerationConfig.asset` remains available for runtime generation after the shipped generated pack, so stages 1-200 are asset-backed and stage 201+ can be generated on demand. The default difficulty pattern is three Normal stages, one Hard stage, then one SuperHard stage. Vehicle counts scale from 25 toward 50 over the generated set, and SuperHard stages can include one to five seed-driven garage obstacles with hidden queued vehicles.

Runtime generation is expected only after the shipped generated pack is exhausted. Release builds should keep the verified generated level pack in sync with `StageGenerationConfig.asset` and keep the clear-screen/preload transition active so the first generated stage after the pack does not cause a visible stall.

## LevelPlay Ads Setup

The active mobile provider is Unity LevelPlay with the Unity Ads adapter. App keys and banner/rewarded ad unit IDs are configured in `Assets/BusPuzzle/Resources/Ads/LevelPlaySettings.asset`.

- Unity LevelPlay `com.unity.services.levelplay` 9.5.0 is pinned in `Packages/manifest.json`.
- The Unity Ads adapter uses its official Android and iOS dependencies under `Assets/LevelPlay/Editor/`.
- `Assets/csc.rsp` enables `BUS_PUZZLE_LEVELPLAY`; `BUS_PUZZLE_ADMOB` stays disabled while AdMob is suspended.
- Existing AdMob services and settings remain in the repository as a rollback provider, but the Google Mobile Ads package and direct native dependencies are not included in LevelPlay release builds.
- Existing Firebase Remote Config switches still gate global, platform, banner, rewarded, and banner-start-stage behavior.
- Rewarded gameplay changes run only after LevelPlay's rewarded callback. This is the phase-one client-authoritative flow; add signed S2S verification before ads protect purchased, withdrawable, competitive, or otherwise high-value server-authoritative assets.
- Until a complete consent flow is added, LevelPlay is initialized in contextual-only mode. iOS tracking authorization is not requested.
- LevelPlay automatically adds the installed networks' SKAdNetwork IDs during iOS post-processing.
- Mobile release builds fail validation when the provider define, IDs, SDK/adapter dependencies, privacy mode, or SKAdNetwork setting is inconsistent.

Do not document production ad IDs in public-facing material. Before enabling ads, confirm that the LevelPlay dashboard maps each platform's Unity Ads game ID and placements to the corresponding `Banner_Main` and `Rewarded_Main` ad units. Use app-version conditions in Remote Config so an older AdMob build cannot be re-enabled when the LevelPlay build rolls out.

## Next Steps After MVP

- Add tuned level layouts with stronger blocking and release-order puzzles.
- Replace primitive views with prefab/model references.
- Add tweened boarding/departure animation and audio feedback.
- Add Android build profile, analytics, ads, and IAP after the core loop is fun.
