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
├── Rendering3D/            # True 3D engine
│   ├── MeshData.cs         # 3D mesh data + Transform3DComponent
│   ├── MeshFactory.cs      # Primitives: cube/sphere/cylinder/plane/pyramid
│   ├── Scene3D.cs          # Viewport3D + camera + lighting
│   ├── MeshRenderer.cs     # Singleton managing 3D objects in scene
│   ├── World3D.cs          # 2D<->3D coord mapping, ground grid, arena
│   ├── Collision3D.cs      # AABB3D, Sphere3D, Ray3D, swept collision
│   ├── SpatialHashGrid3D.cs # Spatial hash for broad-phase collision
│   ├── Hitbox3D.cs         # 3D hitbox system + manager
│   ├── MeshSync3D.cs       # Sync 2D cubes to 3D meshes + health bars
│   ├── GameWorld3D.cs      # Orchestrates 3D world + combat sync
│   ├── FramePerfectTimer.cs # Frame-perfect combat timing
│   ├── ObjectPool3D.cs     # Object pooling for performance
│   └── DebugRenderer3D.cs  # Hitbox/bounds/spatial hash visualization
├── SaveSystem/             # Save/Load system
│   ├── SaveData.cs         # Serializable data models
│   └── SaveManager.cs      # Save/Load/Apply logic
├── Settings/
│   └── GameSettings.cs     # All settings including 3D camera
├── UI/                     # UI components
│   ├── GameIcons.cs        # Anime-style tab icons
│   ├── TooltipManager.cs   # Custom cursor-following tooltip
│   └── TooltipDescriptions.cs  # All tooltip text (centralized)
├── AI/                     # Advanced AI system
│   ├── AIBrain.cs          # Utility AI core
│   ├── AIMemory.cs         # Player pattern tracking
│   ├── AIPredictor.cs      # Markov-chain prediction
│   ├── AILearner.cs        # Adaptive difficulty
│   ├── AIActionEvaluator.cs # 16-action scoring
│   ├── AIController.cs     # Decision execution
│   └── AIDebug.cs          # Debug overlay
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

### Console Commands
- `3d` - Toggle 3D view
- `3d hitboxes` - Toggle 3D hitbox visualization
- `3d bounds` - Toggle 3D AABB bounds
- `3d grid` - Toggle spatial hash grid
- `3d reset` - Reset camera to default
- `3d info` - Show 3D stats

### Architecture
```
World3D        - 2D<->3D coordinate mapping, ground grid, arena walls
Collision3D    - AABB3D, Sphere3D, Ray3D, swept collision detection
SpatialHash    - Broad-phase collision culling (O(1) lookups)
Hitbox3D       - Frame-timed 3D hitboxes synced from 2D combat
MeshSync3D     - Real-time mesh updates, health bars, name plates
GameWorld3D    - Orchestrates everything, registers cubes
FramePerfect   - Sub-frame accuracy combat timing
DebugRenderer  - Visual hitbox/bounds/spatial hash overlay
```

### Creating 3D Objects
```csharp
var renderer = MeshRenderer.Instance;
renderer.AddCube(1.0, Colors.Blue, new Point3D(0, 0, 0));
renderer.AddSphere(0.5, Colors.Red, new Point3D(2, 0, 0));
renderer.AddCylinder(0.3, 1.5, Colors.Green, new Point3D(-2, 0, 0));
renderer.AddPlane(5, 5, Colors.Gray, new Point3D(0, -1, 0));
renderer.AddPyramid(0.8, Colors.Purple, new Point3D(0, 0, 2));
```

### 3D Collision (AABB3D)
```csharp
var box = new AABB3D(center, halfExtents);
bool hit = box.Intersects(otherBox);
SweepResult3D sweep = CollisionMath3D.SweepAABB(moving, velocity, stationary, maxTime, out result);
var closest = CollisionMath3D.ClosestPointOnAABB(point, box);
```

### Spatial Hash Grid
```csharp
var grid = new SpatialHashGrid3D(cellSize: 1.5);
grid.Rebuild(bodies, worldMin, worldMax);
var nearby = grid.QueryAABB(queryBox);
grid.QueryPotentialCollisions(body, results);
```

### Camera Settings (GameSettings.cs)
```csharp
Show3DView = false      // Toggle with F7
CameraDistance = 10.0    // Zoom level
CameraAngleX = 30.0     // Pitch
CameraAngleY = 0.0      // Yaw
```

---

## AI System

### Architecture
```
AI/
├── AIBrain.cs          # Utility AI core: personality, difficulty, action scoring
├── AIMemory.cs         # Player pattern tracking, action history, transition prediction
├── AIPredictor.cs      # Markov-chain player action prediction
├── AILearner.cs        # Adaptive difficulty, player skill estimation
├── AIActionEvaluator.cs # 16-action scoring with 6 bonus categories
├── AIController.cs     # Decision execution, context building, movement
└── AIDebug.cs          # Debug overlay, decision log, status text
```

### Initialization
```csharp
// Auto-initialized on enemy cubes in MainWindow
cube.InitializeAI(targetCube, AIPersonality.Aggressive, AIDifficulty.Normal);

// Or manually
var brain = new AIBrain();
brain.Initialize(AIPersonality.Balanced, AIDifficulty.Hard);
brain.Update(context, deltaTime);
AIAction action = brain.GetChosenAction(context);
```

### AI Personalities (10)
- **Balanced** - Well-rounded, adaptive
- **Aggressive** - High aggression, seeks damage
- **Defensive** - Cautious, blocks/parries more
- **Berserker** - Low HP = high damage
- **Assassin** - Fast, dodges frequently
- **Tank** - High defense, slow attacks
- **ParryMaster** - Prioritizes parry/counter
- **DodgeMaster** - Dodges everything
- **CounterMaster** - Punishes mistakes

### AI Difficulties (6)
- **Easy** - Slow reactions, poor accuracy
- **Normal** - Balanced
- **Hard** - Faster reactions, better predictions
- **Expert** - Near-perfect timing
- **Nightmare** - Frame-perfect parries
- **Adaptive** - Learns player patterns

### AI Actions (16)
Attack, HeavyAttack, Block, Parry, Dodge, Dash, DashAttack, Wait, Reposition, Chase, Retreat, Heal, Taunt, GuardBreak, Feint, PerfectParry

### Console Commands
- `ai info` - Show AI brain status, personality, difficulty
- `ai scores` - Show top 5 action scores
- `ai log` - Show recent AI decisions
- `ai difficulty <easy|normal|hard|expert|nightmare|adaptive>` - Change difficulty
- `ai reset` - Reset all AI learning

### AI Pattern Tracking
```csharp
// Memory tracks player patterns
AIMemory memory = new AIMemory();
memory.RecordAction(AIAction.Attack);
double aggression = memory.GetPlayerAggression(20);  // 0.0-1.0
double pattern = memory.GetPlayerPatternScore("AttackBlockAttack");
AIAction predicted = memory.PredictNextPlayerAction(3);
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
| `Cube.cs` | Combat, movement, dash, AI | Changing cube behavior |
| `GameEngine.cs` | Game loop, collision, combat | Changing game rules |
| `MainWindow.xaml` | Tab UI layout | Changing UI |
| `MainWindow.xaml.cs` | UI logic, console, 3D, AI | Adding commands/features |
| `CombatSystem.cs` | Combat state machine | Changing combat logic |
| `AIBrain.cs` | Utility AI core, scoring | Changing AI decisions |
| `AIController.cs` | AI execution, context | Changing AI behavior |
| `AIMemory.cs` | Player pattern tracking | Changing memory system |
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
