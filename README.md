# Bus Puzzle

Unity + C# mobile casual puzzle MVP.

## MVP Concept

Bus Puzzle is an original bus dispatch puzzle prototype. Buses sit on a compact lower grid with a white arrow on top. The player taps a bus, and it tries to drive straight in the arrow direction. If another bus blocks its path, it bumps and returns. If the path is open, it moves into a station slot under the passenger rotary.

Passenger units are not a fixed queue. Four-person units circulate around the top rotary from the left/right waiting areas. When a bus of the matching color is parked in the open boarding area, the matching unit boards as it reaches the lower gate of the rotary.

Passenger units represent four people. Bus sizes are:

- Small: 1 board cell, 16 people, 4 passenger units
- Medium: 2 board cells, 24 people, 6 passenger units
- Large: 3 board cells, 40 people, 10 passenger units

The station starts with four active slots. Four extra ad slots are shown as locked visual placeholders for a post-MVP rewarded-ad unlock.

The passenger rotary scales from level data:

- Small rotary: up to 40 passenger units, two lanes
- Large rotary: up to 80 passenger units, four lanes

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
    UI/
Packages/
ProjectSettings/
```

## How To Run

1. Open this repository with Unity `6000.2.6f2` or a compatible Unity 6 editor.
2. Open `Assets/BusPuzzle/Scenes/MainGame.unity`.
3. Press Play.

The first playable loop is implemented in `GameManager`: tap buses to dispatch them along their arrows, manage the station slots, and let matching passenger units board automatically when they walk past the rotary gate. Use Restart/Next from the on-screen UI.

## Next Steps After MVP

- Add tuned level layouts with stronger blocking and release-order puzzles.
- Implement rewarded-ad unlock for the four extra station slots.
- Replace primitive views with prefab/model references.
- Add tweened boarding/departure animation and audio feedback.
- Add Android build profile, analytics, ads, and IAP after the core loop is fun.
