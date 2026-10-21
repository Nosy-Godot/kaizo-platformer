# kaizo-project

Godot 4.7 / C# / net8.0 — 2D platformer (640×360, integer stretch). **Responder ao usuário em pt-BR** (código em inglês).

## Estrutura

```
project.godot    # Jolt Physics 3D, addon BetterTerrain autoload (Sprint deadzone 0.5)
*.csproj / *.sln # Net8.0 (Android → net9.0 via $(GodotTargetPlatform))
Dev/
├ Code/Scripts/
│  ├ Nosy/       # FSM própria: `NosyCore.FSM` (único namespace do projeto)
│  └ Player/     # Player.cs + PlayerStates.cs (3 states num arquivo)
├ Prefabs/        # TSCN
└ Levels/         # TSCN
```

Arquivos .cs raiz: **sem namespace** (`public partial class`). FSM é `partial` (herda de `CharacterBody2D` + `IStateContext`).

## Convenções

- Código/namespaces: **en-US**. Respostas ao usuário: **pt-BR**.
- Lógica física: `_PhysicsProcess`. Apresentação/animação: `_Process`.
- Estados do player em um único arquivo (ver `Dev/Code/Scripts/Player/States/PlayerStates.cs`).

## FSM (NosyCore.FSM) — gotchas críticos

1. **Caching por tipo exato**: FSM usa `state.GetType()` como chave no `Dictionary`. Two states com mesmo base type compartilham nó.
2. Transições avaliadas na `Update(delta)` do FSM (não no physics tick). Adicione ao `AddTransition` ou `AddAnyTransition`.
3. `BaseState<T>` dá `_context` tipado. Estenda-o, não `IState` direta.
4. `FuncPredicate(null)` → lança `NotImplementedException` (IPredicate.cs:14) — nunca omita o lambda.

## Fluxo de estado do player (`OnFixedUpdate` em cada state)

```
grounded:  gravity → jump → movement → slide
air:       gravity → movement → slide
dash:      velocity fix (3× speed, 100ms one-shot direction on Enter) → slide
```

Transições FSM registradas em `Player._Ready()`: grounded ↔ air, grounded/air → dash, dash → grounded/air.

## Comandos

```bash
godot --path .     # editor
dotnet build       # compile (Godot.NET.Sdk/4.7.1)
```

Configurações da solution: `Debug`, `ExportDebug`, `ExportRelease`.
