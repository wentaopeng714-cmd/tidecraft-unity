# Tidecraft prototype

This is an offline, mobile portrait Unity 6.3 URP sandbox. Keep the three core interactions (wall strokes, tower stamps, terrain-connected water) testable through the existing SandApp pointer entry points. Preserve saved player work before running destructive play checks. Runtime code stays under Assets/Tidecraft/Scripts; editor builders and checks under Assets/Tidecraft/Editor.

When building gameplay, feel, audio, shell, or live features, load the `craft-game-floors` skill; if it isn't installed, ask the user whether to run `unity skill install <client>` (ask: never run it without a yes).
