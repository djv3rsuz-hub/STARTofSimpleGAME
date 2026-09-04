# SimpleWPFGame - Project Guide

> Open this file first before making any changes to the project.

## Quick Start

```bash
dotnet build -c Debug    # Always build debug
dotnet run               # Launch game
```

**Default Controls:** WASD = Move, Space/B = Dash, J = Attack, U = Heavy, I = Dodge, L = Parry, K = Block, F3 = Collision Debug, F5 = Quick Save, F6 = Hitbox Debug, F7 = 3D View, F9 = Quick Load, ESC = Options

---

## Project Structure

```
STARTofSimpleWPFGame/
├── Config/                 # Character data (INI-based, user-editable)
│   ├── CharSettings.cs     # INI parser + CharacterData model
│   ├── CharacterStats.cs   # 50+ stats, AI profiles, resistances
│   ├── charsettings.ini    # All cube stats (edit in Notepad)
│   └── IniParser.cs        # Generic INI file reader
├── Combat/                 # Combat system
│   ├── CombatTypes.cs      # CombatState, FrameData, Hitbox, AttackResult
│   ├── Weapon.cs           # Weapon base + Sword with 3-hit combo
│   ├── CombatCalculator.cs # Damage math, crit/dodge/parry rolls
│   ├── CombatSystem.cs     # CombatComponent state machine + hitbox tracking
│   └── DamageNumbers.cs    # Floating damage/heal/crit text
├── Game/                   # Core engine
│   ├── GameEngine.cs       # 60fps loop, collision, combat hit detection
│   ├── GameObject.cs       # Base class (position, bounds, collision)
│   └── Cube.cs             # Player/enemy/NPC with combat, movement, dash
├── Input/                  # Controller + keyboard/mouse
│   ├── ControllerManager.cs  # XInput via SharpDX (120Hz polling)
│   └── InputManager.cs     # Keyboard + mouse state tracking
├── Logging/
│   └── Logger.cs           # File + UI logging
├── Rendering3D/            # 3D rendering system
│   ├── MeshData.cs         # 3D mesh data + Transform3DComponent
│   ├── MeshFactory.cs      # Primitives: cube/sphere/cylinder/plane/pyramid
│   ├── Scene3D.cs          # Viewport3D + camera + lighting
│   └── MeshRenderer.cs     # Singleton managing 3D objects in scene
├── SaveSystem/             # Save/Load system
│   ├── SaveData.cs         # Serializable data models
│   └── SaveManager.cs      # Save/Load/Apply logic
├── Settings/
│   └── GameSettings.cs     # All settings including 3D camera
├── UI/                     # UI components
│   ├── GameIcons.cs        # Anime-style tab icons
│   ├── TooltipManager.cs   # Custom cursor-following tooltip
│   └── TooltipDescriptions.cs  # All tooltip text (centralized)
├── VFX/                    # Visual effects
│   ├── Particle.cs         # Single particle
│   ├── VFXSystem.cs        # Particle manager
│   ├── FireEffect.cs       # Fire (4 presets)
│   ├── LightningEffect.cs  # Branching bolts
│   ├── LaserEffect.cs      # Pulsing beams
│   ├── SmokeEffect.cs      # Steam/smoke (4 presets)
│   └── WaterEffect.cs      # Fountain/splash
├── MainWindow.xaml         # Tab UI, stats panels, 3D view, options
├── MainWindow.xaml.cs      # All UI logic + console commands
├── App.xaml                # Button styles (GameButton)
└── STARTofSimpleWPFGame.csproj
```

---

## Architecture Principles

### 1. Singleton Pattern
Used for managers that need global access:
```csharp
GameSettings.Instance    // Settings (load/save)
GameEngine.Instance      // Game loop + rendering
InputManager.Instance    // Keyboard/mouse state
ControllerManager.Instance // Gamepad state
TooltipManager.Instance  // Tooltip display
SaveManager.Instance     // Save/load system
VFXSystem.Instance       // Particle effects
DamageNumberSystem.Instance // Floating damage text
MeshRenderer.Instance    // 3D object management
```

### 2. Data-Driven Design
All character stats live in `charsettings.ini`, not hardcoded:
```ini
[PlayerCube]
Size = 60
MoveSpeed = 720
HP = 1.00
Attack = 0.15
DashEnabled = true
# 12 Resistances
PhysicalResist = 0.10
FireResist = 0.05
# AI Profile
AIAggression = 0.70
```
Edit in Notepad, restart game to apply.

### 3. Stats as Float (0.00 format)
All RPG stats use float 0.00 = 0%, 1.00 = 100%:
```csharp
Stats.HP = 1.00f;        // 100% health
Stats.Attack = 0.15f;    // 15% attack
Stats.CriticalDamage = 1.50f;  // 150% crit multiplier
```

---

## Combat System

### Frame Data Format
```ini
# startup, active, recovery, parryWindow, dodgeWindow, comboDamageMultiplier
AttackStartup = 6
AttackActive = 3
AttackRecovery = 12
```

### Combat States
- **Idle** - Normal state
- **Attacking** - Playing attack animation
- **ComboAttacking** - In combo chain
- **Dodging** - i-frames active
- **Blocking** - Reduced damage
- **Parrying** - Counter window
- **Countering** - Post-parry attack
- **Stunned** - Can't act
- **Dead** - HP = 0

### Controller Bindings
- X = Attack (combo), Y = Heavy Attack
- A = Dodge, LB = Parry, RB = Block
- Space = B = Dash

### Keyboard Bindings
- J = Attack, U = Heavy, I = Dodge
- L = Parry, K = Block
- WASD = Move, Space = Dash

---

## 3D System

### Controls
- F7 = Toggle 3D view
- Mouse drag = Rotate camera
- Scroll = Zoom in/out

### Creating 3D Objects
```csharp
var renderer = MeshRenderer.Instance;
renderer.AddCube(1.0, Colors.Blue, new Point3D(0, 0, 0));
renderer.AddSphere(0.5, Colors.Red, new Point3D(2, 0, 0));
renderer.AddCylinder(0.3, 1.5, Colors.Green, new Point3D(-2, 0, 0));
renderer.AddPlane(5, 5, Colors.Gray, new Point3D(0, -1, 0));
renderer.AddPyramid(0.8, Colors.Purple, new Point3D(0, 0, 2));
```

### Camera Settings (GameSettings.cs)
```csharp
Show3DView = false      // Toggle with F7
CameraDistance = 10.0    // Zoom level
CameraAngleX = 30.0     // Pitch
CameraAngleY = 0.0      // Yaw
```

---

## How to Add New Features

### Add a New Stat
1. Add property to `Config/CharacterStats.cs`
2. Add to `Config/charsettings.ini` under each cube
3. Add loader in `Config/CharSettings.cs`
4. Add display in `MainWindow.xaml` left sidebar
5. Add tooltip in `UI/TooltipDescriptions.cs`
6. Add to `SaveSystem/SaveData.cs` StatsSaveData

### Add a New Combat Move
1. Add to `Combat/Weapon.cs` - extend weapon class
2. Add frame data to `Combat/CombatTypes.cs` FrameData
3. Wire into `Combat/CombatSystem.cs` state machine
4. Add controller/keyboard binding in `Cube.cs` TryCombatInput
5. Add charsettings.ini entries for frame data

### Add a New Console Command
1. Add help text in `RunConsoleCommand` case `"help":`
2. Add case in the switch statement

### Add a New 3D Primitive
1. Add mesh generation to `Rendering3D/MeshFactory.cs`
2. Add convenience method to `Rendering3D/MeshRenderer.cs`

---

## Key Files to Know

| File | What it does | When to edit |
|------|-------------|--------------|
| `charsettings.ini` | Character stats, AI, resistances | Changing cube properties |
| `GameSettings.cs` | All game settings + 3D camera | Adding new settings |
| `Cube.cs` | Combat, movement, dash, rendering | Changing cube behavior |
| `GameEngine.cs` | Game loop, collision, combat | Changing game rules |
| `MainWindow.xaml` | Tab UI layout | Changing UI |
| `MainWindow.xaml.cs` | UI logic, console, 3D | Adding commands/features |
| `CombatSystem.cs` | Combat state machine | Changing combat logic |
| `MeshFactory.cs` | 3D mesh generation | Adding 3D primitives |
| `TooltipDescriptions.cs` | All tooltip text | Adding/changing tooltips |
| `SaveData.cs` | Save file format | Adding new save fields |

---

## Save/Load System

- **F5** = Quick Save to slot 1 (JSON file in `Saves/` folder)
- **F9** = Quick Load from slot 1
- Console: `save [slot]`, `load [slot]`, `saves`, `delsave <slot>`
- Saves full game state: positions, velocities, stats, combat, settings

---

## Performance Notes

- Game loop: 60fps via DispatcherTimer (1ms interval)
- UI updates: 30fps (33ms interval)
- Controller polling: 120Hz (separate timer)
- Rendering: Cached pens/brushes, quality-tiered shadows
- Combat: Frame-based hitboxes, pixel-perfect parry/dodge
- 3D: Viewport3D with ambient + directional lighting

---

## Git Workflow

```bash
git add -A
git commit -m "Description of changes"
git push
```

Always build and verify before committing. Never commit secrets or keys.
