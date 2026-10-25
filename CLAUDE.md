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

- `Player : CharacterBody2D` builds the three states and registers all transitions in `_Ready()`: grounded↔air (via `IsOnFloor()`), grounded/air→dash (`PlayerInput.DashPressed`), dash→grounded/air (`DashEnded` + floor check).
- States are one class per file in `Player/States/` and hold only per-state logic; shared movement helpers (`ProcessGravity`, `ProcessInputMovement`, `TryJump`, `DoMoveAndSlide`, `IsWallSliding`) live on `Player`.
- **Two-velocity model**: `KinematicVel` (input-driven horizontal movement) and `DynamicVel` (gravity, jump, wall jump, dash impulse). `DoMoveAndSlide()` sets `Velocity = KinematicVel + DynamicVel`, applies horizontal drag to `DynamicVel.X`, then calls `MoveAndSlide()`. States mutate these fields (sometimes `ref _context.DynamicVel`) rather than `Velocity` directly.
- Per-state `OnFixedUpdate` order: grounded = gravity → jump → input movement → move; air = wall-slide check (else gravity + air input movement; wall-jump impulse when sliding and jump pressed) → move; dash = move only (velocity set once in `OnEnter`: `MoveAxis * Speed * 3` into `DynamicVel`, `KinematicVel` zeroed, ends after `DashDurationTicks` physics ticks).
- `PlayerInput` (`Utility/`) is a static wrapper over `Godot.Input` actions; use it instead of calling `Input` in states.
- `PlayerPresentation : AnimationPlayer` is presentation only (`_Process`): flips the sprite and drives `AnimationTree` blend positions from `Player.Velocity`. Physics logic goes in `_PhysicsProcess`/states, animation in `_Process`.

`Player.DebugStates` (export) logs state changes through `StateChanged`; `Player.CurrentState` exposes the FSM state for tests.
