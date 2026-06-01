# Bus Puzzle

Unity + C# mobile casual puzzle MVP.

## MVP Concept

Bus Puzzle is an original bus sorting prototype. Passengers wait in a colored queue, and the player taps the matching colored bus to board the front passenger. A bus leaves when its capacity is full. The level clears when every passenger boards correctly, and fails when the next passenger has no available matching bus.

## Asset Direction

The MVP uses Unity primitives only:

- Passengers: colored capsules
- Buses: colored cube bodies, simple wheels, visible seat markers
- Board: simple floor and lane blocks
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

The first playable loop is implemented in `GameManager`: tap a bus whose color matches the front passenger. Use Restart/Next from the on-screen UI.

## Next Steps After MVP

- Add bus queue/parking constraints for deeper puzzle decisions.
- Replace primitive views with prefab/model references.
- Add tweened boarding/departure animation and audio feedback.
- Add Android build profile, analytics, ads, and IAP after the core loop is fun.
