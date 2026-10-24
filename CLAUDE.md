# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

Godot 4.7 / C# (`Godot.NET.Sdk/4.7.1`, net8.0) 2D platformer. Respond to the user in pt-BR; code and identifiers in en-US.

## Commands

```bash
godot --path .        # open editor (run the game from the editor / `godot --path . Dev/Levels/teste_level.tscn`)
dotnet build          # compile C# (configs: Debug, ExportDebug, ExportRelease)
```

No tests or linter are set up. Formatting follows [.editorconfig](.editorconfig).

## Architecture

All scripts live in `Dev/Code/Scripts/`; scenes in `Dev/Prefabs/` (`player.tscn`) and `Dev/Levels/` (`teste_level.tscn`). The `BetterTerrain` addon is autoloaded. Display is 1280×720 with viewport stretch, `keep_height`, integer scale. Input actions (`Move_Left/Right/Up/Down`, `Jump`, `Dash`) are defined in `project.godot`.

### FSM (`Nosy/`, namespace `NosyCore.FSM`, the only namespace in the project)

A plain C# (non-Node) state machine. Files outside `Nosy/` have no namespace.

- `FiniteStateMachine` keys its node dictionary by `state.GetType()`: **one instance per concrete state type**; two instances of the same type collapse into one.
- `Update(delta)` (called from the owner's `_Process`) evaluates transitions (any-transitions first, then the current state's) and then calls `OnUpdate`. `FixedUpdate` (called from `_PhysicsProcess`) only runs the current state's `OnFixedUpdate`. So transitions happen at render frame rate, not in the physics tick.
- Transitions are `IPredicate`s; use `new FuncPredicate(() => ...)`. A null lambda throws `NotImplementedException`.
- States extend `BaseState<TContext>` (gives typed `_context`; `TContext : IStateContext`), not `IState` directly.

### Player (`Player/`)

- `Player : CharacterBody2D, IStateContext` builds the three states and registers all transitions in `_Ready()`: grounded↔air (via `IsOnFloor()`), grounded/air→dash (`PlayerInput.DashPressed`), dash→grounded/air (`DashEnded` + floor check).
- States are one class per file in `Player/States/` and hold only per-state logic; shared movement helpers (`ProcessGravity`, `ProcessInputMovement`, `TryJump`, `DoMoveAndSlide`, `IsWallSliding`) live on `Player`.
- **Two-velocity model**: `KinematicVel` (input-driven horizontal movement) and `DynamicVel` (gravity, jump, wall jump, dash impulse). `DoMoveAndSlide()` sets `Velocity = KinematicVel + DynamicVel`, applies horizontal drag to `DynamicVel.X`, then calls `MoveAndSlide()`. States mutate these fields (sometimes `ref _context.DynamicVel`) rather than `Velocity` directly.
- Per-state `OnFixedUpdate` order: grounded = gravity → jump → input movement → move; air = wall-slide check (else gravity + air input movement; wall-jump impulse when sliding and jump pressed) → move; dash = move only (velocity set once in `OnEnter`: `MoveAxis * Speed * 3` into `DynamicVel`, `KinematicVel` zeroed, ends after 100 ms via `Time.GetTicksMsec`).
- `PlayerInput` (`Utility/`) is a static wrapper over `Godot.Input` actions; use it instead of calling `Input` in states.
- `PlayerPresentation : AnimationPlayer` is presentation only (`_Process`): flips the sprite and drives `AnimationTree` blend positions from `Player.Velocity`. Physics logic goes in `_PhysicsProcess`/states, animation in `_Process`.

Note: `Player._PhysicsProcess` currently has a debug `GD.Print` of velocities every tick.
