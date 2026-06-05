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

Level assets can be checked in the editor with `Bus Puzzle/Levels/Validate Level Assets`. Generated stages can be rebuilt with `Bus Puzzle/Levels/Rebuild Generated Stage Set`; that command writes only verified, fully solvable stages to `Assets/BusPuzzle/Resources/Levels/Generated/`, keeps the hand-tuned `Level01`-`Level03` assets untouched, and updates `LevelSequence.asset` for asset-based builds. If any stage cannot find a verified candidate in the configured attempt budget, the rebuild aborts without overwriting the existing generated set.

Play mode loads `Assets/BusPuzzle/Resources/Levels/Generated/GeneratedLevelSequence.asset` first. `StageGenerationConfig.asset` is a build-time generation config, not the primary runtime level source. The default difficulty pattern is three Normal stages, one Hard stage, then one SuperHard stage. Vehicle counts scale from 25 toward 50 over the generated set, and SuperHard stages can include one to five seed-driven garage obstacles with hidden queued vehicles.

Runtime generation is retained only as a development fallback. Release builds should ship with the verified generated level pack and load levels from assets, not from on-device generation or best-effort cache candidates.

## AdMob Setup

Rewarded ads are configured through `Assets/BusPuzzle/Resources/Ads/AdMobSettings.asset`.

- Reward type: `station_slot_unlock`
- Reward amount: `1`
- VIP reward type: `vip_bus_teleport`
- VIP reward amount: `1`
- Google Mobile Ads Unity SDK `com.google.ads.mobile` is installed through OpenUPM in `Packages/manifest.json`.
- `Assets/csc.rsp` enables `BUS_PUZZLE_ADMOB`, so the real AdMob adapter is compiled when Unity resolves the package.
- Editor and development builds use Google's rewarded test ad unit IDs.
- Release Android/iOS builds use the production IDs in `AdMobSettings.asset`.
- Release builds fail before build if any mobile production ID is missing, still points at Google's test publisher, or the `BUS_PUZZLE_ADMOB` scripting define is not enabled.

Current known IDs:

- Android app ID: `ca-app-pub-5773331970563455~5379288524`
- Android station rewarded production ad unit ID: missing, needs the `/...` rewarded unit ID from AdMob
- Android VIP rewarded production ad unit ID: missing, needs the `/...` rewarded unit ID from AdMob
- iOS app ID: currently Google's test app ID, needs the production `~...` app ID from AdMob before release
- iOS station rewarded production ad unit ID: `ca-app-pub-5773331970563455/7771471978`
- iOS VIP rewarded production ad unit ID: missing, needs the `/...` rewarded unit ID from AdMob

If Unity cannot download OpenUPM packages, the project falls back to a compile error until package resolution succeeds; this is intentional so ad builds do not silently ship in mock mode.

## Next Steps After MVP

- Add tuned level layouts with stronger blocking and release-order puzzles.
- Replace primitive views with prefab/model references.
- Add tweened boarding/departure animation and audio feedback.
- Add Android build profile, analytics, ads, and IAP after the core loop is fun.
