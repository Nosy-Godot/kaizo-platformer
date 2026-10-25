# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

Godot 4.7 / C# (`Godot.NET.Sdk/4.7.1`, net8.0) 2D platformer. Respond to the user in pt-BR; code and identifiers in en-US.

## Commands

```bash
godot --path .        # open editor (run the game from the editor / `godot --path . Dev/Levels/teste_level.tscn`)
dotnet build kaizo-project.sln                      # compile game + modules (configs: Debug, ExportDebug, ExportRelease)
dotnet test Modules/Nosy.FSM.Tests                  # FSM unit tests (xUnit, no engine, ~40 ms)
dotnet test kaizo-project.csproj --settings .runsettings  # GdUnit4 tests in Tests/ (boots Godot headless via GODOT_BIN)
dotnet test kaizo-project.sln --settings .runsettings     # everything
```

No linter is set up. Formatting follows [.editorconfig](.editorconfig).

Avoid comments in code: no XML doc comments (`/// <summary>`) and nothing that restates what the code says. Add a short comment only where something is not obvious from the code (a hidden constraint, a workaround, a surprising order of operations).

### Tests

- `Modules/Nosy.FSM.Tests/`: xUnit against the FSM only. `RecordingState` logs callbacks (`"A.Exit(B)"`, `"B.Fixed"`) so tests assert exact call order.
- `Tests/`: GdUnit4 (`gdUnit4.api` + `gdUnit4.test.adapter` NuGet packages in the game csproj). Suites use `[TestSuite]`, `[RequireGodotRuntime]` and `ISceneRunner.Load("res://Tests/Scenes/...")`; step physics with `await ISceneRunner.SyncPhysicsFrame`. The packages, `Tests/**` and the adapter-generated `gdunit4_testadapter_v*/` are excluded from `ExportRelease`.
- `gdUnit4.analyzers` is not used: it needs a newer Roslyn than SDK 8.0.1xx (warning CS9057).

## Architecture

Game scripts live in `Dev/Code/Scripts/`; scenes in `Dev/Prefabs/` (`player.tscn`) and `Dev/Levels/` (`teste_level.tscn`). The `BetterTerrain` addon is autoloaded. Display is 1280×720 with viewport stretch, `keep_height`, integer scale. Input actions (`Move_Left/Right/Up/Down`, `Jump`, `Dash`) are defined in `project.godot`.

### FSM (`Modules/Nosy.FSM/`, namespace `NosyCore.FSM`)

A separate class library (`Microsoft.NET.Sdk`, no Godot reference: keep it engine-agnostic) referenced by the game via `ProjectReference`. The game csproj has `<Compile Remove="Modules/**" />` and `Modules/.gdignore` hides it from Godot. Files outside the module have no namespace.

- States are keyed **by instance**, so several instances of one type are distinct states. `SetState` on an unregistered state throws; use `AddState` for states without transitions.
- `TransitionMode` (default `FixedUpdate`): in that mode `FixedUpdate` evaluates transitions and then calls `OnFixedUpdate`, while `Update` only calls `OnUpdate`. At most one transition per tick. Any-transitions are checked first, then the current state's, each in registration order (**order = priority**).
- Transitions to the current state are skipped unless `allowReentry`. Conditions are `IPredicate`/`Func<bool>`, composable with `Predicates.From/And/Or/Not`.
- Event transitions: `AddTriggerTransition`/`AddAnyTriggerTransition` + `Fire(trigger)` switch immediately (not on a tick).
- The constructor does not enter the initial state; `Start()` (or the first tick/`SetState`/`Fire`) does. `StateChanged`, `PreviousState`, `TimeInState` and `TicksInState` are exposed.
- Changing state from inside `OnEnter`/`OnExit` throws (a `StateChanged` handler may change state).
- States extend `BaseState<TContext>` (typed `_context`), not `IState` directly.

### Player (`Player/`)

- `Player : CharacterBody2D` builds the four states (grounded, air, wall slide, dash) and registers all transitions in `_Ready()` (order = priority; dash is checked first from every non-dash state).
- States are one class per file in `Player/States/` and hold only per-state logic; shared movement helpers (`ProcessRun`, `ProcessGravity`, `ProcessVarJump`, `TryJump`, `TryWallJump`, `TrySuperJump`, `CornerCorrect`, `WallAt`/`WallDir`, `DoMoveAndSlide`) live on `Player`. Tuning values are `[Export]`s grouped by `[ExportGroup]`, roughly Celeste's constants ×2 (16 px tiles).
- **Single velocity**: states mutate `Player.Vel`; `DoMoveAndSlide()` applies upward corner correction, copies `Vel` into `Velocity`, calls `MoveAndSlide()` and copies the slid velocity back (so collisions zero blocked components).
- **Timers are physics ticks** (coyote, jump/dash buffers, var jump, wall-jump forced input, dash cooldown/refill), decremented in `TickTimers()` before the FSM tick. Coyote and dash refill are refreshed there while `IsOnFloor()`.
- Wall detection uses `TestMove` probes (`WallAt`), not `IsOnWall()`. Wall jump works within `WallJumpCheckDist` px of a wall and locks horizontal input away from it for `WallJumpForceTicks`; holding toward the wall during that lock trims the push target to `WallJumpTowardSpeedMult` × run speed (faster single-wall climbing).
- A single wall can be climbed indefinitely with wall jumps (by design). Tiles with the TileSet custom data `slippery = true` are ignored by `WallAt` (no wall slide or wall jump); the cell is found from the probe's contact point. Locked by `Tests/WallJumpTest.cs`.
- Dash: `DashFreezeTicks` of no movement, then the 8-way `PlayerInput.DashAim` (fallback `Facing`) is read and `Vel = dir * DashSpeed` for `DashTicks`. A buffered jump while grounded/coyote turns it into a super (horizontal) or hyper (down-diagonal) jump; otherwise `OnExit` sets `EndDashSpeed`.
- Jump metrics are level-design contracts locked by `Tests/JumpMetricsTest.cs`: a tap clears 1.5 tiles (not 2), a held jump clears 4 (not 5), a held running jump covers ≥ 5 tiles horizontally, a tap from standstill ≥ 2. Retuning `Jump*`/`Gravity` must keep them passing.
- `PlayerInput` (`Utility/`) is a static wrapper over `Godot.Input` actions; use it instead of calling `Input` in states.
- `PlayerPresentation : AnimationPlayer` is presentation only (`_Process`): flips the sprite by `Player.Facing` and drives `AnimationTree` blend positions from `Player.Velocity`. Physics logic goes in `_PhysicsProcess`/states, animation in `_Process`.

`Player.DebugStates` (export) logs state changes through `StateChanged`; `Player.CurrentState` exposes the FSM state for tests.

### Camera (`Camera/`)

- `GameCamera : Camera2D` (prefab `Dev/Prefabs/game_camera.tscn`, zoom 2 = 640×360 world px) lives in the level, not in the player, and reads `Target` (the `Player`) by polling only. It ticks in `_PhysicsProcess` with `ProcessPhysicsPriority = 100` so it runs after the player in the same frame.
- Per tick: target = focus + smoothed look-ahead (by `Facing` × speed) and `_anchorY` + look-down (fast fall) → `CameraZone` override → clamp to room → exponential damping (`Damp`) → hard margins (the player never gets within `MarginX/Y` of the screen edge) → clamp to room again → shake on `Offset`.
- `_anchorY` follows the player only on the floor or in wall slide; in the air it moves only past `AirDeadZoneUp/Down`, so normal jumps don't move the camera vertically.
- `CameraRoom`/`CameraZone` extend `CameraArea : Area2D`; bounds come from a child `CollisionShape2D` with a `RectangleShape2D` (monitoring is turned off). A room smaller than the view on an axis centers the camera on that axis. Entering another room tweens the camera over `TransitionDuration` with `Player.Frozen = true` (`_PhysicsProcess` returns before `TickTimers`). Use `SnapToTarget()` after teleports/respawns.
- Physics interpolation and `snap_2d_transforms_to_pixel` are on: the camera is not rounded by hand. Move/teleport nodes in physics ticks and call `ResetPhysicsInterpolation()` after teleports.
